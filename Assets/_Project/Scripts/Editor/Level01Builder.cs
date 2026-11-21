using System.Collections.Generic;
using System.IO;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Level;
using System.Linq;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Niveau 1, la cave du château, en greybox (D-052). Grille de salles et de carrefours (7 × 7 à 120 m : environ 800 × 800 m,
    /// 49 salles), deux niveaux (sol et +16 m, pentes entre les deux), couloirs retirés au hasard en gardant des boucles (au
    /// moins 2 couloirs par salle : règle de Pac-Man du traqueur), repaire du traqueur en cul-de-sac à l'est, salle de départ au
    /// sud-ouest (marchand de l'artefact), salle de sortie au nord-est (grande porte, escaliers ; arène du combat de boss
    /// derrière, D-049). Canaux d'égout (eau profonde = vide), coffres derrière des obstacles, mini-zones de parcours non
    /// magnétiques, pièces fermées et salles verrouillées sur le pourtour. Volumes de salle et de tronçon, points d'apparition
    /// partout. Le constructeur n'écrit que Level_01_Base ; Level_01 (la copie que l'utilisateur retravaille) est créée une
    /// seule fois et jamais écrasée.
    /// </summary>
    public static partial class TestArenaSceneBuilder
    {
        private const string Level01LayoutPath = SettingsDir + "/Editor/ArenaLayout_Level01.asset";
        private const string Level01PlanPath = SettingsDir + "/Editor/LevelLayout_Level01.asset";
        private const string Level01ZonePath = SettingsDir + "/Zone_Level01.asset";
        public const string Level01BasePath = "Assets/Scenes/Level_01_Base.unity";
        public const string Level01Path = "Assets/Scenes/Level_01.unity";

        private static LevelLayoutSettings P;
        private static System.Random _rng;
        private static float _canalDepth = 4f, _waterDamage = 20f;
        private static Material _water;
        private static int _chestCount, _canalCount, _parkourCount;

        /// <summary>Résumé du dernier plan généré (mesures).</summary>
        public static string Level01Summary { get; private set; }

        [MenuItem("ItHunts/Générer le niveau 1 (Level_01_Base, sans toucher à Level_01)")]
        private static void BuildLevel01FromMenu() => Debug.Log(BuildLevel01());

        private static float R01() => (float)_rng.NextDouble();

        public static string BuildLevel01()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
                return "Prefab joueur absent : lancer d'abord ItHunts > Reconstruire le prefab joueur et la scène Test_Movement.";
            bool firstLayout = !File.Exists(Level01LayoutPath);
            var scene = BeginScene(Level01LayoutPath, out StalkerSettings stalkerSettings);
            if (firstLayout)
            {
                L.propsPerHall = 8;                                        // 40 salles : 8 objets chacune (physique)
                EditorUtility.SetDirty(L);
            }

            P = LoadOrCreate<LevelLayoutSettings>(Level01PlanPath);
            _rng = new System.Random(P.seed);
            _canalDepth = P.canalDepth;
            _waterDamage = P.waterDamage;
            _chestCount = _canalCount = _parkourCount = 0;
            _water = WaterSetup.Water();                                   // eau de la DA (PR #24), sinon greybox
            var zoneSettings = LoadOrCreate<ZoneSettings>(Level01ZonePath);

            DefineLevel01(out List<RoomDef> rooms, out HashSet<int> chestHalls, out HashSet<int> parkourHalls);
            for (int i = 0; i < Nodes.Count; i++)
                BuildNode(i);
            for (int e = 0; e < Edges.Count; e++)
                if (!IsLairEdge(e))
                    Corridor(e);
            _decorCount = 0;
            PropLibrary.Usage.Clear();
            foreach (RoomDef r in rooms)
            {
                BuildRoom(r);
                FurnishAnnex(r, AnnexTypes.TryGetValue(r.Name, out string kind) ? kind : "réserve");
            }

            BuildGalleries(P.waterDamage);
            for (int e = 0; e < Edges.Count; e++)
                if (!IsLairEdge(e))
                    CorridorDecor(e, P.minLane);
            ModuleUsage.Clear();
            for (int i = 0; i < Nodes.Count; i++)
            {
                if (i == _lairNode)
                    continue;
                if (NodeModules.ContainsKey(i))
                    PlaceModule(i, chestHalls.Contains(i));
                else if (Nodes[i].Hall)
                    HallInterior(i, false, false);                         // salle de départ (marchand)
            }

            BuildPassages();                                               // après les modules : on vérifie la place devant
            Level01CorridorFeatures();
            Residents();

            var souls = new GameObject("Âmes (éclats)").AddComponent<SoulSystem>();
            SetRef(souls, "settings", _economy);
            Material soulMat = UnlitMat("Economy_Soul", "Universal Render Pipeline/Unlit", _economy.soulColor);
            soulMat.enableInstancing = true;
            SetRef(souls, "soulMaterial", soulMat);
            Node st = Nodes[_startNode];
            Vector3 startAt = st.C + new Vector3(-st.Half * 0.5f, 0f, -st.HalfZ * 0.5f);
            Vector3 mAt = st.C + new Vector3(st.Half * 0.15f, 0f, st.HalfZ * 0.2f);
            BuildArtifactMerchant(_level, mAt, Quaternion.LookRotation(startAt - mAt, Vector3.up), _economy, _offerOrb);

            Vector3 min = new Vector3(float.MaxValue, -10f, float.MaxValue), max = new Vector3(float.MinValue, L.upperLevel + L.hallCeiling + 10f, float.MinValue);
            foreach (Node n in Nodes)
            {
                min = Vector3.Min(min, new Vector3(n.C.x - n.Half - 30f, -10f, n.C.z - n.HalfZ - 30f));
                max = Vector3.Max(max, new Vector3(n.C.x + n.Half + 30f, max.y, n.C.z + n.HalfZ + 30f));
            }

            Vector3 exitAt = FinishScene(stalkerSettings, zoneSettings, min, max, startAt, Quaternion.Euler(0f, 45f, 0f), playerPrefab);
            LastExitLanding = exitAt;
            EditorSceneManager.SaveScene(scene, Level01BasePath);

            bool copied = false;
            if (!File.Exists(Level01Path))
            {
                AssetDatabase.CopyAsset(Level01BasePath, Level01Path);
                copied = true;
            }

            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == Level01Path))
            {
                scenes.Add(new EditorBuildSettingsScene(Level01Path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            int halls = 0, crossings = 0, upper = 0;
            foreach (Node n in Nodes)
            {
                if (n.Name.StartsWith("Repaire") || n.Name.StartsWith("Arène"))
                    continue;
                if (n.Hall) halls++; else crossings++;
                if (n.C.y > 0.1f) upper++;
            }

            Vector2 size = new Vector2(max.x - min.x - 60f, max.z - min.z - 60f);
            Level01Summary = $"{halls} salles et {crossings} carrefours ({upper} en hauteur), {Edges.Count - 1} couloirs, {rooms.Count} pièces fermées, " +
                             $"{_chestCount} coffres, {_canalCount} canaux, {_parkourCount} zones de parcours ({Galleries.Count} galeries), {Passages.Count - _closedPassages} passages en hauteur ({_closedPassages} refermés : module devant), {_spawnCount} points d'apparition, " +
                             $"{_residentGhouls} goules et {_residentFlyers} volants habitants, {_decorCount} props de décor (salles en plus), " +
                             $"{Object.FindObjectsByType<RoomVolume>(FindObjectsSortMode.None).Length} volumes ; emprise {size.x:0} × {size.y:0} m";
            return $"OK : {Level01BasePath} ({Level01Summary}) ; " +
                   (copied ? $"{Level01Path} créée (copie à retravailler)." : $"{Level01Path} existe déjà : pas touchée.");
        }

        private static int _residentGhouls, _residentFlyers;

        /// <summary>
        /// Habitants (étape 8, après playtest) : dans chaque grande salle (pas la salle de départ), 2 à 5 goules selon sa taille,
        /// et 1 à 2 volants dans les salles du niveau haut ; en attente avec l'aggro habituelle ; tués, ils ne reviennent pas.
        /// </summary>
        private static void Residents()
        {
            var root = new GameObject("Habitants des salles").transform;
            _ghouls = root;
            _residentGhouls = _residentFlyers = 0;
            for (int i = 0; i < Nodes.Count; i++)
            {
                Node n = Nodes[i];
                if (!n.Hall || i == _startNode || i == _lairNode || n.Name.StartsWith("Arène"))
                    continue;
                float t = Mathf.InverseLerp(P.hallMin, P.hallMax, n.Half * 2f);
                int g = Mathf.RoundToInt(Mathf.Lerp(P.residentGhoulsMin, P.residentGhoulsMax, t));
                Vector3[] spots = ResidentSpots(i);
                for (int k = 0; k < g; k++)
                {
                    Vector3 p = spots[k % spots.Length];
                    BuildGhoul(p, Quaternion.Euler(0f, _rng.Next(360), 0f), $"habitante, {n.Name} {k + 1}");
                    _residentGhouls++;
                }

                if (n.C.y > 0.1f)
                {
                    int f = _rng.Next(P.residentFlyersMin, P.residentFlyersMax + 1);
                    for (int k = 0; k < f; k++)
                    {
                        BuildFlyer(n.C + new Vector3((k - 0.5f) * n.Half * 0.5f, 12f, n.HalfZ * 0.2f), $"habitant, {n.Name} {k + 1}");
                        _residentFlyers++;
                    }
                }
            }
        }

        /// <summary>Emplacements des habitants : marqueurs du module (ResidentSpot), sinon un cercle au sol libre.</summary>
        private static Vector3[] ResidentSpots(int node)
        {
            Node n = Nodes[node];
            var list = new List<Vector3>();
            if (ModuleSpots.TryGetValue(node, out List<Vector3> fromModule))
                list.AddRange(fromModule);
            if (list.Count == 0)
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * Mathf.PI * 2f + 0.2f;
                    Vector3 p = n.C + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * n.Half * 0.45f;
                    if (!Physics.CheckSphere(p + Vector3.up * 0.8f, 0.6f, ~0, QueryTriggerInteraction.Ignore))
                        list.Add(p);
                }

            if (list.Count == 0)
                list.Add(n.C);
            return list.ToArray();
        }

        /// <summary>Emplacements des habitants posés par les modules de salle (salle → points au sol).</summary>
        private static readonly Dictionary<int, List<Vector3>> ModuleSpots = new Dictionary<int, List<Vector3>>();

        /// <summary>Palier de sortie du dernier niveau généré (arène du combat de boss derrière).</summary>
        public static Vector3 LastExitLanding { get; private set; }

        // ---------- Plan ----------

        private static void DefineLevel01(out List<RoomDef> rooms, out HashSet<int> chestHalls, out HashSet<int> parkourHalls)
        {
            Nodes.Clear();
            Edges.Clear();
            NodeOpenings.Clear();
            CorridorOpenings.Clear();
            Canals.Clear();
            Pits.Clear();
            RoomHalls.Clear();
            ModuleSpots.Clear();
            int cols = P.columns, rws = P.rows;
            float sp = P.spacing, x0 = -(cols - 1) * sp * 0.5f, z0 = -(rws - 1) * sp * 0.5f;
            _startNode = 0;
            _exitNode = (rws - 1) * cols + (cols - 1);
            int total = cols * rws;
            var crossingOf = new bool[total];
            var highOf = new bool[total];
            for (int idx = 0; idx < total; idx++)
            {
                bool fixedHall = idx == _startNode || idx == _exitNode;
                crossingOf[idx] = !fixedHall && R01() < P.crossingShare;
                highOf[idx] = !fixedHall && R01() < P.upperShare;
            }

            AssignModules(total, crossingOf, highOf);
            for (int j = 0; j < rws; j++)
            for (int i = 0; i < cols; i++)
            {
                int idx = j * cols + i;
                bool start = idx == _startNode, exit = idx == _exitNode;
                bool crossing = crossingOf[idx], high = highOf[idx];
                RoomModule mod = NodeModules.TryGetValue(idx, out RoomModule mm) ? mm : null;
                float size = mod != null ? mod.size : crossing ? L.crossingSize : 56f;
                string name = $"{(char)('A' + i)}{j + 1}" + (start ? " (départ)" : exit ? " (sortie)" : crossing ? " (carrefour)" : "");
                Nodes.Add(new Node
                {
                    Name = name, C = new Vector3(x0 + i * sp, high ? L.upperLevel : 0f, z0 + j * sp), Half = size * 0.5f, HalfZ = size * 0.5f,
                    Ceil = crossing ? L.crossingCeiling : L.hallCeiling, Hall = !crossing,
                });
                if (mod != null && mod.hole == ModuleHole.Canal)
                    Canals[idx] = new Vector2(mod.holeRect.center.y, mod.holeRect.height);
                if (mod != null && mod.hole == ModuleHole.Vide)
                    Pits[idx] = mod;
            }

            // Couloirs : toute la grille, puis on en retire en gardant 2 couloirs par salle et une carte d'un seul tenant.
            var edges = new List<Vector2Int>();
            for (int j = 0; j < rws; j++)
            for (int i = 0; i < cols; i++)
            {
                int idx = j * cols + i;
                if (i + 1 < cols) edges.Add(new Vector2Int(idx, idx + 1));
                if (j + 1 < rws) edges.Add(new Vector2Int(idx, idx + cols));
            }

            int toRemove = Mathf.RoundToInt(edges.Count * P.removedCorridorShare);
            var order = new List<int>();
            for (int k = 0; k < edges.Count; k++) order.Add(k);
            for (int k = order.Count - 1; k > 0; k--)
            {
                int r = _rng.Next(k + 1);
                (order[k], order[r]) = (order[r], order[k]);
            }

            var removed = new HashSet<int>();
            foreach (int k in order)
            {
                if (removed.Count >= toRemove)
                    break;
                removed.Add(k);
                if (Degree(edges, removed, edges[k].x) < 2 || Degree(edges, removed, edges[k].y) < 2 || !Connected(edges, removed, Nodes.Count))
                    removed.Remove(k);
            }

            for (int k = 0; k < edges.Count; k++)
                if (!removed.Contains(k))
                    Edges.Add(edges[k]);

            // Repaire du traqueur : cul-de-sac à l'est de la salle du milieu de la colonne est.
            int host = (rws / 2) * cols + (cols - 1);
            Node h = Nodes[host];
            float depth = L.lairDepth, X = L.crossingSize * 0.5f;
            _lairNode = Nodes.Count;
            Nodes.Add(new Node { Name = "Repaire du traqueur", C = h.C + new Vector3(h.Half + depth * 0.5f, 0f, 0f), Half = depth * 0.5f, HalfZ = X, Ceil = L.lairHeight });
            Edges.Add(new Vector2Int(host, _lairNode));

            float hgt = L.corridorHeight;
            for (int e = 0; e < Edges.Count; e++)
            {
                float w = Width(e);
                Node a = Nodes[Edges[e].x], b = Nodes[Edges[e].y];
                int sa = SideOf(b.C - a.C), sb = SideOf(a.C - b.C);
                float along = sa == SideE || sa == SideW ? a.C.z : a.C.x;
                AddOpening(Edges[e].x, sa, new Opening(along - w / 2f, along + w / 2f, a.C.y, a.C.y + hgt));
                if (!IsLairEdge(e))
                    AddOpening(Edges[e].y, sb, new Opening(along - w / 2f, along + w / 2f, b.C.y, b.C.y + hgt));
            }

            AddExitOpening(_exitNode);

            // Pièces annexes (2 bis) le long des couloirs plats : meublées, ouvertes par une porte de 3 m ; les premières sont les
            // salles verrouillées. Une par côté de couloir au plus ; le côté pris est noté (galeries de parcours, passages).
            rooms = new List<RoomDef>();
            AnnexTypes.Clear();
            CorridorSideUsed.Clear();
            var flat = new List<int>();
            for (int e = 0; e < Edges.Count; e++)
                if (!IsLairEdge(e) && !IsSlope(e))
                    flat.Add(e);
            Shuffle(flat);
            foreach (int e in flat)
            {
                CorridorEnds(e, out Vector3 cs, out Vector3 ce, out _, out _, out _, out bool ax);
                float len = Vector3.Distance(cs, ce);
                if (len < 40f || R01() > P.annexShare)
                    continue;
                int side = R01() < 0.5f ? 1 : -1;
                float t = R01() < 0.5f ? 0.3f : 0.7f;
                float along = ax ? Mathf.Lerp(cs.x, ce.x, t) : Mathf.Lerp(cs.z, ce.z, t);
                bool locked = rooms.Count(r => r.Locked) < P.lockedRooms;
                string type = locked ? "réserve" : AnnexKinds[_rng.Next(AnnexKinds.Length)];
                string name = locked ? $"Salle verrouillée {CorridorName(e)}" : $"Pièce ({type}) {CorridorName(e)}";
                rooms.Add(new RoomDef { Name = name, Node = -1, Edge = e, Side = side, Along = along, Locked = locked });
                AnnexTypes[name] = type;
                CorridorSideUsed[(e, side)] = true;
            }

            PlanParkour(P.parkourCorridors, P.passages);                 // points 3 et 4 (après les annexes)
            AddRoomOpenings(rooms);

            // Coffres : dans des salles à module (un de leurs emplacements de coffre), ni départ ni sortie.
            var halls = new List<int>();
            for (int i = 0; i < _lairNode; i++)
                if (Nodes[i].Hall && i != _startNode && i != _exitNode && NodeModules.ContainsKey(i))
                    halls.Add(i);
            Shuffle(halls);
            parkourHalls = new HashSet<int>();
            chestHalls = new HashSet<int>();
            foreach (int i in halls)
                if (chestHalls.Count < P.chests)
                    chestHalls.Add(i);
        }

        /// <summary>Types d'annexes meublées (2 bis).</summary>
        private static readonly string[] AnnexKinds = { "chambre", "cellule", "réserve", "cuisine", "armurerie", "chapelle", "crypte" };
        private static readonly Dictionary<string, string> AnnexTypes = new Dictionary<string, string>();
        /// <summary>Côtés de couloir déjà pris (annexe, galerie de parcours, passage).</summary>
        private static readonly Dictionary<(int, int), bool> CorridorSideUsed = new Dictionary<(int, int), bool>();
        /// <summary>Module de chaque salle (et carrefour) ; fosses de parcours.</summary>
        private static readonly Dictionary<int, RoomModule> NodeModules = new Dictionary<int, RoomModule>();
        private static readonly Dictionary<int, RoomModule> Pits = new Dictionary<int, RoomModule>();

        /// <summary>
        /// Tirage des modules : canaux (P.canalHalls) et fosses de parcours (P.parkourHalls, au sol) d'abord, puis les autres au
        /// hasard selon leur poids et leur niveau (sol, haut) ; carrefours : modules de carrefour. La salle de départ n'en a pas.
        /// </summary>
        private static void AssignModules(int total, bool[] crossingOf, bool[] highOf)
        {
            NodeModules.Clear();
            Pits.Clear();
            List<RoomModule> mods = ModuleLibrary.Load();
            bool Fits(RoomModule m, bool high) => m.level == ModuleLevel.Tous || (m.level == ModuleLevel.Haut) == high;
            RoomModule Pick(IEnumerable<RoomModule> pool)
            {
                var list = pool.ToList();
                float sum = list.Sum(m => m.weight);
                if (list.Count == 0 || sum <= 0f)
                    return null;
                float r = R01() * sum;
                foreach (RoomModule m in list)
                {
                    r -= m.weight;
                    if (r <= 0f)
                        return m;
                }

                return list[list.Count - 1];
            }

            var halls = new List<int>();
            for (int i = 0; i < total; i++)
                if (i != _startNode && !crossingOf[i])
                    halls.Add(i);
            Shuffle(halls);
            int canals = 0, pits = 0;
            foreach (int i in halls)
            {
                bool exit = i == _exitNode, high = highOf[i];
                RoomModule m = null;
                if (!exit && canals < P.canalHalls)
                    m = Pick(mods.Where(x => x.kind == ModuleKind.Salle && x.hole == ModuleHole.Canal && Fits(x, high)));
                if (m != null)
                    canals++;
                else if (!exit && pits < P.parkourHalls && !high)
                {
                    m = Pick(mods.Where(x => x.kind == ModuleKind.Salle && x.hole == ModuleHole.Vide && Fits(x, high)));
                    if (m != null)
                        pits++;
                }

                if (m == null)
                    m = Pick(mods.Where(x => x.kind == ModuleKind.Salle && x.hole == ModuleHole.Aucun && Fits(x, high)));
                if (m != null)
                    NodeModules[i] = m;
            }

            for (int i = 0; i < total; i++)
                if (crossingOf[i])
                {
                    RoomModule m = Pick(mods.Where(x => x.kind == ModuleKind.Carrefour));
                    if (m != null)
                        NodeModules[i] = m;
                }
        }

        /// <summary>
        /// Pose le module d'une salle (prefab lié : une retouche du module se répercute) : tourné d'un quart de tour au hasard
        /// (sauf canal). Habitants à ses emplacements ; coffre à l'un de ses emplacements si la salle en a un.
        /// </summary>
        private static void PlaceModule(int i, bool chest)
        {
            if (!NodeModules.TryGetValue(i, out RoomModule mod))
                return;
            Node n = Nodes[i];
            int k = mod.rotate ? _rng.Next(4) : 0;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(mod.gameObject, _level);
            go.name = $"Module {mod.displayName} : {n.Name}";
            go.transform.SetPositionAndRotation(n.C, Quaternion.Euler(0f, k * 90f, 0f));
            var spots = new List<Vector3>();
            foreach (ResidentSpot r in go.GetComponentsInChildren<ResidentSpot>())
                if (!r.flyer)
                    spots.Add(r.transform.position);
            ModuleSpots[i] = spots;
            _spawnCount += go.GetComponentsInChildren<SpawnPoint>().Length;
            ChestSpot[] chestSpots = go.GetComponentsInChildren<ChestSpot>();
            if (chest && chestSpots.Length > 0)
            {
                ChestSpot c = chestSpots[_rng.Next(chestSpots.Length)];
                BuildChest($"Coffre : {n.Name}", c.transform.position, c.transform.rotation, -1);
                _chestCount++;
            }

            ModuleUsage[mod.displayName] = ModuleUsage.TryGetValue(mod.displayName, out int u) ? u + 1 : 1;
        }

        public static readonly Dictionary<string, int> ModuleUsage = new Dictionary<string, int>();

        /// <summary>Sol percé d'une fosse vide (module de parcours) : tour du sol, fond, bords, vide (retour au bord, dégâts).</summary>
        private static void PitFloor(Node n, RoomModule mod)
        {
            float hx = n.Half, hz = n.HalfZ, y = n.C.y, d = mod.holeDepth;
            Vector3 c = n.C;
            Rect r = mod.holeRect;
            float x0 = r.xMin, x1 = r.xMax, za = r.yMin, zb = r.yMax;
            void Slab(string nm, float xa, float xb, float zq, float zr)
            {
                if (xb - xa < 0.01f || zr - zq < 0.01f)
                    return;
                if (y > 0.1f)
                    Piece($"{n.Name} : sol {nm} (bloc de {y:0} m)", new Vector3(c.x + (xa + xb) / 2f, y / 2f, c.z + (zq + zr) / 2f), new Vector3(xb - xa, y, zr - zq), _floor);
                else
                    Piece($"{n.Name} : sol {nm}", new Vector3(c.x + (xa + xb) / 2f, -0.5f, c.z + (zq + zr) / 2f), new Vector3(xb - xa, 1f, zr - zq), _floor);
            }

            Slab("ouest", -hx - T, x0, -hz - T, hz + T);
            Slab("est", x1, hx + T, -hz - T, hz + T);
            Slab("sud", x0, x1, -hz - T, za);
            Slab("nord", x0, x1, zb, hz + T);
            float bottom = y - d;
            Piece($"{n.Name} : fond de la fosse", new Vector3(c.x + r.center.x, bottom - 0.5f, c.z + r.center.y), new Vector3(r.width, 1f, r.height), _ceiling, walkable: false);
            Piece($"{n.Name} : bord ouest de la fosse", new Vector3(c.x + x0 - 0.5f, (bottom + y - 1f) / 2f, c.z + r.center.y), new Vector3(1f, d - 1f, r.height + 2f), _wall, walkable: false);
            Piece($"{n.Name} : bord est de la fosse", new Vector3(c.x + x1 + 0.5f, (bottom + y - 1f) / 2f, c.z + r.center.y), new Vector3(1f, d - 1f, r.height + 2f), _wall, walkable: false);
            Piece($"{n.Name} : bord sud de la fosse", new Vector3(c.x + r.center.x, (bottom + y - 1f) / 2f, c.z + za - 0.5f), new Vector3(r.width, d - 1f, 1f), _wall, walkable: false);
            Piece($"{n.Name} : bord nord de la fosse", new Vector3(c.x + r.center.x, (bottom + y - 1f) / 2f, c.z + zb + 0.5f), new Vector3(r.width, d - 1f, 1f), _wall, walkable: false);
            var trig = new GameObject($"{n.Name} : vide de la fosse");
            trig.transform.SetParent(_level, false);
            trig.transform.position = new Vector3(c.x + r.center.x, (bottom + y - 1.5f) / 2f, c.z + r.center.y);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(r.width, y - 1.5f - bottom, r.height);
            var respawns = new List<Transform>();
            foreach (Vector3 p in new[] { new Vector3(x0 - 2.5f, 0f, r.center.y), new Vector3(x1 + 2.5f, 0f, r.center.y), new Vector3(r.center.x, 0f, za - 2.5f), new Vector3(r.center.x, 0f, zb + 2.5f) })
            {
                var t = new GameObject("Reprise").transform;
                t.SetParent(trig.transform, false);
                t.position = c + p + Vector3.up * 0.2f;
                respawns.Add(t);
            }

            trig.AddComponent<FallVoid>().Configure(_waterDamage, respawns.ToArray());
            trig.AddComponent<NotWalkable>();
            _parkourCount++;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int k = list.Count - 1; k > 0; k--)
            {
                int r = _rng.Next(k + 1);
                (list[k], list[r]) = (list[r], list[k]);
            }
        }

        private static int Degree(List<Vector2Int> edges, HashSet<int> removed, int node)
        {
            int d = 0;
            for (int k = 0; k < edges.Count; k++)
                if (!removed.Contains(k) && (edges[k].x == node || edges[k].y == node))
                    d++;
            return d;
        }

        private static bool Connected(List<Vector2Int> edges, HashSet<int> removed, int count)
        {
            var seen = new bool[count];
            var stack = new Stack<int>();
            stack.Push(0);
            seen[0] = true;
            int n = 1;
            while (stack.Count > 0)
            {
                int a = stack.Pop();
                for (int k = 0; k < edges.Count; k++)
                {
                    if (removed.Contains(k))
                        continue;
                    int b = edges[k].x == a ? edges[k].y : edges[k].y == a ? edges[k].x : -1;
                    if (b >= 0 && !seen[b])
                    {
                        seen[b] = true;
                        n++;
                        stack.Push(b);
                    }
                }
            }

            return n == count;
        }

        // ---------- Canal (D-052) ----------

        /// <summary>Sol coupé par une tranchée (le long de x, au milieu zc relatif, largeur w), eau profonde, rives, ponts.</summary>
        private static void CanalFloor(Node n, float zc, float w)
        {
            float hx = n.Half, hz = n.HalfZ, y = n.C.y, d = _canalDepth;
            Vector3 c = n.C;
            float za = zc - w / 2f, zb = zc + w / 2f;          // bords de la tranchée (relatifs)
            float south = za + hz + T, north = hz + T - zb;
            if (y > 0.1f)
            {
                Piece($"{n.Name} : sol sud (bloc de {y:0} m)", new Vector3(c.x, y / 2f, c.z + (-hz - T + za) / 2f), new Vector3(2f * hx, y, south), _floor);
                Piece($"{n.Name} : sol nord (bloc de {y:0} m)", new Vector3(c.x, y / 2f, c.z + (zb + hz + T) / 2f), new Vector3(2f * hx, y, north), _floor);
                Piece($"{n.Name} : fond du canal", new Vector3(c.x, (y - d) / 2f, c.z + zc), new Vector3(2f * hx, y - d, w), _ceiling, walkable: false);
            }
            else
            {
                Piece($"{n.Name} : sol sud", new Vector3(c.x, -0.5f, c.z + (-hz - T + za) / 2f), new Vector3(2f * hx + 2f * T, 1f, south), _floor);
                Piece($"{n.Name} : sol nord", new Vector3(c.x, -0.5f, c.z + (zb + hz + T) / 2f), new Vector3(2f * hx + 2f * T, 1f, north), _floor);
                Piece($"{n.Name} : fond du canal", new Vector3(c.x, -d - 0.5f, c.z + zc), new Vector3(2f * hx + 2f * T, 1f, w + 2f), _ceiling, walkable: false);
                Piece($"{n.Name} : bord sud du canal", new Vector3(c.x, -(d + 1f) / 2f, c.z + za - 0.5f), new Vector3(2f * hx + 2f * T, d - 1f, 1f), _wall, walkable: false);
                Piece($"{n.Name} : bord nord du canal", new Vector3(c.x, -(d + 1f) / 2f, c.z + zb + 0.5f), new Vector3(2f * hx + 2f * T, d - 1f, 1f), _wall, walkable: false);
            }

            // Eau (surface à 1.2 m sous le sol) et déclencheur (vide : on remonte sur la rive, D-052).
            Piece($"{n.Name} : eau du canal", new Vector3(c.x, y - 1.2f, c.z + zc), new Vector3(2f * hx, 0.05f, w), _water, walkable: false, collider: false);
            var water = new GameObject($"{n.Name} : eau profonde (vide)");
            water.transform.SetParent(_level, false);
            float top = y - 0.8f, bottom = y - d;
            water.transform.position = new Vector3(c.x, (top + bottom) / 2f, c.z + zc);
            var box = water.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f * hx, top - bottom, w);
            water.AddComponent<DeepWater>().Configure(_waterDamage, w / 2f + 1.5f, y + 0.1f - water.transform.position.y);
            WaterSplash.Add(water, WaterSetup.Splash(), y - 1.2f);
            water.AddComponent<NotWalkable>();

            // Ponts au niveau du sol : le grand dans l'axe du couloir nord (le traqueur passe), un petit sur le côté.
            Piece($"{n.Name} : pont du canal (grand)", new Vector3(c.x, y - 0.5f, c.z + zc), new Vector3(L.corridorWidth, 1f, w + 2f), _bridge);
            Piece($"{n.Name} : pont du canal (petit)", new Vector3(c.x + hx * 0.65f, y - 0.5f, c.z + zc), new Vector3(4f, 1f, w + 2f), _bridge);
            Lamp($"{n.Name} : lumière du canal", new Vector3(c.x - hx * 0.5f, y + 4f, c.z + zc), new Color(0.3f, 0.7f, 0.8f), 3f, 25f);
            _canalCount++;
        }

        // ---------- Intérieurs ----------

        /// <summary>Point relatif tourné de k quarts de tour autour du centre de la salle.</summary>
        private static Vector3 Rot(int k, float x, float z, float y = 0f)
        {
            for (int i = 0; i < k; i++)
                (x, z) = (z, -x);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Intérieur d'une grande salle : deux corniches pleines (11 m) reliées par un pont, murs de course, cibles de traction,
        /// objets à lancer et lustre, points d'apparition ; coffre dans un coin derrière un muret ; ou zone de parcours non
        /// magnétique (D-051). Les salles à canal gardent leur moitié nord dégagée.
        /// </summary>
        private static readonly HashSet<int> RoomHalls = new HashSet<int>();

        private static void HallInterior(int i, bool chest, bool parkour)
        {
            Node n = Nodes[i];
            float h = n.Half;
            int k = Canals.ContainsKey(i) ? 0 : _rng.Next(4);
            Vector3 f = n.C;
            float open = L.slopeCorridorWidth / 2f + 3f;          // dégagement des ouvertures de couloir (au milieu des murs)

            void Rect(string name, float xa, float xb, float za, float zb)
            {
                Vector3 p = Rot(k, xa, za), q = Rot(k, xb, zb);
                Mezzanine(name, f, Mathf.Min(p.x, q.x), Mathf.Max(p.x, q.x), Mathf.Min(p.z, q.z), Mathf.Max(p.z, q.z), SideN);
            }

            bool canal = Canals.ContainsKey(i);
            if (parkour)
                ParkourCourse(n, k);
            else if (!canal && !RoomHalls.Contains(i))               // pièce fermée sur un mur : pas de corniche devant sa porte
            {
                Rect($"{n.Name} A", -h, -open, h - 6f, h);
                Rect($"{n.Name} B", -h, -open, -h, -h + 6f);
                float xm = (-h + -open) / 2f;
                Bridge($"{n.Name}", f, Rot(k, xm, -h + 6f), Rot(k, xm, h - 6f));
            }

            float zs = canal ? -0.45f : 0.15f;                 // salle à canal : tout dans la moitié sud
            RunWall($"{n.Name} 1", f + Rot(k, h * 0.45f, h * zs), 14f, 12f, k % 2 == 0);
            RunWall($"{n.Name} 2", f + Rot(k, h * 0.05f, -h * 0.55f), 12f, 10f, k % 2 == 1);
            Magnet($"{n.Name} haut", f + Rot(k, h - 0.6f, -h * 0.4f, 22f), k % 2 == 0 ? new Vector3(0.5f, 6f, 6f) : new Vector3(6f, 6f, 0.5f));
            Magnet($"{n.Name} plafond", f + new Vector3(0f, n.Ceil - 0.3f, canal ? -h * 0.4f : 0f), new Vector3(8f, 0.5f, 8f));
            // Muret de couverture (on y monte, on s'y cache des goules, pas du traqueur).
            Piece($"{n.Name} : muret", f + Rot(k, -h * 0.15f, -h * 0.25f, 0.75f), k % 2 == 0 ? new Vector3(6f, 1.5f, 1.2f) : new Vector3(1.2f, 1.5f, 6f), _platform);

            if (chest)
                ChestCorner(n, k, canal);

            var clusters = new[] { Rot(k, h * 0.55f, -h * 0.65f), Rot(k, -h * 0.35f, h * zs), Rot(k, h * 0.2f, -h * 0.15f), Rot(k, -h * 0.6f, -h * 0.55f), Rot(k, h * 0.65f, h * (canal ? -0.15f : 0.45f)) };
            var ghouls = new Vector2[10];
            for (int g = 0; g < ghouls.Length; g++)
            {
                float a = g / 10f * Mathf.PI * 2f + 0.3f;
                float r = h * (g % 2 == 0 ? 0.6f : 0.35f);
                Vector3 p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (canal && p.z > h * 0.2f)
                    p.z = -p.z;                                      // pas dans le canal
                ghouls[g] = new Vector2(p.x, p.z);
            }

            HallContents(n, clusters, ghouls, Rot(k, h * 0.2f, canal ? -h * 0.2f : h * 0.2f, 12f));
        }

        /// <summary>Coffre dans un coin, derrière un muret de 3 m (on le contourne par le bout ouvert).</summary>
        private static void ChestCorner(Node n, int k, bool canal)
        {
            float h = n.Half;
            float zc = canal ? -1f : 1f;                          // canal : coin sud
            Vector3 f = n.C;
            Vector3 wallC = Rot(k, h * 0.62f, zc * h * 0.74f, 1.5f);
            Vector3 wallSize = k % 2 == 0 ? new Vector3(1f, 3f, h * 0.42f) : new Vector3(h * 0.42f, 3f, 1f);
            Piece($"{n.Name} : muret du coffre", f + wallC, wallSize, _wall);
            Vector3 at = f + Rot(k, h * 0.84f, zc * h * 0.84f);
            Vector3 face = f + Rot(k, h * 0.3f, zc * h * 0.84f) - at;
            BuildChest($"Coffre : {n.Name}", at, Quaternion.LookRotation(new Vector3(face.x, 0f, face.z).normalized, Vector3.up), -1);
            _chestCount++;
        }

        /// <summary>
        /// Mini-zone de parcours non magnétique (D-051) : une route en hauteur le long d'un côté de la salle (plateformes de
        /// 4 × 4 m de 2.5 à 6.5 m, écarts de 3 m, panneaux de course), où la traction ne s'accroche pas.
        /// </summary>
        private static void ParkourCourse(Node n, int k)
        {
            float h = n.Half;
            Vector3 f = n.C;
            var parent = new GameObject($"Zone de parcours (non magnétique) : {n.Name}").transform;
            parent.SetParent(_level, false);
            float[] heights = { 2.5f, 4.5f, 6.5f, 5f, 6.5f, 4.5f, 2.5f };
            float x = -h * 0.55f, step = 7f;
            // 4 plateformes au plus, décalées vers le sud : le reste de la salle reste large (le traqueur fait 7 m de large).
            int count = Mathf.Min(4, Mathf.FloorToInt((2f * h * 0.85f) / step));
            float z0 = -h * 0.35f - (count - 1) * step / 2f;
            for (int p = 0; p < count; p++)
            {
                float top = heights[p];
                Vector3 c = Rot(k, x, z0 + p * step, top / 2f);
                NonMagneticPiece($"plateforme {p + 1} ({top:0.#} m)", parent, f + c, new Vector3(4f, top, 4f));
                if (p % 2 == 1)
                {
                    Vector3 wc = Rot(k, x - 3.5f, z0 + p * step - step / 2f, 5f);
                    NonMagneticPiece($"panneau de course {p}", parent, f + wc, k % 2 == 0 ? new Vector3(0.6f, 10f, 6f) : new Vector3(6f, 10f, 0.6f), walkable: false);
                }
            }

            _parkourCount++;
        }

        /// <summary>Poutres et ponts en hauteur dans une partie des couloirs plats (dessous à 10 m).</summary>
        private static void Level01CorridorFeatures()
        {
            for (int e = 0; e < Edges.Count; e++)
            {
                if (IsLairEdge(e) || IsSlope(e) || R01() >= P.corridorFeatureShare)
                    continue;
                Vector3 p = CorridorPoint(e, 0.5f);
                CorridorEnds(e, out _, out _, out _, out _, out Vector3 dir, out bool alongX);
                if (R01() < 0.5f)
                    Piece($"Poutre (dessous à {L.bridgeBottom:0} m) : {CorridorName(e)}", p + Vector3.up * (L.bridgeBottom + 0.5f),
                        alongX ? new Vector3(1f, 1f, L.corridorWidth) : new Vector3(L.corridorWidth, 1f, 1f), _bridge);
                else
                {
                    float w = L.corridorWidth, bw = L.bridgeWidth;
                    Piece($"Pont (dessous à {L.bridgeBottom:0} m) : {CorridorName(e)}", p + Vector3.up * (L.bridgeBottom + 0.5f),
                        alongX ? new Vector3(bw, 1f, w) : new Vector3(w, 1f, bw), _bridge);
                    Magnet($"{CorridorName(e)} au-dessus du pont", p + Vector3.up * (L.corridorHeight - 0.3f) + dir * 8f, new Vector3(5f, 0.5f, 5f));
                }
            }
        }
    }
}
