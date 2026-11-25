using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Input
{
    /// <summary>
    /// Touches changées par joueur (A5, matin du 2026-11-21) : un fichier JSON d'overrides de l'Input System par place de joueur
    /// (touches_joueur1.json…), chargé au démarrage sur les actions de CE joueur (écran partagé : chacun les siennes). Lignes du
    /// menu des touches, conflits, échange, réinitialisation : opérations pures sur un InputActionAsset (testées en EditMode).
    /// </summary>
    public static class RebindStore
    {
        public const string Keyboard = "KeyboardMouse", Gamepad = "Gamepad";

        /// <summary>Dossier des fichiers (tests : un dossier temporaire).</summary>
        public static string Folder = null;

        public static string FilePath(int slot) =>
            Path.Combine(Folder ?? Application.persistentDataPath, $"touches_joueur{slot + 1}.json");

        /// <summary>Une ligne du menu : une action, l'index de sa liaison dans ce groupe, son libellé.</summary>
        public readonly struct Row
        {
            public readonly InputAction Action;
            public readonly int Binding;
            public readonly string Label;

            public Row(InputAction action, int binding, string label)
            {
                Action = action;
                Binding = binding;
                Label = label;
            }

            public string Path => Action.bindings[Binding].effectivePath;
            public string Display => French(Action.GetBindingDisplayString(Binding));
            public bool Overridden => !string.IsNullOrEmpty(Action.bindings[Binding].overridePath);
        }

        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "Jump", "Sauter" }, { "Slide", "Glisser" }, { "Dash", "Dash" }, { "Attract", "Attirer (traction)" },
            { "Repel", "Repousser" }, { "Grab", "Attraper / relâcher" }, { "Throw", "Lancer / artefact" }, { "Reload", "Recharger" },
            { "Interact", "Interagir" }, { "Parry", "Parer" }, { "Map", "Carte" }, { "Pause", "Pause" }, { "ToggleDebug", "Affichage de debug" },
            { "FreeCursor", "Libérer le curseur (améliorations)" },
        };

        private static readonly Dictionary<string, string> MoveParts = new Dictionary<string, string>
        {
            { "up", "Avancer" }, { "down", "Reculer" }, { "left", "Aller à gauche" }, { "right", "Aller à droite" },
        };

        /// <summary>Lignes d'un groupe (clavier et souris, ou manette) : boutons et touches ; les sticks et la souris (visée) restent fixes.</summary>
        public static List<Row> Rows(InputActionAsset asset, string group)
        {
            var rows = new List<Row>();
            InputActionMap map = asset.FindActionMap("Player", false);
            if (map == null)
                return rows;
            foreach (InputAction a in map.actions)
            {
                int firstComposite = -1, composite = -1;
                for (int i = 0; i < a.bindings.Count; i++)
                {
                    InputBinding b = a.bindings[i];
                    if (b.isComposite)
                    {
                        composite = i;
                        if (firstComposite < 0)
                            firstComposite = i;
                        continue;
                    }

                    if (string.IsNullOrEmpty(b.groups) || !b.groups.Contains(group))
                        continue;
                    if (b.isPartOfComposite)
                    {
                        // Déplacement : les quatre directions du premier composite (ZQSD / WASD), pas les flèches.
                        if (a.name == "Move" && composite == firstComposite && MoveParts.TryGetValue(b.name, out string part))
                            rows.Add(new Row(a, i, part));
                        continue;
                    }

                    if (Labels.TryGetValue(a.name, out string label))
                        rows.Add(new Row(a, i, label));                      // visée (souris, stick) : pas de rebinding
                }
            }

            return rows;
        }

        /// <summary>Autre ligne du même groupe déjà liée à ce chemin (conflit), ou null.</summary>
        public static Row? Conflict(List<Row> rows, Row self, string path)
        {
            foreach (Row r in rows)
                if (!(r.Action == self.Action && r.Binding == self.Binding) && string.Equals(r.Path, path, System.StringComparison.OrdinalIgnoreCase))
                    return r;
            return null;
        }

        /// <summary>Échange les touches de deux lignes.</summary>
        public static void Swap(Row a, Row b)
        {
            string pa = a.Path, pb = b.Path;
            a.Action.ApplyBindingOverride(a.Binding, pb);
            b.Action.ApplyBindingOverride(b.Binding, pa);
        }

        public static void Set(Row r, string path) => r.Action.ApplyBindingOverride(r.Binding, path);

        public static void Reset(Row r) => r.Action.RemoveBindingOverride(r.Binding);

        public static void ResetAll(InputActionAsset asset) => asset.RemoveAllBindingOverrides();

        public static void Save(InputActionAsset asset, int slot)
        {
            string json = asset.SaveBindingOverridesAsJson();
            string path = FilePath(slot);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, json);
        }

        /// <summary>Charge les touches du joueur (aucun fichier : touches par défaut). Vrai si un fichier a été appliqué.</summary>
        public static bool Load(InputActionAsset asset, int slot)
        {
            string path = FilePath(slot);
            asset.RemoveAllBindingOverrides();
            if (!File.Exists(path))
                return false;
            try
            {
                asset.LoadBindingOverridesFromJson(File.ReadAllText(path));
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Touches du joueur {slot + 1} illisibles ({e.Message}) : touches par défaut.");
                asset.RemoveAllBindingOverrides();
                return false;
            }
        }

        /// <summary>Texte d'une touche pour les invites à l'écran (« E », « Y »…), selon l'appareil du joueur.</summary>
        public static string Prompt(InputAction action, bool gamepad)
        {
            if (action == null)
                return "?";
            string s = action.GetBindingDisplayString(InputBinding.MaskByGroup(gamepad ? Gamepad : Keyboard));
            return string.IsNullOrEmpty(s) ? "?" : French(s);
        }

        /// <summary>Noms des boutons de la souris en français (l'Input System les donne en anglais).</summary>
        public static string French(string display) => display switch
        {
            "LMB" => "Clic gauche",
            "RMB" => "Clic droit",
            "MMB" => "Clic milieu",
            _ => display,
        };
    }
}
