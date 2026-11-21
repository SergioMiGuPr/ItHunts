using System.Collections.Generic;
using ItHunts.Hud;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Level
{
    /// <summary>
    /// Menus d'avant-partie : la logique. L'affichage est celui des prefabs de la DA (PR #18, UI-HANDOFF § 2 : maquettes de
    /// l'utilisateur au pixel), posé par l'assembly ItHunts.Menus (MainMenuArt) qui lit cet état ; sans lui, un repli greybox.
    /// - Écran titre : Jouer, Paramètres, Quitter ; Vitrine (test, F2) et Playtest (F3) en petit en bas à droite (éditeur et
    ///   builds de développement).
    /// - Transition : la brume monte jusqu'à couvrir l'écran, puis se dissipe sur l'autre écran (durées réglables).
    /// - Campement (CampLobby) : J1 = dernier personnage joué ; un joueur rejoint avec Start sur une autre manette (place 2 ;
    ///   places 3 et 4 : en ligne, plus tard) ; ACTE I / II / III (◄ ►) ; Prêt : en solo, lance la partie ; en coop, bascule
    ///   le joueur prêt, la partie part quand tout le monde l'est ; Prêt est grisé si un personnage est pris deux fois.
    /// - Choix du personnage (CampCharacterPick) : un écran plein (celui de la DA), pour J1 ou pour le joueur qui a rejoint.
    /// - Paramètres (GameSettings) : volumes, sensibilité, plein écran, réduire les animations.
    /// - Audio : musique en boucle du titre au campement sans coupure ; crépitement du feu en boucle au campement.
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
        [Tooltip("Modules du niveau aléatoire (menu Playtest : salle de test d'un module).")]
        [SerializeField] private ModuleCatalog moduleCatalog;

        /// <summary>Scène vitrine (retours du lot, point 9) : assets de la DA, ennemis dans chaque état, vue FPS.</summary>
        public const string VitrineScene = "Test_Vitrine";
        public const string VitrineLinkLabel = "Vitrine (test) (F2)", PlaytestLinkLabel = "Playtest (F3)";

        /// <summary>Le prochain chargement du menu ouvre directement le campement (Retour au campement depuis la pause).</summary>
        public static bool StartAtCamp { get; set; }

        /// <summary>Playtest : seulement dans l'éditeur et les builds de développement.</summary>
        public static bool PlaytestVisible => Application.isEditor || Debug.isDebugBuild;

        /// <summary>La vue de la DA (assembly ItHunts.Menus) s'abonne ici : elle se pose sur chaque menu principal.</summary>
        public static event System.Action<MainMenu> Created;

        public enum Page : byte { Titre, Transition, Campement, Parametres, Playtest }

        // Menus (logique clavier-manette de GreyboxMenu). _menu : le menu Playtest (noms gardés par les tests).
        private readonly GreyboxMenu _menu = new GreyboxMenu();
        private readonly GreyboxMenu _title = new GreyboxMenu(), _camp = new GreyboxMenu(), _settings = new GreyboxMenu();
        private CampLobby _lobby;
        private CampCharacterPick _pick;
        private int _act;
        private string _status = "";
        private float _statusUntil;
        private Page _page = Page.Titre, _back = Page.Titre, _target = Page.Campement;
        private bool _showCamp;
        private float _transitionT;
        private bool _campCoop;
        private bool _launching;

        public RunDefinition Run => run;
        public MenuSkinSettings Skin => skin;
        public Page Current => _page;
        /// <summary>Page sous la transition et sous Paramètres (titre ou campement).</summary>
        public Page Back => _back;
        public CampLobby Lobby => _lobby;
        public CharacterRoster Roster => run != null ? run.characters : null;
        /// <summary>Écran affiché sous la brume : faux le titre, vrai le campement.</summary>
        public bool ShowsCamp => _showCamp;
        /// <summary>Brume de la transition (0 : rien, 1 : l'écran est couvert).</summary>
        public float FogCover { get; private set; }
        public int Act => _act;
        public string Status => _status;
        public GreyboxMenu TitleMenu => _title;
        public GreyboxMenu CampMenu => _camp;
        public GreyboxMenu SettingsMenu => _settings;
        public GreyboxMenu PlaytestMenu => _menu;
        /// <summary>Choix du personnage ouvert (null : aucun).</summary>
        public CampCharacterPick Pick => _pick != null && _pick.IsOpen ? _pick : null;
        /// <summary>Entrées du campement, une par bouton de la maquette.</summary>
        public GreyboxMenu.Item CampModify { get; private set; }
        public GreyboxMenu.Item CampReady { get; private set; }
        public GreyboxMenu.Item CampSettings { get; private set; }
        public GreyboxMenu.Item CampFriends { get; private set; }
        public GreyboxMenu.Item CampBack { get; private set; }
        /// <summary>Habillé par la DA : plus de repli greybox (sauf la page Playtest, de développement).</summary>
        public bool Skinned { get; set; }

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ItHunts.Player.PlayerLook.MenuOpen = false;
            RunSession.End();
            RunSession.CoopDevice = null;
            RandomLevel.SeedOverride = 0;                                     // seed et salle de test : seulement pour le lancement choisi
            RandomLevel.TestModule = null;
            _playSeed = Random.Range(1, 100000);
            GameSettings.Apply();
            Build();
            if (StartAtCamp)
            {
                StartAtCamp = false;
                _page = Page.Campement;
                _showCamp = true;
                FocusCamp();
            }

            StartAudio();
            Created?.Invoke(this);
        }

        // ---------- Menus ----------

        /// <summary>Menus et salon (public pour les tests).</summary>
        public void Build()
        {
            CharacterRoster r = Roster;
            int first = r != null ? r.defaultIndex : 0;
            string last = CampLobby.LoadLast();
            if (r != null && !string.IsNullOrEmpty(last))
                for (int i = 0; i < r.Count; i++)
                    if (r[i] != null && r[i].id == last && r[i].Selectable)
                        first = i;
            _lobby = new CampLobby(r != null ? r.Selectable() : new bool[0], first);
            _pick = new CampCharacterPick(r);

            _title.Items.Clear();
            _title.Add("Jouer", () => GoTo(Page.Campement));
            _title.Add("Paramètres", () => OpenSettings(Page.Titre));
            _title.Add("Quitter", Application.Quit);
            _title.FocusFirst();

            BuildCampMenu();

            _settings.Items.Clear();
            GameSettings.Fill(_settings, OpenKeys);
            _settings.Add("Retour", () => _page = _back);
            _settings.FocusFirst();

            _menu.Items.Clear();
            _menu.Add("Lancer la run (niveau 1)", Launch);
            // Niveau aléatoire d'une seed choisie (◄ ►), et salle de test de chaque module avec le traqueur (point 2 du 2026-11-21).
            GreyboxMenu.Item seedItem = _menu.Add(SeedLabel, () => LaunchRandom(_playSeed, null));
            seedItem.Value = () => _playSeed.ToString();
            seedItem.Change = d => _playSeed = Mathf.Max(1, _playSeed + d);
            GreyboxMenu.Item moduleItem = _menu.Add(ModuleTestLabel, () => LaunchRandom(_playSeed, TestModuleName));
            moduleItem.Value = () => TestModuleName ?? "aucun module";
            moduleItem.Change = d => _testModule = TestModules.Count == 0 ? 0 : ((_testModule + d) % TestModules.Count + TestModules.Count) % TestModules.Count;
            _menu.Add("Playtest : combat de boss", LaunchBossFight);
            _menu.Add("Playtest : vitrine", () => Direct(VitrineScene));
            _menu.Add("Test_Movement (banc d'essai)", () => Direct(movementScene));
            _menu.Add("Test_Arena (labyrinthe)", () => Direct(arenaScene));
            _menu.Add("Level_01 (carte fixe de référence)", () => Direct(fixedLevelScene));
            _menu.Add("Retour", () => _page = _back);
            _menu.FocusFirst();
        }

        /// <summary>
        /// Boutons du campement (maquette) : Modifier, Prêt, Paramètres, Amis, Retour ; ◄ ► change l'acte depuis n'importe lequel.
        /// Prêt : en solo, lance la partie ; en coop, bascule J1 prêt (la partie part quand tout le monde l'est).
        /// </summary>
        private void BuildCampMenu()
        {
            _campCoop = _lobby.Count > 1;
            _camp.Items.Clear();
            CampModify = _camp.Add("Modifier le personnage", () => OpenSelect(0));
            CampReady = _camp.Add(_campCoop ? "Prêt" : "Jouer", PressReady);
            CampReady.Dim = () => ReadyBlocked;
            CampSettings = _camp.Add("Paramètres", () => OpenSettings(Page.Campement));
            CampFriends = _camp.Add("Amis", () => Say("Amis et jeu en ligne : plus tard"));
            CampBack = _camp.Add("Retour", () => GoTo(Page.Titre));
            foreach (GreyboxMenu.Item it in _camp.Items)
                it.Change = ChangeAct;
            FocusCamp();
        }

        /// <summary>Le focus du campement part sur Prêt (Entrée lance la partie en solo).</summary>
        private void FocusCamp() => _camp.SetFocus(_camp.Items.IndexOf(CampReady));

        /// <summary>Prêt grisé : un personnage pris deux fois, ou un acte pas encore jouable.</summary>
        public bool ReadyBlocked => AnyDuplicate || !ActPlayable(_act);

        private bool AnyDuplicate
        {
            get
            {
                for (int i = 0; i < CampLobby.Seats; i++)
                    if (_lobby[i] != null && _lobby.Duplicate(i))
                        return true;
                return false;
            }
        }

        private void PressReady()
        {
            if (!ActPlayable(_act))
            {
                Say($"{ActLabel(_act)} : pas encore jouable");
                return;
            }

            if (_lobby.Count <= 1)
                Launch();
            else
                ToggleReady(0);
        }

        public void ChangeAct(int d) => _act = (_act + d + 3) % 3;

        public static string ActLabel(int act) => act switch { 0 => "ACTE I", 1 => "ACTE II", _ => "ACTE III" };
        public static bool ActPlayable(int act) => act == 0;

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

        public void Say(string text, float seconds = 3f)
        {
            _status = text;
            _statusUntil = Time.unscaledTime + seconds;
        }

        private void ToggleReady(int seat)
        {
            if (!_lobby.ToggleReady(seat) && _lobby.Duplicate(seat))
                Say($"{CharacterName(_lobby[seat].Character)} pris deux fois : changez de personnage pour être prêt.");
        }

        public string CharacterName(int i) => Roster != null && Roster[i] != null ? Roster[i].displayName : "Ce personnage";

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
            _launching = true;
            if (!RunSession.Load(scene))
            {
                _launching = false;
                RunSession.End();
                Say($"Scène « {scene} » absente du build (lancer le constructeur du niveau 1)", 6f);
            }
        }

        public const string SeedLabel = "Niveau aléatoire (seed)", ModuleTestLabel = "Salle de test d'un module (traqueur)";
        private int _playSeed = 1;
        private int _testModule;

        /// <summary>Modules jouables du niveau aléatoire (noms), dans l'ordre du catalogue.</summary>
        public List<string> TestModules
        {
            get
            {
                var r = new List<string>();
                if (moduleCatalog != null)
                    foreach (RoomModule m in moduleCatalog.Usable())
                        r.Add(m.displayName);
                return r;
            }
        }

        public string TestModuleName
        {
            get
            {
                List<string> l = TestModules;
                return l.Count > 0 ? l[Mathf.Clamp(_testModule, 0, l.Count - 1)] : null;
            }
        }

        /// <summary>La run du niveau 1, sur cette seed de carte, avec ce module à côté du départ (null : carte normale).</summary>
        public void LaunchRandom(int seed, string module)
        {
            RandomLevel.SeedOverride = seed;
            RandomLevel.TestModule = module;
            Launch();
            if (!_launching)
            {
                RandomLevel.SeedOverride = 0;
                RandomLevel.TestModule = null;
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

        /// <summary>Vitrine (test) : lancée directement (F2 ou lien « Vitrine (test) »), éditeur et builds de développement seulement.</summary>
        public void OpenVitrine()
        {
            if (PlaytestVisible)
                Direct(VitrineScene);
        }

        /// <summary>F2 : vitrine ; F3 : menu Playtest (éditeur et builds de développement).</summary>
        private void DevKeys()
        {
            if (!PlaytestVisible || Keyboard.current == null)
                return;
            if (Keyboard.current.f2Key.wasPressedThisFrame)
                OpenVitrine();
            else if (Keyboard.current.f3Key.wasPressedThisFrame)
                OpenPlaytest();
        }

        public void OpenPlaytest()
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

        /// <summary>Menu des touches (A5) : une copie des actions du jeu, touches du joueur 1 (place au choix).</summary>
        private void OpenKeys()
        {
            if (skin == null || skin.controls == null)
                return;
            var copy = Instantiate(skin.controls);
            ItHunts.Input.RebindStore.Load(copy, 0);
            ItHunts.Hud.KeyBindingsPanel.Open(copy, 0, true, null, skin, null, ownsAsset: true);
        }

        private void Update()
        {
            if (_lobby == null)
                return;
            if (ItHunts.Hud.KeyBindingsPanel.IsOpen)
                return;                                                  // le menu des touches a la main
            InputDevice p2 = SecondPad;
            _title.Exclude = _camp.Exclude = _settings.Exclude = _menu.Exclude = p2;
            if (_status.Length > 0 && Time.unscaledTime > _statusUntil)
                _status = "";
            switch (_page)
            {
                case Page.Titre:
                    _title.Update();
                    DevKeys();
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

            UpdateAudio();
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
                    if (_page == Page.Campement)
                        FocusCamp();
                    else
                        _title.FocusFirst();
                }
            }
        }

        private void UpdateCamp(InputDevice p2)
        {
            if ((_lobby.Count > 1) != _campCoop)
                BuildCampMenu();                                         // Prêt bascule en coop, Jouer en solo
            if (Pick != null)
            {
                _pick.Exclude = _pick.Seat == 0 ? p2 : null;
                int pick = _pick.Update();
                if (pick == CampCharacterPick.Cancel)
                    _pick.Close();
                else if (pick >= 0)
                {
                    _lobby.SetCharacter(_pick.Seat, pick);
                    _pick.Close();
                }
            }
            else
            {
                _camp.Update();
                if (BackPressed(p2))
                    GoTo(Page.Titre);
                DevKeys();
            }

            // Écran partagé : Start sur une autre manette que celle du joueur 1 fait rejoindre (ou retirer) un joueur.
            foreach (Gamepad g in Gamepad.all)
            {
                if (!g.startButton.wasPressedThisFrame || g == GreyboxMenu.LastNavPad)
                    continue;
                int seat = _lobby.SeatOf(g);
                if (seat >= 0)
                {
                    if (_pick.Seat == seat)
                        _pick.Close();
                    _lobby.Leave(g);
                }
                else if (_lobby.Join(g) < 0)
                    Say("Plus de place en écran partagé : le jeu en ligne viendra plus tard");
            }

            // Les joueurs qui ont rejoint : leur manette seule (A : prêt, Y : modifier, B : plus prêt).
            for (int seat = 1; seat < CampLobby.Seats; seat++)
            {
                if (!(_lobby[seat]?.Device is Gamepad pad) || _pick.Seat == seat)
                    continue;
                if (pad.buttonSouth.wasPressedThisFrame)
                    ToggleReady(seat);
                else if (pad.buttonNorth.wasPressedThisFrame)
                    OpenSelect(seat);
                else if (pad.buttonEast.wasPressedThisFrame && _lobby[seat].Ready)
                    _lobby[seat].Ready = false;
            }

            // Coop : tout le monde est prêt, sans doublon, l'acte est jouable : la partie part.
            if (_lobby.Count > 1 && !_launching && _lobby.CanLaunch && ActPlayable(_act))
                Launch();
        }

        /// <summary>Ouvre le choix du personnage de cette place (un seul écran de choix à la fois).</summary>
        public void OpenSelect(int seat)
        {
            CampLobby.Seat s = _lobby[seat];
            if (s == null || _pick == null)
                return;
            if (_pick.IsOpen && _pick.Seat != seat)
            {
                Say($"J{_pick.Seat + 1} choisit son personnage");
                return;
            }

            _pick.Open(seat, s.Character, seat == 0 ? null : s.Device as InputDevice);
        }

        // ---------- Audio : musique du titre au campement sans coupure, feu au campement ----------

        private AudioSource _music, _fire;
        private float _fireLevel;

        /// <summary>Musique et feu (tests, mesures).</summary>
        public AudioSource MusicSource => _music;
        public AudioSource FireSource => _fire;

        private void StartAudio()
        {
            if (FindFirstObjectByType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();               // la scène du menu n'en avait pas
            _music = NewSource("Musique du menu", skin != null && skin.music != null ? skin.music : Drone());
            if (skin != null && skin.fire != null)
            {
                _fire = NewSource("Feu du campement", skin.fire);
                var gain = _fire.gameObject.AddComponent<ItHunts.Audio.AudioGain>();
                gain.gainDb = skin.fireGainDb;
                _fireLevel = _showCamp ? 1f : skin.fireOnTitle;
            }

            UpdateAudio();
            _music.Play();
            if (_fire != null)
                _fire.Play();
        }

        private AudioSource NewSource(string name, AudioClip clip)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            return src;
        }

        private void UpdateAudio()
        {
            if (_music == null)
                return;
            _music.volume = (skin != null ? skin.musicVolume : 0.3f) * GameSettings.MusicGain;   // l'écouteur porte les effets
            if (_fire == null)
                return;
            // Le feu monte quand le campement apparaît sous la brume, et redescend vers le titre.
            float target = _showCamp ? 1f : skin.fireOnTitle;
            _fireLevel = Mathf.MoveTowards(_fireLevel, target, Time.unscaledDeltaTime / Mathf.Max(0.05f, skin.fireFadeSeconds));
            _fire.volume = skin.fireVolume * _fireLevel;                                      // effets : sur l'écouteur
            var gain = _fire.GetComponent<ItHunts.Audio.AudioGain>();
            if (gain != null)
                gain.gainDb = skin.fireGainDb;
        }

        /// <summary>Nappe synthétisée (si la musique de la DA manque).</summary>
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

            AudioClip clip = AudioClip.Create("Ambiance du menu (repli)", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // ---------- Repli sans habillage (IMGUI), et page Playtest (développement) ----------

        private void OnGUI()
        {
            if (_lobby == null || (Skinned && _page != Page.Playtest))
                return;
            if (Pick != null)
            {
                CharacterDefinition c = Roster != null ? Roster[_pick.Focus] : null;
                GUI.Label(new Rect(40f, 40f, 900f, 60f), $"J{_pick.Seat + 1} : ◄ {(c != null ? c.displayName : "?")} ► ({(c != null ? c.ArtifactName : "")})  Entrée valider · Échap retour");
                return;
            }

            GreyboxMenu m = _page switch { Page.Campement => _camp, Page.Parametres => _settings, Page.Playtest => _menu, _ => _title };
            m.Draw(_page == Page.Campement ? ActLabel(_act) : "IT HUNTS", _status, 560f);
            if (Skinned)
                return;
            if (PlaytestVisible && _page == Page.Titre && GUI.Button(new Rect(Screen.width - 180f, Screen.height - 50f, 160f, 34f), PlaytestLinkLabel))
                OpenPlaytest();
            if (PlaytestVisible && _page == Page.Titre && GUI.Button(new Rect(Screen.width - 180f, Screen.height - 90f, 160f, 34f), "Vitrine (test) (F2)"))
                OpenVitrine();
        }
    }
}
