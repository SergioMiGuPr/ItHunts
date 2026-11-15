using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Vagues (D-041, D-060) : tendance, pics et accalmies, fournée au début du pic, lieux interdits, distance, asset.</summary>
    public class WaveTests
    {
        private WaveSettings _s;

        [SetUp]
        public void SetUp() => _s = ScriptableObject.CreateInstance<WaveSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_s);

        [Test]
        public void Asset_ValeursValidees()
        {
            var a = AssetDatabase.LoadAssetAtPath<WaveSettings>("Assets/_Project/Settings/Waves_Default.asset");
            Assert.That(a, Is.Not.Null);
            Assert.That(a.trendTimes, Is.EqualTo(new[] { 0f, 0.25f, 0.5f, 0.75f, 1f }), "en part du timer (4 min en test, 20 min en vrai)");
            Assert.That(a.ghoulTrend, Is.EqualTo(new[] { 10, 20, 35, 50, 60 }));
            Assert.That(a.flyerTrend, Is.EqualTo(new[] { 2, 4, 8, 12, 16 }));
            Assert.That(new[] { a.ghoulEnraged, a.flyerEnraged }, Is.EqualTo(new[] { 80, 20 }));
            Assert.That(new[] { a.peakDuration, a.lullDuration }, Is.EqualTo(new[] { 20f, 15f }));
            Assert.That(new[] { a.minDistance, a.maxDistance, a.telegraph }, Is.EqualTo(new[] { 25f, 80f, 0.6f }));
            Assert.That(a.ghoulWaveSpawns, Is.EqualTo(new[] { 3, 4, 6, 8, 10 }), "goules par vague (D-060)");
            Assert.That(a.flyerWaveSpawns, Is.EqualTo(new[] { 1, 1, 2, 2, 3 }));
            Assert.That(new[] { a.ghoulWaveEnraged, a.flyerWaveEnraged }, Is.EqualTo(new[] { 14, 4 }));
            Assert.That(new[] { a.waveSpawnWindow, a.waveSpawnStep, a.recentVisitSeconds }, Is.EqualTo(new[] { 8f, 0.2f, 60f }));
            Assert.That(new[] { a.ghoulPool, a.flyerPool }, Is.EqualTo(new[] { 80, 20 }));
        }

        [Test]
        public void Tendance_InterpoleeEnPartDuTimer_EnrageAuDessus()
        {
            Assert.That(WaveMath.Trend(0f, _s.trendTimes, _s.ghoulTrend), Is.EqualTo(10f));
            Assert.That(WaveMath.Trend(0.125f, _s.trendTimes, _s.ghoulTrend), Is.EqualTo(15f).Within(1e-4f));
            Assert.That(WaveMath.Trend(0.5f, _s.trendTimes, _s.ghoulTrend), Is.EqualTo(35f));
            Assert.That(WaveMath.Trend(2f, _s.trendTimes, _s.ghoulTrend), Is.EqualTo(60f), "bornée");
            // Même courbe pour un timer de 4 min et de 20 min.
            Assert.That(WaveMath.Level(60f, 240f, _s.ghoulTrend, 80, _s), Is.EqualTo(WaveMath.Level(300f, 1200f, _s.ghoulTrend, 80, _s)));
            Assert.That(WaveMath.Level(239f, 240f, _s.flyerTrend, 20, _s), Is.LessThan(16.1f));
            Assert.That(WaveMath.Level(240f, 240f, _s.ghoulTrend, 80, _s), Is.EqualTo(80f), "Enragé");
            Assert.That(WaveMath.Level(240f, 240f, _s.flyerTrend, 20, _s), Is.EqualTo(20f));
        }

        [Test]
        public void PicsEtAccalmies_CommenceParUneAccalmie_60PourCent()
        {
            Assert.That(WaveMath.InPeak(0f, _s, out float left, out int index), Is.False, "accalmie de 15 s au début");
            Assert.That(left, Is.EqualTo(15f).Within(1e-4f));
            Assert.That(index, Is.EqualTo(0));
            Assert.That(WaveMath.InPeak(15f, _s, out left, out index), Is.True, "premier pic");
            Assert.That(index, Is.EqualTo(1));
            Assert.That(left, Is.EqualTo(20f).Within(1e-4f));
            Assert.That(WaveMath.InPeak(34.9f, _s, out _, out _), Is.True);
            Assert.That(WaveMath.InPeak(35f, _s, out _, out index), Is.False, "accalmie suivante");
            Assert.That(WaveMath.InPeak(50f, _s, out _, out index), Is.True);
            Assert.That(index, Is.EqualTo(2), "deuxième pic");
        }

        [Test]
        public void Vague_FourneeAuDebutDuPic_SousLePlafond()
        {
            // Fournée : waveSpawns du moment, bornée par le plafond de population (vivants et annoncés compris).
            Assert.That(WaveMath.Level(0f, 900f, _s.ghoulWaveSpawns, _s.ghoulWaveEnraged, _s), Is.EqualTo(3f), "début : 3 goules");
            Assert.That(WaveMath.Level(450f, 900f, _s.ghoulWaveSpawns, _s.ghoulWaveEnraged, _s), Is.EqualTo(6f), "milieu : 6");
            Assert.That(WaveMath.Level(900f, 900f, _s.ghoulWaveSpawns, _s.ghoulWaveEnraged, _s), Is.EqualTo(14f), "Enragé : 14");
            Assert.That(WaveMath.WaveBatch(6f, 10, 0, 35), Is.EqualTo(6), "place sous le plafond");
            Assert.That(WaveMath.WaveBatch(6f, 32, 1, 35), Is.EqualTo(2), "plafond presque atteint");
            Assert.That(WaveMath.WaveBatch(6f, 40, 0, 35), Is.EqualTo(0), "au-dessus du plafond : rien");
            Assert.That(WaveMath.Cap(35.4f), Is.EqualTo(35));
        }

        [Test]
        public void SalleRecente_60s_EtSalleDeDepart_JamaisDApparition()
        {
            Assert.That(WaveMath.RecentlyVisited(float.NegativeInfinity, 100f, 60f), Is.False, "jamais visitée");
            Assert.That(WaveMath.RecentlyVisited(50f, 100f, 60f), Is.True, "passé il y a 50 s");
            Assert.That(WaveMath.RecentlyVisited(30f, 100f, 60f), Is.False, "il y a 70 s : de nouveau permise");

            var made = new List<GameObject>();
            try
            {
                ItHunts.Level.RoomVolume Room(string n, Vector3 c, ItHunts.Level.RoomKind kind)
                {
                    var go = new GameObject(n);
                    made.Add(go);
                    go.transform.position = c;
                    var r = go.AddComponent<ItHunts.Level.RoomVolume>();
                    r.Configure(n, new Vector3(20f, 8f, 20f), kind);
                    typeof(ItHunts.Level.RoomVolume).GetMethod("OnEnable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(r, null);
                    return r;
                }

                ItHunts.Level.RoomVolume depart = Room("Départ", new Vector3(0f, 4f, 0f), ItHunts.Level.RoomKind.Depart);
                ItHunts.Level.RoomVolume salle = Room("Salle", new Vector3(100f, 4f, 0f), ItHunts.Level.RoomKind.Salle);
                var dgo = new GameObject("Vagues (test)");
                made.Add(dgo);
                var waves = dgo.AddComponent<WaveDirector>();
                typeof(WaveDirector).GetField("settings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(waves, _s);
                Assert.That(waves.PlaceAllowed(new Vector3(2f, 0f, 2f), 1000f), Is.False, "salle de départ : jamais, toute la zone");
                Assert.That(waves.PlaceAllowed(new Vector3(102f, 0f, 2f), 1000f), Is.True, "salle jamais visitée");
                waves.NoteVisit(salle, 1000f);
                Assert.That(waves.PlaceAllowed(new Vector3(102f, 0f, 2f), 1030f), Is.False, "un joueur y est passé il y a 30 s");
                Assert.That(waves.PlaceAllowed(new Vector3(102f, 0f, 2f), 1061f), Is.True, "61 s après : permise");
                Assert.That(waves.PlaceAllowed(new Vector3(200f, 0f, 0f), 1030f), Is.True, "hors de tout volume");
                foreach (GameObject go in made)
                    if (go.GetComponent<ItHunts.Level.RoomVolume>() != null)
                        typeof(ItHunts.Level.RoomVolume).GetMethod("OnDisable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(go.GetComponent<ItHunts.Level.RoomVolume>(), null);
            }
            finally
            {
                foreach (GameObject go in made)
                    Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Distance_25a80m_DeTousLesJoueurs()
        {
            var players = new List<Vector3> { Vector3.zero, new Vector3(100f, 0f, 0f) };
            Assert.That(WaveMath.DistanceOk(new Vector3(50f, 0f, 0f), players, 25f, 80f), Is.True);
            Assert.That(WaveMath.DistanceOk(new Vector3(20f, 0f, 0f), players, 25f, 80f), Is.False, "trop près du premier");
            Assert.That(WaveMath.DistanceOk(new Vector3(50f, 0f, 90f), players, 25f, 80f), Is.False, "trop loin de tous");
            Assert.That(WaveMath.DistanceOk(new Vector3(0f, 0f, 30f), new List<Vector3>(), 25f, 80f), Is.False, "aucun joueur");
            // Champ de vision : 70° vertical en 16:9 ≈ 51° de demi-champ horizontal, plus 10° de marge.
            Assert.That(WaveMath.OutsideView(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 30f), 70f, 16f / 9f, 10f), Is.False);
            Assert.That(WaveMath.OutsideView(Vector3.zero, Vector3.forward, new Vector3(30f, 0f, 10f), 70f, 16f / 9f, 10f), Is.True, "à 72° sur le côté");
            Assert.That(WaveMath.OutsideView(Vector3.zero, Vector3.forward, new Vector3(0f, 0f, -30f), 70f, 16f / 9f, 10f), Is.True, "derrière");
        }

        [Test]
        public void Chasseurs_ParPaliers_1Puis2_4_6_8_Enrage10()
        {
            var a = AssetDatabase.LoadAssetAtPath<WaveSettings>("Assets/_Project/Settings/Waves_Default.asset");
            Assert.That(a.chaserTimes, Is.EqualTo(new[] { 0f, 0.15f, 0.35f, 0.55f, 0.75f }));
            Assert.That(a.chaserCounts, Is.EqualTo(new[] { 1, 2, 4, 6, 8 }));
            Assert.That(a.chaserEnraged, Is.EqualTo(10));
            Assert.That(WaveMath.ChasersPerPlayer(0f, 900f, _s), Is.EqualTo(1), "début : 1 par joueur");
            Assert.That(WaveMath.ChasersPerPlayer(134f, 900f, _s), Is.EqualTo(1), "juste avant 15 %");
            Assert.That(WaveMath.ChasersPerPlayer(135f, 900f, _s), Is.EqualTo(2), "palier, sans interpolation");
            Assert.That(WaveMath.ChasersPerPlayer(400f, 900f, _s), Is.EqualTo(4));
            Assert.That(WaveMath.ChasersPerPlayer(500f, 900f, _s), Is.EqualTo(6));
            Assert.That(WaveMath.ChasersPerPlayer(700f, 900f, _s), Is.EqualTo(8));
            Assert.That(WaveMath.ChasersPerPlayer(900f, 900f, _s), Is.EqualTo(10), "Enragé");
            Assert.That(WaveMath.ChasersPerPlayer(60f, 240f, _s), Is.EqualTo(WaveMath.ChasersPerPlayer(225f, 900f, _s)), "en part du timer");
        }

        [Test]
        public void Habitants_1a3ParGrandeSalle_SelonLaTaille()
        {
            Assert.That(new[] { _s.residentGhoulsMin, _s.residentGhoulsMax, _s.residentFlyersMax }, Is.EqualTo(new[] { 1, 3, 1 }));
            Assert.That(WaveMath.ResidentsToKeep(40f, _s), Is.EqualTo(1), "petite salle");
            Assert.That(WaveMath.ResidentsToKeep(56f, _s), Is.EqualTo(2));
            Assert.That(WaveMath.ResidentsToKeep(80f, _s), Is.EqualTo(3), "grande salle");
        }

        [Test]
        public void DensiteParSurface_EchelleBornee()
        {
            var w = AssetDatabase.LoadAssetAtPath<WaveSettings>("Assets/_Project/Settings/Waves_Default.asset");
            Assert.That(w.scaleWithMapArea, Is.True);
            Assert.That(WaveDirector.AreaScaleFor(w.referenceArea, w), Is.EqualTo(1f).Within(1e-4f), "Test_Arena : courbe inchangée");
            Assert.That(WaveDirector.AreaScaleFor(w.referenceArea * 2f, w), Is.EqualTo(2f).Within(1e-4f), "deux fois plus grand : deux fois plus");
            Assert.That(WaveDirector.AreaScaleFor(w.referenceArea * 6f, w), Is.EqualTo(w.maxAreaScale), "borné (niveau 1 : ×2.5)");
            Assert.That(WaveDirector.AreaScaleFor(100f, w), Is.EqualTo(w.minAreaScale));
        }

        [Test]
        public void Niveau1_HabitantsDansLesSalles()
        {
            string scene = System.IO.File.ReadAllText("Assets/Scenes/Level_01_Base.unity");
            Assert.That(scene.Contains("Habitants des salles"), Is.True);
            int residents = System.Text.RegularExpressions.Regex.Matches(scene, "habitante, ").Count;
            Assert.That(residents, Is.InRange(100, 220), "2 à 5 goules par grande salle (40 salles)");
        }
    }
}
