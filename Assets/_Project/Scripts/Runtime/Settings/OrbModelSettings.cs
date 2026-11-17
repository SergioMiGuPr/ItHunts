using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Orbe v2 de la DA (SM_Orbe, docs/art/MATERIAUX.md « Orbe : couleur du personnage ») en vue FPS : taille, couleur du
    /// personnage (D-066), rotation des anneaux, tourbillon de l'énergie, jauge des tablettes. Le flottement au-dessus de la paume
    /// (hauteur, période, rotation d'ensemble) reste dans FpsViewSettings (floatAmplitude, floatPeriod, floatSpin).
    /// </summary>
    [CreateAssetMenu(fileName = "OrbModel_Default", menuName = "ItHunts/Vue FPS/Orbe (modèle)")]
    public sealed class OrbModelSettings : ScriptableObject
    {
        [Header("Taille")]
        [Tooltip("Échelle du modèle (1 = taille modélisée : anneaux 0.18 m, tablettes 0.295 m). 0.45 : anneaux de 8 cm, une pomme.")]
        [Min(0.05f)] public float modelScale = 0.45f;

        [Header("Couleur (celle du personnage, D-066)")]
        [Tooltip("Couleur sans personnage (scènes de test).")]
        public Color fallbackColor = new Color(0.56f, 0.11f, 0.07f);
        [Tooltip("Intensité de l'énergie (HDR : brille avec le bloom).")]
        [Min(0f)] public float energyIntensity = 1f;
        [Tooltip("Pulsation de l'énergie au repos (fraction) et sa fréquence (Hz).")]
        [Range(0f, 1f)] public float energyPulse = 0.1f;
        [Min(0f)] public float energyPulseHz = 0.5f;
        [Tooltip("Énergie plus vive pendant la traction (multiplicateur).")]
        [Min(1f)] public float tractionEnergyBoost = 1.5f;
        [Tooltip("Tourbillon : rotation de l'énergie sur elle-même (°/s) et son axe (repère de l'orbe).")]
        public float energySpin = 140f;
        public Vector3 energySpinAxis = new Vector3(0.3f, 1f, 0.15f);

        [Header("Anneaux (anneau 1 : axe X, 2 : Y, 3 : Z)")]
        [Tooltip("Rotation au repos (°/s), désynchronisée.")]
        public Vector3 ringSpeeds = new Vector3(20f, 30f, 45f);
        [Tooltip("Pendant la traction, les anneaux tournent beaucoup plus vite (multiplicateur).")]
        [Min(1f)] public float tractionRingBoost = 8f;
        [Tooltip("Lissage des changements de vitesse (1/s).")]
        [Min(0.1f)] public float ringResponse = 8f;

        [Header("Tablettes : une tablette allumée par charge prête")]
        [Tooltip("Intensité d'un glyphe allumé (HDR) ; éteint : noir.")]
        [Min(0f)] public float glyphIntensity = 1.3f;
        [Tooltip("Rotation de la couronne de tablettes pendant la traction (°/s) ; immobile au repos.")]
        public float tractionCrownSpin = 90f;
    }
}
