using ItHunts.Orbs;
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
    /// Les joueurs ne sont jamais blessés (ils ne sont pas des Damageable).
    /// Réseau (étape 11) : ce calcul se fera là où la physique fait autorité (serveur).
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public sealed class ImpactDamager : MonoBehaviour
    {
        [SerializeField] private DamageSettings settings;

        private static readonly HitCooldown Cooldown = new HitCooldown();
        private static readonly RaycastHit[] LosHits = new RaycastHit[16];
        private Grabbable _grabbable;

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
                _grabbable.InitDurability(AreaEffects.DurabilityForMass(_grabbable.Mass, settings, _grabbable.DurabilityOverride));
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
            HandleImpact(_grabbable, target, collision.relativeVelocity, point, settings, Time.time);
        }

        /// <summary>
        /// Un impact de l'objet (logique partagée avec les tests) : coup direct sur la cible touchée, puis, au premier
        /// impact d'un lancer (ou à tout impact pour un objet fragile), dégâts de zone et durabilité.
        /// </summary>
        public static void HandleImpact(Grabbable g, Damageable target, Vector3 relativeVelocity, Vector3 point,
            DamageSettings cfg, float time)
        {
            float speed = relativeVelocity.magnitude;
            bool falling = ImpactDamage.IsFall(relativeVelocity, g.transform.position, point, cfg);
            if (target != null)
                TryDamage(target, g.Id, g.Mass, speed, point, cfg, time, out _, falling);

            bool thrownImpact = g.TakeThrowImpact() && speed >= cfg.speedThreshold;
            bool fragileImpact = g.BreaksOnAnyImpact && !g.Held && speed >= cfg.fallSpeedThreshold;
            if (!thrownImpact && !fragileImpact)
                return;

            float impactDamage = ImpactDamage.Compute(g.Mass, speed, cfg, falling || !thrownImpact);
            float radius = AreaEffects.SplashRadius(g.Mass, cfg, g.SplashRadiusOverride);
            ApplyArea(g, target, point, radius, impactDamage, speed, cfg, time);

            bool breaks = fragileImpact || g.ConsumeDurability();
            if (breaks)
                g.Break();
        }

        /// <summary>
        /// Dégâts de zone autour du point : chaque cible vivante dont le collider est dans le rayon (sauf celle déjà
        /// touchée directement), si rien ne la cache, prend des dégâts décroissants vers le bord. Publie la zone.
        /// </summary>
        public static void ApplyArea(Grabbable source, Damageable directTarget, Vector3 point, float radius, float impactDamage,
            float speed, DamageSettings cfg, float time)
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
                if (d == null || d == directTarget || d.Dead)
                    continue;
                Collider col = d.MainCollider;
                Vector3 closest = col != null ? col.ClosestPoint(point) : d.transform.position;
                float distance = Vector3.Distance(point, closest);
                float damage = AreaEffects.SplashDamage(impactDamage, distance, radius, cfg);
                if (damage <= 0f || !InSight(source, d, point))
                    continue;
                ApplyRaw(d, source != null ? source.Id : 0, damage, speed, source != null ? source.Mass : 0f, closest, false, true,
                    cfg, time, out _);
            }
        }

        /// <summary>Rien de solide entre le point d'impact et la cible (hors l'objet lui-même).</summary>
        private static bool InSight(Grabbable source, Damageable target, Vector3 point)
        {
            Vector3 to = target.Center - point;
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
                if (c.GetComponentInParent<Damageable>() == target)
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
        /// Applique un impact à une cible (logique partagée avec les tests) : calcule les dégâts, respecte l'intervalle
        /// par cible, retire les PV et publie le coup (DamageEvents). Vrai si des dégâts ont été pris.
        /// </summary>
        public static bool TryDamage(Damageable target, int sourceId, float mass, float impactSpeed, Vector3 point,
            DamageSettings cfg, float time, out DamageEvent e, bool falling = false)
        {
            float damage = ImpactDamage.Compute(mass, impactSpeed, cfg, falling);
            return ApplyRaw(target, sourceId, damage, impactSpeed, mass, point, falling, false, cfg, time, out e);
        }

        private static bool ApplyRaw(Damageable target, int sourceId, float damage, float impactSpeed, float mass, Vector3 point,
            bool falling, bool area, DamageSettings cfg, float time, out DamageEvent e)
        {
            e = default;
            if (target == null || target.Dead)
                return false;
            if (damage <= 0f || !Cooldown.TryHit(sourceId, target.Id, time, cfg.perTargetCooldown))
                return false;
            float taken = target.ApplyDamage(damage, time, out bool killed);
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
