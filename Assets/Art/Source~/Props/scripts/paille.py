# -*- coding: utf-8 -*-
"""Litière de paille (Prop_Paille, 2 × 0.3 × 2 m, décor sans collision), nuit 4, lot 4 b.

Pas de Tripo pour ce prop : une litière plate se reconstruit mal en image vers 3D (une galette bosselée). Le maillage est
construit ici : une nappe basse au contour irrégulier (bord à 3 mm du sol, quelques touffes, 0.3 m au plus haut) et des
brins isolés (prismes triangulaires très fins) posés dessus ou qui dépassent du bord. Texture : image peinte Higgsfield
(Nano Banana 2.1, docs/art/props/sources/concept_paille_texture_v01.png) réduite à 1024 et plaquée de dessus (1 répétition
sur 2 m, ≈ 512 px/m). Pas de LOD1 (petit prop plat, ≈ 1 000 triangles).

    blender -b --factory-startup -P paille.py
"""
import json, math, os, random, sys
import bpy, bmesh
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Kits", "scripts"))
sys.path.insert(0, HERE)
import kitlib  # noqa: E402
import faces_superposees  # noqa: E402

ROOT = kitlib.ROOT
SRC = os.path.join(ROOT, "docs", "art", "props", "sources")
TEX = os.path.join(ROOT, "Assets", "Art", "Textures", "Props", "T_Prop_Paille_BaseColor.png")
FBX = os.path.join(ROOT, "Assets", "Art", "Models", "Props", "SM_Prop_Paille.fbx")
CTRL = os.path.join(ROOT, "docs", "art", "rendus", "Nuit4", "Lot4", "controle_Paille.json")

R = random.Random(4242)
ANNEAUX, SECTEURS = 8, 44
TOUFFES = [(R.uniform(-0.5, 0.5), R.uniform(-0.5, 0.5), R.uniform(0.22, 0.38), R.uniform(0.04, 0.09)) for _ in range(5)]
ONDES = [(R.randint(2, 6), R.uniform(0, math.tau), R.uniform(0.03, 0.07)) for _ in range(4)]


def rayon(t):
    """Contour : super-ellipse (presque carrée) bruitée, demi-côté ≈ 0.93 m."""
    c, s = abs(math.cos(t)), abs(math.sin(t))
    r = (c ** 3 + s ** 3) ** (-1 / 3)
    r *= 0.86 + sum(a * math.sin(k * t + p) for k, p, a in ONDES)
    return r


def hauteur(x, y, rho):
    h = 0.075 * max(0.0, 1 - rho ** 2) ** 0.7
    for cx, cy, sig, amp in TOUFFES:
        h += amp * math.exp(-((x - cx) ** 2 + (y - cy) ** 2) / (2 * sig * sig)) * max(0.0, 1 - rho ** 2)
    h += 0.008 * math.sin(x * 17 + 1.3) * math.sin(y * 13 + 0.4) * (1 - rho)
    return max(0.003, h)


def nappe(bm, uv):
    rings = []
    centre = bm.verts.new((0, 0, hauteur(0, 0, 0)))
    for i in range(1, ANNEAUX + 1):
        rho = (i / ANNEAUX) ** 0.85
        ring = []
        for j in range(SECTEURS):
            t = math.tau * j / SECTEURS
            r = rayon(t) * rho
            x, y = r * math.cos(t), r * math.sin(t)
            z = 0.003 if i == ANNEAUX else hauteur(x, y, rho)
            ring.append(bm.verts.new((x, y, z)))
        rings.append(ring)
    faces = []
    for j in range(SECTEURS):
        faces.append(bm.faces.new((centre, rings[0][j], rings[0][(j + 1) % SECTEURS])))
    for a, b in zip(rings, rings[1:]):
        for j in range(SECTEURS):
            k = (j + 1) % SECTEURS
            faces.append(bm.faces.new((a[j], b[j], b[k], a[k])))
    for f in faces:
        for l in f.loops:
            l[uv].uv = (l.vert.co.x * 0.5 + 0.5, l.vert.co.y * 0.5 + 0.5)
    return faces


def surface(bm_faces, x, y):
    """Hauteur de la nappe sous (x, y) : on reprend la formule (le contour ne compte pas, brins près du bord)."""
    t = math.atan2(y, x)
    rho = min(1.0, math.hypot(x, y) / rayon(t))
    return hauteur(x, y, rho) if rho < 1 else 0.003


def brin(bm, uv, x, y, ang, long, rayon_b, z0, z1):
    """Prisme triangulaire ouvert (3 faces) de (x, y, z0) à son autre bout (z1)."""
    d = Vector((math.cos(ang), math.sin(ang), 0))
    a = Vector((x, y, z0)) - d * long / 2
    b = Vector((x, y, z1)) + d * long / 2
    b.z = z1
    axe = (b - a).normalized()
    n1 = axe.cross(Vector((0, 0, 1))).normalized()
    n2 = axe.cross(n1).normalized()
    pa, pb = [], []
    for k in range(3):
        o = (n1 * math.cos(k * math.tau / 3) + n2 * math.sin(k * math.tau / 3)) * rayon_b
        pa.append(bm.verts.new(a + o))
        pb.append(bm.verts.new(b + o * 0.6))
    u0, v0 = R.uniform(0.1, 0.8), R.uniform(0.1, 0.9)
    faces = []
    for k in range(3):
        m = (k + 1) % 3
        f = bm.faces.new((pa[k], pa[m], pb[m], pb[k]))
        for l, (du, dv) in zip(f.loops, ((0, 0), (0, 0.006), (0.12, 0.006), (0.12, 0))):
            l[uv].uv = (u0 + du, v0 + dv)
        faces.append(f)
    return faces


def main():
    kitlib.reset()
    me = bpy.data.meshes.new("SM_Prop_Paille")
    ob = bpy.data.objects.new("SM_Prop_Paille", me)
    bpy.context.scene.collection.objects.link(ob)
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    nappe(bm, uv)
    n = 0
    while n < 70:
        t = R.uniform(0, math.tau)
        rho = R.uniform(0.15, 1.12)
        r = rayon(t) * rho
        x, y = r * math.cos(t), r * math.sin(t)
        if abs(x) > 0.98 or abs(y) > 0.98:
            continue
        zs = surface(None, x, y)
        long = R.uniform(0.16, 0.34)
        z0 = zs + R.uniform(0.004, 0.012)
        z1 = z0 + R.uniform(-0.01, 0.05) if rho < 1 else 0.008
        brin(bm, uv, x, y, R.uniform(0, math.tau), long, R.uniform(0.0045, 0.007), z0, max(0.006, z1))
        n += 1
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for f in bm.faces:                          # nappe tournée vers le haut
        if f.calc_area() > 1e-4 and f.normal.z < 0 and len(f.verts) == 4 and abs(f.normal.z) > 0.5:
            f.normal_flip()
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    # emprise centrée sur le pivot et ramenée dans 2 × 2 m (même facteur en X et Y), hauteur plafonnée à 0.3 m
    xs = [v.co.x for v in bm.verts]; ys = [v.co.y for v in bm.verts]; zs = [v.co.z for v in bm.verts]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    k = min(1.0, 2.0 / (max(xs) - min(xs)), 2.0 / (max(ys) - min(ys)))
    kz = min(1.0, 0.3 / max(zs))
    for v in bm.verts:
        v.co.x, v.co.y, v.co.z = (v.co.x - cx) * k, (v.co.y - cy) * k, v.co.z * kz
    bm.to_mesh(me)
    bm.free()

    # texture
    img = bpy.data.images.load(os.path.join(SRC, "concept_paille_texture_v01.png"))
    img.scale(1024, 1024)
    img.filepath_raw = TEX
    img.file_format = "PNG"
    img.save()
    mat = bpy.data.materials.new("M_Prop_Paille")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next(nd for nd in nt.nodes if nd.type == "BSDF_PRINCIPLED")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(TEX, check_existing=True)
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    me.materials.append(mat)

    avant = faces_superposees.nettoie(ob)
    sup = faces_superposees.analyse(ob, False)
    vs = [v.co for v in me.vertices]
    mn = Vector([min(v[i] for v in vs) for i in range(3)])
    mx = Vector([max(v[i] for v in vs) for i in range(3)])
    tri = len(me.polygons)
    isoles = len(me.vertices) - len({i for p in me.polygons for i in p.vertices})
    nulles = sum(1 for e in me.edges if (me.vertices[e.vertices[0]].co - me.vertices[e.vertices[1]].co).length < 1e-6)
    ctrl = dict(nom="Paille", objets={"SM_Prop_Paille": dict(avant=avant, apres=dict(
        triangles=tri, sommets_isoles=isoles, aretes_nulles=nulles, faces_degenerees=sup["degenerees"],
        superposees=sup["superposees"], superposees_partielles=sup["partielles"]))},
        dimensions=[round(x, 3) for x in (mx - mn)], triangles_lod0=tri, triangles_lod1=0, lod1="aucun (prop plat)")
    ctrl["ok"] = isoles == 0 and nulles == 0 and sup["degenerees"] == 0 and sup["superposees"] == 0 and sup["partielles"] == 0
    kitlib.export([ob], FBX)
    rapport = dict(nom="Paille", source="maillage construit par paille.py (graine 4242) ; texture Nano Banana 2.1",
                   dimensions=ctrl["dimensions"], pivot_bas=round(mn.z, 4),
                   centre_emprise=[round((mn.x + mx.x) / 2, 3), round((mn.y + mx.y) / 2, 3)], triangles=tri,
                   triangles_lod1=0, echelle_appliquee=[1, 1, 1], texture="Assets/Art/Textures/Props/T_Prop_Paille_BaseColor.png",
                   texture_taille=1024, fbx="Assets/Art/Models/Props/SM_Prop_Paille.fbx", objets_fbx=["SM_Prop_Paille"])
    json.dump(rapport, open(os.path.join(SRC, "rapport_Paille.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    os.makedirs(os.path.dirname(CTRL), exist_ok=True)
    json.dump(ctrl, open(CTRL, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print("PAILLE", json.dumps(rapport, ensure_ascii=False), "CONTROLE", ctrl["ok"])


main()
