using ItHunts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Eau de la DA (PR #24, docs/art/EAU.md) : matériau ItHunts/EauToon (M_Eau_Toon) sur toutes les surfaces d'eau, éclaboussure
    /// peinte (VFX_Eau_Eclaboussure) sur les déclencheurs d'eau. Les constructeurs de niveaux passent par Water() et Splash() ;
    /// ApplyToScenes() met à jour les scènes existantes sans les reconstruire.
    /// </summary>
    public static class WaterSetup
    {
        public const string ToonPath = "Assets/Art/Eau/Materiaux/M_Eau_Toon.mat";
        public const string SplashPath = "Assets/Art/Eau/Prefabs/VFX_Eau_Eclaboussure.prefab";
        private const string GreyboxPath = "Assets/_Project/Art/Greybox/Level_Water.mat";

        /// <summary>Matériau de l'eau : celui de la DA, sinon l'eau greybox.</summary>
        public static Material Water()
        {
            var toon = AssetDatabase.LoadAssetAtPath<Material>(ToonPath);
            return toon != null ? toon : TestMovementSceneBuilder.UnlitMat("Level_Water", "Universal Render Pipeline/Unlit", new Color(0.12f, 0.42f, 0.5f));
        }

        public static GameObject Splash() => AssetDatabase.LoadAssetAtPath<GameObject>(SplashPath);

        [MenuItem("ItHunts/Eau de la DA dans les niveaux existants")]
        private static void Menu() => Debug.Log(ApplyToScenes());

        public static string ApplyToScenes()
        {
            Material toon = AssetDatabase.LoadAssetAtPath<Material>(ToonPath);
            Material grey = AssetDatabase.LoadAssetAtPath<Material>(GreyboxPath);
            GameObject splash = Splash();
            if (toon == null)
                return "M_Eau_Toon absent";
            var report = new System.Text.StringBuilder();
            foreach (string path in new[] { "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_01_Base.unity", "Assets/Scenes/Level_01_Aleatoire.unity" })
            {
                if (!System.IO.File.Exists(path))
                    continue;
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int surfaces = 0, splashes = 0;
                foreach (MeshRenderer r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (r.sharedMaterial != null && (r.sharedMaterial == grey || r.sharedMaterial.name == "Level_Water"))
                    {
                        r.sharedMaterial = toon;
                        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        surfaces++;
                    }

                foreach (DeepWater d in Object.FindObjectsByType<DeepWater>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    splashes += AddSplash(d.gameObject, splash, float.NaN);
                foreach (FallVoid f in Object.FindObjectsByType<FallVoid>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (f.name.ToLowerInvariant().Contains("eau"))
                        splashes += AddSplash(f.gameObject, splash, float.NaN);
                foreach (RandomLevel g in Object.FindObjectsByType<RandomLevel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(g);
                    so.FindProperty("water").objectReferenceValue = toon;
                    so.FindProperty("waterSplash").objectReferenceValue = splash;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    surfaces++;
                }

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                report.Append(System.IO.Path.GetFileNameWithoutExtension(path)).Append($" : {surfaces} eau(x), {splashes} éclaboussure(s) ; ");
            }

            return report.ToString();
        }

        private static int AddSplash(GameObject go, GameObject prefab, float surfaceY)
        {
            if (prefab == null || go.GetComponent<WaterSplash>() != null)
                return 0;
            WaterSplash.Add(go, prefab, surfaceY);
            return 1;
        }
    }
}
