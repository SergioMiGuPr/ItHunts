using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Volant (D-033, D-040) : petit ennemi qui tourne au-dessus du joueur et pique sur lui ; attrapable à tout moment comme
    /// une caisse de 8 kg, il se débat et se libère au bout de 2 s ; lancé, il éclate à l'impact (zone de 3 m) et meurt ;
    /// repoussé, il tombe au sol étourdi. Valeurs de départ : docs/design/features/volant/design.md. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Flyer_Default", menuName = "ItHunts/Ennemis/Volant")]
    public sealed class FlyerSettings : EnemySettings
    {
        [Header("Vol")]
        [Tooltip("Vitesse en ronde (m/s) et accélération (m/s²).")]
        [Min(0f)] public float flySpeed = 6f;
        [Min(0f)] public float acceleration = 20f;
        [Tooltip("Ronde : hauteur au-dessus du joueur (m) et rayon (m). Sous un plafond bas, il vole 1.5 m sous le plafond.")]
        [Min(1f)] public float orbitHeight = 8f;
        [Min(1f)] public float orbitRadius = 8f;
        [Tooltip("Vitesse pour remonter ou rejoindre le joueur de loin (m/s).")]
        [Min(0f)] public float travelSpeed = 9f;
        [Tooltip("Rayon de sa sphère de collision (m), pour éviter le décor.")]
        [Min(0.1f)] public float radius = 0.4f;

        [Header("Fragile (D-040)")]
        [Tooltip("Meurt au premier impact de projectile, quels que soient les dégâts : balle, objet lancé ou repoussé, ennemi " +
                 "lancé, dégâts contre un mur. L'onde de répulsion seule ne le tue pas (il tombe étourdi), une zone non plus.")]
        public bool dieOnProjectileHit = true;

        [Header("Piqué")]
        [Tooltip("Il pique quand le joueur est en ligne de vue à moins de cette distance (m) et que sa recharge est finie.")]
        [Min(0f)] public float diveRange = 16f;
        [Tooltip("Annonce (s, cri et lueur), vitesse du piqué (m/s), durée maximale (s).")]
        [Min(0f)] public float diveTelegraph = 0.4f;
        [Min(0f)] public float diveSpeed = 14f;
        [Min(0.1f)] public float diveMaxTime = 1.5f;
        [Tooltip("Pendant le piqué, il corrige sa visée vers l'endroit où sera le joueur, en tournant au plus de cet angle par " +
                 "seconde (°/s) ; 0 : piqué en ligne droite. Il abandonne quand le joueur passe derrière lui.")]
        [Min(0f)] public float diveTurnRate = 80f;
        [Tooltip("Part de l'avance prise sur le joueur en mouvement (0 : il vise où est le joueur ; 1 : où il sera à l'arrivée).")]
        [Range(0f, 1f)] public float diveLead = 0f;
        [Tooltip("Recharge entre deux piqués (s), aussi après une libération (il remonte d'abord en ronde).")]
        [Min(0f)] public float diveCooldown = 3f;
        [Tooltip("Coup du piqué (PV) et poussée légère (m/s), jamais de ralentissement ; distance de contact (m).")]
        [Min(0f)] public float diveDamage = 8f;
        [Min(0f)] public float divePush = 3f;
        [Min(0f)] public float contactRange = 0.3f;

        [Header("Lancé : il éclate (D-040)")]
        [Tooltip("Zone de l'éclatement (m) : dégâts au centre, part au bord ; jamais à travers un mur ; tir ami appliqué.")]
        [Min(0f)] public float burstRadius = 3f;
        [Min(0f)] public float burstDamage = 30f;
        [Range(0f, 1f)] public float burstEdgeShare = 0.25f;
        [Tooltip("Vitesse d'impact minimale pour éclater (m/s).")]
        [Min(0f)] public float burstMinSpeed = 4f;

        [Header("Poursuite (D-040)")]
        [Tooltip("Volants en chasse au même moment ; les autres attendent et prennent le relais (le plus proche).")]
        [Min(0)] public int maxHunting = 8;
        [Tooltip("Aggro des volants en attente : vue (m, ligne de vue), proximité (m) ; total en chasse au plus.")]
        [Min(0f)] public float aggroSightRange = 20f;
        [Min(0f)] public float aggroProximity = 8f;
        [Min(0)] public int maxHuntingTotal = 12;
        [Tooltip("Réveillé par l'aggro : se rendort si le joueur reste au-delà de cette distance (m) pendant ce temps (s).")]
        [Min(0f)] public float aggroLoseDistance = 50f;
        [Min(0f)] public float aggroLoseTime = 5f;

        [Header("Retour visuel")]
        [ColorUsage(false, true)] public Color eyeColor = new Color(3f, 2f, 0.4f);
        [ColorUsage(false, true)] public Color telegraphColor = new Color(4f, 0.4f, 0.2f);

        public override bool DiesOnProjectileHit => dieOnProjectileHit;

        /// <summary>Valeurs du volant sur la base commune (PV, masse, étourdissement).</summary>
        private void Reset()
        {
            maxHealth = 25f;
            respawnDelay = 0f;
            mass = 8f;
            stunDuration = 1.5f;
            stunImpactSpeed = 6f;
        }
    }
}
