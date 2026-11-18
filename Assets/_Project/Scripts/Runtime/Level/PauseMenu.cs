using System.Collections.Generic;
using System.Linq;
using ItHunts.Hud;
using ItHunts.Input;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.Level
{
    /// <summary>
    /// Menu Échap, un par joueur (Échap au clavier, Start à la manette), habillé par la DA (panneau en 9 tranches, titre en Grenze
    /// Gotisch, boutons en EB Garamond, focus visible) dans la vue du joueur (écran partagé compris), piloté par son seul appareil
    /// (et la souris pour le clavier). Le jeu ne s'arrête pas (coop, D-048) : le joueur qui l'a ouvert ne bouge plus, l'autre continue.
    /// Entrées : Reprendre ; Paramètres (cadrage des mains, options d'image) ; Quitter l'écran partagé (2e joueur) ; Abandonner la
    /// run (retour au campement) ; Quitter le jeu. Posé par SplitScreen, sinon au chargement de toute scène de jeu (Ensure).
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private sealed class Open
        {
            public PlayerInput Player;
            public GreyboxMenu Menu;
            public GreyboxMenu Settings;
            public bool InSettings;
            public int Frame;
            public RectTransform Root;
            public UiList List, SettingsList;
            public Text Title;
        }

        private Canvas _canvas;

        private readonly List<Open> _open = new List<Open>();
        private readonly Dictionary<PlayerInput, int> _closedAt = new Dictionary<PlayerInput, int>();

        public static PauseMenu Current { get; private set; }

        private void OnEnable() => Current = this;

        /// <summary>
        /// Menu pause dans toutes les scènes de jeu, y compris celles sans écran partagé (Test_Movement…) : posé au chargement de
        /// chaque scène s'il n'y en a pas déjà un (SplitScreen pose le sien). Pas dans la scène du menu principal.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            Ensure();
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode) => Ensure();

        /// <summary>Pose un menu pause s'il manque (hors menu principal) ; renvoie celui de la scène.</summary>
        public static PauseMenu Ensure()
        {
            PauseMenu existing = FindAnyObjectByType<PauseMenu>();
            if (existing != null)
                return existing;
            string active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (active == RunSession.MenuScene || FindAnyObjectByType<MainMenu>() != null)
                return null;
            return new GameObject("Menu pause").AddComponent<PauseMenu>();
        }

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        public bool IsOpenFor(PlayerInput player) => _open.Any(o => o.Player == player);

        /// <summary>Entrées du menu de ce joueur (tests).</summary>
        public IReadOnlyList<string> ItemsFor(PlayerInput player) =>
            _open.FirstOrDefault(o => o.Player == player)?.Menu.Items.Select(i => i.Label).ToList() ?? new List<string>();

        private void Update()
        {
            if (RunEndScreen.Current != null)
            {
                foreach (Open o in _open.ToList())
                    Close(o, false);
                return;
            }

            foreach (PlayerInput pi in PlayerInput.all.ToList())
            {
                if (IsOpenFor(pi))
                    continue;
                var reader = pi.GetComponent<PlayerInputReader>();
                bool justClosed = _closedAt.TryGetValue(pi, out int f) && Time.frameCount <= f + 1;
                if (reader != null && reader.PausePressed && !justClosed)
                    OpenFor(pi);
            }

            foreach (Open o in _open.ToList())
            {
                if (o.Player == null)
                {
                    _open.Remove(o);
                    continue;
                }

                if (Time.frameCount > o.Frame && BackPressed(o.Menu.Owner))
                {
                    if (o.InSettings)
                        o.InSettings = false;                             // retour : des paramètres au menu
                    else
                        Close(o, true);
                }
                else if (o.InSettings)
                    o.Settings.Update();
                else
                    o.Menu.Update();
                if (o.Root != null && _open.Contains(o))
                {
                    if (o.Player != null && o.Player.camera != null)
                        UiKit.FitTo(o.Root, o.Player.camera);           // l'autre joueur rejoint ou part : la vue change
                    o.Title.text = o.InSettings ? "Paramètres" : "Pause";
                    o.List.Root.gameObject.SetActive(!o.InSettings);
                    o.SettingsList.Root.gameObject.SetActive(o.InSettings);
                    (o.InSettings ? o.SettingsList : o.List).Sync();
                }
            }

            if (_open.Count == 0 && PlayerLook.MenuOpen && RunEndScreen.Current == null && _hadKeyboardMenu)
            {
                PlayerLook.MenuOpen = false;
                _hadKeyboardMenu = false;
            }
        }

        private bool _hadKeyboardMenu;

        /// <summary>
        /// Referme le menu de ce joueur s'il vient de s'ouvrir (cette image ou la précédente) : le Start d'une manette qui rejoint a pu
        /// être lu comme une pause par le 1er joueur avant que SplitScreen ne lui rende ses appareils.
        /// </summary>
        public void CancelJustOpened(PlayerInput player)
        {
            Open o = _open.FirstOrDefault(x => x.Player == player && Time.frameCount <= x.Frame + 1);
            if (o != null)
                Close(o, true);
        }

        /// <summary>Ouvre le menu pause de ce joueur.</summary>
        public void OpenFor(PlayerInput pi)
        {
            if (pi == null || IsOpenFor(pi))
                return;
            var o = new Open { Player = pi, Menu = new GreyboxMenu { Owner = OwnerOf(pi) }, Settings = new GreyboxMenu { Owner = OwnerOf(pi) }, Frame = Time.frameCount };
            o.Menu.Add("Reprendre", () => Close(o, true));
            o.Menu.Add("Paramètres", () => { o.InSettings = true; o.Settings.FocusFirst(); });
            var fpv = pi.GetComponent<FirstPersonView>();
            if (fpv != null)
            {
                GreyboxMenu.Item framing = o.Settings.Add("Cadrage des mains", null);
                framing.Value = () => FirstPersonView.FramingLabel(fpv.Framing);
                framing.Change = d => fpv.Framing = fpv.Framing == HandFraming.Reference ? HandFraming.Ecarte : HandFraming.Reference;
                framing.Submit = () => framing.Change(1);
            }
            foreach (GraphicsOptions.Effect e in System.Enum.GetValues(typeof(GraphicsOptions.Effect)))
            {
                GraphicsOptions.Effect effect = e;
                GreyboxMenu.Item item = o.Settings.Add(GraphicsOptions.Label(effect), null);
                item.Value = () => GraphicsOptions.Get(effect) ? "oui" : "non";
                item.Change = d => GraphicsOptions.Set(effect, !GraphicsOptions.Get(effect));
                item.Submit = () => item.Change(1);
            }

            o.Settings.Add("Retour", () => { o.InSettings = false; o.Menu.SetFocus(1); });
            int index = IndexOf(pi);
            if (index > 0 && PlayerInput.all.Count > 1 && SplitScreen.Current != null)
                o.Menu.Add("Quitter l'écran partagé", () => LeaveSplit(o));
            // Scènes de test (Test_*) : accès direct à la vitrine (retours du lot, point 9).
            string active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (active.StartsWith("Test_") && active != MainMenu.VitrineScene)
                o.Menu.Add("Vitrine (assets et ennemis)", () => { RunSession.End(); RunSession.Load(MainMenu.VitrineScene); });
            o.Menu.Add("Abandonner la run", AbandonRun);
            o.Menu.Add("Quitter le jeu", Application.Quit);
            o.Menu.FocusFirst();
            o.Settings.FocusFirst();
            _open.Add(o);
            BuildUi(o);
            pi.DeactivateInput();                                       // il ne bouge plus ; l'autre joueur continue
            if (o.Menu.Owner is Keyboard)
            {
                PlayerLook.MenuOpen = true;                             // la souris sert au menu
                _hadKeyboardMenu = true;
            }
        }

        /// <summary>Habillage de la DA (le skin des menus du HUD du joueur) ; sans lui, le menu greybox (IMGUI).</summary>
        private void BuildUi(Open o)
        {
            var hud = o.Player.GetComponent<ItHunts.Hud.PlayerHud>();
            ItHunts.Settings.MenuSkinSettings skin = hud != null ? hud.MenuSkin : null;
            if (skin == null)
                return;
            if (_canvas == null)
                _canvas = UiKit.NewCanvas("Menus pause (DA)", 80, transform);
            o.Root = UiKit.Rect("Pause : joueur " + (IndexOf(o.Player) + 1), _canvas.transform, Vector2.zero, Vector2.zero, Vector2.zero);
            UiKit.FitTo(o.Root, o.Player.camera);
            Image shade = UiKit.Img("Voile", o.Root, null, UiKit.Shade);
            UiKit.Stretch(shade.rectTransform);
            int rows = Mathf.Max(o.Menu.Items.Count, o.Settings.Items.Count);
            RectTransform panel = UiKit.Rect("Panneau", o.Root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(660f, rows * 56f + 210f));
            UiKit.Panel(panel, skin);
            o.Title = UiKit.Txt("Titre", panel, skin.titleFont, 60, UiKit.TextColor);
            o.Title.rectTransform.anchorMin = new Vector2(0f, 1f);
            o.Title.rectTransform.anchorMax = new Vector2(1f, 1f);
            o.Title.rectTransform.anchoredPosition = new Vector2(0f, -72f);
            o.Title.rectTransform.sizeDelta = new Vector2(0f, 80f);
            Text who = UiKit.Txt("Joueur", panel, skin.textFont, 24, UiKit.Dim);
            who.rectTransform.anchorMin = new Vector2(0f, 1f);
            who.rectTransform.anchorMax = new Vector2(1f, 1f);
            who.rectTransform.anchoredPosition = new Vector2(0f, -120f);
            who.rectTransform.sizeDelta = new Vector2(0f, 30f);
            who.text = (PlayerInput.all.Count > 1 ? $"Joueur {IndexOf(o.Player) + 1} · " : "") + "le jeu continue";
            RectTransform main = UiKit.Rect("Boutons", panel, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(560f, rows * 56f), new Vector2(0.5f, 1f));
            o.List = new UiList(o.Menu, main, skin, 56f, 30);
            RectTransform set = UiKit.Rect("Paramètres", panel, new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(600f, rows * 56f), new Vector2(0.5f, 1f));
            o.SettingsList = new UiList(o.Settings, set, skin, 56f, 28);
        }

        /// <summary>Abandonner la run : retour au campement (pas à l'écran titre).</summary>
        private static void AbandonRun()
        {
            MainMenu.StartAtCamp = true;
            ToMenu();
        }

        private void Close(Open o, bool reactivate)
        {
            _open.Remove(o);
            if (o.Root != null)
                Destroy(o.Root.gameObject);
            if (o.Player == null)
                return;
            _closedAt[o.Player] = Time.frameCount;
            if (reactivate)
                o.Player.ActivateInput();
            if (o.Menu.Owner is Keyboard && !_open.Any(x => x.Menu.Owner is Keyboard) && RunEndScreen.Current == null)
            {
                PlayerLook.MenuOpen = false;
                _hadKeyboardMenu = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void LeaveSplit(Open o)
        {
            PlayerInput pi = o.Player;
            _open.Remove(o);
            if (o.Root != null)
                Destroy(o.Root.gameObject);
            if (SplitScreen.Current != null)
                SplitScreen.Current.Leave(pi);
        }

        private static void ToMenu()
        {
            string menu = RunSession.MenuScene;
            RunSession.End();
            if (!RunSession.Load(menu))
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private static int IndexOf(PlayerInput pi)
        {
            for (int i = 0; i < PlayerInput.all.Count; i++)
                if (PlayerInput.all[i] == pi)
                    return i;
            return -1;
        }

        /// <summary>Appareil du joueur : sa manette s'il joue à la manette, sinon le clavier.</summary>
        private static InputDevice OwnerOf(PlayerInput pi)
        {
            if (pi.currentControlScheme == "Gamepad")
            {
                Gamepad pad = pi.devices.OfType<Gamepad>().FirstOrDefault();
                if (pad != null)
                    return pad;
            }

            return Keyboard.current;
        }

        private static bool BackPressed(InputDevice owner)
        {
            if (owner is Gamepad g)
                return g.startButton.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame;
            Keyboard k = Keyboard.current;
            return k != null && k.escapeKey.wasPressedThisFrame;
        }

        private void OnGUI()
        {
            foreach (Open o in _open)
            {
                if (o.Player == null || o.Root != null)
                    continue;                                           // habillé par la DA (uGUI)
                Camera cam = o.Player.camera;
                Rect r = cam != null ? cam.rect : new Rect(0f, 0f, 1f, 1f);
                var region = new Rect(r.x * Screen.width, (1f - r.y - r.height) * Screen.height, r.width * Screen.width, r.height * Screen.height);
                string who = PlayerInput.all.Count > 1 ? $"Joueur {IndexOf(o.Player) + 1}. " : "";
                (o.InSettings ? o.Settings : o.Menu).Draw(o.InSettings ? "Paramètres" : "Pause", who + "Le jeu continue.", 460f, region);
            }
        }
    }
}
