using System.Linq;
using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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

    /// <summary>
    /// Orbe unique (D-024) : traction (A / RB maintenu), répulsion (clic droit / LT), attraper ou relâcher (F / X) et lancer
    /// (clic gauche / RT), toutes disponibles en même temps ; pas d'endurance ; couleur de l'orbe selon l'action.
    /// </summary>
    public class TelekinesisTests
    {
        private const float Dt = 1f / 60f;
        private MovementSettings _move;
        private MagnetismOrbSettings _magnetCfg;
        private TelekinesisOrbSettings _cfg;
        private FakeGroundProbe _ground;
        private FakeAimProbe _aim;
        private FakeGrabProbe _grab;
        private MovementState _s;
        private MagnetismState _m;
        private TelekinesisState _t;
        private TelekinesisCommand _last;

        [SetUp]
        public void SetUp()
        {
            _move = ScriptableObject.CreateInstance<MovementSettings>();
            _magnetCfg = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            _cfg = ScriptableObject.CreateInstance<TelekinesisOrbSettings>();
            _ground = new FakeGroundProbe { Position = Vector3.zero };
            _aim = new FakeAimProbe();
            _grab = new FakeGrabProbe();
            _s = MovementState.Create(Vector3.zero);
            _m = MagnetismState.Create();
            _t = TelekinesisState.Create();
            Step(MovementInput.Neutral);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_move);
            Object.DestroyImmediate(_magnetCfg);
            Object.DestroyImmediate(_cfg);
        }

        /// <summary>Comme PlayerMotor : magnétisme, télékinésie (même entrée, chacun sur ses touches), mouvement.</summary>
        private void Step(MovementInput input)
        {
            MagnetismOrb.Step(ref _m, ref _s, input, _magnetCfg, _move, Dt, _aim);
            _last = TelekinesisOrb.Step(ref _t, _s, input, _cfg, _move, Dt, _grab);
            MovementMotor.Step(ref _s, input, _move, Dt, _ground);
        }

        private MovementInput Grab(int pressTick = 0)
        {
            MovementInput i = MovementInput.Neutral;
            i.GrabPressedTick = pressTick != 0 ? pressTick : _s.Tick + 1;
            return i;
        }

        private MovementInput Throw(int pressTick = 0)
        {
            MovementInput i = MovementInput.Neutral;
            i.ThrowPressedTick = pressTick != 0 ? pressTick : _s.Tick + 1;
            return i;
        }

        private void GrabTarget(float mass = 10f)
        {
            _grab.HasTarget = true;
            _grab.Mass = mass;
            Step(Grab());
            Assume.That(_t.Holding, Is.True);
        }

        /// <summary>Surface à 35 m devant, en haut (pitch -45°), pour une traction.</summary>
        private MovementInput AimAtWall(MovementInput i)
        {
            _aim.HasTarget = true;
            _aim.Target = MagnetismOrb.EyePosition(_s, _move) + new Vector3(0f, 25f, 25f);
            i.Pitch = -45f;
            i.Yaw = 0f;
            return i;
        }

        private int StartTraction()
        {
            MovementInput attract = AimAtWall(MovementInput.Neutral);
            attract.AttractPressedTick = _s.Tick + 1;
            attract.AttractHeld = true;
            Step(attract);
            Assume.That(_s.Mode, Is.EqualTo(MovementMode.Traction));
            return attract.AttractPressedTick;
        }

        private MovementInput HoldTraction(int pressTick)
        {
            MovementInput i = AimAtWall(MovementInput.Neutral);
            i.AttractPressedTick = pressTick;
            i.AttractHeld = true;
            return i;
        }

        // ---------- Orbe unique : toutes les actions en même temps ----------

        [Test]
        public void OrbeUnique_TractionEnTenantUnObjet_PuisLancerEnArrivant()
        {
            GrabTarget();
            int press = StartTraction();
            Assert.That(_t.Holding, Is.True, "l'objet reste tenu pendant la traction");
            for (int i = 0; i < 20; i++)
                Step(HoldTraction(press));
            Assume.That(_s.Mode, Is.EqualTo(MovementMode.Traction));

            MovementInput throwDuring = HoldTraction(press);
            throwDuring.ThrowPressedTick = _s.Tick + 1;
            Step(throwDuring);
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Lancer), "lancer pendant la traction");
            Assert.That(_t.Holding, Is.False);
            Assert.That(_s.Mode, Is.EqualTo(MovementMode.Traction), "le lancer n'arrête pas la traction");
            Assert.That(_m.Tracting, Is.True);
        }

        [Test]
        public void OrbeUnique_RepulsionEnTenantUnObjet_LObjetResteTenu()
        {
            GrabTarget();
            MovementInput repel = AimAtWall(MovementInput.Neutral);
            repel.Pitch = 60f;                                        // vers le sol
            _aim.HasTarget = false;
            _aim.BlockAt = 3f;
            repel.RepelPressedTick = _s.Tick + 1;
            Step(repel);
            Assert.That(_m.LastRepelTick, Is.EqualTo(_s.Tick), "la répulsion part");
            Assert.That(_t.Holding, Is.True);
        }

        [Test]
        public void OrbeUnique_ChaqueToucheSonAction()
        {
            // F ne lance pas de traction, A n'attrape pas.
            _grab.HasTarget = true;
            MovementInput grab = AimAtWall(Grab());
            Step(grab);
            Assert.That(_t.Holding, Is.True);
            Assert.That(_m.Tracting, Is.False, "F n'attire pas");

            Step(Grab());                                             // relâche
            Assume.That(_t.Holding, Is.False);
            int press = StartTraction();
            Assert.That(_t.Holding, Is.False, "A n'attrape pas");
            Assert.That(press, Is.GreaterThan(0));
        }

        [Test]
        public void ClicGauche_SansObjet_NeFaitRien()
        {
            _grab.HasTarget = true;
            Step(AimAtWall(Throw()));
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Aucune));
            Assert.That(_t.Holding, Is.False, "le clic gauche n'attrape pas");
            Assert.That(_t.LastRefusalTick, Is.EqualTo(MovementState.NeverTick), "aucun refus : réservé à l'artefact");
            Assert.That(_m.Tracting, Is.False);
            Assert.That(_m.LastRepelTick, Is.EqualTo(MovementState.NeverTick));
        }

        // ---------- Attraper, tenir, relâcher ----------

        [Test]
        public void Attraper_F_ObjetTenuSansMaintenir()
        {
            GrabTarget();
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Attraper));
            Assert.That(_last.TargetId, Is.EqualTo(_grab.Id));
            for (int i = 0; i < 30; i++)
                Step(MovementInput.Neutral);                         // touche relâchée
            Assert.That(_t.Holding, Is.True, "tenu sans maintenir la touche");
            Assert.That(_t.Status, Is.EqualTo(OrbStatus.EnUsage));
        }

        [Test]
        public void Attraper_Refus_TropLourd_Rien()
        {
            _grab.HasTarget = true;
            _grab.Mass = 60f;
            Step(Grab());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.TropLourd));
            Assert.That(_t.AimTooHeavy, Is.True);

            _grab.HasTarget = false;
            Step(Grab());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.RienAttrapable));
            Assert.That(_t.Holding, Is.False);
        }

        [Test]
        public void Attraper_HorsDePortee_Refuse()
        {
            _grab.HasTarget = true;
            _grab.Distance = _cfg.range + 1f;
            Step(Grab());
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.RienAttrapable));
        }

        [Test]
        public void Relacher_F_PoseLObjet()
        {
            GrabTarget();
            Step(MovementInput.Neutral);
            Step(Grab());
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Poser));
            Assert.That(_last.TargetId, Is.EqualTo(_grab.Id));
            Assert.That(_t.Holding, Is.False);
            Assert.That(_t.LastThrowTick, Is.EqualTo(MovementState.NeverTick), "poser n'est pas un lancer (pas de recharge)");
        }

        // ---------- Lancer ----------

        [Test]
        public void Lancer_ClicGauche_RelacheLObjetDansLaVisee()
        {
            GrabTarget();
            Step(MovementInput.Neutral);
            MovementInput throwInput = Throw();
            throwInput.Yaw = 90f;
            Step(throwInput);
            Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Lancer));
            Assert.That(_last.TargetId, Is.EqualTo(_grab.Id));
            Assert.That(Vector3.Angle(_last.AimDirection, Vector3.right), Is.LessThan(0.1f), "direction de la visée");
            Assert.That(_t.Holding, Is.False);
            Assert.That(_t.LastThrownId, Is.EqualTo(_grab.Id));
        }

        [Test]
        public void Lancer_DirectionToujoursCelleDuViseur_SeuleLaPartUtileDeLaVitesseSAjoute()
        {
            Vector3 aim = new Vector3(0f, 0f, 2f);
            // Course de côté : la direction ne change pas, rien ne s'ajoute.
            Vector3 v = TelekinesisOrb.ThrowVelocity(aim, new Vector3(15f, 0f, 0f), _cfg);
            Assert.That(Vector3.Angle(v, aim), Is.LessThan(1e-3f), "jamais dévié");
            Assert.That(v.magnitude, Is.EqualTo(_cfg.throwSpeed).Within(1e-3f));
            // Course vers la cible : la vitesse s'ajoute.
            v = TelekinesisOrb.ThrowVelocity(aim, new Vector3(0f, 0f, 15f), _cfg);
            Assert.That(v.magnitude, Is.EqualTo(_cfg.throwSpeed + 15f).Within(1e-3f), "plus fort en courant vers la cible");
            // En diagonale : seule la part dans l'axe du lancer.
            v = TelekinesisOrb.ThrowVelocity(aim, new Vector3(10f, 4f, 10f), _cfg);
            Assert.That(Vector3.Angle(v, aim), Is.LessThan(1e-3f));
            Assert.That(v.magnitude, Is.EqualTo(_cfg.throwSpeed + 10f).Within(1e-3f));
            // En s'éloignant de la cible : pas de perte.
            v = TelekinesisOrb.ThrowVelocity(aim, new Vector3(0f, 0f, -15f), _cfg);
            Assert.That(v.magnitude, Is.EqualTo(_cfg.throwSpeed).Within(1e-3f), "jamais moins que la vitesse de lancer");
            _cfg.inheritPlayerVelocity = false;
            v = TelekinesisOrb.ThrowVelocity(aim, new Vector3(0f, 0f, 15f), _cfg);
            Assert.That(v.magnitude, Is.EqualTo(_cfg.throwSpeed).Within(1e-3f));
        }

        [Test]
        public void Recharge_ApresUnLancer()
        {
            int cooldown = MovementSettings.SecondsToTicks(_cfg.cooldown, Dt);   // 18
            int buffer = MovementSettings.SecondsToTicks(_cfg.pressBuffer, Dt);  // 6
            GrabTarget();
            int thrown = _s.Tick + 1;
            Step(Throw(thrown));
            Assume.That(_t.LastThrowTick, Is.EqualTo(thrown));
            Assert.That(_t.Status, Is.EqualTo(OrbStatus.Recharge));

            int early = thrown + 3;
            while (_s.Tick < early - 1)
                Step(MovementInput.Neutral);
            while (_s.Tick < early + buffer)
                Step(Grab(early));
            Assert.That(_t.LastRefusal, Is.EqualTo(TelekinesisRefusal.Recharge));
            Assert.That(_t.Holding, Is.False);

            int late = thrown + cooldown - 4;
            while (_s.Tick < late - 1)
                Step(MovementInput.Neutral);
            while (_s.Tick < thrown + cooldown)
                Step(Grab(late));
            Assert.That(_t.Holding, Is.True, "attrapé dès la fin de la recharge (appui mémorisé)");
            Assert.That(_t.GrabTick, Is.EqualTo(thrown + cooldown));
        }

        [Test]
        public void PasDEndurance_AttraperEtLancerEnChaine()
        {
            // Avec l'ancienne endurance (100, 10 + 15 par cycle, sans régénération pendant 0.8 s), le 5e cycle était refusé.
            int cooldown = MovementSettings.SecondsToTicks(_cfg.cooldown, Dt);
            _grab.HasTarget = true;
            for (int cycle = 0; cycle < 12; cycle++)
            {
                Step(Grab());
                Assert.That(_t.Holding, Is.True, $"cycle {cycle} : attrapé");
                Step(Throw());
                Assert.That(_last.Kind, Is.EqualTo(TelekinesisCommandKind.Lancer), $"cycle {cycle} : lancé");
                for (int i = 0; i < cooldown; i++)
                    Step(MovementInput.Neutral);
            }

            Assert.That(_t.LastRefusalTick, Is.EqualTo(MovementState.NeverTick), "jamais refusé");
        }

        // ---------- Perte et viseur ----------

        [Test]
        public void ObjetPerdu_PlusRienEnMain()
        {
            GrabTarget();
            TelekinesisOrb.NotifyLost(ref _t, _s.Tick);
            Assert.That(_t.Holding, Is.False);
            Assert.That(_t.Status, Is.EqualTo(OrbStatus.Prete));
        }

        [Test]
        public void Viseur_AttraperEtLancerPossibles()
        {
            _grab.HasTarget = true;
            Step(MovementInput.Neutral);
            Assert.That(TelekinesisOrb.CanGrab(_t, _cfg, _s.Tick, Dt), Is.True);
            Assert.That(TelekinesisOrb.CanThrow(_t), Is.False);
            _grab.Mass = 60f;
            Step(MovementInput.Neutral);
            Assert.That(TelekinesisOrb.CanGrab(_t, _cfg, _s.Tick, Dt), Is.False, "trop lourd");
            _grab.Mass = 10f;
            GrabTarget();
            Assert.That(TelekinesisOrb.CanGrab(_t, _cfg, _s.Tick, Dt), Is.False, "déjà un objet en main");
            Assert.That(TelekinesisOrb.CanThrow(_t), Is.True);
        }

        // ---------- Couleur de l'orbe (OrbLook) ----------

        private OrbLookKind Look() => OrbLook.Resolve(_s, _m, _t, flashTicks: 9);

        [Test]
        public void Couleur_PreteTractionTenuLancer()
        {
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Prete));
            GrabTarget();
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Tenu), "violet : objet tenu");
            int press = StartTraction();
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Traction), "bleu : la traction l'emporte sur l'objet tenu");
            MovementInput throwDuring = HoldTraction(press);
            throwDuring.ThrowPressedTick = _s.Tick + 1;
            Step(throwDuring);
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Lancer), "vert : le lancer l'emporte (éclat)");
            for (int i = 0; i < 9; i++)
                Step(HoldTraction(press));
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Traction), "puis retour au bleu après l'éclat");
        }

        [Test]
        public void Couleur_RepulsionEclatSurLObjetTenu()
        {
            GrabTarget();
            MovementInput repel = MovementInput.Neutral;
            repel.Pitch = 60f;
            _aim.BlockAt = 3f;
            repel.RepelPressedTick = _s.Tick + 1;
            Step(repel);
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Repulsion), "orange : la répulsion l'emporte (éclat)");
            for (int i = 0; i < 9; i++)
                Step(MovementInput.Neutral);
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Tenu), "puis retour au violet");
        }

        [Test]
        public void Couleur_RechargeSansChargeDAttraction()
        {
            _m.AttractCharges = 0.5f;
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Recharge));
            GrabTarget();
            Assert.That(Look(), Is.EqualTo(OrbLookKind.Tenu), "l'objet tenu passe avant la recharge");
        }

        // ---------- Réglages et commandes du projet ----------

        [Test]
        public void Reglages_AttractionUneChargeToutesLes4s()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MagnetismOrbSettings>("Assets/_Project/Settings/Magnetism_Default.asset");
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.attractChargeRechargeTime, Is.EqualTo(4f));
        }

        [Test]
        public void Commandes_OrbeUnique_ClavierAzertyEtManette()
        {
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/_Project/Input/PlayerControls.inputactions");
            Assert.That(actions, Is.Not.Null);
            InputActionMap map = actions.FindActionMap("Player", throwIfNotFound: true);
            string Paths(string action) =>
                string.Join(" ", map.FindAction(action, throwIfNotFound: true).bindings.Select(b => b.path).OrderBy(p => p));

            // <Keyboard>/q = la touche physique à la place du Q en QWERTY, marquée A sur un AZERTY.
            Assert.That(Paths("Attract"), Is.EqualTo("<Gamepad>/rightShoulder <Keyboard>/q"));
            Assert.That(Paths("Repel"), Is.EqualTo("<Gamepad>/leftTrigger <Mouse>/rightButton"));
            Assert.That(Paths("Grab"), Is.EqualTo("<Gamepad>/buttonWest <Keyboard>/f"));
            Assert.That(Paths("Throw"), Is.EqualTo("<Gamepad>/rightTrigger <Mouse>/leftButton"));
            foreach (string removed in new[] { "SwapOrb", "SwapOrbScroll", "SelectOrb1", "SelectOrb2" })
                Assert.That(map.FindAction(removed), Is.Null, $"{removed} supprimé");

            // Aucune touche n'est partagée entre deux actions.
            var all = map.bindings.Where(b => !b.isComposite).GroupBy(b => b.path).Where(g => g.Select(b => b.action).Distinct().Count() > 1);
            Assert.That(all.Select(g => g.Key), Is.Empty);
        }

        // ---------- Aide à la visée : cône ----------

        [Test]
        public void Aide_ConeSelonLaDistance()
        {
            Assert.That(_cfg.aimAssist, Is.True);
            Assert.That(GrabAssist.ConeAngle(_cfg, 2f), Is.EqualTo(8f).Within(1e-3f), "8° de près");
            Assert.That(GrabAssist.ConeAngle(_cfg, 5f), Is.EqualTo(8f).Within(1e-3f));
            Assert.That(GrabAssist.ConeAngle(_cfg, 60f), Is.EqualTo(3f).Within(1e-3f), "3° à 60 m");
            Assert.That(GrabAssist.ConeAngle(_cfg, 32.5f), Is.EqualTo(5.5f).Within(1e-3f), "linéaire entre les deux");
            Assert.That(GrabAssist.InCone(_cfg, 2.9f, 60f), Is.True);
            Assert.That(GrabAssist.InCone(_cfg, 3.1f, 60f), Is.False);
            Assert.That(GrabAssist.InCone(_cfg, 1f, 61f), Is.False, "hors de portée");
            Assert.That(GrabAssist.AngleTo(Vector3.zero, Vector3.forward, Vector3.back), Is.EqualTo(180f).Within(1e-3f), "derrière");
            _cfg.aimAssist = false;
            Assert.That(GrabAssist.ConeAngle(_cfg, 10f), Is.EqualTo(0f));
        }

        [Test]
        public void Aide_ReglagesParDefaut()
        {
            var asset = AssetDatabase.LoadAssetAtPath<TelekinesisOrbSettings>("Assets/_Project/Settings/Telekinesis_Default.asset");
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.aimAssist, Is.True);
            Assert.That(asset.assistNearAngle, Is.EqualTo(8f));
            Assert.That(asset.assistNearDistance, Is.EqualTo(5f));
            Assert.That(asset.assistFarAngle, Is.EqualTo(3f));
        }

        [Test]
        public void Aide_AngleDeLObjetSelectionneRetenu()
        {
            _grab.HasTarget = true;
            Step(MovementInput.Neutral);
            Assert.That(_t.AimAngle, Is.EqualTo(0f));
        }
    }
}
