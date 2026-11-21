using ItHunts.Enemies;
using ItHunts.Level;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Module d'exemple de l'atelier (2026-11-21), construit avec l'outil (mêmes fonctions que la palette) : salle de 56 m sur deux
    /// niveaux — mezzanine à 4 m (bande de 4 m) avec escalier, pont et passerelle ; piliers et arches ; mur avec porte à barreaux ; chaîne ;
    /// plate-forme à 6 m où le traqueur bondit ; coffre derrière un balcon ; apparitions et habitants ; lumières. La voie du traqueur
    /// (au centre, entre les entrées nord et sud) reste libre sous 10 m.
    /// </summary>
    public static class AtelierExample
    {
        public const string Name = "Exemple de l'atelier";
        private static string P(string file) => $"{ModuleLibrary.AtelierPieceDir}/{file}.prefab";
        private static string G(string file) => $"{ModuleLibrary.AtelierGameplayDir}/{file}.prefab";

        private static GameObject Put(RoomModule m, string path, float x, float y, float z, float yaw = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), m.transform);
            go.transform.localPosition = new Vector3(x, y, z);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        [MenuItem("ItHunts/Atelier : construire le module d'exemple")]
        private static void Menu() => Debug.Log(Build());

        public static string Build()
        {
            if (!System.IO.File.Exists(AtelierSceneBuilder.ScenePath))
                AtelierSceneBuilder.Build();
            EditorSceneManager.OpenScene(AtelierSceneBuilder.ScenePath);
            RoomModule m = AtelierModules.New(Name, ModuleKind.Salle, ModuleShape.Etages, Vector2Int.one, 56f);

            // Mezzanine (2e niveau, sol à 4 m) le long du mur ouest, en deux moitiés reliées par un pont.
            foreach (float z in new[] { -20f, -16f, -12f, -8f, 8f, 12f, 16f })
                Put(m, P("Kit_Jeu_Sol4"), -24f, 4f, z);              // une bande de 4 m : sa main atteint dessous
            Put(m, P("Kit_Jeu_Pont"), -24f, 4f, -4f);
            Put(m, P("Kit_Jeu_Pont"), -24f, 4f, 0f);
            Put(m, P("Kit_Jeu_Pont"), -24f, 4f, 4f);
            foreach (float z in new[] { -20f, -8f, 8f, 16f })
                Put(m, P("Kit_Jeu_Pilier"), -22.7f, 0f, z);
            // Escalier jusqu'à la mezzanine, au sud ; passerelle vers l'est au nord.
            Put(m, P("Kit_Atelier_EscalierPierre_4m"), -20.5f, 0f, -18f);
            Put(m, P("Kit_Atelier_Passerelle_8m"), -17.9f, 4f, 16f, 90f);
            // Balcon (garde-corps) au bord de la mezzanine, coffre derrière.
            Put(m, P("Kit_Atelier_Balcon_6m"), -22.6f, 2.8f, 10f, 90f);
            Put(m, G("G_Coffre_Moyen"), -25f, 4f, 11f);

            // Piliers avec arches, côté est.
            Put(m, P("Kit_Jeu_Arc"), 18f, 0f, 2f, 90f);
            Put(m, P("Kit_Jeu_Arc"), 12f, 0f, 2f, 90f);
            // Mur avec porte à barreaux, au nord-est (pas une pièce fermée : on en fait le tour).
            Put(m, P("Kit_Jeu_Mur4x4"), 12f, 0f, 22f);
            Put(m, P("Kit_Jeu_Mur4x4"), 24f, 0f, 22f);
            Put(m, P("Kit_Jeu_Mur4x4"), 12f, 4f, 22f);
            Put(m, P("Kit_Atelier_PorteCellule"), 18f, 0f, 22f);
            // Chaîne à grimper (joueur) au sud-est.
            Put(m, P("Kit_Atelier_ChaineGrimpe_12m"), 12f, 0f, -10f);

            // Plate-forme à 6 m (sud-est) sur des plots de bond ; le traqueur y bondit depuis le sol.
            // Massif de 16 plots de bond (12 × 12 m, 6 m) dans le coin sud-est : plein dessous, le traqueur tient dessus.
            foreach (float x in new[] { 17.5f, 20.5f, 23.5f, 26.5f })
            foreach (float z in new[] { -26.5f, -23.5f, -20.5f, -17.5f })
                Put(m, P("Kit_Atelier_PlateformeBond_6m"), x, 0f, z);
            GameObject bond = Put(m, G("G_Passage_Bond"), 8f, 0f, -22f, 90f);
            bond.GetComponent<StalkerPassage>().end.position = m.transform.TransformPoint(new Vector3(22f, 6f, -22f));
            Put(m, G("G_Coffre_Difficile"), 25f, 6f, -25f);

            // Apparitions, habitants, objets à lancer, plaque magnétique, lumières.
            Put(m, G("G_Apparition_Goule"), 14f, 0f, 14f);
            Put(m, G("G_Apparition_Goule"), -9f, 0f, -24f);
            Put(m, G("G_Apparition_Goule"), 22f, 0f, -4f);
            Put(m, G("G_Apparition_Volant"), 0f, 12f, 10f);
            Put(m, G("G_Habitant_Goule"), 10f, 0f, -2f);
            Put(m, G("G_Habitant_Goule"), -24f, 4f, -14f);
            Put(m, G("G_Coffre_Facile"), 24f, 0f, 12f);
            foreach ((float x, float z) in new[] { (9f, 16f), (10f, 18f), (-10f, -6f) })
                Put(m, "Assets/_Project/Prefabs/Props/Prop_Caisse_Attrapable.prefab", x, 0.5f, z);
            Put(m, G("G_PlaqueMagnetique"), 0f, 14f, 27f);
            foreach ((float x, float z) in new[] { (-20f, -20f), (-20f, 20f), (20f, 20f), (20f, -20f), (0f, 0f) })
                Put(m, G("G_Lumiere"), x, 9f, z);

            string path = AtelierModules.Save(m, active: true);
            bool ok = AtelierModules.Verify(m);
            AtelierModules.RememberLast(path);
            return $"{Name} : {(ok ? "VALIDÉ" : "REFUSÉ")} ({path})\n{AtelierModules.LastReport}";
        }
    }
}
