using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Calculs purs des effets de zone (D-027) : dégâts de zone d'un objet lancé, durabilité selon la masse, poussée de
    /// l'onde de répulsion, étourdissement. Testables, rejouables par un serveur plus tard.
    /// </summary>
    public static class AreaEffects
    {
        /// <summary>Rayon de la zone de dégâts : base + par kg × masse, sauf si l'objet impose le sien (override &gt; 0).</summary>
        public static float SplashRadius(float mass, DamageSettings cfg, float overrideRadius = 0f) =>
            overrideRadius > 0f ? overrideRadius : cfg.splashBaseRadius + cfg.splashRadiusPerKg * Mathf.Max(0f, mass);

        /// <summary>
        /// Dégâts de zone à cette distance du point d'impact : part du centre × dégâts d'impact, décroissance linéaire
        /// jusqu'à splashEdgeShare au bord ; 0 au-delà du rayon.
        /// </summary>
        public static float SplashDamage(float impactDamage, float distance, float radius, DamageSettings cfg)
        {
            if (impactDamage <= 0f || radius <= 0f || distance > radius)
                return 0f;
            float t = Mathf.Clamp01(distance / radius);
            return impactDamage * cfg.splashCenterShare * Mathf.Lerp(1f, cfg.splashEdgeShare, t);
        }

        /// <summary>Nombre de lancers tenus selon la masse (1, 2, 3, puis heavyDurability), sauf durabilité imposée.</summary>
        public static int DurabilityForMass(float mass, DamageSettings cfg, int overrideDurability = 0)
        {
            if (overrideDurability > 0)
                return overrideDurability;
            if (mass <= cfg.durability1MaxMass)
                return 1;
            if (mass <= cfg.durability2MaxMass)
                return 2;
            if (mass <= cfg.durability3MaxMass)
                return 3;
            return cfg.heavyDurability;
        }

        /// <summary>
        /// Vitesse de poussée de l'onde de répulsion sur un objet ou un ennemi : vitesse de référence × (masse de
        /// référence / masse, borné) × décroissance linéaire jusqu'à waveEdgeShare au bord ; 0 hors de l'onde.
        /// </summary>
        public static float WavePushSpeed(float mass, float distance, bool enemy, MagnetismOrbSettings cfg)
        {
            if (distance > cfg.waveRadius || cfg.waveRadius <= 0f)
                return 0f;
            float speed = enemy ? cfg.waveEnemySpeed : cfg.waveObjectSpeed;
            float reference = enemy ? cfg.waveEnemyReferenceMass : cfg.waveObjectReferenceMass;
            float massScale = Mathf.Clamp(reference / Mathf.Max(0.1f, mass), cfg.waveMassScaleMin, cfg.waveMassScaleMax);
            float falloff = Mathf.Lerp(1f, cfg.waveEdgeShare, Mathf.Clamp01(distance / cfg.waveRadius));
            return speed * massScale * falloff;
        }

        /// <summary>Direction de poussée : du centre de l'onde vers la cible, avec un peu de montée.</summary>
        public static Vector3 WavePushDirection(Vector3 waveCenter, Vector3 target, MagnetismOrbSettings cfg)
        {
            Vector3 away = target - waveCenter;
            away.y = Mathf.Max(0f, away.y);
            if (away.sqrMagnitude < 1e-6f)
                away = Vector3.up;
            return (away.normalized + Vector3.up * cfg.waveLift).normalized;
        }

        /// <summary>Un ennemi qui percute un obstacle à cette vitesse (composante vers l'obstacle) est-il étourdi ?</summary>
        public static bool Stuns(float impactSpeedIntoObstacle, TrainingDummySettings cfg) =>
            impactSpeedIntoObstacle >= cfg.stunImpactSpeed;
    }

    /// <summary>Dégâts de zone à l'impact d'un objet lancé (cercle au sol, son).</summary>
    public struct AreaImpactEvent
    {
        public Vector3 Point;
        public float Radius;
        public float ImpactDamage;

        /// <summary>Onde de répulsion (pas des dégâts) : autre couleur, pas de son de zone.</summary>
        public bool Wave;
    }

    /// <summary>Objet cassé (débris, éclat, son).</summary>
    public struct BreakEvent
    {
        public Vector3 Point;
        public Vector3 Size;
        public Vector3 Velocity;
        public Color Color;
    }

    /// <summary>Ennemi étourdi (repère, son).</summary>
    public struct StunEvent
    {
        public Transform Target;
        public Vector3 Point;
        public float Duration;
    }

    /// <summary>Canal des effets de zone, des objets cassés et des étourdissements (retour visuel et sonore).</summary>
    public static class AreaEvents
    {
        public static event System.Action<AreaImpactEvent> Area;
        public static event System.Action<BreakEvent> Broken;
        public static event System.Action<StunEvent> Stunned;

        public static void RaiseArea(in AreaImpactEvent e) => Area?.Invoke(e);
        public static void RaiseBroken(in BreakEvent e) => Broken?.Invoke(e);
        public static void RaiseStunned(in StunEvent e) => Stunned?.Invoke(e);
    }
}
