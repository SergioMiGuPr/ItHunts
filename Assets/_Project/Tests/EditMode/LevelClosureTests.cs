using ItHunts.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Salles fermées et hissage (A3, matin du 2026-11-21) : le hissage (D-074) passe un mur jusqu'à 5.5 m environ ; une pièce
    /// verrouillée, une pièce annexe et la sortie avec l'arène du boss ont donc un plafond partout (ou des murs plus hauts).
    /// </summary>
    public class LevelClosureTests
    {
        [TestCase("Salle_Verrouillee")]
        [TestCase("Piece_Annexe")]
        [TestCase("Sortie_Arene")]
        public void SalleFermee_PlafondPartout(string prefab)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Niveau/{prefab}.prefab");
            Assert.That(asset, Is.Not.Null, prefab);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.position = new Vector3(5000f, 3000f, 5000f);
            try
            {
                Physics.SyncTransforms();
                RoomVolume[] rooms = go.GetComponentsInChildren<RoomVolume>();
                Assert.That(rooms.Length, Is.GreaterThan(0), "volume de salle");
                foreach (RoomVolume v in rooms)
                {
                    Vector3 size = new SerializedObject(v).FindProperty("size").vector3Value;
                    int n = 0, covered = 0;
                    for (int i = 0; i < 7; i++)
                    for (int j = 0; j < 7; j++)
                    {
                        Vector3 top = v.transform.TransformPoint(new Vector3((i / 6f - 0.5f) * size.x * 0.9f, size.y * 0.5f - 0.2f, (j / 6f - 0.5f) * size.z * 0.9f));
                        if (!Physics.Raycast(top, Vector3.down, out RaycastHit floor, size.y + 2f, ~0, QueryTriggerInteraction.Ignore))
                            continue;
                        n++;
                        if (Physics.Raycast(floor.point + Vector3.up, Vector3.up, 60f, ~0, QueryTriggerInteraction.Ignore))
                            covered++;
                    }

                    Assert.That(n, Is.GreaterThan(20), $"{prefab} / {v.name} : sol trouvé");
                    Assert.That(covered, Is.EqualTo(n), $"{prefab} / {v.name} : plafond au-dessus de chaque point du sol");
                }
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
