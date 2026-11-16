using System.Linq;
using ItHunts.Level;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Rendu du jeu (D-067, docs/art/RENDU.md fait foi) sur `main` : passe de contours plein écran (M_Contours, clair dans le
    /// noir) et pré-passe sans les couches des mains, GPU Resident Drawer (Instanced Drawing) et variantes BatchRendererGroup
    /// gardées, ambiance partagée (intermédiaire, « horreur » prête) dans toutes les scènes de jeu, passes des mains
    /// (transparents, contour), contour des mains et des artefacts (coque inversée). Idempotent.
    /// </summary>
    public static class RenderSetup
    {
        /// <summary>Composant existant ou ajouté (pas de « ?? » : GetComponent renvoie un faux null dans l'éditeur).</summary>
        private static T Comp<T>(Component c) where T : Component => Comp<T>(c.gameObject);

        private static T Comp<T>(GameObject go) where T : Component
        {
            T t = go.GetComponent<T>();
            return t != null ? t : go.AddComponent<T>();
        }

        public const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        public const string PipelinePath = "Assets/Settings/PC_RPAsset.asset";
        public const string ContoursMaterialPath = "Assets/Art/Materials/Rendu/M_Contours.mat";
        public const string OutlineMaterialPath = "Assets/_Project/Art/Rendu/M_ContourMains.mat";
        public const string IntermediatePath = SettingsDir + "/Ambiance_Intermediaire.asset";
        public const string HorrorPath = SettingsDir + "/Ambiance_Horreur.asset";
        public const string ContoursName = "ItHunts Contours";
        public static readonly string[] GameScenes =
            { "Assets/Scenes/Test_Movement.unity", "Assets/Scenes/Test_Arena.unity", "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_01_Base.unity", "Assets/Scenes/Level_01_Aleatoire.unity" };

        [MenuItem("ItHunts/Rendu : contours, pré-passe, GPU Resident Drawer, ambiance, passes et contour des mains")]
        public static void Menu() => Debug.Log(All());

        public static string All() =>
            string.Join(" ; ", Contours(true), Performance(), HandsPasses(), Outline(), Ambiance());

        /// <summary>Passe de contours (Full Screen Pass) et pré-passe : fixPrepass = false remet l'ancienne pré-passe (captures « avant »).</summary>
        public static string Contours(bool fixPrepass)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(ContoursMaterialPath);
            if (data == null || mat == null)
                return "ERREUR : renderer ou M_Contours introuvable";
            var f = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(x => x.name == ContoursName);
            if (f == null)
            {
                f = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                f.name = ContoursName;
                AssetDatabase.AddObjectToAsset(f, data);
                data.rendererFeatures.Insert(0, f);
            }

            f.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
            f.requirements = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            f.fetchColorBuffer = true;
            f.bindDepthStencilAttachment = false;
            f.passMaterial = mat;
            f.passIndex = 0;
            f.SetActive(true);
            EditorUtility.SetDirty(f);

            var so = new SerializedObject(data);
            SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
            map.arraySize = data.rendererFeatures.Count;
            for (int i = 0; i < data.rendererFeatures.Count; i++)
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[i], out string _, out long id))
                    map.GetArrayElementAtIndex(i).longValue = id;
            // Pré-passe profondeur + normales : sans les couches des mains (contours fantômes, SSAO), comme les passes opaques.
            uint opaque = (uint)so.FindProperty("m_OpaqueLayerMask.m_Bits").longValue;
            so.FindProperty("m_PrepassLayerMask.m_Bits").longValue = fixPrepass ? opaque : uint.MaxValue;
            so.ApplyModifiedPropertiesWithoutUndo();
            data.SetDirty();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return $"contours (passe plein écran, M_Contours) ; pré-passe {(fixPrepass ? $"= couches opaques ({opaque})" : "= tout (ancienne)")}";
        }

        /// <summary>
        /// GPU Resident Drawer et variantes BatchRendererGroup (RENDU.md § 4). Mesuré dans un build sur la carte aléatoire, pic de vague
        /// actif : activé, 1 007 draw calls mais 37 ms par image en solo (69.9 à deux) ; coupé, 4 712 draw calls et 9.0 ms (15.7) :
        /// le coût CPU de ses milliers de renderers créés à l'exécution l'emporte. Coupé par défaut (gpuResidentDrawer = false) ;
        /// les variantes sont gardées (Keep All) pour pouvoir le réessayer sans rebuild des shaders.
        /// </summary>
        public static string Performance(bool gpuResidentDrawer = false)
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (rp == null)
                return "ERREUR : PC_RPAsset introuvable";
            var rps = new SerializedObject(rp);
            rps.FindProperty("m_GPUResidentDrawerMode").intValue = gpuResidentDrawer ? 1 : 0;   // 1 : Instanced Drawing
            rps.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rp);
            var gs = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
            gs.FindProperty("m_BrgStripping").intValue = 2;                  // Keep All
            gs.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return $"GPU Resident Drawer : {(gpuResidentDrawer ? "Instanced Drawing" : "coupé (mesuré plus lent)")} ; BatchRendererGroup Variants : Keep All";
        }

        /// <summary>Passes des mains (transparents, étiquette du contour) : voir EnsureFirstPersonRendering.</summary>
        public static string HandsPasses()
        {
            EnsureFirstPersonRendering(LoadOrCreate<FpsViewSettings>(SettingsDir + "/FpsView_Default.asset"));
            return "passes des mains : mains (+ contour), transparents, HUD";
        }

        /// <summary>Matériau du contour des mains et composant HandsOutline sur le prefab joueur.</summary>
        public static string Outline()
        {
            EnsureFolder("Assets/_Project/Art/Rendu");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("ItHunts/ContourMains")) { name = "M_ContourMains" };
                AssetDatabase.CreateAsset(mat, OutlineMaterialPath);
            }

            mat.enableInstancing = true;
            mat.SetColor("_OutlineColor", new Color(0.01f, 0.008f, 0.012f));     // encre de M_Contours
            mat.SetFloat("_Thickness", 1.5f);
            EditorUtility.SetDirty(mat);
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform view = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Mains (vue)");
                var h = Comp<HandsOutline>(root);
                SetRef(h, "viewRoot", view);
                SetRef(h, "outline", mat);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            return "contour des mains (M_ContourMains, 1.5 px) sur le prefab joueur";
        }

        /// <summary>
        /// Signaux de survie (bloc 4) : matériaux additifs qui traversent le brouillard (colonne de la zone de prise, halo des âmes),
        /// branchés dans les réglages ; lueur « à portée » des objets attrapables 0.35 → 1.0 (RENDU.md § 3 ter).
        /// </summary>
        public static string Signals()
        {
            EnsureFolder("Assets/_Project/Art/Rendu");
            Material Signal(string name, Color c)
            {
                string path = $"Assets/_Project/Art/Rendu/{name}.mat";
                var m = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (m == null)
                {
                    m = new Material(Shader.Find("ItHunts/SignalAdditif")) { name = name };
                    AssetDatabase.CreateAsset(m, path);
                }

                m.SetColor("_BaseColor", c);
                m.enableInstancing = true;
                EditorUtility.SetDirty(m);
                return m;
            }

            Material column = Signal("M_Signal_Colonne", new Color(1.6f, 0.08f, 0.04f));
            Material halo = Signal("M_Signal_Halo", new Color(0.14f, 0.9f, 1.1f));
            var stalker = AssetDatabase.LoadAssetAtPath<StalkerSettings>(SettingsDir + "/Stalker_Default.asset");
            stalker.grabColumnMaterial = column;
            EditorUtility.SetDirty(stalker);
            var eco = AssetDatabase.LoadAssetAtPath<EconomySettings>(SettingsDir + "/Economy_Default.asset");
            eco.soulHaloMaterial = halo;
            EditorUtility.SetDirty(eco);
            // Corps de la goule : émission active (noire au repos) pour sa lueur pendant l'annonce du bond.
            var ghoulMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Greybox/Greybox_Ghoul.mat");
            if (ghoulMat != null && !ghoulMat.IsKeywordEnabled("_EMISSION"))
            {
                ghoulMat.EnableKeyword("_EMISSION");
                ghoulMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                if (ghoulMat.GetColor("_EmissionColor").maxColorComponent <= 0f)
                    ghoulMat.SetColor("_EmissionColor", Color.black);
                EditorUtility.SetDirty(ghoulMat);
            }

            var tk = AssetDatabase.LoadAssetAtPath<TelekinesisFeedbackSettings>(SettingsDir + "/TelekinesisFeedback_Default.asset");
            float before = tk.glowInRange;
            tk.glowInRange = 1f;
            EditorUtility.SetDirty(tk);
            AssetDatabase.SaveAssets();
            return $"signaux : colonne (M_Signal_Colonne), halo des âmes (M_Signal_Halo), lueur à portée {before} → {tk.glowInRange}";
        }

        public static AmbianceSettings Intermediate()
        {
            var a = AssetDatabase.LoadAssetAtPath<AmbianceSettings>(IntermediatePath);
            if (a != null)
                return a;
            a = ScriptableObject.CreateInstance<AmbianceSettings>();
            AssetDatabase.CreateAsset(a, IntermediatePath);
            return a;
        }

        public static AmbianceSettings Horror()
        {
            var a = AssetDatabase.LoadAssetAtPath<AmbianceSettings>(HorrorPath);
            if (a != null)
                return a;
            a = ScriptableObject.CreateInstance<AmbianceSettings>();
            a.ambient = new Color(0.012f, 0.013f, 0.020f);
            a.fogColor = new Color(0.004f, 0.005f, 0.009f);
            a.fogStart = 6f;
            a.fogEnd = 55f;
            a.moonIntensity = 0.12f;
            a.torchIntensity = 1.4f;
            a.torchRange = 9f;
            a.lampIntensity = 1.4f;
            a.lampRange = 10f;
            a.contourFadeStart = 30f;
            a.contourFadeEnd = 70f;
            AssetDatabase.CreateAsset(a, HorrorPath);
            return a;
        }

        /// <summary>Ambiance intermédiaire dans toutes les scènes de jeu ; torches réglées par l'ambiance.</summary>
        public static string Ambiance()
        {
            AmbianceSettings mid = Intermediate();
            Horror();
            // Torche : sa lumière suit l'ambiance (AmbianceLight).
            string torch = SettingsDir.Replace("Settings", "Prefabs") + "/Props/Prop_Torche.prefab";
            GameObject t = PrefabUtility.LoadPrefabContents(torch);
            try
            {
                foreach (Light l in t.GetComponentsInChildren<Light>(true))
                    if (l.type == LightType.Point && l.GetComponent<AmbianceLight>() == null)
                        l.gameObject.AddComponent<AmbianceLight>().kind = AmbianceLight.Kind.Torche;
                PrefabUtility.SaveAsPrefabAsset(t, torch);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(t);
            }

            string current = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return "annulé";
            int n = 0;
            foreach (string path in GameScenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                AddTo(AssetDatabase.LoadAssetAtPath<AmbianceSettings>(IntermediatePath));
                EditorSceneManager.SaveScene(scene);
                n++;
            }

            if (!string.IsNullOrEmpty(current))
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            AssetDatabase.SaveAssets();
            return $"ambiance intermédiaire dans {n} scènes (Ambiance_Horreur prête, non activée)";
        }

        /// <summary>Ambiance de la scène ouverte (constructeurs de scène) : objet SceneAmbiance et réglages de la scène.</summary>
        public static void AddTo(AmbianceSettings a)
        {
            if (a == null)
                a = Intermediate();
            var amb = Object.FindFirstObjectByType<SceneAmbiance>(FindObjectsInactive.Include);
            if (amb == null)
                amb = new GameObject("Ambiance de rendu (D-067)").AddComponent<SceneAmbiance>();
            SetRef(amb, "ambiance", a);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = a.ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = a.fogColor;
            RenderSettings.fogStartDistance = a.fogStart;
            RenderSettings.fogEndDistance = a.fogEnd;
        }
    }
}
