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
        private static readonly string[] PlantNames = { "Massif", "Haie", "SousBois", "ArbreMort", "GrandArbre" };
        private int _group;

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
                if (ok && !NearDoor(r, Plan.CellCenter(c, r.Floor), door))
                    ground.Add(c);
            }

            // Départ du joueur : 8 m libres.
            if (r.Role == RoomRole.Depart)
            {
                var at = Vector2Int.RoundToInt(r.CenterCell);
                ground.RemoveWhere(c => Mathf.Abs(c.x - at.x) <= 4 && Mathf.Abs(c.y - at.y) <= 4);
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
            FloorEnsembles(r, root, lib, ground, blocked, lib.Sets.Where(s => !s.Wall && !s.Any && s.Fits(r.Function)).ToList());
            FloorEnsembles(r, root, lib, ground, blocked, lib.Sets.Where(s => !s.Wall && s.Any).ToList());
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
                    foreach (EnsembleLibrary.Set set in sets.OrderBy(_ => _rng.Next()))
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
            var live = sets.OrderByDescending(s => lib.Tall(s)).ThenByDescending(s => s.Width * s.Depth).ToList();
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
                if (_tallCells.Contains(k) || (r.Use.TryGetValue(k, out CellUse u) && u == CellUse.Terrasse))
                    return true;
            }

            return false;
        }

        private bool Gallery(Vector2Int c) =>
            Plan.OwnerAt(c) >= 0 && Plan.Rooms[Plan.OwnerAt(c)].Use.TryGetValue(c, out CellUse u) && u == CellUse.Terrasse;

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
                Place(p.Name, root, at + rot * new Vector3(p.X, p.Y, p.Z), rot * Quaternion.Euler(0f, p.Yaw, 0f), p.Scale, group);
            EnsembleCount++;
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

        private void PaintFoliage()
        {
            if (settings.paintedFoliage == null)
                return;
            foreach (MeshRenderer r in _root.GetComponentsInChildren<MeshRenderer>(true))
            {
                Material[] mats = r.sharedMaterials;
                bool any = false;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] != null && mats[i].name == "M_Kit_Feuillage")
                    {
                        mats[i] = settings.paintedFoliage;
                        any = true;
                    }

                if (any)
                    r.sharedMaterials = mats;
            }
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
            Light[] lights = go.GetComponentsInChildren<Light>(true);
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

        /// <summary>
        /// Contrôle des salles vides : points de sol à plus de <paramref name="far"/> m de tout mur, décor, pilier, creux ou palier.
        /// Les salles de jeu spéciales (arrivée du traqueur, hall, salle verrouillée) et le départ ne comptent pas.
        /// </summary>
        public List<string> EmptySpots(float far = 9f)
        {
            var res = new List<string>();
            foreach (ActRoom r in Plan.Rooms)
            {
                if (r.Special || r.Role == RoomRole.Depart)
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
