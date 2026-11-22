using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Boss final (nuit 3, point 8) : le traqueur blessable, en arène ouverte, 3 phases. Inspiré des combats de boss en arène ouverte
    /// (Elden Ring, Monster Hunter, Doom Eternal, Shadow of the Colossus pour les points faibles en hauteur ; recherche nuit 3) : chaque
    /// attaque est annoncée (600 à 1 200 ms, ×0.8 en phase 3, jamais sous 450 ms), lisible au son, à la pose et au sol ; le mouvement
    /// ouvre les dégâts (points faibles sur la nuque et les omoplates, atteints depuis les piliers par la traction, ou par derrière).
    /// </summary>
    [CreateAssetMenu(fileName = "FinalBoss_Default", menuName = "ItHunts/Réglages/Boss final")]
    public sealed class FinalBossSettings : ScriptableObject
    {
        [Header("Vie et phases")]
        [Min(1f)] public float maxHealth = 2400f;
        [Range(0f, 1f)] public float phase2At = 0.66f, phase3At = 0.33f;
        [Tooltip("Dégâts reçus par un point faible × ce facteur (balles, objets lancés, épée, lanterne, drain).")]
        [Min(0f)] public float weakPointMultiplier = 2.5f;
        [Tooltip("Un point faible cède après ces dégâts : le boss chancelle, puis le point se reforme.")]
        [Min(1f)] public float weakPointBreak = 260f;
        [Min(0f)] public float weakPointRegrow = 18f;
        [Min(0f)] public float staggerTime = 3.5f;
        [Tooltip("Dégâts d'un objet lancé sur son corps (pas un point faible) × ce facteur.")]
        [Range(0f, 1f)] public float bodyThrowShare = 0.3f;

        [Header("Déplacement")]
        public Vector3 speedByPhase = new Vector3(6.5f, 8f, 10f);
        [Min(10f)] public float turnRate = 110f;
        [Tooltip("Pause entre deux attaques (s) par phase.")]
        public Vector3 cooldownByPhase = new Vector3(2.2f, 1.6f, 1.1f);

        [Header("Annonces (s)")]
        [Min(0.3f)] public float grabTelegraph = 0.9f, sweepTelegraph = 0.85f, chargeTelegraph = 1.1f, leapTelegraph = 1.2f;
        [Min(0.3f)] public float cryTelegraph = 0.8f, debrisTelegraph = 1f, eruptionTelegraph = 1.1f, summonTelegraph = 1.2f;
        [Range(0.5f, 1f)] public float phase3TelegraphFactor = 0.8f;
        [Min(0.2f)] public float minTelegraph = 0.45f;

        [Header("Dégâts (PV du joueur)")]
        [Min(0f)] public float grabDamage = 45f, sweepDamage = 35f, chargeDamage = 40f, leapDamage = 35f, debrisDamage = 22f, eruptionDamage = 30f;

        [Header("Portées")]
        [Min(1f)] public float grabRange = 13f, sweepRange = 15f, chargeSpeed = 26f, chargeDistance = 40f, leapRadius = 9f;
        [Min(1f)] public float cryRadius = 20f, cryPush = 24f, eruptionRadius = 3.8f;
        [Range(1, 10)] public int eruptions = 5, debrisCount = 3, summonCount = 4;
        [Min(1f)] public float debrisSpeed = 24f;
    }
}
