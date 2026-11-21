using System.Collections.Generic;
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

        /// <summary>Oriente la vue (téléportation, réapparition).</summary>
        public void SetView(float yaw, float pitch)
        {
            _yaw = Mathf.Repeat(yaw, 360f);
            _pitch = pitch;
        }

        public float Yaw => _yaw;
        public float Pitch => _pitch;
        public float CurrentFov => _fov;

        /// <summary>Rig de vue (détaché du joueur au démarrage) : caméra, caméra virtuelle, mains.</summary>
        public Transform ViewRig => viewRig;

        /// <summary>FOV ajouté pour cette image (°) : coup de FOV du dash (PlayerFeel).</summary>
        public float FovPunch { get; set; }

        /// <summary>Facteur de FOV posé par l'écran partagé selon la découpe (1 en solo).</summary>
        public float FovScale { get; set; } = 1f;

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
                    Vector2 mouse = _reader.LookMouseDelta * (settings.mouseSensitivity * ItHunts.Level.GameSettings.Sensitivity) * AssistSlowdown(false, peek: true);
                    _yaw += mouse.x;
                    _pitch += settings.invertMouseY ? mouse.y : -mouse.y;
                }

                Vector2 stick = _reader.LookStick;
                float magnitude = stick.magnitude;
                float slow = AssistSlowdown(_reader.UsingGamepad) * ItHunts.Level.GameSettings.Sensitivity;   // réglage du joueur (Paramètres)
                if (magnitude > 1e-4f)
                {
                    stick = stick / magnitude * Mathf.Pow(magnitude, settings.gamepadResponseExponent);
                    _yaw += stick.x * settings.gamepadYawSpeed * slow * dt;
                    _pitch += (settings.invertGamepadY ? stick.y : -stick.y) * settings.gamepadPitchSpeed * slow * dt;
                }

                // Légère attraction vers la cible, quand le joueur vise ou se déplace.
                if (_assistActive && settings.aimAssistPull > 0f && (magnitude > 0.1f || _reader.Move.sqrMagnitude > 0.01f
                                                                     || (!_reader.UsingGamepad && _reader.LookMouseDelta.sqrMagnitude > 0.01f)))
                {
                    Vector2 pull = AimAssist.Pull(_yaw, _pitch, viewRig.position, _assistTarget, settings.aimAssistPull, dt);
                    _yaw += pull.x;
                    _pitch += pull.y;
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

        private bool _assistActive;
        private Vector3 _assistTarget;
        private int _assistFrame = -1;
        private float _assistSlow = 1f;

        /// <summary>Aide à la visée active ce pas-ci (debug, tests).</summary>
        public bool AimAssistActive => _assistActive;

        /// <summary>
        /// Facteur de vitesse de visée (aide à la visée) : cible la plus proche du viseur dans le cône, visible ; calculé une fois
        /// par image. peek : seulement lire le facteur pour la souris (actif seulement si le réglage souris l'est).
        /// </summary>
        private float AssistSlowdown(bool gamepad, bool peek = false)
        {
            if (_assistFrame != Time.frameCount)
            {
                _assistFrame = Time.frameCount;
                _assistActive = false;
                _assistSlow = 1f;
                bool on = AimAssist.Enabled(settings, _reader != null && _reader.UsingGamepad);
                if (on && viewRig != null)
                {
                    Vector3 eye = viewRig.position;
                    Vector3 fwd = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward;
                    bool freeHand = _motor == null || _motor.Holder == null || _motor.Holder.Held == null;
                    IReadOnlyList<Vector3> pts = AimAssist.Targets(eye, freeHand, _motor != null ? _motor.TelekinesisSettings : null, settings.aimAssistRange);
                    if (AimAssist.BestVisible(eye, fwd, pts, settings.aimAssistAngle, settings.aimAssistRange, out Vector3 best, out float angle))
                    {
                        _assistActive = true;
                        _assistTarget = best;
                        _assistSlow = AimAssist.Slowdown(angle, settings.aimAssistAngle, settings.aimAssistSlowdown);
                    }
                }
            }

            if (peek && (_reader == null || _reader.UsingGamepad))
                return 1f;
            return _assistSlow;
        }

        private void UpdateFov(float dt)
        {
            float speed = _motor != null ? _motor.State.HorizontalSpeed : 0f;
            float t = settings.fovSpeedMax > settings.fovSpeedMin
                ? Mathf.InverseLerp(settings.fovSpeedMin, settings.fovSpeedMax, speed)
                : 0f;
            float target = settings.baseFov + settings.fovBonusAtMaxSpeed * t * GameFeel.Intensity;   // intensité des effets (point 9)
            _fov = Mathf.Lerp(_fov, target, 1f - Mathf.Exp(-settings.fovLerpSpeed * dt));

            if (virtualCamera != null)
            {
                LensSettings lens = virtualCamera.Lens;
                lens.FieldOfView = Mathf.Clamp((_fov + FovPunch) * FovScale, 20f, 150f);
                virtualCamera.Lens = lens;
            }
        }

        private void HandleCursor()
        {
            if (_reader == null)
                return;

            if (MenuOpen)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                    SetCursorLocked(false);
                return;
            }

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

        /// <summary>Un menu (écran de fin, marchand…) a besoin de la souris : le curseur reste libre.</summary>
        public static bool MenuOpen { get; set; }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
