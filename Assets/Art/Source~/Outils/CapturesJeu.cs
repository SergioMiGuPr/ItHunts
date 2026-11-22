// Captures en jeu à points fixes (nuit 4, 2026-11-22) : mêmes angles avant / après chaque lot, éclairage du jeu.
// 1) unity cmd script run --file CapturesJeu.cs --entry CapturesJeu.Prep --args '["Assets/Scenes/Acte_1_Caves.unity", 4242]'
// 2) unity command editor.playmode start
// 3) unity cmd script run --file CapturesJeu.cs --entry CapturesJeu.Shoot --args '["<dossier>", "avant", 5]'
// 4) unity command editor.playmode stop
// Points : la seed fixe donne le même plan ; salles choisies dans l'ordre (départ, hall de sortie, puis les plus grandes de
// fonctions différentes). Par salle : « loin » (centre, œil à 1.7 m, vers la plus grande profondeur) et « pres » (2.5 m du mur le
// plus proche, face à lui). Caméra = copie de celle du joueur (post-process, rendu, FOV), joueur déplacé au point (sa lanterne
// éclaire comme en jeu), temps figé. HUD exclu (image de page de magasin).
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ItHunts.Level.Acts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class CapturesJeu
{
    public static string Prep(string scene, int seed)
    {
        EditorSettings.asyncShaderCompilation = false;
        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        var lvl = UnityEngine.Object.FindFirstObjectByType<ActLevel>();
        if (lvl == null) return "pas d'ActLevel (scène " + scene + ")";
        var so = new SerializedObject(lvl);
        so.FindProperty("fixedSeed").intValue = seed;
        so.ApplyModifiedPropertiesWithoutUndo();
        return "ok " + scene + " seed " + seed;
    }

    static Camera PlayerCam()
    {
        foreach (var p in ItHunts.Player.PlayerHealth.All)
        {
            var pi = p != null ? p.GetComponent<UnityEngine.InputSystem.PlayerInput>() : null;
            if (pi != null && pi.camera != null) return pi.camera;
        }
        return Camera.main;
    }

    public struct Vue { public string nom; public Vector3 pos; public Vector3 dir; }

    static float Ray(Vector3 o, Vector3 d)
    {
        return Physics.Raycast(o, d, out RaycastHit h, 120f, ~0, QueryTriggerInteraction.Ignore) ? h.distance : 120f;
    }

    public static List<Vue> Points(ActPlan plan, int nbSalles)
    {
        var rooms = new List<ActRoom>();
        if (plan.Start >= 0) rooms.Add(plan.Rooms[plan.Start]);
        if (plan.ExitHall >= 0) rooms.Add(plan.Rooms[plan.ExitHall]);
        var fonctions = new HashSet<string>(rooms.Select(r => r.Function));
        foreach (var r in plan.Rooms.Where(r => !r.Special).OrderByDescending(r => r.Cells.Count).ThenBy(r => r.Id))
        {
            if (rooms.Count >= nbSalles) break;
            if (rooms.Contains(r) || fonctions.Contains(r.Function)) continue;
            rooms.Add(r); fonctions.Add(r.Function);
        }
        var vues = new List<Vue>();
        foreach (var r in rooms)
        {
            // case libre la plus proche du centre
            Vector2Int best = r.Cells[0]; float bd = float.MaxValue;
            foreach (var c in r.Cells)
            {
                float d = (c - r.CenterCell).sqrMagnitude;
                if (d < bd) { bd = d; best = c; }
            }
            Vector3 eye = plan.CellCenter(best, r.Floor + 1.7f);
            if (Physics.Raycast(eye + Vector3.up * 6f, Vector3.down, out RaycastHit g, 20f, ~0, QueryTriggerInteraction.Ignore))
                eye.y = g.point.y + 1.7f;
            Vector3 far = Vector3.forward, near = Vector3.forward; float fd = 0, nd = 999;
            for (int i = 0; i < 16; i++)
            {
                Vector3 d = Quaternion.Euler(0, i * 22.5f, 0) * Vector3.forward;
                float l = Ray(eye, d);
                if (l > fd) { fd = l; far = d; }
                if (l < nd) { nd = l; near = d; }
            }
            string n = $"{r.Id:00}_{Sanit(r.Function)}";
            vues.Add(new Vue { nom = n + "_loin", pos = eye, dir = Quaternion.AngleAxis(-4f, Vector3.Cross(far, Vector3.up)) * far });
            Vector3 p = eye + near * Mathf.Max(0f, nd - 2.5f);
            vues.Add(new Vue { nom = n + "_pres", pos = p, dir = Quaternion.AngleAxis(-8f, Vector3.Cross(near, Vector3.up)) * near });
        }
        return vues;
    }

    static string Sanit(string s) => string.IsNullOrEmpty(s) ? "salle" : new string(s.Normalize(System.Text.NormalizationForm.FormD).Where(c => c < 128 && char.IsLetterOrDigit(c)).ToArray());

    public static async Task<string> Shoot(string dossier, string etiquette, int nbSalles)
    {
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 240f && (ActLevel.Current == null || ActLevel.Current.Loading || ActLevel.Current.Plan == null || PlayerCam() == null))
            await Task.Delay(500);
        if (ActLevel.Current == null) return "pas de niveau";
        await Task.Delay(1500);
        Camera src = PlayerCam();
        Transform player = ItHunts.Player.PlayerHealth.All.Count > 0 ? ItHunts.Player.PlayerHealth.All[0].transform : null;
        var go = new GameObject("CaptureCam");
        var cam = go.AddComponent<Camera>();
        cam.CopyFrom(src);
        var dst = go.AddComponent<UniversalAdditionalCameraData>();
        var sd = src.GetComponent<UniversalAdditionalCameraData>();
        if (sd != null)
        {
            dst.renderPostProcessing = sd.renderPostProcessing;
            dst.antialiasing = sd.antialiasing;
            dst.SetRenderer(0);
            dst.renderShadows = sd.renderShadows;
            dst.volumeLayerMask = sd.volumeLayerMask;
        }
        cam.cullingMask = src.cullingMask & ~LayerMask.GetMask("UI");
        cam.enabled = false;
        int W = 1920, H = 1080;
        var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        Directory.CreateDirectory(dossier);
        var vues = Points(ActLevel.Current.Plan, nbSalles);
        var lignes = new List<string>();
        foreach (var v in vues)
        {
            if (player != null)
            {
                var cc = player.GetComponent<CharacterController>();
                if (cc) cc.enabled = false;
                player.position = v.pos - Vector3.up * 1.6f - v.dir.normalized * 0.3f;
                if (cc) cc.enabled = true;
            }
            cam.transform.SetPositionAndRotation(v.pos, Quaternion.LookRotation(v.dir));
            Time.timeScale = 1f;
            await Task.Delay(700);   // lumières, culling, lanterne
            Time.timeScale = 0f;
            await Task.Delay(100);
            cam.targetTexture = rt;
            cam.Render(); cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            string f = Path.Combine(dossier, $"{v.nom}_{etiquette}.jpg");
            File.WriteAllBytes(f, tex.EncodeToJPG(92));
            lignes.Add($"{v.nom}: pos {v.pos} dir {v.dir}");
        }
        Time.timeScale = 1f;
        UnityEngine.Object.Destroy(go); UnityEngine.Object.Destroy(rt); UnityEngine.Object.Destroy(tex);
        File.WriteAllLines(Path.Combine(dossier, $"points_{etiquette}.txt"), lignes);
        return $"{vues.Count} captures, seed {ActLevel.Current.LevelSeed}\n" + string.Join("\n", lignes);
    }
}

// Vues fixes dans une scène d'art (édition) : [nom, x, y, z, lacet, tangage] ; caméra du jeu (post-process, FOV 60).
public static class VuesFixes
{
    public static string Racines(string scene)
    {
        EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        var sb = new System.Text.StringBuilder();
        foreach (var g in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            var b = new Bounds(g.transform.position, Vector3.zero);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            sb.AppendLine($"{g.name} pos {g.transform.position} bounds c {b.center} s {b.size}");
        }
        return sb.ToString();
    }

    public static string Shoot(string scene, string dossier, string etiquette, string vuesCsv)
    {
        EditorSettings.asyncShaderCompilation = false;
        if (!string.IsNullOrEmpty(scene)) EditorSceneManager.OpenScene(scene, OpenSceneMode.Single);
        var go = new GameObject("CaptureCam") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.farClipPlane = 400f;
            var ad = go.AddComponent<UniversalAdditionalCameraData>();
            ad.renderPostProcessing = true; ad.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            int W = 1920, H = 1080;
            var rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            Directory.CreateDirectory(dossier);
            int n = 0;
            foreach (var l in vuesCsv.Split(';'))
            {
                var p = l.Split(',');
                if (p.Length < 6) continue;
                float f(int i) => float.Parse(p[i], System.Globalization.CultureInfo.InvariantCulture);
                cam.transform.SetPositionAndRotation(new Vector3(f(1), f(2), f(3)), Quaternion.Euler(f(5), f(4), 0));
                cam.targetTexture = rt; cam.Render(); cam.Render();
                RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply();
                RenderTexture.active = null; cam.targetTexture = null;
                File.WriteAllBytes(Path.Combine(dossier, $"{p[0].Trim()}_{etiquette}.jpg"), tex.EncodeToJPG(92));
                n++;
            }
            UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(tex);
            return n + " vues";
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
