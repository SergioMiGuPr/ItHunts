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
    /// - Munitions lisibles sur le barillet : 6 chambres sur la face arrière, allumées si chargées, éteintes si vides ; en
    ///   rechargement, elles se rallument une à une ; grisées quand le barillet est inactif (l'orbe tient un objet).
    /// - Le barillet tourne d'un cran à chaque tir ; traînée lumineuse brève de la bouche au point touché ; éclat à
    ///   l'impact ; sons provisoires (tir, rechargement, fin de rechargement).
    /// Le barillet entier est caché si l'artefact du joueur n'est pas le barillet.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public sealed class BarrelView : MonoBehaviour
    {
        [SerializeField] private Transform barrelRoot;
        [Tooltip("Partie qui tourne d'un cran (360° / munitions) à chaque tir.")]
        [SerializeField] private Transform cylinder;
        [SerializeField] private Renderer[] chambers;
        [Tooltip("Bouche du canon : départ de la traînée.")]
        [SerializeField] private Transform muzzle;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Renderer impact;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.BarrelView");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
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
        private float _angle;

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

            // Rotation d'un cran par tir.
            float target = b.ShotCount * 360f / Mathf.Max(1, cfg.rounds);
            _angle = Mathf.MoveTowards(_angle, target, 360f / Mathf.Max(1, cfg.rounds) / 0.06f * Time.deltaTime);
            if (cylinder != null)
                cylinder.localRotation = Quaternion.Euler(0f, 0f, -_angle);

            // Chambres : munitions restantes (en rechargement, elles se rallument une à une).
            if (chambers == null)
                return;
            float progress = Barrel.ReloadProgress(b, tick, cfg, Time.fixedDeltaTime);
            int lit = b.Reloading ? Mathf.FloorToInt(progress * cfg.rounds) : b.Rounds;
            for (int i = 0; i < chambers.Length; i++)
            {
                if (chambers[i] == null)
                    continue;
                // Les chambres tournent avec le barillet : celle en haut est la prochaine à tirer (après k tirs, la chambre k).
                int fromTop = ((i - b.ShotCount) % cfg.rounds + cfg.rounds) % cfg.rounds;
                bool loaded = fromTop < lit;
                Color c = !loaded ? cfg.emptyColor : b.Inactive ? cfg.inactiveColor : cfg.loadedColor;
                _block.SetColor(BaseColorId, c);
                chambers[i].SetPropertyBlock(_block);
            }
        }
    }
}
