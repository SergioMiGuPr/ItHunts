using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Economy;
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
    /// par défaut. PlayerMotor appelle Step à chaque tick après la télékinésie (même entrée). Artefacts : le barillet (caméléon),
    /// la lanterne de feu (renard), l'épée en os (koala), le livre à lien (hibou) ; un nouvel artefact ajoute son cas ici (son
    /// état et son action). Les améliorations à rangs (D-100) de l'artefact sont lues dans PlayerUpgrades (Upgrades).
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
        private readonly HashSet<int> _burned = new HashSet<int>();
        private int _playerId;
        private PlayerUpgrades _upgrades;
        private bool _sixthNext;

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
        /// <summary>Liens en plus (Lien multiple, H1) et cibles du chaînage (H2) : pour la vue.</summary>
        public IReadOnlyList<Damageable> BookExtraLinks => _bookExtra;
        private SwordState _sword = SwordState.Create();
        private SwordStep _pendingSwing;
        private int _pendingSwingTick = MovementState.NeverTick;
        private Vector3 _pendingAim;
        private bool _pendingAssisted;
        private Vector3 _lastAim = Vector3.forward, _lastEye;

        /// <summary>Dernier coup d'épée : cible visée par l'aide (debug, tests), élan donné (m/s).</summary>
        public Damageable LastSwordTarget { get; private set; }
        public float LastSwordLunge { get; private set; }
        private BookState _book = BookState.Create();
        private Damageable _bookEnemy;
        private readonly List<Damageable> _bookExtra = new List<Damageable>();
        private PlayerHealth _bookAlly;
        private float _drainShown, _nextChain;
        private int _reviveAct = -1;
        private PlayerMotor _motor;
        public LanternState Lantern => _lantern;
        /// <summary>Cibles touchées par le jet en cours (ou le dernier) et au total (mesures, debug).</summary>
        public int LastJetHits { get; private set; }
        public int TotalJetHits { get; private set; }
        /// <summary>Personnage du joueur (null sans liste de personnages).</summary>
        public CharacterDefinition Character { get; private set; }

        /// <summary>Améliorations du joueur (null s'il n'en a pas encore).</summary>
        public PlayerUpgrades Upgrades => _upgrades != null ? _upgrades : _upgrades = GetComponent<PlayerUpgrades>();

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
            _bookExtra.Clear();
            _bookAlly = null;
            _burned.Clear();
            _sixthNext = false;
            LastJetHits = 0;
        }

        /// <summary>Avance la recharge de l'artefact de seconds s (Orbe vorace, Mèche vive) : lanterne, livre, rechargement du barillet.</summary>
        public void ReduceCooldown(float seconds)
        {
            if (seconds <= 0f)
                return;
            float dt = Time.fixedDeltaTime;
            int ticks = MovementSettings.SecondsToTicks(seconds, dt);
            if (Current is FireLanternSettings)
                FireLantern.ReduceCooldown(ref _lantern, seconds, dt);
            else if (Current is SpiritBookSettings)
                _book.CooldownTicks = Mathf.Max(0, _book.CooldownTicks - ticks);
            else if (Current is BarrelSettings && _barrel.Reloading)
                _barrel.ReloadStartTick -= ticks;
        }

        /// <summary>Garde d'os Mythique (K6) : la parade porte un coup d'épée (index du combo) tout de suite.</summary>
        public void TriggerSwordStrike(int index)
        {
            if (!(Current is BoneSwordSettings sw))
                return;
            if (_motor == null)
                _motor = GetComponent<PlayerMotor>();
            MovementState ms = _motor != null ? _motor.State : default;
            Vector3 eye = _motor != null ? ItHunts.Orbs.MagnetismOrb.EyePosition(ms, _motor.Settings) : transform.position + Vector3.up * 1.6f;
            var step = new SwordStep { Swung = true, Index = index };
            LastSwordHits = BoneSword.Strike(eye, _lastAim, ms.Velocity, step, ms.HorizontalSpeed, sw, transform, _playerId, Time.time, null, Upgrades);
            TotalSwordHits += LastSwordHits;
        }

        // ---------- Livre à lien ----------

        /// <summary>
        /// Livre à lien (D-077) : un appui sur une cible visée ouvre le lien (drain sur un ennemi, soin sur un allié), qui dure tant
        /// que le bouton est tenu, puis recharge. Améliorations (D-100) : liens multiples (H1), chaînage (H2), ralentissement (H3),
        /// bouclier (H4), ancre (H6), pages brûlantes (H7), soin et résurrection (H8). En coop en ligne, l'hôte décidera du lien.
        /// </summary>
        private void BookStep(int tick, in MovementInput input, bool heldAtTickStart, bool holdingNow, Vector3 eye, Vector3 aim,
            SpiritBookSettings bk, float dt)
        {
            float time = Time.time;
            PlayerUpgrades up = Upgrades;
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
                bool allowDowned = up != null && up.Mythic(UpgradeKind.SoinLie) && _reviveAct != ItHunts.Level.RunSession.Act;
                BookLink link = SpiritBook.Find(eye, aim, bk, transform, out _bookEnemy, out _bookAlly, allowDowned);
                if (link == BookLink.Aucun)
                    return;
                SpiritBook.Start(ref _book, link, tick, input.ThrowPressedTick);
                _drainShown = 0f;
                LastBookEnd = BookEnd.Aucune;
                if (link == BookLink.Drain)
                    StartDrain(up, eye);
                else if (_bookAlly != null && _bookAlly.Downed && allowDowned)
                {
                    _bookAlly.ReviveFromDown(30f);                       // Soin lié Mythique : le lien relève l'allié (une fois par acte)
                    _reviveAct = ItHunts.Level.RunSession.Act;
                }

                return;
            }

            Transform target = BookTarget;
            bool alive = _book.Link == BookLink.Drain ? SpiritBook.Targetable(_bookEnemy) : _bookAlly != null && !_bookAlly.Dead && !_bookAlly.Downed;
            Vector3 point = target != null ? TargetPoint(target) : eye;
            float distance = Vector3.Distance(eye, point);
            bool sight = alive && SpiritBook.InSight(eye, point, target, transform, bk);
            BookEnd end = SpiritBook.Check(ref _book, tick, input.ThrowHeld, alive, distance, sight, bk, dt);
            if (end == BookEnd.CibleMorte && _book.Link == BookLink.Drain && up != null)
            {
                // Pages brûlantes (H7) : la mort sous le lien rend la recharge, relance sur un voisin (Or), onde qui étourdit (Mythique).
                Vector3 at = _bookEnemy != null ? _bookEnemy.Center : point;
                if (up.Mythic(UpgradeKind.PagesBrulantes))
                    UpgradeEffects.StunAround(at, 4f, 1f);
                bool relaunch = up.AtLeast(UpgradeKind.PagesBrulantes, 2) || up.Mythic(UpgradeKind.Chainage);
                Damageable next = relaunch ? UpgradeEffects.NearestEnemy(at, up.Mythic(UpgradeKind.Chainage) ? up.V3(UpgradeKind.Chainage) : 6f, _bookEnemy) : null;
                if (next != null && SpiritBook.InSight(eye, next.Center, next.transform, transform, bk))
                {
                    _bookEnemy = next;                                    // le lien passe au suivant sans recharge
                    _bookExtra.Remove(next);
                    return;
                }

                SpiritBook.End(ref _book, tick, bk, dt);
                LastBookEnd = end;
                _book.CooldownTicks = Mathf.RoundToInt(_book.CooldownTicks * (1f - Mathf.Clamp01(up.V2(UpgradeKind.PagesBrulantes))));
                _bookEnemy = null;
                _bookExtra.Clear();
                _bookAlly = null;
                return;
            }

            if (end != BookEnd.Aucune)
            {
                LastBookEnd = end;
                SpiritBook.End(ref _book, tick, bk, dt);
                _bookEnemy = null;
                _bookExtra.Clear();
                _bookAlly = null;
                return;
            }

            if (_book.Link == BookLink.Drain)
                DrainTick(up, bk, eye, point, time, dt);
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

        /// <summary>Début d'un drain : liens en plus (H1) sur les voisins, ancre (H6 : tirée, clouée).</summary>
        private void StartDrain(PlayerUpgrades up, Vector3 eye)
        {
            _bookExtra.Clear();
            _nextChain = 0f;
            if (up == null || _bookEnemy == null)
                return;
            int links = Mathf.RoundToInt(up.V(UpgradeKind.LienMultiple));
            if (links > 1)
                foreach (Damageable d in UpgradeEffects.EnemiesAround(_bookEnemy.Center, 8f, _bookEnemy))
                {
                    if (_bookExtra.Count >= links - 1)
                        break;
                    _bookExtra.Add(d);
                }

            if (up.Has(UpgradeKind.LienDAncrage) && _bookEnemy.Body != null)
            {
                float pull = up.V3(UpgradeKind.LienDAncrage);
                if (pull > 0f)
                {
                    Vector3 toward = eye - _bookEnemy.Center;
                    toward.y = 0f;
                    if (toward.sqrMagnitude > 1e-4f)
                        _bookEnemy.Body.Push(toward.normalized * (pull * 2.5f));
                }

                if (up.Mythic(UpgradeKind.LienDAncrage))
                    _bookEnemy.Body.StunFor(2f);                          // clouée au sol
            }
        }

        /// <summary>Un tick de drain : cible, liens en plus, chaînage ; part rendue, bouclier, régénération ; états « lié », ralenti.</summary>
        private void DrainTick(PlayerUpgrades up, SpiritBookSettings bk, Vector3 eye, Vector3 point, float time, float dt)
        {
            float dmg = SpiritBook.DrainTick(_book, bk, dt);
            int links = 1 + _bookExtra.Count;
            float perLink = up != null ? up.V3(UpgradeKind.LienMultiple) : 0f;
            float taken = Drain(_bookEnemy, dmg * (1f + perLink * links), time, up, true);
            float share = up != null && up.Has(UpgradeKind.LienMultiple) ? up.V2(UpgradeKind.LienMultiple) : 0f;
            for (int i = _bookExtra.Count - 1; i >= 0; i--)
            {
                Damageable d = _bookExtra[i];
                if (!SpiritBook.Targetable(d) || Vector3.Distance(d.Center, _bookEnemy.Center) > 12f)
                {
                    _bookExtra.RemoveAt(i);
                    continue;
                }

                taken += Drain(d, dmg * share * (1f + perLink * links), time, up, false);
            }

            // Chaînage (H2) : le drain saute aux ennemis les plus proches de la cible (portée, part du drain).
            if (up != null && up.Has(UpgradeKind.Chainage))
            {
                int jumps = Mathf.RoundToInt(up.V(UpgradeKind.Chainage));
                float range = up.V3(UpgradeKind.Chainage), chainShare = up.V2(UpgradeKind.Chainage);
                Damageable from = _bookEnemy;
                var done = new HashSet<Damageable> { _bookEnemy };
                foreach (Damageable d in _bookExtra)
                    done.Add(d);
                for (int j = 0; j < jumps && from != null; j++)
                {
                    Damageable next = null;
                    float best = range * range;
                    foreach (Damageable d in Damageable.All)
                    {
                        if (d == null || d.Dead || d.ImmuneToOrbs || done.Contains(d))
                            continue;
                        float sq = (d.Center - from.Center).sqrMagnitude;
                        if (sq < best)
                        {
                            best = sq;
                            next = d;
                        }
                    }

                    if (next == null)
                        break;
                    done.Add(next);
                    taken += Drain(next, dmg * chainShare, time, up, false);
                    from = next;
                }
            }

            _book.Drained += taken;
            _drainShown += taken;
            var self = GetComponent<PlayerHealth>();
            if (taken > 0f && self != null && !self.Dead)
            {
                float back = taken * bk.drainReturn;
                _book.Returned += back;
                float missing = self.MaxHealth - self.CurrentHealth;
                self.Heal(back);
                if (up != null && up.AtLeast(UpgradeKind.Sangsue, 2) && back > missing)
                    self.AddShield(back - missing);                      // Sangsue Or : le surplus devient un bouclier
            }

            if (up != null && self != null && !self.Dead && up.Has(UpgradeKind.SoinLie))
                self.Heal(up.V2(UpgradeKind.SoinLie) * dt);              // Soin lié : régénération pendant un drain

            if (_drainShown >= 3f || (taken > 0f && _bookEnemy != null && _bookEnemy.Dead))
            {
                DamageEvents.Raise(new DamageEvent
                {
                    Target = _bookEnemy, Damage = _drainShown, Point = point, Killed = _bookEnemy != null && _bookEnemy.Dead, Tier = HitTier.Petit, Time = time,
                    AttackerId = _playerId,
                });
                _drainShown = 0f;
            }
        }

        /// <summary>Drain d'une cible : dégâts (améliorations), état « lié », ralentissement (Lien vorace, Lien d'ancrage), plus de bond.</summary>
        private float Drain(Damageable d, float dmg, float time, PlayerUpgrades up, bool primary)
        {
            if (d == null || d.Dead || dmg <= 0f)
                return 0f;
            d.NoteAttacker(_playerId);
            d.BoundUntil = time + 0.3f;
            d.BoundBy = _playerId;
            if (up != null)
            {
                float slow = Mathf.Max(up.V2(UpgradeKind.LienVorace), primary ? up.V(UpgradeKind.LienDAncrage) : 0f);
                if (slow > 0f)
                    d.Slow(slow, 0.3f);
                if (up.Mythic(UpgradeKind.LienVorace))
                    d.LeapBlockedUntil = time + 0.3f;
            }

            return d.ApplyDamage(PlayerUpgrades.Outgoing(_playerId, d, dmg), time, out _);
        }

        private static Vector3 TargetPoint(Transform t)
        {
            var c = t.GetComponentInChildren<Collider>();
            return c != null ? c.bounds.center : t.position + Vector3.up;
        }

        // ---------- Un tick ----------

        /// <summary>Un tick de l'artefact. heldAtTickStart : l'orbe tenait un objet avant la télékinésie de ce tick.</summary>
        public void Step(int tick, in MovementInput input, bool heldAtTickStart, bool holdingNow, Vector3 eye, Vector3 aim, float dt)
        {
            EnsureReady();
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            _lastAim = aim;
            _lastEye = eye;
            PlayerUpgrades up = Upgrades;
            if (Current is BarrelSettings cfg)
            {
                BarrelStep step = ItHunts.Artifacts.Barrel.Step(ref _barrel, tick, input.ThrowPressedTick, input.ReloadPressedTick,
                    heldAtTickStart, holdingNow, cfg, dt);
                bool burst = false;
                // Éventail (C7) : le clic tenu vide le barillet en rafale (cadence ×3, dispersion, dégâts réduits).
                if (!step.Fired && up != null && up.Has(UpgradeKind.Eventail) && !heldAtTickStart
                    && ItHunts.Artifacts.Barrel.TryBurstFire(ref _barrel, tick, input.ThrowHeld, holdingNow, 3f, cfg, dt))
                {
                    step.Fired = true;
                    burst = true;
                }

                LastStep = step;
                if (step.Fired)
                {
                    var opt = new BarrelShotOptions
                    {
                        LastRound = _barrel.Rounds == 0 || _sixthNext, Burst = burst,
                        Spread = burst ? up.V(UpgradeKind.Eventail) : 0f, DamageFactor = burst ? up.V2(UpgradeKind.Eventail) : 1f,
                    };
                    _sixthNext = false;
                    LastShot = BarrelShooter.Fire(eye, aim, cfg, transform, _playerId, Time.time, tick, null, up, opt);
                    if (LastShot.FullReload)
                    {
                        ItHunts.Artifacts.Barrel.InstantReload(ref _barrel, tick, cfg);
                        if (opt.LastRound && up != null && up.Mythic(UpgradeKind.SixiemeChambre))
                            _sixthNext = true;                           // la balle suivante est aussi une « sixième » (une fois)
                    }
                    else if (LastShot.RoundsRefill > 0)                  // rechargement sur kill (nuit 3)
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
                    LastSwordHits = BoneSword.Strike(eye, strikeAim, ms.Velocity, _pendingSwing, ms.HorizontalSpeed, sw, transform, _playerId, Time.time, null, up);
                    TotalSwordHits += LastSwordHits;
                    if (LastSwordHits > 0)
                    {
                        bool third = _pendingSwing.Index >= 2 || _pendingSwing.Lunge;
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
                    if (up != null && up.Has(UpgradeKind.FeuGregeois))
                        Puddles(lc.Origin(eye, aim), aim, lc, up);
                }

                if (step.Burning)
                {
                    float bonus = up != null && _lantern.Consecutive > 0 && up.AtLeast(UpgradeKind.SouffleDouble, 2) ? up.V3(UpgradeKind.SouffleDouble) : 0f;
                    int n = FireLanternJet.Sweep(lc.Origin(eye, aim), aim, lc, transform, _playerId, _burned, Time.time, tick, null, null, bonus);   // depuis la lanterne
                    LastJetHits += n;
                    TotalJetHits += n;
                }
            }
        }

        /// <summary>Feu grégeois (R5) : le jet laisse des flaques de feu au sol, le long du viseur, jusqu'à la portée.</summary>
        private void Puddles(Vector3 origin, Vector3 aim, FireLanternSettings lc, PlayerUpgrades up)
        {
            Vector3 flat = new Vector3(aim.x, 0f, aim.z);
            if (flat.sqrMagnitude < 1e-4f)
                flat = Vector3.forward;
            flat.Normalize();
            float duration = up.V(UpgradeKind.FeuGregeois), radius = up.V2(UpgradeKind.FeuGregeois), slow = up.V3(UpgradeKind.FeuGregeois);
            float stun = up.Mythic(UpgradeKind.FeuGregeois) ? 0.6f : 0f;
            for (float d = 3f; d <= Mathf.Min(lc.range, 12f); d += 4f)
            {
                Vector3 probe = origin + flat * d + Vector3.up * 1.5f;
                if (Physics.Raycast(probe, Vector3.down, out RaycastHit hit, 6f, ~0, QueryTriggerInteraction.Ignore) && hit.collider.attachedRigidbody == null)
                    FirePuddle.Spawn(hit.point + Vector3.up * 0.05f, radius, duration, lc.burnDps * 0.5f, _playerId, slow, stun, lc.burnMaxStacks);
            }
        }
    }
}
