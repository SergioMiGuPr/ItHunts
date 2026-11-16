using System.Collections.Generic;
using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Hud;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Level
{
    /// <summary>
    /// Choix du personnage d'un joueur qui rejoint en cours de partie (D-064) : un menu greybox dans sa moitié d'écran, piloté par
    /// sa seule manette. Les personnages pris par les autres joueurs sont grisés (« pris »), ceux sans artefact aussi
    /// (« bientôt »). Pendant le choix, il ne bouge pas ; A valide (son artefact est équipé), B garde le personnage proposé. Posé
    /// par SplitScreen, à côté du menu pause.
    /// </summary>
    public sealed class CharacterSelectMenu : MonoBehaviour
    {
        private sealed class Open
        {
            public PlayerInput Player;
            public GreyboxMenu Menu;
            public int Frame;
        }

        private readonly List<Open> _open = new List<Open>();

        public static CharacterSelectMenu Current { get; private set; }

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }

        public bool IsOpenFor(PlayerInput player) => _open.Any(o => o.Player == player);

        /// <summary>Entrées du menu de ce joueur (tests) : libellé, choisissable.</summary>
        public IReadOnlyList<(string label, bool enabled)> ItemsFor(PlayerInput player) =>
            _open.FirstOrDefault(o => o.Player == player)?.Menu.Items.Select(i => (i.Label, i.Enabled)).ToList()
            ?? new List<(string, bool)>();

        private static CharacterRoster Roster => RunState.Current != null ? RunState.Current.Characters : null;

        /// <summary>Personnages des autres joueurs (indices dans la liste).</summary>
        private static int[] Taken(PlayerInput self)
        {
            CharacterRoster r = Roster;
            var taken = new List<int>();
            if (r == null)
                return taken.ToArray();
            foreach (PlayerInput pi in PlayerInput.all)
            {
                if (pi == self)
                    continue;
                var h = pi.GetComponent<ArtifactHolder>();
                if (h == null)
                    continue;
                h.EnsureReady();
                int i = r.characters.IndexOf(h.Character);
                if (i >= 0)
                    taken.Add(i);
            }

            return taken.ToArray();
        }

        /// <summary>Ouvre le choix pour ce joueur (il rejoint en jeu).</summary>
        public void OpenFor(PlayerInput pi)
        {
            CharacterRoster r = Roster;
            if (pi == null || r == null || r.Count == 0 || IsOpenFor(pi))
                return;
            var holder = pi.GetComponent<ArtifactHolder>();
            holder?.EnsureReady();
            var o = new Open { Player = pi, Menu = new GreyboxMenu { Owner = OwnerOf(pi) }, Frame = Time.frameCount };
            int[] taken = Taken(pi);
            int current = holder != null ? r.characters.IndexOf(holder.Character) : -1;
            for (int i = 0; i < r.Count; i++)
            {
                CharacterDefinition c = r[i];
                int index = i;
                bool isTaken = taken.Contains(i);
                GreyboxMenu.Item it = o.Menu.Add($"{c.displayName} ({c.ArtifactName})", () => Choose(o, index), c.Selectable && !isTaken);
                it.DisabledNote = isTaken ? "(pris)" : "(bientôt)";
            }

            o.Menu.FocusFirst();
            o.Menu.SetFocus(current);                                 // sur le personnage proposé s'il est libre
            _open.Add(o);
            pi.DeactivateInput();                                     // il ne bouge pas pendant le choix
        }

        /// <summary>Choix validé : personnage et artefact équipés, mémorisés pour la suite de la run.</summary>
        public void Choose(PlayerInput pi, int index)
        {
            Open o = _open.FirstOrDefault(x => x.Player == pi);
            if (o != null)
                Choose(o, index);
        }

        private void Choose(Open o, int index)
        {
            CharacterRoster r = Roster;
            if (r == null || r[index] == null || !r[index].Selectable || Taken(o.Player).Contains(index))
                return;
            var holder = o.Player.GetComponent<ArtifactHolder>();
            if (holder != null)
                holder.EquipCharacter(r[index]);
            int p = IndexOf(o.Player);
            int[] chars = RunSession.Characters ?? new int[0];
            if (p >= 0)
            {
                if (chars.Length <= p)
                    System.Array.Resize(ref chars, p + 1);
                chars[p] = index;
                RunSession.Characters = chars;
            }

            Close(o);
        }

        private void Close(Open o)
        {
            _open.Remove(o);
            if (o.Player != null)
                o.Player.ActivateInput();
        }

        private void Update()
        {
            foreach (Open o in _open.ToList())
            {
                if (o.Player == null)
                {
                    _open.Remove(o);
                    continue;
                }

                if (Time.frameCount > o.Frame && o.Menu.Owner is Gamepad g && g.buttonEast.wasPressedThisFrame)
                    Close(o);                                         // B : garde le personnage proposé
                else
                    o.Menu.Update();
            }
        }

        private static int IndexOf(PlayerInput pi)
        {
            for (int i = 0; i < PlayerInput.all.Count; i++)
                if (PlayerInput.all[i] == pi)
                    return i;
            return -1;
        }

        private static InputDevice OwnerOf(PlayerInput pi)
        {
            Gamepad pad = pi.devices.OfType<Gamepad>().FirstOrDefault();
            return pad != null ? pad : (InputDevice)Keyboard.current;
        }

        private void OnGUI()
        {
            foreach (Open o in _open)
            {
                if (o.Player == null)
                    continue;
                Camera cam = o.Player.camera;
                Rect rc = cam != null ? cam.rect : new Rect(0f, 0f, 1f, 1f);
                var region = new Rect(rc.x * Screen.width, (1f - rc.y - rc.height) * Screen.height, rc.width * Screen.width, rc.height * Screen.height);
                int n = IndexOf(o.Player) + 1;
                o.Menu.Draw("Personnage", $"Joueur {n} : choisis ton personnage (A). Les personnages pris sont grisés.", 460f, region);
            }
        }
    }
}
