using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Artifacts
{
    /// <summary>
    /// Emplacement d'artefact d'un joueur (main gauche, D-022, D-064), sur son prefab. Au démarrage, il demande son
    /// artefact à l'état de la run de la scène (RunState : l'artefact du personnage choisi) ; sans RunState, il prend l'artefact
    /// par défaut. PlayerMotor appelle Step à chaque tick après la télékinésie (même entrée). Artefacts : le barillet (caméléon)
    /// et la lanterne de feu (renard) ; un nouvel artefact ajoute son cas ici (son état et son action).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ArtifactHolder : MonoBehaviour
    {
        [Tooltip("Artefact si la scène n'a pas d'état de run (RunState).")]
        [SerializeField] private ArtifactDefinition fallback;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.Artifact");
        private bool _ready;
        private BarrelState _barrel;
        private LanternState _lantern;
        private readonly System.Collections.Generic.HashSet<int> _burned = new System.Collections.Generic.HashSet<int>();
        private int _playerId;

        public ArtifactDefinition Current { get; private set; }
        public BarrelSettings BarrelSettings => Current as BarrelSettings;
        public BarrelState Barrel => _barrel;
        public BarrelShot LastShot { get; private set; }
        public BarrelStep LastStep { get; private set; }
        public FireLanternSettings LanternSettings => Current as FireLanternSettings;
        public LanternState Lantern => _lantern;
        /// <summary>Cibles touchées par le jet en cours (ou le dernier) et au total (mesures, debug).</summary>
        public int LastJetHits { get; private set; }
        public int TotalJetHits { get; private set; }
        /// <summary>Personnage du joueur (null sans liste de personnages).</summary>
        public CharacterDefinition Character { get; private set; }

        private void Start() => EnsureReady();

        /// <summary>Artefact équipé dès maintenant (avant Start : marchand, HUD).</summary>
        public void EnsureReady()
        {
            if (_ready)
                return;
            _ready = true;
            var run = FindFirstObjectByType<RunState>();
            var input = GetComponent<PlayerInput>();
            int index = input != null ? input.playerIndex : 0;
            Character = run != null ? run.CharacterFor(index) : null;
            ArtifactDefinition a = run != null ? run.ArtifactFor(index) : null;
            Equip(a != null ? a : fallback);
        }

        /// <summary>Équipe un artefact (début de run, tests, debug).</summary>
        public void Equip(ArtifactDefinition artifact)
        {
            _ready = true;
            Current = artifact;
            _playerId = PlayerHealth.IdOf(transform);
            if (artifact is BarrelSettings b)
                _barrel = BarrelState.Create(b.rounds);
            _lantern = LanternState.Create();
            _burned.Clear();
            LastJetHits = 0;
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
            else if (Current is FireLanternSettings lc)
            {
                LanternStep step = FireLantern.Step(ref _lantern, tick, input.ThrowPressedTick, heldAtTickStart, holdingNow, lc, dt);
                if (step.Started)
                {
                    _burned.Clear();
                    LastJetHits = 0;
                }

                if (step.Burning)
                {
                    int n = FireLanternJet.Sweep(eye, aim, lc, transform, _playerId, _burned, Time.time, tick);
                    LastJetHits += n;
                    TotalJetHits += n;
                }
            }
        }
    }
}
