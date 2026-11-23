using ItHunts.Movement;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>États d'une orbe (ou d'un de ses modes) : Prête → EnUsage → Recharge → Prête.</summary>
    public enum OrbStatus : byte
    {
        Prete,
        EnUsage,
        Recharge,
    }

    public enum MagnetMode : byte
    {
        Attraction,
        Repulsion,
    }

    /// <summary>Raison d'un usage refusé (debug, puis son et HUD).</summary>
    public enum MagnetRefusal : byte
    {
        Aucun,
        PasDeCharge,
        Recharge,
        RienAPortee,
        /// <summary>Surface non magnétique visée (D-051).</summary>
        NonMagnetique,
    }

    /// <summary>
    /// État de l'orbe de magnétisme d'un joueur. Données seulement, copiable et rejouable comme le
    /// MovementState (prédiction réseau). La traction elle-même (point, vitesse) vit dans le MovementState.
    /// Les délais sont en ticks.
    /// </summary>
    public struct MagnetismState
    {
        /// <summary>État affiché de l'orbe, et de chaque mode.</summary>
        public OrbStatus Status;
        public OrbStatus AttractStatus;
        public OrbStatus RepelStatus;

        /// <summary>Mode utilisé en dernier (couleur de l'orbe).</summary>
        public MagnetMode LastMode;

        /// <summary>Traction en cours (vue par l'orbe ; le moteur peut la terminer : dash, obstacle).</summary>
        public bool Tracting;

        /// <summary>Charges d'attraction (fractionnaires pendant la recharge). Négatif : pas encore initialisé.</summary>
        public float AttractCharges;

        /// <summary>Fin de la dernière traction et dernière répulsion (sa recharge part de là).</summary>
        public int LastTractionEndTick;
        public int LastRepelTick;

        /// <summary>
        /// Répulsion maintenue en cours (D-029) : poussée continue depuis RepelHoldStartTick ; la recharge part de
        /// LastRepelEndTick (fin du maintien, ou appui simple).
        /// </summary>
        public bool RepelHolding;
        public int RepelHoldStartTick;
        public int LastRepelEndTick;

        /// <summary>Appuis déjà consommés (un appui = un usage ou un refus).</summary>
        public int LastConsumedAttractPressTick;
        public int LastConsumedRepelPressTick;

        /// <summary>Visée de ce tick (portée de l'attraction) : surface touchée, distance, point.</summary>
        public bool AimHit;
        public float AimDistance;
        public Vector3 AimPoint;
        public Vector3 AimNormal;

        /// <summary>Le point visé est sur un ennemi : pas de traction (la répulsion reste possible).</summary>
        public bool AimOnEnemy;
        /// <summary>La surface visée est non magnétique (D-051) : pas de traction.</summary>
        public bool AimNonMagnetic;

        /// <summary>La répulsion peut s'appliquer au point visé (surface à portée de la répulsion).</summary>
        public bool RepelInRange;

        /// <summary>Dernière traction : tick de départ et normale de la surface au point d'ancrage (retour visuel).</summary>
        public int LastTractionStartTick;
        public Vector3 TractionNormal;

        /// <summary>Dernière traction (debug) : vitesses au départ, maximale et à la fin, durée, raison de la fin.</summary>
        public float LastTractionStartSpeed;
        public float LastTractionPeakSpeed;
        public float LastTractionEndSpeed;
        public int LastTractionTicks;
        public TractionEnd LastTractionEnd;

        /// <summary>Dernière répulsion (debug).</summary>
        public Vector3 LastRepelVelocityBefore;
        public Vector3 LastRepelVelocityAfter;
        public Vector3 LastRepelPoint;
        public Vector3 LastRepelNormal;

        /// <summary>Dernier refus : tick, raison, mode demandé.</summary>
        public int LastRefusalTick;
        public MagnetRefusal LastRefusal;
        public MagnetMode LastRefusalMode;

        /// <summary>Charges de répulsion en plus (Charge de répulsion, G17, D-100) : −1 avant le premier tick (pleines).</summary>
        public float RepelCharges;

        public static MagnetismState Create() => new MagnetismState
        {
            RepelCharges = -1f,
            Status = OrbStatus.Prete,
            AttractStatus = OrbStatus.Prete,
            RepelStatus = OrbStatus.Prete,
            LastMode = MagnetMode.Attraction,
            Tracting = false,
            AttractCharges = -1f,
            LastTractionEndTick = MovementState.NeverTick,
            LastRepelTick = MovementState.NeverTick,
            RepelHolding = false,
            RepelHoldStartTick = MovementState.NeverTick,
            LastRepelEndTick = MovementState.NeverTick,
            LastConsumedAttractPressTick = MovementState.NeverTick,
            LastConsumedRepelPressTick = MovementState.NeverTick,
            AimHit = false,
            AimDistance = 0f,
            AimPoint = Vector3.zero,
            AimNormal = Vector3.up,
            RepelInRange = false,
            LastTractionStartTick = MovementState.NeverTick,
            TractionNormal = Vector3.up,
            LastTractionStartSpeed = 0f,
            LastTractionPeakSpeed = 0f,
            LastTractionEndSpeed = 0f,
            LastTractionTicks = 0,
            LastTractionEnd = TractionEnd.Aucune,
            LastRepelVelocityBefore = Vector3.zero,
            LastRepelVelocityAfter = Vector3.zero,
            LastRepelPoint = Vector3.zero,
            LastRepelNormal = Vector3.up,
            LastRefusalTick = MovementState.NeverTick,
            LastRefusal = MagnetRefusal.Aucun,
            LastRefusalMode = MagnetMode.Attraction,
        };
    }
}
