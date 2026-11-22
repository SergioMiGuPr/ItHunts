using System.Collections.Generic;
using System.Linq;
using ItHunts.Level;
using ItHunts.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.Tests
{
    /// <summary>
    /// Accès aux modules (point 2 du 2026-11-21) : les parties (Jouer, Lancer la run) tirent la carte aléatoire du catalogue du niveau 1,
    /// où l'on tombe sur chacun des modules en quelques parties ; le menu Playtest lance une seed choisie et une salle de test par
    /// module (le module posé contre la salle de départ).
    /// </summary>
    public class ModuleAccessTests
    {
        private const string CatalogPath = "Assets/_Project/Settings/ModuleCatalog_Niveau1.asset";
        private const string LayoutPath = "Assets/_Project/Settings/MapLayout_Niveau1.asset";
        private static readonly string[] NewModules = { "Prison à étages", "Caserne", "Citerne inondée", "Puits circulaire" };

        private static List<ModuleInfo> Infos(List<RoomModule> mods) => mods.Select((m, i) => ModuleInfo.From(m, i)).ToList();

        [Test]
        public void Partie_LeNiveau1EstLaCarteAleatoireDuCatalogue()
        {
            // Nuit 3 : la run enchaîne les 3 actes organiques (ActLevel) ; la carte aléatoire des modules reste jouable (atelier).
            var run = AssetDatabase.LoadAssetAtPath<RunDefinition>("Assets/_Project/Settings/Run_Default.asset");
            Assert.AreEqual("Acte_1_Caves", run.levels[0].sceneName);
            Assert.AreEqual("Acte_2_Chateau", run.levels[1].sceneName);
            Assert.AreEqual("Acte_3_Jardin", run.levels[2].sceneName);
            string scene = System.IO.File.ReadAllText("Assets/Scenes/Level_01_Aleatoire.unity");
            Assert.IsTrue(scene.Contains(AssetDatabase.AssetPathToGUID(CatalogPath)), "la scène tire ses modules du catalogue du niveau 1");
            var usable = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(CatalogPath).Usable().Select(m => m.displayName).ToList();
            foreach (string n in NewModules)
                CollectionAssert.Contains(usable, n, "module tirable");
        }

        [Test]
        public void Partie_ChaqueModuleSortEnQuelquesParties()
        {
            List<RoomModule> mods = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(CatalogPath).Usable();
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutSettings>(LayoutPath);
            List<ModuleInfo> infos = Infos(mods);
            var seen = new HashSet<int>();
            for (int seed = 1; seed <= 5; seed++)
                foreach (PlanRoom r in MapPlanner.Plan(layout, infos, seed).Rooms)
                    if (r.Module >= 0)
                        seen.Add(r.Module);
            for (int i = 0; i < mods.Count; i++)
                Assert.IsTrue(seen.Contains(i), $"« {mods[i].displayName} » absent de 5 parties");
        }

        [Test]
        public void SalleDeTest_ChaqueModuleEstPoseContreLeDepart()
        {
            List<RoomModule> mods = AssetDatabase.LoadAssetAtPath<ModuleCatalog>(CatalogPath).Usable();
            var layout = AssetDatabase.LoadAssetAtPath<MapLayoutSettings>(LayoutPath);
            for (int t = 0; t < mods.Count; t++)
            {
                List<ModuleInfo> infos = Infos(mods);
                infos[t].Weight *= RandomLevel.TestModuleWeight;
                int seed = RandomLevel.TestModuleSeed(layout, infos, t, 777);
                MapPlan p = MapPlanner.Plan(layout, infos, seed);
                bool next = p.Links.Any(l => (l.A == p.Start && p.Rooms[l.B].Module == t) || (l.B == p.Start && p.Rooms[l.A].Module == t));
                Assert.IsTrue(next, $"« {mods[t].displayName} » : pas de seed qui le relie au départ");
            }
        }

        [Test]
        public void MenuPlaytest_SeedEtSalleDeTestParModule()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            var menu = Object.FindFirstObjectByType<MainMenu>(FindObjectsInactive.Include);
            Assert.IsNotNull(menu);
            Assert.AreEqual(AssetDatabase.LoadAssetAtPath<ModuleCatalog>(CatalogPath).Usable().Count, menu.TestModules.Count,
                "un module de test par module tirable");
            foreach (string n in NewModules)
                CollectionAssert.Contains(menu.TestModules, n);
        }
    }
}
