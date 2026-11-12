using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Points de vie d'un joueur (provisoire, D-032) : servent pour l'instant au tir ami. La mort et la relance de la
    /// run viendront à l'étape 8 ; d'ici là, un joueur à 0 PV réapparaît à son point de départ avec tous ses PV.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerHealth_Default", menuName = "ItHunts/Réglages/PV du joueur")]
    public sealed class PlayerHealthSettings : ScriptableObject
    {
        [Min(1f)] public float maxHealth = 100f;
        [Tooltip("Délai avant de réapparaître au point de départ (s), provisoire (étape 8 : mort et relance).")]
        [Min(0f)] public float respawnDelay = 1f;
    }
}
