using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Campement (menus, logique pure et testable) : 4 places autour du feu ; le joueur 1 (place 0) prend le dernier personnage joué
    /// (enregistré) ; en écran partagé, Start sur une autre manette fait rejoindre un joueur à la première place libre (dans la limite
    /// des joueurs locaux), Start de nouveau le retire. Chacun choisit son personnage et se met Prêt. Le même personnage peut être
    /// choisi par deux joueurs, mais Prêt (et Jouer) est refusé tant qu'il y a un doublon (UI-HANDOFF, décision du 2026-11-17).
    /// </summary>
    public sealed class CampLobby
    {
        public const int Seats = 4;
        public const string LastCharacterKey = "ItHunts.DernierPersonnage";

        public sealed class Seat
        {
            /// <summary>Appareil du joueur (null pour le joueur 1 : clavier et première manette).</summary>
            public object Device;
            public int Character = -1;
            public bool Ready;
        }

        private readonly Seat[] _seats = new Seat[Seats];
        private readonly bool[] _selectable;

        /// <summary>Joueurs locaux au plus (écran partagé à 2 pour l'instant ; les places 3 et 4 attendent le jeu en ligne).</summary>
        public int MaxLocal { get; }

        public CampLobby(bool[] selectable, int firstCharacter, int maxLocal = 2)
        {
            _selectable = selectable ?? new bool[0];
            MaxLocal = Mathf.Clamp(maxLocal, 1, Seats);
            _seats[0] = new Seat { Character = Valid(firstCharacter) ? firstCharacter : FirstSelectable() };
        }

        public Seat this[int i] => i >= 0 && i < Seats ? _seats[i] : null;
        public bool Occupied(int i) => this[i] != null;
        public int Count
        {
            get
            {
                int n = 0;
                foreach (Seat s in _seats)
                    if (s != null)
                        n++;
                return n;
            }
        }

        public int SeatOf(object device)
        {
            for (int i = 1; i < Seats; i++)
                if (_seats[i] != null && Equals(_seats[i].Device, device))
                    return i;
            return -1;
        }

        /// <summary>Un joueur rejoint avec cet appareil : première place libre ; −1 si c'est complet ou s'il est déjà assis.</summary>
        public int Join(object device)
        {
            if (device == null || SeatOf(device) >= 0 || Count >= MaxLocal)
                return -1;
            for (int i = 1; i < Seats; i++)
                if (_seats[i] == null)
                {
                    _seats[i] = new Seat { Device = device, Character = FirstFree() };
                    return i;
                }

            return -1;
        }

        /// <summary>Il se retire (sa place se libère).</summary>
        public bool Leave(object device)
        {
            int i = SeatOf(device);
            if (i < 1)
                return false;
            _seats[i] = null;
            return true;
        }

        public bool Valid(int character) => character >= 0 && character < _selectable.Length && _selectable[character];

        /// <summary>Change le personnage d'une place (jouable seulement) ; son Prêt tombe.</summary>
        public bool SetCharacter(int seat, int character)
        {
            Seat s = this[seat];
            if (s == null || !Valid(character))
                return false;
            s.Character = character;
            s.Ready = false;
            return true;
        }

        /// <summary>Ce personnage est-il aussi pris par une autre place ?</summary>
        public bool Duplicate(int seat)
        {
            Seat s = this[seat];
            if (s == null)
                return false;
            for (int i = 0; i < Seats; i++)
                if (i != seat && _seats[i] != null && _seats[i].Character == s.Character)
                    return true;
            return false;
        }

        /// <summary>Qui a déjà ce personnage (places autres que celle-ci), pour le griser dans le choix.</summary>
        public List<int> TakenBy(int character, int exceptSeat)
        {
            var who = new List<int>();
            for (int i = 0; i < Seats; i++)
                if (i != exceptSeat && _seats[i] != null && _seats[i].Character == character)
                    who.Add(i);
            return who;
        }

        public bool CanReady(int seat) => this[seat] != null && Valid(this[seat].Character) && !Duplicate(seat);

        /// <summary>Prêt / plus prêt ; refusé tant que le personnage est en double.</summary>
        public bool ToggleReady(int seat)
        {
            Seat s = this[seat];
            if (s == null)
                return false;
            if (s.Ready)
            {
                s.Ready = false;
                return true;
            }

            if (!CanReady(seat))
                return false;
            s.Ready = true;
            return true;
        }

        /// <summary>Jouer : en solo, tout de suite (pas de Prêt) ; en coop, tout le monde prêt, sans doublon.</summary>
        public bool CanLaunch => Blocker(null) == null;

        /// <summary>
        /// Pourquoi Jouer est refusé (null : on peut jouer) : un personnage pris deux fois (« Caméléon pris deux fois »), puis un joueur
        /// pas prêt en coop (« J2 n'est pas prêt »).
        /// </summary>
        public string Blocker(System.Func<int, string> characterName)
        {
            if (Count == 0)
                return "Personne au campement";
            for (int i = 0; i < Seats; i++)
                if (_seats[i] != null && Duplicate(i))
                    return $"{characterName?.Invoke(_seats[i].Character) ?? "Un personnage"} pris deux fois";
            for (int i = 0; i < Seats; i++)
                if (_seats[i] != null && !Valid(_seats[i].Character))
                    return $"J{i + 1} n'a pas de personnage jouable";
            if (Count > 1)
                for (int i = 0; i < Seats; i++)
                    if (_seats[i] != null && !_seats[i].Ready)
                        return $"J{i + 1} n'est pas prêt";
            return null;
        }

        private int FirstSelectable()
        {
            for (int i = 0; i < _selectable.Length; i++)
                if (_selectable[i])
                    return i;
            return -1;
        }

        /// <summary>Premier personnage jouable que personne n'a (sinon le premier jouable : un doublon, à changer).</summary>
        private int FirstFree()
        {
            for (int i = 0; i < _selectable.Length; i++)
                if (_selectable[i] && TakenBy(i, -1).Count == 0)
                    return i;
            return FirstSelectable();
        }

        // ---------- Dernier personnage joué (joueur 1) ----------

        public static void SaveLast(string characterId)
        {
            if (string.IsNullOrEmpty(characterId))
                return;
            PlayerPrefs.SetString(LastCharacterKey, characterId);
            PlayerPrefs.Save();
        }

        public static string LoadLast() => PlayerPrefs.GetString(LastCharacterKey, string.Empty);
    }
}
