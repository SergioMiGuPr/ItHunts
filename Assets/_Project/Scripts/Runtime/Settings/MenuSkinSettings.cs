using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Habillage des menus (lot B de la DA, UI-HANDOFF § 2) : polices, panneaux, fonds vivants de l'écran titre et du campement,
    /// réglages des animations (parallaxe, nuages, brume, fenêtres, feu), transition par la brume, places autour du foyer.
    /// Les morceaux actuels (découpes du lot B) seront remplacés par les scènes composées de la DA ; pièces manquantes : PROGRESS.
    /// </summary>
    [CreateAssetMenu(fileName = "MenuSkin_Default", menuName = "ItHunts/Menus/Habillage")]
    public sealed class MenuSkinSettings : ScriptableObject
    {
        public enum TitleStyle : byte
        {
            /// <summary>Grenze Gotisch SemiBold, plus grand, contour sombre.</summary>
            GothiqueContour,
            /// <summary>EB Garamond Bold en capitales espacées.</summary>
            GaramondEspace,
        }

        [System.Serializable]
        public sealed class CloudLayer
        {
            [Tooltip("Indice de la bande (Fond_Nuages_1 à 5).")]
            public int band;
            [Tooltip("Haut de la bande dans la toile (px, origine en haut).")]
            public float yTop;
            [Tooltip("Décalage horizontal (px de la toile) : le bord coupé hors de l'écran.")]
            public float x;
            [Range(0f, 1f)] public float opacity = 0.8f;
            [Tooltip("Vitesse de dérive (aller-retour, rad/s × 100) ; le signe donne le sens de départ.")]
            public float speed = 8f;
            [Tooltip("Amplitude de la dérive en aller-retour (px 1080p).")]
            public float drift = 70f;
            [Min(0.2f)] public float scale = 1f;
            public float parallax = 0.15f;
        }

        [Header("Polices (UI-HANDOFF : EB Garamond pour le texte, Grenze Gotisch pour les titres)")]
        public Font textFont;
        public Font textBold;
        public Font titleFont;
        [Tooltip("EB Garamond Bold (titre en capitales espacées).")]
        public Font titleBold;

        [Header("Titre (en attendant le logo de la DA)")]
        public TitleStyle titleStyle = TitleStyle.GothiqueContour;
        [Min(20)] public int titleSize = 145;
        [Tooltip("Tiers gauche de l'écran réservé au titre et au menu (part de la largeur).")]
        [Range(0.2f, 0.5f)] public float leftColumn = 0.34f;

        [Header("Panneaux (pièces du HUD et de la carte réutilisées)")]
        [Tooltip("Panneau en 9 tranches (cadre de la légende de la carte).")]
        public Sprite panel;
        [Tooltip("Repère du bouton qui a le focus (étoile de l'emblème).")]
        public Sprite focusMark;
        [Tooltip("Matériau additif de l'interface (ItHunts/UIAdditif).")]
        public Material additive;
        [Tooltip("Matériau des calques à fondu et à détourage érodé (ItHunts/UIFondu), copié par calque.")]
        public Material fade;

        [Header("Écran titre : calques (toile de 2752 × 1536)")]
        public Sprite sky;
        public Sprite moon, moonHalo;
        [Tooltip("Halo de la lune (le disque gris translucide) : coupé par défaut.")]
        public bool moonHaloOn;
        public Sprite[] clouds = new Sprite[0];
        public Sprite[] fog = new Sprite[0];
        public Sprite castle, castleWindows;
        [Tooltip("Coin haut gauche du château dans la toile (px, origine en haut à gauche).")]
        public Vector2 castleAt = new Vector2(1142f, 74f);
        [Tooltip("Érosion du détourage du château (texels) : cache le halo en attendant la couche propre de la DA.")]
        [Range(0f, 8f)] public float castleErode = 3f;
        [Tooltip("Centre de la lune dans la toile (0..1, origine en bas à gauche) et son échelle : à droite du titre.")]
        public Vector2 moonAt = new Vector2(0.47f, 0.8f);
        [Min(0.05f)] public float moonScale = 0.3f;
        [Tooltip("Couches de nuages (moins de bandes, espacements irréguliers, opacité et vitesse par couche).")]
        public CloudLayer[] cloudLayers =
        {
            new CloudLayer { band = 0, yTop = 50f, x = -260f, opacity = 0.5f, speed = 5f, drift = 50f, scale = 1.1f, parallax = 0.12f },
            new CloudLayer { band = 2, yTop = 300f, x = -120f, opacity = 0.72f, speed = -8f, drift = 80f, scale = 1.15f, parallax = 0.18f },
            new CloudLayer { band = 4, yTop = 520f, x = -200f, opacity = 0.9f, speed = 12f, drift = 110f, scale = 1.2f, parallax = 0.26f },
        };
        [Tooltip("Fondu en bas de chaque bande de nuages (part de sa hauteur) : cache le trait noir coupé.")]
        [Range(0f, 1f)] public float cloudFadeBottom = 0.35f;
        [Tooltip("Fondu en haut des bandes de brume (px de la toile, au moins 150 px à l'écran).")]
        [Min(0f)] public float fogFadeTop = 330f;

        [Header("Campement : effet de feu sur le foyer peint, places autour")]
        public Sprite campBackground;
        [Tooltip("Foyer peint dans le fond (bûches croisées), 0..1 dans la toile, origine en bas à gauche.")]
        public Vector2 hearthAt = new Vector2(0.6f, 0.205f);
        [Tooltip("Flammes : hauteur (px de la toile), largeur, nombre de langues.")]
        [Min(10f)] public float flameHeight = 300f;
        [Min(10f)] public float flameWidth = 190f;
        [Range(1, 8)] public int flameTongues = 5;
        [Tooltip("Lueur orange additive autour du foyer (diamètre en px de la toile) qui éclaire le décor.")]
        [Min(10f)] public float fireGlow = 1500f;
        [ColorUsage(false, true)] public Color fireColor = new Color(1.25f, 0.55f, 0.18f);
        [Tooltip("Braises : nombre, vitesse de montée (px/s de la toile), durée de vie (s).")]
        [Range(0, 80)] public int embers = 34;
        public float emberSpeed = 140f;
        public Vector2 emberLife = new Vector2(1.4f, 3.2f);
        [Tooltip("Places autour du foyer (0..1 dans la toile, aux pieds) ; J1, J2 devant, J3, J4 au fond.")]
        public Vector2[] seatAnchors = { new Vector2(0.4f, 0.12f), new Vector2(0.81f, 0.12f), new Vector2(0.45f, 0.31f), new Vector2(0.75f, 0.31f) };
        [Tooltip("Échelle de chaque place (perspective : les places du fond plus petites).")]
        public float[] seatScales = { 1f, 1f, 0.7f, 0.7f };
        [Tooltip("Hauteur d'un personnage à l'échelle 1 (px de la toile).")]
        [Min(20f)] public float figureHeight = 300f;
        [Tooltip("Places 3 et 4 (en ligne, plus tard) : affichées discrètement, ou cachées.")]
        public bool showOnlineSeats;

        [Header("Animations")]
        [Tooltip("Décalage maximal de la parallaxe (px 1080p) à 100 % ; chaque calque en prend sa part.")]
        [Min(0f)] public float parallaxPixels = 70f;
        public float skyParallax = 0.1f, moonParallax = 0.12f, castleParallax = 0.25f, farFogParallax = 0.4f, nearFogParallax = 0.8f;
        [Tooltip("Défilement de la brume (px 1080p par seconde).")]
        public float fogSpeed = 22f;
        [Tooltip("Fenêtres du château : couleur (HDR, additif), vitesse et profondeur du clignotement.")]
        [ColorUsage(false, true)] public Color windowColor = new Color(1.6f, 0.25f, 0.12f);
        [Min(0f)] public float windowFlickerSpeed = 2.5f;
        [Range(0f, 1f)] public float windowFlickerDepth = 0.55f;
        [Tooltip("Feu : vitesse et profondeur de la palpitation.")]
        [Min(0f)] public float fireFlickerSpeed = 6f;
        [Range(0f, 1f)] public float fireFlickerDepth = 0.3f;

        [Header("Transition titre ↔ campement : la brume monte, puis se dissipe")]
        [Min(0.05f)] public float fogRiseSeconds = 0.8f;
        [Min(0.05f)] public float fogClearSeconds = 0.8f;
        public Color transitionFog = new Color(0.36f, 0.42f, 0.55f, 1f);

        [Header("Musique d'ambiance (provisoire : nappe synthétisée si vide)")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
    }
}
