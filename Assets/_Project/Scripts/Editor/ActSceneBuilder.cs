using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItHunts.Combat;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Level.Acts;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Run en 3 actes + boss (D-092, nuit 3), côté éditeur : réglages de chaque acte (ActLayoutSettings, ZoneSettings), matériaux des
    /// actes (copies teintées des matériaux en coordonnées monde de la DA, sans toucher Assets/Art), prefabs (marchand des
    /// améliorations, coffre gratuit) et les scènes Acte_1_Caves, Acte_2_Chateau, Acte_3_Jardin (tout sauf le niveau, tiré au
    /// chargement par ActLevel). La définition de la run enchaîne les actes.
    /// </summary>
    public static partial class TestArenaSceneBuilder
    {
        public const string ActsDir = SettingsDir + "/Actes";
        public const string ActMaterialDir = "Assets/_Project/Art/Actes";
        public static readonly string[] ActScenes = { "Acte_1_Caves", "Acte_2_Chateau", "Acte_3_Jardin", "Acte_4_Boss" };
        public static readonly string[] ActNames = { "Acte I : les caves", "Acte II : les étages du château", "Acte III : le jardin", "Boss final" };

        [MenuItem("ItHunts/Actes : reconstruire les réglages, les prefabs et les scènes des 3 actes")]
        private static void BuildActsFromMenu() => Debug.Log(BuildActScenes());

        public static string BuildActScenes()
        {
            var log = new List<string>();
            for (int i = 0; i < 3; i++)
                log.Add(BuildActScene(i));
            log.Add(UpdateRunDefinition());
            AssetDatabase.SaveAssets();
            return string.Join("\n", log);
        }

        private static Material ActMat(string name, string source, Color tint, Vector2? tile = null, Color? emission = null)
        {
            EnsureFolder(ActMaterialDir);
            string path = $"{ActMaterialDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null)
                return m;
            var src = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/Niveau1/{source}.mat");
            m = src != null ? new Material(src) : new Material(Shader.Find("ItHunts/Toon") ?? Shader.Find("Universal Render Pipeline/Lit"));
            m.name = name;
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", tint);
            if (tile.HasValue && m.HasProperty("_TileSize"))
                m.SetVector("_TileSize", new Vector4(tile.Value.x, tile.Value.y, 0f, 0f));
            if (emission.HasValue && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetFloat("_Emission", 1f);
                m.SetColor("_EmissionColor", emission.Value);
            }

            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static Material RayMaterial()
        {
            string path = $"{ActMaterialDir}/Acte_RayonLune.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null)
                return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);                                         // additif
            m.SetColor("_BaseColor", new Color(0.35f, 0.42f, 0.7f, 0.08f));
            m.renderQueue = 3000;
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            m.SetInt("_ZWrite", 0);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Réglages d'un acte : créés avec les valeurs de la nuit 3 s'ils n'existent pas, sinon gardés (réglages de l'utilisateur).</summary>
        private static ActLayoutSettings ActSettings(int act, KitSet kit)
        {
            EnsureFolder(ActsDir);
            string path = $"{ActsDir}/Act_{act + 1}.asset";
            var s = AssetDatabase.LoadAssetAtPath<ActLayoutSettings>(path);
            bool created = s == null;
            if (created)
            {
                s = ScriptableObject.CreateInstance<ActLayoutSettings>();
                AssetDatabase.CreateAsset(s, path);
            }

            s.displayName = ActNames[act];
            s.kit = kit;
            Material water = WaterSetup.Water();
            Material wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Niveau1/M_GrandePorte_Bois.mat");
            Material iron = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Niveau1/M_FerRouille.mat");
            if (created)
                switch (act)
                {
                    case 0:
                        s.theme = ActTheme.Caves;
                        s.rooms = 16;
                        s.roomArea = new Vector2(700f, 2300f);
                        s.ceiling = new Vector2(16f, 22f);
                        s.floorStep = 3f;
                        s.floorChangeChance = 0.35f;
                        s.waterShare = 0.3f;
                        s.voidShare = 0.25f;
                        s.terraceShare = 0.55f;
                        s.sconceSpacing = 10f;
                        s.ghoulsPer100 = 0.22f;
                        s.chests = 9;
                        s.lockedRooms = 2;
                        s.stalkerFromArrival = false;
                        break;
                    case 1:
                        s.theme = ActTheme.Chateau;
                        s.rooms = 15;
                        s.roomArea = new Vector2(800f, 2600f);
                        s.ceiling = new Vector2(22f, 30f);
                        s.floorStep = 4f;
                        s.floorChangeChance = 0.4f;
                        s.waterShare = 0.15f;
                        s.voidShare = 0.2f;
                        s.terraceShare = 0.7f;
                        s.terraceHeight = new Vector2(4f, 5.5f);
                        s.sconceSpacing = 9f;
                        s.chandeliersPer1000 = 1.2f;
                        s.moonWindowSpacing = 14f;
                        s.ambientScale = 1.35f;
                        s.torchColor = new Color(1f, 0.62f, 0.32f);
                        s.ghoulsPer100 = 0.24f;
                        s.chests = 9;
                        s.lockedRooms = 2;
                        s.doorHeight = 10f;
                        s.stalkerFromArrival = true;
                        break;
                    default:
                        s.theme = ActTheme.Jardin;
                        s.rooms = 16;
                        s.roomArea = new Vector2(1200f, 3400f);
                        s.ceiling = Vector2.zero;
                        s.minWidth = 12f;
                        s.floorStep = 3f;
                        s.floorChangeChance = 0.3f;
                        s.waterShare = 0.3f;
                        s.voidShare = 0.2f;
                        s.terraceShare = 0.5f;
                        s.sconceSpacing = 16f;
                        s.moonlight = 0.55f;
                        s.ambientScale = 1.2f;
                        s.ghoulsPer100 = 0.2f;
                        s.maxGhoulsPerRoom = 9;
                        s.chests = 10;
                        s.lockedRooms = 1;
                        s.doorWidth = 10f;
                        s.stalkerFromArrival = true;
                        break;
                }

            // Matériaux (copies teintées des matériaux en coordonnées monde de la DA).
            switch (s.theme)
            {
                case ActTheme.Caves:
                    s.wallMaterial = ActMat("Acte1_Mur", "M_Pierre_Monde", new Color(0.82f, 0.8f, 0.78f));
                    s.floorMaterial = ActMat("Acte1_Sol", "M_Dalles_Monde", new Color(0.8f, 0.78f, 0.76f));
                    s.ceilingMaterial = ActMat("Acte1_Voute", "M_Voute_Briques_Monde", new Color(0.55f, 0.52f, 0.5f));
                    s.trimMaterial = ActMat("Acte1_Taille", "M_Pierre_Monde", new Color(0.62f, 0.58f, 0.55f), new Vector2(2f, 2f));
                    break;
                case ActTheme.Chateau:
                    s.wallMaterial = ActMat("Acte2_Mur", "M_Pierre_Monde", new Color(0.95f, 0.9f, 0.84f), new Vector2(3f, 3f));
                    s.floorMaterial = ActMat("Acte2_Sol", "M_Dalles_Monde", new Color(0.72f, 0.55f, 0.45f), new Vector2(2f, 2f));
                    s.ceilingMaterial = ActMat("Acte2_Voute", "M_Voute_Briques_Monde", new Color(0.6f, 0.55f, 0.5f));
                    s.trimMaterial = ActMat("Acte2_Taille", "M_Pierre_Monde", new Color(0.7f, 0.62f, 0.52f), new Vector2(1.5f, 1.5f));
                    break;
                default:
                    s.wallMaterial = ActMat("Acte3_Falaise", "M_Pierre_Monde", new Color(0.5f, 0.52f, 0.5f), new Vector2(8f, 8f));
                    s.floorMaterial = ActMat("Acte3_Sol", "M_Dalles_Monde", new Color(0.36f, 0.42f, 0.3f), new Vector2(6f, 6f));
                    s.ceilingMaterial = ActMat("Acte3_Fond", "M_Fond_Noir", new Color(0.05f, 0.05f, 0.06f));
                    s.trimMaterial = ActMat("Acte3_Taille", "M_Pierre_Monde", new Color(0.66f, 0.66f, 0.62f), new Vector2(1.5f, 1.5f));
                    break;
            }

            s.waterMaterial = water;
            s.propMaterial = wood;
            s.foliageMaterial = ActMat("Acte_Feuillage", "M_Fond_Noir", new Color(0.12f, 0.24f, 0.14f));
            s.windowMaterial = ActMat("Acte_Vitrail", "M_Fond_Noir", new Color(0.2f, 0.25f, 0.4f), null, new Color(0.45f, 0.6f, 1.4f));
            if (iron != null && s.theme == ActTheme.Chateau)
                s.trimMaterial = s.trimMaterial != null ? s.trimMaterial : iron;
            s.props = AssetDatabase.FindAssets("t:Prefab Prop_", new[] { "Assets/_Project/Prefabs/Props" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g))).Where(p => p != null).ToList();
            EditorUtility.SetDirty(s);
            return s;
        }

        private static ZoneSettings ActZone(int act)
        {
            string path = $"{ActsDir}/Zone_Acte{act + 1}.asset";
            var z = AssetDatabase.LoadAssetAtPath<ZoneSettings>(path);
            if (z != null)
                return z;
            z = ScriptableObject.CreateInstance<ZoneSettings>();
            z.timer = 720f;
            z.gracePeriod = act == 0 ? 150f : 60f;
            z.exitUnlockAt = 240f;
            z.exitPrice = 200 + act * 50;
            z.deathEndsRun = true;
            AssetDatabase.CreateAsset(z, path);
            return z;
        }

        /// <summary>Marchand des améliorations (D-043) : silhouette du marchand, offres posées par le code.</summary>
        private static GameObject UpgradeMerchantPrefab()
        {
            var holder = new GameObject("tmp");
            BuildArtifactMerchant(holder.transform, Vector3.zero, Quaternion.identity, _economy, _offerOrb);
            GameObject m = holder.transform.GetChild(0).gameObject;
            m.transform.SetParent(null);
            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(m.GetComponent<ArtifactMerchant>());
            m.name = "Marchand";
            var mer = m.AddComponent<Merchant>();
            SetRef(mer, "settings", _economy);
            SetRef(mer, "orbMaterial", _offerOrb);
            return SavePrefab(m, "Marchand_Ameliorations");
        }

        public static string BuildActScene(int act)
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
                return "Prefab joueur absent.";
            var scene = BeginScene(Level01LayoutPath, out StalkerSettings stalkerSettings);
            UseEditorSpawner();
            KitSet kit = AssetDatabase.LoadAssetAtPath<KitSet>(KitPath);
            ActLayoutSettings layout = ActSettings(act, kit);
            ZoneSettings zoneSettings = ActZone(act);
            GameObject artifactMerchant = AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelPrefabDir}/Marchand_Artefact.prefab") ?? MerchantPrefab();
            GameObject merchant = UpgradeMerchantPrefab();
            GameObject chest = AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelPrefabDir}/Coffre.prefab") ?? ChestPrefab(-1, "Coffre");
            GameObject freeChest = ChestPrefab(0, "Coffre_Gratuit");
            GameObject ghoul = AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelPrefabDir}/Goule.prefab") ?? EnemyPrefab(false);
            GameObject flyer = AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelPrefabDir}/Volant.prefab") ?? EnemyPrefab(true);
            KitBuild.Spawner = (prefab, parent) => Object.Instantiate(prefab, parent, false);

            var graph = new GameObject("Graphe des salles (acte)").AddComponent<MazeGraph>();
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
            SetRef(runState, "characters", CharacterSetup.Roster());
            RenderSetup.AddTo(null);
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

            var gen = new GameObject("Niveau de l'acte (générateur)").AddComponent<ActLevel>();
            SetRef(gen, "settings", layout);
            SetRef(gen, "artifactMerchant", artifactMerchant);
            SetRef(gen, "merchant", merchant);
            SetRef(gen, "chest", chest);
            SetRef(gen, "freeChest", freeChest);
            SetRef(gen, "ghoul", ghoul);
            SetRef(gen, "flyer", flyer);
            SetRef(gen, "waterSplash", WaterSetup.Splash());
            SetRef(gen, "zoneNav", nav);
            SetRef(gen, "graph", graph);
            SetRef(gen, "playerStart", start);
            SetRef(gen, "stalker", stalker);
            SetRef(gen, "economy", _economy);
            SetRef(gen, "_rayMaterial", RayMaterial());
            Light moon = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            SetRef(gen, "moon", moon);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.up * 2f;

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
            if (layout.theme == ActTheme.Jardin)
            {
                RenderSettings.ambientLight = RenderSettings.ambientLight * layout.ambientScale;
                if (moon != null)
                {
                    moon.transform.rotation = Quaternion.Euler(38f, -50f, 0f);
                    moon.color = layout.moonColor;
                    moon.intensity = layout.moonlight;
                }
            }
            else if (layout.ambientScale > 1.01f)
                RenderSettings.ambientLight = RenderSettings.ambientLight * layout.ambientScale;

            string path = $"Assets/Scenes/{ActScenes[act]}.unity";
            EditorSceneManager.SaveScene(scene, path);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            return $"OK : {path}";
        }

        /// <summary>La run enchaîne les actes (Facile : les 3 actes et le boss final).</summary>
        private static string UpdateRunDefinition()
        {
            var run = AssetDatabase.LoadAssetAtPath<RunDefinition>(SettingsDir + "/Run_Default.asset");
            if (run == null)
                return "Run_Default absent";
            run.levels.Clear();
            for (int i = 0; i < 4; i++)
                run.levels.Add(new RunLevel { displayName = ActNames[i], sceneName = i < 3 || File.Exists($"Assets/Scenes/{ActScenes[i]}.unity") ? ActScenes[i] : "" });
            foreach (RunDifficulty d in run.difficulties)
                d.levelCount = 4;
            if (run.difficulties.Count > 0)
            {
                run.difficulties[0].description = "Les caves, les étages du château, le jardin, puis le traqueur en combat final.";
                run.difficulties[0].playable = true;
            }

            EditorUtility.SetDirty(run);
            return "Run_Default : 3 actes + boss final";
        }
    }
}
