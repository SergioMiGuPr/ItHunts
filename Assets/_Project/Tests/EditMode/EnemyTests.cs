using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ItHunts.Combat;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace ItHunts.Tests
{
    /// <summary>
    /// Ennemis (étape 6, D-033) : prédiction, vue, pointe et vitesse du traqueur (plafonnée sous le bunny hop), poussée
    /// sans ralentissement, bond, contact, coups des ennemis (hors tir ami), traqueur intuable, timer et grâce de la zone,
    /// NavMesh (petite porte refusée au traqueur), réglages et scène Test_Arena.
    /// </summary>
    public class EnemyTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _objects)
                if (o != null)
                    Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private T Asset<T>() where T : ScriptableObject
        {
            var a = ScriptableObject.CreateInstance<T>();
            _objects.Add(a);
            return a;
        }

        // ---------- Calculs purs ----------

        [Test]
        public void Prediction_OuSeraLeJoueur_HorizontalSeulement()
        {
            Vector3 p = EnemyMath.Predict(new Vector3(1f, 0f, 2f), new Vector3(10f, -20f, 0f), 0.8f);
            Assert.That(p.x, Is.EqualTo(9f).Within(1e-4f));
            Assert.That(p.y, Is.EqualTo(0f), "la chute ne compte pas");
            Assert.That(p.z, Is.EqualTo(2f));
        }

        [Test]
        public void Vue_ConeEtPortee()
        {
            Vector3 eye = Vector3.up * 3f;
            Assert.That(EnemyMath.InSightCone(eye, Vector3.forward, new Vector3(0f, 1f, 20f), 30f, 110f), Is.True);
            Assert.That(EnemyMath.InSightCone(eye, Vector3.forward, new Vector3(0f, 1f, 31f), 30f, 110f), Is.False, "trop loin");
            Assert.That(EnemyMath.InSightCone(eye, Vector3.forward, new Vector3(20f, 1f, 10f), 30f, 110f), Is.False, "63° : hors du cône de 110°");
            Assert.That(EnemyMath.InSightCone(eye, Vector3.forward, new Vector3(10f, 1f, 10f), 30f, 110f), Is.True, "45°");
            Assert.That(EnemyMath.InSightCone(eye, Vector3.forward, new Vector3(0f, 1f, -10f), 30f, 110f), Is.False, "derrière");
        }

        [Test]
        public void Pointe_3s_PuisRecharge6s_SeulementQuandIlVoit()
        {
            var b = BurstState.Create();
            Assert.That(EnemyMath.UpdateBurst(ref b, false, 0f, 3f, 6f), Is.False, "ne voit pas : pas de pointe");
            Assert.That(EnemyMath.UpdateBurst(ref b, true, 1f, 3f, 6f), Is.True);
            Assert.That(b.Active(3.9f), Is.True);
            Assert.That(b.Active(4.01f), Is.False, "3 s");
            Assert.That(EnemyMath.UpdateBurst(ref b, true, 9.9f, 3f, 6f), Is.False, "recharge de 6 s après la fin");
            Assert.That(b.CooldownRemaining(9.9f), Is.EqualTo(0.1f).Within(1e-4f));
            Assert.That(EnemyMath.UpdateBurst(ref b, true, 10f, 3f, 6f), Is.True);
        }

        [Test]
        public void VitesseDuTraqueur_Base_Suivi_PointePlafonneeSousLeBunnyHop()
        {
            var c = Asset<StalkerSettings>();
            Assert.That(c.followPlayerSpeedShare, Is.EqualTo(0f), "suivi désactivé par défaut (D-034)");
            float b = c.speedBonus;                                     // nuit 3 : +0.5 m/s partout
            Assert.That(b, Is.EqualTo(0.5f));
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Chasse, false, 26f, c), Is.EqualTo(9f + b), "sans suivi : sa base");
            c.followPlayerSpeedShare = 0.85f;
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Repaire, false, 0f, c), Is.EqualTo(0f), "au repaire : immobile");
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Rode, false, 20f, c), Is.EqualTo(3f + b));
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Chasse, false, 0f, c), Is.EqualTo(9f + b), "base de chasse");
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Enrage, false, 0f, c), Is.EqualTo(11f + b), "base enragée");
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Chasse, false, 7.5f, c), Is.EqualTo(9f + b), "joueur qui court : sa base");
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Chasse, false, 14f, c), Is.EqualTo(11.9f + b).Within(1e-4f), "85 % du joueur");
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Chasse, false, 26f, c), Is.EqualTo(16f + b), "plafond : sa pointe");
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Chasse, true, 0f, c), Is.EqualTo(16f + b), "pointe");
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Enrage, true, 0f, c), Is.EqualTo(18f + b), "pointe enragée");
            foreach (float player in new[] { 18f, 22f, 26f })
                Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Enrage, true, player, c), Is.LessThanOrEqualTo(18f + b), "jamais au-dessus du bunny hop");
            c.followPlayerSpeedShare = 0f;
            Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Chasse, false, 26f, c), Is.EqualTo(9f + b), "suivi désactivé");
        }

        [Test]
        public void Poussee_NeRalentitJamaisLeJoueur()
        {
            var rng = new System.Random(5);
            for (int i = 0; i < 500; i++)
            {
                var v = new Vector3((float)rng.NextDouble() * 40f - 20f, (float)rng.NextDouble() * 10f - 5f, (float)rng.NextDouble() * 40f - 20f);
                var push = new Vector3((float)rng.NextDouble() * 10f - 5f, 1f, (float)rng.NextDouble() * 10f - 5f);
                Vector3 r = EnemyMath.PushWithoutSlowdown(v, push);
                Assert.That(new Vector2(r.x, r.z).magnitude, Is.GreaterThanOrEqualTo(new Vector2(v.x, v.z).magnitude - 1e-3f));
            }

            Vector3 still = EnemyMath.PushWithoutSlowdown(Vector3.zero, new Vector3(4f, 1f, 0f));
            Assert.That(still.x, Is.EqualTo(4f), "à l'arrêt : il est poussé");
            Vector3 against = EnemyMath.PushWithoutSlowdown(new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, -4f));
            Assert.That(new Vector2(against.x, against.z).magnitude, Is.EqualTo(10f).Within(1e-3f), "poussé de face : même vitesse");
        }

        [Test]
        public void MainDuTraqueur_CapsuleContreBoiteOrientee()
        {
            Vector3 half = new Vector3(2f, 1.1f, 2.25f);
            Quaternion rot = Quaternion.Euler(0f, 45f, 0f);
            Vector3 center = new Vector3(0f, 1.1f, 0f);
            Assert.That(EnemyMath.CapsuleIntersectsBox(Vector3.zero, 0.4f, 1.8f, center, half, rot), Is.True, "au centre");
            Assert.That(EnemyMath.CapsuleIntersectsBox(new Vector3(0f, 0f, 3.3f), 0.4f, 1.8f, center, half, rot), Is.True, "dans un coin de la boîte tournée");
            Assert.That(EnemyMath.CapsuleIntersectsBox(new Vector3(3.3f, 0f, 3.3f), 0.4f, 1.8f, center, half, rot), Is.False, "à côté");
            Assert.That(EnemyMath.CapsuleIntersectsBox(new Vector3(0f, 2.7f, 0f), 0.4f, 1.8f, center, half, rot), Is.False, "au-dessus");
        }

        [Test]
        public void TremblementDeCamera_SeulementPres()
        {
            Assert.That(ItHunts.Player.CameraShake.StepTrauma(0f, 25f, 0.6f), Is.EqualTo(0.6f));
            Assert.That(ItHunts.Player.CameraShake.StepTrauma(12.5f, 25f, 0.6f), Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(ItHunts.Player.CameraShake.StepTrauma(26f, 25f, 0.6f), Is.EqualTo(0f), "au-delà de 25 m : rien");
            Assert.That(ItHunts.Player.CameraShake.StepTrauma(5f, 0f, 0.6f), Is.EqualTo(0f), "désactivé");
        }

        [Test]
        public void Bond_ArcEtVitesseMax_Contact()
        {
            Vector3 v = EnemyMath.LeapVelocity(Vector3.zero, new Vector3(0f, 0f, 5f), 14f, 1f, 20f);
            float flight = 2f * v.y / 20f;
            Assert.That(v.y, Is.EqualTo(Mathf.Sqrt(40f)).Within(1e-3f), "arc de 1 m");
            Assert.That(v.z * flight, Is.EqualTo(6f).Within(0.01f), "retombe 1 m après le joueur");
            Vector3 far = EnemyMath.LeapVelocity(Vector3.zero, new Vector3(0f, 0f, 30f), 14f, 1f, 20f);
            Assert.That(new Vector2(far.x, far.z).magnitude, Is.EqualTo(14f).Within(1e-3f), "au plus 14 m/s");

            Assert.That(EnemyMath.CapsuleGap(Vector3.zero, 0.45f, 1.1f, new Vector3(1f, 0f, 0f), 0.4f, 1.8f), Is.EqualTo(0.15f).Within(1e-4f));
            Assert.That(EnemyMath.CapsuleGap(Vector3.zero, 0.45f, 1.1f, new Vector3(0f, 3f, 0f), 0.4f, 1.8f), Is.EqualTo(float.PositiveInfinity), "au-dessus");
        }

        // ---------- Joueur, traqueur, zone ----------

        private PlayerHealth Player(CoopSettings coop)
        {
            var go = new GameObject("Joueur (test)");
            _objects.Add(go);
            go.AddComponent<CharacterController>();
            var h = go.AddComponent<PlayerHealth>();
            h.Configure(Asset<PlayerHealthSettings>(), coop);
            return h;
        }

        [Test]
        public void CoupsDesEnnemis_SansTirAmi_MortAnnoncee()
        {
            var coop = Asset<CoopSettings>();
            Assert.That(coop.friendlyFire, Is.False);
            PlayerHealth p = Player(coop);
            int died = 0;
            void OnDied(PlayerHealth x) => died++;
            PlayerHealth.Died += OnDied;
            try
            {
                Assert.That(p.TakeEnemyDamage(15f, Vector3.zero, 0f), Is.True, "une goule blesse même sans tir ami");
                Assert.That(p.Health.Current, Is.EqualTo(85f));
                for (int i = 0; i < 6; i++)
                    p.TakeEnemyDamage(15f, Vector3.zero, i);
                Assert.That(p.Dead, Is.True);
                Assert.That(died, Is.EqualTo(1), "une seule mort annoncée");
                p.Restore();
                Assert.That(p.Health.Current, Is.EqualTo(100f));
                Assert.That(p.Dead, Is.False);
            }
            finally
            {
                PlayerHealth.Died -= OnDied;
            }
        }

        [Test]
        public void Traqueur_Intuable()
        {
            var cfg = Asset<StalkerSettings>();
            var go = new GameObject("Traqueur (test)");
            _objects.Add(go);
            go.AddComponent<CapsuleCollider>();
            var d = go.AddComponent<Damageable>();
            var so = new SerializedObject(d);
            so.FindProperty("settings").objectReferenceValue = cfg;
            so.FindProperty("immuneToOrbs").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            d.Init();
            Assert.That(d.ApplyDamage(1000f, 0f, out bool killed), Is.EqualTo(0f));
            Assert.That(killed, Is.False);
            var dmg = Asset<DamageSettings>();
            ImpactDamager.ResetCooldowns();
            Assert.That(ImpactDamager.TryDamage(d, 1, 80f, 30f, Vector3.zero, dmg, 0f, out _), Is.False, "objet lancé : rien");
            Assert.That(ImpactDamager.ApplyDirect(d, 2, 40f, 20f, 80f, Vector3.zero, 0f), Is.False, "mur : rien");
            Assert.That(d.Health.Current, Is.EqualTo(d.Health.Max));
        }

        [Test]
        public void Zone_GraceTimerEnrage()
        {
            var s = Asset<ZoneSettings>();
            Assert.That(s.timer, Is.EqualTo(900f), "15 min (D-048)");
            Assert.That(s.gracePeriod, Is.EqualTo(180f), "traqueur lâché à 3 min (D-048)");
            var go = new GameObject("Zone (test)");
            _objects.Add(go);
            var z = go.AddComponent<ZoneState>();
            typeof(ZoneState).GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(z, s);
            FieldInfo elapsed = typeof(ZoneState).GetField("<Elapsed>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo started = typeof(ZoneState).GetField("<Started>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            elapsed.SetValue(z, 1000f);
            Assert.That(z.InGrace && !z.Enraged && !z.ExitUnlocked, Is.True, "timer pas démarré (salle de départ) : rien ne se passe");
            started.SetValue(z, true);
            elapsed.SetValue(z, 179f);
            Assert.That(z.InGrace, Is.True);
            Assert.That(z.Enraged, Is.False);
            elapsed.SetValue(z, 181f);
            Assert.That(z.InGrace, Is.False);
            Assert.That(z.TimeLeft, Is.EqualTo(719f));
            elapsed.SetValue(z, 599f);
            Assert.That(z.ExitUnlocked, Is.False);
            elapsed.SetValue(z, 600f);
            Assert.That(z.ExitUnlocked, Is.True, "porte de sortie à 10 min");
            elapsed.SetValue(z, 900f);
            Assert.That(z.Enraged, Is.True, "à 0 : traqueur enragé");
            Assert.That(z.TimeLeft, Is.EqualTo(0f));
            PlayerHealth.ZoneHandlesDeath = false;
        }

        // ---------- NavMesh ----------

        [Test]
        public void NavMesh_PetitePorte_LaGoulePasse_LeTraqueurFaitLeTour()
        {
            // Sol de 40 × 20 m coupé en deux par un mur (x = 0) : une petite porte de 1.2 m (z −6) et un passage de 4 m loin
            // (z 6..10) ; le traqueur (rayon 1.25) ne passe que par le grand, la goule (0.45) aussi par la petite.
            var sources = new List<NavMeshBuildSource>
            {
                Box(new Vector3(0f, -0.25f, 0f), new Vector3(40f, 0.5f, 20f)),
                Box(new Vector3(0f, 2f, -8.3f), new Vector3(0.5f, 4f, 3.4f)),   // z −10 .. −6.6
                Box(new Vector3(0f, 2f, 0.3f), new Vector3(0.5f, 4f, 11.4f)),   // z −5.4 .. 6
            };
            var bounds = new Bounds(Vector3.zero, new Vector3(50f, 20f, 30f));
            var instances = new List<NavMeshDataInstance>();
            var ids = new List<int>();
            try
            {
                int small = Bake(sources, bounds, 0.45f, 1.2f, instances, ids);
                int big = Bake(sources, bounds, 1.25f, 3.5f, instances, ids);
                Vector3 from = new Vector3(-6f, 0f, -6f), to = new Vector3(6f, 0f, -6f);
                float ghoul = PathLength(small, from, to, out NavMeshPathStatus gs);
                float stalker = PathLength(big, from, to, out NavMeshPathStatus ss);
                Assert.That(gs, Is.EqualTo(NavMeshPathStatus.PathComplete));
                Assert.That(ghoul, Is.LessThan(13f), "la goule passe la petite porte (12 m en ligne droite)");
                Assert.That(ss, Is.EqualTo(NavMeshPathStatus.PathComplete), "le traqueur arrive quand même");
                Assert.That(stalker, Is.GreaterThan(28f), "il fait le tour par le passage de 4 m");
            }
            finally
            {
                foreach (NavMeshDataInstance i in instances)
                    i.Remove();
                foreach (int id in ids)
                    NavMesh.RemoveSettings(id);
            }
        }

        private static NavMeshBuildSource Box(Vector3 center, Vector3 size) => new NavMeshBuildSource
        {
            shape = NavMeshBuildSourceShape.Box, size = size, transform = Matrix4x4.Translate(center), area = 0,
        };

        private static int Bake(List<NavMeshBuildSource> sources, Bounds bounds, float radius, float height,
            List<NavMeshDataInstance> instances, List<int> ids)
        {
            NavMeshBuildSettings s = NavMesh.CreateSettings();
            s.agentRadius = radius;
            s.agentHeight = height;
            s.agentClimb = 0.45f;
            s.agentSlope = 45f;
            NavMeshData data = NavMeshBuilder.BuildNavMeshData(s, sources, bounds, Vector3.zero, Quaternion.identity);
            instances.Add(NavMesh.AddNavMeshData(data));
            ids.Add(s.agentTypeID);
            return s.agentTypeID;
        }

        private static float PathLength(int agent, Vector3 from, Vector3 to, out NavMeshPathStatus status)
        {
            var f = new NavMeshQueryFilter { agentTypeID = agent, areaMask = NavMesh.AllAreas };
            NavMesh.SamplePosition(from, out NavMeshHit a, 2f, f);
            NavMesh.SamplePosition(to, out NavMeshHit b, 2f, f);
            var path = new NavMeshPath();
            NavMesh.CalculatePath(a.position, b.position, f, path);
            status = path.status;
            float d = 0f;
            for (int i = 1; i < path.corners.Length; i++)
                d += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            return d;
        }

        // ---------- Réglages et scène ----------

        [Test]
        public void Reglages_ValeursDeDepart()
        {
            var g = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            Assert.That(g.maxHealth, Is.EqualTo(60f));
            Assert.That(g.mass, Is.EqualTo(50f));
            Assert.That(g.runSpeed, Is.EqualTo(10f));
            Assert.That(g.leapSpeed, Is.EqualTo(14f));
            Assert.That(g.leapTelegraph, Is.EqualTo(0.3f));
            Assert.That(g.leapCooldown, Is.EqualTo(2.5f));
            Assert.That(g.contactDamage, Is.EqualTo(15f));
            Assert.That(g.contactInterval, Is.EqualTo(1f));
            Assert.That(g.stunDuration, Is.EqualTo(1.5f));
            var s = AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            Assert.That(new[] { s.roamSpeed, s.chaseSpeed, s.enragedSpeed }, Is.EqualTo(new[] { 3f, 9f, 11f }));
            Assert.That(new[] { s.burstSpeed, s.enragedBurstSpeed, s.burstDuration, s.burstCooldown }, Is.EqualTo(new[] { 16f, 18f, 3f, 6f }));
            Assert.That(s.predictionHorizon, Is.EqualTo(0.8f));
            // Géant (D-034).
            Assert.That(new[] { s.width, s.height }, Is.EqualTo(new[] { 7f, 14f }));
            Assert.That(new[] { s.armLength, s.grabTelegraph, s.grabCooldown }, Is.EqualTo(new[] { 8.3f, 0.5f, 2.5f }), "bras réel du v4 (D-080)");
            Assert.That(new[] { s.sightRange, s.soundRange, s.shakeRange }, Is.EqualTo(new[] { 60f, 60f, 25f }));
            Assert.That(s.followPlayerSpeedShare, Is.EqualTo(0f), "suivi désactivé par défaut");
            Assert.That(s.handClearance, Is.EqualTo(4.5f), "dégagement de la main au-dessus du point visé");
            // Poursuite façon Pac-Man (D-035).
            // v4 (D-080) : baissé à 8.5 m (dos à quatre pattes 8.3 m), bras par une porte 10.3 m (8.3 + 2 m d'épaule engagée).
            Assert.That(new[] { s.crouchMinHeight, s.crouchSpeedFactor, s.roomReach, s.retargetInterval, s.grabReaim },
                Is.EqualTo(new[] { 8.5f, 0.6f, 10.3f, 5f, 0.15f }));
            Assert.That(s.allowUTurnWhenEnraged, Is.True);
            // Mains au-dessus de la tête (D-037).
            Assert.That(new[] { s.overheadRadius, s.overheadAboveHead, s.overheadTelegraph, s.overheadDuration, s.overheadCooldown },
                Is.EqualTo(new[] { 10f, 12f, 0.2f, 1f, 1.5f }));
            // D-038 : accélération de l'Enragé, aggro des goules.
            Assert.That(s.enragedRampTimes, Is.EqualTo(new[] { 0f, 10f, 20f, 25f, 30f }));
            Assert.That(s.enragedRampSpeeds, Is.EqualTo(new[] { 11f, 13f, 15f, 16.5f, 18f }));
            Assert.That(new[] { s.enragedRampBlend, s.maxSpeed }, Is.EqualTo(new[] { 1f, 18f }));
            Assert.That(new[] { g.aggroSightRange, g.aggroProximity, g.aggroLoseDistance, g.aggroLoseTime }, Is.EqualTo(new[] { 20f, 8f, 50f, 5f }));
            var m = AssetDatabase.LoadAssetAtPath<MagnetismOrbSettings>("Assets/_Project/Settings/Magnetism_Default.asset");
            Assert.That(m.tractionSpeed, Is.EqualTo(18f), "traction plafonnée à 18 m/s (D-036)");
            var z = AssetDatabase.LoadAssetAtPath<ZoneSettings>("Assets/_Project/Settings/Zone_Test.asset");
            Assert.That(z.timer, Is.EqualTo(900f));
        }

        private static int CountScript(string scenePath, string scriptPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(scriptPath);
            Assert.That(guid, Is.Not.Empty, scriptPath);
            return Regex.Matches(File.ReadAllText(scenePath), $@"m_Script: \{{fileID: 11500000, guid: {guid}").Count;
        }

        [Test]
        public void Scene_TestArena_Labyrinthe()
        {
            const string scene = "Assets/Scenes/Test_Arena.unity";
            const string dir = "Assets/_Project/Scripts/Runtime/Enemies/";
            Assert.That(File.Exists(scene), Is.True);
            string text = File.ReadAllText(scene);
            Assert.That(CountScript(scene, dir + "ZoneNavMesh.cs"), Is.EqualTo(1));
            Assert.That(CountScript(scene, dir + "ZoneState.cs"), Is.EqualTo(1));
            Assert.That(CountScript(scene, dir + "ZoneExit.cs"), Is.EqualTo(1), "une sortie");
            Assert.That(CountScript(scene, dir + "MazeGraph.cs"), Is.EqualTo(1), "le graphe du labyrinthe");
            Assert.That(CountScript(scene, dir + "Stalker.cs"), Is.EqualTo(1), "le traqueur");
            // Vagues (D-041) : plus de goules ni de volants posés à la main ; une réserve inactive et des points d'apparition.
            Assert.That(CountScript(scene, dir + "Ghoul.cs"), Is.EqualTo(80), "réserve de 80 goules");
            Assert.That(CountScript(scene, dir + "Flyer.cs"), Is.EqualTo(20), "réserve de 20 volants");
            Assert.That(Regex.Matches(text, @"m_Name: ""?Goule \(r(é|\\xE9)serve\)").Count, Is.EqualTo(80), "toutes dans la réserve");
            Assert.That(CountScript(scene, dir + "WaveDirector.cs"), Is.EqualTo(1), "le directeur des vagues");
            Assert.That(CountScript(scene, dir + "SpawnPoint.cs"), Is.GreaterThanOrEqualTo(5 * 8 + 12 * 3 + 5 * 4 + 12), "points d'apparition : salles, couloirs, en hauteur");
            Assert.That(Regex.Matches(text, @"kinds: 2").Count, Is.GreaterThanOrEqualTo(5 * 4 + 12), "points des volants : salles et couloirs");
            Assert.That(CountScript(scene, dir + "ReachRoom.cs"), Is.EqualTo(6), "pièces fermées où il passe le bras");
            Assert.That(CountScript(scene, dir + "ClimbLink.cs"), Is.EqualTo(0), "plus de murs grimpables posés à la main : ZoneNavMesh les trouve (D-038)");
            Assert.That(Regex.Matches(text, @"voie au sol").Count, Is.EqualTo(3), "une voie au sol (et son mur de 16 m) par couloir en pente");
            Assert.That(Regex.Matches(text, @"m_Name: Main gauche").Count, Is.EqualTo(1), "deux mains (geste au-dessus de la tête)");
            Assert.That(CountScript(scene, dir + "StalkerShortcut.cs"), Is.EqualTo(0), "plus de grilles réservées (D-035)");
            Assert.That(text.Contains("petite porte"), Is.False, "plus de tunnels bas de 4 × 4 m");
            Assert.That(Regex.Matches(text, @"m_Name: (Caisse|Bloc lourd)").Count, Is.GreaterThanOrEqualTo(5 * 15), "au moins 15 objets par grande salle");
            Assert.That(Regex.IsMatch(text, @"ItHunts\.Enemies\.ZoneState\s+settings: \{fileID: 0\}"), Is.False, "la zone a ses réglages");
            Assert.That(Regex.IsMatch(text, @"ItHunts\.Enemies\.Stalker\s+settings: \{fileID: 0\}"), Is.False, "le traqueur a ses réglages");
            Assert.That(Regex.IsMatch(text, @"immuneToOrbs: 1"), Is.True, "le traqueur est intuable");
        }

        // ---------- Labyrinthe et règle de Pac-Man (D-035) ----------

        /// <summary>
        /// Graphe 3 × 3 du labyrinthe plus un cul-de-sac (repaire) :
        ///   6 – 7 – 8
        ///   3 – 4 – 5 – 9
        ///   0 – 1 – 2
        /// </summary>
        private static MazeGraph Grid(out GameObject go)
        {
            go = new GameObject("graphe");
            var g = go.AddComponent<MazeGraph>();
            var centers = new Vector3[10];
            var halves = new Vector3[10];
            var names = new string[10];
            for (int i = 0; i < 9; i++)
            {
                centers[i] = new Vector3((i % 3 - 1) * 100f, 0f, (i / 3 - 1) * 100f);
                halves[i] = new Vector3(10f, 10f, 10f);
                names[i] = i.ToString();
            }

            centers[9] = new Vector3(130f, 0f, 0f);
            halves[9] = new Vector3(10f, 10f, 10f);
            names[9] = "repaire";
            g.Configure(names, centers, halves, new[]
            {
                new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(3, 4), new Vector2Int(4, 5), new Vector2Int(6, 7), new Vector2Int(7, 8),
                new Vector2Int(0, 3), new Vector2Int(3, 6), new Vector2Int(1, 4), new Vector2Int(4, 7), new Vector2Int(2, 5), new Vector2Int(5, 8),
                new Vector2Int(5, 9),
            });
            return g;
        }

        [Test]
        public void PacMan_JamaisDeDemiTour_SaufCulDeSacOuPermis()
        {
            MazeGraph g = Grid(out GameObject go);
            try
            {
                // Au centre (4), arrivé par l'ouest (3) ; le joueur est derrière lui, dans le couloir 3 – 4 : il ne revient
                // pas en arrière, il fait le tour par une autre branche.
                Vector3 behind = new Vector3(-50f, 0f, 0f);
                int next = g.ChooseNext(4, 3, behind, false);
                Assert.That(next, Is.Not.EqualTo(3), "jamais la branche d'où il vient");
                Assert.That(next == 1 || next == 7, Is.True, "il fait le tour par le sud ou le nord");
                Assert.That(g.ChooseNext(4, 3, behind, true), Is.EqualTo(3), "demi-tour permis (Enragé) : le plus court");
                Assert.That(g.ChooseNext(4, 3, new Vector3(60f, 0f, 0f), false), Is.EqualTo(5), "joueur devant : il y va");
                Assert.That(g.ChooseNext(9, 5, behind, false), Is.EqualTo(5), "cul-de-sac : demi-tour permis");
                Assert.That(g.ChooseNext(4, 3, new Vector3(3f, 0f, 2f), false), Is.EqualTo(-1), "joueur dans l'intersection : droit dessus");
                var rng = new System.Random(7);
                for (int k = 0; k < 500; k++)
                {
                    int at = rng.Next(0, 9);
                    var nb = g.Neighbors(at);
                    int from = nb[rng.Next(0, nb.Count)];
                    var target = new Vector3(rng.Next(-120, 140), 0f, rng.Next(-120, 120));
                    int n = g.ChooseNext(at, from, target, false);
                    if (n >= 0)
                        Assert.That(n, Is.Not.EqualTo(from), $"de {at} arrivé par {from}, cible {target}");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Labyrinthe_ChaqueCouloirDansUneBoucle()
        {
            MazeGraph g = Grid(out GameObject go);
            try
            {
                for (int e = 0; e < g.EdgeCount; e++)
                    Assert.That(g.EdgeInLoop(e), Is.EqualTo(g.Edge(e).y != 9), $"couloir {g.Edge(e)}");
                Assert.That(g.NodeAt(new Vector3(1f, 2f, -3f)), Is.EqualTo(4));
                Assert.That(g.NodeAt(new Vector3(50f, 0f, 0f)), Is.EqualTo(-1), "dans un couloir");
                Assert.That(g.Distance(0, 8), Is.EqualTo(400f).Within(1e-3f));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Labyrinthe_DeLaScene_ToutesLesBoucles()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene("Assets/Scenes/Test_Arena.unity");
            try
            {
                MazeGraph g = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<MazeGraph>()).First(x => x != null);
                Assert.That(g.NodeCount, Is.EqualTo(11), "10 intersections et l'arène du combat de boss (D-049)");
                Assert.That(g.EdgeCount, Is.EqualTo(14));
                int deadEnds = 0;
                for (int e = 0; e < g.EdgeCount; e++)
                    if (!g.EdgeInLoop(e))
                        deadEnds++;
                Assert.That(deadEnds, Is.EqualTo(2), "seuls le repaire et l'arène (par les escaliers de sortie) sont des culs-de-sac");
            }
            finally
            {
                UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void Volant_ValeursEtEclatement()
        {
            var f = AssetDatabase.LoadAssetAtPath<FlyerSettings>("Assets/_Project/Settings/Flyer_Default.asset");
            Assert.That(f, Is.Not.Null);
            Assert.That(new[] { f.maxHealth, f.mass, f.flySpeed, f.diveSpeed, f.diveDamage, f.diveTelegraph, f.diveCooldown, f.stunDuration },
                Is.EqualTo(new[] { 25f, 8f, 6f, 14f, 8f, 0.4f, 3f, 1.5f }));
            Assert.That(new[] { f.orbitHeight, f.orbitRadius, f.burstRadius, f.burstDamage, f.burstEdgeShare }, Is.EqualTo(new[] { 8f, 8f, 3f, 30f, 0.25f }));
            Assert.That(f.dieOnProjectileHit, Is.True, "meurt au premier impact de projectile (activé par défaut)");
            Assert.That(f.diveTurnRate, Is.EqualTo(80f), "piqué : virage limité à 80°/s (90 touchait 38 % des joueurs qui courent, objectif 25 à 35 %)");
            Assert.That(f.diveLead, Is.EqualTo(0f), "piqué : il vise où est le joueur (mesuré : 0.3 touchait 68 % des joueurs qui courent)");
            // Virage du piqué : au plus l'angle donné, vitesse gardée.
            Vector3 v = EnemyMath.TurnTowards(new Vector3(0f, 0f, 14f), Vector3.right, 45f, 14f);
            Assert.That(Vector3.Angle(Vector3.forward, v), Is.EqualTo(45f).Within(0.01f));
            Assert.That(v.magnitude, Is.EqualTo(14f).Within(1e-3f));
            v = EnemyMath.TurnTowards(new Vector3(0f, 0f, 14f), new Vector3(0.1f, 0f, 1f), 45f, 14f);
            Assert.That(Vector3.Angle(new Vector3(0.1f, 0f, 1f), v), Is.LessThan(0.01f), "petit écart : rattrapé d'un coup");
            Assert.That(new[] { f.aggroSightRange, f.aggroProximity, f.aggroLoseDistance, f.aggroLoseTime }, Is.EqualTo(new[] { 20f, 8f, 50f, 5f }));
            // Éclatement : 30 au centre, le quart au bord, linéaire, rien au-delà.
            Assert.That(EnemyMath.BurstDamage(0f, 3f, 30f, 0.25f), Is.EqualTo(30f));
            Assert.That(EnemyMath.BurstDamage(1.5f, 3f, 30f, 0.25f), Is.EqualTo(18.75f).Within(1e-4f));
            Assert.That(EnemyMath.BurstDamage(3f, 3f, 30f, 0.25f), Is.EqualTo(7.5f).Within(1e-4f));
            Assert.That(EnemyMath.BurstDamage(3.01f, 3f, 30f, 0.25f), Is.EqualTo(0f));
        }

        [Test]
        public void MainPlantee_LeCorpsGardeSaVitesse_PriseOrdinaireRalentie()
        {
            var c = ScriptableObject.CreateInstance<StalkerSettings>();
            try
            {
                Assert.That(EnemyMath.GrabMoveFactor(false, false, c), Is.EqualTo(1f), "sans prise : vitesse normale");
                Assert.That(EnemyMath.GrabMoveFactor(true, false, c), Is.EqualTo(0.3f), "annonce et descente : ×0.3");
                Assert.That(EnemyMath.GrabMoveFactor(true, true, c), Is.EqualTo(1f), "main plantée (piste b) : il continue à sa vitesse");
                c.plantedMoveFactor = 0.5f;
                Assert.That(EnemyMath.GrabMoveFactor(true, true, c), Is.EqualTo(0.5f), "réglable");

                // Revers de la main gauche (piste a) : 7 m depuis le bord du corps (rayon 3.5), 12 m de haut, après 0.5 s.
                Assert.That(new[] { c.backhandReach, c.backhandHeight, c.backhandDelay, c.backhandTelegraph, c.backhandSweep },
                    Is.EqualTo(new[] { 7f, 12f, 0.5f, 0.2f, 0.25f }));
                Assert.That(c.backhandEnabled, Is.True);
                Vector3 feet = Vector3.zero;
                Assert.That(EnemyMath.InBackhandZone(feet, 3.5f, new Vector3(10.4f, 0f, 0f), c), Is.True, "à 6.9 m du bord");
                Assert.That(EnemyMath.InBackhandZone(feet, 3.5f, new Vector3(0f, 0f, -10.6f), c), Is.False, "à 7.1 m du bord");
                Assert.That(EnemyMath.InBackhandZone(feet, 3.5f, new Vector3(5f, 12.5f, 0f), c), Is.False, "au-dessus de 12 m");
                var asset = AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
                Assert.That(asset.backhandReach, Is.EqualTo(7f), "asset : portée réglée sur la mesure");
            }
            finally
            {
                Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void MainPlantee_ApresUnePriseRatee_PasApresUnePriseReussie()
        {
            var c = ScriptableObject.CreateInstance<StalkerSettings>();
            try
            {
                Assert.That(new[] { c.plantedDuration, c.plantedTurnFactor, c.grabCooldown }, Is.EqualTo(new[] { 1.5f, 0.4f, 2.5f }));
                var asset = AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
                Assert.That(new[] { asset.plantedDuration, asset.plantedTurnFactor, asset.plantedMoveFactor }, Is.EqualTo(new[] { 1.5f, 0.4f, 1f }),
                    "asset : 1.5 s, rotation ×0.4, corps à sa vitesse normale (piste b)");
                float missed = EnemyMath.GrabHoldTime(false, c, out bool plantedMiss);
                Assert.That(plantedMiss, Is.True, "prise ratée : main plantée");
                Assert.That(missed, Is.EqualTo(1.5f));
                float hit = EnemyMath.GrabHoldTime(true, c, out bool plantedHit);
                Assert.That(plantedHit, Is.False, "prise réussie : pas de main plantée");
                Assert.That(hit, Is.EqualTo(c.grabHold));
                c.plantedDuration = 3f;
                Assert.That(EnemyMath.GrabHoldTime(false, c, out _), Is.EqualTo(3f), "réglable");
            }
            finally
            {
                Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void Enrage_PaliersSurLaMemeCible_PlafondA18()
        {
            var c = ScriptableObject.CreateInstance<StalkerSettings>();
            try
            {
                float Ramp(float t, out int tier) => EnemyMath.EnragedRampSpeed(t, c.enragedRampTimes, c.enragedRampSpeeds, c.enragedRampBlend, out tier);
                Assert.That(Ramp(0f, out int t0), Is.EqualTo(11f)); Assert.That(t0, Is.EqualTo(0));
                Assert.That(Ramp(9.9f, out _), Is.EqualTo(11f), "palier suivant à 10 s");
                Assert.That(Ramp(10.5f, out int t1), Is.EqualTo(12f).Within(1e-4f), "montée en 1 s : à mi-chemin"); Assert.That(t1, Is.EqualTo(1));
                Assert.That(Ramp(11f, out _), Is.EqualTo(13f).Within(1e-4f));
                Assert.That(Ramp(21f, out _), Is.EqualTo(15f).Within(1e-4f));
                Assert.That(Ramp(26f, out _), Is.EqualTo(16.5f).Within(1e-4f));
                Assert.That(Ramp(31f, out int t4), Is.EqualTo(18f).Within(1e-4f)); Assert.That(t4, Is.EqualTo(4));
                Assert.That(Ramp(300f, out _), Is.EqualTo(18f).Within(1e-4f), "maximum");
                // Pointe et suivi jamais au-delà de 18 m/s, même au dernier palier.
                // Nuit 3, point 5 : +0.5 m/s dans tous les états (speedBonus), plafond compris.
                float b = c.speedBonus;
                Assert.That(b, Is.EqualTo(0.5f));
                Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Enrage, true, 30f, c, 18f), Is.EqualTo(18f + b));
                Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Enrage, false, 30f, c, 15f), Is.EqualTo(15f + b), "suivi désactivé : la base du palier");
                c.enragedBurstSpeed = 25f;
                Assert.That(EnemyMath.StalkerSpeed(StalkerMode.Enrage, true, 0f, c, 11f), Is.EqualTo(18f + b), "plafond maxSpeed");
            }
            finally
            {
                Object.DestroyImmediate(c);
            }
        }

        [Test]
        public void MainsLevees_ZoneEtTrajectoirePredite()
        {
            // Traqueur aux pieds en (0, 0, 0), 14 m : cylindre de 10 m de rayon, de 7 m jusqu'à 26 m (12 m au-dessus de la tête).
            Vector3 c = Vector3.zero;
            bool In(Vector3 feet, Vector3 v) => EnemyMath.WillPassOver(feet, v, 20f, c, 10f, 7f, 26f, 0.6f);
            Assert.That(In(new Vector3(3f, 15f, 0f), Vector3.zero), Is.True, "au-dessus de lui");
            Assert.That(In(new Vector3(9f, 24f, 0f), Vector3.zero), Is.True, "en haut du cylindre");
            Assert.That(In(new Vector3(5f, 0f, 0f), Vector3.zero), Is.False, "au sol à côté : la prise s'en charge, pas ce geste");
            Assert.That(In(new Vector3(12f, 15f, 0f), Vector3.zero), Is.False, "trop loin, immobile");
            Assert.That(In(new Vector3(18f, 15f, 0f), new Vector3(-18f, 0f, 0f)), Is.True, "traction à 18 m/s vers lui : prédit");
            Assert.That(In(new Vector3(18f, 15f, 0f), new Vector3(18f, 0f, 0f)), Is.False, "s'éloigne");
            Assert.That(In(new Vector3(5f, 30f, 0f), Vector3.zero), Is.False, "plus haut que la zone");
            Assert.That(In(new Vector3(5f, 30f, 0f), new Vector3(0f, -6f, 0f)), Is.True, "redescend dans la zone (chute prédite)");
        }

        [Test]
        public void BondDeHaut_AtteintLePointPlusBas()
        {
            // Goule sur une corniche de 11 m, joueur 5 m devant au sol : l'arc retombe sur lui (au plus 14 m/s à plat).
            Vector3 from = new Vector3(0f, 11f, 0f), to = new Vector3(0f, 0f, 5f);
            Vector3 v = EnemyMath.LeapVelocityTo(from, to, 14f, 1f, 30f, 0f);
            Vector3 p = from;
            float dt = 1f / 240f;
            while (p.y > to.y || v.y > 0f)
            {
                v.y -= 30f * dt;
                p += v * dt;
            }

            Assert.That(new Vector2(v.x, v.z).magnitude, Is.LessThanOrEqualTo(14f + 1e-3f));
            Assert.That(p.z, Is.EqualTo(5f).Within(0.2f), "retombe sur le joueur");
            Vector3 a = EnemyMath.LeapVelocityTo(Vector3.zero, new Vector3(4f, 0f, 0f), 14f, 1f, 30f);
            Vector3 b = EnemyMath.LeapVelocity(Vector3.zero, new Vector3(4f, 0f, 0f), 14f, 1f, 30f);
            Assert.That((a - b).magnitude, Is.LessThan(1e-3f), "même niveau : identique au bond au sol");
        }

        [Test]
        public void Goule_SignauxDeLAnnonce_EteintsALaRemiseAZeroEtAuRecyclage()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Niveau/Goule.prefab");
            GameObject go = Object.Instantiate(prefab);
            try
            {
                var g = go.GetComponent<Ghoul>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(Ghoul).GetMethod("SetMode", flags).Invoke(g, new object[] { GhoulMode.Annonce });
                typeof(Ghoul).GetMethod("UpdateVisual", flags).Invoke(g, null);
                Assert.That(g.SignalsOn, Is.True, "annonce : yeux, gueule et disque allumés");
                g.ClearSignals();
                Assert.That(g.SignalsOn, Is.False, "mort ou recyclée : plus rien d'allumé (point rouge resté au sol)");
                typeof(Ghoul).GetMethod("UpdateVisual", flags).Invoke(g, null);
                Assert.That(g.SignalsOn, Is.True);
                // Désactivée par la réserve : OnDisable (non appelé en mode édition), vérifié en jeu.
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
