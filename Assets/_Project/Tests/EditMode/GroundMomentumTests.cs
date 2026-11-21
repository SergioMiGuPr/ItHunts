using ItHunts.Movement;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Élan gardé au sol (D-073) : simulation à pas fixe depuis la structure d'entrée, sol factice.</summary>
    public class GroundMomentumTests
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
            Step(MovementInput.Neutral);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        private void Step(MovementInput input) => MovementMotor.Step(ref _state, input, _cfg, Dt, _probe);

        /// <summary>Avancer, la caméra tournée de yaw (90 = +x, le sens de l'élan de départ).</summary>
        private static MovementInput Move(Vector2 move, float yaw = 90f) => new MovementInput
        {
            Move = move, Yaw = yaw, JumpPressedTick = MovementState.NeverTick, DashPressedTick = MovementState.NeverTick,
        };

        private void Run(int ticks, MovementInput input)
        {
            for (int i = 0; i < ticks; i++)
                Step(input);
        }

        private void Launch(float speed) => _state.Velocity = new Vector3(speed, 0f, 0f);

        [Test]
        public void Course_DeBase_8_5()
        {
            Assert.That(_cfg.runSpeed, Is.EqualTo(8.5f));
            Run(120, Move(Vector2.up));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(8.5f).Within(1e-3f), "on n'accélère pas au-delà de la course");
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Aucun));
        }

        [Test]
        public void Elan_GardeEnLigneDroite()
        {
            Launch(15f);
            Run(180, Move(Vector2.up));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(15f).Within(1e-3f), "3 s en avançant : vitesse gardée");
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Garde));
        }

        [Test]
        public void Elan_DecroissanceReglable()
        {
            _cfg.momentumDecay = 2f;
            Launch(15f);
            Run(60, Move(Vector2.up));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(13f).Within(0.05f), "2 m/s² pendant 1 s");
        }

        [Test]
        public void Elan_PerduProgressivementSiOnLache()
        {
            Launch(15f);
            Run(30, MovementInput.Neutral);
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.AvancerLache));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(15f - 12f * 0.5f).Within(0.05f), "perte progressive (12 m/s²), pas d'arrêt net");
            Run(30, MovementInput.Neutral);
            Assert.That(_state.HorizontalSpeed, Is.LessThanOrEqualTo(_cfg.runSpeed + 1e-3f), "revenu à la course en ≈ 0.55 s");
        }

        [Test]
        public void Elan_PerduEnReculant_PlusVite()
        {
            Launch(15f);
            Run(12, Move(Vector2.down));
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Recule));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(15f - 30f * 0.2f).Within(0.05f), "freinage 30 m/s²");
        }

        [Test]
        public void Elan_DansLeCone_LaTrajectoireSuitLEntree()
        {
            Launch(15f);
            Run(120, Move(Vector2.up, 90f + 30f));    // 30° : dans le cône de 45°
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(15f).Within(1e-3f));
            var h = new Vector3(_state.Velocity.x, 0f, _state.Velocity.z);
            Vector3 wish = Quaternion.Euler(0f, 120f, 0f) * Vector3.forward;
            Assert.That(Vector3.Angle(h, wish), Is.LessThan(1f), "la trajectoire a tourné vers l'entrée");
        }

        [Test]
        public void Elan_HorsDuCone_PerteProgressive()
        {
            Launch(15f);
            Step(Move(Vector2.up, 90f + 52f));        // entre 45 et 60°
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.HorsDuCone));
            Assert.That(_state.HorizontalSpeed, Is.LessThan(15f));
        }

        [Test]
        public void Elan_CasseAuDelaDeLAngle_MemeEnRevenantDansLeCone()
        {
            Launch(15f);
            Step(Move(Vector2.up, 90f + 70f));        // virage de 70° > 60°
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Virage));
            Assert.That(_state.MomentumBroken, Is.True);
            Run(30, Move(Vector2.up, 90f));           // on revient dans l'axe : l'élan reste cassé
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Virage));
            Assert.That(_state.HorizontalSpeed, Is.LessThan(15f - 5f));
            Run(60, Move(Vector2.up, 90f));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.runSpeed).Within(0.1f), "revenu à la course");
            Launch(15f);                              // à la course, l'élan n'est plus cassé : gardé de nouveau
            Run(30, Move(Vector2.up, 90f));
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Garde));
        }

        [Test]
        public void Elan_PlafondDe20AuSol()
        {
            Launch(26f);
            Step(Move(Vector2.up));
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Plafond));
            Assert.That(_state.HorizontalSpeed, Is.LessThan(26f).And.GreaterThan(20f), "redescente progressive");
            Run(60, Move(Vector2.up));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.groundSpeedCap).Within(1e-3f));
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Garde));
        }

        [Test]
        public void Elan_CasseParUnMurPercute()
        {
            _probe.Walls.Add(new FakeGroundProbe.Wall { X = 3f, NormalX = -1f, Id = 1 });
            _probe.Position = new Vector3(2.5f, 0f, 0f);
            _state.Velocity = new Vector3(8f, 0f, 14f);    // vers le mur, à 30° de lui
            float yaw = Mathf.Atan2(8f, 14f) * Mathf.Rad2Deg;
            Run(10, Move(Vector2.up, yaw));
            Assert.That(_state.MomentumBroken, Is.True);
            Assert.That(_state.Momentum, Is.EqualTo(MomentumStatus.Mur));
            Assert.That(_state.HorizontalSpeed, Is.LessThan(14f - 1f), "le long du mur, l'élan se perd");
        }

        [Test]
        public void Elan_Desactivable_FrictionClassique()
        {
            _cfg.keepGroundMomentum = false;
            Launch(15f);
            Run(30, Move(Vector2.up));
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(_cfg.runSpeed).Within(0.05f), "comme avant D-073");
        }

        [Test]
        public void Elan_BunnyHop_Inchange()
        {
            Launch(18f);
            // Saut maintenu (auto-bunny hop) : aucun tick de friction, comme avant.
            for (int i = 0; i < 240; i++)
                Step(new MovementInput
                {
                    Move = Vector2.up, Yaw = 90f, JumpHeld = true, JumpPressedTick = i == 0 ? _state.Tick + 1 : MovementState.NeverTick,
                    DashPressedTick = MovementState.NeverTick,
                });
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(18f).Within(0.05f));
        }
    }
}
