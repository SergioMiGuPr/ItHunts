using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Settings;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Modèle du barillet (SM_Barillet, livré par la DA, PR #2) branché sur l'artefact barillet de la vue FPS : remplace le
    /// visuel greybox (corps et chambres) sous « Barillet (vue) » dans la main gauche du prefab Player. Le prefab est modifié
    /// en place (les scènes gardent leurs instances et leurs réglages). Le FBX n'est pas touché (Assets/Art appartient à la DA) :
    /// le laiton est un matériau du projet (texture de couleur de base seulement) posé sur l'instance.
    /// Le canon et la bouche restent greybox, alignés sur la chambre du haut.
    /// </summary>
    public static class BarrelModelInstaller
    {
        public const string ModelPath = "Assets/Art/Models/FPS/SM_Barillet.fbx";
        public const string LaitonTexturePath = "Assets/Art/Textures/FPS/T_Barillet_Laiton_BaseColor.png";
        public const string MaterialDir = "Assets/_Project/Art/Barillet";
        public const string LaitonMaterialPath = MaterialDir + "/M_Barillet_Laiton.mat";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";

        /// <summary>Hauteur de la chambre du haut au-dessus de l'axe (m, taille modélisée) : axe du canon.</summary>
        public const float TopChamberHeight = 0.0307f;
        /// <summary>Longueur du barillet modélisé (m).</summary>
        public const float ModelLength = 0.111f;

        [MenuItem("ItHunts/Barillet : installer le modèle SM_Barillet dans le prefab Player")]
        public static void Menu() => Debug.Log(Install());

        /// <summary>Installe le modèle dans le prefab Player. Renvoie un résumé.</summary>
        public static string Install()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath) == null)
                return $"ERREUR : {ModelPath} introuvable.";
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform barrel = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Barillet (vue)");
                if (barrel == null)
                    return "ERREUR : « Barillet (vue) » absent du prefab Player.";
                var view = root.GetComponent<BarrelView>();
                Parts parts = Build(barrel);
                var so = new SerializedObject(view);
                so.FindProperty("barrelRoot").objectReferenceValue = barrel;
                so.FindProperty("cylinder").objectReferenceValue = parts.Spin;
                so.FindProperty("muzzle").objectReferenceValue = parts.Muzzle;
                SerializedProperty list = so.FindProperty("chambers");
                list.arraySize = parts.Chambers.Length;
                for (int i = 0; i < parts.Chambers.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = parts.Chambers[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return $"OK : SM_Barillet installé dans {PlayerPrefabPath} ({parts.Chambers.Length} chambres, laiton {LaitonMaterialPath}).";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        public struct Parts
        {
            public Transform Spin;
            public Transform Muzzle;
            public Renderer[] Chambers;
        }

        /// <summary>
        /// Construit le barillet sous barrelRoot (déjà placé dans la main gauche) : retire le greybox, instancie SM_Barillet
        /// (pivot au centre, axe de tir +Z), canon greybox sur la chambre du haut, bouche au bout. Sert aussi au constructeur.
        /// </summary>
        public static Parts Build(Transform barrelRoot)
        {
            for (int i = barrelRoot.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(barrelRoot.GetChild(i).gameObject);
            var fps = AssetDatabase.LoadAssetAtPath<FpsViewSettings>("Assets/_Project/Settings/FpsView_Default.asset");
            if (fps != null)
            {
                barrelRoot.localPosition = fps.barrelInHand;
                barrelRoot.localRotation = Quaternion.Euler(fps.barrelInHandEuler);
                barrelRoot.localScale = Vector3.one * fps.barrelScale;
            }

            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), barrelRoot);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;
            var body = model.GetComponent<MeshRenderer>();
            Material[] mats = body.sharedMaterials;
            mats[0] = LaitonMaterial();                                    // sous-maillage 0 : M_Barillet_Laiton
            body.sharedMaterials = mats;
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            {
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }

            var chambers = new Renderer[BarrelModel.Chambers];
            for (int i = 0; i < chambers.Length; i++)
            {
                Transform c = model.transform.Find($"SM_Barillet_Chambre_{i + 1:00}");
                chambers[i] = c != null ? c.GetComponent<Renderer>() : null;
            }

            // Canon greybox (provisoire, pas encore modélisé) : dans l'axe de la chambre du haut, devant le barillet.
            Material metal = TestMovementSceneBuilder.Mat("Greybox_Barrel", new Color(0.18f, 0.17f, 0.2f));
            float front = ModelLength * 0.5f;
            TestMovementSceneBuilder.Part(PrimitiveType.Cylinder, "Canon", barrelRoot, new Vector3(0f, TopChamberHeight, front + 0.05f),
                new Vector3(0.022f, 0.05f, 0.022f), new Vector3(90f, 0f, 0f), metal);
            var muzzle = new GameObject("Bouche du canon").transform;
            muzzle.SetParent(barrelRoot, false);
            muzzle.localPosition = new Vector3(0f, TopChamberHeight, front + 0.101f);
            return new Parts { Spin = model.transform, Muzzle = muzzle, Chambers = chambers };
        }

        /// <summary>M_Barillet_Laiton : texture de couleur de base seulement, ni métal ni brillance (le reste avec la DA).</summary>
        public static Material LaitonMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(LaitonMaterialPath);
            if (m != null)
                return m;
            if (!AssetDatabase.IsValidFolder(MaterialDir))
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Barillet");
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "M_Barillet_Laiton" };
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(LaitonTexturePath));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_Smoothness", 0f);
            AssetDatabase.CreateAsset(m, LaitonMaterialPath);
            return m;
        }
    }
}
