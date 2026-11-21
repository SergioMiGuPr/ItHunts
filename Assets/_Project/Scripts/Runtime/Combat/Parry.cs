using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>État de la parade d'un joueur (temps en secondes, horloge du jeu). Données seulement : copiable, rejouable.</summary>
    public struct ParryState
    {
        public float LastPress;
        public float ReadyAt;
        public float LastSuccess;
        public int Successes;

        public static ParryState Create() => new ParryState
        {
            LastPress = float.NegativeInfinity, ReadyAt = 0f, LastSuccess = float.NegativeInfinity, Successes = 0,
        };
    }

    /// <summary>Événement de parade réussie (retour visuel et sonore).</summary>
    public struct ParryEvent
    {
        public Transform Player;
        public Transform Attacker;
        public Vector3 Point;
    }

    /// <summary>Règles de la parade (D-075), pures et testables.</summary>
    public static class ParryMath
    {
        public static bool Ready(in ParryState s, float now) => now >= s.ReadyAt;

        /// <summary>Appui : accepté si la parade est prête ; la recharge démarre (réussie ou non).</summary>
        public static bool Press(ref ParryState s, float now, ParrySettings cfg)
        {
            if (!Ready(s, now))
                return false;
            s.LastPress = now;
            s.ReadyAt = now + cfg.cooldown;
            return true;
        }

        /// <summary>
        /// Un impact maintenant est-il paré ? Oui si l'appui est au plus <c>window</c> avant, et si l'attaquant peut être paré
        /// (pas le traqueur : <paramref name="attackerParryable"/> faux).
        /// </summary>
        public static bool TryParry(ref ParryState s, float now, ParrySettings cfg, bool attackerParryable)
        {
            if (!attackerParryable || cfg == null)
                return false;
            float since = now - s.LastPress;
            if (since < 0f || since > cfg.window)
                return false;
            if (s.LastSuccess != s.LastPress)
            {
                s.Successes++;
                s.ReadyAt = s.LastPress + cfg.cooldown * cfg.cooldownAfterSuccess;
            }
            s.LastSuccess = s.LastPress;
            return true;
        }

        public static float CooldownRemaining(in ParryState s, float now) => Mathf.Max(0f, s.ReadyAt - now);

        /// <summary>Remplissage de l'icône de recharge (1 = prête).</summary>
        public static float Fill(in ParryState s, float now, ParrySettings cfg) =>
            cfg == null || cfg.cooldown <= 0f ? 1f : 1f - Mathf.Clamp01(CooldownRemaining(s, now) / cfg.cooldown);

        /// <summary>Un attaquant parable : ni intuable (traqueur, D-035), ni sans corps.</summary>
        public static bool Parryable(Component attacker)
        {
            if (attacker == null)
                return false;
            var d = attacker.GetComponentInParent<Damageable>();
            return d == null || !d.ImmuneToOrbs;
        }
    }

    /// <summary>Canal des parades réussies.</summary>
    public static class ParryEvents
    {
        public static event System.Action<ParryEvent> Parried;
        public static void Raise(in ParryEvent e) => Parried?.Invoke(e);
    }
}
