using ItHunts.Combat;
using ItHunts.Input;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Parade du joueur (D-075) : « Parer » (Alt gauche / R3, D-079) ouvre une fenêtre courte ; une attaque de goule qui touche dans cette
    /// fenêtre ne fait aucun dégât et étourdit la goule. Ne touche jamais au mouvement (on pare en courant, en sautant, en
    /// glissant). Recharge après chaque appui. Sans effet contre le traqueur. En coop en ligne, l'hôte décidera.
    /// </summary>
    public sealed class PlayerParry : MonoBehaviour
    {
        [SerializeField] private ParrySettings settings;

        private ParryState _state = ParryState.Create();
        private PlayerInputReader _reader;
        private PlayerHealth _health;
        private float _missCheckedFor = float.NegativeInfinity;
        private static AudioClip _missClip;

        /// <summary>Texte du debug de la dernière parade (vide au-delà de 5 s).</summary>
        public string DebugLine => Time.time - _state.ResultTime < 5f ? ParryMath.Describe(_state, settings) : "";

        /// <summary>Parades dans le vide (son sec) : nombre (tests, mesures).</summary>
        public int Misses { get; private set; }

        public ParrySettings Settings { get => settings; set => settings = value; }
        public ParryState State => _state;

        /// <summary>Remplissage de l'icône de recharge (1 = prête).</summary>
        public float Fill => ParryMath.Fill(_state, Time.time, settings);
        public float CooldownRemaining => ParryMath.CooldownRemaining(_state, Time.time);

        private void Awake()
        {
            _reader = GetComponent<PlayerInputReader>();
            _health = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (settings == null)
                return;
            float now = Time.time;
            // Fenêtre passée sans rien parer : petit son sec (raté), une fois par appui.
            if (_state.LastPress > _state.LastSuccess && _state.LastPress != _missCheckedFor
                && now - _state.LastPress > settings.window + settings.lateWindow)
            {
                _missCheckedFor = _state.LastPress;
                Misses++;
                if (_state.LastResult == ParryResult.Aucun || _state.ResultTime < _state.LastPress)
                {
                    _state.LastResult = ParryResult.Vide;
                    _state.ResultTime = now;
                }

                if (_missClip == null)
                    _missClip = ItHunts.Audio.GameAudio.Or(s => s.parryMiss, ItHunts.Audio.ProceduralSfx.ToClip("Parade ratée", ItHunts.Audio.ProceduralSfx.BarrelReloadDone()));
                ItHunts.Audio.GameAudio.PlayAt(_missClip, transform.position + Vector3.up * 1.4f, settings.missVolume);
            }

            if (_reader == null || !_reader.ParryPressed || (_health != null && _health.Dead))
                return;
            Press(now);
        }

        /// <summary>Tolérance après l'impact (s) de la parade de ce joueur (0 : aucune).</summary>
        public static float LateWindowOf(PlayerHealth player)
        {
            var parry = player != null ? player.GetComponent<PlayerParry>() : null;
            return parry != null && parry.settings != null ? parry.settings.lateWindow : 0f;
        }

        /// <summary>Coup au contact gardé en attente (lateWindow) : paré par un appui juste après l'impact ?</summary>
        public static bool TryLateParry(PlayerHealth player, Component attacker, float impact, float now, Vector3 point)
        {
            var parry = player != null ? player.GetComponent<PlayerParry>() : null;
            if (parry == null || parry.settings == null)
                return false;
            if (!ParryMath.TryLateParry(ref parry._state, impact, now, parry.settings, ParryMath.Parryable(attacker)))
                return false;
            Succeed(parry, player, attacker, point);
            return true;
        }

        private static void Succeed(PlayerParry parry, PlayerHealth player, Component attacker, Vector3 point)
        {
            EnemyBody body = attacker.GetComponentInParent<EnemyBody>();
            if (body != null && body.StunFor(parry.settings.stunDuration))
            {
                // Goule parée bien visible : repoussée, éclair blanc (en plus des étoiles et du clip Goule_Etourdie).
                Vector3 away = attacker.transform.position - player.transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 1e-4f && parry.settings.parryPush > 0f)
                    body.Push(away.normalized * parry.settings.parryPush + Vector3.up * 1.5f);
            }

            var dmg = attacker.GetComponentInParent<Damageable>();
            if (dmg != null && parry.settings.parryFlash > 0f)
                dmg.Flash(Color.white * 3f, parry.settings.parryFlash);
            ParryEvents.Raise(new ParryEvent { Player = player.transform, Attacker = attacker.transform, Point = point });
        }

        /// <summary>Appui (aussi pour les tests et le debug).</summary>
        public bool Press(float now) => settings != null && ParryMath.Press(ref _state, now, settings);

        /// <summary>
        /// Appelé par une attaque juste avant ses dégâts : vrai si la parade l'annule (la goule est alors étourdie et le
        /// retour visuel et sonore part). Le traqueur n'est jamais paré.
        /// </summary>
        public static bool TryParry(PlayerHealth player, Component attacker, float now, Vector3 point)
        {
            if (player == null)
                return false;
            var parry = player.GetComponent<PlayerParry>();
            if (parry == null || parry.settings == null)
                return false;
            if (!ParryMath.TryParry(ref parry._state, now, parry.settings, ParryMath.Parryable(attacker)))
                return false;
            Succeed(parry, player, attacker, point);
            return true;
        }
    }
}
