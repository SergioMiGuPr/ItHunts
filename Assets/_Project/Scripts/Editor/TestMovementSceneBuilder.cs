using System.IO;
using ItHunts.Artifacts;
using ItHunts.Audio;
using ItHunts.Combat;
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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ItHunts.EditorTools
{
    /// <summary>
    /// Construit le banc d'essai du mouvement : réglages, prefab joueur, scène Test_Movement (greybox).
    /// Les assets de réglage existants ne sont jamais écrasés (ils contiennent les valeurs réglées).
    /// </summary>
    public static class TestMovementSceneBuilder
    {
        internal const string SettingsDir = "Assets/_Project/Settings";
        private const string PrefabDir = "Assets/_Project/Prefabs";
        internal const string MaterialDir = "Assets/_Project/Art/Greybox";
        private const string ActionsPath = "Assets/_Project/Input/PlayerControls.inputactions";
        private const string MovementPath = SettingsDir + "/Movement_Default.asset";
        private const string CameraPath = SettingsDir + "/Camera_Default.asset";
        private const string MagnetismPath = SettingsDir + "/Magnetism_Default.asset";
        private const string FeedbackPath = SettingsDir + "/MagnetismFeedback_Default.asset";
        private const string TelekinesisPath = SettingsDir + "/Telekinesis_Default.asset";
        private const string TkFeedbackPath = SettingsDir + "/TelekinesisFeedback_Default.asset";
        internal const string DamagePath = SettingsDir + "/Damage_Default.asset";
        private const string DummyPath = SettingsDir + "/TrainingDummy_Default.asset";
        private const string DamageFeedbackPath = SettingsDir + "/DamageFeedback_Default.asset";
        private const string FpsViewPath = SettingsDir + "/FpsView_Default.asset";
        private const string HudPath = SettingsDir + "/Hud_Default.asset";
        private const string BarrelPath = SettingsDir + "/Barrel_Default.asset";
        private const string ArtifactPoolPath = SettingsDir + "/ArtifactPool_Default.asset";
        private const string CoopPath = SettingsDir + "/Coop_Default.asset";
        private const string PlayerHealthPath = SettingsDir + "/PlayerHealth_Default.asset";
        private const int FirstPersonLayerStart = 8;    // couches 8 à 11 : FirstPersonP1..P4 (mains de chaque joueur)
        internal const string PostProcessPath = SettingsDir + "/PostProcess_TestMovement.asset";
        internal const string PlayerPrefabPath = PrefabDir + "/Player.prefab";
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
            var feedback = LoadOrCreate<MagnetismFeedbackSettings>(FeedbackPath);
            var tk = new TelekinesisAssets
            {
                Orb = LoadOrCreate<TelekinesisOrbSettings>(TelekinesisPath),
                Feedback = LoadOrCreate<TelekinesisFeedbackSettings>(TkFeedbackPath),
            };
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null)
                return $"ERREUR : {ActionsPath} introuvable.";
            EnsureFirstPersonLayers();
            EnsureFirstPersonRendering(LoadOrCreate<FpsViewSettings>(FpsViewPath));
            BuildPlayerPrefab(movement, camera, magnetism, feedback, tk, actions);
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
            var feedback = LoadOrCreate<MagnetismFeedbackSettings>(FeedbackPath);
            var tk = new TelekinesisAssets
            {
                Orb = LoadOrCreate<TelekinesisOrbSettings>(TelekinesisPath),
                Feedback = LoadOrCreate<TelekinesisFeedbackSettings>(TkFeedbackPath),
            };
            var actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            if (actions == null)
                return $"ERREUR : {ActionsPath} introuvable.";

            EnsureFirstPersonLayers();
            EnsureFirstPersonRendering(LoadOrCreate<FpsViewSettings>(FpsViewPath));
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (prefab == null || overwrite)
                prefab = BuildPlayerPrefab(movement, camera, magnetism, feedback, tk, actions);

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

        /// <summary>Couches des mains, une par joueur (écran partagé : chaque caméra des mains ne dessine que les siennes).</summary>
        internal static void EnsureFirstPersonLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            for (int i = 0; i < 4; i++)
            {
                SerializedProperty layer = layers.GetArrayElementAtIndex(FirstPersonLayerStart + i);
                string name = FirstPersonView.LayerPrefix + (i + 1);
                if (layer.stringValue != name)
                {
                    if (!string.IsNullOrEmpty(layer.stringValue))
                        throw new System.InvalidOperationException($"La couche {FirstPersonLayerStart + i} est déjà prise : {layer.stringValue}");
                    layer.stringValue = name;
                }
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        private const string FeaturePrefix = "ItHunts Mains";

        /// <summary>
        /// Option 2 de la vue FPS (D-028), sur chaque renderer URP du projet : les passes normales ignorent les couches des
        /// mains ; après les transparents, une passe efface la profondeur, une passe « Render Objects » dessine les mains
        /// (file opaque) avec un champ de vision de viewmodelFov, puis une autre dessine le HUD (file transparente, couches
        /// des mains, sans changer la caméra). Idempotent : les passes « ItHunts Mains » existantes sont remplacées.
        /// </summary>
        public static void EnsureFirstPersonRendering(FpsViewSettings fps)
        {
            int mask = 0;
            for (int i = 0; i < 4; i++)
                mask |= 1 << (FirstPersonLayerStart + i);
            foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.StartsWith("Assets/"))
                    continue;                                            // jamais les renderers des packages (lecture seule)
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
                if (data == null)
                    continue;
                data.opaqueLayerMask &= ~mask;
                data.transparentLayerMask &= ~mask;

                // Passes des mains : réutilisées si elles existent déjà (mêmes identifiants : pas de modification inutile du
                // renderer partagé), créées sinon ; les autres passes « ItHunts Mains » sont retirées.
                string clearName = FeaturePrefix + " : profondeur effacée";
                string handsName = FeaturePrefix + " : mains";
                string hudName = FeaturePrefix + " : HUD";
                string glassName = FeaturePrefix + " : transparents";
                var clear = Reuse<FirstPersonDepthClearFeature>(data, clearName);
                var hands = Reuse<RenderObjects>(data, handsName);
                var glass = Reuse<RenderObjects>(data, glassName);
                var hud = Reuse<RenderObjects>(data, hudName);
                for (int i = data.rendererFeatures.Count - 1; i >= 0; i--)
                {
                    ScriptableRendererFeature f = data.rendererFeatures[i];
                    if (f != null && f.name.StartsWith(FeaturePrefix) && f != clear && f != hands && f != glass && f != hud)
                    {
                        data.rendererFeatures.RemoveAt(i);
                        AssetDatabase.RemoveObjectFromAsset(f);
                        Object.DestroyImmediate(f, true);
                    }
                }

                hands.settings.Event = RenderPassEvent.AfterRenderingTransparents;
                hands.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
                hands.settings.filterSettings.LayerMask = mask;
                hands.settings.overrideDepthState = true;
                hands.settings.depthCompareFunction = UnityEngine.Rendering.CompareFunction.LessEqual;
                hands.settings.enableWrite = true;
                hands.settings.cameraSettings.overrideCamera = true;
                hands.settings.cameraSettings.restoreCamera = true;
                hands.settings.cameraSettings.cameraFieldOfView = fps.viewmodelFov;
                hands.settings.cameraSettings.offset = Vector4.zero;
                // Contour des mains et des artefacts (coque inversée, D-067) : la passe des mains dessine aussi « ItHuntsOutline ».
                hands.settings.filterSettings.PassNames = new[] { "UniversalForward", "UniversalForwardOnly", "SRPDefaultUnlit", "ItHuntsOutline" };
                // Transparents des mains (verre de la lanterne, MATERIAUX.md) : même caméra que les mains, sans écrire la profondeur.
                glass.settings.Event = RenderPassEvent.AfterRenderingTransparents;
                glass.settings.filterSettings.RenderQueueType = RenderQueueType.Transparent;
                glass.settings.filterSettings.LayerMask = mask;
                glass.settings.filterSettings.PassNames = new[] { "UniversalForward", "UniversalForwardOnly" };
                glass.settings.overrideDepthState = true;
                glass.settings.depthCompareFunction = UnityEngine.Rendering.CompareFunction.LessEqual;
                glass.settings.enableWrite = false;
                glass.settings.cameraSettings.overrideCamera = true;
                glass.settings.cameraSettings.restoreCamera = true;
                glass.settings.cameraSettings.cameraFieldOfView = fps.viewmodelFov;
                glass.settings.cameraSettings.offset = Vector4.zero;
                hud.settings.Event = RenderPassEvent.AfterRenderingTransparents;
                hud.settings.filterSettings.RenderQueueType = RenderQueueType.Transparent;
                hud.settings.filterSettings.LayerMask = mask;
                hud.settings.filterSettings.PassNames = new[] { "SRPDefaultUnlit" };   // interface seulement (pas le verre)

                // Ordre : profondeur effacée, mains, transparents des mains, HUD, à la fin de la liste.
                foreach (ScriptableRendererFeature f in new ScriptableRendererFeature[] { clear, hands, glass, hud })
                {
                    data.rendererFeatures.Remove(f);
                    data.rendererFeatures.Add(f);
                }

                // Table des identifiants des passes (comme l'éditeur du renderer).
                var so = new SerializedObject(data);
                SerializedProperty map = so.FindProperty("m_RendererFeatureMap");
                map.arraySize = data.rendererFeatures.Count;
                for (int i = 0; i < data.rendererFeatures.Count; i++)
                    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(data.rendererFeatures[i], out string _, out long id))
                        map.GetArrayElementAtIndex(i).longValue = id;
                so.ApplyModifiedPropertiesWithoutUndo();
                data.SetDirty();
                EditorUtility.SetDirty(data);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>Passe du renderer de ce nom et de ce type (existante, renommée si besoin, ou créée et ajoutée à l'asset).</summary>
        private static T Reuse<T>(UniversalRendererData data, string name) where T : ScriptableRendererFeature
        {
            foreach (ScriptableRendererFeature f in data.rendererFeatures)
                if (f is T t && f.name.StartsWith(name))
                {
                    t.name = name;
                    return t;
                }

            var created = ScriptableObject.CreateInstance<T>();
            created.name = name;
            AssetDatabase.AddObjectToAsset(created, data);
            data.rendererFeatures.Add(created);
            return created;
        }

        /// <summary>
        /// Main greybox (repère : poignet à l'origine, doigts vers +z) : avant-bras, paume, doigts (articulés au bout de
        /// la paume), pouce. Mesures dans docs/art/ART-HANDOFF.md. Renvoie la main et l'articulation des doigts.
        /// </summary>
        private static (Transform hand, Transform fingers) Hand(string name, Transform parent, bool right, Material skin)
        {
            var hand = new GameObject(name).transform;
            hand.SetParent(parent, false);
            float side = right ? 1f : -1f;
            Part(PrimitiveType.Cylinder, "Avant-bras", hand, new Vector3(0f, 0f, -0.14f), new Vector3(0.06f, 0.13f, 0.06f), new Vector3(90f, 0f, 0f), skin);
            Part(PrimitiveType.Cube, "Paume", hand, new Vector3(0f, 0f, 0.045f), new Vector3(0.085f, 0.03f, 0.09f), Vector3.zero, skin);
            var fingers = new GameObject("Doigts (articulation)").transform;
            fingers.SetParent(hand, false);
            fingers.localPosition = new Vector3(0f, 0f, 0.09f);
            Part(PrimitiveType.Cube, "Doigts", fingers, new Vector3(0f, 0f, 0.035f), new Vector3(0.08f, 0.025f, 0.07f), Vector3.zero, skin);
            Part(PrimitiveType.Cube, "Pouce", hand, new Vector3(-0.05f * side, 0.005f, 0.05f), new Vector3(0.022f, 0.022f, 0.055f),
                new Vector3(0f, -30f * side, 0f), skin);
            return (hand, fingers);
        }

        internal static void Part(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Vector3 euler, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private struct TelekinesisAssets
        {
            public TelekinesisOrbSettings Orb;
            public TelekinesisFeedbackSettings Feedback;
        }

        private static GameObject BuildPlayerPrefab(MovementSettings movement, CameraSettings cameraSettings,
            MagnetismOrbSettings magnetism, MagnetismFeedbackSettings feedback, TelekinesisAssets tk, InputActionAsset actions)
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
                unityCamera.GetUniversalAdditionalCameraData().renderPostProcessing = true;   // bloom (lueurs)
                camGo.AddComponent<CinemachineBrain>();
                camGo.AddComponent<AudioListener>();

                // Vue première personne : caméra des mains (superposée, FOV fixe), mains greybox, orbe dans la paume droite.
                var fps = LoadOrCreate<FpsViewSettings>(FpsViewPath);
                // Option 2 (D-028) : pas de caméra des mains ; passes du renderer URP (EnsureFirstPersonRendering).

                var viewRoot = new GameObject("Mains (vue)").transform;
                viewRoot.SetParent(camGo.transform, false);
                Material skin = Mat("Greybox_Skin", new Color(0.72f, 0.58f, 0.48f));
                (Transform rightHand, Transform rightFingers) = Hand("Main droite", viewRoot, true, skin);
                (Transform leftHand, Transform leftFingers) = Hand("Main gauche", viewRoot, false, skin);
                rightHand.localPosition = fps.rightHandRest;
                rightHand.localRotation = Quaternion.Euler(fps.rightHandRestEuler);
                leftHand.localPosition = fps.leftHandRest;
                leftHand.localRotation = Quaternion.Euler(fps.leftHandRestEuler);
                BarrelParts barrel = BuildBarrel(leftHand);

                GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                orb.name = "Orbe (vue)";
                Object.DestroyImmediate(orb.GetComponent<Collider>());
                orb.transform.SetParent(rightHand, false);
                orb.transform.localPosition = fps.orbInHand;
                orb.transform.localScale = Vector3.one * fps.orbDiameter;
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

                var tkLineGo = new GameObject("Trait de télékinésie");
                tkLineGo.transform.SetParent(camGo.transform, false);
                var tkLine = tkLineGo.AddComponent<LineRenderer>();
                tkLine.useWorldSpace = true;
                tkLine.positionCount = 2;
                tkLine.startWidth = 0.03f;
                tkLine.endWidth = 0.05f;
                tkLine.sharedMaterial = line.sharedMaterial;
                tkLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tkLine.receiveShadows = false;
                tkLine.enabled = false;

                // Marquage de la répulsion : point visé et impact (placés dans le monde à chaque image).
                Material markerMat = UnlitMat("Orb_Marker", "Universal Render Pipeline/Unlit", Color.white);
                Renderer aimMarker = Marker("Marque de répulsion (visée)", root.transform, 0.35f, markerMat);
                Renderer impactMarker = Marker("Marque de répulsion (impact)", root.transform, 0.6f, markerMat);
                Renderer anchorMarker = Marker("Ancre de traction", root.transform, 0.3f, markerMat);
                Renderer tractionFlash = Marker("Éclat de traction", root.transform, 0.5f, markerMat);

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
                var view = root.AddComponent<FirstPersonView>();
                var hud = root.AddComponent<PlayerHud>();
                var orbView = root.AddComponent<MagnetismOrbView>();
                var audio = root.AddComponent<MagnetismAudio>();
                var tkView = root.AddComponent<TelekinesisView>();

                // Artefact (D-032) : porte-artefact, vue du barillet, PV du joueur (tir ami).
                var barrelSettings = LoadOrCreate<BarrelSettings>(BarrelPath);
                var holder = root.AddComponent<ArtifactHolder>();
                SetRef(holder, "fallback", barrelSettings);
                var barrelView = root.AddComponent<BarrelView>();
                SetRef(barrelView, "barrelRoot", barrel.Root);
                SetRef(barrelView, "cylinder", barrel.Cylinder);
                SetRef(barrelView, "muzzle", barrel.Muzzle);
                var chambersProp = new SerializedObject(barrelView);
                SerializedProperty list = chambersProp.FindProperty("chambers");
                list.arraySize = barrel.Chambers.Length;
                for (int i = 0; i < barrel.Chambers.Length; i++)
                    list.GetArrayElementAtIndex(i).objectReferenceValue = barrel.Chambers[i];
                chambersProp.ApplyModifiedPropertiesWithoutUndo();
                var tracerGo = new GameObject("Traînée du barillet");
                tracerGo.transform.SetParent(root.transform, false);
                var tracer = tracerGo.AddComponent<LineRenderer>();
                tracer.useWorldSpace = true;
                tracer.positionCount = 2;
                tracer.sharedMaterial = UnlitMat("Orb_Line", "Universal Render Pipeline/Particles/Unlit", Color.white);
                tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                tracer.receiveShadows = false;
                tracer.enabled = false;
                SetRef(barrelView, "tracer", tracer);
                GameObject impactGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                impactGo.name = "Impact de balle";
                Object.DestroyImmediate(impactGo.GetComponent<Collider>());
                impactGo.transform.SetParent(root.transform, false);
                var impactRenderer = impactGo.GetComponent<MeshRenderer>();
                impactRenderer.sharedMaterial = UnlitMat("Barrel_Impact", "Universal Render Pipeline/Unlit", Color.white);
                impactRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                impactRenderer.receiveShadows = false;
                impactRenderer.enabled = false;
                SetRef(barrelView, "impact", impactRenderer);
                CharacterSetup.BuildLantern(leftHand, root);                // lanterne de feu (D-065), cachée sans elle
                var shake = root.AddComponent<CameraShake>();
                SetRef(shake, "virtualCamera", vcam);
                var health = root.AddComponent<PlayerHealth>();
                SetRef(health, "settings", LoadOrCreate<PlayerHealthSettings>(PlayerHealthPath));
                SetRef(health, "coop", LoadOrCreate<CoopSettings>(CoopPath));
                SetRef(view, "settings", fps);
                SetRef(view, "viewRoot", viewRoot);
                SetRef(view, "rightHand", rightHand);
                SetRef(view, "leftHand", leftHand);
                SetRef(view, "rightFingers", rightFingers);
                SetRef(view, "leftFingers", leftFingers);
                SetRef(view, "orb", orb.transform);
                SetRef(hud, "settings", LoadOrCreate<HudSettings>(HudPath));
                SetRef(hud, "damageFeedback", LoadOrCreate<DamageFeedbackSettings>(DamageFeedbackPath));

                SetRef(motor, "settings", movement);
                SetRef(motor, "magnetism", magnetism);
                SetRef(orbView, "orbRenderer", orbRenderer);
                SetRef(orbView, "line", line);
                SetRef(orbView, "aimMarker", aimMarker);
                SetRef(orbView, "impactMarker", impactMarker);
                SetRef(orbView, "anchorMarker", anchorMarker);
                SetRef(orbView, "tractionFlash", tractionFlash);
                SetRef(orbView, "settings", feedback);
                SetRef(orbView, "tkFeedback", tk.Feedback);
                SetRef(audio, "settings", feedback);
                SetRef(hud, "feedback", feedback);
                SetRef(hud, "tkFeedback", tk.Feedback);
                SetRef(motor, "telekinesis", tk.Orb);
                SetRef(tkView, "settings", tk.Feedback);
                SetRef(tkView, "orbRenderer", orbRenderer);
                SetRef(tkView, "line", tkLine);
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

        private struct BarrelParts
        {
            public Transform Root;
            public Transform Cylinder;
            public Transform Muzzle;
            public Renderer[] Chambers;
        }

        /// <summary>
        /// Barillet (D-032) tenu dans la paume gauche, pointé dans le sens des doigts (+z de la main) : corps de
        /// 7.5 cm de diamètre et 7 cm de long qui tourne, 6 chambres de 1.6 cm sur la face arrière (vers la caméra : les
        /// munitions se lisent dessus), canon de 2.2 cm × 10 cm. Mesures dans docs/art/ART-HANDOFF.md.
        /// </summary>
        private static BarrelParts BuildBarrel(Transform leftHand)
        {
            // Modèle SM_Barillet de la DA (étape 10) : BarrelModelInstaller (position, rotation et échelle : FpsView_Default).
            var root = new GameObject("Barillet (vue)").transform;
            root.SetParent(leftHand, false);
            BarrelModelInstaller.Parts parts = BarrelModelInstaller.Build(root);
            return new BarrelParts { Root = root, Cylinder = parts.Spin, Muzzle = parts.Muzzle, Chambers = parts.Chambers };
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

        internal static void SetRef(Object target, string field, Object value)
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

            // Post-process : bloom à seuil 1, seules les couleurs HDR (orbe, traits, marques) brillent.
            var volumeGo = new GameObject("Post-process (bloom)");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = LoadOrCreatePostProcess();

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
            BuildTelekinesisArea(level, blockMat, lineMat);
            BuildTeleportPoints();

            // État de la run (D-032) : seed et tirage des artefacts.
            var pool = LoadOrCreate<ArtifactPool>(ArtifactPoolPath);
            var barrelAsset = LoadOrCreate<BarrelSettings>(BarrelPath);
            if (barrelAsset.id == "artefact")
            {
                barrelAsset.id = "barillet";
                barrelAsset.displayName = "Barillet";
                EditorUtility.SetDirty(barrelAsset);
            }

            if (!pool.artifacts.Contains(barrelAsset))
            {
                pool.artifacts.Add(barrelAsset);
                EditorUtility.SetDirty(pool);
                AssetDatabase.SaveAssets();
            }

            var run = new GameObject("État de la run").AddComponent<RunState>();
            SetRef(run, "pool", pool);
            SetRef(run, "characters", CharacterSetup.Roster());          // personnages (D-064)
            RenderSetup.AddTo(null);                                     // ambiance de rendu par défaut : sombre (D-067, correctifs-rendu)

            // Joueur.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);

            // Mesure de performance dans un build (inactive sans l'argument -ithunts-perf).
            var probe = new GameObject("Mesure de performance (build)").AddComponent<PerfProbe>();
            SetRef(probe, "playerPrefab", playerPrefab);

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
        //   x −100..−50, z −38..−5  : télékinésie (objets à attraper et lancer, lustre suspendu)

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

        /// <summary>Points de téléportation de debug : F4 départ, F5 à F12 zones (position des pieds, yaw).</summary>
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
                ("Télékinésie", new Vector3(-75f, 0f, -35f), 0f),
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

        /// <summary>
        /// Zone de la télékinésie (x −100..−50, z −38..−5), départ F12 en (−75, 0, −35) face au nord. Le mur sud de la
        /// grande salle sert de mur de tir. Caisses de 5, 15, 25, 40 kg et une de 60 kg (trop lourde), tonneaux de
        /// 20 kg, pile de petites caisses, portique avec un lustre accroché (figé jusqu'à ce qu'on l'attrape), repère de
        /// portée à 15 m du départ.
        /// </summary>
        private static void BuildTelekinesisArea(Transform level, Material blockMat, Material lineMat)
        {
            var area = new GameObject("Télékinésie").transform;
            area.SetParent(level, false);
            Material crateMat = GrabbableMat("Greybox_Grabbable", new Color(0.55f, 0.42f, 0.28f));
            Material heavyMat = GrabbableMat("Greybox_Grabbable_Lourd", new Color(0.35f, 0.3f, 0.28f));

            (float mass, float size)[] crates = { (5f, 0.5f), (15f, 0.7f), (25f, 0.9f), (40f, 1.1f), (60f, 1.3f) };
            for (int i = 0; i < crates.Length; i++)
            {
                (float mass, float size) = crates[i];
                GrabbableBox($"Caisse {mass} kg{(mass > 40f ? " (trop lourde)" : "")}", area,
                    new Vector3(-90f + i * 6f, size * 0.5f, -24f), Vector3.one * size, mass, mass > 40f ? heavyMat : crateMat, false);
            }

            for (int i = 0; i < 3; i++)
                GrabbableBarrel($"Tonneau 20 kg ({i + 1})", area, new Vector3(-60f + i * 2f, 0.45f, -24f), 20f, crateMat);

            // Pile de petites caisses (5 kg) : 3 + 2 + 1.
            int n = 0;
            for (int row = 0; row < 3; row++)
            for (int k = 0; k < 3 - row; k++)
                GrabbableBox($"Petite caisse ({++n})", area,
                    new Vector3(-95f + k * 0.55f + row * 0.275f, 0.25f + row * 0.5f, -15f), Vector3.one * 0.5f, 5f, crateMat, false);

            // Portique et lustre accroché.
            // Portique de 8 m : le lustre pend à 7.6 m, à 5.8 m au-dessus du factice placé dessous, hors de l'onde de
            // répulsion (4 m) qui le décroche.
            Box("Portique - pilier ouest", area, new Vector3(-82f, 4f, -12f), new Vector3(0.6f, 8f, 0.6f), blockMat);
            Box("Portique - pilier est", area, new Vector3(-68f, 4f, -12f), new Vector3(0.6f, 8f, 0.6f), blockMat);
            Box("Portique - poutre", area, new Vector3(-75f, 8.2f, -12f), new Vector3(15f, 0.4f, 0.6f), blockMat);
            GrabbableBox("Lustre (accroché)", area, new Vector3(-75f, 7.6f, -12f), new Vector3(1.6f, 0.3f, 1.6f), 25f, crateMat, true);

            BuildTrainingDummies(area);
            BuildShootingLane(area, lineMat);

            Box("Repère portée 15 m (depuis le départ)", area, new Vector3(-75f, 0.012f, -20f), new Vector3(50f, 0.02f, 0.3f), lineMat, collider: false);
        }

        /// <summary>
        /// Pas de tir du barillet (D-032), au sud de la zone : marque de départ en (−95, 0, −34) face à l'est, cibles fixes à
        /// 10, 30 et 60 m (la portée), repères au sol tous les 10 m.
        /// </summary>
        private static void BuildShootingLane(Transform area, Material lineMat)
        {
            var lane = new GameObject("Pas de tir (barillet)").transform;
            lane.SetParent(area, false);
            var dummySettings = LoadOrCreate<TrainingDummySettings>(DummyPath);
            Material targetMat = GrabbableMat("Greybox_Dummy", new Color(0.55f, 0.2f, 0.18f));
            Vector3 start = new Vector3(-95f, 0f, -34f);
            Box("Pas de tir : départ", lane, start + new Vector3(0f, 0.012f, 0f), new Vector3(1.2f, 0.02f, 1.2f), lineMat, collider: false);
            for (int d = 10; d <= 60; d += 10)
                Box($"Repère {d} m", lane, start + new Vector3(d, 0.012f, 0f), new Vector3(0.15f, 0.02f, 2f), lineMat, collider: false);
            // Décalées sur le côté : aucune ne cache les autres depuis le départ.
            foreach ((int d, float side) in new[] { (10, 0f), (30, 2.5f), (60, -4f) })
                Dummy($"Cible de tir ({d} m)", lane, start + new Vector3(d, 0f, side), targetMat, dummySettings, moving: false);
        }

        private static GameObject GrabbableBox(string name, Transform parent, Vector3 center, Vector3 size, float mass,
            Material mat, bool anchored)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            MakeGrabbable(go, mass, anchored);
            return go;
        }

        /// <summary>
        /// Ennemis factices (D-025) : 3 fixes (dont un sous le lustre) et 1 mobile en allers-retours devant le mur de
        /// la grande salle ; 100 PV, réapparition après 3 s (TrainingDummy_Default). Retour des dégâts (éclats, sons)
        /// dans un objet de la scène.
        /// </summary>
        private static void BuildTrainingDummies(Transform area)
        {
            var dummySettings = LoadOrCreate<TrainingDummySettings>(DummyPath);
            Material dummyMat = GrabbableMat("Greybox_Dummy", new Color(0.55f, 0.2f, 0.18f));
            var root = new GameObject("Ennemis factices").transform;
            root.SetParent(area, false);
            (string name, Vector3 position)[] fixedDummies =
            {
                ("Factice fixe (18 m)", new Vector3(-64f, 0f, -17f)),
                ("Factice fixe sous le lustre", new Vector3(-75f, 0f, -12f)),
                ("Factice fixe (30 m)", new Vector3(-88f, 0f, -9f)),
            };
            foreach ((string name, Vector3 position) in fixedDummies)
                Dummy(name, root, position, dummyMat, dummySettings, moving: false);
            GameObject mover = Dummy("Factice mobile (allers-retours de 36 m)", root, new Vector3(-94f, 0f, -4f), dummyMat,
                dummySettings, moving: true);
            var so = new SerializedObject(mover.GetComponent<TrainingDummyMover>());
            so.FindProperty("travel").vector3Value = new Vector3(36f, 0f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();

            var feedbackGo = new GameObject("Retour des dégâts");
            feedbackGo.transform.SetParent(area, false);
            var feedback = feedbackGo.AddComponent<DamageFeedback>();
            SetRef(feedback, "settings", LoadOrCreate<DamageFeedbackSettings>(DamageFeedbackPath));
            SetRef(feedback, "flashMaterial", UnlitMat("Damage_Flash", "Universal Render Pipeline/Unlit", Color.white));
            SetRef(feedback, "lineMaterial", UnlitMat("Orb_Line", "Universal Render Pipeline/Particles/Unlit", Color.white));
        }

        /// <summary>Mannequin : capsule de 1.8 m × 0.8 m (taille du joueur), posée au sol.</summary>
        private static GameObject Dummy(string name, Transform parent, Vector3 feet, Material mat, TrainingDummySettings settings,
            bool moving)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = feet + Vector3.up * 0.9f;
            go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            var body = go.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.mass = settings.mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            // Ennemi attrapable quand il est étourdi (D-030), qui blesse ce qu'il percute une fois lancé.
            var grabbable = go.AddComponent<Grabbable>();
            var gso = new SerializedObject(grabbable);
            gso.FindProperty("isEnemy").boolValue = true;
            gso.ApplyModifiedPropertiesWithoutUndo();
            SetRef(go.AddComponent<ImpactDamager>(), "settings", LoadOrCreate<DamageSettings>(DamagePath));
            var enemy = go.AddComponent<EnemyBody>();
            SetRef(enemy, "settings", settings);
            SetRef(go.AddComponent<Damageable>(), "settings", settings);
            if (moving)
            {
                SetRef(go.AddComponent<TrainingDummyMover>(), "settings", settings);
                var so = new SerializedObject(enemy);
                so.FindProperty("returnHome").boolValue = false;        // le trajet le ramène
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            return go;
        }

        private static void GrabbableBarrel(string name, Transform parent, Vector3 center, float mass, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = new Vector3(0.6f, 0.45f, 0.6f);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            MakeGrabbable(go, mass, false);
        }

        /// <summary>Corps dynamique attrapable (jamais statique : il bouge).</summary>
        internal static void MakeGrabbable(GameObject go, float mass, bool anchored)
        {
            GameObjectUtility.SetStaticEditorFlags(go, 0);
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<Grabbable>();
            SetRef(go.AddComponent<ImpactDamager>(), "settings", LoadOrCreate<DamageSettings>(DamagePath));
            var so = new SerializedObject(go.GetComponent<Grabbable>());
            so.FindProperty("respawnAfterBreak").floatValue = 4f;      // scène de test : il revient après s'être cassé
            if (anchored)
            {
                // Lustre : accroché, fragile (cassé au premier impact, lancé ou tombé), grande zone de dégâts.
                so.FindProperty("startsAnchored").boolValue = true;
                so.FindProperty("breaksOnAnyImpact").boolValue = true;
                so.FindProperty("splashRadiusOverride").floatValue = 4f;
                body.isKinematic = true;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Matériau Lit avec émission activée (la lueur est réglée par objet, par MaterialPropertyBlock).</summary>
        internal static Material GrabbableMat(string name, Color color)
        {
            Material mat = Mat(name, color);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
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

        internal static GameObject Box(string name, Transform parent, Vector3 center, Vector3 size, Material mat,
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

        internal static Material Mat(string name, Color color)
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

        /// <summary>Profil de post-process de la scène de test (créé une fois, ensuite conservé comme les réglages).</summary>
        internal static VolumeProfile LoadOrCreatePostProcess()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProcessPath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, PostProcessPath);
            }

            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>(true);
                bloom.name = "Bloom";
                AssetDatabase.AddObjectToAsset(bloom, profile);
                bloom.threshold.Override(1f);
                bloom.intensity.Override(1.2f);
                bloom.scatter.Override(0.65f);
                EditorUtility.SetDirty(bloom);
                EditorUtility.SetDirty(profile);
                AssetDatabase.SaveAssets();
            }

            return profile;
        }

        internal static Material UnlitMat(string name, string shaderName, Color color)
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

        internal static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            return asset;
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        internal static void AddSceneToBuild(string path)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path))
                return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
