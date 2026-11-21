using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Le barillet (premier artefact, D-022, D-032) : la main gauche tient un barillet de 6 coups. Tir instantané
    /// (rayon depuis les yeux, sans chute) sur le clic gauche / RT quand l'orbe ne tient rien ; rechargement automatique
    /// quand il est vide, ou plus tôt avec R / Y. Inactif pendant que l'orbe tient un objet. Valeurs de départ :
    /// docs/design/features/artefacts/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Barrel_Default", menuName = "ItHunts/Artefacts/Barillet")]
    public sealed class BarrelSettings : ArtifactDefinition
    {
        public BarrelSettings() => viewScale = 0.5f;

        [Header("Tir")]
        [Tooltip("Munitions dans le barillet.")]
        [Min(1)] public int rounds = 6;
        [Tooltip("Dégâts d'une balle.")]
        [Min(0f)] public float damage = 12f;
        [Tooltip("Temps minimal entre deux tirs (s).")]
        [Min(0.02f)] public float fireInterval = 0.25f;
        [Tooltip("Portée du tir instantané (m) ; au-delà, la balle se perd.")]
        [Min(1f)] public float range = 60f;
        [Tooltip("Un clic arrivé un peu trop tôt (cadence, fin de rechargement) part dès que possible pendant ce délai (s).")]
        [Min(0f)] public float pressBuffer = 0.12f;
        [Tooltip("Ce que la balle peut toucher (le joueur qui tire est toujours ignoré).")]
        public LayerMask hitMask = ~0;

        [Header("Rechargement")]
        [Tooltip("Durée du rechargement (s), automatique quand le barillet est vide, ou sur R / Y.")]
        [Min(0f)] public float reloadTime = 1.2f;
        [Tooltip("Prendre un objet pendant un rechargement l'annule (vide : il reprendra à zéro au relâchement). " +
                 "Décoché : il continue et se termine pendant que l'objet est tenu.")]
        public bool grabCancelsReload = true;

        [Header("Retour visuel")]
        [Tooltip("Traînée lumineuse : durée (s), largeur au départ et à l'arrivée (m), couleur (HDR, lueur).")]
        [Min(0.01f)] public float tracerDuration = 0.06f;
        [Min(0f)] public float tracerStartWidth = 0.02f;
        [Min(0f)] public float tracerEndWidth = 0.05f;
        [ColorUsage(false, true)] public Color tracerColor = new Color(4f, 2.6f, 1f);
        [Tooltip("Impact : taille (m) et durée (s) de l'éclat au point touché.")]
        [Min(0f)] public float impactSize = 0.25f;
        [Min(0.01f)] public float impactDuration = 0.12f;
        [Tooltip("Chambres chargées (lueur), vides, et barillet inactif (objet tenu).")]
        [ColorUsage(false, true)] public Color loadedColor = new Color(3f, 1.8f, 0.6f);
        public Color emptyColor = new Color(0.05f, 0.05f, 0.05f);
        [ColorUsage(false, true)] public Color inactiveColor = new Color(0.4f, 0.35f, 0.3f);
        [Tooltip("Recul de la main gauche à chaque tir : distance (m), relèvement (°), durée (s).")]
        [Min(0f)] public float recoilKick = 0.035f;
        [Min(0f)] public float recoilPitch = 8f;
        [Min(0.02f)] public float recoilDuration = 0.12f;

        [Header("Vue FPS : taille, décalage et flottement au-dessus de la paume gauche")]
        // Échelle du barillet : viewScale de ArtifactDefinition (un seul champ sérialisé ; 0.5 ≈ la largeur de la paume, le modèle
        // fait 0.10 m de diamètre). Le champ était déclaré deux fois (erreur de sérialisation au build du matin du 2026-11-21).
        [Tooltip("Centre du barillet dans le repère de la main gauche (m), au-dessus de la paume ; son axe de tir (+Z) vise devant.")]
        public Vector3 viewInHand = new Vector3(0f, 0.11f, 0.05f);
        [Tooltip("Rotation du barillet (degrés), en plus de l'orientation de la vue.")]
        public Vector3 viewInHandEuler = Vector3.zero;
        [Tooltip("Flottement : amplitude (m), période (s) et oscillation (°) ; il ne monte pas en même temps que l'orbe (floatPhaseOffset de la vue).")]
        [Min(0f)] public float floatAmplitude = 0.006f;
        [Min(0.1f)] public float floatPeriod = 2.6f;
        [Min(0f)] public float floatWobble = 4f;

        [Header("Modèle SM_Barillet (vue FPS)")]
        [Tooltip("Couleur d'émission d'une chambre chargée (blanc chaud provisoire, pas le violet de la télékinésie) ; multipliée par l'intensité.")]
        public Color chamberEmission = new Color(1f, 0.86f, 0.66f);
        [Tooltip("Intensité de l'émission d'une chambre chargée : un peu au-delà du seuil de bloom (1) pour qu'elle brille sans éblouir.")]
        [Min(0f)] public float chamberEmissionIntensity = 1.4f;
        [Tooltip("Part de l'émission gardée quand le barillet est inactif (l'orbe tient un objet).")]
        [Range(0f, 1f)] public float chamberInactiveFactor = 0.2f;
        [Tooltip("Couleur de base des chambres (chargées ou vides) : presque noir, #1A120C.")]
        public Color chamberBaseColor = new Color32(0x1A, 0x12, 0x0C, 0xFF);
        [Tooltip("Durée de la rotation d'un cran (−60°) à chaque tir (s).")]
        [Min(0f)] public float spinDuration = 0.08f;
        [Tooltip("Courbe de la rotation (0 → 1 sur la durée).")]
        public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Sons (provisoires)")]
        [Range(0f, 1f)] public float shotVolume = 0.7f;
        [Range(0f, 1f)] public float reloadVolume = 0.5f;
    }
}
