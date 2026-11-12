using ItHunts.Movement;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Input
{
    /// <summary>
    /// Lit les actions du PlayerInput de CE joueur à chaque image (jamais en FixedUpdate, où un appui
    /// serait manqué ou lu deux fois) et horodate les appuis avec le numéro du prochain tick.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public const string KeyboardMouseScheme = "KeyboardMouse";
        public const string GamepadScheme = "Gamepad";

        private PlayerInput _playerInput;
        private PlayerMotor _motor;
        private InputAction _move;
        private InputAction _look;
        private InputAction _lookGamepad;
        private InputAction _jump;
        private InputAction _slide;
        private InputAction _dash;
        private InputAction _attract;
        private InputAction _repel;
        private InputAction _grab;
        private InputAction _throw;
        private InputAction _toggleDebug;
        private InputAction _pause;

        public Vector2 Move { get; private set; }

        /// <summary>Déplacement de la souris depuis l'image précédente (points).</summary>
        public Vector2 LookMouseDelta { get; private set; }

        /// <summary>Stick droit (-1..1).</summary>
        public Vector2 LookStick { get; private set; }

        public bool JumpHeld { get; private set; }
        public int JumpPressedTick { get; private set; } = MovementState.NeverTick;
        public bool SlideHeld { get; private set; }
        public int DashPressedTick { get; private set; } = MovementState.NeverTick;
        public int AttractPressedTick { get; private set; } = MovementState.NeverTick;
        public bool AttractHeld { get; private set; }
        public int RepelPressedTick { get; private set; } = MovementState.NeverTick;
        public bool RepelHeld { get; private set; }
        public int GrabPressedTick { get; private set; } = MovementState.NeverTick;
        public int ThrowPressedTick { get; private set; } = MovementState.NeverTick;
        public bool ToggleDebugPressed { get; private set; }
        public bool PausePressed { get; private set; }

        public string ControlScheme => _playerInput != null ? _playerInput.currentControlScheme : null;
        public bool UsingGamepad => ControlScheme == GamepadScheme;
        public int PlayerIndex => _playerInput != null ? _playerInput.playerIndex : 0;

        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            _motor = GetComponent<PlayerMotor>();
            InputActionAsset actions = _playerInput.actions;
            _move = actions.FindAction("Player/Move", throwIfNotFound: true);
            _look = actions.FindAction("Player/Look", throwIfNotFound: true);
            _lookGamepad = actions.FindAction("Player/LookGamepad", throwIfNotFound: true);
            _jump = actions.FindAction("Player/Jump", throwIfNotFound: true);
            _slide = actions.FindAction("Player/Slide", throwIfNotFound: true);
            _dash = actions.FindAction("Player/Dash", throwIfNotFound: true);
            _attract = actions.FindAction("Player/Attract", throwIfNotFound: true);
            _repel = actions.FindAction("Player/Repel", throwIfNotFound: true);
            _grab = actions.FindAction("Player/Grab", throwIfNotFound: true);
            _throw = actions.FindAction("Player/Throw", throwIfNotFound: true);
            _toggleDebug = actions.FindAction("Player/ToggleDebug", throwIfNotFound: true);
            _pause = actions.FindAction("Player/Pause", throwIfNotFound: true);
        }

        private void Update()
        {
            Move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
            LookMouseDelta = _look.ReadValue<Vector2>();
            LookStick = Vector2.ClampMagnitude(_lookGamepad.ReadValue<Vector2>(), 1f);
            JumpHeld = _jump.IsPressed();
            if (_jump.WasPressedThisFrame())
                JumpPressedTick = _motor != null ? _motor.NextTick : 0;
            SlideHeld = _slide.IsPressed();
            if (_dash.WasPressedThisFrame())
                DashPressedTick = _motor != null ? _motor.NextTick : 0;
            // Orbe unique (D-024) : traction (A en AZERTY / RB, maintenu), répulsion (clic droit / LT), attraper ou
            // relâcher (F / X), lancer (clic gauche / RT). Souris libre (clic pour la reprendre) : rien ne part.
            bool aimReady = UsingGamepad || Cursor.lockState == CursorLockMode.Locked;
            int next = _motor != null ? _motor.NextTick : 0;
            if (_attract.WasPressedThisFrame() && aimReady)
                AttractPressedTick = next;
            AttractHeld = _attract.IsPressed() && aimReady;
            if (_repel.WasPressedThisFrame() && aimReady)
                RepelPressedTick = next;
            RepelHeld = _repel.IsPressed() && aimReady;
            if (_grab.WasPressedThisFrame() && aimReady)
                GrabPressedTick = next;
            if (_throw.WasPressedThisFrame() && aimReady)
                ThrowPressedTick = next;
            ToggleDebugPressed = _toggleDebug.WasPressedThisFrame();
            PausePressed = _pause.WasPressedThisFrame();
        }
    }
}
