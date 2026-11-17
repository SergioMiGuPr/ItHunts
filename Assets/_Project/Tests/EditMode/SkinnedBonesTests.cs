using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Playtest du 2026-11-17 : le traqueur de Test_Vitrine n'était plus dessiné (son rendu gardait 32 os quand le maillage de la
    /// PR #8 en a 42 : Unity ne dessine pas un maillage gréé dont le nombre d'os ne correspond pas). Chaque SkinnedMeshRenderer des
    /// scènes du jeu et des prefabs a autant d'os que son maillage a de poses de liaison, et aucun os vide.
    /// </summary>
    public class SkinnedBonesTests
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Level_01.unity", "Assets/Scenes/Level_01_Aleatoire.unity", "Assets/Scenes/Level_01_Base.unity",
            "Assets/Scenes/Test_Arena.unity", "Assets/Scenes/Test_Vitrine.unity", "Assets/Scenes/Test_Movement.unity",
        };

        private static IEnumerable<string> Problems(GameObject root, string where) =>
            root.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.sharedMesh != null && (r.bones.Length != r.sharedMesh.bindposes.Length || r.bones.Any(b => b == null)))
                .Select(r => $"{where} : {r.transform.parent?.name}/{r.name} a {r.bones.Length} os pour {r.sharedMesh.bindposes.Length} poses de liaison");

        [Test]
        public void Prefabs_OsDesRendusGrees_CorrespondentAuMaillage()
        {
            var bad = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .SelectMany(p => Problems(AssetDatabase.LoadAssetAtPath<GameObject>(p), p)).ToList();
            Assert.IsEmpty(bad, string.Join("\n", bad));
        }

        [Test]
        public void Scenes_OsDesRendusGrees_CorrespondentAuMaillage()
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            var bad = new List<string>();
            try
            {
                foreach (string path in Scenes)
                {
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    foreach (GameObject root in scene.GetRootGameObjects())
                        bad.AddRange(Problems(root, path));
                }
            }
            finally
            {
                if (setup.Length > 0)
                    EditorSceneManager.RestoreSceneManagerSetup(setup);
            }

            Assert.IsEmpty(bad, string.Join("\n", bad.Distinct()));
        }
    }
}
