using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Pièce trop petite pour le traqueur (D-035) : il n'y entre pas, il s'arrête devant une ouverture (porte, fenêtre) et
    /// passe le bras ; sa main va jusqu'à roomReach (15 m) à l'intérieur, depuis l'ouverture. La boîte (size, centrée sur
    /// cet objet, sans collider) est l'intérieur ; chaque ouverture est un repère au centre de l'ouverture, son axe z vers
    /// l'extérieur.
    /// Règle de construction : aucun point de la pièce à plus de roomReach d'une ouverture.
    /// </summary>
    public sealed class ReachRoom : MonoBehaviour
    {
        [Tooltip("Taille de l'intérieur (m), centrée sur cet objet.")]
        [SerializeField] private Vector3 size = new Vector3(12f, 6f, 12f);
        [SerializeField] private Transform[] openings = new Transform[0];

        public static readonly List<ReachRoom> All = new List<ReachRoom>();

        public IReadOnlyList<Transform> Openings => openings;

        private void OnEnable() => All.Add(this);

        private void OnDisable() => All.Remove(this);

        public bool Contains(Vector3 p)
        {
            Vector3 local = transform.InverseTransformPoint(p);
            Vector3 h = size * 0.5f;
            // Le seuil des ouvertures (épaisseur du mur, côté −z) compte comme la pièce.
            return Mathf.Abs(local.x) <= h.x && Mathf.Abs(local.y) <= h.y + 0.5f && local.z <= h.z && local.z >= -h.z - 1.2f;
        }

        /// <summary>Ouverture la plus proche du point (null s'il n'y en a pas).</summary>
        public Transform NearestOpening(Vector3 p)
        {
            Transform best = null;
            float bestD = float.PositiveInfinity;
            foreach (Transform o in openings)
            {
                if (o == null)
                    continue;
                float d = (o.position - p).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = o;
                }
            }

            return best;
        }

        public Vector3 Size => size;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, size);
        }

        /// <summary>La pièce qui contient le point (null : aucune).</summary>
        public static ReachRoom At(Vector3 p)
        {
            foreach (ReachRoom r in All)
                if (r != null && r.Contains(p))
                    return r;
            return null;
        }
    }
}
