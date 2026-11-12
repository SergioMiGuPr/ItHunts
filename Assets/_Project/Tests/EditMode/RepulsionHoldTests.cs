using System.Reflection;
using ItHunts.Combat;
using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItHunts.Tests
{
    /// <summary>
    /// Répulsion maintenue (D-029) : onde de choc et propulsion à l'appui seulement, poussée continue tant que le bouton est
    /// tenu (au plus 0.8 s, coupée par une autre action), recharge à partir de la fin, plafond de 25 m/s, pas de dégâts ni
    /// d'étourdissement continus contre un mur, traqueur exclu.
    /// </summary>
    public class RepulsionHoldTests
    {
        private const float Dt = 1f / 60f;
        private MovementSettings _move;
        private MagnetismOrbSettings _cfg;
        private TrainingDummySettings _dummyCfg;
        private Scene _scene;

        [SetUp]
        public void SetUp()
        {
            _move = ScriptableObject.CreateInstance<MovementSettings>();
            _cfg = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            _dummyCfg = ScriptableObject.CreateInstance<TrainingDummySettings>();
            _scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _scene.GetRootGameObjects())
            {
                foreach (var g in go.GetComponentsInChildren<Grabbable>())
                    Invoke(g, "OnDisable");
                foreach (var d in go.GetComponentsInChildren<Damageable>())
                    Invoke(d, "OnDisable");
            }

            EditorSceneManager.ClosePreviewScene(_scene);
            Object.DestroyImmediate(_move);
            Object.DestroyImmediate(_cfg);
            Object.DestroyImmediate(_dummyCfg);
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        // ---------- Logique de l'orbe ----------

        private sealed class Sim
        {
            public MovementState S = MovementState.Create(Vector3.zero);
            public MagnetismState M = MagnetismState.Create();
            public readonly FakeGroundProbe Ground = new FakeGroundProbe { Position = Vector3.zero };
            public readonly FakeAimProbe Aim = new FakeAimProbe { BlockAt = 3f };   // le sol devant, à 3 m
            public int PressTick;

            public void Step(MovementSettings move, MagnetismOrbSettings cfg, MovementInput input)
            {
                input.Pitch = 60f;
                MagnetismOrb.Step(ref M, ref S, input, cfg, move, Dt, Aim);
                MovementMotor.Step(ref S, input, move, Dt, Ground);
            }
        }

        private MovementInput Repel(Sim sim, bool held)
        {
            MovementInput i = MovementInput.Neutral;
            i.RepelPressedTick = sim.PressTick;
            i.RepelHeld = held;
            return i;
        }

        private Sim Started(bool held)
        {
            var sim = new Sim();
            sim.Step(_move, _cfg, MovementInput.Neutral);
            sim.PressTick = sim.S.Tick + 1;
            sim.Step(_move, _cfg, Repel(sim, held));
            Assume.That(sim.M.LastRepelTick, Is.EqualTo(sim.PressTick), "la répulsion part");
            return sim;
        }

        [Test]
        public void Propulsion_SeulementALAppui_IdentiqueAvecEtSansMaintien()
        {
            Sim tap = Started(held: false);
            Sim hold = Started(held: true);
            Assert.That(tap.M.RepelHolding, Is.False, "appui simple : pas de maintien");
            Assert.That(hold.M.RepelHolding, Is.True);
            for (int i = 0; i < 40; i++)
            {
                tap.Step(_move, _cfg, MovementInput.Neutral);
                hold.Step(_move, _cfg, Repel(hold, true));
                Assert.That(Vector3.Distance(tap.S.Velocity, hold.S.Velocity), Is.LessThan(1e-5f), $"tick {i} : même propulsion");
            }

            Assert.That(hold.M.LastRepelTick, Is.EqualTo(hold.PressTick), "aucune nouvelle répulsion pendant le maintien");
        }

        [Test]
        public void Maintien_SArreteAuRelachement_RechargeDepuisLaFin()
        {
            Sim sim = Started(held: true);
            for (int i = 0; i < 10; i++)
                sim.Step(_move, _cfg, Repel(sim, true));
            Assume.That(sim.M.RepelHolding, Is.True);
            Assert.That(sim.M.RepelStatus, Is.EqualTo(OrbStatus.EnUsage));
            sim.Step(_move, _cfg, Repel(sim, false));                  // relâché
            Assert.That(sim.M.RepelHolding, Is.False, "relâché : fin");
            int end = sim.S.Tick;
            Assert.That(sim.M.LastRepelEndTick, Is.EqualTo(end));
            Assert.That(MagnetismOrb.RepelCooldownRemaining(sim.M, _cfg, end, Dt), Is.EqualTo(_cfg.repelCooldown).Within(1e-4f),
                "la recharge de 5 s part de la fin");
            int cooldown = MovementSettings.SecondsToTicks(_cfg.repelCooldown, Dt);
            while (sim.S.Tick < end + cooldown - 1)
                sim.Step(_move, _cfg, MovementInput.Neutral);
            Assert.That(sim.M.RepelStatus, Is.EqualTo(OrbStatus.Recharge), "pas encore prête");
            sim.Step(_move, _cfg, MovementInput.Neutral);
            Assert.That(sim.M.RepelStatus, Is.EqualTo(OrbStatus.Prete), "prête 5 s après la fin");
        }

        [Test]
        public void Maintien_SArreteA08s()
        {
            Assert.That(_cfg.repelHoldMaxDuration, Is.EqualTo(0.8f));
            Sim sim = Started(held: true);
            int maxTicks = MovementSettings.SecondsToTicks(_cfg.repelHoldMaxDuration, Dt);   // 48
            while (sim.S.Tick < sim.PressTick + maxTicks - 1)
                sim.Step(_move, _cfg, Repel(sim, true));
            Assert.That(sim.M.RepelHolding, Is.True, "encore en cours juste avant 0.8 s");
            sim.Step(_move, _cfg, Repel(sim, true));
            Assert.That(sim.M.RepelHolding, Is.False, "fin à 0.8 s, même bouton tenu");
            Assert.That(sim.M.LastRepelEndTick, Is.EqualTo(sim.PressTick + maxTicks));
        }

        [Test]
        public void Maintien_CoupeParUneAutreActionDeLOrbe()
        {
            foreach (string action in new[] { "prise", "lancer" })
            {
                Sim sim = Started(held: true);
                sim.Step(_move, _cfg, Repel(sim, true));
                MovementInput i = Repel(sim, true);
                if (action == "prise")
                    i.GrabPressedTick = sim.S.Tick + 1;
                else
                    i.ThrowPressedTick = sim.S.Tick + 1;
                sim.Step(_move, _cfg, i);
                Assert.That(sim.M.RepelHolding, Is.False, $"{action} : le maintien s'arrête");
            }

            // Traction : une surface visée et une charge d'attraction.
            Sim t = Started(held: true);
            t.Aim.BlockAt = 0f;
            t.Aim.HasTarget = true;
            t.Aim.Target = MagnetismOrb.EyePosition(t.S, _move) + MagnetismOrb.AimDirection(0f, 60f) * 10f;
            MovementInput a = Repel(t, true);
            a.AttractPressedTick = t.S.Tick + 1;
            a.AttractHeld = true;
            t.Step(_move, _cfg, a);
            Assert.That(t.M.RepelHolding, Is.False, "traction : le maintien s'arrête");
        }

        // ---------- Poussée continue (physique, scène isolée) ----------

        private Grabbable Floating(Vector3 position, float mass)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.7f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = mass;
            rb.useGravity = false;
            var g = go.AddComponent<Grabbable>();
            Invoke(g, "Awake");
            Invoke(g, "OnEnable");
            return g;
        }

        private (Damageable d, EnemyBody body) Dummy(Vector3 feet, bool stalker = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = feet + Vector3.up * 0.9f;
            go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var body = go.AddComponent<EnemyBody>();
            var bso = new SerializedObject(body);
            bso.FindProperty("settings").objectReferenceValue = _dummyCfg;
            bso.ApplyModifiedPropertiesWithoutUndo();
            var d = go.AddComponent<Damageable>();
            var dso = new SerializedObject(d);
            dso.FindProperty("settings").objectReferenceValue = _dummyCfg;
            dso.FindProperty("immuneToOrbs").boolValue = stalker;
            dso.ApplyModifiedPropertiesWithoutUndo();
            body.Init();
            d.Init();
            Invoke(d, "OnEnable");
            return (d, body);
        }

        private void SyncPhysics()
        {
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(Dt);
        }

        [Test]
        public void Poussee_ContinueNeDepassePas25ms()
        {
            Assert.That(_cfg.repelHoldMaxSpeed, Is.EqualTo(25f));
            Assert.That(AreaEffects.HoldDelta(24f, 35f, 0.1f, _cfg), Is.EqualTo(1f).Within(1e-4f), "jusqu'à 25 m/s, pas plus");
            Assert.That(AreaEffects.HoldDelta(30f, 35f, 0.1f, _cfg), Is.EqualTo(0f), "au-delà : n'ajoute rien");
            Assert.That(AreaEffects.HoldDelta(0f, 35f, Dt, _cfg), Is.EqualTo(35f * Dt).Within(1e-4f));

            Grabbable crate = Floating(new Vector3(0f, 1f, 3f), 5f);       // légère : ×1.3
            SyncPhysics();
            var eye = new Vector3(0f, 1f, 0f);
            float max = 0f;
            for (int i = 0; i < 240; i++)
            {
                // Le joueur suit la caisse : elle reste dans le cône tant qu'elle est à portée.
                RepulsionWave.ApplyContinuous(eye, Vector3.forward, eye, _cfg, null, null, Dt, _scene.GetPhysicsScene());
                _scene.GetPhysicsScene().Simulate(Dt);
                max = Mathf.Max(max, Vector3.Dot(crate.Body.linearVelocity, Vector3.forward));
                eye = crate.Body.position - Vector3.forward * 3f;
            }

            Assert.That(max, Is.LessThanOrEqualTo(25f + 1e-3f), "la seule poussée continue ne dépasse jamais 25 m/s");
            Assert.That(max, Is.GreaterThan(24f), "elle y arrive");
        }

        [Test]
        public void Poussee_PlaqueContreUnMur_NiDegatsNiEtourdissementContinus()
        {
            (Damageable d, EnemyBody body) = Dummy(new Vector3(0f, 0f, 3f));
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = new Vector3(0f, 1f, 3.8f);
            wall.transform.localScale = new Vector3(6f, 4f, 0.3f);
            SyncPhysics();
            int stuns = 0;
            void OnStun(StunEvent e) => stuns++;
            AreaEvents.Stunned += OnStun;
            try
            {
                var eye = new Vector3(0f, 1.6f, 0f);
                for (int i = 0; i < 60; i++)
                {
                    RepulsionWave.ApplyContinuous(eye, Vector3.forward, eye, _cfg, null, null, Dt, _scene.GetPhysicsScene());
                    body.Step(Dt);
                    Physics.SyncTransforms();
                }
            }
            finally
            {
                AreaEvents.Stunned -= OnStun;
            }

            Assert.That(stuns, Is.EqualTo(0), "plaqué en continu (arrivé à moins de 6 m/s) : jamais étourdi");
            Assert.That(d.Health.Current, Is.EqualTo(d.Health.Max), "aucun dégât en continu");
            Assert.That(d.transform.position.z, Is.LessThan(3.65f - 0.39f), "il ne traverse pas le mur");
        }

        [Test]
        public void Poussee_EtourdiUneSeuleFoisALImpact_PuisImmunite()
        {
            (Damageable d, EnemyBody body) = Dummy(new Vector3(0f, 0f, 2f));
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = new Vector3(0f, 1f, 4.5f);
            wall.transform.localScale = new Vector3(6f, 4f, 0.3f);
            SyncPhysics();
            int stuns = 0;
            void OnStun(StunEvent e) => stuns++;
            AreaEvents.Stunned += OnStun;
            try
            {
                var eye = new Vector3(0f, 1.6f, 0f);
                RepulsionWave.Apply(eye, Vector3.forward, eye, _cfg, null, null, _scene.GetPhysicsScene());   // onde de départ
                for (int i = 0; i < 60; i++)
                {
                    RepulsionWave.ApplyContinuous(eye, Vector3.forward, eye, _cfg, null, null, Dt, _scene.GetPhysicsScene());
                    body.Step(Dt);
                    Physics.SyncTransforms();
                }
            }
            finally
            {
                AreaEvents.Stunned -= OnStun;
            }

            Assert.That(stuns, Is.EqualTo(1), "un seul étourdissement, à l'impact");
            float expected = AreaEffects.WallDamage(body.LastStunImpactSpeed, _dummyCfg);
            Assert.That(expected, Is.GreaterThan(0f), "impact assez fort pour des dégâts contre le mur");
            Assert.That(d.Health.Current, Is.EqualTo(d.Health.Max - expected).Within(1e-3f),
                "dégâts contre le mur une seule fois, à l'impact, puis plus rien en continu");
        }

        [Test]
        public void Poussee_TraqueurExclu()
        {
            (_, EnemyBody stalker) = Dummy(new Vector3(0f, 0f, 3f), stalker: true);
            SyncPhysics();
            var eye = new Vector3(0f, 1.6f, 0f);
            RepulsionWave.Apply(eye, Vector3.forward, eye, _cfg, null, null, _scene.GetPhysicsScene());
            for (int i = 0; i < 30; i++)
                RepulsionWave.ApplyContinuous(eye, Vector3.forward, eye, _cfg, null, null, Dt, _scene.GetPhysicsScene());
            Assert.That(stalker.Knocked, Is.False, "le traqueur n'est jamais repoussé, ni à l'appui ni en continu");
            Assert.That(stalker.Velocity.magnitude, Is.EqualTo(0f));
        }

        [Test]
        public void Reglages_RepulsionMaintenue()
        {
            var asset = AssetDatabase.LoadAssetAtPath<MagnetismOrbSettings>("Assets/_Project/Settings/Magnetism_Default.asset");
            Assert.That(asset.waveRange, Is.EqualTo(14f));
            Assert.That(asset.waveConeAngle, Is.EqualTo(60f));
            Assert.That(asset.waveObjectSpeed, Is.EqualTo(22f));
            Assert.That(asset.waveEnemySpeed, Is.EqualTo(22f));
            Assert.That(asset.waveEdgeShare, Is.EqualTo(0.6f));
            Assert.That(asset.repelHoldMaxDuration, Is.EqualTo(0.8f));
            Assert.That(asset.repelHoldAcceleration, Is.EqualTo(35f));
            Assert.That(asset.repelHoldEdgeShare, Is.EqualTo(0.5f));
            Assert.That(asset.repelHoldMaxSpeed, Is.EqualTo(25f));
            Assert.That(AreaEffects.WavePushSpeed(15f, 2f, false, asset), Is.EqualTo(22f).Within(1e-3f), "22 m/s à 2 m");
            Assert.That(AreaEffects.WavePushSpeed(80f, 14f, true, asset), Is.EqualTo(13.2f).Within(1e-3f), "×0.6 au bout");
            Assert.That(AreaEffects.HoldAcceleration(15f, 2f, false, asset), Is.EqualTo(35f).Within(1e-3f));
            Assert.That(AreaEffects.HoldAcceleration(80f, 14f, true, asset), Is.EqualTo(17.5f).Within(1e-3f), "×0.5 au bout");
        }

        // ---------- Dégâts contre un mur (D-031) ----------

        [Test]
        public void Mur_5PVParMsAuDelaDe6_Plafond40()
        {
            Assert.That(_dummyCfg.wallDamagePerSpeed, Is.EqualTo(5f));
            Assert.That(_dummyCfg.wallDamageMax, Is.EqualTo(40f));
            Assert.That(AreaEffects.WallDamage(6f, _dummyCfg), Is.EqualTo(0f), "au seuil : rien");
            Assert.That(AreaEffects.WallDamage(10f, _dummyCfg), Is.EqualTo(20f).Within(1e-4f));
            Assert.That(AreaEffects.WallDamage(14f, _dummyCfg), Is.EqualTo(40f).Within(1e-4f));
            Assert.That(AreaEffects.WallDamage(25f, _dummyCfg), Is.EqualTo(40f), "plafond de 40 PV par impact");
        }

        [Test]
        public void Mur_UnSeulDegatParImpact_PasPendantLImmunite_TraqueurExclu()
        {
            (Damageable d, EnemyBody body) = Dummy(Vector3.zero);
            Assert.That(body.Stun(12f), Is.True);
            body.ApplyWallDamage(12f, Vector3.zero);
            Assert.That(d.Health.Current, Is.EqualTo(70f).Within(1e-3f), "12 m/s : 30 PV");
            Assert.That(body.Stun(12f), Is.False, "pendant l'étourdissement ou l'immunité : pas de nouvel impact compté");

            (Damageable stalker, EnemyBody stalkerBody) = Dummy(new Vector3(5f, 0f, 0f), stalker: true);
            Assert.That(stalkerBody.ApplyWallDamage(20f, Vector3.zero), Is.EqualTo(0f), "traqueur exclu");
            Assert.That(stalker.Health.Current, Is.EqualTo(stalker.Health.Max));
        }

        // ---------- Objets repoussés (D-031) ----------

        private DamageSettings Damage() => ScriptableObject.CreateInstance<DamageSettings>();

        private Grabbable PushedCrate(Vector3 position, float mass, DamageSettings cfg)
        {
            Grabbable g = Floating(position, mass);
            var damager = g.gameObject.AddComponent<ImpactDamager>();
            var so = new SerializedObject(damager);
            so.FindProperty("settings").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();
            damager.Init();
            return g;
        }

        [Test]
        public void Repousse_BlesseCommeUnLancerFois05_SansZone_DurabiliteSurEnnemiSeulement()
        {
            DamageSettings cfg = Damage();
            try
            {
                ImpactDamager.ResetCooldowns();
                Assert.That(cfg.pushedDamageMultiplier, Is.EqualTo(0.5f));
                Grabbable crate = PushedCrate(new Vector3(0f, 1f, 0f), 15f, cfg);
                (Damageable d, _) = Dummy(new Vector3(0f, 0f, 2f));
                int areas = 0;
                void OnArea(AreaImpactEvent e) => areas++;
                AreaEvents.Area += OnArea;
                try
                {
                    crate.MarkPushed(Time.time);
                    Assume.That(crate.PushDangerous(Time.time, cfg.pushedDangerTime), Is.True);
                    int before = crate.ThrowsLeft;
                    ImpactDamager.HandleImpact(crate, null, new Vector3(0f, 0f, 20f), Vector3.zero, cfg, Time.time);   // un mur
                    Assert.That(crate.ThrowsLeft, Is.EqualTo(before), "contre un mur : pas de perte de durabilité");
                    ImpactDamager.HandleImpact(crate, d, new Vector3(0f, 0f, 20f), Vector3.zero, cfg, Time.time);      // un ennemi
                    Assert.That(d.Health.Current, Is.EqualTo(100f - 15f * 12f * 0.1f * 0.5f).Within(1e-3f), "15 kg à 20 m/s : 18 × 0.5 = 9");
                    Assert.That(crate.ThrowsLeft, Is.EqualTo(before - 1), "sur un ennemi : un lancer de durabilité en moins");
                    Assert.That(areas, Is.EqualTo(0), "pas de dégâts de zone");
                }
                finally
                {
                    AreaEvents.Area -= OnArea;
                }
            }
            finally
            {
                Object.DestroyImmediate(cfg);
            }
        }

        [Test]
        public void Repousse_InoffensifSous6ms_EtApres1s_EtPourLeJoueur()
        {
            DamageSettings cfg = Damage();
            try
            {
                ImpactDamager.ResetCooldowns();
                Grabbable crate = PushedCrate(new Vector3(0f, 1f, 0f), 15f, cfg);
                (Damageable d, _) = Dummy(new Vector3(0f, 0f, 2f));

                // Sous 6 m/s : plus dangereux (même s'il accélère ensuite).
                crate.MarkPushed(Time.time);
                crate.Body.linearVelocity = new Vector3(0f, 0f, 5f);
                crate.TickPushDanger(cfg.pushedMinSpeed);
                Assert.That(crate.PushDangerous(Time.time, cfg.pushedDangerTime), Is.False, "passé sous 6 m/s");
                ImpactDamager.HandleImpact(crate, d, new Vector3(0f, 0f, 20f), Vector3.zero, cfg, Time.time);
                Assert.That(d.Health.Current, Is.EqualTo(100f), "plus dangereux : aucun dégât");

                // Plus de 1 s après la poussée : plus dangereux.
                crate.MarkPushed(Time.time - 1.2f);
                Assert.That(crate.PushDangerous(Time.time, cfg.pushedDangerTime), Is.False, "1.2 s après");
                crate.MarkPushed(Time.time - 0.8f);
                Assert.That(crate.PushDangerous(Time.time, cfg.pushedDangerTime), Is.True, "0.8 s après : encore dangereux");

                // Le joueur qui l'a repoussé (et ses alliés) : jamais une cible.
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
                Assert.That(prefab.GetComponentInChildren<Damageable>(true), Is.Null, "un joueur n'est jamais blessé par un objet");
                int hits = 0;
                void OnHit(DamageEvent e) => hits++;
                DamageEvents.Hit += OnHit;
                try
                {
                    crate.MarkPushed(Time.time);
                    ImpactDamager.HandleImpact(crate, null, new Vector3(0f, 0f, 20f), Vector3.zero, cfg, Time.time);   // percute le joueur
                }
                finally
                {
                    DamageEvents.Hit -= OnHit;
                }

                Assert.That(hits, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(cfg);
            }
        }
    }
}
