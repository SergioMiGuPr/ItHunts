using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Les personnages, dans l'ordre du menu (D-064) : renard, caméléon, koala, hibou. defaultIndex : personnage d'un joueur qui
    /// n'a rien choisi (scène de test ouverte directement) ; les suivants prennent le premier personnage jouable libre.
    /// </summary>
    [CreateAssetMenu(fileName = "Characters_Default", menuName = "ItHunts/Personnages/Liste des personnages")]
    public sealed class CharacterRoster : ScriptableObject
    {
        public List<CharacterDefinition> characters = new List<CharacterDefinition>();
        [Tooltip("Personnage du joueur 1 s'il n'a rien choisi (le caméléon et son barillet : les scènes de test restent comme avant).")]
        [Min(0)] public int defaultIndex;

        public int Count => characters.Count;
        public CharacterDefinition this[int i] => i >= 0 && i < characters.Count ? characters[i] : null;

        /// <summary>Jouabilité de chaque personnage (pour CharacterPick).</summary>
        public bool[] Selectable()
        {
            var r = new bool[characters.Count];
            for (int i = 0; i < r.Length; i++)
                r[i] = characters[i] != null && characters[i].Selectable;
            return r;
        }
    }
}
