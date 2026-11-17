using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Contour des mains et des artefacts (D-067, RENDU.md § 5 bis) : ajoute le matériau de coque inversée (ItHunts/ContourMains)
    /// à chaque renderer opaque de la vue FPS (mains, barillet, lanterne, orbe). Les transparents (verre de la lanterne), les
    /// flammes et les traînées n'en ont pas. Au démarrage, après les vues des artefacts (qui remplacent certains matériaux).
    /// </summary>
    [DefaultExecutionOrder(500)]
    public sealed class HandsOutline : MonoBehaviour
    {
        [SerializeField] private Transform viewRoot;
        [SerializeField] private Material outline;
        [Tooltip("Maillages retournés (faces vers l'intérieur), relevés par FpsArmsSetup : leur contour cache les faces arrière.")]
        [SerializeField] private List<Mesh> insideOut = new List<Mesh>();

        /// <summary>Liste des maillages retournés (écrite par FpsArmsSetup, éditeur).</summary>
        public List<Mesh> InsideOutMeshes => insideOut;

        /// <summary>Renderers qui ont reçu le contour (tests, mesures).</summary>
        public int Outlined { get; private set; }

        private void Start() => Apply();

        public void Apply()
        {
            if (viewRoot == null || outline == null)
                return;
            Outlined = 0;
            foreach (Renderer r in viewRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (!(r is MeshRenderer || r is SkinnedMeshRenderer) || !Wants(r))
                    continue;
                var mats = new List<Material>(r.sharedMaterials);
                Material use = InsideOut(r) ? Inverted() : outline;
                if (mats.Contains(outline) || mats.Contains(use))
                    continue;
                mats.Add(use);
                r.sharedMaterials = mats.ToArray();
                Outlined++;
            }
        }

        private Material _inverted;
        private static readonly Dictionary<Mesh, bool> InsideOutCache = new Dictionary<Mesh, bool>();

        /// <summary>Contour pour un maillage retourné (faces vers l'intérieur) : on cache les faces arrière au lieu des faces avant.</summary>
        private Material Inverted()
        {
            if (_inverted == null)
            {
                _inverted = new Material(outline) { name = outline.name + " (maillage retourné)" };
                _inverted.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Back);
                // Normales vers l'intérieur : la coque se gonfle à l'opposé de la normale pour entourer le bras.
                _inverted.SetFloat("_Thickness", -Mathf.Abs(outline.GetFloat("_Thickness")));
            }

            return _inverted;
        }

        /// <summary>
        /// Maillage retourné (volume signé négatif) : les bras FPS v2 de la PR #11 sont exportés faces vers l'intérieur ; avec le contour
        /// normal (faces avant cachées), la coque recouvrait tout le bras en noir. Contournement en attendant leur réexport (ART-HANDOFF).
        /// </summary>
        private bool InsideOut(Renderer r)
        {
            Mesh m = r is SkinnedMeshRenderer s ? s.sharedMesh : r.TryGetComponent(out MeshFilter f) ? f.sharedMesh : null;
            if (m == null)
                return false;
            if (insideOut.Contains(m))
                return true;
            if (!m.isReadable)
                return false;                                           // en build, seule la liste fait foi
            if (InsideOutCache.TryGetValue(m, out bool known))
                return known;
            Vector3[] v = m.vertices;
            int[] t = m.triangles;
            double vol = 0;
            for (int i = 0; i + 2 < t.Length; i += 3)
                vol += Vector3.Dot(v[t[i]], Vector3.Cross(v[t[i + 1]], v[t[i + 2]]));
            return InsideOutCache[m] = vol < 0;
        }

        /// <summary>Volume signé d'un maillage lisible (éditeur) : négatif s'il est retourné.</summary>
        public static bool IsInsideOut(Mesh m)
        {
            Vector3[] v = m.vertices;
            int[] t = m.triangles;
            double vol = 0;
            for (int i = 0; i + 2 < t.Length; i += 3)
                vol += Vector3.Dot(v[t[i]], Vector3.Cross(v[t[i + 1]], v[t[i + 2]]));
            return vol < 0;
        }

        private bool Wants(Renderer r)
        {
            string n = r.name;
            if (n.Contains("Flamme") || n.Contains("Verre") || n.Contains("Traînée") || n.Contains("Impact") || n.Contains("Bouffée"))
                return false;
            foreach (Material m in r.sharedMaterials)
                if (m == null || m.renderQueue >= 2500)
                    return false;                                      // transparents : pas de coque
            return true;
        }
    }
}
