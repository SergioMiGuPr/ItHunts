# -*- coding: utf-8 -*-
"""SM_Orbe : orbe de la main droite (vue FPS), d'après fiche3d_orbe_v2_v01 et ref_orbe_v2.
Les cotes de la commande priment sur la fiche (qui a des incohérences).

Objets (pivot de tous au centre de l'orbe, transformations nulles) :
- SM_Orbe : racine vide ;
- SM_Orbe_Anneau_1 (Ø ext. 0.18), _2 (0.15), _3 (0.12) : bandes de 0.018 (largeur) × 0.008 m
  (épaisseur) ; gorge en fer (fond + deux rebords) et dalles de pierre sombre posées dedans, séparées
  par des joints, un petit rivet à chaque joint sur chaque rebord. Emboîtés sans contact quelle que
  soit leur rotation. Axe de rotation (repère Unity) : anneau 1 autour de X, 2 autour de Y, 3 autour
  de Z ; modélisés à leur pose de repos (0°, 60°, 120°) : l'axe de chacun est dans son plan.
  Repos : anneau 1 horizontal (normale +Y, 0° autour de X) ; anneau 2 : normale +Z tournée de 60°
  autour de Y ; anneau 3 : normale +X tournée de 120° autour de Z. Ainsi les trois montrent leur
  bande de pierre au joueur (un anneau face au joueur ne montrerait que sa tranche de fer de 8 mm) ;
- SM_Orbe_Tablette_01 à _06 : tablettes flottantes 0.035 × 0.05 × 0.008 m en couronne à 0.13 m du
  centre, dans le plan face au joueur, grand côté tangent à la couronne ; corps en pierre
  (M_Orbe_Pierre), glyphe abstrait en léger relief sur la face tournée vers le joueur, seul porteur de
  M_Orbe_Tablette_0X (allumer ce matériau allume le glyphe) ;
- SM_Orbe_Energie : sphère de Ø 0.09 m, M_Orbe_Energie, UV sphériques (support du shader de vortex) ;
- FX_Orbe : repère vide au centre.

Repère Unity : X droite, Y haut, Z avant (loin du joueur). Converti en Blender par U().

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sm_orbe.py"
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

PARAMS = dict(
    anneaux=[  # Ø ext., nb de dalles, normale au repos (Unity), axe de rotation (Unity)
        dict(d=0.18, dalles=12, normale=(0, 1, 0), axe=(1, 0, 0)),
        dict(d=0.15, dalles=10, normale=(math.sin(math.radians(60)), 0, math.cos(math.radians(60))), axe=(0, 1, 0)),
        dict(d=0.12, dalles=8, normale=(math.cos(math.radians(120)), math.sin(math.radians(120)), 0), axe=(0, 0, 1)),
    ],
    largeur=0.018, epaisseur=0.008, rebord=0.0025, fond=0.002, joint=0.0025,
    dalle_retrait=0.0003, pas_dalle=2, segs=24,
    rivet=dict(cote=0.0022, haut=0.0012),
    tablette=dict(l=0.035, h=0.05, e=0.008, rayon=0.13, angles=(0, 40, 140, 180, 220, 320),
                  trait=0.0022, relief=0.0006),
    energie=dict(d=0.09, segs=16, anneaux=8),
    couleurs=dict(glyphe="#D9D3C7", energie="#8E1B12"),   # pierre et fer : tuiles peintes ; énergie : rouge renard par défaut (couleur du personnage, réglée en jeu)
)

# glyphes abstraits (aucune lettre), traits en coordonnées normalisées [-1, 1] de la face
GLYPHES = [
    [[(0, 0.5), (0.6, 0), (0, -0.5), (-0.6, 0), (0, 0.5)], [(0, 0.65), (0, 0.92)], [(0, -0.65), (0, -0.92)]],
    [[(0, -0.9), (0, 0.9)], [(0, 0.5), (0.45, 0.75)], [(0, 0.0), (-0.45, 0.25)], [(0, -0.5), (0.45, -0.25)]],
    [[(-0.5, 0.3), (-0.5, 0.8), (0.5, 0.8)], [(0, 0.8), (0, -0.8)], [(0, -0.2), (0.5, -0.7)]],
    [[(0, 0.7), (0.6, -0.35), (-0.6, -0.35), (0, 0.7)], [(0, -0.35), (0, -0.92)]],
    [[(-0.6, -0.1), (0, 0.5), (0.6, -0.1)], [(-0.6, -0.75), (0, -0.15), (0.6, -0.75)], [(0, 0.68), (0, 0.92)]],
    [[(-0.5, 0.8), (0.5, 0.8), (0.5, -0.8), (-0.3, -0.8), (-0.3, 0.3), (0.2, 0.3), (0.2, -0.4)]],
]


def U(x, y, z):
    return Vector((-x, -z, y))


def frame(normal_u, axis_u):
    """Matrice locale -> Blender : Z local = normale de l'anneau, X local = axe de rotation."""
    z = U(*normal_u).normalized()
    x = U(*axis_u).normalized()
    x = (x - z * x.dot(z)).normalized()
    y = z.cross(x)
    return Matrix((x, y, z)).transposed().to_4x4()


def build_ring(P, k, A, mats):
    R = A["d"] / 2
    w, t, lw = P["largeur"], P["epaisseur"], P["rebord"]
    ri, rf = R - t, R - t + P["fond"]
    h = w / 2
    B = C.Builder()
    for key in ("pierre", "fer"):
        B.set_uv_params(key, F.TILE_M, F.TILE_M)
    seg = P["segs"]
    # gorge en fer : fond + deux rebords (profil fermé, sens trigonométrique dans le plan (r, z))
    prof = [(ri, -h), (R, -h), (R, -h + lw), (rf, -h + lw), (rf, h - lw), (R, h - lw), (R, h), (ri, h), (ri, -h)]
    C.revolve(B, prof, seg, "fer")
    # dalles de pierre entre les rebords
    n = A["dalles"]
    gap = P["joint"] / R
    rt = R - P["dalle_retrait"]
    z0, z1 = -h + lw + 0.0002, h - lw - 0.0002
    steps = P["pas_dalle"]
    for i in range(n):
        a0 = 2 * math.pi * i / n + gap / 2
        a1 = 2 * math.pi * (i + 1) / n - gap / 2
        rings = []
        for s in range(steps + 1):
            a = a0 + (a1 - a0) * s / steps
            c, sn = math.cos(a), math.sin(a)
            rings.append([Vector((r * c, r * sn, z)) for r, z in ((rf, z0), (rt, z0), (rt, z1), (rf, z1))])
        B.sweep(rings, "pierre")
    # rivets : petite pyramide sur chaque rebord, à chaque joint
    rv = P["rivet"]
    for i in range(n):
        a = 2 * math.pi * i / n
        rad = Vector((math.cos(a), math.sin(a), 0))
        tan = Vector((-math.sin(a), math.cos(a), 0))
        for zc in (-h + lw / 2, h - lw / 2):
            B.new_shell()
            base = rad * R + Vector((0, 0, zc))
            s = rv["cote"] / 2
            sq = [base + tan * dx * s + Vector((0, 0, dz * s)) for dx, dz in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
            apex = base + rad * rv["haut"]
            for j in range(4):
                tri = [sq[j], sq[(j + 1) % 4], apex]
                cen = sum(tri, Vector()) / 3
                B.face(tri, "fer", cen - (base - rad * 0.001))
    ob = B.finish("SM_Orbe_Anneau_%d" % k, mats)
    ob.data.transform(frame(A["normale"], A["axe"]))
    ob.data.update()
    return ob


def stroke_box(B, a, b, e1, e2, n, face_c, width, h0, h1, mat):
    """Trait en relief sur une face : a, b en coordonnées 2D (e1, e2) depuis face_c ; n normale."""
    pa = face_c + e1 * a[0] + e2 * a[1]
    pb = face_c + e1 * b[0] + e2 * b[1]
    d = (pb - pa)
    L = d.length
    if L < 1e-6:
        return
    d /= L
    pa -= d * width / 2; pb += d * width / 2          # traits qui se rejoignent aux coins
    p = n.cross(d).normalized() * width / 2
    lo = [pa - p + n * h0, pb - p + n * h0, pb + p + n * h0, pa + p + n * h0]
    hi = [q + n * (h1 - h0) for q in lo]
    B.new_shell()
    cen = sum(lo + hi, Vector()) / 8
    B.face(hi, mat, n)
    for j in range(4):
        q = [lo[j], lo[(j + 1) % 4], hi[(j + 1) % 4], hi[j]]
        B.face(q, mat, sum(q, Vector()) / 4 - cen)


def build_tablet(P, i, mats):
    T = P["tablette"]
    ang = math.radians(T["angles"][i])
    cu = (T["rayon"] * math.cos(ang), T["rayon"] * math.sin(ang), 0.0)
    c = U(*cu)
    radial = (U(math.cos(ang), math.sin(ang), 0)).normalized()
    along = (U(-math.sin(ang), math.cos(ang), 0)).normalized()     # grand côté : tangent
    n = U(0, 0, -1).normalized()                                     # face vers le joueur
    key = "glyphe%d" % (i + 1)
    B = C.Builder()
    B.set_uv_params("pierre", F.TILE_M, F.TILE_M)
    B.set_uv_params(key, F.TILE_M, F.TILE_M)
    hl, hh, he = T["l"] / 2, T["h"] / 2, T["e"] / 2
    corners = []
    for sn in (-1, 1):
        for sa in (-1, 1):
            for sr in (-1, 1):
                corners.append(c + n * sn * he + along * sa * hh + radial * sr * hl)
    # boîte orientée : 6 faces
    def P_(sn, sa, sr):
        return c + n * sn * he + along * sa * hh + radial * sr * hl
    B.new_shell()
    for axis_vec, idx in ((n, 0), (along, 1), (radial, 2)):
        for sgn in (-1, 1):
            pts = []
            for u_, v_ in ((-1, -1), (1, -1), (1, 1), (-1, 1)):
                s = [0, 0, 0]
                s[idx] = sgn
                others = [j for j in range(3) if j != idx]
                s[others[0]], s[others[1]] = u_, v_
                pts.append(P_(*s))
            B.face(pts, "pierre", axis_vec * sgn)
    # glyphe sur la face joueur : e1 = largeur (radial), e2 = hauteur (le long du grand côté)
    face_c = c + n * he
    sx, sy = hl * 0.70, hh * 0.78
    e1, e2 = radial * sx, along * sy
    for poly in GLYPHES[i]:
        for a, b in zip(poly[:-1], poly[1:]):
            stroke_box(B, a, b, e1, e2, n, face_c, T["trait"], -0.0002, T["relief"], key)
    mats_all = dict(mats)
    mats_all[key] = mats["tab"][i]
    return B.finish("SM_Orbe_Tablette_%02d" % (i + 1), mats_all)


def build_energy(P, mats):
    E = P["energie"]
    me = bpy.data.meshes.new("SM_Orbe_Energie")
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=E["segs"], v_segments=E["anneaux"], radius=E["d"] / 2, calc_uvs=True)
    for f in bm.faces:
        f.smooth = True
    bm.to_mesh(me); bm.free()
    me.materials.append(mats["energie"])
    ob = bpy.data.objects.new("SM_Orbe_Energie", me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def build(P):
    col = P["couleurs"]
    mats = {"pierre": F.tex_material("M_Orbe_Pierre", "T_Orbe_Pierre_BaseColor"),
            "fer": F.tex_material("M_Orbe_Fer", "T_Orbe_Fer_BaseColor"),
            "energie": F.flat_material("M_Orbe_Energie", col["energie"])}
    mats["tab"] = [F.flat_material("M_Orbe_Tablette_%02d" % (i + 1), col["glyphe"]) for i in range(6)]
    root = bpy.data.objects.new("SM_Orbe", None)
    root.empty_display_type = "SPHERE"; root.empty_display_size = 0.02
    bpy.context.scene.collection.objects.link(root)
    parts = []
    for k, A in enumerate(P["anneaux"], 1):
        parts.append(build_ring(P, k, A, {"pierre": mats["pierre"], "fer": mats["fer"]}))
    for i in range(6):
        parts.append(build_tablet(P, i, mats))
    parts.append(build_energy(P, mats))
    for ob in parts:
        F.attach(ob, root)
    fx = F.empty("FX_Orbe", (0, 0, 0), root)
    for ob in parts:
        print("%s : %d triangles" % (ob.name, C.tri_count(ob)))
    print("total : %d triangles" % sum(C.tri_count(o) for o in parts))
    return [root] + parts + [fx]


if __name__ == "__main__":
    F.run("SM_Orbe", build, PARAMS, views=("dos", "cote", "34", "oeil", "face"))
