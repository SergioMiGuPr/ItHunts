using ItHunts.Combat;
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
        public BoneSwordSettings SwordSettings => Current as BoneSwordSettings;
        public SpiritBookSettings BookSettings => Current as SpiritBookSettings;
        public SwordState Sword => _sword;
        public BookState Book => _book;
        public int LastSwordHits { get; private set; }
        public int TotalSwordHits { get; private set; }
        public BookEnd LastBookEnd { get; private set; }
        /// <summary>Cible du lien en cours (ennemi drainé ou allié soigné), null sans lien.</summary>
        public Transform BookTarget => _bookEnemy != null ? _bookEnemy.transform : _bookAlly != null ? _bookAlly.transform : null;
        private SwordState _sword = SwordState.Create();
        private SwordStep _pendingSwing;
        private int _pendingSwingTick = MovementState.NeverTick;
        private Vector3 _pendingAim;
        private bool _pendingAssisted;

        /// <summary>Dernier coup d'épée : cible visée par l'aide (debug, tests), élan donné (m/s).</summary>
        public Damageable LastSwordTarget { get; private set; }
        public float LastSwordLunge { get; private set; }
        private BookState _book = BookState.Create();
        private Damageable _bookEnemy;
        private PlayerHealth _bookAlly;
        private float _drainShown;
        private PlayerMotor _motor;
        public LanternState Lantern => _lantern;
        /// <summary>Cibles touchées par le jet en cours (ou le dernier) et au total (mesures, debug).</summary>
        public int LastJetHits { get; private set; }
        public int TotalJetHits { get; private set; }
        /// <summary>Personnage du joueur (null sans liste de personnages).</summary>
        public CharacterDefinition Character { get; private set; }

        private void Start()
        {
            EnsureReady();
            if (GetComponent<ArtifactModelView>() == null)
                gameObject.AddComponent<ArtifactModelView>();          // épée en os, livre à lien (D-077)
        }

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

        /// <summary>Change de personnage (choix en jeu d'un joueur qui rejoint) : son artefact remplace le précédent.</summary>
        public void EquipCharacter(CharacterDefinition character)
        {
            if (character == null)
                return;
            Character = character;
            Equip(character.artifact != null ? character.artifact : fallback);
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
            _sword = SwordState.Create();
            _book = BookState.Create();
            _bookEnemy = null;
            _bookAlly = null;
            _burned.Clear();
            LastJetHits = 0;
        }

        /// <summary>
        /// Livre à lien (D-077) : un appui sur une cible visée ouvre le lien (drain sur un ennemi, soin sur un allié), qui dure tant
        /// que le bouton est tenu, puis recharge. En coop en ligne, l'hôte décidera du lien (même calcul, côté hôte).
        /// </summary>
        private void BookStep(int tick, in MovementInput input, bool heldAtTickStart, bool holdingNow, Vector3 eye, Vector3 aim,
            SpiritBookSettings bk, float dt)
        {
            float time = Time.time;
            if (_book.Link == BookLink.Aucun)
            {
                int buffer = MovementSettings.SecondsToTicks(0.2f, dt);
                if (!SpiritBook.Fresh(_book, tick, input.ThrowPressedTick, buffer))
                    return;
                if (heldAtTickStart || holdingNow)
                {
                    _book.LastConsumedPressTick = input.ThrowPressedTick;   // clic du lancer
                    return;
                }

                if (!SpiritBook.Ready(_book, tick) || !input.ThrowHeld)
                    return;
                BookLink link = SpiritBook.Find(eye, aim, bk, transform, out _bookEnemy, out _bookAlly);
                if (link == BookLink.Aucun)
                    return;
                SpiritBook.Start(ref _book, link, tick, input.ThrowPressedTick);
                _drainShown = 0f;
                LastBookEnd = BookEnd.Aucune;
                return;
            }

            Transform target = BookTarget;
            bool alive = _book.Link == BookLink.Drain ? SpiritBook.Targetable(_bookEnemy) : _bookAlly != null && !_bookAlly.Dead && !_bookAlly.Downed;
            Vector3 point = target != null ? TargetPoint(target) : eye;
            float distance = Vector3.Distance(eye, point);
            bool sight = alive && SpiritBook.InSight(eye, point, target, transform, bk);
            BookEnd end = SpiritBook.Check(ref _book, tick, input.ThrowHeld, alive, distance, sight, bk, dt);
            if (end != BookEnd.Aucune)
            {
                LastBookEnd = end;
                SpiritBook.End(ref _book, tick, bk, dt);
                _bookEnemy = null;
                _bookAlly = null;
                return;
            }

            if (_book.Link == BookLink.Drain)
            {
                float dmg = SpiritBook.DrainTick(_book, bk, dt);
                float taken = dmg > 0f ? _bookEnemy.ApplyDamage(dmg, time, out bool killed) : 0f;
                _book.Drained += taken;
                _drainShown += taken;
                if (taken > 0f)
                {
                    _bookEnemy.NoteAttacker(_playerId);
                    float back = taken * bk.drainReturn;
                    _book.Returned += back;
                    var self = GetComponent<PlayerHealth>();
                    if (self != null && !self.Dead)
                        self.Heal(back);
                }

                if (_drainShown >= 3f || (taken > 0f && _bookEnemy.Dead))
                {
                    DamageEvents.Raise(new DamageEvent
                    {
                        Target = _bookEnemy, Damage = _drainShown, Point = point, Killed = _bookEnemy.Dead, Tier = HitTier.Petit, Time = time,
                    });
                    _drainShown = 0f;
                }
            }
            else
            {
                float heal = SpiritBook.HealTick(_book, _bookAlly.MaxHealth - _bookAlly.CurrentHealth, bk, dt);
                if (heal > 0f)
                {
                    _bookAlly.Heal(heal);
                    _book.Healed += heal;
                }
            }
        }

        private static Vector3 TargetPoint(Transform t)
        {
            var c = t.GetComponentInChildren<Collider>();
            return c != null ? c.bounds.center : t.position + Vector3.up;
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
                {
                    LastShot = BarrelShooter.Fire(eye, aim, cfg, transform, _playerId, Time.time, tick, null, GetComponent<ItHunts.Economy.PlayerUpgrades>());
                    if (LastShot.RoundsRefill > 0)                       // rechargement sur kill (nuit 3)
                        _barrel.Rounds = Mathf.Min(cfg.rounds, _barrel.Rounds + LastShot.RoundsRefill);
                }
            }
            else if (Current is BoneSwordSettings sw)
            {
                if (_motor == null)
                    _motor = GetComponent<PlayerMotor>();
                MovementState ms = _motor != null ? _motor.State : default;
                bool lunge = _motor != null && (ms.DashTicksLeft > 0 || ms.Mode == MovementMode.Slide);
                SwordStep st = BoneSword.Step(ref _sword, tick, input.ThrowPressedTick, heldAtTickStart, holdingNow, lunge, sw, dt);
                if (st.Swung)
                {
                    // Aide au corps à corps (2026-11-21) : cible visée, la vue tourne un peu vers elle, petit élan sans perte de vitesse.
                    _pendingAssisted = BoneSword.FindTarget(eye, aim, sw, transform, out Damageable target, out Vector3 point);
                    LastSwordTarget = target;
                    LastSwordLunge = 0f;
                    _pendingAim = _pendingAssisted ? (point - eye).normalized : aim;
                    if (_pendingAssisted)
                    {
                        var look = GetComponent<PlayerLook>();
                        if (look != null)
                            look.Nudge(point - eye, sw.assistTurn, sw.assistMaxTurn);
                        Vector3 push = st.Lunge ? Vector3.zero : BoneSword.LungePush(ms.Velocity, point - eye, sw);
                        if (push.sqrMagnitude > 1e-4f && _motor != null)
                        {
                            _motor.AddVelocity(push);
                            LastSwordLunge = push.magnitude;
                        }
                    }

                    _pendingSwing = st;
                    _pendingSwingTick = tick + MovementSettings.SecondsToTicks(sw.strikeDelay, dt);
                }

                // L'impact tombe quand la lame passe devant (strikeDelay) : dégâts, arrêt sur image, secousse.
                if (_pendingSwingTick != MovementState.NeverTick && tick >= _pendingSwingTick)
                {
                    _pendingSwingTick = MovementState.NeverTick;
                    Vector3 strikeAim = _pendingAssisted ? _pendingAim : aim;
                    LastSwordHits = BoneSword.Strike(eye, strikeAim, ms.Velocity, _pendingSwing, ms.HorizontalSpeed, sw, transform, _playerId, Time.time);
                    TotalSwordHits += LastSwordHits;
                    if (LastSwordHits > 0)
                    {
                        bool third = _pendingSwing.Index == 2 || _pendingSwing.Lunge;
                        GameFeelSettings feel = GameFeel.Settings;
                        if (feel != null)
                            HitStop.Request(feel.swordHitStop * (third ? sw.thirdHitStopFactor : 1f));
                        var shake = GetComponent<CameraShake>();
                        if (shake != null)
                            shake.AddTrauma(third ? sw.thirdHitTrauma : sw.hitTrauma);
                    }
                }
            }
            else if (Current is SpiritBookSettings bk)
                BookStep(tick, input, heldAtTickStart, holdingNow, eye, aim, bk, dt);
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
                    int n = FireLanternJet.Sweep(lc.Origin(eye, aim), aim, lc, transform, _playerId, _burned, Time.time, tick);   // depuis la lanterne
                    LastJetHits += n;
                    TotalJetHits += n;
                }
            }
        }
    }
}
