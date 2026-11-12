using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Rayon de visée avec la physique Unity. Les déclencheurs (triggers) et les colliders du joueur lui-même
    /// (sa capsule, que le rayon peut toucher de l'intérieur) sont ignorés, ainsi que l'objet qu'il tient par télékinésie
    /// (orbe unique, D-024 : sinon une traction en tenant une caisse viserait la caisse ou verrait sa ligne de vue coupée).
    /// </summary>
    public sealed class PhysicsAimProbe : IAimProbe
    {
        private readonly RaycastHit[] _hits = new RaycastHit[16];
        private readonly Transform _owner;

        public LayerMask Mask { get; set; }

        /// <summary>Scène physique interrogée (par défaut, celle du jeu ; tests : une scène isolée).</summary>
        public PhysicsScene Scene { get; set; } = Physics.defaultPhysicsScene;

        /// <summary>Corps ignoré par le rayon : l'objet tenu (null : aucun).</summary>
        public Rigidbody IgnoreBody { get; set; }

        /// <param name="owner">Racine du joueur qui vise : ses colliders sont ignorés (null : aucun).</param>
        public PhysicsAimProbe(LayerMask mask, Transform owner = null)
        {
            Mask = mask;
            _owner = owner;
        }

        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out AimHit hit)
        {
            hit = default;
            int count = Scene.Raycast(origin, direction, _hits, maxDistance, Mask, QueryTriggerInteraction.Ignore);
            int best = -1;
            for (int i = 0; i < count; i++)
            {
                if (_owner != null && _hits[i].collider.transform.IsChildOf(_owner))
                    continue;
                if (IgnoreBody != null && _hits[i].collider.attachedRigidbody == IgnoreBody)
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
