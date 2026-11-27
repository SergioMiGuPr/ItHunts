using System;
using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Vagues d'une zone (D-041, revu par D-060), objet de la scène (pas du joueur) : au début de chaque pic de vague, il fait
    /// apparaître une fournée de goules et de volants de la réserve (inactifs, créés par le constructeur) : waveSpawns du moment,
    /// sous le plafond de population. Rien entre deux vagues : ni apparition continue, ni remplacement des tués. Apparition : un
    /// point d'apparition (SpawnPoint) hors de la vue de tous les joueurs, à minDistance au moins de chacun, jamais dans la salle
    /// ou le tronçon d'un joueur (RoomVolume, D-045), jamais dans une salle où un joueur est passé depuis recentVisitSeconds,
    /// jamais dans la salle de départ ; annoncé par une lueur au sol, puis l'ennemi apparaît. Un ennemi tué retourne dans la
    /// réserve. Réseau (étape 11) : tout ceci tourne sur l'autorité.
    /// </summary>
    public sealed class WaveDirector : MonoBehaviour
    {
        [SerializeField] private WaveSettings settings;
        [Tooltip("Parents des goules et des volants de la réserve (inactifs).")]
        [SerializeField] private Transform ghoulPool;
        [SerializeField] private Transform flyerPool;
        [Tooltip("Matériau de la lueur d'annonce d'une apparition.")]
        [SerializeField] private Material telegraphMaterial;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Waves");

        /// <summary>Début d'un pic de vague (numéro du pic) : HUD et son.</summary>
        public static event Action<int> PeakStarted;
        /// <summary>Une apparition (position, type) : mesures.</summary>
        public static event Action<Vector3, SpawnKinds> Spawned;
        public static float LastPeakTime { get; private set; } = float.NegativeInfinity;

        private sealed class Kind
        {
            public SpawnKinds Type;
            public readonly List<GameObject> Members = new List<GameObject>();
            public readonly HashSet<GameObject> CountedDead = new HashSet<GameObject>();
            public readonly Dictionary<GameObject, float> DeadSince = new Dictionary<GameObject, float>();
            public int Wanted, Present, Pending, Spawns, Kills;
            /// <summary>Vague en cours : apparitions qui restent à faire, avant WaveUntil.</summary>
            public int WaveLeft, LastWave, Abandoned;
            public float NextSpawn, WaveUntil;
            public float Level;
            public int RoundRobin;
            /// <summary>Coop : joueur autour de qui chaque ennemi est apparu (sa part).</summary>
            public readonly Dictionary<GameObject, int> Owner = new Dictionary<GameObject, int>();
        }

        private struct PendingSpawn
        {
            public Kind Kind;
            public GameObject Member;
            public SpawnPoint Point;
            public int OwnerId;
            public float At;
            public GameObject Marker;
            /// <summary>Effet de sortie déjà joué (coin sombre : la goule paraît spawnShadowDelay s après).</summary>
            public bool ExitPlayed;
        }

        private readonly Kind _ghouls = new Kind { Type = SpawnKinds.Goule };
        private readonly Kind _flyers = new Kind { Type = SpawnKinds.Volant };
        private readonly List<PendingSpawn> _pending = new List<PendingSpawn>();
        private readonly List<GameObject> _markers = new List<GameObject>();
        private readonly List<Vector3> _players = new List<Vector3>();
        private readonly List<int> _playerIds = new List<int>();
        private readonly List<SpawnPoint> _candidates = new List<SpawnPoint>();
        /// <summary>Dernier passage d'un joueur dans chaque volume (salle ou tronçon).</summary>
        private readonly Dictionary<ItHunts.Level.RoomVolume, float> _visits = new Dictionary<ItHunts.Level.RoomVolume, float>();
        private ZoneState _zone;
        private System.Random _rng = new System.Random(1);
        private AudioSource _audio;
        private AudioClip _horn;
        private int _peakIndex;

        public WaveSettings Settings => settings;
        public bool InPeak { get; private set; }
        public float PhaseLeft { get; private set; }
        public int PeakIndex => _peakIndex;
        public int WantedGhouls => _ghouls.Wanted;
        public int WantedFlyers => _flyers.Wanted;
        public int PresentGhouls => _ghouls.Present;
        public int PresentFlyers => _flyers.Present;
        public int PendingCount => _pending.Count;
        /// <summary>Vague en cours : goules et volants qui restent à faire apparaître.</summary>
        public int GhoulWaveLeft => _ghouls.WaveLeft;
        public int FlyerWaveLeft => _flyers.WaveLeft;
        /// <summary>Taille de la dernière vague (goules, volants) : mesures.</summary>
        public int LastGhoulWave => _ghouls.LastWave;
        public int LastFlyerWave => _flyers.LastWave;
        /// <summary>Apparitions abandonnées (pas de point permis avant la fin de la fenêtre de la vague).</summary>
        public int Abandoned => _ghouls.Abandoned + _flyers.Abandoned;
        public float GhoulLevel => _ghouls.Level;
        public float FlyerLevel => _flyers.Level;
        public int Spawns => _ghouls.Spawns + _flyers.Spawns;
        public int Kills => _ghouls.Kills + _flyers.Kills;
        public int SpawnFailures { get; private set; }
        /// <summary>Apparitions annulées : un joueur est entré dans la salle du point pendant l'annonce (D-045).</summary>
        public int SpawnsCancelled { get; private set; }
        public int GhoulPoolSize => _ghouls.Members.Count;
        /// <summary>Coop : multiplicateur du moment (joueurs vivants).</summary>
        public float PlayerMultiplier { get; private set; } = 1f;

        /// <summary>Coop : ennemis vivants apparus pour ce joueur (sa part), par type.</summary>
        public int OwnedBy(int playerId, SpawnKinds kind)
        {
            Kind k = kind == SpawnKinds.Goule ? _ghouls : _flyers;
            int n = 0;
            foreach (KeyValuePair<GameObject, int> o in k.Owner)
                if (o.Value == playerId && o.Key != null && o.Key.activeSelf)
                {
                    var d = o.Key.GetComponent<Damageable>();
                    if (d == null || !d.Dead)
                        n++;
                }

            return n;
        }
        public int FlyerPoolSize => _flyers.Members.Count;

        private void Awake()
        {
            if (ghoulPool != null)
                foreach (Ghoul g in ghoulPool.GetComponentsInChildren<Ghoul>(true))
                    _ghouls.Members.Add(g.gameObject);
            if (flyerPool != null)
                foreach (Flyer f in flyerPool.GetComponentsInChildren<Flyer>(true))
                    _flyers.Members.Add(f.gameObject);
            // Nuit 4 (PR #50) : marque d'apparition de la DA (cercle plein pulsé, même shader que le boss), plus de disque orange.
            Material art = Resources.Load<Material>("M_VFX_Apparition");
            if (art != null)
                telegraphMaterial = art;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake = false;
            _horn = ItHunts.Audio.GameAudio.Or(s => s.wavePeak, ItHunts.Audio.ProceduralSfx.ToClip("Vagues : pic", ItHunts.Audio.ProceduralSfx.WaveHorn()));
        }

        private void OnEnable() => PlayerHealth.Hurt += OnPlayerHurt;

        private void OnDisable()
        {
            PlayerHealth.Hurt -= OnPlayerHurt;
            foreach (IHunter h in HuntDirector.Gather())
                h.Hunt.Release();
        }

        // ---------- Habitants des salles (D-061) ----------

        private readonly List<GameObject> _residents = new List<GameObject>();
        private bool _residentsFound;

        /// <summary>Habitantes déplacées au début de la zone : posées sur un îlot (étage de module sans accès) ou sur la même place.</summary>
        public int ResidentsMoved { get; private set; }

        private UnityEngine.AI.NavMeshPath _residentPath;
        private readonly List<Ghoul> _toPlace = new List<Ghoul>();

        /// <summary>Habitantes gardées à vérifier, une fois le NavMesh construit (il l'est parfois après le début de la zone).</summary>
        private void PlaceResidentsWhenReady()
        {
            if (_toPlace.Count == 0)
                return;
            var nav = FindFirstObjectByType<ZoneNavMesh>();
            if (nav == null || !nav.Built)
                return;
            var placed = new List<Vector3>();
            foreach (Ghoul g in _toPlace)
                PlaceResident(g, placed);
            _toPlace.Clear();
        }

        /// <summary>
        /// Une habitante doit pouvoir sortir de sa salle : posée sur un îlot du NavMesh (étage de module sans accès), elle va à
        /// l'entrée de sa salle (MazeGraph) ; deux habitantes au même endroit sont écartées de 1.6 m.
        /// </summary>
        private void PlaceResident(Ghoul g, List<Vector3> placed)
        {
            var nav = FindFirstObjectByType<ZoneNavMesh>();
            var maze = FindFirstObjectByType<MazeGraph>();
            if (g == null || nav == null || !nav.Built)
                return;
            var filter = new UnityEngine.AI.NavMeshQueryFilter { agentTypeID = nav.SmallAgent, areaMask = UnityEngine.AI.NavMesh.AllAreas };
            Vector3 at = g.transform.position;
            Vector3 to = at;
            int node = maze != null ? maze.NodeAt(at) : -1;
            if (node >= 0 && UnityEngine.AI.NavMesh.SamplePosition(at, out UnityEngine.AI.NavMeshHit here, 2f, filter))
            {
                _residentPath ??= new UnityEngine.AI.NavMeshPath();
                bool outOk = false;
                Vector3 firstEntry = at;
                bool hasEntry = false;
                for (int e = 0; e < maze.EdgeCount && !outOk; e++)
                {
                    if (maze.Edge(e).x != node && maze.Edge(e).y != node)
                        continue;
                    if (!UnityEngine.AI.NavMesh.SamplePosition(maze.Entry(node, e), out UnityEngine.AI.NavMeshHit entry, 10f, filter))
                        continue;
                    if (!hasEntry)
                    {
                        hasEntry = true;
                        firstEntry = entry.position;
                    }

                    outOk = UnityEngine.AI.NavMesh.CalculatePath(here.position, entry.position, filter, _residentPath)
                            && _residentPath.status == UnityEngine.AI.NavMeshPathStatus.PathComplete;
                }

                if (!outOk && hasEntry)
                    to = firstEntry;                                     // îlot : à l'entrée de sa salle
            }

            for (int k = 0; k < 8 && placed.Exists(p => (p - to).sqrMagnitude < 0.8f); k++)
            {
                Vector3 off = to + Quaternion.Euler(0f, k * 45f, 0f) * Vector3.forward * 1.6f;
                if (UnityEngine.AI.NavMesh.SamplePosition(off, out UnityEngine.AI.NavMeshHit o, 1f, filter))
                    to = o.position;
            }

            placed.Add(to);
            if ((to - at).sqrMagnitude > 0.01f)
            {
                g.Spawn(to, g.transform.rotation);
                ResidentsMoved++;
            }
        }

        /// <summary>Habitants gardés et retirés au dernier début de zone (mesures).</summary>
        public int ResidentsKept { get; private set; }
        public int ResidentsRemoved { get; private set; }

        /// <summary>
        /// Habitants (goules et volants posés par le constructeur, hors réserve) : chaque grande salle en garde de
        /// residentGhoulsMin à residentGhoulsMax selon sa taille, et au plus residentFlyersMax volants ; les autres sont retirés
        /// pour la zone. Toujours les mêmes (ordre des noms) : la remise à zéro redonne la même population.
        /// </summary>
        private void ApplyResidents()
        {
            if (settings == null)
                return;
            if (!_residentsFound)
            {
                _residentsFound = true;
                var pool = new HashSet<GameObject>(_ghouls.Members);
                pool.UnionWith(_flyers.Members);
                foreach (Ghoul g in FindObjectsByType<Ghoul>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (!pool.Contains(g.gameObject))
                        _residents.Add(g.gameObject);
                foreach (Flyer f in FindObjectsByType<Flyer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (!pool.Contains(f.gameObject))
                        _residents.Add(f.gameObject);
                _residents.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            }

            _toPlace.Clear();
            ResidentsMoved = 0;
            var ghoulsIn = new Dictionary<ItHunts.Level.RoomVolume, int>();
            var flyersIn = new Dictionary<ItHunts.Level.RoomVolume, int>();
            ResidentsKept = ResidentsRemoved = 0;
            foreach (GameObject m in _residents)
            {
                if (m == null)
                    continue;
                bool flyer = m.GetComponent<Flyer>() != null;
                Vector3 at = m.transform.position;
                ItHunts.Level.RoomVolume room = null;
                foreach (ItHunts.Level.RoomVolume r in ItHunts.Level.RoomVolume.All)
                    if (r != null && r.Kind == ItHunts.Level.RoomKind.Salle && r.Contains(flyer ? new Vector3(at.x, r.transform.position.y, at.z) : at + Vector3.up * 0.5f))
                    {
                        room = r;
                        break;
                    }

                bool keep = true;
                if (room != null)
                {
                    Dictionary<ItHunts.Level.RoomVolume, int> count = flyer ? flyersIn : ghoulsIn;
                    count.TryGetValue(room, out int n);
                    int max = flyer ? settings.residentFlyersMax : WaveMath.ResidentsToKeep(Mathf.Max(room.Size.x, room.Size.z), settings);
                    keep = n < max;
                    count[room] = n + 1;
                }

                if (keep)
                {
                    ResidentsKept++;
                    if (!m.activeSelf)
                        m.SetActive(true);
                    if (!flyer)
                        _toPlace.Add(m.GetComponent<Ghoul>());
                }
                else
                {
                    ResidentsRemoved++;
                    m.SetActive(false);
                }
            }
        }

        /// <summary>Remise à zéro de la zone : toute la réserve rentre, la population de départ sera reposée.</summary>
        public void ResetAll()
        {
            foreach (PendingSpawn p in _pending)
                if (p.Marker != null)
                    p.Marker.SetActive(false);
            _pending.Clear();
            foreach (Kind k in new[] { _ghouls, _flyers })
            {
                foreach (GameObject m in k.Members)
                    if (m != null)
                        m.SetActive(false);
                k.CountedDead.Clear();
                k.DeadSince.Clear();
                k.Owner.Clear();
                k.Present = k.Pending = k.WaveLeft = 0;
                k.NextSpawn = k.WaveUntil = 0f;
            }

            _peakIndex = 0;
            _visits.Clear();
            _roomFoes.Clear();
            _cleared.Clear();
            _respiteUntil = float.NegativeInfinity;
            ApplyResidents();
            HuntDirector.ResetCounters();
            var run = FindFirstObjectByType<ItHunts.Artifacts.RunState>();
            _rng = new System.Random((run != null ? run.Seed : 1) * 7919 + (_zone != null ? _zone.Resets : 0));
        }

        /// <summary>Surface de la carte (m², sol des volumes) et multiplicateur du nombre voulu qui en découle.</summary>
        public float MapArea { get; private set; }
        public float AreaScale { get; private set; } = 1f;

        /// <summary>Surface de la carte : somme du sol des volumes de salle et de tronçon (le recouvrement est négligé).</summary>
        public static float MeasureMapArea()
        {
            float a = 0f;
            foreach (ItHunts.Level.RoomVolume r in FindObjectsByType<ItHunts.Level.RoomVolume>(FindObjectsSortMode.None))
                a += r.Size.x * r.Size.z;
            return a;
        }

        public static float AreaScaleFor(float area, WaveSettings s) =>
            s == null || !s.scaleWithMapArea ? 1f : Mathf.Clamp(area / Mathf.Max(1f, s.referenceArea), s.minAreaScale, s.maxAreaScale);

        private void FixedUpdate()
        {
            if (settings == null)
                return;
            PlaceResidentsWhenReady();
            if (MapArea <= 0f)
            {
                MapArea = MeasureMapArea();
                AreaScale = AreaScaleFor(MapArea, settings);
            }
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            if (_zone == null)
                _zone = FindFirstObjectByType<ZoneState>();
            if (_zone == null || _zone.Settings == null || _zone.Finished || !_zone.Started)
                return;                                                  // salle de départ (D-048) : rien avant la première sortie
            float now = Time.time, elapsed = _zone.Elapsed, timer = _zone.Settings.timer;

            // Chasseurs (D-061), revus toutes les 0.5 s ; aucun pendant le combat de boss.
            if (now >= _nextHunt || _nextHunt - now > 1f)
            {
                _nextHunt = now + 0.5f;
                HuntDirector.Tick(PlayerHealth.All, _zone.BossFightActive ? 0 : WaveMath.ChasersPerPlayer(elapsed, timer, settings) , HuntDirector.Gather());
            }

            if (_zone.BossFightActive)
                return;
            InPeak = WaveMath.InPeak(elapsed, settings, out float left, out int index);
            PhaseLeft = left;

            _players.Clear();
            _playerIds.Clear();
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead)
                {
                    _players.Add(p.transform.position);
                    _playerIds.Add(p.PlayerId);
                }

            // Coop : nombre voulu et plafonds × (1 + part × joueurs vivants en plus) ; accalmie : plafond de chasse à 60 %.
            PlayerMultiplier = WaveMath.PlayerMultiplier(_players.Count, settings) * ItHunts.Level.RunSession.EnemyCount;   // difficulté (D-048)
            float ghoulBase = WaveMath.Level(elapsed, timer, settings.ghoulTrend, settings.ghoulEnraged, settings);
            float flyerBase = WaveMath.Level(elapsed, timer, settings.flyerTrend, settings.flyerEnraged, settings);
            _ghouls.Level = ghoulBase * PlayerMultiplier * AreaScale;            // densité par surface (étape 8)
            _flyers.Level = flyerBase * PlayerMultiplier * AreaScale;

            TrackVisits(now);
            CheckCleared(now);
            UpdateKind(_ghouls, now);
            UpdateKind(_flyers, now);

            if (settings.continuous)
            {
                // Pression constante (nuit 4) : pas de pic ni de klaxon ; sous le quota, une apparition toutes les continuousInterval s,
                // derrière le joueur et hors de vue (Begin), jamais pendant le repos qui suit une grosse blessure.
                InPeak = false;
                int act = Mathf.Clamp(ItHunts.Level.RunSession.Act, 0, 2);
                float enraged = elapsed >= timer ? settings.enragedQuotaScale : 1f;
                float scale = PlayerMultiplier * EnemyScaling.CountMultiplier() * enraged;
                int players = Mathf.Max(1, _players.Count);
                int quotaG = Mathf.RoundToInt(Quota(settings.continuousQuota, act) * players * scale);
                int quotaF = Mathf.RoundToInt(Quota(settings.continuousFlyerQuota, act) * players * scale);
                int cap = settings.hardCapPerPlayer * players;
                _ghouls.Wanted = Mathf.Min(cap, Mathf.Max(quotaG, _ghouls.Wanted));
                _flyers.Wanted = Mathf.Min(cap / 3, Mathf.Max(quotaF, _flyers.Wanted));
                UpdatePending(now);
                if (_players.Count == 0 || now < _relaxUntil || InRespite)
                    return;
                float interval = settings.continuousInterval / (elapsed >= timer ? 2f : 1f);
                ContinuousStep(_ghouls, quotaG, interval, now);
                ContinuousStep(_flyers, quotaF, interval * 1.5f, now);
                return;
            }

            // Début d'un pic : une fournée par type (D-060), sous le plafond de population.
            if (InPeak && index != _peakIndex)
            {
                _peakIndex = index;
                LastPeakTime = now;
                if (_horn != null)
                    _audio.PlayOneShot(_horn, 0.9f);
                float scale = PlayerMultiplier * AreaScale;
                StartWave(_ghouls, WaveMath.Level(elapsed, timer, settings.ghoulWaveSpawns, settings.ghoulWaveEnraged, settings) * scale, now);
                StartWave(_flyers, WaveMath.Level(elapsed, timer, settings.flyerWaveSpawns, settings.flyerWaveEnraged, settings) * scale, now);
                PeakStarted?.Invoke(index);
            }

            UpdatePending(now);
            if (_players.Count == 0 || InRespite)
                return;
            WaveStep(_ghouls, now);
            WaveStep(_flyers, now);
        }

        private float _relaxUntil;

        // ---------- Répit après une salle nettoyée (D-122) ----------

        // Par salle (nom de salle : une salle en L a plusieurs volumes) : ennemis vus dedans au dernier passage.
        private readonly Dictionary<string, HashSet<IHunter>> _roomFoes = new Dictionary<string, HashSet<IHunter>>();
        private readonly HashSet<string> _occupied = new HashSet<string>();
        private readonly HashSet<string> _cleared = new HashSet<string>();
        private float _nextClearCheck, _respiteUntil = float.NegativeInfinity;

        /// <summary>Répit en cours : aucune apparition, les annonces en cours sont annulées.</summary>
        public bool InRespite => Time.time < _respiteUntil && !(_zone != null && _zone.Enraged);   // jamais en Enragé (D-122)
        /// <summary>Secondes de répit restantes (0 hors répit).</summary>
        public float RespiteLeft => Mathf.Max(0f, _respiteUntil - Time.time);
        /// <summary>Répits déclenchés (mesures, tests).</summary>
        public int Respites { get; private set; }

        /// <summary>
        /// Toutes les 0.5 s, pour chaque salle où se tient un joueur : les ennemis vivants dedans (tous ses volumes). La salle est nettoyée
        /// quand il n'en reste aucun alors qu'au passage précédent il y en avait, et qu'au moins l'un d'eux est mort (un ennemi qui sort
        /// en suivant le joueur ne compte pas). Une salle ne donne qu'un répit par zone (pas de répit sans fin en tuant une goule isolée),
        /// jamais en Enragé. Une salle quittée est oubliée.
        /// </summary>
        private void CheckCleared(float now)
        {
            if (settings.clearRespite <= 0f || now < _nextClearCheck)
                return;
            _nextClearCheck = now + 0.5f;
            _occupied.Clear();
            foreach (Vector3 p in _players)
                foreach (ItHunts.Level.RoomVolume r in ItHunts.Level.RoomVolume.All)
                    if (r != null && r.Kind != ItHunts.Level.RoomKind.Depart && !string.IsNullOrEmpty(r.RoomName) && r.Contains(p + Vector3.up * 0.5f))
                        _occupied.Add(r.RoomName);
            List<IHunter> hunters = HuntDirector.Gather();
            bool enraged = _zone != null && _zone.Enraged;
            foreach (string room in _occupied)
            {
                var now_ = new HashSet<IHunter>();
                foreach (IHunter h in hunters)
                    if (h.CanHunt && InRoom(room, h.Position + Vector3.up * 0.5f))
                        now_.Add(h);
                if (_roomFoes.TryGetValue(room, out HashSet<IHunter> before) && before.Count > 0 && now_.Count == 0 && !enraged
                    && !_cleared.Contains(room) && AnyDied(before))
                {
                    _cleared.Add(room);
                    StartRespite(now);
                }

                _roomFoes[room] = now_;
            }

            _forget.Clear();
            foreach (string r in _roomFoes.Keys)
                if (!_occupied.Contains(r))
                    _forget.Add(r);
            foreach (string r in _forget)
                _roomFoes.Remove(r);
        }

        private static bool InRoom(string room, Vector3 p)
        {
            foreach (ItHunts.Level.RoomVolume r in ItHunts.Level.RoomVolume.All)
                if (r != null && r.RoomName == room && r.Contains(p))
                    return true;
            return false;
        }

        private static bool AnyDied(HashSet<IHunter> foes)
        {
            foreach (IHunter h in foes)
                if (h is Component c && (c == null || (c.TryGetComponent(out ItHunts.Combat.Damageable d) && d.Dead)))
                    return true;
            return false;
        }

        private readonly List<string> _forget = new List<string>();

        /// <summary>Commence (ou prolonge) le répit de clearRespite s (aussi appelé par les tests).</summary>
        public void StartRespite(float now)
        {
            _respiteUntil = Mathf.Max(_respiteUntil, now + settings.clearRespite);
            Respites++;
        }
        private static int Quota(int[] perAct, int act) => perAct == null || perAct.Length == 0 ? 6 : perAct[Mathf.Clamp(act, 0, perAct.Length - 1)];

        /// <summary>Repos (cycle L4D) : un joueur vient de perdre beaucoup de PV : aucune apparition pendant relaxAfterHurt s.</summary>
        private void OnPlayerHurt(PlayerHurtEvent e)
        {
            if (settings == null || !settings.continuous || e.Damage < 30f)
                return;
            _relaxUntil = Mathf.Max(_relaxUntil, Time.time + settings.relaxAfterHurt);
        }

        /// <summary>Mode continu : sous le quota de chasseurs vivants, une apparition par intervalle.</summary>
        private void ContinuousStep(Kind k, int quota, float interval, float now)
        {
            if (k.Present + k.Pending >= quota || now < k.NextSpawn)
                return;
            k.NextSpawn = now + interval;
            if (Begin(k, now, settings.telegraph))
                k.Spawns++;
        }

        private void StartWave(Kind k, float spawns, float now)
        {
            k.Abandoned += k.WaveLeft;                                   // reste de la vague précédente : abandonné
            k.WaveLeft = k.LastWave = WaveMath.WaveBatch(spawns, k.Present, k.Pending, k.Wanted);
            k.WaveUntil = now + settings.waveSpawnWindow;
            k.NextSpawn = now;
        }

        /// <summary>Vague en cours : une apparition tous les waveSpawnStep, jusqu'à la fin de la fenêtre.</summary>
        private void WaveStep(Kind k, float now)
        {
            if (k.WaveLeft <= 0)
                return;
            if (now >= k.WaveUntil)
            {
                k.Abandoned += k.WaveLeft;
                k.WaveLeft = 0;
                return;
            }

            if (now < k.NextSpawn)
                return;
            k.NextSpawn = now + settings.waveSpawnStep;
            if (k.Present + k.Pending >= k.Wanted)
            {
                k.Abandoned += k.WaveLeft;                               // plafond atteint entre-temps
                k.WaveLeft = 0;
                return;
            }

            if (Begin(k, now, settings.telegraph))
            {
                k.WaveLeft--;
                k.Spawns++;
            }
        }

        private float _nextHunt;

        /// <summary>Dernier passage d'un joueur dans chaque volume qui le contient (salle, tronçon).</summary>
        private void TrackVisits(float now)
        {
            foreach (Vector3 p in _players)
                foreach (ItHunts.Level.RoomVolume r in ItHunts.Level.RoomVolume.All)
                    if (r != null && r.Contains(p + Vector3.up * 0.5f))
                        _visits[r] = now;
        }

        /// <summary>Dernier passage d'un joueur dans ce volume (moins l'infini : jamais).</summary>
        public float LastVisit(ItHunts.Level.RoomVolume r) =>
            r != null && _visits.TryGetValue(r, out float t) ? t : float.NegativeInfinity;

        /// <summary>Note un passage (tests, mesures).</summary>
        public void NoteVisit(ItHunts.Level.RoomVolume r, float now)
        {
            if (r != null)
                _visits[r] = now;
        }

        /// <summary>
        /// Règles de lieu d'une apparition (D-060) : jamais dans la salle de départ, jamais dans un volume où un joueur est
        /// passé depuis recentVisitSeconds.
        /// </summary>
        public bool PlaceAllowed(Vector3 point, float now)
        {
            Vector3 p = point + Vector3.up * 0.5f;
            foreach (ItHunts.Level.RoomVolume r in ItHunts.Level.RoomVolume.All)
            {
                if (r == null || !r.Contains(p))
                    continue;
                if (r.Kind == ItHunts.Level.RoomKind.Depart)
                    return false;
                if (WaveMath.RecentlyVisited(LastVisit(r), now, settings != null ? settings.recentVisitSeconds : 60f))
                    return false;
            }

            return true;
        }

        /// <summary>Présents, morts (retour dans la réserve après despawnDelay), plafond de population.</summary>
        private void UpdateKind(Kind k, float now)
        {
            k.Present = 0;
            foreach (GameObject m in k.Members)
            {
                if (m == null || !m.activeSelf)
                    continue;
                var d = m.GetComponent<Damageable>();
                if (d != null && d.Dead)
                {
                    if (k.CountedDead.Add(m))
                    {
                        k.Kills++;
                        k.DeadSince[m] = now;
                    }

                    if (now - k.DeadSince[m] >= settings.despawnDelay)
                    {
                        m.SetActive(false);
                        k.CountedDead.Remove(m);
                        k.DeadSince.Remove(m);
                        k.Owner.Remove(m);
                    }

                    continue;
                }

                k.Present++;
            }

            k.Pending = 0;
            foreach (PendingSpawn p in _pending)
                if (p.Kind == k)
                    k.Pending++;
            k.Wanted = WaveMath.Cap(k.Level);
        }

        /// <summary>Debug et mesures : pose d'un coup, sans annonce, hors de vue, jusqu'au plafond.</summary>
        private void FillNow(Kind k)
        {
            int want = k.Wanted;
            for (int i = k.Present; i < want; i++)
            {
                if (!Begin(k, Time.time, 0f))
                    break;
                k.Spawns++;
                k.Present++;
            }

            UpdatePending(Time.time);
        }

        /// <summary>Remplit tout de suite jusqu'au plafond de population (mesures, debug).</summary>
        public void FillToWanted()
        {
            UpdateKind(_ghouls, Time.time);
            UpdateKind(_flyers, Time.time);
            FillNow(_ghouls);
            FillNow(_flyers);
        }

        /// <summary>Choisit un membre libre et un point ; l'annonce commence (lueur), l'apparition suit après le délai.</summary>
        private bool Begin(Kind k, float now, float telegraph)
        {
            GameObject member = null;
            foreach (GameObject m in k.Members)
                if (m != null && !m.activeSelf && !IsPending(m))
                {
                    member = m;
                    break;
                }

            if (member == null || _players.Count == 0)
                return false;
            int who = ChoosePlayer(k);
            SpawnPoint point = ChoosePoint(k.Type, now, who);
            if (point == null)
            {
                SpawnFailures++;
                return false;
            }

            point.LastUsed = now;
            GameObject marker = telegraph > 0f ? ShowMarker(point) : null;
            _pending.Add(new PendingSpawn { Kind = k, Member = member, Point = point, OwnerId = _playerIds[who], At = now + telegraph, Marker = marker });
            k.Pending++;
            return true;
        }

        private bool IsPending(GameObject m)
        {
            foreach (PendingSpawn p in _pending)
                if (p.Member == m)
                    return true;
            return false;
        }

        private void UpdatePending(float now)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingSpawn p = _pending[i];
                if (p.Marker != null)
                {
                    float t = Mathf.Clamp01(1f - (p.At - now) / Mathf.Max(0.01f, settings.telegraph));
                    p.Marker.transform.localScale = new Vector3(1.6f + t, 0.01f, 1.6f + t);
                }

                if (now < p.At)
                    continue;
                if (p.Marker != null)
                    p.Marker.SetActive(false);
                if (InRespite || ItHunts.Level.RoomVolume.SharesRoomWithAny(p.Point.transform.position, _players))
                {
                    _pending.RemoveAt(i);
                    SpawnsCancelled++;                                   // un joueur est entré dans la salle pendant l'annonce (D-045)
                    continue;
                }

                // Coin sombre (nuit 5, lot 4) : la fumée d'encre converge d'abord, la goule paraît quand elle est la plus dense.
                if (!p.ExitPlayed && p.Kind == _ghouls && p.Point.Effect == SpawnEffect.Ombre)
                {
                    GameVfxSettings fx = GameVfx.Settings;
                    if (fx != null && fx.spawnShadow != null && fx.spawnShadowDelay > 0f)
                    {
                        SpawnExitVfx(fx.spawnShadow, p.Point);
                        p.ExitPlayed = true;
                        p.Marker = null;
                        p.At = now + fx.spawnShadowDelay;
                        _pending[i] = p;
                        continue;
                    }
                }

                _pending.RemoveAt(i);
                Activate(p.Member, p.Point);
                if (!p.ExitPlayed && p.Kind == _ghouls)
                    PlayExit(p.Member, p.Point);
                p.Kind.Owner[p.Member] = p.OwnerId;
                Spawned?.Invoke(p.Point.transform.position, p.Kind.Type);
            }
        }

        /// <summary>
        /// Effet de la DA à la sortie d'une goule (nuit 5, lot 4), une fois activée : trou d'ombre au sol (elle monte de spawnHoleDepth m
        /// entre spawnHoleRise.x et .y s) ou grille ; le coin sombre est joué avant l'activation (UpdatePending).
        /// </summary>
        private static void PlayExit(GameObject m, SpawnPoint point)
        {
            GameVfxSettings fx = GameVfx.Settings;
            if (fx == null || point == null)
                return;
            switch (point.Effect)
            {
                case SpawnEffect.Trou:
                    if (fx.spawnHole == null)
                        return;
                    SpawnExitVfx(fx.spawnHole, point);
                    var g = m.GetComponent<Ghoul>();
                    if (g != null)
                        g.Emerge(fx.spawnHoleDepth, fx.spawnHoleRise.x, fx.spawnHoleRise.y);
                    break;
                case SpawnEffect.Grille:
                    if (fx.spawnGrate != null)
                        GameVfx.Spawn(fx.spawnGrate, point.transform.position, point.transform.rotation, 1f, null, 4f);   // +Y du point : côté salle
                    break;
                case SpawnEffect.Ombre:
                    if (fx.spawnShadow != null)
                        SpawnExitVfx(fx.spawnShadow, point);             // sans délai (réglage à 0) : en même temps que la goule
                    break;
            }
        }

        /// <summary>Effet posé au sol sous le point (pivot au sol), tourné comme le point.</summary>
        private static void SpawnExitVfx(GameObject prefab, SpawnPoint point)
        {
            Vector3 at = point.transform.position;
            if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out RaycastHit ground, 3f, ~0, QueryTriggerInteraction.Ignore))
                at = ground.point;
            GameVfx.Spawn(prefab, at, Quaternion.Euler(0f, point.transform.eulerAngles.y, 0f), 1f, null, 4f);   // filet : il se détruit seul vers 2,5 s
        }

        private static void Activate(GameObject m, SpawnPoint point)
        {
            Transform t = point.transform;
            m.transform.SetPositionAndRotation(t.position, Quaternion.Euler(0f, t.eulerAngles.y, 0f));
            m.SetActive(true);
            var g = m.GetComponent<Ghoul>();
            if (g != null)
                g.Spawn(t.position, Quaternion.Euler(0f, t.eulerAngles.y, 0f));
            var f = m.GetComponent<Flyer>();
            if (f != null)
                f.Spawn(t.position);
        }

        /// <summary>
        /// Coop : à tour de rôle, le premier joueur (vivant) à qui il manque des ennemis de sa part (part − les siens − ceux qui
        /// arrivent pour lui) ; sinon le suivant dans le tour.
        /// </summary>
        private int ChoosePlayer(Kind k)
        {
            int n = _players.Count;
            if (n <= 1)
                return 0;
            int[] shares = WaveMath.Shares(k.Wanted, n);
            var deficits = new int[n];
            for (int i = 0; i < n; i++)
            {
                int pending = 0;
                foreach (PendingSpawn p in _pending)
                    if (p.Kind == k && p.OwnerId == _playerIds[i])
                        pending++;
                deficits[i] = shares[i] - OwnedBy(_playerIds[i], k.Type) - pending;
            }

            int who = WaveMath.NextPlayer(deficits, k.RoundRobin);
            if (who < 0)
                who = k.RoundRobin % n;
            k.RoundRobin = who + 1;
            return who;
        }

        /// <summary>
        /// Un point du bon type, à minDistance au moins de chaque joueur et à maxDistance au plus du joueur choisi, hors de la vue
        /// de tous les joueurs, pas utilisé à l'instant.
        /// </summary>
        private SpawnPoint ChoosePoint(SpawnKinds kind, float now, int forPlayer)
        {
            _candidates.Clear();
            foreach (SpawnPoint p in SpawnPoint.All)
                if (p != null && p.Accepts(kind) && now - p.LastUsed > 2f
                    && WaveMath.DistanceOkFor(p.transform.position, _players, forPlayer, settings.minDistance, settings.maxDistance)
                    && !ItHunts.Level.RoomVolume.SharesRoomWithAny(p.transform.position, _players)    // jamais dans la salle d'un joueur (D-045)
                    && PlaceAllowed(p.transform.position, now))                                       // salle récente, salle de départ (D-060)
                    _candidates.Add(p);
            for (int i = _candidates.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (_candidates[i], _candidates[j]) = (_candidates[j], _candidates[i]);
            }

            int tests = 0;
            foreach (SpawnPoint p in _candidates)
            {
                if (tests++ >= 24)
                    break;
                if (Hidden(p.transform.position + Vector3.up * (kind == SpawnKinds.Goule ? 0.6f : 0f)))
                    return p;
            }

            return null;
        }

        /// <summary>Hors de la vue de tous les joueurs : hors du champ de vision, ou caché par le décor.</summary>
        public bool Hidden(Vector3 point)
        {
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                if (VisibleTo(p, point))
                    return false;
            }

            return true;
        }

        /// <summary>Le joueur voit-il ce point (dans son champ de vision et sans décor entre ses yeux et le point) ? Règle commune : PlayerSight.</summary>
        public bool VisibleTo(PlayerHealth p, Vector3 point) => PlayerSight.VisibleTo(p, point, 10f);

        private GameObject ShowMarker(SpawnPoint point)
        {
            GameObject m = null;
            foreach (GameObject x in _markers)
                if (x != null && !x.activeSelf)
                {
                    m = x;
                    break;
                }

            if (m == null)
            {
                m = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                m.name = "Annonce d'apparition";
                Destroy(m.GetComponent<Collider>());
                m.transform.SetParent(transform, false);
                var r = m.GetComponent<MeshRenderer>();
                if (telegraphMaterial != null)
                    r.sharedMaterial = telegraphMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _markers.Add(m);
            }

            Vector3 at = point.transform.position;
            // Point de volant en l'air : la marque est projetée au sol (elle ne flotte plus dans le vide).
            if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out RaycastHit ground, 40f, ~0, QueryTriggerInteraction.Ignore))
                at = ground.point;
            m.transform.position = at + Vector3.up * 0.03f;
            m.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
            m.SetActive(true);
            return m;
        }
    }
}
