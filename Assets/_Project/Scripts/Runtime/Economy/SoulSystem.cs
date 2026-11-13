using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.Rendering;

namespace ItHunts.Economy
{
    /// <summary>
    /// Éclats d'âme (étape 7, D-043), objet de la scène : à la mort d'un ennemi auquel un joueur a pris part dans les 5 dernières
    /// secondes, chaque joueur à moins de 40 m reçoit ses propres éclats (valeur de l'ennemi, ×2 en Enragé au moment de la mort),
    /// visibles par lui seul (dessinés pour sa caméra) et que seule son orbe ramasse. Les éclats tombent, l'orbe les attire dans
    /// son rayon de collecte ; au bout de 30 s, ils disparaissent (clignotement les 5 dernières). Le traqueur ne donne jamais d'âmes.
    /// Réseau (étape 11) : création, attribution et collecte sur l'autorité (personne ne vole les âmes d'un autre).
    /// </summary>
    public sealed class SoulSystem : MonoBehaviour
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Matériau des éclats (instancié pour le GPU).")]
        [SerializeField] private Material soulMaterial;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Souls");

        private struct Shard
        {
            public int Owner;
            public Vector3 Position, Velocity;
            public float Born, FloorY;
            public bool Pulled;
        }

        private readonly List<Shard> _shards = new List<Shard>();
        private readonly Matrix4x4[] _matrices = new Matrix4x4[1023];
        private readonly Dictionary<int, int> _lost = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _collected = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _spawned = new Dictionary<int, int>();
        private readonly List<Vector3> _pos = new List<Vector3>();
        private readonly List<bool> _alive = new List<bool>();
        private readonly List<PlayerHealth> _players = new List<PlayerHealth>();
        private Mesh _mesh;
        private ZoneState _zone;
        private System.Random _rng = new System.Random(3);
        private AudioClip _pickup;
        private float _nextPickupSound;

        public EconomySettings Settings => settings;
        public int Count => _shards.Count;
        public int Lost(int playerId) => _lost.TryGetValue(playerId, out int n) ? n : 0;
        public int Collected(int playerId) => _collected.TryGetValue(playerId, out int n) ? n : 0;
        public int SpawnedFor(int playerId) => _spawned.TryGetValue(playerId, out int n) ? n : 0;
        public int CountFor(int playerId)
        {
            int n = 0;
            foreach (Shard s in _shards)
                if (s.Owner == playerId)
                    n++;
            return n;
        }

        private void Awake()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            _pickup = ItHunts.Audio.ProceduralSfx.ToClip("Âme ramassée", ItHunts.Audio.ProceduralSfx.SoulPickup());
        }

        private void OnEnable()
        {
            Damageable.Died += OnDied;
            RenderPipelineManager.beginCameraRendering += Draw;
            // Améliorations (D-043) branchées sur les dégâts des objets lancés et de la répulsion.
            ImpactDamager.OwnerDamageMultiplier = PlayerUpgrades.ThrowMultiplier;
            RepulsionWave.RepelDamageFor = id =>
            {
                PlayerUpgrades u = PlayerUpgrades.OfId(id);
                return u != null ? u.RepelDamage : 0f;
            };
        }

        private void OnDisable()
        {
            Damageable.Died -= OnDied;
            RenderPipelineManager.beginCameraRendering -= Draw;
            ImpactDamager.OwnerDamageMultiplier = null;
            RepulsionWave.RepelDamageFor = null;
        }

        /// <summary>Remise à zéro (zone) : plus d'éclats au sol.</summary>
        public void Clear() => _shards.Clear();

        private void OnDied(Damageable d)
        {
            if (settings == null || d == null || d.ImmuneToOrbs || d.Settings == null || d.Settings.soulValue <= 0)
                return;
            float now = Time.time;
            if (!SoulMath.Involved(d.LastAttackerId, d.LastAttackTime, now, settings.involvementWindow))
                return;                                                  // mort sans joueur : pas d'âme
            if (_zone == null)
                _zone = FindFirstObjectByType<ZoneState>();
            int souls = SoulMath.SoulsAtDeath(d.Settings.soulValue, _zone != null && _zone.Enraged, settings.enragedMultiplier);
            SpawnFor(d.Center, souls, now);
        }

        /// <summary>Éclats pour chaque joueur vivant à moins de shareRadius (mesures, tests, debug).</summary>
        public void SpawnFor(Vector3 at, int souls, float now)
        {
            _players.Clear();
            _pos.Clear();
            _alive.Clear();
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null)
                {
                    _players.Add(p);
                    _pos.Add(p.transform.position);
                    _alive.Add(!p.Dead);
                }

            float floor = at.y - 1f;
            if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                floor = hit.point.y;
            foreach (int i in SoulMath.Recipients(at, _pos, _alive, settings.shareRadius))
            {
                int owner = _players[i].PlayerId;
                for (int k = 0; k < souls; k++)
                {
                    float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
                    var v = new Vector3(Mathf.Cos(a), 1.2f + (float)_rng.NextDouble(), Mathf.Sin(a)) * settings.scatterSpeed * 0.6f;
                    _shards.Add(new Shard { Owner = owner, Position = at, Velocity = v, Born = now, FloorY = floor + settings.soulSize * 0.6f });
                }

                _spawned[owner] = SpawnedFor(owner) + souls;
            }
        }

        private void FixedUpdate()
        {
            if (settings == null || _shards.Count == 0)
                return;
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            float now = Time.time, dt = Time.fixedDeltaTime;
            RunState run = RunState.Current;
            for (int i = _shards.Count - 1; i >= 0; i--)
            {
                Shard s = _shards[i];
                if (now - s.Born >= settings.soulLifetime)
                {
                    _lost[s.Owner] = Lost(s.Owner) + 1;
                    _shards.RemoveAt(i);
                    continue;
                }

                PlayerHealth p = PlayerHealth.Find(s.Owner);
                Vector3 orb = p != null ? OrbOf(p) : s.Position;
                PlayerUpgrades up = p != null ? p.GetComponent<PlayerUpgrades>() : null;
                float radius = settings.collectRadius * (1f + (up != null ? up.CollectRadiusBonus : 0f));
                float pull = settings.pullSpeed * (1f + (up != null ? up.PullSpeedBonus : 0f));
                float d = Vector3.Distance(s.Position, orb);
                if (p != null && !p.Dead && (s.Pulled || d <= radius))
                {
                    s.Pulled = true;                                     // attiré : il suit l'orbe jusqu'au bout
                    s.Position = Vector3.MoveTowards(s.Position, orb, pull * dt);
                    if (Vector3.Distance(s.Position, orb) <= settings.collectDistance)
                    {
                        if (run != null)
                            run.AddSouls(s.Owner, 1);
                        _collected[s.Owner] = Collected(s.Owner) + 1;
                        if (_pickup != null && now >= _nextPickupSound)
                        {
                            _nextPickupSound = now + 0.06f;
                            AudioSource.PlayClipAtPoint(_pickup, orb, 0.35f);
                        }

                        _shards.RemoveAt(i);
                        continue;
                    }
                }
                else
                {
                    s.Velocity.y -= 20f * dt;
                    s.Position += s.Velocity * dt;
                    if (s.Position.y <= s.FloorY)
                    {
                        s.Position.y = s.FloorY;
                        s.Velocity = Vector3.zero;
                    }
                }

                _shards[i] = s;
            }
        }

        /// <summary>L'orbe du joueur (dans sa main droite, devant ses yeux).</summary>
        public static Vector3 OrbOf(PlayerHealth p) => p.transform.position + Vector3.up * 1.2f;

        /// <summary>Dessine pour chaque caméra seulement les éclats de son joueur (âmes individuelles, D-007).</summary>
        private void Draw(ScriptableRenderContext context, Camera cam)
        {
            if (settings == null || soulMaterial == null || _mesh == null || _shards.Count == 0 || cam == null)
                return;
            PlayerHealth owner = Interactables.PlayerOfCamera(cam);   // seulement la caméra du monde de ce joueur
            if (owner == null)
                return;
            int id = owner.PlayerId, n = 0;
            float now = Time.time, size = settings.soulSize;
            foreach (Shard s in _shards)
            {
                if (s.Owner != id || !SoulMath.BlinkVisible(now - s.Born, settings))
                    continue;
                float bob = s.Pulled ? 0f : Mathf.Sin((now - s.Born) * 3f + s.Born) * 0.08f;
                _matrices[n++] = Matrix4x4.TRS(s.Position + Vector3.up * bob, Quaternion.identity, Vector3.one * size);
                if (n >= _matrices.Length)
                    break;
            }

            if (n == 0)
                return;
            var rp = new RenderParams(soulMaterial) { camera = cam, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
            Graphics.RenderMeshInstanced(rp, _mesh, 0, _matrices, n);
        }
    }
}
