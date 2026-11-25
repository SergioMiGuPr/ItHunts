using UnityEngine;
using UnityEngine.Rendering;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Voûte céleste centrée sur la caméra qui rend (écran partagé compris) : le ciel reste à l'infini où que soit le joueur.
    /// Revue des cartes de la DA (jour 5) : au bout d'une carte de 700 m, la caméra sortait de la voûte de 900 m posée au départ.
    /// </summary>
    public sealed class SkyFollowCamera : MonoBehaviour
    {
        private void OnEnable() => RenderPipelineManager.beginCameraRendering += Follow;

        private void OnDisable() => RenderPipelineManager.beginCameraRendering -= Follow;

        private void Follow(ScriptableRenderContext context, Camera cam)
        {
            if (cam == null || cam.cameraType == CameraType.Preview)
                return;
            transform.position = cam.transform.position;
        }
    }
}
