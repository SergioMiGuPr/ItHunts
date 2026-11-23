using UnityEngine;
using UnityEngine.Serialization;

namespace ItHunts.Settings
{
    /// <summary>
    /// Réglages du mouvement du joueur. Les valeurs par défaut viennent de
    /// docs/design/features/mouvement/design.md (qui fait foi).
    /// Modifiable en Play Mode : les changements restent dans l'asset.
    /// </summary>
    /// <summary>Profil de vitesse pendant un dash.</summary>
    public enum DashProfile
    {
        /// <summary>Départ au pic puis baisse linéaire jusqu'à la vitesse de sortie : pas de cassure.</summary>
        Progressif,
        /// <summary>Vitesse constante pendant tout le dash, puis vitesse de sortie.</summary>
        Constant,
    }

    [CreateAssetMenu(fileName = "Movement_Default", menuName = "ItHunts/Réglages/Mouvement")]
    public sealed class MovementSettings : ScriptableObject
    {
        [Header("Course au sol")]
        [Tooltip("Vitesse de course de base (m/s, D-072 : 8.5). La chasse du traqueur est à 9 m/s, sa pointe à 16.")]
        [Min(0f)] public float runSpeed = 8.5f;
        [Tooltip("Accélération au sol, façon Source (sv_accelerate). Vitesse max atteinte en ≈ 0.15 s.")]
        [Min(0f)] public float groundAcceleration = 10f;
        [Tooltip("Friction au sol (/s), façon Quake/Source (sv_friction).")]
        [Min(0f)] public float groundFriction = 6f;
        [Tooltip("Sous cette vitesse (m/s), la friction freine comme à cette vitesse, pour s'arrêter net (sv_stopspeed).")]
        [Min(0f)] public float stopSpeed = 2.5f;

        [Header("Élan gardé au sol (D-073)")]
        [Tooltip("Au sol, tant qu'on tient « avancer » dans le cône, la vitesse au-dessus de la course est gardée (pas de friction).")]
        public bool keepGroundMomentum = true;
        [Tooltip("Cône (°, demi-angle) autour de la direction de déplacement : entrée dedans = élan gardé, la trajectoire suit l'entrée.")]
        [Range(0f, 90f)] public float momentumConeAngle = 45f;
        [Tooltip("Au-delà de cet angle (°) entre l'entrée et la trajectoire, l'élan est cassé (virage) : redescente vers la course. " +
                 "Entre le cône et cet angle : perte progressive tant qu'on y reste.")]
        [Range(0f, 180f)] public float momentumTurnAngle = 60f;
        [Tooltip("Braquage dans le cône : part de l'angle rattrapée par tick de 1/60 s (la vitesse ne change pas).")]
        [Range(0f, 1f)] public float momentumSteering = 0.2f;
        [Tooltip("Décroissance de l'élan gardé (m/s²), vers la course. 0 : gardé tel quel tant qu'on avance.")]
        [Min(0f)] public float momentumDecay = 0f;
        [Tooltip("Perte progressive de l'élan (m/s²) vers la course : avancer lâché, hors du cône, virage, mur percuté.")]
        [Min(0f)] public float momentumLossDeceleration = 12f;
        [Tooltip("Perte quand on recule ou freine (entrée opposée à la trajectoire, m/s²).")]
        [Min(0f)] public float momentumBrakeDeceleration = 30f;
        [Tooltip("Plafond au sol (m/s) : au-dessus (atterrissage d'un bunny hop rapide, traction), redescente vers ce plafond.")]
        [Min(0f)] public float groundSpeedCap = 20f;
        [Tooltip("Redescente vers le plafond au sol (m/s²).")]
        [Min(0f)] public float groundOverCapDeceleration = 15f;
        [Tooltip("Mur percuté au sol : élan cassé si la trajectoire entre dans le mur avec au moins cet angle (°).")]
        [Range(0f, 90f)] public float momentumWallHitAngle = 20f;

        [Header("En l'air (façon Source)")]
        [Tooltip("Accélération en l'air (sv_airaccelerate).")]
        [Min(0f)] public float airAcceleration = 10f;
        [Tooltip("Plafond de la vitesse voulue en l'air (m/s). C'est lui qui permet de gagner de la vitesse en strafant.")]
        [Min(0f)] public float airWishSpeedCap = 1f;
        [Tooltip("Contrôle aérien façon CPMA/Deadlock, seulement en avançant sans strafer : fait tourner la " +
                 "vitesse vers la caméra sans changer sa valeur. 0 = Source pur, 1 = direction suivie " +
                 "instantanément. Part de l'angle rattrapée à chaque tick de 1/60 s.")]
        [Range(0f, 1f)] public float airControl = 0.15f;
        [Tooltip("Le contrôle aérien ne s'applique que si la caméra regarde à moins de cet angle (°) de la " +
                 "trajectoire. Au-delà (par ex. face au mur après un wall jump), il ne ramène pas en arrière.")]
        [Range(0f, 180f)] public float airControlMaxAngle = 100f;

        [Header("Sauts multiples et bunny hop")]
        [Tooltip("Nombre total de sauts avant de retoucher le sol (1 au sol + les sauts en l'air). " +
                 "Les améliorations pourront l'augmenter.")]
        [Min(1)] public int maxJumps = 2;
        [Tooltip("Hauteur d'un saut en l'air (m). La vitesse horizontale est conservée.")]
        [Min(0.01f)] public float airJumpHeight = 1.6f;
        [Tooltip("Maintenir le saut fait resauter automatiquement à chaque atterrissage (jamais en l'air).")]
        public bool autoBunnyHop = true;

        [Header("Glissade et accroupi")]
        [Tooltip("Vitesse horizontale minimale (m/s) pour déclencher une glissade. En dessous, la touche accroupit.")]
        [Min(0f)] public float slideMinSpeed = 6f;
        [Tooltip("Bonus de vitesse (m/s) à l'entrée d'une glissade.")]
        [Min(0f)] public float slideBoost = 2f;
        [Tooltip("Délai (s) avant qu'une nouvelle glissade redonne le bonus.")]
        [Min(0f)] public float slideBoostCooldown = 0.8f;
        [Tooltip("Décélération de glissade sur le plat (m/s²), constante : aller vite ne fait pas perdre plus " +
                 "vite. À 3 m/s², une glissade lancée à 20 m/s dure environ 5 à 6 s.")]
        [Min(0f)] public float slideDeceleration = 3f;
        [Tooltip("Sous un obstacle (pas la place de se relever), la glissade continue au moins à cette vitesse (m/s).")]
        [Min(0f)] public float slideMinSpeedUnderObstacle = 5f;
        [Tooltip("Part de la gravité le long de la pente ajoutée en glissade (descente : accélère, montée : freine).")]
        [Min(0f)] public float slideSlopeAcceleration = 1f;
        [Tooltip("Sous cette vitesse (m/s), la glissade s'arrête.")]
        [Min(0f)] public float slideExitSpeed = 4f;
        [Tooltip("Braquage en glissade : la vitesse tourne vers la direction voulue, sans changer de valeur.")]
        [Range(0f, 1f)] public float slideSteering = 0.05f;
        [Tooltip("Hauteur de la capsule en glissade et accroupi (m).")]
        [Min(0.5f)] public float crouchHeight = 0.9f;
        [Tooltip("Hauteur des yeux en glissade et accroupi (m).")]
        [Min(0.1f)] public float crouchEyeHeight = 0.75f;
        [Tooltip("Vitesse de marche accroupie (m/s).")]
        [Min(0f)] public float crouchSpeed = 3.5f;

        [Header("Murs : wall slide et wall jump")]
        [Tooltip("Vitesse de chute maximale (m/s) en wall slide, à faible vitesse le long du mur.")]
        [Min(0f)] public float wallSlideMaxFallSpeed = 3f;
        [Tooltip("Wall slide si l'entrée vise le mur à moins de cet angle (°) de sa direction intérieure.")]
        [Range(0f, 90f)] public float wallSlideInputTolerance = 75f;
        [Tooltip("Wall slide sans entrée dès que la vitesse le long du mur atteint cette valeur (m/s).")]
        [Min(0f)] public float wallSlideMinSpeed = 3f;
        [Tooltip("Effet wall run : à partir de cette vitesse le long du mur (m/s), la chute ralentit…")]
        [Min(0f)] public float wallRunSpeedStart = 8f;
        [Tooltip("… jusqu'à cette vitesse (m/s), où la chute maximale vaut wallRunMaxFallSpeed.")]
        [Min(0f)] public float wallRunSpeedFull = 15f;
        [Tooltip("Vitesse de chute maximale (m/s) à pleine vitesse de wall run.")]
        [Min(0f)] public float wallRunMaxFallSpeed = 0.5f;
        [Tooltip("Légère poussée vers le mur (m/s, sur le déplacement seulement) pour garder le contact en wall slide.")]
        [Min(0f)] public float wallStickSpeed = 1f;
        [Tooltip("Impulsion horizontale (m/s) du wall jump, AJOUTÉE à la vitesse acquise.")]
        [Min(0f)] public float wallJumpKick = 7.5f;
        [Tooltip("Hauteur d'un wall jump (m).")]
        [Min(0.01f)] public float wallJumpHeight = 1.8f;
        [Tooltip("Direction de l'impulsion : 0 = pur éloignement du mur, 1 = pur vers l'avant (caméra, sinon trajectoire).")]
        [Range(0f, 1f)] public float wallJumpForwardBias = 0.5f;
        [Tooltip("Après un wall jump, vitesse horizontale au moins égale à celle d'avant + ce bonus (m/s).")]
        [Min(0f)] public float wallJumpSpeedBonus = 1f;

        [Tooltip("Part du gain de vitesse gardée sur un mur : wall jump (impulsion et bonus) et strafe le long du mur en wall slide / " +
                 "wall run. 0.85 = −15 % (D-044).")]
        [Range(0f, 1f)] public float wallGainScale = 0.85f;
        [Tooltip("Les gains de vitesse sur un mur s'arrêtent à cette vitesse (m/s), comme la traction (D-036), au lieu du plafond " +
                 "commun ; au-delà, la vitesse est conservée sans gain : rien ne freine un joueur déjà plus rapide.")]
        [Min(0f)] public float wallGainCap = 18f;
        [Tooltip("Délai (s) après avoir quitté un mur pendant lequel le wall jump reste possible.")]
        [Min(0f)] public float wallCoyoteTime = 0.10f;
        [Tooltip("Durée (s) sans contrôle en l'air après un wall jump, pour ne pas annuler l'impulsion.")]
        [Min(0f)] public float wallJumpSteerLock = 0.15f;
        [Tooltip("Autoriser deux wall jumps de suite sur le même mur (sinon il faut toucher le sol ou un autre mur).")]
        public bool allowSameWallJump;
        [Tooltip("Toucher un mur recharge le compteur de sauts (maxJumps).")]
        public bool wallRefillsJumps = true;
        [Tooltip("Toucher un mur recharge aussi les charges de mouvement (non par défaut : elles reviennent avec le temps).")]
        public bool wallRefillsCharges;

        [Header("Hissage sur un rebord (D-074)")]
        [Tooltip("En l'air contre un mur en avançant vers lui : si son dessus est à portée, on se hisse dessus automatiquement.")]
        public bool mantleEnabled = true;
        [Tooltip("Dessus du rebord au plus à cette hauteur (m) au-dessus de la tête (pieds + capsuleHeight).")]
        [Min(0f)] public float mantleMaxAboveHead = 0.6f;
        [Tooltip("Dessus du rebord au moins à cette hauteur (m) au-dessus des pieds (plus bas : on passe par-dessus en sautant).")]
        [Min(0.05f)] public float mantleMinRise = 0.5f;
        [Tooltip("Profondeur minimale du dessus (m) : plus étroit, pas de hissage.")]
        [Min(0.1f)] public float mantleMinDepth = 0.6f;
        [Tooltip("Durée du hissage (s).")]
        [Min(0.05f)] public float mantleDuration = 0.3f;
        [Tooltip("Part de la durée passée à monter le long du mur (le reste : avancer sur le dessus).")]
        [Range(0.1f, 0.9f)] public float mantleRiseShare = 0.6f;
        [Tooltip("Part de la vitesse horizontale d'avant (juste avant le contact avec le mur) gardée à la sortie.")]
        [Range(0f, 1f)] public float mantleKeepSpeed = 0.7f;
        [Tooltip("L'entrée doit viser le mur à moins de cet angle (°) de sa direction intérieure.")]
        [Range(0f, 90f)] public float mantleInputAngle = 50f;

        [Header("Charges de mouvement et dash")]
        [Tooltip("Charges de mouvement, partagées par le saut en l'air, le dash et le magnétisme (1 chacun). " +
                 "Les améliorations pourront l'augmenter.")]
        [Min(0)] public int movementCharges = 3;
        [Tooltip("Temps (s) pour recharger une charge au sol.")]
        [Min(0.01f)] public float chargeRechargeTimeGround = 2f;
        [Tooltip("Temps (s) pour recharger une charge en l'air.")]
        [Min(0.01f)] public float chargeRechargeTimeAir = 4f;
        [Tooltip("Distance parcourue pendant un dash (m), à l'arrêt.")]
        [Min(0f)] public float dashDistance = 6.5f;
        [Tooltip("Durée du dash (s) : ni friction ni gravité pendant ce temps.")]
        [Min(0.02f)] public float dashDuration = 0.3f;
        [Tooltip("Profil de vitesse du dash. Progressif : départ au pic puis baisse jusqu'à la vitesse de sortie " +
                 "(le pic est calculé pour parcourir dashDistance).")]
        public DashProfile dashProfile = DashProfile.Progressif;
        [Tooltip("Gain de vitesse (m/s) d'un dash par rapport à la vitesse d'avant, au sol comme en l'air (D-084), sous le plafond commun.")]
        [Min(0f)] public float dashSpeedGain = 1f;
        [Tooltip("Part de la vitesse moyenne du dash (dashDistance / dashDuration) gardée à la sortie, sous le plafond commun (D-084) : " +
                 "0.75 → ≈ 16 m/s depuis l'arrêt. Gardée ensuite comme l'élan (sol : avancer dans le cône ; air : comme le bunny hop). " +
                 "0 : ancienne sortie (dashExitSpeed).")]
        [Range(0f, 1f)] public float dashExitCarry = 0.75f;
        [Tooltip("Dash-jump : sauter pendant un dash au sol ou dans cette fenêtre (s) après sa fin.")]
        [Min(0f)] public float dashJumpWindow = 0.15f;
        [Tooltip("Gain de vitesse (m/s) d'un dash-jump réussi, ajouté à la vitesse de sortie du dash (D-084), sous le plafond commun.")]
        [Min(0f)] public float dashJumpSpeedGain = 3f;
        [Tooltip("À la sortie du dash, vitesse horizontale = la plus grande entre celle d'avant et celle-ci (m/s).")]
        [Min(0f)] public float dashExitSpeed = 12f;
        [Tooltip("Délai minimal (s) entre le début de deux dashs (un dash ne commence jamais avant la fin du précédent).")]
        [Min(0f)] public float dashCooldown = 0.3f;
        [Tooltip("Durée (s) pendant laquelle un appui de dash est mémorisé (un appui pendant un dash part ensuite).")]
        [Min(0f)] public float dashBuffer = 0.3f;
        [Tooltip("Après un dash, durée (s) sans friction au sol, pour ne pas retomber brutalement à la vitesse de course.")]
        [Min(0f)] public float dashFrictionGrace = 0.2f;

        [Header("Plafond commun des gains")]
        [Tooltip("Les gains de vitesse (wall jump, dash au sol, dash-jump, bonus de glissade) ne s'appliquent qu'en dessous de cette " +
                 "vitesse (m/s) ; au-delà, la vitesse est conservée sans gain.")]
        [FormerlySerializedAs("wallJumpBonusCap")]
        [Min(0f)] public float movementGainCap = 20f;

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

        [Header("Plané (Plumes, H-P2, D-100 : réglé par les améliorations sur la copie du joueur)")]
        [Tooltip("En l'air, saut tenu, en train de tomber : gravité × ce facteur (1 : pas de plané).")]
        [Range(0.05f, 1f)] public float glideGravityScale = 1f;
        [Tooltip("Pendant le plané, la vitesse horizontale ne baisse pas.")]
        public bool glideKeepsSpeed;
        [Tooltip("Pendant le plané, contrôle complet de la direction en l'air.")]
        public bool glideSteer;

        /// <summary>Impulsion verticale d'un saut complet (m/s).</summary>
        public float JumpVelocity => Mathf.Sqrt(2f * gravity * jumpHeight);

        /// <summary>Impulsion verticale d'un saut en l'air (m/s).</summary>
        public float AirJumpVelocity => Mathf.Sqrt(2f * gravity * airJumpHeight);

        /// <summary>Impulsion verticale d'un wall jump (m/s).</summary>
        public float WallJumpVelocity => Mathf.Sqrt(2f * gravity * wallJumpHeight);

        public static int SecondsToTicks(float seconds, float fixedDeltaTime) =>
            Mathf.Max(0, Mathf.RoundToInt(seconds / fixedDeltaTime));
    }
}
