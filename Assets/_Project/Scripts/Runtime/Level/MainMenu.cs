using System.Collections.Generic;
using ItHunts.Hud;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Level
{
    /// <summary>
    /// Menus d'avant-partie (lot B de la DA, UI-HANDOFF § 2), un panorama unique :
    /// - écran titre : ciel, lune et halo qui pulse, nuages et brume qui défilent, château dont les fenêtres clignotent, parallaxe
    ///   selon la souris ou le stick droit ; titre (logo provisoire en Grenze Gotisch) ; Jouer, Paramètres, Playtest, Quitter ;
    ///   musique d'ambiance provisoire ;
    /// - Jouer : la caméra glisse vers la droite jusqu'au campement (courbe et durée réglables) ;
    /// - campement (CampLobby) : 4 places autour du feu qui palpite ; J1 = dernier personnage joué ; Start sur une autre manette
    ///   fait rejoindre un joueur (A : prêt, Y : modifier, Start : se retirer) ; sélecteur ACTE I / II / III ; Modifier ouvre le
    ///   choix du personnage ; Prêt et Jouer refusés tant qu'il y a un doublon ; chat et amis masqués (jeu en ligne plus tard) ;
    /// - Paramètres (options d'image), Playtest (scènes de test, combat de boss).
    /// Navigation complète : souris, clavier (flèches, Entrée, Échap), manette (croix ou stick, A, B). Aucun menu ne met en pause.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        [SerializeField] private RunDefinition run;
        [SerializeField] private MenuSkinSettings skin;
        [SerializeField] private string movementScene = "Test_Movement";
        [SerializeField] private string arenaScene = "Test_Arena";
        [Tooltip("Carte fixe de référence du niveau 1 (comparer les réglages) ; les vraies parties sont tirées au hasard.")]
        [SerializeField] private string fixedLevelScene = "Level_01";

        /// <summary>Scène vitrine (retours du lot, point 9) : assets de la DA, ennemis dans chaque état, vue FPS.</summary>
        public const string VitrineScene = "Test_Vitrine";

        /// <summary>Le prochain chargement du menu ouvre directement le campement (Abandonner la run).</summary>
        public static bool StartAtCamp { get; set; }

        public enum Page : byte { Titre, Glissement, Campement, Parametres, Playtest }

        // Menus (logique clavier-manette de GreyboxMenu ; affichés en uGUI). _menu : le menu Playtest (noms gardés par les tests).
        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private readonly GreyboxMenu _title = new GreyboxMenu(), _camp = new GreyboxMenu(), _settings = new GreyboxMenu();
        private CampLobby _lobby;
        private int _act;
        private string _status = "";
        private Page _page = Page.Titre, _back = Page.Titre, _glideTo = Page.Campement;
        private float _glide, _glideFrom, _glideTarget, _glideT;
        private GreyboxMenu.Item _play, _ready, _modify;

        public RunDefinition Run => run;
        public Page Current => _page;
        public CampLobby Lobby => _lobby;
        /// <summary>Glissement du panorama : 0 écran titre, 1 campement.</summary>
        public float Glide => _glide;

        private CharacterRoster Roster => run != null ? run.characters : null;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ItHunts.Player.PlayerLook.MenuOpen = false;
            RunSession.End();
            RunSession.CoopDevice = null;
            Build();
            if (skin != null)
                BuildUi();
            if (StartAtCamp)
            {
                StartAtCamp = false;
                _page = Page.Campement;
                _glide = 1f;
                _camp.FocusFirst();
            }

            StartMusic();
        }

        // ---------- Menus ----------

        private void Build()
        {
            CharacterRoster r = Roster;
            int first = r != null ? r.defaultIndex : 0;
            string last = CampLobby.LoadLast();
            if (r != null && !string.IsNullOrEmpty(last))
                for (int i = 0; i < r.Count; i++)
                    if (r[i] != null && r[i].id == last && r[i].Selectable)
                        first = i;
            _lobby = new CampLobby(r != null ? r.Selectable() : new bool[0], first);

            _title.Items.Clear();
            _title.Add("Jouer", () => GoTo(Page.Campement));
            _title.Add("Paramètres", () => OpenSettings(Page.Titre));
            _title.Add("Playtest", () => { _back = Page.Titre; _page = Page.Playtest; _menu.FocusFirst(); });
            _title.Add("Quitter", Application.Quit);
            _title.FocusFirst();

            _camp.Items.Clear();
            GreyboxMenu.Item act = _camp.Add("Acte", null);
            act.Value = () => ActLabel(_act) + (ActPlayable(_act) ? "" : " (bientôt)");
            act.Change = d => _act = (_act + d + 3) % 3;
            _modify = _camp.Add("Modifier le personnage", () => OpenSelect(0));
            _ready = _camp.Add("Prêt", () => ToggleReady(0));
            _ready.Value = () => _lobby[0] != null && _lobby[0].Ready ? "oui" : "non";
            _ready.Change = d => ToggleReady(0);
            _play = _camp.Add("Jouer", Launch);
            _play.DisabledNote = "(tous prêts, sans doublon)";
            _camp.Add("Paramètres", () => OpenSettings(Page.Campement));
            _camp.Add("Retour", () => GoTo(Page.Titre));
            _camp.FocusFirst();

            _settings.Items.Clear();
            foreach (GraphicsOptions.Effect e in System.Enum.GetValues(typeof(GraphicsOptions.Effect)))
            {
                GraphicsOptions.Effect effect = e;
                GreyboxMenu.Item item = _settings.Add(GraphicsOptions.Label(effect), null);
                item.Value = () => GraphicsOptions.Get(effect) ? "oui" : "non";
                item.Change = d => GraphicsOptions.Set(effect, !GraphicsOptions.Get(effect));
                item.Submit = () => item.Change(1);
            }

            _settings.Add("Retour", () => _page = _back);
            _settings.FocusFirst();

            _menu.Items.Clear();
            _menu.Add("Lancer la run (niveau 1)", Launch);
            _menu.Add("Playtest : combat de boss", LaunchBossFight);
            _menu.Add("Playtest : vitrine", () => Direct(VitrineScene));
            _menu.Add("Test_Movement (banc d'essai)", () => Direct(movementScene));
            _menu.Add("Test_Arena (labyrinthe)", () => Direct(arenaScene));
            _menu.Add("Level_01 (carte fixe de référence)", () => Direct(fixedLevelScene));
            _menu.Add("Retour", () => _page = _back);
            _menu.FocusFirst();
        }

        private static string ActLabel(int act) => act switch { 0 => "ACTE I", 1 => "ACTE II", _ => "ACTE III" };
        private static bool ActPlayable(int act) => act == 0;

        private void OpenSettings(Page from)
        {
            _back = from;
            _page = Page.Parametres;
            _settings.FocusFirst();
        }

        private void GoTo(Page target)
        {
            _glideFrom = _glide;
            _glideTarget = target == Page.Campement ? 1f : 0f;
            _glideTo = target;
            _glideT = 0f;
            _page = Page.Glissement;
            _status = "";
        }

        private void ToggleReady(int seat)
        {
            if (_lobby.ToggleReady(seat))
                _status = "";
            else if (_lobby.Duplicate(seat))
                _status = $"J{seat + 1} : un autre joueur a le même personnage. Changez-en pour être prêt.";
        }

        // ---------- Lancement ----------

        /// <summary>Les personnages du campement passent à la run (J1, puis le joueur qui a rejoint) ; J1 est enregistré.</summary>
        private void ApplyLobby()
        {
            var chars = new List<int> { _lobby[0].Character };
            RunSession.CoopDevice = null;
            for (int i = 1; i < CampLobby.Seats; i++)
                if (_lobby[i] != null)
                {
                    chars.Add(_lobby[i].Character);
                    RunSession.CoopDevice = _lobby[i].Device as InputDevice;
                    break;                                               // écran partagé à 2 pour l'instant
                }

            while (chars.Count < 2)
                chars.Add(-1);
            RunSession.Characters = chars.ToArray();
            CharacterDefinition c = Roster != null ? Roster[_lobby[0].Character] : null;
            if (c != null)
                CampLobby.SaveLast(c.id);
        }

        private void Launch()
        {
            if (!_lobby.CanLaunch)
            {
                _status = "Chaque joueur se met prêt, sans personnage en double.";
                return;
            }

            if (!ActPlayable(_act))
            {
                _status = $"{ActLabel(_act)} : pas encore jouable.";
                return;
            }

            int easy = run != null ? run.difficulties.FindIndex(d => d.playable) : -1;
            if (easy < 0)
            {
                _status = "Pas de définition de run.";
                return;
            }

            ApplyLobby();
            string scene = RunSession.Begin(run, easy);
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
            ApplyLobby();
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
            ApplyLobby();
            RunSession.End();
            if (!RunSession.Load(scene))
                _status = $"Scène « {scene} » absente du build.";
        }

        // ---------- Entrées ----------

        private static bool BackPressed(InputDevice except)
        {
            Keyboard k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame)
                return true;
            foreach (Gamepad g in Gamepad.all)
                if (g != except && g.buttonEast.wasPressedThisFrame)
                    return true;
            return false;
        }

        private InputDevice SecondPad => _lobby != null ? (_lobby[1]?.Device as InputDevice) : null;

        private void Update()
        {
            if (_lobby == null)
                return;
            InputDevice p2 = SecondPad;
            _title.Exclude = _camp.Exclude = _settings.Exclude = _menu.Exclude = p2;
            switch (_page)
            {
                case Page.Titre:
                    _title.Update();
                    break;
                case Page.Glissement:
                    float d = skin != null ? skin.glideSeconds : 1.5f;
                    _glideT = Mathf.Min(1f, _glideT + Time.unscaledDeltaTime / Mathf.Max(0.05f, d));
                    float k = skin != null ? skin.glideCurve.Evaluate(_glideT) : Mathf.SmoothStep(0f, 1f, _glideT);
                    _glide = Mathf.Lerp(_glideFrom, _glideTarget, k);
                    if (_glideT >= 1f)
                    {
                        _page = _glideTo;
                        (_page == Page.Campement ? _camp : _title).FocusFirst();
                    }

                    break;
                case Page.Campement:
                    UpdateCamp(p2);
                    break;
                case Page.Parametres:
                    _settings.Update();
                    if (BackPressed(p2))
                        _page = _back;
                    break;
                case Page.Playtest:
                    _menu.Update();
                    if (BackPressed(p2))
                        _page = _back;
                    break;
            }

            if (_ui != null)
                SyncUi();
        }

        private void UpdateCamp(InputDevice p2)
        {
            _play.Enabled = _lobby.CanLaunch && ActPlayable(_act);
            bool selecting = false;
            for (int seat = 0; seat < CampLobby.Seats; seat++)
                if (_select != null && _select[seat] != null && _select[seat].IsOpen)
                {
                    selecting |= seat == 0;
                    int pick = _select[seat].Update();
                    if (pick == CharacterSelectPanel.Cancel)
                        _select[seat].Close();
                    else if (pick >= 0)
                    {
                        _lobby.SetCharacter(seat, pick);
                        _select[seat].Close();
                    }
                }

            if (!selecting)
            {
                _camp.Update();
                if (BackPressed(p2))
                    GoTo(Page.Titre);
            }

            // Écran partagé : Start sur une autre manette que celle du joueur 1 fait rejoindre (ou retirer) un joueur.
            foreach (Gamepad g in Gamepad.all)
            {
                if (!g.startButton.wasPressedThisFrame || g == GreyboxMenu.LastNavPad)
                    continue;
                if (_lobby.SeatOf(g) >= 0)
                {
                    _lobby.Leave(g);
                    _status = "Un joueur s'est retiré.";
                }
                else
                {
                    int seat = _lobby.Join(g);
                    _status = seat > 0 ? $"Joueur {seat + 1} a rejoint ({g.displayName}) : A prêt, Y modifier, Start se retirer."
                        : "Plus de place en écran partagé (le jeu en ligne viendra plus tard).";
                }
            }

            // Les joueurs qui ont rejoint : leur manette seule.
            for (int seat = 1; seat < CampLobby.Seats; seat++)
            {
                if (!(_lobby[seat]?.Device is Gamepad pad) || (_select != null && _select[seat] != null && _select[seat].IsOpen))
                    continue;
                if (pad.buttonSouth.wasPressedThisFrame)
                    ToggleReady(seat);
                else if (pad.buttonNorth.wasPressedThisFrame)
                    OpenSelect(seat);
                else if (pad.buttonEast.wasPressedThisFrame && _lobby[seat].Ready)
                    _lobby[seat].Ready = false;
            }
        }

        // ---------- Interface (uGUI) ----------

        private sealed class Ui
        {
            public Canvas Canvas;
            public RectTransform Pages, TitlePage, CampPage, TitleScene, CampScene;
            public UiList Title, Camp, Settings, Playtest;
            public RectTransform SettingsPanel, PlaytestPanel;
            public Text Status, ActHeader;
            public readonly List<(RectTransform rect, float parallax, float scroll, float width)> Layers = new List<(RectTransform, float, float, float)>();
            public Image MoonHalo, Windows, FireAdd, Fire;
            public readonly List<(Image figure, Text label, Text state, Image seat)> Seats = new List<(Image, Text, Text, Image)>();
            public GameObject Chat, Friends;
        }

        private Ui _ui;
        private CharacterSelectPanel[] _select;

        /// <summary>Interface construite (tests, captures).</summary>
        public Canvas MenuCanvas => _ui?.Canvas;
        public CharacterSelectPanel SelectPanel(int seat) => _select != null && seat >= 0 && seat < _select.Length ? _select[seat] : null;

        private const float CanvasW = 2752f, CanvasH = 1536f;

        private void BuildUi()
        {
            _ui = new Ui { Canvas = UiKit.NewCanvas("Menus (lot B)", 50, transform) };
            var root = (RectTransform)_ui.Canvas.transform;
            _ui.Pages = UiKit.Stretch(UiKit.Rect("Panorama", root, Vector2.zero, Vector2.zero, Vector2.zero));
            UiKit.Img("Noir", _ui.Pages, null, Color.black).rectTransform.SetAsFirstSibling();
            UiKit.Stretch((RectTransform)_ui.Pages.GetChild(0));
            _ui.TitlePage = UiKit.Stretch(UiKit.Rect("Écran titre", _ui.Pages, Vector2.zero, Vector2.zero, Vector2.zero));
            _ui.CampPage = UiKit.Stretch(UiKit.Rect("Campement", _ui.Pages, Vector2.zero, Vector2.zero, Vector2.zero));
            _ui.TitleScene = SceneRect(_ui.TitlePage);
            _ui.CampScene = SceneRect(_ui.CampPage);
            BuildTitleBackdrop();
            BuildTitleUi();
            BuildCamp();
            _ui.SettingsPanel = CenterPanel(root, "Paramètres", _settings, out _ui.Settings);
            _ui.PlaytestPanel = CenterPanel(root, "Playtest", _menu, out _ui.Playtest);
            _ui.Status = UiKit.Txt("État", root, skin.textFont, 26, UiKit.Gold);
            _ui.Status.rectTransform.anchorMin = new Vector2(0f, 0f);
            _ui.Status.rectTransform.anchorMax = new Vector2(1f, 0f);
            _ui.Status.rectTransform.anchoredPosition = new Vector2(0f, 40f);
            _ui.Status.rectTransform.sizeDelta = new Vector2(-200f, 40f);
            var region = UiKit.Stretch(UiKit.Rect("Choix des personnages", root, Vector2.zero, Vector2.zero, Vector2.zero));
            _select = new CharacterSelectPanel[CampLobby.Seats];
            for (int i = 0; i < CampLobby.Seats; i++)
                _select[i] = new CharacterSelectPanel(region, skin, Roster);
        }

        /// <summary>Rectangle de la toile (2752 × 1536) qui couvre la page (sans déformation, rogné sur les bords).</summary>
        private static RectTransform SceneRect(RectTransform page)
        {
            RectTransform r = UiKit.Rect("Toile", page, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CanvasW, CanvasH));
            var fit = r.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = CanvasW / CanvasH;
            return r;
        }

        /// <summary>Calque posé dans la toile par son rectangle (px de la toile, origine en haut à gauche).</summary>
        private Image Layer(string name, RectTransform scene, Sprite sprite, float x, float yTop, float w, float h, bool additive = false, float parallax = 0f, float scroll = 0f)
        {
            Image img = UiKit.Img(name, scene, sprite, Color.white, Image.Type.Simple, additive ? skin.additive : null);
            RectTransform r = img.rectTransform;
            r.anchorMin = new Vector2(x / CanvasW, 1f - (yTop + h) / CanvasH);
            r.anchorMax = new Vector2((x + w) / CanvasW, 1f - yTop / CanvasH);
            r.offsetMin = r.offsetMax = Vector2.zero;
            _ui.Layers.Add((r, parallax, scroll, w / CanvasW));
            return img;
        }

        private float Px(Sprite s, bool width) => s == null ? 0f : width ? s.rect.width : s.rect.height;

        private void BuildTitleBackdrop()
        {
            RectTransform sc = _ui.TitleScene;
            Layer("Ciel", sc, skin.sky, -60f, -40f, CanvasW + 120f, CanvasH + 80f, parallax: skin.skyParallax);
            if (skin.moon != null)
            {
                float d = Px(skin.moon, true) * skin.moonScale;
                float cx = skin.moonAt.x * CanvasW, cy = (1f - skin.moonAt.y) * CanvasH;
                _ui.MoonHalo = Layer("Lune : halo", sc, skin.moonHalo, cx - d / 2f, cy - d / 2f, d, d, true, skin.moonParallax);
                Layer("Lune", sc, skin.moon, cx - d / 2f, cy - d / 2f, d, d, false, skin.moonParallax);
            }

            // Nuages : les bandes ne se raccordent pas (un bord coupé) : une copie un peu plus large que la toile, le bord coupé hors de
            // l'écran, qui dérive lentement en aller-retour (dérive : signe de « scroll » négatif pour les distinguer du défilement).
            float[] cloudY = { 30f, 170f, 330f, 470f, 600f };
            for (int i = 0; i < skin.clouds.Length; i++)
            {
                Sprite s = skin.clouds[i];
                if (s == null)
                    continue;
                float w = CanvasW * 1.14f, h = Px(s, false) * 1.05f, y = cloudY[Mathf.Min(i, cloudY.Length - 1)];
                float x = i % 2 == 0 ? -CanvasW * 0.1f : -CanvasW * 0.04f;
                float par = i < 2 ? skin.farCloudParallax : skin.nearCloudParallax;
                Layer($"Nuages {i + 1}", sc, s, x, y, w, h, false, par, -(skin.cloudSpeed * (0.6f + 0.2f * i))).color = new Color(1f, 1f, 1f, i < 2 ? 0.75f : 0.9f);
            }

            if (skin.castle != null)
            {
                float w = Px(skin.castle, true), h = Px(skin.castle, false);
                Layer("Château", sc, skin.castle, skin.castleAt.x, skin.castleAt.y, w, h, false, skin.castleParallax);
                if (skin.castleWindows != null)
                    _ui.Windows = Layer("Château : fenêtres", sc, skin.castleWindows, skin.castleAt.x, skin.castleAt.y, w, h, true, skin.castleParallax);
            }

            float[] fogY = { CanvasH - 700f, CanvasH - 560f };
            for (int i = 0; i < skin.fog.Length; i++)
            {
                Sprite s = skin.fog[i];
                if (s == null)
                    continue;
                float w = Px(s, true), h = Px(s, false);
                for (int c = 0; c < 2; c++)
                    Layer($"Brume {i + 1} ({c + 1})", sc, s, c * w, fogY[Mathf.Min(i, 1)], w, h, true, i == 0 ? skin.farFogParallax : skin.nearFogParallax,
                        skin.fogSpeed * (i == 0 ? 0.6f : 1f)).color = new Color(1f, 1f, 1f, 0.75f);
            }
        }

        private void BuildTitleUi()
        {
            RectTransform col = UiKit.Rect("Colonne", _ui.TitlePage, new Vector2(0f, 1f), new Vector2(150f, -170f), new Vector2(620f, 800f), new Vector2(0f, 1f));
            Text logo = UiKit.Txt("Titre (logo provisoire)", col, skin.titleFont, 150, new Color(0.92f, 0.88f, 0.9f), TextAnchor.UpperLeft);
            logo.rectTransform.anchorMin = new Vector2(0f, 1f);
            logo.rectTransform.anchorMax = new Vector2(1f, 1f);
            logo.rectTransform.pivot = new Vector2(0f, 1f);
            logo.rectTransform.sizeDelta = new Vector2(0f, 180f);
            logo.text = "IT HUNTS";
            RectTransform list = UiKit.Rect("Boutons", col, new Vector2(0f, 1f), new Vector2(0f, -300f), new Vector2(480f, 300f), new Vector2(0f, 1f));
            _ui.Title = new UiList(_title, list, skin, 66f, 38, TextAnchor.MiddleLeft);
        }

        private void BuildCamp()
        {
            RectTransform sc = _ui.CampScene;
            Layer("Fond du campement", sc, skin.campBackground, 0f, 0f, CanvasW, CanvasH);
            for (int i = 0; i < CampLobby.Seats; i++)
            {
                Sprite seat = skin.seats != null && skin.seats.Length > 0 ? skin.seats[i % skin.seats.Length] : null;
                Vector2 at = skin.seatAnchors[Mathf.Min(i, skin.seatAnchors.Length - 1)];
                float w = Px(seat, true) * skin.seatScale, h = Px(seat, false) * skin.seatScale;
                float x = at.x * CanvasW - w / 2f, yTop = (1f - at.y) * CanvasH - h;
                Image figure = Layer($"Place {i + 1} : personnage", sc, UiKit.Silhouette(), x + w / 2f - 100f, yTop - 200f, 200f, 300f);
                figure.preserveAspect = true;
                Image s = Layer($"Place {i + 1} : siège", sc, seat, x, yTop, w, h);
                RectTransform tagRect = UiKit.Rect($"Place {i + 1} : étiquette", sc, new Vector2(at.x, 1f - (yTop - 215f) / CanvasH), Vector2.zero, new Vector2(460f, 110f), new Vector2(0.5f, 0f));
                Text label = UiKit.Txt("Joueur", tagRect, skin.titleFont, 40, UiKit.TextColor);
                UiKit.Stretch(label.rectTransform);
                label.rectTransform.offsetMin = new Vector2(0f, 50f);
                Text state = UiKit.Txt("État", tagRect, skin.textFont, 25, UiKit.Gold);
                UiKit.Stretch(state.rectTransform);
                state.rectTransform.offsetMax = new Vector2(0f, -70f);
                _ui.Seats.Add((figure, label, state, s));
            }

            if (skin.fire != null)
            {
                float w = Px(skin.fire, true) * skin.fireScale, h = Px(skin.fire, false) * skin.fireScale;
                float x = skin.fireAt.x * CanvasW - w / 2f, yTop = (1f - skin.fireAt.y) * CanvasH - h;
                _ui.Fire = Layer("Feu", sc, skin.fire, x, yTop, w, h);
                _ui.FireAdd = Layer("Feu : lueur", sc, skin.fireAdditive, x, yTop, w, h, true);
            }

            _ui.ActHeader = UiKit.Txt("Acte", _ui.CampPage, skin.titleFont, 84, new Color(0.92f, 0.88f, 0.9f), TextAnchor.UpperLeft);
            _ui.ActHeader.rectTransform.anchorMin = _ui.ActHeader.rectTransform.anchorMax = new Vector2(0f, 1f);
            _ui.ActHeader.rectTransform.pivot = new Vector2(0f, 1f);
            _ui.ActHeader.rectTransform.anchoredPosition = new Vector2(90f, -50f);
            _ui.ActHeader.rectTransform.sizeDelta = new Vector2(700f, 110f);
            RectTransform panel = UiKit.Rect("Menu du campement", _ui.CampPage, new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(600f, 6 * 52f + 60f), new Vector2(1f, 1f));
            UiKit.Panel(panel, skin, 0.7f);
            RectTransform list = UiKit.Stretch(UiKit.Rect("Boutons", panel, Vector2.zero, Vector2.zero, Vector2.zero), 30f);
            _ui.Camp = new UiList(_camp, list, skin, 52f, 29, TextAnchor.MiddleLeft);
            // Jeu en ligne plus tard : chat (bas gauche) et amis, présents mais masqués.
            _ui.Chat = UiKit.Rect("Chat (en ligne, plus tard)", _ui.CampPage, new Vector2(0f, 0f), new Vector2(70f, 70f), new Vector2(520f, 220f), Vector2.zero).gameObject;
            UiKit.Panel(_ui.Chat.transform, skin, 0.6f);
            _ui.Chat.SetActive(false);
            _ui.Friends = UiKit.Rect("Amis (en ligne, plus tard)", _ui.CampPage, new Vector2(1f, 1f), new Vector2(-70f, -60f), new Vector2(320f, 70f), Vector2.one).gameObject;
            _ui.Friends.SetActive(false);
        }

        private RectTransform CenterPanel(RectTransform root, string title, GreyboxMenu menu, out UiList list)
        {
            RectTransform shade = UiKit.Stretch(UiKit.Rect(title, root, Vector2.zero, Vector2.zero, Vector2.zero));
            UiKit.Img("Voile", shade, null, UiKit.Shade).rectTransform.anchorMin = Vector2.zero;
            UiKit.Stretch((RectTransform)shade.GetChild(0));
            RectTransform panel = UiKit.Rect("Panneau", shade, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, menu.Items.Count * 60f + 190f));
            UiKit.Panel(panel, skin);
            Text t = UiKit.Txt("Titre", panel, skin.titleFont, 58, UiKit.TextColor);
            t.rectTransform.anchorMin = new Vector2(0f, 1f);
            t.rectTransform.anchorMax = new Vector2(1f, 1f);
            t.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            t.rectTransform.sizeDelta = new Vector2(0f, 80f);
            t.text = title;
            RectTransform l = UiKit.Rect("Boutons", panel, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(720f, menu.Items.Count * 60f), new Vector2(0.5f, 1f));
            list = new UiList(menu, l, skin, 60f, 30);
            shade.gameObject.SetActive(false);
            return shade;
        }

        private Vector2 _pointer;

        private void SyncUi()
        {
            float t = Time.unscaledTime;
            float w = ((RectTransform)_ui.Canvas.transform).rect.width;
            _ui.TitlePage.anchoredPosition = new Vector2(-_glide * w, 0f);
            _ui.CampPage.anchoredPosition = new Vector2((1f - _glide) * w, 0f);

            // Parallaxe : la souris (ou le stick droit) décale les calques selon leur profondeur.
            Vector2 target = Vector2.zero;
            if (Mouse.current != null && Screen.width > 0)
                target = new Vector2(Mouse.current.position.ReadValue().x / Screen.width * 2f - 1f, Mouse.current.position.ReadValue().y / Screen.height * 2f - 1f);
            if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.05f)
                target = Gamepad.current.rightStick.ReadValue();
            _pointer = Vector2.Lerp(_pointer, Vector2.ClampMagnitude(target, 1f), 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
            float sceneW = _ui.TitleScene.rect.width;
            foreach ((RectTransform r, float parallax, float scroll, float width) in _ui.Layers)
            {
                Vector2 off = -_pointer * skin.parallaxPixels * parallax;
                if (scroll < 0f)
                    off.x += Mathf.Sin(t * -scroll * 0.02f + parallax * 17f) * 60f;           // dérive des nuages (aller-retour)
                else if (scroll > 0f)
                {
                    // Deux copies côte à côte décalées ensemble : la bande défile sans couture.
                    float bandW = width * sceneW;
                    float s = Mathf.Repeat(t * Mathf.Abs(scroll) * sceneW / CanvasW, Mathf.Max(1f, bandW));
                    off.x += s - bandW;
                }

                r.anchoredPosition = off;
            }

            if (_ui.MoonHalo != null)
                _ui.MoonHalo.color = Color.white * (1f - skin.moonPulseDepth * 0.5f + skin.moonPulseDepth * 0.5f * Mathf.Sin(t * skin.moonPulseHz * 2f * Mathf.PI));
            if (_ui.Windows != null)
            {
                float n = Mathf.PerlinNoise(t * skin.windowFlickerSpeed, 0.37f);
                _ui.Windows.color = skin.windowColor * (1f - skin.windowFlickerDepth + skin.windowFlickerDepth * n);
            }

            if (_ui.FireAdd != null)
            {
                float n = Mathf.PerlinNoise(t * skin.fireFlickerSpeed, 3.1f);
                _ui.FireAdd.color = Color.white * (1f - skin.fireFlickerDepth + skin.fireFlickerDepth * 2f * n);
                _ui.Fire.rectTransform.localScale = new Vector3(1f, 1f + 0.04f * (n - 0.5f), 1f);
            }

            _ui.Title.Root.gameObject.SetActive(_glide < 0.99f);
            if (_page == Page.Titre)
                _ui.Title.Sync();
            _ui.Camp.Root.parent.gameObject.SetActive(_glide > 0.01f);
            if (_page == Page.Campement && !SelectOpen(0))
                _ui.Camp.Sync();
            _ui.SettingsPanel.gameObject.SetActive(_page == Page.Parametres);
            if (_page == Page.Parametres)
                _ui.Settings.Sync();
            _ui.PlaytestPanel.gameObject.SetActive(_page == Page.Playtest);
            if (_page == Page.Playtest)
                _ui.Playtest.Sync();
            _ui.ActHeader.text = ActLabel(_act);
            SyncSeats();
            PlaceSelectPanels();
            _ui.Status.text = _status;
        }

        private bool SelectOpen(int seat) => _select != null && _select[seat] != null && _select[seat].IsOpen;

        private void SyncSeats()
        {
            CharacterRoster r = Roster;
            for (int i = 0; i < _ui.Seats.Count; i++)
            {
                (Image figure, Text label, Text state, Image seat) = _ui.Seats[i];
                CampLobby.Seat s = _lobby[i];
                CharacterDefinition c = s != null && r != null ? r[s.Character] : null;
                figure.enabled = c != null;
                if (c != null)
                    figure.color = Color.Lerp(c.color, Color.black, 0.35f);
                label.text = s == null ? "" : $"J{i + 1}" + (c != null ? $" · {c.displayName}" : "");
                bool online = i >= _lobby.MaxLocal;
                state.text = s != null
                    ? (_lobby.Duplicate(i) ? "Même personnage qu'un autre joueur" : s.Ready ? "Prêt ✓" : i == 0 ? "Pas prêt" : "A : prêt · Y : modifier")
                    : online ? "En ligne, plus tard" : i == 1 || _lobby.Count < _lobby.MaxLocal ? "Start sur une manette pour rejoindre" : "";
                state.color = s != null && _lobby.Duplicate(i) ? new Color(1f, 0.45f, 0.35f) : s != null && s.Ready ? UiKit.Gold : UiKit.Dim;
                seat.color = s != null ? Color.white : new Color(0.55f, 0.55f, 0.6f);
            }
        }

        /// <summary>Un choix ouvert : au centre ; deux : J1 à gauche, l'autre à droite, réduits pour tenir.</summary>
        private void PlaceSelectPanels()
        {
            int open = 0;
            for (int i = 0; i < CampLobby.Seats; i++)
                if (SelectOpen(i))
                    open++;
            int k = 0;
            for (int i = 0; i < CampLobby.Seats; i++)
            {
                if (!SelectOpen(i))
                    continue;
                RectTransform r = _select[i].Root;
                float x = open == 1 ? 0.5f : k == 0 ? 0.25f : 0.75f;
                r.anchorMin = r.anchorMax = new Vector2(x, 0.5f);
                r.anchoredPosition = Vector2.zero;
                r.localScale = Vector3.one * (open == 1 ? 1f : 0.85f);
                k++;
            }
        }

        private void OpenSelect(int seat)
        {
            CampLobby.Seat s = _lobby[seat];
            if (s == null || _select == null || _select[seat] == null)
                return;
            InputDevice owner = seat == 0 ? null : s.Device as InputDevice;
            _select[seat].Open($"Joueur {seat + 1}", s.Character, i =>
            {
                List<int> who = _lobby.TakenBy(i, seat);
                return who.Count == 0 ? "" : "pris par " + string.Join(", ", who.ConvertAll(w => "J" + (w + 1)));
            }, true, owner);
        }

        // ---------- Musique d'ambiance (provisoire) ----------

        private void StartMusic()
        {
            if (FindFirstObjectByType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();               // la scène du menu n'en avait pas
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = skin != null && skin.music != null ? skin.music : Drone();
            src.loop = true;
            src.volume = skin != null ? skin.musicVolume : 0.3f;
            src.spatialBlend = 0f;
            src.Play();
        }

        /// <summary>Nappe d'horreur synthétisée (provisoire, à remplacer par une vraie musique) : basses qui battent, souffle filtré.</summary>
        private static AudioClip Drone()
        {
            const int rate = 22050;
            const float seconds = 16f;
            int n = (int)(rate * seconds);
            var data = new float[n];
            float noise = 0f;
            var rng = new System.Random(7);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)rate;
                float lfo = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / seconds * 2f);
                float s = 0.32f * Mathf.Sin(2f * Mathf.PI * 55f * t) + 0.22f * Mathf.Sin(2f * Mathf.PI * 55.6f * t)
                          + 0.14f * Mathf.Sin(2f * Mathf.PI * 82.4f * t) * lfo + 0.07f * Mathf.Sin(2f * Mathf.PI * 164.8f * t + Mathf.Sin(t * 0.7f));
                noise = Mathf.Lerp(noise, (float)rng.NextDouble() * 2f - 1f, 0.02f);
                data[i] = (s + noise * 0.5f) * 0.5f;
            }

            // Fondu des bords : la boucle ne claque pas.
            int fade = rate / 2;
            for (int i = 0; i < fade; i++)
            {
                float k = i / (float)fade;
                data[i] *= k;
                data[n - 1 - i] *= k;
            }

            AudioClip clip = AudioClip.Create("Ambiance du menu (provisoire)", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------- Repli sans habillage (IMGUI) ----------

        private void OnGUI()
        {
            if (_ui != null || _lobby == null)
                return;
            GreyboxMenu m = _page switch { Page.Campement => _camp, Page.Parametres => _settings, Page.Playtest => _menu, _ => _title };
            m.Draw(_page == Page.Campement ? ActLabel(_act) : "IT HUNTS", _status, 560f);
        }
    }
}
