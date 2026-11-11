using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>
    /// Simulation du mouvement, un tick à la fois. Classe pure : aucune lecture d'entrée, d'horloge
    /// ou de scène. Tout passe par les paramètres, pour pouvoir tester et rejouer (réseau).
    /// Étape 1 : états Sol et Air, course, saut (coyote, buffer, hauteur variable, gravité de chute),
    /// sauts multiples, auto-bunny hop, contrôle aérien, plafond de sécurité.
    /// </summary>
    public static class MovementMotor
    {
        public static void Step(ref MovementState s, in MovementInput input, MovementSettings cfg, float dt,
            ICollisionProbe probe)
        {
            s.Tick++;

            int coyoteTicks = MovementSettings.SecondsToTicks(cfg.coyoteTime, dt);
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.jumpBuffer, dt);

            // 1. Sol. On ne se colle pas au sol pendant une montée (sinon le saut serait annulé).
            bool groundHit = probe.ProbeGround(out Vector3 groundNormal);
            bool grounded = groundHit && s.Velocity.y <= 1e-3f;
            if (grounded)
            {
                s.LastGroundedTick = s.Tick;
                s.JumpRising = false;
                s.JumpsUsed = 0;
                if (s.Velocity.y < 0f)
                    s.Velocity.y = 0f;
            }

            // 2. Saut.
            //    - au sol ou dans le coyote time : appui récent (buffer) non consommé, ou bouton maintenu
            //      au sol si l'auto-bunny hop est actif ;
            //    - en l'air : seulement un NOUVEL appui, s'il reste des sauts.
            int sincePress = s.Tick - input.JumpPressedTick;
            bool freshPress = input.JumpPressedTick > s.LastConsumedJumpPressTick
                              && sincePress >= 0 && sincePress <= bufferTicks;
            bool inCoyote = !grounded && s.Tick - s.LastGroundedTick <= coyoteTicks;
            bool groundJump = (grounded || inCoyote)
                              && (freshPress || (grounded && cfg.autoBunnyHop && input.JumpHeld));

            bool airJump = false;
            if (!groundJump && !grounded && !inCoyote && freshPress)
            {
                // Tombé d'un bord sans sauter : le saut au sol est perdu une fois le coyote time passé.
                s.JumpsUsed = Mathf.Max(s.JumpsUsed, 1);
                airJump = s.JumpsUsed < cfg.maxJumps;
            }

            if (groundJump || airJump)
            {
                float jumpVelocity = groundJump ? cfg.JumpVelocity : cfg.AirJumpVelocity;
                s.Velocity.y = groundJump ? jumpVelocity : Mathf.Max(s.Velocity.y, jumpVelocity);
                s.JumpsUsed = groundJump ? 1 : s.JumpsUsed + 1;
                if (freshPress)
                    s.LastConsumedJumpPressTick = input.JumpPressedTick;
                s.LastGroundedTick = MovementState.NeverTick;   // pas de second saut via le coyote time
                s.LastJumpTick = s.Tick;
                s.JumpRising = true;
                grounded = false;                                // pas de friction ce tick-ci (bunny hop)
                if (!input.JumpHeld)
                {
                    // Appui déjà relâché avant le tick (tape très brève) : petit saut.
                    s.Velocity.y *= cfg.jumpCutMultiplier;
                    s.JumpRising = false;
                }
            }

            // 3. Horizontal.
            Vector3 wish = MovementMath.WishVector(input.Move, input.Yaw);
            float wishAmount = wish.magnitude;
            Vector3 wishDir = wishAmount > 1e-4f ? wish / wishAmount : Vector3.zero;
            float wishSpeed = cfg.runSpeed * wishAmount;

            if (grounded)
            {
                s.Velocity = MovementMath.ApplyFriction(s.Velocity, cfg.groundFriction, cfg.stopSpeed, dt);
                s.Velocity = MovementMath.Accelerate(s.Velocity, wishDir, wishSpeed, cfg.groundAcceleration, dt);
            }
            else
            {
                s.Velocity = MovementMath.AirAccelerate(s.Velocity, wishDir, wishSpeed, cfg.airWishSpeedCap,
                    cfg.airAcceleration, dt);

                // Contrôle aérien (W seul) : la vitesse acquise tourne vers la caméra. En strafant, rien :
                // on garde le gain de vitesse façon Source.
                if (MovementMath.IsForwardOnly(input.Move))
                    s.Velocity = MovementMath.AirControl(s.Velocity, wishDir, cfg.airControl * wishAmount, dt);
            }

            s.Velocity = MovementMath.ClampHorizontal(s.Velocity, cfg.maxHorizontalSpeed);

            // 4. Vertical : coupe au relâchement, puis gravité en deux demi-pas (hauteur de saut exacte).
            float g = 0f;
            if (!grounded)
            {
                if (s.JumpRising && !input.JumpHeld && s.Velocity.y > 0f)
                {
                    s.Velocity.y *= cfg.jumpCutMultiplier;
                    s.JumpRising = false;
                }

                g = s.Velocity.y > 0f ? cfg.gravity : cfg.gravity * cfg.fallGravityMultiplier;
                s.Velocity.y -= 0.5f * g * dt;
            }

            // 5. Déplacement.
            MoveResult result = probe.Move(s.Velocity * dt, snapToGround: grounded);

            if (!grounded)
                s.Velocity.y -= 0.5f * g * dt;
            s.Velocity.y = Mathf.Max(s.Velocity.y, -cfg.maxFallSpeed);

            // 6. Réponse aux collisions.
            if (result.HitAbove && s.Velocity.y > 0f)
            {
                s.Velocity.y = 0f;
                s.JumpRising = false;
            }

            if (result.HitBelow && s.Velocity.y < 0f)
                s.Velocity.y = 0f;

            if (result.HitSides && dt > 0f)
            {
                // On perd seulement la part de vitesse qui est entrée dans le mur.
                var actual = new Vector3(result.ActualDelta.x, 0f, result.ActualDelta.z) / dt;
                var wanted = new Vector3(s.Velocity.x, 0f, s.Velocity.z);
                if (actual.sqrMagnitude < wanted.sqrMagnitude)
                    s.Velocity = new Vector3(actual.x, s.Velocity.y, actual.z);
            }

            if (s.Velocity.y <= 0f)
                s.JumpRising = false;

            s.Position = probe.Position;
            s.Grounded = grounded;
            s.GroundNormal = grounded ? groundNormal : Vector3.up;
            s.Mode = grounded ? MovementMode.Ground : MovementMode.Air;
        }
    }
}
