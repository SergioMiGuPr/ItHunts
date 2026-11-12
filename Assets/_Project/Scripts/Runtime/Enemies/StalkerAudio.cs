using ItHunts.Audio;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Annonce sonore du traqueur (boss-traqueur, règle 6), provisoire (ProceduralSfx, vrais sons à l'étape 10) : pas
    /// lourds en 3D au rythme de sa vitesse, souffle en boucle (plus fort en chasse), grondement quand il repère le joueur,
    /// et une nappe de tension (2D) qui monte en Alerte et en Chasse. On l'entend avant de le voir.
    /// </summary>
    [RequireComponent(typeof(Stalker))]
    public sealed class StalkerAudio : MonoBehaviour
    {
        /// <summary>Un pas du traqueur : position, distance de tremblement, force (CameraShake de chaque joueur).</summary>
        public static event System.Action<Vector3, float, float> Footstep;

        private Stalker _stalker;
        private AudioSource _steps, _breath, _music;
        private AudioClip _step, _roar;
        private float _stepPhase;
        private float _seenRoar = float.NegativeInfinity;

        private AudioSource Source(string name, bool spatial, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.up * 1.5f;
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = spatial ? 1f : 0f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 3f;
            s.maxDistance = range;
            s.dopplerLevel = 0f;
            return s;
        }

        private void Start()
        {
            _stalker = GetComponent<Stalker>();
            float range = _stalker.Settings != null ? _stalker.Settings.soundRange : 45f;
            _steps = Source("Pas du traqueur", true, range);
            _breath = Source("Souffle du traqueur", true, range * 0.6f);
            _music = Source("Tension (provisoire)", false, range);
            _step = ProceduralSfx.ToClip("Traqueur : pas", ProceduralSfx.StalkerStep());
            _roar = ProceduralSfx.ToClip("Traqueur : grondement", ProceduralSfx.StalkerRoar());
            _breath.clip = ProceduralSfx.ToClip("Traqueur : souffle", ProceduralSfx.StalkerBreath());
            _breath.loop = true;
            _breath.Play();
            _music.clip = ProceduralSfx.ToClip("Tension", ProceduralSfx.TensionDrone());
            _music.loop = true;
            _music.volume = 0f;
            _music.Play();
        }

        private void Update()
        {
            if (_stalker == null || _stalker.Settings == null || _steps == null)
                return;
            var cfg = _stalker.Settings;
            float speed = _stalker.Speed;
            float rate = cfg.stepsPerSecondAtChase * Mathf.Max(0.3f, speed / Mathf.Max(0.1f, cfg.chaseSpeed));
            if (speed > 0.3f)
            {
                _stepPhase += rate * Time.deltaTime;
                if (_stepPhase >= 1f)
                {
                    _stepPhase -= 1f;
                    _steps.pitch = Random.Range(0.9f, 1.05f);
                    _steps.PlayOneShot(_step, cfg.stepVolume);
                    Footstep?.Invoke(transform.position, cfg.shakeRange, cfg.shakeStrength);
                }
            }

            bool hunting = _stalker.Mode == StalkerMode.Chasse || _stalker.Mode == StalkerMode.Enrage;
            _breath.volume = Mathf.MoveTowards(_breath.volume, cfg.breathVolume * (hunting ? 1f : 0.5f), Time.deltaTime);
            if (_stalker.LastRoarTime > _seenRoar)
            {
                _seenRoar = _stalker.LastRoarTime;
                _steps.PlayOneShot(_roar, 1f);
            }

            float music = _stalker.Mode switch
            {
                StalkerMode.Alerte => 0.25f,
                StalkerMode.Recherche => 0.2f,
                StalkerMode.Chasse => 0.55f,
                StalkerMode.Enrage => 0.7f,
                _ => 0f,
            };
            _music.volume = Mathf.MoveTowards(_music.volume, music, Time.deltaTime * 0.5f);
        }
    }
}
