using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// Réglages d'import des sprites du HUD (lot A) et atlas.
public static class ImportUI
{
    const int PPU = 200; // sprites en double définition : taille native = pixels / 2 sur un canvas de référence 1080p (Reference PPU 100)

    // bordures (gauche, bas, droite, haut) en pixels du sprite
    static readonly Dictionary<string, Vector4> Bords = new Dictionary<string, Vector4>
    {
        { "BarreVie_Cadre", new Vector4(245, 0, 106, 0) },
        { "BarreVie_Fond", new Vector4(245, 0, 106, 0) },
        { "BarreVie_Remplissage", new Vector4(23, 0, 28, 0) },
        { "BarreVie_Remplissage_Retard", new Vector4(23, 0, 28, 0) },
        { "Ames_Cadre", new Vector4(190, 0, 93, 0) },
        { "Ames_Fond", new Vector4(190, 0, 93, 0) },
    };

    public static string Run()
    {
        if (!Application.dataPath.Replace("\\", "/").EndsWith("ItHunts-art/Assets")) return "MAUVAIS PROJET";
        var sb = new StringBuilder();
        AssetDatabase.Refresh();
        var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/UI/HUD", "Assets/Art/UI/MiniCarte" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith(".png")).OrderBy(p => p).ToList();
        foreach (var p in paths)
        {
            var ti = (TextureImporter)AssetImporter.GetAtPath(p);
            string n = Path.GetFileNameWithoutExtension(p);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = PPU;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.sRGBTexture = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.npotScale = TextureImporterNPOTScale.None;
            var s = new TextureImporterSettings(); ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;   // obligatoire pour Sliced / Tiled
            s.spriteAlignment = (int)SpriteAlignment.Center;
            ti.SetTextureSettings(s);
            ti.spriteBorder = Bords.TryGetValue(n, out var b) ? b : Vector4.zero;
            ti.SaveAndReimport();
            var sp = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            sb.AppendFormat("{0} : {1}x{2} px, natif 1080p {3}x{4}, bordures {5}\n", n, sp.texture.width, sp.texture.height,
                sp.rect.width / 2, sp.rect.height / 2, sp.border);
        }

        // Atlas (V1 ; le projet a le Sprite Packer désactivé : réglage à activer sur main)
        const string atlasPath = "Assets/Art/UI/Atlas_HUD.spriteatlas";
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
        if (atlas == null) { atlas = new SpriteAtlas(); AssetDatabase.CreateAsset(atlas, atlasPath); }
        atlas.SetPackingSettings(new SpriteAtlasPackingSettings { enableRotation = false, enableTightPacking = false, padding = 4, blockOffset = 1, enableAlphaDilation = true });
        atlas.SetTextureSettings(new SpriteAtlasTextureSettings { readable = false, generateMipMaps = false, sRGB = true, filterMode = FilterMode.Bilinear });
        var ps = atlas.GetPlatformSettings("DefaultTexturePlatform");
        ps.maxTextureSize = 4096; ps.textureCompression = TextureImporterCompression.CompressedHQ; atlas.SetPlatformSettings(ps);
        atlas.Remove(atlas.GetPackables());
        atlas.Add(new Object[] { AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/UI/HUD"), AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Art/UI/MiniCarte") });
        EditorUtility.SetDirty(atlas);
        AssetDatabase.SaveAssets();
        SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        sb.AppendFormat("atlas : {0} sprites, mode packer {1}\n", atlas.spriteCount, EditorSettings.spritePackerMode);
        return sb.ToString();
    }
}
