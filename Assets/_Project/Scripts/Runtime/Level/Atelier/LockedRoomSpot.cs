using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Emplacement d'une salle verrouillée (atelier). Le générateur les pose aujourd'hui le long des couloirs.</summary>
    public sealed class LockedRoomSpot : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.3f, 1f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, 3f, 0f), new Vector3(12f, 6f, 8f));
        }
    }
}
