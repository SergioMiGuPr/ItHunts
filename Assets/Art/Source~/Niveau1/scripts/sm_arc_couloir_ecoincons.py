# -*- coding: utf-8 -*-
"""SM_Arc_Couloir_Ecoincons : variante de SM_Arc_Couloir dont le contour extérieur est plein (14 × 11 m).

Même anneau en ogive (sm_arc_couloir, mêmes cotes et même pivot : au sol, au milieu entre les deux pieds) ; les deux
écoinçons (entre l'extrados et le cadre) sont remplis par une plaque de pierre de PARAMS["ecoincon_ep"] d'épaisseur,
un peu en retrait de la face de l'anneau (sa mouluration reste en relief). Remplace les blocs de pierre provisoires
que le jeu pose entre l'arc et la voûte.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_arc_couloir_ecoincons.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("sm_arc_couloir", None)
import sm_arc_couloir as A
C = A.C

PARAMS = dict(A.PARAMS)
PARAMS.update(ecoincon_ep=1.2, ecoincon_pas=24)


def spandrels(B, g, P=PARAMS):
    W, H = P["largeur"] / 2, g["H"]
    ep = P["ecoincon_ep"] / 2
    n = P["ecoincon_pas"]
    for sx in (1, -1):
        pts = []
        for k in range(n + 1):                                   # extrados, du pied jusqu'à la clé
            t = g["theta_k"] * k / n
            pts.append((sx * (g["cx"] + g["Re"] * math.cos(t)), g["z0"] + g["Re"] * math.sin(t)))
        pts.append((sx * g["P2"].x, H))                          # long du flanc de la clé jusqu'au sommet
        pts.append((sx * W, H))
        pts.append((sx * W, g["z0"]))
        B.prism_y(pts, -ep, ep, "pierre", bevel=0.02)


def build(P):
    return A.build(P, extra=lambda B, g: spandrels(B, g, P), name="SM_Arc_Couloir_Ecoincons")


if __name__ == "__main__":
    C.run_piece("SM_Arc_Couloir_Ecoincons", build, PARAMS, "SM_Arc_Couloir_Ecoincons")
