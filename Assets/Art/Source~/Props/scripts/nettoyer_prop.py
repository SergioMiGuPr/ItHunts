# -*- coding: utf-8 -*-
"""Nettoyage d'un prop généré (Higgsfield → Tripo H3.1, GLB texturé) pour Unity (nuit 4, lot 4).

Usage (Blender en ligne de commande) :
    blender -b --factory-startup -P nettoyer_prop.py -- <Nom>            # un prop de la table PROPS
    blender -b --factory-startup -P nettoyer_prop.py -- <Nom> --inspecter # vues orthographiques du GLB brut

Étapes : import du GLB (docs/art/props/sources/tripo_<nom>_v01.glb) → un seul maillage → soudure des sommets
→ suppression des faces dégénérées et des parties flottantes (îlots minuscules) → rotation pour mettre l'avant vers -Y
→ mise à l'échelle sur les cotes réelles (1 unité = 1 m) → pivot au sol, au centre de l'emprise → décimation si
le budget est dépassé → (coffres) découpe du couvercle, objet « Couvercle » dont l'origine est sur l'axe de la
charnière arrière, ouvertures refermées par une face « Interieur » sombre → texture de base en PNG
(Assets/Art/Textures/Props/T_Prop_<Nom>_BaseColor.png) → FBX (réglages de kitlib.export : Unity +Y en haut, +Z en
avant) dans Assets/Art/Models/Props/SM_Prop_<Nom>.fbx. Un rapport JSON est écrit à côté du GLB.

Conventions : Blender Z en haut, avant vers -Y, charnière des coffres côté +Y.
"""
import json, math, os, sys
import bpy, bmesh
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Kits", "scripts"))
import kitlib  # noqa: E402  (réglages d'export FBX communs)

ROOT = kitlib.ROOT
SRC = os.path.join(ROOT, "docs", "art", "props", "sources")
OUT_FBX = os.path.join(ROOT, "Assets", "Art", "Models", "Props")
OUT_TEX = os.path.join(ROOT, "Assets", "Art", "Textures", "Props")

# Nom : (glb, cotes X × Y × Z en m [largeur, profondeur, hauteur], rotation Z en degrés pour amener l'avant vers -Y,
#        budget de triangles, taille de texture, hauteur relative de la découpe du couvercle ou None)
# rot_z et couvercle sont relevés sur les vues --inspecter.
PROPS = {
    "Tonneau":      dict(glb="tripo_tonneau_v01.glb",      dims=(0.6, 0.6, 0.9),  rot_z=0,  budget=4000, tex=1024),
    "Chaise":       dict(glb="tripo_chaise_v01.glb",       dims=(0.45, 0.45, 1.0), rot_z=0, budget=4000, tex=1024),
    "Poutre":       dict(glb="tripo_poutre_v01.glb",       dims=(4.0, 0.3, 0.3),  rot_z=0,  budget=3000, tex=1024),
    "Sarcophage":   dict(glb="tripo_sarcophage_v01.glb",   dims=(2.2, 0.9, 0.8),  rot_z=0,  budget=7000, tex=2048),
    "Ratelier":     dict(glb="tripo_ratelier_v01.glb",     dims=(1.6, 0.5, 1.8),  rot_z=0,  budget=6000, tex=2048),
    "Coffre":       dict(glb="tripo_coffre_v01.glb",       dims=(0.9, 0.55, 0.6), rot_z=0,  budget=5000, tex=1024,
                         couvercle=0.55),
    "CoffreVerrou": dict(glb="tripo_coffreverrou_v01.glb", dims=(0.9, 0.55, 0.6), rot_z=0,  budget=7000, tex=2048,
                         couvercle=0.55),
    "Statue":       dict(glb="tripo_statue_v01.glb",       dims=(1.0, 1.0, 2.6),  rot_z=0,  budget=7000, tex=2048),
}
INSPECT = os.environ.get("PROPS_INSPECT", os.path.join(SRC, "inspect"))  # vues de contrôle (hors dépôt de préférence)
OVERRIDES = os.path.join(SRC, "reglages_props.json")  # rot_z / couvercle / dims relevés à l'inspection (prioritaires)


def args():
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    return a[0], "--inspecter" in a


def importer(path):
    kitlib.reset()
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    # détacher des parents (nœuds vides du glTF) en gardant la transformation, puis l'appliquer
    mw = ob.matrix_world.copy()
    ob.parent = None
    ob.matrix_world = mw
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for o in list(bpy.context.scene.objects):
        if o is not ob:
            bpy.data.objects.remove(o, do_unlink=True)
    ob.name = "Prop"
    return ob


def bbox(ob):
    vs = [v.co for v in ob.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    return mn, mx


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def nettoyer(ob, rapport):
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    n0 = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    rapport["sommets_soudes"] = n0 - len(bm.verts)
    # faces dégénérées (aire nulle) et arêtes de longueur nulle
    mn, mx = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for v in bm.verts:
        mn = Vector(map(min, mn, v.co)); mx = Vector(map(max, mx, v.co))
    diag = (mx - mn).length
    bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=diag * 1e-6)
    deg = [f for f in bm.faces if f.calc_area() < (diag * 1e-6) ** 2]
    bmesh.ops.delete(bm, geom=deg, context="FACES")
    rapport["faces_degenerees"] = len(deg)
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    # îlots : on supprime les parties flottantes minuscules (débris de reconstruction)
    bm.verts.ensure_lookup_table()
    seen, islands = set(), []
    for v in bm.verts:
        if v.index in seen:
            continue
        stack, isl = [v], []
        seen.add(v.index)
        while stack:
            c = stack.pop()
            isl.append(c)
            for e in c.link_edges:
                o = e.other_vert(c)
                if o.index not in seen:
                    seen.add(o.index)
                    stack.append(o)
        islands.append(isl)
    retires = 0
    for isl in islands:
        a = Vector((min(v.co.x for v in isl), min(v.co.y for v in isl), min(v.co.z for v in isl)))
        b = Vector((max(v.co.x for v in isl), max(v.co.y for v in isl), max(v.co.z for v in isl)))
        if (b - a).length < diag * 0.015 and len(isl) < 60:
            bmesh.ops.delete(bm, geom=isl, context="VERTS")
            retires += 1
    rapport["ilots"] = len(islands)
    rapport["ilots_flottants_retires"] = retires
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()


def aligner(ob):
    """Tourne le maillage autour de Z pour que son emprise au sol soit un rectangle d'aire minimale (Tripo pose
    parfois l'objet de biais, comme sur l'image 3/4). Renvoie l'angle appliqué en degrés (entre -45 et 45)."""
    pts = [(v.co.x, v.co.y) for v in ob.data.vertices]
    best, ang = None, 0.0
    for i in range(-90, 91):
        a = math.radians(i * 0.5)
        c, s = math.cos(a), math.sin(a)
        xs = [x * c - y * s for x, y in pts]
        ys = [x * s + y * c for x, y in pts]
        aire = (max(xs) - min(xs)) * (max(ys) - min(ys))
        if best is None or aire < best:
            best, ang = aire, i * 0.5
    ob.data.transform(Matrix.Rotation(math.radians(ang), 4, "Z"))
    ob.data.update()
    return ang


def placer(ob, rot_z, dims, uniforme=False):
    """uniforme : une seule échelle, réglée sur la hauteur (statue : on ne l'élargit pas pour remplir l'emprise)."""
    ob.data.transform(Matrix.Rotation(math.radians(rot_z), 4, "Z"))
    mn, mx = bbox(ob)
    size = mx - mn
    s = Vector((dims[0] / size.x, dims[1] / size.y, dims[2] / size.z))
    if uniforme:
        s = Vector((s.z, s.z, s.z))
    c = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    ob.data.transform(Matrix.Translation(-c))
    ob.data.transform(Matrix.Diagonal((s.x, s.y, s.z, 1.0)))
    ob.data.update()
    return s


def decimer(ob, budget, rapport):
    t = tris(ob)
    rapport["triangles_tripo"] = t
    if t <= budget:
        return
    m = ob.modifiers.new("Dec", "DECIMATE")
    m.ratio = budget / t * 0.98
    m.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=m.name)


def trianguler(ob):
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.triangulate(bm, faces=bm.faces)
    bm.to_mesh(ob.data)
    bm.free()


def texture(ob, nom, taille):
    """Exporte la texture de base du GLB en PNG et remplace le matériau par M_Prop_<Nom> (une texture, sans PBR)."""
    img = None
    for m in ob.data.materials:
        if m and m.use_nodes:
            for n in m.node_tree.nodes:
                if n.type == "TEX_IMAGE" and n.image:
                    img = n.image
                    break
        if img:
            break
    os.makedirs(OUT_TEX, exist_ok=True)
    path = os.path.join(OUT_TEX, "T_Prop_%s_BaseColor.png" % nom)
    src = img.size[:] if img else None
    if img:
        img = img.copy()
        if img.size[0] != taille:
            img.scale(taille, taille)
        img.filepath_raw = path
        img.file_format = "PNG"
        img.save()
        img.filepath = path
    mat = bpy.data.materials.new("M_Prop_%s" % nom)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Roughness"].default_value = 1.0
    if img:
        tex = nt.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(path, check_existing=True)
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    ob.data.materials.clear()
    ob.data.materials.append(mat)
    return src, path


def couvercle(ob, frac, nom, rapport):
    """Coupe le maillage à la hauteur frac × H : la partie haute devient l'objet « Couvercle », origine sur la charnière
    (bord arrière +Y, au niveau de la coupe). Les deux ouvertures sont refermées par une face « Interieur »."""
    mn, mx = bbox(ob)
    zc = mn.z + (mx.z - mn.z) * frac
    interieur = bpy.data.materials.new("M_Prop_%s_Interieur" % nom)
    interieur.use_nodes = True
    next(n for n in interieur.node_tree.nodes if n.type == "BSDF_PRINCIPLED").inputs["Base Color"].default_value = (0.02, 0.015, 0.012, 1)
    ob.data.materials.append(interieur)
    idx_int = len(ob.data.materials) - 1

    bm = bmesh.new()
    bm.from_mesh(ob.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    res = bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-6, plane_co=(0, 0, zc), plane_no=(0, 0, 1))
    cut_edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
    # séparer : dupliquer les sommets de la coupe pour que haut et bas deviennent deux îlots
    bmesh.ops.split_edges(bm, edges=cut_edges)
    haut = [f for f in bm.faces if f.calc_center_median().z > zc]
    bas = [f for f in bm.faces if f.calc_center_median().z <= zc]
    bm.to_mesh(ob.data)
    bm.free()

    # séparer le haut dans un nouvel objet
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="DESELECT")
    bpy.ops.object.mode_set(mode="OBJECT")
    for p in ob.data.polygons:
        p.select = p.center.z > zc
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.separate(type="SELECTED")
    bpy.ops.object.mode_set(mode="OBJECT")
    lid = next(o for o in bpy.context.selected_objects if o is not ob)
    lid.name = "Couvercle"

    for o in (ob, lid):
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
        bnd = [e for e in bm.edges if e.is_boundary and all(abs(v.co.z - zc) < 1e-4 for v in e.verts)]
        if bnd:
            r = bmesh.ops.holes_fill(bm, edges=bnd, sides=0)
            r = bmesh.ops.triangulate(bm, faces=r["faces"])
            uv = bm.loops.layers.uv.active
            for f in r["faces"]:
                f.material_index = idx_int
                if uv:
                    for l in f.loops:
                        l[uv].uv = (0.0, 0.0)
        bm.to_mesh(o.data)
        bm.free()
    # origine du couvercle : charnière arrière (y max) au niveau de la coupe
    _, lmx = bbox(lid)
    piv = Vector((0.0, lmx.y, zc))
    lid.data.transform(Matrix.Translation(-piv))
    lid.location = piv
    lid.parent = ob
    lid.matrix_parent_inverse = Matrix.Identity(4)
    rapport["couvercle"] = dict(hauteur_coupe=round(zc, 3), charniere=[round(c, 3) for c in piv],
                                triangles=tris(lid))


def inspecter(ob, nom):
    """Vues orthographiques (dessus, face -Y, côté +X) du GLB brut pour relever rot_z et la coupe du couvercle."""
    mn, mx = bbox(ob)
    c = (mn + mx) / 2
    r = (mx - mn).length
    scn = bpy.context.scene
    try:
        scn.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scn.render.engine = "BLENDER_EEVEE"
    scn.render.resolution_x = scn.render.resolution_y = 512
    w = bpy.data.worlds.new("W"); w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.5, 0.5, 0.5, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
    scn.world = w
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scn.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = r * 1.05
    scn.camera = cam
    out = []
    for vue, d in (("dessus", Vector((0, 0, 1))), ("face", Vector((0, -1, 0))), ("cote", Vector((1, 0, 0)))):
        cam.location = c + d * r * 2
        cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
        p = os.path.join(INSPECT, "%s_%s.png" % (nom, vue))
        os.makedirs(os.path.dirname(p), exist_ok=True)
        scn.render.filepath = p
        bpy.ops.render.render(write_still=True)
        out.append(p)
    print("INSPECT", nom, "taille", [round(x, 3) for x in (mx - mn)], "tris", tris(ob), out)


def main():
    nom, insp = args()
    cfg = dict(PROPS[nom])
    if os.path.exists(OVERRIDES):
        with open(OVERRIDES, encoding="utf-8") as f:
            cfg.update(json.load(f).get(nom, {}))
    ob = importer(os.path.join(SRC, cfg["glb"]))
    ang = aligner(ob) if cfg.get("aligner", True) else 0.0
    if insp:
        print("ALIGNEMENT", ang)
        inspecter(ob, nom)
        return
    rapport = dict(nom=nom, glb=cfg["glb"], alignement_deg=ang, rot_z=cfg["rot_z"])
    nettoyer(ob, rapport)
    s = placer(ob, cfg["rot_z"], cfg["dims"], cfg.get("uniforme", False))
    rapport["echelle_appliquee"] = [round(x, 4) for x in s]
    decimer(ob, cfg["budget"] - (300 if cfg.get("couvercle") else 0), rapport)
    trianguler(ob)
    src, tex = texture(ob, nom, cfg["tex"])
    rapport["texture_tripo"] = src
    rapport["texture"] = os.path.relpath(tex, ROOT).replace("\\", "/")
    if cfg.get("couvercle"):
        couvercle(ob, cfg["couvercle"], nom, rapport)
    ob.name = "SM_Prop_%s" % nom
    ob.data.name = ob.name
    mn, mx = bbox(ob)
    objs = [ob] + list(ob.children)
    rapport["dimensions"] = [round(x, 3) for x in (mx - mn)]
    rapport["triangles"] = sum(tris(o) for o in objs)
    os.makedirs(OUT_FBX, exist_ok=True)
    fbx = os.path.join(OUT_FBX, "SM_Prop_%s.fbx" % nom)
    kitlib.export([ob], fbx)
    rapport["fbx"] = os.path.relpath(fbx, ROOT).replace("\\", "/")
    with open(os.path.join(SRC, "rapport_%s.json" % nom), "w", encoding="utf-8") as f:
        json.dump(rapport, f, ensure_ascii=False, indent=1)
    print("RAPPORT", json.dumps(rapport, ensure_ascii=False))


main()
