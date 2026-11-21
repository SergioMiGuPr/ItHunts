using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Ancre de coffre d'un module (point 9, 2026-11-21) : placée de façon logique (au bout d'une route difficile, sur un balcon,
    /// de l'autre côté de l'eau, au fond du puits), avec un niveau de difficulté d'accès. Le générateur préfère les ancres
    /// difficiles et y met parfois un coffre rare (meilleur butin, D-071).
    /// </summary>
    public sealed class ChestSpot : MonoBehaviour
    {
        [Tooltip("Difficulté d'accès : 0 facile (au sol), 1 moyenne (étage, saut), 2 difficile (route de parcours, de l'autre côté de l'eau).")]
        [Range(0, 2)] public int difficulty;
        [Tooltip("Ce que la route demande (debug, plan).")]
        public string route = "";

        /// <summary>Poids du tirage d'une ancre : les ancres difficiles sont préférées (1, 2, 3).</summary>
        public static float Weight(int difficulty) => 1f + Mathf.Clamp(difficulty, 0, 2);

        /// <summary>Un coffre rare peut aller sur une ancre difficile seulement.</summary>
        public static bool RareAllowed(int difficulty) => difficulty >= 2;

        private void OnDrawGizmos()
        {
            Gizmos.color = difficulty >= 2 ? new Color(1f, 0.3f, 0.2f) : difficulty == 1 ? new Color(1f, 0.6f, 0.2f) : new Color(1f, 0.8f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, 0.45f, 0f), new Vector3(1.4f, 0.9f, 0.9f));
        }
    }
}
