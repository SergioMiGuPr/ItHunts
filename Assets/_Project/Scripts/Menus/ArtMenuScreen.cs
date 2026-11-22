using System.Collections.Generic;
using ItHunts.Hud;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Menus
{
    /// <summary>
    /// Écran de menu dans la DA (nuit 3) : l'écran Paramètres de la DA (cadre, cartouches, polices) avec un autre titre et d'autres
    /// lignes, lié à un GreyboxMenu (logique clavier, manette). La dernière entrée du menu est le bouton « Retour » de la maquette ;
    /// les autres sont des lignes (‹ valeur › si l'entrée a une valeur). Sert au menu Playtest et au panneau admin.
    /// </summary>
    public sealed class ArtMenuScreen
    {
        public GameObject Root { get; private set; }
        private readonly List<(RectTransform rect, Text value, Graphic[] tint)> _rows = new List<(RectTransform, Text, Graphic[])>();
        private Selectable _back;
        private Text _backLabel;
        private int _builtFor = -1;
        private GameObject _prefab;
        private Transform _parent;
        private string _name, _title;

        public static ArtMenuScreen Create(GameObject settingsPrefab, Transform parent, string name, string title)
        {
            if (settingsPrefab == null)
                return null;
            return new ArtMenuScreen { _prefab = settingsPrefab, _parent = parent, _name = name, _title = title };
        }

        private void Rebuild(GreyboxMenu m)
        {
            if (Root != null)
                Object.Destroy(Root);
            Root = MenuArtKit.Spawn(_prefab, _parent, _name);
            var title = MenuArtKit.Find<Text>(Root, "Titre");
            if (title == null)
                foreach (Text t in Root.GetComponentsInChildren<Text>(true))
                    if (t.name == "Titre" || t.text == "Paramètres")
                        title = t;
            if (title != null)
                title.text = _title;
            var labels = new List<string>();
            for (int i = 0; i < m.Items.Count - 1; i++)
                labels.Add(m.Items[i].Label);
            _rows.Clear();
            _rows.AddRange(MenuArtKit.SettingsRows(Root, labels, 120f));
            // Lignes en trop de la maquette (moins d'entrées que de lignes) : masquées.
            for (int i = labels.Count; ; i++)
            {
                Transform extra = Root.transform.Find("Option_" + i);
                if (extra == null)
                    break;
                extra.gameObject.SetActive(false);
            }

            _back = MenuArtKit.Find<Selectable>(Root, "Retour");
            _backLabel = _back != null ? _back.GetComponentInChildren<Text>(true) : null;
            _builtFor = m.Items.Count;
        }

        /// <summary>Met l'écran à l'état du menu ; open faux : caché.</summary>
        public void Sync(GreyboxMenu m, Camera cam, bool open, bool interactive = true)
        {
            if (!open || m == null || m.Items.Count == 0)
            {
                if (Root != null)
                    Root.SetActive(false);
                return;
            }

            if (Root == null || _builtFor != m.Items.Count)
                Rebuild(m);
            Root.SetActive(true);
            for (int i = 0; i < _rows.Count && i < m.Items.Count - 1; i++)
            {
                (RectTransform rect, Text value, Graphic[] tint) = _rows[i];
                GreyboxMenu.Item it = m.Items[i];
                Text label = rect.Find("Libelle")?.GetComponent<Text>();
                if (label != null && label.text != it.Label)
                    label.text = it.Label;
                if (value != null)
                {
                    string v = it.Value?.Invoke();
                    value.text = string.IsNullOrEmpty(v) ? "" : it.Change != null ? "‹  " + v + "  ›" : v;
                }

                bool over = interactive && MenuArtKit.Over(rect, cam);
                if (over && MenuArtKit.MouseMoved)
                    m.SetFocus(i);
                MenuArtKit.Tint(tint, !it.Enabled ? MenuArtKit.State.Disabled : m.Focus == i ? MenuArtKit.State.Hover : MenuArtKit.State.Normal);
                if (over && MenuArtKit.Clicked)
                {
                    m.SetFocus(i);
                    if (it.Change != null && value != null && !string.IsNullOrEmpty(value.text) && MenuArtKit.LocalX(rect, cam) > 0.55f)
                        it.Change(MenuArtKit.LocalX(rect, cam) < 0.78f ? -1 : 1);
                    else
                        it.Submit?.Invoke();
                }
            }

            GreyboxMenu.Item back = m.Items[m.Items.Count - 1];
            if (_back != null)
            {
                if (_backLabel != null && _backLabel.text != back.Label)
                    _backLabel.text = back.Label;
                int index = m.Items.Count - 1;
                bool over = interactive && MenuArtKit.Over((RectTransform)_back.transform, cam);
                if (over && MenuArtKit.MouseMoved)
                    m.SetFocus(index);
                MenuArtKit.Tint(MenuArtKit.Targets(_back), over && MenuArtKit.MouseDown ? MenuArtKit.State.Pressed
                    : m.Focus == index ? MenuArtKit.State.Hover : MenuArtKit.State.Normal);
                if (over && MenuArtKit.Clicked)
                {
                    m.SetFocus(index);
                    back.Submit?.Invoke();
                }
            }
        }

        public void Destroy()
        {
            if (Root != null)
                Object.Destroy(Root);
        }
    }
}
