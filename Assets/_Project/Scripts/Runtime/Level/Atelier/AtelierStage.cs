using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Scène de l'atelier des modules (2026-11-21) : sol de travail à grille (pas 1, 2 ou 4 m), éclairage neutre pour bien voir, ou
    /// éclairage du jeu (noir et torches du module). Piloté par la palette (ItHunts > Palette des modules) ; rien ici en jeu.
    /// </summary>
    [ExecuteAlways]
    public sealed class AtelierStage : MonoBehaviour
    {
        public Renderer grid;
        [Tooltip("Taille du sol de travail (m).")]
        public float gridSize = 400f;
        [Tooltip("Pas de la grille (m) : 1, 2 ou 4.")]
        public float step = 2f;
        public GameObject neutralLights;
        public bool gameLighting;
        public Color neutralAmbient = new Color(0.42f, 0.42f, 0.46f);
        public Color gameAmbient = new Color(0.02f, 0.02f, 0.03f);

        public void SetStep(float s)
        {
            step = s;
            Apply();
        }

        public void SetGameLighting(bool on)
        {
            gameLighting = on;
            Apply();
        }

        public void Apply()
        {
            if (grid != null && grid.sharedMaterial != null)
            {
                float t = gridSize / Mathf.Max(0.25f, step);
                grid.sharedMaterial.SetTextureScale("_BaseMap", new Vector2(t, t));
            }

            if (neutralLights != null)
                neutralLights.SetActive(!gameLighting);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = gameLighting ? gameAmbient : neutralAmbient;
        }

        private void OnEnable() => Apply();
    }
}
