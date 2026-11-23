using ItHunts.Audio;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Sons du traqueur (boss-traqueur, règle 6) et musique en jeu. Sons de la DA (GameAudio_Default, PR #20), repli sur les sons
    /// provisoires (ProceduralSfx) s'ils manquent : pas lourds en 3D au rythme de sa vitesse (le tremblement de caméra suit
    /// chaque pas), souffle en boucle à proximité (plus fort en chasse), grognement quand il repère le joueur (Alerte,
    /// Recherche), cri au hasard au passage en Enragé, cri à la prise. Musique : exploration en boucle, fondu croisé vers la
    /// musique Enragé à la fin du timer, retour au calme ensuite. Volumes : réglages du joueur (musique, effets).
    /// </summary>
    [RequireComponent(typeof(Stalker))]
    public sealed class StalkerAudio : MonoBehaviour
    {
        /// <summary>Un pas du traqueur : position, distance de tremblement, force (CameraShake de chaque joueur).</summary>
        public static event System.Action<Vector3, float, float> Footstep;

        /// <summary>Choc au sol (réception d'un bond, point 9) : même tremblement que les pas, plus fort.</summary>
        public static void RaiseFootstep(Vector3 at, float range, float strength) => Footstep?.Invoke(at, range, strength);

        /// <summary>Cri du traqueur (prise, passage en Enragé) : position (sensations de jeu : secousse des joueurs proches).</summary>
        public static event System.Action<Vector3> Cry;

        private Stalker _stalker;
        private AudioSource _steps, _stepsLoop, _breath, _voice, _explore, _enraged;
        private AudioClip _step, _roar;
        private float _stepPhase;
        private float _seenRoar = float.NegativeInfinity, _seenGrab = float.NegativeInfinity, _seenGrowl = float.NegativeInfinity, _seenSpot = float.NegativeInfinity;
        private StalkerMode _lastMode;
        private int _seenKills;
        private bool _artSteps;

        /// <summary>Volume actuel de la musique d'exploration et de la musique enragée (tests, debug).</summary>
        public Vector2 MusicLevels => new Vector2(_explore != null ? _explore.volume : 0f, _enraged != null ? _enraged.volume : 0f);

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
            GameAudioSettings art = GameAudio.Settings;
            float range = _stalker.Settings != null ? _stalker.Settings.soundRange : 45f;
            _steps = Source("Pas du traqueur", true, range);
            _voice = Source("Voix du traqueur", true, range * 1.5f);
            _breath = Source("Souffle du traqueur", true, range * 0.6f);
            _step = ProceduralSfx.ToClip("Traqueur : pas", ProceduralSfx.StalkerStep());
            _roar = art != null && art.stalkerGrowl != null ? art.stalkerGrowl : ProceduralSfx.ToClip("Traqueur : grondement", ProceduralSfx.StalkerRoar());
            _breath.clip = art != null && art.stalkerBreath != null ? art.stalkerBreath : ProceduralSfx.ToClip("Traqueur : souffle", ProceduralSfx.StalkerBreath());
            _breath.loop = true;
            _breath.Play();

            // Pas de la DA : le fichier est une suite de pas ; il tourne en boucle tant que le traqueur avance, plus vite quand il
            // court (hauteur), et chaque pas compté fait trembler la caméra comme avant.
            _artSteps = art != null && art.stalkerSteps != null;
            if (_artSteps)
            {
                _stepsLoop = Source("Pas du traqueur (boucle)", true, range);
                _stepsLoop.clip = art.stalkerSteps;
                _stepsLoop.loop = true;
                _stepsLoop.volume = 0f;
                _stepsLoop.Play();
            }

            // Musique (2D) : exploration et enragé, en boucle, fondu croisé.
            _explore = Source("Musique : exploration", false, range);
            _enraged = Source("Musique : enragé", false, range);
            _explore.loop = _enraged.loop = true;
            _explore.volume = _enraged.volume = 0f;
            _explore.clip = art != null && art.musicExploration != null ? art.musicExploration : ProceduralSfx.ToClip("Tension", ProceduralSfx.TensionDrone());
            // Musique de l'acte (PR #41) : château (acte II), jardin (acte III), boss (arène).
            string scene = gameObject.scene.name;
            AudioClip actMusic = art == null ? null : scene.Contains("Boss") ? art.musicBoss : scene.Contains("Chateau") ? art.musicChateau
                : scene.Contains("Jardin") ? art.musicJardin : null;
            if (actMusic != null)
                _explore.clip = actMusic;
            _enraged.clip = art != null && art.musicEnraged != null ? art.musicEnraged : _explore.clip;
            _explore.Play();
            _enraged.Play();
            _lastMode = _stalker.Mode;
            _seenKills = _stalker.GrabKills;
            GameSettings.Apply();                                         // volume des effets sur l'écouteur (scène lancée sans le menu)
        }

        private void Update()
        {
            if (_stalker == null || _stalker.Settings == null || _steps == null)
                return;
            var cfg = _stalker.Settings;
            GameAudioSettings art = GameAudio.Settings;
            float speed = _stalker.Speed;
            float rate = cfg.stepsPerSecondAtChase * Mathf.Max(0.3f, speed / Mathf.Max(0.1f, cfg.chaseSpeed));
            bool moving = speed > 0.3f;
            if (moving)
            {
                _stepPhase += rate * Time.deltaTime;
                if (_stepPhase >= 1f)
                {
                    _stepPhase -= 1f;
                    if (!_artSteps)
                    {
                        _steps.pitch = Random.Range(0.9f, 1.05f);
                        _steps.PlayOneShot(_step, cfg.stepVolume);
                    }

                    Footstep?.Invoke(transform.position, cfg.shakeRange, cfg.shakeStrength);
                }
            }

            if (_stepsLoop != null)
            {
                float target = moving ? cfg.stepVolume * (art != null ? art.stalkerStepsVolume : 1f) : 0f;
                _stepsLoop.volume = Mathf.MoveTowards(_stepsLoop.volume, target, Time.deltaTime * 4f);
                _stepsLoop.pitch = Mathf.Clamp(speed / Mathf.Max(0.1f, cfg.chaseSpeed), 0.85f, 1.2f);
            }

            bool hunting = _stalker.Mode == StalkerMode.Chasse || _stalker.Mode == StalkerMode.Enrage;
            _breath.volume = Mathf.MoveTowards(_breath.volume, cfg.breathVolume * (hunting ? 1f : 0.5f), Time.deltaTime);
            if (_stalker.LastRoarTime > _seenRoar)
            {
                _seenRoar = _stalker.LastRoarTime;
                GameAudio.PlayOn(_voice, art != null ? art.stalkerGrowl : null, _roar, 1f);
            }

            // Cris : passage en Enragé (une variante au hasard), prise d'un joueur.
            float cry = art != null ? art.stalkerCryVolume : 1f;
            // Repérage d'un joueur (nuit 4, point 7) : cri marquant, portée longue, secousse pour qui est proche.
            if (_stalker.LastSpotCryTime > _seenSpot)
            {
                _seenSpot = _stalker.LastSpotCryTime;
                if (art != null)
                {
                    AudioClip spotted = GameAudio.Pick(art.stalkerSpottedCries);
                    GameAudio.PlayOn(_voice, spotted != null ? spotted : GameAudio.Pick(art.stalkerEnragedCries), null, cry);
                }

                Cry?.Invoke(transform.position);
            }

            if (_stalker.Mode == StalkerMode.Enrage && _lastMode != StalkerMode.Enrage)
            {
                if (art != null)
                    GameAudio.PlayOn(_voice, GameAudio.Pick(art.stalkerEnragedCries), null, cry);
                Cry?.Invoke(transform.position);
            }
            _lastMode = _stalker.Mode;
            // Cri juste avant la prise (C4 : avec la forme Cri du visage), plus au moment où elle tue.
            if (_stalker.LastGrabStart > _seenGrab)
            {
                _seenGrab = _stalker.LastGrabStart;
                if (art != null)
                    GameAudio.PlayOn(_voice, art.stalkerGrabCry, null, cry);
                Cry?.Invoke(transform.position);
            }

            // Grognement de proximité (lèvres retroussées).
            if (_stalker.LastGrowlTime > _seenGrowl)
            {
                _seenGrowl = _stalker.LastGrowlTime;
                GameAudio.PlayOn(_voice, art != null ? art.stalkerGrowl : null, _roar, 0.7f);
            }

            // Musique : enragé en Enragé, exploration sinon (fondu croisé).
            bool enraged = _stalker.Mode == StalkerMode.Enrage;
            float level = (art != null ? art.musicVolume : 0.5f) * GameSettings.MusicGain;
            float fade = Time.deltaTime / Mathf.Max(0.05f, art != null ? art.musicCrossfade : 2.5f);
            _explore.volume = Mathf.MoveTowards(_explore.volume, enraged ? 0f : level, fade * Mathf.Max(level, 0.01f));
            _enraged.volume = Mathf.MoveTowards(_enraged.volume, enraged ? level : 0f, fade * Mathf.Max(level, 0.01f));
        }
    }
}
