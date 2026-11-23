using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Effets de zone partagés par les améliorations (nuit 4, D-100) : explosion (Détonateur, Riposte, Rebond, Harpon, Combustion,
    /// Cadavre explosif, Chute amortie), étourdissement autour d'un point, embrasement des voisins (Allumette). Le traqueur n'est
    /// jamais touché hors de l'arène du boss (Damageable.ApplyDamage et EnemyBody.StunFor le refusent : ImmuneToOrbs). Les morts
    /// causées par une explosion sont chaînées ici (nombre de maillons borné) : les abonnés à Damageable.Died ignorent les morts
    /// pendant <see cref="InExplosion"/> pour ne pas doubler la chaîne.
    /// Réseau (étape 11) : effets calculés par l'autorité.
    /// </summary>
    public static class UpgradeEffects
    {
        /// <summary>Profondeur d'explosion en cours (0 : aucune).</summary>
        public static int Depth { get; private set; }
        public static bool InExplosion => Depth > 0;
        /// <summary>Explosions déclenchées (mesures, tests).</summary>
        public static int Explosions { get; private set; }

        /// <summary>
        /// Explosion : dégâts (pleins au centre, moitié au bord) à tous les ennemis vivants à moins de radius ; étourdit ou paralyse ;
        /// enflamme (12 PV/s, 3 s) ; repousse (m/s) ; les morts explosent à leur tour tant que chain > 0. Renvoie le nombre d'ennemis
        /// touchés.
        /// </summary>
        public static int Explode(Vector3 at, float radius, float damage, int attackerId, float stun = 0f, float paralyze = 0f,
            bool fire = false, int chain = 0, float push = 0f, Damageable except = null, bool fx = true)
        {
            if (radius <= 0f)
                return 0;
            Explosions++;
            if (fx)
                Fx(at, radius);
            Depth++;
            int touched = 0;
            var chained = new List<Damageable>();
            float now = Time.time;
            try
            {
                foreach (Damageable d in Snapshot())
                {
                    if (d == null || d == except || d.Dead || d.ImmuneToOrbs)
                        continue;
                    float dist = Vector3.Distance(at, d.Center);
                    if (dist > radius + 0.4f)
                        continue;
                    touched++;
                    d.NoteAttacker(attackerId);
                    float k = 1f - 0.5f * Mathf.Clamp01(dist / Mathf.Max(0.1f, radius));
                    bool killed = false;
                    float taken = damage > 0f ? d.ApplyDamage(damage * k, now, out killed) : 0f;
                    if (taken > 0f)
                        DamageEvents.Raise(new DamageEvent
                        {
                            Target = d, Damage = taken, Point = d.Center, Killed = killed, Tier = taken >= 25f ? HitTier.Moyen : HitTier.Petit,
                            Time = now, Area = true, AttackerId = attackerId,
                        });
                    if (killed)
                    {
                        if (chain > 0)
                            chained.Add(d);
                        continue;
                    }

                    if (stun > 0f && d.Body != null)
                        d.Body.StunFor(stun);
                    if (paralyze > 0f)
                        d.Paralyze(paralyze);
                    if (fire)
                        Burning.Apply(d, 12f, 3f, attackerId);
                    if (push > 0f && d.Body != null)
                    {
                        Vector3 away = d.Center - at;
                        away.y = 0f;
                        if (away.sqrMagnitude > 1e-4f)
                            d.Body.Push(away.normalized * push + Vector3.up * (push * 0.25f));
                    }
                }

                foreach (Damageable d in chained)
                    Explode(d.Center, radius, damage, attackerId, stun, paralyze, fire, chain - 1, push, d, fx);
            }
            finally
            {
                Depth--;
            }

            return touched;
        }

        /// <summary>Étourdit (duration s) les ennemis vivants à moins de radius.</summary>
        public static int StunAround(Vector3 at, float radius, float duration, Damageable except = null)
        {
            int n = 0;
            foreach (Damageable d in Snapshot())
            {
                if (d == null || d == except || d.Dead || d.ImmuneToOrbs || d.Body == null)
                    continue;
                if (Vector3.Distance(at, d.Center) > radius + 0.4f)
                    continue;
                if (d.Body.StunFor(duration))
                    n++;
            }

            return n;
        }

        /// <summary>Enflamme les ennemis vivants à moins de radius (dps PV/s pendant duration s), avec ralentissement et cumul.</summary>
        public static int IgniteAround(Vector3 at, float radius, float dps, float duration, int attackerId, float slow = 0f, int maxStacks = 1,
            Damageable except = null)
        {
            int n = 0;
            foreach (Damageable d in Snapshot())
            {
                if (d == null || d == except || d.Dead || d.ImmuneToOrbs)
                    continue;
                if (Vector3.Distance(at, d.Center) > radius + 0.4f)
                    continue;
                if (Burning.Apply(d, dps, duration, attackerId, 0.25f, null, -1f, maxStacks, slow) != null)
                    n++;
            }

            return n;
        }

        /// <summary>Ennemi vivant (pas le traqueur) le plus proche d'un point, à moins de radius ; null sinon.</summary>
        public static Damageable NearestEnemy(Vector3 at, float radius, Damageable except = null)
        {
            Damageable best = null;
            float bd = radius * radius;
            foreach (Damageable d in Damageable.All)
            {
                if (d == null || d == except || d.Dead || d.ImmuneToOrbs)
                    continue;
                float sq = (d.Center - at).sqrMagnitude;
                if (sq < bd)
                {
                    bd = sq;
                    best = d;
                }
            }

            return best;
        }

        /// <summary>Ennemis vivants à moins de radius (copie : la liste peut changer pendant les effets).</summary>
        public static List<Damageable> EnemiesAround(Vector3 at, float radius, Damageable except = null)
        {
            var r = new List<Damageable>();
            foreach (Damageable d in Damageable.All)
                if (d != null && d != except && !d.Dead && !d.ImmuneToOrbs && Vector3.Distance(at, d.Center) <= radius + 0.4f)
                    r.Add(d);
            return r;
        }

        private static Damageable[] Snapshot()
        {
            var a = new Damageable[Damageable.All.Count];
            for (int i = 0; i < a.Length; i++)
                a[i] = Damageable.All[i];
            return a;
        }

        /// <summary>Retour visuel et sonore d'une explosion (effet de la DA « explosion sur kill », à l'échelle du rayon).</summary>
        public static void Fx(Vector3 at, float radius)
        {
            if (!Application.isPlaying)
                return;
            var vfx = GameVfx.Settings;
            if (vfx != null)
            {
                GameObject prefab = vfx.explosionKill != null ? vfx.explosionKill : vfx.impactHeavy != null ? vfx.impactHeavy : vfx.sparks;
                if (prefab != null)
                    GameVfx.Spawn(prefab, at, Quaternion.identity, vfx.explosionKill != null ? radius / 3.5f : Mathf.Max(1f, radius * 0.5f), null, 3f);
            }

            ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.explosionKill, at, 1f);
        }
    }
}
