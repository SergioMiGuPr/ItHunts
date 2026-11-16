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
    /// Éclats d'âme (étape 7, D-043 ; D-007 révisée), objet de la scène : à la mort d'un ennemi auquel un joueur a pris part dans
    /// les 5 dernières secondes, ses éclats tombent (valeur de l'ennemi, ×2 en Enragé au moment de la mort). **Âmes gagnées en
    /// commun, dépensées individuellement :** un éclat est commun (visible par tous) et n'importe quelle orbe le ramasse (la plus
    /// proche dans son rayon de collecte) ; chaque joueur vivant reçoit alors 1 âme dans sa propre réserve. Au bout de 30 s au sol,
    /// un éclat disparaît (clignotement les 5 dernières). Le traqueur ne donne jamais d'âmes.
    /// Réseau (étape 11) : création, collecte et gain sur l'autorité.
    /// </summary>
    public sealed class SoulSystem : MonoBehaviour
    {
        [SerializeField] private EconomySettings settings;
        [Tooltip("Matériau des éclats (instancié pour le GPU).")]
        [SerializeField] private Material soulMaterial;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Souls");

        private struct Shard
        {
            public int Collector;                                        // joueur dont l'orbe l'attire (0 : au sol)
            public Vector3 Position, Velocity;
            public float Born, FloorY;
        }

        private readonly List<Shard> _shards = new List<Shard>();
        private readonly Matrix4x4[] _matrices = new Matrix4x4[1023];
        private readonly Dictionary<int, int> _collectedBy = new Dictionary<int, int>();
        private readonly List<PlayerHealth> _alive = new List<PlayerHealth>();
        private Mesh _mesh;
        private Matrix4x4[] _haloMatrices;
        private MaterialPropertyBlock _haloBlock;
        private static readonly int HaloColorId = Shader.PropertyToID("_BaseColor");
        private ZoneState _zone;
        private System.Random _rng = new System.Random(3);
        private AudioClip _pickup;
        private float _nextPickupSound;

        public EconomySettings Settings => settings;
        /// <summary>Éclats au sol ou attirés.</summary>
        public int Count => _shards.Count;
        /// <summary>Éclats tombés, ramassés, perdus (au bout de 30 s) depuis la dernière remise à zéro des compteurs.</summary>
        public int Spawned { get; private set; }
        public int Collected { get; private set; }
        public int Lost { get; private set; }
        /// <summary>Éclats ramassés par l'orbe de ce joueur (chaque joueur vivant en a reçu autant d'âmes).</summary>
        public int CollectedBy(int playerId) => _collectedBy.TryGetValue(playerId, out int n) ? n : 0;

        private void Awake()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _mesh = go.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying)
                Destroy(go);
            else
                DestroyImmediate(go);
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

        /// <summary>Compteurs à zéro (mesures).</summary>
        public void ResetStats()
        {
            Spawned = Collected = Lost = 0;
            _collectedBy.Clear();
        }

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
            Spawn(d.Center, souls, now);
        }

        /// <summary>souls éclats communs tombent ici (mort d'un ennemi ; mesures, tests, debug).</summary>
        public void Spawn(Vector3 at, int souls, float now)
        {
            if (settings == null || souls <= 0)
                return;
            float floor = at.y - 1f;
            if (Physics.Raycast(at + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 40f, ~0, QueryTriggerInteraction.Ignore))
                floor = hit.point.y;
            for (int k = 0; k < souls; k++)
            {
                float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
                var v = new Vector3(Mathf.Cos(a), 1.2f + (float)_rng.NextDouble(), Mathf.Sin(a)) * settings.scatterSpeed * 0.6f;
                _shards.Add(new Shard { Position = at, Velocity = v, Born = now, FloorY = floor + settings.soulSize * 0.6f });
            }

            Spawned += souls;
        }

        /// <summary>Rayon de collecte et vitesse d'aspiration de l'orbe de ce joueur (avec ses améliorations).</summary>
        private void Reach(PlayerHealth p, out float radius, out float pull)
        {
            PlayerUpgrades up = p.GetComponent<PlayerUpgrades>();
            radius = settings.collectRadius * (1f + (up != null ? up.CollectRadiusBonus : 0f));
            pull = settings.pullSpeed * (1f + (up != null ? up.PullSpeedBonus : 0f));
        }

        private void FixedUpdate() => Step(Time.time, Time.fixedDeltaTime);

        /// <summary>Un pas (public pour les tests) : chute, attraction par l'orbe la plus proche, collecte, disparition.</summary>
        public void Step(float now, float dt)
        {
            if (settings == null || _shards.Count == 0)
                return;
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            _alive.Clear();
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead)
                    _alive.Add(p);
            RunState run = RunState.Current;
            for (int i = _shards.Count - 1; i >= 0; i--)
            {
                Shard s = _shards[i];
                if (s.Collector == 0 && now - s.Born >= settings.soulLifetime)
                {
                    Lost++;
                    _shards.RemoveAt(i);
                    continue;
                }

                // Orbe qui l'attire : celle qui l'a déjà pris (tant que son joueur vit), sinon la plus proche dans son rayon.
                PlayerHealth collector = s.Collector != 0 ? PlayerHealth.Find(s.Collector) : null;
                if (collector != null && collector.Dead)
                    collector = null;
                float pull = 0f;
                if (collector != null)
                    Reach(collector, out _, out pull);
                else
                {
                    float best = float.PositiveInfinity;
                    foreach (PlayerHealth p in _alive)
                    {
                        Reach(p, out float r, out float sp);
                        float d = Vector3.Distance(s.Position, OrbOf(p));
                        if (d <= r && d < best)
                        {
                            best = d;
                            collector = p;
                            pull = sp;
                        }
                    }
                }

                if (collector != null)
                {
                    s.Collector = collector.PlayerId;                    // attiré : il suit cette orbe jusqu'au bout
                    Vector3 orb = OrbOf(collector);
                    s.Position = Vector3.MoveTowards(s.Position, orb, pull * dt);
                    if (Vector3.Distance(s.Position, orb) <= settings.collectDistance)
                    {
                        // Gagnées en commun (D-007 révisée) : chaque joueur vivant reçoit 1 âme dans sa propre réserve.
                        if (run != null)
                            foreach (PlayerHealth p in _alive)
                                run.AddSouls(p.PlayerId, 1);
                        Collected++;
                        _collectedBy[collector.PlayerId] = CollectedBy(collector.PlayerId) + 1;
                        if (_pickup != null && now >= _nextPickupSound && Application.isPlaying)
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
                    s.Collector = 0;                                     // son joueur est mort : il retombe
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

        /// <summary>Dessine les éclats (communs) pour chaque caméra du jeu.</summary>
        private void Draw(ScriptableRenderContext context, Camera cam)
        {
            if (settings == null || soulMaterial == null || _mesh == null || _shards.Count == 0 || cam == null)
                return;
            if (cam.cameraType != CameraType.Game && cam.cameraType != CameraType.SceneView)
                return;
            int n = 0;
            float now = Time.time, size = settings.soulSize;
            foreach (Shard s in _shards)
            {
                if (s.Collector == 0 && !SoulMath.BlinkVisible(now - s.Born, settings))
                    continue;
                float bob = s.Collector != 0 ? 0f : Mathf.Sin((now - s.Born) * 3f + s.Born) * 0.08f;
                _matrices[n++] = Matrix4x4.TRS(s.Position + Vector3.up * bob, Quaternion.identity, Vector3.one * size);
                if (n >= _matrices.Length)
                    break;
            }

            if (n == 0)
                return;
            var rp = new RenderParams(soulMaterial) { camera = cam, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
            Graphics.RenderMeshInstanced(rp, _mesh, 0, _matrices, n);
            // Halo (bloc 4) : sphère additive plus grande, même centre ; lisible de loin à travers le brouillard.
            if (settings.soulHaloMaterial != null && settings.soulHaloSize > 0f && settings.soulHaloIntensity > 0f)
            {
                _haloBlock ??= new MaterialPropertyBlock();
                _haloBlock.SetColor(HaloColorId, settings.soulColor * settings.soulHaloIntensity);
                float k = settings.soulHaloSize / Mathf.Max(0.01f, settings.soulSize);
                if (_haloMatrices == null || _haloMatrices.Length < _matrices.Length)
                    _haloMatrices = new Matrix4x4[_matrices.Length];
                for (int i = 0; i < n; i++)
                    _haloMatrices[i] = _matrices[i] * Matrix4x4.Scale(Vector3.one * k);
                var hp = new RenderParams(settings.soulHaloMaterial) { camera = cam, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false, matProps = _haloBlock };
                Graphics.RenderMeshInstanced(hp, _mesh, 0, _haloMatrices, n);
            }
        }
    }
}
