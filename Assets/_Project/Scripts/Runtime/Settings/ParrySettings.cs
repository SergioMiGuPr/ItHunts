using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Parade (D-075) : appuyer sur « Parer » (E / LB) juste avant l'impact d'une attaque de goule annule ses dégâts et
    /// l'étourdit. Ne ralentit jamais le joueur. Sans effet contre le traqueur (sa prise passe). Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Parry_Default", menuName = "ItHunts/Réglages/Parade")]
    public sealed class ParrySettings : ScriptableObject
    {
        [Tooltip("Fenêtre (s) : un impact au plus ce temps après l'appui est paré.")]
        [Min(0.01f)] public float window = 0.18f;
        [Tooltip("Étourdissement de la goule parée (s).")]
        [Min(0f)] public float stunDuration = 1.5f;
        [Tooltip("Temps de recharge (s) après un appui, réussi ou non (comme la répulsion).")]
        [Min(0f)] public float cooldown = 4f;
        [Tooltip("Une parade réussie rend la recharge plus courte (part gardée : 1 = inchangée).")]
        [Range(0f, 1f)] public float cooldownAfterSuccess = 1f;

        [Header("Retour (provisoire en attendant les effets d'art)")]
        [ColorUsage(false, true)] public Color flashColor = new Color(3f, 2.6f, 1.4f, 1f);
        [Tooltip("Taille de l'éclat, posé sur la goule (m).")]
        [Min(0f)] public float flashSize = 0.7f;
        [Range(0f, 1f)] public float volume = 0.8f;
        [Tooltip("Couleur de l'icône de recharge provisoire du HUD.")]
        public Color hudColor = new Color(1f, 0.86f, 0.45f, 1f);
    }
}
