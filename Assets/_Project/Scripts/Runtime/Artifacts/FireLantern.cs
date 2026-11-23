using ItHunts.Movement;
using ItHunts.Settings;

namespace ItHunts.Artifacts
{
    /// <summary>État de la lanterne de feu d'un joueur : données seulement, copiables (réseau). Les délais sont en ticks.</summary>
    public struct LanternState
    {
        public bool Jetting;
        public int JetStartTick;
        /// <summary>Fin du dernier jet (début de la recharge) ; NeverTick avant le premier.</summary>
        public int JetEndTick;
        public int JetCount;
        /// <summary>Lanterne inactive : l'orbe tient un objet (pas de jet).</summary>
        public bool Inactive;
        public int LastConsumedFirePressTick;
        /// <summary>Charges de jet en plus (Souffle double, R7, D-100) : −1 avant le premier tick (pleines) ; un jet pendant la recharge en dépense une.</summary>
        public float ExtraCharges;
        /// <summary>Jets enchaînés sans laisser finir la recharge (le 2e jet consécutif brûle plus, R7 Or).</summary>
        public int Consecutive;

        public static LanternState Create() => new LanternState
        {
            JetStartTick = MovementState.NeverTick,
            JetEndTick = MovementState.NeverTick,
            LastConsumedFirePressTick = MovementState.NeverTick,
            ExtraCharges = -1f,
        };
    }

    /// <summary>Ce qui s'est passé ce tick.</summary>
    public struct LanternStep
    {
        public bool Started;
        /// <summary>Le jet brûle ce tick (le monde cherche les cibles dans le cône).</summary>
        public bool Burning;
        public bool Ended;
    }

    /// <summary>
    /// La lanterne de feu, un tick à la fois (D-065), logique pure et testable. Même clic que le lancer (D-024) : sans objet tenu,
    /// un clic lance un jet de jetDuration ; à la fin du jet, recharge de cooldown avant le suivant. Un clic un peu trop tôt part
    /// dès que possible (pressBuffer). Attraper un objet pendant le jet l'arrête (la recharge commence) ; pendant que l'orbe tient
    /// un objet, le clic est un lancer et la lanterne ne part pas. Charges en plus (R7, D-100) : un jet peut partir pendant la
    /// recharge tant qu'il reste une charge en plus ; elles reviennent une par une.
    /// </summary>
    public static class FireLantern
    {
        public static LanternStep Step(ref LanternState s, int tick, int firePressTick, bool heldAtTickStart, bool holdingNow,
            FireLanternSettings cfg, float dt)
        {
            var step = new LanternStep();
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.pressBuffer, dt);
            int jetTicks = System.Math.Max(1, MovementSettings.SecondsToTicks(cfg.jetDuration, dt));
            if (s.ExtraCharges < 0f)
                s.ExtraCharges = cfg.jetExtraCharges;
            if (cfg.jetExtraCharges > 0 && cfg.cooldown > 0f)
                s.ExtraCharges = UnityEngine.Mathf.Min(cfg.jetExtraCharges, s.ExtraCharges + dt / cfg.cooldown * UnityEngine.Mathf.Max(0.1f, cfg.extraChargeRechargeFactor));
            else
                s.ExtraCharges = UnityEngine.Mathf.Min(s.ExtraCharges, cfg.jetExtraCharges);
            bool fire = IsFresh(firePressTick, s.LastConsumedFirePressTick, tick, bufferTicks);
            if (fire && (heldAtTickStart || holdingNow))
            {
                s.LastConsumedFirePressTick = firePressTick;             // clic du lancer
                fire = false;
            }

            s.Inactive = holdingNow;
            if (s.Jetting && (tick - s.JetStartTick >= jetTicks || holdingNow))
            {
                s.Jetting = false;
                s.JetEndTick = tick;
                step.Ended = true;
            }

            if (!holdingNow && fire)
            {
                bool cooled = CooldownProgress(s, tick, cfg, dt) >= 1f;
                if (!s.Jetting && (cooled || s.ExtraCharges >= 1f))
                {
                    if (!cooled)
                        s.ExtraCharges -= 1f;                            // jet pendant la recharge : une charge en plus
                    s.Consecutive = cooled ? 0 : s.Consecutive + 1;
                    s.LastConsumedFirePressTick = firePressTick;
                    s.Jetting = true;
                    s.JetStartTick = tick;
                    s.JetCount++;
                    step.Started = true;
                }
                else if (tick - firePressTick >= bufferTicks)
                {
                    s.LastConsumedFirePressTick = firePressTick;         // trop tôt et plus mémorisé : perdu
                }
            }

            step.Burning = s.Jetting;
            return step;
        }

        /// <summary>Prête : pas de jet en cours et recharge finie (ou une charge en plus disponible).</summary>
        public static bool Ready(in LanternState s, int tick, FireLanternSettings cfg, float dt) =>
            !s.Jetting && (CooldownProgress(s, tick, cfg, dt) >= 1f || s.ExtraCharges >= 1f);

        /// <summary>Recharge (0..1) : 0 pendant le jet et juste après, 1 quand un nouveau jet peut partir.</summary>
        public static float CooldownProgress(in LanternState s, int tick, FireLanternSettings cfg, float dt)
        {
            if (s.Jetting)
                return 0f;
            if (s.JetEndTick == MovementState.NeverTick)
                return 1f;
            int ticks = MovementSettings.SecondsToTicks(cfg.cooldown, dt);
            return ticks <= 0 ? 1f : UnityEngine.Mathf.Clamp01((tick - s.JetEndTick) / (float)ticks);
        }

        /// <summary>Avancement du jet en cours (0..1) ; 0 sans jet.</summary>
        public static float JetProgress(in LanternState s, int tick, FireLanternSettings cfg, float dt)
        {
            if (!s.Jetting)
                return 0f;
            int ticks = System.Math.Max(1, MovementSettings.SecondsToTicks(cfg.jetDuration, dt));
            return UnityEngine.Mathf.Clamp01((tick - s.JetStartTick) / (float)ticks);
        }

        /// <summary>Avance la recharge de seconds s (Mèche vive Or et Mythique, Orbe vorace).</summary>
        public static void ReduceCooldown(ref LanternState s, float seconds, float dt)
        {
            if (s.JetEndTick != MovementState.NeverTick && seconds > 0f)
                s.JetEndTick -= MovementSettings.SecondsToTicks(seconds, dt);
        }

        private static bool IsFresh(int pressTick, int consumedTick, int tick, int bufferTicks)
        {
            int since = tick - pressTick;
            return pressTick > 0 && pressTick > consumedTick && since >= 0 && since <= bufferTicks;
        }
    }
}
