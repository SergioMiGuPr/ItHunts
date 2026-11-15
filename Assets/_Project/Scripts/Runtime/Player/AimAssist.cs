using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Aide à la visée (après le playtest des étapes 8 et 9, la coop se joue souvent à la manette) : près d'une cible, la visée
    /// ralentit (aimAssistSlowdown) et glisse légèrement vers elle (aimAssistPull, en °/s, seulement quand le joueur bouge la
    /// vue ou se déplace). Cibles : goules et volants vivants (balles du barillet) et, main libre, les objets attrapables à
    /// portée (prise F / X). Activée par défaut à la manette, coupée à la souris (CameraSettings). Calculs purs pour les tests.
    /// </summary>
    public static class AimAssist
    {
        private static readonly List<Vector3> Points = new List<Vector3>();
        private static readonly RaycastHit[] Hits = new RaycastHit[8];

        /// <summary>Angle (°) entre le regard et le point, et distance.</summary>
        public static float AngleTo(Vector3 eye, Vector3 forward, Vector3 point, out float distance)
        {
            Vector3 to = point - eye;
            distance = to.magnitude;
            return distance < 1e-4f ? 0f : Vector3.Angle(forward, to);
        }

        /// <summary>La cible la plus proche du viseur (en angle), dans le cône et à portée ; faux s'il n'y en a pas.</summary>
        public static bool Best(Vector3 eye, Vector3 forward, IReadOnlyList<Vector3> points, float coneDeg, float range, out Vector3 best, out float angle)
        {
            best = default;
            angle = float.PositiveInfinity;
            for (int i = 0; i < points.Count; i++)
            {
                float a = AngleTo(eye, forward, points[i], out float d);
                if (d > range || a > coneDeg || a >= angle)
                    continue;
                angle = a;
                best = points[i];
            }

            return !float.IsPositiveInfinity(angle);
        }

        private static readonly List<(Vector3 p, float a)> Sorted = new List<(Vector3, float)>();

        /// <summary>La cible visible (ligne de vue) la plus proche du viseur, dans le cône et à portée (4 essais au plus).</summary>
        public static bool BestVisible(Vector3 eye, Vector3 forward, IReadOnlyList<Vector3> points, float coneDeg, float range, out Vector3 best, out float angle)
        {
            Sorted.Clear();
            for (int i = 0; i < points.Count; i++)
            {
                float a = AngleTo(eye, forward, points[i], out float d);
                if (d <= range && a <= coneDeg)
                    Sorted.Add((points[i], a));
            }

            Sorted.Sort((x, y) => x.a.CompareTo(y.a));
            for (int i = 0; i < Mathf.Min(4, Sorted.Count); i++)
                if (Visible(eye, Sorted[i].p))
                {
                    best = Sorted[i].p;
                    angle = Sorted[i].a;
                    return true;
                }

            best = default;
            angle = float.PositiveInfinity;
            return false;
        }

        /// <summary>Facteur de vitesse de visée : slowdown au centre de la cible, 1 au bord du cône (interpolé).</summary>
        public static float Slowdown(float angle, float coneDeg, float slowdown) =>
            coneDeg <= 0f ? 1f : Mathf.Lerp(slowdown, 1f, Mathf.Clamp01(angle / coneDeg));

        /// <summary>Petit pas (lacet, tangage, en °) vers la cible, au plus maxDegPerSec × dt, sans la dépasser.</summary>
        public static Vector2 Pull(float yaw, float pitch, Vector3 eye, Vector3 target, float maxDegPerSec, float dt)
        {
            Vector3 to = target - eye;
            if (to.sqrMagnitude < 1e-6f)
                return Vector2.zero;
            float wantYaw = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg;
            float wantPitch = -Mathf.Atan2(to.y, new Vector2(to.x, to.z).magnitude) * Mathf.Rad2Deg;
            float dy = Mathf.DeltaAngle(yaw, wantYaw), dp = Mathf.DeltaAngle(pitch, wantPitch);
            float step = maxDegPerSec * dt;
            Vector2 d = new Vector2(dy, dp);
            return d.magnitude <= step ? d : d.normalized * step;
        }

        /// <summary>Actif pour ce mode de jeu (manette ou souris), selon les réglages.</summary>
        public static bool Enabled(CameraSettings s, bool gamepad) => s != null && (gamepad ? s.aimAssistGamepad : s.aimAssistMouse);

        /// <summary>Cibles du moment : goules et volants vivants ; main libre, objets attrapables à portée de la télékinésie.</summary>
        public static IReadOnlyList<Vector3> Targets(Vector3 eye, bool freeHand, TelekinesisOrbSettings tk, float range)
        {
            Points.Clear();
            foreach (Ghoul g in Ghoul.All)
                if (g != null && g.CanHunt)
                    Points.Add(g.transform.position + Vector3.up * 0.6f);
            foreach (Flyer f in Flyer.All)
                if (f != null && !f.Dead)
                    Points.Add(f.transform.position);
            if (freeHand && tk != null)
                foreach (Grabbable gr in Grabbable.All)
                    if (gr != null && !gr.IsEnemy && !gr.Held && gr.Mass <= tk.maxMass && gr.Body != null
                        && (gr.Body.position - eye).sqrMagnitude <= tk.range * tk.range)
                        Points.Add(gr.Body.position);
            for (int i = Points.Count - 1; i >= 0; i--)
                if ((Points[i] - eye).sqrMagnitude > range * range)
                    Points.RemoveAt(i);
            return Points;
        }

        /// <summary>Ligne de vue jusqu'au point (le décor seulement bloque).</summary>
        public static bool Visible(Vector3 eye, Vector3 point)
        {
            Vector3 d = point - eye;
            float len = d.magnitude;
            int n = Physics.RaycastNonAlloc(eye, d / Mathf.Max(1e-4f, len), Hits, Mathf.Max(0f, len - 0.6f), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Hits[i].collider;
                if (c.attachedRigidbody == null && c.GetComponentInParent<PlayerHealth>() == null)
                    return false;
            }

            return true;
        }
    }
}
