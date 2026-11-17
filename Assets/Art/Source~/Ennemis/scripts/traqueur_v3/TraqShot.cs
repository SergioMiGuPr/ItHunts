// Capture du traqueur dans Unity (shader toon, matériaux réels) : clips échantillonnés, maillage cuit (BakeMesh) ;
// volume signé de chaque maillage (positif = faces vers l'extérieur).
using System.IO; using System.Linq; using System.Text; using UnityEditor; using UnityEditor.SceneManagement; using UnityEngine;
using UnityEngine.Rendering.Universal;
public static class TraqShot
{
    public static float Volume(Mesh m)
    {
        var v = m.vertices; var t = m.triangles; double s = 0;
        for (int i = 0; i < t.Length; i += 3) s += Vector3.Dot(v[t[i]], Vector3.Cross(v[t[i + 1]], v[t[i + 2]])) / 6.0;
        return (float)s;
    }
    public static string Run(string outDir, string fbx, string clips)
    {
        var sb = new StringBuilder();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.12f, 0.11f, 0.14f);
        var lg = new GameObject("lune").AddComponent<Light>(); lg.type = LightType.Directional; lg.intensity = 1.6f; lg.color = new Color(0.8f, 0.82f, 1f);
        lg.transform.rotation = Quaternion.Euler(35, 150, 0);
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
        var go = Object.Instantiate(src);
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>()) sb.AppendFormat("{0} : volume {1:0} m³ ; ", r.name, Volume(r.sharedMesh));
        var all = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().ToArray();
        var camGo = new GameObject("cam"); var cam = camGo.AddComponent<Camera>(); cam.fieldOfView = 40; cam.farClipPlane = 300;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.05f, 0.05f, 0.07f);
        Directory.CreateDirectory(outDir);
        foreach (var spec in clips.Split(';'))
        {
            var p = spec.Split(':'); var clip = all.First(c => c.name.EndsWith(p[0]));
            clip.SampleAnimation(go, float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));
            var baked = new System.Collections.Generic.List<GameObject>();
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var m = new Mesh(); smr.BakeMesh(m, true);
                var g = new GameObject("_cuit"); g.transform.SetPositionAndRotation(smr.transform.position, smr.transform.rotation);
                g.AddComponent<MeshFilter>().sharedMesh = m; g.AddComponent<MeshRenderer>().sharedMaterials = smr.sharedMaterials; smr.enabled = false; baked.Add(g);
            }
            camGo.transform.position = new Vector3(-17, 12, 22); camGo.transform.LookAt(new Vector3(0, 7.5f, 2));
            var rt = new RenderTexture(1200, 1200, 24); cam.targetTexture = rt; cam.Render(); cam.Render();
            RenderTexture.active = rt; var tx = new Texture2D(1200, 1200, TextureFormat.RGB24, false); tx.ReadPixels(new Rect(0, 0, 1200, 1200), 0, 0); tx.Apply();
            RenderTexture.active = null; cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(outDir, "u_" + p[0] + ".jpg"), tx.EncodeToJPG(90));
            foreach (var g in baked) Object.DestroyImmediate(g);
            foreach (var smr in go.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.enabled = true;
            sb.Append("capture " + p[0] + " ; ");
        }
        return sb.ToString();
    }
}
