using System.IO;
using ItHunts.Debugging;
using ItHunts.Hud;
using ItHunts.Input;
using ItHunts.Orbs;
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
        private const string MagnetismPath = SettingsDir + "/Magnetism_Default.asset";
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

        /// <summary>Reconstruit seulement le prefab joueur (la scène garde son instance, liée au prefab).</summary>
        public static string RebuildPlayerPrefab()
        {
            var movement = LoadOrCreate<MovementSettings>(MovementPath);
            var camera = LoadOrCreate<CameraSettings>(CameraPath);
            var magnetism = LoadOrCreate<MagnetismOrbSettings>(MagnetismPath);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null)
                return $"ERREUR : {ActionsPath} introuvable.";
            BuildPlayerPrefab(movement, camera, magnetism, actions);
            return $"OK : {PlayerPrefabPath} reconstruit.";
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
            var magnetism = LoadOrCreate<MagnetismOrbSettings>(MagnetismPath);
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null)
                return $"ERREUR : {ActionsPath} introuvable.";

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null || overwrite)
                prefab = BuildPlayerPrefab(movement, camera, magnetism, actions);

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
            MagnetismOrbSettings magnetism, InputActionAsset actions)
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
                unityCamera.farClipPlane = 1000f;
                unityCamera.clearFlags = CameraClearFlags.SolidColor;
                unityCamera.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
                camGo.AddComponent<CinemachineBrain>();
                camGo.AddComponent<AudioListener>();

                // Orbe tenue en bas à droite de la vue, et trait de traction.
                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "Orbe (vue)";
                Object.DestroyImmediate(orb.GetComponent<Collider>());
                orb.transform.SetParent(camGo.transform, false);
                orb.transform.localPosition = new Vector3(0.28f, -0.24f, 0.55f);
                orb.transform.localScale = Vector3.one * 0.09f;
                var orbRenderer = orb.GetComponent<MeshRenderer>();
                orbRenderer.sharedMaterial = UnlitMat("Orb_View", "Universal Render Pipeline/Unlit", Color.white);
                orbRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                orbRenderer.receiveShadows = false;

                var lineGo = new GameObject("Trait de traction");
                lineGo.transform.SetParent(camGo.transform, false);
                var line = lineGo.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.startWidth = 0.03f;
                line.endWidth = 0.06f;
                line.sharedMaterial = UnlitMat("Orb_Line", "Universal Render Pipeline/Particles/Unlit", Color.white);
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.enabled = false;

                // Marquage de la répulsion : point visé et impact (placés dans le monde à chaque image).
                Material markerMat = UnlitMat("Orb_Marker", "Universal Render Pipeline/Unlit", Color.white);
                Renderer aimMarker = Marker("Marque de répulsion (visée)", root.transform, 0.35f, markerMat);
                Renderer impactMarker = Marker("Marque de répulsion (impact)", root.transform, 0.6f, markerMat);

                var vcamGo = new GameObject("CinemachineCamera");
                vcamGo.transform.SetParent(rig.transform, false);
                var vcam = vcamGo.AddComponent<CinemachineCamera>();
                LensSettings lens = vcam.Lens;
                lens.FieldOfView = cameraSettings.baseFov;
                lens.NearClipPlane = 0.05f;
                lens.FarClipPlane = 1000f;
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
                root.AddComponent<DebugTeleporter>();
                root.AddComponent<MovementHud>();
                var orbView = root.AddComponent<MagnetismOrbView>();

                SetRef(motor, "settings", movement);
                SetRef(motor, "magnetism", magnetism);
                SetRef(orbView, "orbRenderer", orbRenderer);
                SetRef(orbView, "line", line);
                SetRef(orbView, "aimMarker", aimMarker);
                SetRef(orbView, "impactMarker", impactMarker);
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

        /// <summary>Disque plat sans collision (cylindre écrasé), masqué au départ.</summary>
        private static Renderer Marker(string name, Transform parent, float diameter, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(diameter, 0.005f, diameter);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.enabled = false;
            return r;
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

            BuildGround(level, floorMat, lineMat, wallMat);

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

            Material landmarkMat = Mat("Greybox_Landmark", new Color(0.85f, 0.45f, 0.15f));
            Material laneMat = Mat("Greybox_Lane", new Color(0.9f, 0.8f, 0.2f));
            Material tallWallMat = Mat("Greybox_TallWall", new Color(0.25f, 0.32f, 0.45f));
            BuildOpenArea(level, landmarkMat);
            BuildBunnyHopLane(level, laneMat, lineMat);
            BuildLongSlopes(level, rampMat, platformMat);
            BuildTallWalls(level, tallWallMat);
            BuildGapsAndVerticality(level, platformMat, rampMat, blockMat);
            Material magneticMat = Mat("Greybox_Magnetic", new Color(0.15f, 0.45f, 1f));
            BuildMagnetismArea(level, magneticMat, platformMat, rampMat, wallMat, lineMat, landmarkMat);
            BuildGreatHall(level, wallMat, platformMat, magneticMat, lineMat);
            BuildTeleportPoints();

            // Joueur.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
        }

        // ---------- Grand terrain (400 × 400 m) ----------
        //
        // Plan (vue de dessus, +z = nord, départ en (0, 0, −10) face au nord) :
        //   x −200..−100 : longues pentes (10°, 20°, 30°)
        //   x  −40.. 40, z −20..60 : zone de base (blocs, trous, rampes, couloir de glissade)
        //   x −100..100, z  80..300 : grande zone ouverte (200 × 220 m)
        //   x  110..170, z  80..300 : grands murs (paires de 6 et 10 m, mur seul de 200 m, cheminée)
        //   x  180..190, z −30..350 : ligne droite de bunny hop (380 m)
        //   x −100..100, z 305..355 : écarts larges et verticalité
        //   x   45..95,  z −30..60  : magnétisme (cibles en bleu, mais toute surface répond)
        //   x −100..−50, z   0..50  : grande salle de 45 m de haut (portée de la traction)

        private const float GroundMinX = -200f, GroundMaxX = 200f, GroundMinZ = -40f, GroundMaxZ = 360f;

        private static void BuildGround(Transform level, Material floorMat, Material lineMat, Material wallMat)
        {
            float sizeX = GroundMaxX - GroundMinX, sizeZ = GroundMaxZ - GroundMinZ;
            float cx = (GroundMinX + GroundMaxX) * 0.5f, cz = (GroundMinZ + GroundMaxZ) * 0.5f;
            Box("Sol 400 × 400 m", level, new Vector3(cx, -0.5f, cz), new Vector3(sizeX, 1f, sizeZ), floorMat);

            // Grille tous les 10 m sur tout le terrain, et tous les 5 m dans la zone de base (sans collision).
            var grid = new GameObject("Grille 10 m").transform;
            grid.SetParent(level, false);
            for (float z = GroundMinZ; z <= GroundMaxZ; z += 10f)
                Box($"z={z}", grid, new Vector3(cx, 0.005f, z), new Vector3(sizeX, 0.01f, 0.1f), lineMat, collider: false);
            for (float x = GroundMinX; x <= GroundMaxX; x += 10f)
                Box($"x={x}", grid, new Vector3(x, 0.005f, cz), new Vector3(0.1f, 0.01f, sizeZ), lineMat, collider: false);

            var fine = new GameObject("Repères 5 m (zone de base)").transform;
            fine.SetParent(level, false);
            for (int z = -15; z <= 55; z += 10)
                Box($"z={z}", fine, new Vector3(0f, 0.005f, z), new Vector3(80f, 0.01f, 0.06f), lineMat, collider: false);
            for (int x = -35; x <= 35; x += 10)
                Box($"x={x}", fine, new Vector3(x, 0.005f, 20f), new Vector3(0.06f, 0.01f, 80f), lineMat, collider: false);

            // Enceinte de 12 m.
            var walls = new GameObject("Enceinte").transform;
            walls.SetParent(level, false);
            const float h = 12f;
            Box("Mur N", walls, new Vector3(cx, h * 0.5f, GroundMaxZ + 0.5f), new Vector3(sizeX + 2f, h, 1f), wallMat);
            Box("Mur S", walls, new Vector3(cx, h * 0.5f, GroundMinZ - 0.5f), new Vector3(sizeX + 2f, h, 1f), wallMat);
            Box("Mur E", walls, new Vector3(GroundMaxX + 0.5f, h * 0.5f, cz), new Vector3(1f, h, sizeZ), wallMat);
            Box("Mur O", walls, new Vector3(GroundMinX - 0.5f, h * 0.5f, cz), new Vector3(1f, h, sizeZ), wallMat);
        }

        /// <summary>Points de téléportation de debug : F4 départ, F5 à F11 zones (position des pieds, yaw).</summary>
        private static void BuildTeleportPoints()
        {
            var root = new GameObject("Points de téléportation (debug)");
            var points = root.AddComponent<DebugTeleportPoints>();
            (string label, Vector3 pos, float yaw)[] defs =
            {
                ("Départ", new Vector3(0f, 0f, -10f), 0f),
                ("Grande zone ouverte", new Vector3(0f, 0f, 90f), 0f),
                ("Ligne droite bunny hop", new Vector3(185f, 0f, -25f), 0f),
                ("Longues pentes", new Vector3(-160f, 0f, -32f), 0f),
                ("Grands murs", new Vector3(121f, 0f, 95f), 0f),
                ("Écarts larges et verticalité", new Vector3(-99f, 4f, 315f), 90f),
                ("Magnétisme", new Vector3(60f, 0f, -30f), 0f),
                ("Grande salle 45 m", new Vector3(-56f, 0f, 25f), -90f),
            };
            points.points = new DebugTeleportPoints.Point[defs.Length];
            for (int i = 0; i < defs.Length; i++)
            {
                var t = new GameObject($"F{4 + i} {defs[i].label}").transform;
                t.SetParent(root.transform, false);
                t.SetPositionAndRotation(defs[i].pos, Quaternion.Euler(0f, defs[i].yaw, 0f));
                points.points[i] = new DebugTeleportPoints.Point { label = defs[i].label, target = t };
            }
        }

        /// <summary>Zone ouverte de 200 × 220 m, vide ; quatre tours repères de 30 m aux coins.</summary>
        private static void BuildOpenArea(Transform level, Material landmarkMat)
        {
            var area = new GameObject("Grande zone ouverte 200 × 220 m").transform;
            area.SetParent(level, false);
            Vector3[] corners = { new(-101.5f, 0f, 78.5f), new(101.5f, 0f, 78.5f), new(-101.5f, 0f, 301.5f), new(101.5f, 0f, 301.5f) };  // juste hors de la zone
            foreach (Vector3 c in corners)
                Box($"Tour repère ({c.x}, {c.z})", area, c + new Vector3(0f, 15f, 0f), new Vector3(3f, 30f, 3f), landmarkMat);
        }

        /// <summary>Ligne droite de 380 m (x = 185) : bandes tous les 25 m, plus marquées tous les 100 m.</summary>
        private static void BuildBunnyHopLane(Transform level, Material laneMat, Material lineMat)
        {
            var lane = new GameObject("Ligne droite bunny hop 380 m").transform;
            lane.SetParent(level, false);
            const float x = 185f, width = 10f, z0 = -30f, z1 = 350f;
            Box("Bord gauche", lane, new Vector3(x - width * 0.5f, 0.01f, (z0 + z1) * 0.5f), new Vector3(0.3f, 0.02f, z1 - z0), laneMat, collider: false);
            Box("Bord droit", lane, new Vector3(x + width * 0.5f, 0.01f, (z0 + z1) * 0.5f), new Vector3(0.3f, 0.02f, z1 - z0), laneMat, collider: false);
            for (float z = z0; z <= z1 + 0.1f; z += 25f)
            {
                bool major = Mathf.Approximately((z - z0) % 100f, 0f);
                Box($"{z - z0} m", lane, new Vector3(x, 0.012f, z), new Vector3(width, 0.02f, major ? 1f : 0.3f),
                    major ? laneMat : lineMat, collider: false);
            }
        }

        /// <summary>Longues pentes de 10°, 20° et 30° (ouest), chacune menant à une plateforme de 20 m.</summary>
        private static void BuildLongSlopes(Transform level, Material rampMat, Material platformMat)
        {
            var slopes = new GameObject("Longues pentes").transform;
            slopes.SetParent(level, false);
            (float x, float angle, float length)[] defs = { (-180f, 10f, 150f), (-160f, 20f, 90f), (-140f, 30f, 60f) };
            const float zBottom = -20f, width = 14f, platformDepth = 20f;
            foreach (var (x, angle, length) in defs)
            {
                float a = angle * Mathf.Deg2Rad;
                float rise = length * Mathf.Sin(a), run = length * Mathf.Cos(a);
                Ramp($"Pente {angle}° ({length} m, +{rise:0} m)", slopes, x, zBottom, angle, length, width, rampMat);
                Box($"Sommet pente {angle}°", slopes, new Vector3(x, rise * 0.5f, zBottom + run + platformDepth * 0.5f),
                    new Vector3(width, rise, platformDepth), platformMat);
            }
        }

        /// <summary>Murs hauts et longs pour le wall slide (entre la zone ouverte et la ligne de bunny hop).</summary>
        private static void BuildTallWalls(Transform level, Material wallMat)
        {
            var walls = new GameObject("Grands murs").transform;
            walls.SetParent(level, false);
            const float zA = 100f, zB = 220f, h = 20f;
            float len = zB - zA, zc = (zA + zB) * 0.5f;
            // Paire à 6 m (bords intérieurs) et paire à 10 m.
            Box("Paire 6 m - ouest", walls, new Vector3(117.5f, h * 0.5f, zc), new Vector3(1f, h, len), wallMat);
            Box("Paire 6 m - est", walls, new Vector3(124.5f, h * 0.5f, zc), new Vector3(1f, h, len), wallMat);
            Box("Paire 10 m - ouest", walls, new Vector3(137.5f, h * 0.5f, zc), new Vector3(1f, h, len), wallMat);
            Box("Paire 10 m - est", walls, new Vector3(148.5f, h * 0.5f, zc), new Vector3(1f, h, len), wallMat);
            // Mur seul de 200 m, 15 m de haut.
            Box("Mur seul 200 m", walls, new Vector3(162f, 7.5f, 190f), new Vector3(1f, 15f, 200f), wallMat);
            // Cheminée de 3 m de large, 30 m de haut.
            Box("Cheminée - ouest", walls, new Vector3(128.5f, 15f, 250f), new Vector3(1f, 30f, 8f), wallMat);
            Box("Cheminée - est", walls, new Vector3(132.5f, 15f, 250f), new Vector3(1f, 30f, 8f), wallMat);
        }

        /// <summary>
        /// Nord : plateformes à 4 m séparées d'écarts de 8 à 22 m (accès par une rampe de 20°), et un
        /// escalier de plateformes qui monte de 2 m en 2 m jusqu'à 16 m.
        /// </summary>
        private static void BuildGapsAndVerticality(Transform level, Material platformMat, Material rampMat,
            Material blockMat)
        {
            var gaps = new GameObject("Écarts larges 8 à 22 m").transform;
            gaps.SetParent(level, false);
            const float top = 4f, zc = 315f, depth = 10f, platLen = 6f;
            float start = -95f;
            float rampRun = top / Mathf.Tan(20f * Mathf.Deg2Rad);
            Ramp("Accès 20°", gaps, start - 4f, zc - depth * 0.5f - rampRun, 20f, top / Mathf.Sin(20f * Mathf.Deg2Rad), 8f, rampMat);
            Box("Départ", gaps, new Vector3(start - 4f, top * 0.5f, zc), new Vector3(8f, top, depth), platformMat);
            float x = start;
            foreach (float gap in new[] { 8f, 10f, 12f, 15f, 18f, 22f })
            {
                x += gap;
                Box($"Après écart de {gap} m", gaps, new Vector3(x + platLen * 0.5f, top * 0.5f, zc),
                    new Vector3(platLen, top, depth), platformMat);
                x += platLen;
            }

            var vertical = new GameObject("Verticalité 2 à 16 m").transform;
            vertical.SetParent(level, false);
            for (int i = 0; i < 8; i++)
            {
                float height = 2f * (i + 1);
                Box($"Plateforme {height} m", vertical, new Vector3(20f + i * 9f, height * 0.5f, 342f),
                    new Vector3(5f, height, 5f), i % 2 == 0 ? platformMat : blockMat);
            }
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

        /// <summary>
        /// Zone du magnétisme, départ F10 en (60, 0, −30) face au nord. Toute surface répond au magnétisme ; le bleu
        /// sert seulement de repère (cibles prévues) :
        /// - mur bleu de 14 m derrière une plateforme de 6 m, repères au sol à 10, 20 et 25 m du mur ;
        /// - mur gris (répond aussi : toute surface) ;
        /// - plafond bleu à 14 m ; puits de 25 m ;
        /// - dalles bleues au sol (rocket jump) près de plateformes de 4 et 8 m ;
        /// - gouffre de 30 m entre deux plateformes de 6 m, bloc flottant au milieu.
        /// </summary>
        private static void BuildMagnetismArea(Transform level, Material magneticMat, Material platformMat,
            Material rampMat, Material wallMat, Material lineMat, Material markMat)
        {
            var area = new GameObject("Magnétisme").transform;
            area.SetParent(level, false);

            Box("Plateforme 6 m", area, new Vector3(60f, 3f, 5f), new Vector3(10f, 6f, 6f), platformMat);
            Box("Mur bleu 14 m", area, new Vector3(60f, 7f, 8.5f), new Vector3(10f, 14f, 1f), magneticMat);
            foreach (float d in new[] { 10f, 20f, 25f })   // face du mur en z = 8
                Box($"Repère {d} m du mur", area, new Vector3(60f, 0.012f, 8f - d), new Vector3(10f, 0.02f, d >= 25f ? 0.6f : 0.3f),
                    d >= 25f ? markMat : lineMat, collider: false);
            Box("Mur gris", area, new Vector3(71f, 7f, 8.5f), new Vector3(8f, 14f, 1f), wallMat);

            Box("Plafond bleu 14 m", area, new Vector3(88f, 14.5f, 5f), new Vector3(10f, 1f, 10f), magneticMat);

            Box("Puits 25 m - paroi ouest", area, new Vector3(87.5f, 12.5f, -22f), new Vector3(1f, 25f, 6f), magneticMat);
            Box("Puits 25 m - paroi est", area, new Vector3(92.5f, 12.5f, -22f), new Vector3(1f, 25f, 6f), magneticMat);
            Box("Puits 25 m - fond", area, new Vector3(90f, 12.5f, -18.5f), new Vector3(6f, 25f, 1f), wallMat);

            Box("Dalle bleue 1", area, new Vector3(48f, 0.05f, -25f), new Vector3(4f, 0.1f, 4f), magneticMat);
            Box("Dalle bleue 2", area, new Vector3(48f, 0.05f, -21f), new Vector3(4f, 0.1f, 2f), magneticMat);
            Box("Plateforme 4 m", area, new Vector3(48f, 2f, -17f), new Vector3(6f, 4f, 4f), platformMat);
            Box("Plateforme 8 m", area, new Vector3(48f, 4f, -9f), new Vector3(6f, 8f, 4f), platformMat);

            const float top = 6f, angle = 20f;
            float run = top / Mathf.Tan(angle * Mathf.Deg2Rad);
            Ramp("Accès au gouffre 20°", area, 52f, 45f - run, angle, top / Mathf.Sin(angle * Mathf.Deg2Rad), 8f, rampMat);
            Box("Gouffre 30 m - départ", area, new Vector3(52f, top * 0.5f, 50f), new Vector3(8f, top, 10f), platformMat);
            Box("Gouffre 30 m - bloc flottant", area, new Vector3(71f, 10f, 50f), new Vector3(2f, 2f, 2f), magneticMat);
            Box("Gouffre 30 m - arrivée (bleue)", area, new Vector3(90f, top * 0.5f, 50f), new Vector3(8f, top, 10f), magneticMat);
        }

        /// <summary>
        /// Grande salle fermée de 50 × 50 m et 45 m de haut (x −100..−50, z 0..50), porte de 8 m à l'est. Repères de
        /// hauteur tous les 10 m sur le mur ouest ; pilier central de 40 m ; corniche à 20 m ; plateforme à 30 m ;
        /// blocs flottants à 15, 25 et 35 m. Éclairée par des lampes (le toit coupe le soleil).
        /// </summary>
        private static void BuildGreatHall(Transform level, Material wallMat, Material platformMat, Material targetMat,
            Material lineMat)
        {
            var hall = new GameObject("Grande salle 45 m").transform;
            hall.SetParent(level, false);
            const float x0 = -100f, x1 = -50f, z0 = 0f, z1 = 50f, h = 45f, door = 8f, doorH = 8f;
            float cx = (x0 + x1) * 0.5f, cz = (z0 + z1) * 0.5f, size = x1 - x0;
            Box("Mur ouest", hall, new Vector3(x0 - 0.5f, h * 0.5f, cz), new Vector3(1f, h, size + 2f), wallMat);
            Box("Mur nord", hall, new Vector3(cx, h * 0.5f, z1 + 0.5f), new Vector3(size + 2f, h, 1f), wallMat);
            Box("Mur sud", hall, new Vector3(cx, h * 0.5f, z0 - 0.5f), new Vector3(size + 2f, h, 1f), wallMat);
            float doorZ0 = cz - door * 0.5f, doorZ1 = cz + door * 0.5f;
            Box("Mur est (sud)", hall, new Vector3(x1 + 0.5f, h * 0.5f, (z0 + doorZ0) * 0.5f), new Vector3(1f, h, doorZ0 - z0), wallMat);
            Box("Mur est (nord)", hall, new Vector3(x1 + 0.5f, h * 0.5f, (doorZ1 + z1) * 0.5f), new Vector3(1f, h, z1 - doorZ1), wallMat);
            Box("Mur est (au-dessus de la porte)", hall, new Vector3(x1 + 0.5f, (doorH + h) * 0.5f, cz), new Vector3(1f, h - doorH, door), wallMat);
            Box("Toit à 45 m", hall, new Vector3(cx, h + 0.5f, cz), new Vector3(size + 2f, 1f, size + 2f), wallMat);

            for (int y = 10; y < h; y += 10)
                Box($"Repère {y} m", hall, new Vector3(x0 + 0.03f, y, cz), new Vector3(0.06f, 0.3f, size), lineMat, collider: false);

            Box("Pilier central 40 m", hall, new Vector3(cx, 20f, cz), new Vector3(3f, 40f, 3f), platformMat);
            Box("Corniche à 20 m", hall, new Vector3(x0 + 3f, 19.5f, cz), new Vector3(6f, 1f, 12f), platformMat);
            Box("Plateforme à 30 m", hall, new Vector3(cx, 29.5f, z1 - 4f), new Vector3(8f, 1f, 8f), platformMat);
            Box("Bloc flottant 15 m", hall, new Vector3(x0 + 12f, 15f, z0 + 12f), new Vector3(2f, 2f, 2f), targetMat);
            Box("Bloc flottant 25 m", hall, new Vector3(x1 - 12f, 25f, z0 + 14f), new Vector3(2f, 2f, 2f), targetMat);
            Box("Bloc flottant 35 m", hall, new Vector3(x0 + 12f, 35f, z1 - 12f), new Vector3(2f, 2f, 2f), targetMat);

            foreach (Vector3 p in new[] { new Vector3(cx - 15f, 40f, cz - 15f), new Vector3(cx + 15f, 40f, cz - 15f),
                         new Vector3(cx - 15f, 40f, cz + 15f), new Vector3(cx + 15f, 40f, cz + 15f), new Vector3(cx, 12f, cz - 12f) })
            {
                var lamp = new GameObject($"Lampe ({p.x}, {p.y}, {p.z})");
                lamp.transform.SetParent(hall, false);
                lamp.transform.localPosition = p;
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 45f;
                light.intensity = 6f;
                light.color = new Color(1f, 0.9f, 0.75f);
                light.shadows = LightShadows.None;
            }
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

        private static Material UnlitMat(string name, string shaderName, Color color)
        {
            string path = $"{MaterialDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find(shaderName)) { name = name };
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
