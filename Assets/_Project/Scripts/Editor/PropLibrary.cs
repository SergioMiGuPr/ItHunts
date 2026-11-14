using System.Collections.Generic;
using System.Linq;
using ItHunts.Orbs;
using UnityEditor;
using UnityEngine;
using static ItHunts.EditorTools.TestMovementSceneBuilder;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Props greybox de la cave du château (étape 8, après playtest) : un prefab par catégorie (Prop_Tonneau, Prop_Etagere…),
    /// fait de formes simples, une seule boîte de collision (collisions simplifiées), batching statique pour le décor fixe.
    /// Les props attrapables brillent (Grabbable). La DA remplace le contenu du prefab par le vrai modèle, sans toucher aux
    /// scènes. Catalogue (dimensions, usage) : ART-HANDOFF.md.
    /// </summary>
    public static class PropLibrary
    {
        public const string Dir = "Assets/_Project/Prefabs/Props";

        public sealed class Def
        {
            public string Id, Category;
            public Vector3 Size;
            public bool Grabbable, Light, NoCollider;
            public float Mass;
        }

        /// <summary>Catalogue : id (nom du prefab sans « Prop_ »), catégorie, dimensions (m), attrapable, lumière.</summary>
        public static readonly List<Def> Catalog = new List<Def>
        {
            D("Tonneau", "Tonneau", 0.9f, 1.1f, 0.9f),
            D("Tonneau_Attrapable", "Tonneau", 0.6f, 0.9f, 0.6f, grab: 20f),
            D("Caisse", "Caisse", 1.3f, 1.3f, 1.3f),
            D("Caisse_Attrapable", "Caisse", 0.8f, 0.8f, 0.8f, grab: 15f),
            D("PileDeCaisses", "Caisse", 2.6f, 2.6f, 2.6f),
            D("Etagere", "Étagère", 2.6f, 3.2f, 0.7f),
            D("Table", "Table", 2.6f, 0.95f, 1.2f),
            D("Chaise_Attrapable", "Chaise", 0.55f, 1f, 0.55f, grab: 8f),
            D("Banc", "Banc", 2.6f, 0.55f, 0.6f),
            D("Lit", "Lit", 1.3f, 0.7f, 2.3f),
            D("CoffreDeco", "Coffre décoratif", 1.2f, 0.8f, 0.8f),
            D("Armoire", "Armoire", 1.6f, 2.7f, 0.8f),
            D("Ratelier", "Râtelier d'armes", 2.6f, 2.1f, 0.5f),
            D("Statue", "Statue", 1.4f, 3.6f, 1.4f),
            D("Colonne", "Pilier / colonne", 1.4f, 8f, 1.4f),
            D("ColonneBrisee", "Colonne brisée", 1.4f, 2.6f, 1.4f),
            D("Chaine", "Chaîne", 0.2f, 4f, 0.2f, noCollider: true),
            D("Cage", "Cage", 2.2f, 3f, 2.2f),
            D("Lanterne", "Lanterne", 0.5f, 0.8f, 0.5f, noCollider: true, light: true),
            D("Torche", "Torche", 0.3f, 1f, 0.5f, noCollider: true, light: true),
            D("TorcheEteinte", "Torche (flamme sans lumière)", 0.3f, 1f, 0.5f, noCollider: true),
            D("Gravats", "Tas de gravats", 3.2f, 1.2f, 3f),
            D("MurEffondre", "Mur effondré", 5f, 3f, 1.2f),
            D("Echafaudage", "Échafaudage", 4f, 6f, 2f),
            D("Poutre", "Poutre", 6f, 0.5f, 0.5f),
            D("Grille", "Grille", 3f, 3f, 0.2f),
            D("Tapisserie", "Tapisserie", 2.6f, 4f, 0.08f, noCollider: true),
            D("Autel", "Autel", 2.6f, 1.2f, 1.3f),
            D("Sarcophage", "Sarcophage", 1.2f, 1f, 2.5f),
            D("Paille", "Paille (litière)", 2f, 0.3f, 2f, noCollider: true),
            D("Chaudron", "Chaudron", 1.3f, 1f, 1.3f),
            D("Tuyau", "Tuyau d'égout", 0.8f, 0.8f, 6f),
        };

        /// <summary>Nombre d'instances posées par id, depuis la dernière remise à zéro (catalogue d'ART-HANDOFF).</summary>
        public static readonly Dictionary<string, int> Usage = new Dictionary<string, int>();

        private static Def D(string id, string cat, float x, float y, float z, float grab = 0f, bool noCollider = false, bool light = false) =>
            new Def { Id = id, Category = cat, Size = new Vector3(x, y, z), Grabbable = grab > 0f, Mass = grab, NoCollider = noCollider, Light = light };

        private static readonly Dictionary<string, GameObject> Cache = new Dictionary<string, GameObject>();
        private static Material _wood, _darkWood, _stone, _iron, _cloth, _straw, _flame, _glowWood, _water;

        private static void Mats()
        {
            _wood = Mat("Prop_Bois", new Color(0.36f, 0.25f, 0.15f));
            _darkWood = Mat("Prop_BoisSombre", new Color(0.22f, 0.15f, 0.1f));
            _stone = Mat("Prop_Pierre", new Color(0.33f, 0.32f, 0.34f));
            _iron = Mat("Prop_Fer", new Color(0.16f, 0.16f, 0.18f));
            _cloth = Mat("Prop_Tissu", new Color(0.45f, 0.08f, 0.08f));
            _straw = Mat("Prop_Paille", new Color(0.55f, 0.47f, 0.22f));
            _flame = UnlitMat("Prop_Flamme", "Universal Render Pipeline/Unlit", new Color(3f, 1.5f, 0.4f));
            _glowWood = GrabbableMat("Greybox_Grabbable", new Color(0.55f, 0.42f, 0.28f));
        }

        [MenuItem("ItHunts/Reconstruire les props greybox (Assets/_Project/Prefabs/Props)")]
        private static void BuildFromMenu() => Debug.Log(BuildAll());

        /// <summary>(Re)crée tous les prefabs de props. Un prefab déjà remplacé par la DA est gardé tel quel (étiquette « DA »).</summary>
        public static string BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Props");
            Mats();
            Cache.Clear();
            int made = 0, kept = 0;
            foreach (Def d in Catalog)
            {
                string path = $"{Dir}/Prop_{d.Id}.prefab";
                var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (existing != null && System.Array.IndexOf(AssetDatabase.GetLabels(existing), "DA") >= 0)
                {
                    kept++;
                    continue;
                }

                GameObject go = Make(d);
                PrefabUtility.SaveAsPrefabAsset(go, path);
                Object.DestroyImmediate(go);
                made++;
            }

            AssetDatabase.SaveAssets();
            return $"props : {made} prefabs greybox, {kept} gardés (DA)";
        }

        /// <summary>Pose un prop (prefab) : position des pieds, rotation autour de y. Compte l'usage.</summary>
        private static int _torches;

        public static GameObject Place(string id, Transform parent, Vector3 feet, float yaw = 0f)
        {
            // Performance (étape 9, écran partagé : 3 ms de lumières) : une torche sur deux éclaire, l'autre n'a que sa flamme.
            if (id == "Torche" && (_torches++ & 1) == 1)
                id = "TorcheEteinte";
            if (!Cache.TryGetValue(id, out GameObject prefab) || prefab == null)
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/Prop_{id}.prefab");
                if (prefab == null)
                {
                    BuildAll();
                    prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Dir}/Prop_{id}.prefab");
                }

                Cache[id] = prefab;
            }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            Def d = Get(id);
            Vector3 lift = d != null && d.Grabbable ? Vector3.up * (d.Size.y / 2f) : Vector3.zero;   // attrapable : pivot au centre
            go.transform.SetPositionAndRotation(feet + lift, Quaternion.Euler(0f, yaw, 0f));
            Usage[id] = Usage.TryGetValue(id, out int n) ? n + 1 : 1;
            return go;
        }

        public static Def Get(string id) => Catalog.Find(d => d.Id == id);

        // ---------- Formes ----------

        private static GameObject Part(Transform parent, PrimitiveType type, Vector3 center, Vector3 size, Material mat, Vector3 euler = default)
        {
            GameObject p = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.name = type.ToString();
            p.transform.SetParent(parent, false);
            p.transform.localPosition = center;
            p.transform.localRotation = Quaternion.Euler(euler);
            p.transform.localScale = size;
            p.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return p;
        }

        private static GameObject Make(Def d)
        {
            var root = new GameObject($"Prop_{d.Id}");
            Transform t = root.transform;
            Vector3 s = d.Size;
            if (d.Grabbable)
                return MakeGrabbableProp(d);

            switch (d.Id)
            {
                case "Tonneau":
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, s.y / 2f, 0f), new Vector3(s.x, s.y / 2f, s.z), _wood);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, s.y * 0.25f, 0f), new Vector3(s.x * 1.04f, 0.04f, s.z * 1.04f), _iron);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, s.y * 0.75f, 0f), new Vector3(s.x * 1.04f, 0.04f, s.z * 1.04f), _iron);
                    break;
                case "Caisse":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y / 2f, 0f), s, _wood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y / 2f, 0f), new Vector3(s.x * 1.02f, 0.12f, s.z * 1.02f), _darkWood);
                    break;
                case "PileDeCaisses":
                    Part(t, PrimitiveType.Cube, new Vector3(-0.6f, 0.65f, -0.6f), Vector3.one * 1.3f, _wood);
                    Part(t, PrimitiveType.Cube, new Vector3(0.65f, 0.65f, -0.55f), Vector3.one * 1.3f, _darkWood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.65f, 0.65f), Vector3.one * 1.3f, _wood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f), Vector3.one * 1.3f, _darkWood, new Vector3(0f, 20f, 0f));
                    break;
                case "Etagere":
                    Part(t, PrimitiveType.Cube, new Vector3(-s.x / 2f + 0.05f, s.y / 2f, 0f), new Vector3(0.1f, s.y, s.z), _darkWood);
                    Part(t, PrimitiveType.Cube, new Vector3(s.x / 2f - 0.05f, s.y / 2f, 0f), new Vector3(0.1f, s.y, s.z), _darkWood);
                    for (int i = 0; i < 4; i++)
                    {
                        float y = 0.2f + i * 0.95f;
                        Part(t, PrimitiveType.Cube, new Vector3(0f, y, 0f), new Vector3(s.x, 0.08f, s.z), _wood);
                        Part(t, PrimitiveType.Cube, new Vector3(-0.6f + 0.4f * i, y + 0.25f, 0f), new Vector3(0.35f, 0.4f, 0.4f), i % 2 == 0 ? _cloth : _straw);
                    }
                    break;
                case "Table":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y - 0.06f, 0f), new Vector3(s.x, 0.12f, s.z), _wood);
                    foreach (float x in new[] { -1f, 1f })
                    foreach (float z in new[] { -1f, 1f })
                        Part(t, PrimitiveType.Cube, new Vector3(x * (s.x / 2f - 0.15f), (s.y - 0.12f) / 2f, z * (s.z / 2f - 0.15f)), new Vector3(0.14f, s.y - 0.12f, 0.14f), _darkWood);
                    break;
                case "Banc":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y - 0.05f, 0f), new Vector3(s.x, 0.1f, s.z), _wood);
                    Part(t, PrimitiveType.Cube, new Vector3(-s.x / 2f + 0.2f, (s.y - 0.1f) / 2f, 0f), new Vector3(0.15f, s.y - 0.1f, s.z), _darkWood);
                    Part(t, PrimitiveType.Cube, new Vector3(s.x / 2f - 0.2f, (s.y - 0.1f) / 2f, 0f), new Vector3(0.15f, s.y - 0.1f, s.z), _darkWood);
                    break;
                case "Lit":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), new Vector3(s.x, 0.5f, s.z), _darkWood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.58f, 0.1f), new Vector3(s.x * 0.92f, 0.16f, s.z * 0.85f), _cloth);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.6f, -s.z / 2f + 0.05f), new Vector3(s.x, 1.2f, 0.1f), _darkWood);
                    break;
                case "CoffreDeco":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.32f, 0f), new Vector3(s.x, 0.64f, s.z), _darkWood);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, 0.64f, 0f), new Vector3(s.z, s.x / 2f, s.z * 0.6f), _wood, new Vector3(0f, 0f, 90f));
                    break;
                case "Armoire":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y / 2f, 0f), s, _darkWood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y / 2f, s.z / 2f + 0.02f), new Vector3(0.05f, s.y * 0.9f, 0.04f), _iron);
                    break;
                case "Ratelier":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.15f, 0f), new Vector3(s.x, 0.3f, s.z), _darkWood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y - 0.1f, 0f), new Vector3(s.x, 0.12f, s.z), _darkWood);
                    for (int i = 0; i < 7; i++)
                        Part(t, PrimitiveType.Cube, new Vector3(-s.x / 2f + 0.3f + i * 0.33f, s.y / 2f, 0f), new Vector3(0.06f, s.y * 0.95f, 0.06f), _iron, new Vector3(0f, 0f, 6f));
                    break;
                case "Statue":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(s.x, 0.8f, s.z), _stone);
                    Part(t, PrimitiveType.Capsule, new Vector3(0f, 2f, 0f), new Vector3(0.9f, 1.2f, 0.7f), _stone);
                    Part(t, PrimitiveType.Sphere, new Vector3(0f, 3.3f, 0f), Vector3.one * 0.55f, _stone);
                    break;
                case "Colonne":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(s.x * 1.2f, 0.6f, s.z * 1.2f), _stone);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, s.y / 2f, 0f), new Vector3(s.x * 0.8f, s.y / 2f, s.z * 0.8f), _stone);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y - 0.3f, 0f), new Vector3(s.x * 1.2f, 0.6f, s.z * 1.2f), _stone);
                    break;
                case "ColonneBrisee":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.3f, 0f), new Vector3(s.x * 1.2f, 0.6f, s.z * 1.2f), _stone);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, s.y / 2f, 0f), new Vector3(s.x * 0.8f, s.y / 2f, s.z * 0.8f), _stone, new Vector3(4f, 0f, 3f));
                    Part(t, PrimitiveType.Cylinder, new Vector3(1.6f, 0.4f, 0.3f), new Vector3(s.x * 0.8f, 1f, s.z * 0.8f), _stone, new Vector3(0f, 30f, 90f));
                    break;
                case "Chaine":
                    for (int i = 0; i < 8; i++)
                        Part(t, PrimitiveType.Cube, new Vector3(0f, s.y - 0.25f - i * 0.5f, 0f), new Vector3(0.12f, 0.4f, 0.05f), _iron, new Vector3(0f, i * 90f, 0f));
                    break;
                case "Cage":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(s.x, 0.1f, s.z), _iron);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y - 0.05f, 0f), new Vector3(s.x, 0.1f, s.z), _iron);
                    for (int i = 0; i < 6; i++)
                    {
                        float k = -s.x / 2f + i * s.x / 5f;
                        Part(t, PrimitiveType.Cube, new Vector3(k, s.y / 2f, -s.z / 2f), new Vector3(0.06f, s.y, 0.06f), _iron);
                        Part(t, PrimitiveType.Cube, new Vector3(k, s.y / 2f, s.z / 2f), new Vector3(0.06f, s.y, 0.06f), _iron);
                        Part(t, PrimitiveType.Cube, new Vector3(-s.x / 2f, s.y / 2f, k), new Vector3(0.06f, s.y, 0.06f), _iron);
                        Part(t, PrimitiveType.Cube, new Vector3(s.x / 2f, s.y / 2f, k), new Vector3(0.06f, s.y, 0.06f), _iron);
                    }
                    break;
                case "Lanterne":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(0.4f, 0.55f, 0.4f), _iron);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(0.3f, 0.4f, 0.3f), _flame);
                    AddLight(t, new Vector3(0f, 0.4f, 0f), new Color(1f, 0.7f, 0.4f), 3f, 11f);
                    break;
                case "TorcheEteinte":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.2f), new Vector3(0.08f, 0.7f, 0.08f), _darkWood, new Vector3(-25f, 0f, 0f));
                    Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0.38f), Vector3.one * 0.22f, _flame);
                    break;
                case "Torche":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.2f), new Vector3(0.08f, 0.7f, 0.08f), _darkWood, new Vector3(-25f, 0f, 0f));
                    Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.85f, 0.38f), Vector3.one * 0.22f, _flame);
                    AddLight(t, new Vector3(0f, 1.1f, 0.5f), new Color(1f, 0.6f, 0.3f), 3.5f, 12f);
                    break;
                case "Gravats":
                    Part(t, PrimitiveType.Cube, new Vector3(-0.6f, 0.35f, -0.4f), new Vector3(1.6f, 0.7f, 1.4f), _stone, new Vector3(5f, 20f, 8f));
                    Part(t, PrimitiveType.Cube, new Vector3(0.7f, 0.3f, 0.5f), new Vector3(1.4f, 0.6f, 1.3f), _stone, new Vector3(-6f, 50f, 0f));
                    Part(t, PrimitiveType.Cube, new Vector3(0.1f, 0.75f, 0f), new Vector3(1f, 0.6f, 1f), _stone, new Vector3(10f, 10f, -12f));
                    break;
                case "MurEffondre":
                    Part(t, PrimitiveType.Cube, new Vector3(-1.6f, 1.5f, 0f), new Vector3(1.8f, 3f, s.z), _stone);
                    Part(t, PrimitiveType.Cube, new Vector3(0.2f, 1f, 0f), new Vector3(1.8f, 2f, s.z), _stone);
                    Part(t, PrimitiveType.Cube, new Vector3(1.8f, 0.5f, 0f), new Vector3(1.4f, 1f, s.z), _stone);
                    break;
                case "Echafaudage":
                    foreach (float x in new[] { -1f, 1f })
                    foreach (float z in new[] { -1f, 1f })
                        Part(t, PrimitiveType.Cube, new Vector3(x * (s.x / 2f - 0.1f), s.y / 2f, z * (s.z / 2f - 0.1f)), new Vector3(0.15f, s.y, 0.15f), _wood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 3f, 0f), new Vector3(s.x, 0.12f, s.z), _wood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y - 0.06f, 0f), new Vector3(s.x, 0.12f, s.z), _wood);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 1.5f, s.z / 2f), new Vector3(s.x, 0.1f, 0.1f), _wood, new Vector3(0f, 0f, 35f));
                    break;
                case "Poutre":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y / 2f, 0f), s, _darkWood);
                    break;
                case "Grille":
                    for (int i = 0; i < 9; i++)
                        Part(t, PrimitiveType.Cube, new Vector3(-s.x / 2f + 0.15f + i * (s.x - 0.3f) / 8f, s.y / 2f, 0f), new Vector3(0.07f, s.y, 0.07f), _iron);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y * 0.66f, 0f), new Vector3(s.x, 0.08f, 0.08f), _iron);
                    break;
                case "Tapisserie":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y / 2f, 0f), s, _cloth);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y + 0.05f, 0f), new Vector3(s.x + 0.2f, 0.1f, 0.1f), _darkWood);
                    break;
                case "Autel":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y / 2f, 0f), s, _stone);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, s.y + 0.02f, 0f), new Vector3(s.x * 0.9f, 0.04f, s.z * 0.6f), _cloth);
                    Part(t, PrimitiveType.Cylinder, new Vector3(-0.9f, s.y + 0.25f, 0f), new Vector3(0.12f, 0.25f, 0.12f), _straw);
                    Part(t, PrimitiveType.Cylinder, new Vector3(0.9f, s.y + 0.25f, 0f), new Vector3(0.12f, 0.25f, 0.12f), _straw);
                    break;
                case "Sarcophage":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.4f, 0f), new Vector3(s.x, 0.8f, s.z), _stone);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.9f, 0f), new Vector3(s.x * 1.05f, 0.2f, s.z * 1.03f), _stone);
                    break;
                case "Paille":
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(s.x, 0.25f, s.z), _straw, new Vector3(0f, 12f, 0f));
                    break;
                case "Chaudron":
                    Part(t, PrimitiveType.Sphere, new Vector3(0f, 0.55f, 0f), new Vector3(s.x, 0.9f, s.z), _iron);
                    Part(t, PrimitiveType.Cube, new Vector3(0f, 0.12f, 0f), new Vector3(1f, 0.25f, 1f), _stone);
                    break;
                case "Tuyau":
                    Part(t, PrimitiveType.Cylinder, new Vector3(0f, s.y / 2f, 0f), new Vector3(s.x, s.z / 2f, s.y), _iron, new Vector3(90f, 0f, 0f));
                    break;
            }

            Merge(root, d.Id);                                              // un seul rendu par prop (performance)

            if (!d.NoCollider)
            {
                var box = root.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, s.y / 2f, 0f);
                box.size = s;
            }

            foreach (Transform c in root.GetComponentsInChildren<Transform>(true))
                if (c.GetComponent<Light>() == null)
                    GameObjectUtility.SetStaticEditorFlags(c.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
            return root;
        }

        /// <summary>
        /// Fusionne les formes du prop en un seul maillage (un sous-maillage par matériau), enregistré dans Props/Meshes : un
        /// seul rendu par prop au lieu de 3 à 30 (draw calls et culling ; mesuré dans le build de l'étape 8).
        /// </summary>
        private static void Merge(GameObject root, string id)
        {
            var parts = root.GetComponentsInChildren<MeshFilter>(true).Where(f => f.gameObject != root).ToList();
            if (parts.Count == 0)
                return;
            var byMat = new Dictionary<Material, List<CombineInstance>>();
            Matrix4x4 toRoot = root.transform.worldToLocalMatrix;
            foreach (MeshFilter f in parts)
            {
                Material m = f.GetComponent<MeshRenderer>().sharedMaterial;
                if (!byMat.TryGetValue(m, out List<CombineInstance> list))
                    byMat[m] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = f.sharedMesh, transform = toRoot * f.transform.localToWorldMatrix });
            }

            var subs = new List<CombineInstance>();
            var mats = new List<Material>();
            foreach (var kv in byMat)
            {
                var sub = new Mesh();
                sub.CombineMeshes(kv.Value.ToArray(), true, true);
                subs.Add(new CombineInstance { mesh = sub, transform = Matrix4x4.identity });
                mats.Add(kv.Key);
            }

            var mesh = new Mesh { name = $"Prop_{id}" };
            mesh.CombineMeshes(subs.ToArray(), false, false);
            mesh.RecalculateBounds();
            foreach (CombineInstance c in subs)
                Object.DestroyImmediate(c.mesh);
            string dir = Dir + "/Meshes";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder(Dir, "Meshes");
            string path = $"{dir}/Prop_{id}.asset";
            var old = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (old != null)
            {
                old.Clear();
                EditorUtility.CopySerialized(mesh, old);
                Object.DestroyImmediate(mesh);
                mesh = old;
            }
            else
                AssetDatabase.CreateAsset(mesh, path);

            foreach (MeshFilter f in parts)
                Object.DestroyImmediate(f.gameObject);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
        }

        private static void AddLight(Transform t, Vector3 at, Color color, float intensity, float range)
        {
            var go = new GameObject("Lumière");
            go.transform.SetParent(t, false);
            go.transform.localPosition = at;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        /// <summary>Prop attrapable : une seule forme (son collider sert à la prise et aux impacts), lueur des objets attrapables.</summary>
        private static GameObject MakeGrabbableProp(Def d)
        {
            PrimitiveType type = d.Id.StartsWith("Tonneau") ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = $"Prop_{d.Id}";
            Vector3 s = d.Size;
            go.transform.localScale = type == PrimitiveType.Cylinder ? new Vector3(s.x, s.y / 2f, s.z) : s;
            go.GetComponent<MeshRenderer>().sharedMaterial = _glowWood;
            MakeGrabbable(go, d.Mass, false);
            return go;
        }
    }
}
