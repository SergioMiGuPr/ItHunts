using System.Collections.Generic;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>Décor d'un acte (D-092) : caves (acte I), étages hauts du château (II), jardin (III), arène du boss final.</summary>
    public enum ActTheme
    {
        Caves,
        Chateau,
        Jardin,
        Boss,
    }

    /// <summary>
    /// Réglages d'un acte (D-092, nuit 3) : salles organiques reliées salle à salle (plus de grille ni de couloirs droits), hauteurs,
    /// eau, vide, terrasses, lumière, densité des goules. Un asset par acte ; tout se règle en Play Mode et se relit à la génération
    /// suivante. Les cotes respectent celles du traqueur (14 m debout, 8.5 m baissé, portes ≥ 4 × 5.2 m) et du joueur (hissage
    /// jusqu'à 3.7 m, mur infranchissable à 5.5 m+).
    /// </summary>
    [CreateAssetMenu(fileName = "Act_", menuName = "ItHunts/Réglages/Acte (niveau organique)")]
    public sealed class ActLayoutSettings : ScriptableObject
    {
        public ActTheme theme = ActTheme.Caves;
        [Tooltip("Nom affiché (chargement, écran de fin).")]
        public string displayName = "Acte I : les caves";

        [Header("Grille de tracé (invisible)")]
        [Tooltip("Taille d'une case (m) : les contours des salles suivent cette résolution (biseaux à 45°).")]
        [Range(1f, 4f)] public float cell = 2f;
        [Range(60, 300)] public int gridSize = 220;

        [Header("Salles")]
        [Range(4, 40)] public int rooms = 16;
        [Tooltip("Surface d'une salle (m²), tirée entre ces bornes (les grandes salles plus rares).")]
        public Vector2 roomArea = new Vector2(700f, 2400f);
        [Tooltip("Largeur minimale d'un passage dans une salle (m) : les parties plus étroites sont retirées (traqueur : 2.5 m + marge).")]
        [Range(4f, 16f)] public float minWidth = 10f;
        [Tooltip("Part des nouvelles salles posées au bout du chemin principal (le reste : branches).")]
        [Range(0f, 1f)] public float mainPathShare = 0.4f;
        [Tooltip("Chance d'ouvrir une porte entre deux salles voisines non reliées (boucles : on échappe au traqueur en tournant).")]
        [Range(0f, 1f)] public float loopChance = 0.55f;
        [Tooltip("Salles au moins entre le départ et la sortie.")]
        [Range(2, 20)] public int minStartExit = 6;
        [Tooltip("Sorties au moins par salle de combat (nuit 4 : zéro impasse, règle Pac-Man) : une porte est forcée vers une voisine sinon.")]
        [Range(1, 4)] public int minDoorsPerRoom = 2;
        [Tooltip("Porte entre deux étages : chance d'un palier (terrasse devant la porte, escalier + wall jump) au lieu d'une rampe : portes en hauteur.")]
        [Range(0f, 1f)] public float landingChance = 0.5f;

        [Header("Portes : le traqueur (7 m de large, 8.5 m baissé) passe sans passage spécial (nuit 3)")]
        [Range(4f, 20f)] public float doorWidth = 12f;
        [Range(5.2f, 16f)] public float doorHeight = 9f;

        [Header("Hauteurs")]
        [Tooltip("Hauteur sous plafond (m) : petite salle → grande salle. 0 : à ciel ouvert (jardin).")]
        public Vector2 ceiling = new Vector2(16f, 22f);
        [Tooltip("Pas entre deux étages de sol (m) ; la porte entre deux salles de hauteurs différentes a une rampe.")]
        [Range(0f, 6f)] public float floorStep = 3f;
        [Range(0f, 1f)] public float floorChangeChance = 0.35f;
        [Range(-12f, 0f)] public float minFloor = -6f;
        [Range(0f, 12f)] public float maxFloor = 6f;
        [Tooltip("Pente des rampes entre salles (°).")]
        [Range(10f, 30f)] public float rampSlope = 22f;
        [Tooltip("Hauteur des murs entre deux salles à ciel ouvert (haies, murets : franchissables par traction ou wall jump).")]
        [Range(4f, 20f)] public float openInnerWall = 8f;
        [Tooltip("Hauteur des limites extérieures à ciel ouvert (falaises, forêt dense) : jamais franchissables.")]
        [Range(20f, 120f)] public float openOuterWall = 55f;
        [Tooltip("Au-dessus de cette hauteur, les falaises extérieures ne sont plus magnétiques (la traction ne les escalade pas).")]
        [Range(4f, 40f)] public float outerMagneticTop = 12f;

        [Header("Relief, eau et vide")]
        [Tooltip("Part des salles avec un bassin (eau sans danger : escaliers ou échelles pour sortir), traversé par des ponts.")]
        [Range(0f, 1f)] public float waterShare = 0.25f;
        [Tooltip("Part des salles avec une fosse dans le vide (−voidDamage PV, retour au dernier sol sûr), traversée par des ponts.")]
        [Range(0f, 1f)] public float voidShare = 0.2f;
        [Range(0f, 100f)] public float voidDamage = 20f;
        [Tooltip("Profondeur des bassins (m) : on y nage sans danger.")]
        [Range(1.5f, 6f)] public float waterDepth = 3f;
        [Tooltip("Part des salles avec une terrasse (plateforme haute avec rampe, wall jump et points de traction).")]
        [Range(0f, 1f)] public float terraceShare = 0.6f;
        [Tooltip("Hauteur des terrasses (m) : à portée de la main du traqueur (aucune cachette).")]
        public Vector2 terraceHeight = new Vector2(3.5f, 5.5f);
        [Tooltip("Largeur d'un couloir de wall jump (m) : 3 à 4.5 m (recherche nuit 3).")]
        [Range(2.5f, 6f)] public float wallJumpGap = 3.6f;

        [Header("Lumière (beaucoup de sources, ensemble sombre)")]
        [Tooltip("Une applique tous les … m de mur.")]
        [Range(4f, 40f)] public float sconceSpacing = 11f;
        public Color torchColor = new Color(1f, 0.55f, 0.25f);
        [Range(0f, 10f)] public float torchIntensity = 2.2f;
        [Range(2f, 30f)] public float torchRange = 11f;
        [Tooltip("Lustres (acte II) : par 1000 m² de salle.")]
        [Range(0f, 4f)] public float chandeliersPer1000 = 0f;
        [Tooltip("Fenêtres à rayons de lune sur les murs extérieurs (acte II), tous les … m (0 : aucune).")]
        [Range(0f, 60f)] public float moonWindowSpacing = 0f;
        public Color moonColor = new Color(0.55f, 0.65f, 1f);
        [Tooltip("Lumière directionnelle de la lune (0 : aucune ; jardin).")]
        [Range(0f, 2f)] public float moonlight = 0f;
        [Tooltip("Lumière d'ambiance (multiplie celle de la scène) : acte II un peu plus clair.")]
        [Range(0.2f, 3f)] public float ambientScale = 1f;

        [Header("Population (aucune salle vide)")]
        [Tooltip("Goules habitantes par 100 m² de sol (au moins minGhoulsPerRoom).")]
        [Range(0f, 2f)] public float ghoulsPer100 = 0.22f;
        [Range(0, 10)] public int minGhoulsPerRoom = 2;
        [Range(0, 20)] public int maxGhoulsPerRoom = 7;
        [Tooltip("Volants habitants par salle haute (plafond ≥ 18 m ou ciel ouvert).")]
        [Range(0, 5)] public int flyersPerTallRoom = 1;
        [Range(0, 8)] public int spawnPointsPerRoom = 3;

        [Header("Économie")]
        [Range(0, 30)] public int chests = 9;
        [Range(0, 6)] public int lockedRooms = 2;
        [Range(0, 4)] public int merchants = 1;

        [Header("Traqueur")]
        [Tooltip("Vrai : il entre par la porte d'arrivée (une herse se lève) ; faux : il sort de son repaire.")]
        public bool stalkerFromArrival;

        [Header("Cartes faites à la main (D-110)")]
        [Tooltip("Ambiance propre à l'acte (brouillard, lumière d'ambiance, lune) ; vide : celle de la scène. Acte III : ciel ouvert, brouillard lointain.")]
        public ItHunts.Settings.AmbianceSettings ambiance;
        [Tooltip("Cartes de l'acte (fichiers texte, format docs/design/level/CARTES.md). Non vide : une carte tirée par seed à la place du plan au hasard.")]
        public List<TextAsset> handMaps = new List<TextAsset>();
        [Tooltip("Ensembles de décor de l'acte (D-118) : compositions posées selon la fonction de chaque salle.")]
        public EnsembleLibrary ensembles;

        [Header("Matériaux (formes simples en attendant les kits d'art)")]
        public Material wallMaterial;
        public Material floorMaterial;
        public Material ceilingMaterial;
        public Material trimMaterial;
        public Material waterMaterial;
        public Material propMaterial;
        public Material foliageMaterial;
        [Tooltip("Haies et massifs : remplace le vert uni M_Kit_Feuillage (blocs noirs la nuit) par une matière peinte ; vide : inchangé.")]
        public Material paintedFoliage;
        public Material windowMaterial;

        [Header("Kits d'art de l'acte (PR #36 à #39)")]
        public ActKit art = new ActKit();

        [Tooltip("Paver murs et sols avec les pièces du kit (sinon : volumes simples aux matériaux en coordonnées monde de la DA).")]
        public bool kitPieces;
        [Tooltip("Kit d'art de l'acte (torches, marches des rampes ; pièces si kitPieces) ; vide : formes simples.")]
        public KitSet kit;
        [Tooltip("Prefabs de props par nom (Prop_Table, Prop_Etagere…) : remplacés par les kits d'art quand ils arrivent.")]
        public List<GameObject> props = new List<GameObject>();

        public GameObject Prop(string name)
        {
            foreach (GameObject p in props)
                if (p != null && p.name == name)
                    return p;
            return null;
        }

        public bool Open => ceiling.y <= 0.01f;
    }
}
