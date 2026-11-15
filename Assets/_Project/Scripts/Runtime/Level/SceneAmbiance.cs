using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Applique l'ambiance de rendu de la scène (AmbianceSettings, D-067) : lumière ambiante en mode Flat, brouillard linéaire,
    /// lune (lumières directionnelles), torches et lampes (AmbianceLight), fond des caméras = couleur du brouillard (aussi pour
    /// un joueur qui rejoint). S'exécute avant tout le monde (le générateur de la carte aléatoire lit Active). Une par scène de
    /// jeu ; Use(…) change d'ambiance en jeu (phase enragée, plus tard).
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class SceneAmbiance : MonoBehaviour
    {
        [SerializeField] private AmbianceSettings ambiance;

        public static AmbianceSettings Active { get; private set; }
        private int _cameras = -1;

        private void Awake()
        {
            Active = ambiance;
            Apply();
        }

        private void OnDestroy()
        {
            if (Active == ambiance)
                Active = null;
        }

        /// <summary>Change d'ambiance (même scène).</summary>
        public void Use(AmbianceSettings settings)
        {
            ambiance = settings;
            Active = settings;
            Apply();
            foreach (AmbianceLight l in FindObjectsByType<AmbianceLight>(FindObjectsSortMode.None))
                l.Apply();
        }

        private void Apply()
        {
            if (ambiance == null)
                return;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = ambiance.ambient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = ambiance.fogColor;
            RenderSettings.fogStartDistance = ambiance.fogStart;
            RenderSettings.fogEndDistance = ambiance.fogEnd;
            foreach (Light l in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.type == LightType.Directional)
                    l.intensity = ambiance.moonIntensity;
            Cameras();
        }

        private void Cameras()
        {
            if (ambiance == null)
                return;
            _cameras = Camera.allCamerasCount;
            foreach (Camera c in Camera.allCameras)
            {
                if (c.cullingMask == 0)
                    continue;                                          // écran de chargement : noir
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = ambiance.fogColor;
            }
        }

        private void Update()
        {
            if (Camera.allCamerasCount != _cameras)
                Cameras();                                             // joueur qui rejoint, écran partagé
        }
    }
}
