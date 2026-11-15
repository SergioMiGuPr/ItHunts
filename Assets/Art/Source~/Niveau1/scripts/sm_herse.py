# -*- coding: utf-8 -*-
"""SM_Herse (+ SM_Herse_Grille) : herse de l'arène du boss (ART-HANDOFF, section arène : piliers de 28 m, passage
de 10 m, grille de 10 × 12 m).

Deux piliers de pierre (section 3 × 3 m, à valider) de 28 m de haut encadrant un passage de 10 m : socle, cordons
tous les 7 m, corniche ; une rainure (0.4 × 0.3 m) sur toute la hauteur de la face intérieure guide la grille.
Grille de fer séparée SM_Herse_Grille, 10 × 12 m : 21 barreaux de 0.15 m tous les 0.5 m, traverses tous les 2 m,
pointes en bas ; pivot en bas au centre (pointe des barreaux au sol) : le jeu la fait monter et descendre sur Y.
Textures : mur_pierre (M_Pierre) et fer_rouille (M_FerRouille).

Pivot de la herse : au sol, au centre du passage. Avant vers -Y (Blender) = +Z (Unity).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_herse.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("n1_props", None)
import n1_props as N
C, F = N.C, N.F
from mathutils import Vector

PARAMS = dict(
    passage=10.0, hauteur=28.0,
    pilier=(3.0, 3.0), rainure=(0.4, 0.3),
    socle=dict(debord=0.3, h=1.5), cordons=dict(z=(7.0, 14.0, 21.0), debord=0.15, h=0.35),
    corniche=dict(debord=0.35, h=0.7),
    grille=dict(largeur=10.0, hauteur=12.0, barreau=0.15, pas=0.5, traverses_pas=2.0, traverse=(0.22, 0.25),
                pointe=0.45),
    v_decalage=0.0104,
)


def build(P):
    mats = N.materials()
    B = N.builder()
    B.set_uv_params("pierre", 4.0, 3.0, P["v_decalage"])
    hp = P["passage"] / 2
    pw, pd = P["pilier"]
    rw, rd = P["rainure"]
    H = P["hauteur"]
    S, Co, Cn = P["socle"], P["cordons"], P["corniche"]
    for sx in (-1, 1):
        xi, xo = sx * hp, sx * (hp + pw)
        x0, x1 = min(xi, xo), max(xi, xo)
        # fût du pilier avec la rainure sur la face intérieure (polygone en plan, extrudé)
        poly = [(xo, -pd / 2), (xi, -pd / 2), (xi, -rw / 2), (xi + sx * rd, -rw / 2), (xi + sx * rd, rw / 2), (xi, rw / 2),
                (xi, pd / 2), (xo, pd / 2)]
        C.fit_stones(B, B.prism_z(poly, S["h"], H - Cn["h"], "pierre", skip_bottom=True, skip_top=True))
        d = S["debord"]
        C.fit_stones(B, B.box((x0 - d, -pd / 2 - d, 0), (x1 + d, pd / 2 + d, S["h"]), "pierre", skip=("-z",), bevel=0.06))
        for z in Co["z"]:
            d = Co["debord"]
            B.box((x0 - d, -pd / 2 - d, z), (x1 + d, pd / 2 + d, z + Co["h"]), "pierre", bevel=0.04)
        d = Cn["debord"]
        C.fit_stones(B, B.box((x0 - d, -pd / 2 - d, H - Cn["h"]), (x1 + d, pd / 2 + d, H), "pierre", bevel=0.06))
    piliers = B.finish("SM_Herse", mats)

    # grille de fer (objet séparé), pivot en bas au centre
    G = P["grille"]
    B2 = N.builder()
    W, Hg, b = G["largeur"], G["hauteur"], G["barreau"]
    n = int(round(W / G["pas"])) + 1
    for i in range(n):
        x = -W / 2 + b / 2 + (W - b) * i / (n - 1)
        B2.box((x - b / 2, -b / 2, G["pointe"]), (x + b / 2, b / 2, Hg), "fer", skip=("-z",))
        B2.new_shell()
        base = [Vector((x - b / 2, -b / 2, G["pointe"])), Vector((x + b / 2, -b / 2, G["pointe"])),
                Vector((x + b / 2, b / 2, G["pointe"])), Vector((x - b / 2, b / 2, G["pointe"]))]
        tip = Vector((x, 0, 0))
        for k in range(4):
            tri = [base[k], base[(k + 1) % 4], tip]
            B2.face(tri, "fer", sum(tri, Vector()) / 3 - Vector((x, 0, G["pointe"] * 0.6)))
    tw, td = G["traverse"]
    z = G["pointe"] + 0.6
    while z < Hg - 0.3:
        B2.box((-W / 2, -td / 2, z - tw / 2), (W / 2, td / 2, z + tw / 2), "fer")
        z += G["traverses_pas"]
    B2.box((-W / 2, -td / 2, Hg - tw), (W / 2, td / 2, Hg), "fer")      # traverse du haut
    grille = B2.finish("SM_Herse_Grille", mats)
    F.attach(grille, piliers)
    print("herse : piliers %d, grille %d triangles (%d barreaux)" % (C.tri_count(piliers), C.tri_count(grille), n))
    return [piliers, grille]


if __name__ == "__main__":
    N.run("SM_Herse", build, PARAMS, views=("face", "cote", "34"))
