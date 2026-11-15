using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Vagues d'une zone (D-041, revu après le playtest des étapes 8 et 9, D-060) : les ennemis apparaissent seulement au début
    /// de chaque pic de vague (une fournée, waveSpawns), jamais en continu ni pour remplacer les tués. La fournée et le plafond
    /// de population suivent le temps de la zone (en part du timer), plus hauts en Enragé. Apparitions hors de la vue de tous les
    /// joueurs, entre minDistance et maxDistance, jamais dans une salle où un joueur est passé depuis recentVisitSeconds, jamais
    /// dans la salle de départ, sur des points d'apparition (SpawnPoint). Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Waves_Default", menuName = "ItHunts/Vagues")]
    public sealed class WaveSettings : ScriptableObject
    {
        [Header("Tendance (part du timer, interpolé)")]
        [Tooltip("Moments de la tendance, en part du timer de la zone (0 = début, 1 = fin du timer).")]
        public float[] trendTimes = { 0f, 0.25f, 0.5f, 0.75f, 1f };
        [Tooltip("Plafond de population des goules (vivantes sur la carte) à chacun de ces moments : une vague n'apparaît " +
                 "que jusqu'à ce plafond.")]
        public int[] ghoulTrend = { 10, 20, 35, 50, 60 };
        [Tooltip("Plafond de population des volants à chacun de ces moments.")]
        public int[] flyerTrend = { 2, 4, 8, 12, 16 };
        [Tooltip("En Enragé (après le timer) : plafonds des goules et des volants.")]
        [Min(0)] public int ghoulEnraged = 80;
        [Min(0)] public int flyerEnraged = 20;

        [Header("Pics et accalmies")]
        [Tooltip("Durée d'un pic de vague (s), annoncé par un son et une indication dans le HUD.")]
        [Min(1f)] public float peakDuration = 20f;
        [Tooltip("Durée d'une accalmie (s). La zone commence par une accalmie.")]
        [Min(0f)] public float lullDuration = 15f;
        [Tooltip("Durée de l'indication « vague » dans le HUD (s).")]
        [Min(0f)] public float peakAnnounceTime = 1.5f;

        [Header("Vagues d'apparitions (seulement au début d'un pic)")]
        [Tooltip("Goules qui apparaissent au début de chaque pic, aux moments de la tendance (interpolé), × joueurs × surface.")]
        public int[] ghoulWaveSpawns = { 3, 4, 6, 8, 10 };
        [Tooltip("Volants qui apparaissent au début de chaque pic.")]
        public int[] flyerWaveSpawns = { 1, 1, 2, 2, 3 };
        [Tooltip("En Enragé : goules et volants par vague.")]
        [Min(0)] public int ghoulWaveEnraged = 14;
        [Min(0)] public int flyerWaveEnraged = 4;
        [Tooltip("Les apparitions d'une vague s'étalent au plus sur ce temps après le début du pic (s) ; ce qui n'a pas trouvé " +
                 "de point à temps est abandonné.")]
        [Min(0.5f)] public float waveSpawnWindow = 8f;
        [Tooltip("Écart entre deux apparitions d'une même vague (s).")]
        [Min(0.02f)] public float waveSpawnStep = 0.2f;
        [Tooltip("Jamais d'apparition dans une salle ou un tronçon où un joueur est passé depuis ce temps (s).")]
        [Min(0f)] public float recentVisitSeconds = 60f;

        [Header("Apparitions")]
        [Tooltip("Distance au joueur (m) : jamais plus près que min de tous les joueurs, au plus max du plus proche.")]
        [Min(0f)] public float minDistance = 25f;
        [Min(0f)] public float maxDistance = 80f;
        [Tooltip("Annonce d'une apparition (s) : lueur au sol au point choisi, puis l'ennemi apparaît.")]
        [Min(0f)] public float telegraph = 0.6f;
        [Tooltip("Un ennemi mort retourne dans la réserve après ce temps (s).")]
        [Min(0f)] public float despawnDelay = 2f;

        [Header("Coop (D-041)")]
        [Tooltip("Nombre voulu et plafonds de chasse × (1 + cette part × joueurs vivants en plus) : 0.5 → ×1.5 à 2, ×2.5 à 4. " +
                 "Chaque joueur a sa part ; les apparitions se font à tour de rôle autour de chacun.")]
        [Min(0f)] public float extraPlayerShare = 0.5f;

        [Header("Chasseurs (D-061)")]
        [Tooltip("Paliers des chasseurs, en part du timer : à partir de chacun de ces moments, chaque joueur debout a le nombre " +
                 "de chasseurs correspondant (ennemis qui savent toujours où il est). Pas d'interpolation.")]
        public float[] chaserTimes = { 0f, 0.15f, 0.35f, 0.55f, 0.75f };
        [Tooltip("Chasseurs par joueur à chaque palier.")]
        public int[] chaserCounts = { 1, 2, 4, 6, 8 };
        [Tooltip("Chasseurs par joueur en Enragé.")]
        [Min(0)] public int chaserEnraged = 10;

        [Header("Habitants des salles (posés par le constructeur)")]
        [Tooltip("Au début de la zone, chaque grande salle garde entre min et max goules habitantes selon sa taille (les autres " +
                 "posées par le constructeur sont retirées), et au plus residentFlyersMax volants.")]
        [Range(0, 10)] public int residentGhoulsMin = 1;
        [Range(0, 10)] public int residentGhoulsMax = 3;
        [Range(0, 5)] public int residentFlyersMax = 1;
        [Tooltip("Côté de salle (m) qui donne le minimum, et celui qui donne le maximum (interpolé).")]
        [Min(1f)] public float residentRoomSmall = 46f;
        [Min(1f)] public float residentRoomLarge = 66f;

        [Header("Taille de la carte (étape 8, après playtest)")]
        [Tooltip("Le nombre voulu suit la surface de la carte (somme du sol des volumes de salle et de tronçon) : × surface / " +
                 "referenceArea, borné par maxAreaScale. Les plafonds de chasse ne changent pas.")]
        public bool scaleWithMapArea = true;
        [Tooltip("Surface de référence (m²) : celle de Test_Arena (36 254 m² mesurés), où la courbe a été réglée.")]
        [Min(1000f)] public float referenceArea = 36300f;
        [Range(0.25f, 6f)] public float minAreaScale = 0.5f;
        [Range(0.25f, 6f)] public float maxAreaScale = 2.5f;

        [Header("Réserve (scène)")]
        [Tooltip("Goules et volants créés par le constructeur de scène, inactifs, réutilisés par les vagues (× l'échelle de surface).")]
        [Min(0)] public int ghoulPool = 80;
        [Min(0)] public int flyerPool = 20;
    }
}
