using System.Collections.Generic;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Aide à la visée (après le playtest des étapes 8 et 9) : manette oui, souris non ; cône, ralentissement, attraction.</summary>
    public class AimAssistTests
    {
        [Test]
        public void Reglages_ManetteOui_SourisNon()
        {
            var c = AssetDatabase.LoadAssetAtPath<CameraSettings>("Assets/_Project/Settings/Camera_Default.asset");
            Assert.That(AimAssist.Enabled(c, gamepad: true), Is.True, "activée par défaut à la manette");
            Assert.That(AimAssist.Enabled(c, gamepad: false), Is.False, "coupée à la souris");
            Assert.That(new[] { c.aimAssistAngle, c.aimAssistRange, c.aimAssistSlowdown, c.aimAssistPull }, Is.EqualTo(new[] { 6f, 45f, 0.5f, 8f }));
        }

        [Test]
        public void Cible_LaPlusProcheDuViseur_DansLeConeEtAPortee()
        {
            var pts = new List<Vector3>
            {
                new Vector3(2f, 0f, 20f),     // ≈ 5.7° : dans le cône
                new Vector3(0.5f, 0f, 20f),   // ≈ 1.4° : la plus proche du viseur
                new Vector3(0f, 0f, 60f),     // dans l'axe mais trop loin
                new Vector3(10f, 0f, 10f),    // 45° : hors du cône
            };
            Assert.That(AimAssist.Best(Vector3.zero, Vector3.forward, pts, 6f, 45f, out Vector3 best, out float angle), Is.True);
            Assert.That(best, Is.EqualTo(pts[1]));
            Assert.That(angle, Is.EqualTo(1.43f).Within(0.02f));
            Assert.That(AimAssist.Best(Vector3.zero, Vector3.forward, new List<Vector3> { pts[2], pts[3] }, 6f, 45f, out _, out _), Is.False, "rien à aider");
        }

        [Test]
        public void Ralentissement_AuCentre_RetourA1AuBord_AttractionBornee()
        {
            Assert.That(AimAssist.Slowdown(0f, 6f, 0.5f), Is.EqualTo(0.5f));
            Assert.That(AimAssist.Slowdown(3f, 6f, 0.5f), Is.EqualTo(0.75f).Within(1e-4f));
            Assert.That(AimAssist.Slowdown(6f, 6f, 0.5f), Is.EqualTo(1f));
            // Cible à 5° à droite : 8°/s pendant 0.1 s → 0.8° vers elle, pas plus.
            Vector3 target = Quaternion.Euler(0f, 5f, 0f) * Vector3.forward * 20f;
            Vector2 pull = AimAssist.Pull(0f, 0f, Vector3.zero, target, 8f, 0.1f);
            Assert.That(pull.x, Is.EqualTo(0.8f).Within(1e-3f), "lacet vers la droite");
            Assert.That(Mathf.Abs(pull.y), Is.LessThan(1e-3f));
            // Presque dessus : elle ne la dépasse pas.
            Vector2 last = AimAssist.Pull(4.9f, 0f, Vector3.zero, target, 8f, 0.1f);
            Assert.That(last.x, Is.EqualTo(0.1f).Within(1e-3f));
            // Cible plus haute : tangage négatif (on lève les yeux).
            Assert.That(AimAssist.Pull(0f, 0f, Vector3.zero, new Vector3(0f, 2f, 20f), 8f, 0.1f).y, Is.LessThan(0f));
        }
    }
}
