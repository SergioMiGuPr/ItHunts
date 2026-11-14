using System.Collections.Generic;
using System.Linq;
using ItHunts.Economy;
using ItHunts.Level;
using ItHunts.Settings;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.Rendering;

namespace ItHunts.Player
{
    /// <summary>
    /// Écran partagé local à 2 joueurs (étape 9, D-059). Le 2e joueur rejoint en appuyant sur Start (Options) de sa manette, en
    /// jeu ou dans le menu (RunSession.CoopDevice). Entrées : clavier-souris + une manette, ou deux manettes ; chaque joueur garde
    /// ses appareils (plus de changement automatique). Écran coupé gauche / droite (réglage : haut / bas) ; chaque moitié a sa
    /// caméra (canal Cinemachine propre), son HUD (toile de sa caméra), ses mains (masquées dans la vue de l'autre) et sa minimap.
    /// Chaque joueur voit le corps de l'autre (capsule greybox), couché quand il est à terre, avec « maintiens E / Y ». Spectateur :
    /// sa moitié suit la caméra de l'allié vivant, bandeau « Spectateur ». Réseau (étape 11) : remplacé par la session en ligne.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class SplitScreen : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private CoopSettings settings;

        private readonly Dictionary<PlayerHealth, Transform> _bodies = new Dictionary<PlayerHealth, Transform>();
        private readonly Dictionary<PlayerHealth, WorldLabelHolder> _labels = new Dictionary<PlayerHealth, WorldLabelHolder>();
        private static GUIStyle _banner, _small;
        private Material _bodyMat1, _bodyMat2;

        private sealed class WorldLabelHolder
        {
            public WorldLabel Label;
        }

        public static SplitScreen Current { get; private set; }
        public int Joins { get; private set; }
        public CoopSettings Settings => settings;

        private void OnEnable()
        {
            Current = this;
            if (GetComponent<ItHunts.Level.PauseMenu>() == null)
                gameObject.AddComponent<ItHunts.Level.PauseMenu>();     // menu pause de chaque joueur (Échap / Start)
            ItHunts.Debugging.LevelPerfProbe.SpawnSecondPlayer = () => Join(null);
            RenderPipelineManager.beginCameraRendering += BeforeCamera;
        }

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
            RenderPipelineManager.beginCameraRendering -= BeforeCamera;
        }

        private void Start()
        {
            if (RunSession.CoopDevice != null && RunSession.CoopDevice.added)
                Join(RunSession.CoopDevice);
        }

        private void Update()
        {
            foreach (PlayerHealth p in PlayerHealth.All)
                if (p != null && p.GetComponent<CoopRevive>() == null)
                    p.gameObject.AddComponent<CoopRevive>();
            for (int i = 0; i < PlayerInput.all.Count; i++)
                foreach (AudioListener al in PlayerInput.all[i].GetComponentsInChildren<AudioListener>(true))
                    if (al.enabled != (i == 0))
                        al.enabled = i == 0;                             // un seul auditeur : le 1er joueur
            int max = settings != null ? settings.maxLocalPlayers : 2;
            if (PlayerInput.all.Count >= max)
                return;
            foreach (Gamepad g in Gamepad.all)
                if (g.startButton.wasPressedThisFrame && !Paired(g))
                {
                    Join(g);
                    break;
                }
        }

        private static bool Paired(InputDevice d)
        {
            foreach (PlayerInput pi in PlayerInput.all)
                if (pi.user.valid && pi.user.pairedDevices.Contains(d))
                    return true;
            return false;
        }

        /// <summary>
        /// Le 2e joueur arrive avec cet appareil (null : sans appareil, mesures). Le 1er garde le sien (clavier-souris ou sa
        /// manette), le nouveau apparaît à côté de lui ; l'écran se coupe en deux.
        /// </summary>
        public GameObject Join(InputDevice device)
        {
            if (playerPrefab == null || PlayerInput.all.Count == 0)
                return null;
            PlayerInput p1 = PlayerInput.all[0];
            if (device != null && Paired(device) && p1.user.pairedDevices.Contains(device) && p1.currentControlScheme == "Gamepad")
                return null;                                             // c'est la manette du 1er joueur
            LockFirstPlayer(p1, device);
            Vector3 at = p1.transform.position + p1.transform.right * 2f;
            PlayerInput p2 = device != null
                ? PlayerInput.Instantiate(playerPrefab, controlScheme: "Gamepad", pairWithDevice: device)
                : PlayerInput.Instantiate(playerPrefab, controlScheme: "Gamepad", pairWithDevices: new InputDevice[0]);
            p2.neverAutoSwitchControlSchemes = true;
            var motor = p2.GetComponent<PlayerMotor>();
            if (motor != null)
                motor.Teleport(at);
            foreach (AudioListener al in p2.GetComponentsInChildren<AudioListener>(true))
                al.enabled = false;                                      // un seul auditeur : le 1er joueur
            foreach (ItHunts.Debugging.MovementDebugOverlay o in p2.GetComponentsInChildren<ItHunts.Debugging.MovementDebugOverlay>(true))
                o.enabled = false;                                       // IMGUI plein écran : coupé pour le 2e joueur
            Layout();
            Joins++;
            return p2.gameObject;
        }

        public int Leaves { get; private set; }

        /// <summary>
        /// Le 2e joueur quitte l'écran partagé (menu pause) : il disparaît, le 1er reprend tout l'écran. Si le 1er était à
        /// terre, plus personne ne peut le relever : il meurt (fin de partie). Le 1er joueur ne peut pas quitter ainsi.
        /// </summary>
        public bool Leave(PlayerInput player)
        {
            if (player == null || PlayerInput.all.Count < 2 || PlayerInput.all[0] == player)
                return false;
            RunSession.CoopDevice = null;                                // le niveau suivant ne le rajoute pas
            PlayerLook look = player.GetComponent<PlayerLook>();
            if (look != null && look.ViewRig != null)
                look.ViewRig.gameObject.SetActive(false);                // sa caméra s'arrête tout de suite
            player.gameObject.SetActive(false);                          // sort de PlayerInput.all et PlayerHealth.All
            Destroy(player.gameObject);
            Layout();
            foreach (PlayerHealth p in PlayerHealth.All.ToList())
                if (p != null)
                    p.NoAllyLeft();
            Leaves++;
            return true;
        }

        private static void LockFirstPlayer(PlayerInput p1, InputDevice joining)
        {
            p1.neverAutoSwitchControlSchemes = true;
            if (p1.currentControlScheme == "Gamepad")
            {
                Gamepad own = p1.devices.OfType<Gamepad>().FirstOrDefault(g => g != joining) ?? Gamepad.all.FirstOrDefault(g => g != joining);
                if (own != null)
                    p1.SwitchCurrentControlScheme("Gamepad", own);
            }
            else if (Keyboard.current != null && Mouse.current != null)
                p1.SwitchCurrentControlScheme("KeyboardMouse", Keyboard.current, Mouse.current);
        }

        /// <summary>Moitié d'écran du joueur index parmi count (gauche / droite, ou haut / bas).</summary>
        public static Rect ViewRect(int index, int count, bool horizontal) =>
            count < 2 ? new Rect(0f, 0f, 1f, 1f)
            : horizontal ? new Rect(0f, index == 0 ? 0.5f : 0f, 1f, 0.5f)
            : new Rect(index == 0 ? 0f : 0.5f, 0f, 0.5f, 1f);

        /// <summary>Facteur de FOV d'une moitié d'écran : 1 en solo, réglages de CoopSettings sinon.</summary>
        public static float FovScaleFor(int count, bool horizontal, CoopSettings s) =>
            count < 2 || s == null ? 1f : horizontal ? s.splitStackedFovScale : s.splitSideFovScale;

        /// <summary>Découpe de l'écran (gauche / droite ou haut / bas) et canaux Cinemachine.</summary>
        public void Layout()
        {
            bool horizontal = settings != null && settings.splitHorizontal;
            int n = PlayerInput.all.Count;
            for (int i = 0; i < n; i++)
            {
                PlayerInput pi = PlayerInput.all[i];
                Camera cam = pi.camera;
                if (cam == null)
                    continue;
                cam.rect = ViewRect(i, n, horizontal);
                OutputChannels ch = i == 0 ? OutputChannels.Default : OutputChannels.Channel01;
                var brain = cam.GetComponent<CinemachineBrain>();
                if (brain != null)
                    brain.ChannelMask = ch;
                // Le rig de vue est détaché du joueur au démarrage (PlayerLook) : ses caméras virtuelles ne sont pas des enfants.
                PlayerLook look = pi.GetComponent<PlayerLook>();
                if (look != null)
                    look.FovScale = FovScaleFor(n, horizontal, settings);
                Transform rig = look != null && look.ViewRig != null ? look.ViewRig : pi.transform;
                foreach (CinemachineCamera v in rig.GetComponentsInChildren<CinemachineCamera>(true))
                    v.OutputChannel = ch;
            }
        }

        // ---------- Corps vus par l'autre, mains de chacun, spectateur ----------

        private Transform Body(PlayerHealth p, int index)
        {
            if (_bodies.TryGetValue(p, out Transform t) && t != null)
                return t;
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Destroy(go.GetComponent<Collider>());
            go.name = "Corps (vu par les autres joueurs)";
            go.transform.SetParent(p.transform, false);
            go.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            go.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
            if (_bodyMat1 == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                _bodyMat1 = new Material(shader) { color = new Color(0.25f, 0.55f, 1f) };
                _bodyMat2 = new Material(shader) { color = new Color(1f, 0.55f, 0.15f) };
            }

            go.GetComponent<MeshRenderer>().sharedMaterial = index == 0 ? _bodyMat1 : _bodyMat2;
            _bodies[p] = go.transform;
            WorldLabel label = WorldLabel.Create(p.transform, new Vector3(0f, 1.3f, 0f), "À terre\nmaintiens E / Y", new Color(1f, 0.4f, 0.3f), 0.2f);
            label.VisibleFor = viewer => viewer != p && p.Downed && !p.Spectating;
            _labels[p] = new WorldLabelHolder { Label = label };
            return go.transform;
        }

        private void LateUpdate()
        {
            List<PlayerHealth> players = PlayerHealth.All.Where(p => p != null).ToList();
            for (int i = 0; i < players.Count; i++)
            {
                PlayerHealth p = players[i];
                if (players.Count < 2 && !_bodies.ContainsKey(p))
                    continue;
                Transform body = Body(p, i);
                body.localRotation = p.Downed ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
                body.localPosition = p.Downed ? new Vector3(0f, 0.4f, 0f) : new Vector3(0f, 0.9f, 0f);

                // Spectateur : sa caméra suit celle d'un allié vivant.
                Camera cam = Interactables.CameraOf(p);
                if (cam == null)
                    continue;
                var brain = cam.GetComponent<CinemachineBrain>();
                PlayerHealth ally = p.Spectating ? players.FirstOrDefault(o => o != p && !o.Dead) : null;
                if (brain != null)
                    brain.enabled = ally == null;
                if (ally != null)
                {
                    Camera ac = Interactables.CameraOf(ally);
                    if (ac != null)
                        cam.transform.SetPositionAndRotation(ac.transform.position, ac.transform.rotation);
                }
            }
        }

        private void BeforeCamera(ScriptableRenderContext context, Camera cam)
        {
            if (PlayerHealth.All.Count < 2)
                return;
            PlayerHealth viewer = Interactables.PlayerOfCamera(cam);
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null)
                    continue;
                Camera own = Interactables.CameraOf(p);
                bool mine = viewer == null || p == viewer;
                if (own != null)
                    foreach (Renderer r in own.GetComponentsInChildren<Renderer>(true))
                        if (!(r is LineRenderer))
                            r.enabled = mine;                            // mains et orbe : seulement dans sa propre vue
                if (_bodies.TryGetValue(p, out Transform body) && body != null)
                    body.GetComponent<MeshRenderer>().enabled = !mine && !p.Spectating && viewer != null;
            }
        }

        // ---------- Bandeaux (IMGUI léger, dans la moitié de chaque joueur) ----------

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || PlayerLook.MenuOpen)
                return;
            if (_banner == null)
            {
                _banner = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _banner.normal.textColor = new Color(1f, 0.5f, 0.4f);
                _small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
                _small.normal.textColor = Color.white;
            }

            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null)
                    continue;
                Camera cam = Interactables.CameraOf(p);
                Rect v = cam != null ? cam.pixelRect : new Rect(0f, 0f, Screen.width, Screen.height);
                var r = new Rect(v.x, Screen.height - v.yMax, v.width, v.height);
                if (p.Spectating)
                    GUI.Label(new Rect(r.x, r.y + r.height * 0.12f, r.width, 40f), "Spectateur — tu reviendras à la zone suivante", _banner);
                else if (p.Downed)
                    GUI.Label(new Rect(r.x, r.y + r.height * 0.4f, r.width, 40f), $"À terre — un allié peut te relever ({Mathf.CeilToInt(p.DownedLeft)} s)", _banner);
                var rev = p.GetComponent<CoopRevive>();
                if (rev != null && rev.Target != null)
                {
                    float hold = settings != null ? settings.reviveHoldSeconds : 3f;
                    var bar = new Rect(r.x + r.width * 0.35f, r.y + r.height * 0.62f, r.width * 0.3f, 14f);
                    GUI.color = new Color(0f, 0f, 0f, 0.6f);
                    GUI.DrawTexture(bar, Texture2D.whiteTexture);
                    GUI.color = new Color(0.4f, 1f, 0.5f);
                    GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(rev.Progress / hold), bar.height), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(bar.x, bar.y - 24f, bar.width, 22f), "Réanimation…", _small);
                }
            }

            if (PlayerInput.all.Count == 1 && Gamepad.all.Count > (PlayerInput.all[0].currentControlScheme == "Gamepad" ? 1 : 0))
                GUI.Label(new Rect(0f, Screen.height - 34f, Screen.width - 16f, 26f), "Joueur 2 : appuie sur Start sur ta manette", _small);
        }
    }
}
