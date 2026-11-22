using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItHunts.Level
{
    /// <summary>
    /// Statistiques de la run (nuit 3, point 3), gardées d'un acte à l'autre comme le stuff : temps survécu, acte atteint, morts par
    /// type, âmes gagnées et dépensées, améliorations, artefact, caisses lancées, headshots, parades, distance, vitesse maximale,
    /// cause de la mort. Remplies par des événements du jeu (RunStatsTracker) et lues par l'écran de fin. État de la run, pas d'un
    /// joueur (coop : totaux de l'équipe ; mouvement : le meilleur des joueurs).
    /// </summary>
    public static class RunStats
    {
        public static float TimeSurvived;
        public static int ActReached;
        public static readonly Dictionary<string, int> Kills = new Dictionary<string, int>();
        public static int SoulsGained, SoulsSpent;
        public static readonly List<string> Upgrades = new List<string>();
        public static string Artifact = "";
        public static int CratesThrown, Headshots, Parries;
        public static float Distance, MaxSpeed;
        public static string DeathCause = "";
        public static readonly List<(string act, float seconds)> ActTimes = new List<(string, float)>();

        public static int TotalKills
        {
            get
            {
                int n = 0;
                foreach (int k in Kills.Values)
                    n += k;
                return n;
            }
        }

        public static void Reset()
        {
            TimeSurvived = 0f;
            ActReached = 0;
            Kills.Clear();
            SoulsGained = SoulsSpent = 0;
            Upgrades.Clear();
            Artifact = "";
            CratesThrown = Headshots = Parries = 0;
            Distance = MaxSpeed = 0f;
            DeathCause = "";
            ActTimes.Clear();
        }

        public static void AddKill(string kind)
        {
            Kills.TryGetValue(kind, out int n);
            Kills[kind] = n + 1;
        }
    }

    /// <summary>Remplit RunStats pendant un niveau (posé automatiquement dans chaque scène qui a une zone).</summary>
    public sealed class RunStatsTracker : MonoBehaviour
    {
        private readonly Dictionary<PlayerHealth, (Vector3 pos, int throwTick)> _last = new Dictionary<PlayerHealth, (Vector3, int)>();
        private ZoneState _zone;
        private float _actStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnScene;
            SceneManager.sceneLoaded += OnScene;
        }

        private static void OnScene(Scene scene, LoadSceneMode mode)
        {
            if (FindFirstObjectByType<RunStatsTracker>() != null)
                return;
            // Scène de jeu (zone) : ses systèmes peuvent être sous une racine inactive pendant le chargement.
            if (FindFirstObjectByType<ZoneState>(FindObjectsInactive.Include) == null)
                return;
            new GameObject("Statistiques de la run").AddComponent<RunStatsTracker>();
        }

        private void OnEnable()
        {
            Damageable.Died += OnDied;
            RunState.SoulsChanged += OnSouls;
            ParryEvents.Parried += OnParry;
            PlayerHealth.Died += OnPlayerDied;
        }

        private void OnDisable()
        {
            Damageable.Died -= OnDied;
            RunState.SoulsChanged -= OnSouls;
            ParryEvents.Parried -= OnParry;
            PlayerHealth.Died -= OnPlayerDied;
        }

        private void Start()
        {
            if (!RunSession.Active || !RunSession.StatsStarted)
            {
                RunStats.Reset();
                RunSession.StatsStarted = true;
            }

            RunStats.ActReached = Mathf.Max(RunStats.ActReached, RunSession.Act);
            _actStart = RunStats.TimeSurvived;
        }

        private void OnDied(Damageable d)
        {
            if (d == null)
                return;
            if (d.GetComponent<Ghoul>() != null)
                RunStats.AddKill("Goules");
            else if (d.GetComponent<Flyer>() != null)
                RunStats.AddKill("Volants");
            else if (d.GetComponent<Stalker>() != null)
                RunStats.AddKill("Traqueur");
            else if (d.GetComponent<ItHunts.Orbs.Grabbable>() == null)
                RunStats.AddKill("Autres");
        }

        private static void OnSouls(int player, int total, int delta)
        {
            if (delta > 0)
                RunStats.SoulsGained += delta;
            else
                RunStats.SoulsSpent -= delta;
        }

        private static void OnParry(ParryEvent e) => RunStats.Parries++;

        private static void OnPlayerDied(PlayerHealth p)
        {
            if (p != null && !string.IsNullOrEmpty(p.LastCause))
                RunStats.DeathCause = p.LastCause;
        }

        private void Update()
        {
            _zone ??= FindFirstObjectByType<ZoneState>();
            if (_zone != null && _zone.Started && !_zone.Finished && Time.timeScale > 0f)
                RunStats.TimeSurvived += Time.deltaTime;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Motor == null || p.Dead)
                    continue;
                Vector3 pos = p.transform.position;
                int tick = p.Motor.Telekinesis.LastThrowTick;
                if (_last.TryGetValue(p, out (Vector3 pos, int throwTick) prev))
                {
                    float step = Vector3.Distance(prev.pos, pos);
                    if (step < 20f)                                           // pas une téléportation
                    {
                        RunStats.Distance += step;
                        Vector3 v = (pos - prev.pos) / Mathf.Max(1e-4f, Time.deltaTime);
                        v.y = 0f;
                        if (Time.deltaTime > 1e-4f && v.magnitude < 60f)
                            RunStats.MaxSpeed = Mathf.Max(RunStats.MaxSpeed, v.magnitude);
                    }

                    if (tick != prev.throwTick && tick != ItHunts.Movement.MovementState.NeverTick)
                        RunStats.CratesThrown++;
                }

                _last[p] = (pos, tick);
                if (string.IsNullOrEmpty(RunStats.Artifact))
                {
                    var holder = p.GetComponent<ArtifactHolder>();
                    if (holder != null && holder.Current != null)
                        RunStats.Artifact = holder.Current.displayName;
                }
            }
        }

        /// <summary>Fin de l'acte (sortie franchie ou mort) : durée de l'acte notée.</summary>
        public static void CloseCurrentAct()
        {
            var t = FindFirstObjectByType<RunStatsTracker>();
            string name = RunSession.Active && RunSession.Definition != null && RunSession.Act < RunSession.Definition.levels.Count
                ? MainMenu.PlaytestActName(RunSession.Act) : SceneManager.GetActiveScene().name;
            if (t != null)
                t.CloseAct(name);
        }

        public void CloseAct(string name)
        {
            RunStats.ActTimes.Add((name, RunStats.TimeSurvived - _actStart));
            _actStart = RunStats.TimeSurvived;
        }
    }
}
