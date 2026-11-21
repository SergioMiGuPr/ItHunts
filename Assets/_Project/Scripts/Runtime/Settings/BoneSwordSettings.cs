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
        [Tooltip("Portée du coup (m) depuis les yeux (2026-11-21 : 2.5 → 3.2, corps à corps plus facile).")]
        [Min(0.5f)] public float range = 3.2f;
        [Tooltip("Demi-angle de l'arc touché (°) (2026-11-21 : 40 → 55).")]
        [Range(5f, 90f)] public float arcHalfAngle = 55f;
        [Tooltip("Délai entre le clic et l'impact (s) : l'impact tombe quand la lame passe devant (animation du coup).")]
        [Min(0f)] public float strikeDelay = 0.06f;

        [Header("Aide au corps à corps (2026-11-21)")]
        [Tooltip("Cible visée : ennemi à moins de assistRange (m) et de assistAngle (°) du viseur, visible. Le coup part vers elle.")]
        [Min(0f)] public float assistRange = 6f;
        [Range(0f, 90f)] public float assistAngle = 35f;
        [Tooltip("La vue tourne vers la cible de cette part de l'angle, au plus assistMaxTurn (°), au départ du coup.")]
        [Range(0f, 1f)] public float assistTurn = 0.35f;
        [Range(0f, 30f)] public float assistMaxTurn = 10f;
        [Tooltip("Petit élan vers la cible (m/s) : vitesse vers elle portée au moins à cette valeur, jamais de perte de vitesse ; " +
                 "seulement si elle est à plus de lungeMinDistance (m). 0 : aucun.")]
        [Min(0f)] public float lungeSpeed = 5f;
        [Min(0f)] public float lungeMinDistance = 1.6f;

        [Header("Sensations à l'impact (arrêt sur image : GameFeel_Default.swordHitStop)")]
        [Range(0f, 1f)] public float hitTrauma = 0.12f;
        [Range(0f, 1f)] public float thirdHitTrauma = 0.22f;
        [Tooltip("3e coup : arrêt sur image plus long (× ce facteur).")]
        [Min(1f)] public float thirdHitStopFactor = 1.4f;

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

        [Header("Vue : l'épée flottante frappe (mains gelées) : taille horizontale, diagonale, estoc")]
        [Tooltip("Durée de l'animation d'un coup (s).")]
        [Min(0.05f)] public float swingAnimDuration = 0.26f;
        [Tooltip("Traînée de lame : matériau de la DA (trace peinte), largeur (m), part de l'arc couverte.")]
        public Material trailMaterial;
        [Min(0.005f)] public float trailWidth = 0.045f;
        [Range(0.05f, 1f)] public float trailLength = 0.45f;
        [ColorUsage(false, true)] public Color trailTone = new Color(0.85f, 0.82f, 0.72f, 1f);
    }
}
