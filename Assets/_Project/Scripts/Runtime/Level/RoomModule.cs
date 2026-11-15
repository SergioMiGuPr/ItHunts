using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level
{
    public enum ModuleKind
    {
        Salle,
        Carrefour,
    }

    public enum ModuleLevel
    {
        Tous,
        Sol,
        Haut,
    }

    public enum ModuleHole
    {
        Aucun,
        /// <summary>Canal d'égout le long de x (eau profonde : retour sur la rive, dégâts), ponts posés par le générateur.</summary>
        Canal,
        /// <summary>Fosse vide (zone de parcours) : chute = retour au bord, dégâts.</summary>
        Vide,
    }

    /// <summary>Type de module, pour la hauteur du plafond (réglages de la carte).</summary>
    public enum ModuleShape
    {
        /// <summary>Un seul niveau de jeu (ponts à 10 m au plus).</summary>
        Simple,
        /// <summary>Plusieurs étages (sols jusqu'à 12 m).</summary>
        Etages,
        /// <summary>Salle d'égout (canal).</summary>
        Egout,
    }

    /// <summary>
    /// Module de salle (étape 8, carte aléatoire) : l'intérieur d'une salle, fait à la main en prefab, que le générateur pose sur
    /// une ou plusieurs cases de la grille (120 m) et tourne d'un quart de tour au hasard (sauf canal). Le générateur fait la
    /// coque avec les pièces du kit (sol, murs, plafond, arcs des entrées, murs qui ferment les entrées inutilisées) et le volume
    /// de salle. Le module apporte décor, étages, ponts, escaliers (pièces du kit), objets à lancer, lumières, points
    /// d'apparition (SpawnPoint), emplacements des habitants (ResidentSpot) et des coffres (ChestSpot), et ses entrées
    /// (ModuleDoor ; sans entrée déclarée, les 4 côtés de chaque case). Il n'entre dans le tirage que s'il a été validé
    /// (ItHunts > Valider le module) et qu'il n'a pas changé depuis. Règles : voir la fiche run et niveaux.
    /// </summary>
    public sealed class RoomModule : MonoBehaviour
    {
        /// <summary>Pas de la grille de la carte (m) : une case par salle ; doit valoir le pas des réglages de carte.</summary>
        public const float GridSpacing = 120f;

        [Tooltip("Nom affiché (debug, plan).")]
        public string displayName = "Salle";
        public ModuleKind kind = ModuleKind.Salle;
        [Tooltip("Côté de la salle sur une case (m, multiple de 4 : pièces du kit) : 48, 56 ou 64 pour une salle, 20 pour un carrefour.")]
        [Min(10f)] public float size = 56f;
        [Tooltip("Empreinte en cases de la grille (x, z) : 1 × 1, 1 × 2, 2 × 1 ou 2 × 2. Sur deux cases, la salle couvre aussi l'espace entre elles.")]
        public Vector2Int footprint = Vector2Int.one;
        [Tooltip("Niveau où le module peut aller : sol, haut (+16 m, seulement les modules qui le déclarent), ou les deux.")]
        public ModuleLevel level = ModuleLevel.Tous;
        [Tooltip("Poids du tirage (0 : jamais tiré).")]
        [Min(0f)] public float weight = 1f;
        [Tooltip("Le générateur peut le tourner d'un quart de tour (pas les modules à canal).")]
        public bool rotate = true;
        [Tooltip("Type, pour la hauteur du plafond (réglée par type dans MapLayout) : salle simple, à étages, d'égout.")]
        public ModuleShape shape = ModuleShape.Simple;
        [Tooltip("Plafond imposé (m, 15 au moins) ; 0 : celui de son type.")]
        [Min(0f)] public float ceilingOverride;

        [Header("Contraintes de tirage")]
        [Tooltip("Nombre minimal par carte (0 : aucun).")]
        [Min(0)] public int minPerMap;
        [Tooltip("Nombre maximal par carte (0 : pas de limite).")]
        [Min(0)] public int maxPerMap;
        [Tooltip("Distance minimale à la salle de départ, en salles (0 : aucune).")]
        [Min(0)] public int minStartDistance;
        [Tooltip("Peut recevoir la sortie du niveau (grande porte vers l'arène du boss) : salle seulement.")]
        public bool exitAllowed = true;

        [Header("Sol percé")]
        public ModuleHole hole = ModuleHole.Aucun;
        [Tooltip("Rectangle du trou dans le sol (local : x min, z min, largeur, profondeur en z).")]
        public Rect holeRect = new Rect(-8f, 8f, 16f, 6f);
        [Min(1f)] public float holeDepth = 6f;

        [Header("Circulation")]
        [Tooltip("Demi-largeur des voies dégagées entre les entrées (m) : rien sous 10 m de haut (le traqueur passe).")]
        [Min(2f)] public float laneHalfWidth = 5f;
        [Tooltip("Zone de parcours (non magnétique) : seul endroit où l'on peut contourner le traqueur.")]
        public bool parkour;

        [Header("Validation (ItHunts > Valider le module)")]
        [Tooltip("Validé et inchangé depuis : seul un module validé entre dans le tirage.")]
        public bool validated;
        [Tooltip("Empreinte du contenu au moment de la validation : si le module change, il doit être validé de nouveau.")]
        public string validatedHash = "";
        [TextArea(3, 20)] public string validationReport = "";

        /// <summary>Taille de la salle (m, x et z, avant rotation) : cases × pas, moins ce qui dépasse de la dernière case.</summary>
        public Vector2 Extent => new Vector2((Mathf.Max(1, footprint.x) - 1) * GridSpacing + size, (Mathf.Max(1, footprint.y) - 1) * GridSpacing + size);

        /// <summary>Nombre de cases le long d'un côté.</summary>
        public int CellsAlong(DoorSide side) => side == DoorSide.Nord || side == DoorSide.Sud ? Mathf.Max(1, footprint.x) : Mathf.Max(1, footprint.y);

        /// <summary>Direction (monde ou locale) vers l'extérieur par ce côté.</summary>
        public static Vector3 SideVector(DoorSide s) => s switch
        {
            DoorSide.Nord => Vector3.forward,
            DoorSide.Est => Vector3.right,
            DoorSide.Sud => Vector3.back,
            _ => Vector3.left,
        };

        /// <summary>Orientation (lacet) qui regarde vers l'extérieur par ce côté.</summary>
        public static float SideYaw(DoorSide side) => side switch { DoorSide.Nord => 0f, DoorSide.Est => 90f, DoorSide.Sud => 180f, _ => 270f };

        /// <summary>Position locale (au sol, sur le mur) de l'entrée d'un côté et d'une case.</summary>
        public Vector3 DoorLocal(DoorSide side, int cell)
        {
            Vector2 e = Extent * 0.5f;
            float along = (cell - (CellsAlong(side) - 1) * 0.5f) * GridSpacing;
            return side switch
            {
                DoorSide.Nord => new Vector3(along, 0f, e.y),
                DoorSide.Sud => new Vector3(along, 0f, -e.y),
                DoorSide.Est => new Vector3(e.x, 0f, along),
                _ => new Vector3(-e.x, 0f, along),
            };
        }

        /// <summary>Entrées permises : celles déclarées, sinon toutes les places (4 côtés de chaque case).</summary>
        public List<(DoorSide side, int cell, bool required)> Doors()
        {
            var list = new List<(DoorSide, int, bool)>();
            foreach (ModuleDoor d in GetComponentsInChildren<ModuleDoor>(true))
                list.Add((d.side, Mathf.Clamp(d.cell, 0, CellsAlong(d.side) - 1), d.required));
            if (list.Count > 0)
                return list;
            foreach (DoorSide s in new[] { DoorSide.Nord, DoorSide.Est, DoorSide.Sud, DoorSide.Ouest })
                for (int c = 0; c < CellsAlong(s); c++)
                    list.Add((s, c, false));
            return list;
        }
    }
}
