using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Épée en os du koala (point 5 de la nuit du 2026-11-21, D-077) : corps à corps en main gauche, clic gauche sans objet tenu.
    /// Combo de 3 coups (le 3e plus fort, léger recul des ennemis), dégâts qui montent avec la vitesse du joueur (plafonnés),
    /// fente tranchante qui traverse la cible pendant un dash ou une glissade. Les coups ne touchent jamais au mouvement.
    /// Le traqueur est intouchable (D-035). Valeurs de départ : dégâts par seconde du même ordre que le barillet (≈ 29 PV/s).
    /// </summary>
    [CreateAssetMenu(fileName = "BoneSword_Default", menuName = "ItHunts/Artefacts/Épée en os")]
    public sealed class BoneSwordSettings : ArtifactDefinition
    {
        [Header("Combo")]
        [Tooltip("Dégâts des 3 coups du combo (PV), à la vitesse de course.")]
        public float[] comboDamage = { 10f, 10f, 16f };
        [Tooltip("Délai minimal entre deux coups (s).")]
        [Min(0.05f)] public float swingInterval = 0.3f;
        [Tooltip("Après le 3e coup, pause en plus avant le coup suivant (s).")]
        [Min(0f)] public float comboRecovery = 0.25f;
        [Tooltip("Au-delà de ce temps (s) sans frapper, le combo repart du 1er coup.")]
        [Min(0.1f)] public float comboWindow = 0.8f;
        [Tooltip("Un clic un peu trop tôt part dès que possible (s).")]
        [Min(0f)] public float pressBuffer = 0.2f;

        [Header("Portée et visée")]
        [Tooltip("Portée du coup (m) depuis les yeux.")]
        [Min(0.5f)] public float range = 2.5f;
        [Tooltip("Demi-angle de l'arc touché (°) : aide à la visée légère.")]
        [Range(5f, 90f)] public float arcHalfAngle = 40f;

        [Header("Vitesse : plus fort en pleine vitesse")]
        [Tooltip("Vitesse (m/s) sous laquelle les dégâts sont ceux de base.")]
        [Min(0f)] public float speedRef = 8.5f;
        [Tooltip("Vitesse (m/s) où le bonus est au maximum.")]
        [Min(0.1f)] public float speedFull = 20f;
        [Tooltip("Multiplicateur maximal des dégâts (plafond).")]
        [Min(1f)] public float speedMaxMultiplier = 1.8f;

        [Header("3e coup : recul")]
        [Tooltip("Vitesse de recul donnée aux ennemis touchés par le 3e coup (m/s).")]
        [Min(0f)] public float knockback = 6f;

        [Header("Fente (pendant un dash ou une glissade)")]
        [Tooltip("Longueur de la fente devant le joueur, dans le sens de son déplacement (m).")]
        [Min(0.5f)] public float lungeLength = 4.5f;
        [Tooltip("Rayon de la fente (m).")]
        [Min(0.1f)] public float lungeRadius = 1f;
        [Tooltip("Dégâts de la fente : ceux du 3e coup × ce facteur (avant le bonus de vitesse).")]
        [Min(0f)] public float lungeDamageFactor = 1.2f;

        [Tooltip("Calques touchés.")]
        public LayerMask hitMask = ~0;

        [Header("Vue (provisoire : animation procédurale du coup)")]
        [Tooltip("Durée de l'animation d'un coup (s).")]
        [Min(0.05f)] public float swingAnimDuration = 0.22f;
    }
}
