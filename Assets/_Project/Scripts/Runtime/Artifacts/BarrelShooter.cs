using System;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    public enum BarrelHitKind : byte
    {
        Rien,          // rien dans la portée
        Surface,       // mur, sol, objet
        Ennemi,        // blessé
        Insensible,    // le traqueur ignore les balles
        Joueur,        // un autre joueur (blessé seulement avec le tir ami)
    }

    /// <summary>Une balle tirée (retour visuel, debug, mesures).</summary>
    public struct BarrelShot
    {
        public int Tick;
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
        public BarrelHitKind Kind;
        public float Damage;
        public bool Killed;
        public Collider Collider;
        public int ShooterId;
    }

    /// <summary>
    /// Tir instantané du barillet (D-032) : rayon depuis les yeux du joueur au moment du clic, exactement dans la direction
    /// du viseur, jusqu'à la portée, sans chute. Le premier collider touché (hors le tireur et les déclencheurs) arrête la
    /// balle : un ennemi prend les dégâts (sauf le traqueur), un autre joueur seulement avec le tir ami.
    /// Réseau (étape 11) : le serveur rejouera ce rayon avec compensation de latence (positions des cibles au moment du clic).
    /// </summary>
    public static class BarrelShooter
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        public static BarrelShot Fire(Vector3 eye, Vector3 direction, BarrelSettings cfg, Transform shooter, int shooterId,
            float time, int tick, PhysicsScene? scene = null)
        {
            Vector3 dir = direction.normalized;
            var shot = new BarrelShot
            {
                Tick = tick, Origin = eye, Direction = dir, Point = eye + dir * cfg.range, Normal = -dir, Distance = cfg.range,
                Kind = BarrelHitKind.Rien, ShooterId = shooterId,
            };
            PhysicsScene physics = scene ?? Physics.defaultPhysicsScene;
            int count = physics.Raycast(eye, dir, Hits, cfg.range, cfg.hitMask, QueryTriggerInteraction.Ignore);
            Array.Sort(Hits, 0, count, Comparer.Instance);
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = Hits[i];
                if (h.collider == null || (shooter != null && h.collider.transform.IsChildOf(shooter)))
                    continue;
                shot.Point = h.point;
                shot.Normal = h.normal;
                shot.Distance = h.distance;
                shot.Collider = h.collider;
                shot.Kind = BarrelHitKind.Surface;

                var enemy = h.collider.GetComponentInParent<Damageable>();
                if (enemy != null && !enemy.Dead)
                {
                    if (enemy.ImmuneToOrbs)
                    {
                        shot.Kind = BarrelHitKind.Insensible;            // le traqueur ignore les balles
                        break;
                    }

                    shot.Kind = BarrelHitKind.Ennemi;
                    float taken = enemy.ApplyDamage(cfg.damage, time, out bool killed);
                    shot.Damage = taken;
                    shot.Killed = killed;
                    if (taken > 0f)
                        DamageEvents.Raise(new DamageEvent
                        {
                            Target = enemy, Damage = taken, Point = h.point, Killed = killed, Tier = HitTier.Petit, Time = time,
                            Bullet = true,
                        });
                    break;
                }

                var player = h.collider.GetComponentInParent<PlayerHealth>();
                if (player != null)
                {
                    shot.Kind = BarrelHitKind.Joueur;
                    if (player.TryHurt(shooterId, cfg.damage, h.point, time))
                        shot.Damage = cfg.damage;
                }

                break;
            }

            BarrelEvents.Raise(shot);
            return shot;
        }

        private sealed class Comparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }

    /// <summary>Canal des balles tirées (traînée, impact, son) : chaque vue joue celles qu'elle voit.</summary>
    public static class BarrelEvents
    {
        public static event Action<BarrelShot> Shot;
        public static void Raise(in BarrelShot s) => Shot?.Invoke(s);
    }
}
