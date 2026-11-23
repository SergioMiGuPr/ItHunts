using ItHunts.Enemies;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Herse de la porte d'arrivée (actes II et III, D-093) : le traqueur attend derrière. Quelques secondes avant la fin de son temps
    /// d'attente (gracePeriod de la zone), la herse tremble, grince, puis se lève : il entre par où le joueur est arrivé. Remise à zéro
    /// de la zone : refermée.
    /// </summary>
    public sealed class ArrivalGate : MonoBehaviour
    {
        [SerializeField] private Transform bars;
        [SerializeField] private float lift = 12f;
        [Tooltip("Tremblement avant l'ouverture (s), puis montée (s).")]
        [SerializeField] private float warn = 3f, rise = 2.2f;

        private ZoneState _zone;
        private Vector3 _closed;
        private AudioSource _audio;

        public bool Opened { get; private set; }
        public float OpenAmount { get; private set; }

        public void Configure(Transform barsRoot, float height)
        {
            bars = barsRoot;
            lift = height;
        }

        private Transform _leftHinge, _rightHinge;
        private Collider[] _leafColliders;

        /// <summary>Porte pleine à deux battants (nuit 4) : les gonds pivotent de 110° vers le vestibule quand le traqueur l'enfonce.</summary>
        public void ConfigureLeaves(Transform leftHinge, Transform rightHinge)
        {
            _leftHinge = leftHinge;
            _rightHinge = rightHinge;
            bars = null;
            _leafColliders = GetComponentsInChildren<Collider>(true);
        }

        private void ApplyLeaves(float open, float shake)
        {
            if (_leftHinge == null || _rightHinge == null)
                return;
            float jitter = shake > 0f ? Mathf.Sin(Time.time * 43f) * 1.5f * shake : 0f;
            _leftHinge.localRotation = Quaternion.Euler(0f, -110f * open + jitter, 0f);
            _rightHinge.localRotation = Quaternion.Euler(0f, 110f * open - jitter, 0f);
            bool passable = open > 0.3f;
            if (_leafColliders != null)
                foreach (Collider c in _leafColliders)
                    if (c != null)
                        c.enabled = !passable;
        }

        private void Start()
        {
            _zone = FindFirstObjectByType<ZoneState>();
            if (bars != null)
                _closed = bars.localPosition;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f;
            _audio.minDistance = 8f;
            _audio.maxDistance = 90f;
            _audio.playOnAwake = false;
        }

        private void Update()
        {
            if (_zone == null || (bars == null && _leftHinge == null))
                return;
            float release = _zone.Settings != null ? _zone.Settings.gracePeriod : 60f;
            float t = _zone.Started ? _zone.Elapsed : 0f;
            if (!_zone.Started || _zone.Elapsed < 0.01f)
            {
                Opened = false;
                OpenAmount = 0f;
                if (bars != null)
                    bars.localPosition = _closed;
                ApplyLeaves(0f, 0f);
                return;
            }

            float warnStart = release - warn - rise;
            if (t < warnStart)
            {
                if (bars != null)
                    bars.localPosition = _closed;
                ApplyLeaves(0f, 0f);
                OpenAmount = 0f;
                return;
            }

            if (t < release - rise)
            {
                // Tremblement : il pousse derrière.
                float k = Mathf.InverseLerp(warnStart, release - rise, t);
                if (bars != null)
                    bars.localPosition = _closed + new Vector3(Mathf.Sin(Time.time * 47f) * 0.05f * k, Mathf.Abs(Mathf.Sin(Time.time * 31f)) * 0.08f * k, 0f);
                ApplyLeaves(0f, k);
                if (!_shook)
                {
                    _shook = true;
                    ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.stalkerGrowl, transform.position, 1f);
                }

                return;
            }

            float u = Mathf.Clamp01(Mathf.InverseLerp(release - rise, release, t));
            OpenAmount = u * u * (3f - 2f * u);
            if (bars != null)
                bars.localPosition = _closed + Vector3.up * (lift * OpenAmount);
            ApplyLeaves(OpenAmount, 0f);
            if (!Opened && OpenAmount > 0.01f)
            {
                Opened = true;
                ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.stalkerArrival, transform.position, 1f, 120f);
                ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Pick(ItHunts.Audio.GameAudio.Settings?.stalkerEnragedCries), transform.position, 1f);
            }
        }

        private bool _shook;
    }
}
