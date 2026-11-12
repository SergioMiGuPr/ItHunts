using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    public enum GhoulMode : byte
    {
        Poste,      // aucun joueur en vie : elle attend
        Chasse,
        Annonce,    // posture visible avant le bond
        Bond,
        Retour,     // inutilisé depuis D-035 (elles ne lâchent plus), gardé pour les vagues
        Mur,        // elle grimpe ou court sur un mur (ClimbLink), ou en redescend
    }

    /// <summary>
    /// Goule (D-033, D-035) : elle sait toujours où sont les joueurs et court vers le plus proche par le chemin (même
    /// derrière elle), à 10 m/s ; loin de lui, elle vise un point décalé autour de lui (encerclement) au lieu de faire la
    /// queue. Elle grimpe aux murs (liens ClimbLink du NavMesh) pour rejoindre ponts, corniches et salles en hauteur, et
    /// en redescend d'un saut. À portée de bond (au sol, d'un mur, d'une corniche ou d'un pont), elle s'arrête en posture
    /// annoncée (0.3 s, lueur rouge, sifflement), puis bondit vers l'endroit où il sera ; esquivable. Un bond qui touche
    /// fait des dégâts et pousse légèrement, sans jamais ralentir. Au contact, 15 PV au plus une fois par seconde.
    /// Repoussée, étourdie, tenue ou lancée, son corps (EnemyBody) prend la main.
    /// </summary>
    [RequireComponent(typeof(EnemyBody))]
    public sealed class Ghoul : MonoBehaviour
    {
        [SerializeField] private GhoulSettings settings;
        [Tooltip("Partie visible qui s'accroupit pendant l'annonce du bond.")]
        [SerializeField] private Transform visual;
        [Tooltip("Repère de l'annonce du bond (lueur rouge), caché sinon.")]
        [SerializeField] private Renderer telegraph;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Ghoul");
        private static int _count;
        private EnemyBody _body;
        private Damageable _damageable;
        private Rigidbody _rb;
        private CapsuleCollider _capsule;
        private ZoneNavMesh _nav;
        private readonly EnemyLocomotion _move = new EnemyLocomotion();
        private Vector3 _home;
        private Quaternion _homeRotation;
        private PlayerHealth _target;
        private float _retargetAt;
        private float _modeSince;
        private float _nextLeap;
        private float _nextContact;
        private Vector3 _leapVelocity;
        private bool _leapHit;
        private bool _wasBusy;
        private Vector3 _climbFrom, _climbTo;
        private Vector3 _encircle;
        private AudioSource _audio;
        private AudioClip _hiss;
        private readonly RaycastHit[] _hits = new RaycastHit[8];

        public GhoulMode Mode { get; private set; }
        public GhoulSettings Settings => settings;
        public PlayerHealth Target => _target;
        public float LeapCooldownRemaining => Mathf.Max(0f, _nextLeap - Time.time);
        public float Speed => Mode == GhoulMode.Bond ? new Vector3(_leapVelocity.x, 0f, _leapVelocity.z).magnitude : _move.Speed;
        public int LeapCount { get; private set; }
        public int LeapHits { get; private set; }
        public int WallLeaps { get; private set; }
        public int Climbs { get; private set; }
        public Vector3 LastLeapStart { get; private set; }
        public Vector3 LastLeapEnd { get; private set; }
        public EnemyLocomotion Locomotion => _move;

        /// <summary>Goules actives (debug, mesures).</summary>
        public static readonly System.Collections.Generic.List<Ghoul> All = new System.Collections.Generic.List<Ghoul>();

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void Awake()
        {
            _body = GetComponent<EnemyBody>();
            _damageable = GetComponent<Damageable>();
            _rb = GetComponent<Rigidbody>();
            _capsule = GetComponent<CapsuleCollider>();
            _home = transform.position;
            _homeRotation = transform.rotation;
            _move.Heading = transform.forward;
            // Décalage d'encerclement propre à chaque goule (angle d'or : réparties tout autour du joueur).
            float angle = (_count++ * 137.508f) % 360f;
            _encircle = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.spatialBlend = 1f;
            _audio.maxDistance = 30f;
            _audio.rolloffMode = AudioRolloffMode.Linear;
            _audio.playOnAwake = false;
            _hiss = ItHunts.Audio.ProceduralSfx.ToClip("Goule : sifflement", ItHunts.Audio.ProceduralSfx.GhoulHiss());
            if (telegraph != null)
                telegraph.enabled = false;
        }

        private void Start()
        {
            _nav = FindFirstObjectByType<ZoneNavMesh>();
            if (_nav != null)
                _move.AgentType = _nav.SmallAgent;
        }

        /// <summary>Remise à zéro de la zone : à son poste, sans cible.</summary>
        public void ResetState()
        {
            transform.SetPositionAndRotation(_home, _homeRotation);
            if (_rb != null)
                _rb.position = _home;
            _move.Clear();
            _move.Heading = _homeRotation * Vector3.forward;
            _target = null;
            Hunting = false;
            Aggroed = false;
            _farSince = float.NegativeInfinity;
            _retargetAt = 0f;
            _nextLeap = 0f;
            _nextContact = 0f;
            SetMode(GhoulMode.Poste);
        }

        private void SetMode(GhoulMode m)
        {
            Mode = m;
            _modeSince = Time.time;
        }

        private float Radius => _capsule != null ? _capsule.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z) : 0.45f;
        private float Height => _capsule != null ? _capsule.height * transform.lossyScale.y : 1.1f;

        private void FixedUpdate()
        {
            if (settings == null || (_damageable != null && _damageable.Dead))
                return;
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            float now = Time.time, dt = Time.fixedDeltaTime;
            if (_body.Busy)
            {
                // Repoussée, étourdie, tenue ou lancée : son corps la déplace ; elle reprendra la chasse après.
                if (Mode != GhoulMode.Chasse && Mode != GhoulMode.Poste)
                    SetMode(GhoulMode.Chasse);
                _wasBusy = true;
                _move.Speed = 0f;
                UpdateVisual();
                return;
            }

            if (_wasBusy)
            {
                _wasBusy = false;
                _move.Clear();
            }

            Vector3 pos = _rb.position;
            Pack(now);
            Aggro(pos, now);
            if (!Hunting)
            {
                // En attente : elle prendra le relais quand une goule en chasse mourra (D-037).
                if (Mode != GhoulMode.Poste)
                    SetMode(GhoulMode.Poste);
                _move.Speed = 0f;
                ContactDamage(now);
                UpdateVisual();
                return;
            }

            ChooseTarget(pos, now);

            switch (Mode)
            {
                case GhoulMode.Poste:
                case GhoulMode.Retour:
                    if (_target != null)
                        SetMode(GhoulMode.Chasse);
                    break;

                case GhoulMode.Chasse:
                    Chase(pos, now, dt);
                    break;

                case GhoulMode.Mur:
                    ClimbStep(pos, now, dt);
                    break;

                case GhoulMode.Annonce:
                    Face(_target != null ? _target.transform.position : pos + _move.Heading, dt);
                    if (now - _modeSince >= settings.leapTelegraph)
                        StartLeap(pos);
                    break;

                case GhoulMode.Bond:
                    LeapStep(pos, now, dt);
                    break;
            }

            ContactDamage(now);
            UpdateVisual();
        }

        /// <summary>En chasse (une des maxHunting goules autorisées, ou réveillée par l'aggro), sinon en attente.</summary>
        public bool Hunting { get; private set; }
        /// <summary>Réveillée par l'aggro (D-038), en plus des maxHunting.</summary>
        public bool Aggroed { get; private set; }
        public int AggroWakes { get; private set; }
        private float _farSince = float.NegativeInfinity;
        private float _nextAggroCheck;

        /// <summary>Goules en chasse grâce au plafond (sans l'aggro).</summary>
        public static int PackCount
        {
            get
            {
                int n = 0;
                foreach (Ghoul g in All)
                    if (g != null && g.Hunting && !g.Aggroed && (g._damageable == null || !g._damageable.Dead))
                        n++;
                return n;
            }
        }

        /// <summary>
        /// Aggro (D-038) : en attente, elle se réveille si un joueur est visible à moins de aggroSightRange ou à moins de
        /// aggroProximity (sans ligne de vue), tant que le total en chasse reste sous maxHuntingTotal. Réveillée, elle se rendort
        /// si le joueur le plus proche reste à plus de aggroLoseDistance pendant aggroLoseTime, sauf si une place du plafond
        /// est libre (elle la prend).
        /// </summary>
        private void Aggro(Vector3 pos, float now)
        {
            if (now < _nextAggroCheck)
                return;
            _nextAggroCheck = now + 0.2f;
            float nearest = float.PositiveInfinity;
            PlayerHealth seen = null;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                float d = Vector3.Distance(pos, p.transform.position);
                nearest = Mathf.Min(nearest, d);
                if (seen == null && (d <= settings.aggroProximity
                                     || (d <= settings.aggroSightRange && LineOfSight(pos + Vector3.up * Height * 0.8f, p))))
                    seen = p;
            }

            if (!Hunting)
            {
                if (seen != null && HuntingCount < settings.maxHuntingTotal)
                {
                    Hunting = true;
                    Aggroed = true;
                    AggroWakes++;
                    _farSince = float.NegativeInfinity;
                    _retargetAt = 0f;
                }

                return;
            }

            if (!Aggroed)
                return;
            if (nearest <= settings.aggroLoseDistance)
            {
                _farSince = float.NegativeInfinity;
                return;
            }

            if (float.IsNegativeInfinity(_farSince))
                _farSince = now;
            if (now - _farSince < settings.aggroLoseTime)
                return;
            if (PackCount < settings.maxHunting)
                Aggroed = false;                                    // une place du plafond est libre : elle la prend
            else
            {
                Hunting = false;                                    // retour en attente, là où elle est
                Aggroed = false;
                _target = null;
                _move.Clear();
            }

            _farSince = float.NegativeInfinity;
        }

        private static float _packNext;
        private static readonly System.Collections.Generic.List<(Ghoul g, float d)> PackCandidates = new System.Collections.Generic.List<(Ghoul, float)>();

        /// <summary>Goules en chasse en ce moment (vivantes).</summary>
        public static int HuntingCount
        {
            get
            {
                int n = 0;
                foreach (Ghoul g in All)
                    if (g != null && g.Hunting && (g._damageable == null || !g._damageable.Dead))
                        n++;
                return n;
            }
        }

        /// <summary>
        /// Plafond des goules en chasse (D-037), revu toutes les 0.5 s par une seule goule : s'il manque des chasseuses, les
        /// goules en attente les plus proches d'un joueur par le chemin prennent le relais (chemin calculé pour chacune).
        /// </summary>
        private void Pack(float now)
        {
            if (_packNext - now > 1f)
                _packNext = 0f;                                      // nouvelle partie (le temps est reparti de zéro)
            if (now < _packNext)
                return;
            _packNext = now + 0.5f;
            int free = settings.maxHunting - PackCount;     // une goule réveillée par l'aggro peut prendre une place du plafond
            if (free <= 0)
                return;
            PackCandidates.Clear();
            foreach (Ghoul g in All)
            {
                if (g == null || (g.Hunting && !g.Aggroed) || !g.isActiveAndEnabled || (g._damageable != null && g._damageable.Dead))
                    continue;
                float d = float.PositiveInfinity;
                foreach (PlayerHealth p in PlayerHealth.All)
                    if (p != null && !p.Dead)
                        d = Mathf.Min(d, Vector3.Distance(g._rb.position, p.transform.position));
                if (!float.IsPositiveInfinity(d))
                    PackCandidates.Add((g, d));
            }

            if (PackCandidates.Count == 0)
                return;
            // Par le chemin, pour toutes les goules en attente (rare : seulement quand une place se libère).
            int keep = PackCandidates.Count;
            for (int i = 0; i < keep; i++)
            {
                Ghoul g = PackCandidates[i].g;
                float best = float.PositiveInfinity;
                foreach (PlayerHealth p in PlayerHealth.All)
                {
                    if (p == null || p.Dead)
                        continue;
                    float len = g._move.Probe(g._rb.position, p.transform.position, out float l, out _) ? l : PackCandidates[i].d * 3f;
                    best = Mathf.Min(best, len);
                }

                PackCandidates[i] = (g, best);
            }

            PackCandidates.RemoveRange(keep, PackCandidates.Count - keep);
            PackCandidates.Sort((a, b) => a.d.CompareTo(b.d));
            for (int i = 0; i < Mathf.Min(free, PackCandidates.Count); i++)
            {
                PackCandidates[i].g.Hunting = true;
                PackCandidates[i].g.Aggroed = false;
            }
        }

        /// <summary>Elle sait toujours où sont les joueurs : le plus proche par le chemin, revu toutes les targetInterval s.</summary>
        private void ChooseTarget(Vector3 pos, float now)
        {
            if (_target != null && !_target.Dead && now < _retargetAt)
                return;
            _retargetAt = now + settings.targetInterval;
            PlayerHealth best = null;
            float bestLen = float.PositiveInfinity;
            int alive = 0;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead)
                {
                    alive++;
                    best = p;
                }

            if (alive > 1)
            {
                best = null;
                foreach (PlayerHealth p in PlayerHealth.All)
                {
                    if (p == null || p.Dead)
                        continue;
                    float len = _move.Probe(pos, p.transform.position, out float l, out _) ? l : Vector3.Distance(pos, p.transform.position) * 3f;
                    if (len < bestLen)
                    {
                        bestLen = len;
                        best = p;
                    }
                }
            }

            _target = best;
            if (_target == null && Mode == GhoulMode.Chasse)
                SetMode(GhoulMode.Poste);
        }

        private void Chase(Vector3 pos, float now, float dt)
        {
            if (_target == null)
                return;
            Vector3 targetPos = _target.transform.position;
            if (TryStartLeap(pos, targetPos, now))
                return;

            // Encerclement : loin du joueur, elle vise un point décalé autour de lui (chacune le sien).
            float distance = new Vector3(targetPos.x - pos.x, 0f, targetPos.z - pos.z).magnitude;
            Vector3 goal = targetPos;
            if (distance > settings.encircleDirectDistance && settings.encircleRadius > 0f
                && _move.Snap(targetPos + _encircle * settings.encircleRadius, 2f, out Vector3 around)
                && Mathf.Abs(around.y - targetPos.y) < 2f)
                goal = around;
            _move.SetDestination(pos, goal, now, settings.repathInterval);

            // Mur sur le chemin (lien ClimbLink) : coin suivant bien plus haut ou plus bas, tout près à l'horizontale.
            if (_move.HasPath)
            {
                Vector3 c = _move.CurrentCorner;
                float dy = c.y - pos.y;
                float flat = new Vector2(c.x - pos.x, c.z - pos.z).magnitude;
                if (Mathf.Abs(dy) > 1.5f && flat < 4f)
                {
                    _climbFrom = pos;
                    _climbTo = c;
                    Climbs++;
                    SetMode(GhoulMode.Mur);
                    return;
                }
            }

            MoveTo(_move.Step(pos, settings.runSpeed, settings.acceleration, settings.turnRate, dt));
        }

        /// <summary>Le long du mur (montée à climbSpeed, descente à dropSpeed) ; d'un mur, elle peut bondir sur le joueur.</summary>
        private void ClimbStep(Vector3 pos, float now, float dt)
        {
            if (_target != null && TryStartLeap(pos, _target.transform.position, now))
                return;
            Vector3 to = _climbTo - pos;
            float speed = _climbTo.y >= _climbFrom.y ? settings.climbSpeed : settings.dropSpeed;
            float step = speed * dt;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            if (flat.sqrMagnitude > 0.01f)
                _move.Heading = flat.normalized;
            if (to.magnitude <= step || now - _modeSince > 6f)
            {
                MoveTo(_climbTo);
                _move.Advance();
                _move.Speed = 0f;
                SetMode(GhoulMode.Chasse);
                return;
            }

            _move.Speed = speed;
            MoveTo(pos + to.normalized * step);
        }

        /// <summary>
        /// Bond annoncé : joueur en ligne de vue, au sol entre leapMinRange et leapMaxRange, ou plus bas qu'elle (mur,
        /// corniche, pont) à moins de leapMaxRange à l'horizontale (plus loin s'il est plus bas).
        /// </summary>
        private bool TryStartLeap(Vector3 pos, Vector3 targetPos, float now)
        {
            if (now < _nextLeap)
                return false;
            float distance = new Vector3(targetPos.x - pos.x, 0f, targetPos.z - pos.z).magnitude;
            float drop = pos.y - targetPos.y;
            bool fromHigh = drop > 1.5f && drop < 16f && distance <= settings.leapMaxRange + drop * 0.5f;
            bool ground = Mathf.Abs(drop) <= 1.5f && distance >= settings.leapMinRange && distance <= settings.leapMaxRange;
            if (!(fromHigh || ground) || !LineOfSight(pos + Vector3.up * Height * 0.8f, _target))
                return false;
            _move.Speed = 0f;
            SetMode(GhoulMode.Annonce);
            if (fromHigh)
                WallLeaps++;
            if (_hiss != null)
                _audio.PlayOneShot(_hiss, 0.8f);
            return true;
        }

        private bool LineOfSight(Vector3 eye, PlayerHealth p)
        {
            Vector3 to = p.Center - eye;
            float len = to.magnitude;
            if (len < 0.1f)
                return true;
            int n = Physics.RaycastNonAlloc(eye, to / len, _hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _hits[i].collider;
                if (c.transform.IsChildOf(transform) || c.GetComponentInParent<PlayerHealth>() != null
                    || c.GetComponentInParent<Damageable>() != null || c.GetComponentInParent<ItHunts.Orbs.Grabbable>() != null)
                    continue;
                return false;
            }

            return true;
        }

        private void StartLeap(Vector3 pos)
        {
            if (_target == null)
            {
                SetMode(GhoulMode.Chasse);
                return;
            }

            Vector3 aim = EnemyMath.Predict(_target.transform.position, EnemySenses.VelocityOf(_target), settings.leapLead);
            _leapVelocity = EnemyMath.LeapVelocityTo(pos, aim, settings.leapSpeed, settings.leapHeight, settings.gravity);
            _leapHit = false;
            LeapCount++;
            LastLeapStart = pos;
            _move.Clear();
            SetMode(GhoulMode.Bond);
        }

        private void LeapStep(Vector3 pos, float now, float dt)
        {
            _leapVelocity.y -= settings.gravity * dt;
            Vector3 move = _leapVelocity * dt;
            Vector3 next = pos + move;
            float distance = move.magnitude;
            if (distance > 1e-5f && _rb.SweepTest(move / distance, out RaycastHit hit, distance + 0.03f, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PlayerHealth>() == null)
            {
                next = pos + move / distance * Mathf.Max(0f, hit.distance - 0.03f);
                Vector3 n = hit.normal;
                float into = Vector3.Dot(_leapVelocity, n);
                if (into < 0f)
                    _leapVelocity -= n * into;                        // glisse le long du mur, sans s'étourdir
            }

            // Retombée : sol sous elle et vitesse vers le bas.
            if (_leapVelocity.y <= 0f && _move.Snap(next, 0.25f, out Vector3 ground) && next.y <= ground.y + 0.05f)
            {
                next = ground;
                LastLeapEnd = next;
                _nextLeap = now + settings.leapCooldown;
                _move.Speed = 0f;
                _move.Clear();
                SetMode(GhoulMode.Chasse);
            }
            else if (now - _modeSince > 3f)
            {
                if (_move.Snap(next, 20f, out Vector3 safe))
                    next = safe;                                      // sécurité : jamais coincée en l'air
                SetMode(GhoulMode.Chasse);
                _nextLeap = now + settings.leapCooldown;
            }

            MoveTo(next);
            Vector3 flat = new Vector3(_leapVelocity.x, 0f, _leapVelocity.z);
            if (flat.sqrMagnitude > 0.01f)
                _move.Heading = flat.normalized;

            // Touché en vol : dégâts du bond et petite poussée, une fois par bond.
            if (!_leapHit)
                foreach (PlayerHealth p in PlayerHealth.All)
                {
                    if (p == null || p.Dead || Gap(p) > settings.contactRange)
                        continue;
                    _leapHit = true;
                    LeapHits++;
                    if (p.TakeEnemyDamage(settings.leapDamage, p.Center, now))
                        _nextContact = now + settings.contactInterval;
                    Vector3 push = flat.sqrMagnitude > 0.01f ? flat.normalized * settings.leapPush : Vector3.zero;
                    if (p.Motor != null)
                        p.Motor.AddVelocity(push + Vector3.up * settings.leapPush * 0.25f);
                    break;
                }
        }

        private void ContactDamage(float now)
        {
            if (now < _nextContact)
                return;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead || Gap(p) > settings.contactRange)
                    continue;
                if (p.TakeEnemyDamage(settings.contactDamage, p.Center, now))
                    _nextContact = now + settings.contactInterval;
                break;
            }
        }

        private float Gap(PlayerHealth p)
        {
            EnemySenses.Capsule(p, out float r, out float h);
            return EnemyMath.CapsuleGap(_rb.position, Radius, Height, p.transform.position, r, h);
        }

        private void Face(Vector3 point, float dt)
        {
            Vector3 to = point - _rb.position;
            to.y = 0f;
            if (to.sqrMagnitude > 1e-4f)
                _move.Heading = Vector3.RotateTowards(_move.Heading, to.normalized, settings.turnRate * Mathf.Deg2Rad * dt, 0f).normalized;
            MoveTo(_rb.position);
        }

        private void MoveTo(Vector3 position)
        {
            _rb.MovePosition(position);
            transform.position = position;
            if (_move.Heading.sqrMagnitude > 1e-4f)
            {
                Quaternion r = Quaternion.LookRotation(_move.Heading, Vector3.up);
                _rb.MoveRotation(r);
                transform.rotation = r;
            }
        }

        private void UpdateVisual()
        {
            bool announcing = Mode == GhoulMode.Annonce;
            if (telegraph != null)
                telegraph.enabled = announcing && (_damageable == null || !_damageable.Dead);
            if (visual != null)
            {
                float crouch = announcing ? Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((Time.time - _modeSince) / Mathf.Max(0.01f, settings.leapTelegraph)))
                    : Mode == GhoulMode.Bond ? 1.15f : 1f;
                visual.localScale = new Vector3(1f / Mathf.Sqrt(crouch), crouch, 1f / Mathf.Sqrt(crouch));
                // Sur un mur : le corps se plaque contre la paroi (tête vers le haut en montant, vers le bas en descendant).
                bool wall = Mode == GhoulMode.Mur;
                float pitch = wall ? (_climbTo.y >= _climbFrom.y ? -80f : 80f) : 0f;
                visual.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
    }
}
