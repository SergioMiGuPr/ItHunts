using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Vagues d'une zone (bloc 7, D-041) : le nombre d'ennemis voulus suit une tendance qui monte avec le temps de la zone (en part
    /// du timer : marche avec 4 min en test et 20 min en vrai), plus haute en Enragé ; par-dessus, des pics de vague annoncés et
    /// des accalmies (nombre voulu à lullShare de la tendance). Les ennemis tués se reconstituent lentement. Apparitions hors de la
    /// vue de tous les joueurs, entre minDistance et maxDistance, sur des points d'apparition (SpawnPoint). Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Waves_Default", menuName = "ItHunts/Vagues")]
    public sealed class WaveSettings : ScriptableObject
    {
        [Header("Tendance (part du timer → nombre voulu, interpolé)")]
        [Tooltip("Moments de la tendance, en part du timer de la zone (0 = début, 1 = fin du timer).")]
        public float[] trendTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        [Tooltip("Goules voulues à chacun de ces moments (pendant un pic).")]
        public int[] ghoulTrend = { 10, 20, 35, 50, 60 };
        [Tooltip("Volants voulus à chacun de ces moments (pendant un pic).")]
        public int[] flyerTrend = { 2, 4, 8, 12, 16 };
        [Tooltip("En Enragé (après le timer) : goules et volants voulus.")]
        [Min(0)] public int ghoulEnraged = 80;
        [Min(0)] public int flyerEnraged = 20;

        [Header("Pics et accalmies")]
        [Tooltip("Durée d'un pic de vague (s), annoncé par un son et une indication dans le HUD.")]
        [Min(1f)] public float peakDuration = 20f;
        [Tooltip("Durée d'une accalmie (s). La zone commence par une accalmie.")]
        [Min(0f)] public float lullDuration = 15f;
        [Tooltip("Pendant une accalmie, le nombre voulu descend à cette part de la tendance.")]
        [Range(0f, 1f)] public float lullShare = 0.6f;
        [Tooltip("Pendant une accalmie, le plafond de chasse (sans l'aggro) descend à cette part : les ennemis en trop (les plus " +
                 "loin) repassent en attente et reculent ; au pic suivant, ils reprennent la chasse. Personne ne disparaît.")]
        [Range(0f, 1f)] public float lullHuntingShare = 0.6f;
        [Tooltip("Durée de l'indication « vague » dans le HUD (s).")]
        [Min(0f)] public float peakAnnounceTime = 1.5f;

        [Header("Apparitions")]
        [Tooltip("Intervalle entre deux apparitions (s) : au début, à la fin du timer (interpolé), en Enragé.")]
        [Min(0.05f)] public float spawnIntervalStart = 3f;
        [Min(0.05f)] public float spawnIntervalEnd = 1f;
        [Min(0.05f)] public float spawnIntervalEnraged = 0.5f;
        [Tooltip("Distance au joueur (m) : jamais plus près que min de tous les joueurs, au plus max du plus proche.")]
        [Min(0f)] public float minDistance = 25f;
        [Min(0f)] public float maxDistance = 80f;
        [Tooltip("Annonce d'une apparition (s) : lueur au sol au point choisi, puis l'ennemi apparaît.")]
        [Min(0f)] public float telegraph = 0.6f;
        [Tooltip("Ennemis tués : le nombre voulu se reconstitue au plus d'un ennemi toutes ces secondes, en plus des apparitions " +
                 "normales (jamais d'un coup).")]
        [Min(0.1f)] public float refillInterval = 4f;
        [Tooltip("Un ennemi mort retourne dans la réserve après ce temps (s).")]
        [Min(0f)] public float despawnDelay = 2f;
        [Tooltip("Au début de la zone, la population de départ est posée d'un coup (hors de vue), sans annonce.")]
        public bool fillAtStart = true;

        [Header("Coop (D-041)")]
        [Tooltip("Nombre voulu et plafonds de chasse × (1 + cette part × joueurs vivants en plus) : 0.5 → ×1.5 à 2, ×2.5 à 4. " +
                 "Chaque joueur a sa part ; les apparitions se font à tour de rôle autour de chacun.")]
        [Min(0f)] public float extraPlayerShare = 0.5f;

        [Header("Plafonds de chasse")]
        [Tooltip("Les plafonds de chasse des goules et des volants (et leur total avec l'aggro) suivent la tendance : réglage de " +
                 "l'ennemi × tendance du moment / tendance à la fin du timer (Enragé : au-dessus).")]
        public bool scaleHuntingCaps = true;

        [Header("Réserve (scène)")]
        [Tooltip("Goules et volants créés par le constructeur de scène, inactifs, réutilisés par les vagues.")]
        [Min(0)] public int ghoulPool = 80;
        [Min(0)] public int flyerPool = 20;
    }
}
