using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Hud
{
    /// <summary>
    /// Liste de boutons greybox (IMGUI) pour les menus provisoires (D-048) : souris, clavier (flèches, ZQSD, Entrée, Espace) et
    /// manette (croix, stick gauche, A). Les éléments grisés se voient mais ne se choisissent pas. Refaite à l'étape 10.
    /// </summary>
    public sealed class GreyboxMenu
    {
        public sealed class Item
        {
            public string Label;
            public bool Enabled = true;
            /// <summary>Valeur affichée à droite (choix : « ◀ Facile ▶ »), gauche/droite la change.</summary>
            public System.Func<string> Value;
            public System.Action<int> Change;
            public System.Action Submit;
        }

        public readonly List<Item> Items = new List<Item>();
        public int Focus { get; private set; }
        private float _repeatAt;
        private static GUIStyle _button, _title, _text;

        public Item Add(string label, System.Action submit, bool enabled = true)
        {
            var it = new Item { Label = label, Submit = submit, Enabled = enabled };
            Items.Add(it);
            return it;
        }

        /// <summary>Clavier et manette (à appeler dans Update).</summary>
        public void Update()
        {
            if (Items.Count == 0)
                return;
            int dy = 0, dx = 0;
            bool submit = false;
            Keyboard k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.zKey.wasPressedThisFrame) dy = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dy = 1;
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame) dx = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dx = 1;
                submit |= k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
            }

            Gamepad g = Gamepad.current;
            if (g != null)
            {
                Vector2 s = g.leftStick.ReadValue() + g.dpad.ReadValue();
                if (Time.unscaledTime >= _repeatAt && s.sqrMagnitude > 0.4f)
                {
                    if (Mathf.Abs(s.y) > Mathf.Abs(s.x)) dy = s.y > 0f ? -1 : 1;
                    else dx = s.x > 0f ? 1 : -1;
                    _repeatAt = Time.unscaledTime + 0.25f;
                }
                else if (s.sqrMagnitude < 0.1f)
                    _repeatAt = 0f;
                submit |= g.buttonSouth.wasPressedThisFrame;
            }

            if (dy != 0)
                Focus = Next(Focus, dy);
            Item it = Items[Mathf.Clamp(Focus, 0, Items.Count - 1)];
            if (dx != 0 && it.Enabled && it.Change != null)
                it.Change(dx);
            if (submit && it.Enabled)
                it.Submit?.Invoke();
        }

        private int Next(int from, int dir)
        {
            int i = from;
            for (int n = 0; n < Items.Count; n++)
            {
                i = (i + dir + Items.Count) % Items.Count;
                if (Items[i].Enabled)
                    return i;
            }

            return from;
        }

        public void FocusFirst() => Focus = Items.Count > 0 && Items[0].Enabled ? 0 : Next(0, 1);

        /// <summary>Dessine le titre, un texte et les boutons au centre de l'écran (dans OnGUI).</summary>
        public void Draw(string title, string text, float width = 520f)
        {
            Styles();
            float h = 64f + (string.IsNullOrEmpty(text) ? 0f : _text.CalcHeight(new GUIContent(text), width) + 16f) + Items.Count * 50f;
            var area = new Rect((Screen.width - width) / 2f, Mathf.Max(20f, (Screen.height - h) / 2f), width, h);
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(area.x - 24f, area.y - 20f, area.width + 48f, area.height + 40f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(area);
            GUILayout.Label(title, _title);
            if (!string.IsNullOrEmpty(text))
            {
                GUILayout.Label(text, _text);
                GUILayout.Space(10f);
            }

            for (int i = 0; i < Items.Count; i++)
            {
                Item it = Items[i];
                string label = it.Value != null ? $"{it.Label} :   ◀  {it.Value()}  ▶" : it.Label;
                if (!it.Enabled)
                    label += "   (bientôt)";
                GUI.enabled = it.Enabled;
                GUI.color = i == Focus && it.Enabled ? new Color(1f, 0.85f, 0.4f) : Color.white;
                Rect r = GUILayoutUtility.GetRect(new GUIContent(label), _button, GUILayout.Height(42f));
                if (Event.current.type == EventType.MouseMove && r.Contains(Event.current.mousePosition) && it.Enabled)
                    Focus = i;
                if (GUI.Button(r, label, _button) && it.Enabled)
                {
                    Focus = i;
                    if (it.Change != null && it.Submit == null)
                        it.Change(Event.current.button == 1 ? -1 : 1);
                    else
                        it.Submit?.Invoke();
                }

                GUILayout.Space(8f);
            }

            GUI.enabled = true;
            GUI.color = Color.white;
            GUILayout.EndArea();
        }

        private static void Styles()
        {
            if (_button != null)
                return;
            _button = new GUIStyle(GUI.skin.button) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
            _title = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _title.normal.textColor = Color.white;
            _text = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            _text.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
        }
    }
}
