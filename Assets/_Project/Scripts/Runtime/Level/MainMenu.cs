using ItHunts.Hud;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Menu principal greybox (D-048) : personnage de chaque joueur (D-064 : caméléon et renard jouables, koala et hibou grisés ;
    /// jamais deux fois le même en écran partagé), difficulté (Facile seule jouable), lancer le niveau 1, accès direct
    /// à Test_Movement et Test_Arena (avec le personnage choisi). Inviter un ami, multijoueur, paramètres et touches sont présents mais grisés. Souris,
    /// clavier et manette. Refait à l'étape 10 (structure de Ravenswatch et Dead by Daylight, aucun menu ne met en pause).
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private RunDefinition run;
        [SerializeField] private string movementScene = "Test_Movement";
        [SerializeField] private string arenaScene = "Test_Arena";
        [Tooltip("Carte fixe de référence du niveau 1 (comparer les réglages) ; les vraies parties sont tirées au hasard.")]
        [SerializeField] private string fixedLevelScene = "Level_01";

        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private int _difficulty;
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
            Fix();
            GreyboxMenu.Item c = _menu.Add("Joueur 1", null);
            c.Value = () => CharacterLabel(0);
            c.Change = d => Cycle(0, d);
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
            _menu.Add("Playtest : combat de boss", LaunchBossFight);
            _menu.Add("Test_Movement (banc d'essai)", () => Direct(movementScene));
            _menu.Add("Test_Arena (labyrinthe)", () => Direct(arenaScene));
            _menu.Add("Level_01 (carte fixe de référence)", () => Direct(fixedLevelScene));
            GreyboxMenu.Item coop = _menu.Add("Joueur 2 (écran partagé)", null);
            coop.Value = () => RunSession.CoopDevice != null ? CharacterLabel(1) : "Start sur sa manette";
            coop.Change = d =>
            {
                if (RunSession.CoopDevice != null)
                    Cycle(1, d);
            };
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

            Fix();
            string scene = RunSession.Begin(run, _difficulty);
            if (!RunSession.Load(scene))
            {
                RunSession.End();
                _status = $"Scène « {scene} » absente du build (lancer le constructeur du niveau 1).";
            }
        }

        /// <summary>Playtest : le niveau 1 en Facile, directement dans l'arène du combat de boss (traqueur, pièges, leviers).</summary>
        private void LaunchBossFight()
        {
            int easy = run != null ? run.difficulties.FindIndex(d => d.playable) : -1;
            Fix();
            string scene = easy >= 0 ? RunSession.Begin(run, easy) : null;
            if (scene == null)
            {
                _status = "Pas de niveau jouable pour le combat de boss.";
                return;
            }

            RunSession.BossPlaytest = true;
            if (!RunSession.Load(scene))
            {
                RunSession.BossPlaytest = false;
                RunSession.End();
                _status = $"Scène « {scene} » absente du build.";
            }
        }

        private void Direct(string scene)
        {
            Fix();
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
                    Fix();                                                        // il prend le premier personnage libre
                    _status = RunSession.CoopDevice != null ? $"Joueur 2 prêt ({g.displayName}) : écran partagé au lancement." : "Joueur 2 retiré.";
                }
        }

        // ---------- Personnages (D-064) ----------

        private CharacterRoster Roster => run != null ? run.characters : null;

        /// <summary>Choix gardés valides : un personnage jouable par joueur, jamais deux fois le même (CharacterPick).</summary>
        private void Fix()
        {
            CharacterRoster r = Roster;
            if (r == null || r.Count == 0)
                return;
            int[] req = RunSession.Characters ?? new[] { -1, -1 };
            if (req.Length < 2)
                req = new[] { req.Length > 0 ? req[0] : -1, -1 };
            RunSession.Characters = ItHunts.Artifacts.CharacterPick.Assign(r.Selectable(), req, 2, r.defaultIndex);
        }

        private void Cycle(int player, int dir)
        {
            CharacterRoster r = Roster;
            if (r == null || r.Count == 0)
                return;
            Fix();
            int[] all = RunSession.Characters;
            // Le 2e joueur ne compte que s'il a rejoint ; sans lui, le joueur 1 peut prendre n'importe quel personnage jouable.
            int[] taken = player == 0 ? (RunSession.CoopDevice != null ? new[] { all[1] } : new int[0]) : new[] { all[0] };
            all[player] = ItHunts.Artifacts.CharacterPick.Cycle(r.Selectable(), all[player], dir, taken);
            if (player == 0 && RunSession.CoopDevice == null && all[1] == all[0])
                all[1] = -1;                                              // repris par le joueur 2 quand il rejoindra
            RunSession.Characters = all;
            Fix();
            _status = "";
        }

        private string CharacterLabel(int player)
        {
            CharacterRoster r = Roster;
            if (r == null || r.Count == 0)
                return "—";
            int[] sel = RunSession.Characters;
            CharacterDefinition c = r[sel != null && player < sel.Length ? sel[player] : -1];
            return c != null ? $"{c.displayName} ({c.ArtifactName})" : "—";
        }

        /// <summary>Les personnages en cartes : couleur, nom, artefact ; grisés s'ils ne sont pas jouables ; [J1] / [J2] sur le choix.</summary>
        private void DrawCharacters(Rect area)
        {
            CharacterRoster r = Roster;
            if (r == null || r.Count == 0)
                return;
            _cardTitle ??= new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            _cardText ??= new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
            float cardH = Mathf.Min(110f, (area.height - 50f) / r.Count - 12f);
            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(area.x - 16f, area.y - 16f, area.width + 32f, 50f + r.Count * (cardH + 12f) + 20f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            _cardTitle.normal.textColor = Color.white;
            GUI.Label(new Rect(area.x, area.y, area.width, 34f), "Personnages", _cardTitle);
            int[] sel = RunSession.Characters ?? new int[0];
            for (int i = 0; i < r.Count; i++)
            {
                CharacterDefinition c = r[i];
                if (c == null)
                    continue;
                var card = new Rect(area.x, area.y + 50f + i * (cardH + 12f), area.width, cardH);
                bool on = c.Selectable;
                bool p1 = sel.Length > 0 && sel[0] == i;
                bool p2 = RunSession.CoopDevice != null && sel.Length > 1 && sel[1] == i;
                GUI.color = p1 || p2 ? new Color(1f, 0.85f, 0.4f, 0.3f) : new Color(1f, 1f, 1f, 0.06f);
                GUI.DrawTexture(card, Texture2D.whiteTexture);
                GUI.color = on ? c.color : Color.Lerp(c.color, new Color(0.3f, 0.3f, 0.3f), 0.75f);
                GUI.DrawTexture(new Rect(card.x + 12f, card.y + 12f, cardH - 24f, cardH - 24f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                _cardTitle.normal.textColor = on ? Color.white : new Color(0.5f, 0.5f, 0.5f);
                _cardText.normal.textColor = on ? new Color(0.85f, 0.85f, 0.85f) : new Color(0.45f, 0.45f, 0.45f);
                float x = card.x + cardH + 4f, w = card.width - cardH - 12f;
                string tags = (p1 ? "   [J1]" : "") + (p2 ? "   [J2]" : "");
                GUI.Label(new Rect(x, card.y + 10f, w, 30f), c.displayName + tags, _cardTitle);
                GUI.Label(new Rect(x, card.y + 42f, w, cardH - 46f), on ? c.ArtifactName : $"{c.ArtifactName} · bientôt", _cardText);
            }

            _cardTitle.normal.textColor = Color.white;
        }

        private GUIStyle _cardTitle, _cardText;

        private void OnGUI()
        {
            string desc = run != null && run.difficulties.Count > 0 ? run.difficulties[_difficulty].description : "";
            float split = Screen.width * 0.62f;
            _menu.Draw("IT HUNTS", string.IsNullOrEmpty(_status) ? desc : _status, 560f, new Rect(0f, 0f, split, Screen.height));
            DrawCharacters(new Rect(split + 10f, Screen.height * 0.14f, Mathf.Min(420f, Screen.width - split - 50f), Screen.height * 0.72f));
            GUI.Label(new Rect(16f, Screen.height - 30f, 900f, 24f), "Greybox (étape 8) : menu provisoire, refait à l'étape 10. Flèches / croix pour choisir, gauche-droite pour changer, Entrée / A.");
        }
    }
}
