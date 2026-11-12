using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Réglages communs à tous les ennemis (étape 6, D-033) : PV, masse, recul, étourdissement, dégâts contre un mur.
    /// Chaque ennemi a son asset, d'une classe qui hérite de celle-ci (factice, goule, traqueur…). Modifiable en Play Mode.
    /// </summary>
    public abstract class EnemySettings : ScriptableObject
    {
        [Header("PV")]
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Réapparition après la mort (s), avec tous ses PV (factices ; les ennemis d'une zone ne réapparaissent pas).")]
        [Min(0f)] public float respawnDelay = 3f;

        [Header("Recul (répulsion) et étourdissement (D-027)")]
        [Tooltip("Masse (kg) : plus lourd, moins repoussé (onde de répulsion) ; tenu, c'est un objet de cette masse.")]
        [Min(0.1f)] public float mass = 80f;
        [Tooltip("Gravité pendant le recul et les sauts (m/s²), comme le joueur.")]
        [Min(0f)] public float gravity = 20f;
        [Tooltip("Freinage au sol pendant le recul (m/s²).")]
        [Min(0f)] public float groundFriction = 15f;
        [Tooltip("Percuter un mur ou un obstacle solide au-dessus de cette vitesse (m/s, vers le mur) étourdit.")]
        [Min(0f)] public float stunImpactSpeed = 6f;
        [Tooltip("Durée de l'étourdissement (s). Étourdi, il peut être attrapé par la télékinésie (goule : 1.5 s).")]
        [Min(0f)] public float stunDuration = 1.5f;
        [Tooltip("Dégâts contre un mur d'un ennemi repoussé (D-031) : PV par m/s au-delà de stunImpactSpeed, une fois par " +
                 "impact (avec l'étourdissement, donc pas pendant l'immunité).")]
        [Min(0f)] public float wallDamagePerSpeed = 5f;
        [Tooltip("Plafond des dégâts contre un mur, par impact (PV).")]
        [Min(0f)] public float wallDamageMax = 40f;
        [Tooltip("Immunité après la fin d'un étourdissement (s) : pas de nouvel étourdissement pendant ce temps.")]
        [Min(0f)] public float stunImmunity = 2f;
        [Tooltip("Retour au poste après un recul (factices) : attente (s) puis vitesse de marche (m/s).")]
        [Min(0f)] public float returnHomeDelay = 2f;
        [Min(0f)] public float returnHomeSpeed = 3f;
    }
}
