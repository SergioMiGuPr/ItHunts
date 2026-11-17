# -*- coding: utf-8 -*-
"""SK_BrasFPS_<Perso>_D / _G, version 2 (2026-11-17, retour de playtest : « les doigts sont des tubes à anneaux »).

Main anatomique (main_anat.py : base CC0 MakeHuman, subdivisée et modelée par script), réduite au budget, puis habillée
par personnage : gant sans doigts en coque posée sur la main (rebord épais aux ouvertures et à la manchette), griffes,
fourrure de l'avant-bras et touffes, manche à ourlet déchiré, sangle et boucle. Texture peinte (peinture_bras + plis,
coutures, usure, ombrage de creux) dans T_BrasFPS_<Perso>_BaseColor, matériau unique M_BrasFPS_<Perso> (mêmes noms qu'en v1).

Squelette (armature « BrasFPS », mêmes noms que la v1 pour les 17 os d'origine) : Avant_Bras, Poignet,
Paume_Pouce > Pouce_1 à _3, Index_1 à _3, Majeur_1 à _3, Metacarpe_Annulaire > Annulaire_1 à _3,
Metacarpe_Auriculaire > Auriculaire_1 à _3 : 20 os. Les 3 os de paume (nouveaux) creusent la paume et opposent le pouce ;
ils changent la hiérarchie (Pouce_1, Annulaire_1 et Auriculaire_1 n'ont plus Poignet pour parent) : main doit relancer
le pré-calcul des poses (FpsArmsSetup). Poids : chaleur des os (Blender), lissés, 4 os au plus par sommet.

Repères : Socket_Poignet, Socket_Orbe (D), Socket_Artefact et Socket_Artefact_Anneau (G) aux mêmes places qu'en v1 (fixes,
enfants de l'armature) ; nouveau Socket_Flottement (D et G), enfant de l'os Poignet, à 6.5 cm de la paume : point de
flottement qui suit la main (l'orbe et l'artefact flottent au-dessus des mains).

Poses : celles de la v1 (mêmes noms) + Pose_MainOuverte et Pose_Griffe ; boucles Boucle_*.

Lancer dans Blender (fenêtre ouverte, pour le calcul des poids) :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sk_brasfps2.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
for m in ("main_anat", "sk_brasfps"):
    sys.modules.pop(m, None)
import main_anat as MA
import sk_brasfps as SB
import numpy as np
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion, kdtree

C, F, PB = SB.C, SB.F, SB.PB
W = PB.W
UH, ELBOW, RX = MA.UH, MA.ELBOW, SB.RX
TEX = 2048
TRI_HAND = 5800                    # budget de la main (peau + gant) après réduction
CUFF_Z = -0.052                    # bout de la manchette du gant (vers le coude)
GLOVE_OFF = 0.0011                 # épaisseur du gant au-dessus de la peau
RIM = 0.0018                       # rebord rentré sous la peau aux ouvertures

STYLE2 = {
    "Renard": dict(SB.STYLE["Renard"], peau="#B87358", peau_ombre="#7A3B2E", griffe="#1A1416", gant="#3E2B25",
                   gant_clair="#5C4136", poil="#B5512C", poil_clair="#D67A45", manche="#5E1F24", lisere="#3A1A1C",
                   rune="#9C3A34", sangle="#2B1E1A", boucle="#9C7A3A", couture="#7A655C",
                   doigts_fins=1.0, gant_doigts=0.42, gant_pouce=0.55, griffe_long=0.0075, avant_bras="poil",
                   manche_z=(-0.275, -0.168)),
}


# ----------------------------------------------------------------------------- géométrie
def decimate(bm, target_tris):
    me = bpy.data.meshes.new("_dec"); bm.to_mesh(me)
    ob = bpy.data.objects.new("_dec", me); bpy.context.scene.collection.objects.link(ob)
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    if tris > target_tris:
        mod = ob.modifiers.new("D", "DECIMATE"); mod.ratio = target_tris / tris; mod.use_collapse_triangulate = False
    dg = bpy.context.evaluated_depsgraph_get()
    me2 = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    out = bmesh.new(); out.from_mesh(me2)
    bpy.data.objects.remove(ob); bpy.data.meshes.remove(me); bpy.data.meshes.remove(me2)
    bpy.context.view_layer.update()
    return out


def classify(P, JH, st):
    """Par point : doigt le plus proche, abscisse S, gant (bool)."""
    FC = {n: MA.finger_coords(P, JH, n) for n in MA.FINGER_NAMES}
    owner = np.argmin(np.stack([FC[n][0] for n in MA.FINGER_NAMES]), 0)
    S = np.choose(owner, [FC[n][1] for n in MA.FINGER_NAMES])
    gl = np.where(owner == 0, S < st["gant_pouce"], S < st["gant_doigts"])
    # la paume (avant les jointures) : S = 0 -> gant ; manchette jusqu'à CUFF_Z
    glove = gl & (P[:, 2] > CUFF_Z)
    return owner, S, glove


def glove_planes(JH, st):
    """Plans des ouvertures du gant : un par doigt (perpendiculaire au doigt, à l'abscisse gant_doigts / gant_pouce)."""
    out = {}
    for name in MA.FINGER_NAMES:
        pts = JH[name]; g = st["gant_pouce"] if name == "Pouce" else st["gant_doigts"]
        lens = [np.linalg.norm(pts[k + 1] - pts[k]) for k in range(3)]; target = g * sum(lens)
        acc = 0.0
        for k in range(3):
            if acc + lens[k] >= target:
                t = (target - acc) / lens[k]; co = pts[k] + t * (pts[k + 1] - pts[k])
                no = (pts[k + 1] - pts[k]) / lens[k]
                break
            acc += lens[k]
        # ouverture légèrement en biais : plus longue côté dos (comme une vraie mitaine)
        no = no + np.array([0, -0.25, 0]); no /= np.linalg.norm(no)
        out[name] = (co, no)
    return out


def cut_glove(bm, JH, st):
    """Tranche le maillage aux ouvertures du gant (doigts, pouce, manchette) : des bords nets, en boucle plane."""
    planes = glove_planes(JH, st)
    for name, (co, no) in planes.items():
        bm.faces.index_update(); bm.faces.ensure_lookup_table()
        C_ = np.array([f.calc_center_median()[:] for f in bm.faces])
        dist, S, K, R, A = MA.finger_coords(C_, JH, name)
        others = np.min(np.stack([MA.finger_coords(C_, JH, n)[0] for n in MA.FINGER_NAMES if n != name]), 0)
        sel = [f for f in bm.faces if dist[f.index] < others[f.index] and np.linalg.norm(C_[f.index] - co) < 0.022]
        geom = list({x for f in sel for x in list(f.verts) + list(f.edges)}) + sel
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=Vector(co), plane_no=Vector(no), dist=1e-5)
    bm.faces.index_update(); bm.faces.ensure_lookup_table()
    sel = [f for f in bm.faces if abs(f.calc_center_median().z - CUFF_Z) < 0.02]
    geom = list({x for f in sel for x in list(f.verts) + list(f.edges)}) + sel
    bmesh.ops.bisect_plane(bm, geom=geom, plane_co=Vector((0, 0, CUFF_Z)), plane_no=Vector((0, 0, 1)), dist=1e-5)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
    bm.faces.index_update(); bm.verts.index_update(); bm.edges.index_update()
    bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table(); bm.normal_update()
    # gant : au-delà de la manchette, sauf la peau d'un doigt (à moins de 13 mm de son axe, hors paume) passé son plan
    C_ = np.array([f.calc_center_median()[:] for f in bm.faces])
    g = C_[:, 2] > CUFF_Z
    for name in MA.FINGER_NAMES:
        co, no = planes[name]
        dist, S, K, R, A = MA.finger_coords(C_, JH, name)
        g &= ~((dist < 0.013) & (S > 0.02) & ((C_ - co) @ no > 0))
    return g


def ring_depth(bm, inside):
    """Nombre d'anneaux de faces entre chaque sommet « dedans » et le bord de la zone."""
    depth = {v: (0 if not inside[v.index] else 10**6) for v in bm.verts}
    front = [v for v in bm.verts if depth[v] == 0]
    d = 0
    while front:
        nxt = []
        for v in front:
            for e in v.link_edges:
                w = e.other_vert(v)
                if depth[w] > d + 1:
                    depth[w] = d + 1; nxt.append(w)
        front = nxt; d += 1
    return depth


MAT_KEYS = ["peau", "gant", "griffe", "poil", "manche", "lisere", "sangle", "boucle"]
MI = {k: i for i, k in enumerate(MAT_KEYS)}


def build_mesh(perso):
    st = STYLE2[perso]
    bm0, JH, fr = MA.build_base()
    hi = MA.subdivide(bm0, 1); bm0.free()
    MA.sculpt(hi, JH, st)
    bm = decimate(hi, TRI_HAND + 900); hi.free()
    bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table(); bm.normal_update()
    proxy = bpy.data.meshes.new("_proxy_poids"); bm.to_mesh(proxy)
    P = np.array([v.co[:] for v in bm.verts]); N = np.array([v.normal[:] for v in bm.verts])
    # gant : faces du côté main des plans d'ouverture (bords tranchés nets), petits trous bouchés
    gface = cut_glove(bm, JH, st)
    FCt = np.array([f.calc_center_median()[:] for f in bm.faces])
    nb = [[g.index for e in f.edges for g in e.link_faces if g is not f] for f in bm.faces]
    for _ in range(2):
        cnt = np.array([sum(gface[j] for j in n) / max(len(n), 1) for n in nb])
        gface = gface | (cnt > 0.99)
    for f in bm.faces:
        z = FCt[f.index][2]
        f.material_index = MI[st["avant_bras"]] if z < CUFF_Z else MI["peau"]
    # ---- gant : coque des faces « gant », poussée de GLOVE_OFF, rebord rentré aux bords
    gfaces = [f for f in bm.faces if gface[f.index]]
    gset = set(gfaces)
    inside = np.zeros(len(bm.verts), bool)
    for f in gfaces:
        for v in f.verts: inside[v.index] = True
    for v in bm.verts:                                # un sommet touchant une face hors gant est au bord
        if inside[v.index] and any(g not in gset for g in v.link_faces):
            inside[v.index] = False
    depth = ring_depth(bm, inside)
    dup = bmesh.ops.duplicate(bm, geom=gfaces)
    vmap = dup["vert_map"]
    newf = [g for g in dup["geom"] if isinstance(g, bmesh.types.BMFace)]
    bm.normal_update()
    shell = {v for f in newf for v in f.verts}
    snrm = {v: v.normal.copy() for v in shell}
    for v in shell:
        off = GLOVE_OFF + 0.0012 * float(np.clip((-v.co.z) / 0.05, 0, 1))      # la manchette s'évase un peu
        v.co = v.co + snrm[v] * off
    print("gant : %d sommets de coque, %d faces" % (len(shell), len(newf)))
    for f in newf: f.material_index = MI["gant"]
    bm.normal_update()
    bnd = list({e for f in newf for e in f.edges if e.is_boundary})
    bverts = {v for e in bnd for v in e.verts}
    nrm = {v: v.normal.copy() for v in bverts}
    ext = bmesh.ops.extrude_edge_only(bm, edges=bnd)
    for g in ext["geom"]:
        if isinstance(g, bmesh.types.BMVert):
            src = [e.other_vert(g) for e in g.link_edges if e.other_vert(g) in bverts]
            n = nrm[src[0]] if src else Vector((0, 1, 0))
            g.co = g.co - n * RIM
        elif isinstance(g, bmesh.types.BMFace):
            g.material_index = MI["gant"]
    # peau cachée sous le gant (à plus de 2 anneaux du bord) : supprimée
    hidden = [f for f in gset if all(depth[v] >= 3 for v in f.verts)]
    bmesh.ops.delete(bm, geom=hidden, context="FACES_ONLY")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.normal_update()
    return bm, JH, st, proxy


def add_tube(bm, centers, radii, n, mat, jag=None, closed=False):
    """Tube d'anneaux (repère de la main) ; radii[i] = (rx, ry) ; jag(k, i) décale z."""
    rings = []
    for i, (c, (rx, ry)) in enumerate(zip(centers, radii)):
        ring = []
        for k in range(n):
            a = 2 * math.pi * k / n
            z = c[2] + (jag(k, i) if jag else 0.0)
            ring.append(bm.verts.new((c[0] + rx * math.cos(a), c[1] + ry * math.sin(a), z)))
        rings.append(ring)
    faces = []
    for i in range(len(rings) - 1):
        for k in range(n):
            a, b = rings[i][k], rings[i][(k + 1) % n]
            c2, d = rings[i + 1][(k + 1) % n], rings[i + 1][k]
            f = bm.faces.new((a, b, c2, d)); f.material_index = mat; faces.append(f)
    return rings, faces


def section(P, z, dz=0.006):
    z = max(z, P[:, 2].min() + 0.002)
    m = np.abs(P[:, 2] - z) < dz
    while m.sum() < 8:
        dz *= 1.6; m = np.abs(P[:, 2] - z) < dz
    Q = P[m]
    c = (Q.max(0) + Q.min(0)) / 2
    return np.array([c[0], c[1], z]), (np.ptp(Q[:, 0]) / 2, np.ptp(Q[:, 1]) / 2)


def decor(bm, JH, st):
    """Manche, touffes de fourrure, sangle et boucle de la manchette, griffes."""
    bm.verts.ensure_lookup_table()
    P = np.array([v.co[:] for v in bm.verts if any(f.material_index in (MI["peau"], MI["poil"]) for f in v.link_faces)])
    rng = np.random.default_rng(7)
    # -- manche : de z0 (coude) à z1, ourlet déchiré, bande de runes en bas (texture)
    z0, z1 = st["manche_z"]
    zs = np.linspace(z0, z1, 6)
    cs, rs = [], []
    for z in zs:
        c, (rx, ry) = section(P, min(z, -0.06))
        cs.append(c); rs.append((rx + 0.0085 + 0.002 * (z - z1) / (z0 - z1), ry + 0.0085))
    jagv = rng.uniform(0, 1, 32)
    jag = lambda k, i: (0.0 if i < len(zs) - 1 else -0.004 + 0.009 * jagv[k] ** 1.5)
    rings, faces = add_tube(bm, cs, rs, 32, MI["manche"], jag)
    for f in faces[-32:]: f.material_index = MI["lisere"]           # bande de l'ourlet (runes peintes)
    # retour intérieur de l'ourlet (épaisseur visible)
    last = rings[-1]; c = cs[-1]
    inner = []
    for v in last:
        d = Vector((v.co.x - c[0], v.co.y - c[1], 0)); d.normalize()
        inner.append(bm.verts.new(v.co - d * 0.0025 - Vector((0, 0, 0.012))))
    for k in range(32):
        f = bm.faces.new((last[(k + 1) % 32], last[k], inner[k], inner[(k + 1) % 32])); f.material_index = MI["lisere"]
    inner2 = [bm.verts.new(v.co + Vector((0, 0, -0.0003))) for v in inner]
    last2 = [bm.verts.new(v.co) for v in last]
    for k in range(32):
        f = bm.faces.new((inner2[(k + 1) % 32], inner2[k], last2[k], last2[(k + 1) % 32])); f.material_index = MI["lisere"]
    # -- touffes de fourrure : sous l'ourlet de la manche (vers la main) et au bord de la manchette (vers le coude)
    if st["avant_bras"] == "poil":
        for zr, sgn, cnt, L in ((z1 + 0.002, +1, 18, 0.016), (CUFF_Z - 0.001, -1, 16, 0.012)):
            c, (rx, ry) = section(P, zr)
            for k in range(cnt):
                a = 2 * math.pi * (k + rng.uniform(-0.3, 0.3)) / cnt
                base = Vector((c[0] + (rx + 0.0005) * math.cos(a), c[1] + (ry + 0.0005) * math.sin(a), zr))
                out = Vector((math.cos(a), math.sin(a), 0))
                tang = Vector((-math.sin(a), math.cos(a), 0))
                l = L * rng.uniform(0.6, 1.2)
                tip = base + Vector((0, 0, sgn * l)) + out * (0.0025 + 0.002 * rng.uniform())
                w = 0.0035 * rng.uniform(0.8, 1.2)
                b1, b2 = bm.verts.new(base - tang * w), bm.verts.new(base + tang * w)
                b3 = bm.verts.new(base - out * 0.0015 + Vector((0, 0, -sgn * 0.002)))
                t = bm.verts.new(tip)
                for tri in ((b1, b2, t), (b2, b3, t), (b3, b1, t)):
                    f = bm.faces.new(tri if sgn > 0 else tri[::-1]); f.material_index = MI["poil"]
    # -- sangle et boucle sur la manchette
    c, (rx, ry) = section(P, -0.032)
    rings, faces = add_tube(bm, [c + np.array([0, 0, -0.004]), c + np.array([0, 0, 0.004])],
                            [(rx + GLOVE_OFF + 0.0024, ry + GLOVE_OFF + 0.0024)] * 2, 28, MI["sangle"])
    bc = Vector((c[0] - 0.004, c[1] + ry + GLOVE_OFF + 0.003, c[2]))
    cube = bmesh.ops.create_cube(bm, size=1.0, matrix=Matrix.Translation(bc) @ Matrix.Diagonal((0.010, 0.0022, 0.012, 1)))
    for f in {f for v in cube["verts"] for f in v.link_faces}:
        f.material_index = MI["boucle"]
    # -- griffes : du lit de l'ongle au-delà du bout, recourbées vers la paume
    claws = {}
    up = np.array([0, 1.0, 0])
    for name in MA.FINGER_NAMES:
        pts = JH[name]; T, Dp = pts[3], pts[2]
        d = (T - Dp) / np.linalg.norm(T - Dp)
        u = up - d * (d @ up); u /= np.linalg.norm(u)
        if name == "Pouce":                                          # l'ongle du pouce regarde vers l'extérieur
            u = np.array([-0.7, 0.5, -0.1]) - d * (d @ np.array([-0.7, 0.5, -0.1])); u /= np.linalg.norm(u)
        side = np.cross(d, u)
        # rayon de la dernière phalange (sommets de peau proches de l'axe, côté dos)
        rel = P - Dp; along = rel @ d
        m = (along > 0.4 * np.linalg.norm(T - Dp)) & (along < 0.8 * np.linalg.norm(T - Dp)) & (np.linalg.norm(rel - np.outer(along, d), axis=1) < 0.012)
        r = np.median((rel[m] - np.outer(along[m], d)) @ u) if m.sum() > 4 else 0.005
        L = st["griffe_long"] * (0.75 if name == "Auriculaire" else 1.0)
        root = Dp + d * (0.52 * np.linalg.norm(T - Dp)) + u * (r - 0.0006)
        secs = 6; n = 7; ring_prev = None; vs_all = []
        for i in range(secs):
            t = i / (secs - 1)
            ctr = root + d * (t * (np.linalg.norm(T - Dp) * 0.5 + L)) - u * (0.0045 * t ** 2.2) + u * (0.0012 * math.sin(math.pi * t))
            wdt = 0.0040 * (1 - t) ** 0.8 + 0.0003; hgt = 0.0022 * (1 - t) ** 0.7 + 0.0003
            ring = []
            for k in range(n):
                a = 2 * math.pi * k / n
                q = ctr + side * (wdt * math.cos(a)) + u * (hgt * max(math.sin(a), -0.35))
                ring.append(bm.verts.new(q))
            vs_all += ring
            if ring_prev:
                for k in range(n):
                    f = bm.faces.new((ring_prev[k], ring_prev[(k + 1) % n], ring[(k + 1) % n], ring[k])); f.material_index = MI["griffe"]
            ring_prev = ring
        tip = bm.verts.new(root + d * (np.linalg.norm(T - Dp) * 0.5 + L + 0.002) - u * 0.0050)
        for k in range(n):
            f = bm.faces.new((ring_prev[k], ring_prev[(k + 1) % n], tip)); f.material_index = MI["griffe"]
        vs_all.append(tip)
        claws[name] = vs_all
    bm.normal_update()
    return claws


# ----------------------------------------------------------------------------- squelette
def bones_hand(JH):
    """Os dans le repère de la main : nom -> (tête, queue, parent)."""
    b = {"Avant_Bras": (np.array([0, 0, ELBOW]), np.zeros(3), None),
         "Poignet": (np.zeros(3), JH["Majeur"][0].copy(), "Avant_Bras")}
    for name in ("Index", "Majeur", "Annulaire", "Auriculaire"):
        pts = JH[name]; par = "Poignet"
        if name in ("Annulaire", "Auriculaire"):
            mc = "Metacarpe_" + name
            b[mc] = (pts[0] * np.array([0.55, 1, 0.32]) + np.array([0, 0.002, 0]), pts[0].copy(), "Poignet")
            par = mc
        for k in range(3):
            b["%s_%d" % (name, k + 1)] = (pts[k].copy(), pts[k + 1].copy(), par if k == 0 else "%s_%d" % (name, k))
    pts = JH["Pouce"]
    b["Paume_Pouce"] = (np.array([-0.008, -0.004, 0.006]), pts[0].copy(), "Poignet")
    for k in range(3):
        b["Pouce_%d" % (k + 1)] = (pts[k].copy(), pts[k + 1].copy(), "Paume_Pouce" if k == 0 else "Pouce_%d" % k)
    return b


def to_arm(p):
    return RX @ UH(*p)


# axes de pose (dans le repère de l'armature, main droite) : flexion vers la paume, écart vers le pouce, torsion,
# pouce : flexion à travers la paume, abduction (devant la paume), opposition (os Paume_Pouce)
def pose_axes(B, JH):
    ax = {}
    y = np.array([0, 1.0, 0])
    def V(v): return (RX.to_3x3() @ Vector((-v[0], -v[2], v[1]))).normalized()      # direction, repère main -> armature
    def check(a, d, want):
        a = a.normalized()
        r = Quaternion(a, 0.2) @ d
        return a if (r - d).dot(want) > 0 else -a
    palm = V(-y)
    thumbward = V(np.array([-1.0, 0, 0]))
    for name, (h, t, par) in B.items():
        d = V(t - h)
        dors = V(y - (t - h) / np.linalg.norm(t - h) * (((t - h) / np.linalg.norm(t - h)) @ y))
        e = {}
        e["curl"] = check(dors.cross(d), d, palm)
        e["spread"] = check(dors, d, thumbward)
        e["twist"] = d
        if name.startswith("Pouce") or name == "Paume_Pouce":
            tgt = JH["Auriculaire"][0] + np.array([0, -0.012, -0.01]) - h
            tp = V(tgt); tp = (tp - d * d.dot(tp)).normalized()
            e["tcurl"] = check(d.cross(tp), d, tp)
            e["tabd"] = check(d.cross(palm), d, palm)
            e["topp"] = check(V(np.array([0, 0, 1.0])), d, palm)
        ax[name] = e
    return ax


# ----------------------------------------------------------------------------- poses
FING = ("Index", "Majeur", "Annulaire", "Auriculaire")
SPREAD_SIGN = {"Index": 1.0, "Majeur": 0.25, "Annulaire": -0.45, "Auriculaire": -1.0}


def fingers(c1, c2, c3, spread=0.0, only=None):
    d = {}
    for n in FING:
        if only and n not in only: continue
        d["%s_1" % n] = [("curl", c1), ("spread", spread * SPREAD_SIGN[n])]
        d["%s_2" % n] = [("curl", c2)]
        d["%s_3" % n] = [("curl", c3)]
    return d


def thumb(curl=0.0, abd=0.0, opp=0.0, c1=None):
    return {"Paume_Pouce": [("topp", opp)], "Pouce_1": [("tabd", abd), ("tcurl", c1 if c1 is not None else curl * 0.4)],
            "Pouce_2": [("tcurl", curl)], "Pouce_3": [("tcurl", curl * 0.9)]}


def cup(a):
    return {"Metacarpe_Annulaire": [("curl", a * 0.6)], "Metacarpe_Auriculaire": [("curl", a)]}


merge = SB.merge
PALM_UP = {"Avant_Bras": [("twist", 180)]}
FLOAT = {
    "Pose_Repos": merge(PALM_UP, fingers(10, 16, 9, 7), thumb(curl=6, abd=12, opp=4), cup(4), {"Poignet": [("curl", -14)]}),
    "Pose_OrbePaume": merge(PALM_UP, fingers(5, 10, 7, 11), thumb(curl=3, abd=16, opp=2), cup(6), {"Poignet": [("curl", -18)]}),
    "Pose_Pistolet": merge(PALM_UP, fingers(4, 6, 4, 2, only=("Index", "Majeur")), fingers(30, 42, 26, 3, only=("Annulaire", "Auriculaire")),
                           thumb(curl=4, abd=-6, opp=-6), {"Poignet": [("curl", -12)]}),
    "Pose_PriseAnneau": merge(PALM_UP, fingers(14, 24, 16, 5), thumb(curl=10, abd=14, opp=8), cup(8), {"Poignet": [("curl", -14)]}),
}
POSES = {
    "Pose_Repos": [FLOAT["Pose_Repos"]],
    "Pose_OrbePaume": [FLOAT["Pose_OrbePaume"]],
    "Pose_Pistolet": [FLOAT["Pose_Pistolet"]],
    "Pose_PriseAnneau": [FLOAT["Pose_PriseAnneau"]],
    "Pose_Traction": [merge(PALM_UP, fingers(-4, 4, 2, 10), thumb(curl=0, abd=8), {"Poignet": [("curl", -14)]})],
    "Pose_Repulsion": [merge(PALM_UP, fingers(-14, -4, 0, 16), thumb(curl=-4, abd=4, opp=-8), cup(-4), {"Poignet": [("curl", -30)]})],
    "Pose_Attraper": [FLOAT["Pose_Repos"],
                      merge(PALM_UP, fingers(-6, 0, 0, 14), thumb(curl=0, abd=10, opp=-4), {"Poignet": [("curl", -14)]}),
                      merge(PALM_UP, fingers(46, 66, 46, -2), thumb(curl=34, abd=20, opp=30), cup(10), {"Poignet": [("curl", -8)]})],
    "Pose_TenirObjet": [merge(PALM_UP, fingers(24, 40, 28, 4), thumb(curl=16, abd=22, opp=24), cup(10), {"Poignet": [("curl", -14)]})],
    "Pose_Lancer": [merge(PALM_UP, fingers(0, 4, 0, 10), thumb(curl=4, abd=6), {"Poignet": [("curl", 14)]})],
    "Pose_Poing": [merge(fingers(76, 100, 58, -2), thumb(curl=60, abd=0, opp=20, c1=30), cup(12))],
    "Pose_MainOuverte": [merge(PALM_UP, fingers(-8, -2, 0, 15), thumb(curl=-6, abd=4, opp=-10), cup(-3), {"Poignet": [("curl", -10)]})],
    "Pose_Griffe": [merge(PALM_UP, fingers(16, 62, 50, 13), thumb(curl=18, abd=30, opp=4), cup(8), {"Poignet": [("curl", -20)]})],
}
FRAMES = SB.FRAMES
FPS_ANIM, LOOP_S, LOOP_STEP = SB.FPS_ANIM, SB.LOOP_S, SB.LOOP_STEP


def loop_spec(base, t):
    w = 2 * math.pi * t / LOOP_S
    extra = {"Avant_Bras": [("curl", -1.4 * math.sin(w)), ("twist", 0.8 * math.sin(w + 0.7))], "Poignet": [("curl", 1.2 * math.sin(w + 1.4))]}
    for n in FING:
        a = 5.0 * math.sin(2 * w + SB.FINGER_PHASE[n])
        extra["%s_1" % n] = [("curl", a)]; extra["%s_2" % n] = [("curl", a * 0.8)]; extra["%s_3" % n] = [("curl", a * 0.5)]
    extra["Pouce_2"] = [("tcurl", 3.0 * math.sin(2 * w + SB.FINGER_PHASE["Pouce"]))]
    return merge(base, extra)


def apply_pose(arm_ob, spec, AXES, mirror):
    for pb in arm_ob.pose.bones:
        pb.rotation_mode = "QUATERNION"; pb.rotation_quaternion = Quaternion()
    # du parent vers les enfants : l'axe d'un os suit la rotation déjà donnée à ses parents
    order = sorted(spec, key=lambda n: len(arm_ob.data.bones[n].parent_recursive))
    for name in order:
        pb = arm_ob.pose.bones[name]
        R = pb.bone.matrix_local.to_quaternion()
        q = Quaternion()
        for axn, ang in spec[name]:
            a = AXES[name][axn].copy()
            if mirror:
                a = SB.mirror_axis(a)
            q = Quaternion(a, math.radians(ang)) @ q
        pb.rotation_quaternion = R.inverted() @ q @ R


def make_actions(arm_ob, AXES, mirror):
    acts = []
    arm_ob.animation_data_create()
    for name, keys in POSES.items():
        act = bpy.data.actions.new(name); arm_ob.animation_data.action = act
        frames = FRAMES.get(name, (1, 2))
        if len(keys) == 1: keys = keys * len(frames)
        for f, spec in zip(frames, keys):
            apply_pose(arm_ob, spec, AXES, mirror)
            for pb in arm_ob.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f); pb.keyframe_insert("location", frame=f)
        acts.append(act)
    n = int(round(LOOP_S * FPS_ANIM))
    for name, base in FLOAT.items():
        act = bpy.data.actions.new(name.replace("Pose_", "Boucle_")); arm_ob.animation_data.action = act
        for f in range(0, n + 1, LOOP_STEP):
            apply_pose(arm_ob, loop_spec(base, f / FPS_ANIM), AXES, mirror)
            for pb in arm_ob.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=1 + f); pb.keyframe_insert("location", frame=1 + f)
        acts.append(act)
    arm_ob.animation_data.action = acts[0]
    return acts


# ----------------------------------------------------------------------------- poids
def heat_weights(mesh_ob, arm_ob, proxy_me, claws_idx):
    """Poids par chaleur des os calculés sur la main nue (proxy_me : une seule surface, avant gant et décors ; échelle x10
    le temps du calcul), lissés, puis reportés sur le maillage final (moyenne des 3 sommets les plus proches) ; griffes :
    dernière phalange ; 4 os au plus par sommet, seuil 0.015, normalisés."""
    vl = bpy.context.view_layer; sc = bpy.context.scene
    prox = bpy.data.objects.new("_proxy_poids", proxy_me); sc.collection.objects.link(prox)
    for o in vl.objects:
        if o is not None: o.select_set(False)
    s0a = arm_ob.scale.copy()
    arm_ob.scale = (10, 10, 10)
    prox.matrix_world = arm_ob.matrix_basis.copy()
    vl.update()
    prox.select_set(True); arm_ob.select_set(True); vl.objects.active = arm_ob
    r = bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    print("poids par chaleur :", r, len(prox.vertex_groups), "groupes,", sum(1 for v in proxy_me.vertices if len(v.groups)), "/", len(proxy_me.vertices), "sommets pondérés ; actif", vl.objects.active.name, "sélection", [o.name for o in vl.objects if o is not None and o.select_get()])
    arm_ob.scale = s0a; vl.update()
    names = [b.name for b in arm_ob.data.bones]
    pv = proxy_me.vertices
    gi = {g.index: g.name for g in prox.vertex_groups}
    Wp = np.zeros((len(pv), len(names)), np.float32)
    for v in pv:
        for g in v.groups:
            Wp[v.index, names.index(gi[g.group])] = g.weight
    adj = [[] for _ in pv]
    for e in proxy_me.edges:
        a, b = e.vertices; adj[a].append(b); adj[b].append(a)
    empty = Wp.sum(1) < 1e-6
    for _ in range(3):                               # lissage (et comble les sommets sans poids)
        Wn = Wp.copy()
        for i in range(len(pv)):
            if adj[i]: Wn[i] = (Wp[i] if not empty[i] else 0) * 0.5 + 0.5 * Wp[adj[i]].mean(0)
        Wp = Wn
    kd = kdtree.KDTree(len(pv))
    for v in pv: kd.insert(v.co, v.index)
    kd.balance()
    me = mesh_ob.data
    Wt = np.zeros((len(me.vertices), len(names)), np.float32)
    for v in me.vertices:
        hits = kd.find_n(v.co, 3)
        ws = np.array([1.0 / max(d, 1e-5) for _, _, d in hits])
        Wt[v.index] = (ws[:, None] * Wp[[j for _, j, _ in hits]]).sum(0) / ws.sum()
    print("poids : proxy", float(Wp.sum()), "report", float(Wt.sum()), "noms", names[:3], "groupes proxy", list(gi.values())[:3])
    for fname, ids in claws_idx.items():
        k = names.index(fname + "_3")
        for i in ids: Wt[i] = 0; Wt[i, k] = 1
    for i in range(len(Wt)):
        row = Wt[i].copy(); idx = np.argsort(-row)[:4]; keep = [j for j in idx if row[j] > 0.015]
        s = sum(row[j] for j in keep) or 1.0
        Wt[i] = 0
        for j in keep: Wt[i, j] = row[j] / s
    vg = mesh_ob.vertex_groups
    for n in names:
        if n not in vg: vg.new(name=n)
    for j, n in enumerate(names):
        for i in np.nonzero(Wt[:, j])[0]:
            vg[n].add([int(i)], float(Wt[i, j]), "REPLACE")
    bpy.data.objects.remove(prox); bpy.data.meshes.remove(proxy_me); vl.update()
    nonzero = (Wt > 0).sum(1)
    return int(nonzero.max()), int((nonzero == 0).sum())


# ----------------------------------------------------------------------------- texture peinte
def raster_attr(me, uv_name, size, attrs):
    """Comme peinture_bras.raster, avec en plus des grandeurs par sommet interpolées (attrs : (nv, k))."""
    me.calc_loop_triangles()
    uv = me.uv_layers[uv_name].data
    k = attrs.shape[1]
    pos = np.full((size, size, 3), np.nan, np.float32)
    mid = np.full((size, size), -1, np.int16)
    att = np.zeros((size, size, k), np.float32)
    co = np.array([v.co[:] for v in me.vertices], np.float32)
    for tri in me.loop_triangles:
        vi = list(tri.vertices)
        Pt = co[vi]; A = attrs[vi]
        U = np.array([uv[l].uv[:] for l in tri.loops], np.float64) * size
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
        inside = (l0 >= -0.02) & (l1 >= -0.02) & (l2 >= -0.02)
        if not inside.any():
            continue
        yy, xx = np.nonzero(inside)
        L = np.stack([l0[yy, xx], l1[yy, xx], l2[yy, xx]], 1)
        pos[y0 + yy, x0 + xx] = L @ Pt
        att[y0 + yy, x0 + xx] = L @ A
        mid[y0 + yy, x0 + xx] = tri.material_index
    return pos, mid, att


def vertex_fields(me):
    """Grandeurs par sommet : creux (> 0) ou bosse (< 0), distance au bord du gant (m, sur le gant)."""
    import heapq
    co = np.array([v.co[:] for v in me.vertices]); nv = len(co)
    nrm = np.array([v.normal[:] for v in me.vertices])
    adj = [[] for _ in range(nv)]
    for e in me.edges:
        a, b = e.vertices; adj[a].append(b); adj[b].append(a)
    cav = np.zeros(nv)
    for i in range(nv):
        if adj[i]:
            cav[i] = (co[adj[i]].mean(0) - co[i]) @ nrm[i]
    for _ in range(2):
        cav = np.array([0.5 * cav[i] + 0.5 * cav[adj[i]].mean() if adj[i] else cav[i] for i in range(nv)])
    s = np.percentile(np.abs(cav), 95) or 1.0
    cav = np.clip(cav / s, -1, 1)
    gl = np.zeros(nv, bool)
    ecount = {}
    for p in me.polygons:
        if p.material_index == MI["gant"]:
            for i in p.vertices: gl[i] = True
            for ek in p.edge_keys: ecount[ek] = ecount.get(ek, 0) + 1
    seeds = {i for ek, c in ecount.items() if c == 1 for i in ek}
    dist = np.full(nv, 1.0)
    h = [(0.0, i) for i in seeds]
    for _, i in h: dist[i] = 0.0
    heapq.heapify(h)
    while h:
        d0, i = heapq.heappop(h)
        if d0 > dist[i]: continue
        for j in adj[i]:
            if not gl[j]: continue
            dj = d0 + float(np.linalg.norm(co[i] - co[j]))
            if dj < dist[j]:
                dist[j] = dj; heapq.heappush(h, (dj, j))
    return np.stack([cav, dist], 1).astype(np.float32)


def runes(u, v):
    """Glyphes de runes (traits) dans une bande : u (0..1 autour), v (0..1 en hauteur) -> masque 0/1."""
    n = 14
    cell = np.clip(np.floor(u * n).astype(int), 0, n - 1); fu = u * n - cell
    rng = np.random.default_rng(5)
    segs = rng.uniform(0.15, 0.85, (n, 3, 4))
    m = np.zeros_like(u)
    for k in range(3):
        a = segs[cell, k]
        ax, ay, bx, by = a[..., 0], a[..., 1], a[..., 2], a[..., 3]
        if k == 0:
            ax = np.full_like(ax, 0.5); bx = np.full_like(bx, 0.5); ay = np.full_like(ay, 0.15); by = np.full_like(by, 0.85)
        abx, aby = bx - ax, by - ay
        t = np.clip(((fu - ax) * abx + (v - ay) * aby) / np.maximum(abx * abx + aby * aby, 1e-6), 0, 1)
        dx, dy = fu - ax - t * abx, v - ay - t * aby
        m = np.maximum(m, (np.hypot(dx * 0.6, dy) < 0.06).astype(float))
    return m * (v > 0.1) * (v < 0.9)


def paint_texture(ob, JH, st, path):
    me = ob.data
    vl = bpy.context.view_layer
    for o in vl.objects: o.select_set(o == ob)
    vl.objects.active = ob
    uvp = me.uv_layers.new(name="Peinture"); me.uv_layers.active = uvp
    bpy.ops.object.mode_set(mode="EDIT"); bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(55), island_margin=0.004)
    bpy.ops.object.mode_set(mode="OBJECT")
    for l in [l for l in me.uv_layers if l.name != "Peinture"]: me.uv_layers.remove(l)
    fields = vertex_fields(me)
    pos, mid, att = raster_attr(me, "Peinture", TEX, fields)
    ok = mid >= 0
    H = pos[ok].astype(np.float64)                 # le maillage est encore dans le repère de la main
    sel = mid[ok]; cav = att[ok][:, 0]; gdist = att[ok][:, 1]
    vals = np.zeros((len(H), 3), np.float32)
    joints = []
    for nm in MA.FINGER_NAMES:
        pts = JH[nm]
        for k in range(1, 3):
            D = (pts[k + 1] - pts[k]) / np.linalg.norm(pts[k + 1] - pts[k])
            joints.append((pts[k], D, 0.0095))
    hexa = lambda k: np.array(W.hexrgb(st[k]))
    ink = np.array([0.07, 0.05, 0.06])
    for i, key in enumerate(MAT_KEYS):
        m = sel == i
        if not m.any():
            continue
        h = H[m]; c = cav[m]
        base = PB.lift(hexa(key)[None], SB.LIFT.get(key, 0.3))[0]
        col = PB.paint(key, h, base, joints if key in ("peau", "gant") else None, None)
        cpos, cneg = np.clip(c, 0, 1)[:, None], np.clip(-c, 0, 1)[:, None]
        if key == "peau":
            # ombre chaude dans les creux, reflet sur les bosses ; ongle plus sombre cerné d'encre
            col = col * (1 - 0.3 * cpos) + hexa("peau_ombre")[None] * 0.3 * cpos + 0.08 * cneg
            for nm in MA.FINGER_NAMES:
                pts = JH[nm]; T, Dp = pts[3], pts[2]
                d = (T - Dp) / np.linalg.norm(T - Dp)
                rel = h - Dp; along = rel @ d; perp = rel - np.outer(along, d)
                up = np.array([0, 1.0, 0]) if nm != "Pouce" else np.array([-0.7, 0.5, -0.1])
                up = up - d * (d @ up); up /= np.linalg.norm(up)
                rad = np.linalg.norm(perp, axis=1)
                cosu = (perp @ up) / np.maximum(rad, 1e-6)
                Ld = np.linalg.norm(T - Dp)
                nail = (along > 0.40 * Ld) & (cosu > 0.5) & (rad < 0.012) & (along < 1.2 * Ld)
                edge = nail & ((along < 0.45 * Ld) | (cosu < 0.58))
                col[nail] = col[nail] * 0.5 + hexa("peau_ombre") * 0.5
                col[edge] = ink
        elif key == "gant":
            g = gdist[m]
            wear = cneg[:, 0] * W.smoothstep(0.45, 0.7, W.fbm(h, 160.0, 81, 2))
            col = col * (1 - 0.3 * cpos) + (hexa("gant_clair")[None] - col) * 0.6 * wear[:, None]
            dash = np.mod((h[:, 0] * 1.3 + h[:, 1] * 0.7 + h[:, 2]) / 0.0035, 1.0) < 0.55
            stitch = (np.abs(g - 0.0026) < 0.00045) & dash
            col[stitch] = hexa("couture")
            rim = g < 0.0008
            col[rim] = col[rim] * 0.5
        elif key == "griffe":
            t = W.smoothstep(0.0, 1.0, W.vnoise(h, 300.0, 91))
            col = col * (0.85 + 0.35 * t[:, None])
        elif key == "poil":
            # mèches peintes le long du bras : trois échelles étirées, traits sombres entre les mèches, pointes claires
            v1 = W.vnoise(h, 260.0, 21, stretch=(1.0, 1.0, 0.08))
            v2 = W.vnoise(h, 900.0, 22, stretch=(1.0, 1.0, 0.12))
            v3 = W.vnoise(h, 2200.0, 24, stretch=(1.0, 1.0, 0.2))
            col = base[None] * (0.62 + 0.25 * v1 + 0.25 * v2 + 0.12 * v3)[:, None]
            dark = W.smoothstep(0.3, 0.15, v2)
            col = col * (1 - 0.4 * dark[:, None])
            hi = W.smoothstep(0.72, 0.85, v3) * W.smoothstep(0.4, 0.7, v1)
            col = col * (1 - 0.5 * hi[:, None]) + hexa("poil_clair")[None] * 0.5 * hi[:, None]
            col = col * (1 - 0.35 * cpos)
        elif key in ("manche", "lisere"):
            col = col * (1 - 0.3 * cpos)
            if key == "lisere":
                ang = (np.arctan2(h[:, 1], h[:, 0]) / (2 * math.pi)) % 1.0
                z1 = st["manche_z"][1]
                v = np.clip((h[:, 2] - (z1 - 0.013)) / 0.012, 0, 1)
                r = runes(ang, v)
                col = col * (1 - r[:, None]) + hexa("rune")[None] * r[:, None]
        vals[m] = np.clip(col, 0, 1)
    out = np.zeros((TEX, TEX, 3), np.float32); out[ok] = vals
    out = W.dilate(out, ok, 12)
    px = np.ones((TEX, TEX, 4), np.float32); px[..., :3] = out
    im = bpy.data.images.new(os.path.basename(path)[:-4], TEX, TEX, alpha=False)
    im.pixels.foreach_set(px.ravel())
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im.filepath_raw = path; im.file_format = "PNG"; im.save(); bpy.data.images.remove(im)


# ----------------------------------------------------------------------------- assemblage
FLOAT_SOCKET = (0.003, -0.065, 0.050)             # repère de la main, côté paume : au-dessus de la paume une fois tournée
HAND_TO_FILE = Matrix(RX) @ Matrix(((-1, 0, 0, 0), (0, 0, -1, ELBOW), (0, 1, 0, 0), (0, 0, 0, 1)))


def build_side(perso, side, paint=True):
    sc = C.new_scene("SK_BrasFPS_%s_%s" % (perso, side)); sc.render.fps = FPS_ANIM
    SB.clear_actions()
    bm, JH, st, proxy = build_mesh(perso)
    nb_body = len(bm.verts)
    claws = decor(bm, JH, st)
    bm.verts.index_update(); bm.verts.ensure_lookup_table()
    claw_idx = {n: [v.index for v in vs] for n, vs in claws.items()}
    cl_all = {i for ids in claw_idx.values() for i in ids}
    deco_idx = [v.index for v in bm.verts if v.index >= nb_body and v.index not in cl_all]
    name = "SK_BrasFPS_%s_%s" % (perso, side)
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    for p in me.polygons: p.use_smooth = True
    mats = [F.flat_material("M_BrasFPS_%s_%s" % (perso, k.capitalize()), st[k]) for k in MAT_KEYS]
    for m in mats: me.materials.append(m)
    ob = bpy.data.objects.new(name, me); sc.collection.objects.link(ob)
    path = os.path.join(SB.TEX_DIR, "T_BrasFPS_%s_BaseColor.png" % perso)
    if paint:
        paint_texture(ob, JH, st, path)
    else:
        me.uv_layers.new(name="Peinture")
    B = bones_hand(JH)
    AXES = pose_axes(B, JH)
    me.transform(HAND_TO_FILE); proxy.transform(HAND_TO_FILE)
    bones = {n: (to_arm(h), to_arm(t), par) for n, (h, t, par) in B.items()}
    mirror = side == "G"
    if mirror:
        me.transform(Matrix.Scale(-1, 4, (1, 0, 0))); me.flip_normals(); me.update()
        proxy.transform(Matrix.Scale(-1, 4, (1, 0, 0))); proxy.flip_normals(); proxy.update()
    bpy.context.view_layer.update()
    arm_ob = SB.make_armature(bones, mirror)
    maxinf = heat_weights(ob, arm_ob, proxy, claw_idx)
    ob.parent = arm_ob; ob.matrix_parent_inverse.identity(); ob.matrix_basis = Matrix.Identity(4)
    if not any(m.type == "ARMATURE" for m in ob.modifiers):
        mod = ob.modifiers.new("Armature", "ARMATURE"); mod.object = arm_ob
    if os.environ.get("BRAS_FLAT") != "1":                     # (diagnostic : garder une couleur par matière)
        mat = C.make_material("M_BrasFPS_%s" % perso, path) if os.path.exists(path) else mats[0]
        me.materials.clear(); me.materials.append(mat)
        for p in me.polygons: p.material_index = 0
    socks = SB.sockets(arm_ob, side)
    other = bpy.data.objects.get("Socket_Flottement")
    if other is not None: other.name = "Socket_Flottement_autre"
    e = bpy.data.objects.new("Socket_Flottement", None); e.name = "Socket_Flottement"
    e.empty_display_type = "ARROWS"; e.empty_display_size = 0.02
    sc.collection.objects.link(e)
    p_arm = RX @ UH(*FLOAT_SOCKET)
    if mirror: p_arm.x = -p_arm.x
    bpy.context.view_layer.update()
    pw = arm_ob.matrix_world @ p_arm
    e.parent = arm_ob; e.parent_type = "BONE"; e.parent_bone = "Poignet"
    bpy.context.view_layer.update()
    # mêmes axes que Socket_Poignet (rotation nulle dans le repère de l'armature)
    e.matrix_world = Matrix.Translation(pw) @ arm_ob.matrix_world.to_quaternion().to_matrix().to_4x4()
    socks.append(e)
    acts = make_actions(arm_ob, AXES, mirror)
    return sc, arm_ob, ob, socks, acts, C.tri_count(ob), maxinf, JH


def run(persos=("Renard",), export=True):
    rep = []
    for perso in persos:
        for side in ("D", "G"):
            sc, arm_ob, ob, socks, acts, tris, maxinf, JH = build_side(perso, side, paint=(side == "D"))
            name = "SK_BrasFPS_%s_%s" % (perso, side)
            line = "%s : %d triangles, %d os, %d actions, %d os max par sommet, %d sommets sans poids" % (name, tris, len(arm_ob.data.bones), len(acts), maxinf[0], maxinf[1])
            if export:
                fbx = os.path.join(SB.MODELS_DIR, name + ".fbx")
                SB.export_skinned([arm_ob, ob] + socks, fbx)
                SB.save_blend(sc, acts, os.path.join(SB.SOURCE_DIR, name + ".blend"))
                line += "\n" + SB.verify(fbx)
            rep.append(line)
    print("\n".join(rep))


if __name__ == "__main__":
    run()
