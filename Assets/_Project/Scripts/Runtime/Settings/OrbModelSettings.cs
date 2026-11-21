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
        [Min(0.05f)] public float modelScale = 0.5f;

        [Header("Couleur (celle du personnage, D-066)")]
        [Tooltip("Couleur sans personnage (scènes de test).")]
        public Color fallbackColor = new Color(0.56f, 0.11f, 0.07f);
        [Tooltip("Intensité de l'énergie (HDR : brille avec le bloom).")]
        [Min(0f)] public float energyIntensity = 1.7f;
        [Tooltip("Pulsation de l'énergie au repos (fraction) et sa fréquence (Hz).")]
        [Range(0f, 1f)] public float energyPulse = 0.1f;
        [Min(0f)] public float energyPulseHz = 0.5f;
        [Tooltip("Énergie plus vive pendant la traction (multiplicateur).")]
        [Min(1f)] public float tractionEnergyBoost = 1.5f;
        [Tooltip("Tourbillon : rotation de l'énergie sur elle-même (°/s) et son axe (repère de l'orbe).")]
        public float energySpin = 0f;                                   // 0 : le tourbillon est dans le shader (ItHunts/OrbeVortex)
        public Vector3 energySpinAxis = new Vector3(0.3f, 1f, 0.15f);

        [Tooltip("Tourbillon pendant la traction : multiplicateur de _SpinBoost (ItHunts/OrbeVortex).")]
        [Min(1f)] public float vortexTractionBoost = 3.5f;

        [Tooltip("Tourbillon au repos : multiplicateur de _SpinBoost (l'orbe est immobile, l'énergie remue à peine).")]
        [Min(0f)] public float vortexRestBoost = 0.4f;

        [Header("Repos et action (D-069, lisibilité) : immobile au repos, elle ne tourne que pendant une action")]
        [Tooltip("Au repos, la couronne de tablettes fait face à la caméra (sinon : orientation du poignet).")]
        public bool restFacesCamera = true;
        [Tooltip("Inclinaison de la couronne au repos par rapport à la caméra (°, repère de la vue) : un peu de relief.")]
        public Vector3 restTilt = new Vector3(-12f, 0f, 0f);
        [Tooltip("Montée de l'activité au début d'une action (traction, répulsion, prise, lancer) (1/s).")]
        [Min(0.1f)] public float actionRise = 14f;
        [Tooltip("Retour au repos après l'action (1/s) : anneaux et couronne reviennent à leur place en douceur.")]
        [Min(0.1f)] public float restReturn = 3f;
        [Tooltip("Rotation de la couronne de tablettes pendant une action (°/s).")]
        public float actionCrownSpin = 220f;
        [Tooltip("Une répulsion, une prise ou un lancer comptent comme action pendant ce temps (s) après le geste.")]
        [Min(0f)] public float gestureActionSeconds = 0.45f;

        [Header("Anneaux (anneau 1 : axe X, 2 : Y, 3 : Z)")]
        [Tooltip("Rotation de base (°/s), désynchronisée ; × tractionRingBoost pendant une action, immobile au repos.")]
        public Vector3 ringSpeeds = new Vector3(20f, 30f, 45f);
        [Tooltip("Pendant une action, les anneaux tournent à ringSpeeds × ce multiplicateur.")]
        [Min(1f)] public float tractionRingBoost = 8f;

        [Header("Tablettes (D-069) : une par charge de traction et de répulsion ; allumée = charge prête")]
        [Tooltip("Intensité d'un glyphe allumé (HDR : légère lueur avec le bloom, pas de vraie lumière) ; éteint : rune sombre.")]
        [Min(0f)] public float glyphIntensity = 3f;
        [Tooltip("Apparition d'une tablette ajoutée par une amélioration (s).")]
        [Min(0f)] public float tabletAppearSeconds = 0.35f;
        [Tooltip("Glissement des tablettes vers leur nouvelle place quand leur nombre change (°/s).")]
        [Min(1f)] public float tabletSlideSpeed = 240f;
        [Tooltip("Rayon de la couronne des tablettes seules (m, à l'échelle du modèle : 0.13 d'après la DA).")]
        [Min(0.01f)] public float crownRadius = 0.15f;
    }
}
