using System.Collections.Generic;
using System.Linq;
using System.Text;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Orbs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// « Valider le module » (carte aléatoire) : un module n'entre dans le tirage que s'il passe ces contrôles, faits une fois
    /// dans l'éditeur (trop longs pendant la partie). Le module est posé dans une scène temporaire, dans une coque de test (sol,
    /// murs, plafond, toutes ses entrées ouvertes). Erreurs (module refusé) :
    /// - format : empreinte 1 ou 2 cases, côté au plus 112 m (8 m de couloir au moins entre deux salles), au moins 2 entrées ;
    /// - voies : rien de solide sous 10 m dans les voies (laneHalfWidth) de chaque entrée au centre de sa case et entre les
    ///   cases (le traqueur passe) ; sauf les modules à fosse ;
    /// - cachettes : tout point où l'on tient debout est à portée de la main du traqueur (ReachCheck) ;
    /// - NavMesh : un chemin complet (goule) entre chaque paire d'entrées.
    /// Avertissements : pièces hors kit (structure en primitives), côté non multiple de 4 m, pas de point d'apparition,
    /// d'habitant ou de coffre. Le résultat (validé, rapport, empreinte du contenu) est écrit dans le RoomModule.
    /// </summary>
    public static class ModuleValidator
    {
        public const string CatalogPath = "Assets/_Project/Settings/ModuleCatalog_Niveau1.asset";

        [MenuItem("ItHunts/Modules : valider le module sélectionné")]
        private static void ValidateSelected()
        {
            foreach (Object o in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(o);
                if (path.EndsWith(".prefab"))
                    Debug.Log(Validate(path));
            }
        }

        [MenuItem("ItHunts/Modules : valider tous les modules du catalogue")]
        private static void ValidateCatalogMenu() => Debug.Log(ValidateCatalog());

        /// <summary>Catalogue du niveau 1 : créé s'il manque, avec tous les modules du dossier (ordre des noms) ; complété des nouveaux.</summary>
        public static ModuleCatalog EnsureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<ModuleCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { ModuleLibrary.Dir }).OrderBy(g => AssetDatabase.GUIDToAssetPath(g)))
            {
                var m = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))?.GetComponent<RoomModule>();
                if (m != null && !catalog.modules.Contains(m))
                    catalog.modules.Add(m);
            }

            catalog.modules.RemoveAll(m => m == null);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return catalog;
        }

        public static string ValidateCatalog()
        {
            ModuleCatalog catalog = EnsureCatalog();
            var lines = new List<string>();
            foreach (RoomModule m in catalog.modules.Where(x => x != null))
                lines.Add(Validate(AssetDatabase.GetAssetPath(m)));
            return string.Join("\n", lines);
        }

        /// <summary>Empreinte stable du contenu (hiérarchie, positions, composants), sans les champs de validation.</summary>
        public static string Hash(GameObject root)
        {
            var sb = new StringBuilder();
            RoomModule m = root.GetComponent<RoomModule>();
            if (m != null)
                sb.Append($"{m.kind}|{m.size}|{m.footprint}|{m.level}|{m.hole}|{m.holeRect}|{m.holeDepth}|{m.laneHalfWidth}|{m.parkour}|{m.shape}|{m.ceilingOverride};");
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root.transform)
                    continue;
                sb.Append(t.name).Append(t.localPosition.ToString("F2")).Append(t.localEulerAngles.ToString("F1")).Append(t.localScale.ToString("F2"));
                foreach (Component c in t.GetComponents<Component>())
                    sb.Append(c != null ? c.GetType().Name[0] : '?');
                sb.Append(';');
            }

            uint h = 2166136261;
            foreach (char ch in sb.ToString())
                h = (h ^ ch) * 16777619;
            return h.ToString("x8");
        }

        /// <summary>Remet à « non validé » les modules du catalogue modifiés depuis leur validation.</summary>
        public static int InvalidateChanged()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(CatalogPath);
            int n = 0;
            if (catalog == null)
                return 0;
            foreach (RoomModule m in catalog.modules.Where(x => x != null && x.validated))
                if (Hash(m.gameObject) != m.validatedHash)
                {
                    string path = AssetDatabase.GetAssetPath(m);
                    GameObject root = PrefabUtility.LoadPrefabContents(path);
                    var rm = root.GetComponent<RoomModule>();
                    rm.validated = false;
                    rm.validationReport = "Modifié depuis la validation : à valider de nouveau.\n" + rm.validationReport;
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    PrefabUtility.UnloadPrefabContents(root);
                    n++;
                }

            return n;
        }

        /// <summary>Valide un module (chemin du prefab) ; écrit le résultat dans son RoomModule. Renvoie une ligne de résumé.</summary>
        public static string Validate(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            RoomModule module = prefab != null ? prefab.GetComponent<RoomModule>() : null;
            if (module == null)
                return $"{path} : pas de RoomModule.";
            var errors = new List<string>();
            var warnings = new List<string>();
            var info = new List<string>();

            // Format.
            Vector2Int fp = module.footprint;
            if (fp.x < 1 || fp.x > 2 || fp.y < 1 || fp.y > 2)
                errors.Add($"empreinte {fp.x} × {fp.y} : 1 ou 2 cases de chaque côté");
            if (module.size > RoomModule.GridSpacing - 8f)
                errors.Add($"côté de {module.size} m : au plus {RoomModule.GridSpacing - 8f} m (couloir de 8 m au moins)");
            if (Mathf.Abs(module.size / 4f - Mathf.Round(module.size / 4f)) > 0.01f)
                warnings.Add($"côté de {module.size} m : pas un multiple de 4 m (pièces du kit raccourcies au bord)");
            List<(DoorSide side, int cell, bool required)> doors = module.Doors();
            if (doors.Count < 2)
                errors.Add($"{doors.Count} entrée(s) : 2 au moins (chaque salle est dans une boucle)");

            Scene previous = SceneManager.GetActiveScene();
            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var offset = new Vector3(20000f, 0f, 20000f);
            var navs = new List<NavMeshDataInstance>();
            var agentTypes = new List<int>();
            try
            {
                SceneManager.SetActiveScene(temp);
                var container = new GameObject("Validation").transform;
                var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, container);
                root.transform.position = offset;
                Vector2 ext = module.Extent;
                var layoutAsset = AssetDatabase.LoadAssetAtPath<MapLayoutSettings>(TestArenaSceneBuilder.MapLayoutPath);
                float ceil = layoutAsset != null ? layoutAsset.Ceiling(module.kind, module.shape, module.ceilingOverride) : (module.kind == ModuleKind.Salle ? 18f : 16f);
                TestShell(container, offset, ext, ceil, module, doors);
                Physics.SyncTransforms();

                // Voies du traqueur (sauf fosse) : de chaque entrée au centre de sa case, et entre les centres des cases.
                if (module.hole != ModuleHole.Vide)
                {
                    float lane = module.laneHalfWidth;
                    var lanes = new List<(Vector3 a, Vector3 b)>();
                    foreach ((DoorSide side, int cell, bool _) in doors)
                    {
                        Vector3 door = module.DoorLocal(side, cell);
                        Vector3 cellCenter = NearestCellCenter(module, door);
                        lanes.Add((door, cellCenter));
                    }

                    for (int i = 0; i < fp.x; i++)
                    for (int j = 0; j < fp.y; j++)
                    {
                        Vector3 c = new Vector3(CellCoord(fp.x, i), 0f, CellCoord(fp.y, j));
                        if (i + 1 < fp.x) lanes.Add((c, new Vector3(CellCoord(fp.x, i + 1), 0f, c.z)));
                        if (j + 1 < fp.y) lanes.Add((c, new Vector3(c.x, 0f, CellCoord(fp.y, j + 1))));
                    }

                    var blocking = new HashSet<string>();
                    foreach ((Vector3 a, Vector3 b) in lanes)
                    {
                        Vector3 mid = (a + b) * 0.5f + offset + Vector3.up * 5f;
                        Vector3 d = b - a;
                        Vector3 half = Mathf.Abs(d.x) > Mathf.Abs(d.z) ? new Vector3(Mathf.Abs(d.x) * 0.5f + lane, 4.9f, lane) : new Vector3(lane, 4.9f, Mathf.Abs(d.z) * 0.5f + lane);
                        foreach (Collider c in Physics.OverlapBox(mid, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                            if (c.transform.IsChildOf(root.transform) && c.attachedRigidbody == null && c.GetComponentInParent<Grabbable>() == null && c.bounds.min.y < offset.y + 9.9f)
                                blocking.Add(c.name);
                    }

                    if (blocking.Count > 0)
                        errors.Add($"{blocking.Count} objet(s) dans les voies du traqueur (rien de solide sous 10 m) : {string.Join(", ", blocking.Take(6))}");
                }

                // NavMesh des goules : un chemin complet entre chaque paire d'entrées.
                var bounds = new Bounds(offset + Vector3.up * (ceil * 0.5f), new Vector3(ext.x + 40f, ceil + 20f, ext.y + 40f));
                NavMeshBuildSettings small = NavMesh.CreateSettings();
                small.agentRadius = 0.45f;
                small.agentHeight = 1.2f;
                small.agentClimb = 0.45f;
                small.agentSlope = 45f;
                small.overrideVoxelSize = true;
                small.voxelSize = 0.2f;
                agentTypes.Add(small.agentTypeID);
                var sources = new List<NavMeshBuildSource>();
                NavMeshBuilder.CollectSources(container, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);

                sources.RemoveAll(src => src.component is Collider c && (c.isTrigger || c.attachedRigidbody != null));
                for (int i = 0; i < sources.Count; i++)
                    if (sources[i].component != null && sources[i].component.GetComponentInParent<NotWalkable>() != null)
                    {
                        NavMeshBuildSource src = sources[i];
                        src.area = 1;
                        sources[i] = src;
                    }

                navs.Add(NavMesh.AddNavMeshData(NavMeshBuilder.BuildNavMeshData(small, sources, bounds, Vector3.zero, Quaternion.identity)));
                var filter = new NavMeshQueryFilter { agentTypeID = small.agentTypeID, areaMask = NavMesh.AllAreas };
                var navPath = new NavMeshPath();
                int bad = 0, pairs = 0;
                var points = doors.Select(d => offset + module.DoorLocal(d.side, d.cell) - SideOut(d.side) * 2f).ToList();
                for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                {
                    pairs++;
                    if (!NavMesh.SamplePosition(points[i], out NavMeshHit a, 3f, filter) || !NavMesh.SamplePosition(points[j], out NavMeshHit b, 3f, filter)
                        || !NavMesh.CalculatePath(a.position, b.position, filter, navPath) || navPath.status != NavMeshPathStatus.PathComplete)
                        bad++;
                }

                if (bad > 0)
                    errors.Add($"NavMesh : {bad} paire(s) d'entrées sur {pairs} sans chemin complet");
                else
                    info.Add($"NavMesh : {pairs} paires d'entrées reliées");

                // Cachettes : portée de la main du traqueur.
                // Portée et taille du traqueur v4 (D-080) : bras + fente, épaule voûtée, hauteur baissée.
                var stalkerCfg = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
                ReachCheck.Configure(stalkerCfg);
                navs.Add(stalkerCfg != null
                    ? ReachCheck.BuildStalkerNavMesh(container, bounds, out int stalkerAgent, stalkerCfg.width * 0.5f, stalkerCfg.crouchMinHeight)
                    : ReachCheck.BuildStalkerNavMesh(container, bounds, out stalkerAgent));
                agentTypes.Add(stalkerAgent);
                var stalker = new NavMeshQueryFilter { agentTypeID = stalkerAgent, areaMask = NavMesh.AllAreas };
                var area = new Bounds(offset + Vector3.up * (ceil * 0.5f), new Vector3(ext.x - 0.5f, ceil - 1f, ext.y - 0.5f));
                ReachCheck.Result reach = ReachCheck.Run(Physics.defaultPhysicsScene, area, 2f, stalker);
                // Zone de parcours (fosse) : le seul endroit où l'on échappe au traqueur (D-051) ; comptée à part.
                int parkourPoints = 0;
                if (module.hole == ModuleHole.Vide)
                {
                    Rect hr = module.holeRect;
                    parkourPoints = reach.Hidden.RemoveAll(p => hr.Contains(new Vector2(p.x - offset.x, p.z - offset.z)));
                    info.Add($"zone de parcours : {parkourPoints} point(s) hors de portée dans la fosse (permis)");
                }
                if (reach.Hidden.Count > 0)
                    errors.Add($"{reach.Hidden.Count} cachette(s) sur {reach.Points} points (hors de portée du traqueur), par exemple {string.Join(", ", reach.Hidden.Take(4).Select(p => (p - offset).ToString("F0")))} ; "
                               + ReachCheck.Explain(Physics.defaultPhysicsScene, reach.Hidden[0], stalker, offset));
                else
                    info.Add($"cachettes : 0 sur {reach.Points} points");

                // Pièces hors kit, contenu, triangles.
                int tris = 0;
                var outside = new List<string>();
                foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null)
                        tris += mf.sharedMesh.triangles.Length / 3;
                    GameObject inst = PrefabUtility.GetNearestPrefabInstanceRoot(r.gameObject);
                    bool kit = inst != null && inst != root && (inst.name.StartsWith("Kit_") || inst.name.StartsWith("Prop_") || inst.name.StartsWith("SM_"));
                    if (!kit && r.GetComponent<Collider>() != null)
                        outside.Add(r.name);
                }

                if (outside.Count > 0)
                    warnings.Add($"{outside.Count} pièce(s) de structure hors kit ({string.Join(", ", outside.Take(5))})");
                int spawns = root.GetComponentsInChildren<SpawnPoint>(true).Length, residents = root.GetComponentsInChildren<ResidentSpot>(true).Length;
                int chests = root.GetComponentsInChildren<ChestSpot>(true).Length;
                if (spawns < 2)
                    warnings.Add($"{spawns} point(s) d'apparition (2 au moins conseillés)");
                if (module.kind == ModuleKind.Salle && residents == 0)
                    warnings.Add("aucun emplacement d'habitant");
                if (module.kind == ModuleKind.Salle && chests == 0)
                    warnings.Add("aucun emplacement de coffre");
                info.Add($"{doors.Count} entrées, {spawns} apparitions, {residents} habitants, {chests} coffres, {tris} triangles");
            }
            finally
            {
                foreach (NavMeshDataInstance n in navs)
                    NavMesh.RemoveNavMeshData(n);
                foreach (int a in agentTypes.Distinct())
                    NavMesh.RemoveSettings(a);
                if (previous.IsValid())
                    SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(temp, true);
            }

            bool ok = errors.Count == 0;
            string report = (ok ? "VALIDÉ" : "REFUSÉ") + $" ({System.DateTime.Now:yyyy-MM-dd HH:mm})\n"
                            + string.Join("\n", errors.Select(e => "ERREUR : " + e).Concat(warnings.Select(w => "Avertissement : " + w)).Concat(info));
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var rm = contents.GetComponent<RoomModule>();
            rm.validated = ok;
            rm.validationReport = report;
            rm.validatedHash = Hash(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            return $"{module.displayName} : {report.Replace("\n", " | ")}";
        }

        private static float CellCoord(int cells, int i) => (i - (cells - 1) * 0.5f) * RoomModule.GridSpacing;

        private static Vector3 NearestCellCenter(RoomModule m, Vector3 local)
        {
            float x = CellCoord(m.footprint.x, Mathf.Clamp(Mathf.RoundToInt(local.x / RoomModule.GridSpacing + (m.footprint.x - 1) * 0.5f), 0, m.footprint.x - 1));
            float z = CellCoord(m.footprint.y, Mathf.Clamp(Mathf.RoundToInt(local.z / RoomModule.GridSpacing + (m.footprint.y - 1) * 0.5f), 0, m.footprint.y - 1));
            return new Vector3(x, 0f, z);
        }

        private static Vector3 SideOut(DoorSide s) => RoomModule.SideVector(s);

        /// <summary>Coque de test (collision seulement) : sol, plafond, murs avec toutes les entrées ouvertes (14 × 19 m).</summary>
        private static void TestShell(Transform parent, Vector3 c, Vector2 ext, float ceil, RoomModule m, List<(DoorSide side, int cell, bool required)> doors)
        {
            var shell = new GameObject("Coque de test").transform;
            shell.SetParent(parent, false);
            float hx = ext.x * 0.5f, hz = ext.y * 0.5f;
            // Même sol que le générateur (canal, fosse) : pas de dalle pleine qui ferait passer le traqueur au-dessus d'une fosse.
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutSettings>(TestArenaSceneBuilder.MapLayoutPath);
            if (layout == null)
                layout = ScriptableObject.CreateInstance<MapLayoutSettings>();
            var room = new PlanRoom { Name = m.displayName, Center = c, Extent = ext, Hole = m.hole, Ceiling = ceil, Hall = m.kind == ModuleKind.Salle };
            RandomLevel.BuildFloor(shell, room, m, layout, null, null);
            KitBuild.Box(shell, "Plafond", c + new Vector3(0f, ceil + 0.5f, 0f), new Vector3(ext.x + 2f, 1f, ext.y + 2f), KitBuild.Kind.Sol, null, walkable: false);
            foreach (DoorSide s in new[] { DoorSide.Nord, DoorSide.Est, DoorSide.Sud, DoorSide.Ouest })
            {
                bool alongX = s == DoorSide.Nord || s == DoorSide.Sud;
                float len = alongX ? ext.x + 2f : ext.y;
                var cuts = doors.Where(d => d.side == s).Select(d => alongX ? m.DoorLocal(s, d.cell).x : m.DoorLocal(s, d.cell).z).OrderBy(a => a).ToList();
                float cursor = -len * 0.5f;
                Vector3 wallC = c + SideOut(s) * ((alongX ? hz : hx) + 0.5f);
                void Seg(float a, float b, float y0, float y1)
                {
                    if (b - a < 0.01f || y1 - y0 < 0.01f)
                        return;
                    Vector3 center = wallC + (alongX ? new Vector3((a + b) * 0.5f, 0f, 0f) : new Vector3(0f, 0f, (a + b) * 0.5f)) + Vector3.up * ((y0 + y1) * 0.5f);
                    Vector3 size = alongX ? new Vector3(b - a, y1 - y0, 1f) : new Vector3(1f, y1 - y0, b - a);
                    KitBuild.Box(shell, "Mur", center, size, KitBuild.Kind.Mur, null, walkable: false);
                }

                foreach (float a in cuts)
                {
                    Seg(cursor, a - 7f, 0f, ceil);
                    Seg(a - 7f, a + 7f, 19f, ceil);
                    cursor = a + 7f;
                }

                Seg(cursor, len * 0.5f, 0f, ceil);
                // Couloir de 10 m devant chaque entrée (le NavMesh sort par là).
                foreach (float a in cuts)
                {
                    Vector3 p = wallC + (alongX ? new Vector3(a, 0f, 0f) : new Vector3(0f, 0f, a)) + SideOut(s) * 5.5f;
                    KitBuild.Box(shell, "Couloir", p + Vector3.down * 0.5f, alongX ? new Vector3(14f, 1f, 10f) : new Vector3(10f, 1f, 14f), KitBuild.Kind.Sol, null);
                }
            }
        }
    }
}
