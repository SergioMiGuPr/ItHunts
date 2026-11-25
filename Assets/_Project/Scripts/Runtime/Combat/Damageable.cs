using System.Collections.Generic;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Cible qui prend des dégâts (ennemi factice pour l'instant ; les vrais ennemis à l'étape 6). Points de vie dans un
    /// HealthState copiable ; à sa mort, elle disparaît (rendus et collisions coupés) et réapparaît après le délai si
    /// respawns est coché. Le magnétisme ne s'accroche jamais à elle (PhysicsAimProbe). Registre des cibles de la scène
    /// (objets du monde, pas des joueurs).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Damageable : MonoBehaviour
    {
        [SerializeField] private EnemySettings settings;
        [Tooltip("Réapparaît après settings.respawnDelay (ennemi factice).")]
        [SerializeField] private bool respawns = true;
        [Tooltip("Traqueur : intuable (aucun dégât), insensible à la répulsion, à l'étourdissement, aux dégâts de zone, aux " +
                 "balles et à la prise (GDD, D-033).")]
        [SerializeField] private bool immuneToOrbs;
        [Tooltip("Couleur et intensité de la lueur pendant l'étourdissement.")]
        [SerializeField] private Color stunColor = new Color(1f, 0.9f, 0.2f);
        [SerializeField, Min(0f)] private float stunGlowIntensity = 1.5f;

        private static readonly List<Damageable> AllList = new List<Damageable>();
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");

        private HealthState _health;
        private Renderer[] _renderers;
        private Collider[] _colliders;
        private MaterialPropertyBlock _block;
        private float _flashUntil = float.NegativeInfinity;
        private Color _flashColor;
        private EnemyBody _body;
        private ItHunts.Orbs.Grabbable _grabbable;

        public static IReadOnlyList<Damageable> All => AllList;

        /// <summary>Une cible meurt (âmes, D-043).</summary>
        public static event System.Action<Damageable> Died;
        public EnemySettings Settings => settings;
        public HealthState Health => _health;
        public bool Dead => _health.Dead;
        public int Id => GetInstanceID();

        /// <summary>Corps (recul, étourdissement) ; null pour une cible fixe.</summary>
        public EnemyBody Body => _body;
        public bool Stunned => _body != null && _body.Stunned;

        // ---------- Les 5 états lisibles par les synergies (nuit 4, D-100) : étourdi, paralysé, en feu, saignant, lié ----------

        /// <summary>Paralysé (balles paralysantes, Mythiques) jusqu'à cet instant : un étourdissement marqué « paralysie ».</summary>
        public float ParalyzedUntil { get; private set; } = float.NegativeInfinity;
        public bool Paralyzed => Time.time < ParalyzedUntil;
        public bool StunnedOrParalyzed => Stunned || Paralyzed;

        /// <summary>Paralyse (étourdit, et marque l'état) pendant duration s ; faux pour le traqueur ou sans corps.</summary>
        public bool Paralyze(float duration)
        {
            if (_body == null || duration <= 0f || !_body.StunFor(duration))
                return false;
            ParalyzedUntil = Mathf.Max(ParalyzedUntil, Time.time + duration);
            return true;
        }

        /// <summary>Prolonge l'étourdissement en cours (Briseur d'os Or, Brise-garde Or).</summary>
        public void ExtendStun(float seconds)
        {
            if (_body != null)
                _body.ExtendStun(seconds);
        }

        private Burning _burning;
        private Bleeding _bleeding;

        /// <summary>En feu (lanterne, braises, Allumette…) ; vrai aussi à l'instant de la mort pour les effets « meurt en feu ».</summary>
        public bool Burning
        {
            get
            {
                if (_burning == null)
                    _burning = GetComponent<Burning>();
                return _burning != null && _burning.Lit;
            }
        }

        public int BurnStacks
        {
            get
            {
                if (_burning == null)
                    _burning = GetComponent<Burning>();
                return _burning != null && _burning.Lit ? _burning.Stacks : 0;
            }
        }

        /// <summary>Saignant (épée en os, Os brisé).</summary>
        public bool Bleeding
        {
            get
            {
                if (_bleeding == null)
                    _bleeding = GetComponent<Bleeding>();
                return _bleeding != null && _bleeding.Lit;
            }
        }

        /// <summary>Lié par le livre du hibou (drain en cours) : l'artefact le renouvelle à chaque tick du lien.</summary>
        public float BoundUntil { get; set; } = float.NegativeInfinity;
        public bool Bound => Time.time < BoundUntil;
        /// <summary>Joueur dont le lien le tient (0 : aucun).</summary>
        public int BoundBy { get; set; }

        /// <summary>Ralenti (Sillage, Onde blessante Or, feu d'Allumette, flaques, liens) : part de vitesse retirée jusqu'à SlowUntil.</summary>
        public float SlowUntil { get; private set; } = float.NegativeInfinity;
        public float SlowShare { get; private set; }
        /// <summary>Facteur de vitesse de déplacement (1 : pas de ralentissement), lu par la locomotion des ennemis.</summary>
        public float SpeedScale => Time.time < SlowUntil ? 1f - Mathf.Clamp01(SlowShare) : 1f;

        public void Slow(float share, float duration)
        {
            if (immuneToOrbs || share <= 0f || duration <= 0f)
                return;                                                 // le traqueur n'est jamais ralenti (D-035)
            float until = Time.time + duration;
            if (until >= SlowUntil || share >= SlowShare)
            {
                SlowShare = Time.time < SlowUntil ? Mathf.Max(SlowShare, share) : share;
                SlowUntil = Mathf.Max(SlowUntil, until);
            }
        }

        /// <summary>Ne peut plus bondir (Lien vorace Mythique) jusqu'à cet instant.</summary>
        public float LeapBlockedUntil { get; set; } = float.NegativeInfinity;
        public bool LeapBlocked => Time.time < LeapBlockedUntil;

        /// <summary>Contour « Vision nocturne » du hibou (H-P1) : lueur douce quand aucune autre lueur d'état ne l'emporte.</summary>
        public Color VisionGlow { get; set; } = Color.black;

        /// <summary>Traqueur : jamais blessé (aucun dégât), repoussé, étourdi, blessé par une zone ni attrapé.</summary>
        public bool ImmuneToOrbs => immuneToOrbs;

        /// <summary>
        /// Cible qui encaisse les coups portés à celle-ci (boss final, point 5 du jour 5) : le traqueur reste intouchable (ImmuneToOrbs),
        /// mais pendant le combat de boss chaque coup sur un de ses colliders va à ce relais, qui transmet les dégâts au boss. null sinon.
        /// </summary>
        public Damageable HitProxy { get; set; }

        /// <summary>
        /// Ne meurt jamais : les dégâts sont comptés (renvoyés et publiés) sans toucher aux PV (relais du boss, points faibles). Ni
        /// barre de PV, ni âmes, ni mort comptée.
        /// </summary>
        public bool Unkillable { get; set; }

        /// <summary>La cible touchée par ce collider : son Damageable, ou le relais de celui-ci (HitProxy) ; null sans cible.</summary>
        public static Damageable Of(Collider c)
        {
            if (c == null)
                return null;
            var d = c.GetComponentInParent<Damageable>();
            return d != null && d.HitProxy != null ? d.HitProxy : d;
        }

        /// <summary>Réapparaît seul après sa mort (factices) ; les ennemis d'une zone reviennent à la remise à zéro.</summary>
        public bool Respawns => respawns;

        public Collider MainCollider => _colliders != null && _colliders.Length > 0 ? _colliders[0] : null;

        public Vector3 Center => MainCollider != null ? MainCollider.bounds.center : transform.position + Vector3.up;

        /// <summary>Haut de la cible (barre de PV, chiffres de dégâts).</summary>
        public Vector3 Top
        {
            get
            {
                if (_colliders == null || _colliders.Length == 0 || _colliders[0] == null)
                    return transform.position + Vector3.up * 2f;
                Bounds b = _colliders[0].bounds;
                return new Vector3(b.center.x, b.max.y, b.center.z);
            }
        }

        private void Awake() => Init();

        /// <summary>Prépare la cible (appelé par Awake ; aussi par les tests, sans cycle de vie en mode édition).</summary>
        public void Init()
        {
            _renderers = GetComponentsInChildren<Renderer>();
            _colliders = GetComponentsInChildren<Collider>();
            _block = new MaterialPropertyBlock();
            _body = GetComponent<EnemyBody>();
            _grabbable = GetComponent<ItHunts.Orbs.Grabbable>();
            _health = HealthState.Create(ScaledMaxHealth);
        }

        /// <summary>PV max × multiplicateur de la difficulté (D-048) ; pas pour le traqueur (ImmuneToOrbs).</summary>
        private float ScaledMaxHealth => (settings != null ? settings.maxHealth : 100f)
                                          * (ImmuneToOrbs ? 1f : ItHunts.Level.RunSession.EnemyHealth * ItHunts.Enemies.EnemyScaling.HealthMultiplier());   // puissance du joueur et acte (nuit 4)

        private void OnEnable()
        {
            if (!AllList.Contains(this))
                AllList.Add(this);
        }

        private void OnDisable() => AllList.Remove(this);

        /// <summary>Dernier joueur qui l'a blessé, lancé ou attrapé (coop, D-041 : il se retourne contre lui).</summary>
        public int LastAttackerId { get; private set; }
        public float LastAttackTime { get; private set; } = float.NegativeInfinity;
        /// <summary>Dernière fois qu'il a perdu des PV (barre de PV affichée un instant seulement, nuit 3).</summary>
        public float LastDamagedAt { get; private set; } = float.NegativeInfinity;

        public void NoteAttacker(int playerId)
        {
            if (playerId == 0)
                return;
            LastAttackerId = playerId;
            LastAttackTime = Time.time;
        }

        /// <summary>Meurt au premier impact de projectile (volant, D-040) : réglage de son asset.</summary>
        public bool DiesOnProjectileHit => !immuneToOrbs && settings != null && settings.DiesOnProjectileHit;

        /// <summary>
        /// Dégâts d'un impact de projectile (balle, objet lancé ou repoussé, ennemi lancé, mur) : tous ses PV si la cible meurt au
        /// premier impact, sinon les dégâts donnés. Jamais pour une zone (l'appelant ne l'utilise pas).
        /// </summary>
        public float ProjectileDamage(float damage)
        {
            if (_renderers == null)
                Init();
            return damage > 0f && DiesOnProjectileHit ? Mathf.Max(damage, _health.Current) : damage;
        }

        /// <summary>Retire des PV ; renvoie les dégâts réellement pris.</summary>
        public float ApplyDamage(float damage, float time, out bool killed)
        {
            if (_renderers == null)
                Init();
            killed = false;
            if (immuneToOrbs)
                return 0f;                                              // le traqueur est intuable
            if (Unkillable)
                return Mathf.Max(0f, damage);                           // relais du boss : compté, jamais retiré
            float taken = HealthState.Apply(ref _health, damage, time, out killed);
            if (taken > 0f)
                LastDamagedAt = Time.time;
            if (killed)
            {
                SetAlive(false);
                Died?.Invoke(this);
            }
            return taken;
        }

        /// <summary>Remise à zéro de la zone : tous les PV, présent (rendus et collisions).</summary>
        public void Revive()
        {
            if (_renderers == null)
                Init();
            _health = HealthState.Create(ScaledMaxHealth);
            if (_body != null)
                _body.ResetToHome();
            ParalyzedUntil = SlowUntil = BoundUntil = LeapBlockedUntil = float.NegativeInfinity;
            SetAlive(true);
        }

        /// <summary>Éclair d'émission sur la cible (coup reçu).</summary>
        public void Flash(Color color, float duration)
        {
            _flashColor = color;
            _flashUntil = Time.time + duration;
        }

        /// <summary>Lueur d'un état de l'ennemi (annonce du bond de la goule, bloc 4) : émission quand il n'y a ni flash ni étourdissement.</summary>
        public Color ExtraGlow { get; set; } = Color.black;

        /// <summary>
        /// Mort animée (goule de la DA) : à la mort, les collisions s'éteignent tout de suite, les rendus après ce délai (s) ; 0 :
        /// tout disparaît d'un coup.
        /// </summary>
        public float HideDelay { get; set; }
        private float _hideAt;

        /// <summary>
        /// Lueur propre à certains rendus (mains du traqueur : griffes ivoire, alors que ses pupilles sont rouges) ; remplace
        /// ExtraGlow pour eux. Le coup reçu et l'étourdissement l'emportent toujours.
        /// </summary>
        public readonly Dictionary<Renderer, Color> GlowOverrides = new Dictionary<Renderer, Color>();

        /// <summary>
        /// Lueur d'un matériau d'un rendu (traqueur gréé : un seul rendu, pupilles sur M_Traqueur, griffes sur M_Traqueur_Mains) :
        /// pour ces rendus, l'émission est écrite matériau par matériau ; ExtraGlow pour les matériaux sans entrée.
        /// </summary>
        public readonly Dictionary<(Renderer, int), Color> MaterialGlows = new Dictionary<(Renderer, int), Color>();
        private readonly HashSet<Renderer> _perMaterial = new HashSet<Renderer>();
        private MaterialPropertyBlock _matBlock;

        public void SetMaterialGlow(Renderer r, int index, Color c)
        {
            MaterialGlows[(r, index)] = c;
            _perMaterial.Add(r);
        }

        private void Update()
        {
            if (_hideAt > 0f && Time.time >= _hideAt)
            {
                _hideAt = 0f;
                if (_health.Dead && _renderers != null)
                    foreach (Renderer r in _renderers)
                        if (r != null)
                            r.enabled = false;
            }

            if (_health.Dead && respawns && HealthState.TryRespawn(ref _health, Time.time, settings != null ? settings.respawnDelay : 3f))
            {
                if (_body != null)
                    _body.ResetToHome();
                SetAlive(true);
            }

            if (_renderers == null)
                return;
            if (_grabbable != null && _grabbable.Held)
                return;                                                  // tenu : sa lueur vient de la télékinésie
            bool whole = Time.time < _flashUntil || Stunned;
            Color glow = Time.time < _flashUntil ? _flashColor
                : Stunned ? (Paralyzed ? new Color(0.5f, 0.9f, 1f) : stunColor) * (stunGlowIntensity * (0.6f + 0.4f * Mathf.Sin(Time.time * 18f)))
                : ExtraGlow.maxColorComponent > 0f ? ExtraGlow
                : VisionGlow;
            foreach (Renderer r in _renderers)
                if (r != null && _perMaterial.Contains(r))
                {
                    if (_matBlock == null)
                        _matBlock = new MaterialPropertyBlock();
                    Material[] mats = r.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++)
                    {
                        r.GetPropertyBlock(_matBlock, i);
                        Color c = !whole && MaterialGlows.TryGetValue((r, i), out Color mg) ? mg : glow;
                        _matBlock.SetColor(EmissionId, c);
                        if (mats[i] != null && mats[i].HasProperty(EmissionMapId))
                        {
                            Texture own = mats[i].GetTexture(EmissionMapId);
                            _matBlock.SetTexture(EmissionMapId, whole || own == null ? Texture2D.whiteTexture : own);
                        }

                        r.SetPropertyBlock(_matBlock, i);
                    }
                }
                else if (r != null)
                {
                    r.GetPropertyBlock(_block);                           // garde les autres propriétés (lueur du bond, D-bloc 4)
                    _block.SetColor(EmissionId, !whole && GlowOverrides.TryGetValue(r, out Color ownGlow) ? ownGlow : glow);
                    // Modèles à masque d'émission (goule de la DA) : le coup reçu et l'étourdissement allument tout le corps, la
                    // lueur d'un état (ExtraGlow : yeux et gueule pendant l'annonce) seulement le masque.
                    Material m = r.sharedMaterial;
                    if (m != null && m.HasProperty(EmissionMapId))
                    {
                        Texture own = m.GetTexture(EmissionMapId);
                        _block.SetTexture(EmissionMapId, whole || own == null ? Texture2D.whiteTexture : own);
                    }

                    r.SetPropertyBlock(_block);
                }
        }

        private void SetAlive(bool alive)
        {
            foreach (Collider c in _colliders)
                if (c != null)
                    c.enabled = alive;
            _hideAt = !alive && HideDelay > 0f ? Time.time + HideDelay : 0f;
            if (!alive && HideDelay > 0f)
                return;                                                   // le corps reste visible le temps de sa mort
            foreach (Renderer r in _renderers)
                if (r != null)
                    r.enabled = alive;
        }
    }
}
