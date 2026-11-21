using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Player
{
    /// <summary>
    /// Tremblement de caméra du joueur (D-034) : les pas du traqueur à moins de 25 m font légèrement trembler la vue, plus
    /// fort quand il est près. Réglage de confort par joueur (0 à 100 %, enregistré), 0 le coupe. Affichage seulement : la
    /// simulation (visée, tirs) n'est jamais décalée ; seule la caméra virtuelle bouge, juste après PlayerLook.
    /// </summary>
    [DefaultExecutionOrder(-40)]
    public sealed class CameraShake : MonoBehaviour
    {
        private const string IntensityKey = "ItHunts.CameraShake.";

        [SerializeField] private CinemachineCamera virtualCamera;
        [Tooltip("Réglage de confort : 0 = aucun tremblement. Enregistré par joueur.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 1f;
        [Tooltip("Décalage (m) et rotation (°) au plus fort.")]
        [SerializeField, Min(0f)] private float maxOffset = 0.05f;
        [SerializeField, Min(0f)] private float maxAngle = 1.2f;
        [Tooltip("Retombée du tremblement (/s) et fréquence (Hz).")]
        [SerializeField, Min(0f)] private float decay = 3f;
        [SerializeField, Min(0.1f)] private float frequency = 18f;

        private float _trauma;
        private int _playerIndex;
        private float _seed;

        public float Trauma => _trauma;

        /// <summary>Mouvement de caméra posé par PlayerFeel cette image (hissage, atterrissage…) : décalage local (m) et angles (°).</summary>
        public Vector3 FeelOffset { get; set; }
        public Vector3 FeelEuler { get; set; }

        /// <summary>Sensations de jeu (point 9) : réglages et échelle des secousses (intensité × « Réduire les secousses »).</summary>
        public ItHunts.Settings.GameFeelSettings Feel { get; set; }

        /// <summary>Secousse brève (0 à 1) : atterrissage fort, répulsion, coup reçu, parade, cri du traqueur.</summary>
        public void AddTrauma(float amount)
        {
            if (amount > 0f)
                _trauma = Mathf.Clamp01(_trauma + amount);
        }

        public float Intensity
        {
            get => intensity;
            set
            {
                intensity = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(IntensityKey + _playerIndex, intensity);
            }
        }

        private void Awake()
        {
            var input = GetComponent<PlayerInput>();
            _playerIndex = input != null ? Mathf.Max(0, input.playerIndex) : 0;
            intensity = PlayerPrefs.GetFloat(IntensityKey + _playerIndex, intensity);
            _seed = Random.value * 100f;
        }

        private void OnEnable() => ItHunts.Enemies.StalkerAudio.Footstep += OnFootstep;
        private void OnDisable() => ItHunts.Enemies.StalkerAudio.Footstep -= OnFootstep;

        /// <summary>Force d'un pas à cette distance : 0 au-delà de range, strength à bout portant (pur, testable).</summary>
        public static float StepTrauma(float distance, float range, float strength) =>
            range <= 0f || distance >= range ? 0f : strength * (1f - distance / range);

        private void OnFootstep(Vector3 position, float range, float strength) =>
            _trauma = Mathf.Clamp01(Mathf.Max(_trauma, StepTrauma(Vector3.Distance(position, transform.position), range, strength)));

        private void Update()
        {
            _trauma = Mathf.MoveTowards(_trauma, 0f, decay * Time.unscaledDeltaTime);   // un arrêt sur image ne fige pas la secousse
            if (virtualCamera == null)
                return;
            float k = _trauma * _trauma * intensity * GameFeel.MotionScale(Feel);
            float t = Time.unscaledTime * frequency;
            Transform v = virtualCamera.transform;
            Vector3 pos = FeelOffset;
            Quaternion rot = Quaternion.Euler(FeelEuler);
            if (k > 1e-4f)
            {
                float nx = Mathf.PerlinNoise(_seed, t) * 2f - 1f, ny = Mathf.PerlinNoise(_seed + 7f, t) * 2f - 1f;
                float nr = Mathf.PerlinNoise(_seed + 13f, t) * 2f - 1f;
                pos += new Vector3(nx, ny, 0f) * (maxOffset * k);
                rot *= Quaternion.Euler(ny * maxAngle * k, nx * maxAngle * k, nr * maxAngle * k);
            }

            v.localPosition = pos;
            v.localRotation = rot;
        }
    }
}
