using ItHunts.Movement;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Sensations de jeu d'un joueur (points 4 et 9 du 2026-11-21) : lit l'état de son mouvement et pose les mouvements de caméra
    /// (hissage…) sur sa CameraShake, qui les compose avec les secousses. Affichage seulement ; un par joueur (écran partagé).
    /// </summary>
    [DefaultExecutionOrder(-45)]                                         // après PlayerLook (−50), avant CameraShake (−40)
    public sealed class PlayerFeel : MonoBehaviour
    {
        [SerializeField] private GameFeelSettings settings;

        private PlayerMotor _motor;
        private CameraShake _shake;
        private int _lastMantleTick = int.MinValue;
        private float _mantleT = -1f;
        private float _mantleSide = 1f;

        public GameFeelSettings Settings => settings;

        /// <summary>Mouvement de caméra en cours (debug, tests) : décalage local (m) et angles (°).</summary>
        public Vector3 Offset { get; private set; }
        public Vector3 Euler { get; private set; }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _shake = GetComponent<CameraShake>();
            if (_shake != null)
                _shake.Feel = settings;
        }

        /// <summary>
        /// Courbe du hissage à l'instant t (s) : plongeon (descente, tangage vers le bas, roulis) puis remontée avec un léger
        /// dépassement ; tout revient à 0 à la fin. Pur, testable.
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

        private void Update()
        {
            if (_motor == null || settings == null)
                return;
            float scale = GameFeel.MotionScale(settings);
            Vector3 offset = Vector3.zero, euler = Vector3.zero;

            // Hissage : départ détecté au tick du hissage.
            MovementState s = _motor.State;
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

            Offset = offset * scale;
            Euler = euler * scale;
            if (_shake != null)
            {
                _shake.FeelOffset = Offset;
                _shake.FeelEuler = Euler;
            }
        }
    }
}
