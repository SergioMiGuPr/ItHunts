using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Sur un objet attrapable : tout impact sur une cible (Damageable) au-dessus du seuil la blesse (D-025) : objet
    /// lancé, rebond, chute, objet tenu. Dégâts = masse × (vitesse d'impact − seuil) × facteur, vitesse d'impact = vitesse
    /// relative au contact ; seuil plus bas pour une chute. Un même objet ne blesse la même cible qu'une fois par
    /// perTargetCooldown.
    /// Premier impact d'un lancer au-dessus du seuil (D-027) : dégâts de zone autour du point (rayon selon la masse,
    /// décroissants vers le bord, jamais à travers un mur), et un lancer de durabilité en moins : à 0, l'objet se
    /// casse. Un objet qui se casse à tout impact (lustre) le fait aussi en tombant, au-dessus du seuil de chute.
    /// Les joueurs ne sont pas des Damageable : un objet ne blesse un joueur que par le tir ami (D-032), attribué au dernier
    /// joueur qui l'a attrapé, lancé ou repoussé (jamais ce joueur lui-même, les autres seulement si le réglage coop le
    /// permet) ; mêmes dégâts que sur un ennemi (×0.5 s'il est repoussé), zone comprise pour un lancer.
    /// Réseau (étape 11) : ce calcul se fera là où la physique fait autorité (serveur).
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public sealed class ImpactDamager : MonoBehaviour
    {
        [SerializeField] private DamageSettings settings;

        private static readonly HitCooldown Cooldown = new HitCooldown();
        private static readonly RaycastHit[] LosHits = new RaycastHit[16];
        private Grabbable _grabbable;
        private Vector3 _lastVelocity;

        /// <summary>Distance (m) en dessous de laquelle un objet dangereux touche un joueur (contact détecté à la main).</summary>
        public const float PlayerContactDistance = 0.12f;

        public DamageSettings Settings => settings;

        /// <summary>Oublie les coups récents (tests, changement de scène).</summary>
        public static void ResetCooldowns() => Cooldown.Clear();

        // Start, pas Awake : la masse du Rigidbody doit être lue après l'initialisation de Grabbable (ordre des Awake libre).
        private void Start() => Init();

        /// <summary>Prépare la durabilité (Start ; aussi par les tests, sans cycle de vie en mode édition).</summary>
        public void Init()
        {
            _grabbable = GetComponent<Grabbable>();
            if (settings != null && _grabbable != null)
                _grabbable.PushMinSpeed = settings.pushedMinSpeed;
            if (settings != null && _grabbable != null && !_grabbable.IsEnemy)
                _grabbable.InitDurability(AreaEffects.DurabilityForMass(_grabbable.Mass, settings, _grabbable.DurabilityOverride));
        }

        /// <summary>
        /// Tir ami (D-032) : Unity n'envoie pas OnCollisionEnter à un objet qui percute un CharacterController (mesuré en
        /// Play Mode : une caisse repoussée sur un joueur ne le blessait jamais). À chaque pas, un objet dangereux qui a un
        /// propriétaire teste donc son contact avec les autres joueurs ; vitesse d'impact = sa vitesse au pas précédent
        /// (avant que le contact ne la freine) moins celle du joueur. L'intervalle par cible évite les coups répétés.
        /// </summary>
        private void FixedUpdate()
        {
            if (_grabbable == null || _grabbable.Body == null)
                return;
            Vector3 velocity = _grabbable.Body.isKinematic ? Vector3.zero : _grabbable.Body.linearVelocity;
            if (settings != null && !_grabbable.Broken && _grabbable.LastOwnerId != 0 && PlayerHealth.All.Count > 0 && Dangerous(_grabbable, settings))
                CheckPlayerContacts();
            _lastVelocity = velocity;
        }

        private static bool Dangerous(Grabbable g, DamageSettings cfg, float? time = null) =>
            g.ThrowArmed || g.Held || (g.Pushed && g.PushDangerous(time ?? Time.time, cfg.pushedDangerTime))
            || (g.EnemyBody != null && g.EnemyBody.InFlight);

        private void CheckPlayerContacts()
        {
            Collider own = _grabbable.Colliders != null && _grabbable.Colliders.Length > 0 ? _grabbable.Colliders[0] : null;
            if (own == null || !own.enabled)
                return;
            Bounds near = own.bounds;
            near.Expand(PlayerContactDistance * 2f);
            for (int i = 0; i < PlayerHealth.All.Count; i++)
            {
                PlayerHealth p = PlayerHealth.All[i];
                if (p == null || p.Collider == null || !FriendlyFire.Allows(_grabbable.LastOwnerId, p.PlayerId, p.Coop))
                    continue;
                if (!near.Intersects(p.Collider.bounds))
                    continue;
                Vector3 onPlayer = p.Collider.ClosestPoint(own.bounds.center);
                Vector3 onObject = own.ClosestPoint(onPlayer);
                if ((onPlayer - onObject).sqrMagnitude > PlayerContactDistance * PlayerContactDistance)
                    continue;
                var cc = p.Collider as CharacterController;
                Vector3 relative = _lastVelocity - (cc != null ? cc.velocity : Vector3.zero);
                HandleImpact(_grabbable, null, relative, onPlayer, settings, Time.time, p);
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (settings == null)
                return;
            if (_grabbable == null)
                Init();
            if (_grabbable.Broken)
                return;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Damageable target = collision.collider != null ? collision.collider.GetComponentInParent<Damageable>() : null;
            PlayerHealth player = target == null && collision.collider != null ? collision.collider.GetComponentInParent<PlayerHealth>() : null;
            HandleImpact(_grabbable, target, collision.relativeVelocity, point, settings, Time.time, player);
        }

        /// <summary>
        /// Un impact de l'objet (logique partagée avec les tests) : coup direct sur la cible touchée, puis, au premier
        /// impact d'un lancer (ou à tout impact pour un objet fragile), dégâts de zone et durabilité.
        /// </summary>
        public static void HandleImpact(Grabbable g, Damageable target, Vector3 relativeVelocity, Vector3 point,
            DamageSettings cfg, float time, PlayerHealth player = null)
        {
            // Un ennemi n'est un projectile que tenu ou en vol (lancé, lâché) : au repos ou repoussé, il ne blesse pas ce
            // qui le percute (c'est l'objet qui arrive qui blesse).
            if (g.IsEnemy && (g.EnemyBody == null || !(g.EnemyBody.InFlight || g.Held)))
                return;
            float speed = relativeVelocity.magnitude;
            bool falling = ImpactDamage.IsFall(relativeVelocity, g.transform.position, point, cfg);
            if (player != null)
                HurtPlayer(g, player, speed, falling, point, cfg, time);

            // Objet repoussé par la répulsion (D-031) : il blesse ce qu'il percute comme un objet lancé, × multiplicateur,
            // sans zone ; il perd un lancer de durabilité seulement s'il blesse un ennemi. Dangereux pushedDangerTime après
            // la poussée ou jusqu'à passer sous pushedMinSpeed ; ensuite, il ne blesse plus (sauf une vraie chute).
            if (!g.IsEnemy && g.Pushed && !g.ThrowArmed)
            {
                if (target != null && g.PushDangerous(time, cfg.pushedDangerTime))
                {
                    float damage = ImpactDamage.Compute(g.Mass, speed, cfg, falling) * cfg.pushedDamageMultiplier;
                    if (ApplyRaw(target, g.Id, damage, speed, g.Mass, point, falling, false, cfg, time, out _, lethal: true) && g.ConsumeDurability())
                        g.Break();
                    return;
                }

                if (!falling)
                    return;
            }

            if (target != null)
                TryDamage(target, g.Id, g.Mass, speed, point, cfg, time, out _, falling);

            // Ennemi lancé (D-030) : il subit lui aussi les dégâts de chaque impact pendant son vol, mur compris.
            if (g.IsEnemy && g.Damageable != null && g.EnemyBody != null && g.EnemyBody.InFlight)
                ApplyRaw(g.Damageable, ~g.Id, ImpactDamage.Compute(g.Mass, speed, cfg, falling), speed, g.Mass, point, falling,
                    false, cfg, time, out _);   // pas mortel d'un coup : un volant lancé éclate (Flyer), il ne meurt pas avant

            bool thrownImpact = g.TakeThrowImpact() && speed >= cfg.speedThreshold;
            bool fragileImpact = g.BreaksOnAnyImpact && !g.Held && speed >= cfg.fallSpeedThreshold;
            if (!thrownImpact && !fragileImpact)
                return;

            float impactDamage = ImpactDamage.Compute(g.Mass, speed, cfg, falling || !thrownImpact);
            float radius = AreaEffects.SplashRadius(g.Mass, cfg, g.SplashRadiusOverride);
            ApplyArea(g, target, point, radius, impactDamage, speed, cfg, time, player);

            if (g.IsEnemy)
                return;                                                  // pas de durabilité pour un ennemi
            bool breaks = fragileImpact || g.ConsumeDurability();
            if (breaks)
                g.Break();
        }

        /// <summary>
        /// Tir ami (D-032) : un objet qui percute un joueur le blesse comme un ennemi s'il est dangereux (lancé, tenu, ennemi
        /// en vol, repoussé depuis peu ×0.5, ou qui tombe), au nom du dernier joueur qui l'a manipulé. FriendlyFire décide.
        /// </summary>
        public static bool HurtPlayer(Grabbable g, PlayerHealth player, float speed, bool falling, Vector3 point, DamageSettings cfg,
            float time)
        {
            if (player == null || g.LastOwnerId == 0)
                return false;
            float damage;
            if (!g.IsEnemy && g.Pushed && !g.ThrowArmed)
                damage = g.PushDangerous(time, cfg.pushedDangerTime) || falling
                    ? ImpactDamage.Compute(g.Mass, speed, cfg, falling) * (falling ? 1f : cfg.pushedDamageMultiplier)
                    : 0f;
            else if (g.ThrowArmed || g.Held || falling || (g.EnemyBody != null && g.EnemyBody.InFlight))
                damage = ImpactDamage.Compute(g.Mass, speed, cfg, falling);
            else
                damage = 0f;
            if (damage <= 0f || !FriendlyFire.Allows(g.LastOwnerId, player.PlayerId, player.Coop)
                || !Cooldown.TryHit(g.Id, player.PlayerId, time, cfg.perTargetCooldown))
                return false;
            return player.TryHurt(g.LastOwnerId, damage, point, time);
        }

        /// <summary>
        /// Dégâts de zone autour du point : chaque cible vivante dont le collider est dans le rayon (sauf celle déjà
        /// touchée directement), si rien ne la cache, prend des dégâts décroissants vers le bord. Publie la zone.
        /// </summary>
        public static void ApplyArea(Grabbable source, Damageable directTarget, Vector3 point, float radius, float impactDamage,
            float speed, DamageSettings cfg, float time, PlayerHealth directPlayer = null)
        {
            AreaEvents.RaiseArea(new AreaImpactEvent { Point = point, Radius = radius, ImpactDamage = impactDamage });
            if (impactDamage <= 0f)
                return;
            // Copie : une cible tuée peut quitter le registre pendant la boucle.
            Damageable[] targets = new Damageable[Damageable.All.Count];
            for (int i = 0; i < targets.Length; i++)
                targets[i] = Damageable.All[i];
            foreach (Damageable d in targets)
            {
                if (d == null || d == directTarget || d.Dead || d.ImmuneToOrbs)
                    continue;
                if (source != null && d.gameObject == source.gameObject)
                    continue;                                            // l'ennemi lancé ne se blesse pas par sa propre zone
                Collider col = d.MainCollider;
                Vector3 closest = col != null ? col.ClosestPoint(point) : d.transform.position;
                float distance = Vector3.Distance(point, closest);
                float damage = AreaEffects.SplashDamage(impactDamage, distance, radius, cfg);
                if (damage <= 0f || !InSight(source, d, point))
                    continue;
                ApplyRaw(d, source != null ? source.Id : 0, damage, speed, source != null ? source.Mass : 0f, closest, false, true,
                    cfg, time, out _);
            }

            // Tir ami (D-032) : la zone d'un objet lancé touche aussi les autres joueurs, si le réglage coop le permet.
            if (source == null || source.LastOwnerId == 0)
                return;
            PlayerHealth[] players = new PlayerHealth[PlayerHealth.All.Count];
            for (int i = 0; i < players.Length; i++)
                players[i] = PlayerHealth.All[i];
            foreach (PlayerHealth p in players)
            {
                if (p == null || p == directPlayer || !FriendlyFire.Allows(source.LastOwnerId, p.PlayerId, p.Coop))
                    continue;
                Vector3 closest = p.Collider != null ? p.Collider.ClosestPoint(point) : p.Center;
                float damage = AreaEffects.SplashDamage(impactDamage, Vector3.Distance(point, closest), radius, cfg);
                if (damage <= 0f || !InSight(source, p.transform, p.Center, point))
                    continue;
                p.TryHurt(source.LastOwnerId, damage, closest, time);
            }
        }

        /// <summary>Rien de solide entre le point d'impact et la cible (hors l'objet lui-même).</summary>
        private static bool InSight(Grabbable source, Damageable target, Vector3 point) =>
            InSight(source, target.transform, target.Center, point);

        /// <summary>Rien de solide entre le point et le centre de la cible (hors la source et la cible) : zones de dégâts.</summary>
        public static bool InSight(Grabbable source, Transform targetRoot, Vector3 targetCenter, Vector3 point)
        {
            Vector3 to = targetCenter - point;
            float length = to.magnitude;
            if (length < 1e-3f)
                return true;
            int count = Physics.RaycastNonAlloc(point, to / length, LosHits, length, ~0, QueryTriggerInteraction.Ignore);
            float nearest = float.PositiveInfinity;
            bool blocked = false;
            for (int i = 0; i < count; i++)
            {
                Collider c = LosHits[i].collider;
                if (source != null && c.attachedRigidbody == source.Body)
                    continue;
                if (c.transform.IsChildOf(targetRoot))
                    continue;
                if (LosHits[i].distance < nearest && LosHits[i].distance > 0.05f)
                {
                    nearest = LosHits[i].distance;
                    blocked = true;
                }
            }

            return !blocked;
        }

        /// <summary>
        /// Dégâts publiés sans calcul d'impact (mur, D-031) : retire les PV, respecte l'intervalle par cible, publie le
        /// coup. Les paliers viennent des réglages de la cible (son ImpactDamager), sinon « petit ».
        /// </summary>
        public static bool ApplyDirect(Damageable target, int sourceId, float damage, float speed, float mass, Vector3 point,
            float time, bool lethal = true)
        {
            if (target == null || damage <= 0f)
                return false;
            var damager = target.GetComponent<ImpactDamager>();
            if (damager != null && damager.Settings != null)
                return ApplyRaw(target, sourceId, damage, speed, mass, point, false, !lethal, damager.Settings, time, out _, lethal);
            float taken = target.ApplyDamage(lethal ? target.ProjectileDamage(damage) : damage, time, out bool killed);
            if (taken <= 0f)
                return false;
            DamageEvents.Raise(new DamageEvent
            {
                Target = target, Damage = taken, ImpactSpeed = speed, SourceMass = mass, Point = point, Killed = killed,
                Tier = HitTier.Petit, Time = time,
            });
            return true;
        }

        /// <summary>
        /// Applique un impact à une cible (logique partagée avec les tests) : calcule les dégâts, respecte l'intervalle
        /// par cible, retire les PV et publie le coup (DamageEvents). Vrai si des dégâts ont été pris.
        /// </summary>
        public static bool TryDamage(Damageable target, int sourceId, float mass, float impactSpeed, Vector3 point,
            DamageSettings cfg, float time, out DamageEvent e, bool falling = false)
        {
            float damage = ImpactDamage.Compute(mass, impactSpeed, cfg, falling);
            return ApplyRaw(target, sourceId, damage, impactSpeed, mass, point, falling, false, cfg, time, out e, lethal: true);
        }

        private static bool ApplyRaw(Damageable target, int sourceId, float damage, float impactSpeed, float mass, Vector3 point,
            bool falling, bool area, DamageSettings cfg, float time, out DamageEvent e, bool lethal = false)
        {
            e = default;
            if (target == null || target.Dead)
                return false;
            if (damage <= 0f || !Cooldown.TryHit(sourceId, target.Id, time, cfg.perTargetCooldown))
                return false;
            // Impact de projectile (pas une zone) : une cible fragile (volant, D-040) meurt d'un coup.
            float taken = target.ApplyDamage(lethal && !area ? target.ProjectileDamage(damage) : damage, time, out bool killed);
            if (taken <= 0f)
                return false;
            e = new DamageEvent
            {
                Target = target,
                Damage = taken,
                ImpactSpeed = impactSpeed,
                SourceMass = mass,
                Point = point,
                Killed = killed,
                Falling = falling,
                Area = area,
                Tier = ImpactDamage.Tier(damage, cfg),
                Time = time,
            };
            DamageEvents.Raise(e);
            return true;
        }
    }
}
