using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>Cartes faites à la main (D-110) : conversion d'une HandMap en plan d'acte.</summary>
    public sealed partial class ActPlan
    {
        /// <summary>Carte d'origine (null : plan tiré au hasard).</summary>
        public HandMap Hand;
        /// <summary>Cases par caractère du plan et marge (cases) autour.</summary>
        public int HandK = 1, HandMargin;
        /// <summary>Salles d'ancrage des coffres et du marchand (vides : toutes).</summary>
        public readonly List<int> ChestAnchors = new List<int>(), MerchantAnchors = new List<int>();
        public readonly Dictionary<char, int> RoomOfChar = new Dictionary<char, int>();

        /// <summary>Case de la grille au coin bas-gauche d'un point du plan (x vers la droite, y vers le bas, en caractères).</summary>
        public Vector2 HandToCell(Vector2 p) => new Vector2(HandMargin + p.x * HandK, HandMargin + (Hand.Height - p.y) * HandK);

        public Vector3 HandToWorld(Vector2 p, float y) => Origin + new Vector3(HandToCell(p).x * Cell, y, HandToCell(p).y * Cell);

        public static ActPlan FromHand(ActLayoutSettings settings, HandMap map, int seed)
        {
            var p = new ActPlan { Settings = settings, Seed = seed, Cell = settings.cell, Hand = map };
            p.HandK = Mathf.Max(1, Mathf.RoundToInt(map.Scale / p.Cell));
            p.HandMargin = 8;
            p.Size = Mathf.Max(map.Width, map.Height) * p.HandK + p.HandMargin * 2;
            p.Origin = new Vector3(-p.Size * p.Cell * 0.5f, 0f, -p.Size * p.Cell * 0.5f);
            p.Reset(seed);
            foreach (string e in map.Errors)
                p.Failures.Add("carte : " + e);
            p.BuildHand();
            return p;
        }

        private void BuildHand()
        {
            HandMap m = Hand;
            // Salles : cases de chaque caractère.
            var cellsOf = new Dictionary<char, List<Vector2Int>>();
            for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                char c = m.At(x, y);
                if (c == '.')
                    continue;
                if (!cellsOf.TryGetValue(c, out List<Vector2Int> list))
                    cellsOf[c] = list = new List<Vector2Int>();
                int cx = HandMargin + x * HandK, cy = HandMargin + (m.Height - 1 - y) * HandK;
                for (int i = 0; i < HandK; i++)
                for (int j = 0; j < HandK; j++)
                    list.Add(new Vector2Int(cx + i, cy + j));
            }

            var handCeiling = new HashSet<int>();
            foreach (HandMap.Room hr in m.Rooms)
            {
                if (!cellsOf.TryGetValue(hr.Id, out List<Vector2Int> cells))
                    continue;
                int id = Commit(null, default, -1, hr.Role, hr.Floor, cells);
                ActRoom r = Rooms[id];
                RoomOfChar[hr.Id] = id;
                r.Name = hr.Name;
                r.Function = string.IsNullOrEmpty(hr.Function) ? hr.Name : hr.Function;
                r.Feature = hr.Feature;
                if (hr.Ceiling >= 0f)
                {
                    r.Ceiling = hr.Ceiling;
                    handCeiling.Add(id);
                }
                switch (hr.Role)
                {
                    case RoomRole.Depart: Start = id; break;
                    case RoomRole.Sortie: Exit = id; break;
                    case RoomRole.HallDeSortie: ExitHall = id; break;
                    case RoomRole.Arrivee: Arrival = id; break;
                }
            }

            // Portes, dans l'ordre du fichier.
            foreach (HandMap.Door hd in m.Doors)
            {
                if (!RoomOfChar.TryGetValue(hd.A, out int a) || !RoomOfChar.TryGetValue(hd.B, out int b))
                    continue;
                if (!HandDoorRun(a, b, hd, out Vector2Int first, out Vector2Int dir, out int len))
                {
                    Failures.Add($"porte {hd.A}-{hd.B} (ligne {hd.Line}) : les salles ne se touchent pas{(hd.Side != '\0' ? " de ce côté" : "")}");
                    continue;
                }

                ActDoor d = MakeDoor(a, b, first, dir, len);
                d.Locked = hd.Locked || Rooms[a].Role == RoomRole.Verrouillee || Rooms[b].Role == RoomRole.Verrouillee;
                d.Exit = hd.Exit || (Rooms[a].Role == RoomRole.HallDeSortie || Rooms[b].Role == RoomRole.HallDeSortie);
                d.Gate = hd.Gate || Rooms[a].Role == RoomRole.Arrivee || Rooms[b].Role == RoomRole.Arrivee;
                // Palier possible seulement s'il ne contredit pas la galerie écrite de la salle basse (une hauteur par salle).
                HandMap.Room lowHand = m.Find(Rooms[a].Floor < Rooms[b].Floor ? hd.A : hd.B);
                float dz = Mathf.Abs(Rooms[a].Floor - Rooms[b].Floor);
                bool landingOk = lowHand == null || lowHand.TerraceHeight < 0.01f || Mathf.Abs(lowHand.TerraceHeight - dz) < 0.05f;
                // Vers une salle basse à fosse ou à bassin : rampe (12 m de large, le traqueur la descend), pas de palier à escalier étroit.
                if (lowHand != null && lowHand.Feature != RoomFeature.Aucun)
                    landingOk = false;
                bool ok = hd.High ? PlanHighLanding(d) : hd.Ramp ? PlanRamp(d) : PlanConnection(d, allowLanding: landingOk);
                if (!ok)
                    Failures.Add($"porte {hd.A}-{hd.B} (ligne {hd.Line}) : pas de place pour {(hd.High ? "le palier" : "la rampe ou l'escalier")} "
                                 + $"(dénivelé {Mathf.Abs(Rooms[a].Floor - Rooms[b].Floor):0.#} m)");
                AddDoor(d);
            }

            // Galeries écrites à la main (le long des côtés demandés).
            foreach (HandMap.Room hr in m.Rooms)
                if (RoomOfChar.TryGetValue(hr.Id, out int id) && hr.TerraceHeight > 0.01f && hr.TerraceSides.Length > 0)
                    MarkHandTerrace(Rooms[id], hr);

            foreach (char c in m.ChestRooms)
                if (RoomOfChar.TryGetValue(c, out int id))
                    ChestAnchors.Add(id);
            foreach (char c in m.MerchantRooms)
                if (RoomOfChar.TryGetValue(c, out int id))
                    MerchantAnchors.Add(id);

            // Noms uniques (« Cachot », « Cachot II »…) comme pour les plans tirés.
            var seen = new Dictionary<string, int>();
            foreach (ActRoom r in Rooms)
            {
                seen.TryGetValue(r.Name, out int n);
                seen[r.Name] = n + 1;
                if (n > 0)
                    r.Name = $"{r.Name} {Roman(n + 1)}";
            }

            if (Start >= 0)
            {
                int[] depth = Depths(Start);
                for (int i = 0; i < Rooms.Count; i++)
                    Rooms[i].Depth = depth[i];
                if (Exit >= 0)
                    StartExitDepth = depth[Exit];
                for (int i = 0; i < Rooms.Count; i++)
                    if (depth[i] < 0 || depth[i] >= int.MaxValue / 2)
                        Failures.Add($"salle {Rooms[i].Name} : inaccessible depuis le départ");
            }

            if (m.Organic > 0.5f)
                CarveOrganic(Mathf.Max(1, Mathf.RoundToInt(m.Organic / Cell)));
            foreach (ActRoom r in Rooms)
                PlanInterior(r);
            foreach (ActRoom r in Rooms)
                FillPockets(r);
            foreach (ActRoom r in Rooms)
                if (!r.Special && r.Doors.Count < 2)
                    Failures.Add($"salle {r.Name} : impasse ({r.Doors.Count} sortie)");
            LowerSmallCeilings(handCeiling);
        }

        /// <summary>
        /// Seconde revue de la DA (D-120) : les petites salles (couloirs, halls, salles verrouillées) n'ont plus un plafond de 22 à 30 m
        /// qui fait une cheminée noire. Plafond abaissé à smallRoomCeiling, jamais sous le traqueur debout (il se baisserait et ralentirait)
        /// ni sous le haut des portes (arc compris). Pas dans les salles à galerie, à palier, à fosse ou à bassin (bonds du traqueur vers
        /// les étages et les îlots), ni à la porte d'arrivée (herse de 11 m), ni quand la carte écrit sa hauteur.
        /// </summary>
        private void LowerSmallCeilings(HashSet<int> handCeiling)
        {
            if (Settings.Open || Settings.smallRoomCeiling <= 0f)
                return;
            foreach (ActRoom r in Rooms)
            {
                if (r.Ceiling <= 0f || handCeiling.Contains(r.Id) || r.Role == RoomRole.Arrivee || r.Role == RoomRole.Repaire
                    || r.Feature != RoomFeature.Aucun || r.HasLanding || r.TerraceHeight > 0.01f || r.Use.Values.Any(u => u == CellUse.Terrasse))
                    continue;
                float narrow = Mathf.Min(r.Box.width, r.Box.height) * Cell;
                if (narrow > Settings.smallRoomWidth && r.Area(Cell) > Settings.smallRoomArea)
                    continue;
                float need = Mathf.Max(Settings.smallRoomCeiling, StalkerStandingHeight);
                foreach (int di in r.Doors)
                {
                    ActDoor d = Doors[di];
                    ActRoom o = Rooms[d.Other(r.Id)];
                    float opening = d.Gate ? Mathf.Max(Settings.doorHeight, 11f)
                        : d.Exit && Settings.art != null && Settings.art.exitDoor != null ? Mathf.Max(Settings.doorHeight, Settings.art.exitDoorOpening) : Settings.doorHeight;
                    need = Mathf.Max(need, Mathf.Max(r.Floor, o.Floor) - r.Floor + opening + 2f);
                }

                if (need < r.Ceiling - 0.5f)
                    r.Ceiling = need;
            }
        }

        /// <summary>Hauteur du traqueur debout (14 m) et une marge : au-dessous, il se baisse et ralentit.</summary>
        public const float StalkerStandingHeight = 15f;

        /// <summary>
        /// Ouverture d'une porte écrite à la main : la plus longue limite commune (du côté demandé), à la position demandée.
        /// first : case de A au bord de l'ouverture ; dir : de A vers B.
        /// </summary>
        private bool HandDoorRun(int a, int b, HandMap.Door hd, out Vector2Int first, out Vector2Int dir, out int len)
        {
            first = default;
            dir = default;
            len = 0;
            var setA = new HashSet<Vector2Int>(Rooms[a].Cells);
            var edges = new Dictionary<(Vector2Int d, int line), List<int>>();
            Vector2Int? want = hd.Side switch { 'n' => Vector2Int.up, 's' => Vector2Int.down, 'e' => Vector2Int.right, 'o' => Vector2Int.left, _ => (Vector2Int?)null };
            foreach (Vector2Int c in Rooms[b].Cells)
                foreach (Vector2Int d in Dirs)
                {
                    Vector2Int o = c - d;
                    if (!setA.Contains(o) || (want.HasValue && d != want.Value))
                        continue;
                    int line = d.x != 0 ? o.x : o.y, pos = d.x != 0 ? o.y : o.x;
                    if (!edges.TryGetValue((d, line), out List<int> l))
                        edges[(d, line)] = l = new List<int>();
                    l.Add(pos);
                }

            (Vector2Int d, int line, int from, int n) best = default;
            foreach (KeyValuePair<(Vector2Int d, int line), List<int>> kv in edges)
            {
                List<int> l = kv.Value;
                l.Sort();
                int start = 0;
                for (int i = 1; i <= l.Count; i++)
                    if (i == l.Count || l[i] != l[i - 1] + 1)
                    {
                        int n = i - start;
                        if (n > best.n)
                            best = (kv.Key.d, kv.Key.line, l[start], n);
                        start = i;
                    }
            }

            if (best.n < 3)
                return false;
            int doorLen = hd.Narrow ? NarrowCells : Mathf.Max(2, Mathf.RoundToInt(Settings.doorWidth / Cell));
            doorLen = Mathf.Min(doorLen, best.n - 2);
            if (doorLen < 2)
                doorLen = best.n;
            int slack = best.n - doorLen;
            int offset = Mathf.Clamp(Mathf.RoundToInt(slack * hd.At), slack > 1 ? 1 : 0, Mathf.Max(0, slack - (slack > 1 ? 1 : 0)));
            int at = best.from + offset;
            first = best.d.x != 0 ? new Vector2Int(best.line, at) : new Vector2Int(at, best.line);
            dir = best.d;
            len = doorLen;
            return true;
        }

        /// <summary>
        /// Porte haute (D-110) : palier devant la porte, côté bas, à la hauteur de la salle haute, sans escalier : on y monte par
        /// traction, wall jump ou hissage (le traqueur escalade, les goules grimpent). Accepte une galerie déjà écrite à la même hauteur.
        /// </summary>
        private bool PlanHighLanding(ActDoor d)
        {
            float fa = Rooms[d.A].Floor, fb = Rooms[d.B].Floor;
            float h = Mathf.Abs(fa - fb);
            if (h < 0.05f)
            {
                d.RampRoom = -1;
                return true;
            }

            int lowId = fa < fb ? d.A : d.B;
            ActRoom low = Rooms[lowId];
            if (low.TerraceHeight > 0.01f && Mathf.Abs(low.TerraceHeight - h) > 0.05f)
                return false;                                           // une seule hauteur de galerie par salle
            Vector2Int inward = lowId == d.A ? -d.Dir : d.Dir;
            Vector2Int edge0 = lowId == d.A ? d.First : d.First + d.Dir;
            var band = new List<Vector2Int>();
            for (int i = -1; i <= d.Length; i++)
            for (int k = 0; k < 3; k++)
            {
                Vector2Int c = edge0 + d.Along * i + inward * k;
                if (OwnerAt(c) != lowId)
                    return false;
                band.Add(c);
            }

            foreach (Vector2Int c in band)
                low.Use[c] = CellUse.Terrasse;
            low.TerraceHeight = h;
            low.HandTerrace = true;
            var landing = new Landing { Door = Doors.Count, StairsDir = inward, Height = h };
            landing.Cells.AddRange(band);
            low.Landings.Add(landing);
            d.Landing = true;
            d.High = true;
            d.RampRoom = -1;
            d.RampCells.Clear();
            return true;
        }

        /// <summary>
        /// Poches de sol fermées (D-117) : un bout de sol de moins de 60 cases coupé du reste de la salle par une rampe (sans porte, hors
        /// galerie) devient de la roche : on n'y tombe plus pour y rester coincé.
        /// </summary>
        private void FillPockets(ActRoom r)
        {
            if (r.Special)
                return;
            bool Open(Vector2Int c) => r.Use.TryGetValue(c, out CellUse u) && u != CellUse.Rampe;
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int start in r.Cells.ToList())
            {
                if (!Open(start) || !seen.Add(start))
                    continue;
                var comp = new List<Vector2Int> { start };
                for (int i = 0; i < comp.Count; i++)
                    foreach (Vector2Int d in Dirs)
                        if (Open(comp[i] + d) && seen.Add(comp[i] + d))
                            comp.Add(comp[i] + d);
                if (comp.Count >= 30 || comp.Count == r.Cells.Count || !comp.All(c => NearRamp(r, c)))
                    continue;
                var set = new HashSet<Vector2Int>(comp);
                if (r.Doors.Any(di => set.Contains(DoorCellInside(Doors[di], r.Id))) || comp.Any(c => r.Use[c] == CellUse.Terrasse))
                    continue;
                // Le bas d'une rampe y débouche peut-être : gardé si une case touche un passage.
                if (comp.Any(c => r.Use[c] == CellUse.Passage))
                    continue;
                foreach (Vector2Int c in comp)
                {
                    Owner[c.x, c.y] = -1;
                    r.Cells.Remove(c);
                    r.Use.Remove(c);
                }
            }
        }

        /// <summary>Cases laissées libres devant une galerie (12 m).</summary>
        private static bool NearRamp(ActRoom r, Vector2Int c)
        {
            for (int dx = -2; dx <= 2; dx++)
            for (int dy = -2; dy <= 2; dy++)
                if (r.Use.TryGetValue(c + new Vector2Int(dx, dy), out CellUse u) && u == CellUse.Rampe)
                    return true;
            return false;
        }

        private const int GalleryFront = 6;

        /// <summary>
        /// Bords organiques (caves, jardin) : les cases au bord d'une salle sont rendues à la roche selon un bruit (jusqu'à depth cases),
        /// sauf les abords des portes (6 cases), les galeries, les paliers et les salles spéciales. Deux salles voisines se retrouvent
        /// séparées par une épaisseur de roche irrégulière, la porte devient un passage creusé dans la roche. La salle reste d'un tenant.
        /// </summary>
        private void CarveOrganic(int depth)
        {
            float ox = (Seed % 997) * 0.37f, oy = (Seed % 991) * 0.53f;
            var keep = new HashSet<Vector2Int>();
            foreach (ActDoor d in Doors)
                for (int i = -2; i < d.Length + 2; i++)
                for (int k = -4; k <= 5; k++)
                    keep.Add(d.First + d.Along * i + d.Dir * k);
            foreach (ActRoom r in Rooms)
            {
                if (r.Special || r.Role == RoomRole.Depart)
                    continue;
                Dictionary<Vector2Int, int> edge = EdgeDistance(r);
                // Devant une galerie, jamais de roche (D-117) : elle donne toujours sur la salle, pas de recoin fermé hors de portée du traqueur.
                var front = new HashSet<Vector2Int>();
                foreach (Vector2Int c in r.Cells)
                    if (r.Use.TryGetValue(c, out CellUse cu) && cu == CellUse.Terrasse)
                        for (int dx = -GalleryFront; dx <= GalleryFront; dx++)
                        for (int dy = -GalleryFront; dy <= GalleryFront; dy++)
                            front.Add(c + new Vector2Int(dx, dy));
                var carve = new List<Vector2Int>();
                foreach (Vector2Int c in r.Cells)
                {
                    if (keep.Contains(c) || front.Contains(c) || !edge.TryGetValue(c, out int e) || e >= depth || r.Use[c] != CellUse.Sol)
                        continue;
                    // Deux octaves : grandes avancées de roche et petites aspérités.
                    float n = Mathf.PerlinNoise(c.x * 0.07f + ox, c.y * 0.07f + oy) * 0.7f + Mathf.PerlinNoise(c.x * 0.23f + oy, c.y * 0.23f + ox) * 0.3f;
                    if (e < Mathf.FloorToInt(n * (depth + 1.4f)))
                        carve.Add(c);
                }

                if (r.Cells.Count - carve.Count < r.Cells.Count * 0.6f)
                    continue;
                var carveSet = new HashSet<Vector2Int>(carve);
                // D'un tenant : les cases gardées accessibles depuis une porte.
                var left = new HashSet<Vector2Int>(r.Cells.Where(c => !carveSet.Contains(c)));
                var seen = new HashSet<Vector2Int>();
                var q = new Queue<Vector2Int>();
                foreach (int di in r.Doors)
                {
                    Vector2Int s0 = DoorCellInside(Doors[di], r.Id);
                    if (left.Contains(s0) && seen.Add(s0))
                        q.Enqueue(s0);
                }

                while (q.Count > 0)
                {
                    Vector2Int c = q.Dequeue();
                    foreach (Vector2Int d in Dirs)
                        if (left.Contains(c + d) && seen.Add(c + d))
                            q.Enqueue(c + d);
                }

                foreach (Vector2Int c in r.Cells.ToList())
                    if (!seen.Contains(c))
                    {
                        Owner[c.x, c.y] = -1;
                        r.Cells.Remove(c);
                        r.Use.Remove(c);
                    }

                RecomputeBox(r);
            }
        }

        /// <summary>Galerie écrite : bande de TerraceDepth cases le long des côtés demandés, hors abords des portes du sol.</summary>
        private void MarkHandTerrace(ActRoom r, HandMap.Room hr)
        {
            if (r.TerraceHeight > 0.01f && Mathf.Abs(r.TerraceHeight - hr.TerraceHeight) > 0.05f)
            {
                Failures.Add($"salle {r.Name} : galerie à {hr.TerraceHeight} m mais palier à {r.TerraceHeight} m");
                return;
            }

            int depth = hr.TerraceDepth;
            RectInt b = r.Box;
            // Devant chaque porte du sol : encoche de la largeur de la porte + 3 cases de chaque côté, 6 cases de profond (le traqueur
            // fait 7 m de large ; une encoche de 6 m le bloquait derrière la porte).
            var clear = new HashSet<Vector2Int>();
            foreach (int di in r.Doors)
            {
                ActDoor d = Doors[di];
                if (d.High)
                    continue;
                Vector2Int inward = r.Id == d.A ? -d.Dir : d.Dir;
                Vector2Int edge0 = r.Id == d.A ? d.First : d.First + d.Dir;
                for (int i = -3; i < d.Length + 3; i++)
                for (int k = 0; k < 6; k++)
                    clear.Add(edge0 + d.Along * i + inward * k);
            }

            foreach (Vector2Int c in r.Cells)
            {
                if (clear.Contains(c))
                    continue;
                bool inside = false;
                foreach (char s in hr.TerraceSides)
                    inside |= s switch
                    {
                        'n' => c.y >= b.yMax - depth,
                        's' => c.y < b.yMin + depth,
                        'e' => c.x >= b.xMax - depth,
                        'o' => c.x < b.xMin + depth,
                        _ => false,
                    };
                if (!inside || !r.Use.TryGetValue(c, out CellUse u))
                    continue;
                if (u == CellUse.Sol)
                    r.Use[c] = CellUse.Terrasse;
            }

            r.TerraceHeight = hr.TerraceHeight;
            r.HandTerrace = true;
            r.NoTerraceStairs = !hr.TerraceStairs;
        }
    }
}
