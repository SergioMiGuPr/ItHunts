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
        [Tooltip("Zone rouge au sol du revers (D-039, piste a), cachée sinon.")]
        [SerializeField] private Renderer backhandZone;

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

        /// <summary>Revers de la main gauche pendant la main plantée (D-039, piste a).</summary>
        public enum BackhandPhase : byte
        {
            Aucune,
            Annonce,
            Coup,
        }

        private BackhandPhase _back;
        private float _backStart;
        private bool _backUsed;
        private bool _wasPlanted;
        private float _plantedSince;
        private Vector3 _backDir = Vector3.left;
        public BackhandPhase Backhand => _back;
        public int BackhandAttempts { get; private set; }
        public int BackhandKills { get; private set; }

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
        private float ShoulderHeight => ShoulderOffset.y * _crouch;

        /// <summary>
        /// Épaule droite dans le repère du traqueur (x : côté, y : hauteur, z : devant), lue au départ sur l'os Bras.R du modèle
        /// gréé (SK_Traqueur, pose de repos : 2.6 m du centre, 11.7 m, 4 m devant le pivot) ; réglages en repli sans modèle.
        /// </summary>
        public Vector3 ShoulderOffset => _shoulderOffset.sqrMagnitude > 0.01f ? _shoulderOffset
            : settings != null ? new Vector3(settings.shoulderSide, settings.shoulderHeight, 0f) : new Vector3(2.6f, 11.7f, 4f);
        private Vector3 _shoulderOffset;

        /// <summary>Épaule pour la portée : il pivote le buste vers le point visé. Portée = handReach depuis ce point.</summary>
        public Vector3 ReachShoulder(Vector3 target) => ShoulderFrom(_rb != null ? _rb.position : transform.position, target);

        private Vector3 ShoulderFrom(Vector3 feet, Vector3 target)
        {
            Vector3 flat = target - feet;
            flat.y = 0f;
            Vector3 dir = flat.sqrMagnitude > 0.01f ? flat.normalized : transform.forward;
            Vector3 o = ShoulderOffset;
            return feet + Vector3.up * (o.y * _crouch) + dir * o.z + Vector3.Cross(Vector3.up, dir) * o.x;
        }

        /// <summary>Épaule du bras qui attrape (droite) : l'os du modèle s'il est gréé, sinon le réglage.</summary>
        public Vector3 Shoulder => _boneR[0] != null ? _boneR[0].position
            : _rb.position + transform.rotation * Vector3.Scale(ShoulderOffset, new Vector3(1f, _crouch, 1f));

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
            // Pupilles du modèle de la DA (rouge sombre) : Damageable écrit l'émission de tous ses rendus à chaque image ; les bras et
            // les mains (M_Traqueur_Mains : griffes) ont leur propre lueur ivoire (UpdateClawGlow).
            var dmg = GetComponent<ItHunts.Combat.Damageable>();
            _dmg = dmg;
            SetupModel();
            if (dmg != null && settings != null)
            {
                dmg.ExtraGlow = settings.pupilGlow * settings.pupilGlowIntensity;
                _claw = settings.clawGlowRest;
                UpdateClawGlow(0f);
            }
            // Mains de la DA (boîtes unité) : la taille réglée est celle de la main visible, qui est aussi la zone qui tue.
            if (settings != null)
            {
                if (hand != null)
                    hand.localScale = settings.handSize;
                if (leftHand != null)
                    leftHand.localScale = settings.handSize;
            }
            _nav = FindFirstObjectByType<ZoneNavMesh>();
            _zone = FindFirstObjectByType<ZoneState>();
            _graph = FindFirstObjectByType<MazeGraph>();
            if (_nav != null)
                _move.AgentType = _nav.StalkerAgent;
        }

        private float _staggerUntil;

        /// <summary>Poses de la vitrine (Test_Vitrine).</summary>
        public enum ShowcasePose : byte
        {
            MarcheVoutee,
            Prise,
            MainPlantee,
            MainsLevees,
            Revers,
            Baisse,
            Repos,
        }

        private bool _showcasing;
        private float _showPhase;

        private void OnEnable() => _showcasing = false;

        /// <summary>Vitrine (IA coupée : LateUpdate ne tourne pas) : griffes et bras vers leurs zones, après l'Animator.</summary>
        public void ShowcaseLate()
        {
            UpdateClawGlow(Time.deltaTime);
            if (_anim != null)
                ApplyArmIK(Time.deltaTime);
        }

        /// <summary>
        /// Vitrine : pose sans l'IA (composant désactivé), phase de 0 à 1 en boucle : marche voûtée (bras qui balancent), prise
        /// (la main s'abat sur la colonne rouge), main plantée, mains levées, revers (main gauche tendue, zone au sol), baissé (sous
        /// un pont). Affichage seulement.
        /// </summary>
        public void ShowPose(ShowcasePose pose, float phase)
        {
            if (settings == null || _rb == null)
                return;
            _showcasing = true;
            if (_anim != null)
            {
                string clip = pose switch
                {
                    ShowcasePose.MarcheVoutee => "Traqueur_Marche",
                    ShowcasePose.Prise => "Traqueur_Prise",
                    ShowcasePose.MainPlantee => "Traqueur_MainPlantee",
                    ShowcasePose.MainsLevees => "Traqueur_MainsLevees",
                    ShowcasePose.Revers => "Traqueur_Revers",
                    ShowcasePose.Baisse => "Traqueur_PassageBas",
                    _ => "Traqueur_Repos",
                };
                Play(clip, 0.1f);
                if (phase < _showPhase && (pose == ShowcasePose.Prise || pose == ShowcasePose.MainPlantee || pose == ShowcasePose.Revers || pose == ShowcasePose.MainsLevees))
                    _anim.Play(clip, 0, 0f);                                       // un geste : rejoué à chaque boucle
                _showPhase = phase;
                _wantIkR = pose == ShowcasePose.MainPlantee || pose == ShowcasePose.MainsLevees || (pose == ShowcasePose.Prise && phase > 0.2f);
                _wantIkL = pose == ShowcasePose.MainsLevees || pose == ShowcasePose.Revers;
            }

            _crouch = pose == ShowcasePose.Baisse ? 0.64f : 1f;
            ApplyCrouch();
            _grab = GrabPhase.Aucune;
            Vector3 up = Vector3.up, fwd = transform.forward;
            Vector3 ground = _rb.position + fwd * 12f;
            Vector3 handOnGround = ground + up * HandHalfExtents().y;
            bool leftDone = false;
            float swing = Mathf.Sin(phase * Mathf.PI * 2f);
            switch (pose)
            {
                case ShowcasePose.MarcheVoutee:
                    PoseArm(RestHand() + fwd * (1.5f * swing), 0f);
                    PoseLimb(leftArm, leftHand, LeftShoulder, LeftRestHand() - fwd * (1.5f * swing), transform.rotation);
                    leftDone = true;
                    break;
                case ShowcasePose.Prise:
                {
                    float k = Mathf.Clamp01(phase * 1.6f);
                    Vector3 raised = Shoulder + up * 4f + fwd * 2f;
                    PoseArm(Vector3.Lerp(raised, handOnGround, k * k), k);
                    _grab = k < 1f ? GrabPhase.Descente : GrabPhase.Aucune;
                    if (grabMarker != null)
                    {
                        grabMarker.gameObject.SetActive(true);
                        grabMarker.position = ground + up * 0.05f;
                        GrabColumn(1f - k * 0.5f);
                    }

                    break;
                }
                case ShowcasePose.MainPlantee:
                    PoseArm(handOnGround, 1f);
                    _grab = GrabPhase.Descente;                               // griffes vives : la main est au sol
                    break;
                case ShowcasePose.MainsLevees:
                    PoseArm(Shoulder + up * (4f + 0.5f * swing) + fwd * 2f, 0f);
                    PoseLimb(leftArm, leftHand, LeftShoulder, LeftShoulder + up * (4f - 0.5f * swing) + fwd * 2f, transform.rotation);
                    leftDone = true;
                    break;
                case ShowcasePose.Revers:
                {
                    PoseArm(RestHand(), 0f);
                    Vector3 dir = Quaternion.AngleAxis(-120f * Mathf.Clamp01(phase * 1.5f), up) * transform.right;
                    PoseLimb(leftArm, leftHand, LeftShoulder, _rb.position + dir * (Radius + settings.backhandReach * 0.6f) + up * 1.5f,
                        Quaternion.LookRotation(dir, up));
                    leftDone = true;
                    if (backhandZone != null)
                    {
                        backhandZone.enabled = true;
                        float d = 2f * (Radius + settings.backhandReach);
                        Transform z = backhandZone.transform;
                        z.position = _rb.position + up * 0.05f;
                        Vector3 ps = z.parent != null ? z.parent.lossyScale : Vector3.one;
                        z.localScale = new Vector3(d / Mathf.Max(0.01f, ps.x), 0.01f, d / Mathf.Max(0.01f, ps.z));
                    }

                    break;
                }
                default:
                    PoseArm(RestHand(), 0f);
                    break;
            }

            if (!leftDone)
                PoseLimb(leftArm, leftHand, LeftShoulder, LeftRestHand(), transform.rotation);
            if (pose != ShowcasePose.Revers && backhandZone != null)
                backhandZone.enabled = false;
            if (pose != ShowcasePose.Prise && grabMarker != null && grabMarker.gameObject.activeSelf)
                grabMarker.gameObject.SetActive(false);
            UpdateClawGlow(Time.deltaTime);
        }

        private ItHunts.Combat.Damageable _dmg;
        private List<Renderer> _limbRenderers;
        private float _claw;

        /// <summary>Intensité courante de la lueur des griffes (retours du lot, point 3).</summary>
        public float ClawGlow => _claw;

        private void LateUpdate()
        {
            UpdateClawGlow(Time.deltaTime);
            if (_anim == null)
                return;
            Animate();
            ApplyArmIK(Time.deltaTime);
        }

        // ---------- Modèle gréé (SK_Traqueur, PR #6) ----------

        private Animator _anim;
        private string _clip;
        private readonly Transform[] _boneR = new Transform[3], _boneL = new Transform[3];   // Bras, AvantBras, Main
        private Vector3[] _restR, _restL;                                                     // positions locales (étirement)
        private float _ikR, _ikL;
        private bool _wantIkR, _wantIkL;
        private Renderer _bodyRenderer;

        /// <summary>Clip joué (tests, vitrine) ; null sans modèle gréé.</summary>
        public string Clip => _clip;
        public bool Animated => _anim != null;

        /// <summary>Le modèle gréé : Animator, os des bras, épaule lue sur Bras.R ; les bras et mains en boîtes ne sont plus dessinés.</summary>
        private void SetupModel()
        {
            _anim = body != null ? body.GetComponentInChildren<Animator>(true) : null;
            if (_anim == null)
                return;
            string[] names = { "Bras", "AvantBras", "Main" };
            Transform[] all = _anim.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < 3; i++)
            {
                _boneR[i] = System.Array.Find(all, t => t.name == names[i] + ".R");
                _boneL[i] = System.Array.Find(all, t => t.name == names[i] + ".L");
            }

            if (_boneR[0] == null || _boneR[1] == null || _boneR[2] == null)
            {
                _anim = null;
                return;
            }

            _restR = new[] { _boneR[0].localPosition, _boneR[1].localPosition, _boneR[2].localPosition };
            if (_boneL[2] != null)
                _restL = new[] { _boneL[0].localPosition, _boneL[1].localPosition, _boneL[2].localPosition };
            _anim.Update(0f);
            Vector3 local = transform.InverseTransformPoint(_boneR[0].position);
            _shoulderOffset = new Vector3(Mathf.Abs(local.x), local.y, local.z);
            ItHunts.Level.ReachCheck.ShoulderHeight = _shoulderOffset.y;
            ItHunts.Level.ReachCheck.ShoulderSide = _shoulderOffset.x;
            ItHunts.Level.ReachCheck.ShoulderForward = _shoulderOffset.z;
            // Bras et mains en boîtes : zones qui tuent, invisibles (le modèle gréé les dessine). forceRenderingOff et pas enabled :
            // Damageable rallume ses rendus (enabled) quand il se remet en vie, ce qui affichait les anciennes mains en double.
            foreach (Transform t in new[] { arm, hand, leftArm, leftHand })
                if (t != null && t.TryGetComponent(out Renderer piece))
                    piece.forceRenderingOff = true;
            foreach (Renderer r in _anim.GetComponentsInChildren<Renderer>(true))
                if (r.sharedMaterials.Length > 1)
                    _bodyRenderer = r;
            SetupSelfLight();
        }

        private Light _selfLight;
        /// <summary>Lumière d'appoint du traqueur (tests, réglages en Play Mode).</summary>
        public Light SelfLight => _selfLight;

        /// <summary>
        /// Lisibilité dans le noir : une lumière qui n'éclaire que lui (calque de rendu réservé, ses rendus y sont ajoutés). Son
        /// corps gréé (texture sombre, 14 m de haut, loin des torches) sortait noir ; on ne le voyait plus que par ses contours.
        /// </summary>
        private void SetupSelfLight()
        {
            if (settings == null || !settings.selfLight || _anim == null)
                return;
            uint bit = 1u << settings.selfRenderingLayer;
            foreach (Renderer r in _anim.GetComponentsInChildren<Renderer>(true))
                r.renderingLayerMask |= bit;
            var go = new GameObject("Lumière d'appoint (traqueur seul)");
            go.transform.SetParent(body != null ? body : transform, false);
            _selfLight = go.AddComponent<Light>();
            _selfLight.type = LightType.Point;
            _selfLight.shadows = LightShadows.None;
            _selfLight.renderingLayerMask = (int)bit;
            var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalLightData>();
            data.renderingLayers = bit;
            ApplySelfLight();
        }

        private void ApplySelfLight()
        {
            if (_selfLight == null)
                return;
            _selfLight.enabled = settings.selfLight;
            _selfLight.color = settings.selfLightColor;
            _selfLight.intensity = settings.selfLightIntensity;
            _selfLight.range = settings.selfLightRange;
            _selfLight.transform.localPosition = settings.selfLightOffset;
        }

        private void Play(string clip, float fade = 0.15f, float speed = 1f)
        {
            if (_anim == null || !_anim.isActiveAndEnabled)
                return;
            _anim.speed = speed;
            if (_clip == clip)
                return;
            _clip = clip;
            _anim.CrossFadeInFixedTime(clip, fade, 0);
        }

        /// <summary>Clip selon l'état : prise (0.5 s), main plantée, mains levées, revers, passage baissé, marche, repos.</summary>
        private void Animate()
        {
            if (_showcasing)
                return;
            float v = _rb != null ? new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.z).magnitude : 0f;
            v = Mathf.Max(v, WantedSpeed);
            _wantIkR = _grab != GrabPhase.Aucune || Planted || OverheadUsesRight;
            _wantIkL = _over != OverheadPhase.Aucune || _back != BackhandPhase.Aucune;
            if (_back != BackhandPhase.Aucune)
                Play("Traqueur_Revers", 0.06f);
            else if (_over != OverheadPhase.Aucune)
                Play("Traqueur_MainsLevees", 0.06f);
            else if (Planted || _grab == GrabPhase.Descente)
                Play("Traqueur_MainPlantee", 0.06f);
            else if (_grab == GrabPhase.Annonce)
                Play("Traqueur_Prise", 0.05f, 0.5f / Mathf.Max(0.05f, settings.grabTelegraph));
            else if (Crouching)
                Play("Traqueur_PassageBas", 0.25f, Mathf.Clamp(v / Mathf.Max(1f, settings.chaseSpeed), 0.4f, 1.6f));
            else if (v > 0.5f)
                Play("Traqueur_Marche", 0.25f, Mathf.Clamp(v / Mathf.Max(1f, settings.chaseSpeed), 0.4f, 2f));
            else
                Play("Traqueur_Repos", 0.3f);
        }

        /// <summary>
        /// Bras vers la cible (après l'Animator) : épaule (Bras), coude (AvantBras), poignet (Main) orientés pour que la main tombe
        /// sur la zone qui tue posée par le code (hand, leftHand) ; au-delà de la longueur du bras, le bras s'étire (la portée de
        /// 15 m ne change pas). Fondu de 0.15 s à l'entrée et à la sortie.
        /// </summary>
        private void ApplyArmIK(float dt)
        {
            float rate = dt / 0.15f;
            _ikR = Mathf.MoveTowards(_ikR, _wantIkR ? 1f : 0f, rate);
            _ikL = Mathf.MoveTowards(_ikL, _wantIkL ? 1f : 0f, rate);
            Restore(_boneR, _restR);
            Restore(_boneL, _restL);
            if (_ikR > 0.001f && hand != null)
                SolveArm(_boneR, _restR, hand.position, _ikR, 1f);
            if (_ikL > 0.001f && leftHand != null && _boneL[2] != null)
                SolveArm(_boneL, _restL, leftHand.position, _ikL, -1f);
        }

        private static void Restore(Transform[] b, Vector3[] rest)
        {
            if (rest == null)
                return;
            for (int i = 1; i < 3; i++)
                if (b[i] != null)
                    b[i].localPosition = rest[i];
        }

        private void SolveArm(Transform[] b, Vector3[] rest, Vector3 handCenter, float weight, float side)
        {
            Transform a = b[0], m = b[1], e = b[2];
            Vector3 ap = a.position;
            float l1 = Vector3.Distance(ap, m.position), l2 = Vector3.Distance(m.position, e.position);
            Vector3 aim = handCenter - ap;
            float half = settings.handSize.z * 0.5f;                       // le poignet reste à une demi-main du centre
            Vector3 target = handCenter - aim.normalized * half;
            float d = Vector3.Distance(ap, target);
            Quaternion ra = a.rotation, rm = m.rotation, re = e.rotation;
            if (d > l1 + l2 - 0.01f)
            {
                // Étirement : les deux segments s'allongent ensemble, le bras droit vers la cible.
                float k = d / Mathf.Max(0.01f, l1 + l2);
                m.localPosition = rest[1] * Mathf.Lerp(1f, k, weight);
                e.localPosition = rest[2] * Mathf.Lerp(1f, k, weight);
                a.rotation = Quaternion.FromToRotation(e.position - ap, target - ap) * a.rotation;
                m.rotation = Quaternion.FromToRotation(e.position - m.position, target - m.position) * m.rotation;
            }
            else
            {
                Vector3 dir = (target - ap) / Mathf.Max(0.01f, d);
                Vector3 hint = ap + Vector3.down * 3f + transform.right * (side * 3f) - transform.forward * 1f;   // coude vers le bas et l'extérieur
                Vector3 n = Vector3.Cross(dir, hint - ap);
                if (n.sqrMagnitude < 1e-4f)
                    n = transform.right;
                n.Normalize();
                float cosA = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
                float ang = Mathf.Acos(cosA) * Mathf.Rad2Deg;
                Vector3 e1 = Quaternion.AngleAxis(ang, n) * dir, e2 = Quaternion.AngleAxis(-ang, n) * dir;
                Vector3 elbow = ap + ((Vector3.Dot(e1, hint - ap) > Vector3.Dot(e2, hint - ap)) ? e1 : e2) * l1;
                a.rotation = Quaternion.FromToRotation(m.position - ap, elbow - ap) * a.rotation;
                m.rotation = Quaternion.FromToRotation(e.position - m.position, target - m.position) * m.rotation;
            }

            // Poignet : la main prolonge le bras, vers le centre de la zone.
            Transform tip = e.childCount > 0 ? e.GetChild(0) : null;
            if (tip != null)
                e.rotation = Quaternion.FromToRotation(tip.position - e.position, handCenter - e.position) * e.rotation;
            if (weight < 0.999f)
            {
                a.rotation = Quaternion.Slerp(ra, a.rotation, weight);
                m.rotation = Quaternion.Slerp(rm, m.rotation, weight);
                e.rotation = Quaternion.Slerp(re, e.rotation, weight);
            }
        }

        /// <summary>
        /// Griffes ivoire (masque de M_Traqueur_Mains) : faibles au repos, plus intenses pendant la prise (annonce, descente, main au
        /// sol). Jamais de rouge sur les mains : le signal de danger reste la colonne rouge au sol.
        /// </summary>
        private void UpdateClawGlow(float dt)
        {
            if (settings == null || _dmg == null)
                return;
            ApplySelfLight();                                           // réglable en Play Mode
            if (_limbRenderers == null)
            {
                _limbRenderers = new List<Renderer>();
                foreach (Transform t in new[] { arm, hand, leftArm, leftHand })
                    if (t != null && t.GetComponent<Renderer>() != null)
                        _limbRenderers.Add(t.GetComponent<Renderer>());
            }

            float target = _grab != GrabPhase.Aucune || Planted ? settings.clawGlowGrab : settings.clawGlowRest;
            _claw = dt <= 0f ? target : Mathf.Lerp(_claw, target, 1f - Mathf.Exp(-settings.clawGlowRate * dt));
            foreach (Renderer r in _limbRenderers)
                _dmg.GlowOverrides[r] = settings.clawGlow * _claw;
            // Modèle gréé : un rendu, deux matériaux (M_Traqueur_Mains : griffes ; M_Traqueur : pupilles) ; mèches sans lueur.
            if (_anim != null)
            {
                foreach (Renderer r in _anim.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        string n = mats[i] != null ? mats[i].name : "";
                        Color c = n.Contains("Mains") ? settings.clawGlow * _claw
                            : n.Contains("Cheveux") ? Color.black
                            : settings.pupilGlow * settings.pupilGlowIntensity;
                        _dmg.SetMaterialGlow(r, i, c);
                    }
                }
            }
        }

        /// <summary>Pression du combat de boss : recharges de la prise, des mains levées et de la pointe × ce facteur (1 hors combat).</summary>
        public float CooldownScale { get; set; } = 1f;

        /// <summary>Sonné par un piège du combat de boss (D-049) : il ne bouge ni n'attaque pendant ce temps.</summary>
        public bool Staggered => Time.time < _staggerUntil;

        public void Stagger(float seconds) => _staggerUntil = Mathf.Max(_staggerUntil, Time.time + seconds);

        /// <summary>Le pose ailleurs (entrée de l'arène du combat de boss) : sans prise ni attaque en cours, sans chemin.</summary>
        public void WarpTo(Vector3 feet, Quaternion rotation)
        {
            transform.SetPositionAndRotation(feet, rotation);
            _rb.position = feet;
            _move.Clear();
            _move.Heading = rotation * Vector3.forward;
            _grab = GrabPhase.Aucune;
            Planted = false;
            _grabOpening = null;
            _over = OverheadPhase.Aucune;
            _back = BackhandPhase.Aucune;
            _inNode = _lastNode = -1;
            GoalNode = ArrivedFrom = -1;
            _trackEdge = -1;
            _hasDest = false;
            _retargetAt = 0f;
            _nextDecision = 0f;
            if (grabMarker != null)
                grabMarker.gameObject.SetActive(false);
            PoseArm(RestHand(), 0f);
        }

        /// <summary>Remise à zéro de la zone : au repaire, sans cible, pointe rechargée.</summary>
        public void ResetState()
        {
            _staggerUntil = 0f;
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
            _back = BackhandPhase.Aucune;
            _backUsed = false;
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
            // NavMesh reconstruit après son Start (carte régénérée) : nouvel identifiant d'agent, comme les goules.
            if (_nav == null)
                _nav = FindFirstObjectByType<ZoneNavMesh>();
            if (_nav != null && _nav.Built && _move.AgentType != _nav.StalkerAgent)
            {
                _move.AgentType = _nav.StalkerAgent;
                _move.Clear();
            }

            bool grace = _zone != null && _zone.InGrace;
            bool enraged = _zone != null && _zone.Enraged;
            Vector3 pos = _rb.position;
            UpdateCrouch(pos, dt);
            if (Staggered)
            {
                WantedSpeed = 0f;
                return;                                                  // sonné par un piège (D-049)
            }

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

            EnemyMath.UpdateBurst(ref _burst, SeesTarget && !Planted, now, settings.burstDuration, settings.burstCooldown * CooldownScale);
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
            WantedSpeed *= EnemyMath.GrabMoveFactor(_grab != GrabPhase.Aucune, Planted, settings);   // main plantée : il continue (piste b)
            if (_hasDest && Flat(_dest - pos) < 1f)
                WantedSpeed = 0f;
            MoveTo(_move.Step(pos, WantedSpeed, settings.acceleration, settings.turnRate * (Planted ? settings.plantedTurnFactor : 1f), dt));
            CountUTurns(enraged);
            UpdateGrab(now);
            UpdateOverhead(now, dt);
            UpdateBackhand(now);

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
                body.localScale = new Vector3(1f, _anim != null ? 1f : _crouch, 1f);   // gréé : le clip Traqueur_PassageBas se baisse
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
                    probe += Vector3.down * (ShoulderOffset.y - 1f);
                if (!_move.Snap(probe, ring == 0 ? Radius + 6f : 5f, out Vector3 s))
                    continue;
                float flat = Flat(s - feet);
                if (flat >= bestD)
                    continue;
                Vector3 fd = FlatDir(feet - s);
                Vector3 shoulder = s + Vector3.up * ShoulderOffset.y + fd * ShoulderOffset.z + Vector3.Cross(Vector3.up, fd) * ShoulderOffset.x;
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
                        _grabReadyAt = now + settings.grabCooldown * CooldownScale;
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
                    GrabColumn(Planted ? 1f - Mathf.Clamp01((now - _grabStart) / Mathf.Max(0.01f, _holdTime)) : 1f);
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
            && c.GetComponentInParent<ItHunts.Orbs.Grabbable>() == null && c.GetComponentInParent<Damageable>() == null
            && c.GetComponentInParent<StalkerPassesThrough>() == null;

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
        private Vector3 LeftShoulder => _boneL[0] != null ? _boneL[0].position
            : _rb.position + transform.rotation * Vector3.Scale(ShoulderOffset, new Vector3(-1f, _crouch, 1f));
        private Vector3 LeftRestHand() => LeftShoulder + Vector3.down * (ShoulderHeight * 0.45f) + transform.forward * 1.5f;
        private float HeadTop => _rb.position.y + Height;

        /// <summary>
        /// Revers (D-039, piste a) : pendant la main plantée, un joueur à portée (backhandReach du bord du corps) déclenche une fois
        /// une annonce courte (zone rouge au sol, main gauche tendue vers lui), puis un coup mortel dans toute la zone.
        /// </summary>
        private void UpdateBackhand(float now)
        {
            if (Planted && !_wasPlanted)
                _plantedSince = now;
            _wasPlanted = Planted;
            if (!Planted && _back == BackhandPhase.Aucune)
                _backUsed = false;
            if (!settings.backhandEnabled)
                _back = BackhandPhase.Aucune;
            switch (_back)
            {
                case BackhandPhase.Aucune:
                    if (!settings.backhandEnabled || !Planted || _backUsed || now - _plantedSince < settings.backhandDelay)
                        break;
                    foreach (PlayerHealth p in PlayerHealth.All)
                    {
                        if (p == null || p.Dead || !EnemyMath.InBackhandZone(_rb.position, Radius, p.transform.position, settings))
                            continue;
                        _back = BackhandPhase.Annonce;
                        _backStart = now;
                        _backUsed = true;
                        _backDir = FlatDir(p.transform.position - _rb.position);
                        BackhandAttempts++;
                        break;
                    }

                    break;

                case BackhandPhase.Annonce:
                    if (now - _backStart >= settings.backhandTelegraph)
                    {
                        _back = BackhandPhase.Coup;
                        _backStart = now;
                    }

                    break;

                case BackhandPhase.Coup:
                    foreach (PlayerHealth p in PlayerHealth.All)
                    {
                        if (p == null || p.Dead || !EnemyMath.InBackhandZone(_rb.position, Radius, p.transform.position, settings))
                            continue;
                        if (StalkerStrike.Hit(p, now))
                        {
                            Kills++;
                            BackhandKills++;
                        }
                    }

                    if (now - _backStart >= settings.backhandSweep)
                        _back = BackhandPhase.Aucune;
                    break;
            }

            if (backhandZone != null)
            {
                bool show = _back != BackhandPhase.Aucune;
                if (backhandZone.enabled != show)
                    backhandZone.enabled = show;
                if (show)
                {
                    float d = 2f * (Radius + settings.backhandReach);
                    Transform z = backhandZone.transform;
                    z.position = _rb.position + Vector3.up * 0.05f;
                    Vector3 parent = z.parent != null ? z.parent.lossyScale : Vector3.one;
                    z.localScale = new Vector3(d / Mathf.Max(0.01f, parent.x), _back == BackhandPhase.Coup ? 0.03f : 0.01f, d / Mathf.Max(0.01f, parent.z));
                }
            }

            // Main gauche : tendue vers le côté visé (annonce), puis balayée devant lui (coup) ; le geste au-dessus de la tête l'emporte.
            if (_back == BackhandPhase.Coup && _over == OverheadPhase.Aucune)
            {
                float sweep = Mathf.Clamp01((now - _backStart) / settings.backhandSweep);
                Vector3 dir = Quaternion.AngleAxis(-120f * sweep, Vector3.up) * _backDir;
                Vector3 target = _rb.position + Vector3.up * 1.5f + dir * (Radius + settings.backhandReach * 0.6f);
                PoseLimb(leftArm, leftHand, LeftShoulder, ClampReach(LeftShoulder, target), Quaternion.LookRotation(dir, Vector3.up));
            }
        }

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
                        _overReadyAt = now + settings.overheadCooldown * CooldownScale;
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

        private Renderer _column;
        private MaterialPropertyBlock _columnBlock;

        /// <summary>
        /// Colonne de lumière rouge au-dessus de la zone de prise (bloc 4) : un disque au sol vu de loin devient une ligne ; la colonne
        /// se lit à 50 m. Hauteur, intensité et couleur dans StalkerSettings. Créée à la première annonce, enfant du repère.
        /// </summary>
        private void GrabColumn(float fade)
        {
            if (settings == null || !settings.grabColumn || settings.grabColumnMaterial == null || grabMarker == null)
                return;
            if (_column == null)
            {
                GameObject c = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                c.name = "Colonne de lumière (zone de prise)";
                Destroy(c.GetComponent<Collider>());
                c.transform.SetParent(grabMarker, false);
                _column = c.GetComponent<Renderer>();
                _column.sharedMaterial = settings.grabColumnMaterial;
                _column.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _column.receiveShadows = false;
                _columnBlock = new MaterialPropertyBlock();
            }

            float d = settings.grabColumnDiameter;
            Vector3 ls = grabMarker.lossyScale;
            _column.transform.localScale = new Vector3(d / Mathf.Max(0.01f, ls.x), settings.grabColumnHeight * 0.5f / Mathf.Max(0.01f, ls.y), d / Mathf.Max(0.01f, ls.z));
            _column.transform.localPosition = new Vector3(0f, settings.grabColumnHeight * 0.5f / Mathf.Max(0.01f, ls.y), 0f);
            _column.transform.rotation = Quaternion.identity;
            _columnBlock.SetColor("_BaseColor", settings.grabColumnColor * (settings.grabColumnIntensity * fade));
            _column.SetPropertyBlock(_columnBlock);
        }

        private void OnDisable()
        {
            // Zone de prise et sa colonne : jamais allumées sans traqueur actif (fin de zone, remise à zéro).
            if (grabMarker != null && grabMarker.gameObject.activeSelf)
                grabMarker.gameObject.SetActive(false);
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
