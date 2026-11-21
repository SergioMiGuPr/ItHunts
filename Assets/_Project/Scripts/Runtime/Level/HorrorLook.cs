using System.Collections.Generic;
using ItHunts.Enemies;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Level
{
    /// <summary>
    /// Lumière d'ambiance horreur (HorrorLookSettings, une partie par élément, chacune activable) : étalonnage par ambiance et bascule
    /// rouge sang en Enragé (volume global créé ici), halos des torches, lampes et lanternes (un système de particules), torches qui
    /// vacillent et lampes qui grésillent (près des joueurs seulement), cône de la lanterne qui projette des ombres, budget d'ombres
    /// (les N lumières les plus proches des joueurs), bloom, vignettage, grain et aberration (aussi coupés par GraphicsOptions).
    /// Ajouté par SceneAmbiance quand l'ambiance a des réglages (champ look). Visuel seulement : rien de simulé (réseau : local).
    /// </summary>
    [DefaultExecutionOrder(700)]                                        // après la lanterne du joueur (600)
    public sealed class HorrorLook : MonoBehaviour
    {
        public static HorrorLook Current { get; private set; }

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.HorrorLook");

        private Volume _volume;
        private VolumeProfile _profile;
        private Bloom _bloom;
        private Vignette _vignette;
        private FilmGrain _grain;
        private ChromaticAberration _aberration;
        private ColorAdjustments _color;
        private SplitToning _split;
        private static readonly int EnrageWeightId = Shader.PropertyToID("_ItHunts_EnrageWeight");
        private static readonly int EnrageTintId = Shader.PropertyToID("_ItHunts_EnrageTint");
        private static readonly int EnrageGainId = Shader.PropertyToID("_ItHunts_EnrageGain");
        private ScriptableRendererFeature _enragePass;
        private bool _enragePassSearched;

        /// <summary>Poids de la bascule rouge appliqué au monde (0..1 ; la vue FPS en est exclue par le stencil).</summary>
        public float EnrageWeight { get; private set; }
        private ZoneState _zone;
        private float _nextSearch;

        /// <summary>Avancement de la bascule rouge sang (0..1).</summary>
        public float Monochrome { get; private set; }

        private ParticleSystem _halos;
        private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[0];
        private readonly List<PlayerLanternLight> _lanterns = new List<PlayerLanternLight>();
        private readonly Dictionary<PlayerLanternLight, Light> _cones = new Dictionary<PlayerLanternLight, Light>();
        private readonly Dictionary<Light, Camera> _coneCameras = new Dictionary<Light, Camera>();
        private readonly Dictionary<Light, ConeState> _coneStates = new Dictionary<Light, ConeState>();

        /// <summary>État lissé du cône d'une lanterne : hauteur, lacet et tangage (degrés).</summary>
        public struct ConeState
        {
            public bool Ready;
            public float Height, Yaw, Pitch;
        }

        /// <summary>
        /// Cône qui suit le corps (pur, testable) : l'horizontale suit l'œil exactement, la hauteur et la direction le rejoignent
        /// par un lissage exponentiel (pas d'à-coup au saut, au dash, à une rotation rapide). Le tangage est atténué.
        /// </summary>
        public static ConeState FollowBody(ConeState c, float eyeHeight, float yaw, float pitch, float dt, HorrorLookSettings s)
        {
            float targetPitch = pitch * s.lanternConePitchScale;
            if (!c.Ready)
                return new ConeState { Ready = true, Height = eyeHeight, Yaw = yaw, Pitch = targetPitch };
            float kh = 1f - Mathf.Exp(-s.lanternConeHeightFollow * dt), kr = 1f - Mathf.Exp(-s.lanternConeFollow * dt);
            c.Height = Mathf.Lerp(c.Height, eyeHeight, kh);
            c.Yaw += Mathf.DeltaAngle(c.Yaw, yaw) * kr;
            c.Pitch = Mathf.Lerp(c.Pitch, targetPitch, kr);
            return c;
        }

        /// <summary>État du cône d'une lumière (tests, mesures).</summary>
        public bool TryGetConeState(Light cone, out ConeState state) => _coneStates.TryGetValue(cone, out state);
        private readonly List<Vector3> _eyes = new List<Vector3>();
        private readonly HashSet<AmbianceLight> _moved = new HashSet<AmbianceLight>();
        private readonly Dictionary<AmbianceLight, float> _buzzUntil = new Dictionary<AmbianceLight, float>();
        private readonly Dictionary<AmbianceLight, float> _buzzNext = new Dictionary<AmbianceLight, float>();
        private readonly List<(Light light, float d, bool cone)> _shadowCandidates = new List<(Light, float, bool)>();
        private float _nextShadow;

        /// <summary>Lumières qui projettent des ombres, fondu compris (tests, mesures).</summary>
        public int ShadowedCount { get; private set; }
        public int HaloCount { get; private set; }
        public IReadOnlyDictionary<PlayerLanternLight, Light> Cones => _cones;

        private static HorrorLookSettings Settings => SceneAmbiance.Active != null ? SceneAmbiance.Active.look : null;

        private void Awake()
        {
            Current = this;
            var go = new GameObject("Volume (lumière d'ambiance horreur)");
            go.transform.SetParent(transform, false);
            _volume = go.AddComponent<Volume>();
            _volume.isGlobal = true;
            _volume.priority = 50f;                                      // au-dessus du volume de la scène (bloom)
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "Lumière d'ambiance horreur";
            _profile.hideFlags = HideFlags.DontSave;
            _bloom = _profile.Add<Bloom>(true);
            _vignette = _profile.Add<Vignette>(true);
            _grain = _profile.Add<FilmGrain>(true);
            _aberration = _profile.Add<ChromaticAberration>(true);
            _color = _profile.Add<ColorAdjustments>(true);
            _split = _profile.Add<SplitToning>(true);
            _volume.sharedProfile = _profile;
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
            if (_profile != null && !Quitting)
                Destroy(_profile);                                      // à la fermeture, Unity libère tout lui-même
        }

        private void LateUpdate()
        {
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            HorrorLookSettings s = Settings;
            float dt = Time.deltaTime;
            if (Time.unscaledTime >= _nextSearch)
            {
                _nextSearch = Time.unscaledTime + 1f;
                if (_zone == null)
                    _zone = FindFirstObjectByType<ZoneState>();
                _lanterns.Clear();
                _lanterns.AddRange(FindObjectsByType<PlayerLanternLight>(FindObjectsSortMode.None));
            }

            _eyes.Clear();
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null)
                {
                    var input = p.GetComponent<PlayerInput>();
                    Camera c = input != null ? input.camera : null;
                    _eyes.Add(c != null ? c.transform.position : p.transform.position + Vector3.up * 1.6f);
                }

            UpdateVolume(s, dt);
            CullLights(s);
            UpdateCones(s);
            UpdateFlicker(s);
            UpdateHalos(s);
            UpdateShadows(s);
        }

        // ---------- Étalonnage, Enragé, effets d'écran ----------

        private void UpdateVolume(HorrorLookSettings s, float dt)
        {
            AmbianceSettings a = SceneAmbiance.Active;
            bool enraged = _zone != null && _zone.Enraged;
            float monoTime = s != null ? s.enragedMonoSeconds : 1f;
            Monochrome = Mathf.MoveTowards(Monochrome, enraged && s != null && s.enragedMonochrome ? 1f : 0f, dt / (enraged ? monoTime : 2f));
            _volume.enabled = s != null;
            if (s == null || a == null)
                return;

            bool grade = s.grading;
            float m = Monochrome;
            _color.active = true;
            _color.postExposure.Override(grade ? a.postExposure : 0f);
            _color.contrast.Override((grade ? a.contrast : 0f) + s.enragedContrast * m);
            _color.saturation.Override(grade ? a.saturation : 0f);
            _color.colorFilter.Override(grade ? a.colorFilter : Color.white);
            // Enragé : monochrome rouge sang sur le monde seulement (passe plein écran « ItHunts Enragé (monde) », stencil de la vue FPS) :
            // les mains, l'orbe, les tablettes et l'artefact gardent la couleur du personnage ; le HUD est superposé (pas étalonné).
            EnrageWeight = m * (1f - s.enragedKeepColor);
            Shader.SetGlobalFloat(EnrageWeightId, EnrageWeight);
            Shader.SetGlobalVector(EnrageTintId, s.enragedTint);
            Shader.SetGlobalFloat(EnrageGainId, Mathf.Pow(2f, s.enragedExposure));
            ScriptableRendererFeature pass = EnragePass();
            if (pass != null && pass.isActive != EnrageWeight > 0f)
            {
                pass.SetActive(EnrageWeight > 0f);                          // hors Enragé : la passe ne coûte rien
                SetHandsStencil(EnrageWeight > 0f);                         // le stencil des mains seulement pendant l'Enragé
            }
            _split.active = grade;
            _split.shadows.Override(a.splitShadows);
            _split.highlights.Override(a.splitHighlights);
            _split.balance.Override(a.splitBalance);

            // Un effet coupé garde son override à zéro : il éteint aussi le bloom du volume de la scène.
            bool bloom = s.bloom && GraphicsOptions.Get(GraphicsOptions.Effect.Bloom);
            _bloom.active = true;
            _bloom.intensity.Override(bloom ? s.bloomIntensity : 0f);
            _bloom.threshold.Override(s.bloomThreshold);
            _bloom.scatter.Override(s.bloomScatter);
            bool vig = s.vignette && GraphicsOptions.Get(GraphicsOptions.Effect.Vignettage);
            _vignette.active = true;
            _vignette.intensity.Override(vig ? s.vignetteIntensity : 0f);
            _vignette.smoothness.Override(s.vignetteSmoothness);
            bool grain = s.filmGrain && GraphicsOptions.Get(GraphicsOptions.Effect.Grain);
            _grain.active = grain;
            _grain.intensity.Override(s.grainIntensity);
            _grain.type.Override(FilmGrainLookup.Thin1);
            bool ca = s.chromaticAberration && GraphicsOptions.Get(GraphicsOptions.Effect.Aberration);
            _aberration.active = true;
            _aberration.intensity.Override(ca ? s.chromaticIntensity : 0f);
        }

        /// <summary>La passe plein écran de l'Enragé dans le renderer URP actif (cherchée une fois, par son nom).</summary>
        private ScriptableRendererFeature EnragePass()
        {
            if (_enragePassSearched)
                return _enragePass;
            _enragePassSearched = true;
            var asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            var list = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(asset) as ScriptableRendererData[];
            if (list != null)
                foreach (ScriptableRendererData data in list)
                    if (data != null)
                        foreach (ScriptableRendererFeature f in data.rendererFeatures)
                            if (f != null && f.name == EnragePassName)
                                _enragePass = f;
            return _enragePass;
        }

        public const string EnragePassName = "ItHunts Enragé (monde)";

        /// <summary>L'application se ferme : on ne touche plus au renderer URP, aux globales ni au profil.</summary>
        public static bool Quitting { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void WatchQuit()
        {
            Quitting = false;
            _quitDeferred = false;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
            Application.wantsToQuit -= BeforeQuit;
            Application.wantsToQuit += BeforeQuit;
        }

        private static void OnQuitting() => Quitting = true;

        private void OnDisable()
        {
            if (Quitting)
                return;                                                 // fermeture : le pipeline se détruit, ne rien lui demander
            Shader.SetGlobalFloat(EnrageWeightId, 0f);
            if (_enragePass != null && _enragePass.isActive)
                _enragePass.SetActive(false);
            SetHandsStencil(false);
        }

        // ---------- Stencil des mains (Enragé) et plantage D3D12 à la fermeture ----------

        /// <summary>
        /// Plantage à la fermeture des builds (D3D12Core.dll, 0xc0000005, à chaque fermeture) : il vient du stencil écrit par les
        /// passes « ItHunts Mains » du renderer (bissection par builds : renderer de l'ancienne version = fermeture propre, même
        /// renderer sans le stencil = propre ; le stencil allumé en jeu puis éteint avant de quitter = propre). Le stencil ne sert
        /// qu'à l'Enragé (le monde rougit, les mains gardent leur couleur) : éteint dans le renderer, il est allumé seulement pendant
        /// l'Enragé, et éteint avant la fermeture (wantsToQuit : deux images plus tard, on quitte vraiment).
        /// </summary>
        public static bool HandsStencilOn { get; private set; }

        public const string HandsPassPrefix = "ItHunts Mains";

        public static void SetHandsStencil(bool on)
        {
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset asset))
                return;
            var list = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(asset) as ScriptableRendererData[];
            if (list == null)
                return;
            bool changed = false;
            foreach (ScriptableRendererData data in list)
            {
                if (data == null)
                    continue;
                foreach (ScriptableRendererFeature f in data.rendererFeatures)
                    if (f is RenderObjects ro && ro.name.StartsWith(HandsPassPrefix) && ro.settings.stencilSettings.overrideStencilState != on)
                    {
                        ro.settings.stencilSettings.overrideStencilState = on;
                        ro.settings.stencilSettings.stencilReference = on ? 1 : 0;
                        ro.settings.stencilSettings.stencilCompareFunction = CompareFunction.Always;
                        ro.settings.stencilSettings.passOperation = on ? StencilOp.Replace : StencilOp.Keep;
                        ro.Create();
                        changed = true;
                    }
                if (changed)
                    data.SetDirty();
            }

            HandsStencilOn = on;
        }

        private static bool _quitDeferred;

        /// <summary>Avant de quitter : stencil éteint, puis on quitte deux images plus tard (le rendu ne l'utilise plus).</summary>
        private static bool BeforeQuit()
        {
            if (!HandsStencilOn || _quitDeferred || Current == null || !Current.isActiveAndEnabled)
                return true;
            _quitDeferred = true;
            SetHandsStencil(false);
            Current.StartCoroutine(QuitLater());
            return false;
        }

        private static System.Collections.IEnumerator QuitLater()
        {
            yield return null;
            yield return null;
            Application.Quit();
        }

        // ---------- Lanterne : cône vers l'avant ----------

        private void UpdateCones(HorrorLookSettings s)
        {
            foreach (PlayerLanternLight lantern in _lanterns)
            {
                if (lantern == null || lantern.Light == null)
                    continue;
                var input = lantern.GetComponent<PlayerInput>();
                Camera cam = input != null ? input.camera : null;
                if (!_cones.TryGetValue(lantern, out Light cone) || cone == null)
                {
                    if (cam == null || s == null || !s.lanternCone)
                        continue;
                    var go = new GameObject("Lanterne : cône (ombres)");
                    go.transform.SetParent(transform, false);                  // pas sous la caméra : il la suit avec un lissage
                    cone = go.AddComponent<Light>();
                    cone.type = LightType.Spot;
                    cone.shadows = LightShadows.None;
                    var coneData = go.AddComponent<UniversalAdditionalLightData>();
                    // Résolution « haute » de l'atlas pour le cône (lecture seule dans l'API : champ sérialisé ; sinon : moyenne).
                    typeof(UniversalAdditionalLightData).GetField("m_AdditionalLightsShadowResolutionTier",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.SetValue(coneData, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh);
                    _cones[lantern] = cone;
                    _coneCameras[cone] = cam;
                }

                bool on = s != null && s.lanternCone && lantern.Light.enabled;
                cone.enabled = on;
                if (!on)
                    continue;
                if (_coneCameras.TryGetValue(cone, out Camera follow) && follow != null)
                {
                    // Le corps, pas la caméra (lumières qui sautaient, 2026-11-17) : la caméra donne la position de l'œil et le regard,
                    // le cône les rejoint en douceur ; décalage de la lanterne dans le repère du lacet seul (le tangage ne la fait pas
                    // tourner autour de l'œil).
                    Vector3 eye = follow.transform.position;
                    Vector3 e = follow.transform.eulerAngles;
                    float pitch = Mathf.DeltaAngle(0f, e.x);
                    _coneStates.TryGetValue(cone, out ConeState st);
                    st = FollowBody(st, eye.y, e.y, pitch, Time.deltaTime, s);
                    _coneStates[cone] = st;
                    Quaternion body = Quaternion.Euler(0f, st.Yaw, 0f);
                    cone.transform.position = new Vector3(eye.x, st.Height, eye.z) + body * s.lanternConeOffset;
                    cone.transform.rotation = Quaternion.Euler(st.Pitch, st.Yaw, 0f);
                }
                cone.spotAngle = s.lanternConeAngle;
                cone.innerSpotAngle = s.lanternConeAngle * 0.55f;
                cone.range = s.lanternConeRange;
                cone.intensity = s.lanternConeIntensity;
                cone.color = lantern.Light.color;
                cone.renderingLayerMask = lantern.Light.renderingLayerMask;   // pas les mains (calque FPS), comme la lanterne
                var data = cone.GetComponent<UniversalAdditionalLightData>();
                var src = lantern.Light.GetComponent<UniversalAdditionalLightData>();
                if (data != null && src != null && data.renderingLayers != src.renderingLayers)
                    data.renderingLayers = src.renderingLayers;
            }
        }

        // ---------- Torches qui vacillent, lampes qui grésillent ----------

        private float Near(Vector3 p)
        {
            float best = float.PositiveInfinity;
            foreach (Vector3 e in _eyes)
                best = Mathf.Min(best, (p - e).sqrMagnitude);
            return Mathf.Sqrt(best);
        }

        // Lumières proches d'un joueur (vacillement, halos animés) : liste refaite 4 fois par seconde, pas à chaque image.
        private readonly List<AmbianceLight> _near = new List<AmbianceLight>();
        private readonly HashSet<AmbianceLight> _nearSet = new HashSet<AmbianceLight>();
        private readonly List<AmbianceLight> _restore = new List<AmbianceLight>();
        private float _nextNear;
        private float _nearRadius = -1f;

        private void RefreshNear(HorrorLookSettings s)
        {
            float radius = s != null ? s.flickerRadius : 0f;
            if (Time.time < _nextNear && Mathf.Approximately(radius, _nearRadius))
                return;
            _nextNear = Time.time + 0.25f;
            _nearRadius = radius;
            _near.Clear();
            _nearSet.Clear();
            float r2 = radius * radius;
            foreach (AmbianceLight l in AmbianceLight.All)
            {
                if (l == null || l.Light == null)
                    continue;
                Vector3 p = l.transform.position;
                foreach (Vector3 e in _eyes)
                    if ((p - e).sqrMagnitude < r2)
                    {
                        _near.Add(l);
                        _nearSet.Add(l);
                        break;
                    }
            }
        }

        private void UpdateFlicker(HorrorLookSettings s)
        {
            bool on = s != null && s.flicker;
            RefreshNear(s);
            float now = Time.time;
            // Celles qui ne vacillent plus (loin, ou effet coupé) reprennent leur intensité et leur place.
            if (_moved.Count > 0)
            {
                _restore.Clear();
                foreach (AmbianceLight l in _moved)
                    if (l == null || !on || !_nearSet.Contains(l))
                        _restore.Add(l);
                foreach (AmbianceLight l in _restore)
                {
                    _moved.Remove(l);
                    if (l == null || l.Light == null)
                        continue;
                    if (l.BaseIntensity > 0f)
                        l.Light.intensity = l.BaseIntensity;
                    l.transform.localPosition = l.BaseLocalPosition;
                }
            }

            if (!on)
                return;
            foreach (AmbianceLight l in _near)
            {
                if (l == null || l.Light == null)
                    continue;
                float baseI = l.BaseIntensity > 0f ? l.BaseIntensity : l.Light.intensity;
                _moved.Add(l);
                float seed = (l.GetInstanceID() & 1023) * 0.7319f;
                if (l.kind == AmbianceLight.Kind.Torche)
                {
                    float t = now * s.torchFlickerSpeed;
                    float n = (Mathf.PerlinNoise(t, seed) - 0.5f) * 1.4f + (Mathf.PerlinNoise(t * 2.7f, seed + 9f) - 0.5f) * 0.6f;
                    l.Light.intensity = baseI * Mathf.Max(0f, 1f + s.torchFlickerAmount * 2f * n);
                    Vector3 j = new Vector3(Mathf.PerlinNoise(t * 0.6f, seed + 3f) - 0.5f, Mathf.PerlinNoise(t * 0.6f, seed + 5f) - 0.5f,
                        Mathf.PerlinNoise(t * 0.6f, seed + 7f) - 0.5f) * (2f * s.torchJitter);
                    l.transform.localPosition = s.torchJitter > 0f ? l.BaseLocalPosition + j : l.BaseLocalPosition;
                }
                else
                {
                    // Une part des lampes grésille : tirage fixe par lampe.
                    bool buzzing = Mathf.Repeat(seed * 7.13f, 1f) < s.buzzingLampShare;
                    float k = 1f;
                    if (buzzing)
                    {
                        if (!_buzzNext.TryGetValue(l, out float next))
                            _buzzNext[l] = next = now + Random.Range(2f, 9f);
                        if (now >= next)
                        {
                            _buzzUntil[l] = now + Random.Range(0.05f, 0.4f);
                            _buzzNext[l] = now + Random.Range(2f, 9f);
                        }

                        if (_buzzUntil.TryGetValue(l, out float until) && now < until)
                            k = 1f - s.buzzDepth * (Mathf.PerlinNoise(now * 40f, seed) > 0.45f ? 1f : 0.3f);
                    }

                    l.Light.intensity = baseI * k;
                }
            }
        }

        // ---------- Halos (un système de particules) ----------

        private void EnsureHalos(HorrorLookSettings s)
        {
            if (_halos != null || s.haloMaterial == null)
                return;
            var go = new GameObject("Halos des lumières");
            go.transform.SetParent(transform, false);
            _halos = go.AddComponent<ParticleSystem>();
            _halos.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = _halos.main;
            main.playOnAwake = false;
            main.loop = false;
            main.maxParticles = 20000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = 1e6f;
            main.startSpeed = 0f;
            ParticleSystem.EmissionModule emission = _halos.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = _halos.shape;
            shape.enabled = false;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = s.haloMaterial;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.sortMode = ParticleSystemSortMode.None;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.maxParticleSize = 3f;
        }

        // Halos : tous reconstruits une fois par seconde (lumières ajoutées ou retirées) ; chaque image, seuls ceux des lumières
        // proches (vacillement) et des lanternes changent.
        private readonly Dictionary<AmbianceLight, int> _haloIndex = new Dictionary<AmbianceLight, int>();
        private float _nextHaloRebuild;
        private int _staticHalos;

        private void UpdateHalos(HorrorLookSettings s)
        {
            bool on = s != null && s.halos && s.haloMaterial != null;
            if (!on)
            {
                if (_halos != null && _halos.particleCount > 0)
                    _halos.Clear();
                HaloCount = 0;
                _nextHaloRebuild = 0f;
                return;
            }

            EnsureHalos(s);
            int n = AmbianceLight.All.Count + _lanterns.Count;
            if (_particles.Length < n)
            {
                _particles = new ParticleSystem.Particle[n + 64];
                _nextHaloRebuild = 0f;
            }

            int k;
            if (Time.time >= _nextHaloRebuild)
            {
                _nextHaloRebuild = Time.time + 1f;
                _haloIndex.Clear();
                k = 0;
                foreach (AmbianceLight l in AmbianceLight.All)
                {
                    if (l == null || l.Light == null || !l.isActiveAndEnabled)
                        continue;                                       // éteinte par le brouillard : son halo reste (le brouillard l'efface)
                    _haloIndex[l] = k;
                    Add(ref k, HaloPosition(l, s), l.kind == AmbianceLight.Kind.Torche ? s.torchHaloSize : s.lampHaloSize, l.Light.color,
                        s.haloIntensity * Flick(l));
                }

                _staticHalos = k;
            }
            else
            {
                foreach (AmbianceLight l in _near)
                    if (l != null && l.Light != null && _haloIndex.TryGetValue(l, out int i))
                    {
                        Color c = l.Light.color;
                        c.a = Mathf.Clamp01(s.haloIntensity * Flick(l));
                        _particles[i].startColor = c;
                        _particles[i].position = HaloPosition(l, s);
                    }
            }

            k = _staticHalos;
            foreach (PlayerLanternLight lantern in _lanterns)
                if (lantern != null && lantern.Light != null && lantern.Light.enabled)
                    Add(ref k, lantern.Light.transform.position, s.lanternHaloSize, lantern.Light.color, s.haloIntensity);
            _halos.SetParticles(_particles, k);
            HaloCount = k;
        }

        private static float Flick(AmbianceLight l)
        {
            float baseI = l.BaseIntensity > 0f ? l.BaseIntensity : Mathf.Max(0.01f, l.Light.intensity);
            return Mathf.Clamp(l.Light.intensity / baseI, 0f, 1.6f);
        }

        private static Vector3 HaloPosition(AmbianceLight l, HorrorLookSettings s)
        {
            Vector3 pos = l.transform.position;
            if (l.kind == AmbianceLight.Kind.Torche)
            {
                Vector3 f = l.transform.forward;
                f.y = 0f;
                if (f.sqrMagnitude > 0.01f)
                    pos += f.normalized * s.haloWallOffset;
            }

            return pos;
        }

        private void Add(ref int k, Vector3 pos, float size, Color color, float alpha)
        {
            ref ParticleSystem.Particle p = ref _particles[k++];
            p.position = pos;
            p.startSize = size;
            p.startLifetime = 1e6f;
            p.remainingLifetime = 1e6f;
            p.velocity = Vector3.zero;
            color.a = Mathf.Clamp01(alpha);
            p.startColor = color;
        }

        // ---------- Budget d'ombres ----------

        private readonly Dictionary<Light, float> _shadowNow = new Dictionary<Light, float>();
        private readonly HashSet<Light> _shadowWanted = new HashSet<Light>();
        private readonly List<Light> _shadowDone = new List<Light>();

        private void UpdateShadows(HorrorLookSettings s)
        {
            if (Time.time >= _nextShadow)
                PickShadows(s);
            // Fondu de l'intensité des ombres vers leur cible (0 ou 1) : une lumière qui entre ou sort du budget ne bascule pas.
            float step = Time.deltaTime / (s != null ? s.shadowFade : 0.5f);
            foreach (Light l in _shadowWanted)
                if (l != null && !_shadowNow.ContainsKey(l))
                    _shadowNow[l] = 0f;
            _shadowDone.Clear();
            int shadowed = 0;
            foreach (KeyValuePair<Light, float> kv in _shadowNow)
            {
                Light l = kv.Key;
                if (l == null)
                {
                    _shadowDone.Add(l);
                    continue;
                }

                float v = Mathf.MoveTowards(kv.Value, _shadowWanted.Contains(l) ? 1f : 0f, step);
                _pending.Add((l, v));
            }

            foreach ((Light l, float v) in _pending)
            {
                _shadowNow[l] = v;
                ApplyShadow(l, v, s);
                if (v > 0f)
                    shadowed++;
                else if (!_shadowWanted.Contains(l))
                    _shadowDone.Add(l);
            }

            _pending.Clear();
            foreach (Light l in _shadowDone)
                _shadowNow.Remove(l);
            ShadowedCount = shadowed;
        }

        private readonly List<(Light, float)> _pending = new List<(Light, float)>();

        private void PickShadows(HorrorLookSettings s)
        {
            _nextShadow = Time.time + (s != null ? s.shadowRefresh : 0.5f);
            _shadowCandidates.Clear();
            bool on = s != null && s.shadowBudget && s.shadowedLights > 0;
            float keep = s != null ? s.shadowHysteresis : 0f;
            foreach (KeyValuePair<PlayerLanternLight, Light> kv in _cones)
                if (kv.Value != null && kv.Value.enabled)
                    _shadowCandidates.Add((kv.Value, -1000f, true));      // les cônes d'abord
            if (on)
                foreach (AmbianceLight l in AmbianceLight.All)
                    if (l != null && l.Light != null && l.Light.isActiveAndEnabled)
                    {
                        float d = Near(l.transform.position);
                        if (d < s.shadowMaxDistance)
                            _shadowCandidates.Add((l.Light, _shadowWanted.Contains(l.Light) ? d - keep : d, false));   // hystérésis
                    }

            _shadowCandidates.Sort((x, y) => x.d.CompareTo(y.d));
            _shadowWanted.Clear();
            int budget = on ? s.shadowedLights : 0;
            foreach ((Light light, float _, bool _) in _shadowCandidates)
            {
                if (_shadowWanted.Count >= budget)
                    break;
                _shadowWanted.Add(light);
            }
        }

        private static void ApplyShadow(Light l, float strength, HorrorLookSettings s)
        {
            LightShadows want = strength > 0.001f ? LightShadows.Soft : LightShadows.None;
            if (l.shadows != want)
                l.shadows = want;
            l.shadowStrength = Mathf.SmoothStep(0f, 1f, strength);
            if (s != null)
            {
                l.shadowBias = s.shadowDepthBias;
                l.shadowNormalBias = s.shadowNormalBias;
                var data = l.GetComponent<UniversalAdditionalLightData>();
                if (data != null && data.usePipelineSettings)
                    data.usePipelineSettings = false;                   // biais de la lumière, pas ceux de l'asset URP
            }
        }

        // ---------- Lumières au-delà du brouillard ----------

        private readonly List<Light> _decorLights = new List<Light>();
        private readonly HashSet<Light> _culled = new HashSet<Light>();
        private float _nextDecorScan, _nextCull;

        /// <summary>Lumières du décor éteintes parce qu'elles sont au-delà du brouillard (tests, mesures).</summary>
        public int CulledCount => _culled.Count;

        private void CullLights(HorrorLookSettings s)
        {
            bool on = s != null && s.lightCulling && RenderSettings.fog && _eyes.Count > 0;
            if (!on)
            {
                foreach (Light l in _culled)
                    if (l != null)
                        l.enabled = true;
                _culled.Clear();
                return;
            }

            if (Time.unscaledTime >= _nextDecorScan)
            {
                // Lumières du décor : torches et lampes (AmbianceLight), lanternes posées (Prop_*). Jamais celles des joueurs ni des ennemis.
                _nextDecorScan = Time.unscaledTime + 2f;
                _decorLights.Clear();
                foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type != LightType.Directional && (l.GetComponent<AmbianceLight>() != null || InProp(l.transform)))
                        _decorLights.Add(l);
            }

            if (Time.time < _nextCull)
                return;
            _nextCull = Time.time + 0.1f;
            float fogEnd = RenderSettings.fogEndDistance;
            foreach (Light l in _decorLights)
            {
                if (l == null)
                    continue;
                float d = Near(l.transform.position) - l.range;
                bool culled = _culled.Contains(l);
                bool off = culled ? d > fogEnd + s.lightCullMargin - s.lightCullHysteresis : d > fogEnd + s.lightCullMargin;
                if (off == culled)
                    continue;
                if (off)
                {
                    if (!l.enabled)
                        continue;                                       // éteinte par autre chose : on n'y touche pas
                    l.enabled = false;
                    _culled.Add(l);
                }
                else
                {
                    l.enabled = true;
                    _culled.Remove(l);
                }
            }
        }

        private static bool InProp(Transform t)
        {
            for (int i = 0; i < 3 && t != null; i++, t = t.parent)
                if (t.name.StartsWith("Prop_"))
                    return true;
            return false;
        }
    }
}
