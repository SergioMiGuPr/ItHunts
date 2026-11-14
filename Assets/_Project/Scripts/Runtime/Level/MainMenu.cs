using ItHunts.Hud;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Menu principal greybox (D-048) : personnage (un seul), difficulté (Facile seule jouable), lancer le niveau 1, accès direct
    /// à Test_Movement et Test_Arena. Inviter un ami, multijoueur, paramètres et touches sont présents mais grisés. Souris,
    /// clavier et manette. Refait à l'étape 10 (structure de Ravenswatch et Dead by Daylight, aucun menu ne met en pause).
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private RunDefinition run;
        [SerializeField] private string movementScene = "Test_Movement";
        [SerializeField] private string arenaScene = "Test_Arena";

        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private int _character, _difficulty;
        private string _status = "";

        public RunDefinition Run => run;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ItHunts.Player.PlayerLook.MenuOpen = false;
            RunSession.End();
            RunSession.CoopDevice = null;
            Build();
        }

        private void Build()
        {
            _menu.Items.Clear();
            GreyboxMenu.Item c = _menu.Add("Personnage", null);
            c.Value = () => run != null && run.characters.Count > 0 ? run.characters[_character] : "—";
            c.Change = d => _character = run == null || run.characters.Count == 0 ? 0 : (_character + d + run.characters.Count) % run.characters.Count;
            GreyboxMenu.Item diff = _menu.Add("Difficulté", null);
            diff.Value = () =>
            {
                if (run == null || run.difficulties.Count == 0)
                    return "—";
                RunDifficulty d = run.difficulties[_difficulty];
                return d.playable ? d.displayName : $"{d.displayName} (bientôt)";
            };
            diff.Change = d => _difficulty = run == null || run.difficulties.Count == 0 ? 0 : (_difficulty + d + run.difficulties.Count) % run.difficulties.Count;
            _menu.Add("Lancer la run (niveau 1)", Launch);
            _menu.Add("Test_Movement (banc d'essai)", () => Direct(movementScene));
            _menu.Add("Test_Arena (labyrinthe)", () => Direct(arenaScene));
            GreyboxMenu.Item coop = _menu.Add("Joueur 2 (écran partagé)", null);
            coop.Value = () => RunSession.CoopDevice != null ? "prêt" : "Start sur sa manette";
            _menu.Add("Inviter un ami", null, false);
            _menu.Add("Multijoueur", null, false);
            _menu.Add("Paramètres", null, false);
            _menu.Add("Touches", null, false);
            _menu.Add("Quitter", Application.Quit);
            _menu.FocusFirst();
        }

        private void Launch()
        {
            if (run == null || run.difficulties.Count == 0)
            {
                _status = "Pas de définition de run.";
                return;
            }

            if (!run.difficulties[_difficulty].playable)
            {
                _status = $"{run.difficulties[_difficulty].displayName} : pas encore jouable. Facile seulement pour l'instant.";
                return;
            }

            string scene = RunSession.Begin(run, _difficulty, _character);
            if (!RunSession.Load(scene))
            {
                RunSession.End();
                _status = $"Scène « {scene} » absente du build (lancer le constructeur du niveau 1).";
            }
        }

        private void Direct(string scene)
        {
            RunSession.End();
            if (!RunSession.Load(scene))
                _status = $"Scène « {scene} » absente du build.";
        }

        private void Update()
        {
            _menu.Update();
            // Écran partagé (étape 9) : le 2e joueur rejoint avec Start sur une autre manette que celle du 1er.
            foreach (UnityEngine.InputSystem.Gamepad g in UnityEngine.InputSystem.Gamepad.all)
                if (g.startButton.wasPressedThisFrame && g != GreyboxMenu.LastNavPad)
                {
                    RunSession.CoopDevice = RunSession.CoopDevice == g ? null : g;   // Start de nouveau : il se retire
                    _status = RunSession.CoopDevice != null ? $"Joueur 2 prêt ({g.displayName}) : écran partagé au lancement." : "Joueur 2 retiré.";
                }
        }

        private void OnGUI()
        {
            string desc = run != null && run.difficulties.Count > 0 ? run.difficulties[_difficulty].description : "";
            _menu.Draw("IT HUNTS", string.IsNullOrEmpty(_status) ? desc : _status, 560f);
            GUI.Label(new Rect(16f, Screen.height - 30f, 900f, 24f), "Greybox (étape 8) : menu provisoire, refait à l'étape 10. Flèches / croix pour choisir, gauche-droite pour changer, Entrée / A.");
        }
    }
}
