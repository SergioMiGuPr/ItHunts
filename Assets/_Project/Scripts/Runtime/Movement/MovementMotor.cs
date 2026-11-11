using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>
    /// Simulation du mouvement, un tick à la fois. Classe pure : aucune lecture d'entrée, d'horloge
    /// ou de scène. Tout passe par les paramètres, pour pouvoir tester et rejouer (réseau).
    /// États : Sol (debout ou accroupi), Air, Glissade. Saut (coyote, buffer, hauteur variable,
    /// gravité de chute), sauts multiples, auto-bunny hop, contrôle aérien, plafond de sécurité.
    /// </summary>
    public static class MovementMotor
    {
        public static void Step(ref MovementState s, in MovementInput input, MovementSettings cfg, float dt,
            ICollisionProbe probe)
        {
            s.Tick++;

            int coyoteTicks = MovementSettings.SecondsToTicks(cfg.coyoteTime, dt);
            int bufferTicks = MovementSettings.SecondsToTicks(cfg.jumpBuffer, dt);
            int slideBoostTicks = MovementSettings.SecondsToTicks(cfg.slideBoostCooldown, dt);

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

            bool canStand = !s.Crouched || probe.HasHeadroom(cfg.capsuleHeight);
            float horizontalSpeed = s.HorizontalSpeed;

            // 2. Saut.
            //    - au sol ou dans le coyote time : appui récent (buffer) non consommé, ou bouton maintenu
            //      au sol si l'auto-bunny hop est actif ; impossible sous un plafond bas ;
            //    - en l'air : seulement un NOUVEL appui, s'il reste des sauts.
            int sincePress = s.Tick - input.JumpPressedTick;
            bool freshPress = input.JumpPressedTick > s.LastConsumedJumpPressTick
                              && sincePress >= 0 && sincePress <= bufferTicks;
            bool inCoyote = !grounded && s.Tick - s.LastGroundedTick <= coyoteTicks;
            bool groundJump = (grounded || inCoyote) && canStand
                              && (freshPress || (grounded && cfg.autoBunnyHop && input.JumpHeld));

            bool airJump = false;
            if (!groundJump && !grounded && !inCoyote && freshPress)
            {
                // Tombé d'un bord sans sauter : le saut au sol est perdu une fois le coyote time passé.
                s.JumpsUsed = Mathf.Max(s.JumpsUsed, 1);
                airJump = s.JumpsUsed < cfg.maxJumps && canStand;
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
                s.Crouched = false;                              // on se relève pour sauter (canStand)
                grounded = false;                                // pas de friction ce tick-ci (bunny hop)
                s.Mode = MovementMode.Air;
                if (!input.JumpHeld)
                {
                    // Appui déjà relâché avant le tick (tape très brève) : petit saut.
                    s.Velocity.y *= cfg.jumpCutMultiplier;
                    s.JumpRising = false;
                }
            }
            else if (grounded)
            {
                UpdateGroundPosture(ref s, input, cfg, canStand, horizontalSpeed, slideBoostTicks);
            }
            else
            {
                // En l'air : on se relève dès qu'il y a la place.
                if (s.Crouched && canStand)
                    s.Crouched = false;
                s.Mode = MovementMode.Air;
            }

            // 3. Horizontal.
            Vector3 wish = MovementMath.WishVector(input.Move, input.Yaw);
            float wishAmount = wish.magnitude;
            Vector3 wishDir = wishAmount > 1e-4f ? wish / wishAmount : Vector3.zero;

            if (grounded && s.Mode == MovementMode.Slide)
            {
                // Décélération constante (pas proportionnelle à la vitesse), puis effet de la pente.
                s.Velocity = MovementMath.Decelerate(s.Velocity, cfg.slideDeceleration, dt);
                s.Velocity += MovementMath.SlopeAcceleration(groundNormal, cfg.gravity * cfg.slideSlopeAcceleration) * dt;
                if (wishAmount > 1e-4f)
                    s.Velocity = MovementMath.AirControl(s.Velocity, wishDir, cfg.slideSteering * wishAmount, dt);

                // Sous un obstacle, la glissade ne s'arrête pas : vitesse minimale jusqu'à la sortie.
                if (!canStand)
                {
                    Vector3 facing = Quaternion.Euler(0f, input.Yaw, 0f) * Vector3.forward;
                    s.Velocity = MovementMath.EnsureMinHorizontalSpeed(s.Velocity, cfg.slideMinSpeedUnderObstacle, facing);
                }
            }
            else if (grounded)
            {
                float topSpeed = s.Crouched ? cfg.crouchSpeed : cfg.runSpeed;
                s.Velocity = MovementMath.ApplyFriction(s.Velocity, cfg.groundFriction, cfg.stopSpeed, dt);
                s.Velocity = MovementMath.Accelerate(s.Velocity, wishDir, topSpeed * wishAmount, cfg.groundAcceleration, dt);
            }
            else
            {
                s.Velocity = MovementMath.AirAccelerate(s.Velocity, wishDir, cfg.runSpeed * wishAmount,
                    cfg.airWishSpeedCap, cfg.airAcceleration, dt);

                // Contrôle aérien (W seul) : la vitesse acquise tourne vers la caméra. En strafant, rien :
                // on garde le gain de vitesse façon Source.
                if (MovementMath.IsForwardOnly(input.Move))
                    s.Velocity = MovementMath.AirControl(s.Velocity, wishDir, cfg.airControl * wishAmount, dt);
            }

            s.Velocity = MovementMath.ClampHorizontal(s.Velocity, cfg.maxHorizontalSpeed);

            // 4. Capsule : basse en glissade et accroupi, debout sinon (on ne grandit que s'il y a la place).
            probe.SetHeight(s.Crouched ? cfg.crouchHeight : cfg.capsuleHeight);

            // 5. Vertical : coupe au relâchement, puis gravité en deux demi-pas (hauteur de saut exacte).
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

            // 6. Déplacement.
            MoveResult result = probe.Move(s.Velocity * dt, snapToGround: grounded);

            if (!grounded)
                s.Velocity.y -= 0.5f * g * dt;
            s.Velocity.y = Mathf.Max(s.Velocity.y, -cfg.maxFallSpeed);

            // 7. Réponse aux collisions.
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
        }

        /// <summary>Au sol sans sauter : entrée et sortie de glissade, accroupi, relevé.</summary>
        private static void UpdateGroundPosture(ref MovementState s, in MovementInput input, MovementSettings cfg,
            bool canStand, float horizontalSpeed, int slideBoostTicks)
        {
            if (s.Mode == MovementMode.Slide)
            {
                // Sous un obstacle, on continue de glisser (vitesse minimale appliquée plus loin).
                if (!canStand)
                    return;

                if (!input.SlideHeld || horizontalSpeed < cfg.slideExitSpeed)
                {
                    s.Mode = MovementMode.Ground;
                    s.Crouched = input.SlideHeld;   // touche toujours tenue : accroupi
                }

                return;
            }

            if (input.SlideHeld && horizontalSpeed >= cfg.slideMinSpeed)
            {
                s.Mode = MovementMode.Slide;
                s.Crouched = true;
                if (s.Tick - s.LastSlideBoostTick >= slideBoostTicks && horizontalSpeed > 1e-3f)
                {
                    var dir = new Vector3(s.Velocity.x, 0f, s.Velocity.z) / horizontalSpeed;
                    s.Velocity += dir * cfg.slideBoost;
                    s.LastSlideBoostTick = s.Tick;
                }

                return;
            }

            s.Mode = MovementMode.Ground;
            if (input.SlideHeld)
                s.Crouched = true;
            else if (s.Crouched && canStand)
                s.Crouched = false;
        }
    }
}
