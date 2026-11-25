using System.Collections;
using System.Collections.Generic;
using System.IO;
using ItHunts.Level;
using ItHunts.Level.Acts;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ItHunts.Tests
{
    /// <summary>
    /// Revue des cartes de la DA (jour 5, D-115) : sur chaque carte faite à la main (deux par acte, graines 11 et 22), plus aucun défaut
    /// de décor une fois le niveau construit : lumière murale sans mur ou plantée dans un prop, suspendu sans plafond, lustres à moins de
    /// 6 m, props qui se chevauchent, décor de plus de 0.8 m sans collision, forme provisoire visible. Compte rendu (corrections faites à
    /// la construction et défauts restants) dans docs/design/jour-5/decor/.
    /// </summary>
    public class DecorAuditTests
    {
        private const string RunAsset = "Assets/_Project/Settings/Run_Default.asset";

        private static IEnumerable<TestCaseData> Maps()
        {
            for (int act = 0; act < 3; act++)
                foreach (int seed in new[] { 11, 22 })
                    yield return new TestCaseData(act, seed).SetName($"Decor_Acte{act + 1}_Seed{seed}").Returns(null);
        }

        [UnityTest, Timeout(600000), TestCaseSource(nameof(Maps))]
        public IEnumerator AucunDefautDeDecor(int act, int seed)
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test de l'éditeur.");
            yield break;
#else
            LogAssert.ignoreFailingMessages = true;
            RunSession.ResetForTests();
            RunSession.AdminMode = true;
            PlayerHealth.ResetDebugFlags();
            var def = UnityEditor.AssetDatabase.LoadAssetAtPath<RunDefinition>(RunAsset);
            Assert.IsNotNull(def, "Run_Default introuvable");
            string scene = RunSession.BeginAt(def, 0, act);
            Assert.IsNotNull(scene, "acte non prêt : " + act);
            ActLevel.SeedOverride = seed;
            SceneManager.LoadScene(scene);
            yield return null;
            float wait = Time.realtimeSinceStartup + 240f;
            while (Time.realtimeSinceStartup < wait && (ActLevel.Current == null || ActLevel.Current.Loading || ActLevel.Current.Root == null))
                yield return null;
            ActLevel level = ActLevel.Current;
            Assert.IsNotNull(level, "niveau non construit");
            yield return null;
            DecorReport left = DecorAudit.Run(level.Root, false);
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "design", "jour-5", "decor"));
            Directory.CreateDirectory(dir);
            string map = level.Plan != null && level.Plan.Hand != null ? level.Plan.Hand.Name : "?";
            // Traqueur : il atteint chaque zone accessible au joueur (0 point manqué), une fois le NavMesh construit.
            float navWait = Time.realtimeSinceStartup + 240f;
            ItHunts.Enemies.ZoneNavMesh nav = null;
            while (Time.realtimeSinceStartup < navWait && ((nav = Object.FindFirstObjectByType<ItHunts.Enemies.ZoneNavMesh>()) == null || !nav.Built))
                yield return null;
            yield return new WaitForSecondsRealtime(1f);
            var reach = ItHunts.Debugging.StalkerReachAudit.Run(level);
            File.WriteAllText(Path.Combine(dir, $"{scene}_s{seed}.md"),
                $"# Décor : {scene}, graine {seed} ({map})\n\nCorrigé à la construction : {level.DecorFixes}\n\nRestant : {left.Text(200)}\n\n" +
                $"Traqueur : {reach.Summary()}\n\nÎlots de son NavMesh reliés : {nav?.IslandBridges} (restés seuls : {nav?.IslandsLeft}, {nav?.IslandMilliseconds} ms)\n" +(reach.Misses.Count > 0 ? $"\nPoints :\n\n{reach.All()}\n" : ""));
            Assert.AreEqual(0, left.Total, $"{scene} graine {seed} ({map}) : {left.Text(30)}");
            Assert.Greater(reach.Samples, 0, "audit du traqueur : aucun point");
            Assert.AreEqual(0, reach.Misses.Count, $"{scene} graine {seed} ({map}) : traqueur, {reach.Summary()}");
#endif
        }

        [TearDown]
        public void TearDown()
        {
            ActLevel.SeedOverride = 0;
            Time.timeScale = 1f;
        }
    }
}
