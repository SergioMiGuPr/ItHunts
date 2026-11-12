using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ItHunts.Tests
{
    /// <summary>
    /// Artefacts (D-032) : tirage par la seed sans doublon (2 à 4 joueurs simulés), barillet (6 coups, cadence, rechargement
    /// automatique et sur R, inactif quand l'orbe tient un objet, tir et lancer jamais au même tick), tir instantané (portée,
    /// traqueur insensible), tir ami (jamais soi-même, les alliés seulement si activé), réglages et prefab.
    /// </summary>
    public class ArtifactTests
    {
        private const float Dt = 1f / 60f;
        private BarrelSettings _barrel;
        private Scene _scene;
        private readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _barrel = ScriptableObject.CreateInstance<BarrelSettings>();
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
                foreach (var p in go.GetComponentsInChildren<PlayerHealth>())
                    Invoke(p, "OnDisable");
            }

            EditorSceneManager.ClosePreviewScene(_scene);
            Object.DestroyImmediate(_barrel);
            foreach (Object o in _assets)
                Object.DestroyImmediate(o);
            _assets.Clear();
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var a = ScriptableObject.CreateInstance<T>();
            _assets.Add(a);
            return a;
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        // ---------- Tirage ----------

        [Test]
        public void Tirage_MemeSeedMemeResultat_SansDoublonTantQueLePoolSuffit()
        {
            for (int seed = 1; seed <= 200; seed++)
            {
                int[] a = ArtifactDraw.Sequence(seed, 4, 4);
                CollectionAssert.AreEqual(a, ArtifactDraw.Sequence(seed, 4, 4), "même seed, même tirage");
                Assert.That(a.Distinct().Count(), Is.EqualTo(4), $"seed {seed} : 4 joueurs, 4 artefacts, aucun doublon");
                for (int players = 2; players <= 4; players++)
                    Assert.That(ArtifactDraw.Sequence(seed, 6, players).Distinct().Count(), Is.EqualTo(players), "pool de 6");
            }

            // L'artefact d'un joueur ne dépend pas du nombre de joueurs (rejoindre en cours de route).
            for (int seed = 1; seed <= 50; seed++)
            for (int i = 0; i < 4; i++)
                Assert.That(ArtifactDraw.ForPlayer(seed, 5, i), Is.EqualTo(ArtifactDraw.Sequence(seed, 5, 4)[i]));

            // Les seeds donnent des tirages différents (le premier joueur ne reçoit pas toujours le même).
            var first = new HashSet<int>();
            for (int seed = 1; seed <= 50; seed++)
                first.Add(ArtifactDraw.ForPlayer(seed, 4, 0));
            Assert.That(first.Count, Is.EqualTo(4), "chaque artefact sort en premier pour au moins une seed");
        }

        [Test]
        public void Tirage_PoolTropPetit_DoublonsRepartisAuMieux_PoolVide()
        {
            // Pool d'un seul artefact : 2, 3 ou 4 joueurs ont tous le barillet.
            for (int players = 2; players <= 4; players++)
                CollectionAssert.AreEqual(Enumerable.Repeat(0, players).ToArray(), ArtifactDraw.Sequence(42, 1, players));

            // Pool de 2 pour 3 ou 4 joueurs : chaque artefact au plus 2 fois.
            for (int seed = 1; seed <= 100; seed++)
            {
                Assert.That(ArtifactDraw.Sequence(seed, 2, 3).GroupBy(x => x).Max(g => g.Count()), Is.EqualTo(2));
                Assert.That(ArtifactDraw.Sequence(seed, 2, 4).GroupBy(x => x).All(g => g.Count() == 2), Is.True);
                Assert.That(ArtifactDraw.Sequence(seed, 3, 4).GroupBy(x => x).Max(g => g.Count()), Is.EqualTo(2));
            }

            Assert.That(ArtifactDraw.Sequence(1, 0, 4), Is.Empty);
            Assert.That(ArtifactDraw.ForPlayer(1, 0, 0), Is.EqualTo(-1));
        }

        [Test]
        public void EtatDeLaRun_QuatreJoueursSimules_PoolDUnSeulArtefact()
        {
            var pool = Asset<ArtifactPool>();
            pool.artifacts.Add(_barrel);
            var go = new GameObject("Run");
            SceneManager.MoveGameObjectToScene(go, _scene);
            var run = go.AddComponent<RunState>();
            run.Configure(pool, 1234);
            for (int i = 0; i < 4; i++)
                Assert.That(run.ArtifactFor(i), Is.SameAs(_barrel), $"joueur {i + 1}");
            pool.artifacts.Clear();
            Assert.That(run.ArtifactFor(0), Is.Null, "pool vide : pas d'artefact");
        }

        // ---------- Barillet ----------

        private sealed class Gun
        {
            public BarrelState B;
            public int Tick;
            public int FirePress = MovementState.NeverTick, ReloadPress = MovementState.NeverTick;
            public bool Held;
            public int Shots;

            public Gun(BarrelSettings cfg) => B = BarrelState.Create(cfg.rounds);

            public BarrelStep Step(BarrelSettings cfg, bool heldAtStart = false)
            {
                Tick++;
                BarrelStep s = Barrel.Step(ref B, Tick, FirePress, ReloadPress, heldAtStart || Held, Held, cfg, Dt);
                if (s.Fired)
                    Shots++;
                return s;
            }

            public void Click() => FirePress = Tick + 1;
        }

        [Test]
        public void Barillet_SixCoups_Cadence025_RechargementAuto12s_TempsPourTuer()
        {
            Assert.That(_barrel.rounds, Is.EqualTo(6));
            Assert.That(_barrel.damage, Is.EqualTo(12f));
            var gun = new Gun(_barrel);
            var shotTicks = new List<int>();
            // Clic à chaque tick : la cadence limite à un tir toutes les 15 ticks (0.25 s).
            for (int i = 0; i < 400 && shotTicks.Count < 9; i++)
            {
                gun.Click();
                if (gun.Step(_barrel).Fired)
                    shotTicks.Add(gun.Tick);
                if (shotTicks.Count == 6)
                    Assert.That(gun.B.Reloading || gun.B.Rounds > 0, Is.True);
            }

            Assert.That(shotTicks.Count, Is.EqualTo(9), "9 balles de 12 pour 100 PV");
            for (int i = 1; i < 6; i++)
                Assert.That(shotTicks[i] - shotTicks[i - 1], Is.EqualTo(15), "0.25 s entre deux tirs");
            Assert.That(shotTicks[6] - shotTicks[5], Is.EqualTo(72), "rechargement automatique de 1.2 s après le 6e coup");
            Assert.That(shotTicks[8] - shotTicks[0], Is.EqualTo(5 * 15 + 72 + 2 * 15), "temps pour tuer : 2.95 s");
            Assert.That(9 * _barrel.damage, Is.GreaterThanOrEqualTo(100f));
            Assert.That(8 * _barrel.damage, Is.LessThan(100f));
        }

        [Test]
        public void Barillet_RSeulementSIlManqueUneMunition_PasDeTirPendantLeRechargement()
        {
            var gun = new Gun(_barrel);
            gun.ReloadPress = 1;
            Assert.That(gun.Step(_barrel).ReloadStarted, Is.False, "plein : R ne fait rien");

            gun.Click();
            Assert.That(gun.Step(_barrel).Fired, Is.True);
            Assert.That(gun.B.Rounds, Is.EqualTo(5));
            gun.ReloadPress = gun.Tick + 1;
            Assert.That(gun.Step(_barrel).ReloadStarted, Is.True, "5/6 : R recharge");
            for (int i = 0; i < 30; i++)
            {
                gun.Click();
                Assert.That(gun.Step(_barrel).Fired, Is.False, "pas de tir pendant le rechargement");
            }

            for (int i = 0; i < 60 && gun.B.Reloading; i++)
                gun.Step(_barrel);
            Assert.That(gun.B.Rounds, Is.EqualTo(6));
            Assert.That(gun.Tick - gun.B.ReloadStartTick, Is.LessThan(1000));
        }

        [Test]
        public void Barillet_ClicUnPeuTropTot_PartDesQuePossible_TropTot_Perdu()
        {
            var gun = new Gun(_barrel);
            gun.Click();
            Assert.That(gun.Step(_barrel).Fired, Is.True);
            for (int i = 0; i < 8; i++)
                gun.Step(_barrel);                                       // 9 ticks après le tir
            gun.Click();                                                 // 6 ticks trop tôt (< 0.12 s de mémorisation)
            int fired = 0;
            for (int i = 0; i < 10; i++)
                if (gun.Step(_barrel).Fired)
                {
                    fired++;
                    Assert.That(gun.Tick - gun.B.LastShotTick, Is.EqualTo(0));
                }

            Assert.That(fired, Is.EqualTo(1), "le clic mémorisé part à la fin de la cadence");

            gun.Click();
            gun.Step(_barrel);                                           // juste après le tir : 14 ticks trop tôt
            for (int i = 0; i < 30; i++)
                Assert.That(gun.Step(_barrel).Fired, Is.False, "trop tôt et plus mémorisé : perdu");
        }

        [Test]
        public void Barillet_ObjetTenu_Inactif_ClicDuLancerJamaisUnTir_RechargementAnnule()
        {
            var gun = new Gun(_barrel);
            // Le clic qui lance l'objet (l'orbe tenait un objet au début du tick) : aucun tir, ni ce tick ni le suivant.
            gun.Click();
            Assert.That(gun.Step(_barrel, heldAtStart: true).Fired, Is.False, "tir et lancer jamais au même tick");
            for (int i = 0; i < 10; i++)
                Assert.That(gun.Step(_barrel).Fired, Is.False, "le clic du lancer est consommé");

            // Objet tenu : inactif, pas de tir ni de rechargement qui démarre.
            gun.Held = true;
            gun.Click();
            gun.ReloadPress = gun.Tick + 1;
            BarrelStep s = gun.Step(_barrel);
            Assert.That(s.Fired || s.ReloadStarted, Is.False);
            Assert.That(gun.B.Inactive, Is.True);

            // Barillet vide pendant qu'on tient un objet : pas de rechargement automatique avant le relâchement.
            gun.Held = false;
            for (int k = 0; k < 6; k++)
            {
                for (int i = 0; i < 15; i++)
                    gun.Step(_barrel);
                gun.Click();
                gun.Step(_barrel);
            }

            Assert.That(gun.B.Rounds, Is.EqualTo(0));
            Assert.That(gun.B.Reloading, Is.True, "vide : rechargement automatique");
            gun.Held = true;
            Assert.That(gun.Step(_barrel).ReloadCancelled, Is.True, "prendre un objet annule le rechargement");
            for (int i = 0; i < 100; i++)
                gun.Step(_barrel);
            Assert.That(gun.B.Reloading || gun.B.Rounds > 0, Is.False, "tenu : il ne redémarre pas");
            gun.Held = false;
            Assert.That(gun.Step(_barrel).ReloadStarted, Is.True, "relâché : il reprend à zéro");

            // Réglage : le rechargement continue pendant la prise.
            _barrel.grabCancelsReload = false;
            gun.Held = true;
            for (int i = 0; i < 80; i++)
                gun.Step(_barrel);
            Assert.That(gun.B.Rounds, Is.EqualTo(6), "grabCancelsReload décoché : il se termine pendant la prise");
        }

        // ---------- Tir instantané ----------

        private (Damageable d, EnemyBody body) Dummy(Vector3 feet, TrainingDummySettings cfg, bool stalker = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = feet + Vector3.up * 0.9f;
            go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            var body = go.AddComponent<EnemyBody>();
            var bso = new SerializedObject(body);
            bso.FindProperty("settings").objectReferenceValue = cfg;
            bso.ApplyModifiedPropertiesWithoutUndo();
            var d = go.AddComponent<Damageable>();
            var dso = new SerializedObject(d);
            dso.FindProperty("settings").objectReferenceValue = cfg;
            dso.FindProperty("immuneToOrbs").boolValue = stalker;
            dso.ApplyModifiedPropertiesWithoutUndo();
            body.Init();
            d.Init();
            Invoke(d, "OnEnable");
            return (d, body);
        }

        private PlayerHealth Player(Vector3 feet, CoopSettings coop)
        {
            var go = new GameObject("Joueur (test)");
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = feet;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            // Un CharacterController créé dans une scène de prévisualisation reste dans la physique de la scène principale
            // (limite d'Unity) : capsule ordinaire pour que les rayons du test le touchent. En jeu, le rayon touche le
            // CharacterController (vérifié en Play Mode).
            var hitbox = new GameObject("Capsule (test)");
            SceneManager.MoveGameObjectToScene(hitbox, _scene);
            hitbox.transform.SetParent(go.transform, false);
            var cap = hitbox.AddComponent<CapsuleCollider>();
            cap.height = 1.8f;
            cap.radius = 0.4f;
            cap.center = new Vector3(0f, 0.9f, 0f);
            var h = go.AddComponent<PlayerHealth>();
            h.Configure(Asset<PlayerHealthSettings>(), coop);
            Invoke(h, "OnEnable");
            return h;
        }

        [Test]
        public void Tir_InstantaneDepuisLesYeux_Portee60_MurBloque_TraqueurInsensible_TireurIgnore()
        {
            var dummyCfg = Asset<TrainingDummySettings>();
            (Damageable near, _) = Dummy(new Vector3(0f, 0f, 30f), dummyCfg);
            (Damageable far, _) = Dummy(new Vector3(5f, 0f, 61f), dummyCfg);
            (Damageable stalker, _) = Dummy(new Vector3(-5f, 0f, 20f), dummyCfg, stalker: true);
            PlayerHealth shooter = Player(Vector3.zero, Asset<CoopSettings>());
            Physics.SyncTransforms();
            PhysicsScene physics = _scene.GetPhysicsScene();
            Vector3 eye = new Vector3(0f, 1.6f, 0f);
            int id = shooter.PlayerId;

            // Depuis l'intérieur de sa propre capsule : le tireur est ignoré ; 12 dégâts à 30 m, exactement au viseur.
            Vector3 toNear = near.Center - eye;
            BarrelShot s = BarrelShooter.Fire(eye, toNear, _barrel, shooter.transform, id, 0f, 1, physics);
            Assert.That(s.Kind, Is.EqualTo(BarrelHitKind.Ennemi));
            Assert.That(near.Health.Current, Is.EqualTo(88f).Within(1e-3f));
            Assert.That(Vector3.Distance(s.Point, eye + toNear.normalized * s.Distance), Is.LessThan(1e-3f), "sans dispersion ni chute");

            // Au-delà de 60 m : rien.
            Vector3 toFar = far.Center - eye;
            Assert.That(toFar.magnitude, Is.GreaterThan(60f));
            s = BarrelShooter.Fire(eye, toFar, _barrel, shooter.transform, id, 0f, 2, physics);
            Assert.That(s.Kind, Is.EqualTo(BarrelHitKind.Rien));
            Assert.That(far.Health.Current, Is.EqualTo(far.Health.Max));
            Assert.That(s.Distance, Is.EqualTo(60f));

            // Traqueur : la balle l'atteint mais ne le blesse pas.
            s = BarrelShooter.Fire(eye, stalker.Center - eye, _barrel, shooter.transform, id, 0f, 3, physics);
            Assert.That(s.Kind, Is.EqualTo(BarrelHitKind.Insensible));
            Assert.That(stalker.Health.Current, Is.EqualTo(stalker.Health.Max));

            // Un mur entre les deux : la balle s'arrête au mur.
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = new Vector3(0f, 1.5f, 15f);
            wall.transform.localScale = new Vector3(4f, 4f, 0.5f);
            Physics.SyncTransforms();
            s = BarrelShooter.Fire(eye, near.Center - eye, _barrel, shooter.transform, id, 0f, 4, physics);
            Assert.That(s.Kind, Is.EqualTo(BarrelHitKind.Surface));
            Assert.That(near.Health.Current, Is.EqualTo(88f).Within(1e-3f));
        }

        // ---------- Tir ami ----------

        [Test]
        public void TirAmi_JamaisSoiMeme_AlliesSeulementSiActive_MondeJamais()
        {
            var coop = Asset<CoopSettings>();
            Assert.That(coop.friendlyFire, Is.False, "désactivé par défaut");
            Assert.That(FriendlyFire.Allows(1, 2, coop), Is.False);
            coop.friendlyFire = true;
            Assert.That(FriendlyFire.Allows(1, 2, coop), Is.True);
            Assert.That(FriendlyFire.Allows(1, 1, coop), Is.False, "jamais soi-même");
            Assert.That(FriendlyFire.Allows(0, 2, coop), Is.False, "un objet du monde ne blesse pas un joueur");
            Assert.That(FriendlyFire.Allows(1, 2, null), Is.False);
        }

        [Test]
        public void TirAmi_Balles()
        {
            var coop = Asset<CoopSettings>();
            PlayerHealth p1 = Player(Vector3.zero, coop);
            PlayerHealth p2 = Player(new Vector3(0f, 0f, 10f), coop);
            Physics.SyncTransforms();
            PhysicsScene physics = _scene.GetPhysicsScene();
            Vector3 eye = new Vector3(0f, 1.6f, 0f);

            BarrelShot s = BarrelShooter.Fire(eye, p2.Center - eye, _barrel, p1.transform, p1.PlayerId, 0f, 1, physics);
            Assert.That(s.Kind, Is.EqualTo(BarrelHitKind.Joueur), "la balle s'arrête sur l'allié");
            Assert.That(p2.Health.Current, Is.EqualTo(100f), "tir ami désactivé : aucun dégât");

            coop.friendlyFire = true;
            BarrelShooter.Fire(eye, p2.Center - eye, _barrel, p1.transform, p1.PlayerId, 1f, 2, physics);
            Assert.That(p2.Health.Current, Is.EqualTo(88f).Within(1e-3f), "tir ami activé : 12 dégâts");
            Assert.That(p1.TryHurt(p1.PlayerId, 50f, Vector3.zero, 2f), Is.False, "jamais soi-même");
            Assert.That(p1.Health.Current, Is.EqualTo(100f));
        }

        [Test]
        public void TirAmi_ObjetsLancesEtRepousses_AuNomDuDernierJoueur()
        {
            var coop = Asset<CoopSettings>();
            var cfg = Asset<DamageSettings>();
            ImpactDamager.ResetCooldowns();
            PlayerHealth p1 = Player(Vector3.zero, coop);
            PlayerHealth p2 = Player(new Vector3(0f, 0f, 10f), coop);

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = new Vector3(0f, 1f, 8f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 15f;
            rb.useGravity = false;
            var crate = go.AddComponent<Grabbable>();
            Invoke(crate, "Awake");
            Invoke(crate, "OnEnable");
            var damager = go.AddComponent<ImpactDamager>();
            var so = new SerializedObject(damager);
            so.FindProperty("settings").objectReferenceValue = cfg;
            so.ApplyModifiedPropertiesWithoutUndo();
            damager.Init();

            // Lancé par P1 : sur P2, tir ami désactivé → rien ; activé → comme sur un ennemi (15 kg à 20 m/s : 18).
            crate.SetOwner(p1.PlayerId);
            crate.OnReleased(thrown: true);
            float t = 10f;
            ImpactDamager.HandleImpact(crate, null, new Vector3(0f, 0f, 20f), p2.Center, cfg, t, p2);
            Assert.That(p2.Health.Current, Is.EqualTo(100f));
            coop.friendlyFire = true;
            crate.OnReleased(thrown: true);
            Assert.That(ImpactDamager.HurtPlayer(crate, p2, 20f, false, p2.Center, cfg, t += 1f), Is.True);
            Assert.That(p2.Health.Current, Is.EqualTo(82f).Within(1e-3f));
            Assert.That(ImpactDamager.HurtPlayer(crate, p1, 20f, false, p1.Center, cfg, t += 1f), Is.False, "jamais son lanceur");

            // Repoussé par P1 : ×0.5 (9), puis inoffensif sous 6 m/s.
            ImpactDamager.ResetCooldowns();
            crate.OnGrabbed();
            crate.OnReleased(thrown: false);
            crate.MarkPushed(Time.time, p1.PlayerId);
            Assert.That(ImpactDamager.HurtPlayer(crate, p2, 20f, false, p2.Center, cfg, Time.time), Is.True);
            Assert.That(p2.Health.Current, Is.EqualTo(73f).Within(1e-3f));
            crate.Body.linearVelocity = new Vector3(0f, 0f, 5f);
            crate.TickPushDanger(cfg.pushedMinSpeed);
            Assert.That(ImpactDamager.HurtPlayer(crate, p2, 20f, false, p2.Center, cfg, Time.time + 0.5f), Is.False, "sous 6 m/s");
        }

        // ---------- Réglages, prefab, scène, rendu ----------

        [Test]
        public void Reglages_PrefabEtRendu()
        {
            var barrel = AssetDatabase.LoadAssetAtPath<BarrelSettings>("Assets/_Project/Settings/Barrel_Default.asset");
            Assert.That(barrel, Is.Not.Null);
            Assert.That(barrel.rounds, Is.EqualTo(6));
            Assert.That(barrel.damage, Is.EqualTo(12f));
            Assert.That(barrel.fireInterval, Is.EqualTo(0.25f));
            Assert.That(barrel.reloadTime, Is.EqualTo(1.2f));
            Assert.That(barrel.range, Is.EqualTo(60f));
            var pool = AssetDatabase.LoadAssetAtPath<ArtifactPool>("Assets/_Project/Settings/ArtifactPool_Default.asset");
            Assert.That(pool.artifacts, Does.Contain(barrel));
            var coop = AssetDatabase.LoadAssetAtPath<CoopSettings>("Assets/_Project/Settings/Coop_Default.asset");
            Assert.That(coop.friendlyFire, Is.False, "tir ami désactivé par défaut");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            Assert.That(prefab.GetComponent<ArtifactHolder>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerHealth>(), Is.Not.Null);
            Assert.That(prefab.GetComponentInChildren<Damageable>(true), Is.Null, "un joueur n'est pas une cible du monde");
            var view = prefab.GetComponent<BarrelView>();
            var chambers = (Renderer[])typeof(BarrelView).GetField("chambers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            Assert.That(chambers.Length, Is.EqualTo(6), "munitions lisibles sur le barillet");
            Transform left = prefab.GetComponentsInChildren<Transform>(true).First(t => t.name == "Main gauche");
            Assert.That(left.Find("Barillet (vue)"), Is.Not.Null, "le barillet est dans la main gauche");

            // Occlusion ambiante coupée pour les mains (l'ombre du décor derrière elles ne s'y dessine plus).
            foreach (string path in new[] { "Assets/Settings/PC_Renderer.asset", "Assets/Settings/Mobile_Renderer.asset" })
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                var clear = data.rendererFeatures.OfType<FirstPersonDepthClearFeature>().Single();
                Assert.That(clear.HandsReceiveAmbientOcclusion, Is.False, path);
            }
        }
    }
}
