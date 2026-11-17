using System.Linq;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Orbe v2 (SM_Orbe, DA) dans le prefab Player : instance du modèle sous « Orbe (vue) » (copie dépaquetée : les tablettes
    /// sont rangées sous une « Couronne » qui tourne pendant la traction), réglages OrbModel_Default, composant OrbModelView.
    /// La sphère greybox reste (MagnetismOrbView y lit sa taille) mais elle est masquée en jeu. Le FBX n'est pas touché
    /// (Assets/Art appartient à la DA). Relancer le menu est sans risque : l'ancienne instance est remplacée.
    /// </summary>
    public static class OrbModelInstaller
    {
        public const string ModelPath = "Assets/Art/Models/FPS/SM_Orbe.fbx";
        public const string SettingsPath = "Assets/_Project/Settings/OrbModel_Default.asset";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
        private const string InstanceName = "SM_Orbe (vue)";

        [MenuItem("ItHunts/Orbe : installer le modèle SM_Orbe dans le prefab Player")]
        public static void Menu() => Debug.Log(Install());

        public static string Install()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (fbx == null)
                return $"ERREUR : {ModelPath} introuvable.";
            var settings = AssetDatabase.LoadAssetAtPath<OrbModelSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<OrbModelSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform orb = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Orbe (vue)");
                if (orb == null)
                    return "ERREUR : « Orbe (vue) » absent du prefab Player.";
                Transform old = orb.Find(InstanceName);
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                GameObject model = Object.Instantiate(fbx, orb, false);
                model.name = InstanceName;
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                var crown = new GameObject("Couronne").transform;
                crown.SetParent(model.transform, false);
                var tablets = new Renderer[6];
                for (int i = 0; i < 6; i++)
                {
                    Transform t = model.transform.Find($"SM_Orbe_Tablette_0{i + 1}");
                    if (t == null)
                        continue;
                    t.SetParent(crown, false);
                    tablets[i] = t.GetComponent<Renderer>();
                }

                foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    r.receiveShadows = false;
                }

                if (!root.TryGetComponent(out OrbModelView view))
                    view = root.AddComponent<OrbModelView>();
                var so = new SerializedObject(view);
                so.FindProperty("settings").objectReferenceValue = settings;
                so.FindProperty("model").objectReferenceValue = model.transform;
                so.FindProperty("greybox").objectReferenceValue = orb.GetComponent<Renderer>();
                so.FindProperty("ring1").objectReferenceValue = model.transform.Find("SM_Orbe_Anneau_1");
                so.FindProperty("ring2").objectReferenceValue = model.transform.Find("SM_Orbe_Anneau_2");
                so.FindProperty("ring3").objectReferenceValue = model.transform.Find("SM_Orbe_Anneau_3");
                Transform energy = model.transform.Find("SM_Orbe_Energie");
                so.FindProperty("energy").objectReferenceValue = energy;
                so.FindProperty("energyRenderer").objectReferenceValue = energy != null ? energy.GetComponent<Renderer>() : null;
                so.FindProperty("crown").objectReferenceValue = crown;
                SerializedProperty list = so.FindProperty("tablets");
                list.arraySize = 6;
                for (int i = 0; i < 6; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = tablets[i];
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return $"OK : SM_Orbe installé sous « Orbe (vue) » ({tablets.Count(t => t != null)} tablettes), réglages {SettingsPath}.";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
