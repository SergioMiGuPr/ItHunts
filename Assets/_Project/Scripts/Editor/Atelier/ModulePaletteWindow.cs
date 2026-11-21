using System.Collections.Generic;
using System.Linq;
using ItHunts.Level;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Palette des modules (ItHunts > Palette des modules, atelier du 2026-11-21) : toutes les pièces « jeu » par catégorie, avec
    /// vignettes ; clic sur une vignette puis clic dans la vue Scene = pose, aimantée à la grille et aux coins des autres pièces ;
    /// R / Maj+R : rotation de 90° (Ctrl : 15°) ; Alt + molette : hauteur ; Échap : arrêter de poser. Ctrl+D, Ctrl+Z et la
    /// sélection d'Unity restent utilisables. Onglet « Module » : nouveau, sauvegarder, charger, dupliquer, supprimer, vérifier,
    /// jouer (AtelierModules).
    /// </summary>
    public sealed partial class ModulePaletteWindow : EditorWindow
    {
        private List<AtelierPiece> _pieces;
        private string[] _categories;
        private int _category;
        private int _tab;
        private Vector2 _scroll;
        private AtelierPiece _armed;
        private GameObject _ghost;
        private float _yaw, _height;
        private bool _snapVertices = true;
        private static readonly string[] Tabs = { "Pièces", "Module" };

        /// <summary>Pièce en cours de pose (tests).</summary>
        public AtelierPiece Armed => _armed;

        [MenuItem("ItHunts/Palette des modules")]
        public static ModulePaletteWindow Open()
        {
            var w = GetWindow<ModulePaletteWindow>("Palette des modules");
            w.minSize = new Vector2(320f, 420f);
            return w;
        }

        private void OnEnable()
        {
            Reload();
            SceneView.duringSceneGui += OnScene;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnScene;
            Disarm();
        }

        private void Reload()
        {
            _pieces = ModuleLibrary.AtelierCatalog();
            if (_pieces.Any(p => p.Prefab == null))
            {
                ModuleLibrary.GenerateAtelierPieces();                       // enveloppes « jeu » manquantes
                _pieces = ModuleLibrary.AtelierCatalog();
            }

            _pieces.RemoveAll(p => p.Prefab == null);
            _categories = _pieces.Select(p => p.Category).Distinct().ToArray();
        }

        private static AtelierStage Stage => Object.FindFirstObjectByType<AtelierStage>();

        private void OnGUI()
        {
            _tab = GUILayout.Toolbar(_tab, Tabs);
            if (_tab == 1)
            {
                ModuleGUI();
                return;
            }

            AtelierStage stage = Stage;
            using (new EditorGUILayout.HorizontalScope())
            {
                if (stage == null)
                {
                    if (GUILayout.Button("Ouvrir la scène de l'atelier"))
                        AtelierSceneBuilder.Open();
                }
                else
                {
                    GUILayout.Label("Grille", GUILayout.Width(40f));
                    foreach (float s in new[] { 1f, 2f, 4f })
                        if (GUILayout.Toggle(Mathf.Approximately(stage.step, s), $"{s:0} m", "Button", GUILayout.Width(44f)) && !Mathf.Approximately(stage.step, s))
                        {
                            Undo.RecordObject(stage, "Pas de la grille");
                            stage.SetStep(s);
                        }

                    bool game = GUILayout.Toggle(stage.gameLighting, "Éclairage du jeu", "Button");
                    if (game != stage.gameLighting)
                    {
                        Undo.RecordObject(stage, "Éclairage");
                        stage.SetGameLighting(game);
                    }
                }
            }

            _snapVertices = EditorGUILayout.ToggleLeft("Aimanter aux coins des autres pièces", _snapVertices);
            EditorGUILayout.HelpBox(_armed != null
                ? $"Pose : {_armed.Name}. Clic : poser ; R / Maj+R : 90° (Ctrl : 15°) ; Alt + molette : hauteur ({_height:0.0} m) ; Échap : arrêter."
                : "Cliquez une vignette, puis dans la vue Scene.", MessageType.None);
            _category = Mathf.Clamp(_category, 0, Mathf.Max(0, _categories.Length - 1));
            _category = EditorGUILayout.Popup("Catégorie", _category, _categories);
            if (GUILayout.Button("Recharger les pièces"))
                Reload();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            string cat = _categories.Length > 0 ? _categories[_category] : "";
            var inCat = _pieces.Where(p => p.Category == cat).ToList();
            const float cell = 96f;
            int perRow = Mathf.Max(1, Mathf.FloorToInt((position.width - 24f) / (cell + 6f)));
            for (int i = 0; i < inCat.Count; i += perRow)
            {
                using (new EditorGUILayout.HorizontalScope())
                    for (int j = i; j < Mathf.Min(i + perRow, inCat.Count); j++)
                    {
                        AtelierPiece p = inCat[j];
                        Texture tex = AssetPreview.GetAssetPreview(p.Prefab);
                        if (tex == null && AssetPreview.IsLoadingAssetPreview(p.Prefab.GetInstanceID()))
                            Repaint();
                        var content = new GUIContent(tex, p.Name + (p.Approximate ? " (collisions approchées)" : ""));
                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(cell)))
                        {
                            GUI.backgroundColor = _armed == p ? new Color(0.5f, 0.9f, 0.6f) : Color.white;
                            bool clicked = tex != null
                                ? GUILayout.Button(content, GUILayout.Width(cell), GUILayout.Height(cell))
                                : GUILayout.Button(p.Name, GUILayout.Width(cell), GUILayout.Height(cell));
                            GUI.backgroundColor = Color.white;
                            GUILayout.Label(p.Name, EditorStyles.miniLabel, GUILayout.Width(cell));
                            if (clicked)
                                Arm(p);
                        }
                    }
            }

            EditorGUILayout.EndScrollView();
        }

        public void Arm(AtelierPiece p)
        {
            Disarm();
            _armed = p;
            if (p.Prefab == null)
                return;
            _ghost = (GameObject)PrefabUtility.InstantiatePrefab(p.Prefab);
            _ghost.name = "Aperçu : " + p.Name;
            _ghost.hideFlags = HideFlags.HideAndDontSave;
            foreach (Collider c in _ghost.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            SceneView.lastActiveSceneView?.Focus();
        }

        public void Disarm()
        {
            _armed = null;
            if (_ghost != null)
                DestroyImmediate(_ghost);
            _ghost = null;
        }

        // ---------- Pose dans la vue Scene ----------

        /// <summary>Point aimanté à la grille (x, z) ; y : sol touché + hauteur réglée.</summary>
        public static Vector3 SnapToGrid(Vector3 p, float step, float height) =>
            new Vector3(Mathf.Round(p.x / step) * step, p.y + height, Mathf.Round(p.z / step) * step);

        /// <summary>
        /// Aimant de sommets : décalage qui pose un coin de la boîte de la pièce sur le coin le plus proche d'une autre pièce, si
        /// l'écart est sous <paramref name="radius"/> (sinon zéro). Pur, testable.
        /// </summary>
        public static Vector3 VertexSnap(Bounds moving, IEnumerable<Bounds> others, float radius)
        {
            Vector3 best = Vector3.zero;
            float bestD = radius;
            Vector3[] mine = Corners(moving);
            foreach (Bounds o in others)
                foreach (Vector3 c in Corners(o))
                    foreach (Vector3 m in mine)
                    {
                        float d = Vector3.Distance(c, m);
                        if (d < bestD)
                        {
                            bestD = d;
                            best = c - m;
                        }
                    }

            return best;
        }

        private static Vector3[] Corners(Bounds b)
        {
            var r = new Vector3[8];
            for (int i = 0; i < 8; i++)
                r[i] = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            return r;
        }

        private static Bounds Bounds(GameObject go)
        {
            Bounds b = default;
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                if (!any) b = r.bounds;
                else b.Encapsulate(r.bounds);
                any = true;
            }

            return any ? b : new Bounds(go.transform.position, Vector3.one * 0.5f);
        }

        private void OnScene(SceneView view)
        {
            if (_armed == null || _ghost == null)
                return;
            Event e = Event.current;
            int id = GUIUtility.GetControlID(FocusType.Passive);
            if (e.type == EventType.Layout)
                HandleUtility.AddDefaultControl(id);

            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    Disarm();
                    Repaint();
                    e.Use();
                    return;
                }

                if (e.keyCode == KeyCode.R)
                {
                    _yaw += (e.shift ? -1f : 1f) * (e.control ? 15f : 90f);
                    e.Use();
                }
            }

            if (e.type == EventType.ScrollWheel && e.alt)
            {
                AtelierStage st = Stage;
                _height = Mathf.Max(0f, _height - Mathf.Sign(e.delta.y) * (st != null ? Mathf.Min(st.step, 1f) : 0.5f));
                e.Use();
                Repaint();
            }

            // Où pointe la souris : décor touché (pas l'aperçu), sinon le plan y = 0.
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            Vector3 hit = ray.GetPoint(30f);
            if (Physics.Raycast(ray, out RaycastHit h, 2000f, ~0, QueryTriggerInteraction.Ignore))
                hit = h.point;
            else if (new Plane(Vector3.up, Vector3.zero).Raycast(ray, out float t))
                hit = ray.GetPoint(t);
            AtelierStage stage = Stage;
            float step = stage != null ? stage.step : 1f;
            Vector3 pos = SnapToGrid(hit, step, _height);
            _ghost.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, _yaw, 0f));
            if (_snapVertices)
            {
                Bounds me = Bounds(_ghost);
                var others = new List<Bounds>();
                foreach (Renderer r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                    if (r.gameObject.scene.IsValid() && !r.transform.IsChildOf(_ghost.transform) && r.GetComponentInParent<AtelierStage>() == null
                        && Vector3.Distance(r.bounds.ClosestPoint(me.center), me.center) < me.extents.magnitude + 2f)
                        others.Add(r.bounds);
                _ghost.transform.position += VertexSnap(me, others, Mathf.Max(0.5f, step * 0.5f));
            }

            Handles.color = new Color(0.4f, 1f, 0.5f);
            Handles.DrawWireCube(Bounds(_ghost).center, Bounds(_ghost).size);
            Handles.Label(_ghost.transform.position + Vector3.up * (Bounds(_ghost).size.y + 0.5f),
                $"{_armed.Name}  {_yaw:0}°  +{_height:0.0} m", EditorStyles.whiteBoldLabel);

            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt)
            {
                Place();
                e.Use();
            }

            if (e.type == EventType.MouseMove)
                view.Repaint();
        }

        /// <summary>Pose la pièce armée là où est l'aperçu, dans le module ouvert (sinon à la racine de la scène).</summary>
        public GameObject Place()
        {
            if (_armed == null || _ghost == null)
                return null;
            RoomModule module = AtelierModules.OpenModule();
            var go = (GameObject)PrefabUtility.InstantiatePrefab(_armed.Prefab, module != null ? module.transform : null);
            go.transform.SetPositionAndRotation(_ghost.transform.position, _ghost.transform.rotation);
            Undo.RegisterCreatedObjectUndo(go, "Poser " + _armed.Name);
            Selection.activeGameObject = go;
            return go;
        }
    }
}
