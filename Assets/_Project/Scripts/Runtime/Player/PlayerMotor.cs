using ItHunts.Input;
using ItHunts.Movement;
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
        [SerializeField] private LayerMask collisionMask = ~0;
        [Tooltip("Sous cette hauteur (m), le joueur revient à son point de départ.")]
        [SerializeField] private float killHeight = -30f;

        private CharacterController _controller;
        private CharacterControllerProbe _probe;
        private PlayerInputReader _reader;
        private PlayerLook _look;
        private MovementState _state;
        private Vector3 _previousPosition;
        private Vector3 _currentPosition;
        private Vector3 _spawnPosition;

        public MovementState State => _state;
        public MovementSettings Settings => settings;

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
            _reader = GetComponent<PlayerInputReader>();
            _look = GetComponent<PlayerLook>();
            ApplyCapsuleSettings();
            _probe = new CharacterControllerProbe(_controller, settings, collisionMask);
            _spawnPosition = transform.position;
            _state = MovementState.Create(_spawnPosition);
            _previousPosition = _currentPosition = _spawnPosition;
        }

        private void FixedUpdate()
        {
            ApplyCapsuleSettings();
            _previousPosition = _currentPosition;

            var input = MovementInput.Neutral;
            input.Yaw = _look != null ? _look.Yaw : transform.eulerAngles.y;
            if (_reader != null)
            {
                input.Move = _reader.Move;
                input.JumpHeld = _reader.JumpHeld;
                input.JumpPressedTick = _reader.JumpPressedTick;
            }

            MovementMotor.Step(ref _state, input, settings, Time.fixedDeltaTime, _probe);
            _currentPosition = transform.position;

            if (_currentPosition.y < killHeight)
                Teleport(_spawnPosition);
        }

        /// <summary>Déplace le joueur sans interpolation ; garde le compteur de ticks (appuis horodatés).</summary>
        public void Teleport(Vector3 position)
        {
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;

            int tick = _state.Tick;
            int consumed = _state.LastConsumedJumpPressTick;
            _state = MovementState.Create(position);
            _state.Tick = tick;
            _state.LastConsumedJumpPressTick = consumed;
            _previousPosition = _currentPosition = position;
        }

        private void ApplyCapsuleSettings()
        {
            float height = settings.capsuleHeight;
            float radius = Mathf.Min(settings.capsuleRadius, height * 0.5f);
            if (!Mathf.Approximately(_controller.height, height) || !Mathf.Approximately(_controller.radius, radius))
            {
                _controller.height = height;
                _controller.radius = radius;
                _controller.center = new Vector3(0f, height * 0.5f, 0f);
            }

            if (!Mathf.Approximately(_controller.slopeLimit, settings.slopeLimit))
                _controller.slopeLimit = settings.slopeLimit;
            if (!Mathf.Approximately(_controller.stepOffset, settings.stepOffset))
                _controller.stepOffset = Mathf.Min(settings.stepOffset, height);
            _controller.minMoveDistance = 0f;
        }

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
