using ItHunts.Combat;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Parade, playtest du 2026-11-21 (point 5) : fenêtre calée sur l'impact réel du coup de goule, debug « réussie / ratée de X ms
    /// (avant ou après l'impact) », tolérance après l'impact (réglable), taux de réussite selon le temps de réaction.
    /// </summary>
    public class ParryFeedbackTests
    {
        private ParrySettings _cfg;

        [SetUp]
        public void SetUp() => _cfg = ScriptableObject.CreateInstance<ParrySettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        [Test]
        public void Fenetre_CaleeSurLImpactDuClipGouleCoup()
        {
            var ghoul = AssetDatabase.LoadAssetAtPath<GhoulSettings>("Assets/_Project/Settings/Ghoul_Default.asset");
            AnimationClip clip = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Models/Ennemis/SK_Goule.fbx"))
                if (o is AnimationClip c && c.name == "Goule|Goule_Coup")
                    clip = c;
            Assert.IsNotNull(clip, "clip Goule_Coup");
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Ennemis/SK_Goule.fbx");
            var go = Object.Instantiate(fbx);
            try
            {
                Transform hand = null;
                foreach (Transform t in go.GetComponentsInChildren<Transform>())
                    if (t.name == "Doigts.R")
                        hand = t;
                Assert.IsNotNull(hand, "main droite");
                float best = float.NegativeInfinity, bestT = 0f;
                for (int i = 0; i <= Mathf.RoundToInt(clip.length * 100f); i++)
                {
                    float t = i * 0.01f;
                    clip.SampleAnimation(go, t);
                    float z = go.transform.InverseTransformPoint(hand.position).z;
                    if (z > best)
                    {
                        best = z;
                        bestT = t;
                    }
                }

                // Nuit 3, point 7 : l'impact du clip à vitesse normale (contactClipImpact) est celui de la main ; le clip est ralenti pour
                // que cet impact tombe à contactImpactDelay (coup annoncé de 0.55 s) : les dégâts tombent toujours quand la main frappe.
                Assert.That(ghoul.contactClipImpact, Is.EqualTo(bestT).Within(0.02f),
                    $"impact du clip ({ghoul.contactClipImpact:0.00} s) à l'instant où la main frappe ({bestT:0.00} s)");
                Assert.That(ghoul.contactImpactDelay, Is.EqualTo(0.55f).Within(1e-3f), "coup lisible (temps de réaction humain)");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Debug_Reussie_AvecLEcartAvantLImpact()
        {
            ParryState s = ParryState.Create();
            ParryMath.Press(ref s, 10f, _cfg);
            Assert.IsTrue(ParryMath.TryParry(ref s, 10.12f, _cfg, true));
            Assert.That(s.LastResult, Is.EqualTo(ParryResult.Reussie));
            Assert.That(ParryMath.Describe(s, _cfg), Does.StartWith("Parade RÉUSSIE : appui 120 ms avant l'impact"));
        }

        [Test]
        public void Debug_RateeTropTot_DeCombien()
        {
            ParryState s = ParryState.Create();
            ParryMath.Press(ref s, 10f, _cfg);
            Assert.IsFalse(ParryMath.TryParry(ref s, 10.32f, _cfg, true));
            Assert.That(s.LastResult, Is.EqualTo(ParryResult.TropTot));
            Assert.That(ParryMath.Describe(s, _cfg), Is.EqualTo("Parade ratée : appui 320 ms avant l'impact, trop tôt de 70 ms"));
        }

        [Test]
        public void Debug_RateeTropTard_AppuiJusteApres()
        {
            _cfg.lateWindow = 0f;                                        // sans tolérance après l'impact
            ParryState s = ParryState.Create();
            Assert.IsFalse(ParryMath.TryParry(ref s, 10f, _cfg, true), "coup sans appui");
            ParryMath.Press(ref s, 10.04f, _cfg);
            Assert.That(s.LastResult, Is.EqualTo(ParryResult.TropTard));
            Assert.That(ParryMath.Describe(s, _cfg), Is.EqualTo("Parade ratée : appui 40 ms après l'impact, trop tard de 40 ms"));
        }

        [Test]
        public void ToleranceApresLImpact_Reglable_ZeroParDefaut()
        {
            Assert.That(_cfg.lateWindow, Is.EqualTo(0.08f), "nuit 3 : 80 ms de tolérance après l'impact");
            _cfg.lateWindow = 0f;
            ParryState s = ParryState.Create();
            Assert.IsFalse(ParryMath.TryParry(ref s, 10f, _cfg, true));
            ParryMath.Press(ref s, 10.04f, _cfg);
            Assert.IsFalse(ParryMath.TryLateParry(ref s, 10f, 10.05f, _cfg, true), "sans tolérance : raté");

            _cfg.lateWindow = 0.06f;
            s = ParryState.Create();
            Assert.IsFalse(ParryMath.TryParry(ref s, 10f, _cfg, true));
            ParryMath.Press(ref s, 10.04f, _cfg);
            Assert.IsTrue(ParryMath.TryLateParry(ref s, 10f, 10.05f, _cfg, true), "40 ms après, tolérance 60 ms : paré");
            Assert.That(ParryMath.Describe(s, _cfg), Does.StartWith("Parade RÉUSSIE : appui 40 ms après l'impact"));
            Assert.IsFalse(ParryMath.TryLateParry(ref s, 10f, 10.05f, _cfg, false), "jamais le traqueur");
        }

        /// <summary>
        /// Taux de réussite d'un joueur qui réagit au départ du coup (son d'attaque et élan du bras, 0.24 s avant l'impact), temps de
        /// réaction ~ N(210 ms, 45 ms) : fenêtre actuelle et propositions.
        /// </summary>
        [Test]
        public void Mesure_TauxDeReussite_SelonLaFenetre()
        {
            const float impact = 0.24f, mean = 0.21f, sd = 0.045f;
            float Rate(float before, float after)
            {
                int ok = 0, n = 20000;
                var rng = new System.Random(5);
                for (int i = 0; i < n; i++)
                {
                    double u1 = 1.0 - rng.NextDouble(), u2 = rng.NextDouble();
                    float rt = mean + sd * (float)(System.Math.Sqrt(-2.0 * System.Math.Log(u1)) * System.Math.Cos(2.0 * System.Math.PI * u2));
                    if (rt >= impact - before && rt <= impact + after)
                        ok++;
                }

                return ok * 100f / n;
            }

            float now = Rate(0.18f, 0f), wide = Rate(0.24f, 0f), late = Rate(0.24f, 0.06f), lateOnly = Rate(0.18f, 0.06f);
            Assert.That(late, Is.GreaterThan(now));
            Assert.Pass($"actuelle 180 ms avant : {now:0} % ; 240 ms avant : {wide:0} % ; 180 avant + 60 après : {lateOnly:0} % ; " +
                        $"240 avant + 60 après : {late:0} %");
        }
    }
}
