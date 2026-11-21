using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Habillage des menus (DA, UI-HANDOFF § 2, PR #18) : les écrans sont les prefabs livrés par la session art (maquettes de
    /// l'utilisateur découpées au pixel) : titre, campement, choix du personnage, pause, paramètres. La logique (MainMenu,
    /// PauseMenu, CampLobby) les pilote par l'assembly ItHunts.Menus. Ici : les prefabs, les polices des menus de jeu restants
    /// (choix d'un joueur qui rejoint en cours de partie), la transition par la brume et l'audio du menu.
    /// </summary>
    [CreateAssetMenu(fileName = "MenuSkin_Default", menuName = "ItHunts/Menus/Habillage")]
    public sealed class MenuSkinSettings : ScriptableObject
    {
        [Header("Écrans de la DA (Assets/Art/UI/Menus/Prefabs)")]
        public GameObject titleScreen;
        public GameObject campScreen;
        public GameObject choiceScreen;
        public GameObject pauseScreen;
        public GameObject settingsScreen;
        [Tooltip("Nom affiché dans le cartouche d'une place du campement ({0} : numéro du joueur).")]
        public string playerNameFormat = "Joueur {0}";
        [Tooltip("Chat du campement (bas gauche) : masqué tant que le jeu en ligne n'existe pas (décision du 2026-11-17).")]
        public bool showChat;

        [Header("Polices (UI-HANDOFF : EB Garamond pour le texte, Grenze Gotisch pour les titres)")]
        public Font textFont;
        public Font textBold;
        public Font titleFont;
        [Tooltip("EB Garamond Italic : messages du menu (« J2 n'est pas prêt »), liens de développement.")]
        public Font textItalic;

        [Header("Panneaux des menus de jeu restants (choix d'un joueur qui rejoint en cours de partie)")]
        [Tooltip("Panneau en 9 tranches (cadre de la légende de la carte).")]
        public Sprite panel;
        [Tooltip("Repère du bouton qui a le focus (étoile de l'emblème).")]
        public Sprite focusMark;
        [Tooltip("Menu des touches (A5) : cartouche d'une ligne (sprite commun des menus de la DA) et actions du jeu (copie au menu principal).")]
        public Sprite keyRow;
        public UnityEngine.InputSystem.InputActionAsset controls;
        [Tooltip("Matériau additif de l'interface (ItHunts/UIAdditif).")]
        public Material additive;

        [Header("Transition titre ↔ campement : la brume monte, puis se dissipe")]
        [Min(0.05f)] public float fogRiseSeconds = 0.8f;
        [Min(0.05f)] public float fogClearSeconds = 0.8f;
        [Tooltip("Voile de la brume (dégradé qui monte).")]
        public Color transitionFog = new Color(0.36f, 0.42f, 0.55f, 1f);
        [Tooltip("Brume qui dérive par-dessus le voile (shader ItHunts/UI/Additif en mode Brume de la DA).")]
        public Material transitionFogMaterial;

        [Header("Audio du menu (Assets/Art/Audio/Menu)")]
        [Tooltip("Musique en boucle, du titre au campement sans coupure (la transition ne la touche pas).")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.55f;
        [Tooltip("Crépitement du feu en boucle, au campement.")]
        public AudioClip fire;
        [Range(0f, 1f)] public float fireVolume = 1f;
        [Tooltip("Gain du feu au mixage (dB, avant le limiteur) : livré à −46 dB RMS ; +28 dB le placent à 5 dB environ sous la musique.")]
        [Range(0f, 40f)] public float fireGainDb = 28f;
        [Tooltip("Part du volume du feu sur l'écran titre (0 : absent).")]
        [Range(0f, 1f)] public float fireOnTitle;
        [Tooltip("Fondu du feu entre le titre et le campement (s).")]
        [Min(0.05f)] public float fireFadeSeconds = 1.2f;
    }
}
