using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Le traqueur (boss unique, D-002, D-033, D-034) : un géant de 14 m qui attrape à la main (portée 15 m depuis
    /// l'épaule, prise annoncée 0.5 s), tue en un coup, devient enragé à la fin du timer et il est intuable. Vitesse : base Rôde / Chasse / Enragé, pointe quand il voit le joueur, suivi de la vitesse du joueur
    /// plafonné sous le bunny hop, et visée de l'endroit où le joueur sera (prédiction). Il ne bride jamais le mouvement
    /// du joueur (D-012). Valeurs de départ : docs/design/features/boss-traqueur/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Stalker_Default", menuName = "ItHunts/Ennemis/Traqueur")]
    public sealed class StalkerSettings : EnemySettings
    {
        [Header("Vitesses (m/s) — course du joueur 7.5, plafond des gains 20, bunny hop 18 à 26, traction 30")]
        [Min(0f)] public float roamSpeed = 3f;
        [Tooltip("Alerte : il va voir le dernier bruit ou la dernière position aperçue.")]
        [Min(0f)] public float alertSpeed = 6f;
        [Min(0f)] public float chaseSpeed = 9f;
        [Min(0f)] public float enragedSpeed = 11f;
        [Min(0f)] public float searchSpeed = 4f;
        [Tooltip("Pointe quand il voit le joueur (m/s), normale et enragée, pendant burstDuration, puis burstCooldown.")]
        [Min(0f)] public float burstSpeed = 16f;
        [Min(0f)] public float enragedBurstSpeed = 18f;
        [Min(0f)] public float burstDuration = 3f;
        [Min(0f)] public float burstCooldown = 6f;
        [Tooltip("Suivi de la vitesse du joueur en Chasse (option c) : il va au moins à cette part de la vitesse du joueur, " +
                 "jamais au-delà de sa pointe (plafond sous le bunny hop). 0 : désactivé.")]
        [Range(0f, 1f)] public float followPlayerSpeedShare = 0f;
        [Min(0f)] public float acceleration = 12f;
        [Tooltip("Vitesse de rotation (°/s) : les virages serrés permettent de lui échapper.")]
        [Min(0f)] public float turnRate = 180f;

        [Header("Enragé : il accélère tant qu'il reste sur la même cible (D-038)")]
        [Tooltip("Paliers : temps passé sur la même cible (s) et vitesse de base (m/s) à partir de ce temps. Repart de zéro s'il " +
                 "change de cible ou si la cible meurt.")]
        public float[] enragedRampTimes = { 0f, 10f, 20f, 25f, 30f };
        public float[] enragedRampSpeeds = { 11f, 13f, 15f, 16.5f, 18f };
        [Tooltip("Montée progressive vers chaque palier (s).")]
        [Min(0f)] public float enragedRampBlend = 1f;
        [Tooltip("Vitesse maximale du traqueur, pointe comprise (m/s).")]
        [Min(0f)] public float maxSpeed = 18f;

        [Header("Poursuite façon Pac-Man (D-035)")]
        [Tooltip("Il ne fait jamais demi-tour (sauf dans un cul-de-sac) ; en Enragé, demi-tour autorisé si coché.")]
        public bool allowUTurnWhenEnraged = true;
        [Tooltip("Il fonce droit sur le joueur quand celui-ci est à moins de cette distance (m) et devant lui (angle total °) ; " +
                 "sinon il suit le labyrinthe d'intersection en intersection.")]
        [Min(0f)] public float approachDistance = 35f;
        [Range(10f, 360f)] public float approachAngle = 200f;
        [Tooltip("Arrivée à une intersection (m) : il choisit la branche suivante.")]
        [Min(1f)] public float nodeArrivalDistance = 9f;
        [Tooltip("Coop : il change de cible (le joueur le plus proche par le chemin) au plus une fois par intervalle (s).")]
        [Min(0f)] public float retargetInterval = 5f;

        [Header("Se baisser (D-035)")]
        [Tooltip("Hauteur minimale de passage (m) : sous un pont, une poutre ou un passage plus bas que lui, il se baisse ; " +
                 "plus bas que ça, il ne passe pas.")]
        [Min(1f)] public float crouchMinHeight = 9f;
        [Tooltip("Vitesse quand il est baissé (part de sa vitesse).")]
        [Range(0.1f, 1f)] public float crouchSpeedFactor = 0.6f;

        [Header("Prédiction")]
        [Tooltip("Il vise l'endroit où sera le joueur dans ce temps (s), selon sa vitesse.")]
        [Min(0f)] public float predictionHorizon = 0.8f;
        [Min(0.02f)] public float repathInterval = 0.2f;

        [Header("Perception")]
        [Tooltip("Vue : portée (m), angle total (°), hauteur des yeux (m).")]
        [Min(0f)] public float sightRange = 60f;
        [Range(1f, 360f)] public float sightAngle = 110f;
        [Min(0f)] public float eyeHeight = 12.5f;
        [Tooltip("Ouïe (m) : atterrissage dur, objet brisé, tir du barillet, répulsion, combat (coups portés).")]
        [Min(0f)] public float hearHardLanding = 15f;
        [Min(0f)] public float hearBreak = 25f;
        [Min(0f)] public float hearShot = 25f;
        [Min(0f)] public float hearRepel = 15f;
        [Min(0f)] public float hearCombat = 20f;
        [Tooltip("Recherche : durée (s) et rayon (m) autour de la dernière position connue avant de retourner rôder.")]
        [Min(0f)] public float searchDuration = 10f;
        [Min(0f)] public float searchRadius = 8f;
        [Tooltip("Sans le voir pendant ce temps (s), la Chasse devient Recherche.")]
        [Min(0f)] public float loseSightTime = 1.5f;

        [Header("Prise à la main (D-034) : elle tue quels que soient les PV")]
        [Tooltip("Hauteur de l'épaule (m) ; la main atteint tout point à moins de handReach de l'épaule, en hauteur aussi.")]
        [Min(0f)] public float shoulderHeight = 12f;
        [Min(0f)] public float handReach = 15f;
        [Tooltip("Annonce : le bras se lève et une lueur rouge marque la zone visée pendant ce temps (s).")]
        [Min(0f)] public float grabTelegraph = 0.5f;
        [Tooltip("Durée de la descente de la main (s), puis temps où elle reste au sol (s).")]
        [Min(0.01f)] public float grabSlam = 0.12f;
        [Min(0f)] public float grabHold = 0.35f;
        [Tooltip("Recharge entre deux prises (s), à partir de la fin de la prise.")]
        [Min(0f)] public float grabCooldown = 2.5f;
        [Tooltip("Main plantée (D-039) : après une prise ratée, la main reste au sol ce temps (s) ; pas de nouvelle prise, rotation " +
                 "ralentie (part de la vitesse de rotation), pas de pointe. Le corps et la main plantée tuent toujours au contact.")]
        [Min(0f)] public float plantedDuration = 1.5f;
        [Range(0.05f, 1f)] public float plantedTurnFactor = 0.4f;
        [Tooltip("Main plantée (D-039, piste b) : part de sa vitesse normale gardée par le corps, qui continue vers le joueur " +
                 "(1 : vitesse normale ; grabMoveFactor pendant une prise ordinaire).")]
        [Range(0f, 1f)] public float plantedMoveFactor = 1f;
        [Header("Revers de la main gauche (D-039, piste a)")]
        [Tooltip("Pendant la main plantée, un joueur à portée déclenche un revers (une fois par main plantée) : annonce courte (zone " +
                 "rouge au sol), puis coup mortel sur toute la zone.")]
        public bool backhandEnabled = true;
        [Tooltip("Portée du revers (m), à l'horizontale depuis le bord de son corps ; hauteur couverte au-dessus de ses pieds (m).")]
        [Min(0f)] public float backhandReach = 7f;
        [Min(0f)] public float backhandHeight = 12f;
        [Tooltip("Délai après le début de la main plantée avant que le revers puisse partir (s) : le joueur qui vient d'esquiver a ce temps pour s'éloigner.")]
        [Min(0f)] public float backhandDelay = 0.5f;
        [Tooltip("Annonce (s) puis durée du coup (s), mortel dans toute la zone.")]
        [Min(0f)] public float backhandTelegraph = 0.2f;
        [Min(0.02f)] public float backhandSweep = 0.25f;

        [Tooltip("Pendant une prise, il ralentit à cette part de sa vitesse.")]
        [Range(0f, 1f)] public float grabMoveFactor = 0.3f;
        [Tooltip("Taille de la main (m, largeur × épaisseur × longueur) : la zone de prise est la main elle-même, posée sur " +
                 "le point visé.")]
        public Vector3 handSize = new Vector3(4f, 2.2f, 4.5f);

        [Header("Mains au-dessus de la tête (D-037) : on ne passe plus par-dessus lui")]
        [Tooltip("Zone surveillée : cylindre autour de lui (rayon, m), d'une hauteur minimale au-dessus de ses pieds jusqu'à " +
                 "tant de mètres au-dessus de sa tête. Un joueur dedans, ou qui va y passer (trajectoire prédite), déclenche le geste.")]
        [Min(0f)] public float overheadRadius = 10f;
        [Min(0f)] public float overheadMinHeight = 7f;
        [Min(0f)] public float overheadAboveHead = 12f;
        [Tooltip("Trajectoire prédite du joueur (s) : en ligne droite et en chute.")]
        [Min(0f)] public float overheadPrediction = 0.6f;
        [Tooltip("Annonce (s) : les deux mains se lèvent ; puis zone mortelle (s) : ses mains suivent le joueur au-dessus de lui.")]
        [Min(0f)] public float overheadTelegraph = 0.2f;
        [Min(0.05f)] public float overheadDuration = 1f;
        [Tooltip("Recharge du geste (s), séparée de celle de la prise à la main.")]
        [Min(0f)] public float overheadCooldown = 1.5f;
        [Tooltip("Vitesse des mains pendant le geste (m/s) et écart entre les deux mains (m).")]
        [Min(0f)] public float overheadHandSpeed = 45f;
        [Min(0f)] public float overheadHandSpread = 3f;
        [Tooltip("Bras (boîtes de l'épaule à la main) : épaisseur (m) ; tout contact avec le corps, les bras ou les mains tue.")]
        [Min(0.1f)] public float armThickness = 1.5f;
        [Tooltip("Abris : sa main passe par-dessus les murs et les bords, mais il lui faut ce dégagement (m) au-dessus du " +
                 "point visé ; sous un plafond plus bas (tunnels de 4 m, petites portes), elle ne passe pas.")]
        [Min(0f)] public float handClearance = 4.5f;
        [Tooltip("Visée corrigée (s) : à ce temps avant la descente, il vise de nouveau le point prédit.")]
        [Min(0f)] public float grabReaim = 0.15f;
        [Tooltip("Pièces où il n'entre pas : il s'arrête devant la porte ou la fenêtre et passe le bras ; sa main va jusqu'à " +
                 "cette distance (m) à l'intérieur, depuis l'ouverture.")]
        [Min(0f)] public float roomReach = 15f;
        [Tooltip("Bords de la carte : collé à un mur, il se penche et tend le bras de cette longueur en plus (m).")]
        [Min(0f)] public float edgeLean = 2.5f;
        [Tooltip("Il ne peut pas t'atteindre (passage étroit) : il vise où tu seras dans ce temps (s) pour te couper la sortie.")]
        [Min(0f)] public float cutOffHorizon = 3f;

        [Header("Contact du corps")]
        [Tooltip("Distance entre sa surface et celle du joueur (m) en dessous de laquelle son corps tue aussi.")]
        [Min(0f)] public float attackRange = 0.3f;

        [Header("Corps")]
        [Tooltip("Largeur et hauteur (m) : il ne passe pas les petites portes ni les couloirs bas (NavMesh à part, rayon = " +
                 "largeur / 2, hauteur = hauteur) ; il ne grimpe pas.")]
        [Min(0.5f)] public float width = 7f;
        [Min(0.5f)] public float height = 14f;

        [Header("Sons (provisoires)")]
        [Range(0f, 1f)] public float stepVolume = 0.9f;
        [Range(0f, 1f)] public float breathVolume = 0.5f;
        [Tooltip("Distance (m) jusqu'à laquelle on l'entend (atténuation 3D).")]
        [Min(1f)] public float soundRange = 60f;
        [Tooltip("Ses pas font trembler la caméra à moins de cette distance (m) ; force à bout portant (0..1). Réglage de " +
                 "confort du joueur : CameraShake.")]
        [Min(0f)] public float shakeRange = 25f;
        [Range(0f, 1f)] public float shakeStrength = 0.6f;
        [Tooltip("Pas par seconde à la vitesse de chasse.")]
        [Min(0.1f)] public float stepsPerSecondAtChase = 1.6f;

        [Header("Signal : colonne de lumière de la zone de prise (bloc 4)")]
        [Tooltip("Colonne rouge faible au-dessus du disque de la zone de prise pendant l'annonce : lisible à 50 m (un disque au sol vu de loin devient une ligne).")]
        public bool grabColumn = true;
        [Min(0.5f)] public float grabColumnHeight = 12f;
        [Tooltip("Diamètre de la colonne (m) : celui du disque de la zone de prise.")]
        [Min(0.2f)] public float grabColumnDiameter = 4f;
        [Min(0f)] public float grabColumnIntensity = 1f;
        [ColorUsage(false, true)] public Color grabColumnColor = new Color(1.6f, 0.08f, 0.04f);
        [Tooltip("Matériau additif qui traverse le brouillard (ItHunts/SignalAdditif).")]
        public Material grabColumnMaterial;
    }
}
