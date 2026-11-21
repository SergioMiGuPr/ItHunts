using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Interface, playtest du 2026-11-21 (point 8) : plus d'ancien rayon de traction ni de petit point de visée, répulsion très
    /// réduite, réticule de la DA partout ; flot de traction et effet bref de répulsion de la DA.
    /// </summary>
    public class OrbVisualsTests
    {
        [Test]
        public void AnciensVisuels_Coupes_EffetsDeLaDA()
        {
            var fb = AssetDatabase.LoadAssetAtPath<MagnetismFeedbackSettings>("Assets/_Project/Settings/MagnetismFeedback_Default.asset");
            Assert.IsFalse(fb.showTractionLine, "plus d'ancien rayon de traction");
            Assert.IsFalse(fb.showTractionMarks);
            Assert.IsFalse(fb.showRepelAimMarker, "plus de petit point de visée");
            Assert.IsFalse(fb.showRepelImpactFlash);
            Assert.IsTrue(fb.tractionVfx != null || fb.tractionStreamMaterial != null, "flot de traction : effet ou matériau de la DA");
            if (fb.tractionStreamMaterial != null)
                StringAssert.StartsWith("Assets/Art/", AssetDatabase.GetAssetPath(fb.tractionStreamMaterial));
            Assert.That(fb.repelVfxScale, Is.LessThanOrEqualTo(1f), "répulsion : effet bref et réduit");

            var dmg = AssetDatabase.LoadAssetAtPath<DamageFeedbackSettings>("Assets/_Project/Settings/DamageFeedback_Default.asset");
            if (dmg != null)
                Assert.IsFalse(dmg.repelConeRings, "plus d'anneaux du cône de répulsion");
        }

        [Test]
        public void Reticule_DeLaDA_SurLeHudDuJoueur()
        {
            var skin = AssetDatabase.LoadAssetAtPath<HudSkinSettings>("Assets/_Project/Settings/HudSkin_Default.asset");
            Assert.IsNotNull(skin.reticlePrefab, "réticule de la DA");
            StringAssert.StartsWith("Assets/Art/", AssetDatabase.GetAssetPath(skin.reticlePrefab));
        }
    }
}
