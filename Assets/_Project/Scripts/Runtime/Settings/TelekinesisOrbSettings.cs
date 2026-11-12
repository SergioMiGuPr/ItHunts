using UnityEngine;
using UnityEngine.Serialization;

namespace ItHunts.Settings
{
    /// <summary>
    /// Réglages de la télékinésie de l'orbe unique (D-024) : attraper (F / X), tenir, lancer (clic gauche / RT), poser
    /// (F de nouveau). Pas d'endurance : seul un court délai après un lancer. Valeurs de départ :
    /// docs/design/features/orbes/design.md (qui fait foi).
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Telekinesis_Default", menuName = "ItHunts/Réglages/Orbe de télékinésie")]
    public sealed class TelekinesisOrbSettings : ScriptableObject
    {
        [Header("Visée")]
        [Tooltip("Portée pour attraper (m), depuis les yeux ; à peu près celle de l'attraction du magnétisme.")]
        [Min(0.5f)] public float range = 60f;
        [Tooltip("Masse maximale attrapable (kg). Au-delà, l'objet brille en rouge terne et l'usage est refusé.")]
        [Min(0f)] public float maxMass = 40f;
        [Tooltip("Calques touchés par le rayon de visée et le rayon qui oriente le lancer.")]
        public LayerMask aimMask = ~0;
        [Header("Aide à la visée (attraper)")]
        [Tooltip("Sélectionne l'objet attrapable vu dans un cône autour du viseur au lieu d'un rayon exact. Jamais à " +
                 "travers un mur ; le plus proche du viseur l'emporte.")]
        public bool aimAssist = true;
        [Tooltip("Demi-angle du cône de près (degrés), jusqu'à assistNearDistance.")]
        [Range(0f, 30f)] public float assistNearAngle = 8f;
        [Tooltip("Distance (m) jusqu'à laquelle le cône garde son angle de près ; il se resserre ensuite jusqu'à la portée.")]
        [Min(0f)] public float assistNearDistance = 5f;
        [Tooltip("Demi-angle du cône à la portée (degrés).")]
        [Range(0f, 30f)] public float assistFarAngle = 3f;

        [Tooltip("Un appui pendant la recharge est mémorisé ce temps (s).")]
        [Min(0f)] public float pressBuffer = 0.1f;

        [Header("Ennemis étourdis (D-030)")]
        [Tooltip("Un ennemi tenu se libère seul au bout de ce temps (s).")]
        [Min(0.1f)] public float enemyHoldDuration = 3f;
        [Tooltip("Il se débat et clignote pendant ce temps avant de se libérer (s).")]
        [Min(0f)] public float enemyStruggleWarning = 0.5f;
        [Tooltip("Amplitude du « débattement » de l'ennemi tenu (m).")]
        [Min(0f)] public float enemyStruggleShake = 0.06f;

        [Header("Rythme")]
        [Tooltip("Après un lancer, délai avant de pouvoir attraper de nouveau (s).")]
        [Min(0f)] public float cooldown = 0.3f;

        [Header("Tenir")]
        [Tooltip("Distance du point de maintien devant les yeux (m).")]
        [Min(0.5f)] public float holdDistance = 2.5f;
        [Tooltip("Le point de maintien est un peu sous la ligne de visée (m), pour garder le viseur dégagé.")]
        public float holdDrop = 0.35f;
        [Tooltip("Rapidité avec laquelle l'objet rejoint le point de maintien (/s).")]
        [Min(0f)] public float holdGain = 18f;
        [Tooltip("Vitesse d'approche : un objet attrapé loin vole vers le point de maintien à cette vitesse au plus, en " +
                 "plus de la vitesse du joueur (m/s) ; il ralentit en arrivant (holdGain). 40 m/s : 60 m en ≈ 1.5 s.")]
        [FormerlySerializedAs("holdMaxCorrection")]
        [Min(0f)] public float approachSpeed = 40f;
        [Tooltip("Si l'objet reste coincé plus loin que cette distance du point de maintien (m)…")]
        [Min(0.5f)] public float breakDistance = 4f;
        [Tooltip("… pendant ce temps (s), il est lâché.")]
        [Min(0f)] public float breakTime = 0.3f;
        [Tooltip("Poser (F de nouveau) : vitesse maximale de l'objet relâché (m/s).")]
        [Min(0f)] public float placeMaxSpeed = 4f;

        [Header("Lancer")]
        [Tooltip("Vitesse du lancer (m/s), dans la direction visée.")]
        [Min(0f)] public float throwSpeed = 30f;
        [Tooltip("Tir tendu (D-025) : pas de gravité pendant ce temps après un lancer (s), ou jusqu'au premier choc ; " +
                 "l'objet va où vise le viseur. 0.5 s : ≈ 15 m à l'arrêt, 20 m en course.")]
        [Min(0f)] public float throwNoGravityTime = 0.5f;
        [Tooltip("Ajoute la part de la vitesse du joueur dans la direction du lancer (si elle est positive) : lancer en " +
                 "pleine course frappe plus fort. Le reste est ignoré : la direction reste celle du viseur.")]
        public bool inheritPlayerVelocity = true;
        [Tooltip("Plafond de ce bonus (m/s) : 20 par défaut, à rééquilibrer après des runs complètes.")]
        [Min(0f)] public float throwSpeedBonusCap = 20f;
        [Tooltip("Pendant ce temps après un lancer ou un lâcher (s), l'objet ne touche pas son lanceur.")]
        [Min(0f)] public float ignoreHolderAfterRelease = 0.4f;
    }
}
