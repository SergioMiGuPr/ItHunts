using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// Emplacement d'artefact d'un joueur (main gauche, D-022, D-032), sur son prefab. Au démarrage, il demande son
    /// artefact à l'état de la run de la scène (RunState, tirage par la seed) ; sans RunState, il prend l'artefact par
    /// défaut. PlayerMotor appelle Step à chaque tick après la télékinésie (même entrée). Un seul artefact pour l'instant :
    /// le barillet ; un nouvel artefact ajoutera son cas ici (son état et son tir).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArtifactHolder : MonoBehaviour
    {
        [Tooltip("Artefact si la scène n'a pas d'état de run (RunState).")]
        [SerializeField] private ArtifactDefinition fallback;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Artifact");
        private bool _ready;
        private BarrelState _barrel;
        private int _playerId;

        public ArtifactDefinition Current { get; private set; }
        public BarrelSettings BarrelSettings => Current as BarrelSettings;
        public BarrelState Barrel => _barrel;
        public BarrelShot LastShot { get; private set; }
        public BarrelStep LastStep { get; private set; }

        private void Start() => EnsureReady();

        private void EnsureReady()
        {
            if (_ready)
                return;
            _ready = true;
            var run = FindFirstObjectByType<RunState>();
            var input = GetComponent<PlayerInput>();
            int index = input != null ? input.playerIndex : 0;
            Equip(run != null ? run.ArtifactFor(index) : fallback);
        }

        /// <summary>Équipe un artefact (début de run, tests, debug).</summary>
        public void Equip(ArtifactDefinition artifact)
        {
            _ready = true;
            Current = artifact;
            _playerId = PlayerHealth.IdOf(transform);
            if (artifact is BarrelSettings b)
                _barrel = BarrelState.Create(b.rounds);
        }

        /// <summary>Un tick de l'artefact. heldAtTickStart : l'orbe tenait un objet avant la télékinésie de ce tick.</summary>
        public void Step(int tick, in MovementInput input, bool heldAtTickStart, bool holdingNow, Vector3 eye, Vector3 aim, float dt)
        {
            EnsureReady();
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            if (Current is BarrelSettings cfg)
            {
                BarrelStep step = ItHunts.Artifacts.Barrel.Step(ref _barrel, tick, input.ThrowPressedTick, input.ReloadPressedTick,
                    heldAtTickStart, holdingNow, cfg, dt);
                LastStep = step;
                if (step.Fired)
                    LastShot = BarrelShooter.Fire(eye, aim, cfg, transform, _playerId, Time.time, tick);
            }
        }
    }
}
