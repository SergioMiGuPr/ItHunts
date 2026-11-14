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

        /// <summary>Traqueur : jamais blessé (aucun dégât), repoussé, étourdi, blessé par une zone ni attrapé.</summary>
        public bool ImmuneToOrbs => immuneToOrbs;

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
        private float ScaledMaxHealth => (settings != null ? settings.maxHealth : 100f) * (ImmuneToOrbs ? 1f : ItHunts.Level.RunSession.EnemyHealth);

        private void OnEnable()
        {
            if (!AllList.Contains(this))
                AllList.Add(this);
        }

        private void OnDisable() => AllList.Remove(this);

        /// <summary>Dernier joueur qui l'a blessé, lancé ou attrapé (coop, D-041 : il se retourne contre lui).</summary>
        public int LastAttackerId { get; private set; }
        public float LastAttackTime { get; private set; } = float.NegativeInfinity;

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
            float taken = HealthState.Apply(ref _health, damage, time, out killed);
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
            SetAlive(true);
        }

        /// <summary>Éclair d'émission sur la cible (coup reçu).</summary>
        public void Flash(Color color, float duration)
        {
            _flashColor = color;
            _flashUntil = Time.time + duration;
        }

        private void Update()
        {
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
            Color glow = Time.time < _flashUntil ? _flashColor
                : Stunned ? stunColor * (stunGlowIntensity * (0.6f + 0.4f * Mathf.Sin(Time.time * 18f)))
                : Color.black;
            _block.SetColor(EmissionId, glow);
            foreach (Renderer r in _renderers)
                if (r != null)
                    r.SetPropertyBlock(_block);
        }

        private void SetAlive(bool alive)
        {
            foreach (Collider c in _colliders)
                if (c != null)
                    c.enabled = alive;
            foreach (Renderer r in _renderers)
                if (r != null)
                    r.enabled = alive;
        }
    }
}
