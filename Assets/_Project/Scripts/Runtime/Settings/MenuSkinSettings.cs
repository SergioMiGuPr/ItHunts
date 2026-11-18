using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Habillage des menus (lot B de la DA, UI-HANDOFF § 2) : polices, panneaux, fonds vivants de l'écran titre et du campement,
    /// réglages des animations (parallaxe, nuages, brume, fenêtres, feu), glissement vers le campement, places autour du feu.
    /// Pièces manquantes (logo, boutons, cadre du choix du personnage, personnages 2D) : habillage provisoire, voir PROGRESS.
    /// </summary>
    [CreateAssetMenu(fileName = "MenuSkin_Default", menuName = "ItHunts/Menus/Habillage")]
    public sealed class MenuSkinSettings : ScriptableObject
    {
        [Header("Polices (UI-HANDOFF : EB Garamond pour le texte, Grenze Gotisch pour les titres)")]
        public Font textFont;
        public Font textBold;
        public Font titleFont;

        [Header("Panneaux (pièces du HUD et de la carte réutilisées)")]
        [Tooltip("Panneau en 9 tranches (cadre de la légende de la carte).")]
        public Sprite panel;
        [Tooltip("Repère du bouton qui a le focus (étoile de l'emblème).")]
        public Sprite focusMark;
        [Tooltip("Matériau additif de l'interface (ItHunts/UIAdditif) : lune, brume, feu, fenêtres.")]
        public Material additive;

        [Header("Écran titre : calques (toile de 2752 × 1536)")]
        public Sprite sky;
        public Sprite moon, moonHalo;
        public Sprite[] clouds = new Sprite[0];
        public Sprite[] fog = new Sprite[0];
        public Sprite castle, castleWindows;
        [Tooltip("Coin haut gauche du château dans la toile (px, origine en haut à gauche).")]
        public Vector2 castleAt = new Vector2(1142f, 74f);
        [Tooltip("Centre de la lune dans la toile (0..1, origine en bas à gauche) et son échelle.")]
        public Vector2 moonAt = new Vector2(0.3f, 0.78f);
        [Min(0.05f)] public float moonScale = 0.42f;

        [Header("Campement")]
        public Sprite campBackground;
        public Sprite fire, fireAdditive;
        [Tooltip("Sièges : tronc, pierre, banc, rocher (même hauteur de toile).")]
        public Sprite[] seats = new Sprite[0];
        [Tooltip("Places autour du feu (0..1 dans la toile, pied du siège) ; la 1re est celle du joueur 1.")]
        public Vector2[] seatAnchors = { new Vector2(0.34f, 0.12f), new Vector2(0.66f, 0.12f), new Vector2(0.13f, 0.2f), new Vector2(0.87f, 0.2f) };
        [Min(0.05f)] public float seatScale = 0.42f;
        public Vector2 fireAt = new Vector2(0.5f, 0.14f);
        [Min(0.05f)] public float fireScale = 0.26f;

        [Header("Animations")]
        [Tooltip("Décalage maximal de la parallaxe (px 1080p) à 100 % ; chaque calque en prend sa part.")]
        [Min(0f)] public float parallaxPixels = 70f;
        [Tooltip("Parallaxe par calque (part) : ciel, lune, nuages (proches / lointains), château, brume (lointaine / proche).")]
        public float skyParallax = 0.1f, moonParallax = 0.12f, farCloudParallax = 0.15f, nearCloudParallax = 0.25f, castleParallax = 0.25f,
            farFogParallax = 0.4f, nearFogParallax = 0.8f;
        [Tooltip("Défilement des nuages et de la brume (px 1080p par seconde).")]
        public float cloudSpeed = 9f, fogSpeed = 22f;
        [Tooltip("Fenêtres du château : couleur (HDR, additif), vitesse et profondeur du clignotement.")]
        [ColorUsage(false, true)] public Color windowColor = new Color(1.6f, 0.25f, 0.12f);
        [Min(0f)] public float windowFlickerSpeed = 2.5f;
        [Range(0f, 1f)] public float windowFlickerDepth = 0.55f;
        [Tooltip("Feu et lanternes : vitesse et profondeur de la palpitation.")]
        [Min(0f)] public float fireFlickerSpeed = 6f;
        [Range(0f, 1f)] public float fireFlickerDepth = 0.25f;
        [Tooltip("Halo de la lune : pulsation (Hz) et profondeur.")]
        public float moonPulseHz = 0.15f;
        [Range(0f, 1f)] public float moonPulseDepth = 0.2f;

        [Header("Jouer : la caméra glisse vers le campement")]
        [Min(0.1f)] public float glideSeconds = 1.5f;
        [Tooltip("Courbe du glissement (0 → 1).")]
        public AnimationCurve glideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Musique d'ambiance (provisoire : nappe synthétisée si vide)")]
        public AudioClip music;
        [Range(0f, 1f)] public float musicVolume = 0.35f;
    }
}
