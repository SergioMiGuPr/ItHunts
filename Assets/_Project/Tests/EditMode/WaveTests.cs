using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Vagues (bloc 7, D-041) : tendance, pics et accalmies, rythme, distance, reconstitution, plafonds, asset.</summary>
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
            Assert.That(new[] { a.peakDuration, a.lullDuration, a.lullShare }, Is.EqualTo(new[] { 20f, 15f, 0.6f }));
            Assert.That(new[] { a.spawnIntervalStart, a.spawnIntervalEnd, a.spawnIntervalEnraged }, Is.EqualTo(new[] { 3f, 1f, 0.5f }));
            Assert.That(new[] { a.minDistance, a.maxDistance, a.telegraph, a.refillInterval }, Is.EqualTo(new[] { 25f, 80f, 0.6f, 4f }));
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
            Assert.That(WaveMath.Wanted(60f, true, _s), Is.EqualTo(60), "pic : la tendance");
            Assert.That(WaveMath.Wanted(60f, false, _s), Is.EqualTo(36), "accalmie : 60 %");
            Assert.That(WaveMath.Wanted(10f, false, _s), Is.EqualTo(6));
        }

        [Test]
        public void Rythme_3sA1s_PuisDemiSecondeEnEnrage()
        {
            Assert.That(WaveMath.SpawnInterval(0f, 240f, _s), Is.EqualTo(3f));
            Assert.That(WaveMath.SpawnInterval(120f, 240f, _s), Is.EqualTo(2f).Within(1e-4f));
            Assert.That(WaveMath.SpawnInterval(240f, 240f, _s), Is.EqualTo(0.5f));
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
        public void TuesReconstituesLentement_JamaisDUnCoup()
        {
            // 20 voulus, 20 présents ; 5 tués : dette 5, pas d'apparition normale pour eux.
            int present = 15, pending = 0, wanted = 20, debt = 5;
            Assert.That(WaveMath.NormalSpawnAllowed(present, pending, wanted, debt), Is.False, "les tués ne reviennent pas par les apparitions normales");
            Assert.That(WaveMath.RefillAllowed(present, pending, wanted, debt), Is.True, "un par refillInterval");
            // La tendance monte à 22 : 2 apparitions normales en plus de la reconstitution.
            Assert.That(WaveMath.NormalSpawnAllowed(15, 0, 22, 5), Is.True);
            Assert.That(WaveMath.NormalSpawnAllowed(17, 0, 22, 5), Is.False);
            // Accalmie : le voulu descend à 12 ; la dette ne dépasse jamais ce qui manque.
            Assert.That(WaveMath.ClampDebt(5, 15, 0, 12), Is.EqualTo(0));
            Assert.That(WaveMath.ClampDebt(5, 10, 0, 12), Is.EqualTo(2));
            Assert.That(WaveMath.RefillAllowed(12, 0, 12, 0), Is.False);

            // Simulation : 5 tués d'un coup, pic stable : il faut 16 s pour revenir à 20 (un toutes les 4 s, le premier tout de suite).
            float t = 0f, nextRefill = 0f;
            int back = 0;
            debt = 5;
            present = 15;
            float done = -1f;
            while (t < 30f && done < 0f)
            {
                if (t >= nextRefill && WaveMath.RefillAllowed(present, 0, 20, debt))
                {
                    nextRefill = t + _s.refillInterval;
                    debt--;
                    present++;
                    back++;
                }

                if (present == 20)
                    done = t;
                t += 0.02f;
            }

            Assert.That(back, Is.EqualTo(5));
            Assert.That(done, Is.EqualTo(16f).Within(0.05f));
        }

        [Test]
        public void PlafondsDeChasse_SuiventLaTendance()
        {
            Assert.That(WaveMath.HuntingScale(60f, _s.ghoulTrend, _s), Is.EqualTo(1f), "fin du timer : plafond réglé");
            Assert.That(WaveMath.ScaledCap(25, WaveMath.HuntingScale(10f, _s.ghoulTrend, _s)), Is.EqualTo(4), "au début : 25 × 10/60");
            Assert.That(WaveMath.ScaledCap(25, WaveMath.HuntingScale(80f, _s.ghoulTrend, _s)), Is.EqualTo(33), "Enragé : 25 × 80/60");
            Assert.That(WaveMath.ScaledCap(8, WaveMath.HuntingScale(2f, _s.flyerTrend, _s)), Is.EqualTo(1), "au moins 1");
            Assert.That(WaveMath.ScaledCap(0, 0.5f), Is.EqualTo(0));
            _s.scaleHuntingCaps = false;
            Assert.That(WaveMath.HuntingScale(10f, _s.ghoulTrend, _s), Is.EqualTo(1f), "réglage coupé");
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
