using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Prix d'un achat pour un joueur (D-043, D-100) : prix de base × remise de ses améliorations (Marchandage, Œil du collectionneur),
    /// hausse à chaque achat de la catégorie (douce, plafonnée ; divisée par 2 avec Marchandage Or), par joueur.
    /// </summary>
    public static class Prices
    {
        /// <summary>Prix de base après la remise des améliorations du joueur (pur).</summary>
        public static int Discounted(PlayerHealth p, PriceKind kind, int basePrice)
        {
            if (basePrice <= 0 || p == null)
                return basePrice;
            return Mathf.Max(1, Mathf.RoundToInt(basePrice * PlayerUpgrades.PriceScale(p.PlayerId, kind)));
        }

        private static float Increase(EconomySettings settings, PlayerHealth p, PriceKind kind) =>
            (settings != null ? settings.IncreaseFor(kind) : 0f) * (p != null ? PlayerUpgrades.IncreaseScale(p.PlayerId, kind) : 1f);

        public static int For(EconomySettings settings, PlayerHealth p, PriceKind kind, int basePrice)
        {
            RunState run = RunState.Current;
            int price = Discounted(p, kind, basePrice);
            if (run == null || p == null)
                return price;
            return run.PriceFor(p.PlayerId, kind, price, Increase(settings, p, kind), settings != null ? settings.priceIncreaseCap : 0f);
        }

        /// <summary>Paie le prix actuel (refus en rouge sinon) ; vrai si payé.</summary>
        public static bool TryBuy(EconomySettings settings, PlayerHealth p, PriceKind kind, int basePrice)
        {
            RunState run = RunState.Current;
            return run != null && p != null && run.TryBuy(p.PlayerId, kind, Discounted(p, kind, basePrice), Increase(settings, p, kind),
                settings != null ? settings.priceIncreaseCap : 0f);
        }
    }
}
