using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItHunts.Level.Acts
{
    /// <summary>
    /// Habillage (D-092) : chaque salle a une fonction et ses props suivent cette fonction (barreaux de cellule le long des murs du
    /// cachot, bancs en rangées face à l'autel de la chapelle, étagères de la bibliothèque, tonneaux en rangs de la cave à vin…).
    /// Rien sur les passages, les rampes ni dans les creux ; plus aucun pilier posé au hasard. Lumière : appliques régulières (beaucoup
    /// de sources faibles), lustres (acte II), fenêtres à rayons de lune (acte II), arbres et haies (acte III).
    /// </summary>
    public sealed partial class ActLevel
    {
        /// <summary>Recette d'une fonction : props le long des murs (pas en m), au centre (rangées, groupes), point focal.</summary>
        private sealed class Recipe
        {
            public string[] Wall = new string[0];
            public float WallStep = 6f;
            public float WallChance = 0.8f;
            public string[] Cluster = new string[0];
            public int Clusters;
            public string Row;
            public int Rows;
            public string Focal;
            public string[] FocalSides = new string[0];
            public bool Candles;
            public string[] Hang = new string[0];
            public int Trees;
            public int Hedges;
            public int Tombs;
            public int Planters;
        }

        private static readonly Dictionary<string, Recipe> Recipes = new Dictionary<string, Recipe>
        {
            // Acte I : caves
            ["Cachot"] = new Recipe { Wall = new[] { "Prop_Grille", "Cave_Chaines_Mur", "Prop_Grille", "Cave_Lit" }, WallStep = 6.8f, Cluster = new[] { "Prop_Paille", "Cave_Seau" }, Clusters = 3, Focal = "Cave_Brasero" },
            ["Salle des gardes"] = new Recipe { Wall = new[] { "Prop_Ratelier", "Prop_Tonneau", "Cave_Brasero" }, Cluster = new[] { "Prop_Table", "Cave_Tabouret", "Prop_Chaise_Attrapable" }, Clusters = 3, Focal = "Cave_Brasero" },
            ["Réserve"] = new Recipe { Wall = new[] { "Prop_Etagere", "Prop_PileDeCaisses", "Cave_Sacs" }, WallStep = 4.5f, Cluster = new[] { "Prop_PileDeCaisses", "Prop_Caisse_Attrapable", "Prop_Caisse_Attrapable", "Prop_Tonneau_Attrapable" }, Clusters = 5 },
            ["Ossuaire"] = new Recipe { Wall = new[] { "Prop_Sarcophage", "Prop_Gravats" }, WallStep = 5f, Focal = "Prop_Statue", FocalSides = new[] { "Prop_Sarcophage" }, Candles = true },
            ["Citerne"] = new Recipe { Wall = new[] { "Prop_Tuyau", "Prop_Tonneau" }, WallStep = 9f, Cluster = new[] { "Prop_Tonneau_Attrapable", "Prop_Caisse_Attrapable" }, Clusters = 2 },
            ["Gouffre"] = new Recipe { Wall = new[] { "Prop_Gravats", "Prop_MurEffondre", "Prop_Poutre" }, WallStep = 8f, Cluster = new[] { "Prop_Gravats" }, Clusters = 2 },
            ["Chapelle souterraine"] = new Recipe { Row = "Prop_Banc", Rows = 4, Focal = "Prop_Autel", FocalSides = new[] { "Prop_Statue" }, Candles = true, Wall = new[] { "Prop_Tapisserie" }, WallStep = 9f },
            ["Galerie de mine"] = new Recipe { Wall = new[] { "Prop_Echafaudage", "Prop_Poutre", "Prop_Tonneau" }, WallStep = 7f, Cluster = new[] { "Prop_Caisse_Attrapable", "Prop_PileDeCaisses" }, Clusters = 3 },
            ["Grotte inondée"] = new Recipe { Wall = new[] { "Prop_Gravats", "Prop_ColonneBrisee" }, WallStep = 9f, Cluster = new[] { "Prop_Tonneau_Attrapable" }, Clusters = 2 },
            ["Salle de torture"] = new Recipe { Wall = new[] { "Prop_Cage", "Prop_Ratelier", "Prop_Chaine" }, WallStep = 5.5f, Cluster = new[] { "Prop_Table", "Prop_Chaudron" }, Clusters = 2, Hang = new[] { "Prop_Chaine" } },
            ["Cave à vin"] = new Recipe { Wall = new[] { "Prop_Tonneau", "Prop_Tonneau", "Prop_Etagere" }, WallStep = 3f, Row = "Prop_Tonneau", Rows = 3, Cluster = new[] { "Prop_Tonneau_Attrapable" }, Clusters = 3 },
            ["Crypte"] = new Recipe { Row = "Prop_Sarcophage", Rows = 3, Wall = new[] { "Prop_Statue", "Prop_Tapisserie" }, WallStep = 8f, Focal = "Prop_Autel", Candles = true },
            ["Salle du puits"] = new Recipe { Wall = new[] { "Prop_Tonneau", "Prop_Caisse" }, WallStep = 10f, Cluster = new[] { "Prop_Caisse_Attrapable", "Prop_Tonneau_Attrapable" }, Clusters = 2 },
            // Acte II : château
            ["Salle de bal"] = new Recipe { Focal = "Chateau_Estrade_Musiciens", Wall = new[] { "Prop_Statue", "Chateau_Fauteuil", "Prop_Tapisserie" }, WallStep = 7f },
            ["Bibliothèque"] = new Recipe { Wall = new[] { "Prop_Etagere" }, WallStep = 2.8f, WallChance = 0.95f, Row = "Prop_Etagere", Rows = 3, Cluster = new[] { "Prop_Table", "Prop_Chaise_Attrapable" }, Clusters = 2 },
            ["Chapelle"] = new Recipe { Row = "Prop_Banc", Rows = 5, Focal = "Prop_Autel", FocalSides = new[] { "Chateau_Lutrin", "Prop_Statue" }, Candles = true, Wall = new[] { "Prop_Tapisserie" }, WallStep = 8f },
            ["Salle à manger"] = new Recipe { Row = "Prop_Table", Rows = 2, Wall = new[] { "Prop_Armoire", "Prop_Tapisserie" }, WallStep = 7f, Cluster = new[] { "Prop_Chaise_Attrapable", "Prop_Chaise_Attrapable" }, Clusters = 4, Focal = "Prop_CoffreDeco" },
            ["Galerie des portraits"] = new Recipe { Wall = new[] { "Prop_Tapisserie", "Prop_Statue" }, WallStep = 4.5f, WallChance = 0.95f },
            ["Chambre"] = new Recipe { Focal = "Prop_Lit", FocalSides = new[] { "Chateau_MiroirBrise", "Prop_CoffreDeco" }, Wall = new[] { "Prop_Tapisserie", "Chateau_Fauteuil" }, WallStep = 8f, Cluster = new[] { "Prop_Chaise_Attrapable" }, Clusters = 2 },
            ["Armurerie"] = new Recipe { Wall = new[] { "Prop_Ratelier" }, WallStep = 3.5f, Cluster = new[] { "Prop_Table", "Prop_Caisse_Attrapable" }, Clusters = 3 },
            ["Salle du trône"] = new Recipe { Focal = "Prop_Autel", FocalSides = new[] { "Prop_Statue", "Prop_Statue" }, Wall = new[] { "Prop_Tapisserie", "Prop_Colonne" }, WallStep = 6f, WallChance = 1f, Candles = true },
            ["Bains"] = new Recipe { Wall = new[] { "Prop_Statue", "Prop_Tonneau" }, WallStep = 8f, Cluster = new[] { "Prop_Banc" }, Clusters = 2 },
            ["Tour effondrée"] = new Recipe { Wall = new[] { "Prop_Gravats", "Prop_MurEffondre" }, WallStep = 7f, Cluster = new[] { "Prop_Poutre", "Prop_Gravats" }, Clusters = 2 },
            ["Grand escalier"] = new Recipe { Wall = new[] { "Prop_Statue", "Prop_Tapisserie" }, WallStep = 6f },
            ["Salon de musique"] = new Recipe { Cluster = new[] { "Prop_Banc", "Prop_Chaise_Attrapable", "Prop_Table" }, Clusters = 3, Wall = new[] { "Prop_Tapisserie", "Prop_Etagere" }, WallStep = 7f },
            ["Palier"] = new Recipe { Wall = new[] { "Prop_Tapisserie", "Prop_Statue" }, WallStep = 8f },
            // Acte III : jardin
            ["Roseraie"] = new Recipe { Planters = 6, Hedges = 2, Trees = 2 },
            ["Grand bassin"] = new Recipe { Focal = "Prop_Statue", Trees = 4, Wall = new[] { "Prop_Banc" }, WallStep = 12f },
            ["Labyrinthe de haies"] = new Recipe { Hedges = 9 },
            ["Verger"] = new Recipe { Trees = 14 },
            ["Cimetière"] = new Recipe { Tombs = 18, Trees = 3, Focal = "Jardin_Caveau", Candles = true },
            ["Serre en ruine"] = new Recipe { Focal = "Jardin_Serre_Ruine", Planters = 4, Wall = new[] { "Prop_ColonneBrisee", "Prop_Table" }, WallStep = 6f, Cluster = new[] { "Prop_Tonneau_Attrapable", "Prop_Caisse_Attrapable" }, Clusters = 2 },
            ["Allée des statues"] = new Recipe { Row = "Prop_Statue", Rows = 2, Trees = 4 },
            ["Kiosque"] = new Recipe { Focal = "Jardin_Kiosque", Trees = 4, Hedges = 2 },
            ["Ravin"] = new Recipe { Trees = 5, Cluster = new[] { "Prop_Gravats" }, Clusters = 2 },
            ["Potager"] = new Recipe { Planters = 9, Cluster = new[] { "Prop_Tonneau_Attrapable", "Prop_Caisse_Attrapable" }, Clusters = 2 },
            ["Bosquet"] = new Recipe { Trees = 10, Hedges = 1 },
            ["Fontaine"] = new Recipe { Focal = "Jardin_Fontaine", Hedges = 4, Wall = new[] { "Prop_Banc" }, WallStep = 9f },
            ["Terrasse du château"] = new Recipe { Wall = new[] { "Prop_Statue", "Prop_Banc" }, WallStep = 10f, Trees = 2 },
            ["Arène"] = new Recipe(),
        };

        private readonly List<Vector3> _chandeliers = new List<Vector3>();
        private int _decorOrder;

        /// <summary>Étiquette d'audit du décor (D-115) : rôle de l'élément et ordre de pose (le plus récent part en cas de doublon).</summary>
        private GameObject Tag(GameObject go, DecorKind kind)
        {
            if (go == null)
                return null;
            DecorTag t = go.GetComponent<DecorTag>() ?? go.AddComponent<DecorTag>();
            t.kind = kind;
            t.order = _decorOrder++;
            return go;
        }

        private void Dress(ActRoom r)
        {
            Transform root = Group("Habillage", _roomRoots[r.Id]);
            Sconces(r, root);
            if (r.Role == RoomRole.HallDeSortie)
                DressExitHall(r, root);
            if (r.Special)
                return;
            Recipes.TryGetValue(r.Function ?? "", out Recipe rec);
            rec ??= new Recipe();
            List<ActContour.Segment> segs = _contours[r.Id];
            // Point focal : le mur droit le plus long, loin de la première porte.
            ActContour.Segment? focalWall = null;
            Vector3 entry = r.Doors.Count > 0 ? Plan.DoorCenter(Plan.Doors[r.Doors[0]]) : Plan.CellCenter(r.CenterCell, r.Floor);
            float best = 0f;
            foreach (ActContour.Segment s in segs)
            {
                if (s.Diagonal || s.Length < 3f)
                    continue;
                float score = s.Length * Plan.Cell + Vector3.Distance(World((s.A + s.B) * 0.5f, r.Floor), entry) * 0.6f;
                if (score > best)
                {
                    best = score;
                    focalWall = s;
                }
            }

            HallPillars(r, root);
            if (focalWall.HasValue && !string.IsNullOrEmpty(rec.Focal))
                FocalProps(r, root, focalWall.Value, rec);
            if (rec.Rows > 0 && !string.IsNullOrEmpty(rec.Row) && focalWall.HasValue)
                RowProps(r, root, focalWall.Value, rec);
            // Cartes faites à la main (D-110) : salles 3 à 8 fois plus grandes que celles des recettes ; les quantités suivent la
            // surface (une salle remplie avec soin, pas un champ vide).
            // Revue de la DA (jour 5) : un groupe de props tous les 8 à 10 m, plus de grands sols vides.
            float density = Plan.Hand != null ? Mathf.Clamp(r.Area(Plan.Cell) / 500f, 1f, 20f) : 1f;
            int N(int count) => Mathf.RoundToInt(count * density);
            if (settings.theme == ActTheme.Jardin || rec.Trees > 0)
                for (int k = 0; k < N(rec.Trees); k++)
                    Tree(r, root, 1f);
            bool sets = HasEnsembles(r);
            if (sets)
                Ensembles(r, root, segs);
            else if (rec.Wall.Length > 0)
                WallProps(r, root, segs, rec);
            for (int k = 0; k < (sets ? rec.Clusters : N(rec.Clusters)) && rec.Cluster.Length > 0; k++)
                ClusterProps(r, root, rec.Cluster);
            for (int k = 0; k < (sets ? rec.Hedges / 3 : N(rec.Hedges)); k++)
                Hedge(r, root);
            for (int k = 0; k < (sets ? rec.Tombs / 2 : N(rec.Tombs)); k++)
                Tomb(r, root);
            for (int k = 0; k < (sets ? rec.Planters / 3 : N(rec.Planters)); k++)
                Planter(r, root);
            if (settings.theme == ActTheme.Jardin && Plan.Hand != null)
                Undergrowth(r, root, Mathf.RoundToInt(r.Area(Plan.Cell) / 450f));
            // Revue de la DA : le décor se lit à 20 m ; une source au sol par 450 m² environ (600 au jardin, la lune éclaire), 14 m
            // d'écart au moins. HorrorLook éteint celles qui sont au-delà du brouillard (coût en lumières tenu).
            if (Plan.Hand != null && r.Area(Plan.Cell) > 300f)
                FieldLights(r, root, Mathf.Max(1, Mathf.RoundToInt(r.Area(Plan.Cell) / (settings.theme == ActTheme.Jardin ? 600f : 450f))));
            // Suspendus (chaînes) : seulement sans pièce du kit (les chaînes du kit sont murales) et sous un plafond.
            if (rec.Hang.Length > 0 && r.Ceiling > 0f && (Art == null || Art.Swap(rec.Hang[0]) == null))
                for (int k = 0; k < 4; k++)
                    if (FreeCell(r, out Vector2Int c, clearance: 0))
                    {
                        _occupied.Remove(c);
                        Prop(root, rec.Hang[k % rec.Hang.Length], Plan.CellCenter(c, r.Floor + r.Ceiling - 4f), _rng.Next(360));
                    }

            if (settings.chandeliersPer1000 > 0f && r.Ceiling > 0f)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(r.Area(Plan.Cell) / 1000f * settings.chandeliersPer1000));
                // Lustre proportionné à la salle (nuit 4) : grand dans un hall de 30 m, modeste dans une chambre.
                float chandelierScale = Mathf.Clamp(Mathf.Sqrt(r.Area(Plan.Cell) / 1400f) * (r.Ceiling / 24f), 0.7f, 1.8f);
                // Jamais deux lustres côte à côte : 22 m au moins entre deux (jour 5).
                for (int k = 0; k < n; k++)
                    for (int tries = 0; tries < 6; tries++)
                    {
                        if (!FreeCell(r, out Vector2Int c, clearance: 2))
                            break;
                        Vector3 at = Plan.CellCenter(c, r.Floor);
                        if (_chandeliers.Any(o => (new Vector2(o.x - at.x, o.z - at.z)).magnitude < 22f))
                            continue;
                        _occupied.Remove(c);
                        _chandeliers.Add(at);
                        Chandelier(root, at, r.Ceiling, chandelierScale);
                        break;
                    }
            }

            if (settings.moonWindowSpacing > 0f)
                MoonWindows(r, root, segs);
            // Garde-fou du jardin ouvert : la forêt dense le long des falaises (limite naturelle). Avec le kit (lisière de forêt, falaises),
            // plus de cimes en sphères : elles faisaient de grosses formes noires ellipsoïdales dans le ciel (nuit 4, point 1).
            if (settings.theme == ActTheme.Jardin && (Art == null || Art.outerWalls.Count == 0))
                ForestEdge(r, root, segs);
        }

        /// <summary>
        /// Seconde revue de la DA (D-120) : le hall de sortie des actes I et II n'est plus une salle nue. Ensembles muraux de la fonction
        /// « Hall de sortie » (échafaudages, enfeus, portraits, banquettes…), détails au sol (tapis, gravats), deux sources au sol ; le
        /// milieu reste libre (chemin vers le déclencheur de fin d'acte) et les abords des portes aussi.
        /// </summary>
        private void DressExitHall(ActRoom r, Transform root)
        {
            if (Plan.Hand == null || settings.ensembles == null || settings.theme == ActTheme.Jardin)
                return;
            EnsembleLibrary lib = settings.ensembles;
            HashSet<Vector2Int> ground = EnsembleGround(r);
            var blocked = new HashSet<Vector2Int>();
            foreach (Vector2Int c in r.Use.Keys)
                if (Occupied(c))
                    blocked.Add(c);
            WallEnsembles(r, root, _contours[r.Id], lib, ground, blocked);
            GroundDetails(r, root, lib, blocked);
            foreach (Vector2Int c in blocked)
                _occupied.Add(c);
            FieldLights(r, root, 2);
        }

        // ---------- Props ----------

        private GameObject Prop(Transform root, string name, Vector3 at, float yaw)
        {
            // Pièce du kit de l'acte à la place du prop greybox (table de remplacement), ou pièce du kit appelée par son nom.
            GameObject prefab = Art != null ? Art.Swap(name) ?? Art.Piece(name) : null;
            bool greybox = prefab == null;
            if (prefab == null)
                prefab = settings.Prop(name);
            if (prefab == null)
                return null;
            GameObject go = Instantiate(prefab, at, Quaternion.Euler(0f, yaw, 0f), root);
            go.name = name;
            // Cage et chaîne greybox : suspendues ; leurs pièces du kit (grille de cellule, chaînes murales) se posent au sol ou au mur.
            Tag(go, greybox && (name == "Prop_Cage" || name == "Prop_Chaine") ? DecorKind.Suspendu
                : !greybox && name == "Prop_Chaine" ? DecorKind.Mural : DecorKind.Prop);
            // Le traqueur (14 m) enjambe le mobilier : il ne découpe pas son NavMesh (sinon des îlots hors de sa portée, D-080).
            if (go.GetComponent<ItHunts.Enemies.StalkerPassesThrough>() == null)
                go.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
            return go;
        }

        private bool CellFree(ActRoom r, Vector3 world, bool allowTerrace = false)
        {
            Vector2Int c = Plan.CellOf(world);
            return Plan.OwnerAt(c) == r.Id && r.Use.TryGetValue(c, out CellUse u) && (u == CellUse.Sol || (allowTerrace && u == CellUse.Terrasse)) && !Occupied(c);
        }

        private void Take(Vector3 world) => _occupied.Add(Plan.CellOf(world));

        private void WallProps(ActRoom r, Transform root, List<ActContour.Segment> segs, Recipe rec)
        {
            int i = 0;
            foreach (ActContour.Segment s in segs)
            {
                if (s.Diagonal || s.Length * Plan.Cell < rec.WallStep)
                    continue;
                Vector3 a = World(s.A, r.Floor), b = World(s.B, r.Floor);
                Vector3 inward = -new Vector3(s.Out.x, 0f, s.Out.y).normalized;
                float len = Vector3.Distance(a, b);
                int n = Mathf.FloorToInt(len / rec.WallStep);
                for (int k = 0; k < n; k++)
                {
                    if (_rng.NextDouble() > rec.WallChance)
                        continue;
                    Vector3 p = Vector3.Lerp(a, b, (k + 0.5f) / n) + inward * 1.4f;
                    if (!CellFree(r, p))
                        continue;
                    string name = rec.Wall[i++ % rec.Wall.Length];
                    float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
                    // Tapisseries et grilles : contre le mur, plus haut pour la tapisserie.
                    // Chaînes murales du kit : contre le mur (revue de la DA : plus de chaînes posées à 1.4 m d'un mur).
                    Vector3 at = name == "Prop_Tapisserie" ? p - inward * 0.85f + Vector3.up * 2.5f : name == "Prop_Grille" ? p - inward * 0.5f
                        : name == "Prop_Chaine" && Art != null && Art.Swap(name) != null ? p - inward * 1.3f : p;
                    Prop(root, name, at, yaw);
                    Take(p);
                }
            }
        }

        private void FocalProps(ActRoom r, Transform root, ActContour.Segment wall, Recipe rec)
        {
            Vector3 a = World(wall.A, r.Floor), b = World(wall.B, r.Floor);
            Vector3 inward = -new Vector3(wall.Out.x, 0f, wall.Out.y).normalized;
            Vector3 mid = (a + b) * 0.5f + inward * 3f;
            float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
            if (!CellFree(r, mid))
                return;
            Prop(root, rec.Focal, mid, yaw);
            Take(mid);
            Vector3 along = (b - a).normalized;
            for (int i = 0; i < rec.FocalSides.Length; i++)
            {
                float side = i % 2 == 0 ? 1f : -1f;
                Vector3 p = mid + along * side * (4f + 2f * (i / 2));
                if (CellFree(r, p))
                {
                    Prop(root, rec.FocalSides[i], p, yaw);
                    Take(p);
                }
            }

            if (rec.Candles)
            {
                PointLight(root, mid + Vector3.up * 2.2f + inward * 0.8f, new Color(1f, 0.7f, 0.4f), 2.2f, 9f).name = "Cierges";
                for (int k = -2; k <= 2; k++)
                    Visual(root, "Cierge", CylinderMesh, settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial,
                        mid + along * (k * 0.5f) + inward * 0.9f + Vector3.up * (1.8f + Mathf.Abs(k) * 0.1f), Quaternion.identity, new Vector3(0.12f, 0.3f, 0.12f));
            }
        }

        /// <summary>Rangées parallèles au mur focal (bancs face à l'autel, tables du réfectoire, étagères), avec une allée au milieu.</summary>
        private void RowProps(ActRoom r, Transform root, ActContour.Segment wall, Recipe rec)
        {
            Vector3 a = World(wall.A, r.Floor), b = World(wall.B, r.Floor);
            Vector3 inward = -new Vector3(wall.Out.x, 0f, wall.Out.y).normalized;
            Vector3 along = (b - a).normalized;
            Vector3 mid = (a + b) * 0.5f;
            float yaw = Mathf.Atan2(-inward.x, -inward.z) * Mathf.Rad2Deg;   // face au mur focal
            float spacing = rec.Row == "Prop_Banc" ? 2.6f : rec.Row == "Prop_Etagere" ? 5f : rec.Row == "Prop_Sarcophage" ? 4f : 4f;
            float width = rec.Row == "Prop_Banc" || rec.Row == "Prop_Table" ? 2.8f : rec.Row == "Prop_Statue" ? 7f : 3f;
            for (int row = 0; row < rec.Rows; row++)
            {
                Vector3 line = mid + inward * (7f + row * spacing);
                for (int k = 1; k <= 4; k++)
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Vector3 p = line + along * side * (1.8f + (k - 0.5f) * width);
                        if (!CellFree(r, p))
                            continue;
                        Prop(root, rec.Row, p, rec.Row == "Prop_Etagere" ? yaw + 90f : yaw);
                        Take(p);
                    }
            }
        }

        private void ClusterProps(ActRoom r, Transform root, string[] names)
        {
            if (!FreeCell(r, out Vector2Int c, clearance: 1))
                return;
            Vector3 center = Plan.CellCenter(c, r.Floor);
            float baseYaw = _rng.Next(360);
            for (int i = 0; i < names.Length; i++)
            {
                Vector3 p = center + Quaternion.Euler(0f, baseYaw + i * 360f / names.Length, 0f) * Vector3.forward * (i == 0 ? 0f : 1.6f);
                if (i > 0 && !CellFree(r, p) && Plan.CellOf(p) != c)
                    continue;
                GameObject go = Prop(root, names[i], p + Vector3.up * (names[i].Contains("Attrapable") ? 0.6f : 0f), baseYaw + i * 40f);
                if (go != null)
                    Take(p);
            }
        }

        // ---------- Lumière ----------

        /// <summary>Appliques régulières (beaucoup de sources faibles) sur les murs droits, à 3.4 m, hors des portes.</summary>
        private void Sconces(ActRoom r, Transform root)
        {
            GameObject torchPrefab = Art != null && Art.torch != null ? Art.torch : settings.kit != null ? settings.kit.torche : null;
            if (torchPrefab == null)
                return;
            if (r.Role == RoomRole.Arrivee || r.Role == RoomRole.Repaire)
                return;                                                 // le traqueur attend dans le noir : rien ne le montre avant l'heure (nuit 4)
            float spacing = settings.sconceSpacing * (r.Special ? 1.6f : 1f);
            int i = 0;
            foreach (ActContour.Segment s in _contours[r.Id])
            {
                if (s.Diagonal || s.Length * Plan.Cell < 3.5f)
                    continue;
                Vector3 a = World(s.A, 0f), b = World(s.B, 0f);
                Vector3 inward = -new Vector3(s.Out.x, 0f, s.Out.y).normalized;
                float len = Vector3.Distance(a, b);
                int n = Mathf.Max(1, Mathf.RoundToInt(len / spacing));
                for (int k = 0; k < n; k++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (k + 0.5f) / n);
                    Vector3 cellPoint = p + inward * 1.2f;
                    Vector2Int c = Plan.CellOf(cellPoint);
                    if (Plan.OwnerAt(c) != r.Id || (r.Use.TryGetValue(c, out CellUse u) && (u == CellUse.Passage || u == CellUse.Rampe) && NearDoor(r, cellPoint, 5f)))
                        continue;
                    float ground = GroundAt(r, c);
                    float yaw = Mathf.Atan2(inward.x, inward.z) * Mathf.Rad2Deg;
                    if (settings.theme == ActTheme.Jardin && Art != null && Art.candles != null)
                    {
                        if (k % 2 == 1 || Occupied(c))
                            continue;                                   // au jardin, une source sur deux : la lune éclaire déjà
                        Tag(KitPiece(Art.candles, root, new Vector3(p.x, ground, p.z) + inward * 2.2f, inward, Vector3.one * 1.3f, true), DecorKind.LumiereSol)
                            .AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
                        _occupied.Add(c);
                        continue;
                    }
                    Vector3 at = p + inward * (T * 0.5f + 0.05f) + Vector3.up * (ground + 3.4f);
                    GameObject torch = Art != null && Art.torch != null
                        ? KitPiece(Art.torch, root, at - inward * 0.05f, inward, Vector3.one, true)
                        : KitBuild.Piece(torchPrefab, root, at, yaw);
                    torch.transform.position = at;
                    Tag(torch, DecorKind.Mural);
                    foreach (Light l in torch.GetComponentsInChildren<Light>())
                    {
                        l.color = settings.torchColor;
                        l.intensity = settings.torchIntensity * (0.85f + 0.3f * (float)_rng.NextDouble());
                        l.range = settings.torchRange;
                    }

                    i++;
                }
            }
        }

        private bool NearDoor(ActRoom r, Vector3 p, float distance)
        {
            foreach (int di in r.Doors)
            {
                Vector3 c = Plan.DoorCenter(Plan.Doors[di]);
                if (new Vector2(c.x - p.x, c.z - p.z).sqrMagnitude < distance * distance + Plan.DoorWidth(Plan.Doors[di]) * Plan.DoorWidth(Plan.Doors[di]) * 0.25f)
                    return true;
            }

            return false;
        }

        /// <summary>Lustre (acte II) : couronne de fer, bougies, chaîne jusqu'au plafond, une lumière chaude.</summary>
        private void Chandelier(Transform root, Vector3 floor, float ceiling, float scale = 1f)
        {
            if (KitChandelier(root, floor, ceiling, scale))
                return;
            float y = floor.y + Mathf.Max(8f, ceiling - 7f);
            var go = new GameObject("Lustre");
            go.transform.SetParent(root, false);
            Tag(go, DecorKind.Lustre);
            go.transform.position = new Vector3(floor.x, y, floor.z);
            Material iron = settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial;
            for (int k = 0; k < 10; k++)
            {
                float a = k * Mathf.PI * 0.2f;
                Vector3 p = go.transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 1.6f;
                Visual(go.transform, "Couronne", CubeMesh, iron, p, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f), new Vector3(0.15f, 0.15f, 1.05f));
                Visual(go.transform, "Bougie", CylinderMesh, settings.windowMaterial != null ? settings.windowMaterial : iron, p + Vector3.up * 0.3f, Quaternion.identity,
                    new Vector3(0.1f, 0.25f, 0.1f));
            }

            Visual(go.transform, "Chaîne", CubeMesh, iron, go.transform.position + Vector3.up * ((floor.y + ceiling - y) * 0.5f), Quaternion.identity,
                new Vector3(0.12f, floor.y + ceiling - y, 0.12f));
            PointLight(go.transform, go.transform.position + Vector3.down * 0.6f, new Color(1f, 0.72f, 0.42f), 3.2f, 20f).name = "Lumière du lustre";
        }

        /// <summary>Fenêtres hautes à rayons de lune (acte II) sur les murs extérieurs : vitrail pâle, faisceau qui descend dans la salle.</summary>
        private void MoonWindows(ActRoom r, Transform root, List<ActContour.Segment> segs)
        {
            foreach (ActContour.Segment s in segs)
            {
                if (s.Neighbor >= 0 || s.Diagonal || s.Length * Plan.Cell < 8f)
                    continue;
                Vector3 a = World(s.A, r.Floor), b = World(s.B, r.Floor);
                Vector3 outward = new Vector3(s.Out.x, 0f, s.Out.y).normalized;
                float len = Vector3.Distance(a, b);
                int n = Mathf.Max(1, Mathf.FloorToInt(len / settings.moonWindowSpacing));
                float wh = Mathf.Min(9f, (r.Ceiling > 0f ? r.Ceiling : 16f) * 0.45f);
                for (int k = 0; k < n; k++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (k + 0.5f) / n);
                    Vector3 face = p - outward * (T * 0.5f + 0.06f);
                    if (KitWindow(root, face, -outward, r.Floor))
                        continue;
                    float centerY = r.Floor + (r.Ceiling > 0f ? r.Ceiling * 0.62f : 9f);
                    Quaternion rot = Quaternion.LookRotation(-outward, Vector3.up);
                    Visual(root, "Vitrail", CubeMesh, settings.windowMaterial != null ? settings.windowMaterial : settings.wallMaterial,
                        new Vector3(face.x, centerY, face.z), rot, new Vector3(2.6f, wh, 0.1f));
                    Visual(root, "Meneau", CubeMesh, settings.trimMaterial != null ? settings.trimMaterial : settings.wallMaterial,
                        new Vector3(face.x, centerY, face.z) - outward * 0.05f, rot, new Vector3(0.25f, wh + 0.4f, 0.2f));
                    // Rayon : projecteur froid incliné vers le sol de la salle.
                    var lgo = new GameObject("Rayon de lune");
                    lgo.transform.SetParent(root, false);
                    lgo.transform.position = new Vector3(face.x, centerY + wh * 0.3f, face.z) - outward * 0.5f;
                    lgo.transform.rotation = Quaternion.LookRotation((-outward * 1f + Vector3.down * 1.3f).normalized, Vector3.up);
                    var l = lgo.AddComponent<Light>();
                    l.type = LightType.Spot;
                    l.spotAngle = 34f;
                    l.innerSpotAngle = 18f;
                    l.range = 28f;
                    l.intensity = 5f;
                    l.color = settings.moonColor;
                    l.shadows = LightShadows.None;
                    if (_rayMaterial != null)
                    {
                        Visual(root, "Faisceau", CubeMesh, _rayMaterial, lgo.transform.position + lgo.transform.forward * 7f, lgo.transform.rotation,
                            new Vector3(2.2f, 0.4f, 14f), merge: false).transform.localScale = new Vector3(2.4f, 0.05f, 14f);
                    }
                }
            }
        }

        [SerializeField] private Material _rayMaterial;

        // ---------- Jardin ----------

        /// <summary>
        /// Acte III (jour 5, PR #56) : rochers de trois tailles (petit enjambé, moyen hissé, grand au double saut) et sous-bois de
        /// fougères au pied des murs et des arbres, pour que la forêt soit dense sans boucher les allées.
        /// </summary>
        /// <summary>
        /// Cartes à la main (jour 5) : sources posées au sol dans les grandes salles (braseros, candélabres), pas seulement au pied des
        /// murs, pour que le centre d'une salle de 100 m ne soit plus un trou noir ; chacune garde au moins 18 m d'écart avec les autres.
        /// </summary>
        private void FieldLights(ActRoom r, Transform root, int count)
        {
            // Caves : braseros ; château et jardin : candélabres.
            GameObject prefab = Art == null ? null : settings.theme == ActTheme.Caves ? Art.Piece("Cave_Brasero") ?? Art.candles : Art.candles;
            if (prefab == null)
                return;
            var placed = new List<Vector3>();
            for (int k = 0; k < count * 3 && placed.Count < count; k++)
            {
                if (!FreeCell(r, out Vector2Int c, clearance: 1))
                    break;
                Vector3 at = Plan.CellCenter(c, GroundAt(r, c));
                if (placed.Any(o => (o - at).sqrMagnitude < 14f * 14f))
                {
                    _occupied.Remove(c);
                    continue;
                }

                placed.Add(at);
                GameObject go = KitPiece(prefab, root, at, Quaternion.Euler(0f, _rng.Next(360), 0f) * Vector3.forward,
                    Vector3.one * (prefab == Art.candles ? 1.3f : 1f), true);
                Tag(go, DecorKind.LumiereSol);
                if (go.GetComponent<ItHunts.Enemies.StalkerPassesThrough>() == null)
                    go.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
                foreach (Light l in go.GetComponentsInChildren<Light>())
                {
                    l.color = settings.torchColor;
                    l.intensity = settings.torchIntensity * 1.1f;
                    l.range = settings.torchRange * 1.4f;
                }
            }
        }

        private void Undergrowth(ActRoom r, Transform root, int count)
        {
            string[] rocks = { "Jardin_Rocher_Petit", "Jardin_Rocher_Petit", "Jardin_Rocher_Moyen", "Jardin_Rocher_Grand" };
            string[] ferns = { "Jardin_SousBois_A", "Jardin_SousBois_B" };
            Dictionary<Vector2Int, int> edge = Plan.EdgeDistance(r);
            for (int k = 0; k < count; k++)
            {
                if (!FreeCell(r, out Vector2Int c, clearance: 0))
                    break;
                bool nearWall = edge.TryGetValue(c, out int e) && e <= 2;
                string name = nearWall || _rng.NextDouble() < 0.45 ? ferns[_rng.Next(ferns.Length)] : rocks[_rng.Next(rocks.Length)];
                GameObject prefab = Art != null ? Art.Piece(name) : null;
                if (prefab == null)
                    continue;
                // Rochers du kit : base plane enfoncée de 8 cm par la DA (PR #64, nuit 7) : posés au sol, plus d'enfoncement ici.
                // Fougères : sans collision.
                bool rock = name.Contains("Rocher");
                float scale = 0.85f + 0.3f * (float)_rng.NextDouble();
                GameObject go = KitPiece(prefab, root, Plan.CellCenter(c, r.Floor), Quaternion.Euler(0f, _rng.Next(360), 0f) * Vector3.forward,
                    Vector3.one * scale, rock);
                Tag(go, DecorKind.Vegetal);
                if (rock)
                    go.AddComponent<ItHunts.Enemies.NotWalkable>();
                if (go.GetComponent<ItHunts.Enemies.StalkerPassesThrough>() == null)
                    go.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();   // le traqueur enjambe rochers et fougères
            }
        }

        private readonly List<Vector3> _wallJumps = new List<Vector3>();
        private readonly List<Vector2Int> _trees = new List<Vector2Int>();

        private void Tree(ActRoom r, Transform root, float scale)
        {
            if (!FreeCell(r, out Vector2Int c, clearance: 1))
                return;
            if (Plan.Hand != null && _trees.Any(t => (t - c).sqrMagnitude < 9))
            {
                _occupied.Remove(c);
                return;
            }

            _trees.Add(c);
            // Jamais devant un couloir de wall jump (fuite bouchée, main du traqueur bloquée).
            Vector3 spot = Plan.CellCenter(c, r.Floor);
            if (_wallJumps.Any(w => (w - spot).sqrMagnitude < 14f * 14f))
                return;
            Vector3 at = Plan.CellCenter(c, r.Floor);
            string[] kitTrees = { "Jardin_ArbreMort_Grand", "Jardin_ArbreMort_Penche", "Jardin_ArbreMort_Tordu", "Jardin_GrandArbre_Praticable" };
            GameObject kitTree = Art != null ? Art.Piece(kitTrees[_rng.Next(kitTrees.Length)]) : null;
            if (kitTree != null)
            {
                // Gros arbre mort du kit : son tronc sert au wall jump (collision gardée), le traqueur l'enjambe (NavMesh).
                GameObject t = KitPiece(kitTree, root, at, Quaternion.Euler(0f, _rng.Next(360), 0f) * Vector3.forward, Vector3.one * scale, true);
                t.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
                Tag(t, DecorKind.Vegetal);
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    _occupied.Add(c + new Vector2Int(dx, dy));
                return;
            }

            float h = Mathf.Lerp(9f, 15f, (float)_rng.NextDouble()) * scale;
            float trunk = Mathf.Lerp(1.4f, 2.4f, (float)_rng.NextDouble());
            var go = new GameObject("Arbre");
            go.transform.SetParent(root, false);
            go.transform.position = at;
            var cap = go.AddComponent<CapsuleCollider>();
            cap.radius = trunk * 0.5f;
            cap.height = h;
            cap.center = Vector3.up * h * 0.5f;
            Material bark = settings.propMaterial != null ? settings.propMaterial : settings.wallMaterial;
            Material leaves = settings.foliageMaterial != null ? settings.foliageMaterial : settings.wallMaterial;
            Visual(go.transform, "Tronc", CylinderMesh, bark, at + Vector3.up * h * 0.5f, Quaternion.Euler(0f, _rng.Next(360), (float)_rng.NextDouble() * 4f), new Vector3(trunk, h * 0.5f, trunk));
            for (int k = 0; k < 3; k++)
            {
                float s = Mathf.Lerp(5f, 8f, (float)_rng.NextDouble()) * scale;
                Vector3 off = new Vector3((float)_rng.NextDouble() - 0.5f, 0f, (float)_rng.NextDouble() - 0.5f) * 3f;
                Visual(go.transform, "Feuillage", SphereMesh, leaves, at + Vector3.up * (h * 0.85f + k * 1.2f) + off, Quaternion.identity, new Vector3(s, s * 0.75f, s));
            }

            _occupied.Add(c + Vector2Int.right);
            _occupied.Add(c + Vector2Int.up);
            _occupied.Add(c - Vector2Int.right);
            _occupied.Add(c - Vector2Int.up);
        }

        /// <summary>Haie basse (2.4 m : on la franchit par un saut + hissage) : un muret de verdure qui oriente la course.</summary>
        private void Hedge(ActRoom r, Transform root)
        {
            if (!FreeCell(r, out Vector2Int c, clearance: 2))
                return;
            Vector2Int d = _rng.Next(2) == 0 ? Vector2Int.right : Vector2Int.up;
            int len = 3 + _rng.Next(3);
            for (int k = 1; k < len; k++)
            {
                Vector2Int q = c + d * k;
                if (!r.Use.TryGetValue(q, out CellUse u) || u != CellUse.Sol || Occupied(q))
                {
                    len = k;
                    break;
                }
            }

            if (len < 2)
                return;
            for (int k = 0; k < len; k++)
                _occupied.Add(c + d * k);
            Vector3 a = Plan.CellCenter(c, r.Floor), b = Plan.CellCenter(c + d * (len - 1), r.Floor);
            Vector3 mid = (a + b) * 0.5f + Vector3.up * 1.2f;
            float yaw = d.x != 0 ? 90f : 0f;
            GameObject hedge = Solid(root, "Haie", mid, new Vector3(1.4f, 2.4f, (len - 1) * Plan.Cell + 1.4f), yaw, KitBuild.Kind.Bloc,
                settings.foliageMaterial != null ? settings.foliageMaterial : settings.wallMaterial);
            hedge.AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
            if (Art != null && Art.innerWall != null)
            {
                foreach (MeshRenderer mr in hedge.GetComponentsInChildren<MeshRenderer>())
                    mr.enabled = false;
                Vector3 axis = d.x != 0 ? Vector3.right : Vector3.forward;
                float hl = (len - 1) * Plan.Cell + 1.4f;
                KitPiece(Art.innerWall, root, new Vector3(mid.x, r.Floor, mid.z), new Vector3(-axis.z, 0f, axis.x),
                    new Vector3(hl / Art.innerWallWidth, 2.4f / Art.innerWallHeight, 0.8f));
            }
        }

        private void Tomb(ActRoom r, Transform root)
        {
            if (!FreeCell(r, out Vector2Int c, clearance: 0))
                return;
            Vector3 at = Plan.CellCenter(c, r.Floor);
            float yaw = _rng.Next(-8, 9);
            GameObject kitTomb = Art != null ? Art.Piece(_rng.Next(3) == 0 ? "Jardin_Tombe_Croix" : "Jardin_Tombe_Stele") : null;
            if (kitTomb != null)
            {
                Tag(KitPiece(kitTomb, root, at, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, Vector3.one, true), DecorKind.Vegetal).AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
                return;
            }

            Solid(root, "Tombe", at + Vector3.up * 0.7f, new Vector3(0.9f, 1.4f, 0.25f), yaw, KitBuild.Kind.Bloc, settings.trimMaterial)
                .AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
            Visual(root, "Dalle", CubeMesh, settings.floorMaterial != null ? settings.floorMaterial : settings.wallMaterial, at + new Vector3(0f, 0.05f, 1.1f),
                Quaternion.Euler(0f, yaw, 0f), new Vector3(1f, 0.1f, 2f));
        }

        private void Planter(ActRoom r, Transform root)
        {
            if (!FreeCell(r, out Vector2Int c, clearance: 1))
                return;
            Vector3 at = Plan.CellCenter(c, r.Floor);
            float yaw = _rng.Next(2) * 90f;
            GameObject massif = settings.ensembles != null && settings.ensembles.Find("Ens_Buis_Boule") != null ? settings.ensembles.Find(_rng.Next(2) == 0 ? "Ens_Buis_Boule" : "Ens_Rosiers") ?? settings.ensembles.Find("Ens_Buis_Boule")
                : Art != null ? Art.Piece("Jardin_Massif") : null;   // massif peint de la DA (le bac du kit faisait un cube)
            if (massif != null)
            {
                Tag(KitPiece(massif, root, at, Quaternion.Euler(0f, yaw, 0f) * Vector3.forward, Vector3.one, true), DecorKind.Vegetal).AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
                return;
            }

            Solid(root, "Jardinière", at + Vector3.up * 0.4f, new Vector3(3.2f, 0.8f, 1.2f), yaw, KitBuild.Kind.Bloc, settings.trimMaterial)
                .AddComponent<ItHunts.Enemies.StalkerPassesThrough>();
            for (int k = -1; k <= 1; k++)
                Visual(root, "Massif", SphereMesh, settings.foliageMaterial != null ? settings.foliageMaterial : settings.wallMaterial,
                    at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(k * 1f, 1.1f, 0f), Quaternion.identity, new Vector3(1.1f, 0.8f, 1.1f));
        }

        /// <summary>Lisière de forêt dense le long des falaises extérieures : on voit la limite, on ne la cherche pas.</summary>
        private void ForestEdge(ActRoom r, Transform root, List<ActContour.Segment> segs)
        {
            Material leaves = settings.foliageMaterial != null ? settings.foliageMaterial : settings.wallMaterial;
            foreach (ActContour.Segment s in segs)
            {
                if (s.Neighbor >= 0 || s.Length * Plan.Cell < 4f)
                    continue;
                Vector3 a = World(s.A, r.Floor), b = World(s.B, r.Floor);
                Vector3 outward = new Vector3(s.Out.x, 0f, s.Out.y).normalized;
                int n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / 5f));
                for (int k = 0; k < n; k++)
                {
                    Vector3 p = Vector3.Lerp(a, b, (k + 0.5f) / n) - outward * 0.2f;
                    float sz = Mathf.Lerp(7f, 11f, (float)_rng.NextDouble());
                    Visual(root, "Cime", SphereMesh, leaves, p + Vector3.up * Mathf.Lerp(12f, 20f, (float)_rng.NextDouble()), Quaternion.identity, new Vector3(sz, sz * 0.8f, sz));
                }
            }
        }
    }
}

