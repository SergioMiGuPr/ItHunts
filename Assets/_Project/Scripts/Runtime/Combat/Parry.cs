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

        /// <summary>Dernier coup reçu pendant qu'on pouvait parer (debug) : instant, résultat, écart appui − impact (s, négatif : avant).</summary>
        public float LastImpact;
        public ParryResult LastResult;
        public float LastOffset;
        public float ResultTime;
        /// <summary>Coup reçu sans appui récent : un appui juste après dira « trop tard de X ms ».</summary>
        public bool AwaitLate;

        public static ParryState Create() => new ParryState
        {
            LastPress = float.NegativeInfinity, ReadyAt = 0f, LastSuccess = float.NegativeInfinity, Successes = 0,
            LastImpact = float.NegativeInfinity, LastResult = ParryResult.Aucun, ResultTime = float.NegativeInfinity,
        };
    }

    /// <summary>Résultat de la dernière parade (debug).</summary>
    public enum ParryResult : byte { Aucun, Reussie, TropTot, TropTard, Vide }

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

        /// <summary>Un appui au plus tant après un coup reçu est compté « trop tard » (debug) ; un appui au plus tant avant, « trop tôt ».</summary>
        public const float MissReport = 0.6f;

        /// <summary>Appui : accepté si la parade est prête ; la recharge démarre (réussie ou non).</summary>
        public static bool Press(ref ParryState s, float now, ParrySettings cfg)
        {
            if (!Ready(s, now))
                return false;
            s.LastPress = now;
            s.ReadyAt = now + cfg.cooldown;
            if (s.AwaitLate && now - s.LastImpact <= MissReport)
                Report(ref s, ParryResult.TropTard, now - s.LastImpact, now);
            s.AwaitLate = false;
            return true;
        }

        private static void Report(ref ParryState s, ParryResult r, float offset, float now)
        {
            s.LastResult = r;
            s.LastOffset = offset;
            s.ResultTime = now;
        }

        /// <summary>Texte du debug : « Parade réussie (appui 120 ms avant l'impact) », « ratée : trop tard de 40 ms »…</summary>
        public static string Describe(in ParryState s, ParrySettings cfg)
        {
            int ms = Mathf.RoundToInt(Mathf.Abs(s.LastOffset) * 1000f);
            string when = s.LastOffset <= 0f ? "avant" : "après";
            int win = cfg != null ? Mathf.RoundToInt(cfg.window * 1000f) : 0, late = cfg != null ? Mathf.RoundToInt(cfg.lateWindow * 1000f) : 0;
            return s.LastResult switch
            {
                ParryResult.Reussie => $"Parade RÉUSSIE : appui {ms} ms {when} l'impact (fenêtre {win} ms avant{(late > 0 ? $", {late} ms après" : "")})",
                ParryResult.TropTot => $"Parade ratée : appui {ms} ms avant l'impact, trop tôt de {ms - win} ms",
                ParryResult.TropTard => $"Parade ratée : appui {ms} ms après l'impact, trop tard de {ms - late} ms",
                ParryResult.Vide => "Parade dans le vide (aucun coup)",
                _ => "",
            };
        }

        /// <summary>
        /// Coup au contact parable après coup (lateWindow) : un appui dans (impact, impact + lateWindow] pare ; vrai alors.
        /// </summary>
        public static bool TryLateParry(ref ParryState s, float impact, float now, ParrySettings cfg, bool attackerParryable)
        {
            if (!attackerParryable || cfg == null || cfg.lateWindow <= 0f)
                return false;
            float after = s.LastPress - impact;
            if (after <= 0f || after > cfg.lateWindow || s.LastPress > now)
                return false;
            if (s.LastSuccess != s.LastPress)
            {
                s.Successes++;
                s.ReadyAt = s.LastPress + cfg.cooldown * cfg.cooldownAfterSuccess;
            }

            s.LastSuccess = s.LastPress;
            s.AwaitLate = false;
            Report(ref s, ParryResult.Reussie, after, now);
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
            s.LastImpact = now;
            if (since < 0f || since > cfg.window)
            {
                // Raté : trop tôt (appui récent), ou pas d'appui (un appui juste après dira de combien c'était trop tard).
                if (since >= 0f && since <= MissReport)
                {
                    Report(ref s, ParryResult.TropTot, -since, now);
                    s.AwaitLate = false;
                }
                else
                    s.AwaitLate = true;
                return false;
            }

            Report(ref s, ParryResult.Reussie, -since, now);
            s.AwaitLate = false;
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
