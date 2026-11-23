using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Sortie de la zone (déclencheur) : un joueur qui y entre termine la zone (ZoneState.ExitReached).
    /// Nuit 4, point 1 (« porte qui n'envoie pas à l'acte suivant ») : mesuré en jeu sur 3 actes × 3 seeds, le joueur traversait la boîte en
    /// marchant sans qu'aucun OnTriggerEnter n'arrive. La sortie teste donc elle-même, à chaque pas fixe, si le centre d'un joueur vivant
    /// est dans sa boîte ; l'événement de déclenchement reste en plus.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ZoneExit : MonoBehaviour
    {
        private ZoneState _zone;
        private Collider _box;

        /// <summary>Sorties prises par le test de boîte (mesure) et par l'événement de déclenchement.</summary>
        public static int ByOverlap { get; private set; }
        public static int ByTrigger { get; private set; }

        private void Start()
        {
            _zone = FindFirstObjectByType<ZoneState>();
            _box = GetComponent<Collider>();
        }

        private void OnTriggerEnter(Collider other)
        {
            var p = other.GetComponentInParent<PlayerHealth>();
            if (p == null || Zone == null || Zone.Finished || DoorClosed())
                return;
            ByTrigger++;
            Zone.ExitReached(p);
        }

        private ZoneState Zone => _zone != null ? _zone : _zone = FindFirstObjectByType<ZoneState>();

        // Relecture de la nuit 4 : la boîte du hall ne compte que porte ouverte (sinon une téléportation ou une traction à travers
        // les battants faisait gagner l'acte sans payer la sortie).
        private ItHunts.Level.ExitDoor _door;
        private bool _doorSearched;

        private bool DoorClosed()
        {
            if (!_doorSearched)
            {
                _doorSearched = true;
                _door = FindFirstObjectByType<ItHunts.Level.ExitDoor>();
            }

            return _door != null && !_door.Open;
        }

        private void FixedUpdate()
        {
            if (Zone == null || Zone.Finished || _box == null || DoorClosed())
                return;
            Bounds b = _box.bounds;
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                if (b.Contains(p.Center) || b.Contains(p.transform.position + Vector3.up * 0.3f))
                {
                    ByOverlap++;
                    Zone.ExitReached(p);
                    return;
                }
            }
        }
    }
}
