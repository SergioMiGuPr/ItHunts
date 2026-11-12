using UnityEngine;
using UnityEngine.Serialization;

namespace ItHunts.Settings
{
    /// <summary>
    /// Réglages de l'orbe de télékinésie : attraper (un clic), tenir, lancer (clic droit), poser (nouveau clic gauche).
    /// L'endurance est dans StaminaSettings. Valeurs de départ : docs/design/features/orbes/design.md (qui fait foi).
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
        [Tooltip("Un appui pendant la recharge est mémorisé ce temps (s).")]
        [Min(0f)] public float pressBuffer = 0.1f;

        [Header("Coûts (points d'endurance) et rythme")]
        [Min(0f)] public float grabCost = 10f;
        [Min(0f)] public float throwCost = 15f;
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
        [Tooltip("Poser (nouveau clic gauche) : vitesse maximale de l'objet relâché (m/s).")]
        [Min(0f)] public float placeMaxSpeed = 4f;

        [Header("Lancer")]
        [Tooltip("Vitesse du lancer (m/s), dans la direction visée.")]
        [Min(0f)] public float throwSpeed = 30f;
        [Tooltip("Ajoute la vitesse du joueur au lancer : lancer en pleine course frappe plus fort.")]
        public bool inheritPlayerVelocity = true;
        [Tooltip("Pendant ce temps après un lancer ou un lâcher (s), l'objet ne touche pas son lanceur.")]
        [Min(0f)] public float ignoreHolderAfterRelease = 0.4f;
    }
}
