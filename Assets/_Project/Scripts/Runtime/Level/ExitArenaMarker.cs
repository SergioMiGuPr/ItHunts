using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Repère du prefab « sortie et arène du boss » (carte aléatoire) : origine sur la face extérieure du mur de la salle de
    /// sortie, au pied de la grande porte, +Z vers l'extérieur. Donne la boîte de l'arène (nœud du graphe du labyrinthe, relié à
    /// la salle de sortie) et l'emprise totale (NavMesh), dans le repère du prefab.
    /// </summary>
    public sealed class ExitArenaMarker : MonoBehaviour
    {
        [Tooltip("Centre du sol de l'arène (repère du prefab).")]
        public Vector3 arenaCenter;
        [Tooltip("Demi-taille de l'arène (x, plafond / 2, z).")]
        public Vector3 arenaHalfExtents = new Vector3(40f, 15f, 40f);
        [Tooltip("Emprise de tout le prefab (escaliers et arène), pour les bornes du NavMesh.")]
        public Bounds footprint = new Bounds(new Vector3(0f, 10f, 80f), new Vector3(100f, 60f, 170f));
    }
}
