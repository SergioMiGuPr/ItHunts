using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>Sortie de la zone (déclencheur) : un joueur qui y entre termine la zone (ZoneState.ExitReached).</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ZoneExit : MonoBehaviour
    {
        private ZoneState _zone;

        private void Start() => _zone = FindFirstObjectByType<ZoneState>();

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerHealth>();
            if (p != null && _zone != null)
                _zone.ExitReached(p);
        }
    }
}
