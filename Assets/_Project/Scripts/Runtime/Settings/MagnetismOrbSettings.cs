using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Réglages de l'orbe de magnétisme : attraction (traction continue, bouton maintenu) et répulsion
    /// (impulsion). Les valeurs par défaut viennent de docs/design/features/orbes/design.md (qui fait foi).
    /// Aucune charge de mouvement : l'attraction a ses propres charges, la répulsion une charge unique (D-021).
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Magnetism_Default", menuName = "ItHunts/Réglages/Orbe de magnétisme")]
    public sealed class MagnetismOrbSettings : ScriptableObject
    {
        [Header("Visée")]
        [Tooltip("Calques touchés par le rayon. Toute surface touchée répond (murs, sol, plafond).")]
        public LayerMask aimMask = ~0;
        [Tooltip("Un appui pendant la recharge est mémorisé ce temps (s), puis part dès que le mode est prêt.")]
        [Min(0f)] public float pressBuffer = 0.1f;

        [Header("Attraction : traction continue")]
        [Tooltip("Portée du rayon d'attraction (m), depuis les yeux.")]
        [Min(1f)] public float attractRange = 60f;
        [Tooltip("Accélération de la traction (m/s²) jusqu'à la vitesse de traction.")]
        [Min(0f)] public float tractionAcceleration = 80f;
        [Tooltip("Vitesse de traction (m/s). Plus vite, on garde sa vitesse, redirigée vers le point (jamais de perte, " +
                 "pas de plafond des gains).")]
        [Min(0f)] public float tractionSpeed = 30f;
        [Tooltip("Durée maximale d'une traction (s).")]
        [Min(0.05f)] public float tractionMaxDuration = 2.5f;
        [Tooltip("La traction s'arrête à cette distance du point visé (m, depuis les yeux).")]
        [Min(0f)] public float tractionArrivalDistance = 1f;
        [Tooltip("Charges d'attraction (séparées des charges de mouvement) : 1 traction = 1 charge.")]
        [Min(1)] public int attractCharges = 3;
        [Tooltip("Temps de recharge d'une charge d'attraction (s) ; elles reviennent une par une.")]
        [Min(0.05f)] public float attractChargeRechargeTime = 4f;

        [Header("Répulsion : impulsion")]
        [Tooltip("Portée du rayon de répulsion (m).")]
        [Min(1f)] public float repelRange = 40f;
        [Tooltip("Impulsion à l'opposé du point visé (m/s). La vitesse horizontale ne baisse jamais ; son gain suit le " +
                 "plafond commun des gains (MovementSettings.movementGainCap).")]
        [Min(0f)] public float repelImpulse = 14f;
        [Tooltip("Recharge de la répulsion (s) : une seule charge, qui ne s'accumule pas ; elle revient ce temps après " +
                 "chaque usage.")]
        [Min(0f)] public float repelCooldown = 5f;
        [Tooltip("Durée de l'état « En usage » après une répulsion (s) : retour visuel.")]
        [Min(0f)] public float repelUseDuration = 0.15f;
        [Tooltip("Au sol, pas de friction pendant ce temps après une répulsion (s).")]
        [Min(0f)] public float groundFrictionGrace = 0.2f;
    }
}
