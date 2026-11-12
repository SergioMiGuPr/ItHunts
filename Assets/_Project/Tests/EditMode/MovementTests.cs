using ItHunts.Movement;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Sol factice : un plan y = 0 qui s'arrête à x = EdgeX (au-delà, le vide). GroundNormal simule
    /// une pente (seule la normale change, la géométrie reste plate). Un plafond à CeilingY couvre
    /// la bande CeilingMinX..CeilingMaxX.
    /// </summary>
    internal sealed class FakeGroundProbe : ICollisionProbe
    {
        public float EdgeX = float.PositiveInfinity;
        public Vector3 GroundNormal = Vector3.up;
        public float CeilingY = float.PositiveInfinity;
        public float CeilingMinX = float.NegativeInfinity;
        public float CeilingMaxX = float.PositiveInfinity;
        public Vector3 Position { get; set; }
        public float Height { get; private set; } = 1.8f;

        /// <summary>Mur vertical infini, plan x = X ; le joueur est du côté de NormalX (+1 ou −1).</summary>
        public struct Wall
        {
            public float X;
            public float NormalX;
            public int Id;
        }

        public readonly System.Collections.Generic.List<Wall> Walls = new System.Collections.Generic.List<Wall>();
        private const float Radius = 0.4f;
        private const float Contact = 0.15f;

        private bool OverGround(Vector3 p) => p.x <= EdgeX;

        public bool ProbeWall(System.ReadOnlySpan<Vector3> directions, out Vector3 normal, out int wallId)
        {
            foreach (Vector3 dir in directions)
            {
                if (dir.sqrMagnitude < 1e-4f)
                    continue;
                foreach (Wall w in Walls)
                {
                    float gap = (Position.x - w.X) * w.NormalX - Radius;   // distance entre la capsule et le mur
                    if (dir.x * w.NormalX < -0.01f && gap <= Contact)
                    {
                        normal = new Vector3(w.NormalX, 0f, 0f);
                        wallId = w.Id;
                        return true;
                    }
                }
            }

            normal = Vector3.zero;
            wallId = 0;
            return false;
        }

        public void SetHeight(float height)
        {
            if (height <= Height || HasHeadroom(height))
                Height = height;
        }

        public bool HasHeadroom(float targetHeight)
        {
            bool underCeiling = Position.x >= CeilingMinX && Position.x <= CeilingMaxX;
            return !underCeiling || Position.y + targetHeight <= CeilingY;
        }

        public bool ProbeGround(out Vector3 normal)
        {
            normal = GroundNormal;
            return OverGround(Position) && Position.y <= 1e-3f;
        }

        public MoveResult Move(Vector3 delta, bool snapToGround)
        {
            Vector3 p = Position + delta;
            bool below = false;
            if (OverGround(p) && (p.y < 0f || (snapToGround && p.y < 0.5f)))
            {
                p.y = 0f;
                below = true;
            }

            bool sides = false;
            Vector3 sideNormal = Vector3.zero;
            foreach (Wall w in Walls)
            {
                if ((p.x - w.X) * w.NormalX < Radius)
                {
                    p.x = w.X + w.NormalX * Radius;
                    sides = true;
                    sideNormal = new Vector3(w.NormalX, 0f, 0f);
                }
            }

            var result = new MoveResult
            {
                HitBelow = below, HitSides = sides, ActualDelta = p - Position,
                SideNormal = sideNormal, HasSideNormal = sides,
            };
            Position = p;
            return result;
        }
    }

    public class MovementTests
    {
        private const float Dt = 1f / 60f;
        private MovementSettings _cfg;
        private FakeGroundProbe _probe;
        private MovementState _state;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<MovementSettings>();
            _probe = new FakeGroundProbe { Position = Vector3.zero };
            _state = MovementState.Create(Vector3.zero);
            Step(MovementInput.Neutral); // un tick pour se poser
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        private void Step(MovementInput input) => MovementMotor.Step(ref _state, input, _cfg, Dt, _probe);

        /// <summary>Un seul saut, sans auto-bunny hop : pour tester saut, coyote et buffer isolément.</summary>
        private void Classic()
        {
            _cfg.maxJumps = 1;
            _cfg.autoBunnyHop = false;
        }

        private static MovementInput Forward(bool jumpHeld = false, int pressTick = MovementState.NeverTick) =>
            new MovementInput { Move = Vector2.up, Yaw = 90f, JumpHeld = jumpHeld, JumpPressedTick = pressTick };

        private MovementInput PressJumpNow(bool held = true, Vector2 move = default) => new MovementInput
        {
            Move = move, Yaw = 90f, JumpHeld = held, JumpPressedTick = _state.Tick + 1,
        };

        // --- Course ---

        [Test]
        public void Course_AtteintLaVitesseMaxEnMoinsDe015s()
        {
            int ticks = 0;
            while (_state.HorizontalSpeed < _cfg.runSpeed - 1e-3f && ticks < 120)
            {
                Step(Forward());
                ticks++;
            }

            Assert.That(ticks, Is.LessThanOrEqualTo(9), $"vitesse max atteinte en {ticks} ticks");
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.runSpeed).Within(1e-3f));
        }

        [Test]
        public void Friction_ArretCompletEnMoinsDe04s()
        {
            _state.Velocity = new Vector3(_cfg.runSpeed, 0f, 0f);
            int ticks = 0;
            while (_state.HorizontalSpeed > 0f && ticks < 120)
            {
                Step(MovementInput.Neutral);
                ticks++;
            }

            Assert.That(ticks, Is.LessThanOrEqualTo(24), $"arrêt en {ticks} ticks");
        }

        // --- Saut ---

        [Test]
        public void Saut_HauteurEtTempsAuSommet()
        {
            Classic();
            float startY = _probe.Position.y;
            Step(PressJumpNow());
            int jumpTick = _state.Tick;
            float maxY = startY;
            int apexTick = jumpTick;
            for (int i = 0; i < 120 && !(_state.Grounded && _state.Tick > jumpTick); i++)
            {
                Step(new MovementInput { JumpHeld = true, JumpPressedTick = jumpTick });
                if (_probe.Position.y > maxY)
                {
                    maxY = _probe.Position.y;
                    apexTick = _state.Tick;
                }
            }

            Assert.That(maxY - startY, Is.EqualTo(_cfg.jumpHeight).Within(_cfg.jumpHeight * 0.01f));
            float apexTime = (apexTick - jumpTick + 1) * Dt;
            Assert.That(apexTime, Is.EqualTo(0.36f).Within(Dt * 1.01f), $"sommet en {apexTime:0.000} s");
        }

        [Test]
        public void Saut_SansAutoBhopUnSeulSautParAppui()
        {
            Classic();
            Step(PressJumpNow());
            int press = _state.LastConsumedJumpPressTick;
            int jumps = 1;
            int lastJump = _state.LastJumpTick;
            for (int i = 0; i < 180; i++)
            {
                Step(new MovementInput { JumpHeld = true, JumpPressedTick = press });
                if (_state.LastJumpTick != lastJump)
                {
                    jumps++;
                    lastJump = _state.LastJumpTick;
                }
            }

            Assert.That(jumps, Is.EqualTo(1));
            Assert.That(_state.Grounded, Is.True, "le joueur est bien retombé");
        }

        [Test]
        public void Saut_PasDeDoubleSautParLeCoyoteTime()
        {
            Classic();
            Step(PressJumpNow());
            int firstJump = _state.LastJumpTick;
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = _state.LastConsumedJumpPressTick });
            Step(PressJumpNow()); // nouvel appui 2 ticks après le saut, dans la fenêtre de coyote
            Assert.That(_state.LastJumpTick, Is.EqualTo(firstJump));
        }

        [Test]
        public void Saut_RelacherPendantLaMonteeCoupeLaVitesse()
        {
            Step(PressJumpNow());
            int press = _state.LastConsumedJumpPressTick;
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = press });
            float before = _state.Velocity.y;
            Step(new MovementInput { JumpHeld = false, JumpPressedTick = press });
            float g = _cfg.gravity;
            Assert.That(_state.Velocity.y, Is.EqualTo(before * _cfg.jumpCutMultiplier - g * Dt).Within(1e-3f));
        }

        [Test]
        public void Saut_RelacherEnDescenteNeChangeRien()
        {
            Step(PressJumpNow());
            int press = _state.LastConsumedJumpPressTick;
            while (_state.Velocity.y > 0f)
                Step(new MovementInput { JumpHeld = true, JumpPressedTick = press });
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = press });
            float before = _state.Velocity.y;
            Step(new MovementInput { JumpHeld = false, JumpPressedTick = press });
            float fallG = _cfg.gravity * _cfg.fallGravityMultiplier;
            Assert.That(_state.Velocity.y, Is.EqualTo(before - fallG * Dt).Within(1e-3f));
        }

        // --- Coyote time : 6 ticks à 60 Hz ---

        private bool JumpAfterLeavingEdge(int ticksAfterEdge)
        {
            Classic();
            _probe.EdgeX = 0.5f;
            int lastGrounded = MovementState.NeverTick;
            for (int i = 0; i < 120; i++)
            {
                Step(Forward());
                if (_state.Grounded)
                    lastGrounded = _state.Tick;
                else if (lastGrounded != MovementState.NeverTick)
                    break;
            }

            while (_state.Tick + 1 < lastGrounded + ticksAfterEdge)
                Step(Forward());

            int before = _state.LastJumpTick;
            Step(Forward(jumpHeld: true, pressTick: _state.Tick + 1));
            return _state.LastJumpTick != before;
        }

        [Test]
        public void Coyote_SautAccepte6TicksApresLeBord() => Assert.That(JumpAfterLeavingEdge(6), Is.True);

        [Test]
        public void Coyote_SautRefuse7TicksApresLeBord() => Assert.That(JumpAfterLeavingEdge(7), Is.False);

        // --- Buffer : 7 ticks à 60 Hz ---

        private bool JumpWithPressBeforeLanding(int ticksBeforeLanding)
        {
            Classic();
            // Même chute, rejouée deux fois : la première mesure le tick d'atterrissage.
            var startState = _state;
            _probe.Position = new Vector3(0f, 3f, 0f);
            _state.Velocity = Vector3.zero;
            int landing = 0;
            for (int i = 0; i < 200 && landing == 0; i++)
            {
                Step(MovementInput.Neutral);
                if (_state.Grounded)
                    landing = _state.Tick;
            }

            _state = startState;
            _probe.Position = new Vector3(0f, 3f, 0f);
            _state.Velocity = Vector3.zero;
            int press = landing - ticksBeforeLanding;
            while (_state.Tick < landing)
            {
                bool pressed = _state.Tick + 1 >= press;
                Step(new MovementInput { JumpHeld = pressed, JumpPressedTick = pressed ? press : MovementState.NeverTick });
            }

            return _state.LastJumpTick == landing;
        }

        [Test]
        public void Buffer_Appui7TicksAvantAtterrissageSaute() => Assert.That(JumpWithPressBeforeLanding(7), Is.True);

        [Test]
        public void Buffer_Appui8TicksAvantAtterrissageNeSautePas() =>
            Assert.That(JumpWithPressBeforeLanding(8), Is.False);

        // --- En l'air (façon Source) ---

        [Test]
        public void Air_TenirLaDirectionDeLaVitesseNAjouteRien()
        {
            var v = new Vector3(10f, 0f, 0f);
            Vector3 after = MovementMath.AirAccelerate(v, Vector3.right, _cfg.runSpeed, _cfg.airWishSpeedCap,
                _cfg.airAcceleration, Dt);
            Assert.That(after.magnitude, Is.EqualTo(10f).Within(1e-5f));
        }

        [Test]
        public void Air_StraferA90DegresAjouteDeLaVitesse()
        {
            var v = new Vector3(10f, 0f, 0f);
            Vector3 after = MovementMath.AirAccelerate(v, Vector3.forward, _cfg.runSpeed, _cfg.airWishSpeedCap,
                _cfg.airAcceleration, Dt);
            Assert.That(after.magnitude, Is.GreaterThan(10f));
        }

        [Test]
        public void Parametres_ValeursDuDesign()
        {
            Assert.That(_cfg.JumpVelocity, Is.EqualTo(7.21f).Within(0.01f));
            Assert.That(MovementSettings.SecondsToTicks(_cfg.coyoteTime, Dt), Is.EqualTo(6));
            Assert.That(MovementSettings.SecondsToTicks(_cfg.jumpBuffer, Dt), Is.EqualTo(7));
            Assert.That(_cfg.maxJumps, Is.EqualTo(2));
            Assert.That(_cfg.autoBunnyHop, Is.True);
            Assert.That(_cfg.airControl, Is.EqualTo(0.15f));
            Assert.That(_cfg.maxHorizontalSpeed, Is.EqualTo(50f));
            Assert.That(_cfg.movementCharges, Is.EqualTo(3));
            Assert.That(_cfg.dashDistance, Is.EqualTo(6.5f));
            Assert.That(_cfg.dashDuration, Is.EqualTo(0.3f));
            Assert.That(_cfg.dashProfile, Is.EqualTo(DashProfile.Progressif));
            Assert.That(_cfg.dashSpeedGain, Is.EqualTo(1f));
            Assert.That(_cfg.dashJumpSpeedGain, Is.EqualTo(3f));
            Assert.That(_cfg.dashJumpWindow, Is.EqualTo(0.15f));
            Assert.That(_cfg.dashExitSpeed, Is.EqualTo(12f));
            Assert.That(_cfg.movementGainCap, Is.EqualTo(20f));
            Assert.That(_cfg.airControlMaxAngle, Is.EqualTo(100f));
        }

        // --- Charges de mouvement et dash ---

        private static MovementInput DashNow(int tick, Vector2 move = default, float yaw = 90f) => new MovementInput
        {
            Move = move, Yaw = yaw, JumpPressedTick = MovementState.NeverTick, DashPressedTick = tick,
        };

        private void InAirSinceLongAgo(Vector3 velocity)
        {
            _probe.Position = new Vector3(0f, 100f, 0f);
            _state.Velocity = velocity;
            _state.LastGroundedTick = MovementState.NeverTick;
        }

        [Test]
        public void Charges_TroisPleinesAuDepart()
        {
            Assert.That(_state.Charges, Is.EqualTo(3f).Within(1e-4f));
        }

        [Test]
        public void Charges_LeSautAuSolEstGratuit_LeSautEnLAirCouteUne()
        {
            Assert.That(PressJumpAndCheck(), Is.True);
            Assert.That(_state.Charges, Is.EqualTo(3f).Within(0.02f), "saut au sol gratuit");
            HoldTicks(10);
            float before = _state.Charges;
            Assert.That(PressJumpAndCheck(), Is.True);
            Assert.That(_state.Charges, Is.EqualTo(before - 1f).Within(0.02f), "saut en l'air : une charge");
        }

        [Test]
        public void Charges_SansChargePasDeSautEnLAirNiDeDash()
        {
            InAirSinceLongAgo(Vector3.zero);
            _state.Charges = 0.5f;
            int before = _state.LastJumpTick;
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = _state.Tick + 1, DashPressedTick = MovementState.NeverTick });
            Assert.That(_state.LastJumpTick, Is.EqualTo(before), "pas de saut en l'air");
            Step(DashNow(_state.Tick + 1));
            Assert.That(_state.LastDashTick, Is.EqualTo(MovementState.NeverTick), "pas de dash");
        }

        [Test]
        public void Charges_RechargePlusViteAuSolQuEnLAir()
        {
            _state.Charges = 1f;
            for (int i = 0; i < 60; i++)
                Step(MovementInput.Neutral);
            Assert.That(_state.Charges, Is.EqualTo(2f).Within(0.02f), "au sol : 1 charge par seconde");

            TearDown();                                   // repartir d'un état neuf (le TearDown final reste celui de NUnit)
            SetUp();
            InAirSinceLongAgo(Vector3.zero);
            _state.Charges = 1f;
            for (int i = 0; i < 60; i++)
                Step(MovementInput.Neutral);
            Assert.That(_state.Charges, Is.EqualTo(1f + 1f / 3f).Within(0.02f), "en l'air : 1 charge en 3 s");
        }

        private int DashTicks => MovementSettings.SecondsToTicks(_cfg.dashDuration, Dt);
        private float DashSpeed => _cfg.dashDistance / (DashTicks * Dt);

        /// <summary>Lance un dash puis avance jusqu'à sa fin. Renvoie le nombre de ticks du dash.</summary>
        private int DashToTheEnd(MovementInput press, MovementInput during)
        {
            Step(press);
            Assert.That(_state.LastDashTick, Is.EqualTo(_state.Tick), "dash parti");
            int ticks = 1;
            while (_state.DashTicksLeft > 0 && ticks < 100)
            {
                Step(during);
                ticks++;
            }

            return ticks;
        }

        [Test]
        public void Dash_DureDistanceEtVitesseDeSortie_AuSol()
        {
            Vector3 p0 = _probe.Position;
            int ticks = DashToTheEnd(DashNow(_state.Tick + 1), MovementInput.Neutral);   // yaw 90 : avant = +x
            Assert.That(ticks, Is.EqualTo(DashTicks), "durée");
            Assert.That(_probe.Position.x - p0.x, Is.EqualTo(_cfg.dashDistance).Within(0.05f), "distance");
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.dashExitSpeed).Within(1e-3f), "sortie depuis l'arrêt");
            Assert.That(_state.Charges, Is.EqualTo(2f + _cfg.dashDuration / _cfg.chargeRechargeTimeGround).Within(0.05f),
                "une charge consommée, puis recharge au sol pendant le dash");
            Assert.That(_state.LastDashDistance, Is.EqualTo(_cfg.dashDistance).Within(0.05f), "debug : distance");
        }

        [Test]
        public void Dash_EnLAirNiGraviteNiChutePendantLeDash()
        {
            InAirSinceLongAgo(new Vector3(0f, -10f, 0f));
            float y0 = _probe.Position.y;
            Step(DashNow(_state.Tick + 1));
            Assert.That(_state.LastDashTick, Is.EqualTo(_state.Tick), "le dash part en l'air");
            while (_state.DashTicksLeft > 0)
            {
                Step(MovementInput.Neutral);
                Assert.That(_probe.Position.y, Is.EqualTo(y0).Within(1e-4f), "altitude constante");
            }

            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.dashExitSpeed).Within(1e-3f));
        }

        [Test]
        public void Dash_PasDeChuteBrutaleALaSortieAuSol()
        {
            DashToTheEnd(DashNow(_state.Tick + 1), MovementInput.Neutral);
            for (int i = 0; i < 6; i++)                                       // 0.1 s après la sortie
                Step(MovementInput.Neutral);
            Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(_cfg.dashExitSpeed - 0.01f), "grâce sans friction");
        }

        [Test]
        public void Dash_HuitDirections([Values(0, 1, 2, 3, 4, 5, 6, 7)] int sector)
        {
            float a = sector * 45f * Mathf.Deg2Rad;
            var move = new Vector2(Mathf.Sin(a), Mathf.Cos(a));               // 0 = avant, 2 = droite, 4 = arrière…
            Vector3 p0 = _probe.Position;
            DashToTheEnd(DashNow(_state.Tick + 1, move, yaw: 0f), DashNow(MovementState.NeverTick, move, yaw: 0f));
            var moved = new Vector3(_probe.Position.x - p0.x, 0f, _probe.Position.z - p0.z);
            Vector3 wish = MovementMath.WishVector(move, 0f);
            Assert.That(Vector3.Angle(moved, wish), Is.LessThan(0.5f), "direction");
            Assert.That(moved.magnitude, Is.EqualTo(_cfg.dashDistance).Within(0.05f), "distance");
        }

        [Test]
        public void Dash_EnLAirAHauteVitesse_PlusRapidePendantPuisVitesseDAvant()
        {
            InAirSinceLongAgo(new Vector3(20f, 0f, 0f));                      // bunny hop à 20 m/s vers +x
            Step(DashNow(_state.Tick + 1, move: Vector2.right, yaw: 0f));     // dash vers +x
            Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(20f), "pendant le dash");
            while (_state.DashTicksLeft > 0)
                Step(MovementInput.Neutral);
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(20f).Within(1e-3f), "sortie : la vitesse d'avant");

            TearDown();
            SetUp();
            InAirSinceLongAgo(new Vector3(20f, 0f, 0f));
            Step(DashNow(_state.Tick + 1, move: Vector2.up, yaw: 0f));        // dash sur le côté (+z)
            while (_state.DashTicksLeft > 0)
                Step(MovementInput.Neutral);
            Assert.That(_state.Velocity.z, Is.EqualTo(20f).Within(1e-3f), "20 m/s redirigés");
            Assert.That(_state.Velocity.x, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Dash_EnchainementsMemorisesEtCharges()
        {
            int start = _state.Tick + 1;
            int[] pressAt = { 0, 3, 18, 33 };                                 // le 2e appui tombe pendant le 1er dash
            int press = MovementState.NeverTick, dashes = 0, last = _state.LastDashTick;
            var dashTicks = new System.Collections.Generic.List<int>();
            for (int t = 0; t < 70; t++)
            {
                if (System.Array.IndexOf(pressAt, t) >= 0)
                    press = start + t;
                Step(DashNow(press));
                if (_state.LastDashTick != last)
                {
                    dashes++;
                    last = _state.LastDashTick;
                    dashTicks.Add(_state.LastDashTick - start);
                }
            }

            int gap = Mathf.Max(MovementSettings.SecondsToTicks(_cfg.dashCooldown, Dt), DashTicks);
            Assert.That(dashes, Is.EqualTo(3), "3 charges : le 4e appui est refusé");
            Assert.That(dashTicks, Is.EqualTo(new[] { 0, gap, 2 * gap }), "appuis mémorisés, partis dès la fin du délai");
        }

        [Test]
        public void Dash_UnSautTermineLeDash()
        {
            Step(DashNow(_state.Tick + 1));
            Step(MovementInput.Neutral);
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = _state.Tick + 1, Yaw = 90f,
                DashPressedTick = MovementState.NeverTick });
            Assert.That(_state.DashTicksLeft, Is.EqualTo(0));
            Assert.That(_state.Velocity.y, Is.GreaterThan(0f), "le saut part");
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.dashExitSpeed).Within(1e-3f), "vitesse de sortie du dash");
        }

        [Test]
        public void Charges_EnLAirSautPuisDeuxDashs_PuisPlusRien()
        {
            Assert.That(PressJumpAndCheck(), Is.True, "saut au sol (gratuit)");
            HoldTicks(5);
            Assert.That(PressJumpAndCheck(), Is.True, "saut en l'air : 3 → 2");
            int cooldown = Mathf.Max(MovementSettings.SecondsToTicks(_cfg.dashCooldown, Dt), DashTicks);
            for (int dash = 0; dash < 2; dash++)
            {
                HoldTicks(cooldown);
                int before = _state.LastDashTick;
                Step(DashNow(_state.Tick + 1));
                Assert.That(_state.LastDashTick, Is.Not.EqualTo(before), $"dash {dash + 1}");
            }

            HoldTicks(cooldown);
            int last = _state.LastDashTick;
            Step(DashNow(_state.Tick + 1));
            Assert.That(_state.LastDashTick, Is.EqualTo(last), "plus de charge entière");
        }

        /// <summary>Vitesses horizontales pendant un dash (une par tick), départ du point courant.</summary>
        private System.Collections.Generic.List<float> DashProfileFrom(MovementInput press)
        {
            var speeds = new System.Collections.Generic.List<float>();
            Step(press);
            speeds.Add(_state.HorizontalSpeed);
            while (_state.DashTicksLeft > 0)
            {
                Step(MovementInput.Neutral);
                if (_state.DashTicksLeft > 0)
                    speeds.Add(_state.HorizontalSpeed);
            }

            return speeds;
        }

        [Test]
        public void Dash_ProfilProgressif_PicPlusBasEtSansCassure()
        {
            InAirSinceLongAgo(Vector3.zero);
            var speeds = DashProfileFrom(DashNow(_state.Tick + 1));
            Assert.That(speeds[0], Is.LessThan(32f), "pic plus bas qu'avant (35.5 m/s)");
            for (int i = 1; i < speeds.Count; i++)
                Assert.That(speeds[i], Is.LessThan(speeds[i - 1]), $"baisse régulière au tick {i}");
            // Le dernier tick du dash roule à ≈ 13 m/s mais affiche déjà la sortie : on regarde l'avant-dernier,
            // qui doit être à moins de deux pas de baisse de la sortie (pas de marche brutale).
            float step = speeds[0] - speeds[1];
            Assert.That(speeds[speeds.Count - 1] - _cfg.dashExitSpeed, Is.LessThan(2f * step + 1e-3f), "pas de cassure à la sortie");
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.dashExitSpeed).Within(1e-3f));
            Assert.That(_state.LastDashDistance, Is.EqualTo(_cfg.dashDistance).Within(0.05f));
        }

        [Test]
        public void Dash_ProfilConstantReglable()
        {
            _cfg.dashProfile = DashProfile.Constant;
            InAirSinceLongAgo(Vector3.zero);
            var speeds = DashProfileFrom(DashNow(_state.Tick + 1));
            foreach (float v in speeds)
                Assert.That(v, Is.EqualTo(speeds[0]).Within(1e-3f));
            Assert.That(_state.LastDashDistance, Is.EqualTo(_cfg.dashDistance).Within(0.05f));
        }

        [Test]
        public void Dash_AuSolGagneUnMetreParSeconde([Values(10f, 15f, 19.5f, 22f)] float before)
        {
            RunAt(before);
            DashToTheEnd(DashNow(_state.Tick + 1), MovementInput.Neutral);
            float expected = Mathf.Max(_cfg.dashExitSpeed, MovementMath.CappedGain(before, _cfg.dashSpeedGain, _cfg.movementGainCap));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(expected).Within(1e-3f),
                $"{before} m/s → {expected} (sortie ≥ 12, +1 plafonné à 20)");
        }

        [Test]
        public void Dash_EnLAirAucunGainEtRedirection([Values(0, 2, 4, 6)] int sector)
        {
            InAirSinceLongAgo(new Vector3(15f, 0f, 0f));
            float a = sector * 45f * Mathf.Deg2Rad;
            var move = new Vector2(Mathf.Sin(a), Mathf.Cos(a));               // avant, droite, arrière, gauche
            Step(DashNow(_state.Tick + 1, move, yaw: 0f));
            while (_state.DashTicksLeft > 0)
                Step(MovementInput.Neutral);
            var h = new Vector3(_state.Velocity.x, 0f, _state.Velocity.z);
            Assert.That(h.magnitude, Is.EqualTo(15f).Within(1e-3f), "vitesse exactement conservée");
            Assert.That(Vector3.Angle(h, MovementMath.WishVector(move, 0f)), Is.LessThan(0.5f), "redirigée");
        }

        private MovementInput JumpNow() => new MovementInput
        {
            Yaw = 90f, JumpHeld = true, JumpPressedTick = _state.Tick + 1, DashPressedTick = MovementState.NeverTick,
        };

        [Test]
        public void DashJump_ReussiPendantLeDash()
        {
            RunAt(15f);
            Step(DashNow(_state.Tick + 1));
            for (int i = 0; i < 4; i++)
                Step(MovementInput.Neutral);
            Step(JumpNow());
            Assert.That(_state.LastDashJumpSuccess, Is.True);
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(18f).Within(1e-3f), "15 + 3");
        }

        [Test]
        public void DashJump_ReussiJusteApresLeDash()
        {
            RunAt(15f);
            DashToTheEnd(DashNow(_state.Tick + 1), MovementInput.Neutral);
            for (int i = 0; i < 5; i++)                                      // 5 ticks < fenêtre de 9 ticks
                Step(MovementInput.Neutral);
            Step(JumpNow());
            Assert.That(_state.LastDashJumpSuccess, Is.True);
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(18f).Within(1e-3f));
        }

        [Test]
        public void DashJump_RateHorsFenetre()
        {
            RunAt(15f);
            DashToTheEnd(DashNow(_state.Tick + 1), MovementInput.Neutral);
            for (int i = 0; i < 15; i++)                                     // 15 ticks > fenêtre de 9 ticks
                Step(MovementInput.Neutral);
            Step(JumpNow());
            Assert.That(_state.LastDashJumpSuccess, Is.False);
            Assert.That(_state.LastDashJumpLateTicks, Is.GreaterThan(0), "retard affiché dans le debug");
            Assert.That(_state.HorizontalSpeed, Is.LessThanOrEqualTo(16f + 1e-3f), "pas de +3");
        }

        [Test]
        public void DashJump_PlafonneA20([Values(19f, 22f)] float before)
        {
            RunAt(before);
            Step(DashNow(_state.Tick + 1));
            Step(MovementInput.Neutral);
            Step(JumpNow());
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(Mathf.Max(before, _cfg.movementGainCap)).Within(1e-3f));
        }

        [Test]
        public void DashJump_PasEnLAir()
        {
            InAirSinceLongAgo(new Vector3(15f, 0f, 0f));
            Step(DashNow(_state.Tick + 1));
            Step(MovementInput.Neutral);
            Step(JumpNow());                                                 // saut en l'air pendant le dash
            Assert.That(_state.LastDashJumpTick, Is.EqualTo(MovementState.NeverTick), "pas de dash-jump en l'air");
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(15f).Within(1e-3f), "aucun gain");
        }

        [Test]
        public void WallJump_GainPlafonneA20([Values(19f, 25f)] float alongWallSpeed)
        {
            WallJumpFrom(alongWallSpeed, yaw: 0f);
            float expected = Mathf.Max(alongWallSpeed, _cfg.movementGainCap);
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(expected).Within(0.05f),
                alongWallSpeed < _cfg.movementGainCap ? "gain jusqu'à 20 m/s" : "au-delà : vitesse conservée, pas de gain");
        }

        // --- Contrôle aérien (W seul, yaw 0 = regarder vers +z) ---

        private static float HorizontalAngle(Vector3 v, Vector3 dir) => Vector3.Angle(new Vector3(v.x, 0f, v.z), dir);

        private void LaunchInAir(Vector3 horizontalVelocity)
        {
            _probe.Position = new Vector3(0f, 20f, 0f);
            _state.Velocity = horizontalVelocity;
        }

        private static readonly MovementInput ForwardYaw0 = new MovementInput
        {
            Move = Vector2.up, Yaw = 0f, JumpPressedTick = MovementState.NeverTick,
        };

        [Test]
        public void ControleAerien_A1LaVitesseSuitLaCameraInstantanement()
        {
            _cfg.airControl = 1f;
            LaunchInAir(new Vector3(10f, 0f, 0f));
            Step(ForwardYaw0);
            Assert.That(HorizontalAngle(_state.Velocity, Vector3.forward), Is.LessThan(0.5f));
            Assert.That(_state.HorizontalSpeed, Is.InRange(10f, 10f + _cfg.airWishSpeedCap + 0.01f));
        }

        [Test]
        public void ControleAerien_A0RestePurSource()
        {
            _cfg.airControl = 0f;
            LaunchInAir(new Vector3(10f, 0f, 0f));
            Step(ForwardYaw0);
            Assert.That(HorizontalAngle(_state.Velocity, Vector3.forward), Is.GreaterThan(80f));
        }

        [Test]
        public void ControleAerien_ParDefautTourneProgressivementSansPerdreDeVitesse()
        {
            LaunchInAir(new Vector3(10f, 0f, 0f));
            Step(ForwardYaw0);
            float afterOneTick = HorizontalAngle(_state.Velocity, Vector3.forward);
            Assert.That(afterOneTick, Is.InRange(60f, 85f), "tourne un peu dès le premier tick");

            for (int i = 0; i < 29; i++)
            {
                _probe.Position = new Vector3(_probe.Position.x, 20f, _probe.Position.z); // reste en l'air
                Step(ForwardYaw0);
                Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(10f - 1e-3f), "jamais de perte de vitesse");
            }

            Assert.That(HorizontalAngle(_state.Velocity, Vector3.forward), Is.LessThan(2f), "aligné en 0.5 s");
            Assert.That(_state.HorizontalSpeed, Is.LessThan(10f + _cfg.airWishSpeedCap + 0.01f));
        }

        [Test]
        public void ControleAerien_PasDeDemiTourAuDelaDe100Degres()
        {
            _cfg.airControl = 1f;
            LaunchInAir(new Vector3(0f, 0f, -10f));       // on s'éloigne vers −z, la caméra regarde +z (180°)
            Step(ForwardYaw0);
            Assert.That(HorizontalAngle(_state.Velocity, Vector3.back), Is.LessThan(1f), "trajectoire inchangée");

            LaunchInAir(new Vector3(10f, 0f, -10f));      // 135° : au-delà du seuil
            Step(ForwardYaw0);
            Assert.That(HorizontalAngle(_state.Velocity, Vector3.forward), Is.GreaterThan(130f));
        }

        [Test]
        public void ControleAerien_IgnoreEnStrafantEtLeGainSourceReste()
        {
            _cfg.airControl = 1f;
            LaunchInAir(new Vector3(0f, 0f, 10f));
            Step(new MovementInput { Move = Vector2.right, Yaw = 0f, JumpPressedTick = MovementState.NeverTick });
            Assert.That(HorizontalAngle(_state.Velocity, Vector3.forward), Is.LessThan(10f), "pas de virage forcé");
            Assert.That(_state.HorizontalSpeed, Is.GreaterThan(10f), "le strafe ajoute de la vitesse");
        }

        // --- Auto-bunny hop ---

        private int CountJumps(int ticks, System.Func<MovementInput> input)
        {
            int jumps = 0;
            int last = _state.LastJumpTick;
            for (int i = 0; i < ticks; i++)
            {
                Step(input());
                if (_state.LastJumpTick != last)
                {
                    jumps++;
                    last = _state.LastJumpTick;
                }
            }

            return jumps;
        }

        [Test]
        public void AutoBhop_MaintenirResauteAChaqueAtterrissage()
        {
            int press = _state.Tick + 1;
            int jumps = CountJumps(180, () => new MovementInput { JumpHeld = true, JumpPressedTick = press });
            Assert.That(jumps, Is.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void AutoBhop_Desactive_UnSeulSaut()
        {
            _cfg.autoBunnyHop = false;
            int press = _state.Tick + 1;
            int jumps = CountJumps(180, () => new MovementInput { JumpHeld = true, JumpPressedTick = press });
            Assert.That(jumps, Is.EqualTo(1));
        }

        [Test]
        public void AutoBhop_MaintenirNeDeclenchePasDeSautEnLAir()
        {
            int press = _state.Tick + 1;
            int jumps = CountJumps(30, () => new MovementInput { JumpHeld = true, JumpPressedTick = press });
            Assert.That(jumps, Is.EqualTo(1), "30 ticks < durée d'un saut : un seul saut");
            Assert.That(_state.JumpsUsed, Is.EqualTo(1));
        }

        [Test]
        public void AutoBhop_GardeUneGrandeVitesseSansFriction()
        {
            _state.Velocity = new Vector3(30f, 0f, 0f);
            int press = _state.Tick + 1;
            CountJumps(180, () => new MovementInput { JumpHeld = true, JumpPressedTick = press });
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(30f).Within(1e-3f));
        }

        // --- Sauts multiples ---

        private bool PressJumpAndCheck(bool held = true)
        {
            int before = _state.LastJumpTick;
            Step(PressJumpNow(held));
            return _state.LastJumpTick != before;
        }

        /// <summary>Avance de quelques ticks sans nouvel appui (held : bouton toujours maintenu).</summary>
        private void HoldTicks(int ticks, bool held = false)
        {
            int press = _state.LastConsumedJumpPressTick;
            for (int i = 0; i < ticks; i++)
                Step(new MovementInput { JumpHeld = held, JumpPressedTick = press });
        }

        [Test]
        public void SautsMultiples_DeuxParDefaut_LeTroisiemeEstRefuse()
        {
            Assert.That(PressJumpAndCheck(), Is.True, "saut au sol");
            HoldTicks(10);
            Assert.That(PressJumpAndCheck(), Is.True, "saut en l'air");
            Assert.That(_state.JumpsUsed, Is.EqualTo(2));
            HoldTicks(5);
            Assert.That(PressJumpAndCheck(), Is.False, "plus de saut");
        }

        [Test]
        public void SautsMultiples_ReglableATrois()
        {
            _cfg.maxJumps = 3;
            Assert.That(PressJumpAndCheck(), Is.True);
            HoldTicks(10);
            Assert.That(PressJumpAndCheck(), Is.True);
            HoldTicks(10);
            Assert.That(PressJumpAndCheck(), Is.True);
            HoldTicks(5);
            Assert.That(PressJumpAndCheck(), Is.False);
        }

        [Test]
        public void SautsMultiples_LeSautEnLAirGardeLaVitesseHorizontale()
        {
            _state.Velocity = new Vector3(9f, 0f, 0f);
            PressJumpAndCheck();
            HoldTicks(25, held: true); // saut complet : sommet vers 22 ticks, donc en descente
            Assert.That(_state.Grounded, Is.False);
            Assert.That(_state.Velocity.y, Is.LessThan(0f));
            Vector3 before = _state.Velocity;
            Assert.That(PressJumpAndCheck(), Is.True);
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(new Vector2(before.x, before.z).magnitude).Within(1e-3f));
            Assert.That(_state.Velocity.y, Is.GreaterThan(0f), "le saut en l'air annule la chute");
        }

        [Test]
        public void SautsMultiples_RechargesAuSol()
        {
            PressJumpAndCheck();
            HoldTicks(10);
            PressJumpAndCheck();
            for (int i = 0; i < 200 && !_state.Grounded; i++)
                HoldTicks(1);
            Assert.That(_state.Grounded, Is.True);
            Assert.That(_state.JumpsUsed, Is.EqualTo(0));
        }

        [Test]
        public void SautsMultiples_TomberDUnBordLaisseLeSeulSautEnLAir()
        {
            _probe.EdgeX = 0.5f;
            for (int i = 0; i < 120 && (_state.Grounded || _state.Tick < 3); i++)
                Step(Forward());
            for (int i = 0; i < 10; i++) // au-delà du coyote time
                Step(Forward());
            Assert.That(PressJumpAndCheck(), Is.True, "le saut en l'air reste");
            HoldTicks(5);
            Assert.That(PressJumpAndCheck(), Is.False, "le saut au sol est perdu");
        }

        // --- Plafond de sécurité ---

        [Test]
        public void Securite_VitesseHorizontalePlafonnee()
        {
            LaunchInAir(new Vector3(100f, 0f, 0f));
            Step(MovementInput.Neutral);
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.maxHorizontalSpeed).Within(1e-3f));
        }

        // --- Murs : wall slide et wall jump (yaw 0 : +x = droite) ---

        private const int WallA = 1, WallB = 2;

        /// <summary>
        /// Mur A en x = 1 (à droite du joueur), joueur collé dessus, en l'air à 100 m depuis longtemps
        /// (hors du coyote time : sinon le saut au sol, prioritaire, partirait à la place du wall jump).
        /// </summary>
        private void HangOnWallA(float alongWallSpeed = 0f)
        {
            _probe.Walls.Add(new FakeGroundProbe.Wall { X = 1f, NormalX = -1f, Id = WallA });
            _probe.Position = new Vector3(0.6f, 100f, 0f);
            _state.Velocity = new Vector3(0f, 0f, alongWallSpeed);
            _state.LastGroundedTick = MovementState.NeverTick;
        }

        private static MovementInput Push(float x, int press = MovementState.NeverTick, bool held = false) =>
            new MovementInput { Move = new Vector2(x, 0f), Yaw = 0f, JumpPressedTick = press, JumpHeld = held };

        [Test]
        public void WallSlide_LimiteLaChuteA3()
        {
            HangOnWallA();
            for (int i = 0; i < 60; i++)
                Step(Push(1f));
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.WallSlide));
            Assert.That(_state.Velocity.y, Is.GreaterThanOrEqualTo(-_cfg.wallSlideMaxFallSpeed - 1e-3f));
        }

        [Test]
        public void WallSlide_ConserveLaVitesseLeLongDuMur()
        {
            HangOnWallA(alongWallSpeed: 12f);
            for (int i = 0; i < 60; i++)
                Step(Push(1f));
            Assert.That(_state.Velocity.z, Is.EqualTo(12f).Within(1e-3f));
            Assert.That(_state.Velocity.x, Is.EqualTo(0f).Within(1e-3f), "rien n'entre dans le mur");
        }

        [Test]
        public void WallSlide_SansPousserOnTombeNormalement()
        {
            HangOnWallA();
            for (int i = 0; i < 60; i++)
                Step(MovementInput.Neutral);
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Air));
            Assert.That(_state.Velocity.y, Is.LessThan(-_cfg.wallSlideMaxFallSpeed));
        }

        [Test]
        public void WallJump_ImpulsionLoinDuMurEnGardantLElan()
        {
            _cfg.wallJumpForwardBias = 0f;               // pur éloignement du mur
            HangOnWallA(alongWallSpeed: 12f);
            Step(Push(1f));
            Assert.That(_state.OnWall, Is.True);
            int jumpsUsed = _state.JumpsUsed;
            Step(Push(1f, press: _state.Tick + 1, held: true));
            Assert.That(_state.LastJumpTick, Is.EqualTo(_state.Tick));
            Assert.That(_state.Velocity.x, Is.EqualTo(-_cfg.wallJumpKick).Within(1e-3f), "loin du mur");
            Assert.That(_state.Velocity.z, Is.EqualTo(12f).Within(1e-3f), "élan le long du mur conservé");
            Assert.That(_state.Velocity.y, Is.GreaterThan(5f));
            Assert.That(_state.JumpsUsed, Is.EqualTo(jumpsUsed), "le wall jump ne consomme pas de saut");
        }

        [Test]
        public void WallJump_VerrouDeDirectionPendant015s()
        {
            _cfg.wallJumpForwardBias = 0f;
            HangOnWallA();
            Step(Push(1f));
            int press = _state.Tick + 1;
            Step(Push(1f, press, held: true));
            int jumpTick = _state.Tick;
            int lockTicks = MovementSettings.SecondsToTicks(_cfg.wallJumpSteerLock, Dt);
            while (_state.Tick < jumpTick + lockTicks - 1)
            {
                Step(Push(1f, press, held: true));       // on pousse vers le mur : ignoré pendant le verrou
                Assert.That(_state.Velocity.x, Is.EqualTo(-_cfg.wallJumpKick).Within(1e-3f), $"tick {_state.Tick - jumpTick}");
            }

            Step(Push(1f, press, held: true));
            Step(Push(1f, press, held: true));
            Assert.That(_state.Velocity.x, Is.GreaterThan(-_cfg.wallJumpKick + 1e-3f), "contrôle rendu après le verrou");
        }

        private bool WallJumpAfterLeavingWall(int ticksAfterContact)
        {
            _cfg.maxJumps = 1;                            // pas de saut en l'air : seul le wall jump peut partir
            _cfg.wallJumpForwardBias = 0f;
            HangOnWallA();
            Step(Push(1f));
            int contact = _state.LastWallTick;
            _probe.Position = new Vector3(0f, 100f, 0f);  // décroché du mur (0.6 m d'écart)
            while (_state.Tick + 1 < contact + ticksAfterContact)
                Step(MovementInput.Neutral);
            int before = _state.LastJumpTick;
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = _state.Tick + 1 });
            return _state.LastJumpTick != before && _state.Velocity.x < -_cfg.wallJumpKick + 0.01f;
        }

        [Test]
        public void WallJump_WallCoyoteAccepte6Ticks() => Assert.That(WallJumpAfterLeavingWall(6), Is.True);

        [Test]
        public void WallJump_WallCoyoteRefuse7Ticks() => Assert.That(WallJumpAfterLeavingWall(7), Is.False);

        [Test]
        public void WallJump_PasDeuxFoisDeSuiteLeMemeMur()
        {
            _cfg.maxJumps = 1;
            HangOnWallA();
            Step(Push(1f));
            Step(Push(1f, press: _state.Tick + 1, held: true));
            Assert.That(_state.LastWallJumpId, Is.EqualTo(WallA));

            // Retour sur le mur A : refusé.
            _probe.Position = new Vector3(0.6f, 100f, 0f);
            _state.Velocity = Vector3.zero;
            Step(Push(1f));
            int before = _state.LastJumpTick;
            Step(Push(1f, press: _state.Tick + 1, held: true));
            Assert.That(_state.LastJumpTick, Is.EqualTo(before), "même mur deux fois de suite");

            // Mur B (x = −1, à gauche) : accepté.
            _probe.Walls.Add(new FakeGroundProbe.Wall { X = -1f, NormalX = 1f, Id = WallB });
            _probe.Position = new Vector3(-0.6f, 100f, 0f);
            _state.Velocity = Vector3.zero;
            Step(Push(-1f));
            Step(Push(-1f, press: _state.Tick + 1, held: true));
            Assert.That(_state.LastWallJumpId, Is.EqualTo(WallB));

            // Puis de nouveau A : accepté (un autre mur a été touché entre-temps).
            _probe.Position = new Vector3(0.6f, 100f, 0f);
            _state.Velocity = Vector3.zero;
            Step(Push(1f));
            Step(Push(1f, press: _state.Tick + 1, held: true));
            Assert.That(_state.LastWallJumpId, Is.EqualTo(WallA));
        }

        [Test]
        public void Mur_LeContactRechargeLesSauts()
        {
            Assert.That(PressJumpAndCheck(), Is.True, "saut au sol");
            HoldTicks(10);
            Assert.That(PressJumpAndCheck(), Is.True, "saut en l'air");
            Assert.That(_state.JumpsUsed, Is.EqualTo(2));

            HangOnWallA();
            Step(Push(1f));
            Assert.That(_state.JumpsUsed, Is.EqualTo(0), "recharge au contact du mur");

            _probe.Position = new Vector3(0f, 100f, 0f);  // on quitte le mur, au-delà du wall-coyote
            for (int i = 0; i < 10; i++)
                Step(MovementInput.Neutral);
            int before = _state.LastJumpTick;
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = _state.Tick + 1 });
            Assert.That(_state.LastJumpTick, Is.Not.EqualTo(before), "un saut en l'air est de nouveau disponible");
            Assert.That(_state.Velocity.x, Is.EqualTo(0f).Within(1e-3f), "c'est un saut en l'air, pas un wall jump");
        }

        // --- Corrections des murs après playtest ---

        private void WallJumpFrom(float alongWallSpeed, float yaw)
        {
            HangOnWallA(alongWallSpeed);
            Step(Push(1f));                                                   // contact
            Step(new MovementInput { Move = Vector2.zero, Yaw = yaw, JumpHeld = true, JumpPressedTick = _state.Tick + 1,
                DashPressedTick = MovementState.NeverTick });
            Assert.That(_state.LastJumpTick, Is.EqualTo(_state.Tick), "wall jump parti");
        }

        [Test]
        public void WallJump_MonteDe18m()
        {
            HangOnWallA();
            for (int i = 0; i < 30; i++)
                Step(Push(1f));                                               // wall slide : chute à 3 m/s
            float y0 = _probe.Position.y;
            int press = _state.Tick + 1;
            Step(new MovementInput { JumpHeld = true, JumpPressedTick = press, Yaw = 0f, DashPressedTick = MovementState.NeverTick });
            float maxY = _probe.Position.y;
            for (int i = 0; i < 90; i++)
            {
                Step(new MovementInput { JumpHeld = true, JumpPressedTick = press, Yaw = 0f, DashPressedTick = MovementState.NeverTick });
                maxY = Mathf.Max(maxY, _probe.Position.y);
            }

            Assert.That(maxY - y0, Is.EqualTo(_cfg.wallJumpHeight).Within(_cfg.wallJumpHeight * 0.03f));
            Assert.That(_cfg.wallJumpHeight, Is.EqualTo(1.8f));
        }

        [Test]
        public void WallJump_DirectionMelangeNormaleEtCamera([Values(0f, 0.5f, 1f)] float bias)
        {
            _cfg.wallJumpForwardBias = bias;
            WallJumpFrom(alongWallSpeed: 0f, yaw: 0f);                        // mur à +x, caméra le long du mur (+z)
            var h = new Vector3(_state.Velocity.x, 0f, _state.Velocity.z);
            var expected = (new Vector3(-1f, 0f, 0f) * (1f - bias) + Vector3.forward * bias).normalized;
            Assert.That(Vector3.Angle(h, expected), Is.LessThan(1f), $"bias {bias}");
            Assert.That(h.magnitude, Is.EqualTo(_cfg.wallJumpKick).Within(1e-3f));
        }

        [Test]
        public void WallJump_CameraFaceAuMur_PasVersLeMur()
        {
            WallJumpFrom(alongWallSpeed: 10f, yaw: 90f);                      // la caméra regarde le mur (+x)
            Assert.That(_state.Velocity.x, Is.LessThan(-1f), "on s'éloigne du mur");
            Assert.That(_state.Velocity.z, Is.GreaterThan(10f), "vers l'avant de la trajectoire");
        }

        [Test]
        public void WallJump_VitesseConserveePlusBonus([Values(0f, 90f, 180f)] float yaw)
        {
            WallJumpFrom(alongWallSpeed: 15f, yaw: yaw);                      // yaw 180 : caméra à contre-sens
            Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(15f + _cfg.wallJumpSpeedBonus - 1e-3f));
        }

        [Test]
        public void WallSlide_SansPerteAvecLaCameraVersLeMur()
        {
            HangOnWallA(alongWallSpeed: 15f);
            var input = new MovementInput { Move = Vector2.up, Yaw = 20f, JumpPressedTick = MovementState.NeverTick,
                DashPressedTick = MovementState.NeverTick };                  // W seul, caméra à 20° vers le mur
            for (int i = 0; i < 60; i++)
            {
                Step(input);
                Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(15f - 1e-3f), $"tick {i}");
            }

            Assert.That(_state.Mode, Is.EqualTo(MovementMode.WallSlide));
        }

        [Test]
        public void WallSlide_SActiveSansPousserQuandOnLongeLeMur()
        {
            HangOnWallA(alongWallSpeed: 10f);
            for (int i = 0; i < 30; i++)
                Step(MovementInput.Neutral);
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.WallSlide));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(10f).Within(1e-3f));
        }

        [Test]
        public void WallSlide_ToleranceDAngleDeLEntree()
        {
            // À l'arrêt le long du mur : seule l'entrée compte. 60° du mur : accroché ; 85° : non (tolérance 75°).
            foreach ((float angle, bool expected) in new[] { (60f, true), (85f, false) })
            {
                TearDown();
                SetUp();
                HangOnWallA();
                float a = angle * Mathf.Deg2Rad;
                var input = new MovementInput { Move = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), Yaw = 0f,
                    JumpPressedTick = MovementState.NeverTick, DashPressedTick = MovementState.NeverTick };
                Step(input);
                _state.Velocity = new Vector3(0f, -1f, 0f);
                Step(input);
                Assert.That(_state.Mode == MovementMode.WallSlide, Is.EqualTo(expected), $"{angle}°");
            }
        }

        [Test]
        public void WallRun_ChuteRalentieAHauteVitesse()
        {
            HangOnWallA(alongWallSpeed: 15f);
            for (int i = 0; i < 60; i++)
                Step(MovementInput.Neutral);
            Assert.That(_state.Velocity.y, Is.GreaterThanOrEqualTo(-_cfg.wallRunMaxFallSpeed - 1e-3f), "15 m/s : chute ≤ 0.5 m/s");

            TearDown();
            SetUp();
            HangOnWallA();
            for (int i = 0; i < 60; i++)
                Step(Push(1f));
            Assert.That(_state.Velocity.y, Is.EqualTo(-_cfg.wallSlideMaxFallSpeed).Within(1e-3f), "à l'arrêt : 3 m/s");
        }

        [Test]
        public void Mur_NeRechargePasLesChargesParDefaut()
        {
            HangOnWallA();
            _state.Charges = 1f;
            Step(Push(1f));
            Assert.That(_state.Charges, Is.LessThan(1.1f), "seulement la recharge lente en l'air");

            _cfg.wallRefillsCharges = true;
            Step(Push(1f));
            Assert.That(_state.Charges, Is.EqualTo(_cfg.movementCharges).Within(1e-4f), "option : recharge au mur");
        }

        [Test]
        public void WallJump_OnMonteDansUneChemineeDe3m()
        {
            _probe.Walls.Add(new FakeGroundProbe.Wall { X = 1.5f, NormalX = -1f, Id = WallA });
            _probe.Walls.Add(new FakeGroundProbe.Wall { X = -1.5f, NormalX = 1f, Id = WallB });
            _cfg.maxJumps = 1;                            // seuls les wall jumps comptent
            _probe.Position = new Vector3(1.1f, 5f, 0f);
            _state.Velocity = Vector3.zero;
            _state.LastGroundedTick = MovementState.NeverTick;
            float startY = _probe.Position.y;

            float heading = 1f;   // on pousse vers le mur visé
            int jumps = 0;
            int last = _state.LastJumpTick;
            for (int i = 0; i < 400 && jumps < 4; i++)
            {
                bool newWall = _state.OnWall && _state.LastWallId != _state.LastWallJumpId;
                int press = newWall ? _state.Tick + 1 : MovementState.NeverTick;
                Step(Push(heading, press, held: true));
                if (_state.LastJumpTick != last)
                {
                    jumps++;
                    last = _state.LastJumpTick;
                    heading = -heading;
                }
            }

            Assert.That(jumps, Is.EqualTo(4), "quatre wall jumps en alternant les parois");
            Assert.That(_probe.Position.y, Is.GreaterThan(startY + 2f), "on a pris de la hauteur");
        }

        // --- Glissade et accroupi (déplacement vers +x) ---

        private static MovementInput SlideIn(bool slide = true, bool jumpHeld = false,
            int press = MovementState.NeverTick, Vector2 move = default) => new MovementInput
        {
            Move = move, Yaw = 90f, SlideHeld = slide, JumpHeld = jumpHeld, JumpPressedTick = press,
        };

        private void RunAt(float speed) => _state.Velocity = new Vector3(speed, 0f, 0f);

        [Test]
        public void Glissade_EntreeDonneLeBonusEtBaisseLaCapsule()
        {
            RunAt(7.5f);
            Step(SlideIn());
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Slide));
            Assert.That(_state.Crouched, Is.True);
            Assert.That(_probe.Height, Is.EqualTo(_cfg.crouchHeight));
            Assert.That(_state.HorizontalSpeed, Is.InRange(9.3f, 9.5f), "7.5 + 2, moins un tick de friction");
        }

        [Test]
        public void Glissade_SousLaVitesseMinimaleOnSAccroupit()
        {
            RunAt(5f);
            Step(SlideIn());
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Ground));
            Assert.That(_state.Crouched, Is.True);
            for (int i = 0; i < 120; i++)
                Step(SlideIn(move: Vector2.up));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.crouchSpeed).Within(0.01f), "marche accroupie");
        }

        [Test]
        public void Glissade_LeBonusARechargeDe08s()
        {
            RunAt(7.5f);
            Step(SlideIn());                          // bonus
            Step(SlideIn(slide: false));              // relâche : debout
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Ground));
            RunAt(7.5f);
            Step(SlideIn());                          // trop tôt : pas de bonus
            Assert.That(_state.HorizontalSpeed, Is.LessThan(7.5f));

            Step(SlideIn(slide: false));
            for (int i = 0; i < 48; i++)
                Step(MovementInput.Neutral);
            RunAt(7.5f);
            Step(SlideIn());                          // plus de 0.8 s après le premier bonus
            Assert.That(_state.HorizontalSpeed, Is.GreaterThan(9f));
        }

        [Test]
        public void Glissade_APlatRalentitLentementPuisSArrete()
        {
            RunAt(7.5f);
            Step(SlideIn());
            for (int i = 0; i < 60; i++)
                Step(SlideIn());
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Slide), "toujours en glissade après 1 s");
            Assert.That(_state.HorizontalSpeed, Is.InRange(6.2f, 6.7f), "≈ 9.45 − 3 m/s² × 1 s");

            for (int i = 0; i < 120 && _state.Mode == MovementMode.Slide; i++)
                Step(SlideIn());
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Ground), "fin de glissade sous 4 m/s");
            Assert.That(_state.Crouched, Is.True, "bouton toujours tenu : accroupi");
        }

        [Test]
        public void Glissade_EntreeA20ConserveToutLElanPlusLeBonus()
        {
            RunAt(20f);
            Step(SlideIn());
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Slide));
            // 20 m/s = plafond commun des gains : l'élan est conservé, le bonus ne s'ajoute plus.
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(20f - _cfg.slideDeceleration * Dt).Within(1e-3f));
        }

        [Test]
        public void Glissade_AtterrirA20DepuisUnBunnyHopConserveLElan()
        {
            _probe.Position = new Vector3(0f, 1f, 0f);
            RunAt(20f);
            Step(SlideIn());
            for (int i = 0; i < 120 && !_state.Grounded; i++)
                Step(SlideIn());
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Slide));
            Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(20f - 0.1f), "élan conservé (bonus plafonné)");
        }

        [Test]
        public void Glissade_A20DurePlusieursSecondesAPlat()
        {
            RunAt(20f);
            Step(SlideIn());
            int ticks = 0;
            while (_state.Mode == MovementMode.Slide && ticks < 1200)
            {
                Step(SlideIn());
                ticks++;
            }

            float seconds = ticks * Dt;
            Assert.That(seconds, Is.InRange(5f, 7f), $"glissade de {seconds:0.00} s ((22 − 4) / 3 ≈ 6 s attendues)");
        }

        [Test]
        public void Glissade_DecelerationConstanteQuelleQueSoitLaVitesse()
        {
            float LossOverOneSecond(float start)
            {
                SetUp();
                RunAt(start);
                Step(SlideIn());
                float before = _state.HorizontalSpeed;
                for (int i = 0; i < 60; i++)
                    Step(SlideIn());
                float loss = before - _state.HorizontalSpeed;
                TearDown();
                return loss;
            }

            Assert.That(LossOverOneSecond(20f), Is.EqualTo(3f).Within(0.01f));
            Assert.That(LossOverOneSecond(10f), Is.EqualTo(3f).Within(0.01f));
        }

        [Test]
        public void Glissade_TraverseUnPassageBasDe10mDepuis7v5([Values(true, false)] bool keepHolding)
        {
            // Plafond à 1.2 m entre x = 3 et x = 13 : 0.9 m passe, 1.8 m non.
            _probe.CeilingY = 1.2f;
            _probe.CeilingMinX = 3f;
            _probe.CeilingMaxX = 13f;
            RunAt(7.5f);
            Step(SlideIn());

            float minSpeedUnder = float.MaxValue;
            for (int i = 0; i < 600 && _probe.Position.x <= 14f; i++)
            {
                bool under = _probe.Position.x >= 3f && _probe.Position.x <= 13f;
                Step(SlideIn(slide: keepHolding || !under));   // variante : relâcher dès l'entrée
                if (under)
                {
                    Assert.That(_state.Mode, Is.EqualTo(MovementMode.Slide), $"toujours en glissade à x = {_probe.Position.x:0.0}");
                    minSpeedUnder = Mathf.Min(minSpeedUnder, _state.HorizontalSpeed);
                }
            }

            Assert.That(_probe.Position.x, Is.GreaterThan(14f), "passage traversé");
            Assert.That(minSpeedUnder, Is.GreaterThanOrEqualTo(_cfg.slideMinSpeedUnderObstacle - 1e-3f));
        }

        [Test]
        public void Glissade_BonusPlafonneParLePlafondCommun([Values(17f, 19f, 25f)] float before)
        {
            RunAt(before);
            Step(SlideIn());
            float expected = MovementMath.CappedGain(before, _cfg.slideBoost, _cfg.movementGainCap) - _cfg.slideDeceleration * Dt;
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(expected).Within(1e-3f), $"{before} m/s : +2 jusqu'à 20, rien au-delà");
        }

        [Test]
        public void Glissade_EnDescenteAccelere()
        {
            _probe.GroundNormal = new Vector3(0.5f, Mathf.Sqrt(3f) / 2f, 0f); // pente de 30° qui descend vers +x
            RunAt(7.5f);
            Step(SlideIn());
            for (int i = 0; i < 60; i++)
                Step(SlideIn());
            Assert.That(_state.HorizontalSpeed, Is.GreaterThan(11f), "≈ 12.5 m/s attendus (9.4 au départ)");
        }

        [Test]
        public void Glissade_EnMonteeFreine()
        {
            _probe.GroundNormal = new Vector3(-0.5f, Mathf.Sqrt(3f) / 2f, 0f); // pente de 30° qui monte vers +x
            RunAt(7.5f);
            Step(SlideIn());
            for (int i = 0; i < 30; i++)
                Step(SlideIn());
            Assert.That(_state.HorizontalSpeed, Is.LessThan(6f));
        }

        [Test]
        public void Glissade_RelacherRelevesLeJoueur()
        {
            RunAt(7.5f);
            Step(SlideIn());
            Step(SlideIn(slide: false));
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Ground));
            Assert.That(_state.Crouched, Is.False);
            Assert.That(_probe.Height, Is.EqualTo(_cfg.capsuleHeight));
        }

        [Test]
        public void Glissade_SousUnPlafondBasOnResteBasEtOnNeSautePas()
        {
            RunAt(7.5f);
            Step(SlideIn());
            _probe.CeilingY = 1.2f;                    // 0.9 m passe, 1.8 m non
            Step(SlideIn(slide: false));
            Assert.That(_state.Crouched, Is.True, "pas la place de se relever");
            Assert.That(_probe.Height, Is.EqualTo(_cfg.crouchHeight));

            int before = _state.LastJumpTick;
            Step(SlideIn(slide: false, jumpHeld: true, press: _state.Tick + 1));
            Assert.That(_state.LastJumpTick, Is.EqualTo(before), "pas de saut sous le plafond");

            _probe.CeilingY = float.PositiveInfinity;  // sorti du passage
            Step(SlideIn(slide: false));
            Assert.That(_state.Crouched, Is.False);
            Assert.That(_probe.Height, Is.EqualTo(_cfg.capsuleHeight));
        }

        [Test]
        public void Glissade_SauterGardeLaVitesseEtRelève()
        {
            RunAt(7.5f);
            Step(SlideIn());
            float speed = _state.HorizontalSpeed;
            Step(SlideIn(jumpHeld: true, press: _state.Tick + 1));
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Air));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(speed).Within(1e-4f), "aucune friction au saut");
            Assert.That(_state.Crouched, Is.False);
            Assert.That(_probe.Height, Is.EqualTo(_cfg.capsuleHeight));
        }

        [Test]
        public void Glissade_AtterrirEnMaintenantLaToucheGlisse()
        {
            _probe.Position = new Vector3(0f, 1f, 0f);
            RunAt(8f);
            Step(SlideIn());
            Assert.That(_state.Grounded, Is.False, "en l'air");
            for (int i = 0; i < 120 && !_state.Grounded; i++)
                Step(SlideIn());
            Assert.That(_state.Grounded, Is.True, "atterri");
            Assert.That(_state.Mode, Is.EqualTo(MovementMode.Slide));
        }

        [Test]
        public void Glissade_SautEtGlissadeMaintenusALAtterrissage_LeSautGagne()
        {
            int press = _state.Tick + 1;
            Step(SlideIn(slide: false, jumpHeld: true, press: press)); // premier saut
            _state.Velocity = new Vector3(8f, _state.Velocity.y, 0f);  // élan, sans toucher au saut
            int jumps = 0;
            int last = _state.LastJumpTick;
            for (int i = 0; i < 60; i++)
            {
                Step(SlideIn(slide: true, jumpHeld: true, press: press));
                if (_state.LastJumpTick != last)
                {
                    jumps++;
                    last = _state.LastJumpTick;
                }

                Assert.That(_state.Mode, Is.Not.EqualTo(MovementMode.Slide));
            }

            Assert.That(jumps, Is.EqualTo(1), "rebond au premier atterrissage, pas de glissade");
        }
    }
}
