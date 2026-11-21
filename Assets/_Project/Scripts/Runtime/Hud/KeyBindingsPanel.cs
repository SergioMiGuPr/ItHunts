using System.Collections.Generic;
using ItHunts.Input;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Hud
{
    /// <summary>
    /// Menu des touches (A5, matin du 2026-11-21), ouvert depuis Paramètres (menu principal et menu pause) : onglets Clavier / souris
    /// et Manette, toutes les actions avec leur touche ; clic ou A / Entrée = capture par le rebinding interactif de l'Input System
    /// (Échap ou B annule) ; conflit affiché avec proposition d'échange ; Réinitialiser une action (X / Suppr) ou tout (Y / R) ;
    /// sauvegarde JSON des overrides par joueur (RebindStore) ; navigation complète à la manette. Dessiné avec les sprites et les
    /// polices des menus de la DA (MenuSkinSettings : panneau, cartouche, marque de focus) ; ce qui manque (icônes des touches,
    /// onglets) est en texte provisoire (liste pour la DA dans ART-HANDOFF).
    /// </summary>
    public sealed class KeyBindingsPanel : MonoBehaviour
    {
        public enum State : byte { Liste, Capture, Conflit }

        public static KeyBindingsPanel Current { get; private set; }
        public static bool IsOpen => Current != null;

        private InputActionAsset _asset;
        private int _slot;
        private bool _slotSelectable;
        private InputDevice _owner;
        private System.Action _onClose;
        private MenuSkinSettings _skin;
        private bool _ownsAsset;

        public string Group { get; private set; } = RebindStore.Keyboard;
        public State Mode { get; private set; }
        public int Focus { get; private set; }
        public int Slot => _slot;
        public List<RebindStore.Row> Rows { get; private set; } = new List<RebindStore.Row>();
        /// <summary>Message du bas (conflit, capture) : tests et affichage.</summary>
        public string Message { get; private set; } = "";

        private InputActionRebindingExtensions.RebindingOperation _op;
        private RebindStore.Row _pendingRow;
        private string _pendingPath, _oldPath;
        private RebindStore.Row? _conflictWith;
        private float _repeatAt;
        private int _openedFrame;

        // Vue
        private Canvas _canvas;
        private Text _title, _tabs, _footer, _message;
        private readonly List<(RectTransform rect, Image bg, Text label, Text key, Image focus)> _lines = new List<(RectTransform, Image, Text, Text, Image)>();
        private const int VisibleRows = 14;
        private int _scroll;

        /// <summary>
        /// Ouvre le menu des touches. asset : actions du joueur (en jeu : celles de son PlayerInput ; au menu : une copie de
        /// PlayerControls), slot : place du joueur (fichier de touches), slotSelectable : choix du joueur (menu principal).
        /// </summary>
        public static KeyBindingsPanel Open(InputActionAsset asset, int slot, bool slotSelectable, InputDevice owner, MenuSkinSettings skin,
            System.Action onClose, bool ownsAsset = false)
        {
            if (Current != null)
                Current.Close();
            var go = new GameObject("Menu des touches");
            var p = go.AddComponent<KeyBindingsPanel>();
            p._asset = asset;
            p._slot = slot;
            p._slotSelectable = slotSelectable;
            p._owner = owner;
            p._skin = skin;
            p._onClose = onClose;
            p._ownsAsset = ownsAsset;
            p._openedFrame = Time.frameCount;
            p.Group = owner is Gamepad ? RebindStore.Gamepad : RebindStore.Keyboard;
            Current = p;
            p.Refresh();
            p.Build();
            return p;
        }

        public void Close()
        {
            CancelCapture();
            if (Current == this)
                Current = null;
            if (_ownsAsset && _asset != null)
                Destroy(_asset);
            Destroy(gameObject);
            _onClose?.Invoke();
        }

        private void OnDestroy()
        {
            _op?.Dispose();
            if (Current == this)
                Current = null;
        }

        public void Refresh()
        {
            Rows = RebindStore.Rows(_asset, Group);
            Focus = Mathf.Clamp(Focus, 0, Mathf.Max(0, Rows.Count - 1));
        }

        public void SwitchTab(int dir)
        {
            if (Mode != State.Liste)
                return;
            Group = Group == RebindStore.Keyboard ? RebindStore.Gamepad : RebindStore.Keyboard;
            Focus = 0;
            _scroll = 0;
            Refresh();
        }

        /// <summary>Menu principal : touches d'un autre joueur (place 1 à 4).</summary>
        public void SwitchSlot(int dir)
        {
            if (!_slotSelectable || Mode != State.Liste)
                return;
            _slot = (_slot + dir + 4) % 4;
            RebindStore.Load(_asset, _slot);
            Refresh();
        }

        public void MoveFocus(int dir)
        {
            if (Rows.Count == 0 || Mode != State.Liste)
                return;
            Focus = (Focus + dir + Rows.Count) % Rows.Count;
        }

        public void ResetFocused()
        {
            if (Mode != State.Liste || Rows.Count == 0)
                return;
            RebindStore.Reset(Rows[Focus]);
            Save();
            Message = $"« {Rows[Focus].Label} » : touche par défaut.";
        }

        public void ResetAll()
        {
            if (Mode != State.Liste)
                return;
            RebindStore.ResetAll(_asset);
            Save();
            Message = "Toutes les touches par défaut.";
        }

        /// <summary>Capture : la prochaine touche (clavier et souris, ou manette selon l'onglet) ; Échap ou B annule.</summary>
        public void StartCapture()
        {
            if (Mode != State.Liste || Rows.Count == 0)
                return;
            RebindStore.Row row = Rows[Focus];
            _pendingRow = row;
            _oldPath = row.Path;
            bool pad = Group == RebindStore.Gamepad;
            bool wasEnabled = row.Action.enabled;
            row.Action.Disable();
            _op = row.Action.PerformInteractiveRebinding(row.Binding)
                .WithCancelingThrough(pad ? "<Gamepad>/buttonEast" : "<Keyboard>/escape")
                .WithControlsExcluding(pad ? "<Keyboard>" : "<Gamepad>")
                .WithControlsExcluding(pad ? "<Mouse>" : "<Gamepad>")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Gamepad>/leftStick")
                .WithControlsExcluding("<Gamepad>/rightStick")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(op => { Finish(row, wasEnabled, false); })
                .OnCancel(op => { Finish(row, wasEnabled, true); });
            Mode = State.Capture;
            Message = $"« {row.Label} » : appuie sur la nouvelle touche ({(pad ? "B" : "Échap")} pour annuler).";
            _op.Start();
        }

        private void Finish(RebindStore.Row row, bool wasEnabled, bool canceled)
        {
            _op?.Dispose();
            _op = null;
            if (wasEnabled)
                row.Action.Enable();
            if (canceled)
            {
                Mode = State.Liste;
                Message = "Annulé.";
                return;
            }

            ApplyCaptured(row, row.Path);
        }

        /// <summary>Touche capturée (aussi appelée par les tests) : conflit → proposition d'échange ; sinon gardée et sauvegardée.</summary>
        public void ApplyCaptured(RebindStore.Row row, string path)
        {
            if (string.IsNullOrEmpty(_oldPath) || !string.Equals(row.Path, path, System.StringComparison.OrdinalIgnoreCase))
            {
                _oldPath = row.Path;
                RebindStore.Set(row, path);
            }

            RebindStore.Row? other = RebindStore.Conflict(Rows, row, path);
            if (other.HasValue)
            {
                _pendingRow = row;
                _pendingPath = path;
                _conflictWith = other;
                Mode = State.Conflit;
                Message = $"{InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice)} sert déjà à « {other.Value.Label} ». " +
                          "Échanger les deux touches ? (A / Entrée : oui ; B / Échap : non)";
                return;
            }

            Mode = State.Liste;
            _oldPath = null;
            Save();
            Message = $"« {row.Label} » : {row.Display}.";
        }

        /// <summary>Conflit : oui = échange (l'autre action prend l'ancienne touche), non = l'ancienne touche revient.</summary>
        public void AnswerConflict(bool swap)
        {
            if (Mode != State.Conflit || !_conflictWith.HasValue)
                return;
            RebindStore.Row other = _conflictWith.Value;
            if (swap)
            {
                RebindStore.Set(other, _oldPath);
                Message = $"Échangé : « {_pendingRow.Label} » {_pendingRow.Display}, « {other.Label} » {other.Display}.";
            }
            else
            {
                RebindStore.Set(_pendingRow, _oldPath);
                Message = "Gardé tel quel.";
            }

            _conflictWith = null;
            _oldPath = null;
            Mode = State.Liste;
            Save();
        }

        public void CancelCapture()
        {
            if (_op != null)
                _op.Cancel();
        }

        /// <summary>Le joueur en jeu prend ses touches tout de suite ; tout est écrit dans touches_joueurN.json.</summary>
        private void Save()
        {
            RebindStore.Save(_asset, _slot);
            foreach (PlayerInput pi in PlayerInput.all)
                if (pi != null && pi.playerIndex == _slot && pi.actions != _asset)
                    RebindStore.Load(pi.actions, _slot);
        }

        // ---------- Commandes ----------

        private void Update()
        {
            if (Time.frameCount == _openedFrame)
                return;                                                       // l'appui qui a ouvert le menu
            if (Mode == State.Capture)
            {
                SyncView();
                return;
            }

            Keyboard k = _owner == null || _owner is Keyboard ? Keyboard.current : null;
            Gamepad g = _owner == null ? Gamepad.current : _owner as Gamepad;
            int dy = 0, dx = 0;
            bool submit = false, back = false, reset = false, resetAll = false, tabL = false, tabR = false;
            if (k != null)
            {
                dy = k.upArrowKey.wasPressedThisFrame ? -1 : k.downArrowKey.wasPressedThisFrame ? 1 : 0;
                dx = k.leftArrowKey.wasPressedThisFrame ? -1 : k.rightArrowKey.wasPressedThisFrame ? 1 : 0;
                submit = k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame;
                back = k.escapeKey.wasPressedThisFrame;
                reset = k.deleteKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame;
                resetAll = k.rKey.wasPressedThisFrame;
                tabL = k.pageUpKey.wasPressedThisFrame;
                tabR = k.tabKey.wasPressedThisFrame || k.pageDownKey.wasPressedThisFrame;
            }

            if (g != null)
            {
                Vector2 s = g.leftStick.ReadValue() + g.dpad.ReadValue();
                if (Time.unscaledTime >= _repeatAt && s.sqrMagnitude > 0.4f)
                {
                    if (Mathf.Abs(s.y) > Mathf.Abs(s.x)) dy = s.y > 0f ? -1 : 1;
                    else dx = s.x > 0f ? 1 : -1;
                    _repeatAt = Time.unscaledTime + 0.22f;
                }
                else if (s.sqrMagnitude < 0.1f)
                    _repeatAt = 0f;
                submit |= g.buttonSouth.wasPressedThisFrame;
                back |= g.buttonEast.wasPressedThisFrame;
                reset |= g.buttonWest.wasPressedThisFrame;
                resetAll |= g.buttonNorth.wasPressedThisFrame;
                tabL |= g.leftShoulder.wasPressedThisFrame;
                tabR |= g.rightShoulder.wasPressedThisFrame;
            }

            if (Mode == State.Conflit)
            {
                if (submit)
                    AnswerConflict(true);
                else if (back)
                    AnswerConflict(false);
                SyncView();
                return;
            }

            if (back)
            {
                Close();
                return;
            }

            if (tabL || tabR)
                SwitchTab(tabR ? 1 : -1);
            if (dx != 0)
            {
                if (_slotSelectable)
                    SwitchSlot(dx);
                else
                    SwitchTab(dx);
            }

            if (dy != 0)
                MoveFocus(dy);
            if (reset)
                ResetFocused();
            if (resetAll)
                ResetAll();
            // Souris : survol = focus, clic = capture.
            for (int i = 0; i < _lines.Count; i++)
            {
                int row = _scroll + i;
                if (row >= Rows.Count || !_lines[i].rect.gameObject.activeSelf)
                    continue;
                if (UiKit.MouseMoved && UiKit.Hover(_lines[i].rect))
                    Focus = row;
                if (UiKit.Clicked && UiKit.Hover(_lines[i].rect))
                {
                    Focus = row;
                    submit = true;
                }
            }

            if (submit)
                StartCapture();
            SyncView();
        }

        // ---------- Vue ----------

        private void Build()
        {
            _canvas = UiKit.NewCanvas("Menu des touches", 900, transform);
            var scaler = _canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            Font text = _skin != null && _skin.textFont != null ? _skin.textFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Font title = _skin != null && _skin.titleFont != null ? _skin.titleFont : text;
            Image dim = UiKit.Img("Voile", _canvas.transform, null, new Color(0f, 0f, 0f, 0.78f));
            UiKit.Stretch(dim.rectTransform);
            RectTransform frame = UiKit.Rect("Cadre", _canvas.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1240f, 980f));
            Image panel = UiKit.Img("Panneau", frame, _skin != null ? _skin.panel : null, _skin != null && _skin.panel != null ? Color.white : new Color(0.08f, 0.07f, 0.09f, 0.95f),
                _skin != null && _skin.panel != null && _skin.panel.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple);
            UiKit.Stretch(panel.rectTransform);
            _title = UiKit.Txt("Titre", frame, title, 58, UiKit.Gold);
            Place(_title.rectTransform, new Vector2(0f, 420f), new Vector2(1100f, 80f));
            _tabs = UiKit.Txt("Onglets", frame, text, 30, UiKit.TextColor);
            Place(_tabs.rectTransform, new Vector2(0f, 352f), new Vector2(1100f, 50f));
            for (int i = 0; i < VisibleRows; i++)
            {
                RectTransform r = UiKit.Rect($"Ligne {i}", frame, new Vector2(0.5f, 0.5f), new Vector2(0f, 290f - i * 46f), new Vector2(1060f, 42f));
                Sprite rowSprite = _skin != null ? _skin.keyRow : null;
                Image bg = UiKit.Img("Fond", r, rowSprite, rowSprite != null ? new Color(1f, 1f, 1f, 0.55f) : new Color(1f, 1f, 1f, 0.05f),
                    rowSprite != null && rowSprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple);
                UiKit.Stretch(bg.rectTransform);
                Image focus = UiKit.Img("Focus", r, _skin != null ? _skin.focusMark : null, UiKit.Gold);
                focus.rectTransform.anchorMin = focus.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                focus.rectTransform.sizeDelta = new Vector2(30f, 30f);
                focus.rectTransform.anchoredPosition = new Vector2(-22f, 0f);
                Text label = UiKit.Txt("Action", r, text, 28, UiKit.TextColor, TextAnchor.MiddleLeft);
                Place(label.rectTransform, new Vector2(-230f, 0f), new Vector2(560f, 42f));
                Text key = UiKit.Txt("Touche", r, text, 28, UiKit.Gold, TextAnchor.MiddleRight);
                Place(key.rectTransform, new Vector2(240f, 0f), new Vector2(540f, 42f));
                _lines.Add((r, bg, label, key, focus));
            }

            _message = UiKit.Txt("Message", frame, text, 26, UiKit.TextColor);
            Place(_message.rectTransform, new Vector2(0f, -385f), new Vector2(1120f, 70f));
            _footer = UiKit.Txt("Aide", frame, text, 22, new Color(0.75f, 0.72f, 0.68f));
            Place(_footer.rectTransform, new Vector2(0f, -440f), new Vector2(1160f, 40f));
            SyncView();
        }

        private static void Place(RectTransform r, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
        }

        private void SyncView()
        {
            if (_canvas == null)
                return;
            _title.text = _slotSelectable || PlayerInput.all.Count > 1 ? $"Touches · Joueur {_slot + 1}" : "Touches";
            bool kb = Group == RebindStore.Keyboard;
            _tabs.text = (kb ? "[ Clavier / souris ]" : "  Clavier / souris  ") + "        " + (kb ? "  Manette  " : "[ Manette ]")
                         + (_slotSelectable ? "        ‹ joueur ›" : "");
            if (Focus < _scroll)
                _scroll = Focus;
            if (Focus >= _scroll + VisibleRows)
                _scroll = Focus - VisibleRows + 1;
            for (int i = 0; i < _lines.Count; i++)
            {
                int row = _scroll + i;
                bool on = row < Rows.Count;
                (RectTransform rect, Image bg, Text label, Text key, Image focus) = _lines[i];
                rect.gameObject.SetActive(on);
                if (!on)
                    continue;
                RebindStore.Row r = Rows[row];
                bool f = row == Focus;
                label.text = r.Label;
                key.text = Mode == State.Capture && f ? "…" : r.Display + (r.Overridden ? "  •" : "");
                focus.enabled = f;
                bg.color = f ? new Color(1f, 0.85f, 0.6f, 0.75f) : (bg.sprite != null ? new Color(1f, 1f, 1f, 0.45f) : new Color(1f, 1f, 1f, 0.05f));
            }

            _message.text = Message;
            _footer.text = Mode == State.Conflit ? "A / Entrée : échanger     B / Échap : garder l'ancienne touche"
                : Mode == State.Capture ? (Group == RebindStore.Gamepad ? "B : annuler" : "Échap : annuler")
                : "A / clic : changer     X / Suppr : par défaut     Y / R : tout par défaut     LB RB / Tab : onglet     B / Échap : retour";
        }
    }
}
