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
                if (mats.Contains(outline))
                    continue;
                mats.Add(outline);
                r.sharedMaterials = mats.ToArray();
                Outlined++;
            }
        }

        /// <summary>
        /// Volume signé d'un maillage lisible : négatif s'il est retourné (faces vers l'intérieur). La coque du contour cache les faces
        /// avant : sur un maillage retourné, elle recouvrirait tout en noir. Les bras FPS v2 sont réexportés à l'endroit (PR #14) ;
        /// un test le vérifie à chaque livraison.
        /// </summary>
        public static double SignedVolume(Mesh m)
        {
            Vector3[] v = m.vertices;
            int[] t = m.triangles;
            double vol = 0;
            for (int i = 0; i + 2 < t.Length; i += 3)
                vol += Vector3.Dot(v[t[i]], Vector3.Cross(v[t[i + 1]], v[t[i + 2]]));
            return vol / 6.0;
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
