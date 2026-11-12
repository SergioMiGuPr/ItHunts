using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>États de la machine à états du mouvement.</summary>
    public enum MovementMode : byte
    {
        Ground,
        Air,
        Slide,
        WallSlide,
        /// <summary>Traction de l'orbe de magnétisme : ligne droite vers un point, sans gravité ni friction.</summary>
        Traction,
    }

    /// <summary>Raison de la fin d'une traction (debug).</summary>
    public enum TractionEnd : byte
    {
        Aucune,
        Relache,
        Arrivee,
        LigneCoupee,
        DureeMax,
        Saut,
        Dash,
        Repulsion,
        Bloquee,
        Echange,
    }

    /// <summary>
    /// État complet du mouvement d'un joueur. Données seulement (pas de référence Unity) :
    /// copiable, comparable et rejouable pour la prédiction réseau. Les délais sont en ticks.
    /// </summary>
    public struct MovementState
    {
        /// <summary>Valeur « jamais » pour les ticks, loin de toute soustraction qui déborderait.</summary>
        public const int NeverTick = int.MinValue / 2;

        public int Tick;
        public Vector3 Position;
        public Vector3 Velocity;
        public MovementMode Mode;
        public bool Grounded;
        public Vector3 GroundNormal;

        /// <summary>Dernier tick passé au sol (sert au coyote time). NeverTick juste après un saut.</summary>
        public int LastGroundedTick;

        /// <summary>Tick d'appui du dernier saut consommé : un appui ne fait sauter qu'une fois.</summary>
        public int LastConsumedJumpPressTick;

        /// <summary>Vrai pendant la montée d'un saut, tant que la coupe au relâchement est possible.</summary>
        public bool JumpRising;

        /// <summary>Tick du dernier saut (pour le debug).</summary>
        public int LastJumpTick;

        /// <summary>Sauts utilisés depuis le dernier contact avec le sol (ou un mur, à l'étape des murs).</summary>
        public int JumpsUsed;

        /// <summary>Capsule basse (glissade ou accroupi).</summary>
        public bool Crouched;

        /// <summary>Tick du dernier bonus de glissade (recharge).</summary>
        public int LastSlideBoostTick;

        /// <summary>Contact avec un mur ce tick-ci (en l'air).</summary>
        public bool OnWall;

        /// <summary>Dernier tick au contact d'un mur (wall-coyote), sa normale horizontale et son identifiant.</summary>
        public int LastWallTick;
        public Vector3 LastWallNormal;
        public int LastWallId;

        /// <summary>Mur du dernier wall jump (0 = aucun) : pas deux wall jumps de suite sur le même mur.</summary>
        public int LastWallJumpId;

        /// <summary>Jusqu'à ce tick (exclu), pas de contrôle en l'air (après un wall jump).</summary>
        public int SteerLockUntilTick;

        /// <summary>Charges de mouvement (fractionnaires pendant la recharge). Négatif : pas encore initialisé.</summary>
        public float Charges;

        /// <summary>Tick du dernier dash, et appui de dash déjà consommé.</summary>
        public int LastDashTick;
        public int LastConsumedDashPressTick;

        /// <summary>Dash en cours : ticks restants, direction, vitesse horizontale avant le dash, point de départ.</summary>
        public int DashTicksLeft;
        public Vector3 DashDir;
        public float DashSpeedBefore;
        public Vector3 DashStartPosition;

        /// <summary>Jusqu'à ce tick (exclu), pas de friction au sol (sortie de dash).</summary>
        public int DashGraceUntilTick;

        /// <summary>Profil du dash en cours : départ au sol, vitesse au pic, vitesse de sortie ; tick de fin.</summary>
        public bool DashStartedGrounded;
        public float DashPeakSpeed;
        public float DashExitTarget;
        public int DashEndTick;

        /// <summary>Dernière tentative de dash-jump (debug) : tick, réussite, retard en ticks, vitesses.</summary>
        public int LastDashJumpTick;
        public bool LastDashJumpSuccess;
        public int LastDashJumpLateTicks;
        public float LastDashJumpSpeedBefore;
        public float LastDashJumpSpeedAfter;

        /// <summary>Dernier dash, pour le debug : entrée locale, vitesses au départ et à l'arrivée, distance.</summary>
        public Vector2 LastDashMove;
        public float LastDashStartSpeed;
        public float LastDashEndSpeed;
        public float LastDashDistance;

        /// <summary>
        /// Traction en cours (Mode == Traction) : point visé (fixé au clic), accélération et vitesse de traction
        /// (copiées des réglages au départ), tick de départ. Fin : tick et raison.
        /// </summary>
        public Vector3 TractionPoint;
        public float TractionAcceleration;
        public float TractionSpeed;
        public int TractionStartTick;
        public int TractionEndTick;
        public TractionEnd TractionEndReason;

        public static MovementState Create(Vector3 position) => new MovementState
        {
            Tick = 0,
            Position = position,
            Velocity = Vector3.zero,
            Mode = MovementMode.Air,
            Grounded = false,
            GroundNormal = Vector3.up,
            LastGroundedTick = NeverTick,
            LastConsumedJumpPressTick = NeverTick,
            JumpRising = false,
            LastJumpTick = NeverTick,
            JumpsUsed = 0,
            Crouched = false,
            LastSlideBoostTick = NeverTick,
            OnWall = false,
            LastWallTick = NeverTick,
            LastWallNormal = Vector3.zero,
            LastWallId = 0,
            LastWallJumpId = 0,
            SteerLockUntilTick = NeverTick,
            Charges = -1f,
            LastDashTick = NeverTick,
            LastConsumedDashPressTick = NeverTick,
            DashTicksLeft = 0,
            DashDir = Vector3.zero,
            DashSpeedBefore = 0f,
            DashStartPosition = position,
            DashGraceUntilTick = NeverTick,
            DashStartedGrounded = false,
            DashPeakSpeed = 0f,
            DashExitTarget = 0f,
            DashEndTick = NeverTick,
            LastDashJumpTick = NeverTick,
            LastDashJumpSuccess = false,
            LastDashJumpLateTicks = 0,
            LastDashJumpSpeedBefore = 0f,
            LastDashJumpSpeedAfter = 0f,
            LastDashMove = Vector2.zero,
            LastDashStartSpeed = 0f,
            LastDashEndSpeed = 0f,
            LastDashDistance = 0f,
            TractionPoint = Vector3.zero,
            TractionAcceleration = 0f,
            TractionSpeed = 0f,
            TractionStartTick = NeverTick,
            TractionEndTick = NeverTick,
            TractionEndReason = TractionEnd.Aucune,
        };

        public float HorizontalSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;
    }
}
