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
        public void ConeDeLaLanterne_SuitLeCorps_SansACoup()
        {
            // Lumières qui sautaient (2026-11-17) : le cône suivait la caméra et se recalait d'un coup après une rotation rapide.
            var s = ScriptableObject.CreateInstance<HorrorLookSettings>();
            const float dt = 1f / 60f;
            var c = ItHunts.Level.HorrorLook.FollowBody(default, 1.6f, 0f, 0f, dt, s);
            Assert.That(c.Yaw, Is.EqualTo(0f));
            // Rotation de 120° en une image : le cône ne saute pas, il glisse et la rejoint en moins d'une seconde.
            var c1 = ItHunts.Level.HorrorLook.FollowBody(c, 1.6f, 120f, 0f, dt, s);
            Assert.That(c1.Yaw, Is.LessThan(15f), "pas de saut en une image");
            for (int i = 0; i < 60; i++)
                c1 = ItHunts.Level.HorrorLook.FollowBody(c1, 1.6f, 120f, 0f, dt, s);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(c1.Yaw, 120f)), Is.LessThan(1f), "rejoint en 1 s");
            // Passage par 360° : le chemin le plus court.
            var w = ItHunts.Level.HorrorLook.FollowBody(c1, 1.6f, -170f, 0f, dt, s);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(w.Yaw, 120f)), Is.LessThan(10f));
            // Saut : la hauteur ne suit pas d'un coup ; le tangage est atténué.
            var j = ItHunts.Level.HorrorLook.FollowBody(c, 3.1f, 0f, 40f, dt, s);
            Assert.That(j.Height, Is.LessThan(1.8f));
            for (int i = 0; i < 120; i++)
                j = ItHunts.Level.HorrorLook.FollowBody(j, 1.6f, 0f, 40f, dt, s);
            Assert.That(j.Pitch, Is.EqualTo(40f * s.lanternConePitchScale).Within(0.5f));
            Object.DestroyImmediate(s);
        }

        [Test]
        public void LanterneDuJoueur_SuitLeCorpsAffiche()
        {
            // Lumières qui sautaient dans la vitrine (2026-11-18) : la lanterne, enfant du corps simulé à 60/s, avançait par à-coups
            // pendant que la caméra suivait la position interpolée. Elle se place désormais sur la position interpolée du corps.
            var offset = new Vector3(0f, 1.1f, 0.3f);
            Vector3 a = ItHunts.Player.PlayerLanternLight.FollowBody(new Vector3(2f, 0f, 5f), 0f, offset);
            Assert.That(Vector3.Distance(a, new Vector3(2f, 1.1f, 5.3f)), Is.LessThan(1e-4f));
            Vector3 b = ItHunts.Player.PlayerLanternLight.FollowBody(new Vector3(2f, 0f, 5f), 90f, offset);
            Assert.That(Vector3.Distance(b, new Vector3(2.3f, 1.1f, 5f)), Is.LessThan(1e-4f), "décalage dans le repère du lacet");
            // Entre deux pas de simulation, la lanterne avance avec le corps affiché (pas de pas de 12.5 cm puis 0).
            Vector3 prev = new Vector3(0f, 0f, 0f), cur = new Vector3(0f, 0f, 0.125f);
            float last = ItHunts.Player.PlayerLanternLight.FollowBody(prev, 0f, offset).z;
            for (int i = 1; i <= 4; i++)
            {
                float z = ItHunts.Player.PlayerLanternLight.FollowBody(Vector3.Lerp(prev, cur, i / 4f), 0f, offset).z;
                Assert.That(z - last, Is.EqualTo(0.125f / 4f).Within(1e-4f));
                last = z;
            }
        }

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
