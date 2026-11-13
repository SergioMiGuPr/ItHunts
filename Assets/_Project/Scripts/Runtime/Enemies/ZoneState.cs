using ItHunts.Combat;
using ItHunts.Level;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// État d'une zone, c'est-à-dire d'un niveau (objet de la scène, pas du joueur ; D-033, D-048). Le timer démarre quand le
    /// premier joueur sort de la salle de départ (volume « Départ ») ; traqueur lâché à gracePeriod, porte de sortie
    /// déverrouillée à exitUnlockAt, Enragé à timer. Mort du dernier joueur vivant : écran de fin (deathEndsRun), sinon remise à
    /// zéro de la zone (ancien comportement de test). Niveau gagné (combat de boss, ou sortie s'il n'y a pas d'arène) : niveau
    /// suivant ou écran de victoire. Réseau (étape 11) : démarrage, fin et changement de niveau décidés par l'hôte.
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
        public bool Enraged => settings != null && Started && Elapsed >= settings.timer;
        public bool InGrace => settings != null && (!Started || Elapsed < settings.gracePeriod);
        /// <summary>Le timer tourne : un joueur est sorti de la salle de départ (D-048).</summary>
        public bool Started { get; private set; }
        public bool ExitUnlocked => settings != null && Started && Elapsed >= settings.exitUnlockAt;
        public float ExitUnlockLeft => settings != null ? Mathf.Max(0f, settings.exitUnlockAt - Elapsed) : 0f;
        /// <summary>Niveau gagné (combat de boss ou sortie) ; écran de fin affiché ou niveau suivant en chargement.</summary>
        public bool Won { get; private set; }
        public bool RunOver { get; private set; }
        /// <summary>Le timer vient de démarrer (premier joueur sorti de la salle de départ).</summary>
        public static event System.Action<ZoneState> TimerStarted;
        /// <summary>Remplacé par le combat de boss (D-049) : vrai s'il prend la main à la sortie.</summary>
        public System.Func<PlayerHealth, bool> ExitHandler { get; set; }
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
            if (!Started && !Finished)
                TryStart();
            if (Started && !Finished && float.IsPositiveInfinity(_resetAt))
                Elapsed += Time.fixedDeltaTime;
            if (Time.time >= _resetAt)
            {
                _resetAt = float.PositiveInfinity;
                if (_pending != null)
                {
                    System.Action a = _pending;
                    _pending = null;
                    a();
                }
                else
                    ResetZone();
            }
        }

        private System.Action _pending;

        private void Update()
        {
            // Debug (éditeur et builds de dev) : Page haut avance le timer de 60 s (événements à 3, 10 et 15 min).
            if ((Application.isEditor || Debug.isDebugBuild) && UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.pageUpKey.wasPressedThisFrame && !Finished)
            {
                DebugAdvance(60f);
                Show($"Debug : timer +60 s ({Mathf.FloorToInt(Elapsed) / 60}:{Mathf.FloorToInt(Elapsed) % 60:00})", 1.5f);
            }
        }

        /// <summary>Démarre le timer dès qu'un joueur vivant est hors de la salle de départ (ou tout de suite sans salle de départ).</summary>
        private void TryStart()
        {
            bool hasStart = false;
            foreach (RoomVolume r in RoomVolume.All)
                if (r != null && r.Kind == RoomKind.Depart)
                {
                    hasStart = true;
                    break;
                }

            bool go = settings == null || !settings.timerWaitsForStartRoom || !hasStart;
            if (!go)
                foreach (PlayerHealth p in PlayerHealth.All)
                    if (p != null && !p.Dead && !RoomVolume.InKind(p.transform.position + Vector3.up * 0.5f, RoomKind.Depart))
                    {
                        go = true;
                        break;
                    }

            if (!go)
                return;
            Started = true;
            TimerStarted?.Invoke(this);
        }

        /// <summary>Avance le timer (debug F9, mesures) ; le démarre si besoin.</summary>
        public void DebugAdvance(float seconds)
        {
            if (!Started)
            {
                Started = true;
                TimerStarted?.Invoke(this);
            }

            Elapsed = Mathf.Max(0f, Elapsed + seconds);
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
            if (!float.IsPositiveInfinity(_resetAt) || RunOver)
                return;
            if (settings == null || settings.deathEndsRun)
            {
                // Mort du dernier joueur vivant = fin de la run (D-048). Coop : réanimation et spectateur à l'étape 9 (CoopLife).
                RunOver = true;
                Finished = true;
                FinishTime = Elapsed;
                float d = settings != null ? settings.exitRestartDelay : 2f;
                Show("Mort", d + 1f);
                _pending = () => RunEndScreen.Show(false, Summary("Tu es mort."));
                _resetAt = Time.time + d;
                return;
            }

            Show("Mort : la zone recommence", (settings != null ? settings.resetDelay : 1.5f) + 1f);
            _resetAt = Time.time + (settings != null ? settings.resetDelay : 1.5f);
        }

        private string Summary(string head)
        {
            int s = Mathf.FloorToInt(Elapsed);
            string level = RunSession.Active
                ? $"{RunSession.Definition.levels[RunSession.LevelIndex].displayName} ({RunSession.Difficulty.displayName})"
                : gameObject.scene.name;
            int souls = 0;
            var run = ItHunts.Artifacts.RunState.Current;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && run != null)
                    souls = Mathf.Max(souls, run.Souls(p.PlayerId));
            return $"{head}\n{level} : {s / 60}:{s % 60:00} de timer, {souls} âmes en poche.";
        }

        /// <summary>
        /// Un joueur atteint la sortie (en haut des escaliers). Si le combat de boss (D-049) prend la main, il commence ; sinon le
        /// niveau est gagné.
        /// </summary>
        public void ExitReached(PlayerHealth p)
        {
            if (Finished || p == null || p.Dead)
                return;
            if (ExitHandler != null && ExitHandler(p))
                return;
            LevelWon();
        }

        /// <summary>Niveau gagné (D-048) : niveau suivant, ou écran de victoire si la run est finie (Facile : après le niveau 1).</summary>
        public void LevelWon()
        {
            if (Won)
                return;
            Won = true;
            Finished = true;
            FinishTime = Elapsed;
            string next = RunSession.NextLevelScene();
            float delay = settings != null ? settings.exitRestartDelay : 2f;
            Show("Niveau terminé", delay + 1f);
            if (next != null)
                _pending = () => RunSession.Load(next);
            else
            {
                RunOver = true;
                _pending = () => RunEndScreen.Show(true, Summary(RunSession.Active ? "La run est gagnée." : "Niveau gagné."));
            }

            _resetAt = Time.time + delay;
        }

        /// <summary>Remise à zéro : joueurs au départ avec tous leurs PV, ennemis et objets à leur place, timer à zéro.</summary>
        public void ResetZone()
        {
            _resetAt = float.PositiveInfinity;
            _pending = null;
            Elapsed = 0f;
            Finished = false;
            Started = false;
            Won = false;
            RunOver = false;
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
            foreach (Flyer fl in FindObjectsByType<Flyer>(FindObjectsSortMode.None))
                fl.ResetState();
            foreach (Stalker st in FindObjectsByType<Stalker>(FindObjectsSortMode.None))
                st.ResetState();
            foreach (WaveDirector w in FindObjectsByType<WaveDirector>(FindObjectsSortMode.None))
                w.ResetAll();                                            // vagues (D-041) : la réserve rentre, départ reposé
            // Économie (D-043) : éclats au sol effacés, coffres refermés, portes reverrouillées, marchand replacé. Les âmes et les
            // améliorations des joueurs restent (la vraie mort, qui termine la run, vient à l'étape 8).
            foreach (ItHunts.Economy.SoulSystem s in FindObjectsByType<ItHunts.Economy.SoulSystem>(FindObjectsSortMode.None))
                s.Clear();
            foreach (ItHunts.Economy.Chest c in FindObjectsByType<ItHunts.Economy.Chest>(FindObjectsSortMode.None))
                c.ResetChest();
            foreach (ItHunts.Economy.LockedDoor d in FindObjectsByType<ItHunts.Economy.LockedDoor>(FindObjectsSortMode.None))
                d.SetOpen(false);
            foreach (ItHunts.Economy.Merchant m in FindObjectsByType<ItHunts.Economy.Merchant>(FindObjectsSortMode.None))
                m.Place();
            foreach (ItHunts.Economy.ArtifactMerchant m in FindObjectsByType<ItHunts.Economy.ArtifactMerchant>(FindObjectsSortMode.None))
                m.ResetMerchant();
            if (MapDiscovery.Current != null)
                MapDiscovery.Current.Clear();                            // brouillard de guerre (D-053)
            foreach (ExitDoor d in FindObjectsByType<ExitDoor>(FindObjectsSortMode.None))
                d.ResetDoor();
            ImpactDamager.ResetCooldowns();
        }
    }
}
