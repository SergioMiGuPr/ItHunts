using System.Collections.Generic;
using ItHunts.Level.Acts;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Branche les kits d'art (PR #36 à #39, nuit 3) dans les réglages des actes : pièces de mur, fenêtres et rayons de lune, lumières,
    /// porte de sortie, repères, ciel, matériaux de la bibliothèque selon la fonction des salles, et la table de remplacement des props
    /// greybox par les pièces des kits. Les assets de la DA ne sont pas modifiés (références seulement).
    /// </summary>
    public static class ActKitSetup
    {
        private const string Kits = "Assets/Art/Prefabs/";
        private const string Bib = "Assets/Art/Materials/Bibliotheque/M_Bib_";

        private static GameObject P(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(Kits + path + ".prefab");
        private static Material M(string name) => AssetDatabase.LoadAssetAtPath<Material>(Bib + name + ".mat");

        /// <summary>Matériau du château (PR #47, nuit 4) : M_Chateau_name ; repli sur la bibliothèque (M_Bib_name).</summary>
        private static Material C(string name) =>
            AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Chateau/M_Chateau_" + name + ".mat") ?? M(name);

        private static FunctionMaterials FC(string function, string wall, string floor, string ceiling) =>
            new FunctionMaterials { function = function, wall = C(wall), floor = C(floor), ceiling = C(ceiling) };

        /// <summary>Réapplique les kits d'art aux réglages des 3 actes (sans reconstruire les scènes).</summary>
        [MenuItem("ItHunts/Actes : réappliquer les kits d'art aux réglages")]
        public static void ApplyAll()
        {
            for (int i = 1; i <= 3; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<ActLayoutSettings>("Assets/_Project/Settings/Actes/Act_" + i + ".asset");
                if (s == null)
                    continue;
                Apply(s);
                EditorUtility.SetDirty(s);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Kits d'art réappliqués aux réglages des actes.");
        }

        private static FunctionMaterials F(string function, string wall, string floor, string ceiling) =>
            new FunctionMaterials { function = function, wall = wall != null ? M(wall) : null, floor = floor != null ? M(floor) : null, ceiling = ceiling != null ? M(ceiling) : null };

        private static List<PropSwap> Swaps(params (string prop, string piece)[] pairs)
        {
            var l = new List<PropSwap>();
            foreach ((string prop, string piece) in pairs)
            {
                GameObject p = P(piece);
                if (p != null)
                    l.Add(new PropSwap { prop = prop, piece = p });
            }

            return l;
        }

        private static List<GameObject> All(string folder)
        {
            var l = new List<GameObject>();
            foreach (string g in AssetDatabase.FindAssets("t:Prefab", new[] { Kits + folder }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                if (go != null)
                    l.Add(go);
            }

            return l;
        }

        /// <summary>Kit de l'acte (theme) dans ses réglages ; matériaux par défaut de la bibliothèque.</summary>
        public static void Apply(ActLayoutSettings s)
        {
            var k = new ActKit();
            switch (s.theme)
            {
                case ActTheme.Caves:
                    k.outerWalls = new List<GameObject> { P("KitCaves/Cave_Paroi_Droite_18m"), P("KitCaves/Cave_Paroi_Droite_10m") };
                    k.outerWalls.RemoveAll(x => x == null);
                    k.wallPieceWidth = 12.5f;
                    k.wallPieceHeight = 18.8f;
                    k.wallFrontDepth = 2.1f;
                    k.wallAccent = P("KitCaves/Cave_Paroi_Saillie_10m");
                    k.wallAccentEvery = 4;
                    k.torch = P("KitCaves/Cave_Torche_Murale");
                    k.candles = P("KitCaves/Cave_Candelabre");
                    k.exitDoor = P("KitCaves/Cave_PorteSortie");
                    k.exitDoorOpening = 10.2f;
                    k.startLandmark = P("KitCaves/Cave_Puits");
                    k.hallPillar = P("KitCaves/Cave_PilierRoche_18m");
                    k.pieces = All("KitCaves");
                    k.swaps = Swaps(("Prop_Cage", "KitCaves/Cave_Cellule_Grille"), ("Prop_Grille", "KitCaves/Cave_Cellule_Grille"),
                        ("Prop_Paille", "KitCaves/Cave_Paille"), ("Prop_Chaine", "KitCaves/Cave_Chaines_Mur"), ("Prop_Table", "KitCaves/Cave_TableGarde"),
                        ("Prop_Banc", "KitCaves/Cave_Banc"), ("Prop_Chaudron", "KitCaves/Cave_Brasero"), ("Prop_PileDeCaisses", "KitCaves/Cave_Caisses_Pile"),
                        ("Prop_Etagere", "KitCaves/Cave_Etagere"), ("Prop_Tonneau", "KitCaves/Cave_Tonneaux_Pile"), ("Prop_Lit", "KitCaves/Cave_Lit"),
                        ("Prop_Gravats", "KitCaves/Cave_Eboulis_Petit"), ("Prop_MurEffondre", "KitCaves/Cave_MurBrise_4m"), ("Prop_Lanterne", "KitCaves/Cave_Candelabre"),
                        ("Prop_Caisse", "KitCaves/Cave_Sacs"));
                    k.functionMaterials = new List<FunctionMaterials>
                    {
                        F("Cachot", "Moellons_Salpetre", "Dalles_Sales", "Moellons"), F("Salle des gardes", "Moellons", "Dalles_Sales", "Moellons"),
                        F("Réserve", "Briques_Salpetre", "Dalles_Sales", "Briques"), F("Ossuaire", "RocheCave_Humide", "Terre", "RocheCave"),
                        F("Citerne", "Moellons_Moussus", "Dalles_Sales", "Moellons_Moussus"), F("Gouffre", "RocheCave", "Terre_Racines", "RocheCave"),
                        F("Chapelle souterraine", "PierreTaillee_Salpetre", "Dalles", "PierreTaillee_Sale"), F("Galerie de mine", "RocheCave", "Terre", "RocheCave"),
                        F("Grotte inondée", "RocheCave_Humide", "Terre_Racines", "RocheCave_Humide"), F("Salle de torture", "Briques", "Dalles_Sales", "Briques_Salpetre"),
                        F("Cave à vin", "Briques_Salpetre", "Terre", "Briques"), F("Crypte", "PierreTaillee_Sale", "Dalles", "PierreTaillee_Salpetre"),
                        F("Salle du puits", "Moellons_Moussus", "Dalles_Sales", "RocheCave"),
                    };
                    s.wallMaterial = M("RocheCave");
                    s.floorMaterial = M("Dalles_Sales");
                    s.ceilingMaterial = M("RocheCave");
                    s.trimMaterial = M("PierreTaillee");
                    break;
                case ActTheme.Chateau:
                    k.outerWalls = new List<GameObject> { P("KitChateau/Chateau_Mur_8x20") };
                    k.outerWalls.RemoveAll(x => x == null);
                    k.wallPieceWidth = 8f;
                    k.wallPieceHeight = 20f;
                    k.wallFrontDepth = 0.7f;
                    k.wallAccent = P("KitChateau/Chateau_Fenetre_Haute");
                    k.wallAccentEvery = 2;
                    k.window = P("KitChateau/Chateau_Fenetre_Haute");
                    k.moonRay = P("KitChateau/Chateau_RayonLune");
                    k.windowCenterHeight = 9.5f;
                    k.torch = P("KitCaves/Cave_Applique");
                    k.candles = P("KitCaves/Cave_Candelabre");
                    k.chandelier = P("KitChateau/Chateau_Chandelier");
                    k.exitDoor = P("KitChateau/Chateau_GrandeEntree");
                    k.exitDoorOpening = 13.2f;
                    k.hallPillar = P("KitChateau/Chateau_Pilier_20m");
                    k.pieces = All("KitChateau");
                    k.swaps = Swaps(("Prop_Etagere", "KitChateau/Chateau_Bibliotheque"), ("Prop_Banc", "KitChateau/Chateau_Banc_Eglise"),
                        ("Prop_Autel", "KitChateau/Chateau_Autel"), ("Prop_Table", "KitChateau/Chateau_TableBanquet"), ("Prop_Lit", "KitChateau/Chateau_LitBaldaquin"),
                        ("Prop_Armoire", "KitChateau/Chateau_Armoire_Tombee"), ("Prop_CoffreDeco", "KitChateau/Chateau_Coffre"),
                        ("Prop_Colonne", "KitChateau/Chateau_Colonne_12m"), ("Prop_Lanterne", "KitCaves/Cave_Candelabre"), ("Prop_Ratelier", "KitChateau/Chateau_Lutrin"),
                        ("Prop_Caisse", "KitChateau/Chateau_Chaise_Renversee"));
                    k.functionMaterials = new List<FunctionMaterials>
                    {
                        // Château de l'acte II (PR #47, tableau de l'ART-HANDOFF) : pierre claire, dalles claires, marbre, parquet,
                        // boiseries, plâtre peint au pochoir, tapis.
                        FC("Salle de bal", "PierreClaire", "Marbre", "PlatrePeint"), FC("Salle du trône", "PierreClaire", "Marbre", "PlatrePeint"),
                        FC("Grand escalier", "PierreClaire", "Marbre", "PlatrePeint"),
                        FC("Chapelle", "PierreClaire", "Marbre", "PierreClaire"), FC("Bains", "PierreClaire", "Marbre", "PierreClaire"),
                        FC("Bibliothèque", "Boiserie", "Tapis", "PlatrePeint"),
                        FC("Salle à manger", "Boiserie", "Parquet", "PlatrePeint"), FC("Salon de musique", "Boiserie", "Parquet", "PlatrePeint"),
                        FC("Galerie des portraits", "PlatrePeint", "Parquet", "PlatrePeint"), FC("Chambre", "PlatrePeint", "Tapis", "PlatrePeint"),
                        FC("Armurerie", "PierreClaire", "Dalles", "PierreClaire"), FC("Tour effondrée", "PierreClaire", "Dalles", "PierreClaire"),
                        FC("Palier", "PierreClaire", "Dalles", "PlatrePeint"),
                    };
                    s.wallMaterial = C("PierreClaire");
                    s.floorMaterial = C("Dalles");
                    s.ceilingMaterial = C("PlatrePeint");
                    s.trimMaterial = C("PierreClaire");
                    // Nuit 4 : vraies fenêtres à rayons de lune sur les murs extérieurs, tous les 18 m (les murs du kit n'en portent pas).
                    s.moonWindowSpacing = 18f;
                    break;
                case ActTheme.Jardin:
                    k.outerWalls = new List<GameObject> { P("KitJardin/Jardin_Foret_Lisiere_20m"), P("KitJardin/Jardin_Falaise_16m"), P("KitJardin/Jardin_Foret_Lisiere_20m") };
                    k.outerWalls.RemoveAll(x => x == null);
                    k.wallPieceWidth = 19f;
                    k.wallPieceHeight = 23f;
                    k.wallFrontDepth = 3f;
                    k.wallAccent = P("KitJardin/Jardin_MurEnceinte_8m");
                    k.wallAccentEvery = 5;
                    k.innerWall = P("KitJardin/Jardin_Haie_6m");
                    k.innerWallWidth = 6.9f;
                    k.innerWallHeight = 3f;
                    k.candles = P("KitCaves/Cave_Candelabre");
                    k.exitHallPiece = P("KitJardin/Arene_EscalierDescente");
                    k.startLandmark = P("KitJardin/Jardin_Statue_Pleureuse");
                    k.sky = P("KitJardin/Jardin_Ciel");
                    k.mist = P("KitJardin/Jardin_Brume");
                    k.pieces = All("KitJardin");
                    k.swaps = Swaps(("Prop_Statue", "KitJardin/Jardin_Statue_Pleureuse"), ("Prop_Banc", "KitCaves/Cave_Banc"),
                        ("Prop_Gravats", "KitCaves/Cave_Eboulis_Petit"), ("Prop_ColonneBrisee", "KitJardin/Arene_PilierBrise"), ("Prop_Table", "KitCaves/Cave_TableGarde"));
                    k.functionMaterials = new List<FunctionMaterials>
                    {
                        F("Roseraie", null, "Gravier_Herbe", null), F("Grand bassin", null, "Dalles_Sales", null), F("Labyrinthe de haies", null, "HerbeSeche", null),
                        F("Verger", null, "Terre_Racines", null), F("Cimetière", null, "Terre_Racines", null), F("Serre en ruine", null, "Gravier", null),
                        F("Allée des statues", null, "Gravier", null), F("Kiosque", null, "Gravier_Herbe", null), F("Ravin", null, "Terre", null),
                        F("Potager", null, "Terre", null), F("Bosquet", null, "HerbeSeche", null), F("Fontaine", null, "Dalles_Sales", null),
                        F("Terrasse du château", null, "Dalles_Sales", null),
                    };
                    s.wallMaterial = M("RocheCave");
                    s.floorMaterial = M("Gravier_Herbe");
                    s.trimMaterial = M("PierreTaillee");
                    s.foliageMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Kits/M_Kit_Feuillage.mat") ?? s.foliageMaterial;
                    break;
            }

            s.art = k;
            EditorUtility.SetDirty(s);
        }
    }
}
