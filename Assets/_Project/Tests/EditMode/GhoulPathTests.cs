using ItHunts.Enemies;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Goules après le playtest des étapes 8 et 9 : long trajet par étapes (MazeGraph), seuils des portes des pièces, escalade
    /// vers un joueur hors d'atteinte, déblocage (réglages).
    /// </summary>
    public class GhoulPathTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
        }

        /// <summary>A (0, 0) — B (100, 0) — C (200, 0), et D (100, 100) relié à B : salles de 56 m.</summary>
        private MazeGraph Graph()
        {
            _go = new GameObject("Graphe (test)");
            var g = _go.AddComponent<MazeGraph>();
            Vector3 h = new Vector3(28f, 18f, 28f);
            g.Configure(new[] { "A", "B", "C", "D" },
                new[] { new Vector3(0f, 0f, 0f), new Vector3(100f, 0f, 0f), new Vector3(200f, 0f, 0f), new Vector3(100f, 0f, 100f) },
                new[] { h, h, h, h },
                new[] { new Vector2Int(0, 1), new Vector2Int(1, 2), new Vector2Int(1, 3) });
            return g;
        }

        [Test]
        public void LongTrajet_EntreeDeLaSalleSuivante_JamaisDeRetourAuSeuil()
        {
            MazeGraph g = Graph();
            int last = -1;
            Vector3 target = new Vector3(200f, 0f, 5f);                   // dans C
            Assert.That(g.Waypoint(new Vector3(0f, 0f, 0f), target, ref last, out Vector3 p), Is.True);
            Assert.That(last, Is.EqualTo(0), "dernière salle traversée : A");
            Assert.That(p, Is.EqualTo(g.Entry(1, 0)), "l'entrée de B côté A (pas son centre : une fosse peut l'occuper)");
            Assert.That(p.x, Is.EqualTo(100f - 28f * 0.75f).Within(1e-3f));

            // Juste sorti de A (dans le couloir A–B, hors de la boîte de A) : toujours vers B, jamais de retour vers A.
            Assert.That(g.Waypoint(new Vector3(30f, 0f, 0f), target, ref last, out p), Is.True);
            Assert.That(p, Is.EqualTo(g.Entry(1, 0)));
            Assert.That(last, Is.EqualTo(0));

            // Dans B : vers l'entrée de C.
            Assert.That(g.Waypoint(new Vector3(100f, 0f, 0f), target, ref last, out p), Is.True);
            Assert.That(last, Is.EqualTo(1));
            Assert.That(p, Is.EqualTo(g.Entry(2, 1)));

            // Dans la même salle que la cible : droit dessus.
            Assert.That(g.Waypoint(new Vector3(195f, 0f, 0f), target, ref last, out _), Is.False);
            Assert.That(g.EdgeBetween(1, 3), Is.EqualTo(2));
            Assert.That(g.EdgeBetween(0, 2), Is.EqualTo(-1));
        }

        [Test]
        public void SeuilDePorte_DalleAuSol_PasSousUneFenetre()
        {
            _go = new GameObject("Pièce (test)");
            _go.transform.position = new Vector3(0f, 3f, 0f);               // sol à 0 (taille 6 m)
            var room = _go.AddComponent<ReachRoom>();
            var door = new GameObject("Ouverture : porte").transform;
            door.SetParent(_go.transform, false);
            door.position = new Vector3(0f, 1.5f, -6f);
            var window = new GameObject("Ouverture : fenêtre").transform;
            window.SetParent(_go.transform, false);
            window.position = new Vector3(3f, 2.55f + 1.5f, -6f);
            Assert.That(ZoneNavMesh.Threshold(room, door, out UnityEngine.AI.NavMeshBuildSource sill), Is.True, "porte : un seuil");
            Assert.That(sill.shape, Is.EqualTo(UnityEngine.AI.NavMeshBuildSourceShape.Box));
            Vector3 at = sill.transform.GetColumn(3);
            Assert.That(at.y, Is.EqualTo(-0.05f).Within(1e-4f), "au niveau du sol de la pièce");
            Assert.That(new Vector2(at.x, at.z), Is.EqualTo(new Vector2(0f, -6f)), "sous l'ouverture");
            Assert.That(sill.size.z, Is.GreaterThan(2.5f), "couvre l'épaisseur du mur et dépasse des deux côtés");
            Assert.That(ZoneNavMesh.Threshold(room, window, out _), Is.False, "fenêtre (à 2.5 m et plus) : rien");
        }

        [Test]
        public void Reglages_Escalade_Deblocage_LongTrajet()
        {
            var s = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            Assert.That(s.leapTelegraph, Is.EqualTo(0.3f), "le bond depuis un mur garde son annonce de 0.3 s");
            Assert.That(new[] { s.wallAssaultRange, s.wallSearchRange, s.wallLeapRange, s.wallHoldTime }, Is.EqualTo(new[] { 30f, 12f, 12f, 3f }));
            Assert.That(new[] { s.longPathDistance, s.unstuckTime }, Is.EqualTo(new[] { 100f, 2f }), "immobile plus de 2 s en chasse : replacée");
            Assert.That(new EnemyLocomotion().StayOnMesh, Is.False, "le traqueur garde son pas ; la goule active StayOnMesh");
        }
    }
}
