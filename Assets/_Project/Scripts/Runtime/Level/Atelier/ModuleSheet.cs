using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>Un connecteur du module (entrée vers un couloir), relu depuis ses ModuleDoor.</summary>
    [System.Serializable]
    public struct SheetConnector
    {
        public DoorSide side;
        public int cell;
        public bool required;
        public float width, height;
    }

    /// <summary>
    /// Fiche d'un module de l'atelier (2026-11-21) : ce que le générateur et l'atelier savent du module sans ouvrir son prefab.
    /// Écrite à chaque sauvegarde depuis le prefab (qui fait foi) ; « actif dans la génération » met le module dans le catalogue.
    /// </summary>
    [CreateAssetMenu(fileName = "Fiche_Module", menuName = "ItHunts/Niveau/Fiche de module (atelier)")]
    public sealed class ModuleSheet : ScriptableObject
    {
        public string displayName = "Module";
        public ModuleKind kind = ModuleKind.Salle;
        public ModuleShape shape = ModuleShape.Simple;
        public Vector2Int footprint = Vector2Int.one;
        [Min(10f)] public float size = 56f;
        [Tooltip("Difficulté du module (0 facile à 2 difficile) : coffres et rencontres.")]
        [Range(0, 2)] public int difficulty;
        [Tooltip("Poids d'apparition dans le tirage.")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Actif dans la génération : le module est dans le catalogue du niveau (s'il est validé).")]
        public bool active = true;
        public GameObject prefab;
        public List<SheetConnector> connectors = new List<SheetConnector>();
        [Tooltip("Zone limite du module (locale) et zones de gameplay (coffres, apparitions…) : résumé.")]
        public Bounds bounds;
        public int chests, spawns, passages, lights, triangles, pieces;
        [TextArea(2, 12)] public string lastReport = "";
    }
}
