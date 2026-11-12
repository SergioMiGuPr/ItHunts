using ItHunts.Movement;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Orbe visible en vue FPS (en bas à droite, comme tenue en main), version greybox : bleue en attraction, orange
    /// en répulsion, terne et plus petite pendant la recharge, neutre quand elle est prête. Trait entre l'orbe et le
    /// point visé pendant la traction. Marquage de la répulsion : un disque au point visé quand une surface est à
    /// portée (orange vif si prête, terne en recharge), et une marque qui s'étend puis s'efface à l'impact.
    /// Sur la racine du joueur : l'orbe et le trait sont sous la caméra (détachée), les marques sont placées dans le monde.
    /// </summary>
    public sealed class MagnetismOrbView : MonoBehaviour
    {
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private LineRenderer line;
        [SerializeField] private Renderer aimMarker;
        [SerializeField] private Renderer impactMarker;
        [SerializeField, Min(0.05f)] private float impactDuration = 0.8f;
        [SerializeField, Min(0f)] private float impactGrowth = 2.5f;
        [SerializeField] private Color attractColor = new Color(0.2f, 0.55f, 1f);
        [SerializeField] private Color repelColor = new Color(1f, 0.5f, 0.1f);
        [SerializeField] private Color readyColor = new Color(0.8f, 0.85f, 0.95f);
        [SerializeField] private Color rechargeColor = new Color(0.16f, 0.16f, 0.18f);
        [Tooltip("Intensité de la couleur en usage (au-dessus de 1 : lueur avec le bloom).")]
        [SerializeField, Min(0f)] private float usageIntensity = 3f;
        [SerializeField, Min(0f)] private float readyIntensity = 0.7f;
        [SerializeField, Range(0.1f, 1f)] private float rechargeScale = 0.75f;
        [SerializeField, Min(0f)] private float lerpSpeed = 25f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private PlayerMotor _motor;
        private MaterialPropertyBlock _block;
        private Vector3 _baseScale;
        private Color _color;
        private float _scale = 1f;
        private Vector3 _aimMarkerScale;
        private Vector3 _impactScale;
        private int _seenRepelTick = MovementState.NeverTick;
        private float _impactAge = float.PositiveInfinity;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _block = new MaterialPropertyBlock();
            if (orbRenderer != null)
                _baseScale = orbRenderer.transform.localScale;
            if (line != null)
                line.enabled = false;
            if (aimMarker != null)
            {
                _aimMarkerScale = aimMarker.transform.localScale;
                aimMarker.enabled = false;
            }

            if (impactMarker != null)
            {
                _impactScale = impactMarker.transform.localScale;
                impactMarker.enabled = false;
            }

            _seenRepelTick = MovementState.NeverTick;
            _color = readyColor * readyIntensity;
        }

        private void LateUpdate()
        {
            if (_motor == null || _motor.MagnetismSettings == null || orbRenderer == null)
                return;

            MagnetismState m = _motor.Magnetism;
            MovementState s = _motor.State;
            Color modeColor = m.LastMode == MagnetMode.Attraction ? attractColor : repelColor;
            Color target;
            float targetScale = 1f;
            switch (m.Status)
            {
                case OrbStatus.EnUsage:
                    target = modeColor * usageIntensity;
                    break;
                case OrbStatus.Recharge:
                    target = Color.Lerp(rechargeColor, modeColor, 0.2f);
                    targetScale = rechargeScale;
                    break;
                default:
                    target = readyColor * readyIntensity;
                    break;
            }

            // L'allumage est immédiat ; le retour à l'état prêt est un peu adouci.
            float k = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
            _color = m.Status == OrbStatus.EnUsage ? target : Color.Lerp(_color, target, k);
            _scale = Mathf.Lerp(_scale, targetScale, k);
            _block.SetColor(BaseColorId, _color);
            orbRenderer.SetPropertyBlock(_block);
            orbRenderer.transform.localScale = _baseScale * _scale;

            UpdateRepelMarkers(m);

            if (line != null)
            {
                bool tracting = s.Mode == MovementMode.Traction;
                line.enabled = tracting;
                if (tracting)
                {
                    line.SetPosition(0, orbRenderer.transform.position);
                    line.SetPosition(1, s.TractionPoint);
                    Color c = attractColor * usageIntensity;
                    line.startColor = c;
                    line.endColor = c;
                }
            }
        }

        private void UpdateRepelMarkers(in MagnetismState m)
        {
            if (aimMarker != null)
            {
                bool show = m.RepelInRange;
                aimMarker.enabled = show;
                if (show)
                {
                    Place(aimMarker.transform, m.AimPoint, m.AimNormal);
                    bool ready = m.RepelStatus == OrbStatus.Prete;
                    aimMarker.transform.localScale = _aimMarkerScale * (ready ? 1f : 0.6f);
                    SetColor(aimMarker, ready ? repelColor * 1.5f : Color.Lerp(rechargeColor, repelColor, 0.25f));
                }
            }

            if (impactMarker == null)
                return;
            if (m.LastRepelTick != MovementState.NeverTick && m.LastRepelTick != _seenRepelTick)
            {
                _seenRepelTick = m.LastRepelTick;
                _impactAge = 0f;
                Place(impactMarker.transform, m.LastRepelPoint, m.LastRepelNormal);
            }

            bool visible = _impactAge < impactDuration;
            impactMarker.enabled = visible;
            if (!visible)
                return;
            float t = _impactAge / impactDuration;
            impactMarker.transform.localScale = _impactScale * (1f + impactGrowth * t);
            SetColor(impactMarker, Color.Lerp(repelColor * usageIntensity, rechargeColor, t));
            _impactAge += Time.deltaTime;
        }

        /// <summary>Disque posé sur la surface (axe Y = normale), juste devant pour éviter le scintillement.</summary>
        private static void Place(Transform marker, Vector3 point, Vector3 normal)
        {
            if (normal.sqrMagnitude < 1e-6f)
                normal = Vector3.up;
            marker.SetPositionAndRotation(point + normal * 0.02f, Quaternion.FromToRotation(Vector3.up, normal));
        }

        private void SetColor(Renderer r, Color c)
        {
            _block.SetColor(BaseColorId, c);
            r.SetPropertyBlock(_block);
        }
    }
}
