using System;
using System.Linq;
using System.Reflection;
using ItHunts.Player;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Traqueur (boss-traqueur, règle 4) : son coup tue le joueur quels que soient ses PV. Préparé à l'étape 5 bis, avant que
    /// le traqueur existe : tant que le type n'est pas là, le test est ignoré (pas en échec). Contrat attendu à l'étape 6 :
    /// une classe statique <c>ItHunts.Enemies.StalkerStrike</c> avec <c>bool Hit(PlayerHealth target, float time)</c>,
    /// qui tue la cible (PV à 0) sans passer par le tir ami. Si l'étape 6 choisit une autre forme, adapter ce test.
    /// </summary>
    public class StalkerTests
    {
        private const string StrikeType = "ItHunts.Enemies.StalkerStrike";

        [Test]
        public void Traqueur_TueEnUnCoup_QuelsQueSoientLesPVDuJoueur()
        {
            Type strike = typeof(PlayerHealth).Assembly.GetType(StrikeType);
            if (strike == null)
                Assert.Ignore("Étape 6 : le traqueur n'existe pas encore (" + StrikeType + ").");
            MethodInfo hit = strike.GetMethod("Hit", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(PlayerHealth), typeof(float) }, null);
            Assert.That(hit, Is.Not.Null, StrikeType + ".Hit(PlayerHealth, float) attendu");

            foreach (float max in new[] { 1f, 100f, 1000f, 1e6f })
            {
                var go = new GameObject("Joueur (test)");
                var health = ScriptableObject.CreateInstance<PlayerHealthSettings>();
                var coop = ScriptableObject.CreateInstance<CoopSettings>();
                try
                {
                    health.maxHealth = max;
                    go.AddComponent<CharacterController>();
                    var p = go.AddComponent<PlayerHealth>();
                    p.Configure(health, coop);
                    hit.Invoke(null, new object[] { p, 1f });
                    Assert.That(p.Health.Dead, Is.True, $"{max} PV : tué en un coup");
                    Assert.That(p.Health.Current, Is.EqualTo(0f));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(go);
                    UnityEngine.Object.DestroyImmediate(health);
                    UnityEngine.Object.DestroyImmediate(coop);
                }
            }
        }
    }
}
