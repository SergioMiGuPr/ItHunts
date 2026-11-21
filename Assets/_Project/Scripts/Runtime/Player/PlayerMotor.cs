using ItHunts.Input;
using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Fait avancer la simulation du mouvement à pas fixe et fournit une position interpolée à la
    /// caméra. Tout l'état vit sur ce composant : instanciable N fois (écran partagé, coop).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private MovementSettings settings;
        [Tooltip("Orbe de magnétisme (vide : pas d'orbe).")]
        [SerializeField] private MagnetismOrbSettings magnetism;
        [Tooltip("Télékinésie de l'orbe (vide : pas de télékinésie).")]
        [SerializeField] private TelekinesisOrbSettings telekinesis;
        [SerializeField] private LayerMask collisionMask = ~0;
        [Tooltip("Sous cette hauteur (m), le joueur revient à son point de départ.")]
        [SerializeField] private float killHeight = -30f;

        private CharacterController _controller;
        private CharacterControllerProbe _probe;
        private PlayerInputReader _reader;
        private PlayerLook _look;
        private MovementState _state;
        private MagnetismState _magnet;
        private PhysicsAimProbe _aim;
        private TelekinesisState _tk;
        private PhysicsGrabProbe _grabProbe;
        private TelekinesisHolder _holder;
        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private Vector3 _spawnPosition;
        private Vector3 _lastEye;
        private FirstPersonView _view;
        private ItHunts.Artifacts.ArtifactHolder _artifact;

        public MovementState State => _state;
        public MovementSettings Settings => settings;
        public MagnetismState Magnetism => _magnet;
        public MagnetismOrbSettings MagnetismSettings => magnetism;

        /// <summary>
        /// Améliorations (D-043) : réglages propres à ce joueur (copies modifiées des assets partagés). Les assets eux-mêmes ne
        /// changent pas ; un réglage modifié en Play Mode sur l'asset ne touche plus ce joueur après sa première amélioration.
        /// </summary>
        public void UseSettings(MovementSettings movement, MagnetismOrbSettings magnet)
        {
            if (movement != null)
                settings = movement;
            if (magnet != null)
                magnetism = magnet;
        }
        public TelekinesisState Telekinesis => _tk;
        public TelekinesisOrbSettings TelekinesisSettings => telekinesis;
        public TelekinesisHolder Holder => _holder;
        public ItHunts.Artifacts.ArtifactHolder Artifact => _artifact;
        private bool HasTelekinesis => telekinesis != null;

        /// <summary>Numéro du prochain tick simulé : sert à horodater les appuis.</summary>
        public int NextTick => _state.Tick + 1;

        /// <summary>Position entre les deux derniers ticks, pour un affichage fluide à toute fréquence d'image.</summary>
        public Vector3 InterpolatedPosition
        {
            get
            {
                float alpha = Time.fixedDeltaTime > 0f
                    ? Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime)
                    : 1f;
                return Vector3.Lerp(_previousPosition, _currentPosition, alpha);
            }
        }

        private void Awake()
        {
            if (settings == null)
            {
                Debug.LogError($"{name} : aucun MovementSettings assigné à PlayerMotor.", this);
                enabled = false;
                return;
            }

            _controller = GetComponent<CharacterController>();
            if (Physics.simulationMode != SimulationMode.FixedUpdate)
                Debug.LogWarning($"Physique en mode {Physics.simulationMode} : les objets (télékinésie) ne bougeront pas. " +
                                 "Remettre Physics.simulationMode à FixedUpdate.", this);
            _reader = GetComponent<PlayerInputReader>();
            _look = GetComponent<PlayerLook>();
            ApplyCapsuleSettings();
            _probe = new CharacterControllerProbe(_controller, settings, collisionMask);
            _probe.SetHeight(settings.capsuleHeight);
            _spawnPosition = transform.position;
            _state = MovementState.Create(_spawnPosition);
            _magnet = MagnetismState.Create();
            _aim = new PhysicsAimProbe(magnetism != null ? magnetism.aimMask : (LayerMask)~0, transform);
            _tk = TelekinesisState.Create();
            _grabProbe = new PhysicsGrabProbe(telekinesis != null ? telekinesis.aimMask : (LayerMask)~0, transform);
            _holder = new TelekinesisHolder(_controller, _grabProbe, gameObject.GetInstanceID());
            _artifact = GetComponent<ItHunts.Artifacts.ArtifactHolder>();
            _previousPosition = _currentPosition = _spawnPosition;
            _lastEye = MagnetismOrb.EyePosition(_state, settings);
        }

        private PlayerHealth _health;

        private void FixedUpdate()
        {
            if (_health == null)
                _health = GetComponent<PlayerHealth>();
            ApplyCapsuleSettings();
            _previousPosition = _currentPosition;

            var input = MovementInput.Neutral;
            input.Yaw = _look != null ? _look.Yaw : transform.eulerAngles.y;
            input.Pitch = _look != null ? _look.Pitch : 0f;
            if (_reader != null && (_health == null || !_health.Dead))   // à terre ou mort : plus d'actions
            {
                input.Move = _reader.Move;
                input.JumpHeld = _reader.JumpHeld;
                input.JumpPressedTick = _reader.JumpPressedTick;
                input.SlideHeld = _reader.SlideHeld;
                input.DashPressedTick = _reader.DashPressedTick;
                input.AttractPressedTick = _reader.AttractPressedTick;
                input.AttractHeld = _reader.AttractHeld;
                input.RepelPressedTick = _reader.RepelPressedTick;
                input.RepelHeld = _reader.RepelHeld;
                input.GrabPressedTick = _reader.GrabPressedTick;
                input.ThrowPressedTick = _reader.ThrowPressedTick;
                input.ThrowHeld = _reader.ThrowHeld;
                input.ReloadPressedTick = _reader.ReloadPressedTick;
            }

            float dt = Time.fixedDeltaTime;

            // Même tick, même entrée. Orbe unique (D-024) : magnétisme et télékinésie reçoivent tous les boutons, chacun
            // sur sa touche ; puis le mouvement, puis le côté physique de la télékinésie.
            if (magnetism != null)
            {
                _aim.Mask = magnetism.aimMask;
                _aim.IgnoreBody = _holder.Held != null ? _holder.Held.Body : null;   // l'objet tenu ne bloque pas la visée
                MagnetismOrb.Step(ref _magnet, ref _state, input, magnetism, settings, dt, _aim);
            }

            var command = new TelekinesisCommand();
            bool heldAtTickStart = _tk.Holding;
            if (HasTelekinesis)
            {
                _grabProbe.Mask = telekinesis.aimMask;
                _grabProbe.IgnoreId = _tk.HeldId;
                _grabProbe.Settings = telekinesis;
                command = TelekinesisOrb.Step(ref _tk, _state, input, telekinesis, settings, dt, _grabProbe);
            }

            // Yeux au début du tick : c'est de là que le joueur voyait la scène au moment du clic (le rayon du lancer en
            // part ; partir des yeux après le mouvement décalait l'impact d'un tick de déplacement, 0.13 m à 7.5 m/s).
            Vector3 eyeAtClick = MagnetismOrb.EyePosition(_state, settings);
            bool wasGrounded = _state.Grounded;
            float fallSpeed = -_state.Velocity.y;
            MovementMotor.Step(ref _state, input, settings, dt, _probe);
            if (!wasGrounded && _state.Grounded && fallSpeed >= ItHunts.Enemies.NoiseEvents.HardLandingSpeed)
                ItHunts.Enemies.NoiseEvents.Raise(new ItHunts.Enemies.Noise
                {
                    Point = transform.position, Kind = ItHunts.Enemies.NoiseKind.AtterrissageDur, PlayerId = gameObject.GetInstanceID(),
                    Time = Time.time,
                });
            _currentPosition = transform.position;

            // Répulsion partie ce tick : onde en cône depuis le joueur, dans la direction visée (D-029).
            if (magnetism != null && _magnet.LastRepelTick == _state.Tick)
            {
                ItHunts.Enemies.NoiseEvents.Raise(new ItHunts.Enemies.Noise
                {
                    Point = transform.position, Kind = ItHunts.Enemies.NoiseKind.Repulsion, PlayerId = gameObject.GetInstanceID(),
                    Time = Time.time,
                });
                if (_view == null)
                    _view = GetComponent<FirstPersonView>();
                Vector3 aim = MagnetismOrb.AimDirection(input.Yaw, input.Pitch);
                ItHunts.Combat.RepulsionWave.Apply(eyeAtClick, aim, _view != null ? _view.LineStart : eyeAtClick, magnetism,
                    _holder != null && _holder.Held != null ? _holder.Held.Body : null, transform);
            }
            else if (magnetism != null && _magnet.RepelHolding)
            {
                // Répulsion maintenue : poussée continue dans le cône, sans nouvelle propulsion du joueur (D-029).
                if (_view == null)
                    _view = GetComponent<FirstPersonView>();
                Vector3 aim = MagnetismOrb.AimDirection(input.Yaw, input.Pitch);
                ItHunts.Combat.RepulsionWave.ApplyContinuous(eyeAtClick, aim, _view != null ? _view.LineStart : eyeAtClick, magnetism,
                    _holder != null && _holder.Held != null ? _holder.Held.Body : null, transform, dt);
            }

            // Artefact (main gauche, D-032) : même clic que le lancer ; il ne tire que si l'orbe ne tenait rien au début du
            // tick et ne tient rien maintenant (tir et lancer jamais au même tick). Rayon depuis les yeux au moment du clic.
            if (_artifact != null)
                _artifact.Step(_state.Tick, input, heldAtTickStart, _tk.Holding, eyeAtClick,
                    MagnetismOrb.AimDirection(input.Yaw, input.Pitch), dt);

            if (HasTelekinesis)
            {
                Vector3 eye = MagnetismOrb.EyePosition(_state, settings);
                Vector3 eyeVelocity = (eye - _lastEye) / dt;     // déplacement réel des yeux ce tick (collisions, accroupi)
                Vector3 aimDir = MagnetismOrb.AimDirection(input.Yaw, input.Pitch);
                if (!_holder.Apply(command, telekinesis, eyeAtClick, _state.Velocity))
                    TelekinesisOrb.NotifyLost(ref _tk, _state.Tick);
                if (_tk.Holding && !_holder.Drive(telekinesis, eye, aimDir, eyeVelocity, dt))
                    TelekinesisOrb.NotifyLost(ref _tk, _state.Tick);
                _holder.Tick(Time.time);
            }

            _lastEye = MagnetismOrb.EyePosition(_state, settings);
            if (_currentPosition.y < killHeight)
                Teleport(_spawnPosition);
        }

        /// <summary>
        /// Poussée venue d'un ennemi (bond de la goule) : ajoutée à la vitesse simulée sans jamais ralentir le joueur
        /// (D-012, EnemyMath.PushWithoutSlowdown). Réseau : à appliquer côté autorité, comme un coup.
        /// </summary>
        public void AddVelocity(Vector3 push) => _state.Velocity = ItHunts.Enemies.EnemyMath.PushWithoutSlowdown(_state.Velocity, push);

        /// <summary>Retour au point de départ (PV à 0, provisoire avant la mort de l'étape 8).</summary>
        public void Respawn() => Teleport(_spawnPosition);

        /// <summary>Déplace le joueur sans interpolation ; garde le compteur de ticks (appuis horodatés).</summary>
        public void Teleport(Vector3 position)
        {
            // L'objet tenu est lâché sur place.
            if (_holder != null && _tk.Holding && telekinesis != null)
            {
                _holder.Apply(new TelekinesisCommand { Kind = TelekinesisCommandKind.Lacher, TargetId = _tk.HeldId }, telekinesis,
                    transform.position, Vector3.zero);
                TelekinesisOrb.NotifyLost(ref _tk, _state.Tick);
            }

            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;

            int tick = _state.Tick;
            int consumed = _state.LastConsumedJumpPressTick;
            int consumedDash = _state.LastConsumedDashPressTick;
            _probe.SetHeight(settings.capsuleHeight);
            _probe.ResetContacts();
            _state = MovementState.Create(position);
            _state.Tick = tick;
            _state.LastConsumedJumpPressTick = consumed;
            _state.LastConsumedDashPressTick = consumedDash;
            _previousPosition = _currentPosition = position;
            _lastEye = MagnetismOrb.EyePosition(_state, settings);
        }

        /// <summary>Rayon, pente et marche. La hauteur est gérée par la simulation (glissade, accroupi).</summary>
        private void ApplyCapsuleSettings()
        {
            float radius = Mathf.Min(settings.capsuleRadius, settings.crouchHeight * 0.5f);
            if (!Mathf.Approximately(_controller.radius, radius))
                _controller.radius = radius;

            if (!Mathf.Approximately(_controller.slopeLimit, settings.slopeLimit))
                _controller.slopeLimit = settings.slopeLimit;
            if (!Mathf.Approximately(_controller.stepOffset, settings.stepOffset))
                _controller.stepOffset = Mathf.Min(settings.stepOffset, settings.crouchHeight);
            _controller.minMoveDistance = 0f;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit) => _probe?.ReportHit(hit.normal);

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            Vector3 origin = transform.position + Vector3.up * 1f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + _state.Velocity * 0.25f);
        }
    }
}
