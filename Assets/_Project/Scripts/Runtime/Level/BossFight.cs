using ItHunts.Enemies;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Combat de boss de fin de niveau (D-049) : en haut des escaliers de sortie, l'arène. Il commence quand un joueur atteint
    /// la sortie : les joueurs vivants entrent dans l'arène, le traqueur arrive par l'autre côté, les vagues s'arrêtent. Il
    /// garde toutes ses attaques. Trois pièges (herse, pont qui s'effondre, lustre géant) : il faut l'attirer dans la zone de
    /// chacun et le déclencher avec l'orbe. Un piège gagné le sonne (staggerSeconds), puis il revient par l'entrée du fond ; au
    /// troisième, il tombe : niveau gagné (Facile : fin de la run). Réseau (étape 11) : déclenchements décidés par l'hôte.
    /// </summary>
    public sealed class BossFight : MonoBehaviour
    {
        [SerializeField] private Transform playerEntry;
        [SerializeField] private Transform stalkerEntry;
        [SerializeField] private BossTrap[] traps;
        [Tooltip("Temps où le traqueur reste sonné après un piège gagné (s).")]
        [Min(0f)] [SerializeField] private float staggerSeconds = 4f;
        [Tooltip("Pression : à chaque piège gagné, les recharges de ses attaques (prise, mains levées, pointe) × ce facteur.")]
        [Range(0.3f, 1f)] [SerializeField] private float pressurePerTrap = 0.7f;
        [Tooltip("Délai entre le troisième piège et la fin du niveau (s).")]
        [Min(0f)] [SerializeField] private float finishDelay = 2f;

        private ZoneState _zone;
        private Stalker _stalker;
        private float _returnAt = float.PositiveInfinity, _finishAt = float.PositiveInfinity;

        public bool Active { get; private set; }
        public bool Won { get; private set; }
        public float StartedAt { get; private set; }
        public float WonAt { get; private set; }
        public int Hits { get; private set; }
        public int TrapCount => traps != null ? traps.Length : 0;
        public BossTrap[] Traps => traps;
        public Transform PlayerEntry => playerEntry;
        public Transform StalkerEntry => stalkerEntry;

        private void Start()
        {
            if (RunSession.BossPlaytest)
            {
                RunSession.BossPlaytest = false;                         // une seule fois (Rejouer refait le niveau normal)
                StartCoroutine(DebugBeginWhenReady());
            }

            _zone = FindFirstObjectByType<ZoneState>();
            _stalker = FindFirstObjectByType<Stalker>(FindObjectsInactive.Include);
            if (_zone != null)
                _zone.ExitHandler = OnExit;
            if (traps != null)
                foreach (BossTrap t in traps)
                    if (t != null)
                        t.Bind(this);
        }

        private bool OnExit(PlayerHealth p)
        {
            if (Won)
                return false;
            if (!Active)
                Begin();
            return true;
        }

        /// <summary>Début du combat : joueurs dans l'arène, traqueur par l'entrée du fond, vagues arrêtées.</summary>
        public void Begin()
        {
            Active = true;
            StartedAt = Time.time;
            Hits = 0;
            int i = 0;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead || p.Motor == null || playerEntry == null)
                    continue;
                p.Motor.Teleport(playerEntry.position + playerEntry.right * (1.5f * i++));
                var look = p.GetComponent<PlayerLook>();
                if (look != null)
                    look.SetView(playerEntry.eulerAngles.y, 0f);
            }

            if (_zone != null)
                _zone.BossFightActive = true;
            if (_stalker != null)
                _stalker.CooldownScale = 1f;
            if (_stalker != null && stalkerEntry != null)
            {
                _stalker.gameObject.SetActive(true);
                _stalker.WarpTo(stalkerEntry.position, stalkerEntry.rotation);
                _stalker.Stagger(1.5f);                                  // il entre, rugit, puis charge
            }
        }

        /// <summary>
        /// Playtest et debug (Page bas) : le combat commence tout de suite. Timer avancé jusqu'à l'ouverture de la sortie (le
        /// traqueur est lâché, comme en vraie partie), une fois le NavMesh construit.
        /// </summary>
        public void DebugBegin()
        {
            if (Active || Won)
                return;
            StartCoroutine(DebugBeginWhenReady());
        }

        private System.Collections.IEnumerator DebugBeginWhenReady()
        {
            ZoneNavMesh nav = FindFirstObjectByType<ZoneNavMesh>();
            while (nav != null && !nav.Built)
                yield return null;
            yield return null;
            ZoneState zone = _zone != null ? _zone : FindFirstObjectByType<ZoneState>();
            if (zone != null && zone.Settings != null)
                zone.DebugAdvance(Mathf.Max(0f, zone.Settings.exitUnlockAt - zone.Elapsed));
            if (!Active && !Won)
                Begin();
        }

        public bool StalkerIn(Bounds zone) => _stalker != null && zone.Contains(_stalker.transform.position + Vector3.up);

        /// <summary>Piège gagné : sonné, puis il revient par le fond ; au troisième, il tombe.</summary>
        public void TrapSprung(BossTrap trap)
        {
            Hits++;
            if (_stalker != null)
            {
                _stalker.Stagger(Hits >= TrapCount ? 999f : staggerSeconds);
                _stalker.CooldownScale *= pressurePerTrap;                  // la pression monte à chaque piège
            }
            if (Hits >= TrapCount)
            {
                Won = true;
                WonAt = Time.time;
                _finishAt = Time.time + finishDelay;
            }
            else
                _returnAt = Time.time + staggerSeconds;
        }

        private void Update()
        {
            if (Time.time >= _returnAt)
            {
                _returnAt = float.PositiveInfinity;
                if (_stalker != null && stalkerEntry != null && Active && !Won)
                    _stalker.WarpTo(stalkerEntry.position, stalkerEntry.rotation);
            }

            if (Time.time >= _finishAt)
            {
                _finishAt = float.PositiveInfinity;
                Active = false;
                if (_stalker != null)
                    _stalker.gameObject.SetActive(false);               // il est tombé
                if (_zone != null)
                    _zone.LevelWon();
            }
        }

        /// <summary>Remise à zéro de la zone : combat pas commencé, pièges armés.</summary>
        public void ResetFight()
        {
            if (_stalker != null)
                _stalker.CooldownScale = 1f;
            Active = Won = false;
            Hits = 0;
            _returnAt = _finishAt = float.PositiveInfinity;
            if (traps != null)
                foreach (BossTrap t in traps)
                    if (t != null)
                        t.ResetTrap();
            if (_stalker != null && !_stalker.gameObject.activeSelf)
            {
                _stalker.gameObject.SetActive(true);
                _stalker.ResetState();
            }
        }
    }
}
