using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Orbs;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Enemies
{
    /// <summary>
    /// NavMesh de la zone, construit au lancement à partir de la géométrie fixe de la scène (D-033) : un pour les ennemis
    /// de taille humaine (goule : passe les petites portes de 1.2 m), un pour le traqueur (rayon 1.25 m : il ne les passe
    /// pas et fait le tour ; il emprunte les raccourcis qui lui sont réservés). Les objets mobiles (caisses, ennemis,
    /// joueurs) et les déclencheurs ne comptent pas.
    /// </summary>
    public sealed class ZoneNavMesh : MonoBehaviour
    {
        [Tooltip("Taille de la zone couverte (m), centrée sur cet objet.")]
        [SerializeField] private Vector3 size = new Vector3(160f, 40f, 160f);
        [SerializeField] private LayerMask layers = ~0;
        [Header("Ennemis de taille humaine (goule)")]
        [SerializeField, Min(0.1f)] private float smallRadius = 0.45f;
        [SerializeField, Min(0.1f)] private float smallHeight = 1.2f;
        [Header("Traqueur")]
        [SerializeField, Min(0.1f)] private float stalkerRadius = 1.25f;
        [Tooltip("Hauteur de passage du traqueur (m) : sa hauteur baissé (D-035), pas sa taille debout.")]
        [SerializeField, Min(0.1f)] private float stalkerHeight = 3.5f;
        [SerializeField, Min(0f)] private float stepHeight = 0.45f;
        [Tooltip("Taille des voxels (m) : plus petits, plus précis mais plus longs à construire (grandes arènes).")]
        [SerializeField, Min(0.05f)] private float smallVoxel = 0.1f;
        [Tooltip("Surface minimale d'une région du NavMesh des goules (m²) : 1 garde les plateaux d'échafaudage.")]
        [SerializeField, Min(0f)] private float smallMinRegionArea = 1f;
        [SerializeField, Min(0.05f)] private float stalkerVoxel = 0.6f;
        [SerializeField, Range(0f, 60f)] private float maxSlope = 45f;
        [Tooltip("Taille des tuiles (voxels). Plus grandes : moins de polygones, donc des chemins longs qui aboutissent (le calcul " +
                 "de chemin a un nombre de nœuds limité).")]
        [SerializeField, Range(32, 1024)] private int tileSize = 128;
        [Header("Murs que les goules grimpent (D-038), créés automatiquement")]
        [Tooltip("Liens d'escalade sur tous les murs verticaux qui mènent à un sol praticable (corniche, pont, niveau haut), " +
                 "sans placement à la main.")]
        [SerializeField] private bool autoClimbLinks = true;
        [Tooltip("Hauteur maximale d'un mur grimpable (m).")]
        [SerializeField, Min(1f)] private float climbMaxHeight = 20f;
        [Tooltip("Hauteur minimale (m) : en dessous, une marche ou un muret, pas un mur.")]
        [SerializeField, Min(0.5f)] private float climbMinHeight = 2f;
        [Tooltip("Pas de la recherche (m) et écart minimal entre deux liens sur un même mur (m).")]
        [SerializeField, Min(0.5f)] private float climbScanStep = 2f;
        [SerializeField, Min(1f)] private float climbSpacing = 5f;

        private readonly List<NavMeshDataInstance> _instances = new List<NavMeshDataInstance>();
        private readonly List<int> _settings = new List<int>();
        private readonly List<NavMeshLinkInstance> _links = new List<NavMeshLinkInstance>();

        /// <summary>Liens de mur ajoutés pour les goules.</summary>
        public int ClimbLinkCount => _links.Count;
        /// <summary>Maillages non lisibles remplacés par leur boîte (mesures).</summary>
        public int UnreadableMeshes { get; private set; }

        public int SmallAgent { get; private set; } = EnemyLocomotion.NoAgent;
        public int StalkerAgent { get; private set; } = EnemyLocomotion.NoAgent;
        public bool Built { get; private set; }

        private void Awake() => Build();

        /// <summary>
        /// Bornes de la zone (carte aléatoire : posées par le générateur avant le Awake, la construction se fait ensuite sur la
        /// carte générée).
        /// </summary>
        public void Configure(Vector3 center, Vector3 boundsSize)
        {
            transform.position = center;
            size = boundsSize;
        }

        private void OnDestroy() => Clear();

        public void Build()
        {
            Clear();
            Physics.SyncTransforms();                                       // colliders posés dans la même image (carte générée)
            var bounds = new Bounds(transform.position, size);
            var all = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, layers, NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), all);
            var common = new List<NavMeshBuildSource>();
            var stalker = new List<NavMeshBuildSource>();
            foreach (NavMeshBuildSource s in all)
            {
                Component c = s.component;
                if (c == null || IsMoving(c) || c.GetComponentInParent<StalkerPassesThrough>() != null)
                    continue;                                           // objets qui bougent ; anneau des arcs (joueurs seulement)
                NavMeshBuildSource src = s;
                if (src.shape == NavMeshBuildSourceShape.Mesh && src.sourceObject is Mesh mesh && !mesh.isReadable)
                {
                    // Maillage du kit sans accès en lecture (« Source mesh … does not allow read access », casserait le build, nuit 4) :
                    // sa boîte englobante remplace le maillage (obstacle ou sol approché).
                    Collider col = c as Collider;
                    if (col == null)
                        continue;
                    Bounds b = col.bounds;
                    src.shape = NavMeshBuildSourceShape.Box;
                    src.sourceObject = null;
                    src.size = b.size;
                    src.transform = Matrix4x4.TRS(b.center, Quaternion.identity, Vector3.one);
                    UnreadableMeshes++;
                }

                if (c.GetComponentInParent<NotWalkable>() != null)
                    src.area = 1;                                       // plafonds, toits, murs : obstacle, jamais un sol
                // Porte d'une salle verrouillée : hors du NavMesh des goules (il n'est pas refait à l'ouverture ; fermée, la porte
                // bloque par sa collision et la salle est un cul-de-sac où aucun joueur ne se trouve encore).
                if (c.GetComponentInParent<ItHunts.Economy.LockedDoor>() == null)
                    common.Add(src);
                if (c.GetComponentInParent<StalkerShortcut>() == null)
                    stalker.Add(src);
            }

            // Raccourcis : leur boîte devient un sol praticable pour le traqueur seulement.
            foreach (StalkerShortcut sc in FindObjectsByType<StalkerShortcut>(FindObjectsSortMode.None))
            {
                var box = sc.GetComponent<BoxCollider>();
                if (box == null)
                    continue;
                Vector3 worldSize = Vector3.Scale(box.size, sc.transform.lossyScale);
                stalker.Add(new NavMeshBuildSource
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(worldSize.x, 0.1f, worldSize.z),
                    transform = Matrix4x4.TRS(sc.transform.TransformPoint(box.center) + Vector3.down * (worldSize.y * 0.5f),
                        sc.transform.rotation, Vector3.one),
                    area = 0,
                });
            }

            // Seuils des portes des pièces (D-035) : sous l'épaisseur du mur, entre le sol de la pièce et celui du couloir, il n'y
            // a pas de sol ; le joueur l'enjambe, le NavMesh des goules y était coupé (playtest des étapes 8 et 9).
            ThresholdCount = 0;
            foreach (ReachRoom room in FindObjectsByType<ReachRoom>(FindObjectsSortMode.None))
                foreach (Transform o in room.Openings)
                    if (o != null && Threshold(room, o, out NavMeshBuildSource sill))
                    {
                        common.Add(sill);
                        ThresholdCount++;
                    }

            var watch = System.Diagnostics.Stopwatch.StartNew();
            SmallAgent = Bake(common, bounds, smallRadius, smallHeight, smallVoxel);
            StalkerAgent = Bake(stalker, bounds, stalkerRadius, stalkerHeight, stalkerVoxel);
            BakeMilliseconds = watch.ElapsedMilliseconds;
            // Murs que les goules grimpent : liens posés à la main (ClimbLink) et liens trouvés automatiquement (D-038) ;
            // pour les ennemis de taille humaine seulement.
            if (SmallAgent != EnemyLocomotion.NoAgent)
            {
                foreach (ClimbLink link in FindObjectsByType<ClimbLink>(FindObjectsSortMode.None))
                    AddClimb(link.Bottom, link.Top, link.Width);
                var auto = System.Diagnostics.Stopwatch.StartNew();
                AutoClimbCount = autoClimbLinks ? FindClimbs(bounds) : 0;
                AutoClimbMilliseconds = auto.ElapsedMilliseconds;
            }

            // Passages du traqueur posés par les modules (point 9) : liens typés, coût = temps de traversée.
            StalkerLinkCount = 0;
            StalkerLinksRejected = 0;
            NarrowDoorsTooSmall = 0;
            if (stalkerSettings == null)
            {
                var st = FindFirstObjectByType<Stalker>(FindObjectsInactive.Include);
                if (st != null)
                    stalkerSettings = st.Settings;
            }

            if (StalkerAgent != EnemyLocomotion.NoAgent && stalkerSettings != null && stalkerSettings.passagesEnabled)
                foreach (StalkerPassage p in FindObjectsByType<StalkerPassage>(FindObjectsSortMode.None))
                {
                    p.Linked = false;
                    Vector3 rawA = p.RawA, rawB = p.RawB;
                    if (p.kind == StalkerLinkKind.PorteEtroite)
                    {
                        // Porte étroite (D-080) : un lien la traverse si elle est au moins aussi grande que sa plus petite porte.
                        if (!StalkerPassageMath.DoorPassable(p.doorHalfSize, stalkerSettings))
                        {
                            NarrowDoorsTooSmall++;
                            continue;
                        }

                        StalkerPassageMath.DoorEnds(p.transform, p.doorHalfSize, stalkerRadius, out rawA, out rawB);
                    }

                    // Bouts recalés sur le NavMesh du traqueur (4 m au plus) : un lit, un garde-corps ou le bord érodé décalent le sol
                    // praticable ; sans sol proche, pas de lien.
                    var filter = new NavMeshQueryFilter { agentTypeID = StalkerAgent, areaMask = NavMesh.AllAreas };
                    // Escalades (D-110) : sans sol au pied, on recule jusqu'à 12 m dans l'axe (fosse, escalier, abords d'un mur).
                    if (StalkerPassageMath.IsClimb(p.kind) && !NavMesh.SamplePosition(rawA, out _, 4f, filter))
                    {
                        // Le plus proche sol praticable autour du pied (cercles de 4 à 12 m), à la même hauteur.
                        float bestD = float.PositiveInfinity;
                        Vector3 best = rawA;
                        for (float k = 4f; k <= 12f; k += 4f)
                        for (int a = 0; a < 12; a++)
                        {
                            Vector3 probe = rawA + Quaternion.Euler(0f, a * 30f, 0f) * Vector3.forward * k;
                            if (NavMesh.SamplePosition(probe, out NavMeshHit far, 2.5f, filter) && Mathf.Abs(far.position.y - rawA.y) < 1.5f)
                            {
                                float dd = Vector3.Distance(far.position, rawA);
                                if (dd < bestD)
                                {
                                    bestD = dd;
                                    best = far.position;
                                }
                            }
                        }

                        rawA = best;
                    }

                    if (!NavMesh.SamplePosition(rawA, out NavMeshHit ha, 4f, filter) || !NavMesh.SamplePosition(rawB, out NavMeshHit hb, 4f, filter)
                        || Mathf.Abs(ha.position.y - rawA.y) > 1.5f || Mathf.Abs(hb.position.y - rawB.y) > 1.5f
                        || !StalkerPassageMath.Allowed(p.kind, ha.position, hb.position, stalkerSettings))
                    {
                        StalkerLinksRejected++;
                        continue;
                    }

                    NavMeshLinkInstance li = NavMesh.AddLink(new NavMeshLinkData
                    {
                        startPosition = ha.position,
                        endPosition = hb.position,
                        width = p.width,
                        costModifier = StalkerPassageMath.CostModifier(p.kind, ha.position, hb.position, stalkerSettings),
                        bidirectional = true,
                        area = 0,
                        agentTypeID = StalkerAgent,
                    });
                    if (li.valid)
                    {
                        _links.Add(li);
                        StalkerLinkCount++;
                        p.LinkA = ha.position;
                        p.LinkB = hb.position;
                        p.Linked = true;
                    }
                }

            if (StalkerAgent != EnemyLocomotion.NoAgent && stalkerSettings != null && stalkerSettings.passagesEnabled && bridgeStalkerIslands
                && StalkerOrigin.HasValue)
            {
                var islands = System.Diagnostics.Stopwatch.StartNew();
                BridgeStalkerIslands(bounds, StalkerOrigin.Value);
                IslandMilliseconds = islands.ElapsedMilliseconds;
            }

            BuildMilliseconds = watch.ElapsedMilliseconds;
            Built = true;
        }

        [Tooltip("Jour 5 : chaque îlot du NavMesh du traqueur sans chemin vers le départ (poche entre colonnes, galerie sans escalade, " +
                 "îlot de bassin) reçoit un bond ou une escalade vers le reste, pour qu'il atteigne toute zone accessible au joueur.")]
        [SerializeField] private bool bridgeStalkerIslands = true;
        [Tooltip("Pas de la grille d'échantillonnage des îlots (m).")]
        [SerializeField, Min(2f)] private float islandStep = 3f;

        /// <summary>Point de départ du traqueur pour les îlots (posé par ActLevel avant la construction) ; vide : pas de pontage.</summary>
        public Vector3? StalkerOrigin { get; set; }
        /// <summary>Îlots reliés et îlots restés seuls (dernière construction).</summary>
        public int IslandBridges { get; private set; }
        public int IslandsLeft { get; private set; }
        public long IslandMilliseconds { get; private set; }
        private readonly List<GameObject> _islandPassages = new List<GameObject>();

        /// <summary>
        /// Le traqueur peut-il passer de low à high (revue indépendante, jour 5) ? Bond : rien de solide entre les deux à 2 m et à 5 m
        /// au-dessus des bouts. Escalade : un mur entre les deux (rayon horizontal à 1 m du bas), rien au-dessus du pied jusqu'au haut,
        /// puis le passage du bord vers le haut libre à 2 m au-dessus.
        /// </summary>
        private bool BridgeClear(StalkerLinkKind kind, Vector3 low, Vector3 high)
        {
            bool Blocked(Vector3 a, Vector3 b) => Physics.Linecast(a, b, out RaycastHit hit, layers, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<StalkerPassesThrough>() == null && !IsMoving(hit.collider);
            if (kind != StalkerLinkKind.MurGrimpe)
                return !Blocked(low + Vector3.up * 2f, high + Vector3.up * 2f) && !Blocked(low + Vector3.up * 5f, high + Vector3.up * 5f);
            Vector3 foot = low + Vector3.up;
            Vector3 across = new Vector3(high.x, foot.y, high.z);
            if (!Blocked(foot, across))
                return false;                                           // pas de mur à grimper
            Vector3 top = new Vector3(low.x, high.y + 2f, low.z);
            return !Blocked(foot, top) && !Blocked(top, high + Vector3.up * 2f);
        }

        /// <summary>
        /// Îlots du NavMesh du traqueur (jour 5) : une grille de points (tous les étages, rayons vers le bas) ; chaque point sans chemin
        /// vers le départ (chemin calculé depuis le point : la recherche s'arrête vite dans un petit îlot) est regroupé avec les points
        /// qu'il rejoint ; pour chaque îlot, la paire la plus courte (îlot, reste) qu'un bond ou une escalade permet devient un passage
        /// typé (StalkerPassage, que Stalker parcourt comme ceux des modules).
        /// </summary>
        private void BridgeStalkerIslands(Bounds bounds, Vector3 origin)
        {
            IslandBridges = 0;
            IslandsLeft = 0;
            var filter = new NavMeshQueryFilter { agentTypeID = StalkerAgent, areaMask = NavMesh.AllAreas };
            if (!NavMesh.SamplePosition(origin, out NavMeshHit o, 30f, filter))
                return;
            var pts = new List<Vector3>();
            var seen = new HashSet<Vector3Int>();
            var hits = new RaycastHit[24];
            float top = bounds.max.y;
            for (float x = bounds.min.x; x <= bounds.max.x; x += islandStep)
            for (float z = bounds.min.z; z <= bounds.max.z; z += islandStep)
            {
                int n = Physics.RaycastNonAlloc(new Vector3(x, top, z), Vector3.down, hits, bounds.size.y, layers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++)
                {
                    if (!NavMesh.SamplePosition(hits[i].point, out NavMeshHit h, 1.5f, filter) || Mathf.Abs(h.position.y - hits[i].point.y) > 1.5f)
                        continue;
                    var key = Vector3Int.RoundToInt(h.position / 2f);
                    if (seen.Add(key))
                        pts.Add(h.position);
                }
            }

            // Composantes : voisins de grille reliés en ligne droite sur le NavMesh (requête locale), puis un chemin par composante.
            int count = pts.Count;
            var parent = new int[count];
            for (int i = 0; i < count; i++)
                parent[i] = i;
            int Root(int i)
            {
                while (parent[i] != i)
                    i = parent[i] = parent[parent[i]];
                return i;
            }

            var cells = new Dictionary<Vector2Int, List<int>>();
            for (int i = 0; i < count; i++)
            {
                var cell = new Vector2Int(Mathf.FloorToInt(pts[i].x / islandStep), Mathf.FloorToInt(pts[i].z / islandStep));
                if (!cells.TryGetValue(cell, out List<int> l))
                    cells[cell] = l = new List<int>();
                l.Add(i);
            }

            foreach (KeyValuePair<Vector2Int, List<int>> kv in cells)
                foreach (int i in kv.Value)
                    for (int dx = 0; dx <= 1; dx++)
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        if (dx == 0 && dz < 0)
                            continue;
                        if (!cells.TryGetValue(kv.Key + new Vector2Int(dx, dz), out List<int> other))
                            continue;
                        foreach (int j in other)
                            if (j != i && Mathf.Abs(pts[j].y - pts[i].y) < 2f && Root(i) != Root(j) && !NavMesh.Raycast(pts[i], pts[j], out _, filter))
                                parent[Root(i)] = Root(j);
                    }

            var comps = new Dictionary<int, List<Vector3>>();
            for (int i = 0; i < count; i++)
            {
                int r = Root(i);
                if (!comps.TryGetValue(r, out List<Vector3> l))
                    comps[r] = l = new List<Vector3>();
                l.Add(pts[i]);
            }

            var path = new NavMeshPath();
            var main = new List<Vector3>();
            var groups = new List<List<Vector3>>();
            foreach (List<Vector3> comp in comps.Values)
            {
                if (NavMesh.CalculatePath(comp[0], o.position, filter, path) && path.status == NavMeshPathStatus.PathComplete)
                    main.AddRange(comp);
                else
                    groups.Add(comp);
            }

            foreach (List<Vector3> g in groups)
            {
                // Paire la plus courte que le traqueur sait franchir : bond (montée ≤ leapMaxRise), sinon escalade d'un mur.
                float best = float.PositiveInfinity;
                Vector3 a = default, b = default;
                StalkerLinkKind kind = StalkerLinkKind.Bond;
                foreach (Vector3 u in g)
                foreach (Vector3 v in main)
                {
                    float d = (u - v).sqrMagnitude;
                    if (d >= best)
                        continue;
                    StalkerLinkKind k = Mathf.Abs(u.y - v.y) <= stalkerSettings.leapMaxRise ? StalkerLinkKind.Bond : StalkerLinkKind.MurGrimpe;
                    Vector3 low = u.y < v.y ? u : v, high = u.y < v.y ? v : u;
                    if (!StalkerPassageMath.Allowed(k, low, high, stalkerSettings) || !BridgeClear(k, low, high))
                        continue;
                    best = d;
                    a = low;
                    b = high;
                    kind = k;
                }

                if (float.IsInfinity(best))
                {
                    IslandsLeft++;
                    continue;
                }

                var go = new GameObject("Passage du traqueur (îlot)");
                go.transform.SetParent(transform, false);
                go.transform.position = a;
                var end = new GameObject("Bout").transform;
                end.SetParent(go.transform, false);
                end.position = b;
                var pass = go.AddComponent<StalkerPassage>();
                pass.kind = kind;
                pass.end = end;
                pass.width = 4f;
                NavMeshLinkInstance li = NavMesh.AddLink(new NavMeshLinkData
                {
                    startPosition = a,
                    endPosition = b,
                    width = pass.width,
                    costModifier = StalkerPassageMath.CostModifier(kind, a, b, stalkerSettings),
                    bidirectional = true,
                    area = 0,
                    agentTypeID = StalkerAgent,
                });
                if (!li.valid)
                {
                    IslandsLeft++;
                    Destroy(go);
                    continue;
                }

                _links.Add(li);
                _islandPassages.Add(go);
                pass.Linked = true;
                pass.LinkA = a;
                pass.LinkB = b;
                StalkerLinkCount++;
                IslandBridges++;
            }
        }

        [Tooltip("Réglages du traqueur (passages : distances maximales, coût des liens).")]
        [SerializeField] private ItHunts.Settings.StalkerSettings stalkerSettings;

        /// <summary>Liens des passages du traqueur ajoutés (dernière construction).</summary>
        public int StalkerLinkCount { get; private set; }
        /// <summary>Portes étroites plus petites que sa plus petite porte (4 × 6 m) : pas de lien, seul son bras passe.</summary>
        public int NarrowDoorsTooSmall { get; private set; }
        /// <summary>Passages sans lien (pas de sol praticable du traqueur à moins de 4 m d'un bout, ou trop long).</summary>
        public int StalkerLinksRejected { get; private set; }

        /// <summary>Seuils de porte ajoutés au NavMesh des goules (dernière construction).</summary>
        public int ThresholdCount { get; private set; }

        /// <summary>
        /// Seuil d'une ouverture de pièce au ras du sol (une porte, pas une fenêtre) : une dalle de 2.4 × 3.2 m sous l'ouverture,
        /// au niveau du sol de la pièce, pour le NavMesh des goules seulement.
        /// </summary>
        public static bool Threshold(ReachRoom room, Transform opening, out NavMeshBuildSource sill)
        {
            sill = default;
            float floorY = room.transform.position.y - room.Size.y * 0.5f;
            if (opening.position.y - floorY > 2.5f)
                return false;                                           // fenêtre
            Quaternion flat = Quaternion.LookRotation(Vector3.ProjectOnPlane(opening.forward, Vector3.up).normalized, Vector3.up);
            sill = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(2.4f, 0.1f, 3.2f),
                transform = Matrix4x4.TRS(new Vector3(opening.position.x, floorY - 0.05f, opening.position.z), flat, Vector3.one),
                area = 0,
            };
            return true;
        }

        /// <summary>Durée de la dernière construction (ms), dont la cuisson des NavMesh et la recherche des murs grimpables.</summary>
        public long BuildMilliseconds { get; private set; }
        public long BakeMilliseconds { get; private set; }
        public long AutoClimbMilliseconds { get; private set; }
        public int AutoClimbCount { get; private set; }

        /// <summary>Liens d'escalade trouvés automatiquement (bas, haut), pour le debug et les mesures.</summary>
        public readonly List<(Vector3 bottom, Vector3 top)> AutoClimbs = new List<(Vector3, Vector3)>();

        private void AddClimb(Vector3 bottom, Vector3 top, float width)
        {
            NavMeshLinkInstance li = NavMesh.AddLink(new NavMeshLinkData
            {
                startPosition = bottom,
                endPosition = top,
                width = width,
                costModifier = -1f,
                bidirectional = true,
                area = ClimbArea,                                       // zone « Jump » : le bot de playtest (un joueur) ne grimpe pas à pied
                agentTypeID = SmallAgent,
            });
            if (li.valid)
                _links.Add(li);
        }

        /// <summary>Zone des liens d'escalade des goules (2 : « Jump », zone intégrée d'Unity) ; même coût que la marche.</summary>
        public const int ClimbArea = 2;

        private static bool StaticSolid(Collider c) => c != null && !IsMoving(c);

        private readonly RaycastHit[] _topHits = new RaycastHit[16];

        private sealed class HitOrder : IComparer<RaycastHit>
        {
            public static readonly HitOrder Instance = new HitOrder();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }

        /// <summary>
        /// Cherche les murs grimpables : depuis chaque point du NavMesh des goules (grille de climbScanStep, tous les étages),
        /// un mur vertical tout proche dans une des quatre directions, continu sur sa hauteur, avec en haut (au-delà du mur
        /// ou contre lui, comme un pont accroché) un sol praticable du même NavMesh entre climbMinHeight et climbMaxHeight
        /// plus haut. Un lien au plus par case de climbSpacing et par direction.
        /// </summary>
        private int FindClimbs(Bounds bounds)
        {
            AutoClimbs.Clear();
            var filter = new NavMeshQueryFilter { agentTypeID = SmallAgent, areaMask = NavMesh.AllAreas };
            var used = new HashSet<(int, int, int, int)>();
            var hits = new RaycastHit[16];
            Vector3[] dirs = { Vector3.forward, Vector3.back, Vector3.right, Vector3.left };
            int count = 0;
            float top = bounds.max.y;
            for (float x = bounds.min.x; x <= bounds.max.x; x += climbScanStep)
            for (float z = bounds.min.z; z <= bounds.max.z; z += climbScanStep)
            {
                int n = Physics.RaycastNonAlloc(new Vector3(x, top, z), Vector3.down, hits, bounds.size.y, layers, QueryTriggerInteraction.Ignore);
                for (int h = 0; h < n; h++)
                {
                    RaycastHit floor = hits[h];
                    if (floor.normal.y < 0.7f || !StaticSolid(floor.collider))
                        continue;
                    if (!NavMesh.SamplePosition(floor.point, out NavMeshHit onMesh, 0.4f, filter))
                        continue;
                    Vector3 basePos = onMesh.position;
                    for (int d = 0; d < 4; d++)
                    {
                        Vector3 dir = dirs[d];
                        if (!Physics.Raycast(basePos + Vector3.up * 0.6f, dir, out RaycastHit wall, climbScanStep * 0.75f + 0.6f, layers, QueryTriggerInteraction.Ignore)
                            || Mathf.Abs(wall.normal.y) > 0.2f || !StaticSolid(wall.collider))
                            continue;
                        var key = (Mathf.FloorToInt(wall.point.x / climbSpacing), Mathf.FloorToInt(wall.point.y / climbSpacing),
                            Mathf.FloorToInt(wall.point.z / climbSpacing), d);
                        if (used.Contains(key))
                            continue;
                        if (!TryTop(basePos, wall, filter, out Vector3 topPos))
                            continue;
                        used.Add(key);
                        Vector3 bottom = wall.point + wall.normal * 0.6f;
                        bottom.y = basePos.y;
                        if (!NavMesh.SamplePosition(bottom, out NavMeshHit b, 0.8f, filter))
                            continue;
                        AddClimb(b.position, topPos, 1.5f);
                        AutoClimbs.Add((b.position, topPos));
                        count++;
                    }
                }
            }

            return count;
        }

        /// <summary>Sol praticable en haut du mur : derrière sa face (corniche, niveau haut) ou devant, accroché au mur (pont).</summary>
        private bool TryTop(Vector3 basePos, RaycastHit wall, NavMeshQueryFilter filter, out Vector3 topPos)
        {
            topPos = default;
            Vector3 inward = -wall.normal;
            inward.y = 0f;
            inward.Normalize();
            float[] offsets = { 1.2f, 2.2f, 3.5f, -0.6f, -1.4f };
            foreach (float off in offsets)
            {
                Vector3 probe = new Vector3(wall.point.x, basePos.y + climbMaxHeight + 0.5f, wall.point.z) + inward * off;
                // Toutes les surfaces sous le point (un plafond peut cacher un pont) : la plus haute qui soit un sol praticable.
                int n = Physics.RaycastNonAlloc(probe, Vector3.down, _topHits, climbMaxHeight + 0.5f - climbMinHeight, layers, QueryTriggerInteraction.Ignore);
                System.Array.Sort(_topHits, 0, n, HitOrder.Instance);
                bool found = false;
                RaycastHit surf = default;
                NavMeshHit t = default;
                for (int i = 0; i < n && !found; i++)
                {
                    surf = _topHits[i];
                    float r = surf.point.y - basePos.y;
                    found = r >= climbMinHeight && r <= climbMaxHeight && surf.normal.y >= 0.7f && StaticSolid(surf.collider)
                            && NavMesh.SamplePosition(surf.point, out t, 0.6f, filter) && Mathf.Abs(t.position.y - surf.point.y) <= 0.5f;
                }

                if (!found)
                    continue;
                float rise = surf.point.y - basePos.y;
                // Le mur doit monter d'un seul tenant jusque-là (pas un trou au milieu).
                bool continuous = true;
                for (float y = 1.5f; y < rise - 0.5f; y += 2f)
                    if (!Physics.Raycast(basePos + Vector3.up * y + wall.normal * 0.3f, inward, 2.5f, layers, QueryTriggerInteraction.Ignore))
                    {
                        continuous = false;
                        break;
                    }

                if (!continuous)
                    continue;
                topPos = t.position;
                return true;
            }

            return false;
        }

        private int Bake(List<NavMeshBuildSource> sources, Bounds bounds, float radius, float height, float voxel)
        {
            NavMeshBuildSettings s = NavMesh.CreateSettings();
            s.overrideVoxelSize = true;
            s.voxelSize = voxel;
            s.overrideTileSize = true;
            s.tileSize = tileSize;
            s.agentRadius = radius;
            s.agentHeight = height;
            s.agentClimb = stepHeight;
            s.agentSlope = maxSlope;
            // Petites surfaces gardées pour les goules : un plateau d'échafaudage (4 × 2 m, poteaux aux bords) ne garde que ≈ 2.4 m²
            // une fois rogné du rayon ; au seuil par défaut (2 m²), il manquait sur 85 % des échafaudages (lot art, bloc 2).
            if (radius < 1f)
                s.minRegionArea = smallMinRegionArea;
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(s, sources, bounds, Vector3.zero, Quaternion.identity);
            if (data == null)
            {
                Debug.LogWarning($"NavMesh non construit (rayon {radius} m).", this);
                return EnemyLocomotion.NoAgent;
            }

            _instances.Add(NavMesh.AddNavMeshData(data));
            _settings.Add(s.agentTypeID);
            return s.agentTypeID;
        }

        private void Clear()
        {
            foreach (NavMeshLinkInstance l in _links)
                l.Remove();
            _links.Clear();
            foreach (GameObject g in _islandPassages)
                if (g != null)
                    DestroyImmediate(g);                                // sinon relus par FindObjectsByType<StalkerPassage> au build suivant
            _islandPassages.Clear();
            foreach (NavMeshDataInstance i in _instances)
                i.Remove();
            _instances.Clear();
            foreach (int id in _settings)
                NavMesh.RemoveSettings(id);
            _settings.Clear();
            Built = false;
        }

        /// <summary>Objets qui bougent ou ne sont pas du décor : caisses, ennemis, joueurs, déclencheurs.</summary>
        private static bool IsMoving(Component c)
        {
            if (c is Collider col && col.isTrigger)
                return true;
            if (c.GetComponentInParent<Grabbable>() != null || c.GetComponentInParent<Damageable>() != null
                || c.GetComponentInParent<PlayerHealth>() != null || c.GetComponentInParent<CharacterController>() != null)
                return true;
            Rigidbody rb = c is Collider cl ? cl.attachedRigidbody : c.GetComponentInParent<Rigidbody>();
            return rb != null;
        }
    }
}
