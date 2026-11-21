using System.Linq;
using ItHunts.Audio;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// Retour visuel et sonore de la lanterne de feu (D-065), sur le prefab du joueur. Lit l'état simulé (ArtifactHolder) à chaque
    /// image, n'écrit rien dans la simulation.
    /// - Lanterne greybox en main gauche (le modèle de la DA arrivera par une PR) ; sa flamme brille quand elle est prête,
    ///   s'éteint à la fin du jet et se rallume peu à peu pendant la recharge (petit son quand elle est de nouveau prête).
    /// - Jet : bouffées de flammes (sphères sans collision, couleur HDR qui passe du jaune au rouge) lancées depuis la lanterne
    ///   dans le cône du viseur ; elles s'arrêtent sur le décor. Visibles par tous les joueurs (objets du monde).
    /// Cachée si l'artefact du joueur n'est pas la lanterne.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class FireLanternView : MonoBehaviour
    {
        [SerializeField] private Transform lanternRoot;
        [Tooltip("Flamme dans la cage de la lanterne (émission réglée par le code).")]
        [SerializeField] private Renderer flame;
        [Tooltip("Sortie du jet (repère FX_Jet du modèle SM_LanterneFeu).")]
        [SerializeField] private Transform muzzle;
        [Tooltip("Verre de la lanterne (SM_LanterneFeu_Verre) : allumé par _EmissionColor à la couleur du personnage, éteint pendant la recharge.")]
        [SerializeField] private Renderer glass;
        [Tooltip("Intensité de l'émission du verre allumé (× couleur du personnage).")]
        [SerializeField] private float glassIntensity = 2.5f;
        [Tooltip("Maillage et matériau des bouffées de flammes (sphère, Particles/Unlit).")]
        [SerializeField] private Mesh puffMesh;
        [SerializeField] private Material puffMaterial;

        private const int MaxPuffs = 64;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private struct Puff
        {
            public float Born;
            public Vector3 From;
            public Vector3 Dir;
            public float Reach;
            public float Size;
        }

        private ArtifactHolder _holder;
        private PlayerMotor _motor;
        private FirstPersonView _view;
        private Camera _camera;
        private Transform _puffRoot;
        private readonly Transform[] _puffT = new Transform[MaxPuffs];
        private readonly MeshRenderer[] _puffR = new MeshRenderer[MaxPuffs];
        private readonly Puff[] _puffs = new Puff[MaxPuffs];
        private int _next;
        private float _spawnDebt;
        private MaterialPropertyBlock _block, _glassBlock;
        private AudioSource _audio;
        private AudioClip _jet, _relight;
        private AudioSource _loop;
        private float _jetClipFor = -1f;
        private int _seenJet;
        private bool _wasReady = true;
        private System.Random _rng;

        /// <summary>Mesures : lueur affichée de la lanterne (0 éteinte, 1 prête) et bouffées visibles.</summary>
        public float ShownGlow { get; private set; }
        public int ShownPuffs { get; private set; }

        private void Awake()
        {
            _holder = GetComponent<ArtifactHolder>();
            _motor = GetComponent<PlayerMotor>();
            _view = GetComponent<FirstPersonView>();
            var input = GetComponent<PlayerInput>();
            _camera = input != null ? input.camera : null;
            _block = new MaterialPropertyBlock();
            _glassBlock = new MaterialPropertyBlock();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _relight = ProceduralSfx.ToClip("Lanterne rallumée", ProceduralSfx.LanternRelight());
            _rng = new System.Random(GetInstanceID());
            // Flamme de la DA (SM_LanterneFeu_Flamme, M_LanterneFeu_Flamme) à la place de la sphère greybox, si elle est là.
            if (lanternRoot != null)
            {
                Renderer da = lanternRoot.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name == "SM_LanterneFeu_Flamme");
                if (da != null)
                {
                    if (flame != null && flame != da)
                        flame.enabled = false;
                    flame = da;
                    _daFlame = true;
                }
            }

            _flameScale = flame != null ? flame.transform.localScale : Vector3.one;
        }

        private Vector3 _flameScale;
        private bool _daFlame;

        [Tooltip("Flamme de la DA : intensité de l'émission (× couleur du personnage) quand elle brûle.")]
        [SerializeField] private float flameIntensity = 4f;

        /// <summary>Émission de la flamme au centre (mesures, tests).</summary>
        public Color ShownFlame { get; private set; }

        private void OnDestroy()
        {
            if (_jetVfx != null)
                Destroy(_jetVfx.gameObject);
            if (_puffRoot != null)
                Destroy(_puffRoot.gameObject);
        }

        private ItHunts.Art.VFX.VfxPeint _jetVfx;
        private bool _jetVfxOn;

        /// <summary>Jet peint de la DA (VFX_Lanterne_Jet, PR #22) : remplace les bouffées s'il existe.</summary>
        public bool PaintedJet => ItHunts.Combat.GameVfx.Settings != null && ItHunts.Combat.GameVfx.Settings.lanternJet != null;

        private void UpdatePaintedJet(bool jetting)
        {
            if (_jetVfx == null)
            {
                GameObject go = ItHunts.Combat.GameVfx.Spawn(ItHunts.Combat.GameVfx.Settings.lanternJet, transform.position, Quaternion.identity, 1f, null, 0f);
                if (go == null)
                    return;
                go.name = $"Jet de la lanterne ({name})";
                _jetVfx = go.GetComponent<ItHunts.Art.VFX.VfxPeint>();
                if (_jetVfx == null)
                    _jetVfx = go.AddComponent<ItHunts.Art.VFX.VfxPeint>();
                _jetVfx.Arrete();
            }

            Vector3 from = muzzle != null && _view != null ? _view.ViewToWorld(muzzle.position) : transform.position + Vector3.up * 1.5f;
            Transform cam = _camera != null ? _camera.transform : transform;
            _jetVfx.transform.SetPositionAndRotation(from, Quaternion.LookRotation(cam.forward, Vector3.up));
            if (jetting && !_jetVfxOn)
                _jetVfx.Joue();
            else if (!jetting && _jetVfxOn)
                _jetVfx.Arrete();
            _jetVfxOn = jetting;
        }

        private void EnsurePuffs()
        {
            if (_puffRoot != null || puffMesh == null || puffMaterial == null || PaintedJet)
                return;
            _puffRoot = new GameObject($"Flammes de la lanterne ({name})").transform;
            for (int i = 0; i < MaxPuffs; i++)
            {
                var go = new GameObject("Bouffée");
                go.transform.SetParent(_puffRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = puffMesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = puffMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                r.enabled = false;
                _puffT[i] = go.transform;
                _puffR[i] = r;
                _puffs[i].Born = float.NegativeInfinity;
            }
        }

        private void LateUpdate()
        {
            FireLanternSettings cfg = _holder != null ? _holder.LanternSettings : null;
            if (lanternRoot != null && lanternRoot.gameObject.activeSelf != (cfg != null))
                lanternRoot.gameObject.SetActive(cfg != null);
            if (cfg == null)
            {
                HidePuffs();
                return;
            }

            EnsurePuffs();
            FpsViewSettings fps = _view != null ? _view.Settings : null;
            if (lanternRoot != null && fps != null)                         // réglable en Play Mode ; flotte au-dessus de la paume
                BarrelView.PlaceFloating(lanternRoot, fps.lanternInHand, fps.lanternInHandEuler, fps.lanternScale, fps, _view);

            LanternState s = _holder.Lantern;
            if (_view != null && _view.Preview == ViewPreview.Jet)
                s.Jetting = true;                                            // aperçu de la vitrine (copie locale de l'état)
            int tick = _motor != null ? _motor.State.Tick : 0;
            float dt = Time.fixedDeltaTime;
            bool ready = FireLantern.Ready(s, tick, cfg, dt);

            // Nouveau jet : son à la durée du jet.
            if (s.JetCount != _seenJet)
            {
                _seenJet = s.JetCount;
                if (!Mathf.Approximately(_jetClipFor, cfg.jetDuration))
                {
                    _jetClipFor = cfg.jetDuration;
                    _jet = ProceduralSfx.ToClip("Jet de la lanterne", ProceduralSfx.LanternJet(cfg.jetDuration));
                }

                if (s.Jetting)
                {
                    var art = ItHunts.Audio.GameAudio.Settings;          // début puis boucle de la DA (PR #20), sinon le jet provisoire
                    ItHunts.Audio.GameAudio.PlayOn(_audio, art != null ? art.lanternJetStart : null, _jet, cfg.jetVolume);
                    if (art != null && art.lanternJetLoop != null)
                    {
                        if (_loop == null)
                        {
                            _loop = gameObject.AddComponent<AudioSource>();
                            _loop.playOnAwake = false;
                            _loop.spatialBlend = 0f;
                            _loop.loop = true;
                        }

                        _loop.clip = art.lanternJetLoop;
                        _loop.volume = cfg.jetVolume;
                        if (!_loop.isPlaying)
                            _loop.Play();
                    }
                }
            }

            // Boucle du jet : tant que le jet dure, puis fondu court.
            if (_loop != null && _loop.isPlaying && !s.Jetting)
            {
                var a = ItHunts.Audio.GameAudio.Settings;
                _loop.volume = Mathf.MoveTowards(_loop.volume, 0f, cfg.jetVolume * Time.deltaTime / Mathf.Max(0.01f, a != null ? a.lanternLoopFade : 0.15f));
                if (_loop.volume <= 0.001f)
                    _loop.Stop();
            }

            if (ready && !_wasReady && s.JetCount > 0)
                _audio.PlayOneShot(_relight, cfg.relightVolume);
            _wasReady = ready;

            // Flamme de la lanterne : vive pendant le jet, éteinte à sa fin, puis se rallume avec la recharge (scintille un peu).
            float glow = s.Jetting ? 1.4f : Mathf.SmoothStep(0f, 1f, FireLantern.CooldownProgress(s, tick, cfg, dt));
            float flicker = 0.9f + 0.1f * Mathf.PerlinNoise(Time.time * 9f, GetInstanceID() * 0.01f);
            ShownGlow = glow;
            if (glass != null)
            {
                Color c = _holder.Character != null ? _holder.Character.color : new Color(0.9f, 0.22f, 0.16f);
                float kg = glow * flicker * (s.Inactive ? cfg.inactiveFactor : 1f);
                glass.GetPropertyBlock(_glassBlock);
                _glassBlock.SetColor(EmissionColorId, c * (glassIntensity * kg));
                glass.SetPropertyBlock(_glassBlock);
            }

            if (flame != null && _daFlame)
            {
                // Flamme de la DA : couleur du personnage ; éteinte pendant la recharge, rallumée quand la lanterne est prête.
                Color pc = _holder.Character != null ? _holder.Character.color : new Color(0.9f, 0.22f, 0.16f);
                float k = (s.Jetting ? 1.4f : ready ? 1f : 0f) * flicker * (s.Inactive ? cfg.inactiveFactor : 1f);
                flame.enabled = k > 0.001f;
                ShownFlame = pc * (flameIntensity * k);
                flame.GetPropertyBlock(_block);
                _block.SetColor(EmissionColorId, ShownFlame);
                flame.SetPropertyBlock(_block);
            }
            else if (flame != null)
            {
                float k = glow * flicker * (s.Inactive ? cfg.inactiveFactor : 1f);
                flame.transform.localScale = _flameScale * Mathf.Lerp(0.25f, 1f, Mathf.Clamp01(glow));
                flame.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, cfg.lanternGlow * k);
                _block.SetColor(EmissionColorId, cfg.lanternGlow * k);
                flame.SetPropertyBlock(_block);
            }

            if (PaintedJet)
                UpdatePaintedJet(s.Jetting);

            // Bouffées : lancées pendant le jet depuis la lanterne, dans le cône du viseur.
            if (s.Jetting && _puffRoot != null)
            {
                _spawnDebt += cfg.puffsPerSecond * Time.deltaTime;
                Vector3 from = muzzle != null && _view != null ? _view.ViewToWorld(muzzle.position) : transform.position + Vector3.up * 1.5f;
                Transform cam = _camera != null ? _camera.transform : transform;
                while (_spawnDebt >= 1f)
                {
                    _spawnDebt -= 1f;
                    Spawn(from, cam, cfg);
                }
            }
            else
                _spawnDebt = 0f;

            UpdatePuffs(cfg);
        }

        private void Spawn(Vector3 from, Transform cam, FireLanternSettings cfg)
        {
            // Direction au hasard dans le cône (plus dense au centre), portée raccourcie par le décor.
            float a = (float)_rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Sqrt((float)_rng.NextDouble()) * Mathf.Tan(cfg.HalfAngle * Mathf.Deg2Rad) * 0.85f;
            Vector3 dir = (cam.forward + cam.right * (Mathf.Cos(a) * r) + cam.up * (Mathf.Sin(a) * r)).normalized;
            float reach = cfg.range;
            if (Physics.Raycast(from, dir, out RaycastHit hit, cfg.range, cfg.hitMask, QueryTriggerInteraction.Ignore)
                && hit.collider.attachedRigidbody == null && !hit.transform.IsChildOf(transform)
                && hit.collider.GetComponentInParent<ItHunts.Combat.Damageable>() == null)
                reach = Mathf.Max(0.2f, hit.distance - 0.2f);
            _puffs[_next] = new Puff
            {
                Born = Time.time, From = from + dir * 0.25f, Dir = dir, Reach = Mathf.Max(0.1f, reach - 0.25f),
                Size = 0.8f + 0.4f * (float)_rng.NextDouble(),
            };
            _next = (_next + 1) % MaxPuffs;
        }

        private void UpdatePuffs(FireLanternSettings cfg)
        {
            int shown = 0;
            for (int i = 0; i < MaxPuffs; i++)
            {
                if (_puffR[i] == null)
                    continue;
                float u = (Time.time - _puffs[i].Born) / cfg.puffLifetime;
                bool on = u >= 0f && u < 1f;
                _puffR[i].enabled = on;
                if (!on)
                    continue;
                shown++;
                float travel = 1f - (1f - u) * (1f - u);                 // part vite, ralentit
                _puffT[i].position = _puffs[i].From + _puffs[i].Dir * (_puffs[i].Reach * travel) + Vector3.up * (0.4f * u * u);
                _puffT[i].localScale = Vector3.one * (Mathf.Lerp(cfg.puffStartSize, cfg.puffEndSize, travel) * _puffs[i].Size);
                // Additif : fondu d'apparition (pas d'éclat plein écran près de la caméra) et de disparition.
                Color c = Color.Lerp(cfg.flameStartColor, cfg.flameEndColor, u) * (Mathf.SmoothStep(0f, 1f, u / 0.2f) * (1f - u * u));
                _block.Clear();
                _block.SetColor(BaseColorId, c);
                _puffR[i].SetPropertyBlock(_block);
            }

            ShownPuffs = shown;
        }

        private void HidePuffs()
        {
            for (int i = 0; i < MaxPuffs; i++)
                if (_puffR[i] != null)
                    _puffR[i].enabled = false;
        }
    }
}
