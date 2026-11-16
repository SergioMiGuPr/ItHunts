using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Test_Arena (étape 6, D-035) : labyrinthe en greybox, à l'échelle du mouvement et du traqueur géant. Dimensions dans
    /// ArenaLayout_Test (Assets/_Project/Settings/Editor). Plan (vue de dessus, +z = nord, grille de 100 m) :
    ///
    ///   NO (salle haute) ── N (carrefour haut) ── NE (salle haute, SORTIE)      niveau haut : +16 m
    ///        │ pente 15°            │ pente 15° (glissade)   │ pente 14°
    ///   O (carrefour) ──── C (salle centrale) ──── E (carrefour) ── repaire     niveau du sol
    ///        │                      │                       │
    ///   SO (salle, DÉPART) ── S (carrefour) ───── SE (salle)
    ///
    /// 12 couloirs de 14 × 19 m, tous dans une boucle ; lignes droites de 145 à 255 m ; ponts et poutres à 10 m du sol (le
    /// traqueur se baisse dessous), corniches à 11 m ; 6 pièces fermées (portes de 3 m, fenêtres) où il passe le bras.
    /// Règle du traqueur : tout point où l'on tient debout est à portée de sa main (mesuré en Play Mode).
    /// </summary>
    public static partial class TestArenaSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Test_Arena.unity";
        private const string LayoutPath = SettingsDir + "/Editor/ArenaLayout_Test.asset";
        private const string GhoulPath = SettingsDir + "/Ghoul_Default.asset";
        private const string FlyerPath = SettingsDir + "/Flyer_Default.asset";
        private const string WavesPath = SettingsDir + "/Waves_Default.asset";
        private const string EconomyPath = SettingsDir + "/Economy_Default.asset";
        private const string UpgradePoolPath = SettingsDir + "/UpgradePool_Default.asset";
        private const string UpgradesDir = SettingsDir + "/Upgrades";
        private const string BarrelPoolPath = SettingsDir + "/UpgradePool_Barillet.asset";
        private static EconomySettings _economy;
        private static Material _offerOrb;
        private const string StalkerPath = SettingsDir + "/Stalker_Default.asset";
        private const string ZonePath = SettingsDir + "/Zone_Test.asset";
        private const string PoolPath = SettingsDir + "/ArtifactPool_Default.asset";
        private const string DamageFeedbackPath = SettingsDir + "/DamageFeedback_Default.asset";
        private const float T = 1f;                 // épaisseur des murs
        private const int SideN = 0, SideS = 1, SideE = 2, SideW = 3;

        private static ArenaLayoutSettings L;
        private static Material _floor, _wall, _ceiling, _platform, _ramp, _wallRun, _magnetic, _crate, _heavy, _ghoulSkin, _bridge, _room;
        private static GhoulSettings _ghoul;
        private static FlyerSettings _flyer;
        private static int _flyerCount;
        private static Transform _level, _ghouls;
        private static int _crateCount, _ghoulCount, _spawnCount;
        private static Transform _spawns;

        private struct Node
        {
            public string Name;
            public Vector3 C;       // centre, au niveau du sol de l'intersection
            public float Half;
            public float HalfZ;
            public float Ceil;
            public bool Hall;
        }

        private struct Opening
        {
            public float A, B, Y0, Y1;
            public Opening(float a, float b, float y0, float y1)
            {
                A = a;
                B = b;
                Y0 = y0;
                Y1 = y1;
            }
        }

        private static readonly List<Node> Nodes = new List<Node>();
        private static readonly List<Vector2Int> Edges = new List<Vector2Int>();
        private static readonly Dictionary<(int, int), List<Opening>> NodeOpenings = new Dictionary<(int, int), List<Opening>>();
        private static readonly Dictionary<(int, int), List<Opening>> CorridorOpenings = new Dictionary<(int, int), List<Opening>>();
        /// <summary>Canaux d'égout (D-052) : salle → (milieu du canal en z relatif au centre, largeur).</summary>
        private static readonly Dictionary<int, Vector2> Canals = new Dictionary<int, Vector2>();

        [MenuItem("ItHunts/Reconstruire la scène Test_Arena (labyrinthe)")]
        private static void BuildFromMenu() => Debug.Log(Build());

        public static string Build()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
                return "Prefab joueur absent : lancer d'abord ItHunts > Reconstruire le prefab joueur et la scène Test_Movement.";
            var scene = BeginScene(LayoutPath, out StalkerSettings stalkerSettings);
            _startNode = 0;
            _exitNode = 8;
            _lairNode = 9;
            DefineMaze();
            DefineRooms(out List<RoomDef> rooms);
            for (int i = 0; i < Nodes.Count; i++)
                BuildNode(i);
            for (int e = 0; e < Edges.Count; e++)
                if (!IsLairEdge(e))
                    Corridor(e);
            foreach (RoomDef r in rooms)
                BuildRoom(r);
            Halls();
            CorridorFeatures();
            EconomyContent();

            float S = L.gridSpacing;
            Vector3 min = new Vector3(-S - 50f, -5f, -S - 50f), max = new Vector3(S + 50f, L.upperLevel + L.hallCeiling + 10f, S + 50f);
            Node sw = Nodes[0];
            FinishScene(stalkerSettings, LoadOrCreate<ZoneSettings>(ZonePath), min, max,
                sw.C + new Vector3(-12f, 0f, -10f), Quaternion.Euler(0f, 45f, 0f), playerPrefab);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            return $"OK : {ScenePath} (labyrinthe {Nodes.Count} intersections, {Edges.Count} couloirs, {_spawnCount} points d'apparition, " +
                   $"réserve de {_ghoulCount} goules et {_flyerCount} volants, {_crateCount} objets ; murs grimpables trouvés au lancement)";
        }

        /// <summary>Indices des nœuds : salle de départ, salle de la sortie, repaire du traqueur (cul-de-sac).</summary>
        private static int _startNode, _exitNode, _lairNode;

        /// <summary>Nouvelle scène : réglages, matériaux, lumière d'ambiance, post-process, racines du niveau.</summary>
        private static UnityEngine.SceneManagement.Scene BeginScene(string layoutPath, out StalkerSettings stalkerSettings)
        {
            EnsureFolder(SettingsDir + "/Editor");
            InitDefaults();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // Rechargés après NewScene : la nouvelle scène décharge les assets chargés avant.
            L = LoadOrCreate<ArenaLayoutSettings>(layoutPath);
            _ghoul = LoadOrCreate<GhoulSettings>(GhoulPath);
            _flyer = LoadOrCreate<FlyerSettings>(FlyerPath);
            stalkerSettings = LoadOrCreate<StalkerSettings>(StalkerPath);
            Materials();

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = L.ambient;
            var moon = new GameObject("Lumière d'ambiance").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.intensity = 0.25f;
            moon.color = new Color(0.6f, 0.7f, 1f);
            moon.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
            var volume = new GameObject("Post-process (bloom)").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = LoadOrCreatePostProcess();
            _level = new GameObject("Niveau").transform;
            _spawns = new GameObject("Points d'apparition (vagues)").transform;
            _crateCount = _ghoulCount = _flyerCount = _spawnCount = 0;
            _economy = LoadOrCreate<EconomySettings>(EconomyPath);
            _offerOrb = UnlitMat("Economy_Offer_Orb", "Universal Render Pipeline/Unlit", new Color(1f, 0.8f, 0.3f));
            return scene;
        }

        /// <summary>
        /// Fin commune : sortie et escaliers, graphe du labyrinthe, NavMesh, zone, run, retour des dégâts, traqueur dans son
        /// repaire, réserve des vagues, joueur au départ.
        /// </summary>
        private static Vector3 FinishScene(StalkerSettings stalkerSettings, ZoneSettings zoneSettings, Vector3 navMin, Vector3 navMax,
            Vector3 startAt, Quaternion startRotation, GameObject playerPrefab)
        {
            // Sortie (D-048) : grande porte dans le mur nord de la salle de sortie, escaliers, palier de sortie.
            Vector3 exitAt = ExitStairs(Nodes[_exitNode]);
            Bounds arena = BossArena(exitAt);                             // combat de boss (D-049), derrière le palier
            navMin = Vector3.Min(navMin, arena.min);
            navMax = Vector3.Max(navMax, arena.max);

            // Graphe du labyrinthe (règle de Pac-Man du traqueur).
            var graph = new GameObject("Graphe du labyrinthe").AddComponent<MazeGraph>();
            var names = new string[Nodes.Count];
            var centers = new Vector3[Nodes.Count];
            var halves = new Vector3[Nodes.Count];
            for (int i = 0; i < Nodes.Count; i++)
            {
                names[i] = Nodes[i].Name;
                centers[i] = Nodes[i].C;
                halves[i] = new Vector3(Nodes[i].Half, Nodes[i].Ceil * 0.5f, Nodes[i].HalfZ);
            }

            graph.Configure(names, centers, halves, Edges.ToArray());
            EditorUtility.SetDirty(graph);

            var nav = new GameObject("NavMesh de la zone").AddComponent<ZoneNavMesh>();
            nav.transform.position = (navMin + navMax) * 0.5f;
            var nso = new SerializedObject(nav);
            nso.FindProperty("size").vector3Value = navMax - navMin;
            nso.FindProperty("stalkerRadius").floatValue = stalkerSettings.width * 0.5f;
            nso.FindProperty("stalkerHeight").floatValue = stalkerSettings.crouchMinHeight;   // il se baisse jusqu'à 9 m
            nso.FindProperty("smallVoxel").floatValue = 0.2f;                                  // portes de 3 m : 0.2 m suffit
            nso.FindProperty("stalkerVoxel").floatValue = 0.3f;                                // voies de 8 m des pentes : 1 m de NavMesh pour lui
            nso.ApplyModifiedPropertiesWithoutUndo();
            var start = new GameObject("Départ des joueurs").transform;
            start.SetPositionAndRotation(startAt, startRotation);
            var zone = new GameObject("État de la zone").AddComponent<ZoneState>();
            SetRef(zone, "settings", zoneSettings);
            SetRef(zone, "playerStart", start);
            var runState = new GameObject("État de la run").AddComponent<RunState>();
            SetRef(runState, "pool", LoadOrCreate<ArtifactPool>(PoolPath));
            SetRef(runState, "characters", CharacterSetup.Roster());    // personnages (D-064)
            RenderSetup.AddTo(null);                                     // ambiance de rendu par défaut : sombre (D-067, correctifs-rendu)
            SetRef(runState, "upgrades", UpgradePoolAsset());
            new GameObject("Minimap (brouillard de guerre)").AddComponent<MapDiscovery>().gameObject.AddComponent<ItHunts.Hud.MinimapHud>();
            new GameObject("Mesure de performance du niveau (build, -ithunts-perf-level)").AddComponent<ItHunts.Debugging.LevelPerfProbe>();
            var split = new GameObject("Écran partagé (2e joueur : Start sur sa manette)").AddComponent<ItHunts.Player.SplitScreen>();
            SetRef(split, "playerPrefab", playerPrefab);
            SetRef(split, "settings", LoadOrCreate<CoopSettings>(SettingsDir + "/Coop_Default.asset"));
            var feedback = new GameObject("Retour des dégâts").AddComponent<DamageFeedback>();
            SetRef(feedback, "settings", LoadOrCreate<DamageFeedbackSettings>(DamageFeedbackPath));
            SetRef(feedback, "flashMaterial", UnlitMat("Damage_Flash", "Universal Render Pipeline/Unlit", Color.white));
            SetRef(feedback, "lineMaterial", UnlitMat("Orb_Line", "Universal Render Pipeline/Particles/Unlit", Color.white));

            Node lair = Nodes[_lairNode];
            BuildStalker(stalkerSettings, lair.C + new Vector3(lair.Half * 0.3f, 0f, 0f));

            // Vagues (D-041) : réserve de goules et de volants inactifs (sous le niveau), réutilisés par le directeur des vagues.
            var waves = LoadOrCreate<WaveSettings>(WavesPath);
            // Réserve à la taille de la carte (densité par surface, étape 8) : × la même échelle que le nombre voulu.
            float areaScale = WaveDirector.AreaScaleFor(WaveDirector.MeasureMapArea(), waves);
            int ghoulReserve = Mathf.CeilToInt(waves.ghoulPool * Mathf.Max(1f, areaScale));
            int flyerReserve = Mathf.CeilToInt(waves.flyerPool * Mathf.Max(1f, areaScale));
            var ghoulPool = new GameObject("Réserve des vagues : goules").transform;
            var flyerPool = new GameObject("Réserve des vagues : volants").transform;
            _ghouls = ghoulPool;
            for (int i = 0; i < ghoulReserve; i++)
                BuildGhoul(new Vector3(i * 2f, -60f, 0f), Quaternion.identity, $"(réserve) {i + 1}");
            _ghouls = flyerPool;
            for (int i = 0; i < flyerReserve; i++)
                BuildFlyer(new Vector3(i * 2f, -60f, 10f), $"(réserve) {i + 1}");
            foreach (Transform t in ghoulPool)
                t.gameObject.SetActive(false);
            foreach (Transform t in flyerPool)
                t.gameObject.SetActive(false);
            var director = new GameObject("Vagues (directeur)").AddComponent<WaveDirector>();
            SetRef(director, "settings", waves);
            SetRef(director, "ghoulPool", ghoulPool);
            SetRef(director, "flyerPool", flyerPool);
            SetRef(director, "telegraphMaterial", UnlitMat("Wave_Spawn_Telegraph", "Universal Render Pipeline/Unlit", new Color(3f, 0.6f, 0.15f)));

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetPositionAndRotation(start.position, start.rotation);
            return exitAt;
        }

        /// <summary>Valeurs du traqueur géant (D-034, D-035) et de la goule sur les assets existants.</summary>
        private static void InitDefaults()
        {
            var g = LoadOrCreate<GhoulSettings>(GhoulPath);
            if (g.maxHealth == 100f && g.mass == 80f)
            {
                g.maxHealth = 60f;
                g.mass = 50f;
                g.respawnDelay = 0f;
                EditorUtility.SetDirty(g);
            }

            var f = LoadOrCreate<FlyerSettings>(FlyerPath);
            if (f.maxHealth == 100f && f.mass == 80f)
            {
                // Volant (D-033, D-040) : 25 PV, 8 kg, ne réapparaît pas seul.
                f.maxHealth = 25f;
                f.mass = 8f;
                f.respawnDelay = 0f;
                f.stunDuration = 1.5f;
                EditorUtility.SetDirty(f);
            }

            var s = LoadOrCreate<StalkerSettings>(StalkerPath);
            if (s.height < 10f)
            {
                s.width = 7f;
                s.height = 14f;
                s.eyeHeight = 12.5f;
                s.shoulderHeight = 12f;
                s.handReach = 15f;
                s.sightRange = 60f;
                s.soundRange = 60f;
                s.attackRange = 0.3f;
                s.followPlayerSpeedShare = 0f;
                s.stepsPerSecondAtChase = 0.9f;
                EditorUtility.SetDirty(s);
            }

            if (s.mass < 1000f)
            {
                s.mass = 1000f;
                s.maxHealth = 1f;
                s.respawnDelay = 0f;
                EditorUtility.SetDirty(s);
            }

            AssetDatabase.SaveAssets();
        }

        private static void Materials()
        {
            _floor = Mat("Arena_Floor", new Color(0.16f, 0.16f, 0.18f));
            _wall = Mat("Arena_Wall", new Color(0.24f, 0.23f, 0.27f));
            _ceiling = Mat("Arena_Ceiling", new Color(0.1f, 0.1f, 0.12f));
            _platform = Mat("Greybox_Platform", new Color(0.32f, 0.42f, 0.55f));
            _ramp = Mat("Greybox_Ramp", new Color(0.55f, 0.42f, 0.3f));
            _wallRun = Mat("Greybox_TallWall", new Color(0.25f, 0.32f, 0.45f));
            _magnetic = GrabbableMat("Greybox_Magnetic", new Color(0.15f, 0.45f, 1f));
            _magnetic.SetColor("_EmissionColor", new Color(0.1f, 0.35f, 1.2f));
            _bridge = Mat("Arena_Bridge", new Color(0.42f, 0.36f, 0.3f));
            _room = Mat("Arena_Room", new Color(0.3f, 0.22f, 0.2f));
            _crate = GrabbableMat("Greybox_Grabbable", new Color(0.55f, 0.42f, 0.28f));
            _heavy = GrabbableMat("Greybox_Grabbable_Lourd", new Color(0.35f, 0.3f, 0.28f));
            _ghoulSkin = GrabbableMat("Greybox_Ghoul", new Color(0.16f, 0.22f, 0.14f));
        }

        // ---------- Plan ----------

        private static void DefineMaze()
        {
            Nodes.Clear();
            Edges.Clear();
            NodeOpenings.Clear();
            CorridorOpenings.Clear();
            Canals.Clear();
            NodeModules.Clear();
            Pits.Clear();
            float S = L.gridSpacing, U = L.upperLevel, X = L.crossingSize * 0.5f, CC = L.crossingCeiling, HC = L.hallCeiling;
            AddNode("SO (départ)", new Vector3(-S, 0f, -S), L.startHallSize * 0.5f, HC, true);           // 0
            AddNode("S (carrefour)", new Vector3(0f, 0f, -S), X, CC, false);                              // 1
            AddNode("SE", new Vector3(S, 0f, -S), L.southEastHallSize * 0.5f, HC, true);                 // 2
            AddNode("O (carrefour)", new Vector3(-S, 0f, 0f), X, CC, false);                              // 3
            AddNode("C (centrale)", Vector3.zero, L.centralHallSize * 0.5f, HC, true);                   // 4
            AddNode("E (carrefour)", new Vector3(S, 0f, 0f), X, CC, false);                               // 5
            AddNode("NO (haute)", new Vector3(-S, U, S), L.northWestHallSize * 0.5f, HC, true);          // 6
            AddNode("N (carrefour haut)", new Vector3(0f, U, S), X, CC, false);                           // 7
            AddNode("NE (haute, sortie)", new Vector3(S, U, S), L.exitHallSize * 0.5f, HC, true);        // 8
            float depth = L.lairDepth;
            Nodes.Add(new Node
            {
                Name = "Repaire", C = new Vector3(S + X + depth * 0.5f, 0f, 0f), Half = depth * 0.5f, HalfZ = X, Ceil = L.lairHeight,
            });                                                                                           // 9
            int[,] links = { { 0, 1 }, { 1, 2 }, { 3, 4 }, { 4, 5 }, { 6, 7 }, { 7, 8 }, { 0, 3 }, { 3, 6 }, { 1, 4 }, { 4, 7 }, { 2, 5 }, { 5, 8 }, { 5, 9 } };
            for (int i = 0; i < links.GetLength(0); i++)
                Edges.Add(new Vector2Int(links[i, 0], links[i, 1]));

            // Porte de sortie (D-048) : ouverture de 10 × 10 m au milieu du mur nord de la salle NE.
            AddExitOpening(8);

            // Ouvertures des couloirs dans les murs des intersections.
            float h = L.corridorHeight;
            for (int e = 0; e < Edges.Count; e++)
            {
                float w = Width(e);
                Node a = Nodes[Edges[e].x], b = Nodes[Edges[e].y];
                int sa = SideOf(b.C - a.C), sb = SideOf(a.C - b.C);
                float along = sa == SideE || sa == SideW ? a.C.z : a.C.x;
                AddOpening(Edges[e].x, sa, new Opening(along - w / 2f, along + w / 2f, a.C.y, a.C.y + h));
                if (!IsLairEdge(e))
                    AddOpening(Edges[e].y, sb, new Opening(along - w / 2f, along + w / 2f, b.C.y, b.C.y + h));
            }
        }

        private const float ExitDoorWidth = 10f, ExitDoorHeight = 10f;

        /// <summary>Porte de sortie (D-048) : ouverture de 10 × 10 m au milieu du mur nord de la salle (côté nord libre).</summary>
        private static void AddExitOpening(int node)
        {
            Node n = Nodes[node];
            AddOpening(node, SideN, new Opening(n.C.x - ExitDoorWidth / 2f, n.C.x + ExitDoorWidth / 2f, n.C.y, n.C.y + ExitDoorHeight));
        }

        /// <summary>
        /// Matériau greybox des surfaces non magnétiques (D-051) : pierre gris pâle à rayures diagonales sombres, sans lueur (les
        /// cibles de traction sont bleues et brillent ; celles-ci, jamais).
        /// </summary>
        public static Material NonMagneticMat()
        {
            const string texPath = MaterialDir + "/NonMagnetic_Stripes.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    t.SetPixel(x, y, ((x + y) / 8) % 2 == 0 ? new Color(0.78f, 0.76f, 0.72f) : new Color(0.42f, 0.4f, 0.38f));
                System.IO.File.WriteAllBytes(texPath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }

            Material m = Mat("Greybox_NonMagnetic", Color.white);
            m.SetTexture("_BaseMap", tex);
            m.SetTextureScale("_BaseMap", new Vector2(4f, 4f));
            m.DisableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Pièce non magnétique (D-051) : la traction ne s'y accroche pas.</summary>
        public static GameObject NonMagneticPiece(string name, Transform parent, Vector3 center, Vector3 size, bool walkable = true)
        {
            GameObject go = Box($"Non magnétique : {name}", parent, center, size, NonMagneticMat());
            go.AddComponent<ItHunts.Orbs.NonMagneticSurface>();
            if (!walkable)
                go.AddComponent<NotWalkable>();
            return go;
        }

        /// <summary>
        /// Sortie du niveau (D-048) : grande porte (rouge verrouillée, orange payable) dans le mur nord de la salle, escaliers de
        /// 20 marches (8 m de montée sur 32 m), palier avec le déclencheur de sortie. Le combat de boss (D-049) se branche derrière.
        /// </summary>
        private static Vector3 ExitStairs(Node n)
        {
            float x = n.C.x, z0 = n.C.z + n.HalfZ + T, y0 = n.C.y, w = ExitDoorWidth;
            Material locked = GrabbableMat("Exit_Door_Locked", new Color(0.35f, 0.08f, 0.06f));
            locked.SetColor("_EmissionColor", new Color(1.4f, 0.1f, 0.05f));
            Material open = GrabbableMat("Exit_Door_Unlocked", new Color(0.45f, 0.25f, 0.08f));
            open.SetColor("_EmissionColor", new Color(2.2f, 1.1f, 0.2f));
            GameObject door = Box("Porte de sortie", _level, new Vector3(x, y0 + ExitDoorHeight / 2f, z0 - T / 2f), new Vector3(w + 0.4f, ExitDoorHeight, T + 0.6f), locked);
            GameObjectUtility.SetStaticEditorFlags(door, 0);
            var exitDoor = door.AddComponent<ItHunts.Level.ExitDoor>();
            SetRef(exitDoor, "unlockedMaterial", open);
            Lamp("Porte de sortie : lumière", new Vector3(x, y0 + ExitDoorHeight + 2f, z0 - 4f), new Color(1f, 0.4f, 0.2f), 4f, 20f);

            const int steps = 20;
            const float rise = 0.4f, depth = 1.6f, headroom = 14f;
            float zTop = z0 + steps * depth, yTop = y0 + steps * rise, landing = 12f;
            for (int i = 0; i < steps; i++)
            {
                float top = y0 + rise * (i + 1);
                Piece($"Escaliers de sortie : marche {i + 1}", new Vector3(x, top / 2f, z0 + depth * (i + 0.5f)), new Vector3(w, top, depth), _bridge);
            }

            Piece("Escaliers de sortie : palier", new Vector3(x, yTop / 2f, zTop + landing / 2f), new Vector3(w, yTop, landing), _floor);
            float zEnd = zTop + landing, len = zEnd - z0, yWall = yTop + headroom;
            Piece("Escaliers de sortie : mur ouest", new Vector3(x - w / 2f - T / 2f, yWall / 2f, z0 + len / 2f), new Vector3(T, yWall, len), _wall, walkable: false);
            Piece("Escaliers de sortie : mur est", new Vector3(x + w / 2f + T / 2f, yWall / 2f, z0 + len / 2f), new Vector3(T, yWall, len), _wall, walkable: false);
            Piece("Escaliers de sortie : plafond", new Vector3(x, yWall + 0.5f, z0 + len / 2f), new Vector3(w + 2f * T, 1f, len), _ceiling, walkable: false);
            // Pas de mur du fond : le palier s'ouvre sur l'arène du combat de boss (D-049).
            for (int i = 0; i < 3; i++)
                Lamp($"Escaliers de sortie : lumière {i + 1}", new Vector3(x, y0 + rise * steps * (i + 1) / 3f + 6f, z0 + len * (i + 0.5f) / 3f),
                    new Color(1f, 0.75f, 0.45f), 4f, 22f);

            // Palier : déclencheur de sortie (combat de boss ensuite, D-049).
            Material exitMat = UnlitMat("Arena_Exit", "Universal Render Pipeline/Unlit", new Color(0.3f, 3f, 0.8f));
            Vector3 exitAt = new Vector3(x, yTop, zTop + landing / 2f);
            GameObject exit = Box("Sortie (haut des escaliers)", _level, exitAt + Vector3.up * 0.05f, new Vector3(6f, 0.1f, 6f), exitMat, collider: false);
            var trigger = exit.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 12f, 0f);                 // boîte aplatie (0.1 m) : 6 × 2.4 × 6 m dans le monde
            trigger.size = new Vector3(1f, 24f, 1f);
            exit.AddComponent<ZoneExit>();
            Lamp("Lumière de la sortie", exitAt + Vector3.up * 3f, new Color(0.3f, 1f, 0.5f), 4f, 15f);
            return exitAt;
        }

        /// <summary>Couloir en pente (deux niveaux) : plus large, une voie en pente et une voie au sol qui finit au pied d'un mur.</summary>
        private static bool IsSlope(int e) => !Mathf.Approximately(Nodes[Edges[e].x].C.y, Nodes[Edges[e].y].C.y);

        private static float Width(int e) => IsSlope(e) ? L.slopeCorridorWidth : L.corridorWidth;

        private static void AddNode(string name, Vector3 c, float half, float ceil, bool hall) =>
            Nodes.Add(new Node { Name = name, C = c, Half = half, HalfZ = half, Ceil = ceil, Hall = hall });

        private static bool IsLairEdge(int e) => Edges[e].y == _lairNode;

        private static int SideOf(Vector3 d) =>
            Mathf.Abs(d.x) > Mathf.Abs(d.z) ? (d.x > 0f ? SideE : SideW) : (d.z > 0f ? SideN : SideS);

        private static void AddOpening(int node, int side, Opening o)
        {
            if (!NodeOpenings.TryGetValue((node, side), out List<Opening> list))
                NodeOpenings[(node, side)] = list = new List<Opening>();
            list.Add(o);
        }

        private static List<Opening> Openings(int node, int side) =>
            NodeOpenings.TryGetValue((node, side), out List<Opening> list) ? list : new List<Opening>();

        // ---------- Pièces de base ----------

        private static GameObject Piece(string name, Vector3 center, Vector3 size, Material mat, bool walkable = true, bool collider = true)
        {
            GameObject go = Box(name, _level, center, size, mat, collider);
            if (!walkable)
                go.AddComponent<NotWalkable>();
            return go;
        }

        private static void Lamp(string name, Vector3 at, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_level, false);
            go.transform.localPosition = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
            l.shadows = LightShadows.None;
        }

        /// <summary>
        /// Mur droit (le long de x si alongX, sinon de z), de <paramref name="from"/> à <paramref name="to"/>, entre yb et yt,
        /// percé d'ouvertures (début, fin le long du mur ; bas, haut) : allège sous l'ouverture, linteau au-dessus.
        /// </summary>
        private static void WallLine(string name, bool alongX, float fixedCoord, float from, float to, float yb, float yt, List<Opening> openings)
        {
            var cuts = new List<Opening>(openings ?? new List<Opening>());
            cuts.Sort((p, q) => p.A.CompareTo(q.A));
            float cursor = from;
            int i = 0;

            void Block(float a, float b, float y0, float y1, string label, bool walkable = false)
            {
                if (b - a < 0.01f || y1 - y0 < 0.01f)
                    return;
                Vector3 c = alongX ? new Vector3((a + b) / 2f, (y0 + y1) / 2f, fixedCoord) : new Vector3(fixedCoord, (y0 + y1) / 2f, (a + b) / 2f);
                Vector3 s = alongX ? new Vector3(b - a, y1 - y0, T) : new Vector3(T, y1 - y0, b - a);
                Piece($"{name} {++i}{label}", c, s, _wall, walkable);
            }

            foreach (Opening o in cuts)
            {
                if (o.A > cursor)
                    Block(cursor, o.A, yb, yt, "");
                Block(Mathf.Max(o.A, cursor), o.B, o.Y1, yt, " (linteau)");
                Block(Mathf.Max(o.A, cursor), o.B, yb, o.Y0, " (allège)", walkable: true);   // seuil : on y marche
                cursor = Mathf.Max(cursor, o.B);
            }

            if (cursor < to)
                Block(cursor, to, yb, yt, "");
        }

        /// <summary>Intersection : sol (bloc plein jusqu'au sol si elle est en hauteur), plafond, quatre murs percés, lumières.</summary>
        private static void BuildNode(int i)
        {
            Node n = Nodes[i];
            float hx = n.Half, hz = n.HalfZ, y = n.C.y, top = y + n.Ceil;
            Vector3 c = n.C;
            if (Canals.TryGetValue(i, out Vector2 cut))
                CanalFloor(n, cut.x, cut.y);
            else if (Pits.TryGetValue(i, out RoomModule pit))
                PitFloor(n, pit);
            else if (y > 0.1f)
                Piece($"{n.Name} : sol (bloc de {y:0} m)", new Vector3(c.x, y / 2f, c.z), new Vector3(2f * hx, y, 2f * hz), _floor);
            else
                Piece($"{n.Name} : sol", new Vector3(c.x, -0.5f, c.z), new Vector3(2f * hx + 2f * T, 1f, 2f * hz + 2f * T), _floor);
            Piece($"{n.Name} : plafond", new Vector3(c.x, top + 0.5f, c.z), new Vector3(2f * hx + 2f * T, 1f, 2f * hz + 2f * T), _ceiling, walkable: false);
            bool lair = i == _lairNode;
            WallLine($"{n.Name} : mur nord", true, c.z + hz + T / 2f, c.x - hx - T, c.x + hx + T, 0f, top, Openings(i, SideN));
            WallLine($"{n.Name} : mur sud", true, c.z - hz - T / 2f, c.x - hx - T, c.x + hx + T, 0f, top, Openings(i, SideS));
            WallLine($"{n.Name} : mur est", false, c.x + hx + T / 2f, c.z - hz, c.z + hz, 0f, top, Openings(i, SideE));
            if (!lair)                                                   // le repaire s'ouvre dans le mur est du carrefour E
                WallLine($"{n.Name} : mur ouest", false, c.x - hx - T / 2f, c.z - hz, c.z + hz, 0f, top, Openings(i, SideW));
            RoomVolumeAt(n.Name, new Vector3(c.x, y + n.Ceil / 2f, c.z), Quaternion.identity, new Vector3(2f * hx, n.Ceil, 2f * hz),
                i == _startNode ? RoomKind.Depart : lair ? RoomKind.Salle : RoomKind.Salle);   // salle de départ (D-048)

            if (lair)
            {
                Lamp($"{n.Name} : lumière", c + Vector3.up * (n.Ceil - 3f), new Color(1f, 0.15f, 0.1f), 5f, 30f);
                return;
            }

            if (!n.Hall)
            {
                Lamp($"{n.Name} : lumière", c + Vector3.up * (n.Ceil - 2f), new Color(1f, 0.8f, 0.6f), 5f, 35f);
                return;
            }

            int grid = NodeModules.ContainsKey(i) ? 2 : 3;                // salle à module : 4 grandes lampes, ses torches font le reste
            for (int a = 0; a < grid; a++)
            for (int b = 0; b < grid; b++)
            {
                float fx = grid == 3 ? (a - 1) * 0.6f : (a - 0.5f) * 0.9f, fz = grid == 3 ? (b - 1) * 0.6f : (b - 0.5f) * 0.9f;
                Lamp($"{n.Name} : lumière {a * 3 + b + 1}", c + new Vector3(fx * hx, n.Ceil * 0.75f, fz * hz),
                    (a + b) % 2 == 0 ? new Color(1f, 0.78f, 0.55f) : new Color(0.75f, 0.82f, 1f), L.lightIntensity, L.lightRange);
            }
        }

        /// <summary>Volume de salle (D-045) : pas d'apparition d'ennemi dans la salle d'un joueur ; à poser de même à la main.</summary>
        private static RoomVolume RoomVolumeAt(string name, Vector3 center, Quaternion rotation, Vector3 size, RoomKind kind = RoomKind.Salle)
        {
            var go = new GameObject($"Volume de salle : {name}");
            go.transform.SetParent(_level, false);
            go.transform.SetPositionAndRotation(center, rotation);
            RoomVolume v = go.AddComponent<RoomVolume>();
            v.Configure(name, size, kind);
            return v;
        }

        /// <summary>Extrémités d'un couloir (faces des deux intersections), à plat, et ses sols.</summary>
        private static void CorridorEnds(int e, out Vector3 start, out Vector3 end, out float ya, out float yb, out Vector3 dir, out bool alongX)
        {
            Node a = Nodes[Edges[e].x], b = Nodes[Edges[e].y];
            Vector3 d = b.C - a.C;
            d.y = 0f;
            dir = d.normalized;
            alongX = Mathf.Abs(dir.x) > 0.5f;
            start = new Vector3(a.C.x, 0f, a.C.z) + dir * (alongX ? a.Half : a.HalfZ);
            end = new Vector3(b.C.x, 0f, b.C.z) - dir * (alongX ? b.Half : b.HalfZ);
            ya = a.C.y;
            yb = b.C.y;
        }

        private static string CorridorName(int e) => $"Couloir {Nodes[Edges[e].x].Name.Split(' ')[0]} – {Nodes[Edges[e].y].Name.Split(' ')[0]}";

        /// <summary>Point au sol du couloir à la fraction t (0 : début), à la hauteur du sol (pente comprise).</summary>
        private static Vector3 CorridorPoint(int e, float t, float side = 0f)
        {
            CorridorEnds(e, out Vector3 s, out Vector3 en, out float ya, out float yb, out Vector3 dir, out _);
            Vector3 lateral = new Vector3(-dir.z, 0f, dir.x);
            return Vector3.Lerp(s, en, t) + Vector3.up * Mathf.Lerp(ya, yb, t) + lateral * side;
        }

        /// <summary>Couloir de 14 × 19 m : sol (pente pleine entre deux niveaux), plafond, murs (percés pour les pièces), lumières.</summary>
        private static void Corridor(int e)
        {
            CorridorEnds(e, out Vector3 start, out Vector3 end, out float ya, out float yb, out Vector3 dir, out bool alongX);
            string name = CorridorName(e);
            float w = Width(e), h = L.corridorHeight, len = Vector3.Distance(start, end);
            Vector3 mid = (start + end) * 0.5f;
            Vector3 sizeAlong = alongX ? new Vector3(len, 1f, w) : new Vector3(w, 1f, len);
            if (Mathf.Approximately(ya, yb))
            {
                if (ya > 0.1f)
                    Piece($"{name} : sol (bloc de {ya:0} m)", mid + Vector3.up * (ya / 2f), alongX ? new Vector3(len, ya, w) : new Vector3(w, ya, len), _floor);
                else
                    Piece($"{name} : sol", mid + Vector3.down * 0.5f, sizeAlong, _floor);
                Piece($"{name} : plafond", mid + Vector3.up * (ya + h + 0.5f), sizeAlong + (alongX ? new Vector3(0f, 0f, 2f * T) : new Vector3(2f * T, 0f, 0f)),
                    _ceiling, walkable: false);
            }
            else
            {
                // Pente pleine (ni dessous, ni recoin) et plafond parallèle.
                bool up = yb > ya;
                Vector3 low = up ? start : end, rise = up ? dir : -dir;
                float dy = Mathf.Abs(yb - ya);
                float ang = Mathf.Atan2(dy, len) * Mathf.Rad2Deg;
                float yaw = Mathf.Atan2(rise.x, rise.z) * Mathf.Rad2Deg;
                // Deux voies (D-037) : la pente (côté gauche du couloir) et une voie au sol qui finit au pied d'un mur de la
                // hauteur du niveau haut ; les goules grimpent ce mur pour rejoindre directement la salle haute.
                Vector3 lateral = new Vector3(-dir.z, 0f, dir.x);
                float lane = L.cliffLaneWidth, slopeW = w - lane;
                Vector3 slopeOffset = lateral * (w / 2f - slopeW / 2f), laneOffset = -lateral * (w / 2f - lane / 2f);
                Ramp($"{name} : pente ({len:0} m, {ang:0.0}°)", low + slopeOffset + Vector3.up * Mathf.Min(ya, yb), dy, len, slopeW, yaw, _ramp);
                if (lane > 0.1f)                                            // niveau 1 : 0, pente sur toute la largeur (pas de vide à côté)
                    Piece($"{name} : voie au sol", mid + laneOffset + Vector3.down * 0.5f + Vector3.up * Mathf.Min(ya, yb),
                        alongX ? new Vector3(len, 1f, lane) : new Vector3(lane, 1f, len), _floor);
                // Mur au bout de la voie au sol (allège de la salle haute) : les goules le grimpent (liens automatiques, D-038).
                float l = Mathf.Sqrt(len * len + dy * dy);
                var ceil = Piece($"{name} : plafond", mid + Vector3.up * ((ya + yb) / 2f + h + 0.5f), new Vector3(w + 2f * T, 1f, l), _ceiling, walkable: false);
                ceil.transform.localRotation = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(-ang, 0f, 0f);
            }

            float yLow = Mathf.Min(ya, yb), yTop = Mathf.Max(ya, yb) + h;
            float from = alongX ? Mathf.Min(start.x, end.x) : Mathf.Min(start.z, end.z);
            float to = alongX ? Mathf.Max(start.x, end.x) : Mathf.Max(start.z, end.z);
            float sideCoord = alongX ? mid.z : mid.x;
            WallLine($"{name} : mur {(alongX ? "nord" : "est")}", alongX, sideCoord + w / 2f + T / 2f, from, to, yLow > 0.1f ? 0f : yLow, yTop,
                CorridorOpenings.TryGetValue((e, +1), out List<Opening> plus) ? plus : null);
            WallLine($"{name} : mur {(alongX ? "sud" : "ouest")}", alongX, sideCoord - w / 2f - T / 2f, from, to, yLow > 0.1f ? 0f : yLow, yTop,
                CorridorOpenings.TryGetValue((e, -1), out List<Opening> minus) ? minus : null);

            // Volumes des tronçons (D-047) : environ corridorSegmentLength chacun, du sol (pente comprise) au plafond.
            int segments = Mathf.Max(1, Mathf.CeilToInt(len / L.corridorSegmentLength - 0.05f));   // tronçons de 40 m au plus
            for (int i = 0; i < segments; i++)
            {
                Vector3 p0 = CorridorPoint(e, (float)i / segments), p1 = CorridorPoint(e, (i + 1f) / segments);
                float y0 = Mathf.Min(p0.y, p1.y), y1 = Mathf.Max(p0.y, p1.y) + h;
                Vector3 c = (p0 + p1) * 0.5f;
                float segLen = len / segments;
                RoomVolumeAt($"{name}, tronçon {i + 1}/{segments}", new Vector3(c.x, (y0 + y1) * 0.5f, c.z), Quaternion.identity,
                    alongX ? new Vector3(segLen, y1 - y0, w) : new Vector3(w, y1 - y0, segLen), RoomKind.Couloir);
            }

            int lamps = Mathf.Max(1, Mathf.RoundToInt(len / 25f));
            for (int i = 0; i < lamps; i++)
            {
                float t = (i + 0.5f) / lamps;
                Lamp($"{name} : lumière {i + 1}", CorridorPoint(e, t) + Vector3.up * (h - 2f), i % 2 == 0 ? new Color(1f, 0.75f, 0.5f) : new Color(0.8f, 0.85f, 1f), 5f, 40f);
            }

            // Goules du couloir, réparties le long de l'axe.
            for (int i = 0; i < L.ghoulsPerCorridor; i++)
            {
                float t = (i + 1f) / (L.ghoulsPerCorridor + 1f);
                float side = IsSlope(e) ? (Width(e) - L.cliffLaneWidth) / 2f : (i % 2 == 0 ? 1f : -1f) * 3f;   // pente : sur la voie en pente
                SpawnPointAt(CorridorPoint(e, t, side), Quaternion.Euler(0f, i * 90f, 0f), SpawnKinds.Goule, $"goule, {name} {i + 1}");
            }

            // Volants : en hauteur au milieu du couloir (D-041).
            for (int i = 0; i < L.flyersPerCorridor; i++)
            {
                float t = (i + 1f) / (L.flyersPerCorridor + 1f);
                SpawnPointAt(CorridorPoint(e, t) + Vector3.up * (h - 5f), Quaternion.identity, SpawnKinds.Volant, $"volant, {name} {i + 1}");
            }
        }

        private static void Ramp(string name, Vector3 bottom, float height, float length, float width, float yaw, Material mat = null)
        {
            float ang = Mathf.Atan2(height, length) * Mathf.Rad2Deg;
            float l = Mathf.Sqrt(height * height + length * length);
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            Vector3 dir = rot * Vector3.forward;
            // Rampe pleine : dalle épaisse dont la face du dessus est la pente ; le reste s'enfonce sous le sol.
            float thick = height + 1f;
            Quaternion r0 = rot * Quaternion.Euler(-ang, 0f, 0f);
            Vector3 top = bottom + dir * (length / 2f) + Vector3.up * (height / 2f);
            var r = Piece($"Rampe : {name}", top - (r0 * Vector3.up) * (thick / 2f), new Vector3(width, thick, l), mat != null ? mat : _ramp);
            r.transform.localRotation = r0;
        }

        // ---------- Pièces fermées (le traqueur passe le bras) ----------

        private struct RoomDef
        {
            public string Name;
            public int Node;        // hôte : une salle (côté Side) …
            public int Edge;        // … ou un couloir (côté +1 / −1)
            public int Side;
            public float Along;     // centre de la porte le long du mur
            public bool Locked;     // salle verrouillée (D-043) : porte payante, coffre gratuit dedans
        }

        private static void DefineRooms(out List<RoomDef> rooms)
        {
            float S = L.gridSpacing;
            rooms = new List<RoomDef>
            {
                new RoomDef { Name = "Pièce SO", Node = 0, Edge = -1, Side = SideW, Along = -S },
                new RoomDef { Name = "Pièce SE", Node = 2, Edge = -1, Side = SideS, Along = S },
                new RoomDef { Name = "Pièce NO", Node = 6, Edge = -1, Side = SideN, Along = -S },
                new RoomDef { Name = "Pièce NE", Node = 8, Edge = -1, Side = SideE, Along = S, Locked = true },
                new RoomDef { Name = "Pièce du couloir S – SE", Node = -1, Edge = 1, Side = -1, Along = S * 0.4f },
                new RoomDef { Name = "Pièce du couloir O – C", Node = -1, Edge = 2, Side = +1, Along = -S * 0.61f },
            };

            AddRoomOpenings(rooms);
        }

        /// <summary>Porte et fenêtre de chaque pièce dans le mur de son hôte (salle ou couloir).</summary>
        private static void AddRoomOpenings(List<RoomDef> rooms)
        {
            float dw = L.roomDoorWidth, dh = L.roomDoorHeight;
            foreach (RoomDef r in rooms)
            {
                RoomFrame(r, out _, out _, out _, out float floorY);
                var door = new Opening(r.Along - dw / 2f, r.Along + dw / 2f, floorY, floorY + dh);
                float wa = r.Along + L.roomSize * 0.3f;
                var window = new Opening(wa - 1.5f, wa + 1.5f, floorY + 1.8f, floorY + 3.3f);
                if (r.Node >= 0)
                {
                    AddOpening(r.Node, r.Side, door);
                    AddOpening(r.Node, r.Side, window);
                }
                else
                {
                    if (!CorridorOpenings.TryGetValue((r.Edge, r.Side), out List<Opening> list))
                        CorridorOpenings[(r.Edge, r.Side)] = list = new List<Opening>();
                    list.Add(door);
                    list.Add(window);
                }
            }
        }

        /// <summary>Repère de la pièce : point de la face extérieure du mur hôte au centre de la porte, direction vers la pièce, axe du mur.</summary>
        private static void RoomFrame(RoomDef r, out Vector3 face, out Vector3 into, out Vector3 along, out float floorY)
        {
            if (r.Node >= 0)
            {
                Node n = Nodes[r.Node];
                floorY = n.C.y;
                switch (r.Side)
                {
                    case SideN: into = Vector3.forward; face = new Vector3(r.Along, 0f, n.C.z + n.HalfZ + T); break;
                    case SideS: into = Vector3.back; face = new Vector3(r.Along, 0f, n.C.z - n.HalfZ - T); break;
                    case SideE: into = Vector3.right; face = new Vector3(n.C.x + n.Half + T, 0f, r.Along); break;
                    default: into = Vector3.left; face = new Vector3(n.C.x - n.Half - T, 0f, r.Along); break;
                }
            }
            else
            {
                CorridorEnds(r.Edge, out Vector3 s, out Vector3 en, out float ya, out _, out Vector3 dir, out bool alongX);
                floorY = ya;
                Vector3 mid = (s + en) * 0.5f;
                float off = L.corridorWidth / 2f + T;
                into = alongX ? Vector3.forward * r.Side : Vector3.right * r.Side;
                face = alongX ? new Vector3(r.Along, 0f, mid.z + off * r.Side) : new Vector3(mid.x + off * r.Side, 0f, r.Along);
            }

            along = Mathf.Abs(into.x) > 0.5f ? Vector3.forward : Vector3.right;
        }

        private static void BuildRoom(RoomDef r)
        {
            RoomFrame(r, out Vector3 face, out Vector3 into, out Vector3 along, out float y);
            float R = L.roomSize, H = L.roomHeight;
            Vector3 inner = face + into * (R / 2f);
            Vector3 foot = Abs(along) * (R + 2f * T) + Abs(into) * (R + T);
            Vector3 footCenter = face + into * ((R + T) / 2f);
            if (y > 0.1f)
                Piece($"{r.Name} : sol (bloc de {y:0} m)", footCenter + Vector3.up * (y / 2f), foot + Vector3.up * y, _room);
            else
                Piece($"{r.Name} : sol", footCenter + Vector3.down * 0.5f, foot + Vector3.up, _room);
            Piece($"{r.Name} : toit", footCenter + Vector3.up * (y + H + 0.5f), foot + Vector3.up, _ceiling, walkable: false);
            Piece($"{r.Name} : mur du fond", face + into * (R + T / 2f) + Vector3.up * (y + H / 2f), Abs(along) * (R + 2f * T) + Abs(into) * T + Vector3.up * H, _room, walkable: false);
            Piece($"{r.Name} : mur 1", inner + along * (R / 2f + T / 2f) + Vector3.up * (y + H / 2f), Abs(into) * R + Abs(along) * T + Vector3.up * H, _room, walkable: false);
            Piece($"{r.Name} : mur 2", inner - along * (R / 2f + T / 2f) + Vector3.up * (y + H / 2f), Abs(into) * R + Abs(along) * T + Vector3.up * H, _room, walkable: false);
            Lamp($"{r.Name} : lumière", inner + Vector3.up * (y + H - 1f), new Color(1f, 0.6f, 0.35f), 2.5f, 14f);

            var root = new GameObject($"{r.Name} (le traqueur passe le bras)");
            root.transform.SetParent(_level, false);
            root.transform.SetPositionAndRotation(inner + Vector3.up * (y + H / 2f), Quaternion.LookRotation(into, Vector3.up));
            var room = root.AddComponent<ReachRoom>();
            root.AddComponent<RoomVolume>().Configure(r.Name, new Vector3(R, H, R));
            Vector3 wallPlane = face - into * (T / 2f);
            var door = new GameObject("Ouverture : porte").transform;
            door.SetParent(root.transform, false);
            door.SetPositionAndRotation(wallPlane + Vector3.up * (y + L.roomDoorHeight / 2f), Quaternion.LookRotation(-into, Vector3.up));
            var window = new GameObject("Ouverture : fenêtre").transform;
            window.SetParent(root.transform, false);
            window.SetPositionAndRotation(wallPlane + along * (L.roomSize * 0.3f) + Vector3.up * (y + 2.55f), Quaternion.LookRotation(-into, Vector3.up));
            var so = new SerializedObject(room);
            so.FindProperty("size").vector3Value = new Vector3(R, H, R);
            SerializedProperty list = so.FindProperty("openings");
            list.arraySize = 2;
            list.GetArrayElementAtIndex(0).objectReferenceValue = door;
            list.GetArrayElementAtIndex(1).objectReferenceValue = window;
            so.ApplyModifiedPropertiesWithoutUndo();
            Crate(inner + Vector3.up * y + along * 3f, 15f, root.transform);

            // Salle verrouillée (D-043) : la pièce NE ; porte payante (E / Y), fenêtre à barreaux, coffre gratuit dedans.
            if (r.Locked)
            {
                Material doorMat = GrabbableMat("Economy_Locked_Door", new Color(0.35f, 0.22f, 0.12f));
                GameObject block = Box($"{r.Name} : porte verrouillée", _level, wallPlane + Vector3.up * (y + L.roomDoorHeight / 2f),
                    Abs(along) * (L.roomDoorWidth + 0.2f) + Abs(into) * (T + 0.2f) + Vector3.up * L.roomDoorHeight, doorMat);
                block.GetComponent<MeshRenderer>().sharedMaterial.SetColor("_EmissionColor", new Color(1.2f, 0.6f, 0.15f));
                GameObject bars = Box($"{r.Name} : barreaux de la fenêtre", _level, wallPlane + along * (L.roomSize * 0.3f) + Vector3.up * (y + 2.55f),
                    Abs(along) * 3.2f + Abs(into) * (T + 0.2f) + Vector3.up * 1.7f, _wall);
                var locked = block.AddComponent<LockedDoor>();
                SetRef(locked, "settings", _economy);
                var lso = new SerializedObject(locked);
                SerializedProperty also = lso.FindProperty("alsoBlocking");
                also.arraySize = 1;
                also.GetArrayElementAtIndex(0).objectReferenceValue = bars;
                lso.ApplyModifiedPropertiesWithoutUndo();
                BuildChest($"{r.Name} : coffre gratuit", inner + Vector3.up * y - along * 3f, Quaternion.LookRotation(-into, Vector3.up), 0);
            }
        }

        // ---------- Économie (étape 7, D-043) ----------

        private static UpgradePool UpgradePoolAsset()
        {
            if (!AssetDatabase.IsValidFolder(UpgradesDir))
                AssetDatabase.CreateFolder(SettingsDir, "Upgrades");
            var pool = LoadOrCreate<UpgradePool>(UpgradePoolPath);
            pool.upgrades.Clear();
            pool.upgrades.Add(Upgrade("Upgrade_Charge", "Charge en plus", "+1 charge de mouvement (dash, saut en l'air).", "+1", new Color(0.4f, 2.4f, 3f), UpgradeKind.ChargeDeMouvement, 1f, 70));
            pool.upgrades.Add(Upgrade("Upgrade_Traction", "Traction rapide", "Une charge de traction revient 1 s plus vite.", "-1s", new Color(0.3f, 0.9f, 3.2f), UpgradeKind.RechargeTraction, 1f, 60));
            pool.upgrades.Add(Upgrade("Upgrade_Collecte", "Grande orbe", "Rayon de collecte des âmes +50 %.", "R+", new Color(0.3f, 3f, 2.2f), UpgradeKind.RayonDeCollecte, 0.5f, 40));
            pool.upgrades.Add(Upgrade("Upgrade_Projectiles", "Projectiles lourds", "Objets lancés ou repoussés : +25 % de dégâts.", "x1.25", new Color(2f, 0.6f, 3f), UpgradeKind.DegatsObjetsLances, 0.25f, 70));
            pool.upgrades.Add(Upgrade("Upgrade_Repulsion", "Onde blessante", "La répulsion blesse les ennemis touchés (10 PV).", "10", new Color(3.2f, 1.3f, 0.3f), UpgradeKind.DegatsRepulsion, 10f, 60));
            pool.upgrades.Add(Upgrade("Upgrade_PV", "Cœur solide", "+25 PV max (et 25 PV tout de suite).", "PV+", new Color(3.2f, 0.4f, 0.4f), UpgradeKind.PvMax, 25f, 50));
            pool.upgrades.Add(Upgrade("Upgrade_Aspiration", "Aspiration", "Les âmes viennent à l'orbe 50 % plus vite.", ">>", new Color(1.2f, 2.6f, 3.2f), UpgradeKind.AspirationAmes, 0.5f, 40));
            EditorUtility.SetDirty(pool);
            return pool;
        }

        /// <summary>Améliorations du barillet (D-050, provisoires) : vendues par le marchand de la salle de départ.</summary>
        public static UpgradePool BarrelPoolAsset()
        {
            if (!AssetDatabase.IsValidFolder(UpgradesDir))
                AssetDatabase.CreateFolder(SettingsDir, "Upgrades");
            var pool = LoadOrCreate<UpgradePool>(BarrelPoolPath);
            pool.upgrades.Clear();
            pool.upgrades.Add(Upgrade("Upgrade_Barillet", "Barillet garni", "+2 balles au barillet.", "+2", new Color(3f, 2.5f, 0.4f), UpgradeKind.BallesBarillet, 2f, 50));
            pool.upgrades.Add(Upgrade("Upgrade_Barillet_Degats", "Balles lourdes", "Balles du barillet : +30 % de dégâts.", "x1.3", new Color(3.2f, 0.9f, 0.3f), UpgradeKind.DegatsBarillet, 0.3f, 60));
            pool.upgrades.Add(Upgrade("Upgrade_Barillet_Cadence", "Gâchette vive", "Tir du barillet 25 % plus rapide.", ">>>", new Color(3f, 1.8f, 0.3f), UpgradeKind.CadenceBarillet, 0.25f, 55));
            pool.upgrades.Add(Upgrade("Upgrade_Barillet_Rechargement", "Rechargement éclair", "Rechargement du barillet 35 % plus court.", "R", new Color(2.6f, 2.6f, 0.6f), UpgradeKind.RechargementBarillet, 0.35f, 45));
            pool.upgrades.Add(Upgrade("Upgrade_Barillet_Portee", "Long canon", "Portée du barillet +25 m.", "+25", new Color(2.2f, 2.8f, 0.8f), UpgradeKind.PorteeBarillet, 25f, 40));
            EditorUtility.SetDirty(pool);
            return pool;
        }

        /// <summary>Marchand de l'artefact dans la salle de départ (D-050) : silhouette violette, enseigne, offres par joueur.</summary>
        public static void BuildArtifactMerchant(Transform parent, Vector3 at, Quaternion rotation, EconomySettings economy, Material orb)
        {
            var root = new GameObject("Marchand de l'artefact (salle de départ)");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(at, rotation);
            Material robe = GrabbableMat("Economy_Merchant", new Color(0.2f, 0.12f, 0.25f));
            robe.SetColor("_EmissionColor", new Color(0.8f, 0.3f, 1.2f));
            GameObject figure = Box("Marchand (silhouette)", root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(0.9f, 2.2f, 0.9f), robe);
            GameObjectUtility.SetStaticEditorFlags(figure, 0);
            GameObject sign = Box("Enseigne", root.transform, ArtifactMerchant.SignOffset(3, true, 0.4f), new Vector3(1.6f, 0.4f, 0.1f),        // au-dessus des offres (replacée au lancement)
                UnlitMat("Economy_Merchant_Sign", "Universal Render Pipeline/Unlit", new Color(2.5f, 1f, 3f)), collider: false);
            GameObjectUtility.SetStaticEditorFlags(sign, 0);
            var m = root.AddComponent<ArtifactMerchant>();
            SetRef(m, "settings", economy);
            SetRef(m, "pool", BarrelPoolAsset());
            SetRef(m, "orbMaterial", orb);
            var l = new GameObject("Lumière du marchand").AddComponent<Light>();
            l.transform.SetParent(root.transform, false);
            l.transform.localPosition = new Vector3(0f, 4f, 1.5f);
            l.type = LightType.Point;
            l.color = new Color(0.8f, 0.4f, 1f);
            l.intensity = 3f;
            l.range = 12f;
        }

        /// <summary>Asset d'amélioration : créé avec ces valeurs s'il n'existe pas (sinon gardé tel quel, réglages compris).</summary>
        private static UpgradeDefinition Upgrade(string file, string name, string description, string icon, Color color, UpgradeKind kind,
            float amount, int price)
        {
            string path = $"{UpgradesDir}/{file}.asset";
            var u = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (u != null)
                return u;
            u = ScriptableObject.CreateInstance<UpgradeDefinition>();
            u.displayName = name;
            u.description = description;
            u.iconSymbol = icon;
            u.color = color;
            u.kind = kind;
            u.amount = amount;
            u.merchantPrice = price;
            AssetDatabase.CreateAsset(u, path);
            return u;
        }

        /// <summary>Coffre greybox : caisse sombre à liseré lumineux, couvercle (caché à l'ouverture) ; prix -1 : celui des réglages.</summary>
        private static void BuildChest(string name, Vector3 feet, Quaternion rotation, int price)
        {
            var root = new GameObject(name);
            root.transform.SetParent(_level, false);
            root.transform.SetPositionAndRotation(feet, rotation);
            Material wood = GrabbableMat("Economy_Chest", new Color(0.3f, 0.2f, 0.1f));
            wood.SetColor("_EmissionColor", new Color(0.9f, 0.6f, 0.15f));
            GameObject body = Box("Caisse du coffre", root.transform, new Vector3(0f, 0.4f, 0f), new Vector3(1.4f, 0.8f, 0.9f), wood, collider: false);
            GameObjectUtility.SetStaticEditorFlags(body, 0);
            var box = root.AddComponent<BoxCollider>();                 // sur la racine : le coffre reçoit les chocs des objets lancés
            box.center = new Vector3(0f, 0.45f, 0f);
            box.size = new Vector3(1.4f, 0.9f, 0.9f);
            Material glow = UnlitMat("Economy_Chest_Glow", "Universal Render Pipeline/Unlit", new Color(3f, 2f, 0.5f));
            GameObject lid = Box("Couvercle", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(1.5f, 0.2f, 1f), glow, collider: false);
            GameObjectUtility.SetStaticEditorFlags(lid, 0);
            var chest = root.AddComponent<Chest>();
            SetRef(chest, "settings", _economy);
            SetRef(chest, "lid", lid.transform);
            SetRef(chest, "orbMaterial", _offerOrb);
            var so = new SerializedObject(chest);
            so.FindProperty("price").intValue = price;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Âmes, coffres, marchand (5 emplacements possibles, tirés avec la seed de la run).</summary>
        private static void EconomyContent()
        {
            var souls = new GameObject("Âmes (éclats)").AddComponent<SoulSystem>();
            SetRef(souls, "settings", _economy);
            Material soulMat = UnlitMat("Economy_Soul", "Universal Render Pipeline/Unlit", _economy.soulColor);
            soulMat.enableInstancing = true;
            EditorUtility.SetDirty(soulMat);
            SetRef(souls, "soulMaterial", soulMat);

            BuildChest("Coffre : salle C", Nodes[4].C + new Vector3(10f, 0f, -12f), Quaternion.Euler(0f, 180f, 0f), -1);
            BuildChest("Coffre : salle SE", Nodes[2].C + new Vector3(-12f, 0f, 10f), Quaternion.Euler(0f, 90f, 0f), -1);

            // Marchand de l'artefact dans la salle de départ SO (D-050), face au départ des joueurs.
            Node sw0 = Nodes[0];
            Vector3 mAt = sw0.C + new Vector3(4f, 0f, 6f);
            NonMagneticPiece("mur d'essai (salle de départ)", _level, sw0.C + new Vector3(-20f, 6f, 14f), new Vector3(1f, 12f, 14f), walkable: false);
            BuildArtifactMerchant(_level, mAt, Quaternion.LookRotation(new Vector3(-12f, 0f, -10f) - new Vector3(4f, 0f, 6f), Vector3.up), _economy, _offerOrb);
        }

        private static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // ---------- Intérieurs des salles ----------

        /// <summary>Mur libre pour le wall run et le wall jump (« Mur de course »).</summary>
        private static void RunWall(string name, Vector3 center, float length, float height, bool alongZ) =>
            Piece($"Mur de course : {name}", center + Vector3.up * height / 2f, alongZ ? new Vector3(1f, height, length) : new Vector3(length, height, 1f), _wallRun, walkable: false);

        /// <summary>Surface magnétique en hauteur (« Point de traction »), bleue et lumineuse.</summary>
        private static void Magnet(string name, Vector3 center, Vector3 size) => Piece($"Point de traction : {name}", center, size, _magnetic, walkable: false);

        /// <summary>Corniche pleine jusqu'au sol (11 m), contre un mur ; les goules y grimpent (liens automatiques, D-038).</summary>
        private static void Mezzanine(string name, Vector3 floor, float x0, float x1, float z0, float z1, int frontSide)
        {
            float top = L.bridgeBottom + 1f;
            Piece($"Corniche {top:0} m : {name}", floor + new Vector3((x0 + x1) / 2f, top / 2f, (z0 + z1) / 2f), new Vector3(x1 - x0, top, z1 - z0), _platform);
        }

        /// <summary>Pont : tablier de 1 m dont le dessous est à bridgeBottom (le traqueur se baisse dessous).</summary>
        private static void Bridge(string name, Vector3 floor, Vector3 from, Vector3 to)
        {
            Vector3 d = to - from;
            bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
            float w = L.bridgeWidth, len = d.magnitude;
            Vector3 c = floor + (from + to) * 0.5f + Vector3.up * (L.bridgeBottom + 0.5f);
            Piece($"Pont (dessous à {L.bridgeBottom:0} m) : {name}", c, alongX ? new Vector3(len, 1f, w) : new Vector3(w, 1f, len), _bridge);
        }

        private static void Halls()
        {
            // SO (départ) : couloirs à l'est et au nord, pièce à l'ouest.
            {
                Node n = Nodes[0];
                Vector3 f = n.C;
                Mezzanine("SO sud", f, -25f, -4f, -25f, -19f, SideN);
                Mezzanine("SO nord", f, -25f, -10f, 19f, 25f, SideS);
                Bridge("SO nord-sud", f, new Vector3(-17f, 0f, -19f), new Vector3(-17f, 0f, 19f));
                Ramp("SO vers la corniche sud", f + new Vector3(18f, 0f, -23f), L.bridgeBottom + 1f, 22f, 4f, -90f);
                RunWall("SO nord-est", f + new Vector3(15f, 0f, 14f), 14f, 12f, false);
                RunWall("SO est", f + new Vector3(10f, 0f, -13f), 10f, 12f, true);
                Magnet("SO haut ouest", f + new Vector3(-24.4f, 22f, -12f), new Vector3(0.5f, 6f, 6f));
                Magnet("SO haut nord", f + new Vector3(-17f, 24f, 24.4f), new Vector3(6f, 6f, 0.5f));
                Magnet("SO haut est", f + new Vector3(24.4f, 22f, 15f), new Vector3(0.5f, 6f, 6f));
                Magnet("SO plafond", f + new Vector3(0f, n.Ceil - 0.3f, 0f), new Vector3(8f, 0.5f, 8f));
                HallContents(n, new[] { new Vector3(-20f, 0f, -16f), new Vector3(13f, 0f, 9f), new Vector3(-21f, 0f, 14f), new Vector3(13f, 0f, -20f), new Vector3(2f, 0f, 20f) },
                    new[] { new Vector2(15f, 5f), new Vector2(18f, -18f), new Vector2(5f, 18f), new Vector2(20f, 20f), new Vector2(-18f, 17f), new Vector2(12f, -5f), new Vector2(0f, 12f), new Vector2(22f, -5f), new Vector2(-5f, 5f), new Vector2(8f, -14f) },
                    new Vector3(-5f, 12f, 5f));
            }

            // SE : couloirs à l'ouest et au nord, pièce au sud.
            {
                Node n = Nodes[2];
                Vector3 f = n.C;
                Mezzanine("SE ouest", f, -30f, -24f, 10f, 30f, SideE);
                Mezzanine("SE est", f, 24f, 30f, 10f, 30f, SideW);
                Bridge("SE est-ouest", f, new Vector3(-24f, 0f, 19f), new Vector3(24f, 0f, 19f));
                Ramp("SE vers la corniche est", f + new Vector3(28f, 0f, -16f), L.bridgeBottom + 1f, 26f, 4f, 0f);
                RunWall("SE sud-ouest", f + new Vector3(-15f, 0f, -19f), 14f, 12f, true);
                RunWall("SE sud", f + new Vector3(13f, 0f, -15f), 14f, 12f, false);
                Magnet("SE haut sud", f + new Vector3(-15f, 22f, -29.4f), new Vector3(6f, 6f, 0.5f));
                Magnet("SE haut est", f + new Vector3(29.4f, 24f, -22f), new Vector3(0.5f, 6f, 6f));
                Magnet("SE haut nord", f + new Vector3(15f, 24f, 29.4f), new Vector3(6f, 6f, 0.5f));
                Magnet("SE plafond", f + new Vector3(0f, n.Ceil - 0.3f, -10f), new Vector3(8f, 0.5f, 8f));
                HallContents(n, new[] { new Vector3(-21f, 0f, 13f), new Vector3(19f, 0f, 13f), new Vector3(-11f, 0f, -24f), new Vector3(10f, 0f, -11f), new Vector3(-20f, 0f, -8f) },
                    new[] { new Vector2(0f, -20f), new Vector2(20f, -20f), new Vector2(-20f, -25f), new Vector2(10f, 0f), new Vector2(-10f, 15f), new Vector2(20f, 0f), new Vector2(-20f, 0f), new Vector2(5f, 24f), new Vector2(-5f, -8f), new Vector2(15f, 8f) },
                    new Vector3(5f, 12f, -5f));
            }

            // C (centrale) : couloirs des quatre côtés ; deux ponts (est-ouest et nord-sud).
            {
                Node n = Nodes[4];
                Vector3 f = n.C;
                float h = n.Half;
                Mezzanine("C ouest", f, -h, -h + 6f, 12f, h, SideE);
                Mezzanine("C est", f, h - 6f, h, 12f, h, SideW);
                Mezzanine("C sud", f, 12f, h, -h, -h + 6f, SideN);
                Bridge("C est-ouest", f, new Vector3(-h + 6f, 0f, 21f), new Vector3(h - 6f, 0f, 21f));
                Bridge("C nord-sud", f, new Vector3(21f, 0f, -h + 6f), new Vector3(21f, 0f, 21f));
                Ramp("C vers la corniche est", f + new Vector3(8f, 0f, h - 4f), L.bridgeBottom + 1f, h - 6f - 8f, 4f, 90f);
                RunWall("C sud-ouest", f + new Vector3(-18f, 0f, -16f), 16f, 12f, false);
                RunWall("C nord-ouest (sous le pont)", f + new Vector3(-14f, 0f, 19f), 12f, 9f, true);
                Magnet("C plafond", f + new Vector3(0f, n.Ceil - 0.3f, 0f), new Vector3(8f, 0.5f, 8f));
                Magnet("C haut ouest", f + new Vector3(-h + 0.6f, 24f, -18f), new Vector3(0.5f, 6f, 6f));
                Magnet("C haut sud", f + new Vector3(-18f, 22f, -h + 0.6f), new Vector3(6f, 6f, 0.5f));
                Magnet("C haut est", f + new Vector3(h - 0.6f, 22f, -18f), new Vector3(0.5f, 6f, 6f));
                HallContents(n, new[] { new Vector3(-23f, 0f, -24f), new Vector3(-23f, 0f, 14f), new Vector3(16f, 0f, -23f), new Vector3(22f, 0f, 16f), new Vector3(-12f, 0f, -12f) },
                    new[] { new Vector2(-20f, -20f), new Vector2(20f, -12f), new Vector2(-12f, 22f), new Vector2(15f, 22f), new Vector2(-25f, 0f), new Vector2(0f, -20f), new Vector2(25f, 0f), new Vector2(0f, 15f), new Vector2(-10f, 8f), new Vector2(10f, -8f) },
                    new Vector3(-10f, 12f, -10f));
            }

            // NO (haute, +16 m) : couloirs à l'est et au sud, pièce au nord.
            {
                Node n = Nodes[6];
                Vector3 f = n.C;
                Mezzanine("NO est", f, 24f, 30f, -30f, -10f, SideW);
                Mezzanine("NO ouest", f, -30f, -24f, -30f, -10f, SideE);
                Bridge("NO est-ouest", f, new Vector3(-24f, 0f, -19f), new Vector3(24f, 0f, -19f));
                Ramp("NO vers la corniche ouest", f + new Vector3(-28f, 0f, 16f), L.bridgeBottom + 1f, 26f, 4f, 180f);
                RunWall("NO nord-est", f + new Vector3(15f, 0f, 19f), 14f, 12f, true);
                RunWall("NO nord", f + new Vector3(-13f, 0f, 15f), 14f, 12f, false);
                Magnet("NO haut nord", f + new Vector3(15f, 22f, 29.4f), new Vector3(6f, 6f, 0.5f));
                Magnet("NO haut ouest", f + new Vector3(-29.4f, 24f, 22f), new Vector3(0.5f, 6f, 6f));
                Magnet("NO haut sud", f + new Vector3(-15f, 24f, -29.4f), new Vector3(6f, 6f, 0.5f));
                Magnet("NO plafond", f + new Vector3(0f, n.Ceil - 0.3f, 10f), new Vector3(8f, 0.5f, 8f));
                HallContents(n, new[] { new Vector3(19f, 0f, -15f), new Vector3(-21f, 0f, -15f), new Vector3(9f, 0f, 22f), new Vector3(-12f, 0f, 9f), new Vector3(18f, 0f, 6f) },
                    new[] { new Vector2(0f, 20f), new Vector2(-20f, 20f), new Vector2(20f, 25f), new Vector2(-10f, 0f), new Vector2(10f, -15f), new Vector2(-20f, 0f), new Vector2(20f, 0f), new Vector2(-5f, -24f), new Vector2(5f, 8f), new Vector2(-15f, -8f) },
                    new Vector3(-5f, 12f, 5f));
            }

            // NE (haute, sortie) : couloirs à l'ouest et au sud, pièce à l'est.
            {
                Node n = Nodes[8];
                Vector3 f = n.C;
                Mezzanine("NE nord", f, 4f, 25f, 19f, 25f, SideS);
                Mezzanine("NE sud", f, 10f, 25f, -25f, -19f, SideN);
                Bridge("NE nord-sud", f, new Vector3(17f, 0f, -19f), new Vector3(17f, 0f, 19f));
                Ramp("NE vers la corniche nord", f + new Vector3(-18f, 0f, 23f), L.bridgeBottom + 1f, 22f, 4f, 90f);
                RunWall("NE sud-ouest", f + new Vector3(-10f, 0f, -14f), 14f, 12f, false);
                RunWall("NE ouest", f + new Vector3(-12f, 0f, 9f), 10f, 12f, true);
                Magnet("NE haut est", f + new Vector3(24.4f, 22f, 12f), new Vector3(0.5f, 6f, 6f));
                Magnet("NE haut sud", f + new Vector3(0f, 24f, -24.4f), new Vector3(6f, 6f, 0.5f));
                Magnet("NE haut ouest", f + new Vector3(-24.4f, 22f, -15f), new Vector3(0.5f, 6f, 6f));
                Magnet("NE plafond", f + new Vector3(0f, n.Ceil - 0.3f, 0f), new Vector3(8f, 0.5f, 8f));
                HallContents(n, new[] { new Vector3(10f, 0f, 15f), new Vector3(14f, 0f, -16f), new Vector3(-20f, 0f, -20f), new Vector3(-20f, 0f, 14f), new Vector3(-6f, 0f, -6f) },
                    new[] { new Vector2(-15f, -15f), new Vector2(0f, -18f), new Vector2(-18f, 0f), new Vector2(0f, 10f), new Vector2(-10f, 20f), new Vector2(10f, -5f), new Vector2(20f, 5f), new Vector2(-20f, -8f), new Vector2(5f, 18f), new Vector2(-5f, -12f) },
                    new Vector3(5f, 12f, -5f));
            }
        }

        /// <summary>Objets à lancer (grappes de 4, près des murs et des corniches), un lustre et les goules d'une salle.</summary>
        private static void HallContents(Node n, Vector3[] clusters, Vector2[] ghoulSpots, Vector3 lustre)
        {
            var parent = new GameObject($"Objets {n.Name}").transform;
            parent.SetParent(_level, false);
            float[] masses = { 5f, 15f, 25f, 5f, 40f };
            int k = 0;
            for (int c = 0; c < clusters.Length && k < L.propsPerHall; c++)
                for (int i = 0; i < 4 && k < L.propsPerHall; i++)
                {
                    Vector3 off = new Vector3((i % 2) * 1.6f, 0f, (i / 2) * 1.6f);
                    Crate(n.C + clusters[c] + off, masses[k++ % masses.Length], parent);
                }

            Lustre(n.Name, n.C + lustre, n.C.y + n.Ceil, parent);
            // Volants : dans les grandes salles seulement, à 12 m du sol, répartis (D-040).
            for (int i = 0; i < L.flyersPerHall; i++)
            {
                float a = (i + 0.5f) / Mathf.Max(1, L.flyersPerHall) * Mathf.PI * 2f;
                SpawnPointAt(n.C + new Vector3(Mathf.Cos(a) * n.Half * 0.45f, 12f, Mathf.Sin(a) * n.Half * 0.45f), Quaternion.identity, SpawnKinds.Volant, $"volant, {n.Name} {i + 1}");
            }
            for (int i = 0; i < Mathf.Min(L.ghoulsPerHall, ghoulSpots.Length); i++)
                SpawnPointAt(n.C + new Vector3(ghoulSpots[i].x, 0f, ghoulSpots[i].y), Quaternion.Euler(0f, i * 47f, 0f), SpawnKinds.Goule, $"goule, {n.Name} {i + 1}");
        }

        /// <summary>Poutres (dessous à 10 m) et ponts au-dessus des couloirs (les goules y montent par les murs).</summary>
        private static void CorridorFeatures()
        {
            int[] beams = { 0, 5, 10 };                 // SO–S, N–NE, SE–E
            foreach (int e in beams)
            {
                Vector3 p = CorridorPoint(e, 0.5f);
                CorridorEnds(e, out _, out _, out _, out _, out _, out bool alongX);
                Piece($"Poutre (dessous à {L.bridgeBottom:0} m) : {CorridorName(e)}", p + Vector3.up * (L.bridgeBottom + 0.5f),
                    alongX ? new Vector3(1f, 1f, L.corridorWidth) : new Vector3(L.corridorWidth, 1f, 1f), _bridge);
            }

            int[] bridges = { 8, 3 };                   // S–C, C–E
            foreach (int e in bridges)
            {
                Vector3 p = CorridorPoint(e, 0.45f);
                CorridorEnds(e, out _, out _, out _, out _, out Vector3 dir, out bool alongX);
                float w = L.corridorWidth, bw = L.bridgeWidth;
                Piece($"Pont (dessous à {L.bridgeBottom:0} m) : {CorridorName(e)}", p + Vector3.up * (L.bridgeBottom + 0.5f),
                    alongX ? new Vector3(bw, 1f, w) : new Vector3(w, 1f, bw), _bridge);
                Magnet($"{CorridorName(e)} au-dessus du pont", p + Vector3.up * (L.corridorHeight - 0.3f) + dir * 8f, new Vector3(5f, 0.5f, 5f));
            }
        }

        private static void Crate(Vector3 at, float mass, Transform parent)
        {
            float size = mass <= 5f ? 0.5f : mass <= 15f ? 0.7f : mass <= 25f ? 0.9f : 1.1f;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"{(mass >= 40f ? "Bloc lourd" : "Caisse")} {mass:0} kg ({++_crateCount})";
            go.transform.SetParent(parent, false);
            go.transform.position = at + Vector3.up * size / 2f;
            go.transform.localScale = Vector3.one * size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mass >= 40f ? _heavy : _crate;
            MakeGrabbable(go, mass, false);
        }

        private static void Lustre(string name, Vector3 at, float ceiling, Transform parent)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Lustre (accroché) : {name}";
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.localScale = new Vector3(2.4f, 0.4f, 2.4f);
            go.GetComponent<MeshRenderer>().sharedMaterial = _crate;
            MakeGrabbable(go, 25f, true);
            Piece($"Chaîne du lustre : {name}", new Vector3(at.x, (at.y + ceiling) / 2f, at.z), new Vector3(0.15f, ceiling - at.y, 0.15f), _wall, walkable: false, collider: false);
        }

        /// <summary>Point d'apparition des vagues (D-041) : objet vide avec SpawnPoint (on peut en poser à la main de la même façon).</summary>
        private static void SpawnPointAt(Vector3 at, Quaternion rotation, SpawnKinds kinds, string label)
        {
            _spawnCount++;
            var go = new GameObject($"Apparition : {label}");
            go.transform.SetParent(_spawns, false);
            go.transform.SetPositionAndRotation(at, rotation);
            var sp = go.AddComponent<SpawnPoint>();
            var so = new SerializedObject(sp);
            so.FindProperty("kinds").intValue = (int)kinds;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Goule greybox : capsule basse de 1.1 m (rayon 0.45), corps sombre, yeux qui brillent, repère rouge d'annonce du
        /// bond au sol. Attrapable étourdie (Grabbable ennemi), blessée par les objets (ImpactDamager), repoussée (EnemyBody).
        /// </summary>
        private static void BuildGhoul(Vector3 feet, Quaternion rotation, string label)
        {
            _ghoulCount++;
            var root = new GameObject($"Goule {label}");
            root.transform.SetParent(_ghouls, false);
            root.transform.SetPositionAndRotation(feet, rotation);
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = _ghoul.mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = 0.45f;
            capsule.height = 1.1f;
            capsule.center = new Vector3(0f, 0.55f, 0f);
            var visual = new GameObject("Corps (visuel)").transform;
            visual.SetParent(root.transform, false);
            Part(PrimitiveType.Capsule, "Corps", visual, new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 0.55f, 0.9f), Vector3.zero, _ghoulSkin);
            Part(PrimitiveType.Sphere, "Tête", visual, new Vector3(0f, 0.8f, 0.38f), new Vector3(0.45f, 0.38f, 0.5f), Vector3.zero, _ghoulSkin);
            Material eyes = UnlitMat("Ghoul_Eyes", "Universal Render Pipeline/Unlit", _ghoul.eyeColor);
            Part(PrimitiveType.Sphere, "Oeil gauche", visual, new Vector3(-0.1f, 0.86f, 0.6f), Vector3.one * 0.07f, Vector3.zero, eyes);
            Part(PrimitiveType.Sphere, "Oeil droit", visual, new Vector3(0.1f, 0.86f, 0.6f), Vector3.one * 0.07f, Vector3.zero, eyes);
            Material warn = UnlitMat("Ghoul_Telegraph", "Universal Render Pipeline/Unlit", _ghoul.telegraphColor);
            Part(PrimitiveType.Cylinder, "Annonce du bond", root.transform, new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.005f, 1.6f), Vector3.zero, warn);
            Renderer telegraph = root.transform.Find("Annonce du bond").GetComponent<Renderer>();
            var grab = root.AddComponent<Grabbable>();
            var gso = new SerializedObject(grab);
            gso.FindProperty("isEnemy").boolValue = true;
            gso.ApplyModifiedPropertiesWithoutUndo();
            SetRef(root.AddComponent<ImpactDamager>(), "settings", LoadOrCreate<DamageSettings>(DamagePath));
            var body = root.AddComponent<EnemyBody>();
            SetRef(body, "settings", _ghoul);
            var bso = new SerializedObject(body);
            bso.FindProperty("returnHome").boolValue = false;
            bso.ApplyModifiedPropertiesWithoutUndo();
            var dmg = root.AddComponent<Damageable>();
            SetRef(dmg, "settings", _ghoul);
            var dso = new SerializedObject(dmg);
            dso.FindProperty("respawns").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();
            var ghoul = root.AddComponent<Ghoul>();
            SetRef(ghoul, "settings", _ghoul);
            SetRef(ghoul, "visual", visual);
            SetRef(ghoul, "telegraph", telegraph);
        }

        /// <summary>
        /// Volant greybox (D-040) : corps de 0.6 m, deux ailes de 0.9 m qui battent, yeux jaunes, lueur rouge d'annonce du
        /// piqué ; sphère de collision de 0.4 m ; attrapable à tout moment (Grabbable ennemi, libéré au bout de 2 s), blessé par
        /// les impacts (ImpactDamager), repoussé et étourdi (EnemyBody, dégâts contre un mur même étourdi).
        /// </summary>
        private static void BuildFlyer(Vector3 at, string label)
        {
            _flyerCount++;
            var root = new GameObject($"Volant {label}");
            root.transform.SetParent(_ghouls, false);
            root.transform.position = at;
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = _flyer.mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            var sphere = root.AddComponent<SphereCollider>();
            sphere.radius = _flyer.radius;
            Material skin = GrabbableMat("Greybox_Flyer", new Color(0.22f, 0.16f, 0.2f));
            var visual = new GameObject("Corps (visuel)").transform;
            visual.SetParent(root.transform, false);
            Part(PrimitiveType.Sphere, "Corps", visual, Vector3.zero, new Vector3(0.6f, 0.45f, 0.8f), Vector3.zero, skin);
            Material eyes = UnlitMat("Flyer_Eyes", "Universal Render Pipeline/Unlit", _flyer.eyeColor);
            Part(PrimitiveType.Sphere, "Oeil gauche", visual, new Vector3(-0.12f, 0.08f, 0.36f), Vector3.one * 0.08f, Vector3.zero, eyes);
            Part(PrimitiveType.Sphere, "Oeil droit", visual, new Vector3(0.12f, 0.08f, 0.36f), Vector3.one * 0.08f, Vector3.zero, eyes);
            Transform Wing(string name, float side)
            {
                var pivot = new GameObject(name).transform;
                pivot.SetParent(visual, false);
                pivot.localPosition = new Vector3(side * 0.25f, 0.05f, 0f);
                Part(PrimitiveType.Cube, $"{name} (membrane)", pivot, new Vector3(side * 0.45f, 0f, 0f), new Vector3(0.9f, 0.04f, 0.5f), Vector3.zero, skin);
                // Zone de touche de l'aile (déclencheur, suit le battement) : les balles et les projectiles la touchent (D-040).
                var membrane = pivot.Find($"{name} (membrane)");
                var box = membrane.gameObject.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.size = new Vector3(1f, 2.5f, 1.1f);                 // 0.9 × 0.1 × 0.55 m
                membrane.gameObject.AddComponent<ItHunts.Combat.Hitbox>();
                return pivot;
            }

            Transform left = Wing("Aile gauche", -1f), right = Wing("Aile droite", 1f);
            Material warn = UnlitMat("Flyer_Telegraph", "Universal Render Pipeline/Unlit", _flyer.telegraphColor);
            Part(PrimitiveType.Sphere, "Annonce du piqué", root.transform, Vector3.zero, Vector3.one * 1.1f, Vector3.zero, warn);
            Renderer telegraph = root.transform.Find("Annonce du piqué").GetComponent<Renderer>();
            var grab = root.AddComponent<Grabbable>();
            var gso = new SerializedObject(grab);
            gso.FindProperty("isEnemy").boolValue = true;
            gso.FindProperty("grabbableAnytime").boolValue = true;
            gso.FindProperty("holdDurationOverride").floatValue = 2f;
            gso.ApplyModifiedPropertiesWithoutUndo();
            SetRef(root.AddComponent<ImpactDamager>(), "settings", LoadOrCreate<DamageSettings>(DamagePath));
            var body = root.AddComponent<EnemyBody>();
            SetRef(body, "settings", _flyer);
            var bso = new SerializedObject(body);
            bso.FindProperty("returnHome").boolValue = false;
            bso.FindProperty("wallDamageWhileStunned").boolValue = true;
            bso.ApplyModifiedPropertiesWithoutUndo();
            var dmg = root.AddComponent<Damageable>();
            SetRef(dmg, "settings", _flyer);
            var dso = new SerializedObject(dmg);
            dso.FindProperty("respawns").boolValue = false;
            dso.ApplyModifiedPropertiesWithoutUndo();
            var flyer = root.AddComponent<Flyer>();
            SetRef(flyer, "settings", _flyer);
            SetRef(flyer, "visual", visual);
            SetRef(flyer, "wingLeft", left);
            SetRef(flyer, "wingRight", right);
            SetRef(flyer, "telegraph", telegraph);
        }

        /// <summary>
        /// Traqueur géant (D-034) : 7 m de large, 14 m de haut ; corps (jambes, torse, tête, yeux rouges) sous un objet qui
        /// se baisse sous les ponts (D-035) ; bras droit (boîte étirée de l'épaule à la main) et main de 4 × 2.2 × 4.5 m dont
        /// la BoxCollider (déclencheur) est la zone de prise ; repère rouge (lueur) et ombre de la zone visée. Corps solide
        /// (capsule), intuable (Damageable insensible).
        /// </summary>
        private static void BuildStalker(StalkerSettings s, Vector3 feet)
        {
            var root = new GameObject("Traqueur");
            root.transform.SetParent(_level, false);
            root.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, -90f, 0f));
            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = s.mass;
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.radius = s.width * 0.5f;
            capsule.height = s.height;
            capsule.center = new Vector3(0f, s.height * 0.5f, 0f);
            Material skin = Mat("Greybox_Stalker", new Color(0.07f, 0.06f, 0.07f));
            var body = new GameObject("Corps (se baisse)").transform;
            body.SetParent(root.transform, false);
            float W = s.width, Hh = s.height;
            Part(PrimitiveType.Cube, "Jambe gauche", body, new Vector3(-W * 0.22f, Hh * 0.2f, 0f), new Vector3(W * 0.28f, Hh * 0.4f, W * 0.3f), Vector3.zero, skin);
            Part(PrimitiveType.Cube, "Jambe droite", body, new Vector3(W * 0.22f, Hh * 0.2f, 0f), new Vector3(W * 0.28f, Hh * 0.4f, W * 0.3f), Vector3.zero, skin);
            Part(PrimitiveType.Capsule, "Torse", body, new Vector3(0f, Hh * 0.62f, 0f), new Vector3(W * 0.85f, Hh * 0.25f, W * 0.55f), Vector3.zero, skin);
            Part(PrimitiveType.Sphere, "Tête", body, new Vector3(0f, Hh * 0.92f, W * 0.08f), new Vector3(W * 0.35f, Hh * 0.13f, W * 0.35f), Vector3.zero, skin);
            Material eyes = UnlitMat("Stalker_Eyes", "Universal Render Pipeline/Unlit", new Color(5f, 0.3f, 0.2f));
            Part(PrimitiveType.Sphere, "Oeil gauche", body, new Vector3(-0.5f, Hh * 0.93f, W * 0.22f), Vector3.one * 0.45f, Vector3.zero, eyes);
            Part(PrimitiveType.Sphere, "Oeil droit", body, new Vector3(0.5f, Hh * 0.93f, W * 0.22f), Vector3.one * 0.45f, Vector3.zero, eyes);
            // Bras et main gauches : posés par le code (repos, mains levées au-dessus de la tête, D-037).
            var leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftArm.name = "Bras gauche";
            Object.DestroyImmediate(leftArm.GetComponent<Collider>());
            leftArm.transform.SetParent(root.transform, false);
            leftArm.transform.localScale = new Vector3(s.armThickness, s.armThickness, 6f);
            leftArm.GetComponent<MeshRenderer>().sharedMaterial = skin;
            var leftHand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftHand.name = "Main gauche";
            Object.DestroyImmediate(leftHand.GetComponent<Collider>());
            leftHand.transform.SetParent(root.transform, false);
            leftHand.transform.localScale = s.handSize;
            leftHand.GetComponent<MeshRenderer>().sharedMaterial = skin;

            // Bras droit et main qui attrape (posés par le code).
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Bras qui attrape";
            Object.DestroyImmediate(arm.GetComponent<Collider>());
            arm.transform.SetParent(root.transform, false);
            arm.transform.localScale = new Vector3(s.armThickness, s.armThickness, 6f);
            arm.GetComponent<MeshRenderer>().sharedMaterial = skin;
            var hand = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hand.name = "Main (zone de prise)";
            hand.transform.SetParent(root.transform, false);
            hand.transform.localScale = s.handSize;
            hand.GetComponent<MeshRenderer>().sharedMaterial = skin;
            var zone = hand.GetComponent<BoxCollider>();
            zone.isTrigger = true;

            // Repère de la zone visée : ombre et lueur rouge à la taille de la main, au sol.
            var marker = new GameObject("Zone visée par la main").transform;
            marker.SetParent(_level, false);
            Material red = UnlitMat("Stalker_Grab_Warning", "Universal Render Pipeline/Unlit", new Color(4f, 0.25f, 0.15f));
            Material shadow = UnlitMat("Stalker_Grab_Shadow", "Universal Render Pipeline/Unlit", new Color(0.02f, 0f, 0f));
            Part(PrimitiveType.Cube, "Ombre", marker, new Vector3(0f, 0.02f, 0f), new Vector3(s.handSize.x * 1.15f, 0.02f, s.handSize.z * 1.15f), Vector3.zero, shadow);
            Part(PrimitiveType.Cube, "Lueur", marker, new Vector3(0f, 0.04f, 0f), new Vector3(s.handSize.x, 0.02f, s.handSize.z), Vector3.zero, red);
            // Main plantée après une prise ratée (D-039) : nuage de poussière au sol, caché sinon.
            Material dustMat = Mat("Stalker_Dust", new Color(0.45f, 0.4f, 0.36f));
            Part(PrimitiveType.Cylinder, "Poussière", marker, new Vector3(0f, 0.3f, 0f), new Vector3(s.handSize.x * 1.3f, 0.3f, s.handSize.z * 1.3f), Vector3.zero, dustMat);
            marker.Find("Poussière").gameObject.SetActive(false);
            marker.gameObject.SetActive(false);

            var dmg = root.AddComponent<Damageable>();
            SetRef(dmg, "settings", s);
            var dso = new SerializedObject(dmg);
            dso.FindProperty("respawns").boolValue = false;
            dso.FindProperty("immuneToOrbs").boolValue = true;
            dso.ApplyModifiedPropertiesWithoutUndo();
            var stalker = root.AddComponent<Stalker>();
            SetRef(stalker, "settings", s);
            SetRef(stalker, "body", body);
            SetRef(stalker, "arm", arm.transform);
            SetRef(stalker, "hand", hand.transform);
            SetRef(stalker, "handZone", zone);
            SetRef(stalker, "grabMarker", marker);
            SetRef(stalker, "leftArm", leftArm.transform);
            SetRef(stalker, "leftHand", leftHand.transform);
            // Revers de la main gauche (D-039, piste a) : zone rouge au sol, posée et agrandie par le code, cachée sinon.
            float backD = 2f * (s.width * 0.5f + s.backhandReach);
            Part(PrimitiveType.Cylinder, "Zone du revers (traqueur)", _level, feet + Vector3.up * 0.05f, new Vector3(backD, 0.01f, backD), Vector3.zero, red);
            var backZone = _level.Find("Zone du revers (traqueur)").GetComponent<MeshRenderer>();
            backZone.enabled = false;
            SetRef(stalker, "backhandZone", backZone);
            GameObject predicted = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            predicted.name = "Point prédit du traqueur (debug)";
            Object.DestroyImmediate(predicted.GetComponent<Collider>());
            predicted.transform.SetParent(_level, false);
            predicted.transform.localScale = Vector3.one * 0.8f;
            predicted.GetComponent<MeshRenderer>().sharedMaterial = UnlitMat("Stalker_Marker", "Universal Render Pipeline/Unlit", new Color(3f, 0.2f, 0.2f));
            predicted.SetActive(false);
            SetRef(stalker, "predictedMarker", predicted.transform);
            root.AddComponent<StalkerAudio>();
        }
    }
}
