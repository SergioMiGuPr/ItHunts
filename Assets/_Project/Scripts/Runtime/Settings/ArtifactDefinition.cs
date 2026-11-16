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
        [Tooltip("Améliorations de cet artefact, vendues par le marchand de l'artefact (salle de départ) au joueur qui le porte.")]
        public UpgradePool upgrades;
    }
}
