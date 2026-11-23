using System.Collections;
using System.IO;
using System.Linq;
using ItHunts.Combat;
using ItHunts.Debugging;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Level.Acts;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ItHunts.Tests
{
    /// <summary>
    /// Parade en jeu (nuit 4, point 1) : dans l'acte I réel, une goule attaque le joueur ; la touche V est pressée par un clavier simulé
    /// de l'Input System 0.40 s après le début de l'annonce. Preuve demandée : la parade réussit par le vrai chemin (binding → lecteur →
    /// PlayerParry → ParryMath → statistique de la run), avec le debug en millisecondes, et trois captures autour de l'impact.
    /// </summary>
    public class ParryInGameTests
    {
        private const string RunAsset = "Assets/_Project/Settings/Run_Default.asset";

        [UnityTest, Timeout(600000)]
        public IEnumerator ParadeReussie_ParLaToucheV_DansLActeI()
        {
#if !UNITY_EDITOR
            Assert.Ignore("Test de l'éditeur.");
            yield break;
#else
            LogAssert.ignoreFailingMessages = true;
            Application.runInBackground = true;
            RunSession.ResetForTests();
            RunSession.AdminMode = true;
            RunSession.Characters = new[] { 1, -1 };
            var def = UnityEditor.AssetDatabase.LoadAssetAtPath<RunDefinition>(RunAsset);
            string scene = RunSession.BeginAt(def, 0, 0);
            ActLevel.SeedOverride = 11;
            SceneManager.LoadScene(scene);
            yield return null;
            float wait = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < wait && (PlayerHealth.All.Count == 0 || ActLevel.Current == null || ActLevel.Current.Loading || ActLevel.Current.Plan == null))
                yield return null;
            PlayerHealth p = PlayerHealth.All.First(x => x != null);
            PlayerHealth.DebugInvulnerable = true;
            var parry = p.GetComponent<PlayerParry>();
            Assert.IsNotNull(parry, "pas de PlayerParry sur le joueur");
            Assert.IsNotNull(parry.Settings, "PlayerParry sans réglages sur le prefab du joueur");
            var keyboard = InputSystem.AddDevice<Keyboard>("Clavier du test");
            keyboard.MakeCurrent();
            // Le clavier simulé est associé au joueur, comme le vrai clavier en partie (sinon son PlayerInput ne le lit pas).
            var pi = p.GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (pi != null && pi.user.valid)
                UnityEngine.InputSystem.Users.InputUser.PerformPairingWithDevice(keyboard, pi.user);
            var reader = p.GetComponent<ItHunts.Input.PlayerInputReader>();
            int pressesSeen = 0;
            var behavior = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            string outDir = Path.Combine(BotRunTests.OutDir, "parade");
            Directory.CreateDirectory(outDir);
            int parriesBefore = RunStats.Parries;
            var log = new System.Text.StringBuilder();
            try
            {
                // Le joueur quitte la salle de départ (la zone se réveille) : centre d'une salle de combat, puis une goule à 4 m devant lui.
                ActPlan plan = ActLevel.Current.Plan;
                ActRoom room = plan.Rooms.First(r => r.Role == RoomRole.Normale && r.Feature == RoomFeature.Aucun);
                Vector3 spot = plan.CellCenter(System.Linq.Enumerable.First(room.Cells.Where(c => room.Use[c] == CellUse.Sol || room.Use[c] == CellUse.Passage)
                    .OrderBy(c => (c - room.CenterCell).sqrMagnitude)), room.Floor) + Vector3.up * 0.2f;
                yield return new WaitForSecondsRealtime(1.5f);   // la zone se remet à zéro (Start) juste après le chargement
                p.Motor.Teleport(spot);
                ZoneState zone = Object.FindFirstObjectByType<ZoneState>();
                float wake = Time.realtimeSinceStartup + 10f;
                while (!zone.Started && Time.realtimeSinceStartup < wake)
                    yield return null;
                ActLevel.Current.SpawnEnemy(false, spot + p.transform.forward * 4f);
                yield return new WaitForSecondsRealtime(0.5f);
                Ghoul g = Ghoul.All.OrderBy(x => Vector3.Distance(x.Position, p.transform.position)).First();
                var look = p.GetComponent<PlayerLook>();
                int attempts = 0, successes = 0, seenStrikes = g.Strikes;
                float deadline = Time.realtimeSinceStartup + 180f;
                while (Time.realtimeSinceStartup < deadline && successes < 3 && attempts < 12)
                {
                    if (g == null || g.GetComponent<Damageable>().Dead)
                        break;
                    Vector3 to = g.Position - p.transform.position;
                    look.SetView(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 5f);
                    if (g.Strikes == seenStrikes)
                    {
                        if (Time.frameCount % 300 == 0)
                            log.AppendLine($"  (état de la goule : {g.Mode}, chasse {g.Hunting}, cible {(g.Target != null ? g.Target.name : "aucune")}, à {Vector3.Distance(g.Position, p.transform.position):0.0} m, joueur {p.transform.position:F1} (visé {spot:F1}), joueurs {PlayerHealth.All.Count}, zone démarrée {zone.Started}, endormie {ZoneState.Asleep}, coups {g.Strikes}, branche {g.DebugBranch})");
                        yield return null;
                        continue;
                    }

                    // Un coup commence (impact à +0.55 s, D-096) : appui sur V à +0.40 s, dans la fenêtre de 0.25 s avant l'impact.
                    seenStrikes = g.Strikes;
                    float signal = Time.time;
                    attempts++;
                    ActCapture.Save(Camera.main, Path.Combine(outDir, $"parade_{attempts}_1_annonce.jpg"));
                    while (Time.time - signal < 0.40f)
                        yield return null;
                    float pressBefore = parry.State.LastPress;
                    // Comme un vrai clavier : l'événement attend la mise à jour de l'Input System du début de l'image suivante (un appel
                    // manuel à InputSystem.Update() le consommerait dans une mise à jour à part : « pressé cette image » serait faux).
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.V));
                    yield return null;
                    yield return null;
                    if (parry.State.LastPress > pressBefore)
                        pressesSeen++;
                    ActCapture.Save(Camera.main, Path.Combine(outDir, $"parade_{attempts}_2_appui.jpg"));
                    InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                    float t1 = Time.time + 0.5f;
                    while (Time.time < t1)
                        yield return null;
                    ActCapture.Save(Camera.main, Path.Combine(outDir, $"parade_{attempts}_3_apres.jpg"));
                    string line = ParryMath.Describe(parry.State, parry.Settings);
                    log.AppendLine($"- essai {attempts} : {line} ; appui lu par le joueur : {(parry.State.LastPress > signal ? $"oui, {Mathf.RoundToInt((parry.State.LastPress - signal) * 1000f)} ms après le début du coup" : "non")} ; statistique : {RunStats.Parries - parriesBefore}");
                    if (parry.State.LastResult == ParryResult.Reussie)
                        successes++;
                    float t2 = Time.time + 2.5f;
                    while (Time.time < t2)
                        yield return null;
                    seenStrikes = g.Strikes;
                }

                File.WriteAllText(Path.Combine(outDir, "PARADE.md"),
                    $"# Parade en jeu ({System.DateTime.Now:yyyy-MM-dd HH:mm})\n\nActe I, seed 11, salle de combat, goule posée à 4 m, touche V simulée par l'Input System (événement clavier) 0.40 s après le début du coup.\n\n{log}\nRéussies : {successes} / {attempts} ; RunStats.Parries : {RunStats.Parries - parriesBefore}.\n");
                Assert.Greater(attempts, 0, "la goule n'a jamais annoncé de coup : " + log);
                Assert.Greater(successes, 0, "aucune parade réussie :\n" + log);
                Assert.Greater(RunStats.Parries - parriesBefore, 0, "la statistique de la run n'a pas compté la parade");
            }
            finally
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = behavior;
                InputSystem.RemoveDevice(keyboard);
                ActLevel.SeedOverride = 0;
                RunSession.ResetForTests();
                PlayerHealth.DebugInvulnerable = false;
            }
#endif
        }
    }
}
