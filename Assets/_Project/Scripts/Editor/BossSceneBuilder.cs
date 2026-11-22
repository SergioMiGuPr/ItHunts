using System.Collections.Generic;
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
    /// <summary>Scène du boss final (nuit 3, point 8) : arène en contrebas du jardin (BossArena), traqueur, joueur, zone, run.</summary>
    public static partial class TestArenaSceneBuilder
    {
        [MenuItem("ItHunts/Actes : reconstruire la scène du boss final (Acte_4_Boss)")]
        private static void BuildBossFromMenu() => Debug.Log(BuildBossScene());

        private static GameObject Kit(string path) => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Prefabs/" + path + ".prefab");

        public static string BuildBossScene()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            var scene = BeginScene(Level01LayoutPath, out StalkerSettings stalkerSettings);
            EnsureFolder(ActsDir);
            var bossSettings = AssetDatabase.LoadAssetAtPath<FinalBossSettings>(ActsDir + "/FinalBoss_Default.asset");
            if (bossSettings == null)
            {
                bossSettings = ScriptableObject.CreateInstance<FinalBossSettings>();
                AssetDatabase.CreateAsset(bossSettings, ActsDir + "/FinalBoss_Default.asset");
            }

            string zonePath = ActsDir + "/Zone_Boss.asset";
            var zoneSettings = AssetDatabase.LoadAssetAtPath<ZoneSettings>(zonePath);
            if (zoneSettings == null)
            {
                zoneSettings = ScriptableObject.CreateInstance<ZoneSettings>();
                zoneSettings.timer = 3600f;
                zoneSettings.gracePeriod = 3600f;
                zoneSettings.exitUnlockAt = 3600f;
                zoneSettings.timerWaitsForStartRoom = false;
                AssetDatabase.CreateAsset(zoneSettings, zonePath);
            }

            var nav = new GameObject("NavMesh de la zone").AddComponent<ZoneNavMesh>();
            var nso = new SerializedObject(nav);
            nso.FindProperty("stalkerRadius").floatValue = stalkerSettings.width * 0.5f;
            nso.FindProperty("stalkerHeight").floatValue = stalkerSettings.crouchMinHeight;
            nso.FindProperty("smallVoxel").floatValue = 0.2f;
            nso.FindProperty("stalkerVoxel").floatValue = 0.4f;
            nso.ApplyModifiedPropertiesWithoutUndo();
            var start = new GameObject("Départ des joueurs").transform;
            var zone = new GameObject("État de la zone").AddComponent<ZoneState>();
            SetRef(zone, "settings", zoneSettings);
            SetRef(zone, "playerStart", start);
            var runState = new GameObject("État de la run").AddComponent<ItHunts.Artifacts.RunState>();
            SetRef(runState, "pool", LoadOrCreate<ArtifactPool>(PoolPath));
            SetRef(runState, "characters", CharacterSetup.Roster());
            SetRef(runState, "upgrades", UpgradePoolAsset());
            RenderSetup.AddTo(null);
            var split = new GameObject("Écran partagé").AddComponent<ItHunts.Player.SplitScreen>();
            SetRef(split, "playerPrefab", playerPrefab);
            SetRef(split, "settings", LoadOrCreate<CoopSettings>(SettingsDir + "/Coop_Default.asset"));
            var feedback = new GameObject("Retour des dégâts").AddComponent<DamageFeedback>();
            SetRef(feedback, "settings", LoadOrCreate<DamageFeedbackSettings>(DamageFeedbackPath));
            SetRef(feedback, "flashMaterial", UnlitMat("Damage_Flash", "Universal Render Pipeline/Unlit", Color.white));
            SetRef(feedback, "lineMaterial", UnlitMat("Orb_Line", "Universal Render Pipeline/Particles/Unlit", Color.white));
            var souls = new GameObject("Âmes (éclats)").AddComponent<SoulSystem>();
            SetRef(souls, "settings", _economy);
            Material soulMat = UnlitMat("Economy_Soul", "Universal Render Pipeline/Unlit", _economy.soulColor);
            SetRef(souls, "soulMaterial", soulMat);
            BuildStalker(stalkerSettings, new Vector3(0f, 0f, -18f));
            Transform stalker = _level.Find("Traqueur");

            var arena = new GameObject("Arène du boss (générateur)").AddComponent<BossArena>();
            SetRef(arena, "boss", bossSettings);
            SetRef(arena, "stalker", stalker);
            SetRef(arena, "playerStart", start);
            SetRef(arena, "zoneNav", nav);
            SetRef(arena, "ghoul", AssetDatabase.LoadAssetAtPath<GameObject>($"{LevelPrefabDir}/Goule.prefab"));
            SetRef(arena, "crate", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/Prop_Caisse_Attrapable.prefab"));
            SetRef(arena, "floorMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Bibliotheque/M_Bib_Terre_Racines.mat"));
            SetRef(arena, "wallMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Bibliotheque/M_Bib_RocheCave.mat"));
            SetRef(arena, "cliff", Kit("KitJardin/Jardin_Falaise_16m"));
            SetRef(arena, "stairs", Kit("KitJardin/Arene_EscalierDescente"));
            SetRef(arena, "pillar", Kit("KitJardin/Arene_PilierBrise"));
            SetRef(arena, "cover", Kit("KitJardin/Arene_Couvert"));
            SetRef(arena, "mound", Kit("KitJardin/Arene_Butte"));
            SetRef(arena, "statue", Kit("KitJardin/Jardin_Statue_Geante"));
            SetRef(arena, "candelabra", Kit("KitCaves/Cave_Candelabre"));
            SetRef(arena, "brazier", Kit("KitCaves/Cave_Brasero"));
            SetRef(arena, "sky", Kit("KitJardin/Jardin_Ciel"));
            SetRef(arena, "mist", Kit("KitJardin/Jardin_Brume"));

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 9f, 58f);
            Light moon = Object.FindObjectsByType<Light>(FindObjectsSortMode.None).FirstOrDefault(l => l.type == LightType.Directional);
            if (moon != null)
            {
                moon.transform.rotation = Quaternion.Euler(35f, 160f, 0f);
                moon.color = new Color(0.62f, 0.7f, 1f);
                moon.intensity = 0.6f;
            }

            var game = new GameObject("Jeu (activé quand l'arène est prête)");
            foreach (Transform t in new[] { nav.transform, start, zone.transform, souls.transform, feedback.transform, split.transform, _level, player.transform })
                if (t != null)
                    t.SetParent(game.transform, true);
            game.SetActive(false);
            SetRef(arena, "gameplayRoot", game);
            const string path = "Assets/Scenes/Acte_4_Boss.unity";
            EditorSceneManager.SaveScene(scene, path);
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            UpdateRunDefinition();
            AssetDatabase.SaveAssets();
            return "OK : " + path;
        }
    }
}
