// Traqueur (art-traqueur) : matériaux (corps sur la nouvelle texture, tête et pieds sur la texture d'origine), remappage,
// contrôle de l'import (os, clips, matériaux, triangles).
using System.Linq; using System.Text; using UnityEditor; using UnityEngine;
public static class TraqUnity
{
    const string Mat = "Assets/Art/Materials/Ennemis/";
    const string Tex = "Assets/Art/Textures/Ennemis/";
    const string Fbx = "Assets/Art/Models/Ennemis/SK_Traqueur.fbx";
    public static string Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("ItHunts-art/Assets")) return "MAUVAIS PROJET";
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var sb = new StringBuilder();
        var ti = (TextureImporter)AssetImporter.GetAtPath(Tex + "T_Traqueur_Corps_BaseColor.png");
        var old = (TextureImporter)AssetImporter.GetAtPath(Tex + "T_Traqueur_BaseColor.png");
        ti.maxTextureSize = old.maxTextureSize; ti.textureCompression = old.textureCompression; ti.mipmapEnabled = true; ti.SaveAndReimport();
        var corps = AssetDatabase.LoadAssetAtPath<Material>(Mat + "M_Traqueur.mat");
        // tête et pieds : copie de l'ancien M_Traqueur (texture d'origine, émission des pupilles)
        string tp = Mat + "M_Traqueur_Tete.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(tp) == null) AssetDatabase.CopyAsset(Mat + "M_Traqueur.mat", tp);
        var tete = AssetDatabase.LoadAssetAtPath<Material>(tp);
        // corps : nouvelle texture (UV de la retopologie), pas d'émission (les yeux sont sur la tête)
        corps.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "T_Traqueur_Corps_BaseColor.png"));
        corps.SetTexture("_EmissionMap", null);
        corps.SetColor("_EmissionColor", Color.black);
        corps.SetFloat("_Emission", 0); corps.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(corps); EditorUtility.SetDirty(tete); AssetDatabase.SaveAssets();
        var imp = (ModelImporter)AssetImporter.GetAtPath(Fbx);
        foreach (var n in new[] { "M_Traqueur", "M_Traqueur_Mains", "M_Traqueur_Tete", "M_Traqueur_Cheveux" })
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(Mat + n + ".mat");
            if (m) imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), n), m);
        }
        var clips = imp.defaultClipAnimations;
        foreach (var c in clips)
        {
            string nm = c.name.Contains("|") ? c.name.Substring(c.name.LastIndexOf('|') + 1) : c.name;
            c.loopTime = new[] { "Traqueur_Repos", "Traqueur_Marche", "Traqueur_PassageBas", "Traqueur_Machoire" }.Contains(nm);
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(Fbx);
        foreach (var r in go.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            var bw = r.sharedMesh.GetBonesPerVertex(); int mx = 0; foreach (var b in bw) mx = Mathf.Max(mx, b);
            sb.AppendFormat("{0} : {1} tris, {2} os, max {3} os/sommet, matériaux [{4}]\n", r.name, r.sharedMesh.triangles.Length / 3, r.bones.Length, mx,
                string.Join(", ", r.sharedMaterials.Select(m => m ? m.name : "null")));
        }
        var all = AssetDatabase.LoadAllAssetsAtPath(Fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToArray();
        sb.Append("clips : " + string.Join(", ", all.Select(c => c.name + (c.isLooping ? " (boucle)" : "") + " " + c.length.ToString("0.00") + " s")));
        return sb.ToString();
    }
}
