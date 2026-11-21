using ItHunts.Movement;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Hissage automatique sur un rebord (D-074), simulation à pas fixe, rebord factice.</summary>
    public class MantleTests
    {
        private const float Dt = 1f / 60f;
        private MovementSettings _cfg;
        private FakeGroundProbe _probe;
        private MovementState _state;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<MovementSettings>();
            _probe = new FakeGroundProbe { Position = Vector3.zero, HasLedge = true, LedgeX = 3f, LedgeDepth = 50f };
            _state = MovementState.Create(Vector3.zero);
            Step(Input());
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        private void Step(MovementInput input) => MovementMotor.Step(ref _state, input, _cfg, Dt, _probe);

        private MovementInput Input(bool forward = false, bool jump = false) => new MovementInput
        {
            Move = forward ? Vector2.up : Vector2.zero, Yaw = 90f, JumpHeld = jump,
            JumpPressedTick = jump ? _state.Tick + 1 : MovementState.NeverTick, DashPressedTick = MovementState.NeverTick,
        };

        /// <summary>Course vers le rebord puis saut au pied du mur ; renvoie vrai si un hissage a eu lieu.</summary>
        private bool RunJumpAndHold(int ticks = 150)
        {
            bool mantled = false;
            for (int i = 0; i < 120 && _probe.Position.x < _probe.LedgeX - 1.2f; i++)
                Step(Input(forward: true));
            bool jumped = false;
            for (int i = 0; i < ticks; i++)
            {
                bool press = !jumped && _state.Grounded;
                Step(press ? Input(forward: true, jump: true) : new MovementInput
                {
                    Move = Vector2.up, Yaw = 90f, JumpHeld = true, JumpPressedTick = MovementState.NeverTick,
                    DashPressedTick = MovementState.NeverTick,
                });
                jumped |= press;
                mantled |= _state.Mode == MovementMode.Mantle;
            }

            return mantled;
        }

        [Test]
        public void Hissage_RebordAHauteurDeTete_OnMonteDessus()
        {
            _probe.LedgeTop = 2.4f;
            _cfg.autoBunnyHop = false;
            Assert.That(RunJumpAndHold(), Is.True);
            Assert.That(_probe.Position.y, Is.EqualTo(2.4f).Within(0.03f), "posé sur le dessus");
            Assert.That(_probe.Position.x, Is.GreaterThan(_probe.LedgeX), "sur le rebord, pas contre le mur");
        }

        [Test]
        public void Hissage_DureeReglable_EtElanGardeEnPartie()
        {
            _probe.LedgeTop = 2.4f;
            _cfg.autoBunnyHop = false;
            _cfg.maxJumps = 1;
            int start = -1, end = -1;
            float exit = 0f;
            for (int i = 0; i < 120 && _probe.Position.x < _probe.LedgeX - 1.2f; i++)
                Step(Input(forward: true));
            Step(Input(forward: true, jump: true));
            for (int i = 0; i < 120; i++)
            {
                MovementMode before = _state.Mode;
                Step(new MovementInput
                {
                    Move = Vector2.up, Yaw = 90f, JumpHeld = true, JumpPressedTick = MovementState.NeverTick,
                    DashPressedTick = MovementState.NeverTick,
                });
                if (start < 0 && _state.Mode == MovementMode.Mantle)
                    start = i;
                if (start >= 0 && end < 0 && before == MovementMode.Mantle && _state.Mode != MovementMode.Mantle)
                {
                    end = i;
                    exit = _state.MantleExitSpeed;
                }
            }

            Assert.That(start, Is.GreaterThanOrEqualTo(0), "hissage");
            Assert.That(end - start, Is.EqualTo(Mathf.RoundToInt(_cfg.mantleDuration / Dt)).Within(1), "≈ 0.3 s");
            Assert.That(exit, Is.EqualTo(_cfg.runSpeed * _cfg.mantleKeepSpeed).Within(0.2f), "70 % de l'élan d'avant le mur");
        }

        [Test]
        public void Hissage_HauteurLimite_JusquA06AuDessusDeLaTete()
        {
            // Saut de 1.3 m : pieds au plus à 1.3, tête à 3.1 ; limite = tête + 0.6 = 3.7 m.
            _cfg.autoBunnyHop = false;
            _cfg.maxJumps = 1;
            _probe.LedgeTop = 3.6f;
            Assert.That(RunJumpAndHold(), Is.True, "3.6 m : juste sous la limite");

            SetUp();
            _cfg.autoBunnyHop = false;
            _cfg.maxJumps = 1;
            _probe.LedgeTop = 3.9f;
            Assert.That(RunJumpAndHold(), Is.False, "3.9 m : trop haut");
            Assert.That(_probe.Position.y, Is.EqualTo(0f).Within(0.01f), "retombé au pied du mur");
        }

        [Test]
        public void Hissage_LimiteReglable()
        {
            _cfg.autoBunnyHop = false;
            _cfg.maxJumps = 1;
            _cfg.mantleMaxAboveHead = 0f;
            _probe.LedgeTop = 3.4f;               // tête à 3.1 au sommet : plus haut que la tête
            Assert.That(RunJumpAndHold(), Is.False);
        }

        [Test]
        public void Hissage_RebordBloque_Refuse()
        {
            _cfg.autoBunnyHop = false;
            _probe.LedgeTop = 2.4f;
            _probe.LedgeBlocked = true;
            Assert.That(RunJumpAndHold(), Is.False);
        }

        [Test]
        public void Hissage_RebordTropEtroit_Refuse()
        {
            _cfg.autoBunnyHop = false;
            _probe.LedgeTop = 2.4f;
            _probe.LedgeDepth = 0.3f;
            Assert.That(RunJumpAndHold(), Is.False);
        }

        [Test]
        public void Hissage_SansAvancer_Rien()
        {
            _cfg.autoBunnyHop = false;
            _probe.LedgeTop = 2.4f;
            _probe.Position = new Vector3(_probe.LedgeX - 0.45f, 0f, 0f);
            Step(Input(jump: true));
            bool mantled = false;
            for (int i = 0; i < 90; i++)
            {
                Step(Input());
                mantled |= _state.Mode == MovementMode.Mantle;
            }

            Assert.That(mantled, Is.False, "il faut avancer vers le mur");
        }

        [Test]
        public void Hissage_Chemin_MonteAvantDAvancer()
        {
            var a = new Vector3(0f, 1f, 0f);
            var b = new Vector3(1f, 3f, 0f);
            Vector3 mid = MovementMotor.MantlePath(a, b, 0.6f, 0.6f);
            Assert.That(mid.x, Is.EqualTo(0f).Within(1e-4f), "d'abord la montée, le long du mur");
            Assert.That(mid.y, Is.EqualTo(3.02f).Within(1e-3f));
            Vector3 end = MovementMotor.MantlePath(a, b, 1f, 0.6f);
            Assert.That(end.x, Is.EqualTo(1f).Within(1e-4f));
        }
    }
}
