using ItHunts.Movement;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Playtest du 2026-11-21 (D-084) : la vitesse de sortie du dash est gardée comme l'élan au sol et le bunny hop, tant qu'on tient
    /// « avancer » dans le cône, au sol comme en l'air, sous le plafond commun de 20 m/s ; elle ne redescend que selon les règles de
    /// perte d'élan. Réglages du jeu (Movement_Default).
    /// </summary>
    public class DashMomentumTests
    {
        private const float Dt = 1f / 60f;
        private MovementSettings _cfg;
        private FakeGroundProbe _probe;
        private MovementState _state;

        [SetUp]
        public void SetUp()
        {
            _cfg = Object.Instantiate(AssetDatabase.LoadAssetAtPath<MovementSettings>("Assets/_Project/Settings/Movement_Default.asset"));
            _probe = new FakeGroundProbe { Position = Vector3.zero };
            _state = MovementState.Create(Vector3.zero);
            Step(MovementInput.Neutral);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        private void Step(MovementInput input) => MovementMotor.Step(ref _state, input, _cfg, Dt, _probe);

        private MovementInput Fwd(bool dash = false, bool jump = false, Vector2? move = null) => new MovementInput
        {
            Move = move ?? Vector2.up, Yaw = 90f, JumpHeld = jump, JumpPressedTick = jump ? _state.Tick + 1 : MovementState.NeverTick,
            DashPressedTick = dash ? _state.Tick + 1 : MovementState.NeverTick,
        };

        private static MovementInput Idle()
        {
            MovementInput i = MovementInput.Neutral;
            i.Yaw = 90f;
            return i;
        }

        private float DashAverage => _cfg.dashDistance / (MovementSettings.SecondsToTicks(_cfg.dashDuration, Dt) * Dt);
        private float CarriedExit => Mathf.Min(_cfg.movementGainCap, _cfg.dashExitCarry * DashAverage);

        private void RunForward(float seconds)
        {
            for (int i = 0; i < Mathf.RoundToInt(seconds / Dt); i++)
                Step(Fwd());
        }

        private float DashToTheEnd()
        {
            Step(Fwd(dash: true));
            Assert.That(_state.LastDashTick, Is.EqualTo(_state.Tick), "dash parti");
            while (_state.DashTicksLeft > 0)
                Step(Fwd());
            return _state.HorizontalSpeed;
        }

        private void InAir(float speed)
        {
            _probe.Position = new Vector3(0f, 200f, 0f);
            _state.Velocity = new Vector3(speed, 0f, 0f);
            _state.LastGroundedTick = MovementState.NeverTick;
        }

        [Test]
        public void Reglages_SortieGardeeAuDessusDeLAncienne12()
        {
            Assert.That(_cfg.dashExitCarry, Is.GreaterThan(0f));
            Assert.That(CarriedExit, Is.GreaterThan(_cfg.dashExitSpeed), "la sortie gardée dépasse l'ancienne sortie de 12 m/s");
            Assert.That(_cfg.movementGainCap, Is.EqualTo(20f), "plafond commun");
        }

        [Test]
        public void AuSol_VitesseDeSortieGardeeEnAvancantDansLeCone()
        {
            RunForward(1f);
            float exit = DashToTheEnd();
            Assert.That(exit, Is.EqualTo(CarriedExit).Within(1e-3f), "sortie depuis la course");
            for (int i = 0; i < 180; i++)
            {
                // Avancer, avec un peu de côté (dans le cône de 45°) : 3 s sans perte.
                Step(Fwd(move: new Vector2(0.3f, 1f).normalized));
                Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(exit - 1e-3f), $"tick {i} : vitesse gardée");
            }
        }

        [Test]
        public void AuSol_AvancerLache_RedescendSelonLesReglesDePerteDElan()
        {
            RunForward(1f);
            float exit = DashToTheEnd();
            int grace = MovementSettings.SecondsToTicks(_cfg.dashFrictionGrace, Dt) + 2;
            for (int i = 0; i < grace; i++)
                Step(Idle());
            float before = _state.HorizontalSpeed;
            Step(Idle());
            Assert.That(before - _state.HorizontalSpeed, Is.EqualTo(_cfg.momentumLossDeceleration * Dt).Within(0.05f),
                "perte d'élan réglée (avancer lâché)");
            Assert.That(_state.HorizontalSpeed, Is.LessThan(exit));
        }

        [Test]
        public void EnLAir_VitesseDeSortieGardee_CommeLeBunnyHop()
        {
            InAir(_cfg.runSpeed);
            float exit = DashToTheEnd();
            Assert.That(exit, Is.EqualTo(CarriedExit).Within(1e-3f), "en l'air : même sortie qu'au sol");
            for (int i = 0; i < 60; i++)
            {
                Step(Fwd());
                Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(exit - 1e-3f), $"tick {i} en l'air");
            }

            // Atterrissage en avançant : l'élan au sol prend le relais.
            _probe.Position = new Vector3(_probe.Position.x, 0f, 0f);
            for (int i = 0; i < 60; i++)
                Step(Fwd());
            Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(exit - 1e-3f), "gardée après l'atterrissage");
        }

        [Test]
        public void Plafond20_AuSolCommeEnLAir_JamaisDePerteAuDessus([Values(false, true)] bool air, [Values(15f, 19.5f, 25f)] float before)
        {
            if (air)
                InAir(before);
            else
            {
                _state.Velocity = new Vector3(before, 0f, 0f);
                Step(Fwd());
            }

            float start = _state.HorizontalSpeed;
            float exit = DashToTheEnd();
            float expected = start >= _cfg.movementGainCap ? start : Mathf.Min(_cfg.movementGainCap, Mathf.Max(CarriedExit, start + _cfg.dashSpeedGain));
            Assert.That(exit, Is.EqualTo(expected).Within(0.02f), $"{(air ? "air" : "sol")}, {start:0.0} m/s avant");
            Assert.That(exit, Is.LessThanOrEqualTo(Mathf.Max(_cfg.movementGainCap, start) + 1e-3f), "aucun gain au-dessus du plafond");
        }

        [Test]
        public void DashJump_GainSurLaSortie_PuisGardeEnLAir()
        {
            RunForward(1f);
            float exit = DashToTheEnd();
            Step(Fwd(jump: true));
            Assert.That(_state.LastDashJumpSuccess, Is.True, "dash-jump réussi");
            float expected = Mathf.Min(_cfg.movementGainCap, exit + _cfg.dashJumpSpeedGain);
            Assert.That(_state.HorizontalSpeed, Is.EqualTo(expected).Within(1e-3f), "+3 m/s sur la sortie, plafonné");
            for (int i = 0; i < 30; i++)
                Step(Fwd(jump: false));
            Assert.That(_state.HorizontalSpeed, Is.GreaterThanOrEqualTo(expected - 1e-3f), "gardée en l'air");
        }

        [Test]
        public void Mesure_TroisDashsDAffilee_EtEcartAvecLeTraqueur()
        {
            var stalker = AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            RunForward(1f);
            float g1 = DashToTheEnd(), g2 = DashToTheEnd(), g3 = DashToTheEnd();
            RunForward(2f);
            float kept = _state.HorizontalSpeed;
            SetUp();
            InAir(_cfg.runSpeed);
            float a1 = DashToTheEnd(), a2 = DashToTheEnd(), a3 = DashToTheEnd();
            Assert.That(g3, Is.LessThanOrEqualTo(_cfg.movementGainCap + 1e-3f));
            Assert.That(a3, Is.LessThanOrEqualTo(_cfg.movementGainCap + 1e-3f));
            Assert.That(kept, Is.EqualTo(g3).Within(1e-3f), "gardée 2 s après le 3e dash");
            Assert.Pass($"sol : {g1:0.0} → {g2:0.0} → {g3:0.0} m/s (gardée {kept:0.0}) ; air : {a1:0.0} → {a2:0.0} → {a3:0.0} ; " +
                        $"traqueur : chasse {stalker.chaseSpeed} (écart {g3 - stalker.chaseSpeed:+0.0}), pointe {stalker.burstSpeed} " +
                        $"(écart {g3 - stalker.burstSpeed:+0.0}), enragé {stalker.enragedSpeed} / pointe enragée {stalker.enragedBurstSpeed} " +
                        $"(écart {g3 - stalker.enragedBurstSpeed:+0.0})");
        }
    }
}
