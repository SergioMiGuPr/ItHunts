using System;
using ItHunts.Combat;
using ItHunts.Economy;
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
        /// <summary>Sixième chambre (C6) qui a tué : rechargement instantané (ArtifactHolder). Headshot qui tue avec Main leste Mythique : tout rechargé.</summary>
        public bool FullReload;
        /// <summary>Objet accroché par la balle-harpon (Langue de caméléon, C8).</summary>
        public bool Harpooned;
    }

    /// <summary>Options d'un tir : dernière balle du barillet (Sixième chambre), rafale (Éventail), dispersion (°).</summary>
    public struct BarrelShotOptions
    {
        public bool LastRound;
        public bool Burst;
        public float Spread;
        public float DamageFactor;
    }

    /// <summary>
    /// Tir instantané du barillet (D-032) : rayon depuis les yeux du joueur au moment du clic, exactement dans la direction
    /// du viseur, jusqu'à la portée, sans chute. Le premier collider touché (hors le tireur et les déclencheurs) arrête la
    /// balle : un ennemi prend les dégâts (sauf le traqueur), un autre joueur seulement avec le tir ami.
    /// Améliorations du caméléon (nuit 3 ; rangs : nuit 4, D-100) lues dans PlayerUpgrades : paralysie, cadavre explosif,
    /// perforation, ricochet, main leste, sixième chambre, éventail, langue de caméléon, œil fendu, peau changeante.
    /// Réseau (étape 11) : le serveur rejouera ce rayon avec compensation de latence (positions des cibles au moment du clic).
    /// </summary>
    public static class BarrelShooter
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>Critiques en chaîne : dernier critique et longueur de la chaîne, par tireur.</summary>
        private static readonly System.Collections.Generic.Dictionary<int, (float time, int chain)> Chains =
            new System.Collections.Generic.Dictionary<int, (float, int)>();
        /// <summary>Balles paralysantes Mythique : touches consécutives sur un paralysé, par tireur (cible, nombre).</summary>
        private static readonly System.Collections.Generic.Dictionary<int, (Damageable target, int hits)> ParalyzedHits =
            new System.Collections.Generic.Dictionary<int, (Damageable, int)>();
        private static readonly System.Collections.Generic.HashSet<Damageable> HitSet = new System.Collections.Generic.HashSet<Damageable>();

        /// <summary>Chance de critique hyperbolique (recherche nuit 3, Risk of Rain 2) : 1 − 1 / (1 + k · piles).</summary>
        public static float CritChance(int stacks, float k) => stacks <= 0 ? 0f : 1f - 1f / (1f + k * stacks);

        /// <summary>Chance de critique sur tout le corps (Œil fendu Or : 8 %, Mythique : 15 %).</summary>
        public static float BodyCritChance(PlayerUpgrades up) =>
            up == null ? 0f : up.Mythic(UpgradeKind.OeilFendu) ? 0.15f : up.AtLeast(UpgradeKind.OeilFendu, 2) ? 0.08f : 0f;

        /// <summary>Multiplicateur d'un critique : headshot + bonus de chaîne (par critique enchaîné, borné).</summary>
        public static float CritMultiplier(BarrelModsSettings m, int chainStacks, int chain) =>
            (m != null ? m.headshotMultiplier : 2f) + (m != null ? m.chainBonusPerStack : 0.5f) * chainStacks * Mathf.Min(chain, m != null ? m.chainMax : 4);

        /// <summary>Multiplicateur d'un critique avec Œil fendu (bonus et plafond de chaîne au rang courant).</summary>
        public static float CritMultiplier(BarrelModsSettings m, PlayerUpgrades up, int chain)
        {
            float head = m != null ? m.headshotMultiplier : 2f;
            if (up == null || !up.Has(UpgradeKind.OeilFendu))
                return head;
            return head + up.V2(UpgradeKind.OeilFendu) * Mathf.Min(chain, Mathf.RoundToInt(up.V3(UpgradeKind.OeilFendu)));
        }

        public static BarrelShot Fire(Vector3 eye, Vector3 direction, BarrelSettings cfg, Transform shooter, int shooterId,
            float time, int tick, PhysicsScene? scene = null, PlayerUpgrades up = null) =>
            Fire(eye, direction, cfg, shooter, shooterId, time, tick, scene, up, default);

        public static BarrelShot Fire(Vector3 eye, Vector3 direction, BarrelSettings cfg, Transform shooter, int shooterId,
            float time, int tick, PhysicsScene? scene, PlayerUpgrades up, BarrelShotOptions opt)
        {
            Vector3 dir = direction.normalized;
            if (opt.Spread > 0f)
                dir = (Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), dir) * Quaternion.AngleAxis(UnityEngine.Random.Range(0f, opt.Spread), Vector3.Cross(dir, Vector3.up).normalized) * dir).normalized;
            var shot = new BarrelShot
            {
                Tick = tick, Origin = eye, Direction = dir, Point = eye + dir * cfg.range, Normal = -dir, Distance = cfg.range,
                Kind = BarrelHitKind.Rien, ShooterId = shooterId,
            };
            BarrelModsSettings mods = cfg.mods;
            bool sixth = opt.LastRound && up != null && up.Has(UpgradeKind.SixiemeChambre);
            int pierceLeft = up != null ? Mathf.RoundToInt(up.V(UpgradeKind.BallePerforante)) : 0;
            bool pierceObjects = up != null && up.AtLeast(UpgradeKind.BallePerforante, 2);
            float pierceKept = up != null && up.Has(UpgradeKind.BallePerforante) ? up.V2(UpgradeKind.BallePerforante) : 0.75f;
            if (sixth && up.AtLeast(UpgradeKind.SixiemeChambre, 2))
            {
                pierceLeft = 99;                                         // sixième chambre Or : perce tout
                pierceKept = 1f;
            }

            float damage = cfg.damage * (opt.DamageFactor > 0f ? opt.DamageFactor : 1f) * (sixth ? up.V(UpgradeKind.SixiemeChambre) : 1f);
            bool camouflageCrit = up != null && up.ConsumeCamouflageCrit();
            HitSet.Clear();
            Damageable lastPierced = null;
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
                    HitEnemy(ref shot, enemy, head, damage, h.point, cfg, mods, up, shooterId, time, 0, camouflageCrit, sixth, opt);
                    camouflageCrit = false;
                    lastPierced = enemy;
                    if (pierceLeft > 0)
                    {
                        pierceLeft--;                                    // perforation : elle traverse, un peu affaiblie
                        damage *= pierceKept;
                        continue;
                    }

                    break;
                }

                // Objet physique : perforation Or le traverse ; Langue de caméléon (C8) le ramène vers l'orbe.
                var grab = h.collider.GetComponentInParent<ItHunts.Orbs.Grabbable>();
                if (grab != null && !grab.IsEnemy && grab.Body != null)
                {
                    if (up != null && up.Has(UpgradeKind.LangueDeCameleon) && h.distance <= up.V(UpgradeKind.LangueDeCameleon))
                    {
                        Harpoon(grab, eye, up, shooterId);
                        shot.Harpooned = true;
                    }

                    if (pierceObjects && !first)
                        continue;
                    if (pierceObjects && first)
                    {
                        first = false;
                        continue;
                    }
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

            // Balle perforante Mythique : le dernier ennemi traversé est paralysé.
            if (lastPierced != null && !lastPierced.Dead && up != null && up.Mythic(UpgradeKind.BallePerforante) && shot.EnemiesHit > 1)
                lastPierced.Paralyze(0.5f);

            // Ricochets : depuis le dernier point touché, vers l'ennemi le plus proche en vue.
            int ricochets = up != null ? Mathf.RoundToInt(up.V(UpgradeKind.Ricochet)) : 0;
            float rrange = up != null && up.Has(UpgradeKind.Ricochet) ? Mathf.Max(up.V3(UpgradeKind.Ricochet), mods != null ? mods.ricochetRange : 14f) : 0f;
            Vector3 from = shot.Point;
            float rdamage = cfg.damage * (up != null && up.Has(UpgradeKind.Ricochet) ? up.V2(UpgradeKind.Ricochet) : 0.6f);
            bool ricochetHead = shot.Headshot && up != null && up.Mythic(UpgradeKind.Ricochet);
            for (int r = 0; r < ricochets && shot.Kind != BarrelHitKind.Rien; r++)
            {
                Damageable next = Nearest(from, rrange);
                if (next == null)
                    break;
                Vector3 to = next.Top - Vector3.up * (ricochetHead ? 0.15f : 0.4f);
                SecondaryTrace(from, to);
                HitSet.Add(next);
                HitEnemy(ref shot, next, ricochetHead, rdamage, to, cfg, mods, up, shooterId, time, 0, false, false, opt);
                from = to;
            }

            BarrelEvents.Raise(shot);
            return shot;
        }

        /// <summary>Langue de caméléon (C8) : l'objet touché est ramené vers l'orbe (vitesse vers le joueur) ; Argent : il arrive « tenu ».</summary>
        private static void Harpoon(ItHunts.Orbs.Grabbable g, Vector3 eye, PlayerUpgrades up, int shooterId)
        {
            Vector3 to = eye - g.Body.worldCenterOfMass;
            float d = to.magnitude;
            if (d < 0.5f)
                return;
            g.Body.linearVelocity = to / d * Mathf.Clamp(d * 1.5f, 8f, 25f) + Vector3.up * 1.5f;
            g.SetOwner(shooterId);
            if (up.AtLeast(UpgradeKind.LangueDeCameleon, 1))
                g.HarpoonedBy = shooterId;                               // la télékinésie le prend à l'arrivée (TelekinesisOrb)
        }

        /// <summary>Un ennemi touché : critique (tête, chance), chaîne, dégâts, paralysie ; kill : explosion et balles remises.</summary>
        private static void HitEnemy(ref BarrelShot shot, Damageable enemy, bool head, float damage, Vector3 point, BarrelSettings cfg,
            BarrelModsSettings mods, PlayerUpgrades up, int shooterId, float time, int depth, bool forcedCrit, bool sixth, BarrelShotOptions opt)
        {
            enemy.NoteAttacker(shooterId);                               // coop : il se retourne contre le tireur (D-041)
            bool crit = head || forcedCrit || (up != null && UnityEngine.Random.value < BodyCritChance(up));
            float mult = 1f;
            if (crit)
            {
                Chains.TryGetValue(shooterId, out (float time, int chain) c);
                int chain = mods != null && time - c.time <= mods.chainWindow ? c.chain + 1 : 0;
                Chains[shooterId] = (time, chain);
                mult = forcedCrit ? Mathf.Max(3f, CritMultiplier(mods, up, chain)) : CritMultiplier(mods, up, chain);
            }

            float raw = damage * mult;
            // Œil fendu Mythique : headshot sur un paralysé = kill (goule et volant).
            if (head && enemy.Paralyzed && up != null && up.Mythic(UpgradeKind.OeilFendu) && !enemy.ImmuneToOrbs)
                raw = Mathf.Max(raw, enemy.Health.Current);
            float taken = enemy.ApplyDamage(enemy.ProjectileDamage(PlayerUpgrades.Outgoing(shooterId, enemy, raw, false, true)), time, out bool killed);   // volant : un coup
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
            if (crit && GameVfx.Settings != null && GameVfx.Settings.critical != null)
                GameVfx.Spawn(GameVfx.Settings.critical, point, Quaternion.identity, 1f, null, 1.5f);
            if (crit)
                ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings != null ? ItHunts.Audio.GameAudio.Settings.headshot : null, point, 1f);
            if (up == null)
                return;
            if (!killed)
            {
                // Balles paralysantes (C1), Éventail Mythique (C7), Sixième chambre Argent (C6).
                float paralyze = up.V(UpgradeKind.BallesParalysantes) + (opt.Burst && up.Mythic(UpgradeKind.Eventail) ? 0.3f : 0f);
                bool wasParalyzed = enemy.Paralyzed;
                if (paralyze > 0f && enemy.Paralyze(paralyze))
                {
                    ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.paralysis, point, 0.9f);
                    var pv = GameVfx.Settings;
                    if (pv != null && pv.paralysis != null)
                        GameVfx.Spawn(pv.paralysis, enemy.Top - Vector3.up * 0.6f, Quaternion.identity, 1f, enemy.transform, paralyze + 0.3f);
                }

                if (sixth && up.AtLeast(UpgradeKind.SixiemeChambre, 1) && enemy.Body != null)
                    enemy.Body.StunFor(1f);
                // Balles paralysantes Mythique : un paralysé touché 3 fois d'affilée est projeté (Rebond).
                if (up.Mythic(UpgradeKind.BallesParalysantes) && wasParalyzed)
                {
                    ParalyzedHits.TryGetValue(shooterId, out (Damageable target, int hits) ph);
                    int n = ph.target == enemy ? ph.hits + 1 : 1;
                    ParalyzedHits[shooterId] = (enemy, n);
                    if (n >= 3 && enemy.Body != null)
                    {
                        Vector3 away = enemy.Center - shot.Origin;
                        away.y = 0f;
                        enemy.Body.Push((away.sqrMagnitude > 1e-4f ? away.normalized : shot.Direction) * 14f + Vector3.up * 3f);
                        ParalyzedHits[shooterId] = (enemy, 0);
                    }
                }
                else
                    ParalyzedHits[shooterId] = (enemy, 0);

                // Éventail Or : la rafale pousse les goules.
                if (opt.Burst && up.AtLeast(UpgradeKind.Eventail, 2) && enemy.Body != null && enemy.GetComponent<ItHunts.Enemies.Ghoul>() != null)
                    enemy.Body.Push(shot.Direction * 3f);
                // Langue de caméléon : volants tirés vers le joueur et étourdis (Or) ; goules tirées (Diamant 3 m, Mythique jusqu'à vous).
                if (up.Has(UpgradeKind.LangueDeCameleon) && enemy.Body != null)
                {
                    bool flyer = enemy.GetComponent<ItHunts.Enemies.Flyer>() != null;
                    Vector3 toward = shot.Origin - enemy.Center;
                    toward.y = 0f;
                    if (flyer && up.AtLeast(UpgradeKind.LangueDeCameleon, 2))
                    {
                        enemy.Body.StunFor(1f);
                        enemy.Body.Push(toward.normalized * 10f);
                    }
                    else if (!flyer && up.AtLeast(UpgradeKind.LangueDeCameleon, 3) && toward.magnitude > 1.5f)
                    {
                        float pull = up.Mythic(UpgradeKind.LangueDeCameleon) ? Mathf.Min(toward.magnitude - 1.5f, 40f) : 3f;
                        enemy.Body.Push(toward.normalized * Mathf.Clamp(pull * 2f, 4f, 30f));
                        if (up.Mythic(UpgradeKind.LangueDeCameleon))
                            enemy.Body.StunFor(1f);
                    }
                }

                return;
            }

            // Kill : Main leste (C5), Éventail Mythique, Sixième chambre Mythique, Cadavre explosif (C2), Sixième chambre Diamant.
            if (up.Has(UpgradeKind.MainLeste))
                shot.RoundsRefill += Mathf.RoundToInt(up.V(UpgradeKind.MainLeste));
            if (opt.Burst && up.Mythic(UpgradeKind.Eventail))
                shot.RoundsRefill += 2;
            if (head && up.Mythic(UpgradeKind.MainLeste))
                shot.FullReload = true;
            if (sixth && up.Mythic(UpgradeKind.SixiemeChambre))
                shot.FullReload = true;
            if (up.Has(UpgradeKind.CadavreExplosif) && depth == 0)
                UpgradeEffects.Explode(enemy.Top - Vector3.up * 0.6f, up.V2(UpgradeKind.CadavreExplosif), up.V(UpgradeKind.CadavreExplosif), shooterId,
                    0f, up.Mythic(UpgradeKind.CadavreExplosif) ? 0.5f : 0f, false, Mathf.RoundToInt(up.V3(UpgradeKind.CadavreExplosif)) - 1, 0f, enemy);
            if (sixth && up.AtLeast(UpgradeKind.SixiemeChambre, 3) && depth == 0)
                UpgradeEffects.Explode(enemy.Top - Vector3.up * 0.6f, 4f, 30f, shooterId, 0f, 0f, false, 0, 0f, enemy);
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
