using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Objet que la télékinésie peut attraper et lancer (caisse, tonneau, lustre…). Corps physique dynamique ; sa
    /// masse est celle du Rigidbody. Un objet « accroché » (lustre) reste figé jusqu'à ce qu'on l'attrape. Il brille
    /// (émission) selon ce que le joueur peut en faire : la couleur est choisie par TelekinesisView.
    /// Registre des objets de la scène (pas un singleton de joueur : ce sont des objets du monde).
    /// Durabilité (D-027) : il tient un nombre de lancers selon sa masse (ou imposé ici), puis se casse à l'impact
    /// (débris, éclat, son) ; dans la scène de test, il réapparaît à sa place après respawnAfterBreak.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Grabbable : MonoBehaviour
    {
        [Tooltip("Figé (cinématique) jusqu'à ce qu'on l'attrape : objet suspendu ou accroché.")]
        [SerializeField] private bool startsAnchored;
        [Tooltip("Lueur sur tout le modèle au lieu du masque d'émission de son matériau (repli ; SM_Caisse : la rune n'est que sur " +
                 "la face avant, invisible des autres côtés).")]
        [SerializeField] private bool glowWholeModel;

        [Header("Durabilité et zone (0 : selon la masse, DamageSettings)")]
        [Tooltip("Nombre de lancers tenus avant de se casser (0 : selon la masse).")]
        [SerializeField, Min(0)] private int durabilityOverride;
        [Tooltip("Se casse au premier impact au-dessus du seuil de chute, lancé ou tombé (lustre).")]
        [SerializeField] private bool breaksOnAnyImpact;
        [Tooltip("Rayon de la zone de dégâts à l'impact (m ; 0 : selon la masse).")]
        [SerializeField, Min(0f)] private float splashRadiusOverride;
        [Tooltip("Réapparaît à sa place ce temps après s'être cassé (s ; 0 : jamais). Scène de test.")]
        [SerializeField, Min(0f)] private float respawnAfterBreak;

        [Header("Ennemi (D-030)")]
        [Tooltip("Ennemi : attrapable seulement s'il est étourdi, sans limite de masse ni durabilité.")]
        [SerializeField] private bool isEnemy;
        [Tooltip("Ennemi attrapable à tout moment, sans être étourdi (volant, D-040).")]
        [SerializeField] private bool grabbableAnytime;
        [Tooltip("Ennemi tenu : il se libère au bout de ce temps (s ; 0 : enemyHoldDuration de la télékinésie).")]
        [SerializeField, Min(0f)] private float holdDurationOverride;

        private static readonly List<Grabbable> AllList = new List<Grabbable>();
        private static readonly Dictionary<int, Grabbable> ById = new Dictionary<int, Grabbable>();
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        private Renderer[] _renderers;
        private Collider[] _colliders;
        private MaterialPropertyBlock _block;

        public static IReadOnlyList<Grabbable> All => AllList;
        public Rigidbody Body { get; private set; }
        public Collider[] Colliders => _colliders;
        public int Id => GetInstanceID();
        public float Mass => Body != null ? Body.mass : TryGetComponent(out Rigidbody rb) ? rb.mass : 0f;
        public bool Anchored { get; private set; }

        /// <summary>Tenu par la télékinésie.</summary>
        public bool Held { get; private set; }

        /// <summary>Tir tendu en cours : sans gravité jusqu'à ce temps (Time.time) ou jusqu'au premier choc.</summary>
        public bool InStraightFlight { get; private set; }
        private float _straightFlightUntil;

        /// <summary>Temps (Time.time) du dernier lancer : lueur verte qui s'éteint.</summary>
        public float ThrownAt { get; private set; } = float.NegativeInfinity;

        public int DurabilityOverride => durabilityOverride;

        /// <summary>Ennemi (D-030) : attrapable seulement étourdi, sans limite de masse, sans durabilité.</summary>
        public bool IsEnemy => isEnemy;
        public ItHunts.Combat.Damageable Damageable { get; private set; }
        public ItHunts.Combat.EnemyBody EnemyBody { get; private set; }

        /// <summary>Temps (Time.time) de la prise en cours (libération d'un ennemi au bout du temps réglé).</summary>
        public float HeldSince { get; private set; } = float.NegativeInfinity;

        /// <summary>
        /// Attrapable maintenant : pas cassé, pas déjà tenu (en coop, le premier qui l'attrape le garde) ; un ennemi
        /// seulement s'il est étourdi, vivant et pas le traqueur.
        /// </summary>
        public bool CanBeGrabbedNow =>
            !Broken && !Held && (!isEnemy || (EnemyBody != null && (EnemyBody.Stunned || grabbableAnytime) && Damageable != null
                                              && !Damageable.Dead && !Damageable.ImmuneToOrbs));

        public bool GrabbableAnytime => grabbableAnytime;

        /// <summary>Temps avant qu'un ennemi tenu se libère (son réglage, sinon celui de la télékinésie).</summary>
        public float HoldDuration(ItHunts.Settings.TelekinesisOrbSettings cfg) =>
            holdDurationOverride > 0f ? holdDurationOverride : cfg.enemyHoldDuration;

        public float HoldDurationOverride => holdDurationOverride;

        /// <summary>Peut rester tenu : un ennemi mort est lâché.</summary>
        public bool CanStayHeld => !Broken && (Damageable == null || !Damageable.Dead);
        public bool BreaksOnAnyImpact => breaksOnAnyImpact;
        public float SplashRadiusOverride => splashRadiusOverride;

        /// <summary>Lancers encore tenus (négatif : pas encore initialisé par ImpactDamager) et durabilité de départ.</summary>
        public int ThrowsLeft { get; private set; } = -1;
        public int MaxDurability { get; private set; } = -1;

        /// <summary>Lancé et pas encore touché quoi que ce soit : son premier impact compte (zone, durabilité).</summary>
        public bool ThrowArmed { get; private set; }

        /// <summary>
        /// Dernière poussée de la répulsion (D-031) : l'objet est « repoussé » depuis ce temps (Time.time) ; il est
        /// dangereux pushedDangerTime après, tant que sa vitesse n'est pas passée sous pushedMinSpeed.
        /// </summary>
        public float LastPushedAt { get; private set; } = float.NegativeInfinity;
        public bool Pushed { get; private set; }
        private bool _pushSlowed;

        /// <summary>Cassé : invisible et sans collision, en attente de réapparition.</summary>
        public bool Broken { get; private set; }

        private float _brokenAt;
        private Vector3 _homePosition;
        private Quaternion _homeRotation;

        public static Grabbable Find(int id) => ById.TryGetValue(id, out Grabbable g) ? g : null;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            _renderers = GetComponentsInChildren<Renderer>();
            _colliders = GetComponentsInChildren<Collider>();
            _block = new MaterialPropertyBlock();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Anchored = startsAnchored;
            if (Anchored)
                Body.isKinematic = true;
            _homePosition = transform.position;
            _homeRotation = transform.rotation;
            Damageable = GetComponent<ItHunts.Combat.Damageable>();
            EnemyBody = GetComponent<ItHunts.Combat.EnemyBody>();
        }

        /// <summary>Fixe la durabilité de départ (une seule fois, par ImpactDamager qui connaît les réglages).</summary>
        public void InitDurability(int durability)
        {
            if (MaxDurability > 0)
                return;
            MaxDurability = Mathf.Max(1, durability);
            ThrowsLeft = MaxDurability;
        }

        /// <summary>Premier impact d'un lancer : vrai une seule fois par lancer (puis le lancer est « désarmé »).</summary>
        public bool TakeThrowImpact()
        {
            if (!ThrowArmed)
                return false;
            ThrowArmed = false;
            return true;
        }

        /// <summary>Un lancer de plus encaissé ; vrai s'il n'en reste plus (l'objet doit se casser).</summary>
        public bool ConsumeDurability()
        {
            if (ThrowsLeft < 0)
                return false;
            ThrowsLeft = Mathf.Max(0, ThrowsLeft - 1);
            return ThrowsLeft == 0;
        }

        /// <summary>
        /// Dernier joueur qui a attrapé, lancé ou repoussé l'objet (identifiant d'instance de son objet racine ; 0 : aucun).
        /// Tir ami (D-032) : ce que l'objet percute est attribué à ce joueur (il ne se blesse jamais lui-même).
        /// </summary>
        public int LastOwnerId { get; private set; }

        public void SetOwner(int playerId)
        {
            if (playerId != 0)
                LastOwnerId = playerId;
        }

        /// <summary>Poussé par la répulsion (onde ou poussée continue) à ce temps, par ce joueur (0 : inconnu).</summary>
        public void MarkPushed(float time, int byPlayer = 0)
        {
            SetOwner(byPlayer);
            Pushed = true;
            _pushSlowed = false;
            LastPushedAt = time;
        }

        /// <summary>Danger d'un objet repoussé : ni plus de pushedDangerTime après la dernière poussée, ni passé sous pushedMinSpeed.</summary>
        public bool PushDangerous(float time, float dangerTime) => Pushed && !_pushSlowed && time - LastPushedAt <= dangerTime;

        /// <summary>À chaque pas : un objet repoussé qui passe sous le seuil de vitesse n'est plus dangereux.</summary>
        public void TickPushDanger(float minSpeed)
        {
            if (Pushed && !_pushSlowed && Body != null && !Body.isKinematic && Body.linearVelocity.magnitude < minSpeed)
                _pushSlowed = true;
        }

        private void ClearPush()
        {
            Pushed = false;
            _pushSlowed = false;
        }

        /// <summary>Décroché par une onde de répulsion (lustre) : il tombe.</summary>
        public void Unanchor()
        {
            if (!Anchored || Held)
                return;
            Anchored = false;
            Body.isKinematic = false;
            Body.useGravity = true;
        }

        /// <summary>
        /// Se casse : éclat, débris et son (AreaEvents.Broken, retour dans DamageFeedback), puis disparaît ; réapparaît à
        /// sa place après respawnAfterBreak si ce délai est réglé.
        /// </summary>
        public void Break()
        {
            if (Broken || Held)
                return;
            Broken = true;
            _brokenAt = Time.time;
            ThrowArmed = false;
            InStraightFlight = false;
            Color color = _renderers.Length > 0 && _renderers[0] != null && _renderers[0].sharedMaterial != null
                ? _renderers[0].sharedMaterial.color
                : Color.gray;
            Bounds b = _colliders.Length > 0 && _colliders[0] != null ? _colliders[0].bounds : new Bounds(transform.position, Vector3.one * 0.5f);
            ItHunts.Combat.AreaEvents.RaiseBroken(new ItHunts.Combat.BreakEvent
            {
                Point = b.center,
                Size = b.size,
                Velocity = Body.isKinematic ? Vector3.zero : Body.linearVelocity,
                Color = color,
            });
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }

            Body.isKinematic = true;
            SetPresent(false);
        }

        private void Update()
        {
            if (Broken && respawnAfterBreak > 0f && Time.time - _brokenAt >= respawnAfterBreak)
                Respawn();
        }

        /// <summary>Remise à zéro de la zone : à sa place, intact, immobile (s'il n'est pas tenu).</summary>
        public void ResetToHome()
        {
            if (Held)
                return;
            ClearPush();
            ThrowArmed = false;
            InStraightFlight = false;
            Respawn();
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
        }

        private void Respawn()
        {
            Broken = false;
            transform.SetPositionAndRotation(_homePosition, _homeRotation);
            Body.position = _homePosition;
            Body.rotation = _homeRotation;
            Anchored = startsAnchored;
            Body.isKinematic = Anchored;
            Body.useGravity = true;
            if (MaxDurability > 0)
                ThrowsLeft = MaxDurability;
            SetPresent(true);
        }

        private void SetPresent(bool present)
        {
            foreach (Collider c in _colliders)
                if (c != null)
                    c.enabled = present;
            foreach (Renderer r in _renderers)
                if (r != null)
                    r.enabled = present;
        }

        private void OnEnable()
        {
            AllList.Add(this);
            ById[Id] = this;
        }

        private void OnDisable()
        {
            AllList.Remove(this);
            ById.Remove(Id);
        }

        /// <summary>Attrapé : libéré s'il était accroché, sans gravité, suit le point de maintien.</summary>
        public void OnGrabbed()
        {
            Held = true;
            HeldSince = Time.time;
            ClearPush();
            ThrowArmed = false;
            InStraightFlight = false;
            Anchored = false;
            Body.isKinematic = false;
            Body.useGravity = false;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        /// <summary>
        /// Relâché (posé, lâché ou lancé) : gravité normale ; lancé avec noGravityFor &gt; 0 : tir tendu, sans gravité
        /// pendant ce temps ou jusqu'au premier choc (D-025).
        /// </summary>
        public void OnReleased(bool thrown, float noGravityFor = 0f)
        {
            Held = false;
            ThrowArmed = thrown;
            if (thrown)
                ClearPush();
            InStraightFlight = thrown && noGravityFor > 0f;
            _straightFlightUntil = Time.time + noGravityFor;
            Body.useGravity = !InStraightFlight;
            if (thrown)
                ThrownAt = Time.time;
        }

        /// <summary>Seuil de vitesse du danger d'un objet repoussé (fixé par ImpactDamager, qui connaît les réglages).</summary>
        public float PushMinSpeed { get; set; } = 6f;

        private void FixedUpdate()
        {
            TickPushDanger(PushMinSpeed);
            if (InStraightFlight && Time.time >= _straightFlightUntil)
                EndStraightFlight();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (InStraightFlight)
                EndStraightFlight();
        }

        private void EndStraightFlight()
        {
            InStraightFlight = false;
            if (!Held)
                Body.useGravity = true;
        }

        /// <summary>Couleur d'émission (HDR) de tous ses rendus.</summary>
        public void SetGlow(Color color)
        {
            // Shader toon de la DA : copie du matériau (EmissionGlow) ; repli glowWholeModel : tout le modèle.
            foreach (Renderer r in _renderers)
                ItHunts.Combat.EmissionGlow.Set(r, color, glowWholeModel);
        }
    }
}
