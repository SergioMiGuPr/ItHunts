using System.Collections.Generic;
using System.Linq;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Carte aléatoire (étape 8), côté éditeur : le kit de construction (prefabs Kit_*, avec les modèles livrés par la DA quand
    /// ils existent), les prefabs pré-câblés que le générateur pose à l'exécution (sortie et arène du boss, marchand, coffre,
    /// goule, volant, pièce annexe, salle verrouillée) et la scène Level_01_Aleatoire (tout sauf la carte : gestion de la zone,
    /// vagues, traqueur, joueur, générateur). La carte elle-même est tirée au chargement (RandomLevel).
    /// </summary>
    public static partial class TestArenaSceneBuilder
    {
        public const string KitDir = "Assets/_Project/Prefabs/Kit";
        public const string LevelPrefabDir = "Assets/_Project/Prefabs/Niveau";
        public const string KitPath = SettingsDir + "/Kit_Niveau1.asset";
        public const string MapLayoutPath = SettingsDir + "/MapLayout_Niveau1.asset";
        public const string RandomScenePath = "Assets/Scenes/Level_01_Aleatoire.unity";
        private const string ModelDir = "Assets/Art/Models/Niveau1";

        [MenuItem("ItHunts/Carte aléatoire : reconstruire le kit, les prefabs et la scène Level_01_Aleatoire")]
        private static void BuildRandomFromMenu() => Debug.Log(BuildRandomLevelScene());

        // ---------- Kit ----------

        /// <summary>Prefabs du kit (Kit_*) et l'asset KitSet. Greybox : cubes aux cotes ; modèles livrés : instance du FBX.</summary>
        public static KitSet BuildKit()
        {
            if (!AssetDatabase.IsValidFolder(KitDir))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Kit");
            Material stone = Mat("Module_Pierre", new Color(0.27f, 0.26f, 0.29f));
            Material slab = Mat("Module_Dalles", new Color(0.21f, 0.2f, 0.22f));
            Material wood = Mat("Prop_BoisSombre", new Color(0.22f, 0.15f, 0.1f));
            Material step = Mat("Kit_Marche", new Color(0.3f, 0.28f, 0.3f));
            var kit = LoadOrCreate<KitSet>(KitPath);
            // Pièces de la DA (lot du 2026-11-16, UV en coordonnées monde, mêmes cotes et pivots que les greybox) ; greybox sinon.
            kit.mur4x4 = ModelPiece("Kit_Mur4x4", "SM_Kit_Mur4x4") ?? CubePiece("Kit_Mur4x4", new Vector3(0f, 2f, 0f), new Vector3(4f, 4f, 1f), stone);
            kit.mur2x4 = ModelPiece("Kit_Mur2x4", "SM_Kit_Mur2x4");
            kit.mur4x3 = ModelPiece("Kit_Mur4x3", "SM_Kit_Mur4x3");
            kit.sol4 = ModelPiece("Kit_Sol4", "SM_Kit_Sol4") ?? CubePiece("Kit_Sol4", new Vector3(0f, -0.5f, 0f), new Vector3(4f, 1f, 4f), slab);
            kit.escalier = ModelPiece("Kit_Escalier", "SM_Kit_Escalier") ?? CubePiece("Kit_Escalier", new Vector3(0f, 0.125f, 0.5f), new Vector3(4f, 0.25f, 1f), step);
            kit.pont = ModelPiece("Kit_Pont", "SM_Kit_Pont") ?? CubePiece("Kit_Pont", new Vector3(0f, -0.5f, 0f), new Vector3(3f, 1f, 4f), wood);
            kit.pilier = ModelPiece("Kit_Pilier", "SM_Colonne");
            // Arc à écoinçons pleins de la DA (PR #6, même pivot, 14 × 11 × 1.6 m), sinon l'anneau seul.
            kit.arc = ModelPiece("Kit_Arc", "SM_Arc_Couloir_Ecoincons") ?? ModelPiece("Kit_Arc", "SM_Arc_Couloir");
            kit.portePiece = ModelPiece("Kit_PortePiece", "SM_Porte_Piece");
            kit.grandePorte = ModelPiece("Kit_GrandePorte", "SM_GrandePorte");
            kit.poutre = ModelPiece("Kit_Poutre", "SM_Poutre");
            kit.pontEgout4 = ModelPiece("Kit_PontEgout4", "SM_Pont_Egout_4m");
            kit.pontEgout14 = ModelPiece("Kit_PontEgout14", "SM_Pont_Egout_14m");
            // Kit gothique des couloirs (PR #8) : voûte en lancette, arcs-doubleaux, colonnes engagées, murs à arcade et à niche.
            kit.voute8 = ModelPiece("Kit_Voute_Ogive_8m", "SM_Voute_Ogive_8m");
            kit.arcDoubleau = ModelPiece("Kit_ArcDoubleau_14m", "SM_ArcDoubleau_14m");
            kit.colonneEngagee = ModelPiece("Kit_Colonne_Engagee_8m", "SM_Colonne_Engagee_8m");
            kit.murArcade = ModelPiece("Kit_Mur_Arcade_8m", "SM_Mur_Arcade_8m");
            kit.murNiche = ModelPiece("Kit_Mur_Niche_8m", "SM_Mur_Niche_8m");
            kit.torche = AssetDatabase.LoadAssetAtPath<GameObject>(PropLibrary.Dir + "/Prop_Torche.prefab");
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssets();
            return kit;
        }

        private static GameObject CubePiece(string name, Vector3 center, Vector3 size, Material mat)
        {
            var root = new GameObject(name);
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(cube.GetComponent<Collider>());     // visuel : la collision est la boîte fusionnée de KitBuild
            cube.name = name + " (greybox)";
            cube.transform.SetParent(root.transform, false);
            cube.transform.localPosition = center;
            cube.transform.localScale = size;
            cube.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.BatchingStatic);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{KitDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static GameObject ModelPiece(string name, string model)
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>($"{ModelDir}/{model}.fbx");
            if (fbx == null)
                return null;
            var root = new GameObject(name);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx, root.transform);
            foreach (Collider c in inst.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
                GameObjectUtility.SetStaticEditorFlags(r.gameObject, StaticEditorFlags.BatchingStatic);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, $"{KitDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Dans l'éditeur, les pièces gardent le lien à leur prefab (une retouche du kit se répercute partout).</summary>
        public static void UseEditorSpawner() => KitBuild.Spawner = (prefab, parent) => (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

        // ---------- Prefabs pré-câblés ----------

        private static GameObject SavePrefab(GameObject root, string name)
        {
            if (!AssetDatabase.IsValidFolder(LevelPrefabDir))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Niveau");
            GameObject p = PrefabUtility.SaveAsPrefabAsset(root, $"{LevelPrefabDir}/{name}.prefab");
            Object.DestroyImmediate(root);
            return p;
        }

        /// <summary>Sortie (grande porte, escaliers, palier) et arène du boss, autour d'une origine sur la face extérieure du mur.</summary>
        private static GameObject ExitArenaPrefab(KitSet kit)
        {
            var root = new GameObject("Sortie_Arene");
            Transform keep = _level;
            _level = root.transform;
            Nodes.Clear();
            Edges.Clear();
            Nodes.Add(new Node { Name = "Salle de sortie", C = new Vector3(0f, 0f, -T), Half = 28f, HalfZ = 0f, Ceil = 36f, Hall = true });
            _exitNode = 0;
            Vector3 landing = ExitStairs(Nodes[0]);
            Bounds arena = BossArena(landing);
            Node a = Nodes[Nodes.Count - 1];
            var marker = root.AddComponent<ExitArenaMarker>();
            marker.arenaCenter = a.C;
            marker.arenaHalfExtents = new Vector3(a.Half, a.Ceil * 0.5f, a.HalfZ);
            Bounds all = arena;
            all.Encapsulate(new Bounds(new Vector3(0f, 10f, (landing.z) * 0.5f), new Vector3(30f, 40f, landing.z + 20f)));
            marker.footprint = all;
            // Grande porte du kit autour du passage de 10 × 10 m (dans le mur : 1 m d'épaisseur derrière l'origine).
            Transform door = root.transform.Find("Porte de sortie");
            if (door != null && kit.grandePorte != null)
            {
                var gp = (GameObject)PrefabUtility.InstantiatePrefab(kit.grandePorte, door);
                gp.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                gp.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                Vector3 s = door.lossyScale;
                gp.transform.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
                gp.transform.position = new Vector3(0f, 0f, -T * 0.5f);
            }

            _level = keep;
            return SavePrefab(root, "Sortie_Arene");
        }

        private static GameObject MerchantPrefab()
        {
            var holder = new GameObject("tmp");
            BuildArtifactMerchant(holder.transform, Vector3.zero, Quaternion.identity, _economy, _offerOrb);
            GameObject m = holder.transform.GetChild(0).gameObject;
            m.transform.SetParent(null);
            Object.DestroyImmediate(holder);
            return SavePrefab(m, "Marchand_Artefact");
        }

        private static GameObject ChestPrefab(int price, string name)
        {
            Transform keep = _level;
            var holder = new GameObject("tmp");
            _level = holder.transform;
            BuildChest(name, Vector3.zero, Quaternion.identity, price);
            GameObject c = holder.transform.GetChild(0).gameObject;
            c.transform.SetParent(null);
            Object.DestroyImmediate(holder);
            _level = keep;
            return SavePrefab(c, name);
        }

        private static GameObject EnemyPrefab(bool flyer)
        {
            Transform keep = _ghouls;
            var holder = new GameObject("tmp");
            _ghouls = holder.transform;
            if (flyer)
                BuildFlyer(Vector3.zero, "(carte aléatoire)");
            else
                BuildGhoul(Vector3.zero, Quaternion.identity, "(carte aléatoire)");
            GameObject e = holder.transform.GetChild(0).gameObject;
            e.transform.SetParent(null);
            Object.DestroyImmediate(holder);
            _ghouls = keep;
            return SavePrefab(e, flyer ? "Volant" : "Goule");
        }

        /// <summary>
        /// Pièce annexe de 12 × 12 m, 6 m de haut (le traqueur passe le bras par la porte), en pièces du kit. Origine sur la face
        /// extérieure du mur du couloir, au pied de la porte de 3 × 3 m ; +Z vers l'intérieur. Le sol passe sous le mur du couloir
        /// (seuil des goules). Verrouillée : porte payante (E / Y) et coffre gratuit dedans.
        /// </summary>
        private static GameObject AnnexPrefab(KitSet kit, bool locked)
        {
            const float R = 12f, H = 6f, D = 3f;
            var root = new GameObject(locked ? "Salle_Verrouillee" : "Piece_Annexe");
            Transform t = root.transform;
            KitBuild.Box(t, "Sol", new Vector3(0f, -0.5f, R * 0.5f), new Vector3(R + 2f * T, 1f, R + 2f * T), KitBuild.Kind.Sol, kit);   // de la face extérieure du mur du couloir (−T) au fond : pas sous le sol du couloir
            // Toit : son bout côté couloir 2 cm dans le mur du couloir (affleurant, il scintille avec lui).
            KitBuild.Box(t, "Toit", new Vector3(0f, H + 0.5f, R * 0.5f + 0.01f), new Vector3(R + 2f * T, 1f, R + 2f * T - 0.02f), KitBuild.Kind.Sol, kit, walkable: false);
            KitBuild.Box(t, "Mur du fond", new Vector3(0f, H * 0.5f, R + T * 0.5f), new Vector3(R + 2f * T, H, T), KitBuild.Kind.Mur, kit, walkable: false);
            KitBuild.Box(t, "Mur 1", new Vector3(R * 0.5f + T * 0.5f, H * 0.5f, R * 0.5f), new Vector3(T, H, R), KitBuild.Kind.Mur, kit, walkable: false);
            KitBuild.Box(t, "Mur 2", new Vector3(-R * 0.5f - T * 0.5f, H * 0.5f, R * 0.5f), new Vector3(T, H, R), KitBuild.Kind.Mur, kit, walkable: false);
            // Cadre de 1 m dans un mur de 1 m : 2 cm de plus de chaque côté, sinon ses faces scintillent avec celles du mur.
            KitBuild.Piece(kit.portePiece, t, new Vector3(0f, 0f, -T * 0.5f), 180f, new Vector3(1f, 1f, 1.04f));
            var lamp = new GameObject("Lumière").AddComponent<Light>();
            lamp.transform.SetParent(t, false);
            lamp.transform.localPosition = new Vector3(0f, H - 1f, R * 0.5f);
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.6f, 0.35f);
            lamp.intensity = 2.5f;
            lamp.range = 14f;
            var holder = new GameObject("Pièce (le traqueur passe le bras)").transform;
            holder.SetParent(t, false);
            holder.localPosition = new Vector3(0f, H * 0.5f, R * 0.5f);
            var reach = holder.gameObject.AddComponent<ReachRoom>();
            holder.gameObject.AddComponent<RoomVolume>().Configure(locked ? "Salle verrouillée" : "Pièce annexe", new Vector3(R, H, R));
            var opening = new GameObject("Ouverture : porte").transform;
            opening.SetParent(holder, false);
            opening.localPosition = new Vector3(0f, D * 0.5f - H * 0.5f, -R * 0.5f - T * 0.5f);
            opening.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var so = new SerializedObject(reach);
            so.FindProperty("size").vector3Value = new Vector3(R, H, R);
            so.FindProperty("openings").arraySize = 1;
            so.FindProperty("openings").GetArrayElementAtIndex(0).objectReferenceValue = opening;
            so.ApplyModifiedPropertiesWithoutUndo();
            PropLibrary.Place("Caisse_Attrapable", t, new Vector3(3f, 0f, R * 0.5f));
            PropLibrary.Place("Tonneau", t, new Vector3(-4.5f, 0f, R - 1.5f));
            PropLibrary.Place("Etagere", t, new Vector3(4f, 0f, R - 0.6f), 180f);
            if (locked)
            {
                Material doorMat = GrabbableMat("Economy_Locked_Door", new Color(0.35f, 0.22f, 0.12f));
                GameObject block = Box("Porte verrouillée", t, new Vector3(0f, D * 0.5f, -T * 0.5f), new Vector3(D + 0.2f, D, T + 0.2f), doorMat);
                GameObjectUtility.SetStaticEditorFlags(block, 0);
                var ld = block.AddComponent<LockedDoor>();
                SetRef(ld, "settings", _economy);
                Transform keep = _level;
                _level = t;
                BuildChest("Coffre gratuit", new Vector3(-3f, 0f, R * 0.5f), Quaternion.Euler(0f, 180f, 0f), 0);
                _level = keep;
            }

            return SavePrefab(root, locked ? "Salle_Verrouillee" : "Piece_Annexe");
        }

        // ---------- Scène ----------

        /// <summary>Kit, prefabs, catalogue validé et scène Level_01_Aleatoire. Renvoie un résumé.</summary>
        public static string BuildRandomLevelScene()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
                return "Prefab joueur absent.";
            var scene = BeginScene(Level01LayoutPath, out StalkerSettings stalkerSettings);
            UseEditorSpawner();
            KitSet kit = BuildKit();
            ModuleCatalog catalog = ModuleValidator.EnsureCatalog();
            int invalidated = ModuleValidator.InvalidateChanged();
            var layout = LoadOrCreate<MapLayoutSettings>(MapLayoutPath);
            GameObject exitArena = ExitArenaPrefab(kit);
            GameObject merchant = MerchantPrefab();
            GameObject chest = ChestPrefab(-1, "Coffre");
            GameObject ghoul = EnemyPrefab(false);
            GameObject flyer = EnemyPrefab(true);
            GameObject annex = AnnexPrefab(kit, false);
            GameObject locked = AnnexPrefab(kit, true);
            KitBuild.Spawner = (prefab, parent) => Object.Instantiate(prefab, parent, false);
            var water = UnlitMat("Level_Water", "Universal Render Pipeline/Unlit", new Color(0.12f, 0.42f, 0.5f));   // lisible au fond des vides (animations, point 6)
            var zoneSettings = LoadOrCreate<ZoneSettings>(Level01ZonePath);

            // Gestion de la zone (comme FinishScene, sans la carte : elle est tirée au chargement).
            var graph = new GameObject("Graphe du labyrinthe (carte aléatoire)").AddComponent<MazeGraph>();
            var nav = new GameObject("NavMesh de la zone").AddComponent<ZoneNavMesh>();
            var nso = new SerializedObject(nav);
            nso.FindProperty("size").vector3Value = new Vector3(1000f, 100f, 1000f);
            nso.FindProperty("stalkerRadius").floatValue = stalkerSettings.width * 0.5f;
            nso.FindProperty("stalkerHeight").floatValue = stalkerSettings.crouchMinHeight;
            nso.FindProperty("smallVoxel").floatValue = 0.2f;
            nso.FindProperty("stalkerVoxel").floatValue = 0.3f;
            nso.ApplyModifiedPropertiesWithoutUndo();
            var start = new GameObject("Départ des joueurs").transform;
            var zone = new GameObject("État de la zone").AddComponent<ZoneState>();
            SetRef(zone, "settings", zoneSettings);
            SetRef(zone, "playerStart", start);
            var runState = new GameObject("État de la run").AddComponent<ItHunts.Artifacts.RunState>();
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
            var souls = new GameObject("Âmes (éclats)").AddComponent<SoulSystem>();
            SetRef(souls, "settings", _economy);
            Material soulMat = UnlitMat("Economy_Soul", "Universal Render Pipeline/Unlit", _economy.soulColor);
            soulMat.enableInstancing = true;
            SetRef(souls, "soulMaterial", soulMat);
            BuildStalker(stalkerSettings, new Vector3(0f, 0f, -2000f));
            Transform stalker = _level.Find("Traqueur");

            // Réserve des vagues : à la taille d'une carte de 49 cases (densité par surface, échelle au plus haut).
            var waves = LoadOrCreate<WaveSettings>(WavesPath);
            const float areaScale = 2.5f;
            var ghoulPool = new GameObject("Réserve des vagues : goules").transform;
            var flyerPool = new GameObject("Réserve des vagues : volants").transform;
            _ghouls = ghoulPool;
            for (int i = 0; i < Mathf.CeilToInt(waves.ghoulPool * areaScale); i++)
                BuildGhoul(new Vector3(i * 2f, -60f, 0f), Quaternion.identity, $"(réserve) {i + 1}");
            _ghouls = flyerPool;
            for (int i = 0; i < Mathf.CeilToInt(waves.flyerPool * areaScale); i++)
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

            // Générateur : s'exécute avant tout le monde au chargement.
            var gen = new GameObject("Carte aléatoire (générateur)").AddComponent<RandomLevel>();
            SetRef(gen, "settings", layout);
            SetRef(gen, "catalog", catalog);
            SetRef(gen, "kit", kit);
            SetRef(gen, "exitArena", exitArena);
            SetRef(gen, "merchant", merchant);
            SetRef(gen, "chest", chest);
            SetRef(gen, "ghoul", ghoul);
            SetRef(gen, "flyer", flyer);
            SetRef(gen, "annex", annex);
            SetRef(gen, "lockedAnnex", locked);
            SetRef(gen, "water", water);
            SetRef(gen, "nonMagnetic", NonMagneticMat());
            SetRef(gen, "movement", AssetDatabase.LoadAssetAtPath<ItHunts.Settings.MovementSettings>(SettingsDir + "/Movement_Default.asset"));   // écarts des parcours
            SetRef(gen, "zoneNav", nav);
            SetRef(gen, "graph", graph);
            SetRef(gen, "playerStart", start);
            SetRef(gen, "stalker", stalker);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.up * 2f;

            // Systèmes de jeu sous une racine inactive : le générateur l'active quand la carte est finie (écran de chargement).
            var game = new GameObject("Jeu (activé quand la carte est prête)");
            foreach (Transform t in new[] { graph.transform, nav.transform, start, zone.transform, souls.transform, feedback.transform,
                         split.transform, _level, _spawns, ghoulPool, flyerPool, director.transform, player.transform })
                if (t != null)
                    t.SetParent(game.transform, true);
            foreach (string n in new[] { "Minimap (brouillard de guerre)", "Mesure de performance du niveau (build, -ithunts-perf-level)" })
            {
                GameObject go = GameObject.Find(n);
                if (go != null)
                    go.transform.SetParent(game.transform, true);
            }

            game.SetActive(false);
            SetRef(gen, "gameplayRoot", game);
            EditorSceneManager.SaveScene(scene, RandomScenePath);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == RandomScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(RandomScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            int usable = catalog.Usable().Count;
            return $"OK : {RandomScenePath} ; kit {KitDir} ; prefabs {LevelPrefabDir} ; catalogue : {usable} modules validés sur {catalog.modules.Count}" +
                   (invalidated > 0 ? $" ({invalidated} modifiés depuis leur validation : à valider)" : "");
        }
    }
}
