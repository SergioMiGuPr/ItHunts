# -*- coding: utf-8 -*-
"""SM_Tuyau : gros tuyau d'égout couché (Prop_Tuyau, 0.8 × 0.8 × 6 m), en fonte rouillée.

Corps de Ø 0.8 m sur 6 m le long de la profondeur (Z Unity), 3 colliers boulonnés (4 boulons chacun), bouche
ouverte à l'avant (+Z Unity) avec un rebord épais ; intérieur de la bouche, face avant du rebord et une langue
de vase (fond de la bouche noir, M_Fond_Noir) qui coule par-dessus le rebord en M_Egout ; arrière fermé (côté mur). Le rebord (Ø 0.92) et les
colliers (Ø 0.88) dépassent du corps : le tuyau repose sur eux, d'où 0.92 m de haut hors tout.

Pivot : au sol, au centre.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_tuyau.py"
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
    longueur=6.0, r=0.4, segs=20,
    rebord=dict(r=0.46, r_int=0.36, long=0.18, prof_int=1.4),
    colliers=dict(z=(-2.2, -0.2, 1.8), r=0.44, largeur=0.12, boulon=(0.05, 0.035)),
    vase=dict(largeur=0.34, epaisseur=0.03, coulure=0.32),
)


def build(P):
    mats = N.materials()
    B = N.builder()
    L, R, seg = P["longueur"], P["r"], P["segs"]
    zb, zf = -L / 2, L / 2                     # repère local : axe Z, arrière en zb, bouche en zf
    Rb = P["rebord"]
    zl = zf - Rb["long"]
    ph = math.pi / seg
    # corps, fond arrière fermé
    C.revolve(B, [(R, zb), (R, zl)], seg, "fer", phase=ph)
    B.face([Vector((R * math.cos(ph + 2 * math.pi * k / seg), R * math.sin(ph + 2 * math.pi * k / seg), zb)) for k in range(seg)],
           "fer", (0, 0, -1))
    # rebord de la bouche : flanc et dos en fonte, face avant et intérieur en vase
    C.revolve(B, [(R, zl), (Rb["r"], zl), (Rb["r"], zf)], seg, "fer", phase=ph)
    C.revolve(B, [(Rb["r"], zf), (Rb["r_int"], zf)], seg, "egout", phase=ph, shell=False)
    zi = zf - Rb["prof_int"]
    C.revolve(B, [(Rb["r_int"], zf), (Rb["r_int"], zi)], seg, "egout", phase=ph, shell=False)
    B.face([Vector((Rb["r_int"] * math.cos(ph + 2 * math.pi * k / seg), Rb["r_int"] * math.sin(ph + 2 * math.pi * k / seg), zi))
            for k in range(seg)], "noir", (0, 0, 1))          # fond noir : le tuyau se perd dans l'ombre
    # colliers boulonnés
    Co = P["colliers"]
    bs, bh = Co["boulon"]
    for z in Co["z"]:
        w = Co["largeur"] / 2
        C.revolve(B, [(R - 0.005, z - w), (Co["r"], z - w), (Co["r"], z + w), (R - 0.005, z + w)], seg, "fer", phase=ph)
        for k in range(4):
            a = math.pi / 4 + k * math.pi / 2
            rad = Vector((math.cos(a), math.sin(a), 0))
            tan = Vector((-math.sin(a), math.cos(a), 0))
            N.obox(B, rad * (Co["r"] + bh / 2 - 0.004), tan, Vector((0, 0, 1)), rad, bs / 2, bs / 2, bh / 2, "fer", skip=("-z",))
    # vase : langue au fond de la bouche qui déborde du rebord et coule vers le sol (côté -Y local = bas)
    V = P["vase"]
    hw = V["largeur"] / 2
    yb = -Rb["r_int"] + 0.01
    N.obox(B, Vector((0, yb + V["epaisseur"] / 2, (zi + zf) / 2 + 0.04)), (1, 0, 0), (0, 1, 0), (0, 0, 1),
           hw, V["epaisseur"] / 2, (zf - zi) / 2 + 0.04, "egout")
    N.obox(B, Vector((0, -(Rb["r_int"] + Rb["r"]) / 2, zf + 0.015)), (1, 0, 0), (0, 1, 0), (0, 0, 1),
           hw * 0.8, (Rb["r"] - Rb["r_int"]) / 2, 0.015, "egout")       # coulure sur la face avant du rebord, jusqu'au sol
    # couché : l'axe local Z devient l'avant (-Y Blender = +Z Unity), posé sur le rebord (au sol) ; avant les UV
    N.orient(B, Matrix.Translation((0, 0, Rb["r"])) @ Matrix.Rotation(math.radians(90), 4, "X"))
    ob = B.finish("SM_Tuyau", mats)
    print("tuyau : %d triangles" % C.tri_count(ob))
    return [ob]


if __name__ == "__main__":
    N.run("SM_Tuyau", build, PARAMS, views=("face", "cote", "34"))
