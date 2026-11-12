using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Ennemis factices de la zone de test : réglages communs des ennemis (EnemySettings), plus la cible mobile.
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "TrainingDummy_Default", menuName = "ItHunts/Réglages/Ennemi factice")]
    public sealed class TrainingDummySettings : EnemySettings
    {
        [Header("Cible mobile")]
        [Tooltip("Vitesse de ses allers-retours (m/s).")]
        [Min(0f)] public float moveSpeed = 6f;
        [Tooltip("Pause à chaque bout (s).")]
        [Min(0f)] public float pauseAtEnds = 0.3f;
    }
}
