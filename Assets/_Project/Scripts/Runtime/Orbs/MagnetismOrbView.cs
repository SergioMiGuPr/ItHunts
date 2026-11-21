using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Retour visuel de l'orbe unique (couleur, D-024) et du magnétisme (greybox), réglé par MagnetismFeedbackSettings et
    /// TelekinesisFeedbackSettings. Les couleurs HDR brillent avec le bloom.
    /// - Orbe en bas à droite de la vue, une couleur par action (règle : OrbLook) : bleue en traction, orange à la
    ///   répulsion, violette quand un objet est tenu, verte au lancer ; terne et plus petite sans charge d'attraction,
    ///   neutre quand elle est prête.
    /// - Traction : trait de l'orbe au point, ancre bleue au point (tant qu'on tire), éclat bleu au départ.
    /// - Répulsion : disque orange au point visé quand une surface est à portée (terne en recharge), éclat orange à
    ///   l'impact.
    /// Sur la racine du joueur : l'orbe et le trait sont sous la caméra (détachée), les marques sont placées dans le monde.
    /// </summary>
    public sealed class MagnetismOrbView : MonoBehaviour
    {
        [SerializeField] private MagnetismFeedbackSettings settings;
        [Tooltip("Couleurs de la télékinésie (objet tenu : violet ; lancer : vert).")]
        [SerializeField] private TelekinesisFeedbackSettings tkFeedback;
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
        private FirstPersonView _view;
        private Camera _camera;
        private MaterialPropertyBlock _block;
        private Vector3 _baseScale, _aimMarkerScale, _anchorScale;
        private Color _color;
        private float _scale = 1f;
        private readonly Flash _repelFlash = new Flash();
        private readonly Flash _attractFlash = new Flash();
        private int _seenRepelTick = MovementState.NeverTick;
        private int _seenTractionTick = MovementState.NeverTick;
        private ParticleSystem _stream;
        private GameObject _tractionArt;
        private ItHunts.Art.VFX.FluxVfx _flux;
        private bool _fluxOpen;
        private float _streamCarry;

        /// <summary>Flot de traction actif (tests, debug).</summary>
        public bool StreamActive => (_stream != null && _stream.particleCount > 0) || _fluxOpen;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _view = GetComponent<FirstPersonView>();
            var playerInput = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            _camera = playerInput != null ? playerInput.camera : null;
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
            UpdateOrb(m, s);
            UpdateTraction(m, s);
            UpdateRepel(m);
        }

        private void UpdateOrb(in MagnetismState m, in MovementState s)
        {
            int flashTicks = MovementSettings.SecondsToTicks(settings.orbFlashDuration, Time.fixedDeltaTime);
            OrbLookKind look = OrbLook.Resolve(s, m, _motor.Telekinesis, flashTicks);
            Color grab = tkFeedback != null ? tkFeedback.grabColor : new Color(0.6f, 0.25f, 1f);
            Color thrown = tkFeedback != null ? tkFeedback.throwColor : new Color(0.25f, 1f, 0.35f);
            Color target;
            float targetScale = 1f;
            switch (look)
            {
                case OrbLookKind.Lancer:
                    target = thrown * settings.orbUsageIntensity;
                    break;
                case OrbLookKind.Repulsion:
                    target = settings.repelColor * settings.orbUsageIntensity;
                    break;
                case OrbLookKind.Traction:
                    target = settings.attractColor * settings.orbUsageIntensity;
                    break;
                case OrbLookKind.Tenu:
                    target = grab * settings.orbUsageIntensity;
                    break;
                case OrbLookKind.Recharge:
                    target = Color.Lerp(settings.rechargeColor, settings.attractColor, 0.2f);
                    targetScale = rechargeScale;
                    break;
                default:
                    target = settings.readyColor * settings.orbReadyIntensity;
                    break;
            }

            bool lit = look != OrbLookKind.Prete && look != OrbLookKind.Recharge;
            // L'allumage est immédiat ; le retour à l'état prêt est un peu adouci.
            float k = 1f - Mathf.Exp(-lerpSpeed * Time.deltaTime);
            _color = lit ? target : Color.Lerp(_color, target, k);
            _scale = Mathf.Lerp(_scale, targetScale, k);
            SetColor(orbRenderer, _color);
            orbRenderer.transform.localScale = _baseScale * _scale;
        }

        private void UpdateTraction(in MagnetismState m, in MovementState s)
        {
            bool tracting = s.Mode == MovementMode.Traction;
            Color glow = settings.attractColor * settings.lineIntensity;
            UpdateStream(tracting, s.TractionPoint);
            if (line != null)
            {
                line.enabled = tracting && settings.showTractionLine;
                if (tracting)
                {
                    line.SetPosition(0, _view != null ? _view.LineStart : orbRenderer.transform.position);
                    line.SetPosition(1, s.TractionPoint);
                    line.startColor = glow;
                    line.endColor = glow;
                }
            }

            if (anchorMarker != null)
            {
                anchorMarker.enabled = tracting && settings.showTractionMarks;
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
                if (settings.showTractionMarks)
                    Trigger(_attractFlash, s.TractionPoint, m.TractionNormal);
            }

            Animate(_attractFlash, settings.attractColor, settings.tractionFlashDuration, settings.tractionFlashGrowth);
        }

        private void UpdateRepel(in MagnetismState m)
        {
            if (aimMarker != null)
            {
                bool show = m.RepelInRange && settings.showRepelAimMarker;
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
                if (settings.showRepelImpactFlash)
                    Trigger(_repelFlash, m.LastRepelPoint, m.LastRepelNormal);
                RepelBurst(m.LastRepelPoint, m.LastRepelNormal);
            }

            Animate(_repelFlash, settings.repelColor, settings.repelFlashDuration, settings.repelFlashGrowth);
        }

        /// <summary>Départ du flot : la main de l'orbe (vue FPS ramenée dans le monde), sinon l'orbe.</summary>
        private Vector3 StreamStart => _view != null ? _view.LineStart : orbRenderer.transform.position;

        /// <summary>
        /// Traction (point 8) : un flot qui file de l'orbe vers le point visé. Effet de la DA s'il est fourni (posé à l'orbe, tourné
        /// vers le point), sinon des bouffées de fumée peinte (matériau de la DA, teinte de la traction) émises à la main et lancées vers
        /// le point, qui s'éteignent en l'atteignant.
        /// </summary>
        private void UpdateStream(bool tracting, Vector3 point)
        {
            Vector3 from = StreamStart;
            if (settings.tractionVfx != null)
            {
                // VFX_Traction (PR #35) : FluxVfx, de la main de l'orbe au point visé ; Ferme laisse finir les particules parties.
                if (_tractionArt == null && tracting)
                {
                    _tractionArt = Instantiate(settings.tractionVfx, from, Quaternion.identity);
                    _flux = _tractionArt.GetComponent<ItHunts.Art.VFX.FluxVfx>();
                }

                if (_flux != null)
                {
                    if (tracting)
                    {
                        _tractionArt.transform.position = from;
                        _flux.Regle(from, point);
                        if (!_fluxOpen)
                            _flux.Ouvre();
                        _fluxOpen = true;
                    }
                    else if (_fluxOpen)
                    {
                        _flux.Ferme();
                        _fluxOpen = false;
                    }
                }

                return;
            }

            if (!tracting && _stream == null)
                return;
            if (_stream == null)
                _stream = CreateStream();
            if (!tracting)
                return;                                                  // les bouffées en vol finissent leur course
            Vector3 d = point - from;
            float dist = d.magnitude;
            if (dist < 0.5f)
                return;
            _streamCarry += settings.tractionStreamRate * Time.deltaTime * ItHunts.Player.GameFeel.Intensity;
            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            while (_streamCarry >= 1f)
            {
                _streamCarry -= 1f;
                float speed = settings.tractionStreamSpeed * Random.Range(0.85f, 1.15f);
                p.position = from + Random.insideUnitSphere * 0.08f;
                p.velocity = (d / dist) * speed + Random.insideUnitSphere * 0.6f;
                p.startLifetime = dist / speed;
                p.startSize = Random.Range(settings.tractionStreamSize.x, settings.tractionStreamSize.y);
                p.rotation = Random.Range(0f, 360f);
                _stream.Emit(p, 1);
            }
        }

        private ParticleSystem CreateStream()
        {
            var go = new GameObject("Traction : flot vers le point");
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;
            main.startColor = Color.white;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.4f)));
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0.3f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (settings.tractionStreamMaterial != null)
            {
                r.sharedMaterial = settings.tractionStreamMaterial;
                var block = new MaterialPropertyBlock();                // teinte de la traction, le matériau de la DA reste intact
                block.SetColor("_Ton1", settings.tractionStreamTone * 0.35f);
                block.SetColor("_Ton2", settings.tractionStreamTone * 0.65f);
                block.SetColor("_Ton3", settings.tractionStreamTone);
                r.SetPropertyBlock(block);
            }

            ps.Play();
            return ps;
        }

        /// <summary>Répulsion (point 8) : effet bref de la DA devant l'orbe, réduit ; la secousse vient des sensations de jeu.</summary>
        private void RepelBurst(Vector3 point, Vector3 normal)
        {
            if (settings.repelVfx != null && ItHunts.Player.GameFeel.Intensity > 0.01f)
            {
                // VFX_Repulsion (PR #35) : onde courte au point visé, +Z = normale de la surface.
                ItHunts.Combat.GameVfx.Spawn(settings.repelVfx, point + normal * 0.03f,
                    Quaternion.LookRotation(normal.sqrMagnitude > 0.01f ? normal : Vector3.up), settings.repelVfxScale, null, settings.repelVfxLife);
                return;
            }

            Vector3 from = StreamStart;
            Vector3 dir = point - from;
            Quaternion rot = dir.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(dir) : Quaternion.identity;
            GameObject prefab = null;
            if (prefab == null)
            {
                GameVfxSettings vfx = ItHunts.Combat.GameVfx.Settings;
                prefab = vfx != null ? vfx.sparks : null;
            }

            if (prefab != null && ItHunts.Player.GameFeel.Intensity > 0.01f)
                ItHunts.Combat.GameVfx.Spawn(prefab, from + (dir.sqrMagnitude > 1e-4f ? dir.normalized * 0.6f : Vector3.zero), rot,
                    settings.repelVfxScale, null, settings.repelVfxLife);
        }

        private void OnDestroy()
        {
            if (_stream != null)
                Destroy(_stream.gameObject);
            if (_tractionArt != null)
                Destroy(_tractionArt);
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
            Vector3 eye = _camera != null ? _camera.transform.position : transform.position + Vector3.up * 1.6f;
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
