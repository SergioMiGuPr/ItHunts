using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Livre magique à lien du hibou (D-077 ; drain continu : D-088, playtest du 2026-11-21) : clic gauche maintenu sur un ennemi =
    /// lien de **drain continu**, sans durée maximale, tant que le clic est tenu et que la cible reste à portée de maintien (la ligne
    /// de vue peut se perdre un court instant) ; il retire des PV et en rend une part au hibou ; pas de recharge pendant le lien, une
    /// courte quand il casse. Sur un allié : lien de **soin**, inchangé (portée, durée maximale, plafond, recharge). Le traqueur est
    /// insensible. Plus tard : améliorations multi-cibles. En coop en ligne, le lien sera décidé par l'hôte.
    /// </summary>
    [CreateAssetMenu(fileName = "SpiritBook_Default", menuName = "ItHunts/Artefacts/Livre à lien")]
    public sealed class SpiritBookSettings : ArtifactDefinition
    {
        [Header("Lien")]
        [Tooltip("Cône de visée pour accrocher une cible (demi-angle, °).")]
        [Range(1f, 30f)] public float aimHalfAngle = 8f;
        [Tooltip("Vue : le lien de la DA part du livre et file jusqu'à la cible en ce temps (s) à chaque lancer, puis reste accroché.")]
        [Min(0f)] public float linkThrowTime = 0.12f;

        [Header("Drain (ennemi) : continu, sans durée maximale (D-088)")]
        [Tooltip("Portée de lancement (m) : un ennemi plus loin ne s'accroche pas.")]
        [Min(1f)] public float castRange = 20f;
        [Tooltip("Portée de maintien (m) : le lien casse au-delà.")]
        [Min(1f)] public float holdRange = 30f;
        [Tooltip("Ligne de vue perdue : le lien tient encore ce temps (s), puis casse.")]
        [Min(0f)] public float sightGrace = 0.5f;
        [Tooltip("PV retirés par seconde.")]
        [Min(0f)] public float drainDps = 15f;
        [Tooltip("Part des PV retirés rendue au hibou (soin).")]
        [Range(0f, 1f)] public float drainReturn = 0.2f;
        [Tooltip("Recharge quand le lien de drain casse (s) ; aucune pendant le lien.")]
        [Min(0f)] public float drainCooldown = 1f;

        [Header("Soin (allié) : inchangé")]
        [Tooltip("Portée du soin (m), au lancement comme au maintien.")]
        [Min(1f)] public float range = 15f;
        [Tooltip("Durée maximale d'un lien de soin (s).")]
        [Min(0.1f)] public float maxDuration = 4f;
        [Min(0f)] public float healDps = 8f;
        [Tooltip("Recharge après un lien de soin (s).")]
        [Min(0f)] public float healCooldown = 8f;

        [Header("Plafonds")]
        [Tooltip("PV rendus au plus par lien de soin.")]
        [Min(0f)] public float maxHealPerLink = 32f;

        [Tooltip("Calques qui coupent la ligne de vue.")]
        public LayerMask sightMask = ~0;
    }
}
