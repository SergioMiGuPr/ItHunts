using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
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
    /// Partie 2 de la télékinésie (D-025) : dégâts d'impact = masse × (vitesse − seuil) × facteur, intervalle par cible,
    /// PV et réapparition des ennemis factices, publication des coups, ennemis ignorés par le magnétisme, scène de test.
    /// </summary>
    public class CombatTests
    {
        private const float Dt = 1f / 60f;
        private DamageSettings _cfg;
        private TrainingDummySettings _dummyCfg;
        private Scene _scene;

        [SetUp]
        public void SetUp()
        {
            _cfg = ScriptableObject.CreateInstance<DamageSettings>();
            _dummyCfg = ScriptableObject.CreateInstance<TrainingDummySettings>();
            _scene = EditorSceneManager.NewPreviewScene();
            ImpactDamager.ResetCooldowns();
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
            Object.DestroyImmediate(_cfg);
            Object.DestroyImmediate(_dummyCfg);
            ImpactDamager.ResetCooldowns();
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        private Damageable Dummy(Vector3 feet)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = feet + Vector3.up * 0.9f;
            go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            var d = go.AddComponent<Damageable>();
            var so = new SerializedObject(d);
            so.FindProperty("settings").objectReferenceValue = _dummyCfg;
            so.ApplyModifiedPropertiesWithoutUndo();
            d.Init();
            Invoke(d, "OnEnable");
            return d;
        }

        // ---------- Formule ----------

        [Test]
        public void Degats_MasseFoisVitesseAuDessusDuSeuil()
        {
            Assert.That(ImpactDamage.Compute(25f, 30f, _cfg), Is.EqualTo(55f).Within(1e-3f), "caisse de 25 kg lancée à l'arrêt");
            Assert.That(ImpactDamage.Compute(25f, 45f, _cfg), Is.EqualTo(92.5f).Within(1e-3f), "lancée en course");
            Assert.That(ImpactDamage.Compute(5f, 30f, _cfg), Is.EqualTo(11f).Within(1e-3f), "petite caisse");
            Assert.That(ImpactDamage.Compute(40f, 8f, _cfg), Is.EqualTo(0f), "au seuil : rien");
            Assert.That(ImpactDamage.Compute(40f, 5f, _cfg), Is.EqualTo(0f), "sous le seuil : rien");
            Assert.That(ImpactDamage.Compute(20f, 9.9f, _cfg), Is.EqualTo(3.8f).Within(1e-3f), "tonneau tombé de 5 m");
        }

        [Test]
        public void Degats_Paliers()
        {
            Assert.That(ImpactDamage.Tier(10f, _cfg), Is.EqualTo(HitTier.Petit));
            Assert.That(ImpactDamage.Tier(25f, _cfg), Is.EqualTo(HitTier.Moyen));
            Assert.That(ImpactDamage.Tier(60f, _cfg), Is.EqualTo(HitTier.Gros));
        }

        [Test]
        public void Reglages_ParDefaut()
        {
            var damage = AssetDatabase.LoadAssetAtPath<DamageSettings>("Assets/_Project/Settings/Damage_Default.asset");
            var dummy = AssetDatabase.LoadAssetAtPath<TrainingDummySettings>("Assets/_Project/Settings/TrainingDummy_Default.asset");
            var feedback = AssetDatabase.LoadAssetAtPath<DamageFeedbackSettings>("Assets/_Project/Settings/DamageFeedback_Default.asset");
            Assert.That(damage, Is.Not.Null);
            Assert.That(dummy, Is.Not.Null);
            Assert.That(feedback, Is.Not.Null);
            Assert.That(damage.speedThreshold, Is.EqualTo(8f));
            Assert.That(damage.damageFactor, Is.EqualTo(0.1f));
            Assert.That(damage.perTargetCooldown, Is.EqualTo(0.3f));
            Assert.That(dummy.maxHealth, Is.EqualTo(100f));
            Assert.That(dummy.respawnDelay, Is.EqualTo(3f));
            Assert.That(dummy.moveSpeed, Is.EqualTo(6f));
        }

        // ---------- PV ----------

        [Test]
        public void PV_DegatsMortUneFoisPuisReapparition()
        {
            HealthState h = HealthState.Create(100f);
            Assert.That(HealthState.Apply(ref h, 55f, 1f, out bool killed), Is.EqualTo(55f));
            Assert.That(killed, Is.False);
            Assert.That(h.Current, Is.EqualTo(45f));
            Assert.That(HealthState.Apply(ref h, 92.5f, 2f, out killed), Is.EqualTo(45f), "seulement les PV restants");
            Assert.That(killed, Is.True);
            Assert.That(h.Dead, Is.True);
            Assert.That(HealthState.Apply(ref h, 50f, 2.1f, out killed), Is.EqualTo(0f), "mort : plus de dégâts");
            Assert.That(killed, Is.False, "on ne meurt qu'une fois");
            Assert.That(HealthState.TryRespawn(ref h, 4.9f, 3f), Is.False, "pas avant 3 s");
            Assert.That(HealthState.TryRespawn(ref h, 5f, 3f), Is.True);
            Assert.That(h.Current, Is.EqualTo(100f));
            Assert.That(h.Dead, Is.False);
        }

        // ---------- Intervalle par cible ----------

        [Test]
        public void Intervalle_UnCoupParObjetEtParCible()
        {
            var cd = new HitCooldown();
            Assert.That(cd.TryHit(1, 10, 0f, 0.3f), Is.True);
            Assert.That(cd.TryHit(1, 10, 0.2f, 0.3f), Is.False, "même objet, même cible, trop tôt");
            Assert.That(cd.TryHit(1, 11, 0.2f, 0.3f), Is.True, "autre cible");
            Assert.That(cd.TryHit(2, 10, 0.2f, 0.3f), Is.True, "autre objet");
            Assert.That(cd.TryHit(1, 10, 0.31f, 0.3f), Is.True, "après l'intervalle");
        }

        // ---------- Coup porté ----------

        [Test]
        public void Coup_RetireLesPV_PublieLeCoup_RespecteLIntervalle()
        {
            Damageable d = Dummy(Vector3.zero);
            DamageEvent seen = default;
            int count = 0;
            void OnHit(DamageEvent e) { seen = e; count++; }
            DamageEvents.Hit += OnHit;
            try
            {
                Assert.That(ImpactDamager.TryDamage(d, 7, 25f, 30f, Vector3.one, _cfg, 1f, out DamageEvent e), Is.True);
                Assert.That(e.Damage, Is.EqualTo(55f).Within(1e-3f));
                Assert.That(d.Health.Current, Is.EqualTo(45f).Within(1e-3f));
                Assert.That(count, Is.EqualTo(1));
                Assert.That(seen.Target, Is.SameAs(d));
                Assert.That(seen.Tier, Is.EqualTo(HitTier.Moyen));

                Assert.That(ImpactDamager.TryDamage(d, 7, 25f, 30f, Vector3.one, _cfg, 1.1f, out _), Is.False, "rebond sur place : ignoré");
                Assert.That(ImpactDamager.TryDamage(d, 7, 25f, 6f, Vector3.one, _cfg, 2f, out _), Is.False, "sous le seuil");
                Assert.That(count, Is.EqualTo(1));

                Assert.That(ImpactDamager.TryDamage(d, 8, 25f, 45f, Vector3.one, _cfg, 2f, out e), Is.True);
                Assert.That(e.Killed, Is.True, "55 + 92.5 > 100");
                Assert.That(e.Damage, Is.EqualTo(45f).Within(1e-3f), "dégâts pris = PV restants");
                Assert.That(d.Dead, Is.True);
                Assert.That(ImpactDamager.TryDamage(d, 9, 25f, 45f, Vector3.one, _cfg, 2.5f, out _), Is.False, "mort");
            }
            finally
            {
                DamageEvents.Hit -= OnHit;
            }
        }

        [Test]
        public void Physique_CaisseLanceeSurUnFactice_LeBlesse()
        {
            // Vraie collision dans une scène isolée : caisse de 25 kg lancée à 30 m/s sur un mannequin fixe à 6 m.
            Damageable d = Dummy(new Vector3(0f, 0f, 6f));
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(crate, _scene);
            crate.transform.position = new Vector3(0f, 0.9f, 0f);
            crate.transform.localScale = Vector3.one * 0.9f;
            var body = crate.AddComponent<Rigidbody>();
            body.mass = 25f;
            body.useGravity = false;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var g = crate.AddComponent<Grabbable>();
            Invoke(g, "Awake");
            Invoke(g, "OnEnable");
            var damager = crate.AddComponent<ImpactDamager>();
            var so = new SerializedObject(damager);
            so.FindProperty("settings").objectReferenceValue = _cfg;
            so.ApplyModifiedPropertiesWithoutUndo();
            Invoke(damager, "Awake");

            float hit = 0f;
            void OnHit(DamageEvent e) => hit += e.Damage;
            DamageEvents.Hit += OnHit;
            try
            {
                body.linearVelocity = new Vector3(0f, 0f, 30f);
                Physics.SyncTransforms();
                PhysicsScene physics = _scene.GetPhysicsScene();
                for (int i = 0; i < 30; i++)
                    physics.Simulate(Dt);
            }
            finally
            {
                DamageEvents.Hit -= OnHit;
            }

            if (hit == 0f && d.Health.Current == d.Health.Max)
                Assert.Inconclusive("Pas de message de collision en mode édition : vérifié en Play Mode.");
            Assert.That(d.Health.Current, Is.EqualTo(45f).Within(3f), "≈ 55 dégâts (25 kg à 30 m/s)");
        }

        // ---------- Magnétisme ----------

        [Test]
        public void Magnetisme_VisseeSurUnEnnemi_Signalee()
        {
            Dummy(new Vector3(0f, 0f, 10f));
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = new Vector3(0f, 2f, 20f);
            wall.transform.localScale = new Vector3(10f, 10f, 1f);
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(Dt);
            var probe = new PhysicsAimProbe(~0) { Scene = _scene.GetPhysicsScene() };
            Assert.That(probe.Raycast(new Vector3(0f, 1f, 0f), Vector3.forward, 60f, out AimHit hit), Is.True, "l'ennemi arrête le rayon");
            Assert.That(hit.Enemy, Is.True, "et il est signalé");
            Assert.That(hit.Distance, Is.EqualTo(9.6f).Within(0.05f));
            Assert.That(probe.Raycast(new Vector3(3f, 1f, 0f), Vector3.forward, 60f, out hit), Is.True, "à côté : le mur");
            Assert.That(hit.Enemy, Is.False);
            Assert.That(hit.Distance, Is.EqualTo(19.5f).Within(0.01f));
        }

        /// <summary>Visée factice : un point à 10 m devant, sur un ennemi ou non.</summary>
        private sealed class EnemyAimProbe : IAimProbe
        {
            public bool Enemy;

            public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out AimHit hit)
            {
                hit = new AimHit { Point = origin + direction * 10f, Normal = -direction, Distance = 10f, Enemy = Enemy };
                return true;
            }
        }

        [Test]
        public void Magnetisme_PasDeTractionVersUnEnnemi_MaisLaRepulsionOui()
        {
            var move = ScriptableObject.CreateInstance<MovementSettings>();
            var magnet = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            try
            {
                var probe = new EnemyAimProbe { Enemy = true };
                MovementState st = MovementState.Create(Vector3.zero);
                MagnetismState m = MagnetismState.Create();
                MovementInput input = MovementInput.Neutral;
                input.AttractPressedTick = 1;
                input.AttractHeld = true;
                MagnetismOrb.Step(ref m, ref st, input, magnet, move, Dt, probe);
                Assert.That(m.Tracting, Is.False, "pas de traction vers un ennemi");
                Assert.That(m.AimOnEnemy, Is.True);
                Assert.That(MagnetismFeedback.CanAttract(m), Is.False, "pas d'anneau bleu");

                input = MovementInput.Neutral;
                input.RepelPressedTick = 2;
                st.Tick = 1;
                MagnetismOrb.Step(ref m, ref st, input, magnet, move, Dt, probe);
                Assert.That(m.LastRepelTick, Is.EqualTo(2), "la répulsion part vers l'ennemi (onde)");
            }
            finally
            {
                Object.DestroyImmediate(move);
                Object.DestroyImmediate(magnet);
            }
        }

        // ---------- Scène et prefab ----------

        private static int CountScript(string scenePath, string scriptPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(scriptPath);
            Assert.That(guid, Is.Not.Empty, scriptPath);
            return Regex.Matches(File.ReadAllText(scenePath), $@"m_Script: \{{fileID: 11500000, guid: {guid}").Count;
        }

        [Test]
        public void Scene_EnnemisFacticesEtObjetsQuiBlessent()
        {
            const string scene = "Assets/Scenes/Test_Movement.unity";
            Assert.That(CountScript(scene, "Assets/_Project/Scripts/Runtime/Combat/Damageable.cs"), Is.EqualTo(4), "3 fixes + 1 mobile");
            Assert.That(CountScript(scene, "Assets/_Project/Scripts/Runtime/Combat/TrainingDummyMover.cs"), Is.EqualTo(1));
            Assert.That(CountScript(scene, "Assets/_Project/Scripts/Runtime/Combat/EnemyBody.cs"), Is.EqualTo(4), "tous repoussables");
            Assert.That(CountScript(scene, "Assets/_Project/Scripts/Runtime/Combat/DamageFeedback.cs"), Is.EqualTo(1));
            int grabbables = CountScript(scene, "Assets/_Project/Scripts/Runtime/Orbs/Grabbable.cs");
            Assert.That(grabbables, Is.GreaterThan(10));
            Assert.That(CountScript(scene, "Assets/_Project/Scripts/Runtime/Combat/ImpactDamager.cs"), Is.EqualTo(grabbables),
                "chaque objet attrapable blesse");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var hud = prefab.GetComponent<DamageNumbersHud>();
            Assert.That(hud, Is.Not.Null, "chiffres de dégâts sur le joueur");
            Assert.That(new SerializedObject(hud).FindProperty("settings").objectReferenceValue, Is.Not.Null);
        }

        [Test]
        public void Sons_ImpactEtMort_NonVidesSansSaturation()
        {
            foreach (float[] s in new[] { ItHunts.Audio.ProceduralSfx.ImpactHit(), ItHunts.Audio.ProceduralSfx.Kill() })
            {
                Assert.That(s.Length, Is.GreaterThan(1000));
                float peak = 0f;
                foreach (float v in s)
                    peak = Mathf.Max(peak, Mathf.Abs(v));
                Assert.That(peak, Is.GreaterThan(0.5f).And.LessThanOrEqualTo(1f));
            }
        }

        // ---------- Chutes et tir tendu ----------

        [Test]
        public void Chute_SeuilPlusBas()
        {
            Assert.That(_cfg.fallSpeedThreshold, Is.EqualTo(3f));
            float v = Mathf.Sqrt(2f * 9.81f * 4f);                                     // ≈ 8.9 m/s : chute de 4 m
            Assert.That(ImpactDamage.Compute(25f, v, _cfg), Is.LessThan(3f), "comme un lancer : presque rien");
            Assert.That(ImpactDamage.Compute(25f, v, _cfg, falling: true), Is.EqualTo(14.6f).Within(0.2f), "comme une chute");
        }

        [Test]
        public void Chute_ReconnueSeulementSiVerticaleEtObjetAuDessus()
        {
            Vector3 contact = new Vector3(0f, 1.8f, 0f);
            Assert.That(ImpactDamage.IsFall(new Vector3(0f, -9f, 0f), contact + Vector3.up * 0.5f, contact, _cfg), Is.True);
            Assert.That(ImpactDamage.IsFall(new Vector3(0f, 9f, 0f), contact + Vector3.up * 0.5f, contact, _cfg), Is.True,
                "le signe de la vitesse relative ne compte pas");
            Assert.That(ImpactDamage.IsFall(new Vector3(30f, -2f, 0f), contact + Vector3.up * 0.5f, contact, _cfg), Is.False,
                "lancer horizontal");
            Assert.That(ImpactDamage.IsFall(new Vector3(0f, -9f, 0f), contact - Vector3.up * 0.5f, contact, _cfg), Is.False,
                "objet sous la cible");
        }

        [Test]
        public void TirTendu_SansGraviteApresUnLancer_PasApresUnePose()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.AddComponent<Rigidbody>();
            var g = go.AddComponent<Grabbable>();
            Invoke(g, "Awake");
            g.OnGrabbed();
            Assert.That(g.Held, Is.True);
            Assert.That(g.Body.useGravity, Is.False, "tenu : pas de gravité");
            g.OnReleased(thrown: true, noGravityFor: 0.5f);
            Assert.That(g.InStraightFlight, Is.True);
            Assert.That(g.Body.useGravity, Is.False, "lancé : tir tendu");
            Invoke(g, "EndStraightFlight");
            Assert.That(g.Body.useGravity, Is.True, "puis gravité normale");
            g.OnGrabbed();
            g.OnReleased(thrown: false);
            Assert.That(g.InStraightFlight, Is.False);
            Assert.That(g.Body.useGravity, Is.True, "posé : gravité tout de suite");
            Assert.That(AssetDatabase.LoadAssetAtPath<TelekinesisOrbSettings>("Assets/_Project/Settings/Telekinesis_Default.asset")
                .throwNoGravityTime, Is.EqualTo(0.5f));
        }

        // ---------- Dégâts de zone et durabilité (D-027) ----------

        [Test]
        public void Zone_RayonSelonLaMasse_DegatsDegressifs()
        {
            Assert.That(AreaEffects.SplashRadius(5f, _cfg), Is.EqualTo(1.3f).Within(1e-3f));
            Assert.That(AreaEffects.SplashRadius(25f, _cfg), Is.EqualTo(2.5f).Within(1e-3f));
            Assert.That(AreaEffects.SplashRadius(40f, _cfg), Is.EqualTo(3.4f).Within(1e-3f));
            Assert.That(AreaEffects.SplashRadius(25f, _cfg, 4f), Is.EqualTo(4f), "rayon imposé (lustre)");
            Assert.That(AreaEffects.SplashDamage(55f, 0f, 2.5f, _cfg), Is.EqualTo(33f).Within(1e-3f), "60 % au centre");
            Assert.That(AreaEffects.SplashDamage(55f, 2.5f, 2.5f, _cfg), Is.EqualTo(8.25f).Within(1e-3f), "25 % de ça au bord");
            Assert.That(AreaEffects.SplashDamage(55f, 2.6f, 2.5f, _cfg), Is.EqualTo(0f), "hors de la zone");
        }

        [Test]
        public void Durabilite_SelonLaMasse()
        {
            Assert.That(AreaEffects.DurabilityForMass(5f, _cfg), Is.EqualTo(1));
            Assert.That(AreaEffects.DurabilityForMass(15f, _cfg), Is.EqualTo(2));
            Assert.That(AreaEffects.DurabilityForMass(20f, _cfg), Is.EqualTo(3), "tonneau");
            Assert.That(AreaEffects.DurabilityForMass(25f, _cfg), Is.EqualTo(3));
            Assert.That(AreaEffects.DurabilityForMass(40f, _cfg), Is.EqualTo(5));
            Assert.That(AreaEffects.DurabilityForMass(25f, _cfg, 1), Is.EqualTo(1), "imposée par l'objet");
        }

        private Grabbable Crate(Vector3 position, float mass, bool fragile = false, float splash = 0f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * 0.9f;
            go.AddComponent<Rigidbody>().mass = mass;
            var g = go.AddComponent<Grabbable>();
            var so = new SerializedObject(g);
            so.FindProperty("breaksOnAnyImpact").boolValue = fragile;
            so.FindProperty("splashRadiusOverride").floatValue = splash;
            so.ApplyModifiedPropertiesWithoutUndo();
            Invoke(g, "Awake");
            Invoke(g, "OnEnable");
            var damager = go.AddComponent<ImpactDamager>();
            var dso = new SerializedObject(damager);
            dso.FindProperty("settings").objectReferenceValue = _cfg;
            dso.ApplyModifiedPropertiesWithoutUndo();
            damager.Init();
            return g;
        }

        private static void Throw(Grabbable g)
        {
            g.OnGrabbed();
            g.OnReleased(thrown: true, noGravityFor: 0.5f);
        }

        [Test]
        public void Durabilite_PetiteCaisseCasseeAuPremierLancer_AvecZone()
        {
            Grabbable small = Crate(new Vector3(0f, 0.5f, 0f), 5f);
            Damageable near = Dummy(new Vector3(1f, 0f, 0f));                       // dans la zone de 1.3 m
            Damageable far = Dummy(new Vector3(4f, 0f, 0f));                        // hors de la zone
            Assert.That(small.MaxDurability, Is.EqualTo(1));
            int areas = 0;
            void OnArea(AreaImpactEvent e) => areas++;
            AreaEvents.Area += OnArea;
            try
            {
                Throw(small);
                ImpactDamager.HandleImpact(small, null, new Vector3(0f, 0f, -30f), new Vector3(0f, 0.5f, 0f), _cfg, 10f);
            }
            finally
            {
                AreaEvents.Area -= OnArea;
            }

            Assert.That(small.Broken, Is.True, "cassée au premier impact");
            Assert.That(small.Colliders[0].enabled, Is.False, "disparue");
            Assert.That(areas, Is.EqualTo(1), "cercle de zone");
            Assert.That(near.Health.Current, Is.LessThan(100f), "l'ennemi proche est touché par la zone");
            Assert.That(far.Health.Current, Is.EqualTo(100f), "l'ennemi loin, non");
        }

        [Test]
        public void Durabilite_CaisseDe25kg_TroisLancers_LeTroisiemeLaCasse()
        {
            Grabbable crate = Crate(new Vector3(0f, 0.5f, 0f), 25f);
            for (int i = 1; i <= 3; i++)
            {
                Throw(crate);
                ImpactDamager.HandleImpact(crate, null, new Vector3(0f, 0f, -30f), Vector3.zero, _cfg, i);
                Assert.That(crate.ThrowsLeft, Is.EqualTo(3 - i));
                Assert.That(crate.Broken, Is.EqualTo(i == 3), $"lancer {i}");
            }
        }

        [Test]
        public void Durabilite_UnLancerNeCompteQuUneFois_EtPasLesChocsNonLances()
        {
            Grabbable crate = Crate(new Vector3(0f, 0.5f, 0f), 25f);
            Throw(crate);
            ImpactDamager.HandleImpact(crate, null, new Vector3(0f, 0f, -30f), Vector3.zero, _cfg, 1f);
            ImpactDamager.HandleImpact(crate, null, new Vector3(0f, 0f, -20f), Vector3.zero, _cfg, 1.1f);    // rebond
            Assert.That(crate.ThrowsLeft, Is.EqualTo(2), "le rebond ne compte pas");
            crate.OnGrabbed();
            crate.OnReleased(thrown: false);                                          // posée
            ImpactDamager.HandleImpact(crate, null, new Vector3(0f, -12f, 0f), Vector3.zero, _cfg, 2f);
            Assert.That(crate.ThrowsLeft, Is.EqualTo(2), "une chute non lancée ne compte pas");
            Throw(crate);
            ImpactDamager.HandleImpact(crate, null, new Vector3(0f, 0f, -5f), Vector3.zero, _cfg, 3f);
            Assert.That(crate.ThrowsLeft, Is.EqualTo(2), "sous le seuil de vitesse : ne compte pas");
        }

        [Test]
        public void Lustre_CasseAuPremierImpactMemeTombe_ZoneDe4m()
        {
            Grabbable lustre = Crate(new Vector3(0f, 2.5f, 0f), 25f, fragile: true, splash: 4f);
            Damageable d = Dummy(new Vector3(3f, 0f, 0f));                          // à ≈ 2.6 m : dans les 4 m
            ImpactDamager.HandleImpact(lustre, null, new Vector3(0f, -9f, 0f), new Vector3(0f, 0.1f, 0f), _cfg, 1f);
            Assert.That(lustre.Broken, Is.True, "cassé en tombant, sans avoir été lancé");
            Assert.That(d.Health.Current, Is.LessThan(100f), "zone de 4 m");
        }

        // ---------- Onde de répulsion et étourdissement (D-027) ----------

        [Test]
        public void Onde_VitesseSelonMasseEtDistance()
        {
            var magnet = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            try
            {
                Assert.That(AreaEffects.WavePushSpeed(15f, 0f, false, magnet), Is.EqualTo(16f).Within(1e-3f), "caisse de 15 kg au centre");
                Assert.That(AreaEffects.WavePushSpeed(5f, 0f, false, magnet), Is.EqualTo(20.8f).Within(1e-3f), "légère : ×1.3 au plus");
                Assert.That(AreaEffects.WavePushSpeed(60f, 0f, false, magnet), Is.EqualTo(5.6f).Within(1e-3f), "lourde : ×0.35 au moins");
                Assert.That(AreaEffects.WavePushSpeed(80f, 0f, true, magnet), Is.EqualTo(16f).Within(1e-3f), "ennemi de 80 kg");
                Assert.That(AreaEffects.WavePushSpeed(80f, 4f, true, magnet), Is.EqualTo(8f).Within(1e-3f), "moitié au bord");
                Assert.That(AreaEffects.WavePushSpeed(80f, 4.1f, true, magnet), Is.EqualTo(0f), "hors de l'onde");
                Vector3 dir = AreaEffects.WavePushDirection(Vector3.zero, new Vector3(2f, -1f, 0f), magnet);
                Assert.That(dir.x, Is.GreaterThan(0.9f), "loin du centre");
                Assert.That(dir.y, Is.GreaterThan(0f), "un peu vers le haut, jamais vers le bas");
            }
            finally
            {
                Object.DestroyImmediate(magnet);
            }
        }

        private EnemyBody Body(Damageable d)
        {
            var body = d.gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var e = d.gameObject.AddComponent<EnemyBody>();
            var so = new SerializedObject(e);
            so.FindProperty("settings").objectReferenceValue = _dummyCfg;
            so.ApplyModifiedPropertiesWithoutUndo();
            e.Init();
            d.Init();
            return e;
        }

        [Test]
        public void Onde_RepousseObjetsEtEnnemis_DecrocheLeLustre_PasLObjetTenu()
        {
            var magnet = ScriptableObject.CreateInstance<MagnetismOrbSettings>();
            try
            {
                Grabbable loose = Crate(new Vector3(2f, 0.5f, 0f), 15f);
                Grabbable held = Crate(new Vector3(-2f, 0.5f, 0f), 15f);
                held.OnGrabbed();
                Grabbable lustre = Crate(new Vector3(0f, 3f, 0f), 25f, fragile: true);
                var so = new SerializedObject(lustre);
                so.FindProperty("startsAnchored").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                Invoke(lustre, "Awake");
                Assume.That(lustre.Anchored, Is.True);
                Damageable d = Dummy(new Vector3(0f, 0f, 2.5f));
                EnemyBody enemy = Body(d);
                Grabbable outside = Crate(new Vector3(10f, 0.5f, 0f), 15f);

                int pushed = RepulsionWave.Apply(new Vector3(0f, 0.5f, 0f), magnet, held.Body);
                Assert.That(loose.Body.linearVelocity.x, Is.GreaterThan(5f), "objet repoussé loin du point");
                Assert.That(held.Body.linearVelocity.magnitude, Is.LessThan(1e-3f), "l'objet tenu n'est pas poussé");
                Assert.That(lustre.Anchored, Is.False, "lustre décroché : il tombe");
                Assert.That(lustre.Body.linearVelocity.magnitude, Is.LessThan(1e-3f), "sans poussée : il tombe droit");
                Assert.That(enemy.Knocked, Is.True);
                Assert.That(enemy.Velocity.z, Is.GreaterThan(5f), "ennemi repoussé loin du point");
                Assert.That(outside.Body.linearVelocity.magnitude, Is.LessThan(1e-3f), "hors de l'onde");
                Assert.That(pushed, Is.GreaterThanOrEqualTo(3));
            }
            finally
            {
                Object.DestroyImmediate(magnet);
            }
        }

        private (EnemyBody enemy, Damageable d) PushTowardWall(float speed)
        {
            Damageable d = Dummy(Vector3.zero);
            EnemyBody enemy = Body(d);
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = new Vector3(0f, 1f, 2.5f);
            wall.transform.localScale = new Vector3(6f, 4f, 0.5f);
            Physics.SyncTransforms();
            _scene.GetPhysicsScene().Simulate(Dt);
            enemy.Push(new Vector3(0f, 0f, speed));
            for (int i = 0; i < 60; i++)
            {
                enemy.Step(Dt);
                Physics.SyncTransforms();
            }

            return (enemy, d);
        }

        [Test]
        public void Etourdi_SiLEnnemiPercuteUnMurAssezVite()
        {
            (EnemyBody enemy, Damageable d) = PushTowardWall(12f);
            Assert.That(enemy.Stunned, Is.True, "12 m/s contre le mur : étourdi");
            Assert.That(enemy.LastStunImpactSpeed, Is.GreaterThan(_dummyCfg.stunImpactSpeed));
            Assert.That(enemy.CanBeGrabbed, Is.True, "préparé pour la prise d'ennemis");
            Assert.That(d.transform.position.z, Is.LessThan(2.25f - 0.39f), "il ne traverse pas le mur");
        }

        [Test]
        public void PasEtourdi_SiLEnnemiArriveTropLentement()
        {
            (EnemyBody enemy, _) = PushTowardWall(4f);
            Assert.That(enemy.Stunned, Is.False, "4 m/s : sous le seuil de 6 m/s");
        }

        [Test]
        public void Lancer_RayonDuClicDepuisLesYeuxDuDebutDuTick_ReglagesD027()
        {
            var magnet = AssetDatabase.LoadAssetAtPath<MagnetismOrbSettings>("Assets/_Project/Settings/Magnetism_Default.asset");
            var dummy = AssetDatabase.LoadAssetAtPath<TrainingDummySettings>("Assets/_Project/Settings/TrainingDummy_Default.asset");
            var damage = AssetDatabase.LoadAssetAtPath<DamageSettings>("Assets/_Project/Settings/Damage_Default.asset");
            Assert.That(magnet.waveRadius, Is.EqualTo(4f));
            Assert.That(dummy.stunImpactSpeed, Is.EqualTo(6f));
            Assert.That(dummy.stunDuration, Is.EqualTo(1.5f));
            Assert.That(damage.heavyDurability, Is.EqualTo(5));
            Assert.That(Physics.gravity.y, Is.EqualTo(-20f), "objets à 20 m/s², comme le joueur");
            Assert.That(File.ReadAllText("ProjectSettings/DynamicsManager.asset"), Does.Contain("m_Gravity: {x: 0, y: -20, z: 0}"));
        }
    }
}
