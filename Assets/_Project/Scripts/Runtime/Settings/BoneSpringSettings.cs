using System;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>Réglages d'une chaîne d'os à ressort (prévus pour Stalker_Default, traqueur v4 : pas encore activés).</summary>
    [Serializable]
    public struct BoneSpringSettings
    {
        [Tooltip("Retour vers la pose animée (0 : libre, 1 : rigide), par seconde.")]
        [Range(0f, 1f)] public float stiffness;
        [Tooltip("Amortissement de la vitesse (0 : aucun, 1 : figé).")]
        [Range(0f, 1f)] public float damping;
        [Tooltip("Gravité appliquée aux os (m/s²).")]
        public Vector3 gravity;
        [Tooltip("Part du mouvement de la racine transmise aux os (0 : ils traînent derrière, 1 : ils suivent).")]
        [Range(0f, 1f)] public float inertia;
        [Tooltip("Rayon de collision de chaque os (m).")]
        [Min(0f)] public float radius;

        public static BoneSpringSettings Default => new BoneSpringSettings
        {
            stiffness = 0.12f, damping = 0.08f, gravity = new Vector3(0f, -4f, 0f), inertia = 0.4f, radius = 0.05f,
        };
    }
}
