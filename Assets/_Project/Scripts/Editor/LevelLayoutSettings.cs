using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Plan du niveau 1 en greybox (D-052), réglable avant de relancer le constructeur (qui n'écrit que Level_01_Base). Les
    /// dimensions des couloirs, ponts et pièces viennent de ArenaLayout_Level01 (même type que Test_Arena).
    /// </summary>
    [CreateAssetMenu(fileName = "LevelLayout_Level01", menuName = "ItHunts/Réglages/Plan de niveau (constructeur)")]
    public sealed class LevelLayoutSettings : ScriptableObject
    {
        [Header("Carte")]
        [Tooltip("Graine du tirage (types de salles, hauteurs, couloirs retirés, coffres, canaux).")]
        public int seed = 1801;
        [Tooltip("Salles sur la largeur (x) et la profondeur (z) : 7 × 7 = 49 salles.")]
        [Range(3, 10)] public int columns = 7;
        [Range(3, 10)] public int rows = 7;
        [Tooltip("Écart entre les centres de deux salles voisines (m) : 7 salles à 120 m ≈ 800 m de côté.")]
        [Range(80f, 200f)] public float spacing = 120f;

        [Header("Salles")]
        [Tooltip("Côté des grandes salles (m), tiré entre ces bornes.")]
        [Range(40f, 90f)] public float hallMin = 46f;
        [Range(40f, 90f)] public float hallMax = 66f;
        [Tooltip("Part des intersections qui sont de simples carrefours (20 m) plutôt que des salles.")]
        [Range(0f, 0.6f)] public float crossingShare = 0.2f;
        [Tooltip("Part des salles au niveau haut (+16 m) : les couloirs entre deux niveaux sont des pentes.")]
        [Range(0f, 0.6f)] public float upperShare = 0.3f;
        [Tooltip("Part des couloirs de la grille retirés (chaque salle garde au moins 2 couloirs : boucles, pas de cul-de-sac).")]
        [Range(0f, 0.4f)] public float removedCorridorShare = 0.2f;

        [Header("Habitants (étape 8, après playtest)")]
        [Tooltip("Goules posées dans chaque grande salle dès le départ (en attente, aggro habituelle), selon sa taille : le " +
                 "minimum pour la plus petite, le maximum pour la plus grande. Tuées, elles ne reviennent pas avant la remise à zéro.")]
        [Range(0, 10)] public int residentGhoulsMin = 2;
        [Range(0, 10)] public int residentGhoulsMax = 5;
        [Tooltip("Volants posés dans les salles du niveau haut.")]
        [Range(0, 5)] public int residentFlyersMin = 1;
        [Range(0, 5)] public int residentFlyersMax = 2;

        [Header("Annexes (pièces meublées le long des couloirs)")]
        [Tooltip("Probabilité qu'un couloir plat de 40 m ou plus ait une pièce annexe (chambre, cellule, réserve, cuisine, armurerie, " +
                 "chapelle, crypte) ; les premières sont les salles verrouillées.")]
        [Range(0f, 1f)] public float annexShare = 0.7f;
        [Tooltip("Largeur minimale de la voie dégagée au milieu des couloirs (m) : le décor reste le long des murs.")]
        [Range(4f, 12f)] public float minLane = 8f;

        [Header("Parcours et passages (points 3 et 4)")]
        [Tooltip("Galeries de parcours le long des couloirs plats de 55 m et plus (seul endroit où l'on dépasse le traqueur).")]
        [Range(0, 20)] public int parkourCorridors = 6;
        [Tooltip("Passages en hauteur (saut mural) entre une salle et un couloir.")]
        [Range(0, 30)] public int passages = 10;

        [Header("Contenu")]
        [Tooltip("Coffres placés dans le décor, derrière des obstacles.")]
        [Range(0, 40)] public int chests = 14;
        [Tooltip("Pièces fermées (le traqueur passe le bras) dont les salles verrouillées.")]
        [Range(0, 12)] public int sideRooms = 6;
        [Range(0, 4)] public int lockedRooms = 2;
        [Tooltip("Salles traversées par un canal d'égout (eau profonde = vide).")]
        [Range(0, 20)] public int canalHalls = 7;
        [Range(4f, 10f)] public float canalWidth = 6f;
        [Range(2f, 6f)] public float canalDepth = 4f;
        [Tooltip("Dégâts en tombant dans un canal (PV).")]
        [Range(0f, 100f)] public float waterDamage = 20f;
        [Tooltip("Salles avec une mini-zone de parcours non magnétique (D-051).")]
        [Range(0, 20)] public int parkourHalls = 6;
        [Tooltip("Couloirs avec une poutre ou un pont en hauteur (dessous à 10 m).")]
        [Range(0f, 1f)] public float corridorFeatureShare = 0.35f;
    }
}
