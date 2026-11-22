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

        /// <summary>Zone de tête (nuit 3, point 6) : une balle qui la touche est un headshot (dégâts × headshotMultiplier).</summary>
        public bool Head;

        /// <summary>Rayons des zones de tête (m), réglés par BarrelModsSettings.</summary>
        public static float GhoulHeadRadius = 0.3f, FlyerHeadRadius = 0.32f;

        /// <summary>
        /// Pose une zone de tête (sphère de touche, déclencheur) sur cet os : elle suit l'animation. Rayon en mètres (monde).
        /// </summary>
        public static Hitbox AddHead(Transform bone, Vector3 localCenter, float worldRadius)
        {
            if (bone == null)
                return null;
            Transform existing = bone.Find("Zone de tête (headshot)");
            if (existing != null)
            {
                // Taille réappliquée (le modèle a pu changer d'échelle) : rayon toujours en mètres monde.
                var es = existing.GetComponent<SphereCollider>();
                float sc = Mathf.Max(1e-3f, Mathf.Max(bone.lossyScale.x, Mathf.Max(bone.lossyScale.y, bone.lossyScale.z)));
                if (es != null)
                    es.radius = worldRadius / sc;
                return existing.GetComponent<Hitbox>();
            }
            var go = new GameObject("Zone de tête (headshot)");
            go.layer = bone.gameObject.layer;
            go.transform.SetParent(bone, false);
            go.transform.localPosition = localCenter;
            var s = go.AddComponent<SphereCollider>();
            s.isTrigger = true;
            float scale = Mathf.Max(1e-3f, Mathf.Max(bone.lossyScale.x, Mathf.Max(bone.lossyScale.y, bone.lossyScale.z)));
            s.radius = worldRadius / scale;
            var h = go.AddComponent<Hitbox>();
            h.Head = true;
            return h;
        }

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
