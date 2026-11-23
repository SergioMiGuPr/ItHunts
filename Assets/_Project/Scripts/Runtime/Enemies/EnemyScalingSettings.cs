using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>Réglages du scaling des ennemis (nuit 4), modifiables en Play Mode.</summary>
    [CreateAssetMenu(fileName = "EnemyScaling_Default", menuName = "ItHunts/Réglages/Scaling des ennemis")]
    public sealed class EnemyScalingSettings : ScriptableObject
    {
        [Header("Par point de puissance du joueur (améliorations + rangs)")]
        [Range(0f, 0.5f)] public float healthPerPower = 0.12f;
        [Range(0f, 0.5f)] public float damagePerPower = 0.06f;
        [Range(0f, 0.5f)] public float countPerPower = 0.06f;
        [Tooltip("Exposant sur la puissance (< 1 : le joueur progresse plus vite que les goules).")]
        [Range(0.5f, 1f)] public float powerExponent = 0.9f;
        [Header("Par acte (le stuff est gardé d'un acte à l'autre)")]
        [Range(1f, 2f)] public float healthPerAct = 1.25f;
        [Range(1f, 2f)] public float damagePerAct = 1.15f;
        [Range(1f, 2f)] public float countPerAct = 1.2f;
        [Range(1f, 5f)] public float countMax = 2.5f;
    }
}
