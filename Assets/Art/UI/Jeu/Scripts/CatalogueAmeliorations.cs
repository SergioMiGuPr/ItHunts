using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Catalogue des illustrations d'améliorations (nuit 4, lots 5 et 6) : identifiant de docs/design/AMELIORATIONS.md →
    /// sprite, et les 10 cadres de rang (carrés pour les améliorations, losanges pour les passifs ; Bronze à Mythique).
    /// Asset : Assets/Art/UI/Ameliorations/Catalogue_Ameliorations.asset (construit par UIJeu4Builder).
    /// </summary>
    [CreateAssetMenu(menuName = "ItHunts/Art/Catalogue des améliorations")]
    public sealed class CatalogueAmeliorations : ScriptableObject
    {
        [Serializable] public struct Entree { public string id; public Sprite icone; }

        public Entree[] entrees = new Entree[0];
        [Tooltip("Bronze, Argent, Or, Diamant, Mythique.")] public Sprite[] cadres = new Sprite[5];
        public Sprite[] cadresPassifs = new Sprite[5];
        public Sprite masquePassif;
        public static readonly string[] Rangs = { "Bronze", "Argent", "Or", "Diamant", "Mythique" };

        private Dictionary<string, Sprite> _index;

        public Sprite Icone(string id)
        {
            if (_index == null)
            {
                _index = new Dictionary<string, Sprite>();
                foreach (var e in entrees) if (!string.IsNullOrEmpty(e.id)) _index[e.id] = e.icone;
            }
            return id != null && _index.TryGetValue(id, out var s) ? s : null;
        }

        /// <summary>Passifs : identifiants « X-P1 », « X-P2 ».</summary>
        public bool EstPassif(string id) => id != null && id.Contains("-P");
    }
}
