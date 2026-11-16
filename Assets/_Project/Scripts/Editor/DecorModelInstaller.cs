using System.Collections.Generic;
using System.Linq;
using ItHunts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Décor du niveau (bloc 3 de rendu-integration) : les modèles livrés par la DA (lot de modélisation, groupes B et C) remplacent
    /// le visuel greybox des props et des pièces de l'arène du boss. Les colliders du jeu sont gardés (réglés pour le gameplay) ;
    /// les FBX et leurs matériaux toon ne sont pas modifiés. Pièces mobiles : grille de la herse (tombe avec le piège), manche des
    /// leviers (±35°, lueur), manivelle du treuil (tourne pendant la chute du lustre), plateformes qui tombent. L'échafaudage garde
    /// deux plateaux et six poteaux (variante praticable, fiche § 2). Idempotent : un modèle déjà posé est remplacé.
    /// </summary>
    public static class DecorModelInstaller
    {
        private const string Models = "Assets/Art/Models/Niveau1/";
        private const string PropsDir = "Assets/_Project/Prefabs/Props/";
        public const string ModelTag = "Modèle (DA) : ";
        public const float TorchLift = 0.545f;

        private static readonly (string prop, string model)[] PropMap =
        {
            ("Torche", "SM_Torche"), ("TorcheEteinte", "SM_Torche"), ("Cage", "SM_Cage"), ("Grille", "SM_Grille"),
            ("Tapisserie", "SM_Tapisserie"), ("Chaine", "SM_Chaine"), ("Tuyau", "SM_Tuyau"), ("Echafaudage", "SM_Echafaudage"),
            ("ColonneBrisee", "SM_ColonneBrisee"), ("Colonne", "SM_Colonne"),
        };

        [MenuItem("ItHunts/Décor : poser les modèles livrés (props, arène du boss)")]
        public static void Menu() => Debug.Log(InstallAll());

        public static string InstallAll() => InstallProps() + " ; " + InstallArenas();

        /// <summary>Props : visuel greybox (maillage de la racine) remplacé par le modèle, aligné sur la même emprise.</summary>
        public static string InstallProps()
        {
            var report = new List<string>();
            foreach ((string prop, string modelName) in PropMap)
            {
                string path = $"{PropsDir}Prop_{prop}.prefab";
                var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}{modelName}.fbx");
                if (model == null || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                {
                    report.Add($"{prop} : absent");
                    continue;
                }

                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    report.Add(InstallProp(root, model, prop));
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }

                // Étiquette « DA » : PropLibrary.BuildAll garde ce prop au lieu de le refaire en greybox.
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                string[] labels = AssetDatabase.GetLabels(saved);
                if (System.Array.IndexOf(labels, "DA") < 0)
                    AssetDatabase.SetLabels(saved, labels.Concat(new[] { "DA" }).ToArray());
            }

            AssetDatabase.SaveAssets();
            return "props : " + string.Join(", ", report);
        }

        /// <summary>
        /// Décalage du modèle dans le prefab, selon docs/art/INTEGRATION-PROPS.md § 2 (fait foi, lot du 2026-11-16) : 0 sauf la torche
        /// (pivot au dos de la plaque, tête à 0.96 m comme la greybox), la tapisserie et la chaîne (pivot en haut).
        /// </summary>
        public static Vector3 SheetOffset(string prop) => prop switch
        {
            "Torche" or "TorcheEteinte" => new Vector3(0f, TorchLift, 0f),
            "Tapisserie" => new Vector3(0f, 4f, 0f),
            "Chaine" => new Vector3(0f, 3.95f, 0f),
            _ => Vector3.zero,
        };

        private static string InstallProp(GameObject root, GameObject model, string prop)
        {
            foreach (Transform old in root.transform.Cast<Transform>().Where(t => t.name.StartsWith(ModelTag)).ToList())
                Object.DestroyImmediate(old.gameObject);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            inst.name = ModelTag + model.name;
            inst.transform.localPosition = SheetOffset(prop);                 // rotation nulle, échelle 1 (fiche, règles communes)
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            if (root.GetComponent<MeshRenderer>() != null)
                Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            var mf = root.GetComponent<MeshFilter>();
            if (mf != null)
                Object.DestroyImmediate(mf);

            // Torches : flamme sur FX_Flamme ; la lumière exactement sur FX_Flamme (fiche : (0, 0.988, 0.354) dans le prefab).
            Transform fx = inst.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "FX_Flamme");
            string note = "";
            if (fx != null)
            {
                foreach (Transform f in fx.Cast<Transform>().Where(t => t.name == "Flamme").ToList())
                    Object.DestroyImmediate(f.gameObject);
                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                flame.name = "Flamme";
                Object.DestroyImmediate(flame.GetComponent<Collider>());
                flame.transform.SetParent(fx, false);
                flame.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                flame.transform.localScale = new Vector3(0.12f, 0.2f, 0.12f);
                var r = flame.GetComponent<MeshRenderer>();
                r.sharedMaterial = UnlitMat("Torch_Flame", "Universal Render Pipeline/Unlit", new Color(2.4f, 1.1f, 0.3f));
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                foreach (Light l in root.GetComponentsInChildren<Light>(true))
                    l.transform.position = fx.position;
                note = $" ; FX_Flamme en {root.transform.InverseTransformPoint(fx.position).ToString("F3")}";
            }

            // Collisions de la fiche.
            if (prop == "Tuyau")
                SetRootBox(root, new Vector3(0f, 0.457f, 0.04f), new Vector3(0.91f, 0.91f, 6.08f));
            if (prop == "Echafaudage")
                ScaffoldColliders(root);

            Bounds after = LocalBounds(inst.transform, root.transform);
            return $"{prop} ({model.name}, décalage {inst.transform.localPosition.ToString("F3")}{note} ; modèle {after.size.x:0.##}×{after.size.y:0.##}×{after.size.z:0.##})";
        }

        private static void SetRootBox(GameObject root, Vector3 center, Vector3 size)
        {
            var box = root.GetComponent<BoxCollider>();
            if (box == null)
                box = root.AddComponent<BoxCollider>();
            Vector3 s = root.transform.localScale;
            box.center = new Vector3(center.x / s.x, center.y / s.y, center.z / s.z);
            box.size = new Vector3(size.x / s.x, size.y / s.y, size.z / s.z);
        }

        /// <summary>
        /// Échafaudage praticable (fiche § 2, variante retenue) : deux plateaux (dessus à 3 et 6 m) et six poteaux, intérieur libre ;
        /// plus de boîte pleine. Les plateaux sont des sols du NavMesh (les goules y grimpent par les flancs, comme les plateformes).
        /// </summary>
        public static void ScaffoldColliders(GameObject root)
        {
            foreach (Collider c in root.GetComponents<Collider>())
                Object.DestroyImmediate(c);
            foreach (Transform old in root.transform.Cast<Transform>().Where(t => t.name.StartsWith("Collision : ")).ToList())
                Object.DestroyImmediate(old.gameObject);
            void Box(string name, Vector3 center, Vector3 size, bool walkable)
            {
                var go = new GameObject("Collision : " + name);
                go.transform.SetParent(root.transform, false);
                go.transform.localPosition = center;
                go.AddComponent<BoxCollider>().size = size;
                if (!walkable)
                    go.AddComponent<ItHunts.Enemies.NotWalkable>();
            }

            // Plateaux : dessus à 3 et 6 m (fiche) ; 0.3 m d'épaisseur vers le bas au lieu de 0.07 (plus fin que les voxels du NavMesh :
            // selon sa hauteur, le plateau manquait à la cuisson). 2.7 m libres dessous.
            Box("plateau du bas", new Vector3(0f, 2.85f, -0.027f), new Vector3(4f, 0.3f, 2f), true);
            Box("plateau du haut", new Vector3(0f, 5.85f, -0.027f), new Vector3(4f, 0.3f, 2f), true);
            int n = 0;
            foreach (float x in new[] { -1.925f, 0f, 1.925f })
            foreach (float z in new[] { -0.952f, 0.898f })
                Box($"poteau {++n}", new Vector3(x, 3f, z), new Vector3(0.15f, 6f, 0.15f), false);
            Box("échelle", new Vector3(-1f, 3f, 1.013f), new Vector3(0.57f, 6f, 0.05f), false);
        }

        // ---------- Goule : SM_Goule (lot du 2026-11-16) ----------

        public const string GhoulPrefab = "Assets/_Project/Prefabs/Niveau/Goule.prefab";
        public const string GhoulModel = "Assets/Art/Models/Ennemis/SM_Goule.fbx";

        /// <summary>
        /// Goule (fiche § 4 ter) : SM_Goule en (0, 0, 0) sous « Corps (visuel) », rotation nulle ; la capsule, la tête et les yeux
        /// greybox sont retirés. Le code garde l'écrasement de l'annonce et la bascule de 80° sur les murs (sur « Corps (visuel) ») ;
        /// l'annonce du bond allume les yeux et la gueule par le masque d'émission de M_Goule.
        /// </summary>
        public static string InstallGhoul(GameObject root)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(GhoulModel);
            Transform visual = root.transform.Find("Corps (visuel)");
            if (model == null || visual == null)
                return "goule : modèle ou « Corps (visuel) » absent";
            foreach (Transform c in visual.Cast<Transform>().ToList())
                Object.DestroyImmediate(c.gameObject);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, visual);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            foreach (Collider c in inst.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Bounds b = LocalBounds(inst.transform, root.transform);
            return $"goule : SM_Goule {b.size.x:0.00} × {b.size.y:0.00} × {b.size.z:0.00} m, pied à {b.min.y:0.00} m";
        }

        [MenuItem("ItHunts/Décor : goule de la DA (SM_Goule)")]
        public static void GhoulMenu() => Debug.Log(InstallGhoulPrefab());

        public static string InstallGhoulPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(GhoulPrefab);
            try
            {
                string r = InstallGhoul(root);
                PrefabUtility.SaveAsPrefabAsset(root, GhoulPrefab);
                return r;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Le prefab de la goule et toutes les goules des scènes de jeu (réserves comprises : des copies, pas des instances).</summary>
        public static string InstallGhoulsEverywhere()
        {
            string r = InstallGhoulPrefab();
            string current = EditorSceneManager.GetActiveScene().path;
            int n = 0;
            foreach (string path in new[] { "Assets/Scenes/Test_Arena.unity", "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_01_Base.unity", "Assets/Scenes/Level_01_Aleatoire.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int here = 0;
                foreach (ItHunts.Enemies.Ghoul g in Object.FindObjectsByType<ItHunts.Enemies.Ghoul>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(g.gameObject))
                        continue;
                    InstallGhoul(g.gameObject);
                    here++;
                }

                if (here > 0)
                    EditorSceneManager.SaveScene(scene);
                n += here;
            }

            if (!string.IsNullOrEmpty(current))
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            return r + $" ; {n} goules dans les scènes";
        }

        // ---------- Caisses attrapables : SM_Caisse ----------

        public const string CrateModelPath = Models + "SM_Caisse.fbx";
        public const string CrateTag = "Modèle (DA) : SM_Caisse";
        /// <summary>Lueur des caisses sur tout le modèle (repli) au lieu de la rune seule (masque de la DA, face avant). Faux : la rune.</summary>
        public const bool CrateGlowWholeModel = false;

        /// <summary>
        /// Caisse attrapable (fiche § 4 bis) : le cube greybox (racine mise à l'échelle, pivot au centre) prend le modèle SM_Caisse
        /// (cube de 0.8 m, pivot au sol), à la taille du cube ; le collider et le Rigidbody restent. La lueur passe par le masque de la
        /// rune (Grabbable.SetGlow écrit _EmissionColor sur tous ses rendus).
        /// </summary>
        public static bool ApplyCrateModel(GameObject crate)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(CrateModelPath);
            var mf = crate.GetComponent<MeshFilter>();
            if (model == null || (mf != null && mf.sharedMesh != null && mf.sharedMesh.name != "Cube"))
                return false;
            foreach (Transform old in crate.transform.Cast<Transform>().Where(t => t.name.StartsWith(CrateTag)).ToList())
                Object.DestroyImmediate(old.gameObject);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, crate.transform);
            inst.name = CrateTag;
            inst.transform.localPosition = new Vector3(0f, -0.5f, 0f);   // pied du cube unité
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one / 0.8f;               // 0.8 m → 1, puis l'échelle du cube
            foreach (Collider c in inst.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(c);
            if (crate.GetComponent<MeshRenderer>() != null)
                Object.DestroyImmediate(crate.GetComponent<MeshRenderer>());
            if (mf != null)
                Object.DestroyImmediate(mf);
            // Lueur par le masque de la rune (CrateGlowWholeModel faux) ; le repli sur tout le modèle reste réglable par caisse.
            var g = crate.GetComponent<ItHunts.Orbs.Grabbable>();
            if (g != null)
            {
                var so = new SerializedObject(g);
                so.FindProperty("glowWholeModel").boolValue = CrateGlowWholeModel;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return true;
        }

        /// <summary>Le prefab de la caisse attrapable et toutes les caisses attrapables (cubes) des scènes de jeu.</summary>
        [MenuItem("ItHunts/Décor : caisses attrapables en SM_Caisse (prefab et scènes)")]
        public static void CratesMenu() => Debug.Log(InstallCrates());

        public static string InstallCrates()
        {
            int n = 0;
            const string prefab = PropsDir + "Prop_Caisse_Attrapable.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(prefab);
            try
            {
                if (ApplyCrateModel(root))
                    n++;
                PrefabUtility.SaveAsPrefabAsset(root, prefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            string current = EditorSceneManager.GetActiveScene().path;
            int inScenes = 0;
            var names = new List<string>();
            foreach (string path in new[] { "Assets/Scenes/Test_Movement.unity", "Assets/Scenes/Test_Arena.unity", "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_01_Base.unity" })
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int here = 0;
                foreach (ItHunts.Orbs.Grabbable g in Object.FindObjectsByType<ItHunts.Orbs.Grabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (g.IsEnemy || PrefabUtility.IsPartOfPrefabInstance(g.gameObject)
                        || new SerializedObject(g).FindProperty("startsAnchored").boolValue)
                        continue;                                        // ennemis, instances du prefab (faites par le prefab), lustres
                    names.Add(g.name);
                    if (ApplyCrateModel(g.gameObject))
                        here++;
                }

                if (here > 0)
                    EditorSceneManager.SaveScene(scene);
                inScenes += here;
            }

            if (!string.IsNullOrEmpty(current))
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            return $"caisses : prefab {n}, scènes {inScenes} ({string.Join(", ", names.Select(x => System.Text.RegularExpressions.Regex.Replace(x, @"[\d.,]+", "#")).Distinct())})";
        }

        private static Bounds LocalBounds(Transform t, Transform space)
        {
            var rs = t.GetComponentsInChildren<Renderer>(true);
            if (rs.Length == 0)
                return new Bounds(t.localPosition, Vector3.zero);
            bool first = true;
            var b = new Bounds();
            foreach (Renderer r in rs)
            {
                var f = r.GetComponent<MeshFilter>();
                if (f == null || f.sharedMesh == null)
                    continue;
                Bounds m = f.sharedMesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = m.center + Vector3.Scale(m.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = space.InverseTransformPoint(r.transform.TransformPoint(c));
                    if (first) { b = new Bounds(p, Vector3.zero); first = false; } else b.Encapsulate(p);
                }
            }

            return b;
        }

        // ---------- Arène du boss ----------

        public static readonly string[] ArenaScenes = { "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_01_Base.unity", "Assets/Scenes/Test_Arena.unity" };
        public const string ArenaPrefab = "Assets/_Project/Prefabs/Niveau/Sortie_Arene.prefab";

        public static string InstallArenas()
        {
            var report = new List<string>();
            GameObject p = PrefabUtility.LoadPrefabContents(ArenaPrefab);
            try
            {
                report.Add("Sortie_Arene : " + InstallArena(p.transform));
                PrefabUtility.SaveAsPrefabAsset(p, ArenaPrefab);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(p);
            }

            string current = EditorSceneManager.GetActiveScene().path;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return "annulé";
            foreach (string path in ArenaScenes)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var roots = scene.GetRootGameObjects();
                string r = "";
                foreach (GameObject g in roots)
                    if (g.GetComponentsInChildren<Transform>(true).Any(t => t.name == "Herse : grille"))
                        r = InstallArena(g.transform);
                EditorSceneManager.SaveScene(scene);
                report.Add($"{System.IO.Path.GetFileNameWithoutExtension(path)} : {r}");
            }

            if (!string.IsNullOrEmpty(current))
                EditorSceneManager.OpenScene(current, OpenSceneMode.Single);
            return "arène : " + string.Join(" ; ", report);
        }

        private static Transform Find(Transform root, string name) => root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        private static GameObject Place(string model, Transform parent, Vector3 worldPos, Quaternion worldRot)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}{model}.fbx");
            if (asset == null)
                return null;
            string tag = ModelTag + model;
            foreach (Transform old in parent.Cast<Transform>().Where(t => t.name == tag && Vector3.Distance(t.position, worldPos) < 0.5f).ToList())
                Object.DestroyImmediate(old.gameObject);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
            inst.name = tag;
            inst.transform.SetPositionAndRotation(worldPos, worldRot);
            return inst;
        }

        /// <summary>
        /// Modèle dans une boîte du jeu (pièce qui bouge, plateforme) : la boîte passe à l'échelle 1 (sa taille va dans son collider),
        /// le modèle est son enfant, au fond de la boîte. localBottom : en unités de la boîte d'origine (−0.5 = dessous).
        /// </summary>
        private static GameObject PlaceInBox(string model, Transform box, Vector3 localBottom, Quaternion localRot)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"{Models}{model}.fbx");
            if (asset == null)
                return null;
            foreach (Transform old in box.Cast<Transform>().Where(t => t.name.StartsWith(ModelTag)).ToList())
                Object.DestroyImmediate(old.gameObject);
            Vector3 size = box.localScale;
            if (size != Vector3.one)
            {
                var bc = box.GetComponent<BoxCollider>();
                if (bc != null)
                {
                    bc.size = Vector3.Scale(bc.size, size);
                    bc.center = Vector3.Scale(bc.center, size);
                }

                foreach (Transform child in box)
                    child.localPosition = Vector3.Scale(child.localPosition, size);
                box.localScale = Vector3.one;
            }
            else
            {
                var bc = box.GetComponent<BoxCollider>();
                if (bc != null)
                    size = bc.size;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(asset, box);
            inst.name = ModelTag + model;
            inst.transform.localPosition = Vector3.Scale(localBottom, size);
            inst.transform.localRotation = localRot;
            return inst;
        }

        private static void DropVisual(GameObject go)
        {
            if (go.GetComponent<MeshRenderer>() != null)
                Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
            if (go.GetComponent<MeshFilter>() != null)
                Object.DestroyImmediate(go.GetComponent<MeshFilter>());
        }

        private static string InstallArena(Transform root)
        {
            var done = new List<string>();
            // Herse : piliers (un modèle pour les deux) et grille (enfant de la pièce qui tombe).
            Transform south = Find(root, "Herse : pilier sud"), north = Find(root, "Herse : pilier nord"), gate = Find(root, "Herse : grille");
            if (south != null && north != null && gate != null)
            {
                Vector3 mid = (south.position + north.position) * 0.5f;
                Vector3 ground = new Vector3(mid.x, south.position.y - south.lossyScale.y * 0.5f, mid.z);
                Vector3 across = (north.position - south.position).normalized;            // le modèle a ses piliers sur x
                GameObject pillars = Place("SM_Herse", south.parent, ground, Quaternion.LookRotation(Vector3.Cross(across, Vector3.up), Vector3.up));
                Transform staticGrid = pillars != null ? Find(pillars.transform, "SM_Herse_Grille") : null;
                if (staticGrid != null)
                    staticGrid.gameObject.SetActive(false);                                // la grille mobile est posée à part
                foreach (Transform t in new[] { south, north })
                    DropVisual(t.gameObject);
                // Grille : 10 × 12 m, pivot en bas au centre, dans le plan du passage ; enfant de la pièce qui tombe.
                GameObject g = PlaceInBox("SM_Herse", gate, new Vector3(0f, -0.5f, 0f), Quaternion.identity);
                if (g != null)
                {
                    var pillarRenderer = g.GetComponent<MeshRenderer>();
                    if (pillarRenderer != null)
                        pillarRenderer.enabled = false;                                   // seulement la grille
                    Vector3 axis = gate.InverseTransformDirection(across);
                    g.transform.localRotation = Quaternion.FromToRotation(Vector3.right, new Vector3(axis.x, 0f, axis.z).normalized);
                    DropVisual(gate.gameObject);
                }

                done.Add("herse");
            }

            // Leviers : socle, plaque, cristal, manche ; la plaque garde sa boîte de collision (cible des objets lancés).
            int levers = 0;
            foreach (BossLever lev in root.GetComponentsInChildren<BossLever>(true))
            {
                Transform plate = lev.transform;
                Vector3 feet = plate.position - Vector3.up * 1.6f;
                GameObject m = Place("SM_Levier", plate.parent, feet, plate.rotation);
                if (m == null)
                    continue;
                Transform socle = Find(root, plate.name + " : socle");
                if (socle != null)
                    DropVisual(socle.gameObject);
                DropVisual(plate.gameObject);
                var so = new SerializedObject(lev);
                Renderer[] glow = m.GetComponentsInChildren<Renderer>(true).Where(r => r.sharedMaterials.Any(x => x != null && x.name.Contains("Levier_Lueur"))).ToArray();
                SerializedProperty gp = so.FindProperty("glow");
                gp.arraySize = glow.Length;
                for (int i = 0; i < glow.Length; i++)
                    gp.GetArrayElementAtIndex(i).objectReferenceValue = glow[i];
                so.FindProperty("handle").objectReferenceValue = Find(m.transform, "SM_Levier_Manche");
                so.ApplyModifiedPropertiesWithoutUndo();
                levers++;
            }

            if (levers > 0)
                done.Add($"{levers} leviers");

            // Treuil : contre le mur nord, sous la cible du lustre ; la manivelle tourne pendant la chute.
            BossTrap lustre = root.GetComponentsInChildren<BossTrap>(true).FirstOrDefault(t => t.Kind == TrapKind.Lustre);
            if (lustre != null && lustre.Trigger != null)
            {
                Vector3 target = lustre.Trigger.transform.position;
                Vector3 toWall = new Vector3(0f, 0f, 1f);
                float floor = lustre.transform.position.y - 7f;
                GameObject w = Place("SM_Treuil", lustre.transform.parent, new Vector3(target.x + 2.5f, floor, target.z + 1.15f), Quaternion.LookRotation(-toWall, Vector3.up));
                if (w != null)
                {
                    var so = new SerializedObject(lustre);
                    so.FindProperty("spinner").objectReferenceValue = Find(w.transform, "SM_Treuil_Manivelle");
                    so.ApplyModifiedPropertiesWithoutUndo();
                    done.Add("treuil");
                }
            }

            // Plateformes qui tombent (traversée de la fosse) : dalle de pierre fissurée.
            int plats = 0;
            foreach (CrumblePlatform c in root.GetComponentsInChildren<CrumblePlatform>(true).Where(c => c.name.Contains("traversée de la fosse")))
            {
                if (PlaceInBox("SM_PlateformeTombe", c.transform, new Vector3(0f, -0.5f, 0f), Quaternion.identity) == null)
                    continue;
                DropVisual(c.gameObject);
                plats++;
            }

            if (plats > 0)
                done.Add($"{plats} plateformes qui tombent");
            return string.Join(", ", done);
        }
    }
}
