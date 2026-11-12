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
        /// Vitesse de poussée de l'onde de répulsion (cône depuis le joueur, D-029) sur un objet ou un ennemi : vitesse de
        /// référence × (masse de référence / masse, borné) × décroissance linéaire avec la distance jusqu'à waveEdgeShare
        /// au bout de la portée × (vers waveAngleEdgeShare au bord du cône) ; 0 hors du cône.
        /// </summary>
        public static float WavePushSpeed(float mass, float distance, bool enemy, MagnetismOrbSettings cfg, float angle = 0f)
        {
            float half = cfg.waveConeAngle * 0.5f;
            if (cfg.waveRange <= 0f || distance > cfg.waveRange || angle > half)
                return 0f;
            float speed = enemy ? cfg.waveEnemySpeed : cfg.waveObjectSpeed;
            float massScale = MassScale(mass, enemy, cfg);
            return speed * massScale * Falloff(distance, cfg, cfg.waveEdgeShare) * Angular(angle, cfg);
        }

        /// <summary>
        /// Poussée continue de la répulsion maintenue (m/s²) : accélération de référence × facteur de masse ×
        /// décroissance avec la distance (jusqu'à repelHoldEdgeShare au bout) ; 0 hors du cône.
        /// </summary>
        public static float HoldAcceleration(float mass, float distance, bool enemy, MagnetismOrbSettings cfg, float angle = 0f)
        {
            if (cfg.waveRange <= 0f || distance > cfg.waveRange || angle > cfg.waveConeAngle * 0.5f)
                return 0f;
            return cfg.repelHoldAcceleration * MassScale(mass, enemy, cfg) * Falloff(distance, cfg, cfg.repelHoldEdgeShare) * Angular(angle, cfg);
        }

        /// <summary>
        /// Vitesse ajoutée dans l'axe par la poussée continue pendant dt : accélération × dt, sans jamais dépasser
        /// repelHoldMaxSpeed dans l'axe (au-delà, elle n'ajoute plus rien).
        /// </summary>
        public static float HoldDelta(float speedAlongAxis, float acceleration, float dt, MagnetismOrbSettings cfg) =>
            Mathf.Clamp(cfg.repelHoldMaxSpeed - speedAlongAxis, 0f, acceleration * dt);

        private static float MassScale(float mass, bool enemy, MagnetismOrbSettings cfg)
        {
            float reference = enemy ? cfg.waveEnemyReferenceMass : cfg.waveObjectReferenceMass;
            return Mathf.Clamp(reference / Mathf.Max(0.1f, mass), cfg.waveMassScaleMin, cfg.waveMassScaleMax);
        }

        /// <summary>Pleine force jusqu'à waveFalloffStart, puis linéaire jusqu'à <paramref name="edge"/> au bout de la portée.</summary>
        private static float Falloff(float distance, MagnetismOrbSettings cfg, float edge)
        {
            float span = cfg.waveRange - cfg.waveFalloffStart;
            float t = span > 1e-4f ? Mathf.Clamp01((distance - cfg.waveFalloffStart) / span) : 0f;
            return Mathf.Lerp(1f, edge, t);
        }

        private static float Angular(float angle, MagnetismOrbSettings cfg)
        {
            float half = cfg.waveConeAngle * 0.5f;
            return Mathf.Lerp(1f, cfg.waveAngleEdgeShare, half > 0f ? Mathf.Clamp01(angle / half) : 0f);
        }

        /// <summary>Distance et angle (degrés) d'un point vu depuis les yeux, par rapport à la direction visée.</summary>
        public static void ConeCoordinates(Vector3 eye, Vector3 aimDir, Vector3 point, out float distance, out float angle)
        {
            Vector3 to = point - eye;
            distance = to.magnitude;
            angle = distance < 1e-5f ? 0f : Vector3.Angle(aimDir, to);
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

        /// <summary>Dégâts contre un mur d'un ennemi repoussé (D-031) : PV par m/s au-delà du seuil, plafonnés par impact.</summary>
        public static float WallDamage(float impactSpeedIntoWall, TrainingDummySettings cfg) =>
            Mathf.Min(cfg.wallDamageMax, Mathf.Max(0f, impactSpeedIntoWall - cfg.stunImpactSpeed) * cfg.wallDamagePerSpeed);

        /// <summary>Peut-on étourdir maintenant ? Non s'il l'est déjà ou s'il est encore immunisé (après le précédent).</summary>
        public static bool CanStun(float now, float stunnedUntil, float immuneUntil) => now >= stunnedUntil && now >= immuneUntil;

        /// <summary>Fin de l'étourdissement et de l'immunité qui le suit, pour un étourdissement qui commence maintenant.</summary>
        public static void StunTimes(float now, TrainingDummySettings cfg, out float stunnedUntil, out float immuneUntil)
        {
            stunnedUntil = now + cfg.stunDuration;
            immuneUntil = stunnedUntil + cfg.stunImmunity;
        }
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

    /// <summary>Onde de répulsion en cône (retour visuel) : départ (main droite), direction, portée, ouverture.</summary>
    public struct ConeWaveEvent
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float Range;
        public float ConeAngle;

        /// <summary>Poussée continue (maintien) : anneaux plus discrets, émis en continu.</summary>
        public bool Continuous;
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
        public static event System.Action<ConeWaveEvent> ConeWave;

        public static void RaiseConeWave(in ConeWaveEvent e) => ConeWave?.Invoke(e);

        public static void RaiseArea(in AreaImpactEvent e) => Area?.Invoke(e);
        public static void RaiseBroken(in BreakEvent e) => Broken?.Invoke(e);
        public static void RaiseStunned(in StunEvent e) => Stunned?.Invoke(e);
    }
}
