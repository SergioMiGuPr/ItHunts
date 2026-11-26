using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Voûtes (seconde revue de la DA, D-120) : sous chaque plafond, des arcs de pierre (château) ou des poutres de bois sur corbeaux
    /// (caves) en travers de la salle, d'un mur à l'autre, tous les ceilingRibSpacing m ; une applique haute tournée vers le haut tous
    /// les upLightEvery arcs (elle éclaire l'arc, le plafond et le haut du mur) ; une brume sous le plafond qui fond le plafond lointain
    /// dans un voile au lieu d'un aplat noir. Tout est visuel (aucune collision : le mouvement, le NavMesh et le traqueur ne changent
    /// pas) et fusionné par salle avec le reste du décor.
    /// </summary>
    public sealed partial class ActLevel
    {
        /// <summary>Arcs ou poutres posés sous les plafonds (mesure).</summary>
        public int CeilingRibs { get; private set; }

        /// <summary>Appliques hautes posées (mesure).</summary>
        public int UpLights { get; private set; }

        private void BuildCeilings()
        {
            CeilingRibs = 0;
            UpLights = 0;
            if (settings.Open)
                return;
            foreach (ActRoom r in Plan.Rooms)
                if (r.Ceiling > 0f)
                {
                    _roomMaterials = Art != null ? Art.MaterialsFor(r.Function) : null;
                    BuildCeiling(r, Group("Voûte", _roomRoots[r.Id]));
                }
        }

        private void BuildCeiling(ActRoom r, Transform root)
        {
            float top = r.Floor + r.Ceiling;
            if (settings.ceilingHaze != null)
                foreach (RectInt rc in Rects(new HashSet<Vector2Int>(r.Cells)))
                {
                    var haze = new GameObject("Brume du plafond");
                    haze.transform.SetParent(root, false);
                    haze.transform.position = RectCenter(rc, top - 0.8f);
                    haze.transform.localScale = new Vector3(rc.width * Plan.Cell, 1f, rc.height * Plan.Cell);
                    haze.AddComponent<MeshFilter>().sharedMesh = PlaneMesh;
                    var mr = haze.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = settings.ceilingHaze;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }

            if (settings.ceilingStructure == CeilingStructure.Aucune)
                return;
            var cells = new HashSet<Vector2Int>(r.Cells);
            bool alongX = r.Box.width >= r.Box.height;                 // grand axe : les arcs le traversent
            int longMin = alongX ? r.Box.xMin : r.Box.yMin, longLen = alongX ? r.Box.width : r.Box.height;
            int crossMin = alongX ? r.Box.yMin : r.Box.xMin, crossLen = alongX ? r.Box.height : r.Box.width;
            float step = settings.ceilingRibSpacing / Plan.Cell;
            int count = Mathf.Max(1, Mathf.FloorToInt((longLen - 1) / step) + 1);
            float first = longMin + (longLen - (count - 1) * step) * 0.5f;
            bool lights = settings.upLightEvery > 0 && r.Role != RoomRole.Arrivee && r.Role != RoomRole.Repaire;
            Material mat = settings.ceilingRibMaterial != null ? settings.ceilingRibMaterial : settings.trimMaterial != null ? settings.trimMaterial : RoomWall;
            int rib = 0;
            for (int k = 0; k < count; k++)
            {
                int line = Mathf.FloorToInt(first + k * step);
                // Plages de cases de la salle le long de cette ligne (une salle organique peut en avoir plusieurs).
                int v = crossMin;
                while (v < crossMin + crossLen)
                {
                    if (!cells.Contains(Cell(alongX, line, v)))
                    {
                        v++;
                        continue;
                    }

                    int v0 = v;
                    while (v < crossMin + crossLen && cells.Contains(Cell(alongX, line, v)))
                        v++;
                    if ((v - v0) * Plan.Cell >= 6f)
                        Rib(r, root, alongX, line + 0.5f, v0, v, top, mat, lights && rib++ % settings.upLightEvery == 0, rib % 2 == 0);
                }
            }
        }

        private static Vector2Int Cell(bool alongX, int line, int v) => alongX ? new Vector2Int(line, v) : new Vector2Int(v, line);

        /// <summary>Un arc (ou une poutre) en travers, entre les deux murs trouvés par rayon (contours organiques, linteaux des portes).</summary>
        private void Rib(ActRoom r, Transform root, bool alongX, float line, int v0, int v1, float top, Material mat, bool light, bool lightAtStart)
        {
            bool arch = settings.ceilingStructure == CeilingStructure.Arcs;
            float thick = arch ? 1.1f : 0.8f;
            Vector3 across = alongX ? Vector3.forward : Vector3.right;
            Vector3 a = World(alongX ? new Vector2(line, v0) : new Vector2(v0, line), 0f);
            Vector3 b = World(alongX ? new Vector2(line, v1) : new Vector2(v1, line), 0f);
            float probeY = top - thick - 0.5f;
            Vector3 mid = (a + b) * 0.5f;
            mid.y = probeY;
            float half = Vector3.Distance(a, b) * 0.5f;
            // Mur réel de chaque côté (les biseaux des coins et les linteaux ne tombent pas sur le bord des cases).
            float da = WallDistance(mid, -across, half + 3f, out bool wallA);
            float db = WallDistance(mid, across, half + 3f, out bool wallB);
            if (!wallA || !wallB)
                return;                                                   // jamais d'arc qui finit dans le vide
            Vector3 pa = mid - across * (da + 0.3f), pb = mid + across * (db + 0.3f);
            pa.y = pb.y = 0f;
            float span = Vector3.Distance(pa, pb);
            if (span < 5f)
                return;
            // Retombée de l'arc : jamais plus bas que 15.5 m au-dessus du sol (le traqueur debout), 7 m au plus.
            float rise = arch ? Mathf.Clamp(Mathf.Min(span * 0.2f, r.Ceiling - StalkerHeadroom - thick), 0.3f, 7f) : 0f;
            CeilingRibs++;
            Mesh mesh = RibMesh(span, arch ? 1f : 0.7f, thick, rise, arch ? 14 : 1);
            var go = new GameObject("Kit_" + (arch ? "Arc de la voûte" : "Poutre du plafond"));
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(new Vector3((pa.x + pb.x) * 0.5f, top, (pa.z + pb.z) * 0.5f), Quaternion.LookRotation(across, Vector3.up));
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _ribMeshes.Add(mesh);

            float spring = top - thick - rise;                            // dessous de l'arc contre le mur
            foreach (bool start in new[] { true, false })
            {
                Vector3 inward = start ? across : -across;
                Vector3 foot = (start ? pa : pb) + inward * 0.3f;          // face du mur
                if (arch)
                    // Console de pierre sous la retombée de l'arc.
                    Visual(root, "Console de l'arc", CubeMesh, mat, new Vector3(foot.x, spring - 0.45f, foot.z) + inward * 0.45f,
                        Quaternion.LookRotation(inward, Vector3.up), new Vector3(1.3f, 0.9f, 0.9f));
                else
                    // Corbeau de bois : une jambe de force du mur au dessous de la poutre.
                    Visual(root, "Corbeau de la poutre", CubeMesh, mat, new Vector3(foot.x, spring - 0.85f, foot.z) + inward * 0.75f,
                        Quaternion.LookRotation(inward, Vector3.up) * Quaternion.Euler(-45f, 0f, 0f), new Vector3(0.45f, 0.45f, 2.3f));
                if (light && start == lightAtStart)
                    UpLight(root, new Vector3(foot.x, spring - (arch ? 2.2f : 2.4f), foot.z), inward);
            }
        }

        /// <summary>Hauteur gardée libre sous les arcs (traqueur debout, 14 m, et une marge).</summary>
        private const float StalkerHeadroom = 15.5f;

        private readonly List<Mesh> _ribMeshes = new List<Mesh>();

        /// <summary>Distance au mur (ou au linteau) le plus proche dans cette direction ; sinon la distance donnée.</summary>
        private float WallDistance(Vector3 from, Vector3 dir, float max, out bool wall)
        {
            float best = max;
            wall = false;
            foreach (RaycastHit h in Physics.RaycastAll(from, dir, max, ~0, QueryTriggerInteraction.Ignore))
            {
                Transform t = h.collider.transform;
                string n = t.name;
                bool isWall = n.StartsWith("Mur") || n.StartsWith("Linteau") || n.StartsWith("Falaise")
                              || (t.parent != null && (t.parent.name.StartsWith("Mur") || t.parent.name.StartsWith("Linteau")));
                if (isWall && h.distance < best)
                {
                    best = h.distance;
                    wall = true;
                }
            }

            return best;
        }

        /// <summary>Applique haute : coupelle de fer, flamme, projecteur chaud tourné vers le haut et un peu vers la salle.</summary>
        private void UpLight(Transform root, Vector3 at, Vector3 inward)
        {
            Material iron = settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial;
            Vector3 bowl = at + inward * 0.45f;
            Visual(root, "Applique haute", CylinderMesh, iron, bowl, Quaternion.identity, new Vector3(0.7f, 0.14f, 0.7f));
            Visual(root, "Support de l'applique", CubeMesh, iron, bowl - inward * 0.25f - Vector3.up * 0.3f, Quaternion.LookRotation(inward, Vector3.up),
                new Vector3(0.12f, 0.6f, 0.5f));
            if (settings.windowMaterial != null)
                Visual(root, "Flamme de l'applique", SphereMesh, settings.windowMaterial, bowl + Vector3.up * 0.25f, Quaternion.identity, new Vector3(0.38f, 0.42f, 0.38f));
            var go = new GameObject("Lumière vers la voûte");
            go.transform.SetParent(root, false);
            go.transform.position = bowl + Vector3.up * 0.5f;
            go.transform.rotation = Quaternion.LookRotation((Vector3.up * 3f + inward).normalized, inward);
            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 125f;
            l.innerSpotAngle = 70f;
            l.range = 16f;
            l.intensity = settings.upLightIntensity;
            l.color = settings.torchColor;
            l.shadows = LightShadows.None;
            AmbianceLight al = go.AddComponent<AmbianceLight>();
            al.kind = AmbianceLight.Kind.Torche;
            al.keepOwn = true;
            UpLights++;
        }

        /// <summary>
        /// Maillage d'un arc (ou d'une poutre si rise = 0), le long de +Z, centré : dessus plat contre le plafond (y = 0), dessous courbe
        /// (y = −thick au milieu, −thick − rise aux murs), largeur width en X. Faces : dessous, deux flancs, deux bouts.
        /// </summary>
        private static Mesh RibMesh(float span, float width, float thick, float rise, int segments)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            float hw = width * 0.5f;
            float Bottom(float t) => -thick - rise * (2f * t - 1f) * (2f * t - 1f);

            // Dessous (normales calculées à partir de la pente).
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments, z = (t - 0.5f) * span, y = Bottom(t);
                float slope = -rise * 4f * (2f * t - 1f) / span;     // dy/dz
                Vector3 normal = new Vector3(0f, -1f, slope).normalized;
                foreach (float x in new[] { -hw, hw })
                {
                    v.Add(new Vector3(x, y, z));
                    n.Add(normal);
                    uv.Add(new Vector2(x, z));
                }
            }

            for (int i = 0; i < segments; i++)
            {
                int b = i * 2;
                tri.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 });
            }

            // Flancs (x = ±hw) : bande verticale entre le dessous courbe et le plafond.
            foreach (float x in new[] { -hw, hw })
            {
                int b = v.Count;
                Vector3 normal = new Vector3(Mathf.Sign(x), 0f, 0f);
                for (int i = 0; i <= segments; i++)
                {
                    float t = (float)i / segments, z = (t - 0.5f) * span;
                    v.Add(new Vector3(x, Bottom(t), z));
                    v.Add(new Vector3(x, 0f, z));
                    n.Add(normal);
                    n.Add(normal);
                    uv.Add(new Vector2(z, Bottom(t)));
                    uv.Add(new Vector2(z, 0f));
                }

                for (int i = 0; i < segments; i++)
                {
                    int q = b + i * 2;
                    if (x > 0f)
                        tri.AddRange(new[] { q, q + 3, q + 2, q, q + 1, q + 3 });
                    else
                        tri.AddRange(new[] { q, q + 2, q + 3, q, q + 3, q + 1 });
                }
            }

            // Bouts (dans le mur, mais visibles si le mur est un peu plus loin).
            foreach (float z in new[] { -span * 0.5f, span * 0.5f })
            {
                int b = v.Count;
                float y = Bottom(z < 0f ? 0f : 1f);
                Vector3 normal = new Vector3(0f, 0f, Mathf.Sign(z));
                v.Add(new Vector3(-hw, y, z));
                v.Add(new Vector3(hw, y, z));
                v.Add(new Vector3(hw, 0f, z));
                v.Add(new Vector3(-hw, 0f, z));
                for (int i = 0; i < 4; i++)
                    n.Add(normal);
                uv.Add(new Vector2(-hw, y));
                uv.Add(new Vector2(hw, y));
                uv.Add(new Vector2(hw, 0f));
                uv.Add(new Vector2(-hw, 0f));
                if (z > 0f)
                    tri.AddRange(new[] { b, b + 1, b + 2, b, b + 2, b + 3 });
                else
                    tri.AddRange(new[] { b, b + 2, b + 1, b, b + 3, b + 2 });
            }

            var m = new Mesh { name = rise > 0f ? "Arc de la voûte" : "Poutre du plafond" };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tri, 0);
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }
    }
}
