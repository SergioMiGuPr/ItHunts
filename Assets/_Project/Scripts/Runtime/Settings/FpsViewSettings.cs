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
        public Vector3 rightHandRest = new Vector3(0.21f, -0.09f, 0.34f);          // cadrage de la référence (docs/design/references-visuelles/reference_bras_fps.png)
        public Vector3 rightHandRestEuler = new Vector3(-45.1f, -21f, 39.7f);    // avant-bras par les coins bas, paume ouverte vers le haut et vers le centre
        public Vector3 leftHandRest = new Vector3(-0.21f, -0.09f, 0.34f);
        public Vector3 leftHandRestEuler = new Vector3(-45.1f, 21f, -39.7f);

        [Header("Préréglage « écarté » du cadrage des mains (menu pause, vitrine) ; « référence » = la position de repos ci-dessus")]
        [Tooltip("Main droite (gauche en miroir) : plus écartée et plus basse, moins de place à l'écran que le cadrage de la référence.")]
        public Vector3 wideRightHandRest = new Vector3(0.29f, -0.15f, 0.42f);         // recalé pour les bras peints (PR #7) : 12 à 14 % de l'écran
        public Vector3 wideRightHandRestEuler = new Vector3(-45.1f, -21f, 39.7f);

        [Header("Lanterne de feu (main gauche, greybox)")]
        [Tooltip("Centre de la lanterne dans le repère de la main gauche (m), posée dans la paume comme le barillet ; le jet sort du haut.")]
        public Vector3 lanternInHand = new Vector3(0f, 0.16f, 0.05f);
        [Tooltip("Rotation de la lanterne dans la main gauche (degrés).")]
        public Vector3 lanternInHandEuler = Vector3.zero;
        [Tooltip("Échelle de la lanterne (1 = greybox de 0.11 m de haut).")]
        [Min(0.1f)] public float lanternScale = 1f;

        [Header("Poses des bras v2 (PR #11) pour la main droite ; absentes des bras : la pose d'avant")]
        [Tooltip("Répulsion (geste et maintenue) : paume ouverte qui repousse.")]
        public string repelPose = "Pose_MainOuverte";
        [Tooltip("Traction : la main agrippe le point.")]
        public string tractionPose = "Pose_Griffe";

        [Header("Orbe (dans la paume droite)")]
        [Tooltip("Diamètre de l'orbe (m).")]
        [Min(0.01f)] public float orbDiameter = 0.08f;
        [Tooltip("Centre de l'orbe dans le repère de la main droite (au-dessus de la paume).")]
        public Vector3 orbInHand = new Vector3(0f, 0.14f, 0.045f);

        [Header("Flottement de l'orbe et de l'artefact (retours du lot, point 6) : ils ne sont pas tenus")]
        [Tooltip("Hauteur au-dessus de la paume (m), en plus du point de la DA (Socket_Orbe, Socket_Artefact) ou du réglage greybox.")]
        public float floatHeight = 0f;                                   // PR #6 : les sockets de la DA placent déjà l'orbe et l'artefact
        [Tooltip("Montée et descente lentes : amplitude (m) et période (s).")]
        [Min(0f)] public float floatAmplitude = 0.007f;
        [Min(0.1f)] public float floatPeriod = 2.6f;
        [Tooltip("Rotation lente de l'orbe (°/s) ; légère oscillation de l'artefact (°), qui reste pointé vers l'avant.")]
        public float floatSpin = 25f;

        [Header("Pose « tenir » au repos (retours du 2026-11-17) : doigts à demi refermés autour de l'orbe ou du barillet")]
        [Tooltip("Pose de la DA vers laquelle les doigts se referment (poing).")]
        public string holdTowardPose = "Pose_Poing";
        [Tooltip("Main droite (orbe) au repos : part du chemin vers le poing (0 : boucle de la DA telle quelle, 1 : poing fermé).")]
        [Range(0f, 1f)] public float restHoldRight = 0.45f;
        [Tooltip("Main gauche au repos avec le barillet.")]
        [Range(0f, 1f)] public float restHoldLeftBarrel = 0.5f;
        [Tooltip("Main gauche au repos avec la lanterne (la boucle de la DA tient déjà l'anneau).")]
        [Range(0f, 1f)] public float restHoldLeftLantern = 0f;
        [Tooltip("Main gauche au repos sans artefact.")]
        [Range(0f, 1f)] public float restHoldLeftEmpty = 0.3f;
        [Tooltip("Le pouce se referme moins (multiplicateur) : il reste sur le côté de l'objet.")]
        [Range(0f, 1f)] public float restHoldThumb = 0.5f;
        [Min(0f)] public float floatWobble = 4f;
        [Tooltip("Décalage de phase entre l'orbe et l'artefact (fraction de période) : ils ne montent pas ensemble.")]
        [Range(0f, 1f)] public float floatPhaseOffset = 0.35f;

        [Header("Lumière d'appoint de la vue FPS (calque de rendu « FPS » seulement : jamais le décor)")]
        [Tooltip("Une lumière faible qui n'éclaire que les mains, l'orbe et l'artefact, pour qu'ils se lisent même quand seule la lune éclaire.")]
        public bool fillLight = true;
        [Min(0f)] public float fillLightIntensity = 1.8f;                  // bras peints de la PR #7 : lisibles quand seule la lune éclaire
        [Min(0.1f)] public float fillLightRange = 2.2f;
        public Color fillLightColor = new Color(0.78f, 0.84f, 1f);
        [Tooltip("Position de la lumière dans le repère de la caméra (m) : au-dessus et un peu derrière, pour un éclairage égal des mains et des objets.")]
        public Vector3 fillLightOffset = new Vector3(0f, 0.55f, -0.25f);   // loin des objets : éclairage égal, barillet non brûlé

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
