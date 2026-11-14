using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>Un joueur blessé (tir ami) : HUD de la victime (flash, PV) et chiffres de dégâts chez les autres.</summary>
    public struct PlayerHurtEvent
    {
        public PlayerHealth Victim;
        public int AttackerId;
        public float Damage;
        public Vector3 Point;
        public bool Killed;
        public float Time;
    }

    /// <summary>
    /// Points de vie d'un joueur (provisoire, D-032), sur son prefab : servent au tir ami. Ce n'est pas un Damageable
    /// (registre des cibles du monde) : les ennemis, la répulsion et la zone des objets ne le traitent pas comme une cible.
    /// Toute blessure passe par TryHurt, qui applique la règle du tir ami (jamais soi-même, les alliés seulement si le
    /// réglage coop l'autorise). À 0 PV : réapparition au point de départ (la vraie mort viendra à l'étape 8).
    /// Identifiant du joueur : l'identifiant d'instance de son objet racine (le même que celui noté sur les objets qu'il
    /// lance ou repousse).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerHealthSettings settings;
        [SerializeField] private CoopSettings coop;

        private static readonly List<PlayerHealth> AllList = new List<PlayerHealth>();
        private HealthState _health;
        private PlayerMotor _motor;
        private CharacterController _controller;

        public static IReadOnlyList<PlayerHealth> All => AllList;
        public static event System.Action<PlayerHurtEvent> Hurt;

        /// <summary>Un joueur vient de mourir (la zone le remet à zéro, sinon il réapparaît seul).</summary>
        public static event System.Action<PlayerHealth> Died;

        /// <summary>Identifiant d'attaquant des ennemis (pas un joueur : le tir ami ne s'applique pas).</summary>
        public const int EnemyAttacker = -1;

        /// <summary>Quand vrai, la zone gère la mort (remise à zéro) : pas de réapparition seule.</summary>
        public static bool ZoneHandlesDeath { get; set; }

        /// <summary>Mesures seulement (scripts de l'éditeur) : aucun dégât ; les coups qui auraient tué sont comptés.</summary>
        public static bool DebugInvulnerable { get; set; }
        public static int DebugBlockedKills { get; set; }
        public static int DebugBlockedHits { get; set; }

        public bool Dead => _health.Dead;
        public PlayerMotor Motor => _motor;

        public int PlayerId => gameObject.GetInstanceID();

        /// <summary>Joueur de cet identifiant (null s'il n'existe plus).</summary>
        public static PlayerHealth Find(int playerId)
        {
            if (playerId == 0)
                return null;
            foreach (PlayerHealth p in All)
                if (p != null && p.PlayerId == playerId)
                    return p;
            return null;
        }
        public HealthState Health => _health;
        public CoopSettings Coop => coop;
        public Collider Collider => _controller;
        public Vector3 Center => _controller != null ? _controller.bounds.center : transform.position + Vector3.up * 0.9f;

        /// <summary>Identifiant de joueur d'un objet (sa racine), 0 si ce n'est pas un joueur.</summary>
        public static int IdOf(Transform owner)
        {
            if (owner == null)
                return 0;
            var h = owner.GetComponentInParent<PlayerHealth>();
            return h != null ? h.PlayerId : 0;
        }

        private void Awake() => Init();

        /// <summary>Prépare les PV (Awake ; aussi par les tests).</summary>
        public void Init()
        {
            _motor = GetComponent<PlayerMotor>();
            _controller = GetComponent<CharacterController>();
            _health = HealthState.Create((settings != null ? settings.maxHealth : 100f) + MaxHealthBonus);
        }

        /// <summary>Réglages (tests).</summary>
        public void Configure(PlayerHealthSettings health, CoopSettings coopSettings)
        {
            settings = health;
            coop = coopSettings;
            Init();
        }

        private void OnEnable()
        {
            if (!AllList.Contains(this))
                AllList.Add(this);
        }

        private void OnDisable() => AllList.Remove(this);

        /// <summary>Blessure venue d'un joueur (balle, objet lancé ou repoussé) : vrai si des PV ont été retirés.</summary>
        public bool TryHurt(int attackerId, float damage, Vector3 point, float time)
        {
            if (damage <= 0f || _health.Dead || DebugInvulnerable || !FriendlyFire.Allows(attackerId, PlayerId, coop))
                return false;
            float taken = HealthState.Apply(ref _health, FriendlyFire.Scale(damage, coop), time, out bool killed);
            if (taken <= 0f)
                return false;
            Raise(attackerId, taken, point, killed, time);
            return true;
        }

        /// <summary>Coup d'un ennemi (goule, volant) : pas de tir ami, il blesse toujours. Vrai si des PV ont été retirés.</summary>
        public bool TakeEnemyDamage(float damage, Vector3 point, float time)
        {
            if (damage <= 0f || _health.Dead)
                return false;
            if (DebugInvulnerable)
            {
                DebugBlockedHits++;
                return false;
            }
            damage *= ItHunts.Level.RunSession.EnemyDamage;                 // difficulté (D-048)
            float taken = HealthState.Apply(ref _health, damage, time, out bool killed);
            if (taken <= 0f)
                return false;
            Raise(EnemyAttacker, taken, point, killed, time);
            return true;
        }

        /// <summary>Dégâts du décor (chute dans un canal, D-052) : pas de multiplicateur de difficulté. Vrai si des PV ont été retirés.</summary>
        public bool TakeEnvironmentDamage(float damage, Vector3 point, float time)
        {
            if (damage <= 0f || _health.Dead)
                return false;
            float taken = HealthState.Apply(ref _health, damage, time, out bool killed);
            if (taken <= 0f)
                return false;
            Raise(EnemyAttacker, taken, point, killed, time);
            return true;
        }

        /// <summary>Tué net, quels que soient ses PV (coup du traqueur). Vrai s'il était vivant.</summary>
        public bool Kill(Vector3 point, float time)
        {
            if (DebugInvulnerable && !_health.Dead)
            {
                DebugBlockedKills++;                                    // mesures : le coup aurait tué
                return false;
            }

            if (_health.Dead)
                return false;
            float taken = _health.Current;
            HealthState.Apply(ref _health, float.PositiveInfinity, time, out bool killed);
            Raise(EnemyAttacker, taken, point, killed, time);
            return true;
        }

        /// <summary>Tous les PV, vivant (remise à zéro de la zone).</summary>
        public void Restore() => _health = HealthState.Create((settings != null ? settings.maxHealth : 100f) + MaxHealthBonus);

        /// <summary>Amélioration « PV max » (D-043) : bonus de PV max, gagné aussi tout de suite.</summary>
        public float MaxHealthBonus { get; private set; }

        public void SetMaxHealthBonus(float bonus)
        {
            float delta = bonus - MaxHealthBonus;
            MaxHealthBonus = bonus;
            _health.Max += delta;
            if (!_health.Dead)
                _health.Current = Mathf.Clamp(_health.Current + delta, 0f, _health.Max);
        }

        /// <summary>Soin (marchand) : rend des PV, jamais au-delà du max.</summary>
        public void Heal(float amount)
        {
            if (_health.Dead || amount <= 0f)
                return;
            _health.Current = Mathf.Min(_health.Max, _health.Current + amount);
        }

        public float CurrentHealth => _health.Current;
        public float MaxHealth => _health.Max;

        private void Raise(int attacker, float taken, Vector3 point, bool killed, float time)
        {
            Hurt?.Invoke(new PlayerHurtEvent
            {
                Victim = this, AttackerId = attacker, Damage = taken, Point = point, Killed = killed, Time = time,
            });
            if (killed)
                Died?.Invoke(this);
        }

        private void Update()
        {
            if (!_health.Dead || ZoneHandlesDeath
                || !HealthState.TryRespawn(ref _health, Time.time, settings != null ? settings.respawnDelay : 1f))
                return;
            if (_motor != null)
                _motor.Respawn();
        }
    }
}
