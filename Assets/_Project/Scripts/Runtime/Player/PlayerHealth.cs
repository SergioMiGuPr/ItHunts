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
        /// <summary>Ennemi qui a frappé (goule, volant), s'il s'est nommé ; null sinon (Fourrure de braise, D-100).</summary>
        public Component Attacker;
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
        /// <summary>Cause du dernier coup reçu (écran de fin : « cause de la mort »).</summary>
        public string LastCause { get; private set; } = "";
        /// <summary>Invulnérable jusqu'à ce temps (parade réussie, nuit 3 point 7) : les coups des ennemis ne passent pas.</summary>
        public float InvulnerableUntil { get; set; } = float.NegativeInfinity;
        public static int DebugBlockedKills { get; set; }
        public static int DebugBlockedHits { get; set; }

        /// <summary>
        /// Remet les drapeaux de mesure à zéro : au chargement du jeu, au lancement d'une run, et à la sortie du Play Mode
        /// (DebugFlagsReset, éditeur). Les variables statiques survivent sinon à la sortie du Play Mode.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetDebugFlags()
        {
            DebugInvulnerable = false;
            DebugBlockedKills = 0;
            DebugBlockedHits = 0;
        }

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
            float scaled = Mitigate(FriendlyFire.Scale(damage, coop));
            LastHurtTime = Time.time;
            if (scaled <= 0f || TryCheatDeath(scaled))
                return false;
            float taken = HealthState.Apply(ref _health, scaled, time, out bool killed);
            if (taken <= 0f)
                return false;
            Raise(attackerId, taken, point, killed, time);
            return true;
        }

        // ---------- Crochets des améliorations (nuit 4, D-100), posés par Economy.PlayerUpgrades ----------

        /// <summary>Dégâts reçus × ce facteur (Peau épaisse, Fourrure de braise Or) ; null : 1.</summary>
        public System.Func<float> IncomingScale { get; set; }
        /// <summary>Dégâts du décor (chute dans le vide) → dégâts réels (Chute amortie, Plumes) ; null : inchangés.</summary>
        public System.Func<float, float> FallDamageScale { get; set; }
        /// <summary>Un coup qui tuerait : vrai pour revenir à 50 % de PV à la place (Cœur solide Mythique, une fois par run) ; jamais face à la prise du traqueur.</summary>
        public System.Func<bool> SecondChance { get; set; }
        /// <summary>Bouclier (Sangsue Or) : absorbe les dégâts avant les PV, jusqu'à ShieldMax ; se recharge par le drain du livre.</summary>
        public float Shield { get; private set; }
        public float ShieldMax { get; set; }
        /// <summary>Sangsue Mythique dans l'arène du boss : le bouclier absorbe un coup mortel du traqueur.</summary>
        public bool ShieldBlocksKill { get; set; }
        /// <summary>Peau épaisse Or : pas de recul quand on est touché (bond de la goule).</summary>
        public bool KnockbackImmune { get; set; }
        /// <summary>Dernier coup reçu (régénération hors combat).</summary>
        public float LastHurtTime { get; private set; } = float.NegativeInfinity;

        /// <summary>Ajoute du bouclier (jamais au-delà de ShieldMax) ; renvoie la part gardée.</summary>
        public float AddShield(float amount)
        {
            if (amount <= 0f || ShieldMax <= 0f)
                return 0f;
            float before = Shield;
            Shield = Mathf.Min(ShieldMax, Shield + amount);
            return Shield - before;
        }

        /// <summary>Dégâts après réduction et bouclier.</summary>
        private float Mitigate(float damage)
        {
            if (IncomingScale != null)
                damage *= Mathf.Clamp01(IncomingScale());
            if (Shield > 0f && damage > 0f)
            {
                float absorbed = Mathf.Min(Shield, damage);
                Shield -= absorbed;
                damage -= absorbed;
            }

            return damage;
        }

        /// <summary>Seconde chance (Cœur solide Mythique) : si ce coup tue et qu'elle est due, le joueur revient à 50 % ; vrai alors.</summary>
        private bool TryCheatDeath(float damage)
        {
            if (damage < _health.Current || SecondChance == null || !SecondChance())
                return false;
            _health.Current = Mathf.Max(1f, _health.Max * 0.5f);
            return true;
        }

        /// <summary>Coup d'un ennemi (goule, volant) : pas de tir ami, il blesse toujours. Vrai si des PV ont été retirés.</summary>
        public bool TakeEnemyDamage(float damage, Vector3 point, float time, string cause = null, Component attacker = null)
        {
            if (damage <= 0f || _health.Dead)
                return false;
            if (Time.time < InvulnerableUntil)
                return false;
            if (!string.IsNullOrEmpty(cause))
                LastCause = cause;
            if (DebugInvulnerable)
            {
                DebugBlockedHits++;
                return false;
            }
            damage *= ItHunts.Level.RunSession.EnemyDamage * ItHunts.Enemies.EnemyScaling.DamageMultiplier();   // difficulté (D-048) et scaling (nuit 4)
            damage = Mitigate(damage);                                      // améliorations (D-100) : réduction, bouclier
            LastHurtTime = Time.time;
            if (damage <= 0f || TryCheatDeath(damage))
                return false;
            float taken = HealthState.Apply(ref _health, damage, time, out bool killed);
            if (taken <= 0f)
                return false;
            Raise(EnemyAttacker, taken, point, killed, time, attacker);
            return true;
        }

        /// <summary>Dégâts du décor (chute dans un canal, D-052) : pas de multiplicateur de difficulté. Vrai si des PV ont été retirés.</summary>
        public bool TakeEnvironmentDamage(float damage, Vector3 point, float time, string cause = null)
        {
            if (damage <= 0f || _health.Dead)
                return false;
            if (!string.IsNullOrEmpty(cause))
                LastCause = cause;
            if (FallDamageScale != null)
                damage = FallDamageScale(damage);                          // Chute amortie, Plumes (D-100)
            if (damage <= 0f || TryCheatDeath(damage))
                return false;
            float taken = HealthState.Apply(ref _health, damage, time, out bool killed);
            if (taken <= 0f)
                return false;
            Raise(EnemyAttacker, taken, point, killed, time);
            return true;
        }

        /// <summary>Tué net, quels que soient ses PV (coup du traqueur). Vrai s'il était vivant.</summary>
        public bool Kill(Vector3 point, float time, string cause = null)
        {
            if (!string.IsNullOrEmpty(cause) && !_health.Dead && !DebugInvulnerable)
                LastCause = cause;
            if (DebugInvulnerable && !_health.Dead)
            {
                DebugBlockedKills++;                                    // mesures : le coup aurait tué
                return false;
            }

            if (_health.Dead)
                return false;
            if (ShieldBlocksKill && Shield > 0f)
            {
                Shield = 0f;                                             // Sangsue Mythique : le bouclier encaisse le coup du boss
                InvulnerableUntil = Mathf.Max(InvulnerableUntil, Time.time + 1f);
                return false;
            }

            float taken = _health.Current;
            HealthState.Apply(ref _health, float.PositiveInfinity, time, out bool killed);
            Raise(EnemyAttacker, taken, point, killed, time);
            if (killed && cause == "Le traqueur")
                StalkerDeathCam.Begin(this);                             // screamer de la prise (nuit 4, point 7)
            return true;
        }

        /// <summary>Tous les PV, vivant (remise à zéro de la zone).</summary>
        public void Restore()
        {
            if (Downed || Spectating)
                CoopLife.RaiseChanged(PlayerId, Spectating ? LifeState.Spectateur : LifeState.ATerre, LifeState.Vivant);
            Downed = Spectating = false;
            DownedAt = float.NegativeInfinity;
            _health = HealthState.Create((settings != null ? settings.maxHealth : 100f) + MaxHealthBonus);
        }

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

        /// <summary>À terre (coop, D-007) : il compte comme mort (cibles, vagues, fin de partie) mais un allié peut le relever.</summary>
        public bool Downed { get; private set; }
        /// <summary>Non relevé à temps : il regarde un allié vivant et revient au début de la zone suivante.</summary>
        public bool Spectating { get; private set; }
        public float DownedAt { get; private set; } = float.NegativeInfinity;
        public float DownedLeft => Downed && !Spectating ? Mathf.Max(0f, DownedSeconds - (Time.time - DownedAt)) : 0f;
        private float DownedSeconds => coop != null ? coop.downedSeconds : 30f;

        /// <summary>À terre depuis downedSeconds sans être relevé : spectateur (public pour les tests).</summary>
        public void TickDowned(float now)
        {
            if (Downed && !Spectating && now - DownedAt >= DownedSeconds)
            {
                Spectating = true;
                CoopLife.RaiseChanged(PlayerId, LifeState.ATerre, LifeState.Spectateur);
            }
        }

        /// <summary>Autres joueurs debout (ni morts, ni à terre).</summary>
        public int OthersStanding()
        {
            int n = 0;
            foreach (PlayerHealth p in AllList)
                if (p != null && p != this && !p.Dead)
                    n++;
            return n;
        }

        /// <summary>
        /// Plus aucun allié debout pour le relever (l'autre joueur a quitté l'écran partagé) : à terre devient mort, la zone
        /// décide de la fin.
        /// </summary>
        public void NoAllyLeft()
        {
            if (Downed && OthersStanding() == 0)
                Died?.Invoke(this);
        }

        /// <summary>Relevé par un allié : revient avec hp PV (D-007, étape 9).</summary>
        public bool ReviveFromDown(float hp)
        {
            if (!Downed || Spectating)
                return false;
            Downed = false;
            DownedAt = float.NegativeInfinity;
            _health = HealthState.Create((settings != null ? settings.maxHealth : 100f) + MaxHealthBonus);
            _health.Current = Mathf.Clamp(hp, 1f, _health.Max);
            CoopLife.RaiseChanged(PlayerId, LifeState.ATerre, LifeState.Vivant);
            return true;
        }

        private void Raise(int attacker, float taken, Vector3 point, bool killed, float time, Component by = null)
        {
            if (killed && !Downed && OthersStanding() > 0)
            {
                // Coop : à terre au lieu de mort (un allié peut le relever) ; seul : mort.
                Downed = true;
                DownedAt = time;
                CoopLife.RaiseChanged(PlayerId, LifeState.Vivant, LifeState.ATerre);
            }

            Hurt?.Invoke(new PlayerHurtEvent
            {
                Victim = this, AttackerId = attacker, Damage = taken, Point = point, Killed = killed, Time = time, Attacker = by,
            });
            if (killed)
                Died?.Invoke(this);
        }

        private void Update()
        {
            TickDowned(Time.time);

            if (!_health.Dead || ZoneHandlesDeath
                || !HealthState.TryRespawn(ref _health, Time.time, settings != null ? settings.respawnDelay : 1f))
                return;
            if (_motor != null)
                _motor.Respawn();
        }
    }
}
