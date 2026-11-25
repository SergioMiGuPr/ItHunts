using System.Collections.Generic;
using System.Linq;
using ItHunts.Level.Acts;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Bibliothèques d'ensembles de décor (D-118) : une par acte, pièces retrouvées par leur nom dans les prefabs de la DA et du projet,
    /// branchées dans les réglages d'acte. À relancer quand un texte d'ensembles nomme une nouvelle pièce.
    /// </summary>
    public static class EnsembleSetup
    {
        private const string Dir = "Assets/_Project/Settings/Ensembles";

        [MenuItem("ItHunts/Ensembles de décor : mettre à jour les pièces")]
        public static void Apply()
        {
            var byName = new Dictionary<string, GameObject>();
            foreach (string g in AssetDatabase.FindAssets("t:prefab", new[] { "Assets/_Project/Prefabs/Props", "Assets/Art/Prefabs", "Assets/Art/Levels/Exemples", "Assets/Art/Levels/Ensembles" }))
            {
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g));
                if (go != null && !byName.ContainsKey(go.name))
                    byName[go.name] = go;
            }

            for (int act = 1; act <= 3; act++)
            {
                var text = AssetDatabase.LoadAssetAtPath<TextAsset>($"{Dir}/Acte{act}.txt");
                string path = $"{Dir}/Ensembles_Acte{act}.asset";
                var lib = AssetDatabase.LoadAssetAtPath<EnsembleLibrary>(path);
                if (lib == null)
                {
                    lib = ScriptableObject.CreateInstance<EnsembleLibrary>();
                    AssetDatabase.CreateAsset(lib, path);
                }

                var act0 = AssetDatabase.LoadAssetAtPath<ActLayoutSettings>($"Assets/_Project/Settings/Actes/Act_{act}.asset");
                lib.text = text;
                var missing = new List<string>();
                lib.pieces = new List<GameObject>();
                foreach (string n in EnsembleLibrary.Parse(text.text).SelectMany(s => s.Pieces).Select(p => p.Name).Distinct())
                {
                    // Nom de prop du jeu : pièce du kit de l'acte d'abord (table de remplacement).
                    GameObject kit = act0.art != null ? act0.art.Swap(n) : null;
                    if (kit != null)
                        continue;
                    if (byName.TryGetValue(n, out GameObject go))
                        lib.pieces.Add(go);
                    else
                        missing.Add(n);
                }

                EditorUtility.SetDirty(lib);
                act0.ensembles = lib;
                EditorUtility.SetDirty(act0);
                Debug.Log($"Ensembles acte {act} : {EnsembleLibrary.Parse(text.text).Count} ensembles, {lib.pieces.Count} pièces" +
                          (missing.Count > 0 ? ", introuvables : " + string.Join(", ", missing) : ""));
            }

            AssetDatabase.SaveAssets();
        }
    }
}
