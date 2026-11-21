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
            ShopMenu.Opened -= ShopPanelArt.Attach;
            ShopMenu.Opened += ShopPanelArt.Attach;
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
