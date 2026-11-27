using System.Collections;
using System.Collections.Generic;
using ItHunts.Level;
using ItHunts.Level.Acts;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ItHunts.Tests
{
    /// <summary>Les 6 cartes faites à la main (3 actes, graines 11 et 22) : chargement commun des tests de la nuit du 27/11.</summary>
    public static class ActTestMaps
    {
        public const string RunAsset = "Assets/_Project/Settings/Run_Default.asset";

        public static IEnumerable<TestCaseData> Maps(string prefix)
        {
            for (int act = 0; act < 3; act++)
                foreach (int seed in new[] { 11, 22 })
                    yield return new TestCaseData(act, seed).SetName($"{prefix}_Acte{act + 1}_Seed{seed}").Returns(null);
        }

        /// <summary>Nom de la scène chargée par le dernier Load.</summary>
        public static string Scene { get; private set; }

        /// <summary>
        /// Charge l'acte à cette graine comme une vraie run (menu Playtest « Choisir l'acte ») et attend le niveau et le NavMesh. characters :
        /// personnage choisi par joueur (index dans la liste des personnages), null pour celui par défaut.
        /// </summary>
        public static IEnumerator Load(int act, int seed, int[] characters = null)
        {
#if UNITY_EDITOR
            RunSession.ResetForTests();
            RunSession.AdminMode = true;
            PlayerHealth.ResetDebugFlags();
            if (characters != null)
                RunSession.Characters = characters;
            var def = UnityEditor.AssetDatabase.LoadAssetAtPath<RunDefinition>(RunAsset);
            Assert.IsNotNull(def, "Run_Default introuvable");
            Scene = RunSession.BeginAt(def, 0, act);
            Assert.IsNotNull(Scene, "acte non prêt : " + act);
            if (characters != null)
                RunSession.Characters = characters;                       // BeginAt ne les touche pas ; posés deux fois par sûreté
            ActLevel.SeedOverride = seed;
            SceneManager.LoadScene(Scene);
            yield return null;
            float wait = Time.realtimeSinceStartup + 240f;
            while (Time.realtimeSinceStartup < wait && (ActLevel.Current == null || ActLevel.Current.Loading || ActLevel.Current.Root == null))
                yield return null;
            Assert.IsNotNull(ActLevel.Current, "niveau non construit");
            float navWait = Time.realtimeSinceStartup + 240f;
            ItHunts.Enemies.ZoneNavMesh nav = null;
            while (Time.realtimeSinceStartup < navWait && ((nav = Object.FindFirstObjectByType<ItHunts.Enemies.ZoneNavMesh>()) == null || !nav.Built))
                yield return null;
            Assert.IsTrue(nav != null && nav.Built, "NavMesh non construit");
            yield return new WaitForSecondsRealtime(1f);
#else
            yield break;
#endif
        }

        public static string MapName => ActLevel.Current != null && ActLevel.Current.Plan != null && ActLevel.Current.Plan.Hand != null
            ? ActLevel.Current.Plan.Hand.Name : "?";

        public static void TearDown()
        {
            RunSession.ResetForTests();
            ActLevel.SeedOverride = 0;
            Time.timeScale = 1f;
        }
    }
}
