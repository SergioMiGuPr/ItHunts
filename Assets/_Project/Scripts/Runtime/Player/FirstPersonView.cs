using ItHunts.Movement;
using ItHunts.Orbs;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace ItHunts.Player
{
    /// <summary>Geste de la main droite en cours (debug, mesures).</summary>
    public enum HandGesture : byte
    {
        Aucun,
        Repulsion,
        Lancer,
        Prise,
    }

    /// <summary>Cadrage des mains (menu pause, vitrine) : référence (reference_bras_fps.png) ou écarté.</summary>
    public enum HandFraming : byte
    {
        Reference,
        Ecarte,
    }

    /// <summary>
    /// Aperçu forcé de la vue (scène vitrine, captures) : une pose de fond ou un état de l'artefact sans passer par la simulation.
    /// Aucun : la vue suit l'état simulé du joueur (en jeu).
    /// </summary>
    public enum ViewPreview : byte
    {
        Aucun,
        Traction,
        RepulsionTenue,
        ObjetTenu,
        Jet,
    }

    /// <summary>
    /// Vue première personne (greybox, étape 5) : deux mains, l'orbe dans la paume droite, sur une couche propre à chaque
    /// joueur (FirstPersonP1..P4). Option 2 (D-028, après mesure dans un build) : pas de caméra en plus ; une passe
    /// « Render Objects » du renderer URP dessine la couche des mains à 60°, après une passe qui efface la profondeur
    /// (FirstPersonDepthClearFeature) ; la caméra du joueur ne voit que sa propre couche (écran partagé). Une caméra
    /// superposée (overlayCamera, ancienne option 1) reste possible si elle est assignée. Mouvement des mains (balancement,
    /// balancier de course, plongée à l'atterrissage : réglés par l'intensité de confort du joueur), poses d'état
    /// (glissade, recul avec la vitesse, traction, objet tenu) et gestes (répulsion, lancer, prise), lus dans l'état
    /// simulé du joueur. Affichage seulement : aucune influence sur la simulation.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class FirstPersonView : MonoBehaviour
    {
        public const string LayerPrefix = "FirstPersonP";
        private const string IntensityKey = "ItHunts.HandMotionIntensity.P";
        private const string FramingKey = "ItHunts.HandFraming2.P";      // v2 : « écarté » par défaut (playtest des animations)
        private HandFraming _framing;

        /// <summary>Cadrage des mains, enregistré par joueur (menu pause, vitrine).</summary>
        public HandFraming Framing
        {
            get => _framing;
            set
            {
                _framing = value;
                PlayerPrefs.SetInt(FramingKey + _playerIndex, (int)value);
            }
        }

        public static string FramingLabel(HandFraming f) => f == HandFraming.Ecarte ? "écarté" : "référence";

        /// <summary>Position et angles de repos d'une main selon le cadrage (gauche : miroir du préréglage droit).</summary>
        private Vector3 RestPos(bool right) => _framing == HandFraming.Ecarte
            ? (right ? settings.wideRightHandRest : Vector3.Scale(settings.wideRightHandRest, new Vector3(-1f, 1f, 1f)))
            : right ? settings.rightHandRest : settings.leftHandRest;

        private Vector3 RestEuler(bool right) => _framing == HandFraming.Ecarte
            ? (right ? settings.wideRightHandRestEuler : Vector3.Scale(settings.wideRightHandRestEuler, new Vector3(1f, -1f, -1f)))
            : right ? settings.rightHandRestEuler : settings.leftHandRestEuler;

        [SerializeField] private FpsViewSettings settings;
        [SerializeField] private Camera overlayCamera;
        [SerializeField] private Transform viewRoot;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightFingers;
        [SerializeField] private Transform leftFingers;
        [SerializeField] private Transform orb;
        [Tooltip("Réglage de confort du joueur : intensité du mouvement des mains (balancement, balancier, plongée ; pas " +
                 "les gestes). Enregistré par joueur.")]
        [SerializeField, Range(0f, 1f)] private float handMotionIntensity = 1f;

        private static readonly Unity.Profiling.ProfilerMarker Marker = new Unity.Profiling.ProfilerMarker("ItHunts.FirstPersonView");
        private PlayerMotor _motor;
        private PlayerLook _look;
        private Camera _mainCamera;
        private int _playerIndex;
        private int _layer = -1;
        private float _lastYaw, _lastPitch;
        private Vector3 _swayOffset, _swayEuler;
        private float _bobPhase;
        private float _dip;
        private bool _wasGrounded = true;
        private float _lastVerticalSpeed;
        private float _slide;
        private Vector3 _lag;
        private Vector3 _rightOffset;
        private Vector3 _rightEuler;
        private float _rightCurl, _leftCurl;
        private int _seenRepelTick = MovementState.NeverTick;
        private int _seenActionTick = MovementState.NeverTick;
        private float _gestureStart = float.NegativeInfinity;
        private ItHunts.Artifacts.ArtifactHolder _artifact;
        private int _seenShotTick = MovementState.NeverTick;
        private float _recoilStart = float.NegativeInfinity;
        private float _inactive, _jetPush;

        public FpsViewSettings Settings => settings;
        public Camera OverlayCamera => overlayCamera;
        public Transform Orb => orb;
        /// <summary>Racine des mains (repère de la vue) : l'artefact qui flotte reste pointé vers l'avant de la vue.</summary>
        public Transform ViewRoot => viewRoot;
        /// <summary>Main gauche de la vue FPS (artefacts).</summary>
        public Transform LeftHand => leftHand;
        public int Layer => _layer;
        public HandGesture Gesture { get; private set; }

        /// <summary>Aperçu forcé (vitrine) : pose de fond ou jet de la lanterne, sans la simulation.</summary>
        public ViewPreview Preview { get; set; }
        /// <summary>Pose de fond affichée (lue par les bras de la DA) : traction, répulsion maintenue, objet tenu.</summary>
        public bool ShowsTraction { get; private set; }
        public bool ShowsRepelHold { get; private set; }
        public bool ShowsHolding { get; private set; }
        /// <summary>Jet de la lanterne affiché (simulation ou aperçu).</summary>
        public bool ShowsJet => Preview == ViewPreview.Jet || (_artifact != null && _artifact.LanternSettings != null && _artifact.Lantern.Jetting);

        /// <summary>Joue un geste de la main droite (vitrine) ; le recul d'un tir du barillet avec PreviewRecoil.</summary>
        public void PlayGesture(HandGesture gesture) => StartGesture(gesture, MovementState.NeverTick);
        public void PreviewRecoil() => _recoilStart = Time.time;
        /// <summary>Avancement du geste en cours (0 au début, 1 à la fin) : poses des bras de la DA (FpsArms).</summary>
        public float GestureProgress { get; private set; } = 1f;
        /// <summary>Fermeture des doigts (°, lissée) : main droite et main gauche (repli des bras de la DA sans clips).</summary>
        public float RightCurl => _rightCurl;
        public float LeftCurl => _leftCurl;

        /// <summary>Image (Time.frameCount) où le dernier geste a commencé, et tick simulé qui l'a déclenché.</summary>
        public int LastGestureFrame { get; private set; } = -1;
        public int LastGestureTick { get; private set; } = MovementState.NeverTick;

        /// <summary>Intensité du mouvement des mains (0..1), enregistrée par joueur.</summary>
        public float HandMotionIntensity
        {
            get => handMotionIntensity;
            set
            {
                handMotionIntensity = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(IntensityKey + _playerIndex, handMotionIntensity);
            }
        }

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor>();
            _look = GetComponent<PlayerLook>();
            _artifact = GetComponent<ItHunts.Artifacts.ArtifactHolder>();
            var playerInput = GetComponent<PlayerInput>();
            _mainCamera = playerInput != null ? playerInput.camera : null;
            _playerIndex = playerInput != null ? Mathf.Max(0, playerInput.playerIndex) : 0;
            handMotionIntensity = PlayerPrefs.GetFloat(IntensityKey + _playerIndex, handMotionIntensity);
            _framing = (HandFraming)Mathf.Clamp(PlayerPrefs.GetInt(FramingKey + _playerIndex, (int)HandFraming.Ecarte), 0, 1);
            SetupCameras();
            SetupFillLight();
            if (orb != null && settings != null)
            {
                orb.localPosition = settings.orbInHand;
                orb.localScale = Vector3.one * settings.orbDiameter;
            }
        }

        /// <summary>Couche du joueur, caméra des mains dans la pile de sa caméra, et la caméra du monde sans les mains.</summary>
        private void SetupCameras()
        {
            _layer = LayerMask.NameToLayer(LayerPrefix + (_playerIndex + 1));
            if (_layer < 0)
            {
                Debug.LogWarning($"Couche {LayerPrefix}{_playerIndex + 1} absente : mains dessinées par la caméra du monde.", this);
                if (overlayCamera != null)
                    overlayCamera.enabled = false;
                return;
            }

            if (viewRoot != null)
                SetLayerRecursively(viewRoot, _layer);
            if (_mainCamera != null)
            {
                for (int i = 1; i <= 4; i++)
                {
                    int l = LayerMask.NameToLayer(LayerPrefix + i);
                    if (l >= 0)
                        _mainCamera.cullingMask &= ~(1 << l);
                }

                // Option 2 : la caméra du joueur voit SA couche de mains (la passe « Render Objects » la dessine à 60°).
                if (overlayCamera == null)
                    _mainCamera.cullingMask |= 1 << _layer;
            }

            if (overlayCamera == null)
                return;
            overlayCamera.cullingMask = 1 << _layer;
            if (settings != null)
            {
                overlayCamera.fieldOfView = settings.viewmodelFov;
                overlayCamera.nearClipPlane = settings.viewmodelNearClip;
            }

            UniversalAdditionalCameraData overlayData = overlayCamera.GetUniversalAdditionalCameraData();
            overlayData.renderType = CameraRenderType.Overlay;
            if (_mainCamera != null)
            {
                UniversalAdditionalCameraData baseData = _mainCamera.GetUniversalAdditionalCameraData();
                if (!baseData.cameraStack.Contains(overlayCamera))
                    baseData.cameraStack.Add(overlayCamera);
            }
        }

        private Light _fill;

        /// <summary>Lumière d'appoint de la vue (point lumineux sans ombre, enfant de la caméra) : n'éclaire que le calque « FPS ».</summary>
        public Light FillLight => _fill;

        private void SetupFillLight()
        {
            Transform cam = viewRoot != null && viewRoot.parent != null ? viewRoot.parent : transform;
            var go = new GameObject("Lumière d'appoint (vue FPS)");
            go.transform.SetParent(cam, false);
            _fill = go.AddComponent<Light>();
            _fill.type = LightType.Point;
            _fill.shadows = LightShadows.None;
            uint fps = 1u << ItHunts.Level.AmbianceLight.FpsRenderingLayer;
            _fill.renderingLayerMask = (int)fps;
            var data = go.AddComponent<UniversalAdditionalLightData>();
            data.renderingLayers = fps;
            UpdateFillLight();
        }

        private void UpdateFillLight()
        {
            if (_fill == null || settings == null)
                return;
            _fill.enabled = settings.fillLight;
            _fill.intensity = settings.fillLightIntensity;
            _fill.range = settings.fillLightRange;
            _fill.color = settings.fillLightColor;
            _fill.transform.localPosition = settings.fillLightOffset;
        }

        /// <summary>Affiche ou cache les mains (mesures de performance).</summary>
        public void SetHandsVisible(bool visible)
        {
            if (viewRoot != null)
                viewRoot.gameObject.SetActive(visible);
        }

        public static void SetLayerRecursively(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t)
                SetLayerRecursively(child, layer);
        }

        /// <summary>
        /// Point du monde où la caméra du monde voit l'orbe à la même place que la caméra des mains (les deux n'ont pas le
        /// même champ de vision) : départ des traits de traction et de télékinésie.
        /// </summary>
        public Vector3 LineStart => orb != null ? ViewToWorld(orb.position) : transform.position;

        /// <summary>
        /// Point du monde que la caméra du monde voit à la même place qu'un point des mains (dessiné à viewmodelFov) :
        /// départ des traits de l'orbe, de la traînée du barillet.
        /// </summary>
        public Vector3 ViewToWorld(Vector3 viewmodelPoint)
        {
            if (_mainCamera == null || settings == null)
                return viewmodelPoint;
            if (overlayCamera != null && overlayCamera.enabled)
            {
                Vector3 vp = overlayCamera.WorldToViewportPoint(viewmodelPoint);
                return _mainCamera.ViewportToWorldPoint(new Vector3(vp.x, vp.y, Mathf.Max(vp.z, _mainCamera.nearClipPlane + 0.01f)));
            }

            // Option 2 : les mains sont dessinées à viewmodelFov ; même direction à l'écran pour la caméra du monde.
            Transform cam = _mainCamera.transform;
            Vector3 local = cam.InverseTransformPoint(viewmodelPoint);
            float k = Mathf.Tan(_mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(settings.viewmodelFov * 0.5f * Mathf.Deg2Rad);
            return cam.TransformPoint(new Vector3(local.x * k, local.y * k, local.z));
        }

        private void LateUpdate()
        {
            if (settings == null || _motor == null || viewRoot == null)
                return;
            using Unity.Profiling.ProfilerMarker.AutoScope scope = Marker.Auto();
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            MovementState s = _motor.State;
            float intensity = handMotionIntensity;

            // Balancement : rotation de la vue sur cette image, ramenée à une image à 60 Hz.
            float yaw = _look != null ? _look.Yaw : 0f, pitch = _look != null ? _look.Pitch : 0f;
            float k60 = (1f / 60f) / dt;
            ViewmodelMotion.Sway(Mathf.DeltaAngle(_lastYaw, yaw) * k60, Mathf.DeltaAngle(_lastPitch, pitch) * k60, settings,
                intensity, out Vector3 swayTarget, out Vector3 swayEulerTarget);
            _lastYaw = yaw;
            _lastPitch = pitch;
            float ks = 1f - Mathf.Exp(-settings.swayReturn * dt);
            _swayOffset = Vector3.Lerp(_swayOffset, swayTarget, ks);
            _swayEuler = Vector3.Lerp(_swayEuler, swayEulerTarget, ks);

            // Balancier de course, plongée à l'atterrissage.
            _bobPhase = ViewmodelMotion.AdvanceBobPhase(_bobPhase, s.HorizontalSpeed, s.Grounded, settings, dt);
            Vector3 bob = ViewmodelMotion.Bob(_bobPhase, s.HorizontalSpeed, s.Grounded, settings, intensity);
            if (s.Grounded && !_wasGrounded)
                _dip = Mathf.Max(_dip, ViewmodelMotion.LandingDip(-_lastVerticalSpeed, settings, intensity));
            _wasGrounded = s.Grounded;
            _lastVerticalSpeed = s.Velocity.y;
            _dip = Mathf.Lerp(_dip, 0f, 1f - Mathf.Exp(-settings.landingReturn * dt));

            // Poses d'état : glissade, recul avec la vitesse (sans l'intensité).
            float kp = 1f - Mathf.Exp(-settings.poseSmoothing * dt);
            _slide = Mathf.Lerp(_slide, s.Mode == MovementMode.Slide ? 1f : 0f, kp);
            Transform cam = viewRoot.parent != null ? viewRoot.parent : transform;
            Vector3 localVelocity = cam.InverseTransformDirection(s.Velocity);
            _lag = Vector3.Lerp(_lag, Vector3.ClampMagnitude(ViewmodelMotion.SpeedLag(localVelocity, settings), settings.speedLagAt20 * 2.5f), kp);

            viewRoot.localPosition = _swayOffset + bob + Vector3.down * (_dip + settings.slideDrop * _slide) + _lag;
            viewRoot.localRotation = Quaternion.Euler(_swayEuler + new Vector3(0f, 0f, settings.slideRoll * _slide));

            UpdateHands(s, kp);
            UpdateFillLight();                                           // réglable en Play Mode
        }

        private void UpdateHands(in MovementState s, float kp)
        {
            MagnetismState m = _motor.Magnetism;
            TelekinesisState t = _motor.Telekinesis;

            // Gestes déclenchés par l'état simulé (même image que le tick qui les produit).
            if (m.LastRepelTick != MovementState.NeverTick && m.LastRepelTick != _seenRepelTick)
            {
                _seenRepelTick = m.LastRepelTick;
                StartGesture(HandGesture.Repulsion, m.LastRepelTick);
            }

            if (t.LastActionTick != MovementState.NeverTick && t.LastActionTick != _seenActionTick)
            {
                _seenActionTick = t.LastActionTick;
                if (t.LastAction == TelekinesisCommandKind.Lancer)
                    StartGesture(HandGesture.Lancer, t.LastActionTick);
                else if (t.LastAction == TelekinesisCommandKind.Attraper)
                    StartGesture(HandGesture.Prise, t.LastActionTick);
            }

            // Pose de fond de la main droite : traction (bras tendu), objet tenu (main relevée), repos.
            Vector3 baseOffset = Vector3.zero, baseEuler = Vector3.zero;
            float curl = 25f;                                            // doigts un peu fermés au repos
            ShowsRepelHold = m.RepelHolding || Preview == ViewPreview.RepulsionTenue;
            ShowsTraction = !ShowsRepelHold && (s.Mode == MovementMode.Traction || Preview == ViewPreview.Traction);
            ShowsHolding = !ShowsRepelHold && !ShowsTraction && (t.Holding || Preview == ViewPreview.ObjetTenu);
            if (ShowsRepelHold)
            {
                baseOffset = settings.tractionOffset + new Vector3(0f, -0.04f, 0.02f);
                baseEuler = new Vector3(-70f, 0f, 0f);                     // paume tendue vers l'avant : on repousse
                curl = 0f;
            }
            else if (ShowsTraction)
            {
                baseOffset = settings.tractionOffset;
                baseEuler = new Vector3(-55f, 0f, 0f);                     // paume vers l'avant
                curl = 0f;
            }
            else if (ShowsHolding)
            {
                baseOffset = settings.holdOffset;
                baseEuler = new Vector3(-35f, 0f, 0f);
                curl = 5f;
            }

            // Geste en cours : il s'ajoute à la pose de fond.
            Vector3 gesture = Vector3.zero;
            float duration = Gesture == HandGesture.Repulsion ? settings.repelGestureDuration
                : Gesture == HandGesture.Lancer ? settings.throwGestureDuration
                : Gesture == HandGesture.Prise ? settings.grabGestureDuration
                : 0f;
            float u = duration > 0f ? (Time.time - _gestureStart) / duration : 1f;
            GestureProgress = Mathf.Clamp01(u);
            if (u >= 1f)
                Gesture = HandGesture.Aucun;
            switch (Gesture)
            {
                case HandGesture.Repulsion:
                    gesture = new Vector3(-0.03f, 0.03f, settings.repelPunch) * ViewmodelMotion.Punch(u);
                    baseEuler += new Vector3(-60f, 0f, 0f) * ViewmodelMotion.Punch(u);
                    curl = 0f;
                    break;
                case HandGesture.Lancer:
                    gesture = new Vector3(-0.02f, 0.02f, ViewmodelMotion.ThrowCurve(u, settings.throwWindup, settings.throwPush));
                    curl = 0f;
                    break;
                case HandGesture.Prise:
                    gesture = new Vector3(-0.02f, 0.02f, settings.grabReach) * ViewmodelMotion.Punch(u);
                    curl = Mathf.Lerp(0f, 40f, u);                       // la main s'ouvre, puis se referme
                    break;
            }

            // Parade (nuit 3, point 7) : les deux mains se croisent en garde devant la poitrine (0.3 s), puis repoussent si elle réussit.
            ParryGuard(out Vector3 guardRight, out Vector3 guardRightEuler, out Vector3 guardLeft, out Vector3 guardLeftEuler);
            gesture += guardRight;
            baseEuler += guardRightEuler;

            // Les gestes répondent sans lissage ; la pose de fond est lissée.
            _rightOffset = Vector3.Lerp(_rightOffset, baseOffset, kp);
            _rightEuler = Vector3.Lerp(_rightEuler, baseEuler, kp);
            _rightCurl = Mathf.Lerp(_rightCurl, curl, kp);
            if (rightHand != null)
            {
                rightHand.localPosition = RestPos(true) + _rightOffset + gesture;
                rightHand.localRotation = Quaternion.Euler(RestEuler(true) + _rightEuler);
            }

            if (rightFingers != null)
                rightFingers.localRotation = Quaternion.Euler(_rightCurl, 0f, 0f);

            // Main gauche : tient l'artefact (D-032). Recul à chaque tir (sans lissage, sans l'intensité) ; un peu abaissée
            // quand le barillet est inactif (l'orbe tient un objet).
            Vector3 leftOffset = Vector3.zero, leftEuler = Vector3.zero;
            float leftCurl = 30f;
            ItHunts.Settings.BarrelSettings barrel = _artifact != null ? _artifact.BarrelSettings : null;
            if (barrel != null)
            {
                leftCurl = 70f;                                          // doigts refermés sur la poignée
                ItHunts.Artifacts.BarrelState b = _artifact.Barrel;
                if (b.LastShotTick != MovementState.NeverTick && b.LastShotTick != _seenShotTick)
                {
                    _seenShotTick = b.LastShotTick;
                    _recoilStart = Time.time;
                }

                float r = (Time.time - _recoilStart) / barrel.recoilDuration;
                if (r < 1f)
                {
                    float kick = ViewmodelMotion.Punch(r);
                    leftOffset += new Vector3(0f, 0.01f, -barrel.recoilKick) * kick;
                    leftEuler += new Vector3(-barrel.recoilPitch, 0f, 0f) * kick;
                }

                _inactive = Mathf.Lerp(_inactive, b.Inactive ? 1f : 0f, kp);
                leftOffset += new Vector3(-0.01f, -0.04f, -0.02f) * _inactive;
                leftEuler += new Vector3(15f, 0f, 0f) * _inactive;
            }

            // Lanterne de feu (D-065) : main tendue et qui tremble pendant le jet, abaissée si l'orbe tient un objet.
            ItHunts.Settings.FireLanternSettings lantern = _artifact != null ? _artifact.LanternSettings : null;
            if (lantern != null)
            {
                leftCurl = 70f;
                ItHunts.Artifacts.LanternState ls = _artifact.Lantern;
                _jetPush = Mathf.Lerp(_jetPush, ShowsJet ? 1f : 0f, 1f - Mathf.Exp(-20f * Time.deltaTime));
                float shake = Mathf.Sin(Time.time * 60f) * 0.004f * _jetPush;
                leftOffset += new Vector3(shake, 0.01f * _jetPush, lantern.jetPush * _jetPush);
                leftEuler += new Vector3(-6f * _jetPush, 0f, 0f);
                _inactive = Mathf.Lerp(_inactive, ls.Inactive ? 1f : 0f, kp);
                leftOffset += new Vector3(-0.01f, -0.04f, -0.02f) * _inactive;
                leftEuler += new Vector3(15f, 0f, 0f) * _inactive;
            }

            leftOffset += guardLeft;
            leftEuler += guardLeftEuler;
            _leftCurl = Mathf.Lerp(_leftCurl, leftCurl, kp);
            if (leftHand != null)
            {
                leftHand.localPosition = RestPos(false) + leftOffset;
                leftHand.localRotation = Quaternion.Euler(RestEuler(false) + leftEuler);
            }

            if (leftFingers != null)
                leftFingers.localRotation = Quaternion.Euler(_leftCurl, 0f, 0f);
        }

        private PlayerParry _parry;

        /// <summary>Durée du geste de garde (s) et de la poussée d'une parade réussie (s).</summary>
        public const float ParryGuardDuration = 0.32f, ParryPushDuration = 0.22f;

        /// <summary>
        /// Geste de parade (nuit 3, point 7 ; exception autorisée aux mains gelées : mouvement simple des mains actuelles, sans changer
        /// leur modèle). Appui : les deux mains montent et se croisent devant la poitrine, paumes vers l'avant ; réussite : elles
        /// repoussent d'un coup sec. Aucune incidence sur le mouvement.
        /// </summary>
        public void ParryGuard(out Vector3 right, out Vector3 rightEuler, out Vector3 left, out Vector3 leftEuler)
        {
            right = rightEuler = left = leftEuler = Vector3.zero;
            if (_parry == null)
                _parry = GetComponent<PlayerParry>();
            if (_parry == null)
                return;
            float now = Time.time;
            float g = (now - _parry.State.LastPress) / ParryGuardDuration;
            float k = g >= 0f && g < 1f ? Mathf.Sin(Mathf.PI * Mathf.Pow(g, 0.6f)) : 0f;
            float p = (now - _parry.State.LastSuccess) / ParryPushDuration;
            float push = p >= 0f && p < 1f ? ViewmodelMotion.Punch(p) : 0f;
            right = new Vector3(-0.11f, 0.08f, 0.04f) * k + new Vector3(0f, 0f, 0.12f) * push;
            rightEuler = new Vector3(-45f, -20f, 35f) * k + new Vector3(-20f, 0f, 0f) * push;
            left = new Vector3(0.11f, 0.08f, 0.04f) * k + new Vector3(0f, 0f, 0.12f) * push;
            leftEuler = new Vector3(-45f, 20f, -35f) * k + new Vector3(-20f, 0f, 0f) * push;
            ParryGuardAmount = k;
        }

        /// <summary>Ampleur du geste de garde en cours (0 à 1 ; tests, captures).</summary>
        public float ParryGuardAmount { get; private set; }

        private void StartGesture(HandGesture gesture, int tick)
        {
            Gesture = gesture;
            _gestureStart = Time.time;
            LastGestureFrame = Time.frameCount;
            LastGestureTick = tick;
        }
    }
}
