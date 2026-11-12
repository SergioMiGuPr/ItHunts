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
    /// - rangée 2 : charges d'attraction (bleu) puis la répulsion (orange : pleine, ou remplissage et temps restant) ;
    ///   le groupe d'un mode refusé clignote en rouge ;
    /// - viseur : anneaux de l'orbe unique (D-024), toutes les actions en même temps : bleu (traction possible), orange
    ///   (répulsion possible), puis à l'extérieur violet (objet visé attrapable) ou vert (objet tenu, lancer possible) ;
    ///   l'anneau extérieur clignote en rouge après un refus de la télékinésie. Pas d'endurance.
    /// Segments pleins, vides, ou en cours de recharge. Dessiné dans la zone de la caméra du joueur (prêt pour l'écran
    /// partagé). Version greybox en IMGUI ; le vrai HUD viendra à l'étape 5.
    /// </summary>
    public sealed class MovementHud : MonoBehaviour
    {
        [SerializeField] private Color filledColor = new Color(0.35f, 0.85f, 1f, 0.95f);
        [SerializeField] private Color rechargeColor = new Color(0.35f, 0.85f, 1f, 0.45f);
        [Tooltip("Couleurs de l'attraction, de la répulsion et du refus, anneaux du viseur, clignotement.")]
        [SerializeField] private MagnetismFeedbackSettings feedback;
        [Tooltip("Couleurs de la télékinésie (anneaux violet et vert).")]
        [SerializeField] private TelekinesisFeedbackSettings tkFeedback;
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
        private readonly System.Collections.Generic.Dictionary<Vector2, Texture2D> _rings =
            new System.Collections.Generic.Dictionary<Vector2, Texture2D>();

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
            DrawRings(center);

            // Rangée 1 : charges de mouvement.
            float y = center.y + offsetBelowCrosshair;
            int count = _motor.Settings.movementCharges;
            if (count > 0)
            {
                float total = Width(count);
                Segments(center.x - total * 0.5f, y, count, Mathf.Max(0f, _motor.State.Charges), filledColor, rechargeColor, emptyColor);
            }

            // Rangée 2 : magnétisme (charges d'attraction, répulsion).
            y += segmentHeight + rowGap;
            if (_motor.MagnetismSettings != null)
                DrawMagnetismRow(center, y, 1f);
        }

        /// <summary>Charges d'attraction (bleu) puis répulsion (orange, temps restant) ; renvoie la largeur de la rangée.</summary>
        private float DrawMagnetismRow(Vector2 center, float y, float alpha)
        {
            MagnetismOrbSettings mc = _motor.MagnetismSettings;
            MagnetismState m = _motor.Magnetism;
            Color attractColor = Opaque(feedback != null ? feedback.attractColor : new Color(0.25f, 0.45f, 1f));
            Color repelColor = Opaque(feedback != null ? feedback.repelColor : new Color(1f, 0.55f, 0.1f));
            bool attractBlink = Blink(m, MagnetMode.Attraction);
            bool repelBlink = Blink(m, MagnetMode.Repulsion);
            if (attractBlink)
                attractColor = feedback.refusalColor;
            if (repelBlink)
                repelColor = feedback.refusalColor;
            attractColor.a *= alpha;
            repelColor.a *= alpha;
            Color empty = emptyColor;
            empty.a *= alpha;

            int attract = mc.attractCharges;
            float row = Width(attract) + groupGap + segmentWidth;
            float x = center.x - row * 0.5f;
            Color attractRecharge = attractColor;
            attractRecharge.a *= 0.45f;
            Segments(x, y, attract, m.AttractCharges < 0f ? attract : m.AttractCharges, attractColor, attractRecharge,
                attractBlink ? feedback.refusalColor : empty);

            float repelX = x + Width(attract) + groupGap;
            float remaining = RepelRemaining(m, mc);
            float fill = mc.repelCooldown > 0f ? 1f - remaining / mc.repelCooldown : 1f;
            Color repelRecharge = repelColor;
            repelRecharge.a *= 0.45f;
            Segments(repelX, y, 1, remaining > 0f ? Mathf.Clamp01(fill) : 1f, repelColor, repelRecharge,
                repelBlink ? feedback.refusalColor : empty);

            if (remaining > 0f)
            {
                _timeStyle ??= new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleLeft };
                _timeStyle.normal.textColor = repelColor;
                GUI.Label(new Rect(repelX + segmentWidth + 4f, y - 6f, 60f, 18f), $"{remaining:0.0} s", _timeStyle);
            }

            return row;
        }

        private static Color Opaque(Color c) => new Color(c.r, c.g, c.b, 0.95f);

        private bool Blink(in MagnetismState m, MagnetMode mode) =>
            feedback != null && MagnetismFeedback.RefusalBlinkOn(m, mode, _motor.State.Tick, Time.fixedDeltaTime,
                feedback.refusalFlashDuration, feedback.refusalBlinkPeriod);

        /// <summary>
        /// Anneaux du viseur, toutes les actions de l'orbe en même temps : bleu (traction possible), orange (répulsion
        /// possible), puis violet (objet visé attrapable) ou vert (objet tenu : lancer possible) ; rouge un court instant
        /// après un refus de la télékinésie (rien d'attrapable, trop lourd, recharge).
        /// </summary>
        private void DrawRings(Vector2 center)
        {
            if (feedback == null)
                return;
            if (_motor.MagnetismSettings != null)
            {
                MagnetismState m = _motor.Magnetism;
                if (MagnetismFeedback.CanAttract(m))
                    DrawRing(center, Ring(feedback.attractRingRadius), Opaque(feedback.attractColor));
                if (MagnetismFeedback.CanRepel(m))
                    DrawRing(center, Ring(feedback.repelRingRadius), Opaque(feedback.repelColor));
            }

            if (tkFeedback == null || _motor.TelekinesisSettings == null)
                return;
            TelekinesisState t = _motor.Telekinesis;
            bool refused = MagnetismFeedback.BlinkOn(t.LastRefusalTick, _motor.State.Tick, Time.fixedDeltaTime,
                feedback.refusalFlashDuration, feedback.refusalBlinkPeriod);
            if (refused)
                DrawRing(center, Ring(tkFeedback.grabRingRadius), feedback.refusalColor);
            else if (TelekinesisOrb.CanThrow(t))
                DrawRing(center, Ring(tkFeedback.throwRingRadius), Opaque(tkFeedback.throwColor));
            else if (TelekinesisOrb.CanGrab(t, _motor.TelekinesisSettings, _motor.State.Tick, Time.fixedDeltaTime))
                DrawRing(center, Ring(tkFeedback.grabRingRadius), Opaque(tkFeedback.grabColor));
        }

        private static void DrawRing(Vector2 center, Texture2D tex, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - tex.width * 0.5f, center.y - tex.height * 0.5f, tex.width, tex.height), tex);
            GUI.color = previous;
        }

        /// <summary>Texture d'anneau anticrénelée, une par rayon et épaisseur.</summary>
        private Texture2D Ring(float radius)
        {
            float thickness = feedback.ringThickness;
            var key = new Vector2(radius, thickness);
            if (_rings.TryGetValue(key, out Texture2D cached) && cached != null)
                return cached;
            int size = Mathf.CeilToInt((radius + thickness) * 2f) + 4;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave,
            };
            var pixels = new Color32[size * size];
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(thickness * 0.5f + 0.5f - Mathf.Abs(d - radius));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            _rings[key] = tex;
            return tex;
        }

        private void OnDestroy()
        {
            foreach (Texture2D tex in _rings.Values)
                if (tex != null)
                    Destroy(tex);
            _rings.Clear();
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

        private void Segments(float x, float y, int count, float value, Color full, Color partial, Color empty)
        {
            for (int i = 0; i < count; i++)
            {
                var rect = new Rect(x + i * (segmentWidth + segmentGap), y, segmentWidth, segmentHeight);
                Fill(rect, empty);
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
