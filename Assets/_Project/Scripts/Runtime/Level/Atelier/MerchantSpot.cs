using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Emplacement du marchand dans un module (atelier). Pas encore posé par le générateur (salle de départ seulement).</summary>
    public sealed class MerchantSpot : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.2f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1.2f, 2f, 1.2f));
        }
    }
}
