using ItHunts.Input;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Téléportation de debug : F4 = départ, F5 à F11 = zones de la scène de test (DebugTeleportPoints).
    /// Outil de test seulement : lit le clavier directement, et seulement pour un joueur au clavier-souris.
    /// </summary>
    public sealed class DebugTeleporter : MonoBehaviour
    {
        private static readonly Key[] Keys = { Key.F4, Key.F5, Key.F6, Key.F7, Key.F8, Key.F9, Key.F10, Key.F11 };

        private PlayerMotor _motor;
        private PlayerLook _look;
        private PlayerInputReader _reader;
        private DebugTeleportPoints _points;

        /// <summary>Libellé de la dernière téléportation (affiché par le debug).</summary>
        public string LastLabel { get; private set; }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _look = GetComponent<PlayerLook>();
            _reader = GetComponent<PlayerInputReader>();
        }

        private void Start() => _points = FindAnyObjectByType<DebugTeleportPoints>();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _points == null || _motor == null)
                return;
            if (_reader != null && _reader.UsingGamepad)
                return;

            for (int i = 0; i < Keys.Length && i < _points.points.Length; i++)
            {
                if (!keyboard[Keys[i]].wasPressedThisFrame)
                    continue;

                DebugTeleportPoints.Point point = _points.points[i];
                if (point.target == null)
                    return;
                _motor.Teleport(point.target.position);
                if (_look != null)
                    _look.SetView(point.target.eulerAngles.y, 0f);
                LastLabel = $"{Keys[i]} : {point.label}";
                return;
            }
        }
    }
}
