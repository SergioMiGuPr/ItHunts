using ItHunts.Movement;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Télékinésie de l'orbe unique (D-024), un tick à la fois, avant MovementMotor.Step (même tick, même entrée). Logique
    /// pure : elle décide et renvoie une commande (attraper, lancer, poser) que TelekinesisHolder applique aux corps
    /// physiques. Disponible en même temps que la traction et la répulsion, chacune sur sa touche :
    /// - F / X : attrape l'objet visé (il reste tenu sans maintenir) ; si un objet est tenu, le pose doucement ;
    /// - clic gauche / RT : lance l'objet tenu ; sans objet, ne fait rien (réservé à l'artefact, étape 5 bis).
    /// Aucune endurance ; après un lancer, cooldown avant d'attraper de nouveau (appui mémorisé). Refus : rien
    /// d'attrapable, trop lourd, recharge.
    /// </summary>
    public static class TelekinesisOrb
    {
        public static TelekinesisCommand Step(ref TelekinesisState t, in MovementState s, in MovementInput input,
            TelekinesisOrbSettings cfg, MovementSettings move, float dt, IGrabProbe probe)
        {
            int tick = s.Tick + 1;   // le tick que le moteur va simuler juste après
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.pressBuffer, dt);
            int cooldownTicks = MovementSettings.SecondsToTicks(cfg.cooldown, dt);

            // Visée.
            Vector3 eye = MagnetismOrb.EyePosition(s, move);
            Vector3 aimDir = MagnetismOrb.AimDirection(input.Yaw, input.Pitch);
            bool hit = probe.FindGrabbable(eye, aimDir, cfg.range, out GrabCandidate c);
            t.AimHit = hit;
            t.AimId = hit ? c.Id : 0;
            t.AimTooHeavy = hit && c.Mass > cfg.maxMass;
            t.AimDistance = hit ? c.Distance : 0f;
            t.AimAngle = hit ? c.Angle : 0f;

            var cmd = new TelekinesisCommand { Kind = TelekinesisCommandKind.Aucune };
            bool cooling = t.LastThrowTick != MovementState.NeverTick && tick - t.LastThrowTick < cooldownTicks;

            bool grab = IsFresh(input.GrabPressedTick, t.LastConsumedGrabPressTick, tick, bufferTicks);
            bool throwPress = IsFresh(input.ThrowPressedTick, t.LastConsumedThrowPressTick, tick, bufferTicks);

            if (grab)
            {
                if (t.Holding)
                {
                    cmd = Release(ref t, tick, TelekinesisCommandKind.Poser);
                    t.LastConsumedGrabPressTick = input.GrabPressedTick;
                }
                else if (cooling)
                {
                    // Mémorisé : part dès la fin de la recharge, sinon refusé une fois le délai de mémorisation passé.
                    if (tick - input.GrabPressedTick >= bufferTicks)
                        Refuse(ref t, tick, TelekinesisRefusal.Recharge, ref t.LastConsumedGrabPressTick, input.GrabPressedTick);
                }
                else if (!hit)
                    Refuse(ref t, tick, TelekinesisRefusal.RienAttrapable, ref t.LastConsumedGrabPressTick, input.GrabPressedTick);
                else if (c.Mass > cfg.maxMass)
                    Refuse(ref t, tick, TelekinesisRefusal.TropLourd, ref t.LastConsumedGrabPressTick, input.GrabPressedTick);
                else
                {
                    t.HeldId = c.Id;
                    t.GrabTick = tick;
                    t.LastAction = TelekinesisCommandKind.Attraper;
                    t.LastActionTick = tick;
                    t.LastConsumedGrabPressTick = input.GrabPressedTick;
                    cmd = new TelekinesisCommand { Kind = TelekinesisCommandKind.Attraper, TargetId = c.Id };
                }
            }

            if (throwPress)
            {
                // Sans objet en main, le clic gauche est réservé à l'artefact : consommé sans effet ni refus.
                t.LastConsumedThrowPressTick = input.ThrowPressedTick;
                if (t.Holding && cmd.Kind == TelekinesisCommandKind.Aucune)
                {
                    int id = t.HeldId;
                    cmd = Release(ref t, tick, TelekinesisCommandKind.Lancer);
                    cmd.AimDirection = aimDir;
                    t.LastThrownId = id;
                    t.LastThrowTick = tick;
                }
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
            return !t.Holding && t.AimHit && !t.AimTooHeavy && !cooling;
        }

        /// <summary>Peut-on lancer l'objet tenu maintenant ? (anneau vert du viseur)</summary>
        public static bool CanThrow(in TelekinesisState t) => t.Holding;

        /// <summary>
        /// Vitesse d'un lancer, toujours exactement dans la direction voulue (de l'objet vers le point sous le viseur) :
        /// throwSpeed, plus la part de la vitesse du joueur dans cette direction si elle est positive et si
        /// inheritPlayerVelocity (lancer en pleine course frappe plus fort). Le reste de la vitesse du joueur est ignoré :
        /// il ne dévie jamais l'objet (avant correction, toute la vitesse était ajoutée : 7 m d'écart à 30 m en course de
        /// côté, 10 m en traction).
        /// </summary>
        public static Vector3 ThrowVelocity(Vector3 direction, Vector3 playerVelocity, TelekinesisOrbSettings cfg)
        {
            Vector3 dir = direction.normalized;
            float bonus = cfg.inheritPlayerVelocity ? Mathf.Max(0f, Vector3.Dot(playerVelocity, dir)) : 0f;
            return dir * (cfg.throwSpeed + bonus);
        }

        private static TelekinesisCommand Release(ref TelekinesisState t, int tick, TelekinesisCommandKind kind)
        {
            var cmd = new TelekinesisCommand { Kind = kind, TargetId = t.HeldId };
            t.HeldId = 0;
            t.LastAction = kind;
            t.LastActionTick = tick;
            return cmd;
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
