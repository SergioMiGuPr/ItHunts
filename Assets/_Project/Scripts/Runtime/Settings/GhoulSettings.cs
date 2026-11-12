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
        [Tooltip("Goules en chasse au même moment, au plus (D-037) ; les autres attendent et prennent le relais quand une goule " +
                 "meurt (la plus proche du joueur par le chemin).")]
        [Min(0)] public int maxHunting = 25;
        [Tooltip("Aggro (D-038) : une goule en attente qui voit un joueur à moins de cette distance (m, ligne de vue), ou qui est " +
                 "à moins de aggroProximity (m) même sans le voir, se met en chasse en plus des maxHunting, jusqu'à maxHuntingTotal.")]
        [Min(0f)] public float aggroSightRange = 20f;
        [Min(0f)] public float aggroProximity = 8f;
        [Min(0)] public int maxHuntingTotal = 40;
        [Tooltip("Une goule réveillée par l'aggro retourne en attente si le joueur est à plus de cette distance (m) pendant ce " +
                 "temps (s), sauf si une place se libère parmi les maxHunting (elle la prend alors).")]
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
        [ColorUsage(false, true)] public Color eyeColor = new Color(2.5f, 1.4f, 0.3f);
    }
}
