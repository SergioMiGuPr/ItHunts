using ItHunts.Hud;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Menus
{
    /// <summary>
    /// Vue du panneau admin (nuit 3, point 2) dans la DA : l'écran Paramètres de la DA titré « Panneau admin », une ligne par action,
    /// ‹ valeur › pour les choix, la note de la dernière action sous le cadre. Posée par MenuArtInstall.
    /// </summary>
    public sealed class AdminPanelArt : MonoBehaviour
    {
        private AdminPanel _panel;
        private Canvas _canvas;
        private ArtMenuScreen _screen;
        private Text _note;

        public void Attach(AdminPanel panel)
        {
            _panel = panel;
            TryBuild();
        }

        private void TryBuild()
        {
            AdminPanel panel = _panel;
            PlayerInput pi = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
            MenuSkinSettings skin = PauseMenu.SkinOf(pi);
            if (skin == null || skin.settingsScreen == null)
                return;
            _canvas = UiKit.NewCanvas("Panneau admin (DA)", 90, transform);
            _screen = ArtMenuScreen.Create(skin.settingsScreen, _canvas.transform, "Menu_Admin", "Panneau admin");
            Font italic = null;
            foreach (Text t in skin.settingsScreen.GetComponentsInChildren<Text>(true))
                if (t.font != null)
                {
                    italic = t.font;
                    break;
                }

            _note = MenuArtKit.Label(_canvas.transform, "Note", italic, 26, new Color(0.92f, 0.85f, 0.7f), TextAnchor.LowerCenter);
            UiKit.Stretch(_note.rectTransform);
            _note.rectTransform.offsetMin = new Vector2(0f, 40f);
            panel.Skinned = true;
        }

        private void LateUpdate()
        {
            if (_panel != null && _screen == null)
                TryBuild();
            if (_panel == null || _screen == null)
                return;
            _screen.Sync(_panel.Menu, null, _panel.Open);
            _note.gameObject.SetActive(_panel.Open || !string.IsNullOrEmpty(_panel.Note));
            _note.text = _panel.Note;
        }
    }
}
