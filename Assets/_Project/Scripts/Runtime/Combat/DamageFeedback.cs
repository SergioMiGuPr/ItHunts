using System.Collections.Generic;
using ItHunts.Audio;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Retour des coups dans le monde (un seul objet dans la scène, pas par joueur) : éclat au point d'impact (sphère
    /// HDR qui grandit et s'éteint, couleur et taille selon les dégâts), éclair blanc sur la cible, sons synthétisés 3D
    /// au point d'impact (coup, plus grave pour un objet lourd ; son de mort). Effets de zone (D-027) : cercle bref au sol
    /// de la zone de dégâts (orange) et de l'onde de répulsion (couleur de la répulsion), débris d'un objet cassé, étoiles
    /// au-dessus d'un ennemi étourdi, sons de casse et d'étourdissement. Les chiffres de dégâts sont dessinés par chaque
    /// joueur (PlayerHud).
    /// </summary>
    public sealed class DamageFeedback : MonoBehaviour
    {
        [SerializeField] private DamageFeedbackSettings settings;
        [Tooltip("Matériau de l'éclat (URP Unlit, couleur pilotée par le code).")]
        [SerializeField] private Material flashMaterial;
        [SerializeField, Min(1)] private int poolSize = 16;
        [Tooltip("Couleur de l'onde de répulsion (cône).")]
        [SerializeField] private Color waveColor = new Color(1f, 0.5f, 0.1f);
        [Tooltip("Matériau des traits de l'onde en cône (couleur par sommet, transparent).")]
        [SerializeField] private Material lineMaterial;
        [Tooltip("Onde de départ (appui) : durée, intensité, épaisseur de l'anneau (m).")]
        [SerializeField, Min(0.05f)] private float coneWaveDuration = 0.35f;
        [SerializeField, Min(0f)] private float coneWaveIntensity = 4f;
        [SerializeField, Min(0.01f)] private float coneWaveWidth = 0.22f;
        [Tooltip("Répulsion maintenue : un anneau tous les … (s), durée de vie, intensité.")]
        [SerializeField, Min(0.02f)] private float holdRingInterval = 0.07f;
        [SerializeField, Min(0.05f)] private float holdRingDuration = 0.3f;
        [SerializeField, Min(0f)] private float holdRingIntensity = 1.8f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private sealed class Flash
        {
            public Transform Transform;
            public Renderer Renderer;
            public float Age = float.PositiveInfinity;
            public float Size;
            public Color Color;
        }

        private readonly List<Flash> _flashes = new List<Flash>();
        private readonly List<Flash> _rings = new List<Flash>();
        private readonly List<(Transform star, Transform target, float until, float phase)> _stars =
            new List<(Transform, Transform, float, float)>();
        private readonly List<(GameObject go, float until)> _debris = new List<(GameObject, float)>();
        private AudioClip _break, _stun;
        private sealed class ConeRing
        {
            public LineRenderer Line;
            public ConeWaveEvent Wave;
            public float Age = float.PositiveInfinity;
            public bool Strong;
        }

        private readonly List<ConeRing> _coneRings = new List<ConeRing>();
        private readonly LineRenderer[] _coneEdges = new LineRenderer[4];
        private ConeWaveEvent _edgeWave;
        private float _edgeAge = float.PositiveInfinity;
        private float _lastHoldRing = float.NegativeInfinity;
        private int _nextConeRing;
        private const int RingPoints = 32;
        private int _nextRing;
        private readonly List<AudioSource> _voices = new List<AudioSource>();
        private MaterialPropertyBlock _block;
        private AudioClip _hit, _kill;
        private int _nextFlash, _nextVoice;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _hit = ProceduralSfx.ToClip("Impact", ProceduralSfx.ImpactHit());
            _kill = ProceduralSfx.ToClip("Ennemi tué", ProceduralSfx.Kill());
            _break = ProceduralSfx.ToClip("Objet cassé", ProceduralSfx.Break());
            _stun = ProceduralSfx.ToClip("Étourdi", ProceduralSfx.Stun());
            for (int i = 0; i < 10; i++)
                _coneRings.Add(new ConeRing { Line = NewLine($"Onde de répulsion (anneau {i})", RingPoints, true) });
            for (int i = 0; i < _coneEdges.Length; i++)
                _coneEdges[i] = NewLine($"Onde de répulsion (arête {i})", 2, false);
            for (int i = 0; i < 6; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"Cercle de zone {i}";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<Renderer>();
                if (flashMaterial != null)
                    r.sharedMaterial = flashMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.enabled = false;
                _rings.Add(new Flash { Transform = go.transform, Renderer = r });
            }
            for (int i = 0; i < poolSize; i++)
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = $"Éclat d'impact {i}";
                Destroy(go.GetComponent<Collider>());
                go.transform.SetParent(transform, false);
                var r = go.GetComponent<Renderer>();
                if (flashMaterial != null)
                    r.sharedMaterial = flashMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.enabled = false;
                _flashes.Add(new Flash { Transform = go.transform, Renderer = r });
            }

            for (int i = 0; i < 8; i++)
            {
                var voice = new GameObject($"Son d'impact {i}").AddComponent<AudioSource>();
                voice.transform.SetParent(transform, false);
                voice.playOnAwake = false;
                voice.spatialBlend = 1f;
                voice.rolloffMode = AudioRolloffMode.Linear;
                _voices.Add(voice);
            }
        }

        private LineRenderer NewLine(string name, int points, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = points;
            line.loop = loop;
            line.startWidth = line.endWidth = 0.05f;
            if (lineMaterial != null)
                line.sharedMaterial = lineMaterial;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        /// <summary>
        /// Onde de répulsion en cône depuis la main droite : à l'appui, un anneau épais et lumineux qui file jusqu'au bout
        /// de la portée, avec les quatre arêtes du cône ; pendant le maintien, des anneaux plus fins émis à intervalle
        /// régulier, qui s'estompent à la fin.
        /// </summary>
        private void OnConeWave(ConeWaveEvent e)
        {
            if (e.Continuous && Time.time - _lastHoldRing < holdRingInterval)
                return;
            if (e.Continuous)
                _lastHoldRing = Time.time;
            else
            {
                _edgeWave = e;
                _edgeAge = 0f;
            }

            ConeRing ring = _coneRings[_nextConeRing];
            _nextConeRing = (_nextConeRing + 1) % _coneRings.Count;
            ring.Wave = e;
            ring.Age = 0f;
            ring.Strong = !e.Continuous;
        }

        private static void ConeFrame(in ConeWaveEvent w, float d, out Vector3 center, out float radius, out Vector3 right, out Vector3 up)
        {
            Vector3 axis = w.Direction.normalized;
            right = Vector3.Cross(Mathf.Abs(axis.y) > 0.95f ? Vector3.forward : Vector3.up, axis).normalized;
            up = Vector3.Cross(axis, right);
            center = w.Origin + axis * d;
            radius = d * Mathf.Tan(w.ConeAngle * 0.5f * Mathf.Deg2Rad);
        }

        private void UpdateCone()
        {
            foreach (ConeRing ring in _coneRings)
            {
                float duration = ring.Strong ? coneWaveDuration : holdRingDuration;
                bool visible = ring.Age < duration;
                ring.Line.enabled = visible;
                if (!visible)
                    continue;
                float t = ring.Age / duration;
                ConeFrame(ring.Wave, ring.Wave.Range * Mathf.Sqrt(t), out Vector3 center, out float radius, out Vector3 right, out Vector3 up);
                Color c = waveColor * (ring.Strong ? coneWaveIntensity : holdRingIntensity);
                c.a = (1f - t) * (ring.Strong ? 1f : 0.6f);
                ring.Line.startColor = ring.Line.endColor = c;
                float width = ring.Strong ? Mathf.Lerp(0.06f, coneWaveWidth, t) : Mathf.Lerp(0.03f, 0.08f, t);
                ring.Line.startWidth = ring.Line.endWidth = width;
                for (int i = 0; i < RingPoints; i++)
                {
                    float a = i * 2f * Mathf.PI / RingPoints;
                    ring.Line.SetPosition(i, center + (right * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius);
                }

                ring.Age += Time.deltaTime;
            }

            bool edges = _edgeAge < coneWaveDuration;
            foreach (LineRenderer edge in _coneEdges)
                if (edge != null)
                    edge.enabled = edges;
            if (!edges)
                return;
            float u = _edgeAge / coneWaveDuration;
            ConeFrame(_edgeWave, _edgeWave.Range, out Vector3 end, out float endRadius, out Vector3 r, out Vector3 upv);
            Color ec = waveColor * coneWaveIntensity;
            ec.a = (1f - u) * 0.7f;
            for (int i = 0; i < _coneEdges.Length; i++)
            {
                float a = i * Mathf.PI * 0.5f + Mathf.PI * 0.25f;
                LineRenderer edge = _coneEdges[i];
                edge.startColor = ec;
                edge.endColor = new Color(ec.r, ec.g, ec.b, 0f);
                edge.startWidth = 0.03f;
                edge.endWidth = 0.1f;
                edge.SetPosition(0, _edgeWave.Origin);
                edge.SetPosition(1, end + (r * Mathf.Cos(a) + upv * Mathf.Sin(a)) * endRadius);
            }

            _edgeAge += Time.deltaTime;
        }

        private void OnEnable()
        {
            AreaEvents.ConeWave += OnConeWave;
            DamageEvents.Hit += OnHit;
            AreaEvents.Area += OnArea;
            AreaEvents.Broken += OnBroken;
            AreaEvents.Stunned += OnStunned;
            ParryEvents.Parried += OnParried;
        }

        private void OnDisable()
        {
            AreaEvents.ConeWave -= OnConeWave;
            DamageEvents.Hit -= OnHit;
            AreaEvents.Area -= OnArea;
            AreaEvents.Broken -= OnBroken;
            AreaEvents.Stunned -= OnStunned;
            ParryEvents.Parried -= OnParried;
        }

        /// <summary>Cercle bref au point : zone de dégâts (orange) ou onde de répulsion.</summary>
        private void OnArea(AreaImpactEvent e)
        {
            if (settings == null)
                return;
            Flash ring = _rings[_nextRing];
            _nextRing = (_nextRing + 1) % _rings.Count;
            ring.Age = 0f;
            ring.Size = e.Radius;
            ring.Color = e.Wave ? waveColor : settings.areaColor;
            ring.Transform.position = e.Point;
        }

        /// <summary>Objet cassé : éclat, débris (petits cubes qui volent puis disparaissent), son.</summary>
        private void OnBroken(BreakEvent e)
        {
            if (settings == null)
                return;
            Flash f = _flashes[_nextFlash];
            _nextFlash = (_nextFlash + 1) % _flashes.Count;
            f.Age = 0f;
            f.Size = Mathf.Clamp(e.Size.magnitude, 0.5f, settings.flashMaxSize);
            f.Color = settings.areaColor;
            f.Transform.position = e.Point;

            float piece = Mathf.Max(0.08f, Mathf.Min(e.Size.x, Mathf.Min(e.Size.y, e.Size.z)) * 0.35f);
            for (int i = 0; i < settings.debrisCount; i++)
            {
                GameObject d = GameObject.CreatePrimitive(PrimitiveType.Cube);
                d.name = "Débris";
                d.transform.position = e.Point + Random.insideUnitSphere * piece;
                d.transform.rotation = Random.rotation;
                d.transform.localScale = Vector3.one * piece * Random.Range(0.6f, 1.2f);
                var r = d.GetComponent<Renderer>();
                r.material.color = e.Color;
                var body = d.AddComponent<Rigidbody>();
                body.mass = 0.5f;
                body.linearVelocity = e.Velocity * 0.3f + (Random.onUnitSphere + Vector3.up * 0.8f).normalized * settings.debrisSpeed;
                body.angularVelocity = Random.insideUnitSphere * 10f;
                _debris.Add((d, Time.time + settings.debrisLifetime));
            }

            Play(_break, e.Point, settings.breakVolume, Random.Range(0.9f, 1.1f));
        }

        private AudioClip _parryClip;

        /// <summary>Parade réussie (D-075) : grand éclat blanc doré entre le joueur et la goule, tintement (provisoire, en attendant
        /// l'éclair de parade de la DA).</summary>
        private void OnParried(ParryEvent e)
        {
            if (settings == null || _flashes.Count == 0)
                return;
            ParrySettings cfg = e.Player != null && e.Player.TryGetComponent(out ItHunts.Player.PlayerParry pp) ? pp.Settings : null;
            // Sur la goule (pas entre elle et la caméra : à 1 m des yeux, l'éclat couvrait tout l'écran).
            Vector3 at = e.Point;
            if (e.Attacker != null)
            {
                Vector3 chest = e.Attacker.position + Vector3.up * 1f;
                Vector3 away = chest - e.Point;
                away.y = 0f;
                at = chest + (away.sqrMagnitude > 1e-4f ? away.normalized * 0.3f : Vector3.zero);
            }
            Flash f = _flashes[_nextFlash];
            _nextFlash = (_nextFlash + 1) % _flashes.Count;
            f.Age = 0f;
            f.Size = cfg != null ? cfg.flashSize : 0.7f;
            f.Color = cfg != null ? cfg.flashColor : new Color(3f, 2.6f, 1.4f, 1f);
            f.Transform.position = at;
            if (_parryClip == null)
                _parryClip = ItHunts.Audio.ProceduralSfx.ToClip("Parade", ItHunts.Audio.ProceduralSfx.Parry());
            Play(_parryClip, at, cfg != null ? cfg.volume : 0.8f, Random.Range(0.96f, 1.04f));
        }

        /// <summary>Ennemi étourdi : trois étoiles jaunes tournent au-dessus de sa tête, gazouillis.</summary>
        private void OnStunned(StunEvent e)
        {
            if (settings == null)
                return;
            for (int i = 0; i < 3; i++)
            {
                GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = "Étoile d'étourdissement";
                Destroy(star.GetComponent<Collider>());
                star.transform.localScale = Vector3.one * 0.18f;
                var r = star.GetComponent<Renderer>();
                if (flashMaterial != null)
                    r.sharedMaterial = flashMaterial;
                var block = new MaterialPropertyBlock();
                block.SetColor(BaseColorId, settings.stunColor * settings.stunGlowIntensity * 2f);
                r.SetPropertyBlock(block);
                _stars.Add((star.transform, e.Target, Time.time + e.Duration, i * 2f * Mathf.PI / 3f));
            }

            Play(_stun, e.Point, settings.stunVolume, 1f);
        }

        public Color TierColor(DamageEvent e) => e.Killed ? settings.killColor
            : e.Tier == HitTier.Gros ? settings.bigColor
            : e.Tier == HitTier.Moyen ? settings.mediumColor
            : settings.smallColor;

        private void OnHit(DamageEvent e)
        {
            if (settings == null)
                return;
            Color color = TierColor(e);

            // Éclat au point d'impact (plus gros à la mort).
            Flash f = _flashes[_nextFlash];
            _nextFlash = (_nextFlash + 1) % _flashes.Count;
            f.Age = 0f;
            f.Size = Mathf.Min(settings.flashMaxSize, settings.flashBaseSize + settings.flashSizePerDamage * e.Damage) * (e.Killed ? 1.6f : 1f);
            f.Color = color;
            f.Transform.position = e.Point;

            if (e.Target != null)
                e.Target.Flash(Color.white * settings.targetFlashIntensity, settings.targetFlashDuration);

            // Son 3D au point d'impact : plus grave pour un objet lourd, plus fort pour un gros coup.
            float heavy = Mathf.InverseLerp(5f, 40f, e.SourceMass);
            Play(_hit, e.Point, settings.hitVolume * Mathf.Lerp(0.5f, 1f, Mathf.InverseLerp(0f, 60f, e.Damage)),
                Mathf.Lerp(settings.hitPitchLight, settings.hitPitchHeavy, heavy));
            if (e.Killed)
                Play(_kill, e.Point, settings.killVolume, 1f);
        }

        private void UpdateRings()
        {
            foreach (Flash ring in _rings)
            {
                bool visible = ring.Age < settings.areaRingDuration;
                ring.Renderer.enabled = visible;
                if (!visible)
                    continue;
                float t = ring.Age / settings.areaRingDuration;
                float diameter = ring.Size * 2f * (0.3f + 0.7f * Mathf.Sqrt(t));
                ring.Transform.localScale = new Vector3(diameter, 0.01f, diameter);
                _block.SetColor(BaseColorId, Color.Lerp(ring.Color * settings.areaRingIntensity, Color.black, t));
                ring.Renderer.SetPropertyBlock(_block);
                ring.Age += Time.deltaTime;
            }
        }

        private void UpdateStars()
        {
            for (int i = _stars.Count - 1; i >= 0; i--)
            {
                (Transform star, Transform target, float until, float phase) = _stars[i];
                if (star == null || target == null || Time.time >= until)
                {
                    if (star != null)
                        Destroy(star.gameObject);
                    _stars.RemoveAt(i);
                    continue;
                }

                float a = phase + Time.time * 6f;
                Collider col = target.GetComponent<Collider>();
                Vector3 top = col != null ? new Vector3(col.bounds.center.x, col.bounds.max.y + 0.3f, col.bounds.center.z) : target.position + Vector3.up * 2f;
                star.position = top + new Vector3(Mathf.Cos(a), 0.1f * Mathf.Sin(a * 2f), Mathf.Sin(a)) * 0.45f;
            }
        }

        private void Play(AudioClip clip, Vector3 point, float volume, float pitch)
        {
            AudioSource voice = _voices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _voices.Count;
            voice.transform.position = point;
            voice.minDistance = settings.soundMinDistance;
            voice.maxDistance = settings.soundMaxDistance;
            voice.pitch = pitch;
            voice.volume = volume;
            voice.clip = clip;
            voice.Play();
        }

        private void Update()
        {
            if (settings == null)
                return;
            UpdateRings();
            UpdateStars();
            UpdateCone();
            for (int i = _debris.Count - 1; i >= 0; i--)
            {
                if (_debris[i].go != null && Time.time < _debris[i].until)
                    continue;
                if (_debris[i].go != null)
                    Destroy(_debris[i].go);
                _debris.RemoveAt(i);
            }

            foreach (Flash f in _flashes)
            {
                bool visible = f.Age < settings.flashDuration;
                f.Renderer.enabled = visible;
                if (!visible)
                    continue;
                float t = f.Age / settings.flashDuration;
                f.Transform.localScale = Vector3.one * (f.Size * (0.4f + 0.6f * Mathf.Sqrt(t)));
                _block.SetColor(BaseColorId, Color.Lerp(f.Color * settings.flashIntensity, Color.black, t));
                f.Renderer.SetPropertyBlock(_block);
                f.Age += Time.deltaTime;
            }
        }
    }
}
