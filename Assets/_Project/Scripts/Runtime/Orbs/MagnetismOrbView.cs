using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Retour visuel de l'orbe de magnétisme (greybox), réglé par MagnetismFeedbackSettings. Les couleurs HDR brillent
    /// avec le bloom.
    /// - Orbe en bas à droite de la vue : bleue en traction, orange à la répulsion, terne et plus petite en recharge,
    ///   neutre quand elle est prête.
    /// - Traction : trait de l'orbe au point, ancre bleue au point (tant qu'on tire), éclat bleu au départ.
    /// - Répulsion : disque orange au point visé quand une surface est à portée (terne en recharge), éclat orange à
    ///   l'impact.
    /// Sur la racine du joueur : l'orbe et le trait sont sous la caméra (détachée), les marques sont placées dans le monde.
    /// </summary>
    public sealed class MagnetismOrbView : MonoBehaviour
    {
        [SerializeField] private MagnetismFeedbackSettings settings;
        [SerializeField] private Renderer orbRenderer;
        [SerializeField] private LineRenderer line;
        [SerializeField] private Renderer aimMarker;
        [SerializeField] private Renderer impactMarker;
        [SerializeField] private Renderer anchorMarker;
        [SerializeField] private Renderer tractionFlash;
        [SerializeField, Range(0.1f, 1f)] private float rechargeScale = 0.75f;
        [SerializeField, Min(0f)] private float lerpSpeed = 25f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private sealed class Flash
        {
            public Renderer Renderer;
            public Vector3 BaseScale;
            public float Age = float.PositiveInfinity;
            public Vector3 Point;
        }

        private PlayerMotor _motor;
        private MaterialPropertyBlock _block;
        private Vector3 _baseScale, _aimMarkerScale, _anchorScale;
        private Color _color;
        private float _scale = 1f;
        private readonly Flash _repelFlash = new Flash();
        private readonly Flash _attractFlash = new Flash();
        private int _seenRepelTick = MovementState.NeverTick;
        private int _seenTractionTick = MovementState.NeverTick;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _block = new MaterialPropertyBlock();
            if (orbRenderer != null)
                _baseScale = orbRenderer.transform.localScale;
            if (line != null)
                line.enabled = false;
            _aimMarkerScale = Hide(aimMarker);
            _anchorScale = Hide(anchorMarker);
            _repelFlash.Renderer = impactMarker;
            _repelFlash.BaseScale = Hide(impactMarker);
            _attractFlash.Renderer = tractionFlash;
            _attractFlash.BaseScale = Hide(tractionFlash);
            _color = settings != null ? settings.readyColor * settings.orbReadyIntensity : Color.white;
        }

        private void Start()
        {
            // Pas d'éclat pour l'état de départ.
            _seenRepelTick = _motor.Magnetism.LastRepelTick;
            _seenTractionTick = _motor.Magnetism.LastTractionStartTick;
        }

        private static Vector3 Hide(Renderer r)
        {
            if (r == null)
                return Vector3.one;
            r.enabled = false;
            return r.transform.localScale;
        }

        private void LateUpdate()
        {
            if (_motor == null || settings == null || _motor.MagnetismSettings == null || orbRenderer == null)
                return;

            MagnetismState m = _motor.Magnetism;
            MovementState s = _motor.State;
            bool active = _motor.ActiveOrb == ActiveOrb.Magnetisme;
            if (active)
                UpdateOrb(m);                   // sinon, la couleur de l'orbe vient de TelekinesisView
            UpdateTraction(m, s);
            UpdateRepel(m, active);
        }

        private void UpdateOrb(in MagnetismState m)
        {
            Color modeColor = m.LastMode == MagnetMode.Attraction ? settings.attractColor : settings.repelColor;
            Color target;
            float targetScale = 1f;
            switch (m.Status)
            {
                case OrbStatus.EnUsage:
                    target = modeColor * settings.orbUsageIntensity;
                    break;
                case OrbStatus.Recharge:
                    target = Color.Lerp(settings.rechargeColor, modeColor, 0.2f);
                    targetScale = rechargeScale;
                    break;
                default:
                    target = settings.readyColor * settings.orbReadyIntensity;
                    break;
            }

            // L'allumage est immédiat ; le retour à l'état prêt est un peu adouci.
            float k = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
            _color = m.Status == OrbStatus.EnUsage ? target : Color.Lerp(_color, target, k);
            _scale = Mathf.Lerp(_scale, targetScale, k);
            SetColor(orbRenderer, _color);
            orbRenderer.transform.localScale = _baseScale * _scale;
        }

        private void UpdateTraction(in MagnetismState m, in MovementState s)
        {
            bool tracting = s.Mode == MovementMode.Traction;
            Color glow = settings.attractColor * settings.lineIntensity;
            if (line != null)
            {
                line.enabled = tracting;
                if (tracting)
                {
                    line.SetPosition(0, orbRenderer.transform.position);
                    line.SetPosition(1, s.TractionPoint);
                    line.startColor = glow;
                    line.endColor = glow;
                }
            }

            if (anchorMarker != null)
            {
                anchorMarker.enabled = tracting;
                if (tracting)
                {
                    Place(anchorMarker.transform, s.TractionPoint, m.TractionNormal);
                    float pulse = 1f + 0.15f * Mathf.Sin(Time.time * 20f);
                    anchorMarker.transform.localScale = _anchorScale * (pulse * DistanceScale(s.TractionPoint));
                    SetColor(anchorMarker, settings.attractColor * settings.anchorIntensity);
                }
            }

            if (m.LastTractionStartTick != MovementState.NeverTick && m.LastTractionStartTick != _seenTractionTick)
            {
                _seenTractionTick = m.LastTractionStartTick;
                Trigger(_attractFlash, s.TractionPoint, m.TractionNormal);
            }

            Animate(_attractFlash, settings.attractColor, settings.tractionFlashDuration, settings.tractionFlashGrowth);
        }

        private void UpdateRepel(in MagnetismState m, bool active)
        {
            if (aimMarker != null)
            {
                bool show = active && m.RepelInRange;
                aimMarker.enabled = show;
                if (show)
                {
                    Place(aimMarker.transform, m.AimPoint, m.AimNormal);
                    bool ready = m.RepelStatus == OrbStatus.Prete;
                    aimMarker.transform.localScale = _aimMarkerScale * ((ready ? 1f : 0.6f) * DistanceScale(m.AimPoint));
                    SetColor(aimMarker, ready
                        ? settings.repelColor * settings.repelAimMarkerIntensity
                        : Color.Lerp(settings.rechargeColor, settings.repelColor, 0.25f));
                }
            }

            if (m.LastRepelTick != MovementState.NeverTick && m.LastRepelTick != _seenRepelTick)
            {
                _seenRepelTick = m.LastRepelTick;
                Trigger(_repelFlash, m.LastRepelPoint, m.LastRepelNormal);
            }

            Animate(_repelFlash, settings.repelColor, settings.repelFlashDuration, settings.repelFlashGrowth);
        }

        private static void Trigger(Flash flash, Vector3 point, Vector3 normal)
        {
            if (flash.Renderer == null)
                return;
            flash.Age = 0f;
            flash.Point = point;
            Place(flash.Renderer.transform, point, normal);
        }

        /// <summary>Éclat : s'étend et passe de la couleur vive au sombre, puis disparaît.</summary>
        private void Animate(Flash flash, Color color, float duration, float growth)
        {
            if (flash.Renderer == null)
                return;
            bool visible = flash.Age < duration;
            flash.Renderer.enabled = visible;
            if (!visible)
                return;
            float t = flash.Age / duration;
            flash.Renderer.transform.localScale = flash.BaseScale * ((1f + growth * t) * DistanceScale(flash.Point));
            SetColor(flash.Renderer, Color.Lerp(color * settings.flashIntensity, settings.rechargeColor, t));
            flash.Age += Time.deltaTime;
        }

        /// <summary>Grandit au-delà de markerReferenceDistance pour garder la même taille à l'écran.</summary>
        private float DistanceScale(Vector3 point)
        {
            Vector3 eye = orbRenderer.transform.parent != null ? orbRenderer.transform.parent.position : transform.position;
            return Mathf.Max(1f, Vector3.Distance(eye, point) / settings.markerReferenceDistance);
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
