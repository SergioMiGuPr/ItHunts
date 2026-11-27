// ItHunts (DA), nuit 7, lot 2 : sous-bois du jardin refaits en cartes peintes, rochers recalés au sol.
// Lancé par la CLI Unity (unity command script run --file SousBoisBuilder.cs --entry SousBoisBuilder.Run), pas compilé dans
// le projet (dossier Source~). Après l'export Blender (kit_jardin.py -- SM_Jardin_SousBois_A SM_Jardin_SousBois_B
// SM_Jardin_Rocher_*) :
//   - M_Kit_SousBois : copie de M_Ens_Feuillage (atlas T_Ens_Feuillage_BaseColor, découpe alpha) avec culling des faces
//     arrière (les cartes ont deux faces dos à dos, normales forcées vers le ciel : jamais vues de dos, jamais noires) ;
//   - remappage des matériaux des deux FBX (M_Kit_SousBois, M_Bib_RocheCave_Peint pour les pierres du corps) ;
//   - prefabs Jardin_SousBois_A/B : matériaux du corps (écorce, roche) et du feuillage, collision du corps (branche ou
//     souche, pierres), feuillage sans collision ; prefabs des rochers : collision vérifiée.
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class SousBoisBuilder
{
    const string KIT = "Assets/Art/Models/KitJardin/";
    const string PRE = "Assets/Art/Prefabs/KitJardin/";
    const string MAT = "Assets/Art/Materials/Kits/M_Kit_SousBois.mat";

    static Material Charge(string nom)
    {
        foreach (string g in AssetDatabase.FindAssets(nom + " t:Material"))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (System.IO.Path.GetFileNameWithoutExtension(p) == nom) return AssetDatabase.LoadAssetAtPath<Material>(p);
        }
        return null;
    }

    public static string Run()
    {
        var sb = new StringBuilder();
        // 1. matériau du sous-bois
        var src = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/Ensembles/M_Ens_Feuillage.mat");
        var m = AssetDatabase.LoadAssetAtPath<Material>(MAT);
        if (m == null) { m = new Material(src); AssetDatabase.CreateAsset(m, MAT); }
        else m.CopyPropertiesFromMaterial(src);
        m.shader = src.shader;
        m.renderQueue = src.renderQueue;   // AlphaTest (2450), comme M_Ens_Feuillage
        m.SetFloat("_Cull", 2f);   // faces arrière masquées
        // variation de teinte carte par carte (canal B des sommets) : mode décor sans saleté, couche ni humidité
        m.SetFloat("_Decor", 1f); m.EnableKeyword("_DECOR"); m.SetFloat("_VertexPaint", 1f);
        m.SetFloat("_MacroVariation", 0.55f); m.SetFloat("_MacroScale", 3f); m.SetColor("_MacroTint", new Color(0.78f, 0.86f, 0.55f, 1f));
        m.SetFloat("_GrimeStrength", 0f); m.SetFloat("_WetStrength", 0f);
        m.SetFloat("_LayerAmount", 0f); m.SetFloat("_LayerLow", 0f); m.SetFloat("_LayerTop", 0f);
        EditorUtility.SetDirty(m);
        AssetDatabase.SaveAssets();
        sb.AppendLine("matériau " + MAT + " cull=" + m.GetFloat("_Cull") + " clip=" + m.GetFloat("_AlphaClip"));
        var roche = Charge("M_Bib_RocheCave_Peint");
        var ecorce = Charge("M_Bib_Ecorce");
        // 2. remappage des FBX
        // remappages ajoutés (changement d'import délibéré, listé dans INTEGRATION-PROPS) : M_Kit_SousBois partout,
        // M_Bib_RocheCave_Peint seulement pour les pierres des sous-bois
        foreach (string n in new[] { "SM_Jardin_SousBois_A", "SM_Jardin_SousBois_B", "SM_Jardin_Lisiere_12m", "SM_Jardin_Lisiere_Coin",
                                     "SM_Jardin_GrandArbre_Praticable", "SM_Jardin_Foret_Lisiere_20m" })
        {
            var mi = (ModelImporter)AssetImporter.GetAtPath(KIT + n + ".fbx");
            mi.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_Kit_SousBois"), m);
            var idRoche = new AssetImporter.SourceAssetIdentifier(typeof(Material), "M_Bib_RocheCave_Peint");
            if (n.Contains("SousBois")) mi.AddRemap(idRoche, roche);
            else mi.RemoveRemap(idRoche);   // entrée inutile du premier passage (lisières)
            mi.SaveAndReimport();
        }
        foreach (string n in new[] { "SM_Jardin_Rocher_Petit", "SM_Jardin_Rocher_Moyen", "SM_Jardin_Rocher_Grand" })
            AssetDatabase.ImportAsset(KIT + n + ".fbx", ImportAssetOptions.ForceUpdate);
        // 3. prefabs
        foreach (string n in new[] { "Jardin_SousBois_A", "Jardin_SousBois_B", "Jardin_Rocher_Petit", "Jardin_Rocher_Moyen", "Jardin_Rocher_Grand",
                                     "Jardin_Lisiere_12m", "Jardin_Lisiere_Coin", "Jardin_GrandArbre_Praticable", "Jardin_Foret_Lisiere_20m" })
        {
            string path = PRE + n + ".prefab";
            var modele = AssetDatabase.LoadAssetAtPath<GameObject>(KIT + "SM_" + n + ".fbx");
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // enfants du modèle absents du prefab (Deco_Feuillage des lisières, nuit 7) : ajoutés avec leur maillage
                foreach (var mm in modele.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mm.transform == modele.transform || root.transform.Find(mm.name) != null) continue;
                    var go = new GameObject(mm.name);
                    go.layer = root.layer;
                    go.transform.SetParent(root.transform, false);
                    go.transform.localPosition = mm.transform.localPosition; go.transform.localRotation = mm.transform.localRotation;
                    go.transform.localScale = mm.transform.localScale;
                    go.AddComponent<MeshFilter>().sharedMesh = mm.sharedMesh;
                    go.AddComponent<MeshRenderer>();
                    GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(root));   // statique comme le reste
                    sb.AppendLine(n + " : enfant ajouté " + mm.name);
                }
                foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
                {
                    var r = mf.GetComponent<MeshRenderer>();
                    bool deco = mf.name.StartsWith("Deco_");
                    if (mf.name.StartsWith("Col_")) { sb.AppendLine($"{n}/{mf.name} : collision seule"); continue; }
                    // matériaux : ceux du modèle importé (remappés), même ordre que ses sous-maillages
                    var mr = mf.transform == root.transform ? modele.GetComponent<MeshRenderer>()
                           : modele.transform.Find(mf.name)?.GetComponent<MeshRenderer>();
                    if (r != null && mr != null && (n.Contains("SousBois") || n.Contains("Lisiere") || n.Contains("GrandArbre"))) r.sharedMaterials = mr.sharedMaterials;
                    // feuillage des lisières (bord de carte, répété) : pas d'ombre portée (coût) ; sous-bois et grand arbre : ombres gardées
                    if (deco) GameObjectUtility.SetStaticEditorFlags(mf.gameObject, GameObjectUtility.GetStaticEditorFlags(root));
                    if (deco && r != null) r.shadowCastingMode = n.Contains("Lisiere")
                        ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    var mc = mf.GetComponent<MeshCollider>();
                    var bc = mf.GetComponent<BoxCollider>();
                    var b = mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds();
                    // petit rocher : boîte de KitBuilder (on l'enjambe), recalée sur le maillage recalé ; pas de MeshCollider en plus
                    if (bc != null) { bc.center = b.center; bc.size = b.size; if (mc != null) { Object.DestroyImmediate(mc); mc = null; } }
                    if (deco && mc != null) { Object.DestroyImmediate(mc); mc = null; }
                    bool nonTouche = n.Contains("Lisiere") || n.Contains("GrandArbre");   // collisions déjà faites par KitBuilder (Col_*, tronc)
                    if (!deco && mc == null && bc == null && !nonTouche) mc = mf.gameObject.AddComponent<MeshCollider>();
                    if (mc != null) mc.sharedMesh = mf.sharedMesh;
                    sb.AppendLine($"{n}/{mf.name} : maillage {(mf.sharedMesh ? mf.sharedMesh.name : "MANQUANT")} {mf.sharedMesh?.triangles.Length / 3} tris, " +
                                  $"sous-maillages {mf.sharedMesh?.subMeshCount}, matériaux {string.Join(",", r.sharedMaterials.Select(x => x ? x.name : "null"))}, " +
                                  $"collision {(mc != null ? "maillage" : bc != null ? $"boîte {bc.center.y - bc.size.y / 2:0.00}..{bc.center.y + bc.size.y / 2:0.00}" : "non")}, bas y {b.min.y:0.00}, haut {b.max.y:0.00}");
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        return sb.ToString();
    }
}
