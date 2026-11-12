using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Le traqueur (étape 6, D-033, D-034, D-035), boss unique de la zone : un géant qui poursuit sans relâche, façon Pac-Man.
    /// - Repaire pendant le temps de grâce, puis poursuite continue : il sait toujours où est le joueur visé (plus de vue
    ///   ni d'ouïe) ; à la fin du timer, Enragé.
    /// - Règle de Pac-Man (MazeGraph) : dans un couloir, il ne fait jamais demi-tour ; à une intersection, il prend la
    ///   branche qui le rapproche le plus du joueur (par le chemin), jamais celle d'où il vient, sauf dans un cul-de-sac.
    ///   Enragé, demi-tour permis (réglage). Quand le joueur est proche et devant lui, il fonce droit dessus.
    /// - Il remplit la largeur d'un couloir (corps et main) : on ne le dépasse pas, on le sème par une boucle.
    /// - Il passe partout où la hauteur dépasse crouchMinHeight (9 m) : sous un pont ou une poutre, il se baisse (vitesse
    ///   × 0.6). Les pièces aux petites portes : il s'arrête devant une ouverture et passe le bras (15 m à l'intérieur).
    /// - Prise à la main (D-034) : annonce 0.5 s (zone rouge), visée corrigée 0.15 s avant la descente, puis la main s'abat ;
    ///   le joueur qu'elle recouvre meurt. Portée calculée depuis là où il peut vraiment se tenir (bords de la carte : il se
    ///   penche). Corps solide, mortel au contact. Pointe de vitesse quand il voit le joueur, prédiction.
    /// - Coop : il vise le joueur le plus proche par le chemin, change de cible au plus toutes les 5 s.
    /// - Intuable (Damageable.ImmuneToOrbs).
    /// Réseau (étape 11) : décisions et coup sur l'autorité ; les joueurs ne reçoivent que sa position et son état.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Stalker : MonoBehaviour
    {
        [SerializeField] private StalkerSettings settings;
        [Tooltip("Repère du point prédit (debug, page 3).")]
        [SerializeField] private Transform predictedMarker;
        [Tooltip("Corps visible (se baisse sous les ponts).")]
        [SerializeField] private Transform body;
        [Header("Bras qui attrape (modèle)")]
        [Tooltip("Bras (boîte étirée de l'épaule à la main), main (sa BoxCollider, déclencheur, est la zone de prise), " +
                 "repère rouge de la zone visée pendant l'annonce.")]
        [SerializeField] private Transform arm;
        [SerializeField] private Transform hand;
        [SerializeField] private BoxCollider handZone;
        [SerializeField] private Transform grabMarker;
        [Tooltip("Bras et main gauches (posés par le code) : au repos le long du corps, levés pour le geste au-dessus de la tête.")]
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform leftHand;

        /// <summary>Affiche le point prédit (la page 3 du debug l'allume).</summary>
        public static bool ShowDebugMarker { get; set; }

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Stalker");
        private readonly EnemyLocomotion _move = new EnemyLocomotion();
        private Rigidbody _rb;
        private CapsuleCollider _capsule;
        private ZoneNavMesh _nav;
        private ZoneState _zone;
        private MazeGraph _graph;
        private Vector3 _home;
        private Quaternion _homeRotation;
        private BurstState _burst = BurstState.Create();
        private GrabPhase _grab;
        private float _grabStart;
        private float _grabReadyAt;
        private Vector3 _grabTarget;
        private Vector3 _handFrom;
        private Transform _grabOpening;
        private bool _reaimed;
        private float _retargetAt;
        private float _nextDecision;
        private Vector3 _dest;
        private bool _hasDest;
        private int _inNode = -1;
        private int _lastNode = -1;
        private int _trackEdge = -1;
        private int _trackSign;
        private float _crouch = 1f;
        private OverheadPhase _over;
        private float _overStart;
        private float _overReadyAt;
        private PlayerHealth _overTarget;
        private Vector3 _overLeft, _overRight;

        /// <summary>Geste au-dessus de la tête (D-037) : les deux mains se lèvent, puis suivent le joueur au-dessus de lui.</summary>
        public enum OverheadPhase : byte
        {
            Aucune,
            Annonce,
            Active,
            Retour,
        }

        public OverheadPhase Overhead => _over;

        /// <summary>Main plantée au sol après une prise ratée (D-039).</summary>
        public bool Planted { get; private set; }
        public int MissedGrabs { get; private set; }
        private float _holdTime;

        private float _targetSince, _enragedSince;
        /// <summary>Enragé (D-038) : temps passé sur la même cible, palier atteint, moment du dernier palier.</summary>
        public float TimeOnTarget { get; private set; }
        public int EnragedTier { get; private set; }
        public float LastTierTime { get; private set; } = float.NegativeInfinity;
        public float EnragedBaseSpeed { get; private set; }
        public float OverheadCooldownRemaining => Mathf.Max(0f, _overReadyAt - Time.time);
        public int OverheadAttempts { get; private set; }
        public int OverheadKills { get; private set; }
        /// <summary>Morts au contact des bras ou des mains (hors prise résolue).</summary>
        public int LimbKills { get; private set; }
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Collider[] _overlap = new Collider[16];

        public enum GrabPhase : byte
        {
            Aucune,
            Annonce,
            Descente,
            AuSol,
            Retour,
        }

        public GrabPhase Grab => _grab;
        public float GrabCooldownRemaining => Mathf.Max(0f, _grabReadyAt - Time.time);
        public Vector3 GrabTarget => _grabTarget;
        public bool GrabThroughOpening => _grabOpening != null;
        public int GrabAttempts { get; private set; }
        public int GrabKills { get; private set; }
        public int ContactKills { get; private set; }
        public int RoomGrabs { get; private set; }
        public float LastGrabStart { get; private set; } = float.NegativeInfinity;

        /// <summary>Le joueur visé est hors de portée de partout (ne devrait jamais arriver dans le labyrinthe : mesure).</summary>
        public bool TargetSheltered { get; private set; }

        public StalkerSettings Settings => settings;
        public StalkerMode Mode { get; private set; } = StalkerMode.Repaire;
        public PlayerHealth Target { get; private set; }
        public bool SeesTarget { get; private set; }
        public Vector3 Predicted { get; private set; }
        public float Speed => _move.Speed;
        public float WantedSpeed { get; private set; }
        public bool BurstActive => _burst.Active(Time.time);
        public float BurstRemaining => _burst.Remaining(Time.time);
        public float BurstCooldownRemaining => _burst.CooldownRemaining(Time.time);
        public float LastRoarTime { get; private set; } = float.NegativeInfinity;
        public int Kills { get; private set; }
        public Vector3 Home => _home;
        public EnemyLocomotion Locomotion => _move;

        /// <summary>Il fonce droit sur le joueur (proche et devant lui), sinon il suit le labyrinthe.</summary>
        public bool Direct { get; private set; }
        public int InNode => _inNode;
        public int GoalNode { get; private set; } = -1;
        public int ArrivedFrom { get; private set; } = -1;
        public MazeGraph Graph => _graph;
        /// <summary>Demi-tours dans un couloir hors cul-de-sac et hors Enragé (doit rester à 0).</summary>
        public int UTurns { get; private set; }
        /// <summary>Hauteur relative (1 debout ; moins : baissé).</summary>
        public float Crouch => _crouch;
        public bool Crouching => _crouch < 0.98f;
        public float CrouchSeconds { get; private set; }
        public Vector3 Destination => _dest;

        private float Radius => settings != null ? settings.width * 0.5f : 1.25f;
        private float Height => (settings != null ? settings.height : 3.5f) * _crouch;
        private float ShoulderHeight => settings.shoulderHeight * _crouch;

        /// <summary>Épaule pour la portée : il pivote le buste vers le point visé. Portée = handReach depuis ce point.</summary>
        public Vector3 ReachShoulder(Vector3 target) => ShoulderFrom(_rb != null ? _rb.position : transform.position, target);

        private Vector3 ShoulderFrom(Vector3 feet, Vector3 target)
        {
            Vector3 flat = target - feet;
            flat.y = 0f;
            Vector3 dir = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
            return feet + Vector3.up * ShoulderHeight + dir * (settings.width * 0.4f);
        }

        /// <summary>Épaule du bras qui attrape (droite).</summary>
        public Vector3 Shoulder => _rb.position + Vector3.up * ShoulderHeight + transform.right * (settings.width * 0.4f);

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.isKinematic = true;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _capsule = GetComponent<CapsuleCollider>();
            _home = transform.position;
            _homeRotation = transform.rotation;
            _move.Heading = transform.forward;
        }

        private void Start()
        {
            _nav = FindFirstObjectByType<ZoneNavMesh>();
            _zone = FindFirstObjectByType<ZoneState>();
            _graph = FindFirstObjectByType<MazeGraph>();
            if (_nav != null)
                _move.AgentType = _nav.StalkerAgent;
        }

        /// <summary>Remise à zéro de la zone : au repaire, sans cible, pointe rechargée.</summary>
        public void ResetState()
        {
            transform.SetPositionAndRotation(_home, _homeRotation);
            _rb.position = _home;
            _move.Clear();
            _move.Heading = _homeRotation * Vector3.forward;
            Target = null;
            SeesTarget = false;
            _burst = BurstState.Create();
            Mode = StalkerMode.Repaire;
            _grab = GrabPhase.Aucune;
            _grabReadyAt = 0f;
            Planted = false;
            _grabOpening = null;
            TargetSheltered = false;
            _inNode = _lastNode = -1;
            GoalNode = ArrivedFrom = -1;
            _trackEdge = -1;
            _hasDest = false;
            _retargetAt = 0f;
            _nextDecision = 0f;
            _crouch = 1f;
            _targetSince = _enragedSince = 0f;
            EnragedTier = 0;
            TimeOnTarget = 0f;
            _over = OverheadPhase.Aucune;
            _overReadyAt = 0f;
            _overTarget = null;
            ApplyCrouch();
            if (grabMarker != null)
                grabMarker.gameObject.SetActive(false);
            PoseArm(RestHand(), 0f);
            PoseLimb(leftArm, leftHand, LeftShoulder, LeftRestHand(), transform.rotation);
        }

        private void FixedUpdate()
        {
            if (settings == null)
                return;
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            float now = Time.time, dt = Time.fixedDeltaTime;
            if (_zone == null)
                _zone = FindFirstObjectByType<ZoneState>();
            if (_graph == null)
                _graph = FindFirstObjectByType<MazeGraph>();
            bool grace = _zone != null && _zone.InGrace;
            bool enraged = _zone != null && _zone.Enraged;
            Vector3 pos = _rb.position;
            UpdateCrouch(pos, dt);

            if (grace)
            {
                Mode = StalkerMode.Repaire;
                WantedSpeed = 0f;
                MoveTo(_move.Step(pos, 0f, settings.acceleration, settings.turnRate, dt));
                UpdateMarker();
                return;
            }

            StalkerMode before = Mode;
            Mode = enraged ? StalkerMode.Enrage : StalkerMode.Chasse;
            if (before == StalkerMode.Repaire || (Mode == StalkerMode.Enrage && before != StalkerMode.Enrage))
                LastRoarTime = now;
            if (Mode == StalkerMode.Enrage && before != StalkerMode.Enrage)
                _enragedSince = now;

            ChooseTarget(pos, now);
            TrackNodes(pos, enraged);
            if (Target == null)
            {
                WantedSpeed = 0f;
                MoveTo(_move.Step(pos, 0f, settings.acceleration, settings.turnRate, dt));
                UpdateGrab(now);
                UpdateMarker();
                return;
            }

            Vector3 feet = Target.transform.position;
            Vector3 vel = EnemySenses.VelocityOf(Target);
            SeesTarget = EnemySenses.Sees(pos + Vector3.up * settings.eyeHeight * _crouch, _move.Heading, Target,
                settings.sightRange, settings.sightAngle, transform);
            Predicted = EnemyMath.Predict(feet, vel, settings.predictionHorizon);

            if (now >= _nextDecision)
            {
                _nextDecision = now + settings.repathInterval;
                Decide(pos, feet, enraged, now);
            }

            EnemyMath.UpdateBurst(ref _burst, SeesTarget && !Planted, now, settings.burstDuration, settings.burstCooldown);
            float playerSpeed = new Vector2(vel.x, vel.z).magnitude;
            // Enragé (D-038) : paliers de vitesse tant qu'il reste sur la même cible ; chaque palier s'annonce (grondement, HUD).
            float rampBase = -1f;
            if (Mode == StalkerMode.Enrage)
            {
                TimeOnTarget = now - Mathf.Max(_targetSince, _enragedSince);
                rampBase = EnemyMath.EnragedRampSpeed(TimeOnTarget, settings.enragedRampTimes, settings.enragedRampSpeeds,
                    settings.enragedRampBlend, out int tier);
                if (tier != EnragedTier)
                {
                    if (tier > EnragedTier)
                    {
                        LastRoarTime = now;
                        LastTierTime = now;
                    }

                    EnragedTier = tier;
                }

                EnragedBaseSpeed = rampBase;
            }
            else
            {
                TimeOnTarget = 0f;
                EnragedTier = 0;
            }

            WantedSpeed = EnemyMath.StalkerSpeed(Mode, _burst.Active(now) && !Planted, playerSpeed, settings, rampBase);   // main plantée : pas de pointe
            if (Crouching)
                WantedSpeed *= settings.crouchSpeedFactor;
            if (_grab != GrabPhase.Aucune)
                WantedSpeed *= settings.grabMoveFactor;
            if (_hasDest && Flat(_dest - pos) < 1f)
                WantedSpeed = 0f;
            MoveTo(_move.Step(pos, WantedSpeed, settings.acceleration, settings.turnRate * (Planted ? settings.plantedTurnFactor : 1f), dt));
            CountUTurns(enraged);
            UpdateGrab(now);
            UpdateOverhead(now, dt);

            // Contact du corps, des bras ou des mains = mort, quels que soient les PV (D-037).
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                EnemySenses.Capsule(p, out float r, out float h);
                if (EnemyMath.CapsuleGap(_rb.position, Radius, Height, p.transform.position, r, h) <= settings.attackRange
                    && StalkerStrike.Hit(p, now))
                {
                    Kills++;
                    ContactKills++;
                    continue;
                }

                if (TouchesLimb(p.transform.position, r, h) && StalkerStrike.Hit(p, now))
                {
                    Kills++;
                    LimbKills++;
                    if (_over == OverheadPhase.Active)
                        OverheadKills++;
                }
            }

            UpdateMarker();
        }

        // ---------- Cible et poursuite (D-035) ----------

        /// <summary>Coop : le joueur le plus proche par le chemin, au plus une fois par retargetInterval (tout de suite s'il meurt).</summary>
        private void ChooseTarget(Vector3 pos, float now)
        {
            bool lost = Target == null || Target.Dead;
            if (!lost && now < _retargetAt)
                return;
            _retargetAt = now + settings.retargetInterval;
            PlayerHealth best = null;
            float bestLen = float.PositiveInfinity;
            int alive = 0;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead)
                    alive++;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                if (alive == 1)
                {
                    best = p;
                    break;
                }

                Vector3 goal = PursuitGoal(p.transform.position, p.transform.position);
                float len = _move.Probe(pos, goal, out float l, out _) ? l : Vector3.Distance(pos, goal) * 3f;
                if (len < bestLen)
                {
                    bestLen = len;
                    best = p;
                }
            }

            if (best != Target || lost)
                _targetSince = now;                                 // nouvelle cible (ou cible morte) : l'accélération repart de zéro
            Target = best;
        }

        /// <summary>
        /// Où il doit aller pour atteindre le joueur : devant l'ouverture la plus proche s'il est dans une pièce, sinon le point
        /// prédit ramené sur son NavMesh (près d'un mur ou au bord : le point le plus proche où il tient).
        /// </summary>
        private Vector3 PursuitGoal(Vector3 feet, Vector3 predicted)
        {
            ReachRoom room = ReachRoom.At(feet);
            if (room != null && StandForRoom(room, feet, out Vector3 standRoom, out _))
                return standRoom;
            if (ReachRoom.At(predicted) == null && _move.Snap(predicted, Radius + 4f, out Vector3 p))
                return p;
            if (_move.Snap(feet, Radius + 4f, out p))
                return p;
            if (CanReach(feet, out Vector3 stand, out _))
                return stand;
            return _hasDest ? _dest : feet;
        }

        private void Decide(Vector3 pos, Vector3 feet, bool enraged, float now)
        {
            Vector3 goal = PursuitGoal(feet, Predicted);
            TargetSheltered = !CanReach(feet, out _, out _);
            bool uTurnOk = enraged && settings.allowUTurnWhenEnraged;
            Direct = uTurnOk || _graph == null || NearAhead(pos, goal);
            if (!Direct)
            {
                int next = NextNode(pos, goal);
                if (next < 0)
                    Direct = true;
                else
                {
                    GoalNode = next;
                    goal = _graph.Center(next);
                    if (_move.Snap(goal, Radius + 6f, out Vector3 c))
                        goal = c;
                }
            }

            _dest = goal;
            _hasDest = true;
            _move.SetDestination(pos, goal, now, 0f, true);
        }

        /// <summary>Joueur proche, devant lui, et le chemin pour l'atteindre part vers l'avant (pas de demi-tour).</summary>
        private bool NearAhead(Vector3 pos, Vector3 goal)
        {
            Vector3 to = goal - pos;
            if (Mathf.Abs(to.y) > 8f)
                return false;
            to.y = 0f;
            float d = to.magnitude;
            if (d > settings.approachDistance)
                return false;
            if (d < 2f)
                return true;
            if (Vector3.Angle(_move.Heading, to) > settings.approachAngle * 0.5f)
                return false;
            if (!_move.Probe(pos, goal, out float len, out Vector3 first))
                return false;
            if (len > d * 1.4f + 8f)
                return false;                                           // derrière un mur : il faudrait faire le tour
            return first.sqrMagnitude < 0.01f || Vector3.Dot(first.normalized, _move.Heading) > -0.2f;
        }

        /// <summary>Intersection suivante selon la règle de Pac-Man (−1 : le joueur est dans son intersection).</summary>
        private int NextNode(Vector3 pos, Vector3 goal)
        {
            if (_inNode >= 0)
            {
                int n = _graph.ChooseNext(_inNode, ArrivedFrom, goal, false);
                return n;
            }

            // Dans un couloir : il garde la direction prise (vers l'intersection devant lui).
            if (GoalNode < 0 || !IsAhead(pos, GoalNode))
                GoalNode = NodeAhead(pos);
            return GoalNode;
        }

        private bool IsAhead(Vector3 pos, int node)
        {
            Vector3 to = _graph.Center(node) - pos;
            to.y = 0f;
            return Vector3.Dot(to, _move.Heading) > -2f;
        }

        private int NodeAhead(Vector3 pos)
        {
            int e = _graph.NearestEdge(pos, out _);
            if (e < 0)
                return -1;
            Vector2Int ed = _graph.Edge(e);
            Vector3 toB = _graph.Center(ed.y) - pos;
            toB.y = 0f;
            return Vector3.Dot(toB, _move.Heading) >= 0f ? ed.y : ed.x;
        }

        /// <summary>
        /// Suit l'intersection où il est : en y entrant, il note par quel couloir il est arrivé (celui dont la direction
        /// colle à sa position d'entrée), qu'il n'a pas le droit de reprendre.
        /// </summary>
        private void TrackNodes(Vector3 pos, bool enraged)
        {
            if (_graph == null)
                return;
            int at = _graph.NodeAt(pos);
            if (at == _inNode)
                return;
            if (at >= 0)
            {
                // Premier pas après une remise à zéro (déjà dans l'intersection) : il vient de derrière lui.
                bool fresh = _inNode < 0 && _lastNode < 0;
                ArrivedFrom = AlignedNeighbor(at, fresh ? -_move.Heading : pos - _graph.Center(at));
                GoalNode = -1;
                _nextDecision = 0f;                                      // il choisit sa branche tout de suite
            }
            else
                _lastNode = _inNode;

            _inNode = at;
            _trackEdge = -1;
        }

        private int AlignedNeighbor(int node, Vector3 offset)
        {
            offset.y = 0f;
            int best = -1;
            float bestDot = float.NegativeInfinity;
            foreach (int n in _graph.Neighbors(node))
            {
                Vector3 d = _graph.Center(n) - _graph.Center(node);
                d.y = 0f;
                float dot = Vector3.Dot(d.normalized, offset.sqrMagnitude > 1e-4f ? offset.normalized : -_move.Heading);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = n;
                }
            }

            return best;
        }

        /// <summary>Mesure : sens de marche dans le couloir ; un changement de sens hors cul-de-sac et hors Enragé est un demi-tour.</summary>
        private void CountUTurns(bool enraged)
        {
            if (_graph == null || _inNode >= 0 || _move.Speed < 2f)
                return;
            int e = _graph.NearestEdge(_rb.position, out _);
            if (e < 0)
                return;
            Vector2Int ed = _graph.Edge(e);
            Vector3 axis = _graph.Center(ed.y) - _graph.Center(ed.x);
            axis.y = 0f;
            float dot = Vector3.Dot(_move.Heading, axis.normalized);
            if (Mathf.Abs(dot) < 0.6f)
                return;
            int sign = dot > 0f ? 1 : -1;
            if (e == _trackEdge && sign != _trackSign)
            {
                bool deadEnd = _graph.Neighbors(ed.x).Count <= 1 || _graph.Neighbors(ed.y).Count <= 1;
                if (!deadEnd && !(enraged && settings.allowUTurnWhenEnraged))
                    UTurns++;
            }

            _trackEdge = e;
            _trackSign = sign;
        }

        // ---------- Se baisser (D-035) ----------

        private void UpdateCrouch(Vector3 pos, float dt)
        {
            float full = settings.height;
            float clear = Mathf.Min(Clearance(pos), Clearance(pos + _move.Heading * (Radius + 3f)));
            float wanted = Mathf.Clamp(clear - 0.4f, settings.crouchMinHeight, full) / full;
            _crouch = Mathf.MoveTowards(_crouch, wanted, dt * 1.5f);
            if (Crouching)
                CrouchSeconds += dt;
            ApplyCrouch();
        }

        /// <summary>Hauteur libre au-dessus de lui (m), jusqu'à sa taille debout.</summary>
        private float Clearance(Vector3 feet)
        {
            float full = settings.height;
            int n = Physics.SphereCastNonAlloc(feet + Vector3.up * 1.5f, Radius * 0.7f, Vector3.up, _hits, full, ~0,
                QueryTriggerInteraction.Ignore);
            float best = full + 1f;
            for (int i = 0; i < n; i++)
                if (Solid(_hits[i].collider) && _hits[i].distance > 0f)
                    best = Mathf.Min(best, _hits[i].distance + 1.5f + Radius * 0.7f);
            return best;
        }

        private void ApplyCrouch()
        {
            if (body != null)
                body.localScale = new Vector3(1f, _crouch, 1f);
            if (_capsule != null && settings != null)
            {
                float h = settings.height * _crouch;
                _capsule.height = h;
                _capsule.center = new Vector3(0f, h * 0.5f, 0f);
            }
        }

        // ---------- Portée de la main ----------

        /// <summary>
        /// Peut-il attraper un joueur qui se tient en <paramref name="feet"/> ? Dans une pièce : depuis une ouverture
        /// (roomReach). Ailleurs : depuis un point où il tient debout sur son NavMesh (le plus proche parmi plusieurs
        /// essais), épaule tournée vers le joueur, portée handReach (+ edgeLean s'il ne peut pas se placer dessous),
        /// ligne libre pour le bras et place au-dessus du point pour la main.
        /// </summary>
        public bool CanReach(Vector3 feet, out Vector3 stand, out Transform opening)
        {
            opening = null;
            ReachRoom room = ReachRoom.At(feet);
            if (room != null)
                return StandForRoom(room, feet, out stand, out opening);
            stand = feet;
            if (!OverheadClear(feet))
                return false;
            Vector3 chest = feet + Vector3.up;
            bool found = false;
            float bestD = float.PositiveInfinity;
            // Essais autour du joueur, à sa hauteur et plus bas (joueur sur une corniche, un pont : il se tient au sol dessous).
            for (int k = 0; k < 34; k++)
            {
                int ring = k % 17;
                float radius = ring <= 8 ? 6f : 11f;
                Vector3 probe = ring == 0 ? feet : feet + Quaternion.Euler(0f, ring * 45f + (ring > 8 ? 22.5f : 0f), 0f) * Vector3.forward * radius;
                if (k >= 17)
                    probe += Vector3.down * (settings.shoulderHeight - 1f);
                if (!_move.Snap(probe, ring == 0 ? Radius + 6f : 5f, out Vector3 s))
                    continue;
                float flat = Flat(s - feet);
                if (flat >= bestD)
                    continue;
                Vector3 shoulder = s + Vector3.up * settings.shoulderHeight + FlatDir(feet - s) * (settings.width * 0.4f);
                float reach = settings.handReach + (flat > Radius + 1f ? settings.edgeLean : 0f);
                if ((chest - shoulder).magnitude > reach)
                    continue;
                if (!Clear(shoulder, chest) && !Clear(s + Vector3.up * 2f, chest))
                    continue;
                found = true;
                bestD = flat;
                stand = s;
            }

            return found;
        }

        /// <summary>Point devant l'ouverture de la pièce la plus proche du joueur et à moins de roomReach de lui.</summary>
        private bool StandForRoom(ReachRoom room, Vector3 feet, out Vector3 stand, out Transform opening)
        {
            stand = feet;
            opening = null;
            float best = float.PositiveInfinity;
            foreach (Transform o in room.Openings)
            {
                if (o == null)
                    continue;
                float d = Vector3.Distance(o.position, feet + Vector3.up);
                if (d > settings.roomReach || d >= best)
                    continue;
                Vector3 outside = o.position + FlatDir(o.forward) * (Radius + 1.5f);
                if (!_move.Snap(new Vector3(outside.x, feet.y, outside.z), Radius + 4f, out Vector3 s) || Mathf.Abs(s.y - feet.y) > 4f)
                    continue;
                best = d;
                stand = s;
                opening = o;
            }

            return opening != null;
        }

        private static Vector3 FlatDir(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-4f ? v.normalized : Vector3.forward;
        }

        // ---------- Prise à la main (D-034) ----------

        private void UpdateGrab(float now)
        {
            switch (_grab)
            {
                case GrabPhase.Aucune:
                    if (!OverheadUsesRight)
                        PoseArm(RestHand(), 0f);
                    if (Target != null && Mode != StalkerMode.Repaire && now >= _grabReadyAt
                        && TryAim(Target, settings.grabTelegraph + settings.grabSlam, out Vector3 aim, out Transform opening))
                    {
                        _grab = GrabPhase.Annonce;
                        _grabStart = now;
                        _grabTarget = aim;
                        _grabOpening = opening;
                        _reaimed = false;
                        GrabAttempts++;
                        if (opening != null)
                            RoomGrabs++;
                        LastGrabStart = now;
                    }

                    break;

                case GrabPhase.Annonce:
                    PoseArm(RaisedHand(), 0f);
                    // Visée corrigée : peu avant la descente, il vise de nouveau où sera le joueur.
                    if (!_reaimed && now - _grabStart >= settings.grabTelegraph - settings.grabReaim)
                    {
                        _reaimed = true;
                        if (Target != null && !Target.Dead
                            && TryAim(Target, settings.grabReaim + settings.grabSlam, out Vector3 again, out Transform o2)
                            && o2 == _grabOpening)
                            _grabTarget = again;
                    }

                    if (now - _grabStart >= settings.grabTelegraph)
                    {
                        _grab = GrabPhase.Descente;
                        _grabStart = now;
                        _handFrom = RaisedHand();
                    }

                    break;

                case GrabPhase.Descente:
                {
                    float t = Mathf.Clamp01((now - _grabStart) / settings.grabSlam);
                    PoseArm(Vector3.Lerp(_handFrom, HandAtTarget(), t * t), t);
                    if (t >= 1f)
                    {
                        bool caught = ResolveGrab(now);
                        _holdTime = EnemyMath.GrabHoldTime(caught, settings, out bool planted);
                        Planted = planted;
                        if (planted)
                            MissedGrabs++;
                        _grab = GrabPhase.AuSol;
                        _grabStart = now;
                    }

                    break;
                }

                case GrabPhase.AuSol:
                    PoseArm(HandAtTarget(), 1f);
                    if (now - _grabStart >= _holdTime)
                    {
                        _grab = GrabPhase.Retour;
                        _grabStart = now;
                        _grabReadyAt = now + settings.grabCooldown;
                        Planted = false;
                    }

                    break;

                case GrabPhase.Retour:
                {
                    float t = Mathf.Clamp01((now - _grabStart) / 0.5f);
                    PoseArm(Vector3.Lerp(HandAtTarget(), RestHand(), t), 1f - t);
                    if (t >= 1f)
                    {
                        _grab = GrabPhase.Aucune;
                        _grabOpening = null;
                    }

                    break;
                }
            }

            if (grabMarker != null)
            {
                // Zone visée pendant l'annonce ; main plantée : poussière et lueur qui s'éteint (D-039).
                bool show = _grab == GrabPhase.Annonce || _grab == GrabPhase.Descente || Planted;
                if (grabMarker.gameObject.activeSelf != show)
                    grabMarker.gameObject.SetActive(show);
                if (show)
                {
                    grabMarker.position = _grabTarget + Vector3.up * 0.05f;
                    grabMarker.rotation = HandRotation();
                    float fade = Planted ? 1f - Mathf.Clamp01((now - _grabStart) / Mathf.Max(0.01f, _holdTime)) : 1f;
                    Transform glow = grabMarker.Find("Lueur"), dust = grabMarker.Find("Poussière");
                    if (glow != null)
                        glow.localScale = new Vector3(settings.handSize.x * fade, 0.02f, settings.handSize.z * fade);
                    if (dust != null)
                    {
                        if (dust.gameObject.activeSelf != Planted)
                            dust.gameObject.SetActive(Planted);
                        float r = 1f + 1.2f * (1f - fade);
                        dust.localScale = new Vector3(settings.handSize.x * 1.3f * r, 0.6f * fade + 0.05f, settings.handSize.z * 1.3f * r);
                    }
                }
            }
        }

        /// <summary>
        /// Point visé : où sera le joueur dans <paramref name="lead"/> s, posé sur la surface sous lui ; s'il ne l'atteint pas
        /// là, il vise sa position actuelle. Faux si rien n'est à portée depuis là où il se tient.
        /// </summary>
        private bool TryAim(PlayerHealth target, float lead, out Vector3 aim, out Transform opening)
        {
            Vector3 feet = target.transform.position;
            Vector3 p = EnemyMath.Predict(feet, EnemySenses.VelocityOf(target), lead);
            Vector3 pos = _rb.position;
            for (int k = 0; k < 2; k++)
            {
                Vector3 candidate = k == 0 ? p : feet;
                aim = SurfaceBelow(new Vector3(candidate.x, Mathf.Max(candidate.y, feet.y) + 1.5f, candidate.z), feet.y);
                ReachRoom room = ReachRoom.At(aim + Vector3.up * 0.5f);
                if (room != null)
                {
                    // Par une ouverture : il doit être devant, la main passe à l'horizontale.
                    Transform o = room.NearestOpening(aim);
                    if (o != null && Vector3.Distance(o.position, aim + Vector3.up) <= settings.roomReach
                        && Flat(o.position - pos) <= Radius + 6f)
                    {
                        opening = o;
                        return true;
                    }

                    continue;
                }

                if (!OverheadClear(aim))
                    continue;
                Vector3 shoulder = ShoulderFrom(pos, aim);
                float reach = settings.handReach + settings.edgeLean;
                if ((aim + Vector3.up - shoulder).magnitude > reach)
                    continue;
                if (!Clear(shoulder, aim + Vector3.up) && !Clear(pos + Vector3.up * 2f, aim + Vector3.up))
                    continue;
                opening = null;
                return true;
            }

            aim = feet;
            opening = null;
            return false;
        }

        private Vector3 SurfaceBelow(Vector3 from, float fallbackY)
        {
            int n = Physics.RaycastNonAlloc(from, Vector3.down, _hits, 40f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(_hits, 0, n, Comparer.Instance);
            for (int i = 0; i < n; i++)
                if (Solid(_hits[i].collider))
                    return _hits[i].point;
            return new Vector3(from.x, fallbackY, from.z);
        }

        /// <summary>
        /// Dégagement au-dessus du point visé (handClearance) : sa main descend d'en haut. Dans le labyrinthe, seuls les
        /// plafonds des pièces sont plus bas ; la main y passe par l'ouverture (ReachRoom). Objets, ennemis, joueurs : ignorés.
        /// </summary>
        public bool OverheadClear(Vector3 point)
        {
            float h = settings.handClearance;
            int n = Physics.OverlapBoxNonAlloc(point + Vector3.up * (0.25f + h * 0.5f), new Vector3(0.3f, h * 0.5f - 0.1f, 0.3f), _overlap,
                Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (Solid(_overlap[i]))
                    return false;
            return true;
        }

        /// <summary>Rien de solide (décor) entre a et b : ni le traqueur, ni les joueurs, ni les objets, ni les ennemis ne bloquent.</summary>
        private bool Clear(Vector3 a, Vector3 b)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-3f)
                return true;
            int n = Physics.RaycastNonAlloc(a, d / len, _hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (Solid(_hits[i].collider))
                    return false;
            return true;
        }

        private bool Solid(Collider c) =>
            c != null && !c.transform.IsChildOf(transform) && c.GetComponentInParent<PlayerHealth>() == null
            && c.GetComponentInParent<ItHunts.Orbs.Grabbable>() == null && c.GetComponentInParent<Damageable>() == null;

        /// <summary>La main s'abat : vrai si elle a attrapé quelqu'un.</summary>
        private bool ResolveGrab(float now)
        {
            Vector3 center = HandAtTarget();
            Quaternion rot = HandRotation();
            Vector3 half = HandHalfExtents();
            bool caught = false;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                if (StalkerStrike.Grab(p, center, half, rot, now))
                {
                    Kills++;
                    GrabKills++;
                    caught = true;
                }
            }

            return caught;
        }

        /// <summary>Demi-tailles de la zone de prise : la BoxCollider de la main du modèle (sinon handSize).</summary>
        private Vector3 HandHalfExtents()
        {
            if (handZone != null)
                return Vector3.Scale(handZone.size, handZone.transform.lossyScale) * 0.5f;
            return settings.handSize * 0.5f;
        }

        private Quaternion HandRotation()
        {
            Vector3 f = _grabOpening != null ? -_grabOpening.forward : _grabTarget - Shoulder;
            f.y = 0f;
            return f.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(f.normalized, Vector3.up) : transform.rotation;
        }

        private Vector3 HandAtTarget() => _grabTarget + Vector3.up * HandHalfExtents().y;
        private Vector3 RestHand() => Shoulder + Vector3.down * (ShoulderHeight * 0.45f) + transform.forward * 1.5f;

        private Vector3 RaisedHand()
        {
            if (_grabOpening != null)
                return _grabOpening.position + FlatDir(_grabOpening.forward) * 2f;   // devant l'ouverture, prête à entrer
            Vector3 f = _grabTarget - Shoulder;
            f.y = 0f;
            return Shoulder + Vector3.up * 4f + (f.sqrMagnitude > 1e-4f ? f.normalized : transform.forward) * 2f;
        }

        // ---------- Mains au-dessus de la tête (D-037) ----------

        private bool OverheadUsesRight => _over != OverheadPhase.Aucune && _grab == GrabPhase.Aucune;
        private Vector3 LeftShoulder => _rb.position + Vector3.up * ShoulderHeight - transform.right * (settings.width * 0.4f);
        private Vector3 LeftRestHand() => LeftShoulder + Vector3.down * (ShoulderHeight * 0.45f) + transform.forward * 1.5f;
        private float HeadTop => _rb.position.y + Height;

        /// <summary>Cylindre surveillé : rayon overheadRadius, de overheadMinHeight (baissé compris) à overheadAboveHead au-dessus de la tête.</summary>
        public bool InOverheadZone(Vector3 feet, Vector3 velocity) =>
            EnemyMath.WillPassOver(feet, velocity, 20f, _rb.position, settings.overheadRadius, _rb.position.y + settings.overheadMinHeight * _crouch,
                HeadTop + settings.overheadAboveHead, settings.overheadPrediction);

        private void UpdateOverhead(float now, float dt)
        {
            Vector3 up = Vector3.up * (Height + 2.5f) + _rb.position;
            Vector3 right = transform.right * (settings.overheadHandSpread * 0.5f + settings.width * 0.25f);
            switch (_over)
            {
                case OverheadPhase.Aucune:
                    PoseLimb(leftArm, leftHand, LeftShoulder, LeftRestHand(), transform.rotation);
                    if (Mode == StalkerMode.Repaire || now < _overReadyAt)
                        break;
                    foreach (PlayerHealth p in PlayerHealth.All)
                    {
                        if (p == null || p.Dead || !InOverheadZone(p.transform.position, EnemySenses.VelocityOf(p)))
                            continue;
                        _over = OverheadPhase.Annonce;
                        _overStart = now;
                        _overTarget = p;
                        _overLeft = LeftRestHand();
                        _overRight = hand != null ? hand.position : RestHand();
                        OverheadAttempts++;
                        break;
                    }

                    break;

                case OverheadPhase.Annonce:
                {
                    // Les deux mains montent au-dessus de la tête.
                    float t = Mathf.Clamp01((now - _overStart) / Mathf.Max(0.01f, settings.overheadTelegraph));
                    _overLeft = Vector3.Lerp(_overLeft, up - right, t);
                    _overRight = Vector3.Lerp(_overRight, up + right, t);
                    if (now - _overStart >= settings.overheadTelegraph)
                    {
                        _over = OverheadPhase.Active;
                        _overStart = now;
                    }

                    break;
                }

                case OverheadPhase.Active:
                {
                    // Elles suivent le joueur au-dessus de lui (jamais plus bas que la zone surveillée, à portée des épaules).
                    Vector3 aim = up;
                    if (_overTarget != null && !_overTarget.Dead)
                    {
                        Vector3 feet = _overTarget.transform.position;
                        aim = EnemyMath.Predict(feet, EnemySenses.VelocityOf(_overTarget), 0.1f) + Vector3.up;
                        aim.y = Mathf.Max(aim.y, _rb.position.y + settings.overheadMinHeight * _crouch);
                        Vector3 flat = aim - _rb.position;
                        flat.y = 0f;
                        if (flat.magnitude > settings.overheadRadius)
                            aim = new Vector3(_rb.position.x, aim.y, _rb.position.z) + flat.normalized * settings.overheadRadius;
                    }

                    Vector3 side = Vector3.Cross(Vector3.up, FlatDir(aim - _rb.position)) * (settings.overheadHandSpread * 0.5f);
                    float step = settings.overheadHandSpeed * dt;
                    _overLeft = Vector3.MoveTowards(_overLeft, ClampReach(LeftShoulder, aim - side), step);
                    _overRight = Vector3.MoveTowards(_overRight, ClampReach(Shoulder, aim + side), step);
                    if (now - _overStart >= settings.overheadDuration)
                    {
                        _over = OverheadPhase.Retour;
                        _overStart = now;
                        _overReadyAt = now + settings.overheadCooldown;
                    }

                    break;
                }

                case OverheadPhase.Retour:
                {
                    float t = Mathf.Clamp01((now - _overStart) / 0.3f);
                    _overLeft = Vector3.Lerp(_overLeft, LeftRestHand(), t);
                    _overRight = Vector3.Lerp(_overRight, RestHand(), t);
                    if (t >= 1f)
                    {
                        _over = OverheadPhase.Aucune;
                        _overTarget = null;
                    }

                    break;
                }
            }

            if (_over == OverheadPhase.Aucune)
                return;
            Quaternion rot = Quaternion.LookRotation(FlatDir(_overLeft - _rb.position), Vector3.up);
            PoseLimb(leftArm, leftHand, LeftShoulder, _overLeft, rot);
            if (OverheadUsesRight)
                PoseLimb(arm, hand, Shoulder, _overRight, Quaternion.LookRotation(FlatDir(_overRight - _rb.position), Vector3.up));
        }

        private Vector3 ClampReach(Vector3 shoulder, Vector3 point)
        {
            Vector3 d = point - shoulder;
            return d.magnitude <= settings.handReach ? point : shoulder + d.normalized * settings.handReach;
        }

        /// <summary>Le joueur (capsule) touche-t-il un bras ou une main (boîtes orientées du modèle) ?</summary>
        public bool TouchesLimb(Vector3 feet, float radius, float height)
        {
            Vector3 half = HandHalfExtents();
            if (hand != null && EnemyMath.CapsuleIntersectsBox(feet, radius, height, hand.position, half, hand.rotation))
                return true;
            if (leftHand != null && EnemyMath.CapsuleIntersectsBox(feet, radius, height, leftHand.position, half, leftHand.rotation))
                return true;
            return TouchesArm(arm, feet, radius, height) || TouchesArm(leftArm, feet, radius, height);
        }

        private bool TouchesArm(Transform a, Vector3 feet, float radius, float height)
        {
            if (a == null)
                return false;
            Vector3 half = new Vector3(settings.armThickness, settings.armThickness, a.localScale.z) * 0.5f;
            return EnemyMath.CapsuleIntersectsBox(feet, radius, height, a.position, half, a.rotation);
        }

        /// <summary>Pose un bras (boîte de l'épaule à la main) et sa main.</summary>
        private void PoseLimb(Transform armT, Transform handT, Vector3 shoulder, Vector3 handCenter, Quaternion handRot)
        {
            if (handT != null)
                handT.SetPositionAndRotation(handCenter, handRot);
            if (armT == null)
                return;
            Vector3 d = handCenter - shoulder;
            float len = d.magnitude;
            if (len < 0.01f)
                return;
            armT.SetPositionAndRotation(shoulder + d * 0.5f, Quaternion.LookRotation(d / len, Vector3.up));
            armT.localScale = new Vector3(settings.armThickness, settings.armThickness, len);
        }

        /// <summary>Pose le bras (boîte de l'épaule à la main) et la main.</summary>
        private void PoseArm(Vector3 handCenter, float closed)
        {
            if (hand != null)
            {
                hand.position = handCenter;
                hand.rotation = _grab == GrabPhase.Aucune ? transform.rotation : HandRotation();
            }

            if (arm == null)
                return;
            Vector3 s = Shoulder;
            Vector3 d = handCenter - s;
            float len = d.magnitude;
            if (len < 0.01f)
                return;
            arm.position = s + d * 0.5f;
            arm.rotation = Quaternion.LookRotation(d / len, Vector3.up);
            Vector3 scale = arm.localScale;
            arm.localScale = new Vector3(scale.x, scale.y, len);
        }

        private sealed class Comparer : IComparer<RaycastHit>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }

        private static float Flat(Vector3 v) => new Vector2(v.x, v.z).magnitude;

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

        private void UpdateMarker()
        {
            if (predictedMarker == null)
                return;
            bool show = ShowDebugMarker && (Mode == StalkerMode.Chasse || Mode == StalkerMode.Enrage);
            if (predictedMarker.gameObject.activeSelf != show)
                predictedMarker.gameObject.SetActive(show);
            if (show)
                predictedMarker.position = Predicted;
        }
    }
}
