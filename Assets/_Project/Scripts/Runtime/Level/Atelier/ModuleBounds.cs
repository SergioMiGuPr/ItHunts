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
}
