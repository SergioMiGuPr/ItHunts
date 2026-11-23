using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Hud
{
    /// <summary>
    /// Règle « dans le champ de vision du joueur » de la carte (D-101) : un ennemi n'est marqué sur la carte de CE joueur que s'il est
    /// dans le cône de sa caméra (angle horizontal ≤ demi-FOV horizontal + marge), à portée (60 m par défaut) et sans décor entre les
    /// yeux et son centre (lancer de rayon ; les colliders du joueur et de l'ennemi visé ne comptent pas, ni les déclencheurs). Le test
    /// d'angle et de distance est pur (testé en EditMode) ; en coop, chaque vue montre ce que son joueur voit.
    /// </summary>
    public static class MinimapVision
    {
        public const float DefaultMarginDeg = 5f, DefaultRange = 60f;
        private static readonly RaycastHit[] Hits = new RaycastHit[24];

        /// <summary>
        /// Cône et portée (pur) : l'angle est mesuré à plat (plan x, z) entre l'avant de la caméra et la direction de la cible ; vrai si
        /// cet angle ≤ horizontalFov / 2 + marginDeg et si la distance (3D) ≤ range. Cible confondue avec l'œil : vraie.
        /// </summary>
        public static bool InSight(Vector3 eye, Vector3 forward, Vector3 target, float horizontalFov, float marginDeg, float range)
        {
            Vector3 to = target - eye;
            float d = to.magnitude;
            if (d > range)
                return false;
            if (d < 0.01f)
                return true;
            var f = new Vector2(forward.x, forward.z);
            var t = new Vector2(to.x, to.z);
            if (f.sqrMagnitude < 1e-6f || t.sqrMagnitude < 1e-6f)
                return true;                                            // regard à la verticale, ou cible juste au-dessus / en dessous
            return Vector2.Angle(f, t) <= horizontalFov * 0.5f + marginDeg;
        }

        /// <summary>Champ de vision horizontal (°) de la caméra du joueur (90° sans caméra).</summary>
        public static float HorizontalFov(Camera cam) =>
            cam != null ? Camera.VerticalToHorizontalFieldOfView(cam.fieldOfView, Mathf.Max(0.1f, cam.aspect)) : 90f;

        /// <summary>
        /// Ligne de vue libre de décor entre eye et target : rien de solide sauf les colliders du joueur (viewer) et de l'ennemi visé
        /// (enemy), déclencheurs ignorés.
        /// </summary>
        public static bool LineClear(Vector3 eye, Vector3 target, Transform viewer, Transform enemy)
        {
            Vector3 to = target - eye;
            float length = to.magnitude;
            if (length < 1e-3f)
                return true;
            int count = Physics.RaycastNonAlloc(eye, to / length, Hits, length, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = Hits[i].collider;
                if (c == null)
                    continue;
                if (viewer != null && c.transform.IsChildOf(viewer))
                    continue;
                if (enemy != null && c.transform.IsChildOf(enemy))
                    continue;
                return false;
            }

            return true;
        }

        /// <summary>Ce joueur voit-il l'ennemi (centre target) : cône de sa caméra, portée, ligne de vue.</summary>
        public static bool Sees(Camera cam, PlayerHealth viewer, Vector3 target, Transform enemy, float marginDeg = DefaultMarginDeg, float range = DefaultRange)
        {
            if (viewer == null || viewer.Dead)
                return false;
            Vector3 eye = cam != null ? cam.transform.position : viewer.transform.position + Vector3.up * 1.6f;
            Vector3 fwd = cam != null ? cam.transform.forward : viewer.transform.forward;
            return InSight(eye, fwd, target, HorizontalFov(cam), marginDeg, range) && LineClear(eye, target, viewer.transform, enemy);
        }
    }
}
