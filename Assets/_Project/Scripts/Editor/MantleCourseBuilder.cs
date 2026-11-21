using ItHunts.Economy;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Zone d'essai du hissage (D-074), commune à Test_Movement et à la vitrine : une rangée de rebords face au nord (+z), de
    /// 1.5 à 4.2 m de haut (limite avec un saut simple : 3.7 m = pieds à 1.3 + tête 1.8 + 0.6), un rebord trop étroit et un rebord
    /// bloqué. On les aborde en venant du nord, en avançant vers le mur.
    /// </summary>
    public static class MantleCourseBuilder
    {
        /// <summary>Ajoute (ou refait) la zone dans les scènes existantes, sans les reconstruire : Test_Movement et Test_Vitrine.</summary>
        public static string AddToScenes()
        {
            var report = new System.Text.StringBuilder();
            Material block = TestMovementSceneBuilder.Mat("Greybox_Block", new Color(0.45f, 0.45f, 0.5f));
            Material warn = TestMovementSceneBuilder.Mat("Greybox_Landmark", new Color(0.85f, 0.45f, 0.15f));
            foreach ((string path, string parent, Vector3 origin) in new[]
                     {
                         ("Assets/Scenes/Test_Movement.unity", "Level", new Vector3(-18f, 0f, -26f)),
                         ("Assets/Scenes/Test_Vitrine.unity", "Vitrine", new Vector3(-20f, 0f, -18f)),
                     })
            {
                if (!System.IO.File.Exists(path))
                {
                    report.Append(path).Append(" absente ; ");
                    continue;
                }

                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Single);
                GameObject root = System.Array.Find(scene.GetRootGameObjects(), g => g.name == parent);
                if (root == null)
                {
                    report.Append(path).Append(" : pas de racine ").Append(parent).Append(" ; ");
                    continue;
                }

                Transform old = root.transform.Find("Hissage (rebords, D-074)");
                if (old != null)
                    Object.DestroyImmediate(old.gameObject);
                Build(root.transform, origin, block, warn);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                report.Append(path).Append(" : zone de hissage en ").Append(origin).Append(" ; ");
            }

            return report.ToString();
        }

        public static void Build(Transform parent, Vector3 origin, Material mat, Material warn)
        {
            var area = new GameObject("Hissage (rebords, D-074)").transform;
            area.SetParent(parent, false);
            area.localPosition = origin;
            float x = 0f;
            void Ledge(string name, float height, float depth, bool blocked, Material m)
            {
                TestMovementSceneBuilder.Box($"Rebord {name}", area, new Vector3(x, height * 0.5f, -depth * 0.5f),
                    new Vector3(3f, height, depth), m);
                if (blocked)
                    TestMovementSceneBuilder.Box($"Rebord {name} : dalle qui bloque", area, new Vector3(x, height + 2f, -1.5f),
                        new Vector3(3f, 2f, 3f), warn);   // 1 m au-dessus du dessus : pas la place debout
                var anchor = new GameObject("Étiquette : " + name).transform;
                anchor.SetParent(area, false);
                anchor.localPosition = new Vector3(x, Mathf.Max(height, 1.6f) + 0.5f, 0.6f);
                WorldLabel.Create(anchor, Vector3.zero, name, new Color(1f, 0.9f, 0.6f), 0.3f);
                x += 4.5f;
            }

            foreach (float h in new[] { 1.5f, 2f, 2.4f, 3f, 3.4f, 3.7f })
                Ledge($"{h:0.0} m", h, 3f, false, mat);
            Ledge("4.2 m (trop haut)", 4.2f, 3f, false, warn);
            Ledge("2.4 m étroit (0.4 m)", 2.4f, 0.4f, false, warn);
            Ledge("2.4 m bloqué", 2.4f, 3f, true, mat);
        }
    }
}
