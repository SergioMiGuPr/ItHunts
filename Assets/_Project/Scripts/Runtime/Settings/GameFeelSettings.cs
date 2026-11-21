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

        [Header("Secousses (0 à 1 ; multipliées par l'intensité et la réduction)")]
        [Tooltip("Atterrissage fort : secousse nulle à cette vitesse de chute (m/s), pleine à la suivante.")]
        [Min(0f)] public float hardLandingSpeedMin = 13f;
        [Min(0f)] public float hardLandingSpeedFull = 26f;
        [Range(0f, 1f)] public float hardLandingTrauma = 0.35f;
        [Range(0f, 1f)] public float repelTrauma = 0.22f;
        [Tooltip("Coup reçu : secousse pour un coup de hurtTraumaDamage PV (proportionnelle, plafonnée à 1).")]
        [Range(0f, 1f)] public float hurtTrauma = 0.45f;
        [Min(1f)] public float hurtTraumaDamage = 25f;
        [Range(0f, 1f)] public float parryTrauma = 0.25f;
        [Tooltip("Cri du traqueur (prise, passage en Enragé) : secousse pleine à bout portant, nulle à stalkerCryRange (m).")]
        [Range(0f, 1f)] public float stalkerCryTrauma = 0.4f;
        [Min(1f)] public float stalkerCryRange = 30f;

        [Header("FOV (le FOV selon la vitesse est dans Camera_Default)")]
        [Tooltip("Ouverture du FOV au départ d'un dash (°), qui retombe à fovPunchDecay (°/s).")]
        [Range(0f, 20f)] public float dashFovPunch = 6f;
        [Min(0.1f)] public float fovPunchDecay = 18f;

        [Header("Arrêts sur image (s, temps réel ; écran partagé : les deux vues s'arrêtent)")]
        [Min(0f)] public float throwHitStop = 0.045f;
        [Tooltip("Dégâts minimaux d'un objet lancé pour un arrêt sur image (les petits chocs n'arrêtent rien).")]
        [Min(0f)] public float throwHitStopMinDamage = 8f;
        [Min(0f)] public float swordHitStop = 0.05f;
        [Min(0f)] public float parryHitStop = 0.08f;
        [Tooltip("Durée maximale d'un arrêt (s), quoi qu'on demande.")]
        [Min(0f)] public float maxHitStop = 0.12f;

        [Header("Atterrissage : plongeon de caméra")]
        [Tooltip("Plongeon nul à cette vitesse de chute (m/s), plein à landDipSpeedFull.")]
        [Min(0f)] public float landDipSpeedMin = 5f;
        [Min(0f)] public float landDipSpeedFull = 20f;
        [Min(0f)] public float landDipDepth = 0.14f;
        [Range(0f, 10f)] public float landDipPitch = 1.5f;
        [Min(0.05f)] public float landDipTime = 0.28f;

        [Header("Traînées de vitesse")]
        [Tooltip("Traits qui filent autour de la vue au-delà de trailSpeedMin (m/s), pleins à trailSpeedFull.")]
        public bool speedTrails = true;
        [Min(0f)] public float trailSpeedMin = 14f;
        [Min(0f)] public float trailSpeedFull = 22f;
        [Tooltip("Traits par seconde à pleine vitesse ; matériau de la DA (étincelle étirée, shader ItHunts/VFX/Peint : l'alpha dissout " +
                 "la forme, il reste à 1 sauf aux bouts) ; ton du cœur des traits (bloc de propriétés, le matériau n'est pas modifié).")]
        [Min(0f)] public float trailRate = 70f;
        public Material trailMaterial;
        public Color trailColor = Color.white;
        [ColorUsage(false, true)] public Color trailTone = new Color(0.8f, 0.9f, 1.2f, 1f);
        [Tooltip("Épaisseur des traits (m).")]
        [Min(0.005f)] public float trailWidth = 0.035f;

        [Header("Éclats d'impact")]
        [Tooltip("Étincelles de la DA (VFX_Etincelles) aux impacts d'objets lancés et de l'épée, à partir de ces dégâts.")]
        public bool impactSparks = true;
        [Min(0f)] public float sparksMinDamage = 4f;

        [Header("Flou de vitesse (URP Motion Blur, caméra seule, qualité basse ; solo seulement)")]
        public bool speedBlur = true;
        [Min(0f)] public float blurSpeedMin = 14f;
        [Min(0f)] public float blurSpeedFull = 24f;
        [Range(0f, 1f)] public float blurMaxIntensity = 0.2f;
    }
}
