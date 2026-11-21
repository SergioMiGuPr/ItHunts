using ItHunts.Combat;
using ItHunts.Movement;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    public enum BookLink : byte { Aucun, Drain, Soin }

    /// <summary>État du livre à lien d'un joueur : données seulement, copiables (réseau). Délais en ticks.</summary>
    public struct BookState
    {
        public BookLink Link;
        public int StartTick;
        /// <summary>Fin du dernier lien et recharge (en ticks) qui en découle.</summary>
        public int EndTick;
        public int CooldownTicks;
        public int LastConsumedPressTick;
        /// <summary>PV retirés et rendus pendant le lien en cours.</summary>
        public float Drained, Returned, Healed;
        public int Links;

        public static BookState Create() => new BookState
        {
            StartTick = MovementState.NeverTick, EndTick = MovementState.NeverTick, LastConsumedPressTick = MovementState.NeverTick,
        };
    }

    public enum BookEnd : byte { Aucune, Relache, Portee, Vue, Duree, CibleMorte, Plafond }

    /// <summary>
    /// Le livre à lien du hibou (D-077), logique pure et testable : départ (cible trouvée par le monde), maintien tant que le bouton
    /// est tenu, la cible valide et la durée maximale pas atteinte ; effets par tick (drain : dégâts et part rendue ; soin : PV
    /// rendus sans dépasser le maximum, plafonds par lien) ; recharge selon le type de lien. Le traqueur n'est jamais une cible.
    /// </summary>
    public static class SpiritBook
    {
        public static bool Ready(in BookState s, int tick) =>
            s.Link == BookLink.Aucun && (s.EndTick == MovementState.NeverTick || tick - s.EndTick >= s.CooldownTicks);

        /// <summary>Appui frais (dans la mémoire d'appui) ?</summary>
        public static bool Fresh(in BookState s, int tick, int pressTick, int bufferTicks) =>
            pressTick > 0 && pressTick > s.LastConsumedPressTick && tick - pressTick >= 0 && tick - pressTick <= bufferTicks;

        public static void Start(ref BookState s, BookLink link, int tick, int pressTick)
        {
            s.Link = link;
            s.StartTick = tick;
            s.LastConsumedPressTick = pressTick;
            s.Drained = s.Returned = s.Healed = 0f;
            s.Links++;
        }

        public static void End(ref BookState s, int tick, SpiritBookSettings cfg, float dt)
        {
            if (s.Link == BookLink.Aucun)
                return;
            float cooldown = s.Link == BookLink.Drain ? cfg.drainCooldown : cfg.healCooldown;
            s.CooldownTicks = MovementSettings.SecondsToTicks(cooldown, dt);
            s.EndTick = tick;
            s.Link = BookLink.Aucun;
        }

        /// <summary>Le lien doit-il casser ce tick ? (bouton relâché, durée, portée, vue, cible morte, plafond)</summary>
        public static BookEnd Check(in BookState s, int tick, bool held, bool targetAlive, float distance, bool inSight, SpiritBookSettings cfg, float dt)
        {
            if (!held)
                return BookEnd.Relache;
            if (!targetAlive)
                return BookEnd.CibleMorte;
            if (tick - s.StartTick >= MovementSettings.SecondsToTicks(cfg.maxDuration, dt))
                return BookEnd.Duree;
            if (distance > cfg.range)
                return BookEnd.Portee;
            if (!inSight)
                return BookEnd.Vue;
            if (s.Link == BookLink.Drain && s.Drained >= cfg.maxDrainPerLink)
                return BookEnd.Plafond;
            if (s.Link == BookLink.Soin && s.Healed >= cfg.maxHealPerLink)
                return BookEnd.Plafond;
            return BookEnd.Aucune;
        }

        /// <summary>PV à retirer ce tick (drain), plafonnés par lien.</summary>
        public static float DrainTick(in BookState s, SpiritBookSettings cfg, float dt) =>
            Mathf.Max(0f, Mathf.Min(cfg.drainDps * dt, cfg.maxDrainPerLink - s.Drained));

        /// <summary>PV à rendre ce tick (soin) : jamais au-delà du maximum de l'allié ni du plafond du lien.</summary>
        public static float HealTick(in BookState s, float allyMissing, SpiritBookSettings cfg, float dt) =>
            Mathf.Max(0f, Mathf.Min(cfg.healDps * dt, Mathf.Min(allyMissing, cfg.maxHealPerLink - s.Healed)));

        /// <summary>Une cible pour le livre (le traqueur et les morts exclus) : ennemi = drain, allié = soin.</summary>
        public static bool Targetable(Damageable enemy) => enemy != null && !enemy.Dead && !enemy.ImmuneToOrbs;

        private static readonly Collider[] Overlaps = new Collider[96];
        private static readonly RaycastHit[] Rays = new RaycastHit[16];

        /// <summary>
        /// Cible visée : ennemi ou allié vivant à portée, dans le cône de visée, en vue ; le plus proche du centre du viseur.
        /// </summary>
        public static BookLink Find(Vector3 eye, Vector3 aim, SpiritBookSettings cfg, Transform self, out Damageable enemy, out PlayerHealth ally)
        {
            enemy = null;
            ally = null;
            float best = float.PositiveInfinity;
            int n = Physics.OverlapSphereNonAlloc(eye, cfg.range, Overlaps, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                Collider c = Overlaps[i];
                if (c == null || (self != null && c.transform.IsChildOf(self)))
                    continue;
                var e = c.GetComponentInParent<Damageable>();
                PlayerHealth p = e == null ? c.GetComponentInParent<PlayerHealth>() : null;
                if (e == null && (p == null || p.Dead || p.transform == self))
                    continue;
                if (e != null && !Targetable(e))
                    continue;
                Vector3 point = c.bounds.center;
                float angle = Vector3.Angle(aim, point - eye);
                if (angle > cfg.aimHalfAngle || angle >= best || !InSight(eye, point, c.transform, self, cfg))
                    continue;
                best = angle;
                enemy = e;
                ally = p;
            }

            return enemy != null ? BookLink.Drain : ally != null ? BookLink.Soin : BookLink.Aucun;
        }

        /// <summary>Ligne de vue entre les yeux et la cible (le décor coupe, pas les objets physiques ni le joueur).</summary>
        public static bool InSight(Vector3 eye, Vector3 point, Transform target, Transform self, SpiritBookSettings cfg)
        {
            Vector3 d = point - eye;
            float len = d.magnitude;
            if (len < 0.1f)
                return true;
            int n = Physics.RaycastNonAlloc(eye, d / len, Rays, len - 0.1f, cfg.sightMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = Rays[i].collider;
                if (c == null || c.attachedRigidbody != null || (self != null && c.transform.IsChildOf(self))
                    || (target != null && (c.transform.IsChildOf(target.root) || target.IsChildOf(c.transform))))
                    continue;
                if (c.GetComponentInParent<Damageable>() != null || c.GetComponentInParent<PlayerHealth>() != null)
                    continue;
                return false;
            }

            return true;
        }
    }
}
