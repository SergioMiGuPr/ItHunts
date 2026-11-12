using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Visée factice : une cible ponctuelle, touchée si le rayon passe à moins de 2° et à portée. BlockAt > 0 : tout
    /// rayon touche un obstacle à cette distance (ligne de vue coupée).
    /// </summary>
    internal sealed class FakeAimProbe : IAimProbe
    {
        public bool HasTarget;
        public Vector3 Target;
        public float BlockAt;

        public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out AimHit hit)
        {
            hit = default;
            if (BlockAt > 0f && BlockAt <= maxDistance)
            {
                hit = new AimHit { Point = origin + direction * BlockAt, Normal = -direction, Distance = BlockAt };
                return true;
            }

            if (!HasTarget)
                return false;
            Vector3 to = Target - origin;
            if (to.magnitude > maxDistance || Vector3.Angle(direction, to) > 2f)
                return false;
            hit = new AimHit { Point = Target, Normal = -to.normalized, Distance = to.magnitude };
            return true;
        }
    }

    public class MagnetismTests
    {
        private const float Dt = 1f / 60f;
        private MovementSettings _move;
        private MagnetismOrbSettings _cfg;
        private FakeGroundProbe _probe;
        private FakeAimProbe _aim;
        private MovementState _s;
        private MagnetismState _m;
        private int _attractTick;

        [SetUp]
        public void SetUp()
        {
            _move = ScriptableObject.CreateInstance<MovementSettings>();
            _cfg = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            _probe = new FakeGroundProbe { Position = Vector3.zero };
            _aim = new FakeAimProbe();
            _s = MovementState.Create(Vector3.zero);
            _m = MagnetismState.Create();
            Step(MovementInput.Neutral); // un tick pour se poser
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_move);
            Object.DestroyImmediate(_cfg);
        }

        /// <summary>Comme PlayerMotor : l'orbe, puis le mouvement, même tick, même entrée.</summary>
        private void Step(MovementInput input)
        {
            MagnetismOrb.Step(ref _m, ref _s, input, _cfg, _move, Dt, _aim);
            MovementMotor.Step(ref _s, input, _move, Dt, _probe);
        }

        private Vector3 Eye => MagnetismOrb.EyePosition(_s, _move);

        /// <summary>Place la cible (relative aux yeux) et vise-la.</summary>
        private MovementInput AimAt(Vector3 offsetFromEye)
        {
            _aim.HasTarget = true;
            _aim.Target = Eye + offsetFromEye;
            Vector3 dir = offsetFromEye.normalized;
            MovementInput input = MovementInput.Neutral;
            input.Yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            input.Pitch = -Mathf.Asin(dir.y) * Mathf.Rad2Deg;
            return input;
        }

        /// <summary>Nouvel appui d'attraction, bouton tenu.</summary>
        private MovementInput PressAttract(Vector3 offsetFromEye, int pressTick = 0)
        {
            MovementInput input = AimAt(offsetFromEye);
            _attractTick = pressTick != 0 ? pressTick : _s.Tick + 1;
            input.AttractPressedTick = _attractTick;
            input.AttractHeld = true;
            return input;
        }

        /// <summary>Bouton d'attraction toujours tenu (même appui).</summary>
        private MovementInput Holding()
        {
            MovementInput input = MovementInput.Neutral;
            input.AttractPressedTick = _attractTick;
            input.AttractHeld = true;
            return input;
        }

        private MovementInput PressRepel(Vector3 offsetFromEye, int pressTick = 0)
        {
            MovementInput input = AimAt(offsetFromEye);
            input.RepelPressedTick = pressTick != 0 ? pressTick : _s.Tick + 1;
            return input;
        }

        private void PutInAir(Vector3 velocity, float height = 100f)
        {
            _probe.Position = new Vector3(0f, height, 0f);
            _s.Position = _probe.Position;
            _s.Velocity = velocity;
            _s.Grounded = false;
            _s.Mode = MovementMode.Air;
            _s.LastGroundedTick = MovementState.NeverTick;
        }

        private static float Horizontal(Vector3 v) => new Vector2(v.x, v.z).magnitude;

        // ---------- Attraction : traction ----------

        [Test]
        public void Traction_AccelereJusquALaVitesseDeTraction()
        {
            Step(PressAttract(new Vector3(0f, 25f, 25f)));
            Assert.That(_s.Mode, Is.EqualTo(MovementMode.Traction));
            Assert.That(_s.Velocity.magnitude, Is.EqualTo(_cfg.tractionAcceleration * Dt).Within(1e-3f));
            for (int i = 0; i < 22; i++)
                Step(Holding());
            Assert.That(_s.Velocity.magnitude, Is.EqualTo(_cfg.tractionSpeed).Within(1e-3f), "30 m/s en 23 ticks (80 m/s²)");
            Step(Holding());
            Assert.That(_s.Velocity.magnitude, Is.EqualTo(_cfg.tractionSpeed).Within(1e-3f), "pas au-delà de la vitesse de traction");
        }

        [Test]
        public void Traction_LigneDroiteSansGravite()
        {
            Vector3 start = Eye;
            Vector3 offset = new Vector3(5f, 30f, 20f);
            Step(PressAttract(offset));
            for (int i = 0; i < 40; i++)
                Step(Holding());
            Assume.That(_s.Mode, Is.EqualTo(MovementMode.Traction));
            Vector3 dir = offset.normalized;
            Vector3 rel = Eye - start;
            float offLine = (rel - dir * Vector3.Dot(rel, dir)).magnitude;
            Assert.That(offLine, Is.LessThan(1e-3f), "les yeux restent sur la droite départ → point");
            Assert.That(Vector3.Dot(rel, dir), Is.GreaterThan(10f));
        }

        [Test]
        public void Traction_PlusVite_VitesseGardeeEtRedirigee_SansPlafond()
        {
            PutInAir(new Vector3(40f, 0f, 0f));
            var offset = new Vector3(0f, 0f, 30f);
            Step(PressAttract(offset));
            Assert.That(_s.Velocity.magnitude, Is.EqualTo(40f).Within(1e-3f), "40 m/s gardés (pas de plafond des gains)");
            Assert.That(Vector3.Angle(_s.Velocity, offset), Is.LessThan(0.5f), "redirigée vers le point");
            for (int i = 0; i < 20; i++)
                Step(Holding());
            Assert.That(_s.Velocity.magnitude, Is.EqualTo(40f).Within(1e-3f));
        }

        [Test]
        public void Traction_VersLeHaut_LaChuteNestPasComptee()
        {
            PutInAir(new Vector3(10f, -20f, 0f));
            Step(PressAttract(new Vector3(0f, 30f, 0f)));
            Assert.That(_s.Velocity.magnitude, Is.EqualTo(10f + _cfg.tractionAcceleration * Dt).Within(1e-3f));
            Assert.That(_s.Velocity.y, Is.GreaterThan(0f));
        }

        [Test]
        public void Traction_Relacher_GardeVitesseEtDirection()
        {
            Step(PressAttract(new Vector3(0f, 25f, 25f)));
            for (int i = 0; i < 30; i++)
                Step(Holding());
            Vector3 before = _s.Velocity;
            Step(MovementInput.Neutral);
            Assert.That(_s.Mode, Is.Not.EqualTo(MovementMode.Traction));
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.Relache));
            Assert.That(Horizontal(_s.Velocity), Is.EqualTo(Horizontal(before)).Within(1e-3f), "vitesse horizontale intacte");
            Assert.That(Mathf.Abs(_s.Velocity.y - before.y), Is.LessThanOrEqualTo(_move.gravity * Dt + 1e-3f), "seule la gravité reprend");
        }

        [Test]
        public void Traction_SArreteAuPoint()
        {
            Step(PressAttract(new Vector3(0f, 0f, 6f)));
            for (int i = 0; i < 60 && _s.Mode == MovementMode.Traction; i++)
                Step(Holding());
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.Arrivee));
            Assert.That(Vector3.Distance(Eye, _aim.Target), Is.LessThan(_cfg.tractionArrivalDistance + 0.6f));
        }

        [Test]
        public void Traction_DureeMax()
        {
            _cfg.tractionMaxDuration = 0.5f;
            PutInAir(Vector3.zero);
            Step(PressAttract(new Vector3(0f, 55f, 0f)));
            for (int i = 0; i < 40 && _s.Mode == MovementMode.Traction; i++)
                Step(Holding());
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.DureeMax));
            Assert.That(_m.LastTractionTicks, Is.EqualTo(30));
        }

        [Test]
        public void Traction_LigneDeVueCoupee()
        {
            Step(PressAttract(new Vector3(0f, 30f, 20f)));
            for (int i = 0; i < 5; i++)
                Step(Holding());
            _aim.BlockAt = 2f;
            Step(Holding());
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.LigneCoupee));
            Assert.That(_s.Mode, Is.Not.EqualTo(MovementMode.Traction));
        }

        [Test]
        public void Traction_BloqueeParUnObstacle()
        {
            _probe.Walls.Add(new FakeGroundProbe.Wall { X = 3f, NormalX = -1f, Id = 1 });
            Step(PressAttract(new Vector3(10f, 0f, 0f)));
            for (int i = 0; i < 60 && _s.Mode == MovementMode.Traction; i++)
                Step(Holding());
            Assert.That(_s.TractionEndReason, Is.EqualTo(TractionEnd.Bloquee), "arrêtée par le moteur");
            Step(Holding());   // l'orbe l'enregistre au tick suivant (et démarre sa recharge)
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.Bloquee));
            Assert.That(_m.Tracting, Is.False);
        }

        [Test]
        public void Traction_UnSautLArrete_EtLeSautPart()
        {
            PutInAir(Vector3.zero);
            Step(PressAttract(new Vector3(0f, 20f, 20f)));
            for (int i = 0; i < 10; i++)
                Step(Holding());
            Vector3 before = _s.Velocity;
            MovementInput jump = Holding();
            jump.JumpHeld = true;
            jump.JumpPressedTick = _s.Tick + 1;
            Step(jump);
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.Saut));
            Assert.That(_s.LastJumpTick, Is.EqualTo(_s.Tick), "le saut en l'air part le même tick");
            Assert.That(Horizontal(_s.Velocity), Is.EqualTo(Horizontal(before)).Within(1e-3f));
        }

        [Test]
        public void Traction_UnDashLArrete()
        {
            Step(PressAttract(new Vector3(0f, 20f, 20f)));
            Step(Holding());
            MovementInput dash = Holding();
            dash.DashPressedTick = _s.Tick + 1;
            Step(dash);
            Step(Holding());
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.Dash));
            Assert.That(_s.Mode, Is.Not.EqualTo(MovementMode.Traction));
        }

        /// <summary>Une traction courte : appui, un tick tenu, relâchement.</summary>
        private void QuickTraction()
        {
            Step(PressAttract(new Vector3(0f, 25f, 25f)));
            Step(Holding());
            Step(MovementInput.Neutral);
        }

        [Test]
        public void Attraction_UneChargeParTraction_SansDelaiEntreDeux()
        {
            Assert.That(_m.AttractCharges, Is.EqualTo(_cfg.attractCharges).Within(1e-3f), "3 charges au départ");
            QuickTraction();
            Assert.That(_m.AttractCharges, Is.EqualTo(_cfg.attractCharges - 1f).Within(0.05f));
            Step(PressAttract(new Vector3(0f, 25f, 25f)));            // tout de suite après le relâchement
            Assert.That(_s.Mode, Is.EqualTo(MovementMode.Traction), "pas de délai : seules les charges limitent");
            Assert.That(_m.AttractCharges, Is.EqualTo(_cfg.attractCharges - 2f).Within(0.05f));
        }

        [Test]
        public void Attraction_SansCharge_Refusee()
        {
            for (int i = 0; i < _cfg.attractCharges; i++)
                QuickTraction();
            Assume.That(_m.AttractCharges, Is.LessThan(1f));
            int buffer = MovementSettings.SecondsToTicks(_cfg.pressBuffer, Dt);
            int press = _s.Tick + 1;
            MovementInput input = PressAttract(new Vector3(0f, 25f, 25f), press);
            while (_s.Tick < press + buffer)
                Step(input);
            Assert.That(_s.Mode, Is.Not.EqualTo(MovementMode.Traction));
            Assert.That(_m.LastRefusal, Is.EqualTo(MagnetRefusal.PasDeCharge));
            Assert.That(_m.AttractStatus, Is.EqualTo(OrbStatus.Recharge));
        }

        [Test]
        public void Attraction_UneChargeToutesLes4s_UneParUne()
        {
            Assert.That(_cfg.attractChargeRechargeTime, Is.EqualTo(4f), "1 charge toutes les 4 s (D-024)");
            _m.AttractCharges = 0f;
            int recharge = MovementSettings.SecondsToTicks(_cfg.attractChargeRechargeTime, Dt);   // 240
            for (int i = 0; i < recharge - 2; i++)
                Step(MovementInput.Neutral);
            Assert.That(_m.AttractCharges, Is.LessThan(1f), "pas encore de charge juste avant 4 s");
            Step(MovementInput.Neutral);
            Step(MovementInput.Neutral);
            Assert.That(_m.AttractCharges, Is.EqualTo(1f).Within(0.02f), "1 charge après 4 s");
            for (int i = 0; i < recharge; i++)
                Step(MovementInput.Neutral);
            Assert.That(_m.AttractCharges, Is.EqualTo(2f).Within(0.02f), "2 après 8 s");
            for (int i = 0; i < 3 * recharge; i++)
                Step(MovementInput.Neutral);
            Assert.That(_m.AttractCharges, Is.EqualTo(_cfg.attractCharges).Within(1e-3f), "plafonnées à 3");
        }

        [Test]
        public void Attraction_ChargesSepareesDesChargesDeMouvement()
        {
            _s.Charges = 0f;
            Step(PressAttract(new Vector3(0f, 25f, 25f)));
            Assert.That(_s.Mode, Is.EqualTo(MovementMode.Traction), "la traction ne dépend pas des charges de mouvement");
        }

        [Test]
        public void Traction_RienAPortee()
        {
            Step(PressAttract(new Vector3(0f, 0f, _cfg.attractRange + 5f)));
            Assert.That(_m.LastRefusal, Is.EqualTo(MagnetRefusal.RienAPortee));
            Assert.That(_s.Mode, Is.Not.EqualTo(MovementMode.Traction));
        }

        [Test]
        public void AucuneChargeDeMouvementDepensee()
        {
            float charges = _s.Charges;
            Step(PressAttract(new Vector3(0f, 25f, 25f)));
            for (int i = 0; i < 10; i++)
                Step(Holding());
            Step(MovementInput.Neutral);
            Step(PressRepel(new Vector3(0f, -5f, 0f)));
            Assert.That(_s.Charges, Is.EqualTo(charges).Within(1e-3f));
        }

        // ---------- Répulsion ----------

        [Test]
        public void Repulsion_SurLeSol_RocketJump()
        {
            Step(PressRepel(new Vector3(0f, -_move.eyeHeight, 0f)));
            Assert.That(Vector3.Distance(_m.LastRepelVelocityAfter, Vector3.up * _cfg.repelImpulse), Is.LessThan(1e-3f));
            for (int i = 0; i < 10; i++)
                Step(MovementInput.Neutral);
            Assert.That(_s.Grounded, Is.False);
            Assert.That(_s.Position.y, Is.GreaterThan(1.5f));
        }

        [TestCase(25f, 0f, -5f, 0f, 25f, TestName = "Repulsion_SolSous25ms_VitesseHorizontaleIntacte")]
        [TestCase(15f, 10f, 0f, 0f, 15f, TestName = "Repulsion_MurDevant_PasDePerte")]
        [TestCase(10f, -10f, 0f, 0f, 20f, TestName = "Repulsion_MurDerriere_GainPlafonneA20")]
        [TestCase(25f, -10f, 0f, 0f, 25f, TestName = "Repulsion_MurDerriere_Au-dela_DuPlafond_Conservee")]
        public void Repulsion_JamaisDePerteHorizontale(float speed, float ox, float oy, float oz, float expected)
        {
            PutInAir(new Vector3(speed, 0f, 0f));
            Step(PressRepel(new Vector3(ox, oy, oz)));
            Assert.That(_m.LastRepelTick, Is.EqualTo(_s.Tick));
            Assert.That(Horizontal(_m.LastRepelVelocityAfter), Is.EqualTo(expected).Within(1e-3f));
        }

        [Test]
        public void Repulsion_VersLeHaut_AnnuleLaChute()
        {
            PutInAir(new Vector3(0f, -15f, 0f));
            Step(PressRepel(new Vector3(0f, -10f, 0f)));
            Assert.That(_m.LastRepelVelocityAfter.y, Is.EqualTo(_cfg.repelImpulse).Within(1e-3f));
        }

        [Test]
        public void Repulsion_UneSeuleCharge_Recharge5s()
        {
            int cooldown = MovementSettings.SecondsToTicks(_cfg.repelCooldown, Dt);   // 300
            int buffer = MovementSettings.SecondsToTicks(_cfg.pressBuffer, Dt);
            PutInAir(Vector3.zero, 1000f);
            int t1 = _s.Tick + 1;
            Step(PressRepel(new Vector3(0f, -10f, 0f), t1));
            Assume.That(_m.LastRepelTick, Is.EqualTo(t1));

            // Un second appui 1 s après : refusé (une seule charge).
            int p2 = t1 + 60;
            while (_s.Tick < p2 - 1)
                Step(MovementInput.Neutral);
            while (_s.Tick < p2 + buffer)
                Step(PressRepel(new Vector3(0f, -10f, 0f), p2));
            Assert.That(_m.LastRepelTick, Is.EqualTo(t1));
            Assert.That(_m.LastRefusal, Is.EqualTo(MagnetRefusal.Recharge));
            Assert.That(_m.RepelStatus, Is.EqualTo(OrbStatus.Recharge));

            // Juste avant 5 s : l'appui part dès que la charge revient.
            int p3 = t1 + cooldown - 3;
            while (_s.Tick < p3 - 1)
                Step(MovementInput.Neutral);
            while (_s.Tick < t1 + cooldown)
                Step(PressRepel(new Vector3(0f, -10f, 0f), p3));
            Assert.That(_m.LastRepelTick, Is.EqualTo(t1 + cooldown));
        }

        [Test]
        public void Repulsion_NeSAccumulePas()
        {
            int cooldown = MovementSettings.SecondsToTicks(_cfg.repelCooldown, Dt);
            int buffer = MovementSettings.SecondsToTicks(_cfg.pressBuffer, Dt);
            PutInAir(Vector3.zero, 1000f);
            Step(PressRepel(new Vector3(0f, -10f, 0f)));
            for (int i = 0; i < 3 * cooldown; i++)                    // 15 s d'attente
                Step(MovementInput.Neutral);
            int t2 = _s.Tick + 1;
            Step(PressRepel(new Vector3(0f, -10f, 0f), t2));
            Assume.That(_m.LastRepelTick, Is.EqualTo(t2));
            int p3 = t2 + 1;
            while (_s.Tick < p3 + buffer)
                Step(PressRepel(new Vector3(0f, -10f, 0f), p3));
            Assert.That(_m.LastRepelTick, Is.EqualTo(t2), "une seule répulsion, même après une longue attente");
            Assert.That(_m.LastRefusal, Is.EqualTo(MagnetRefusal.Recharge));
        }

        [TestCase(38f, true, TestName = "Repulsion_Portee40m_A38mAcceptee")]
        [TestCase(45f, false, TestName = "Repulsion_Portee40m_A45mRefusee")]
        public void Repulsion_Portee(float distance, bool accepted)
        {
            Assume.That(_cfg.repelRange, Is.EqualTo(40f));
            Step(PressRepel(new Vector3(0f, 0f, distance)));
            Assert.That(_m.LastRepelTick == _s.Tick, Is.EqualTo(accepted));
            if (!accepted)
                Assert.That(_m.LastRefusal, Is.EqualTo(MagnetRefusal.RienAPortee));
            Assert.That(_m.RepelInRange, Is.EqualTo(accepted), "marquage de la visée seulement à portée");
        }

        [Test]
        public void Repulsion_PendantLaTraction_LArrete()
        {
            Step(PressAttract(new Vector3(0f, 25f, 25f)));
            for (int i = 0; i < 5; i++)
                Step(Holding());
            MovementInput repel = PressRepel(new Vector3(0f, -5f, 0f));
            repel.AttractHeld = true;
            repel.AttractPressedTick = _attractTick;
            Step(repel);
            Assert.That(_m.LastTractionEnd, Is.EqualTo(TractionEnd.Repulsion));
            Assert.That(_m.LastRepelTick, Is.EqualTo(_s.Tick));
        }

        // ---------- Élan : dash et glissade ----------

        [Test]
        public void Dash_PuisTraction_GardeLaSortieDuDashPasLePic()
        {
            var dash = MovementInput.Neutral;
            dash.Yaw = 90f;
            dash.DashPressedTick = _s.Tick + 1;
            Step(dash);
            Assume.That(_s.DashTicksLeft, Is.GreaterThan(0));
            float exit = _s.DashExitTarget;
            Step(PressAttract(new Vector3(0f, 20f, 20f)));
            Assert.That(_s.DashTicksLeft, Is.EqualTo(0));
            Assert.That(_s.Velocity.magnitude, Is.EqualTo(exit + _cfg.tractionAcceleration * Dt).Within(1e-2f));
        }

        [Test]
        public void Glissade_TractionSansPerte()
        {
            _s.Velocity = new Vector3(10f, 0f, 0f);
            var slide = MovementInput.Neutral;
            slide.Yaw = 90f;
            slide.SlideHeld = true;
            Step(slide);
            Assume.That(_s.Mode, Is.EqualTo(MovementMode.Slide));
            float speed = _s.Velocity.magnitude;
            Step(PressAttract(new Vector3(10f, 5f, 0f)));
            Assert.That(_s.Velocity.magnitude, Is.GreaterThanOrEqualTo(speed));
        }

        [Test]
        public void Glissade_RepulsionHorizontale_LaGlissadeContinue()
        {
            _s.Velocity = new Vector3(10f, 0f, 0f);
            var slide = MovementInput.Neutral;
            slide.Yaw = 90f;
            slide.SlideHeld = true;
            Step(slide);
            Assume.That(_s.Mode, Is.EqualTo(MovementMode.Slide));
            MovementInput repel = PressRepel(new Vector3(0f, 0f, -10f));   // mur à gauche, à hauteur des yeux
            repel.SlideHeld = true;
            Step(repel);
            Assert.That(_s.Mode, Is.EqualTo(MovementMode.Slide));
            Assert.That(Horizontal(_m.LastRepelVelocityAfter), Is.GreaterThanOrEqualTo(10f - 1e-3f));
        }

        // ---------- États de l'orbe ----------

        [Test]
        public void Etats_PreteEnUsageRecharge()
        {
            Assert.That(_m.Status, Is.EqualTo(OrbStatus.Prete));
            Step(PressAttract(new Vector3(0f, 25f, 25f)));
            Assert.That(_m.AttractStatus, Is.EqualTo(OrbStatus.EnUsage));
            Step(Holding());
            Step(MovementInput.Neutral);
            Assert.That(_m.AttractStatus, Is.EqualTo(OrbStatus.Prete), "il reste des charges d'attraction");

            Step(PressRepel(new Vector3(0f, -5f, 0f)));
            Assert.That(_m.RepelStatus, Is.EqualTo(OrbStatus.EnUsage));
            Assert.That(_m.LastMode, Is.EqualTo(MagnetMode.Repulsion));
            for (int i = 0; i < MovementSettings.SecondsToTicks(_cfg.repelUseDuration, Dt); i++)
                Step(MovementInput.Neutral);
            Assert.That(_m.RepelStatus, Is.EqualTo(OrbStatus.Recharge));
        }
    }
}
