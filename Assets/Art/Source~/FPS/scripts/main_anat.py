# -*- coding: utf-8 -*-
"""Main anatomique des bras FPS (v2, 2026-11-17) : base CC0 (maillage de base MakeHuman, avant-bras et main droite,
Assets/Art/Source~/FPS/MakeHuman/), calée dans le repère de la main du jeu, subdivisée puis modelée par script
(phalanges qui s'affinent, jointures, coussinets de la paume et des doigts, tendons du dos de la main, os du poignet).

Repère de la main (Unity, comme sk_brasfps) : origine au centre du poignet, +z vers les doigts (jointure du majeur),
+y dos de la main, pouce côté x négatif (main droite). Les fonctions renvoient des positions dans ce repère ; UH()
les passe dans les coordonnées du modèle Blender (origine au coude), comme sk_brasfps.
"""
import os, json, math
import numpy as np
import bpy, bmesh
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
MH_DIR = os.path.join(HERE, "..", "MakeHuman")
ELBOW = -0.27
FINGER_NAMES = ("Pouce", "Index", "Majeur", "Annulaire", "Auriculaire")
MIDDLE_TIP = 0.165          # poignet -> bout du majeur (m) : taille des mains greybox (jointures à ~0.088, doigts ~0.075)


def UH(x, y, z):
    return Vector((-x, -(z - ELBOW), y))


def HU(v):
    """coordonnées du modèle Blender -> repère de la main"""
    return np.array([-v[0], v[2], -v[1] + ELBOW])


def load_mh():
    V, F = [], []
    for line in open(os.path.join(MH_DIR, "mh_bras_droit.obj"), encoding="utf8"):
        if line.startswith("v "):
            V.append([float(x) for x in line.split()[1:4]])
        elif line.startswith("f "):
            F.append([int(t) - 1 for t in line.split()[1:]])
    J = {k: np.array(v) for k, v in json.load(open(os.path.join(MH_DIR, "mh_bras_droit_reperes.json"))).items()}
    return np.array(V), F, J


def hand_frame(V, J):
    """Repère de la main à partir des repères MakeHuman. Renvoie (centre, axes t, d, f, échelle, miroir)."""
    wr = J["hand"]; mid = J["finger-3-1"]
    f0 = mid - wr; f0 /= np.linalg.norm(f0)
    sl = np.abs((V - wr) @ f0) < 0.12                            # section du poignet
    c = V[sl].mean(0)
    f = mid - c; f /= np.linalg.norm(f)
    t = J["finger-2-1"] - J["finger-5-1"]; t -= f * (t @ f); t /= np.linalg.norm(t)   # vers le pouce
    d = np.cross(f, t)                                           # base (t, f, d) : t x f = -d -> d = f x t
    mirror = (J["finger-1-4"] - c) @ d > 0                       # le bout du pouce doit être côté paume (d < 0)
    if mirror:
        d = -d
    tip = J["finger-3-4"]
    s = MIDDLE_TIP / ((tip - c) @ f)
    return c, t, d, f, s, mirror


def to_hand(P, frame):
    c, t, d, f, s, _ = frame
    Q = (np.atleast_2d(P) - c)
    return np.stack([-(Q @ t) * s, (Q @ d) * s, (Q @ f) * s], -1)


def joints_hand(J, frame):
    out = {}
    for i, n in enumerate(FINGER_NAMES):
        out[n] = [to_hand(J["finger-%d-%d" % (i + 1, k)], frame)[0] for k in (1, 2, 3, 4)]
    out["wrist"] = np.zeros(3)
    return out


def build_base():
    """Maillage bmesh (repère de la main, en m) de l'avant-bras et de la main, avant-bras prolongé jusqu'au coude."""
    V, F, J = load_mh()
    fr = hand_frame(V, J)
    H = to_hand(V, fr)
    if fr[5]:
        F = [f[::-1] for f in F]
    bm = bmesh.new()
    vs = [bm.verts.new(p) for p in H]
    for f in F:
        bm.faces.new([vs[i] for i in f])
    # garder la plus grande pièce (l'extraction emporte quelques débris du corps)
    bm.faces.ensure_lookup_table()
    seen, best = set(), []
    for f0 in bm.faces:
        if f0 in seen: continue
        comp, stack = [], [f0]; seen.add(f0)
        while stack:
            f = stack.pop(); comp.append(f)
            for e in f.edges:
                for g in e.link_faces:
                    if g not in seen: seen.add(g); stack.append(g)
        if len(comp) > len(best): best = comp
    keep = set(best)
    bmesh.ops.delete(bm, geom=[f for f in bm.faces if f not in keep], context="FACES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.normal_update()
    # faces orientées vers l'extérieur (l'extrait MakeHuman est tourné vers l'intérieur) : contrôle par le volume
    cen = Vector(np.mean([v.co[:] for v in bm.verts], 0))
    out = sum((f.calc_center_median() - cen).dot(f.normal) * f.calc_area() for f in bm.faces)
    if out < 0:
        bmesh.ops.reverse_faces(bm, faces=list(bm.faces))
        bm.normal_update()
    # bord ouvert de l'avant-bras : prolongé jusqu'à z = ELBOW (sous la manche), en 3 anneaux
    bnd = [e for e in bm.edges if e.is_boundary and max(v.co.z for v in e.verts) < -0.08]
    ring = list({v for e in bnd for v in e.verts})
    zc = np.mean([v.co.z for v in ring])
    cen = Vector(np.mean([v.co for v in ring], 0))
    prev = bnd
    for k, z in enumerate(np.linspace(zc, ELBOW - 0.005, 4)[1:]):
        r = bmesh.ops.extrude_edge_only(bm, edges=prev)
        nv = [g for g in r["geom"] if isinstance(g, bmesh.types.BMVert)]
        grow = 1.0 + 0.05 * (k + 1)
        for v in nv:
            off = Vector((v.co.x - cen.x, v.co.y - cen.y, 0)) * grow
            v.co = Vector((cen.x + off.x, cen.y + off.y, z))
        prev = [g for g in r["geom"] if isinstance(g, bmesh.types.BMEdge) and g.is_boundary]
    bm.normal_update()
    return bm, joints_hand(J, fr), fr


# ----------------------------------------------------------------------------- modelage
def seg_closest(p, A, B):
    ab = B - A; t = np.clip(((p - A) @ ab) / (ab @ ab), 0, 1)
    q = A + np.outer(t, ab) if p.ndim == 2 else A + t * ab
    return t, q


def finger_coords(P, JH, name):
    """Pour chaque point : distance à l'axe du doigt, abscisse s (0 = jointure, 1 = bout), phalange k, vecteur radial."""
    pts = JH[name]
    best = np.full(len(P), 1e9); S = np.zeros(len(P)); K = np.zeros(len(P), int); R = np.zeros_like(P); A = np.zeros_like(P)
    lens = [np.linalg.norm(pts[k + 1] - pts[k]) for k in range(3)]
    tot = sum(lens)
    acc = 0.0
    for k in range(3):
        t, q = seg_closest(P, pts[k], pts[k + 1])
        dist = np.linalg.norm(P - q, axis=1)
        m = dist < best
        best[m] = dist[m]; S[m] = (acc + t[m] * lens[k]) / tot; K[m] = k; R[m] = (P - q)[m]
        A[m] = (pts[k + 1] - pts[k]) / lens[k]
        acc += lens[k]
    return best, S, K, R, A


def gauss(x, s):
    return np.exp(-(x / s) ** 2)


def sculpt(bm, JH, style):
    """Déplacements le long des normales et retouches de forme (en m)."""
    bm.verts.ensure_lookup_table(); bm.normal_update()
    P = np.array([v.co[:] for v in bm.verts]); N = np.array([v.normal[:] for v in bm.verts])
    D = np.zeros(len(P))
    up = np.array([0, 1.0, 0])
    newP = P.copy()
    thin = style.get("doigts_fins", 1.0)
    FC = {name: finger_coords(P, JH, name) for name in FINGER_NAMES}
    owner = np.argmin(np.stack([FC[n][0] for n in FINGER_NAMES]), 0)
    for fi, name in enumerate(FINGER_NAMES):
        dist, S, K, R, A = FC[name]
        pts = JH[name]
        mine = owner == fi
        on = mine & (dist < 0.016) & (S > 0.0) & (P[:, 2] > 0.02)
        if name == "Pouce":
            on &= S > 0.30
        # sur le doigt : dos = +y local (perpendiculaire à l'axe), paume = -y
        dor = R @ up / np.maximum(np.linalg.norm(R, axis=1), 1e-9)
        # 1) affinement vers le bout (phalanges qui s'affinent), finesse du personnage
        s0 = 0.30 if name == "Pouce" else 0.0
        u = np.clip((S - s0) / (1 - s0), 0, 1)
        taper = (1.0 - 0.22 * u ** 1.3) * (thin if name != "Pouce" else 0.5 + 0.5 * thin)
        w = on * np.clip((S - s0) / 0.08, 0, 1)                    # pas de rupture à la jointure
        newP[on] = P[on] + R[on] * ((taper[on] - 1.0) * w[on])[:, None]
        # 2) jointures bombées sur le dos, plis nets côté paume
        lens = np.cumsum([0] + [np.linalg.norm(pts[k + 1] - pts[k]) for k in range(3)]); tot = lens[-1]
        amp = (0.0022, 0.0014, 0.0008) if name != "Pouce" else (0.0, 0.0014, 0.0008)
        for k in range(3):
            sj = lens[k] / tot
            along = (S - sj) * tot
            g = gauss(along, 0.0045 if k == 0 else 0.0032) * (dist < 0.02) * mine
            D += amp[k] * g * np.clip(dor, 0, 1) ** 2
            if k > 0:
                D -= 0.00045 * gauss(along, 0.0013) * np.clip(-dor, 0, 1) ** 1.5 * (dist < 0.02) * mine
        # 3) coussinets des phalanges (paume) et pulpe du bout
        for k in range(3):
            sm = (lens[k] + lens[k + 1]) / 2 / tot
            g = gauss((S - sm) * tot, 0.006) * on
            D += (0.0006 if k < 2 else 0.0009) * g * np.clip(-dor, 0, 1) ** 1.5
        # 4) ongle : dessus de la dernière phalange aplati
        nail = on & (K == 2) & (S > 0.86)
        D -= 0.00035 * nail * np.clip(dor, 0, 1) ** 3
    # 5) paume : éminences thénar (pouce) et hypothénar, bourrelet sous les jointures, creux central ; os du poignet
    palmside = np.clip(-N[:, 1], 0, 1) ** 1.5
    th = (JH["Pouce"][0] * 0.55 + JH["Pouce"][1] * 0.45) + np.array([0.004, -0.006, 0.0])
    D += 0.0032 * gauss(np.linalg.norm(P - th, axis=1), 0.016) * palmside
    hy = np.array([JH["Auriculaire"][0][0] * 0.85, -0.008, 0.030])
    D += 0.0022 * gauss(np.linalg.norm((P - hy) * [1, 1, 0.6], axis=1), 0.014) * palmside
    for name in FINGER_NAMES[1:]:
        m = JH[name][0] + np.array([0, -0.006, -0.008])
        D += 0.0011 * gauss(np.linalg.norm(P - m, axis=1), 0.008) * palmside
    pc = np.array([0.004, -0.012, 0.045])
    D -= 0.0018 * gauss(np.linalg.norm((P - pc) * [1, 1, 0.8], axis=1), 0.016) * palmside
    # 6) tendons du dos de la main : crêtes du poignet vers chaque jointure
    dorside = np.clip(N[:, 1], 0, 1) ** 2
    for name in FINGER_NAMES[1:]:
        A0 = np.array([JH[name][0][0] * 0.35, 0.0, 0.012]); B0 = JH[name][0] - np.array([0, 0, 0.008])
        t, q = seg_closest(P, A0, B0)
        dd = np.linalg.norm((P - q) * [1, 0.3, 1], axis=1)
        D += 0.00055 * gauss(dd, 0.0016) * dorside * np.sin(np.pi * t) ** 0.7 * (P[:, 1] > 0)
    # 7) os du poignet (styloïde du cubitus, côté auriculaire, sur le dos)
    us = np.array([0.021, 0.011, -0.006])
    D += 0.0016 * gauss(np.linalg.norm(P - us, axis=1), 0.007)
    newP += N * D[:, None]
    for v, p in zip(bm.verts, newP):
        v.co = Vector(p)
    bm.normal_update()
    return D


def subdivide(bm, levels=1):
    me = bpy.data.meshes.new("_tmp_sub"); bm.to_mesh(me)
    ob = bpy.data.objects.new("_tmp_sub", me); bpy.context.scene.collection.objects.link(ob)
    mod = ob.modifiers.new("S", "SUBSURF"); mod.levels = levels; mod.render_levels = levels
    mod.boundary_smooth = "PRESERVE_CORNERS"
    dg = bpy.context.evaluated_depsgraph_get()
    me2 = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    out = bmesh.new(); out.from_mesh(me2)
    bpy.data.objects.remove(ob); bpy.data.meshes.remove(me); bpy.data.meshes.remove(me2)
    bpy.context.view_layer.update()
    return out
