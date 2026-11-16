# -*- coding: utf-8 -*-
"""SM_Traqueur : le traqueur (boss), version statique provisoire (sans squelette), modèle généré par IA avec Tripo
(Assets/Art/Source~/Tripo/tripo_traqueur_v02.glb, Tripo H3.1), retouché dans Blender.

Retouches communes : mèches de cheveux devant le visage retirées ; peau ramenée vers le gris-mauve (visage blanc, bouche,
dents et griffes inchangés) ; deux petites pupilles rouges peintes au fond des orbites noires, avec leur masque
d'émission (T_Traqueur_Emission) ; texture de couleur gardée en 4096 px.

Livrables (1 unité = 1 m, avant vers -Y Blender = +Z Unity, matériau unique M_Traqueur) :
- SM_Traqueur : le géant d'un seul tenant, 14 m de haut (capsule de jeu Ø 7 × 14 m), pivot au sol au centre ; bras
  baissés le long du corps (bout des doigts au genou), épaissis à 1.5 m, mains à 0.67 de leur taille d'origine.
- Pièces pour le code (Stalker.cs pose lui-même les bras et les mains) :
  - SM_Traqueur_Corps : le même corps à 14 m, sans les bras (épaules refermées) : remplace « Corps (se baisse) » ;
  - SM_Traqueur_Bras : un tronçon de bras ramené à une boîte unité (Ø 1, longueur 1 le long de +Z, centré) :
    remplace le cube des bras (le code met l'échelle (armThickness, armThickness, longueur)) ;
  - SM_Traqueur_Main_D, SM_Traqueur_Main_G : les mains (taille d'origine, doigts et griffes), ramenées à une boîte
    unité (doigts vers +Z, paume vers -Y, centrée) : remplacent les cubes des mains (échelle handSize) ; la main
    visible remplit donc exactement la zone qui tue.

Lancer dans Blender (avec l'interface) :
    p = r"<dépôt>/Assets/Art/Source~/Ennemis/scripts/sm_traqueur.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("ennemis_common", None)
import ennemis_common as E
C = E.C
import bpy, bmesh
import numpy as np
from mathutils import Matrix, Vector, Quaternion

PARAMS = dict(
    glb="tripo_traqueur_v02",
    hauteur=14.0, texture=4096, masque=2048,
    # coordonnées brutes du GLB (avant vers +X, hauteur 0.576, droite du modèle vers -Y)
    epaule=(-0.045, 0.075, 0.170),       # centre de l'articulation (y : côté, signe appliqué)
    poignet=(0.014, 0.300, 0.033),
    genou=0.26,                           # hauteur du genou en fraction de la hauteur
    pendant=dict(avant=0.12, ecart_deg=12.0),
    main=0.67, bras_diametre_m=1.5, torsion_main_deg=90.0,
    meches=dict(x_min=0.012, z_max=0.262, y_max=0.05),
    yeux=dict(z=(0.222, 0.260), y=(0.003, 0.022), x_min=0.025, sombre=0.14, pupille=0.32),
    coupe_bras=(0.20, 0.92), coupe_main=0.97,
)
CACHE = {}


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def verts(me):
    a = np.zeros(len(me.vertices) * 3)
    me.vertices.foreach_get("co", a)
    return a.reshape(-1, 3)


def set_verts(me, V):
    me.vertices.foreach_set("co", V.ravel())
    me.update()


def remove_front_hair(ob, px, P):
    """Mèches devant le visage : faces noires reliées à la masse des cheveux, devant le plan du visage."""
    me = ob.data
    S = px.shape[0]
    uv = me.uv_layers.active.data
    bm = bmesh.new(); bm.from_mesh(me)
    uvl = bm.loops.layers.uv.active
    black = set()
    for f in bm.faces:
        u = sum((l[uvl].uv for l in f.loops), Vector((0, 0))) / len(f.loops)
        x = min(max(int(u.x * S), 0), S - 1); y = min(max(int(u.y * S), 0), S - 1)
        if px[y, x, :3].max() < 0.10:
            black.add(f)
    # composantes connexes des faces noires (par les arêtes)
    comp = {}
    cid = 0
    for f in black:
        if f in comp:
            continue
        stack = [f]; comp[f] = cid
        while stack:
            g = stack.pop()
            for e in g.edges:
                for h in e.link_faces:
                    if h in black and h not in comp:
                        comp[h] = cid; stack.append(h)
        cid += 1
    sizes = np.bincount(np.array(list(comp.values()))) if comp else np.zeros(1)
    hair_ids = set(np.argsort(sizes)[::-1][:3])              # la masse des cheveux (et ses gros morceaux)
    M = P["meches"]
    kill = []
    for f, c in comp.items():
        if c not in hair_ids:
            continue
        cc = f.calc_center_median()
        if cc.x > M["x_min"] and cc.z < M["z_max"] and abs(cc.y) < M["y_max"]:
            kill.append(f)
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    bm.to_mesh(me); bm.free()
    return len(kill), int(sizes.max()) if len(sizes) else 0


def pupils(px_base, em_size, pos, P):
    """Pupilles rouges : centre des orbites noires (texels sombres), disque rouge dans la couleur + masque blanc."""
    Y = P["yeux"]
    lum = px_base[..., :3].max(-1)
    x, y, z = pos[..., 0], pos[..., 1], pos[..., 2]
    ok = ~np.isnan(x)
    with np.errstate(invalid="ignore"):
        sel = ok & (z > Y["z"][0]) & (z < Y["z"][1]) & (np.abs(y) > Y["y"][0]) & (np.abs(y) < Y["y"][1]) \
            & (x > Y["x_min"]) & (lum < Y["sombre"])
    base = px_base.copy()
    S = base.shape[0]
    mask = np.zeros((S, S), dtype=np.float32)
    info = {}
    red = np.array([0.85, 0.06, 0.03], dtype=np.float32)
    for side, sgn in (("droit", -1), ("gauche", 1)):
        pts = pos[sel & (np.sign(y) == sgn)]
        if len(pts) < 10:
            info[side] = "introuvable"
            continue
        c = pts.mean(0)
        r = np.percentile(np.linalg.norm(pts - c, axis=1), 85)
        rp = r * Y["pupille"]
        d = np.linalg.norm(np.nan_to_num(pos, nan=9.0) - c, axis=-1)
        disk = np.clip((rp - d) / (0.25 * rp), 0, 1)
        halo = np.clip((rp * 1.8 - d) / (0.6 * rp), 0, 1) * 0.35
        base[..., :3] = base[..., :3] * (1 - disk[..., None]) + red * disk[..., None]
        mask = np.maximum(mask, np.maximum(disk, halo))
        info[side] = "orbite (%.4f, %.4f, %.4f), rayon %.4f, pupille %.4f, %d texels" % (c[0], c[1], c[2], r, rp, len(pts))
    em = np.zeros((S, S, 4), dtype=np.float32); em[..., 3] = 1
    em[..., 0] = em[..., 1] = em[..., 2] = mask
    return base, em, info


def arm_frame(P, sgn):
    S = Vector((P["epaule"][0], sgn * P["epaule"][1], P["epaule"][2]))
    W = Vector((P["poignet"][0], sgn * P["poignet"][1], P["poignet"][2]))
    L = (W - S).length
    return S, W, L, (W - S).normalized()


def arm_params(V, S, d, L, sgn):
    p = V - np.array(S[:])
    along = p @ np.array(d[:])
    t = along / L
    radial = p - along[:, None] * np.array(d[:])
    dist = np.linalg.norm(radial, axis=1)
    side = (V[:, 1] * sgn) > 0.05
    # bras : près de l'axe épaule-poignet (le coude l'écarte de 2 cm au plus) ; main : tout ce qui est très écarté du
    # corps (les mains sont à plus de 0.25 de l'axe du corps en pose A, les jambes à moins de 0.1)
    near = smoothstep(0.065, 0.05, dist)
    hand = (t > 0.9) & ((V[:, 1] * sgn) > 0.22)
    rad_ok = np.where(hand, 1.0, near)
    w = smoothstep(0.0, 0.2, t) * rad_ok * side
    return along, t, radial, dist, w


def lower_arms(ob, P, H, zmin):
    """Bras baissés le long du corps, raccourcis (bout des doigts au genou), épaissis ; mains à P['main']."""
    me = ob.data
    V = verts(me)
    out = V.copy()
    report = []
    for sgn in (-1, 1):
        S, W, L, d = arm_frame(P, sgn)
        along, t, radial, dist, w = arm_params(V, S, d, L, sgn)
        hand_len = along[(t > 1.0) & (w > 0.5)].max() - L
        ds = []
        for tb in np.arange(0.3, 0.71, 0.05):       # tranches : centre propre à chaque tranche (le coude décale l'axe)
            sl = (np.abs(t - tb) < 0.02) & (w > 0.9)
            if sl.sum() < 12:
                continue
            c = V[sl].mean(0)
            r = V[sl] - c
            r = r - np.outer(r @ np.array(d[:]), np.array(d[:]))
            ds.append(2 * np.median(np.linalg.norm(r, axis=1)))
        diam = float(np.median(ds))
        k14 = P["hauteur"] / H
        f_th = (P["bras_diametre_m"] / k14) / diam
        sh = P["main"]
        a = math.radians(P["pendant"]["ecart_deg"])
        dn = Vector((P["pendant"]["avant"], sgn * math.sin(a), -math.cos(a))).normalized()
        knee = zmin + P["genou"] * H
        total = (S.z - knee) / (-dn.z)
        s_arm = (total - hand_len * sh) / L
        q0 = d.rotation_difference(dn)
        twist = Quaternion(dn, sgn * math.radians(P["torsion_main_deg"]))
        rs_arm = np.where(t < 0.2, 1.25 + (f_th - 1.25) * smoothstep(0, 0.2, t),
                          np.where(t < 0.75, f_th, f_th + (sh * 1.1 - f_th) * smoothstep(0.75, 1.0, t)))
        along_new = np.where(t <= 1.0, along * s_arm, L * s_arm + (along - L) * sh)
        rs = np.where(t <= 1.0, rs_arm, sh)
        idx = np.nonzero(w > 1e-4)[0]
        for i in idx:
            wi = w[i]
            ab = along[i] + (along_new[i] - along[i]) * wi
            rb = 1 + (rs[i] - 1) * wi
            tw = float(smoothstep(0.75, 1.0, t[i]))
            qi = Quaternion().slerp(Quaternion().slerp(twist, tw) @ q0, wi)
            v = Vector(d) * ab + Vector(radial[i]) * rb
            out[i] = np.array((S + qi @ v)[:])
        report.append("bras %s : diamètre %.4f -> × %.2f (1.5 m à 14 m), longueur × %.2f, main × %.2f, longueur de la main %.4f"
                      % ("droit" if sgn < 0 else "gauche", diam, f_th, s_arm, sh, hand_len))
    set_verts(me, out)
    return report


def bisect_keep(ob, origin, normal):
    """Coupe l'objet par un plan et garde le côté où pointe la normale ; trous rebouchés."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    geom = bm.verts[:] + bm.edges[:] + bm.faces[:]
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=origin, plane_no=normal, clear_inner=True)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-6)
    edges = [e for e in bm.edges if e.is_boundary]
    if edges:
        bmesh.ops.holes_fill(bm, edges=edges, sides=0)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    bm.to_mesh(ob.data); bm.free()


def keep_nearest(ob, point):
    """Garde le morceau (composante connexe) qui contient le sommet le plus proche de point."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bm.verts.ensure_lookup_table()
    p = Vector(point)
    start = min(bm.verts, key=lambda v: (v.co - p).length)
    seen = {start: 1}; st = [start]
    while st:
        x = st.pop()
        for e in x.link_edges:
            y = e.other_vert(x)
            if y not in seen:
                seen[y] = 1; st.append(y)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v not in seen], context="VERTS")
    bm.to_mesh(ob.data); bm.free()


def keep_largest(ob):
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bm.verts.ensure_lookup_table()
    seen = {}
    comps = []
    for v in bm.verts:
        if v in seen:
            continue
        c = [v]; seen[v] = 1; st = [v]
        while st:
            x = st.pop()
            for e in x.link_edges:
                y = e.other_vert(x)
                if y not in seen:
                    seen[y] = 1; c.append(y); st.append(y)
        comps.append(c)
    comps.sort(key=len, reverse=True)
    for c in comps[1:]:
        bmesh.ops.delete(bm, geom=c, context="VERTS")
    bm.to_mesh(ob.data); bm.free()


def dup(ob, name):
    o = ob.copy(); o.data = ob.data.copy(); o.name = name; o.data.name = name
    bpy.context.scene.collection.objects.link(o)
    return o


def remove_arms(ob, P):
    """Corps sans les bras : sommets des bras supprimés au-delà de l'épaule, trous rebouchés."""
    me = ob.data
    V = verts(me)
    bm = bmesh.new(); bm.from_mesh(me)
    bm.verts.ensure_lookup_table()
    kill = set()
    for sgn in (-1, 1):
        S, W, L, d = arm_frame(P, sgn)
        along, t, radial, dist, w = arm_params(V, S, d, L, sgn)
        for i in np.nonzero((t > 0.10) & (w > 0.35))[0]:
            kill.add(bm.verts[i])
    bmesh.ops.delete(bm, geom=list(kill), context="VERTS")
    shoulders = [arm_frame(P, sgn)[0] for sgn in (-1, 1)]
    edges = [e for e in bm.edges if e.is_boundary
             and min(((e.verts[0].co + e.verts[1].co) / 2 - Sh).length for Sh in shoulders) < 0.09]
    bmesh.ops.holes_fill(bm, edges=edges, sides=0)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    bm.to_mesh(me); bm.free()
    keep_largest(ob)
    return len(kill)


def to_unit_box(ob, origin, fwd, up):
    """Pièce ramenée à une boîte unité centrée : +Z Unity (= -Y Blender) le long de fwd, +Y Unity (= +Z Blender) vers
    up, l'axe X complète un repère direct (rotation pure : aucune pièce en miroir). Renvoie la taille d'origine
    (x, y, z Unity)."""
    f = Vector(fwd).normalized()
    u = Vector(up); u = (u - f * u.dot(f)).normalized()
    ax = (-f).cross(u).normalized()
    R = Matrix((ax, -f, u))                      # lignes : axes Blender de la pièce exprimés en coordonnées brutes
    V = verts(ob.data) - np.array(origin[:])
    Vb = V @ np.array(R).T
    mn, mx = Vb.min(0), Vb.max(0)
    c, s = (mn + mx) / 2, (mx - mn)
    set_verts(ob.data, (Vb - c) / s)
    return np.array([s[0], s[2], s[1]])


def build_all(P):
    if CACHE:
        return CACHE
    ob = E.import_glb(P["glb"])
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    bm.to_mesh(ob.data); bm.free()
    S = P["texture"]
    px = E.source_pixels(ob, S)
    n_hair, n_mass = remove_front_hair(ob, px, P)
    V = verts(ob.data)
    zmin, zmax = V[:, 2].min(), V[:, 2].max()
    H = zmax - zmin
    # textures : peau, pupilles (rastérisation de la tête), masque d'émission
    base, part = E.skin_to_mauve(px, sat_max=0.45)
    pos = E.rasterize(ob, S, lambda c: c.x > 0.0 and c.z > 0.20 and abs(c.y) < 0.06)
    base, em, info = pupils(base, P["masque"], pos, P)
    p_base = E.save_png(base, "T_Traqueur_BaseColor")
    em_small = bpy.data.images.new("_em", S, S, alpha=False)
    em_small.pixels.foreach_set(em.ravel()); em_small.scale(P["masque"], P["masque"])
    em_px = np.array(em_small.pixels[:], dtype=np.float32).reshape(P["masque"], P["masque"], 4)
    bpy.data.images.remove(em_small)
    E.save_png(em_px, "T_Traqueur_Emission")
    mat = C.make_material("M_Traqueur", p_base)
    E.smooth_shading(ob, 60)
    ob.data.materials.clear(); ob.data.materials.append(mat)
    k = P["hauteur"] / H
    log = ["mèches retirées : %d faces (masse des cheveux : %d faces)" % (n_hair, n_mass),
           "peau recolorée : %.1f %% des texels" % (100 * part), "hauteur brute %.4f -> échelle × %.3f" % (H, k)]
    log += ["pupille %s : %s" % kv for kv in info.items()]
    # pièces pour le code, prises sur le modèle d'origine (avant de baisser les bras)
    parts = {}
    for sgn, nom in ((-1, "SM_Traqueur_Main_D"), (1, "SM_Traqueur_Main_G")):
        Sv, Wv, L, d = arm_frame(P, sgn)
        h = dup(ob, nom)
        bisect_keep(h, Sv + d * (L * P["coupe_main"]), d)
        keep_nearest(h, Sv + d * (L * 1.3))
        Vh = verts(h.data)
        thumb = Vector(Vh[np.argmax(Vh[:, 2])].tolist())          # le pouce pointe vers le haut en pose A
        f = d.copy()
        T = thumb - Wv; T = (T - f * T.dot(f)).normalized()
        # paume vers le bas, doigts vers l'avant : main droite, pouce à gauche (dos = f × T) ; main gauche, l'inverse
        up = f.cross(T) if sgn < 0 else T.cross(f)
        s = to_unit_box(h, Wv, f, up)
        parts[nom] = (h, s * k)
    a = dup(ob, "SM_Traqueur_Bras")
    Sv, Wv, L, d = arm_frame(P, -1)
    bisect_keep(a, Sv + d * (L * P["coupe_bras"][0]), d)
    bisect_keep(a, Sv + d * (L * P["coupe_bras"][1]), -d)
    keep_nearest(a, Sv + d * (L * 0.55))
    s = to_unit_box(a, Sv, d, Vector((0, 0, 1)))
    parts["SM_Traqueur_Bras"] = (a, s * k)
    body = dup(ob, "SM_Traqueur_Corps")
    n_cut = remove_arms(body, P)
    log.append("corps sans bras : %d sommets retirés" % n_cut)
    E.face_front_and_ground(body, scale=k)
    parts["SM_Traqueur_Corps"] = (body, None)
    log += lower_arms(ob, P, H, zmin)
    kk, size = E.face_front_and_ground(ob, scale=k)
    ob.name = ob.data.name = "SM_Traqueur"
    log.append("SM_Traqueur : %d triangles, taille (x, y, z) = (%.2f, %.2f, %.2f) m" % (E.tri_count(ob), *size))
    for nom, (o, s) in parts.items():
        log.append("%s : %d triangles%s" % (nom, E.tri_count(o), "" if s is None else
                   ", taille réelle avant mise à l'unité (x, y, z) = (%.2f, %.2f, %.2f) m à 14 m" % tuple(s)))
    CACHE.update(dict(ob=ob, parts=parts, log=log))
    return CACHE


def show_only(o):
    """Rendus : seule la pièce en cours est visible."""
    c = CACHE
    for x in [c["ob"]] + [v[0] for v in c["parts"].values()]:
        x.hide_render = x is not o


def build_full(P):
    c = build_all(P)
    print("\n".join(c["log"]))
    show_only(c["ob"])
    return [c["ob"]]


def builder(name):
    def b(P):
        o = build_all(P)["parts"][name][0]
        sc = bpy.context.window.scene if bpy.context.window else bpy.context.scene
        if o.name not in sc.collection.objects:
            sc.collection.objects.link(o)
        show_only(o)
        return [o]
    return b


if __name__ == "__main__":
    C.run_piece("SM_Traqueur", build_full, PARAMS, "SM_Traqueur", views=("face", "cote", "34"))
    for nom in ("SM_Traqueur_Corps", "SM_Traqueur_Bras", "SM_Traqueur_Main_D", "SM_Traqueur_Main_G"):
        C.run_piece(nom, builder(nom), PARAMS, nom, views=("face", "cote", "34"))
