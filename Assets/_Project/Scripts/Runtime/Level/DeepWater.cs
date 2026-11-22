using ItHunts.Combat;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Eau profonde d'un canal d'égout (D-052) : c'est un vide. Un joueur qui y tombe remonte sur la rive la plus proche (du côté
    /// d'où il vient) et perd damage PV ; un ennemi qui y tombe (lancé, repoussé) se noie ; le traqueur n'y entre pas (NavMesh).
    /// Déclencheur en boîte : axe local z = travers du canal (les rives sont à ±(bankDistance) en z local), x = le long.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DeepWater : MonoBehaviour
    {
        [Tooltip("Dégâts en tombant dans l'eau (PV).")]
        [Min(0f)] [SerializeField] private float damage = 20f;
        [Tooltip("Distance (m, z local) du milieu du canal au point de remontée sur chaque rive.")]
        [Min(0.5f)] [SerializeField] private float bankDistance = 5f;
        [Tooltip("Hauteur du sol des rives (m, y local).")]
        [SerializeField] private float bankHeight = 1f;

        public float Damage => damage;
        public int Falls { get; private set; }

        public void Configure(float damagePoints, float bank, float height)
        {
            damage = damagePoints;
            bankDistance = bank;
            bankHeight = height;
        }

        /// <summary>Point de remontée pour un joueur à cette position : rive de son côté, même abscisse le long du canal.</summary>
        public Vector3 BankFor(Vector3 position)
        {
            Vector3 local = transform.InverseTransformPoint(position);
            var box = GetComponent<BoxCollider>();
            float halfLen = box.size.x * 0.5f - 1f;
            float x = Mathf.Clamp(local.x, box.center.x - halfLen, box.center.x + halfLen);
            float side = local.z >= 0f ? 1f : -1f;
            return transform.TransformPoint(new Vector3(x, bankHeight, side * bankDistance));
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
                    p.Motor.Teleport(BankFor(p.transform.position));
                p.TakeEnvironmentDamage(damage, p.transform.position, Time.time, "Noyade");
                return;
            }

            var d = other.GetComponentInParent<Damageable>();
            if (d != null && !d.ImmuneToOrbs && !d.Dead)
                d.ApplyDamage(1e6f, Time.time, out _);                     // noyé
        }
    }
}
