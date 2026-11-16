# -*- coding: utf-8 -*-
"""SK_Traqueur : le traqueur (boss) gréé et animé, géant voûté (retours de playtest du 2026-11-16).

- Modèle Tripo H3.1 (tripo_traqueur_v02.glb) : masse des cheveux retirée et remplacée par 7 mèches modélisées ; orteils
  refaits (5 par pied, griffes) ; bras raccourcis (épaule-poignet × BRAS, main × MAIN) pour que, voûté, le bout des doigts
  arrive au genou ; texture repeinte en 4096 px (module peinture : gris-mauve en aplats, encre, coups de pinceau, visage
  blanc gardé, pupilles rouge sombre) ; griffes claires et leur masque d'émission (M_Traqueur_Mains) gardés.
- Géant d'environ 20 m mis debout puis voûté pour tenir dans 14 m : la pose voûtée est la pose de repos du squelette
  (épaule ≈ 12 m, yeux ≈ 13 m) ; avant vers +Y dans le .blend = +Z dans Unity (conversion d'axes à l'import).
- Squelette « Traqueur » (34 os, métarig Rigify) : colonne, cou, tête, mâchoire déboîtée, clavicules, bras (épaule,
  coude, poignet que le code peut orienter), doigts (2 phalanges groupées) et pouce, jambes et orteils.
- Animations : Traqueur_Repos, _Marche, _Prise, _MainsLevees, _MainPlantee, _Revers, _PassageBas, _Machoire (la
  mâchoire seule, pour un calque). Durées et événements : INTEGRATION-PROPS.md § 4 quater.

Lancer dans Blender (avec l'interface) :
    p = r"<dépôt>/Assets/Art/Source~/Ennemis/scripts/sk_traqueur.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
for _m in ("sm_traqueur", "peinture", "ennemis_common"):
    sys.modules.pop(_m, None)
import sm_traqueur as T
E, C = T.E, T.C
import peinture as W
sys.modules.pop("poids_lisses", None)
import poids_lisses as PL
import bpy, bmesh
import numpy as np
from mathutils import Vector, Quaternion, Matrix, kdtree

FPS = 30
VOXEL = 0.07           # m, copie étanche pour la chaleur (géant de 20 m debout)
SMOOTH_IT = 12         # passes de lissage des poids (raccords chaleur / distance)
SHOULDER_IT = 12       # passes de Taubin sur les épaules
SHOULDER_R = 0.45      # rayon du lissage des épaules, en longueur du bras (épaule-coude)
HELPERS = [n + s for n in ("Bras", "AvantBras", "Main", "Jambe", "Pied") for s in (".L", ".R")]   # épaules, coudes, poignets, genoux, chevilles
P = dict(T.PARAMS)
P.update(
    hauteur_debout=20.0, texture=4096, masque=2048,
    bras=0.42, main=0.55, bras_epaisseur=1.15,
    peau=dict(echelle=0.576, rampe=["#2B262E", "#463E4C", "#62596B", "#7C7286"], doux=0.018,
              ecchymoses_freq=9.0, ecchymoses_seuil=0.66, ecchymose="#3A1E2C", aureole="#5E5A48",
              craquelures_zone=0.62, craquelures_freq=40.0, craquelures_trait=0.06,
              plis=(0.30, 0.62), plis_force=0.9, encre="#141015"),
    pupille="#5A0A0E",
)
S20 = P["hauteur_debout"] / 0.576
Z0 = -0.2876                          # sol du modèle brut
X0 = -0.05                            # centre du bassin (avant-arrière) dans le modèle brut


def to_final(p):
    """Brut (avant +X, gauche +Y) -> final (avant +Y, gauche -X, sol à 0, 20 m debout)."""
    x, y, z = (p[0] - X0) * S20, p[1] * S20, (p[2] - Z0) * S20
    return Vector((-y, x, z))


# ----------------------------------------------------------------------------- cheveux, orteils
def dark_faces(ob, px):
    me = ob.data; S = px.shape[0]
    bm = bmesh.new(); bm.from_mesh(me)
    uvl = bm.loops.layers.uv.active
    dark = set()
    for f in bm.faces:
        u = sum((l[uvl].uv for l in f.loops), Vector((0, 0))) / len(f.loops)
        x = min(max(int(u.x * S), 0), S - 1); y = min(max(int(u.y * S), 0), S - 1)
        if px[y, x, :3].max() < 0.10:
            dark.add(f)
    return bm, dark


def remove_hair(ob, px):
    """Masse des cheveux : composantes de faces noires assez grandes qui montent au-dessus des épaules (le pagne et les
    orbites restent). Trous du crâne rebouchés."""
    bm, dark = dark_faces(ob, px)
    comp = {}; cid = 0; members = []
    for f in dark:
        if f in comp:
            continue
        st = [f]; comp[f] = cid; mem = [f]
        while st:
            g = st.pop()
            for e in g.edges:
                for h in e.link_faces:
                    if h in dark and h not in comp:
                        comp[h] = cid; st.append(h); mem.append(h)
        members.append(mem); cid += 1
    kill = []
    for mem in members:
        zs = [f.calc_center_median().z for f in mem]
        xs = [f.calc_center_median().x for f in mem]
        if len(mem) > 60 and max(zs) > 0.19 and min(xs) < 0.02:
            kill += mem
    bmesh.ops.delete(bm, geom=kill, context="FACES")
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    holes = [e for e in bm.edges if e.is_boundary and (e.verts[0].co.z > 0.12)]
    bmesh.ops.holes_fill(bm, edges=holes, sides=0)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 3])
    bm.to_mesh(ob.data); bm.free()
    return len(kill)


def capsule(bm, a, b, r0, r1, n=6, rings=4, up=Vector((0, 0, 1))):
    """Tube effilé de a vers b (rayons r0 -> r1), fermé, ajouté au bmesh ; renvoie ses faces."""
    a, b = Vector(a), Vector(b)
    d = (b - a).normalized()
    s = d.cross(up)
    if s.length < 1e-6:
        s = d.cross(Vector((1, 0, 0)))
    s.normalize(); u = s.cross(d).normalize() or s.cross(d)
    u = s.cross(d).normalized()
    rows = []
    for k in range(rings + 1):
        t = k / rings
        c = a.lerp(b, t)
        r = r0 + (r1 - r0) * t
        rows.append([bm.verts.new(c + (s * math.cos(2 * math.pi * j / n) + u * math.sin(2 * math.pi * j / n) * 0.8) * r) for j in range(n)])
    faces = []
    for k in range(rings):
        for j in range(n):
            j2 = (j + 1) % n
            faces.append(bm.faces.new((rows[k][j], rows[k][j2], rows[k + 1][j2], rows[k + 1][j])))
    tip = bm.verts.new(b + d * r1 * 1.5)
    for j in range(n):
        faces.append(bm.faces.new((rows[-1][j], rows[-1][(j + 1) % n], tip)))
    base = bm.verts.new(a - d * r0 * 0.3)
    for j in range(n):
        faces.append(bm.faces.new((rows[0][(j + 1) % n], rows[0][j], base)))
    return faces


def remake_toes(ob):
    """Orteils en boule retirés (avant du pied) et remplacés par 5 orteils effilés par pied, avec une griffe ; leurs UV
    pointent vers un texel de peau et un texel de griffe (réglés après la peinture : set_toe_uv)."""
    bm = bmesh.new(); bm.from_mesh(ob.data)
    info = []
    toes, claws = [], []
    for sgn in (-1, 1):
        foot = [f for f in bm.faces if f.calc_center_median().z < Z0 + 0.03 and f.calc_center_median().x > 0.006
                and sgn * f.calc_center_median().y > 0.0 and abs(f.calc_center_median().y) < 0.10]
        if not foot:
            continue
        ys = [f.calc_center_median().y for f in foot]
        xs = [f.calc_center_median().x for f in foot]
        y0, y1 = min(ys), max(ys)
        x_root = min(xs)
        bmesh.ops.delete(bm, geom=foot, context="FACES")
        loose = [v for v in bm.verts if not v.link_faces]
        bmesh.ops.delete(bm, geom=loose, context="VERTS")
        hole = [e for e in bm.edges if e.is_boundary and e.verts[0].co.z < Z0 + 0.04 and sgn * e.verts[0].co.y > 0]
        bmesh.ops.holes_fill(bm, edges=hole, sides=0)
        # 5 orteils : le gros côté intérieur (petit |y|)
        width = y1 - y0
        for k in range(5):
            frac = (k + 0.5) / 5
            yc = (y0 + width * frac) if sgn > 0 else (y1 - width * frac)
            inner = 1 - frac
            L = 0.014 + 0.010 * inner
            r = 0.0040 + 0.0030 * inner
            a = Vector((x_root - 0.004, yc, Z0 + r * 1.3))
            b = Vector((x_root + L, yc + sgn * 0.002 * (k - 2), Z0 + r * 0.8))
            toes += capsule(bm, a, b, r, r * 0.7)
            claws += capsule(bm, b - (b - a).normalized() * r * 0.2, b + (b - a).normalized() * r * 1.6 + Vector((0, 0, -r * 0.6)),
                             r * 0.55, r * 0.08, n=5, rings=2)
        info.append("pied %s : %d faces retirées, largeur %.3f" % ("gauche" if sgn > 0 else "droit", len(foot), width))
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 3])
    bm.to_mesh(ob.data); bm.free()
    return info


# ----------------------------------------------------------------------------- texture
def repaint(ob, px):
    S = P["texture"]
    pos = E.rasterize(ob, S, lambda c: True)
    ok = ~np.isnan(pos[..., 0])
    rgb = px[..., :3].copy()
    h, s, v = E.hsv(rgb)
    lum = rgb @ np.array([0.3, 0.59, 0.11], np.float32)
    lum_b = W.blur2d(lum, 1)
    crease = np.clip((W.blur2d(lum, 12) - lum_b) / 0.12, 0, 1)
    x, y, z = pos[..., 0], pos[..., 1], pos[..., 2]
    with np.errstate(invalid="ignore"):
        head = ok & (z > 0.20) & (np.abs(y) < 0.06)
        face = head & (x > 0.015) & (v > 0.55) & (s < 0.22)
        teeth = head & (x > 0.02) & (v > 0.5) & (h > 30) & (h < 80) & (s > 0.15)
        mouth = head & (x > 0.02) & (((h > 330) | (h < 20)) & (s > 0.35))
        dark = ok & (v < 0.10)
        hands = ok & (np.abs(y) > 0.22)
        claws = hands & (h > 25) & (h < 90) & (s < 0.32) & (v > 0.33)
    skin = ok & ~(face | teeth | mouth | dark | claws)
    out = rgb.copy()
    SK = dict(P["peau"])
    for k in ("ecchymose", "aureole", "encre"):
        SK[k] = W.hexrgb(SK[k])
    SK["rampe"] = [W.hexrgb(c) for c in SK["rampe"]]
    out[skin] = W.skin(pos[skin], W.norm_lum(lum_b, skin)[skin], crease[skin], SK)
    ink = SK["encre"]
    for m, levels in ((face, 3), (teeth, 2), (mouth, 2), (claws, 2), (dark, 2)):
        if m.any():
            f = W.flat(rgb[m], W.norm_lum(lum_b, m)[m], levels=levels)
            k = W.smoothstep(0.35, 0.7, crease[m])[:, None] * 0.85
            out[m] = f * (1 - k) + ink * k
    base = px.copy(); base[..., :3] = out
    # pupilles rouge sombre (dans la couleur) et masque d'émission ; griffes (masque à part)
    base, em, info = T.pupils(base, P["masque"], pos, P)
    red = W.hexrgb(P["pupille"])
    pm = em[..., 0] > 0.5
    base[pm, :3] = base[pm, :3] * 0.2 + red * 0.8
    pos_h = np.where(hands[..., None], pos, np.nan)
    base, em_g, info_g = T.claws(px, base, pos_h, P)
    base[..., :3] = W.dilate(np.clip(base[..., :3], 0, 1), ok, 48)
    em = np.repeat(W.dilate(em[..., :1], ok, 24), 4, -1); em[..., 3] = 1
    em_g = np.repeat(W.dilate(em_g[..., :1], ok, 24), 4, -1); em_g[..., 3] = 1
    p_base = E.save_png(base, "T_Traqueur_BaseColor")
    E.save_png(T.resize_px(em, P["masque"]), "T_Traqueur_Emission")
    E.save_png(T.resize_px(em_g, P["masque"]), "T_Traqueur_Griffes_Emission")
    zones = "peau %d, visage %d, dents %d, gueule %d, griffes %d, sombre %d" % tuple(int(m.sum()) for m in (skin, face, teeth, mouth, claws, dark))
    # texels de référence pour les UV des orteils (peau moyenne du pied, griffe)
    return p_base, info, info_g, zones


def set_new_uv(ob):
    """Faces ajoutées (orteils, trous rebouchés : UV toutes à 0) : UV d'un texel de peau du mollet ; les petites faces
    (griffes des orteils) sur un texel de griffe de la main."""
    me = ob.data
    uv = me.uv_layers.active.data
    new = [p for p in me.polygons if all(uv[li].uv.length < 1e-9 for li in p.loop_indices)]
    skin_uv = claw_uv = None
    for p in me.polygons:
        c = p.center
        if skin_uv is None and Z0 + 0.11 < c.z < Z0 + 0.13 and 0.02 < abs(c.y) < 0.07 and c.x > -0.05:
            skin_uv = uv[p.loop_indices[0]].uv.copy()
        if claw_uv is None and abs(c.y) > 0.46:
            claw_uv = uv[p.loop_indices[0]].uv.copy()
        if skin_uv is not None and claw_uv is not None:
            break
    return len(new)
    for p in new:
        small = p.area < (0.0035 ** 2) and p.center.z < Z0 + 0.03
        for li in p.loop_indices:
            uv[li].uv = claw_uv if (small and claw_uv is not None) else skin_uv


# ----------------------------------------------------------------------------- bras raccourcis
def arm_weights(V):
    w = np.zeros(len(V))
    for sgn in (-1, 1):
        Sv, Wv, L, d = T.arm_frame(P, sgn)
        w = np.maximum(w, T.arm_params(V, Sv, d, L, sgn)[4])
    return w


def shorten_arms(ob):
    """Bras raccourcis le long de leur axe (épaule-poignet × BRAS), mains réduites (× MAIN), épaisseur × BRAS_EP ;
    renvoie, par côté, (épaule, coude, poignet, bout des doigts, pouce) en coordonnées brutes."""
    V = T.verts(ob.data); out = V.copy(); joints = {}
    for sgn in (-1, 1):
        Sv, Wv, L, d = T.arm_frame(P, sgn)
        along, t, radial, dist, w = T.arm_params(V, Sv, d, L, sgn)
        sa, sh, th = P["bras"], P["main"], P["bras_epaisseur"]
        along_new = np.where(t <= 1.0, along * sa, L * sa + (along - L) * sh)
        rs = np.where(t <= 1.0, np.where(t < 0.2, 1 + (th - 1) * t / 0.2, th), sh)
        new = np.array(Sv[:]) + along_new[:, None] * np.array(d[:]) + radial * rs[:, None]
        out = np.where((w > 1e-4)[:, None], out * (1 - w[:, None]) + new * w[:, None], out)
        hand = (t > 1.0) & (w > 0.5)
        Wn = np.array(Sv[:]) + np.array(d[:]) * L * sa
        tip = out[hand][np.argmax(along_new[hand])]
        thumb = out[hand][np.argmax(out[hand][:, 2])]
        joints[sgn] = (np.array(Sv[:]), np.array(Sv[:]) + np.array(d[:]) * L * sa * 0.5 + np.array((-0.004, 0, 0)), Wn, tip, thumb)
    T.set_verts(ob.data, out)
    return joints


# ----------------------------------------------------------------------------- peau (poids par régions)
def seg_dist(P3, a, b):
    ab = b - a
    t = np.clip(((P3 - a) @ ab) / max(float(ab @ ab), 1e-12), 0, 1)
    return np.linalg.norm(P3 - (a + t[:, None] * ab), axis=1)


def region_allowed(ob, arm_ob, Vraw, wa):
    """Os permis par sommet, classé par région sur le modèle brut (bras, jambe, tête, mâchoire, tronc ; côté par le
    signe de y) : un bras ne tire jamais le torse, une jambe jamais l'autre, la mâchoire jamais le crâne."""
    x, y, z = Vraw.T
    side = np.where(y >= 0, "L", "R")
    arm = wa > 0.3
    head = (z > 0.200) & ~arm
    jaw = head & (x > 0.005) & (z < 0.232)
    leg = (z < 0.03) & ~arm & (np.abs(y) > 0.004)
    names = [b.name for b in arm_ob.data.bones if b.name != "Racine" and "_Aide" not in b.name]
    idx = {n: i for i, n in enumerate(names)}
    allowed = np.zeros((len(Vraw), len(names)), dtype=bool)
    spine = ["Bassin", "Dos_1", "Dos_2", "Poitrine", "Cou", "Epaule.L", "Epaule.R", "Cuisse.L", "Cuisse.R"]
    for n in spine:
        allowed[:, idx[n]] = True
    for s_ in ("L", "R"):
        m = arm & (side == s_)
        allowed[m] = False
        for n in ("Epaule", "Bras", "AvantBras", "Main", "Doigts_1", "Doigts_2", "Pouce_1", "Pouce_2"):
            allowed[m, idx[n + "." + s_]] = True
        allowed[m, idx["Poitrine"]] = True
        m = leg & (side == s_)
        allowed[m] = False
        for n in ("Cuisse", "Jambe", "Pied", "Orteils"):
            allowed[m, idx[n + "." + s_]] = True
        allowed[m, idx["Bassin"]] = True
    allowed[head] = False
    for n in ("Cou", "Tete"):
        allowed[head, idx[n]] = True
    allowed[jaw, idx["Machoire"]] = True
    return names, allowed


def smooth_shoulders(ob, arm_ob, it=SHOULDER_IT):
    """Épaules boursouflées (retours du 2026-11-16) : lissage de Taubin (sans rétrécir) de la géométrie autour de
    chaque épaule, dégressif avec la distance à l'articulation."""
    me = ob.data
    co = np.array([v.co[:] for v in me.vertices])
    f = np.zeros(len(co))
    for s in ("L", "R"):
        b = arm_ob.data.bones["Bras." + s]
        r = (b.tail_local - b.head_local).length * SHOULDER_R
        d = np.linalg.norm(co - np.array(b.head_local[:]), axis=1)
        f = np.maximum(f, np.clip(1 - d / r, 0, 1) ** 1.5)
    edges = np.array([e.vertices[:] for e in me.edges])
    i, j = edges[:, 0], edges[:, 1]
    deg = np.bincount(np.concatenate([i, j]), minlength=len(co)).astype(float)
    for k in range(it * 2):
        acc = np.zeros_like(co)
        np.add.at(acc, i, co[j]); np.add.at(acc, j, co[i])
        lap = np.where(deg[:, None] > 0, acc / np.maximum(deg[:, None], 1) - co, 0)
        lam = 0.5 if k % 2 == 0 else -0.53
        co = co + lam * f[:, None] * lap
    for v, p in zip(me.vertices, co):
        v.co = p
    me.update()
    return int((f > 0).sum())


# ----------------------------------------------------------------------------- squelette (coordonnées brutes)
def bone_table(J):
    raw = [
        ("Racine", (X0, 0, Z0), (X0, 0, Z0 + 0.03), None, False),
        ("Bassin", (X0, 0, 0.02), (-0.055, 0, 0.07), "Racine", False),
        ("Dos_1", (-0.055, 0, 0.07), (-0.055, 0, 0.11), "Bassin", True),
        ("Dos_2", (-0.055, 0, 0.11), (-0.05, 0, 0.15), "Dos_1", True),
        ("Poitrine", (-0.05, 0, 0.15), (-0.045, 0, 0.19), "Dos_2", True),
        ("Cou", (-0.045, 0, 0.19), (-0.03, 0, 0.215), "Poitrine", True),
        ("Tete", (-0.03, 0, 0.215), (-0.012, 0, 0.285), "Cou", True),
        ("Machoire", (0.0, 0, 0.229), (0.046, 0, 0.204), "Tete", False),
    ]
    for side, sgn in (("L", 1), ("R", -1)):
        Sv, E_, Wv, tip, thumb = J[sgn]
        d = (tip - Wv); hl = np.linalg.norm(d); d = d / hl
        K1 = Wv + d * hl * 0.42; K2 = Wv + d * hl * 0.72
        raw += [
            ("Epaule." + side, (-0.045, 0.02 * sgn, 0.18), tuple(Sv), "Poitrine", False),
            ("Bras." + side, tuple(Sv), tuple(E_), "Epaule." + side, True),
            ("AvantBras." + side, tuple(E_), tuple(Wv), "Bras." + side, True),
            ("Main." + side, tuple(Wv), tuple(K1), "AvantBras." + side, True),
            ("Doigts_1." + side, tuple(K1), tuple(K2), "Main." + side, True),
            ("Doigts_2." + side, tuple(K2), tuple(tip), "Doigts_1." + side, True),
            ("Pouce_1." + side, tuple(Wv + (thumb - Wv) * 0.25), tuple(Wv + (thumb - Wv) * 0.62), "Main." + side, False),
            ("Pouce_2." + side, tuple(Wv + (thumb - Wv) * 0.62), tuple(thumb), "Pouce_1." + side, True),
            ("Cuisse." + side, (-0.055, 0.04 * sgn, 0.0), (-0.045, 0.036 * sgn, -0.13), "Bassin", False),
            ("Jambe." + side, (-0.045, 0.036 * sgn, -0.13), (-0.065, 0.044 * sgn, -0.25), "Cuisse." + side, True),
            ("Pied." + side, (-0.065, 0.044 * sgn, -0.25), (0.004, 0.046 * sgn, -0.278), "Jambe." + side, True),
            ("Orteils." + side, (0.004, 0.046 * sgn, -0.278), (0.04, 0.046 * sgn, -0.283), "Pied." + side, True),
        ]
    return [(n, to_final(h), to_final(t), p, c) for n, h, t, p, c in raw]


RIGIFY = {"Bassin": "spines.basic_spine", "Cou": "spines.super_head", "Machoire": "basic.super_copy"}
for _s in ("L", "R"):
    RIGIFY.update({"Epaule." + _s: "basic.super_copy", "Bras." + _s: "limbs.arm", "Doigts_1." + _s: "limbs.super_finger",
                   "Pouce_1." + _s: "limbs.super_finger", "Cuisse." + _s: "limbs.leg"})


def make_armature(table, name="Traqueur"):
    arm = bpy.data.armatures.new(name)
    ob = bpy.data.objects.new(name, arm)
    sc = bpy.context.window.scene if bpy.context.window else bpy.context.scene
    sc.collection.objects.link(ob)
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    vl.objects.active = ob; ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for n, h, t, p, c in table:
        b = arm.edit_bones.new(n); b.head, b.tail = h, t; b.roll = 0.0; eb[n] = b
    for n, h, t, p, c in table:
        if p:
            eb[n].parent = eb[p]; eb[n].use_connect = c
    bpy.ops.object.mode_set(mode="OBJECT")
    for n, ty in RIGIFY.items():
        try:
            ob.pose.bones[n].rigify_type = ty
        except Exception:
            ob.pose.bones[n]["rigify_type"] = ty
    arm.display_type = "STICK"
    return ob


# ----------------------------------------------------------------------------- poses (avant +Y ; mêmes conventions que la goule)
# x : bascule (+ = penche l'avant vers le bas ; un membre pendant part vers l'arrière) ; y : roulis (+ = le haut part vers
# la gauche du personnage) ; z : lacet (+ = tourne vers la gauche).
AXES = {"x": Vector((-1, 0, 0)), "y": Vector((0, -1, 0)), "z": Vector((0, 0, 1))}


def apply_pose(arm_ob, spec, root=(0, 0, 0), only=None):
    for pb in arm_ob.pose.bones:
        if only and pb.name not in only:
            continue
        pb.rotation_mode = "QUATERNION"; pb.rotation_quaternion = Quaternion(); pb.location = (0, 0, 0)
    for name, rots in spec.items():
        pb = arm_ob.pose.bones[name]
        R = pb.bone.matrix_local.to_quaternion()
        q = Quaternion()
        for ax, ang in rots:
            q = Quaternion(AXES[ax], math.radians(ang)) @ q
        pb.rotation_quaternion = R.inverted() @ q @ R
    rb = arm_ob.pose.bones["Racine"]
    rb.location = rb.bone.matrix_local.to_quaternion().inverted() @ Vector((-root[0], -root[1], root[2]))


def add(s, b, ax, a):
    s.setdefault(b, []).append((ax, a))


def both(s, n, ax, a, mirror=1):
    add(s, n + ".L", ax, a); add(s, n + ".R", ax, a * (mirror if ax in ("y", "z") else 1))


def lerp(a, b, u):
    out = {}
    for bone in set(a) | set(b):
        ra, rb = {}, {}
        for ax, v in a.get(bone, []):
            ra[ax] = ra.get(ax, 0) + v
        for ax, v in b.get(bone, []):
            rb[ax] = rb.get(ax, 0) + v
        out[bone] = [(ax, ra.get(ax, 0) * (1 - u) + rb.get(ax, 0) * u) for ax in ("y", "z", "x")]
    return out


def blend(pa, pb, u):
    return lerp(pa[0], pb[0], u), tuple(pa[1][i] * (1 - u) + pb[1][i] * u for i in range(3))


def ss(x):
    x = min(max(x, 0.0), 1.0); return x * x * (3 - 2 * x)


# pose voûtée réglée au banc (2026-11-16) : épaule 12.1 m, yeux 12.3 m, crâne 14.6 m, genou 4.6 m, bout des doigts 4.4 m
HUNCH = dict(cuisse=-50, jambe=72, bassin=26, dos1=18, dos2=14, poitrine=12, cou=-35, tete=-12,
             machoire_x=12, machoire_z=7,
             # bras visés dans le monde (avant +Y) : bras vers le bas un peu en avant, avant-bras plus en avant, main
             bras=(0.10, 0.20, -1.0), avant=(0.0, 0.8, -1.0), main=(0.0, 0.35, -1.0),
             pied=(0.0, 1.0, -0.55), orteils=(0.0, 1.0, -0.08))       # pieds à plat


def hunch_spec(H):
    s = {}
    both(s, "Cuisse", "x", H["cuisse"]); both(s, "Jambe", "x", H["jambe"])
    add(s, "Bassin", "x", H["bassin"]); add(s, "Dos_1", "x", H["dos1"]); add(s, "Dos_2", "x", H["dos2"])
    add(s, "Poitrine", "x", H["poitrine"]); add(s, "Cou", "x", H["cou"]); add(s, "Tete", "x", H["tete"])
    add(s, "Machoire", "x", H["machoire_x"]); add(s, "Machoire", "z", H["machoire_z"])     # mâchoire déboîtée
    return s


def aim(arm_ob, name, target):
    """Oriente l'os vers une direction du monde (rotation la plus courte depuis sa direction posée actuelle)."""
    pb = arm_ob.pose.bones[name]
    m = pb.matrix.copy()
    cur = (m.to_3x3() @ Vector((0, 1, 0))).normalized()
    q = cur.rotation_difference(Vector(target).normalized())
    pb.matrix = Matrix.Translation(m.translation) @ q.to_matrix().to_4x4() @ m.to_3x3().to_4x4()
    bpy.context.view_layer.update()


def pose_hunch(arm_ob, H):
    apply_pose(arm_ob, hunch_spec(H))
    bpy.context.view_layer.update()
    for side, sx in (("L", -1), ("R", 1)):                      # gauche du personnage = -X
        for bone, key in (("Bras", "bras"), ("AvantBras", "avant"), ("Main", "main"), ("Pied", "pied"), ("Orteils", "orteils")):
            t = H[key]
            aim(arm_ob, bone + "." + side, (t[0] * sx, t[1], t[2]))


def apply_rest(arm_ob, meshes, H):
    """La pose voûtée devient la pose de repos : modificateurs appliqués aux maillages, pose appliquée au squelette, sol
    ramené sous les pieds."""
    pose_hunch(arm_ob, H)
    for o in meshes:
        with bpy.context.temp_override(object=o, active_object=o):
            bpy.ops.object.modifier_apply(modifier="Armature")
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    arm_ob.select_set(True); vl.objects.active = arm_ob
    bpy.ops.object.mode_set(mode="POSE")
    bpy.ops.pose.armature_apply(selected=False)
    bpy.ops.object.mode_set(mode="OBJECT")
    zmin = min(min(v.co.z for v in o.data.vertices) for o in meshes)
    for o in meshes:
        o.data.transform(Matrix.Translation((0, 0, -zmin)))
    arm_ob.data.transform(Matrix.Translation((0, 0, -zmin)))
    for o in meshes:
        m = o.modifiers.new("Armature", "ARMATURE"); m.object = arm_ob
    return zmin


def measure(arm_ob, eye_raw_pts, meshes):
    b = arm_ob.data.bones
    out = {"épaule G": b["Bras.L"].head_local.z, "épaule D": b["Bras.R"].head_local.z,
           "genou": b["Jambe.L"].head_local.z, "bout des doigts": b["Doigts_2.L"].tail_local.z,
           "sommet": max(max(v.co.z for v in o.data.vertices) for o in meshes)}
    return out


# ----------------------------------------------------------------------------- mèches de cheveux
def hair_strands(arm_ob, n=7, seed=3):
    """7 mèches effilées (ruban épais) plantées sur l'arrière du crâne, tombant par gravité vers l'arrière et le bas dans
    la pose voûtée ; liées à la tête."""
    rng = np.random.default_rng(seed)
    hb = arm_ob.data.bones["Tete"]
    head, tail = hb.head_local, hb.tail_local
    c = head + (tail - head) * 0.55
    R = (tail - head).length * 0.40
    side, back, up = Vector((1, 0, 0)), Vector((0, -1, 0)), Vector((0, 0, 1))
    me = bpy.data.meshes.new("SK_Traqueur_Cheveux")
    bm = bmesh.new()
    for k in range(n):
        a = math.pi * (0.22 + 0.56 * k / (n - 1))                     # de la gauche à la droite, par l'arrière
        root = c + (side * math.cos(a) * 0.9 + back * math.sin(a) * 1.0 + up * 0.15) * R + back * R * 0.25
        L = 5.5 + rng.uniform(-1.0, 1.5)
        pts = [root]
        dirv = (side * math.cos(a) * 0.45 + back * math.sin(a) * 1.0 - up * 0.1).normalized()
        step = L / 8
        for j in range(8):
            dirv = (dirv * 0.6 - up * 0.4 + Vector(rng.normal(0, 0.05, 3))).normalized()
            pts.append(pts[-1] + dirv * step)
        w0 = 0.42 + rng.uniform(-0.08, 0.12)
        for j in range(8):
            r0 = w0 * (1 - j / 8) ** 0.7 + 0.04
            r1 = w0 * (1 - (j + 1) / 8) ** 0.7 + 0.04
            capsule(bm, pts[j], pts[j + 1], r0, r1, n=6, rings=1, up=up)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("SK_Traqueur_Cheveux", me)
    sc = bpy.context.window.scene if bpy.context.window else bpy.context.scene
    sc.collection.objects.link(ob)
    g = ob.vertex_groups.new(name="Tete"); g.add(list(range(len(me.vertices))), 1.0, "REPLACE")
    ob.parent = arm_ob
    m = ob.modifiers.new("Armature", "ARMATURE"); m.object = arm_ob
    mat = bpy.data.materials.get("M_Traqueur_Cheveux") or bpy.data.materials.new("M_Traqueur_Cheveux")
    mat.diffuse_color = (0.02, 0.018, 0.022, 1)
    me.materials.append(mat)
    for p in me.polygons:
        p.use_smooth = True
    return ob


# ----------------------------------------------------------------------------- animations
def a_repos(t):
    ph = 2 * math.pi * t / 3.0
    s = {}
    add(s, "Poitrine", "x", 2.0 * math.sin(ph)); add(s, "Dos_2", "x", 1.0 * math.sin(ph + 0.3))
    add(s, "Cou", "z", 6 * math.sin(ph)); add(s, "Tete", "y", 3 * math.sin(ph + 1.0))
    both(s, "Bras", "x", 2.5 * math.sin(ph + 0.6)); both(s, "Doigts_1", "x", 5 * math.sin(2 * ph))
    add(s, "Machoire", "z", 3 * math.sin(ph))
    return s, (0, 0, 0.08 * math.sin(ph))


def walk(t, T_, deep=0.0):
    ph = 2 * math.pi * t / T_
    s = {}
    for side, o in (("L", 0.0), ("R", math.pi)):
        p = ph + o
        add(s, "Cuisse." + side, "x", -18 * math.sin(p) - 40 * deep)
        add(s, "Jambe." + side, "x", 26 * max(0.0, math.cos(p)) + 55 * deep)
        add(s, "Pied." + side, "x", -10 * max(0.0, math.cos(p)) + 6 * math.sin(p) - 15 * deep)
        add(s, "Bras." + side, "x", 12 * math.sin(p) - 10 * deep)
        add(s, "AvantBras." + side, "x", -6 - 6 * max(0.0, math.sin(p)))
    add(s, "Bassin", "y", 4 * math.sin(ph)); add(s, "Bassin", "z", 3 * math.sin(ph))
    add(s, "Bassin", "x", 25 * deep); add(s, "Dos_1", "x", 12 * deep); add(s, "Dos_2", "x", 8 * deep)
    add(s, "Poitrine", "z", -4 * math.sin(ph)); add(s, "Cou", "z", 3 * math.sin(ph)); add(s, "Cou", "x", -18 * deep)
    add(s, "Tete", "x", -14 * deep); add(s, "Machoire", "z", 3 * math.sin(ph))
    return s, (0, 0, 0.25 * abs(math.cos(ph)) - 0.12 - 4.2 * deep)


def a_marche(t):
    return walk(t, 2.0)


PASSAGE = 0.7


def a_passage(t):
    return walk(t, 2.4, deep=PASSAGE)


REACH = {"Bras.R": (0.12, 1.0, -0.25), "AvantBras.R": (0.05, 1.0, -0.35), "Main.R": (0.0, 1.0, -0.55)}   # avant +Y, droite +X


def reach_spec(close):
    s = {}
    add(s, "Poitrine", "z", 10); add(s, "Poitrine", "x", 6); add(s, "Machoire", "x", 10)
    add(s, "Doigts_1.R", "x", -12 + 70 * close); add(s, "Doigts_2.R", "x", -8 + 60 * close)
    add(s, "Pouce_1.R", "x", 45 * close); add(s, "Pouce_2.R", "x", 35 * close)
    return s


def a_prise(t):
    """Bras tendu (visé dans le monde, après la pose), griffes ouvertes puis refermées de 0.32 à 0.5 s."""
    u = ss(t / 0.32)
    if t < 0.32:
        s, r = blend(({}, (0, 0, 0)), (reach_spec(0.0), (0, 0, -0.2)), u)
    else:
        s, r = blend((reach_spec(0.0), (0, 0, -0.2)), (reach_spec(1.0), (0, 0, -0.2)), ss((t - 0.32) / 0.18))
    s["_aim"] = (REACH, u)
    return s, r


def a_mains_levees(t):
    up = {}
    both(up, "Bras", "x", -150); add(up, "Bras.L", "y", -20); add(up, "Bras.R", "y", 20)
    both(up, "AvantBras", "x", -20); both(up, "Main", "x", -10); both(up, "Doigts_1", "x", -15); both(up, "Doigts_2", "x", -10)
    add(up, "Poitrine", "x", -10); add(up, "Cou", "x", -10); add(up, "Machoire", "x", 22)
    return blend(({}, (0, 0, 0)), (up, (0, 0, 0.3)), ss(t / 0.2))


def plant_spec():
    s = {}
    add(s, "Bras.R", "x", -40); add(s, "Bras.R", "y", 12); add(s, "AvantBras.R", "x", 20); add(s, "Main.R", "x", -30)
    add(s, "Doigts_1.R", "x", -20); add(s, "Doigts_2.R", "x", -10)
    add(s, "Poitrine", "x", 14); add(s, "Dos_2", "x", 8); add(s, "Poitrine", "z", 8); add(s, "Cou", "x", -12)
    both(s, "Cuisse", "x", -12); both(s, "Jambe", "x", 18); add(s, "Machoire", "x", 16)
    return s, (0, 0, -0.9)


def a_main_plantee(t):
    if t < 0.2:
        return blend(({}, (0, 0, 0)), plant_spec(), ss(t / 0.2))
    if t < 1.7:
        s, r = plant_spec()
        add(s, "Poitrine", "x", 1.5 * math.sin(2 * math.pi * (t - 0.2) / 1.5))
        return s, r
    return blend(plant_spec(), ({}, (0, 0, 0)), ss((t - 1.7) / 0.3))


def a_revers(t):
    a = {}
    add(a, "Bras.L", "x", -60); add(a, "Bras.L", "z", -45); add(a, "AvantBras.L", "x", -25); add(a, "Poitrine", "z", -12)
    b = {}
    add(b, "Bras.L", "x", -55); add(b, "Bras.L", "z", 55); add(b, "Bras.L", "y", 20); add(b, "AvantBras.L", "x", -5)
    add(b, "Poitrine", "z", 14); add(b, "Machoire", "x", 14); both(b, "Doigts_1", "x", -15)
    return blend((a, (0, 0, 0)), (b, (0, 0, 0)), ss(t / 0.2))


def a_machoire(t):
    ph = 2 * math.pi * t / 2.4
    s = {}
    add(s, "Machoire", "z", 12 * math.sin(ph)); add(s, "Machoire", "x", 9 + 8 * math.sin(2 * ph + 0.5))
    return s, (0, 0, 0)


ANIMS = [  # nom, fonction, durée, boucle, os animés (None : tous)
    ("Traqueur_Repos", a_repos, 3.0, True, None),
    ("Traqueur_Marche", a_marche, 2.0, True, None),
    ("Traqueur_Prise", a_prise, 0.5, False, None),
    ("Traqueur_MainsLevees", a_mains_levees, 0.2, False, None),
    ("Traqueur_MainPlantee", a_main_plantee, 2.0, False, None),
    ("Traqueur_Revers", a_revers, 0.2, False, None),
    ("Traqueur_PassageBas", a_passage, 2.4, True, None),
    ("Traqueur_Machoire", a_machoire, 2.4, True, {"Machoire"}),
]


def make_actions(arm_ob):
    arm_ob.animation_data_create()
    acts = []
    for name, fn, dur, loop, only in ANIMS:
        old = bpy.data.actions.get(name)
        if old:
            bpy.data.actions.remove(old)
        act = bpy.data.actions.new(name)
        arm_ob.animation_data.action = act
        n = max(2, int(round(dur * FPS)))
        for f in range(0, n + 1):
            spec, root = fn(f / FPS if not loop or f < n else 0.0)
            aim_t = spec.pop("_aim", None)
            apply_pose(arm_ob, spec, root)
            if aim_t:                                         # os visés dans le monde, mélangés à la pose (u)
                bpy.context.view_layer.update()
                targets, u = aim_t
                for bn, d in targets.items():
                    pb = arm_ob.pose.bones[bn]
                    cur = (pb.matrix.to_3x3() @ Vector((0, 1, 0))).normalized()
                    aim(arm_ob, bn, cur.lerp(Vector(d).normalized(), u))
            for pb in arm_ob.pose.bones:
                if only and pb.name not in only:
                    continue
                pb.keyframe_insert("rotation_quaternion", frame=1 + f)
                pb.keyframe_insert("location", frame=1 + f)
        acts.append(act)
    arm_ob.animation_data.action = acts[0]
    apply_pose(arm_ob, {})
    return acts


def measure_anim(arm_ob, name, f):
    act = bpy.data.actions[name]
    arm_ob.animation_data.action = act
    bpy.context.scene.frame_set(f)
    bpy.context.view_layer.update()
    return max((arm_ob.matrix_world @ pb.tail).z for pb in arm_ob.pose.bones)


def export(objs, path):
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    vl.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_mesh_modifiers=False, mesh_smooth_type="OFF", use_triangles=True, use_tspace=False,
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X", use_armature_deform_only=True,
        armature_nodetype="NULL", bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        path_mode="RELATIVE", embed_textures=False, use_custom_props=False, use_metadata=True)


def run():
    log = []
    sc = C.new_scene("SK_Traqueur"); sc.render.fps = FPS
    ob = E.import_glb(P["glb"])
    bm = bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-7)
    bm.to_mesh(ob.data); bm.free()
    px = E.source_pixels(ob, P["texture"])
    log.append("cheveux retirés : %d faces" % remove_hair(ob, px))
    n_before = len(ob.data.polygons)
    log += remake_toes(ob)
    me = ob.data
    p_base, info, info_g, zones = repaint(ob, px)
    log += ["pupille %s : %s" % kv for kv in info.items()] + [info_g, "zones : " + zones]
    log.append("faces ajoutées (orteils, trous) : %d" % set_new_uv(ob))
    V = T.verts(me)
    eye_idx = int(np.argmin(np.linalg.norm(V - np.array((0.037, 0.012, 0.243)), axis=1)))
    wa = arm_weights(V)
    J = shorten_arms(ob)
    # final : 20 m debout, avant +Y
    M = Matrix.Rotation(math.pi / 2, 4, "Z") @ Matrix.Scale(S20, 4) @ Matrix.Translation((-X0, 0, -Z0))
    me.transform(M)
    E.smooth_shading(ob, 60)
    mat = C.make_material("M_Traqueur", p_base)
    mat_m = C.make_material("M_Traqueur_Mains", p_base)
    me.materials.clear(); me.materials.append(mat); me.materials.append(mat_m)
    for p in me.polygons:
        if np.mean(wa[list(p.vertices)]) > 0.5:
            p.material_index = 1
    ob.name = me.name = "SK_Traqueur"
    table = bone_table(J)
    arm_ob = make_armature(table)
    # 1. poids lissés, debout (pour voûter le modèle sans bosses), masqués par régions
    names, allowed = region_allowed(ob, arm_ob, V, wa)
    _, _, rep = PL.skin_lisse(ob, arm_ob, VOXEL, names=names, allowed=allowed, smooth_it=SMOOTH_IT)
    log.append("debout : " + rep)
    zmin = apply_rest(arm_ob, [ob], HUNCH)
    # 2. épaules lissées, puis poids recalculés sur le modèle voûté (pose de repos), avec os d'aide
    log.append("épaules lissées : %d sommets" % smooth_shoulders(ob, arm_ob))
    made = PL.add_helpers(arm_ob, HELPERS)
    for m in [m for m in ob.modifiers if m.type == "ARMATURE"]:
        ob.modifiers.remove(m)
    _, _, rep = PL.skin_lisse(ob, arm_ob, VOXEL, names=names, allowed=allowed, smooth_it=SMOOTH_IT, helpers=made)
    log.append("voûté : " + rep + " ; %d os d'aide" % len(made))
    hair = hair_strands(arm_ob)
    meas = measure(arm_ob, None, [ob, hair])
    meas["yeux"] = me.vertices[eye_idx].co.z
    log.append("repos voûté : " + ", ".join("%s %.2f m" % kv for kv in meas.items()))
    acts = make_actions(arm_ob)
    log.append("passage bas : sommet des os %.2f m (images 1, 19, 37 : %s)" % (0, ", ".join("%.2f" % measure_anim(arm_ob, "Traqueur_PassageBas", f) for f in (1, 19, 37))))
    log.append("marche : sommet des os %s" % ", ".join("%.2f" % measure_anim(arm_ob, "Traqueur_Marche", f) for f in (1, 16, 31)))
    log.append("mains levées : sommet des os %.2f m" % measure_anim(arm_ob, "Traqueur_MainsLevees", 7))
    arm_ob.animation_data.action = acts[0]; bpy.context.scene.frame_set(1)
    path = os.path.join(C.MODELS_DIR, "SK_Traqueur.fbx")
    export([arm_ob, ob, hair], path)
    sc2 = bpy.context.window.scene if bpy.context.window else bpy.context.scene
    blocks = {sc2} | set(sc2.objects) | {o.data for o in sc2.objects if o.data is not None} | set(acts)
    sc2.use_fake_user = True
    bpy.data.libraries.write(os.path.join(C.SOURCE_DIR, "SK_Traqueur.blend"), blocks, path_remap="RELATIVE_ALL",
                             fake_user=True, compress=True)
    log.append("SK_Traqueur : %d os ; corps %d triangles, mèches %d" % (len(arm_ob.data.bones), E.tri_count(ob), E.tri_count(hair)))
    print("\n".join(log))
    return arm_ob, ob, hair


if __name__ == "__main__":
    run()
