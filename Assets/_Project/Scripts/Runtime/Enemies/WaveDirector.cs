using System;
using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Vagues d'une zone (bloc 7, D-041), objet de la scène (pas du joueur) : il fait apparaître les goules et les volants de la
    /// réserve (inactifs, créés par le constructeur) pour suivre le nombre voulu (WaveMath : tendance selon le temps de la zone,
    /// pics annoncés et accalmies, Enragé). Apparition : un point d'apparition (SpawnPoint) hors de la vue de tous les joueurs,
    /// jamais dans la salle d'un joueur (RoomVolume, D-045), entre minDistance et maxDistance, annoncé par une lueur au sol, puis l'ennemi apparaît. Un ennemi tué retourne dans la
    /// réserve ; il est remplacé lentement (refillInterval), en plus des apparitions normales. Les plafonds de chasse suivent la
    /// tendance. Réseau (étape 11) : tout ceci tourne sur l'autorité.
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
            public int Wanted, Present, Pending, Debt, Spawns, Refills, Kills;
            public float NextSpawn, NextRefill;
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
        }

        private readonly Kind _ghouls = new Kind { Type = SpawnKinds.Goule };
        private readonly Kind _flyers = new Kind { Type = SpawnKinds.Volant };
        private readonly List<PendingSpawn> _pending = new List<PendingSpawn>();
        private readonly List<GameObject> _markers = new List<GameObject>();
        private readonly List<Vector3> _players = new List<Vector3>();
        private readonly List<int> _playerIds = new List<int>();
        private readonly List<SpawnPoint> _candidates = new List<SpawnPoint>();
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private ZoneState _zone;
        private System.Random _rng = new System.Random(1);
        private AudioSource _audio;
        private AudioClip _horn;
        private int _peakIndex;
        private bool _needFill = true;

        public WaveSettings Settings => settings;
        public bool InPeak { get; private set; }
        public float PhaseLeft { get; private set; }
        public int PeakIndex => _peakIndex;
        public int WantedGhouls => _ghouls.Wanted;
        public int WantedFlyers => _flyers.Wanted;
        public int PresentGhouls => _ghouls.Present;
        public int PresentFlyers => _flyers.Present;
        public int PendingCount => _pending.Count;
        public int GhoulDebt => _ghouls.Debt;
        public int FlyerDebt => _flyers.Debt;
        public float GhoulLevel => _ghouls.Level;
        public float FlyerLevel => _flyers.Level;
        public int Spawns => _ghouls.Spawns + _flyers.Spawns;
        public int Refills => _ghouls.Refills + _flyers.Refills;
        public int Kills => _ghouls.Kills + _flyers.Kills;
        public int SpawnFailures { get; private set; }
        /// <summary>Apparitions annulées : un joueur est entré dans la salle du point pendant l'annonce (D-045).</summary>
        public int SpawnsCancelled { get; private set; }
        public float NextSpawnIn => Mathf.Max(0f, Mathf.Min(_ghouls.NextSpawn, _flyers.NextSpawn) - Time.time);
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
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 0f;
            _audio.playOnAwake = false;
            _horn = ItHunts.Audio.ProceduralSfx.ToClip("Vagues : pic", ItHunts.Audio.ProceduralSfx.WaveHorn());
        }

        private void OnDisable()
        {
            Ghoul.HuntingScale = Flyer.HuntingScale = 1f;
            Ghoul.PackShare = Flyer.PackShare = 1f;
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
                k.Debt = k.Present = k.Pending = 0;
                k.NextSpawn = k.NextRefill = 0f;
            }

            _peakIndex = 0;
            _needFill = true;
            var run = FindFirstObjectByType<ItHunts.Artifacts.RunState>();
            _rng = new System.Random((run != null ? run.Seed : 1) * 7919 + (_zone != null ? _zone.Resets : 0));
        }

        private void FixedUpdate()
        {
            if (settings == null)
                return;
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            if (_zone == null)
                _zone = FindFirstObjectByType<ZoneState>();
            if (_zone == null || _zone.Settings == null || _zone.Finished || !_zone.Started)
                return;                                                  // salle de départ (D-048) : rien avant la première sortie
            float now = Time.time, elapsed = _zone.Elapsed, timer = _zone.Settings.timer;

            // Pics et accalmies.
            InPeak = WaveMath.InPeak(elapsed, settings, out float left, out int index);
            PhaseLeft = left;
            if (InPeak && index != _peakIndex)
            {
                _peakIndex = index;
                LastPeakTime = now;
                if (_horn != null)
                    _audio.PlayOneShot(_horn, 0.9f);
                PeakStarted?.Invoke(index);
            }

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
            _ghouls.Level = ghoulBase * PlayerMultiplier;
            _flyers.Level = flyerBase * PlayerMultiplier;
            Ghoul.HuntingScale = WaveMath.HuntingScale(ghoulBase, settings.ghoulTrend, settings) * PlayerMultiplier;
            Flyer.HuntingScale = WaveMath.HuntingScale(flyerBase, settings.flyerTrend, settings) * PlayerMultiplier;
            Ghoul.PackShare = Flyer.PackShare = WaveMath.PackShare(InPeak, settings);

            UpdateKind(_ghouls, now);
            UpdateKind(_flyers, now);
            UpdatePending(now);
            if (_players.Count == 0)
                return;

            if (_needFill && settings.fillAtStart)
            {
                _needFill = false;
                FillNow(_ghouls);
                FillNow(_flyers);
            }

            _needFill = false;
            float interval = WaveMath.SpawnInterval(elapsed, timer, settings);
            Spawning(_ghouls, now, interval);
            Spawning(_flyers, now, interval);
        }

        /// <summary>Présents, morts (dette, retour dans la réserve après despawnDelay), nombre voulu.</summary>
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
                        k.Debt++;
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
            k.Wanted = WaveMath.Wanted(k.Level, InPeak, settings);
            k.Debt = WaveMath.ClampDebt(k.Debt, k.Present, k.Pending, k.Wanted);
        }

        private void Spawning(Kind k, float now, float interval)
        {
            if (now >= k.NextSpawn && WaveMath.NormalSpawnAllowed(k.Present, k.Pending, k.Wanted, k.Debt))
            {
                k.NextSpawn = now + interval;
                if (Begin(k, now, settings.telegraph))
                    k.Spawns++;
            }

            if (now >= k.NextRefill && WaveMath.RefillAllowed(k.Present, k.Pending, k.Wanted, k.Debt))
            {
                k.NextRefill = now + settings.refillInterval;
                if (Begin(k, now, settings.telegraph))
                {
                    k.Debt--;
                    k.Refills++;
                }
            }
        }

        /// <summary>Population de départ : posée d'un coup, sans annonce, hors de vue.</summary>
        private void FillNow(Kind k)
        {
            int want = WaveMath.Wanted(k.Level, InPeak, settings);
            for (int i = k.Present; i < want; i++)
            {
                if (!Begin(k, Time.time, 0f))
                    break;
                k.Spawns++;
                k.Present++;
            }

            UpdatePending(Time.time);
        }

        /// <summary>Remplit tout de suite jusqu'au nombre voulu (mesures, debug).</summary>
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
                _pending.RemoveAt(i);
                if (p.Marker != null)
                    p.Marker.SetActive(false);
                if (ItHunts.Level.RoomVolume.SharesRoomWithAny(p.Point.transform.position, _players))
                {
                    SpawnsCancelled++;                                   // un joueur est entré dans la salle pendant l'annonce (D-045)
                    continue;
                }

                Activate(p.Member, p.Point);
                p.Kind.Owner[p.Member] = p.OwnerId;
                Spawned?.Invoke(p.Point.transform.position, p.Kind.Type);
            }
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
                    && !ItHunts.Level.RoomVolume.SharesRoomWithAny(p.transform.position, _players))   // jamais dans la salle d'un joueur (D-045)
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

        /// <summary>Le joueur voit-il ce point (dans son champ de vision et sans décor entre ses yeux et le point) ?</summary>
        public bool VisibleTo(PlayerHealth p, Vector3 point)
        {
            Vector3 eye = p.transform.position + Vector3.up * 1.6f;
            var look = p.GetComponent<PlayerLook>();
            Vector3 forward = look != null ? Quaternion.Euler(look.Pitch, look.Yaw, 0f) * Vector3.forward : p.transform.forward;
            float fov = look != null ? look.CurrentFov : 70f;
            if (WaveMath.OutsideView(eye, forward, point, fov, 16f / 9f, 10f))
                return false;
            Vector3 d = point - eye;
            float len = d.magnitude;
            int n = Physics.RaycastNonAlloc(eye, d / Mathf.Max(0.001f, len), _hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _hits[i].collider;
                if (c.attachedRigidbody == null && c.GetComponentInParent<PlayerHealth>() == null && c.GetComponentInParent<Damageable>() == null)
                    return false;                                        // caché par le décor
            }

            return true;
        }

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
            if (point.Accepts(SpawnKinds.Volant) && !point.Accepts(SpawnKinds.Goule))
                at += Vector3.down * 0.3f;
            m.transform.position = at + Vector3.up * 0.03f;
            m.transform.localScale = new Vector3(1.6f, 0.01f, 1.6f);
            m.SetActive(true);
            return m;
        }
    }
}
