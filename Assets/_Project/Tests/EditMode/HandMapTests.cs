using System.IO;
using System.Linq;
using ItHunts.Level.Acts;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Cartes faites à la main (D-110) : chaque fichier se lit, se construit sans échec et respecte les règles des cartes.</summary>
    public class HandMapTests
    {
        private static string[] Files() => Directory.GetFiles("Assets/_Project/Settings/Cartes", "*.txt");

        private static ActPlan Build(string file, int seed)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            int act = name[4] - '0';
            var s = AssetDatabase.LoadAssetAtPath<ActLayoutSettings>($"Assets/_Project/Settings/Actes/Act_{act}.asset");
            return ActPlan.FromHand(s, HandMap.Parse(File.ReadAllText(file), name), seed);
        }

        [Test]
        public void ChaqueActe_AuMoinsDeuxCartes_Branchees()
        {
            for (int act = 1; act <= 3; act++)
            {
                var s = AssetDatabase.LoadAssetAtPath<ActLayoutSettings>($"Assets/_Project/Settings/Actes/Act_{act}.asset");
                Assert.GreaterOrEqual(s.handMaps.Count(m => m != null), act <= 1 ? 2 : 0, $"acte {act} : cartes faites à la main branchées");
            }
        }

        [Test]
        public void ChaqueCarte_SansErreur_SansImpasse_SurfaceCible([Values(11, 22, 33)] int seed)
        {
            Assert.Greater(Files().Length, 0, "aucune carte");
            foreach (string f in Files())
            {
                ActPlan p = Build(f, seed);
                string name = Path.GetFileNameWithoutExtension(f);
                Assert.IsEmpty(p.Failures, $"{name} : " + string.Join(" | ", p.Failures));
                Assert.GreaterOrEqual(p.Start, 0, name + " : départ");
                Assert.GreaterOrEqual(p.Exit, 0, name + " : sortie");
                Assert.GreaterOrEqual(p.ExitHall, 0, name + " : hall");
                Assert.GreaterOrEqual(p.Arrival, 0, name + " : porte d'arrivée du traqueur");
                foreach (ActRoom r in p.Rooms.Where(r => !r.Special))
                    Assert.GreaterOrEqual(r.Doors.Count, 2, $"{name} : {r.Name} n'a que {r.Doors.Count} sortie(s)");
                Assert.IsTrue(p.Depths(p.Start).All(d => d >= 0), name + " : salle inaccessible");
                // Surface : ×4 celle des actes tirés au hasard pour les actes I et II (≈ 44 000 et 48 000 m²), ×6 pour l'acte III.
                float area = p.Rooms.Sum(r => r.Area(p.Cell));
                int act = name[4] - '0';
                float target = act == 1 ? 174000f : act == 2 ? 190000f : 358000f;
                Assert.GreaterOrEqual(area, target, $"{name} : {area:0} m²");
                Assert.GreaterOrEqual(p.ChestAnchors.Count, 6, name + " : ancres de coffre");
                Assert.That(p.MerchantAnchors.Count, Is.InRange(2, 3), name + " : emplacements du marchand");
            }
        }

        [Test]
        public void Lecture_PorteHaute_PalierSansEscalier()
        {
            const string text = "nom: t\nacte: 1\nechelle: 8\n[plan]\n|AAAABBBB|\n|AAAABBBB|\n|AAAABBBB|\n|CCCCCCCC|\n|HH......|\n[salles]\n" +
                                "A : Départ ; sol 0 ; depart\nB : Haut ; sol 6\nC : Sortie ; sol 0 ; sortie\nH : Hall ; sol 0 ; hall\n" +
                                "[portes]\nA-B haute\nA-C\nB-C rampe\nC-H sortie\n";
            HandMap m = HandMap.Parse(text, "test");
            Assert.IsEmpty(m.Errors, string.Join(" | ", m.Errors));
            var s = AssetDatabase.LoadAssetAtPath<ActLayoutSettings>("Assets/_Project/Settings/Actes/Act_1.asset");
            ActPlan p = ActPlan.FromHand(s, m, 1);
            ActDoor high = p.Doors.First(d => d.High);
            ActRoom low = p.Rooms[p.RoomOfChar['A']];
            Assert.IsTrue(low.Landings.Any(l => l.Stairs.Count == 0 && l.Height > 5.9f), "palier à 6 m sans escalier dans la salle basse");
            Assert.IsTrue(high.Landing);
        }
    }
}
