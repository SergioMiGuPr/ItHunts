using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Emplacement possible d'un coffre (derrière un obstacle) dans un module ; le générateur en choisit un ou aucun.</summary>
    public sealed class ChestSpot : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 0.9f));
        }
    }
}
