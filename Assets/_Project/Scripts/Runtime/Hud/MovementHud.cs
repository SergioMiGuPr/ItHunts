using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Hud
{
    /// <summary>
    /// HUD minimal toujours visible, sous le viseur :
    /// - rangée 1 : charges de mouvement (saut en l'air, dash) ;
    /// - rangée 2 : charges d'attraction (bleu) puis la répulsion (orange : pleine, ou remplissage et temps restant).
    /// Segments pleins, vides, ou en cours de recharge. Dessiné dans la zone de la caméra du joueur (prêt pour l'écran
    /// partagé). Version greybox en IMGUI ; le vrai HUD viendra à l'étape 5.
    /// </summary>
    public sealed class MovementHud : MonoBehaviour
    {
        [SerializeField] private Color filledColor = new Color(0.35f, 0.85f, 1f, 0.95f);
        [SerializeField] private Color rechargeColor = new Color(0.35f, 0.85f, 1f, 0.45f);
        [SerializeField] private Color attractColor = new Color(0.25f, 0.45f, 1f, 0.95f);
        [SerializeField] private Color repelColor = new Color(1f, 0.55f, 0.1f, 0.95f);
        [SerializeField] private Color emptyColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField, Min(1f)] private float segmentWidth = 22f;
        [SerializeField, Min(1f)] private float segmentHeight = 6f;
        [SerializeField, Min(0f)] private float segmentGap = 4f;
        [SerializeField, Min(0f)] private float rowGap = 6f;
        [SerializeField, Min(0f)] private float groupGap = 14f;
        [SerializeField] private float offsetBelowCrosshair = 26f;

        private PlayerMotor _motor;
        private Camera _camera;
        private GUIStyle _timeStyle;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            var playerInput = GetComponent<PlayerInput>();
            _camera = playerInput != null ? playerInput.camera : null;
        }

        private void OnGUI()
        {
            if (_motor == null || _motor.Settings == null || Event.current.type != EventType.Repaint)
                return;

            // Zone de l'écran du joueur (IMGUI : origine en haut à gauche).
            Rect pixels = _camera != null ? _camera.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
            var center = new Vector2(pixels.x + pixels.width * 0.5f, Screen.height - (pixels.y + pixels.height * 0.5f));

            Fill(new Rect(center.x - 2f, center.y - 2f, 4f, 4f), crosshairColor);

            // Rangée 1 : charges de mouvement.
            float y = center.y + offsetBelowCrosshair;
            int count = _motor.Settings.movementCharges;
            if (count > 0)
            {
                float total = Width(count);
                Segments(center.x - total * 0.5f, y, count, Mathf.Max(0f, _motor.State.Charges), filledColor, rechargeColor);
            }

            // Rangée 2 : attraction (charges) et répulsion (charge unique).
            MagnetismOrbSettings mc = _motor.MagnetismSettings;
            if (mc == null)
                return;
            MagnetismState m = _motor.Magnetism;
            y += segmentHeight + rowGap;
            int attract = mc.attractCharges;
            float row = Width(attract) + groupGap + segmentWidth;
            float x = center.x - row * 0.5f;
            Color attractRecharge = attractColor;
            attractRecharge.a *= 0.45f;
            Segments(x, y, attract, m.AttractCharges < 0f ? attract : m.AttractCharges, attractColor, attractRecharge);

            float repelX = x + Width(attract) + groupGap;
            float remaining = RepelRemaining(m, mc);
            float fill = mc.repelCooldown > 0f ? 1f - remaining / mc.repelCooldown : 1f;
            Color repelRecharge = repelColor;
            repelRecharge.a *= 0.45f;
            Segments(repelX, y, 1, remaining > 0f ? Mathf.Clamp01(fill) : 1f, repelColor, repelRecharge);

            if (remaining > 0f)
            {
                _timeStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
                _timeStyle.normal.textColor = repelColor;
                GUI.Label(new Rect(repelX + segmentWidth + 4f, y - 6f, 60f, 18f), $"{remaining:0.0} s", _timeStyle);
            }
        }

        /// <summary>Temps restant avant que la répulsion soit de nouveau prête (s).</summary>
        private float RepelRemaining(in MagnetismState m, MagnetismOrbSettings mc)
        {
            if (m.LastRepelTick == ItHunts.Movement.MovementState.NeverTick)
                return 0f;
            float elapsed = (_motor.State.Tick - m.LastRepelTick) * Time.fixedDeltaTime;
            return Mathf.Max(0f, mc.repelCooldown - elapsed);
        }

        private float Width(int count) => count * segmentWidth + (count - 1) * segmentGap;

        private void Segments(float x, float y, int count, float value, Color full, Color partial)
        {
            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(x + i * (segmentWidth + segmentGap), y, segmentWidth, segmentHeight);
                Fill(rect, emptyColor);
                float fill = Mathf.Clamp01(value - i);
                if (fill >= 1f)
                    Fill(rect, full);
                else if (fill > 0f)
                    Fill(new Rect(rect.x, rect.y, rect.width * fill, rect.height), partial);
            }
        }

        private static void Fill(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
