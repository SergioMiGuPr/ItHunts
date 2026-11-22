using System.Collections.Generic;
using ItHunts.Combat;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Vide d'une zone de parcours ou d'une fosse (étape 8, après playtest) : un joueur qui y tombe revient au point de reprise le
    /// plus proche et perd damage PV ; un ennemi qui y tombe meurt ; le traqueur n'y va pas (NavMesh).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class FallVoid : MonoBehaviour
    {
        [Min(0f)] [SerializeField] private float damage = 20f;
        [Tooltip("Points de reprise (bords) ; vide : le centre du dessus du déclencheur.")]
        [SerializeField] private Transform[] respawns;

        public int Falls { get; private set; }

        public void Configure(float damagePoints, Transform[] points)
        {
            damage = damagePoints;
            respawns = points;
        }

        /// <summary>
        /// Dernier sol sûr de chaque joueur (nuit 3, point 4) : posé au sol, sur un appui large (le sol tient aussi à 1.5 m autour) et
        /// hors de tout vide. La chute ramène là (pas au bord le plus proche) ; sans sol connu, au point de reprise le plus proche.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<PlayerHealth, Vector3> LastSafe =
            new System.Collections.Generic.Dictionary<PlayerHealth, Vector3>();
        private static readonly System.Collections.Generic.List<FallVoid> Voids = new System.Collections.Generic.List<FallVoid>();
        private static int _trackedFrame = -1;

        private void OnEnable() => Voids.Add(this);
        private void OnDisable() => Voids.Remove(this);

        private void FixedUpdate()
        {
            if (_trackedFrame == Time.frameCount || Voids.Count == 0 || Voids[0] != this)
                return;
            _trackedFrame = Time.frameCount;
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && !p.Dead && p.Motor != null && p.Motor.State.Grounded && IsSafe(p.transform.position))
                    LastSafe[p] = p.transform.position + Vector3.up * 0.1f;
        }

        public static bool IsSafe(Vector3 feet)
        {
            foreach (FallVoid v in Voids)
            {
                Bounds b = v.GetComponent<BoxCollider>().bounds;
                if (feet.x > b.min.x - 1.5f && feet.x < b.max.x + 1.5f && feet.z > b.min.z - 1.5f && feet.z < b.max.z + 1.5f && feet.y > b.min.y)
                {
                    // Au-dessus de la boîte du vide : sûr seulement si le sol tient tout autour (pont large, bord éloigné).
                    for (int i = 0; i < 8; i++)
                    {
                        float a = i * Mathf.PI * 0.25f;
                        Vector3 probe = feet + new Vector3(Mathf.Cos(a), 0.5f, Mathf.Sin(a)) * 1.5f;
                        if (!Physics.Raycast(probe, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore))
                            return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Point de retour d'un joueur tombé : son dernier sol sûr, sinon le point de reprise le plus proche.</summary>
        public Vector3 RespawnFor(PlayerHealth p) =>
            p != null && LastSafe.TryGetValue(p, out Vector3 safe) ? safe : RespawnFor(p != null ? p.transform.position : transform.position);

        public Vector3 RespawnFor(Vector3 position)
        {
            Vector3 best = transform.position + Vector3.up * GetComponent<BoxCollider>().size.y;
            float d = float.PositiveInfinity;
            if (respawns != null)
                foreach (Transform t in respawns)
                    if (t != null && (t.position - position).sqrMagnitude < d)
                    {
                        d = (t.position - position).sqrMagnitude;
                        best = t.position;
                    }

            return best;
        }

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerHealth>();
            if (p != null)
            {
                if (p.Dead)
                    return;
                Falls++;
                ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Settings?.fallVoid, p.transform.position, 1f, 30f);
                if (p.Motor != null)
                    p.Motor.Teleport(RespawnFor(p));
                p.TakeEnvironmentDamage(damage, p.transform.position, Time.time, "Chute dans le vide");
                return;
            }

            var dmg = other.GetComponentInParent<Damageable>();
            if (dmg != null && !dmg.ImmuneToOrbs && !dmg.Dead)
                dmg.ApplyDamage(1e6f, Time.time, out _);
        }
    }
}
