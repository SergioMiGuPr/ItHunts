using ItHunts.Economy;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;

namespace ItHunts.Tests
{
    /// <summary>D-125 (playtest du 26/11) : une commune qui ne sert qu'avec du feu n'est proposée qu'au renard.</summary>
    public class UpgradeRulesNightTests
    {
        [Test]
        public void Allumette_SeulementPourLeRenard_LesAutresCommunesPourTous()
        {
            var g28 = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>("Assets/_Project/Settings/Upgrades/Upgrade_G28.asset");
            var g1 = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>("Assets/_Project/Settings/Upgrades/Upgrade_G1.asset");
            Assert.IsTrue(g28.needsFire && g28.Generic, "Allumette : commune qui demande du feu");
            Assert.IsTrue(UpgradeRules.VisibleBy(g28, UpgradeCharacter.Renard));
            foreach (UpgradeCharacter c in new[] { UpgradeCharacter.Cameleon, UpgradeCharacter.Koala, UpgradeCharacter.Hibou })
            {
                Assert.IsFalse(UpgradeRules.VisibleBy(g28, c), c.ToString());
                Assert.IsTrue(UpgradeRules.VisibleBy(g1, c), "Riposte pour " + c);
            }
        }
    }
}
