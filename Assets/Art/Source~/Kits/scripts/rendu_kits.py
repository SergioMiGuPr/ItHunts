# -*- coding: utf-8 -*-
"""Rendus de contrôle des pièces des kits (nuit 4, lot 3) : Blender en ligne de commande, Eevee, vue 3/4 avant-gauche,
mannequin de 1.8 m à l'échelle (et gabarit du traqueur pour les pièces qui le concernent), matériaux du jeu relus dans les
.mat d'Unity (texture de base en UV monde, taille de tuile `_TileSize`, couleur), ombrage en aplats et contours encrés
(Freestyle). Les surfaces `Col_*` (collision seule) ne sont pas rendues.

    blender -b --factory-startup -P rendu_kits.py -- <dossier de sortie> <fbx> [<fbx> …]
"""
import bpy, bmesh, sys, os, re, math, glob
from mathutils import Vector, Matrix

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
ART = os.path.join(ROOT, "Assets", "Art")

# ------------------------------------------------------------------ matériaux du jeu (lus dans les .mat)
GUID = {}
for meta in glob.glob(os.path.join(ART, "**", "*.png.meta"), recursive=True):
    with open(meta, encoding="utf-8", errors="ignore") as f:
        m = re.search(r"guid: ([0-9a-f]{32})", f.read())
    if m:
        GUID[m.group(1)] = meta[:-5]
MATS = {}
for p in glob.glob(os.path.join(ART, "**", "*.mat"), recursive=True):
    MATS[os.path.splitext(os.path.basename(p))[0]] = p


def lit_mat(nom):
    p = MATS.get(nom)
    info = dict(tex=None, tile=(4.0, 4.0), col=(0.5, 0.5, 0.5), em=None)
    if not p:
        return info
    s = open(p, encoding="utf-8", errors="ignore").read()
    m = re.search(r"- _BaseMap:\s*\n\s*m_Texture: \{fileID: \d+, guid: ([0-9a-f]{32})", s)
    if m and m.group(1) in GUID:
        info["tex"] = GUID[m.group(1)]
    m = re.search(r"- _TileSize: \{r: ([\d.]+), g: ([\d.]+)", s)
    if m:
        info["tile"] = (float(m.group(1)), float(m.group(2)))
    m = re.search(r"- _BaseColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", s)
    if m:
        info["col"] = tuple(float(x) for x in m.groups())
    m = re.search(r"- _EmissionColor: \{r: ([\d.e-]+), g: ([\d.e-]+), b: ([\d.e-]+)", s)
    if m and sum(float(x) for x in m.groups()) > 0.02:
        info["em"] = tuple(float(x) for x in m.groups())
    return info


SPECIAUX = {
    "M_Eau_Toon": dict(col=(0.05, 0.13, 0.17), em=(0.01, 0.04, 0.06), tex=None),
    "M_Kit_Sceau_Ame": dict(col=(0.3, 0.9, 1.0), em=(0.4, 2.6, 3.2), tex=None),
    "M_Kit_Vitrail": dict(col=(0.05, 0.06, 0.09), em=(0.02, 0.035, 0.08), tex=None),
    "M_Kit_Vitrail_Rouge": dict(col=(0.12, 0.02, 0.03), em=(0.09, 0.01, 0.015), tex=None),
    "M_Kit_Vitrail_Ambre": dict(col=(0.13, 0.08, 0.02), em=(0.1, 0.055, 0.012), tex=None),
    "M_Kit_Feuillage": dict(col=(0.10, 0.13, 0.09), em=None, tex=None),
    "M_Kit_Cire": dict(col=(0.78, 0.72, 0.58), em=None, tex=None),
    "M_Fond_Noir": dict(col=(0.01, 0.01, 0.012), em=None, tex=None),
}


def materiau(nom):
    base = re.sub(r"\.\d{3}$", "", nom)
    info = lit_mat(base)
    if base in SPECIAUX:
        info.update(SPECIAUX[base])
        info.setdefault("tile", (4, 4))
    m = bpy.data.materials.new("R_" + base)
    m.use_nodes = True
    nt = m.node_tree
    for n in list(nt.nodes):
        nt.nodes.remove(n)
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    dif = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    nt.links.new(dif.outputs[0], s2r.inputs[0])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "CONSTANT"
    e = ramp.color_ramp.elements
    e[0].position, e[0].color = 0.0, (0.34, 0.36, 0.42, 1)
    e[1].position, e[1].color = 0.5, (1.0, 0.97, 0.92, 1)
    mid = e.new(0.12)
    mid.color = (0.62, 0.62, 0.66, 1)
    bw = nt.nodes.new("ShaderNodeRGBToBW")
    nt.links.new(s2r.outputs[0], bw.inputs[0])
    nt.links.new(bw.outputs[0], ramp.inputs[0])
    # couleur de base : texture en projection boîte (UV monde) ou couleur unie
    if info.get("tex") and os.path.exists(info["tex"]):
        geo = nt.nodes.new("ShaderNodeNewGeometry")
        mp = nt.nodes.new("ShaderNodeMapping")
        tx, ty = info["tile"]
        mp.inputs["Scale"].default_value = (1 / tx, 1 / tx, 1 / ty)
        nt.links.new(geo.outputs["Position"], mp.inputs["Vector"])
        im = nt.nodes.new("ShaderNodeTexImage")
        im.image = bpy.data.images.load(info["tex"], check_existing=True)
        im.projection = "BOX"
        im.projection_blend = 0.25
        nt.links.new(mp.outputs[0], im.inputs[0])
        col = im.outputs[0]
        tint = nt.nodes.new("ShaderNodeMix")
        tint.data_type = "RGBA"
        tint.blend_type = "MULTIPLY"
        tint.inputs[0].default_value = 1.0
        nt.links.new(col, tint.inputs[6])
        tint.inputs[7].default_value = (*info["col"], 1)
        col = tint.outputs[2]
    else:
        rgb = nt.nodes.new("ShaderNodeRGB")
        rgb.outputs[0].default_value = (*info["col"], 1)
        col = rgb.outputs[0]
    # saleté de la peinture de sommets (R) : assombrit le bas
    ca = nt.nodes.new("ShaderNodeVertexColor")
    ca.layer_name = "Col"
    sep = nt.nodes.new("ShaderNodeSeparateColor")
    nt.links.new(ca.outputs[0], sep.inputs[0])
    sal = nt.nodes.new("ShaderNodeMath")
    sal.operation = "MULTIPLY_ADD"
    nt.links.new(sep.outputs[0], sal.inputs[0])
    sal.inputs[1].default_value = -0.35 if base.endswith("_Peint") or base.startswith("M_Chateau") else 0.0
    sal.inputs[2].default_value = 1.0
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    mix.blend_type = "MULTIPLY"
    mix.inputs[0].default_value = 1.0
    nt.links.new(col, mix.inputs[6])
    nt.links.new(ramp.outputs[0], mix.inputs[7])
    mix2 = nt.nodes.new("ShaderNodeVectorMath")
    mix2.operation = "SCALE"
    nt.links.new(mix.outputs[2], mix2.inputs[0])
    nt.links.new(sal.outputs[0], mix2.inputs["Scale"])
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(mix2.outputs[0], em.inputs[0])
    if info.get("em"):
        add = nt.nodes.new("ShaderNodeVectorMath")
        add.operation = "ADD"
        nt.links.new(mix2.outputs[0], add.inputs[0])
        add.inputs[1].default_value = info["em"]
        nt.links.new(add.outputs[0], em.inputs[0])
    nt.links.new(em.outputs[0], out.inputs[0])
    return m


def plat(nom, col, alpha=1.0, em=0.0):
    m = bpy.data.materials.new(nom)
    m.use_nodes = True
    nt = m.node_tree
    b = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (*col, 1)
    b.inputs["Roughness"].default_value = 0.9
    if em:
        b.inputs["Emission Color"].default_value = (*col, 1)
        b.inputs["Emission Strength"].default_value = em
    if alpha < 1:
        b.inputs["Alpha"].default_value = alpha
        try:
            m.surface_render_method = "BLENDED"
        except Exception:
            pass
    return m


# ------------------------------------------------------------------ figurants d'échelle
def mannequin(loc, rot=0.0):
    bm = bmesh.new()
    def cyl(c, r, h, n=10):
        ret = bmesh.ops.create_cone(bm, cap_ends=True, segments=n, radius1=r, radius2=r, depth=h,
                                    matrix=Matrix.Translation(Vector(c) + Vector((0, 0, h / 2))))
    def box(c, s):
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(Vector(c)) @ Matrix.Diagonal((*s, 1)))
    for sx in (-1, 1):
        cyl((sx * 0.1, 0, 0), 0.085, 0.86)
        cyl((sx * 0.27, 0, 0.82), 0.06, 0.62)
    box((0, 0, 1.15), (0.44, 0.24, 0.62))
    cyl((0, 0, 1.45), 0.06, 0.1)
    bmesh.ops.create_uvsphere(bm, u_segments=12, v_segments=8, radius=0.125, matrix=Matrix.Translation((0, 0, 1.675)))
    me = bpy.data.meshes.new("Mannequin_1m80")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("Mannequin_1m80", me)
    ob.location = loc
    ob.rotation_euler = (0, 0, rot)
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(plat("R_Mannequin", (0.78, 0.66, 0.48)))
    return ob


def capsule(nom, loc, r, h):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=24, v_segments=12, radius=r)
    for v in bm.verts:
        if v.co.z > 0:
            v.co.z += h - 2 * r
        v.co.z += r
    me = bpy.data.meshes.new(nom)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(nom, me)
    ob.location = loc
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(plat("R_Traqueur", (0.75, 0.08, 0.06), alpha=0.28, em=0.4))
    ob.visible_shadow = False
    return ob


def cadre(nom, loc, w, h, ep=0.08):
    bm = bmesh.new()
    for c, s in (((0, 0, h - ep / 2), (w, ep, ep)), ((-w / 2 + ep / 2, 0, h / 2), (ep, ep, h)), ((w / 2 - ep / 2, 0, h / 2), (ep, ep, h))):
        bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(Vector(c)) @ Matrix.Diagonal((*s, 1)))
    me = bpy.data.meshes.new(nom)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(nom, me)
    ob.location = loc
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(plat("R_Gabarit", (0.95, 0.12, 0.08), em=2.0))
    return ob


def flamme(loc):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=6, radius1=0.03, radius2=0.0, depth=0.09, matrix=Matrix.Translation(Vector(loc) + Vector((0, 0, 0.0))))
    me = bpy.data.meshes.new("Flamme")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("Flamme", me)
    bpy.context.scene.collection.objects.link(ob)
    me.materials.append(plat("R_Flamme", (1.0, 0.6, 0.25), em=12.0))
    return ob


# ------------------------------------------------------------------ scène
def scene_rendu():
    sc = bpy.context.scene
    for eng in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
        try:
            sc.render.engine = eng
            break
        except TypeError:
            continue
    sc.render.resolution_x, sc.render.resolution_y = 960, 720
    sc.render.resolution_percentage = 100
    try:
        sc.eevee.taa_render_samples = 16
    except Exception:
        pass
    sc.render.image_settings.file_format = "JPEG"
    sc.render.image_settings.quality = 82
    sc.view_settings.view_transform = "Standard"
    sc.render.use_freestyle = True
    sc.render.line_thickness_mode = "ABSOLUTE"
    sc.render.line_thickness = 1.1
    vl = sc.view_layers[0]
    vl.use_freestyle = True
    fs = vl.freestyle_settings
    fs.crease_angle = math.radians(140)
    ls = fs.linesets[0] if fs.linesets else fs.linesets.new("Encre")
    ls.select_by_visibility = True
    ls.select_by_edge_types = True
    ls.select_silhouette = ls.select_border = ls.select_crease = True
    if ls.linestyle is None:
        ls.linestyle = bpy.data.linestyles.new("Encre")
    ls.linestyle.color = (0.03, 0.025, 0.03)
    w = bpy.data.worlds.new("Monde")
    w.use_nodes = True
    bg = next(n for n in w.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs[0].default_value = (0.11, 0.12, 0.15, 1)
    bg.inputs[1].default_value = 0.6
    sc.world = w
    sun = bpy.data.lights.new("Soleil", "SUN")
    sun.energy = 3.2
    sun.angle = math.radians(3)
    so = bpy.data.objects.new("Soleil", sun)
    so.rotation_euler = (math.radians(50), math.radians(-18), math.radians(-35))
    sc.collection.objects.link(so)


def rend(fbx, out):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    nom = os.path.splitext(os.path.basename(fbx))[0]
    sc = bpy.context.scene
    # mise à plat dans le monde (l'espace objet de la projection boîte = monde), matériaux du jeu
    cache = {}
    meshes = [o for o in sc.objects if o.type == "MESH"]
    empties = [o for o in sc.objects if o.type == "EMPTY"]
    reperes = {o.name.split(".")[0]: o.matrix_world.translation.copy() for o in empties}
    # parties mobiles entrouvertes (contrôle des pivots) : battant gauche, trappe droite, herse à mi-course, vantail levé
    vers_salle = any(x in nom for x in ("Barricade", "Voliere"))
    for o in sc.objects:
        n_ = o.name.split(".")[0]
        if o.type != "MESH":
            continue
        mw = o.matrix_world.copy()
        piv = mw.translation.copy()
        R = None
        if n_ == "Battant_G":
            R = Matrix.Rotation(math.radians(-65 if vers_salle else 65), 4, "Z")
        elif n_ == "Trappe_D":
            R = Matrix.Rotation(math.radians(110), 4, "Y")
        elif n_ == "Serrure":
            R = Matrix.Rotation(math.radians(45), 4, "Y")
        if R is not None:
            o.matrix_world = Matrix.Translation(piv) @ R @ Matrix.Translation(-piv) @ mw
            if n_ == "Serrure":   # rangée sous Vantail dans le prefab : elle monte avec lui
                o.matrix_world = Matrix.Translation((0, 0, 1.6)) @ o.matrix_world
        elif n_ == "Herse":
            o.matrix_world = Matrix.Translation((0, 0, 4.6)) @ mw
        elif n_ == "Vantail":
            o.matrix_world = Matrix.Translation((0, 0, 1.6)) @ mw
        bpy.context.view_layer.update()
    mws = {o.name: o.matrix_world.copy() for o in meshes}
    for o in meshes:
        mw = mws[o.name]
        o.parent = None
        o.data.transform(mw)
        o.matrix_world = Matrix.Identity(4)
        if o.name.startswith("Col_"):
            o.hide_render = True
            continue
        for i, sl in enumerate(o.material_slots):
            if sl.material:
                k = re.sub(r"\.\d{3}$", "", sl.material.name)
                if k not in cache:
                    cache[k] = materiau(k)
                o.material_slots[i].material = cache[k]
    vis = [o for o in meshes if not o.hide_render]
    pts = [v.co for o in vis for v in o.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    for o in empties:
        if o.name.startswith("Feu"):
            flamme(o.matrix_world.translation)
    # sol et figurants
    sol_z = 0.0
    if "Chandelier" in nom or "Lustre" in nom:
        sol_z = mn.z - 3.5
    elif "Applique" in nom:
        sol_z = -2.0
    elif any(x in nom for x in ("Coursive", "Balcon", "Passerelle")):
        sol_z = -4.5
    figs = []
    xm = mn.x - 1.0
    ym = mn.y - 0.6
    if "Colimacon" in nom or "Puits" in nom or "Echelle" in nom or "Applique" in nom or "Candelabre" in nom:
        xm = mx.x + 0.8
    figs.append(mannequin((xm, ym, sol_z), rot=math.radians(20)))
    if "Escalier" in nom or "Mezzanine" in nom or "Coursive" in nom or "Passerelle" in nom or "Tremie" in nom:
        # second mannequin sur le niveau haut
        if "Coursive" in nom or "Passerelle" in nom or "Tremie" in nom:
            figs.append(mannequin(((mn.x + mx.x) / 2 + 0.6, (mn.y + mx.y) / 2 - 0.2 if "Tremie" not in nom else -3.0, 0.0)))
        elif "Mezzanine" in nom:
            figs.append(mannequin((0.5, -2.0, 5.0)))
        elif "Colimacon" in nom:
            pass
        else:
            princ = next((o for o in vis if o.name.split(".")[0] == nom), vis[0])
            ztop = float(re.search(r"_(\d+)m", nom).group(1))
            ptop = [v.co for v in princ.data.vertices if abs(v.co.z - ztop) < 0.01] or [Vector((0, 0, ztop))]
            px = sum(p.x for p in ptop) / len(ptop)
            py = sum(p.y for p in ptop) / len(ptop)
            figs.append(mannequin((px, py, ztop)))
    if "Balcon" in nom:
        figs.append(mannequin((0.4, -0.9, 0.0)))
    if "PorteHaute" in nom or "MurRebords" in nom or "Falaise_Rebords" in nom:
        for k_, p in reperes.items():
            if k_.startswith("Accroche_Bord") or k_.startswith("Accroche_Rebord_2"):
                figs.append(mannequin((p.x + 0.3, p.y + 0.5, p.z), rot=0.3))
                break
    gab = []
    if "PorteEtroite" in nom:
        p = reperes.get("Accroche_PorteEtroite", Vector((0, 0.75, 0)))
        gab.append(cadre("Gabarit_4x5m2", (p.x, p.y - 0.1, 0.0), 4.0, 5.2))
        gab.append(cadre("Gabarit_4x5m2_b", (p.x, p.y + 0.1, 0.0), 4.0, 5.2))
    if "Traqueur" in nom:
        p = reperes.get("Apparition_Traqueur", Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, 0)))
        gab.append(capsule("Traqueur_7x14", p, 3.5, 14.0))
    if "Falaise_Passage" in nom:
        gab.append(capsule("Traqueur_baisse_9m", Vector((0, 8.0, 0.0)), 3.5, 9.0))
    allp = pts + [v.co + f.location for f in figs for v in f.data.vertices]
    mn2 = Vector((min(min(p.x for p in allp), mn.x), min(min(p.y for p in allp), mn.y), min(min(p.z for p in allp), mn.z)))
    mx2 = Vector((max(max(p.x for p in allp), mx.x), max(max(p.y for p in allp), mx.y), max(max(p.z for p in allp), mx.z)))
    def sol(z, trou=None, rond=False):
        """Sol de contexte (troué au centre pour la fosse et le puits)."""
        cx, cy = (mn2.x + mx2.x) / 2, (mn2.y + mx2.y) / 2
        ex = max(mx2.x - mn2.x, mx2.y - mn2.y) / 2 + 8
        bm = bmesh.new()
        if trou is None:
            vs = [bm.verts.new((cx + a * ex, cy + b * ex, z)) for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
            bm.faces.new(vs)
        else:
            n = 32
            for i in range(n):
                a0, a1 = 2 * math.pi * i / n, 2 * math.pi * (i + 1) / n
                def inn(a):
                    if rond:
                        return (math.cos(a) * trou, math.sin(a) * trou)
                    c, s_ = math.cos(a), math.sin(a)
                    m_ = max(abs(c), abs(s_))
                    return (c / m_ * trou, s_ / m_ * trou)
                def out(a):
                    c, s_ = math.cos(a), math.sin(a)
                    m_ = max(abs(c), abs(s_))
                    return (c / m_ * ex, s_ / m_ * ex)
                q = [inn(a0), out(a0), out(a1), inn(a1)]
                bm.faces.new([bm.verts.new((x, y, z)) for x, y in q])
        me = bpy.data.meshes.new("Sol")
        bm.to_mesh(me)
        bm.free()
        ob = bpy.data.objects.new("Sol", me)
        sc.collection.objects.link(ob)
        me.materials.append(plat("R_Sol", (0.07, 0.075, 0.08)))
    if "Fosse" in nom:
        sol(-0.005, trou=7.5)
    elif "Puits" in nom:
        sol(-0.005, trou=1.6, rond=True)
    elif any(x in nom for x in ("Coursive", "Balcon", "Passerelle")):
        sol(sol_z)
        if "Passerelle" not in nom:
            bpy.ops.mesh.primitive_plane_add(size=1, location=((mn.x + mx.x) / 2, 0.01, (sol_z + 3.0) / 2), rotation=(math.radians(90), 0, 0))
            pl = bpy.context.active_object
            pl.scale = (mx.x - mn.x + 3.0, 3.0 - sol_z, 1)
            pl.data.materials.append(plat("R_Mur", (0.3, 0.29, 0.28)))
        else:
            for sx in (-1, 1):
                bpy.ops.mesh.primitive_cube_add(size=1, location=(sx * ((mx.x - mn.x) / 2 + 1.0), 0, (sol_z - 0.02) / 2))
                pl = bpy.context.active_object
                pl.scale = (2.6, 4.0, -sol_z - 0.02)
                pl.data.materials.append(plat("R_Mur", (0.3, 0.29, 0.28)))
    elif "Echelle" in nom:
        sol(0.0)
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0.01, (mx.z + 1) / 2), rotation=(math.radians(90), 0, 0))
        pl = bpy.context.active_object
        pl.scale = (4.0, mx.z + 1, 1)
        pl.data.materials.append(plat("R_Mur", (0.3, 0.29, 0.28)))
    elif mn.z > -0.6 and "Chandelier" not in nom and "Lustre" not in nom and "Applique" not in nom:
        bpy.ops.mesh.primitive_plane_add(size=1, location=((mn2.x + mx2.x) / 2, (mn2.y + mx2.y) / 2, -0.005))
        pl = bpy.context.active_object
        pl.scale = ((mx2.x - mn2.x) + 14, (mx2.y - mn2.y) + 14, 1)
        pl.data.materials.append(plat("R_Sol", (0.07, 0.075, 0.08)))
    elif "Chandelier" in nom or "Lustre" in nom:
        sol(sol_z)
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0, 0.05))
        pl = bpy.context.active_object
        pl.scale = (mx.x - mn.x + 3, mx.y - mn.y + 3, 1)
        pl.data.materials.append(plat("R_Plafond", (0.25, 0.24, 0.23)))
        pl.visible_shadow = False
    elif "Applique" in nom:
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, 0.002, 0), rotation=(math.radians(90), 0, 0))
        pl = bpy.context.active_object
        pl.scale = (3.0, 4.6, 1)
        pl.data.materials.append(plat("R_Mur", (0.35, 0.33, 0.3)))
        bpy.ops.mesh.primitive_plane_add(size=1, location=(0, -1.5, sol_z))
        pl = bpy.context.active_object
        pl.scale = (3.0, 3.0, 1)
        pl.data.materials.append(plat("R_Sol", (0.07, 0.075, 0.08)))
    scene_rendu()
    # caméra 3/4 avant-gauche, au-dessus
    c = (mn2 + mx2) / 2
    r = (mx2 - mn2).length / 2
    elev = 1.25 if ("Fosse" in nom or "Tremie" in nom) else 0.3 if ("Chandelier" in nom or "Lustre" in nom) else 0.62
    d = Vector((-0.72, -1.0, elev)).normalized()
    cam = bpy.data.cameras.new("Cam")
    cam.lens = 40
    co = bpy.data.objects.new("Cam", cam)
    sc.collection.objects.link(co)
    vfov = 2 * math.atan(math.tan(cam.angle / 2) * 0.75)
    dist = r / math.sin(vfov / 2) * 0.98
    co.location = c + d * dist
    co.rotation_euler = (-d).to_track_quat("-Z", "Y").to_euler()
    cam.clip_end = dist * 4
    sc.camera = co
    sc.render.filepath = os.path.join(out, nom + ".jpg")
    bpy.ops.render.render(write_still=True)
    print("rendu", nom, flush=True)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:]
    out = argv[0]
    os.makedirs(out, exist_ok=True)
    for f in argv[1:]:
        rend(f, out)
