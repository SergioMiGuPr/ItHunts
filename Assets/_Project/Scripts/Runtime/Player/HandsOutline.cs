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
