using System.Collections.Generic;
using System.Reflection;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Level;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItHunts.Tests
{
    /// <summary>
    /// Lanterne de feu (D-065) et personnages (D-064) : cône (portée, angle), murs (le jet traverse les ennemis, pas le décor),
    /// une touche par ennemi et par jet, durée du jet et recharge, clic du lancer, traqueur exclu, volant tué en un coup, tir ami,
    /// personnages différents en coop (menu, joueur qui rejoint en jeu), artefact du personnage, assets et prefab.
    /// </summary>
    public class LanternTests
    {
        private const float Dt = 0.02f;
        private FireLanternSettings _cfg;
        private Scene _scene;
        private readonly List<Object> _assets = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _cfg = Asset<FireLanternSettings>();
            _scene = EditorSceneManager.NewPreviewScene();
            RunSession.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _scene.GetRootGameObjects())
            {
                foreach (var d in go.GetComponentsInChildren<Damageable>())
                    Invoke(d, "OnDisable");
                foreach (var p in go.GetComponentsInChildren<PlayerHealth>())
                    Invoke(p, "OnDisable");
            }

            EditorSceneManager.ClosePreviewScene(_scene);
            foreach (Object o in _assets)
                Object.DestroyImmediate(o);
            _assets.Clear();
            RunSession.ResetForTests();
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var a = ScriptableObject.CreateInstance<T>();
            _assets.Add(a);
            return a;
        }

        private static void Invoke(Object target, string method) =>
            target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);

        private Damageable Dummy(Vector3 feet, ScriptableObject cfg, bool stalker = false)
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
            return d;
        }

        private PlayerHealth Player(Vector3 feet, CoopSettings coop)
        {
            var go = new GameObject("Joueur (test)");
            SceneManager.MoveGameObjectToScene(go, _scene);
            go.transform.position = feet;
            // Capsule ordinaire (un CharacterController d'une scène de prévisualisation reste dans la physique principale).
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

        private GameObject Wall(Vector3 center, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(wall, _scene);
            wall.transform.position = center;
            wall.transform.localScale = size;
            return wall;
        }

        private int Sweep(Vector3 eye, Vector3 aim, Transform shooter, int id, HashSet<int> burned, float time = 0f) =>
            FireLanternJet.Sweep(eye, aim, _cfg, shooter, id, burned, time, 1, null, _scene.GetPhysicsScene());

        // ---------- Valeurs de départ ----------

        [Test]
        public void ValeursDeDepart_5m_40deg_06s_32PV_25s()
        {
            Assert.That(_cfg.range, Is.EqualTo(5f));
            Assert.That(_cfg.coneAngle, Is.EqualTo(40f));
            Assert.That(_cfg.HalfAngle, Is.EqualTo(20f));
            Assert.That(_cfg.jetDuration, Is.EqualTo(0.6f));
            Assert.That(_cfg.damage, Is.EqualTo(32f), "une goule de 60 PV en 2 jets");
            Assert.That(_cfg.cooldown, Is.EqualTo(2.5f));
            Assert.That(_cfg.Origin(Vector3.up * 1.6f, Vector3.forward).y, Is.EqualTo(1.15f).Within(1e-3f), "le cône part de la lanterne, sous les yeux");
        }

        // ---------- Cône ----------

        [Test]
        public void Cone_PorteeEtAngle_UneToucheParEnnemiEtParJet()
        {
            var dummyCfg = Asset<TrainingDummySettings>();
            PlayerHealth shooter = Player(Vector3.zero, Asset<CoopSettings>());
            Vector3 eye = new Vector3(0f, 0.9f, 0f);
            // Devant à 3 m ; à 15° du viseur à 4 m ; à 35° (hors du cône de 40°) ; à 7 m (hors de portée) ; derrière.
            Damageable front = Dummy(new Vector3(0f, 0f, 3f), dummyCfg);
            Damageable side = Dummy(Quaternion.Euler(0f, 15f, 0f) * new Vector3(0f, 0f, 4f), dummyCfg);
            Damageable wide = Dummy(Quaternion.Euler(0f, 35f, 0f) * new Vector3(0f, 0f, 4f), dummyCfg);
            Damageable far = Dummy(new Vector3(0f, 0f, 7f), dummyCfg);
            Damageable behind = Dummy(new Vector3(0f, 0f, -3f), dummyCfg);
            Physics.SyncTransforms();
            var burned = new HashSet<int>();

            int n = Sweep(eye, Vector3.forward, shooter.transform, shooter.PlayerId, burned);
            Assert.That(n, Is.EqualTo(2), "devant et à 15°");
            Assert.That(front.Health.Current, Is.EqualTo(front.Health.Max - _cfg.damage).Within(1e-3f));
            Assert.That(side.Health.Current, Is.EqualTo(side.Health.Max - _cfg.damage).Within(1e-3f));
            Assert.That(wide.Health.Current, Is.EqualTo(wide.Health.Max), "à 35° : hors du cône (20° de chaque côté)");
            Assert.That(far.Health.Current, Is.EqualTo(far.Health.Max), "à 7 m : hors de portée");
            Assert.That(behind.Health.Current, Is.EqualTo(behind.Health.Max));

            // Les ticks suivants du même jet ne retouchent pas les mêmes ennemis ; un nouveau jet (liste vidée) les retouche.
            Assert.That(Sweep(eye, Vector3.forward, shooter.transform, shooter.PlayerId, burned, 0.1f), Is.EqualTo(0));
            Assert.That(front.Health.Current, Is.EqualTo(front.Health.Max - _cfg.damage).Within(1e-3f));
            burned.Clear();
            Assert.That(Sweep(eye, Vector3.forward, shooter.transform, shooter.PlayerId, burned, 4f), Is.EqualTo(2));
            Assert.That(front.Health.Current, Is.EqualTo(front.Health.Max - 2f * _cfg.damage).Within(1e-3f));

            // Viser à 35° : l'ennemi « large » entre dans le cône.
            burned.Clear();
            Sweep(eye, Quaternion.Euler(0f, 35f, 0f) * Vector3.forward, shooter.transform, shooter.PlayerId, burned, 8f);
            Assert.That(wide.Health.Current, Is.EqualTo(wide.Health.Max - _cfg.damage).Within(1e-3f));
        }

        // ---------- Murs ----------

        [Test]
        public void Murs_ArretentLeJet_EnnemisEtObjetsNon()
        {
            var dummyCfg = Asset<TrainingDummySettings>();
            PlayerHealth shooter = Player(Vector3.zero, Asset<CoopSettings>());
            Vector3 eye = new Vector3(0f, 0.9f, 0f);
            // Couloir : trois goules en file à 1.5, 3 et 4.5 m ; elles se cachent l'une l'autre, le jet les traverse.
            Damageable a = Dummy(new Vector3(0f, 0f, 1.5f), dummyCfg);
            Damageable b = Dummy(new Vector3(0f, 0f, 3f), dummyCfg);
            Damageable c = Dummy(new Vector3(0f, 0f, 4.5f), dummyCfg);
            // Une caisse (Rigidbody) devant la deuxième n'arrête pas non plus le jet.
            GameObject crate = Wall(new Vector3(0f, 0.9f, 2.3f), new Vector3(0.6f, 0.6f, 0.6f));
            crate.AddComponent<Rigidbody>().isKinematic = true;
            // Ennemi derrière un mur (décor) à 3 m sur le côté : protégé.
            Damageable hidden = Dummy(new Vector3(1.2f, 0f, 3.5f), dummyCfg);
            Wall(new Vector3(0.8f, 1.5f, 2.2f), new Vector3(1f, 3f, 0.6f));
            Physics.SyncTransforms();

            int n = Sweep(eye, Vector3.forward, shooter.transform, shooter.PlayerId, new HashSet<int>());
            Assert.That(n, Is.EqualTo(3), "les trois goules du couloir");
            foreach (Damageable d in new[] { a, b, c })
                Assert.That(d.Health.Current, Is.LessThan(d.Health.Max));
            Assert.That(hidden.Health.Current, Is.EqualTo(hidden.Health.Max), "le mur arrête le jet");

            // Un mur plein en travers : plus personne derrière lui.
            Wall(new Vector3(0f, 1.5f, 0.8f), new Vector3(6f, 3f, 0.2f));
            Physics.SyncTransforms();
            Assert.That(Sweep(eye, Vector3.forward, shooter.transform, shooter.PlayerId, new HashSet<int>(), 5f), Is.EqualTo(0));
        }

        // ---------- Traqueur, volant, tir ami ----------

        [Test]
        public void Traqueur_PasTouche_Volant_TueEnUnCoup()
        {
            PlayerHealth shooter = Player(Vector3.zero, Asset<CoopSettings>());
            Damageable stalker = Dummy(new Vector3(-0.8f, 0f, 3f), Asset<TrainingDummySettings>(), stalker: true);
            var flyerCfg = Asset<FlyerSettings>();
            flyerCfg.maxHealth = 60f;
            flyerCfg.dieOnProjectileHit = true;
            Damageable flyer = Dummy(new Vector3(0.8f, 0f, 3f), flyerCfg);
            Physics.SyncTransforms();

            int n = Sweep(new Vector3(0f, 0.9f, 0f), Vector3.forward, shooter.transform, shooter.PlayerId, new HashSet<int>());
            Assert.That(n, Is.EqualTo(1), "seul le volant");
            Assert.That(stalker.Health.Current, Is.EqualTo(stalker.Health.Max), "le traqueur n'est pas touché");
            Assert.That(flyer.Dead, Is.True, "un volant meurt en un coup (60 PV, jet de 32)");
            Assert.That(flyer.LastAttackerId, Is.EqualTo(shooter.PlayerId), "attribué au joueur (aggro, coop)");
        }

        [Test]
        public void TirAmi_Respecte_JamaisSoiMeme()
        {
            var coop = Asset<CoopSettings>();
            PlayerHealth p1 = Player(Vector3.zero, coop);
            PlayerHealth p2 = Player(new Vector3(0f, 0f, 3f), coop);
            Physics.SyncTransforms();
            Vector3 eye = new Vector3(0f, 0.9f, 0f);

            Sweep(eye, Vector3.forward, p1.transform, p1.PlayerId, new HashSet<int>());
            Assert.That(p2.Health.Current, Is.EqualTo(p2.Health.Max), "tir ami désactivé : aucun dégât");
            Assert.That(p1.Health.Current, Is.EqualTo(p1.Health.Max), "jamais soi-même");

            coop.friendlyFire = true;
            Sweep(eye, Vector3.forward, p1.transform, p1.PlayerId, new HashSet<int>(), 1f);
            Assert.That(p2.Health.Current, Is.EqualTo(p2.Health.Max - _cfg.damage).Within(1e-3f), "tir ami activé : 32 PV");
            Assert.That(p1.Health.Current, Is.EqualTo(p1.Health.Max), "jamais soi-même, même dans son propre cône");
        }

        // ---------- Durée du jet et recharge ----------

        [Test]
        public void Jet_06s_PuisRecharge_25s_ClicTropTotMemoriseOuPerdu()
        {
            LanternState s = LanternState.Create();
            int jetTicks = MovementSettings.SecondsToTicks(0.6f, Dt), cdTicks = MovementSettings.SecondsToTicks(_cfg.cooldown, Dt);
            Assert.That(FireLantern.Ready(s, 1, _cfg, Dt), Is.True);

            LanternStep st = FireLantern.Step(ref s, 10, 10, false, false, _cfg, Dt);
            Assert.That(st.Started && st.Burning, Is.True);
            int burning = 1, t = 10;
            while (true)
            {
                t++;
                st = FireLantern.Step(ref s, t, 10, false, false, _cfg, Dt);
                if (!st.Burning)
                    break;
                burning++;
            }

            Assert.That(burning, Is.EqualTo(jetTicks), "le jet brûle 0.6 s");
            Assert.That(st.Ended, Is.True);
            int end = t;
            Assert.That(FireLantern.CooldownProgress(s, end, _cfg, Dt), Is.EqualTo(0f), "la lanterne s'éteint");

            // Clic pendant la recharge (bien avant la fin) : perdu, pas de jet.
            st = FireLantern.Step(ref s, end + 10, end + 10, false, false, _cfg, Dt);
            Assert.That(st.Started, Is.False);
            for (int k = end + 11; k < end + 20; k++)
                Assert.That(FireLantern.Step(ref s, k, end + 10, false, false, _cfg, Dt).Started, Is.False, "le clic n'est pas rejoué");
            Assert.That(FireLantern.CooldownProgress(s, end + cdTicks / 2, _cfg, Dt), Is.EqualTo(0.5f).Within(0.02f), "se rallume");

            // Clic 0.1 s avant la fin de la recharge : mémorisé, il part dès qu'elle est prête.
            int early = end + cdTicks - 5;
            Assert.That(FireLantern.Step(ref s, early, early, false, false, _cfg, Dt).Started, Is.False);
            int started = -1;
            for (int k = early + 1; k <= end + cdTicks + 2 && started < 0; k++)
                if (FireLantern.Step(ref s, k, early, false, false, _cfg, Dt).Started)
                    started = k;
            Assert.That(started, Is.EqualTo(end + cdTicks), "part dès la fin de la recharge (2.5 s)");
            Assert.That(s.JetCount, Is.EqualTo(2));
        }

        [Test]
        public void ObjetTenu_LeClicEstUnLancer_PrendreUnObjetArreteLeJet()
        {
            LanternState s = LanternState.Create();
            // Clic du lancer (l'orbe tenait un objet au début du tick) : pas de jet, même après le lancer.
            LanternStep st = FireLantern.Step(ref s, 5, 5, true, false, _cfg, Dt);
            Assert.That(st.Started, Is.False);
            Assert.That(FireLantern.Step(ref s, 6, 5, false, false, _cfg, Dt).Started, Is.False, "le clic du lancer est consommé");

            // Jet en cours, puis un objet est pris : le jet s'arrête et la recharge commence.
            Assert.That(FireLantern.Step(ref s, 10, 10, false, false, _cfg, Dt).Started, Is.True);
            st = FireLantern.Step(ref s, 15, 10, false, true, _cfg, Dt);
            Assert.That(st.Ended, Is.True);
            Assert.That(s.Inactive, Is.True);
            Assert.That(s.JetEndTick, Is.EqualTo(15));
        }

        // ---------- Personnages ----------

        [Test]
        public void Personnages_JamaisDeuxFoisLeMeme_GrisesExclus()
        {
            bool[] sel = { true, true, false, false };              // renard, caméléon, koala, hibou

            // Rien choisi : le joueur 1 prend le défaut (caméléon), le joueur 2 le premier libre (renard).
            Assert.That(CharacterPick.Assign(sel, null, 2, 1), Is.EqualTo(new[] { 1, 0 }));
            // Les deux veulent le renard : le joueur 1 l'a, le joueur 2 prend le premier libre.
            Assert.That(CharacterPick.Assign(sel, new[] { 0, 0 }, 2, 1), Is.EqualTo(new[] { 0, 1 }));
            // Personnage grisé demandé : remplacé.
            Assert.That(CharacterPick.Assign(sel, new[] { 2, 3 }, 2, 1), Is.EqualTo(new[] { 1, 0 }));
            // Plus de joueurs que de personnages jouables : pas de doublon, les derniers n'ont rien.
            Assert.That(CharacterPick.Assign(sel, null, 4, 1), Is.EqualTo(new[] { 1, 0, -1, -1 }));

            // Menu : défiler saute les grisés et celui de l'autre joueur.
            Assert.That(CharacterPick.Cycle(sel, 1, 1, new int[0]), Is.EqualTo(0), "caméléon → (koala, hibou grisés) → renard");
            Assert.That(CharacterPick.Cycle(sel, 0, -1, new int[0]), Is.EqualTo(1));
            Assert.That(CharacterPick.Cycle(sel, 1, 1, new[] { 0 }), Is.EqualTo(1), "le renard est pris : on reste sur le caméléon");
        }

        [Test]
        public void EtatDeLaRun_ArtefactDuPersonnage_DeuxJoueursDifferents()
        {
            var barrel = Asset<BarrelSettings>();
            CharacterRoster roster = Roster(barrel);
            var go = new GameObject("Run");
            SceneManager.MoveGameObjectToScene(go, _scene);
            var run = go.AddComponent<RunState>();
            run.Configure(null, 1234, null, roster);

            // Scène de test sans menu : caméléon (barillet) pour le joueur 1, renard (lanterne) pour celui qui rejoint.
            Assert.That(run.ArtifactFor(0), Is.SameAs(barrel));
            Assert.That(run.ArtifactFor(1), Is.SameAs(_cfg));
            Assert.That(run.CharacterFor(1).displayName, Is.EqualTo("Renard"));

            // Choix du menu : le joueur 1 prend le renard, le joueur 2 a donc le caméléon, même s'il demandait aussi le renard.
            RunSession.Characters = new[] { 0, 0 };
            Assert.That(run.ArtifactFor(0), Is.SameAs(_cfg));
            Assert.That(run.ArtifactFor(1), Is.SameAs(barrel));
            Assert.That(run.CharacterIndices(2), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(run.ArtifactFor(2), Is.Null, "pas de 3e personnage jouable : pas d'artefact plutôt qu'un doublon");
        }

        private CharacterRoster Roster(BarrelSettings barrel)
        {
            CharacterRoster r = Asset<CharacterRoster>();
            CharacterDefinition C(string n, ArtifactDefinition a, bool p)
            {
                CharacterDefinition c = Asset<CharacterDefinition>();
                c.displayName = n;
                c.artifact = a;
                c.playable = p;
                return c;
            }

            r.characters.Add(C("Renard", _cfg, true));
            r.characters.Add(C("Caméléon", barrel, true));
            r.characters.Add(C("Koala", null, false));
            r.characters.Add(C("Hibou", null, false));
            r.defaultIndex = 1;
            return r;
        }

        // ---------- Assets du projet ----------

        [Test]
        public void Assets_PersonnagesDuProjet_EtLanterneDuPrefab()
        {
            var roster = AssetDatabase.LoadAssetAtPath<CharacterRoster>("Assets/_Project/Settings/Personnages/Characters_Default.asset");
            Assert.That(roster, Is.Not.Null);
            Assert.That(roster.Count, Is.EqualTo(4));
            string[] names = { "Renard", "Caméléon", "Koala", "Hibou" };
            bool[] playable = { true, true, false, false };
            for (int i = 0; i < 4; i++)
            {
                Assert.That(roster[i].displayName, Is.EqualTo(names[i]));
                Assert.That(roster[i].Selectable, Is.EqualTo(playable[i]), names[i]);
            }

            Assert.That(roster[0].artifact, Is.InstanceOf<FireLanternSettings>(), "renard : lanterne de feu");
            Assert.That(roster[1].artifact, Is.InstanceOf<BarrelSettings>(), "caméléon : barillet");
            Assert.That(roster[roster.defaultIndex].displayName, Is.EqualTo("Caméléon"), "scènes de test : comme avant");
            var run = AssetDatabase.LoadAssetAtPath<RunDefinition>("Assets/_Project/Settings/Run_Default.asset");
            Assert.That(run.characters, Is.SameAs(roster), "menu");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            Assert.That(prefab.GetComponent<FireLanternView>(), Is.Not.Null);
            var so = new SerializedObject(prefab.GetComponent<FireLanternView>());
            foreach (string f in new[] { "lanternRoot", "flame", "muzzle", "puffMesh", "puffMaterial" })
                Assert.That(so.FindProperty(f).objectReferenceValue, Is.Not.Null, f);
        }
    }
}
