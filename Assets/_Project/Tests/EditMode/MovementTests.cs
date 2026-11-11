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

        private bool OverGround(Vector3 p) => p.x <= EdgeX;

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

            var result = new MoveResult { HitBelow = below, ActualDelta = p - Position };
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
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(20f + _cfg.slideBoost - _cfg.slideDeceleration * Dt).Within(1e-3f));
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
            Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(20f + _cfg.slideBoost - 0.1f));
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
