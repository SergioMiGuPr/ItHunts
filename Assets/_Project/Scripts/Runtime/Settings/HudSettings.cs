using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// HUD (uGUI, un canvas sur la caméra de chaque joueur) : disposition autour du viseur, coins réservés, HUD discret
    /// (jauges pleines estompées), clignotements. Tailles en pixels pour une hauteur de référence de 1080. Valeurs de
    /// départ : docs/design/features/vue-fps/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Hud_Default", menuName = "ItHunts/Réglages/HUD")]
    public sealed class HudSettings : ScriptableObject
    {
        [Header("Habillage de la DA (PR #10) : barre de vie, âmes, mini-carte ronde, polices ; vide : HUD greybox")]
        public HudSkinSettings skin;

        [Header("Échelle")]
        [Tooltip("Résolution de référence (le HUD suit la hauteur de la zone de chaque joueur).")]
        public Vector2 referenceResolution = new Vector2(1920f, 1080f);
        [Min(0.1f)] public float scale = 1f;
        [Tooltip("Largeur minimale de mise en page (px à l'échelle 1) : une vue plus étroite (écran partagé côte à côte, 4:3) réduit le HUD " +
                 "pour que les coins tiennent. 0 : seule la hauteur compte.")]
        [Min(0f)] public float minLayoutWidth = 1100f;

        [Header("Timer de la zone")]
        [Min(8)] public int timerFontSize = 56;
        [Tooltip("Marge de sécurité sur les bords (part de la hauteur) : coins réservés.")]
        [Range(0f, 0.2f)] public float safeMargin = 0.05f;

        [Header("Viseur")]
        public Color crosshairColor = new Color(1f, 1f, 1f, 0.85f);
        [Min(1f)] public float crosshairSize = 4f;

        [Header("Jauges sous le viseur")]
        [Min(1f)] public float segmentWidth = 22f;
        [Min(1f)] public float segmentHeight = 6f;
        [Min(0f)] public float segmentGap = 4f;
        [Min(0f)] public float rowGap = 6f;
        [Min(0f)] public float groupGap = 14f;
        public float offsetBelowCrosshair = 26f;
        public Color movementColor = new Color(0.35f, 0.85f, 1f, 0.95f);
        public Color emptyColor = new Color(0f, 0f, 0f, 0.55f);
        [Tooltip("Opacité d'un segment en recharge (partie remplie).")]
        [Range(0f, 1f)] public float rechargeAlpha = 0.45f;
        [Min(6)] public int timeFontSize = 14;

        [Header("HUD discret")]
        [Tooltip("Une jauge pleine et inutilisée s'estompe à cette opacité…")]
        [Range(0f, 1f)] public float idleAlpha = 0.4f;
        [Tooltip("… après ce temps (s) …")]
        [Min(0f)] public float idleDelay = 1.5f;
        [Tooltip("… à cette vitesse (opacité par seconde). Toute utilisation, tout refus, toute recharge : 100 % tout de suite.")]
        [Min(0.01f)] public float fadeSpeed = 1.5f;
        [Tooltip("Une charge qui revient clignote une fois (s) ; luminosité du clignotement.")]
        [Min(0.01f)] public float returnBlinkDuration = 0.2f;
        [Min(1f)] public float returnBlinkBrightness = 1.8f;

        [Header("Artefact (barillet, D-032) : munitions sous les jauges de l'orbe")]
        [Min(1f)] public float ammoSegmentWidth = 8f;
        public Color ammoColor = new Color(1f, 0.75f, 0.35f, 0.95f);
        [Tooltip("Munitions quand le barillet est inactif (l'orbe tient un objet).")]
        public Color ammoInactiveColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);

        [Header("PV du joueur (tir ami, provisoire) : coin bas gauche, cachés quand ils sont pleins")]
        public Vector2 playerHealthSize = new Vector2(220f, 10f);
        public Color playerHealthColor = new Color(0.9f, 0.25f, 0.25f, 0.95f);
        public Color friendlyHitColor = new Color(1f, 0.45f, 0.85f, 1f);

        [Header("Chiffres de dégâts, barres de PV, durabilité")]
        [Min(6)] public int damageFontMin = 18;
        [Min(6)] public int damageFontMax = 40;
        [Min(0)] public int damageNumberPool = 24;
        [Min(0)] public int healthBarPool = 12;
    }
}
