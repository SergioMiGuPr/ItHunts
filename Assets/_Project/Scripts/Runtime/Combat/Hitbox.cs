using ItHunts.Orbs;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Zone de touche d'un ennemi (déclencheur, ailes du volant, D-040) : elle suit le modèle sans changer sa physique. Les
    /// balles la touchent (BarrelShooter) ; un projectile qui y entre (objet lancé ou repoussé, ennemi lancé) compte comme un
    /// impact sur l'ennemi, avec la logique habituelle (ImpactDamager.HandleImpact).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class Hitbox : MonoBehaviour
    {
        private Damageable _owner;

        public Damageable Owner => _owner != null ? _owner : _owner = GetComponentInParent<Damageable>();

        private void OnTriggerEnter(Collider other)
        {
            Damageable owner = Owner;
            if (owner == null || owner.Dead || other == null || other.transform.IsChildOf(owner.transform))
                return;
            var ownGrab = owner.GetComponent<Grabbable>();
            if (ownGrab != null && ownGrab.Held)
                return;                                                  // tenu : c'est lui le projectile
            var g = other.GetComponentInParent<Grabbable>();
            var damager = g != null ? g.GetComponent<ImpactDamager>() : null;
            if (g == null || damager == null || damager.Settings == null || g.Body == null || g.Body.isKinematic || g.Broken)
                return;
            bool projectile = g.ThrowArmed || (g.Pushed && g.PushDangerous(Time.time, damager.Settings.pushedDangerTime))
                              || (g.IsEnemy && g.EnemyBody != null && g.EnemyBody.InFlight);
            if (!projectile)
                return;
            Vector3 point = other.ClosestPoint(transform.position);
            ImpactDamager.HandleImpact(g, owner, g.Body.linearVelocity, point, damager.Settings, Time.time);
        }
    }
}
