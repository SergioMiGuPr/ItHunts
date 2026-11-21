using System.Linq;
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
    public sealed class Ghoul : MonoBehaviour, IHunter
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
        // Escalade vers un joueur hors d'atteinte (plateforme) : pied du mur, haut du mur, accrochée depuis.
        private bool _assault;
        private Vector3 _wallBase, _wallTop, _wallFor = new Vector3(float.NaN, 0f, 0f);
        private float _wallFoundAt = float.NegativeInfinity, _clingSince, _nextAssault;
        // Escalades ratées (redescendue sans bond, ou bond depuis le mur sans toucher) : après 2 en 20 s, elle renonce 10 s.
        private int _assaultFails;
        private float _firstFailAt = float.NegativeInfinity;
        private bool _wallLeap;
        // Déblocage : dernière position et depuis quand elle n'a pas bougé ; chute hors du NavMesh.
        private Vector3 _stillAt;
        private float _stillSince, _offMeshSince = float.NegativeInfinity;
        private float _fallSpeed;
        private MazeGraph _maze;
        private int _lastNode = -1;
        private bool _mazeLooked;

        /// <summary>Replacements sur le NavMesh (goule immobile en chasse), toutes goules : mesures.</summary>
        public static int Unsticks { get; private set; }
        /// <summary>Escalades vers un joueur hors d'atteinte, et bonds depuis un mur : mesures.</summary>
        public static int WallAssaults { get; private set; }
        public int Unstuck { get; private set; }
        /// <summary>Déblocages remis à plus tard parce qu'un joueur voyait la goule ou son point d'arrivée, toutes goules : mesures.</summary>
        public static int UnstickWaits { get; private set; }
        /// <summary>Identifiant d'agent NavMesh remis à jour (NavMesh reconstruit après son Start), toutes goules : mesures.</summary>
        public static int AgentResyncs { get; private set; }
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
        /// <summary>Attaques parées par un joueur (D-075).</summary>
        public int Parried { get; private set; }
        public int WallLeaps { get; private set; }
        public int Climbs { get; private set; }
        public Vector3 LastLeapStart { get; private set; }
        public Vector3 LastLeapEnd { get; private set; }
        public EnemyLocomotion Locomotion => _move;

        /// <summary>Goules actives (debug, mesures).</summary>
        public static readonly System.Collections.Generic.List<Ghoul> All = new System.Collections.Generic.List<Ghoul>();

        private readonly TargetKeeper _keeper = new TargetKeeper();
        private readonly HuntState _hunt = new HuntState();
        private ItHunts.Orbs.Grabbable _grabbable;

        private void OnEnable() => All.Add(this);
        private void OnDisable()
        {
            All.Remove(this);
            ClearSignals();                                               // recyclée par la réserve : rien ne reste allumé
        }

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
            ApplySize();
            _anim = visual != null ? visual.GetComponentInChildren<Animator>(true) : GetComponentInChildren<Animator>(true);
            if (_anim != null && _damageable != null && settings != null)
                _damageable.HideDelay = settings.deathVisualTime;
        }

        // ---------- Animations (SK_Goule, PR #6) ----------

        private Animator _anim;
        private string _clip;
        private GhoulMode _animMode;
        private float _landedAt = -9f, _strikeAt = -9f, _crouch = 1f;
        private string _forcedClip;

        /// <summary>Clip joué (tests, vitrine) ; null sans modèle animé.</summary>
        public string Clip => _clip;
        public bool Animated => _anim != null;
        /// <summary>Accroupissement dans un passage bas (1 : debout).</summary>
        public float Crouch => _crouch;

        /// <summary>Vitrine : impose un clip (Goule_Coup, Goule_Reception…) ; null rend la main à l'état.</summary>
        public void ForceClip(string clip) => _forcedClip = clip;

        /// <summary>Vitrine : rejoue un clip depuis le début (coup, annonce).</summary>
        public void RestartClip(string clip)
        {
            _forcedClip = clip;
            _clip = clip;
            if (_anim != null && _anim.isActiveAndEnabled)
                _anim.Play(clip, 0, 0f);
        }

        private void Play(string clip, float fade = 0.12f, float speed = 1f)
        {
            if (_anim == null || !_anim.isActiveAndEnabled)
                return;
            _anim.speed = speed;
            if (_clip == clip)
                return;
            _clip = clip;
            _anim.CrossFadeInFixedTime(clip, fade, 0);
        }

        /// <summary>
        /// Clip selon l'état : mort, étourdie, annonce (calée sur leapTelegraph), bond (dernière image tenue en vol), réception
        /// (0.33 s après l'atterrissage), escalade, coup (au contact), course (lecture selon la vitesse), repos.
        /// </summary>
        private void Animate()
        {
            if (_anim == null || settings == null)
                return;
            float now = Time.time;
            if (_animMode == GhoulMode.Bond && Mode != GhoulMode.Bond)
                _landedAt = now;
            _animMode = Mode;
            if (_forcedClip != null)
            {
                Play(_forcedClip);
                return;
            }

            bool dead = _damageable != null && _damageable.Dead;
            bool stunned = _body != null && (_body.Stunned || _body.Held);       // tenue : elle se débat
            float speed = _rb != null ? new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.z).magnitude : 0f;
            if (_move.Speed > speed)
                speed = _move.Speed;
            if (dead)
                Play("Goule_Mort", 0.08f);
            else if (stunned)
                Play("Goule_Etourdie", 0.1f);
            else if (Mode == GhoulMode.Annonce)
                Play("Goule_Annonce", 0.05f, 0.3f / Mathf.Max(0.05f, settings.leapTelegraph));
            else if (Mode == GhoulMode.Bond)
                Play("Goule_Bond", 0.05f);
            else if (Mode == GhoulMode.Mur)
                Play("Goule_Escalade", 0.1f);
            else if (now - _landedAt < 0.33f)
                Play("Goule_Reception", 0.04f);
            else if (now - _strikeAt < 0.47f)
                Play("Goule_Coup", 0.06f);
            else if (speed > 0.5f)
                Play("Goule_Course", 0.15f, Mathf.Clamp(speed / Mathf.Max(0.1f, settings.runSpeed), 0.3f, 2f));
            else
                Play("Goule_Repos", 0.2f);
        }

        /// <summary>
        /// Passage bas (retours du lot) : plafond au-dessus d'elle plus bas que sa taille : elle se baisse (visuel et capsule)
        /// jusqu'à minCrouch, comme le traqueur sous un pont. Le NavMesh des goules la laisse passer partout où passait la goule de
        /// 1.1 m (agent de 1.2 m).
        /// </summary>
        private void UpdateCrouch(float dt)
        {
            if (settings == null || _rb == null)
                return;
            float full = GhoulSettings.ModelHeight * settings.BodyScale;
            float target = 1f;
            if (Mode != GhoulMode.Mur && Physics.Raycast(_rb.position + Vector3.up * 0.3f, Vector3.up, out RaycastHit hit, full, ~0, QueryTriggerInteraction.Ignore)
                && !hit.collider.transform.IsChildOf(transform))
                target = Mathf.Clamp((hit.distance + 0.3f - 0.05f) / full, settings.minCrouch, 1f);
            _crouch = dt <= 0f ? target : Mathf.MoveTowards(_crouch, target, settings.crouchRate * dt);
            if (_capsule != null)
            {
                float h = full * _crouch;
                _capsule.height = Mathf.Max(h, _capsule.radius * 2f);
                _capsule.center = new Vector3(0f, _capsule.height * 0.5f, 0f);
            }
        }

        private float _sizeApplied = -1f;
        private Vector3 _telegraphBase;

        /// <summary>
        /// Taille réglée (GhoulSettings.bodyHeight, retours du lot, point 4) : capsule (hauteur, centre, rayon), modèle sous
        /// « Corps (visuel) » (mis à l'échelle, le code garde l'écrasement de l'annonce et la bascule sur les murs) et disque d'annonce.
        /// Rejouée si le réglage change en Play Mode.
        /// </summary>
        public void ApplySize()
        {
            if (settings == null)
                return;
            float s = settings.BodyScale;
            if (Mathf.Approximately(s, _sizeApplied) && _capsule != null && Mathf.Approximately(_capsule.radius, settings.bodyRadius))
                return;
            _sizeApplied = s;
            if (_capsule != null)
            {
                _capsule.height = GhoulSettings.ModelHeight * s * _crouch;
                _capsule.center = new Vector3(0f, _capsule.height * 0.5f, 0f);
                _capsule.radius = Mathf.Min(settings.bodyRadius, GhoulSettings.ModelHeight * s * 0.5f);
            }

            if (visual != null)
                foreach (Transform child in visual)
                    child.localScale = Vector3.one * s;
            // Seuils de LOD réglables (la DA les règle à l'import pour 1.1 m ; le .meta de son modèle ne se touche pas sur main).
            foreach (LODGroup group in GetComponentsInChildren<LODGroup>(true))
            {
                LOD[] lods = group.GetLODs();
                for (int i = 0; i < lods.Length && i < 3; i++)
                    lods[i].screenRelativeTransitionHeight = settings.lodTransitions[i];
                group.SetLODs(lods);
                group.RecalculateBounds();
            }
            if (telegraph != null)
            {
                if (_telegraphBase == Vector3.zero)
                    _telegraphBase = telegraph.transform.localScale;
                telegraph.transform.localScale = new Vector3(_telegraphBase.x * s, _telegraphBase.y, _telegraphBase.z * s);
            }
        }

        private void Start()
        {
            ApplySize();
            _move.StayOnMesh = true;
            _nav = FindFirstObjectByType<ZoneNavMesh>();
            if (_nav != null)
                _move.AgentType = _nav.SmallAgent;
        }

        /// <summary>Remise à zéro de la zone : à son poste, sans cible.</summary>
        /// <summary>Vagues (D-041) : apparaît à ce point (nouvelle place de départ), entière, en attente.</summary>
        public void Spawn(Vector3 feet, Quaternion rotation)
        {
            if (_body == null)
                Awake();
            _home = feet;
            _homeRotation = rotation;
            if (_body != null)
                _body.SetHome(feet);
            if (_damageable != null)
                _damageable.Revive();
            ResetState();
        }

        public void ResetState()
        {
            _move.SpeedScale = ItHunts.Level.RunSession.EnemySpeed;   // difficulté (D-048)
            transform.SetPositionAndRotation(_home, _homeRotation);
            if (_rb != null)
                _rb.position = _home;
            _move.Clear();
            _move.Heading = _homeRotation * Vector3.forward;
            _target = null;
            _keeper.Clear();
            _hunt.Clear();
            _lastNode = -1;
            _retargetAt = 0f;
            _nextLeap = 0f;
            _nextContact = 0f;
            SetMode(GhoulMode.Poste);
            ClearSignals();
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
            if (settings == null)
                return;
            if (_damageable != null && _damageable.Dead)
            {
                ClearSignals();                                           // morte pendant l'annonce : yeux, gueule, corps et disque éteints
                return;
            }
            if (ZoneState.Asleep && !_body.Busy)
            {
                _move.Speed = 0f;                                          // salle de départ pas encore quittée (D-048)
                return;
            }
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            float now = Time.time, dt = Time.fixedDeltaTime;
            // Le NavMesh peut être reconstruit après son Start (nouvel identifiant d'agent) : sans ça, plus aucun chemin
            // (playtest des étapes 8 et 9 : goules immobiles à leur place de départ).
            if (_nav == null)
                _nav = FindFirstObjectByType<ZoneNavMesh>();
            if (_nav != null && _move.AgentType != _nav.SmallAgent)
            {
                _move.AgentType = _nav.SmallAgent;
                _move.Clear();
                AgentResyncs++;
            }

            if (_body.Busy)
            {
                if (_grabbable == null)
                    _grabbable = GetComponent<ItHunts.Orbs.Grabbable>();
                if (_grabbable != null && _grabbable.Held && _damageable != null && _damageable.LastAttackerId != _grabbable.LastOwnerId)
                    _damageable.NoteAttacker(_grabbable.LastOwnerId);    // attrapée : elle se retournera contre ce joueur
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
            Aggro(pos, now);
            if (!Hunting)
            {
                // En attente (D-061) : l'aggro la réveille, ou le directeur en fait une chasseuse.
                if (Mode != GhoulMode.Poste)
                    SetMode(GhoulMode.Poste);
                _move.Speed = 0f;
                ContactDamage(now);
                UpdateVisual();
                return;
            }

            ChooseTarget(pos, now);
            if (Unstick(pos, now))
            {
                UpdateVisual();
                return;
            }

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

        // ---------- Chasse (D-061) : aggro par proximité, sans limite ; chasseuses du directeur ----------

        /// <summary>En chasse : réveillée par l'aggro, ou chasseuse d'un joueur (HuntDirector) ; sinon en attente.</summary>
        public bool Hunting => _hunt.Hunting;
        /// <summary>Réveillée par l'aggro (pas chasseuse).</summary>
        public bool Aggroed => _hunt.Aggroed;
        /// <summary>Chasseuse : elle sait toujours où est son joueur.</summary>
        public bool Chaser => _hunt.ChaserFor != null;
        public int AggroWakes => _hunt.AggroWakes;
        private float _nextAggroCheck;

        public HuntState Hunt => _hunt;
        public bool CanHunt => isActiveAndEnabled && (_damageable == null || !_damageable.Dead) && !ZoneState.Asleep;
        public Vector3 Position => _rb != null ? _rb.position : transform.position;

        public float PathLength(PlayerHealth p)
        {
            Vector3 pos = Position;
            return _move.Probe(pos, p.transform.position, out float l, out _) ? l : Vector3.Distance(pos, p.transform.position) * 3f;
        }

        /// <summary>
        /// Aggro (D-061) : en attente, elle se réveille si un joueur debout est visible à moins de aggroSightRange ou à moins de
        /// aggroProximity (sans ligne de vue), sans limite de nombre. Elle le poursuit jusqu'à être semée : plus aucun joueur à
        /// moins de aggroLoseDistance pendant aggroLoseTime ; elle attend alors là où elle est.
        /// </summary>
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
                if (!seen && (d <= settings.aggroProximity
                              || (d <= settings.aggroSightRange && LineOfSight(pos + Vector3.up * Height * 0.8f, p))))
                    seen = true;
            }

            bool was = Hunting;
            _hunt.Step(seen, nearest, now, settings.aggroLoseDistance, settings.aggroLoseTime);
            if (was && !Hunting)
            {
                _target = null;                                          // semée : elle attend là où elle est
                _keeper.Clear();
                _move.Clear();
            }
            else if (!was && Hunting)
                _retargetAt = 0f;
        }

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
        /// Cible : une chasseuse vise son joueur. Sinon elle sait où sont les joueurs (D-035) ; coop (D-041) : au départ le plus
        /// proche par le chemin, puis elle garde sa cible (TargetKeeper : marge, abandon, délai ; immédiat si sa cible tombe ou si
        /// un autre joueur la blesse ou la lance).
        /// </summary>
        private void ChooseTarget(Vector3 pos, float now)
        {
            PlayerHealth chased = _hunt.ChaserFor;
            if (chased != null && !chased.Dead)
                _target = chased;
            else
                _target = _keeper.Update(now, _damageable, settings, settings.targetInterval,
                    p => _move.Probe(pos, p.transform.position, out float l, out _) ? l : Vector3.Distance(pos, p.transform.position) * 3f);
            if (_target == null && Mode == GhoulMode.Chasse)
                SetMode(GhoulMode.Poste);
        }

        /// <summary>Changements de cible (mesures, debug).</summary>
        public int TargetSwitches => _keeper.Switches;

        /// <summary>Debug et mesures : dernier passage dans la poursuite, but visé, branche prise.</summary>
        public float DebugChaseAt { get; private set; }
        public Vector3 DebugGoal { get; private set; }
        public string DebugBranch { get; private set; } = "";

        private void Chase(Vector3 pos, float now, float dt)
        {
            DebugChaseAt = now;
            DebugBranch = "début";
            if (_target == null)
                return;
            Vector3 targetPos = _target.transform.position;
            if (TryStartLeap(pos, targetPos, now))
            {
                DebugBranch = "bond";
                return;
            }

            // À portée de frappe (playtest des animations) : elle s'arrête face au joueur et frappe (ContactDamage), au lieu d'avancer
            // contre lui et de le pousser. Elle repart quand il sort de portée (écart > contactRange).
            float gap = Gap(_target);
            if (gap <= settings.attackStopGap || (_holdingAttack && gap <= settings.contactRange))
            {
                _holdingAttack = true;
                _move.Speed = 0f;
                Face(targetPos, dt);
                DebugBranch = "à portée : frappe";
                return;
            }

            _holdingAttack = false;

            // Hors du NavMesh (posée sur une plateforme après un bond, ou tombée) : marche droit ou chute.
            if (!_move.Snap(pos, 0.6f, out _))
            {
                DebugBranch = "hors NavMesh";
                OffMeshStep(pos, targetPos, dt);
                return;
            }

            _fallSpeed = 0f;
            float distance = new Vector3(targetPos.x - pos.x, 0f, targetPos.z - pos.z).magnitude;
            Vector3 goal = targetPos;
            if (distance > settings.longPathDistance && Maze != null && Maze.Waypoint(pos, targetPos, ref _lastNode, out Vector3 step)
                && _move.Snap(step, 10f, out Vector3 stepOnMesh))
                goal = stepOnMesh;                                      // long trajet : la salle suivante vers le joueur
            else if (distance > settings.encircleDirectDistance && settings.encircleRadius > 0f
                && _move.Snap(targetPos + _encircle * settings.encircleRadius, 2f, out Vector3 around)
                && Mathf.Abs(around.y - targetPos.y) < 2f)
                goal = around;                                          // encerclement : un point décalé autour de lui
            _move.SetDestination(pos, goal, now, settings.repathInterval);
            if (_move.Partial && goal != targetPos && distance <= settings.longPathDistance)
                _move.SetDestination(pos, targetPos, now, settings.repathInterval, force: true);   // point sur un îlot : droit sur lui

            // Joueur hors d'atteinte par le sol (plateforme, poutre) : le mur le plus proche de lui, puis on grimpe.
            DebugGoal = goal;
            DebugBranch = goal == targetPos ? "droit" : "étape ou encerclement";
            if (TryWallAssault(pos, targetPos, distance, now))
            {
                DebugBranch = "escalade";
                return;
            }

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
            if (_assault)
            {
                // Vers le joueur : face au mur en montant, accrochée en haut jusqu'au bond (ou elle redescend).
                Vector3 face = _target != null ? _target.transform.position - pos : flat;
                face.y = 0f;
                if (face.sqrMagnitude > 0.01f)
                    _move.Heading = face.normalized;
                if (to.magnitude > step)
                {
                    _move.Speed = speed;
                    MoveTo(pos + to.normalized * step);
                    _clingSince = now;
                    return;
                }

                MoveTo(_climbTo);
                _move.Speed = 0f;
                if (now - _clingSince > settings.wallHoldTime || _target == null || now - _modeSince > 10f)
                    EndAssault(now);
                return;
            }

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
            // Depuis un mur (escalade vers un joueur hors d'atteinte) : à sa hauteur ou au-dessus, jusqu'à wallLeapRange.
            bool fromWall = Mode == GhoulMode.Mur && _assault && drop > -2.5f && drop < 16f && distance <= settings.wallLeapRange;
            if (!(fromHigh || ground || fromWall) || !LineOfSight(pos + Vector3.up * Height * 0.8f, _target))
                return false;
            _move.Speed = 0f;
            SetMode(GhoulMode.Annonce);
            if (fromHigh || fromWall)
                WallLeaps++;
            _wallLeap = fromWall;
            _assault = false;
            var art = ItHunts.Audio.GameAudio.Settings;                 // alerte de la DA (PR #20), sinon sifflement provisoire
            ItHunts.Audio.GameAudio.PlayOn(_audio, art != null ? art.ghoulAlert : null, _hiss, art != null && art.ghoulAlert != null ? art.ghoulVolume : 0.8f);
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
            bool landed = false;
            if (distance > 1e-5f && _rb.SweepTest(move / distance, out RaycastHit hit, distance + 0.03f, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<PlayerHealth>() == null)
            {
                next = pos + move / distance * Mathf.Max(0f, hit.distance - 0.03f);
                Vector3 n = hit.normal;
                float into = Vector3.Dot(_leapVelocity, n);
                if (into < 0f)
                    _leapVelocity -= n * into;                        // glisse le long du mur, sans s'étourdir
                landed = n.y > 0.7f && _leapVelocity.y <= 0.5f;        // posée sur une plateforme (même sans NavMesh)
            }

            if (landed)
            {
                if (_wallLeap && !_leapHit)
                    AssaultFailed(now);
                _wallLeap = false;
                LastLeapEnd = next;
                _nextLeap = now + settings.leapCooldown;
                _move.Speed = 0f;
                _move.Clear();
                SetMode(GhoulMode.Chasse);
                MoveTo(next);
                LeapHitCheck(now, new Vector3(_leapVelocity.x, 0f, _leapVelocity.z));
                return;
            }

            // Retombée : sol sous elle et vitesse vers le bas.
            if (_leapVelocity.y <= 0f && _move.Snap(next, 0.25f, out Vector3 ground) && next.y <= ground.y + 0.05f)
            {
                next = ground;
                if (_wallLeap && !_leapHit)
                    AssaultFailed(now);
                _wallLeap = false;
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

            LeapHitCheck(now, flat);
        }

        /// <summary>Touché en vol : dégâts du bond et petite poussée, une fois par bond.</summary>
        private void LeapHitCheck(float now, Vector3 flat)
        {
            if (!_leapHit)
                foreach (PlayerHealth p in PlayerHealth.All)
                {
                    if (p == null || p.Dead || Gap(p) > settings.contactRange)
                        continue;
                    _leapHit = true;
                    LeapHits++;
                    AttackSound();
                    if (ItHunts.Player.PlayerParry.TryParry(p, this, now, p.Center))
                    {
                        Parried++;                                     // paré (D-075) : ni dégâts ni poussée, elle est étourdie
                        _nextContact = now + settings.contactInterval;
                        break;
                    }

                    if (p.TakeEnemyDamage(settings.leapDamage, p.Center, now))
                        _nextContact = now + settings.contactInterval;
                    Vector3 push = flat.sqrMagnitude > 0.01f ? flat.normalized * settings.leapPush : Vector3.zero;
                    if (p.Motor != null)
                        p.Motor.AddVelocity(push + Vector3.up * settings.leapPush * 0.25f);
                    break;
                }
        }

        private MazeGraph Maze
        {
            get
            {
                if (!_mazeLooked)
                {
                    _mazeLooked = true;
                    _maze = FindFirstObjectByType<MazeGraph>();
                }

                return _maze;
            }
        }

        /// <summary>
        /// Joueur hors d'atteinte par le sol (plateforme ou poutre au-dessus d'elle, rien sur le NavMesh à sa hauteur) et à
        /// moins de wallAssaultRange : elle court au pied du mur le plus proche de lui, le grimpe jusqu'à sa hauteur et bondit
        /// (TryStartLeap, annonce leapTelegraph). Vrai si elle s'en occupe ce pas-ci.
        /// </summary>
        private bool TryWallAssault(Vector3 pos, Vector3 targetPos, float distance, float now)
        {
            float rise = targetPos.y - pos.y;
            bool unreachable = rise > 1.4f && (_move.Destination.y < targetPos.y - 1.2f || (_move.Partial && Vector3.Distance(_move.PathEnd, targetPos) > 3f));
            if (!unreachable || distance > settings.wallAssaultRange || now < _nextAssault)
                return false;
            if (float.IsNaN(_wallFor.x) || Vector3.Distance(_wallFor, targetPos) > 3f || now - _wallFoundAt > 2f)
            {
                _wallFoundAt = now;
                _wallFor = targetPos;
                if (!FindAssaultWall(targetPos, out _wallBase, out _wallTop))
                {
                    _wallFor = new Vector3(float.NaN, 0f, 0f);
                    _nextAssault = now + 1f;
                    return false;
                }
            }

            Vector3 toBase = _wallBase - pos;
            toBase.y = 0f;
            if (toBase.magnitude < 1.2f)
            {
                _climbFrom = pos;
                _climbTo = _wallTop;
                _assault = true;
                _clingSince = now;
                Climbs++;
                WallAssaults++;
                SetMode(GhoulMode.Mur);
                return true;
            }

            _move.SetDestination(pos, _wallBase, now, settings.repathInterval, force: Vector3.Distance(_move.Destination, _wallBase) > 1f);
            MoveTo(_move.Step(pos, settings.runSpeed, settings.acceleration, settings.turnRate, Time.fixedDeltaTime));
            return true;
        }

        private readonly RaycastHit[] _wallHits = new RaycastHit[8];

        /// <summary>
        /// Mur le plus proche du joueur (8 directions, à wallSearchRange au plus), vertical, plein depuis le sol (NavMesh au pied)
        /// jusqu'à sa hauteur : pied (sur le NavMesh) et haut (contre le mur, à la hauteur de ses pieds + 0.5 m).
        /// </summary>
        public bool FindAssaultWall(Vector3 targetPos, out Vector3 bottom, out Vector3 top)
        {
            bottom = top = default;
            float best = float.PositiveInfinity;
            Vector3 eye = targetPos + Vector3.up * 1f;
            for (int i = 0; i < 16; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, (i % 8) * 45f, 0f) * Vector3.forward;
                RaycastHit wall;
                if (i < 8)
                {
                    // Un mur autour du joueur, à sa hauteur.
                    if (!Physics.Raycast(eye, dir, out wall, settings.wallSearchRange, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                }
                else
                {
                    // Le flanc de sa plateforme : depuis 6 m dehors, 1.5 m sous ses pieds, vers lui.
                    Vector3 outside = targetPos + dir * 6f + Vector3.down * 1.5f;
                    if (Physics.CheckSphere(outside, 0.3f, ~0, QueryTriggerInteraction.Ignore)
                        || !Physics.Raycast(outside, -dir, out wall, 6f, ~0, QueryTriggerInteraction.Ignore))
                        continue;
                }

                if (Mathf.Abs(wall.normal.y) > 0.3f || wall.collider.attachedRigidbody != null
                    || wall.collider.GetComponentInParent<PlayerHealth>() != null || wall.collider.GetComponentInParent<Damageable>() != null)
                    continue;
                Vector3 n = new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
                Vector3 foot = wall.point + n * 0.6f;
                if (!Physics.Raycast(foot, Vector3.down, out RaycastHit floor, 30f, ~0, QueryTriggerInteraction.Ignore)
                    || floor.point.y > targetPos.y - 1.2f || !_move.Snap(floor.point, 1.2f, out Vector3 onMesh))
                    continue;
                // Le mur doit être plein du sol jusqu'en haut (pas un trou).
                bool solid = true;
                float upTo = i < 8 ? targetPos.y + 0.5f : targetPos.y - 0.5f;
                for (float y = onMesh.y + 1f; y < upTo; y += 1.5f)
                    if (!Physics.Raycast(new Vector3(foot.x, y, foot.z), -n, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    {
                        solid = false;
                        break;
                    }

                float d = new Vector2(wall.point.x - targetPos.x, wall.point.z - targetPos.z).magnitude;
                if (!solid || d >= best)
                    continue;
                best = d;
                bottom = onMesh;
                top = wall.point + n * 0.45f;
                top.y = targetPos.y + (i < 8 ? 0.5f : 0.2f);
            }

            return !float.IsPositiveInfinity(best);
        }

        private float _nextIslandCheck, _lastOnMeshAt, _inPitSince = float.NegativeInfinity;

        /// <summary>Une entrée de la salle reliée à une autre de ses entrées (ou la seule) : sur le NavMesh.</summary>
        private bool RoomExit(int node, out Vector3 exit)
        {
            exit = default;
            bool any = false;
            Vector3 first = default;
            for (int e = 0; e < Maze.EdgeCount; e++)
            {
                if (Maze.Edge(e).x != node && Maze.Edge(e).y != node)
                    continue;
                if (!_move.Snap(Maze.Entry(node, e), 10f, out Vector3 entry))
                    continue;
                if (!any)
                {
                    first = entry;
                    any = true;
                    continue;
                }

                if (_move.Probe(first, entry, out _, out _))
                {
                    exit = first;
                    return true;
                }
            }

            exit = first;
            return any;
        }
        /// <summary>Goules replacées après plus de 6 s hors du NavMesh, toutes goules : mesures.</summary>
        public static int Rescues { get; private set; }
        /// <summary>Goules sorties d'un îlot du NavMesh (fosse, étage sans accès), toutes goules : mesures.</summary>
        public static int IslandEscapes { get; private set; }

        /// <summary>
        /// Sur un îlot : depuis ce point, aucune entrée de sa salle (MazeGraph) n'est atteignable alors qu'une autre l'est depuis
        /// l'entrée choisie. exit : cette entrée (sur le NavMesh).
        /// </summary>
        private bool OnIsland(Vector3 pos, out Vector3 exit)
        {
            exit = pos;
            if (Maze == null)
                return false;
            int node = Maze.NodeAt(pos);
            if (node < 0)
                return false;                                           // dans un couloir : jamais un îlot
            Vector3 candidate = pos;
            bool found = false;
            for (int e = 0; e < Maze.EdgeCount; e++)
            {
                if (Maze.Edge(e).x != node && Maze.Edge(e).y != node)
                    continue;
                if (!_move.Snap(Maze.Entry(node, e), 10f, out Vector3 entry))
                    continue;
                if (_move.Probe(pos, entry, out _, out _))
                    return false;                                       // une sortie : pas un îlot
                if (!found)
                {
                    candidate = entry;
                    found = true;
                }
            }

            exit = candidate;
            return found;
        }

        /// <summary>Un chemin complet existe de ce point vers le joueur, ou vers l'étape suivante (long trajet).</summary>
        private bool PathOut(Vector3 from, Vector3 target)
        {
            float d = new Vector3(target.x - from.x, 0f, target.z - from.z).magnitude;
            Vector3 goal = target;
            int node = _lastNode;
            if (d > settings.longPathDistance && Maze != null && Maze.Waypoint(from, target, ref node, out Vector3 w) && _move.Snap(w, 10f, out Vector3 wOn))
                goal = wOn;
            return _move.Probe(from, goal, out _, out _);
        }

        private void AssaultFailed(float now)
        {
            if (now - _firstFailAt > 20f)
            {
                _firstFailAt = now;
                _assaultFails = 0;
            }

            if (++_assaultFails >= 2)
            {
                _nextAssault = now + 10f;                               // elle renonce un moment et attend au bord
                _assaultFails = 0;
                _firstFailAt = float.NegativeInfinity;
            }
        }

        /// <summary>Fin d'une escalade sans bond : elle se laisse retomber au pied du mur et réessaie plus tard.</summary>
        private void EndAssault(float now)
        {
            _assault = false;
            _nextAssault = now + 2f;
            AssaultFailed(now);
            _wallFor = new Vector3(float.NaN, 0f, 0f);
            if (_move.Snap(_climbFrom, 3f, out Vector3 ground))
                MoveTo(ground);
            _move.Clear();
            SetMode(GhoulMode.Chasse);
        }

        /// <summary>
        /// Hors du NavMesh (plateforme sans NavMesh après un bond, ou dans le vide) : avec du sol sous elle, elle marche droit
        /// vers le joueur sans dépasser le bord ; sans sol, elle tombe.
        /// </summary>
        private void OffMeshStep(Vector3 pos, Vector3 targetPos, float dt)
        {
            if (!Physics.Raycast(pos + Vector3.up * 0.3f, Vector3.down, out RaycastHit under, 0.6f, ~0, QueryTriggerInteraction.Ignore)
                || under.collider.GetComponentInParent<PlayerHealth>() != null)
            {
                _fallSpeed = Mathf.Min(_fallSpeed + settings.gravity * dt, 40f);
                float fall = _fallSpeed * dt;
                Vector3 next = pos + Vector3.down * fall;
                if (Physics.Raycast(pos + Vector3.up * 0.3f, Vector3.down, out RaycastHit land, fall + 0.3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    next = land.point;
                    _fallSpeed = 0f;
                }

                MoveTo(next);
                return;
            }

            _fallSpeed = 0f;
            Vector3 to = targetPos - pos;
            to.y = 0f;
            if (to.magnitude < 0.6f)
                return;
            _move.Heading = Vector3.RotateTowards(_move.Heading, to.normalized, settings.turnRate * Mathf.Deg2Rad * dt, 0f).normalized;
            Vector3 ahead = pos + _move.Heading * Mathf.Min(settings.runSpeed * 0.6f * dt, to.magnitude - 0.5f);
            if (Physics.Raycast(ahead + Vector3.up * 0.3f, Vector3.down, out RaycastHit g, 0.8f, ~0, QueryTriggerInteraction.Ignore)
                && !Physics.Raycast(pos + Vector3.up * 0.5f, _move.Heading, 0.5f + Radius, ~0, QueryTriggerInteraction.Ignore))
                MoveTo(new Vector3(ahead.x, g.point.y, ahead.z));     // pas au-delà du bord ni dans un mur
        }

        /// <summary>
        /// Déblocage (playtest des étapes 8 et 9) : en chasse, immobile plus de unstuckTime alors que le joueur est à plus de 3 m
        /// (ni accrochée à un mur pour bondir, ni en annonce, ni attrapée) : elle se replace sur le NavMesh le plus proche et
        /// recalcule son chemin. Vrai si elle vient d'être replacée.
        /// </summary>
        private bool Unstick(Vector3 pos, float now)
        {
            bool busyMode = Mode == GhoulMode.Annonce || Mode == GhoulMode.Bond || Mode == GhoulMode.Mur;
            bool onMesh0 = _move.Snap(pos, 0.6f, out _);
            // Filet de sécurité : plus de 6 s sans toucher le NavMesh, quel que soit son mode (fosse, poutres, va-et-vient sur un
            // mur) : elle est replacée à une entrée de sa salle, à défaut sur le NavMesh le plus proche.
            // Dans une fosse de module (plus de 0.8 m sous le sol de sa salle) : replacée dès 1.5 s, hors escalade.
            int pitNode = Maze != null ? Maze.NodeAt(pos) : -1;
            bool inPit = pitNode >= 0 && pos.y < Maze.Center(pitNode).y - 0.8f && Mode != GhoulMode.Mur;
            if (!inPit)
                _inPitSince = float.NegativeInfinity;
            else if (float.IsNegativeInfinity(_inPitSince))
                _inPitSince = now;
            bool pitTooLong = inPit && now - _inPitSince > 1.5f;
            if (onMesh0 && !pitTooLong)
                _lastOnMeshAt = now;
            else if (pitTooLong || now - _lastOnMeshAt > settings.unstuckTime + 4f)
            {
                Vector3 rescue = pos;
                bool ok = (pitNode >= 0 && RoomExit(pitNode, out rescue))
                          || _move.Snap(pos, 8f, out rescue) || _move.Snap(pos, 25f, out rescue);
                if (ok && Seen(pos, rescue))
                    return false;                                         // un joueur la voit : elle attend (nouvel essai au pas suivant)
                _lastOnMeshAt = now;
                _inPitSince = float.NegativeInfinity;
                if (ok)
                {
                    MoveTo(rescue);
                    _move.Clear();
                    _assault = false;
                    SetMode(GhoulMode.Chasse);
                    Unsticks++;
                    Rescues++;
                    Unstuck++;
                    return true;
                }
            }

            // Toutes les 2 s : sur un îlot du NavMesh (fond d'une fosse, étage de module sans accès), aucune sortie de sa salle :
            // elle est replacée à une entrée de sa salle. Indépendant du joueur (un joueur hors d'atteinte n'en est pas un).
            if (now >= _nextIslandCheck && Mode != GhoulMode.Annonce)
            {
                _nextIslandCheck = now + 2f + (GetInstanceID() & 7) * 0.05f;
                if (onMesh0 && OnIsland(pos, out Vector3 exit))
                {
                    if (Seen(pos, exit))
                    {
                        _nextIslandCheck = now + 0.25f;                   // un joueur la voit : elle attend
                        return false;
                    }

                    MoveTo(exit);
                    _move.Clear();
                    _assault = false;
                    SetMode(GhoulMode.Chasse);
                    Unsticks++;
                    IslandEscapes++;
                    Unstuck++;
                    return true;
                }
            }

            // Hors du NavMesh depuis unstuckTime (tombée dans une fosse, prise dans un décor), même si elle gigote.
            // (Accrochée à un mur ou en annonce : hors du NavMesh normalement. Un bond compte, mais avec ses 3 s de vol en plus.)
            if (onMesh0 || Mode == GhoulMode.Mur || Mode == GhoulMode.Annonce)
                _offMeshSince = float.NegativeInfinity;
            else if (float.IsNegativeInfinity(_offMeshSince))
                _offMeshSince = now;
            bool offTooLong = !float.IsNegativeInfinity(_offMeshSince)
                              && now - _offMeshSince > settings.unstuckTime + (Mode == GhoulMode.Bond ? 3f : 0f);

            bool waiting = busyMode || _target == null || Vector3.Distance(pos, _target.transform.position) < 3f;
            if (!offTooLong && (waiting || (pos - _stillAt).sqrMagnitude > 0.09f))
            {
                _stillAt = pos;
                _stillSince = now;
                return false;
            }

            if (!offTooLong && now - _stillSince < settings.unstuckTime)
                return false;
            float stillSince0 = _stillSince, offMeshSince0 = _offMeshSince;
            _stillSince = now;
            _offMeshSince = float.NegativeInfinity;
            // Sous un joueur hors d'atteinte, sans mur à grimper : elle attend, ce n'est pas un blocage.
            if (onMesh0 && !_move.HasPath && _target != null && _target.transform.position.y - pos.y > 1.4f)
                return false;
            if (!_move.Snap(pos, 8f, out Vector3 onMesh) && !_move.Snap(pos, 25f, out onMesh))
                return false;
            // Sur un îlot (étage de module sans accès) : l'entrée de sa salle.
            if (_target != null && !PathOut(onMesh, _target.transform.position) && Maze != null)
            {
                int node = Maze.NodeAt(onMesh) >= 0 ? Maze.NodeAt(onMesh) : _lastNode;
                if (node >= 0 && Maze.Waypoint(onMesh, _target.transform.position, ref node, out Vector3 w) && _move.Snap(w, 10f, out Vector3 wOn))
                {
                    int self = Maze.NodeAt(onMesh);
                    for (int e = 0; self >= 0 && e < Maze.EdgeCount; e++)
                        if ((Maze.Edge(e).x == self || Maze.Edge(e).y == self) && _move.Snap(Maze.Entry(self, e), 10f, out Vector3 entry)
                            && _move.Probe(entry, wOn, out _, out _))
                        {
                            onMesh = entry;
                            break;
                        }
                }
            }

            if (Seen(pos, onMesh))
            {
                _stillSince = stillSince0;                                // un joueur la voit : elle attend (nouvel essai au pas suivant)
                _offMeshSince = offMeshSince0;
                return false;
            }

            MoveTo(onMesh);
            _move.Clear();
            _assault = false;
            SetMode(GhoulMode.Chasse);
            Unsticks++;
            Unstuck++;
            return true;
        }

        /// <summary>
        /// Téléportation de déblocage permise ? Seulement hors de la vue de tous les joueurs (corrections des étapes 8 et 9) :
        /// ni la goule (pieds et tête) ni son point d'arrivée ne sont vus (PlayerSight : champ de la caméra + 10°, sans décor
        /// entre). Vrai si un joueur voit l'un des deux : elle attend.
        /// </summary>
        private bool Seen(Vector3 from, Vector3 to)
        {
            Vector3 low = Vector3.up * 0.3f, high = Vector3.up * Mathf.Max(0.5f, Height);
            bool seen = PlayerSight.SeenByAnyPlayer(from + low, from + high, 10f, transform)
                        || PlayerSight.SeenByAnyPlayer(to + low, to + high, 10f, transform);
            if (seen)
                UnstickWaits++;
            return seen;
        }

        private PlayerHealth _strikeTarget;
        private bool _holdingAttack;
        private PlayerHealth _pendingHit;
        private float _pendingImpact;

        /// <summary>Coups portés au contact (mesures).</summary>
        public int Strikes { get; private set; }
        /// <summary>À l'arrêt, à portée de frappe (mesures).</summary>
        public bool HoldingAttack => _holdingAttack;

        /// <summary>
        /// Coup au contact (Goule_Coup) : au contact, elle arme son coup ; les dégâts tombent à l'impact du clip (contactImpactDelay,
        /// 0.24 s) si le joueur est encore à portée (un peu de marge), au plus une fois par contactInterval.
        /// </summary>
        private void ContactDamage(float now)
        {
            // Coup tombé mais en attente (tolérance après l'impact de la parade) : paré par un appui juste après, sinon les dégâts.
            if (_pendingHit != null)
            {
                PlayerHealth p = _pendingHit;
                if (p.Dead)
                    _pendingHit = null;
                else if (ItHunts.Player.PlayerParry.TryLateParry(p, this, _pendingImpact, now, p.Center))
                {
                    Parried++;
                    _pendingHit = null;
                }
                else if (now - _pendingImpact >= ItHunts.Player.PlayerParry.LateWindowOf(p))
                {
                    _pendingHit = null;
                    p.TakeEnemyDamage(settings.contactDamage, p.Center, now);
                }

                return;
            }

            if (_strikeTarget != null)
            {
                if (now - _strikeAt < settings.contactImpactDelay)
                    return;
                PlayerHealth target = _strikeTarget;
                _strikeTarget = null;
                if (target != null && !target.Dead && Gap(target) <= settings.contactRange + 0.4f)
                {
                    if (ItHunts.Player.PlayerParry.TryParry(target, this, now, target.Center))
                        Parried++;                                    // paré (D-075) : aucun dégât, elle est étourdie
                    else if (ItHunts.Player.PlayerParry.LateWindowOf(target) > 0f)
                    {
                        _pendingHit = target;                         // tolérance après l'impact : les dégâts attendent un peu
                        _pendingImpact = now;
                    }
                    else
                        target.TakeEnemyDamage(settings.contactDamage, target.Center, now);
                }

                return;
            }

            if (now < _nextContact)
                return;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead || Gap(p) > settings.contactRange)
                    continue;
                _strikeAt = now;
                _strikeTarget = p;
                Strikes++;
                AttackSound();
                _nextContact = now + settings.contactInterval;
                if (settings.contactImpactDelay <= 0f)
                    ContactDamage(now);
                break;
            }
        }

        /// <summary>Coup ou bond qui touche : une variante d'attaque de la DA au hasard (PR #20).</summary>
        private void AttackSound()
        {
            var art = ItHunts.Audio.GameAudio.Settings;
            if (art != null && _audio != null)
                ItHunts.Audio.GameAudio.PlayOn(_audio, ItHunts.Audio.GameAudio.Pick(art.ghoulAttacks), null, art.ghoulVolume);
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

        private Renderer[] _glowParts, _bodyParts;
        private Color[] _bodyColors;
        private MaterialPropertyBlock _glowBlock;
        private bool _glowShown, _glowApplied;

        /// <summary>
        /// Lueur de l'annonce du bond (bloc 4) : yeux et gueule en rouge HDR, corps teinté de rouge ; lisible à 30 m et plus, sur la
        /// goule et pas seulement au sol. La gueule (greybox) est ajoutée sous la tête, sombre au repos.
        /// </summary>
        private Renderer _model;
        private bool _modelChecked;

        private void LeapGlow(bool on)
        {
            // Modèle de la DA (SM_Goule, lot du 2026-11-16) : yeux et lueur entre les dents par le masque d'émission de M_Goule,
            // sans teinter le corps (le masque suffit) ; la lueur passe par Damageable.ExtraGlow (émission de tous ses rendus).
            if (!_modelChecked)
            {
                _modelChecked = true;
                _model = GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.StartsWith("SM_Goule"));
            }

            if (_model != null)
            {
                _glowShown = on;
                _glowApplied = true;
                // Rouge sombre et discret, au repos comme pendant l'annonce (retours du lot, point 3) : une légère montée signale le
                // bond, avec la gueule (même masque) et le disque au sol ; les trois LOD partagent le matériau et le masque.
                if (_damageable != null && settings != null)
                    _damageable.ExtraGlow = settings.eyeGlowColor * (on ? settings.eyeGlowAnnounce : settings.eyeGlowRest);
                return;
            }

            if (_glowParts == null)
            {
                var eyes = GetComponentsInChildren<Renderer>(true).Where(r => r.name.StartsWith("Oeil")).ToList();
                if (eyes.Count >= 2 && eyes[0].transform.parent != null)
                {
                    GameObject mouth = Instantiate(eyes[0].gameObject, eyes[0].transform.parent);
                    mouth.name = "Gueule";
                    Vector3 a = eyes[0].transform.localPosition, b = eyes[1].transform.localPosition;
                    Vector3 sc = eyes[0].transform.localScale;
                    mouth.transform.localPosition = (a + b) * 0.5f + Vector3.down * Mathf.Abs(a.x - b.x) * 0.9f;
                    mouth.transform.localScale = new Vector3(sc.x * 1.8f, sc.y * 0.6f, sc.z);
                    eyes.Add(mouth.GetComponent<Renderer>());
                }

                _glowParts = eyes.ToArray();
                _bodyParts = GetComponentsInChildren<Renderer>(true).Where(r => r.name == "Corps" || r.name == "Tête").ToArray();
                _bodyColors = _bodyParts.Select(r => r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor") ? r.sharedMaterial.GetColor("_BaseColor") : Color.gray).ToArray();
                _glowBlock = new MaterialPropertyBlock();
            }

            if (_damageable != null)
                _damageable.ExtraGlow = on ? settings.leapGlowColor * (0.6f * settings.leapBodyTint) : Color.black;   // le corps luit
            if (_glowApplied && on == _glowShown)
                return;
            _glowApplied = true;
            _glowShown = on;
            foreach (Renderer r in _glowParts)
            {
                if (r == null)
                    continue;
                r.GetPropertyBlock(_glowBlock);
                if (on)
                    _glowBlock.SetColor("_BaseColor", settings.leapGlowColor);
                else if (r.name == "Gueule")
                    _glowBlock.SetColor("_BaseColor", new Color(0.06f, 0.02f, 0.02f));
                else
                    _glowBlock.Clear();
                r.SetPropertyBlock(_glowBlock);
            }

            for (int i = 0; i < _bodyParts.Length; i++)
            {
                if (_bodyParts[i] == null)
                    continue;
                _bodyParts[i].GetPropertyBlock(_glowBlock);
                if (on)
                    _glowBlock.SetColor("_BaseColor", Color.Lerp(_bodyColors[i], new Color(0.9f, 0.05f, 0.03f), settings.leapBodyTint));
                else
                    _glowBlock.Clear();
                _bodyParts[i].SetPropertyBlock(_glowBlock);
            }
        }

        private bool _signalsOn = true;                                  // inconnu au départ : la première remise à zéro éteint tout

        /// <summary>
        /// Éteint les signaux de l'annonce (correctifs-rendu point 4) : à la mort, à la remise à zéro et au recyclage. Avant, une goule
        /// morte pendant l'annonce gardait ses yeux rouges et son disque (sa mise à jour s'arrête à la mort).
        /// </summary>
        public void ClearSignals()
        {
            if (!_signalsOn)
                return;
            _signalsOn = false;
            if (telegraph != null)
                telegraph.enabled = false;
            if (settings != null)
                LeapGlow(false);
            else if (_damageable != null)
                _damageable.ExtraGlow = Color.black;
        }

        /// <summary>Signaux de l'annonce allumés (tests, mesures).</summary>
        public bool SignalsOn => (telegraph != null && telegraph.enabled) || (_glowApplied && _glowShown)
            || (_model == null && _damageable != null && _damageable.ExtraGlow.maxColorComponent > 0.001f);

        /// <summary>
        /// Vitrine (Test_Vitrine) : affiche un état sans l'IA (composant désactivé) : mode, depuis combien de temps (annonce qui
        /// s'écrase), mur (de climbFrom vers climbTo) et écrasement en plus (réception). Affichage seulement.
        /// </summary>
        public void ShowPose(GhoulMode mode, float modeAge, Vector3 climbFrom, Vector3 climbTo, float squash = 1f)
        {
            if (_anim != null)
                squash = 1f;                                                  // le clip montre la réception
            Mode = mode;
            _modeSince = Time.time - modeAge;
            _climbFrom = climbFrom;
            _climbTo = climbTo;
            UpdateVisual();
            if (visual != null && !Mathf.Approximately(squash, 1f))
                visual.localScale = Vector3.Scale(visual.localScale, new Vector3(1f / Mathf.Sqrt(squash), squash, 1f / Mathf.Sqrt(squash)));
        }

        private void UpdateVisual()
        {
            bool announcing = Mode == GhoulMode.Annonce;
            if (announcing)
                _signalsOn = true;
            LeapGlow(announcing && (_damageable == null || !_damageable.Dead));
            if (telegraph != null)
                telegraph.enabled = announcing && (_damageable == null || !_damageable.Dead);
            UpdateCrouch(Time.inFixedTimeStep ? Time.fixedDeltaTime : Time.deltaTime);
            Animate();
            if (visual != null)
            {
                // Modèle animé : l'accroupissement de l'annonce et l'étirement du bond sont dans les clips ; le code ne garde que la
                // bascule sur les murs et le passage bas.
                float crouch = _anim != null ? 1f
                    : announcing ? Mathf.Lerp(1f, 0.6f, Mathf.Clamp01((Time.time - _modeSince) / Mathf.Max(0.01f, settings.leapTelegraph)))
                    : Mode == GhoulMode.Bond ? 1.15f : 1f;
                visual.localScale = new Vector3(1f / Mathf.Sqrt(crouch), crouch * _crouch, 1f / Mathf.Sqrt(crouch));
                // Sur un mur : le corps se plaque contre la paroi (tête vers le haut en montant, vers le bas en descendant).
                bool wall = Mode == GhoulMode.Mur;
                float pitch = wall ? (_climbTo.y >= _climbFrom.y ? -80f : 80f) : 0f;
                visual.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }
        }
    }
}
