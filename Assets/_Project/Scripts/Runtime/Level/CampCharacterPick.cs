using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Level
{
    /// <summary>
    /// Choix du personnage d'une place du campement (logique de l'écran de choix de la DA, UI-HANDOFF § 2.5) : gauche / droite
    /// (clavier, manette du joueur) ou la souris passent d'un personnage à l'autre, Valider (Entrée, A, clic) le prend s'il est
    /// jouable, Retour (Échap, B) referme sans rien changer. Les doublons sont permis ici : c'est Prêt qui les refuse.
    /// </summary>
    public sealed class CampCharacterPick
    {
        public const int None = -2, Cancel = -1;

        private readonly CharacterRoster _roster;
        private float _repeatAt;
        private int _openedFrame;

        public CampCharacterPick(CharacterRoster roster) => _roster = roster;

        public bool IsOpen { get; private set; }
        public int Seat { get; private set; } = -1;
        public int Focus { get; private set; }
        /// <summary>Appareil du joueur (null : clavier, souris et toute manette sauf celles exclues).</summary>
        public InputDevice Owner { get; private set; }
        public InputDevice Exclude { get; set; }
        /// <summary>La vue a cliqué sur Valider (souris).</summary>
        public bool ValidateClicked { get; set; }

        public int Count => _roster != null ? _roster.Count : 0;

        public void Open(int seat, int current, InputDevice owner)
        {
            Seat = seat;
            Owner = owner;
            Focus = Mathf.Clamp(current, 0, Mathf.Max(0, Count - 1));
            IsOpen = true;
            _openedFrame = Time.frameCount;
            _repeatAt = Time.unscaledTime + 0.2f;
            ValidateClicked = false;
        }

        public void Close()
        {
            IsOpen = false;
            Seat = -1;
        }

        public bool Choosable(int i)
        {
            CharacterDefinition c = _roster != null ? _roster[i] : null;
            return c != null && c.Selectable;
        }

        public void SetFocus(int i)
        {
            if (i >= 0 && i < Count)
                Focus = i;
        }

        /// <summary>None (rien), Cancel (Retour) ou l'indice validé.</summary>
        public int Update()
        {
            if (!IsOpen || Time.frameCount <= _openedFrame)
                return None;
            int dx = 0;
            bool ok = ValidateClicked, back = false;
            ValidateClicked = false;
            Keyboard k = Owner == null || Owner is Keyboard ? Keyboard.current : null;
            if (k != null)
            {
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.qKey.wasPressedThisFrame) dx = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dx = 1;
                ok |= k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
                back |= k.escapeKey.wasPressedThisFrame;
            }

            Gamepad g = Owner == null ? Gamepad.current : Owner as Gamepad;
            if (Owner == null && g != null && g == Exclude)
                g = null;
            if (g != null)
            {
                float x = g.leftStick.ReadValue().x + g.dpad.ReadValue().x;
                if (Mathf.Abs(x) > 0.6f && Time.unscaledTime >= _repeatAt)
                {
                    dx = x > 0f ? 1 : -1;
                    _repeatAt = Time.unscaledTime + 0.22f;
                }
                else if (Mathf.Abs(x) < 0.2f)
                    _repeatAt = 0f;
                ok |= g.buttonSouth.wasPressedThisFrame;
                back |= g.buttonEast.wasPressedThisFrame;
            }

            if (dx != 0 && Count > 0)
                Focus = (Focus + dx + Count) % Count;
            if (back)
                return Cancel;
            return ok && Choosable(Focus) ? Focus : None;
        }
    }
}
