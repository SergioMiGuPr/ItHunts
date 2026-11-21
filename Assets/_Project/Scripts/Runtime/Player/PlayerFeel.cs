using ItHunts.Combat;
using ItHunts.Movement;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Player
{
    /// <summary>
    /// Sensations de jeu d'un joueur (points 4 et 9 du 2026-11-21) : lit l'état de son mouvement et les événements qui le touchent,
    /// pose les mouvements de caméra (hissage, plongeon d'atterrissage) et les secousses sur sa CameraShake, le coup de FOV du dash
    /// sur son PlayerLook, les traînées de vitesse autour de sa vue, le flou de vitesse (solo). Affichage seulement ; un par joueur.
    /// Tout suit « Intensité des effets » et « Réduire les secousses » (Paramètres).
    /// </summary>
    [DefaultExecutionOrder(-45)]                                         // après PlayerLook (−50), avant CameraShake (−40)
    public sealed class PlayerFeel : MonoBehaviour
    {
        [SerializeField] private GameFeelSettings settings;

        private PlayerMotor _motor;
        private CameraShake _shake;
        private PlayerLook _look;
        private PlayerHealth _health;
        private PlayerInput _input;
        private int _lastMantleTick = int.MinValue;
        private float _mantleT = -1f;
        private float _mantleSide = 1f;
        private int _seenLanding, _seenDashTick = int.MinValue, _seenRepelTick = int.MinValue;
        private float _landT = -1f, _landK;
        private float _fovPunch;
        private ParticleSystem _trails;
        private ItHunts.Art.VFX.VitesseVfx _edges;

        private static Volume _blurVolume;
        private static MotionBlur _blur;

        public GameFeelSettings Settings => settings;

        /// <summary>Mouvement de caméra en cours (debug, tests) : décalage local (m) et angles (°).</summary>
        public Vector3 Offset { get; private set; }
        public Vector3 Euler { get; private set; }
        /// <summary>Traînées par seconde et intensité du flou cette image (debug, mesures).</summary>
        public float TrailRate { get; private set; }
        public static float BlurIntensity => _blur != null ? _blur.intensity.value : 0f;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _shake = GetComponent<CameraShake>();
            _look = GetComponent<PlayerLook>();
            _health = GetComponent<PlayerHealth>();
            _input = GetComponent<PlayerInput>();
            if (_shake != null)
                _shake.Feel = settings;
            if (GameFeel.Settings == null)
                GameFeel.Settings = settings;
        }

        private void Start()
        {
            if (_motor == null)
                return;
            _seenLanding = _motor.LandingCount;
            _seenDashTick = _motor.State.LastDashTick;
            _seenRepelTick = _motor.Magnetism.LastRepelTick;
        }

        private void OnEnable()
        {
            PlayerHealth.Hurt += OnHurt;
            ParryEvents.Parried += OnParried;
            ItHunts.Enemies.StalkerAudio.Cry += OnStalkerCry;
        }

        private void OnDisable()
        {
            PlayerHealth.Hurt -= OnHurt;
            ParryEvents.Parried -= OnParried;
            ItHunts.Enemies.StalkerAudio.Cry -= OnStalkerCry;
            if (_look != null)
                _look.FovPunch = 0f;
        }

        private void OnDestroy()
        {
            if (_trails != null)
                Destroy(_trails.gameObject);
            if (_edges != null)
                Destroy(_edges.gameObject);
        }

        // ---------- Courbes (pures, testables) ----------

        /// <summary>
        /// Courbe du hissage à l'instant t (s) : plongeon (descente, tangage vers le bas, roulis) puis remontée avec un léger
        /// dépassement ; tout revient à 0 à la fin.
        /// </summary>
        public static void MantleCurve(GameFeelSettings s, float t, float side, out Vector3 offset, out Vector3 euler)
        {
            offset = Vector3.zero;
            euler = Vector3.zero;
            if (s == null || t < 0f || t >= s.mantleCameraTime)
                return;
            float p = t / s.mantleCameraTime;
            float dip = s.mantleDipShare;
            float q = Mathf.Clamp01((p - dip) / (1f - dip));
            float y = p < dip
                ? -s.mantleDipDepth * Mathf.Sin(0.5f * Mathf.PI * p / dip)                                   // descente
                : Mathf.Lerp(-s.mantleDipDepth, 0f, 1f - (1f - q) * (1f - q)) + s.mantleRiseOvershoot * Mathf.Sin(Mathf.PI * q);   // remontée
            float bell = Mathf.Sin(Mathf.PI * p);
            offset = new Vector3(0f, y, 0f);
            euler = new Vector3(s.mantlePitch * bell, 0f, s.mantleRoll * bell * side);
        }

        /// <summary>Plongeon d'atterrissage (k : force 0 à 1 selon la vitesse de chute) : descente rapide, remontée douce.</summary>
        public static void LandingCurve(GameFeelSettings s, float t, float k, out Vector3 offset, out Vector3 euler)
        {
            offset = Vector3.zero;
            euler = Vector3.zero;
            if (s == null || t < 0f || t >= s.landDipTime || k <= 0f)
                return;
            float p = t / s.landDipTime;
            float shape = p < 0.25f ? Mathf.Sin(0.5f * Mathf.PI * p / 0.25f) : 1f - Mathf.SmoothStep(0f, 1f, (p - 0.25f) / 0.75f);
            offset = new Vector3(0f, -s.landDipDepth * k * shape, 0f);
            euler = new Vector3(s.landDipPitch * k * shape, 0f, 0f);
        }

        // ---------- Événements ----------

        private void OnHurt(PlayerHurtEvent e)
        {
            if (settings == null || _shake == null || e.Victim == null || e.Victim != _health)
                return;
            _shake.AddTrauma(settings.hurtTrauma * Mathf.Clamp(e.Damage / settings.hurtTraumaDamage, 0.4f, 1f));
        }

        private void OnParried(ParryEvent e)
        {
            if (settings == null || e.Player != transform)
                return;
            if (_shake != null)
                _shake.AddTrauma(settings.parryTrauma);
            HitStop.Request(settings.parryHitStop);
        }

        private void OnStalkerCry(Vector3 at)
        {
            if (settings == null || _shake == null)
                return;
            float d = Vector3.Distance(at, transform.position);
            _shake.AddTrauma(settings.stalkerCryTrauma * (1f - GameFeel.Ramp(d, 0f, settings.stalkerCryRange)));
        }

        // ---------- Image par image ----------

        private void Update()
        {
            if (_motor == null || settings == null)
                return;
            float dt = Time.unscaledDeltaTime;
            float scale = GameFeel.MotionScale(settings);
            Vector3 offset = Vector3.zero, euler = Vector3.zero;
            MovementState s = _motor.State;

            // Hissage : départ détecté au tick du hissage.
            if (settings.mantleCamera && s.LastMantleTick != _lastMantleTick && s.Mode == MovementMode.Mantle)
            {
                _lastMantleTick = s.LastMantleTick;
                _mantleT = 0f;
                _mantleSide = Random.value < 0.5f ? -1f : 1f;
            }

            if (_mantleT >= 0f)
            {
                MantleCurve(settings, _mantleT, _mantleSide, out Vector3 o, out Vector3 e);
                offset += o;
                euler += e;
                _mantleT += Time.deltaTime;
                if (_mantleT >= settings.mantleCameraTime)
                    _mantleT = -1f;
            }

            // Atterrissage : plongeon selon la vitesse de chute, secousse au-delà d'un atterrissage fort.
            if (_motor.LandingCount != _seenLanding)
            {
                _seenLanding = _motor.LandingCount;
                float fall = _motor.LastLandingSpeed;
                float k = GameFeel.Ramp(fall, settings.landDipSpeedMin, settings.landDipSpeedFull);
                if (k > 0f && _mantleT < 0f)
                {
                    _landT = 0f;
                    _landK = k;
                }

                if (_shake != null)
                    _shake.AddTrauma(settings.hardLandingTrauma * GameFeel.Ramp(fall, settings.hardLandingSpeedMin, settings.hardLandingSpeedFull));
                // Poussière de la DA aux pieds (PR #35) : 0.6 petit saut, 1 chute normale, 1.5 grande chute.
                GameVfxSettings fx = ItHunts.Combat.GameVfx.Settings;
                if (fx != null && fx.playerDust != null && fall >= settings.landDipSpeedMin && GameFeel.Intensity > 0.01f)
                    ItHunts.Combat.GameVfx.Spawn(fx.playerDust, transform.position + Vector3.up * 0.05f, Quaternion.identity,
                        Mathf.Lerp(0.6f, 1.5f, GameFeel.Ramp(fall, settings.landDipSpeedMin, settings.hardLandingSpeedFull)), null, 2.5f);
            }

            if (_landT >= 0f)
            {
                LandingCurve(settings, _landT, _landK, out Vector3 o, out Vector3 e);
                offset += o;
                euler += e;
                _landT += Time.deltaTime;
                if (_landT >= settings.landDipTime)
                    _landT = -1f;
            }

            // Dash : coup de FOV ; répulsion : petite secousse.
            if (s.LastDashTick != _seenDashTick)
            {
                _seenDashTick = s.LastDashTick;
                _fovPunch = settings.dashFovPunch;
            }

            _fovPunch = Mathf.MoveTowards(_fovPunch, 0f, settings.fovPunchDecay * dt);
            if (_look != null)
                _look.FovPunch = _fovPunch * GameFeel.Intensity;
            if (_motor.Magnetism.LastRepelTick != _seenRepelTick)
            {
                _seenRepelTick = _motor.Magnetism.LastRepelTick;
                if (_shake != null)
                    _shake.AddTrauma(settings.repelTrauma);
            }

            Offset = offset * scale;
            Euler = euler * scale;
            if (_shake != null)
            {
                _shake.FeelOffset = Offset;
                _shake.FeelEuler = Euler;
            }

            float speed = s.HorizontalSpeed;
            UpdateTrails(speed);
            UpdateBlur(speed);
        }

        /// <summary>Traits qui filent autour de la vue (repère de la caméra) au-delà de trailSpeedMin.</summary>
        private void UpdateTrails(float speed)
        {
            float k = settings.speedTrails ? GameFeel.Ramp(speed, settings.trailSpeedMin, settings.trailSpeedFull) * GameFeel.Intensity : 0f;
            TrailRate = settings.trailRate * k;
            // Traits de bord d'écran de la DA (PR #35), à la place des lignes provisoires : mêmes seuils, force = intensité.
            GameVfxSettings fx = ItHunts.Combat.GameVfx.Settings;
            if (fx != null && fx.speedEdges != null && _look != null && _look.ViewRig != null)
            {
                if (_edges == null)
                {
                    GameObject go = Instantiate(fx.speedEdges, _look.ViewRig, false);
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localRotation = Quaternion.identity;
                    _edges = go.GetComponent<ItHunts.Art.VFX.VitesseVfx>();
                }

                if (_edges != null)
                {
                    _edges.seuilBas = settings.trailSpeedMin;
                    _edges.seuilHaut = settings.trailSpeedFull;
                    _edges.force = settings.speedTrails ? GameFeel.Intensity : 0f;
                    _edges.Regle(speed);
                    return;
                }
            }

            if (_trails == null)
            {
                if (k <= 0f || _look == null || _look.ViewRig == null)
                    return;
                _trails = CreateTrails(_look.ViewRig, settings);
            }

            ParticleSystem.EmissionModule em = _trails.emission;
            em.rateOverTime = TrailRate;
        }

        private static ParticleSystem CreateTrails(Transform rig, GameFeelSettings s)
        {
            var go = new GameObject("Traînées de vitesse");
            go.transform.SetParent(rig, false);
            go.transform.localPosition = new Vector3(0f, 0f, 4f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 0.2f;                                    // lignes de vitesse : projetées vers l'extérieur de la vue
            main.startSpeed = 0f;
            main.startSize = s.trailWidth;
            main.startColor = s.trailColor;
            main.maxParticles = 120;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 1.3f;
            shape.donutRadius = 0.5f;
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(0f);
            vel.y = new ParticleSystem.MinMaxCurve(0f);
            vel.z = new ParticleSystem.MinMaxCurve(-16f, -12f);           // vers la caméra…
            vel.radial = new ParticleSystem.MinMaxCurve(22f, 30f);        // …et surtout vers les bords : traits étirés en rayons
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.4f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0.4f, 1f) });
            col.color = g;
            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.055f;
            r.lengthScale = 1f;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (s.trailMaterial != null)
            {
                r.sharedMaterial = s.trailMaterial;
                var block = new MaterialPropertyBlock();                  // teinte des traits sans toucher au matériau de la DA
                block.SetColor("_Ton1", s.trailTone * 0.45f);
                block.SetColor("_Ton2", s.trailTone * 0.75f);
                block.SetColor("_Ton3", s.trailTone);
                r.SetPropertyBlock(block);
            }
            ps.Play();
            return ps;
        }

        /// <summary>Flou de vitesse (solo) : Motion Blur d'URP, caméra seule, qualité basse, au-delà de blurSpeedMin.</summary>
        private void UpdateBlur(float speed)
        {
            if (_input != null && _input.playerIndex > 0)
                return;                                                  // le premier joueur le pilote
            bool solo = PlayerInput.all.Count <= 1;
            float k = settings.speedBlur && solo ? GameFeel.Ramp(speed, settings.blurSpeedMin, settings.blurSpeedFull) * GameFeel.Intensity : 0f;
            if (_blurVolume == null)
            {
                if (k <= 0f)
                    return;
                var go = new GameObject("Flou de vitesse (sensations)");
                _blurVolume = go.AddComponent<Volume>();
                _blurVolume.isGlobal = true;
                _blurVolume.priority = 50f;
                VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _blur = profile.Add<MotionBlur>(true);
                _blur.mode.Override(MotionBlurMode.CameraOnly);
                _blur.quality.Override(MotionBlurQuality.Low);
                _blur.clamp.Override(0.03f);
                _blur.intensity.Override(0f);
                _blurVolume.sharedProfile = profile;
            }

            _blur.intensity.Override(settings.blurMaxIntensity * k);
        }
    }
}
