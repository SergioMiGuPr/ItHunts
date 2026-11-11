using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Réglages du mouvement du joueur. Les valeurs par défaut viennent de
    /// docs/design/features/mouvement/design.md (qui fait foi).
    /// Modifiable en Play Mode : les changements restent dans l'asset.
    /// </summary>
    [CreateAssetMenu(fileName = "Movement_Default", menuName = "ItHunts/Réglages/Mouvement")]
    public sealed class MovementSettings : ScriptableObject
    {
        [Header("Course au sol")]
        [Tooltip("Vitesse de course (m/s). Doit rester sous la vitesse de chasse du boss (9 m/s).")]
        [Min(0f)] public float runSpeed = 7.5f;
        [Tooltip("Accélération au sol, façon Source (sv_accelerate). Vitesse max atteinte en ≈ 0.15 s.")]
        [Min(0f)] public float groundAcceleration = 10f;
        [Tooltip("Friction au sol (/s), façon Quake/Source (sv_friction).")]
        [Min(0f)] public float groundFriction = 6f;
        [Tooltip("Sous cette vitesse (m/s), la friction freine comme à cette vitesse, pour s'arrêter net (sv_stopspeed).")]
        [Min(0f)] public float stopSpeed = 2.5f;

        [Header("En l'air (façon Source)")]
        [Tooltip("Accélération en l'air (sv_airaccelerate).")]
        [Min(0f)] public float airAcceleration = 10f;
        [Tooltip("Plafond de la vitesse voulue en l'air (m/s). C'est lui qui permet de gagner de la vitesse en strafant.")]
        [Min(0f)] public float airWishSpeedCap = 1f;
        [Tooltip("Contrôle aérien façon CPMA/Deadlock, seulement en avançant sans strafer : fait tourner la " +
                 "vitesse vers la caméra sans changer sa valeur. 0 = Source pur, 1 = direction suivie " +
                 "instantanément. Part de l'angle rattrapée à chaque tick de 1/60 s.")]
        [Range(0f, 1f)] public float airControl = 0.15f;

        [Header("Sauts multiples et bunny hop")]
        [Tooltip("Nombre total de sauts avant de retoucher le sol (1 au sol + les sauts en l'air). " +
                 "Les améliorations pourront l'augmenter.")]
        [Min(1)] public int maxJumps = 2;
        [Tooltip("Hauteur d'un saut en l'air (m). La vitesse horizontale est conservée.")]
        [Min(0.01f)] public float airJumpHeight = 1.3f;
        [Tooltip("Maintenir le saut fait resauter automatiquement à chaque atterrissage (jamais en l'air).")]
        public bool autoBunnyHop = true;

        [Header("Sécurité")]
        [Tooltip("Plafond de sécurité de la vitesse horizontale (m/s). Volontairement haut : ce sont les " +
                 "sauts qui limitent, pas la vitesse.")]
        [Min(1f)] public float maxHorizontalSpeed = 50f;

        [Header("Saut et gravité")]
        [Tooltip("Gravité (m/s²).")]
        [Min(0.01f)] public float gravity = 20f;
        [Tooltip("Multiplicateur de gravité quand on descend.")]
        [Min(1f)] public float fallGravityMultiplier = 1.5f;
        [Tooltip("Vitesse de chute maximale (m/s).")]
        [Min(0.1f)] public float maxFallSpeed = 35f;
        [Tooltip("Hauteur d'un saut complet (m). L'impulsion en découle : √(2·g·h).")]
        [Min(0.01f)] public float jumpHeight = 1.3f;
        [Tooltip("Relâcher le saut pendant la montée multiplie la vitesse verticale par cette valeur.")]
        [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;
        [Tooltip("Coyote time (s) : on peut encore sauter juste après avoir quitté un bord.")]
        [Min(0f)] public float coyoteTime = 0.10f;
        [Tooltip("Buffer de saut (s) : un appui juste avant l'atterrissage est mémorisé.")]
        [Min(0f)] public float jumpBuffer = 0.12f;

        [Header("Capsule (CharacterController)")]
        [Min(0.05f)] public float capsuleRadius = 0.4f;
        [Min(0.5f)] public float capsuleHeight = 1.8f;
        [Tooltip("Hauteur des yeux (m), depuis les pieds.")]
        [Min(0.1f)] public float eyeHeight = 1.6f;
        [Tooltip("Pente maximale sur laquelle on marche (°).")]
        [Range(0f, 89f)] public float slopeLimit = 45f;
        [Tooltip("Hauteur de marche franchie sans sauter (m).")]
        [Min(0f)] public float stepOffset = 0.3f;
        [Tooltip("Distance maximale de « collage » au sol en descendant une pente (m).")]
        [Min(0f)] public float groundSnapDistance = 0.5f;

        /// <summary>Impulsion verticale d'un saut complet (m/s).</summary>
        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);

        /// <summary>Impulsion verticale d'un saut en l'air (m/s).</summary>
        public float AirJumpVelocity => Mathf.Sqrt(2f * gravity * airJumpHeight);

        public static int SecondsToTicks(float seconds, float fixedDeltaTime) =>
            Mathf.Max(0, Mathf.RoundToInt(seconds / fixedDeltaTime));
    }
}
