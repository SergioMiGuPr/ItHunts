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
