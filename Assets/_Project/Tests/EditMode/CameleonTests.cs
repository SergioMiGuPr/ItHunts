using System.Linq;
using ItHunts.Artifacts;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Caméléon (barillet), nuit 3 point 6 : headshots ×2, critiques, 8 améliorations réservées au barillet, réglages.</summary>
    public sealed class CameleonTests
    {
        private const string Dir = "Assets/_Project/Settings";

        [Test]
        public void Headshot_DegatsDoubles_EtCritiquesEnChaine()
        {
            var mods = AssetDatabase.LoadAssetAtPath<BarrelModsSettings>(Dir + "/BarrelMods_Default.asset");
            Assert.That(mods, Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<BarrelSettings>(Dir + "/Barrel_Default.asset").mods, Is.EqualTo(mods), "branché sur le barillet");
            Assert.That(mods.headshotMultiplier, Is.EqualTo(2f));
            Assert.That(BarrelShooter.CritMultiplier(mods, 0, 3), Is.EqualTo(2f), "sans Sang-froid : ×2");
            Assert.That(BarrelShooter.CritMultiplier(mods, 1, 1), Is.EqualTo(2.5f));
            Assert.That(BarrelShooter.CritMultiplier(mods, 1, 10), Is.EqualTo(2f + 0.5f * mods.chainMax), "borné");
            Assert.That(BarrelShooter.CritChance(0, 0.12f), Is.EqualTo(0f));
            Assert.That(BarrelShooter.CritChance(1, 0.12f), Is.EqualTo(1f - 1f / 1.12f).Within(1e-5f));
            Assert.That(BarrelShooter.CritChance(50, 0.12f), Is.LessThan(1f), "hyperbolique : jamais 100 %");
        }

        [Test]
        public void DixAmeliorations_DuCameleon_ReserveesASonPorteur()
        {
            // Nuit 4 (D-100) : 2 passifs + 8 améliorations du barillet, dans la table du caméléon, jamais proposées au renard.
            var barrel = AssetDatabase.LoadAssetAtPath<BarrelSettings>(Dir + "/Barrel_Default.asset");
            var lantern = AssetDatabase.LoadAssetAtPath<ArtifactDefinition>(Dir + "/FireLantern_Default.asset");
            var bpool = AssetDatabase.LoadAssetAtPath<UpgradePool>(Dir + "/UpgradePool_Cameleon.asset");
            Assert.That(barrel.upgrades, Is.SameAs(bpool), "la table du barillet est celle du caméléon");
            Assert.That(bpool.upgrades.Count, Is.EqualTo(10));
            Assert.That(bpool.upgrades.Count(u => u.isPassive), Is.EqualTo(2));
            var kinds = new[]
            {
                UpgradeKind.BallesParalysantes, UpgradeKind.CadavreExplosif, UpgradeKind.BallePerforante, UpgradeKind.Ricochet,
                UpgradeKind.MainLeste, UpgradeKind.SixiemeChambre, UpgradeKind.Eventail, UpgradeKind.LangueDeCameleon,
            };
            foreach (UpgradeKind k in kinds)
            {
                UpgradeDefinition u = bpool.upgrades.FirstOrDefault(x => x != null && x.kind == k);
                Assert.That(u, Is.Not.Null, $"{k} dans la table du caméléon");
                Assert.That(u.character, Is.EqualTo(UpgradeCharacter.Cameleon), $"{k} réservée au caméléon");
                Assert.That(ItHunts.Economy.UpgradeRules.VisibleBy(u, UpgradeCharacter.Renard, lantern), Is.False, $"{k} jamais pour le renard");
                Assert.That(ItHunts.Economy.UpgradeRules.VisibleBy(u, UpgradeCharacter.Cameleon, barrel), Is.True, $"{k} pour le caméléon");
            }
        }
    }
}
