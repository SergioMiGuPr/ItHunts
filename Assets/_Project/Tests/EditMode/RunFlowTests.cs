using System.IO;
using System.Text.RegularExpressions;
using ItHunts.Level;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Structure de la run et déroulé d'un niveau (étape 8, D-048).</summary>
    public class RunFlowTests
    {
        private RunDefinition _def;

        [SetUp]
        public void SetUp()
        {
            RunSession.ResetForTests();
            _def = ScriptableObject.CreateInstance<RunDefinition>();
            _def.levels.Add(new RunLevel { displayName = "Niveau 1", sceneName = "Level_01" });
            _def.levels.Add(new RunLevel { displayName = "Niveau 2" });
            _def.levels.Add(new RunLevel { displayName = "Niveau 3" });
            _def.levels.Add(new RunLevel { displayName = "Zone finale" });
            _def.difficulties.Add(new RunDifficulty { displayName = "Facile", playable = true, levelCount = 1 });
            _def.difficulties.Add(new RunDifficulty { displayName = "Moyenne", playable = false, levelCount = 2, enemyHealth = 1.5f, enemyDamage = 1.3f });
            _def.difficulties.Add(new RunDifficulty { displayName = "Difficile", playable = false, levelCount = 4, enemyHealth = 2f });
        }

        [TearDown]
        public void TearDown()
        {
            RunSession.ResetForTests();
            Object.DestroyImmediate(_def);
        }

        [Test]
        public void Facile_NiveauUnPuisVictoire()
        {
            Assert.That(RunSession.Begin(_def, 0), Is.EqualTo("Level_01"));
            Assert.That(RunSession.Active, Is.True);
            Assert.That(RunSession.EnemyHealth, Is.EqualTo(1f));
            Assert.That(RunSession.NextLevelScene(), Is.Null, "Facile : la run finit après le niveau 1");
            RunSession.End();
            Assert.That(RunSession.Active, Is.False);
        }

        [Test]
        public void DifficulteNonJouable_Refusee_SansRunMultiplicateursA1()
        {
            Assert.That(RunSession.Begin(_def, 1), Is.Null, "Moyenne grisée");
            Assert.That(RunSession.Begin(_def, 2), Is.Null, "Difficile grisée");
            Assert.That(RunSession.Active, Is.False);
            Assert.That(new[] { RunSession.EnemyHealth, RunSession.EnemyDamage, RunSession.EnemySpeed, RunSession.EnemyCount }, Is.All.EqualTo(1f),
                "scène de test ouverte directement : rien ne change");
        }

        [Test]
        public void Moyenne_NiveauSuivantSeulementSIlExiste_Multiplicateurs()
        {
            _def.difficulties[1].playable = true;
            Assert.That(RunSession.Begin(_def, 1), Is.EqualTo("Level_01"));
            Assert.That(RunSession.EnemyHealth, Is.EqualTo(1.5f));
            Assert.That(RunSession.EnemyDamage, Is.EqualTo(1.3f));
            Assert.That(RunSession.NextLevelScene(), Is.Null, "niveau 2 pas encore fait : fin de la run");
            _def.levels[1].sceneName = "Level_02";
            RunSession.Begin(_def, 1);
            Assert.That(RunSession.NextLevelScene(), Is.EqualTo("Level_02"));
            Assert.That(RunSession.LevelIndex, Is.EqualTo(1));
            Assert.That(RunSession.NextLevelScene(), Is.Null, "Moyenne : 2 niveaux");
        }

        [Test]
        public void Reglages_Etape8_TimersPorteEtPV()
        {
            var z = AssetDatabase.LoadAssetAtPath<ZoneSettings>("Assets/_Project/Settings/Zone_Test.asset");
            Assert.That(new[] { z.gracePeriod, z.exitUnlockAt, z.timer }, Is.EqualTo(new[] { 180f, 600f, 900f }), "traqueur 3 min, porte 10 min, Enragé 15 min");
            Assert.That(z.exitPrice, Is.EqualTo(200));
            Assert.That(z.timerWaitsForStartRoom && z.deathEndsRun, Is.True);
            var h = AssetDatabase.LoadAssetAtPath<PlayerHealthSettings>("Assets/_Project/Settings/PlayerHealth_Default.asset");
            Assert.That(h.maxHealth, Is.EqualTo(150f));
        }
    
        [Test]
        public void Piege_RateSansCombat_SeReArme()
        {
            var go = new GameObject("Piège (test)");
            try
            {
                var trap = go.AddComponent<BossTrap>();
                Assert.That(trap.Fire(), Is.False, "pas de combat : coup raté");
                Assert.That(trap.Sprung, Is.False);
                Assert.That(trap.Misses, Is.EqualTo(1));
                Assert.That(trap.Down, Is.True);
                trap.ResetTrap();
                Assert.That(trap.Down || trap.Misses > 0, Is.False, "remise à zéro : armé");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static int CountScript(string scenePath, string scriptPath)
        {
            string guid = AssetDatabase.AssetPathToGUID(scriptPath);
            Assert.That(guid, Is.Not.Empty, scriptPath);
            return Regex.Matches(File.ReadAllText(scenePath), $@"m_Script: \{{fileID: 11500000, guid: {guid}").Count;
        }

        [Test]
        public void Scene_Niveau1_Base_EtCopieRetravaillee()
        {
            const string basePath = "Assets/Scenes/Level_01_Base.unity", copy = "Assets/Scenes/Level_01.unity";
            Assert.That(File.Exists(basePath), Is.True, "lancer ItHunts > Générer le niveau 1");
            Assert.That(File.Exists(copy), Is.True, "copie à retravailler, jamais écrasée par le constructeur");
            const string rt = "Assets/_Project/Scripts/Runtime/";
            Assert.That(CountScript(basePath, rt + "Level/RoomVolume.cs"), Is.GreaterThan(150), "un volume par salle et par tronçon");
            Assert.That(CountScript(basePath, rt + "Level/ExitDoor.cs"), Is.EqualTo(1));
            Assert.That(CountScript(basePath, rt + "Economy/ArtifactMerchant.cs"), Is.EqualTo(1));
            Assert.That(CountScript(basePath, rt + "Economy/Merchant.cs"), Is.EqualTo(0), "plus de marchand placé au hasard (D-050)");
            Assert.That(CountScript(basePath, rt + "Level/DeepWater.cs"), Is.GreaterThanOrEqualTo(5), "canaux d'égout (D-052)");
            Assert.That(CountScript(basePath, rt + "Orbs/NonMagneticSurface.cs"), Is.GreaterThanOrEqualTo(20), "zones de parcours (D-051)");
            Assert.That(CountScript(basePath, rt + "Economy/Chest.cs"), Is.GreaterThanOrEqualTo(12));
            Assert.That(CountScript(basePath, rt + "Enemies/SpawnPoint.cs"), Is.GreaterThan(250), "couloirs (ceux des modules sont dans leurs prefabs)");
            Assert.That(File.ReadAllText(basePath).Contains("kind: 2"), Is.True, "une salle de départ (RoomKind.Depart)");
            Assert.That(CountScript(basePath, rt + "Level/BossFight.cs"), Is.EqualTo(1), "arène du combat de boss (D-049)");
            Assert.That(CountScript(basePath, rt + "Level/BossTrap.cs"), Is.EqualTo(3), "herse, pont, lustre");
            Assert.That(CountScript(basePath, rt + "Level/MapDiscovery.cs"), Is.EqualTo(1), "minimap (D-053)");
            Assert.That(System.Array.Exists(EditorBuildSettings.scenes, sc => sc.path == copy), Is.True, "Level_01 dans le build (la run la charge)");
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo("Assets/Scenes/MainMenu.unity"), "le menu d'abord");
        }
    }
}
