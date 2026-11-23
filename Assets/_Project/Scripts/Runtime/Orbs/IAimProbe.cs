using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>Résultat d'un rayon de visée.</summary>
    public struct AimHit
    {
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;

        /// <summary>La surface touchée est un ennemi : la répulsion y part (onde), jamais la traction.</summary>
        public bool Enemy;

        /// <summary>Surface non magnétique (D-051) : la traction ne s'y accroche pas, la répulsion oui.</summary>
        public bool NonMagnetic;

        /// <summary>Type d'ennemi touché (améliorations D-100 : Harpon) : 0 aucun, 1 goule, 2 volant, 3 autre (traqueur, factice).</summary>
        public byte EnemyKind;
        /// <summary>Ennemi lié par le livre du joueur qui vise (Lien d'ancrage, H6).</summary>
        public bool EnemyBound;
    }

    /// <summary>
    /// Rayon de visée des orbes, séparé de la simulation (tests : version factice ; jeu : Physics.Raycast).
    /// La première surface touchée arrête le rayon.
    /// </summary>
    public interface IAimProbe
    {
        bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out AimHit hit);
    }
}
