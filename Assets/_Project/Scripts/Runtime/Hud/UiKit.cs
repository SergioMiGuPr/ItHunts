using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// Outils uGUI des menus (lot B de la DA) : canvas à l'échelle de 1080p (suit la hauteur de l'écran), rectangles, images, textes
    /// (EB Garamond, Grenze Gotisch), panneaux en 9 tranches, zone d'un joueur en écran partagé.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Gold = new Color(0.93f, 0.78f, 0.47f), TextColor = new Color(0.88f, 0.85f, 0.8f),
            Dim = new Color(0.45f, 0.43f, 0.42f), Shade = new Color(0f, 0f, 0f, 0.62f);

        /// <summary>Canvas superposé, à l'échelle de 1920 × 1080 par la hauteur (lisible en 0,66x comme en 4K).</summary>
        public static Canvas NewCanvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
                go.transform.SetParent(parent, false);
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.AddComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920f, 1080f);
            s.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            s.matchWidthOrHeight = 1f;
            return c;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent != null ? parent.gameObject.layer : 5;
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(RectTransform r, float inset = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
            return r;
        }

        public static Image Img(string name, Transform parent, Sprite sprite, Color color, Image.Type type = Image.Type.Simple, Material material = null)
        {
            RectTransform r = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.type = sprite != null ? type : Image.Type.Simple;
            img.color = color;
            img.raycastTarget = false;
            if (material != null)
                img.material = material;
            return img;
        }

        public static Text Txt(string name, Transform parent, Font font, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter)
        {
            RectTransform r = Rect(name, parent, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            var t = r.gameObject.AddComponent<Text>();
            t.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var sh = r.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.85f);
            sh.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        /// <summary>Panneau (9 tranches de la DA, sinon un fond sombre) qui remplit son parent.</summary>
        public static Image Panel(Transform parent, MenuSkinSettings skin, float shade = 0.82f)
        {
            Image back = Img("Fond du panneau", parent, null, new Color(0.03f, 0.03f, 0.045f, shade));
            Stretch(back.rectTransform, 10f);
            if (skin != null && skin.panel != null)
            {
                Image frame = Img("Cadre du panneau", parent, skin.panel, Color.white, Image.Type.Sliced);
                Stretch(frame.rectTransform);
            }

            return back;
        }

        /// <summary>Zone d'une caméra (écran partagé) dans un canvas plein écran : ancrage sur le rectangle de la caméra.</summary>
        public static void FitTo(RectTransform r, Camera cam)
        {
            UnityEngine.Rect v = cam != null ? cam.rect : new UnityEngine.Rect(0f, 0f, 1f, 1f);
            r.anchorMin = v.min;
            r.anchorMax = v.max;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        /// <summary>La souris est-elle au-dessus de ce rectangle (canvas superposé) ?</summary>
        public static bool Hover(RectTransform r) =>
            Mouse.current != null && r != null && r.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(r, Mouse.current.position.ReadValue(), null);

        public static bool MouseMoved => Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 0.5f;
        public static bool Clicked => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static bool RightClicked => Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

        /// <summary>Silhouette provisoire (en attendant les personnages 2D du lot C) : tête, épaules, buste, en blanc sur transparent.</summary>
        public static Sprite Silhouette()
        {
            if (_silhouette != null)
                return _silhouette;
            const int w = 160, h = 240;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "Silhouette (provisoire)", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = (x - w / 2f) / (w / 2f), v = y / (float)h;
                float head = new Vector2(u / 0.32f, (v - 0.8f) / 0.13f).magnitude;                  // tête
                float ears = Mathf.Min(new Vector2((u - 0.22f) / 0.09f, (v - 0.93f) / 0.07f).magnitude,
                    new Vector2((u + 0.22f) / 0.09f, (v - 0.93f) / 0.07f).magnitude);                // oreilles
                // Buste aux épaules arrondies : large en bas, épaules en arc sous la tête.
                float bodyHalf = v < 0.5f ? Mathf.Lerp(0.92f, 0.78f, v / 0.5f) : 0.78f * Mathf.Sqrt(Mathf.Clamp01(1f - Mathf.Pow((v - 0.5f) / 0.16f, 2f)));
                bool body = v < 0.66f && Mathf.Abs(u) < bodyHalf;
                float a = head < 1f || ears < 1f || body ? 1f : 0f;
                // Bord adouci.
                if (a == 0f && (head < 1.06f || ears < 1.08f))
                    a = 0.5f;
                px[y * w + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }

            tex.SetPixels32(px);
            tex.Apply(false);
            _silhouette = Sprite.Create(tex, new UnityEngine.Rect(0, 0, w, h), new Vector2(0.5f, 0f), 100f);
            return _silhouette;
        }

        private static Sprite _silhouette;
    }

    /// <summary>
    /// Liste de boutons uGUI pour un GreyboxMenu (la logique clavier et manette reste celle de GreyboxMenu) : focus visible (texte or,
    /// repère d'étoile, barre), éléments grisés lisibles, valeurs « ◀ choix ▶ », souris (survol = focus, clic = choisir, clic droit =
    /// valeur précédente) quand le menu est piloté par le clavier ou par n'importe qui.
    /// </summary>
    public sealed class UiList
    {
        private readonly GreyboxMenu _menu;
        private readonly RectTransform _root;
        private readonly MenuSkinSettings _skin;
        private readonly float _rowHeight;
        private readonly int _fontSize;
        private readonly TextAnchor _align;
        private readonly List<(RectTransform rect, Text text, Image bar, Image mark)> _rows = new List<(RectTransform, Text, Image, Image)>();

        public RectTransform Root => _root;
        public GreyboxMenu Menu => _menu;

        public UiList(GreyboxMenu menu, RectTransform root, MenuSkinSettings skin, float rowHeight = 54f, int fontSize = 30, TextAnchor align = TextAnchor.MiddleCenter)
        {
            _menu = menu;
            _root = root;
            _skin = skin;
            _rowHeight = rowHeight;
            _fontSize = fontSize;
            _align = align;
        }

        /// <summary>Hauteur de la liste (px 1080p).</summary>
        public float Height => _menu.Items.Count * _rowHeight;

        /// <summary>Met la liste à jour (textes, focus) et traite la souris. À appeler à chaque image, après GreyboxMenu.Update.</summary>
        public void Sync()
        {
            while (_rows.Count < _menu.Items.Count)
                _rows.Add(NewRow(_rows.Count));
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].rect.gameObject.SetActive(i < _menu.Items.Count);
            bool mouse = _menu.Owner == null || _menu.Owner is Keyboard;
            for (int i = 0; i < _menu.Items.Count; i++)
            {
                GreyboxMenu.Item it = _menu.Items[i];
                (RectTransform rect, Text text, Image bar, Image mark) = _rows[i];
                rect.anchoredPosition = new Vector2(0f, -i * _rowHeight);
                string label = it.Value != null ? $"{it.Label}   ◀  {it.Value()}  ▶" : it.Label;
                if (!it.Enabled && !string.IsNullOrEmpty(it.DisabledNote))
                    label += "   " + it.DisabledNote;
                bool focus = i == _menu.Focus && it.Enabled;
                text.text = label;
                text.color = !it.Enabled ? UiKit.Dim : focus ? UiKit.Gold : UiKit.TextColor;
                text.font = focus && _skin != null && _skin.textBold != null ? _skin.textBold : _skin != null && _skin.textFont != null ? _skin.textFont : text.font;
                bar.enabled = focus;
                mark.enabled = focus && mark.sprite != null;
                if (mouse && it.Enabled && UiKit.Hover(rect))
                {
                    if (UiKit.MouseMoved)
                        _menu.SetFocus(i);
                    if (UiKit.Clicked)
                    {
                        _menu.SetFocus(i);
                        if (it.Submit != null)
                            it.Submit();
                        else
                            it.Change?.Invoke(1);
                    }
                    else if (UiKit.RightClicked)
                        it.Change?.Invoke(-1);
                }
            }
        }

        private (RectTransform, Text, Image, Image) NewRow(int i)
        {
            RectTransform r = UiKit.Rect($"Bouton {i + 1}", _root, new Vector2(0.5f, 1f), new Vector2(0f, -i * _rowHeight), new Vector2(0f, _rowHeight - 6f), new Vector2(0.5f, 1f));
            r.anchorMin = new Vector2(0f, 1f);
            r.anchorMax = new Vector2(1f, 1f);
            r.sizeDelta = new Vector2(0f, _rowHeight - 6f);
            Image bar = UiKit.Img("Focus", r, null, new Color(0.93f, 0.78f, 0.47f, 0.12f));
            UiKit.Stretch(bar.rectTransform);
            Image mark = UiKit.Img("Repère", r, _skin != null ? _skin.focusMark : null, UiKit.Gold);
            mark.preserveAspect = true;
            mark.rectTransform.anchorMin = mark.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            mark.rectTransform.sizeDelta = Vector2.one * (_rowHeight - 14f);
            mark.rectTransform.anchoredPosition = new Vector2(_rowHeight * 0.5f, 0f);
            Text t = UiKit.Txt("Texte", r, _skin != null ? _skin.textFont : null, _fontSize, UiKit.TextColor, _align);
            UiKit.Stretch(t.rectTransform);
            t.rectTransform.offsetMin = new Vector2(_rowHeight + 4f, 0f);
            t.rectTransform.offsetMax = new Vector2(-_rowHeight * 0.4f, 0f);
            return (r, t, bar, mark);
        }
    }
}
