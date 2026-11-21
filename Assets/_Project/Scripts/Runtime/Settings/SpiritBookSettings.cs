using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Livre magique à lien du hibou (point 5 de la nuit du 2026-11-21, D-077) : clic gauche maintenu sur un ennemi à portée et en
    /// vue = lien de drain (lui retire des PV, en rend une partie au hibou) ; sur un allié = lien de soin (sans coût). Le lien casse
    /// hors de portée, sans ligne de vue, au relâchement ou au bout de la durée maximale, puis recharge. Le traqueur est
    /// insensible (aucun lien). En coop en ligne, le lien sera décidé par l'hôte.
    /// </summary>
    [CreateAssetMenu(fileName = "SpiritBook_Default", menuName = "ItHunts/Artefacts/Livre à lien")]
    public sealed class SpiritBookSettings : ArtifactDefinition
    {
        [Header("Lien")]
        [Tooltip("Portée du lien (m).")]
        [Min(1f)] public float range = 15f;
        [Tooltip("Cône de visée pour accrocher une cible (demi-angle, °).")]
        [Range(1f, 30f)] public float aimHalfAngle = 8f;
        [Tooltip("Durée maximale d'un lien (s).")]
        [Min(0.1f)] public float maxDuration = 4f;

        [Header("Drain (ennemi)")]
        [Min(0f)] public float drainDps = 6f;
        [Tooltip("Part des PV retirés rendue au hibou.")]
        [Range(0f, 1f)] public float drainReturn = 0.5f;
        [Tooltip("Recharge après un lien de drain (s).")]
        [Min(0f)] public float drainCooldown = 6f;

        [Header("Soin (allié)")]
        [Min(0f)] public float healDps = 8f;
        [Tooltip("Recharge après un lien de soin (s).")]
        [Min(0f)] public float healCooldown = 8f;

        [Header("Plafonds")]
        [Tooltip("PV retirés au plus par lien (plafond de sécurité).")]
        [Min(0f)] public float maxDrainPerLink = 30f;
        [Tooltip("PV rendus au plus par lien de soin.")]
        [Min(0f)] public float maxHealPerLink = 32f;

        [Tooltip("Calques qui coupent la ligne de vue.")]
        public LayerMask sightMask = ~0;
    }
}
