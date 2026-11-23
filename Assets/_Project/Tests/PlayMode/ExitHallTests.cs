using System.Collections;
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
    /// Porte de sortie → acte suivant (nuit 4, point 1 : « porte qui n'envoie pas à l'acte suivant »). Dans chaque acte réel, pour 3 seeds :
    /// la porte est ouverte (payée), le joueur avance de la porte vers le hall de sortie pas à pas ; l'acte doit se terminer (ZoneState.Won)
    /// avant 12 m parcourus ; sinon le test décrit la géométrie (déclencheur, hall, obstacles).
    /// </summary>
    public class ExitHallTests
    {
        private const string RunAsset = "Assets/_Project/Settings/Run_Default.asset";

        private static IEnumerable Cases()
        {
            for (int act = 0; act < 3; act++)
                foreach (int seed in BotRunTests.Seeds)
                    yield return new TestCaseData(act, seed).SetName($"PorteDeSortie_Acte{act + 1}_Seed{seed}").Returns(null);
        }

        [UnityTest, Timeout(600000), TestCaseSource(nameof(Cases))]
        public IEnumerator PorteDeSortie_TermineLActe(int act, int seed)
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
            string scene = RunSession.BeginAt(def, 0, act);
            ActLevel.SeedOverride = seed;
            SceneManager.LoadScene(scene);
            yield return null;
            float wait = Time.realtimeSinceStartup + 90f;
            while (Time.realtimeSinceStartup < wait && (PlayerHealth.All.Count == 0 || ActLevel.Current == null || ActLevel.Current.Loading || ActLevel.Current.Plan == null))
                yield return null;
            PlayerHealth p = PlayerHealth.All.First(x => x != null);
            PlayerHealth.DebugInvulnerable = true;
            ActLevel level = ActLevel.Current;
            ZoneState zone = Object.FindFirstObjectByType<ZoneState>();
            Assert.IsNotNull(level.ExitDoorObject, "pas de porte de sortie");
            ActPlan plan = level.Plan;
            ActRoom hall = plan.Rooms[plan.ExitHall];
            Vector3 door = level.ExitDoorObject.transform.position;
            Vector3 hallCenter = plan.CellCenter(hall.CenterCell, hall.Floor);
            Vector3 dir = hallCenter - door;
            dir.y = 0f;
            dir.Normalize();
            // Le joueur quitte la salle de départ (le timer démarre), puis la porte est ouverte comme si elle était payée.
            yield return new WaitForSecondsRealtime(1.5f);   // la zone se remet à zéro (Start) juste après le chargement
            p.Motor.Teleport(level.ExitPoint + Vector3.up * 0.2f);
            yield return new WaitForSeconds(0.5f);
            zone.DebugAdvance(1f);
            // Relecture de la nuit 4 : porte fermée, un joueur posé dans la boîte du hall ne termine pas l'acte.
            var closedTrigger = Object.FindObjectsByType<ZoneExit>(FindObjectsSortMode.None).FirstOrDefault();
            if (closedTrigger != null)
            {
                p.Motor.Teleport(closedTrigger.GetComponent<BoxCollider>().bounds.center - Vector3.up * 1.5f);
                yield return new WaitForSeconds(0.4f);
                Assert.IsFalse(zone.Won, $"acte {act + 1} seed {seed} : acte gagné porte fermée");
                p.Motor.Teleport(level.ExitPoint + Vector3.up * 0.2f);
                yield return new WaitForSeconds(0.3f);
            }

            level.ExitDoorObject.SetOpen(true);
            yield return new WaitForSeconds(0.3f);
            var trigger = Object.FindObjectsByType<ZoneExit>(FindObjectsSortMode.None).FirstOrDefault();
            Assert.IsNotNull(trigger, "pas de déclencheur de fin d'acte");
            var box = trigger.GetComponent<BoxCollider>();
            string geo = $"porte {door:F1}, point devant {level.ExitPoint:F1}, centre du hall {hallCenter:F1} (sol {hall.Floor}), déclencheur {box.bounds.center:F1} taille {box.bounds.size:F1}, joueur y {p.transform.position.y:F2}";
            float walked = 0f;
            var outDir = System.IO.Path.Combine(BotRunTests.OutDir, "porte");
            System.IO.Directory.CreateDirectory(outDir);
            ActCapture.Save(null, System.IO.Path.Combine(outDir, $"acte{act + 1}_s{seed}_0_devant.jpg"));
            // Le joueur marche vraiment (entrée « avancer », comme au clavier) : un CharacterController téléporté ne déclenche pas les zones.
            var reader = p.GetComponent<ItHunts.Input.PlayerInputReader>();
            var look = p.GetComponent<PlayerLook>();
            look.SetView(Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg, 0f);
            reader.Override = _ => new ItHunts.Input.PlayerInputReader.Frame { Move = Vector2.up };
            Vector3 from = p.transform.position;
            float until = Time.time + 8f;
            while (Time.time < until && !zone.Finished)
                yield return null;
            reader.Override = null;
            walked = Vector3.Distance(from, p.transform.position);
            geo += $" ; arrivé à {p.transform.position:F1}, dans la boîte {box.bounds.Contains(p.transform.position + Vector3.up)}, couches joueur {LayerMask.LayerToName(p.gameObject.layer)} / boîte {LayerMask.LayerToName(box.gameObject.layer)}, collision {!Physics.GetIgnoreLayerCollision(p.gameObject.layer, box.gameObject.layer)}, zone démarrée {zone.Started}, finie {zone.Finished}, boîte active {box.enabled && box.gameObject.activeInHierarchy}";
            ActCapture.Save(null, System.IO.Path.Combine(outDir, $"acte{act + 1}_s{seed}_1_hall.jpg"));
            yield return new WaitForSeconds(0.5f);
            // La fin d'acte s'affiche puis la scène suivante se charge après le délai : on vérifie seulement la victoire de la zone.
            ActLevel.SeedOverride = 0;
                RunSession.ResetForTests();
            PlayerHealth.DebugInvulnerable = false;
            Assert.IsTrue(zone.Won, $"acte {act + 1} seed {seed} : la porte ouverte ne termine pas l'acte après {walked:0.0} m vers le hall ; {geo}");
            TestContext.WriteLine($"acte {act + 1} seed {seed} : acte terminé après {walked:0.0} m ; {geo}");
#endif
        }
    }
}
