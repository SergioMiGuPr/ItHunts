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
        [Tooltip("Plafond de vitesse de la traction (m/s, D-036) : elle accélère jusqu'à cette vitesse. Déjà plus vite (bunny " +
                 "hop, dash, pente), elle garde la vitesse sans rien ajouter et la redirige vers le point visé : elle ne freine jamais.")]
        [Min(0f)] public float tractionSpeed = 18f;
        [Tooltip("Durée maximale d'une traction (s).")]
        [Min(0.05f)] public float tractionMaxDuration = 2.5f;
        [Tooltip("La traction s'arrête à cette distance du point visé (m, depuis les yeux).")]
        [Min(0f)] public float tractionArrivalDistance = 1f;
        [Tooltip("Charges d'attraction (séparées des charges de mouvement) : 1 traction = 1 charge.")]
        [Min(1)] public int attractCharges = 2;
        [Tooltip("Temps de recharge d'une charge d'attraction (s, 6 depuis D-036) ; elles reviennent une par une.")]
        [Min(0.05f)] public float attractChargeRechargeTime = 6f;

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
        [Header("Répulsion : onde en cône depuis le joueur (D-029)")]
        [Tooltip("Portée de l'onde (m), depuis les yeux, dans la direction visée : elle repousse les objets déplaçables et " +
                 "les ennemis ; le joueur n'est propulsé qu'à l'appui (inchangé).")]
        [Min(0f)] public float waveRange = 14f;
        [Tooltip("Ouverture totale du cône (degrés) : 60° = 30° de chaque côté de la visée.")]
        [Range(1f, 179f)] public float waveConeAngle = 60f;
        [Tooltip("Au bord du cône (angle), part de la vitesse sur l'axe (1 : la même).")]
        [Range(0f, 1f)] public float waveAngleEdgeShare = 1f;
        [Tooltip("Onde de départ (appui) : vitesse donnée à un objet de masse de référence jusqu'à waveFalloffStart (m/s) ; " +
                 "plus faible pour un objet lourd.")]
        [Min(0f)] public float waveObjectSpeed = 22f;
        [Min(0.1f)] public float waveObjectReferenceMass = 15f;
        [Tooltip("Onde de départ : vitesse donnée à un ennemi de masse de référence (m/s).")]
        [Min(0f)] public float waveEnemySpeed = 22f;
        [Min(0.1f)] public float waveEnemyReferenceMass = 80f;
        [Tooltip("Facteur de masse (référence / masse) borné entre ces valeurs : un objet léger part au plus 1.3 fois plus " +
                 "vite, un objet très lourd au moins 0.35 fois.")]
        [Min(0f)] public float waveMassScaleMin = 0.35f;
        [Min(0f)] public float waveMassScaleMax = 1.3f;
        [Tooltip("Distance (m) jusqu'à laquelle l'onde garde toute sa force ; elle décroît ensuite jusqu'au bout de la portée.")]
        [Min(0f)] public float waveFalloffStart = 2f;
        [Tooltip("Onde de départ : au bout de la portée, part de la vitesse donnée de près.")]
        [Range(0f, 1f)] public float waveEdgeShare = 0.6f;

        [Header("Répulsion maintenue : poussée continue (D-029)")]
        [Tooltip("Bouton maintenu : la répulsion continue au plus ce temps (s) et pousse tout ce qui est dans le cône ; " +
                 "elle s'arrête au relâchement, à cette durée ou à une autre action de l'orbe. La recharge part de la fin.")]
        [Min(0f)] public float repelHoldMaxDuration = 0.8f;
        [Tooltip("Poussée continue dans l'axe du cône (m/s²), pour la masse de référence, jusqu'à waveFalloffStart.")]
        [Min(0f)] public float repelHoldAcceleration = 35f;
        [Tooltip("Poussée continue : au bout de la portée, part de la poussée de près.")]
        [Range(0f, 1f)] public float repelHoldEdgeShare = 0.5f;
        [Tooltip("La poussée continue n'ajoute plus rien au-delà de cette vitesse dans l'axe du cône (m/s).")]
        [Min(0f)] public float repelHoldMaxSpeed = 25f;
        [Tooltip("Part de montée ajoutée à la direction de poussée (les objets décollent un peu).")]
        [Range(0f, 1f)] public float waveLift = 0.25f;
        [Tooltip("Au sol, pas de friction pendant ce temps après une répulsion (s).")]
        [Min(0f)] public float groundFrictionGrace = 0.2f;
    }
}
