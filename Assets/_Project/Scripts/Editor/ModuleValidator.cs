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

        /// <summary>Repère de la dernière validation (atelier) : position locale au module, erreur ou avertissement, texte.</summary>
        public struct Marker
        {
            public Vector3 Local;
            public bool Error;
            public string Text;
        }

        /// <summary>Repères de la dernière validation (cachettes, entrées et passages fautifs, coffres hors d'atteinte…).</summary>
        public static readonly List<Marker> LastMarkers = new List<Marker>();
        /// <summary>Dernier résultat : validé ou non, et le rapport complet (atelier, tests).</summary>
        public static bool LastOk;
        public static string LastReport = "";

        /// <summary>
        /// Entrée mal placée (atelier) : un connecteur doit être à sa place (DoorLocal de son côté et de sa case, à 0.5 m près), sur la
        /// grille des couloirs (côté du module multiple de 4 m), et assez large pour le couloir. Pur, testable : liste des fautes.
        /// </summary>
        public static List<string> ConnectorProblems(RoomModule m, StalkerSettingsRef cfg = null)
        {
            var list = new List<string>();
            foreach (ModuleDoor d in m.GetComponentsInChildren<ModuleDoor>(true))
            {
                int cells = m.CellsAlong(d.side);
                Vector3 want = m.DoorLocal(d.side, Mathf.Clamp(d.cell, 0, cells - 1));
                Vector3 local = m.transform.InverseTransformPoint(d.transform.position);
                if (d.cell < 0 || d.cell >= cells)
                    list.Add($"entrée {d.side} {d.cell} : case hors du côté ({cells} case(s))");
                else if (Vector3.Distance(local, want) > 0.5f)
                    list.Add($"entrée {d.side} {d.cell} : à {Vector3.Distance(local, want):0.0} m de sa place ({want.x:0}, {want.z:0}) sur la grille des couloirs");
                float minW = cfg != null ? cfg.NarrowDoorWidth : 4f, minH = cfg != null ? cfg.NarrowDoorHeight : 5.2f;
                if (d.width < minW || d.height < minH)
                    list.Add($"entrée {d.side} {d.cell} : {d.width:0.#} × {d.height:0.#} m, plus petite que la plus petite porte du traqueur ({minW} × {minH} m)");
            }

            if (Mathf.Abs(m.size / 4f - Mathf.Round(m.size / 4f)) > 0.01f)
                list.Add($"côté de {m.size} m : pas sur la grille de 4 m des couloirs");
            return list;
        }

        /// <summary>Seuils de porte du traqueur (sans dépendre des réglages dans les tests purs).</summary>
        public sealed class StalkerSettingsRef
        {
            public float NarrowDoorWidth = 4f, NarrowDoorHeight = 5.2f;
        }

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
            LastMarkers.Clear();
            var stalkerRef = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            var doorRef = new StalkerSettingsRef();
            if (stalkerRef != null)
            {
                doorRef.NarrowDoorWidth = stalkerRef.narrowDoorWidth;
                doorRef.NarrowDoorHeight = stalkerRef.narrowDoorHeight;
            }

            // Entrées : à leur place sur la grille des couloirs, assez grandes (atelier).
            foreach (string c in ConnectorProblems(module, doorRef))
                if (!c.StartsWith("côté"))                                  // le côté hors grille de 4 m reste un avertissement (plus bas)
                    errors.Add(c);
            foreach (ModuleDoor d in module.GetComponentsInChildren<ModuleDoor>(true))
                if (Vector3.Distance(module.transform.InverseTransformPoint(d.transform.position), module.DoorLocal(d.side, Mathf.Clamp(d.cell, 0, module.CellsAlong(d.side) - 1))) > 0.5f)
                    LastMarkers.Add(new Marker { Local = module.transform.InverseTransformPoint(d.transform.position), Error = true, Text = "entrée mal placée" });

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

                LastMarkers.AddRange(reach.Hidden.Select(p => new Marker { Local = p - offset, Error = true, Text = "cachette" }));

                // NavMesh du traqueur : chemin entre chaque paire d'entrées (avec ses passages, comptés à part ci-dessous).
                int sBad = 0;
                for (int i = 0; i < points.Count; i++)
                for (int j = i + 1; j < points.Count; j++)
                    if (!NavMesh.SamplePosition(points[i], out NavMeshHit sa, 4f, stalker) || !NavMesh.SamplePosition(points[j], out NavMeshHit sb, 4f, stalker)
                        || !NavMesh.CalculatePath(sa.position, sb.position, stalker, navPath) || navPath.status != NavMeshPathStatus.PathComplete)
                        sBad++;
                if (sBad > 0)
                    warnings.Add($"NavMesh du traqueur : {sBad} paire(s) d'entrées sans chemin à pied (ses passages peuvent les relier)");
                else
                    info.Add("NavMesh du traqueur : entrées reliées");

                // Passages du traqueur : bouts posés, distance et hauteur permises, bouts sur son NavMesh.
                int passOk = 0;
                foreach (StalkerPassage ps in root.GetComponentsInChildren<StalkerPassage>(true))
                {
                    string why = null;
                    if (ps.end == null)
                        why = "sans arrivée";
                    else if (stalkerCfg != null && !StalkerPassageMath.Allowed(ps.kind, ps.RawA, ps.RawB, stalkerCfg))
                        why = $"{ps.kind} : distance ou hauteur hors limites ({Vector3.Distance(ps.RawA, ps.RawB):0} m)";
                    else if (ps.kind != StalkerLinkKind.PorteEtroite && (!NavMesh.SamplePosition(ps.RawA, out _, 4f, stalker) || !NavMesh.SamplePosition(ps.RawB, out _, 4f, stalker)))
                        why = $"{ps.kind} : un bout loin de son NavMesh (4 m)";
                    if (why == null)
                    {
                        passOk++;
                        continue;
                    }

                    errors.Add("passage du traqueur " + why);
                    LastMarkers.Add(new Marker { Local = ps.RawA - offset, Error = true, Text = "passage : " + why });
                }

                info.Add($"passages du traqueur : {passOk} valide(s)");

                // Coffres : atteignables par le joueur (à pied, ou en sautant / se hissant : 5.5 m au plus au-dessus d'un sol praticable).
                PlayerIslands islands = PlayerIslands.Build(filter, points.Count > 0 ? points[0] : offset, offset, ext, ceil);
                foreach (ChestSpot ch in root.GetComponentsInChildren<ChestSpot>(true))
                {
                    Vector3 cp = ch.transform.position;
                    bool walk = NavMesh.SamplePosition(cp, out NavMeshHit cn, 1.5f, filter) && points.Count > 0
                                && NavMesh.SamplePosition(points[0], out NavMeshHit d0, 3f, filter)
                                && NavMesh.CalculatePath(d0.position, cn.position, filter, navPath) && navPath.status == NavMeshPathStatus.PathComplete;
                    bool hop = !walk && JumpReach(cp, filter, points.Count > 0 ? points[0] : cp, navPath);
                    bool pull = !walk && !hop && (TractionReach(cp, filter, points.Count > 0 ? points[0] : cp, navPath, root.transform) || islands.Reaches(cp));
                    if (!walk && !hop && !pull)
                    {
                        errors.Add($"coffre {ch.name} hors d'atteinte du joueur ({(cp - offset).ToString("F0")})");
                        LastMarkers.Add(new Marker { Local = cp - offset, Error = true, Text = "coffre hors d'atteinte" });
                    }
                    else
                        info.Add($"coffre {ch.name} : atteignable {(walk ? "à pied" : hop ? "en sautant ou en se hissant" : "en traction (magnétisme)")}");
                }

                // Trous et sorties : un sol praticable hors de la zone du module.
                Vector2 halfExt = ext * 0.5f;
                NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                int outsideWalk = 0;
                foreach (Vector3 v in tri.vertices)
                {
                    Vector3 l = v - offset;
                    if (Mathf.Abs(l.x) > halfExt.x - 0.5f || Mathf.Abs(l.z) > halfExt.y - 0.5f)
                        continue;                                            // coque de test et couloirs : pas le module
                    if (l.y > ceil - 2f)
                    {
                        outsideWalk++;
                        LastMarkers.Add(new Marker { Local = l, Error = false, Text = "sol collé au plafond (sortie par hissage ?)" });
                    }
                }

                if (outsideWalk > 0)
                    warnings.Add($"{outsideWalk} sommet(s) de sol praticable à moins de 2 m du plafond (sortie par hissage ?)");

                // Portes posées : ouverture et qui la franchit (le traqueur à partir de {narrow}, sinon seule sa main : les cachettes le disent).
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                {
                    var m = System.Text.RegularExpressions.Regex.Match(t.name, @"GrandePorte_(\d+)x(\d+)");
                    if (!m.Success || PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) != t.gameObject)
                        continue;
                    float w = float.Parse(m.Groups[1].Value), h = float.Parse(m.Groups[2].Value);
                    bool passes = w >= doorRef.NarrowDoorWidth && h >= doorRef.NarrowDoorHeight;
                    info.Add($"porte {w:0} × {h:0} m : {(passes ? "il la franchit" : "seule sa main passe")}");
                }

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
                int lights = root.GetComponentsInChildren<Light>(true).Length;
                int pieces = root.GetComponentsInChildren<Transform>(true).Count(t => t != root.transform && PrefabUtility.GetNearestPrefabInstanceRoot(t.gameObject) == t.gameObject);
                info.Add($"{doors.Count} entrées, {spawns} apparitions, {residents} habitants, {chests} coffres");
                info.Add($"coût : {pieces} pièces, {tris} triangles, {lights} lumière(s)");
                if (lights > 24)
                    warnings.Add($"{lights} lumières : beaucoup pour une salle (24 au plus conseillées)");
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
            LastOk = ok;
            string report = (ok ? "VALIDÉ" : "REFUSÉ") + $" ({System.DateTime.Now:yyyy-MM-dd HH:mm})\n"
                            + string.Join("\n", errors.Select(e => "ERREUR : " + e).Concat(warnings.Select(w => "Avertissement : " + w)).Concat(info));
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            var rm = contents.GetComponent<RoomModule>();
            rm.validated = ok;
            rm.validationReport = report;
            rm.validatedHash = Hash(contents);
            PrefabUtility.SaveAsPrefabAsset(contents, path);
            PrefabUtility.UnloadPrefabContents(contents);
            LastReport = report;
            return $"{module.displayName} : {report.Replace("\n", " | ")}";
        }

        /// <summary>
        /// Où le joueur peut aller (vérification des coffres) : le NavMesh à sa taille découpé en îlots ; un îlot est atteint depuis
        /// l'entrée à pied, puis d'îlot en îlot en se laissant tomber, en sautant ou en se hissant (5.5 m au plus), ou en traction (40 m,
        /// ligne de vue libre). Échelles et escaliers sans NavMesh sont couverts par le hissage et la traction.
        /// </summary>
        public sealed class PlayerIslands
        {
            private readonly List<List<Vector3>> _islands = new List<List<Vector3>>();
            private readonly HashSet<int> _reached = new HashSet<int>();
            private NavMeshQueryFilter _filter;

            public int Count => _islands.Count;
            /// <summary>Bornes du calcul (îlots, points par îlot) : au plus MaxIslands² × PerIsland² rayons.</summary>
            public const int MaxIslands = 40, PerIsland = 14;
            public int Reached => _reached.Count;

            public static PlayerIslands Build(NavMeshQueryFilter filter, Vector3 door, Vector3 offset, Vector2 ext, float ceil)
            {
                var r = new PlayerIslands { _filter = filter };
                var path = new NavMeshPath();
                NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
                var reps = new List<Vector3>();
                var rnd = new System.Random(3);
                IEnumerable<Vector3> verts = tri.vertices.Where(v => Mathf.Abs(v.x - offset.x) < ext.x * 0.5f && Mathf.Abs(v.z - offset.z) < ext.y * 0.5f)
                    .OrderBy(_ => rnd.Next()).Take(400);
                foreach (Vector3 v in verts)
                {
                    if (!NavMesh.SamplePosition(v, out NavMeshHit h, 0.3f, filter))
                        continue;                                        // NavMesh d'un autre agent (traqueur)
                    int island = -1;
                    for (int i = 0; i < reps.Count && island < 0; i++)
                        if (NavMesh.CalculatePath(reps[i], h.position, filter, path) && path.status == NavMeshPathStatus.PathComplete)
                            island = i;
                    if (island < 0)
                    {
                        if (reps.Count >= MaxIslands)
                            continue;                                    // borné : le reste est compté comme hors îlot
                        reps.Add(h.position);
                        r._islands.Add(new List<Vector3>());
                        island = reps.Count - 1;
                    }

                    if (r._islands[island].Count < PerIsland)
                        r._islands[island].Add(h.position);
                }

                // Îlot de l'entrée, puis propagation.
                var queue = new Queue<int>();
                if (NavMesh.SamplePosition(door, out NavMeshHit d0, 3f, filter))
                    for (int i = 0; i < reps.Count; i++)
                        if (NavMesh.CalculatePath(d0.position, reps[i], filter, path) && path.status == NavMeshPathStatus.PathComplete && r._reached.Add(i))
                            queue.Enqueue(i);
                while (queue.Count > 0)
                {
                    int a = queue.Dequeue();
                    for (int b = 0; b < r._islands.Count; b++)
                        if (!r._reached.Contains(b) && Linked(r._islands[a], r._islands[b]) && r._reached.Add(b))
                            queue.Enqueue(b);
                }

                return r;
            }

            private static bool Linked(List<Vector3> from, List<Vector3> to)
            {
                foreach (Vector3 a in from)
                    foreach (Vector3 b in to)
                    {
                        float flat = new Vector2(b.x - a.x, b.z - a.z).magnitude, dy = b.y - a.y;
                        if (dy <= 5.5f && flat <= 4f)
                            return true;                                 // saut, hissage, chute
                        if (dy < 0f && flat <= 8f)
                            return true;                                 // se laisser tomber
                        Vector3 eye = a + Vector3.up * 1.7f, tgt = b + Vector3.up * 0.3f, d = tgt - eye;
                        if (d.magnitude <= 40f && !Physics.Raycast(eye, d.normalized, d.magnitude - 0.5f, ~0, QueryTriggerInteraction.Ignore))
                            return true;                                 // traction
                    }

                return false;
            }

            /// <summary>Le joueur atteint-il ce point (un sol à 2.5 m au plus en dessous, dans un îlot atteint) ?</summary>
            public bool Reaches(Vector3 p)
            {
                if (!NavMesh.SamplePosition(p, out NavMeshHit h, 2.5f, _filter))
                    return false;
                for (int i = 0; i < _islands.Count; i++)
                    if (_reached.Contains(i) && _islands[i].Any(v => Vector3.Distance(v, h.position) < 12f && Mathf.Abs(v.y - h.position.y) < 1f))
                        return true;
                return false;
            }
        }

        /// <summary>
        /// Coffre atteignable en traction (magnétisme, 60 m) : un sol relié à l'entrée voit le coffre (ligne de vue libre) à 40 m au
        /// plus ; le joueur s'y tracte puis se pose.
        /// </summary>
        private static bool TractionReach(Vector3 chest, NavMeshQueryFilter filter, Vector3 door, NavMeshPath path, Transform module)
        {
            if (!NavMesh.SamplePosition(door, out NavMeshHit d0, 3f, filter))
                return false;
            Vector3 target = chest + Vector3.up * 0.5f;
            for (int a = 0; a < 24; a++)
            for (float r = 4f; r <= 36f; r += 8f)
            {
                Vector3 probe = chest + Quaternion.Euler(0f, a * 15f, 0f) * Vector3.forward * r;
                probe.y = chest.y;
                if (!NavMesh.SamplePosition(probe, out NavMeshHit g, 20f, filter))
                    continue;
                Vector3 eye = g.position + Vector3.up * 1.7f;
                Vector3 d = target - eye;
                if (d.magnitude > 40f || Physics.Raycast(eye, d.normalized, d.magnitude - 0.8f, ~0, QueryTriggerInteraction.Ignore))
                    continue;
                if (NavMesh.CalculatePath(d0.position, g.position, filter, path) && path.status == NavMeshPathStatus.PathComplete)
                    return true;
            }

            return false;
        }

        /// <summary>Coffre atteignable en sautant ou en se hissant : un sol relié à l'entrée, à 4 m à plat au plus et 5.5 m plus bas au plus.</summary>
        private static bool JumpReach(Vector3 chest, NavMeshQueryFilter filter, Vector3 door, NavMeshPath path)
        {
            if (!NavMesh.SamplePosition(door, out NavMeshHit d0, 3f, filter))
                return false;
            for (int a = 0; a < 16; a++)
            for (float r = 1f; r <= 4f; r += 1.5f)
            {
                Vector3 probe = chest + Quaternion.Euler(0f, a * 22.5f, 0f) * Vector3.forward * r;
                if (!NavMesh.SamplePosition(probe, out NavMeshHit g, 6f, filter) || chest.y - g.position.y > 5.5f || g.position.y - chest.y > 1f)
                    continue;
                if (NavMesh.CalculatePath(d0.position, g.position, filter, path) && path.status == NavMeshPathStatus.PathComplete)
                    return true;
            }

            return false;
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
