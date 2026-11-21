using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItHunts.Enemies;
using ItHunts.Level;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>Une pièce de la palette de l'atelier : catégorie, nom, prefab « jeu » (collisions, NavMesh, réglages).</summary>
    public sealed class AtelierPiece
    {
        public string Category, Name, PrefabPath;
        /// <summary>Collisions approchées (boîte des bornes) : pas de recette de collisions pour cette pièce d'art.</summary>
        public bool Approximate;
        public GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    /// <summary>
    /// Pièces de l'atelier des modules (2026-11-21). Les pièces « jeu » existantes (kit gothique Kit_*, props Prop_*) sont utilisées
    /// telles quelles ; chaque pièce d'art sans version « jeu » (kit des modules SM_Mod_*, herse, treuil…) reçoit une enveloppe dans
    /// Assets/_Project/Prefabs/Atelier/Pieces : le modèle de la DA (référencé, jamais modifié), posé base au sol et centré, avec les
    /// collisions de sa recette (A2) ou, à défaut, une boîte de ses bornes. Les éléments de gameplay (connecteurs, coffres,
    /// apparitions, passages du traqueur…) sont des prefabs de Assets/_Project/Prefabs/Atelier/Gameplay.
    /// </summary>
    public static partial class ModuleLibrary
    {
        public const string AtelierPieceDir = "Assets/_Project/Prefabs/Atelier/Pieces";
        public const string AtelierGameplayDir = "Assets/_Project/Prefabs/Atelier/Gameplay";
        private const string ArtModelsDir = "Assets/Art/Models/";

        /// <summary>Pièces d'art à envelopper : (catégorie, nom affiché, chemin du FBX sous Assets/Art/Models).</summary>
        private static readonly (string cat, string name, string fbx)[] ArtWraps =
        {
            ("Murs", "Mur grimpable 8 × 10", "Modules/SM_Mod_MurGrimpe_8x10"),
            ("Sols et plafonds", "Sol inondé 8 m", "Modules/SM_Mod_SolInonde_8m"),
            ("Sols et plafonds", "Voûte en ogive 4 m", "Niveau1/SM_Voute_Ogive_4m"),
            ("Piliers et arches", "Arche brisée 8 m", "Modules/SM_Mod_ArcheBrisee_8m"),
            ("Piliers et arches", "Arc de couloir", "Niveau1/SM_Arc_Couloir"),
            ("Portes", "Grande porte 4 × 10", "Modules/SM_Mod_GrandePorte_4x10"),
            ("Portes", "Grande porte 6 × 10", "Modules/SM_Mod_GrandePorte_6x10"),
            ("Portes", "Grande porte 6 × 12", "Modules/SM_Mod_GrandePorte_6x12"),
            ("Portes", "Grande porte 8 × 14", "Modules/SM_Mod_GrandePorte_8x14"),
            ("Portes", "Grande porte 8 × 16", "Modules/SM_Mod_GrandePorte_8x16"),
            ("Portes", "Porte de cellule (barreaux)", "Modules/SM_Mod_PorteCellule"),
            ("Portes", "Herse", "Niveau1/SM_Herse"),
            ("Cellules", "Cellule 4 m", "Modules/SM_Mod_Cellule_4m"),
            ("Escaliers et échelles", "Escalier de pierre 4 m", "Modules/SM_Mod_EscalierPierre_4m"),
            ("Escaliers et échelles", "Escalier de bois 4 m", "Modules/SM_Mod_EscalierBois_4m"),
            ("Escaliers et échelles", "Escalier en spirale (45°)", "Modules/SM_Mod_EscalierSpirale_45"),
            ("Escaliers et échelles", "Échelle 6 m", "Modules/SM_Mod_Echelle_6m"),
            ("Passerelles et ponts", "Passerelle 8 m", "Modules/SM_Mod_Passerelle_8m"),
            ("Passerelles et ponts", "Passerelle sur pilotis 8 m", "Modules/SM_Mod_PasserellePilotis_8m"),
            ("Passerelles et ponts", "Galerie 8 m", "Modules/SM_Mod_Galerie_8m"),
            ("Passerelles et ponts", "Pont de cordes 12 m", "Modules/SM_Mod_PontCordes_12m"),
            ("Balcons et coursives", "Balcon 6 m", "Modules/SM_Mod_Balcon_6m"),
            ("Balcons et coursives", "Coursive en arc (45°)", "Modules/SM_Mod_Coursive_45"),
            ("Plateformes et rebords", "Plateforme de bond 6 m", "Modules/SM_Mod_PlateformeBond_6m"),
            ("Plateformes et rebords", "Rebord 6 m", "Modules/SM_Mod_Rebord_6m"),
            ("Plateformes et rebords", "Plateforme qui tombe", "Niveau1/SM_PlateformeTombe"),
            ("Chaînes, cages, treuil", "Chaîne à grimper 12 m", "Modules/SM_Mod_ChaineGrimpe_12m"),
            ("Chaînes, cages, treuil", "Chaîne tendue 16 m", "Modules/SM_Mod_ChaineTendue_16m"),
            ("Chaînes, cages, treuil", "Treuil", "Niveau1/SM_Treuil"),
            ("Tuyaux et eau", "Gros tuyau", "Modules/SM_Mod_TuyauGros"),
            ("Tuyaux et eau", "Roue à eau 8 m", "Modules/SM_Mod_RoueEau_8m"),
            ("Props", "Lits superposés", "Modules/SM_Mod_LitsSuperposes"),
            ("Props", "Râtelier (modules)", "Modules/SM_Mod_Ratelier"),
            ("Props", "Table (modules)", "Modules/SM_Mod_Table"),
            ("Props", "Tonneau (modules)", "Modules/SM_Mod_Tonneau"),
            ("Props", "Levier", "Niveau1/SM_Levier"),
        };

        /// <summary>Kit gothique (Kit_*) : modèles sans collisions (le générateur les fusionne) ; enveloppes « jeu » avec collisions.</summary>
        private static readonly (string cat, string name, string kit)[] KitWraps =
        {
            ("Murs", "Mur 4 × 4", "Kit_Mur4x4"),
            ("Murs", "Mur 4 × 3", "Kit_Mur4x3"),
            ("Murs", "Mur 2 × 4", "Kit_Mur2x4"),
            ("Murs", "Mur à arcade 8 m", "Kit_Mur_Arcade_8m"),
            ("Murs", "Mur à niche 8 m", "Kit_Mur_Niche_8m"),
            ("Sols et plafonds", "Sol 4 m", "Kit_Sol4"),
            ("Sols et plafonds", "Voûte en ogive 8 m", "Kit_Voute_Ogive_8m"),
            ("Sols et plafonds", "Poutre", "Kit_Poutre"),
            ("Piliers et arches", "Pilier", "Kit_Pilier"),
            ("Piliers et arches", "Colonne engagée 8 m", "Kit_Colonne_Engagee_8m"),
            ("Piliers et arches", "Arc", "Kit_Arc"),
            ("Piliers et arches", "Arc doubleau 14 m", "Kit_ArcDoubleau_14m"),
            ("Portes", "Grande porte (kit)", "Kit_GrandePorte"),
            ("Portes", "Porte de pièce", "Kit_PortePiece"),
            ("Escaliers et échelles", "Escalier (kit)", "Kit_Escalier"),
            ("Passerelles et ponts", "Pont (kit)", "Kit_Pont"),
            ("Passerelles et ponts", "Pont d'égout 4 m", "Kit_PontEgout4"),
            ("Passerelles et ponts", "Pont d'égout 14 m", "Kit_PontEgout14"),
        };

        /// <summary>Pièces « jeu » existantes (props : collisions comprises), utilisées telles quelles.</summary>
        private static readonly (string cat, string name, string path)[] GamePieces =
        {
            ("Murs", "Mur effondré (brisé)", "Props/Prop_MurEffondre"),
            ("Piliers et arches", "Colonne", "Props/Prop_Colonne"), ("Piliers et arches", "Colonne brisée", "Props/Prop_ColonneBrisee"),
            ("Portes", "Grille", "Props/Prop_Grille"),
            ("Cellules", "Cage", "Props/Prop_Cage"),
            ("Plateformes et rebords", "Échafaudage", "Props/Prop_Echafaudage"),
            ("Chaînes, cages, treuil", "Chaîne", "Props/Prop_Chaine"), ("Chaînes, cages, treuil", "Cage suspendue", "Props/Prop_Cage"),
            ("Tuyaux et eau", "Tuyau", "Props/Prop_Tuyau"),
            ("Torches et lumières", "Torche", "Props/Prop_Torche"), ("Torches et lumières", "Torche éteinte", "Props/Prop_TorcheEteinte"),
            ("Torches et lumières", "Lanterne", "Props/Prop_Lanterne"),
            ("Props", "Tonneau", "Props/Prop_Tonneau"), ("Props", "Table", "Props/Prop_Table"), ("Props", "Lit", "Props/Prop_Lit"),
            ("Props", "Râtelier", "Props/Prop_Ratelier"), ("Props", "Caisse", "Props/Prop_Caisse"), ("Props", "Pile de caisses", "Props/Prop_PileDeCaisses"),
            ("Props", "Armoire", "Props/Prop_Armoire"), ("Props", "Autel", "Props/Prop_Autel"), ("Props", "Banc", "Props/Prop_Banc"),
            ("Props", "Chaudron", "Props/Prop_Chaudron"), ("Props", "Coffre (décor)", "Props/Prop_CoffreDeco"), ("Props", "Étagère", "Props/Prop_Etagere"),
            ("Props", "Gravats", "Props/Prop_Gravats"), ("Props", "Paille", "Props/Prop_Paille"), ("Props", "Sarcophage", "Props/Prop_Sarcophage"),
            ("Props", "Statue", "Props/Prop_Statue"), ("Props", "Tapisserie", "Props/Prop_Tapisserie"),
            ("Gameplay", "Caisse attrapable", "Props/Prop_Caisse_Attrapable"), ("Gameplay", "Tonneau attrapable", "Props/Prop_Tonneau_Attrapable"),
            ("Gameplay", "Chaise attrapable", "Props/Prop_Chaise_Attrapable"),
        };

        /// <summary>Demandées mais sans pièce dans le projet (rapport de l'atelier).</summary>
        public static readonly string[] MissingPieces =
        {
            "mur avec fenêtre", "murs de plusieurs hauteurs (seulement 3 et 4 m, à empiler)", "cheminée",
            "portes de 10 à 16 m en 4 m de large (seulement 4 × 10)", "chute d'eau dédiée (plan vertical avec le shader d'eau : uv xz, figée)",
        };

        private static readonly (string name, string path)[] GameplayPrefabs =
        {
            ("Connecteur (entrée vers un couloir)", "G_Connecteur"), ("Zone limite du module", "G_ZoneLimite"),
            ("Coffre facile", "G_Coffre_Facile"), ("Coffre moyen", "G_Coffre_Moyen"), ("Coffre difficile", "G_Coffre_Difficile"),
            ("Marchand", "G_Marchand"), ("Salle verrouillée", "G_SalleVerrouillee"),
            ("Apparition de goules", "G_Apparition_Goule"), ("Apparition de volants", "G_Apparition_Volant"),
            ("Habitant : goule", "G_Habitant_Goule"), ("Habitant : volant", "G_Habitant_Volant"),
            ("Arrivée du traqueur", "G_Arrivee_Traqueur"),
            ("Passage : bond", "G_Passage_Bond"), ("Passage : escalade de rebord", "G_Passage_EscaladeRebord"),
            ("Passage : mur grimpable", "G_Passage_MurGrimpe"), ("Passage : chaîne à grimper", "G_Passage_ChaineGrimpe"),
            ("Passage : saut de chaîne", "G_Passage_ChaineSaut"), ("Passage : porte étroite", "G_Passage_PorteEtroite"),
            ("Plaque magnétique (lueur)", "G_PlaqueMagnetique"), ("Eau : surface 8 m", "G_Eau_Surface"), ("Eau : chute", "G_Eau_Chute"),
            ("Lumière (point)", "G_Lumiere"),
        };

        /// <summary>Toutes les pièces de la palette (les enveloppes manquantes sont créées à la demande : GenerateAtelierPieces).</summary>
        public static List<AtelierPiece> AtelierCatalog()
        {
            var list = new List<AtelierPiece>();
            foreach ((string cat, string name, string path) in GamePieces)
                list.Add(new AtelierPiece { Category = cat, Name = name, PrefabPath = $"Assets/_Project/Prefabs/{path}.prefab" });
            foreach ((string cat, string name, string kit) in KitWraps)
                list.Add(new AtelierPiece { Category = cat, Name = name, PrefabPath = $"{AtelierPieceDir}/Kit_Jeu_{kit.Replace("Kit_", "")}.prefab", Approximate = true });
            foreach ((string cat, string name, string fbx) in ArtWraps)
                list.Add(new AtelierPiece
                {
                    Category = cat, Name = name, PrefabPath = $"{AtelierPieceDir}/Kit_Atelier_{Path.GetFileName(fbx).Replace("SM_Mod_", "").Replace("SM_", "")}.prefab",
                    Approximate = !HasRecipe(Path.GetFileName(fbx).Replace("SM_Mod_", "")),
                });
            foreach ((string name, string path) in GameplayPrefabs)
            {
                string cat = name.StartsWith("Eau") ? "Tuyaux et eau" : name.StartsWith("Lumière") ? "Torches et lumières" : "Gameplay";
                list.Add(new AtelierPiece { Category = cat, Name = name, PrefabPath = $"{AtelierGameplayDir}/{path}.prefab" });
            }

            return list.Where(p => File.Exists(p.PrefabPath) || p.PrefabPath.StartsWith(AtelierPieceDir) || p.PrefabPath.StartsWith(AtelierGameplayDir)).ToList();
        }

        private static readonly HashSet<string> Recipes = new HashSet<string>
        {
            "GrandePorte_4x10", "GrandePorte_6x10", "Cellule_4m", "Echelle_6m", "MurGrimpe_8x10", "ChaineGrimpe_12m", "ChaineTendue_16m",
            "PontCordes_12m", "Rebord_6m", "LitsSuperposes", "Balcon_6m", "Ratelier", "PasserellePilotis_8m", "ArcheBrisee_8m", "TuyauGros",
            "RoueEau_8m", "EscalierSpirale_45", "Coursive_45",
        };

        private static bool HasRecipe(string piece) => Recipes.Contains(piece);

        [MenuItem("ItHunts/Atelier : générer les pièces « jeu » et les éléments de gameplay")]
        private static void GenerateMenu() => Debug.Log(GenerateAtelierPieces());

        /// <summary>Crée les enveloppes « jeu » des pièces d'art et les prefabs de gameplay (sans toucher à Assets/Art). Résumé.</summary>
        public static string GenerateAtelierPieces()
        {
            Directory.CreateDirectory(AtelierPieceDir);
            Directory.CreateDirectory(AtelierGameplayDir);
            int wraps = 0, missing = 0;
            var approx = new List<string>();
            var sources = ArtWraps.Select(a => (a.name, piece: Path.GetFileName(a.fbx).Replace("SM_Mod_", ""), path: ArtModelsDir + a.fbx + ".fbx", output: "Kit_Atelier_" + Path.GetFileName(a.fbx).Replace("SM_Mod_", "").Replace("SM_", "")))
                .Concat(KitWraps.Select(k => (k.name, piece: k.kit, path: $"Assets/_Project/Prefabs/Kit/{k.kit}.prefab", output: "Kit_Jeu_" + k.kit.Replace("Kit_", ""))));
            foreach ((string name, string piece, string srcPath, string output) in sources)
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(srcPath);
                if (model == null)
                {
                    missing++;
                    continue;
                }

                var root = new GameObject(output);
                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
                go.name = "Modèle : " + Path.GetFileNameWithoutExtension(srcPath);
                foreach (Collider c in go.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(c);
                Bounds b = RenderBounds(go);
                go.transform.localPosition = new Vector3(-b.center.x, -b.min.y, -b.center.z);   // base au sol, centrée
                if (HasRecipe(piece))
                    KitColliders(go, piece);
                else
                {
                    approx.Add(name);
                    b = RenderBounds(go);
                    var holder = new GameObject("Collision (boîte des bornes)");
                    holder.transform.SetParent(root.transform, false);
                    System.Text.RegularExpressions.Match dim = System.Text.RegularExpressions.Regex.Match(piece, @"GrandePorte_(\d+)x(\d+)");
                    if (dim.Success || piece.Contains("Arc") && !piece.Contains("Arcade") || piece.Contains("GrandePorte") || piece.Contains("PortePiece"))
                    {
                        // Porte ou arc : deux jambages et un linteau, l'ouverture reste libre (largeur × hauteur du nom, ou bornes − 1 m).
                        Object.DestroyImmediate(holder);
                        Vector3 lc = root.transform.InverseTransformPoint(b.center);
                        bool alongX = b.size.x >= b.size.z;
                        float outer = alongX ? b.size.x : b.size.z, depth = alongX ? b.size.z : b.size.x;
                        float open = dim.Success ? float.Parse(dim.Groups[1].Value) : outer - 2f;
                        float high = dim.Success ? float.Parse(dim.Groups[2].Value) : b.size.y * 0.8f;
                        float jamb = Mathf.Max(0.3f, (outer - open) * 0.5f);
                        foreach (float sx in new[] { 1f, -1f })
                        {
                            float off = sx * (open + jamb) * 0.5f;
                            AddBox(root, "jambage", lc + (alongX ? new Vector3(off, 0f, 0f) : new Vector3(0f, 0f, off)) + Vector3.up * (b.size.y * 0.5f - lc.y),
                                alongX ? new Vector3(jamb, b.size.y, depth) : new Vector3(depth, b.size.y, jamb));
                        }

                        float top = b.size.y - high;
                        if (top > 0.1f)
                            AddBox(root, "linteau", new Vector3(lc.x, high + top * 0.5f, lc.z), alongX ? new Vector3(open, top, depth) : new Vector3(depth, top, open));
                    }
                    else if (piece.Contains("Escalier") && b.size.y > 1f)
                    {
                        // Rampe : la diagonale de la boîte, du bas d'un côté au haut de l'autre (le long de la plus grande dimension).
                        bool alongZ = b.size.z >= b.size.x;
                        float len = alongZ ? b.size.z : b.size.x;
                        float ang = Mathf.Atan2(b.size.y, len) * Mathf.Rad2Deg;
                        holder.transform.localPosition = b.center;
                        holder.transform.localRotation = alongZ ? Quaternion.Euler(-ang, 0f, 0f) : Quaternion.Euler(0f, 0f, ang);
                        var box = holder.AddComponent<BoxCollider>();
                        box.size = alongZ ? new Vector3(b.size.x, 0.3f, Mathf.Sqrt(len * len + b.size.y * b.size.y))
                                          : new Vector3(Mathf.Sqrt(len * len + b.size.y * b.size.y), 0.3f, b.size.z);
                    }
                    else
                    {
                        var box = holder.AddComponent<BoxCollider>();
                        box.center = root.transform.InverseTransformPoint(b.center);
                        box.size = b.size;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, $"{AtelierPieceDir}/{root.name}.prefab");
                Object.DestroyImmediate(root);
                wraps++;
            }

            int gameplay = BuildGameplayPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return $"Atelier : {wraps} enveloppes « jeu » ({approx.Count} à collisions approchées : {string.Join(", ", approx)}), " +
                   $"{missing} modèle(s) introuvable(s), {gameplay} éléments de gameplay.";
        }

        private static void AddBox(GameObject root, string name, Vector3 center, Vector3 size)
        {
            var g = new GameObject("Collision : " + name);
            g.transform.SetParent(root.transform, false);
            var box = g.AddComponent<BoxCollider>();
            box.center = center;
            box.size = size;
            g.AddComponent<NotWalkable>();
        }

        private static Bounds RenderBounds(GameObject go)
        {
            Bounds b = default;
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) b = r.bounds;
                else b.Encapsulate(r.bounds);
                any = true;
            }

            return any ? b : new Bounds(go.transform.position, Vector3.one);
        }

        private static int BuildGameplayPrefabs()
        {
            int n = 0;
            void Save(GameObject go, string file)
            {
                PrefabUtility.SaveAsPrefabAsset(go, $"{AtelierGameplayDir}/{file}.prefab");
                Object.DestroyImmediate(go);
                n++;
            }

            GameObject New(string name) => new GameObject(name);

            var door = New("Connecteur");
            door.AddComponent<ModuleDoor>();
            Save(door, "G_Connecteur");
            var zone = New("Zone limite du module");
            zone.AddComponent<ModuleBounds>();
            Save(zone, "G_ZoneLimite");
            for (int d = 0; d < 3; d++)
            {
                var chest = New(new[] { "Coffre facile", "Coffre moyen", "Coffre difficile" }[d]);
                chest.AddComponent<ChestSpot>().difficulty = d;
                Save(chest, new[] { "G_Coffre_Facile", "G_Coffre_Moyen", "G_Coffre_Difficile" }[d]);
            }

            var merchant = New("Marchand");
            merchant.AddComponent<MerchantSpot>();
            Save(merchant, "G_Marchand");
            var locked = New("Salle verrouillée");
            locked.AddComponent<LockedRoomSpot>();
            Save(locked, "G_SalleVerrouillee");
            foreach ((SpawnKinds k, string file) in new[] { (SpawnKinds.Goule, "G_Apparition_Goule"), (SpawnKinds.Volant, "G_Apparition_Volant") })
            {
                var sp = New(k == SpawnKinds.Goule ? "Apparition de goules" : "Apparition de volants");
                var c = sp.AddComponent<SpawnPoint>();
                var so = new SerializedObject(c);
                so.FindProperty("kinds").intValue = (int)k;
                so.ApplyModifiedPropertiesWithoutUndo();
                Save(sp, file);
            }

            foreach (bool flyer in new[] { false, true })
            {
                var res = New(flyer ? "Habitant : volant" : "Habitant : goule");
                res.AddComponent<ResidentSpot>().flyer = flyer;
                Save(res, flyer ? "G_Habitant_Volant" : "G_Habitant_Goule");
            }

            var arrival = New("Arrivée du traqueur");
            arrival.AddComponent<StalkerArrivalSpot>();
            Save(arrival, "G_Arrivee_Traqueur");

            foreach ((StalkerLinkKind k, Vector3 end, string file) in new[]
                     {
                         (StalkerLinkKind.Bond, new Vector3(0f, 0f, 12f), "G_Passage_Bond"),
                         (StalkerLinkKind.EscaladeRebord, new Vector3(0f, 8f, 4f), "G_Passage_EscaladeRebord"),
                         (StalkerLinkKind.MurGrimpe, new Vector3(0f, 10f, 3f), "G_Passage_MurGrimpe"),
                         (StalkerLinkKind.ChaineGrimpe, new Vector3(0f, 12f, 2f), "G_Passage_ChaineGrimpe"),
                         (StalkerLinkKind.ChaineSaut, new Vector3(0f, -6f, 10f), "G_Passage_ChaineSaut"),
                         (StalkerLinkKind.PorteEtroite, new Vector3(0f, 0f, 6f), "G_Passage_PorteEtroite"),
                     })
            {
                var go = New("Passage du traqueur : " + k);
                var p = go.AddComponent<StalkerPassage>();
                p.kind = k;
                var e = new GameObject("Arrivée").transform;
                e.SetParent(go.transform, false);
                e.localPosition = end;
                p.end = e;
                Save(go, file);
            }

            // Plaque magnétique : dalle lumineuse (toute surface répond, celle-ci se voit de loin).
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plaque magnétique";
            plate.transform.localScale = new Vector3(3f, 3f, 0.25f);
            plate.AddComponent<MagneticPlate>();
            var glow = AtelierMaterial("M_Atelier_PlaqueMagnetique", new Color(0.15f, 0.35f, 0.9f), new Color(0.3f, 0.6f, 2.5f));
            plate.GetComponent<Renderer>().sharedMaterial = glow;
            Save(plate, "G_PlaqueMagnetique");

            var water = AssetDatabase.LoadAssetAtPath<Material>(WaterSetup.ToonPath);
            var surf = GameObject.CreatePrimitive(PrimitiveType.Quad);
            surf.name = "Eau : surface 8 m";
            Object.DestroyImmediate(surf.GetComponent<Collider>());
            surf.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            surf.transform.localScale = new Vector3(8f, 8f, 1f);
            var surfRoot = New("Eau : surface 8 m");
            surf.transform.SetParent(surfRoot.transform, false);
            if (water != null)
                surf.GetComponent<Renderer>().sharedMaterial = water;
            Save(surfRoot, "G_Eau_Surface");
            var fall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(fall.GetComponent<Collider>());
            fall.transform.localPosition = new Vector3(0f, 5f, 0f);
            fall.transform.localScale = new Vector3(3f, 10f, 1f);
            var fallRoot = New("Eau : chute");
            fall.transform.SetParent(fallRoot.transform, false);
            if (water != null)
                fall.GetComponent<Renderer>().sharedMaterial = water;
            Save(fallRoot, "G_Eau_Chute");

            var light = New("Lumière");
            var l = light.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 14f;
            l.intensity = 6f;
            l.color = new Color(1f, 0.72f, 0.42f);
            light.transform.localPosition = Vector3.up * 3f;
            Save(light, "G_Lumiere");
            return n;
        }

        /// <summary>Matériau simple de l'atelier (Lit d'URP, émission), dans Assets/_Project/Art/Atelier.</summary>
        public static Material AtelierMaterial(string name, Color baseColor, Color emission)
        {
            const string dir = "Assets/_Project/Art/Atelier";
            Directory.CreateDirectory(dir);
            string path = $"{dir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }

            m.SetColor("_BaseColor", baseColor);
            if (emission.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }

            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
