using ItHunts.Enemies;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItHunts.Audio
{
    /// <summary>
    /// Sons de la nuit 3 (PR #41) branchés sur le jeu : ambiance de fond de l'acte (cave, château, jardin) sous la musique ; cœur qui
    /// bat quand le traqueur approche (paliers 60 / 40 / 20 m, recherche : rayon de terreur de Dead by Daylight) ; sons du mouvement
    /// (saut, atterrissage, dash, glissade, wall jump, hissage, plongeon) et de la télékinésie (attraper, relâcher, lancer), lus sur l'état
    /// du joueur. Posé automatiquement dans chaque scène de jeu.
    /// </summary>
    public sealed class ActAudio : MonoBehaviour
    {
        private AudioSource _ambience, _heart, _slide;
        private PlayerMotor _motor;
        private MovementState _last;
        private int _lastThrow = MovementState.NeverTick, _lastAction = MovementState.NeverTick;
        private bool _had;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
        }

        private static void OnScene(Scene s, LoadSceneMode m)
        {
            if (FindFirstObjectByType<ZoneState>(FindObjectsInactive.Include) == null || FindFirstObjectByType<ActAudio>() != null)
                return;
            new GameObject("Sons de l'acte (ambiance, cœur, mouvement)").AddComponent<ActAudio>();
        }

        private AudioSource Loop(string name, AudioClip clip, float volume)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var a = go.AddComponent<AudioSource>();
            a.clip = clip;
            a.loop = true;
            a.spatialBlend = 0f;
            a.volume = volume;
            if (clip != null)
                a.Play();
            return a;
        }

        private void OnEnable()
        {
            ItHunts.Artifacts.RunState.SoulsChanged += OnSouls;
            ItHunts.Economy.ShopMenu.Opened += OnMenu;
        }

        private void OnDisable()
        {
            ItHunts.Artifacts.RunState.SoulsChanged -= OnSouls;
            ItHunts.Economy.ShopMenu.Opened -= OnMenu;
        }

        private void OnSouls(int player, int total, int delta)
        {
            if (delta < 0)
                One(GameAudio.Settings?.purchase, 0.9f);                // achat : âmes dépensées
        }

        private void OnMenu(ItHunts.Economy.ShopMenu m) => One(GameAudio.Settings?.menuInteraction, 0.8f);

        private void Start()
        {
            GameAudioSettings s = GameAudio.Settings;
            if (s == null)
                return;
            string scene = gameObject.scene.name;
            AudioClip amb = scene.Contains("Chateau") ? s.ambienceChateau : scene.Contains("Jardin") || scene.Contains("Boss") ? s.ambienceJardin : s.ambienceCave;
            _ambience = Loop("Ambiance", amb, s.ambienceVolume * 0.5f);
            _heart = Loop("Cœur", s.heartbeat, 0f);
            _slide = Loop("Glissade", s.slideLoop, 0f);
            if (_slide.clip != null)
                _slide.Stop();
        }

        private void Update()
        {
            GameAudioSettings s = GameAudio.Settings;
            if (s == null)
                return;
            if (_motor == null)
            {
                PlayerHealth p = PlayerHealth.All.Count > 0 ? PlayerHealth.All[0] : null;
                _motor = p != null ? p.Motor : null;
                if (_motor == null)
                    return;
            }

            Heart(s);
            Movement(s);
        }

        /// <summary>Cœur : silencieux au-delà de 60 m ; 0.3 / 0.6 / 1 de volume et 1 / 1.25 / 1.5 de vitesse à 60 / 40 / 20 m.</summary>
        private void Heart(GameAudioSettings s)
        {
            if (_heart == null || _heart.clip == null)
                return;
            Stalker st = FindStalker();
            float d = st != null && st.isActiveAndEnabled ? Vector3.Distance(st.transform.position, _motor.transform.position) : 999f;
            float vol = d > 60f ? 0f : d > 40f ? 0.3f : d > 20f ? 0.6f : 1f;
            float pitch = d > 40f ? 1f : d > 20f ? 1.25f : 1.5f;
            _heart.volume = Mathf.MoveTowards(_heart.volume, vol * 0.8f, Time.deltaTime * 0.8f);
            _heart.pitch = Mathf.MoveTowards(_heart.pitch, pitch, Time.deltaTime * 0.5f);
        }

        private Stalker _stalker;
        private Stalker FindStalker() => _stalker != null ? _stalker : _stalker = FindFirstObjectByType<Stalker>();

        private void One(AudioClip c, float v)
        {
            if (c != null && _motor != null)
                GameAudio.PlayAt(c, _motor.transform.position + Vector3.up * 0.5f, v, 15f);
        }

        private void Movement(GameAudioSettings s)
        {
            MovementState now = _motor.State;
            if (!_had)
            {
                _last = now;
                _had = true;
                return;
            }

            if (_last.Grounded && !now.Grounded && now.Velocity.y > 2f)
                One(s.jump, 0.7f);
            else if (!_last.Grounded && now.Grounded && _last.Velocity.y < -6f)
                One(s.land, Mathf.Clamp01(-_last.Velocity.y / 25f));
            if (now.LastWallJumpId != _last.LastWallJumpId)
                One(s.wallJump, 0.8f);
            if (_last.Mode != MovementMode.Mantle && now.Mode == MovementMode.Mantle)
                One(s.mantle, 0.8f);
            if (_last.DashTicksLeft <= 0 && now.DashTicksLeft > 0)
                One(s.dash, 0.8f);
            bool sliding = now.Mode == MovementMode.Slide;
            if (_slide != null && _slide.clip != null && sliding != _slide.isPlaying)
            {
                if (sliding)
                    _slide.Play();
                else
                    _slide.Stop();
            }

            if (_slide != null)
                _slide.volume = sliding ? 0.5f : 0f;
            ItHunts.Orbs.TelekinesisState t = _motor.Telekinesis;
            if (t.LastActionTick != _lastAction && t.LastActionTick != MovementState.NeverTick)
            {
                _lastAction = t.LastActionTick;
                One(t.LastAction == ItHunts.Orbs.TelekinesisCommandKind.Lancer ? s.throwObject
                    : t.LastAction == ItHunts.Orbs.TelekinesisCommandKind.Attraper ? s.grab : s.release, 0.7f);
            }

            _last = now;
        }
    }
}
