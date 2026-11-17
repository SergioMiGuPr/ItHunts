using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Habillage du HUD livré par la DA (PR #10, docs/art/ui/UI-HANDOFF.md) : barre de vie, cartouche des âmes, mini-carte ronde
    /// et ses icônes, polices. Les mesures (coins, échelle k, canal de la barre) sont celles de la scène d'essai de la DA
    /// (Test_UI_HUD, 1920 × 1080) ; tout est réglable en Play Mode. Vide : le HUD greybox d'avant.
    /// </summary>
    [CreateAssetMenu(fileName = "HudSkin_Default", menuName = "ItHunts/Réglages/Habillage du HUD")]
    public sealed class HudSkinSettings : ScriptableObject
    {
        [Header("Polices (OFL)")]
        [Tooltip("Texte et nombres : EB Garamond SemiBold.")]
        public Font textFont;
        [Tooltip("Titres et messages de zone : Grenze Gotisch.")]
        public Font titleFont;
        [Tooltip("Contour sombre des nombres (UI-HANDOFF : 1.5 px, #0A050A à 90 %).")]
        public Color numberOutline = new Color(0.04f, 0.02f, 0.04f, 0.9f);
        [Min(0f)] public float numberOutlineWidth = 1.5f;

        [Header("Barre de vie (coin bas gauche)")]
        public Sprite healthFrame;
        public Sprite healthBack;
        public Sprite healthFill;
        public Sprite healthTrail;
        [Tooltip("Coin bas gauche (px en 1080p), échelle k des bordures, longueur (px).")]
        public Vector2 healthCorner = new Vector2(32f, 32f);
        [Range(0.3f, 2f)] public float healthScale = 0.9f;
        [Min(100f)] public float healthLength = 440f;
        [Tooltip("Canal du remplissage dans le cadre natif (px 1080p) : gauche, droite, haut, hauteur.")]
        public Vector4 healthChannel = new Vector4(100f, 26f, 54f, 23f);
        [Tooltip("Hauteur native du cadre (px 1080p).")]
        [Min(1f)] public float healthNativeHeight = 131f;
        [Tooltip("Traînée des dégâts récents : elle rattrape la valeur en ce temps (s), après ce délai (s).")]
        [Min(0.01f)] public float trailCatchUp = 0.4f;
        [Min(0f)] public float trailDelay = 0.25f;
        [Min(6)] public int healthFontSize = 20;
        public Color healthTextColor = new Color(0.92f, 0.88f, 0.86f);

        [Header("Cartouche des âmes (coin haut gauche)")]
        public Sprite soulsFrame;
        public Sprite soulsBack;
        public Sprite soulsFlame;
        public Vector2 soulsCorner = new Vector2(32f, 32f);
        [Range(0.3f, 2f)] public float soulsScale = 0.75f;
        [Min(100f)] public float soulsLength = 250f;
        [Min(1f)] public float soulsNativeHeight = 102f;
        [Tooltip("Rond de l'icône dans le cadre natif (px 1080p) : centre x, centre y depuis le haut, diamètre ; début du nombre.")]
        public Vector4 soulsEmblem = new Vector4(58f, 51f, 51f, 86f);
        [Min(6)] public int soulsFontSize = 30;
        public Color soulsTextColor = new Color(0.84f, 0.88f, 0.92f);
        [Tooltip("Marge à droite du nombre d'âmes (px natifs) : le nombre est centré entre l'emblème et le bout du cartouche.")]
        [Min(0f)] public float soulsTextRightInset = 28f;

        [Header("Mini-carte ronde (coin haut droit)")]
        public Sprite minimapFrame;
        public Sprite minimapMask;
        public Sprite minimapBack;
        public Vector2 minimapCorner = new Vector2(32f, 32f);
        [Range(0.3f, 2f)] public float minimapScale = 0.85f;
        [Tooltip("Taille native du cadre (px 1080p), centre et rayon de la zone visible.")]
        public Vector2 minimapNativeSize = new Vector2(281f, 295f);
        public Vector3 minimapCircle = new Vector3(140f, 143f, 96f);
        [Tooltip("Mètres visibles dans le rond (diamètre).")]
        [Min(20f)] public float minimapMetres = 200f;

        [Header("Icônes de la mini-carte (taille en px 1080p)")]
        public Sprite iconArrow;
        public Sprite iconChest;
        public Sprite iconChestOpen;
        [Tooltip("Coffre rare (coffres des parcours) : MiniCarte_Coffre_Rare.")]
        public Sprite iconChestRare;
        public Sprite iconMerchant;
        public Sprite iconSouls;
        public Sprite iconExitLocked;
        public Sprite iconExitOpen;
        public Sprite iconAllyDead;
        [Tooltip("Pastilles des alliés par personnage : renard, koala, caméléon, hibou.")]
        public Sprite allyFox, allyKoala, allyChameleon, allyOwl;
        [Min(4f)] public float iconSize = 22f;
        [Min(4f)] public float allySize = 16f;
        [Min(4f)] public float arrowSize = 24f;
        [Tooltip("Icônes de la carte ouverte (Tab), px 1080p.")]
        [Min(4f)] public float mapIconSize = 30f;
    }
}
