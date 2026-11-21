using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Un artefact (D-022, D-032) : arme ou pouvoir de combat porté en main gauche. Un asset par artefact, qui hérite de
    /// cette classe et porte ses propres réglages (tir, munitions, rechargement). Le tirage au début de la run se fait
    /// dans le pool (ArtifactPool), sans doublon entre les joueurs.
    /// </summary>
    public abstract class ArtifactDefinition : ScriptableObject
    {
        [Tooltip("Identifiant stable (sauvegarde, réseau) : ne pas le changer une fois publié.")]
        public string id = "artefact";
        [Tooltip("Nom affiché.")]
        public string displayName = "Artefact";
        [Tooltip("Description courte, affichée sous le nom sur l'écran de choix du personnage (UI-HANDOFF § 2.5).")]
        [TextArea(2, 3)] public string description = "";
        [Tooltip("Améliorations de cet artefact, vendues par le marchand de l'artefact (salle de départ) au joueur qui le porte.")]
        public UpgradePool upgrades;
        [Tooltip("Modèle en vue FPS (main gauche, flottant) pour les artefacts sans vue dédiée (épée en os, livre à lien) ; " +
                 "vide : forme provisoire.")]
        public GameObject viewModel;
        [Tooltip("Position (repère de la main), rotation et taille du modèle en vue FPS.")]
        public Vector3 viewOffset = new Vector3(0f, 0.12f, 0.06f);
        public Vector3 viewEuler = Vector3.zero;
        [Min(0.01f)] public float viewScale = 1f;
    }
}
