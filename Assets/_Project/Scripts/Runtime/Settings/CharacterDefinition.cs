using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Un personnage (D-064) : choisi avant la run, il apporte son artefact (main gauche) et sa couleur (l'orbe la gardera en
    /// permanence avec l'orbe v2, D-066). En coop, chaque joueur prend un personnage différent. Un asset par personnage, listé
    /// dans CharacterRoster.
    /// </summary>
    [CreateAssetMenu(fileName = "Character_", menuName = "ItHunts/Personnages/Personnage")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Identifiant stable (sauvegarde, réseau) : ne pas le changer une fois publié.")]
        public string id = "personnage";
        [Tooltip("Nom affiché (menu).")]
        public string displayName = "Personnage";
        [Tooltip("Couleur du personnage (menu ; plus tard, couleur permanente de son orbe, D-066).")]
        public Color color = Color.white;
        [Tooltip("Couleur de la lumière « lanterne » portée par le joueur (D-067) : plus sombre que la couleur du personnage.")]
        public Color lanternColor = new Color(0.62f, 0.16f, 0.07f);
        [Tooltip("Son artefact (main gauche). Vide : artefact à concevoir (le personnage n'est pas jouable).")]
        public ArtifactDefinition artifact;
        [Tooltip("Nom de l'artefact affiché quand il n'existe pas encore.")]
        public string artifactLabel = "artefact à concevoir";
        [Tooltip("Jouable : choisissable dans le menu. Décoché : affiché grisé (« bientôt »).")]
        public bool playable = true;

        public bool Selectable => playable && artifact != null;
        public string ArtifactName => artifact != null ? artifact.displayName : artifactLabel;
    }
}
