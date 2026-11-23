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
        [Tooltip("Une orbe attire les éclats (communs) à moins de ce rayon (m), à cette vitesse (m/s) ; ramassé à ce contact (m) : " +
                 "chaque joueur vivant reçoit alors 1 âme (D-007 révisée).")]
        [Min(0f)] public float collectRadius = 8f;
        [Min(0.1f)] public float pullSpeed = 25f;
        [Min(0.05f)] public float collectDistance = 0.7f;
        [Tooltip("Mort sans joueur : des âmes seulement si un joueur y est pour quelque chose dans ce délai (s).")]
        [Min(0f)] public float involvementWindow = 5f;
        [Tooltip("Multiplicateur d'âmes en Enragé (D-009), appliqué au moment où l'ennemi meurt.")]
        [Min(1f)] public float enragedMultiplier = 2f;
        [Tooltip("Éclats projetés à la mort : vitesse (m/s) ; taille (m) ; couleur (HDR, lueur).")]
        [Min(0f)] public float scatterSpeed = 4f;
        [Min(0.05f)] public float soulSize = 0.25f;
        [ColorUsage(false, true)] public Color soulColor = new Color(0.4f, 2.6f, 3.2f);
        [Tooltip("Halo des éclats (bloc 4) : diamètre (m) et part de la couleur de l'éclat ; lueur additive qui traverse le brouillard.")]
        [Min(0f)] public float soulHaloSize = 1.1f;
        [Min(0f)] public float soulHaloIntensity = 0.35f;
        public Material soulHaloMaterial;

        [Header("Prix")]
        [Min(0)] public int chestPrice = 30;
        [Min(0)] public int lockedDoorPrice = 50;
        [Tooltip("Soin du marchand : prix (âmes) et PV rendus.")]
        [Min(0)] public int healPrice = 35;
        [Min(0f)] public float healAmount = 40f;
        [Tooltip("Hausse du prix à chaque achat (% du prix de base, ajoutée par achat, par joueur) : coffres, marchand, portes. 0 : prix fixes. " +
                 "Nuit 4 (D-100) : +10 % par achat au marchand, plafonnée à ×2 (avant : +25 % composés, qui donnait 7 000 à 16 000 âmes).")]
        [Min(0f)] public float chestPriceIncrease;
        [Min(0f)] public float merchantPriceIncrease = 10f;
        [Min(0f)] public float doorPriceIncrease;
        [Tooltip("Plafond de la hausse (× le prix de base) ; 0 : sans plafond.")]
        [Min(0f)] public float priceIncreaseCap = 2f;

        public float IncreaseFor(ItHunts.Economy.PriceKind kind) => kind switch
        {
            ItHunts.Economy.PriceKind.Coffre => chestPriceIncrease,
            ItHunts.Economy.PriceKind.Marchand => merchantPriceIncrease,
            _ => doorPriceIncrease
        };

        [Header("Améliorations à rangs (D-100)")]
        [Tooltip("Prix d'une amélioration selon le rang visé : nouvelle (Bronze) 60 ; montée vers Argent 90, Or 140, Diamant 220, Mythique 350.")]
        public int[] rankPrices = { 60, 90, 140, 220, 350 };
        [Tooltip("Acte (0 = acte I) à partir duquel un coffre peut proposer une montée de rang (Œil du collectionneur Or : dès l'acte I).")]
        [Min(0)] public int chestRankUpFromAct = 1;

        /// <summary>Prix d'une offre qui mène au rang donné (0 Bronze = nouvelle).</summary>
        public int RankPrice(int targetRank)
        {
            if (rankPrices == null || rankPrices.Length == 0)
                return 60;
            return rankPrices[Mathf.Clamp(targetRank, 0, rankPrices.Length - 1)];
        }

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

        [Header("Coffre rare (D-071) : coffres des parcours, difficiles d'accès, meilleur butin")]
        [Tooltip("Table de butin dédiée aux coffres rares (vide : la table de la run).")]
        public UpgradePool rarePool;
        [Tooltip("Choix en plus au-dessus d'un coffre rare.")]
        [Min(0)] public int rareExtraChoices = 1;
        [Tooltip("Bonus de rareté : la valeur de chaque amélioration tirée × ce facteur (entiers arrondis au-dessus, réductions plafonnées).")]
        [Min(1f)] public float rareAmountMultiplier = 1.5f;
        [Tooltip("Plafond des réductions (cadence, rechargement : part retirée) d'une amélioration rare.")]
        [Range(0.1f, 0.9f)] public float rareCutCap = 0.6f;
    }
}
