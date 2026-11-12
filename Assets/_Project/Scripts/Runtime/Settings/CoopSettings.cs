using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Règles de la coop (D-032). Tir ami : les balles, les objets lancés (zone comprise) et les objets repoussés d'un
    /// joueur peuvent-ils blesser les autres joueurs ? Jamais le joueur lui-même. Désactivé par défaut. Modifiable en
    /// Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Coop_Default", menuName = "ItHunts/Réglages/Coop")]
    public sealed class CoopSettings : ScriptableObject
    {
        [Tooltip("Les balles, objets lancés et objets repoussés d'un joueur blessent les autres joueurs.")]
        public bool friendlyFire;
        [Tooltip("Part des dégâts appliquée à un allié quand le tir ami est actif.")]
        [Range(0f, 2f)] public float friendlyFireMultiplier = 1f;
    }
}
