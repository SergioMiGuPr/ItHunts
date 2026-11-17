// Import des sprites de la carte ouverte (Tab) : comme le HUD (PPU 200, Full Rect, sans mipmaps), bordures des 9 tranches
// pour le cadre et la légende, répétition pour le fond et la brume (Image Tiled ou RawImage à UV répétées).
using System.Linq; using UnityEditor; using UnityEngine;
public static class ImportCarte
{
    public static string Run()
    {
        if (!Application.dataPath.Replace('\\', '/').EndsWith("ItHunts-art/Assets")) return "MAUVAIS PROJET";
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var borders = new System.Collections.Generic.Dictionary<string, Vector4> {
            { "Carte_Cadre", new Vector4(260, 260, 260, 260) }, { "Carte_Legende_Cadre", new Vector4(64, 64, 64, 64) } };
        var repeat = new[] { "Carte_Fond", "Carte_Brume" };
        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/UI/Carte" }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        foreach (var p in paths)
        {
            string n = System.IO.Path.GetFileNameWithoutExtension(p);
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            ti.textureType = TextureImporterType.Sprite; ti.spriteImportMode = SpriteImportMode.Single; ti.spritePixelsPerUnit = 200;
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s); s.spriteMeshType = SpriteMeshType.FullRect; ti.SetTextureSettings(s);
            ti.mipmapEnabled = false; ti.filterMode = FilterMode.Bilinear; ti.alphaIsTransparency = true; ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.wrapMode = repeat.Contains(n) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.spriteBorder = borders.ContainsKey(n) ? borders[n] : Vector4.zero;
            ti.SaveAndReimport();
        }
        return paths.Length + " sprites : " + string.Join(", ", paths.Select(System.IO.Path.GetFileNameWithoutExtension));
    }
}
