# -*- coding: utf-8 -*-
"""SM_Echafaudage : échafaudage en bois (Prop_Echafaudage, 4 × 6 × 2 m, on y monte).

6 poteaux en bois équarri de 0.15 m ; deux plateaux à 3 m et 6 m (dessus à ces hauteurs), plats et pleins :
planches de 0.25 m dans le sens de la profondeur, posées sur un plancher continu (aucun trou) et sur des
longerons ; **variante praticable** (INTEGRATION-PROPS.md, 2026-11-15) : croisillons en X seulement à l'étage (petits
côtés et arrière), jambes de force courtes sous le plateau du bas ; le rez-de-chaussée est libre (2.9 m de passage)
pour le joueur et les goules ; échelle intégrée sur la face avant (à droite), du sol au plateau du haut ; ligatures de
corde autour des poteaux à chaque niveau et au croisement de chaque X. Bois : M_Bois_Brut (tuile peinte, 256 px/m) ; corde : M_Corde.

Pivot : au sol, au centre de l'emprise. Avant (échelle) vers -Y (Blender) = +Z (Unity).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_echafaudage.py"
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
    largeur=4.0, profondeur=2.0, niveaux=(3.0, 6.0),
    poteau=0.15, planche=dict(l=0.25, e=0.05, jeu=0.012), plancher=0.02,
    longeron=(0.15, 0.15), croisillon=(0.1, 0.04),
    echelle=dict(x=(0.75, 1.25), montant=(0.07, 0.05), barreau=0.04, pas=0.3, y=-1.04),
    ligature=dict(h=0.08, ep=0.012),
)


def build(P):
    mats = N.materials()
    B = N.builder()
    W, D = P["largeur"], P["profondeur"]
    hw, hd = W / 2, D / 2
    p = P["poteau"]
    xs = (-hw + p / 2, 0.0, hw - p / 2)
    ys = (-hd + p / 2, hd - p / 2)
    top = max(P["niveaux"])
    for x in xs:
        for y in ys:
            B.box((x - p / 2, y - p / 2, 0), (x + p / 2, y + p / 2, top), "bois", skip=("-z",))
    lw, lh = P["longeron"]
    Pl = P["planche"]
    for zt in P["niveaux"]:
        z_pl = zt - Pl["e"]                       # dessous des planches
        # plancher continu sous les planches (plein, sans trou), puis longerons
        B.box((-hw, -hd, z_pl - P["plancher"]), (hw, hd, z_pl), "bois")
        zl = z_pl - P["plancher"]
        for y in ys:                              # longerons avant et arrière (le long de X)
            B.box((-hw + p, y - lw / 2, zl - lh), (hw - p, y + lw / 2, zl), "bois")
        for x in xs:                              # traverses le long de Y
            B.box((x - lw / 2, -hd + p, zl - lh), (x + lw / 2, hd - p, zl), "bois")
        # planches (le long de Y), petits jeux entre elles : elles se lisent, le plancher reste plein dessous
        n = int(round(W / Pl["l"]))
        for i in range(n):
            x0 = -hw + i * W / n + Pl["jeu"] / 2
            x1 = -hw + (i + 1) * W / n - Pl["jeu"] / 2
            B.box((x0, -hd, z_pl), (x1, hd, zt), "bois", skip=("-z",))
    # croisillons en X : petits côtés et arrière, contre la face intérieure des poteaux, deux étages
    cw, ce = P["croisillon"]
    # variante praticable (INTEGRATION-PROPS.md) : croisillons seulement à l'étage (3 → 6 m) ; le rez-de-chaussée reste
    # libre (2.9 m sous le plateau du bas, entre les poteaux), comme les collisions : on y passe, les goules aussi
    levels = [(P["niveaux"][0] + 0.15, P["niveaux"][1] - 0.4)]
    Lg = P["ligature"]
    # jambes de force sous le plateau du bas, dans le plan avant et arrière : du poteau (à 2.1 m) au longeron (à 0.7 m)
    zl0 = P["niveaux"][0] - Pl["e"] - P["plancher"] - lh
    for y in ys:
        for x in xs:
            for d in (-1, 1):
                if abs(x + d * 0.7) > hw - p / 2:
                    continue
                N.beam(B, (x + d * p / 2, y, zl0 - 0.75), (x + d * 0.7, y, zl0 - 0.02), 0.09, 0.09, "bois", up=(0, 1, 0))
    for z0, z1 in levels:
        for sx in (-1, 1):
            x = sx * (hw - p - ce / 2)            # contre la face intérieure des poteaux : l'emprise reste de 4 m
            for ya, yb in ((-hd + p, hd - p), (hd - p, -hd + p)):
                N.beam(B, (x, ya, z0), (x, yb, z1), ce, cw, "bois", up=(1, 0, 0))
            N.obox(B, Vector((x, 0, (z0 + z1) / 2)), (1, 0, 0), (0, 1, 0), (0, 0, 1), ce / 2 + Lg["ep"], cw * 0.9, Lg["h"] / 2, "corde")
        y = hd - p - ce / 2
        for xa, xb in ((-hw + p, -p / 2), (p / 2, hw - p)):
            N.beam(B, (xa, y, z0), (xb, y, z1), ce, cw, "bois", up=(0, 1, 0))
            N.beam(B, (xb, y, z0), (xa, y, z1), ce, cw, "bois", up=(0, 1, 0))
            N.obox(B, Vector(((xa + xb) / 2, y, (z0 + z1) / 2)), (1, 0, 0), (0, 1, 0), (0, 0, 1), cw * 0.9, ce / 2 + Lg["ep"], Lg["h"] / 2, "corde")
    # ligatures autour des poteaux, sous chaque plateau
    for zt in P["niveaux"]:
        zc = zt - Pl["e"] - P["plancher"] - lh - Lg["h"] / 2 - 0.02
        for x in xs:
            for y in ys:
                N.obox(B, Vector((x, y, zc)), (1, 0, 0), (0, 1, 0), (0, 0, 1), p / 2 + Lg["ep"], p / 2 + Lg["ep"], Lg["h"] / 2, "corde",
                       skip=("-z", "+z"))
    # échelle sur la face avant
    E = P["echelle"]
    mw, me = E["montant"]
    ye = E["y"]
    for x in E["x"]:
        B.box((x - mw / 2, ye - me / 2, 0), (x + mw / 2, ye + me / 2, top), "bois", skip=("-z",))
    nb = int(top / E["pas"])
    for i in range(1, nb):
        z = i * E["pas"]
        b = E["barreau"]
        B.box((E["x"][0] + mw / 2, ye - b / 2, z - b / 2), (E["x"][1] - mw / 2, ye + b / 2, z + b / 2), "bois", skip=("-x", "+x"))
    # pivot au centre de l'emprise réelle (l'échelle dépasse un peu à l'avant)
    ys_all = [v.co.y for v in B.bm.verts]
    xs_all = [v.co.x for v in B.bm.verts]
    cx, cy = (min(xs_all) + max(xs_all)) / 2, (min(ys_all) + max(ys_all)) / 2
    N.orient(B, Matrix.Translation((-cx, -cy, 0)))
    ob = B.finish("SM_Echafaudage", mats)
    print("échafaudage : %d triangles ; recentrage (%.3f, %.3f)" % (C.tri_count(ob), cx, cy))
    return [ob]


if __name__ == "__main__":
    N.run("SM_Echafaudage", build, PARAMS, views=("face", "cote", "34"))
