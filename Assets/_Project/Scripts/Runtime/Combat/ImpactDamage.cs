using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    public enum HitTier : byte
    {
        Petit,
        Moyen,
        Gros,
    }

    /// <summary>Calculs purs des dégâts d'impact (D-025) : testables, rejouables par un serveur plus tard.</summary>
    public static class ImpactDamage
    {
        /// <summary>
        /// Dégâts = masse × (vitesse d'impact − seuil) × facteur ; 0 au seuil et en dessous. Pour une chute, le seuil est
        /// fallSpeedThreshold (plus bas).
        /// </summary>
        public static float Compute(float mass, float impactSpeed, DamageSettings cfg, bool falling = false) =>
            Mathf.Max(0f, mass) * Mathf.Max(0f, impactSpeed - (falling ? cfg.fallSpeedThreshold : cfg.speedThreshold)) * cfg.damageFactor;

        /// <summary>
        /// Une chute : la vitesse relative est surtout verticale (part au moins fallVerticalShare) et l'objet est
        /// au-dessus du point de contact.
        /// </summary>
        public static bool IsFall(Vector3 relativeVelocity, Vector3 sourcePosition, Vector3 contactPoint, DamageSettings cfg)
        {
            float speed = relativeVelocity.magnitude;
            return speed > 1e-3f && Mathf.Abs(relativeVelocity.y) / speed >= cfg.fallVerticalShare && sourcePosition.y > contactPoint.y;
        }

        public static HitTier Tier(float damage, DamageSettings cfg) =>
            damage >= cfg.bigHit ? HitTier.Gros : damage >= cfg.mediumHit ? HitTier.Moyen : HitTier.Petit;
    }

    /// <summary>
    /// Un même objet ne blesse le même ennemi qu'une fois par intervalle (rebonds sur place, contact qui frotte).
    /// État du monde (pas d'un joueur).
    /// </summary>
    public sealed class HitCooldown
    {
        private readonly Dictionary<(int source, int target), float> _last = new Dictionary<(int, int), float>();

        /// <summary>Vrai si le coup compte (et le note) ; faux s'il arrive moins de <paramref name="cooldown"/> s après le précédent.</summary>
        public bool TryHit(int source, int target, float time, float cooldown)
        {
            var key = (source, target);
            if (_last.TryGetValue(key, out float last) && time - last < cooldown)
                return false;
            _last[key] = time;
            if (_last.Count > 512)
                Prune(time, cooldown);
            return true;
        }

        public void Clear() => _last.Clear();

        private void Prune(float time, float cooldown)
        {
            var old = new List<(int, int)>();
            foreach (KeyValuePair<(int, int), float> e in _last)
                if (time - e.Value >= cooldown)
                    old.Add(e.Key);
            foreach ((int, int) k in old)
                _last.Remove(k);
        }
    }

    /// <summary>Points de vie d'une cible : données seulement, copiables (réseau).</summary>
    public struct HealthState
    {
        public float Max;
        public float Current;
        public bool Dead;
        public float DiedAt;

        public static HealthState Create(float max) => new HealthState { Max = max, Current = max, Dead = false, DiedAt = float.NegativeInfinity };

        /// <summary>Retire des PV ; renvoie les dégâts réellement pris (0 si déjà mort) et si ce coup tue.</summary>
        public static float Apply(ref HealthState h, float damage, float time, out bool killed)
        {
            killed = false;
            if (h.Dead || damage <= 0f)
                return 0f;
            float taken = Mathf.Min(h.Current, damage);
            h.Current -= taken;
            if (h.Current <= 0f)
            {
                h.Current = 0f;
                h.Dead = true;
                h.DiedAt = time;
                killed = true;
            }

            return taken;
        }

        /// <summary>Réapparition : tous les PV, si le délai est écoulé. Vrai si elle a eu lieu.</summary>
        public static bool TryRespawn(ref HealthState h, float time, float delay)
        {
            if (!h.Dead || time - h.DiedAt < delay)
                return false;
            h.Current = h.Max;
            h.Dead = false;
            return true;
        }
    }

    /// <summary>Un coup porté (retour visuel et sonore, chiffres de dégâts).</summary>
    public struct DamageEvent
    {
        public Damageable Target;
        public float Damage;
        public float ImpactSpeed;
        public float SourceMass;
        public Vector3 Point;
        public bool Killed;
        public bool Falling;
        public bool Area;
        public HitTier Tier;
        public float Time;

        /// <summary>Balle du barillet (D-032) : ni vitesse d'impact ni masse.</summary>
        public bool Bullet;
    }

    /// <summary>Canal des coups portés dans le monde. Chaque joueur affiche les chiffres pour sa propre caméra.</summary>
    public static class DamageEvents
    {
        public static event System.Action<DamageEvent> Hit;

        public static void Raise(in DamageEvent e) => Hit?.Invoke(e);
    }
}
