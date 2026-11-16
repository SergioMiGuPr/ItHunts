using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ItHunts.Enemies;
using ItHunts.Level;
using ItHunts.Player;
using Unity.Profiling;
using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Mesure de performance d'un niveau dans un build (étape 8, après playtest ; étape 9) : inactive sauf avec
    /// <c>-ithunts-perf-level</c> (et <c>-ithunts-perf-out chemin</c>). Joueur invulnérable au centre de la carte, timer avancé au
    /// pic des vagues de la fin (≈ 14 min), 5 s de chauffe puis 20 s de mesure : temps d'image, fil principal, fil de rendu, GPU
    /// (FrameTimingManager), marqueurs du jeu, nombre d'ennemis et de lumières ; puis quitte. <c>-ithunts-perf-split</c> : avec
    /// un 2e joueur en écran partagé (étape 9).
    /// </summary>
    public sealed class LevelPerfProbe : MonoBehaviour
    {
        private readonly StringBuilder _report = new StringBuilder();
        private string _outPath;
        private bool _split;

        /// <summary>Le 2e joueur de la mesure en écran partagé (posé par l'étape 9 : PlayerJoin).</summary>
        public static System.Func<GameObject> SpawnSecondPlayer;

        private void Awake()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            if (!args.Contains("-ithunts-perf-level"))
            {
                enabled = false;
                return;
            }

            _split = args.Contains("-ithunts-perf-split");
            Application.runInBackground = true;                          // la mesure continue sans le focus
            int i = System.Array.IndexOf(args, "-ithunts-perf-out");
            _outPath = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "perf_level.txt");
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

        private static double Pct(List<double> v, double p)
        {
            if (v.Count == 0)
                return 0;
            var s = v.OrderBy(x => x).ToList();
            return s[Mathf.Clamp((int)(p * (s.Count - 1)), 0, s.Count - 1)];
        }

        private IEnumerator Run()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var nav = FindFirstObjectByType<ZoneNavMesh>();
            while (nav != null && !nav.Built)
                yield return null;
            PlayerHealth.DebugInvulnerable = true;
            var zone = FindFirstObjectByType<ZoneState>();
            var motor = FindFirstObjectByType<PlayerMotor>();
            var mid = RoomVolume.All.Where(r => r.Kind == RoomKind.Salle && r.Size.x > 30f).OrderBy(r => new Vector2(r.transform.position.x, r.transform.position.z).sqrMagnitude).First();
            Vector3 at = new Vector3(mid.transform.position.x, mid.transform.position.y - mid.Size.y / 2f + 0.3f, mid.transform.position.z);
            motor.Teleport(at);
            yield return null;
            if (_split && SpawnSecondPlayer == null)
                Write("ERREUR : pas d'écran partagé dans la scène");
            if (_split && SpawnSecondPlayer != null)
            {
                GameObject p2 = SpawnSecondPlayer();
                if (p2 != null && p2.GetComponent<PlayerMotor>() != null)
                    p2.GetComponent<PlayerMotor>().Teleport(at + Vector3.right * 3f);
            }

            yield return new WaitForSeconds(0.5f);
            string[] args = System.Environment.GetCommandLineArgs();
            string variant = "";
            if (!args.Contains("-ithunts-perf-debug"))
                foreach (MovementDebugOverlay o in FindObjectsByType<MovementDebugOverlay>(FindObjectsSortMode.None))
                    o.enabled = false;                                   // affichage de debug (F3) coupé : on mesure le jeu
            if (args.Contains("-ithunts-perf-nolights"))
            {
                int off = 0;
                foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Point)
                    {
                        l.enabled = false;
                        off++;
                    }

                variant += $" ; {off} lumières ponctuelles coupées";
            }

            int fi = System.Array.IndexOf(args, "-ithunts-perf-far");
            if (fi >= 0 && fi + 1 < args.Length && float.TryParse(args[fi + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float far))
            {
                foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    cam.farClipPlane = Mathf.Min(cam.farClipPlane, far);
                variant += $" ; distance d'affichage {far} m";
            }

            if (zone != null)
                zone.DebugAdvance(820f - zone.Elapsed);
            // Pic de vague réellement actif : on avance encore par pas de 5 s jusqu'au prochain pic (selon les réglages des vagues).
            var director = FindFirstObjectByType<ItHunts.Enemies.WaveDirector>();
            for (int k = 0; k < 120 && zone != null && director != null && !director.InPeak; k++)
            {
                zone.DebugAdvance(5f);
                yield return new WaitForSeconds(0.1f);
            }

            float elapsedAt = zone != null ? zone.Elapsed : 0f;
            Write($"Niveau {gameObject.scene.name}, {Screen.width}×{Screen.height}, {(_split ? "écran partagé à 2" : "1 joueur")} ; salle {mid.RoomName} ; timer à {Mathf.FloorToInt(elapsedAt / 60f)}:{Mathf.FloorToInt(elapsedAt % 60f):00}, pic {(director != null && director.InPeak ? "ACTIF" : "non")}{variant}");
            yield return new WaitForSeconds(5f);

            var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 15);
            var markers = new[] { "ItHunts.Ghoul", "ItHunts.Stalker", "ItHunts.Waves", "ItHunts.Flyer", "ItHunts.Souls", "ItHunts.PlayerHud",
                    "FixedUpdate.ScriptRunBehaviourFixedUpdate", "Update.ScriptRunBehaviourUpdate", "PreLateUpdate.ScriptRunBehaviourLateUpdate",
                    "FixedUpdate.PhysicsFixedUpdate", "PostLateUpdate.FinishFrameRendering", "Camera.Render", "CullScriptable",
                    "RenderPipelineManager.DoRenderLoop_Internal()", "UpdateRendererBoundingVolumes", "GUI.Repaint", "UGUI.Rendering.UpdateBatches" }
                .Select(n => (n, ProfilerRecorder.StartNew(ProfilerCategory.Scripts, n, 15))).ToList();
            var physics = ProfilerRecorder.StartNew(ProfilerCategory.Physics, "Physics.Simulate", 15);
            var drawCalls = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count", 15);
            var batches = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count", 15);
            var frame = new List<double>();
            var cpuMain = new List<double>();
            var cpuRender = new List<double>();
            var gpu = new List<double>();
            var mainRec = new List<double>();
            var phys = new List<double>();
            var mk = markers.ToDictionary(m => m.n, m => new List<double>());
            var dc = new List<double>();
            var bt = new List<double>();
            var timings = new FrameTiming[1];
            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 20f)
            {
                yield return null;
                frame.Add(Time.unscaledDeltaTime * 1000.0);
                FrameTimingManager.CaptureFrameTimings();
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                {
                    cpuMain.Add(timings[0].cpuMainThreadFrameTime);
                    cpuRender.Add(timings[0].cpuRenderThreadFrameTime);
                    gpu.Add(timings[0].gpuFrameTime);
                }

                mainRec.Add(main.LastValue / 1e6);
                phys.Add(physics.LastValue / 1e6);
                foreach (var m in markers)
                    mk[m.n].Add(m.Item2.LastValue / 1e6);
                dc.Add(drawCalls.LastValue);
                bt.Add(batches.LastValue);
            }

            int enemies = FindObjectsByType<Ghoul>(FindObjectsSortMode.None).Count(g => g.isActiveAndEnabled && g.transform.position.y > -30f)
                          + FindObjectsByType<Flyer>(FindObjectsSortMode.None).Count(f => f.isActiveAndEnabled && f.transform.position.y > -30f);
            var waves = FindFirstObjectByType<WaveDirector>();
            Write($"image : médiane {Pct(frame, 0.5):0.0} ms, 95 % {Pct(frame, 0.95):0.0} ms, max {frame.Max():0.0} ms ; images > 16.6 ms : {frame.Count(f => f > 16.6)}/{frame.Count}");
            Write($"fil principal (FrameTiming) : médiane {Pct(cpuMain, 0.5):0.0} ms, 95 % {Pct(cpuMain, 0.95):0.0} ms ; fil de rendu : médiane {Pct(cpuRender, 0.5):0.0} ms ; GPU : médiane {Pct(gpu, 0.5):0.0} ms, 95 % {Pct(gpu, 0.95):0.0} ms");
            Write($"Main Thread (profileur) médiane {Pct(mainRec, 0.5):0.0} ms ; physique {Pct(phys, 0.5):0.00} ms ; draw calls {Pct(dc, 0.5):0}, batches {Pct(bt, 0.5):0}");
            Write("marqueurs (médiane ms) : " + string.Join(", ", mk.Select(kv => $"{kv.Key.Replace("ItHunts.", "")} {Pct(kv.Value, 0.5):0.00}")));
            Write($"ennemis actifs {enemies} ; au pic {(waves != null && waves.InPeak)} ; lumières {FindObjectsByType<Light>(FindObjectsSortMode.None).Length} ; joueurs {PlayerHealth.All.Count}");
            Write("#fin");
            PlayerHealth.DebugInvulnerable = false;
            Application.Quit();
        }
    }
}
