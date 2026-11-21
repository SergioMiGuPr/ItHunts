using ItHunts.Level;
using ItHunts.Settings;

namespace ItHunts.Player
{
    /// <summary>
    /// Échelles des sensations de jeu (points 4 et 9 du 2026-11-21) : curseur « Intensité des effets » des Paramètres, et
    /// « Réduire les secousses » pour les secousses et les mouvements de caméra.
    /// </summary>
    public static class GameFeel
    {
        /// <summary>Intensité globale (0 à 1) : tous les effets.</summary>
        public static float Intensity => GameSettings.FeelIntensity;

        /// <summary>Secousses et mouvements de caméra : intensité × réduction.</summary>
        public static float MotionScale(GameFeelSettings s) =>
            Intensity * (GameSettings.ReduceShake ? (s != null ? s.reducedShakeScale : 0.3f) : 1f);
    }
}
