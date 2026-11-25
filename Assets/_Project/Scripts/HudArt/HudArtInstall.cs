using ItHunts.Economy;
using ItHunts.Hud;
using UnityEngine;

namespace ItHunts.HudArt
{
    /// <summary>
    /// Pose l'interface en jeu de la DA dès que la logique la demande : PlayerHudArt quand un HUD de joueur est construit,
    /// ShopPanelArt quand un menu de marchand ou de coffre s'ouvre (la logique, assembly ItHunts.Runtime, ne connaît pas les
    /// prefabs de la DA).
    /// </summary>
    public static class HudArtInstall
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            PlayerHud.Built -= AttachHud;
            PlayerHud.Built += AttachHud;
            ShopMenu.Opened -= AttachShop;
            ShopMenu.Opened += AttachShop;
        }

        /// <summary>Coffre et marchand : panneau d'offres de la nuit 5 (cartes illustrées, relance) ; sinon l'ancien panneau en liste.</summary>
        private static void AttachShop(ShopMenu menu)
        {
            if (menu == null || OffersPanelArt.Attach(menu))
                return;
            ShopPanelArt.Attach(menu);
        }

        private static void AttachHud(PlayerHud hud) => Attach(hud);

        /// <summary>Vue de la DA sur ce HUD (null : pas de prefabs, repli sur les formes provisoires). Public pour les tests.</summary>
        public static PlayerHudArt Attach(PlayerHud hud)
        {
            if (hud == null)
                return null;
            PlayerHudArt view = hud.GetComponent<PlayerHudArt>();
            if (view != null)
                return view;
            view = hud.gameObject.AddComponent<PlayerHudArt>();
            if (!view.Build(hud))
            {
                Object.Destroy(view);
                return null;
            }

            return view;
        }
    }
}
