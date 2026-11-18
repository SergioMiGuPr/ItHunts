using System.Collections.Generic;
using System.Linq;
using ItHunts.Art.Menus;
using ItHunts.Hud;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Menus
{
    /// <summary>
    /// Vue du menu Échap : les prefabs Menu_Pause et Menu_Parametres de la DA (UI-HANDOFF § 2.6), un par joueur, dans sa vue
    /// (écran partagé compris : l'écran est contenu dans la vue, le voile la couvre). Les boutons suivent les entrées du menu :
    /// au-delà des 4 de la maquette (Quitter l'écran partagé, vitrine, options de développement), le dernier cartouche est
    /// reproduit en dessous au même pas et le cadre en 9 tranches s'allonge. Posée sur chaque PauseMenu (MenuArtInstall).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PauseMenuArt : MonoBehaviour
    {
        private sealed class View
        {
            public RectTransform Root;
            public GameObject Pause, Settings;
            public readonly List<(Selectable button, Text label)> Buttons = new List<(Selectable, Text)>();
            public readonly List<(RectTransform rect, Text value, Graphic[] tint)> Rows = new List<(RectTransform, Text, Graphic[])>();
            public Selectable SettingsBack;
            public int Items = -1;
        }

        private PauseMenu _pause;
        private Canvas _canvas;
        private readonly Dictionary<PauseMenu.Open, View> _views = new Dictionary<PauseMenu.Open, View>();

        public void Attach(PauseMenu pause)
        {
            _pause = pause;
            pause.Skinned = true;
        }

        /// <summary>Écrans affichés pour ce joueur (tests).</summary>
        public GameObject PauseScreenOf(PlayerInput pi) => _views.FirstOrDefault(kv => kv.Key.Player == pi).Value?.Pause;
        public GameObject SettingsScreenOf(PlayerInput pi) => _views.FirstOrDefault(kv => kv.Key.Player == pi).Value?.Settings;

        private void LateUpdate()
        {
            if (_pause == null)
                return;
            foreach (PauseMenu.Open o in _views.Keys.ToList())
                if (!_pause.Opened.Contains(o))
                {
                    if (_views[o].Root != null)
                        Destroy(_views[o].Root.gameObject);
                    _views.Remove(o);
                }

            foreach (PauseMenu.Open o in _pause.Opened)
            {
                if (o.Player == null)
                    continue;
                if (!_views.TryGetValue(o, out View v))
                {
                    MenuSkinSettings skin = PauseMenu.SkinOf(o.Player);
                    if (skin == null || skin.pauseScreen == null)
                        continue;
                    v = Build(o, skin);
                    _views[o] = v;
                }

                Sync(o, v);
            }
        }

        private View Build(PauseMenu.Open o, MenuSkinSettings skin)
        {
            if (_canvas == null)
                _canvas = UiKit.NewCanvas("Menus pause (DA)", 80, transform);
            var v = new View();
            v.Root = new GameObject("Pause : " + o.Player.name, typeof(RectTransform)).GetComponent<RectTransform>();
            v.Root.SetParent(_canvas.transform, false);
            v.Pause = ScreenIn(skin.pauseScreen, v.Root, "Menu_Pause");
            v.Settings = ScreenIn(skin.settingsScreen, v.Root, "Menu_Parametres");
            if (v.Settings != null)
            {
                for (int i = 0; i < GameSettings.Labels.Length; i++)
                {
                    Transform row = v.Settings.transform.Find("Option_" + i);
                    if (row == null)
                        continue;
                    var label = row.Find("Libelle")?.GetComponent<Text>();
                    if (label != null)
                        label.text = GameSettings.Labels[i];
                    v.Rows.Add(((RectTransform)row, row.Find("Valeur")?.GetComponent<Text>(), MenuArtKit.Targets(row.GetComponent<Selectable>())));
                }

                v.SettingsBack = MenuArtKit.Find<Selectable>(v.Settings, "Retour");
            }

            return v;
        }

        /// <summary>Écran contenu dans la vue du joueur ; le voile de la maquette couvre toute la vue.</summary>
        private static GameObject ScreenIn(GameObject prefab, RectTransform root, string name)
        {
            if (prefab == null)
                return null;
            var holder = new GameObject(name + " (vue)", typeof(RectTransform)).GetComponent<RectTransform>();
            holder.SetParent(root, false);
            UiKit.Stretch(holder);
            GameObject go = MenuArtKit.Spawn(prefab, holder, name);
            var screen = go.GetComponent<MenuEcran>();
            if (screen != null)
                screen.couvrir = false;                                  // la moitié d'écran garde tout le cadre
            Transform veil = go.transform.Find("Voile");
            if (veil != null)
            {
                veil.SetParent(holder, false);
                veil.SetAsFirstSibling();
                UiKit.Stretch((RectTransform)veil);
                veil.localScale = Vector3.one;
            }

            holder.gameObject.SetActive(false);
            return go;
        }

        /// <summary>Autant de cartouches que d'entrées : le 4e est reproduit au même pas, le cadre s'allonge, l'ensemble reste centré.</summary>
        private static void Layout(View v, GreyboxMenu menu)
        {
            v.Items = menu.Items.Count;
            v.Buttons.Clear();
            var buttons = new List<RectTransform>();
            for (int i = 0; ; i++)
            {
                Transform b = v.Pause.transform.Find("Bouton_" + i);
                if (b == null)
                    break;
                buttons.Add((RectTransform)b);
            }

            if (buttons.Count == 0)
                return;
            float step = buttons.Count > 1 ? buttons[1].anchoredPosition.y - buttons[0].anchoredPosition.y : -70f;
            int original = buttons.Count;
            while (buttons.Count < menu.Items.Count)
            {
                RectTransform last = buttons[buttons.Count - 1];
                RectTransform copy = Object.Instantiate(last, last.parent, false);
                copy.name = "Bouton_" + buttons.Count;
                copy.anchoredPosition = last.anchoredPosition + new Vector2(0f, step);
                buttons.Add(copy);
            }

            int extra = Mathf.Max(0, menu.Items.Count - original);
            if (extra > 0)
            {
                float grow = Mathf.Abs(step) * extra;
                foreach (RectTransform child in v.Pause.transform)
                    child.anchoredPosition += new Vector2(0f, grow * 0.5f);   // tout remonte d'un demi-pas par entrée ajoutée
                var frame = v.Pause.transform.Find("Cadre") as RectTransform;
                if (frame != null)
                    frame.sizeDelta += new Vector2(0f, grow);
            }

            for (int i = 0; i < buttons.Count; i++)
            {
                bool used = i < menu.Items.Count;
                buttons[i].gameObject.SetActive(used);
                if (!used)
                    continue;
                Text label = buttons[i].Find("Libelle")?.GetComponent<Text>();
                if (label != null)
                    label.text = menu.Items[i].Label;
                v.Buttons.Add((buttons[i].GetComponent<Selectable>(), label));
            }
        }

        private void Sync(PauseMenu.Open o, View v)
        {
            if (o.Player.camera != null)
                UiKit.FitTo(v.Root, o.Player.camera);                   // l'autre joueur rejoint ou part : la vue change
            if (v.Items != o.Menu.Items.Count)
                Layout(v, o.Menu);
            bool settings = o.InSettings && v.Settings != null;
            v.Pause.transform.parent.gameObject.SetActive(!settings && !o.InDevelopment);
            if (v.Settings != null)
                v.Settings.transform.parent.gameObject.SetActive(settings && !o.InDevelopment);
            MenuFige.Regle(GameSettings.ReduceMotion);
            bool mouse = o.Menu.Owner is Keyboard;
            if (o.InDevelopment)
                return;
            if (!settings)
            {
                for (int i = 0; i < v.Buttons.Count; i++)
                    Bind(o.Menu, i, v.Buttons[i].button, mouse);
                return;
            }

            GreyboxMenu m = o.Settings;
            for (int i = 0; i < v.Rows.Count && i < m.Items.Count; i++)
            {
                (RectTransform rect, Text value, Graphic[] tint) = v.Rows[i];
                GreyboxMenu.Item it = m.Items[i];
                if (value != null)
                    value.text = "‹  " + it.Value?.Invoke() + "  ›";
                bool over = mouse && MenuArtKit.Over(rect, null);
                if (over && MenuArtKit.MouseMoved)
                    m.SetFocus(i);
                MenuArtKit.Tint(tint, m.Focus == i ? MenuArtKit.State.Hover : MenuArtKit.State.Normal);
                if (over && MenuArtKit.Clicked)
                {
                    m.SetFocus(i);
                    it.Change?.Invoke(MenuArtKit.LocalX(rect, null) < 0.5f ? -1 : 1);
                }
            }

            Bind(m, m.Items.Count - 1, v.SettingsBack, mouse);
        }

        private static void Bind(GreyboxMenu m, int index, Selectable button, bool mouse)
        {
            if (button == null || index < 0 || index >= m.Items.Count)
                return;
            var rect = (RectTransform)button.transform;
            bool over = mouse && MenuArtKit.Over(rect, null);
            if (over && MenuArtKit.MouseMoved)
                m.SetFocus(index);
            MenuArtKit.State st = over && MenuArtKit.MouseDown ? MenuArtKit.State.Pressed : m.Focus == index ? MenuArtKit.State.Hover : MenuArtKit.State.Normal;
            MenuArtKit.Tint(MenuArtKit.Targets(button), st);
            if (over && MenuArtKit.Clicked)
            {
                m.SetFocus(index);
                m.Items[index].Submit?.Invoke();
            }
        }
    }
}
