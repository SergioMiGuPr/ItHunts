using ItHunts.Artifacts;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Player
{
    /// <summary>
    /// Lanterne du joueur (D-067) : une lumière ponctuelle sans ombres, enfant du joueur, couleur de son personnage. Les renderers
    /// de la vue FPS (mains, artefacts, orbe) passent sur un calque de rendu à part, que la lanterne n'éclaire pas. Réglages
    /// relus à chaque image (Play Mode). En coop, chaque joueur a la sienne (elle est sur son prefab).
    /// Limite : le shader toon de la DA ne teste pas encore les calques de lumière ; les artefacts en toon reçoivent donc
    /// encore la lanterne (demande à la session art : IsMatchingLightLayer dans ItHunts_ToonForwardPass).
    /// </summary>
    [DefaultExecutionOrder(600)]
    public sealed class PlayerLanternLight : MonoBehaviour
    {
        [SerializeField] private PlayerLanternSettings settings;
        [SerializeField] private Light lanternLight;
        [SerializeField] private Transform viewRoot;

        private ArtifactHolder _holder;
        private PlayerMotor _motor;
        private bool _layered;

        public Light Light => lanternLight;

        private void Awake()
        {
            _holder = GetComponent<ArtifactHolder>();
            _motor = GetComponent<PlayerMotor>();
        }

        /// <summary>
        /// Position de la lanterne (pure, testable) : sur le corps affiché, pas sur le corps simulé. Le corps avance par pas fixes
        /// (60/s) et la caméra suit sa position interpolée ; une lanterne enfant du corps avançait donc par à-coups (0 puis 12.5 cm
        /// à 7.5 m/s, une image sur deux, à 130 images/s) : sa tache de lumière sautait sur le sol (vitrine, 2026-11-18).
        /// </summary>
        public static Vector3 FollowBody(Vector3 interpolatedFeet, float bodyYaw, Vector3 offset) =>
            interpolatedFeet + Quaternion.Euler(0f, bodyYaw, 0f) * offset;

        private void Start() => LayerViewmodel();

        /// <summary>Renderers de la vue FPS sur le calque de rendu « vue FPS » seulement ; la lanterne éclaire tous les autres.</summary>
        public void LayerViewmodel()
        {
            if (settings == null || viewRoot == null)
                return;
            uint fps = 1u << settings.fpsRenderingLayer;
            foreach (Renderer r in viewRoot.GetComponentsInChildren<Renderer>(true))
                r.renderingLayerMask = fps;
            _layered = true;
        }

        private void LateUpdate()
        {
            if (lanternLight == null || settings == null)
                return;
            if (!_layered)
                LayerViewmodel();
            lanternLight.enabled = settings.enabled;
            lanternLight.type = LightType.Point;
            lanternLight.shadows = LightShadows.None;
            lanternLight.intensity = settings.intensity;
            lanternLight.range = settings.range;
            if (_motor != null && lanternLight.transform.parent == transform)
                lanternLight.transform.position = FollowBody(_motor.InterpolatedPosition, transform.eulerAngles.y, settings.position);
            else
                lanternLight.transform.localPosition = settings.position;
            CharacterDefinition c = _holder != null ? _holder.Character : null;
            lanternLight.color = c != null ? c.lanternColor : settings.fallbackColor;
            uint all = ~(1u << settings.fpsRenderingLayer);
            lanternLight.renderingLayerMask = (int)all;
            var data = lanternLight.GetComponent<UniversalAdditionalLightData>();
            if (data != null && data.renderingLayers != all)
                data.renderingLayers = all;
        }
    }
}
