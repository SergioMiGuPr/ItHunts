using System;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>Perception des ennemis (D-033) : vue en cône avec ligne de vue, joueurs vivants.</summary>
    public static class EnemySenses
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>
        /// Le joueur est-il vu : dans le cône et à portée, et rien de solide entre les yeux et lui (les colliders de
        /// l'ennemi et les déclencheurs ne bloquent pas ; les objets attrapables non plus : on voit entre les caisses).
        /// </summary>
        public static bool Sees(Vector3 eye, Vector3 forward, PlayerHealth p, float range, float angleTotal, Transform self)
        {
            if (p == null || p.Dead || !p.isActiveAndEnabled)
                return false;
            Vector3 target = p.Center + Vector3.up * 0.5f;
            if (!EnemyMath.InSightCone(eye, forward, target, range, angleTotal))
                return false;
            Vector3 to = target - eye;
            float length = to.magnitude;
            if (length < 1e-3f)
                return true;
            int count = Physics.RaycastNonAlloc(eye, to / length, Hits, length, ~0, QueryTriggerInteraction.Ignore);
            Array.Sort(Hits, 0, count, Comparer.Instance);
            for (int i = 0; i < count; i++)
            {
                Collider c = Hits[i].collider;
                if (c == null || (self != null && c.transform.IsChildOf(self)))
                    continue;
                if (c.transform.IsChildOf(p.transform))
                    return true;
                if (c.GetComponentInParent<ItHunts.Orbs.Grabbable>() != null)
                    continue;
                return false;
            }

            return true;
        }

        /// <summary>Le joueur vivant le plus proche (null s'il n'y en a pas).</summary>
        public static PlayerHealth Nearest(Vector3 from)
        {
            PlayerHealth best = null;
            float bestD = float.PositiveInfinity;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                float d = (p.transform.position - from).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = p;
                }
            }

            return best;
        }

        /// <summary>Joueur par identifiant (0 ou inconnu : null).</summary>
        public static PlayerHealth Find(int playerId)
        {
            if (playerId == 0)
                return null;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && p.PlayerId == playerId)
                    return p;
            return null;
        }

        /// <summary>Vitesse simulée du joueur (pour la prédiction).</summary>
        public static Vector3 VelocityOf(PlayerHealth p) => p != null && p.Motor != null ? p.Motor.State.Velocity : Vector3.zero;

        /// <summary>Rayon et hauteur de la capsule du joueur.</summary>
        public static void Capsule(PlayerHealth p, out float radius, out float height)
        {
            var cc = p != null ? p.Collider as CharacterController : null;
            radius = cc != null ? cc.radius : 0.4f;
            height = cc != null ? cc.height : 1.8f;
        }

        private sealed class Comparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }
}
