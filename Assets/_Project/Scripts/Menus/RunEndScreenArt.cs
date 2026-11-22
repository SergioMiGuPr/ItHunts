using ItHunts.Hud;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Menus
{
    /// <summary>
    /// Vue de l'écran de fin de run (nuit 3, point 3) dans la DA : l'écran Paramètres de la DA titré « Victoire » ou « Mort », une
    /// ligne par statistique (libellé, valeur), la phrase d'accroche (cause de la mort) sous le titre, « Retour au campement » en bas ;
    /// R : nouvelle run tout de suite. Posée par MenuArtInstall.
    /// </summary>
    public sealed class RunEndScreenArt : MonoBehaviour
    {
        private RunEndScreen _end;
        private ArtMenuScreen _screen;
        private readonly GreyboxMenu _view = new GreyboxMenu();
        private Text _headline, _hint;

        public GameObject Screen => _screen?.Root;

        public void Attach(RunEndScreen end)
        {
            _end = end;
            PlayerInput pi = FindFirstObjectByType<PlayerInput>(FindObjectsInactive.Include);
            MenuSkinSettings skin = PauseMenu.SkinOf(pi);
            if (skin == null || skin.settingsScreen == null)
                return;
            Canvas canvas = UiKit.NewCanvas("Écran de fin (DA)", 95, transform);
            _screen = ArtMenuScreen.Create(skin.settingsScreen, canvas.transform, "Menu_FinDeRun", end.Title);
            foreach ((string label, string value) in end.Lines)
            {
                GreyboxMenu.Item it = _view.Add(label, null);
                string v = value;
                it.Value = () => v;
            }

            _view.Add("Retour au campement", end.ToCamp);
            _view.SetFocus(_view.Items.Count - 1);
            Font font = null;
            foreach (Text t in skin.settingsScreen.GetComponentsInChildren<Text>(true))
                if (t.font != null)
                {
                    font = t.font;
                    break;
                }

            _headline = MenuArtKit.Label(canvas.transform, "Accroche", font, 30, new Color(0.95f, 0.78f, 0.55f), TextAnchor.UpperCenter);
            UiKit.Stretch(_headline.rectTransform);
            _headline.rectTransform.offsetMax = new Vector2(0f, -40f);
            _headline.text = end.Headline;
            _hint = MenuArtKit.Label(canvas.transform, "Aide", font, 22, new Color(0.75f, 0.7f, 0.62f), TextAnchor.LowerCenter);
            UiKit.Stretch(_hint.rectTransform);
            _hint.rectTransform.offsetMin = new Vector2(0f, 24f);
            _hint.text = "Entrée : retour au campement   ·   R : nouvelle run tout de suite";
            end.Skinned = true;
        }

        private void LateUpdate()
        {
            if (_end == null || _screen == null)
                return;
            _screen.Sync(_view, null, true);
            // Les lignes du bilan ne se choisissent pas : Entrée valide toujours le retour au campement.
            Keyboard k = Keyboard.current;
            if (k != null && k.rKey.wasPressedThisFrame)
                _end.Replay();
            else if (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame))
                _end.ToCamp();
            Gamepad g = Gamepad.current;
            if (g != null && g.buttonSouth.wasPressedThisFrame)
                _end.ToCamp();
            else if (g != null && g.buttonNorth.wasPressedThisFrame)
                _end.Replay();
        }
    }
}
