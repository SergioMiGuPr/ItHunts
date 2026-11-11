using ItHunts.Input;
using ItHunts.Settings;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Player
{
    /// <summary>
    /// Vue première personne : rotation à chaque image (sans lissage), position interpolée entre
    /// les ticks, FOV qui s'élargit avec la vitesse. Pilote une CinemachineCamera.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerLook : MonoBehaviour
    {
        [SerializeField] private CameraSettings settings;
        [Tooltip("Pivot de la vue. Détaché du joueur au lancement pour suivre la position interpolée.")]
        [SerializeField] private Transform viewRig;
        [SerializeField] private CinemachineCamera virtualCamera;

        private PlayerInputReader _reader;
        private PlayerMotor _motor;
        private float _yaw;
        private float _pitch;
        private float _fov;
        private float _eyeHeight = -1f;

        /// <summary>Rapidité de descente et de remontée de la vue en glissade / accroupi (/s).</summary>
        private const float EyeHeightLerpSpeed = 15f;

        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public float CurrentFov => _fov;

        private void Awake()
        {
            if (settings == null || viewRig == null)
            {
                Debug.LogError($"{name} : CameraSettings ou ViewRig manquant sur PlayerLook.", this);
                enabled = false;
                return;
            }

            _reader = GetComponent<PlayerInputReader>();
            _motor = GetComponent<PlayerMotor>();
            _yaw = transform.eulerAngles.y;
            _fov = settings.baseFov;
            viewRig.SetParent(null, worldPositionStays: true);
            viewRig.name = $"{name} ViewRig";
        }

        private void Start() => SetCursorLocked(!(_reader != null && _reader.UsingGamepad));

        private void OnDestroy()
        {
            if (viewRig != null)
                Destroy(viewRig.gameObject);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            HandleCursor();

            if (_reader != null)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Vector2 mouse = _reader.LookMouseDelta * settings.mouseSensitivity;
                    _yaw += mouse.x;
                    _pitch += settings.invertMouseY ? mouse.y : -mouse.y;
                }

                Vector2 stick = _reader.LookStick;
                float magnitude = stick.magnitude;
                if (magnitude > 1e-4f)
                {
                    stick = stick / magnitude * Mathf.Pow(magnitude, settings.gamepadResponseExponent);
                    _yaw += stick.x * settings.gamepadYawSpeed * dt;
                    _pitch += (settings.invertGamepadY ? stick.y : -stick.y) * settings.gamepadPitchSpeed * dt;
                }
            }

            _yaw = Mathf.Repeat(_yaw, 360f);
            _pitch = Mathf.Clamp(_pitch, -settings.pitchLimit, settings.pitchLimit);

            float targetEye = 1.6f;
            if (_motor != null && _motor.Settings != null)
                targetEye = _motor.State.Crouched ? _motor.Settings.crouchEyeHeight : _motor.Settings.eyeHeight;
            _eyeHeight = _eyeHeight < 0f ? targetEye : Mathf.Lerp(_eyeHeight, targetEye, 1f - Mathf.Exp(-EyeHeightLerpSpeed * dt));

            Vector3 feet = _motor != null ? _motor.InterpolatedPosition : transform.position;
            viewRig.SetPositionAndRotation(feet + Vector3.up * _eyeHeight, Quaternion.Euler(_pitch, _yaw, 0f));

            UpdateFov(dt);
        }

        private void UpdateFov(float dt)
        {
            float speed = _motor != null ? _motor.State.HorizontalSpeed : 0f;
            float t = settings.fovSpeedMax > settings.fovSpeedMin
                ? Mathf.InverseLerp(settings.fovSpeedMin, settings.fovSpeedMax, speed)
                : 0f;
            float target = settings.baseFov + settings.fovBonusAtMaxSpeed * t;
            _fov = Mathf.Lerp(_fov, target, 1f - Mathf.Exp(-settings.fovLerpSpeed * dt));

            if (virtualCamera != null)
            {
                LensSettings lens = virtualCamera.Lens;
                lens.FieldOfView = _fov;
                virtualCamera.Lens = lens;
            }
        }

        private void HandleCursor()
        {
            if (_reader == null)
                return;

            if (_reader.PausePressed)
            {
                SetCursorLocked(Cursor.lockState != CursorLockMode.Locked && !_reader.UsingGamepad);
                return;
            }

            // Un clic dans la fenêtre de jeu reprend la souris.
            Mouse mouse = Mouse.current;
            if (Cursor.lockState != CursorLockMode.Locked && !_reader.UsingGamepad && Application.isFocused
                && mouse != null && mouse.leftButton.wasPressedThisFrame)
                SetCursorLocked(true);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
