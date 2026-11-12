using ItHunts.Movement;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>Raison d'un usage refusé de la télékinésie (debug, son, HUD).</summary>
    public enum TelekinesisRefusal : byte
    {
        Aucun,
        RienAttrapable,
        TropLourd,
        PasDEndurance,
        Recharge,
        RienEnMain,
    }

    /// <summary>Ce que l'orbe demande au monde physique ce tick (appliqué par TelekinesisHolder).</summary>
    public enum TelekinesisCommandKind : byte
    {
        Aucune,
        Attraper,
        Lancer,
        Poser,
        Lacher,     // échange d'orbe, mort : l'objet garde sa vitesse
    }

    public struct TelekinesisCommand
    {
        public TelekinesisCommandKind Kind;
        public int TargetId;

        /// <summary>Lancer : direction visée (depuis les yeux) ; la vitesse finale est calculée par le monde physique.</summary>
        public Vector3 AimDirection;
    }

    /// <summary>Résultat d'un rayon de visée sur un objet attrapable.</summary>
    public struct GrabCandidate
    {
        public int Id;
        public float Mass;
        public float Distance;
        public Vector3 Point;
    }

    /// <summary>
    /// État de l'orbe de télékinésie d'un joueur. Données seulement, copiables (réseau) ; l'objet tenu est désigné
    /// par un identifiant, le corps physique vit dans la scène. Les délais sont en ticks.
    /// </summary>
    public struct TelekinesisState
    {
        public OrbStatus Status;

        /// <summary>Endurance (points). Négatif : pas encore initialisée.</summary>
        public float Stamina;

        /// <summary>Dernière dépense d'endurance (la régénération attend regenDelay).</summary>
        public int LastSpendTick;

        /// <summary>Dernier lancer (recharge avant d'attraper de nouveau).</summary>
        public int LastThrowTick;

        /// <summary>Objet tenu (0 = aucun) et tick où il a été attrapé.</summary>
        public int HeldId;
        public int GrabTick;

        public int LastConsumedPrimaryPressTick;
        public int LastConsumedSecondaryPressTick;

        /// <summary>Visée de ce tick : objet attrapable touché, trop lourd ou non, distance.</summary>
        public bool AimHit;
        public int AimId;
        public bool AimTooHeavy;
        public float AimDistance;

        /// <summary>Dernier objet lancé (retour visuel) et dernière action.</summary>
        public int LastThrownId;
        public TelekinesisCommandKind LastAction;
        public int LastActionTick;

        public int LastRefusalTick;
        public TelekinesisRefusal LastRefusal;

        public bool Holding => HeldId != 0;

        public static TelekinesisState Create() => new TelekinesisState
        {
            Status = OrbStatus.Prete,
            Stamina = -1f,
            LastSpendTick = MovementState.NeverTick,
            LastThrowTick = MovementState.NeverTick,
            HeldId = 0,
            GrabTick = MovementState.NeverTick,
            LastConsumedPrimaryPressTick = MovementState.NeverTick,
            LastConsumedSecondaryPressTick = MovementState.NeverTick,
            AimHit = false,
            AimId = 0,
            AimTooHeavy = false,
            AimDistance = 0f,
            LastThrownId = 0,
            LastAction = TelekinesisCommandKind.Aucune,
            LastActionTick = MovementState.NeverTick,
            LastRefusalTick = MovementState.NeverTick,
            LastRefusal = TelekinesisRefusal.Aucun,
        };
    }

    /// <summary>Rayon de visée sur les objets attrapables (tests : version factice ; jeu : Physics.Raycast).</summary>
    public interface IGrabProbe
    {
        /// <summary>Premier objet touché dans la portée ; faux si rien, ou si la première surface touchée n'est pas attrapable.</summary>
        bool FindGrabbable(Vector3 origin, Vector3 direction, float range, out GrabCandidate candidate);
    }
}
