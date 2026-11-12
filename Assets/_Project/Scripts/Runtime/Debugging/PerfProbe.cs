using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ItHunts.Hud;
using ItHunts.Player;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Mesure de performance dans un build (étape 5, D-028) : coût des mains (caméra superposée) et du HUD. Inactive sauf si
    /// le build est lancé avec <c>-ithunts-perf</c> (et <c>-ithunts-perf-out chemin</c> pour le fichier de résultats).
    /// Déroulé : 1 joueur, puis 2 joueurs en écran partagé ; pour chacun, blocs de 5 s alternés mains et HUD allumés /
    /// éteints, 30 s par état ; médianes du fil principal, du fil de rendu et du GPU (FrameTimingManager) ; puis quitte.
    /// </summary>
    public sealed class PerfProbe : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;
        [SerializeField, Min(1f)] private float blockSeconds = 5f;
        [SerializeField, Min(1)] private int blocksPerState = 6;
        [SerializeField, Min(0f)] private float warmupSeconds = 4f;

        private readonly StringBuilder _report = new StringBuilder();
        private string _outPath;

        private void Awake()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (!args.Contains("-ithunts-perf"))
            {
                enabled = false;
                return;
            }

            int i = System.Array.IndexOf(args, "-ithunts-perf-out");
            _outPath = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "perf.txt");
        }

        private void Start()
        {
            if (enabled)
                StartCoroutine(Run());
        }

        private void Write(string line)
        {
            _report.AppendLine(line);
            File.WriteAllText(_outPath, _report.ToString());
        }

        private static void Toggle(IEnumerable<PlayerMotor> players, bool on)
        {
            foreach (PlayerMotor p in players)
            {
                var view = p.GetComponent<FirstPersonView>();
                var hud = p.GetComponent<PlayerHud>();
                if (view != null)
                {
                    if (view.OverlayCamera != null)
                        view.OverlayCamera.enabled = on;
                    view.SetHandsVisible(on);
                    view.enabled = on;
                }

                if (hud != null)
                {
                    if (hud.Canvas != null)
                        hud.Canvas.enabled = on;
                    hud.enabled = on;
                }
            }
        }

        private static float Median(List<double> v)
        {
            if (v.Count == 0)
                return float.NaN;
            v.Sort();
            return (float)v[v.Count / 2];
        }

        private IEnumerator Measure(string label, List<PlayerMotor> players)
        {
            var main = new Dictionary<bool, List<double>> { [true] = new List<double>(), [false] = new List<double>() };
            var render = new Dictionary<bool, List<double>> { [true] = new List<double>(), [false] = new List<double>() };
            var gpu = new Dictionary<bool, List<double>> { [true] = new List<double>(), [false] = new List<double>() };
            var timings = new FrameTiming[1];
            for (int b = 0; b < blocksPerState * 2; b++)
            {
                bool on = b % 2 == 0;
                Toggle(players, on);
                for (int k = 0; k < 10; k++)
                    yield return null;                                   // laisser passer le changement
                float end = Time.realtimeSinceStartup + blockSeconds;
                while (Time.realtimeSinceStartup < end)
                {
                    yield return null;
                    FrameTimingManager.CaptureFrameTimings();
                    if (FrameTimingManager.GetLatestTimings(1, timings) == 0)
                        continue;
                    main[on].Add(timings[0].cpuMainThreadFrameTime);
                    render[on].Add(timings[0].cpuRenderThreadFrameTime);
                    gpu[on].Add(timings[0].gpuFrameTime);
                }
            }

            Toggle(players, true);
            float mOn = Median(main[true]), mOff = Median(main[false]);
            float rOn = Median(render[true]), rOff = Median(render[false]);
            float gOn = Median(gpu[true]), gOff = Median(gpu[false]);
            Write($"{label} ; mains et HUD allumés / éteints ({main[true].Count} / {main[false].Count} images) :");
            Write($"  fil principal {mOn:0.000} / {mOff:0.000} ms → surcoût {mOn - mOff:+0.000;-0.000} ms");
            Write($"  fil de rendu  {rOn:0.000} / {rOff:0.000} ms → surcoût {rOn - rOff:+0.000;-0.000} ms");
            Write($"  GPU           {gOn:0.000} / {gOff:0.000} ms → surcoût {gOn - gOff:+0.000;-0.000} ms");
            float total = Mathf.Max(mOn - mOff, rOn - rOff) + Mathf.Max(0f, gOn - gOff);
            Write($"  surcoût total (le plus lourd des deux fils CPU + GPU) : {total:+0.000;-0.000} ms");
        }

        private IEnumerator Run()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Write($"# Mesure ItHunts (build de développement), {Screen.width} × {Screen.height}, {SystemInfo.graphicsDeviceName}, {SystemInfo.processorType}");
            var motor = FindFirstObjectByType<PlayerMotor>();
            if (motor == null)
            {
                Write("ERREUR : pas de joueur");
                Application.Quit();
                yield break;
            }

            // Même endroit que les mesures dans l'éditeur : zone Télékinésie, factices et objets en vue.
            motor.Teleport(new Vector3(-75f, 0f, -30f));
            motor.GetComponent<PlayerLook>()?.SetView(0f, 5f);
            yield return new WaitForSeconds(warmupSeconds);
            var players = new List<PlayerMotor> { motor };
            yield return Measure("1 joueur", players);

            if (playerPrefab != null)
            {
                CinemachineCamera[] before = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
                GameObject p2go = Instantiate(playerPrefab, new Vector3(-70f, 0f, -30f), Quaternion.identity);
                foreach (AudioListener al in p2go.GetComponentsInChildren<AudioListener>(true))
                    al.enabled = false;
                yield return null;
                var p2 = p2go.GetComponent<PlayerMotor>();
                Camera c1 = motor.GetComponent<PlayerInput>().camera, c2 = p2.GetComponent<PlayerInput>().camera;
                CinemachineCamera vcam2 = FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None).FirstOrDefault(v => !before.Contains(v));
                var b1 = c1.GetComponent<CinemachineBrain>();
                var b2 = c2.GetComponent<CinemachineBrain>();
                if (b1 != null) b1.ChannelMask = OutputChannels.Default;
                if (b2 != null) b2.ChannelMask = OutputChannels.Channel01;
                if (vcam2 != null) vcam2.OutputChannel = OutputChannels.Channel01;
                c1.rect = new Rect(0f, 0f, 0.5f, 1f);
                c2.rect = new Rect(0.5f, 0f, 0.5f, 1f);
                p2.GetComponent<PlayerLook>()?.SetView(-20f, 5f);
                yield return new WaitForSeconds(warmupSeconds);
                players.Add(p2);
                yield return Measure("2 joueurs (écran partagé)", players);
            }
            else
            {
                Write("2 joueurs : pas de prefab joueur assigné, mesure sautée");
            }

            Write("#fin");
            Application.Quit();
        }
    }
}
