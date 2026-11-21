using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Sensations de jeu (playtest du 2026-11-21, points 4 et 9) : mouvements de caméra, secousses, FOV, arrêts sur image, traînées
    /// de vitesse, éclats d'impact, flou de vitesse. Affichage seulement : la simulation (visée, tirs, mouvement) n'est jamais
    /// décalée. Tout est multiplié par le curseur « Intensité des effets » des Paramètres ; « Réduire les secousses » réduit en plus
    /// les secousses et les mouvements de caméra. Un asset : GameFeel_Default.
    /// </summary>
    [CreateAssetMenu(fileName = "GameFeel_Default", menuName = "ItHunts/Réglages/Sensations de jeu")]
    public sealed class GameFeelSettings : ScriptableObject
    {
        [Header("Réglages du joueur")]
        [Tooltip("« Réduire les secousses » : part gardée des secousses et des mouvements de caméra (0 : coupés).")]
        [Range(0f, 1f)] public float reducedShakeScale = 0.3f;

        [Header("Hissage (point 4, en attendant les mains)")]
        [Tooltip("Mouvement de caméra pendant le hissage : léger plongeon puis remontée, petite inclinaison.")]
        public bool mantleCamera = true;
        [Tooltip("Durée du mouvement (s), depuis le départ du hissage (le hissage dure 0.3 s : la remontée finit un peu après).")]
        [Min(0.05f)] public float mantleCameraTime = 0.45f;
        [Tooltip("Part de la durée passée à plonger (le reste : remontée).")]
        [Range(0.1f, 0.9f)] public float mantleDipShare = 0.4f;
        [Tooltip("Profondeur du plongeon (m) et dépassement de la remontée (m).")]
        [Min(0f)] public float mantleDipDepth = 0.14f;
        [Min(0f)] public float mantleRiseOvershoot = 0.04f;
        [Tooltip("Tangage vers le bas au plongeon (°) et roulis (°, côté tiré au hasard à chaque hissage).")]
        [Range(0f, 15f)] public float mantlePitch = 4f;
        [Range(0f, 15f)] public float mantleRoll = 3f;
    }
}
