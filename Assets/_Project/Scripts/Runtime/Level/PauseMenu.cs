using System.Collections.Generic;
using System.Linq;
using ItHunts.Hud;
using ItHunts.Input;
using ItHunts.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Level
{
    /// <summary>
    /// Menu pause greybox, un par joueur (Échap au clavier, Start à la manette). Il s'affiche dans la moitié d'écran du joueur et
    /// se pilote avec son seul appareil. Le jeu ne s'arrête pas (coop, D-048). Le joueur qui l'a ouvert ne bouge plus, l'autre
    /// continue. Entrées : Reprendre ; Quitter l'écran partagé (2e joueur seulement) ; Retour au menu principal. Posé par
    /// SplitScreen, sinon au chargement de toute scène de jeu (Ensure).
    /// Refait à l'étape 10.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        private sealed class Open
        {
            public PlayerInput Player;
            public GreyboxMenu Menu;
            public int Frame;
        }

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
                    Close(o, true);
                else
                    o.Menu.Update();
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
            var o = new Open { Player = pi, Menu = new GreyboxMenu { Owner = OwnerOf(pi) }, Frame = Time.frameCount };
            o.Menu.Add("Reprendre", () => Close(o, true));
            var fpv = pi.GetComponent<FirstPersonView>();
            if (fpv != null)
            {
                GreyboxMenu.Item framing = o.Menu.Add("Cadrage des mains", null);
                framing.Value = () => FirstPersonView.FramingLabel(fpv.Framing);
                framing.Change = d => fpv.Framing = fpv.Framing == HandFraming.Reference ? HandFraming.Ecarte : HandFraming.Reference;
                framing.Submit = () => framing.Change(1);
            }
            foreach (GraphicsOptions.Effect e in System.Enum.GetValues(typeof(GraphicsOptions.Effect)))
            {
                GraphicsOptions.Effect effect = e;
                GreyboxMenu.Item item = o.Menu.Add(GraphicsOptions.Label(effect), null);
                item.Value = () => GraphicsOptions.Get(effect) ? "oui" : "non";
                item.Change = d => GraphicsOptions.Set(effect, !GraphicsOptions.Get(effect));
                item.Submit = () => item.Change(1);
            }

            int index = IndexOf(pi);
            if (index > 0 && PlayerInput.all.Count > 1 && SplitScreen.Current != null)
                o.Menu.Add("Quitter l'écran partagé", () => LeaveSplit(o));
            // Scènes de test (Test_*) : accès direct à la vitrine (retours du lot, point 9).
            string active = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (active.StartsWith("Test_") && active != MainMenu.VitrineScene)
                o.Menu.Add("Vitrine (assets et ennemis)", () => { RunSession.End(); RunSession.Load(MainMenu.VitrineScene); });
            o.Menu.Add("Retour au menu principal", ToMenu);
            o.Menu.FocusFirst();
            _open.Add(o);
            pi.DeactivateInput();                                       // il ne bouge plus ; l'autre joueur continue
            if (o.Menu.Owner is Keyboard)
            {
                PlayerLook.MenuOpen = true;                             // la souris sert au menu
                _hadKeyboardMenu = true;
            }
        }

        private void Close(Open o, bool reactivate)
        {
            _open.Remove(o);
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
                if (o.Player == null)
                    continue;
                Camera cam = o.Player.camera;
                Rect r = cam != null ? cam.rect : new Rect(0f, 0f, 1f, 1f);
                var region = new Rect(r.x * Screen.width, (1f - r.y - r.height) * Screen.height, r.width * Screen.width, r.height * Screen.height);
                string who = PlayerInput.all.Count > 1 ? $"Joueur {IndexOf(o.Player) + 1}. " : "";
                o.Menu.Draw("Pause", who + "Le jeu continue.", 460f, region);
            }
        }
    }
}
