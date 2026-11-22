using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Pièces des kits d'art d'un acte (PR #36 à #39, nuit 3) : murs habillés, fenêtres et rayons de lune, lumières, porte de sortie,
    /// repère de la salle de départ, ciel et brume, et la table de remplacement des props greybox par les pièces du kit. Rempli par le
    /// constructeur des scènes (ActSceneBuilder) ; tout est optionnel : sans pièce, le générateur garde ses formes simples.
    /// </summary>
    [Serializable]
    public sealed class ActKit
    {
        [Header("Murs extérieurs (face visible vers +Z, dos contre la roche)")]
        [Tooltip("Pièces de mur posées en file le long des murs extérieurs droits (une au hasard par tronçon), mises à la longueur et à la hauteur.")]
        public List<GameObject> outerWalls = new List<GameObject>();
        [Tooltip("Largeur nominale (m) et hauteur nominale (m) d'une pièce de mur ; profondeur devant le pivot (m) : la face visible.")]
        public float wallPieceWidth = 12f, wallPieceHeight = 10f, wallFrontDepth = 2f;
        [Tooltip("Pièces qui cassent les longs murs (saillies), une tous les … tronçons (0 : jamais).")]
        public GameObject wallAccent;
        [Range(0, 10)] public int wallAccentEvery = 3;
        [Tooltip("Murs entre deux salles (deux faces visibles) : haie, muret… (jardin) ; vide : volume simple aux matériaux de la DA.")]
        public GameObject innerWall;
        public float innerWallWidth = 6f, innerWallHeight = 3f;

        [Header("Fenêtres (acte II)")]
        public GameObject window;
        public GameObject moonRay;
        [Tooltip("Hauteur du centre de la baie au-dessus du sol (m) et avancée du rayon devant le mur (m).")]
        public float windowCenterHeight = 9.5f, rayOffset = 0.6f;

        [Header("Lumières")]
        public GameObject torch;
        public GameObject candles;
        public GameObject chandelier;

        [Header("Portes de sortie, repères")]
        [Tooltip("Porte de sortie de l'acte (encadrement et battants Battant_G / Battant_D qui pivotent).")]
        public GameObject exitDoor;
        [Tooltip("Hauteur de l'ouverture de la porte de sortie (m) : le linteau du mur commence au-dessus.")]
        public float exitDoorOpening = 10f;
        [Tooltip("Pièce du hall de sortie (escalier de descente vers l'arène, acte III).")]
        public GameObject exitHallPiece;
        [Tooltip("Repère de la salle de départ (puits des caves, statue du jardin).")]
        public GameObject startLandmark;
        [Tooltip("Pilier qui porte la voûte des grandes salles (posé au milieu, jamais sur une voie).")]
        public GameObject hallPillar;
        [Tooltip("Ciel (jardin) et brume au sol.")]
        public GameObject sky;
        public GameObject mist;

        [Header("Matériaux par fonction (variété)")]
        public List<FunctionMaterials> functionMaterials = new List<FunctionMaterials>();

        [Header("Props : pièces du kit à la place des props greybox (nom du prop → pièce)")]
        public List<PropSwap> swaps = new List<PropSwap>();
        [Tooltip("Pièces du kit utilisables par nom dans les recettes.")]
        public List<GameObject> pieces = new List<GameObject>();

        public GameObject Swap(string propName)
        {
            foreach (PropSwap s in swaps)
                if (s.prop == propName && s.piece != null)
                    return s.piece;
            return null;
        }

        public GameObject Piece(string name)
        {
            foreach (GameObject p in pieces)
                if (p != null && p.name == name)
                    return p;
            return null;
        }

        public FunctionMaterials MaterialsFor(string function)
        {
            foreach (FunctionMaterials m in functionMaterials)
                if (m.function == function)
                    return m;
            return null;
        }
    }

    [Serializable]
    public sealed class PropSwap
    {
        public string prop;
        public GameObject piece;
    }

    [Serializable]
    public sealed class FunctionMaterials
    {
        public string function;
        public Material wall, floor, ceiling;
    }
}
