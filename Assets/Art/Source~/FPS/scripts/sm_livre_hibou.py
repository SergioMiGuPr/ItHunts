# -*- coding: utf-8 -*-
"""SM_Livre_Hibou : livre spectral du hibou (artefact, main gauche, vue FPS), d'après artefact_hibou_livre_v01.

Livre ouvert qui flotte au-dessus de la main gauche, pages tournées vers le joueur : dos vertical au milieu, deux moitiés
en V (ouvertes de 50° au total), blocs de pages bombés, couvertures fines, trois pages libres soulevées (elles frémissent
dans le shader : couleur de sommet R = poids du frémissement, 0 au dos, 1 au bord), trois volutes en rubans qui montent
en spirale autour du livre (uv x le long du ruban, pour le défilement). Modèle à la main, sans génération.

Tailles : 0.25 m de large ouvert, 0.18 m de haut, 0.07 m de profondeur. Pivot au milieu du dos. Repère Unity : +Y en haut,
+Z vers l'avant (les pages regardent -Z, vers le joueur). Objets : SM_Livre_Hibou (couvertures, dos, blocs de pages),
enfants SM_Livre_Hibou_Pages_Libres et SM_Livre_Hibou_Volutes ; repère FX_Livre (centre des pages, pour les liens).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sm_livre_hibou.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("fps_common", None)
import fps_common as F
C = F.C
import bpy, bmesh
from mathutils import Vector, Matrix

P = dict(hauteur=0.175, demi_largeur=0.118, ouverture=25.0, bloc=0.016, bombe=0.010, couverture=0.005,
         deborde=0.004, nu=14, nv=5, pages_libres=(32.0, 58.0, 84.0), volutes=3)


def mat(name, hexa):
    return F.flat_material(name, hexa)


def half(bm, sign, col, uvl):
    """Une moitié : bloc de pages (face avant bombée vers le joueur) + couverture derrière."""
    H, W, t, b = P["hauteur"], P["demi_largeur"], P["bloc"], P["bombe"]
    a = math.radians(P["ouverture"]) * sign
    R = Matrix.Rotation(a, 4, "Z")
    nu, nv = P["nu"], P["nv"]
    def pt(u, v, depth):
        x = sign * u * W
        y = depth
        return R @ Vector((x, y, (v - 0.5) * H))
    front, back = [], []
    for j in range(nv + 1):
        rf, rb = [], []
        for i in range(nu + 1):
            u, v = i / nu, j / nv
            off = b * math.sin(math.pi * min(u * 1.1, 1.0)) ** 0.7 + 0.002   # bombé
            rf.append(bm.verts.new(pt(u, v, t + off)))
            rb.append(bm.verts.new(pt(u, v, 0.0)))
        front.append(rf); back.append(rb)
    faces = []
    def quad(a_, b_, c_, d_, uvs, w):
        f = bm.faces.new((a_, b_, c_, d_) if sign > 0 else (d_, c_, b_, a_))
        f.material_index = 1
        faces.append((f, uvs if sign > 0 else uvs[::-1], w))
    for j in range(nv):
        for i in range(nu):
            uvs = [(i / nu, j / nv), (i / nu + 1 / nu, j / nv), (i / nu + 1 / nu, (j + 1) / nv), (i / nu, (j + 1) / nv)]
            quad(front[j][i], front[j][i + 1], front[j + 1][i + 1], front[j + 1][i], uvs, 0.0)
    # tranches (haut, bas, bord) : pages vues de côté
    for i in range(nu):
        quad(back[0][i + 1], back[0][i], front[0][i], front[0][i + 1], [(0, 0), (1, 0), (1, 1), (0, 1)], 0.0)
        quad(front[nv][i + 1], front[nv][i], back[nv][i], back[nv][i + 1], [(0, 0), (1, 0), (1, 1), (0, 1)], 0.0)
    for j in range(nv):
        quad(back[j][nu], back[j + 1][nu], front[j + 1][nu], front[j][nu], [(0, 0), (1, 0), (1, 1), (0, 1)], 0.0)
    for f, uvs, w in faces:
        for loop, uv in zip(f.loops, uvs):
            loop[uvl].uv = uv
            loop[col] = (w, 0.0, 0.0, 1.0)
    # couverture : plaque derrière le bloc, un peu plus grande
    c, d = P["couverture"], P["deborde"]
    pts = []
    for (u, v) in ((0, 0), (1, 0), (1, 1), (0, 1)):
        x = sign * (u * (W + d))
        z = (v - 0.5) * (H + 2 * d)
        pts.append((R @ Vector((x, -0.0005, z)), R @ Vector((x, -0.0005 - c, z))))
    vf = [bm.verts.new(p[0]) for p in pts]
    vb = [bm.verts.new(p[1]) for p in pts]
    rings = [vf, vb]
    def f4(q):
        f = bm.faces.new(q if sign > 0 else q[::-1]); f.material_index = 0
        for loop in f.loops:
            loop[col] = (0.0, 0.0, 0.0, 1.0)
        return f
    f4(vf[::-1]); f4(vb)
    for k in range(4):
        f4([vf[k], vf[(k + 1) % 4], vb[(k + 1) % 4], vb[k]])


def page_libre(bm, angle, col, uvl, k):
    """Page soulevée depuis la moitié droite, tournée de `angle` degrés autour du dos, courbée."""
    H, W = P["hauteur"] * 0.97, P["demi_largeur"] * 0.97
    nu, nv = 8, 3
    R = Matrix.Rotation(math.radians(P["ouverture"] + angle), 4, "Z")
    vs = []
    for j in range(nv + 1):
        row = []
        for i in range(nu + 1):
            u, v = i / nu, j / nv
            curl = 0.035 * math.sin(math.pi * u * 0.9) * (1 + 0.3 * k)
            p = R @ Vector((u * W, P["bloc"] + 0.012 + curl * (0.6 + 0.4 * v), (v - 0.5) * H))
            row.append(bm.verts.new(p))
        vs.append(row)
    for j in range(nv):
        for i in range(nu):
            f = bm.faces.new((vs[j][i], vs[j][i + 1], vs[j + 1][i + 1], vs[j + 1][i]))
            f.material_index = 1
            uvs = [(i / nu, j / nv), ((i + 1) / nu, j / nv), ((i + 1) / nu, (j + 1) / nv), (i / nu, (j + 1) / nv)]
            for loop, uv in zip(f.loops, uvs):
                loop[uvl].uv = uv
                loop[col] = (uv[0], 0.0, 0.0, 1.0)      # R : poids du frémissement (bord libre = 1)


def volute(bm, k, col, uvl):
    """Ruban qui monte en spirale autour du livre (uv x le long, y en travers)."""
    n = 36
    w = 0.0055
    ph = k * 2.1
    pts = []
    for i in range(n + 1):
        s = i / n
        a = ph + s * math.pi * 2.2
        r = 0.03 + 0.08 * s
        pts.append(Vector((r * math.cos(a) + 0.02 * (k - 1), 0.04 + 0.035 * math.sin(a), 0.02 + 0.20 * s)))
    prev = None
    for i in range(n + 1):
        tdir = (pts[min(i + 1, n)] - pts[max(i - 1, 0)]).normalized()
        side = tdir.cross(Vector((0, 1, 0)))
        if side.length < 1e-4:
            side = Vector((1, 0, 0))
        side.normalize()
        ww = w * math.sin(math.pi * i / n) ** 0.6 + 0.0005
        a_ = bm.verts.new(pts[i] - side * ww); b_ = bm.verts.new(pts[i] + side * ww)
        if prev is not None:
            f = bm.faces.new((prev[0], prev[1], b_, a_))
            f.material_index = 2
            u0, u1 = (i - 1) / n, i / n
            for loop, uv in zip(f.loops, [(u0, 0), (u0, 1), (u1, 1), (u1, 0)]):
                loop[uvl].uv = uv
                loop[col] = (u0, 0.0, 0.0, 1.0)
        prev = (a_, b_)


def objet(name, fill, mats, sc, normales=False):
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    col = bm.loops.layers.color.new("Col")
    uvl = bm.loops.layers.uv.new("UVMap")
    fill(bm, col, uvl)
    if normales:
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(me); bm.free()
    for m in mats:
        me.materials.append(m)
    ob = bpy.data.objects.new(name, me)
    sc.collection.objects.link(ob)
    return ob


def build(_):
    sc = bpy.context.scene
    mats = [mat("M_Livre_Couverture", "#2B4A66"), mat("M_Livre_Pages", "#BFE6FF"), mat("M_Livre_Volutes", "#8FD8FF")]
    def corps(bm, col, uvl):
        half(bm, 1, col, uvl); half(bm, -1, col, uvl)
        # dos : demi-cylindre derrière, axe vertical
        H = P["hauteur"] + 2 * P["deborde"]
        n = 8
        ring0, ring1 = [], []
        for i in range(n + 1):
            a = math.pi * i / n
            p = Vector((0.012 * math.cos(a), -0.004 - 0.010 * math.sin(a), 0))
            ring0.append(bm.verts.new(p + Vector((0, 0, -H / 2)))); ring1.append(bm.verts.new(p + Vector((0, 0, H / 2))))
        for i in range(n):
            f = bm.faces.new((ring0[i + 1], ring0[i], ring1[i], ring1[i + 1])); f.material_index = 0
            for loop in f.loops:
                loop[col] = (0.0, 0.0, 0.0, 1.0)
    root = objet("SM_Livre_Hibou", corps, mats, sc, normales=True)
    pages = objet("SM_Livre_Hibou_Pages_Libres", lambda bm, c, u: [page_libre(bm, a, c, u, k) for k, a in enumerate(P["pages_libres"])], mats, sc)
    vol = objet("SM_Livre_Hibou_Volutes", lambda bm, c, u: [volute(bm, k, c, u) for k in range(P["volutes"])], mats, sc)
    for o in (pages, vol):
        F.attach(o, root)
    fx = F.empty("FX_Livre", (0, P["bloc"] + 0.02, 0), root, size=0.02, sc=sc)
    return [root, pages, vol, fx]


if __name__ == "__main__":
    res = F.run("SM_Livre_Hibou", build, P, views=("face", "34", "oeil"))
