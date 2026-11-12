using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Une zone (D-033) : timer avant que le traqueur devienne enragé, temps de grâce au début, remise à zéro à la mort.
    /// Prototype M3 (Test_Arena) : timer de test de 4 min. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Zone_Test", menuName = "ItHunts/Réglages/Zone")]
    public sealed class ZoneSettings : ScriptableObject
    {
        [Tooltip("Timer de la zone (s) ; à 0, le traqueur devient enragé (20 min en jeu, 4 min en test).")]
        [Min(1f)] public float timer = 240f;
        [Tooltip("Temps de grâce au début de la zone (s) : le traqueur reste dans son repaire (60 s en jeu, 15 s en test).")]
        [Min(0f)] public float gracePeriod = 15f;
        [Tooltip("Après la mort du joueur, délai avant la remise à zéro de la zone (s).")]
        [Min(0f)] public float resetDelay = 1.5f;
        [Tooltip("Sortie atteinte : délai avant de rejouer la zone (s).")]
        [Min(0f)] public float exitRestartDelay = 4f;
    }
}
