using UnityEngine;

namespace ItHunts.Level
{
    public enum ModuleKind
    {
        Salle,
        Carrefour,
    }

    public enum ModuleLevel
    {
        Tous,
        Sol,
        Haut,
    }

    public enum ModuleHole
    {
        Aucun,
        /// <summary>Canal d'égout le long de x (eau profonde : retour sur la rive, dégâts), ponts posés par le générateur.</summary>
        Canal,
        /// <summary>Fosse vide (zone de parcours) : chute = retour au bord, dégâts.</summary>
        Vide,
    }

    /// <summary>
    /// Module de salle (étape 8, après playtest) : l'intérieur d'une salle carrée (son côté : size), fait à la main en prefab,
    /// que le générateur de niveau pose au centre d'une salle (tourné d'un quart de tour au hasard sauf canal). Murs, sol,
    /// plafond, ouvertures des couloirs (au milieu de chaque mur, 14 à 16 m de large, 19 m de haut) et volume de salle sont faits
    /// par le générateur. Le module apporte décor, étages, ponts, objets à lancer, lumières, points d'apparition (SpawnPoint),
    /// emplacements des habitants (ResidentSpot) et des coffres (ChestSpot). Règles : voir la fiche run et niveaux.
    /// </summary>
    public sealed class RoomModule : MonoBehaviour
    {
        [Tooltip("Nom affiché (debug, plan).")]
        public string displayName = "Salle";
        public ModuleKind kind = ModuleKind.Salle;
        [Tooltip("Côté de la salle (m) : 48, 56 ou 64 pour une salle, 20 pour un carrefour.")]
        [Min(10f)] public float size = 56f;
        [Tooltip("Niveau où le module peut aller (sol, haut à +16 m, ou les deux).")]
        public ModuleLevel level = ModuleLevel.Tous;
        [Tooltip("Poids du tirage (0 : jamais tiré).")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Le générateur peut le tourner d'un quart de tour (pas les modules à canal).")]
        public bool rotate = true;

        [Header("Sol percé")]
        public ModuleHole hole = ModuleHole.Aucun;
        [Tooltip("Rectangle du trou dans le sol (local : x min, z min, largeur, profondeur en z).")]
        public Rect holeRect = new Rect(-8f, 8f, 16f, 6f);
        [Min(1f)] public float holeDepth = 6f;

        [Header("Circulation")]
        [Tooltip("Demi-largeur des voies dégagées en croix entre les ouvertures (m) : rien sous 10 m de haut (le traqueur passe).")]
        [Min(2f)] public float laneHalfWidth = 5f;
    }
}
