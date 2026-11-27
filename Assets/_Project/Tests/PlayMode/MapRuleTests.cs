using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ItHunts.Enemies;
using ItHunts.Level.Acts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace ItHunts.Tests
{
    /// <summary>
    /// Règles des cartes de la nuit du 27/11 (playtest du 26/11), sur les 6 cartes faites à la main. Comptes rendus dans
    /// docs/design/nuit-6/cartes/.
    /// </summary>
    public class MapRuleTests
    {
        private static IEnumerable<TestCaseData> SealMaps() => ActTestMaps.Maps("Etanche");
        private static IEnumerable<TestCaseData> StairMaps() => ActTestMaps.Maps("PortesEscalier");

        private static string Report(string name, string text)
        {
            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "design", "nuit-6", "cartes"));
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, name + ".md"), text);
            return text;
        }

        /// <summary>
        /// Aucune salle fermée (coffre derrière une porte payée) n'est accessible autrement que par sa porte : remplissage de voxels de la
        /// taille d'un joueur accroupi depuis son sol, porte fermée (ActLevel.LockedRoomLeaks).
        /// </summary>
        [UnityTest, Timeout(600000), TestCaseSource(nameof(SealMaps))]
        public IEnumerator SallesFermees_SeulementParLaPorte(int act, int seed)
        {
            LogAssert.ignoreFailingMessages = true;
            yield return ActTestMaps.Load(act, seed);
            ActLevel level = ActLevel.Current;
            int rooms = level.LockedRoomCount();
            List<ActLevel.SealLeak> leaks = level.LockedRoomLeaks();
            Report($"etanche_{ActTestMaps.Scene}_s{seed}",
                $"# Salles fermées : {ActTestMaps.Scene}, graine {seed} ({ActTestMaps.MapName})\n\nSalles fermées contrôlées : {rooms}\n\n" +
                $"Fuites : {(leaks.Count == 0 ? "aucune" : string.Join("\n- ", leaks))}\n");
            Assert.Greater(rooms, 0, "aucune salle fermée sur cette carte");
            Assert.AreEqual(0, leaks.Count, string.Join(" ; ", leaks));
        }

        /// <summary>
        /// Portes en haut ou en bas d'un escalier : le vrai traqueur (ronde, sa locomotion, il se baisse) part de l'escalier, franchit la
        /// porte et revient, dans le temps d'une marche normale. Playtest du 26/11 : il restait bloqué par l'encadrement d'une porte.
        /// </summary>
        [UnityTest, Timeout(2400000), TestCaseSource(nameof(StairMaps))]
        public IEnumerator PortesEscalier_TraqueurPasse(int act, int seed)
        {
            LogAssert.ignoreFailingMessages = true;
            yield return ActTestMaps.Load(act, seed);
            ActLevel level = ActLevel.Current;
            var nav = Object.FindFirstObjectByType<ZoneNavMesh>();
            Stalker st = Object.FindFirstObjectByType<Stalker>(FindObjectsInactive.Include);
            Assert.IsNotNull(st, "pas de traqueur");
            st.gameObject.SetActive(true);
            // Joueurs loin de tout (le traqueur en ronde n'attrape personne, mais les goules ne viennent pas gêner la mesure).
            ItHunts.Player.PlayerHealth.DebugInvulnerable = true;
            List<ActLevel.StairDoor> doors = level.StairDoors(nav.StalkerAgent);
            var sb = new StringBuilder();
            var fails = new List<string>();
            Time.timeScale = 3f;
            foreach (ActLevel.StairDoor d in doors)
            {
                int legs0 = st.PatrolLegs;                           // avant la ronde : l'arrivée au départ compte pour 1
                st.StartPatrol(new[] { d.StairSide, d.OtherSide });
                st.WarpTo(d.StairSide, Quaternion.LookRotation(Flat(d.Center - d.StairSide), Vector3.up));
                yield return new WaitForFixedUpdate();
                // Deux marches (escalier vers l'autre côté, puis retour) ; échec s'il parcourt moins de 1.5 m en 8 s de jeu (blocage : un
                // détour qui s'éloigne d'abord du but n'en est pas un), ou si une marche dépasse 60 s.
                float t0 = Time.time;
                var trail = new StringBuilder();
                string fail = null;
                var legTimes = new List<float>();
                for (int leg = 0; leg < 2 && fail == null; leg++)
                {
                    Vector3 goal = leg == 0 ? d.OtherSide : d.StairSide;
                    float start = Time.time, nextTrail = Time.time, nextSample = Time.time;
                    var samples = new Queue<(float t, Vector3 p)>();
                    trail.Append(leg == 0 ? "aller : " : "; retour : ");
                    while (st.PatrolLegs < legs0 + 2 + leg)
                    {
                        yield return new WaitForFixedUpdate();
                        Vector3 at = st.transform.position;
                        float dist = Vector3.Distance(at, goal);
                        if (Time.time >= nextSample)
                        {
                            nextSample = Time.time + 1f;
                            samples.Enqueue((Time.time, at));
                            while (samples.Count > 0 && Time.time - samples.Peek().t > 8.01f)
                                samples.Dequeue();
                        }

                        bool still = samples.Count > 0 && Time.time - samples.Peek().t >= 7.9f && Vector3.Distance(samples.Peek().p, at) < 1.5f;

                        if (Time.time >= nextTrail)
                        {
                            nextTrail = Time.time + 2f;
                            trail.Append($"({at.x:0},{at.y:0},{at.z:0}{(st.Traversing ? " " + st.TraversalKind : "")}) ");
                        }

                        if (still)
                            fail = $"bloqué (marche {leg + 1}) au ({at.x:0.0}, {at.y:0.0}, {at.z:0.0}), à {dist:0.0} m du but : moins de 1.5 m en 8 s";
                        else if (Time.time - start > 60f)
                            fail = $"trop long (marche {leg + 1}) : plus de 60 s, au ({at.x:0.0}, {at.y:0.0}, {at.z:0.0})";
                        if (fail != null)
                            break;
                    }

                    legTimes.Add(Time.time - start);
                }

                string line = fail == null
                    ? $"passe {d} : {string.Join(" + ", legTimes.ConvertAll(x => x.ToString("0.0")))} s"
                    : $"ÉCHEC {d} : {fail} ; trajet {trail}";
                sb.Append("- ").Append(line).AppendLine();
                if (fail != null)
                    fails.Add(line);
            }

            st.PatrolPoints = null;
            Time.timeScale = 1f;
            Report($"portes_escalier_{ActTestMaps.Scene}_s{seed}",
                $"# Portes d'escalier : {ActTestMaps.Scene}, graine {seed} ({ActTestMaps.MapName})\n\n{doors.Count} portes en haut ou en bas d'un " +
                $"escalier ; le traqueur fait l'aller et le retour.\n\n{sb}");
            Assert.Greater(doors.Count, 0, "aucune porte d'escalier trouvée");
            Assert.AreEqual(0, fails.Count, string.Join("\n", fails));
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 1e-4f ? Vector3.forward : v.normalized;
        }

        [TearDown]
        public void TearDown() => ActTestMaps.TearDown();
    }
}
