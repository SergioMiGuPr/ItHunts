using ItHunts.Combat;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Parade (D-075) : fenêtre, recharge, traqueur insensible.</summary>
    public class ParryTests
    {
        private ParrySettings _cfg;

        [SetUp]
        public void SetUp() => _cfg = ScriptableObject.CreateInstance<ParrySettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_cfg);

        [Test]
        public void Reglages_ParDefaut()
        {
            Assert.That(_cfg.window, Is.EqualTo(0.18f));
            Assert.That(_cfg.stunDuration, Is.EqualTo(1.5f));
            Assert.That(_cfg.cooldown, Is.EqualTo(4f));
            var asset = AssetDatabase.LoadAssetAtPath<ParrySettings>("Assets/_Project/Settings/Parry_Default.asset");
            Assert.That(asset, Is.Not.Null, "Parry_Default");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var parry = prefab.GetComponent<ItHunts.Player.PlayerParry>();
            Assert.That(parry, Is.Not.Null, "parade sur le prefab joueur");
            Assert.That(parry.Settings, Is.EqualTo(asset));
        }

        [Test]
        public void Fenetre_Reussie_JusteAvantLImpact()
        {
            ParryState s = ParryState.Create();
            Assert.That(ParryMath.Press(ref s, 10f, _cfg), Is.True);
            Assert.That(ParryMath.TryParry(ref s, 10.17f, _cfg, true), Is.True, "impact 0.17 s après l'appui");
            Assert.That(s.Successes, Is.EqualTo(1));
            Assert.That(ParryMath.TryParry(ref s, 10.175f, _cfg, true), Is.True, "deux goules au même moment : les deux parées");
            Assert.That(s.Successes, Is.EqualTo(1), "un seul appui");
        }

        [Test]
        public void Fenetre_Ratee_TropTotOuSansAppui()
        {
            ParryState s = ParryState.Create();
            Assert.That(ParryMath.TryParry(ref s, 5f, _cfg, true), Is.False, "sans appui");
            ParryMath.Press(ref s, 10f, _cfg);
            Assert.That(ParryMath.TryParry(ref s, 10.25f, _cfg, true), Is.False, "appui trop tôt (0.25 s avant l'impact)");
            Assert.That(ParryMath.TryParry(ref s, 9.9f, _cfg, true), Is.False, "impact avant l'appui");
        }

        [Test]
        public void Recharge_4s_ApresChaqueAppui()
        {
            ParryState s = ParryState.Create();
            Assert.That(ParryMath.Press(ref s, 10f, _cfg), Is.True);
            Assert.That(ParryMath.Press(ref s, 12f, _cfg), Is.False, "en recharge");
            Assert.That(ParryMath.TryParry(ref s, 12.05f, _cfg, true), Is.False, "l'appui refusé n'ouvre pas de fenêtre");
            Assert.That(ParryMath.CooldownRemaining(s, 12f), Is.EqualTo(2f).Within(1e-4f));
            Assert.That(ParryMath.Fill(s, 12f, _cfg), Is.EqualTo(0.5f).Within(1e-4f));
            Assert.That(ParryMath.Press(ref s, 14f, _cfg), Is.True, "prête après 4 s");
        }

        [Test]
        public void Traqueur_Insensible()
        {
            ParryState s = ParryState.Create();
            ParryMath.Press(ref s, 10f, _cfg);
            Assert.That(ParryMath.TryParry(ref s, 10.05f, _cfg, attackerParryable: false), Is.False);

            var stalker = new GameObject("Traqueur factice");
            var dmg = stalker.AddComponent<Damageable>();
            var so = new SerializedObject(dmg);
            so.FindProperty("immuneToOrbs").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            var ghoul = new GameObject("Goule factice");
            ghoul.AddComponent<Damageable>();
            try
            {
                Assert.That(ParryMath.Parryable(dmg), Is.False, "traqueur (ImmuneToOrbs) : sa prise passe");
                Assert.That(ParryMath.Parryable(ghoul.transform), Is.True, "goule");
            }
            finally
            {
                Object.DestroyImmediate(stalker);
                Object.DestroyImmediate(ghoul);
            }
        }

        [Test]
        public void Parade_NeTouchePasAuMouvement()
        {
            // La parade n'a aucune référence au moteur : rien dans MovementInput, rien dans MovementState.
            Assert.That(typeof(ItHunts.Movement.MovementInput).GetField("ParryPressedTick"), Is.Null);
            Assert.That(typeof(ItHunts.Player.PlayerParry).GetField("_motor",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance), Is.Null);
        }
    }
}
