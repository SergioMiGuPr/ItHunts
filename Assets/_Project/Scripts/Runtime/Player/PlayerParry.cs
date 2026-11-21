using ItHunts.Combat;
using ItHunts.Input;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Parade du joueur (D-075) : « Parer » (E / LB) ouvre une fenêtre courte ; une attaque de goule qui touche dans cette
    /// fenêtre ne fait aucun dégât et étourdit la goule. Ne touche jamais au mouvement (on pare en courant, en sautant, en
    /// glissant). Recharge après chaque appui. Sans effet contre le traqueur. En coop en ligne, l'hôte décidera.
    /// </summary>
    public sealed class PlayerParry : MonoBehaviour
    {
        [SerializeField] private ParrySettings settings;

        private ParryState _state = ParryState.Create();
        private PlayerInputReader _reader;
        private PlayerHealth _health;

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
            if (settings == null || _reader == null || !_reader.ParryPressed || (_health != null && _health.Dead))
                return;
            Press(Time.time);
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
            EnemyBody body = attacker.GetComponentInParent<EnemyBody>();
            if (body != null)
                body.StunFor(parry.settings.stunDuration);
            ParryEvents.Raise(new ParryEvent { Player = player.transform, Attacker = attacker.transform, Point = point });
            return true;
        }
    }
}
