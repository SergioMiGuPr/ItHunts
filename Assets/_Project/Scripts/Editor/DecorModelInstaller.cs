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
    /// sa boîte pleine (variante praticable à venir). Idempotent : un modèle déjà posé est remplacé.
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
            }

            AssetDatabase.SaveAssets();
            return "props : " + string.Join(", ", report);
        }

        private static string InstallProp(GameObject root, GameObject model, string prop)
        {
            foreach (Transform old in root.transform.Cast<Transform>().Where(t => t.name.StartsWith(ModelTag)).ToList())
                Object.DestroyImmediate(old.gameObject);
            // Emprise de la greybox (maillage de la racine, gardé dans le prefab tant qu'il n'a pas été retiré).
            var mf = root.GetComponent<MeshFilter>();
            Bounds grey = mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.up * 0.5f, Vector3.one);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            inst.name = ModelTag + model.name;
            Bounds mb = LocalBounds(inst.transform, root.transform);
            // Axes inversés (modèle long en z, greybox long en x) : quart de tour.
            bool swap = Mathf.Abs(grey.size.x - mb.size.z) + Mathf.Abs(grey.size.z - mb.size.x) + 0.05f < Mathf.Abs(grey.size.x - mb.size.x) + Mathf.Abs(grey.size.z - mb.size.z);
            if (swap)
            {
                inst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                mb = LocalBounds(inst.transform, root.transform);
            }

            Vector3 offset = new Vector3(grey.center.x - mb.center.x, grey.min.y - mb.min.y, grey.center.z - mb.center.z);
            string note = "";
            if (prop.StartsWith("Torche"))
            {
                float auto = offset.y;
                offset = new Vector3(grey.center.x - mb.center.x, TorchLift, grey.min.z - mb.min.z);   // dos de la plaque contre le mur
                note = $" (+{TorchLift} m demandé, {auto:0.000} m calculé)";
            }

            inst.transform.localPosition += offset;
            // Le greybox disparaît (le collider de la racine reste).
            if (root.GetComponent<MeshRenderer>() != null)
                Object.DestroyImmediate(root.GetComponent<MeshRenderer>());
            if (mf != null)
                Object.DestroyImmediate(mf);
            // Torches : flamme (sans lumière pour la torche éteinte) et lumière sur FX_Flamme.
            Transform fx = inst.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "FX_Flamme");
            if (fx != null)
            {
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
                    l.transform.position = fx.position + Vector3.up * 0.15f;
            }

            Bounds after = LocalBounds(inst.transform, root.transform);
            return $"{prop} ({model.name}{(swap ? ", ¼ de tour" : "")}{note} ; greybox {grey.size.x:0.##}×{grey.size.y:0.##}×{grey.size.z:0.##}, modèle {after.size.x:0.##}×{after.size.y:0.##}×{after.size.z:0.##})";
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
