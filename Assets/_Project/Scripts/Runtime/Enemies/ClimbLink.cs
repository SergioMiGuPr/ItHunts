using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Passage par un mur pour les goules (D-035) : du pied du mur (cet objet) jusqu'au bord en haut (<see cref="top"/>),
    /// dans les deux sens. ZoneNavMesh en fait un lien de NavMesh pour les ennemis de taille humaine seulement (le traqueur
    /// ne grimpe pas). La goule le parcourt collée au mur (Ghoul : mode Mur).
    /// </summary>
    public sealed class ClimbLink : MonoBehaviour
    {
        [SerializeField] private Transform top;
        [SerializeField, Min(0.5f)] private float width = 2f;

        public Vector3 Bottom => transform.position;
        public Vector3 Top => top != null ? top.position : transform.position;
        public float Width => width;
    }
}
