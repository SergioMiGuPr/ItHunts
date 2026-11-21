using ItHunts.Artifacts;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Épée en os, playtest du 2026-11-21 (point 6) : trois coups aux trajectoires différentes (taille horizontale, diagonale,
    /// estoc), impact quand la lame passe devant, corps à corps plus facile (zone plus large, aide à la visée, élan sans perte).
    /// </summary>
    public class SwordFeelTests
    {
        private BoneSwordSettings _cfg;

        [SetUp]
        public void SetUp() => _cfg = Object.Instantiate(AssetDatabase.LoadAssetAtPath<BoneSwordSettings>("Assets/_Project/Settings/BoneSword_Default.asset"));

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        private static Vector3 Tip(int index, bool lunge, float s)
        {
            ArtifactModelView.SwordPose(index, lunge, s, out Vector3 hilt, out Vector3 dir, out _);
            return hilt + dir * ArtifactModelView.BladeLength;
        }

        [Test]
        public void TroisCoups_TrajectoiresDifferentes()
        {
            // 1er : taille horizontale de droite à gauche, la pointe reste à hauteur.
            Vector3 a0 = Tip(0, false, 0f), a1 = Tip(0, false, 1f);
            Assert.That(a0.x, Is.GreaterThan(0.4f));
            Assert.That(a1.x, Is.LessThan(-0.4f));
            Assert.That(Mathf.Abs(a1.y - a0.y), Is.LessThan(0.12f), "horizontale");

            // 2e : diagonale du haut à droite vers le bas à gauche.
            Vector3 b0 = Tip(1, false, 0f), b1 = Tip(1, false, 1f);
            Assert.That(b0.x, Is.GreaterThan(0.3f));
            Assert.That(b0.y, Is.GreaterThan(0.2f));
            Assert.That(b1.x, Is.LessThan(-0.3f));
            Assert.That(b1.y, Is.LessThan(-0.45f), "diagonale");

            // 3e : estoc vers l'avant, la pointe avance tout droit.
            Vector3 c0 = Tip(2, false, 0f), c1 = Tip(2, false, 1f);
            Assert.That(c1.z - c0.z, Is.GreaterThan(0.35f), "estoc");
            Assert.That(Mathf.Abs(c1.x), Is.LessThan(0.08f));
            Assert.That(Tip(0, true, 1f), Is.EqualTo(c1), "la fente (dash, glissade) est un estoc");
        }

        [Test]
        public void Impact_QuandLaLamePasseDevant()
        {
            Assert.That(_cfg.strikeDelay, Is.GreaterThan(0f));
            float u = _cfg.strikeDelay / _cfg.swingAnimDuration;
            ArtifactModelView.SwingTiming(u, out float w, out float s);
            Assert.That(w, Is.GreaterThan(0.9f), "l'épée est sur sa trajectoire à l'impact");
            ArtifactModelView.SwordPose(0, false, s, out _, out Vector3 dir, out _);
            Assert.That(Vector3.Angle(dir, Vector3.forward), Is.LessThan(55f), $"lame vers l'avant à l'impact (avancée {s:0.00})");
            ArtifactModelView.SwingTiming(1f, out float end, out _);
            Assert.That(end, Is.EqualTo(0f).Within(1e-4f), "retour au flottement à la fin");
        }

        [Test]
        public void CorpsACorps_ZonePlusLarge_AideEtElanReglables()
        {
            Assert.That(_cfg.range, Is.GreaterThanOrEqualTo(3f), "portée élargie");
            Assert.That(_cfg.arcHalfAngle, Is.GreaterThanOrEqualTo(50f), "arc élargi");
            Assert.That(_cfg.assistAngle, Is.GreaterThan(0f));
            Assert.That(_cfg.assistTurn, Is.InRange(0.05f, 0.6f), "la vue tourne légèrement");
            Assert.That(_cfg.lungeSpeed, Is.GreaterThan(0f));
            Assert.IsNotNull(_cfg.trailMaterial, "traînée de lame : matériau de la DA");
        }

        [Test]
        public void Elan_NeCasseJamaisLaVitesse([Values(0f, 3f, 5f, 12f, 20f)] float speedTowardTarget)
        {
            var velocity = new Vector3(0f, 0f, speedTowardTarget);
            var toTarget = new Vector3(0f, 0.5f, 4f);
            Vector3 push = BoneSword.LungePush(velocity, toTarget, _cfg);
            Vector3 after = velocity + push;
            Assert.That(after.magnitude, Is.GreaterThanOrEqualTo(velocity.magnitude - 1e-4f), "jamais de perte");
            Assert.That(Vector3.Dot(after, Vector3.forward), Is.EqualTo(Mathf.Max(speedTowardTarget, _cfg.lungeSpeed)).Within(1e-3f),
                "vers la cible au moins à lungeSpeed, sans dépasser la vitesse acquise");
            Assert.That(BoneSword.LungePush(velocity, new Vector3(0f, 0f, 1f), _cfg), Is.EqualTo(Vector3.zero), "cible déjà au contact : rien");
        }
    }
}
