using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Player
{
    /// <summary>
    /// Vue première personne, option 2 (D-028) : efface la profondeur juste avant la passe « Render Objects » qui dessine
    /// les mains (champ de vision 60°) puis le HUD, après les transparents du monde. Les mains passent ainsi devant tout,
    /// sans jamais rentrer dans les murs, sans caméra en plus. Seulement pour les caméras de jeu (pas la vue Scène).
    /// La profondeur du monde n'est plus utilisée ensuite (le post-process ne lit que la couleur).
    /// </summary>
    public sealed class FirstPersonDepthClearFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;

        private sealed class ClearDepthPass : ScriptableRenderPass
        {
            private sealed class PassData
            {
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("ItHunts : profondeur effacée (mains)", out _);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                    context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1f, 0u));
            }
        }

        private ClearDepthPass _pass;

        public override void Create() => _pass = new ClearDepthPass { renderPassEvent = passEvent };

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
                renderer.EnqueuePass(_pass);
        }
    }
}
