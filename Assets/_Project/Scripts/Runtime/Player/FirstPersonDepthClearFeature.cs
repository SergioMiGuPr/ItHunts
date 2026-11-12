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
    /// Coupe aussi l'occlusion ambiante (SSAO) pour la suite de l'image : elle est calculée en espace écran à partir du
    /// décor, donc les mains, dessinées par-dessus, recevaient l'ombre du mur ou du sol situé derrière elles (taches
    /// sombres). La passe SSAO la rallume à l'image suivante (et pour la caméra de l'autre joueur).
    /// </summary>
    public sealed class FirstPersonDepthClearFeature : ScriptableRendererFeature
    {
        [SerializeField] private RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
        [SerializeField, Tooltip("Les mains reçoivent l'occlusion ambiante du décor situé derrière elles (non par défaut).")]
        private bool handsReceiveAmbientOcclusion;

        private sealed class ClearDepthPass : ScriptableRenderPass
        {
            public bool KeepAmbientOcclusion;
            public GlobalKeyword ScreenSpaceOcclusion;   // créé dans Create() (interdit pendant la désérialisation)

            private sealed class PassData
            {
                public bool KeepAmbientOcclusion;
                public GlobalKeyword Keyword;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resourceData = frameData.Get<UniversalResourceData>();
                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass<PassData>("ItHunts : profondeur effacée (mains)", out PassData data);
                data.KeepAmbientOcclusion = KeepAmbientOcclusion;
                data.Keyword = ScreenSpaceOcclusion;
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((PassData d, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(RTClearFlags.Depth, Color.clear, 1f, 0u);
                    if (!d.KeepAmbientOcclusion)
                        context.cmd.SetKeyword(d.Keyword, false);
                });
            }
        }

        private ClearDepthPass _pass;

        public bool HandsReceiveAmbientOcclusion => handsReceiveAmbientOcclusion;

        public override void Create() => _pass = new ClearDepthPass
        {
            renderPassEvent = passEvent,
            KeepAmbientOcclusion = handsReceiveAmbientOcclusion,
            ScreenSpaceOcclusion = GlobalKeyword.Create(ShaderKeywordStrings.ScreenSpaceOcclusion),
        };

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
                renderer.EnqueuePass(_pass);
        }
    }
}
