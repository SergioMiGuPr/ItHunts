using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Calculs purs du mouvement des mains (testables). L'intensité du mouvement des mains (réglage de confort du joueur,
    /// 0..1) agit sur le balancement, le balancier de course et la plongée à l'atterrissage, jamais sur les gestes ni
    /// sur les poses d'état (glissade, recul avec la vitesse).
    /// </summary>
    public static class ViewmodelMotion
    {
        /// <summary>
        /// Balancement cible quand la vue tourne : les mains restent un peu en arrière de la rotation (décalage opposé à
        /// la rotation de cette image), bornés ; renvoie le décalage (m) et l'inclinaison (°).
        /// </summary>
        public static void Sway(float deltaYaw, float deltaPitch, FpsViewSettings cfg, float intensity,
            out Vector3 offset, out Vector3 euler)
        {
            float k = Mathf.Clamp01(intensity);
            float x = Mathf.Clamp(-deltaYaw * cfg.swayPerDegree, -cfg.swayMaxOffset, cfg.swayMaxOffset);
            float y = Mathf.Clamp(deltaPitch * cfg.swayPerDegree, -cfg.swayMaxOffset, cfg.swayMaxOffset);
            offset = new Vector3(x, y, 0f) * k;
            float roll = Mathf.Clamp(deltaYaw * cfg.swayMaxAngle * 0.5f, -cfg.swayMaxAngle, cfg.swayMaxAngle);
            float pitch = Mathf.Clamp(-deltaPitch * cfg.swayMaxAngle * 0.5f, -cfg.swayMaxAngle, cfg.swayMaxAngle);
            euler = new Vector3(pitch, 0f, roll) * k;
        }

        /// <summary>Avance la phase du balancier (en pas) : proportionnelle à la vitesse au sol, figée en l'air.</summary>
        public static float AdvanceBobPhase(float phase, float horizontalSpeed, bool grounded, FpsViewSettings cfg, float dt)
        {
            if (!grounded)
                return phase;
            float factor = Mathf.Min(horizontalSpeed / cfg.bobReferenceSpeed, cfg.bobMaxSpeedFactor);
            return phase + cfg.bobStepsPerSecond * factor * dt;
        }

        /// <summary>
        /// Balancier de course : un « 8 » (vertical deux fois par pas, latéral une fois), d'amplitude proportionnelle à la
        /// vitesse (bornée), nul à l'arrêt et en l'air.
        /// </summary>
        public static Vector3 Bob(float phase, float horizontalSpeed, bool grounded, FpsViewSettings cfg, float intensity)
        {
            if (!grounded || horizontalSpeed < 0.1f)
                return Vector3.zero;
            float amp = cfg.bobAmplitude * Mathf.Min(horizontalSpeed / cfg.bobReferenceSpeed, cfg.bobMaxSpeedFactor) * Mathf.Clamp01(intensity);
            float a = phase * Mathf.PI;                                  // un pas = demi-période latérale
            return new Vector3(Mathf.Sin(a) * amp, -Mathf.Abs(Mathf.Cos(a)) * amp, 0f);
        }

        /// <summary>Plongée à l'atterrissage (m, positive vers le bas) selon la vitesse de chute.</summary>
        public static float LandingDip(float fallSpeed, FpsViewSettings cfg, float intensity) =>
            Mathf.Min(cfg.landingDipMax, Mathf.Max(0f, fallSpeed) / 10f * cfg.landingDipPer10) * Mathf.Clamp01(intensity);

        /// <summary>Recul des mains avec la vitesse (repère caméra) : à l'opposé du déplacement, proportionnel.</summary>
        public static Vector3 SpeedLag(Vector3 localVelocity, FpsViewSettings cfg) =>
            -localVelocity / 20f * cfg.speedLagAt20;

        /// <summary>Courbe d'aller-retour d'un geste (0 → 1 → 0), t de 0 à 1.</summary>
        /// <summary>Flottement (retours du lot, point 6) : hauteur au-dessus de la paume (m) à l'instant t (phase en fraction de période).</summary>
        public static float FloatHeight(float t, FpsViewSettings cfg, float phase = 0f) =>
            cfg.floatHeight + cfg.floatAmplitude * Mathf.Sin(2f * Mathf.PI * (t / Mathf.Max(0.1f, cfg.floatPeriod) + phase));

        /// <summary>Légère oscillation d'un artefact qui flotte (degrés, x : tangage, y : lacet, z : roulis).</summary>
        public static Vector3 FloatWobble(float t, FpsViewSettings cfg, float phase = 0f)
        {
            float w = 2f * Mathf.PI * (t / Mathf.Max(0.1f, cfg.floatPeriod) + phase);
            return new Vector3(Mathf.Sin(w * 0.7f) * 0.5f, Mathf.Sin(w * 0.5f + 1f), Mathf.Sin(w)) * cfg.floatWobble;
        }

        public static float Punch(float t) => t <= 0f || t >= 1f ? 0f : Mathf.Sin(Mathf.PI * t);

        /// <summary>Lancer : recul au premier tiers, puis poussée (−windup → +push → 0).</summary>
        public static float ThrowCurve(float t, float windup, float push)
        {
            if (t <= 0f || t >= 1f)
                return 0f;
            if (t < 0.3f)
                return -windup * Mathf.Sin(t / 0.3f * Mathf.PI * 0.5f);
            float u = (t - 0.3f) / 0.7f;
            return Mathf.Lerp(-windup, push, Mathf.Sin(Mathf.Min(1f, u * 1.6f) * Mathf.PI * 0.5f)) * (1f - u * u);
        }
    }
}
