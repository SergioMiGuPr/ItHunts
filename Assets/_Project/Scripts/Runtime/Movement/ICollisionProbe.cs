using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>Résultat d'un déplacement avec collisions.</summary>
    public struct MoveResult
    {
        public bool HitBelow;
        public bool HitSides;
        public bool HitAbove;

        /// <summary>Déplacement réellement effectué (m).</summary>
        public Vector3 ActualDelta;
    }

    /// <summary>
    /// Ce dont la simulation a besoin du monde physique. Implémenté par un CharacterController en
    /// jeu, et par un sol factice dans les tests. Permet de changer de méthode de collision plus tard.
    /// </summary>
    public interface ICollisionProbe
    {
        Vector3 Position { get; }

        /// <summary>Vrai si un sol marchable est juste sous les pieds ; normal = normale de ce sol.</summary>
        bool ProbeGround(out Vector3 normal);

        /// <summary>Déplace la capsule. snapToGround : la recoller au sol en descente (pentes, marches).</summary>
        MoveResult Move(Vector3 delta, bool snapToGround);
    }
}
