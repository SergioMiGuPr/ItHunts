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

        public int PlayerId => gameObject.GetInstanceID();
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
            _health = HealthState.Create(settings != null ? settings.maxHealth : 100f);
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
            if (damage <= 0f || _health.Dead || !FriendlyFire.Allows(attackerId, PlayerId, coop))
                return false;
            float taken = HealthState.Apply(ref _health, FriendlyFire.Scale(damage, coop), time, out bool killed);
            if (taken <= 0f)
                return false;
            Hurt?.Invoke(new PlayerHurtEvent
            {
                Victim = this, AttackerId = attackerId, Damage = taken, Point = point, Killed = killed, Time = time,
            });
            return true;
        }

        private void Update()
        {
            if (!_health.Dead || !HealthState.TryRespawn(ref _health, Time.time, settings != null ? settings.respawnDelay : 1f))
                return;
            if (_motor != null)
                _motor.Respawn();
        }
    }
}
