using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Réglages de la carte aléatoire d'un niveau (étape 8) : taille de la grille, règles du tirage et cotes de la coque. Un asset
    /// par niveau (MapLayout_Niveau1…). Le tirage ne dépend que de la seed (System.Random, ordre fixe) : chaque machine refait la
    /// même carte (jeu en ligne).
    /// </summary>
    [CreateAssetMenu(fileName = "MapLayout_Niveau1", menuName = "ItHunts/Niveaux/Réglages de carte aléatoire")]
    public sealed class MapLayoutSettings : ScriptableObject
    {
        [Header("Grille")]
        [Tooltip("Cases sur la largeur (x) et la profondeur (z) : 7 × 7 = 49 cases pour le niveau 1.")]
        [Range(3, 12)] public int columns = 7;
        [Range(3, 12)] public int rows = 7;
        [Tooltip("Pas de la grille (m). Doit valoir RoomModule.GridSpacing (120 m = 30 pièces de 4 m).")]
        public float spacing = RoomModule.GridSpacing;

        [Header("Tirage")]
        [Tooltip("Part des cases qui sont de simples carrefours (20 m) plutôt que des salles.")]
        [Range(0f, 0.6f)] public float crossingShare = 0.2f;
        [Tooltip("Salles de plusieurs cases (1 × 2, 2 × 2) par carte, au plus.")]
        [Range(0, 10)] public int bigRooms = 3;
        [Tooltip("Part des couloirs de la grille retirés (chaque couloir reste dans une boucle : graphe sans pont).")]
        [Range(0f, 0.5f)] public float removedCorridorShare = 0.2f;
        [Tooltip("Distance minimale départ – sortie, en salles traversées (plus court chemin). La sortie est tirée au hasard parmi les salles assez éloignées.")]
        [Range(1, 20)] public int minStartExitRooms = 6;
        [Tooltip("Inclinaison maximale d'une pente de liaison entre deux salles (degrés). Les descentes de glissade dédiées des modules gardent leur angle.")]
        [Range(5f, 35f)] public float maxSlope = 20f;
        [Tooltip("Hauteur du niveau haut (m) : seulement pour les modules qui le déclarent (niveau Haut).")]
        [Min(4f)] public float upperLevel = 16f;
        [Tooltip("Probabilité qu'un module qui accepte les deux niveaux soit posé en haut (0 : jamais ; seuls les modules « Haut » montent).")]
        [Range(0f, 1f)] public float upperShareForBoth;
        [Tooltip("Essais de tirage (sous-seeds) avant la carte de secours.")]
        [Range(1, 50)] public int maxAttempts = 20;

        [Header("Contenu")]
        [Range(0, 40)] public int chests = 14;
        [Range(0, 20)] public int canalHalls = 7;
        [Tooltip("Salles avec une zone de parcours (fosse, non magnétique).")]
        [Range(0, 20)] public int parkourHalls = 3;
        [Tooltip("Probabilité qu'un couloir plat de 40 m ou plus ait une pièce annexe (les premières sont les salles verrouillées).")]
        [Range(0f, 1f)] public float annexShare = 0.35f;
        [Range(0, 4)] public int lockedRooms = 2;
        [Range(0, 10)] public int residentGhoulsMin = 1;
        [Range(0, 10)] public int residentGhoulsMax = 3;
        [Range(0, 5)] public int residentFlyersMin = 1;
        [Range(0, 5)] public int residentFlyersMax = 1;
        [Tooltip("Points d'apparition des goules et des volants par couloir.")]
        [Range(0, 6)] public int ghoulSpawnsPerCorridor = 2;
        [Range(0, 4)] public int flyerSpawnsPerCorridor = 1;
        [Tooltip("Probabilité qu'un couloir plat ait une poutre ou un pont en hauteur (dessous à 10 m).")]
        [Range(0f, 1f)] public float corridorFeatureShare = 0.35f;

        [Header("Coque (cotes d'ART-HANDOFF)")]
        public float startHallSize = 56f;
        public float hallCeiling = 36f;
        public float crossingCeiling = 24f;
        public float corridorWidth = 14f;
        [Tooltip("Largeur d'un couloir en pente (m).")]
        public float slopeCorridorWidth = 16f;
        public float corridorHeight = 19f;
        [Tooltip("Longueur d'un tronçon de couloir (volume de salle, D-047).")]
        public float corridorSegmentLength = 40f;
        public float lairDepth = 26f;
        public float lairWidth = 20f;
        public float lairHeight = 20f;
        public float bridgeBottom = 10f;
        public float bridgeWidth = 6f;
        public float canalWidth = 6f;
        public float canalDepth = 4f;
        public float waterDamage = 20f;
        public float lightIntensity = 6f;
        public float lightRange = 50f;
    }
}
