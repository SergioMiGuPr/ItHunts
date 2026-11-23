using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Audio
{
    /// <summary>
    /// Sons provisoires de l'orbe de magnétisme, pour le joueur local (2D) : début et fin de traction, bourdonnement
    /// pendant la traction (sa hauteur suit la vitesse), répulsion, refus, charge d'attraction revenue et répulsion de
    /// nouveau prête. Détecte les changements de l'état de l'orbe à chaque tick, après la simulation. Plus tard : son
    /// 3D pour les autres joueurs (coop).
    /// </summary>
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class MagnetismAudio : MonoBehaviour
    {
        [SerializeField] private MagnetismFeedbackSettings settings;

        private PlayerMotor _motor;
        private AudioSource _oneShots;
        private AudioSource _hum;
        private AudioClip _start, _end, _repel, _refusal, _ready;
        private bool _wasTracting;
        private int _seenRepelTick = MovementState.NeverTick;
        private int _seenRefusalTick = MovementState.NeverTick;
        private int _lastWholeCharges = -1;
        private OrbStatus _lastRepelStatus = OrbStatus.Prete;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _oneShots = gameObject.AddComponent<AudioSource>();
            _oneShots.playOnAwake = false;
            _oneShots.spatialBlend = 0f;
            _hum = gameObject.AddComponent<AudioSource>();
            _hum.playOnAwake = false;
            _hum.loop = true;
            _hum.spatialBlend = 0f;
            _hum.clip = ItHunts.Audio.GameAudio.Or(s => s.tractionHum, ProceduralSfx.ToClip("Bourdonnement de traction", ProceduralSfx.TractionHum()));
            _start = ItHunts.Audio.GameAudio.Or(s => s.tractionStart, ProceduralSfx.ToClip("Début de traction", ProceduralSfx.TractionStart()));
            _end = ItHunts.Audio.GameAudio.Or(s => s.tractionEnd, ProceduralSfx.ToClip("Fin de traction", ProceduralSfx.TractionEnd()));
            _repel = ItHunts.Audio.GameAudio.Or(s => s.repulsion, ProceduralSfx.ToClip("Répulsion", ProceduralSfx.Repel()));
            _refusal = ItHunts.Audio.GameAudio.Or(s => s.refusal, ProceduralSfx.ToClip("Refus", ProceduralSfx.Refusal()));
            _ready = ItHunts.Audio.GameAudio.Or(s => s.chargeReady, ProceduralSfx.ToClip("Prêt", ProceduralSfx.Ready()));
        }

        private void Start()
        {
            // Pas de son pour l'état de départ (charges pleines, répulsion prête).
            MagnetismState m = _motor.Magnetism;
            _seenRepelTick = m.LastRepelTick;
            _seenRefusalTick = m.LastRefusalTick;
            _wasTracting = m.Tracting;
            _lastRepelStatus = m.RepelStatus;
            _lastWholeCharges = -1;
        }

        private void OnDestroy()
        {
            // Seuls les sons synthétisés se détruisent ; ceux de la DA sont des assets (« Destroying assets is not permitted », nuit 4).
            foreach (AudioClip clip in new[] { _hum != null ? _hum.clip : null, _start, _end, _repel, _refusal, _ready })
                ProceduralSfx.DestroyIfRuntime(clip);
        }

        // Après PlayerMotor (ordre d'exécution −100) : l'état de ce tick est à jour.
        private void FixedUpdate()
        {
            if (settings == null || _motor.MagnetismSettings == null)
                return;

            MagnetismState m = _motor.Magnetism;
            float master = settings.masterVolume;

            if (m.Tracting && !_wasTracting)
            {
                Play(_start, settings.tractionStartVolume * master);
                _hum.volume = settings.humVolume * master;
                _hum.Play();
            }
            else if (!m.Tracting && _wasTracting)
            {
                Play(_end, settings.tractionEndVolume * master);
                _hum.Stop();
            }

            _wasTracting = m.Tracting;
            if (m.Tracting)
            {
                float speed = _motor.State.Velocity.magnitude;
                _hum.pitch = Mathf.Lerp(settings.humPitchMin, settings.humPitchMax, speed / settings.humSpeedForMaxPitch);
                _hum.volume = settings.humVolume * master;
            }

            if (m.LastRepelTick != _seenRepelTick)
            {
                _seenRepelTick = m.LastRepelTick;
                Play(_repel, settings.repelVolume * master);
            }

            if (m.LastRefusalTick != _seenRefusalTick)
            {
                _seenRefusalTick = m.LastRefusalTick;
                Play(_refusal, settings.refusalVolume * master);
            }

            // Une charge d'attraction revient (pas quand on en dépense), ou la répulsion est de nouveau prête.
            int whole = Mathf.FloorToInt(Mathf.Max(0f, m.AttractCharges));
            if (_lastWholeCharges >= 0 && whole > _lastWholeCharges)
                Play(_ready, settings.readyVolume * master);
            _lastWholeCharges = whole;

            if (m.RepelStatus == OrbStatus.Prete && _lastRepelStatus == OrbStatus.Recharge)
                Play(_ready, settings.readyVolume * master);
            _lastRepelStatus = m.RepelStatus;
        }

        private void Play(AudioClip clip, float volume)
        {
            if (clip != null && volume > 0f)
                _oneShots.PlayOneShot(clip, volume);
        }
    }
}
