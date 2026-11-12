using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Combat
{
    /// <summary>
    /// Sur le joueur : chiffres de dégâts flottants (montent et s'effacent, taille et couleur selon le coup, « K.O. » à
    /// la mort), barres de PV au-dessus des cibles proches, et durabilité de l'objet tenu (un carré par lancer : plein =
    /// encore tenu, sombre = déjà encaissé), dessinés pour la caméra de CE joueur (prêt pour l'écran partagé). Greybox
    /// en IMGUI.
    /// </summary>
    public sealed class DamageNumbersHud : MonoBehaviour
    {
        [SerializeField] private DamageFeedbackSettings settings;

        private struct Number
        {
            public Vector3 Point;
            public string Text;
            public Color Color;
            public float Size;
            public float Born;
        }

        private readonly List<Number> _numbers = new List<Number>();
        private Camera _camera;
        private ItHunts.Player.PlayerMotor _motor;
        private GUIStyle _style;

        private void Awake()
        {
            var playerInput = GetComponent<PlayerInput>();
            _camera = playerInput != null ? playerInput.camera : null;
            _motor = GetComponent<ItHunts.Player.PlayerMotor>();
        }

        private void OnEnable() => DamageEvents.Hit += OnHit;
        private void OnDisable() => DamageEvents.Hit -= OnHit;

        private void OnHit(DamageEvent e)
        {
            if (settings == null)
                return;
            Color color = e.Killed ? settings.killColor
                : e.Tier == HitTier.Gros ? settings.bigColor
                : e.Tier == HitTier.Moyen ? settings.mediumColor
                : settings.smallColor;
            float size = Mathf.Min(settings.numberMaxSize, settings.numberBaseSize + settings.numberSizePerDamage * e.Damage);
            Vector3 start = e.Target != null ? Vector3.Lerp(e.Point, e.Target.Top, 0.5f) : e.Point;
            _numbers.Add(new Number { Point = start, Text = Mathf.CeilToInt(e.Damage).ToString(), Color = color, Size = size, Born = Time.time });
            if (e.Killed)
                _numbers.Add(new Number { Point = start + Vector3.up * 0.5f, Text = "K.O.", Color = settings.killColor, Size = settings.numberMaxSize, Born = Time.time });
        }

        private void OnGUI()
        {
            if (settings == null || Event.current.type != EventType.Repaint)
                return;
            Camera cam = _camera != null ? _camera : Camera.main;
            if (cam == null)
                return;
            _style ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            DrawHealthBars(cam);
            DrawDurability(cam);

            for (int i = _numbers.Count - 1; i >= 0; i--)
            {
                Number n = _numbers[i];
                float age = Time.time - n.Born;
                if (age > settings.numberDuration)
                {
                    _numbers.RemoveAt(i);
                    continue;
                }

                float t = age / settings.numberDuration;
                Vector3 world = n.Point + Vector3.up * (settings.numberRise * (1f - (1f - t) * (1f - t)));
                if (!ToScreen(cam, world, out Vector2 screen))
                    continue;
                Color c = n.Color;
                c.a = 1f - t * t;
                _style.fontSize = Mathf.RoundToInt(n.Size * (t < 0.1f ? 1f + (0.1f - t) * 4f : 1f));   // petit « pop » au départ
                var rect = new Rect(screen.x - 80f, screen.y - 25f, 160f, 50f);
                _style.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
                GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), n.Text, _style);   // ombre
                _style.normal.textColor = c;
                GUI.Label(rect, n.Text, _style);
            }
        }

        private void DrawHealthBars(Camera cam)
        {
            foreach (Damageable d in Damageable.All)
            {
                if (d == null || d.Dead)
                    continue;
                Vector3 top = d.Top + Vector3.up * 0.35f;
                if (Vector3.Distance(cam.transform.position, top) > settings.healthBarDistance || !ToScreen(cam, top, out Vector2 p))
                    continue;
                HealthState h = d.Health;
                float w = settings.healthBarWidth, hgt = settings.healthBarHeight;
                var back = new Rect(p.x - w * 0.5f, p.y - hgt * 0.5f, w, hgt);
                Fill(back, new Color(0f, 0f, 0f, 0.7f));
                float k = h.Max > 0f ? h.Current / h.Max : 0f;
                Fill(new Rect(back.x, back.y, back.width * k, back.height), Color.Lerp(settings.bigColor, new Color(0.3f, 1f, 0.3f), k));
            }
        }

        /// <summary>Durabilité de l'objet tenu : une rangée de carrés au-dessus de lui.</summary>
        private void DrawDurability(Camera cam)
        {
            ItHunts.Orbs.Grabbable held = _motor != null && _motor.Holder != null ? _motor.Holder.Held : null;
            if (held == null || held.MaxDurability <= 0 || held.Colliders == null || held.Colliders.Length == 0)
                return;
            Bounds b = held.Colliders[0].bounds;
            if (!ToScreen(cam, new Vector3(b.center.x, b.max.y + 0.15f, b.center.z), out Vector2 p))
                return;
            float size = settings.durabilityPipSize, gap = 3f;
            int n = held.MaxDurability;
            float x = p.x - (n * size + (n - 1) * gap) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var rect = new Rect(x + i * (size + gap), p.y - size, size, size);
                Fill(new Rect(rect.x - 1f, rect.y - 1f, rect.width + 2f, rect.height + 2f), new Color(0f, 0f, 0f, 0.7f));
                Fill(rect, i < held.ThrowsLeft ? settings.durabilityFull : settings.durabilityUsed);
            }
        }

        /// <summary>Position à l'écran (IMGUI, origine en haut à gauche), seulement devant la caméra et dans sa zone.</summary>
        private static bool ToScreen(Camera cam, Vector3 world, out Vector2 screen)
        {
            Vector3 s = cam.WorldToScreenPoint(world);
            screen = new Vector2(s.x, Screen.height - s.y);
            return s.z > 0.1f && cam.pixelRect.Contains(new Vector2(s.x, s.y));
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
