using ItHunts.Artifacts;
using ItHunts.Player;
using ItHunts.Settings;

namespace ItHunts.Economy
{
    /// <summary>Prix d'un achat pour un joueur (D-043) : prix de base, hausse à chaque achat de la catégorie (réglages, par joueur).</summary>
    public static class Prices
    {
        public static int For(EconomySettings settings, PlayerHealth p, PriceKind kind, int basePrice)
        {
            RunState run = RunState.Current;
            if (run == null || p == null)
                return basePrice;
            return run.PriceFor(p.PlayerId, kind, basePrice, settings != null ? settings.IncreaseFor(kind) : 0f);
        }

        /// <summary>Paie le prix actuel (refus en rouge sinon) ; vrai si payé.</summary>
        public static bool TryBuy(EconomySettings settings, PlayerHealth p, PriceKind kind, int basePrice)
        {
            RunState run = RunState.Current;
            return run != null && p != null && run.TryBuy(p.PlayerId, kind, basePrice, settings != null ? settings.IncreaseFor(kind) : 0f);
        }
    }
}
