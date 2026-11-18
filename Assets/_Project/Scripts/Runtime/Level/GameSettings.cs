using ItHunts.Hud;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Réglages du joueur, communs au menu principal et au menu pause (écran « Paramètres » de la DA, UI-HANDOFF § 2.6) :
    /// volume général, musique, effets, sensibilité de la visée, plein écran, réduire les animations. Gardés par poste
    /// (PlayerPrefs), appliqués tout de suite. Les lignes de développement (options d'image, cadrage des mains) s'ajoutent
    /// à part, dans l'éditeur et les builds de développement seulement.
    /// </summary>
    public static class GameSettings
    {
        private const string Prefix = "ItHunts.Reglages.";

        public enum Line : byte { VolumeGeneral, Musique, Effets, Sensibilite, PleinEcran, ReduireAnimations }

        /// <summary>Libellés, dans l'ordre de l'écran de la DA.</summary>
        public static readonly string[] Labels = { "Volume général", "Musique", "Effets", "Sensibilité", "Plein écran", "Réduire les animations" };

        public static float MasterVolume { get => Get("Volume", 1f); set => Set("Volume", value, 0f, 1f); }
        public static float MusicVolume { get => Get("Musique", 0.8f); set => Set("Musique", value, 0f, 1f); }
        public static float EffectsVolume { get => Get("Effets", 0.8f); set => Set("Effets", value, 0f, 1f); }
        /// <summary>Multiplie la sensibilité de la souris et du stick (1 : réglage de CameraSettings).</summary>
        public static float Sensitivity { get => Get("Sensibilite", 1f); set => Set("Sensibilite", value, 0.25f, 2f); }
        public static bool ReduceMotion { get => PlayerPrefs.GetInt(Prefix + "ReduireAnimations", 0) == 1; set { PlayerPrefs.SetInt(Prefix + "ReduireAnimations", value ? 1 : 0); Changed?.Invoke(); } }

        public static bool FullScreen
        {
            get => Application.isEditor ? PlayerPrefs.GetInt(Prefix + "PleinEcran", 1) == 1 : Screen.fullScreen;
            set
            {
                PlayerPrefs.SetInt(Prefix + "PleinEcran", value ? 1 : 0);
                if (!Application.isEditor)
                    Screen.fullScreen = value;
                Changed?.Invoke();
            }
        }

        /// <summary>Un réglage a changé (volumes, animations) : les sources audio et les menus se mettent à jour.</summary>
        public static event System.Action Changed;

        private static float Get(string key, float fallback) => PlayerPrefs.GetFloat(Prefix + key, fallback);

        private static void Set(string key, float value, float min, float max)
        {
            PlayerPrefs.SetFloat(Prefix + key, Mathf.Clamp(Mathf.Round(value * 100f) / 100f, min, max));
            Apply();
        }

        /// <summary>Volume général sur l'écouteur ; prévient les sources (musique, feu).</summary>
        public static void Apply()
        {
            AudioListener.volume = MasterVolume;
            Changed?.Invoke();
        }

        /// <summary>Texte de la valeur d'une ligne (« 80 % », « oui »).</summary>
        public static string Value(Line line) => line switch
        {
            Line.VolumeGeneral => Percent(MasterVolume),
            Line.Musique => Percent(MusicVolume),
            Line.Effets => Percent(EffectsVolume),
            Line.Sensibilite => Percent(Sensitivity),
            Line.PleinEcran => FullScreen ? "oui" : "non",
            _ => ReduceMotion ? "oui" : "non",
        };

        private static string Percent(float v) => Mathf.RoundToInt(v * 100f) + " %";

        /// <summary>Gauche / droite sur une ligne : pas de 10 % (sensibilité : 25 % à 200 %), bascule pour oui / non.</summary>
        public static void Step(Line line, int d)
        {
            switch (line)
            {
                case Line.VolumeGeneral: MasterVolume = StepValue(MasterVolume, d, 0f, 1f, 0.1f); break;
                case Line.Musique: MusicVolume = StepValue(MusicVolume, d, 0f, 1f, 0.1f); break;
                case Line.Effets: EffectsVolume = StepValue(EffectsVolume, d, 0f, 1f, 0.1f); break;
                case Line.Sensibilite: Sensitivity = StepValue(Sensitivity, d, 0.25f, 2f, 0.05f); break;
                case Line.PleinEcran: FullScreen = !FullScreen; break;
                default: ReduceMotion = !ReduceMotion; break;
            }
        }

        /// <summary>Un pas, sans boucler aux bornes (on ne passe pas de 100 % à 0 % d'un coup).</summary>
        public static float StepValue(float v, int d, float min, float max, float step) => Mathf.Clamp(Mathf.Round((v + d * step) / step) * step, min, max);

        /// <summary>Les six lignes de l'écran Paramètres, dans un menu (Entrée = pas suivant).</summary>
        public static void Fill(GreyboxMenu menu)
        {
            for (int i = 0; i < Labels.Length; i++)
            {
                var line = (Line)i;
                GreyboxMenu.Item item = menu.Add(Labels[i], null);
                item.Value = () => Value(line);
                item.Change = d => Step(line, d);
                item.Submit = () => Step(line, 1);
            }
        }

        /// <summary>Lignes de développement (éditeur et builds de développement) : options d'image, à la suite des six lignes.</summary>
        public static void FillDevelopment(GreyboxMenu menu)
        {
            if (!MainMenu.PlaytestVisible)
                return;
            foreach (GraphicsOptions.Effect e in System.Enum.GetValues(typeof(GraphicsOptions.Effect)))
            {
                GraphicsOptions.Effect effect = e;
                GreyboxMenu.Item item = menu.Add(GraphicsOptions.Label(effect) + " (dév.)", null);
                item.Value = () => GraphicsOptions.Get(effect) ? "oui" : "non";
                item.Change = d => GraphicsOptions.Set(effect, !GraphicsOptions.Get(effect));
                item.Submit = () => item.Change(1);
            }
        }
    }
}
