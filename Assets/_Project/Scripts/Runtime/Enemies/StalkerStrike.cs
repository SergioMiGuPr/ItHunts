using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Coup du traqueur (boss-traqueur, règle 4) : il tue le joueur quels que soient ses PV, sans passer par le tir ami.
    /// Réseau (étape 11) : décidé par l'autorité (la mort en un coup est sensible à la latence, GDD).
    /// </summary>
    public static class StalkerStrike
    {
        public static bool Hit(PlayerHealth target, float time) =>
            target != null && target.Kill(target.Center, time);

        /// <summary>
        /// Prise à la main (D-034) : si la capsule du joueur touche la main (boîte posée sur le point visé), il meurt, quels
        /// que soient ses PV. Vrai s'il est tué.
        /// </summary>
        public static bool Grab(PlayerHealth target, Vector3 handCenter, Vector3 halfExtents, Quaternion handRotation, float time)
        {
            if (target == null || target.Dead)
                return false;
            EnemySenses.Capsule(target, out float r, out float h);
            return EnemyMath.CapsuleIntersectsBox(target.transform.position, r, h, handCenter, halfExtents, handRotation) && Hit(target, time);
        }
    }
}
