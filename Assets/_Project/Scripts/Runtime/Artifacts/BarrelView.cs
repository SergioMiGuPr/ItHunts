using ItHunts.Audio;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// Retour visuel et sonore du barillet (D-032), sur le prefab du joueur. Lit l'état simulé (ArtifactHolder) à chaque
    /// image, n'écrit rien dans la simulation.
    /// - Modèle SM_Barillet (étape 10) en main gauche : position, rotation et échelle réglables (FpsViewSettings).
    /// - Munitions lisibles sur les 6 chambres : chargée = émission allumée (BarrelSettings) ; vide = couleur de base seule
    ///   (#1A120C) ; en rechargement, elles se rallument une à une ; atténuées quand le barillet est inactif (objet tenu).
    ///   Un matériau partagé (copie de celui des chambres, émission activée) et un MaterialPropertyBlock par chambre.
    /// - −60° autour du Z local à chaque tir (BarrelSpin : animation courte, toujours finie sur un multiple de 60°).
    /// - L'état visuel est dérivé du compteur de munitions et de tirs du joueur (BarrelModel), sans état à part (coop).
    /// - Traînée lumineuse brève de la bouche au point touché ; éclat à l'impact ; sons provisoires.
    /// Le barillet entier est caché si l'artefact du joueur n'est pas le barillet.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class BarrelView : MonoBehaviour
    {
        [SerializeField] private Transform barrelRoot;
        [Tooltip("Partie qui tourne de −60° à chaque tir (le modèle SM_Barillet).")]
        [SerializeField] private Transform cylinder;
        [Tooltip("Les 6 chambres, dans l'ordre 01 à 06 (01 en haut au repos).")]
        [SerializeField] private Renderer[] chambers;
        [Tooltip("Bouche du canon : départ de la traînée.")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Renderer impact;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.BarrelView");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private ArtifactHolder _holder;
        private PlayerMotor _motor;
        private FirstPersonView _view;
        private MaterialPropertyBlock _block;
        private AudioSource _audio;
        private AudioClip _shot, _reloadStart, _reloadDone;
        private int _seenShotTick = MovementState.NeverTick;
        private int _seenReloadStart = MovementState.NeverTick, _seenReloadDone = MovementState.NeverTick;
        private float _shotTime = float.NegativeInfinity;
        private Vector3 _tracerFrom, _tracerTo;
        private BarrelSpin _spin;
        private MaterialPropertyBlock _chamberBlock;
        private Material _chamberMaterial;

        private int _previewShots;

        /// <summary>
        /// Tir d'aperçu (vitrine) : traînée de la chambre active jusqu'à 20 m devant la vue, un cran de rotation, recul de la main ;
        /// rien dans la simulation.
        /// </summary>
        public void PreviewShot()
        {
            _previewShots++;
            _shotTime = Time.time;
            Transform cam = _view != null && _view.ViewRoot != null ? _view.ViewRoot : transform;
            _tracerFrom = muzzle != null && _view != null ? _view.ViewToWorld(muzzle.position) : cam.position;
            _tracerTo = cam.position + cam.forward * 20f;
            if (_view != null)
                _view.PreviewRecoil();
            BarrelSettings cfg = _holder != null ? _holder.BarrelSettings : null;
            if (cfg != null)
                _audio.PlayOneShot(_shot, cfg.shotVolume);
        }

        /// <summary>Mesures : angle affiché du barillet (Z local, degrés) et nombre de chambres allumées.</summary>
        public float ShownAngle { get; private set; }
        public int ShownLit { get; private set; }

        private void Awake()
        {
            _holder = GetComponent<ArtifactHolder>();
            _motor = GetComponent<PlayerMotor>();
            _view = GetComponent<FirstPersonView>();
            _block = new MaterialPropertyBlock();
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _shot = ProceduralSfx.ToClip("Tir du barillet", ProceduralSfx.BarrelShot());
            _reloadStart = ProceduralSfx.ToClip("Rechargement", ProceduralSfx.BarrelReloadStart());
            _reloadDone = ProceduralSfx.ToClip("Rechargé", ProceduralSfx.BarrelReloadDone());
            if (tracer != null)
                tracer.enabled = false;
            if (impact != null)
                impact.enabled = false;
            _chamberBlock = new MaterialPropertyBlock();
            _chamberMaterial = ChamberMaterial(chambers);
        }

        private void OnDestroy()
        {
            if (_chamberMaterial != null)
                Destroy(_chamberMaterial);
        }

        /// <summary>
        /// Matériau des chambres : une copie de celui du modèle (M_Barillet_Chambre), émission activée, partagée par les 6 ;
        /// chaque chambre règle son émission par un MaterialPropertyBlock (elles ne s'allument pas toutes ensemble).
        /// </summary>
        public static Material ChamberMaterial(Renderer[] chambers)
        {
            if (chambers == null || chambers.Length == 0 || chambers[0] == null || chambers[0].sharedMaterial == null)
                return null;
            var m = new Material(chambers[0].sharedMaterial) { name = chambers[0].sharedMaterial.name + " (chambres, émission)" };
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            m.SetColor(EmissionColorId, Color.black);
            foreach (Renderer r in chambers)
                if (r != null)
                    r.sharedMaterial = m;
            return m;
        }

        /// <summary>
        /// Applique l'état au modèle : rotation (Z local) et chambres (émission des chargées, couleur de base de toutes).
        /// Renvoie le nombre de chambres allumées. Ne dépend que de ses arguments (testable).
        /// </summary>
        public static int ApplyModel(Transform spin, Renderer[] chambers, MaterialPropertyBlock block, float angle, int shotCount,
            int lit, bool inactive, BarrelSettings cfg)
        {
            if (spin != null)
                spin.localRotation = Quaternion.Euler(0f, 0f, angle);
            int on = 0;
            if (chambers == null)
                return 0;
            Color glow = cfg.chamberEmission * cfg.chamberEmissionIntensity * (inactive ? cfg.chamberInactiveFactor : 1f);
            for (int i = 0; i < chambers.Length; i++)
            {
                if (chambers[i] == null)
                    continue;
                bool loaded = BarrelModel.Loaded(i, shotCount, lit);
                if (loaded)
                    on++;
                chambers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, cfg.chamberBaseColor);
                block.SetColor(EmissionColorId, loaded ? glow : Color.black);
                chambers[i].SetPropertyBlock(block);
            }

            return on;
        }

        /// <summary>
        /// Artefact qui flotte au-dessus de la paume gauche (retours du lot, point 6) : point de la paume (réglage), plus la hauteur
        /// de flottement le long de la normale de la paume ; orienté comme la vue (il vise toujours devant), avec une légère
        /// oscillation. Partagé par la lanterne de feu.
        /// </summary>
        public static void PlaceFloating(Transform root, Vector3 inHand, Vector3 euler, float scale, FpsViewSettings fps, FirstPersonView view)
        {
            Transform hand = root.parent;
            float t = Time.time;
            float h = ViewmodelMotion.FloatHeight(t, fps, fps.floatPhaseOffset);
            root.localPosition = inHand + Vector3.up * h;
            Quaternion facing = view != null && view.ViewRoot != null ? view.ViewRoot.rotation : hand != null ? hand.rotation : root.rotation;
            root.rotation = facing * Quaternion.Euler(euler + ViewmodelMotion.FloatWobble(t, fps, fps.floatPhaseOffset));
            root.localScale = Vector3.one * scale;
        }

        private void LateUpdate()
        {
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            BarrelSettings cfg = _holder != null ? _holder.BarrelSettings : null;
            if (barrelRoot != null && barrelRoot.gameObject.activeSelf != (cfg != null))
                barrelRoot.gameObject.SetActive(cfg != null);
            if (cfg == null)
                return;

            BarrelState b = _holder.Barrel;
            int tick = _motor != null ? _motor.State.Tick : 0;
            FpsViewSettings fps = _view != null ? _view.Settings : null;
            if (barrelRoot != null && fps != null)                          // réglable en Play Mode
                PlaceFloating(barrelRoot, fps.barrelInHand, fps.barrelInHandEuler, fps.barrelScale, fps, _view);

            // Nouveau tir : traînée, impact, son, un cran de rotation.
            if (b.LastShotTick != MovementState.NeverTick && b.LastShotTick != _seenShotTick)
            {
                _seenShotTick = b.LastShotTick;
                _shotTime = Time.time;
                BarrelShot shot = _holder.LastShot;
                _tracerFrom = muzzle != null && _view != null ? _view.ViewToWorld(muzzle.position) : shot.Origin;
                _tracerTo = shot.Point;
                if (impact != null)
                {
                    impact.transform.position = shot.Point + shot.Normal * 0.02f;
                    impact.enabled = shot.Kind != BarrelHitKind.Rien;
                }

                _audio.PlayOneShot(_shot, cfg.shotVolume);
            }

            if (b.Reloading && b.ReloadStartTick != _seenReloadStart)
            {
                _seenReloadStart = b.ReloadStartTick;
                _audio.PlayOneShot(_reloadStart, cfg.reloadVolume);
            }

            if (b.LastReloadDoneTick != MovementState.NeverTick && b.LastReloadDoneTick != _seenReloadDone)
            {
                _seenReloadDone = b.LastReloadDoneTick;
                _audio.PlayOneShot(_reloadDone, cfg.reloadVolume);
            }

            float age = Time.time - _shotTime;
            if (tracer != null)
            {
                bool on = age < cfg.tracerDuration;
                tracer.enabled = on;
                if (on)
                {
                    float fade = 1f - age / cfg.tracerDuration;
                    // Départ recalculé à chaque image : la traînée reste accrochée au canon quand le joueur bouge vite.
                    if (muzzle != null && _view != null)
                        _tracerFrom = _view.ViewToWorld(muzzle.position);
                    tracer.SetPosition(0, _tracerFrom);
                    tracer.SetPosition(1, _tracerTo);
                    tracer.startWidth = cfg.tracerStartWidth;
                    tracer.endWidth = cfg.tracerEndWidth;
                    // Couleur HDR (lueur) par le matériau : les couleurs de sommet d'un LineRenderer sont bornées à 1.
                    tracer.startColor = tracer.endColor = Color.white;
                    _block.Clear();
                    _block.SetColor(BaseColorId, cfg.tracerColor * fade);
                    tracer.SetPropertyBlock(_block);
                }
            }

            if (impact != null && impact.enabled)
            {
                float u = age / cfg.impactDuration;
                if (u >= 1f)
                    impact.enabled = false;
                else
                {
                    impact.transform.localScale = Vector3.one * (cfg.impactSize * (0.4f + 0.6f * Mathf.Sin(Mathf.PI * Mathf.Min(1f, u * 1.5f))));
                    _block.SetColor(BaseColorId, cfg.tracerColor * (1f - u));
                    impact.SetPropertyBlock(_block);
                }
            }

            // Rotation d'un cran (−60°) par tir, et chambres : tout vient de l'état simulé (munitions, tirs, rechargement).
            _spin.Follow(b.ShotCount + _previewShots, Time.time, cfg.spinDuration, cfg.spinCurve);
            ShownAngle = _spin.Angle(Time.time, cfg.spinDuration, cfg.spinCurve);
            float progress = Barrel.ReloadProgress(b, tick, cfg, Time.fixedDeltaTime);
            int lit = BarrelModel.Lit(b.Rounds, b.Reloading, progress);
            ShownLit = ApplyModel(cylinder, chambers, _chamberBlock, ShownAngle, b.ShotCount + _previewShots, _previewShots > 0 ? BarrelModel.Chambers : lit, b.Inactive, cfg);
        }
    }
}
