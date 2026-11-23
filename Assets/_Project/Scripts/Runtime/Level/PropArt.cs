using System.Collections.Generic;
using ItHunts.Economy;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Pose les props en 3D de la DA (PR #51) sur les greybox d'un niveau, une fois le décor et le gameplay posés : la greybox garde
    /// ses colliders, son Rigidbody, Grabbable, Chest… ; ses rendus sont cachés et le modèle de la DA est ajouté en enfant (sans
    /// collider). Règle de pose de la DA (Props4.Ajuste) : voir PropArtSettings. Attrapables : la lueur de prise passe sur le
    /// modèle ; coffres : le couvercle de la DA (« Couvercle ») devient celui du coffre.
    /// Réseau : purement visuel, chaque client le refait à partir de la même greybox.
    /// </summary>
    public static class PropArt
    {
        public const string ModelName = "Modèle (DA)";
        private static PropArtSettings _settings;
        private static bool _loaded;
        private static readonly Dictionary<GameObject, Vector3> ModelSizes = new Dictionary<GameObject, Vector3>();

        public static PropArtSettings Settings
        {
            get
            {
                if (!_loaded)
                {
                    _loaded = true;
                    _settings = Resources.Load<PropArtSettings>("PropArt_Default");
                }

                return _settings;
            }
        }

        /// <summary>Remplace les greybox connues sous cette racine. Renvoie le nombre de props remplacés et écartés.</summary>
        public static (int swapped, int kept) DressAll(Transform root)
        {
            PropArtSettings s = Settings;
            if (s == null || !s.enabledInGame || root == null)
                return (0, 0);
            int swapped = 0, kept = 0;
            var targets = new List<(Transform t, PropArtSettings.Entry e)>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.Find(ModelName) != null)
                    continue;
                PropArtSettings.Entry e = s.Find(Clean(t.name));
                if (e != null && e.models.Length > 0)
                    targets.Add((t, e));
            }

            foreach (var (t, e) in targets)
            {
                if (Apply(t.gameObject, e, s.maxShapeError))
                    swapped++;
                else
                    kept++;
            }

            return (swapped, kept);
        }

        private static string Clean(string s)
        {
            s = s.Replace("(Clone)", "").Trim();
            int p = s.LastIndexOf(" (");
            return p > 0 && s.EndsWith(")") && int.TryParse(s.Substring(p + 2, s.Length - p - 3), out _) ? s.Substring(0, p) : s;
        }

        /// <summary>Rendus de la greybox : maillages visibles, sans textes, particules ni traits.</summary>
        private static List<Renderer> GreyboxRenderers(GameObject go)
        {
            var list = new List<Renderer>();
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                if (r.enabled && (r is MeshRenderer || r is SkinnedMeshRenderer) && r.GetComponent<TextMesh>() == null)
                    list.Add(r);
            return list;
        }

        private static void Corners(Bounds b, Matrix4x4 m, ref Vector3 min, ref Vector3 max)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector3 c = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 p = m.MultiplyPoint3x4(c);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
            }
        }

        /// <summary>Taille du modèle dans son repère (LOD0 seulement s'il y en a), en m, mise en cache.</summary>
        private static Vector3 ModelSize(GameObject prefab)
        {
            if (ModelSizes.TryGetValue(prefab, out Vector3 size))
                return size;
            var filters = prefab.GetComponentsInChildren<MeshFilter>(true);
            bool lod0 = false;
            foreach (MeshFilter f in filters)
                lod0 |= f.name.EndsWith("_LOD0");
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            Matrix4x4 toRoot = prefab.transform.worldToLocalMatrix;
            foreach (MeshFilter f in filters)
                if (f.sharedMesh != null && (!lod0 || f.name.EndsWith("_LOD0")))
                    Corners(f.sharedMesh.bounds, toRoot * f.transform.localToWorldMatrix, ref min, ref max);
            size = float.IsInfinity(min.x) ? Vector3.one : max - min;
            ModelSizes[prefab] = size;
            return size;
        }

        private static bool Apply(GameObject go, PropArtSettings.Entry e, float maxError)
        {
            Transform t = go.transform;
            Vector3 euler = t.eulerAngles;
            if (Mathf.Abs(Mathf.DeltaAngle(euler.x, 0f)) > 1f || Mathf.Abs(Mathf.DeltaAngle(euler.z, 0f)) > 1f)
                return false;                                           // greybox penchée (renversée…) : elle reste
            List<Renderer> grey = GreyboxRenderers(go);
            if (grey.Count == 0)
                return false;
            // Boîte de la greybox dans son repère de lacet (m).
            Quaternion frame = Quaternion.Euler(0f, euler.y, 0f);
            Matrix4x4 toFrame = Matrix4x4.TRS(t.position, frame, Vector3.one).inverse;
            Vector3 min = Vector3.positiveInfinity, max = Vector3.negativeInfinity;
            foreach (Renderer r in grey)
            {
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                    Corners(mf.sharedMesh.bounds, toFrame * r.transform.localToWorldMatrix, ref min, ref max);
                else
                    Corners(r.bounds, toFrame, ref min, ref max);
            }

            Vector3 g = max - min;
            if (g.x < 0.05f || g.y < 0.05f || g.z < 0.05f)
                return false;
            // Candidat le plus proche (variante, quart de tour), échelle uniforme.
            GameObject best = null;
            float bestRot = 0f, bestScale = 1f, bestErr = float.PositiveInfinity;
            foreach (GameObject m in e.models)
            {
                if (m == null)
                    continue;
                Vector3 ms = ModelSize(m);
                foreach (float rot in new[] { 0f, 90f })
                {
                    float mx = rot == 0f ? ms.x : ms.z, mz = rot == 0f ? ms.z : ms.x;
                    float lx = Mathf.Log(g.x / Mathf.Max(0.01f, mx)), ly = Mathf.Log(g.y / Mathf.Max(0.01f, ms.y)), lz = Mathf.Log(g.z / Mathf.Max(0.01f, mz));
                    float le = (lx + ly + lz) / 3f;
                    float err = Mathf.Abs(lx - le) + Mathf.Abs(ly - le) + Mathf.Abs(lz - le);
                    if (err < bestErr)
                    {
                        bestErr = err;
                        best = m;
                        bestRot = rot;
                        bestScale = Mathf.Exp(le);
                    }
                }
            }

            if (best == null || (e.fixedScale <= 0f && bestErr > maxError))
                return false;
            float scale = e.fixedScale > 0f ? e.fixedScale : Mathf.Min(bestScale, e.maxScale);
            // Support d'échelle 1 dans le monde sous la greybox (dont l'échelle peut être non uniforme : tonneau 0.6 × 0.45 × 0.6).
            var holder = new GameObject(ModelName).transform;
            holder.SetParent(t, false);
            Vector3 ls = t.lossyScale;
            holder.localScale = new Vector3(1f / Mathf.Max(0.001f, Mathf.Abs(ls.x)), 1f / Mathf.Max(0.001f, Mathf.Abs(ls.y)), 1f / Mathf.Max(0.001f, Mathf.Abs(ls.z)));
            Vector3 centre = (min + max) * 0.5f;
            holder.position = t.position + frame * new Vector3(centre.x, min.y, centre.z);
            holder.rotation = frame;
            GameObject model = Object.Instantiate(best, holder, false);
            model.name = best.name;
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.Euler(0f, bestRot, 0f);
            model.transform.localScale = Vector3.one * scale;
            foreach (Collider c in model.GetComponentsInChildren<Collider>(true))
                Object.Destroy(c);                                      // la greybox garde ses colliders (physique, NavMesh, prise)
            foreach (Renderer r in grey)
                r.enabled = false;
            var modelRenderers = model.GetComponentsInChildren<Renderer>(true);
            if (go.TryGetComponent(out Grabbable grab))
                grab.UseRenderers(modelRenderers);
            if (e.chest && go.TryGetComponent(out Chest chest))
                chest.SetLid(FindDeep(model.transform, "Couvercle"));
            return true;
        }

        private static Transform FindDeep(Transform t, string name)
        {
            foreach (Transform c in t.GetComponentsInChildren<Transform>(true))
                if (c.name == name)
                    return c;
            return null;
        }
    }
}
