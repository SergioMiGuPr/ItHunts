using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Retour visuel et sonore de l'orbe de magnétisme (greybox) : couleurs, lueurs, viseur, refus, sons
    /// synthétisés. Les couleurs au-dessus de 1 (intensité) brillent avec le bloom de la scène.
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "MagnetismFeedback_Default", menuName = "ItHunts/Réglages/Retour du magnétisme")]
    public sealed class MagnetismFeedbackSettings : ScriptableObject
    {
        [Header("Couleurs")]
        public Color attractColor = new Color(0.2f, 0.55f, 1f);
        public Color repelColor = new Color(1f, 0.5f, 0.1f);
        public Color readyColor = new Color(0.8f, 0.85f, 0.95f);
        public Color rechargeColor = new Color(0.16f, 0.16f, 0.18f);
        public Color refusalColor = new Color(1f, 0.15f, 0.15f, 0.95f);

        [Header("Lueur (intensité HDR, bloom au-dessus de 1)")]
        [Min(0f)] public float orbUsageIntensity = 2.5f;
        [Min(0f)] public float orbReadyIntensity = 0.7f;
        [Min(0f)] public float lineIntensity = 2f;
        [Min(0f)] public float repelAimMarkerIntensity = 1.5f;
        [Min(0f)] public float anchorIntensity = 3f;
        [Min(0f)] public float flashIntensity = 4f;
        [Tooltip("Orbe unique : durée de l'éclat de couleur d'une action brève (répulsion orange, lancer vert), qui " +
                 "l'emporte sur la traction (bleu) et l'objet tenu (violet) (s).")]
        [Min(0.02f)] public float orbFlashDuration = 0.15f;

        [Header("Marques dans le monde")]
        [Tooltip("Au-delà de cette distance (m), les marques grandissent avec la distance : même taille à l'écran.")]
        [Min(0.5f)] public float markerReferenceDistance = 8f;

        [Header("Éclats (s, multiplicateur de taille)")]
        [Min(0.05f)] public float tractionFlashDuration = 0.35f;
        [Min(0f)] public float tractionFlashGrowth = 3f;
        [Min(0.05f)] public float repelFlashDuration = 0.8f;
        [Min(0f)] public float repelFlashGrowth = 4f;

        [Header("Viseur : anneaux (pixels)")]
        [Tooltip("Anneau bleu : une traction est possible (surface à portée et une charge d'attraction).")]
        [Min(1f)] public float attractRingRadius = 8f;
        [Tooltip("Anneau orange : la répulsion est possible (surface à portée et répulsion prête).")]
        [Min(1f)] public float repelRingRadius = 13f;
        [Min(0.5f)] public float ringThickness = 2f;

        [Header("Refus")]
        [Tooltip("Durée du clignotement rouge du groupe concerné du HUD (s).")]
        [Min(0f)] public float refusalFlashDuration = 0.3f;
        [Tooltip("Période d'un clignotement (s).")]
        [Min(0.02f)] public float refusalBlinkPeriod = 0.1f;

        [Header("Sons (synthétisés, provisoires)")]
        [Range(0f, 1f)] public float masterVolume = 0.6f;
        [Range(0f, 1f)] public float tractionStartVolume = 0.7f;
        [Range(0f, 1f)] public float tractionEndVolume = 0.5f;
        [Range(0f, 1f)] public float repelVolume = 0.9f;
        [Range(0f, 1f)] public float refusalVolume = 0.6f;
        [Range(0f, 1f)] public float readyVolume = 0.35f;
        [Range(0f, 1f)] public float humVolume = 0.35f;
        [Tooltip("Hauteur du bourdonnement de traction à l'arrêt et à humSpeedForMaxPitch (1 = 110 Hz).")]
        [Min(0.1f)] public float humPitchMin = 0.8f;
        [Min(0.1f)] public float humPitchMax = 2f;
        [Min(1f)] public float humSpeedForMaxPitch = 40f;
    }
}
