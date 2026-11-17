using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Lumière d'ambiance horreur (playtest du 2026-11-17) : des flaques de lumière dramatiques dans le noir. Chaque élément est
    /// activable et réglable séparément (aussi en Play Mode) ; l'étalonnage propre à chaque ambiance est dans AmbianceSettings
    /// (« Étalonnage »). Les effets d'écran (bloom, vignettage, grain, aberration) se coupent aussi dans le menu pause (GraphicsOptions).
    /// Lisibilité : les objets interactifs brillent par leur émission (le bloom les garde), le rouge du danger reste le plus saturé.
    /// </summary>
    [CreateAssetMenu(fileName = "HorrorLook_Default", menuName = "ItHunts/Réglages/Lumière d'ambiance horreur")]
    public sealed class HorrorLookSettings : ScriptableObject
    {
        [Header("Étalonnage des couleurs (valeurs dans chaque AmbianceSettings)")]
        public bool grading = true;

        [Header("Enragé : bascule progressive vers un rouge sang presque monochrome")]
        public bool enragedMonochrome = true;
        [Tooltip("Durée de la bascule complète (s) après le passage en Enragé.")]
        [Min(0.1f)] public float enragedMonoSeconds = 12f;
        [Tooltip("Teinte du monochrome (passe « ItHunts Enragé (monde) » : luminance × cette couleur ; HDR : le rouge garde la luminosité).")]
        [ColorUsage(false, true)] public Color enragedTint = new Color(1.3f, 0.3f, 0.26f);
        [Tooltip("Part de la couleur d'origine gardée dans le monde au bout de la bascule (la vue FPS et le HUD ne sont jamais teintés).")]
        [Range(0f, 1f)] public float enragedKeepColor = 0.35f;
        [Tooltip("Contraste ajouté au bout de la bascule.")]
        [Range(0f, 60f)] public float enragedContrast = 8f;
        [Tooltip("Gain du monochrome (EV) : le rouge porte environ la moitié de la luminance perçue ; le monde garde sa luminosité.")]
        [Range(-1f, 2f)] public float enragedExposure = 0.8f;

        [Header("Halos autour des torches, lampes et lanternes (un système de particules : un seul draw call)")]
        public bool halos = true;
        [Tooltip("Matériau ItHunts/Halo (additif, dégradé radial).")]
        public Material haloMaterial;
        [Tooltip("Diamètre du halo d'une torche et d'une lampe (m).")]
        [Min(0.1f)] public float torchHaloSize = 3f;
        [Min(0.1f)] public float lampHaloSize = 3.2f;
        [Tooltip("Intensité (alpha additif) d'un halo.")]
        [Range(0f, 1f)] public float haloIntensity = 0.5f;
        [Tooltip("Halo de la lanterne du joueur (vu par les autres joueurs et dans les reflets ; le sien est caché à 1 m de la caméra).")]
        [Min(0.1f)] public float lanternHaloSize = 1.4f;
        [Tooltip("Décalage du halo devant le mur de la torche (m) : il ne se coupe pas dans la pierre.")]
        [Min(0f)] public float haloWallOffset = 0.35f;

        [Header("Torches qui vacillent, lampes qui grésillent")]
        public bool flicker = true;
        [Tooltip("Variation d'intensité des torches (fraction) et vitesse du bruit.")]
        [Range(0f, 0.6f)] public float torchFlickerAmount = 0.07f;
        [Min(0.1f)] public float torchFlickerSpeed = 2.2f;
        [Tooltip("Déplacement de la flamme (m). 0 (conseillé) : la lumière ne bouge pas, ses ombres non plus (playtest du 2026-11-17).")]
        [Min(0f)] public float torchJitter = 0f;
        [Tooltip("Part des lampes qui grésillent (tirée une fois par lampe) ; une coupure de 0.05 à 0.4 s toutes les 2 à 9 s.")]
        [Range(0f, 1f)] public float buzzingLampShare = 0.2f;
        [Range(0f, 1f)] public float buzzDepth = 0.85f;
        [Tooltip("Seules les lumières à moins de cette distance d'un joueur bougent (m) : le reste ne coûte rien.")]
        [Min(5f)] public float flickerRadius = 45f;

        [Header("Lumières au-delà du brouillard : éteintes (Forward+ n'en garde que 256 visibles, il en choisissait au hasard de la position)")]
        public bool lightCulling = true;
        [Tooltip("Une lumière du décor s'éteint quand toute sa portée est plus loin que la fin du brouillard + cette marge (m), pour chaque joueur.")]
        [Min(0f)] public float lightCullMargin = 6f;
        [Tooltip("Elle ne se rallume qu'en revenant à (marge − hystérésis) : pas de bascule au bord.")]
        [Min(0f)] public float lightCullHysteresis = 4f;

        [Header("Lanterne du personnage : cône vers l'avant qui projette des ombres")]
        public bool lanternCone = true;
        [Range(10f, 120f)] public float lanternConeAngle = 62f;
        [Min(1f)] public float lanternConeRange = 20f;
        [Min(0f)] public float lanternConeIntensity = 3.5f;
        [Tooltip("Le cône part un peu en avant et au-dessus de la caméra (repère caméra, m).")]
        public Vector3 lanternConeOffset = new Vector3(0.25f, 0.15f, 0.1f);
        [Tooltip("Le cône suit le regard avec ce lissage (1/s) : il ne tremble pas avec le balancement de la marche (ombres qui rampent).")]
        [Min(1f)] public float lanternConeFollow = 18f;

        [Header("Ombres en temps réel : seulement les N lumières les plus proches des joueurs")]
        public bool shadowBudget = true;
        [Tooltip("Nombre de lumières qui projettent des ombres (cônes des lanternes compris, prioritaires). Une lumière ponctuelle coûte 6 faces.")]
        [Range(0, 8)] public int shadowedLights = 3;
        [Tooltip("Ombres des torches et des lampes plus loin que ça : jamais (m).")]
        [Min(1f)] public float shadowMaxDistance = 30f;
        [Tooltip("Intervalle de tri (s).")]
        [Min(0.05f)] public float shadowRefresh = 0.25f;
        [Tooltip("Une lumière ombrée garde sa place tant qu'une autre n'est pas plus proche d'autant (m).")]
        [Min(0f)] public float shadowHysteresis = 4f;
        [Tooltip("Fondu de l'ombre quand une lumière entre ou sort du budget (s) : pas de bascule.")]
        [Min(0.01f)] public float shadowFade = 0.6f;
        [Tooltip("Biais des ombres des lumières additionnelles (profondeur, normale) : dents de scie et acné.")]
        [Min(0f)] public float shadowDepthBias = 0.15f;
        [Min(0f)] public float shadowNormalBias = 0.7f;

        [Header("Effets d'écran (chacun aussi désactivable dans le menu pause)")]
        public bool bloom = true;
        [Min(0f)] public float bloomIntensity = 0.9f;
        [Min(0f)] public float bloomThreshold = 0.95f;
        [Range(0f, 1f)] public float bloomScatter = 0.65f;
        public bool vignette = true;
        [Range(0f, 1f)] public float vignetteIntensity = 0.32f;
        [Range(0.01f, 1f)] public float vignetteSmoothness = 0.45f;
        public bool filmGrain = true;
        [Range(0f, 1f)] public float grainIntensity = 0.18f;
        public bool chromaticAberration = true;
        [Range(0f, 1f)] public float chromaticIntensity = 0.06f;
    }
}
