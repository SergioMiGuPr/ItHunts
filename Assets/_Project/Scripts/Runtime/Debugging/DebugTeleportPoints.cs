using System;
using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Points de téléportation de debug d'une scène de test (F4 = départ, F5 à F9 = zones).
    /// Donnée de scène : chaque point est un Transform (position des pieds, rotation = direction de la vue).
    /// </summary>
    public sealed class DebugTeleportPoints : MonoBehaviour
    {
        [Serializable]
        public struct Point
        {
            public string label;
            public Transform target;
        }

        [Tooltip("Dans l'ordre des touches : F4, F5, F6, F7, F8, F9.")]
        public Point[] points = Array.Empty<Point>();
    }
}
