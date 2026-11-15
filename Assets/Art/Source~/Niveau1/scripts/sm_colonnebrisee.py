# -*- coding: utf-8 -*-
"""SM_ColonneBrisee : colonne brisée (Prop_ColonneBrisee, 1.4 × 2.6 × 1.4 m), d'après SM_Colonne.

Reprend la base à deux gradins, l'anneau mouluré du bas et les tambours de 0.6 m de SM_Colonne (mêmes
mesures, même calage de la texture de pierre), coupés à 2.6 m par une cassure irrégulière (bord en dents,
cœur creusé). La base occupe toute l'emprise de 1.4 × 1.4 m : les 3 morceaux de tambour sont posés sur les
gradins (dans l'emprise), l'un appuyé contre le fût.

Pivot : au sol, au centre.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_colonnebrisee.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("n1_props", None)
import n1_props as N
C, F = N.C, N.F
from mathutils import Vector, Matrix

PARAMS = dict(
    hauteur=2.6,
    gradins=[(1.4, 0.4), (1.2, 0.3)],        # comme SM_Colonne
    fut_r=0.4, anneau_h=0.15, anneau_r=0.47, tambour=0.6, joint=0.02, segments=16,
    v_decalage=0.0104,
    cassure=[0.0, 0.22, 0.08, 0.30, 0.12, 0.05, 0.26, 0.16, 0.02, 0.18, 0.34, 0.10, 0.04, 0.24, 0.14, 0.06],   # creux sous 2.6 m
    coeur=dict(r=0.22, creux=0.18),
    morceaux=[  # (x, y, z de pose, rotation Z °, inclinaison °, rayon, angle °, épaisseur)
        (0.42, -0.42, 0.7, 225, 12, 0.26, 75, 0.2),
        (-0.47, 0.44, 0.7, 40, 0, 0.2, 60, 0.16),
        (0.47, 0.47, 0.7, 205, 18, 0.16, 70, 0.14),
    ],
)


def slice_piece(B, M, r, ang, th, steps=4):
    """Morceau de tambour : part de camembert (rayon r, angle ang, épaisseur th), transformée par M."""
    B.new_shell()
    a0 = -math.radians(ang) / 2
    pts = [Vector((0, 0, 0))] + [Vector((r * math.cos(a0 + math.radians(ang) * i / steps),
                                         r * math.sin(a0 + math.radians(ang) * i / steps), 0)) for i in range(steps + 1)]
    bot = [M @ p for p in pts]
    top = [M @ (p + Vector((0, 0, th))) for p in pts]
    cen = sum(bot + top, Vector()) / (2 * len(pts))
    B.face(top, "pierre", sum(top, Vector()) / len(top) - cen)
    B.face(bot, "pierre", sum(bot, Vector()) / len(bot) - cen)
    n = len(pts)
    for i in range(n):
        q = [bot[i], bot[(i + 1) % n], top[(i + 1) % n], top[i]]
        B.face(q, "pierre", sum(q, Vector()) / 4 - cen)


def build(P):
    mats = N.materials()
    B = N.builder()
    tw, th = 4.0, 3.0
    voff = P["v_decalage"]
    B.set_uv_params("pierre", tw, th, voff)
    seg = P["segments"]
    R = P["fut_r"]
    z = 0.0
    for side, hh in P["gradins"]:
        s = side / 2
        C.fit_stones(B, B.box((-s, -s, z), (s, s, z + hh), "pierre", skip=("-z",), bevel=0.03))
        z += hh
    z_base = z
    Ra, ah = P["anneau_r"], P["anneau_h"]

    def uv_round(z0, course, h, ph=0.0):
        def f(k, i, r, zz):
            return (k / seg + ph, voff + (course + 0.04 + 0.92 * (zz - z0) / h) / 4)
        return f

    C.revolve(B, [(Ra, z_base), (Ra, z_base + ah * 0.55), (R, z_base + ah)], seg, "pierre", uv=uv_round(z_base, 0, ah))
    z_f0 = z_base + ah
    H = P["hauteur"]
    j = P["joint"]
    td = P["tambour"]
    zz = z_f0
    i = 0
    while zz + td < H - 0.2:                 # tambours entiers
        za, zb = zz, zz + td
        prof = [(R - j, za), (R, za + j), (R, zb - j), (R - j, zb)]
        C.revolve(B, prof, seg, "pierre", uv=uv_round(za, (i * 3 + 1) % 4, td, (i * 0.37) % 1.0))
        zz, i = zb, i + 1
    # dernier tambour, cassé : bord en dents (hauteurs variables), cœur creusé
    za = zz
    cas = P["cassure"]
    # cassure en biais (un côté plus bas, jusqu'à 0.3 m) + dents irrégulières
    hts = [H - 0.3 * (1 - math.cos(2 * math.pi * k / seg - 0.6)) / 2 - 0.6 * cas[k % len(cas)] for k in range(seg)]
    hts = [h + (H - max(hts)) for h in hts]          # la plus haute dent à 2.6 m
    course, ph = (i * 3 + 1) % 4, (i * 0.37) % 1.0
    hmax = H - za
    B.new_shell()
    ang = [2 * math.pi * k / seg for k in range(seg)]
    low = [Vector((R * math.cos(a), R * math.sin(a), za + j)) for a in ang]
    low0 = [Vector(((R - j) * math.cos(a), (R - j) * math.sin(a), za)) for a in ang]
    up = [Vector((R * math.cos(a), R * math.sin(a), hts[k])) for k, a in enumerate(ang)]
    def uvp(k, zv):
        return (k / seg + ph, voff + (course + 0.04 + 0.92 * (zv - za) / hmax) / 4)
    for k in range(seg):
        k2 = (k + 1) % seg
        kk2 = k + 1
        rad = Vector((math.cos((ang[k] + ang[k2]) / 2 if k2 else ang[k] + math.pi / seg), math.sin((ang[k] + ang[k2]) / 2 if k2 else ang[k] + math.pi / seg), 0))
        B.face([low0[k], low0[k2], low[k2], low[k]], "pierre", rad - Vector((0, 0, 1)),
               [uvp(k, za), uvp(kk2, za), uvp(kk2, za + j), uvp(k, za + j)])
        B.face([low[k], low[k2], up[k2], up[k]], "pierre", rad,
               [uvp(k, za + j), uvp(kk2, za + j), uvp(kk2, hts[k2]), uvp(k, hts[k])])
    # dessus cassé : anneau intérieur plus bas et irrégulier, puis cœur creusé
    Co = P["coeur"]
    mid = [Vector((Co["r"] * math.cos(a + 0.1), Co["r"] * math.sin(a + 0.1), hts[k] - 0.06 - 0.5 * cas[(k + 5) % len(cas)]))
           for k, a in enumerate(ang)]
    centre = Vector((0.03, -0.02, H - Co["creux"] - 0.15))
    for k in range(seg):
        k2 = (k + 1) % seg
        B.face([up[k], up[k2], mid[k2], mid[k]], "pierre", (0, 0, 1))
        B.face([mid[k], mid[k2], centre], "pierre", (0, 0, 1))
    # morceaux de tambour posés sur les gradins
    for x, y, zp, rz, tilt, r, a, t in P["morceaux"]:
        M = Matrix.Translation((x, y, zp)) @ Matrix.Rotation(math.radians(rz), 4, "Z") @ Matrix.Rotation(math.radians(tilt), 4, "Y")
        slice_piece(B, M, r, a, t)
    ob = B.finish("SM_ColonneBrisee", mats)
    print("colonne brisée : %d triangles ; %d tambours entiers + 1 cassé (de %.2f à 2.26-2.60 m)" % (C.tri_count(ob), i, za))
    return [ob]


if __name__ == "__main__":
    N.run("SM_ColonneBrisee", build, PARAMS, views=("face", "cote", "34"))
