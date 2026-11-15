using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Kit de construction des salles et des couloirs (carte aléatoire) : un prefab par pièce, aux cotes d'ART-HANDOFF. Toute la
    /// structure des modules et de la carte (murs, sols, plafonds, piliers, arcs, portes, ponts, poutres, escaliers) est faite
    /// d'instances de ces prefabs : remplacer le prefab par le vrai modèle met à jour toute la carte d'un coup. Les pièces du kit
    /// sont visuelles ; la collision est une boîte fusionnée par volume (KitBuild), pour garder la physique et le NavMesh légers.
    /// Pivots : murs au pied, au milieu (épaisseur centrée) ; sols et plafonds au milieu du dessus ; pièces des modèles livrés :
    /// voir le manifeste.
    /// </summary>
    [CreateAssetMenu(fileName = "Kit_Niveau1", menuName = "ItHunts/Niveaux/Kit de construction")]
    public sealed class KitSet : ScriptableObject
    {
        [Header("Pièces de base (4 m)")]
        [Tooltip("Mur de 4 × 4 m, 1 m d'épaisseur (pivot au pied, au milieu).")]
        public GameObject mur4x4;
        [Tooltip("Dalle de sol (ou de plafond) de 4 × 4 m, 1 m d'épaisseur (pivot au milieu du dessus).")]
        public GameObject sol4;
        [Tooltip("Marche d'escalier de 4 m de large, 0.25 m de haut, 1 m de profondeur (pivot au milieu du bas de la contremarche) ; étirée en profondeur selon la pente.")]
        public GameObject escalier;
        [Tooltip("Tronçon de pont (passerelle) de 4 m de long, 3 m de large, tablier de 1 m (pivot au milieu du dessus).")]
        public GameObject pont;

        [Header("Modèles livrés par la DA")]
        [Tooltip("Pilier / colonne (SM_Colonne : 1.4 × 8 × 1.4 m, pivot au sol).")]
        public GameObject pilier;
        [Tooltip("Arc de couloir (SM_Arc_Couloir : 14 × 11 × 1.6 m, pivot au sol au milieu).")]
        public GameObject arc;
        [Tooltip("Encadrement de porte de pièce (SM_Porte_Piece : 4.4 × 5 × 1 m, ouverture 3 × 3 m, pivot au sol au centre).")]
        public GameObject portePiece;
        [Tooltip("Grande porte de sortie (SM_GrandePorte : cadre 13 × 14 × 2 m, passage 10 × 10 m).")]
        public GameObject grandePorte;
        [Tooltip("Poutre (SM_Poutre : 6 × 0.5 × 0.5 m, pivot au milieu du dessous).")]
        public GameObject poutre;
        [Tooltip("Pont d'égout de 4 m (SM_Pont_Egout_4m : 8 × 4.6 × 4 m, pivot au centre du tablier).")]
        public GameObject pontEgout4;
        [Tooltip("Pont d'égout de 14 m (SM_Pont_Egout_14m : 8 × 4.6 × 14 m).")]
        public GameObject pontEgout14;

        [Header("Divers")]
        [Tooltip("Torche murale (décor des couloirs).")]
        public GameObject torche;
    }
}
