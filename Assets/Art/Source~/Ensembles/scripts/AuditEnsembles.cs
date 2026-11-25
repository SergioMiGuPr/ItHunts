// Nuit 6, lot 1 : contrôle des prefabs d'ensembles (hors jeu) : objets sans appui, objets enfoncés dans le sol, pièces qui
// s'interpénètrent fort, pièces solides sans collision, matériaux manquants, lumières avec ombres.
//   unity command script run --file AuditEnsembles.cs --entry AuditEnsembles.Run --args '["<rapport.txt>"]'
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AuditEnsembles
{
    static Bounds B(GameObject g)
    {
        var rs = g.GetComponentsInChildren<MeshRenderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    // point le plus bas réel (sommets), pas la boîte englobante d'un objet tourné
    static float Bas(GameObject g)
    {
        float m = float.MaxValue;
        foreach (var f in g.GetComponentsInChildren<MeshFilter>()) if (f.sharedMesh && f.GetComponent<MeshRenderer>())
            foreach (var v in f.sharedMesh.vertices) m = Mathf.Min(m, f.transform.TransformPoint(v).y);
        return m;
    }
    static readonly string[] SansCol = { "Vaisselle", "Bougies", "Chope", "Des_Cartes", "Crane", "Os_Tas", "Squelette", "Livres", "Corde", "Papiers", "Gravats", "Banquet", "Tapis", "Chemin", "Drap_Sol", "Verre", "Decal",
        "Herbes", "Fougeres", "Ronces", "Rosiers", "Champignons", "Lierre", "Habillage", "Eau_", "Nenuphars", "Corbeau", "Chaines", "Outils", "Pioches", "Fers", "Banniere", "Tenture", "Portrait", "Cadre", "Etagere_Murale",
        "Rail", "Bouclier", "Candelabre_Table", "Torche", "Applique", "Lanterne", "Paille", "SousBois", "Pupitres", "Cendres", "Buis", "Serre_Vitres", "VFX", "Candelabre", "Seau", "Brasero", "Tableau", "Caisse_Ouverte__" };
    public static string Run(string sortie)
    {
        var sb = new StringBuilder(); int appui = 0, sous = 0, inter = 0, col = 0, mat = 0, omb = 0;
        foreach (var p in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Art/Levels/Ensembles" }).Select(AssetDatabase.GUIDToAssetPath).Distinct().OrderBy(x => x))
        {
            var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(p));
            var objs = new List<GameObject>();
            foreach (var n in new[] { "Mobilier", "Details" }) { var t = g.transform.Find(n); if (t) foreach (Transform c in t) if (c.GetComponentsInChildren<MeshRenderer>().Length > 0) objs.Add(c.gameObject); }
            var bs = objs.ToDictionary(o => o, B);
            foreach (var o in objs)
            {
                if (o.name.StartsWith("Decal")) continue;
                float bas = Bas(o); var b = bs[o];
                bool mural = b.min.z < 0.35f && bas > 0.3f && g.name.StartsWith("Jardin") == false;
                if (bas > 0.07f && !mural)
                {
                    bool ok = objs.Any(q => q != o && bs[q].min.x < b.max.x && bs[q].max.x > b.min.x && bs[q].min.z < b.max.z && bs[q].max.z > b.min.z && bs[q].min.y < bas - 0.02f && bs[q].max.y > bas - 0.16f);
                    if (!ok) { appui++; sb.AppendLine($"APPUI {g.name} / {o.name} bas {bas:F2} en {o.transform.position.ToString("F2")}"); }
                }
                if (bas < -0.12f && !o.name.Contains("Arbre") && !o.name.Contains("Sapin") && !o.name.Contains("Puits") && !o.name.Contains("Fontaine") && !o.name.Contains("Tombe") && !o.name.Contains("Souche") && !o.name.Contains("Eboulis") && !o.name.Contains("Herbe") && !o.name.Contains("Ronce") && !o.name.Contains("Fougere") && !o.name.Contains("SousBois"))
                { sous++; sb.AppendLine($"SOL {g.name} / {o.name} bas {bas:F2}"); }
                if (b.size.y > 0.4f && !SansCol.Any(s => o.name.Contains(s)) && o.GetComponentsInChildren<Collider>().Length == 0) { col++; sb.AppendLine($"COLLISION {g.name} / {o.name}"); }
                foreach (var r in o.GetComponentsInChildren<MeshRenderer>()) foreach (var m in r.sharedMaterials) if (m == null || m.shader == null || m.shader.name.Contains("Error") || m.shader.name.StartsWith("Universal Render Pipeline/Lit")) { mat++; sb.AppendLine($"MATERIAU {g.name} / {o.name} {(m ? m.shader.name : "nul")}"); }
            }
            string[] mous = { "Gravats", "Papiers", "Herbes", "Fougeres", "Ronces", "Rosiers", "Lierre", "Habillage", "Tapis", "Chemin", "Decal", "Paille", "SousBois", "Eau", "Nenuphars", "Arbre", "Sapin", "Serre", "Kiosque", "Haie", "Plante", "Drap_Sol", "Etai", "Potence", "Grille", "Cellule", "MurBrise", "Eboulis", "Appentis", "Rails", "Verre", "Champignons" };
            for (int i = 0; i < objs.Count; i++) for (int j = i + 1; j < objs.Count; j++)
            {
                if (mous.Any(s => objs[i].name.Contains(s) || objs[j].name.Contains(s))) continue;
                var a = bs[objs[i]]; var b = bs[objs[j]];
                float x = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x), y = Mathf.Min(a.max.y, b.max.y) - Mathf.Max(a.min.y, b.min.y), z = Mathf.Min(a.max.z, b.max.z) - Mathf.Max(a.min.z, b.min.z);
                if (x <= 0.05f || y <= 0.05f || z <= 0.05f) continue;
                float v = x * y * z, vm = Mathf.Min(a.size.x * a.size.y * a.size.z, b.size.x * b.size.y * b.size.z);
                if (v / vm > 0.18f) { inter++; sb.AppendLine($"INTER {g.name} / {objs[i].name} et {objs[j].name} : {v / vm:P0} en {((a.center + b.center) / 2).ToString("F1")}"); }
            }
            foreach (var l in g.GetComponentsInChildren<Light>()) if (l.enabled && l.shadows != LightShadows.None) { omb++; sb.AppendLine($"OMBRE {g.name} / {l.name}"); }
            Object.DestroyImmediate(g);
        }
        sb.Insert(0, $"sans appui {appui}, sous le sol {sous}, interpénétrations {inter}, solides sans collision {col}, matériaux {mat}, lumières avec ombre {omb}\n");
        File.WriteAllText(sortie, sb.ToString());
        return sb.ToString();
    }
}
