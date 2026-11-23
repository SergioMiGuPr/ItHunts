# -*- coding: utf-8 -*-
"""Rendus de contrôle des props du lot 4 (nuit 4, deux passes) : FBX réimporté (on vérifie donc l'export), vue 3/4,
silhouette humaine de 1.8 m à côté pour l'échelle, éclairage sombre et chaud (une lumière chaude principale, un appoint
chaud très faible, un contre-jour discret). Rendu du LOD0 ; une seconde image montre le LOD1 seul. Les coffres sont
rendus couvercle ouvert à 70° (contrôle du pivot de la charnière et du fond creusé). Un fichier texte donne, pour
chaque prop, les cotes mesurées sur le FBX réimporté.

    blender -b --factory-startup -P rendu_props.py -- <dossier_sortie> [Nom ...]
"""
import json, math, os, sys
import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Models", "Props")
NOMS = ["Tonneau", "Chaise", "Poutre", "Sarcophage", "Ratelier", "Coffre", "CoffreVerrou", "Statue",
        "Lanterne", "Etagere", "CoffreDeco", "Table", "Banc", "Paille", "Armoire", "Lit", "Chaudron"]


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def emat(name, rgb, rough=0.9):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = rough
    return m


def silhouette(x, y=0.0):
    """Silhouette humaine de 1.80 m (pieds, jambes, bassin, torse, bras, cou, tête), de face, posée au sol."""
    m = emat("Silhouette", (0.16, 0.16, 0.18))
    parts = []

    def cyl(r, z0, z1, dx=0.0, sx=1.0, sy=1.0):
        bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=r, depth=z1 - z0, location=(x + dx, y, (z0 + z1) / 2))
        o = bpy.context.object
        o.scale = (sx, sy, 1)
        parts.append(o)

    def boite(sx, sy, sz, z, dx=0.0):
        bpy.ops.mesh.primitive_cube_add(size=1, location=(x + dx, y, z))
        o = bpy.context.object
        o.scale = (sx, sy, sz)
        parts.append(o)

    for s in (-1, 1):
        boite(0.11, 0.26, 0.08, 0.04, s * 0.1)              # pieds
        cyl(0.075, 0.08, 0.50, s * 0.1)                       # jambes (bas)
        cyl(0.085, 0.50, 0.95, s * 0.1)                       # cuisses
        cyl(0.048, 0.82, 1.12, s * 0.245)                     # avant-bras
        cyl(0.055, 1.12, 1.44, s * 0.235)                     # bras
    boite(0.36, 0.2, 0.2, 0.98)                               # bassin
    cyl(0.19, 1.02, 1.46, 0, 1.0, 0.62)                       # torse
    boite(0.5, 0.2, 0.08, 1.44)                               # épaules
    cyl(0.05, 1.46, 1.58)                                     # cou
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.105, location=(x, y, 1.68))
    t = bpy.context.object
    t.scale = (0.95, 1.0, 1.14)                               # sommet du crâne à 1.80 m
    parts.append(t)
    for o in parts:
        o.data.materials.append(m)
    return parts


def importer(nom):
    avant = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=os.path.join(FBX, "SM_Prop_%s.fbx" % nom))
    scn = bpy.context.scene
    lid = next((o for o in scn.objects if o.name.startswith("Couvercle") and o.type == "EMPTY" and o not in avant), None)
    if lid:
        lid.rotation_euler.x += math.radians(-70)   # ouverture vers l'arrière autour de la charnière
    bpy.context.view_layer.update()
    return [o for o in scn.objects if o.type == "MESH" and o not in avant]


def bornes(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def scene_nuit():
    scn = bpy.context.scene
    try:
        scn.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scn.render.engine = "BLENDER_EEVEE"
    try:
        scn.view_settings.view_transform = "AgX"
    except TypeError:
        pass
    w = bpy.data.worlds.new("Nuit"); w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.012, 0.009, 0.008, 1)
    bg.inputs[1].default_value = 1.0
    scn.world = w
    sol = emat("Sol", (0.03, 0.025, 0.021))
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    bpy.context.object.data.materials.append(sol)
    return scn


def lampe(scn, nom_l, typ, loc, rgb, energie, cible, taille=1.0):
    l = bpy.data.lights.new(nom_l, typ)
    l.color = rgb
    l.energy = energie
    if typ == "AREA":
        l.size = taille
    o = bpy.data.objects.new(nom_l, l)
    scn.collection.objects.link(o)
    o.location = loc
    o.rotation_euler = (Vector(cible) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()


def camera(scn, mn, mx, xm, res, hmin=1.85):
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scn.collection.objects.link(cam)
    scn.camera = cam
    cam.data.type = "ORTHO"
    dims = mx - mn
    xmax = max(mx.x, xm + 0.3)
    centre = Vector(((mn.x + xmax) / 2, (mn.y + mx.y) / 2, max(dims.z, hmin) / 2))
    larg = xmax - mn.x + 0.2 + dims.y * 0.5
    haut = max(dims.z, hmin) + dims.y * 0.35
    cam.data.ortho_scale = max(larg * 1.2, haut * 1.25 * res[0] / res[1])
    d = Vector((-0.55, -1.0, 0.5)).normalized()
    cam.location = centre + d * 30
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.clip_end = 100


def eclairer(scn, c):
    lampe(scn, "Chaude", "AREA", (c.x - 2.4, c.y - 2.8, 2.6), (1.0, 0.6, 0.3), 320, c, 1.2)
    lampe(scn, "Appoint", "AREA", (c.x + 0.5, c.y - 4.5, 3.5), (1.0, 0.82, 0.62), 20, c, 4)
    lampe(scn, "ContreJour", "AREA", (c.x + 3.0, c.y + 3.2, 2.8), (0.9, 0.55, 0.35), 160, c, 1.5)


def rendu(nom, out_dir, mesures):
    # LOD0 avec la silhouette
    reset()
    scn = scene_nuit()
    scn.render.resolution_x, scn.render.resolution_y = 1200, 800
    objs = importer(nom)
    lod1 = [o for o in objs if o.name.endswith("_LOD1")]
    lod0 = [o for o in objs if o not in lod1]
    for o in lod1:
        o.hide_render = True
    mn, mx = bornes(lod0)
    xm = mx.x + 0.55
    silhouette(xm, mn.y + 0.25)                 # devant à droite : jamais cachée par un meuble profond
    c = Vector(((mn.x + xm) / 2, (mn.y + mx.y) / 2, 0.8))
    eclairer(scn, c)
    camera(scn, mn, mx, xm, (1200, 800))
    p = os.path.join(out_dir, "Prop_%s.png" % nom)
    scn.render.filepath = p
    bpy.ops.render.render(write_still=True)
    # mesures sur le FBX réimporté (couvercle fermé)
    reset()
    objs = importer(nom)
    lid = next((o for o in bpy.context.scene.objects if o.name.startswith("Couvercle") and o.type == "EMPTY"), None)
    if lid:
        lid.rotation_euler.x -= math.radians(-70)
        bpy.context.view_layer.update()
    l0 = [o for o in objs if not o.name.endswith("_LOD1")]
    l1 = [o for o in objs if o.name.endswith("_LOD1")]
    a, b = bornes(l0)
    tri = lambda os_: sum(len(pg.vertices) - 2 for o in os_ for pg in o.data.polygons)
    mesures[nom] = dict(dimensions=[round(v, 3) for v in (b - a)], bas=round(a.z, 4),
                        centre=[round((a.x + b.x) / 2, 3), round((a.y + b.y) / 2, 3)],
                        triangles_lod0=tri(l0), triangles_lod1=tri(l1),
                        objets=sorted(o.name for o in bpy.context.scene.objects))
    # LOD1 seul (vignette)
    if l1:
        reset()
        scn = scene_nuit()
        scn.render.resolution_x, scn.render.resolution_y = 600, 400
        objs = importer(nom)
        for o in objs:
            if not o.name.endswith("_LOD1"):
                o.hide_render = True
        mn, mx = bornes([o for o in objs if o.name.endswith("_LOD1")])
        c = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, 0.8))
        eclairer(scn, c)
        camera(scn, mn, mx, mx.x - 0.3, (600, 400), hmin=0.0)
        scn.render.filepath = os.path.join(out_dir, "Prop_%s_LOD1.png" % nom)
        bpy.ops.render.render(write_still=True)
    print("RENDU", nom, mesures[nom])


def main():
    a = sys.argv[sys.argv.index("--") + 1:]
    out_dir = os.path.abspath(a[0])
    os.makedirs(out_dir, exist_ok=True)
    mesures = {}
    fm = os.path.join(out_dir, "mesures_fbx.json")
    if os.path.exists(fm):
        mesures = json.load(open(fm, encoding="utf-8"))
    for n in (a[1:] or NOMS):
        rendu(n, out_dir, mesures)
    json.dump(mesures, open(fm, "w", encoding="utf-8"), ensure_ascii=False, indent=1)


main()
