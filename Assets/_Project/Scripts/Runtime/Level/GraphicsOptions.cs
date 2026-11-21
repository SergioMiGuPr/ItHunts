using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Options d'image du joueur (menu pause), enregistrées : bloom, vignettage, grain, aberration chromatique. Elles s'ajoutent aux
    /// interrupteurs de HorrorLookSettings (un effet coupé dans les réglages reste coupé).
    /// </summary>
    public static class GraphicsOptions
    {
        public enum Effect : byte
        {
            Bloom,
            Vignettage,
            Grain,
            Aberration,
        }

        private static string Key(Effect e) => "ItHunts.Image." + e;

        public static bool Get(Effect e) => PlayerPrefs.GetInt(Key(e), 1) != 0;

        public static void Set(Effect e, bool on)
        {
            PlayerPrefs.SetInt(Key(e), on ? 1 : 0);
            Changed?.Invoke();
        }

        public static event System.Action Changed;

        public static string Label(Effect e) => e switch
        {
            Effect.Bloom => "Bloom (sources lumineuses)",
            Effect.Vignettage => "Vignettage",
            Effect.Grain => "Grain",
            _ => "Aberration chromatique",
        };
    }
}
