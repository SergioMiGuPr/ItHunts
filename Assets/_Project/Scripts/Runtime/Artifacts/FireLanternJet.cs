using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>Une cible brûlée par un jet (retour visuel, debug, mesures).</summary>
    public struct LanternHit
    {
        public int Tick;
        public Vector3 Point;
        public float Damage;
        public bool Killed;
        public bool Player;
        public int ShooterId;
    }

    /// <summary>
    /// Cibles d'un jet de la lanterne de feu (D-065), à chaque tick du jet : tout ennemi (ou joueur, avec le tir ami) dont un point
    /// est à moins de range de la lanterne (FireLanternSettings.Origin, main gauche) et à moins de coneAngle / 2 du viseur, sans décor entre les yeux et lui. Le jet traverse les
    /// ennemis, les joueurs et les objets (Rigidbody) ; seul le décor solide (collider sans Rigidbody, pas un déclencheur)
    /// l'arrête, la même règle que pour les textes en monde. Chaque cible n'est touchée qu'une fois par jet (burned). Comme toute
    /// attaque d'un joueur : un volant meurt en un coup, le traqueur n'est pas touché, le tir ami est respecté, l'ennemi tué donne
    /// ses âmes (Damageable.Died).
    /// Réseau (étape 11) : cône rejoué par l'autorité, avec les positions des cibles au moment du tick.
    /// </summary>
    public static class FireLanternJet
    {
        private static readonly Collider[] Overlaps = new Collider[128];
        private static readonly RaycastHit[] Rays = new RaycastHit[32];

        /// <summary>Brûle les nouvelles cibles du cône ; renvoie le nombre de cibles touchées ce tick.</summary>
        public static int Sweep(Vector3 eye, Vector3 aim, FireLanternSettings cfg, Transform shooter, int shooterId,
            HashSet<int> burned, float time, int tick, List<LanternHit> hits = null, PhysicsScene? scene = null)
        {
            Vector3 dir = aim.normalized;
            PhysicsScene physics = scene ?? Physics.defaultPhysicsScene;
            int count = physics.OverlapSphere(eye, cfg.range, Overlaps, cfg.hitMask, QueryTriggerInteraction.Collide);
            int touched = 0;
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (c == null || (shooter != null && c.transform.IsChildOf(shooter)))
                    continue;
                if (c.isTrigger && c.GetComponent<Hitbox>() == null)
                    continue;                                            // déclencheurs ignorés, sauf les zones de touche (ailes)

                var enemy = c.GetComponentInParent<Damageable>();
                PlayerHealth player = enemy == null ? c.GetComponentInParent<PlayerHealth>() : null;
                if (enemy == null && player == null)
                    continue;
                if (enemy != null && (enemy.Dead || enemy.ImmuneToOrbs))
                    continue;                                            // le traqueur n'est pas touché (D-035)
                int key = enemy != null ? enemy.GetInstanceID() : player.GetInstanceID();
                if (burned.Contains(key))
                    continue;
                if (!InCone(eye, dir, c, cfg, physics, shooter, out Vector3 point))
                    continue;

                burned.Add(key);
                var hit = new LanternHit { Tick = tick, Point = point, ShooterId = shooterId, Player = player != null };
                if (enemy != null)
                {
                    enemy.NoteAttacker(shooterId);                       // coop : il se retourne contre le tireur (D-041)
                    float taken = enemy.ApplyDamage(enemy.ProjectileDamage(cfg.damage), time, out bool killed);   // volant : un coup
                    hit.Damage = taken;
                    hit.Killed = killed;
                    if (taken > 0f)
                        DamageEvents.Raise(new DamageEvent
                        {
                            Target = enemy, Damage = taken, Point = point, Killed = killed, Tier = HitTier.Moyen, Time = time,
                        });
                }
                else if (player.TryHurt(shooterId, cfg.damage, point, time))
                    hit.Damage = cfg.damage;                             // tir ami seulement (FriendlyFire)

                touched++;
                hits?.Add(hit);
            }

            return touched;
        }

        /// <summary>
        /// Un point du collider dans le cône et à portée, sans décor entre les yeux et lui : le point du collider le plus proche de
        /// l'axe du jet, sinon le centre de ses bornes.
        /// </summary>
        public static bool InCone(Vector3 eye, Vector3 dir, Collider c, FireLanternSettings cfg, PhysicsScene physics, Transform shooter,
            out Vector3 point)
        {
            Bounds b = c.bounds;
            float along = Mathf.Clamp(Vector3.Dot(b.center - eye, dir), 0f, cfg.range);
            Vector3 axis = eye + dir * along;
            Vector3 near = Closest(c, axis);
            point = near;
            if (Inside(eye, dir, near, cfg) && Clear(eye, near, c, physics, shooter, cfg.hitMask))
                return true;
            point = b.center;
            return Inside(eye, dir, b.center, cfg) && Clear(eye, b.center, c, physics, shooter, cfg.hitMask);
        }

        private static bool Inside(Vector3 eye, Vector3 dir, Vector3 p, FireLanternSettings cfg)
        {
            Vector3 to = p - eye;
            float d = to.magnitude;
            if (d > cfg.range)
                return false;
            return d < 0.05f || Vector3.Angle(dir, to) <= cfg.HalfAngle;
        }

        /// <summary>Aucun décor solide (collider sans Rigidbody, pas un déclencheur, pas un ennemi ni un joueur) entre a et b.</summary>
        public static bool Clear(Vector3 a, Vector3 b, Collider target, PhysicsScene physics, Transform shooter, LayerMask mask)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 0.05f)
                return true;
            int n = physics.Raycast(a, d / len, Rays, len - 0.02f, mask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Rays[i].collider;
                if (c == null || c == target || c.attachedRigidbody != null)
                    continue;
                if (shooter != null && c.transform.IsChildOf(shooter))
                    continue;
                if (c is CharacterController || c.GetComponentInParent<Damageable>() != null || c.GetComponentInParent<PlayerHealth>() != null)
                    continue;
                return false;
            }

            return true;
        }

        private static Vector3 Closest(Collider c, Vector3 p)
        {
            if (c is MeshCollider mc && !mc.convex)
                return c.bounds.ClosestPoint(p);
            return c.ClosestPoint(p);
        }
    }
}
