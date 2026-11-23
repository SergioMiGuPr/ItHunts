using System.Collections.Generic;
using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Level;
using ItHunts.Level.Acts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Plan des actes (D-092, nuit 3, point 4) : salles organiques reliées salle à salle, toutes atteignables, portes à la taille du
    /// traqueur, rampes là où les sols diffèrent, aucune case partagée, déterministe ; report du stuff d'un acte à l'autre (point 3).
    /// </summary>
    public sealed class ActPlanTests
    {
        private static ActLayoutSettings Act(int i) => AssetDatabase.LoadAssetAtPath<ActLayoutSettings>($"Assets/_Project/Settings/Actes/Act_{i}.asset");

        private static IEnumerable<(int act, int seed)> Cases()
        {
            for (int a = 1; a <= 3; a++)
                foreach (int s in new[] { 11, 22, 33, 44, 55 })
                    yield return (a, s);
        }

        [Test]
        public void ChaqueActe_5Seeds_ToutesLesSallesAtteignables_SortieEtTraqueur()
        {
            foreach ((int act, int seed) in Cases())
            {
                ActPlan p = ActPlan.Make(Act(act), seed);
                Assert.That(p.Rooms.Count, Is.GreaterThanOrEqualTo(12), $"acte {act} seed {seed} : salles");
                int[] depth = p.Depths(p.Start);
                for (int i = 0; i < p.Rooms.Count; i++)
                    Assert.That(depth[i], Is.GreaterThanOrEqualTo(0), $"acte {act} seed {seed} : « {p.Rooms[i].Name} » isolée");
                Assert.That(p.Exit, Is.GreaterThanOrEqualTo(0), "salle de sortie");
                Assert.That(p.ExitHall, Is.GreaterThanOrEqualTo(0), "hall de sortie");
                Assert.That(p.StartExitDepth, Is.GreaterThanOrEqualTo(3), $"acte {act} seed {seed} : départ trop près de la sortie");
                if (Act(act).stalkerFromArrival)
                    Assert.That(p.Arrival, Is.GreaterThanOrEqualTo(0), $"acte {act} seed {seed} : porte d'arrivée du traqueur");
                else
                    Assert.That(p.Lair, Is.GreaterThanOrEqualTo(0), $"acte {act} seed {seed} : repaire");
            }
        }

        [Test]
        public void Portes_ALaTailleDuTraqueur_EtRampesEntreEtages()
        {
            foreach ((int act, int seed) in Cases())
            {
                ActPlan p = ActPlan.Make(Act(act), seed);
                foreach (ActDoor d in p.Doors)
                {
                    Assert.That(p.DoorWidth(d), Is.GreaterThanOrEqualTo(4f), "porte ≥ 4 m (traqueur baissé, D-080)");
                    Assert.That(Act(act).doorHeight, Is.GreaterThanOrEqualTo(5.2f));
                    // L'ouverture est sur une limite réelle entre les deux salles.
                    for (int k = 0; k < d.Length; k++)
                    {
                        Vector2Int a = d.First + d.Along * k;
                        Assert.That(p.OwnerAt(a), Is.EqualTo(d.A), $"acte {act} seed {seed} : case de A");
                        Assert.That(p.OwnerAt(a + d.Dir), Is.EqualTo(d.B), $"acte {act} seed {seed} : case de B");
                    }

                    bool stepped = Mathf.Abs(p.Rooms[d.A].Floor - p.Rooms[d.B].Floor) > 0.05f;
                    // Sols différents : une rampe, ou un palier (porte en hauteur, nuit 4) ; jamais rien.
                    Assert.That(d.RampRoom >= 0 || d.Landing, Is.EqualTo(stepped), $"acte {act} seed {seed} : rampe ou palier si et seulement si les sols diffèrent");
                    if (stepped && d.RampRoom >= 0)
                    {
                        float rise = Mathf.Abs(p.Rooms[d.A].Floor - p.Rooms[d.B].Floor);
                        float slope = Mathf.Atan2(rise, d.RampCellsDeep * p.Cell) * Mathf.Rad2Deg;
                        Assert.That(slope, Is.LessThanOrEqualTo(Act(act).rampSlope + 0.5f), "pente de rampe");
                    }

                    if (d.Landing)
                    {
                        int low = p.Rooms[d.A].Floor < p.Rooms[d.B].Floor ? d.A : d.B;
                        ActRoom r = p.Rooms[low];
                        Landing l = r.Landings.FirstOrDefault(x => x.Door == p.Doors.IndexOf(d));
                        Assert.IsNotNull(l, $"acte {act} seed {seed} : palier de la porte en hauteur");
                        Assert.That(r.TerraceHeight, Is.EqualTo(l.Height).Within(0.01f), "la terrasse du palier est à la hauteur de la salle haute");
                        Assert.That(l.Stairs.Count, Is.GreaterThanOrEqualTo(3), "escalier réservé");
                        foreach (Vector2Int c in l.Cells)
                            Assert.That(r.Use[c], Is.EqualTo(CellUse.Terrasse), "cases du palier");
                    }
                }
            }
        }

        [Test]
        public void ZeroImpasse_ChaqueSalleDeCombatADeuxSorties_Nuit4()
        {
            foreach ((int act, int seed) in Cases())
            {
                ActPlan p = ActPlan.Make(Act(act), seed);
                int deadEnds = p.DeadEnds;
                // Règle Pac-Man (recherche nuit 4) : au plus une impasse tolérée par carte (forme sans contact possible), signalée.
                Assert.That(deadEnds, Is.LessThanOrEqualTo(1), $"acte {act} seed {seed} : {deadEnds} salle(s) de combat en impasse : "
                    + string.Join(", ", p.Rooms.Where(r => !r.Special && r.Role != RoomRole.Depart && r.Doors.Count < 2).Select(r => r.Name)) + " | " + string.Join(" | ", p.DeadEndNotes));
                int loops = p.Doors.Count(d => d.Loop);
                Assert.That(loops, Is.GreaterThanOrEqualTo(4), $"acte {act} seed {seed} : {loops} boucles seulement");
                int landings = p.Doors.Count(d => d.Landing);
                int stepped = p.Doors.Count(d => Mathf.Abs(p.Rooms[d.A].Floor - p.Rooms[d.B].Floor) > 0.05f);
                TestContext.WriteLine($"acte {act} seed {seed} : {p.Rooms.Count} salles, {p.Doors.Count} portes, {loops} boucles, {stepped} portes entre étages dont {landings} paliers, {deadEnds} impasse(s)");
            }
        }

        [Test]
        public void Plan_Deterministe_EtSansCaseEnDouble()
        {
            ActPlan a = ActPlan.Make(Act(1), 77), b = ActPlan.Make(Act(1), 77);
            Assert.That(b.Rooms.Count, Is.EqualTo(a.Rooms.Count));
            Assert.That(b.Doors.Count, Is.EqualTo(a.Doors.Count));
            for (int i = 0; i < a.Rooms.Count; i++)
                Assert.That(b.Rooms[i].Cells.Count, Is.EqualTo(a.Rooms[i].Cells.Count));
            var seen = new HashSet<Vector2Int>();
            foreach (ActRoom r in a.Rooms)
                foreach (Vector2Int c in r.Cells)
                {
                    Assert.That(seen.Add(c), Is.True, "une case appartient à une seule salle");
                    Assert.That(a.OwnerAt(c), Is.EqualTo(r.Id));
                }
        }

        [Test]
        public void Fonctions_ChaqueSalleAUneIdentite()
        {
            foreach ((int act, int seed) in Cases())
            {
                ActPlan p = ActPlan.Make(Act(act), seed);
                var names = p.Rooms.Select(r => r.Name).ToList();
                Assert.That(names.Distinct().Count(), Is.EqualTo(names.Count), "noms uniques");
                Assert.That(p.Rooms.All(r => !string.IsNullOrEmpty(r.Function)), Is.True, "chaque salle a une fonction");
                // Pas deux salles voisines de même fonction (identité : aucune salle ne ressemble à sa voisine).
                foreach (ActDoor d in p.Doors)
                    if (!p.Rooms[d.A].Special && !p.Rooms[d.B].Special)
                        Assert.That(p.Rooms[d.A].Function, Is.Not.EqualTo(p.Rooms[d.B].Function), $"acte {act} seed {seed} : voisines identiques");
            }
        }

        [Test]
        public void Stuff_GardeDUnActeALAutre()
        {
            var go = new GameObject("Run (test)");
            try
            {
                var run = go.AddComponent<RunState>();
                run.Configure(null, 1234);
                run.AddSouls(1, 150);
                var up = ScriptableObject.CreateInstance<ItHunts.Settings.UpgradeDefinition>();
                run.Grant(1, up);
                RunCarry c = run.Snapshot();
                var go2 = new GameObject("Run acte II (test)");
                var run2 = go2.AddComponent<RunState>();
                run2.Restore(c);
                Assert.That(run2.Souls(1), Is.EqualTo(150));
                Assert.That(run2.UpgradesOf(1).Count, Is.EqualTo(1));
                Assert.That(run2.Seed, Is.EqualTo(1234), "même seed de run");
                Object.DestroyImmediate(go2);
                Object.DestroyImmediate(up);
                // Nouvelle scène : le joueur 1 a un autre identifiant (ici 777) ; le stuff le suit par son emplacement.
                var souls = new System.Collections.Generic.Dictionary<int, int> { [1] = 150 };
                var owned = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<ItHunts.Settings.UpgradeDefinition>>();
                var bought = new System.Collections.Generic.Dictionary<(int, ItHunts.Economy.PriceKind), int>();
                RunCarry.Remap(new System.Collections.Generic.Dictionary<int, int> { [1] = 0 }, new System.Collections.Generic.Dictionary<int, int> { [0] = 777 },
                    souls, owned, bought);
                Assert.That(souls.ContainsKey(777) && souls[777] == 150 && !souls.ContainsKey(1), Is.True, "âmes rattachées au nouveau joueur");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
