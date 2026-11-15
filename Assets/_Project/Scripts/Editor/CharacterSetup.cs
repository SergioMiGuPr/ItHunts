using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Personnages et lanterne de feu (D-064, D-065) : crée les assets (FireLantern_Default, un personnage par animal, la liste
    /// Characters_Default), branche la liste dans Run_Default (menu) et dans l'état de la run de chaque scène, et pose la lanterne
    /// greybox dans la main gauche du prefab Player (modifié en place, comme pour le barillet). Les constructeurs de scène
    /// l'appellent aussi. Les réglages déjà présents dans les assets sont gardés.
    /// </summary>
    public static class CharacterSetup
    {
        public const string LanternPath = SettingsDir + "/FireLantern_Default.asset";
        public const string CharactersDir = SettingsDir + "/Personnages";
        public const string RosterPath = CharactersDir + "/Characters_Default.asset";
        private const string BarrelPath = SettingsDir + "/Barrel_Default.asset";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Player.prefab";
        public const string LanternRootName = "Lanterne de feu (vue)";

        [MenuItem("ItHunts/Personnages : créer les assets, poser la lanterne, brancher les scènes")]
        public static void Menu() => Debug.Log(SetupAll());

        public static string SetupAll()
        {
            CharacterRoster roster = Roster();
            RunDefinition run = AssetDatabase.LoadAssetAtPath<RunDefinition>(MainMenuSceneBuilder.RunPath);
            if (run != null && run.characters != roster)
            {
                run.characters = roster;
                EditorUtility.SetDirty(run);
            }

            AssetDatabase.SaveAssets();
            string list = $"Personnages : {roster.Count} ({string.Join(", ", roster.characters.Select(c => c.displayName + (c.Selectable ? "" : " (grisé)")))})";
            string prefab = InstallLantern();
            string scenes = WireScenes();                                // ouvre les scènes : les assets chargés avant sont déchargés
            return $"{list} ; {prefab} ; scènes : {scenes}";
        }

        /// <summary>La lanterne de feu (créée avec les valeurs de départ si elle n'existe pas).</summary>
        public static FireLanternSettings Lantern()
        {
            var l = AssetDatabase.LoadAssetAtPath<FireLanternSettings>(LanternPath);
            if (l != null)
                return l;
            l = ScriptableObject.CreateInstance<FireLanternSettings>();
            l.id = "lanterne-de-feu";
            l.displayName = "Lanterne de feu";
            AssetDatabase.CreateAsset(l, LanternPath);
            return l;
        }

        /// <summary>La liste des personnages (créée si elle n'existe pas) : renard, caméléon, koala, hibou ; défaut : caméléon.</summary>
        public static CharacterRoster Roster()
        {
            EnsureFolder(CharactersDir);
            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath);
            if (roster != null && roster.Count > 0)
                return roster;
            FireLanternSettings lantern = Lantern();
            var barrel = AssetDatabase.LoadAssetAtPath<BarrelSettings>(BarrelPath);
            if (roster == null)
            {
                roster = ScriptableObject.CreateInstance<CharacterRoster>();
                AssetDatabase.CreateAsset(roster, RosterPath);
            }

            roster.characters.Clear();
            roster.characters.Add(Character("Renard", "renard", new Color(0.9f, 0.22f, 0.16f), lantern, true, ""));
            roster.characters.Add(Character("Caméléon", "cameleon", new Color(1f, 0.55f, 0.12f), barrel, true, ""));
            roster.characters.Add(Character("Koala", "koala", new Color(0.35f, 0.78f, 0.35f), null, false, "artefact à concevoir"));
            roster.characters.Add(Character("Hibou", "hibou", new Color(0.25f, 0.5f, 1f), null, false, "artefact à concevoir"));
            roster.defaultIndex = 1;
            EditorUtility.SetDirty(roster);
            AssetDatabase.SaveAssets();
            return roster;
        }

        private static CharacterDefinition Character(string name, string id, Color color, ArtifactDefinition artifact, bool playable, string label)
        {
            string path = $"{CharactersDir}/Character_{id}.asset";
            var c = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
            if (c != null)
                return c;
            c = ScriptableObject.CreateInstance<CharacterDefinition>();
            c.id = id;
            c.displayName = name;
            c.color = color;
            c.artifact = artifact;
            c.playable = playable;
            if (!string.IsNullOrEmpty(label))
                c.artifactLabel = label;
            AssetDatabase.CreateAsset(c, path);
            return c;
        }

        /// <summary>Pose la lanterne dans le prefab Player (remplace l'ancienne). Renvoie un résumé.</summary>
        public static string InstallLantern()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefab);
            try
            {
                Transform leftHand = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Main gauche");
                if (leftHand == null)
                    return "ERREUR : « Main gauche » absente du prefab Player.";
                BuildLantern(leftHand, root);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefab);
                return $"lanterne posée dans {PlayerPrefab}";
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>
        /// Lanterne greybox dans la paume gauche (cage de 6 × 9 × 6 cm : socle, chapeau, 4 montants, anse ; flamme au centre) et
        /// sa vue (FireLanternView) sur le joueur. Cachée par la vue si le personnage n'a pas la lanterne.
        /// </summary>
        public static void BuildLantern(Transform leftHand, GameObject player)
        {
            Transform old = leftHand.Find(LanternRootName);
            if (old != null)
                Object.DestroyImmediate(old.gameObject);
            var root = new GameObject(LanternRootName).transform;
            root.SetParent(leftHand, false);
            root.localPosition = new Vector3(0f, 0.075f, 0.05f);
            Material metal = Mat("Lantern_Metal", new Color(0.16f, 0.13f, 0.11f));
            Material brass = Mat("Lantern_Brass", new Color(0.55f, 0.38f, 0.16f));
            Part(PrimitiveType.Cube, "Socle", root, new Vector3(0f, -0.045f, 0f), new Vector3(0.07f, 0.012f, 0.07f), Vector3.zero, metal);
            Part(PrimitiveType.Cube, "Chapeau", root, new Vector3(0f, 0.045f, 0f), new Vector3(0.07f, 0.012f, 0.07f), Vector3.zero, metal);
            Part(PrimitiveType.Cube, "Toit", root, new Vector3(0f, 0.056f, 0f), new Vector3(0.04f, 0.012f, 0.04f), Vector3.zero, brass);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0 ? -1f : 1f) * 0.03f, z = (i < 2 ? -1f : 1f) * 0.03f;
                Part(PrimitiveType.Cube, "Montant", root, new Vector3(x, 0f, z), new Vector3(0.007f, 0.08f, 0.007f), Vector3.zero, brass);
            }

            Part(PrimitiveType.Cylinder, "Anse", root, new Vector3(0f, 0.068f, 0f), new Vector3(0.006f, 0.022f, 0.006f), new Vector3(0f, 0f, 90f), brass);
            Material flameMat = UnlitMat("Lantern_Flame", "Universal Render Pipeline/Unlit", new Color(1f, 0.6f, 0.2f));
            Part(PrimitiveType.Sphere, "Flamme", root, new Vector3(0f, -0.005f, 0f), new Vector3(0.032f, 0.045f, 0.032f), Vector3.zero, flameMat);
            Renderer flame = root.Find("Flamme").GetComponent<Renderer>();
            var muzzle = new GameObject("Sortie du jet").transform;
            muzzle.SetParent(root, false);
            muzzle.localPosition = new Vector3(0f, 0.03f, 0.05f);

            var view = player.GetComponent<FireLanternView>();
            if (view == null)
                view = player.AddComponent<FireLanternView>();
            SetRef(view, "lanternRoot", root);
            SetRef(view, "flame", flame);
            SetRef(view, "muzzle", muzzle);
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphere = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            SetRef(view, "puffMesh", sphere);
            SetRef(view, "puffMaterial", PuffMaterial());
            int layer = leftHand.gameObject.layer;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        /// <summary>Matériau des bouffées : Particles/Unlit transparent additif (la couleur HDR vient du code).</summary>
        private static Material PuffMaterial()
        {
            Material m = UnlitMat("Lantern_Puff", "Universal Render Pipeline/Particles/Unlit", Color.white);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);                                    // additif
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            m.SetOverrideTag("RenderType", "Transparent");
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Branche la liste des personnages dans l'état de la run de chaque scène du projet qui en a un.</summary>
        public static string WireScenes()
        {
            Roster();
            string current = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return "annulé";
            var done = new System.Collections.Generic.List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                // Rechargée après chaque ouverture : OpenScene décharge les assets, une ancienne référence serait un faux null.
                var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>(RosterPath);
                bool changed = false;
                foreach (RunState rs in Object.FindObjectsByType<RunState>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    var so = new SerializedObject(rs);
                    SerializedProperty p = so.FindProperty("characters");
                    if (p.objectReferenceValue != null && p.objectReferenceValue == roster)
                        continue;
                    p.objectReferenceValue = roster;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }

                if (changed)
                {
                    EditorSceneManager.SaveScene(scene);
                    done.Add(System.IO.Path.GetFileNameWithoutExtension(path));
                }
            }

            if (!string.IsNullOrEmpty(current))
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            return done.Count == 0 ? "déjà branchées" : string.Join(", ", done);
        }
    }
}
