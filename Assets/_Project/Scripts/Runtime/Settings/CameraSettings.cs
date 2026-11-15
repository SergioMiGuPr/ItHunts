using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Réglages de la vue première personne : sensibilités souris et manette, FOV selon la vitesse.
    /// Valeurs par défaut : docs/design/features/mouvement/design.md, section Caméra.
    /// </summary>
    [CreateAssetMenu(fileName = "Camera_Default", menuName = "ItHunts/Réglages/Caméra")]
    public sealed class CameraSettings : ScriptableObject
    {
        [Header("Souris")]
        [Tooltip("Degrés de rotation par point de déplacement de la souris.")]
        [Min(0f)] public float mouseSensitivity = 0.1f;
        public bool invertMouseY;

        [Header("Manette")]
        [Tooltip("Vitesse de rotation horizontale (°/s), stick à fond.")]
        [Min(0f)] public float gamepadYawSpeed = 270f;
        [Tooltip("Vitesse de rotation verticale (°/s), stick à fond.")]
        [Min(0f)] public float gamepadPitchSpeed = 180f;
        [Tooltip("Courbe de réponse du stick : 1 = linéaire, 2 = plus fin au centre.")]
        [Range(1f, 4f)] public float gamepadResponseExponent = 2f;
        public bool invertGamepadY;

        [Header("Aide à la visée (après le playtest des étapes 8 et 9)")]
        [Tooltip("Active à la manette (par défaut) ; à la souris (coupée par défaut).")]
        public bool aimAssistGamepad = true;
        public bool aimAssistMouse;
        [Tooltip("Cône autour du viseur où une cible agit (demi-angle, °) et portée (m). Cibles : goules et volants ; main libre, " +
                 "objets attrapables à portée de la télékinésie.")]
        [Range(0f, 20f)] public float aimAssistAngle = 6f;
        [Min(1f)] public float aimAssistRange = 45f;
        [Tooltip("Vitesse de visée au centre d'une cible (× la vitesse normale), revenant à 1 au bord du cône.")]
        [Range(0.1f, 1f)] public float aimAssistSlowdown = 0.5f;
        [Tooltip("Légère attraction vers la cible (°/s), seulement quand le joueur bouge la vue ou se déplace ; 0 : aucune.")]
        [Range(0f, 60f)] public float aimAssistPull = 8f;

        [Header("Vue")]
        [Tooltip("Inclinaison maximale vers le haut et vers le bas (°).")]
        [Range(10f, 89.9f)] public float pitchLimit = 89f;

        [Header("FOV selon la vitesse")]
        [Tooltip("FOV vertical de base (°). 70° ≈ 103° horizontal en 16:9.")]
        [Range(40f, 110f)] public float baseFov = 70f;
        [Tooltip("FOV ajouté à pleine vitesse (°).")]
        [Range(0f, 40f)] public float fovBonusAtMaxSpeed = 12f;
        [Tooltip("Vitesse horizontale (m/s) à partir de laquelle le FOV commence à s'élargir.")]
        [Min(0f)] public float fovSpeedMin = 8f;
        [Tooltip("Vitesse horizontale (m/s) où le bonus de FOV est complet.")]
        [Min(0f)] public float fovSpeedMax = 20f;
        [Tooltip("Rapidité d'ajustement du FOV (/s).")]
        [Min(0f)] public float fovLerpSpeed = 8f;
    }
}
