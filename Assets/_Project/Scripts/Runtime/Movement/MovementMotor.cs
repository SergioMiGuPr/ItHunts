using System;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Movement
{
    /// <summary>
    /// Simulation du mouvement, un tick à la fois. Classe pure : aucune lecture d'entrée, d'horloge
    /// ou de scène. Tout passe par les paramètres, pour pouvoir tester et rejouer (réseau).
    /// États : Sol (debout ou accroupi), Air, Glissade, WallSlide, Traction (orbe de magnétisme, démarrée et
    /// arrêtée par MagnetismOrb avant ce pas). Saut (coyote, buffer, hauteur
    /// variable, gravité de chute), sauts multiples, auto-bunny hop, wall jump, contrôle aérien.
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
            int wallCoyoteTicks = MovementSettings.SecondsToTicks(cfg.wallCoyoteTime, dt);
            int steerLockTicks = MovementSettings.SecondsToTicks(cfg.wallJumpSteerLock, dt);
            int dashCooldownTicks = MovementSettings.SecondsToTicks(cfg.dashCooldown, dt);
            int dashBufferTicks = MovementSettings.SecondsToTicks(cfg.dashBuffer, dt);
            int dashDurationTicks = Mathf.Max(1, MovementSettings.SecondsToTicks(cfg.dashDuration, dt));
            int dashGraceTicks = MovementSettings.SecondsToTicks(cfg.dashFrictionGrace, dt);
            int dashJumpWindowTicks = MovementSettings.SecondsToTicks(cfg.dashJumpWindow, dt);
            if (s.Charges < 0f)
                s.Charges = cfg.movementCharges;   // premier tick : barre pleine

            if (s.Mode == MovementMode.Mantle)
            {
                MantleTick(ref s, cfg, dt, probe);
                return;
            }

            Vector3 wish = MovementMath.WishVector(input.Move, input.Yaw);
            float wishAmount = wish.magnitude;
            Vector3 wishDir = wishAmount > 1e-4f ? wish / wishAmount : Vector3.zero;

            // Traction : ligne droite vers le point, ni sol, ni mur, ni gravité, ni friction.
            bool traction = s.Mode == MovementMode.Traction;

            // 1. Sol. On ne se colle pas au sol pendant une montée (sinon le saut serait annulé).
            bool groundHit = probe.ProbeGround(out Vector3 groundNormal);
            bool grounded = !traction && groundHit && s.Velocity.y <= 1e-3f;
            if (!grounded)
            {
                s.MomentumBroken = false;                     // en l'air : l'élan n'est plus cassé (bunny hop, saut)
                s.Momentum = MomentumStatus.Aucun;
            }

            if (grounded)
            {
                s.LastGroundedTick = s.Tick;
                s.JumpRising = false;
                s.JumpsUsed = 0;
                s.LastWallJumpId = 0;
                if (s.Velocity.y < 0f)
                    s.Velocity.y = 0f;
            }

            // Recharge des charges de mouvement, une par une, plus vite au sol.
            float rechargeTime = grounded ? cfg.chargeRechargeTimeGround : cfg.chargeRechargeTimeAir;
            s.Charges = Mathf.Min(cfg.movementCharges, s.Charges + dt / rechargeTime);

            // 1 bis. Mur (en l'air) : vers la direction voulue, la vitesse, le mur du tick précédent, et les deux
            //        côtés de la trajectoire (pour un mur longé bien parallèlement).
            bool onWall = false;
            Vector3 wallNormal = Vector3.zero;
            if (!grounded && !traction)
            {
                var horizontal = new Vector3(s.Velocity.x, 0f, s.Velocity.z);
                Vector3 velocityDir = horizontal.sqrMagnitude > 0.01f ? horizontal.normalized : Vector3.zero;
                Span<Vector3> directions = stackalloc Vector3[5];
                directions[0] = s.Tick - s.LastWallTick <= 1 ? -s.LastWallNormal : Vector3.zero;
                directions[1] = wishDir;
                directions[2] = velocityDir;
                directions[3] = new Vector3(velocityDir.z, 0f, -velocityDir.x);
                directions[4] = -directions[3];
                onWall = probe.ProbeWall(directions, out wallNormal, out int wallId);
                if (onWall)
                {
                    s.LastWallTick = s.Tick;
                    s.LastWallNormal = wallNormal;
                    s.LastWallId = wallId;
                    if (cfg.wallRefillsJumps)
                        s.JumpsUsed = 0;
                    if (cfg.wallRefillsCharges)
                        s.Charges = cfg.movementCharges;
                }
            }

            bool canStand = !s.Crouched || probe.HasHeadroom(cfg.capsuleHeight);
            float horizontalSpeed = s.HorizontalSpeed;
            bool wallAttached = false;

            // 2. Saut, par priorité :
            //    - au sol ou dans le coyote time : appui récent (buffer) non consommé, ou bouton maintenu
            //      au sol si l'auto-bunny hop est actif ; impossible sous un plafond bas ;
            //    - wall jump : nouvel appui, au mur ou dans le wall-coyote, pas deux fois de suite le même mur ;
            //    - en l'air : nouvel appui, s'il reste des sauts.
            int sincePress = s.Tick - input.JumpPressedTick;
            bool freshPress = input.JumpPressedTick > s.LastConsumedJumpPressTick
                              && sincePress >= 0 && sincePress <= bufferTicks;
            bool inCoyote = !grounded && s.Tick - s.LastGroundedTick <= coyoteTicks;
            bool groundJump = !traction && (grounded || inCoyote) && canStand
                              && (freshPress || (grounded && cfg.autoBunnyHop && input.JumpHeld));

            bool wallJump = !traction && !groundJump && !grounded && freshPress && canStand
                            && s.LastWallTick != MovementState.NeverTick
                            && s.Tick - s.LastWallTick <= wallCoyoteTicks
                            && (cfg.allowSameWallJump || s.LastWallId != s.LastWallJumpId);

            bool airJump = false;
            if (!traction && !groundJump && !wallJump && !grounded && !inCoyote && freshPress)
            {
                // Tombé d'un bord sans sauter : le saut au sol est perdu une fois le coyote time passé.
                s.JumpsUsed = Mathf.Max(s.JumpsUsed, 1);
                airJump = s.JumpsUsed < cfg.maxJumps && canStand && s.Charges >= 1f;
            }

            if ((groundJump || airJump || wallJump) && s.DashTicksLeft > 0)
                EndDash(ref s, probe.Position, dashGraceTicks);           // un saut termine le dash (vitesse de sortie)

            if (groundJump || airJump)
            {
                float jumpVelocity = groundJump ? cfg.JumpVelocity : cfg.AirJumpVelocity;
                s.Velocity.y = groundJump ? jumpVelocity : Mathf.Max(s.Velocity.y, jumpVelocity);
                s.JumpsUsed = groundJump ? 1 : s.JumpsUsed + 1;
                if (airJump)
                    s.Charges -= 1f;                              // le saut en l'air coûte une charge
                if (groundJump)
                    TryDashJump(ref s, cfg, dashJumpWindowTicks);
                StartJump(ref s, input, cfg, freshPress);
                grounded = false;                                // pas de friction ce tick-ci (bunny hop)
            }
            else if (wallJump)
            {
                // Impulsion AJOUTÉE à la vitesse acquise, dans une direction qui mélange la normale du mur et
                // l'avant (caméra, sinon trajectoire) ; jamais moins vite qu'avant (+ bonus).
                s.Velocity = MovementMath.WallJumpVelocity(s.Velocity, s.LastWallNormal, input.Yaw, cfg.wallJumpForwardBias,
                    cfg.wallJumpKick, cfg.wallJumpSpeedBonus, cfg.WallJumpVelocity, cfg.wallGainCap, cfg.wallGainScale);
                s.LastWallJumpId = s.LastWallId;
                s.LastWallTick = MovementState.NeverTick;        // pas de second wall jump via le wall-coyote
                s.SteerLockUntilTick = s.Tick + steerLockTicks;
                StartJump(ref s, input, cfg, freshPress: true);
                onWall = false;
            }
            else if (grounded)
            {
                UpdateGroundPosture(ref s, input, cfg, canStand, horizontalSpeed, slideBoostTicks);
            }
            else if (!traction)
            {
                // En l'air : on se relève dès qu'il y a la place. Accroché au mur si on pousse vers lui (avec une
                // tolérance d'angle) ou si on le longe assez vite ; wall slide quand on descend.
                if (s.Crouched && canStand)
                    s.Crouched = false;
                if (onWall)
                {
                    var horizontal = new Vector3(s.Velocity.x, 0f, s.Velocity.z);
                    float alongWall = (horizontal - wallNormal * Vector3.Dot(horizontal, wallNormal)).magnitude;
                    bool pushing = wishAmount > 0.1f && Vector3.Angle(wishDir, -wallNormal) <= cfg.wallSlideInputTolerance;
                    bool leaving = Vector3.Dot(horizontal, wallNormal) > 1f;   // on s'éloigne du mur (après un wall jump)
                    wallAttached = !leaving && (pushing || alongWall >= cfg.wallSlideMinSpeed);
                }

                s.Mode = wallAttached && s.Velocity.y <= 0f ? MovementMode.WallSlide : MovementMode.Air;

                // Hissage (D-074) : contre un mur, en avançant vers lui, dessus du rebord à portée et libre.
                if (onWall && TryStartMantle(ref s, input, cfg, wishDir, wishAmount, wallNormal, dt, probe))
                {
                    MantleTick(ref s, cfg, dt, probe);
                    return;
                }
            }

            // 2 bis. Dash : appui mémorisé (dashBuffer, un appui pendant un dash part ensuite), une charge, délai
            //        entre deux dashs. Direction de l'entrée (avant de la caméra sans entrée). Pendant dashDuration :
            //        vitesse constante (dashDistance / durée, ou plus si on allait plus vite), ni friction ni gravité.
            int sinceDash = s.Tick - input.DashPressedTick;
            bool dashPress = input.DashPressedTick > 0                       // 0 ou moins : aucun appui
                             && input.DashPressedTick > s.LastConsumedDashPressTick
                             && sinceDash >= 0 && sinceDash <= dashBufferTicks;
            if (dashPress && s.DashTicksLeft == 0 && s.Charges >= 1f && s.Tick - s.LastDashTick >= dashCooldownTicks)
            {
                s.DashDir = wishAmount > 1e-4f ? wishDir : Quaternion.Euler(0f, input.Yaw, 0f) * Vector3.forward;
                s.DashSpeedBefore = s.HorizontalSpeed;
                s.DashStartedGrounded = grounded;
                // Sortie (D-084) : au sol comme en l'air, part de la vitesse du dash gardée et gain plafonnés à 20 m/s, jamais
                // sous la vitesse d'avant ni sous dashExitSpeed ; gardée ensuite comme l'élan. Pic calculé pour parcourir dashDistance.
                s.DashExitTarget = MovementMath.DashExitSpeed(s.DashSpeedBefore, cfg.dashDistance / (dashDurationTicks * dt),
                    cfg.dashExitCarry, cfg.dashExitSpeed, cfg.dashSpeedGain, cfg.movementGainCap);
                s.DashPeakSpeed = MovementMath.DashPeakSpeed(cfg.dashDistance, dashDurationTicks, dt, s.DashExitTarget,
                    cfg.dashProfile == DashProfile.Progressif);
                s.DashTicksLeft = dashDurationTicks;
                s.DashStartPosition = probe.Position;
                s.Charges -= 1f;
                s.LastDashTick = s.Tick;
                s.LastConsumedDashPressTick = input.DashPressedTick;
                s.LastDashMove = input.Move;
                s.LastDashStartSpeed = s.DashSpeedBefore;
                s.JumpRising = false;
                if (traction)
                {
                    EndTraction(ref s, TractionEnd.Dash);                  // le dash termine la traction
                    traction = false;
                }
            }

            bool dashed = s.DashTicksLeft > 0;
            if (dashed)
            {
                int index = dashDurationTicks - s.DashTicksLeft;
                float speed = MovementMath.DashSpeedAt(index, dashDurationTicks, s.DashPeakSpeed, s.DashExitTarget,
                    cfg.dashProfile == DashProfile.Progressif);
                s.Velocity = new Vector3(s.DashDir.x * speed, 0f, s.DashDir.z * speed);
            }

            float speedBeforeHorizontal = s.HorizontalSpeed;

            // 3. Horizontal (rien ce tick-ci après un dash : la vitesse du dash est exacte).
            Vector3 tractionDir = Vector3.zero;
            if (dashed)
            {
            }
            else if (traction)
            {
                Vector3 eye = s.Position + Vector3.up * (s.Crouched ? cfg.crouchEyeHeight : cfg.eyeHeight);
                Vector3 to = s.TractionPoint - eye;
                tractionDir = to.sqrMagnitude > 1e-6f ? to.normalized : Vector3.zero;
                s.Velocity = MovementMath.TractionVelocity(s.Velocity, tractionDir, s.TractionAcceleration, s.TractionSpeed, dt);
            }
            else if (grounded && s.Mode == MovementMode.Slide)
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
                // Élan gardé (D-073) : au-dessus de la course, la vitesse est gardée en avançant dans le cône, sinon perdue
                // progressivement ; à la course ou en dessous, sol façon Source.
                if (!cfg.keepGroundMomentum || !GroundMomentum(ref s, input, cfg, wishDir, wishAmount, dt))
                {
                    float topSpeed = s.Crouched ? cfg.crouchSpeed : cfg.runSpeed;
                    if (s.Tick >= s.DashGraceUntilTick)                   // pas de friction juste après un dash
                        s.Velocity = MovementMath.ApplyFriction(s.Velocity, cfg.groundFriction, cfg.stopSpeed, dt);
                    s.Velocity = MovementMath.Accelerate(s.Velocity, wishDir, topSpeed * wishAmount, cfg.groundAcceleration, dt);
                }
            }
            else if (s.Tick >= s.SteerLockUntilTick)
            {
                s.Velocity = MovementMath.AirAccelerate(s.Velocity, wishDir, cfg.runSpeed * wishAmount,
                    cfg.airWishSpeedCap, cfg.airAcceleration, dt);

                // Contrôle aérien (W seul) : la vitesse acquise tourne vers la caméra. En strafant, rien :
                // on garde le gain de vitesse façon Source.
                if (MovementMath.IsForwardOnly(input.Move))
                    s.Velocity = MovementMath.AirControl(s.Velocity, wishDir, cfg.airControl * wishAmount, dt,
                        cfg.airControlMaxAngle);
            }

            // Accroché au mur : la part de vitesse qui entre dans le mur est reportée le long du mur, sans perte.
            // Gain le long du mur (strafe) réduit et plafonné à wallGainCap (D-044).
            if (wallAttached && !dashed)
            {
                s.Velocity = MovementMath.ProjectAlongWall(s.Velocity, wallNormal, speedBeforeHorizontal);
                s.Velocity = MovementMath.WallGain(s.Velocity, speedBeforeHorizontal, cfg.wallGainScale, cfg.wallGainCap);
            }

            s.Velocity = MovementMath.ClampHorizontal(s.Velocity, cfg.maxHorizontalSpeed);

            // 4. Capsule : basse en glissade et accroupi, debout sinon (on ne grandit que s'il y a la place).
            probe.SetHeight(s.Crouched ? cfg.crouchHeight : cfg.capsuleHeight);

            // 5. Vertical : coupe au relâchement, puis gravité en deux demi-pas (hauteur de saut exacte).
            float g = 0f;
            if (!grounded && !dashed && !traction)                    // pas de gravité pendant un dash ni une traction
            {
                if (s.JumpRising && !input.JumpHeld && s.Velocity.y > 0f)
                {
                    s.Velocity.y *= cfg.jumpCutMultiplier;
                    s.JumpRising = false;
                }

                g = s.Velocity.y > 0f ? cfg.gravity : cfg.gravity * cfg.fallGravityMultiplier;
                s.Velocity.y -= 0.5f * g * dt;
            }

            // 6. Déplacement (accroché au mur : légère poussée vers lui pour garder le contact).
            Vector3 delta = s.Velocity * dt;
            if (wallAttached)
                delta -= wallNormal * (cfg.wallStickSpeed * dt);
            MoveResult result = probe.Move(delta, snapToGround: grounded);

            if (!grounded)
                s.Velocity.y -= 0.5f * g * dt;
            if (!traction)
                s.Velocity.y = Mathf.Max(s.Velocity.y, -cfg.maxFallSpeed);

            // Traction bloquée par un obstacle (moins de la moitié du déplacement voulu vers le point) : fin.
            if (traction)
            {
                float wanted = Vector3.Dot(delta, tractionDir);
                if (wanted > 1e-4f && Vector3.Dot(result.ActualDelta, tractionDir) < 0.5f * wanted)
                    EndTraction(ref s, TractionEnd.Bloquee);
            }

            // Fin du dash : vitesse de sortie.
            if (dashed && --s.DashTicksLeft == 0)
                EndDash(ref s, probe.Position, dashGraceTicks);
            if (s.Mode == MovementMode.WallSlide)
            {
                // Effet wall run : plus on longe le mur vite, plus la chute est lente.
                float run = Mathf.InverseLerp(cfg.wallRunSpeedStart, cfg.wallRunSpeedFull, s.HorizontalSpeed);
                float maxFall = Mathf.Lerp(cfg.wallSlideMaxFallSpeed, cfg.wallRunMaxFallSpeed, run);
                s.Velocity.y = Mathf.Max(s.Velocity.y, -maxFall);
            }

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
                var sideNormal = new Vector3(result.SideNormal.x, 0f, result.SideNormal.z);
                if (result.HasSideNormal && sideNormal.sqrMagnitude > 1e-6f)
                {
                    // On retire seulement la part de vitesse qui entre dans l'obstacle (le reste est intact).
                    sideNormal.Normalize();
                    var h = new Vector3(s.Velocity.x, 0f, s.Velocity.z);
                    float into = Vector3.Dot(h, sideNormal);
                    if (into < 0f && !grounded)
                    {
                        s.PreHitSpeed = h.magnitude;                  // pour le hissage : l'élan d'avant le mur
                        s.PreHitTick = s.Tick;
                    }
                    // Mur percuté au sol au-dessus de la course : élan cassé (D-073), redescente vers la course.
                    if (grounded && cfg.keepGroundMomentum && h.magnitude > cfg.runSpeed + 1e-3f
                        && -into >= h.magnitude * Mathf.Sin(cfg.momentumWallHitAngle * Mathf.Deg2Rad))
                    {
                        s.MomentumBroken = true;
                        s.Momentum = MomentumStatus.Mur;
                    }

                    if (into < 0f)
                    {
                        h -= sideNormal * into;
                        s.Velocity = new Vector3(h.x, s.Velocity.y, h.z);
                    }
                }
                else
                {
                    // Sans normale : on se fie au déplacement réellement effectué.
                    var actual = new Vector3(result.ActualDelta.x, 0f, result.ActualDelta.z) / dt;
                    var wanted = new Vector3(s.Velocity.x, 0f, s.Velocity.z);
                    if (actual.sqrMagnitude < wanted.sqrMagnitude)
                        s.Velocity = new Vector3(actual.x, s.Velocity.y, actual.z);
                }
            }

            if (s.Velocity.y <= 0f)
                s.JumpRising = false;

            s.Position = probe.Position;
            s.Grounded = grounded;
            s.OnWall = onWall;
            s.GroundNormal = grounded ? groundNormal : Vector3.up;
        }

        /// <summary>
        /// Départ du hissage (D-074) : en l'air, debout, ni dash ni traction, entrée vers l'avant qui vise le mur, rebord trouvé
        /// par la sonde entre mantleMinRise et la tête + mantleMaxAboveHead. Vitesse de sortie : mantleKeepSpeed de la vitesse
        /// horizontale d'avant le contact (au plus 10 ticks avant), dans la direction du mur.
        /// </summary>
        public static bool TryStartMantle(ref MovementState s, in MovementInput input, MovementSettings cfg, Vector3 wishDir,
            float wishAmount, Vector3 wallNormal, float dt, ICollisionProbe probe)
        {
            if (!cfg.mantleEnabled || s.Crouched || s.DashTicksLeft > 0 || s.Mode == MovementMode.Traction
                || wishAmount < 0.1f || input.Move.y <= 0.1f
                || Vector3.Angle(wishDir, -wallNormal) > cfg.mantleInputAngle)
                return false;
            if (s.Tick - s.LastMantleTick < 6)
                return false;                                       // juste après un hissage : pas de second départ
            float maxRise = cfg.capsuleHeight + cfg.mantleMaxAboveHead;
            if (!probe.ProbeLedge(-wallNormal, cfg.mantleMinRise, maxRise, cfg.mantleMinDepth, out Vector3 stand))
                return false;

            float before = s.HorizontalSpeed;
            if (s.Tick - s.PreHitTick <= 10)
                before = Mathf.Max(before, s.PreHitSpeed);
            s.Mode = MovementMode.Mantle;
            s.MantleStart = probe.Position;
            s.MantleTarget = stand;
            s.MantleTicks = Mathf.Max(2, MovementSettings.SecondsToTicks(cfg.mantleDuration, dt));
            s.MantleTicksLeft = s.MantleTicks;
            s.MantleExitSpeed = before * cfg.mantleKeepSpeed;
            s.MantleDir = -wallNormal;
            s.LastMantleTick = s.Tick;
            s.JumpRising = false;
            s.Crouched = false;
            return true;
        }

        /// <summary>Position du hissage à l'avancement u (0 à 1) : monter le long du mur, puis avancer sur le dessus.</summary>
        public static Vector3 MantlePath(Vector3 start, Vector3 target, float u, float riseShare)
        {
            float topY = target.y + 0.02f;
            if (u <= riseShare)
            {
                float k = Mathf.SmoothStep(0f, 1f, u / riseShare);
                return new Vector3(start.x, Mathf.Lerp(start.y, topY, k), start.z);
            }

            float f = Mathf.SmoothStep(0f, 1f, (u - riseShare) / (1f - riseShare));
            return new Vector3(Mathf.Lerp(start.x, target.x, f), topY, Mathf.Lerp(start.z, target.z, f));
        }

        private static void MantleTick(ref MovementState s, MovementSettings cfg, float dt, ICollisionProbe probe)
        {
            int index = s.MantleTicks - s.MantleTicksLeft + 1;
            float u = index / (float)s.MantleTicks;
            Vector3 want = MantlePath(s.MantleStart, s.MantleTarget, u, cfg.mantleRiseShare);
            Vector3 before = probe.Position;
            probe.SetHeight(cfg.capsuleHeight);
            probe.Move(want - before, snapToGround: false);
            s.Velocity = dt > 0f ? (probe.Position - before) / dt : Vector3.zero;
            s.Position = probe.Position;
            s.Grounded = false;
            s.OnWall = false;
            if (--s.MantleTicksLeft > 0)
                return;

            // Arrivée : posé sur le dessus, élan gardé en partie, vers l'avant.
            s.Mode = MovementMode.Air;
            s.Velocity = new Vector3(s.MantleDir.x * s.MantleExitSpeed, 0f, s.MantleDir.z * s.MantleExitSpeed);
            s.LastGroundedTick = s.Tick;                          // un saut juste à l'arrivée compte comme au sol (coyote)
            s.JumpsUsed = 0;
        }

        /// <summary>
        /// Élan au sol (D-073), debout en mode Sol, au-dessus de la vitesse de course. Gardé (décroissance momentumDecay, 0 par
        /// défaut) tant qu'on tient « avancer » avec l'entrée dans le cône autour de la trajectoire, qui suit alors l'entrée ;
        /// perdu progressivement vers la course sinon : avancer lâché, hors du cône, recul (freinage), virage au-delà de
        /// momentumTurnAngle ou mur percuté (élan cassé jusqu'au retour à la course ou au décollage). Au-dessus de groundSpeedCap,
        /// redescente vers ce plafond. Renvoie faux à la course ou en dessous (sol façon Source) : rien à garder.
        /// </summary>
        public static bool GroundMomentum(ref MovementState s, in MovementInput input, MovementSettings cfg, Vector3 wishDir,
            float wishAmount, float dt)
        {
            var h = new Vector3(s.Velocity.x, 0f, s.Velocity.z);
            float speed = h.magnitude;
            float run = cfg.runSpeed;
            if (speed <= run + 1e-3f)
            {
                s.MomentumBroken = false;
                s.Momentum = MomentumStatus.Aucun;
                return false;
            }

            if (s.Crouched)
            {
                s.Momentum = MomentumStatus.Accroupi;
                return false;                                  // accroupi : friction normale
            }

            Vector3 dir = h / speed;
            bool hasWish = wishAmount > 0.1f;
            float angle = hasWish ? Vector3.Angle(dir, wishDir) : 180f;
            bool forward = input.Move.y > 0.1f;

            MomentumStatus status;
            if (hasWish && (input.Move.y < -0.1f || angle > 120f))
                status = MomentumStatus.Recule;
            else if (s.MomentumBroken)
                status = s.Momentum == MomentumStatus.Mur ? MomentumStatus.Mur : MomentumStatus.Virage;
            else if (!forward)
                status = MomentumStatus.AvancerLache;
            else if (angle > cfg.momentumTurnAngle)
            {
                status = MomentumStatus.Virage;
                s.MomentumBroken = true;
            }
            else if (angle > cfg.momentumConeAngle)
                status = MomentumStatus.HorsDuCone;
            else
                status = MomentumStatus.Garde;

            float newSpeed;
            if (status == MomentumStatus.Garde)
            {
                newSpeed = Mathf.Max(run, speed - cfg.momentumDecay * dt);
                if (cfg.momentumSteering > 0f)
                    dir = MovementMath.AirControl(dir, wishDir, cfg.momentumSteering, dt).normalized;
            }
            else
            {
                float decel = status == MomentumStatus.Recule ? cfg.momentumBrakeDeceleration : cfg.momentumLossDeceleration;
                if (s.Tick < s.DashGraceUntilTick && status != MomentumStatus.Recule)
                    decel = 0f;                                // sortie de dash : pas de perte tout de suite
                newSpeed = Mathf.Max(run, speed - decel * dt);
            }

            if (newSpeed > cfg.groundSpeedCap)
            {
                newSpeed = Mathf.Max(cfg.groundSpeedCap, speed - cfg.groundOverCapDeceleration * dt);
                if (status == MomentumStatus.Garde)
                    status = MomentumStatus.Plafond;
            }

            Vector3 v = dir * newSpeed;
            // En perdant l'élan, l'entrée de côté ou vers l'avant oriente encore la trajectoire (sans faire accélérer).
            if (status != MomentumStatus.Garde && status != MomentumStatus.Plafond && hasWish && angle <= 90f)
            {
                v = MovementMath.Accelerate(v, wishDir, run * wishAmount, cfg.groundAcceleration, dt);
                v = MovementMath.ClampHorizontal(v, newSpeed);
            }

            s.Velocity = new Vector3(v.x, s.Velocity.y, v.z);
            s.Momentum = status;
            return true;
        }

        /// <summary>
        /// Termine le dash : vitesse horizontale = la plus grande entre celle d'avant et dashExitSpeed, dans la
        /// direction du dash ; puis courte période sans friction au sol. Mémorise les chiffres pour le debug.
        /// Appelé aussi par l'orbe de magnétisme (une impulsion termine le dash).
        /// </summary>
        public static void EndDash(ref MovementState s, Vector3 position, int graceTicks)
        {
            float exit = s.DashExitTarget;
            s.Velocity = new Vector3(s.DashDir.x * exit, s.Velocity.y, s.DashDir.z * exit);
            s.DashTicksLeft = 0;
            s.DashEndTick = s.Tick;
            s.DashGraceUntilTick = s.Tick + 1 + graceTicks;
            Vector3 moved = position - s.DashStartPosition;
            s.LastDashDistance = new Vector2(moved.x, moved.z).magnitude;
            s.LastDashEndSpeed = exit;
        }

        /// <summary>Termine la traction (la vitesse reste telle quelle) ; l'orbe démarre sa recharge.</summary>
        public static void EndTraction(ref MovementState s, TractionEnd reason)
        {
            if (s.Mode != MovementMode.Traction)
                return;
            s.Mode = MovementMode.Air;
            s.TractionEndTick = s.Tick;
            s.TractionEndReason = reason;
        }

        /// <summary>
        /// Dash-jump : un saut au sol pendant un dash parti du sol, ou au plus dashJumpWindow après sa fin, donne
        /// dashJumpSpeedGain (plafonné) par rapport à la vitesse d'avant le dash, au lieu de dashSpeedGain. Une seule
        /// fois par dash. Mémorise le résultat (réussi, ou raté avec le retard) pour le debug.
        /// </summary>
        private static void TryDashJump(ref MovementState s, MovementSettings cfg, int windowTicks)
        {
            if (!s.DashStartedGrounded || s.LastDashTick == MovementState.NeverTick || s.LastDashJumpTick >= s.LastDashTick)
                return;
            int sinceEnd = s.Tick - s.DashEndTick;
            if (sinceEnd > windowTicks + 36)                 // trop loin du dash pour compter comme une tentative
                return;

            s.LastDashJumpTick = s.Tick;
            s.LastDashJumpSpeedBefore = s.DashSpeedBefore;
            if (sinceEnd <= windowTicks)
            {
                var h = new Vector3(s.Velocity.x, 0f, s.Velocity.z);
                float target = Mathf.Max(h.magnitude,                     // + le gain sur la sortie du dash (D-084)
                    MovementMath.CappedGain(Mathf.Max(s.DashSpeedBefore, s.DashExitTarget), cfg.dashJumpSpeedGain, cfg.movementGainCap));
                Vector3 dir = h.sqrMagnitude > 1e-6f ? h.normalized : s.DashDir;
                s.Velocity = new Vector3(dir.x * target, s.Velocity.y, dir.z * target);
                s.LastDashJumpSuccess = true;
                s.LastDashJumpLateTicks = 0;
            }
            else
            {
                s.LastDashJumpSuccess = false;
                s.LastDashJumpLateTicks = sinceEnd - windowTicks;
            }

            s.LastDashJumpSpeedAfter = new Vector2(s.Velocity.x, s.Velocity.z).magnitude;
        }

        /// <summary>Partie commune à tous les sauts : consommer l'appui, se relever, passer en l'air.</summary>
        private static void StartJump(ref MovementState s, in MovementInput input, MovementSettings cfg, bool freshPress)
        {
            if (freshPress)
                s.LastConsumedJumpPressTick = input.JumpPressedTick;
            s.LastGroundedTick = MovementState.NeverTick;   // pas de second saut via le coyote time
            s.LastJumpTick = s.Tick;
            s.JumpRising = true;
            s.Crouched = false;                              // on se relève pour sauter (canStand vérifié)
            s.Mode = MovementMode.Air;
            if (!input.JumpHeld)
            {
                // Appui déjà relâché avant le tick (tape très brève) : petit saut.
                s.Velocity.y *= cfg.jumpCutMultiplier;
                s.JumpRising = false;
            }
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
                    float boosted = MovementMath.CappedGain(horizontalSpeed, cfg.slideBoost, cfg.movementGainCap);
                    s.Velocity += dir * (boosted - horizontalSpeed);   // bonus plafonné (plafond commun des gains)
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
