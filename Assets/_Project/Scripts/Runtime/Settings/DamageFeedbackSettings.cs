using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Retour des dégâts (greybox) : chiffres flottants, barre de PV, éclats d'impact, éclair sur la cible, sons
    /// synthétisés provisoires. Couleurs HDR au-dessus de 1 : bloom. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "DamageFeedback_Default", menuName = "ItHunts/Réglages/Retour des dégâts")]
    public sealed class DamageFeedbackSettings : ScriptableObject
    {
        [Header("Couleurs par palier (petit, moyen, gros) et mort")]
        public Color smallColor = new Color(1f, 1f, 1f);
        public Color mediumColor = new Color(1f, 0.85f, 0.2f);
        public Color bigColor = new Color(1f, 0.25f, 0.15f);
        public Color killColor = new Color(1f, 0.1f, 0.1f);

        [Header("Chiffres de dégâts")]
        [Tooltip("Durée d'affichage (s) et montée (m).")]
        [Min(0.1f)] public float numberDuration = 1f;
        [Min(0f)] public float numberRise = 1.2f;
        [Tooltip("Taille du texte : petit coup, et en plus par point de dégât (pixels).")]
        [Min(6f)] public float numberBaseSize = 18f;
        [Min(0f)] public float numberSizePerDamage = 0.15f;
        [Min(6f)] public float numberMaxSize = 40f;

        [Header("Barres de PV")]
        [Tooltip("Visibles jusqu'à cette distance (m).")]
        [Min(1f)] public float healthBarDistance = 45f;
        [Min(1f)] public float healthBarWidth = 60f;
        [Min(1f)] public float healthBarHeight = 6f;

        [Header("Éclats d'impact")]
        [Min(0.02f)] public float flashDuration = 0.25f;
        [Tooltip("Taille de l'éclat (m) : petit coup, et en plus par point de dégât, plafond.")]
        [Min(0f)] public float flashBaseSize = 0.4f;
        [Min(0f)] public float flashSizePerDamage = 0.02f;
        [Min(0f)] public float flashMaxSize = 2.5f;
        [Min(0f)] public float flashIntensity = 4f;
        [Tooltip("Éclair blanc sur la cible touchée (s, intensité d'émission).")]
        [Min(0f)] public float targetFlashDuration = 0.1f;
        [Min(0f)] public float targetFlashIntensity = 2f;

        [Header("Zone de dégâts : cercle bref au sol (D-027)")]
        public Color areaColor = new Color(1f, 0.6f, 0.15f);
        [Min(0.02f)] public float areaRingDuration = 0.35f;
        [Min(0f)] public float areaRingIntensity = 2.5f;

        [Header("Objet cassé : débris (greybox)")]
        [Min(0)] public int debrisCount = 6;
        [Min(0.05f)] public float debrisLifetime = 1.2f;
        [Min(0f)] public float debrisSpeed = 5f;

        [Header("Étourdissement")]
        public Color stunColor = new Color(1f, 0.9f, 0.2f);
        [Min(0f)] public float stunGlowIntensity = 1.5f;

        [Header("Repère de durabilité de l'objet tenu (pixels)")]
        [Min(1f)] public float durabilityPipSize = 7f;
        public Color durabilityFull = new Color(0.65f, 0.4f, 1f, 0.95f);
        public Color durabilityUsed = new Color(0f, 0f, 0f, 0.6f);

        [Header("Sons (synthétisés, provisoires, 3D au point d'impact)")]
        [Range(0f, 1f)] public float hitVolume = 0.9f;
        [Range(0f, 1f)] public float killVolume = 1f;
        [Range(0f, 1f)] public float breakVolume = 0.9f;
        [Range(0f, 1f)] public float stunVolume = 0.8f;
        [Tooltip("Hauteur du coup : objet de 5 kg et de 40 kg (plus lourd = plus grave).")]
        [Min(0.1f)] public float hitPitchLight = 1.5f;
        [Min(0.1f)] public float hitPitchHeavy = 0.7f;
        [Tooltip("Distance (m) jusqu'à laquelle le son garde tout son volume, puis s'atténue jusqu'à soundMaxDistance.")]
        [Min(0.1f)] public float soundMinDistance = 5f;
        [Min(1f)] public float soundMaxDistance = 80f;
    }
}
