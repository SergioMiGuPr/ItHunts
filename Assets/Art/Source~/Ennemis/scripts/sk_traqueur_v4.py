# -*- coding: utf-8 -*-
"""SK_Traqueur_v4 (version b, 2026-11-21) : assemblage, gréement, poids, bouche, formes du visage et cheveux du traqueur.

Référence d'apparence : le modèle Tripo de l'utilisateur (corps + tête, `Source~/Ennemis/Traqueur_v4/*.glb`).
Règles : aucune déformation (pas de changement de proportions, pas d'étirement) ; la tête est mise à l'échelle UNE fois
(échelle uniforme de la tête seule) pour que la largeur de son cou égale celle du cou du corps ; puis une seule échelle
uniforme sur tout le modèle pour atteindre 14 m.

Étapes (dans Blender 5.0, appelées une à une) :
  importer()      : les deux GLB, transformations appliquées, face tournée vers -Y (rotation de -90° autour de Z)
  mesurer_cous()  : largeur minimale du cou du corps (z 0.63..0.74) et du cou de la tête (z -0.13..0) -> TETE_ECHELLE
  assembler()     : allègement (corps 70 000, tête 32 000 faces), tête à l'échelle TETE_ECHELLE posée sur le cou,
                    crâne du corps retiré (z > 0.655 ; devant : z > 0.638)
  orbites()       : yeux noirs brillants creusés en orbites mates (matériau M_Traqueur_v4_Orbite)
  fendre_bouche() : lèvres séparées le long de la ligne de la bouche (pour ouvrir la bouche sans étirer la peau)
  alleger()       : corps 38 000, tête 15 500 faces
  echelle_14m()   : une seule échelle uniforme (corps + tête) pour 14 m, pieds à z = 0
  bouche()        : intérieur de la bouche (cavité sombre) et deux rangées de dents (M_Traqueur_v4_Dents)
  armature(), poids_corps(), poids_tete(), formes(), cheveux(), lier(), exporter()

Repère Blender : Z en haut, face vers -Y ; côté gauche du personnage = +X (suffixe _G), droit = -X (_D).
"""
import bpy, bmesh, math, os, random
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

ROOT = r"C:/Users/PC/ItHunts/ItHunts-art"
SRC = ROOT + "/Assets/Art/Source~/Ennemis/Traqueur_v4/"
V = Vector

# ------------------------------------------------------------------ valeurs mesurées (repère source, face vers -Y)
COU_CORPS = 0.09552        # largeur minimale du cou du corps (z = 0.674)
COU_TETE = 0.09099         # largeur minimale du cou de la tête (z = -0.066, repère de la tête)
TETE_ECHELLE = COU_CORPS / COU_TETE        # 1.0497 (l'ancienne version utilisait 1.25)
TETE_POS = V((0.0165, 0.012, 0.7433))      # cou de la tête centré sur le cou du corps, cous les plus étroits à la même hauteur
Z_PIEDS, Z_SOMMET = -0.89995, 0.92702      # après assemblage et allègement
S14 = 14.0 / (Z_SOMMET - Z_PIEDS)          # 7.663 : échelle uniforme unique de tout le modèle


def p14(p):
    """Point du repère source assemblé -> repère final (14 m, pieds à z = 0)."""
    return (V(p) + V((0, 0, -Z_PIEDS))) * S14


# repères du visage (repère source) : ligne de la bouche (parabole), yeux, charnière de la mâchoire
BOUCHE_XC, BOUCHE_DEMI, BOUCHE_Z, BOUCHE_TOMBE = 0.0157, 0.035, 0.725, 0.0075
YEUX_SRC = [V((-0.0211, -0.058, 0.8032)), V((0.0525, -0.058, 0.8043))]
CHARNIERE_SRC = V((0.0157, 0.005, 0.768))


def importer():
    out = {}
    for n in ("corps", "tete"):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=SRC + "traqueur_v4_%s_source.glb" % n)
        o = [x for x in bpy.data.objects if x not in before and x.type == "MESH"][0]
        o.name = "Src_" + n
        o.data.transform(Matrix.Rotation(math.radians(-90), 4, "Z") @ o.matrix_world)
        o.matrix_world = Matrix.Identity(4)
        out[n] = o
    return out


def _coords(o):
    a = np.empty(len(o.data.vertices) * 3, np.float32)
    o.data.vertices.foreach_get("co", a)
    return a.reshape(-1, 3)


def mesurer_cous():
    """Largeur (en X) minimale des cous ; renvoie (cou du corps, z), (cou de la tête, z), rapport."""
    def larg(A, z, st=0.004):
        s = A[(np.abs(A[:, 2] - z) < st / 2) & (np.abs(A[:, 0]) < 0.12) & (np.abs(A[:, 1]) < 0.2)]
        return s[:, 0].max() - s[:, 0].min() if len(s) > 20 else 9
    B = _coords(bpy.data.objects["Src_corps"]); H = _coords(bpy.data.objects["Src_tete"])
    b = min((larg(B, z), z) for z in np.arange(0.63, 0.74, 0.004))
    h = min((larg(H, z), z) for z in np.arange(-0.13, 0.0, 0.004))
    return b, h, b[0] / h[0]


def _decimer(o, faces):
    sc = bpy.context.scene
    for x in sc.objects:
        x.select_set(False)
    bpy.context.view_layer.objects.active = o; o.select_set(True)
    m = o.modifiers.new("Dec", "DECIMATE"); m.ratio = faces / len(o.data.polygons); m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier="Dec")


def assembler():
    sc = bpy.context.scene
    objs = {}
    for n, f in (("corps", 70000), ("tete", 32000)):
        src = bpy.data.objects["Src_" + n]
        o = bpy.data.objects.new("Jeu_" + n, src.data.copy()); sc.collection.objects.link(o)
        _decimer(o, f)
        bm = bmesh.new(); bm.from_mesh(o.data); bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.00005); bm.to_mesh(o.data); bm.free()
        objs[n] = o
    t, c = objs["tete"], objs["corps"]
    bm = bmesh.new(); bm.from_mesh(t.data)
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z < -0.13], context="VERTS"); bm.to_mesh(t.data); bm.free()
    t.data.transform(Matrix.Translation(TETE_POS) @ Matrix.Scale(TETE_ECHELLE, 4))
    bm = bmesh.new(); bm.from_mesh(c.data)
    kill = [v for v in bm.verts if abs(v.co.x) < 0.13 and abs(v.co.y) < 0.17 and (v.co.z > 0.655 or (v.co.z > 0.638 and v.co.y < 0))]
    bmesh.ops.delete(bm, geom=kill, context="VERTS"); bm.to_mesh(c.data); bm.free()
    return c, t


def orbites(t):
    """Creuse les deux yeux (billes noires) en orbites profondes, matériau noir mat."""
    mo = bpy.data.materials.get("M_Traqueur_v4_Orbite") or bpy.data.materials.new("M_Traqueur_v4_Orbite")
    mo.use_nodes = True
    b = next(n for n in mo.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (0.003, 0.003, 0.004, 1); b.inputs["Roughness"].default_value = 1.0
    mo.diffuse_color = (0.003, 0.003, 0.004, 1)
    if mo.name not in [m.name for m in t.data.materials]:
        t.data.materials.append(mo)
    mi = [m.name for m in t.data.materials].index(mo.name)
    k = TETE_ECHELLE / 1.25
    r, prof = 0.026 * k, 0.03 * k
    bm = bmesh.new(); bm.from_mesh(t.data)
    for v in bm.verts:
        for c in YEUX_SRC:
            rho = math.hypot(v.co.x - c.x, (v.co.z - c.z) * 1.15)
            if rho < r and v.co.y < c.y + 0.02 * k:
                v.co.y = max(v.co.y, c.y - 0.004 * k) + prof * (1 - (rho / r) ** 2)
    for f in bm.faces:
        cc = f.calc_center_median()
        for c in YEUX_SRC:
            if math.hypot(cc.x - c.x, (cc.z - c.z) * 1.15) < r * 0.9 and cc.y < c.y + 0.04 * k:
                f.material_index = mi
    bm.to_mesh(t.data); bm.free()


def ligne_bouche(x, xc=BOUCHE_XC, demi=BOUCHE_DEMI, z=BOUCHE_Z, tombe=BOUCHE_TOMBE):
    return z - tombe * (min(abs(x - xc), demi) / demi) ** 2


def _ss(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)


def fendre_bouche(t):
    """Coupe la peau le long de la ligne de la bouche (10 plans) et sépare les deux lèvres ; retire ensuite les faces
    internes qui relient encore les deux lèvres (repérées en ouvrant la mâchoire à 25°)."""
    xc, hw = BOUCHE_XC, BOUCHE_DEMI
    bm = bmesh.new(); bm.from_mesh(t.data)
    cut = set()
    for i in range(10):
        x0 = xc - hw + 2 * hw * i / 10; x1 = xc - hw + 2 * hw * (i + 1) / 10
        p0 = V((x0, 0, ligne_bouche(x0))); p1 = V((x1, 0, ligne_bouche(x1)))
        nrm = V((0, 1, 0)).cross((p1 - p0).normalized()).normalized()
        faces = [f for f in bm.faces if (lambda c: x0 - 0.0005 <= c.x <= x1 + 0.0005 and c.y < -0.045 and abs(c.z - ligne_bouche(c.x)) < 0.005)(f.calc_center_median())]
        geom = list(set(faces) | {e for f in faces for e in f.edges} | {v for f in faces for v in f.verts})
        r = bmesh.ops.bisect_plane(bm, geom=geom, dist=1e-6, plane_co=p0, plane_no=nrm)
        for g in r["geom_cut"]:
            if isinstance(g, bmesh.types.BMEdge):
                m = (g.verts[0].co + g.verts[1].co) / 2
                if x0 - 1e-4 <= m.x <= x1 + 1e-4 and m.y < -0.045:
                    cut.add(g)
    bmesh.ops.split_edges(bm, edges=[e for e in cut if e.is_valid])
    bm.to_mesh(t.data); bm.free()
    # faces qui relient les lèvres : étirées quand la mâchoire s'ouvre
    W = poids_machoire(t, repere="src")
    H = CHARNIERE_SRC
    ouvert = [H + Matrix.Rotation(math.radians(25 * W[i]), 3, "X") @ (v.co - H) for i, v in enumerate(t.data.vertices)]
    bm = bmesh.new(); bm.from_mesh(t.data); bm.verts.ensure_lookup_table()
    kill = []
    for f in bm.faces:
        vs = [v.index for v in f.verts]
        l0 = max((bm.verts[vs[i]].co - bm.verts[vs[i - 1]].co).length for i in range(len(vs)))
        l1 = max((ouvert[vs[i]] - ouvert[vs[i - 1]]).length for i in range(len(vs)))
        c = f.calc_center_median()
        if (max(W[j] for j in vs) - min(W[j] for j in vs) > 0.5 or l1 > max(2 * l0, 0.003)) and abs(c.x - xc) < hw + 0.006 and c.z > 0.69 and c.y < 0.0:
            kill.append(f)
    bmesh.ops.delete(bm, geom=kill, context="FACES_ONLY")
    bmesh.ops.delete(bm, geom=[e for e in bm.edges if not e.link_faces], context="EDGES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_edges], context="VERTS")
    bm.to_mesh(t.data); bm.free()


def alleger(c, t):
    _decimer(c, 38000)
    _decimer(t, 15500)


def echelle_14m(c, t):
    M = Matrix.Scale(S14, 4) @ Matrix.Translation((0, 0, -Z_PIEDS))
    for o in (c, t):
        o.data.transform(M)


# ------------------------------------------------------------------ repères en 14 m
def _rep14():
    xc = BOUCHE_XC * S14
    return dict(xc=xc, demi=BOUCHE_DEMI * S14, z=p14((0, 0, BOUCHE_Z)).z, tombe=BOUCHE_TOMBE * S14,
                yeux=[p14(y) for y in YEUX_SRC], charn=p14(CHARNIERE_SRC), r_oeil=0.026 * TETE_ECHELLE / 1.25 * S14)


def poids_machoire(t, repere="14"):
    """Poids de la mâchoire par sommet (0..1) : sous la ligne de la bouche (côté lèvre inférieure pour les sommets
    de la fente), fondu vers les joues au-delà des coins, vers l'oreille à l'arrière et vers le cou sous le menton."""
    if repere == "src":
        k, xc, hw, zf = 1.0, BOUCHE_XC, BOUCHE_DEMI, (lambda x: ligne_bouche(x))
        org = V((0, 0, 0))
    else:
        R = _rep14(); k = S14; xc, hw = R["xc"], R["demi"]
        zf = (lambda x: ligne_bouche(x, R["xc"], R["demi"], R["z"], R["tombe"]))
        org = V((0, 0, -Z_PIEDS * S14))
    def src(p):  # vers le repère source pour les seuils de hauteur
        return (p - org) / k if repere != "src" else p
    bm = bmesh.new(); bm.from_mesh(t.data); bm.verts.ensure_lookup_table()
    W = np.zeros(len(bm.verts))
    for v in bm.verts:
        p = v.co; dz = p.z - zf(p.x); adx = abs(p.x - xc)
        if abs(dz) < 0.004 * k and p.y < -0.04 * k and adx < hw + 0.002 * k and v.link_faces:
            below = sum(f.calc_center_median().z - zf(f.calc_center_median().x) for f in v.link_faces) < 0
        else:
            below = dz < 0
        kc = _ss(hw - 0.006 * k, hw + 0.004 * k, adx)
        wv = (1 - kc) * (1.0 if below else 0.0) + kc * (1 - _ss(-0.016 * k, 0.004 * k, dz))
        q = src(p)
        W[v.index] = wv * (1 - _ss(0.05, 0.085, abs(q.x - BOUCHE_XC))) * (1 - _ss(-0.01, 0.03, q.y)) * _ss(0.645, 0.672, q.z)
    bm.free()
    return W


def bouche(t):
    """Intérieur de la bouche : cavité sombre (normales vers l'intérieur) et deux rangées de dents pointues, ajoutées
    au maillage de la tête (elles suivent la mâchoire et les formes)."""
    R = _rep14(); xc, hw = R["xc"], R["demi"]
    zf = lambda x: ligne_bouche(x, R["xc"], R["demi"], R["z"], R["tombe"])
    A = _coords(t)
    def y_avant(x):
        s = A[(np.abs(A[:, 0] - x) < 0.03) & (np.abs(A[:, 2] - zf(x)) < 0.05) & (A[:, 1] < -0.2)]
        return float(s[:, 1].min()) if len(s) else -0.5
    mats = [m.name for m in t.data.materials]
    md = bpy.data.materials.get("M_Traqueur_v4_Dents") or bpy.data.materials.new("M_Traqueur_v4_Dents")
    md.use_nodes = True
    b = next(n for n in md.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (0.62, 0.57, 0.46, 1); md.diffuse_color = (0.62, 0.57, 0.46, 1)
    if md.name not in mats:
        t.data.materials.append(md)
    mats = [m.name for m in t.data.materials]
    i_orb, i_dent = mats.index("M_Traqueur_v4_Orbite"), mats.index("M_Traqueur_v4_Dents")
    bm = bmesh.new(); bm.from_mesh(t.data)
    # cavité : demi-ellipsoïde derrière les lèvres, ouverte vers l'avant
    yc = y_avant(xc)
    cav = bmesh.ops.create_uvsphere(bm, u_segments=16, v_segments=10, radius=1.0)
    cv = cav["verts"]
    ctr = V((xc, yc + 0.24, R["z"] - 0.06))
    for v in cv:
        v.co = ctr + V((v.co.x * (hw * 1.02), v.co.y * 0.26, v.co.z * 0.24))
    front = [v for v in cv if v.co.y < yc + 0.07]
    bmesh.ops.delete(bm, geom=front, context="VERTS")
    cf = {f for v in cv if v.is_valid for f in v.link_faces}
    for f in cf:
        f.material_index = i_orb
    bmesh.ops.reverse_faces(bm, faces=list(cf))
    # dents
    rnd = random.Random(7)
    def dent(base, tip, w, d):
        a = base + V((-w / 2, 0, 0)); b2 = base + V((w / 2, 0, 0)); c2 = base + V((0, d, 0)); e = base + V((0, -d * 0.4, 0))
        vs = [bm.verts.new(x) for x in (a, e, b2, c2)]; tv = bm.verts.new(tip)
        for j in range(4):
            f = bm.faces.new((vs[j], vs[(j + 1) % 4], tv)); f.material_index = i_dent
        return vs + [tv]
    n_h, n_b = 9, 8
    for j in range(n_h):
        x = xc - hw * 0.82 + 2 * hw * 0.82 * j / (n_h - 1)
        y = y_avant(x) + 0.07; z = zf(x)
        h = rnd.uniform(0.08, 0.12) * (1.0 if 0 < j < n_h - 1 else 0.7)
        dent(V((x, y, z + 0.05)), V((x + rnd.uniform(-0.01, 0.01), y + 0.01, z + 0.05 - h)), 0.05, 0.05)
    for j in range(n_b):
        x = xc - hw * 0.72 + 2 * hw * 0.72 * j / (n_b - 1)
        y = y_avant(x) + 0.09; z = zf(x)
        h = rnd.uniform(0.07, 0.1)
        dent(V((x, y, z - 0.05)), V((x + rnd.uniform(-0.01, 0.01), y + 0.01, z - 0.05 + h)), 0.045, 0.045)
    bm.to_mesh(t.data); bm.free()


# ------------------------------------------------------------------ squelette
# positions de l'ancienne version (14 m, tête à 1.25) : le corps est recalé par K_CORPS (rapport des échelles 14 m),
# la tête (Cou_2, Tete, Machoire) par le changement d'échelle et de place de la tête.
S14_ANCIEN = 14.0 / (0.8125 + 1.25 * 0.17504 - Z_PIEDS)
K_CORPS = S14 / S14_ANCIEN
OS_TETE = {"Cou_2", "Tete", "Machoire"}


def _de_l_ancien(p, tete=False):
    p = V(p)
    if not tete:
        return p * K_CORPS
    v = p / S14_ANCIEN + V((0, 0, Z_PIEDS))                        # repère source ancien
    q = (v - V((0, 0.012, 0.8125))) / 1.25                         # repère de la tête
    return p14(q * TETE_ECHELLE + TETE_POS)


OS_ANCIEN = {
    "Racine": ((0, 0, 0), (0, 0.8, 0), None, False),
    "Bassin": ((0, 0.1, 7.0), (0, 0.12, 7.8), "Racine", True),
    "Colonne_1": ((0, 0.12, 7.8), (0, 0.2, 8.6), "Bassin", True),
    "Colonne_2": ((0, 0.2, 8.6), (0, 0.22, 9.3), "Colonne_1", True),
    "Colonne_3": ((0, 0.22, 9.3), (0, 0.18, 10.0), "Colonne_2", True),
    "Colonne_4": ((0, 0.18, 10.0), (0, 0.12, 10.7), "Colonne_3", True),
    "Cou_1": ((0, 0.12, 10.7), (0, 0.15, 11.35), "Colonne_4", True),
    "Cou_2": ((0, 0.15, 11.35), (0, 0.18, 12.0), "Cou_1", True),
    "Tete": ((0, 0.18, 12.0), (0, 0.1, 13.6), "Cou_2", True),
}
for s, k in (("G", 1), ("D", -1)):
    OS_ANCIEN.update({
        "Clavicule_" + s: ((k * 0.25, 0.15, 10.35), (k * 1.3, 0.2, 10.5), "Colonne_4", True),
        "Bras_" + s: ((k * 1.3, 0.2, 10.5), (k * 3.0, 0.55, 7.85), "Clavicule_" + s, True),
        "AvantBras_" + s: ((k * 3.0, 0.55, 7.85), (k * 4.3, -0.2, 5.75), "Bras_" + s, True),
        "Main_" + s: ((k * 4.3, -0.2, 5.75), (k * 5.0, -0.85, 5.05), "AvantBras_" + s, True),
        "Doigts_" + s: ((k * 5.0, -0.85, 5.05), (k * 5.35, -1.2, 4.3), "Main_" + s, True),
        "Cuisse_" + s: ((k * 0.55, 0.1, 7.2), (k * 0.78, 0.0, 3.8), "Bassin", True),
        "Jambe_" + s: ((k * 0.78, 0.0, 3.8), (k * 1.2, 0.3, 0.6), "Cuisse_" + s, True),
        "Pied_" + s: ((k * 1.2, 0.3, 0.6), (k * 1.22, -0.45, 0.12), "Jambe_" + s, True),
        "Orteils_" + s: ((k * 1.22, -0.45, 0.12), (k * 1.22, -0.95, 0.08), "Pied_" + s, True),
        "Contact_Main_" + s: ((k * 5.2, -1.05, 4.35), (k * 5.2, -1.05, 4.05), "Doigts_" + s, False),
        "Contact_Pied_" + s: ((k * 1.22, -0.35, 0.0), (k * 1.22, -0.35, -0.3), "Pied_" + s, False),
        "IK_Main_" + s: ((k * 4.3, -0.2, 5.75), (k * 4.3, -0.9, 5.75), "Racine", False),
        "IK_Pied_" + s: ((k * 1.2, 0.3, 0.6), (k * 1.2, -0.4, 0.6), "Racine", False),
        "Pole_Coude_" + s: ((k * 3.2, 2.5, 7.8), (k * 3.2, 3.0, 7.8), "Racine", False),
        "Pole_Genou_" + s: ((k * 0.8, -2.5, 3.8), (k * 0.8, -3.0, 3.8), "Racine", False),
    })


def os_v4b():
    R = _rep14()
    out = {}
    for n, (h, t, p, d) in OS_ANCIEN.items():
        tete = n in OS_TETE
        out[n] = (_de_l_ancien(h, tete), _de_l_ancien(t, tete), p, d)
    # pieds : contact au sol exact (z = 0) ; racine au sol
    for s in ("G", "D"):
        h, t, p, d = out["Contact_Pied_" + s]; out["Contact_Pied_" + s] = (V((h.x, h.y, 0.0)), V((t.x, t.y, -0.3)), p, d)
    out["Racine"] = (V((0, 0, 0)), V((0, 0.8, 0)), None, False)
    # mâchoire : de la charnière vers le menton
    ch = R["charn"]
    out["Machoire"] = (ch, V((R["xc"], ch.y - 0.75, R["z"] - 0.55)), "Tete", True)
    return out


# ------------------------------------------------------------------ cheveux
CHAINES = 7          # 1-2 rideau gauche (avant, milieu), 3-4 rideau droit, 5 dos gauche, 6 dos droit, 7 dos milieu


def _cr(pts, k):
    """Catmull-Rom à travers pts, k segments au total."""
    out = []
    n = len(pts) - 1
    for i in range(k + 1):
        t = i / k * n
        j = min(int(t), n - 1); f = t - j
        p0 = pts[max(j - 1, 0)]; p1 = pts[j]; p2 = pts[j + 1]; p3 = pts[min(j + 2, n)]
        out.append(0.5 * ((2 * p1) + (-p0 + p2) * f + (2 * p0 - 5 * p1 + 4 * p2 - p3) * f * f + (-p0 + 3 * p1 - 3 * p2 + p3) * f ** 3))
    return out


def _bvh(objs, xmax=1.3):
    """Peau servant d'appui aux cheveux : tête, cou, épaules et dos (les bras sont exclus, |x| > xmax)."""
    vs, fs = [], []
    for o in objs:
        off = len(vs)
        vs += [o.matrix_world @ v.co for v in o.data.vertices]
        fs += [[off + i for i in p.vertices] for p in o.data.polygons if abs(p.center.x) < xmax and p.center.z > 8.0]
    return BVHTree.FromPolygons(vs, fs)


def _pousser(bvh, p, marge):
    for _ in range(3):
        loc, nrm, idx, d = bvh.find_nearest(p)
        if loc is None:
            break
        if (p - loc).dot(nrm) < 0 or d < marge:
            p = loc + nrm * marge
    return p


def _meches(seed=5):
    """Liste des mèches : (côté, position de la racine sur la raie (y), couche, hauteur de la pointe, chaîne)."""
    rnd = random.Random(seed)
    out = []
    for sd in (1, -1):
        # mèches avant : de la tempe, devant l'épaule, jusqu'à la poitrine (6 par côté)
        for i in range(6):
            yr = -0.42 + 0.4 * (i / 5) + rnd.uniform(-0.02, 0.02)
            bas = 10.2 - 0.5 * (i / 5) + rnd.uniform(-0.25, 0.25)
            out.append((sd, yr, i % 2, bas, 1 if sd > 0 else 3, "avant", rnd.uniform(-1, 1)))
        # mèches latérales : par-dessus l'épaule, dans le dos (10 par côté)
        for i in range(10):
            yr = 0.0 + 0.6 * (i / 9) + rnd.uniform(-0.03, 0.03)
            bas = 9.9 - 0.7 * (i / 9) + rnd.uniform(-0.3, 0.3)
            out.append((sd, yr, i % 2, bas, 2 if sd > 0 else 4, "cote", rnd.uniform(-1, 1)))
        # dos : jusqu'au milieu du dos (8 par côté)
        for i in range(8):
            yr = 0.62 + 0.25 * (i / 7) + rnd.uniform(-0.03, 0.03)
            bas = 9.0 + rnd.uniform(-0.35, 0.3)
            ch = 7 if i >= 6 else (5 if sd > 0 else 6)
            out.append((sd, yr, i % 2, bas, ch, "dos", rnd.uniform(-1, 1)))
    return out


def _chemin(sd, yr, couche, bas, grp, xc, jit=0.0):
    """Points de contrôle d'une mèche (avant projection hors de la peau). jit : écart aléatoire vers la pointe."""
    e = 0.05 + 0.09 * couche
    j = jit * 0.12
    if grp == "avant":
        return [V((xc + 0.02 * sd, yr, 14.02)), V((xc + sd * 0.45, yr + 0.02, 13.85)), V((xc + sd * (0.78 + e), yr * 0.7, 13.2)),
                V((xc + sd * (0.86 + e), yr * 0.5 - 0.05, 12.3)), V((xc + sd * (0.92 + e), -0.35, 11.3)),
                V((xc + sd * (0.82 + e + j), -0.62 + 0.5 * j, 10.6)), V((xc + sd * (0.82 + e + 2 * j), -0.68 + 0.5 * j, bas))]
    if grp == "cote":
        return [V((xc + 0.02 * sd, yr, 14.02)), V((xc + sd * 0.5, yr + 0.05, 13.85)), V((xc + sd * (0.8 + e), yr * 0.8 + 0.1, 13.2)),
                V((xc + sd * (0.85 + e), 0.3 + yr * 0.5, 12.3)), V((xc + sd * (0.98 + e), 0.75 + yr * 0.2, 11.3)),
                V((xc + sd * (0.9 + e + j), 1.05 + 0.5 * j, 10.3)), V((xc + sd * (0.85 + e + 2 * j), 1.1 + 0.5 * j, bas))]
    lat = 0.02 + 0.6 * ((yr - 0.62) / 0.25)
    return [V((xc + 0.02 * sd, yr - 0.1, 14.0)), V((xc + sd * (0.05 + lat * 0.4), yr + 0.25, 13.8)), V((xc + sd * (0.35 + lat * 0.4), 0.95 + e, 13.0)),
            V((xc + sd * (0.3 + lat * 0.5), 1.05 + e, 12.0)), V((xc + sd * (0.25 + lat * 0.6), 1.2 + e, 11.0)),
            V((xc + sd * (0.2 + lat * 0.6 + j), 1.25 + e, 10.0)), V((xc + sd * (0.2 + lat * 0.6 + 2 * j), 1.2 + e, bas))]


def chemins_cheveux(peau):
    """Chemins des mèches (16 points) projetés au-dessus de la peau : 0.05 m + 0.09 m par couche."""
    R = _rep14(); bvh = _bvh(peau)
    out = []
    for (sd, yr, couche, bas, ch, grp, jit) in _meches():
        dos = grp == "dos"
        pts = _cr(_chemin(sd, yr, couche, bas, grp, R["xc"], jit), 15)
        marge = 0.06 + 0.09 * couche
        res = [_pousser(bvh, p, marge) for p in pts]
        for _ in range(3):                    # lissage du chemin (pas de coude), puis de nouveau hors de la peau
            res = [res[0]] + [(res[i - 1] + 2 * res[i] + res[i + 1]) / 4 for i in range(1, len(res) - 1)] + [res[-1]]
            res = [_pousser(bvh, p, marge) for p in res]
        out.append((res, ch, dos, sd, couche))
    return out


def cheveux(arm_ob, peau):
    """Rideau noir épais et lisse : 48 mèches en rubans bombés (4 sommets en travers), pointes effilées, raie au milieu ;
    rideau de chaque côté du visage (visage dégagé de face) et dos jusqu'au milieu du dos ; deux couches décalées."""
    sc = bpy.context.scene
    old = bpy.data.objects.get("SK_Traqueur_v4_Cheveux")
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    me = bpy.data.meshes.new("SK_Traqueur_v4_Cheveux")
    bm = bmesh.new()
    rnd = random.Random(3)
    info = []                                       # par sommet : (chaîne, u le long de la mèche)
    R = _rep14()
    for pts, ch, dos, sd, couche in chemins_cheveux(peau):
        n = len(pts)
        L = [0.0]
        for j in range(1, n):
            L.append(L[-1] + (pts[j] - pts[j - 1]).length)
        larg = rnd.uniform(0.55, 0.8)
        rows = []
        for j, p in enumerate(pts):
            t = (pts[min(j + 1, n - 1)] - pts[max(j - 1, 0)]).normalized()
            c = V((R["xc"], 0.15, p.z)) if p.z < 13.3 else V((R["xc"], 0.1, 13.2))
            out = (p - c); out = (out - t * out.dot(t)).normalized()
            b = out.cross(t).normalized()
            u = L[j] / L[-1]
            w = larg * (0.45 + 0.55 * min(1.0, u * 4)) * (1 - 0.55 * max(0.0, (u - 0.7) / 0.3))
            row = []
            for a in (-0.5, -0.17, 0.17, 0.5):
                bomb = 0.05 * (1 - (2 * a) ** 2)
                row.append(bm.verts.new(p + b * (a * w) + out * bomb))
                info.append((ch, u))
            rows.append(row)
        for j in range(n - 2):
            for a in range(3):
                bm.faces.new((rows[j][a], rows[j][a + 1], rows[j + 1][a + 1], rows[j + 1][a]))
        # pointe : 2 ou 3 dents effilées depuis l'avant-dernière rangée
        last = rows[n - 2]; tip = pts[-1]
        tdir = ((pts[-1] - pts[-2]).normalized() + V((0, 0, -2))).normalized()     # pointes vers le bas
        dents = 2 + rnd.randint(0, 1)
        for d in range(dents):
            u0, u1 = d / dents, (d + 1) / dents
            pa = last[0].co.lerp(last[3].co, u0); pb = last[0].co.lerp(last[3].co, u1)
            va = bm.verts.new(pa); vb = bm.verts.new(pb)
            vt = bm.verts.new(tip.lerp((pa + pb) / 2, 0.35) + tdir * rnd.uniform(0.3, 0.7))
            info += [(ch, 0.97), (ch, 0.97), (ch, 1.0)]
            bm.faces.new((va, vb, vt))
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new("SK_Traqueur_v4_Cheveux", me)
    sc.collection.objects.link(ob)
    mat = bpy.data.materials.get("M_Traqueur_v4_Cheveux") or bpy.data.materials.new("M_Traqueur_v4_Cheveux")
    mat.use_nodes = True
    b = next(nd for nd in mat.node_tree.nodes if nd.type == "BSDF_PRINCIPLED")
    b.inputs["Base Color"].default_value = (0.01, 0.011, 0.014, 1); b.inputs["Roughness"].default_value = 0.55
    mat.diffuse_color = (0.01, 0.011, 0.014, 1); mat.use_backface_culling = False
    me.materials.append(mat)
    # sommets non reliés (dernière rangée) : retirés
    bm = bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
    keep = [v.index for v in bm.verts if v.link_faces]
    info = [info[i] for i in keep]
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bm.to_mesh(me); bm.free()
    # poids : Tete sur le crâne (u < 0.22), puis les 4 os de la chaîne avec fondu
    gt = ob.vertex_groups.new(name="Tete")
    groups = {}
    for i, (ch, u) in enumerate(info):
        if u < 0.18:
            gt.add([i], 1.0, "REPLACE"); continue
        x = (u - 0.18) / 0.82 * 4          # 0..4 le long des 4 os
        j = min(int(x), 3); f = x - j
        names = ["Cheveux_%d_%d" % (ch, k + 1) for k in range(4)]
        for nm in names:
            if nm not in groups:
                groups[nm] = ob.vertex_groups.new(name=nm)
        wa = 1.0 - max(0.0, f - 0.5) if j < 3 else 1.0
        groups[names[j]].add([i], wa, "REPLACE")
        if j < 3 and f > 0.5:
            groups[names[j + 1]].add([i], f - 0.5, "REPLACE")
        if u < 0.3:
            gt.add([i], (0.3 - u) / 0.12, "REPLACE")
    return ob


def os_cheveux(eb, peau):
    """Chaînes de 4 os : moyenne des mèches de chaque chaîne, de u = 0.18 à la pointe."""
    ch_pts = {}
    for pts, ch, dos, sd, couche in chemins_cheveux(peau):
        n = len(pts)
        L = [0.0]
        for j in range(1, n):
            L.append(L[-1] + (pts[j] - pts[j - 1]).length)
        smp = []
        for k in range(5):
            u = 0.18 + 0.82 * k / 4
            tgt = u * L[-1]
            j = max(i for i in range(n) if L[i] <= tgt + 1e-9); j = min(j, n - 2)
            f = (tgt - L[j]) / max(L[j + 1] - L[j], 1e-9)
            smp.append(pts[j].lerp(pts[j + 1], f))
        ch_pts.setdefault(ch, []).append(smp)
    for ch, lst in ch_pts.items():
        avg = [sum((s[k] for s in lst), V()) / len(lst) for k in range(5)]
        prev = eb["Tete"]
        for j in range(4):
            b = eb.new("Cheveux_%d_%d" % (ch, j + 1)); b.head = avg[j]; b.tail = avg[j + 1]; b.parent = prev
            b.use_connect = j > 0; b.use_deform = True
            prev = b


def armature(peau):
    sc = bpy.context.scene
    old = bpy.data.objects.get("Traqueur")
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    arm = bpy.data.armatures.new("Traqueur")
    ob = bpy.data.objects.new("Traqueur", arm)
    sc.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    for o in sc.objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm.edit_bones
    OS = os_v4b()
    for n, (h, t, p, d) in OS.items():
        b = eb.new(n); b.head = V(h); b.tail = V(t); b.use_deform = d
    for n, (h, t, p, d) in OS.items():
        if p:
            eb[n].parent = eb[p]; eb[n].use_connect = False
    os_cheveux(eb, peau)
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def dist_seg(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-9)))
    return (p - (a + ab * t)).length, t


def poids(mesh_ob, arm_ob, os_autorises=None):
    """Poids par distance aux os (deux plus proches, lissés) ; les os d'un côté ne prennent que les sommets de ce côté."""
    bones = [b for b in arm_ob.data.bones if b.use_deform and not b.name.startswith("Cheveux") and b.name != "Machoire"]
    if os_autorises:
        bones = [b for b in bones if b.name in os_autorises]
    mw = arm_ob.matrix_world
    segs = [(b.name, mw @ b.head_local, mw @ b.tail_local) for b in bones]
    mesh_ob.vertex_groups.clear()
    groups = {n: mesh_ob.vertex_groups.new(name=n) for n, _, _ in segs}
    for v in mesh_ob.data.vertices:
        p = mesh_ob.matrix_world @ v.co
        cand = []
        for n, a, b in segs:
            if n.endswith("_G") and p.x < -0.15: continue
            if n.endswith("_D") and p.x > 0.15: continue
            d, t = dist_seg(p, a, b)
            cand.append((d, n))
        cand.sort()
        (d0, n0), (d1, n1) = cand[0], cand[1]
        w0 = 1.0 / max(d0, 1e-3) ** 3; w1 = 1.0 / max(d1, 1e-3) ** 3
        s = w0 + w1
        groups[n0].add([v.index], w0 / s, "REPLACE")
        groups[n1].add([v.index], w1 / s, "ADD")
    return groups


def poids_tete(t, arm_ob):
    groups = poids(t, arm_ob, os_autorises={"Cou_1", "Cou_2", "Tete"})
    W = poids_machoire(t)
    gm = t.vertex_groups.new(name="Machoire")
    for v in t.data.vertices:
        w = float(W[v.index])
        if w <= 0:
            continue
        for g in v.groups:
            t.vertex_groups[g.group].add([v.index], g.weight * (1 - w), "REPLACE")
        gm.add([v.index], w, "REPLACE")
    return W


def formes(t):
    """6 formes du visage, lisibles à distance de jeu : Cri, MachoirePendante, LevresRetroussees, YeuxPlisses,
    YeuxEcarquilles, Clignement."""
    R = _rep14(); xc, hw = R["xc"], R["demi"]
    zf = lambda x: ligne_bouche(x, R["xc"], R["demi"], R["z"], R["tombe"])
    if t.data.shape_keys is None:
        t.shape_key_add(name="Base")
    base = [v.co.copy() for v in t.data.vertices]
    W = poids_machoire(t)
    H = R["charn"]
    # côté de la fente pour les sommets des lèvres
    def jaw(i, p, ang, lat=0.0, roll=0.0):
        w = W[i]
        if w <= 0:
            return p
        Rm = Matrix.Rotation(math.radians(ang * w), 3, "X") @ Matrix.Rotation(math.radians(roll * w), 3, "Y")
        return H + Rm @ (p - H) + V((lat * w, 0, 0))
    def levres(i, p, haut, bas, coin=0.0, recul=0.0):
        adx = abs(p.x - xc)
        if p.y > -0.2 or adx > hw + 0.35:
            return p
        dz = p.z - zf(p.x)
        lat = 1 - _ss(hw * 0.75, hw + 0.3, adx)
        q = p.copy()
        if W[i] < 0.5 and 0 <= dz < 0.35:                        # lèvre supérieure
            k = (1 - _ss(0.0, 0.35, dz)) * lat
            q.z += haut * k; q.y += recul * k
        elif W[i] >= 0.5 and -0.3 < dz < 0:                       # lèvre inférieure
            k = (1 - _ss(0.0, 0.3, -dz)) * lat
            q.z -= bas * k; q.y += recul * k
        q.x += coin * (p.x - xc) * (1 - _ss(0.0, 0.4, abs(dz)))
        return q
    def yeux(p, f):
        for c in R["yeux"]:
            r = R["r_oeil"]
            d = p - c
            rho = math.hypot(d.x, d.z * 1.15)
            if rho < r * 1.9 and p.y < c.y + 0.35:
                w = 1 - _ss(r * 1.1, r * 1.9, rho)
                q = p.copy()
                if f == "plisse":              # paupières qui se serrent : le trou se réduit à une fente
                    q.z = c.z + d.z * (1 - 0.6 * w); q.y -= 0.03 * w
                elif f == "ecarquille":        # orbite agrandie
                    q.x = c.x + d.x * (1 + 0.28 * w); q.z = c.z + d.z * (1 + 0.38 * w); q.y += 0.02 * w
                elif f == "cligne":            # paupière supérieure descend, inférieure remonte : trou fermé
                    if rho < r * 1.05:
                        q.z = c.z - 0.25 * r + d.z * 0.06; q.y = min(p.y, c.y - 0.02)
                    else:
                        q.z = c.z + d.z * (1 - 0.92 * w)
                        if d.z > 0:
                            q.z = max(q.z, c.z - 0.2 * r)
                return q
        return p
    defs = {
        "Cri": lambda i, p: levres(i, jaw(i, p, 44.0, lat=0.12, roll=15.0), 0.09, 0.04, 0.12, 0.03),
        "MachoirePendante": lambda i, p: jaw(i, p, 27.0, lat=0.14, roll=13.0),
        "LevresRetroussees": lambda i, p: levres(i, p, 0.11, 0.08, 0.05, 0.04),
        "YeuxPlisses": lambda i, p: yeux(p, "plisse"),
        "YeuxEcarquilles": lambda i, p: yeux(p, "ecarquille"),
        "Clignement": lambda i, p: yeux(p, "cligne"),
    }
    for n, fn in defs.items():
        old = t.data.shape_keys.key_blocks.get(n)
        if old:
            t.shape_key_remove(old)
        sk = t.shape_key_add(name=n, from_mix=False)
        for i, p in enumerate(base):
            sk.data[i].co = fn(i, p.copy())
        sk.value = 0.0                                  # Blender 5 : une nouvelle forme est à 1 par défaut


def lier(mesh_ob, arm_ob):
    mesh_ob.parent = arm_ob
    m = mesh_ob.modifiers.get("Armature") or mesh_ob.modifiers.new("Armature", "ARMATURE")
    m.object = arm_ob


def exporter(arm_ob, objs, path, anim=False):
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    arm_ob.select_set(True)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=False, add_leaf_bones=False, use_armature_deform_only=False,
                             bake_anim=anim, mesh_smooth_type="FACE", use_tspace=True, path_mode="RELATIVE")
