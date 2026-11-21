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
        /// <summary>Décalage des modèles posés sur une surface du kit (pont d'égout) : pas deux faces au même endroit.</summary>
        public const float BridgeLift = 0.02f;
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
        [Tooltip("Éclaboussure de la DA (VFX_Eau_Eclaboussure, PR #24).")]
        [SerializeField] private GameObject waterSplash;

        /// <summary>Éclaboussure posée sur les eaux construites (canal, fosse).</summary>
        public static GameObject Splash { get; set; }
        [Tooltip("Matériau des surfaces non magnétiques (galeries de parcours, passages en hauteur).")]
        [SerializeField] private Material nonMagnetic;
        [Tooltip("Réglages de mouvement du joueur : les écarts des parcours sont calculés depuis ses capacités (sans traction).")]
        [SerializeField] private ItHunts.Settings.MovementSettings movement;

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
        /// <summary>
        /// Salle de test d'un module (menu Playtest, point 2 du 2026-11-21) : nom du module (displayName) posé juste à côté de la salle
        /// de départ (poids renforcé, première seed qui le relie au départ) ; le reste de la carte et le traqueur sont ceux du jeu.
        /// </summary>
        public static string TestModule;
        /// <summary>Poids du module de test (multiplie le sien) et nombre de seeds essayées pour le poser contre le départ.</summary>
        public const float TestModuleWeight = 8f;
        /// <summary>
        /// Module de l'atelier à tester (« Jouer ce module ») : ajouté au tirage même s'il n'est pas encore validé ni au catalogue, et le
        /// joueur part dans sa salle. Posé par l'atelier (éditeur) ou le menu Playtest.
        /// </summary>
        public static RoomModule TestModulePrefab;
        /// <summary>Le joueur commence dans la salle du module de test (au lieu de la salle de départ).</summary>
        public static bool TestStartInModule;

#if UNITY_EDITOR
        /// <summary>« Jouer ce module » depuis l'atelier : le choix survit au rechargement du Play Mode (SessionState).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AtelierTestFromEditor()
        {
            string path = UnityEditor.SessionState.GetString("ItHunts.Atelier.TestModule", "");
            if (string.IsNullOrEmpty(path))
                return;
            UnityEditor.SessionState.EraseString("ItHunts.Atelier.TestModule");
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
            TestModulePrefab = prefab != null ? prefab.GetComponent<RoomModule>() : null;
            if (TestModulePrefab != null)
            {
                TestModule = TestModulePrefab.displayName;
                TestStartInModule = true;
                SeedOverride = UnityEditor.SessionState.GetInt("ItHunts.Atelier.TestSeed", 1);
            }
        }
#endif
        public const int TestModuleTries = 400;
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
        private Transform _residents;
        private readonly Dictionary<(int, DoorSide), List<Cut>> _roomCuts = new Dictionary<(int, DoorSide), List<Cut>>();
        private readonly Dictionary<(int, int), List<Cut>> _corridorCuts = new Dictionary<(int, int), List<Cut>>();
        private const float DuctY = 4.5f, DuctH = 3f, DuctW = 3.5f, DuctLen = 8f, GalleryWidth = 20f;

        /// <summary>Passages refermés (un étage du module bouchait l'ouverture) : indices dans Plan.Passages (mesures).</summary>
        public readonly HashSet<int> ClosedPassages = new HashSet<int>();

        [Header("Chargement")]
        [Tooltip("Racine des systèmes de jeu (zone, vagues, traqueur, joueur, NavMesh…), inactive dans la scène : activée quand la " +
                 "carte est finie. Vide : la carte est construite d'un coup dans Awake (image figée).")]
        [SerializeField] private GameObject gameplayRoot;
        [Tooltip("Temps de construction par image de l'écran de chargement (ms) : plus, chargement plus court mais barre moins fluide.")]
        [Min(5f)] [SerializeField] private float frameBudgetMs = 50f;

        /// <summary>Avancement du chargement (0..1) et étape en cours (écran de chargement).</summary>
        public float Progress { get; private set; }
        public string Stage { get; private set; } = "";
        public bool Loading { get; private set; }
        /// <summary>Durée totale du chargement (de l'Awake au jeu actif, NavMesh compris), en secondes réelles.</summary>
        public float LoadSeconds { get; private set; }

        private void Awake()
        {
            Current = this;
            if (gameplayRoot != null && Application.isPlaying && !gameplayRoot.activeSelf)
                StartCoroutine(LoadRoutine(ChooseSeed()));
            else
                Generate(ChooseSeed());
        }

        /// <summary>
        /// Chargement avec écran : la carte est construite sur plusieurs images (texte et barre de progression), puis le NavMesh
        /// (ZoneNavMesh, à l'activation) et les systèmes de jeu démarrent sur la carte finie.
        /// </summary>
        private System.Collections.IEnumerator LoadRoutine(int seed)
        {
            float t0 = Time.realtimeSinceStartup;
            Loading = true;
            var screen = new GameObject("Écran de chargement").AddComponent<LoadingScreen>();
            screen.Level = this;
            yield return null;
            System.Collections.IEnumerator build = Build(seed, true);
            while (build.MoveNext())
                yield return null;
            Stage = "Préparation des ennemis (NavMesh)";
            Progress = 0.92f;
            yield return null;
            yield return null;
            gameplayRoot.SetActive(true);                                     // ZoneNavMesh se construit ici, puis tous les systèmes
            if (_residents != null)
                _residents.gameObject.SetActive(true);                        // après le graphe et le NavMesh
            Progress = 1f;
            Loading = false;
            LoadSeconds = Time.realtimeSinceStartup - t0;
            Debug.Log($"Chargement de la carte : {LoadSeconds:0.0} s (construction {BuildMs / 1000f:0.0} s)");
            Destroy(screen.gameObject);
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

        /// <summary>Première seed (à partir de from) dont le plan relie ce module à la salle de départ par un couloir ; from sinon.</summary>
        public static int TestModuleSeed(MapLayoutSettings settings, List<ModuleInfo> infos, int target, int from)
        {
            for (int i = 0; i < TestModuleTries; i++)
            {
                int s = unchecked(from + i);
                MapPlan p = MapPlanner.Plan(settings, infos, s);
                if (p.Start < 0)
                    continue;
                foreach (PlanLink l in p.Links)
                {
                    int other = l.A == p.Start ? l.B : l.B == p.Start ? l.A : -1;
                    if (other >= 0 && p.Rooms[other].Module == target)
                        return s;
                }
            }

            return from;
        }

        /// <summary>Tire et construit la carte (remplace la précédente).</summary>
        public void Generate(int seed)
        {
            System.Collections.IEnumerator build = Build(seed, false);
            while (build.MoveNext())
            {
            }
        }

        /// <summary>Construction par étapes (async : une pause entre les étapes, pour l'écran de chargement).</summary>
        private System.Collections.IEnumerator Build(int seed, bool async)
        {
            Stage = "Tirage de la carte";
            Progress = 0f;
            if (_root != null)
            {
                // Nouvelle carte dans la même scène (mesures) : libère aussi les maillages fusionnés.
                foreach (MeshFilter mf in _root.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Décor fusionné"))
                        Destroy(mf.sharedMesh);
                DestroyImmediate(_root.gameObject);
                Resources.UnloadUnusedAssets();
                System.GC.Collect();
            }
            float Weight(RoomModule m)
            {
                float w = WeightOverride != null ? WeightOverride(m) : m.weight;
                return w > 0f && !string.IsNullOrEmpty(TestModule) && m.displayName == TestModule ? w * TestModuleWeight : w;
            }

            _modules = catalog != null ? catalog.modules.Where(m => m != null && m.validated && Weight(m) > 0f).ToList() : new List<RoomModule>();
            if (TestModulePrefab != null && !_modules.Contains(TestModulePrefab))
                _modules.Add(TestModulePrefab);                                // atelier : testé même avant validation
            var infos = _modules.Select((m, i) => ModuleInfo.From(m, i)).ToList();
            foreach (ModuleInfo info in infos)
                info.Weight = Weight(_modules[info.Index]);
            if (!string.IsNullOrEmpty(TestModule))
            {
                int target = _modules.FindIndex(m => m.displayName == TestModule);
                int found = target >= 0 ? TestModuleSeed(settings, infos, target, seed) : seed;
                Debug.Log(target < 0 ? $"Salle de test : module « {TestModule} » absent du catalogue"
                    : $"Salle de test : « {TestModule} » à côté du départ (seed {found})");
                seed = found;
            }

            LevelSeed = seed;
            var sw = Stopwatch.StartNew();
            Plan = MapPlanner.Plan(settings, infos, seed);
            PlanMs = (float)sw.Elapsed.TotalMilliseconds;
            if (Plan.Failures.Count > 0)
                Debug.LogWarning($"Carte aléatoire (seed {seed}) : {string.Join(" ; ", Plan.Failures)}");

            sw.Restart();
            KitBuild.PieceCount = 0;
            _rng = new System.Random(unchecked(seed ^ 0x5bd1e995));
            _root = new GameObject($"Niveau (carte aléatoire, seed {seed})").transform;
            // Inactive pendant la construction : aucun composant des modules ne démarre sur une carte à moitié faite.
            _root.gameObject.SetActive(false);
            _moduleInstances.Clear();
            Courses.Clear();
            _min = new Vector3(float.MaxValue, -10f, float.MaxValue);
            _max = new Vector3(float.MinValue, settings.upperLevel + settings.MaxCeiling + 10f, float.MinValue);
            PlanCuts();
            int rooms = Plan.Rooms.Count, links = Plan.Links.Count;
            var slice = Stopwatch.StartNew();
            for (int i = 0; i < rooms; i++)
            {
                BuildRoom(i);
                if (async && slice.Elapsed.TotalMilliseconds > frameBudgetMs)
                {
                    Stage = $"Salles {i + 1} / {rooms}";
                    Progress = 0.05f + 0.45f * (i + 1) / rooms;
                    yield return null;
                    slice.Restart();
                }
            }

            for (int l = 0; l < links; l++)
            {
                BuildCorridor(l);
                if (async && slice.Elapsed.TotalMilliseconds > frameBudgetMs)
                {
                    Stage = $"Couloirs {l + 1} / {links}";
                    Progress = 0.5f + 0.3f * (l + 1) / links;
                    yield return null;
                    slice.Restart();
                }
            }

            Stage = "Passages, galeries, sortie et repaire";
            Progress = 0.82f;
            if (async)
                yield return null;
            _root.gameObject.SetActive(true);
            Physics.SyncTransforms();
            BuildPassages();
            BuildGalleries();
            BuildParkourRooms();
            int lairNode = BuildLair();
            int arenaNode = BuildExit();
            BuildStart();
            Chests();
            Residents();
            ConfigureScene(lairNode, arenaNode);
            KitPieces = KitBuild.PieceCount;
            Stage = "Regroupement du décor";
            Progress = 0.88f;
            if (async)
                yield return null;
            ClearTorches();
            ThinTorches();
            Batch();
            Physics.SyncTransforms();
            if (!async && _residents != null)
                _residents.gameObject.SetActive(true);
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
            Splash = waterSplash;
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
                    KitBuild.Arch(parent, kit, d.Position + MapPlanner.SideVector(d.Side) * (T * 0.5f), RoomModule.SideYaw(d.Side),   // dans l'épaisseur du mur, sous le linteau
                        Plan.Links[d.Link].Width, settings.corridorHeight);
            }

            foreach (DoorSide s in new[] { DoorSide.Nord, DoorSide.Est, DoorSide.Sud, DoorSide.Ouest })
                if (_roomCuts.TryGetValue((i, s), out List<Cut> extra))
                    cuts[s].AddRange(extra);
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
                PitFloor(parent, r, mod, settings, kit, water);
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
            WaterSplash.Add(wet, Splash, y - 1.2f);
            wet.AddComponent<NotWalkable>();
            KitBuild.Box(parent, "Pont du canal (grand)", new Vector3(c.x, y - 0.5f, c.z + zc), new Vector3(settings.corridorWidth, 1f, w + 2f), KitBuild.Kind.Sol, null);
            KitBuild.Box(parent, "Pont du canal (petit)", new Vector3(c.x + hx * 0.65f, y - 0.5f, c.z + zc), new Vector3(4f, 1f, w + 2f), KitBuild.Kind.Sol, null);
            if (kit != null)
            {
                // Tablier du modèle au niveau de son pivot : 2 cm au-dessus des dalles des rives (sinon il scintille avec elles).
                // Et 0.5 % plus étroit : ses bouts (8 m) restent dans les bords du canal au lieu d'affleurer leur face.
                KitBuild.Piece(kit.pontEgout14, parent, new Vector3(c.x, y + BridgeLift, c.z + zc), 90f, new Vector3(0.995f, 1f, 1f));
                KitBuild.Piece(kit.pontEgout4, parent, new Vector3(c.x + hx * 0.65f, y + BridgeLift, c.z + zc), 90f, new Vector3(0.995f, 1f, 1f));
            }

            Lamp(parent, new Vector3(c.x - hx * 0.5f, y + 4f, c.z + zc), new Color(0.3f, 0.7f, 0.8f), 3f, 25f);
        }

        private static void PitFloor(Transform parent, PlanRoom r, RoomModule mod, MapLayoutSettings settings, KitSet kit, Material water)
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
            // Eau au fond (animations, point 6) : 1 m d'eau ; on ne revient en haut qu'en la touchant (avant : dès 1.5 m sous le sol).
            const float pool = 1f;
            var surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
            surface.name = "Eau de la fosse";
            DestroyImmediate(surface.GetComponent<Collider>());
            surface.transform.SetParent(parent, false);
            surface.transform.position = new Vector3(c.x + cx, bottom + pool - 0.025f, c.z + cz);
            surface.transform.localScale = new Vector3(wx, 0.05f, wz);
            if (water != null)
                surface.GetComponent<MeshRenderer>().sharedMaterial = water;
            var trig = new GameObject("Vide de la fosse : eau (retour au bord)");
            trig.transform.SetParent(parent, false);
            trig.transform.position = new Vector3(c.x + cx, bottom + (pool + 0.3f) * 0.5f, c.z + cz);
            var box = trig.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(wx, pool + 0.3f, wz);
            var respawns = new List<Transform>();
            foreach (Vector3 p in new[] { new Vector3(x0 - 2.5f, 0f, cz), new Vector3(x1 + 2.5f, 0f, cz), new Vector3(cx, 0f, za - 2.5f), new Vector3(cx, 0f, zb + 2.5f) })
            {
                var t = new GameObject("Reprise").transform;
                t.SetParent(trig.transform, false);
                t.position = c + p + Vector3.up * 0.2f;
                respawns.Add(t);
            }

            trig.AddComponent<FallVoid>().Configure(settings.waterDamage, respawns.ToArray());
            WaterSplash.Add(trig, Splash, bottom + pool);
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
            // Couloir plat de 14 m : voûte gothique du kit de la PR #8 (murs jusqu'à la naissance, 8 m ; voûte au-dessus).
            bool gothic = IsGothic(li);

            // Le couloir va de la face intérieure du mur d'une salle à l'autre ; ses murs, son sol et son plafond s'arrêtent à la face
            // extérieure (épaisseur T) : dans l'embrasure, seules les pièces de la salle (sol, linteau, tableaux de la porte). Sinon
            // deux faces au même endroit scintillent (contours de porte, correctifs-rendu point 1).
            float inner = Mathf.Max(0f, len - 2f * T);
            if (!l.Slope)
            {
                Vector3 along = alongX ? new Vector3(inner, 1f, w) : new Vector3(w, 1f, inner);
                if (ya > 0.1f)
                    KitBuild.Box(parent, "Sol (bloc)", mid + Vector3.up * (ya * 0.5f), alongX ? new Vector3(inner, ya, w) : new Vector3(w, ya, inner), KitBuild.Kind.Bloc, kit);
                else
                    KitBuild.Box(parent, "Sol", mid + Vector3.down * 0.5f, along, KitBuild.Kind.Sol, kit);
                if (!gothic)
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
                // 2 cm de plus que le passage du cadre (SM_Porte_Piece) : les tableaux du mur restent cachés dans le cadre.
                (an.Side > 0 ? plus : minus).Add(new Cut(along - AnnexDoor * 0.5f - 0.02f, along + AnnexDoor * 0.5f + 0.02f, p.y, p.y + AnnexDoor + 0.02f));
                BuildAnnex(parent, p, lateral * an.Side, w, an.Locked);
            }

            if (_corridorCuts.TryGetValue((li, 1), out List<Cut> morePlus))
                plus.AddRange(morePlus);
            if (_corridorCuts.TryGetValue((li, -1), out List<Cut> moreMinus))
                minus.AddRange(moreMinus);
            float yLow = Mathf.Min(ya, yb), yTop = Mathf.Max(ya, yb) + h;
            float from = (alongX ? Mathf.Min(flatStart.x, flatEnd.x) : Mathf.Min(flatStart.z, flatEnd.z)) + T;
            float to = (alongX ? Mathf.Max(flatStart.x, flatEnd.x) : Mathf.Max(flatStart.z, flatEnd.z)) - T;
            float sideCoord = alongX ? mid.z : mid.x;
            // Côté +latéral : nord si le couloir va vers +x (latéral = (−dz, 0, dx)).
            float sgn = alongX ? lateral.z : lateral.x;
            // Voûte gothique : murs jusqu'à la naissance (8 m) ; les travées à arcade ou à niche sont retirées du mur (pièces du kit).
            List<GothicBay> bays = null;
            if (gothic)
            {
                yTop = ya + GothicSpring;
                bays = PlanGothicBays(new System.Random(Plan.Seed * 7919 + li * 131 + 17), len, ya, t => Along(Point(t), alongX ? Vector3.right : Vector3.forward), plus, minus);
            }

            Wall(parent, "Mur 1", alongX, sideCoord + sgn * (w * 0.5f + T * 0.5f), from, to, yLow > 0.1f ? 0f : yLow, yTop, plus);
            Wall(parent, "Mur 2", alongX, sideCoord - sgn * (w * 0.5f + T * 0.5f), from, to, yLow > 0.1f ? 0f : yLow, yTop, minus);
            bool Open(int side, float d) => Plan.Galleries.Any(g => g.Link == li && g.Side == side) && d > 2f && d < len - 2f;
            if (gothic)
                BuildGothic(parent, len, w, h, ya, dir, lateral, (t, s) => Point(t, s), bays, Open);

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
            // Arcs tous les 20 m. En pente (20° au plus), plafond parallèle à la rampe : l'arc (1.6 m d'épaisseur) descend de 0.3 m
            // pour que ses deux bords restent sous le plafond, et ses piédroits s'enfoncent d'autant côté haut (posés côté bas).
            if (kit != null && !gothic)
                for (float d = 20f; d < len - 10f; d += 20f)
                {
                    // Pas d'arc devant la porte d'une pièce annexe : son piédroit la bouchait pour les goules (retours, animations point 2).
                    bool nearAnnex = false;
                    foreach (PlanAnnex an in Plan.Annexes)
                        if (an.Link == li && Mathf.Abs(an.T * len - d) < AnnexDoor * 0.5f + 1.6f)
                            nearAnnex = true;
                    if (nearAnnex)
                        continue;
                    float sink = l.Slope ? 0.3f : 0f;
                    KitBuild.Arch(parent, kit, Point(d / len) - Vector3.up * sink, yaw, w, h, l.Slope);
                }
            if (kit != null && kit.torche != null && !gothic)
                for (float d = 6f; d < len - 4f; d += 12f)
                    foreach (int s in new[] { 1, -1 })
                    {
                        if (Open(s, d))
                            continue;                                  // côté ouvert sur une galerie : pas de mur
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
            if (!l.Slope && _rng.NextDouble() < settings.corridorFeatureShare && !Plan.Galleries.Any(g => g.Link == li))
            {
                Vector3 p = Point(0.5f) + Vector3.up * (settings.bridgeBottom + 0.5f);
                if (_rng.NextDouble() < 0.5)
                    KitBuild.Box(parent, $"Poutre (dessous à {settings.bridgeBottom:0} m)", p, alongX ? new Vector3(1f, 1f, w) : new Vector3(w, 1f, 1f), KitBuild.Kind.Poutre, kit);
                else
                    KitBuild.Box(parent, $"Pont (dessous à {settings.bridgeBottom:0} m)", p, alongX ? new Vector3(settings.bridgeWidth, 1f, w) : new Vector3(w, 1f, settings.bridgeWidth), KitBuild.Kind.Pont, kit);
            }
        }

        // ---------- Couloir gothique (kit de la PR #8, docs/art/INTEGRATION-PROPS.md) ----------

        /// <summary>Naissance de la voûte (haut des murs), largeur du couloir, longueur d'une travée (m).</summary>
        private const float GothicSpring = 8f, GothicWidth = 14f, GothicBayLength = 8f;
        /// <summary>
        /// Profil de la voûte en lancette : arcs de cercle centrés sur la ligne de naissance à 7.66 m de l'axe, de l'autre côté ;
        /// intrados R 14.66 (clé à 20.5 m), extrados R 15.26 (dessus à 21.2 m) ; intrados de l'arc-doubleau R 14.06 (clé à 19.8 m).
        /// </summary>
        private const float GothicCenter = 7.66f, VaultInner = 14.66f, VaultOuter = 15.26f, ArcInner = 14.06f;

        /// <summary>
        /// Noms des pièces visuelles du couloir gothique : comme les pièces Kit_*, elles sont fusionnées par matériau et par couloir
        /// (Batch) ; leurs collisions sont sur des objets à part.
        /// </summary>
        public const string GothicVault = "Voûte en ogive", GothicArc = "Arc-doubleau", GothicColumn = "Colonne engagée",
            GothicArcadeWall = "Mur à arcade", GothicNicheWall = "Mur à niche", GothicDoorFill = "Coins de la porte (pierre)";

        private static readonly HashSet<string> GothicMerged =
            new HashSet<string> { GothicVault, GothicArc, GothicColumn, GothicArcadeWall, GothicNicheWall, GothicDoorFill };

        private sealed class GothicBay
        {
            public float D0, D1;
            /// <summary>Mur de la travée, côté +latéral [0] et −latéral [1] : 0 plein (mur du couloir), 1 arcade, 2 niche.</summary>
            public readonly int[] Kind = new int[2];
            /// <summary>Colonne engagée au début de la travée (sous l'arc-doubleau), par côté.</summary>
            public readonly bool[] Column = new bool[2];
        }

        /// <summary>Couloir plat de 14 m (ni pente ni dénivelé) et kit gothique livré : voûte en ogive.</summary>
        private bool IsGothic(int li)
        {
            PlanLink l = Plan.Links[li];
            return settings.gothicCorridors && kit != null && kit.HasGothic && !l.Slope && Mathf.Abs(l.Width - GothicWidth) < 0.6f
                   && Mathf.Abs(l.From.y - l.To.y) < 0.05f;
        }

        /// <summary>Largeur libre de l'intrados à une hauteur au-dessus de la naissance (demi-largeur, m).</summary>
        private static float GothicHalfWidth(float radius, float aboveSpring) =>
            Mathf.Sqrt(Mathf.Max(0f, radius * radius - aboveSpring * aboveSpring)) - GothicCenter;

        /// <summary>
        /// Travées de 8 m (ajustées pour remplir le couloir d'une face de salle à l'autre). Une travée sans ouverture (porte de pièce,
        /// galerie, passage, à 0.7 m près pour les cadres) reçoit, par côté, un mur à arcade aveugle, à niche ou plein ; les deux
        /// premiers sont retirés du mur du couloir (coupe ajoutée). Colonne au début de la travée sauf devant une petite ouverture.
        /// Tirage propre au couloir (seed de la carte) : n'entame pas celui du reste de la carte.
        /// </summary>
        private List<GothicBay> PlanGothicBays(System.Random rng, float len, float ya, System.Func<float, float> along, List<Cut> plus, List<Cut> minus)
        {
            float inner = len - 2f * T;
            int n = Mathf.Max(1, Mathf.RoundToInt(inner / GothicBayLength));
            float bay = inner / n;
            var bays = new List<GothicBay>(n);
            var original = new[] { plus.ToList(), minus.ToList() };
            bool Hits(List<Cut> cuts, float d0, float d1, float margin, bool shortOnly)
            {
                float a0 = along(d0 / len), a1 = along(d1 / len), lo = Mathf.Min(a0, a1) - margin, hi = Mathf.Max(a0, a1) + margin;
                return cuts.Any(c => c.A < hi && c.B > lo && (!shortOnly || c.Y1 - c.Y0 < 12f));
            }

            for (int i = 0; i < n; i++)
            {
                var b = new GothicBay { D0 = T + i * bay, D1 = T + (i + 1) * bay };
                for (int s = 0; s < 2; s++)
                {
                    double r = rng.NextDouble();
                    bool blocked = Hits(original[s], b.D0, b.D1, 0.7f, false);
                    b.Kind[s] = blocked ? 0 : r < settings.gothicArcadeShare ? 1 : r < settings.gothicArcadeShare + settings.gothicNicheShare ? 2 : 0;
                    b.Column[s] = i > 0 && !Hits(original[s], b.D0 - 0.72f, b.D0 + 0.72f, 0.7f, true);
                    if (b.Kind[s] != 0)
                    {
                        float a0 = along(b.D0 / len), a1 = along(b.D1 / len);
                        (s == 0 ? plus : minus).Add(new Cut(Mathf.Min(a0, a1), Mathf.Max(a0, a1), ya, ya + GothicSpring));
                    }
                }

                bays.Add(b);
            }

            return bays;
        }

        /// <summary>
        /// Voûte en lancette, arcs-doubleaux sur colonnes engagées, murs de travée (arcade aveugle, niche), torches sur les trumeaux
        /// à 4 m, fermetures des bouts (coins de la porte de 14 × 15 m hors de la voûte, pignon au-dessus de la porte). Collision : un MeshCollider
        /// statique par couloir pour la voûte, les arcs et les coins des portes (le traqueur passe au travers, le NavMesh l'ignore),
        /// boîtes des murs et des colonnes.
        /// </summary>
        private void BuildGothic(Transform parent, float len, float w, float h, float ya, Vector3 dir, Vector3 lateral,
            System.Func<float, float, Vector3> point, List<GothicBay> bays, System.Func<int, float, bool> open)
        {
            var root = new GameObject("Voûte gothique").transform;
            root.SetParent(parent, false);
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, inner = len - 2f * T;
            Vector3 P(float d, float side = 0f) => point(d / len, side);

            // Collision de la voûte, des arcs-doubleaux et des coins des portes : un seul maillage par couloir (MeshCollider statique,
            // non convexe), à la place des anneaux de boîtes (14 600 boîtes d'arcs sur la seed 104729).
            Vector3 mid = P(len * 0.5f);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Matrix4x4 toCollision = Matrix4x4.TRS(mid, rot, Vector3.one).inverse;
            var solid = new SolidMesh();

            // Voûte : un tronçon par travée (étiré à la longueur de la travée, UV monde).
            foreach (GothicBay b in bays)
                KitBuild.Piece(kit.voute8, root, P((b.D0 + b.D1) * 0.5f), yaw, new Vector3(1f, 1f, (b.D1 - b.D0) / GothicBayLength)).name = GothicVault;
            // Voûte : 0.15 m sous la naissance, dans le haut des murs (pas de joint au ras de la naissance).
            RingSolid(solid, toCollision * Matrix4x4.TRS(mid, rot, Vector3.one), VaultInner, VaultOuter, -inner * 0.5f, inner * 0.5f, 0.15f);

            // Arcs-doubleaux : à chaque limite de travée et contre chaque bout (ils encadrent la porte de la salle).
            var arcs = new List<float> { T + 0.4f, len - T - 0.4f };
            arcs.AddRange(bays.Skip(1).Select(b => b.D0));
            foreach (float d in arcs)
            {
                KitBuild.Piece(kit.arcDoubleau, root, P(d), yaw).name = GothicArc;
                RingSolid(solid, toCollision * Matrix4x4.TRS(P(d), rot, Vector3.one), ArcInner, VaultInner + 0.05f, -0.4f, 0.4f, 0f);
            }

            for (int i = 0; i < bays.Count; i++)
            {
                GothicBay b = bays[i];
                float bay = b.D1 - b.D0;
                for (int s = 0; s < 2; s++)
                {
                    float side = s == 0 ? 1f : -1f;
                    Vector3 inward = -lateral * side;
                    float yawIn = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
                    if (b.Column[s])
                    {
                        KitBuild.Piece(kit.colonneEngagee, root, P(b.D0, side * w * 0.5f), yawIn).name = GothicColumn;
                        // Collision à part : la pièce visuelle est fusionnée puis détruite (Batch).
                        var col = new GameObject("Collision de la colonne");
                        col.transform.SetParent(root, false);
                        col.transform.localPosition = P(b.D0, side * w * 0.5f);
                        col.transform.localRotation = Quaternion.Euler(0f, yawIn, 0f);
                        var box = col.AddComponent<BoxCollider>();
                        box.center = new Vector3(0f, GothicSpring * 0.5f, 0.39f);
                        box.size = new Vector3(1.44f, GothicSpring, 0.78f);
                        col.AddComponent<NotWalkable>();
                    }

                    if (b.Kind[s] != 0)
                        GothicWall(root, P((b.D0 + b.D1) * 0.5f, side * w * 0.5f), yawIn, inward, bay, b.Kind[s] == 1);

                    // Torche sur le trumeau, à 4 m, près de la colonne (une travée sur deux, en quinconce ; pas sur une arcade ni
                    // du côté ouvert d'une galerie). ClearTorches l'écarte encore des colonnes et des cadres si besoin.
                    float td = b.D0 + 2.2f;
                    if (kit.torche != null && (i + s) % 2 == 0 && b.Kind[s] != 1 && !open(s == 0 ? 1 : -1, td))
                        KitBuild.Piece(kit.torche, parent, P(td, side * (w * 0.5f - 0.3f)) + Vector3.up * 4f, yawIn);
                }
            }

            // Bouts : la porte de la salle fait 14 × h m ; au-dessus de la naissance, ses coins sont hors de la voûte.
            foreach (bool atStart in new[] { true, false })
            {
                float d = atStart ? T : len - T;
                Vector3 axis = atStart ? dir : -dir;
                float endYaw = Mathf.Atan2(axis.x, axis.z) * Mathf.Rad2Deg;
                GothicEnd(root, P(d), endYaw, h, solid, toCollision * Matrix4x4.TRS(P(d), Quaternion.Euler(0f, endYaw, 0f), Vector3.one));
                // Pignon : au-dessus de la porte, la voûte monte plus haut que le mur de la salle (plafond de 16 à 22 m).
                Vector3 across = alongXOf(dir) ? new Vector3(0.3f, 21.6f - h, 11f) : new Vector3(11f, 21.6f - h, 0.3f);
                KitBuild.Box(root, "Pignon de la voûte", P(d) + axis * 0.15f + Vector3.up * ((h + 21.6f) * 0.5f), across, KitBuild.Kind.Mur, kit, walkable: false);
            }

            // Pour les joueurs et les objets ; le traqueur passe au travers et le NavMesh l'ignore (StalkerPassesThrough), comme les
            // anneaux de boîtes qu'elle remplace.
            var collision = new GameObject("Collision de la voûte");
            collision.transform.SetParent(root, false);
            collision.transform.localPosition = mid;
            collision.transform.localRotation = rot;
            collision.AddComponent<StalkerPassesThrough>();
            collision.AddComponent<NotWalkable>();
            collision.AddComponent<MeshCollider>().sharedMesh = solid.ToMesh("Collision de la voûte");

            static bool alongXOf(Vector3 v) => Mathf.Abs(v.x) > 0.5f;
        }

        /// <summary>
        /// Couloir gothique isolé (vitrine, zone « Traqueur ») : mêmes pièces, mêmes collisions et mêmes torches que dans la carte
        /// (PlanGothicBays, BuildGothic), de start vers +x sur len m, 14 m de large, bouts ouverts (la porte d'une salle dans la carte).
        /// Côté +z : ouverture de galerie de galleryFrom à galleryTo (m le long du couloir), du dessus de galleryFloor à la naissance ;
        /// poutres en travers aux distances beams, dessous à bridgeBottom (le traqueur se baisse). parent : repère du monde.
        /// </summary>
        public Transform BuildShowcaseGothicCorridor(Transform parent, Vector3 start, float len, int seed, float galleryFrom, float galleryTo,
            float galleryFloor, float[] beams)
        {
            var root = new GameObject("Couloir gothique (vitrine)").transform;
            root.SetParent(parent, false);
            Vector3 dir = Vector3.right, lateral = new Vector3(-dir.z, 0f, dir.x);
            float w = GothicWidth, h = settings != null ? settings.corridorHeight : 16f, ya = start.y;
            Vector3 Point(float t, float side = 0f) => start + dir * (t * len) + lateral * side;
            Vector3 mid = Point(0.5f);
            KitBuild.Box(root, "Sol", mid + Vector3.down * 0.5f, new Vector3(len - 2f * T, 1f, w), KitBuild.Kind.Sol, kit);
            var plus = new List<Cut>();
            var minus = new List<Cut>();
            if (galleryTo > galleryFrom)
                plus.Add(new Cut(start.x + galleryFrom, start.x + galleryTo, ya + galleryFloor, ya + GothicSpring));
            List<GothicBay> bays = PlanGothicBays(new System.Random(seed), len, ya, t => Point(t).x, plus, minus);
            float from = start.x + T, to = start.x + len - T;
            Wall(root, "Mur 1", true, mid.z + (w * 0.5f + T * 0.5f), from, to, ya, ya + GothicSpring, plus);
            Wall(root, "Mur 2", true, mid.z - (w * 0.5f + T * 0.5f), from, to, ya, ya + GothicSpring, minus);
            bool Open(int side, float d) => side == 1 && d > galleryFrom && d < galleryTo;
            BuildGothic(root, len, w, h, ya, dir, lateral, (t, s) => Point(t, s), bays, Open);
            float bottom = settings != null ? settings.bridgeBottom : 10f;
            foreach (float d in beams ?? System.Array.Empty<float>())
                KitBuild.Box(root, $"Poutre (dessous à {bottom:0} m)", Point(d / len) + Vector3.up * (bottom + 0.5f), new Vector3(1f, 1f, w), KitBuild.Kind.Poutre, kit);
            int lamps = Mathf.Max(1, Mathf.RoundToInt(len / 25f));
            for (int i = 0; i < lamps; i++)
                Lamp(root, Point((i + 0.5f) / lamps) + Vector3.up * (h - 2f), i % 2 == 0 ? new Color(1f, 0.75f, 0.5f) : new Color(0.8f, 0.85f, 1f), 5f, 40f);
            return root;
        }

        /// <summary>
        /// Solides fermés (faces tournées vers l'extérieur : un rayon ne touche que la face avant d'un MeshCollider) pour un
        /// MeshCollider statique : sections faites de deux profils A[k], B[k] (plan xy) extrudées en z.
        /// </summary>
        private sealed class SolidMesh
        {
            private readonly List<Vector3> _v = new List<Vector3>();
            private readonly List<int> _i = new List<int>();

            private void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 outward)
            {
                Vector3 n = Vector3.Cross(b - a, c - a);
                if (n.sqrMagnitude < 1e-10f)
                    return;                                                 // triangle dégénéré
                if (Vector3.Dot(n, outward) < 0f)
                    (b, c) = (c, b);
                int k = _v.Count;
                _v.Add(a);
                _v.Add(b);
                _v.Add(c);
                _i.Add(k);
                _i.Add(k + 1);
                _i.Add(k + 2);
            }

            private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward)
            {
                Tri(a, b, c, outward);
                Tri(a, c, d, outward);
            }

            /// <summary>Bande entre les profils A et B (même nombre de points), de z0 à z1, placée par m.</summary>
            public void Strip(IList<Vector2> a, IList<Vector2> b, float z0, float z1, Matrix4x4 m)
            {
                Vector3 W(Vector2 p, float z) => m.MultiplyPoint3x4(new Vector3(p.x, p.y, z));
                Vector3 front = m.MultiplyVector(Vector3.forward);
                int n = a.Count;

                void Side(Vector2 p, Vector2 q, Vector2 inside)
                {
                    Vector2 e = q - p, normal = new Vector2(e.y, -e.x);
                    if (e.sqrMagnitude < 1e-8f)
                        return;
                    if (Vector2.Dot(normal, (p + q) * 0.5f - inside) < 0f)
                        normal = -normal;
                    Quad(W(p, z0), W(q, z0), W(q, z1), W(p, z1), m.MultiplyVector(new Vector3(normal.x, normal.y, 0f)));
                }

                for (int k = 0; k + 1 < n; k++)
                {
                    Quad(W(a[k], z1), W(a[k + 1], z1), W(b[k + 1], z1), W(b[k], z1), front);
                    Quad(W(a[k], z0), W(a[k + 1], z0), W(b[k + 1], z0), W(b[k], z0), -front);
                    Vector2 c = (a[k] + a[k + 1] + b[k] + b[k + 1]) * 0.25f;
                    Side(a[k], a[k + 1], c);
                    Side(b[k], b[k + 1], c);
                }

                Side(a[0], b[0], (a[0] + a[1] + b[0] + b[1]) * 0.25f);
                Side(a[n - 1], b[n - 1], (a[n - 1] + a[n - 2] + b[n - 1] + b[n - 2]) * 0.25f);
            }

            public Mesh ToMesh(string name)
            {
                var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.SetVertices(_v);
                mesh.SetTriangles(_i, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        /// <summary>
        /// Anneau en lancette (voûte ou arc) de z0 à z1 : deux moitiés, intrados rIn et extrados rOut exacts (arcs de cercle centrés
        /// sur la ligne de naissance, à GothicCenter de l'axe, de l'autre côté), de below sous la naissance jusqu'à l'axe.
        /// </summary>
        private static void RingSolid(SolidMesh solid, Matrix4x4 m, float rIn, float rOut, float z0, float z1, float below)
        {
            const int segments = 12;
            foreach (float side in new[] { -1f, 1f })
            {
                var a = new List<Vector2>(segments + 1);
                var b = new List<Vector2>(segments + 1);
                for (int k = 0; k <= segments; k++)
                {
                    float t = (float)k / segments;
                    a.Add(Arc(rIn, t));
                    b.Add(Arc(rOut, t));
                }

                solid.Strip(a, b, z0, z1, m);

                Vector2 Arc(float r, float t)
                {
                    float angle = Mathf.Lerp(-Mathf.Asin(below / r), Mathf.Acos(GothicCenter / r), t);
                    return new Vector2(side * Mathf.Max(0f, r * Mathf.Cos(angle) - GothicCenter), GothicSpring + r * Mathf.Sin(angle));
                }
            }
        }

        /// <summary>
        /// Mur d'une travée : pièce du kit (étirée à la travée) et sa collision. Arcade : passage en ogive de 4.4 × 7 m (naissance 4 m)
        /// fermé derrière l'épaisseur du mur (arcade aveugle, 1.2 m de profondeur, sol et fond) ; niche : collision au nu du mur.
        /// </summary>
        private void GothicWall(Transform root, Vector3 pivot, float yawIn, Vector3 inward, float bay, bool arcade)
        {
            GameObject piece = KitBuild.Piece(arcade ? kit.murArcade : kit.murNiche, root, pivot, yawIn, new Vector3(bay / GothicBayLength, 1f, 1f));
            piece.name = arcade ? GothicArcadeWall : GothicNicheWall;
            var col = new GameObject("Collision du mur").transform;
            col.SetParent(piece.transform.parent, false);
            col.localPosition = pivot;
            col.localRotation = Quaternion.Euler(0f, yawIn, 0f);
            col.gameObject.AddComponent<NotWalkable>();
            void Box(float x0, float x1, float y0, float y1)
            {
                if (x1 - x0 < 0.01f || y1 - y0 < 0.01f)
                    return;
                var go = new GameObject("Bloc");
                go.transform.SetParent(col, false);
                var bc = go.AddComponent<BoxCollider>();
                bc.center = new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, -T * 0.5f);
                bc.size = new Vector3(x1 - x0, y1 - y0, T);
            }

            float half = bay * 0.5f;
            if (!arcade)
            {
                Box(-half, half, 0f, GothicSpring);
                return;
            }

            // Arcade : ouverture de 2.2 m de demi-largeur (à l'échelle de la travée), arc brisé de la naissance (4 m) à la clé (7 m) :
            // centres à 0.945 m de l'axe, rayon 3.145 m. Piédroits, bandes de 0.25 m sous l'arc, plein au-dessus de la clé.
            float sx = bay / GothicBayLength, open = 2.2f * sx;
            Box(-half, -open, 0f, GothicSpring);
            Box(open, half, 0f, GothicSpring);
            for (float y = 4f; y < 7f - 1e-3f; y += 0.25f)
            {
                float rise = y + 0.25f - 4f, hw = Mathf.Max(0f, Mathf.Sqrt(Mathf.Max(0f, 3.145f * 3.145f - rise * rise)) - 0.945f) * sx;
                Box(-open, -hw, y, y + 0.25f);
                Box(hw, open, y, y + 0.25f);
            }

            Box(-open, open, 7f, GothicSpring);
            // Fond et sol de l'arcade aveugle, derrière l'épaisseur de la pièce (1.2 m).
            Vector3 outward = -inward;
            bool alongX = Mathf.Abs(inward.z) > 0.5f;                   // mur le long de x : son épaisseur est en z
            float wide = 2f * open + 0.4f;
            KitBuild.Box(root, "Arcade aveugle (fond)", pivot + outward * 1.35f + Vector3.up * 3.6f,
                alongX ? new Vector3(wide, 7.2f, 0.3f) : new Vector3(0.3f, 7.2f, wide), KitBuild.Kind.Mur, kit, walkable: false);
            KitBuild.Box(root, "Arcade aveugle (sol)", pivot + outward * 0.75f + Vector3.down * 0.5f,
                alongX ? new Vector3(wide, 1f, 1.5f) : new Vector3(1.5f, 1f, wide), KitBuild.Kind.Sol, kit);
        }

        /// <summary>
        /// Coins de la porte d'un bout de couloir (14 × h m) entre la porte et l'intrados de la voûte : remplissage en pierre tourné vers
        /// la salle (maillage en bande, matériau de l'arc-doubleau) ; sa collision (pas de passage derrière la voûte) est ajoutée au
        /// maillage de collision de la voûte du couloir.
        /// </summary>
        private void GothicEnd(Transform root, Vector3 at, float yaw, float h, SolidMesh solid, Matrix4x4 toCollision)
        {
            var go = new GameObject("Coins de la porte (sous la voûte)");
            go.transform.SetParent(root, false);
            go.transform.localPosition = at;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            const float edge = 7.3f, depth = 0.6f;
            float top = h + 0.3f;
            var v = new List<Vector3>();
            var tri = new List<int>();
            foreach (float side in new[] { -1f, 1f })
            {
                int first = v.Count, rows = 0;
                for (float y = GothicSpring; ; y = Mathf.Min(top, y + 0.25f))
                {
                    float x = Mathf.Min(edge, GothicHalfWidth(VaultInner, y - GothicSpring));
                    v.Add(new Vector3(side * x, y, 0f));
                    v.Add(new Vector3(side * edge, y, 0f));
                    rows++;
                    if (y >= top - 1e-4f)
                        break;
                }

                // Faces tournées vers −Z (la salle) : sens horaire vu de −Z.
                for (int k = 0; k + 1 < rows; k++)
                {
                    int a = first + 2 * k, b = a + 1, c = a + 2, e = a + 3;
                    if (side > 0)
                        tri.AddRange(new[] { a, c, b, b, c, e });
                    else
                        tri.AddRange(new[] { a, b, c, b, e, c });
                }

                // Collision (maillage de la voûte du couloir) : de l'intrados au bord de la porte, sur 0.6 m d'épaisseur.
                var inside = new List<Vector2>();
                var outside = new List<Vector2>();
                for (float y = GothicSpring; ; y = Mathf.Min(top, y + 0.25f))
                {
                    inside.Add(new Vector2(side * Mathf.Clamp(GothicHalfWidth(VaultInner, y - GothicSpring), 0f, edge), y));
                    outside.Add(new Vector2(side * edge, y));
                    if (y >= top - 1e-4f)
                        break;
                }

                solid.Strip(inside, outside, 0f, depth, toCollision);
            }

            var mesh = new Mesh { name = "Coins de la porte" };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.SetNormals(Enumerable.Repeat(Vector3.back, v.Count).ToList());
            mesh.SetUVs(0, v.Select(p => new Vector2(p.x, p.y)).ToList());
            mesh.RecalculateBounds();
            // Maillage sur un enfant (fusionné par Batch) ; la collision reste sur go.
            var fill = new GameObject(GothicDoorFill);
            fill.transform.SetParent(go.transform, false);
            fill.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer stone = kit.arcDoubleau.GetComponentInChildren<Renderer>();
            fill.AddComponent<MeshRenderer>().sharedMaterial = stone != null ? stone.sharedMaterial : null;
            go.AddComponent<NotWalkable>();
            go.AddComponent<StalkerPassesThrough>();
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

        // ---------- Contournement : passages en hauteur et galeries de parcours ----------

        /// <summary>Repère d'un bout de couloir : face intérieure du mur de la salle (sol compris), axe vers le couloir, latéral du couloir.</summary>
        private void Frame(int li, bool atStart, out Vector3 face, out Vector3 axis, out Vector3 lateral, out float len)
        {
            PlanLink l = Plan.Links[li];
            Vector3 dir = l.To - l.From;
            dir.y = 0f;
            len = dir.magnitude;
            dir /= Mathf.Max(0.001f, len);
            lateral = new Vector3(-dir.z, 0f, dir.x);
            face = atStart ? l.From : l.To;
            axis = atStart ? dir : -dir;
        }

        private static float Along(Vector3 p, Vector3 wallAxis) => Mathf.Abs(wallAxis.x) > 0.5f ? p.x : p.z;

        private void AddCut<TK>(Dictionary<TK, List<Cut>> map, TK key, Cut cut)
        {
            if (!map.TryGetValue(key, out List<Cut> list))
                map[key] = list = new List<Cut>();
            list.Add(cut);
        }

        /// <summary>Ouvertures des passages (mur de la salle et mur du couloir) et des portes des galeries, avant de construire les murs.</summary>
        private void PlanCuts()
        {
            _roomCuts.Clear();
            _corridorCuts.Clear();
            ClosedPassages.Clear();
            foreach (PlanPassage p in Plan.Passages)
            {
                PlanLink l = Plan.Links[p.Link];
                Frame(p.Link, p.AtStart, out Vector3 face, out Vector3 axis, out Vector3 lateral, out _);
                Vector3 latS = lateral * p.Side;
                int room = p.AtStart ? l.A : l.B;
                DoorSide wall = Plan.Rooms[room].Doors[p.AtStart ? l.DoorA : l.DoorB].Side;
                Vector3 wallAxis = wall == DoorSide.Nord || wall == DoorSide.Sud ? Vector3.right : Vector3.forward;
                float a0 = Along(face + latS * (l.Width * 0.5f + T), wallAxis), a1 = Along(face + latS * (l.Width * 0.5f + T + DuctW), wallAxis);
                AddCut(_roomCuts, (room, wall), new Cut(Mathf.Min(a0, a1) + 0.25f, Mathf.Max(a0, a1) - 0.25f, face.y + DuctY, face.y + DuctY + DuctH));
                // Ouverture côté couloir de 4 m (T + 3.5 à T + 7.5) : à 3 m, le bout du conduit restait hors de portée de la main du
                // traqueur sur une carte sur 20 (retours du lot, point 5).
                float c0 = Along(face + axis * (T + 3.5f), axis), c1 = Along(face + axis * (T + 7.5f), axis);
                AddCut(_corridorCuts, (p.Link, p.Side), new Cut(Mathf.Min(c0, c1), Mathf.Max(c0, c1), face.y + DuctY, face.y + DuctY + DuctH));
            }

            // Galeries : le mur du couloir disparaît sur tout leur long (parcours parallèle à la voie du traqueur, à portée de sa main).
            foreach (PlanGallery g in Plan.Galleries)
            {
                Frame(g.Link, true, out Vector3 face, out Vector3 axis, out _, out float len);
                float c0 = Along(face + axis * 3f, axis), c1 = Along(face + axis * (len - 3f), axis);
                AddCut(_corridorCuts, (g.Link, g.Side), new Cut(Mathf.Min(c0, c1), Mathf.Max(c0, c1), face.y - 1f, face.y + settings.corridorHeight + 1f));
            }
        }

        private GameObject NM(string name, Transform parent, Vector3 center, Vector3 size, bool walkable = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Non magnétique : {name}";
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            if (nonMagnetic != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = nonMagnetic;
            go.AddComponent<Orbs.NonMagneticSurface>();
            if (!walkable)
                go.AddComponent<NotWalkable>();
            return go;
        }

        /// <summary>
        /// Passages en hauteur (D-056) : à côté de l'entrée d'un couloir, ouverture de 3 × 3 m à 4.5 m dans le mur de la salle, conduit
        /// de 8 m vers le mur du couloir ; on y monte en saut mural (murs non magnétiques autour) ; trop petit pour le traqueur, à
        /// portée de sa main. Refermé si un étage du module bouche l'ouverture.
        /// </summary>
        private void BuildPassages()
        {
            var root = new GameObject("Passages en hauteur (saut mural)").transform;
            root.SetParent(_root, false);
            for (int k = 0; k < Plan.Passages.Count; k++)
            {
                PlanPassage p = Plan.Passages[k];
                PlanLink l = Plan.Links[p.Link];
                Frame(p.Link, p.AtStart, out Vector3 face, out Vector3 axis, out Vector3 lateral, out _);
                float w = l.Width;
                Vector3 latS = lateral * p.Side;
                Vector3 Pt(float ax, float lt, float up) => face + axis * ax + latS * lt + Vector3.up * up;
                Vector3 Size(float axLen, float latLen, float hgt) => Mathf.Abs(axis.x) > 0.5f ? new Vector3(axLen, hgt, latLen) : new Vector3(latLen, hgt, axLen);
                float l0 = w * 0.5f + T, l1 = l0 + DuctW, lc = (l0 + l1) * 0.5f;
                string nm = $"Passage {k + 1}";
                Vector3 front = Pt(-1.5f, lc, DuctY + DuctH * 0.5f);
                if (Physics.CheckBox(front, Size(2.6f, DuctW - 0.6f, DuctH - 0.8f) * 0.5f, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    NM($"{nm} : refermé (module devant)", root, Pt(T * 0.5f, lc, DuctY + DuctH * 0.5f), Size(T + 0.2f, DuctW, DuctH + 0.2f), false);
                    NM($"{nm} : refermé (côté couloir)", root, Pt(T + 5.5f, w * 0.5f + T * 0.5f, DuctY + DuctH * 0.5f), Size(3.2f, T + 0.2f, DuctH + 0.2f), false);
                    ClosedPassages.Add(k);
                    continue;
                }

                KitBuild.Box(root, $"{nm} : sol", Pt(T + DuctLen * 0.5f, lc, DuctY - 0.25f), Size(DuctLen, DuctW, 0.5f), KitBuild.Kind.Sol, kit);
                // Plafond à partir de la face extérieure du mur de la salle (pas sous son linteau : faces confondues).
                KitBuild.Box(root, $"{nm} : plafond", Pt(T + DuctLen * 0.5f + 0.5f, lc + 0.5f, DuctY + DuctH + 0.25f), Size(DuctLen + 1f, DuctW + 1f, 0.5f), KitBuild.Kind.Sol, kit, walkable: false);
                KitBuild.Box(root, $"{nm} : mur extérieur", Pt(T + (DuctLen + 1f) * 0.5f, l1 + 0.5f, DuctY + DuctH * 0.5f), Size(DuctLen + 1f, 1f, DuctH + 1f), KitBuild.Kind.Mur, kit, walkable: false);
                KitBuild.Box(root, $"{nm} : mur du fond", Pt(T + DuctLen + 0.5f, lc, DuctY + DuctH * 0.5f), Size(1f, DuctW, DuctH + 1f), KitBuild.Kind.Mur, kit, walkable: false);
                float top = DuctY + DuctH + 1.5f, side = 2.5f, open = DuctW - 0.5f, ac = T + 5.5f;
                NM($"{nm} : plaque (salle)", root, Pt(-0.08f, lc, DuctY * 0.5f), Size(0.12f, open + 2f * side, DuctY), false);
                NM($"{nm} : plaque (salle) haut", root, Pt(-0.08f, lc, (DuctY + DuctH + top) * 0.5f), Size(0.12f, open + 2f * side, top - DuctY - DuctH), false);
                NM($"{nm} : plaque (salle) gauche", root, Pt(-0.08f, lc - (open + side) * 0.5f, DuctY + DuctH * 0.5f), Size(0.12f, side, DuctH), false);
                NM($"{nm} : plaque (salle) droite", root, Pt(-0.08f, lc + (open + side) * 0.5f, DuctY + DuctH * 0.5f), Size(0.12f, side, DuctH), false);
                NM($"{nm} : plaque (couloir)", root, Pt(ac, w * 0.5f - 0.08f, DuctY * 0.5f), Size(9f, 0.12f, DuctY), false);
                // Couloir gothique : la voûte commence à 8 m, la plaque s'arrête à la naissance (sinon elle dépasse devant la voûte).
                float topC = IsGothic(p.Link) ? Mathf.Min(top, GothicSpring) : top;
                NM($"{nm} : plaque (couloir) haut", root, Pt(ac, w * 0.5f - 0.08f, (DuctY + DuctH + topC) * 0.5f), Size(9f, 0.12f, topC - DuctY - DuctH), false);
                NM($"{nm} : plaque (couloir) avant", root, Pt(T + 2.5f, w * 0.5f - 0.08f, DuctY + DuctH * 0.5f), Size(2f, 0.12f, DuctH), false);
                NM($"{nm} : plaque (couloir) arrière", root, Pt(T + 8.5f, w * 0.5f - 0.08f, DuctY + DuctH * 0.5f), Size(2f, 0.12f, DuctH), false);
                if (kit != null && kit.torche != null)
                    KitBuild.Piece(kit.torche, root, Pt(T + 2f, w * 0.5f - 0.35f, DuctY - 1f), Mathf.Atan2(-latS.x, -latS.z) * Mathf.Rad2Deg);
            }
        }

        /// <summary>
        /// Galeries de parcours : parallèles à un couloir plat, derrière son mur (20 m), sans sol (vide : retour au palier, dégâts) ;
        /// portes de 3 m aux deux bouts, paliers et poutres à portée de sa main, plateformes qui s'effondrent et panneaux de course
        /// hors de sa portée ; tout est non magnétique. Le seul endroit où l'on dépasse le traqueur.
        /// </summary>
        /// <summary>
        /// Galeries de parcours (point 4 de carte-suite) : le long d'un couloir plat, ouvertes sur lui (parapet de 1 m que la main du
        /// traqueur passe), au-dessus d'un vide de 10 m. Paliers d'entrée et de sortie au niveau du couloir, puis un parcours exigeant
        /// (ParkourCourse) à 9 m du bord : poutre, marches qui tombent, deux murs (sauts muraux), vide à dash, poteaux, poutre. Tout est
        /// non magnétique ; hors de portée du traqueur, on ne se pose que sur ce qui tombe. Une sur trois environ mène à un coffre.
        /// </summary>
        private void BuildGalleries()
        {
            ParkourCaps caps = ParkourCaps.From(movement);
            for (int k = 0; k < Plan.Galleries.Count; k++)
            {
                PlanGallery g = Plan.Galleries[k];
                Frame(g.Link, true, out Vector3 face, out Vector3 axis, out Vector3 lateral, out float len);
                float w = Plan.Links[g.Link].Width, H = settings.corridorHeight, depth = settings.galleryWaterDepth, pool = 2f;
                Vector3 latS = lateral * g.Side;
                string nm = $"Galerie de parcours {k + 1}";
                var root = new GameObject(nm).transform;
                root.SetParent(_root, false);
                Vector3 Pt(float a, float lt, float up) => face + axis * a + latS * lt + Vector3.up * up;
                Vector3 Size(float axLen, float latLen, float hgt) => Mathf.Abs(axis.x) > 0.5f ? new Vector3(axLen, hgt, latLen) : new Vector3(latLen, hgt, axLen);
                float l0 = w * 0.5f, l1 = l0 + T + GalleryWidth, a0 = 3f, a1 = len - 3f, landing = 5f, line = l0 + 9f;
                NM($"{nm} : mur extérieur", root, Pt((a0 + a1) * 0.5f, l1 + 0.5f, (H - depth - pool) * 0.5f), Size(a1 - a0 + 2f, 1f, H + depth + pool), false);
                // Bouts 2 cm devant les bouts du mur coupé du couloir, plafond 2 cm sous le sien : pas de faces confondues (scintillement).
                NM($"{nm} : bout 1", root, Pt(a0 - 0.49f, (l0 + 0.02f + l1) * 0.5f, (H - depth - pool) * 0.5f), Size(1.02f, l1 - l0 - 0.02f, H + depth + pool), false);
                NM($"{nm} : bout 2", root, Pt(a1 + 0.49f, (l0 + 0.02f + l1) * 0.5f, (H - depth - pool) * 0.5f), Size(1.02f, l1 - l0 - 0.02f, H + depth + pool), false);
                // Couloir gothique : le plafond de la galerie entre dans l'épaisseur de la voûte (bord à 5.4 m de l'axe, entre son
                // intrados et son extrados à 15 m) ; avec un bord à 6 m, une fente restait entre l'extrados et le plafond.
                float inside = IsGothic(g.Link) ? l0 - 1.6f : l0 - 1f;
                NM($"{nm} : plafond", root, Pt((a0 + a1) * 0.5f, (inside + l1 + 1f) * 0.5f, H + 0.48f), Size(a1 - a0 + 2f, l1 + 1f - inside, 1f), false);
                NM($"{nm} : parapet", root, Pt((a0 + a1) * 0.5f, l0 - 0.27f, 0.5f), Size(a1 - a0 - 2f * landing, 0.5f, 1f), false);
                KitBuild.Box(root, $"{nm} : fond du vide", Pt((a0 + a1) * 0.5f, (l0 + l1) * 0.5f, -depth - pool - 0.5f), Size(a1 - a0, l1 - l0, 1f), KitBuild.Kind.Sol, kit, walkable: false);
                // Eau au fond (correctifs-rendu point 3) : surface à depth sous le couloir, 2 m d'eau.
                var waterSurface = GameObject.CreatePrimitive(PrimitiveType.Cube);
                waterSurface.name = $"{nm} : eau";
                Destroy(waterSurface.GetComponent<Collider>());
                waterSurface.transform.SetParent(root, false);
                waterSurface.transform.position = Pt((a0 + a1) * 0.5f, (l0 + l1) * 0.5f, -depth - 0.025f);
                waterSurface.transform.localScale = Size(a1 - a0, l1 - l0, 0.05f);
                if (water != null)
                    waterSurface.GetComponent<MeshRenderer>().sharedMaterial = water;

                // Paliers (au niveau du couloir, à portée) : de la porte du couloir jusqu'à la ligne du parcours.
                var respawns = new List<Transform>();
                foreach ((float a, string tag) in new[] { (a0 + landing * 0.5f, "entrée"), (a1 - landing * 0.5f, "sortie") })
                {
                    NM($"{nm} : palier {tag}", root, Pt(a, (l0 + line + 1f) * 0.5f, -0.5f), Size(landing, line + 1f - l0, 1f));
                    var t = new GameObject($"Reprise {tag}").transform;
                    t.SetParent(root, false);
                    t.position = Pt(a, l0 + 2f, 0.2f);
                    respawns.Add(t);
                }

                // Parcours : de la fin du palier d'entrée au début du palier de sortie, à 9 m du bord du couloir.
                float courseLen = a1 - a0 - 2f * landing;
                var report = new CourseReport { Name = nm, Link = g.Link };
                var rng = new System.Random(Plan.Seed * 977 + k);
                List<ParkourPiece> pieces = null;
                foreach (float height in new[] { 2f, 1f, 0f })
                {
                    bool withChest = g.Chest && height >= 2f;
                    pieces = ParkourCourse.Layout(courseLen, height, -depth, H, withChest ? "palier après les murs" : null, -1f,
                        height + 2.4f, caps, rng, report.Jumps);
                    if (pieces != null)
                    {
                        report.Height = height;
                        report.Chest = withChest;
                        break;
                    }
                }

                report.Length = courseLen;
                if (pieces != null)
                    BuildCourse(root, nm, Pt(a0 + landing, line, 0f), axis, latS, pieces, report);
                Courses.Add(report);

                // Retour au palier seulement au contact de l'eau (avant : dès 1.5 m sous le couloir) : de 0.3 m au-dessus de la surface au fond.
                var trig = new GameObject($"{nm} : eau (retour au palier)");
                trig.transform.SetParent(root, false);
                trig.transform.position = Pt((a0 + a1) * 0.5f, (l0 + l1) * 0.5f, -depth + 0.3f - (pool + 0.3f) * 0.5f);
                var box = trig.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = Size(a1 - a0, l1 - l0, pool + 0.3f);
                trig.AddComponent<FallVoid>().Configure(settings.waterDamage, respawns.ToArray());
                trig.AddComponent<NotWalkable>();
                Volume(root, nm, Pt((a0 + a1) * 0.5f, (l0 + l1) * 0.5f, H * 0.5f), Size(a1 - a0, l1 - l0, H), RoomKind.Parcours);
                if (kit != null && kit.torche != null)
                    for (float a = 12f; a < len - 10f; a += 16f)
                        KitBuild.Piece(kit.torche, root, Pt(a, l1 - 0.35f, 5f), Mathf.Atan2(-latS.x, -latS.z) * Mathf.Rad2Deg);
            }
        }

        /// <summary>
        /// Salles de parcours (point 4) : toute la salle est non magnétique (sol, murs, plafond, objets fixes : la traction y est
        /// refusée partout) et repeinte en gris non magnétique ; l'ancien tracé du module est remplacé par un parcours exigeant en
        /// diagonale de la fosse (ParkourCourse), avec un coffre pour certaines.
        /// </summary>
        private void BuildParkourRooms()
        {
            ParkourCaps caps = ParkourCaps.From(movement);
            for (int i = 0; i < Plan.Rooms.Count; i++)
            {
                PlanRoom r = Plan.Rooms[i];
                if (!r.Parkour || !_moduleInstances.TryGetValue(i, out GameObject inst) || r.Module < 0)
                    continue;
                RoomModule mod = _modules[r.Module];
                Transform room = inst.transform.parent;
                if (room.GetComponent<Orbs.NonMagneticSurface>() == null)
                    room.gameObject.AddComponent<Orbs.NonMagneticSurface>();
                if (nonMagnetic != null)
                    foreach (MeshRenderer mr in room.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        Transform piece = mr.transform;
                        while (piece != null && piece != room && !piece.name.StartsWith("Kit_"))
                            piece = piece.parent;
                        if (piece != null && piece != room && !piece.name.Contains("Torche"))
                            mr.sharedMaterial = nonMagnetic;
                    }

                // Seuils des portes : le couloir commence dans l'épaisseur du mur de la salle ; ces 1.3 m restent non magnétiques.
                foreach (PlanDoor d in r.Doors)
                {
                    if (d.Use != 1)
                        continue;
                    Vector3 o = MapPlanner.SideVector(d.Side);
                    bool alongZ = Mathf.Abs(o.z) > 0.5f;
                    float dw = Plan.Links[d.Link].Width, H = settings.corridorHeight;
                    Vector3 SizeD(float outLen, float across, float hgt) => alongZ ? new Vector3(across, hgt, outLen) : new Vector3(outLen, hgt, across);
                    Vector3 across = alongZ ? Vector3.right : Vector3.forward;
                    // Couloir en pente : sol et plafond du seuil inclinés comme la rampe (montante ou descendante depuis la salle).
                    PlanLink dl = Plan.Links[d.Link];
                    float other = dl.A == i ? dl.To.y : dl.From.y, here = dl.A == i ? dl.From.y : dl.To.y;
                    float tan = dl.Slope ? (other - here) / Mathf.Max(1f, dl.Length) : 0f;
                    Quaternion tilt = Quaternion.FromToRotation(o, (o + Vector3.up * tan).normalized);
                    // Sol et plafond : de 0.25 m dans la salle à 1.3 m dans le couloir (la rampe d'un couloir en pente commence dans
                    // l'embrasure) ; côtés : de 2 cm dans l'embrasure (dans la salle, ils doublaient le mur : scintillement).
                    Vector3 at = d.Position + o * 0.66f + Vector3.up * (tan * 0.66f);
                    Vector3 atFloor = d.Position + o * 0.525f + Vector3.up * (tan * 0.525f);
                    NM($"seuil (sol) {d.Side}", room, atFloor + Vector3.up * 0.03f, SizeD(1.55f, dw, 0.06f)).transform.rotation = tilt;
                    NM($"seuil (plafond) {d.Side}", room, atFloor + Vector3.up * (H - 0.03f), SizeD(1.55f, dw, 0.06f), false).transform.rotation = tilt;
                    foreach (float sgn in new[] { -1f, 1f })
                    {
                        NM($"seuil (côté) {d.Side}", room, at + across * sgn * (dw * 0.5f - 0.03f) + Vector3.up * (H * 0.5f), SizeD(1.28f, 0.06f, H + 1f), false);
                    }

                    // Au-dessus du linteau, côté salle : cache le plafond incliné d'un couloir en pente (magnétique) vu par la porte.
                    NM($"seuil (bout du plafond) {d.Side}", room, d.Position - o * 0.2f + Vector3.up * (H + 1.5f), SizeD(0.06f, dw + 2f * T + 0.4f, 3.2f), false);
                }

                // Passages en hauteur qui débouchent dans cette salle : leurs pièces aussi.
                Transform passages = _root.Find("Passages en hauteur (saut mural)");
                for (int k = 0; passages != null && k < Plan.Passages.Count; k++)
                {
                    PlanPassage pp = Plan.Passages[k];
                    if ((pp.AtStart ? Plan.Links[pp.Link].A : Plan.Links[pp.Link].B) != i)
                        continue;
                    foreach (Transform t in passages)
                        if (t.name.Contains($"Passage {k + 1} :") && t.GetComponent<Orbs.NonMagneticSurface>() == null)
                            t.gameObject.AddComponent<Orbs.NonMagneticSurface>();
                }

                foreach (Transform t in inst.GetComponentsInChildren<Transform>(true).Where(t => t != null && t.name.StartsWith("Parcours :")).ToList())
                    if (t != null)
                        DestroyImmediate(t.gameObject);

                Rect hole = mod.holeRect;
                Vector3 c0 = inst.transform.TransformPoint(new Vector3(hole.xMin, 0f, hole.yMin));
                Vector3 c1 = inst.transform.TransformPoint(new Vector3(hole.xMax, 0f, hole.yMax));
                var rng = new System.Random(Plan.Seed * 613 + i);
                if (rng.NextDouble() < 0.5)
                    (c0, c1) = (inst.transform.TransformPoint(new Vector3(hole.xMax, 0f, hole.yMin)), inst.transform.TransformPoint(new Vector3(hole.xMin, 0f, hole.yMax)));
                Vector3 ax = (c1 - c0).normalized, az = Vector3.Cross(ax, Vector3.up).normalized;
                float length = Vector3.Distance(c0, c1);
                string nm = $"Salle de parcours : {r.Name}";
                var report = new CourseReport { Name = nm, Room = i, Length = length };
                float side = rng.NextDouble() < 0.5 ? 1f : -1f;
                List<ParkourPiece> pieces = null;
                foreach (float height in new[] { 1f, 0f })
                {
                    bool withChest = r.ParkourChest && height >= 1f;
                    pieces = ParkourCourse.Layout(length, height, -mod.holeDepth, r.Ceiling, withChest ? "marche 1" : null, side,
                        height + 2.4f, caps, rng, report.Jumps);
                    if (pieces != null)
                    {
                        report.Height = height;
                        report.Chest = withChest;
                        break;
                    }
                }

                if (pieces != null)
                    BuildCourse(room, nm, c0 + Vector3.up * 0f, ax, az, pieces, report);
                Courses.Add(report);
            }
        }

        /// <summary>Pose les pièces d'un parcours (repère : origine, axe du tracé, côté) ; le socle du coffre reçoit un coffre.</summary>
        private void BuildCourse(Transform root, string nm, Vector3 origin, Vector3 ax, Vector3 az, List<ParkourPiece> pieces, CourseReport report)
        {
            Quaternion rot = Quaternion.LookRotation(ax, Vector3.up);
            foreach (ParkourPiece q in pieces)
            {
                Vector3 at = origin + ax * q.Center.x + az * q.Center.z + Vector3.up * q.Center.y;
                GameObject go = NM($"{nm} : {q.Name}", root, at, new Vector3(q.Size.z, q.Size.y, q.Size.x), !q.Wall);
                go.transform.rotation = rot;
                if (q.Crumble)
                    go.AddComponent<CrumblePlatform>();
                if (q.Chest && chest != null)
                {
                    Vector3 top = at + Vector3.up * (q.Size.y * 0.5f);
                    GameObject c = Instantiate(chest, top, Quaternion.LookRotation(-az * Mathf.Sign(q.Center.z == 0f ? 1f : q.Center.z), Vector3.up), root);
                    c.name = $"Coffre de parcours : {nm}";
                    if (c.TryGetComponent(out Economy.Chest rareChest))
                        rareChest.Rare = true;                          // icône du coffre rare sur la carte
                    c.AddComponent<Orbs.NonMagneticSurface>();          // pas de traction vers lui : on n'y arrive que par le parcours
                }
            }
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
            // Sol et plafond à partir de la face extérieure du mur de la salle hôte : l'embrasure est à la salle (sinon faces confondues).
            if (y > 0.1f)
                KitBuild.Box(parent, "Sol (bloc)", new Vector3(c.x, y * 0.5f, c.z), Size(depth, y, width + 2f * T), KitBuild.Kind.Bloc, kit);
            else
                KitBuild.Box(parent, "Sol", new Vector3(c.x, -0.5f, c.z), Size(depth, 1f, width + 2f * T), KitBuild.Kind.Sol, kit);
            KitBuild.Box(parent, "Plafond", c + o * (T * 0.5f) + Vector3.up * (h + 0.5f), Size(depth + T, 1f, width + 2f * T), KitBuild.Kind.Sol, kit, walkable: false);
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
            // Atelier (« Jouer ce module ») : le joueur part dans la salle du module, sur son sol.
            if (TestStartInModule && !string.IsNullOrEmpty(TestModule))
                for (int i = 0; i < Plan.Rooms.Count; i++)
                {
                    PlanRoom r = Plan.Rooms[i];
                    if (r.Module < 0 || r.Module >= _modules.Count || _modules[r.Module].displayName != TestModule)
                        continue;
                    Vector3 probe = r.Center + new Vector3(r.Extent.x * 0.25f, 40f, r.Extent.y * 0.25f);
                    startAt = Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 80f, ~0, QueryTriggerInteraction.Ignore)
                        ? hit.point + Vector3.up * 0.1f : r.Center + Vector3.up * 0.1f;
                    break;
                }

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
                ChestSpot s = PickSpot(spots);
                GameObject c = Instantiate(chest, s.transform.position, s.transform.rotation, _root);
                c.name = $"Coffre : {Plan.Rooms[r].Name}";
                // Ancre difficile (point 9) : parfois un coffre rare (meilleur butin) au bout de la route la plus dure.
                if (ChestSpot.RareAllowed(s.difficulty) && _rng.NextDouble() < settings.rareChestOnHardSpot
                    && c.TryGetComponent(out Economy.Chest hardChest))
                    hardChest.Rare = true;
                if (Plan.Rooms[r].Parkour)
                    c.AddComponent<Orbs.NonMagneticSurface>();               // salle de parcours : rien n'y est magnétique
            }
        }

        /// <summary>Ancre tirée au hasard, les plus difficiles préférées (poids 1, 2, 3).</summary>
        private ChestSpot PickSpot(ChestSpot[] spots)
        {
            float total = 0f;
            foreach (ChestSpot s in spots)
                total += ChestSpot.Weight(s.difficulty);
            double roll = _rng.NextDouble() * total;
            foreach (ChestSpot s in spots)
            {
                roll -= ChestSpot.Weight(s.difficulty);
                if (roll <= 0)
                    return s;
            }

            return spots[spots.Length - 1];
        }

        private void Residents()
        {
            var parent = new GameObject("Habitants des salles").transform;
            parent.SetParent(_root, false);
            parent.gameObject.SetActive(false);                               // activés avec les systèmes de jeu
            _residents = parent;
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

        /// <summary>Un parcours construit (mesures) : galerie ou salle de parcours, ses sauts vérifiés, son coffre.</summary>
        public sealed class CourseReport
        {
            public string Name;
            public int Link = -1, Room = -1;
            public bool Chest;
            public float Length, Height;
            public readonly List<ParkourJump> Jumps = new List<ParkourJump>();
        }

        /// <summary>Parcours de la carte (galeries et salles de parcours).</summary>
        public readonly List<CourseReport> Courses = new List<CourseReport>();

        /// <summary>Renderers après la fusion (mesures).</summary>
        public int MergedRenderers { get; private set; }
        /// <summary>Pièces gardées hors de la fusion (maillage non lisible).</summary>
        public int KeptPieces { get; private set; }

        /// <summary>
        /// Fusion des pièces du kit (visuel seulement) : pour chaque salle, couloir ou ensemble de premier niveau, un maillage par
        /// matériau, puis les pièces d'origine sont supprimées. Des dizaines de milliers d'objets deviennent quelques centaines :
        /// moins de culling, de bornes à mettre à jour et de draw calls (point 5 de carte-suite). Les prefabs Kit_* restent la source :
        /// remplacer un prefab par le vrai modèle change toujours toute la carte.
        /// </summary>
        /// <summary>Torches retirées (performance, MapLayoutSettings.torchShare) : une sur deux par défaut, dans l'ordre de construction.</summary>
        public int RemovedTorches { get; private set; }

        /// <summary>Torches déplacées ou retirées parce qu'elles touchaient un arc, un pilier ou un encadrement (mesures).</summary>
        public int MovedTorches { get; private set; }
        public int DroppedTorches { get; private set; }

        /// <summary>Préfixes des pièces dont les torches s'écartent : arcs (anneau, écoinçons, piédroits), piliers, colonnes, portes.</summary>
        public static readonly string[] TorchObstacles =
            { "Arc (porte la voûte)", "Kit_Arc", "Kit_Pilier", "Pilier", "Piédroit", "Prop_Colonne", "Colonne", "Kit_PortePiece", "Kit_GrandePorte", "Cadre" };

        private static bool IsTorchObstacle(Transform t)
        {
            for (Transform a = t; a != null; a = a.parent)
                foreach (string p in TorchObstacles)
                    if (a.name.StartsWith(p))
                        return true;
            return false;
        }

        /// <summary>
        /// Torches écartées des arcs, des piliers, des colonnes et des encadrements de porte (retours du lot, point 7) : une torche
        /// à moins de torchClearance glisse le long de son mur (pas de 0.5 m, jusqu'à torchSlide, toujours contre un mur), sinon
        /// elle est retirée. Avant le regroupement du décor (les pièces ont encore leurs propres rendus).
        /// </summary>
        private void ClearTorches()
        {
            MovedTorches = DroppedTorches = 0;
            if (settings == null || settings.torchClearance <= 0f)
                return;
            var obstacles = new List<Bounds>();
            foreach (Renderer r in _root.GetComponentsInChildren<Renderer>(true))
                if (!r.name.StartsWith("Prop_Torche") && IsTorchObstacle(r.transform) && !IsTorchPart(r.transform))
                    obstacles.Add(r.bounds);
            Physics.SyncTransforms();
            foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
            {
                if (!t.name.StartsWith("Prop_Torche") || !t.gameObject.activeInHierarchy)
                    continue;
                if (Clear(TorchBounds(t), obstacles))
                    continue;
                Vector3 home = t.position;
                bool placed = false;
                for (float d = 0.5f; d <= settings.torchSlide + 1e-3f && !placed; d += 0.5f)
                    foreach (float side in new[] { 1f, -1f })
                    {
                        Vector3 at = home + t.right * (side * d);
                        // Toujours contre un mur : derrière la torche (son dos), à moins de 0.8 m.
                        if (!Physics.Raycast(at + t.forward * 0.3f + Vector3.up * 0.3f, -t.forward, 1.1f, ~0, QueryTriggerInteraction.Ignore))
                            continue;
                        t.position = at;
                        if (Clear(TorchBounds(t), obstacles))
                        {
                            placed = true;
                            break;
                        }
                    }

                if (placed)
                    MovedTorches++;
                else
                {
                    t.position = home;
                    t.gameObject.SetActive(false);
                    DroppedTorches++;
                }
            }

            bool Clear(Bounds torch, List<Bounds> list)
            {
                torch.Expand(2f * settings.torchClearance);
                foreach (Bounds b in list)
                    if (b.Intersects(torch))
                        return false;
                return true;
            }
        }

        private static bool IsTorchPart(Transform t)
        {
            for (Transform a = t; a != null; a = a.parent)
                if (a.name.StartsWith("Prop_Torche"))
                    return true;
            return false;
        }

        private static Bounds TorchBounds(Transform torch)
        {
            Renderer[] rs = torch.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
                return new Bounds(torch.position, Vector3.one * 0.3f);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
                b.Encapsulate(r.bounds);
            return b;
        }

        private void ThinTorches()
        {
            RemovedTorches = 0;
            if (settings == null || settings.torchShare >= 0.999f)
                return;
            var torches = new List<Transform>();
            foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
                if (t.name.StartsWith("Prop_Torche"))
                    torches.Add(t);
            float keep = 0f;
            foreach (Transform t in torches)
            {
                keep += settings.torchShare;
                if (keep >= 1f)
                {
                    keep -= 1f;
                    continue;
                }

                t.gameObject.SetActive(false);
                RemovedTorches++;
            }
        }

        private void Batch()
        {
            MergedRenderers = 0;
            KeptPieces = 0;
            foreach (Transform group in _root.Cast<Transform>().ToList())
            {
                if (group.GetComponentInChildren<ExitArenaMarker>(true) != null)
                    continue;                                                 // la grande porte disparaît à l'ouverture : pas fusionnée
                var byMaterial = new Dictionary<Material, List<CombineInstance>>();
                var pieces = new List<GameObject>();
                foreach (Transform t in group.GetComponentsInChildren<Transform>(true))
                {
                    bool piece = t.name.StartsWith("Kit_") || GothicMerged.Contains(t.name);
                    if (!piece || (t.parent != null && (t.parent.name.StartsWith("Kit_") || GothicMerged.Contains(t.parent.name))))
                        continue;
                    // Maillage non lisible (modèle importé sans Read/Write) : Unity ne peut pas le combiner ; la pièce reste telle
                    // quelle au lieu de disparaître (cause des arcs invisibles, voir LevelModelImport).
                    bool readable = true;
                    foreach (MeshFilter f in t.GetComponentsInChildren<MeshFilter>(true))
                        readable &= f.sharedMesh == null || f.sharedMesh.isReadable;
                    if (!readable)
                    {
                        KeptPieces++;
                        continue;
                    }

                    pieces.Add(t.gameObject);
                    foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null || !r.enabled)
                            continue;
                        Matrix4x4 m = group.worldToLocalMatrix * r.transform.localToWorldMatrix;
                        Material[] mats = r.sharedMaterials;
                        for (int s = 0; s < mf.sharedMesh.subMeshCount && s < mats.Length; s++)
                        {
                            if (mats[s] == null)
                                continue;
                            if (!byMaterial.TryGetValue(mats[s], out List<CombineInstance> list))
                                byMaterial[mats[s]] = list = new List<CombineInstance>();
                            list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = m });
                        }
                    }
                }

                if (pieces.Count == 0)
                    continue;
                foreach (KeyValuePair<Material, List<CombineInstance>> kv in byMaterial)
                {
                    var go = new GameObject($"Décor fusionné ({kv.Key.name})");
                    go.transform.SetParent(group, false);
                    var mesh = new Mesh { name = go.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = kv.Key;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    MergedRenderers++;
                }

                foreach (GameObject p in pieces)
                    Destroy(p);
            }
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
            go.AddComponent<AmbianceLight>().kind = AmbianceLight.Kind.Lampe;   // intensité et portée de l'ambiance de la scène (D-067)
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
