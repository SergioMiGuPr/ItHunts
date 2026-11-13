using UnityEngine;

namespace ItHunts.Settings
{
    /// <summary>
    /// Âmes et économie (étape 7, D-043) : éclats d'âme, collecte autour de l'orbe, coffres, marchand, salle verrouillée,
    /// interaction (E / Y). Valeurs de départ validées le 2026-11-13. Modifiable en Play Mode.
    /// </summary>
    [CreateAssetMenu(fileName = "Economy_Default", menuName = "ItHunts/Économie")]
    public sealed class EconomySettings : ScriptableObject
    {
        [Header("Éclats d'âme")]
        [Tooltip("Durée de vie d'un éclat au sol (s) ; il clignote pendant les dernières secondes, puis disparaît (perdu).")]
        [Min(1f)] public float soulLifetime = 30f;
        [Min(0f)] public float soulBlinkTime = 5f;
        [Tooltip("L'orbe attire les éclats de son joueur à moins de ce rayon (m), à cette vitesse (m/s) ; ramassé à ce contact (m).")]
        [Min(0f)] public float collectRadius = 8f;
        [Min(0.1f)] public float pullSpeed = 25f;
        [Min(0.05f)] public float collectDistance = 0.7f;
        [Tooltip("Coop (D-007) : chaque joueur à moins de ce rayon de l'ennemi tué reçoit ses propres éclats (m).")]
        [Min(0f)] public float shareRadius = 40f;
        [Tooltip("Mort sans joueur : des âmes seulement si un joueur y est pour quelque chose dans ce délai (s).")]
        [Min(0f)] public float involvementWindow = 5f;
        [Tooltip("Multiplicateur d'âmes en Enragé (D-009), appliqué au moment où l'ennemi meurt.")]
        [Min(1f)] public float enragedMultiplier = 2f;
        [Tooltip("Éclats projetés à la mort : vitesse (m/s) ; taille (m) ; couleur (HDR, lueur).")]
        [Min(0f)] public float scatterSpeed = 4f;
        [Min(0.05f)] public float soulSize = 0.25f;
        [ColorUsage(false, true)] public Color soulColor = new Color(0.4f, 2.6f, 3.2f);

        [Header("Prix")]
        [Min(0)] public int chestPrice = 30;
        [Min(0)] public int lockedDoorPrice = 50;
        [Tooltip("Soin du marchand : prix (âmes) et PV rendus.")]
        [Min(0)] public int healPrice = 35;
        [Min(0f)] public float healAmount = 40f;
        [Tooltip("Hausse du prix à chaque achat (%, composée, par joueur) : coffres, marchand, portes. 0 : prix fixes.")]
        [Min(0f)] public float chestPriceIncrease;
        [Min(0f)] public float merchantPriceIncrease;
        [Min(0f)] public float doorPriceIncrease;

        public float IncreaseFor(ItHunts.Economy.PriceKind kind) => kind switch
        {
            ItHunts.Economy.PriceKind.Coffre => chestPriceIncrease,
            ItHunts.Economy.PriceKind.Marchand => merchantPriceIncrease,
            _ => doorPriceIncrease
        };

        [Header("Coffres, marchand, interaction")]
        [Tooltip("Choix d'amélioration : nombre d'orbes au-dessus d'un coffre ouvert ; elles restent ce temps si personne ne choisit (s).")]
        [Min(1)] public int choiceCount = 3;
        [Min(1f)] public float choiceLifetime = 30f;
        [Tooltip("Offres d'amélioration du marchand (plus le soin).")]
        [Min(0)] public int merchantOffers = 3;
        [Tooltip("Portée de l'interaction (E / Y) sur un coffre, une porte, le marchand (m) ; sur une orbe de choix ou d'offre (m).")]
        [Min(0.5f)] public float interactRange = 4f;
        [Min(0.5f)] public float orbInteractRange = 8f;
        [Tooltip("Tolérance de visée de l'interaction (°).")]
        [Min(0f)] public float interactAngle = 12f;
    }
}
