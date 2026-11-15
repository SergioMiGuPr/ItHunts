using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>Ce qui peut apparaître à un point d'apparition.</summary>
    [System.Flags]
    public enum SpawnKinds
    {
        Goule = 1,
        Volant = 2,
    }

    /// <summary>
    /// Point d'apparition des vagues (D-041) : un objet vide avec ce composant, posé par le constructeur de scène ou à la main au
    /// level design (trou, grille, nid), sans code. Goule : au sol (sur le NavMesh) ; volant : en hauteur. La position est celle
    /// des pieds (goule) ou du centre (volant), l'orientation donne la direction de départ.
    /// </summary>
    public sealed class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private SpawnKinds kinds = SpawnKinds.Goule;

        public static readonly List<SpawnPoint> All = new List<SpawnPoint>();

        public SpawnKinds Kinds => kinds;

        /// <summary>Type d'ennemi accepté (points posés à l'exécution par le générateur de carte).</summary>
        public void Configure(SpawnKinds accepted) => kinds = accepted;
        /// <summary>Dernière apparition ici (évite deux ennemis au même point en même temps).</summary>
        public float LastUsed { get; set; } = float.NegativeInfinity;

        public bool Accepts(SpawnKinds kind) => (kinds & kind) != 0;

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void OnDrawGizmos()
        {
            Gizmos.color = Accepts(SpawnKinds.Volant) && !Accepts(SpawnKinds.Goule) ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.5f, 0.2f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.5f, transform.position + Vector3.up * 0.5f + transform.forward);
        }
    }
}
