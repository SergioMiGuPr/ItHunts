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
        [Header("Vitesses (m/s) — course du joueur 8.5, plafond des gains 20, bunny hop 18 à 26, traction 30")]
        [Tooltip("Nuit 3, point 5 : ajouté à sa vitesse dans tous les états (rôde, alerte, recherche, chasse, pointe, enragé), plafond compris.")]
        [Range(-3f, 3f)] public float speedBonus = 0.5f;
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

        [Header("Passages des modules (point 9, 2026-11-21) : liens typés du NavMesh, un clip par type plus tard")]
        [Tooltip("Il emprunte les passages posés par les modules (bonds, escalades, chaînes, portes étroites).")]
        public bool passagesEnabled = true;
        [Tooltip("Bond : distance horizontale maximale (m) et dénivelé maximal (m).")]
        [Min(1f)] public float leapMaxDistance = 22f;
        [Min(0f)] public float leapMaxRise = 6f;
        [Tooltip("Durée d'un bond (s, à mi-distance) et hauteur de l'arc au-dessus de la ligne (m).")]
        [Min(0.2f)] public float leapDuration = 1.1f;
        [Min(0f)] public float leapArcHeight = 5f;
        [Tooltip("Télégraphe avant un bond (s) : il s'arrête, se ramasse et crie ; le joueur peut esquiver.")]
        [Min(0f)] public float leapTelegraph = 0.55f;
        [Tooltip("Récupération après la réception à quatre pattes (s).")]
        [Min(0f)] public float leapRecovery = 0.8f;
        [Tooltip("Un bond tue le joueur que son corps traverse (même règle que la prise) : rayon autour du milieu de son corps (m).")]
        [Min(0f)] public float leapHitRadius = 3.2f;
        [Tooltip("Hauteurs maximales : rebord, mur grimpable, chaîne (m).")]
        [Min(0f)] public float ledgeMaxHeight = 8f;
        [Min(0f)] public float wallMaxHeight = 16f;
        [Min(0f)] public float chainMaxHeight = 16f;
        [Tooltip("Saut depuis une chaîne : distance maximale (m).")]
        [Min(1f)] public float chainJumpMaxDistance = 18f;
        [Tooltip("Vitesse de montée (escalade, mur, chaîne, m/s).")]
        [Min(0.1f)] public float climbSpeed = 6f;
        [Tooltip("Pause avant de grimper (s).")]
        [Min(0f)] public float climbStartPause = 0.2f;
        [Tooltip("Porte étroite : part de sa vitesse quand il se faufile.")]
        [Range(0.1f, 1f)] public float narrowDoorSlowFactor = 0.45f;
        [Tooltip("Plus petite porte qu'il franchit en se faufilant (m) : mesurée par la DA sur Traqueur_PorteEtroite (PR #32) : 4 × 5.2 m ; " +
                 "plus petite : seul son bras passe.")]
        [Min(1f)] public float narrowDoorHeight = 5.2f;
        [Min(1f)] public float narrowDoorWidth = 4f;

        [Header("Mouvement provisoire (clips de la DA absents, C5)")]
        [Tooltip("Foulée du cycle de marche provisoire (m par cycle de deux pas) : les pieds suivent la vitesse au sol.")]
        [Min(1f)] public float provisionalStride = 9f;
        [Tooltip("Dos voûté au repos (°, réparti sur la colonne et le cou ; la tête regarde devant).")]
        [Range(0f, 80f)] public float provisionalHunch = 62f;
        [Tooltip("Genoux fléchis au repos (m dont le bassin descend) : avec le dos voûté, épaule à 10.5 m et 0.9 m devant (DA).")]
        [Range(0f, 2f)] public float provisionalKneeDrop = 0.3f;
        [Range(0f, 60f)] public float provisionalLegSwing = 24f;
        [Range(0f, 60f)] public float provisionalArmSwing = 20f;

        [Header("Visage (couche séparée, C4) : regard, mâchoire, cri, grognement, clignements, bave")]
        [Tooltip("Regard vers le joueur repéré : angle maximal à gauche et à droite, en haut et en bas (°), vitesse d'entrée (1/s).")]
        [Range(0f, 120f)] public float faceLookYaw = 70f;
        [Range(0f, 90f)] public float faceLookPitch = 45f;
        [Min(0.1f)] public float faceLookSpeed = 3f;
        [Tooltip("Mâchoire qui pend (poids de MachoirePendante, 0 à 100) et son oscillation.")]
        [Range(0f, 100f)] public float faceJawHang = 35f;
        [Range(0f, 50f)] public float faceJawSway = 22f;
        [Tooltip("Os de la mâchoire (Machoire), en plus des formes (2026-11-21) : ouverture au cri (°), pendante au repos (°) et " +
                 "oscillation (°, en continu). Rotation seule : aucune échelle, aucune déformation du modèle.")]
        [Range(0f, 60f)] public float faceJawCryAngle = 34f;
        [Range(0f, 30f)] public float faceJawHangAngle = 7f;
        [Range(0f, 20f)] public float faceJawSwayAngle = 5f;
        [Tooltip("Durée du cri (forme Cri) au passage en Enragé et juste avant une prise (s), avec cri_enrage ou cri_prise.")]
        [Min(0.2f)] public float faceCryDuration = 1.4f;
        [Tooltip("Grognement à proximité : distance (m), durée de la forme (s), temps entre deux grognements (s).")]
        [Min(0f)] public float faceGrowlRange = 15f;
        [Min(0.2f)] public float faceGrowlDuration = 1.6f;
        [Min(0.5f)] public float faceGrowlCooldown = 7f;
        [Tooltip("Clignements : intervalle au hasard (s).")]
        public Vector2 faceBlinkInterval = new Vector2(2.5f, 6f);
        [Tooltip("Bave en particules (gouttes par seconde, 0 : aucune).")]
        [Min(0f)] public float faceDroolRate = 3f;

        [Header("IK des membres (C3, visuelle) : appuis au sol, mains aux murs et aux chaînes")]
        public bool limbIkEnabled = true;
        [Tooltip("Fondu d'entrée et de sortie de l'IK (s).")]
        [Min(0.01f)] public float limbIkBlend = 0.15f;
        [Tooltip("Un pied ou une main à moins de cette hauteur au-dessus de son sol (m) est en appui : il va sur le sol réel.")]
        [Min(0f)] public float limbIkPlantHeight = 0.5f;
        [Tooltip("Écart de sol corrigé au plus (m) : au-delà (vide, fosse), l'appui garde la pose du clip.")]
        [Min(0f)] public float limbIkMaxReach = 2f;
        [Tooltip("Course : une main écartée de plus de cette distance de l'axe (m) va se plaquer sur un mur à portée (m).")]
        [Min(0f)] public float limbIkWallSpread = 2f;
        [Min(1f)] public float limbIkWallReach = 9f;
        [Tooltip("Main sur un mur : seulement si le point touché est à portée RÉELLE du bras (épaule → griffes × cette part) ; " +
                 "sinon aucune IK (2026-11-21 : il posait les mains dans le vide).")]
        [Range(0.5f, 1f)] public float limbIkReachMargin = 0.95f;
        [Tooltip("Debug visuel des contacts de l'IK : vert = main ou pied posé, rouge = mur trouvé mais hors de portée (refusé).")]
        public bool limbIkDebug;

        [Header("Prise : alignée sur le joueur (2026-11-21)")]
        [Tooltip("Pendant l'annonce et la descente de la prise, il pivote vers la position réelle du joueur (°/s).")]
        [Min(0f)] public float grabFaceRate = 260f;
        [Tooltip("Pendant la fente (clip de prise), la main de prise est tirée vers le joueur par l'IK (poids max, 0 : aucune).")]
        [Range(0f, 1f)] public float grabIkWeight = 0.85f;

        [Header("Cheveux : chaînes d'os à ressort (v4, D-081)")]
        [Tooltip("Active le ressort des cheveux (os Cheveux_N_2 à _4) du modèle v4.")]
        public bool boneChainsEnabled = true;
        [Tooltip("Réglages des chaînes d'os du modèle v4 (raideur, amortissement, gravité, inertie, rayon de collision).")]
        public BoneSpringSettings boneChains = BoneSpringSettings.Default;

        [Header("Se baisser (D-035)")]
        [Tooltip("Hauteur minimale de passage (m) : sous un pont, une poutre ou un passage plus bas que lui, il se baisse ; " +
                 "plus bas que ça, il ne passe pas.")]
        [Min(1f)] public float crouchMinHeight = 8.5f;
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
        [Tooltip("Hauteur de l'épaule (m) dans sa pose voûtée (DA, NOTES.md du v4 : 10.5 m ; debout bras tendu : 11.1 m) ; la main " +
                 "atteint tout point à moins de GrabReach (bras + fente) de l'épaule, en hauteur aussi.")]
        [Min(0f)] public float shoulderHeight = 10.5f;
        [Tooltip("Avance de l'épaule devant ses pieds dans la pose voûtée (m, DA : 0.9 m).")]
        public float shoulderForward = 0.9f;
        [Tooltip("Écart latéral de l'épaule au centre (m) : 1.37 m sur SK_Traqueur_v4 (os Bras_D).")]
        [Min(0f)] public float shoulderSide = 1.37f;
        [Tooltip("Pupilles (émission par le masque de M_Traqueur), toujours allumées : rouge sombre et discret (retours du lot, point 3).")]
        [ColorUsage(false, true)] public Color pupilGlow = new Color(0.55f, 0.05f, 0.03f);
        [Tooltip("Intensité des pupilles (× la couleur).")]
        [Range(0f, 4f)] public float pupilGlowIntensity = 1.5f;
        [Tooltip("Repli (v3, PR #13) : les pupilles peintes du modèle ne s'affichent pas (faces de la tête manquantes, diagnostic DA) ; " +
                 "deux petits points lumineux sur l'os Tete les remplacent. À décocher quand la tête sera corrigée.")]
        public bool eyeGlints = true;
        [Tooltip("Points des yeux dans le repère de l'os Tete (m), relevés sur les sommets des pupilles du modèle v3.")]
        public Vector3 eyeGlintRight = new Vector3(0.24f, 0.44f, 1.75f), eyeGlintLeft = new Vector3(-0.45f, 0.21f, 1.7f);
        [Tooltip("Diamètre d'un point (m).")]
        [Min(0.01f)] public float eyeGlintSize = 0.35f;
        [Tooltip("Intensité des points (× la couleur des pupilles) ; ils traversent le brouillard comme l'émission des matériaux.")]
        [Range(0f, 10f)] public float eyeGlintIntensity = 3f;
        [Tooltip("Griffes (masque d'émission de M_Traqueur_Mains) : ivoire. Pas de rouge sur les mains : la colonne au sol est le signal.")]
        [ColorUsage(false, true)] public Color clawGlow = new Color(1f, 0.93f, 0.78f);
        [Tooltip("Intensité des griffes au repos, puis pendant la prise (annonce, descente, main au sol).")]
        [Range(0f, 3f)] public float clawGlowRest = 0.15f;
        [Range(0f, 3f)] public float clawGlowGrab = 0.7f;
        [Tooltip("Vitesse de passage d'une intensité à l'autre (1/s).")]
        [Min(0.1f)] public float clawGlowRate = 8f;

        [Header("Clips du traqueur v4 (DA, PR #28 et #29 : vitesses réelles des cycles)")]
        [Tooltip("Vitesse au sol du clip Traqueur_Marche lu à la vitesse 1 (m/s, mesurée par la DA) : vitesse de lecture = vitesse ÷ cette valeur, " +
                 "pour que les pieds ne glissent pas.")]
        [Min(0.1f)] public float walkClipSpeed = 1.27f;
        [Tooltip("Vitesse au sol des courses enragées A et B (quatre pattes) et C (deux jambes) à la vitesse 1 (m/s, DA).")]
        [Min(0.1f)] public float runClipSpeedA = 11f, runClipSpeedB = 11f, runClipSpeedC = 9f;
        [Tooltip("Instants des clips de la DA (s, ART-HANDOFF) : impact de la prise (poing), prise depuis un rebord, bond-prise ; décollage " +
                 "et pose des mains du bond et du bond-prise. La mort du joueur tombe à l'instant d'impact du clip.")]
        [Min(0f)] public float clipPriseImpact = 2.37f, clipPriseRebordImpact = 3.25f, clipBondPriseImpact = 2.55f;
        [Min(0f)] public float clipBondTakeoff = 2.05f, clipBondLand = 4.6f, clipBondPriseTakeoff = 1.05f, clipBondPriseLand = 3.0f;
        [Tooltip("Chaîne (Traqueur_ChaineGrimpe) : partie qui boucle (début, durée d'un cycle, s) et montée par cycle (m) ; montée " +
                 "couverte par le clip sans boucle supplémentaire (m). Réception : sortie possible (s).")]
        [Min(0f)] public float clipChainLoopStart = 1.1f, clipChainCycle = 0.9f, clipChainRisePerCycle = 2.4f, clipChainBaseRise = 4.8f;
        [Min(0f)] public float clipReceptionExit = 1.1f;
        [Tooltip("Saut depuis une chaîne (Traqueur_ChaineSaut) : lâcher et pose des mains (s) ; prise depuis la chaîne : poing (s).")]
        [Min(0f)] public float clipChainJumpRelease = 3.4f, clipChainJumpLand = 4.2f, clipChainGrabImpact = 3.75f;
        [Tooltip("Durées des clips des passages (s, DA) : bond, saut de chaîne, escalade, chaîne, porte étroite ; servent au coût des " +
                 "liens du NavMesh (le traqueur choisit le chemin le plus court en temps). 0 : durée calculée sans clip.")]
        [Min(0f)] public float clipBondLength = 6f, clipChainJumpLength = 6f, clipClimbLength = 4.5f, clipChainClimbLength = 5f, clipDoorLength = 7f;
        [Tooltip("Prise depuis un rebord (clip Traqueur_PriseRebord) : la cible est au moins à cette hauteur sous ses pieds (m).")]
        [Min(0f)] public float ledgeGrabDrop = 4f;
        [Tooltip("Au-dessus de cette vitesse (m/s), il court (courses enragées) ; en dessous, il marche à quatre pattes (Rôde).")]
        [Min(0.5f)] public float runThreshold = 4.5f;
        [Tooltip("Vitesse au sol du clip Traqueur_PassageBas à la vitesse 1 (m/s).")]
        [Min(0.1f)] public float crouchClipSpeed = 2.29f;
        [Tooltip("Vitesse de lecture des clips de déplacement : bornes (évite un clip figé ou frénétique).")]
        public Vector2 clipPlaybackRange = new Vector2(0.3f, 5.5f);                 // ruée enragée 18 m/s ÷ 3.71 = 4.9
        [Header("Portée réelle du modèle v4 (D-080) : bras + fente du corps, aucun os étiré")]
        [Tooltip("Longueur du bras tendu, de l'épaule au bout des griffes (m) : mesurée par la DA sur SK_Traqueur_v4 (NOTES.md : 8.3 m).")]
        [Min(1f)] public float armLength = 8.3f;
        [Tooltip("Fente : distance dont le corps avance vers la cible pendant la prise (m) ; portée de la prise = bras + fente.")]
        [Min(0f)] public float grabLunge = 4.5f;
        [Tooltip("Durée de la fente (s), qui finit à l'impact de la main.")]
        [Min(0.05f)] public float grabLungeTime = 0.4f;
        /// <summary>Portée de la prise depuis l'épaule (m) : bras réel + fente (D-080, remplace les 15 m de D-070).</summary>
        public float GrabReach => armLength + grabLunge;
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
        public Vector3 handSize = new Vector3(4.9f, 1.5f, 4.5f);   // la main visible de SM_Traqueur_Main (décision du 2026-11-16)

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
                 "cette distance (m) à l'intérieur, depuis l'ouverture (v4 : bras de 8.3 m + 2 m d'épaule engagée dans une porte de 3 m, D-080).")]
        [Min(0f)] public float roomReach = 10.3f;
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

        [Header("Lisibilité dans le noir (playtest du 2026-11-17 : corps noir, on l'entendait sans le voir)")]
        [Tooltip("Lumière d'appoint qui n'éclaire que le traqueur (calque de rendu « Traqueur ») : il se détache du noir sans éclairer le décor.")]
        public bool selfLight = true;
        [Tooltip("Couleur froide et pâle (pas le rouge du danger, réservé aux signaux).")]
        public Color selfLightColor = new Color(0.55f, 0.6f, 0.78f);
        [Min(0f)] public float selfLightIntensity = 2.8f;
        [Min(1f)] public float selfLightRange = 22f;
        [Tooltip("Position de la lumière dans son repère (m) : devant lui, à mi-hauteur, pour modeler le torse et la tête.")]
        public Vector3 selfLightOffset = new Vector3(0f, 9f, 9f);
        [Tooltip("Calque de rendu réservé au traqueur (0 = Default, 1 = FPS).")]
        [Range(2, 7)] public int selfRenderingLayer = 2;

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
