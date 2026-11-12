using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Onde de répulsion (D-027) : au point visé par la répulsion, en plus de la propulsion du joueur, elle repousse les
    /// objets déplaçables par la télékinésie et les ennemis dans son rayon, loin du point et un peu vers le haut, plus
    /// fort près du centre et pour les cibles légères (AreaEffects.WavePushSpeed). Un objet accroché (lustre) est
    /// seulement décroché : il tombe droit, sur ce qui est dessous. Jamais les joueurs, ni l'objet tenu. Appelée par PlayerMotor au tick de la répulsion.
    /// Réseau (étape 11) : à faire là où la physique fait autorité.
    /// </summary>
    public static class RepulsionWave
    {
        /// <summary>Applique l'onde ; renvoie le nombre de cibles poussées.</summary>
        public static int Apply(Vector3 center, MagnetismOrbSettings cfg, Rigidbody held)
        {
            int pushed = 0;
            AreaEvents.RaiseArea(new AreaImpactEvent { Point = center, Radius = cfg.waveRadius, Wave = true });

            Grabbable[] objects = new Grabbable[Grabbable.All.Count];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = Grabbable.All[i];
            foreach (Grabbable g in objects)
            {
                if (g == null || g.Held || g.Broken || g.Body == null || g.Body == held)
                    continue;
                Collider col = g.Colliders != null && g.Colliders.Length > 0 ? g.Colliders[0] : null;
                Vector3 closest = col != null ? col.ClosestPoint(center) : g.Body.position;
                float speed = AreaEffects.WavePushSpeed(g.Mass, Vector3.Distance(center, closest), false, cfg);
                if (speed <= 0f)
                    continue;
                if (g.Anchored)
                {
                    g.Unanchor();                                    // décroché : il tombe droit (lustre sur un ennemi)
                    pushed++;
                    continue;
                }

                if (g.Body.isKinematic)
                    continue;
                g.Body.linearVelocity += AreaEffects.WavePushDirection(center, g.Body.worldCenterOfMass, cfg) * speed;
                pushed++;
            }

            Damageable[] enemies = new Damageable[Damageable.All.Count];
            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = Damageable.All[i];
            foreach (Damageable d in enemies)
            {
                if (d == null || d.Dead || d.Body == null)
                    continue;
                Collider col = d.MainCollider;
                Vector3 closest = col != null ? col.ClosestPoint(center) : d.transform.position;
                float speed = AreaEffects.WavePushSpeed(d.Body.Mass, Vector3.Distance(center, closest), true, cfg);
                if (speed <= 0f)
                    continue;
                d.Body.Push(AreaEffects.WavePushDirection(center, d.Center, cfg) * speed);
                pushed++;
            }

            return pushed;
        }
    }
}
