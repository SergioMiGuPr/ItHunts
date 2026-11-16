using System.Collections.Generic;
using System.Linq;
using ItHunts.Level;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Carte aléatoire (étape 8) : tirage déterministe, chaque couloir dans une boucle, sortie loin du départ, pentes plafonnées,
    /// niveau haut seulement pour les modules qui le déclarent, grandes salles ; kit (découpage en pièces, escalier lisse).
    /// </summary>
    public class RandomMapTests
    {
        private static ModuleInfo Info(int i, string name, ModuleKind kind = ModuleKind.Salle, float size = 56f, ModuleLevel level = ModuleLevel.Tous,
            Vector2Int? fp = null, float weight = 1f)
        {
            var f = fp ?? Vector2Int.one;
            var doors = new List<(DoorSide, int, bool)>();
            foreach (DoorSide s in new[] { DoorSide.Nord, DoorSide.Est, DoorSide.Sud, DoorSide.Ouest })
                for (int c = 0; c < (s == DoorSide.Nord || s == DoorSide.Sud ? f.x : f.y); c++)
                    doors.Add((s, c, false));
            return new ModuleInfo
            {
                Index = i, Name = name, Kind = kind, Size = size, Footprint = f, Level = level, Weight = weight, Rotate = true,
                ExitAllowed = kind == ModuleKind.Salle, ChestSpots = kind == ModuleKind.Salle ? 1 : 0, Doors = doors,
            };
        }

        private static List<ModuleInfo> Catalog() => new List<ModuleInfo>
        {
            Info(0, "A", size: 48f), Info(1, "B", size: 56f), Info(2, "C", size: 64f),
            Info(3, "Haute", size: 48f, level: ModuleLevel.Haut),
            Info(4, "Carrefour", ModuleKind.Carrefour, 20f),
            Info(5, "Grande", fp: new Vector2Int(1, 2)), Info(6, "Très grande", fp: new Vector2Int(2, 2)),
        };

        private static string Signature(MapPlan p) =>
            string.Join("|", p.Rooms.Select(r => $"{r.Module}{r.Cell}{r.Rot}{r.Center.y}")) + "#" + string.Join("|", p.Links.Select(l => $"{l.A}-{l.B}"))
            + $"#{p.Start}/{p.Exit}/{p.LairHost}#{string.Join(",", p.ChestRooms)}#{p.Annexes.Count}";

        [Test]
        public void Tirage_MemeSeed_MemeCarte_AutreSeed_AutreCarte()
        {
            var s = ScriptableObject.CreateInstance<MapLayoutSettings>();
            string a = Signature(MapPlanner.Plan(s, Catalog(), 1234)), b = Signature(MapPlanner.Plan(s, Catalog(), 1234));
            Assert.That(a, Is.EqualTo(b), "déterministe (jeu en ligne : chaque machine refait la même carte)");
            Assert.That(Signature(MapPlanner.Plan(s, Catalog(), 4321)), Is.Not.EqualTo(a));
            Object.DestroyImmediate(s);
        }

        [Test]
        public void Tirage_30Seeds_ReglesDuJeu()
        {
            var s = ScriptableObject.CreateInstance<MapLayoutSettings>();
            List<ModuleInfo> cat = Catalog();
            int big = 0;
            for (int seed = 1; seed <= 30; seed++)
            {
                MapPlan p = MapPlanner.Plan(s, cat, seed * 7919);
                Assert.That(p.Failures, Is.Empty, $"seed {seed}");
                Assert.That(p.Bridges(), Is.Empty, "chaque couloir dans une boucle");
                Assert.That(p.StartExitRooms, Is.GreaterThanOrEqualTo(s.minStartExitRooms), "sortie loin du départ");
                Assert.That(p.Links.All(l => l.Angle <= s.maxSlope + 0.01f), Is.True, "pentes plafonnées");
                Assert.That(p.LairHost, Is.GreaterThanOrEqualTo(0), "repaire posé");
                foreach (PlanRoom r in p.Rooms)
                    if (r.Center.y > 0.1f)
                        Assert.That(cat[r.Module].Level, Is.EqualTo(ModuleLevel.Haut), "niveau haut : seulement les modules qui le déclarent");
                int cells = p.Rooms.Sum(r => r.Cells.x * r.Cells.y);
                Assert.That(cells, Is.EqualTo(s.columns * s.rows), "toutes les cases couvertes, aucune deux fois");
                big += p.Rooms.Count(r => r.Cells.x * r.Cells.y > 1);
            }

            Assert.That(big, Is.GreaterThan(0), "grandes salles (1 × 2, 2 × 2) tirées");
            Object.DestroyImmediate(s);
        }

        [Test]
        public void Ponts_DuGraphe_Tarjan()
        {
            // Carré 0-1-2-3 (boucle) + 3-4 (pont).
            var e = new List<Vector2Int> { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(2, 3), new Vector2Int(3, 0), new Vector2Int(3, 4) };
            Assert.That(MapPlanner.Bridges(5, e), Is.EqualTo(new[] { 4 }));
            e.Add(new Vector2Int(4, 0));
            Assert.That(MapPlanner.Bridges(5, e), Is.Empty);
        }

        [Test]
        public void Kit_DecoupageEnPieces_EtEscalierLisse()
        {
            var seg = KitBuild.Segments(14f, 4f);
            Assert.That(seg.Count, Is.EqualTo(4), "14 m : 3 pièces entières + 1 raccourcie");
            Assert.That(seg[3].scale, Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(KitBuild.Segments(56f, 4f).All(x => Mathf.Approximately(x.scale, 1f)), Is.True, "56 m : pièces entières");

            var root = new GameObject("test").transform;
            try
            {
                GameObject ramp = KitBuild.Ramp(root, "Pente", Vector3.zero, Vector3.forward, 64f, 16f, 16f, null, 17f);
                float angle = -ramp.transform.localEulerAngles.x;
                Assert.That(Mathf.DeltaAngle(0f, angle), Is.EqualTo(Mathf.Atan2(16f, 64f) * Mathf.Rad2Deg).Within(0.01f), "collision lisse à l'angle de la pente");
                var box = ramp.GetComponent<BoxCollider>();
                Physics.SyncTransforms();
                Assert.That(Physics.Raycast(new Vector3(0f, 30f, 32f), Vector3.down, out RaycastHit h, 60f), Is.True);
                Assert.That(h.point.y, Is.EqualTo(8f).Within(0.05f), "au milieu, la surface est à mi-hauteur (pas de marche)");
                Assert.That(box, Is.Not.Null);
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }
    }
}
