using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Sons du jeu livrés par la DA (PR #20, ART-HANDOFF « Sons du jeu ») à la place des sons synthétisés provisoires
    /// (ProceduralSfx, gardés en repli quand un son manque). Chargé depuis Resources/GameAudio_Default. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "GameAudio_Default", menuName = "ItHunts/Réglages/Sons du jeu")]
    public sealed class GameAudioSettings : ScriptableObject
    {
        [Header("Traqueur")]
        public AudioClip stalkerSteps;
        public AudioClip stalkerBreath;
        public AudioClip stalkerGrowl;
        public AudioClip[] stalkerEnragedCries = new AudioClip[0];
        [Tooltip("Cri quand il apparaît (porte enfoncée, entrée du boss), PR #50.")]
        public AudioClip[] stalkerAppearCries = new AudioClip[0];
        [Tooltip("Cri « je t'ai vu » quand il repère un joueur (au plus toutes les 8 s), PR #50.")]
        public AudioClip[] stalkerSpottedCries = new AudioClip[0];
        public AudioClip stalkerGrabCry;
        [Range(0f, 1f)] public float stalkerStepsVolume = 0.9f;
        [Range(0f, 1f)] public float stalkerCryVolume = 1f;

        [Header("Goule")]
        public AudioClip ghoulAlert;
        public AudioClip[] ghoulAttacks = new AudioClip[0];
        public AudioClip ghoulDeath;
        [Range(0f, 1f)] public float ghoulVolume = 0.85f;

        [Header("Volant")]
        public AudioClip flyerCry;
        public AudioClip flyerWings;
        public AudioClip flyerDeath;
        [Range(0f, 1f)] public float flyerVolume = 0.8f;
        [Range(0f, 1f)] public float flyerWingsVolume = 0.45f;

        [Header("Armes")]
        public AudioClip[] barrelShots = new AudioClip[0];
        public AudioClip barrelReload;
        public AudioClip lanternJetStart;
        public AudioClip lanternJetLoop;
        [Tooltip("Fondu de fin de la boucle du jet (s).")]
        [Min(0.01f)] public float lanternLoopFade = 0.15f;
        [Tooltip("Épée en os du koala (jour 5, point 4) : lame dans le vide au départ des 1er et 2e coups (hauteur variée). Branchée par " +
                 "ItHunts > Brancher les illustrations des améliorations (sons « Epee » d'Assets/Art/Audio) ; silence tant qu'elle manque.")]
        public AudioClip swordSwing;
        [Tooltip("Épée : 3e coup, fente et tourbillon dans le vide ; sans lui, swordSwing.")]
        public AudioClip swordSwingHeavy;
        [Tooltip("Épée : la lame touche ; sans lui, le son d'impact commun (impactHit).")]
        public AudioClip swordHit;
        [Range(0f, 1f)] public float swordVolume = 0.75f;
        [Tooltip("Épée (nuit 5, lot 4) : lame dans le vide à chaque coup (taille et estoc), une variante au hasard sans répéter la précédente " +
                 "(Sfx_Epee_Vide_01 à 03). Prioritaire sur swordSwing / swordSwingHeavy, gardés en repli.")]
        public AudioClip[] swordSwings = new AudioClip[0];
        [Tooltip("Épée : la lame touche, une fois par coup quel que soit le nombre d'ennemis touchés (Sfx_Epee_Impact_01 à 03). " +
                 "Prioritaire sur swordHit.")]
        public AudioClip[] swordHits = new AudioClip[0];
        [Tooltip("Variation de hauteur des sons de l'épée (± part).")]
        [Range(0f, 0.3f)] public float swordPitchVariation = 0.04f;

        /// <summary>
        /// Emplacements qui attendent des sons de la DA : silence tant qu'ils sont vides (jamais un son synthétisé). Vidée à mesure que
        /// les sons arrivent. Nuit 5, lot 4 : l'épée est servie par swordSwings et swordHits ; ces trois champs uniques restent en repli.
        /// </summary>
        public static readonly string[] AwaitingArt = { nameof(swordSwing), nameof(swordSwingHeavy), nameof(swordHit) };

        [Header("Orbe et mouvement (art-sons-2) : silence tant qu'ils manquent")]
        public AudioClip tractionStart;
        public AudioClip tractionHum;
        public AudioClip tractionEnd;
        public AudioClip repulsion;
        public AudioClip refusal;
        public AudioClip chargeReady;

        [Header("Combat (art-sons-2) : silence tant qu'ils manquent")]
        public AudioClip impactHit;
        /// <summary>Objet lourd (caisse, ≥ 15 kg) qui touche : Sfx_Impact_Lourd ; sans lui, impactHit plus grave.</summary>
        public AudioClip impactHeavy;
        public AudioClip enemyKill;
        public AudioClip objectBreak;
        public AudioClip stun;
        public AudioClip parrySuccess;
        public AudioClip parryMiss;
        public AudioClip headshot;
        public AudioClip barrelReloadDone;
        public AudioClip lanternRelight;
        public AudioClip ghoulHiss;
        public AudioClip flyerScreech;

        [Header("Monde (art-sons-2) : silence tant qu'ils manquent")]
        public AudioClip soulPickup;
        public AudioClip wavePeak;
        public AudioClip doorOpen;
        public AudioClip gateRise;
        public AudioClip fallVoid;

        [Header("Boss final (art-sons-2) : silence tant qu'ils manquent")]
        public AudioClip bossHit;
        public AudioClip bossWeakPoint;
        public AudioClip bossPhase;
        public AudioClip bossSlam;
        /// <summary>Bond du boss : morsure à la réception (Sfx_Boss_Attaque_Morsure).</summary>
        public AudioClip bossBite;
        public AudioClip bossDeath;

        [Header("Nuit 3 (PR #41) : musiques et ambiances par acte, peur, mouvements, interface")]
        public AudioClip musicChateau;
        public AudioClip musicJardin;
        public AudioClip musicBoss;
        public AudioClip ambienceCave, ambienceChateau, ambienceJardin;
        [Range(0f, 1f)] public float ambienceVolume = 0.6f;
        [Tooltip("Cœur qui bat quand le traqueur est proche : paliers à 60 / 40 / 20 m (volume 0.3 / 0.6 / 1, vitesse 1 / 1.25 / 1.5).")]
        public AudioClip heartbeat;
        public AudioClip stalkerArrival;
        public AudioClip lockedDoor, menuInteraction, purchase;
        public AudioClip grab, release, throwObject;
        public AudioClip dash, jump, land, slideLoop, wallJump, mantle, waterDive;
        public AudioClip paralysis, explosionKill, crateBreak;
        public AudioClip uiHover, uiClick, uiBack;

        [Header("Musique en jeu")]
        public AudioClip musicExploration;
        public AudioClip musicEnraged;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        [Tooltip("Fondu croisé exploration ↔ enragé (s).")]
        [Min(0.05f)] public float musicCrossfade = 2.5f;

        [Header("Variété")]
        [Tooltip("Variation de hauteur des sons des créatures et des armes (± part).")]
        [Range(0f, 0.3f)] public float pitchVariation = 0.06f;
        [Tooltip("Portée des sons des créatures en 3D (m).")]
        [Min(1f)] public float creatureRange = 40f;
    }
}
