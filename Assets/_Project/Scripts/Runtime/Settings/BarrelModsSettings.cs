using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Headshots et améliorations avancées du caméléon (barillet), nuit 3 point 6. Inspirées des roguelites (Risk of Rain 2 : effets
    /// qui se cumulent, chances hyperboliques, coefficients de déclenchement ; Gunfire Reborn, Roboquest : perforation, ricochet,
    /// rechargement sur kill). Chaque pile ajoute son effet ; les combinaisons se renforcent (explosion qui tue → rechargement, critique
    /// en chaîne sur les ricochets…). Tout est réglable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "BarrelMods_Default", menuName = "ItHunts/Réglages/Barillet : headshots et améliorations")]
    public sealed class BarrelModsSettings : ScriptableObject
    {
        [Header("Headshots")]
        [Tooltip("Dégâts d'une balle dans la tête (goule : os Tete ; volant : entre les yeux) × ce facteur.")]
        [Min(1f)] public float headshotMultiplier = 2f;
        [Tooltip("Rayon de la zone de tête de la goule (m, monde) et du volant.")]
        [Min(0.05f)] public float ghoulHeadRadius = 0.3f;
        [Min(0.05f)] public float flyerHeadRadius = 0.32f;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        /// <summary>Rayons des zones de tête (posées à l'apparition des ennemis).</summary>
        public void Apply()
        {
            ItHunts.Combat.Hitbox.GhoulHeadRadius = ghoulHeadRadius;
            ItHunts.Combat.Hitbox.FlyerHeadRadius = flyerHeadRadius;
        }

        [Header("Balles paralysantes : chaque touche fige l'ennemi")]
        [Tooltip("Durée de la paralysie par pile (s).")]
        [Min(0f)] public float paralysisPerStack = 0.35f;

        [Header("Explosion sur kill : la balle qui tue fait exploser l'ennemi")]
        [Min(0f)] public float explosionRadius = 3.5f;
        [Min(0f)] public float explosionRadiusPerStack = 1f;
        [Min(0f)] public float explosionDamage = 16f;
        [Min(0f)] public float explosionDamagePerStack = 8f;
        [Tooltip("Coefficient de déclenchement de l'explosion (une explosion qui tue ne relance pas d'explosion au-delà de ce nombre).")]
        [Range(0, 5)] public int explosionChainMax = 2;

        [Header("Perforation : la balle traverse")]
        [Tooltip("Ennemis traversés en plus par pile ; dégâts gardés à chaque traversée.")]
        [Min(0)] public int piercePerStack = 1;
        [Range(0.1f, 1f)] public float pierceDamageKept = 0.75f;

        [Header("Ricochet : la balle rebondit vers l'ennemi le plus proche")]
        [Min(0)] public int ricochetsPerStack = 1;
        [Min(1f)] public float ricochetRange = 14f;
        [Range(0.1f, 1f)] public float ricochetDamage = 0.6f;

        [Header("Rechargement sur kill")]
        [Tooltip("Balles remises dans le barillet à chaque kill, par pile.")]
        [Min(0)] public int roundsPerKillPerStack = 2;

        [Header("Critiques en chaîne : chaque critique augmente le suivant")]
        [Tooltip("Bonus au multiplicateur des critiques par critique enchaîné, par pile (×2 → ×2.5 → ×3…).")]
        [Min(0f)] public float chainBonusPerStack = 0.5f;
        [Tooltip("Temps (s) pour enchaîner le critique suivant.")]
        [Min(0.1f)] public float chainWindow = 2.5f;
        [Range(1, 10)] public int chainMax = 4;

        [Header("Œil du caméléon : chance de critique sur tout le corps")]
        [Tooltip("Chance hyperbolique : 1 − 1 / (1 + k · piles).")]
        [Min(0f)] public float critChanceK = 0.12f;

        [Header("Balles incendiaires : la touche enflamme")]
        [Min(0f)] public float burnDpsPerStack = 5f;
        [Min(0f)] public float burnDuration = 3f;
    }
}
