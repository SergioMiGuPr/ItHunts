using System.Collections;
using System.Collections.Generic;
using ItHunts.Art.Menus;
using ItHunts.Hud;
using ItHunts.Level;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Menus
{
    /// <summary>
    /// Vue des menus d'avant-partie : les prefabs de la DA (PR #18, UI-HANDOFF § 2, maquettes de l'utilisateur au pixel)
    /// pilotés par l'état de MainMenu. Rien n'est dessiné ici : seuls les textes variables (noms, ACTE, messages, liens de
    /// développement) sont du texte Unity. Canvas en Screen Space - Camera sur la caméra du menu, comme la scène d'aperçu de la
    /// DA (couleurs calculées en linéaire). Posée sur chaque MainMenu (MenuArtInstall).
    /// </summary>
    [DefaultExecutionOrder(100)]                                        // après la logique du menu (son Update)
    public sealed class MainMenuArt : MonoBehaviour
    {
        private MainMenu _menu;
        private MenuSkinSettings _skin;
        private Canvas _canvas;
        private Camera _cam;
        private RectTransform _root;

        // Titre
        private GameObject _title;
        private TitreApercu _titleView;
        private int _titleShown = -1;
        private Text _vitrineLink, _playtestLink, _status;

        // Campement
        private GameObject _camp;
        private CampementApercu _campView;
        private Selectable _modify, _ready, _settingsButton, _friends, _back, _plus3, _plus4;
        private RectTransform _plus2;
        private RectTransform _actSelector;
        private Text _actText;
        private readonly int[] _shownCharacter = { -2, -2, -2, -2 };
        private readonly bool[] _shownReady = new bool[4];
        private Color[] _nameColors;
        private bool _campSynced;

        // Choix du personnage
        private GameObject _choice;
        private ChoixPersoApercu _choiceView;
        private Selectable _validate;
        private int _choiceShown = -1;

        // Paramètres
        private GameObject _settings;
        private readonly List<(RectTransform rect, Text value, Graphic[] tint)> _rows = new List<(RectTransform, Text, Graphic[])>();
        private Selectable _settingsBack;

        // Transition
        private RectTransform _fog;
        private Image _veil, _mist;

        public MainMenu Menu => _menu;
        public GameObject TitleScreen => _title;
        public GameObject CampScreen => _camp;
        public GameObject ChoiceScreen => _choice;
        public GameObject SettingsScreen => _settings;
        public Canvas Canvas => _canvas;

        /// <summary>Construit la vue (appelé à la pose ; public pour les tests).</summary>
        public bool Build(MainMenu menu)
        {
            _menu = menu;
            _skin = menu != null ? menu.Skin : null;
            if (_skin == null || _skin.titleScreen == null || _skin.campScreen == null)
                return false;

            _cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            var go = new GameObject("Menus (DA)", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            if (_cam != null)
            {
                _canvas.renderMode = RenderMode.ScreenSpaceCamera;          // comme Test_UI_Menus (rendu en linéaire)
                _canvas.worldCamera = _cam;
                _canvas.planeDistance = _cam.nearClipPlane + 0.05f;          // devant tout décor de la scène
            }
            else
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 50;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            _root = (RectTransform)go.transform;

            var black = new GameObject("Noir", typeof(RectTransform)).AddComponent<Image>();
            black.transform.SetParent(_root, false);
            black.color = Color.black;
            black.raycastTarget = false;
            UiKit.Stretch(black.rectTransform);

            BuildTitle();
            BuildCamp();
            BuildChoice();
            BuildSettings();
            BuildTransition();
            BuildTexts();
            menu.Skinned = true;
            Sync();
            return true;
        }

        private RectTransform Page(string name)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(_root, false);
            UiKit.Stretch(r);
            return r;
        }

        private void BuildTitle()
        {
            _title = MenuArtKit.Spawn(_skin.titleScreen, Page("Écran titre"), "Menu_Titre");
            _titleView = _title.GetComponentInChildren<TitreApercu>(true);
        }

        private readonly System.Collections.Generic.List<Material> _ownedMaterials = new System.Collections.Generic.List<Material>();

        /// <summary>
        /// Chaque place reçoit sa copie du matériau d'éclairage (A1, matin du 2026-11-21) : CampementApercu ne copie qu'en Play
        /// Mode ; en mode édition (tests, aperçu) il écrivait dans le matériau partagé de la DA (M_Campement_Assis_N.mat).
        /// </summary>
        private void OwnCampMaterials()
        {
            if (_campView == null || _campView.places == null)
                return;
            foreach (CampementApercu.Place pl in _campView.places)
            {
                if (pl?.corps == null || pl.corps.material == null || pl.matJeu != null)
                    continue;
                Material m = pl.corps.material;
                pl.matJeu = new Material(m) { name = m.name + " (jeu)", hideFlags = HideFlags.DontSave };
                pl.corps.material = pl.matJeu;
                _ownedMaterials.Add(pl.matJeu);
            }
        }

        private void OnDestroy()
        {
            foreach (Material m in _ownedMaterials)
                if (m != null)
                    DestroyImmediate(m);
            _ownedMaterials.Clear();
        }

        private void BuildCamp()
        {
            _camp = MenuArtKit.Spawn(_skin.campScreen, Page("Campement"), "Menu_Campement");
            _campView = _camp.GetComponent<CampementApercu>();
            OwnCampMaterials();
            _modify = MenuArtKit.Find<Selectable>(_camp, "Interface/Modifier");
            _ready = MenuArtKit.Find<Selectable>(_camp, "Interface/Pret");
            _settingsButton = MenuArtKit.Find<Selectable>(_camp, "Interface/Parametres");
            _friends = MenuArtKit.Find<Selectable>(_camp, "Interface/Amis");
            _back = MenuArtKit.Find<Selectable>(_camp, "Interface/Retour");
            _plus3 = MenuArtKit.Find<Selectable>(_camp, "Interface/Plus_3");
            _plus4 = MenuArtKit.Find<Selectable>(_camp, "Interface/Plus_4");
            _actSelector = MenuArtKit.Find<RectTransform>(_camp, "Interface/Selecteur_Acte");
            _actText = MenuArtKit.Find<Text>(_camp, "Interface/Selecteur_Acte/Acte");
            Transform chat = _camp.transform.Find("Interface/Chat");
            if (chat != null)
                chat.gameObject.SetActive(_skin.showChat);
            _nameColors = new Color[_campView != null ? _campView.places.Length : 0];
            for (int i = 0; i < _nameColors.Length; i++)
                _nameColors[i] = _campView.places[i]?.nom != null ? _campView.places[i].nom.color : Color.white;
        }

        private void BuildChoice()
        {
            if (_skin.choiceScreen == null)
                return;
            _choice = MenuArtKit.Spawn(_skin.choiceScreen, Page("Choix du personnage"), "Menu_Choix");
            _choiceView = _choice.GetComponent<ChoixPersoApercu>();
            if (_choiceView != null && _menu.Roster != null)
                _choiceView.roster = _menu.Roster;
            _validate = MenuArtKit.Find<Selectable>(_choice, "Valider");
            _choice.SetActive(false);
        }

        private void BuildSettings()
        {
            if (_skin.settingsScreen == null)
                return;
            _settings = MenuArtKit.Spawn(_skin.settingsScreen, Page("Paramètres"), "Menu_Parametres");
            for (int i = 0; i < GameSettings.Labels.Length; i++)
            {
                Transform row = _settings.transform.Find("Option_" + i);
                if (row == null)
                    continue;
                var label = row.Find("Libelle")?.GetComponent<Text>();
                if (label != null)
                    label.text = GameSettings.Labels[i];
                _rows.Add(((RectTransform)row, row.Find("Valeur")?.GetComponent<Text>(), MenuArtKit.Targets(row.GetComponent<Selectable>())));
            }

            _settingsBack = MenuArtKit.Find<Selectable>(_settings, "Retour");
            _settings.SetActive(false);
        }

        /// <summary>Transition : un voile qui monte (dégradé) et la brume de la DA qui dérive par-dessus (shader ItHunts/UI/Additif).</summary>
        private void BuildTransition()
        {
            _fog = Page("Transition : brume");
            _veil = new GameObject("Voile", typeof(RectTransform)).AddComponent<Image>();
            _veil.transform.SetParent(_fog, false);
            _veil.sprite = UiKit.Gradient();
            _veil.color = _skin.transitionFog;
            _veil.raycastTarget = false;
            _veil.rectTransform.anchorMin = new Vector2(0f, 0f);
            _veil.rectTransform.anchorMax = new Vector2(1f, 0f);
            _veil.rectTransform.pivot = new Vector2(0.5f, 0f);
            if (_skin.transitionFogMaterial != null)
            {
                _mist = new GameObject("Brume (DA)", typeof(RectTransform)).AddComponent<Image>();
                _mist.transform.SetParent(_fog, false);
                _mist.material = _skin.transitionFogMaterial;
                _mist.raycastTarget = false;
                UiKit.Stretch(_mist.rectTransform);
            }

            _fog.gameObject.SetActive(false);
        }

        private void BuildTexts()
        {
            Font italic = _skin.textItalic != null ? _skin.textItalic : _skin.textFont;
            var dim = new Color(0.70f, 0.65f, 0.60f, 0.9f);
            _vitrineLink = MenuArtKit.Label(_root, "Vitrine (développement)", italic, 22, dim, TextAnchor.LowerRight);
            Corner(_vitrineLink.rectTransform, 70f);
            _vitrineLink.text = MainMenu.VitrineLinkLabel;
            _playtestLink = MenuArtKit.Label(_root, "Playtest (développement)", italic, 22, dim, TextAnchor.LowerRight);
            Corner(_playtestLink.rectTransform, 34f);
            _playtestLink.text = MainMenu.PlaytestLinkLabel;
            _vitrineLink.gameObject.SetActive(MainMenu.PlaytestVisible);
            _playtestLink.gameObject.SetActive(MainMenu.PlaytestVisible);
            _status = MenuArtKit.Label(_root, "Message", italic, 30, new Color(0.97f, 0.88f, 0.70f), TextAnchor.MiddleCenter);
            RectTransform s = _status.rectTransform;
            s.anchorMin = new Vector2(0f, 0f);
            s.anchorMax = new Vector2(1f, 0f);
            s.pivot = new Vector2(0.5f, 0f);
            s.anchoredPosition = new Vector2(0f, 150f);
            s.sizeDelta = new Vector2(-200f, 44f);
        }

        private static void Corner(RectTransform r, float y)
        {
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(1f, 0f);
            r.anchoredPosition = new Vector2(-36f, y);
            r.sizeDelta = new Vector2(360f, 32f);
        }

        // ---------- Chaque image ----------

        private void LateUpdate()
        {
            if (_menu != null && _root != null)
                Sync();
        }

        /// <summary>Met la vue à l'état du menu (public pour les tests).</summary>
        public void Sync()
        {
            MenuArtKit.ApplyFreeze();                 // « Réduire les animations » : effets de la DA au repos
            MainMenu.Page page = _menu.Current;
            bool camp = _menu.ShowsCamp;
            _title.SetActive(!camp);
            _camp.SetActive(camp);
            if (!camp)
                SyncTitle(page == MainMenu.Page.Titre);
            else
                SyncCamp(page == MainMenu.Page.Campement && _menu.Pick == null);
            SyncChoice();
            SyncSettings(page == MainMenu.Page.Parametres);
            SyncTransition();
            bool devLinks = MainMenu.PlaytestVisible && page == MainMenu.Page.Titre;
            _vitrineLink.gameObject.SetActive(devLinks);
            _playtestLink.gameObject.SetActive(devLinks);
            if (devLinks && MenuArtKit.Clicked)
            {
                if (MenuArtKit.Over(_vitrineLink.rectTransform, _cam))
                    _menu.OpenVitrine();
                else if (MenuArtKit.Over(_playtestLink.rectTransform, _cam))
                    _menu.OpenPlaytest();
            }

            string status = _menu.Status;
            if (string.IsNullOrEmpty(status) && _menu.Pick != null && _menu.Pick.Seat > 0)
                status = $"Joueur {_menu.Pick.Seat + 1} choisit son personnage";
            _status.text = status;
        }

        private void SyncTitle(bool interactive)
        {
            GreyboxMenu m = _menu.TitleMenu;
            if (_titleView != null && m.Focus != _titleShown)
            {
                _titleShown = m.Focus;
                _titleView.Applique(m.Focus);
            }

            if (!interactive || _titleView == null)
                return;
            for (int i = 0; i < _titleView.elements.Length && i < m.Items.Count; i++)
            {
                var hit = _titleView.elements[i].bouton != null ? (RectTransform)_titleView.elements[i].bouton.transform : null;
                if (!MenuArtKit.Over(hit, _cam))
                    continue;
                if (MenuArtKit.MouseMoved)
                    m.SetFocus(i);
                if (MenuArtKit.Clicked)
                {
                    m.SetFocus(i);
                    m.Items[i].Submit?.Invoke();
                }
            }
        }

        private void SyncCamp(bool interactive)
        {
            CampLobby lobby = _menu.Lobby;
            CharacterRoster roster = _menu.Roster;
            bool coop = lobby.Count > 1;
            for (int i = 0; i < CampLobby.Seats && _campView != null && i < _campView.places.Length; i++)
            {
                CampLobby.Seat s = lobby[i];
                bool online = i >= lobby.MaxLocal;
                int character = s != null && !online ? s.Character : -1;
                if (character != _shownCharacter[i])
                {
                    int perso = PersoIndex(roster != null ? roster[character] : null);
                    if (character < 0 || perso < 0)
                        _campView.Libere(i);
                    else
                    {
                        _campView.Occupe(i, perso, string.Format(_skin.playerNameFormat, i + 1), false);
                        if (_campSynced && _shownCharacter[i] < 0)
                            StartCoroutine(Appear(_campView.places[i]));
                    }

                    _shownCharacter[i] = character;
                    _shownReady[i] = !(coop && s != null && s.Ready);    // force la coche (et la place du nom) à se remettre
                }

                bool ready = coop && s != null && s.Ready;
                if (ready != _shownReady[i])
                {
                    _campView.Pret(i, ready);
                    _shownReady[i] = ready;
                }

                Text name = _campView.places[i]?.nom;
                if (name != null)
                    name.color = s != null && lobby.Duplicate(i) ? new Color(0.95f, 0.45f, 0.38f) : _nameColors[i];
            }

            _campSynced = true;
            if (_actText != null)
            {
                _actText.text = "‹  " + MainMenu.ActLabel(_menu.Act) + "  ›";
                Color c = _actText.color;
                c.a = MainMenu.ActPlayable(_menu.Act) ? 1f : 0.55f;
                _actText.color = c;
            }

            GreyboxMenu m = _menu.CampMenu;
            Bind(m, _menu.CampModify, _modify, interactive);
            Bind(m, _menu.CampReady, _ready, interactive, _menu.ReadyBlocked);
            Bind(m, _menu.CampSettings, _settingsButton, interactive);
            Bind(m, _menu.CampFriends, _friends, interactive);
            Bind(m, _menu.CampBack, _back, interactive);
            if (!interactive || !MenuArtKit.Clicked)
                return;
            if (_plus2 == null && _camp != null)
                _plus2 = _camp.transform.Find("Interface/Plus_2") as RectTransform;   // PR #19 : place 2 vide (+, lanterne éteinte)
            if (_plus2 != null && _plus2.gameObject.activeInHierarchy && MenuArtKit.Over(_plus2, _cam))
                _menu.Say("Place 2 : appuyez sur Start sur une 2e manette");
            if (MenuArtKit.Over(Rect(_plus3), _cam) || MenuArtKit.Over(Rect(_plus4), _cam))
                _menu.Say("Places 3 et 4 : jeu en ligne, plus tard (Start sur une 2e manette : place 2)");
            if (_actSelector != null && MenuArtKit.Over(_actSelector, _cam))
                _menu.ChangeAct(MenuArtKit.LocalX(_actSelector, _cam) < 0.5f ? -1 : 1);
        }

        private static RectTransform Rect(Component c) => c != null ? (RectTransform)c.transform : null;

        /// <summary>Un bouton de la maquette lié à une entrée du menu : teinte (focus, pressé, grisé), survol et clic.</summary>
        private void Bind(GreyboxMenu m, GreyboxMenu.Item item, Selectable button, bool interactive, bool dim = false)
        {
            if (item == null || button == null)
                return;
            int index = m.Items.IndexOf(item);
            bool over = interactive && MenuArtKit.Over(Rect(button), _cam);
            if (over && MenuArtKit.MouseMoved)
                m.SetFocus(index);
            bool focus = interactive && m.Focus == index;
            MenuArtKit.State st = dim ? MenuArtKit.State.Disabled : over && MenuArtKit.MouseDown ? MenuArtKit.State.Pressed
                : focus ? MenuArtKit.State.Hover : MenuArtKit.State.Normal;
            MenuArtKit.Tint(MenuArtKit.Targets(button), st);
            if (over && MenuArtKit.Clicked)
            {
                m.SetFocus(index);
                item.Submit?.Invoke();
            }
        }

        /// <summary>Indice du personnage dans les pièces du campement (Renard, Koala, Cameleon, Hibou), par identifiant.</summary>
        private int PersoIndex(CharacterDefinition c)
        {
            if (c == null || _campView == null)
                return -1;
            for (int i = 0; i < _campView.persos.Length; i++)
                if (string.Equals(_campView.persos[i].id, c.id, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        /// <summary>Un joueur rejoint : fondu et léger grandissement, la lueur s'allume plus fort puis se pose (comme CampementApercu).</summary>
        private IEnumerator Appear(CampementApercu.Place pl)
        {
            var cg = pl.racine != null ? pl.racine.GetComponent<CanvasGroup>() : null;
            Vector3 s0 = pl.corps != null ? pl.corps.rectTransform.localScale : Vector3.one;
            Color lc = pl.lueur != null ? pl.lueur.color : Color.white;
            float d = _campView.dureeApparition;
            for (float t = 0f; t < d; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / d);
                if (cg != null) cg.alpha = k;
                if (pl.corps != null) pl.corps.rectTransform.localScale = new Vector3(1f, Mathf.Lerp(0.96f, 1f, k), 1f);
                if (pl.lueur != null) pl.lueur.color = lc * (1f + 1.5f * (1f - k)) * k;
                yield return null;
            }

            if (cg != null) cg.alpha = 1f;
            if (pl.corps != null) pl.corps.rectTransform.localScale = s0;
            if (pl.lueur != null) pl.lueur.color = lc;
        }

        private void SyncChoice()
        {
            if (_choice == null)
                return;
            CampCharacterPick pick = _menu.Pick;
            _choice.SetActive(pick != null);
            if (pick == null)
            {
                _choiceShown = -1;
                return;
            }

            CharacterRoster roster = _menu.Roster;
            if (pick.Focus != _choiceShown && _choiceView != null)
            {
                _choiceShown = pick.Focus;
                CharacterDefinition c = roster != null ? roster[pick.Focus] : null;
                int entry = EntryIndex(c);
                if (entry >= 0)
                    _choiceView.Choisit(entry);
                // Textes lus dans les données du jeu : nom, artefact, description courte de l'artefact.
                if (c != null)
                {
                    if (_choiceView.nom != null)
                        _choiceView.nom.text = c.displayName;
                    if (_choiceView.nomArtefact != null)
                        _choiceView.nomArtefact.text = c.artifact != null ? c.artifact.displayName : "Artefact " + _choiceView.aVenir;
                    if (_choiceView.description != null)
                        _choiceView.description.text = c.artifact != null ? c.artifact.description : _choiceView.aVenir;
                }
            }

            bool mouse = pick.Owner == null;
            if (mouse && _choiceView != null && roster != null)
                for (int i = 0; i < roster.Count; i++)
                {
                    int entry = EntryIndex(roster[i]);
                    if (entry < 0)
                        continue;
                    ChoixPersoApercu.Entree e = _choiceView.entrees[entry];
                    bool over = MenuArtKit.Over(Rect(e.vignetteDiscrete), _cam) || MenuArtKit.Over(Rect(e.vignetteIlluminee), _cam);
                    if (over && (MenuArtKit.MouseMoved || MenuArtKit.Clicked))
                        pick.SetFocus(i);
                }

            bool overValidate = mouse && MenuArtKit.Over(Rect(_validate), _cam);
            bool ok = pick.Choosable(pick.Focus);
            MenuArtKit.Tint(MenuArtKit.Targets(_validate), !ok ? MenuArtKit.State.Disabled
                : overValidate && MenuArtKit.MouseDown ? MenuArtKit.State.Pressed : MenuArtKit.State.Hover);
            if (overValidate && MenuArtKit.Clicked)
                pick.ValidateClicked = true;
        }

        private int EntryIndex(CharacterDefinition c)
        {
            if (c == null || _choiceView == null)
                return -1;
            for (int i = 0; i < _choiceView.entrees.Length; i++)
                if (string.Equals(_choiceView.entrees[i].id, c.id, System.StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }

        private void SyncSettings(bool open)
        {
            if (_settings == null)
                return;
            _settings.SetActive(open);
            if (!open)
                return;
            GreyboxMenu m = _menu.SettingsMenu;
            for (int i = 0; i < _rows.Count && i < m.Items.Count; i++)
            {
                (RectTransform rect, Text value, Graphic[] tint) = _rows[i];
                GreyboxMenu.Item it = m.Items[i];
                if (value != null)
                    value.text = "‹  " + it.Value?.Invoke() + "  ›";
                bool over = MenuArtKit.Over(rect, _cam);
                if (over && MenuArtKit.MouseMoved)
                    m.SetFocus(i);
                MenuArtKit.Tint(tint, m.Focus == i ? MenuArtKit.State.Hover : MenuArtKit.State.Normal);
                if (over && MenuArtKit.Clicked)
                {
                    m.SetFocus(i);
                    it.Change?.Invoke(MenuArtKit.LocalX(rect, _cam) < 0.5f ? -1 : 1);
                }
            }

            GreyboxMenu.Item back = m.Items.Count > 0 ? m.Items[m.Items.Count - 1] : null;
            Bind(m, back, _settingsBack, true);
        }

        private void SyncTransition()
        {
            float cover = _menu.FogCover;
            bool on = cover > 0.001f;
            if (_fog.gameObject.activeSelf != on)
                _fog.gameObject.SetActive(on);
            if (!on)
                return;
            float h = _root.rect.height;
            // Le voile (dégradé : opaque en bas, transparent en haut) monte ; couvert, il remplit l'écran.
            _veil.rectTransform.sizeDelta = new Vector2(0f, h * 2f);
            _veil.rectTransform.anchoredPosition = new Vector2(0f, Mathf.Lerp(-h * 2f, 0f, cover));
            if (_mist != null)
                _mist.color = new Color(1f, 1f, 1f, cover);
        }
    }
}
