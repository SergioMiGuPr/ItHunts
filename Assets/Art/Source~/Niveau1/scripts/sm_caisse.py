# -*- coding: utf-8 -*-
"""SM_Caisse : caisse attrapable, modèle généré par IA avec Tripo (Assets/Art/Source~/Tripo/tripo_caisse_moyenne_v01.glb),
nettoyée dans Blender.

- Orientation : le GLB regarde vers +X (Blender) ; tourné de -90° autour de Z : sa face avant (la rune) regarde vers
  -Y (Blender) = +Z (Unity).
- Coutures d'UV soudées, petits trous rebouchés (18 bords ouverts), corde soudée gardée.
- Décimation vers ≈ 1 500 triangles (8 907 au départ ; budget des petits props attrapables : 500 à 2 000).
- Cotes : **cube exact de 0.8 m** (Prop_Caisse_Attrapable sur main : cube unité à l'échelle 0.8) ; mise à l'échelle par
  axe (le GLB faisait 1.00 × 0.995 × 0.943). Pivot : au sol, au centre.
- Textures (512 px) : T_Caisse_BaseColor (couleur ; la rune violette rendue neutre, gravée dans le bois) et
  T_Caisse_Emission (masque blanc de la rune, pour l'émission que le jeu pilote sur les objets attrapables).
- La rune du GLB n'est que sur la face avant : elle est relevée sur cette face (coordonnées de la face, par la position 3D
  de chaque texel) puis reportée sur les trois autres faces latérales, même sens de lecture : une caisse attrapable se
  reconnaît sous n'importe quel angle.

Lancer dans Blender (avec l'interface ; le script de reprise en arrière-plan le fait aussi) :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_caisse.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("n1_props", None)
import n1_props as N
C, F = N.C, N.F
import bpy, bmesh
import numpy as np
from mathutils import Matrix, Vector

PARAMS = dict(
    glb=os.path.join(C.ROOT, "Assets", "Art", "Source~", "Tripo", "tripo_caisse_moyenne_v01.glb"),
    cote=0.8, triangles=1500, texture=512,
    couleur="T_Caisse_BaseColor", masque="T_Caisse_Emission",
    rune_teinte=(0.22, 0.17, 0.13),       # bois sombre, gravé (sRGB)
    grille=512, retrait=(0.03, 0.14),     # profondeur des panneaux gravés derrière le cadre (m)                           # relevé de la rune de la face avant (cases par côté)
)


def rune_mask(px):
    """Pixels violets de la rune (et de sa lueur peinte) : teinte 250–320°, saturés et assez clairs."""
    r, g, b = px[..., 0], px[..., 1], px[..., 2]
    mx = px[..., :3].max(-1); mn = px[..., :3].min(-1)
    sat = (mx - mn) / np.maximum(mx, 1e-4)
    hue = np.degrees(np.arctan2(np.sqrt(3) * (g - b), 2 * r - g - b)) % 360
    return (hue > 250) & (hue < 320) & (sat > 0.25) & (mx > 0.18) & (b > g)


def blur(a, k):
    out = a.copy()
    for ax in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-k, k + 1):
            acc += np.roll(out, d, axis=ax)
        out = acc / (2 * k + 1)
    return out


def textures(img, P, ob=None):
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)      # valeurs de l'image (sRGB pour une PNG)
    rgb = px[..., :3]
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = rgb.max(-1); mn = rgb.min(-1)
    sat = (mx - mn) / np.maximum(mx, 1e-4)
    hue = np.degrees(np.arctan2(np.sqrt(3) * (g - b), 2 * r - g - b)) % 360
    violet = (hue > 245) & (hue < 325) & (b > g)
    # 1. couleur : tout violet (rune et sa lueur peinte, reflets mauves) ramené vers le brun du bois, selon sa saturation
    k = np.clip((sat - 0.08) / 0.3, 0, 1) * violet
    lum = rgb.mean(-1, keepdims=True)
    tint = np.array(P["rune_teinte"], dtype=np.float32)
    brown = np.clip(tint / tint.mean() * lum * 0.9, 0, 1)
    base = px.copy()
    base[..., :3] = rgb * (1 - k[..., None]) + brown * k[..., None]
    # 2. masque d'émission : le cœur lumineux de la rune (très saturé et clair), élargi de 2 px
    m = violet & (sat > 0.45) & (mx > 0.35)
    core = np.clip(blur(m.astype(np.float32), 2) * 3.0, 0, 1)
    mask = np.zeros_like(px); mask[..., 3] = 1.0
    mask[..., 0] = mask[..., 1] = mask[..., 2] = core
    return base, mask, k, int(m.sum())


def save_textures(base, mask, P):
    h, w = base.shape[:2]
    out = {}
    for name, data in ((P["couleur"], base), (P["masque"], mask)):
        im = bpy.data.images.new(name, w, h, alpha=False)
        im.pixels.foreach_set(np.ascontiguousarray(data).ravel())
        im.scale(P["texture"], P["texture"])
        path = os.path.join(C.TEX_DIR, name + ".png")
        im.filepath_raw = path; im.file_format = "PNG"; im.save()
        bpy.data.images.remove(im)
        out[name] = path
    return out


def rasterize(ob, size, keep):
    """Position 3D de chaque texel couvert par un triangle qui vérifie keep(centre, normale) (NaN ailleurs)."""
    me = ob.data
    me.calc_loop_triangles()
    uv = me.uv_layers.active.data
    pos = np.full((size, size, 3), np.nan, dtype=np.float32)
    for tri in me.loop_triangles:
        P3 = [me.vertices[i].co for i in tri.vertices]
        nrm = (P3[1] - P3[0]).cross(P3[2] - P3[0])
        if nrm.length < 1e-12 or not keep((P3[0] + P3[1] + P3[2]) / 3, nrm.normalized()):
            continue
        U = np.array([uv[l].uv[:] for l in tri.loops], dtype=np.float64) * size
        x0, y0 = np.maximum(np.floor(U.min(0)).astype(int), 0)
        x1, y1 = np.minimum(np.ceil(U.max(0)).astype(int), size - 1)
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
        inside = (l0 >= -0.01) & (l1 >= -0.01) & (l2 >= -0.01)
        if not inside.any():
            continue
        Pm = np.array([q[:] for q in P3], dtype=np.float32)
        q = l0[..., None] * Pm[0] + l1[..., None] * Pm[1] + l2[..., None] * Pm[2]
        yy, xx = np.nonzero(inside)
        pos[y0 + yy, x0 + xx] = q[yy, xx]
    return pos


# faces latérales : normale (Blender) ; la face avant regarde vers -Y
SIDES = {"avant": (0, -1), "droite": (1, 0), "arriere": (0, 1), "gauche": (-1, 0)}


def runes_on_sides(ob, base, mask, k, P):
    """Relève la rune de la face avant sur une grille (u : vers la droite de qui regarde la face, v : hauteur), puis la
    reporte sur les trois autres faces latérales : gravure dans la couleur (poids k) et masque d'émission."""
    S = base.shape[0]
    half = P["cote"] / 2
    G = P["grille"]
    depth = P["retrait"]

    def face_uv(pos, n):
        right = np.array((-n[1], n[0]))           # (-n) × haut, dans le plan horizontal
        u = pos[..., 0] * right[0] + pos[..., 1] * right[1]
        return (u / P["cote"] + 0.5), pos[..., 2] / P["cote"]

    def on(n):
        # faces tournées vers n (panneau en retrait compris : la rune est gravée 6 à 10 cm derrière le cadre)
        # (les faces saillantes, cadre et corde de la poignée, ne la reçoivent pas)
        return lambda c, nr: (nr.x * n[0] + nr.y * n[1]) > 0.7 and half - depth[1] < (c.x * n[0] + c.y * n[1]) < half - depth[0]
    # relevé sur la face avant
    pf = rasterize(ob, S, on(SIDES["avant"]))
    okf = ~np.isnan(pf[..., 0])
    u, v = face_uv(pf, SIDES["avant"])
    iu = np.clip((u[okf] * G).astype(int), 0, G - 1); iv = np.clip((v[okf] * G).astype(int), 0, G - 1)
    g_col = np.zeros((G, G, 3), np.float32); g_k = np.zeros((G, G), np.float32); g_m = np.zeros((G, G), np.float32)
    g_n = np.zeros((G, G), np.float32)
    np.add.at(g_col, (iv, iu), base[okf][:, :3]); np.add.at(g_n, (iv, iu), 1)
    np.maximum.at(g_k, (iv, iu), k[okf]); np.maximum.at(g_m, (iv, iu), mask[okf][:, 0])
    g_col /= np.maximum(g_n, 1)[..., None]
    allp = rasterize(ob, S, lambda c, nr: True)
    okm = (mask[..., 0] > 0.5) & ~np.isnan(allp[..., 0])
    cm = allp[okm]
    info = ["face avant : %d texels relevés, rune sur la face %d texels ; rune sur le modèle : %d texels, centre %s, min %s, max %s"
            % (okf.sum(), int((mask[okf][:, 0] > 0.5).sum()), okm.sum(), cm.mean(0).round(3) if len(cm) else None,
               cm.min(0).round(3) if len(cm) else None, cm.max(0).round(3) if len(cm) else None)]
    out_b, out_m = base.copy(), mask.copy()
    for name, n in SIDES.items():
        if name == "avant":
            continue
        ps = rasterize(ob, S, on(n))
        ok = ~np.isnan(ps[..., 0])
        u, v = face_uv(ps, n)
        ju = np.clip((u[ok] * G).astype(int), 0, G - 1); jv = np.clip((v[ok] * G).astype(int), 0, G - 1)
        kk = g_k[jv, ju] * (g_n[jv, ju] > 0)
        col = out_b[ok]
        col[:, :3] = col[:, :3] * (1 - kk[:, None]) + g_col[jv, ju] * kk[:, None]
        out_b[ok] = col
        mm = out_m[ok]
        mm[:, :3] = np.maximum(mm[:, :3], (g_m[jv, ju] * (g_n[jv, ju] > 0))[:, None])
        out_m[ok] = mm
        info.append("face %s : %d texels, rune %d texels" % (name, ok.sum(), int((g_m[jv, ju] > 0.5).sum())))
    return out_b, out_m, info


def build(P):
    bpy.ops.import_scene.gltf(filepath=P["glb"])
    obs = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    ob = obs[0]
    for o in bpy.context.scene.objects:
        if o.type != "MESH":
            bpy.data.objects.remove(o, do_unlink=True)
    bpy.context.view_layer.update()
    mw = ob.matrix_world.copy()
    ob.parent = None
    ob.data.transform(mw); ob.matrix_world = Matrix()
    # textures (avant de remplacer le matériau)
    src = next(n.image for n in ob.material_slots[0].material.node_tree.nodes if n.type == "TEX_IMAGE" and n.image)
    base, mask, k_rune, n_rune = textures(src, P)
    # orientation : face avant (+X) -> -Y
    ob.data.transform(Matrix.Rotation(math.radians(-90), 4, "Z"))
    # soudure des coutures, trous rebouchés
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)      # doublons exacts des coutures d'UV seulement
    b0 = sum(1 for e in bm.edges if e.is_boundary)
    # fentes de jonction (< 1 mm) : faces dégénérées dissoutes, faces minuscules des arêtes à 3 faces supprimées, rebouchage
    for _ in range(3):
        bmesh.ops.dissolve_degenerate(bm, dist=0.0005, edges=bm.edges[:])
        nm = [e for e in bm.edges if not e.is_manifold and not e.is_boundary]
        tiny = {f for e in nm for f in e.link_faces if f.calc_area() < 2e-5}
        if tiny:
            bmesh.ops.delete(bm, geom=list(tiny), context="FACES")
        bmesh.ops.holes_fill(bm, edges=[e for e in bm.edges if e.is_boundary], sides=0)
        bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    b1 = sum(1 for e in bm.edges if e.is_boundary)
    nm1 = sum(1 for e in bm.edges if not e.is_manifold and not e.is_boundary)
    print("réparation : bords ouverts %d -> %d, arêtes non-manifold restantes %d" % (b0, b1, nm1))
    bm.to_mesh(ob.data); bm.free()
    # décimation vers ≈ 1 500 triangles
    t0 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    bpy.context.view_layer.objects.active = ob
    # 1. faces coplanaires dissoutes (presque sans perte sur une caisse), coutures d'UV et matériaux gardés
    mod = ob.modifiers.new("Plan", "DECIMATE")
    mod.decimate_type = "DISSOLVE"; mod.angle_limit = math.radians(4.0)
    mod.delimit = {"UV", "MATERIAL"}
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.quads_convert_to_tris(); bpy.ops.object.mode_set(mode="OBJECT")
    t_plan = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    # 2. fusion d'arêtes pour le reste, jusqu'au budget
    if t_plan > P["triangles"]:
        mod = ob.modifiers.new("Fusion", "DECIMATE")
        mod.decimate_type = "COLLAPSE"; mod.use_collapse_triangulate = True
        mod.ratio = P["triangles"] / t_plan
        bpy.ops.object.modifier_apply(modifier=mod.name)
    print("décimation : %d -> %d (plans) -> %d" % (t0, t_plan, sum(len(p.vertices) - 2 for p in ob.data.polygons)))
    bm = bmesh.new(); bm.from_mesh(ob.data)
    print("contrôle final : bords ouverts %d, non-manifold %d" % (sum(e.is_boundary for e in bm.edges),
          sum((not e.is_manifold) and not e.is_boundary for e in bm.edges)))
    bm.free()
    # cube exact, pivot au sol au centre
    vs = [v.co for v in ob.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    sz = mx - mn
    c = (mn + mx) / 2
    k = P["cote"]
    ob.data.transform(Matrix.Diagonal((k / sz.x, k / sz.y, k / sz.z, 1)) @ Matrix.Translation((-c.x, -c.y, -mn.z)))
    bm = bmesh.new(); bm.from_mesh(ob.data)           # lissé, arêtes vives au-delà de 30° (comme les autres props)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) != 2 or e.calc_face_angle(math.pi) > math.radians(30):
            e.smooth = False
    bm.to_mesh(ob.data); bm.free()
    # rune sur les quatre faces latérales, puis textures
    base, mask, info = runes_on_sides(ob, base, mask, k_rune, P)
    print(" ; ".join(info))
    tex = save_textures(base, mask, P)
    # matériau : couleur de base seule (le masque d'émission est pour le .mat d'Unity)
    mat = C.make_material("M_Caisse", tex[P["couleur"]])
    ob.data.materials.clear(); ob.data.materials.append(mat)
    ob.name = ob.data.name = "SM_Caisse"
    t1 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    print("caisse : %d -> %d triangles ; bords ouverts %d -> %d ; échelle par axe (%.3f, %.3f, %.3f) ; rune %d px"
          % (t0, t1, b0, b1, k / sz.x, k / sz.y, k / sz.z, n_rune))
    return [ob]


if __name__ == "__main__":
    N.run("SM_Caisse", build, PARAMS, views=("face", "cote", "34"))
