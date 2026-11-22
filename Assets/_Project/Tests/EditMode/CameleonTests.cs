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
        public void HuitAmeliorations_ReserveesAuBarillet_AuMarchandEtDansLesCoffres()
        {
            var barrel = AssetDatabase.LoadAssetAtPath<BarrelSettings>(Dir + "/Barrel_Default.asset");
            var pool = AssetDatabase.LoadAssetAtPath<UpgradePool>(Dir + "/UpgradePool_Default.asset");
            var bpool = AssetDatabase.LoadAssetAtPath<UpgradePool>(Dir + "/UpgradePool_Barillet.asset");
            var kinds = new[]
            {
                UpgradeKind.BallesParalysantes, UpgradeKind.ExplosionSurKill, UpgradeKind.Perforation, UpgradeKind.Ricochet,
                UpgradeKind.RechargementSurKill, UpgradeKind.CritiquesEnChaine, UpgradeKind.ChanceCritique, UpgradeKind.BallesIncendiaires,
            };
            foreach (UpgradeKind k in kinds)
            {
                UpgradeDefinition u = pool.upgrades.FirstOrDefault(x => x != null && x.kind == k);
                Assert.That(u, Is.Not.Null, $"{k} dans les coffres");
                Assert.That(bpool.upgrades.Contains(u), Is.True, $"{k} au marchand de l'artefact");
                Assert.That(u.forArtifact, Is.EqualTo(barrel), $"{k} réservée au barillet");
            }

            // Un renard (lanterne) ne tire jamais une amélioration du barillet.
            var go = new GameObject("Run (test)");
            try
            {
                var run = go.AddComponent<RunState>();
                run.Configure(null, 42, pool);
                var lantern = AssetDatabase.LoadAssetAtPath<ArtifactDefinition>(Dir + "/FireLantern_Default.asset");
                for (int i = 0; i < 20; i++)
                    foreach (UpgradeDefinition u in run.DrawUpgrades(3, pool, lantern))
                        Assert.That(u.forArtifact == null || u.forArtifact == lantern, Is.True, u.displayName);
                bool seen = false;
                for (int i = 0; i < 30 && !seen; i++)
                    seen = run.DrawUpgrades(3, pool, barrel).Any(u => u.forArtifact == barrel);
                Assert.That(seen, Is.True, "le caméléon les voit");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
