// Nuit 5, lot 3 : lueur des coffres interactifs (P_Prop_Coffre, P_Prop_CoffreVerrou) : lumière dorée qui respire (LueurPulsee),
// posée devant et au-dessus du coffre, qui baigne le sol autour de lui ; émission du laiton un peu renforcée. Idempotent.
// `unity cmd ... script run --file CoffresLueur5.cs --entry CoffresLueur5.Applique`
using ItHunts.Art.VFX;
using UnityEditor;
using UnityEngine;

public static class CoffresLueur5
{
    public static string Applique()
    {
        string r = "";
        foreach (var (nom, mat, em) in new[] { ("P_Prop_Coffre", "M_Prop_Coffre", 1.45f), ("P_Prop_CoffreVerrou", "M_Prop_CoffreVerrou", 1.2f) })
        {
            string p = $"Assets/Art/Prefabs/Props/{nom}.prefab";
            var root = PrefabUtility.LoadPrefabContents(p);
            try
            {
                var b = new Bounds(root.transform.position, Vector3.zero);
                foreach (var mr in root.GetComponentsInChildren<MeshRenderer>()) b.Encapsulate(mr.bounds);
                var t = root.transform.Find("Lueur_Coffre");
                if (t == null) { t = new GameObject("Lueur_Coffre").transform; t.SetParent(root.transform, false); }
                t.localPosition = new Vector3(0f, b.size.y + 0.35f, b.extents.z + 0.45f);
                var l = t.GetComponent<Light>(); if (l == null) l = t.gameObject.AddComponent<Light>();
                l.type = LightType.Point; l.color = new Color(1f, 0.66f, 0.3f); l.range = 3.6f; l.intensity = 1.4f; l.shadows = LightShadows.None;
                l.renderMode = LightRenderMode.ForcePixel;
                var lp = t.GetComponent<LueurPulsee>(); if (lp == null) lp = t.gameObject.AddComponent<LueurPulsee>();
                lp.intensite = 1.4f; lp.amplitude = 0.22f; lp.frequence = 0.55f;
                PrefabUtility.SaveAsPrefabAsset(root, p);
                r += $"{nom} hauteur {b.size.y:F2} ; ";
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var m = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Art/Materials/Props/{mat}.mat");
            if (m != null) { m.SetColor("_EmissionColor", new Color(1f, 0.58f, 0.18f) * em); m.SetFloat("_RimIntensity", 0.85f); EditorUtility.SetDirty(m); }
        }
        AssetDatabase.SaveAssets();
        return r;
    }
}
