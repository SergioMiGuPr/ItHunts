using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Rayon de visée avec la physique Unity. Les déclencheurs (triggers) et les colliders du joueur lui-même
    /// (sa capsule, que le rayon peut toucher de l'intérieur) sont ignorés.
    /// </summary>
    public sealed class PhysicsAimProbe : IAimProbe
    {
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Transform _owner;

        public LayerMask Mask { get; set; }

        /// <param name="owner">Racine du joueur qui vise : ses colliders sont ignorés (null : aucun).</param>
        public PhysicsAimProbe(LayerMask mask, Transform owner = null)
        {
            Mask = mask;
            _owner = owner;
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out AimHit hit)
        {
            hit = default;
            int count = Physics.RaycastNonAlloc(origin, direction, _hits, maxDistance, Mask, QueryTriggerInteraction.Ignore);
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                if (_owner != null && _hits[i].collider.transform.IsChildOf(_owner))
                    continue;
                if (best < 0 || _hits[i].distance < _hits[best].distance)
                    best = i;
            }

            if (best < 0)
                return false;
            RaycastHit h = _hits[best];
            hit = new AimHit { Point = h.point, Normal = h.normal, Distance = h.distance };
            return true;
        }
    }
}
