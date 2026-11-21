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
        /// <summary>Prochain coup du combo (0, 1, 2).</summary>
        public int Combo;
        public int LastSwingTick;
        /// <summary>Pas de coup avant ce tick (intervalle, pause après le 3e).</summary>
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
        /// <summary>Coup du combo (0, 1, 2).</summary>
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

            int window = MovementSettings.SecondsToTicks(cfg.comboWindow, dt);
            if (s.LastSwingTick == MovementState.NeverTick || tick - s.LastSwingTick > window)
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
            if (step.Index == 2)
                wait += MovementSettings.SecondsToTicks(cfg.comboRecovery, dt);
            s.ReadyTick = tick + Mathf.Max(1, wait);
            s.Combo = (s.Combo + 1) % 3;
            return step;
        }

        /// <summary>Multiplicateur de dégâts selon la vitesse horizontale du joueur : 1 jusqu'à speedRef, plafond à speedFull.</summary>
        public static float SpeedMultiplier(float speed, BoneSwordSettings cfg)
        {
            float k = Mathf.InverseLerp(cfg.speedRef, Mathf.Max(cfg.speedRef + 0.01f, cfg.speedFull), speed);
            return 1f + k * (cfg.speedMaxMultiplier - 1f);
        }

        /// <summary>Dégâts d'un coup (index 0..2, fente) à cette vitesse.</summary>
        public static float Damage(int index, bool lunge, float speed, BoneSwordSettings cfg)
        {
            float[] d = cfg.comboDamage;
            float baseDamage = d != null && d.Length > 0 ? d[Mathf.Clamp(index, 0, d.Length - 1)] : 10f;
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
                var enemy = c.GetComponentInParent<Damageable>();
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
                if ((shooter != null && t.IsChildOf(shooter)) || t.IsChildOf(enemy.transform) || SightHits[i].collider.attachedRigidbody != null)
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
        /// au plus une fois par coup ; le 3e coup fait reculer. Le traqueur n'est jamais touché.
        /// </summary>
        public static int Strike(Vector3 eye, Vector3 aim, Vector3 velocity, SwordStep step, float speed, BoneSwordSettings cfg,
            Transform shooter, int shooterId, float time, List<SwordHit> hits = null)
        {
            Vector3 dir = aim.normalized;
            var flat = new Vector3(velocity.x, 0f, velocity.z);
            Vector3 lungeDir = flat.sqrMagnitude > 0.25f ? flat.normalized : new Vector3(dir.x, 0f, dir.z).normalized;
            int count = step.Lunge
                ? Physics.OverlapCapsuleNonAlloc(eye, eye + lungeDir * cfg.lungeLength, cfg.lungeRadius, Overlaps, cfg.hitMask, QueryTriggerInteraction.Collide)
                : Physics.OverlapSphereNonAlloc(eye, cfg.range + 1f, Overlaps, cfg.hitMask, QueryTriggerInteraction.Collide);
            var done = new HashSet<Damageable>();
            float damage = Damage(step.Index, step.Lunge, speed, cfg);
            int touched = 0;
            for (int i = 0; i < count; i++)
            {
                Collider c = Overlaps[i];
                if (c == null || (shooter != null && c.transform.IsChildOf(shooter)))
                    continue;
                var enemy = c.GetComponentInParent<Damageable>();
                if (enemy == null || enemy.Dead || enemy.ImmuneToOrbs || !done.Add(enemy))
                    continue;
                Vector3 point = c.ClosestPoint(eye);
                if (!step.Lunge)
                {
                    Vector3 to = point - eye;
                    if (to.magnitude > cfg.range || (to.sqrMagnitude > 0.04f && Vector3.Angle(dir, to) > cfg.arcHalfAngle))
                        continue;
                }

                enemy.NoteAttacker(shooterId);
                float taken = enemy.ApplyDamage(damage, time, out bool killed);
                if (taken > 0f)
                    DamageEvents.Raise(new DamageEvent
                    {
                        Target = enemy, Damage = taken, Point = point, Killed = killed,
                        Tier = step.Index == 2 || step.Lunge ? HitTier.Gros : HitTier.Moyen, Time = time, Melee = true,
                    });
                if (!killed && step.Index == 2 && !step.Lunge && cfg.knockback > 0f)
                {
                    var body = enemy.GetComponent<EnemyBody>();
                    Vector3 away = enemy.transform.position - eye;
                    away.y = 0f;
                    if (body != null && away.sqrMagnitude > 1e-4f)
                        body.Push(away.normalized * cfg.knockback + Vector3.up * 1.5f);
                }

                touched++;
                hits?.Add(new SwordHit { Target = enemy, Damage = taken, Killed = killed });
            }

            return touched;
        }
    }
}
