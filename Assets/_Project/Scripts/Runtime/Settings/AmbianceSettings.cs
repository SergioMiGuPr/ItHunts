using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Ambiance de rendu d'une scène (docs/art/RENDU.md § 3, décision D-067) : lumière ambiante, brouillard linéaire, lune, torches
    /// et lampes (le shader toon dessine l'îlot d'une lumière ponctuelle sur sa portée), fondu du contour. Un asset partagé par
    /// toutes les scènes de jeu et par le générateur de la carte aléatoire (SceneAmbiance) ; « Ambiance_Intermediaire » est
    /// retenue, « Ambiance_Horreur » est prise en fondu au passage en Enragé (champ enraged). Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Ambiance_", menuName = "ItHunts/Réglages/Ambiance de rendu")]
    public sealed class AmbianceSettings : ScriptableObject
    {
        [Header("Ambiance et brouillard")]
        [Tooltip("Lumière ambiante (mode Flat) : plancher d'éclairage des faces non éclairées.")]
        public Color ambient = new Color(0.055f, 0.058f, 0.080f);
        [Tooltip("Brouillard linéaire : couleur (aussi le fond des caméras), début et fin (m).")]
        public Color fogColor = new Color(0.012f, 0.014f, 0.024f);
        [Min(0f)] public float fogStart = 10f;
        [Min(0.1f)] public float fogEnd = 85f;

        [Header("Lumières")]
        [Tooltip("Lune : intensité des lumières directionnelles de la scène.")]
        [Min(0f)] public float moonIntensity = 0.25f;
        [Tooltip("Torches et bougies : intensité et portée (îlot du toon).")]
        [Min(0f)] public float torchIntensity = 1.8f;
        [Min(0.5f)] public float torchRange = 11f;
        [Tooltip("Lampes des salles et des couloirs de la carte aléatoire : intensité et portée.")]
        [Min(0f)] public float lampIntensity = 1.8f;
        [Min(0.5f)] public float lampRange = 13f;

        [Header("Contours (M_Contours)")]
        [Tooltip("Fondu du contour avec la distance (m), réglé avec le brouillard. Réglé dans M_Contours pour l'ambiance retenue.")]
        [Min(0f)] public float contourFadeStart = 40f;
        [Min(0.1f)] public float contourFadeEnd = 100f;

        [Header("Textes en monde (sortie, salles, coffres, marchand)")]
        [Tooltip("Entièrement lisibles jusqu'à cette distance (m), puis fondu.")]
        [Min(0f)] public float labelFullDistance = 20f;
        [Tooltip("Invisibles au-delà (m). Jamais au-delà de labelFogShare × la fin du brouillard (pas à travers le brouillard).")]
        [Min(0.1f)] public float labelHiddenDistance = 35f;
        [Range(0.2f, 1f)] public float labelFogShare = 0.8f;

        /// <summary>Opacité d'un texte en monde à la distance d (m) : 1 jusqu'à full, 0 à hidden (et avant la fin du brouillard).</summary>
        public static float LabelAlpha(float d, float full, float hidden, float fogEnd, float fogShare)
        {
            float gone = Mathf.Min(hidden, fogEnd * fogShare);
            float start = Mathf.Min(full, gone - 0.5f);
            return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(start, gone, d));
        }

        [Header("Étalonnage (HorrorLookSettings.grading) : flaques de lumière dramatiques, pas un éclairage plat")]
        [Tooltip("Exposition (EV) : négatif assombrit tout, les sources HDR restent vives.")]
        [Range(-3f, 2f)] public float postExposure = 0f;
        [Tooltip("Contraste (−100..100) : creuse les noirs autour des îlots de lumière.")]
        [Range(-100f, 100f)] public float contrast = 15f;
        [Tooltip("Saturation (−100..100).")]
        [Range(-100f, 100f)] public float saturation = -10f;
        [Tooltip("Filtre de couleur global (blanc : aucun).")]
        public Color colorFilter = Color.white;
        [Tooltip("Virage des ombres et des hautes lumières (gris : aucun) ; équilibre (−100 : ombres, 100 : hautes lumières).")]
        public Color splitShadows = new Color(0.5f, 0.5f, 0.5f);
        public Color splitHighlights = new Color(0.5f, 0.5f, 0.5f);
        [Range(-100f, 100f)] public float splitBalance = 0f;

        [Header("Lumière d'ambiance horreur (halos, vacillement, lanterne, ombres, effets d'écran)")]
        [Tooltip("Réglages partagés par les ambiances ; vide : rien de tout ça (scènes de test).")]
        public HorrorLookSettings look;

        [Header("Phase enragée")]
        [Tooltip("Ambiance prise au passage en Enragé (fin du timer de la zone) ; aucune : l'ambiance ne change pas.")]
        public AmbianceSettings enraged;
        [Tooltip("Durée du fondu vers l'ambiance enragée (s), et retour à la remise à zéro de la zone.")]
        [Min(0f)] public float enragedTransition = 2f;

        /// <summary>Mélange de deux ambiances (t = 0 : a, 1 : b) dans into (couleurs, distances, intensités, portées).</summary>
        public static void Lerp(AmbianceSettings a, AmbianceSettings b, float t, AmbianceSettings into)
        {
            t = Mathf.Clamp01(t);
            into.ambient = Color.Lerp(a.ambient, b.ambient, t);
            into.fogColor = Color.Lerp(a.fogColor, b.fogColor, t);
            into.fogStart = Mathf.Lerp(a.fogStart, b.fogStart, t);
            into.fogEnd = Mathf.Lerp(a.fogEnd, b.fogEnd, t);
            into.moonIntensity = Mathf.Lerp(a.moonIntensity, b.moonIntensity, t);
            into.torchIntensity = Mathf.Lerp(a.torchIntensity, b.torchIntensity, t);
            into.torchRange = Mathf.Lerp(a.torchRange, b.torchRange, t);
            into.lampIntensity = Mathf.Lerp(a.lampIntensity, b.lampIntensity, t);
            into.lampRange = Mathf.Lerp(a.lampRange, b.lampRange, t);
            into.contourFadeStart = Mathf.Lerp(a.contourFadeStart, b.contourFadeStart, t);
            into.contourFadeEnd = Mathf.Lerp(a.contourFadeEnd, b.contourFadeEnd, t);
            into.labelFullDistance = Mathf.Lerp(a.labelFullDistance, b.labelFullDistance, t);
            into.labelHiddenDistance = Mathf.Lerp(a.labelHiddenDistance, b.labelHiddenDistance, t);
            into.labelFogShare = Mathf.Lerp(a.labelFogShare, b.labelFogShare, t);
            into.look = a.look != null ? a.look : b.look;
            into.postExposure = Mathf.Lerp(a.postExposure, b.postExposure, t);
            into.contrast = Mathf.Lerp(a.contrast, b.contrast, t);
            into.saturation = Mathf.Lerp(a.saturation, b.saturation, t);
            into.colorFilter = Color.Lerp(a.colorFilter, b.colorFilter, t);
            into.splitShadows = Color.Lerp(a.splitShadows, b.splitShadows, t);
            into.splitHighlights = Color.Lerp(a.splitHighlights, b.splitHighlights, t);
            into.splitBalance = Mathf.Lerp(a.splitBalance, b.splitBalance, t);
        }
    }
}
