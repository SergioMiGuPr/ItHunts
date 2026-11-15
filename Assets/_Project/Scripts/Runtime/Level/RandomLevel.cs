using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Enemies;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ItHunts.Level
{
    /// <summary>
    /// Carte aléatoire (étape 8) : au chargement de la scène, avant tous les autres systèmes, tire le plan (MapPlanner, seed de la
    /// run et numéro du niveau) et construit la carte avec les modules du catalogue et les pièces du kit : coques des salles
    /// (entrées inutilisées fermées), couloirs, pentes de liaison en escaliers (collision lisse), pièces annexes et salles
    /// verrouillées, repaire du traqueur, sortie et arène du boss (prefab), départ et marchand, coffres, habitants, points
    /// d'apparition, volumes de salle et de tronçon, lumières. Puis graphe du labyrinthe, bornes du NavMesh (construit ensuite par
    /// ZoneNavMesh), départ des joueurs et repaire du traqueur. Tout est déterministe à partir de la seed (jeu en ligne).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class RandomLevel : MonoBehaviour
    {
        private const float T = 1f;                       // épaisseur des murs
        private const float ExitDoorWidth = 10f, ExitDoorHeight = 10f, AnnexDoor = 3f;

        [SerializeField] private MapLayoutSettings settings;
        [SerializeField] private ModuleCatalog catalog;
        [SerializeField] private KitSet kit;

        [Header("Prefabs (pré-câblés par l'éditeur)")]
        [SerializeField] private GameObject exitArena;
        [SerializeField] private GameObject merchant;
        [SerializeField] private GameObject chest;
        [SerializeField] private GameObject ghoul;
        [SerializeField] private GameObject flyer;
        [SerializeField] private GameObject annex;
        [SerializeField] private GameObject lockedAnnex;
        [SerializeField] private Material water;

        [Header("Scène")]
        [SerializeField] private ZoneNavMesh zoneNav;
        [SerializeField] private MazeGraph graph;
        [SerializeField] private Transform playerStart;
        [SerializeField] private Transform stalker;

        [Header("Seed")]
        [Tooltip("0 : seed de la run (et numéro du niveau). Sinon cette seed (carte rejouable, tests). Ligne de commande : -ithunts-seed N.")]
        [SerializeField] private int fixedSeed;

        /// <summary>Seed imposée (mesures) : prioritaire si non nulle.</summary>
        public static int SeedOverride;
        /// <summary>Poids imposé d'un module (mesures : faire tirer les modèles de grande salle) ; null : son poids.</summary>
        public static System.Func<RoomModule, float> WeightOverride;
        public static RandomLevel Current { get; private set; }

        public MapPlan Plan { get; private set; }
        public int LevelSeed { get; private set; }
        public float PlanMs { get; private set; }
        public float BuildMs { get; private set; }
        public int KitPieces { get; private set; }
        public Transform Root => _root;
        public MapLayoutSettings Settings => settings;
        public IReadOnlyList<RoomModule> Modules => _modules;

        private Transform _root;
        private System.Random _rng;
        private List<RoomModule> _modules = new List<RoomModule>();
        private readonly Dictionary<int, GameObject> _moduleInstances = new Dictionary<int, GameObject>();
        private Vector3 _min, _max;
        private Vector3 _arenaCenter, _arenaHalf;

        private void Awake()
        {
            Current = this;
            Generate(ChooseSeed());
        }

        /// <summary>Seed de la carte : imposée, fixée, en ligne de commande, sinon seed de la run mêlée au numéro du niveau.</summary>
        public int ChooseSeed()
        {
            if (SeedOverride != 0)
                return SeedOverride;
            if (fixedSeed != 0)
                return fixedSeed;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-ithunts-seed" && int.TryParse(args[i + 1], out int s))
                    return s;
            RunState run = FindFirstObjectByType<RunState>();
            int runSeed = 1;
            if (run != null)
            {
                run.EnsureStarted();
                runSeed = run.Seed;
            }

            return unchecked(runSeed * 73856093 ^ (RunSession.LevelIndex + 1) * 19349663);
        }

        /// <summary>Tire et construit la carte (remplace la précédente).</summary>
        public void Generate(int seed)
        {
            if (_root != null)
            {
                // Nouvelle carte dans la même scène (mesures) : libère aussi les maillages combinés du batching statique.
                DestroyImmediate(_root.gameObject);
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
            LevelSeed = seed;
            float Weight(RoomModule m) => WeightOverride != null ? WeightOverride(m) : m.weight;
            _modules = catalog != null ? catalog.modules.Where(m => m != null && m.validated && Weight(m) > 0f).ToList() : new List<RoomModule>();
            var infos = _modules.Select((m, i) => ModuleInfo.From(m, i)).ToList();
            foreach (ModuleInfo info in infos)
                info.Weight = Weight(_modules[info.Index]);
            var sw = Stopwatch.StartNew();
            Plan = MapPlanner.Plan(settings, infos, seed);
            PlanMs = (float)sw.Elapsed.TotalMilliseconds;
            if (Plan.Failures.Count > 0)
                Debug.LogWarning($"Carte aléatoire (seed {seed}) : {string.Join(" ; ", Plan.Failures)}");

            sw.Restart();
            KitBuild.PieceCount = 0;
            _rng = new System.Random(unchecked(seed ^ 0x5bd1e995));
            _root = new GameObject($"Niveau (carte aléatoire, seed {seed})").transform;
            _moduleInstances.Clear();
            _min = new Vector3(float.MaxValue, -10f, float.MaxValue);
            _max = new Vector3(float.MinValue, settings.upperLevel + settings.hallCeiling + 10f, float.MinValue);
            for (int i = 0; i < Plan.Rooms.Count; i++)
                BuildRoom(i);
            for (int l = 0; l < Plan.Links.Count; l++)
                BuildCorridor(l);
            int lairNode = BuildLair();
            int arenaNode = BuildExit();
            BuildStart();
            Chests();
            Residents();
            ConfigureScene(lairNode, arenaNode);
            KitPieces = KitBuild.PieceCount;
            Batch();
            Physics.SyncTransforms();
            BuildMs = (float)sw.Elapsed.TotalMilliseconds;
            Debug.Log($"Carte aléatoire : seed {seed}, essai {Plan.Attempt}{(Plan.Fallback ? " (secours)" : "")}, {Plan.Rooms.Count} salles, " +
                      $"{Plan.Links.Count} couloirs, départ – sortie {Plan.StartExitRooms} salles ; tirage {PlanMs:0.0} ms, construction {BuildMs:0} ms, {KitPieces} pièces du kit");
        }

        // ---------- Salles ----------

        private struct Cut
        {
            public float A, B, Y0, Y1;
            public Cut(float a, float b, float y0, float y1)
            {
                A = a;
                B = b;
                Y0 = y0;
                Y1 = y1;
            }
        }

        private void BuildRoom(int i)
        {
            PlanRoom r = Plan.Rooms[i];
            var parent = new GameObject(r.Name).transform;
            parent.SetParent(_root, false);
            Vector3 c = r.Center;
            float hx = r.Extent.x * 0.5f, hz = r.Extent.y * 0.5f, y = c.y, top = y + r.Ceiling;
            Encapsulate(new Vector3(c.x - hx - 30f, 0f, c.z - hz - 30f), new Vector3(c.x + hx + 30f, 0f, c.z + hz + 30f));
            RoomModule mod = r.Module >= 0 ? _modules[r.Module] : null;

            // Sol (canal, fosse, bloc plein pour le niveau haut, dalle), plafond.
            BuildFloor(parent, r, mod, settings, kit, water);
            KitBuild.Box(parent, "Plafond", new Vector3(c.x, top + 0.5f, c.z), new Vector3(2f * hx + 2f * T, 1f, 2f * hz + 2f * T), KitBuild.Kind.Sol, kit, walkable: false);

            // Murs percés des entrées utilisées ; les autres restent fermées (mur du kit).
            var cuts = new Dictionary<DoorSide, List<Cut>>();
            foreach (DoorSide s in new[] { DoorSide.Nord, DoorSide.Est, DoorSide.Sud, DoorSide.Ouest })
                cuts[s] = new List<Cut>();
            foreach (PlanDoor d in r.Doors)
            {
                float along = d.Side == DoorSide.Nord || d.Side == DoorSide.Sud ? d.Position.x : d.Position.z;
                float w = d.Use switch
                {
                    1 => Plan.Links[d.Link].Width,
                    2 => ExitDoorWidth,
                    3 => settings.lairWidth,
                    _ => 0f,
                };
                if (w <= 0f)
                    continue;
                float h = d.Use == 2 ? ExitDoorHeight : settings.corridorHeight;
                cuts[d.Side].Add(new Cut(along - w * 0.5f, along + w * 0.5f, y, y + h));
                if (d.Use == 1 && kit != null)
                    KitBuild.Piece(kit.arc, parent, d.Position + MapPlanner.SideVector(d.Side) * (T + 0.8f), RoomModule.SideYaw(d.Side));
            }

            Wall(parent, "Mur nord", true, c.z + hz + T * 0.5f, c.x - hx - T, c.x + hx + T, 0f, top, cuts[DoorSide.Nord]);
            Wall(parent, "Mur sud", true, c.z - hz - T * 0.5f, c.x - hx - T, c.x + hx + T, 0f, top, cuts[DoorSide.Sud]);
            Wall(parent, "Mur est", false, c.x + hx + T * 0.5f, c.z - hz, c.z + hz, 0f, top, cuts[DoorSide.Est]);
            Wall(parent, "Mur ouest", false, c.x - hx - T * 0.5f, c.z - hz, c.z + hz, 0f, top, cuts[DoorSide.Ouest]);

            Volume(parent, r.Name, new Vector3(c.x, y + r.Ceiling * 0.5f, c.z), new Vector3(2f * hx, r.Ceiling, 2f * hz), i == Plan.Start ? RoomKind.Depart : RoomKind.Salle);
            if (!r.Hall)
                Lamp(parent, c + Vector3.up * (r.Ceiling - 2f), new Color(1f, 0.8f, 0.6f), 5f, 35f);
            else
            {
                int grid = mod != null ? 2 : 3;
                for (int a = 0; a < grid; a++)
                for (int b = 0; b < grid; b++)
                {
                    float fx = grid == 3 ? (a - 1) * 0.6f : (a - 0.5f) * 0.9f, fz = grid == 3 ? (b - 1) * 0.6f : (b - 0.5f) * 0.9f;
                    Lamp(parent, c + new Vector3(fx * hx, r.Ceiling * 0.75f, fz * hz), (a + b) % 2 == 0 ? new Color(1f, 0.78f, 0.55f) : new Color(0.75f, 0.82f, 1f),
                        settings.lightIntensity, settings.lightRange);
                }
            }

            if (mod != null)
            {
                GameObject inst = Instantiate(mod.gameObject, c, Quaternion.Euler(0f, r.Rot * 90f, 0f), parent);
                inst.name = $"Module {mod.displayName}";
                _moduleInstances[i] = inst;
            }
        }

        /// <summary>
        /// Sol d'une salle (canal, fosse, bloc plein pour le niveau haut, dalle) : le même pour le générateur et pour la validation
        /// des modules (ItHunts > Valider le module).
        /// </summary>
        public static void BuildFloor(Transform parent, PlanRoom r, RoomModule mod, MapLayoutSettings settings, KitSet kit, Material water)
        {
            Vector3 c = r.Center;
            float hx = r.Extent.x * 0.5f, hz = r.Extent.y * 0.5f, y = c.y;
            if (r.Hole == ModuleHole.Canal && mod != null)
                CanalFloor(parent, r, mod.holeRect.center.y, mod.holeRect.height, settings, kit, water);
            else if (r.Hole == ModuleHole.Vide && mod != null)
                PitFloor(parent, r, mod, settings, kit);
            else if (y > 0.1f)
                KitBuild.Box(parent, "Sol (bloc)", new Vector3(c.x, y * 0.5f, c.z), new Vector3(2f * hx, y, 2f * hz), KitBuild.Kind.Bloc, kit);
            else
                KitBuild.Box(parent, "Sol", new Vector3(c.x, -0.5f, c.z), new Vector3(2f * hx + 2f * T, 1f, 2f * hz + 2f * T), KitBuild.Kind.Sol, kit);
        }

        /// <summary>Mur droit percé (allège praticable sous une ouverture, linteau au-dessus), en pièces du kit.</summary>
        private void Wall(Transform parent, string name, bool alongX, float fixedCoord, float from, float to, float yb, float yt, List<Cut> cuts)
        {
            cuts = cuts.OrderBy(o => o.A).ToList();
            float cursor = from;
            int n = 0;
            void Block(float a, float b, float y0, float y1, string label, bool walkable)
            {
                if (b - a < 0.01f || y1 - y0 < 0.01f)
                    return;
                Vector3 center = alongX ? new Vector3((a + b) * 0.5f, (y0 + y1) * 0.5f, fixedCoord) : new Vector3(fixedCoord, (y0 + y1) * 0.5f, (a + b) * 0.5f);
                Vector3 size = alongX ? new Vector3(b - a, y1 - y0, T) : new Vector3(T, y1 - y0, b - a);
                KitBuild.Box(parent, $"{name} {++n}{label}", center, size, KitBuild.Kind.Mur, kit, walkable);
            }

            foreach (Cut o in cuts)
            {
                if (o.A > cursor)
                    Block(cursor, o.A, yb, yt, "", false);
                Block(Mathf.Max(o.A, cursor), o.B, o.Y1, yt, " (linteau)", false);
                Block(Mathf.Max(o.A, cursor), o.B, yb, o.Y0, " (allège)", true);
                cursor = Mathf.Max(cursor, o.B);
            }

            if (cursor < to)
                Block(cursor, to, yb, yt, "", false);
        }

        private static void CanalFloor(Transform parent, PlanRoom r, float zc, float w, MapLayoutSettings settings, KitSet kit, Material water)
        {
            float hx = r.Extent.x * 0.5f, hz = r.Extent.y * 0.5f, y = r.Center.y, d = settings.canalDepth;
            Vector3 c = r.Center;
            float za = zc - w * 0.5f, zb = zc + w * 0.5f, south = za + hz + T, north = hz + T - zb;
            if (y > 0.1f)
            {
                KitBuild.Box(parent, "Sol sud (bloc)", new Vector3(c.x, y * 0.5f, c.z + (-hz - T + za) * 0.5f), new Vector3(2f * hx, y, south), KitBuild.Kind.Bloc, kit);
                KitBuild.Box(parent, "Sol nord (bloc)", new Vector3(c.x, y * 0.5f, c.z + (zb + hz + T) * 0.5f), new Vector3(2f * hx, y, north), KitBuild.Kind.Bloc, kit);
                KitBuild.Box(parent, "Fond du canal", new Vector3(c.x, (y - d) * 0.5f, c.z + zc), new Vector3(2f * hx, y - d, w), KitBuild.Kind.Bloc, kit, walkable: false);
            }
            else
            {
                KitBuild.Box(parent, "Sol sud", new Vector3(c.x, -0.5f, c.z + (-hz - T + za) * 0.5f), new Vector3(2f * hx + 2f * T, 1f, south), KitBuild.Kind.Sol, kit);
                KitBuild.Box(parent, "Sol nord", new Vector3(c.x, -0.5f, c.z + (zb + hz + T) * 0.5f), new Vector3(2f * hx + 2f * T, 1f, north), KitBuild.Kind.Sol, kit);
                KitBuild.Box(parent, "Fond du canal", new Vector3(c.x, -d - 0.5f, c.z + zc), new Vector3(2f * hx + 2f * T, 1f, w + 2f), KitBuild.Kind.Sol, kit, walkable: false);
                KitBuild.Box(parent, "Bord sud du canal", new Vector3(c.x, -(d + 1f) * 0.5f, c.z + za - 0.5f), new Vector3(2f * hx + 2f * T, d - 1f, 1f), KitBuild.Kind.Mur, kit, walkable: false);
                KitBuild.Box(parent, "Bord nord du canal", new Vector3(c.x, -(d + 1f) * 0.5f, c.z + zb + 0.5f), new Vector3(2f * hx + 2f * T, d - 1f, 1f), KitBuild.Kind.Mur, kit, walkable: false);
            }

            // Eau (surface à 1.2 m sous le sol), déclencheur (on remonte sur la rive), ponts d'égout du kit (grand dans l'axe nord).
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "Eau du canal";
            DestroyImmediate(surface.GetComponent<Collider>());
            surface.transform.SetParent(parent, false);
            surface.transform.position = new Vector3(c.x, y - 1.2f, c.z + zc);
            surface.transform.localScale = new Vector3(2f * hx, 0.05f, w);
            if (water != null)
                surface.GetComponent<MeshRenderer>().sharedMaterial = water;
            var wet = new GameObject("Eau profonde (vide)");
            wet.transform.SetParent(parent, false);
            float topW = y - 0.8f, bottom = y - d;
            wet.transform.position = new Vector3(c.x, (topW + bottom) * 0.5f, c.z + zc);
            var box = wet.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f * hx, topW - bottom, w);
            wet.AddComponent<DeepWater>().Configure(settings.waterDamage, w * 0.5f + 1.5f, y + 0.1f - wet.transform.position.y);
            wet.AddComponent<NotWalkable>();
            KitBuild.Box(parent, "Pont du canal (grand)", new Vector3(c.x, y - 0.5f, c.z + zc), new Vector3(settings.corridorWidth, 1f, w + 2f), KitBuild.Kind.Sol, null);
            KitBuild.Box(parent, "Pont du canal (petit)", new Vector3(c.x + hx * 0.65f, y - 0.5f, c.z + zc), new Vector3(4f, 1f, w + 2f), KitBuild.Kind.Sol, null);
            if (kit != null)
            {
                KitBuild.Piece(kit.pontEgout14, parent, new Vector3(c.x, y, c.z + zc), 90f);
                KitBuild.Piece(kit.pontEgout4, parent, new Vector3(c.x + hx * 0.65f, y, c.z + zc), 90f);
            }

            Lamp(parent, new Vector3(c.x - hx * 0.5f, y + 4f, c.z + zc), new Color(0.3f, 0.7f, 0.8f), 3f, 25f);
        }

        private static void PitFloor(Transform parent, PlanRoom r, RoomModule mod, MapLayoutSettings settings, KitSet kit)
        {
            float hx = r.Extent.x * 0.5f, hz = r.Extent.y * 0.5f, y = r.Center.y, d = mod.holeDepth;
            Vector3 c = r.Center;
            // Trou du module, tourné avec lui (rectangle aligné sur les axes du monde).
            Rect lr = mod.holeRect;
            Vector3 p0 = MapPlanner.Rotate(new Vector3(lr.xMin, 0f, lr.yMin), r.Rot), p1 = MapPlanner.Rotate(new Vector3(lr.xMax, 0f, lr.yMax), r.Rot);
            float x0 = Mathf.Min(p0.x, p1.x), x1 = Mathf.Max(p0.x, p1.x), za = Mathf.Min(p0.z, p1.z), zb = Mathf.Max(p0.z, p1.z);
            void Slab(string nm, float xa, float xb, float zq, float zr)
            {
                if (xb - xa < 0.01f || zr - zq < 0.01f)
                    return;
                if (y > 0.1f)
                    KitBuild.Box(parent, $"Sol {nm} (bloc)", new Vector3(c.x + (xa + xb) * 0.5f, y * 0.5f, c.z + (zq + zr) * 0.5f), new Vector3(xb - xa, y, zr - zq), KitBuild.Kind.Bloc, kit);
                else
                    KitBuild.Box(parent, $"Sol {nm}", new Vector3(c.x + (xa + xb) * 0.5f, -0.5f, c.z + (zq + zr) * 0.5f), new Vector3(xb - xa, 1f, zr - zq), KitBuild.Kind.Sol, kit);
            }

            Slab("ouest", -hx - T, x0, -hz - T, hz + T);
            Slab("est", x1, hx + T, -hz - T, hz + T);
            Slab("sud", x0, x1, -hz - T, za);
            Slab("nord", x0, x1, zb, hz + T);
            float bottom = y - d, cx = (x0 + x1) * 0.5f, cz = (za + zb) * 0.5f, wx = x1 - x0, wz = zb - za;
            KitBuild.Box(parent, "Fond de la fosse", new Vector3(c.x + cx, bottom - 0.5f, c.z + cz), new Vector3(wx, 1f, wz), KitBuild.Kind.Sol, kit, walkable: false);
            KitBuild.Box(parent, "Bord ouest de la fosse", new Vector3(c.x + x0 - 0.5f, (bottom + y - 1f) * 0.5f, c.z + cz), new Vector3(1f, d - 1f, wz + 2f), KitBuild.Kind.Mur, kit, walkable: false);
            KitBuild.Box(parent, "Bord est de la fosse", new Vector3(c.x + x1 + 0.5f, (bottom + y - 1f) * 0.5f, c.z + cz), new Vector3(1f, d - 1f, wz + 2f), KitBuild.Kind.Mur, kit, walkable: false);
            KitBuild.Box(parent, "Bord sud de la fosse", new Vector3(c.x + cx, (bottom + y - 1f) * 0.5f, c.z + za - 0.5f), new Vector3(wx, d - 1f, 1f), KitBuild.Kind.Mur, kit, walkable: false);
            KitBuild.Box(parent, "Bord nord de la fosse", new Vector3(c.x + cx, (bottom + y - 1f) * 0.5f, c.z + zb + 0.5f), new Vector3(wx, d - 1f, 1f), KitBuild.Kind.Mur, kit, walkable: false);
            var trig = new GameObject("Vide de la fosse");
            trig.transform.SetParent(parent, false);
            trig.transform.position = new Vector3(c.x + cx, (bottom + y - 1.5f) * 0.5f, c.z + cz);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(wx, y - 1.5f - bottom, wz);
            var respawns = new List<Transform>();
            foreach (Vector3 p in new[] { new Vector3(x0 - 2.5f, 0f, cz), new Vector3(x1 + 2.5f, 0f, cz), new Vector3(cx, 0f, za - 2.5f), new Vector3(cx, 0f, zb + 2.5f) })
            {
                var t = new GameObject("Reprise").transform;
                t.SetParent(trig.transform, false);
                t.position = c + p + Vector3.up * 0.2f;
                respawns.Add(t);
            }

            trig.AddComponent<FallVoid>().Configure(settings.waterDamage, respawns.ToArray());
            trig.AddComponent<NotWalkable>();
        }

        // ---------- Couloirs ----------

        private void BuildCorridor(int li)
        {
            PlanLink l = Plan.Links[li];
            PlanRoom a = Plan.Rooms[l.A], b = Plan.Rooms[l.B];
            string name = $"Couloir {a.Name.Split(' ')[0]} – {b.Name.Split(' ')[0]}";
            var parent = new GameObject(name).transform;
            parent.SetParent(_root, false);
            Vector3 start = l.From, end = l.To;
            Vector3 dir = end - start;
            dir.y = 0f;
            float len = dir.magnitude;
            dir /= Mathf.Max(0.001f, len);
            bool alongX = Mathf.Abs(dir.x) > 0.5f;
            Vector3 lateral = new Vector3(-dir.z, 0f, dir.x);
            float w = l.Width, h = settings.corridorHeight, ya = start.y, yb = end.y;
            Vector3 flatStart = new Vector3(start.x, 0f, start.z), flatEnd = new Vector3(end.x, 0f, end.z);
            Vector3 mid = (flatStart + flatEnd) * 0.5f;
            Vector3 Point(float t, float side = 0f) => Vector3.Lerp(flatStart, flatEnd, t) + Vector3.up * Mathf.Lerp(ya, yb, t) + lateral * side;

            if (!l.Slope)
            {
                Vector3 along = alongX ? new Vector3(len, 1f, w) : new Vector3(w, 1f, len);
                if (ya > 0.1f)
                    KitBuild.Box(parent, "Sol (bloc)", mid + Vector3.up * (ya * 0.5f), alongX ? new Vector3(len, ya, w) : new Vector3(w, ya, len), KitBuild.Kind.Bloc, kit);
                else
                    KitBuild.Box(parent, "Sol", mid + Vector3.down * 0.5f, along, KitBuild.Kind.Sol, kit);
                KitBuild.Box(parent, "Plafond", mid + Vector3.up * (ya + h + 0.5f), along + (alongX ? new Vector3(0f, 0f, 2f * T) : new Vector3(2f * T, 0f, 0f)),
                    KitBuild.Kind.Sol, kit, walkable: false);
            }
            else
            {
                // Pente de liaison : collision lisse sur toute la largeur, visuel en marches ; plafond parallèle.
                bool up = yb > ya;
                Vector3 low = up ? flatStart : flatEnd, rise = up ? dir : -dir;
                float dy = Mathf.Abs(yb - ya), ang = Mathf.Atan2(dy, len) * Mathf.Rad2Deg;
                KitBuild.Ramp(parent, $"Pente ({len:0} m, {ang:0.0}°)", low + Vector3.up * Mathf.Min(ya, yb), rise, len, dy, w, kit, dy + 1f);
                float sl = Mathf.Sqrt(len * len + dy * dy);
                GameObject ceil = KitBuild.Box(parent, "Plafond", Vector3.zero, new Vector3(w + 2f * T, 1f, sl), KitBuild.Kind.Sol, kit, walkable: false);
                ceil.transform.localPosition = mid + Vector3.up * ((ya + yb) * 0.5f + h + 0.5f);
                ceil.transform.localRotation = Quaternion.LookRotation(rise, Vector3.up) * Quaternion.Euler(-ang, 0f, 0f);
            }

            // Murs (percés pour les pièces annexes), du sol (ou du bas) au plafond.
            var plus = new List<Cut>();
            var minus = new List<Cut>();
            for (int k = 0; k < Plan.Annexes.Count; k++)
            {
                PlanAnnex an = Plan.Annexes[k];
                if (an.Link != li)
                    continue;
                Vector3 p = Point(an.T);
                float along = alongX ? p.x : p.z;
                (an.Side > 0 ? plus : minus).Add(new Cut(along - AnnexDoor * 0.5f, along + AnnexDoor * 0.5f, p.y, p.y + AnnexDoor));
                BuildAnnex(parent, p, lateral * an.Side, w, an.Locked);
            }

            float yLow = Mathf.Min(ya, yb), yTop = Mathf.Max(ya, yb) + h;
            float from = alongX ? Mathf.Min(flatStart.x, flatEnd.x) : Mathf.Min(flatStart.z, flatEnd.z);
            float to = alongX ? Mathf.Max(flatStart.x, flatEnd.x) : Mathf.Max(flatStart.z, flatEnd.z);
            float sideCoord = alongX ? mid.z : mid.x;
            // Côté +latéral : nord si le couloir va vers +x (latéral = (−dz, 0, dx)).
            float sgn = alongX ? lateral.z : lateral.x;
            Wall(parent, "Mur 1", alongX, sideCoord + sgn * (w * 0.5f + T * 0.5f), from, to, yLow > 0.1f ? 0f : yLow, yTop, plus);
            Wall(parent, "Mur 2", alongX, sideCoord - sgn * (w * 0.5f + T * 0.5f), from, to, yLow > 0.1f ? 0f : yLow, yTop, minus);

            // Tronçons (volumes, D-047), lumières, arcs et torches, apparitions.
            int segments = Mathf.Max(1, Mathf.CeilToInt(len / settings.corridorSegmentLength - 0.05f));
            for (int i = 0; i < segments; i++)
            {
                Vector3 p0 = Point((float)i / segments), p1 = Point((i + 1f) / segments);
                float y0 = Mathf.Min(p0.y, p1.y), y1 = Mathf.Max(p0.y, p1.y) + h;
                Vector3 cc = (p0 + p1) * 0.5f;
                float seg = len / segments;
                Volume(parent, $"{name}, tronçon {i + 1}/{segments}", new Vector3(cc.x, (y0 + y1) * 0.5f, cc.z),
                    alongX ? new Vector3(seg, y1 - y0, w) : new Vector3(w, y1 - y0, seg), RoomKind.Couloir);
            }

            int lamps = Mathf.Max(1, Mathf.RoundToInt(len / 25f));
            for (int i = 0; i < lamps; i++)
                Lamp(parent, Point((i + 0.5f) / lamps) + Vector3.up * (h - 2f), i % 2 == 0 ? new Color(1f, 0.75f, 0.5f) : new Color(0.8f, 0.85f, 1f), 5f, 40f);
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            if (kit != null && !l.Slope)
                for (float d = 20f; d < len - 10f; d += 20f)
                    KitBuild.Piece(kit.arc, parent, Point(d / len), yaw);
            if (kit != null && kit.torche != null)
                for (float d = 6f; d < len - 4f; d += 12f)
                    foreach (int s in new[] { 1, -1 })
                    {
                        Vector3 p = Point(d / len, s * (w * 0.5f - 0.3f)) + Vector3.up * 4f;
                        KitBuild.Piece(kit.torche, parent, p, Mathf.Atan2(-lateral.x * s, -lateral.z * s) * Mathf.Rad2Deg);
                    }

            for (int i = 0; i < settings.ghoulSpawnsPerCorridor; i++)
            {
                float t = (i + 1f) / (settings.ghoulSpawnsPerCorridor + 1f);
                Spawn(parent, Point(t, (i % 2 == 0 ? 1f : -1f) * 3f), SpawnKinds.Goule);
            }

            for (int i = 0; i < settings.flyerSpawnsPerCorridor; i++)
                Spawn(parent, Point((i + 1f) / (settings.flyerSpawnsPerCorridor + 1f)) + Vector3.up * (h - 5f), SpawnKinds.Volant);

            // Poutre ou pont en hauteur (dessous à 10 m) dans une partie des couloirs plats.
            if (!l.Slope && _rng.NextDouble() < settings.corridorFeatureShare)
            {
                Vector3 p = Point(0.5f) + Vector3.up * (settings.bridgeBottom + 0.5f);
                if (_rng.NextDouble() < 0.5)
                    KitBuild.Box(parent, $"Poutre (dessous à {settings.bridgeBottom:0} m)", p, alongX ? new Vector3(1f, 1f, w) : new Vector3(w, 1f, 1f), KitBuild.Kind.Poutre, kit);
                else
                    KitBuild.Box(parent, $"Pont (dessous à {settings.bridgeBottom:0} m)", p, alongX ? new Vector3(settings.bridgeWidth, 1f, w) : new Vector3(w, 1f, settings.bridgeWidth), KitBuild.Kind.Pont, kit);
            }
        }

        /// <summary>Pièce annexe (prefab : origine sur la face extérieure du mur du couloir, au pied de la porte, +Z vers l'intérieur).</summary>
        private void BuildAnnex(Transform parent, Vector3 floorPoint, Vector3 outward, float corridorWidth, bool locked)
        {
            GameObject prefab = locked && lockedAnnex != null ? lockedAnnex : annex;
            if (prefab == null)
                return;
            Vector3 at = floorPoint + outward * (corridorWidth * 0.5f + T);
            GameObject go = Instantiate(prefab, at, Quaternion.LookRotation(outward, Vector3.up), parent);
            go.name = locked ? "Salle verrouillée" : "Pièce annexe";
        }

        // ---------- Repaire, sortie, départ ----------

        private int BuildLair()
        {
            if (Plan.LairHost < 0)
                return -1;
            PlanRoom host = Plan.Rooms[Plan.LairHost];
            PlanDoor d = host.Doors[Plan.LairDoor];
            Vector3 o = MapPlanner.SideVector(d.Side), lat = new Vector3(-o.z, 0f, o.x);
            float depth = settings.lairDepth, width = settings.lairWidth, h = settings.lairHeight, y = d.Position.y;
            Vector3 c = new Vector3(d.Position.x, y, d.Position.z) + o * (T + depth * 0.5f);
            var parent = new GameObject("Repaire du traqueur").transform;
            parent.SetParent(_root, false);
            bool alongX = Mathf.Abs(o.x) > 0.5f;
            Vector3 Size(float along, float sy, float across) => alongX ? new Vector3(along, sy, across) : new Vector3(across, sy, along);
            if (y > 0.1f)
                KitBuild.Box(parent, "Sol (bloc)", new Vector3(c.x, y * 0.5f, c.z) - o * (T * 0.5f), Size(depth + T, y, width + 2f * T), KitBuild.Kind.Bloc, kit);
            else
                KitBuild.Box(parent, "Sol", new Vector3(c.x, -0.5f, c.z) - o * (T * 0.5f), Size(depth + T, 1f, width + 2f * T), KitBuild.Kind.Sol, kit);
            KitBuild.Box(parent, "Plafond", c + Vector3.up * (h + 0.5f), Size(depth + 2f * T, 1f, width + 2f * T), KitBuild.Kind.Sol, kit, walkable: false);
            KitBuild.Box(parent, "Mur du fond", c + o * (depth * 0.5f + T * 0.5f) + Vector3.up * ((h - y) * 0.5f), Size(T, h + y, width + 2f * T), KitBuild.Kind.Mur, kit, walkable: false);
            foreach (int s in new[] { 1, -1 })
                KitBuild.Box(parent, "Mur", c + lat * s * (width * 0.5f + T * 0.5f) + Vector3.up * ((h - y) * 0.5f), Size(depth, h + y, T), KitBuild.Kind.Mur, kit, walkable: false);
            Volume(parent, "Repaire du traqueur", c + Vector3.up * (h * 0.5f), Size(depth, h, width), RoomKind.Salle);
            Lamp(parent, c + Vector3.up * (h - 3f), new Color(1f, 0.15f, 0.1f), 5f, 30f);
            Encapsulate(c - Vector3.one * (depth + 10f), c + Vector3.one * (depth + 10f));
            if (stalker != null)
            {
                stalker.position = c + o * (depth * 0.15f);
                stalker.rotation = Quaternion.LookRotation(-o, Vector3.up);
            }

            Plan.Rooms.Add(new PlanRoom { Name = "Repaire du traqueur", Module = -2, Center = c, Extent = alongX ? new Vector2(depth, width) : new Vector2(width, depth), Ceiling = h });
            return Plan.Rooms.Count - 1;
        }

        private int BuildExit()
        {
            if (Plan.Exit < 0 || exitArena == null)
                return -1;
            PlanDoor d = Plan.Rooms[Plan.Exit].Doors[Plan.ExitDoor];
            Vector3 o = MapPlanner.SideVector(d.Side);
            GameObject go = Instantiate(exitArena, d.Position + o * T, Quaternion.LookRotation(o, Vector3.up), _root);
            go.name = "Sortie et arène du boss";
            var marker = go.GetComponent<ExitArenaMarker>();
            if (marker == null)
                return -1;
            _arenaCenter = go.transform.TransformPoint(marker.arenaCenter);
            Vector3 he = marker.arenaHalfExtents;
            _arenaHalf = Mathf.Abs(o.x) > 0.5f ? new Vector3(he.z, he.y, he.x) : he;
            Bounds f = marker.footprint;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = f.center + Vector3.Scale(f.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 w = go.transform.TransformPoint(corner);
                Encapsulate(w, w);
                _max.y = Mathf.Max(_max.y, w.y);
            }

            Plan.Rooms.Add(new PlanRoom { Name = "Arène", Module = -3, Center = _arenaCenter, Extent = new Vector2(_arenaHalf.x * 2f, _arenaHalf.z * 2f), Ceiling = _arenaHalf.y * 2f });
            return Plan.Rooms.Count - 1;
        }

        private void BuildStart()
        {
            if (Plan.Start < 0)
                return;
            PlanRoom st = Plan.Rooms[Plan.Start];
            float hx = st.Extent.x * 0.5f, hz = st.Extent.y * 0.5f;
            Vector3 startAt = st.Center + new Vector3(-hx * 0.5f, 0f, -hz * 0.5f);
            Vector3 mAt = st.Center + new Vector3(hx * 0.15f, 0f, hz * 0.2f);
            if (merchant != null)
                Instantiate(merchant, mAt, Quaternion.LookRotation(startAt - mAt, Vector3.up), _root).name = "Marchand de l'artefact (salle de départ)";
            if (playerStart != null)
                playerStart.SetPositionAndRotation(startAt, Quaternion.Euler(0f, 45f, 0f));
            foreach (Player.PlayerHealth p in FindObjectsByType<Player.PlayerHealth>(FindObjectsSortMode.None))
                p.transform.SetPositionAndRotation(startAt, Quaternion.Euler(0f, 45f, 0f));
        }

        private void Chests()
        {
            if (chest == null)
                return;
            foreach (int r in Plan.ChestRooms)
            {
                if (!_moduleInstances.TryGetValue(r, out GameObject inst))
                    continue;
                ChestSpot[] spots = inst.GetComponentsInChildren<ChestSpot>();
                if (spots.Length == 0)
                    continue;
                ChestSpot s = spots[_rng.Next(spots.Length)];
                Instantiate(chest, s.transform.position, s.transform.rotation, _root).name = $"Coffre : {Plan.Rooms[r].Name}";
            }
        }

        private void Residents()
        {
            var parent = new GameObject("Habitants des salles").transform;
            parent.SetParent(_root, false);
            for (int i = 0; i < Plan.Rooms.Count; i++)
            {
                PlanRoom r = Plan.Rooms[i];
                if (!r.Hall || i == Plan.Start || r.Module < 0 || !_moduleInstances.TryGetValue(i, out GameObject inst))
                    continue;
                var spots = inst.GetComponentsInChildren<ResidentSpot>().Where(x => !x.flyer).Select(x => x.transform.position).ToList();
                if (spots.Count == 0)
                    spots.Add(r.Center);
                float t = Mathf.InverseLerp(46f, 66f, Mathf.Min(r.Extent.x, r.Extent.y));
                int g = Mathf.RoundToInt(Mathf.Lerp(settings.residentGhoulsMin, settings.residentGhoulsMax, t));
                for (int k = 0; k < g && ghoul != null; k++)
                    Instantiate(ghoul, spots[k % spots.Count], Quaternion.Euler(0f, _rng.Next(360), 0f), parent).name = $"Goule habitante, {r.Name} {k + 1}";
                if (r.Center.y > 0.1f && flyer != null)
                {
                    int f = _rng.Next(settings.residentFlyersMin, settings.residentFlyersMax + 1);
                    for (int k = 0; k < f; k++)
                        Instantiate(flyer, r.Center + new Vector3((k - 0.5f) * r.Extent.x * 0.25f, 12f, r.Extent.y * 0.1f), Quaternion.identity, parent).name = $"Volant habitant, {r.Name} {k + 1}";
                }
            }
        }

        // ---------- Scène ----------

        private void ConfigureScene(int lairNode, int arenaNode)
        {
            int n = Plan.Rooms.Count;
            var names = new string[n];
            var centers = new Vector3[n];
            var halves = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                PlanRoom r = Plan.Rooms[i];
                names[i] = r.Name;
                centers[i] = r.Center;
                halves[i] = new Vector3(r.Extent.x * 0.5f, r.Ceiling * 0.5f, r.Extent.y * 0.5f);
            }

            var edges = Plan.Links.Select(l => new Vector2Int(l.A, l.B)).ToList();
            if (lairNode >= 0)
                edges.Add(new Vector2Int(Plan.LairHost, lairNode));
            if (arenaNode >= 0)
                edges.Add(new Vector2Int(Plan.Exit, arenaNode));
            if (graph != null)
                graph.Configure(names, centers, halves, edges.ToArray());
            if (zoneNav != null)
            {
                Vector3 min = new Vector3(_min.x, -10f, _min.z), max = new Vector3(_max.x, _max.y, _max.z);
                zoneNav.Configure((min + max) * 0.5f, max - min);
            }
        }

        /// <summary>Regroupe les pièces du kit (visuel seulement) : batching statique à l'exécution.</summary>
        private void Batch()
        {
            var list = new List<GameObject>();
            foreach (MeshRenderer r in _root.GetComponentsInChildren<MeshRenderer>())
            {
                Transform t = r.transform;
                while (t != null && t != _root && !t.name.StartsWith("Kit_"))
                    t = t.parent;
                if (t != null && t != _root)
                    list.Add(r.gameObject);
            }

            if (list.Count > 0)
                StaticBatchingUtility.Combine(list.ToArray(), _root.gameObject);
        }

        // ---------- Outils ----------

        private void Encapsulate(Vector3 a, Vector3 b)
        {
            _min = Vector3.Min(_min, Vector3.Min(a, b));
            _max = new Vector3(Mathf.Max(_max.x, Mathf.Max(a.x, b.x)), _max.y, Mathf.Max(_max.z, Mathf.Max(a.z, b.z)));
        }

        private static void Volume(Transform parent, string name, Vector3 center, Vector3 size, RoomKind kind)
        {
            var go = new GameObject($"Volume de salle : {name}");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<RoomVolume>().Configure(name, size, kind);
        }

        private static void Lamp(Transform parent, Vector3 at, Color color, float intensity, float range)
        {
            var go = new GameObject("Lumière");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
            l.shadows = LightShadows.None;
        }

        private static void Spawn(Transform parent, Vector3 at, SpawnKinds kinds)
        {
            var go = new GameObject(kinds == SpawnKinds.Volant ? "Apparition : volant" : "Apparition : goule");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.AddComponent<SpawnPoint>().Configure(kinds);
        }
    }
}
