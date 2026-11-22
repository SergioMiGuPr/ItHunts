using System;
using ItHunts.Combat;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    public enum BarrelHitKind : byte
    {
        Rien,          // rien dans la portée
        Surface,       // mur, sol, objet
        Ennemi,        // blessé
        Insensible,    // le traqueur ignore les balles
        Joueur,        // un autre joueur (blessé seulement avec le tir ami)
    }

    /// <summary>Une balle tirée (retour visuel, debug, mesures).</summary>
    public struct BarrelShot
    {
        public int Tick;
        public Vector3 Origin;
        public Vector3 Direction;
        public Vector3 Point;
        public Vector3 Normal;
        public float Distance;
        public BarrelHitKind Kind;
        public float Damage;
        public bool Killed;
        public Collider Collider;
        public int ShooterId;
        /// <summary>Headshot (nuit 3) ; critique (tête ou chance) ; ennemis touchés (perforation, ricochets) ; balles remises (kill).</summary>
        public bool Headshot, Critical;
        public int EnemiesHit, RoundsRefill;
    }

    /// <summary>
    /// Tir instantané du barillet (D-032) : rayon depuis les yeux du joueur au moment du clic, exactement dans la direction
    /// du viseur, jusqu'à la portée, sans chute. Le premier collider touché (hors le tireur et les déclencheurs) arrête la
    /// balle : un ennemi prend les dégâts (sauf le traqueur), un autre joueur seulement avec le tir ami.
    /// Réseau (étape 11) : le serveur rejouera ce rayon avec compensation de latence (positions des cibles au moment du clic).
    /// </summary>
    public static class BarrelShooter
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>Critiques en chaîne : dernier critique et longueur de la chaîne, par tireur.</summary>
        private static readonly System.Collections.Generic.Dictionary<int, (float time, int chain)> Chains =
            new System.Collections.Generic.Dictionary<int, (float, int)>();
        private static readonly System.Collections.Generic.HashSet<Damageable> HitSet = new System.Collections.Generic.HashSet<Damageable>();
        private static readonly Collider[] Around = new Collider[64];

        /// <summary>Chance de critique hyperbolique (recherche nuit 3, Risk of Rain 2) : 1 − 1 / (1 + k · piles).</summary>
        public static float CritChance(int stacks, float k) => stacks <= 0 ? 0f : 1f - 1f / (1f + k * stacks);

        /// <summary>Multiplicateur d'un critique : headshot + bonus de chaîne (par pile et par critique enchaîné, borné).</summary>
        public static float CritMultiplier(BarrelModsSettings m, int chainStacks, int chain) =>
            (m != null ? m.headshotMultiplier : 2f) + (m != null ? m.chainBonusPerStack : 0.5f) * chainStacks * Mathf.Min(chain, m != null ? m.chainMax : 4);

        public static BarrelShot Fire(Vector3 eye, Vector3 direction, BarrelSettings cfg, Transform shooter, int shooterId,
            float time, int tick, PhysicsScene? scene = null, ItHunts.Economy.PlayerUpgrades up = null)
        {
            Vector3 dir = direction.normalized;
            var shot = new BarrelShot
            {
                Tick = tick, Origin = eye, Direction = dir, Point = eye + dir * cfg.range, Normal = -dir, Distance = cfg.range,
                Kind = BarrelHitKind.Rien, ShooterId = shooterId,
            };
            BarrelModsSettings mods = cfg.mods;
            int pierceLeft = up != null && mods != null ? up.Pierce * mods.piercePerStack : 0;
            float damage = cfg.damage;
            HitSet.Clear();
            PhysicsScene physics = scene ?? Physics.defaultPhysicsScene;
            int count = physics.Raycast(eye, dir, Hits, cfg.range, cfg.hitMask, QueryTriggerInteraction.Collide);
            Array.Sort(Hits, 0, count, Comparer.Instance);
            bool first = true;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = Hits[i];
                if (h.collider == null || (shooter != null && h.collider.transform.IsChildOf(shooter)))
                    continue;
                Hitbox hb = h.collider.isTrigger ? h.collider.GetComponent<Hitbox>() : null;
                if (h.collider.isTrigger && hb == null)
                    continue;                                            // déclencheurs ignorés, sauf les zones de touche (ailes, tête)
                var enemy = h.collider.GetComponentInParent<Damageable>();
                if (enemy != null && HitSet.Contains(enemy))
                    continue;                                            // déjà touché (tête puis corps)
                if (first)
                {
                    shot.Point = h.point;
                    shot.Normal = h.normal;
                    shot.Distance = h.distance;
                    shot.Collider = h.collider;
                    shot.Kind = BarrelHitKind.Surface;
                }

                if (enemy != null && !enemy.Dead)
                {
                    if (enemy.ImmuneToOrbs)
                    {
                        if (first)
                            shot.Kind = BarrelHitKind.Insensible;        // le traqueur ignore les balles
                        break;
                    }

                    if (first)
                        shot.Kind = BarrelHitKind.Ennemi;
                    first = false;
                    HitSet.Add(enemy);
                    bool head = hb != null && hb.Head;
                    HitEnemy(ref shot, enemy, head, damage, h.point, cfg, mods, up, shooterId, time, 0);
                    if (pierceLeft > 0)
                    {
                        pierceLeft--;                                    // perforation : elle traverse, un peu affaiblie
                        damage *= mods.pierceDamageKept;
                        continue;
                    }

                    break;
                }

                if (!first)
                    break;                                               // après une perforation : le premier obstacle l'arrête
                var player = h.collider.GetComponentInParent<PlayerHealth>();
                if (player != null)
                {
                    shot.Kind = BarrelHitKind.Joueur;
                    if (player.TryHurt(shooterId, cfg.damage, h.point, time))
                        shot.Damage = cfg.damage;
                }

                break;
            }

            // Ricochets : depuis le dernier point touché, vers l'ennemi le plus proche en vue.
            int ricochets = up != null && mods != null ? up.Ricochets * mods.ricochetsPerStack : 0;
            Vector3 from = shot.Point;
            float rdamage = cfg.damage * (mods != null ? mods.ricochetDamage : 0.6f);
            for (int r = 0; r < ricochets && shot.Kind != BarrelHitKind.Rien; r++)
            {
                Damageable next = Nearest(from, mods.ricochetRange);
                if (next == null)
                    break;
                Vector3 to = next.Top - Vector3.up * 0.4f;
                SecondaryTrace(from, to);
                HitSet.Add(next);
                HitEnemy(ref shot, next, false, rdamage, to, cfg, mods, up, shooterId, time, 0);
                from = to;
            }

            BarrelEvents.Raise(shot);
            return shot;
        }

        /// <summary>Un ennemi touché : critique (tête, chance), chaîne, dégâts, paralysie, feu ; kill : explosion et balles remises.</summary>
        private static void HitEnemy(ref BarrelShot shot, Damageable enemy, bool head, float damage, Vector3 point, BarrelSettings cfg,
            BarrelModsSettings mods, ItHunts.Economy.PlayerUpgrades up, int shooterId, float time, int depth)
        {
            enemy.NoteAttacker(shooterId);                               // coop : il se retourne contre le tireur (D-041)
            bool crit = head || (up != null && mods != null && UnityEngine.Random.value < CritChance(up.CritChance, mods.critChanceK));
            float mult = 1f;
            if (crit)
            {
                Chains.TryGetValue(shooterId, out (float time, int chain) c);
                int chain = mods != null && time - c.time <= mods.chainWindow ? c.chain + 1 : 0;
                Chains[shooterId] = (time, chain);
                mult = CritMultiplier(mods, up != null ? up.ChainCrit : 0, chain);
            }

            float taken = enemy.ApplyDamage(enemy.ProjectileDamage(damage * mult), time, out bool killed);   // volant : un coup
            shot.Damage += taken;
            shot.Killed |= killed;
            shot.EnemiesHit++;
            shot.Headshot |= head;
            shot.Critical |= crit;
            if (head && shooterId != 0)
                ItHunts.Level.RunStats.Headshots++;
            if (taken > 0f)
                DamageEvents.Raise(new DamageEvent
                {
                    Target = enemy, Damage = taken, Point = point, Killed = killed, Tier = crit ? HitTier.Moyen : HitTier.Petit, Time = time,
                    Bullet = true, Critical = crit, Headshot = head, AttackerId = shooterId,
                });
            if (crit)
                ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings != null ? ItHunts.Audio.GameAudio.Settings.headshot : null, point, 1f);
            if (up == null || mods == null)
                return;
            if (!killed && up.Paralysis > 0)
            {
                var body = enemy.GetComponent<EnemyBody>();
                if (body != null)
                    body.StunFor(mods.paralysisPerStack * up.Paralysis);
            }

            if (!killed && up.Incendiary > 0)
                Burning.Apply(enemy, mods.burnDpsPerStack * up.Incendiary, mods.burnDuration, shooterId);
            if (!killed)
                return;
            if (up.ReloadOnKill > 0)
                shot.RoundsRefill += mods.roundsPerKillPerStack * up.ReloadOnKill;
            if (up.ExplodeOnKill > 0 && depth < mods.explosionChainMax)
                Explode(ref shot, enemy.Top - Vector3.up * 0.6f, mods.explosionRadius + mods.explosionRadiusPerStack * (up.ExplodeOnKill - 1),
                    mods.explosionDamage + mods.explosionDamagePerStack * (up.ExplodeOnKill - 1), cfg, mods, up, shooterId, time, depth + 1);
        }

        /// <summary>Explosion d'un ennemi tué (amélioration) : dégâts de zone aux autres ennemis (pas au traqueur), chaîne bornée.</summary>
        private static void Explode(ref BarrelShot shot, Vector3 at, float radius, float damage, BarrelSettings cfg, BarrelModsSettings mods,
            ItHunts.Economy.PlayerUpgrades up, int shooterId, float time, int depth)
        {
            var vfx = GameVfx.Settings;
            if (vfx != null)
                GameVfx.Spawn(vfx.impactHeavy != null ? vfx.impactHeavy : vfx.sparks, at, Quaternion.identity, Mathf.Max(1f, radius * 0.5f), null, 2f);
            int n = Physics.OverlapSphereNonAlloc(at, radius, Around, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var d = Around[i].GetComponentInParent<Damageable>();
                if (d == null || d.Dead || d.ImmuneToOrbs || HitSet.Contains(d))
                    continue;
                if (d.GetComponent<ItHunts.Enemies.Ghoul>() == null && d.GetComponent<ItHunts.Enemies.Flyer>() == null)
                    continue;
                HitSet.Add(d);
                float k = 1f - 0.5f * Mathf.Clamp01(Vector3.Distance(at, d.transform.position) / Mathf.Max(0.1f, radius));
                HitEnemy(ref shot, d, false, damage * k, d.Top - Vector3.up * 0.5f, cfg, mods, up, shooterId, time, depth);
            }
        }

        private static Damageable Nearest(Vector3 from, float range)
        {
            Damageable best = null;
            float bd = range * range;
            foreach (Damageable d in Damageable.All)
            {
                if (d == null || d.Dead || d.ImmuneToOrbs || HitSet.Contains(d))
                    continue;
                if (d.GetComponent<ItHunts.Enemies.Ghoul>() == null && d.GetComponent<ItHunts.Enemies.Flyer>() == null)
                    continue;
                Vector3 to = d.Top - Vector3.up * 0.4f;
                float sq = (to - from).sqrMagnitude;
                if (sq >= bd)
                    continue;
                if (Physics.Linecast(from + (to - from).normalized * 0.3f, to, out RaycastHit hit, ~0, QueryTriggerInteraction.Ignore)
                    && hit.collider.GetComponentInParent<Damageable>() == null && hit.collider.GetComponentInParent<PlayerHealth>() == null)
                    continue;                                            // pas en vue (seul le décor la cache)
                bd = sq;
                best = d;
            }

            return best;
        }

        /// <summary>Traînée peinte de la DA d'un ricochet.</summary>
        private static void SecondaryTrace(Vector3 from, Vector3 to)
        {
            var vfx = GameVfx.Settings;
            if (vfx == null || vfx.barrelTrace == null)
                return;
            GameObject go = GameVfx.Spawn(vfx.barrelTrace, Vector3.zero, Quaternion.identity, 1f, null, 1f);
            var trace = go != null ? go.GetComponent<ItHunts.Art.VFX.TraceVfx>() : null;
            if (trace != null)
                trace.Tire(from, to);
        }

        private sealed class Comparer : System.Collections.Generic.IComparer<RaycastHit>
        {
            public static readonly Comparer Instance = new Comparer();
            public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
        }
    }

    /// <summary>Canal des balles tirées (traînée, impact, son) : chaque vue joue celles qu'elle voit.</summary>
    public static class BarrelEvents
    {
        public static event Action<BarrelShot> Shot;
        public static void Raise(in BarrelShot s) => Shot?.Invoke(s);
    }
}
