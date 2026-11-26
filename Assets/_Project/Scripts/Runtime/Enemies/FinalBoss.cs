using System.Collections.Generic;
using ItHunts.Audio;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Boss final (nuit 3, point 8) : le traqueur devient blessable dans l'arène en contrebas du jardin. Il ne tue plus en un coup.
    /// Trois phases (vie à 66 % et 33 %) ; panel d'attaques, chacune annoncée (pose, son, marque au sol) : prise (main qui s'abat),
    /// balayage (cône devant lui), charge (ligne), bond (cercle d'onde à l'atterrissage), cri (repousse tout autour), jet de débris,
    /// éruptions au sol sous les joueurs, invocation de goules. Trois points faibles lumineux (nuque, omoplates), en hauteur et dans
    /// son dos : on les atteint depuis les piliers et la statue (traction), ou en le contournant ; chaque point cédé le fait chanceler.
    /// Sa vraie IA (Stalker) est coupée pendant le combat ; ses clips de la DA sont joués tels quels (modèle et animations intacts).
    /// </summary>
    public sealed class FinalBoss : MonoBehaviour
    {
        public enum Attack { Aucune, Prise, Balayage, Charge, Bond, Cri, Debris, Eruptions, Invocation }

        private sealed class WeakPoint
        {
            public Transform Bone;
            public Damageable Target;
            public Transform Visual;
            public Light Glow;
            public float Taken, BrokenUntil = float.NegativeInfinity;
            public string Name;
            public bool Open(float now) => now >= BrokenUntil;
        }

        [SerializeField] private FinalBossSettings settings;
        public FinalBossSettings Settings { get => settings; set => settings = value; }

        public float Health { get; private set; }
        public float MaxHealth => settings != null ? settings.maxHealth : 2400f;
        public int Phase => Health > MaxHealth * settings.phase2At ? 1 : Health > MaxHealth * settings.phase3At ? 2 : 3;
        public bool Dead { get; private set; }
        public bool Started { get; private set; }
        public Attack Current { get; private set; }
        public float TelegraphLeft { get; private set; }
        public int AttacksDone { get; private set; }
        public readonly Dictionary<Attack, int> AttackCounts = new Dictionary<Attack, int>();
        public static FinalBoss Current_ { get; private set; }
        public static event System.Action<FinalBoss> Defeated;
        /// <summary>Le combat commence (barre de vie de la DA) ; ArtBar : la barre de la DA est posée (pas de barre provisoire).</summary>
        public static event System.Action<FinalBoss> FightStarted;
        public static bool ArtBar { get; set; }
        public Vector3 ArenaCenter { get; set; }
        public float ArenaRadius { get; set; } = 40f;
        /// <summary>Goules invoquées (prefab posé par l'arène).</summary>
        public GameObject GhoulPrefab { get; set; }

        private Stalker _stalker;
        private Animator _anim;
        private Rigidbody _rb;
        private readonly List<WeakPoint> _weak = new List<WeakPoint>();
        private float _nextAttack, _phaseStart, _staggerUntil = float.NegativeInfinity, _stateStart;
        private int _phaseShown = 1;
        private Vector3 _chargeDir, _leapFrom, _leapTo;
        private readonly List<Vector3> _marks = new List<Vector3>();
        private Material _weakMat;
        private enum Step { Annonce, Action, Recuperation }
        private Step _step;
        private string _clip;
        private System.Random _rng = new System.Random(7);

        public void Begin(FinalBossSettings s, Vector3 center, float radius)
        {
            settings = s;
            ArenaCenter = center;
            ArenaRadius = radius;
            Health = MaxHealth;
            Started = true;
            _nextAttack = Time.time + 3f;
            Current_ = this;
            _stalker = GetComponent<Stalker>();
            if (_stalker != null)
                _stalker.enabled = false;                                // la poursuite de Pac-Man laisse la place au combat
            foreach (MonoBehaviour m in GetComponents<MonoBehaviour>())
                if (m is StalkerProceduralPose)
                    m.enabled = false;
            _anim = GetComponentInChildren<Animator>();
            if (_anim != null)
            {
                _anim.enabled = true;
                _anim.applyRootMotion = false;
            }

            _rb = GetComponent<Rigidbody>();
            if (_rb != null)
                _rb.isKinematic = true;
            BuildMaterials();
            BuildWeakPoints();
            BuildBodyTarget();
            Play("Traqueur_Repos");
            GameAudio.PlayAt(GameAudio.Pick(GameAudio.Settings?.stalkerEnragedCries), transform.position + Vector3.up * 10f, 1f, 120f);
            DamageEvents.Hit += OnHit;
            ArtBar = false;
            FightStarted?.Invoke(this);
            if (!ArtBar)
                ItHunts.Hud.BossHealthBar.Show(this);
        }

        private void OnDestroy()
        {
            DamageEvents.Hit -= OnHit;
            if (_rootTarget != null && _rootTarget.HitProxy == _bodyTarget)
                _rootTarget.HitProxy = null;
            if (Current_ == this)
                Current_ = null;
        }

        private void BuildMaterials()
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            // Toutes les marques au sol passent par MarquageBoss (DA, PR #50) : plus aucune marque de repli dessinée ici (jour 5, point 5).
            _weakMat = new Material(sh) { name = "Point faible (lueur)" };
            _weakMat.SetColor("_BaseColor", new Color(3.2f, 2.2f, 0.7f, 1f));
        }

        private void BuildWeakPoints()
        {
            var bones = new Dictionary<string, Transform>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                bones[t.name] = t;
            foreach ((string bone, string name, bool head) in new[] { ("Cou_1", "nuque", true), ("Clavicule_D", "omoplate droite", false), ("Clavicule_G", "omoplate gauche", false) })
            {
                if (!bones.TryGetValue(bone, out Transform b))
                    continue;
                var go = new GameObject($"Point faible : {name}");
                go.transform.SetParent(transform, true);
                go.transform.position = b.position - transform.forward * 0.9f;
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 1.1f;
                var hb = go.AddComponent<Hitbox>();
                hb.Head = head;
                hb.ApplyScale();
                var d = go.AddComponent<Damageable>();
                d.Unkillable = true;                                     // ne meurt jamais : ni mort comptée, ni barre de PV
                var vis = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(vis.GetComponent<Collider>());
                vis.name = "Lueur";
                vis.transform.SetParent(go.transform, false);
                vis.transform.localScale = Vector3.one * 1.1f;
                vis.GetComponent<MeshRenderer>().sharedMaterial = _weakMat;
                var lgo = new GameObject("Lumière du point faible");
                lgo.transform.SetParent(go.transform, false);
                var l = lgo.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.75f, 0.3f);
                l.range = 6f;
                l.intensity = 2.5f;
                _weak.Add(new WeakPoint { Bone = b, Target = d, Visual = vis.transform, Glow = l, Name = name });
            }
        }

        public int WeakPointsOpen
        {
            get
            {
                int n = 0;
                foreach (WeakPoint w in _weak)
                    if (w.Open(Time.time))
                        n++;
                return n;
            }
        }

        // ---------- Dégâts (jour 5, point 5) : partout sur le corps, critiques sur un point faible ----------

        private Damageable _bodyTarget, _rootTarget;
        private CapsuleCollider _capsule;
        private float _nextCritFx, _nextHitSound;

        /// <summary>Cible du corps (relais) : chaque coup sur un collider du boss lui est donné (Damageable.HitProxy).</summary>
        public Damageable BodyTarget => _bodyTarget;

        /// <summary>
        /// Le traqueur reste intouchable (ImmuneToOrbs : ni répulsion, ni étourdissement, ni prise) ; pendant le combat, ses colliders
        /// renvoient les coups des armes à un relais qui ne meurt jamais : chaque arme (épée, balles, flamme, drain, objets lancés)
        /// le blesse partout.
        /// </summary>
        private void BuildBodyTarget()
        {
            _rootTarget = GetComponent<Damageable>();
            _capsule = GetComponent<CapsuleCollider>();
            var go = new GameObject("Corps du boss (cible)");
            go.transform.SetParent(transform, false);
            _bodyTarget = go.AddComponent<Damageable>();
            _bodyTarget.Unkillable = true;
            if (_rootTarget != null)
                _rootTarget.HitProxy = _bodyTarget;
            UpdateBodyTarget();
        }

        /// <summary>Le relais suit le centre du corps (visée du livre, aide de l'épée, dégâts de zone).</summary>
        private void UpdateBodyTarget()
        {
            if (_bodyTarget == null)
                return;
            _bodyTarget.transform.position = _capsule != null ? _capsule.bounds.center - Vector3.up : transform.position + Vector3.up * 3f;
        }

        /// <summary>
        /// Dégâts au boss d'un coup (logique pure) : point faible = coup critique (× weakPointMultiplier) ; corps = × bodyDamageShare,
        /// ou × bodyThrowShare pour un objet lancé ou repoussé (impact).
        /// </summary>
        public static float HitDamage(float damage, bool weakPoint, bool impact, FinalBossSettings s)
        {
            if (damage <= 0f || s == null)
                return 0f;
            return damage * (weakPoint ? s.weakPointMultiplier : impact ? s.bodyThrowShare : s.bodyDamageShare);
        }

        /// <summary>Un objet (lancé, repoussé, ennemi lancé) qui frappe, pas une arme : ni balle, ni épée, avec une masse.</summary>
        public static bool IsImpact(in DamageEvent e) => !e.Bullet && !e.Melee && e.SourceMass > 0f;

        /// <summary>Un coup sur un point faible (critique) ou sur le corps (relais) : dégâts au boss.</summary>
        private void OnHit(DamageEvent e)
        {
            if (Dead || !Started || e.Target == null)
                return;
            if (e.Target == _bodyTarget)
            {
                TakeDamage(HitDamage(e.Damage, false, IsImpact(e), settings));
                return;
            }

            foreach (WeakPoint w in _weak)
            {
                if (e.Target != w.Target)
                    continue;
                if (w.Target.Dead)
                    w.Target.Revive();
                if (!w.Open(Time.time))
                    return;
                float dmg = HitDamage(e.Damage, true, IsImpact(e), settings);
                TakeDamage(dmg);
                GameVfxSettings vfx = GameVfx.Settings;
                if (vfx != null && vfx.bossWound != null)
                    GameVfx.Spawn(vfx.bossWound, e.Point, Quaternion.identity, 1.5f, null, 2f);
                if (Time.time >= _nextCritFx)
                {
                    // Critique lisible : éclat et son de critique de la DA (au plus toutes les 0.15 s : flamme et drain frappent en continu).
                    _nextCritFx = Time.time + 0.15f;
                    if (vfx != null && vfx.critical != null)
                        GameVfx.Spawn(vfx.critical, e.Point, Quaternion.identity, 1.5f, null, 1.5f);
                    GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.headshot : null, e.Point, 0.8f, 80f);
                }

                w.Taken += dmg;
                if (w.Taken >= settings.weakPointBreak)
                {
                    w.Taken = 0f;
                    w.BrokenUntil = Time.time + settings.weakPointRegrow;
                    Stagger();
                }

                return;
            }
        }

        /// <summary>Dégâts au boss (points faibles, corps).</summary>
        public void TakeDamage(float amount)
        {
            if (Dead || amount <= 0f)
                return;
            Health = Mathf.Max(0f, Health - amount);
            if (Time.time >= _nextHitSound)
            {
                _nextHitSound = Time.time + 0.25f;                       // flamme, drain, saignement frappent en continu : pas de rafale
                GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.bossHit : null, transform.position + Vector3.up * 11f, 1f, 120f);
            }

            if (Health <= 0f)
                Die();
        }

        private void Stagger()
        {
            _staggerUntil = Time.time + settings.staggerTime;
            ClearTelegraphs();
            Current = Attack.Aucune;
            Play("Traqueur_Reception", 1.4f);
            GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.bossWeakPoint : null, transform.position + Vector3.up * 11f, 1f, 120f);
            ShakeAll(0.5f, 60f);
        }

        private void Die()
        {
            Dead = true;
            ClearTelegraphs();
            Play("Traqueur_Reception", 0.5f);
            GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.bossDeath : null, transform.position + Vector3.up * 10f, 1f, 150f);
            GameAudio.PlayAt(GameAudio.Settings?.stalkerGrabCry, transform.position + Vector3.up * 10f, 1f, 150f);
            ShakeAll(1f, 120f);
            if (_rootTarget != null && _rootTarget.HitProxy == _bodyTarget)
                _rootTarget.HitProxy = null;                             // mort : le traqueur redevient intouchable
            foreach (WeakPoint w in _weak)
                if (w.Visual != null)
                    w.Visual.parent.gameObject.SetActive(false);
            _deathAt = Time.time;
            Defeated?.Invoke(this);
        }

        private float _deathAt;

        private void Play(string clip, float speed = 1f)
        {
            if (_anim == null || !_anim.isActiveAndEnabled)
                return;
            _anim.speed = speed;
            if (_clip == clip)
                return;
            _clip = clip;
            _anim.CrossFadeInFixedTime(clip, 0.25f, 0);
        }

        private static PlayerHealth Target(Vector3 from)
        {
            PlayerHealth best = null;
            float bd = float.PositiveInfinity;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                float d = (p.transform.position - from).sqrMagnitude;
                if (d < bd)
                {
                    bd = d;
                    best = p;
                }
            }

            return best;
        }

        private float Telegraph(float baseTime) =>
            Phase >= 3 ? Mathf.Max(settings.minTelegraph, baseTime * settings.phase3TelegraphFactor) : baseTime;

        private void Update()
        {
            if (!Started || settings == null)
                return;
            float now = Time.time;
            UpdateWeakPoints(now);
            UpdateBodyTarget();
            if (Dead)
            {
                // Il s'effondre : il s'enfonce lentement et se couche.
                float u = Mathf.Clamp01((now - _deathAt) / 3f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(transform.forward, Vector3.up) * Quaternion.Euler(-70f * u, 0f, 0f), Time.deltaTime * 2f);
                return;
            }

            if (Phase != _phaseShown)
            {
                _phaseShown = Phase;
                _phaseStart = now;
                GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.bossPhase : null, transform.position + Vector3.up * 10f, 1f, 150f);
                GameAudio.PlayAt(GameAudio.Pick(GameAudio.Settings?.stalkerEnragedCries), transform.position + Vector3.up * 10f, 1f, 150f);
                ShakeAll(0.6f, 120f);
                _nextAttack = now + 1.2f;
            }

            if (now < _staggerUntil)
                return;
            PlayerHealth target = Target(transform.position);
            if (target == null)
                return;
            if (Current == Attack.Aucune)
            {
                Chase(target, now);
                if (now >= _nextAttack)
                    StartAttack(Choose(target), target, now);
                return;
            }

            RunAttack(target, now);
        }

        private void UpdateWeakPoints(float now)
        {
            float pulse = 0.85f + 0.25f * Mathf.Sin(now * 5f);
            foreach (WeakPoint w in _weak)
            {
                if (w.Bone == null)
                    continue;
                Transform root = w.Target.transform;
                root.position = w.Bone.position - transform.forward * 0.9f + Vector3.up * 0.2f;
                bool open = w.Open(now) && !Dead;
                w.Visual.gameObject.SetActive(open);
                w.Glow.enabled = open;
                w.Visual.localScale = Vector3.one * 1.1f * pulse;
                w.Target.GetComponent<Collider>().enabled = open;
            }
        }

        private void Chase(PlayerHealth target, float now)
        {
            Vector3 to = target.transform.position - transform.position;
            to.y = 0f;
            float speed = settings.speedByPhase[Mathf.Clamp(Phase - 1, 0, 2)];
            if (to.magnitude < settings.grabRange * 0.7f)
                speed = 0f;
            Face(to, settings.turnRate);
            Move(transform.forward * speed * Time.deltaTime);
            Play(speed > 7f ? "Traqueur_CourseEnragee_A" : speed > 0.1f ? "Traqueur_Marche_A" : "Traqueur_Repos", speed > 7f ? speed / 11f : speed > 0.1f ? speed / 1.27f * 0.25f : 1f);
        }

        private void Face(Vector3 dir, float rate)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f)
                return;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), rate * Time.deltaTime);
        }

        private void Move(Vector3 delta)
        {
            Vector3 p = transform.position + delta;
            Vector3 off = p - ArenaCenter;
            off.y = 0f;
            if (off.magnitude > ArenaRadius - 6f)
                p = ArenaCenter + off.normalized * (ArenaRadius - 6f) + Vector3.up * (p.y - ArenaCenter.y);
            p.y = ArenaCenter.y;
            transform.position = p;
            if (_rb != null)
                _rb.position = p;
        }

        private Attack Choose(PlayerHealth target)
        {
            float d = Vector3.Distance(Flat(target.transform.position), Flat(transform.position));
            var options = new List<(Attack a, float w)>();
            int ph = Phase;
            if (d < settings.grabRange)
                options.Add((Attack.Prise, 3f));
            if (d < settings.sweepRange)
                options.Add((Attack.Balayage, 2.5f));
            if (d > 16f)
                options.Add((Attack.Charge, 2.5f));
            if (d < 12f)
                options.Add((Attack.Cri, ph >= 3 ? 2f : 0.8f));
            if (ph >= 2)
            {
                options.Add((Attack.Bond, d > 12f ? 2.5f : 1f));
                options.Add((Attack.Debris, d > 14f ? 2.5f : 0.8f));
                if (!AttackCounts.ContainsKey(Attack.Invocation) || AttackCounts[Attack.Invocation] < ph - 1)
                    options.Add((Attack.Invocation, 1.5f));
            }

            if (ph >= 3)
                options.Add((Attack.Eruptions, 2.5f));
            if (options.Count == 0)
                options.Add((Attack.Charge, 1f));
            float total = 0f;
            foreach ((Attack a, float w) o in options)
                total += o.w;
            double roll = _rng.NextDouble() * total;
            foreach ((Attack a, float w) o in options)
            {
                roll -= o.w;
                if (roll <= 0)
                    return o.a;
            }

            return options[0].a;
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private void StartAttack(Attack a, PlayerHealth target, float now)
        {
            Current = a;
            _step = Step.Annonce;
            _stateStart = now;
            AttackCounts[a] = AttackCounts.TryGetValue(a, out int n) ? n + 1 : 1;
            AttacksDone++;
            ClearTelegraphs();
            _marks.Clear();
            // Enchaînements lisibles (phases 2 et 3) : balayage → prise là où le joueur a reculé ; charge → cri à l'arrivée ; bond → balayage.
            if (Phase >= 2 && _combo == Attack.Aucune && _rng.NextDouble() < (Phase >= 3 ? 0.55 : 0.35))
                _combo = a == Attack.Balayage ? Attack.Prise : a == Attack.Charge ? Attack.Cri : a == Attack.Bond ? Attack.Balayage : Attack.Aucune;
            Vector3 tp = Flat(target.transform.position) + Vector3.up * ArenaCenter.y;
            switch (a)
            {
                case Attack.Prise:
                    TelegraphLeft = Telegraph(settings.grabTelegraph);
                    _marks.Add(tp);
                    Disc(tp, 4.5f, TelegraphLeft);
                    Play("Traqueur_Prise", 2.37f / (TelegraphLeft + 0.2f));
                    GameAudio.PlayAt(GameAudio.Settings?.stalkerGrowl, transform.position + Vector3.up * 10f, 1f, 80f);
                    break;
                case Attack.Balayage:
                    TelegraphLeft = Telegraph(settings.sweepTelegraph);
                    Cone(transform.position, transform.forward, settings.sweepRange, 90f, TelegraphLeft);
                    Play("Traqueur_Prise", 1.6f);
                    GameAudio.PlayAt(GameAudio.Settings?.stalkerGrowl, transform.position + Vector3.up * 10f, 1f, 80f);
                    break;
                case Attack.Charge:
                    TelegraphLeft = Telegraph(settings.chargeTelegraph);
                    _chargeDir = Flat(tp - transform.position).normalized;
                    if (_chargeDir.sqrMagnitude < 0.5f)
                        _chargeDir = Flat(transform.forward).normalized;
                    // Couloir marqué = trajet exact : coupé au bord de l'arène (avant, la marque débordait et le boss glissait le long du bord).
                    _chargeFrom = Flat(transform.position) + Vector3.up * ArenaCenter.y;
                    _chargeLength = ChargeLength(_chargeFrom, _chargeDir, ArenaCenter, ArenaRadius - 6.5f, settings.chargeDistance);
                    _chargeAlong = 0f;
                    Lane(transform.position, _chargeDir, _chargeLength, ChargeWidth, TelegraphLeft);
                    if (_lane != null)
                        _lane.Prolonge(_chargeLength / Mathf.Max(1f, settings.chargeSpeed) + 0.1f);   // la marque reste pleine pendant la course
                    Play("Traqueur_Repos");
                    GameAudio.PlayAt(GameAudio.Pick(GameAudio.Settings?.stalkerEnragedCries), transform.position + Vector3.up * 10f, 1f, 120f);
                    break;
                case Attack.Bond:
                    TelegraphLeft = Telegraph(settings.leapTelegraph);
                    _leapFrom = transform.position;
                    _leapTo = tp;
                    Disc(tp, settings.leapRadius, TelegraphLeft, 0.9f);
                    Play("Traqueur_Bond", 2.05f / TelegraphLeft);
                    break;
                case Attack.Cri:
                    TelegraphLeft = Telegraph(settings.cryTelegraph);
                    Disc(Flat(transform.position) + Vector3.up * ArenaCenter.y, settings.cryRadius, TelegraphLeft);
                    Play("Traqueur_Repos");
                    break;
                case Attack.Debris:
                    TelegraphLeft = Telegraph(settings.debrisTelegraph);
                    foreach (PlayerHealth p in PlayerHealth.All)
                        if (p != null && !p.Dead)
                            for (int k = 0; k < settings.debrisCount; k++)
                            {
                                Vector3 m = Flat(p.transform.position) + Vector3.up * ArenaCenter.y + new Vector3((float)_rng.NextDouble() - 0.5f, 0f, (float)_rng.NextDouble() - 0.5f) * 9f;
                                _marks.Add(m);
                                Disc(m, 2.6f, TelegraphLeft);
                            }

                    Play("Traqueur_Prise", 1.2f);
                    break;
                case Attack.Eruptions:
                    TelegraphLeft = Telegraph(settings.eruptionTelegraph);
                    foreach (PlayerHealth p in PlayerHealth.All)
                        if (p != null && !p.Dead)
                            for (int k = 0; k < settings.eruptions; k++)
                            {
                                Vector3 m = k == 0 ? Flat(p.transform.position) + Vector3.up * ArenaCenter.y
                                    : Flat(p.transform.position) + Vector3.up * ArenaCenter.y + Quaternion.Euler(0f, k * 360f / settings.eruptions, 0f) * Vector3.forward * 8f;
                                _marks.Add(m);
                                Disc(m, settings.eruptionRadius, TelegraphLeft);
                            }

                    Play("Traqueur_Repos");
                    break;
                case Attack.Invocation:
                    TelegraphLeft = Telegraph(settings.summonTelegraph);
                    for (int k = 0; k < settings.summonCount; k++)
                    {
                        Vector3 m = ArenaCenter + Quaternion.Euler(0f, k * 360f / settings.summonCount + (float)_rng.NextDouble() * 40f, 0f) * Vector3.forward * (ArenaRadius - 10f);
                        _marks.Add(m);
                        _marquages.Add(ItHunts.Art.VFX.MarquageBoss.Apparition(m, 2f, TelegraphLeft));
                    }

                    Play("Traqueur_Repos");
                    GameAudio.PlayAt(GameAudio.Pick(GameAudio.Settings?.stalkerEnragedCries), transform.position + Vector3.up * 10f, 1f, 150f);
                    break;
            }
        }

        private void RunAttack(PlayerHealth target, float now)
        {
            float t = now - _stateStart;
            switch (_step)
            {
                case Step.Annonce:
                    TelegraphLeft = Mathf.Max(0f, TelegraphLeft - Time.deltaTime);
                    // Le boss suit sa cible du regard, et le marquage du balayage tourne avec lui (enfant du boss) ; dans les 300 derniers ms,
                    // plus rien ne bouge : ce qu'on voit au sol est exactement ce qui frappe.
                    if (TelegraphLeft > FreezeBefore && Current == Attack.Balayage)
                    {
                        Face(target.transform.position - transform.position, settings.turnRate * 0.6f);
                        if (_cone != null)
                            _cone.Oriente(transform.forward);
                    }
                    else if (Current == Attack.Charge)
                        Face(_chargeDir, settings.turnRate * 2f);            // il se tourne dans l'axe du couloir (la course, elle, ne dévie pas)
                    if (TelegraphLeft > 0f)
                        return;
                    _step = Step.Action;
                    _stateStart = now;
                    Resolve(target);
                    return;
                case Step.Action:
                    if (Current == Attack.Charge)
                    {
                        // Il avance sur le couloir marqué, jamais en dehors (ClampToLane), et s'arrête net sur un obstacle (ChargeBlocked).
                        float stepLength = settings.chargeSpeed * Time.deltaTime;
                        bool blocked = ChargeBlocked(stepLength);
                        if (!blocked)
                        {
                            _chargeAlong = Mathf.Min(_chargeAlong + stepLength, _chargeLength);
                            Place(ClampToLane(_chargeFrom + _chargeDir * _chargeAlong, _chargeFrom, _chargeDir, _chargeLength, 0f));
                        }

                        Play("Traqueur_CourseEnragee_A", 2f);
                        foreach (PlayerHealth p in PlayerHealth.All)
                            if (p != null && !p.Dead && Vector3.Distance(Flat(p.transform.position), Flat(transform.position)) < ChargeWidth * 0.5f)
                                Hurt(p, settings.chargeDamage, _chargeDir * 18f + Vector3.up * 6f, "Charge du traqueur");
                        if (!blocked && _chargeAlong < _chargeLength - 0.01f)
                            return;
                        if (blocked)
                        {
                            ChargeStops++;
                            ShakeAll(0.4f, 60f);
                            GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.bossSlam : null, transform.position + _chargeDir * 3f, 1f, 80f);
                            Play("Traqueur_Reception", 1.4f);
                        }
                    }
                    else if (Current == Attack.Bond)
                    {
                        float u = Mathf.Clamp01(t / 0.9f);
                        Vector3 p = Vector3.Lerp(_leapFrom, _leapTo, u) + Vector3.up * (Mathf.Sin(u * Mathf.PI) * 12f);
                        transform.position = p;
                        if (u < 1f)
                            return;
                        transform.position = new Vector3(_leapTo.x, ArenaCenter.y, _leapTo.z);
                        Shockwave(_leapTo, settings.leapRadius, settings.leapDamage, 14f, "Bond du traqueur");
                        GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.bossBite : null, _leapTo, 1f, 80f);
                        Play("Traqueur_Reception", 1.5f);
                    }

                    _step = Step.Recuperation;
                    _stateStart = now;
                    if (_lane != null)
                        _lane.Annule();                                   // la course s'est vraiment arrêtée
                    _lane = null;
                    return;
                case Step.Recuperation:
                    if (t < (Phase >= 3 ? 0.6f : 0.9f))
                        return;
                    Current = Attack.Aucune;
                    if (_combo != Attack.Aucune)
                    {
                        // Enchaînement (Dark Souls, nuit 4) : la suite part tout de suite, annonce un peu plus courte mais jamais sous le plancher.
                        Attack next = _combo;
                        _combo = Attack.Aucune;
                        StartAttack(next, target, now);
                        TelegraphLeft = Mathf.Max(settings.minTelegraph, TelegraphLeft * 0.85f);
                        return;
                    }

                    _nextAttack = now + settings.cooldownByPhase[Mathf.Clamp(Phase - 1, 0, 2)];
                    return;
            }
        }

        private void Resolve(PlayerHealth target)
        {
            switch (Current)
            {
                case Attack.Prise:
                    foreach (Vector3 m in _marks)
                        Shockwave(m, 4.5f, settings.grabDamage, 10f, "Prise du traqueur");
                    GameAudio.PlayAt(GameAudio.Settings != null ? GameAudio.Settings.bossSlam : null, _marks.Count > 0 ? _marks[0] : transform.position, 1f, 80f);
                    ShakeAll(0.45f, 40f);
                    break;
                case Attack.Balayage:
                    foreach (PlayerHealth p in PlayerHealth.All)
                    {
                        if (p == null || p.Dead)
                            continue;
                        Vector3 to = Flat(p.transform.position - transform.position);
                        if (to.magnitude <= settings.sweepRange && Vector3.Angle(transform.forward, to) <= 45f && p.transform.position.y < ArenaCenter.y + 7f)
                            Hurt(p, settings.sweepDamage, Vector3.Cross(Vector3.up, transform.forward) * 16f + Vector3.up * 5f, "Balayage du traqueur");
                    }

                    ShakeAll(0.3f, 40f);
                    break;
                case Attack.Cri:
                    GameAudio.PlayAt(GameAudio.Pick(GameAudio.Settings?.stalkerEnragedCries), transform.position + Vector3.up * 10f, 1f, 150f);
                    foreach (PlayerHealth p in PlayerHealth.All)
                    {
                        if (p == null || p.Dead)
                            continue;
                        Vector3 to = Flat(p.transform.position - transform.position);
                        if (to.magnitude > settings.cryRadius)
                            continue;
                        p.Motor?.AddVelocity(to.normalized * settings.cryPush + Vector3.up * 8f);
                    }

                    ShakeAll(0.7f, 60f);
                    break;
                case Attack.Debris:
                    foreach (Vector3 m in _marks)
                        Shockwave(m, 2.6f, settings.debrisDamage, 6f, "Débris du traqueur");
                    break;
                case Attack.Eruptions:
                    foreach (Vector3 m in _marks)
                        Shockwave(m, settings.eruptionRadius, settings.eruptionDamage, 12f, "Éruption sous les pieds");
                    ShakeAll(0.4f, 80f);
                    break;
                case Attack.Invocation:
                    if (GhoulPrefab != null)
                        foreach (Vector3 m in _marks)
                            Instantiate(GhoulPrefab, m + Vector3.up * 0.1f, Quaternion.LookRotation(Flat(ArenaCenter - m)), transform.parent).name = "Goule invoquée";
                    break;
            }
        }

        private void Shockwave(Vector3 at, float radius, float damage, float push, string cause)
        {
            var vfx = GameVfx.Settings;
            if (vfx != null)
                GameVfx.Spawn(vfx.bossGroundImpact != null ? vfx.bossGroundImpact : vfx.stalkerLandingDust != null ? vfx.stalkerLandingDust : vfx.impactHeavy, at,
                    Quaternion.identity, Mathf.Max(1f, radius / 4f), null, 3f);
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                Vector3 to = Flat(p.transform.position - at);
                if (to.magnitude > radius || p.transform.position.y > at.y + 4f)
                    continue;                                            // en hauteur (pilier, statue) : épargné
                Hurt(p, damage, (to.sqrMagnitude > 0.01f ? to.normalized : Vector3.forward) * push + Vector3.up * 5f, cause);
            }
        }

        private static void Hurt(PlayerHealth p, float damage, Vector3 push, string cause)
        {
            if (p.TakeEnemyDamage(damage, p.Center, Time.time, cause))
                p.Motor?.AddVelocity(push);
        }

        private static void ShakeAll(float trauma, float range)
        {
            foreach (CameraShake s in FindObjectsByType<CameraShake>(FindObjectsSortMode.None))
                s.AddTrauma(trauma);
        }

        // ---------- Charge : couloir marqué, arrêt net (jour 5, point 5) ----------

        private Vector3 _chargeFrom;
        private float _chargeLength, _chargeAlong;
        private static readonly RaycastHit[] ChargeHits = new RaycastHit[16];

        /// <summary>Charges arrêtées net par un obstacle (debug, mesures).</summary>
        public int ChargeStops { get; private set; }

        /// <summary>
        /// Longueur du couloir de charge (logique pure, à plat) : maxLength au plus, coupée là où la droite quitte le cercle de rayon
        /// limitRadius autour du centre ; 0 si le départ est déjà dehors ou si la direction est nulle.
        /// </summary>
        public static float ChargeLength(Vector3 from, Vector3 dir, Vector3 center, float limitRadius, float maxLength)
        {
            var d = new Vector3(dir.x, 0f, dir.z);
            if (d.sqrMagnitude < 1e-6f || maxLength <= 0f || limitRadius <= 0f)
                return 0f;
            d.Normalize();
            var off = new Vector3(from.x - center.x, 0f, from.z - center.z);
            float b = Vector3.Dot(off, d), c = off.sqrMagnitude - limitRadius * limitRadius;
            if (c > 0f)
                return 0f;                                               // déjà au-delà du bord
            float exit = -b + Mathf.Sqrt(Mathf.Max(0f, b * b - c));
            return Mathf.Clamp(exit, 0f, maxLength);
        }

        /// <summary>
        /// Position bornée au couloir marqué (logique pure, à plat ; la hauteur de p est gardée) : de 0 à length le long de dir depuis
        /// from, à au plus halfWidth de son axe.
        /// </summary>
        public static Vector3 ClampToLane(Vector3 p, Vector3 from, Vector3 dir, float length, float halfWidth)
        {
            var d = new Vector3(dir.x, 0f, dir.z);
            if (d.sqrMagnitude < 1e-6f)
                return new Vector3(from.x, p.y, from.z);
            d.Normalize();
            var side = new Vector3(d.z, 0f, -d.x);
            var off = new Vector3(p.x - from.x, 0f, p.z - from.z);
            float along = Mathf.Clamp(Vector3.Dot(off, d), 0f, Mathf.Max(0f, length));
            float lateral = Mathf.Clamp(Vector3.Dot(off, side), -Mathf.Max(0f, halfWidth), Mathf.Max(0f, halfWidth));
            Vector3 q = from + d * along + side * lateral;
            return new Vector3(q.x, p.y, q.z);
        }

        /// <summary>Un décor (pilier, statue, mur) devant lui sur ce pas de charge : sphère lancée à hauteur de poitrine.</summary>
        private bool ChargeBlocked(float stepLength)
        {
            float radius = settings.chargeStopRadius;
            Vector3 origin = Flat(transform.position) + Vector3.up * (ArenaCenter.y + Mathf.Max(settings.chargeStopHeight, radius + 0.3f));
            int n = Physics.SphereCastNonAlloc(origin, radius, _chargeDir, ChargeHits, stepLength + 0.3f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = ChargeHits[i].collider;
                if (c == null || c.transform.IsChildOf(transform) || c.attachedRigidbody != null || c is CharacterController)
                    continue;                                            // lui-même, caisses, joueurs : pas des obstacles
                if (c.GetComponentInParent<Damageable>() != null || c.GetComponentInParent<PlayerHealth>() != null)
                    continue;                                            // goules invoquées, joueurs
                if (ChargeHits[i].distance > 0f && ChargeHits[i].normal.y > 0.6f)
                    continue;                                            // une pente ou une marche, pas un mur
                return true;
            }

            return false;
        }

        /// <summary>Pose le boss (sol de l'arène), Rigidbody compris.</summary>
        private void Place(Vector3 p)
        {
            p.y = ArenaCenter.y;
            transform.position = p;
            if (_rb != null)
                _rb.position = p;
        }

        /// <summary>Largeur de la marque et des dégâts de la charge (m) : alignées (PR #50 : la marque faisait 7 m, les dégâts 10 m).</summary>
        public const float ChargeWidth = 10f;
        private readonly List<ItHunts.Art.VFX.MarquageBoss> _marquages = new List<ItHunts.Art.VFX.MarquageBoss>();
        private ItHunts.Art.VFX.MarquageBoss _cone, _lane;

        // Marquages de la DA (PR #50, MarquageBoss) : bord net = zone exacte des dégâts, montée d'intensité, éclair à l'impact, puis la marque
        // s'éteint seule. Réglage d'accessibilité : « Réduire les animations ».
        private void Disc(Vector3 at, float radius, float duration, float hold = 0f)
        {
            ItHunts.Art.VFX.MarquageBoss.ReduireEffets = ItHunts.Level.GameSettings.ReduceMotion;
            var m = ItHunts.Art.VFX.MarquageBoss.Cercle(at, radius, duration);
            if (m != null && hold > 0f)
                m.Prolonge(hold);
            _marquages.Add(m);
        }

        private void Lane(Vector3 from, Vector3 dir, float length, float width, float duration)
        {
            _lane = ItHunts.Art.VFX.MarquageBoss.Couloir(Flat(from) + Vector3.up * ArenaCenter.y, dir, length, width, duration);
            _marquages.Add(_lane);
        }

        private void Cone(Vector3 from, Vector3 dir, float range, float angle, float duration)
        {
            _cone = ItHunts.Art.VFX.MarquageBoss.Cone(Flat(from) + Vector3.up * ArenaCenter.y, Flat(dir).normalized, range, angle * 0.5f, duration);
            _marquages.Add(_cone);
        }

        /// <summary>Les marquages et la visée se figent ce temps avant l'impact (recherche nuit 4 : snapshot FFXIV, 300 ms).</summary>
        public const float FreezeBefore = 0.3f;
        /// <summary>Enchaînement prévu après l'attaque en cours (Dark Souls : balayage puis prise, charge puis cri) ; Aucune sinon.</summary>
        private Attack _combo = Attack.Aucune;
        public Attack PendingCombo => _combo;

        private void ClearTelegraphs()
        {
            // Attaque interrompue (étourdi, mort, nouvelle attaque) : les marques encore affichées sont annulées.
            foreach (ItHunts.Art.VFX.MarquageBoss m in _marquages)
                if (m != null)
                    m.Annule();
            _marquages.Clear();
            _cone = _lane = null;
        }
    }
}
