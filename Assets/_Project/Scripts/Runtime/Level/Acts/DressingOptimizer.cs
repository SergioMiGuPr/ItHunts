using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Coût du décor des grandes cartes (D-119) : lumières trop proches éteintes pour de bon (une tous les lightGap m), pièces fixes
    /// fusionnées par dalle et par matière (leurs collisions restent), puis, en jeu, dalles et petites pièces masquées au loin ou dans
    /// une salle à plus de deux portes du joueur (niveaux fermés). Purement visuel : rien de la simulation ne change.
    /// </summary>
    public sealed class DressingOptimizer : MonoBehaviour
    {
        private const float Tile = 24f;

        public float lightGap = 7f;
        public float farSmall = 70f;
        public float farTile = 115f;
        public int doorHops = 2;
        /// <summary>Plus gros : dessiné tel quel (les copies d'un grand maillage coûtent trop de mémoire une fois fusionnées).</summary>
        public int maxMergeVertices = 800;

        public int LightsRemoved { get; private set; }
        public int Merged { get; private set; }
        public int Tiles => _groups.Count;

        private sealed class Group
        {
            public Vector3 Center;
            public float Radius, Far;
            public int Room;
            public bool Hidden;
            public readonly List<Renderer> Renderers = new List<Renderer>();
            public readonly List<AmbianceLight> Lights = new List<AmbianceLight>();
        }

        private readonly List<Group> _groups = new List<Group>();
        private ActPlan _plan;
        private bool _closed;
        private readonly List<HashSet<int>> _near = new List<HashSet<int>>();
        private float _next;

        public void Build(ActPlan plan, Transform root, bool closed)
        {
            _plan = plan;
            _closed = closed;
            ThinLights(root);
            Merge(root);
            CollectFlames(root);
            CollectLights(root);
            for (int i = 0; i < plan.Rooms.Count; i++)
                _near.Add(Around(i));
        }

        /// <summary>Salles à doorHops portes au plus.</summary>
        private HashSet<int> Around(int room)
        {
            var seen = new HashSet<int> { room };
            var edge = new List<int> { room };
            for (int k = 0; k < doorHops; k++)
            {
                var next = new List<int>();
                foreach (int r in edge)
                    foreach (int d in _plan.Rooms[r].Doors)
                        if (seen.Add(_plan.Doors[d].Other(r)))
                            next.Add(_plan.Doors[d].Other(r));
                edge = next;
            }

            return seen;
        }

        private void ThinLights(Transform root)
        {
            var kept = new Dictionary<Vector2Int, List<Vector3>>();
            IEnumerable<AmbianceLight> all = root.GetComponentsInChildren<AmbianceLight>(true)
                .Where(l => l.GetComponent<Light>() != null && l.GetComponent<Light>().type != LightType.Directional && !l.name.StartsWith("Lueur"))
                .OrderByDescending(l => Priority(l)).ToList();
            foreach (AmbianceLight l in all)
            {
                Vector3 p = l.transform.position;
                var c = new Vector2Int(Mathf.FloorToInt(p.x / lightGap), Mathf.FloorToInt(p.z / lightGap));
                bool close = false;
                for (int dx = -1; dx <= 1 && !close; dx++)
                for (int dy = -1; dy <= 1 && !close; dy++)
                    if (kept.TryGetValue(c + new Vector2Int(dx, dy), out List<Vector3> near))
                        close = near.Any(o => (o - p).sqrMagnitude < lightGap * lightGap);
                if (!close || l.keepOwn)
                {
                    if (!kept.TryGetValue(c, out List<Vector3> list))
                        kept[c] = list = new List<Vector3>();
                    list.Add(p);
                    continue;
                }

                // Flammes et lumière retirées : la pièce reste, éteinte.
                DecorTag owner = l.GetComponentInParent<DecorTag>();
                if (owner != null)
                    foreach (ParticleSystem ps in owner.GetComponentsInChildren<ParticleSystem>(true))
                        if (ps != null && ps.gameObject != owner.gameObject)
                            Destroy(ps.gameObject);
                var data = l.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
                Light light = l.GetComponent<Light>();
                DestroyImmediate(l);
                if (data != null)
                    DestroyImmediate(data);
                DestroyImmediate(light);
                LightsRemoved++;
            }
        }

        private static int Priority(AmbianceLight l)
        {
            DecorTag t = l.GetComponentInParent<DecorTag>();
            if (l.keepOwn)
                return 4;
            return t == null ? 3 : t.kind == DecorKind.Lustre ? 2 : t.kind == DecorKind.Mural ? 0 : 1;
        }

        private void Merge(Transform root)
        {
            var byKey = new Dictionary<(int room, int x, int y, bool tall, Material mat), List<CombineInstance>>();
            var sources = new List<MeshRenderer>();
            foreach (MeshRenderer r in root.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!r.enabled || r.forceRenderingOff || Excluded(r.transform, root))
                    continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !mf.sharedMesh.isReadable || r.name.StartsWith("Décor fusionné"))
                    continue;
                if (mf.sharedMesh.vertexCount > maxMergeVertices || r.HasPropertyBlock())
                    continue;
                Material[] mats = r.sharedMaterials;
                if (mats.Length < mf.sharedMesh.subMeshCount || mats.Any(m => m == null))
                    continue;
                Vector3 p = r.bounds.center;
                bool tall = r.bounds.size.y > 4.5f || r.bounds.size.x > 12f || r.bounds.size.z > 12f;
                int room = _plan.OwnerAt(_plan.CellOf(p));
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                {
                    var key = (room, Mathf.FloorToInt(p.x / Tile), Mathf.FloorToInt(p.z / Tile), tall, mats[s]);
                    if (!byKey.TryGetValue(key, out List<CombineInstance> list))
                        byKey[key] = list = new List<CombineInstance>();
                    list.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = r.transform.localToWorldMatrix });
                }

                sources.Add(r);
            }

            Transform holder = new GameObject("Décor fusionné (dalles)").transform;
            holder.SetParent(root, false);
            var groups = new Dictionary<(int, int, int, bool), Group>();
            foreach (var kv in byKey)
            {
                var mesh = new Mesh { name = "Décor fusionné", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
                mesh.CombineMeshes(kv.Value.ToArray(), true, true);
                mesh.UploadMeshData(true);
                var go = new GameObject("Décor fusionné (" + kv.Key.mat.name + ")");
                go.transform.SetParent(holder, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = kv.Key.mat;
                mr.shadowCastingMode = kv.Key.tall ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
                var gk = (kv.Key.room, kv.Key.x, kv.Key.y, kv.Key.tall);
                if (!groups.TryGetValue(gk, out Group g))
                {
                    groups[gk] = g = new Group { Room = kv.Key.room, Far = kv.Key.tall ? farTile : farSmall };
                    _groups.Add(g);
                }

                g.Renderers.Add(mr);
            }

            // Les rendus d'origine restent activés (l'audit du décor les lit) mais ne sont plus dessinés.
            foreach (MeshRenderer r in sources)
                r.forceRenderingOff = true;
            Merged = sources.Count;
            foreach (Group g in _groups)
            {
                Bounds b = g.Renderers[0].bounds;
                foreach (Renderer r in g.Renderers)
                    b.Encapsulate(r.bounds);
                g.Center = b.center;
                g.Radius = new Vector2(b.extents.x, b.extents.z).magnitude;
            }

            // Pièces non fusionnables (niveaux de détail, maillages non lisibles) : par dalle aussi.
            var loose = new Dictionary<(int, int, int), Group>();
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(false))
            {
                if (r.forceRenderingOff || !r.enabled || r.transform.IsChildOf(holder) || r.name.StartsWith("Décor fusionné")
                    || (r.GetComponentInParent<DecorTag>() == null && Excluded(r.transform, root)) || r.GetComponentInParent<ItHunts.Orbs.Grabbable>() != null
                    || r.bounds.size.magnitude > 80f)
                    continue;
                Vector3 p = r.bounds.center;
                int room = _plan.OwnerAt(_plan.CellOf(p));
                bool big = r.bounds.size.y > 4.5f;
                var key = (room, Mathf.FloorToInt(p.x / Tile), Mathf.FloorToInt(p.z / Tile) * 2 + (big ? 1 : 0));
                if (!loose.TryGetValue(key, out Group g))
                {
                    loose[key] = g = new Group { Room = room, Far = big ? farTile : farSmall, Center = new Vector3((key.Item2 + 0.5f) * Tile, p.y, (Mathf.FloorToInt(p.z / Tile) + 0.5f) * Tile), Radius = Tile * 0.71f };
                    _groups.Add(g);
                }

                g.Renderers.Add(r);
                if (r is MeshRenderer)
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static bool Excluded(Transform t, Transform root)
        {
            for (Transform x = t; x != null && x != root; x = x.parent)
            {
                if (x.parent == root && x.name == "Jeu du niveau")
                    return true;
                if (x.TryGetComponent<Rigidbody>(out _) || x.TryGetComponent<LODGroup>(out _) || x.TryGetComponent<Animator>(out _)
                    || x.TryGetComponent<ParticleSystem>(out _) || x.TryGetComponent<ItHunts.Orbs.Grabbable>(out _))
                    return true;
                if (x.name.Contains("Porte") || x.name.Contains("Battant") || x.name.Contains("Herse") || x.name.Contains("Coffre"))
                    return true;
            }

            return t.GetComponent<ItHunts.Art.VFX.VfxPeint>() != null;
        }

        private void CollectFlames(Transform root)
        {
            var groups = new Dictionary<(int, int, int), Group>();
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is ParticleSystemRenderer) && r.GetComponentInParent<ItHunts.Art.VFX.VfxPeint>() == null)
                    continue;
                if (r.GetComponentInParent<DecorTag>() == null)
                    continue;
                Vector3 p = r.transform.position;
                int room = _plan.OwnerAt(_plan.CellOf(p));
                var key = (room, Mathf.FloorToInt(p.x / Tile), Mathf.FloorToInt(p.z / Tile));
                if (!groups.TryGetValue(key, out Group g))
                {
                    groups[key] = g = new Group { Room = room, Far = farSmall, Center = new Vector3((key.Item2 + 0.5f) * Tile, p.y, (key.Item3 + 0.5f) * Tile), Radius = Tile * 0.71f };
                    _groups.Add(g);
                }

                g.Renderers.Add(r);
            }
        }

        private void CollectLights(Transform root)
        {
            var groups = new Dictionary<(int, int, int), Group>();
            foreach (AmbianceLight l in root.GetComponentsInChildren<AmbianceLight>(true))
            {
                if (l == null)
                    continue;
                Vector3 p = l.transform.position;
                int room = _plan.OwnerAt(_plan.CellOf(p));
                var key = (room, Mathf.FloorToInt(p.x / Tile), Mathf.FloorToInt(p.z / Tile));
                if (!groups.TryGetValue(key, out Group g))
                {
                    groups[key] = g = new Group { Room = room, Far = farSmall, Center = new Vector3((key.Item2 + 0.5f) * Tile, p.y, (key.Item3 + 0.5f) * Tile), Radius = Tile * 0.71f };
                    _groups.Add(g);
                }

                g.Lights.Add(l);
            }
        }

        private readonly List<Vector3> _eyes = new List<Vector3>();

        private void LateUpdate()
        {
            if (_plan == null || Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + 0.15f;
            _eyes.Clear();
            foreach (Camera c in Camera.allCameras)
                _eyes.Add(c.transform.position);
            if (_eyes.Count == 0)
                return;
            foreach (Group g in _groups)
            {
                bool hide = true;
                foreach (Vector3 e in _eyes)
                {
                    float d = new Vector2(g.Center.x - e.x, g.Center.z - e.z).magnitude - g.Radius;
                    if (d > g.Far)
                        continue;
                    int at = _closed ? _plan.OwnerAt(_plan.CellOf(e)) : -1;
                    // Niveau fermé : seulement les salles à deux portes au plus (tout ce qui est à moins de 12 m reste toujours là).
                    if (at >= 0 && g.Room >= 0 && d > 12f && !_near[at].Contains(g.Room))
                        continue;
                    hide = false;
                    break;
                }

                if (hide == g.Hidden)
                    continue;
                g.Hidden = hide;
                foreach (Renderer r in g.Renderers)
                    if (r != null)
                        r.forceRenderingOff = hide;
                foreach (AmbianceLight l in g.Lights)
                    if (l != null)
                        l.Hidden = hide;
            }
        }
    }
}
