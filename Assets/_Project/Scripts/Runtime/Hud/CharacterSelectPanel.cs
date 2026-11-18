using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// Choix du personnage (habillage provisoire en attendant le lot D de la DA : cadre gothique, vignettes, grande pose animée) :
    /// 4 vignettes (bord à la couleur du personnage, grisées s'il n'est pas jouable « bientôt », ou pris par un autre joueur « pris
    /// par J1 »), grande silhouette, nom, artefact et sa description, Valider. Piloté par l'appareil du joueur (sa manette en coop),
    /// la souris aussi pour le clavier. Au campement, un personnage pris reste choisissable (c'est Prêt qui refuse le doublon) ; en
    /// jeu, il ne l'est pas.
    /// </summary>
    public sealed class CharacterSelectPanel
    {
        public const int None = -2, Cancel = -1;

        private readonly MenuSkinSettings _skin;
        private readonly CharacterRoster _roster;
        private readonly RectTransform _root;
        private readonly List<(RectTransform rect, Image border, Image fill, Image figure, Text name, Text tag)> _cards =
            new List<(RectTransform, Image, Image, Image, Text, Text)>();
        private Text _title, _name, _artifact, _desc, _hint, _validate;
        private Image _big;
        private RectTransform _validateRect;
        private System.Func<int, string> _taken;
        private bool _takenSelectable;
        private float _repeatAt;

        public InputDevice Owner { get; private set; }
        public int Focus { get; private set; }
        public bool IsOpen => _root.gameObject.activeSelf;
        public RectTransform Root => _root;

        public CharacterSelectPanel(RectTransform region, MenuSkinSettings skin, CharacterRoster roster)
        {
            _skin = skin;
            _roster = roster;
            _root = UiKit.Rect("Choix du personnage", region, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1080f, 720f));
            Build();
            _root.gameObject.SetActive(false);
        }

        private Font TextFont => _skin != null ? _skin.textFont : null;
        private Font Bold => _skin != null && _skin.textBold != null ? _skin.textBold : TextFont;

        private void Build()
        {
            UiKit.Panel(_root, _skin, 0.9f);
            _title = UiKit.Txt("Titre", _root, _skin != null ? _skin.titleFont : null, 50, UiKit.TextColor);
            Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, -58f), new Vector2(0f, 70f), true);
            int n = _roster != null ? _roster.Count : 0;
            const float card = 150f, gap = 22f;
            for (int i = 0; i < n; i++)
            {
                float x = (i - (n - 1) * 0.5f) * (card + gap);
                RectTransform r = UiKit.Rect($"Vignette {i + 1}", _root, new Vector2(0.5f, 1f), new Vector2(x, -110f), new Vector2(card, card + 46f), new Vector2(0.5f, 1f));
                Image border = UiKit.Img("Bord", r, null, Color.white);
                border.rectTransform.anchorMin = new Vector2(0f, 1f);
                border.rectTransform.anchorMax = new Vector2(1f, 1f);
                border.rectTransform.pivot = new Vector2(0.5f, 1f);
                border.rectTransform.sizeDelta = new Vector2(0f, card);
                Image fill = UiKit.Img("Fond", border.transform, null, new Color(0.06f, 0.06f, 0.08f, 1f));
                UiKit.Stretch(fill.rectTransform, 5f);
                Image fig = UiKit.Img("Silhouette", fill.transform, UiKit.Silhouette(), Color.white);
                fig.preserveAspect = true;
                UiKit.Stretch(fig.rectTransform, 12f);
                Text name = UiKit.Txt("Nom", r, Bold, 26, UiKit.TextColor);
                Place(name.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 20f), new Vector2(0f, 40f), true);
                Text tag = UiKit.Txt("Pris", fill.transform, Bold, 21, UiKit.Gold);
                Place(tag.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 18f), new Vector2(0f, 30f), true);
                _cards.Add((r, border, fill, fig, name, tag));
            }

            _big = UiKit.Img("Grande silhouette", _root, UiKit.Silhouette(), Color.white);
            _big.preserveAspect = true;
            _big.rectTransform.anchorMin = _big.rectTransform.anchorMax = new Vector2(0f, 0f);
            _big.rectTransform.pivot = new Vector2(0f, 0f);
            _big.rectTransform.anchoredPosition = new Vector2(90f, 110f);
            _big.rectTransform.sizeDelta = new Vector2(230f, 300f);
            _name = UiKit.Txt("Nom du personnage", _root, _skin != null ? _skin.titleFont : null, 46, UiKit.TextColor, TextAnchor.UpperLeft);
            PlaceLeft(_name.rectTransform, new Vector2(360f, 380f), new Vector2(620f, 60f));
            _artifact = UiKit.Txt("Artefact", _root, Bold, 30, UiKit.Gold, TextAnchor.UpperLeft);
            PlaceLeft(_artifact.rectTransform, new Vector2(360f, 320f), new Vector2(620f, 44f));
            _desc = UiKit.Txt("Description", _root, TextFont, 25, UiKit.TextColor, TextAnchor.UpperLeft);
            PlaceLeft(_desc.rectTransform, new Vector2(360f, 268f), new Vector2(620f, 140f));
            _validateRect = UiKit.Rect("Valider", _root, new Vector2(1f, 0f), new Vector2(-70f, 56f), new Vector2(260f, 58f), new Vector2(1f, 0f));
            UiKit.Img("Fond", _validateRect, null, new Color(0.93f, 0.78f, 0.47f, 0.16f)).rectTransform.anchorMin = Vector2.zero;
            UiKit.Stretch((RectTransform)_validateRect.GetChild(0));
            _validate = UiKit.Txt("Texte", _validateRect, Bold, 30, UiKit.Gold);
            UiKit.Stretch(_validate.rectTransform);
            _hint = UiKit.Txt("Aide", _root, TextFont, 21, UiKit.Dim, TextAnchor.MiddleLeft);
            PlaceLeft(_hint.rectTransform, new Vector2(70f, 56f), new Vector2(640f, 58f));
        }

        private static void Place(RectTransform r, Vector2 anchorY, Vector2 pos, Vector2 size, bool stretchX)
        {
            r.anchorMin = new Vector2(0f, anchorY.y);
            r.anchorMax = new Vector2(1f, anchorY.y);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        private static void PlaceLeft(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0f, 0f);
            r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        /// <summary>
        /// Ouvre le choix : titre (« Joueur 2 »), personnage proposé, qui a déjà chaque personnage (texte de l'étiquette, vide si libre),
        /// pris choisissable ou non, appareil du joueur (null : clavier et toute manette).
        /// </summary>
        public void Open(string who, int current, System.Func<int, string> takenNote, bool takenSelectable, InputDevice owner)
        {
            _title.text = "Choix du personnage" + (string.IsNullOrEmpty(who) ? "" : " · " + who);
            _taken = takenNote;
            _takenSelectable = takenSelectable;
            Owner = owner;
            Focus = Mathf.Clamp(current, 0, Mathf.Max(0, _cards.Count - 1));
            _repeatAt = Time.unscaledTime + 0.2f;
            _openedFrame = Time.frameCount;
            _root.gameObject.SetActive(true);
            Refresh();
        }

        private int _openedFrame;

        public void Close() => _root.gameObject.SetActive(false);

        public bool Choosable(int i)
        {
            CharacterDefinition c = _roster != null ? _roster[i] : null;
            return c != null && c.Selectable && (_takenSelectable || string.IsNullOrEmpty(_taken?.Invoke(i)));
        }

        /// <summary>Clavier, manette, souris ; renvoie None (rien), Cancel (B, Échap) ou l'indice validé.</summary>
        public int Update()
        {
            if (!IsOpen)
                return None;
            // Le panneau tient dans la zone du joueur (demi-écran en écran partagé).
            if (_root.parent is RectTransform zone && zone.rect.width > 1f)
                _root.localScale = Vector3.one * Mathf.Min(1f, zone.rect.width / 1120f, zone.rect.height / 760f);
            if (Time.frameCount <= _openedFrame)
                return None;
            int dx = 0;
            bool ok = false, back = false;
            Keyboard k = Owner == null || Owner is Keyboard ? Keyboard.current : null;
            if (k != null)
            {
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame) dx = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dx = 1;
                ok |= k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
                back |= k.escapeKey.wasPressedThisFrame;
            }

            Gamepad g = Owner == null ? Gamepad.current : Owner as Gamepad;
            if (g != null)
            {
                float x = g.leftStick.ReadValue().x + g.dpad.ReadValue().x;
                if (Mathf.Abs(x) > 0.6f && Time.unscaledTime >= _repeatAt)
                {
                    dx = x > 0f ? 1 : -1;
                    _repeatAt = Time.unscaledTime + 0.22f;
                }
                else if (Mathf.Abs(x) < 0.2f)
                    _repeatAt = 0f;
                ok |= g.buttonSouth.wasPressedThisFrame;
                back |= g.buttonEast.wasPressedThisFrame;
            }

            if (dx != 0 && _cards.Count > 0)
                Focus = (Focus + dx + _cards.Count) % _cards.Count;
            if (Owner == null || Owner is Keyboard)
            {
                for (int i = 0; i < _cards.Count; i++)
                    if (UiKit.Hover(_cards[i].rect))
                    {
                        if (UiKit.MouseMoved)
                            Focus = i;
                        if (UiKit.Clicked)
                        {
                            Focus = i;
                            ok = true;
                        }
                    }

                if (UiKit.Hover(_validateRect) && UiKit.Clicked)
                    ok = true;
            }

            Refresh();
            if (back)
                return Cancel;
            return ok && Choosable(Focus) ? Focus : None;
        }

        private void Refresh()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                CharacterDefinition c = _roster[i];
                (RectTransform rect, Image border, Image fill, Image figure, Text name, Text tag) = _cards[i];
                string taken = _taken?.Invoke(i);
                bool playable = c != null && c.Selectable;
                bool grey = !playable || !string.IsNullOrEmpty(taken);
                bool focus = i == Focus;
                Color col = c != null ? c.color : Color.grey;
                border.color = focus ? UiKit.Gold : grey ? Color.Lerp(col, new Color(0.25f, 0.25f, 0.25f), 0.7f) : col;
                figure.color = grey ? new Color(col.r * 0.25f, col.g * 0.25f, col.b * 0.25f, 0.8f) : col * 0.85f;
                rect.localScale = Vector3.one * (focus ? 1.08f : 1f);
                name.text = c != null ? c.displayName : "—";
                name.color = grey ? UiKit.Dim : focus ? UiKit.Gold : UiKit.TextColor;
                tag.text = !playable ? "bientôt" : !string.IsNullOrEmpty(taken) ? taken : "";
            }

            CharacterDefinition f = _roster != null ? _roster[Focus] : null;
            _big.color = f != null ? (Choosable(Focus) ? f.color : f.color * 0.35f) : Color.grey;
            _name.text = f != null ? f.displayName : "";
            _artifact.text = f != null ? f.ArtifactName : "";
            string note = f == null ? "" : !f.Selectable ? "Pas encore jouable." : !string.IsNullOrEmpty(_taken?.Invoke(Focus))
                ? (_takenSelectable ? "Déjà choisi par un autre joueur : Prêt sera refusé tant que vous êtes deux." : "Déjà pris par un autre joueur.")
                : "";
            _desc.text = f != null ? (f.description + (note.Length > 0 ? "\n" + note : "")) : "";
            _validate.text = "Valider";
            _validate.color = Choosable(Focus) ? UiKit.Gold : UiKit.Dim;
            _hint.text = Owner is Gamepad ? "◀ ▶ choisir · A valider · B retour" : "◀ ▶ ou souris · Entrée valider · Échap retour";
        }
    }
}
