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
        /// <summary>Composant existant ou ajouté (pas de « ?? » : GetComponent renvoie un faux null dans l'éditeur).</summary>
        private static T Comp<T>(Component c) where T : Component => Comp<T>(c.gameObject);

        private static T Comp<T>(GameObject go) where T : Component
        {
            T t = go.GetComponent<T>();
            return t != null ? t : go.AddComponent<T>();
        }

        public const string LanternPath = SettingsDir + "/FireLantern_Default.asset";
        public const string CharactersDir = SettingsDir + "/Personnages";
        public const string RosterPath = CharactersDir + "/Characters_Default.asset";
        private const string BarrelPath = SettingsDir + "/Barrel_Default.asset";
        private const string PlayerPrefab = "Assets/_Project/Prefabs/Player.prefab";
        public const string LanternRootName = "Lanterne de feu (vue)";
        public const string LanternModelPath = "Assets/Art/Models/FPS/SM_LanterneFeu.fbx";
        public const string PlayerLanternPath = SettingsDir + "/PlayerLantern_Default.asset";
        /// <summary>Le modèle pend sous son pivot (anneau de portage) : on le remonte pour que le corps soit au centre de la racine.</summary>
        public const float LanternModelLift = 0.16f;

        [MenuItem("ItHunts/Personnages : créer les assets, poser la lanterne, brancher les scènes")]
        public static void Menu() => Debug.Log(SetupAll());

        public static string SetupAll()
        {
            CharacterRoster roster = Roster();
            var lanternColors = new System.Collections.Generic.Dictionary<string, Color>
            {
                { "renard", new Color(0.62f, 0.16f, 0.07f) }, { "cameleon", new Color(0.66f, 0.34f, 0.08f) },
                { "koala", new Color(0.16f, 0.42f, 0.14f) }, { "hibou", new Color(0.12f, 0.24f, 0.60f) },
            };
            foreach (CharacterDefinition c in roster.characters)
                if (c != null && lanternColors.TryGetValue(c.id, out Color lc) && c.lanternColor != lc)
                {
                    c.lanternColor = lc;
                    EditorUtility.SetDirty(c);
                }
            RunDefinition run = AssetDatabase.LoadAssetAtPath<RunDefinition>(MainMenuSceneBuilder.RunPath);
            if (run != null && run.characters != roster)
            {
                run.characters = roster;
                EditorUtility.SetDirty(run);
            }

            AssetDatabase.SaveAssets();
            string list = $"Personnages : {roster.Count} ({string.Join(", ", roster.characters.Select(c => c.displayName + (c.Selectable ? "" : " (grisé)")))})";
            string pools = UpgradePools();
            string prefab = InstallLantern();
            string scenes = WireScenes();                                // ouvre les scènes : les assets chargés avant sont déchargés
            return $"{list} ; {pools} ; {prefab} ; scènes : {scenes}";
        }

        public const string LanternPoolPath = SettingsDir + "/UpgradePool_Lanterne.asset";

        /// <summary>
        /// Améliorations vendues par le marchand de l'artefact, une liste par artefact (point 6) : celles du barillet (D-050) et 4
        /// provisoires pour la lanterne de feu. Les assets déjà présents gardent leurs réglages.
        /// </summary>
        public static string UpgradePools()
        {
            UpgradePool barrelPool = TestArenaSceneBuilder.BarrelPoolAsset();
            var barrel = AssetDatabase.LoadAssetAtPath<BarrelSettings>(BarrelPath);
            if (barrel != null && barrel.upgrades != barrelPool)
            {
                barrel.upgrades = barrelPool;
                EditorUtility.SetDirty(barrel);
            }

            var pool = LoadOrCreate<UpgradePool>(LanternPoolPath);
            pool.upgrades.Clear();
            pool.upgrades.Add(Upgrade("Upgrade_Lanterne_Portee", "Long souffle", "Jet de la lanterne : +1,5 m de portée.", "+1,5", new Color(3.2f, 1.2f, 0.3f), UpgradeKind.PorteeLanterne, 1.5f, 45));
            pool.upgrades.Add(Upgrade("Upgrade_Lanterne_Cone", "Flamme large", "Cône du jet : +15°.", "<>", new Color(3.4f, 1.6f, 0.4f), UpgradeKind.ConeLanterne, 15f, 50));
            pool.upgrades.Add(Upgrade("Upgrade_Lanterne_Recharge", "Mèche vive", "Recharge de la lanterne : -0,5 s.", "-0,5", new Color(3f, 2f, 0.5f), UpgradeKind.RechargeLanterne, 0.5f, 55));
            pool.upgrades.Add(Upgrade("Upgrade_Lanterne_Brulure", "Braises", "Brûlure de la lanterne : +2 s.", "+2 s", new Color(3.6f, 0.7f, 0.2f), UpgradeKind.DureeBrulureLanterne, 2f, 60));
            EditorUtility.SetDirty(pool);
            FireLanternSettings lantern = Lantern();
            if (lantern.upgrades != pool)
            {
                lantern.upgrades = pool;
                EditorUtility.SetDirty(lantern);
            }

            AssetDatabase.SaveAssets();
            return $"améliorations : barillet {barrelPool.upgrades.Count}, lanterne {pool.upgrades.Count}";
        }

        private static UpgradeDefinition Upgrade(string file, string name, string description, string icon, Color color, UpgradeKind kind, float amount, int price)
        {
            string path = $"{SettingsDir}/Upgrades/{file}.asset";
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

        /// <summary>La lanterne de feu (créée avec les valeurs de départ si elle n'existe pas).</summary>
        public static FireLanternSettings Lantern()
        {
            var l = AssetDatabase.LoadAssetAtPath<FireLanternSettings>(LanternPath);
            if (l != null)
            {
                if (l.burnEffect == null)
                {
                    l.burnEffect = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Greybox/Lantern_Puff.mat");   // flammes de la brûlure
                    EditorUtility.SetDirty(l);
                }

                return l;
            }
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
                InstallPlayerLight(root);
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
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(LanternModelPath);
            if (model != null)
            {
                BuildLanternModel(root, model, player, leftHand.gameObject.layer);
                return;
            }

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

        /// <summary>
        /// Modèle SM_LanterneFeu (lot de la DA) à la place de la greybox : flamme (petite sphère émissive) sur FX_Flamme, sortie du
        /// jet sur FX_Jet, verre (SM_LanterneFeu_Verre) allumé par le code. Le FBX n'est pas modifié.
        /// </summary>
        private static void BuildLanternModel(Transform root, GameObject model, GameObject player, int layer)
        {
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root);
            inst.transform.localPosition = new Vector3(0f, LanternModelLift, 0f);
            inst.transform.localRotation = Quaternion.identity;
            Transform[] all = inst.GetComponentsInChildren<Transform>(true);
            Transform fxFlame = all.FirstOrDefault(t => t.name == "FX_Flamme");
            Transform fxJet = all.FirstOrDefault(t => t.name == "FX_Jet");
            Renderer glass = all.Select(t => t.GetComponent<Renderer>()).FirstOrDefault(r => r != null && r.name.Contains("Verre"));
            Material flameMat = UnlitMat("Lantern_Flame", "Universal Render Pipeline/Unlit", new Color(1f, 0.6f, 0.2f));
            GameObject flameGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flameGo.name = "Flamme";
            Object.DestroyImmediate(flameGo.GetComponent<Collider>());
            flameGo.transform.SetParent(fxFlame != null ? fxFlame : root, false);
            flameGo.transform.localPosition = new Vector3(0f, 0.012f, 0f);
            flameGo.transform.localScale = new Vector3(0.018f, 0.028f, 0.018f);
            var flame = flameGo.GetComponent<MeshRenderer>();
            flame.sharedMaterial = flameMat;
            flame.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var view = Comp<FireLanternView>(player);
            SetRef(view, "lanternRoot", root);
            SetRef(view, "flame", flame);
            SetRef(view, "muzzle", fxJet != null ? fxJet : root);
            SetRef(view, "glass", glass);
            GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Mesh sphere = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(tmp);
            SetRef(view, "puffMesh", sphere);
            SetRef(view, "puffMaterial", PuffMaterial());
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = layer;
        }

        /// <summary>Lumière « lanterne » du joueur (D-067) : enfant du prefab, réglages PlayerLantern_Default, couleur du personnage.</summary>
        public static void InstallPlayerLight(GameObject player)
        {
            var settings = AssetDatabase.LoadAssetAtPath<PlayerLanternSettings>(PlayerLanternPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PlayerLanternSettings>();
                AssetDatabase.CreateAsset(settings, PlayerLanternPath);
            }

            Transform old = player.transform.Find("Lanterne (lumière du joueur)");
            if (old == null)
            {
                old = new GameObject("Lanterne (lumière du joueur)").transform;
                old.SetParent(player.transform, false);
            }

            old.localPosition = settings.position;
            var l = Comp<Light>(old);
            l.type = LightType.Point;
            l.shadows = LightShadows.None;
            l.intensity = settings.intensity;
            l.range = settings.range;
            l.color = settings.fallbackColor;
            var comp = Comp<ItHunts.Player.PlayerLanternLight>(player);
            SetRef(comp, "settings", settings);
            SetRef(comp, "lanternLight", l);
            SetRef(comp, "viewRoot", player.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Mains (vue)"));
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
