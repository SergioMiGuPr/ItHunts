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

        /// <summary>Réglages en jeu (posés par le PlayerFeel du premier joueur ; les effets du monde les lisent).</summary>
        public static GameFeelSettings Settings { get; set; }

        /// <summary>0 à 1 entre deux valeurs (vitesse, dégâts…), 0 si la plage est vide.</summary>
        public static float Ramp(float v, float min, float full) => full > min ? UnityEngine.Mathf.Clamp01((v - min) / (full - min)) : (v >= min ? 1f : 0f);
    }

    /// <summary>
    /// Arrêts sur image (point 9) : Time.timeScale à 0 pendant une durée courte en temps réel (la plus longue demandée gagne),
    /// multipliée par l'intensité des effets et plafonnée. Local : en écran partagé les deux vues s'arrêtent ; en ligne, il faudra
    /// un arrêt seulement visuel, par joueur (la simulation ne doit pas s'arrêter).
    /// </summary>
    public static class HitStop
    {
        private static float _until;
        private static bool _active;
        private static Runner _runner;

        public static bool Active => _active;
        public static int Count { get; private set; }
        public static float LastDuration { get; private set; }

        /// <summary>Durée réelle d'un arrêt demandé (pur, testable).</summary>
        public static float Duration(float seconds, float intensity, float max) =>
            UnityEngine.Mathf.Min(UnityEngine.Mathf.Max(0f, seconds) * UnityEngine.Mathf.Clamp01(intensity), max);

        public static void Request(float seconds)
        {
            GameFeelSettings s = GameFeel.Settings;
            float d = Duration(seconds, GameFeel.Intensity, s != null ? s.maxHitStop : 0.12f);
            if (d < 0.005f || !UnityEngine.Application.isPlaying)
                return;
            if (!_active && UnityEngine.Time.timeScale != 1f)
                return;                                                  // le temps est déjà réglé par autre chose
            if (_runner == null)
            {
                var go = new UnityEngine.GameObject("Arrêts sur image");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.hideFlags = UnityEngine.HideFlags.HideInHierarchy;
                _runner = go.AddComponent<Runner>();
            }

            _until = UnityEngine.Mathf.Max(_until, UnityEngine.Time.unscaledTime + d);
            LastDuration = d;
            Count++;
            _active = true;
            UnityEngine.Time.timeScale = 0f;
        }

        private sealed class Runner : UnityEngine.MonoBehaviour
        {
            private void Update()
            {
                if (_active && UnityEngine.Time.unscaledTime >= _until)
                {
                    _active = false;
                    UnityEngine.Time.timeScale = 1f;
                }
            }

            private void OnDestroy()
            {
                if (_active)
                    UnityEngine.Time.timeScale = 1f;
                _active = false;
            }
        }
    }
}
