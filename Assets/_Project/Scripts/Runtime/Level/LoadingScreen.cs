using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Level
{
    /// <summary>
    /// Écran de chargement simple (greybox) pendant la construction de la carte aléatoire : fond noir, titre, étape en cours et barre
    /// de progression, au lieu d'une image figée. Sa propre caméra (les caméras des joueurs ne sont pas encore actives) ; détruit
    /// quand la carte est prête. À habiller à l'étape 10 (DA).
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        /// <summary>Niveau en construction (carte aléatoire ou acte).</summary>
        public ILevelLoading Source;

        private GUIStyle _title, _text;
        private Texture2D _white;
        private Text _titleText, _stageText;
        private Image _fill;
        private bool _built;

        private void Awake()
        {
            var cam = gameObject.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            cam.cullingMask = 0;
            cam.depth = 100f;
            _white = Texture2D.whiteTexture;
        }

        /// <summary>
        /// Écran dans la DA (nuit 3, point 1) : polices de la DA, barre de vie peinte de la DA comme barre de progression. Repli greybox
        /// (IMGUI) seulement si l'habillage manque.
        /// </summary>
        private void Build()
        {
            _built = true;
            ItHunts.Settings.HudSkinSettings skin = null;
            foreach (ItHunts.Hud.PlayerHud h in FindObjectsByType<ItHunts.Hud.PlayerHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (h.Skin != null)
                {
                    skin = h.Skin;
                    break;
                }

            if (skin == null || skin.textFont == null)
                return;
            Canvas c = ItHunts.Hud.UiKit.NewCanvas("Chargement (DA)", 200, transform);
            var bg = new GameObject("Fond", typeof(RectTransform)).AddComponent<Image>();
            bg.transform.SetParent(c.transform, false);
            bg.color = new Color(0.015f, 0.014f, 0.02f);
            ItHunts.Hud.UiKit.Stretch(bg.rectTransform);
            _titleText = Label(c.transform, skin.titleFont != null ? skin.titleFont : skin.textFont, 72, new Color(0.9f, 0.82f, 0.68f), new Vector2(0f, 90f));
            _stageText = Label(c.transform, skin.textFont, 30, new Color(0.7f, 0.66f, 0.6f), new Vector2(0f, -10f));
            var frame = ItHunts.Hud.UiKit.Rect("Barre", c.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(640f, 34f));
            Image back = Slice(frame, skin.healthBack != null ? skin.healthBack : null, new Color(0.08f, 0.07f, 0.08f));
            var fillRect = ItHunts.Hud.UiKit.Rect("Remplissage", frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 22f));
            _fill = fillRect.gameObject.AddComponent<Image>();
            _fill.sprite = skin.healthFill;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.color = skin.healthFill != null ? Color.white : new Color(0.75f, 0.2f, 0.12f);
            _fill.raycastTarget = false;
            if (skin.healthFrame != null)
            {
                var fr = ItHunts.Hud.UiKit.Rect("Cadre", frame, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, 44f));
                Slice(fr, skin.healthFrame, Color.white);
            }

            back.raycastTarget = false;
        }

        private static Image Slice(RectTransform r, Sprite s, Color color)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = s;
            img.type = s != null && s.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private static Text Label(Transform parent, Font font, int size, Color color, Vector2 pos)
        {
            var r = ItHunts.Hud.UiKit.Rect("Texte", parent, new Vector2(0.5f, 0.5f), pos, new Vector2(1600f, size + 20f));
            var t = r.gameObject.AddComponent<Text>();
            t.font = font;
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
            if (Source == null)
                return;
            if (!_built)
                Build();
            if (_titleText == null)
                return;
            _titleText.text = string.IsNullOrEmpty(Source.Title) ? "Les caves du château" : Source.Title;
            _stageText.text = Source.Stage;
            _fill.fillAmount = Mathf.Clamp01(Source.Progress);
        }

        private void OnGUI()
        {
            if (Source == null || _titleText != null)
                return;
            float s = Screen.height / 1080f;
            _title ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            _text ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            _title.fontSize = Mathf.RoundToInt(48 * s);
            _text.fontSize = Mathf.RoundToInt(24 * s);
            _title.normal.textColor = new Color(0.85f, 0.8f, 0.7f);
            _text.normal.textColor = new Color(0.65f, 0.62f, 0.58f);
            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0f, h * 0.36f, w, 70f * s), string.IsNullOrEmpty(Source.Title) ? "Les caves du château" : Source.Title, _title);
            GUI.Label(new Rect(0f, h * 0.45f, w, 40f * s), Source.Stage, _text);
            var bar = new Rect(w * 0.3f, h * 0.53f, w * 0.4f, 14f * s);
            GUI.color = new Color(0.15f, 0.14f, 0.16f);
            GUI.DrawTexture(bar, _white);
            GUI.color = new Color(0.9f, 0.55f, 0.25f);
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(Source.Progress), bar.height), _white);
            GUI.color = Color.white;
        }
    }

    /// <summary>Source de l'écran de chargement (carte aléatoire ou acte).</summary>
    public interface ILevelLoading
    {
        float Progress { get; }
        string Stage { get; }
        string Title { get; }
    }
}
