using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Ce que le planificateur sait d'un module (lu sur son prefab).</summary>
    public sealed class ModuleInfo
    {
        public int Index;
        public string Name;
        public ModuleKind Kind;
        public float Size;
        public Vector2Int Footprint;
        public ModuleLevel Level;
        public float Weight;
        public bool Rotate;
        public int MinPerMap, MaxPerMap, MinStartDistance;
        public bool ExitAllowed;
        public ModuleHole Hole;
        public bool Parkour;
        public int ChestSpots;
        public ModuleShape Shape;
        public float CeilingOverride;
        public List<(DoorSide side, int cell, bool required)> Doors = new List<(DoorSide, int, bool)>();

        public bool Big => Footprint.x * Footprint.y > 1;

        public static ModuleInfo From(RoomModule m, int index) => new ModuleInfo
        {
            Index = index,
            Name = m.displayName,
            Kind = m.kind,
            Size = m.size,
            Footprint = new Vector2Int(Mathf.Clamp(m.footprint.x, 1, 2), Mathf.Clamp(m.footprint.y, 1, 2)),
            Level = m.level,
            Weight = m.weight,
            Rotate = m.rotate && m.hole != ModuleHole.Canal,
            MinPerMap = m.minPerMap,
            MaxPerMap = m.maxPerMap,
            MinStartDistance = m.minStartDistance,
            ExitAllowed = m.exitAllowed && m.kind == ModuleKind.Salle,
            Hole = m.hole,
            Parkour = m.parkour || m.hole == ModuleHole.Vide,
            ChestSpots = m.GetComponentsInChildren<ChestSpot>(true).Length,
            Shape = m.shape,
            CeilingOverride = m.ceilingOverride,
            Doors = m.Doors(),
        };
    }

    /// <summary>Une salle (ou un carrefour) posée sur la grille.</summary>
    public sealed class PlanRoom
    {
        public string Name;
        /// <summary>Module (indice dans le catalogue) ; −1 : salle de départ (sans module) ; −2 : repaire.</summary>
        public int Module = -1;
        public Vector2Int Cell, Cells = Vector2Int.one;
        public int Rot;
        /// <summary>Centre au sol (y : hauteur du sol).</summary>
        public Vector3 Center;
        /// <summary>Taille (m, x et z dans le monde, rotation comprise).</summary>
        public Vector2 Extent;
        public float Ceiling;
        public bool Hall;
        public ModuleHole Hole;
        public bool Parkour;
        public List<PlanDoor> Doors = new List<PlanDoor>();
    }

    /// <summary>Une place d'entrée d'une salle (monde) : reliée à un couloir, à la sortie, au repaire, ou fermée.</summary>
    public sealed class PlanDoor
    {
        public DoorSide Side;
        public Vector3 Position;
        public bool Required;
        /// <summary>Couloir relié (−1 : aucun).</summary>
        public int Link = -1;
        /// <summary>Usage : 0 fermée, 1 couloir, 2 sortie du niveau, 3 repaire.</summary>
        public int Use;
    }

    /// <summary>Couloir entre deux salles (de la porte de A à celle de B, sols compris).</summary>
    public sealed class PlanLink
    {
        public int A, B, DoorA, DoorB;
        public Vector3 From, To;
        public float Width;
        public float Angle;
        public bool Slope => Mathf.Abs(From.y - To.y) > 0.05f;
        public float Length => Vector2.Distance(new Vector2(From.x, From.z), new Vector2(To.x, To.z));
    }

    /// <summary>Pièce annexe le long d'un couloir plat (côté +1 / −1, à la fraction T), verrouillée ou non.</summary>
    public struct PlanAnnex
    {
        public int Link;
        public int Side;
        public float T;
        public bool Locked;
    }

    /// <summary>Résultat du tirage : de quoi construire la carte, et de quoi la vérifier.</summary>
    public sealed class MapPlan
    {
        public int Seed, Attempt;
        public bool Fallback;
        public float Spacing;
        public Vector3 Origin;
        public int Columns, Rows;
        public readonly List<PlanRoom> Rooms = new List<PlanRoom>();
        public readonly List<PlanLink> Links = new List<PlanLink>();
        public int Start = -1, Exit = -1, ExitDoor = -1, LairHost = -1, LairDoor = -1;
        public readonly List<int> ChestRooms = new List<int>();
        public readonly List<PlanAnnex> Annexes = new List<PlanAnnex>();
        public int StartExitRooms;
        public readonly List<string> Failures = new List<string>();

        public int RoomAt(Vector2Int c, int[,] occupancy) => occupancy[c.x, c.y];

        /// <summary>Distances en salles (nombre de couloirs) depuis une salle.</summary>
        public int[] Distances(int from)
        {
            var d = Enumerable.Repeat(int.MaxValue, Rooms.Count).ToArray();
            var q = new Queue<int>();
            d[from] = 0;
            q.Enqueue(from);
            while (q.Count > 0)
            {
                int a = q.Dequeue();
                foreach (PlanLink l in Links)
                {
                    int b = l.A == a ? l.B : l.B == a ? l.A : -1;
                    if (b >= 0 && d[b] == int.MaxValue)
                    {
                        d[b] = d[a] + 1;
                        q.Enqueue(b);
                    }
                }
            }

            return d;
        }

        /// <summary>Couloirs qui ne sont dans aucune boucle (ponts du graphe, Tarjan). Vide : chaque couloir est dans une boucle.</summary>
        public List<int> Bridges() => MapPlanner.Bridges(Rooms.Count, Links.Select(l => new Vector2Int(l.A, l.B)).ToList());
    }

    /// <summary>
    /// Tirage de la carte aléatoire (étape 8), pur et déterministe : tout vient de System.Random(seed), dans un ordre fixe. Grille de
    /// cases (120 m) ; salles de 1, 2 ou 4 cases (modules du catalogue, validés) ; carrefours ; couloirs entre entrées face à face ;
    /// règles garanties par construction :
    /// - chaque couloir dans une boucle (graphe sans pont : le traqueur peut toujours tourner) ;
    /// - au moins minStartExitRooms salles entre le départ et la sortie ; la sortie est tirée parmi les salles assez éloignées qui
    ///   ont une entrée sur le bord de la carte (l'arène du boss est derrière) ;
    /// - aucune pente de liaison au-dessus de maxSlope ; niveau haut seulement pour les modules qui le déclarent ;
    /// - repaire du traqueur en cul-de-sac sur le bord, loin du départ.
    /// Échec d'un essai (entrée obligatoire non reliée, pont, sortie impossible) : nouvel essai avec une sous-seed ; après
    /// maxAttempts, carte de secours (toutes salles au sol, aucun couloir retiré).
    /// </summary>
    public static class MapPlanner
    {
        private static readonly Vector2Int[] SideDir = { new Vector2Int(0, 1), new Vector2Int(1, 0), new Vector2Int(0, -1), new Vector2Int(-1, 0) };

        public static MapPlan Plan(MapLayoutSettings s, IReadOnlyList<ModuleInfo> modules, int seed)
        {
            MapPlan last = null;
            for (int attempt = 0; attempt < Mathf.Max(1, s.maxAttempts); attempt++)
            {
                int sub = unchecked(seed * 486187739 + attempt * 16777619);
                MapPlan p = TryPlan(s, modules, seed, sub, attempt, false);
                if (p.Failures.Count == 0)
                    return p;
                last = p;
            }

            MapPlan fb = TryPlan(s, modules, seed, unchecked(seed * 31 + 7), s.maxAttempts, true);
            fb.Failures.InsertRange(0, last != null ? last.Failures.Select(f => "(essais) " + f) : new string[0]);
            return fb;
        }

        private static MapPlan TryPlan(MapLayoutSettings s, IReadOnlyList<ModuleInfo> modules, int seed, int sub, int attempt, bool fallback)
        {
            var rng = new System.Random(sub);
            var plan = new MapPlan { Seed = seed, Attempt = attempt, Fallback = fallback, Spacing = s.spacing, Columns = s.columns, Rows = s.rows };
            float sp = s.spacing;
            plan.Origin = new Vector3(-(s.columns - 1) * sp * 0.5f, 0f, -(s.rows - 1) * sp * 0.5f);
            var occ = new int[s.columns, s.rows];
            for (int i = 0; i < s.columns; i++)
            for (int j = 0; j < s.rows; j++)
                occ[i, j] = -1;
            var used = new int[modules.Count];

            // 1. Salle de départ (sans module : marchand), sur une case au hasard.
            var startCell = new Vector2Int(rng.Next(s.columns), rng.Next(s.rows));
            plan.Start = AddRoom(plan, occ, null, startCell, 0, 0f, s, "départ");

            // 2. Grandes salles (plusieurs cases), puis les modules qui ont un minimum par carte.
            if (!fallback)
            {
                var bigs = modules.Where(m => m.Big && m.Kind == ModuleKind.Salle && m.Weight > 0f).ToList();
                for (int k = 0; k < s.bigRooms && bigs.Count > 0; k++)
                {
                    ModuleInfo m = Pick(rng, bigs.Where(x => x.MaxPerMap == 0 || used[x.Index] < x.MaxPerMap).ToList());
                    if (m == null)
                        break;
                    int rot = m.Rotate ? rng.Next(4) : 0;
                    Vector2Int cells = rot % 2 == 1 ? new Vector2Int(m.Footprint.y, m.Footprint.x) : m.Footprint;
                    for (int tries = 0; tries < 40; tries++)
                    {
                        var o = new Vector2Int(rng.Next(s.columns - cells.x + 1), rng.Next(s.rows - cells.y + 1));
                        if (!Free(occ, o, cells) || Manhattan(o, startCell) < m.MinStartDistance)
                            continue;
                        AddRoom(plan, occ, m, o, rot, HeightFor(m, rng, s), s, null);
                        used[m.Index]++;
                        break;
                    }
                }
            }

            var cellsLeft = new List<Vector2Int>();
            for (int j = 0; j < s.rows; j++)
            for (int i = 0; i < s.columns; i++)
                if (occ[i, j] < 0)
                    cellsLeft.Add(new Vector2Int(i, j));
            Shuffle(rng, cellsLeft);

            foreach (ModuleInfo m in modules.Where(x => !x.Big && x.MinPerMap > 0))
                for (int k = used[m.Index]; k < m.MinPerMap; k++)
                {
                    int at = cellsLeft.FindIndex(c => Manhattan(c, startCell) >= m.MinStartDistance);
                    if (at < 0)
                        break;
                    Vector2Int c0 = cellsLeft[at];
                    cellsLeft.RemoveAt(at);
                    AddRoom(plan, occ, m, c0, m.Rotate ? rng.Next(4) : 0, HeightFor(m, rng, s), s, null);
                    used[m.Index]++;
                }

            // 3. Les autres cases : carrefours (part crossingShare) ou salles (canaux, puis parcours, puis les autres, au poids).
            int canals = plan.Rooms.Count(r => r.Hole == ModuleHole.Canal), pits = plan.Rooms.Count(r => r.Parkour);
            foreach (Vector2Int c in cellsLeft)
            {
                bool crossing = !fallback && rng.NextDouble() < s.crossingShare;
                int dist = Manhattan(c, startCell);
                bool Ok(ModuleInfo x) => !x.Big && x.Weight > 0f && (x.MaxPerMap == 0 || used[x.Index] < x.MaxPerMap) && dist >= x.MinStartDistance;
                ModuleInfo m = null;
                if (crossing)
                    m = Pick(rng, modules.Where(x => Ok(x) && x.Kind == ModuleKind.Carrefour).ToList());
                if (m == null)
                {
                    var halls = modules.Where(x => Ok(x) && x.Kind == ModuleKind.Salle).ToList();
                    if (canals < s.canalHalls)
                        m = Pick(rng, halls.Where(x => x.Hole == ModuleHole.Canal).ToList());
                    if (m == null && pits < s.parkourHalls)
                        m = Pick(rng, halls.Where(x => x.Parkour).ToList());
                    if (m == null)
                        m = Pick(rng, halls.Where(x => x.Hole == ModuleHole.Aucun && !x.Parkour).ToList());
                    if (m == null)
                        m = Pick(rng, halls);
                }

                if (m == null)
                {
                    plan.Failures.Add($"aucun module pour la case {CellName(c)}");
                    AddRoom(plan, occ, null, c, 0, 0f, s, "vide");
                    continue;
                }

                if (m.Hole == ModuleHole.Canal) canals++;
                if (m.Parkour) pits++;
                AddRoom(plan, occ, m, c, m.Rotate ? rng.Next(4) : 0, fallback ? 0f : HeightFor(m, rng, s), s, null);
                used[m.Index]++;
            }

            // 4. Couloirs candidats : entre deux salles voisines, par deux entrées face à face, pente plafonnée.
            var candidates = new List<PlanLink>();
            var pairs = new HashSet<(int, int)>();
            float maxTan = Mathf.Tan(s.maxSlope * Mathf.Deg2Rad);
            for (int j = 0; j < s.rows; j++)
            for (int i = 0; i < s.columns; i++)
                for (int side = 0; side < 2; side++)                          // nord et est : chaque paire une fois
                {
                    var c = new Vector2Int(i, j);
                    Vector2Int n = c + SideDir[side];
                    if (n.x >= s.columns || n.y >= s.rows)
                        continue;
                    int a = occ[c.x, c.y], b = occ[n.x, n.y];
                    if (a < 0 || b < 0 || a == b || pairs.Contains((Mathf.Min(a, b), Mathf.Max(a, b))))
                        continue;
                    Vector3 cellA = CellCenter(plan, c);
                    int da = DoorAt(plan.Rooms[a], (DoorSide)side, cellA, side == 0);
                    int db = DoorAt(plan.Rooms[b], (DoorSide)((side + 2) % 4), cellA, side == 0);
                    if (da < 0 || db < 0)
                        continue;
                    PlanDoor pa = plan.Rooms[a].Doors[da], pb = plan.Rooms[b].Doors[db];
                    float len = Vector2.Distance(new Vector2(pa.Position.x, pa.Position.z), new Vector2(pb.Position.x, pb.Position.z));
                    float dy = Mathf.Abs(pa.Position.y - pb.Position.y);
                    if (len < 4f || dy > len * maxTan)
                        continue;                                             // pente trop raide : pas de couloir ici
                    pairs.Add((Mathf.Min(a, b), Mathf.Max(a, b)));
                    candidates.Add(new PlanLink
                    {
                        A = a, B = b, DoorA = da, DoorB = db, From = pa.Position, To = pb.Position,
                        Width = dy > 0.05f ? s.slopeCorridorWidth : s.corridorWidth, Angle = Mathf.Atan2(dy, len) * Mathf.Rad2Deg,
                    });
                }

            // 5. Retrait au hasard en gardant : entrées obligatoires reliées, 2 couloirs par salle, une seule carte, aucun pont.
            var removed = new bool[candidates.Count];
            int toRemove = fallback ? 0 : Mathf.RoundToInt(candidates.Count * s.removedCorridorShare);
            var order = Enumerable.Range(0, candidates.Count).ToList();
            Shuffle(rng, order);
            int count = 0;
            foreach (int k in order)
            {
                if (count >= toRemove)
                    break;
                PlanLink l = candidates[k];
                if (plan.Rooms[l.A].Doors[l.DoorA].Required || plan.Rooms[l.B].Doors[l.DoorB].Required)
                    continue;
                removed[k] = true;
                var kept = Kept(candidates, removed);
                if (Degree(kept, l.A) < 2 || Degree(kept, l.B) < 2 || !Connected(plan.Rooms.Count, kept)
                    || Bridges(plan.Rooms.Count, kept.Select(x => new Vector2Int(x.A, x.B)).ToList()).Count > 0)
                    removed[k] = false;
                else
                    count++;
            }

            foreach (PlanLink l in Kept(candidates, removed))
            {
                int id = plan.Links.Count;
                plan.Links.Add(l);
                plan.Rooms[l.A].Doors[l.DoorA].Link = id;
                plan.Rooms[l.A].Doors[l.DoorA].Use = 1;
                plan.Rooms[l.B].Doors[l.DoorB].Link = id;
                plan.Rooms[l.B].Doors[l.DoorB].Use = 1;
            }

            for (int r = 0; r < plan.Rooms.Count; r++)
                foreach (PlanDoor d in plan.Rooms[r].Doors)
                    if (d.Required && d.Link < 0)
                        plan.Failures.Add($"{plan.Rooms[r].Name} : entrée obligatoire ({d.Side}) sans couloir");
            if (!Connected(plan.Rooms.Count, plan.Links))
                plan.Failures.Add("carte en plusieurs morceaux");
            List<int> bridges = plan.Bridges();
            if (bridges.Count > 0 && !fallback)
                plan.Failures.Add($"{bridges.Count} couloir(s) hors boucle");

            // 6. Sortie : salle assez loin du départ, avec une entrée libre sur le bord de la carte (l'arène est derrière).
            int[] dist0 = plan.Distances(plan.Start);
            var exits = new List<(int room, int door)>();
            for (int r = 0; r < plan.Rooms.Count; r++)
            {
                PlanRoom room = plan.Rooms[r];
                if (r == plan.Start || !room.Hall || room.Hole != ModuleHole.Aucun || room.Parkour || dist0[r] == int.MaxValue
                    || dist0[r] < s.minStartExitRooms || (room.Module >= 0 && !modules[room.Module].ExitAllowed))
                    continue;
                for (int d = 0; d < room.Doors.Count; d++)
                    if (room.Doors[d].Link < 0 && OnBorder(plan, room.Doors[d]) && Mathf.Abs(room.Doors[d].Position.y) < 0.05f)
                        exits.Add((r, d));
            }

            if (exits.Count == 0)
            {
                int best = Enumerable.Range(0, plan.Rooms.Count).Where(r => dist0[r] != int.MaxValue).DefaultIfEmpty(0).Max(r => dist0[r]);
                plan.Failures.Add($"aucune sortie possible à {s.minStartExitRooms} salles du départ (au plus {best})");
            }
            else
            {
                (int room, int door) = exits[rng.Next(exits.Count)];
                plan.Exit = room;
                plan.ExitDoor = door;
                plan.Rooms[room].Doors[door].Use = 2;
                plan.StartExitRooms = dist0[room];
            }

            // 7. Repaire du traqueur : cul-de-sac sur le bord, le plus loin possible du départ (hors sortie).
            var lairs = new List<(int room, int door)>();
            int farthest = 0;
            for (int r = 0; r < plan.Rooms.Count; r++)
            {
                if (r == plan.Start || r == plan.Exit || dist0[r] == int.MaxValue || plan.Rooms[r].Hole != ModuleHole.Aucun)
                    continue;
                for (int d = 0; d < plan.Rooms[r].Doors.Count; d++)
                {
                    PlanDoor door = plan.Rooms[r].Doors[d];
                    if (door.Use == 0 && OnBorder(plan, door))
                    {
                        if (dist0[r] > farthest)
                        {
                            farthest = dist0[r];
                            lairs.Clear();
                        }

                        if (dist0[r] == farthest)
                            lairs.Add((r, d));
                    }
                }
            }

            if (lairs.Count == 0)
                plan.Failures.Add("aucune place pour le repaire du traqueur");
            else
            {
                (int room, int door) = lairs[rng.Next(lairs.Count)];
                plan.LairHost = room;
                plan.LairDoor = door;
                plan.Rooms[room].Doors[door].Use = 3;
            }

            // 8. Coffres : salles à emplacement de coffre, ni départ ni sortie.
            var chestRooms = Enumerable.Range(0, plan.Rooms.Count)
                .Where(r => r != plan.Start && r != plan.Exit && plan.Rooms[r].Module >= 0 && modules[plan.Rooms[r].Module].ChestSpots > 0).ToList();
            Shuffle(rng, chestRooms);
            plan.ChestRooms.AddRange(chestRooms.Take(s.chests));

            // 9. Pièces annexes le long des couloirs plats de 40 m et plus (les premières : salles verrouillées).
            var flat = Enumerable.Range(0, plan.Links.Count).Where(i => !plan.Links[i].Slope && plan.Links[i].Length >= 40f).ToList();
            Shuffle(rng, flat);
            foreach (int i in flat)
            {
                if (rng.NextDouble() > s.annexShare)
                    continue;
                plan.Annexes.Add(new PlanAnnex
                {
                    Link = i, Side = rng.NextDouble() < 0.5 ? 1 : -1, T = rng.NextDouble() < 0.5 ? 0.3f : 0.7f,
                    Locked = plan.Annexes.Count(a => a.Locked) < s.lockedRooms,
                });
            }

            return plan;
        }

        // ---------- Pièces du tirage ----------

        private static float HeightFor(ModuleInfo m, System.Random rng, MapLayoutSettings s)
        {
            if (m == null || m.Level == ModuleLevel.Sol)
                return 0f;
            if (m.Level == ModuleLevel.Haut)
                return s.upperLevel;
            return rng.NextDouble() < s.upperShareForBoth ? s.upperLevel : 0f;
        }

        private static int AddRoom(MapPlan plan, int[,] occ, ModuleInfo m, Vector2Int origin, int rot, float height, MapLayoutSettings s, string special)
        {
            Vector2Int fp = m != null ? m.Footprint : Vector2Int.one;
            Vector2Int cells = rot % 2 == 1 ? new Vector2Int(fp.y, fp.x) : fp;
            float size = m != null ? m.Size : s.startHallSize;
            var localExtent = new Vector2((fp.x - 1) * s.spacing + size, (fp.y - 1) * s.spacing + size);
            Vector2 extent = rot % 2 == 1 ? new Vector2(localExtent.y, localExtent.x) : localExtent;
            Vector3 c = plan.Origin + new Vector3((origin.x + (cells.x - 1) * 0.5f) * s.spacing, height, (origin.y + (cells.y - 1) * 0.5f) * s.spacing);
            bool hall = m == null || m.Kind == ModuleKind.Salle;
            var room = new PlanRoom
            {
                Name = CellName(origin) + (special == "départ" ? " (départ)" : m != null && m.Kind == ModuleKind.Carrefour ? " (carrefour)" : m != null && m.Big ? $" ({cells.x}×{cells.y})" : ""),
                Module = m != null ? m.Index : -1, Cell = origin, Cells = cells, Rot = rot, Center = c, Extent = extent,
                Ceiling = m != null ? s.Ceiling(m.Kind, m.Shape, m.CeilingOverride) : s.ceilingSimple, Hall = hall, Hole = m != null ? m.Hole : ModuleHole.Aucun,
                Parkour = m != null && m.Parkour,
            };
            List<(DoorSide side, int cell, bool required)> doors = m != null ? m.Doors : AllDoors(fp);
            foreach ((DoorSide side, int cell, bool required) in doors)
            {
                Vector3 local = DoorLocal(localExtent, fp, side, cell, s.spacing);
                room.Doors.Add(new PlanDoor { Side = (DoorSide)(((int)side + rot) % 4), Position = c + Rotate(local, rot), Required = required });
            }

            int id = plan.Rooms.Count;
            plan.Rooms.Add(room);
            for (int i = 0; i < cells.x; i++)
            for (int j = 0; j < cells.y; j++)
                occ[origin.x + i, origin.y + j] = id;
            return id;
        }

        private static List<(DoorSide, int, bool)> AllDoors(Vector2Int fp)
        {
            var list = new List<(DoorSide, int, bool)>();
            foreach (DoorSide s in new[] { DoorSide.Nord, DoorSide.Est, DoorSide.Sud, DoorSide.Ouest })
                for (int c = 0; c < (s == DoorSide.Nord || s == DoorSide.Sud ? fp.x : fp.y); c++)
                    list.Add((s, c, false));
            return list;
        }

        public static Vector3 DoorLocal(Vector2 extent, Vector2Int fp, DoorSide side, int cell, float spacing)
        {
            Vector2 e = extent * 0.5f;
            int n = side == DoorSide.Nord || side == DoorSide.Sud ? fp.x : fp.y;
            float along = (cell - (n - 1) * 0.5f) * spacing;
            return side switch
            {
                DoorSide.Nord => new Vector3(along, 0f, e.y),
                DoorSide.Sud => new Vector3(along, 0f, -e.y),
                DoorSide.Est => new Vector3(e.x, 0f, along),
                _ => new Vector3(-e.x, 0f, along),
            };
        }

        /// <summary>Rotation de k quarts de tour (lacet +90° : nord → est), comme Quaternion.Euler(0, 90k, 0).</summary>
        public static Vector3 Rotate(Vector3 v, int k)
        {
            float x = v.x, z = v.z;
            for (int i = 0; i < ((k % 4) + 4) % 4; i++)
                (x, z) = (z, -x);
            return new Vector3(x, v.y, z);
        }

        public static Vector3 SideVector(DoorSide s) => RoomModule.SideVector(s);

        private static Vector3 CellCenter(MapPlan p, Vector2Int c) => p.Origin + new Vector3(c.x * p.Spacing, 0f, c.y * p.Spacing);

        /// <summary>Entrée de la salle sur ce côté, dans l'axe de la case (x pour nord / sud, z pour est / ouest).</summary>
        private static int DoorAt(PlanRoom r, DoorSide side, Vector3 cellCenter, bool alongX)
        {
            for (int d = 0; d < r.Doors.Count; d++)
            {
                PlanDoor door = r.Doors[d];
                if (door.Side != side)
                    continue;
                float a = alongX ? door.Position.x : door.Position.z, b = alongX ? cellCenter.x : cellCenter.z;
                if (Mathf.Abs(a - b) < 1f)
                    return d;
            }

            return -1;
        }

        /// <summary>L'entrée donne-t-elle sur l'extérieur de la grille ?</summary>
        private static bool OnBorder(MapPlan p, PlanDoor d)
        {
            Vector3 out1 = d.Position + SideVector(d.Side) * (p.Spacing * 0.5f);
            Vector3 rel = out1 - p.Origin;
            int i = Mathf.RoundToInt(rel.x / p.Spacing), j = Mathf.RoundToInt(rel.z / p.Spacing);
            return i < 0 || j < 0 || i >= p.Columns || j >= p.Rows;
        }

        private static bool Free(int[,] occ, Vector2Int o, Vector2Int cells)
        {
            for (int i = 0; i < cells.x; i++)
            for (int j = 0; j < cells.y; j++)
                if (occ[o.x + i, o.y + j] >= 0)
                    return false;
            return true;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        public static string CellName(Vector2Int c) => $"{(char)('A' + c.x)}{c.y + 1}";

        private static ModuleInfo Pick(System.Random rng, List<ModuleInfo> pool)
        {
            float sum = pool.Sum(m => m.Weight);
            if (pool.Count == 0 || sum <= 0f)
                return null;
            double r = rng.NextDouble() * sum;
            foreach (ModuleInfo m in pool)
            {
                r -= m.Weight;
                if (r <= 0)
                    return m;
            }

            return pool[pool.Count - 1];
        }

        private static void Shuffle<T>(System.Random rng, List<T> list)
        {
            for (int k = list.Count - 1; k > 0; k--)
            {
                int r = rng.Next(k + 1);
                (list[k], list[r]) = (list[r], list[k]);
            }
        }

        private static List<PlanLink> Kept(List<PlanLink> all, bool[] removed)
        {
            var list = new List<PlanLink>();
            for (int k = 0; k < all.Count; k++)
                if (!removed[k])
                    list.Add(all[k]);
            return list;
        }

        private static int Degree(List<PlanLink> links, int node) => links.Count(l => l.A == node || l.B == node);

        private static bool Connected(int n, List<PlanLink> links)
        {
            if (n == 0)
                return true;
            var seen = new bool[n];
            var stack = new Stack<int>();
            stack.Push(0);
            seen[0] = true;
            int count = 1;
            while (stack.Count > 0)
            {
                int a = stack.Pop();
                foreach (PlanLink l in links)
                {
                    int b = l.A == a ? l.B : l.B == a ? l.A : -1;
                    if (b >= 0 && !seen[b])
                    {
                        seen[b] = true;
                        count++;
                        stack.Push(b);
                    }
                }
            }

            return count == n;
        }

        /// <summary>Ponts du graphe (arêtes hors de toute boucle), Tarjan itératif ; arêtes multiples comprises.</summary>
        public static List<int> Bridges(int n, List<Vector2Int> edges)
        {
            var adj = new List<(int to, int e)>[n];
            for (int i = 0; i < n; i++)
                adj[i] = new List<(int, int)>();
            for (int e = 0; e < edges.Count; e++)
            {
                adj[edges[e].x].Add((edges[e].y, e));
                adj[edges[e].y].Add((edges[e].x, e));
            }

            var disc = Enumerable.Repeat(-1, n).ToArray();
            var low = new int[n];
            var result = new List<int>();
            int time = 0;
            for (int root = 0; root < n; root++)
            {
                if (disc[root] >= 0)
                    continue;
                var stack = new Stack<(int node, int parentEdge, int next)>();
                stack.Push((root, -1, 0));
                disc[root] = low[root] = time++;
                while (stack.Count > 0)
                {
                    (int u, int pe, int next) = stack.Pop();
                    if (next < adj[u].Count)
                    {
                        stack.Push((u, pe, next + 1));
                        (int v, int e) = adj[u][next];
                        if (e == pe)
                            continue;
                        if (disc[v] < 0)
                        {
                            disc[v] = low[v] = time++;
                            stack.Push((v, e, 0));
                        }
                        else
                            low[u] = Mathf.Min(low[u], disc[v]);
                    }
                    else if (stack.Count > 0)
                    {
                        (int p, _, _) = stack.Peek();
                        low[p] = Mathf.Min(low[p], low[u]);
                        if (low[u] > disc[p])
                            result.Add(pe);
                    }
                }
            }

            return result;
        }
    }
}
