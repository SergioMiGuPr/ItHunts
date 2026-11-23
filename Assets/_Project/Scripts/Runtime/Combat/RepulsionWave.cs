using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;

namespace ItHunts.Combat
{
    /// <summary>
    /// Onde de répulsion (D-029) : un **cône depuis le joueur**, dans la direction visée (portée 14 m, ouverture 60°). À
    /// l'appui, une onde de choc (Apply) en plus de la propulsion du joueur (inchangée, la seule) ; bouton maintenu, une
    /// poussée continue (ApplyContinuous) jusqu'à 0.8 s. Elle repousse loin du joueur (et un peu vers le haut) les objets
    /// déplaçables par la télékinésie et les ennemis qu'elle voit, plus fort près du joueur et pour les cibles légères
    /// (AreaEffects.WavePushSpeed). Il faut une ligne de vue : un mur ou le décor l'arrêtent (pas les autres objets ou
    /// ennemis). Un objet accroché (lustre) est seulement décroché : il tombe droit. Jamais les joueurs, ni l'objet tenu,
    /// ni un ennemi tenu, ni le traqueur (Damageable.ImmuneToOrbs). Appelée par PlayerMotor au tick de la répulsion.
    /// Réseau (étape 11) : à faire là où la physique fait autorité.
    /// </summary>
    /// <summary>Cible de l'onde de répulsion qui n'est pas un ennemi (levier) : touchée si elle est dans le cône et visible.</summary>
    public interface IWaveTarget
    {
        Collider WaveCollider { get; }
        void OnWave(int playerId);
    }

    public static class RepulsionWave
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[16];

        /// <summary>Applique l'onde ; renvoie le nombre de cibles poussées.</summary>
        /// <param name="eye">Yeux du joueur (sommet du cône).</param>
        /// <param name="aimDir">Direction visée (axe du cône).</param>
        /// <param name="visualOrigin">Départ de l'onde visible (main droite).</param>
        /// <param name="owner">Racine du joueur (ses colliders ne bloquent pas la ligne de vue).</param>
        public static int Apply(Vector3 eye, Vector3 aimDir, Vector3 visualOrigin, MagnetismOrbSettings cfg, Rigidbody held,
            Transform owner, PhysicsScene? scene = null)
        {
            aimDir = aimDir.normalized;
            PhysicsScene physics = scene ?? Physics.defaultPhysicsScene;
            AreaEvents.RaiseConeWave(new ConeWaveEvent
            {
                Origin = visualOrigin,
                Direction = aimDir,
                Range = cfg.waveRange,
                ConeAngle = cfg.waveConeAngle,
            });

            int pushed = 0;
            Grabbable[] objects = new Grabbable[Grabbable.All.Count];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = Grabbable.All[i];
            foreach (Grabbable g in objects)
            {
                if (g == null || g.Held || g.Broken || g.Body == null || g.Body == held || g.IsEnemy)
                    continue;                                            // un ennemi est poussé par son corps (ci-dessous)
                Collider col = g.Colliders != null && g.Colliders.Length > 0 ? g.Colliders[0] : null;
                if (!InCone(col, g.Body.worldCenterOfMass, eye, aimDir, cfg, out Vector3 point, out float distance, out float angle))
                    continue;
                float speed = AreaEffects.WavePushSpeed(g.Mass, distance, false, cfg, angle);
                if (speed <= 0f || !Visible(physics, eye, point, g.Body, owner))
                    continue;
                if (g.Anchored)
                {
                    g.Unanchor();                                    // décroché : il tombe droit (lustre sur un ennemi)
                    g.SetOwner(ItHunts.Player.PlayerHealth.IdOf(owner));
                    pushed++;
                    continue;
                }

                if (g.Body.isKinematic)
                    continue;
                g.Body.linearVelocity += AreaEffects.WavePushDirection(eye, g.Body.worldCenterOfMass, cfg) * speed;
                g.MarkPushed(Time.time, ItHunts.Player.PlayerHealth.IdOf(owner));                             // dangereux un court moment (D-031)
                pushed++;
            }

            Damageable[] enemies = new Damageable[Damageable.All.Count];
            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = Damageable.All[i];
            foreach (Damageable d in enemies)
            {
                if (d == null || d.Dead || d.Body == null || d.ImmuneToOrbs || d.Body.Held)
                    continue;
                if (!InCone(d.MainCollider, d.Center, eye, aimDir, cfg, out Vector3 point, out float distance, out float angle))
                    continue;
                float speed = AreaEffects.WavePushSpeed(d.Body.Mass, distance, true, cfg, angle);
                if (speed <= 0f || !Visible(physics, eye, point, d.Body.Rigidbody, owner))
                    continue;
                d.Body.Push(AreaEffects.WavePushDirection(eye, d.Center, cfg) * speed);
                int ownerId = ItHunts.Player.PlayerHealth.IdOf(owner);
                d.NoteAttacker(ownerId);                                 // coop et âmes : ce joueur y est pour quelque chose
                float direct = RepelDamageFor != null ? RepelDamageFor(ownerId) : 0f;
                if (direct > 0f)
                    ImpactDamager.ApplyDirect(d, -ownerId - 11, direct, speed, 0f, point, Time.time, lethal: false);   // D-008
                float slow = RepelSlowFor != null ? RepelSlowFor(ownerId) : 0f;
                if (slow > 0f)
                    d.Slow(0.4f, slow);                                  // Onde blessante Or (D-100) : ralentit 1.5 s
                pushed++;
            }

            HitTargets(physics, eye, aimDir, cfg, owner);
            return pushed;
        }

        /// <summary>Cibles qui réagissent à l'onde sans être des ennemis (leviers du combat de boss).</summary>
        public static readonly System.Collections.Generic.List<IWaveTarget> Targets = new System.Collections.Generic.List<IWaveTarget>();

        private static void HitTargets(PhysicsScene physics, Vector3 eye, Vector3 aimDir, MagnetismOrbSettings cfg, Transform owner)
        {
            IWaveTarget[] targets = Targets.ToArray();
            foreach (IWaveTarget t in targets)
            {
                Collider c = t?.WaveCollider;
                if (c == null || !c.enabled)
                    continue;
                if (InCone(c, c.bounds.center, eye, aimDir, cfg, out Vector3 point, out _, out _) && Visible(physics, eye, point, null, owner))
                    t.OnWave(ItHunts.Player.PlayerHealth.IdOf(owner));
            }
        }

        /// <summary>Amélioration « dégâts de la répulsion » (D-043, D-008) : PV retirés par l'onde, par joueur (0 sans).</summary>
        public static System.Func<int, float> RepelDamageFor;
        /// <summary>Durée (s) du ralentissement des ennemis touchés par l'onde de ce joueur (Onde blessante Or, D-100) ; 0 : aucun.</summary>
        public static System.Func<int, float> RepelSlowFor;

        /// <summary>
        /// Répulsion maintenue (D-029), un tick : chaque cible visible dans le cône reçoit une poussée continue dans l'axe du
        /// cône (repelHoldAcceleration × facteur de masse × décroissance avec la distance), sans jamais dépasser
        /// repelHoldMaxSpeed dans l'axe ; le joueur n'est pas propulsé de nouveau. Un ennemi plaqué contre un mur ne prend
        /// rien en continu : seul un impact à 6 m/s ou plus l'étourdit (puis 2 s d'immunité). Renvoie le nombre de cibles.
        /// </summary>
        public static int ApplyContinuous(Vector3 eye, Vector3 aimDir, Vector3 visualOrigin, MagnetismOrbSettings cfg,
            Rigidbody held, Transform owner, float dt, PhysicsScene? scene = null)
        {
            aimDir = aimDir.normalized;
            PhysicsScene physics = scene ?? Physics.defaultPhysicsScene;
            AreaEvents.RaiseConeWave(new ConeWaveEvent
            {
                Origin = visualOrigin,
                Direction = aimDir,
                Range = cfg.waveRange,
                ConeAngle = cfg.waveConeAngle,
                Continuous = true,
            });

            int pushed = 0;
            Grabbable[] objects = new Grabbable[Grabbable.All.Count];
            for (int i = 0; i < objects.Length; i++)
                objects[i] = Grabbable.All[i];
            foreach (Grabbable g in objects)
            {
                if (g == null || g.Held || g.Broken || g.Body == null || g.Body == held || g.IsEnemy)
                    continue;
                Collider col = g.Colliders != null && g.Colliders.Length > 0 ? g.Colliders[0] : null;
                if (!InCone(col, g.Body.worldCenterOfMass, eye, aimDir, cfg, out Vector3 point, out float distance, out float angle))
                    continue;
                float accel = AreaEffects.HoldAcceleration(g.Mass, distance, false, cfg, angle);
                if (accel <= 0f || !Visible(physics, eye, point, g.Body, owner))
                    continue;
                if (g.Anchored)
                {
                    g.Unanchor();                                    // le lustre reste décrochable pendant le maintien
                    g.SetOwner(ItHunts.Player.PlayerHealth.IdOf(owner));
                    pushed++;
                    continue;
                }

                if (g.Body.isKinematic)
                    continue;
                float along = Vector3.Dot(g.Body.linearVelocity, aimDir);
                float dv = AreaEffects.HoldDelta(along, accel, dt, cfg);
                if (dv <= 0f)
                    continue;
                g.Body.linearVelocity += aimDir * dv;
                g.MarkPushed(Time.time, ItHunts.Player.PlayerHealth.IdOf(owner));
                pushed++;
            }

            Damageable[] enemies = new Damageable[Damageable.All.Count];
            for (int i = 0; i < enemies.Length; i++)
                enemies[i] = Damageable.All[i];
            foreach (Damageable d in enemies)
            {
                if (d == null || d.Dead || d.Body == null || d.ImmuneToOrbs || d.Body.Held)
                    continue;
                if (!InCone(d.MainCollider, d.Center, eye, aimDir, cfg, out Vector3 point, out float distance, out float angle))
                    continue;
                float accel = AreaEffects.HoldAcceleration(d.Body.Mass, distance, true, cfg, angle);
                if (accel <= 0f || !Visible(physics, eye, point, d.Body.Rigidbody, owner))
                    continue;
                Vector3 v = d.Body.InFlight ? d.Body.Rigidbody.linearVelocity : d.Body.Velocity;
                float dv = AreaEffects.HoldDelta(Vector3.Dot(v, aimDir), accel, dt, cfg);
                if (dv <= 0f)
                    continue;
                d.Body.Push(aimDir * dv);
                pushed++;
            }

            HitTargets(physics, eye, aimDir, cfg, owner);
            return pushed;
        }

        /// <summary>
        /// Le collider est-il dans le cône ? Si l'axe le traverse, on prend le point d'entrée sur sa surface ; sinon, son point
        /// le plus proche de l'axe (un gros objet est touché dès qu'une partie entre dans le cône). Renvoie ce point, sa
        /// distance aux yeux et son angle à l'axe.
        /// </summary>
        public static bool InCone(Collider col, Vector3 fallback, Vector3 eye, Vector3 aimDir, MagnetismOrbSettings cfg,
            out Vector3 point, out float distance, out float angle)
        {
            point = fallback;
            if (col != null && col.enabled && col.Raycast(new Ray(eye, aimDir), out RaycastHit entry, cfg.waveRange + 1f))
            {
                // L'axe du cône traverse l'objet : sa surface avant compte (pas son centre).
                point = entry.point;
                distance = entry.distance;
                angle = 0f;
                return distance <= cfg.waveRange;
            }

            if (col != null && col.enabled)
            {
                Vector3 p = col.bounds.center;
                for (int i = 0; i < 2; i++)
                {
                    float t = Mathf.Max(0f, Vector3.Dot(p - eye, aimDir));
                    p = col.ClosestPoint(eye + aimDir * t);
                }

                point = p;
            }

            AreaEffects.ConeCoordinates(eye, aimDir, point, out distance, out angle);
            return distance <= cfg.waveRange && angle <= cfg.waveConeAngle * 0.5f;
        }

        /// <summary>
        /// Ligne de vue des yeux jusqu'à la cible : seuls les murs et le décor la bloquent (les objets attrapables, les
        /// ennemis et le joueur lui-même non).
        /// </summary>
        private static bool Visible(PhysicsScene physics, Vector3 eye, Vector3 point, Rigidbody target, Transform owner)
        {
            Vector3 to = point - eye;
            float length = to.magnitude;
            if (length < 1e-3f)
                return true;
            int count = physics.Raycast(eye, to / length, Hits, Mathf.Max(0f, length - 0.05f), ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = Hits[i].collider;
                if (owner != null && c.transform.IsChildOf(owner))
                    continue;
                if (c.attachedRigidbody != null && (c.attachedRigidbody == target || c.attachedRigidbody.GetComponent<Grabbable>() != null))
                    continue;
                if (c.GetComponentInParent<Damageable>() != null)
                    continue;
                return false;                                            // un mur, le décor
            }

            return true;
        }
    }
}
