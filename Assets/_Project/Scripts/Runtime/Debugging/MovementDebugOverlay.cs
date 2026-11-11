using System.Text;
using ItHunts.Input;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Affichage de debug du mouvement (F3 au clavier, Select à la manette) : vitesses, état,
    /// pente, compteurs coyote et buffer, gain de vitesse entre deux sauts.
    /// </summary>
    public sealed class MovementDebugOverlay : MonoBehaviour
    {
        [SerializeField] private bool visibleOnStart = true;

        private const int HistoryTicks = 60;

        private PlayerMotor _motor;
        private PlayerInputReader _reader;
        private PlayerLook _look;
        private DebugTeleporter _teleporter;
        private readonly float[] _speedHistory = new float[HistoryTicks];
        private int _historyIndex;
        private bool _visible;
        private int _seenJumpTick = MovementState.NeverTick;
        private float _speedAtLastJump;
        private float _lastJumpGain;
        private GUIStyle _style;
        private readonly StringBuilder _text = new StringBuilder(512);

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _reader = GetComponent<PlayerInputReader>();
            _look = GetComponent<PlayerLook>();
            _teleporter = GetComponent<DebugTeleporter>();
            _visible = visibleOnStart;
        }

        private void FixedUpdate()
        {
            if (_motor == null)
                return;

            MovementState s = _motor.State;
            float speed = s.HorizontalSpeed;
            _speedHistory[_historyIndex] = speed;
            _historyIndex = (_historyIndex + 1) % HistoryTicks;

            if (s.LastJumpTick != _seenJumpTick && s.LastJumpTick != MovementState.NeverTick)
            {
                _lastJumpGain = _seenJumpTick == MovementState.NeverTick ? 0f : speed - _speedAtLastJump;
                _speedAtLastJump = speed;
                _seenJumpTick = s.LastJumpTick;
            }
        }

        private void Update()
        {
            if (_reader != null && _reader.ToggleDebugPressed)
                _visible = !_visible;
        }

        private void OnGUI()
        {
            if (!_visible || _motor == null || _motor.Settings == null)
                return;

            _style ??= new GUIStyle(GUI.skin.box)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(10, 10, 8, 8),
            };

            MovementState s = _motor.State;
            MovementSettings cfg = _motor.Settings;
            float dt = Time.fixedDeltaTime;
            int coyoteTicks = MovementSettings.SecondsToTicks(cfg.coyoteTime, dt);
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.jumpBuffer, dt);

            int coyoteLeft = s.Grounded ? coyoteTicks
                : s.LastGroundedTick == MovementState.NeverTick ? 0
                : Mathf.Max(0, coyoteTicks - (s.Tick - s.LastGroundedTick));

            int bufferLeft = 0;
            if (_reader != null && _reader.JumpPressedTick > s.LastConsumedJumpPressTick)
                bufferLeft = Mathf.Max(0, bufferTicks - (s.Tick - _reader.JumpPressedTick));

            float peak = 0f;
            foreach (float v in _speedHistory)
                peak = Mathf.Max(peak, v);

            string scheme = _reader == null ? "?"
                : _reader.UsingGamepad ? "Manette"
                : _reader.ControlScheme == PlayerInputReader.KeyboardMouseScheme ? "Clavier-souris"
                : _reader.ControlScheme ?? "?";

            _text.Clear();
            _text.AppendLine("Debug mouvement (F3 / Select)");
            _text.AppendLine($"Contrôle : {scheme}");
            _text.AppendLine($"Vitesse horizontale : {s.HorizontalSpeed:0.00} m/s   (pic sur 1 s : {peak:0.00})");
            _text.AppendLine($"Vitesse verticale : {s.Velocity.y:0.00} m/s");
            string mode = s.Mode switch
            {
                MovementMode.Ground => s.Crouched ? "Sol (accroupi)" : "Sol",
                MovementMode.Slide => "Glissade",
                _ => "Air",
            };
            int boostTicks = MovementSettings.SecondsToTicks(cfg.slideBoostCooldown, dt);
            int boostLeft = s.LastSlideBoostTick == MovementState.NeverTick ? 0
                : Mathf.Max(0, boostTicks - (s.Tick - s.LastSlideBoostTick));
            _text.AppendLine($"État : {mode}   Au sol : {(s.Grounded ? "oui" : "non")}");
            _text.AppendLine(boostLeft > 0
                ? $"Bonus de glissade : recharge {boostLeft * dt:0.0} s"
                : "Bonus de glissade : prêt");
            _text.AppendLine($"Pente : {Vector3.Angle(s.GroundNormal, Vector3.up):0.0}°");
            _text.AppendLine($"Coyote : {coyoteLeft}/{coyoteTicks} ticks   Buffer : {bufferLeft}/{bufferTicks} ticks");
            _text.AppendLine($"Sauts restants : {Mathf.Max(0, cfg.maxJumps - s.JumpsUsed)}/{cfg.maxJumps}   " +
                             $"Auto-bhop : {(cfg.autoBunnyHop ? "oui" : "non")}   Contrôle aérien : {cfg.airControl:0.00}");
            _text.AppendLine(_seenJumpTick == MovementState.NeverTick
                ? "Dernier saut : —"
                : $"Dernier saut : {_speedAtLastJump:0.00} m/s (gain {_lastJumpGain:+0.00;-0.00;0.00})");
            if (_teleporter != null)
                _text.AppendLine(string.IsNullOrEmpty(_teleporter.LastLabel)
                    ? "Téléport : F4 départ, F5 ouvert, F6 ligne droite, F7 pentes, F8 murs, F9 écarts"
                    : $"Téléport : {_teleporter.LastLabel}   (F4 départ, F5 à F9 zones)");
            _text.Append($"Tick : {s.Tick} ({Mathf.RoundToInt(1f / dt)} Hz)");
            if (_look != null)
                _text.Append($"   FOV : {_look.CurrentFov:0.0}°");

            int index = _reader != null ? _reader.PlayerIndex : 0;
            var content = new GUIContent(_text.ToString());
            Vector2 size = _style.CalcSize(content);
            GUI.Box(new Rect(10f + index * (size.x + 10f), 10f, size.x, size.y), content, _style);
        }
    }
}
