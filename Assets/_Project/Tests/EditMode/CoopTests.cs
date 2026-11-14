using System.Collections.Generic;
using System.Reflection;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Écran partagé et coop locale (étape 9, D-059) : à terre, réanimation, spectateur, fin de partie, découpe.</summary>
    public class CoopTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();
        private readonly List<PlayerHealth> _players = new List<PlayerHealth>();

        private static void Invoke(object o, string m) =>
            o.GetType().GetMethod(m, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(o, null);

        [TearDown]
        public void TearDown()
        {
            foreach (PlayerHealth p in _players)
                if (p != null)
                    Invoke(p, "OnDisable");
            foreach (GameObject go in _made)
                Object.DestroyImmediate(go);
            _made.Clear();
            _players.Clear();
            PlayerHealth.ZoneHandlesDeath = false;
        }

        private PlayerHealth Player(Vector3 at)
        {
            var go = new GameObject("Joueur (test)");
            _made.Add(go);
            go.transform.position = at;
            var p = go.AddComponent<PlayerHealth>();
            var so = new SerializedObject(p);
            so.FindProperty("coop").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CoopSettings>("Assets/_Project/Settings/Coop_Default.asset");
            so.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PlayerHealthSettings>("Assets/_Project/Settings/PlayerHealth_Default.asset");
            so.ApplyModifiedPropertiesWithoutUndo();
            p.Init();
            Invoke(p, "OnEnable");
            p.Restore();
            _players.Add(p);
            return p;
        }

        [Test]
        public void Reglages_AterreTrenteSecondes_RelevePar3sA2m_50PV()
        {
            var c = AssetDatabase.LoadAssetAtPath<CoopSettings>("Assets/_Project/Settings/Coop_Default.asset");
            Assert.That(new[] { c.downedSeconds, c.reviveHoldSeconds, c.reviveRange, c.reviveHealth }, Is.EqualTo(new[] { 30f, 3f, 2f, 50f }));
            Assert.That(c.friendlyFire, Is.False, "tir ami désactivé par défaut");
            Assert.That(c.splitHorizontal, Is.False, "gauche / droite par défaut");
            Assert.That(c.maxLocalPlayers, Is.EqualTo(2));
        }

        [Test]
        public void Seul_LaMortTue_EnCoop_ATerre()
        {
            PlayerHealth a = Player(Vector3.zero);
            a.TakeEnemyDamage(1000f, Vector3.zero, 1f);
            Assert.That(a.Dead && !a.Downed, Is.True, "seul : mort");
            a.Restore();
            PlayerHealth b = Player(new Vector3(5f, 0f, 0f));
            a.TakeEnemyDamage(1000f, Vector3.zero, 2f);
            Assert.That(a.Downed, Is.True, "un allié debout : à terre");
            Assert.That(a.Dead, Is.True, "compte comme mort (cibles, vagues, fin de partie)");
            Assert.That(b.OthersStanding(), Is.EqualTo(0), "plus personne debout à côté de b");
            b.Kill(Vector3.zero, 3f);
            Assert.That(b.Downed, Is.False, "le dernier debout meurt (plus d'allié pour le relever)");
        }

        [Test]
        public void Reanimation_Maintien3s_A2m_Revient50PV()
        {
            PlayerHealth a = Player(Vector3.zero), b = Player(new Vector3(1.5f, 0f, 0f));
            a.TakeEnemyDamage(1000f, Vector3.zero, 1f);
            var rev = b.gameObject.AddComponent<CoopRevive>();
            Invoke(rev, "Awake");
            rev.HeldOverride = false;
            rev.Step(5f);
            Assert.That(a.Downed, Is.True, "sans maintenir : rien");
            rev.HeldOverride = true;
            rev.Step(1.5f);
            Assert.That(rev.Target, Is.EqualTo(a));
            Assert.That(a.Downed, Is.True, "1.5 s : pas encore");
            rev.HeldOverride = false;
            rev.Step(0.1f);
            Assert.That(rev.Progress, Is.EqualTo(0f), "relâché : le compte repart de zéro");
            rev.HeldOverride = true;
            rev.Step(1.6f);
            rev.Step(1.6f);
            Assert.That(a.Downed, Is.False, "3 s : relevé");
            Assert.That(a.CurrentHealth, Is.EqualTo(50f));
            Assert.That(rev.Revives, Is.EqualTo(1));
            a.TakeEnemyDamage(1000f, Vector3.zero, 10f);
            b.transform.position = new Vector3(4f, 0f, 0f);
            rev.Step(5f);
            Assert.That(a.Downed, Is.True, "à 4 m : trop loin");
        }

        [Test]
        public void Spectateur_Apres30s_RevientALaZoneSuivante()
        {
            PlayerHealth a = Player(Vector3.zero), b = Player(new Vector3(5f, 0f, 0f));
            a.TakeEnemyDamage(1000f, Vector3.zero, 100f);
            a.TickDowned(129f);
            Assert.That(a.Spectating, Is.False, "29 s");
            a.TickDowned(130f);
            Assert.That(a.Spectating, Is.True, "30 s : spectateur");
            Assert.That(a.ReviveFromDown(50f), Is.False, "un spectateur ne se relève plus");
            a.Restore();
            Assert.That(a.Dead || a.Downed || a.Spectating, Is.False, "zone suivante : revenu");
        }

        [Test]
        public void FinDePartie_QuandTousSontATerreOuMorts()
        {
            var zgo = new GameObject("Zone (test)");
            _made.Add(zgo);
            var zone = zgo.AddComponent<ZoneState>();
            typeof(ZoneState).GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(zone, AssetDatabase.LoadAssetAtPath<ZoneSettings>("Assets/_Project/Settings/Zone_Test.asset"));
            Invoke(zone, "OnEnable");
            try
            {
                PlayerHealth a = Player(Vector3.zero), b = Player(new Vector3(5f, 0f, 0f));
                a.TakeEnemyDamage(1000f, Vector3.zero, 1f);
                Assert.That(zone.RunOver, Is.False, "un joueur debout : la partie continue");
                b.TakeEnemyDamage(1000f, Vector3.zero, 2f);
                Assert.That(zone.RunOver, Is.True, "tous à terre ou morts : écran de fin");
            }
            finally
            {
                Invoke(zone, "OnDisable");
            }
        }

        [Test]
        public void Ecran_GaucheDroite_OuHautBas()
        {
            Assert.That(SplitScreen.ViewRect(0, 1, false), Is.EqualTo(new Rect(0f, 0f, 1f, 1f)), "seul : tout l'écran");
            Assert.That(SplitScreen.ViewRect(0, 2, false), Is.EqualTo(new Rect(0f, 0f, 0.5f, 1f)), "1er à gauche");
            Assert.That(SplitScreen.ViewRect(1, 2, false), Is.EqualTo(new Rect(0.5f, 0f, 0.5f, 1f)), "2e à droite");
            Assert.That(SplitScreen.ViewRect(0, 2, true), Is.EqualTo(new Rect(0f, 0.5f, 1f, 0.5f)), "réglage : 1er en haut");
            Assert.That(SplitScreen.ViewRect(1, 2, true), Is.EqualTo(new Rect(0f, 0f, 1f, 0.5f)), "2e en bas");
        }

        [Test]
        public void InvulnerabiliteDeDebug_RemiseAZero_SortieDuPlayModeEtLancementDeRun()
        {
            System.Type hook = null;
            foreach (Assembly asm in System.AppDomain.CurrentDomain.GetAssemblies())
                hook ??= asm.GetType("ItHunts.EditorTools.DebugFlagsReset");
            Assert.That(hook, Is.Not.Null, "crochet de l'éditeur présent");
            try
            {
                PlayerHealth.DebugInvulnerable = true;
                hook.GetMethod("OnPlayModeChanged").Invoke(null, new object[] { PlayModeStateChange.ExitingPlayMode });
                Assert.That(PlayerHealth.DebugInvulnerable, Is.False, "sortie du Play Mode");

                PlayerHealth.DebugInvulnerable = true;
                var run = AssetDatabase.LoadAssetAtPath<ItHunts.Settings.RunDefinition>("Assets/_Project/Settings/Run_Default.asset");
                Assert.That(run, Is.Not.Null);
                ItHunts.Level.RunSession.Begin(run, 0);
                Assert.That(PlayerHealth.DebugInvulnerable, Is.False, "lancement d'une run");
            }
            finally
            {
                PlayerHealth.ResetDebugFlags();
                ItHunts.Level.RunSession.ResetForTests();
            }
        }

        [Test]
        public void Fov_AdapteALaMoitieDEcran()
        {
            var c = AssetDatabase.LoadAssetAtPath<CoopSettings>("Assets/_Project/Settings/Coop_Default.asset");
            Assert.That(SplitScreen.FovScaleFor(1, false, c), Is.EqualTo(1f), "seul : FOV normal");
            Assert.That(SplitScreen.FovScaleFor(2, false, c), Is.GreaterThan(1f), "gauche / droite : moitié étroite, un peu plus large");
            Assert.That(SplitScreen.FovScaleFor(2, true, c), Is.LessThan(1f), "haut / bas : moitié très large, resserré");
        }

        [Test]
        public void Scene_Niveau1_EcranPartage()
        {
            string text = System.IO.File.ReadAllText("Assets/Scenes/Level_01.unity");
            string guid = AssetDatabase.AssetPathToGUID("Assets/_Project/Scripts/Runtime/Player/SplitScreen.cs");
            Assert.That(text.Contains(guid), Is.True, "le niveau 1 accueille un 2e joueur");
        }
    }
}
