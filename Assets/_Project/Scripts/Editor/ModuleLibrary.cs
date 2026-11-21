using System;
using System.Collections.Generic;
using System.Linq;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Orbs;
using UnityEditor;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;
using Object = UnityEngine.Object;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Bibliothèque de modules de salle (étape 8, après playtest) : intérieurs faits à la main en prefabs
    /// (Assets/_Project/Prefabs/Modules), que le générateur du niveau pose dans les salles. Les modules « Module_* » sont recréés
    /// par ce constructeur ; tout autre prefab du dossier avec un RoomModule (fait par l'utilisateur) est gardé et tiré aussi.
    /// Règles : voies dégagées en croix (|x| ou |z| ≤ laneHalfWidth, rien sous 10 m), étages pleins jusqu'au sol (pas de
    /// dessous), ponts dont le dessous est à 10 m, décor en bordure et en petits groupes séparés d'au moins 8 m (le traqueur
    /// passe partout et sa main atteint tout point où l'on tient debout).
    /// </summary>
    public static partial class ModuleLibrary
    {
        public const string Dir = "Assets/_Project/Prefabs/Modules";
        /// <summary>Plafond du module en cours (hauteur de son type dans MapLayout_Niveau1) : suspensions sous le plafond.</summary>
        private static float Ceil = 18f;

        private static Transform _m;
        private static float _h;
        private static KitSet _kit;
        private static Material _stone, _floorStone, _wood, _beam, _platform;

        [MenuItem("ItHunts/Reconstruire les modules de salle (Assets/_Project/Prefabs/Modules)")]
        private static void BuildFromMenu() => Debug.Log(BuildAll());

        private static readonly (string id, Action<RoomModule> build)[] Builders =
        {
            ("Etages_Anneau", EtagesAnneau),
            ("Etages_Galeries", EtagesGaleries),
            ("Egouts", Egouts),
            ("Encombree", Encombree),
            ("Haute_Remplie", HauteRemplie),
            ("Parcours_Fosse", ParcoursFosse),
            ("Crypte", Crypte),
            ("Chapelle", Chapelle),
            ("Armurerie", Armurerie),
            ("Grand_Hall_Colonnes", GrandHall),
            ("Cuisine_Reserve", Cuisine),
            ("Salle_De_Garde", SalleDeGarde),
            ("Prison", Prison),
            ("Carrefour_Niches", CarrefourNiches),
            ("Carrefour_Encombre", CarrefourEncombre),
            // Nuit du 2026-11-21 (point 9) : d'après les 4 références de l'utilisateur.
            ("Prison_Etages", PrisonEtages),
            ("Caserne", Caserne),
            ("Citerne_Inondee", Citerne),
            ("Puits_Circulaire", Puits),
            // Modèles vides à dupliquer (poids 0 : jamais tirés tels quels) : 1 × 1, 1 × 2, 2 × 2.
            ("Modele", m => Modele(m, 1, 1)),
            ("Modele_1x2", m => Modele(m, 1, 2)),
            ("Modele_2x2", m => Modele(m, 2, 2)),
        };

        public static string BuildAll()
        {
            PropLibrary.BuildAll();
            return Build(null);
        }

        /// <summary>
        /// Reconstruit seulement ces modules (ids de Builders), sans reconstruire les props (PropLibrary.BuildAll remet les greybox à
        /// la place des modèles de la DA).
        /// </summary>
        public static string BuildOnly(params string[] ids) => Build(new HashSet<string>(ids));

        private static string Build(HashSet<string> only)
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Modules");
            _stone = Mat("Module_Pierre", new Color(0.27f, 0.26f, 0.29f));
            _floorStone = Mat("Module_Dalles", new Color(0.21f, 0.2f, 0.22f));
            _wood = Mat("Prop_Bois", new Color(0.36f, 0.25f, 0.15f));
            _beam = Mat("Prop_BoisSombre", new Color(0.22f, 0.15f, 0.1f));
            _platform = Mat("Greybox_Platform", new Color(0.32f, 0.42f, 0.55f));
            // Structure en pièces du kit (carte aléatoire) : liées à leur prefab, une retouche du kit se répercute dans les modules.
            _kit = TestArenaSceneBuilder.BuildKit();
            TestArenaSceneBuilder.UseEditorSpawner();
            var report = new List<string>();
            int built = 0;
            foreach ((string id, Action<RoomModule> build) in Builders)
            {
                if (only != null && !only.Contains(id))
                    continue;
                built++;
                var root = new GameObject($"Module_{id}");
                _m = root.transform;
                RoomModule mod = root.AddComponent<RoomModule>();
                build(mod);
                if (mod.kind == ModuleKind.Salle && !id.StartsWith("Modele"))
                    Dress(mod, id.GetHashCode());
                Doors(mod);
                string issues = Validate(mod);
                if (issues.Length > 0)
                    report.Add($"{id} : {issues}");
                PrefabUtility.SaveAsPrefabAsset(root, $"{Dir}/Module_{id}.prefab");
                Object.DestroyImmediate(root);
            }

            KitBuild.Spawner = (prefab, parent) => Object.Instantiate(prefab, parent, false);
            AssetDatabase.SaveAssets();
            return $"{built} modules" + (report.Count > 0 ? " ; à vérifier : " + string.Join(" | ", report) : " ; voies dégagées partout");
        }

        /// <summary>Entrées aux ouvertures actuelles : le milieu de chaque côté de chaque case (ModuleDoor, aimantées).</summary>
        private static void Doors(RoomModule mod)
        {
            foreach (DoorSide s in new[] { DoorSide.Nord, DoorSide.Est, DoorSide.Sud, DoorSide.Ouest })
                for (int c = 0; c < mod.CellsAlong(s); c++)
                {
                    var go = new GameObject($"Entrée {s} {c + 1}");
                    go.transform.SetParent(_m, false);
                    go.transform.localPosition = mod.DoorLocal(s, c);
                    go.transform.localRotation = Quaternion.Euler(0f, RoomModule.SideYaw(s), 0f);
                    var d = go.AddComponent<ModuleDoor>();
                    d.side = s;
                    d.cell = c;
                }
        }

        /// <summary>
        /// Modèle de module vide (à dupliquer pour en créer un sans code) : sa taille et ses entrées, quelques piliers du kit hors
        /// des voies, torches, points d'apparition, habitants et un coffre. Poids 0 : jamais tiré tel quel.
        /// </summary>
        private static void Modele(RoomModule m, int fx, int fz)
        {
            Setup(m, fx * fz == 1 ? "Modèle (1 case)" : $"Modèle ({fx} × {fz} cases)", 56f);
            m.footprint = new Vector2Int(fx, fz);
            m.weight = 0f;
            Vector2 e = m.Extent * 0.5f;
            for (int i = 0; i < fx; i++)
            for (int j = 0; j < fz; j++)
            {
                float cx = (i - (fx - 1) * 0.5f) * RoomModule.GridSpacing, cz = (j - (fz - 1) * 0.5f) * RoomModule.GridSpacing;
                Quad((sx, sz, q) =>
                {
                    Pilier(cx + sx * 14f, cz + sz * 14f);
                    Spawn(cx + sx * 18f, cz + sz * 8f);
                    Resident(cx + sx * 9f, cz + sz * 18f);
                });
                Spawn(cx, cz, true);
                if (i == 0 && j == 0)
                    ChestAt(cx + 20f, cz - 20f, 315f);
            }

            for (float a = -e.x + 6f; a <= e.x - 6f; a += 14f)
            {
                P("Torche", a, e.y - 0.6f, 180f, 4f);
                P("Torche", a, -e.y + 0.6f, 0f, 4f);
            }
        }

        /// <summary>Pilier du kit (Kit_Pilier, colonne de 1.4 × 8 m) : collision fusionnée et modèle.</summary>
        private static void Pilier(float x, float z, float y = 0f)
        {
            GameObject col = KitBuild.Box(_m, "Pilier", new Vector3(x, y + 4f, z), new Vector3(1.4f, 8f, 1.4f), KitBuild.Kind.Sol, null);
            KitBuild.Piece(_kit != null ? _kit.pilier : null, col.transform, new Vector3(0f, -4f, 0f), 0f);
        }

        /// <summary>Tous les modules du dossier (les miens et ceux de l'utilisateur).</summary>
        public static List<RoomModule> Load()
        {
            var list = new List<RoomModule>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Dir }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                var m = go != null ? go.GetComponent<RoomModule>() : null;
                if (m != null && m.weight > 0f)
                    list.Add(m);
            }

            return list;
        }

        /// <summary>Contrôle des voies : aucun collider solide dans la croix sous 10 m (sauf les modules à fosse).</summary>
        private static string Validate(RoomModule mod)
        {
            if (mod.hole == ModuleHole.Vide)
                return "";
            float lane = mod.laneHalfWidth;
            Physics.SyncTransforms();                                      // bornes des colliders à jour en mode édition
            var bad = new List<string>();
            foreach (Collider c in _m.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger || c.GetComponentInParent<Grabbable>() != null)
                    continue;
                Bounds b = c.bounds;
                if (b.min.y >= 9.9f)
                    continue;
                bool inX = b.min.x < lane && b.max.x > -lane, inZ = b.min.z < lane && b.max.z > -lane;
                if (inX || inZ)
                    bad.Add(c.name);
            }

            return bad.Count == 0 ? "" : $"{bad.Count} objets dans les voies ({string.Join(", ", bad.Take(4))})";
        }

        private static readonly string[] WallBase = { "Tonneau", "Caisse", "CoffreDeco", "Tonneau", "Gravats", "Caisse_Attrapable", "Tonneau_Attrapable", "Etagere", "Banc" };
        private static readonly string[] FloorClusters = { "PileDeCaisses", "Statue", "Table", "Gravats", "ColonneBrisee", "Chaudron", "Cage" };

        /// <summary>
        /// Habillage commun (après le module) : petits props au pied des murs tous les 4.5 m et groupes au milieu des quadrants,
        /// seulement là où la place est libre (hors des voies, sans chevaucher ce qui existe) ; chaînes et lanternes suspendues.
        /// </summary>
        private static void Dress(RoomModule mod, int seed)
        {
            var rng = new System.Random(seed);
            float lane = mod.laneHalfWidth + 1f;
            Physics.SyncTransforms();
            bool Free(Vector3 c, Vector3 half) =>
                !Physics.CheckBox(c, half, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
            for (float a = lane + 1.5f; a <= _h - 2f; a += 4.5f)
                foreach (float s in new[] { 1f, -1f })
                foreach (int wall in new[] { 0, 1, 2, 3 })
                {
                    if (rng.NextDouble() < 0.25)
                        continue;
                    Vector3 p = wall switch
                    {
                        0 => new Vector3(s * a, 0f, _h - 1.3f),
                        1 => new Vector3(s * a, 0f, -_h + 1.3f),
                        2 => new Vector3(_h - 1.3f, 0f, s * a),
                        _ => new Vector3(-_h + 1.3f, 0f, s * a),
                    };
                    if (mod.hole != ModuleHole.Aucun && mod.holeRect.Contains(new Vector2(p.x, p.z)))
                        continue;
                    if (Mathf.Abs(p.x) > _h - 12f && Mathf.Abs(p.z) > _h - 12f)
                        continue;                                          // pas dans les angles (le traqueur doit pouvoir y venir)
                    if (!Free(p + Vector3.up * 1.2f, new Vector3(2.4f, 1.1f, 2.4f)))
                        continue;
                    float yaw = wall switch { 0 => 180f, 1 => 0f, 2 => -90f, _ => 90f };
                    P(WallBase[rng.Next(WallBase.Length)], p.x, p.z, yaw);
                    Physics.SyncTransforms();
                }

            Quad((sx, sz, q) =>
            {
                for (int i = 0; i < 2; i++)
                {
                    float x = sx * (_h * (0.42f + 0.18f * i)), z = sz * (_h * (0.62f - 0.18f * i));
                    if (Mathf.Abs(x) < lane + 2f || Mathf.Abs(z) < lane + 2f)
                        continue;
                    if (mod.hole != ModuleHole.Aucun && mod.holeRect.Overlaps(new Rect(x - 3f, z - 3f, 6f, 6f)))
                        continue;
                    var c = new Vector3(x, 1.6f, z);
                    if (!Free(c, new Vector3(6f, 1.5f, 6f)))
                        continue;
                    P(FloorClusters[rng.Next(FloorClusters.Length)], x, z, rng.Next(4) * 90f);
                    P(rng.NextDouble() < 0.5 ? "Caisse_Attrapable" : "Tonneau_Attrapable", x + 2.2f * sx, z - 1.5f * sz);
                    Physics.SyncTransforms();
                }

                P("Chaine", sx * _h * 0.45f, sz * _h * 0.3f, 0f, Ceil - 5f);
                P("Lanterne", sx * _h * 0.45f, sz * _h * 0.3f, 0f, Ceil - 5.8f);
            });
        }

        // ---------- Outils ----------

        private static void Setup(RoomModule m, string name, float size, ModuleKind kind = ModuleKind.Salle, ModuleLevel level = ModuleLevel.Tous,
            ModuleShape shape = ModuleShape.Simple)
        {
            m.displayName = name;
            m.size = size;
            m.kind = kind;
            m.level = level;
            m.shape = shape;
            _h = size / 2f;
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutSettings>(TestArenaSceneBuilder.MapLayoutPath);
            Ceil = layout != null ? layout.Ceiling(kind, shape, 0f) : kind == ModuleKind.Salle ? 18f : 16f;
        }

        private static GameObject P(string id, float x, float z, float yaw = 0f, float y = 0f)
        {
            if (id == "Colonne")
            {
                Pilier(x, z, y);                                           // les colonnes sont des piliers du kit
                return null;
            }

            return PropLibrary.Place(id, _m, new Vector3(x, y, z), yaw);
        }

        /// <summary>Bloc plein (étage rempli jusqu'au sol, ou obstacle) de y0 à y0 + sy.</summary>
        private static GameObject Block(string name, float x0, float x1, float z0, float z1, float sy, float y0 = 0f, Material mat = null, bool walkable = true)
        {
            // Pièces du kit : dalle (1 m au plus), mur (1.5 m d'épaisseur au plus), sinon bloc plein (côtés en murs, dessus en dalles).
            var size = new Vector3(Mathf.Abs(x1 - x0), sy, Mathf.Abs(z1 - z0));
            KitBuild.Kind kind = sy <= 1.01f ? KitBuild.Kind.Sol : Mathf.Min(size.x, size.z) <= 1.5f ? KitBuild.Kind.Mur : KitBuild.Kind.Bloc;
            return KitBuild.Box(_m, name, new Vector3((x0 + x1) / 2f, y0 + sy / 2f, (z0 + z1) / 2f), size, kind, _kit, walkable);
        }

        /// <summary>Pont (tablier de 1 m, dessous à 10 m) de a à b (axe x ou z).</summary>
        private static void Bridge(string name, Vector3 a, Vector3 b, float width = 3f)
        {
            Vector3 d = b - a;
            bool alongX = Mathf.Abs(d.x) > Mathf.Abs(d.z);
            Vector3 c = (a + b) / 2f;
            KitBuild.Box(_m, $"Pont : {name}", new Vector3(c.x, 10.5f, c.z), alongX ? new Vector3(Mathf.Abs(d.x), 1f, width) : new Vector3(width, 1f, Mathf.Abs(d.z)), KitBuild.Kind.Pont, _kit);
            P("Lanterne", c.x, c.z, 0f, 9.2f);
        }

        /// <summary>Escalier de marches pleines (0.4 m de haut, 0.56 m de profondeur : on le monte en courant), du sol à top.</summary>
        private static void Stairs(string name, Vector3 start, Vector3 dir, float top, float width = 3f)
        {
            // Même pente qu'avant (0.4 m pour 0.56 m), mais collision lisse (course, glissade et bunny hop comme sur une pente) et
            // marches visuelles (Kit_Escalier), pleine jusqu'au sol.
            const float rise = 0.4f, run = 0.56f;
            int n = Mathf.CeilToInt(top / rise);
            KitBuild.Ramp(_m, name, start, dir, n * run * top / (n * rise), top, width, _kit, top + 1f);
        }

        private static void Spawn(float x, float z, bool flyer = false, float y = 0f)
        {
            var go = new GameObject(flyer ? "Apparition : volant" : "Apparition : goule");
            go.transform.SetParent(_m, false);
            go.transform.localPosition = new Vector3(x, flyer ? (y > 0f ? y : 14f) : y, z);
            var sp = go.AddComponent<SpawnPoint>();
            var so = new SerializedObject(sp);
            so.FindProperty("kinds").intValue = (int)(flyer ? SpawnKinds.Volant : SpawnKinds.Goule);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Resident(float x, float z, float y = 0f)
        {
            var go = new GameObject("Habitant");
            go.transform.SetParent(_m, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.AddComponent<ResidentSpot>();
        }

        private static void ChestAt(float x, float z, float yaw, float y = 0f)
        {
            var go = new GameObject("Emplacement de coffre");
            go.transform.SetParent(_m, false);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.AddComponent<ChestSpot>();
        }

        /// <summary>Torches sur les quatre murs, hors des ouvertures (|le long| > 10 m), à 4 m de haut, tournées vers l'intérieur.</summary>
        private static void WallTorches(float spacing = 14f, float y = 4f)
        {
            for (float a = -_h + 4f; a <= _h - 4f; a += spacing)
            {
                if (Mathf.Abs(a) < 10f)
                    continue;
                P("Torche", a, _h - 0.6f, 180f, y);
                P("Torche", a, -_h + 0.6f, 0f, y);
                P("Torche", _h - 0.6f, a, -90f, y);
                P("Torche", -_h + 0.6f, a, 90f, y);
            }
        }

        /// <summary>Applique f aux quatre quadrants (signes de x et de z).</summary>
        private static void Quad(Action<float, float, int> f)
        {
            int q = 0;
            foreach (float sx in new[] { 1f, -1f })
            foreach (float sz in new[] { 1f, -1f })
                f(sx, sz, q++);
        }

        private static void Common(int ground, int flyers, int residents)
        {
            for (int i = 0; i < ground; i++)
            {
                float a = (i + 0.5f) / ground * Mathf.PI * 2f + 0.4f;
                Spawn(Mathf.Cos(a) * _h * 0.62f, Mathf.Sin(a) * _h * 0.62f);
            }

            for (int i = 0; i < flyers; i++)
            {
                float a = (i + 0.5f) / Mathf.Max(1, flyers) * Mathf.PI * 2f;
                Spawn(Mathf.Cos(a) * _h * 0.4f, Mathf.Sin(a) * _h * 0.4f, true);
            }

            for (int i = 0; i < residents; i++)
            {
                float a = (i + 0.25f) / residents * Mathf.PI * 2f;
                float r = _h * (i % 2 == 0 ? 0.35f : 0.55f);
                Vector2 p = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Mathf.Abs(p.x) < 6f) p.x = Mathf.Sign(p.x == 0f ? 1f : p.x) * 7f;
                if (Mathf.Abs(p.y) < 6f) p.y = Mathf.Sign(p.y == 0f ? 1f : p.y) * 7f;
                Resident(p.x, p.y);
            }
        }

        // ---------- Modules ----------

        /// <summary>
        /// Anneau d'étages : tours d'angle jusqu'au plafond, mezzanines de 5.5 m entre elles, escaliers, ponts à 10 m au-dessus des voies.
        /// Tours montées au plafond (retours du lot, point 5) : leur dessus à 11 m était une cachette, hors de portée de la main du
        /// traqueur (épaule à 1.8 m du centre et 11.1 m de haut, portée 15 m).
        /// </summary>
        private static void EtagesAnneau(RoomModule m)
        {
            Setup(m, "Étages en anneau", 56f, shape: ModuleShape.Etages);
            float h = _h, lane = 5.5f;
            Quad((sx, sz, q) =>
            {
                Block($"Tour d'angle {q}", sx * (h - 9f), sx * h, sz * (h - 9f), sz * h, Ceil);
                Block($"Mezzanine le long de z {q}", sx * (h - 5f), sx * h, sz * lane, sz * (h - 9f), 5.5f);
                Block($"Mezzanine le long de x {q}", sx * lane, sx * (h - 9f), sz * (h - 5f), sz * h, 5.5f);
                Stairs($"Escalier {q}", new Vector3(sx * (h - 5f - 1.5f), 0f, sz * 9f), new Vector3(-sx, 0f, 0f) * 0f + new Vector3(0f, 0f, sz), 5.5f);
                P("PileDeCaisses", sx * (h - 2f), sz * (h - 11f), 0f, 5.5f);
                P("Tonneau", sx * (h - 2f), sz * (h - 14f), 0f, 5.5f);
                P("Caisse_Attrapable", sx * 12f, sz * 12f);
                P("Caisse_Attrapable", sx * 13.2f, sz * 12f);
                P("Tonneau_Attrapable", sx * 12.5f, sz * 13.4f);
                P("Table", sx * 15f, sz * 17f, 90f);
                P("Chaise_Attrapable", sx * 13.5f, sz * 17f);
                P("Banc", sx * 17f, sz * 15.5f);
                P("Tapisserie", sx * (h - 0.2f), sz * 16f, sx > 0 ? -90f : 90f, 6f);
                P("Torche", sx * (h - 5.4f), sz * 16f, sx > 0 ? -90f : 90f, 3f);
            });
            Bridge("nord", new Vector3(-(h - 9f), 0f, h - 4.5f), new Vector3(h - 9f, 0f, h - 4.5f));
            Bridge("sud", new Vector3(-(h - 9f), 0f, -(h - 4.5f)), new Vector3(h - 9f, 0f, -(h - 4.5f)));
            Bridge("est", new Vector3(h - 4.5f, 0f, -(h - 9f)), new Vector3(h - 4.5f, 0f, h - 9f));
            Bridge("ouest", new Vector3(-(h - 4.5f), 0f, -(h - 9f)), new Vector3(-(h - 4.5f), 0f, h - 9f));
            ChestAt(h - 12.5f, h - 2.2f, 180f, 5.5f);                    // sur la mezzanine (la tour monte au plafond)
            ChestAt(-(h - 2.2f), -(h - 12.5f), 90f, 5.5f);
            WallTorches();
            Common(8, 3, 5);
        }

        /// <summary>
        /// Galeries hautes (11 m) à l'est et à l'ouest, reliées par deux ponts ; échafaudages pour monter. Carré d'angle de chaque
        /// galerie (6 × 6 m) monté au plafond (retours du lot, point 5) : c'était une cachette hors de portée du traqueur.
        /// </summary>
        private static void EtagesGaleries(RoomModule m)
        {
            Setup(m, "Galeries hautes", 64f, shape: ModuleShape.Etages);
            float h = _h, lane = 5.5f;
            foreach (float sx in new[] { 1f, -1f })
            {
                Block($"Galerie {sx} nord", sx * (h - 6f), sx * h, lane, h - 6f, 11f);
                Block($"Galerie {sx} sud", sx * (h - 6f), sx * h, -(h - 6f), -lane, 11f);
                Block($"Angle {sx} nord (jusqu'au plafond)", sx * (h - 6f), sx * h, h - 6f, h, Ceil);
                Block($"Angle {sx} sud (jusqu'au plafond)", sx * (h - 6f), sx * h, -h, -(h - 6f), Ceil);
                foreach (float sz in new[] { 1f, -1f })
                {
                    Block($"Mezzanine {sx} {sz}", sx * lane, sx * (h - 6f), sz * (h - 5f), sz * h, 5.5f);
                    P("Echafaudage", sx * (h - 8f), sz * 9f, 90f);
                    P("Echafaudage", sx * 14f, sz * (h - 7f));
                    P("Statue", sx * 12f, sz * 12f);
                    P("Caisse_Attrapable", sx * 18f, sz * 10f);
                    P("Caisse_Attrapable", sx * 19f, sz * 11.2f);
                    P("Tonneau", sx * (h - 1.5f), sz * (h - 8f), 0f, 11f);
                    P("Etagere", sx * (h - 0.5f), sz * 20f, sx > 0 ? -90f : 90f, 11f);
                    P("Lanterne", sx * 14f, sz * 20f, 0f, 9f);
                }
            }

            Bridge("nord", new Vector3(-(h - 6f), 0f, 16f), new Vector3(h - 6f, 0f, 16f));
            Bridge("sud", new Vector3(-(h - 6f), 0f, -16f), new Vector3(h - 6f, 0f, -16f));
            ChestAt(h - 3f, h - 9f, 270f, 11f);                          // hors du carré d'angle (monté au plafond)
            ChestAt(-(h - 3f), -(h - 9f), 90f, 11f);
            WallTorches(12f);
            Common(10, 4, 5);
        }

        /// <summary>Égouts : canal au nord (le générateur le creuse), passerelles, tuyaux, grilles, tonneaux.</summary>
        private static void Egouts(RoomModule m)
        {
            Setup(m, "Égouts", 56f, shape: ModuleShape.Egout);
            m.rotate = false;
            m.hole = ModuleHole.Canal;
            float h = _h;
            m.holeRect = new Rect(-h, h * 0.5f - 3f, 2f * h, 6f);
            m.holeDepth = 4f;
            float zc = h * 0.5f;
            foreach (float sx in new[] { 1f, -1f })
            {
                Block($"Passerelle sud {sx}", sx * 7f, sx * (h - 2f), zc - 5f, zc - 3.5f, 0.4f, 0f, _beam);
                P("Tuyau", sx * (h - 1f), zc + 8f, 0f, 0.4f);
                P("Tuyau", sx * (h - 1f), -10f, 0f, 2f);
                P("Grille", sx * (h - 0.3f), zc, 90f, 0f);
                P("Tonneau", sx * 18f, zc - 7f);
                P("Tonneau", sx * 19.1f, zc - 7.3f);
                P("Tonneau_Attrapable", sx * 15f, -14f);
                P("Caisse_Attrapable", sx * 12f, -20f);
                P("Caisse_Attrapable", sx * 13.2f, -20f);
                P("PileDeCaisses", sx * (h - 3f), -(h - 3f));
                P("Gravats", sx * 15f, -(h - 4f));
                P("Chaine", sx * 10f, zc, 0f, Ceil - 6f);
                P("Lanterne", sx * 16f, zc - 2f, 0f, 6f);
                P("Paille", sx * 20f, zc + 9f);
            }

            ChestAt(h - 4f, h - 3f, 180f);
            P("PileDeCaisses", h - 8f, h - 4f);
            WallTorches();
            Spawn(-16f, -14f); Spawn(16f, -14f); Spawn(-20f, 22f); Spawn(20f, 22f); Spawn(0f, -20f); Spawn(-10f, -8f);
            Spawn(0f, 0f, true); Spawn(12f, zc, true);
            Resident(-12f, -10f); Resident(12f, -10f); Resident(-18f, 22f); Resident(18f, 22f); Resident(8f, -18f);
        }

        /// <summary>Salle encombrée : colonnes, piles, gravats, murs effondrés, beaucoup d'objets à lancer ; voies dégagées.</summary>
        private static void Encombree(RoomModule m)
        {
            Setup(m, "Salle encombrée", 56f);
            float h = _h;
            Quad((sx, sz, q) =>
            {
                P("Colonne", sx * 9f, sz * 9f);
                P("Colonne", sx * 20f, sz * 9f);
                P("Colonne", sx * 9f, sz * 20f);
                P("ColonneBrisee", sx * 20f, sz * 14f, q * 70f);
                P("PileDeCaisses", sx * (h - 2.5f), sz * 14f);
                P("Gravats", sx * 14.5f, sz * (h - 3f));
                P("MurEffondre", sx * (_h - 0.8f), sz * 14f, 90f);
                for (int i = 0; i < 6; i++)
                    P(i % 3 == 0 ? "Tonneau_Attrapable" : i % 3 == 1 ? "Caisse_Attrapable" : "Chaise_Attrapable", sx * (11.5f + (i % 3) * 1.4f), sz * (13f + (i / 3) * 1.4f));
                P("Lanterne", sx * 14.5f, sz * 9f, 0f, 7f);
            });
            ChestAt(h - 2.5f, h - 6f, 270f);
            ChestAt(-(h - 6f), -(h - 2.5f), 0f);
            WallTorches();
            Common(8, 2, 5);
        }

        /// <summary>Salle haute remplie : gradins pleins le long des murs (4 m, 8 m), tours d'angle de 12 m ; aucun vide.</summary>
        private static void HauteRemplie(RoomModule m)
        {
            Setup(m, "Salle haute remplie", 48f, level: ModuleLevel.Haut, shape: ModuleShape.Etages);
            float h = _h, lane = 5.5f;
            Quad((sx, sz, q) =>
            {
                // Tours d'angle jusqu'au plafond (22 m) : leur dessus à 12 m était hors de portée de la main depuis l'épaule visible du
                // traqueur (1.8 m du centre, 11.1 m ; lot art, bloc 4).
                Block($"Tour {q}", sx * (h - 6f), sx * h, sz * (h - 6f), sz * h, 22f);
                Block($"Gradin bas le long de z {q}", sx * (h - 8f), sx * (h - 4f), sz * lane, sz * (h - 6f), 4f);
                Block($"Gradin haut le long de z {q}", sx * (h - 4f), sx * h, sz * lane, sz * (h - 6f), 8f);
                Block($"Gradin bas le long de x {q}", sx * lane, sx * (h - 6f), sz * (h - 8f), sz * (h - 4f), 3.98f);   // croise l'autre gradin bas : 2 cm plus bas (dessus confondus)
                Block($"Gradin haut le long de x {q}", sx * lane, sx * (h - 6f), sz * (h - 4f), sz * h, 8f);
                P("Banc", sx * 10f, sz * (h - 6f), 0f, 4f);
                P("Tonneau", sx * (h - 2f), sz * 10f, 0f, 8f);
                P("Caisse_Attrapable", sx * 10f, sz * 10f);
                P("Caisse_Attrapable", sx * 11.2f, sz * 10f);
                P("Statue", sx * (h - 11f), sz * (h - 11f), 45f);         // au sol : sur la tour, elle cachait le joueur (Valider le module)
            });
            ChestAt(h - 2f, 12f, 270f, 8f);
            WallTorches(10f, 10f);
            Common(6, 3, 4);
        }

        /// <summary>
        /// Salle de parcours : fosse vide de 30 × 30 m au centre (le traqueur en fait le tour), traversée par des poutres (à sa
        /// portée, près des bords) et des plateformes qui ne tiennent qu'un instant (le centre est hors de sa portée) ; non magnétique.
        /// </summary>
        private static void ParcoursFosse(RoomModule m)
        {
            Setup(m, "Parcours : fosse", 64f, level: ModuleLevel.Sol);
            m.hole = ModuleHole.Vide;
            m.holeRect = new Rect(-15f, -15f, 30f, 30f);
            m.holeDepth = 8f;
            m.laneHalfWidth = 4f;
            Material nm = TestArenaSceneBuilder.NonMagneticMat();
            void NM(string name, Vector3 c, Vector3 s, bool crumble)
            {
                GameObject go = Box($"Parcours : {name}", _m, c, s, nm);
                go.AddComponent<NonMagneticSurface>();
                if (crumble)
                {
                    GameObjectUtility.SetStaticEditorFlags(go, 0);
                    go.AddComponent<CrumblePlatform>();
                }
            }

            // Poutres des bords (0.6 m de large, à sa portée) vers les plateformes du centre (qui tombent).
            foreach (float s in new[] { 1f, -1f })
            {
                NM($"poutre x {s}", new Vector3(s * 12.5f, -0.25f, 0f), new Vector3(5f, 0.5f, 0.6f), false);
                NM($"poutre z {s}", new Vector3(0f, -0.25f, s * 12.5f), new Vector3(0.6f, 0.5f, 5f), false);
            }

            float[] steps = { -7f, -3.5f, 0f, 3.5f, 7f };
            foreach (float a in steps)
            {
                if (Mathf.Abs(a) > 0.1f)
                {
                    NM($"plateforme x {a}", new Vector3(a, -0.25f, 0f), new Vector3(2.2f, 0.5f, 2.2f), true);
                    NM($"plateforme z {a}", new Vector3(0f, -0.25f, a), new Vector3(2.2f, 0.5f, 2.2f), true);
                }
            }

            NM("plateforme centrale", new Vector3(0f, -0.25f, 0f), new Vector3(2.4f, 0.5f, 2.4f), true);
            // Panneaux de course au-dessus de la fosse (sur la diagonale).
            NM("panneau nord-est", new Vector3(8f, 4f, 8f), new Vector3(0.6f, 6f, 8f), false);
            NM("panneau sud-ouest", new Vector3(-8f, 4f, -8f), new Vector3(0.6f, 6f, 8f), false);
            Quad((sx, sz, q) =>
            {
                P("Statue", sx * (h() - 2.5f), sz * 10f, sx > 0 ? -90f : 90f);
                P("Tonneau", sx * (h() - 2f), sz * 22f);
                P("Caisse_Attrapable", sx * 22f, sz * 19f);
                P("Grille", sx * 15.5f, sz * 22f, 90f);
                P("Lanterne", sx * 15f, sz * 15f, 0f, 8f);
            });
            ChestAt(_h - 3f, -(_h - 3f), 315f);
            WallTorches();
            Spawn(-22f, 0f); Spawn(22f, 0f); Spawn(0f, 22f); Spawn(0f, -22f); Spawn(20f, 20f); Spawn(-20f, -20f);
            Spawn(0f, 0f, true);
            Resident(-22f, 8f); Resident(22f, -8f); Resident(8f, 22f); Resident(-8f, -22f);
        }

        private static float h() => _h;

        /// <summary>Crypte : rangées de sarcophages espacées, colonnes, statues, chaînes, lumière basse.</summary>
        private static void Crypte(RoomModule m)
        {
            Setup(m, "Crypte", 56f);
            float h = _h;
            Quad((sx, sz, q) =>
            {
                for (int i = 0; i < 2; i++)
                {
                    P("Sarcophage", sx * (10f + i * 9f), sz * 11f);
                    P("Sarcophage", sx * (10f + i * 9f), sz * 20f);
                }

                P("Colonne", sx * 7f, sz * (h - 4f));
                P("Statue", sx * (h - 2.5f), sz * 7.5f, sx > 0 ? -90f : 90f);
                P("Chaine", sx * 14.5f, sz * 15.5f, 0f, Ceil - 6f);
                P("ColonneBrisee", sx * (h - 4f), sz * (h - 4f), q * 40f);
                P("Caisse_Attrapable", sx * 14.5f, sz * 7.5f);
                P("Lanterne", sx * 14.5f, sz * 15.5f, 0f, 6f);
            });
            ChestAt(h - 2f, h - 9f, 270f);
            P("MurEffondre", h - 5f, h - 9f, 90f);
            WallTorches(12f, 3f);
            Common(8, 2, 5);
        }

        /// <summary>Chapelle : bancs en deux blocs, autel et statues, galerie haute le long du mur nord, tapisseries.</summary>
        private static void Chapelle(RoomModule m)
        {
            Setup(m, "Chapelle", 64f);
            float h = _h;
            foreach (float sx in new[] { 1f, -1f })
            {
                for (int r = 0; r < 4; r++)
                    P("Banc", sx * 11f, -10f - r * 2.6f);
                Block($"Galerie {sx}", sx * 5.5f, sx * h, h - 5f, h, 6f);
                Stairs($"Escalier {sx}", new Vector3(sx * (h - 2f), 0f, h - 5f - 9f), new Vector3(0f, 0f, 1f), 6f);
                P("Statue", sx * 9f, h - 8f);
                P("Tapisserie", sx * (h - 0.2f), 14f, sx > 0 ? -90f : 90f, 5f);
                P("Tapisserie", sx * (h - 0.2f), -14f, sx > 0 ? -90f : 90f, 5f);
                P("Colonne", sx * 7f, 8f);
                P("Colonne", sx * 7f, -8f);
                P("Caisse_Attrapable", sx * 22f, -22f);
                P("Chaise_Attrapable", sx * 20f, -24f);
                P("Banc", sx * 16f, h - 2.5f, 0f, 6f);
            }

            P("Autel", 14f, h - 9f);
            P("Lanterne", 14f, h - 9f, 0f, 7f);
            P("CoffreDeco", -14f, h - 9f);
            ChestAt(-(h - 2.5f), -(h - 6f), 90f);
            P("PileDeCaisses", -(h - 6f), -(h - 6f));
            WallTorches();
            Common(10, 3, 5);
        }

        /// <summary>Armurerie : râteliers et étagères le long des murs, tables, tour d'angle de 6 m avec escalier.</summary>
        private static void Armurerie(RoomModule m)
        {
            Setup(m, "Armurerie", 48f);
            float h = _h;
            Quad((sx, sz, q) =>
            {
                P("Ratelier", sx * (h - 0.5f), sz * 10f, sx > 0 ? -90f : 90f);
                P("Ratelier", sx * 12f, sz * (h - 0.5f), sz > 0 ? 180f : 0f);
                P("Table", sx * 12f, sz * 12f);
                P("Chaise_Attrapable", sx * 10.5f, sz * 13.5f);
                P("Caisse_Attrapable", sx * 15f, sz * 9f);
                P("Caisse_Attrapable", sx * 16.2f, sz * 9f);
                P("Etagere", sx * (h - 0.5f), sz * 18f, sx > 0 ? -90f : 90f);
            });
            Block("Tour de guet", h - 8f, h, h - 8f, h, 6f);
            Stairs("Escalier de la tour", new Vector3(h - 9.5f, 0f, h - 8f - 8.4f), new Vector3(0f, 0f, 1f), 6f);
            P("PileDeCaisses", h - 3f, h - 3f, 0f, 6f);
            ChestAt(h - 6f, h - 2f, 180f, 6f);
            WallTorches(10f);
            Common(6, 2, 4);
        }

        /// <summary>Grand hall à colonnes : colonnades au bord des voies, échafaudages, colonnes brisées, statues.</summary>
        private static void GrandHall(RoomModule m)
        {
            Setup(m, "Grand hall à colonnes", 64f);
            float h = _h;
            for (float a = 10f; a < h - 3f; a += 10f)
                foreach (float s in new[] { 1f, -1f })
                foreach (float t in new[] { 1f, -1f })
                {
                    P("Colonne", s * 7f, t * a);
                    P("Colonne", t * a, s * 7f);
                }

            Quad((sx, sz, q) =>
            {
                P("Echafaudage", sx * 17f, sz * 21f);
                P("ColonneBrisee", sx * 14f, sz * 25f, q * 30f);
                P("Statue", sx * (h - 3f), sz * 14f, sx > 0 ? -90f : 90f);
                P("Gravats", sx * 25f, sz * 13f);
                P("Caisse_Attrapable", sx * 15f, sz * 14f);
                P("Tonneau_Attrapable", sx * 16.3f, sz * 14f);
                P("Tapisserie", sx * 20f, sz * (h - 0.2f), sz > 0 ? 180f : 0f, 6f);
            });
            ChestAt(h - 3f, -(h - 9f), 270f);
            P("PileDeCaisses", h - 7f, -(h - 9f));
            WallTorches(12f);
            Common(10, 4, 5);
        }

        /// <summary>Cuisine et réserve : longues tables, tonneaux, étagères, chaudron, lanternes suspendues.</summary>
        private static void Cuisine(RoomModule m)
        {
            Setup(m, "Cuisine et réserve", 48f);
            float h = _h;
            Quad((sx, sz, q) =>
            {
                P("Table", sx * 12f, sz * 10f);
                P("Table", sx * 12f, sz * 15f);
                P("Chaise_Attrapable", sx * 9.5f, sz * 12.5f);
                P("Etagere", sx * (h - 0.5f), sz * 9f, sx > 0 ? -90f : 90f);
                P("Etagere", sx * (h - 0.5f), sz * 13f, sx > 0 ? -90f : 90f);
                P("Tonneau", sx * (h - 1.5f), sz * (h - 1.5f));
                P("Tonneau", sx * (h - 3f), sz * (h - 1.5f));
                P("Tonneau", sx * (h - 1.5f), sz * (h - 3f));
                P("Tonneau_Attrapable", sx * 17f, sz * 19f);
                P("Caisse_Attrapable", sx * 18.5f, sz * 19f);
                P("Lanterne", sx * 12f, sz * 12.5f, 0f, 6f);
            });
            P("Chaudron", 16f, -6.5f);
            ChestAt(-(h - 2f), h - 6f, 90f);
            P("PileDeCaisses", -(h - 6f), h - 6f);
            WallTorches(10f);
            Common(6, 2, 4);
        }

        /// <summary>Salle de garde : deux tours de 6 m avec escaliers, pont à 10 m entre elles, lits, râteliers.</summary>
        private static void SalleDeGarde(RoomModule m)
        {
            Setup(m, "Salle de garde", 56f, shape: ModuleShape.Etages);
            float h = _h;
            foreach (float s in new[] { 1f, -1f })
            {
                Block($"Tour {s}", s * (h - 10f), s * h, s * (h - 10f), s * h, 11f);
                // Escalier jusqu'à 8.5 m (il reste hors des voies), puis saut mural ou traction jusqu'au haut de la tour.
                Stairs($"Escalier {s}", new Vector3(s * (h - 12f), 0f, s * (h - 10f) - s * 8.5f / 0.4f * 0.56f), new Vector3(0f, 0f, s), 8.5f);
                P("Lit", s * 10f, -s * (h - 2f), s > 0 ? 0f : 180f);
                P("Lit", s * 13f, -s * (h - 2f), s > 0 ? 0f : 180f);
                P("Lit", s * 16f, -s * (h - 2f), s > 0 ? 0f : 180f);
                P("Ratelier", -s * (h - 0.5f), s * 10f, s > 0 ? 90f : -90f);
                P("Table", -s * 14f, s * 14f);
                P("Chaise_Attrapable", -s * 12.5f, s * 15.5f);
                P("Caisse_Attrapable", s * 14f, -s * 12f);
                P("Caisse_Attrapable", s * 15.2f, -s * 12f);
                P("Armoire", -s * (h - 0.6f), -s * 14f, s > 0 ? 90f : -90f);
            }

            Bridge("entre les tours", new Vector3(-(h - 10f), 0f, h - 5f), new Vector3(h - 10f, 0f, h - 5f));
            ChestAt(h - 3f, h - 3f, 225f, 11f);
            WallTorches();
            Common(8, 2, 5);
        }

        /// <summary>Prison : cellules ouvertes le long des murs (grilles, paille, chaînes), cages, gardes.</summary>
        private static void Prison(RoomModule m)
        {
            Setup(m, "Prison", 56f);
            float h = _h;
            Quad((sx, sz, q) =>
            {
                for (int i = 0; i < 3; i++)
                {
                    float z = sz * (9f + i * 6f);
                    Block($"Cloison {q}-{i}", sx * (h - 5f), sx * h, z - 0.25f, z + 0.25f, 4f, 0f, _stone, false);
                    P("Paille", sx * (h - 2.5f), z + sz * 3f);
                    P("Chaine", sx * (h - 0.4f), z + sz * 3f, 0f, 2f);
                }

                P("Grille", sx * (h - 5f), sz * 12f, 90f);
                P("Cage", sx * 14f, sz * 14f);
                P("Table", sx * 10f, sz * (h - 3f));
                P("Caisse_Attrapable", sx * 18f, sz * 9f);
                P("Tonneau_Attrapable", sx * 19.2f, sz * 9f);
            });
            ChestAt(h - 2.5f, h - 3f, 270f);
            WallTorches(14f, 3.5f);
            Common(8, 2, 5);
        }

        /// <summary>Carrefour à niches : colonnes d'angle, poutres à 10 m, torches, gravats.</summary>
        private static void CarrefourNiches(RoomModule m)
        {
            Setup(m, "Carrefour à niches", 20f, ModuleKind.Carrefour);
            Quad((sx, sz, q) =>
            {
                P("Colonne", sx * 7.5f, sz * 7.5f);
                P("Torche", sx * 9.4f, sz * 7.5f, sx > 0 ? -90f : 90f, 3.5f);
            });
            KitBuild.Box(_m, "Poutre nord-sud", new Vector3(0f, 10.5f, 0f), new Vector3(1f, 1f, 20f), KitBuild.Kind.Poutre, _kit);
            KitBuild.Box(_m, "Poutre est-ouest", new Vector3(0f, 11.5f, 0f), new Vector3(20f, 1f, 1f), KitBuild.Kind.Poutre, _kit);
            P("Gravats", 6.8f, -8.4f);                                      // hors de la voie (Valider le module : il y était)
            Spawn(7f, 0f);
            Spawn(0f, 7f, true);
        }

        /// <summary>Carrefour encombré : caisses et tonneaux dans les angles, lanterne suspendue.</summary>
        private static void CarrefourEncombre(RoomModule m)
        {
            Setup(m, "Carrefour encombré", 20f, ModuleKind.Carrefour);
            Quad((sx, sz, q) =>
            {
                P(q % 2 == 0 ? "PileDeCaisses" : "Tonneau", sx * 7.5f, sz * 7.5f);
                P("Caisse_Attrapable", sx * 6f, sz * 8.5f);
            });
            P("Lanterne", 0f, 0f, 0f, 9f);
            P("Chaine", 0f, 0f, 0f, 13f);
            Spawn(-7f, 0f);
        }
    }
}
