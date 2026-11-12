using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Traqueur (boss-traqueur, D-034) : son coup et sa prise à la main tuent le joueur quels que soient ses PV ; la zone de
    /// prise est la main (boîte orientée) : un joueur à côté d'elle survit.
    /// </summary>
    public class StalkerTests
    {
        private readonly List<Object> _objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _objects)
                if (o != null)
                    Object.DestroyImmediate(o);
            _objects.Clear();
        }

        private PlayerHealth Player(float maxHealth, Vector3 feet)
        {
            var go = new GameObject("Joueur (test)");
            _objects.Add(go);
            go.transform.position = feet;
            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.4f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            var health = ScriptableObject.CreateInstance<PlayerHealthSettings>();
            var coop = ScriptableObject.CreateInstance<CoopSettings>();
            _objects.Add(health);
            _objects.Add(coop);
            health.maxHealth = maxHealth;
            var p = go.AddComponent<PlayerHealth>();
            p.Configure(health, coop);
            return p;
        }

        [Test]
        public void Traqueur_TueEnUnCoup_QuelsQueSoientLesPVDuJoueur()
        {
            foreach (float max in new[] { 1f, 100f, 1000f, 1e6f })
            {
                PlayerHealth p = Player(max, Vector3.zero);
                Assert.That(StalkerStrike.Hit(p, 1f), Is.True);
                Assert.That(p.Health.Dead, Is.True, $"{max} PV : tué en un coup");
                Assert.That(p.Health.Current, Is.EqualTo(0f));
                Assert.That(StalkerStrike.Hit(p, 2f), Is.False, "déjà mort");
            }
        }

        [Test]
        public void PriseALaMain_TueQuelsQueSoientLesPV_SeulementSousLaMain()
        {
            var cfg = ScriptableObject.CreateInstance<StalkerSettings>();
            _objects.Add(cfg);
            Vector3 half = cfg.handSize * 0.5f;                       // main de 4 × 2.2 × 4.5 m
            Vector3 handCenter = new Vector3(0f, half.y, 0f);          // posée au sol, sur le point visé
            Quaternion rot = Quaternion.Euler(0f, 30f, 0f);
            foreach (float max in new[] { 1f, 100f, 1e6f })
            {
                PlayerHealth under = Player(max, new Vector3(0.5f, 0f, 1f));
                Assert.That(StalkerStrike.Grab(under, handCenter, half, rot, 1f), Is.True, $"{max} PV sous la main : tué");
                Assert.That(under.Health.Dead, Is.True);
            }

            PlayerHealth beside = Player(100f, new Vector3(3.2f, 0f, -2.5f));
            Assert.That(StalkerStrike.Grab(beside, handCenter, half, rot, 1f), Is.False, "à côté de la main : vivant");
            PlayerHealth above = Player(100f, new Vector3(0f, 2.7f, 0f));
            Assert.That(StalkerStrike.Grab(above, handCenter, half, rot, 1f), Is.False, "au-dessus de la main (double saut) : vivant");
            Assert.That(beside.Health.Current, Is.EqualTo(100f));
        }
    }
}
