using ItHunts.Movement;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Orbs
{
    /// <summary>
    /// Orbe de magnétisme, un tick à la fois, juste AVANT MovementMotor.Step (même tick, même entrée). Classe pure :
    /// tout passe par les paramètres (tests, prédiction réseau). Toute surface répond ; aucune charge de mouvement.
    /// - Attraction (bouton maintenu) : démarre une traction vers le point visé (fixé au clic). Le moteur la simule
    ///   (état Traction : ligne droite, sans gravité ni friction, la vitesse ne baisse jamais). Elle s'arrête au
    ///   relâchement, à l'arrivée, si la ligne de vue est coupée, après la durée max, sur un saut, une répulsion, un
    ///   dash ou un obstacle. Chaque traction coûte une charge d'attraction (compteur séparé des charges de mouvement,
    ///   rechargé une par une).
    /// - Répulsion (appui) : impulsion instantanée à l'opposé du point visé ; la vitesse horizontale ne baisse jamais,
    ///   son gain suit le plafond commun des gains. Une seule charge, qui revient repelCooldown après chaque usage.
    /// Pendant un dash, la vitesse gardée est celle de sortie du dash (pas le pic).
    /// </summary>
    public static class MagnetismOrb
    {
        /// <summary>Marge pour la ligne de vue : la surface visée elle-même ne la coupe pas.</summary>
        private const float LineOfSightMargin = 0.25f;

        public static void Step(ref MagnetismState m, ref MovementState s, in MovementInput input,
            MagnetismOrbSettings cfg, MovementSettings move, float dt, IAimProbe aim)
        {
            int tick = s.Tick + 1;   // le tick que le moteur va simuler juste après
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.pressBuffer, dt);
            int repelCooldownTicks = MovementSettings.SecondsToTicks(cfg.repelCooldown, dt);
            int repelUseTicks = MovementSettings.SecondsToTicks(cfg.repelUseDuration, dt);
            int maxTractionTicks = Mathf.Max(1, MovementSettings.SecondsToTicks(cfg.tractionMaxDuration, dt));
            int dashGraceTicks = MovementSettings.SecondsToTicks(move.dashFrictionGrace, dt);

            // Charges d'attraction : pleines au premier tick, puis une par une avec le temps.
            if (m.AttractCharges < 0f)
                m.AttractCharges = cfg.attractCharges;
            m.AttractCharges = Mathf.Min(cfg.attractCharges, m.AttractCharges + dt / cfg.attractChargeRechargeTime);

            // Traction terminée par le moteur au tick précédent (dash, obstacle) ou par une téléportation.
            if (m.Tracting && s.Mode != MovementMode.Traction)
                FinishTraction(ref m, s, s.TractionEndTick != MovementState.NeverTick ? s.TractionEndTick : tick - 1,
                    s.TractionEndReason);
            if (m.Tracting)
                m.LastTractionPeakSpeed = Mathf.Max(m.LastTractionPeakSpeed, s.Velocity.magnitude);

            // Visée de ce tick (aussi pour l'orbe visible et le debug).
            Vector3 eye = EyePosition(s, move);
            Vector3 aimDir = AimDirection(input.Yaw, input.Pitch);
            bool hit = aim.Raycast(eye, aimDir, cfg.attractRange, out AimHit h);
            m.AimHit = hit;
            m.AimDistance = hit ? h.Distance : 0f;
            m.AimPoint = hit ? h.Point : Vector3.zero;
            m.AimNormal = hit ? h.Normal : Vector3.up;
            m.RepelInRange = hit && h.Distance <= cfg.repelRange;

            bool attractPress = IsFresh(input.AttractPressedTick, m.LastConsumedAttractPressTick, tick, bufferTicks);
            bool repelPress = IsFresh(input.RepelPressedTick, m.LastConsumedRepelPressTick, tick, bufferTicks);

            // 1. Traction en cours : conditions d'arrêt (la vitesse reste telle quelle).
            if (m.Tracting)
            {
                Vector3 to = s.TractionPoint - eye;
                float distance = to.magnitude;
                TractionEnd end = TractionEnd.Aucune;
                if (!input.AttractHeld)
                    end = TractionEnd.Relache;
                else if (JumpPressedDuringTraction(s, input, move, dt, tick))
                    end = TractionEnd.Saut;                       // le saut part ensuite dans le moteur, ce tick-ci
                else if (distance <= cfg.tractionArrivalDistance)
                    end = TractionEnd.Arrivee;
                else if (tick - s.TractionStartTick >= maxTractionTicks)
                    end = TractionEnd.DureeMax;
                else if (aim.Raycast(eye, to / distance, distance, out AimHit los) && los.Distance < distance - LineOfSightMargin)
                    end = TractionEnd.LigneCoupee;

                if (end != TractionEnd.Aucune)
                    EndTraction(ref m, ref s, tick, end);
            }

            // 2. Répulsion : impulsion instantanée.
            if (repelPress)
            {
                bool ready = m.LastRepelTick == MovementState.NeverTick || tick - m.LastRepelTick >= repelCooldownTicks;
                if (!ready)
                {
                    // Appui mémorisé : il part dès la fin de la recharge, ou il est refusé à la fin de la mémorisation.
                    if (tick - input.RepelPressedTick >= bufferTicks)
                        Refuse(ref m, tick, MagnetMode.Repulsion, MagnetRefusal.Recharge, ref m.LastConsumedRepelPressTick, input.RepelPressedTick);
                }
                else if (!hit || h.Distance > cfg.repelRange)
                {
                    Refuse(ref m, tick, MagnetMode.Repulsion, MagnetRefusal.RienAPortee, ref m.LastConsumedRepelPressTick, input.RepelPressedTick);
                }
                else
                {
                    if (m.Tracting)
                        EndTraction(ref m, ref s, tick, TractionEnd.Repulsion);
                    if (s.DashTicksLeft > 0)
                        MovementMotor.EndDash(ref s, s.Position, dashGraceTicks);   // vitesse de sortie, pas le pic

                    Vector3 toPoint = h.Point - eye;
                    Vector3 dir = toPoint.sqrMagnitude > 1e-6f ? -toPoint.normalized : -aimDir;
                    Vector3 before = s.Velocity;
                    s.Velocity = MovementMath.RepelVelocity(s.Velocity, dir, cfg.repelImpulse, move.movementGainCap);
                    s.Velocity = MovementMath.ClampHorizontal(s.Velocity, move.maxHorizontalSpeed);
                    s.JumpRising = false;                               // pas de coupe au relâchement du saut
                    if (s.Velocity.y > 1e-3f)
                    {
                        s.Mode = MovementMode.Air;                      // on décolle : la glissade s'arrête, sans perte
                        s.Grounded = false;
                        s.LastGroundedTick = MovementState.NeverTick;   // pas de saut au sol via le coyote time
                    }
                    else if (s.Grounded)
                    {
                        int graceTicks = MovementSettings.SecondsToTicks(cfg.groundFrictionGrace, dt);
                        s.DashGraceUntilTick = Mathf.Max(s.DashGraceUntilTick, tick + 1 + graceTicks);
                    }

                    m.LastRepelTick = tick;
                    m.LastMode = MagnetMode.Repulsion;
                    m.LastRepelVelocityBefore = before;
                    m.LastRepelVelocityAfter = s.Velocity;
                    m.LastRepelPoint = h.Point;
                    m.LastRepelNormal = h.Normal;
                    m.LastConsumedRepelPressTick = input.RepelPressedTick;
                }
            }

            // 3. Attraction : départ de la traction (bouton toujours tenu), une charge d'attraction.
            if (attractPress && !m.Tracting)
            {
                if (m.AttractCharges < 1f)
                {
                    // Appui mémorisé : il part si une charge revient pendant la mémorisation, sinon il est refusé.
                    if (tick - input.AttractPressedTick >= bufferTicks)
                        Refuse(ref m, tick, MagnetMode.Attraction, MagnetRefusal.PasDeCharge, ref m.LastConsumedAttractPressTick, input.AttractPressedTick);
                }
                else if (!input.AttractHeld)
                {
                    m.LastConsumedAttractPressTick = input.AttractPressedTick;   // déjà relâché : rien
                }
                else if (!hit)
                {
                    Refuse(ref m, tick, MagnetMode.Attraction, MagnetRefusal.RienAPortee, ref m.LastConsumedAttractPressTick, input.AttractPressedTick);
                }
                else
                {
                    if (s.DashTicksLeft > 0)
                        MovementMotor.EndDash(ref s, s.Position, dashGraceTicks);   // vitesse de sortie, pas le pic
                    s.Mode = MovementMode.Traction;
                    s.TractionPoint = h.Point;
                    s.TractionAcceleration = cfg.tractionAcceleration;
                    s.TractionSpeed = cfg.tractionSpeed;
                    s.TractionStartTick = tick;
                    s.TractionEndReason = TractionEnd.Aucune;
                    s.Grounded = false;
                    s.LastGroundedTick = MovementState.NeverTick;
                    s.JumpRising = false;

                    m.Tracting = true;
                    m.AttractCharges -= 1f;
                    m.LastMode = MagnetMode.Attraction;
                    m.LastTractionStartSpeed = s.Velocity.magnitude;
                    m.LastTractionPeakSpeed = m.LastTractionStartSpeed;
                    m.LastConsumedAttractPressTick = input.AttractPressedTick;
                }
            }
            else if (attractPress)
            {
                m.LastConsumedAttractPressTick = input.AttractPressedTick;
            }

            // États affichés.
            m.AttractStatus = m.Tracting ? OrbStatus.EnUsage
                : m.AttractCharges < 1f ? OrbStatus.Recharge
                : OrbStatus.Prete;
            int sinceRepel = tick - m.LastRepelTick;
            m.RepelStatus = m.LastRepelTick == MovementState.NeverTick ? OrbStatus.Prete
                : sinceRepel < repelUseTicks ? OrbStatus.EnUsage
                : sinceRepel < repelCooldownTicks ? OrbStatus.Recharge
                : OrbStatus.Prete;
            m.Status = m.AttractStatus == OrbStatus.EnUsage || m.RepelStatus == OrbStatus.EnUsage ? OrbStatus.EnUsage
                : m.AttractStatus == OrbStatus.Recharge || m.RepelStatus == OrbStatus.Recharge ? OrbStatus.Recharge
                : OrbStatus.Prete;
        }

        /// <summary>Position des yeux (le rayon et la traction partent de là).</summary>
        public static Vector3 EyePosition(in MovementState s, MovementSettings move) =>
            s.Position + Vector3.up * (s.Crouched ? move.crouchEyeHeight : move.eyeHeight);

        /// <summary>Direction de la vue (pitch positif : vers le bas, comme la caméra).</summary>
        public static Vector3 AimDirection(float yaw, float pitch) => Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;

        private static bool IsFresh(int pressTick, int consumedTick, int tick, int bufferTicks)
        {
            int since = tick - pressTick;
            return pressTick > 0 && pressTick > consumedTick && since >= 0 && since <= bufferTicks;
        }

        /// <summary>Nouvel appui de saut depuis le début de la traction (même règle de mémorisation que le moteur).</summary>
        private static bool JumpPressedDuringTraction(in MovementState s, in MovementInput input, MovementSettings move,
            float dt, int tick)
        {
            int bufferTicks = MovementSettings.SecondsToTicks(move.jumpBuffer, dt);
            int since = tick - input.JumpPressedTick;
            return input.JumpPressedTick > s.LastConsumedJumpPressTick && input.JumpPressedTick >= s.TractionStartTick
                   && since >= 0 && since <= bufferTicks;
        }

        /// <summary>Arrête la traction maintenant (avant le pas du moteur) ; la vitesse reste telle quelle.</summary>
        private static void EndTraction(ref MagnetismState m, ref MovementState s, int tick, TractionEnd reason)
        {
            if (s.Mode == MovementMode.Traction)
                s.Mode = MovementMode.Air;
            s.TractionEndTick = tick;
            s.TractionEndReason = reason;
            FinishTraction(ref m, s, tick, reason);
        }

        private static void FinishTraction(ref MagnetismState m, in MovementState s, int endTick, TractionEnd reason)
        {
            m.Tracting = false;
            m.LastTractionEndTick = endTick;
            m.LastTractionEnd = reason;
            m.LastTractionEndSpeed = s.Velocity.magnitude;
            m.LastTractionPeakSpeed = Mathf.Max(m.LastTractionPeakSpeed, m.LastTractionEndSpeed);
            m.LastTractionTicks = s.TractionStartTick == MovementState.NeverTick ? 0 : endTick - s.TractionStartTick;
        }

        private static void Refuse(ref MagnetismState m, int tick, MagnetMode mode, MagnetRefusal refusal,
            ref int consumedTick, int pressTick)
        {
            m.LastRefusalTick = tick;
            m.LastRefusal = refusal;
            m.LastRefusalMode = mode;
            consumedTick = pressTick;
        }
    }
}
