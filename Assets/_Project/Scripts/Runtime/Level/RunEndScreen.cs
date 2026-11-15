using ItHunts.Hud;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ItHunts.Level
{
    /// <summary>
    /// Écran de fin greybox (D-048) : mort du dernier joueur (solo : mort = fin de la run) ou victoire. Bilan court, puis Menu
    /// principal (ou Rejouer). Le jeu n'est pas mis en pause. Refait à l'étape 10.
    /// </summary>
    public sealed class RunEndScreen : MonoBehaviour
    {
        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private string _title, _text;

        public static RunEndScreen Current { get; private set; }
        public bool Victory { get; private set; }

        /// <summary>Affiche l'écran (une seule fois par scène).</summary>
        public static RunEndScreen Show(bool victory, string summary)
        {
            if (Current != null)
                return Current;
            var go = new GameObject(victory ? "Écran de fin (victoire)" : "Écran de fin (mort)");
            var s = go.AddComponent<RunEndScreen>();
            s.Victory = victory;
            s._title = victory ? "Victoire" : "Mort";
            s._text = summary;
            string scene = SceneManager.GetActiveScene().name;
            s._menu.Add("Menu principal", () => s.ToMenu());
            s._menu.Add("Rejouer", () => s.Replay(scene));
            s._menu.FocusFirst();
            Current = s;
            PlayerLook.MenuOpen = true;
            foreach (PlayerInput pi in FindObjectsByType<PlayerInput>(FindObjectsSortMode.None))
                pi.DeactivateInput();
            return s;
        }

        private void Update() => _menu.Update();

        private void OnGUI() => _menu.Draw(_title, _text);

        private void OnDestroy()
        {
            if (Current == this)
            {
                Current = null;
                PlayerLook.MenuOpen = false;
            }
        }

        private void ToMenu()
        {
            string menu = RunSession.MenuScene;
            RunSession.End();
            if (!RunSession.Load(menu))
                Replay(SceneManager.GetActiveScene().name);
        }

        /// <summary>Rejouer : la run recommence au niveau 1 si elle venait du menu, sinon la scène est rechargée.</summary>
        private void Replay(string scene)
        {
            if (RunSession.Active)
            {
                string first = RunSession.Begin(RunSession.Definition, RunSession.DifficultyIndex);
                if (RunSession.Load(first))
                    return;
            }

            SceneManager.LoadScene(scene);
        }
    }
}
