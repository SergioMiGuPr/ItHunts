using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Ennemis factices de la zone de test : PV, réapparition, cible mobile. Les vrais ennemis auront leurs propres
    /// réglages (étape 6). Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "TrainingDummy_Default", menuName = "ItHunts/Réglages/Ennemi factice")]
    public sealed class TrainingDummySettings : ScriptableObject
    {
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Réapparition après la mort (s), avec tous ses PV.")]
        [Min(0f)] public float respawnDelay = 3f;
        [Tooltip("Cible mobile : vitesse de ses allers-retours (m/s).")]
        [Min(0f)] public float moveSpeed = 6f;
        [Tooltip("Cible mobile : pause à chaque bout (s).")]
        [Min(0f)] public float pauseAtEnds = 0.3f;

        [Header("Recul (répulsion) et étourdissement (D-027)")]
        [Tooltip("Masse (kg) : plus lourd, moins repoussé (onde de répulsion).")]
        [Min(0.1f)] public float mass = 80f;
        [Tooltip("Gravité pendant le recul (m/s²), comme le joueur.")]
        [Min(0f)] public float gravity = 20f;
        [Tooltip("Freinage au sol pendant le recul (m/s²).")]
        [Min(0f)] public float groundFriction = 15f;
        [Tooltip("Percuter un mur ou un obstacle solide au-dessus de cette vitesse (m/s, vers le mur) étourdit.")]
        [Min(0f)] public float stunImpactSpeed = 6f;
        [Tooltip("Durée de l'étourdissement (s). Un ennemi étourdi pourra être attrapé par la télékinésie (goule : 1.5 s).")]
        [Min(0f)] public float stunDuration = 1.5f;
        [Tooltip("Retour au poste après un recul : attente (s) puis vitesse de marche (m/s).")]
        [Min(0f)] public float returnHomeDelay = 2f;
        [Min(0f)] public float returnHomeSpeed = 3f;
    }
}
