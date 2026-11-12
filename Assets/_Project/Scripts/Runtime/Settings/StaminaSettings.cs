using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Endurance de la télékinésie (D-015 : la télékinésie a sa propre jauge, le combat ne prive jamais de mobilité).
    /// Valeurs de départ : docs/design/features/orbes/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Stamina_Default", menuName = "ItHunts/Réglages/Endurance")]
    public sealed class StaminaSettings : ScriptableObject
    {
        [Tooltip("Endurance maximale (points).")]
        [Min(1f)] public float max = 100f;
        [Tooltip("Régénération (points par seconde).")]
        [Min(0f)] public float regenPerSecond = 25f;
        [Tooltip("Délai sans dépense avant que la régénération reprenne (s).")]
        [Min(0f)] public float regenDelay = 0.8f;
    }
}
