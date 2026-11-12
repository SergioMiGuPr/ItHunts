using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Hud
{
    /// <summary>
    /// HUD minimal toujours visible : point de visée, et une rangée de segments sous le viseur pour les charges de
    /// mouvement (pleins, vides, et remplissage de la charge en recharge). Dessiné dans la zone de la caméra du
    /// joueur (prêt pour l'écran partagé). Version greybox en IMGUI ; le vrai HUD viendra à l'étape 5.
    /// </summary>
    public sealed class MovementHud : MonoBehaviour
    {
        [SerializeField] private Color filledColor = new Color(0.35f, 0.85f, 1f, 0.95f);
        [SerializeField] private Color rechargeColor = new Color(0.35f, 0.85f, 1f, 0.45f);
        [SerializeField] private Color emptyColor = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color crosshairColor = new Color(1f, 1f, 1f, 0.85f);
        [SerializeField, Min(1f)] private float segmentWidth = 22f;
        [SerializeField, Min(1f)] private float segmentHeight = 6f;
        [SerializeField, Min(0f)] private float segmentGap = 4f;
        [SerializeField] private float offsetBelowCrosshair = 26f;

        private PlayerMotor _motor;
        private Camera _camera;

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

            int count = _motor.Settings.movementCharges;
            if (count <= 0)
                return;
            float charges = Mathf.Max(0f, _motor.State.Charges);
            float total = count * segmentWidth + (count - 1) * segmentGap;
            float x = center.x - total * 0.5f;
            float y = center.y + offsetBelowCrosshair;
            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(x + i * (segmentWidth + segmentGap), y, segmentWidth, segmentHeight);
                Fill(rect, emptyColor);
                float fill = Mathf.Clamp01(charges - i);
                if (fill >= 1f)
                    Fill(rect, filledColor);
                else if (fill > 0f)
                    Fill(new Rect(rect.x, rect.y, rect.width * fill, rect.height), rechargeColor);
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
