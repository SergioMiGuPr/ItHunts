using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Jour 5, point 2 : une goule (ou un volant) que le joueur voit le voit aussi. Portée de détection = la plus grande de son
    /// réglage et de la portée de vue du joueur (fin du brouillard + 5 m) ; la ligne de vue reste exigée par l'appelant.
    /// </summary>
    public static class EnemySight
    {
        public static float Range(float setting) =>
            RenderSettings.fog ? Mathf.Max(setting, RenderSettings.fogEndDistance + 5f) : Mathf.Max(setting, 60f);
    }
}
