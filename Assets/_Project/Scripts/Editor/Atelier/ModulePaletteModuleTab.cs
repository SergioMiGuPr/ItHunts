using System.Linq;
using ItHunts.Level;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>Onglet « Module » de la palette : nouveau, sauvegarder, charger, dupliquer, supprimer, vérifier, jouer.</summary>
    public sealed partial class ModulePaletteWindow
    {
        private string _newName = "Ma salle";
        private ModuleKind _newKind = ModuleKind.Salle;
        private ModuleShape _newShape = ModuleShape.Etages;
        private Vector2Int _newFootprint = Vector2Int.one;
        private float _newSize = 56f;
        private int _loadIndex;
        private string _dupName = "Copie";
        private Vector2 _reportScroll;

        private void ModuleGUI()
        {
            if (Object.FindFirstObjectByType<AtelierStage>() == null && GUILayout.Button("Ouvrir la scène de l'atelier"))
                AtelierSceneBuilder.Open();

            EditorGUILayout.LabelField("Nouveau module", EditorStyles.boldLabel);
            _newName = EditorGUILayout.TextField("Nom", _newName);
            _newKind = (ModuleKind)EditorGUILayout.EnumPopup("Type de salle", _newKind);
            _newShape = (ModuleShape)EditorGUILayout.EnumPopup("Forme (plafond)", _newShape);
            _newFootprint = EditorGUILayout.Vector2IntField("Cases (1 ou 2)", _newFootprint);
            _newSize = EditorGUILayout.Slider("Côté (m, multiple de 4)", _newSize, 24f, 112f);
            if (GUILayout.Button("Nouveau module"))
            {
                RoomModule m = AtelierModules.New(_newName, _newKind, _newShape, _newFootprint, _newSize);
                AtelierModules.RememberLast(AtelierModules.PathOf(m));
            }

            EditorGUILayout.Space();
            RoomModule open = AtelierModules.OpenModule();
            EditorGUILayout.LabelField("Module ouvert", open != null ? open.displayName : "aucun", EditorStyles.boldLabel);
            if (open != null)
            {
                string path = AtelierModules.PathOf(open);
                ModuleSheet sheet = AtelierModules.SheetOf(path);
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Sauvegarder"))
                    {
                        AtelierModules.Save(open);
                        AtelierModules.RememberLast(path);
                    }

                    if (GUILayout.Button("Vérifier le module"))
                        AtelierModules.Verify(open);
                    if (GUILayout.Button("Jouer ce module"))
                    {
                        AtelierModules.RememberLast(path);
                        AtelierModules.Play(open);
                        GUIUtility.ExitGUI();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("+ Entrée"))
                    {
                        DoorSide side = (DoorSide)(open.GetComponentsInChildren<ModuleDoor>().Length % 4);
                        AtelierModules.AddConnector(open, side, 0);
                    }

                    if (GUILayout.Button("Coque d'aperçu"))
                        AtelierModules.Preview(open);
                    if (GUILayout.Button("Fermer"))
                        AtelierModules.CloseOpen();
                }

                if (sheet != null)
                {
                    EditorGUI.BeginChangeCheck();
                    bool active = EditorGUILayout.ToggleLeft("Actif dans la génération (s'il est validé)", sheet.active);
                    int diff = EditorGUILayout.IntSlider("Difficulté", sheet.difficulty, 0, 2);
                    float weight = EditorGUILayout.FloatField("Poids d'apparition", open.weight);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(open, "Poids");
                        open.weight = Mathf.Max(0f, weight);
                        sheet.difficulty = diff;
                        EditorUtility.SetDirty(sheet);
                        AtelierModules.Register(path, active);
                    }

                    EditorGUILayout.LabelField(open.validated ? "Validé" : "Pas encore validé (« Vérifier le module »)",
                        open.validated ? EditorStyles.boldLabel : EditorStyles.miniBoldLabel);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _dupName = EditorGUILayout.TextField(_dupName);
                    if (GUILayout.Button("Dupliquer", GUILayout.Width(90f)))
                    {
                        AtelierModules.Save(open);
                        string dst = AtelierModules.Duplicate(path, _dupName);
                        AtelierModules.Load(dst);
                    }
                }

                if (GUILayout.Button("Supprimer ce module…")
                    && EditorUtility.DisplayDialog("Supprimer le module", $"Supprimer « {open.displayName} » (prefab et fiche) et le retirer de la génération ?", "Supprimer", "Annuler"))
                {
                    AtelierModules.CloseOpen();
                    AtelierModules.Delete(path);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Charger", EditorStyles.boldLabel);
            var paths = AtelierModules.AllModulePaths();
            string[] labels = paths.Select(p =>
            {
                var m = AssetDatabase.LoadAssetAtPath<GameObject>(p).GetComponent<RoomModule>();
                return $"{m.displayName}{(m.fromAtelier ? " (atelier)" : "")}{(AtelierModules.InLibrary(p) ? "" : " — hors génération")}";
            }).ToArray();
            _loadIndex = EditorGUILayout.Popup(_loadIndex, labels);
            if (paths.Count > 0 && GUILayout.Button("Charger"))
            {
                AtelierModules.Load(paths[Mathf.Clamp(_loadIndex, 0, paths.Count - 1)]);
                AtelierModules.RememberLast(paths[Mathf.Clamp(_loadIndex, 0, paths.Count - 1)]);
            }

            if (!string.IsNullOrEmpty(AtelierModules.LastReport))
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Dernière vérification", EditorStyles.boldLabel);
                _reportScroll = EditorGUILayout.BeginScrollView(_reportScroll, GUILayout.MinHeight(140f));
                EditorGUILayout.HelpBox(AtelierModules.LastReport, ModuleValidator.LastOk ? MessageType.Info : MessageType.Error);
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
