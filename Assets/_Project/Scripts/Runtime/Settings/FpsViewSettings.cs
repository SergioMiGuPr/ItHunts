using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Vue première personne (greybox, étape 5) : mains et orbe dessinées par une caméra superposée à champ de vision
    /// fixe (elles ne traversent jamais les murs et ne s'étirent pas avec le FOV de vitesse), mouvement des mains et
    /// gestes. Les distances sont en mètres dans le repère de la caméra (x droite, y haut, z avant). Valeurs de départ :
    /// docs/design/features/vue-fps/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "FpsView_Default", menuName = "ItHunts/Réglages/Vue FPS")]
    public sealed class FpsViewSettings : ScriptableObject
    {
        [Header("Caméra des mains")]
        [Tooltip("Champ de vision fixe de la caméra des mains (degrés), indépendant du FOV de vitesse.")]
        [Range(30f, 100f)] public float viewmodelFov = 60f;
        [Min(0.001f)] public float viewmodelNearClip = 0.01f;

        [Header("Position de repos (repère caméra)")]
        public Vector3 rightHandRest = new Vector3(0.21f, -0.22f, 0.47f);
        public Vector3 rightHandRestEuler = new Vector3(-15f, -10f, 0f);
        public Vector3 leftHandRest = new Vector3(-0.23f, -0.25f, 0.45f);
        public Vector3 leftHandRestEuler = new Vector3(-10f, 12f, 0f);

        [Header("Barillet (main gauche, modèle SM_Barillet)")]
        [Tooltip("Centre du barillet dans le repère de la main gauche (m) ; son axe de tir (+Z) suit les doigts.")]
        public Vector3 barrelInHand = new Vector3(0f, 0.065f, 0.05f);
        [Tooltip("Rotation du barillet dans la main gauche (degrés).")]
        public Vector3 barrelInHandEuler = Vector3.zero;
        [Tooltip("Échelle du barillet (1 = taille modélisée, 0.10 m de diamètre).")]
        [Min(0.1f)] public float barrelScale = 1f;

        [Header("Orbe (dans la paume droite)")]
        [Tooltip("Diamètre de l'orbe (m).")]
        [Min(0.01f)] public float orbDiameter = 0.08f;
        [Tooltip("Centre de l'orbe dans le repère de la main droite (au-dessus de la paume).")]
        public Vector3 orbInHand = new Vector3(0f, 0.06f, 0.045f);

        [Header("Balancement (rotation de la vue) — suit l'intensité du mouvement des mains")]
        [Tooltip("Décalage par degré de rotation de la vue sur une image (m/°).")]
        [Min(0f)] public float swayPerDegree = 0.002f;
        [Tooltip("Décalage et inclinaison maximaux.")]
        [Min(0f)] public float swayMaxOffset = 0.04f;
        [Min(0f)] public float swayMaxAngle = 4f;
        [Tooltip("Retour à la position de repos (/s).")]
        [Min(0f)] public float swayReturn = 10f;

        [Header("Balancier de course — suit l'intensité")]
        [Min(0f)] public float bobAmplitude = 0.015f;
        [Tooltip("Pas par seconde à la vitesse de course de référence ; proportionnel à la vitesse au sol, rien en l'air.")]
        [Min(0f)] public float bobStepsPerSecond = 1.8f;
        [Min(0.1f)] public float bobReferenceSpeed = 7.5f;
        [Tooltip("Au-delà de ce multiple de la vitesse de référence, le balancier n'augmente plus.")]
        [Min(1f)] public float bobMaxSpeedFactor = 1.5f;

        [Header("Plongée à l'atterrissage — suit l'intensité")]
        [Tooltip("Plongée (m) pour 10 m/s de vitesse de chute à l'atterrissage.")]
        [Min(0f)] public float landingDipPer10 = 0.04f;
        [Min(0f)] public float landingDipMax = 0.08f;
        [Tooltip("Retour (/s).")]
        [Min(0f)] public float landingReturn = 8f;

        [Header("États (ne suivent pas l'intensité)")]
        [Tooltip("Glissade : mains plus basses (m) et inclinées (°).")]
        public float slideDrop = 0.06f;
        public float slideRoll = 6f;
        [Tooltip("Recul des mains avec la vitesse (m) à 20 m/s, dans le sens inverse du déplacement.")]
        [Min(0f)] public float speedLagAt20 = 0.03f;
        [Tooltip("Lissage des poses (/s).")]
        [Min(0f)] public float poseSmoothing = 18f;

        [Header("Gestes de la main droite (ne suivent pas l'intensité)")]
        [Min(0.02f)] public float repelGestureDuration = 0.15f;
        [Min(0.02f)] public float throwGestureDuration = 0.2f;
        [Min(0.02f)] public float grabGestureDuration = 0.15f;
        [Tooltip("Traction : bras tendu vers l'avant (m) et relevé.")]
        public Vector3 tractionOffset = new Vector3(-0.04f, 0.06f, 0.12f);
        [Tooltip("Objet tenu : main relevée.")]
        public Vector3 holdOffset = new Vector3(-0.02f, 0.05f, 0.05f);
        [Tooltip("Répulsion : poussée vers l'avant (m) au milieu du geste.")]
        [Min(0f)] public float repelPunch = 0.15f;
        [Tooltip("Lancer : recul puis poussée (m).")]
        [Min(0f)] public float throwWindup = 0.05f;
        [Min(0f)] public float throwPush = 0.18f;
        [Tooltip("Prise : main qui se tend (m).")]
        [Min(0f)] public float grabReach = 0.1f;
    }
}
