using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>États de la machine à états du mouvement. WallSlide arrive à l'étape des murs.</summary>
    public enum MovementMode : byte
    {
        Ground,
        Air,
        Slide,
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
        };

        public float HorizontalSpeed => new Vector2(Velocity.x, Velocity.z).magnitude;
    }
}
