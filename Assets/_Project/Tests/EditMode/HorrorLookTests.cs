using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Lumière d'ambiance horreur (2026-11-17) : les trois ambiances du jeu ont leurs réglages (HorrorLook_Default, matériau du halo),
    /// le fondu vers l'ambiance enragée garde ces réglages et mélange l'étalonnage.
    /// </summary>
    public class HorrorLookTests
    {
        [Test]
        public void Ambiances_OntLeursReglagesHorreur()
        {
            foreach (string n in new[] { "Intermediaire", "Sombre", "Horreur" })
            {
                var a = AssetDatabase.LoadAssetAtPath<AmbianceSettings>($"Assets/_Project/Settings/Ambiance_{n}.asset");
                Assert.IsNotNull(a, n);
                Assert.IsNotNull(a.look, $"{n} : réglages de la lumière d'ambiance horreur");
                Assert.IsNotNull(a.look.haloMaterial, "matériau du halo");
                Assert.AreEqual("ItHunts/Halo", a.look.haloMaterial.shader.name);
            }
        }

        [Test]
        public void FonduEnrage_GardeLesReglages_EtMelangeLEtalonnage()
        {
            var a = ScriptableObject.CreateInstance<AmbianceSettings>();
            var b = ScriptableObject.CreateInstance<AmbianceSettings>();
            var into = ScriptableObject.CreateInstance<AmbianceSettings>();
            var look = ScriptableObject.CreateInstance<HorrorLookSettings>();
            a.look = look;
            a.contrast = 10f;
            b.contrast = 30f;
            a.postExposure = 0f;
            b.postExposure = -1f;
            AmbianceSettings.Lerp(a, b, 0.5f, into);
            Assert.AreSame(look, into.look);
            Assert.AreEqual(20f, into.contrast, 1e-4f);
            Assert.AreEqual(-0.5f, into.postExposure, 1e-4f);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(into);
            Object.DestroyImmediate(look);
        }
    }
}
