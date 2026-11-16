using System.Collections.Generic;
using System.Linq;
using ItHunts.Economy;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Scène vitrine Test_Vitrine (retours du lot, point 9), reconstruite à la demande : copie de Test_Arena réduite à ses systèmes
    /// (joueur, NavMesh, état de la zone, ambiance, écran partagé et menu pause), puis, sur un grand sol éclairé :
    /// - Lumière : repères au sol à 10, 20, 30 et 50 m devant le départ (texte et caisse attrapable à chaque repère) ;
    /// - Props : toutes les pièces du kit et tous les props, rangés par famille (architecture, mobilier, arène du boss, objets
    ///   attrapables), chacun étiqueté « DA » ou « greybox » ;
    /// - Goules (un état chacune, en boucle), une goule libre (mur, échafaudage) ; volants ; traqueurs (une pose chacun) et un
    ///   enclos où un traqueur poursuit vraiment le joueur (activable).
    /// Panneau F1 (Vitrine) : personnage, gestes de la vue FPS, ambiance, figé, poursuite. Ajoutée au build.
    /// </summary>
    public static class VitrineSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Test_Vitrine.unity";
        private const string Source = "Assets/Scenes/Test_Arena.unity";

        private static Transform _root;
        private static readonly Color DaColor = new Color(0.55f, 0.95f, 0.6f), GreyColor = new Color(1f, 0.7f, 0.35f), Title = new Color(0.8f, 0.9f, 1f);

        [MenuItem("ItHunts/Reconstruire la scène Test_Vitrine (vitrine des assets et des ennemis)")]
        public static void Menu() => Debug.Log(Build());

        public static string Build()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(Source, ScenePath))
                return "copie de Test_Arena impossible";
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject[] roots = scene.GetRootGameObjects();
            GameObject Root(string prefix) => roots.FirstOrDefault(r => r != null && r.name.StartsWith(prefix));

            // Modèles d'ennemis pris dans Test_Arena (le traqueur n'a pas de prefab).
            Stalker stalkerTemplate = Object.FindObjectsByType<Stalker>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
            if (stalkerTemplate != null)
                stalkerTemplate.transform.SetParent(null, true);
            foreach (string drop in new[] { "Niveau", "Points d'apparition", "Graphe du labyrinthe", "Minimap", "Mesure de performance",
                         "Réserve des vagues", "Vagues (directeur)" })
                foreach (GameObject r in roots.Where(r => r != null && r.name.StartsWith(drop)))
                    Object.DestroyImmediate(r);

            _root = new GameObject("Vitrine").transform;
            TestArenaSceneBuilder.UseEditorSpawner();
            KitSet kit = TestArenaSceneBuilder.BuildKit();
            var report = new List<string>();

            // Sol et lumières.
            Floor(kit, new Vector3(-30f, 0f, 60f), new Vector3(420f, 1f, 260f));
            foreach (Vector3 p in new[] { new Vector3(0f, 12f, 25f), new Vector3(-60f, 14f, 10f), new Vector3(-60f, 14f, 50f), new Vector3(40f, 14f, 15f),
                         new Vector3(40f, 14f, 45f), new Vector3(80f, 20f, 85f), new Vector3(60f, 20f, 140f), new Vector3(0f, 12f, 55f) })
                Lamp(p);
            for (float lx = -45f; lx >= -230f; lx -= 35f)                    // le long des rangées de props
                foreach (float lz in new[] { 6f, 38f })
                    Lamp(new Vector3(lx, 14f, lz));

            // Départ : au sud, face au nord (+z).
            Vector3 start = new Vector3(0f, 0f, -4f);
            GameObject startGo = Root("Départ des joueurs");
            if (startGo != null)
                startGo.transform.SetPositionAndRotation(start, Quaternion.identity);
            var player = Object.FindFirstObjectByType<ItHunts.Player.PlayerHealth>();
            if (player != null)
                player.transform.SetPositionAndRotation(start + Vector3.up * 0.05f, Quaternion.identity);
            var nav = Object.FindFirstObjectByType<ZoneNavMesh>();
            if (nav != null)
            {
                nav.transform.position = new Vector3(-30f, 10f, 60f);
                nav.Configure(new Vector3(-30f, 10f, 60f), new Vector3(420f, 40f, 260f));
            }

            report.Add(Distances(start));
            report.Add(Props(kit));
            var vitrine = new GameObject("Vitrine (panneau F1, états des ennemis)").AddComponent<Vitrine>();
            var so = new SerializedObject(vitrine);
            report.Add(Ghouls(so, kit));
            report.Add(Flyers(so));
            report.Add(Stalkers(so, stalkerTemplate, kit));
            if (stalkerTemplate != null)
                Object.DestroyImmediate(stalkerTemplate.gameObject);

            // Personnages avec bras, ambiances.
            var chars = AssetDatabase.FindAssets("t:CharacterDefinition").Select(g => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(c => c != null && c.fpsArmRight != null).OrderBy(c => c.id).ToList();
            Fill(so.FindProperty("characters"), chars.Cast<Object>().ToList());
            Fill(so.FindProperty("ambiances"), new[] { "Intermediaire", "Sombre", "Horreur" }
                .Select(n => (Object)AssetDatabase.LoadAssetAtPath<AmbianceSettings>($"Assets/_Project/Settings/Ambiance_{n}.asset")).ToList());
            so.ApplyModifiedPropertiesWithoutUndo();
            KitBuild.Spawner = (prefab, parent) => Object.Instantiate(prefab, parent, false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AddToBuild();
            return "Test_Vitrine : " + string.Join(" ; ", report);
        }

        private static void Fill(SerializedProperty list, List<Object> items)
        {
            list.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static void AddToBuild()
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath))
                return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ---------- Sol, lumières, textes ----------

        private static void Floor(KitSet kit, Vector3 center, Vector3 size)
        {
            GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
            f.name = "Sol de la vitrine";
            f.transform.SetParent(_root, false);
            f.transform.position = center + Vector3.down * 0.5f;
            f.transform.localScale = size;
            Renderer kitFloor = kit != null && kit.sol4 != null ? kit.sol4.GetComponentInChildren<Renderer>() : null;
            if (kitFloor != null)
                f.GetComponent<Renderer>().sharedMaterial = kitFloor.sharedMaterial;          // dalles de la DA (UV monde)
        }

        private static void Lamp(Vector3 p)
        {
            var go = new GameObject("Lampe de la vitrine");
            go.transform.SetParent(_root, false);
            go.transform.position = p;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 45f;
            l.intensity = 6f;
            l.color = new Color(1f, 0.92f, 0.82f);
            l.shadows = LightShadows.None;
        }

        private static void Label(Vector3 at, string text, Color color, float height = 0.35f)
        {
            var anchor = new GameObject("Étiquette : " + text.Split('\n')[0]).transform;
            anchor.SetParent(_root, false);
            anchor.position = at;
            WorldLabel.Create(anchor, Vector3.zero, text, color, height);
        }

        private static Bounds BoundsOf(GameObject go)
        {
            Renderer[] rs = go.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer)).ToArray();
            if (rs.Length == 0)
                return new Bounds(go.transform.position, Vector3.one);
            Bounds b = rs[0].bounds;
            foreach (Renderer r in rs)
                b.Encapsulate(r.bounds);
            return b;
        }

        /// <summary>Un modèle de la DA : au moins un maillage importé depuis Assets/Art.</summary>
        public static bool FromArt(GameObject go) =>
            go.GetComponentsInChildren<MeshFilter>(true).Any(m => m.sharedMesh != null && AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/Art/"))
            || go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any(m => m.sharedMesh != null && AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/Art/"));

        // ---------- Repères de distance ----------

        private static string Distances(Vector3 start)
        {
            var crate = AssetDatabase.LoadAssetAtPath<GameObject>(PropLibrary.Dir + "/Prop_Caisse_Attrapable.prefab");
            Label(start + new Vector3(0f, 2.2f, 3f), "Lumière : repères à 10, 20, 30 et 50 m\n(textes, caisse attrapable ; F1 : ambiance)", Title, 0.3f);
            const string matPath = "Assets/_Project/Art/Greybox/Vitrine_Repere.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = new Color(0.9f, 0.85f, 0.5f) };
                AssetDatabase.CreateAsset(mat, matPath);
            }

            foreach (float d in new[] { 10f, 20f, 30f, 50f })
            {
                GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = $"Repère {d} m";
                Object.DestroyImmediate(line.GetComponent<Collider>());
                line.transform.SetParent(_root, false);
                line.transform.position = start + new Vector3(0f, 0.01f, d);
                line.transform.localScale = new Vector3(10f, 0.02f, 0.25f);
                line.GetComponent<Renderer>().sharedMaterial = mat;
                Label(start + new Vector3(-3.5f, 1.4f, d), $"{d} m", Title, 0.45f);
                if (crate != null)
                {
                    var c = (GameObject)PrefabUtility.InstantiatePrefab(crate, _root);
                    c.transform.position = start + new Vector3(3f, 0.45f, d);
                }
            }

            return "repères 10, 20, 30, 50 m";
        }

        // ---------- Props ----------

        private static readonly string[] Architecture =
        {
            "Kit_Mur4x4", "Kit_Mur2x4", "Kit_Mur4x3", "Kit_Sol4", "Kit_Escalier", "Kit_Pont", "Kit_Pilier", "Kit_Arc", "Kit_PortePiece",
            "Kit_GrandePorte", "Kit_Poutre", "Kit_PontEgout4", "Kit_PontEgout14", "Prop_Colonne", "Prop_ColonneBrisee", "Prop_Poutre",
            "Prop_Echafaudage", "Prop_Torche", "Prop_TorcheEteinte", "Prop_Tapisserie", "Prop_Chaine", "Prop_Grille", "Prop_Tuyau", "Prop_Cage",
            "Prop_MurEffondre", "Prop_Gravats",
        };

        private static readonly string[] Grabbables = { "Prop_Caisse_Attrapable", "Prop_Chaise_Attrapable", "Prop_Tonneau_Attrapable" };
        private static readonly string[] Boss = { "SM_Herse", "SM_Levier", "SM_Treuil", "SM_PlateformeTombe" };

        private static string Props(KitSet kit)
        {
            var all = new Dictionary<string, GameObject>();
            foreach (string dir in new[] { "Assets/_Project/Prefabs/Kit", PropLibrary.Dir })
                foreach (string g in AssetDatabase.FindAssets("t:Prefab", new[] { dir }))
                {
                    var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                    if (go != null)
                        all[go.name] = go;
                }

            foreach (string extra in new[] { "Assets/_Project/Prefabs/Niveau/Coffre.prefab", "Assets/_Project/Prefabs/Niveau/Marchand_Artefact.prefab" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(extra);
                if (go != null)
                    all[go.name] = go;
            }

            var furniture = all.Keys.Where(k => !Architecture.Contains(k) && !Grabbables.Contains(k)).OrderBy(k => k).ToList();
            var boss = Boss.Select(n => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Models/Niveau1/{n}.fbx")).Where(g => g != null).ToList();
            int da = 0, grey = 0;
            float z = 0f;
            void Row(string title, IEnumerable<GameObject> items)
            {
                float x = -30f;
                Label(new Vector3(x + 3f, 3.2f, z - 4f), title, Title, 0.5f);
                foreach (GameObject prefab in items)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _root);
                    inst.transform.position = Vector3.zero;
                    inst.transform.rotation = Quaternion.Euler(0f, 180f, 0f);                // face au joueur qui arrive du sud-est
                    Bounds b = BoundsOf(inst);
                    float w = Mathf.Max(1.2f, b.size.x);
                    x -= w * 0.5f;
                    inst.transform.position = new Vector3(x - (b.center.x - inst.transform.position.x), -b.min.y + Mathf.Max(0f, inst.transform.position.y), z);
                    foreach (Rigidbody rb in inst.GetComponentsInChildren<Rigidbody>(true))
                        rb.isKinematic = true;                                                // posés (attrapables : se réveillent quand on les attrape)
                    bool art = FromArt(inst);
                    if (art) da++; else grey++;
                    Label(new Vector3(x, Mathf.Max(b.size.y, 1f) + 0.6f, z - Mathf.Max(0.6f, b.extents.z) - 0.2f),
                        $"{prefab.name}\n{(art ? "DA" : "greybox")}", art ? DaColor : GreyColor, 0.22f);
                    x -= w * 0.5f + 1.6f;
                }

                z += 16f;
            }

            Row("Architecture (kit et décor)", Architecture.Where(all.ContainsKey).Select(n => all[n]));
            Row("Mobilier", furniture.Select(n => all[n]));
            Row("Arène du boss", boss);
            Row("Objets attrapables", Grabbables.Where(all.ContainsKey).Select(n => all[n]));
            return $"props : {da} DA, {grey} greybox";
        }

        // ---------- Ennemis ----------

        private static Vector3 Ground(float x, float z) => new Vector3(x, 0f, z);

        private static string Ghouls(SerializedObject so, KitSet kit)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Niveau/Goule.prefab");
            if (prefab == null)
                return "goules : prefab absent";
            Label(new Vector3(50f, 4.6f, 14f), "Goules (figées : chaque état en boucle ; F1 : IA)", Title, 0.4f);
            var list = so.FindProperty("ghouls");
            var states = System.Enum.GetValues(typeof(VitrineGhoulState)).Cast<VitrineGhoulState>().ToList();
            list.arraySize = states.Count;
            for (int i = 0; i < states.Count; i++)
            {
                Vector3 at = Ground(30f + i * 4.5f, 10f);
                var g = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _root);
                g.name = "Goule : " + states[i];
                g.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 180f, 0f));
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("ghoul").objectReferenceValue = g.GetComponent<Ghoul>();
                e.FindPropertyRelative("state").enumValueIndex = i;
                if (states[i] == VitrineGhoulState.Escalade)
                {
                    // Un mur derrière elle (côté nord), elle y monte et en descend, face au mur.
                    KitBuild.Box(_root, "Mur d'escalade (goule)", at + new Vector3(0f, 3f, 1.5f), new Vector3(3f, 6f, 1f), KitBuild.Kind.Mur, kit);
                    e.FindPropertyRelative("wallBottom").vector3Value = at + new Vector3(0f, 0.3f, 0.45f);
                    e.FindPropertyRelative("wallTop").vector3Value = at + new Vector3(0f, 4.2f, 0.45f);
                }

                Label(at + new Vector3(0f, 2.8f, -1.2f), Pretty(states[i].ToString()), Title, 0.26f);
            }

            // Goule libre : mur de 4 m puis échafaudage (plateaux à 3 et 6 m), en boucle.
            Vector3 b0 = Ground(40f, 34f);
            KitBuild.Box(_root, "Bloc à escalader (4 m)", b0 + new Vector3(0f, 2f, 4f), new Vector3(6f, 4f, 6f), KitBuild.Kind.Bloc, kit);
            var scaffold = AssetDatabase.LoadAssetAtPath<GameObject>(PropLibrary.Dir + "/Prop_Echafaudage.prefab");
            Vector3 sc = b0 + new Vector3(12f, 0f, 4f);
            if (scaffold != null)
            {
                var s = (GameObject)PrefabUtility.InstantiatePrefab(scaffold, _root);
                s.transform.SetPositionAndRotation(sc, Quaternion.identity);
            }

            var free = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _root);
            free.name = "Goule libre (mur, échafaudage)";
            free.transform.position = b0 + new Vector3(0f, 0f, -6f);
            so.FindProperty("freeGhoul").objectReferenceValue = free.GetComponent<Ghoul>();
            var path = so.FindProperty("freePath");
            var pts = new List<(Vector3 p, GhoulMode m, float d, Vector3 n)>
            {
                (b0 + new Vector3(0f, 0f, -6f), GhoulMode.Bond, 1.1f, Vector3.forward),       // retour du saut
                (b0 + new Vector3(0f, 0f, 0.45f), GhoulMode.Chasse, 0.8f, Vector3.forward),   // court au pied du mur
                (b0 + new Vector3(0f, 3.6f, 0.45f), GhoulMode.Mur, 0.9f, Vector3.forward),    // grimpe
                (b0 + new Vector3(0f, 4f, 2.5f), GhoulMode.Chasse, 0.35f, Vector3.forward),   // passe le rebord
                (b0 + new Vector3(2.5f, 4f, 4f), GhoulMode.Chasse, 0.5f, Vector3.forward),
                (sc + new Vector3(-1.5f, 3f, 0f), GhoulMode.Bond, 0.9f, Vector3.forward),     // saute sur le plateau à 3 m
                (sc + new Vector3(-0.2f, 3f, 0f), GhoulMode.Chasse, 0.4f, Vector3.right),
                (sc + new Vector3(-0.2f, 6f, 0f), GhoulMode.Mur, 0.8f, Vector3.right),        // grimpe au plateau de 6 m
                (sc + new Vector3(0.5f, 6f, 0f), GhoulMode.Chasse, 0.5f, Vector3.right),
            };
            path.arraySize = pts.Count;
            for (int i = 0; i < pts.Count; i++)
            {
                var e = path.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("position").vector3Value = pts[i].p;
                e.FindPropertyRelative("mode").enumValueIndex = (int)pts[i].m;
                e.FindPropertyRelative("duration").floatValue = pts[i].d;
                e.FindPropertyRelative("wallNormal").vector3Value = pts[i].n;
            }

            Label(b0 + new Vector3(6f, 7.5f, -2f), "Goule libre : grimpe le mur, puis l'échafaudage", Title, 0.4f);
            return $"goules : {states.Count} états + 1 libre";
        }

        private static string Flyers(SerializedObject so)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Niveau/Volant.prefab");
            if (prefab == null)
                return "volants : prefab absent";
            Label(new Vector3(40f, 7f, 22f), "Volants", Title, 0.5f);
            var list = so.FindProperty("flyers");
            var states = System.Enum.GetValues(typeof(VitrineFlyerState)).Cast<VitrineFlyerState>().ToList();
            list.arraySize = states.Count;
            for (int i = 0; i < states.Count; i++)
            {
                Vector3 at = new Vector3(30f + i * 6f, 4f, 22f);
                var f = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _root);
                f.name = "Volant : " + states[i];
                f.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 180f, 0f));
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("flyer").objectReferenceValue = f.GetComponent<Flyer>();
                e.FindPropertyRelative("state").enumValueIndex = i;
                Label(new Vector3(at.x, 1.8f, at.z - 1.5f), Pretty(states[i].ToString()), Title, 0.26f);
            }

            return $"volants : {states.Count} états";
        }

        private static string Stalkers(SerializedObject so, Stalker template, KitSet kit)
        {
            if (template == null)
                return "traqueur : modèle absent de Test_Arena";
            Label(new Vector3(70f, 16f, 60f), "Traqueur (figé : une pose chacun)", Title, 0.9f);
            var list = so.FindProperty("stalkers");
            var poses = System.Enum.GetValues(typeof(Stalker.ShowcasePose)).Cast<Stalker.ShowcasePose>().ToList();
            list.arraySize = poses.Count;
            for (int i = 0; i < poses.Count; i++)
            {
                Vector3 at = Ground(20f + i * 22f, 70f);
                var s = Object.Instantiate(template.gameObject, _root);
                s.name = "Traqueur : " + poses[i];
                s.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 180f, 0f));
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("stalker").objectReferenceValue = s.GetComponent<Stalker>();
                e.FindPropertyRelative("pose").enumValueIndex = i;
                if (poses[i] == Stalker.ShowcasePose.Baisse)
                    KitBuild.Box(_root, "Pont (le traqueur passe dessous)", at + new Vector3(0f, 9.5f, 0f), new Vector3(14f, 1f, 4f), KitBuild.Kind.Pont, kit);
                Label(at + new Vector3(0f, 1.6f, -16f), Pretty(poses[i].ToString()), Title, 0.6f);
            }

            // Enclos : un traqueur qui poursuit vraiment le joueur (activable dans le panneau).
            Vector3 c = Ground(60f, 140f);
            foreach ((Vector3 p, Vector3 sz) in new[]
                     {
                         (c + new Vector3(0f, 1.5f, 30f), new Vector3(80f, 3f, 1f)), (c + new Vector3(0f, 1.5f, -30f), new Vector3(30f, 3f, 1f)),
                         (c + new Vector3(-40f, 1.5f, 0f), new Vector3(1f, 3f, 60f)), (c + new Vector3(40f, 1.5f, 0f), new Vector3(1f, 3f, 60f)),
                     })
                KitBuild.Box(_root, "Enclos du traqueur", p, sz, KitBuild.Kind.Mur, kit);
            var chaser = Object.Instantiate(template.gameObject, _root);
            chaser.name = "Traqueur de l'enclos (poursuite)";
            chaser.transform.SetPositionAndRotation(c + new Vector3(0f, 0f, 15f), Quaternion.Euler(0f, 180f, 0f));
            so.FindProperty("chaser").objectReferenceValue = chaser.GetComponent<Stalker>();
            Label(c + new Vector3(0f, 3.5f, -31f), "Enclos : le traqueur te poursuit vraiment (F1 : poursuite)", Title, 0.6f);
            return $"traqueurs : {poses.Count} poses + enclos";
        }

        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            ["Repos"] = "Repos", ["Course"] = "Course (sur place)", ["AnnonceDuBond"] = "Annonce du bond", ["Bond"] = "Bond",
            ["Reception"] = "Réception", ["Escalade"] = "Escalade sur un mur", ["Coup"] = "Coup reçu", ["Etourdie"] = "Étourdie",
            ["Tenue"] = "Tenue (télékinésie)", ["Morte"] = "Morte",
            ["Ronde"] = "Ronde", ["AnnonceDuPique"] = "Annonce du piqué", ["Pique"] = "Piqué", ["EtourdiAuSol"] = "Étourdi au sol",
            ["Tenu"] = "Tenu (télékinésie)",
            ["MarcheVoutee"] = "Marche voûtée", ["Prise"] = "Prise à la main", ["MainPlantee"] = "Main plantée",
            ["MainsLevees"] = "Mains levées", ["Revers"] = "Revers", ["Baisse"] = "Baissé sous un pont",
        };

        private static string Pretty(string id) => Names.TryGetValue(id, out string n) ? n : id;
    }
}
