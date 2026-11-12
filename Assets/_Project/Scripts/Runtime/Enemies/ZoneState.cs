using ItHunts.Combat;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// État d'une zone (objet de la scène, pas du joueur ; D-033) : timer (à 0 le traqueur devient enragé), temps de grâce,
    /// sortie, remise à zéro. Étape 6 : à la mort d'un joueur (en coop : quand tous sont morts), toute la zone est remise
    /// à zéro (joueurs, ennemis, objets, timer, traqueur) ; sortie atteinte : message, puis la zone recommence. La vraie
    /// mort (bilan, relance de la run) vient à l'étape 8.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public sealed class ZoneState : MonoBehaviour
    {
        [SerializeField] private ZoneSettings settings;
        [Tooltip("Départ des joueurs (les suivants à 1.5 m sur la droite) ; son orientation donne la direction du regard.")]
        [SerializeField] private Transform playerStart;

        private float _resetAt = float.PositiveInfinity;
        private float _messageUntil;

        public ZoneSettings Settings => settings;
        public float Elapsed { get; private set; }
        public float TimeLeft => settings != null ? Mathf.Max(0f, settings.timer - Elapsed) : 0f;
        public bool Enraged => settings != null && Elapsed >= settings.timer;
        public bool InGrace => settings != null && Elapsed < settings.gracePeriod;
        public float GraceLeft => settings != null ? Mathf.Max(0f, settings.gracePeriod - Elapsed) : 0f;
        public bool Finished { get; private set; }
        public float FinishTime { get; private set; }
        public int Resets { get; private set; }
        public string Message => Time.time < _messageUntil ? _message : null;
        private string _message;

        private void Awake() => PlayerHealth.ZoneHandlesDeath = true;

        private void OnDestroy() => PlayerHealth.ZoneHandlesDeath = false;

        private void OnEnable() => PlayerHealth.Died += OnDied;
        private void OnDisable() => PlayerHealth.Died -= OnDied;

        private void Start() => ResetZone();

        private void FixedUpdate()
        {
            if (!Finished && float.IsPositiveInfinity(_resetAt))
                Elapsed += Time.fixedDeltaTime;
            if (Time.time >= _resetAt)
                ResetZone();
        }

        private void Show(string message, float seconds)
        {
            _message = message;
            _messageUntil = Time.time + seconds;
        }

        private void OnDied(PlayerHealth p)
        {
            foreach (PlayerHealth other in PlayerHealth.All)
                if (other != null && !other.Dead)
                    return;                                             // coop : un joueur reste debout
            if (!float.IsPositiveInfinity(_resetAt))
                return;
            Show("Mort : la zone recommence", (settings != null ? settings.resetDelay : 1.5f) + 1f);
            _resetAt = Time.time + (settings != null ? settings.resetDelay : 1.5f);
        }

        /// <summary>Un joueur atteint la sortie : zone terminée (solo ; en coop, la règle viendra avec l'étape 9).</summary>
        public void ExitReached(PlayerHealth p)
        {
            if (Finished || p == null || p.Dead)
                return;
            Finished = true;
            FinishTime = Elapsed;
            int s = Mathf.FloorToInt(Elapsed);
            float delay = settings != null ? settings.exitRestartDelay : 4f;
            Show($"Sortie atteinte en {s / 60}:{s % 60:00}", delay + 0.5f);
            _resetAt = Time.time + delay;
        }

        /// <summary>Remise à zéro : joueurs au départ avec tous leurs PV, ennemis et objets à leur place, timer à zéro.</summary>
        public void ResetZone()
        {
            _resetAt = float.PositiveInfinity;
            Elapsed = 0f;
            Finished = false;
            Resets++;
            int i = 0;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null)
                    continue;
                p.Restore();
                if (p.Motor != null && playerStart != null)
                {
                    p.Motor.Teleport(playerStart.position + playerStart.right * (1.5f * i));
                    var look = p.GetComponent<PlayerLook>();
                    if (look != null)
                        look.SetView(playerStart.eulerAngles.y, 0f);
                }

                i++;
            }

            foreach (Grabbable g in FindObjectsByType<Grabbable>(FindObjectsSortMode.None))
                if (!g.IsEnemy)
                    g.ResetToHome();
            foreach (Damageable d in FindObjectsByType<Damageable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!d.Respawns)
                    d.Revive();
            foreach (Ghoul gh in FindObjectsByType<Ghoul>(FindObjectsSortMode.None))
                gh.ResetState();
            foreach (Stalker st in FindObjectsByType<Stalker>(FindObjectsSortMode.None))
                st.ResetState();
            ImpactDamager.ResetCooldowns();
        }
    }
}
