using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Movement;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Artifacts
{
    /// <summary>État de l'épée en os d'un joueur : données seulement, copiables (réseau). Délais en ticks.</summary>
    public struct SwordState
    {
        /// <summary>Prochain coup du combo (0, 1, 2 ; 3 avec le tourbillon).</summary>
        public int Combo;
        public int LastSwingTick;
        /// <summary>Pas de coup avant ce tick (intervalle, pause après le dernier).</summary>
        public int ReadyTick;
        public int LastConsumedPressTick;
        public int Swings;
        public int LastIndex;
        public bool LastLunge;

        public static SwordState Create() => new SwordState
        {
            LastSwingTick = MovementState.NeverTick, ReadyTick = MovementState.NeverTick,
            LastConsumedPressTick = MovementState.NeverTick, LastIndex = -1,
        };
    }

    /// <summary>Ce qui s'est passé ce tick.</summary>
    public struct SwordStep
    {
        public bool Swung;
        /// <summary>Coup du combo (0, 1, 2 ; 3 : tourbillon, Enchaînement Mythique).</summary>
        public int Index;
        public bool Lunge;
    }

    /// <summary>Un coup qui a touché (debug, tests).</summary>
    public struct SwordHit
    {
        public Damageable Target;
        public float Damage;
        public bool Killed;
    }

    /// <summary>
    /// L'épée en os du koala (D-077), logique pure et testable. Même clic que le lancer (D-024) : sans objet tenu, un clic donne
    /// le coup suivant du combo (1, 2, 3) ; plus de comboWindow sans frapper : le combo repart du 1er. Pendant un dash ou une
    /// glissade, le coup est une fente. La vitesse du joueur augmente les dégâts (plafonnés). Rien ne touche au mouvement.
    /// Améliorations (D-100) : 4e coup tourbillon, 3e coup en cercle, saignement, fente qui étourdit, onde de taille, bonus contre
    /// les étourdis (PlayerUpgrades.Outgoing).
    /// </summary>
    public static class BoneSword
    {
        public static SwordStep Step(ref SwordState s, int tick, int pressTick, bool heldAtTickStart, bool holdingNow, bool lunge,
            BoneSwordSettings cfg, float dt)
        {
            var step = new SwordStep();
            int buffer = MovementSettings.SecondsToTicks(cfg.pressBuffer, dt);
            int since = tick - pressTick;
            bool fresh = pressTick > 0 && pressTick > s.LastConsumedPressTick && since >= 0 && since <= buffer;
            if (!fresh)
                return step;
            if (heldAtTickStart || holdingNow)
            {
                s.LastConsumedPressTick = pressTick;                 // clic du lancer : pas de coup
                return step;
            }

            if (s.ReadyTick != MovementState.NeverTick && tick < s.ReadyTick)
                return step;                                         // trop tôt : l'appui reste mémorisé (buffer)

            int hits = Mathf.Clamp(cfg.comboHits, 3, 4);
            int window = MovementSettings.SecondsToTicks(cfg.comboWindow, dt);
            if (s.LastSwingTick == MovementState.NeverTick || tick - s.LastSwingTick > window || s.Combo >= hits)
                s.Combo = 0;
            step.Swung = true;
            step.Index = s.Combo;
            step.Lunge = lunge;
            s.LastConsumedPressTick = pressTick;
            s.LastSwingTick = tick;
            s.Swings++;
            s.LastIndex = step.Index;
            s.LastLunge = lunge;
            int wait = MovementSettings.SecondsToTicks(cfg.swingInterval, dt);
            if (step.Index == hits - 1)
                wait += MovementSettings.SecondsToTicks(cfg.comboRecovery, dt);
            s.ReadyTick = tick + Mathf.Max(1, wait);
            s.Combo = (s.Combo + 1) % hits;
            return step;
        }

        /// <summary>Multiplicateur de dégâts selon la vitesse horizontale du joueur : 1 jusqu'à speedRef, plafond à speedFull.</summary>
        public static float SpeedMultiplier(float speed, BoneSwordSettings cfg)
        {
            float k = Mathf.InverseLerp(cfg.speedRef, Mathf.Max(cfg.speedRef + 0.01f, cfg.speedFull), speed);
            return 1f + k * (cfg.speedMaxMultiplier - 1f);
        }

        /// <summary>Dégâts d'un coup (index 0..3, fente) à cette vitesse ; le 4e coup (tourbillon) vaut 16 PV.</summary>
        public static float Damage(int index, bool lunge, float speed, BoneSwordSettings cfg)
        {
            float[] d = cfg.comboDamage;
            float baseDamage = index >= 3 ? 16f : d != null && d.Length > 0 ? d[Mathf.Clamp(index, 0, d.Length - 1)] : 10f;
            if (lunge)
                baseDamage = (d != null && d.Length > 0 ? d[d.Length - 1] : baseDamage) * cfg.lungeDamageFactor;
            return baseDamage * SpeedMultiplier(speed, cfg);
        }

        private static readonly Collider[] Overlaps = new Collider[64];
        private static readonly RaycastHit[] SightHits = new RaycastHit[16];

        /// <summary>
        /// Cible visée (aide au corps à corps) : l'ennemi parable le plus proche du viseur à moins de assistRange et assistAngle,
        /// visible depuis les yeux. Le traqueur n'est jamais visé (intouchable).
        /// </summary>
        public static bool FindTarget(Vector3 eye, Vector3 aim, BoneSwordSettings cfg, Transform shooter, out Damageable target, out Vector3 point)
        {
            target = null;
            point = Vector3.zero;
            if (cfg.assistRange <= 0f || cfg.assistAngle <= 0f)
                return false;
            int count = Physics.OverlapSphereNonAlloc(eye, cfg.assistRange, Overlaps, cfg.hitMask, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (c == null || (shooter != null && c.transform.IsChildOf(shooter)))
                    continue;
                var enemy = Damageable.Of(c);                          // boss final : son relais (HitProxy)
                if (enemy == null || enemy.Dead || enemy.ImmuneToOrbs)
                    continue;
                Vector3 p = c.bounds.center;
                Vector3 to = p - eye;
                float angle = Vector3.Angle(aim, to);
                if (angle > cfg.assistAngle || to.magnitude > cfg.assistRange || angle >= best || !Visible(eye, p, enemy, shooter))
                    continue;
                best = angle;
                target = enemy;
                point = p;
            }

            return target != null;
        }

        private static bool Visible(Vector3 eye, Vector3 p, Damageable enemy, Transform shooter)
        {
            Vector3 to = p - eye;
            int n = Physics.RaycastNonAlloc(eye, to.normalized, SightHits, to.magnitude, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Transform t = SightHits[i].transform;
                if ((shooter != null && t.IsChildOf(shooter)) || t.IsChildOf(enemy.transform) || Damageable.Of(SightHits[i].collider) == enemy
                    || SightHits[i].collider.attachedRigidbody != null)
                    continue;
                return false;                                            // un mur entre les deux
            }

            return true;
        }

        /// <summary>Élan vers la cible : vitesse à ajouter pour aller vers elle au moins à lungeSpeed ; jamais de freinage.</summary>
        public static Vector3 LungePush(Vector3 velocity, Vector3 toTarget, BoneSwordSettings cfg)
        {
            var flat = new Vector3(toTarget.x, 0f, toTarget.z);
            float d = flat.magnitude;
            if (cfg.lungeSpeed <= 0f || d <= cfg.lungeMinDistance)
                return Vector3.zero;
            Vector3 dir = flat / d;
            float along = Vector3.Dot(new Vector3(velocity.x, 0f, velocity.z), dir);
            return dir * Mathf.Max(0f, cfg.lungeSpeed - along);
        }

        /// <summary>
        /// Touche le monde : arc devant les yeux (portée, demi-angle), ou fente le long du déplacement (capsule). Chaque ennemi
        /// au plus une fois par coup ; le 3e coup fait reculer ; le 4e (tourbillon) frappe tout autour et repousse. Le traqueur
        /// n'est jamais touché. up : améliorations du joueur (saignement, fente qui étourdit, onde de taille).
        /// </summary>
        public static int Strike(Vector3 eye, Vector3 aim, Vector3 velocity, SwordStep step, float speed, BoneSwordSettings cfg,
            Transform shooter, int shooterId, float time, List<SwordHit> hits = null, ItHunts.Economy.PlayerUpgrades up = null)
        {
            Vector3 dir = aim.normalized;
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 lungeDir = flat.sqrMagnitude > 0.25f ? flat.normalized : new Vector3(dir.x, 0f, dir.z).normalized;
            bool whirl = step.Index >= 3;
            bool fullCircle = whirl || (step.Index == 2 && cfg.thirdHitFullCircle);
            int count = step.Lunge
                ? Physics.OverlapCapsuleNonAlloc(eye, eye + lungeDir * cfg.lungeLength, cfg.lungeRadius, Overlaps, cfg.hitMask, QueryTriggerInteraction.Collide)
                : Physics.OverlapSphereNonAlloc(eye, cfg.range + 1f, Overlaps, cfg.hitMask, QueryTriggerInteraction.Collide);
            var done = new HashSet<Damageable>();
            float damage = Damage(step.Index, step.Lunge, speed, cfg);
            bool lungeStun = step.Lunge && up != null && up.Mythic(UpgradeKind.Fente);
            int touched = 0;
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (c == null || (shooter != null && c.transform.IsChildOf(shooter)))
                    continue;
                var enemy = Damageable.Of(c);                          // boss final : son relais (HitProxy)
                if (enemy == null || enemy.Dead || enemy.ImmuneToOrbs || !done.Add(enemy))
                    continue;
                Vector3 point = c.ClosestPoint(eye);
                if (!step.Lunge)
                {
                    Vector3 to = point - eye;
                    if (to.magnitude > cfg.range || (!fullCircle && to.sqrMagnitude > 0.04f && Vector3.Angle(dir, to) > cfg.arcHalfAngle))
                        continue;
                }

                enemy.NoteAttacker(shooterId);
                float taken = enemy.ApplyDamage(ItHunts.Economy.PlayerUpgrades.Outgoing(shooterId, enemy, damage, melee: true), time, out bool killed);
                if (taken > 0f)
                    DamageEvents.Raise(new DamageEvent
                    {
                        Target = enemy, Damage = taken, Point = point, Killed = killed,
                        Tier = step.Index >= 2 || step.Lunge ? HitTier.Gros : HitTier.Moyen, Time = time, Melee = true, AttackerId = shooterId,
                    });
                if (!killed)
                {
                    // Recul à chaque coup (jour 5, point 4) : léger, plus fort au 3e coup et au tourbillon, et décroissant sur un même
                    // ennemi frappé à la suite (KnockbackScale) : on ne peut pas le tenir à distance en frappant sans arrêt.
                    float knockSpeed = whirl ? Mathf.Max(cfg.knockback, 6f) : step.Index == 2 && !step.Lunge ? cfg.knockback : cfg.hitKnockback;
                    var body = enemy.GetComponent<EnemyBody>();
                    if (knockSpeed > 0f && body != null)
                    {
                        Vector3 away = enemy.transform.position - eye;
                        away.y = 0f;
                        float scale = KnockbackScale(enemy, time, cfg);
                        if (scale > 0f && away.sqrMagnitude > 1e-4f)
                            body.Push((away.normalized * knockSpeed + Vector3.up * 1.5f) * scale);
                    }

                    if (up != null)
                    {
                        // Os brisé (K5) : saignement ; Fente Mythique (K3) : étourdit.
                        if (up.Has(UpgradeKind.OsBrise))
                            Bleeding.Apply(enemy, up.V(UpgradeKind.OsBrise), up.V2(UpgradeKind.OsBrise), shooterId, Mathf.RoundToInt(up.V3(UpgradeKind.OsBrise)), time);
                        if (lungeStun && enemy.Body != null)
                            enemy.Body.StunFor(1f);
                    }
                }

                touched++;
                hits?.Add(new SwordHit { Target = enemy, Damage = taken, Killed = killed });
            }

            // Onde de taille (K4) : un coup porté à plus de 12 m/s envoie une onde tranchante devant.
            if (up != null && up.Has(UpgradeKind.OndeDeTaille) && speed > 12f && !step.Lunge)
                touched += Wave(eye, dir, up.V(UpgradeKind.OndeDeTaille), up.V2(UpgradeKind.OndeDeTaille), up.Mythic(UpgradeKind.OndeDeTaille) ? 1f : 0f,
                    shooter, shooterId, time, done);
            return touched;
        }

        /// <summary>Suite de reculs d'un ennemi : nombre de coups qui l'ont fait reculer à la suite, instant du dernier.</summary>
        public struct KnockStreak
        {
            public int Count;
            public float LastAt;

            public static KnockStreak Create() => new KnockStreak { LastAt = float.NegativeInfinity };
        }

        /// <summary>
        /// Part du recul donnée par ce coup (logique pure) : 1 au premier coup, × knockbackDecay à chaque coup suivant porté moins de
        /// knockbackMemory s après le précédent, 0 sous knockbackMinScale ; la suite repart à 1 après knockbackMemory s sans coup.
        /// </summary>
        public static float KnockbackScale(ref KnockStreak s, float now, BoneSwordSettings cfg)
        {
            if (now - s.LastAt > cfg.knockbackMemory)
                s.Count = 0;
            float k = Mathf.Pow(Mathf.Clamp01(cfg.knockbackDecay), s.Count);
            s.Count++;
            s.LastAt = now;
            return k < cfg.knockbackMinScale ? 0f : k;
        }

        private static readonly Dictionary<Damageable, KnockStreak> Streaks = new Dictionary<Damageable, KnockStreak>();
        private static readonly List<Damageable> StaleStreaks = new List<Damageable>();

        /// <summary>Part du recul pour cet ennemi (suite partagée par tous les joueurs : en coop non plus, pas de recul sans fin).</summary>
        public static float KnockbackScale(Damageable enemy, float now, BoneSwordSettings cfg)
        {
            if (Streaks.Count > 64)
            {
                StaleStreaks.Clear();
                foreach (KeyValuePair<Damageable, KnockStreak> kv in Streaks)
                    if (kv.Key == null || now - kv.Value.LastAt > cfg.knockbackMemory)
                        StaleStreaks.Add(kv.Key);
                foreach (Damageable d in StaleStreaks)
                    Streaks.Remove(d);
            }

            KnockStreak s = Streaks.TryGetValue(enemy, out KnockStreak found) ? found : KnockStreak.Create();
            float k = KnockbackScale(ref s, now, cfg);
            Streaks[enemy] = s;
            return k;
        }

        /// <summary>Oublie les suites de reculs (tests, changement de scène).</summary>
        public static void ResetKnockback() => Streaks.Clear();

        /// <summary>Onde tranchante : capsule devant les yeux (longueur m, rayon 1 m), dégâts à chaque ennemi non déjà touché par le coup.</summary>
        public static int Wave(Vector3 eye, Vector3 dir, float length, float damage, float stun, Transform shooter, int shooterId, float time,
            HashSet<Damageable> except = null)
        {
            int count = Physics.OverlapCapsuleNonAlloc(eye, eye + dir * length, 1f, Overlaps, ~0, QueryTriggerInteraction.Collide);
            var done = new HashSet<Damageable>();
            int touched = 0;
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (c == null || (shooter != null && c.transform.IsChildOf(shooter)))
                    continue;
                var enemy = Damageable.Of(c);                          // boss final : son relais (HitProxy)
                if (enemy == null || enemy.Dead || enemy.ImmuneToOrbs || !done.Add(enemy) || (except != null && except.Contains(enemy)))
                    continue;
                enemy.NoteAttacker(shooterId);
                float taken = enemy.ApplyDamage(ItHunts.Economy.PlayerUpgrades.Outgoing(shooterId, enemy, damage, melee: true), time, out bool killed);
                if (taken > 0f)
                    DamageEvents.Raise(new DamageEvent
                    {
                        Target = enemy, Damage = taken, Point = c.ClosestPoint(eye), Killed = killed, Tier = HitTier.Moyen, Time = time, Melee = true, AttackerId = shooterId,
                    });
                if (!killed && stun > 0f && enemy.Body != null)
                    enemy.Body.StunFor(stun);
                touched++;
            }

            return touched;
        }
    }
}
