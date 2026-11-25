// Lot 3 (nuit 5, relecture) : exposition contrôlée des fenêtres de l'acte II. Un mur plein de château tapissé (le cas des
// cartes de main : fenêtre posée contre un mur), l'ancienne lancette et la nouvelle fenêtre avec vue côte à côte ; éclairage
// du jeu (ambiance, brouillard, torches 1.8 / 11 m orange), profil de post-process du jeu, FOV 60. Rendus 1920 × 1080.
// `unity cmd ... script run --file ExpoFenetres.cs --entry ExpoFenetres.Shoot --args '["<dossier>"]'`
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ExpoFenetres
{
    static GameObject P(string n) => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Prefabs/KitChateau/Chateau_{n}.prefab");
    static Material M(string p) => AssetDatabase.LoadAssetAtPath<Material>(p);

    static void Torche(Vector3 p, float i = 1.8f)
    {
        var l = new GameObject("Torche").AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(1f, 0.36f, 0.16f); l.intensity = i; l.range = 11f; l.shadows = LightShadows.Soft;
        l.transform.position = p;
    }

    static void Rendu(Camera cam, string f)
    {
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt; cam.Render(); cam.Render();
        var prev = RenderTexture.active; RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); tex.Apply();
        RenderTexture.active = prev; cam.targetTexture = null;
        File.WriteAllBytes(f, tex.EncodeToJPG(92));
        Object.DestroyImmediate(tex); rt.Release(); Object.DestroyImmediate(rt);
    }

    public static string Shoot(string dossier)
    {
        Directory.CreateDirectory(dossier);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.055f, 0.058f, 0.08f);
        RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 10; RenderSettings.fogEndDistance = 85;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.035f);
        var lune = new GameObject("Lune").AddComponent<Light>();
        lune.type = LightType.Directional; lune.color = new Color(0.55f, 0.62f, 0.85f); lune.intensity = 0.25f;
        lune.transform.rotation = Quaternion.Euler(40, 30, 0);
        // mur plein tapissé (papier peint du château), sol de parquet
        var mur = GameObject.CreatePrimitive(PrimitiveType.Cube);
        mur.transform.position = new Vector3(0, 10, 0.5f); mur.transform.localScale = new Vector3(40, 20, 1);
        mur.GetComponent<MeshRenderer>().sharedMaterial = M("Assets/Art/Materials/Bibliotheque/M_Bib_Tenture.mat");
        var sol = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sol.transform.position = new Vector3(0, -0.5f, -15); sol.transform.localScale = new Vector3(40, 1, 32);
        sol.GetComponent<MeshRenderer>().sharedMaterial = M("Assets/Art/Materials/Chateau/M_Chateau_Parquet.mat");
        // fenêtres contre le mur, face à la salle (-Z) : ancienne lancette à gauche, fenêtre avec vue à droite
        var face = Quaternion.LookRotation(Vector3.back);
        Object.Instantiate(P("Fenetre_Lancette_Double"), new Vector3(-6.5f, 0, 0), face);
        Object.Instantiate(P("Fenetre_Vue_Double"), new Vector3(6.5f, 0, 0), face);
        Object.Instantiate(P("Fenetre_Vue_Triple"), new Vector3(17f, 0, 0), face);
        Object.Instantiate(P("Torche_Murale"), new Vector3(0, 2.4f, -0.05f), face);
        Torche(new Vector3(0, 2.9f, -0.6f)); Torche(new Vector3(-12, 2.9f, -0.6f)); Torche(new Vector3(12, 2.9f, -0.6f));
        // post-process du jeu
        var vol = new GameObject("PostProcess").AddComponent<Volume>();
        vol.isGlobal = true; vol.sharedProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>("Assets/_Project/Settings/PostProcess_TestMovement.asset");
        var cam = new GameObject("Camera").AddComponent<Camera>();
        cam.fieldOfView = 60; cam.nearClipPlane = 0.05f; cam.farClipPlane = 300; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        var cd = cam.gameObject.AddComponent<UniversalAdditionalCameraData>(); cd.renderPostProcessing = true; cd.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        void Vue(string n, Vector3 oeil, Vector3 cible) { cam.transform.SetPositionAndRotation(oeil, Quaternion.LookRotation(cible - oeil)); Rendu(cam, Path.Combine(dossier, n + ".jpg")); }
        Vue("Fenetres_AvantApres_loin", new Vector3(0, 1.7f, -17), new Vector3(0, 6.5f, 0));
        Vue("Fenetre_Avant_pres", new Vector3(-6.5f, 1.7f, -6.5f), new Vector3(-6.5f, 6.8f, 0));
        Vue("Fenetre_Vue_pres", new Vector3(6.5f, 1.7f, -6.5f), new Vector3(6.5f, 6.8f, -0.8f));
        Vue("Fenetre_Vue_biais", new Vector3(1.0f, 1.7f, -7f), new Vector3(6.5f, 7f, -0.8f));
        Vue("Fenetre_Vue_Triple_pres", new Vector3(17f, 1.7f, -9f), new Vector3(17f, 10f, -0.8f));
        return "ok " + dossier;
    }
}
