using UnityEngine;

namespace ItHunts.Enemies
{
    /// <summary>
    /// Raccourci réservé au traqueur (D-033) : l'objet (une grille, un éboulis…) bloque le joueur, mais il est retiré du
    /// NavMesh du traqueur (qui passe au travers) ; le bas de sa boîte devient aussi un sol praticable pour lui seul (un
    /// raccourci au-dessus d'un vide). À placer au level design ; il faut une ouverture d'au moins 2.5 m pour lui.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class StalkerShortcut : MonoBehaviour
    {
    }
}
