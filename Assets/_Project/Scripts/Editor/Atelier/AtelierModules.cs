using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItHunts.Enemies;
using ItHunts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Modules de l'atelier (2026-11-21) : nouveau, sauvegarder (prefab dans Assets/_Project/Level/Modules/Atelier + fiche
    /// ModuleSheet), charger, dupliquer, supprimer, mettre dans la génération (catalogue du niveau 1), vérifier (ModuleValidator,
    /// avec repères dans la scène), jouer (le module seul dans le niveau, avec le joueur, le traqueur et les goules).
    /// </summary>
    public static class AtelierModules
    {
        public const string Dir = "Assets/_Project/Level/Modules/Atelier";
        public const string SheetDir = Dir + "/Fiches";
        public const string LegacyDir = "Assets/_Project/Prefabs/Modules";
        public const string PreviewName = "Coque d'aperçu (non sauvegardée)";

        /// <summary>Module ouvert dans la scène active (racine avec RoomModule), ou null.</summary>
        public static RoomModule OpenModule()
        {
            foreach (GameObject g in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                if (g.TryGetComponent(out RoomModule m))
                    return m;
            return null;
        }

        public static string SafeName(string name)
        {
            // Lettres (accents retirés), chiffres, tiret et soulignement seulement.
            string flat = new string(name.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark).ToArray());
            var sb = new System.Text.StringBuilder();
            foreach (char c in flat.Trim())
                sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            return "Module_" + sb.ToString();
        }

        public static string PrefabPathFor(string name) => $"{Dir}/{SafeName(name)}.prefab";

        /// <summary>Plafond du module selon son type (réglages de la carte).</summary>
        public static float Ceiling(RoomModule m)
        {
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutSettings>(TestArenaSceneBuilder.MapLayoutPath);
            return layout != null ? layout.Ceiling(m.kind, m.shape, m.ceilingOverride) : (m.kind == ModuleKind.Salle ? 18f : 16f);
        }

        // ---------- Nouveau, charger, coque d'aperçu ----------

        /// <summary>Nouveau module dans la scène : racine, zone limite, deux entrées (nord et sud), coque d'aperçu.</summary>
        public static RoomModule New(string name, ModuleKind kind, ModuleShape shape, Vector2Int footprint, float size)
        {
            CloseOpen();
            var root = new GameObject(SafeName(name));
            Undo.RegisterCreatedObjectUndo(root, "Nouveau module");
            var m = root.AddComponent<RoomModule>();
            m.displayName = name;
            m.kind = kind;
            m.shape = shape;
            m.footprint = new Vector2Int(Mathf.Clamp(footprint.x, 1, 2), Mathf.Clamp(footprint.y, 1, 2));
            m.size = Mathf.Round(size / 4f) * 4f;
            m.fromAtelier = true;
            float ceil = Ceiling(m);
            var zone = (GameObject)PrefabUtility.InstantiatePrefab(GameplayPrefab("G_ZoneLimite"), root.transform);
            zone.GetComponent<ModuleBounds>().size = new Vector3(m.Extent.x, ceil, m.Extent.y);
            zone.GetComponent<ModuleBounds>().center = new Vector3(0f, ceil * 0.5f, 0f);
            foreach (DoorSide side in new[] { DoorSide.Nord, DoorSide.Sud })
                AddConnector(m, side, 0);
            Preview(m);
            Selection.activeGameObject = root;
            return m;
        }

        public static ModuleDoor AddConnector(RoomModule m, DoorSide side, int cell)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(GameplayPrefab("G_Connecteur"), m.transform);
            var d = go.GetComponent<ModuleDoor>();
            d.side = side;
            d.cell = cell;
            go.name = $"Entrée {side} {cell}";
            go.transform.localPosition = m.DoorLocal(side, cell);
            go.transform.localRotation = Quaternion.Euler(0f, RoomModule.SideYaw(side), 0f);
            return d;
        }

        public static GameObject GameplayPrefab(string file) => AssetDatabase.LoadAssetAtPath<GameObject>($"{ModuleLibrary.AtelierGameplayDir}/{file}.prefab");

        /// <summary>
        /// Coque d'aperçu : ce que le générateur pose autour du module (sol, murs bas pour voir dedans, plafond en filaire), avec
        /// des collisions pour poser les pièces dessus. Jamais sauvegardée dans le module.
        /// </summary>
        public static GameObject Preview(RoomModule m)
        {
            GameObject old = GameObject.Find(PreviewName);
            if (old != null)
                Object.DestroyImmediate(old);
            var shell = new GameObject(PreviewName);
            shell.hideFlags = HideFlags.DontSave;
            Vector2 e = m.Extent;
            Material floor = ModuleLibrary.AtelierMaterial("M_Atelier_Coque", new Color(0.16f, 0.15f, 0.14f), Color.black);
            Material wall = ModuleLibrary.AtelierMaterial("M_Atelier_CoqueMur", new Color(0.24f, 0.22f, 0.2f), Color.black);
            Box(shell, "Sol", new Vector3(0f, -0.25f, 0f), new Vector3(e.x, 0.5f, e.y), floor);
            foreach ((Vector3 c, Vector3 s) in new[]
                     {
                         (new Vector3(0f, 1f, e.y * 0.5f + 0.5f), new Vector3(e.x + 2f, 2f, 1f)), (new Vector3(0f, 1f, -e.y * 0.5f - 0.5f), new Vector3(e.x + 2f, 2f, 1f)),
                         (new Vector3(e.x * 0.5f + 0.5f, 1f, 0f), new Vector3(1f, 2f, e.y)), (new Vector3(-e.x * 0.5f - 0.5f, 1f, 0f), new Vector3(1f, 2f, e.y)),
                     })
                Box(shell, "Mur (bas, aperçu)", c, s, wall);
            foreach (Transform t in shell.GetComponentsInChildren<Transform>(true))
                t.gameObject.hideFlags = HideFlags.DontSave;
            shell.transform.SetPositionAndRotation(m.transform.position, m.transform.rotation);
            return shell;
        }

        private static void Box(GameObject parent, string name, Vector3 c, Vector3 s, Material mat)
        {
            GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
            g.name = name;
            g.transform.SetParent(parent.transform, false);
            g.transform.localPosition = c;
            g.transform.localScale = s;
            g.GetComponent<Renderer>().sharedMaterial = mat;
        }

        /// <summary>Ferme le module ouvert (sans le sauvegarder) et sa coque d'aperçu.</summary>
        public static void CloseOpen()
        {
            RoomModule open = OpenModule();
            if (open != null)
                Undo.DestroyObjectImmediate(open.gameObject);
            GameObject old = GameObject.Find(PreviewName);
            if (old != null)
                Object.DestroyImmediate(old);
            Markers.Clear();
        }

        /// <summary>Tous les modules ouvrables : ceux de l'atelier et ceux du jeu (dont les 4 de la nuit).</summary>
        public static List<string> AllModulePaths()
        {
            var list = new List<string>();
            foreach (string dir in new[] { Dir, LegacyDir })
                if (Directory.Exists(dir))
                    list.AddRange(Directory.GetFiles(dir, "*.prefab").Select(p => p.Replace('\\', '/')));
            return list.Where(p => AssetDatabase.LoadAssetAtPath<GameObject>(p)?.GetComponent<RoomModule>() != null).OrderBy(p => p).ToList();
        }

        /// <summary>Ouvre un module dans la scène (instance du prefab, à l'origine) avec sa coque d'aperçu.</summary>
        public static RoomModule Load(string path)
        {
            CloseOpen();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            Undo.RegisterCreatedObjectUndo(go, "Charger le module");
            var m = go.GetComponent<RoomModule>();
            Preview(m);
            Selection.activeGameObject = go;
            return m;
        }

        // ---------- Sauvegarde, fiche, bibliothèque ----------

        /// <summary>Chemin du prefab du module ouvert (le sien s'il en vient, sinon dans le dossier de l'atelier).</summary>
        public static string PathOf(RoomModule m)
        {
            string p = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(m.gameObject);
            return !string.IsNullOrEmpty(p) ? p : PrefabPathFor(m.displayName);
        }

        /// <summary>Sauvegarde le module ouvert : prefab, fiche ; bibliothèque du générateur selon « actif ». Renvoie le chemin.</summary>
        public static string Save(RoomModule m, bool? active = null)
        {
            string path = PathOf(m);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            PrefabUtility.SaveAsPrefabAssetAndConnect(m.gameObject, path, InteractionMode.AutomatedAction);
            ModuleSheet sheet = WriteSheet(path);
            if (active.HasValue)
                sheet.active = active.Value;
            Register(path, sheet.active);
            AssetDatabase.SaveAssets();
            return path;
        }

        public static string SheetPathFor(string prefabPath) => $"{SheetDir}/Fiche_{Path.GetFileNameWithoutExtension(prefabPath)}.asset";

        public static ModuleSheet SheetOf(string prefabPath) => AssetDatabase.LoadAssetAtPath<ModuleSheet>(SheetPathFor(prefabPath));

        /// <summary>Fiche du module : relue depuis le prefab (qui fait foi). Garde « actif » et la difficulté déjà réglés.</summary>
        public static ModuleSheet WriteSheet(string prefabPath)
        {
            Directory.CreateDirectory(SheetDir);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var m = prefab.GetComponent<RoomModule>();
            string sp = SheetPathFor(prefabPath);
            ModuleSheet sheet = AssetDatabase.LoadAssetAtPath<ModuleSheet>(sp);
            bool created = sheet == null;
            if (created)
            {
                sheet = ScriptableObject.CreateInstance<ModuleSheet>();
                sheet.active = true;
            }

            sheet.displayName = m.displayName;
            sheet.kind = m.kind;
            sheet.shape = m.shape;
            sheet.footprint = m.footprint;
            sheet.size = m.size;
            sheet.weight = m.weight;
            sheet.prefab = prefab;
            sheet.connectors = prefab.GetComponentsInChildren<ModuleDoor>(true)
                .Select(d => new SheetConnector { side = d.side, cell = d.cell, required = d.required, width = d.width, height = d.height }).ToList();
            var zone = prefab.GetComponentInChildren<ModuleBounds>(true);
            sheet.bounds = zone != null ? zone.Local : new Bounds(Vector3.up * 9f, new Vector3(m.Extent.x, 18f, m.Extent.y));
            sheet.chests = prefab.GetComponentsInChildren<ChestSpot>(true).Length;
            sheet.spawns = prefab.GetComponentsInChildren<SpawnPoint>(true).Length + prefab.GetComponentsInChildren<ResidentSpot>(true).Length;
            sheet.passages = prefab.GetComponentsInChildren<StalkerPassage>(true).Length;
            sheet.lights = prefab.GetComponentsInChildren<Light>(true).Length;
            sheet.triangles = prefab.GetComponentsInChildren<MeshFilter>(true).Where(f => f.sharedMesh != null).Sum(f => f.sharedMesh.triangles.Length / 3);
            sheet.pieces = prefab.GetComponentsInChildren<Transform>(true).Count(t => t != prefab.transform && t.parent == prefab.transform);
            sheet.lastReport = m.validationReport;
            if (created)
                AssetDatabase.CreateAsset(sheet, sp);
            EditorUtility.SetDirty(sheet);
            return sheet;
        }

        /// <summary>Met le module dans le catalogue du générateur (actif) ou l'en retire. Il n'est tiré que s'il est validé.</summary>
        public static void Register(string prefabPath, bool active)
        {
            ModuleCatalog cat = ModuleValidator.EnsureCatalog();
            var m = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)?.GetComponent<RoomModule>();
            if (m == null)
                return;
            bool has = cat.modules.Contains(m);
            if (active && !has)
                cat.modules.Add(m);
            else if (!active && has)
                cat.modules.Remove(m);
            var sheet = SheetOf(prefabPath);
            if (sheet != null && sheet.active != active)
            {
                sheet.active = active;
                EditorUtility.SetDirty(sheet);
            }

            EditorUtility.SetDirty(cat);
            AssetDatabase.SaveAssets();
        }

        public static bool InLibrary(string prefabPath)
        {
            var m = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)?.GetComponent<RoomModule>();
            return m != null && ModuleValidator.EnsureCatalog().modules.Contains(m);
        }

        /// <summary>Copie d'un module (prefab et fiche) sous un autre nom, hors de la génération jusqu'à sa vérification.</summary>
        public static string Duplicate(string prefabPath, string newName)
        {
            string dst = PrefabPathFor(newName);
            Directory.CreateDirectory(Dir);
            AssetDatabase.CopyAsset(prefabPath, dst);
            GameObject contents = PrefabUtility.LoadPrefabContents(dst);
            var m = contents.GetComponent<RoomModule>();
            m.displayName = newName;
            m.fromAtelier = true;
            m.validated = false;
            contents.name = Path.GetFileNameWithoutExtension(dst);
            PrefabUtility.SaveAsPrefabAsset(contents, dst);
            PrefabUtility.UnloadPrefabContents(contents);
            WriteSheet(dst).active = false;
            Register(dst, false);
            return dst;
        }

        /// <summary>Supprime un module (prefab et fiche) et le retire de la génération.</summary>
        public static void Delete(string prefabPath)
        {
            Register(prefabPath, false);
            AssetDatabase.DeleteAsset(SheetPathFor(prefabPath));
            AssetDatabase.DeleteAsset(prefabPath);
            AssetDatabase.SaveAssets();
        }

        // ---------- Vérification ----------

        /// <summary>Repères de la dernière vérification, dans la scène (monde).</summary>
        public static readonly List<ModuleValidator.Marker> Markers = new List<ModuleValidator.Marker>();
        public static string LastReport = "";

        /// <summary>Sauvegarde puis vérifie le module (ModuleValidator) ; repères dans la scène. Vrai s'il est validé.</summary>
        public static bool Verify(RoomModule m)
        {
            string path = Save(m);
            ModuleValidator.Validate(path);
            Markers.Clear();
            Markers.AddRange(ModuleValidator.LastMarkers);
            LastReport = ModuleValidator.LastReport;
            WriteSheet(path).lastReport = LastReport;
            SceneView.RepaintAll();
            return ModuleValidator.LastOk;
        }

        [InitializeOnLoadMethod]
        private static void DrawMarkers() => SceneView.duringSceneGui += _ =>
        {
            RoomModule m = Markers.Count > 0 ? OpenModule() : null;
            if (m == null)
                return;
            foreach (ModuleValidator.Marker k in Markers)
            {
                Vector3 p = m.transform.TransformPoint(k.Local);
                Handles.color = k.Error ? new Color(1f, 0.15f, 0.1f) : new Color(1f, 0.75f, 0.1f);
                Handles.DrawWireDisc(p, Vector3.up, 1.2f);
                Handles.DrawLine(p, p + Vector3.up * 4f);
                Handles.Label(p + Vector3.up * 4.3f, k.Text, EditorStyles.whiteBoldLabel);
            }
        };

        // ---------- Jouer ce module ----------

        public const string TestKey = "ItHunts.Atelier.TestModule";

        /// <summary>
        /// Lance le module en Play Mode : le niveau aléatoire avec ce module (même non validé), le joueur parti dans sa salle, le
        /// traqueur et les goules du niveau. À la sortie du Play Mode, l'atelier se rouvre.
        /// </summary>
        public static void Play(RoomModule m, int seed = 1)
        {
            string path = Save(m);
            EditorSceneManager.SaveOpenScenes();
            SessionState.SetString(TestKey, path);
            SessionState.SetInt("ItHunts.Atelier.TestSeed", seed);
            SessionState.SetBool("ItHunts.Atelier.Reopen", true);
            PlayFromMainMenu.Enabled = false;
            EditorSceneManager.OpenScene("Assets/Scenes/Level_01_Aleatoire.unity");
            EditorApplication.EnterPlaymode();
        }

        [InitializeOnLoadMethod]
        private static void ReopenAfterPlay() => EditorApplication.playModeStateChanged += s =>
        {
            if (s != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool("ItHunts.Atelier.Reopen", false))
                return;
            SessionState.EraseBool("ItHunts.Atelier.Reopen");
            RandomLevel.TestModule = null;                                 // le test du module ne déborde pas sur la partie suivante
            RandomLevel.TestModulePrefab = null;
            RandomLevel.TestStartInModule = false;
            RandomLevel.SeedOverride = 0;
            string path = SessionState.GetString("ItHunts.Atelier.LastModule", "");
            EditorSceneManager.OpenScene(AtelierSceneBuilder.ScenePath);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                Load(path);
        };

        /// <summary>
        /// Met un module existant (fait par code, par exemple les 4 de la nuit du 2026-11-21) au format de l'atelier : zone limite,
        /// drapeau « atelier », fiche ; puis le revalide (son contenu a changé). Résumé.
        /// </summary>
        public static string Convert(string path)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var m = contents.GetComponent<RoomModule>();
            if (contents.GetComponentInChildren<ModuleBounds>(true) == null)
            {
                var zone = (GameObject)PrefabUtility.InstantiatePrefab(GameplayPrefab("G_ZoneLimite"), contents.transform);
                float ceil = Ceiling(m);
                zone.GetComponent<ModuleBounds>().size = new Vector3(m.Extent.x, ceil, m.Extent.y);
                zone.GetComponent<ModuleBounds>().center = new Vector3(0f, ceil * 0.5f, 0f);
            }

            m.fromAtelier = true;
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            ModuleSheet sheet = WriteSheet(path);
            sheet.active = InLibrary(path);
            string r = ModuleValidator.Validate(path);
            WriteSheet(path).lastReport = ModuleValidator.LastReport;
            AssetDatabase.SaveAssets();
            return r;
        }

        public static void RememberLast(string path) => SessionState.SetString("ItHunts.Atelier.LastModule", path);
    }
}
