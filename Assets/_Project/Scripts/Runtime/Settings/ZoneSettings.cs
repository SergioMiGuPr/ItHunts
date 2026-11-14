using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Une zone, c'est-à-dire un niveau (D-033, D-048) : le timer démarre quand le premier joueur sort de la salle de départ ;
    /// traqueur lâché à gracePeriod, porte de sortie déverrouillée à exitUnlockAt puis payée en âmes, Enragé à timer.
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Zone_Test", menuName = "ItHunts/Réglages/Zone")]
    public sealed class ZoneSettings : ScriptableObject
    {
        [Tooltip("Timer de la zone (s) depuis la sortie de la salle de départ ; à 0, le traqueur devient enragé et les âmes " +
                 "comptent double (D-048 : 15 min).")]
        [Min(1f)] public float timer = 900f;
        [Tooltip("Le traqueur reste dans son repaire jusqu'à ce temps (s) du timer (D-048 : 3 min).")]
        [Min(0f)] public float gracePeriod = 180f;
        [Tooltip("La porte de sortie se déverrouille à ce temps (s) du timer, puis se paie en âmes (D-048 : 10 min).")]
        [Min(0f)] public float exitUnlockAt = 600f;
        [Tooltip("Prix de la porte de sortie (âmes ; payé une fois par un joueur, elle s'ouvre pour tous).")]
        [Min(0)] public int exitPrice = 200;
        [Tooltip("Le timer attend que le premier joueur sorte de la salle de départ (volume « Départ »). Sans salle de départ " +
                 "dans la scène, il démarre tout de suite.")]
        public bool timerWaitsForStartRoom = true;

        [Header("Mort et fin")]
        [Tooltip("Vrai : la mort du dernier joueur vivant termine la run (écran de fin, puis menu). Faux : la zone recommence " +
                 "(ancien comportement de test).")]
        public bool deathEndsRun = true;
        [Tooltip("Après la mort du joueur (deathEndsRun faux), délai avant la remise à zéro de la zone (s).")]
        [Min(0f)] public float resetDelay = 1.5f;
        [Tooltip("Délai avant l'écran de fin, après la mort ou la victoire (s).")]
        [Min(0f)] public float exitRestartDelay = 2f;
    }
}
