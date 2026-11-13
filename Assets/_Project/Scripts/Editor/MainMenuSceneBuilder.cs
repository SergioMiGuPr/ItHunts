using ItHunts.Level;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>Menu principal greybox (D-048) et définition de la run (Run_Default) ; le menu est la première scène du build.</summary>
    public static class MainMenuSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/MainMenu.unity";
        public const string RunPath = SettingsDir + "/Run_Default.asset";

        [MenuItem("ItHunts/Reconstruire le menu principal et la définition de la run")]
        private static void BuildFromMenu() => Debug.Log(Build());

        /// <summary>Définition de la run : créée avec ces valeurs si elle n'existe pas (sinon gardée, réglages compris).</summary>
        public static RunDefinition RunAsset()
        {
            var def = AssetDatabase.LoadAssetAtPath<RunDefinition>(RunPath);
            if (def != null)
                return def;
            def = ScriptableObject.CreateInstance<RunDefinition>();
            def.menuScene = "MainMenu";
            def.levels.Add(new RunLevel { displayName = "Niveau 1 : la cave du château", sceneName = "Level_01" });
            def.levels.Add(new RunLevel { displayName = "Niveau 2" });
            def.levels.Add(new RunLevel { displayName = "Niveau 3" });
            def.levels.Add(new RunLevel { displayName = "Zone finale : fuite et combat final" });
            def.difficulties.Add(new RunDifficulty
            {
                displayName = "Facile", playable = true, levelCount = 1,
                description = "Niveau 1, puis son combat de boss. Ennemis normaux.",
            });
            def.difficulties.Add(new RunDifficulty
            {
                displayName = "Moyenne", playable = false, levelCount = 2, enemyHealth = 1.4f, enemyDamage = 1.25f, enemySpeed = 1.1f, enemyCount = 1.25f,
                description = "Niveaux 1 et 2, ennemis plus forts.",
            });
            def.difficulties.Add(new RunDifficulty
            {
                displayName = "Difficile", playable = false, levelCount = 4, enemyHealth = 1.8f, enemyDamage = 1.5f, enemySpeed = 1.2f, enemyCount = 1.5f,
                description = "Niveaux 1, 2 et 3, fuite devant le boss, combat final. Ennemis encore plus forts.",
            });
            AssetDatabase.CreateAsset(def, RunPath);
            AssetDatabase.SaveAssets();
            return def;
        }

        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RunDefinition def = RunAsset();
            var cam = new GameObject("Caméra du menu").AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.03f, 0.06f);
            cam.transform.position = new Vector3(0f, 1.6f, -6f);
            var orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.name = "Orbe (décor)";
            orb.transform.position = new Vector3(2.6f, 1.6f, 0f);
            orb.GetComponent<MeshRenderer>().sharedMaterial = UnlitMat("Menu_Orb", "Universal Render Pipeline/Unlit", new Color(0.4f, 0.7f, 2.5f));
            var menu = new GameObject("Menu principal").AddComponent<MainMenu>();
            SetRef(menu, "run", def);
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            return $"OK : {ScenePath} (première scène du build), définition de la run {RunPath}";
        }
    }
}
