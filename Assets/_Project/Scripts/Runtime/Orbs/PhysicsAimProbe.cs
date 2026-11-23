using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Rayon de visée avec la physique Unity. Les déclencheurs (triggers) et les colliders du joueur lui-même
    /// (sa capsule, que le rayon peut toucher de l'intérieur) sont ignorés, ainsi que l'objet qu'il tient par télékinésie
    /// (orbe unique, D-024 : sinon une traction en tenant une caisse viserait la caisse ou verrait sa ligne de vue coupée).
    /// Un ennemi (Damageable) arrête le rayon et le signale (AimHit.Enemy) : pas de traction sur lui, la répulsion oui.
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
            // Un ennemi arrête le rayon et le signale : la traction ne s'y accroche jamais, la répulsion y part (onde).
            var damageable = h.collider.GetComponentInParent<ItHunts.Combat.Damageable>();
            bool enemy = damageable != null;
            bool nonMagnetic = h.collider.GetComponentInParent<NonMagneticSurface>() != null;   // D-051
            // Améliorations (D-100) : la traction peut s'accrocher aux goules et volants (Harpon) ou à la cible liée (Lien d'ancrage).
            byte kind = 0;
            bool bound = false;
            if (enemy && !damageable.ImmuneToOrbs && !damageable.Dead)
            {
                kind = damageable.GetComponent<ItHunts.Enemies.Ghoul>() != null ? (byte)1
                    : damageable.GetComponent<ItHunts.Enemies.Flyer>() != null ? (byte)2 : (byte)3;
                bound = damageable.Bound;
            }

            hit = new AimHit { Point = h.point, Normal = h.normal, Distance = h.distance, Enemy = enemy, NonMagnetic = nonMagnetic, EnemyKind = kind, EnemyBound = bound };
            return true;
        }
    }
}
