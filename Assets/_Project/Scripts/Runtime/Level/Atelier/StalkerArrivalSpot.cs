using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Point d'arrivée du traqueur dans le module (atelier). Le générateur le fait arriver par son repaire.</summary>
    public sealed class StalkerArrivalSpot : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 7f, 7f);
        }
    }
}
