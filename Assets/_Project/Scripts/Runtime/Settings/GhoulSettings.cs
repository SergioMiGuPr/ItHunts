using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Goule (zone 1, D-033) : rampe très vite au sol, bondit sur le joueur (posture annoncée), blesse au contact ;
    /// attrapable seulement étourdie. Les murs viendront au bloc 5. Valeurs de départ : docs/design/features/goule/design.md.
    /// Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Ghoul_Default", menuName = "ItHunts/Ennemis/Goule")]
    public sealed class GhoulSettings : EnemySettings
    {
        [Header("Déplacement au sol")]
        [Tooltip("Vitesse de course (m/s). Course du joueur : 7.5 ; plafond des gains : 20.")]
        [Min(0f)] public float runSpeed = 10f;
        [Min(0f)] public float acceleration = 40f;
        [Tooltip("Vitesse de rotation (°/s).")]
        [Min(0f)] public float turnRate = 720f;
        [Tooltip("Recalcul du chemin (s).")]
        [Min(0.02f)] public float repathInterval = 0.2f;

        [Header("Perception (inutilisée depuis D-035 : elles savent toujours où sont les joueurs ; gardée pour les vagues)")]
        [Tooltip("Vue : portée (m) et angle total (°) ; elle repère aussi un joueur à cette distance par le bruit.")]
        [Min(0f)] public float sightRange = 30f;
        [Range(1f, 360f)] public float sightAngle = 160f;
        [Tooltip("Au-delà de cette distance (m) de son poste, elle abandonne et revient (vagues : 20 m hors de l'arène).")]
        [Min(0f)] public float leashDistance = 40f;
        [Tooltip("Sans voir le joueur pendant ce temps (s), elle retourne à son poste.")]
        [Min(0f)] public float forgetTime = 4f;

        [Header("Poursuite (D-035) : elles savent toujours où sont les joueurs")]
        [Tooltip("Elles vont vers le joueur le plus proche par le chemin ; recalcul de ce choix (s).")]
        [Min(0.1f)] public float targetInterval = 1f;
        [Tooltip("Aggro (D-061) : une goule en attente qui voit un joueur debout à moins de cette distance (m, ligne de vue), ou " +
                 "qui est à moins de aggroProximity (m) même sans le voir, se met en chasse ; sans limite de nombre. Les " +
                 "chasseuses (pression continue) sont choisies par les vagues (WaveSettings, chasseurs).")]
        [Min(0f)] public float aggroSightRange = 20f;
        [Min(0f)] public float aggroProximity = 8f;
        [Tooltip("Semée : plus aucun joueur à moins de cette distance (m) pendant ce temps (s) ; elle attend là où elle est.")]
        [Min(0f)] public float aggroLoseDistance = 50f;
        [Min(0f)] public float aggroLoseTime = 5f;
        [Tooltip("Encerclement : chacune vise un point décalé autour du joueur (rayon, m) tant qu'elle est loin.")]
        [Min(0f)] public float encircleRadius = 6f;
        [Tooltip("En dessous de cette distance (m), elle va droit sur le joueur.")]
        [Min(0f)] public float encircleDirectDistance = 10f;

        [Header("Murs (D-035) : grimper, courir, bondir")]
        [Tooltip("Vitesse sur un mur (m/s), en montant ou en courant en biais.")]
        [Min(0.1f)] public float climbSpeed = 7f;
        [Tooltip("Vitesse quand elle saute d'une hauteur pour redescendre (m/s).")]
        [Min(0.1f)] public float dropSpeed = 12f;
        [Tooltip("Joueur hors d'atteinte par le sol (plateforme, poutre) à moins de cette distance (m) : elle grimpe le mur le " +
                 "plus proche de lui (jusqu'à wallSearchRange de lui) et bondit depuis le mur.")]
        [Min(0f)] public float wallAssaultRange = 30f;
        [Min(1f)] public float wallSearchRange = 12f;
        [Tooltip("Depuis un mur, elle bondit sur le joueur à moins de cette distance (m) à l'horizontale (annonce leapTelegraph).")]
        [Min(0f)] public float wallLeapRange = 12f;
        [Tooltip("Accrochée en haut du mur sans pouvoir bondir (pas de ligne de vue) : elle redescend au bout de ce temps (s).")]
        [Min(0.5f)] public float wallHoldTime = 3f;

        [Header("Long trajet et déblocage (après le playtest des étapes 8 et 9)")]
        [Tooltip("Au-delà de cette distance (m) au joueur, elle avance par étapes (salle voisine suivante, MazeGraph) : le calcul " +
                 "de chemin d'Unity n'aboutit pas sur de très longues distances.")]
        [Min(20f)] public float longPathDistance = 100f;
        [Tooltip("En chasse, immobile plus de ce temps (s) alors que le joueur est loin : elle se replace sur le NavMesh le plus proche.")]
        [Min(0.5f)] public float unstuckTime = 2f;

        [Header("Contact")]
        [Tooltip("Dégâts au contact (PV), au plus une fois par intervalle (s).")]
        [Min(0f)] public float contactDamage = 15f;
        [Min(0.05f)] public float contactInterval = 1f;
        [Tooltip("Distance (m) entre sa surface et celle du joueur pour un contact.")]
        [Min(0f)] public float contactRange = 0.35f;

        [Header("Bond")]
        [Tooltip("Elle bondit quand le joueur est entre ces distances (m) et visible.")]
        [Min(0f)] public float leapMinRange = 2.5f;
        [Min(0f)] public float leapMaxRange = 6f;
        [Tooltip("Posture annoncée avant le bond (s), bien visible.")]
        [Min(0f)] public float leapTelegraph = 0.3f;
        [Tooltip("Vitesse horizontale du bond (m/s) et hauteur de l'arc (m).")]
        [Min(0f)] public float leapSpeed = 14f;
        [Min(0f)] public float leapHeight = 1f;
        [Tooltip("Recharge entre deux bonds (s).")]
        [Min(0f)] public float leapCooldown = 2.5f;
        [Tooltip("Dégâts du bond s'il touche (PV) et poussée légère donnée au joueur (m/s), jamais de ralentissement.")]
        [Min(0f)] public float leapDamage = 15f;
        [Min(0f)] public float leapPush = 4f;
        [Tooltip("Visée du bond : où sera le joueur dans ce temps (s).")]
        [Min(0f)] public float leapLead = 0.2f;

        [Header("Retour visuel")]
        [ColorUsage(false, true)] public Color telegraphColor = new Color(3f, 0.4f, 0.2f);
        [ColorUsage(false, true)] public Color eyeColor = new Color(0.45f, 0.25f, 0.05f);   // au repos : sous le seuil du bloom (correctifs-rendu point 5), rouge vif pendant l'annonce seulement

        [Header("Signal : lueur pendant l'annonce du bond (bloc 4)")]
        [Tooltip("Yeux et gueule pendant l'annonce du bond (HDR, au-delà du seuil du bloom).")]
        [ColorUsage(false, true)] public Color leapGlowColor = new Color(4f, 0.25f, 0.12f);
        [Tooltip("Teinte rouge du corps pendant l'annonce (0 : aucune, 1 : rouge plein).")]
        [Range(0f, 1f)] public float leapBodyTint = 0.55f;

        [Header("Lueur des yeux et de la gueule (modèle de la DA, masque d'émission ; retours du lot, point 3)")]
        [Tooltip("Rouge sombre et discret, sous le seuil du bloom : jamais un rouge vif. Le signal du bond est la gueule, le disque au " +
                 "sol et une légère montée de cette lueur.")]
        [ColorUsage(false, true)] public Color eyeGlowColor = new Color(0.55f, 0.05f, 0.03f);
        [Tooltip("Intensité de la lueur au repos (× la couleur).")]
        [Range(0f, 2f)] public float eyeGlowRest = 0.45f;
        [Tooltip("Intensité pendant l'annonce du bond (× la couleur) : une légère montée.")]
        [Range(0f, 2f)] public float eyeGlowAnnounce = 1f;
    }
}
