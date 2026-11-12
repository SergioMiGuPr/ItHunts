using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Retour visuel de la télékinésie (greybox) : couleurs par mode (violet : attraper et tenir, vert : lancer),
    /// lueur des objets attrapables, anneaux du viseur, barre d'endurance. Couleurs HDR au-dessus de 1 : bloom.
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "TelekinesisFeedback_Default", menuName = "ItHunts/Réglages/Retour de la télékinésie")]
    public sealed class TelekinesisFeedbackSettings : ScriptableObject
    {
        [Header("Couleurs")]
        public Color grabColor = new Color(0.6f, 0.25f, 1f);
        public Color throwColor = new Color(0.25f, 1f, 0.35f);
        public Color tooHeavyColor = new Color(0.8f, 0.1f, 0.1f);
        public Color staminaColor = new Color(0.65f, 0.35f, 1f, 0.95f);

        [Header("Lueur des objets attrapables (intensité d'émission)")]
        [Tooltip("Objet attrapable hors de portée (repérable de loin).")]
        [Min(0f)] public float glowIdle = 0.08f;
        [Tooltip("Objet attrapable à portée.")]
        [Min(0f)] public float glowInRange = 0.35f;
        [Tooltip("Objet visé et attrapable.")]
        [Min(0f)] public float glowAimed = 1.6f;
        [Tooltip("Objet tenu.")]
        [Min(0f)] public float glowHeld = 1.2f;
        [Tooltip("Objet visé mais trop lourd (rouge terne).")]
        [Min(0f)] public float glowTooHeavy = 0.5f;
        [Tooltip("Objet lancé : lueur verte qui s'éteint en ce temps (s).")]
        [Min(0f)] public float thrownGlowDuration = 1f;
        [Min(0f)] public float glowThrown = 2f;

        [Header("Orbe et trait")]
        [Min(0f)] public float orbUsageIntensity = 2.5f;
        [Min(0f)] public float orbReadyIntensity = 0.8f;
        [Min(0f)] public float lineIntensity = 1.5f;

        [Header("Viseur : anneaux (pixels)")]
        [Tooltip("Anneau violet : un objet visé peut être attrapé.")]
        [Min(1f)] public float grabRingRadius = 8f;
        [Tooltip("Anneau vert : l'objet tenu peut être lancé.")]
        [Min(1f)] public float throwRingRadius = 13f;
    }
}
