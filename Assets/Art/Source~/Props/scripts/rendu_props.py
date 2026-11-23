# -*- coding: utf-8 -*-
"""Rendus de contrôle des props du lot 4 (nuit 4) : FBX réimporté (on vérifie donc l'export), vue 3/4, mannequin de
1.8 m à côté pour l'échelle, éclairage sombre avec une lumière chaude et une froide. Les coffres sont rendus couvercle
ouvert à 70° (contrôle du pivot de la charnière).

    blender -b --factory-startup -P rendu_props.py -- <dossier_sortie> [Nom ...]
"""
import math, os, sys
import bpy
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
FBX = os.path.join(ROOT, "Assets", "Art", "Models", "Props")
NOMS = ["Tonneau", "Chaise", "Poutre", "Sarcophage", "Ratelier", "Coffre", "CoffreVerrou", "Statue"]


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def emat(name, rgb):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = next(n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (*rgb, 1)
    b.inputs["Roughness"].default_value = 0.9
    return m


def mannequin(x):
    """Capsule de 1.8 m (tête comprise) : corps Ø 0.45, tête Ø 0.24."""
    m = emat("Mannequin", (0.18, 0.18, 0.2))
    bpy.ops.mesh.primitive_cylinder_add(radius=0.2, depth=1.3, location=(x, 0, 0.2 + 0.65))
    c = bpy.context.object
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.2, location=(x, 0, 0.2))
    b = bpy.context.object
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.2, location=(x, 0, 1.5))
    h = bpy.context.object
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.12, location=(x, 0, 1.68))
    t = bpy.context.object
    for o in (c, b, h, t):
        o.data.materials.append(m)
    return [c, b, h, t]


def rendu(nom, out_dir):
    reset()
    scn = bpy.context.scene
    try:
        scn.render.engine = "BLENDER_EEVEE_NEXT"
    except TypeError:
        scn.render.engine = "BLENDER_EEVEE"
    scn.render.resolution_x, scn.render.resolution_y = 960, 540
    try:
        scn.view_settings.view_transform = "AgX"
    except TypeError:
        pass
    w = bpy.data.worlds.new("Nuit"); w.use_nodes = True
    bg = w.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.012, 0.012, 0.018, 1)
    bg.inputs[1].default_value = 1.0
    scn.world = w

    bpy.ops.import_scene.fbx(filepath=os.path.join(FBX, "SM_Prop_%s.fbx" % nom))
    objs = [o for o in scn.objects if o.type == "MESH"]
    lid = next((o for o in objs if o.name.startswith("Couvercle")), None)
    if lid:
        lid.rotation_euler.x += math.radians(-70)   # ouverture vers l'arrière autour de la charnière
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    dims = mx - mn
    xm = mx.x + 0.45
    man = mannequin(xm)

    sol = emat("Sol", (0.05, 0.045, 0.04))
    bpy.ops.mesh.primitive_plane_add(size=60, location=(0, 0, 0))
    bpy.context.object.data.materials.append(sol)

    # lumières : chaude devant à gauche, froide derrière à droite, appoint très faible
    def lampe(nom_l, typ, loc, rgb, energie, taille=1.0):
        l = bpy.data.lights.new(nom_l, typ)
        l.color = rgb
        l.energy = energie
        if typ == "AREA":
            l.size = taille
        o = bpy.data.objects.new(nom_l, l)
        scn.collection.objects.link(o)
        o.location = loc
        d = Vector((xm / 2, 0, 0.8)) - Vector(loc)
        o.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    lampe("Chaude", "AREA", (-2.2, -2.6, 2.4), (1.0, 0.62, 0.32), 260, 1.2)
    lampe("Froide", "AREA", (3.2, 2.8, 2.6), (0.45, 0.6, 1.0), 220, 1.5)
    lampe("Appoint", "AREA", (0.5, -4, 3.5), (0.8, 0.8, 0.9), 40, 4)

    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    scn.collection.objects.link(cam)
    scn.camera = cam
    cam.data.type = "ORTHO"
    centre = Vector(((mn.x + xm + 0.25) / 2, 0, max(dims.z, 1.8) / 2))
    larg = (xm + 0.3) - (mn.x - 0.1)
    haut = max(dims.z, 1.85)
    cam.data.ortho_scale = max(larg * 1.25, haut * 1.25 * 16 / 9)
    d = Vector((-0.55, -1.0, 0.5)).normalized()
    cam.location = centre + d * 20
    cam.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.data.clip_end = 100
    p = os.path.join(out_dir, "Prop_%s.png" % nom)
    scn.render.filepath = p
    bpy.ops.render.render(write_still=True)
    print("RENDU", nom, [round(x, 3) for x in dims], p)


def main():
    a = sys.argv[sys.argv.index("--") + 1:]
    out_dir = os.path.abspath(a[0])
    os.makedirs(out_dir, exist_ok=True)
    for n in (a[1:] or NOMS):
        rendu(n, out_dir)


main()
