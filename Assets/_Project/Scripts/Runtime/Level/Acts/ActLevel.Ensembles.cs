using System.Collections.Generic;
using System.Linq;
using ItHunts.Enemies;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Pose des ensembles de décor (D-118) : d'abord le long des murs, puis au sol jusqu'à ce que plus rien n'entre. Le sol « passage » est
    /// utilisé aussi (il couvre presque toutes les salles : c'est lui qui les laissait vides) : chaque ensemble garde une allée libre tout
    /// autour, donc le sol reste d'un tenant ; les abords des portes, des rampes et des paliers restent vides.
    /// </summary>
    public sealed partial class ActLevel
    {
        private static readonly string[] ExampleSkip =
        {
            "Bloc_", "_Mur_", "Paroi", "Fenetre", "Voute", "Arcade", "Rosace", "Rayon", "Porte", "Chandelier", "Tenture", "Tableau",
            "Torche_Murale", "Applique", "Lisiere", "Falaise", "MurEnceinte", "Ciel", "Canal", "Bassin", "Gouffre", "GrandeEntree", "Galerie",
            "GrandEscalier", "EscalierDescente", "Brume", "Arche", "Pont", "Chemin", "Rebord", "Corniche", "Chaines", "Cellule",
        };

        private static readonly string[] MuralNames = { "Tableau", "Applique", "Torche_Murale", "Chaines_Mur", "Tenture", "Tapisserie" };
        private static readonly string[] Routes = { "Passerelle", "Plateforme", "Mezzanine", "Estrade", "Escalier", "Tapis", "Rails" };
        private static readonly string[] PlantNames = { "Massif", "Haie", "SousBois", "ArbreMort", "GrandArbre" };
        private int _group;
        /// <summary>Cases à laisser libres (haut des marches des bassins).</summary>
        private readonly HashSet<Vector2Int> _keepClear = new HashSet<Vector2Int>();

        /// <summary>Ensembles posés dans le niveau (mesure).</summary>
        public int EnsembleCount { get; private set; }

        private bool HasEnsembles(ActRoom r) =>
            Plan.Hand != null && settings.ensembles != null && settings.ensembles.Sets.Any(s => !s.Any && s.Fits(r.Function));

        private HashSet<Vector2Int> EnsembleGround(ActRoom r)
        {
            float door = settings.ensembles.doorClear;
            var ground = new HashSet<Vector2Int>();
            foreach (KeyValuePair<Vector2Int, CellUse> kv in r.Use)
            {
                if (kv.Value != CellUse.Sol && kv.Value != CellUse.Passage)
                    continue;
                Vector2Int c = kv.Key;
                bool ok = true;
                // Abords des rampes, des ponts, et des paliers le long d'un passage (escaliers, wall jumps) : libres.
                for (int dx = -2; dx <= 2 && ok; dx++)
                for (int dy = -2; dy <= 2 && ok; dy++)
                    if (r.Use.TryGetValue(c + new Vector2Int(dx, dy), out CellUse u))
                        ok = u != CellUse.Rampe && u != CellUse.Pont && !(u == CellUse.Terrasse && kv.Value == CellUse.Passage);
                if (ok && !_keepClear.Contains(c) && !NearDoor(r, Plan.CellCenter(c, r.Floor), door))
                    ground.Add(c);
            }

            // Départ du joueur : 5 m libres.
            if (r.Role == RoomRole.Depart)
            {
                var at = Vector2Int.RoundToInt(r.CenterCell);
                ground.RemoveWhere(c => Mathf.Abs(c.x - at.x) <= 2 && Mathf.Abs(c.y - at.y) <= 2);
            }

            return ground;
        }

        private void Ensembles(ActRoom r, Transform root, List<ActContour.Segment> segs)
        {
            EnsembleLibrary lib = settings.ensembles;
            HashSet<Vector2Int> ground = EnsembleGround(r);
            var blocked = new HashSet<Vector2Int>();
            foreach (Vector2Int c in r.Use.Keys)
                if (Occupied(c))
                    blocked.Add(c);
            WallEnsembles(r, root, segs, lib, ground, blocked);
            FloorEnsembles(r, root, lib, ground, blocked, lib.Sets.Where(s => !s.Wall && !s.Ground && !s.Any && s.Fits(r.Function)).ToList());
            FloorEnsembles(r, root, lib, ground, blocked, lib.Sets.Where(s => !s.Wall && !s.Ground && s.Any).ToList());
            GroundDetails(r, root, lib, blocked);
            foreach (Vector2Int c in blocked)
                _occupied.Add(c);
        }

        private void WallEnsembles(ActRoom r, Transform root, List<ActContour.Segment> segs, EnsembleLibrary lib, HashSet<Vector2Int> ground, HashSet<Vector2Int> blocked)
        {
            List<EnsembleLibrary.Set> sets = lib.Sets.Where(s => s.Wall && s.Fits(r.Function)).ToList();
            if (sets.Count == 0)
                return;
            var used = new Dictionary<EnsembleLibrary.Set, int>();
            foreach (ActContour.Segment s in segs)
            {
                if (s.Diagonal)
                    continue;
                Vector3 a = World(s.A, r.Floor), b = World(s.B, r.Floor);
                float len = Vector3.Distance(a, b);
                Vector3 along = (b - a).normalized, inward = -new Vector3(s.Out.x, 0f, s.Out.y).normalized;
                float t = 1f + (float)_rng.NextDouble() * 2f;
                while (t < len - 2f)
                {
                    bool placed = false;
                    foreach (EnsembleLibrary.Set set in sets.OrderBy(s0 => s0.Max == 1 ? 0 : 1).ThenBy(_ => _rng.Next()))
                    {
                        used.TryGetValue(set, out int n);
                        if (n >= set.Max || t + set.Width > len - 0.5f)
                            continue;
                        Vector3 mid = a + along * (t + set.Width * 0.5f);
                        var cells = new HashSet<Vector2Int>();
                        bool ok = true;
                        for (float x = -set.Width * 0.5f + 0.5f; x <= set.Width * 0.5f && ok; x += 1f)
                        for (float z = 0.8f; z <= set.Depth && ok; z += 1f)
                        {
                            Vector2Int c = Plan.CellOf(mid + along * x + inward * z);
                            ok = ground.Contains(c) && !blocked.Contains(c);
                            cells.Add(c);
                        }

                        if (!ok)
                            continue;
                        Stamp(set, root, mid, inward);
                        blocked.UnionWith(cells);
                        used[set] = n + 1;
                        t += set.Width;
                        placed = true;
                        break;
                    }

                    t += placed ? Mathf.Lerp(lib.wallGap.x, lib.wallGap.y, (float)_rng.NextDouble()) : 2f;
                }
            }
        }

        private void FloorEnsembles(ActRoom r, Transform root, EnsembleLibrary lib, HashSet<Vector2Int> ground, HashSet<Vector2Int> blocked, List<EnsembleLibrary.Set> sets)
        {
            if (sets.Count == 0)
                return;
            List<Vector2Int> order = ground.OrderBy(_ => _rng.Next()).ToList();
            FillGrid(r, ground, blocked);
            // Hauts d'abord (il leur faut de la place pour le traqueur), puis grands (salles d'exemple, estrades), puis tour à tour.
            var live = sets.OrderByDescending(s => s.Max == 1).ThenByDescending(s => lib.Tall(s)).ThenByDescending(s => s.Width * s.Depth).ToList();
            var used = new Dictionary<EnsembleLibrary.Set, int>();
            var cursor = new Dictionary<EnsembleLibrary.Set, int>();
            while (live.Count > 0)
                foreach (EnsembleLibrary.Set set in live.ToList())
                {
                    used.TryGetValue(set, out int n);
                    cursor.TryGetValue(set, out int from);
                    bool example = set.Pieces.Count > 0 && set.Pieces[0].Example;
                    bool tall = lib.Tall(set);
                    int ring = Mathf.Max(1, Mathf.RoundToInt((tall ? lib.tallClear : lib.aisle) / Plan.Cell));
                    int at = -1, turn = 0;
                    int first = _rng.Next(2);
                    for (int i = from; i < order.Count && n < set.Max && at < 0; i++)
                        for (int k = 0; k < 2 && at < 0; k++)
                            if (SetFits(order[i], set, (first + k) % 2, ring, example) && !(tall && NearTall(r, order[i], set, (first + k) % 2, ring)))
                            {
                                at = i;
                                turn = (first + k) % 2;
                            }

                    if (at < 0)
                    {
                        live.Remove(set);
                        continue;
                    }

                    cursor[set] = at + 1;
                    used[set] = n + 1;
                    int w = Cells(turn == 0 ? set.Width : set.Depth), d = Cells(turn == 0 ? set.Depth : set.Width);
                    Vector3 center = Plan.Origin + new Vector3((order[at].x + w * 0.5f) * Plan.Cell, r.Floor, (order[at].y + d * 0.5f) * Plan.Cell);
                    Vector3 fwd = turn == 0 ? (_rng.Next(2) == 0 ? Vector3.forward : Vector3.back) : (_rng.Next(2) == 0 ? Vector3.right : Vector3.left);
                    if (example)
                    {
                        StampExample(set, root, center, fwd, ground, blocked);
                        FillGrid(r, ground, blocked);
                    }
                    else
                    {
                        Stamp(set, root, center, fwd);
                        for (int x = 0; x < w; x++)
                        for (int y = 0; y < d; y++)
                        {
                            blocked.Add(order[at] + new Vector2Int(x, y));
                            GridBlock(order[at] + new Vector2Int(x, y));
                            if (tall)
                                _tallCells.Add(order[at] + new Vector2Int(x, y));
                        }
                    }
                }
        }

        /// <summary>Cases des ensembles hauts : pas d'autre ensemble haut à moins de tallClear.</summary>
        private readonly HashSet<Vector2Int> _tallCells = new HashSet<Vector2Int>();

        private bool NearTall(ActRoom r, Vector2Int c, EnsembleLibrary.Set set, int turn, int ring)
        {
            int w = Cells(turn == 0 ? set.Width : set.Depth), d = Cells(turn == 0 ? set.Depth : set.Width);
            for (int x = -ring; x < w + ring; x++)
            for (int y = -ring; y < d + ring; y++)
            {
                // Ni contre une galerie : le traqueur y monte et y tend le bras d'en bas.
                Vector2Int k = c + new Vector2Int(x, y);
                if (_tallCells.Contains(k) || (r.Use.TryGetValue(k, out CellUse u) && (u == CellUse.Terrasse || u == CellUse.Rampe))
                    || (Plan.OwnerAt(k) >= 0 && Plan.OwnerAt(k) != r.Id))
                    return true;
            }

            return false;
        }

        private bool Gallery(Vector2Int c) =>
            Plan.OwnerAt(c) >= 0 && Plan.Rooms[Plan.OwnerAt(c)].Use.TryGetValue(c, out CellUse u) && (u == CellUse.Terrasse || u == CellUse.Rampe);

        /// <summary>Détails au sol sans collision : le sol se lit sans gêner la course.</summary>
        private void GroundDetails(ActRoom r, Transform root, EnsembleLibrary lib, HashSet<Vector2Int> blocked)
        {
            List<EnsembleLibrary.Set> sets = lib.Sets.Where(s => s.Ground && s.Fits(r.Function)).ToList();
            if (sets.Count == 0)
                return;
            List<Vector2Int> order = r.Use.Where(kv => (kv.Value == CellUse.Sol || kv.Value == CellUse.Passage) && !blocked.Contains(kv.Key))
                .Select(kv => kv.Key).OrderBy(_ => _rng.Next()).ToList();
            int want = Mathf.RoundToInt(order.Count * Plan.Cell * Plan.Cell / lib.groundDetailArea), done = 0;
            var taken = new HashSet<Vector2Int>();
            foreach (Vector2Int c in order)
            {
                if (done >= want)
                    break;
                EnsembleLibrary.Set set = sets[_rng.Next(sets.Count)];
                int w = Cells(set.Width * 0.5f), d = Cells(set.Depth * 0.5f);
                bool ok = true;
                for (int x = -w; x <= w && ok; x++)
                for (int y = -d; y <= d && ok; y++)
                {
                    Vector2Int k = c + new Vector2Int(x, y);
                    ok = !blocked.Contains(k) && !taken.Contains(k) && r.Use.TryGetValue(k, out CellUse u) && (u == CellUse.Sol || u == CellUse.Passage);
                }

                if (!ok)
                    continue;
                for (int x = -w; x <= w; x++)
                for (int y = -d; y <= d; y++)
                    taken.Add(c + new Vector2Int(x, y));
                Quaternion rot = Quaternion.Euler(0f, set.Width > 3.5f ? _rng.Next(4) * 90f : _rng.Next(360), 0f);
                int group = ++_group;
                foreach (EnsembleLibrary.Piece p in set.Pieces)
                {
                    GameObject go = Place(p.Name, root, Plan.CellCenter(c, r.Floor) + rot * new Vector3(p.X, p.Y, p.Z), rot * Quaternion.Euler(0f, p.Yaw, 0f), p.Scale, group);
                    if (go == null)
                        continue;
                    go.GetComponent<DecorTag>().kind = DecorKind.Vegetal;
                    foreach (Collider col in go.GetComponentsInChildren<Collider>())
                        DestroyImmediate(col);
                }

                done++;
            }
        }

        private int Cells(float m) => Mathf.Max(1, Mathf.CeilToInt(m / Plan.Cell - 0.05f));

        private bool SetFits(Vector2Int c, EnsembleLibrary.Set set, int turn, int ring, bool example)
        {
            int w = Cells(turn == 0 ? set.Width : set.Depth), d = Cells(turn == 0 ? set.Depth : set.Width);
            if (example)
            {
                // Salle d'exemple : chaque pièce vérifie sa place ; il suffit que 70 % de l'emprise soit libre.
                int free = 0;
                for (int x = 0; x < w; x++)
                for (int y = 0; y < d; y++)
                    if ((GridAt(c.x + x, c.y + y) & (InGround | IsBlocked)) == InGround)
                        free++;
                return free >= w * d * 0.7f;
            }

            for (int x = -ring; x < w + ring; x++)
            for (int y = -ring; y < d + ring; y++)
            {
                byte g = GridAt(c.x + x, c.y + y);
                bool inside = x >= 0 && y >= 0 && x < w && y < d;
                if ((g & IsBlocked) != 0 || (g & (inside ? InGround : InRoom)) == 0)
                    return false;
            }

            return true;
        }

        // État des cases de la salle en cours (recherche de place rapide).
        private const byte InRoom = 1, InGround = 2, IsBlocked = 4;
        private byte[] _grid = new byte[0];
        private RectInt _gridBox;

        private void FillGrid(ActRoom r, HashSet<Vector2Int> ground, HashSet<Vector2Int> blocked)
        {
            int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
            foreach (Vector2Int c in r.Use.Keys)
            {
                x0 = Mathf.Min(x0, c.x);
                y0 = Mathf.Min(y0, c.y);
                x1 = Mathf.Max(x1, c.x);
                y1 = Mathf.Max(y1, c.y);
            }

            _gridBox = new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            int n = _gridBox.width * _gridBox.height;
            if (_grid.Length < n)
                _grid = new byte[n];
            System.Array.Clear(_grid, 0, _grid.Length);
            foreach (Vector2Int c in r.Use.Keys)
                _grid[(c.y - y0) * _gridBox.width + c.x - x0] = (byte)(InRoom | (ground.Contains(c) ? InGround : 0) | (blocked.Contains(c) ? IsBlocked : 0));
        }

        private byte GridAt(int x, int y)
        {
            x -= _gridBox.x;
            y -= _gridBox.y;
            return x < 0 || y < 0 || x >= _gridBox.width || y >= _gridBox.height ? (byte)0 : _grid[y * _gridBox.width + x];
        }

        private void GridBlock(Vector2Int c)
        {
            int x = c.x - _gridBox.x, y = c.y - _gridBox.y;
            if (x >= 0 && y >= 0 && x < _gridBox.width && y < _gridBox.height)
                _grid[y * _gridBox.width + x] |= IsBlocked;
        }

        private void Stamp(EnsembleLibrary.Set set, Transform root, Vector3 at, Vector3 forward)
        {
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            int group = ++_group;
            foreach (EnsembleLibrary.Piece p in set.Pieces)
            {
                GameObject go = Place(p.Name, root, at + rot * new Vector3(p.X, p.Y, p.Z), rot * Quaternion.Euler(0f, p.Yaw, 0f), p.Scale, group);
                if (go != null && p.Name.Contains("PlateformeTraction"))
                    SupportPlatform(go, at.y, group);
            }

            EnsembleCount++;
        }

        /// <summary>Bibliothèques dont le dos nu se voyait : doublées dos à dos, ou retirées faute de place (mesure).</summary>
        public int ShelfBacksFixed { get; private set; }

        /// <summary>Bibliothèques doublées dos à dos (mesure, nuit du 27/11).</summary>
        public int ShelfTwins { get; private set; }

        /// <summary>Bibliothèques retirées (pas de place pour un double derrière).</summary>
        public int ShelfRemoved { get; private set; }

        /// <summary>
        /// Seconde revue de la DA (D-120) : une bibliothèque (ou une étagère) posée au milieu d'une salle montrait son dos nu, un grand
        /// panneau de bois sans livres (rangées de la recette, pièce isolée d'une salle d'exemple, double retiré par l'audit). Après
        /// l'audit, chaque meuble dont le dos n'est ni contre un mur ni contre le dos d'un autre reçoit un double dos à dos ; sans place
        /// derrière, il est retiré.
        /// </summary>
        private void FixShelfBacks()
        {
            ShelfBacksFixed = ShelfTwins = ShelfRemoved = 0;
            Physics.SyncTransforms();
            List<DecorTag> shelves = _root.GetComponentsInChildren<DecorTag>(false)
                .Where(t => t.gameObject.activeInHierarchy && IsShelf(t.name)).ToList();
            foreach (DecorTag t in shelves)
            {
                if (t == null || !t.gameObject.activeInHierarchy || !DecorAudit.Box(t.gameObject, out Bounds b))
                    continue;
                Vector3 f = t.transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude < 1e-4f)
                    continue;
                f.Normalize();
                Vector3 p = t.transform.position;
                float height = b.max.y - p.y;
                if (height < 2f)
                    continue;                                           // meuble bas : son dos ne fait pas un pan
                if (BackCovered(t, p + Vector3.up * Mathf.Min(1.5f, height * 0.5f), f))
                    continue;
                float halfW = Mathf.Max(0.4f, Mathf.Abs(Vector3.Dot(b.extents, new Vector3(Mathf.Abs(f.z), 0f, Mathf.Abs(f.x)))) - 0.1f);
                float depth = Mathf.Max(0.3f, Mathf.Abs(Vector3.Dot(b.size, new Vector3(Mathf.Abs(f.x), 0f, Mathf.Abs(f.z)))));
                // Nuit du 27/11 (relecture) : le dos est pris sur la boîte du meuble, pas sur son pivot (pivot au dos ou au centre selon
                // le modèle). Le double tourne d'un demi-tour autour de ce dos : dos à dos, quel que soit le pivot.
                float back = Vector3.Dot(b.center - p, -f) + depth * 0.5f;
                Vector3 backPoint = p - f * back;
                Vector3 behind = backPoint - f * (depth * 0.5f + 0.08f) + Vector3.up * (height * 0.5f + 0.15f);
                bool free = true;
                foreach (Collider c in Physics.OverlapBox(behind, new Vector3(halfW, height * 0.5f - 0.1f, depth * 0.5f - 0.05f), Quaternion.LookRotation(f), ~0,
                             QueryTriggerInteraction.Ignore))
                    if (!c.transform.IsChildOf(t.transform))
                    {
                        free = false;
                        break;
                    }

                if (free)
                {
                    GameObject twin = Instantiate(t.gameObject, p - f * (2f * back), Quaternion.Euler(0f, 180f, 0f) * t.transform.rotation, t.transform.parent);
                    twin.name = t.name;
                    Physics.SyncTransforms();
                    ShelfTwins++;
                }
                else
                {
                    t.gameObject.SetActive(false);
                    Destroy(t.gameObject);
                    ShelfRemoved++;
                }

                ShelfBacksFixed++;
            }
        }

        private static bool IsShelf(string name) => name == "Chateau_Bibliotheque" || name == "Prop_Etagere" || name == "Cave_Etagere";

        /// <summary>Dos du meuble contre un mur, un pilier ou le dos d'un autre meuble (à moins de 1.6 m).</summary>
        private static bool BackCovered(DecorTag self, Vector3 from, Vector3 forward)
        {
            foreach (RaycastHit h in Physics.RaycastAll(from + forward * 0.3f, -forward, 1.9f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(self.transform))
                    continue;
                DecorTag other = h.collider.GetComponentInParent<DecorTag>();
                if (other == null)
                    return true;                                         // mur, pilier, sol du niveau
                if (IsShelf(other.name) && Vector3.Dot(other.transform.forward, forward) < -0.5f)
                    return true;                                         // dos à dos
            }

            return false;
        }

        /// <summary>Plateformes de traction posées sur une colonne (mesure).</summary>
        public int PlatformsOnColumns { get; private set; }

        /// <summary>
        /// Seconde revue de la DA (D-120) : la plateforme de traction de la DA pend sous un plafond par 4 chaînes de 4 m ; posée sous un
        /// plafond de 16 à 30 m, ses chaînes finissaient dans le vide et se lisaient comme des poteaux noirs. Les chaînes sont retirées et la
        /// plaque (aimantée) repose sur une colonne de pierre jusqu'au sol, au niveau de la passerelle qu'elle porte.
        /// </summary>
        private void SupportPlatform(GameObject platform, float floor, int group)
        {
            Transform plate = platform.transform.Find("Plaque_Magnetique");
            if (plate == null || !plate.TryGetComponent(out MeshFilter pf) || pf.sharedMesh == null)
                return;
            if (platform.TryGetComponent(out MeshRenderer chains))
                chains.enabled = false;                                   // chaînes et accroches : maillage du parent
            foreach (Collider c in platform.GetComponents<Collider>())
                DestroyImmediate(c);
            if (plate.GetComponent<Collider>() == null)
            {
                var box = plate.gameObject.AddComponent<BoxCollider>();
                box.center = pf.sharedMesh.bounds.center;
                box.size = pf.sharedMesh.bounds.size;
            }

            // Dessous de la plaque (le niveau est encore inactif : boîte du maillage, pas celle du rendu).
            Bounds pb = pf.sharedMesh.bounds;
            float under = float.MaxValue;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = pb.center + Vector3.Scale(pb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                under = Mathf.Min(under, plate.TransformPoint(corner).y);
            }

            float h = under - floor;
            if (h < 0.5f)
                return;
            Vector3 foot = new Vector3(platform.transform.position.x, floor, platform.transform.position.z);
            string colName = settings.theme == ActTheme.Caves ? "Cave_PilierRoche_18m" : "Chateau_Colonne_12m";
            GameObject prefab = settings.ensembles != null ? settings.ensembles.Find(colName) : null;
            if (prefab == null && Art != null)
                prefab = Art.Piece(colName);
            GameObject column;
            if (prefab != null)
            {
                float nativeH = settings.theme == ActTheme.Caves ? 18.98f : 12f, width = settings.theme == ActTheme.Caves ? 0.3f : 0.95f;
                column = Instantiate(prefab, foot, platform.transform.rotation, platform.transform.parent);
                column.transform.localScale = new Vector3(width, h / nativeH, width);   // sa collision (maillage) suit l'échelle
            }
            else
            {
                column = new GameObject("Kit_Colonne de la plateforme");
                column.transform.SetParent(platform.transform.parent, false);
                column.transform.SetPositionAndRotation(foot + Vector3.up * (h * 0.5f), platform.transform.rotation);
                Visual(column.transform, "Fût", CylinderMesh, settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial,
                    column.transform.position, column.transform.rotation, new Vector3(1.5f, h * 0.5f, 1.5f), merge: false);
                var col = column.AddComponent<CapsuleCollider>();
                col.radius = 0.75f;
                col.height = h;
            }

            column.name = "Colonne de la plateforme";
            column.AddComponent<NotWalkable>();
            column.AddComponent<StalkerPassesThrough>();
            Tag(column, DecorKind.Prop).GetComponent<DecorTag>().group = group;
            PlatformsOnColumns++;
        }

        private void StampExample(EnsembleLibrary.Set set, Transform root, Vector3 at, Vector3 forward, HashSet<Vector2Int> ground, HashSet<Vector2Int> blocked)
        {
            Quaternion rot = Quaternion.LookRotation(forward, Vector3.up);
            GameObject src = settings.ensembles.Find(set.Pieces[0].Name);
            if (src == null)
                return;
            int group = ++_group;
            var taken = new HashSet<Vector2Int>();
            foreach (Transform t in src.transform)
            {
                if (ExampleSkip.Any(n => t.name.Contains(n)) || (t.GetComponentInChildren<Renderer>(true) == null && t.GetComponentInChildren<Light>(true) == null))
                    continue;
                if (t.localPosition.y > 1.5f)
                    continue;                                           // accroché à une structure qu'on ne reprend pas
                Vector3 p = at + rot * (t.localPosition + new Vector3(set.Pieces[0].X, 0f, set.Pieces[0].Z));
                Vector2Int c = Plan.CellOf(p);
                bool ok = true;
                for (int dx = -1; dx <= 1 && ok; dx++)
                for (int dy = -1; dy <= 1 && ok; dy++)
                    ok = ground.Contains(c + new Vector2Int(dx, dy)) && !blocked.Contains(c + new Vector2Int(dx, dy));
                if (ok && !t.name.Contains("Arbre") && EnsembleLibrary.Height(t.gameObject) > settings.ensembles.stepOver)
                {
                    int ring = Mathf.RoundToInt(settings.ensembles.tallClear / Plan.Cell);
                    for (int dx = -ring; dx <= ring && ok; dx++)
                    for (int dy = -ring; dy <= ring && ok; dy++)
                        ok = Plan.OwnerAt(c + new Vector2Int(dx, dy)) >= 0 && !Gallery(c + new Vector2Int(dx, dy)) && !_tallCells.Contains(c + new Vector2Int(dx, dy));
                    if (ok)
                        _tallCells.Add(c);
                }

                if (!ok)
                    continue;
                Finish(Instantiate(t.gameObject, p, rot * t.localRotation, root), t.name, group);
                // Emprise réelle (étagères, tables, haies) : les ensembles suivants la contournent.
                taken.Add(c);
                if (t.GetComponentInChildren<MeshFilter>() is MeshFilter mf && mf.sharedMesh != null)
                {
                    Vector3 e = Vector3.Scale(mf.sharedMesh.bounds.extents, t.localScale);
                    for (float x = -e.x; x <= e.x; x += 1f)
                    for (float z = -e.z; z <= e.z; z += 1f)
                        taken.Add(Plan.CellOf(p + rot * t.localRotation * new Vector3(x, 0f, z)));
                }
            }

            blocked.UnionWith(taken);
            EnsembleCount++;
        }

        /// <summary>
        /// Décor que le traqueur enjambe (tombes, massifs, arbres, haies…) : obstacle des goules et du bot (D-119), qui marchaient dedans.
        /// </summary>
        private void MarkObstacles()
        {
            if (Plan.Hand == null)
                return;
            foreach (DecorTag t in _root.GetComponentsInChildren<DecorTag>(true))
            {
                if (t.GetComponent<NotWalkable>() != null || t.GetComponentInParent<StalkerPassesThrough>() == null || Routes.Any(n => t.name.Contains(n))
                    || t.GetComponentInChildren<Collider>() == null || t.GetComponentInChildren<ItHunts.Orbs.Grabbable>() != null)
                    continue;
                t.gameObject.AddComponent<NotWalkable>();
            }
        }

        private Material _hedgeMaterial;                                  // propre au niveau, détruit avec lui (OnDestroy)

        /// <summary>
        /// Seconde revue de la DA (D-120) : la haie du kit (Jardin_Haie_6m), à l'ombre, se lisait comme un cube brun. Ses feuilles prennent
        /// une copie plus claire et plus verte de la matière peinte (fusionnée à part, comme une autre matière).
        /// </summary>
        private Material HedgeMaterial()
        {
            if (_hedgeMaterial != null)
                return _hedgeMaterial;
            _hedgeMaterial = new Material(settings.paintedFoliage) { name = settings.paintedFoliage.name + " (haie)" };
            foreach (string prop in new[] { "_BaseColor", "_Color" })
                if (_hedgeMaterial.HasProperty(prop))
                {
                    Color c = _hedgeMaterial.GetColor(prop);
                    _hedgeMaterial.SetColor(prop, new Color(c.r * 1.05f, c.g * 1.75f, c.b * 1.05f, c.a));   // couleur HDR (1.25, 1.35, 1) : pas de plafond à 1
                }

            return _hedgeMaterial;
        }

        private void PaintFoliage()
        {
            if (settings.paintedFoliage == null)
                return;
            foreach (MeshRenderer r in _root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool any = false;
                bool hedge = IsHedge(r.transform);
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && (mats[i].name == "M_Kit_Feuillage" || (hedge && mats[i] == settings.paintedFoliage)))
                    {
                        mats[i] = hedge ? HedgeMaterial() : settings.paintedFoliage;
                        any = true;
                    }

                if (any)
                    r.sharedMaterials = mats;
            }
        }

        private static bool IsHedge(Transform t)
        {
            for (; t != null; t = t.parent)
                if (t.name.StartsWith("Jardin_Haie"))
                    return true;
            return false;
        }

        private GameObject Place(string name, Transform root, Vector3 at, Quaternion rot, float scale, int group)
        {
            GameObject prefab = settings.ensembles.Find(name);
            if (prefab == null && Art != null)
                prefab = Art.Swap(name) ?? Art.Piece(name);
            if (prefab == null)
                prefab = settings.Prop(name);
            if (prefab == null)
                return null;
            GameObject go = Instantiate(prefab, at, rot, root);
            go.transform.localScale = prefab.transform.localScale * scale;
            Finish(go, name, group);
            return go;
        }

        private void Finish(GameObject go, string name, int group)
        {
            go.name = name;
            // Le dessus des meubles, gravats et rochers n'est pas un sol pour les ennemis (ils butaient dessus) ; les routes hautes, si.
            if (!Routes.Any(n => name.Contains(n)) && go.GetComponent<NotWalkable>() == null)
                go.AddComponent<NotWalkable>();
            Light[] lights = go.GetComponentsInChildren<Light>(true);
            if (go.transform.Find("Mobilier") != null)
            {
                ArtSet(go, lights, group);
                return;
            }

            DecorKind kind = MuralNames.Any(n => name.Contains(n)) ? DecorKind.Mural
                : PlantNames.Any(n => name.Contains(n)) ? DecorKind.Vegetal
                : lights.Length > 0 && !name.Contains("Autel") ? DecorKind.LumiereSol
                : DecorKind.Prop;
            Tag(go, kind);
            go.GetComponent<DecorTag>().group = group;
            foreach (Light l in lights)
            {
                if (l.type == LightType.Point && l.GetComponent<AmbianceLight>() == null)
                    l.gameObject.AddComponent<AmbianceLight>().kind = AmbianceLight.Kind.Torche;
                l.shadows = LightShadows.None;
                l.color = settings.torchColor;
                l.intensity = settings.torchIntensity * 1.1f;
                l.range = settings.torchRange * 1.4f;
            }

            float top = DecorAudit.Box(go, out Bounds b) ? b.max.y - go.transform.position.y : 0f;
            // Les arbres s'enjambent toujours (règle des arbres de l'acte III).
            if ((top <= settings.ensembles.stepOver || name.Contains("Arbre")) && go.GetComponent<StalkerPassesThrough>() == null)
                go.AddComponent<StalkerPassesThrough>();
        }

        /// <summary>Ensemble livré par la DA : sa lumière est gardée telle quelle (deux sources au plus), objets à lancer sur ses repères.</summary>
        private void ArtSet(GameObject go, Light[] lights, int group)
        {
            Tag(go, DecorKind.Prop);
            go.GetComponent<DecorTag>().group = group;
            List<Light> strong = lights.OrderByDescending(l => l.intensity * l.range).ToList();
            for (int i = 0; i < strong.Count; i++)
            {
                Light l = strong[i];
                l.shadows = LightShadows.None;
                if (i >= 2)
                {
                    l.gameObject.SetActive(false);
                    continue;
                }

                AmbianceLight al = l.GetComponent<AmbianceLight>();
                if (al == null)
                    al = l.gameObject.AddComponent<AmbianceLight>();
                al.kind = AmbianceLight.Kind.Torche;
                al.keepOwn = true;
            }

            if (go.GetComponent<StalkerPassesThrough>() == null)
                go.AddComponent<StalkerPassesThrough>();
            Transform marks = go.transform.Find("Reperes");
            if (marks == null)
                return;
            foreach (Transform m in marks)
                if (m.name.StartsWith("Pose_Attrapable"))
                    Place(_rng.Next(2) == 0 ? "Prop_Caisse_Attrapable" : "Prop_Tonneau_Attrapable", go.transform.parent, m.position + Vector3.up * 0.6f, m.rotation, 1f, group);
        }

        /// <summary>
        /// Contrôle des salles vides : points de sol à plus de <paramref name="far"/> m de tout mur, décor, pilier, creux ou palier.
        /// Les salles de jeu spéciales (arrivée du traqueur, hall, salle verrouillée) et le départ ne comptent pas.
        /// </summary>
        public List<string> EmptySpots(float far = 9f)
        {
            var res = new List<string>();
            foreach (ActRoom r in Plan.Rooms)
            {
                if (r.Special)
                    continue;
                var dist = new Dictionary<Vector2Int, int>();
                var q = new Queue<Vector2Int>();
                foreach (KeyValuePair<Vector2Int, CellUse> kv in r.Use)
                {
                    bool solid = Occupied(kv.Key) || (kv.Value != CellUse.Sol && kv.Value != CellUse.Passage);
                    for (int dx = -1; dx <= 1 && !solid; dx++)
                    for (int dy = -1; dy <= 1 && !solid; dy++)
                        solid = !r.Use.ContainsKey(kv.Key + new Vector2Int(dx, dy));
                    if (!solid)
                        continue;
                    dist[kv.Key] = 0;
                    q.Enqueue(kv.Key);
                }

                int worst = 0, count = 0;
                Vector2Int at = default;
                while (q.Count > 0)
                {
                    Vector2Int c = q.Dequeue();
                    for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Vector2Int k = c + new Vector2Int(dx, dy);
                        if (dist.ContainsKey(k) || !r.Use.ContainsKey(k))
                            continue;
                        dist[k] = dist[c] + 1;
                        q.Enqueue(k);
                        if (dist[k] * Plan.Cell > far)
                            count++;
                        if (dist[k] > worst)
                        {
                            worst = dist[k];
                            at = k;
                        }
                    }
                }

                if (count > 0)
                    res.Add($"{r.Name} : {count} cases vides, jusqu'à {worst * Plan.Cell:0} m de tout décor en {Plan.CellCenter(at, r.Floor):0}");
            }

            return res;
        }
    }
}
