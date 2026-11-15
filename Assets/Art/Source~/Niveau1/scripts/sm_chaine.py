# -*- coding: utf-8 -*-
"""SM_Chaine : chaîne verticale (Prop_Chaine, 0.2 × 4 × 0.2 m) en fer rouillé.

Maillons ovales (pas de 0.13 m, fil de Ø 0.022 m, 0.174 × 0.09 m hors tout) alternés à 90°, accrochés à un
anneau de fixation en haut (plan de face). 29 maillons : ≈ 3.9 m, sous le budget de 1 500 triangles (un pas de
0.12 m aurait demandé 32 maillons et dépassé le budget).

Pivot : en haut (sommet de l'anneau de fixation).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_chaine.py"
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
    pas=0.13, fil=0.011, largeur=0.09, maillons=29, points=6, cotes=4,
    anneau=dict(r=0.05, fil=0.012, points=12),
)


def build(P):
    mats = N.materials()
    B = N.builder()
    A = P["anneau"]
    zc = -(A["r"] + A["fil"])
    ring = [Vector((A["r"] * math.cos(2 * math.pi * k / A["points"]), 0.0, zc + A["r"] * math.sin(2 * math.pi * k / A["points"])))
            for k in range(A["points"])]
    C.tube(B, ring, A["fil"], 4, "fer", closed=True, up=Vector((0, 1, 0)))
    p, d = P["pas"], P["fil"]
    hl = (p + 2 * 2 * d) / 2 - d          # demi-longueur de l'axe du fil
    hw = P["largeur"] / 2 - d
    z_first = zc - A["r"] + A["fil"] - (hl - d)     # le premier maillon passe dans le bas de l'anneau
    for i in range(P["maillons"]):
        cz = z_first - i * p
        pts = []
        n = P["points"]
        for k in range(n):
            t = 2 * math.pi * k / n + math.pi / 2
            a, c = hw * math.cos(t), hl * math.sin(t)
            pts.append(Vector((0, a, cz + c)) if i % 2 == 0 else Vector((a, 0, cz + c)))
        C.tube(B, pts, d, P["cotes"], "fer", closed=True, up=Vector((1, 0, 0)) if i % 2 == 0 else Vector((0, 1, 0)),
               phase=math.pi / 4)
    ob = B.finish("SM_Chaine", mats)
    print("chaîne : %d triangles, %d maillons" % (C.tri_count(ob), P["maillons"]))
    return [ob]


if __name__ == "__main__":
    N.run("SM_Chaine", build, PARAMS, views=("face", "cote", "34"))
