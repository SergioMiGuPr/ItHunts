using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Côté d'un module (repère du module, avant rotation) : nord = +Z, est = +X.</summary>
    public enum DoorSide
    {
        Nord,
        Est,
        Sud,
        Ouest,
    }

    /// <summary>
    /// Entrée d'un module (carte aléatoire, étape 8) : une place de couloir au milieu du bord d'une case de la grille. Le
    /// générateur ne relie deux modules que par des entrées face à face ; une entrée non reliée est fermée par un mur du kit.
    /// Posée à la main dans le prefab du module : elle s'aimante à la place la plus proche (côté, case) dans l'éditeur.
    /// Un module sans aucune entrée accepte les 4 côtés de chaque case (modules carrés d'avant la carte aléatoire).
    /// </summary>
    public sealed class ModuleDoor : MonoBehaviour
    {
        [Tooltip("Côté du module (avant rotation).")]
        public DoorSide side = DoorSide.Nord;
        [Tooltip("Case le long de ce côté (0 : la première depuis l'ouest pour nord / sud, depuis le sud pour est / ouest).")]
        [Min(0)] public int cell;
        [Tooltip("Obligatoire : le générateur relie toujours cette entrée (sinon le module n'est pas posé à cet endroit).")]
        public bool required;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var module = GetComponentInParent<RoomModule>();
            if (module == null)
                return;
            cell = Mathf.Clamp(cell, 0, module.CellsAlong(side) - 1);
            transform.localPosition = module.DoorLocal(side, cell);   // aimantée à sa place
            transform.localRotation = Quaternion.Euler(0f, RoomModule.SideYaw(side), 0f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = required ? new Color(1f, 0.6f, 0.1f) : new Color(0.2f, 0.9f, 0.4f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(new Vector3(0f, 9.5f, 0f), new Vector3(14f, 19f, 1f));
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * 6f);
        }
#endif
    }
}
