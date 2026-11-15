using System.Collections.Generic;
using System.Reflection;
using ItHunts.Level;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Volumes de salle (D-045) et réglages de la partie 2 du playtest de l'étape 7 (traction, murs).</summary>
    public class RoomVolumeTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made)
            {
                foreach (RoomVolume r in go.GetComponents<RoomVolume>())
                    typeof(RoomVolume).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(r, null);
                Object.DestroyImmediate(go);
            }

            _made.Clear();
        }

        private RoomVolume Room(string name, Vector3 center, Vector3 size, float yaw = 0f, RoomKind kind = RoomKind.Salle)
        {
            var go = new GameObject(name);
            _made.Add(go);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, yaw, 0f));
            var r = go.AddComponent<RoomVolume>();
            r.Configure(name, size, kind);
            typeof(RoomVolume).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(r, null);
            return r;
        }

        [Test]
        public void JamaisDansLaSalleDUnJoueur_CouloirPermis()
        {
            RoomVolume a = Room("A", new Vector3(0f, 4f, 0f), new Vector3(20f, 8f, 20f));
            Room("B", new Vector3(40f, 4f, 0f), new Vector3(20f, 8f, 20f));
            var player = new List<Vector3> { new Vector3(5f, 0f, 5f) };                  // pieds au sol de A
            Assert.That(RoomVolume.At(player[0]), Is.EqualTo(a));
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(-8f, 0f, -8f), player), Is.True, "même salle : pas d'apparition");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(40f, 0f, 0f), player), Is.False, "autre salle : permis");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(20f, 0f, 0f), player), Is.False, "couloir (hors salle) : permis");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(0f, 0f, 0f), new List<Vector3> { new Vector3(20f, 0f, 0f) }), Is.False,
                "joueur dans un couloir : aucune salle interdite");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(0f, 0f, 0f), new List<Vector3>()), Is.False, "aucun joueur vivant");
        }

        [Test]
        public void SalleTourneeEtSalleDansUneSalle()
        {
            Room("Tournée", new Vector3(0f, 3f, 0f), new Vector3(30f, 6f, 4f), yaw: 90f);   // longue selon z une fois tournée
            Assert.That(RoomVolume.At(new Vector3(0f, 0f, 12f)), Is.Not.Null);
            Assert.That(RoomVolume.At(new Vector3(12f, 0f, 0f)), Is.Null);
            RoomVolume big = Room("Grande", new Vector3(100f, 5f, 0f), new Vector3(40f, 10f, 40f));
            RoomVolume small = Room("Alcôve", new Vector3(110f, 2f, 0f), new Vector3(6f, 4f, 6f));
            Assert.That(RoomVolume.At(new Vector3(110f, 0f, 0f)), Is.EqualTo(small), "la plus petite");
            Assert.That(RoomVolume.At(new Vector3(90f, 0f, 0f)), Is.EqualTo(big));
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(90f, 0f, 0f), new List<Vector3> { new Vector3(110f, 0f, 0f) }), Is.True,
                "le joueur dans l'alcôve est aussi dans la grande salle");
        }

        [Test]
        public void Couloir_TronconDuJoueurEtVoisinsInterdits_PlusLoinPermis()
        {
            // Couloir de 200 m le long de x, en 5 tronçons de 40 m (D-047), qui débouche dans une salle à l'est.
            for (int i = 0; i < 5; i++)
                Room($"T{i}", new Vector3(20f + 40f * i, 9.5f, 0f), new Vector3(40f, 19f, 14f), kind: RoomKind.Couloir);
            Room("Salle", new Vector3(230f, 10f, 0f), new Vector3(60f, 20f, 60f));
            var player = new List<Vector3> { new Vector3(100f, 0f, 3f) };              // tronçon T2
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(110f, 0f, -3f), player), Is.True, "son tronçon");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(60f, 0f, 0f), player), Is.True, "tronçon voisin (T1)");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(150f, 0f, 0f), player), Is.True, "tronçon voisin (T3)");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(20f, 0f, 0f), player), Is.False, "deux tronçons plus loin (T0) : permis");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(190f, 0f, 0f), player), Is.False, "T4 : permis");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(230f, 0f, 0f), player), Is.False, "la salle au bout : permise");
            Assert.That(RoomVolume.SharesRoomWithAny(new Vector3(190f, 0f, 0f), new List<Vector3> { new Vector3(230f, 0f, 0f) }), Is.False,
                "joueur dans la salle : le tronçon qui y débouche reste permis (règle des salles inchangée)");
        }

        [Test]
        public void Minimap_DecouverteEnVisitant_PointsClesVisiblesUneFoisDecouverts()
        {
            RoomVolume a = Room("A", new Vector3(0f, 4f, 0f), new Vector3(20f, 8f, 20f));
            RoomVolume t = Room("Tronçon", new Vector3(30f, 4f, 0f), new Vector3(40f, 8f, 14f), kind: RoomKind.Couloir);
            var go = new GameObject("Carte");
            _made.Add(go);
            var map = go.AddComponent<MapDiscovery>();
            Assert.That(map.IsKnown(a) || map.IsKnown(t), Is.False, "tout est dans le noir au début");
            Assert.That(map.DiscoverAt(new Vector3(2f, 0.5f, 2f)), Is.EqualTo(1), "le joueur entre dans A");
            Assert.That(map.IsKnown(a) && !map.IsKnown(t), Is.True);
            Assert.That(map.IsKnownPoint(new Vector3(-8f, 1f, 8f)), Is.True, "coffre dans A : affiché");
            Assert.That(map.IsKnownPoint(new Vector3(40f, 1f, 0f)), Is.False, "coffre dans le couloir pas encore visité : caché");
            Assert.That(map.DiscoverAt(new Vector3(2f, 0.5f, 2f)), Is.EqualTo(0), "déjà découverte");
            map.DiscoverAt(new Vector3(35f, 0.5f, 0f));
            Assert.That(map.IsKnownPoint(new Vector3(40f, 1f, 0f)), Is.True, "découvertes communes : un autre joueur l'a visité");
            map.Clear();
            Assert.That(map.KnownCount, Is.EqualTo(0));

            // Sortie : affichée dès le début, sans la découvrir ; un coffre au même endroit reste caché.
            var exitGo = new GameObject("Sortie");
            _made.Add(exitGo);
            var exit = exitGo.AddComponent<ItHunts.Level.ExitDoor>();
            var chestGo = new GameObject("Coffre");
            _made.Add(chestGo);
            var chest = chestGo.AddComponent<ItHunts.Economy.Chest>();
            Assert.That(ItHunts.Hud.MinimapHud.MarkVisible(exit, new Vector3(40f, 0f, 0f), map), Is.True, "sortie toujours affichée");
            Assert.That(ItHunts.Hud.MinimapHud.MarkVisible(chest, new Vector3(40f, 0f, 0f), map), Is.False, "coffre non découvert : caché");
        }

        [Test]
        public void Reglages_Traction2Charges_Murs85PourcentEt18()
        {
            var m = AssetDatabase.LoadAssetAtPath<MagnetismOrbSettings>("Assets/_Project/Settings/Magnetism_Default.asset");
            Assert.That(m.attractCharges, Is.EqualTo(2), "2 charges (D-036)");
            Assert.That(new[] { m.attractChargeRechargeTime, m.tractionMaxDuration, m.tractionSpeed }, Is.EqualTo(new[] { 6f, 2.5f, 18f }));
            var mv = AssetDatabase.LoadAssetAtPath<MovementSettings>("Assets/_Project/Settings/Movement_Default.asset");
            Assert.That(mv.wallGainScale, Is.EqualTo(0.85f), "−15 % (D-044)");
            Assert.That(mv.wallGainCap, Is.EqualTo(18f), "comme la traction");
            Assert.That(mv.movementGainCap, Is.EqualTo(20f), "le plafond commun reste pour le reste");
        }
    }
}
