using System.Collections.Generic;
using ItHunts.Hud;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Level
{
    /// <summary>
    /// Menus d'avant-partie (lot B de la DA, UI-HANDOFF § 2 ; retours du 2026-11-18) :
    /// - écran titre : tiers gauche réservé au titre (en attendant le logo) et au menu (Jouer, Paramètres, Quitter) ; ciel, lune à
    ///   droite du titre, trois couches de nuages qui dérivent (fondu en bas), château (détourage érodé) dont les fenêtres
    ///   clignotent, brume qui défile (fondu en haut) ; parallaxe à la souris ou au stick droit ; Playtest en petit en bas à droite
    ///   (éditeur et builds de développement) ; musique d'ambiance provisoire ;
    /// - transition : la brume monte jusqu'à couvrir l'écran, puis se dissipe sur l'autre écran (durées réglables) ;
    /// - campement (CampLobby) : effet de feu sur le foyer peint (langues, braises, lueur qui vacille sur le décor) ; 4 places
    ///   ancrées autour du foyer, en perspective, ombre au sol, texte au-dessus de chaque personnage ; grand titre ACTE I / II / III
    ///   qui change avec ◄ ► ; en solo, Jouer lance directement ; en coop, Prêt (A) s'affiche sur la place du joueur ; Jouer grisé
    ///   dit pourquoi quand on essaie ; Start sur une autre manette fait rejoindre un joueur (Y : modifier) ;
    /// - Paramètres (options d'image), Playtest (scènes de test, combat de boss).
    /// Navigation : souris, clavier (flèches, Entrée, Échap), manette (croix ou stick, A, B). Aucun menu ne met en pause.
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

        /// <summary>Playtest : seulement dans l'éditeur et les builds de développement.</summary>
        public static bool PlaytestVisible => Application.isEditor || Debug.isDebugBuild;

        public enum Page : byte { Titre, Transition, Campement, Parametres, Playtest }

        // Menus (logique clavier-manette de GreyboxMenu ; affichés en uGUI). _menu : le menu Playtest (noms gardés par les tests).
        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private readonly GreyboxMenu _title = new GreyboxMenu(), _camp = new GreyboxMenu(), _settings = new GreyboxMenu();
        private CampLobby _lobby;
        private int _act;
        private string _status = "";
        private float _statusUntil;
        private Page _page = Page.Titre, _back = Page.Titre, _target = Page.Campement;
        private bool _showCamp;
        private float _transitionT;
        private bool _campCoop;
        private GreyboxMenu.Item _play;

        public RunDefinition Run => run;
        public Page Current => _page;
        public CampLobby Lobby => _lobby;
        /// <summary>Écran affiché sous la brume : faux le titre, vrai le campement.</summary>
        public bool ShowsCamp => _showCamp;
        /// <summary>Brume de la transition (0 : rien, 1 : l'écran est couvert).</summary>
        public float FogCover { get; private set; }

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
                _showCamp = true;
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
            _title.Add("Quitter", Application.Quit);
            _title.FocusFirst();

            BuildCampMenu();

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

        /// <summary>Menu du campement : en solo, pas de Prêt (Jouer lance) ; en coop, Prêt bascule. ◄ ► change l'acte partout.</summary>
        private void BuildCampMenu()
        {
            _campCoop = _lobby.Count > 1;
            _camp.Items.Clear();
            _camp.Add("Modifier le personnage", () => OpenSelect(0));
            if (_campCoop)
                _camp.Add("Prêt", () => ToggleReady(0));
            _play = _camp.Add("Jouer", Launch);
            _play.Dim = () => _lobby.Blocker(null) != null || !ActPlayable(_act);
            _camp.Add("Paramètres", () => OpenSettings(Page.Campement));
            _camp.Add("Retour", () => GoTo(Page.Titre));
            foreach (GreyboxMenu.Item it in _camp.Items)
                it.Change = ChangeAct;
            _camp.FocusFirst();
        }

        private void ChangeAct(int d) => _act = (_act + d + 3) % 3;

        public static string ActLabel(int act) => act switch { 0 => "ACTE I", 1 => "ACTE II", _ => "ACTE III" };
        private static bool ActPlayable(int act) => act == 0;

        private void OpenSettings(Page from)
        {
            _back = from;
            _page = Page.Parametres;
            _settings.FocusFirst();
        }

        private void GoTo(Page target)
        {
            _target = target;
            _transitionT = 0f;
            _page = Page.Transition;
            _status = "";
        }

        private void Say(string text, float seconds = 3f)
        {
            _status = text;
            _statusUntil = Time.unscaledTime + seconds;
        }

        private void ToggleReady(int seat)
        {
            if (!_lobby.ToggleReady(seat) && _lobby.Duplicate(seat))
                Say($"{CharacterName(_lobby[seat].Character)} pris deux fois : changez de personnage pour être prêt.");
        }

        private string CharacterName(int i) => Roster != null && Roster[i] != null ? Roster[i].displayName : "Ce personnage";

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
            string blocker = _lobby.Blocker(CharacterName);
            if (blocker != null)
            {
                Say(blocker);
                return;
            }

            if (!ActPlayable(_act))
            {
                Say($"{ActLabel(_act)} : pas encore jouable");
                return;
            }

            int easy = run != null ? run.difficulties.FindIndex(d => d.playable) : -1;
            if (easy < 0)
            {
                Say("Pas de définition de run");
                return;
            }

            ApplyLobby();
            string scene = RunSession.Begin(run, easy);
            if (!RunSession.Load(scene))
            {
                RunSession.End();
                Say($"Scène « {scene} » absente du build (lancer le constructeur du niveau 1)", 6f);
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
                Say("Pas de niveau jouable pour le combat de boss");
                return;
            }

            RunSession.BossPlaytest = true;
            if (!RunSession.Load(scene))
            {
                RunSession.BossPlaytest = false;
                RunSession.End();
                Say($"Scène « {scene} » absente du build");
            }
        }

        private void Direct(string scene)
        {
            ApplyLobby();
            RunSession.End();
            if (!RunSession.Load(scene))
                Say($"Scène « {scene} » absente du build");
        }

        private void OpenPlaytest()
        {
            if (!PlaytestVisible)
                return;
            _back = _page == Page.Campement ? Page.Campement : Page.Titre;
            _page = Page.Playtest;
            _menu.FocusFirst();
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
            if (_status.Length > 0 && Time.unscaledTime > _statusUntil)
                _status = "";
            switch (_page)
            {
                case Page.Titre:
                    _title.Update();
                    if (PlaytestVisible && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                        OpenPlaytest();
                    break;
                case Page.Transition:
                    UpdateTransition();
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

        /// <summary>La brume monte (couvre l'écran), on change d'écran, puis elle se dissipe.</summary>
        private void UpdateTransition()
        {
            float rise = skin != null ? skin.fogRiseSeconds : 0.8f, clear = skin != null ? skin.fogClearSeconds : 0.8f;
            _transitionT += Time.unscaledDeltaTime;
            if (_transitionT < rise)
                FogCover = Mathf.SmoothStep(0f, 1f, _transitionT / rise);
            else
            {
                _showCamp = _target == Page.Campement;
                FogCover = 1f - Mathf.SmoothStep(0f, 1f, (_transitionT - rise) / clear);
                if (_transitionT >= rise + clear)
                {
                    FogCover = 0f;
                    _page = _target;
                    (_page == Page.Campement ? _camp : _title).FocusFirst();
                }
            }
        }

        private void UpdateCamp(InputDevice p2)
        {
            if ((_lobby.Count > 1) != _campCoop)
                BuildCampMenu();                                         // Prêt apparaît en coop, disparaît en solo
            bool selecting = false;
            for (int seat = 0; seat < CampLobby.Seats; seat++)
                if (SelectOpen(seat))
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
                if (PlaytestVisible && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
                    OpenPlaytest();
            }

            // Écran partagé : Start sur une autre manette que celle du joueur 1 fait rejoindre (ou retirer) un joueur.
            foreach (Gamepad g in Gamepad.all)
            {
                if (!g.startButton.wasPressedThisFrame || g == GreyboxMenu.LastNavPad)
                    continue;
                if (_lobby.SeatOf(g) >= 0)
                    _lobby.Leave(g);
                else if (_lobby.Join(g) < 0)
                    Say("Plus de place en écran partagé : le jeu en ligne viendra plus tard");
            }

            // Les joueurs qui ont rejoint : leur manette seule (A : prêt, Y : modifier, B : plus prêt).
            for (int seat = 1; seat < CampLobby.Seats; seat++)
            {
                if (!(_lobby[seat]?.Device is Gamepad pad) || SelectOpen(seat))
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
            public RectTransform TitlePage, CampPage, TitleScene, CampScene, Fog;
            public UiList Title, Camp, Settings, Playtest;
            public RectTransform SettingsPanel, PlaytestPanel, Act, ActPrev, ActNext, PlaytestLink;
            public Text Status, ActText;
            public readonly List<(RectTransform rect, float parallax, float drift, float speed)> Layers = new List<(RectTransform, float, float, float)>();
            public Image Windows, FireGlow, FogSolid;
            public readonly List<(RectTransform rect, float h, float w, float seed)> Flames = new List<(RectTransform, float, float, float)>();
            public readonly List<Ember> Embers = new List<Ember>();
            public readonly List<Image> FogBands = new List<Image>();
            public readonly List<SeatUi> Seats = new List<SeatUi>();
        }

        private sealed class Ember
        {
            public RectTransform Rect;
            public Image Img;
            public float Age, Life, Sway, X;
        }

        private sealed class SeatUi
        {
            public RectTransform Root;
            public Image Figure, Shadow;
            public Text Label, State;
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
            UiKit.Stretch(UiKit.Img("Noir", root, null, Color.black).rectTransform);
            _ui.TitlePage = UiKit.Stretch(UiKit.Rect("Écran titre", root, Vector2.zero, Vector2.zero, Vector2.zero));
            _ui.CampPage = UiKit.Stretch(UiKit.Rect("Campement", root, Vector2.zero, Vector2.zero, Vector2.zero));
            _ui.TitleScene = SceneRect(_ui.TitlePage);
            _ui.CampScene = SceneRect(_ui.CampPage);
            BuildTitleBackdrop();
            BuildTitleUi();
            BuildCamp();
            BuildTransition(root);
            _ui.SettingsPanel = CenterPanel(root, "Paramètres", _settings, out _ui.Settings);
            _ui.PlaytestPanel = CenterPanel(root, "Playtest", _menu, out _ui.Playtest);
            _ui.Status = UiKit.Txt("État", root, skin.textBold != null ? skin.textBold : skin.textFont, 30, UiKit.Gold);
            _ui.Status.rectTransform.anchorMin = new Vector2(0f, 0f);
            _ui.Status.rectTransform.anchorMax = new Vector2(1f, 0f);
            _ui.Status.rectTransform.anchoredPosition = new Vector2(0f, 60f);
            _ui.Status.rectTransform.sizeDelta = new Vector2(-200f, 44f);
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

        /// <summary>Rectangle posé dans la toile (px de la toile, origine en haut à gauche).</summary>
        private static void Place(RectTransform r, float x, float yTop, float w, float h)
        {
            r.anchorMin = new Vector2(x / CanvasW, 1f - (yTop + h) / CanvasH);
            r.anchorMax = new Vector2((x + w) / CanvasW, 1f - yTop / CanvasH);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private Image Layer(string name, RectTransform scene, Sprite sprite, float x, float yTop, float w, float h, Material material = null,
            float parallax = 0f, float drift = 0f, float speed = 0f)
        {
            Image img = UiKit.Img(name, scene, sprite, Color.white, Image.Type.Simple, material);
            Place(img.rectTransform, x, yTop, w, h);
            _ui.Layers.Add((img.rectTransform, parallax, drift, speed));
            return img;
        }

        /// <summary>Copie du matériau de fondu (ItHunts/UIFondu) : fondu haut et bas, érosion, additif.</summary>
        private Material Fade(float top, float bottom, float erode, bool additive)
        {
            if (skin.fade == null)
                return additive ? skin.additive : null;
            var m = new Material(skin.fade) { name = skin.fade.name + " (calque)" };
            m.SetFloat("_FadeTop", top);
            m.SetFloat("_FadeBottom", bottom);
            m.SetFloat("_Erode", erode);
            m.SetFloat("_Additive", additive ? 1f : 0f);
            m.SetFloat("_SrcBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            return m;
        }

        private static float Px(Sprite s, bool width) => s == null ? 0f : width ? s.rect.width : s.rect.height;

        private void BuildTitleBackdrop()
        {
            RectTransform sc = _ui.TitleScene;
            Layer("Ciel", sc, skin.sky, -60f, -40f, CanvasW + 120f, CanvasH + 80f, parallax: skin.skyParallax);
            if (skin.moon != null)
            {
                float d = Px(skin.moon, true) * skin.moonScale;
                float cx = skin.moonAt.x * CanvasW, cy = (1f - skin.moonAt.y) * CanvasH;
                if (skin.moonHaloOn && skin.moonHalo != null)
                    Layer("Lune : halo", sc, skin.moonHalo, cx - d / 2f, cy - d / 2f, d, d, skin.additive, skin.moonParallax);
                Layer("Lune", sc, skin.moon, cx - d / 2f, cy - d / 2f, d, d, null, skin.moonParallax);
            }

            // Nuages : peu de couches, espacements irréguliers, opacité et vitesse par couche ; bas de chaque bande fondu (trait coupé).
            foreach (MenuSkinSettings.CloudLayer c in skin.cloudLayers)
            {
                Sprite s = c.band >= 0 && c.band < skin.clouds.Length ? skin.clouds[c.band] : null;
                if (s == null)
                    continue;
                float w = CanvasW * c.scale, h = Px(s, false) * c.scale;
                Layer($"Nuages {c.band + 1}", sc, s, c.x, c.yTop, w, h, Fade(0.08f, skin.cloudFadeBottom, 0f, false), c.parallax, c.drift, c.speed).color =
                    new Color(1f, 1f, 1f, c.opacity);
            }

            if (skin.castle != null)
            {
                float w = Px(skin.castle, true), h = Px(skin.castle, false);
                Layer("Château", sc, skin.castle, skin.castleAt.x, skin.castleAt.y, w, h, Fade(0f, 0f, skin.castleErode, false), skin.castleParallax);
                if (skin.castleWindows != null)
                    _ui.Windows = Layer("Château : fenêtres", sc, skin.castleWindows, skin.castleAt.x, skin.castleAt.y, w, h, Fade(0f, 0f, skin.castleErode, true), skin.castleParallax);
            }

            // Brume : fondu en haut d'au moins 150 px à l'écran (pas de ligne droite avec les nuages) ; deux copies qui défilent.
            float[] fogY = { CanvasH - 760f, CanvasH - 600f };
            for (int i = 0; i < skin.fog.Length; i++)
            {
                Sprite s = skin.fog[i];
                if (s == null)
                    continue;
                float w = Px(s, true), h = Px(s, false) + 140f;
                for (int c = 0; c < 2; c++)
                    Layer($"Brume {i + 1} ({c + 1})", sc, s, c * w, fogY[Mathf.Min(i, 1)], w, h, Fade(Mathf.Clamp01(skin.fogFadeTop / h), 0f, 0f, true),
                        i == 0 ? skin.farFogParallax : skin.nearFogParallax, 0f, skin.fogSpeed * (i == 0 ? 0.6f : 1f)).color = new Color(1f, 1f, 1f, 0.8f);
            }
        }

        private Text TitleText(string name, Transform parent, string text, int size, TextAnchor align)
        {
            bool gothic = skin.titleStyle == MenuSkinSettings.TitleStyle.GothiqueContour;
            Font f = gothic ? skin.titleFont : skin.titleBold != null ? skin.titleBold : skin.textBold;
            Text t = UiKit.Txt(name, parent, f, gothic ? size : Mathf.RoundToInt(size * 0.6f), new Color(0.94f, 0.9f, 0.9f), align);
            t.text = gothic ? text : Spaced(text);
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.02f, 0.05f, 0.95f);
            outline.effectDistance = gothic ? new Vector2(4f, -4f) : new Vector2(3f, -3f);
            return t;
        }

        /// <summary>Capitales espacées (EB Garamond Bold) : une espace fine entre les lettres, une cadratin entre les mots.</summary>
        public static string Spaced(string s)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == ' ')
                    sb.Append("  ");
                else
                {
                    sb.Append(char.ToUpperInvariant(s[i]));
                    if (i + 1 < s.Length && s[i + 1] != ' ')
                        sb.Append(' ');
                }
            }

            return sb.ToString();
        }

        /// <summary>Capitales espacées (EB Garamond Bold) : une lettre par texte, espacement en part de la taille.</summary>
        private void SpacedTitle(RectTransform col, string text, int size, float tracking)
        {
            RectTransform row = UiKit.Rect("Titre (en attendant le logo)", col, new Vector2(0f, 1f), Vector2.zero, new Vector2(10f, size * 1.4f), new Vector2(0f, 1f));
            float x = 0f;
            foreach (char ch in text)
            {
                if (ch == ' ')
                {
                    x += size * 0.45f;
                    continue;
                }

                Text t = UiKit.Txt($"Lettre {ch}", row, skin.titleBold != null ? skin.titleBold : skin.textBold, size, new Color(0.94f, 0.9f, 0.9f), TextAnchor.UpperLeft);
                t.text = ch.ToString();
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                var o = t.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.02f, 0.02f, 0.05f, 0.95f);
                o.effectDistance = new Vector2(3f, -3f);
                t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0f, 1f);
                t.rectTransform.pivot = new Vector2(0f, 1f);
                t.rectTransform.anchoredPosition = new Vector2(x, 0f);
                t.rectTransform.sizeDelta = new Vector2(size, size * 1.4f);
                x += t.preferredWidth + size * tracking;
            }
        }

        private void BuildTitleUi()
        {
            // Tiers gauche : titre et menu ; la lune et le château restent à droite.
            RectTransform col = UiKit.Rect("Colonne", _ui.TitlePage, Vector2.zero, Vector2.zero, Vector2.zero);
            col.anchorMin = new Vector2(0f, 0f);
            col.anchorMax = new Vector2(skin.leftColumn, 1f);
            col.offsetMin = new Vector2(110f, 80f);
            col.offsetMax = new Vector2(0f, -150f);
            if (skin.titleStyle == MenuSkinSettings.TitleStyle.GaramondEspace)
                SpacedTitle(col, "IT HUNTS", Mathf.RoundToInt(skin.titleSize * 0.62f), 0.16f);
            else
            {
                Text logo = TitleText("Titre (en attendant le logo)", col, "IT HUNTS", skin.titleSize, TextAnchor.UpperLeft);
                logo.rectTransform.anchorMin = new Vector2(0f, 1f);
                logo.rectTransform.anchorMax = new Vector2(1f, 1f);
                logo.rectTransform.pivot = new Vector2(0f, 1f);
                logo.rectTransform.sizeDelta = new Vector2(400f, skin.titleSize * 1.25f);
                logo.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            RectTransform list = UiKit.Rect("Boutons", col, new Vector2(0f, 1f), new Vector2(0f, -skin.titleSize * 1.5f - 40f), new Vector2(480f, 260f), new Vector2(0f, 1f));
            _ui.Title = new UiList(_title, list, skin, 70f, 40, TextAnchor.MiddleLeft);

            // Playtest : petit texte en bas à droite, éditeur et builds de développement seulement (F2 au clavier).
            _ui.PlaytestLink = UiKit.Rect("Playtest (développement)", _ui.TitlePage, new Vector2(1f, 0f), new Vector2(-40f, 30f), new Vector2(260f, 40f), new Vector2(1f, 0f));
            Text link = UiKit.Txt("Texte", _ui.PlaytestLink, skin.textFont, 22, UiKit.Dim, TextAnchor.MiddleRight);
            UiKit.Stretch(link.rectTransform);
            link.text = "Playtest (F2)";
            _ui.PlaytestLink.gameObject.SetActive(PlaytestVisible);
        }

        private void BuildCamp()
        {
            RectTransform sc = _ui.CampScene;
            Layer("Fond du campement", sc, skin.campBackground, 0f, 0f, CanvasW, CanvasH);
            float hx = skin.hearthAt.x * CanvasW, hy = (1f - skin.hearthAt.y) * CanvasH;
            // Lueur qui vacille sur le décor, puis les langues de flamme sur le foyer peint, puis les braises.
            float g = skin.fireGlow;
            _ui.FireGlow = UiKit.Img("Feu : lueur sur le décor", sc, UiKit.Glow(), skin.fireColor, Image.Type.Simple, skin.additive);
            Place(_ui.FireGlow.rectTransform, hx - g / 2f, hy - g * 0.62f, g, g * 0.85f);
            for (int i = 0; i < skin.flameTongues; i++)
            {
                float k = skin.flameTongues == 1 ? 0.5f : i / (skin.flameTongues - 1f);
                float w = skin.flameWidth * Mathf.Lerp(0.45f, 0.75f, 1f - Mathf.Abs(k - 0.5f) * 2f);
                float h = skin.flameHeight * Mathf.Lerp(0.55f, 1f, 1f - Mathf.Abs(k - 0.5f) * 2f);
                float x = hx + (k - 0.5f) * skin.flameWidth * 0.75f - w / 2f;
                Image f = UiKit.Img($"Feu : langue {i + 1}", sc, UiKit.Flame(), skin.fireColor, Image.Type.Simple, skin.additive);
                Place(f.rectTransform, x, hy - h + 10f, w, h);
                f.rectTransform.pivot = new Vector2(0.5f, 0f);
                _ui.Flames.Add((f.rectTransform, h, w, i * 1.7f + 0.3f));
            }

            for (int i = 0; i < skin.embers; i++)
            {
                Image e = UiKit.Img($"Braise {i + 1}", sc, UiKit.Glow(), skin.fireColor, Image.Type.Simple, skin.additive);
                e.rectTransform.anchorMin = e.rectTransform.anchorMax = new Vector2(skin.hearthAt.x, skin.hearthAt.y);
                e.rectTransform.sizeDelta = Vector2.one * Random.Range(8f, 16f);
                _ui.Embers.Add(new Ember { Rect = e.rectTransform, Img = e, Age = Random.Range(0f, 3f), Life = Random.Range(skin.emberLife.x, skin.emberLife.y),
                    Sway = Random.Range(0f, 6.28f), X = Random.Range(-skin.flameWidth * 0.4f, skin.flameWidth * 0.4f) });
            }

            // Places : points d'ancrage autour du foyer, en perspective (le fond plus petit), ombre au sol, texte au-dessus du personnage.
            // Ordre de dessin : les places du fond d'abord (le personnage passe devant ce qui est derrière lui).
            var order = new List<int> { 0, 1, 2, 3 };
            order.Sort((a, b) => Anchor(b).y.CompareTo(Anchor(a).y));
            var seats = new SeatUi[CampLobby.Seats];
            foreach (int i in order)
            {
                Vector2 at = Anchor(i);
                float k = i < skin.seatScales.Length ? skin.seatScales[i] : 1f;
                float h = skin.figureHeight * k, w = h * 0.68f;
                var s = new SeatUi { Root = UiKit.Rect($"Place {i + 1}", sc, at, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f)) };
                s.Shadow = UiKit.Img("Ombre au sol", s.Root, UiKit.Glow(), new Color(0f, 0f, 0f, 0.7f));
                s.Shadow.rectTransform.anchoredPosition = new Vector2(0f, 4f);
                s.Shadow.rectTransform.sizeDelta = new Vector2(w * 1.25f, h * 0.16f);
                s.Figure = UiKit.Img("Personnage (silhouette provisoire)", s.Root, UiKit.Silhouette(), Color.white);
                s.Figure.preserveAspect = true;
                s.Figure.rectTransform.pivot = new Vector2(0.5f, 0f);
                s.Figure.rectTransform.anchoredPosition = Vector2.zero;
                s.Figure.rectTransform.sizeDelta = new Vector2(w, h);
                s.Label = TitleText("Joueur", s.Root, "", Mathf.RoundToInt(46 * Mathf.Lerp(0.8f, 1f, k)), TextAnchor.LowerCenter);
                s.Label.rectTransform.pivot = new Vector2(0.5f, 0f);
                s.Label.rectTransform.anchoredPosition = new Vector2(0f, h + 50f * k);
                s.Label.rectTransform.sizeDelta = new Vector2(520f, 70f);
                s.State = UiKit.Txt("État", s.Root, skin.textBold != null ? skin.textBold : skin.textFont, Mathf.RoundToInt(28 * Mathf.Lerp(0.85f, 1f, k)), UiKit.Gold);
                s.State.rectTransform.pivot = new Vector2(0.5f, 0f);
                s.State.rectTransform.anchoredPosition = new Vector2(0f, h + 14f * k);
                s.State.rectTransform.sizeDelta = new Vector2(520f, 40f);
                seats[i] = s;
            }

            _ui.Seats.AddRange(seats);

            // Grand titre de l'acte (◄ ► le change) ; le menu du campement en haut à droite.
            _ui.Act = UiKit.Rect("Acte", _ui.CampPage, new Vector2(0f, 1f), new Vector2(90f, -50f), new Vector2(760f, 130f), new Vector2(0f, 1f));
            _ui.ActPrev = ActArrow("◄", 0f);
            _ui.ActText = TitleText("Titre de l'acte", _ui.Act, ActLabel(0), 104, TextAnchor.MiddleLeft);
            _ui.ActText.rectTransform.anchorMin = new Vector2(0f, 0f);
            _ui.ActText.rectTransform.anchorMax = new Vector2(0f, 1f);
            _ui.ActText.rectTransform.pivot = new Vector2(0f, 0.5f);
            _ui.ActText.rectTransform.anchoredPosition = new Vector2(70f, 0f);
            _ui.ActText.rectTransform.sizeDelta = new Vector2(520f, 0f);
            _ui.ActNext = ActArrow("►", 600f);
            RectTransform panel = UiKit.Rect("Menu du campement", _ui.CampPage, new Vector2(1f, 1f), new Vector2(-60f, -50f), new Vector2(560f, 5 * 54f + 60f), new Vector2(1f, 1f));
            UiKit.Panel(panel, skin, 0.7f);
            RectTransform list = UiKit.Stretch(UiKit.Rect("Boutons", panel, Vector2.zero, Vector2.zero, Vector2.zero), 30f);
            _ui.Camp = new UiList(_camp, list, skin, 54f, 30, TextAnchor.MiddleLeft);
        }

        private RectTransform ActArrow(string glyph, float x)
        {
            RectTransform r = UiKit.Rect("Acte " + glyph, _ui.Act, new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(64f, 100f), new Vector2(0f, 0.5f));
            Text t = UiKit.Txt("Flèche", r, skin.textBold, 54, UiKit.Gold);
            UiKit.Stretch(t.rectTransform);
            t.text = glyph;
            return r;
        }

        private Vector2 Anchor(int i) => i < skin.seatAnchors.Length ? skin.seatAnchors[i] : new Vector2(0.5f, 0.1f);

        /// <summary>Transition : un voile de brume qui monte depuis le bas (dégradé) et des bandes de brume additives qui le suivent.</summary>
        private void BuildTransition(RectTransform root)
        {
            _ui.Fog = UiKit.Stretch(UiKit.Rect("Transition : brume", root, Vector2.zero, Vector2.zero, Vector2.zero));
            _ui.FogSolid = UiKit.Img("Voile", _ui.Fog, UiKit.Gradient(), skin.transitionFog);
            _ui.FogSolid.rectTransform.anchorMin = new Vector2(0f, 0f);
            _ui.FogSolid.rectTransform.anchorMax = new Vector2(1f, 0f);
            _ui.FogSolid.rectTransform.pivot = new Vector2(0.5f, 0f);
            for (int i = 0; i < skin.fog.Length; i++)
            {
                if (skin.fog[i] == null)
                    continue;
                Image b = UiKit.Img($"Bande {i + 1}", _ui.Fog, skin.fog[i], Color.white, Image.Type.Simple, Fade(0.45f, 0f, 0f, true));
                b.rectTransform.anchorMin = new Vector2(-0.1f, 0f);
                b.rectTransform.anchorMax = new Vector2(1.1f, 0f);
                b.rectTransform.pivot = new Vector2(0.5f, 0f);
                _ui.FogBands.Add(b);
            }

            _ui.Fog.gameObject.SetActive(false);
        }

        private RectTransform CenterPanel(RectTransform root, string title, GreyboxMenu menu, out UiList list)
        {
            RectTransform shade = UiKit.Stretch(UiKit.Rect(title, root, Vector2.zero, Vector2.zero, Vector2.zero));
            UiKit.Stretch(UiKit.Img("Voile", shade, null, UiKit.Shade).rectTransform);
            RectTransform panel = UiKit.Rect("Panneau", shade, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, menu.Items.Count * 60f + 190f));
            UiKit.Panel(panel, skin);
            Text t = TitleText("Titre", panel, title, 62, TextAnchor.MiddleCenter);
            t.rectTransform.anchorMin = new Vector2(0f, 1f);
            t.rectTransform.anchorMax = new Vector2(1f, 1f);
            t.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            t.rectTransform.sizeDelta = new Vector2(0f, 80f);
            RectTransform l = UiKit.Rect("Boutons", panel, new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(720f, menu.Items.Count * 60f), new Vector2(0.5f, 1f));
            list = new UiList(menu, l, skin, 60f, 30);
            shade.gameObject.SetActive(false);
            return shade;
        }

        private Vector2 _pointer;

        private void SyncUi()
        {
            float t = Time.unscaledTime, dt = Time.unscaledDeltaTime;
            // Un seul écran actif (pas de page voisine qui dépasse d'un pixel sur le bord).
            _ui.TitlePage.gameObject.SetActive(!_showCamp);
            _ui.CampPage.gameObject.SetActive(_showCamp);

            // Parallaxe : la souris (ou le stick droit) décale les calques selon leur profondeur ; les nuages dérivent.
            Vector2 target = Vector2.zero;
            if (Mouse.current != null && Screen.width > 0)
                target = new Vector2(Mouse.current.position.ReadValue().x / Screen.width * 2f - 1f, Mouse.current.position.ReadValue().y / Screen.height * 2f - 1f);
            if (Gamepad.current != null && Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.05f)
                target = Gamepad.current.rightStick.ReadValue();
            _pointer = Vector2.Lerp(_pointer, Vector2.ClampMagnitude(target, 1f), 1f - Mathf.Exp(-3f * dt));
            float sceneW = _ui.TitleScene.rect.width;
            foreach ((RectTransform r, float parallax, float drift, float speed) in _ui.Layers)
            {
                Vector2 off = -_pointer * skin.parallaxPixels * parallax;
                if (drift > 0f)
                    off.x += Mathf.Sin(t * speed * 0.01f + parallax * 17f) * drift;           // nuages : aller-retour lent
                else if (speed != 0f)
                {
                    float bandW = (r.anchorMax.x - r.anchorMin.x) * sceneW;
                    off.x += Mathf.Repeat(t * speed * sceneW / CanvasW, Mathf.Max(1f, bandW)) - bandW;   // brume : défilement sans couture
                }

                r.anchoredPosition = off;
            }

            if (_ui.Windows != null)
                _ui.Windows.color = skin.windowColor * (1f - skin.windowFlickerDepth + skin.windowFlickerDepth * Mathf.PerlinNoise(t * skin.windowFlickerSpeed, 0.37f));
            if (_showCamp)
                SyncFire(t, dt);

            _ui.Title.Root.parent.gameObject.SetActive(!_showCamp);
            if (_page == Page.Titre)
                _ui.Title.Sync();
            if (_ui.PlaytestLink.gameObject.activeInHierarchy && UiKit.Hover(_ui.PlaytestLink) && UiKit.Clicked && _page == Page.Titre)
                OpenPlaytest();
            if (_page == Page.Campement && !SelectOpen(0))
            {
                _ui.Camp.Sync();
                if (UiKit.Clicked && UiKit.Hover(_ui.ActPrev))
                    ChangeAct(-1);
                if (UiKit.Clicked && UiKit.Hover(_ui.ActNext))
                    ChangeAct(1);
            }

            _ui.SettingsPanel.gameObject.SetActive(_page == Page.Parametres);
            if (_page == Page.Parametres)
                _ui.Settings.Sync();
            _ui.PlaytestPanel.gameObject.SetActive(_page == Page.Playtest);
            if (_page == Page.Playtest)
                _ui.Playtest.Sync();
            _ui.ActText.text = ActLabel(_act);
            _ui.ActNext.anchoredPosition = new Vector2(70f + _ui.ActText.preferredWidth + 18f, 0f);   // ► juste après le titre
            _ui.ActText.color = ActPlayable(_act) ? new Color(0.94f, 0.9f, 0.9f) : UiKit.Dim;
            SyncSeats();
            PlaceSelectPanels();
            SyncTransition(t);
            _ui.Status.text = _status;
        }

        private void SyncFire(float t, float dt)
        {
            float n = Mathf.PerlinNoise(t * skin.fireFlickerSpeed, 3.1f);
            _ui.FireGlow.color = skin.fireColor * (0.45f * (1f - skin.fireFlickerDepth + skin.fireFlickerDepth * 2f * n));
            foreach ((RectTransform r, float h, float w, float seed) in _ui.Flames)
            {
                float f = Mathf.PerlinNoise(t * skin.fireFlickerSpeed * 1.3f, seed);
                r.localScale = new Vector3(1f + 0.12f * (Mathf.PerlinNoise(seed, t * 3f) - 0.5f), 0.75f + 0.5f * f, 1f);
                r.localEulerAngles = new Vector3(0f, 0f, 9f * (Mathf.PerlinNoise(t * 2.2f, seed + 4f) - 0.5f));
            }

            float sceneH = _ui.CampScene.rect.height, k = sceneH / CanvasH;
            foreach (Ember e in _ui.Embers)
            {
                e.Age += dt;
                if (e.Age > e.Life)
                {
                    e.Age = 0f;
                    e.Life = Random.Range(skin.emberLife.x, skin.emberLife.y);
                    e.X = Random.Range(-skin.flameWidth * 0.4f, skin.flameWidth * 0.4f);
                }

                float u = e.Age / e.Life;
                e.Rect.anchoredPosition = new Vector2((e.X + Mathf.Sin(e.Age * 2.3f + e.Sway) * 30f * u) * k, (20f + e.Age * skin.emberSpeed) * k);
                e.Img.color = skin.fireColor * (Mathf.Sin(u * Mathf.PI) * 0.9f);
            }
        }

        private void SyncTransition(float t)
        {
            bool on = FogCover > 0.001f;
            if (_ui.Fog.gameObject.activeSelf != on)
                _ui.Fog.gameObject.SetActive(on);
            if (!on)
                return;
            float h = ((RectTransform)_ui.Canvas.transform).rect.height;
            // Le voile (dégradé : opaque en bas, transparent en haut) monte ; couvert, il remplit l'écran.
            _ui.FogSolid.rectTransform.sizeDelta = new Vector2(0f, h * 2f);
            _ui.FogSolid.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-h * 2f, 0f, FogCover));
            for (int i = 0; i < _ui.FogBands.Count; i++)
            {
                Image b = _ui.FogBands[i];
                b.rectTransform.sizeDelta = new Vector2(0f, h * (0.7f + 0.2f * i));
                b.rectTransform.anchoredPosition = new Vector2(Mathf.Sin(t * 0.4f + i) * 40f, Mathf.Lerp(-h * 0.8f, h * (0.15f + 0.25f * i), FogCover));
                b.color = Color.white * (0.85f * FogCover);
            }
        }

        private bool SelectOpen(int seat) => _select != null && _select[seat] != null && _select[seat].IsOpen;

        private void SyncSeats()
        {
            CharacterRoster r = Roster;
            for (int i = 0; i < _ui.Seats.Count; i++)
            {
                SeatUi ui = _ui.Seats[i];
                CampLobby.Seat s = _lobby[i];
                CharacterDefinition c = s != null && r != null ? r[s.Character] : null;
                bool online = i >= _lobby.MaxLocal;
                ui.Root.gameObject.SetActive(!online || skin.showOnlineSeats);
                ui.Figure.enabled = ui.Shadow.enabled = c != null;
                if (c != null)
                    ui.Figure.color = Color.Lerp(c.color, Color.black, 0.35f);
                bool coop = _lobby.Count > 1;
                if (s != null)
                {
                    ui.Label.text = $"J{i + 1}" + (c != null ? $" · {c.displayName}" : "");
                    ui.Label.color = new Color(0.94f, 0.9f, 0.9f);
                    bool dup = _lobby.Duplicate(i);
                    ui.State.text = dup ? $"{(c != null ? c.displayName : "Personnage")} pris deux fois" : coop && s.Ready ? "Prêt" : coop ? (i == 0 ? "" : "A : prêt · Y : modifier") : "";
                    ui.State.color = dup ? new Color(1f, 0.45f, 0.35f) : s.Ready ? UiKit.Gold : UiKit.Dim;
                }
                else
                {
                    ui.Label.text = "";
                    ui.State.text = online ? "En ligne, plus tard" : "Start pour rejoindre";
                    ui.State.color = new Color(UiKit.Dim.r, UiKit.Dim.g, UiKit.Dim.b, online ? 0.55f : 0.9f);
                }
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
            if (PlaytestVisible && _page == Page.Titre && GUI.Button(new Rect(Screen.width - 180f, Screen.height - 50f, 160f, 34f), "Playtest (F2)"))
                OpenPlaytest();
        }
    }
}
