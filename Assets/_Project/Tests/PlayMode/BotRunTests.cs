using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItHunts.Debugging;
using ItHunts.Enemies;
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
    /// Playtest automatisé (nuit 4) : le bot joue un acte du début à la fin (déplacements, coffres, marchand, combats, traqueur, porte de
    /// sortie, hall) sur une seed, puis le combat de boss et l'écran de fin ; une run complète enchaîne les quatre scènes. Chaque test écrit
    /// son compte rendu dans docs/design/nuit-4/bot/. Tout blocage, chute hors de la carte, porte sans suite, exception ou erreur du journal
    /// fait échouer le test. Vitesse du temps : Library/ithunts-bot.json { "timeScale": 3 } (3 par défaut).
    /// </summary>
    public class BotRunTests
    {
        private const string RunAsset = "Assets/_Project/Settings/Run_Default.asset";
        public static readonly int[] Seeds = { 11, 22, 33 };

        public static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "design", "nuit-4", "bot"));

        [System.Serializable]
        private sealed class Config
        {
            public float timeScale = 3f;
            public int character = 1;                                      // 0 renard, 1 caméléon, 2 koala, 3 hibou
            public string tag = "";
            public bool captures = false;
        }

        private static Config Load()
        {
            string path = Path.Combine(Application.dataPath, "..", "Library", "ithunts-bot.json");
            try
            {
                if (File.Exists(path))
                    return JsonUtility.FromJson<Config>(File.ReadAllText(path)) ?? new Config();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Bot : config illisible : " + e.Message);
            }

            return new Config();
        }

        private static IEnumerable<TestCaseData> Acts()
        {
            for (int act = 0; act < 3; act++)
                foreach (int seed in Seeds)
                    yield return new TestCaseData(act, seed).SetName($"Acte{act + 1}_Seed{seed}").Returns(null);
        }

        private static IEnumerable<TestCaseData> BossSeeds()
        {
            foreach (int seed in Seeds)
                yield return new TestCaseData(seed).SetName($"Boss_Seed{seed}").Returns(null);
        }

        [UnityTest, Timeout(1500000), TestCaseSource(nameof(Acts))]
        public IEnumerator Acte(int act, int seed)
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test de l'éditeur.");
            yield break;
#else
            yield return Play(act, seed, chain: false);
#endif
        }

        [UnityTest, Timeout(1500000), TestCaseSource(nameof(BossSeeds))]
        public IEnumerator Boss(int seed)
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test de l'éditeur.");
            yield break;
#else
            yield return Play(3, seed, chain: false);
#endif
        }

        [UnityTest, Timeout(3600000)]
        public IEnumerator RunComplete_Seed11()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test de l'éditeur.");
            yield break;
#else
            yield return Play(0, 11, chain: true);
#endif
        }

#if UNITY_EDITOR
        private readonly List<BotReport> _reports = new List<BotReport>();

        private IEnumerator Play(int act, int seed, bool chain)
        {
            Config cfg = Load();
            LogAssert.ignoreFailingMessages = true;
            Application.runInBackground = true;
            RunSession.ResetForTests();
            RunSession.AdminMode = true;
            RunSession.Characters = new[] { cfg.character, -1 };
            PlayerHealth.ResetDebugFlags();
            var def = UnityEditor.AssetDatabase.LoadAssetAtPath<RunDefinition>(RunAsset);
            Assert.IsNotNull(def, "Run_Default introuvable");
            string scene = RunSession.BeginAt(def, 0, act);
            Assert.IsNotNull(scene, "acte non prêt : " + act);
            ActLevel.SeedOverride = seed;
            _reports.Clear();
            Directory.CreateDirectory(OutDir);
            float t0 = Time.realtimeSinceStartup;
            SceneManager.LoadScene(scene);
            yield return null;
            Time.timeScale = Mathf.Clamp(cfg.timeScale, 0.5f, 8f);
            string lastScene = "";
            PlaytestBot bot = null;
            BotReport report = null;
            bool endSeen = false;
            try
            {
                while (true)
                {
                    Scene active = SceneManager.GetActiveScene();
                    if (active.name != lastScene)
                    {
                        // Nouvelle scène : attendre le joueur et la fin du chargement, poser le bot.
                        lastScene = active.name;
                        bot = null;
                        if (active.name == RunSession.MenuScene)
                            break;                                       // campement : la run est finie (écran de fin → campement)
                        float wait = Time.realtimeSinceStartup + 60f;
                        while (Time.realtimeSinceStartup < wait)
                        {
                            PlayerHealth p = PlayerHealth.All.FirstOrDefault(x => x != null);
                            bool loading = ActLevel.Current != null && ActLevel.Current.Loading;
                            if (p != null && !loading && Object.FindFirstObjectByType<ZoneState>() != null)
                            {
                                report = new BotReport { Seed = seed, Scene = active.name, Act = active.name.StartsWith("Acte_") && int.TryParse(active.name.Substring(5, 1), out int an) ? an - 1 : act };
                                bot = PlaytestBot.Attach(p, report);
                                _reports.Add(report);
                                break;
                            }

                            yield return null;
                        }

                        Assert.IsNotNull(bot, $"pas de joueur dans {active.name} après 60 s");
                        yield return null;
                        continue;
                    }

                    if (bot == null)
                    {
                        yield return null;
                        continue;
                    }

                    ZoneState zone = Object.FindFirstObjectByType<ZoneState>();
                    if (RunEndScreen.Current != null && !endSeen)
                    {
                        endSeen = true;
                        report.Note("fin", "écran de fin affiché : " + (RunEndScreen.Current.Victory ? "victoire" : "mort"));
                        if (cfg.captures)
                            ActCapture.Save(Camera.main, Path.Combine(OutDir, $"fin_de_run_s{seed}.jpg"));
                        yield return new WaitForSecondsRealtime(1f);
                        // Les deux boutons : Campement (vérifié ici), Recommencer (vérifié par RunComplete : retour à l'acte I).
                        if (chain)
                            RunEndScreen.Current.ToCamp();
                        else
                            break;
                        yield return null;
                        continue;
                    }

                    if (zone != null && zone.Finished && !chain)
                    {
                        // Acte gagné : la scène suivante va se charger ; on s'arrête ici (un acte à la fois).
                        yield return new WaitForSecondsRealtime(0.5f);
                        report.Note("acte", zone.Won ? "acte gagné" : "zone finie sans victoire");
                        if (!zone.Won)
                            report.Fail("acte", "la zone s'est finie sans victoire (mort ?)");
                        break;
                    }

                    if (bot.Done && !chain)
                        break;
                    float limit = act == 3 && !chain ? 540f : 1400f;   // boss : 9 min réelles au plus (premier plan)
                    if (Time.realtimeSinceStartup - t0 > limit)
                    {
                        report.Fail("durée", $"le bot n'a pas fini en {limit:0} s réelles");
                        break;
                    }

                    yield return null;
                }
            }
            finally
            {
                var fb = FinalBoss.Current_;
                if (fb != null && report != null)
                    report.Note("boss", $"PV du boss à la fin : {fb.Health:0} / {fb.MaxHealth:0} (phase {fb.Phase}, {fb.AttacksDone} attaques)");
                Time.timeScale = 1f;
                ActLevel.SeedOverride = 0;
                RunSession.ResetForTests();
                foreach (BotReport r in _reports)
                    r.Close();
                string name = chain ? $"run_complete_s{seed}" : $"{SceneFor(act)}_s{seed}";
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"# Bot : {name} ({System.DateTime.Now:yyyy-MM-dd HH:mm}){(string.IsNullOrEmpty(cfg.tag) ? "" : " · " + cfg.tag)}");
                sb.AppendLine();
                foreach (BotReport r in _reports)
                    sb.AppendLine(r.ToMarkdown());
                File.WriteAllText(Path.Combine(OutDir, name + ".md"), sb.ToString());
                if (bot != null)
                    Object.Destroy(bot);
            }

            foreach (BotReport r in _reports)
                Assert.IsTrue(r.Passed, $"{r.Scene} : {string.Join(" | ", r.Failures.Concat(r.Errors).Take(6))}");
        }

        private static string SceneFor(int act) => act switch { 0 => "Acte_1_Caves", 1 => "Acte_2_Chateau", 2 => "Acte_3_Jardin", _ => "Acte_4_Boss" };
#endif
    }

    /// <summary>Captures à points fixes de chaque salle (nuit 4) : docs/design/nuit-4/captures/&lt;tag&gt;/&lt;scène&gt;_s&lt;seed&gt;/.</summary>
    public class ActCaptureTests
    {
        private const string RunAsset = "Assets/_Project/Settings/Run_Default.asset";

        private static IEnumerable<TestCaseData> Cases()
        {
            for (int act = 0; act < 3; act++)
                foreach (int seed in BotRunTests.Seeds)
                    yield return new TestCaseData(act, seed).SetName($"Captures_Acte{act + 1}_Seed{seed}").Returns(null);
        }

        [UnityTest, Timeout(600000), TestCaseSource(nameof(Cases))]
        public IEnumerator Captures(int act, int seed)
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test de l'éditeur.");
            yield break;
#else
            LogAssert.ignoreFailingMessages = true;
            Application.runInBackground = true;
            string tagPath = Path.Combine(Application.dataPath, "..", "Library", "ithunts-capture-tag.txt");
            string tag = File.Exists(tagPath) ? File.ReadAllText(tagPath).Trim() : "apres";
            RunSession.ResetForTests();
            RunSession.AdminMode = true;
            RunSession.Characters = new[] { 1, -1 };
            var def = UnityEditor.AssetDatabase.LoadAssetAtPath<RunDefinition>(RunAsset);
            string scene = RunSession.BeginAt(def, 0, act);
            ActLevel.SeedOverride = seed;
            SceneManager.LoadScene(scene);
            yield return null;
            float wait = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < wait && (PlayerHealth.All.Count == 0 || (ActLevel.Current != null && ActLevel.Current.Loading)))
                yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            string folder = Path.Combine(ActCapture.Root, tag, $"{SceneManager.GetActiveScene().name}_s{seed}");
            int n = 0;
            yield return ActCapture.CaptureAll(folder, k => n = k);
            ActLevel.SeedOverride = 0;
                RunSession.ResetForTests();
            Assert.Greater(n, 0, "aucune capture");
            Debug.Log($"Captures : {n} images dans {folder}");
#endif
        }
    }
}
