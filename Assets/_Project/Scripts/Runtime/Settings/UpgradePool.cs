using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>Améliorations qu'un coffre ou le marchand peut proposer (tirage avec la seed de la run).</summary>
    [CreateAssetMenu(fileName = "UpgradePool_Default", menuName = "ItHunts/Pool d'améliorations")]
    public sealed class UpgradePool : ScriptableObject
    {
        public List<UpgradeDefinition> upgrades = new List<UpgradeDefinition>();
    }
}
