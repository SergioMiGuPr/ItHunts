using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Applique l'ambiance de rendu de la scène (AmbianceSettings, D-067) : lumière ambiante en mode Flat, brouillard linéaire,
    /// lune (lumières directionnelles), torches et lampes (AmbianceLight), fond des caméras = couleur du brouillard (aussi pour
    /// un joueur qui rejoint). S'exécute avant tout le monde (le générateur de la carte aléatoire lit Active). Une par scène de
    /// jeu ; Use(…) change d'ambiance en jeu. Phase enragée : fondu vers ambiance.enraged en enragedTransition secondes quand
    /// la zone passe en Enragé, retour à la remise à zéro de la zone (ZoneState.Enraged).
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class SceneAmbiance : MonoBehaviour
    {
        [SerializeField] private AmbianceSettings ambiance;

        public static AmbianceSettings Active { get; private set; }
        private int _cameras = -1;
        private AmbianceSettings _mix;
        private ItHunts.Enemies.ZoneState _zone;
        private float _nextZoneSearch;

        /// <summary>Part de l'ambiance enragée (0 : ambiance de la scène, 1 : ambiance enragée).</summary>
        public float EnragedBlend { get; private set; }
        public AmbianceSettings Base => ambiance;

        private void Awake()
        {
            Active = ambiance;
            Apply();
        }

        private void OnDestroy()
        {
            if (Active == ambiance || (Active != null && Active == _mix))
                Active = null;
            if (_mix != null)
                Destroy(_mix);
        }

        /// <summary>Change d'ambiance (même scène).</summary>
        public void Use(AmbianceSettings settings)
        {
            ambiance = settings;
            EnragedBlend = 0f;
            Active = settings;
            Apply();
            ApplyLights();
        }

        private void ApplyLights()
        {
            foreach (AmbianceLight l in AmbianceLight.All)
                if (l != null)
                    l.Apply();
        }

        /// <summary>Fondu vers l'ambiance enragée ou retour (pas en secondes) ; appelé chaque image, public pour les tests.</summary>
        public void StepEnraged(bool enraged, float dt)
        {
            if (ambiance == null || ambiance.enraged == null)
                return;
            float target = enraged ? 1f : 0f;
            if (Mathf.Approximately(EnragedBlend, target))
                return;
            float speed = ambiance.enragedTransition > 0f ? dt / ambiance.enragedTransition : 1f;
            EnragedBlend = Mathf.MoveTowards(EnragedBlend, target, speed);
            if (EnragedBlend <= 0f)
            {
                Active = ambiance;                                     // l'asset lui-même : réglable en Play Mode
            }
            else
            {
                if (_mix == null)
                {
                    _mix = ScriptableObject.CreateInstance<AmbianceSettings>();
                    _mix.name = ambiance.name + " → " + ambiance.enraged.name;
                    _mix.hideFlags = HideFlags.DontSave;
                }
                AmbianceSettings.Lerp(ambiance, ambiance.enraged, EnragedBlend, _mix);
                Active = _mix;
            }

            AmbianceSettings keep = ambiance;
            ambiance = Active;
            Apply();
            ambiance = keep;
            ApplyLights();
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
                {
                    l.intensity = ambiance.moonIntensity;
                    AmbianceLight.LightViewmodel(l);                    // la lune éclaire aussi les mains (pas la lanterne)
                }
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
            if (_zone == null && Time.unscaledTime >= _nextZoneSearch)
            {
                _zone = FindFirstObjectByType<ItHunts.Enemies.ZoneState>();
                _nextZoneSearch = Time.unscaledTime + 1f;
            }
            StepEnraged(_zone != null && _zone.Enraged, Time.deltaTime);
        }
    }
}
