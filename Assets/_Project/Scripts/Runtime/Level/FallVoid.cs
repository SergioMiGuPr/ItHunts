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
                if (p.Motor != null)
                    p.Motor.Teleport(RespawnFor(p.transform.position));
                p.TakeEnvironmentDamage(damage, p.transform.position, Time.time);
                return;
            }

            var dmg = other.GetComponentInParent<Damageable>();
            if (dmg != null && !dmg.ImmuneToOrbs && !dmg.Dead)
                dmg.ApplyDamage(1e6f, Time.time, out _);
        }
    }
}
