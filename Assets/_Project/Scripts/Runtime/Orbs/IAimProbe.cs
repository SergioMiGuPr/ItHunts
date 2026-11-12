using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>Résultat d'un rayon de visée.</summary>
    public struct AimHit
    {
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
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
