using System.Collections.Generic;
using ItHunts.Artifacts;
using ItHunts.Combat;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Economy
{
    /// <summary>
    /// Améliorations d'un joueur (étape 7, D-043 ; rangs : nuit 4, D-100), sur son prefab (ajoutée à la première amélioration) : ses
    /// emplacements (UpgradeSlots, miroir de RunState qui fait foi), les valeurs lues au rang courant, et l'application des effets
    /// à ses systèmes. Les réglages partagés (mouvement, magnétisme, parade, artefact) sont copiés pour lui seul ; les dégâts, la
    /// collecte, les prix et la détection passent par des crochets statiques (un identifiant de joueur → sa valeur). Les effets
    /// déclenchés (parade, kill, coup reçu, atterrissage, traction, tick) viennent des événements des systèmes.
    /// Réseau (étape 11) : tout ce qui touche la simulation (charges, vitesse) sera rejoué par l'autorité.
    /// </summary>
    public sealed class PlayerUpgrades : MonoBehaviour
    {
        private const int KindCount = 70;
        private const float HardLandingSpeed = 13f;

        private readonly UpgradeSlots _slots = new UpgradeSlots();
        private readonly int[] _rank = new int[KindCount];
        private readonly float[] _v = new float[KindCount], _v2 = new float[KindCount], _v3 = new float[KindCount];

        private MovementSettings _moveOrig, _moveCopy;
        private MagnetismOrbSettings _magOrig, _magCopy;
        private ParrySettings _parryOrig, _parryCopy;
        private ArtifactDefinition _artOrig, _artCopy;
        private PlayerMotor _motor;
        private PlayerHealth _health;
        private ArtifactHolder _holder;
        private bool _hooked;

        /// <summary>Change à chaque modification des emplacements (HUD).</summary>
        public int Version { get; private set; }
        public UpgradeSlots Slots => _slots;
        public IReadOnlyList<UpgradeDefinition> Owned => _slots.Owned;
        public int PlayerId => _health != null ? _health.PlayerId : gameObject.GetInstanceID();

        // ---------- Lecture au rang courant ----------

        /// <summary>Rang (0 Bronze … 4 Mythique) de cet effet ; −1 si le joueur ne l'a pas.</summary>
        public int Rank(UpgradeKind k) => _rank[(int)k];
        public bool Has(UpgradeKind k) => _rank[(int)k] >= 0;
        public bool AtLeast(UpgradeKind k, int rank) => _rank[(int)k] >= rank;
        public bool Mythic(UpgradeKind k) => _rank[(int)k] >= UpgradeSlots.MaxRank;
        /// <summary>Paramètre principal au rang courant (0 sans l'amélioration).</summary>
        public float V(UpgradeKind k) => _v[(int)k];
        public float V2(UpgradeKind k) => _v2[(int)k];
        public float V3(UpgradeKind k) => _v3[(int)k];
        public int RankOf(UpgradeDefinition d) => _slots.RankOf(d);

        // ---------- Anciens accès (gardés pour les systèmes qui les lisent) ----------

        public int ExtraCharges => Mathf.RoundToInt(V(UpgradeKind.ChargeEnPlus));
        public float TractionRechargeCut => V(UpgradeKind.TractionRapide);
        public float CollectRadiusBonus => V(UpgradeKind.GrandeOrbe) + V2(UpgradeKind.Flair);
        public float ThrowDamageBonus => V(UpgradeKind.ProjectilesLourds);
        public float RepelDamage => V(UpgradeKind.OndeBlessante);
        public float MaxHealthBonus => V(UpgradeKind.CoeurSolide) + V2(UpgradeKind.PeauEpaisse);
        public float PullSpeedBonus => V(UpgradeKind.Aspiration);
        public int Paralysis => Has(UpgradeKind.BallesParalysantes) ? 1 : 0;
        public int ExplodeOnKill => Has(UpgradeKind.CadavreExplosif) ? 1 : 0;
        public int Pierce => Mathf.RoundToInt(V(UpgradeKind.BallePerforante));
        public int Ricochets => Mathf.RoundToInt(V(UpgradeKind.Ricochet));
        public int ReloadOnKill => Has(UpgradeKind.MainLeste) ? 1 : 0;
        public int ChainCrit => Has(UpgradeKind.OeilFendu) ? 1 : 0;

        public static PlayerUpgrades Of(Component player)
        {
            if (player == null)
                return null;
            var u = player.GetComponentInParent<PlayerUpgrades>();
            if (u == null)
            {
                var h = player.GetComponentInParent<PlayerHealth>();
                if (h != null)
                    u = h.gameObject.AddComponent<PlayerUpgrades>();
            }

            return u;
        }

        /// <summary>Améliorations du joueur d'identifiant donné (null sans amélioration).</summary>
        public static PlayerUpgrades OfId(int playerId)
        {
            PlayerHealth p = PlayerHealth.Find(playerId);
            return p != null ? p.GetComponent<PlayerUpgrades>() : null;
        }

        private void Awake()
        {
            for (int i = 0; i < KindCount; i++)
                _rank[i] = -1;
            _motor = GetComponent<PlayerMotor>();
            _health = GetComponent<PlayerHealth>();
            _holder = GetComponent<ArtifactHolder>();
        }

        private void OnEnable()
        {
            InstallHooks();
            ParryEvents.Parried += OnParried;
            Damageable.Died += OnDied;
            DamageEvents.Hit += OnHit;
            PlayerHealth.Hurt += OnHurt;
            Burning.Ignited += OnIgnited;
            SoulSystem.SoulCollected += OnSoulCollected;
            if (_motor == null)
                _motor = GetComponent<PlayerMotor>();
            if (_motor != null)
            {
                _motor.Landed += OnLanded;
                _motor.TractionEnded += OnTractionEnded;
                _motor.Repelled += OnRepelled;
                _motor.Ticked += OnTick;
            }

            if (_health == null)
                _health = GetComponent<PlayerHealth>();
            if (_health != null)
                _health.SecondChance = TrySecondChance;
        }

        private void OnDisable()
        {
            ParryEvents.Parried -= OnParried;
            Damageable.Died -= OnDied;
            DamageEvents.Hit -= OnHit;
            PlayerHealth.Hurt -= OnHurt;
            Burning.Ignited -= OnIgnited;
            SoulSystem.SoulCollected -= OnSoulCollected;
            if (_motor != null)
            {
                _motor.Landed -= OnLanded;
                _motor.TractionEnded -= OnTractionEnded;
                _motor.Repelled -= OnRepelled;
                _motor.Ticked -= OnTick;
            }
        }

        // ---------- Prise, montée, synchronisation ----------

        /// <summary>Prend une amélioration : nouvelle (Bronze) ou montée d'un rang ; applique les effets. Vrai si prise.</summary>
        public bool Apply(UpgradeDefinition d)
        {
            if (d == null)
                return false;
            UpgradeGrant g = _slots.Grant(d);
            if (!g.Ok)
                return false;
            Recompute();
            return true;
        }

        /// <summary>Pose directement un rang (reprise d'un acte, panneau admin, tests) et applique.</summary>
        public void ApplyAtRank(UpgradeDefinition d, int rank)
        {
            if (d == null)
                return;
            _slots.Set(d, rank);
            Recompute();
        }

        /// <summary>Recopie les emplacements de la run (qui fait foi) et applique.</summary>
        public void SyncFrom(RunState run)
        {
            if (run == null)
                return;
            _slots.CopyFrom(run.Slots(PlayerId));
            Recompute();
        }

        /// <summary>Recalcule les valeurs au rang courant et réapplique tous les effets (copies des réglages).</summary>
        public void Recompute()
        {
            Version++;
            for (int i = 0; i < KindCount; i++)
            {
                _rank[i] = -1;
                _v[i] = _v2[i] = _v3[i] = 0f;
            }

            foreach (UpgradeDefinition d in _slots.Owned)
            {
                if (d == null)
                    continue;
                int k = (int)d.kind;
                if (k < 0 || k >= KindCount)
                    continue;
                int r = _slots.RankOf(d);
                _rank[k] = r;
                _v[k] = d.Value(r);
                _v2[k] = d.Value2(r);
                _v3[k] = d.Value3(r);
            }

            RefreshMovement();
            RefreshHealth();
            RefreshParry();
            RefreshArtifact();
            RefreshStatics();
        }

        // ---------- Copies des réglages ----------

        private void RefreshMovement()
        {
            if (_motor == null)
                _motor = GetComponent<PlayerMotor>();
            if (_motor == null)
                return;
            if (_moveOrig == null)
                _moveOrig = _motor.Settings;
            if (_magOrig == null)
                _magOrig = _motor.MagnetismSettings;
            if (_moveOrig != null)
            {
                Discard(_moveCopy);
                MovementSettings m = _moveCopy = Instantiate(_moveOrig);
                m.name = _moveOrig.name + " (améliorations)";
                m.movementCharges = _moveOrig.movementCharges + ExtraCharges;
                m.chargeRechargeTimeGround = Mathf.Max(0.3f, m.chargeRechargeTimeGround - V2(UpgradeKind.ChargeEnPlus));
                m.chargeRechargeTimeAir = Mathf.Max(0.5f, m.chargeRechargeTimeAir - V3(UpgradeKind.ChargeEnPlus));
                m.runSpeed += V(UpgradeKind.SemellesDeVent);
                if (AtLeast(UpgradeKind.SemellesDeVent, 2))
                    m.momentumLossDeceleration *= 0.7f;
                if (Mythic(UpgradeKind.SemellesDeVent))
                    m.groundSpeedCap += 2f;
                // Pas de chat (G25) : glissade, wall slide, wall jump, hissage ; Mythique : même mur, charges rendues par les murs.
                m.slideDeceleration *= 1f - V(UpgradeKind.PasDeChat);
                m.wallJumpKick += V2(UpgradeKind.PasDeChat);
                m.wallSlideMaxFallSpeed *= Has(UpgradeKind.PasDeChat) ? V3(UpgradeKind.PasDeChat) : 1f;
                if (AtLeast(UpgradeKind.PasDeChat, 2))
                    m.mantleMaxAboveHead += 0.5f;
                if (Mythic(UpgradeKind.PasDeChat))
                    m.allowSameWallJump = m.wallRefillsCharges = true;
                // Vif-argent (G26) : gain en l'air (strafe du bunny hop), contrôle en l'air.
                m.airAcceleration *= 1f + V(UpgradeKind.VifArgent);
                m.airWishSpeedCap *= 1f + V(UpgradeKind.VifArgent);
                m.airControl = Mathf.Clamp01(m.airControl + V2(UpgradeKind.VifArgent));
                // Griffes grimpantes (K-P2).
                m.wallSlideMaxFallSpeed *= 1f - V(UpgradeKind.GriffesGrimpantes);
                m.mantleMaxAboveHead += V2(UpgradeKind.GriffesGrimpantes);
                m.wallJumpHeight += V3(UpgradeKind.GriffesGrimpantes);
                if (AtLeast(UpgradeKind.GriffesGrimpantes, 2))
                    m.wallRefillsCharges = true;
                if (Mythic(UpgradeKind.GriffesGrimpantes))
                    m.wallSlideMaxFallSpeed = 0f;                         // accroché au mur (approximation du « s'accrocher 1 s »)
                // Plumes (H-P2) : plané en tenant saut ; Mythique : un saut en l'air de plus, plané dirigé.
                if (Has(UpgradeKind.Plumes))
                    m.glideGravityScale = V(UpgradeKind.Plumes);
                if (AtLeast(UpgradeKind.Plumes, 2))
                    m.glideKeepsSpeed = true;
                if (Mythic(UpgradeKind.Plumes))
                {
                    m.maxJumps += 1;
                    m.glideSteer = true;
                }

                _baseRunSpeed = m.runSpeed;
            }

            if (_magOrig != null)
            {
                Discard(_magCopy);
                MagnetismOrbSettings g = _magCopy = Instantiate(_magOrig);
                g.name = _magOrig.name + " (améliorations)";
                g.attractChargeRechargeTime = Mathf.Max(0.5f, _magOrig.attractChargeRechargeTime - TractionRechargeCut);
                g.tractionAcceleration *= 1f + V(UpgradeKind.ElanDeTraction);
                g.waveConeAngle = Mathf.Min(179f, g.waveConeAngle + V(UpgradeKind.OndeDeChoc));
                g.waveRange += V2(UpgradeKind.OndeDeChoc);
                if (AtLeast(UpgradeKind.OndeDeChoc, 2))
                    g.waveObjectSpeed *= 1.2f;
                g.repelExtraCharges = Mathf.RoundToInt(V(UpgradeKind.ChargeDeRepulsion));
                g.repelCooldown = Mathf.Max(0.5f, g.repelCooldown - V2(UpgradeKind.ChargeDeRepulsion));
                if (Mythic(UpgradeKind.ChargeDeRepulsion))
                    g.repelGroundLaunchHeight = 4f;
                // Harpon (G11) : traction vers les goules, puis les volants ; Lien d'ancrage (H6) : vers la cible liée.
                int mask = 0;
                if (Has(UpgradeKind.Harpon))
                    mask |= MagnetismOrbSettings.TractionGhouls | (AtLeast(UpgradeKind.Harpon, 1) ? MagnetismOrbSettings.TractionFlyers : 0);
                if (Has(UpgradeKind.LienDAncrage))
                    mask |= MagnetismOrbSettings.TractionBound;
                g.tractionEnemyMask = mask;
            }

            _motor.UseSettings(_moveCopy, _magCopy);
        }

        private float _baseRunSpeed;

        private void RefreshHealth()
        {
            if (_health == null)
                _health = GetComponent<PlayerHealth>();
            if (_health == null)
                return;
            _health.SetMaxHealthBonus(MaxHealthBonus);
            _health.IncomingScale = IncomingScale;
            _health.ShieldMax = V2(UpgradeKind.Sangsue);
            _health.KnockbackImmune = AtLeast(UpgradeKind.PeauEpaisse, 2);
            _health.FallDamageScale = FallDamageScale;
        }

        private void RefreshParry()
        {
            var parry = GetComponent<PlayerParry>();
            if (parry == null)
                return;
            if (_parryOrig == null)
                _parryOrig = parry.Settings;
            if (_parryOrig == null)
                return;
            Discard(_parryCopy);
            ParrySettings c = _parryCopy = Instantiate(_parryOrig);
            c.name = _parryOrig.name + " (améliorations)";
            c.window += V(UpgradeKind.GardeLarge) + V(UpgradeKind.GardeDOs);
            if (AtLeast(UpgradeKind.GardeLarge, 2))
                c.lateWindow *= 2f;
            parry.Settings = c;
        }

        /// <summary>Artefact : copie de ses réglages pour ce joueur, avec ses améliorations (les effets « à rangs » sont lus directement par l'artefact).</summary>
        private void RefreshArtifact()
        {
            if (_holder == null)
                _holder = GetComponent<ArtifactHolder>();
            if (_holder == null)
                return;
            _holder.EnsureReady();
            if (_artOrig == null)
                _artOrig = _holder.Current;
            if (_artOrig == null)
                return;
            bool any = false;
            ArtifactDefinition copy = null;
            switch (_artOrig)
            {
                case BarrelSettings b:
                    any = Has(UpgradeKind.MainLeste);
                    var bc = Instantiate(b);
                    bc.rounds = b.rounds + Mathf.RoundToInt(V3(UpgradeKind.MainLeste));
                    bc.reloadTime = Mathf.Max(0.2f, b.reloadTime * (1f - Mathf.Min(0.8f, V2(UpgradeKind.MainLeste))));
                    copy = bc;
                    break;
                case FireLanternSettings f:
                    any = Has(UpgradeKind.LongSouffle) || Has(UpgradeKind.FlammeLarge) || Has(UpgradeKind.MecheVive) || Has(UpgradeKind.Braises)
                          || Has(UpgradeKind.SouffleDouble) || Has(UpgradeKind.FeuGregeois);
                    var fc = Instantiate(f);
                    fc.range = f.range + V(UpgradeKind.LongSouffle);
                    fc.coneAngle = Mythic(UpgradeKind.FlammeLarge) ? 360f : Mathf.Min(170f, f.coneAngle + V(UpgradeKind.FlammeLarge));
                    fc.cooldown = Mathf.Max(0.3f, f.cooldown - V(UpgradeKind.MecheVive));
                    fc.burnDuration = f.burnDuration + V(UpgradeKind.Braises);
                    fc.burnMaxStacks = Mathf.Max(1, Mathf.RoundToInt(V2(UpgradeKind.Braises)));
                    fc.jetExtraCharges = Mathf.RoundToInt(V(UpgradeKind.SouffleDouble));
                    fc.extraChargeRechargeFactor = Has(UpgradeKind.SouffleDouble) ? Mathf.Max(0.1f, V2(UpgradeKind.SouffleDouble)) : 1f;
                    fc.pushOnHit = AtLeast(UpgradeKind.FlammeLarge, 2) ? 3f : 0f;
                    copy = fc;
                    break;
                case BoneSwordSettings s:
                    any = Has(UpgradeKind.LameLongue) || Has(UpgradeKind.Enchainement) || Has(UpgradeKind.Fente);
                    var sc = Instantiate(s);
                    sc.range = s.range + V(UpgradeKind.LameLongue);
                    sc.arcHalfAngle = Mathf.Min(180f, s.arcHalfAngle + V2(UpgradeKind.LameLongue));
                    if (AtLeast(UpgradeKind.LameLongue, 2))
                        sc.assistRange = Mathf.Max(sc.assistRange, 8f);
                    sc.thirdHitFullCircle = Mythic(UpgradeKind.LameLongue);
                    sc.swingInterval = Mathf.Max(0.08f, s.swingInterval * (1f - V(UpgradeKind.Enchainement)));
                    sc.comboWindow = s.comboWindow + V2(UpgradeKind.Enchainement);
                    if (AtLeast(UpgradeKind.Enchainement, 2))
                        sc.comboRecovery = 0f;
                    sc.comboHits = Mythic(UpgradeKind.Enchainement) ? 4 : 3;
                    sc.lungeLength = s.lungeLength + V(UpgradeKind.Fente);
                    if (Has(UpgradeKind.Fente))
                        sc.lungeDamageFactor = s.lungeDamageFactor * V2(UpgradeKind.Fente);
                    copy = sc;
                    break;
                case SpiritBookSettings k:
                    any = Has(UpgradeKind.LienVorace) || Has(UpgradeKind.Sangsue) || Has(UpgradeKind.LonguePortee) || Has(UpgradeKind.SoinLie);
                    var kc = Instantiate(k);
                    kc.drainDps = k.drainDps + V(UpgradeKind.LienVorace);
                    kc.drainReturn = Mathf.Max(k.drainReturn, V(UpgradeKind.Sangsue));
                    kc.castRange = k.castRange + V(UpgradeKind.LonguePortee);
                    kc.holdRange = k.holdRange + V(UpgradeKind.LonguePortee);
                    kc.range = k.range + V(UpgradeKind.LonguePortee);
                    kc.sightGrace = Mathf.Max(k.sightGrace, V2(UpgradeKind.LonguePortee));
                    kc.healDps = Mathf.Max(k.healDps, V(UpgradeKind.SoinLie));
                    copy = kc;
                    break;
            }

            if (copy == null)
                return;
            if (!any && _artCopy == null)
            {
                Discard(copy);
                return;                                                  // rien à changer : l'asset partagé reste équipé
            }

            Discard(_artCopy);
            copy.name = _artOrig.name + " (améliorations)";
            _artCopy = copy;
            _holder.Equip(copy);
        }

        /// <summary>Réglages globaux partagés entre les joueurs (le plus fort l'emporte : zone de tête du caméléon).</summary>
        private static void RefreshStatics()
        {
            float head = 1f;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                var u = p != null ? p.GetComponent<PlayerUpgrades>() : null;
                if (u != null)
                    head = Mathf.Max(head, 1f + u.V(UpgradeKind.OeilFendu));
            }

            Hitbox.HeadScale = head;
        }

        private static void Discard(Object o)
        {
            if (o == null)
                return;
            if (Application.isPlaying)
                Destroy(o);
            else
                DestroyImmediate(o);
        }

        private void OnDestroy()
        {
            foreach (Object o in new Object[] { _moveCopy, _magCopy, _artCopy, _parryCopy })
                if (o != null)
                    Destroy(o);
        }

        // ---------- Crochets statiques (un identifiant de joueur → sa valeur) ----------

        private static bool _hooksInstalled;

        /// <summary>Branche les crochets des systèmes (idempotent) ; appelé par chaque PlayerUpgrades et par SoulSystem.</summary>
        public static void InstallHooks()
        {
            if (_hooksInstalled)
                return;
            _hooksInstalled = true;
            ImpactDamager.OwnerDamageMultiplier = ThrowMultiplier;
            ImpactDamager.OwnerKeepsDurability = id => { var u = OfId(id); return u != null && u.AtLeast(UpgradeKind.ProjectilesLourds, 2); };
            RepulsionWave.RepelDamageFor = id => { var u = OfId(id); return u != null ? u.RepelDamage * u.GlobalMultiplier() : 0f; };
            RepulsionWave.RepelSlowFor = id => { var u = OfId(id); return u != null ? u.V2(UpgradeKind.OndeBlessante) : 0f; };
            EnemyBody.WallDamageMultiplierFor = id => { var u = OfId(id); return u != null && u.Has(UpgradeKind.Rebond) ? u.V(UpgradeKind.Rebond) : 1f; };
            EnemyBody.StunBonusFor = id => { var u = OfId(id); return u != null && u.AtLeast(UpgradeKind.BriseurDOs, 2) ? 0.5f : 0f; };
            ItHunts.Enemies.EnemySenses.DetectionScaleFor = DetectionScale;
        }

        /// <summary>Multiplicateur des dégâts des objets lancés ou repoussés par ce joueur (Projectiles lourds, piles de combat).</summary>
        public static float ThrowMultiplier(int playerId)
        {
            PlayerUpgrades u = OfId(playerId);
            return u != null ? (1f + u.ThrowDamageBonus) * u.GlobalMultiplier() : 1f;
        }

        /// <summary>Piles temporaires qui touchent tous les dégâts : Prédateur (kills), Rythme de fer (parades), Peau épaisse Mythique.</summary>
        public float GlobalMultiplier()
        {
            float m = 1f;
            if (_predatorStacks > 0 && Time.time < _predatorUntil)
                m += _predatorStacks * V(UpgradeKind.Predateur);
            if (_parryStreak > 0 && Time.time - _lastParry <= 6f)
                m += _parryStreak * V(UpgradeKind.RythmeDeFer);
            if (Mythic(UpgradeKind.PeauEpaisse) && _health != null && _health.CurrentHealth <= 0.25f * _health.MaxHealth)
                m *= 1.3f;
            return m;
        }

        /// <summary>
        /// Dégâts finaux d'un coup de ce joueur sur une cible (balle, épée, feu, lien, zone) : piles globales, bonus contre les
        /// étourdis / paralysés (Briseur d'os, Brise-garde à l'épée, Balles paralysantes Or), critique d'après parade (Garde d'os),
        /// exécutions (Briseur d'os et Brise-garde Mythiques). Effets de bord voulus : Brise-garde Or prolonge l'étourdissement,
        /// Prédateur Mythique étourdit à 5 piles. Le traqueur hors arène ne prend rien de toute façon (Damageable.ApplyDamage).
        /// </summary>
        public static float Outgoing(int playerId, Damageable target, float damage, bool melee = false, bool bullet = false)
        {
            PlayerUpgrades u = OfId(playerId);
            if (u == null || target == null || damage <= 0f)
                return damage;
            float m = u.GlobalMultiplier();
            bool cc = target.Stunned || target.Paralyzed;
            if (cc && u.Has(UpgradeKind.BriseurDOs))
                m *= u.V(UpgradeKind.BriseurDOs);
            if (bullet && target.Paralyzed && u.AtLeast(UpgradeKind.BallesParalysantes, 2))
                m *= 1.15f;
            if (melee && (cc || target.Bleeding) && u.Has(UpgradeKind.BriseGarde))
                m *= u.V(UpgradeKind.BriseGarde);
            if (melee && u._swordCritPending)
            {
                m *= Mathf.Max(1f, u.V2(UpgradeKind.GardeDOs));
                u._swordCritPending = false;
            }

            float result = damage * m;
            float hp = target.Health.Current, max = target.Health.Max;
            if (cc && !target.ImmuneToOrbs)
            {
                if ((u.Mythic(UpgradeKind.BriseurDOs) && hp <= 0.3f * max) || (melee && u.Mythic(UpgradeKind.BriseGarde) && hp <= 0.4f * max))
                    result = Mathf.Max(result, hp);                       // exécution
                if (melee && u.AtLeast(UpgradeKind.BriseGarde, 2))
                    target.ExtendStun(0.5f);
            }

            if (u.Mythic(UpgradeKind.Predateur) && u._predatorStacks >= 5 && Time.time < u._predatorUntil && target.Body != null)
                target.Body.StunFor(0.5f);
            return result;
        }

        /// <summary>Dégâts reçus × (Fourrure de braise Or, Peau épaisse, Peau épaisse Mythique sous 25 %).</summary>
        private float IncomingScale()
        {
            float s = (1f - V2(UpgradeKind.FourrureDeBraise)) * (1f - V(UpgradeKind.PeauEpaisse));
            if (Mythic(UpgradeKind.PeauEpaisse) && _health != null && _health.CurrentHealth <= 0.25f * _health.MaxHealth)
                s *= 0.5f;
            return Mathf.Clamp01(s);
        }

        /// <summary>Chute dans le vide : −20 PV → la valeur de Chute amortie ; Plumes : aucun dégât de chute.</summary>
        private float FallDamageScale(float damage)
        {
            if (Has(UpgradeKind.Plumes))
                return 0f;
            return Has(UpgradeKind.ChuteAmortie) ? V(UpgradeKind.ChuteAmortie) : damage;
        }

        /// <summary>Cœur solide Mythique : une fois par run, revenir à 50 % de PV au lieu de mourir (jamais face à la prise du traqueur).</summary>
        private bool _secondChanceUsed;

        private bool TrySecondChance()
        {
            if (!Mythic(UpgradeKind.CoeurSolide) || _secondChanceUsed)
                return false;
            _secondChanceUsed = true;
            return true;
        }

        /// <summary>Facteur de détection d'un joueur par les ennemis (Souffle court, Peau changeante ; 0 : invisible pour le traqueur).</summary>
        public static float DetectionScale(PlayerHealth p)
        {
            var u = p != null ? p.GetComponent<PlayerUpgrades>() : null;
            if (u == null)
                return 1f;
            if (Time.time < u._invisibleUntil)
                return 0f;
            float s = 1f - u.V(UpgradeKind.SouffleCourt);
            if (u.Camouflaged)
                s *= 1f - u.V2(UpgradeKind.PeauChangeante);
            return Mathf.Clamp01(s);
        }

        /// <summary>Prix d'un achat × (Marchandage au marchand ; Œil du collectionneur aux portes).</summary>
        public static float PriceScale(int playerId, PriceKind kind)
        {
            var u = OfId(playerId);
            if (u == null)
                return 1f;
            return kind switch
            {
                PriceKind.Marchand or PriceKind.Soin => 1f - u.V(UpgradeKind.Marchandage),
                PriceKind.Porte => 1f - u.V2(UpgradeKind.OeilDuCollectionneur),
                _ => 1f,
            };
        }

        /// <summary>Hausse par achat × (Marchandage Or : divisée par 2).</summary>
        public static float IncreaseScale(int playerId, PriceKind kind)
        {
            var u = OfId(playerId);
            return u != null && kind == PriceKind.Marchand && u.AtLeast(UpgradeKind.Marchandage, 2) ? 0.5f : 1f;
        }

        /// <summary>Choix en plus dans les coffres (Œil du collectionneur).</summary>
        public static int ExtraChestChoices(int playerId)
        {
            var u = OfId(playerId);
            return u != null ? Mathf.RoundToInt(u.V(UpgradeKind.OeilDuCollectionneur)) : 0;
        }

        /// <summary>Âmes lâchées par un ennemi tué par ce joueur (Âmes gourmandes ; Pages brûlantes s'il mourait lié).</summary>
        public static int SoulsOnKill(int attackerId, int souls, Damageable d)
        {
            var u = OfId(attackerId);
            if (u == null || souls <= 0)
                return souls;
            float speed = u._motor != null ? u._motor.State.HorizontalSpeed : 0f;
            float n = souls * (1f + u.V(UpgradeKind.AmesGourmandes)) + u._soulCarry;
            int whole = Mathf.FloorToInt(n);
            u._soulCarry = n - whole;
            if (speed > 14f)
            {
                if (u.Mythic(UpgradeKind.AmesGourmandes))
                    whole *= 2;
                else if (u.AtLeast(UpgradeKind.AmesGourmandes, 2))
                    whole += 1;
            }

            if (d != null && d.Bound && d.BoundBy == attackerId)
                whole += Mathf.RoundToInt(u.V(UpgradeKind.PagesBrulantes));
            return whole;
        }

        private float _soulCarry;

        // ---------- Événements ----------

        private int _predatorStacks, _parryStreak;
        private float _predatorUntil, _lastParry = float.NegativeInfinity, _lastHurt = float.NegativeInfinity;
        private float _freeDashUntil, _freeTractionUntil, _invisibleUntil, _speedBoostUntil, _speedBoost;
        private bool _swordCritPending;

        /// <summary>Camouflage du caméléon (Peau changeante) : immobile et sans tirer depuis assez longtemps.</summary>
        public bool Camouflaged { get; private set; }
        private float _stillSince = float.NegativeInfinity;
        private bool _camouflageCrit;

        /// <summary>Tir depuis le camouflage (Mythique) : critique ×3 garanti, une fois ; le tir casse le camouflage.</summary>
        public bool ConsumeCamouflageCrit()
        {
            bool crit = Camouflaged && Mythic(UpgradeKind.PeauChangeante) && !_camouflageCrit;
            _stillSince = Time.time;                                     // le tir casse le camouflage
            Camouflaged = false;
            _camouflageCrit = false;
            return crit;
        }

        private void OnParried(ParryEvent e)
        {
            if (e.Player != transform)
                return;
            int id = PlayerId;
            Vector3 at = transform.position + Vector3.up;
            if (Has(UpgradeKind.Riposte))
                UpgradeEffects.Explode(at, V2(UpgradeKind.Riposte), V(UpgradeKind.Riposte), id, V3(UpgradeKind.Riposte), 0f, false, 0,
                    AtLeast(UpgradeKind.Riposte, 2) ? 8f : 0f);
            if (Has(UpgradeKind.SecondSouffle) && _health != null)
            {
                _health.Heal(V(UpgradeKind.SecondSouffle));
                if (AtLeast(UpgradeKind.SecondSouffle, 2))
                    _motor?.GrantCharges(1f, Mythic(UpgradeKind.SecondSouffle) ? 1f : 0f);
            }

            if (Has(UpgradeKind.RythmeDeFer))
            {
                _parryStreak = Time.time - _lastParry <= 6f ? Mathf.Min(_parryStreak + 1, Mathf.RoundToInt(V2(UpgradeKind.RythmeDeFer))) : 1;
                if (Mythic(UpgradeKind.RythmeDeFer) && _parryStreak >= 3 && Application.isPlaying)
                    StartCoroutine(SlowTime(1f));
            }

            _lastParry = Time.time;
            if (Mythic(UpgradeKind.ElanDeTraction))
                _freeTractionUntil = Time.time + 4f;
            if (Mythic(UpgradeKind.SouffleCourt))
                _invisibleUntil = Time.time + 2f;
            if (Has(UpgradeKind.GardeDOs))
            {
                _swordCritPending = true;
                var target = e.Attacker != null ? e.Attacker.GetComponentInParent<Damageable>() : null;
                if (AtLeast(UpgradeKind.GardeDOs, 2) && target != null && !target.Dead)
                {
                    target.NoteAttacker(id);
                    float taken = target.ApplyDamage(Outgoing(id, target, 10f, melee: true), Time.time, out bool killed);
                    if (taken > 0f)
                        DamageEvents.Raise(new DamageEvent { Target = target, Damage = taken, Point = e.Point, Killed = killed, Tier = HitTier.Moyen, Time = Time.time, Melee = true, AttackerId = id });
                }

                if (Mythic(UpgradeKind.GardeDOs) && _holder != null)
                    _holder.TriggerSwordStrike(2);                      // riposte automatique : le 3e coup
            }
        }

        /// <summary>Rythme de fer Mythique : le temps ralentit 1 s autour du joueur (effet local, pas en coop en ligne).</summary>
        private System.Collections.IEnumerator SlowTime(float seconds)
        {
            if (HitStop.Active || Time.timeScale < 0.99f)
                yield break;
            Time.timeScale = 0.35f;
            yield return new WaitForSecondsRealtime(seconds);
            if (!HitStop.Active)
                Time.timeScale = 1f;
        }

        private void OnDied(Damageable d)
        {
            if (d == null || d.LastAttackerId != PlayerId || Time.time - d.LastAttackTime > 5f)
                return;
            int id = PlayerId;
            Vector3 at = d.Center;
            if (Has(UpgradeKind.Predateur))
            {
                _predatorStacks = Time.time < _predatorUntil ? Mathf.Min(_predatorStacks + 1, Mathf.RoundToInt(V2(UpgradeKind.Predateur))) : 1;
                _predatorUntil = Time.time + 5f;
            }

            if (Mythic(UpgradeKind.OrbeVorace))
                _motor?.GrantCharges(0f, 1f);
            if (Mythic(UpgradeKind.ChargeEnPlus))
                _freeDashUntil = Time.time + 2f;
            if (Has(UpgradeKind.MecheVive) && AtLeast(UpgradeKind.MecheVive, 2) && d.Burning)
                _holder?.ReduceCooldown(V2(UpgradeKind.MecheVive));
            if (Mythic(UpgradeKind.OsBrise) && d.Bleeding && _health != null)
                _health.Heal(8f);
            if (UpgradeEffects.InExplosion)
                return;                                                  // les morts par explosion sont chaînées par l'explosion elle-même
            if (Has(UpgradeKind.Detonateur) && d.StunnedOrParalyzed)
                UpgradeEffects.Explode(at, V2(UpgradeKind.Detonateur), V(UpgradeKind.Detonateur), id, V3(UpgradeKind.Detonateur), 0f, false,
                    Mythic(UpgradeKind.Detonateur) ? 2 : 0, 0f, d);
            if (Has(UpgradeKind.Allumette) && d.Burning)
                UpgradeEffects.IgniteAround(at, V(UpgradeKind.Allumette), 12f, V2(UpgradeKind.Allumette), id, V3(UpgradeKind.Allumette), 1, d);
            if (Has(UpgradeKind.Combustion) && d.Burning)
                UpgradeEffects.Explode(at, V2(UpgradeKind.Combustion), V(UpgradeKind.Combustion), id, 0f, 0f, AtLeast(UpgradeKind.Combustion, 2),
                    Mythic(UpgradeKind.Combustion) ? 2 : 0, 0f, d);
            if (Mythic(UpgradeKind.Rebond) && d.Body != null && (d.Body.Knocked || d.Body.InFlight))
                UpgradeEffects.Explode(at, 3.5f, 30f, id, 0f, 0f, false, 0, 0f, d);
        }

        private void OnHit(DamageEvent e)
        {
            if (!e.Melee || !e.Killed || e.AttackerId != PlayerId || !Has(UpgradeKind.ElanDuBucheron))
                return;
            // Élan du bûcheron (K8) : un kill à l'épée rend une charge et donne de la vitesse.
            _motor?.GrantCharges(1f, Mythic(UpgradeKind.ElanDuBucheron) ? 1f : 0f);
            _speedBoost = V(UpgradeKind.ElanDuBucheron);
            _speedBoostUntil = Time.time + V2(UpgradeKind.ElanDuBucheron);
            if (AtLeast(UpgradeKind.ElanDuBucheron, 2))
                _freeDashUntil = Mathf.Max(_freeDashUntil, Time.time + 1f);
        }

        private void OnHurt(PlayerHurtEvent e)
        {
            if (e.Victim == null || e.Victim.PlayerId != PlayerId)
                return;
            _lastHurt = Time.time;
            Camouflaged = false;
            _stillSince = Time.time;
            if (!Has(UpgradeKind.FourrureDeBraise) || e.Attacker == null)
                return;
            var d = e.Attacker.GetComponentInParent<Damageable>();
            if (d == null || d.Dead || d.ImmuneToOrbs)
                return;
            Burning.Apply(d, 12f, V(UpgradeKind.FourrureDeBraise), PlayerId);
            if (Mythic(UpgradeKind.FourrureDeBraise) && d.Body != null)
            {
                Vector3 away = d.Center - transform.position;
                away.y = 0f;
                if (away.sqrMagnitude > 1e-4f)
                    d.Body.Push(away.normalized * 8f + Vector3.up * 2f);
                d.Body.StunFor(0.8f);
            }
        }

        private void OnIgnited(Damageable d, int attackerId, int stacks, bool fresh)
        {
            if (attackerId != PlayerId || d == null)
                return;
            if (fresh && Mythic(UpgradeKind.Allumette) && d.Body != null)
                d.Body.StunFor(0.5f);
            if (stacks >= 3 && Mythic(UpgradeKind.Braises))
                d.Paralyze(1f);
        }

        private void OnLanded(float fallSpeed)
        {
            if (fallSpeed < HardLandingSpeed || !Has(UpgradeKind.ChuteAmortie))
                return;
            Vector3 at = transform.position;
            if (Mythic(UpgradeKind.ChuteAmortie))
            {
                UpgradeEffects.Explode(at, V3(UpgradeKind.ChuteAmortie), V2(UpgradeKind.ChuteAmortie), PlayerId, 0.8f);
                _motor?.AddVelocity(Vector3.up * 6f);                   // relancé d'un petit saut
            }
            else if (AtLeast(UpgradeKind.ChuteAmortie, 2))
                UpgradeEffects.Explode(at, V3(UpgradeKind.ChuteAmortie), V2(UpgradeKind.ChuteAmortie), PlayerId, 1f, 0f, false, 0, 0f, null, false);
            else if (AtLeast(UpgradeKind.ChuteAmortie, 1))
                UpgradeEffects.StunAround(at, V3(UpgradeKind.ChuteAmortie), 1f);
        }

        private void OnTractionEnded(TractionEnd reason, float speed, Vector3 point)
        {
            _sillageHit.Clear();
            if (reason != TractionEnd.Arrivee)
                return;
            if (Mythic(UpgradeKind.TractionRapide) && speed > 12f)
                _motor?.GrantCharges(0f, 1f);
            if (!Has(UpgradeKind.Harpon) && !Has(UpgradeKind.LienDAncrage))
                return;
            Damageable target = UpgradeEffects.NearestEnemy(point, 2.5f);
            if (target == null)
                return;
            int id = PlayerId;
            float hit = (AtLeast(UpgradeKind.Harpon, 2) ? V(UpgradeKind.Harpon) : 0f) + V2(UpgradeKind.LienDAncrage);
            if (hit > 0f)
            {
                target.NoteAttacker(id);
                float taken = target.ApplyDamage(Outgoing(id, target, hit), Time.time, out bool killed);
                if (taken > 0f)
                    DamageEvents.Raise(new DamageEvent { Target = target, Damage = taken, Point = target.Center, Killed = killed, Tier = HitTier.Moyen, Time = Time.time, AttackerId = id });
            }

            if (V2(UpgradeKind.Harpon) > 0f && target.Body != null)
                target.Body.StunFor(V2(UpgradeKind.Harpon));
            if (Mythic(UpgradeKind.Harpon))
                UpgradeEffects.Explode(target.Center, V3(UpgradeKind.Harpon), 30f, id, 0f, 0f, false, 0, 0f, target);
        }

        private void OnRepelled(Vector3 eye, Vector3 aim, Vector3 origin, Rigidbody held)
        {
            if (Mythic(UpgradeKind.OndeBlessante) && Application.isPlaying)
                StartCoroutine(SecondWave(eye, aim, origin, held));
        }

        private System.Collections.IEnumerator SecondWave(Vector3 eye, Vector3 aim, Vector3 origin, Rigidbody held)
        {
            yield return new WaitForSeconds(0.3f);
            if (_motor != null && _motor.MagnetismSettings != null && (_health == null || !_health.Dead))
                RepulsionWave.Apply(eye, aim, origin, _motor.MagnetismSettings, held, transform);
        }

        private readonly HashSet<Damageable> _sillageHit = new HashSet<Damageable>();
        private float _nextPuddle;
        private int _lastDashTick = MovementState.NeverTick;

        private void OnDashed(Vector3 dir)
        {
            if (Mythic(UpgradeKind.FouleeArdente))
                UpgradeEffects.Explode(transform.position + Vector3.up + dir.normalized * 2.5f, 2.5f, 20f, PlayerId, 0f, 0f, true);
        }

        /// <summary>Un tick de simulation du joueur, après son mouvement.</summary>
        private void OnTick()
        {
            if (_motor == null || (_health != null && _health.Dead))
                return;
            MovementState s = _motor.State;
            float now = Time.time;
            if (now < _freeDashUntil)
                _motor.RefillMovementCharges();
            if (now < _freeTractionUntil)
                _motor.RefillAttractCharges();

            // Sillage (G13) : pendant une traction, les ennemis traversés sont blessés, ralentis, enflammés au Mythique.
            if (Has(UpgradeKind.Sillage) && s.Mode == MovementMode.Traction)
                foreach (Damageable d in UpgradeEffects.EnemiesAround(transform.position + Vector3.up, 1.6f))
                {
                    if (!_sillageHit.Add(d))
                        continue;
                    d.NoteAttacker(PlayerId);
                    float taken = d.ApplyDamage(Outgoing(PlayerId, d, V(UpgradeKind.Sillage)), now, out bool killed);
                    if (taken > 0f)
                        DamageEvents.Raise(new DamageEvent { Target = d, Damage = taken, Point = d.Center, Killed = killed, Tier = HitTier.Petit, Time = now, AttackerId = PlayerId });
                    d.Slow(V2(UpgradeKind.Sillage), V3(UpgradeKind.Sillage));
                    if (Mythic(UpgradeKind.Sillage))
                        Burning.Apply(d, 12f, 3f, PlayerId);
                }

            float speed = s.HorizontalSpeed;
            // Foulée ardente (R8) : au-dessus de 14 m/s (ou en glissade dès l'Or), traînée de feu sous les pas.
            if (Has(UpgradeKind.FouleeArdente) && s.Grounded && now >= _nextPuddle
                && (speed > 14f || (AtLeast(UpgradeKind.FouleeArdente, 2) && s.Mode == MovementMode.Slide)))
            {
                _nextPuddle = now + 0.25f;
                FirePuddle.Spawn(transform.position, 1f, V(UpgradeKind.FouleeArdente), V2(UpgradeKind.FouleeArdente), PlayerId);
            }

            // Vif-argent Mythique : au-delà de 16 m/s, on traverse les goules en les étourdissant.
            if (Mythic(UpgradeKind.VifArgent) && speed > 16f)
                foreach (Damageable d in UpgradeEffects.EnemiesAround(transform.position + Vector3.up, 1.4f))
                    if (d.GetComponent<ItHunts.Enemies.Ghoul>() != null && d.Body != null && d.Body.StunFor(1f))
                        StartCoroutine(PassThrough(d));

            // Peau changeante (C-P1) : immobile et sans tirer pendant v s → camouflé.
            if (Has(UpgradeKind.PeauChangeante))
            {
                if (speed > 0.5f || !s.Grounded)
                    _stillSince = now;
                bool was = Camouflaged;
                Camouflaged = now - _stillSince >= V(UpgradeKind.PeauChangeante);
                if (Camouflaged && !was)
                    _camouflageCrit = false;
            }

            if (s.LastDashTick != _lastDashTick && s.LastDashTick == s.Tick)
                OnDashed(s.DashDir);
            _lastDashTick = s.LastDashTick;
        }

        private System.Collections.IEnumerator PassThrough(Damageable d)
        {
            var cc = GetComponent<CharacterController>();
            Collider other = d != null ? d.MainCollider : null;
            if (cc == null || other == null)
                yield break;
            Physics.IgnoreCollision(cc, other, true);
            yield return new WaitForSeconds(0.5f);
            if (cc != null && other != null)
                Physics.IgnoreCollision(cc, other, false);
        }

        private void OnSoulCollected(int collectorId)
        {
            if (collectorId != PlayerId)
                return;
            if (Has(UpgradeKind.OrbeVorace))
            {
                float cut = V(UpgradeKind.OrbeVorace);
                _motor?.ReduceCooldowns(cut, AtLeast(UpgradeKind.OrbeVorace, 2));
                _holder?.ReduceCooldown(cut);
            }

            if (Mythic(UpgradeKind.Aspiration) && _health != null)
                _health.Heal(1f);
        }

        /// <summary>Vision nocturne (H-P1) : contour des ennemis à portée (lueur douce, visible par tous : à faire par vue en coop).</summary>
        private float _nextVision;

        private void Update()
        {
            float now = Time.time;
            // Vitesse de course dynamique : Prédateur Or (+0.5 m/s avec des piles), Élan du bûcheron.
            if (_moveCopy != null)
            {
                float bonus = 0f;
                if (AtLeast(UpgradeKind.Predateur, 2) && _predatorStacks > 0 && now < _predatorUntil)
                    bonus += 0.5f;
                if (now < _speedBoostUntil)
                    bonus += _speedBoost;
                _moveCopy.runSpeed = _baseRunSpeed + bonus;
            }

            // Cœur solide Or : régénération 1 PV / 4 s hors combat (5 s sans coup reçu).
            if (AtLeast(UpgradeKind.CoeurSolide, 2) && _health != null && !_health.Dead && now - _lastHurt > 5f)
                _health.Heal(0.25f * Time.deltaTime);
            if (_health != null)
                _health.ShieldBlocksKill = Mythic(UpgradeKind.Sangsue) && ItHunts.Enemies.ZoneState.BossFightActiveAnywhere;

            if (Has(UpgradeKind.VisionNocturne) && now >= _nextVision)
            {
                _nextVision = now + 0.25f;
                float range = V(UpgradeKind.VisionNocturne);
                var glow = new Color(0.45f, 0.2f, 0.7f) * 0.9f;
                foreach (Damageable d in Damageable.All)
                {
                    if (d == null || d.Dead)
                        continue;
                    bool stalker = d.ImmuneToOrbs;
                    bool seen = stalker ? Mythic(UpgradeKind.VisionNocturne) && Vector3.Distance(d.Center, transform.position) <= 30f
                        : Vector3.Distance(d.Center, transform.position) <= range;
                    d.VisionGlow = seen ? glow : Color.black;
                }
            }
        }
    }
}
