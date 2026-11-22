using ItHunts.Hud;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Menus
{
    /// <summary>
    /// Vue de l'écran de fin de run (nuit 3, point 3) dans la DA : le cadre de l'écran Paramètres de la DA élargi, titre « Victoire » ou
    /// « Mort », la cause de la mort sous le titre, deux colonnes lisibles (libellé, valeur) aux polices de la DA, puis « Retour au
    /// campement » ; R (ou Y) : nouvelle run tout de suite. Posée par MenuArtInstall.
    /// </summary>
    public sealed class RunEndScreenArt : MonoBehaviour
    {
        private const float FrameW = 1180f, FrameH = 860f, ScreenW = 1672f;
        private RunEndScreen _end;
        private GameObject _screen;
        private Selectable _back;
        private Camera _cam;

        public GameObject Screen => _screen;

        public void Attach(RunEndScreen end)
        {
            if (end.Skinned)
                return;                                                   // l'écran de fin de la DA (PR #42) est déjà posé
            _end = end;
            PlayerInput pi = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
            MenuSkinSettings skin = PauseMenu.SkinOf(pi);
            if (skin == null || skin.settingsScreen == null)
                return;
            Canvas canvas = UiKit.NewCanvas("Écran de fin (DA)", 95, transform);
            _screen = MenuArtKit.Spawn(skin.settingsScreen, canvas.transform, "Menu_FinDeRun");
            Transform t = _screen.transform;
            for (int i = 0; ; i++)
            {
                Transform o = t.Find("Option_" + i);
                if (o == null)
                    break;
                o.gameObject.SetActive(false);
            }

            float x0 = (ScreenW - FrameW) * 0.5f;
            if (t.Find("Cadre") is RectTransform frame)
            {
                frame.anchoredPosition = new Vector2(x0, -50f);
                frame.sizeDelta = new Vector2(FrameW, FrameH);
            }

            Text title = MenuArtKit.Find<Text>(_screen, "Titre");
            Font font = title != null ? title.font : null;
            if (title != null)
            {
                title.text = end.Title;
                ((RectTransform)title.transform).anchoredPosition = new Vector2(x0, -100f);
                ((RectTransform)title.transform).sizeDelta = new Vector2(FrameW, 70f);
            }

            Font body = font;
            foreach (Text tx in _screen.GetComponentsInChildren<Text>(true))
                if (tx.name == "Libelle" && tx.font != null)
                {
                    body = tx.font;
                    break;
                }

            Text head = Cell(t, end.Headline, font, 30, new Color(0.95f, 0.72f, 0.48f), new Vector2(x0, -172f), FrameW, TextAnchor.MiddleCenter);
            head.fontStyle = FontStyle.Italic;
            float y = -232f, step = Mathf.Min(42f, 520f / Mathf.Max(1, end.Lines.Count));
            foreach ((string label, string value) in end.Lines)
            {
                Cell(t, label, body, 26, new Color(0.72f, 0.66f, 0.58f), new Vector2(x0 + 70f, y), 420f, TextAnchor.MiddleLeft);
                Text v = Cell(t, value, body, 26, new Color(0.96f, 0.9f, 0.78f), new Vector2(x0 + 500f, y), FrameW - 570f, TextAnchor.MiddleLeft);
                v.horizontalOverflow = HorizontalWrapMode.Wrap;
                v.resizeTextForBestFit = true;
                v.resizeTextMinSize = 16;
                v.resizeTextMaxSize = 26;
                y -= step;
            }

            _back = MenuArtKit.Find<Selectable>(_screen, "Retour");
            if (_back != null)
            {
                var r = (RectTransform)_back.transform;
                r.sizeDelta = new Vector2(380f, r.sizeDelta.y);
                r.anchoredPosition = new Vector2((ScreenW - 380f) * 0.5f, -50f - FrameH + 132f);
                Text bl = _back.GetComponentInChildren<Text>(true);
                if (bl != null)
                    bl.text = "Retour au campement";
            }

            Text hint = Cell(t, "Entrée : retour au campement   ·   R : nouvelle run tout de suite", body, 22, new Color(0.62f, 0.58f, 0.52f),
                new Vector2(x0, -50f - FrameH + 62f), FrameW, TextAnchor.MiddleCenter);
            hint.fontStyle = FontStyle.Italic;
            end.Skinned = true;
        }

        private static Text Cell(Transform parent, string text, Font font, int size, Color color, Vector2 topLeft, float width, TextAnchor align)
        {
            var go = new GameObject("Texte", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0f, 1f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = topLeft;
            r.sizeDelta = new Vector2(width, size + 12f);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = go.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        private void LateUpdate()
        {
            if (_end == null || _screen == null)
                return;
            if (_back != null)
            {
                bool over = MenuArtKit.Over((RectTransform)_back.transform, _cam);
                MenuArtKit.Tint(MenuArtKit.Targets(_back), over && MenuArtKit.MouseDown ? MenuArtKit.State.Pressed : MenuArtKit.State.Hover);
                if (over && MenuArtKit.Clicked)
                {
                    _end.ToCamp();
                    return;
                }
            }

            Keyboard k = Keyboard.current;
            if (k != null && k.rKey.wasPressedThisFrame)
                _end.Replay();
            else if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame))
                _end.ToCamp();
            Gamepad g = Gamepad.current;
            if (g != null && g.buttonSouth.wasPressedThisFrame)
                _end.ToCamp();
            else if (g != null && g.buttonNorth.wasPressedThisFrame)
                _end.Replay();
        }
    }
}
