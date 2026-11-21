using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Zone limite du module (atelier) : boîte englobante locale ; rien de jouable ne doit en sortir.</summary>
    public sealed class ModuleBounds : MonoBehaviour
    {
        public Vector3 center = new Vector3(0f, 9f, 0f);
        public Vector3 size = new Vector3(56f, 18f, 56f);

        public Bounds Local => new Bounds(center, size);

        private void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(center, size);
        }
    }

    /// <summary>Emplacement du marchand dans un module (atelier). Pas encore posé par le générateur (salle de départ seulement).</summary>
    public sealed class MerchantSpot : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.2f);
            Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1.2f, 2f, 1.2f));
        }
    }

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

    /// <summary>Point d'arrivée du traqueur dans le module (atelier). Le générateur le fait arriver par son repaire.</summary>
    public sealed class StalkerArrivalSpot : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.1f, 0.1f);
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 7f, 7f);
        }
    }

    /// <summary>Plaque magnétique lumineuse (atelier) : toute surface répond au magnétisme ; celle-ci se voit de loin.</summary>
    public sealed class MagneticPlate : MonoBehaviour
    {
    }
}
