using System.IO;
using ItHunts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Scène de l'atelier des modules (Assets/Scenes/Atelier_Modules.unity) : sol sombre à grille, éclairage neutre (ou du jeu),
    /// repères d'échelle (joueur 1.8 m, traqueur v4 debout 14 m et baissé 8.5 m, plus petite porte franchissable 4 × 5.2 m, limite
    /// de hissage 5.5 m). Reconstruite par ItHunts > Atelier : reconstruire la scène.
    /// </summary>
    public static class AtelierSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Atelier_Modules.unity";
        private const string ArtDir = "Assets/_Project/Art/Atelier";

        [MenuItem("ItHunts/Atelier : reconstruire la scène Atelier_Modules")]
        private static void Menu() => Debug.Log(Build());

        [MenuItem("ItHunts/Atelier : ouvrir la scène Atelier_Modules")]
        public static void Open()
        {
            if (!File.Exists(ScenePath))
                Build();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static string Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var stageGo = new GameObject("Atelier");
            var stage = stageGo.AddComponent<AtelierStage>();

            // Sol de travail : un plan sombre, texture de grille répétée (un trait par pas).
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Sol de travail (grille)";
            ground.transform.SetParent(stageGo.transform, false);
            ground.transform.localScale = Vector3.one * (stage.gridSize / 10f);
            ground.transform.position = new Vector3(0f, -0.02f, 0f);
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.AddComponent<BoxCollider>().size = new Vector3(10f, 0.01f, 10f);  // pour poser les pièces sur le sol
            ground.GetComponent<Renderer>().sharedMaterial = GridMaterial();
            stage.grid = ground.GetComponent<Renderer>();

            // Éclairage neutre : soleil doux et ciel gris ; l'éclairage du jeu les coupe (noir + torches du module).
            var lights = new GameObject("Éclairage neutre");
            lights.transform.SetParent(stageGo.transform, false);
            stage.neutralLights = lights;
            foreach ((Vector3 euler, float k) in new[] { (new Vector3(50f, -30f, 0f), 1.1f), (new Vector3(35f, 150f, 0f), 0.45f) })
            {
                var l = new GameObject("Lumière directionnelle").AddComponent<Light>();
                l.transform.SetParent(lights.transform, false);
                l.type = LightType.Directional;
                l.intensity = k;
                l.shadows = LightShadows.Soft;
                l.transform.rotation = Quaternion.Euler(euler);
            }

            // Repères d'échelle, à l'ouest de la zone de travail.
            var refs = new GameObject("Repères d'échelle").transform;
            refs.SetParent(stageGo.transform, false);
            refs.position = new Vector3(-130f, 0f, 0f);           // hors d'un module de 2 cases (240 m)
            Material ghost = ModuleLibrary.AtelierMaterial("M_Atelier_Repere", new Color(0.3f, 0.75f, 1f), new Color(0.05f, 0.2f, 0.35f));
            Material warn = ModuleLibrary.AtelierMaterial("M_Atelier_Limite", new Color(1f, 0.55f, 0.15f), new Color(0.4f, 0.15f, 0f));
            Prim(refs, PrimitiveType.Capsule, "Joueur (1.8 m)", new Vector3(0f, 0.9f, 0f), new Vector3(0.8f, 0.9f, 0.8f), ghost);
            Label(refs, "Joueur 1,8 m", new Vector3(0f, 2.4f, 0f));
            var v4 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Models/Ennemis/SK_Traqueur_v4.fbx");
            if (v4 != null)
            {
                var st = (GameObject)PrefabUtility.InstantiatePrefab(v4, refs);
                st.name = "Traqueur v4 (modèle, debout)";
                st.transform.localPosition = new Vector3(14f, 0f, 0f);
                st.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }

            Prim(refs, PrimitiveType.Cube, "Traqueur debout (14 m)", new Vector3(14f, 7f, 10f), new Vector3(3f, 14f, 3f), ghost);
            Label(refs, "Traqueur debout 14 m", new Vector3(14f, 15f, 6f));
            Prim(refs, PrimitiveType.Cube, "Traqueur baissé (8.5 m)", new Vector3(26f, 4.25f, 0f), new Vector3(4f, 8.5f, 10f), ghost);
            Label(refs, "Traqueur baissé 8,5 m", new Vector3(26f, 9.5f, 0f));
            // Plus petite porte franchissable : un cadre (4 × 5.2 m).
            var door = new GameObject("Plus petite porte franchissable (4 × 5.2 m)").transform;
            door.SetParent(refs, false);
            door.localPosition = new Vector3(38f, 0f, 0f);
            Prim(door, PrimitiveType.Cube, "montant", new Vector3(-2.15f, 2.6f, 0f), new Vector3(0.3f, 5.2f, 0.3f), warn);
            Prim(door, PrimitiveType.Cube, "montant", new Vector3(2.15f, 2.6f, 0f), new Vector3(0.3f, 5.2f, 0.3f), warn);
            Prim(door, PrimitiveType.Cube, "linteau", new Vector3(0f, 5.35f, 0f), new Vector3(4.6f, 0.3f, 0.3f), warn);
            Label(door, "Porte 4 × 5,2 m", new Vector3(0f, 6.4f, 0f));
            // Limite de hissage : un mur plus bas que 5.5 m se franchit en se hissant.
            Prim(refs, PrimitiveType.Cube, "Limite de hissage (5.5 m)", new Vector3(56f, 2.75f, 0f), new Vector3(6f, 5.5f, 0.3f), warn);
            Label(refs, "Hissage : 5,5 m", new Vector3(56f, 6.4f, 0f));
            foreach (Collider c in refs.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);

            stage.Apply();
            EditorSceneManager.SaveScene(scene, ScenePath);
            return "Atelier : scène " + ScenePath;
        }

        private static GameObject Prim(Transform parent, PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject g = GameObject.CreatePrimitive(t);
            g.name = name;
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = m;
            return g;
        }

        private static void Label(Transform parent, string text, Vector3 pos)
        {
            var g = new GameObject("Étiquette : " + text);
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            var tm = g.AddComponent<TextMesh>();
            tm.text = text;
            tm.characterSize = 0.35f;
            tm.fontSize = 48;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;
        }

        /// <summary>Matériau de la grille : texture d'un trait clair sur fond sombre (une case = un pas), sans éclairage.</summary>
        private static Material GridMaterial()
        {
            Directory.CreateDirectory(ArtDir);
            string texPath = ArtDir + "/T_Atelier_Grille.png";
            if (!File.Exists(texPath))
            {
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool line = x < 2 || y < 2;
                    tex.SetPixel(x, y, line ? new Color(0.42f, 0.46f, 0.52f) : new Color(0.07f, 0.075f, 0.085f));
                }

                File.WriteAllBytes(texPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.filterMode = FilterMode.Trilinear;
                imp.SaveAndReimport();
            }

            string matPath = ArtDir + "/M_Atelier_Grille.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, matPath);
            }

            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
