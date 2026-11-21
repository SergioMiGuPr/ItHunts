using ItHunts.Enemies;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Ressort des chaînes d'os (point 10, traqueur v4 inactif) : calcul pur.</summary>
    public class BoneSpringTests
    {
        private static BoneSpringChain.Particle[] Chain(int n, out Vector3[] rest)
        {
            var p = new BoneSpringChain.Particle[n];
            rest = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                rest[i] = new Vector3(0.3f * i, 0f, 0f);                // pose animée : horizontale
                p[i].Position = p[i].Previous = rest[i];
                p[i].Length = i > 0 ? 0.3f : 0f;
            }

            return p;
        }

        private static void Run(BoneSpringChain.Particle[] p, Vector3[] rest, BoneSpringSettings s, int steps, System.Collections.Generic.IList<BoneSpringChain.Obstacle> obs = null)
        {
            for (int k = 0; k < steps; k++)
                BoneSpringChain.Solve(p, rest, Vector3.zero, s, obs, 1f / 60f);
        }

        [Test]
        public void SansRaideur_LaChainePendSousLaGravite_LongueurGardee()
        {
            var p = Chain(6, out Vector3[] rest);
            var s = BoneSpringSettings.Default;
            s.stiffness = 0f;
            s.damping = 0.1f;
            s.gravity = new Vector3(0f, -9.81f, 0f);
            Run(p, rest, s, 600);
            Assert.That(p[5].Position.y, Is.LessThan(-1.2f), "pend vers le bas");
            for (int i = 1; i < 6; i++)
                Assert.That(Vector3.Distance(p[i].Position, p[i - 1].Position), Is.EqualTo(0.3f).Within(1e-3f), "longueur");
        }

        [Test]
        public void RaideurMaximale_GardeLaPoseAnimee()
        {
            var p = Chain(6, out Vector3[] rest);
            var s = BoneSpringSettings.Default;
            s.stiffness = 1f;
            Run(p, rest, s, 120);
            Assert.That(p[5].Position.y, Is.EqualTo(0f).Within(1e-3f));
        }

        [Test]
        public void Inertie_LaChaineTraineDerriereLaRacine()
        {
            var p = Chain(4, out Vector3[] rest);
            var s = BoneSpringSettings.Default;
            s.gravity = Vector3.zero;
            s.inertia = 0f;
            for (int i = 0; i < rest.Length; i++)
                rest[i] += Vector3.forward;                             // la racine avance d'un mètre d'un coup
            BoneSpringChain.Solve(p, rest, Vector3.forward, s, null, 1f / 60f);
            Assert.That(p[3].Position.z, Is.LessThan(0.5f), "le bout traîne");
        }

        [Test]
        public void Collisions_SphereEtCapsule()
        {
            var sphere = new BoneSpringChain.Obstacle { A = Vector3.zero, B = Vector3.zero, Radius = 0.5f };
            Vector3 outside = BoneSpringChain.PushOut(new Vector3(0.1f, 0f, 0f), sphere, 0.05f);
            Assert.That(outside.magnitude, Is.EqualTo(0.55f).Within(1e-4f));
            var capsule = new BoneSpringChain.Obstacle { A = new Vector3(0f, -1f, 0f), B = new Vector3(0f, 1f, 0f), Radius = 0.3f };
            Vector3 o2 = BoneSpringChain.PushOut(new Vector3(0.1f, 0.5f, 0f), capsule, 0f);
            Assert.That(new Vector2(o2.x, o2.z).magnitude, Is.EqualTo(0.3f).Within(1e-4f));
            Assert.That(o2.y, Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Traqueur_CheveuxActives_ReglagesDansStalkerDefault()
        {
            // D-081 : le v4 est livré (PR #27), le ressort des cheveux est actif ; réglages dans Stalker_Default.
            var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<StalkerSettings>("Assets/_Project/Settings/Stalker_Default.asset");
            Assert.That(cfg.boneChainsEnabled, Is.True);
            Assert.That(cfg.boneChains.stiffness, Is.InRange(0.01f, 0.5f));
            Assert.That(cfg.boneChains.damping, Is.InRange(0.01f, 0.5f));
            Assert.That(cfg.boneChains.gravity.y, Is.LessThan(0f));
        }
    }
}
