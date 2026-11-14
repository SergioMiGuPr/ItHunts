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

        [Header("Écran partagé (étape 9)")]
        [Tooltip("Faux : écran coupé gauche / droite ; vrai : haut / bas.")]
        public bool splitHorizontal;
        [Tooltip("Joueurs locaux au plus (écran partagé).")]
        [Range(1, 2)] public int maxLocalPlayers = 2;
        [Tooltip("Écran partagé gauche / droite : FOV vertical multiplié par ce facteur (moitié étroite : un peu plus large pour garder de la vue sur les côtés).")]
        [Range(0.6f, 1.4f)] public float splitSideFovScale = 1.1f;
        [Tooltip("Écran partagé haut / bas : FOV vertical multiplié par ce facteur (moitié très large et basse : resserré pour éviter l'effet fish-eye).")]
        [Range(0.6f, 1.4f)] public float splitStackedFovScale = 0.75f;

        [Header("À terre et réanimation (D-007, étape 9)")]
        [Tooltip("À terre pendant ce temps sans être relevé : le joueur devient spectateur (s).")]
        [Min(1f)] public float downedSeconds = 30f;
        [Tooltip("Un allié le relève en maintenant E / Y pendant ce temps (s)…")]
        [Min(0.1f)] public float reviveHoldSeconds = 3f;
        [Tooltip("… à cette distance au plus (m).")]
        [Min(0.5f)] public float reviveRange = 2f;
        [Tooltip("PV au retour.")]
        [Min(1f)] public float reviveHealth = 50f;
    }
}
