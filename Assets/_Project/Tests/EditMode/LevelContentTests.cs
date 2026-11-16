using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Orbs;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>Contenu du niveau 1 après le playtest de l'étape 8 : props, modules, passages, parcours (points 2 à 4).</summary>
    public class LevelContentTests
    {
        private const string Base = "Assets/Scenes/Level_01_Base.unity";

        [Test]
        public void Props_UnPrefabParCategorie_AttrapablesQuiBrillent()
        {
            string[] ids = { "Tonneau", "Caisse", "Etagere", "Table", "Lit", "Armoire", "Statue", "Colonne", "ColonneBrisee", "Chaine", "Cage", "Lanterne", "Torche", "Gravats", "MurEffondre", "Echafaudage", "Poutre", "Grille", "Tapisserie", "CoffreDeco" };
            foreach (string id in ids)
                Assert.That(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Props/Prop_{id}.prefab"), Is.Not.Null, id);
            foreach (string id in new[] { "Caisse_Attrapable", "Tonneau_Attrapable", "Chaise_Attrapable" })
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Project/Prefabs/Props/Prop_{id}.prefab");
                Assert.That(go.GetComponent<Grabbable>(), Is.Not.Null, id + " attrapable");
                Assert.That(go.GetComponent<Rigidbody>(), Is.Not.Null);
            }

            var torch = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/Prop_Torche.prefab");
            Assert.That(torch.GetComponentInChildren<Light>(), Is.Not.Null, "les torches éclairent");
            var barrel = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Props/Prop_Tonneau.prefab");
            Assert.That(barrel.GetComponentsInChildren<Collider>().Length, Is.EqualTo(1), "une seule boîte de collision (collisions simplifiées)");
        }

        [Test]
        public void Modules_AuMoinsDouze_AvecApparitionsHabitantsEtCoffres()
        {
            var mods = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Modules" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(g => g.GetComponent<RoomModule>() != null && g.GetComponent<RoomModule>().weight > 0f).ToList();   // les modèles (poids 0) ne sont pas tirés
            var halls = mods.Where(m => m.GetComponent<RoomModule>().kind == ModuleKind.Salle).ToList();
            Assert.That(halls.Count, Is.GreaterThanOrEqualTo(12), "au moins 12 modules de salle");
            Assert.That(mods.Count(m => m.GetComponent<RoomModule>().kind == ModuleKind.Carrefour), Is.GreaterThanOrEqualTo(2));
            foreach (GameObject m in halls)
            {
                Assert.That(m.GetComponentsInChildren<SpawnPoint>().Length, Is.GreaterThanOrEqualTo(4), m.name);
                Assert.That(m.GetComponentsInChildren<ResidentSpot>().Length, Is.GreaterThanOrEqualTo(3), m.name);
                Assert.That(m.GetComponentsInChildren<ChestSpot>().Length, Is.GreaterThanOrEqualTo(1), m.name);
                Assert.That(m.GetComponentsInChildren<Grabbable>().Length, Is.GreaterThanOrEqualTo(4), m.name + " : objets à lancer");
                Assert.That(m.GetComponentsInChildren<Light>().Length, Is.GreaterThanOrEqualTo(4), m.name + " : lumière");
                Assert.That(new[] { 48f, 56f, 64f }, Does.Contain(m.GetComponent<RoomModule>().size), m.name);
            }

            Assert.That(halls.Any(m => m.GetComponent<RoomModule>().hole == ModuleHole.Canal), Is.True, "égouts");
            Assert.That(halls.Any(m => m.GetComponent<RoomModule>().hole == ModuleHole.Vide), Is.True, "parcours");
            Assert.That(halls.Any(m => m.GetComponent<RoomModule>().level == ModuleLevel.Haut), Is.True, "salle haute remplie");
        }

        [Test]
        public void VideEtPlateformes_RepriseAuPlusProche_PoseeSurLeDessus()
        {
            var root = new GameObject("Vide (test)");
            try
            {
                root.AddComponent<BoxCollider>().size = new Vector3(10f, 4f, 10f);
                var a = new GameObject("A").transform;
                a.position = new Vector3(-8f, 0f, 0f);
                var b = new GameObject("B").transform;
                b.position = new Vector3(8f, 0f, 0f);
                a.SetParent(root.transform);
                b.SetParent(root.transform);
                var v = root.AddComponent<FallVoid>();
                v.Configure(20f, new[] { a, b });
                Assert.That(v.RespawnFor(new Vector3(3f, -1f, 0f)), Is.EqualTo(b.position), "rive la plus proche");
                Assert.That(v.RespawnFor(new Vector3(-3f, -1f, 0f)), Is.EqualTo(a.position));

                var plat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plat.transform.SetParent(root.transform);
                plat.transform.position = new Vector3(0f, -0.25f, 20f);
                plat.transform.localScale = new Vector3(2.4f, 0.5f, 2.4f);
                Physics.SyncTransforms();
                var c = plat.AddComponent<CrumblePlatform>();
                typeof(CrumblePlatform).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(c, null);
                Assert.That(c.OnTop(new Vector3(0.5f, 0.02f, 20.5f)), Is.True, "pieds sur le dessus");
                Assert.That(c.OnTop(new Vector3(0f, -3f, 20f)), Is.False, "dessous");
                Assert.That(c.OnTop(new Vector3(5f, 0f, 20f)), Is.False, "à côté");
                Assert.That(c.HoldTime, Is.LessThanOrEqualTo(2f), "on ne s'y arrête pas plus de 2 s");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static int Count(string scene, string script) =>
            Regex.Matches(File.ReadAllText(scene), $@"m_Script: \{{fileID: 11500000, guid: {AssetDatabase.AssetPathToGUID(script)}").Count;

        [Test]
        public void Niveau1_ModulesDecorPassagesParcours_PlusDeVideALaPente()
        {
            const string rt = "Assets/_Project/Scripts/Runtime/";
            string text = File.ReadAllText(Base);
            Assert.That(Regex.Matches(text, "value: [\"']?Module [^\n]* : [A-G][0-9]").Count, Is.GreaterThanOrEqualTo(40), "un module par salle et par carrefour (instances de prefab)");
            Assert.That(Regex.Matches(text, "m_Name: \"?D(\\\\xE9|é)cor : Couloir").Count, Is.GreaterThanOrEqualTo(40), "décor dans les couloirs");
            Assert.That(Regex.Matches(text, "m_Name: \"?Mobilier : ").Count, Is.GreaterThanOrEqualTo(10), "pièces annexes meublées");
            Assert.That(Regex.Matches(text, "m_Name: \"?Passage [^\\n]*: sol").Count, Is.GreaterThanOrEqualTo(6), "passages en hauteur");
            Assert.That(Count(Base, rt + "Level/FallVoid.cs"), Is.GreaterThanOrEqualTo(6), "galeries et fosses de parcours");
            Assert.That(Count(Base, rt + "Level/CrumblePlatform.cs"), Is.GreaterThanOrEqualTo(30));
            Assert.That(text.Contains("voie au sol"), Is.False, "pentes sur toute la largeur : plus de vide à côté");
        }

        [Test]
        public void Arc_Ecoincons_DeLExtradosAuPlafond_DUnMurALAutre()
        {
            const float width = 14f, height = 15f, sx = 1f, rise = 4f;
            List<Vector2> outline = KitBuild.SpandrelOutline(width, sx, rise, height);
            Assert.That(outline[0].x, Is.EqualTo(-7.1f).Within(1e-3f), "part du mur (dans son épaisseur)");
            Assert.That(outline[outline.Count - 1].x, Is.EqualTo(0f).Within(1e-3f), "jusqu'à la clé");
            Vector2[] e = KitBuild.ArchExtrados();
            for (int i = 0; i < outline.Count; i++)
            {
                if (i > 0)
                    Assert.That(outline[i].x, Is.GreaterThan(outline[i - 1].x));
                Assert.That(outline[i].y, Is.LessThanOrEqualTo(height), "sous le plafond");
            }
            // Le bas reste dans l'anneau (sous l'extrados, au-dessus de l'intrados) : pas de jour entre l'arc et le mur de pierre.
            for (int i = 1; i < e.Length; i++)
            {
                float yLow = KitBuild.ArchMidline[i].y + rise - KitBuild.ArchRingThickness * 0.5f;
                float yHigh = e[i].y + rise;
                Vector2 p = outline.First(o => Mathf.Abs(o.x - e[i].x * sx) < 1e-3f);
                Assert.That(p.y, Is.InRange(yLow, yHigh), $"point {i}");
            }

            var root = new GameObject("arc (test)").transform;
            try
            {
                Transform col = KitBuild.Spandrels(root, null, width, height, sx, rise);
                var boxes = col.GetComponentsInChildren<BoxCollider>();
                Assert.That(boxes.Length, Is.GreaterThanOrEqualTo(2 * (KitBuild.ArchMidline.Length - 1)));
                Assert.That(col.GetComponent<StalkerPassesThrough>(), Is.Not.Null);
                Assert.That(boxes.Max(b => b.transform.localPosition.y + b.size.y * 0.5f), Is.GreaterThan(height), "entre dans le plafond");
                Assert.That(boxes.Min(b => b.transform.localPosition.y - b.size.y * 0.5f), Is.GreaterThan(rise - KitBuild.SpandrelTuck - 0.01f), "rien sous la naissance de l'arc");
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }
        }

        [Test]
        public void Arc_CollisionDeLAnneau_TraqueurPasseAuTravers_ColonnesDeLaDA()
        {
            var root = new GameObject("arc (test)").transform;
            try
            {
                const float sx = 1f, rise = 4f;
                Transform col = KitBuild.ArchCollision(root, sx, rise);
                var boxes = col.GetComponentsInChildren<BoxCollider>();
                Assert.That(boxes.Length, Is.EqualTo(2 * (KitBuild.ArchMidline.Length - 1)), "une boîte par segment, des deux côtés");
                Assert.That(col.GetComponent<StalkerPassesThrough>(), Is.Not.Null, "le traqueur passe au travers (pas de NavMesh, pas de baisse)");
                Assert.That(col.GetComponent<NotWalkable>(), Is.Not.Null);
                foreach (BoxCollider b in boxes)
                {
                    Vector3 c = b.transform.localPosition;
                    Assert.That(Mathf.Abs(c.x), Is.LessThanOrEqualTo(7f * sx));
                    Assert.That(c.y, Is.InRange(rise, rise + 11f));
                }
                // Passage libre : au centre, rien sous l'intrados (9.6 m au-dessus du pied de l'arc).
                float lowestAtCenter = boxes.Where(b => Mathf.Abs(b.transform.localPosition.x) < 1f).Min(b => b.bounds.min.y);
                Assert.That(lowestAtCenter, Is.GreaterThan(rise + 8.5f));
            }
            finally
            {
                Object.DestroyImmediate(root.gameObject);
            }

            var kit = AssetDatabase.LoadAssetAtPath<KitSet>("Assets/_Project/Settings/Kit_Niveau1.asset");
            Assert.That(kit.pilier, Is.Not.Null, "piédroits : colonne de la DA (SM_Colonne)");
            Assert.That(kit.pilier.GetComponentsInChildren<MeshFilter>(true).Any(m => m.sharedMesh != null && m.sharedMesh.name.Contains("Colonne")), Is.True);
        }
    }
}
