using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Emplacement d'un habitant (goule posée dès le départ) dans un module de salle.</summary>
    public sealed class ResidentSpot : MonoBehaviour
    {
        [Tooltip("Volant au lieu d'une goule (posé à cette hauteur).")]
        public bool flyer;

        private void OnDrawGizmos()
        {
            Gizmos.color = flyer ? new Color(0.6f, 0.9f, 1f) : new Color(0.4f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.6f, 0.6f);
        }
    }
}
