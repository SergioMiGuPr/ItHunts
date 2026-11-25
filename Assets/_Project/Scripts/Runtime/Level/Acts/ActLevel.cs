using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Enemies;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Niveau d'un acte (D-092, nuit 3) : au chargement, avant tous les autres systèmes, tire le plan organique (ActPlan) avec la seed de
    /// la run et le numéro de l'acte, puis construit salles, portes, rampes, bassins, fosses, terrasses, décor selon la fonction de
    /// chaque salle, lumières, et pose le jeu : départ, marchands, coffres, salles verrouillées, porte de sortie payante, traqueur
    /// (repaire ou porte d'arrivée), goules habitantes dans chaque salle, points d'apparition, volumes de salle, graphe du traqueur et
    /// bornes du NavMesh. Tout est déterministe (jeu en ligne).
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed partial class ActLevel : MonoBehaviour, ILevelLoading
    {
        /// <summary>Épaisseur des murs droits (m) ; les biseaux sont plus épais (coins fermés).</summary>
        public const float T = 1f, TDiag = 1.7f;

        [SerializeField] private ActLayoutSettings settings;

        [Header("Prefabs (pré-câblés par l'éditeur)")]
        [SerializeField] private GameObject artifactMerchant;
        [SerializeField] private GameObject merchant;
        [SerializeField] private GameObject chest;
        [SerializeField] private GameObject freeChest;
        [SerializeField] private GameObject ghoul;
        [SerializeField] private GameObject flyer;
        [SerializeField] private GameObject waterSplash;

        [Header("Scène")]
        [SerializeField] private ZoneNavMesh zoneNav;
        [SerializeField] private MazeGraph graph;
        [SerializeField] private Transform playerStart;
        [SerializeField] private Transform stalker;
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private Light moon;

        [Header("Seed")]
        [Tooltip("0 : seed de la run et numéro de l'acte. Sinon cette seed (mesures).")]
        [SerializeField] private int fixedSeed;
        [Tooltip("Budget de construction par image (ms) pendant l'écran de chargement.")]
        [SerializeField] private float frameBudgetMs = 25f;

        /// <summary>Seed imposée (menu Playtest, mesures) : prioritaire si non nulle.</summary>
        public static int SeedOverride;

        public static ActLevel Current { get; private set; }
        public ActLayoutSettings Settings => settings;
        public ActPlan Plan { get; private set; }
        public int LevelSeed { get; private set; }
        public float Progress { get; private set; }
        public string Stage { get; private set; } = "";
        public string Title => settings != null ? settings.displayName : "";
        public bool Loading { get; private set; }
        public float BuildMs { get; private set; }
        public float LoadSeconds { get; private set; }
        /// <summary>Porte de sortie et vestibule d'arrivée (mesures, panneau admin).</summary>
        public ExitDoor ExitDoorObject { get; private set; }
        public Vector3 ExitPoint { get; private set; }
        public Vector3 ArrivalDoorPoint { get; private set; }
        public int GhoulsPlaced { get; private set; }

        private Transform _root, _residents;
        private System.Random _rng;
        private Vector3 _min, _max;

        private void Awake()
        {
            Current = this;
            if (gameplayRoot != null && Application.isPlaying && !gameplayRoot.activeSelf)
                StartCoroutine(LoadRoutine(ChooseSeed()));
            else
                Generate(ChooseSeed());
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
        }

        private System.Collections.IEnumerator LoadRoutine(int seed)
        {
            float t0 = Time.realtimeSinceStartup;
            Loading = true;
            var screen = new GameObject("Écran de chargement").AddComponent<LoadingScreen>();
            screen.Source = this;
            yield return null;
            System.Collections.IEnumerator build = Build(seed, true);
            while (build.MoveNext())
                yield return null;
            Stage = "Les ombres s'éveillent";
            Progress = 0.92f;
            yield return null;
            if (zoneNav != null && Plan != null && Plan.Start >= 0)
                zoneNav.StalkerOrigin = Plan.CellCenter(Plan.Rooms[Plan.Start].CenterCell, Plan.Rooms[Plan.Start].Floor);
            gameplayRoot.SetActive(true);                                     // ZoneNavMesh se construit ici, puis les systèmes
            if (_residents != null)
                _residents.gameObject.SetActive(true);
            Progress = 1f;
            Loading = false;
            LoadSeconds = Time.realtimeSinceStartup - t0;
            Debug.Log($"Acte : chargement {LoadSeconds:0.0} s (construction {BuildMs / 1000f:0.0} s)");
            Destroy(screen.gameObject);
        }

        public const string EditorSeedKey = "ItHunts.Outils.SeedForcee";

        public int ChooseSeed()
        {
            if (SeedOverride != 0)
                return SeedOverride;
            // Éditeur seulement : seed forcée par les outils de vérification (survit au rechargement du domaine de l'entrée en Play).
            if (Application.isEditor && PlayerPrefs.GetInt(EditorSeedKey, 0) != 0)
                return PlayerPrefs.GetInt(EditorSeedKey, 0);
            if (fixedSeed != 0)
                return fixedSeed;
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-ithunts-seed" && int.TryParse(args[i + 1], out int s))
                    return s;
            RunState run = FindFirstObjectByType<RunState>();
            int runSeed = 1;
            if (run != null)
            {
                run.EnsureStarted();
                runSeed = run.Seed;
            }

            return unchecked(runSeed * 73856093 ^ (RunSession.Act + 1) * 19349663);
        }

        /// <summary>Tire et construit le niveau (remplace le précédent ; mesures).</summary>
        public void Generate(int seed)
        {
            System.Collections.IEnumerator build = Build(seed, false);
            while (build.MoveNext())
            {
            }

            if (_residents != null)
                _residents.gameObject.SetActive(true);
        }

        private System.Collections.IEnumerator Build(int seed, bool async)
        {
            Stage = "Le plan se dessine";
            Progress = 0f;
            if (_root != null)
            {
                foreach (MeshFilter mf in _root.GetComponentsInChildren<MeshFilter>(true))
                    if (mf.sharedMesh != null && mf.sharedMesh.name.StartsWith("Décor fusionné"))
                        Destroy(mf.sharedMesh);
                DestroyImmediate(_root.gameObject);
            }

            var sw = Stopwatch.StartNew();
            LevelSeed = seed;
            Plan = settings.handMaps != null && settings.handMaps.Count > 0
                ? ActPlan.FromHand(settings, HandMap.Load(settings.handMaps[Mathf.Abs(seed) % settings.handMaps.Count]), seed)
                : ActPlan.Make(settings, seed);
            if (Plan.Failures.Count > 0)
                Debug.LogWarning($"Acte (seed {seed}) : {string.Join(" ; ", Plan.Failures)}");
            _rng = new System.Random(unchecked(seed ^ 0x2f6b1a37));
            _root = new GameObject($"Niveau ({settings.displayName}, seed {seed})").transform;
            _root.gameObject.SetActive(false);
            _min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            _max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            KitBuild.PieceCount = 0;
            StalkerClimbs = 0;
            _chandeliers.Clear();
            _decorOrder = 0;
            var slice = Stopwatch.StartNew();
            int n = Plan.Rooms.Count;
            for (int i = 0; i < n; i++)
            {
                BuildRoom(Plan.Rooms[i]);
                if (async && slice.Elapsed.TotalMilliseconds > frameBudgetMs)
                {
                    Stage = $"Les salles prennent forme ({i + 1} / {n})";
                    Progress = 0.05f + 0.55f * (i + 1) / n;
                    yield return null;
                    slice.Restart();
                }
            }

            for (int d = 0; d < Plan.Doors.Count; d++)
                BuildDoor(d);
            DressSky();
            Stage = "Les torches s'allument";
            Progress = 0.65f;
            if (async)
                yield return null;
            for (int i = 0; i < n; i++)
            {
                Dress(Plan.Rooms[i]);
                if (async && slice.Elapsed.TotalMilliseconds > frameBudgetMs)
                {
                    Progress = 0.65f + 0.15f * (i + 1) / n;
                    yield return null;
                    slice.Restart();
                }
            }

            if (Plan.Hand != null)
                IslandLeaps();
            _root.gameObject.SetActive(true);
            Physics.SyncTransforms();
            Stage = "Le traqueur s'éveille";
            Progress = 0.82f;
            if (async)
                yield return null;
            PlaceGameplay();
            ConfigureScene();
            Stage = "Le décor se fige";
            Progress = 0.88f;
            if (async)
                yield return null;
            (PropsSwapped, PropsKept) = PropArt.DressAll(_root);      // props en 3D de la DA (PR #51) sur les greybox
            // Revue des cartes de la DA (jour 5, D-115) : lumières sans mur, suspendus sans plafond, lustres en double, props qui se
            // chevauchent, décor traversable, formes provisoires : corrigés ici ; le test DecorAuditTests vérifie qu'il n'en reste aucun.
            DecorFixes = DecorAudit.Run(_root, true, settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial);
            Batch();
            Physics.SyncTransforms();
            BuildMs = (float)sw.Elapsed.TotalMilliseconds;
            Debug.Log($"Acte « {settings.displayName} » : seed {seed}, {Plan.Rooms.Count} salles, {Plan.Doors.Count} portes " +
                      $"({Plan.Doors.Count(x => x.Loop)} boucles), départ – sortie {Plan.StartExitDepth} salles, {GhoulsPlaced} goules, " +
                      $"{BuildMs:0} ms, {KitBuild.PieceCount} pièces du kit ; décor corrigé : {DecorFixes}");
        }

        /// <summary>Défauts du décor corrigés à la construction (lumières sans mur, doublons, chevauchements…).</summary>
        public DecorReport DecorFixes { get; private set; } = new DecorReport();

        /// <summary>Racine du niveau construit (audit du décor).</summary>
        public Transform Root => _root;

        // ---------- Outils ----------

        public Vector3 World(Vector2 u, float y) => Plan.Origin + new Vector3(u.x * Plan.Cell, y, u.y * Plan.Cell);

        private void Encapsulate(Vector3 p)
        {
            _min = Vector3.Min(_min, p);
            _max = Vector3.Max(_max, p);
        }

        private Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent != null ? parent : _root, false);
            return t;
        }

        /// <summary>Volume plein (collision + pièces du kit ou boîte simple), tourné de yaw degrés.</summary>
        private GameObject Solid(Transform parent, string name, Vector3 center, Vector3 size, float yaw, KitBuild.Kind kind, Material mat,
            bool walkable = true, bool collider = true)
        {
            GameObject go;
            if (settings.kitPieces && settings.kit != null && mat == null)
                go = KitBuild.Box(parent, name, Vector3.zero, size, kind, settings.kit, walkable, collider);
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(parent, false);
                if (collider)
                    go.AddComponent<BoxCollider>().size = size;
                if (!walkable)
                    go.AddComponent<NotWalkable>();
                var vis = new GameObject("Kit_Boite");
                vis.transform.SetParent(go.transform, false);
                vis.transform.localScale = size;
                FunctionMaterials fm = _roomMaterials;
                Material wallM = fm != null && fm.wall != null ? fm.wall : settings.wallMaterial;
                Material floorM = fm != null && fm.floor != null ? fm.floor : settings.floorMaterial;
                Material ceilM = fm != null && fm.ceiling != null ? fm.ceiling : settings.ceilingMaterial;
                Material m = mat != null ? mat : kind == KitBuild.Kind.Mur ? wallM
                    : kind == KitBuild.Kind.Sol && !walkable ? ceilM : kind == KitBuild.Kind.Pont ? settings.trimMaterial : floorM;
                m = m != null ? m : settings.wallMaterial;
                // Revue de la DA (jour 5) : un sol, une terrasse ou un îlot a ses faces de côté au matériau du mur (plus de damier ni de
                // dallage posé à la verticale sur les murs des terrasses et les contremarches).
                if (mat == null && walkable && kind != KitBuild.Kind.Mur && kind != KitBuild.Kind.Pont && wallM != null && wallM != m)
                {
                    vis.AddComponent<MeshFilter>().sharedMesh = TwoMaterialCube;
                    vis.AddComponent<MeshRenderer>().sharedMaterials = new[] { m, wallM };
                }
                else
                {
                    vis.AddComponent<MeshFilter>().sharedMesh = CubeMesh;
                    vis.AddComponent<MeshRenderer>().sharedMaterial = m;
                }
            }

            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Encapsulate(center + size * 0.5f);
            Encapsulate(center - size * 0.5f);
            return go;
        }

        private static Mesh _cube;
        public static Mesh CubeMesh
        {
            get
            {
                if (_cube == null)
                {
                    GameObject tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    _cube = tmp.GetComponent<MeshFilter>().sharedMesh;
                    Destroy(tmp);
                }

                return _cube;
            }
        }

        private static Mesh _twoMat;

        /// <summary>Cube unité à deux sous-maillages : 0 = dessus et dessous (sol), 1 = les quatre côtés (mur).</summary>
        public static Mesh TwoMaterialCube
        {
            get
            {
                if (_twoMat != null)
                    return _twoMat;
                var v = new List<Vector3>();
                var n = new List<Vector3>();
                var uv = new List<Vector2>();
                var top = new List<int>();
                var sides = new List<int>();
                void Face(Vector3 normal, Vector3 u, Vector3 w, List<int> into)
                {
                    int b = v.Count;
                    Vector3 c = normal * 0.5f;
                    v.Add(c - u * 0.5f - w * 0.5f);
                    v.Add(c + u * 0.5f - w * 0.5f);
                    v.Add(c + u * 0.5f + w * 0.5f);
                    v.Add(c - u * 0.5f + w * 0.5f);
                    for (int i = 0; i < 4; i++)
                        n.Add(normal);
                    uv.Add(new Vector2(0, 0));
                    uv.Add(new Vector2(1, 0));
                    uv.Add(new Vector2(1, 1));
                    uv.Add(new Vector2(0, 1));
                    // Sens des triangles : la normale sort de la face (u × w = normale).
                    if (Vector3.Dot(Vector3.Cross(u, w), normal) > 0f)
                        into.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                    else
                        into.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
                }

                Face(Vector3.up, Vector3.right, Vector3.forward, top);
                Face(Vector3.down, Vector3.right, Vector3.forward, top);
                Face(Vector3.right, Vector3.forward, Vector3.up, sides);
                Face(Vector3.left, Vector3.forward, Vector3.up, sides);
                Face(Vector3.forward, Vector3.right, Vector3.up, sides);
                Face(Vector3.back, Vector3.right, Vector3.up, sides);
                _twoMat = new Mesh { name = "Cube sol et murs" };
                _twoMat.SetVertices(v);
                _twoMat.SetNormals(n);
                _twoMat.SetUVs(0, uv);
                _twoMat.subMeshCount = 2;
                _twoMat.SetTriangles(top, 0);
                _twoMat.SetTriangles(sides, 1);
                _twoMat.RecalculateBounds();
                _twoMat.RecalculateTangents();
                return _twoMat;
            }
        }

        private static Mesh _cylinder, _sphere;
        public static Mesh CylinderMesh => _cylinder != null ? _cylinder : _cylinder = PrimitiveMesh(PrimitiveType.Cylinder);
        public static Mesh SphereMesh => _sphere != null ? _sphere : _sphere = PrimitiveMesh(PrimitiveType.Sphere);

        private static Mesh PrimitiveMesh(PrimitiveType t)
        {
            GameObject tmp = GameObject.CreatePrimitive(t);
            Mesh m = tmp.GetComponent<MeshFilter>().sharedMesh;
            Destroy(tmp);
            return m;
        }

        /// <summary>Visuel sans collision (pièce fusionnée au lot).</summary>
        private GameObject Visual(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Quaternion rot, Vector3 scale, bool merge = true)
        {
            var go = new GameObject(merge ? "Kit_" + name : name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private Light PointLight(Transform parent, Vector3 at, Color color, float intensity, float range)
        {
            var go = new GameObject("Lumière");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = color;
            l.shadows = LightShadows.None;
            go.AddComponent<AmbianceLight>().kind = AmbianceLight.Kind.Lampe;
            return l;
        }

        /// <summary>Greedy : rectangles qui couvrent ces cases (moins d'objets).</summary>
        public static List<RectInt> Rects(HashSet<Vector2Int> cells)
        {
            var rects = new List<RectInt>();
            var left = new HashSet<Vector2Int>(cells);
            foreach (Vector2Int c in cells.OrderBy(c => c.y).ThenBy(c => c.x))
            {
                if (!left.Contains(c))
                    continue;
                int w = 1;
                while (left.Contains(new Vector2Int(c.x + w, c.y)))
                    w++;
                int h = 1;
                bool grow = true;
                while (grow)
                {
                    for (int x = 0; x < w; x++)
                        if (!left.Contains(new Vector2Int(c.x + x, c.y + h)))
                        {
                            grow = false;
                            break;
                        }

                    if (grow)
                        h++;
                }

                for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    left.Remove(new Vector2Int(c.x + x, c.y + y));
                rects.Add(new RectInt(c.x, c.y, w, h));
            }

            return rects;
        }

        private Vector3 RectCenter(RectInt r, float y) => Plan.Origin + new Vector3((r.x + r.width * 0.5f) * Plan.Cell, y, (r.y + r.height * 0.5f) * Plan.Cell);
        private Vector3 RectSize(RectInt r, float h) => new Vector3(r.width * Plan.Cell, h, r.height * Plan.Cell);

        /// <summary>
        /// Fusion des pièces du kit (visuel seulement), par salle : un maillage par matériau (comme la carte aléatoire). Les maillages non
        /// lisibles restent tels quels.
        /// </summary>
        /// <summary>Props remplacés par un modèle de la DA, greybox gardées (forme trop différente).</summary>
        public int PropsSwapped { get; private set; }
        public int PropsKept { get; private set; }

        private void Batch()
        {
            foreach (Transform group in _root.Cast<Transform>().ToList())
            {
                var byMaterial = new Dictionary<Material, List<CombineInstance>>();
                var pieces = new List<GameObject>();
                foreach (Transform t in group.GetComponentsInChildren<Transform>(true))
                {
                    if (!t.name.StartsWith("Kit_") || (t.parent != null && t.parent.name.StartsWith("Kit_")))
                        continue;
                    bool readable = true;
                    foreach (MeshFilter f in t.GetComponentsInChildren<MeshFilter>(true))
                        readable &= f.sharedMesh == null || f.sharedMesh.isReadable;
                    if (!readable || t.GetComponentInChildren<Light>(true) != null || t.GetComponentInChildren<ParticleSystem>(true) != null)
                        continue;
                    pieces.Add(t.gameObject);
                    foreach (MeshRenderer r in t.GetComponentsInChildren<MeshRenderer>(true))
                    {
                        var mf = r.GetComponent<MeshFilter>();
                        if (mf == null || mf.sharedMesh == null || !r.enabled)
                            continue;
                        Matrix4x4 m = group.worldToLocalMatrix * r.transform.localToWorldMatrix;
                        Material[] mats = r.sharedMaterials;
                        for (int s = 0; s < mf.sharedMesh.subMeshCount && s < mats.Length; s++)
                        {
                            if (mats[s] == null)
                                continue;
                            if (!byMaterial.TryGetValue(mats[s], out List<CombineInstance> list))
                                byMaterial[mats[s]] = list = new List<CombineInstance>();
                            list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = m });
                        }
                    }
                }

                if (pieces.Count == 0)
                    continue;
                foreach (KeyValuePair<Material, List<CombineInstance>> kv in byMaterial)
                {
                    var go = new GameObject($"Décor fusionné ({kv.Key.name})");
                    go.transform.SetParent(group, false);
                    var mesh = new Mesh { name = go.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                    mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var mr = go.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = kv.Key;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

                // Les pièces visuelles disparaissent ; leurs collisions (sur le parent) restent.
                foreach (GameObject p in pieces)
                {
                    foreach (Collider c in p.GetComponents<Collider>())
                        if (c != null)
                        {
                            var keep = new GameObject(p.name.Substring(4) + " (collision)");
                            keep.transform.SetParent(p.transform.parent, false);
                            keep.transform.SetPositionAndRotation(p.transform.position, p.transform.rotation);
                            keep.transform.localScale = p.transform.localScale;
                            CopyCollider(c, keep);
                        }

                    Destroy(p);
                }
            }
        }

        private static void CopyCollider(Collider c, GameObject to)
        {
            switch (c)
            {
                case BoxCollider b:
                    BoxCollider nb = to.AddComponent<BoxCollider>();
                    nb.center = b.center;
                    nb.size = b.size;
                    break;
                case CapsuleCollider k:
                    CapsuleCollider nk = to.AddComponent<CapsuleCollider>();
                    nk.center = k.center;
                    nk.radius = k.radius;
                    nk.height = k.height;
                    nk.direction = k.direction;
                    break;
                case SphereCollider s:
                    SphereCollider ns = to.AddComponent<SphereCollider>();
                    ns.center = s.center;
                    ns.radius = s.radius;
                    break;
                case MeshCollider m:
                    MeshCollider nm = to.AddComponent<MeshCollider>();
                    nm.sharedMesh = m.sharedMesh;
                    nm.convex = m.convex;
                    break;
            }
        }
    }
}
