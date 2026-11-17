using System.Linq;
using ItHunts.Orbs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Orbe v2 (playtest du 2026-11-17 : disque gris plat) : le prefab Player affiche SM_Orbe (3 anneaux, énergie, 6 tablettes)
    /// sous « Orbe (vue) », avec ses réglages ; la sphère greybox est masquée en jeu.
    /// </summary>
    public class OrbModelTests
    {
        [Test]
        public void PrefabPlayer_OrbeV2_Branchee()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
            var view = prefab.GetComponent<OrbModelView>();
            Assert.IsNotNull(view, "OrbModelView absent du prefab Player");
            var so = new SerializedObject(view);
            Assert.IsNotNull(so.FindProperty("settings").objectReferenceValue, "réglages de l'orbe");
            var model = so.FindProperty("model").objectReferenceValue as Transform;
            Assert.IsNotNull(model, "instance de SM_Orbe");
            Assert.AreEqual("Orbe (vue)", model.parent.name);
            foreach (string p in new[] { "ring1", "ring2", "ring3", "energy", "energyRenderer", "greybox", "crown" })
                Assert.IsNotNull(so.FindProperty(p).objectReferenceValue, p);
            SerializedProperty tablets = so.FindProperty("tablets");
            Assert.AreEqual(6, tablets.arraySize);
            for (int i = 0; i < 6; i++)
            {
                var r = tablets.GetArrayElementAtIndex(i).objectReferenceValue as Renderer;
                Assert.IsNotNull(r, $"tablette {i + 1}");
                Assert.GreaterOrEqual(r.sharedMaterials.Length, 2, "glyphe = matériau d'index 1");
            }

            Assert.IsTrue(model.GetComponentsInChildren<MeshFilter>(true).All(f => f.sharedMesh != null), "maillages du modèle");
        }

        [Test]
        public void Tablettes_UneParChargeDeTractionEtDeRepulsion_RepartiesRegulierement()
        {
            float[] a = OrbModelView.TabletAngles(2, 1);
            Assert.AreEqual(3, a.Length);
            Assert.AreEqual(270f, a[2], 1e-3f, "la répulsion en bas");
            Assert.AreEqual(30f, a[0], 1e-3f);
            Assert.AreEqual(150f, a[1], 1e-3f);
            float[] b = OrbModelView.TabletAngles(3, 1);                // amélioration : une charge de traction en plus
            Assert.AreEqual(4, b.Length);
            for (int i = 0; i < b.Length; i++)
                Assert.AreEqual(90f, Mathf.DeltaAngle(b[i], b[(i + 1) % b.Length]), 1e-3f, "écart régulier");
        }
    }
}
