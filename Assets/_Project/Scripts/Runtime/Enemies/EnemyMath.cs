using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>États du traqueur (boss-traqueur, règle 1).</summary>
    public enum StalkerMode : byte
    {
        Repaire,     // temps de grâce au début de la zone : il attend
        Rode,
        Alerte,
        Chasse,
        Recherche,
        Enrage,
    }

    /// <summary>Pointe du traqueur : active jusqu'à Until, de nouveau possible à partir de ReadyAt (s).</summary>
    public struct BurstState
    {
        public float Until;
        public float ReadyAt;

        public static BurstState Create() => new BurstState { Until = float.NegativeInfinity, ReadyAt = float.NegativeInfinity };
        public bool Active(float now) => now < Until;
        public float Remaining(float now) => Mathf.Max(0f, Until - now);
        public float CooldownRemaining(float now) => Active(now) ? 0f : Mathf.Max(0f, ReadyAt - now);
    }

    /// <summary>Calculs purs des ennemis (D-033) : testables, rejouables par un serveur plus tard.</summary>
    public static class EnemyMath
    {
        /// <summary>Où sera la cible dans <paramref name="horizon"/> secondes, à sa vitesse horizontale actuelle.</summary>
        public static Vector3 Predict(Vector3 position, Vector3 velocity, float horizon) =>
            position + new Vector3(velocity.x, 0f, velocity.z) * Mathf.Max(0f, horizon);

        /// <summary>La cible est-elle dans le cône de vue (angle total, à l'horizontale) et à portée ? (sans ligne de vue)</summary>
        public static bool InSightCone(Vector3 eye, Vector3 forward, Vector3 target, float range, float angleTotal)
        {
            Vector3 to = target - eye;
            if (to.sqrMagnitude > range * range)
                return false;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            Vector3 f = new Vector3(forward.x, 0f, forward.z);
            if (flat.sqrMagnitude < 1e-4f || f.sqrMagnitude < 1e-4f)
                return true;
            return Vector3.Angle(f, flat) <= angleTotal * 0.5f;
        }

        /// <summary>
        /// Pointe : quand il voit le joueur et qu'elle est prête, elle part pour <paramref name="duration"/> s ; la recharge
        /// commence à la fin de la pointe. Renvoie vrai si une pointe vient de partir.
        /// </summary>
        public static bool UpdateBurst(ref BurstState b, bool seesTarget, float now, float duration, float cooldown)
        {
            if (!seesTarget || b.Active(now) || now < b.ReadyAt)
                return false;
            b.Until = now + duration;
            b.ReadyAt = b.Until + cooldown;
            return true;
        }

        /// <summary>
        /// Vitesse voulue du traqueur. Chasse (ou Enragé) : sa base (9, enragé 11), au moins followPlayerSpeedShare de la
        /// vitesse du joueur, jamais au-delà de sa pointe (16, enragé 18 : sous le bunny hop) ; pendant une pointe, la
        /// pointe. Les autres états ont leur vitesse fixe.
        /// </summary>
        public static float StalkerSpeed(StalkerMode mode, bool burst, float playerSpeed, StalkerSettings cfg) =>
            StalkerSpeed(mode, burst, playerSpeed, cfg, -1f);

        /// <summary>
        /// Comme ci-dessus ; <paramref name="enragedBase"/> (≥ 0) remplace la base enragée (paliers d'accélération, D-038).
        /// Jamais au-delà de maxSpeed (18 m/s), pointe comprise.
        /// </summary>
        public static float StalkerSpeed(StalkerMode mode, bool burst, float playerSpeed, StalkerSettings cfg, float enragedBase)
        {
            return Mathf.Min(cfg.maxSpeed, StalkerSpeedUncapped(mode, burst, playerSpeed, cfg, enragedBase));
        }

        /// <summary>
        /// Vitesse de base de l'Enragé selon le temps passé sur la même cible (D-038) : paliers (times, speeds), montée
        /// linéaire de <paramref name="blend"/> s vers chaque palier ; <paramref name="tier"/> : palier atteint (0 au départ).
        /// </summary>
        public static float EnragedRampSpeed(float timeOnTarget, float[] times, float[] speeds, float blend, out int tier)
        {
            tier = 0;
            int n = Mathf.Min(times?.Length ?? 0, speeds?.Length ?? 0);
            if (n == 0)
                return 0f;
            for (int i = 1; i < n; i++)
                if (timeOnTarget >= times[i])
                    tier = i;
            if (tier == 0)
                return speeds[0];
            float t = blend <= 0f ? 1f : Mathf.Clamp01((timeOnTarget - times[tier]) / blend);
            return Mathf.Lerp(speeds[tier - 1], speeds[tier], t);
        }

        private static float StalkerSpeedUncapped(StalkerMode mode, bool burst, float playerSpeed, StalkerSettings cfg, float enragedBase)
        {
            switch (mode)
            {
                case StalkerMode.Repaire:
                    return 0f;
                case StalkerMode.Rode:
                    return cfg.roamSpeed;
                case StalkerMode.Alerte:
                    return cfg.alertSpeed;
                case StalkerMode.Recherche:
                    return cfg.searchSpeed;
            }

            bool enraged = mode == StalkerMode.Enrage;
            float baseSpeed = enraged ? (enragedBase >= 0f ? enragedBase : cfg.enragedSpeed) : cfg.chaseSpeed;
            float cap = Mathf.Max(baseSpeed, enraged ? cfg.enragedBurstSpeed : cfg.burstSpeed);
            if (burst)
                return cap;
            return Mathf.Max(baseSpeed, Mathf.Min(cap, cfg.followPlayerSpeedShare * playerSpeed));
        }

        /// <summary>
        /// Ajoute une poussée à la vitesse du joueur sans jamais le ralentir (D-012) : si la vitesse horizontale obtenue est
        /// plus faible qu'avant, elle est ramenée à la même valeur (la poussée dévie, elle ne freine pas).
        /// </summary>
        public static Vector3 PushWithoutSlowdown(Vector3 velocity, Vector3 push)
        {
            Vector3 result = velocity + push;
            float before = new Vector2(velocity.x, velocity.z).magnitude;
            var h = new Vector2(result.x, result.z);
            float after = h.magnitude;
            if (after < before)
            {
                Vector2 dir = after > 1e-4f ? h / after : new Vector2(push.x, push.z).normalized;
                if (dir.sqrMagnitude < 1e-6f)
                    dir = new Vector2(velocity.x, velocity.z) / Mathf.Max(before, 1e-4f);
                h = dir * before;
                result = new Vector3(h.x, result.y, h.y);
            }

            return result;
        }

        /// <summary>
        /// Distance entre les surfaces de deux capsules verticales (pieds, rayon, hauteur) ; négative si elles se
        /// chevauchent ; +∞ si elles ne se recouvrent pas en hauteur.
        /// </summary>
        public static float CapsuleGap(Vector3 feetA, float radiusA, float heightA, Vector3 feetB, float radiusB, float heightB)
        {
            bool overlapY = feetA.y < feetB.y + heightB && feetB.y < feetA.y + heightA;
            if (!overlapY)
                return float.PositiveInfinity;
            float d = new Vector2(feetA.x - feetB.x, feetA.z - feetB.z).magnitude;
            return d - radiusA - radiusB;
        }

        /// <summary>
        /// La capsule verticale du joueur (pieds, rayon, hauteur) touche-t-elle une boîte orientée (centre, demi-tailles,
        /// rotation) ? Calcul pur (la main du traqueur, D-034) : le segment de l'axe de la capsule est ramené dans le repère
        /// de la boîte, et on cherche son point le plus proche de la boîte.
        /// </summary>
        public static bool CapsuleIntersectsBox(Vector3 feet, float radius, float height, Vector3 boxCenter, Vector3 halfExtents,
            Quaternion boxRotation)
        {
            Quaternion inv = Quaternion.Inverse(boxRotation);
            Vector3 a = inv * (feet + Vector3.up * radius - boxCenter);
            Vector3 b = inv * (feet + Vector3.up * Mathf.Max(radius, height - radius) - boxCenter);
            float best = float.PositiveInfinity;
            const int steps = 16;
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)steps);
                Vector3 q = new Vector3(Mathf.Clamp(p.x, -halfExtents.x, halfExtents.x), Mathf.Clamp(p.y, -halfExtents.y, halfExtents.y),
                    Mathf.Clamp(p.z, -halfExtents.z, halfExtents.z));
                best = Mathf.Min(best, (p - q).sqrMagnitude);
            }

            return best <= radius * radius;
        }

        /// <summary>
        /// Vitesse de départ du bond de la goule vers un point : arc de la hauteur voulue, vitesse horizontale au plus
        /// <paramref name="maxSpeed"/>, réglée pour retomber un peu au-delà du point (overshoot, m).
        /// </summary>
        /// <summary>
        /// Éclatement d'un volant lancé (D-040) : dégâts à cette distance du point d'impact, du centre (burstDamage) au bord
        /// (burstDamage × edgeShare) en ligne droite ; 0 au-delà du rayon.
        /// </summary>
        public static float BurstDamage(float distance, float radius, float center, float edgeShare)
        {
            if (radius <= 0f || distance > radius)
                return 0f;
            return center * Mathf.Lerp(1f, edgeShare, Mathf.Clamp01(distance / radius));
        }

        /// <summary>
        /// Piqué du volant (D-040) : tourne la vitesse vers la direction voulue d'au plus maxDegrees, à la vitesse donnée.
        /// </summary>
        public static Vector3 TurnTowards(Vector3 velocity, Vector3 wanted, float maxDegrees, float speed)
        {
            if (wanted.sqrMagnitude < 1e-6f)
                return velocity.normalized * speed;
            if (velocity.sqrMagnitude < 1e-6f)
                return wanted.normalized * speed;
            return Vector3.RotateTowards(velocity.normalized, wanted.normalized, maxDegrees * Mathf.Deg2Rad, 0f) * speed;
        }

        /// <summary>
        /// Coop (D-041) : changer de cible pour l'autre joueur ? Seulement après targetSwitchCooldown depuis le dernier changement,
        /// et si l'autre est plus proche d'au moins targetSwitchMargin, ou si la cible est à plus de targetGiveUpDistance (et
        /// l'autre plus proche qu'elle). Longueurs par le chemin.
        /// </summary>
        public static bool ShouldSwitchTarget(float currentLength, float otherLength, float sinceLastSwitch, EnemySettings cfg)
        {
            if (sinceLastSwitch < cfg.targetSwitchCooldown)
                return false;
            if (otherLength <= currentLength - cfg.targetSwitchMargin)
                return true;
            return currentLength > cfg.targetGiveUpDistance && otherLength < currentLength;
        }

        /// <summary>Accalmie (D-041) : point de recul, à distance du joueur le plus proche, à l'horizontale.</summary>
        public static Vector3 RetreatPoint(Vector3 position, Vector3 player, float distance)
        {
            Vector3 away = position - player;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f)
                away = Vector3.forward;
            return position + away.normalized * distance;
        }

        /// <summary>
        /// Fente de la prise (D-080) : ce qui manque au bras réel pour atteindre la cible (need, m), jamais plus que grabLunge ni
        /// plus loin que la place devant lui (room : distance à plat jusqu'à la cible moins son rayon).
        /// </summary>
        public static float LungeDistance(float need, float room, StalkerSettings s) =>
            Mathf.Clamp(need, 0f, Mathf.Min(s.grabLunge, Mathf.Max(0f, room - 0.5f)));

        /// <summary>Départ dans le clip (s) pour que son instant d'impact tombe après <paramref name="untilImpact"/> s (jamais négatif).</summary>
        public static float ClipOffsetForImpact(float clipImpact, float untilImpact) => Mathf.Max(0f, clipImpact - untilImpact);

        /// <summary>Avancée de la fente (0 à 1, douce) : elle se fait pendant les lungeTime dernières secondes avant l'impact.</summary>
        public static float LungeProgress(float now, float impactAt, float lungeTime)
        {
            float k = Mathf.Clamp01((now - (impactAt - lungeTime)) / Mathf.Max(0.01f, lungeTime));
            return k * k * (3f - 2f * k);
        }

        /// <summary>
        /// Revers (D-039, piste a) : le joueur (pieds) est-il dans la zone, à backhandReach du bord du corps à l'horizontale et
        /// pas plus haut que backhandHeight au-dessus de ses pieds ?
        /// </summary>
        public static bool InBackhandZone(Vector3 stalkerFeet, float bodyRadius, Vector3 playerFeet, StalkerSettings cfg)
        {
            Vector3 flat = playerFeet - stalkerFeet;
            float up = flat.y;
            flat.y = 0f;
            return flat.magnitude <= bodyRadius + cfg.backhandReach && up <= cfg.backhandHeight && up >= -2f;
        }

        /// <summary>
        /// Vitesse du traqueur pendant une prise : grabMoveFactor ; main plantée (D-039, piste b) : plantedMoveFactor (1 : il
        /// continue vers le joueur à sa vitesse normale) ; sans prise : 1.
        /// </summary>
        public static float GrabMoveFactor(bool grabbing, bool planted, StalkerSettings cfg) =>
            !grabbing ? 1f : planted ? cfg.plantedMoveFactor : cfg.grabMoveFactor;

        /// <summary>
        /// Après la descente de la main (D-039) : prise ratée (personne touché) → main plantée plantedDuration ; prise réussie →
        /// main au sol grabHold. La recharge (grabCooldown) part à la fin de ce temps dans les deux cas.
        /// </summary>
        public static float GrabHoldTime(bool caughtSomeone, StalkerSettings cfg, out bool planted)
        {
            planted = !caughtSomeone;
            return planted ? cfg.plantedDuration : cfg.grabHold;
        }

        /// <summary>
        /// Geste au-dessus de la tête (D-037) : le point (pieds du joueur) est-il, ou sera-t-il dans <paramref name="horizon"/> s,
        /// dans le cylindre autour du traqueur (rayon, de minY à maxY en hauteur absolue) ? Trajectoire prédite en ligne droite
        /// et en chute (gravité <paramref name="gravity"/>), échantillonnée tous les 0.05 s.
        /// </summary>
        public static bool WillPassOver(Vector3 feet, Vector3 velocity, float gravity, Vector3 center, float radius, float minY, float maxY, float horizon)
        {
            for (float t = 0f; t <= horizon + 1e-4f; t += 0.05f)
            {
                for (int k = 0; k < 2; k++)
                {
                    Vector3 p = feet + velocity * t + (k == 1 ? Vector3.down * (0.5f * gravity * t * t) : Vector3.zero);
                    float flat = new Vector2(p.x - center.x, p.z - center.z).magnitude;
                    if (flat <= radius && p.y + 0.9f >= minY && p.y <= maxY)
                        return true;
                }

                if (horizon <= 0f)
                    break;
            }

            return false;
        }

        /// <summary>
        /// Bond vers un point plus bas ou plus haut (goule qui saute d'un mur, d'une corniche ou d'un pont, D-035) : même
        /// arc de départ (hauteur <paramref name="height"/>), durée de vol jusqu'au niveau d'arrivée, vitesse horizontale
        /// au plus maxSpeed. Sur le même niveau, identique à LeapVelocity.
        /// </summary>
        public static Vector3 LeapVelocityTo(Vector3 from, Vector3 to, float maxSpeed, float height, float gravity, float overshoot = 1f)
        {
            float g = Mathf.Max(0.1f, gravity);
            float vy = Mathf.Sqrt(2f * Mathf.Max(0.05f, height) * g);
            float drop = from.y - to.y;                                  // > 0 : il saute vers le bas
            float disc = vy * vy + 2f * g * drop;
            if (disc < 0f)
            {
                vy = Mathf.Sqrt(-2f * g * drop) + 0.5f;                  // trop haut pour l'arc : il saute juste assez
                disc = vy * vy + 2f * g * drop;
            }

            float flight = (vy + Mathf.Sqrt(Mathf.Max(0f, disc))) / g;
            Vector3 flat = new Vector3(to.x - from.x, 0f, to.z - from.z);
            float distance = flat.magnitude;
            if (distance < 1e-3f)
                return Vector3.up * vy;
            float speed = Mathf.Min(maxSpeed, (distance + overshoot) / flight);
            return flat / distance * speed + Vector3.up * vy;
        }

        public static Vector3 LeapVelocity(Vector3 from, Vector3 to, float maxSpeed, float height, float gravity, float overshoot = 1f)
        {
            float vy = Mathf.Sqrt(2f * Mathf.Max(0.05f, height) * Mathf.Max(0.1f, gravity));
            float flight = 2f * vy / Mathf.Max(0.1f, gravity);
            Vector3 flat = new Vector3(to.x - from.x, 0f, to.z - from.z);
            float distance = flat.magnitude;
            if (distance < 1e-3f)
                return Vector3.up * vy;
            float speed = Mathf.Min(maxSpeed, (distance + overshoot) / flight);
            return flat / distance * speed + Vector3.up * vy;
        }
    }
}
