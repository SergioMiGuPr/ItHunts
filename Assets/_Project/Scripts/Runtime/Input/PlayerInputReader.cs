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
        private InputAction _swapOrb;
        private InputAction _swapOrbScroll;
        private InputAction _selectOrb1;
        private InputAction _selectOrb2;
        private float _lastScrollSwapTime = float.NegativeInfinity;
        private const float ScrollSwapInterval = 0.15f;
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
        public int OrbSwapPressedTick { get; private set; } = MovementState.NeverTick;
        public int OrbSelectPressedTick { get; private set; } = MovementState.NeverTick;
        public byte OrbSelect { get; private set; }
        public int RepelPressedTick { get; private set; } = MovementState.NeverTick;
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
            _swapOrb = actions.FindAction("Player/SwapOrb", throwIfNotFound: true);
            _swapOrbScroll = actions.FindAction("Player/SwapOrbScroll", throwIfNotFound: true);
            _selectOrb1 = actions.FindAction("Player/SelectOrb1", throwIfNotFound: true);
            _selectOrb2 = actions.FindAction("Player/SelectOrb2", throwIfNotFound: true);
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
            // Souris libre (clic pour la reprendre) : le clic ne déclenche pas l'orbe.
            bool aimReady = UsingGamepad || Cursor.lockState == CursorLockMode.Locked;
            if (_attract.WasPressedThisFrame() && aimReady)
                AttractPressedTick = _motor != null ? _motor.NextTick : 0;
            AttractHeld = _attract.IsPressed() && aimReady;

            // Échange des orbes : Y ou un cran de molette (dans les deux sens) ; touches 1 et 2 pour choisir.
            int next = _motor != null ? _motor.NextTick : 0;
            // (Molette : un échange par geste ; un défilement continu sur un pavé tactile n'en fait pas plusieurs.)
            bool scrolled = Mathf.Abs(_swapOrbScroll.ReadValue<float>()) > 0.01f;
            if (scrolled && Time.unscaledTime - _lastScrollSwapTime < ScrollSwapInterval)
                scrolled = false;
            if (scrolled)
                _lastScrollSwapTime = Time.unscaledTime;
            if (_swapOrb.WasPressedThisFrame() || scrolled)
                OrbSwapPressedTick = next;
            if (_selectOrb1.WasPressedThisFrame())
            {
                OrbSelectPressedTick = next;
                OrbSelect = 1;
            }
            else if (_selectOrb2.WasPressedThisFrame())
            {
                OrbSelectPressedTick = next;
                OrbSelect = 2;
            }
            if (_repel.WasPressedThisFrame() && aimReady)
                RepelPressedTick = _motor != null ? _motor.NextTick : 0;
            ToggleDebugPressed = _toggleDebug.WasPressedThisFrame();
            PausePressed = _pause.WasPressedThisFrame();
        }
    }
}
