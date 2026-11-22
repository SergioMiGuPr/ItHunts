using ItHunts.Level;
using UnityEngine;

namespace ItHunts.Menus
{
    /// <summary>
    /// Pose les vues de la DA sur les menus du jeu dès qu'ils démarrent : MainMenuArt sur le menu principal, PauseMenuArt sur le
    /// menu Échap. La logique (assembly ItHunts.Runtime) ne connaît pas les prefabs : elle prévient par MainMenu.Created et
    /// PauseMenu.Created.
    /// </summary>
    public static class MenuArtInstall
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Register()
        {
            MainMenu.Created -= AttachMain;
            MainMenu.Created += AttachMain;
            PauseMenu.Created -= AttachPause;
            PauseMenu.Created += AttachPause;
            AdminPanel.Created -= AttachAdmin;
            AdminPanel.Created += AttachAdmin;
            RunEndScreen.Created -= AttachEnd;
            RunEndScreen.Created += AttachEnd;
        }

        private static void AttachMain(MainMenu menu) => Attach(menu);

        private static void AttachPause(PauseMenu pause) => Attach(pause);

        private static void AttachEnd(RunEndScreen end)
        {
            if (end != null && end.GetComponent<RunEndScreenArt>() == null)
                end.gameObject.AddComponent<RunEndScreenArt>().Attach(end);
        }

        private static void AttachAdmin(AdminPanel panel)
        {
            if (panel != null && panel.GetComponent<AdminPanelArt>() == null)
                panel.gameObject.AddComponent<AdminPanelArt>().Attach(panel);
        }

        /// <summary>Vue de la DA sur ce menu (null : pas d'écrans de la DA, repli greybox). Public pour les tests.</summary>
        public static MainMenuArt Attach(MainMenu menu)
        {
            if (menu == null)
                return null;
            MainMenuArt view = menu.GetComponent<MainMenuArt>();
            if (view == null)
                view = menu.gameObject.AddComponent<MainMenuArt>();
            if (view.Menu == null && !view.Build(menu))
            {
                Object.DestroyImmediate(view);
                return null;
            }

            return view;
        }

        public static PauseMenuArt Attach(PauseMenu pause)
        {
            if (pause == null)
                return null;
            PauseMenuArt view = pause.GetComponent<PauseMenuArt>();
            if (view == null)
            {
                view = pause.gameObject.AddComponent<PauseMenuArt>();
                view.Attach(pause);
            }

            return view;
        }
    }
}
