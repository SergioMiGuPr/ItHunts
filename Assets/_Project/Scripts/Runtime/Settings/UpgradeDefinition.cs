using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>Effet d'une amélioration (étape 7, D-043).</summary>
    public enum UpgradeKind
    {
        ChargeDeMouvement,      // +amount charges de mouvement
        RechargeTraction,       // −amount s sur la recharge d'une charge d'attraction
        RayonDeCollecte,        // rayon de collecte des âmes × (1 + amount)
        DegatsObjetsLances,     // dégâts des objets lancés ou repoussés × (1 + amount)
        DegatsRepulsion,        // dégâts directs de l'onde de répulsion : amount PV (D-008)
        PvMax,                  // +amount PV max (et autant de PV tout de suite)
        BallesBarillet,         // +amount balles au barillet
        AspirationAmes,         // vitesse d'aspiration des âmes × (1 + amount)
        // Améliorations de l'artefact (barillet), vendues par le marchand de la salle de départ (D-050) ; provisoires.
        DegatsBarillet,         // dégâts d'une balle × (1 + amount)
        CadenceBarillet,        // intervalle entre deux tirs × (1 − amount)
        RechargementBarillet,   // temps de rechargement × (1 − amount)
        PorteeBarillet,         // portée + amount m
        // Améliorations de la lanterne de feu (renard, D-065), au même marchand ; provisoires.
        PorteeLanterne,         // portée du jet + amount m
        ConeLanterne,           // ouverture du cône + amount °
        RechargeLanterne,       // recharge entre deux jets − amount s
        BrulureLanterne,        // brûlure : amount PV/s pendant la durée de brûlure de la lanterne (3 s)
    }

    /// <summary>
    /// Une amélioration (étape 7, D-043) : nom, description d'une ligne, icône (symbole court et couleur, en greybox), effet et
    /// valeur, prix au marchand. Un asset par amélioration ; elles se cumulent si on en prend plusieurs fois.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade", menuName = "ItHunts/Amélioration")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        public string displayName = "Amélioration";
        [Tooltip("Une ligne, lisible sur l'orbe de choix.")]
        public string description = "";
        [Tooltip("Icône greybox : symbole court affiché sur l'orbe, et couleur de l'orbe.")]
        public string iconSymbol = "+";
        [ColorUsage(false, true)] public Color color = new Color(1f, 0.8f, 0.3f);
        public UpgradeKind kind;
        public float amount = 1f;
        [Tooltip("Prix chez le marchand (âmes).")]
        [Min(0)] public int merchantPrice = 60;
    }
}
