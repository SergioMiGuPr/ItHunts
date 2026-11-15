# -*- coding: utf-8 -*-
"""SM_Levier (+ SM_Levier_Manche) : levier de l'arène du boss (ART-HANDOFF : plaque de 1.2 × 0.8 × 1.2 m sur un
socle de 1.2 m ; orange puis vert en jeu).

Socle de pierre de 1.2 m (1 × 1 m, chanfreiné), plaque de fer rivetée de 1.2 × 0.8 × 1.2 m posée dessus (dessus à
2 m), cristal en losange et anneau de rune sur la face avant, matériau à part M_Levier_Lueur (émission prête mais
éteinte : orange puis vert en jeu). Sur le dessus : deux paliers et un axe ; le manche SM_Levier_Manche (tige de
fer, poignée en bois) a son pivot sur l'axe de rotation (X) et bascule de ±35° (modélisé à +35°, vers l'avant).

Pivot du levier : au sol, au centre. Avant vers -Y (Blender) = +Z (Unity).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_levier.py"
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
    socle=(1.0, 1.2), plaque=(1.2, 0.8, 1.2),
    rivets=0.16, cristal=dict(l=0.18, h=0.26, saillie=0.07), rune_r=(0.24, 0.29),
    paliers=dict(ecart=0.3, l=0.08, h=0.18, p=0.18), axe_r=0.04,
    manche=dict(longueur=0.75, section=0.06, poignee=(0.045, 0.3), angle=35.0),
    lueur="#5A3420",           # ambre sombre : éteint par défaut ; l'émission (orange puis vert) est pilotée en jeu
)


def build(P):
    mats = N.materials()
    mats["lueur"] = F.flat_material("M_Levier_Lueur", P["lueur"])
    B = N.builder()
    B.set_uv_params("lueur", 1.0, 1.0)
    s, hs = P["socle"]
    C.fit_stones(B, B.box((-s / 2, -s / 2, 0), (s / 2, s / 2, hs), "pierre", skip=("-z",), bevel=0.05))
    pw, ph, pd = P["plaque"]
    z0, z1 = hs, hs + ph
    B.box((-pw / 2, -pd / 2, z0), (pw / 2, pd / 2, z1), "fer", bevel=0.02)
    r = P["rivets"]
    for x in (-pw / 2 + r, pw / 2 - r):
        for z in (z0 + r, z1 - r):
            N.rivet(B, (x, -pd / 2, z), (0, -1, 0), 0.035, 0.025, "fer")
    # cristal en losange (octaèdre aplati) à moitié enchâssé dans la face avant, anneau de rune autour
    Cr = P["cristal"]
    c = Vector((0, -pd / 2, (z0 + z1) / 2))
    pts = [c + Vector((0, 0, Cr["h"] / 2)), c + Vector((Cr["l"] / 2, 0, 0)), c - Vector((0, 0, Cr["h"] / 2)), c - Vector((Cr["l"] / 2, 0, 0))]
    tip = c - Vector((0, Cr["saillie"], 0))
    B.new_shell()
    for k in range(4):
        tri = [pts[k], pts[(k + 1) % 4], tip]
        B.face(tri, "lueur", sum(tri, Vector()) / 3 - (c + Vector((0, 0.05, 0))))
    r0, r1 = P["rune_r"]
    segs = 16
    B.new_shell()
    for k in range(segs):
        if k % 4 == 3:
            continue                         # anneau interrompu (4 arcs) : se lit comme une rune
        a0, a1 = 2 * math.pi * k / segs, 2 * math.pi * (k + 1) / segs
        q = [c + Vector((rr * math.cos(a), -0.006, rr * math.sin(a))) for rr, a in ((r0, a0), (r0, a1), (r1, a1), (r1, a0))]
        B.face(q, "lueur", (0, -1, 0))
    # paliers et axe sur le dessus
    Pa = P["paliers"]
    za = z1 + Pa["h"] * 0.6
    for sx in (-1, 1):
        x = sx * Pa["ecart"]
        B.box((x - Pa["l"] / 2, -Pa["p"] / 2, z1), (x + Pa["l"] / 2, Pa["p"] / 2, z1 + Pa["h"]), "fer", skip=("-z",))
    C.tube(B, [Vector((-Pa["ecart"] - 0.06, 0, za)), Vector((Pa["ecart"] + 0.06, 0, za))], P["axe_r"], 8, "fer",
           up=Vector((0, 0, 1)))
    socle = B.finish("SM_Levier", mats)

    # manche : modélisé droit (vertical) autour de son pivot, puis incliné de +35° vers l'avant (rotation X)
    M = P["manche"]
    B2 = N.builder()
    L, sct = M["longueur"], M["section"]
    B2.box((-sct / 2, -sct / 2, -0.05), (sct / 2, sct / 2, L), "fer")
    C.tube(B2, [Vector((-Pa["ecart"] + Pa["l"], 0, 0)), Vector((Pa["ecart"] - Pa["l"], 0, 0))], 0.05, 8, "fer", up=Vector((0, 0, 1)))
    pr, pl = M["poignee"]
    C.tube(B2, [Vector((0, 0, L)), Vector((0, 0, L + pl))], pr, 8, "bois", up=Vector((1, 0, 0)))
    N.orient(B2, Matrix.Rotation(math.radians(M["angle"]), 4, "X"))      # rotation X positive : le haut part vers -Y (avant)
    manche = B2.finish("SM_Levier_Manche", mats)
    manche.location = (0, 0, za)                 # pivot sur l'axe de rotation
    F.attach(manche, socle)
    print("levier : socle %d, manche %d triangles ; axe à %.3f m" % (C.tri_count(socle), C.tri_count(manche), za))
    return [socle, manche]


if __name__ == "__main__":
    N.run("SM_Levier", build, PARAMS, views=("face", "cote", "34"))
