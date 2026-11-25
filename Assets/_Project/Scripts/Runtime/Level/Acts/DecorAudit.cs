using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>Rôle d'un élément du décor posé par l'habillage (jour 5, revue des cartes de la DA) : sert à l'audit.</summary>
    public enum DecorKind : byte
    {
        /// <summary>Prop posé au sol (table, étagère, tonneau…).</summary>
        Prop,
        /// <summary>Lumière accrochée à un mur (torche, applique) : il lui faut un mur derrière elle.</summary>
        Mural,
        /// <summary>Suspendu (cage, chaîne) : il lui faut un plafond au-dessus, relié par une chaîne.</summary>
        Suspendu,
        /// <summary>Lustre : un seul par point d'accroche, 6 m au moins entre deux.</summary>
        Lustre,
        /// <summary>Lumière posée au sol (brasero, candélabre).</summary>
        LumiereSol,
        /// <summary>Végétal, tombe, haie : contrôlé pour la forme provisoire seulement.</summary>
        Vegetal,
    }

    /// <summary>Étiquette posée par l'habillage sur chaque élément du décor.</summary>
    public sealed class DecorTag : MonoBehaviour
    {
        public DecorKind kind;
        public int order;
    }

    /// <summary>Résultat de l'audit du décor d'un niveau (une ligne par défaut).</summary>
    public sealed class DecorReport
    {
        public int FloatingLights, Unhung, ChandelierPairs, Overlaps, NoCollision, Greybox;
        public readonly List<string> Details = new List<string>();

        public int Total => FloatingLights + Unhung + ChandelierPairs + Overlaps + NoCollision + Greybox;

        public override string ToString() =>
            $"lumières sans mur {FloatingLights}, suspendus sans plafond {Unhung}, lustres trop proches {ChandelierPairs}, " +
            $"chevauchements {Overlaps}, décor traversable {NoCollision}, formes provisoires {Greybox}";

        public string Text(int max = 40)
        {
            var sb = new StringBuilder(ToString());
            foreach (string d in Details.Take(max))
                sb.Append("\n- ").Append(d);
            if (Details.Count > max)
                sb.Append($"\n- … et {Details.Count - max} autres");
            return sb.ToString();
        }
    }

    /// <summary>
    /// Audit et correction du décor d'un niveau (jour 5, revue des cartes de la DA, D-115) : lumières murales sans mur derrière
    /// (ouverture percée après coup) ou plantées dans un grand prop, cages et chaînes sans plafond (reliées au plafond par une
    /// chaîne, ou retirées sous le ciel), lustres à moins de 6 m l'un de l'autre, props qui se chevauchent, décor de plus de 0.8 m
    /// sans collision (braseros, chaudrons : une boîte de collision), formes provisoires (matériaux greybox) encore visibles.
    /// En mode correction, chaque défaut est corrigé ; un second passage en contrôle doit alors tout compter à 0 (test PlayMode).
    /// </summary>
    public static class DecorAudit
    {
        private const float ChandelierGap = 6f;
        private const float WallReach = 1.3f;
        private const float TallProp = 1.5f;
        private const float CollisionHeight = 0.8f;

        private static readonly string[] Traversable = { "Tapisserie", "Tapis", "Chaine", "Chaîne", "SousBois", "Brume", "Toile", "Rayon", "Faisceau", "Cierge", "Bougie" };
        private static readonly string[] GreyboxPrefixes = { "Prop_", "Greybox_", "Module_" };

        public static DecorReport Run(Transform root, bool fix, Material chainMaterial = null)
        {
            var report = new DecorReport();
            if (root == null)
                return report;
            Physics.SyncTransforms();
            List<DecorTag> tags = root.GetComponentsInChildren<DecorTag>(false).Where(t => t != null && t.gameObject.activeInHierarchy)
                .OrderBy(t => t.order).ToList();

            // 1. Formes provisoires : un matériau greybox encore visible (le modèle de la DA n'a pas pris).
            foreach (DecorTag t in tags)
                if (t.gameObject.activeInHierarchy && t.GetComponentInChildren<ItHunts.Orbs.Grabbable>() == null && HasGreybox(t.gameObject))
                {
                    report.Greybox++;
                    report.Details.Add($"forme provisoire : {t.name} en {P(t.transform.position)}");
                    if (fix)
                        Remove(t);
                }

            // 2. Lustres : un seul par point, 6 m au moins entre deux.
            var kept = new List<Vector3>();
            foreach (DecorTag t in tags.Where(t => t.kind == DecorKind.Lustre && t.gameObject.activeInHierarchy))
            {
                Vector3 p = t.transform.position;
                if (kept.Any(o => Flat(o - p) < ChandelierGap && Mathf.Abs(o.y - p.y) < 8f))
                {
                    report.ChandelierPairs++;
                    report.Details.Add($"lustre à moins de {ChandelierGap} m d'un autre : {P(p)}");
                    if (fix)
                        Remove(t);
                    continue;
                }

                kept.Add(p);
            }

            // Boîtes des props (pour les chevauchements et les lumières plantées dans un grand prop).
            var boxes = new List<(DecorTag tag, Bounds b)>();
            foreach (DecorTag t in tags)
                if (t.gameObject.activeInHierarchy && (t.kind == DecorKind.Prop || t.kind == DecorKind.LumiereSol) && Box(t.gameObject, out Bounds b))
                    boxes.Add((t, b));

            // 3. Lumières murales : un mur derrière, à moins de 1.3 m ; jamais plantées dans un grand prop.
            foreach (DecorTag t in tags.Where(t => t.kind == DecorKind.Mural && t.gameObject.activeInHierarchy))
            {
                Vector3 p = t.transform.position, f = t.transform.forward;
                f.y = 0f;
                f = f.sqrMagnitude > 1e-4f ? f.normalized : Vector3.forward;
                bool wall = StaticHit(p + f * 0.4f, -f, WallReach + 0.4f, out _);
                bool inProp = boxes.Any(x => x.b.size.y > TallProp && Expand(x.b, 1f).Contains(p));
                if (wall && !inProp)
                    continue;
                report.FloatingLights++;
                report.Details.Add((wall ? "lumière murale plantée dans un prop : " : "lumière murale sans mur : ") + $"{t.name} en {P(p)}");
                if (fix)
                    Remove(t);
            }

            // 4. Suspendus : reliés au plafond par une chaîne ; retirés s'il n'y a pas de plafond (ciel ouvert, ouverture).
            foreach (DecorTag t in tags.Where(t => t.kind == DecorKind.Suspendu && t.gameObject.activeInHierarchy))
            {
                if (!Box(t.gameObject, out Bounds b))
                    continue;
                Vector3 top = new Vector3(b.center.x, b.max.y, b.center.z);
                bool hung = t.transform.Find("Chaîne jusqu'au plafond") != null;
                if (hung)
                    continue;
                if (StaticHit(top + Vector3.up * 0.05f, Vector3.up, 40f, out RaycastHit hit))
                {
                    if (!fix)
                    {
                        report.Unhung++;
                        report.Details.Add($"suspendu sans chaîne jusqu'au plafond : {t.name} en {P(top)}");
                        continue;
                    }

                    var chain = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    chain.name = "Chaîne jusqu'au plafond";
                    Object.Destroy(chain.GetComponent<Collider>());
                    chain.transform.SetParent(t.transform, true);
                    float len = hit.distance + 0.1f;
                    chain.transform.position = top + Vector3.up * (len * 0.5f);
                    chain.transform.rotation = Quaternion.identity;
                    Vector3 ls = t.transform.lossyScale;
                    chain.transform.localScale = new Vector3(0.07f / Mathf.Max(0.01f, ls.x), len / Mathf.Max(0.01f, ls.y), 0.07f / Mathf.Max(0.01f, ls.z));
                    if (chainMaterial != null)
                        chain.GetComponent<MeshRenderer>().sharedMaterial = chainMaterial;
                    report.Unhung++;
                    continue;
                }

                report.Unhung++;
                report.Details.Add($"suspendu sous le ciel : {t.name} en {P(top)}");
                if (fix)
                    Remove(t);
            }

            // 5. Chevauchements : deux props qui se pénètrent (boîtes réduites de 10 %) ; le plus récent part. Les objets attrapables
            //    (physiques) se repoussent seuls : ils ne comptent pas.
            var grid = new Dictionary<Vector2Int, List<int>>();
            var alive = new bool[boxes.Count];
            for (int i = 0; i < boxes.Count; i++)
            {
                alive[i] = boxes[i].tag.gameObject.activeInHierarchy;
                if (!alive[i] || IsGrabbable(boxes[i].tag))
                    continue;
                Bounds bi = Shrink(boxes[i].b, 0.1f);
                bool hit = false;
                foreach (Vector2Int c in Cells(bi))
                    if (grid.TryGetValue(c, out List<int> list))
                        foreach (int j in list)
                            if (alive[j] && Overlap(bi, Shrink(boxes[j].b, 0.1f)))
                            {
                                hit = true;
                                break;
                            }

                if (hit)
                {
                    report.Overlaps++;
                    report.Details.Add($"chevauchement : {boxes[i].tag.name} en {P(boxes[i].b.center)}");
                    if (fix)
                    {
                        Remove(boxes[i].tag);
                        alive[i] = false;
                    }

                    continue;
                }

                foreach (Vector2Int c in Cells(bi))
                {
                    if (!grid.TryGetValue(c, out List<int> list))
                        grid[c] = list = new List<int>();
                    list.Add(i);
                }
            }

            // 6. Décor traversable : tout prop ou lumière au sol de plus de 0.8 m a une collision (boîte ajustée au modèle).
            for (int i = 0; i < boxes.Count; i++)
            {
                DecorTag t = boxes[i].tag;
                if (!t.gameObject.activeInHierarchy || boxes[i].b.size.y < CollisionHeight || Traversable.Any(n => t.name.Contains(n)))
                    continue;
                if (t.GetComponentsInChildren<Collider>().Any(c => c.enabled && !c.isTrigger))
                    continue;
                report.NoCollision++;
                report.Details.Add($"sans collision : {t.name} ({boxes[i].b.size.x:0.0} × {boxes[i].b.size.y:0.0} × {boxes[i].b.size.z:0.0} m) en {P(t.transform.position)}");
                if (fix)
                    FitBox(t.gameObject);
            }

            return report;
        }

        // ---------- Outils ----------

        private static void Remove(DecorTag t)
        {
            t.gameObject.SetActive(false);
            Object.Destroy(t.gameObject);
        }

        private static bool IsGrabbable(DecorTag t) => t.GetComponentInChildren<ItHunts.Orbs.Grabbable>() != null;

        private static float Flat(Vector3 d) => new Vector2(d.x, d.z).magnitude;

        private static string P(Vector3 p) => $"({p.x:0.0}, {p.y:0.0}, {p.z:0.0})";

        private static Bounds Expand(Bounds b, float m)
        {
            b.Expand(m * 2f);
            return b;
        }

        private static Bounds Shrink(Bounds b, float share)
        {
            Vector3 s = b.size;
            return new Bounds(b.center, new Vector3(s.x * (1f - share * 2f), s.y * (1f - share * 2f), s.z * (1f - share * 2f)));
        }

        private static bool Overlap(Bounds a, Bounds b) =>
            a.min.x < b.max.x && b.min.x < a.max.x && a.min.y < b.max.y && b.min.y < a.max.y && a.min.z < b.max.z && b.min.z < a.max.z;

        private static IEnumerable<Vector2Int> Cells(Bounds b)
        {
            for (int x = Mathf.FloorToInt(b.min.x / 4f); x <= Mathf.FloorToInt(b.max.x / 4f); x++)
            for (int z = Mathf.FloorToInt(b.min.z / 4f); z <= Mathf.FloorToInt(b.max.z / 4f); z++)
                yield return new Vector2Int(x, z);
        }

        /// <summary>Boîte des rendus visibles (maillages), sans lumières, particules ni textes.</summary>
        public static bool Box(GameObject go, out Bounds b)
        {
            b = default;
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !(r is MeshRenderer || r is SkinnedMeshRenderer) || r.name == "Chaîne jusqu'au plafond")
                    continue;
                if (!any)
                {
                    b = r.bounds;
                    any = true;
                }
                else
                    b.Encapsulate(r.bounds);
            }

            return any;
        }

        private static bool HasGreybox(GameObject go)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !(r is MeshRenderer))
                    continue;
                foreach (Material m in r.sharedMaterials)
                {
                    if (m == null)
                        return true;
                    string n = m.name.Replace(" (Instance)", "");
                    if (GreyboxPrefixes.Any(p => n.StartsWith(p)) || n.StartsWith("Default-"))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Touche une surface fixe du niveau (pas un prop étiqueté, pas un objet physique).</summary>
        private static bool StaticHit(Vector3 from, Vector3 dir, float dist, out RaycastHit best)
        {
            best = default;
            float bestD = float.MaxValue;
            bool any = false;
            foreach (RaycastHit h in Physics.RaycastAll(from, dir, dist, ~0, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.attachedRigidbody != null && !h.collider.attachedRigidbody.isKinematic)
                    continue;
                if (h.collider.GetComponentInParent<DecorTag>() != null)
                    continue;
                if (h.distance < bestD)
                {
                    bestD = h.distance;
                    best = h;
                    any = true;
                }
            }

            return any;
        }

        /// <summary>Boîte de collision ajustée aux maillages, dans le repère de l'objet.</summary>
        private static void FitBox(GameObject go)
        {
            Matrix4x4 toLocal = go.transform.worldToLocalMatrix;
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null || mf.name == "Chaîne jusqu'au plafond")
                    continue;
                Bounds mb = mf.sharedMesh.bounds;
                Matrix4x4 m = toLocal * mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 c = m.MultiplyPoint3x4(mb.center + Vector3.Scale(mb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)));
                    min = Vector3.Min(min, c);
                    max = Vector3.Max(max, c);
                }
            }

            if (float.IsInfinity(min.x))
                return;
            var box = go.AddComponent<BoxCollider>();
            box.center = (min + max) * 0.5f;
            box.size = max - min;
            if (go.GetComponent<ItHunts.Enemies.StalkerPassesThrough>() == null)
                go.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
        }
    }
}
