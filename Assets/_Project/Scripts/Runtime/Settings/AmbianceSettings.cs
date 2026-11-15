using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Ambiance de rendu d'une scène (docs/art/RENDU.md § 3, décision D-067) : lumière ambiante, brouillard linéaire, lune, torches
    /// et lampes (le shader toon dessine l'îlot d'une lumière ponctuelle sur sa portée), fondu du contour. Un asset partagé par
    /// toutes les scènes de jeu et par le générateur de la carte aléatoire (SceneAmbiance) ; « Ambiance_Intermediaire » est
    /// retenue, « Ambiance_Horreur » est prête pour une éventuelle phase enragée (non activée). Modifiable en Play Mode.
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
    }
}
