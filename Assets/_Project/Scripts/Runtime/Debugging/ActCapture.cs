using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItHunts.Level.Acts;
using ItHunts.Player;
using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Captures à points fixes (nuit 4) : pour chaque salle de l'acte chargé, deux vues depuis des points déduits du plan (donc les mêmes
    /// avant et après une modification du générateur, à seed égale) : A, depuis le seuil de la première porte vers le centre de la salle ;
    /// B, depuis le centre vers la porte la plus éloignée. La caméra du joueur rend l'image (éclairage et post-traitement du jeu, lanterne
    /// du joueur comprise : le joueur est téléporté au point de vue). Outil de mesure (Play Mode) : jamais en partie.
    /// </summary>
    public static class ActCapture
    {
        public const int Width = 1280, Height = 720;

        /// <summary>Dossier des captures de la nuit, relatif à la racine du projet.</summary>
        public static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "design", "jour-5", "captures"));

        public struct Shot
        {
            public string Name;
            public Vector3 Eye, LookAt;
        }

        /// <summary>Points de vue de l'acte chargé (déterministes à partir du plan).</summary>
        public static List<Shot> Shots(ActPlan p)
        {
            var shots = new List<Shot>();
            if (p == null)
                return shots;
            foreach (ActRoom r in p.Rooms)
            {
                string safe = Safe($"{r.Id:00}_{r.Name}");
                Vector3 center = p.CellCenter(r.CenterCell, r.Floor);
                if (r.Use.TryGetValue(Vector2Int.RoundToInt(r.CenterCell), out CellUse u) && u == CellUse.Terrasse)
                    center.y += r.TerraceHeight;
                if (r.Doors.Count > 0)
                {
                    ActDoor first = p.Doors[r.Doors.OrderBy(i => i).First()];
                    Vector3 eye = p.DoorFront(first, r.Id, 3.5f) + Vector3.up * 1.7f;
                    shots.Add(new Shot { Name = safe + "_A_porte", Eye = eye, LookAt = center + Vector3.up * 1.2f });
                    ActDoor far = p.Doors[r.Doors.OrderByDescending(i => Vector3.Distance(p.DoorCenter(p.Doors[i]), center)).First()];
                    shots.Add(new Shot { Name = safe + "_B_centre", Eye = center + Vector3.up * 1.7f, LookAt = p.DoorCenter(far) + Vector3.up * 2.5f });
                }
                else
                    shots.Add(new Shot { Name = safe + "_B_centre", Eye = center + Vector3.up * 1.7f, LookAt = center + Vector3.forward * 10f + Vector3.up * 1.5f });
            }

            return shots;
        }

        /// <summary>Point libre (0,8 m autour de l'œil) le plus proche : le décor posé depuis ne bouche pas la vue.</summary>
        private static Vector3 FreeEye(Vector3 eye, Vector3 look)
        {
            for (int i = 0; i < 120; i++)
            {
                float r = 0.5f * Mathf.Sqrt(i);
                Vector3 p = eye + Quaternion.Euler(0f, i * 137.5f, 0f) * Vector3.forward * r;
                if (!Physics.CheckSphere(p, 0.8f, ~0, QueryTriggerInteraction.Ignore) && !Physics.SphereCast(p, 0.6f, (look - p).normalized, out _, 5f, ~0, QueryTriggerInteraction.Ignore) && Physics.Raycast(p, Vector3.down, 2.4f, ~0, QueryTriggerInteraction.Ignore)
                    && !Physics.Raycast(p, Vector3.down, 1.2f, ~0, QueryTriggerInteraction.Ignore))
                    return p;
            }

            return eye;
        }

        public static string Safe(string s)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s.Replace(' ', '_').Replace('\'', '_');
        }

        /// <summary>
        /// Capture toutes les vues de l'acte chargé dans <paramref name="folder"/> (créé). Le joueur est téléporté à chaque point de vue ;
        /// les ennemis ne sont pas touchés. Renvoie le nombre d'images écrites.
        /// </summary>
        public static IEnumerator CaptureAll(string folder, System.Action<int> done = null)
        {
            ActLevel level = ActLevel.Current;
            PlayerHealth player = PlayerHealth.All.FirstOrDefault(x => x != null);
            Camera cam = ViewCamera();
            if (level == null || level.Plan == null || player == null || cam == null)
            {
                Debug.LogWarning("ActCapture : pas d'acte, de joueur ou de caméra");
                done?.Invoke(0);
                yield break;
            }

            Directory.CreateDirectory(folder);
            var look = player.GetComponent<PlayerLook>();
            bool wasInvulnerable = PlayerHealth.DebugInvulnerable;
            PlayerHealth.DebugInvulnerable = true;
            int n = 0;
            foreach (Shot s in Shots(level.Plan))
            {
                Vector3 eye = FreeEye(s.Eye, s.LookAt);
                player.Motor.Teleport(eye - Vector3.up * 1.6f);
                Vector3 d = s.LookAt - eye;
                float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
                look?.SetView(yaw, Mathf.Clamp(pitch, -60f, 60f));
                yield return null;
                yield return null;
                yield return new WaitForSecondsRealtime(0.6f);              // lumières et décor de la nouvelle salle (DressingOptimizer)
                yield return new WaitForEndOfFrame();
                if (Save(cam, Path.Combine(folder, s.Name + ".jpg")))
                    n++;
            }

            PlayerHealth.DebugInvulnerable = wasInvulnerable;
            done?.Invoke(n);
        }

        /// <summary>Rend la caméra dans une texture et l'écrit en JPEG.</summary>
        /// <summary>Caméra qui rend la vue du joueur : la principale, sinon la caméra active de plus grande profondeur.</summary>
        public static Camera ViewCamera()
        {
            if (Camera.main != null)
                return Camera.main;
            Camera best = null;
            foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (c.isActiveAndEnabled && c.targetTexture == null && (best == null || c.depth > best.depth))
                    best = c;
            return best;
        }

        public static bool Save(Camera cam, string path, int width = Width, int height = Height)
        {
            if (cam == null)
                cam = ViewCamera();
            if (cam == null)
                return false;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture previous = cam.targetTexture;
            try
            {
                cam.targetTexture = rt;
                cam.Render();
                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToJPG(88));
                Object.Destroy(tex);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("ActCapture : " + e.Message);
                return false;
            }
            finally
            {
                cam.targetTexture = previous;
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}
