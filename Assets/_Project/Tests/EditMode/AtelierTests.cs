using System.Linq;
using ItHunts.EditorTools;
using ItHunts.Level;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Atelier des modules (2026-11-21) : sauvegarde puis rechargement identique, enregistrement dans la bibliothèque du générateur,
    /// vérification qui trouve une cachette et une entrée mal placée.
    /// </summary>
    public class AtelierTests
    {
        private const string TestName = "Test atelier (automatique)";
        private string _path;

        [SetUp]
        public void SetUp()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");   // scène enregistrée : la vérification ouvre une scène à côté
            _path = AtelierModules.PrefabPathFor(TestName);
        }

        [TearDown]
        public void TearDown()
        {
            AtelierModules.CloseOpen();
            if (System.IO.File.Exists(_path))
                AtelierModules.Delete(_path);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");   // scène enregistrée : la vérification ouvre une scène à côté
        }

        private static GameObject Put(RoomModule m, string prefab, Vector3 local, float yaw = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), m.transform);
            go.transform.localPosition = local;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        private static string Snapshot(Transform root) =>
            string.Join(";", root.GetComponentsInChildren<Transform>(true).Where(t => t != root)
                .Select(t => $"{t.name}@{root.InverseTransformPoint(t.position).ToString("F2")}/{t.localEulerAngles.y:0}"));

        [Test]
        public void Sauvegarde_PuisRechargement_Identique()
        {
            RoomModule m = AtelierModules.New(TestName, ModuleKind.Salle, ModuleShape.Simple, Vector2Int.one, 48f);
            Put(m, "Assets/_Project/Prefabs/Kit/Kit_Pilier.prefab", new Vector3(8f, 0f, 6f), 90f);
            Put(m, $"{ModuleLibrary.AtelierGameplayDir}/G_Coffre_Difficile.prefab", new Vector3(-10f, 0f, 4f));
            Put(m, $"{ModuleLibrary.AtelierPieceDir}/Kit_Atelier_Balcon_6m.prefab", new Vector3(0f, 6f, -14f), 180f);
            string before = Snapshot(m.transform);
            string path = AtelierModules.Save(m);
            Assert.That(path, Is.EqualTo(_path));
            AtelierModules.CloseOpen();

            RoomModule again = AtelierModules.Load(path);
            Assert.That(Snapshot(again.transform), Is.EqualTo(before), "mêmes pièces, mêmes places");
            Assert.That(again.displayName, Is.EqualTo(TestName));
            Assert.That(again.GetComponentInChildren<ChestSpot>().difficulty, Is.EqualTo(2));
            ModuleSheet sheet = AtelierModules.SheetOf(path);
            Assert.IsNotNull(sheet, "fiche");
            Assert.That(sheet.connectors.Count, Is.EqualTo(2));
            Assert.That(sheet.chests, Is.EqualTo(1));
            Assert.That(sheet.size, Is.EqualTo(48f));
            Assert.That(sheet.bounds.size.x, Is.EqualTo(48f).Within(0.01f), "zone limite");
        }

        [Test]
        public void Bibliotheque_EnregistreeEtRetiree()
        {
            RoomModule m = AtelierModules.New(TestName, ModuleKind.Salle, ModuleShape.Simple, Vector2Int.one, 48f);
            string path = AtelierModules.Save(m, active: true);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<RoomModule>();
            Assert.That(ModuleValidator.EnsureCatalog().modules, Has.Member(prefab), "actif : dans le catalogue du générateur");
            Assert.IsTrue(prefab.fromAtelier);
            AtelierModules.Register(path, false);
            Assert.That(ModuleValidator.EnsureCatalog().modules, Has.No.Member(prefab), "inactif : retiré");
            Assert.IsFalse(AtelierModules.SheetOf(path).active);
        }

        [Test]
        public void Verification_EntreeMalPlacee()
        {
            RoomModule m = AtelierModules.New(TestName, ModuleKind.Salle, ModuleShape.Simple, Vector2Int.one, 48f);
            Assert.That(ModuleValidator.ConnectorProblems(m), Is.Empty, "entrées posées par l'atelier : à leur place");
            ModuleDoor d = m.GetComponentsInChildren<ModuleDoor>().First();
            d.transform.localPosition += new Vector3(3f, 0f, 0f);
            Assert.That(ModuleValidator.ConnectorProblems(m), Has.Some.Contains("de sa place"), "décalée de 3 m");
            d.transform.localPosition -= new Vector3(3f, 0f, 0f);
            d.width = 3f;
            Assert.That(ModuleValidator.ConnectorProblems(m), Has.Some.Contains("plus petite"), "porte trop petite pour le traqueur");
        }

        [Test]
        public void Verification_TrouveUneCachette()
        {
            RoomModule m = AtelierModules.New(TestName, ModuleKind.Salle, ModuleShape.Simple, Vector2Int.one, 48f);
            // Tunnel bas et fermé de 18 m (le joueur y entre, ni le traqueur ni sa main n'atteignent le fond), hors des voies.
            var tunnel = new GameObject("Tunnel (cachette)").transform;
            tunnel.SetParent(m.transform, false);
            tunnel.localPosition = new Vector3(16f, 0f, 0f);
            void Wall(Vector3 c, Vector3 s)
            {
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                g.transform.SetParent(tunnel, false);
                g.transform.localPosition = c;
                g.transform.localScale = s;
            }

            Wall(new Vector3(-1.75f, 1.25f, 0f), new Vector3(0.5f, 2.5f, 18f));
            Wall(new Vector3(1.75f, 1.25f, 0f), new Vector3(0.5f, 2.5f, 18f));
            Wall(new Vector3(0f, 2.75f, 0f), new Vector3(4f, 0.5f, 18f));
            Wall(new Vector3(0f, 1.25f, 9.25f), new Vector3(4f, 2.5f, 0.5f));
            tunnel.localRotation = Quaternion.identity;
            tunnel.localPosition = new Vector3(18f, 0f, -12f);
            Assert.IsFalse(AtelierModules.Verify(m), "refusé");
            Assert.That(ModuleValidator.LastReport, Does.Contain("cachette"));
            Assert.That(AtelierModules.Markers.Any(k => k.Error && k.Text == "cachette"), Is.True, "repère dans la scène");
            Vector3 fond = new Vector3(18f, 0f, -3f);
            Assert.That(AtelierModules.Markers.Where(k => k.Text == "cachette").Min(k => Vector3.Distance(new Vector3(k.Local.x, 0f, k.Local.z), fond)),
                Is.LessThan(10f), "la cachette est dans le tunnel");
        }
    }
}
