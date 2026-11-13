using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Dimensions du labyrinthe de Test_Arena (D-035), réglables dans l'inspecteur puis *ItHunts > Reconstruire la scène
    /// Test_Arena*. Grille de 3 × 3 intersections (salles et carrefours) reliées par 12 couloirs : chaque couloir fait
    /// partie d'une boucle, pas de cul-de-sac (sauf le repaire et les pièces fermées). À l'échelle du mouvement (course
    /// 7.5 m/s, traction 18 m/s, bunny hop jusqu'à 26 m/s) et du traqueur (14 × 7 m, 9 m baissé, main à 15 m).
    /// </summary>
    [CreateAssetMenu(fileName = "ArenaLayout_Test", menuName = "ItHunts/Éditeur/Disposition de Test_Arena")]
    public sealed class ArenaLayoutSettings : ScriptableObject
    {
        [Header("Grille")]
        [Tooltip("Distance entre deux intersections voisines (m), centre à centre. 100 m : carte d'environ 250 × 250 m.")]
        [Min(60f)] public float gridSpacing = 100f;
        [Tooltip("Hauteur du niveau haut (m) : rangée nord (salle nord-ouest, carrefour nord, salle de sortie).")]
        [Min(10f)] public float upperLevel = 16f;

        [Header("Grandes salles (côté, m) et plafond (m au-dessus de leur sol)")]
        [Min(40f)] public float startHallSize = 50f;
        [Min(40f)] public float southEastHallSize = 60f;
        [Min(40f)] public float centralHallSize = 64f;
        [Min(40f)] public float northWestHallSize = 60f;
        [Min(40f)] public float exitHallSize = 50f;
        [Min(20f)] public float hallCeiling = 36f;
        [Tooltip("Carrefours : côté (m) et plafond (m).")]
        [Min(16f)] public float crossingSize = 20f;
        [Min(19f)] public float crossingCeiling = 24f;

        [Header("Couloirs")]
        [Tooltip("Largeur (12 à 16 m) et hauteur (18 à 20 m) des couloirs.")]
        [Range(12f, 16f)] public float corridorWidth = 14f;
        [Range(18f, 20f)] public float corridorHeight = 19f;
        [Tooltip("Couloirs en pente entre les niveaux (D-037) : largeur totale (m) et largeur de la voie au sol (m) qui finit au " +
                 "pied d'un mur de la hauteur du niveau haut (les goules le grimpent) ; le reste est la pente (au moins 7 m pour lui).")]
        [Range(14f, 20f)] public float slopeCorridorWidth = 16f;
        [Range(2f, 9f)] public float cliffLaneWidth = 8f;

        [Header("Ponts, corniches et poutres (le traqueur se baisse dessous)")]
        [Tooltip("Dessous des ponts et des poutres (m) : au-dessus de la hauteur minimale de passage du traqueur (9 m).")]
        [Min(9f)] public float bridgeBottom = 10f;
        [Tooltip("Largeur des ponts et profondeur des corniches (m) : moins que la largeur du traqueur (7 m), il n'y monte pas.")]
        [Range(2f, 6.9f)] public float bridgeWidth = 6f;

        [Header("Pièces fermées (le traqueur n'y entre pas, il passe le bras)")]
        [Tooltip("Côté de la pièce (m) : toute la pièce doit rester à portée de main (15 m) depuis une ouverture.")]
        [Min(6f)] public float roomSize = 12f;
        [Min(3f)] public float roomHeight = 6f;
        [Tooltip("Porte : largeur et hauteur (m), 2 à 4 m.")]
        [Range(2f, 4f)] public float roomDoorWidth = 3f;
        [Range(2f, 4f)] public float roomDoorHeight = 3f;

        [Header("Repaire du traqueur (cul-de-sac à l'est du carrefour est)")]
        [Min(16f)] public float lairWidth = 22f;
        [Min(16f)] public float lairDepth = 26f;
        [Min(15f)] public float lairHeight = 20f;

        [Header("Ennemis et objets")]
        [Tooltip("Points d'apparition des goules (vagues, D-041) : par grande salle, par couloir.")]
        [Range(0, 12)] public int ghoulsPerHall = 8;
        [Range(0, 6)] public int ghoulsPerCorridor = 3;
        [Tooltip("Points d'apparition des volants en hauteur (vagues, D-041) : par grande salle, par couloir.")]
        [Range(0, 8)] public int flyersPerHall = 4;
        [Range(0, 4)] public int flyersPerCorridor = 1;
        [Tooltip("Objets à lancer par grande salle (caisses, blocs lourds), plus un lustre.")]
        [Range(0, 30)] public int propsPerHall = 16;

        [Header("Lumière")]
        [Min(0f)] public float lightIntensity = 6f;
        [Min(5f)] public float lightRange = 50f;
        public Color ambient = new Color(0.16f, 0.16f, 0.2f);
    }
}
