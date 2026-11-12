using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Visée factice de la télékinésie : un seul objet, touché s'il est à portée.</summary>
    internal sealed class FakeGrabProbe : IGrabProbe
    {
        public bool HasTarget;
        public int Id = 42;
        public float Mass = 10f;
        public float Distance = 5f;

        public bool FindGrabbable(Vector3 origin, Vector3 direction, float range, out GrabCandidate candidate)
        {
            candidate = default;
            if (!HasTarget || Distance > range)
                return false;
            candidate = new GrabCandidate { Id = Id, Mass = Mass, Distance = Distance, Point = origin + direction * Distance };
            return true;
        }
    }

    public class TelekinesisTests
    {
        private const float Dt = 1f / 60f;
        private MovementSettings _move;
        private MagnetismOrbSettings _magnetCfg;
        private TelekinesisOrbSettings _cfg;
        private StaminaSettings _stamina;
        private FakeGroundProbe _ground;
        private FakeAimProbe _aim;
        private FakeGrabProbe _grab;
        private MovementState _s;
        private MagnetismState _m;
        private TelekinesisState _t;
        private OrbSelectionState _orbs;
        private TelekinesisCommand _last;

        [SetUp]
        public void SetUp()
        {
            _move = ScriptableObject.CreateInstance<MovementSettings>();
            _magnetCfg = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            _cfg = ScriptableObject.CreateInstance<TelekinesisOrbSettings>();
            _stamina = ScriptableObject.CreateInstance<StaminaSettings>();
            _ground = new FakeGroundProbe { Position = Vector3.zero };
            _aim = new FakeAimProbe();
            _grab = new FakeGrabProbe();
            _s = MovementState.Create(Vector3.zero);
            _m = MagnetismState.Create();
            _t = TelekinesisState.Create();
            _orbs = OrbSelectionState.Create();
            Step(MovementInput.Neutral);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_move);
            Object.DestroyImmediate(_magnetCfg);
            Object.DestroyImmediate(_cfg);
            Object.DestroyImmediate(_stamina);
        }

        /// <summary>Comme PlayerMotor : échange, magnétisme, télékinésie (seule l'orbe active reçoit les boutons), mouvement.</summary>
        private void Step(MovementInput input)
        {
            int tick = _s.Tick + 1;
            bool swapped = OrbSelection.Step(ref _orbs, input, tick);
            if (swapped && _orbs.Active != ActiveOrb.Magnetisme)
                MagnetismOrb.StopTraction(ref _m, ref _s, tick, TractionEnd.Echange);
            MovementInput magnetInput = _orbs.Active == ActiveOrb.Magnetisme ? input : OrbSelection.WithoutOrbButtons(input);
            MagnetismOrb.Step(ref _m, ref _s, magnetInput, _magnetCfg, _move, Dt, _aim);
            MovementInput tkInput = _orbs.Active == ActiveOrb.Telekinesie ? input : OrbSelection.WithoutOrbButtons(input);
            _last = TelekinesisOrb.Step(ref _t, _s, tkInput, _cfg, _stamina, _move, Dt, _grab,
                dropHeld: swapped && _orbs.Active != ActiveOrb.Telekinesie);
            MovementMotor.Step(ref _s, input, _move, Dt, _ground);
        }

        private MovementInput Select(byte orb)
        {
            MovementInput i = MovementInput.Neutral;
            i.OrbSelectPressedTick = _s.Tick + 1;
            i.OrbSelect = orb;
            return i;
        }

        private MovementInput Swap()
        {
            MovementInput i = MovementInput.Neutral;
            i.OrbSwapPressedTick = _s.Tick + 1;
            return i;
        }

        private MovementInput Primary(int pressTick = 0)
        {
            MovementInput i = MovementInput.Neutral;
            i.AttractPressedTick = pressTick != 0 ? pressTick : _s.Tick + 1;
            i.AttractHeld = true;
            return i;
        }

        private MovementInput Secondary(int pressTick = 0)
        {
            MovementInput i = MovementInput.Neutral;
            i.RepelPressedTick = pressTick != 0 ? pressTick : _s.Tick + 1;
            return i;
        }

        private void UseTelekinesis()
        {
            Step(Select(2));
            Assume.That(_orbs.Active, Is.EqualTo(ActiveOrb.Telekinesie));
        }

        private void GrabTarget(float mass = 10f)
        {
            _grab.HasTarget = true;
            _grab.Mass = mass;
            Step(Primary());
            Assume.That(_t.Holding, Is.True);
        }

        // ---------- Échange ----------

        [Test]
        public void Echange_BasculeEtChoixDirect()
        {
            Assert.That(_orbs.Active, Is.EqualTo(ActiveOrb.Magnetisme), "magnétisme au départ");
            Step(Swap());
            Assert.That(_orbs.Active, Is.EqualTo(ActiveOrb.Telekinesie));
            Step(Swap());
            Assert.That(_orbs.Active, Is.EqualTo(ActiveOrb.Magnetisme));
            Step(Select(2));
            Assert.That(_orbs.Active, Is.EqualTo(ActiveOrb.Telekinesie));
            int swapTick = _orbs.LastSwapTick;
            Step(Select(2));
            Assert.That(_orbs.LastSwapTick, Is.EqualTo(swapTick), "choisir l'orbe déjà active ne change rien");
            Step(Select(1));
            Assert.That(_orbs.Active, Is.EqualTo(ActiveOrb.Magnetisme));
        }

        [Test]
        public void Echange_ArreteLaTraction_SansPerte()
        {
            _aim.HasTarget = true;
            _aim.Target = MagnetismOrb.EyePosition(_s, _move) + new Vector3(0f, 25f, 25f);
            MovementInput attract = Primary();
            attract.Pitch = -45f;
            attract.Yaw = 0f;
            Step(attract);
            for (int i = 0; i < 10; i++)
            {
                MovementInput hold = MovementInput.Neutral;
                hold.AttractPressedTick = attract.AttractPressedTick;
                hold.AttractHeld = true;
                Step(hold);
            }

            Assume.That(_s.Mode, Is.EqualTo(MovementMode.Traction));
            float speed = _s.Velocity.magnitude;
            MovementInput swap = Swap();
            swap.AttractPressedTick = attract.AttractPressedTick;
            swap.AttractHeld = true;
            Step(swap);
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.Echange));
            Assert.That(_s.Mode, Is.Not.EqualTo(MovementMode.Traction));
            Assert.That(_s.Velocity.magnitude, Is.GreaterThan(speed - _move.gravity * Dt - 1e-3f), "aucune perte d'élan");
        }

        [Test]
        public void TelekinesieActive_LeMagnetismeNeRecoitPasLesBoutons()
        {
            UseTelekinesis();
            _aim.HasTarget = true;
            _aim.Target = MagnetismOrb.EyePosition(_s, _move) + new Vector3(0f, 0f, 10f);
            Step(Primary());
            Assert.That(_m.Tracting, Is.False);
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.RienAttrapable), "le clic va à la télékinésie");
        }

        [Test]
        public void MagnetismeActif_LaTelekinesieNeRecoitPasLesBoutons()
        {
            _grab.HasTarget = true;
            Step(Primary());
            Assert.That(_t.Holding, Is.False);
        }

        // ---------- Attraper, tenir, poser ----------

        [Test]
        public void Attraper_Coute10_ObjetTenuSansMaintenir()
        {
            UseTelekinesis();
            GrabTarget();
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Attraper));
            Assert.That(_last.TargetId, Is.EqualTo(_grab.Id));
            Assert.That(_t.Stamina, Is.EqualTo(_stamina.max - _cfg.grabCost).Within(1e-3f));
            for (int i = 0; i < 30; i++)
                Step(MovementInput.Neutral);                         // bouton relâché
            Assert.That(_t.Holding, Is.True, "tenu sans maintenir le bouton");
            Assert.That(_t.Status, Is.EqualTo(OrbStatus.EnUsage));
        }

        [Test]
        public void Attraper_Refus_TropLourd_Rien_PasDEndurance()
        {
            UseTelekinesis();
            _grab.HasTarget = true;
            _grab.Mass = 60f;
            Step(Primary());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.TropLourd));
            Assert.That(_t.AimTooHeavy, Is.True);

            _grab.HasTarget = false;
            Step(Primary());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.RienAttrapable));

            _grab.HasTarget = true;
            _grab.Mass = 10f;
            _t.Stamina = 5f;
            _t.LastSpendTick = _s.Tick;                             // pas de régénération ce tick
            Step(Primary());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.PasDEndurance));
            Assert.That(_t.Holding, Is.False);
        }

        [Test]
        public void Attraper_HorsDePortee_Refuse()
        {
            UseTelekinesis();
            _grab.HasTarget = true;
            _grab.Distance = _cfg.range + 1f;
            Step(Primary());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.RienAttrapable));
        }

        [Test]
        public void Poser_NouveauClicGauche()
        {
            UseTelekinesis();
            GrabTarget();
            Step(MovementInput.Neutral);
            float stamina = _t.Stamina;
            Step(Primary());
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Poser));
            Assert.That(_t.Holding, Is.False);
            Assert.That(_t.Stamina, Is.EqualTo(stamina).Within(1e-3f), "poser ne coûte rien");
        }

        // ---------- Lancer ----------

        [Test]
        public void Lancer_Coute15_EtRelacheLObjet()
        {
            UseTelekinesis();
            GrabTarget();
            Step(MovementInput.Neutral);
            float stamina = _t.Stamina;
            MovementInput throwInput = Secondary();
            throwInput.Yaw = 90f;
            Step(throwInput);
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Lancer));
            Assert.That(_last.TargetId, Is.EqualTo(_grab.Id));
            Assert.That(Vector3.Angle(_last.AimDirection, Vector3.right), Is.LessThan(0.1f), "direction de la visée");
            Assert.That(_t.Holding, Is.False);
            Assert.That(_t.LastThrownId, Is.EqualTo(_grab.Id));
            Assert.That(_t.Stamina, Is.EqualTo(stamina - _cfg.throwCost).Within(1e-3f));
        }

        [Test]
        public void Lancer_SansEndurance_GardeLObjet()
        {
            UseTelekinesis();
            GrabTarget();
            _t.Stamina = 10f;
            _t.LastSpendTick = _s.Tick;
            Step(Secondary());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.PasDEndurance));
            Assert.That(_t.Holding, Is.True);
        }

        [Test]
        public void Lancer_RienEnMain_Refuse()
        {
            UseTelekinesis();
            Step(Secondary());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.RienEnMain));
        }

        [Test]
        public void Lancer_VitesseDuJoueurAjoutee()
        {
            var player = new Vector3(15f, 0f, 0f);
            Vector3 v = TelekinesisOrb.ThrowVelocity(new Vector3(0f, 0f, 2f), player, _cfg);
            Assert.That(Vector3.Distance(v, new Vector3(15f, 0f, _cfg.throwSpeed)), Is.LessThan(1e-3f), "30 m/s visés + vitesse du joueur");
            _cfg.inheritPlayerVelocity = false;
            v = TelekinesisOrb.ThrowVelocity(new Vector3(0f, 0f, 2f), player, _cfg);
            Assert.That(Vector3.Distance(v, new Vector3(0f, 0f, _cfg.throwSpeed)), Is.LessThan(1e-3f));
        }

        [Test]
        public void Recharge_ApresUnLancer()
        {
            int cooldown = MovementSettings.SecondsToTicks(_cfg.cooldown, Dt);   // 18
            int buffer = MovementSettings.SecondsToTicks(_cfg.pressBuffer, Dt);  // 6
            UseTelekinesis();
            GrabTarget();
            int thrown = _s.Tick + 1;
            Step(Secondary(thrown));
            Assume.That(_t.LastThrowTick, Is.EqualTo(thrown));
            Assert.That(_t.Status, Is.EqualTo(OrbStatus.Recharge));

            int early = thrown + 3;
            while (_s.Tick < early - 1)
                Step(MovementInput.Neutral);
            while (_s.Tick < early + buffer)
                Step(Primary(early));
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.Recharge));
            Assert.That(_t.Holding, Is.False);

            int late = thrown + cooldown - 4;
            while (_s.Tick < late - 1)
                Step(MovementInput.Neutral);
            while (_s.Tick < thrown + cooldown)
                Step(Primary(late));
            Assert.That(_t.Holding, Is.True, "attrapé dès la fin de la recharge");
            Assert.That(_t.GrabTick, Is.EqualTo(thrown + cooldown));
        }

        // ---------- Endurance ----------

        [Test]
        public void Endurance_RegenereApresLeDelai()
        {
            UseTelekinesis();
            GrabTarget();
            _t.Stamina = 50f;                                       // comme après plusieurs usages
            _t.LastSpendTick = _s.Tick;
            int spent = _t.LastSpendTick;
            float after = _t.Stamina;
            int delay = MovementSettings.SecondsToTicks(_stamina.regenDelay, Dt);   // 48
            while (_s.Tick < spent + delay - 1)
                Step(MovementInput.Neutral);
            Assert.That(_t.Stamina, Is.EqualTo(after).Within(1e-3f), "rien pendant 0.8 s");
            for (int i = 0; i < 61; i++)
                Step(MovementInput.Neutral);
            Assert.That(_t.Stamina, Is.EqualTo(Mathf.Min(_stamina.max, after + _stamina.regenPerSecond)).Within(0.5f), "+25 en 1 s");
            for (int i = 0; i < 300; i++)
                Step(MovementInput.Neutral);
            Assert.That(_t.Stamina, Is.EqualTo(_stamina.max).Within(1e-3f), "plafonnée à 100");
        }

        // ---------- Échange et perte ----------

        [Test]
        public void Echange_LacheLObjetTenu()
        {
            UseTelekinesis();
            GrabTarget();
            Step(Swap());
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Lacher));
            Assert.That(_last.TargetId, Is.EqualTo(_grab.Id));
            Assert.That(_t.Holding, Is.False);
            Assert.That(_orbs.Active, Is.EqualTo(ActiveOrb.Magnetisme));
        }

        [Test]
        public void ObjetPerdu_PlusRienEnMain()
        {
            UseTelekinesis();
            GrabTarget();
            TelekinesisOrb.NotifyLost(ref _t, _s.Tick);
            Assert.That(_t.Holding, Is.False);
            Assert.That(_t.Status, Is.EqualTo(OrbStatus.Prete));
        }

        [Test]
        public void Viseur_AttraperEtLancerPossibles()
        {
            UseTelekinesis();
            _grab.HasTarget = true;
            Step(MovementInput.Neutral);
            Assert.That(TelekinesisOrb.CanGrab(_t, _cfg, _s.Tick, Dt), Is.True);
            Assert.That(TelekinesisOrb.CanThrow(_t, _cfg), Is.False);
            _grab.Mass = 60f;
            Step(MovementInput.Neutral);
            Assert.That(TelekinesisOrb.CanGrab(_t, _cfg, _s.Tick, Dt), Is.False, "trop lourd");
            _grab.Mass = 10f;
            GrabTarget();
            Assert.That(TelekinesisOrb.CanGrab(_t, _cfg, _s.Tick, Dt), Is.False, "déjà un objet en main");
            Assert.That(TelekinesisOrb.CanThrow(_t, _cfg), Is.True);
        }
    }
}
