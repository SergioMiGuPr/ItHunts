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
        private ChannelMixer _mixer;
        private ZoneState _zone;
        private float _nextSearch;

        /// <summary>Avancement de la bascule rouge sang (0..1).</summary>
        public float Monochrome { get; private set; }

        private ParticleSystem _halos;
        private ParticleSystem.Particle[] _particles = new ParticleSystem.Particle[0];
        private readonly List<PlayerLanternLight> _lanterns = new List<PlayerLanternLight>();
        private readonly Dictionary<PlayerLanternLight, Light> _cones = new Dictionary<PlayerLanternLight, Light>();
        private readonly List<Vector3> _eyes = new List<Vector3>();
        private readonly HashSet<AmbianceLight> _moved = new HashSet<AmbianceLight>();
        private readonly Dictionary<AmbianceLight, float> _buzzUntil = new Dictionary<AmbianceLight, float>();
        private readonly Dictionary<AmbianceLight, float> _buzzNext = new Dictionary<AmbianceLight, float>();
        private readonly List<(Light light, float d, bool cone)> _shadowCandidates = new List<(Light, float, bool)>();
        private float _nextShadow;

        /// <summary>Lumières qui projettent des ombres (tests, mesures).</summary>
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
            _mixer = _profile.Add<ChannelMixer>(true);
            _volume.sharedProfile = _profile;
        }

        private void OnDestroy()
        {
            if (Current == this)
                Current = null;
            if (_profile != null)
                Destroy(_profile);
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
            _color.postExposure.Override((grade ? a.postExposure : 0f) + s.enragedExposure * m);
            _color.contrast.Override((grade ? a.contrast : 0f) + s.enragedContrast * m);
            _color.saturation.Override(grade ? a.saturation : 0f);
            _color.colorFilter.Override(grade ? a.colorFilter : Color.white);
            // Enragé : monochrome rouge sang par le mélangeur de canaux (sortie = luminance × teinte), en gardant une part de couleur.
            _mixer.active = m > 0f;
            float w = m * (1f - s.enragedKeepColor);
            Vector3 lum = new Vector3(0.2126f, 0.7152f, 0.0722f);
            Vector3 Row(Vector3 identity, float tint) => Vector3.Lerp(identity, lum * tint, w) * 100f;
            Vector3 r = Row(new Vector3(1f, 0f, 0f), s.enragedTint.r), g = Row(new Vector3(0f, 1f, 0f), s.enragedTint.g), b = Row(new Vector3(0f, 0f, 1f), s.enragedTint.b);
            _mixer.redOutRedIn.Override(r.x);
            _mixer.redOutGreenIn.Override(r.y);
            _mixer.redOutBlueIn.Override(r.z);
            _mixer.greenOutRedIn.Override(g.x);
            _mixer.greenOutGreenIn.Override(g.y);
            _mixer.greenOutBlueIn.Override(g.z);
            _mixer.blueOutRedIn.Override(b.x);
            _mixer.blueOutGreenIn.Override(b.y);
            _mixer.blueOutBlueIn.Override(b.z);
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
                    go.transform.SetParent(cam.transform, false);
                    cone = go.AddComponent<Light>();
                    cone.type = LightType.Spot;
                    cone.shadows = LightShadows.None;
                    go.AddComponent<UniversalAdditionalLightData>();
                    _cones[lantern] = cone;
                }

                bool on = s != null && s.lanternCone && lantern.Light.enabled;
                cone.enabled = on;
                if (!on)
                    continue;
                cone.transform.localPosition = s.lanternConeOffset;
                cone.transform.localRotation = Quaternion.identity;
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
                    l.transform.localPosition = l.BaseLocalPosition + j;
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
                    if (l == null || l.Light == null || !l.Light.isActiveAndEnabled)
                        continue;
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

        private void UpdateShadows(HorrorLookSettings s)
        {
            if (Time.time < _nextShadow)
                return;
            _nextShadow = Time.time + (s != null ? s.shadowRefresh : 0.5f);
            _shadowCandidates.Clear();
            bool on = s != null && s.shadowBudget && s.shadowedLights > 0;
            foreach (KeyValuePair<PlayerLanternLight, Light> kv in _cones)
                if (kv.Value != null)
                    _shadowCandidates.Add((kv.Value, -1f, true));          // les cônes d'abord
            if (on)
                foreach (AmbianceLight l in AmbianceLight.All)
                    if (l != null && l.Light != null && l.Light.isActiveAndEnabled)
                    {
                        float d = Near(l.transform.position);
                        if (d < s.shadowMaxDistance)
                            _shadowCandidates.Add((l.Light, d, false));
                    }

            _shadowCandidates.Sort((x, y) => x.d.CompareTo(y.d));
            int budget = on ? s.shadowedLights : 0, used = 0;
            var lit = new HashSet<Light>();
            foreach ((Light light, float _, bool cone) in _shadowCandidates)
            {
                if (used >= budget || (cone && !light.enabled))
                    continue;
                lit.Add(light);
                used++;
            }

            ShadowedCount = used;
            foreach (KeyValuePair<PlayerLanternLight, Light> kv in _cones)
                if (kv.Value != null)
                    SetShadows(kv.Value, lit.Contains(kv.Value));
            foreach (AmbianceLight l in AmbianceLight.All)
                if (l != null && l.Light != null)
                    SetShadows(l.Light, lit.Contains(l.Light));
        }

        private static void SetShadows(Light l, bool on)
        {
            LightShadows want = on ? LightShadows.Soft : LightShadows.None;
            if (l.shadows != want)
                l.shadows = want;
        }
    }
}
