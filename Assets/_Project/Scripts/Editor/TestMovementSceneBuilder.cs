using System.IO;
using ItHunts.Debugging;
using ItHunts.Input;
using ItHunts.Player;
using ItHunts.Settings;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Construit le banc d'essai du mouvement : réglages, prefab joueur, scène Test_Movement (greybox).
    /// Les assets de réglage existants ne sont jamais écrasés (ils contiennent les valeurs réglées).
    /// </summary>
    public static class TestMovementSceneBuilder
    {
        private const string SettingsDir = "Assets/_Project/Settings";
        private const string PrefabDir = "Assets/_Project/Prefabs";
        private const string MaterialDir = "Assets/_Project/Art/Greybox";
        private const string ActionsPath = "Assets/_Project/Input/PlayerControls.inputactions";
        private const string MovementPath = SettingsDir + "/Movement_Default.asset";
        private const string CameraPath = SettingsDir + "/Camera_Default.asset";
        private const string PlayerPrefabPath = PrefabDir + "/Player.prefab";
        private const string ScenePath = "Assets/Scenes/Test_Movement.unity";

        [MenuItem("ItHunts/Reconstruire le prefab joueur et la scène Test_Movement")]
        private static void BuildFromMenu()
        {
            if (EditorUtility.DisplayDialog("Test_Movement",
                    "Reconstruire le prefab Player et la scène Test_Movement ?\n" +
                    "Les modifications faites à la main dans ces deux fichiers seront perdues.\n" +
                    "Les réglages (Movement_Default, Camera_Default) sont conservés.", "Reconstruire", "Annuler"))
                Debug.Log(Build(overwrite: true));
        }

        /// <summary>Point d'entrée (aussi pour la CLI). Renvoie un résumé.</summary>
        public static string Build(bool overwrite = false)
        {
            SetFixedTimestep(1f / 60f);
            EnsureFolder(SettingsDir);
            EnsureFolder(PrefabDir);
            EnsureFolder(MaterialDir);

            var movement = LoadOrCreate<MovementSettings>(MovementPath);
            var camera = LoadOrCreate<CameraSettings>(CameraPath);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null)
                return $"ERREUR : {ActionsPath} introuvable.";

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null || overwrite)
                prefab = BuildPlayerPrefab(movement, camera, actions);

            if (File.Exists(ScenePath) && !overwrite)
                return $"OK (scène existante conservée : {ScenePath}). Pas fixe = {Time.fixedDeltaTime:0.#####} s.";

            BuildScene(prefab);
            return $"OK : {PlayerPrefabPath}, {ScenePath}. Pas fixe = {Time.fixedDeltaTime:0.#####} s.";
        }

        // ---------- Réglages projet ----------

        private static void SetFixedTimestep(float step)
        {
            // Unity 6 stocke le pas fixe comme une fraction exacte : on passe par Time.fixedDeltaTime,
            // puis on force l'écriture de ProjectSettings/TimeManager.asset.
            Time.fixedDeltaTime = step;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
            if (assets.Length > 0)
            {
                EditorUtility.SetDirty(assets[0]);
                AssetDatabase.SaveAssetIfDirty(assets[0]);
            }

            AssetDatabase.SaveAssets();
        }

        // ---------- Prefab joueur ----------

        private static GameObject BuildPlayerPrefab(MovementSettings movement, CameraSettings cameraSettings,
            InputActionAsset actions)
        {
            var root = new GameObject("Player");
            try
            {
                var controller = root.AddComponent<CharacterController>();
                controller.height = movement.capsuleHeight;
                controller.radius = movement.capsuleRadius;
                controller.center = new Vector3(0f, movement.capsuleHeight * 0.5f, 0f);
                controller.slopeLimit = movement.slopeLimit;
                controller.stepOffset = movement.stepOffset;
                controller.skinWidth = 0.04f;
                controller.minMoveDistance = 0f;

                var rig = new GameObject("ViewRig");
                rig.transform.SetParent(root.transform, false);
                rig.transform.localPosition = new Vector3(0f, movement.eyeHeight, 0f);

                var camGo = new GameObject("PlayerCamera");
                camGo.transform.SetParent(rig.transform, false);
                var unityCamera = camGo.AddComponent<Camera>();
                unityCamera.nearClipPlane = 0.05f;
                unityCamera.farClipPlane = 500f;
                unityCamera.clearFlags = CameraClearFlags.SolidColor;
                unityCamera.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
                camGo.AddComponent<CinemachineBrain>();
                camGo.AddComponent<AudioListener>();

                var vcamGo = new GameObject("CinemachineCamera");
                vcamGo.transform.SetParent(rig.transform, false);
                var vcam = vcamGo.AddComponent<CinemachineCamera>();
                LensSettings lens = vcam.Lens;
                lens.FieldOfView = cameraSettings.baseFov;
                lens.NearClipPlane = 0.05f;
                lens.FarClipPlane = 500f;
                vcam.Lens = lens;

                var playerInput = root.AddComponent<PlayerInput>();
                playerInput.actions = actions;
                playerInput.defaultActionMap = "Player";
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
                playerInput.camera = unityCamera;

                root.AddComponent<PlayerInputReader>();
                var motor = root.AddComponent<PlayerMotor>();
                var look = root.AddComponent<PlayerLook>();
                root.AddComponent<MovementDebugOverlay>();

                SetRef(motor, "settings", movement);
                SetRef(look, "settings", cameraSettings);
                SetRef(look, "viewRig", rig.transform);
                SetRef(look, "virtualCamera", vcam);

                GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
                return saved;
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
                throw new System.InvalidOperationException($"Champ {field} introuvable sur {target.GetType().Name}");
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Scène greybox ----------

        private static void BuildScene(GameObject playerPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.26f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.color = new Color(1f, 0.95f, 0.88f);
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            Material floorMat = Mat("Greybox_Floor", new Color(0.22f, 0.22f, 0.24f));
            Material lineMat = Mat("Greybox_Line", new Color(0.55f, 0.55f, 0.6f));
            Material blockMat = Mat("Greybox_Block", new Color(0.45f, 0.45f, 0.5f));
            Material platformMat = Mat("Greybox_Platform", new Color(0.32f, 0.42f, 0.55f));
            Material rampMat = Mat("Greybox_Ramp", new Color(0.55f, 0.42f, 0.3f));
            Material wallMat = Mat("Greybox_Wall", new Color(0.3f, 0.3f, 0.34f));

            var level = new GameObject("Level").transform;

            // Sol 80 × 80 m (x : -40..40, z : -20..60), dessus à y = 0.
            Box("Sol", level, new Vector3(0f, -0.5f, 20f), new Vector3(80f, 1f, 80f), floorMat);

            // Repères tous les 5 m (sans collision).
            var lines = new GameObject("Repères 5 m").transform;
            lines.SetParent(level, false);
            for (int z = -20; z <= 60; z += 5)
                Box($"z={z}", lines, new Vector3(0f, 0.005f, z), new Vector3(80f, 0.01f, 0.08f), lineMat, collider: false);
            for (int x = -40; x <= 40; x += 5)
                Box($"x={x}", lines, new Vector3(x, 0.005f, 20f), new Vector3(0.08f, 0.01f, 80f), lineMat, collider: false);

            // Murs d'enceinte.
            Box("Mur N", level, new Vector3(0f, 3f, 60.5f), new Vector3(80f, 6f, 1f), wallMat);
            Box("Mur S", level, new Vector3(0f, 3f, -20.5f), new Vector3(80f, 6f, 1f), wallMat);
            Box("Mur E", level, new Vector3(40.5f, 3f, 20f), new Vector3(1f, 6f, 82f), wallMat);
            Box("Mur O", level, new Vector3(-40.5f, 3f, 20f), new Vector3(1f, 6f, 82f), wallMat);

            // Blocs de hauteur : 0.5 / 1 / 1.3 / 1.5 / 2 m (x = -20).
            var blocks = new GameObject("Blocs de hauteur").transform;
            blocks.SetParent(level, false);
            float[] heights = { 0.5f, 1f, 1.3f, 1.5f, 2f };
            for (int i = 0; i < heights.Length; i++)
            {
                float h = heights[i];
                Box($"Bloc {h} m", blocks, new Vector3(-20f, h * 0.5f, 5f + i * 5f), new Vector3(2f, h, 2f), blockMat);
            }

            // Parcours de trous : plateformes à 2 m de haut, trous de 2 à 7 m (x = 10).
            var gaps = new GameObject("Trous 2 à 7 m").transform;
            gaps.SetParent(level, false);
            const float platformTop = 2f;
            for (int i = 0; i < 8; i++)   // escalier : marches de 0.25 m, 0.4 m de profondeur
            {
                float stepTop = 0.25f * (i + 1);
                Box($"Marche {i + 1}", gaps, new Vector3(10f, stepTop * 0.5f, 2.8f + i * 0.4f + 0.2f),
                    new Vector3(4f, stepTop, 0.4f), blockMat);
            }

            float zStart = 6f, length = 4f;
            Box("Départ", gaps, new Vector3(10f, platformTop * 0.5f, zStart + length * 0.5f),
                new Vector3(4f, platformTop, length), platformMat);
            float zEnd = zStart + length;
            for (int gap = 2; gap <= 7; gap++)
            {
                float z0 = zEnd + gap;
                const float pl = 3f;
                Box($"Après trou de {gap} m", gaps, new Vector3(10f, platformTop * 0.5f, z0 + pl * 0.5f),
                    new Vector3(4f, platformTop, pl), platformMat);
                zEnd = z0 + pl;
            }

            // Rampes de 15°, 30°, 45° (limite) et 50° (trop raide), x = 20..35.
            var ramps = new GameObject("Rampes").transform;
            ramps.SetParent(level, false);
            float[] angles = { 15f, 30f, 45f, 50f };
            for (int i = 0; i < angles.Length; i++)
                Ramp($"Rampe {angles[i]}°", ramps, 20f + i * 5f, 5f, angles[i], 8f, 3f, rampMat);

            BuildSlideArea(level, blockMat, platformMat, rampMat, wallMat);

            // Joueur.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
        }

        /// <summary>
        /// Couloir de glissade de 50 m (x = −32) avec un passage bas de 1.1 m, et longue rampe de 20°
        /// (x = 0, de z = 28.5 à 45) qui monte à une plateforme de 6 m : on la redescend en glissant.
        /// </summary>
        private static void BuildSlideArea(Transform level, Material blockMat, Material platformMat,
            Material rampMat, Material wallMat)
        {
            var corridor = new GameObject("Couloir de glissade 50 m").transform;
            corridor.SetParent(level, false);
            const float cx = -32f, z0 = -15f, z1 = 35f, inner = 4f, wallH = 3f;
            float len = z1 - z0, zc = (z0 + z1) * 0.5f;
            Box("Mur gauche", corridor, new Vector3(cx - inner * 0.5f - 0.5f, wallH * 0.5f, zc), new Vector3(1f, wallH, len), wallMat);
            Box("Mur droit", corridor, new Vector3(cx + inner * 0.5f + 0.5f, wallH * 0.5f, zc), new Vector3(1f, wallH, len), wallMat);
            // Passage bas : dalle dont le dessous est à 1.1 m (capsule de glissade : 0.9 m).
            const float clearance = 1.1f, slab = 1.9f;
            Box("Passage bas 1.1 m", corridor, new Vector3(cx, clearance + slab * 0.5f, 12f), new Vector3(inner, slab, 6f), blockMat);

            var hill = new GameObject("Longue rampe 20°").transform;
            hill.SetParent(level, false);
            const float top = 6f, angle = 20f, width = 8f;
            float horizontal = top / Mathf.Tan(angle * Mathf.Deg2Rad);   // ≈ 16.5 m
            float slopeLength = top / Mathf.Sin(angle * Mathf.Deg2Rad);  // ≈ 17.5 m
            const float platformZ0 = 45f, platformLen = 10f;
            Ramp("Rampe 20°", hill, 0f, platformZ0 - horizontal, angle, slopeLength, width, rampMat);
            Box("Plateforme 6 m", hill, new Vector3(0f, top * 0.5f, platformZ0 + platformLen * 0.5f),
                new Vector3(width, top, platformLen), platformMat);
        }

        private static void Ramp(string name, Transform parent, float x, float zBottom, float angle, float length,
            float width, Material mat)
        {
            const float thickness = 0.5f;
            float a = angle * Mathf.Deg2Rad;
            // Le bas de la face supérieure touche le sol en (x, 0, zBottom).
            float cy = length * 0.5f * Mathf.Sin(a) - thickness * 0.5f * Mathf.Cos(a);
            float cz = zBottom + length * 0.5f * Mathf.Cos(a) + thickness * 0.5f * Mathf.Sin(a);
            GameObject go = Box(name, parent, new Vector3(x, cy, cz), new Vector3(width, thickness, length), mat);
            go.transform.rotation = Quaternion.Euler(-angle, 0f, 0f);
        }

        private static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat,
            bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!collider)
                Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
            return go;
        }

        private static Material Mat(string name, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // ---------- Utilitaires ----------

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void AddSceneToBuild(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path))
                return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
