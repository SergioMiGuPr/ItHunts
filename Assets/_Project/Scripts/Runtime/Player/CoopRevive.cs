using ItHunts.Input;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Réanimation (D-007, étape 9) : un joueur debout qui maintient E / Y à moins de reviveRange d'un allié à terre le relève au
    /// bout de reviveHoldSeconds (il revient avec reviveHealth PV). Relâcher ou s'éloigner remet le compte à zéro. Ajouté à chaque
    /// joueur par l'écran partagé. Réseau (étape 11) : décidé par l'hôte.
    /// </summary>
    public sealed class CoopRevive : MonoBehaviour
    {
        private PlayerHealth _health;
        private PlayerInputReader _reader;

        public PlayerHealth Target { get; private set; }
        public float Progress { get; private set; }
        public int Revives { get; private set; }

        /// <summary>Maintien simulé (tests, mesures) ; sinon E / Y.</summary>
        public bool? HeldOverride { get; set; }

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            _reader = GetComponent<PlayerInputReader>();
        }

        private void Update() => Step(Time.deltaTime);

        /// <summary>Un pas de la réanimation (public pour les tests).</summary>
        public void Step(float dt)
        {
            if (_health == null)
                _health = GetComponent<PlayerHealth>();
            CoopSettings cfg = _health != null ? _health.Coop : null;
            float range = cfg != null ? cfg.reviveRange : 2f, hold = cfg != null ? cfg.reviveHoldSeconds : 3f;
            bool held = HeldOverride ?? (_reader != null && _reader.InteractHeld);
            PlayerHealth target = held && _health != null && !_health.Dead ? Nearest(range) : null;
            if (target == null || target != Target)
                Progress = 0f;
            Target = target;
            if (target == null)
                return;
            Progress += dt;
            if (Progress >= hold)
            {
                target.ReviveFromDown(cfg != null ? cfg.reviveHealth : 50f);
                Revives++;
                Progress = 0f;
                Target = null;
            }
        }

        private PlayerHealth Nearest(float range)
        {
            PlayerHealth best = null;
            float bestD = range * range;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p == _health || !p.Downed || p.Spectating)
                    continue;
                float d = (p.transform.position - transform.position).sqrMagnitude;
                if (d <= bestD)
                {
                    bestD = d;
                    best = p;
                }
            }

            return best;
        }
    }
}
