using ItHunts.Enemies;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// Barre de vie du boss final dans la DA (nuit 3, point 8) : la barre peinte de la DA (cadre, fond, remplissage) en grand, en haut,
    /// au centre, son nom à la police de titre de la DA, les deux crans de phase, une traînée qui suit les dégâts, et « Points faibles :
    /// 3 » dessous. Apparaît quand le combat commence.
    /// </summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        private FinalBoss _boss;
        private Image _fill, _trail;
        private Text _name, _info;
        private float _shown = 1f;
        private const float Width = 980f, Height = 30f;

        public static BossHealthBar Show(FinalBoss boss)
        {
            var go = new GameObject("Barre de vie du boss (DA)");
            var bar = go.AddComponent<BossHealthBar>();
            bar._boss = boss;
            bar.Build();
            return bar;
        }

        private void Build()
        {
            HudSkinSettings skin = null;
            foreach (PlayerHud h in FindObjectsByType<PlayerHud>(FindObjectsSortMode.None))
                if (h.Skin != null)
                {
                    skin = h.Skin;
                    break;
                }

            Canvas c = UiKit.NewCanvas("Boss", 60, transform);
            RectTransform root = UiKit.Rect("Barre", c.transform, new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(Width, Height));
            Image back = Layer(root, "Fond", skin != null ? skin.healthBack : null, new Color(0.08f, 0.05f, 0.06f, 0.9f), Width, Height);
            _trail = Layer(root, "Traînée", skin != null ? skin.healthTrail : null, new Color(1f, 0.85f, 0.6f, 0.9f), Width - 16f, Height - 10f);
            _fill = Layer(root, "Vie", skin != null ? skin.healthFill : null, skin != null && skin.healthFill != null ? Color.white : new Color(0.7f, 0.08f, 0.06f), Width - 16f, Height - 10f);
            foreach (Image i in new[] { _trail, _fill })
            {
                i.type = Image.Type.Filled;
                i.fillMethod = Image.FillMethod.Horizontal;
            }

            Image frame = Layer(root, "Cadre", skin != null ? skin.healthFrame : null, Color.white, Width + 40f, Height + 26f);
            frame.enabled = skin != null && skin.healthFrame != null;
            back.raycastTarget = false;
            // Crans des phases (66 %, 33 %).
            foreach (float k in new[] { 0.66f, 0.33f })
            {
                RectTransform notch = UiKit.Rect("Cran", root, new Vector2(0f, 0.5f), new Vector2((Width - 16f) * k + 8f, 0f), new Vector2(4f, Height + 6f));
                notch.gameObject.AddComponent<Image>().color = new Color(0.95f, 0.85f, 0.6f, 0.9f);
            }

            Font title = skin != null && skin.titleFont != null ? skin.titleFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font text = skin != null && skin.textFont != null ? skin.textFont : title;
            _name = Label(c.transform, title, 40, new Vector2(0f, -48f), new Color(0.95f, 0.86f, 0.7f));
            _name.text = "Le Traqueur";
            _info = Label(c.transform, text, 24, new Vector2(0f, -132f), new Color(0.85f, 0.78f, 0.62f));
        }

        private static Image Layer(RectTransform parent, string name, Sprite s, Color color, float w, float h)
        {
            RectTransform r = UiKit.Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.type = s != null && s.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text Label(Transform parent, Font f, int size, Vector2 pos, Color color)
        {
            RectTransform r = UiKit.Rect("Texte", parent, new Vector2(0.5f, 1f), pos, new Vector2(1200f, size + 16f));
            var t = r.gameObject.AddComponent<Text>();
            t.font = f;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = r.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        private void Update()
        {
            if (_boss == null)
            {
                Destroy(gameObject);
                return;
            }

            float k = _boss.MaxHealth > 0f ? _boss.Health / _boss.MaxHealth : 0f;
            _fill.fillAmount = k;
            _shown = Mathf.MoveTowards(_shown, k, Time.deltaTime * 0.35f);
            _shown = Mathf.Max(_shown, k);
            _trail.fillAmount = _shown;
            _name.text = _boss.Dead ? "Le Traqueur est tombé" : _boss.Phase >= 3 ? "Le Traqueur — enragé" : "Le Traqueur";
            _info.text = _boss.Dead ? "" : $"Phase {_boss.Phase} / 3   ·   points faibles lumineux : {_boss.WeakPointsOpen}";
        }
    }
}
