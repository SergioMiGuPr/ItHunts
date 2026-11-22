using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    public enum FlyerMode : byte
    {
        Attente,    // en attente (plafond des volants en chasse) : il plane sur place
        Ronde,      // il tourne au-dessus du joueur
        Annonce,    // cri et lueur avant le piqué
        Pique,
        Remontee,   // il remonte vers sa ronde (après un piqué, une libération, une chute)
        Tenu,       // tenu par la télékinésie : il se débat
        Lance,      // lancé : il éclate au premier impact
        AuSol,      // repoussé : il tombe, étourdi
    }

    /// <summary>
    /// Volant (bloc 6, D-033, D-040) : il sait toujours où sont les joueurs (8 en chasse au plus, les autres en attente ;
    /// aggro à 20 m en vue ou 8 m, jusqu'à 12 en chasse). Il tourne au-dessus du joueur (8 m de haut, 8 m de rayon, 6 m/s)
    /// en suivant le chemin des goules quand un mur le sépare du joueur ; recharge prête et joueur en vue à moins de 16 m : cri
    /// et lueur (0.4 s), puis piqué à 14 m/s vers l'endroit où il sera ; touché : 8 PV et une poussée légère, jamais de
    /// ralentissement ; il remonte (recharge de 3 s). Attrapable à tout moment (caisse de 8 kg), il se débat et se libère au bout
    /// de 2 s, puis remonte en ronde (3 s de recharge). Lancé, il éclate au premier impact (zone de 3 m : 30 au centre, le quart
    /// au bord, jamais à travers un mur, tir ami compris) et meurt. Repoussé, il tombe au sol étourdi 1.5 s (dégâts contre un
    /// mur s'il en percute un en tombant). Le traqueur l'ignore.
    /// </summary>
    [RequireComponent(typeof(EnemyBody))]
    public sealed class Flyer : MonoBehaviour, IHunter
    {
        [SerializeField] private FlyerSettings settings;
        [SerializeField] private Transform visual;
        [SerializeField] private Transform wingLeft, wingRight;
        [Tooltip("Lueur de l'annonce du piqué (cachée sinon).")]
        [SerializeField] private Renderer telegraph;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Flyer");
        public static readonly List<Flyer> All = new List<Flyer>();

        private readonly TargetKeeper _keeper = new TargetKeeper();
        private readonly HuntState _hunt = new HuntState();
        private static int _count;

        private Rigidbody _rb;
        private EnemyBody _body;
        private Grabbable _grab;
        private Damageable _damageable;
        private ZoneNavMesh _nav;
        private readonly EnemyLocomotion _path = new EnemyLocomotion();
        private readonly RaycastHit[] _hits = new RaycastHit[8];
        private Vector3 _home;
        private Vector3 _velocity;
        private float _orbitAngle;
        private float _modeSince;
        private float _nextDive;
        private float _retargetAt;
        private float _nextAggroCheck;
        private bool _wasHeld;
        private bool _burst;
        private PlayerHealth _target;
        private AudioSource _audio;
        private AudioClip _screech, _pop;

        public FlyerMode Mode { get; private set; }
        public FlyerSettings Settings => settings;
        public PlayerHealth Target => _target;
        /// <summary>En chasse : aggro, ou chasseur d'un joueur (D-061).</summary>
        public bool Hunting => _hunt.Hunting;
        public bool Aggroed => _hunt.Aggroed;
        public bool Chaser => _hunt.ChaserFor != null;
        public HuntState Hunt => _hunt;
        public bool CanHunt => isActiveAndEnabled && !Dead && !ZoneState.Asleep;
        public Vector3 Position => _rb != null ? _rb.position : transform.position;

        public float PathLength(PlayerHealth p)
        {
            Vector3 pos = Position;
            return _path.Probe(Ground(pos), p.transform.position, out float l, out _) ? l : Vector3.Distance(pos, p.transform.position);
        }
        public int Dives { get; private set; }
        public int DiveHits { get; private set; }
        public bool Burst => _burst;
        public float LastBurstTotal { get; private set; }
        public Vector3 LastBurstPoint { get; private set; }
        public Vector3 Velocity => _velocity;
        public bool Dead => _damageable != null && _damageable.Dead;

        private void OnEnable() => All.Add(this);
        private void OnDisable()
        {
            All.Remove(this);
            if (telegraph != null)
                telegraph.enabled = false;                                 // recyclé par la réserve
            if (_eyesLit)
                EyeGlow(false);
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _body = GetComponent<EnemyBody>();
            _grab = GetComponent<Grabbable>();
            _damageable = GetComponent<Damageable>();
            _home = transform.position;
            _orbitAngle = (_count++ * 137.508f) % 360f * Mathf.Deg2Rad;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f;
            _audio.maxDistance = 35f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.playOnAwake = false;
            _screech = ItHunts.Audio.ProceduralSfx.ToClip("Volant : cri", ItHunts.Audio.ProceduralSfx.FlyerScreech());
            _pop = ItHunts.Audio.ProceduralSfx.ToClip("Volant : éclatement", ItHunts.Audio.ProceduralSfx.Break());
            var art = ItHunts.Audio.GameAudio.Settings;
            if (art != null && art.flyerWings != null)
            {
                // Ailes en boucle (DA, PR #20), en 3D : on l'entend venir.
                var wings = gameObject.AddComponent<AudioSource>();
                wings.clip = art.flyerWings;
                wings.loop = true;
                wings.spatialBlend = 1f;
                wings.rolloffMode = AudioRolloffMode.Linear;
                wings.maxDistance = 30f;
                wings.minDistance = 2f;
                wings.dopplerLevel = 0f;
                wings.volume = art.flyerWingsVolume;
                wings.pitch = ItHunts.Audio.GameAudio.Pitch();
                wings.time = Random.Range(0f, art.flyerWings.length * 0.9f);
                wings.playOnAwake = true;
                if (isActiveAndEnabled)
                    wings.Play();
            }
            if (telegraph != null)
                telegraph.enabled = false;
        }

        private void Start()
        {
            _nav = FindFirstObjectByType<ZoneNavMesh>();
            if (_nav != null)
                _path.AgentType = _nav.SmallAgent;
        }

        /// <summary>Vagues (D-041) : apparaît à ce point (nouvelle place de départ), entier, en attente.</summary>
        public void Spawn(Vector3 at)
        {
            if (_body == null)
                Awake();
            _home = at;
            if (_body != null)
                _body.SetHome(at);
            if (_damageable != null)
                _damageable.Revive();
            ResetState();
        }

        /// <summary>Remise à zéro de la zone : à sa place, en attente, entier.</summary>
        public void ResetState()
        {
            _path.SpeedScale = ItHunts.Level.RunSession.EnemySpeed;   // difficulté (D-048)
            _burst = false;
            _wasHeld = false;
            _hunt.Clear();
            _target = null;
            _keeper.Clear();
            _velocity = Vector3.zero;
            _nextDive = 0f;
            _path.Clear();
            if (_body != null)
                _body.EndFlight();
            transform.SetPositionAndRotation(_home, Quaternion.identity);
            if (_rb != null)
                _rb.position = _home;
            SetMode(FlyerMode.Attente);
        }

        private void SetMode(FlyerMode m)
        {
            Mode = m;
            _modeSince = Time.time;
        }

        private void FixedUpdate()
        {
            if (Dead && telegraph != null && telegraph.enabled)
                telegraph.enabled = false;                                 // mort pendant l'annonce : plus de signal (correctifs-rendu point 4)
            if (Dead && _eyesLit)
                EyeGlow(false);
            if (settings == null || Dead || ZoneState.Asleep)
                return;                                                    // salle de départ pas encore quittée (D-048)
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            float now = Time.time, dt = Time.fixedDeltaTime;
            if (_nav == null)
                _nav = FindFirstObjectByType<ZoneNavMesh>();
            if (_nav != null && _path.AgentType != _nav.SmallAgent)
                _path.AgentType = _nav.SmallAgent;

            // Tenu : il se débat (la télékinésie le fait trembler et le relâche au bout de 2 s).
            if (_grab != null && _grab.Held)
            {
                if (_damageable != null && _damageable.LastAttackerId != _grab.LastOwnerId)
                    _damageable.NoteAttacker(_grab.LastOwnerId);         // attrapé : il se retournera contre ce joueur
                if (Mode != FlyerMode.Tenu)
                    SetMode(FlyerMode.Tenu);
                _wasHeld = true;
                UpdateVisual(now);
                return;
            }

            if (_wasHeld)
            {
                _wasHeld = false;
                if (_grab != null && _grab.ThrowArmed)
                    SetMode(FlyerMode.Lance);                            // il éclatera au premier impact
                else
                {
                    _body.EndFlight();                                   // lâché ou libéré : il reprend son vol
                    _velocity = Vector3.zero;
                    _nextDive = now + settings.diveCooldown;
                    SetMode(FlyerMode.Remontee);
                }
            }

            if (Mode == FlyerMode.Lance)
            {
                if (!_body.InFlight && now - _modeSince > 0.3f)
                {
                    _body.EndFlight();                                   // posé sans éclater (impact trop lent)
                    _nextDive = now + settings.diveCooldown;
                    SetMode(FlyerMode.Remontee);
                }

                UpdateVisual(now);
                return;
            }

            // Repoussé : il tombe, étourdi (le corps applique la gravité et les dégâts contre un mur).
            if (_body.Knocked && !_body.Stunned)
                _body.Stun(0f);
            if (_body.Busy)
            {
                if (Mode != FlyerMode.AuSol)
                    SetMode(FlyerMode.AuSol);
                _velocity = Vector3.zero;
                UpdateVisual(now);
                return;
            }

            if (Mode == FlyerMode.AuSol)
            {
                _nextDive = Mathf.Max(_nextDive, now + 1f);
                SetMode(FlyerMode.Remontee);
            }

            Vector3 pos = _rb.position;
            Aggro(pos, now);
            if (!Hunting)
            {
                if (Mode != FlyerMode.Attente)
                    SetMode(FlyerMode.Attente);
                _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, settings.acceleration * dt);
                Move(pos, dt);
                UpdateVisual(now);
                return;
            }

            ChooseTarget(pos, now);
            if (_target == null)
            {
                UpdateVisual(now);
                return;
            }

            if (Mode == FlyerMode.Attente)
                SetMode(FlyerMode.Remontee);
            switch (Mode)
            {
                case FlyerMode.Ronde:
                case FlyerMode.Remontee:
                {
                    _orbitAngle += settings.flySpeed / Mathf.Max(1f, settings.orbitRadius) * dt;
                    Vector3 orbit = OrbitPoint(_target);
                    float speed = Mode == FlyerMode.Remontee || Vector3.Distance(pos, orbit) > settings.orbitRadius * 1.5f
                        ? settings.travelSpeed : settings.flySpeed;
                    Steer(pos, orbit, speed, now, dt);
                    if (Mode == FlyerMode.Remontee && Vector3.Distance(pos, orbit) < 3f)
                        SetMode(FlyerMode.Ronde);
                    Vector3 chest = _target.Center;
                    if (Mode == FlyerMode.Ronde && now >= _nextDive && Vector3.Distance(pos, chest) <= settings.diveRange && Clear(pos, chest))
                    {
                        SetMode(FlyerMode.Annonce);
                        var art = ItHunts.Audio.GameAudio.Settings;      // cri de la DA (PR #20), sinon provisoire
                        ItHunts.Audio.GameAudio.PlayOn(_audio, art != null ? art.flyerCry : null, _screech, art != null && art.flyerCry != null ? art.flyerVolume : 0.8f);
                    }

                    break;
                }

                case FlyerMode.Annonce:
                    _velocity = Vector3.MoveTowards(_velocity, Vector3.zero, settings.acceleration * 2f * dt);
                    Move(pos, dt);
                    if (now - _modeSince >= settings.diveTelegraph)
                    {
                        // Vers l'endroit où sera le joueur à l'arrivée.
                        Vector3 chest0 = _target.Center;
                        float time = Vector3.Distance(pos, chest0) / Mathf.Max(1f, settings.diveSpeed) * settings.diveLead;
                        Vector3 aim = EnemyMath.Predict(chest0, EnemySenses.VelocityOf(_target), time);
                        _velocity = (aim - pos).normalized * settings.diveSpeed;
                        Dives++;
                        SetMode(FlyerMode.Pique);
                    }

                    break;

                case FlyerMode.Pique:
                {
                    // Visée corrigée pendant le piqué, virage limité (diveTurnRate) : un dash tôt est rattrapé, un dash tard non.
                    Vector3 chest1 = _target.Center;
                    Vector3 toTarget = chest1 - pos;
                    bool passed = Vector3.Dot(toTarget, _velocity) < 0f;
                    if (settings.diveTurnRate > 0f && !passed)
                    {
                        float time = toTarget.magnitude / Mathf.Max(1f, settings.diveSpeed) * settings.diveLead;
                        Vector3 aim = EnemyMath.Predict(chest1, EnemySenses.VelocityOf(_target), time) - pos;
                        _velocity = EnemyMath.TurnTowards(_velocity, aim, settings.diveTurnRate * dt, settings.diveSpeed);
                    }

                    bool blocked = Move(pos, dt);
                    if (TouchPlayer(now))
                        EndDive(now);
                    else if (blocked || passed || now - _modeSince > settings.diveMaxTime)
                        EndDive(now);
                    break;
                }
            }

            UpdateVisual(now);
        }

        private void EndDive(float now)
        {
            _nextDive = now + settings.diveCooldown;
            _velocity *= 0.3f;
            SetMode(FlyerMode.Remontee);
        }

        private bool TouchPlayer(float now)
        {
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead || p.Collider == null)
                    continue;
                Vector3 c = _rb.position;
                Vector3 onPlayer = p.Collider.ClosestPoint(c);
                if (Vector3.Distance(onPlayer, c) > settings.radius + settings.contactRange)
                    continue;
                DiveHits++;
                p.TakeEnemyDamage(settings.diveDamage, onPlayer, now, "Piqué d'un volant");
                Vector3 flat = new Vector3(_velocity.x, 0f, _velocity.z);
                if (p.Motor != null)
                    p.Motor.AddVelocity((flat.sqrMagnitude > 0.01f ? flat.normalized : Vector3.zero) * settings.divePush);
                return true;
            }

            return false;
        }

        // ---------- Vol ----------

        /// <summary>Point de la ronde : au-dessus du joueur, sous le plafond, jamais dans un mur.</summary>
        private Vector3 OrbitPoint(PlayerHealth target)
        {
            Vector3 center = target.transform.position + Vector3.up * 1.2f;
            float height = settings.orbitHeight;
            if (Physics.Raycast(center, Vector3.up, out RaycastHit ceil, height + 2f, ~0, QueryTriggerInteraction.Ignore) && IsStatic(ceil.collider))
                height = Mathf.Max(1.5f, ceil.distance - 1.5f);
            Vector3 top = center + Vector3.up * height;
            Vector3 dir = new Vector3(Mathf.Cos(_orbitAngle), 0f, Mathf.Sin(_orbitAngle));
            float radius = settings.orbitRadius;
            if (Physics.SphereCast(top, settings.radius, dir, out RaycastHit wall, radius, ~0, QueryTriggerInteraction.Ignore) && IsStatic(wall.collider))
                radius = Mathf.Max(0f, wall.distance - 0.5f);
            return top + dir * radius;
        }

        /// <summary>
        /// Vers un point : en ligne droite si rien ne le cache, sinon par le chemin des goules (NavMesh), à hauteur de vol sous
        /// les plafonds.
        /// </summary>
        private void Steer(Vector3 pos, Vector3 goal, float speed, float now, float dt)
        {
            Vector3 aim = goal;
            if (!ClearSphere(pos, goal))
            {
                Vector3 from = Ground(pos), to = Ground(goal);
                _path.SetDestination(from, to, now, 0.5f);
                if (_path.HasPath)
                {
                    Vector3 c = _path.CurrentCorner;
                    Vector3 flat = new Vector3(c.x - pos.x, 0f, c.z - pos.z);
                    if (flat.magnitude < 2f)
                    {
                        _path.Advance();
                        c = _path.CurrentCorner;
                    }

                    float h = settings.orbitHeight * 0.6f;
                    if (Physics.Raycast(c + Vector3.up, Vector3.up, out RaycastHit ceil, h + 2f, ~0, QueryTriggerInteraction.Ignore) && IsStatic(ceil.collider))
                        h = Mathf.Max(1.2f, ceil.distance - 0.5f);
                    aim = c + Vector3.up * h;
                }
            }

            Vector3 desired = aim - pos;
            float distance = desired.magnitude;
            Vector3 wanted = distance > 0.05f ? desired / distance * Mathf.Min(speed, distance / Mathf.Max(dt, 0.02f) * 0.5f) : Vector3.zero;
            _velocity = Vector3.MoveTowards(_velocity, wanted, settings.acceleration * dt);
            Move(pos, dt);
        }

        /// <summary>Déplace le corps de _velocity × dt en glissant contre le décor ; vrai s'il a heurté quelque chose.</summary>
        private bool Move(Vector3 pos, float dt)
        {
            Vector3 move = _velocity * dt;
            float distance = move.magnitude;
            bool hit = false;
            if (distance > 1e-5f)
            {
                Vector3 dir = move / distance;
                int n = Physics.SphereCastNonAlloc(pos, settings.radius, dir, _hits, distance + 0.05f, ~0, QueryTriggerInteraction.Ignore);
                float nearest = float.PositiveInfinity;
                Vector3 normal = Vector3.zero;
                for (int i = 0; i < n; i++)
                {
                    Collider c = _hits[i].collider;
                    if (c.transform.IsChildOf(transform) || c.GetComponentInParent<PlayerHealth>() != null || _hits[i].distance <= 0f)
                        continue;
                    if (_hits[i].distance < nearest)
                    {
                        nearest = _hits[i].distance;
                        normal = _hits[i].normal;
                    }
                }

                if (!float.IsPositiveInfinity(nearest))
                {
                    hit = true;
                    Vector3 allowed = dir * Mathf.Max(0f, nearest - 0.05f);
                    Vector3 slide = Vector3.ProjectOnPlane(move - allowed, normal);
                    move = allowed + slide;
                    _velocity = Vector3.ProjectOnPlane(_velocity, normal);
                }
            }

            Vector3 next = pos + move;
            _rb.MovePosition(next);
            transform.position = next;
            Vector3 flatVel = new Vector3(_velocity.x, 0f, _velocity.z);
            if (flatVel.sqrMagnitude > 0.05f)
            {
                Quaternion r = Quaternion.LookRotation(flatVel.normalized, Vector3.up);
                _rb.MoveRotation(r);
                transform.rotation = r;
            }

            return hit;
        }

        private Vector3 Ground(Vector3 p)
        {
            int n = Physics.RaycastNonAlloc(p, Vector3.down, _hits, 40f, ~0, QueryTriggerInteraction.Ignore);
            float best = float.PositiveInfinity;
            Vector3 g = p + Vector3.down * 8f;
            for (int i = 0; i < n; i++)
                if (IsStatic(_hits[i].collider) && _hits[i].distance < best && _hits[i].normal.y > 0.6f)
                {
                    best = _hits[i].distance;
                    g = _hits[i].point;
                }

            return g;
        }

        private static bool IsStatic(Collider c) =>
            c != null && !c.isTrigger && c.attachedRigidbody == null && c.GetComponentInParent<Damageable>() == null
            && c.GetComponentInParent<PlayerHealth>() == null && c.GetComponentInParent<Grabbable>() == null;

        private bool Clear(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.1f)
                return true;
            int n = Physics.RaycastNonAlloc(a, d / len, _hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (IsStatic(_hits[i].collider))
                    return false;
            return true;
        }

        private bool ClearSphere(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.1f)
                return true;
            int n = Physics.SphereCastNonAlloc(a, settings.radius, d / len, _hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (IsStatic(_hits[i].collider) && _hits[i].distance > 0f)
                    return false;
            return true;
        }

        // ---------- Cible, plafond, aggro (D-040, comme les goules, au plus court à vol d'oiseau) ----------

        /// <summary>Coop (D-041) : comme les goules (TargetKeeper) ; longueur par le chemin au sol, à défaut à vol d'oiseau.</summary>
        private void ChooseTarget(Vector3 pos, float now)
        {
            PlayerHealth chased = _hunt.ChaserFor;
            if (chased != null && !chased.Dead)
            {
                _target = chased;                                        // chasseur (D-061) : son joueur
                return;
            }

            Vector3 ground = Ground(pos);
            _target = _keeper.Update(now, _damageable, settings, 1f,
                p => _path.Probe(ground, p.transform.position, out float l, out _) ? l : Vector3.Distance(pos, p.transform.position));
        }

        public int TargetSwitches => _keeper.Switches;

        public static int HuntingCount
        {
            get
            {
                int n = 0;
                foreach (Flyer f in All)
                    if (f != null && f.Hunting && !f.Dead)
                        n++;
                return n;
            }
        }

        /// <summary>Aggro (D-061) : comme les goules, sans limite de nombre ; semé au-delà de aggroLoseDistance pendant aggroLoseTime.</summary>
        private void Aggro(Vector3 pos, float now)
        {
            if (now < _nextAggroCheck)
                return;
            _nextAggroCheck = now + 0.2f;
            float nearest = float.PositiveInfinity;
            bool seen = false;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                float d = Vector3.Distance(pos, p.transform.position);
                nearest = Mathf.Min(nearest, d);
                if (d <= settings.aggroProximity || (d <= settings.aggroSightRange && Clear(pos, p.Center)))
                    seen = true;
            }

            bool was = Hunting;
            _hunt.Step(seen, nearest, now, settings.aggroLoseDistance, settings.aggroLoseTime);
            if (was && !Hunting)
            {
                _target = null;
                _keeper.Clear();
            }
        }

        // ---------- Lancé : il éclate (D-040) ----------

        private void OnCollisionEnter(Collision collision)
        {
            if (Mode != FlyerMode.Lance || _burst || settings == null || Dead)
                return;
            if (collision.relativeVelocity.magnitude < settings.burstMinSpeed)
                return;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            BurstAt(point, collision.relativeVelocity.magnitude, Time.time);
        }

        /// <summary>
        /// Éclatement : zone de burstRadius autour du point (burstDamage au centre, × burstEdgeShare au bord, jamais à travers
        /// un mur), sur les ennemis et, au nom du joueur qui l'a lancé, sur les autres joueurs si le tir ami le permet ; puis il
        /// meurt. Les dégâts d'impact habituels (ImpactDamager) s'ajoutent.
        /// </summary>
        public float BurstAt(Vector3 point, float speed, float time)
        {
            _burst = true;
            float r = settings.burstRadius, total = 0f;
            AreaEvents.RaiseArea(new AreaImpactEvent { Point = point, Radius = r, ImpactDamage = settings.burstDamage });
            AreaEvents.RaiseBroken(new BreakEvent { Point = point, Size = Vector3.one * 0.6f, Color = new Color(0.25f, 0.2f, 0.18f) });
            if (ItHunts.Combat.GameVfx.Settings != null)
                ItHunts.Combat.GameVfx.Spawn(ItHunts.Combat.GameVfx.Settings.flyerDeath, point, Quaternion.identity, 1f, null, 3f);
            var deathArt = ItHunts.Audio.GameAudio.Settings;
            if (deathArt != null && deathArt.flyerDeath != null)
                ItHunts.Audio.GameAudio.PlayAt(deathArt.flyerDeath, point, deathArt.flyerVolume);
            else if (_pop != null)
                AudioSource.PlayClipAtPoint(_pop, point, 0.9f);
            int sourceId = -GetInstanceID() - 7;
            var targets = new List<Damageable>(Damageable.All);
            foreach (Damageable d in targets)
            {
                if (d == null || d == _damageable || d.Dead || d.ImmuneToOrbs)
                    continue;
                Collider col = d.MainCollider;
                Vector3 closest = col != null ? col.ClosestPoint(point) : d.transform.position;
                float damage = EnemyMath.BurstDamage(Vector3.Distance(point, closest), r, settings.burstDamage, settings.burstEdgeShare);
                if (damage <= 0f || !ImpactDamager.InSight(_grab, d.transform, d.Center, point))
                    continue;
                float before = d.Health.Current;
                if (_grab != null)
                    d.NoteAttacker(_grab.LastOwnerId);                   // âmes (D-043) : son lanceur y est pour quelque chose
                ImpactDamager.ApplyDirect(d, sourceId, damage, speed, settings.mass, closest, time, lethal: false);   // zone
                total += before - d.Health.Current;
            }

            int owner = _grab != null ? _grab.LastOwnerId : 0;
            if (owner != 0)
                foreach (PlayerHealth p in new List<PlayerHealth>(PlayerHealth.All))
                {
                    if (p == null || p.Dead || !FriendlyFire.Allows(owner, p.PlayerId, p.Coop))
                        continue;
                    Vector3 closest = p.Collider != null ? p.Collider.ClosestPoint(point) : p.Center;
                    float damage = EnemyMath.BurstDamage(Vector3.Distance(point, closest), r, settings.burstDamage, settings.burstEdgeShare);
                    if (damage > 0f && ImpactDamager.InSight(_grab, p.transform, p.Center, point))
                        p.TryHurt(owner, damage, closest, time);
                }

            LastBurstTotal = total;
            LastBurstPoint = point;
            if (_damageable != null)
                _damageable.ApplyDamage(_damageable.Health.Current + 1f, time, out _);   // il meurt
            return total;
        }

        // ---------- Visuel ----------

        private Renderer[] _eyes;
        private MaterialPropertyBlock _eyeBlock;
        private bool _eyesLit;

        /// <summary>Yeux rouges pendant l'annonce du piqué seulement (correctifs-rendu point 5) ; sombres au repos.</summary>
        private void EyeGlow(bool on)
        {
            if (_eyes == null)
            {
                _eyes = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(GetComponentsInChildren<Renderer>(true), r => r.name.StartsWith("Oeil")));
                _eyeBlock = new MaterialPropertyBlock();
                _eyesLit = !on;
            }

            if (on == _eyesLit)
                return;
            _eyesLit = on;
            foreach (Renderer r in _eyes)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(_eyeBlock);
                if (on && settings != null)
                    _eyeBlock.SetColor("_BaseColor", settings.diveGlowColor);
                else
                    _eyeBlock.Clear();
                r.SetPropertyBlock(_eyeBlock);
            }
        }

        /// <summary>Vitrine (Test_Vitrine) : affiche un état sans l'IA (composant désactivé). Affichage seulement.</summary>
        public void ShowPose(FlyerMode mode)
        {
            Mode = mode;
            UpdateVisual(Time.time);
        }

        private void UpdateVisual(float now)
        {
            if (telegraph != null)
                telegraph.enabled = Mode == FlyerMode.Annonce && !Dead;
            EyeGlow(Mode == FlyerMode.Annonce && !Dead);
            float flap = Mode == FlyerMode.AuSol ? 0f : Mode == FlyerMode.Tenu ? 18f : Mode == FlyerMode.Pique ? 4f : 10f;
            float angle = Mathf.Sin(now * flap * Mathf.PI * 2f) * (Mode == FlyerMode.Pique ? 8f : 35f);
            if (wingLeft != null)
                wingLeft.localRotation = Quaternion.Euler(0f, 0f, angle + (Mode == FlyerMode.Pique ? 40f : 0f));
            if (wingRight != null)
                wingRight.localRotation = Quaternion.Euler(0f, 0f, -angle - (Mode == FlyerMode.Pique ? 40f : 0f));
            if (visual != null)
                visual.localRotation = Mode == FlyerMode.Pique ? Quaternion.Euler(35f, 0f, 0f)
                    : Mode == FlyerMode.AuSol ? Quaternion.Euler(0f, 0f, 70f) : Quaternion.identity;
        }
    }
}
