# -*- coding: utf-8 -*-
"""Outils communs aux ennemis tirés de Tripo (goule, traqueur) : import du GLB brut, rastérisation des UV (position 3D
de chaque texel, pour peindre des masques par la géométrie), peau vers le gris-mauve, décimation, mise à l'échelle et
pivot. S'appuie sur it_common (export FBX, vérification, rendus) du niveau 1.

Conventions : le GLB de Tripo regarde vers +X (Blender) ; les modèles livrés regardent vers -Y (Blender) = +Z (Unity),
pivot au sol, au centre ; 1 unité = 1 m.
"""
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
N1 = os.path.join(HERE, "..", "..", "Niveau1", "scripts")
sys.path.insert(0, N1)
sys.modules.pop("it_common", None)
import it_common as C
import bpy, bmesh
import numpy as np
from mathutils import Matrix, Vector

ROOT = C.ROOT
C.MODELS_DIR = os.path.join(ROOT, "Assets", "Art", "Models", "Ennemis")
C.SOURCE_DIR = os.path.join(ROOT, "Assets", "Art", "Source~", "Ennemis")
C.TEX_DIR = os.path.join(ROOT, "Assets", "Art", "Textures", "Ennemis")
C.RENDER_DIR = os.path.join(ROOT, "docs", "art", "rendus", "Ennemis")
C.RENDER_FMT = "JPEG"
C.RENDER_DIST_ADD = 0.0
for d in (C.MODELS_DIR, C.SOURCE_DIR, C.TEX_DIR, C.RENDER_DIR):
    os.makedirs(d, exist_ok=True)
TRIPO = os.path.join(ROOT, "Assets", "Art", "Source~", "Tripo")


def import_glb(name):
    """Importe le GLB, garde le maillage seul, transformation appliquée (coordonnées brutes de Tripo)."""
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=os.path.join(TRIPO, name + ".glb"))
    new = [o for o in bpy.data.objects if o not in before]
    ob = next(o for o in new if o.type == "MESH")
    bpy.context.view_layer.update()
    mw = ob.matrix_world.copy()
    ob.parent = None
    ob.data.transform(mw)
    ob.matrix_world = Matrix()
    for o in new:
        if o is not ob:
            bpy.data.objects.remove(o, do_unlink=True)
    return ob


def source_pixels(ob, size):
    """Texture de couleur du GLB, ramenée à size × size, en tableau (h, w, 4) (sRGB, rangée 0 = bas de l'image)."""
    src = next(n.image for n in ob.material_slots[0].material.node_tree.nodes if n.type == "TEX_IMAGE" and n.image)
    im = src.copy()
    im.scale(size, size)
    px = np.array(im.pixels[:], dtype=np.float32).reshape(size, size, 4)
    bpy.data.images.remove(im)
    return px


def save_png(px, name):
    h, w = px.shape[:2]
    im = bpy.data.images.new(name, w, h, alpha=False)
    im.pixels.foreach_set(np.ascontiguousarray(px, dtype=np.float32).ravel())
    path = os.path.join(C.TEX_DIR, name + ".png")
    im.filepath_raw = path
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)
    return path


def rasterize(ob, size, keep):
    """Position 3D (coordonnées de l'objet) de chaque texel couvert par un triangle dont le centre vérifie keep(c).
    Renvoie un tableau (size, size, 3) rempli de NaN hors des triangles retenus."""
    me = ob.data
    me.calc_loop_triangles()
    uv = me.uv_layers.active.data
    pos = np.full((size, size, 3), np.nan, dtype=np.float32)
    for tri in me.loop_triangles:
        P = [me.vertices[i].co for i in tri.vertices]
        c = (P[0] + P[1] + P[2]) / 3
        if not keep(c):
            continue
        U = np.array([uv[l].uv[:] for l in tri.loops], dtype=np.float64) * size
        x0, y0 = np.floor(U.min(0)).astype(int)
        x1, y1 = np.ceil(U.max(0)).astype(int)
        x0, y0 = max(x0, 0), max(y0, 0)
        x1, y1 = min(x1, size - 1), min(y1, size - 1)
        if x1 < x0 or y1 < y0:
            continue
        xs, ys = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
        (ax, ay), (bx, by), (cx, cy) = U
        d = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
        if abs(d) < 1e-12:
            continue
        l0 = ((by - cy) * (xs - cx) + (cx - bx) * (ys - cy)) / d
        l1 = ((cy - ay) * (xs - cx) + (ax - cx) * (ys - cy)) / d
        l2 = 1 - l0 - l1
        inside = (l0 >= -0.02) & (l1 >= -0.02) & (l2 >= -0.02)
        if not inside.any():
            continue
        Pm = np.array([p[:] for p in P], dtype=np.float32)
        q = l0[..., None] * Pm[0] + l1[..., None] * Pm[1] + l2[..., None] * Pm[2]
        yy, xx = np.nonzero(inside)
        pos[y0 + yy, x0 + xx] = q[yy, xx]
    return pos


def hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = rgb.max(-1); mn = rgb.min(-1)
    sat = (mx - mn) / np.maximum(mx, 1e-4)
    hue = np.degrees(np.arctan2(np.sqrt(3) * (g - b), 2 * r - g - b)) % 360
    return hue, sat, mx


def hsv_to_rgb(h, s, v):
    h = (h % 360) / 60.0
    i = np.floor(h).astype(int) % 6
    f = h - np.floor(h)
    p = v * (1 - s); q = v * (1 - s * f); t = v * (1 - s * (1 - f))
    out = np.zeros(h.shape + (3,), dtype=np.float32)
    for k, (a, b, c) in enumerate(((v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q))):
        m = i == k
        out[m, 0] = a[m]; out[m, 1] = b[m]; out[m, 2] = c[m]
    return out


def skin_to_mauve(px, hue_target=285.0, sat_mul=0.5, val_mul=1.12, sat_max=0.5, extra=None):
    """Peau rose (teintes 290 à 360° et 0 à 25°, saturation 0.06 à sat_max) et taches grises tirant sur le cyan
    (150 à 220°, peu saturées) ramenées vers le gris-mauve : teinte hue_target, saturation × sat_mul, valeur × val_mul.
    Fondu selon la saturation : les blancs, noirs et couleurs vives (dents, griffes, bouche, pagne) ne bougent pas.
    extra : masque booléen de texels à exclure en plus."""
    rgb = px[..., :3]
    h, s, v = hsv(rgb)
    pink = ((h > 290) | (h < 25)) & (s > 0.06) & (s < sat_max) & (v > 0.08)
    cyan = (h > 150) & (h < 220) & (s < 0.3) & (v > 0.15)
    m = pink | cyan
    if extra is not None:
        m &= ~extra
    w = np.clip((s - 0.04) / 0.08, 0, 1) * np.clip((sat_max - s) / 0.08, 0, 1) * m
    nh = np.full_like(h, hue_target)
    new = hsv_to_rgb(nh, np.clip(s * sat_mul, 0, 1), np.clip(v * val_mul, 0, 1))
    out = px.copy()
    out[..., :3] = rgb * (1 - w[..., None]) + new * w[..., None]
    return out, float(w.mean())


def blur(a, k):
    out = a.copy()
    for ax in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-k, k + 1):
            acc += np.roll(out, d, axis=ax)
        out = acc / (2 * k + 1)
    return out


def weld_decimate(ob, target, symmetry=None):
    """Soudure des doublons exacts (coutures d'UV du glTF), puis fusion d'arêtes jusqu'à target triangles."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    bm.to_mesh(ob.data); bm.free()
    t0 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    if target and t0 > target:
        vl = bpy.context.view_layer
        vl.objects.active = ob
        mod = ob.modifiers.new("Fusion", "DECIMATE")
        mod.decimate_type = "COLLAPSE"
        mod.use_collapse_triangulate = True
        mod.ratio = target / t0
        if symmetry:
            mod.use_symmetry = True
            mod.symmetry_axis = symmetry
        with bpy.context.temp_override(object=ob, active_object=ob):
            bpy.ops.object.modifier_apply(modifier=mod.name)
    return t0, sum(len(p.vertices) - 2 for p in ob.data.polygons)


def face_front_and_ground(ob, height=None, scale=None):
    """Tourne l'avant (+X du GLB) vers -Y, met à l'échelle (hauteur visée ou facteur), pivot au sol au centre."""
    me = ob.data
    me.transform(Matrix.Rotation(math.radians(-90), 4, "Z"))
    co = np.array([v.co[:] for v in me.vertices])
    mn, mx = co.min(0), co.max(0)
    k = scale if scale else height / (mx[2] - mn[2])
    c = (mn + mx) / 2
    me.transform(Matrix.Scale(k, 4) @ Matrix.Translation((-c[0], -c[1], -mn[2])))
    me.update()
    co = np.array([v.co[:] for v in me.vertices])
    return k, co.max(0) - co.min(0)


def smooth_shading(ob, angle=40.0):
    bm = bmesh.new(); bm.from_mesh(ob.data)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) != 2 or e.calc_face_angle(math.pi) > math.radians(angle):
            e.smooth = False
    bm.to_mesh(ob.data); bm.free()


def tri_count(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)
