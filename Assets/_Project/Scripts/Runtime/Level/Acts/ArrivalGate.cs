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
            if (_zone == null || bars == null)
                return;
            float release = _zone.Settings != null ? _zone.Settings.gracePeriod : 60f;
            float t = _zone.Started ? _zone.Elapsed : 0f;
            if (!_zone.Started || _zone.Elapsed < 0.01f)
            {
                Opened = false;
                OpenAmount = 0f;
                bars.localPosition = _closed;
                return;
            }

            float warnStart = release - warn - rise;
            if (t < warnStart)
            {
                bars.localPosition = _closed;
                OpenAmount = 0f;
                return;
            }

            if (t < release - rise)
            {
                // Tremblement : il pousse derrière.
                float k = Mathf.InverseLerp(warnStart, release - rise, t);
                bars.localPosition = _closed + new Vector3(Mathf.Sin(Time.time * 47f) * 0.05f * k, Mathf.Abs(Mathf.Sin(Time.time * 31f)) * 0.08f * k, 0f);
                if (!_shook)
                {
                    _shook = true;
                    ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.stalkerGrowl, transform.position, 1f);
                }

                return;
            }

            float u = Mathf.Clamp01(Mathf.InverseLerp(release - rise, release, t));
            OpenAmount = u * u * (3f - 2f * u);
            bars.localPosition = _closed + Vector3.up * (lift * OpenAmount);
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
