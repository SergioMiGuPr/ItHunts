using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Collision pour les joueurs et les objets seulement (anneau des arcs de couloir) : le traqueur passe au travers comme avant
    /// (ni NavMesh, ni hauteur libre pour se baisser), sinon il se baisserait sous chaque arc (tous les 20 m).
    /// </summary>
    public sealed class StalkerPassesThrough : MonoBehaviour
    {
    }
}
