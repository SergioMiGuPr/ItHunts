using ItHunts.Player;
using UnityEngine;
using UnityEngine.Rendering;

namespace ItHunts.Economy
{
    /// <summary>
    /// Réservé à un joueur à l'écran (coffres par joueur, D-043) : les rendus de cet objet (et de ses enfants, textes compris) ne
    /// se dessinent que pour la caméra de ce joueur. Une caméra sans joueur (vue Scène) voit tout. PlayerId 0 : tout le monde.
    /// Réseau (étape 11) : l'objet n'existe que chez ce joueur (ou reste caché chez les autres).
    /// </summary>
    public sealed class ViewerOnly : MonoBehaviour
    {
        public int PlayerId;

        private Renderer[] _renderers;

        public bool Allows(PlayerHealth viewer) => PlayerId == 0 || viewer == null || viewer.PlayerId == PlayerId;

        private void OnEnable()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
        }

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= BeforeCamera;

        private void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            if (cam == null)
                return;
            bool on = Allows(Interactables.PlayerOfCamera(cam));
            foreach (Renderer r in _renderers)
                if (r != null && r.GetComponent<WorldLabel>() == null)    // les textes gèrent leur visibilité eux-mêmes
                    r.enabled = on;
        }
    }
}
