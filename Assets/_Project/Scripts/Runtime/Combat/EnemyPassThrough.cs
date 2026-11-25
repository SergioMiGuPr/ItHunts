using System.Collections.Generic;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Jour 5, point 2 : aucune collision entre un joueur et les goules ou les volants (on passe au travers et on ne prend que leurs
    /// dégâts) ; le traqueur reste solide (il n'a pas d'EnemyBody). Physics.IgnoreCollision entre le CharacterController du joueur
    /// et chaque collider plein de l'ennemi, refait à chaque activation (Unity l'oublie quand un des deux objets est désactivé).
    /// Les tirs, la prise et les objets lancés ne sont pas concernés (ce ne sont pas des collisions du joueur). Plus d'ennemi qui
    /// pousse le joueur à travers un mur ou sous le sol en le chevauchant.
    /// </summary>
    public static class EnemyPassThrough
    {
        private static readonly List<EnemyBody> Bodies = new List<EnemyBody>();

        public static void Register(EnemyBody b)
        {
            if (b == null || Bodies.Contains(b))
                return;
            Bodies.Add(b);
            foreach (PlayerHealth p in PlayerHealth.All)
                Apply(p, b);
        }

        public static void Unregister(EnemyBody b) => Bodies.Remove(b);

        public static void RegisterPlayer(PlayerHealth p)
        {
            Bodies.RemoveAll(x => x == null);
            foreach (EnemyBody b in Bodies)
                Apply(p, b);
        }

        private static void Apply(PlayerHealth p, EnemyBody b)
        {
            if (p == null || b == null)
                return;
            var cc = p.GetComponent<CharacterController>();
            if (cc == null)
                return;
            foreach (Collider c in b.GetComponentsInChildren<Collider>(true))
                if (c != null && !c.isTrigger)
                    Physics.IgnoreCollision(cc, c, true);
        }
    }
}
