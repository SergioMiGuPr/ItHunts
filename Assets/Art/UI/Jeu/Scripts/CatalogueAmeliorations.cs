using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Art.UIJeu
{
    /// <summary>
    /// Catalogue des illustrations d'améliorations (nuit 4, lots 5 et 6 ; nuit 5, lot 2) : identifiant de docs/design/AMELIORATIONS.md →
    /// sprite ; cadres **par personnage** (couleur de sa lanterne ; génériques en fer neutre), carrés pour les améliorations,
    /// losanges pour les passifs ; le rang se lit aux **étoiles** (1 à 5). Les anciens cadres de rang (Bronze à Mythique) restent
    /// pour les prefabs qui ne sont pas encore passés aux couleurs.
    /// Personnages : même ordre que <c>ItHunts.Settings.UpgradeCharacter</c> de main (0 Aucun/générique, 1 Renard, 2 Caméléon,
    /// 3 Koala, 4 Hibou). Asset : Assets/Art/UI/Ameliorations/Catalogue_Ameliorations.asset (UIJeu5Builder).
    /// </summary>
    [CreateAssetMenu(menuName = "ItHunts/Art/Catalogue des améliorations")]
    public sealed class CatalogueAmeliorations : ScriptableObject
    {
        [Serializable] public struct Entree { public string id; public Sprite icone; }

        /// <summary>Texte d'une amélioration pour les aperçus de la DA (infobulle, codex) : tiré de AMELIORATIONS.md.</summary>
        [Serializable]
        public sealed class Fiche
        {
            public string id;
            public string nom;
            [TextArea] public string effet;
            [Tooltip("Bronze, Argent, Or, Diamant, Mythique.")] public string[] rangs = new string[5];
            [TextArea] public string synergies;
        }

        public Entree[] entrees = new Entree[0];
        [Tooltip("Anciens cadres de rang : Bronze, Argent, Or, Diamant, Mythique.")] public Sprite[] cadres = new Sprite[5];
        public Sprite[] cadresPassifs = new Sprite[5];
        public Sprite masquePassif;

        [Header("Nuit 5 : cadres par personnage, rang en étoiles")]
        [Tooltip("Générique (fer), Renard, Caméléon, Koala, Hibou : cadre carré (amélioration).")]
        public Sprite[] cadresPersonnage = new Sprite[5];
        [Tooltip("Même ordre : cadre losange (passif).")]
        public Sprite[] cadresPassifsPersonnage = new Sprite[5];
        [Tooltip("Étoile de rang (or vieilli) et emplacement éteint.")]
        public Sprite etoile, etoileVide;
        [Tooltip("Couleur de chaque personnage (lanterne) : onglets, liserés, lueurs. Générique : fer pâle.")]
        public Color[] couleursPersonnage =
        {
            new Color(0.80f, 0.78f, 0.74f), new Color(0.867f, 0.216f, 0.247f), new Color(0.902f, 0.667f, 0.180f),
            new Color(0.333f, 0.784f, 0.318f), new Color(0.373f, 0.725f, 0.929f),
        };
        public Fiche[] fiches = new Fiche[0];

        public static readonly string[] Rangs = { "Bronze", "Argent", "Or", "Diamant", "Mythique" };
        public static readonly string[] Personnages = { "Communes", "Renard", "Caméléon", "Koala", "Hibou" };

        private Dictionary<string, Sprite> _index;
        private Dictionary<string, Fiche> _fiches;

        public Sprite Icone(string id)
        {
            if (_index == null)
            {
                _index = new Dictionary<string, Sprite>();
                foreach (var e in entrees) if (!string.IsNullOrEmpty(e.id)) _index[e.id] = e.icone;
            }
            return id != null && _index.TryGetValue(id, out var s) ? s : null;
        }

        public Fiche FicheDe(string id)
        {
            if (_fiches == null)
            {
                _fiches = new Dictionary<string, Fiche>();
                foreach (var f in fiches) if (f != null && !string.IsNullOrEmpty(f.id)) _fiches[f.id] = f;
            }
            return id != null && _fiches.TryGetValue(id, out var r) ? r : null;
        }

        /// <summary>Passifs : identifiants « X-P1 », « X-P2 ».</summary>
        public bool EstPassif(string id) => id != null && id.Contains("-P");

        /// <summary>Personnage propriétaire d'après l'identifiant : G → 0 (générique), R → 1, C → 2, K → 3, H → 4 ; inconnu → 0.</summary>
        public static int Proprietaire(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            switch (char.ToUpperInvariant(id[0]))
            {
                case 'R': return 1;
                case 'C': return 2;
                case 'K': return 3;
                case 'H': return 4;
                default: return 0;
            }
        }

        public Sprite CadrePersonnage(int proprietaire, bool passif)
        {
            var jeu = passif ? cadresPassifsPersonnage : cadresPersonnage;
            if (jeu == null || jeu.Length == 0) return null;
            return jeu[Mathf.Clamp(proprietaire, 0, jeu.Length - 1)];
        }

        public Color Couleur(int proprietaire) =>
            couleursPersonnage != null && couleursPersonnage.Length > 0 ? couleursPersonnage[Mathf.Clamp(proprietaire, 0, couleursPersonnage.Length - 1)] : Color.white;
    }
}
