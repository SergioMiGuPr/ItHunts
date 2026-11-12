using ItHunts.Movement;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Orbe de télékinésie, un tick à la fois, avant MovementMotor.Step (même tick, même entrée). Logique pure : elle
    /// décide et renvoie une commande (attraper, lancer, poser, lâcher) que TelekinesisHolder applique aux corps
    /// physiques. Commandes (télékinésie active) :
    /// - clic gauche / RT : attrape l'objet visé (il reste tenu sans maintenir) ; si un objet est tenu, le pose ;
    /// - clic droit / LT : lance l'objet tenu.
    /// Coûts en endurance propre (grabCost, throwCost) ; la régénération attend regenDelay après une dépense. Après un
    /// lancer, cooldown avant d'attraper de nouveau. Refus : rien d'attrapable, trop lourd, pas d'endurance, recharge,
    /// rien en main.
    /// </summary>
    public static class TelekinesisOrb
    {
        public static TelekinesisCommand Step(ref TelekinesisState t, in MovementState s, in MovementInput input,
            TelekinesisOrbSettings cfg, StaminaSettings stamina, MovementSettings move, float dt, IGrabProbe probe,
            bool dropHeld)
        {
            int tick = s.Tick + 1;   // le tick que le moteur va simuler juste après
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.pressBuffer, dt);
            int cooldownTicks = MovementSettings.SecondsToTicks(cfg.cooldown, dt);
            int regenDelayTicks = MovementSettings.SecondsToTicks(stamina.regenDelay, dt);

            // Endurance : pleine au premier tick, puis régénération après le délai.
            if (t.Stamina < 0f)
                t.Stamina = stamina.max;
            if (t.LastSpendTick == MovementState.NeverTick || tick - t.LastSpendTick >= regenDelayTicks)
                t.Stamina = Mathf.Min(stamina.max, t.Stamina + stamina.regenPerSecond * dt);

            // Visée.
            Vector3 eye = MagnetismOrb.EyePosition(s, move);
            Vector3 aimDir = MagnetismOrb.AimDirection(input.Yaw, input.Pitch);
            bool hit = probe.FindGrabbable(eye, aimDir, cfg.range, out GrabCandidate c);
            t.AimHit = hit;
            t.AimId = hit ? c.Id : 0;
            t.AimTooHeavy = hit && c.Mass > cfg.maxMass;
            t.AimDistance = hit ? c.Distance : 0f;

            var cmd = new TelekinesisCommand { Kind = TelekinesisCommandKind.Aucune };
            bool cooling = t.LastThrowTick != MovementState.NeverTick && tick - t.LastThrowTick < cooldownTicks;

            // Échange d'orbe (ou mort) : l'objet tenu est lâché avec sa vitesse.
            if (dropHeld && t.Holding)
                cmd = Release(ref t, tick, TelekinesisCommandKind.Lacher);

            bool primary = IsFresh(input.AttractPressedTick, t.LastConsumedPrimaryPressTick, tick, bufferTicks);
            bool secondary = IsFresh(input.RepelPressedTick, t.LastConsumedSecondaryPressTick, tick, bufferTicks);

            if (primary && cmd.Kind == TelekinesisCommandKind.Aucune)
            {
                if (t.Holding)
                {
                    cmd = Release(ref t, tick, TelekinesisCommandKind.Poser);
                    t.LastConsumedPrimaryPressTick = input.AttractPressedTick;
                }
                else if (cooling)
                {
                    if (tick - input.AttractPressedTick >= bufferTicks)
                        Refuse(ref t, tick, TelekinesisRefusal.Recharge, ref t.LastConsumedPrimaryPressTick, input.AttractPressedTick);
                }
                else if (!hit)
                    Refuse(ref t, tick, TelekinesisRefusal.RienAttrapable, ref t.LastConsumedPrimaryPressTick, input.AttractPressedTick);
                else if (c.Mass > cfg.maxMass)
                    Refuse(ref t, tick, TelekinesisRefusal.TropLourd, ref t.LastConsumedPrimaryPressTick, input.AttractPressedTick);
                else if (t.Stamina < cfg.grabCost)
                    Refuse(ref t, tick, TelekinesisRefusal.PasDEndurance, ref t.LastConsumedPrimaryPressTick, input.AttractPressedTick);
                else
                {
                    Spend(ref t, tick, cfg.grabCost);
                    t.HeldId = c.Id;
                    t.GrabTick = tick;
                    t.LastAction = TelekinesisCommandKind.Attraper;
                    t.LastActionTick = tick;
                    t.LastConsumedPrimaryPressTick = input.AttractPressedTick;
                    cmd = new TelekinesisCommand { Kind = TelekinesisCommandKind.Attraper, TargetId = c.Id };
                }
            }

            if (secondary && cmd.Kind == TelekinesisCommandKind.Aucune)
            {
                if (!t.Holding)
                    Refuse(ref t, tick, TelekinesisRefusal.RienEnMain, ref t.LastConsumedSecondaryPressTick, input.RepelPressedTick);
                else if (t.Stamina < cfg.throwCost)
                    Refuse(ref t, tick, TelekinesisRefusal.PasDEndurance, ref t.LastConsumedSecondaryPressTick, input.RepelPressedTick);
                else
                {
                    Spend(ref t, tick, cfg.throwCost);
                    int id = t.HeldId;
                    cmd = Release(ref t, tick, TelekinesisCommandKind.Lancer);
                    cmd.AimDirection = aimDir;
                    t.LastThrownId = id;
                    t.LastThrowTick = tick;
                    t.LastConsumedSecondaryPressTick = input.RepelPressedTick;
                }
            }
            else if (secondary)
            {
                t.LastConsumedSecondaryPressTick = input.RepelPressedTick;
            }

            cooling = t.LastThrowTick != MovementState.NeverTick && tick - t.LastThrowTick < cooldownTicks;
            t.Status = t.Holding ? OrbStatus.EnUsage : cooling ? OrbStatus.Recharge : OrbStatus.Prete;
            return cmd;
        }

        /// <summary>Le monde physique a perdu l'objet tenu (coincé trop loin, détruit) : plus rien en main.</summary>
        public static void NotifyLost(ref TelekinesisState t, int tick)
        {
            if (!t.Holding)
                return;
            t.HeldId = 0;
            t.LastAction = TelekinesisCommandKind.Lacher;
            t.LastActionTick = tick;
            t.Status = OrbStatus.Prete;
        }

        /// <summary>Peut-on attraper l'objet visé maintenant ? (anneau violet du viseur)</summary>
        public static bool CanGrab(in TelekinesisState t, TelekinesisOrbSettings cfg, int tick, float dt)
        {
            int cooldownTicks = MovementSettings.SecondsToTicks(cfg.cooldown, dt);
            bool cooling = t.LastThrowTick != MovementState.NeverTick && tick - t.LastThrowTick < cooldownTicks;
            return !t.Holding && t.AimHit && !t.AimTooHeavy && !cooling && t.Stamina >= cfg.grabCost;
        }

        /// <summary>Peut-on lancer l'objet tenu maintenant ? (anneau vert du viseur)</summary>
        public static bool CanThrow(in TelekinesisState t, TelekinesisOrbSettings cfg) => t.Holding && t.Stamina >= cfg.throwCost;

        /// <summary>
        /// Vitesse d'un lancer : throwSpeed dans la direction voulue, plus la vitesse du joueur si inheritPlayerVelocity
        /// (lancer en pleine course frappe plus fort).
        /// </summary>
        public static Vector3 ThrowVelocity(Vector3 direction, Vector3 playerVelocity, TelekinesisOrbSettings cfg)
        {
            Vector3 v = direction.normalized * cfg.throwSpeed;
            return cfg.inheritPlayerVelocity ? v + playerVelocity : v;
        }

        private static TelekinesisCommand Release(ref TelekinesisState t, int tick, TelekinesisCommandKind kind)
        {
            var cmd = new TelekinesisCommand { Kind = kind, TargetId = t.HeldId };
            t.HeldId = 0;
            t.LastAction = kind;
            t.LastActionTick = tick;
            return cmd;
        }

        private static void Spend(ref TelekinesisState t, int tick, float cost)
        {
            t.Stamina -= cost;
            t.LastSpendTick = tick;
        }

        private static bool IsFresh(int pressTick, int consumedTick, int tick, int bufferTicks)
        {
            int since = tick - pressTick;
            return pressTick > 0 && pressTick > consumedTick && since >= 0 && since <= bufferTicks;
        }

        private static void Refuse(ref TelekinesisState t, int tick, TelekinesisRefusal refusal, ref int consumedTick, int pressTick)
        {
            t.LastRefusalTick = tick;
            t.LastRefusal = refusal;
            consumedTick = pressTick;
        }
    }
}
