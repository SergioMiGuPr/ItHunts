# -*- coding: utf-8 -*-
"""SM_Pont_Egout_4m et SM_Pont_Egout_14m : pont d'égout en arc surbaissé, d'après fiche3d_pont_egout_v01.

Le pont franchit un canal de 6 m (eau à -1.2 m, fond à -4 m). Tablier plat à la cote 0 (pas de
bombé), au niveau des quais. Le pont s'étend sur X (longueur 8 m), sa largeur (tablier) sur Y.
Pivot : au centre du tablier, à la cote 0. Les parapets sont compris dans la largeur.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_pont_egout.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, importlib
sys.dont_write_bytecode = True          # pas de __pycache__ dans Assets/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("it_common", None)   # recharge toujours la version du dossier courant
import it_common as C
from mathutils import Vector

PARAMS = dict(
    longueur=8.0,          # selon X (d'un quai à l'autre)
    portee=6.0,            # largeur du canal = portée de l'arc
    largeur=4.0,           # largeur du tablier (Y), parapets compris : 4 ou 14
    intrados_cle=0.6,      # intrados de l'arc sous le tablier, au sommet
    fleche=1.5,            # flèche de l'arc (surbaissé)
    fond=-4.0,             # fond du canal : bas des culées
    parapet=(0.6, 0.4),    # hauteur, épaisseur
    corniche=0.15,         # bandeau sous le parapet (descend sous le tablier)
    rouleau=0.4,           # épaisseur des voussoirs de tête
    saillie=0.08,          # saillie des voussoirs et du bandeau sur la face
    voussoirs=13,
    segments=16,           # segments de l'intrados
    joint=0.02,            # écart entre voussoirs
)
VARIANTES = [("SM_Pont_Egout_4m", 4.0), ("SM_Pont_Egout_14m", 14.0)]


def build(P):
    mats = {k: C.tile_material(k) for k in ("pierre", "egout", "dalles")}
    B = C.Builder()
    for k in mats:
        B.set_uv_params(k, *C.TILES[k][1:])
    L, s, W = P["longueur"] / 2, P["portee"] / 2, P["largeur"]
    zk = -P["intrados_cle"]
    f = P["fleche"]
    R = (s * s + f * f) / (2 * f)
    zc = zk - R                       # centre de l'arc
    phi = math.asin(s / R)            # demi-angle
    z_s = zk - f                      # naissance
    fond = P["fond"]
    sa = P["saillie"]
    yf = W / 2 - sa                   # faces de tête (tympans)
    N = P["segments"]
    arc = [(R * math.sin(-phi + 2 * phi * i / N), zc + R * math.cos(-phi + 2 * phi * i / N)) for i in range(N + 1)]

    # ---- corps : faces de tête (polygone avec l'ouverture de l'arc par le bas)
    poly = [(-L, fond), (-s, fond)] + arc + [(s, fond), (L, fond), (L, 0.0), (-L, 0.0)]
    B.new_shell()
    for y, n in ((-yf, (0, -1, 0)), (yf, (0, 1, 0))):
        B.face([(x, y, z) for x, z in poly], "pierre", n)
    # dessus du tablier (dalles)
    B.face([(-L, -yf, 0), (L, -yf, 0), (L, yf, 0), (-L, yf, 0)], "dalles", (0, 0, 1))
    # extrémités contre les quais (pierre)
    for sx in (-1, 1):
        B.face([(sx * L, -yf, fond), (sx * L, yf, fond), (sx * L, yf, 0), (sx * L, -yf, 0)], "pierre", (sx, 0, 0))
    # culées côté canal (maçonnerie d'égout)
    for sx in (-1, 1):
        B.face([(sx * s, -yf, fond), (sx * s, yf, fond), (sx * s, yf, z_s), (sx * s, -yf, z_s)], "egout", (-sx, 0, 0))
    # douelle (dessous de l'arc) : UV qui suivent la courbe
    tw, th = C.TILES["egout"][1:]
    for i in range(N):
        (x0, z0), (x1, z1) = arc[i], arc[i + 1]
        u0, u1 = R * 2 * phi * i / N / tw, R * 2 * phi * (i + 1) / N / tw
        pts = [(x0, -yf, z0), (x1, -yf, z1), (x1, yf, z1), (x0, yf, z0)]
        nm = Vector((-(x0 + x1) / 2, 0, zc - (z0 + z1) / 2))     # vers le centre de l'arc (vers le bas)
        B.face(pts, "egout", nm, [(u0, -yf / th), (u1, -yf / th), (u1, yf / th), (u0, yf / th)])

    # ---- voussoirs de tête (en saillie sur les deux faces)
    nv = P["voussoirs"]
    Ro = R + P["rouleau"]
    gap = P["joint"] / R
    for side in (-1, 1):
        y0, y1 = side * W / 2, side * yf
        for i in range(nv):
            a0 = -phi + 2 * phi * i / nv + (gap / 2 if i > 0 else 0)
            a1 = -phi + 2 * phi * (i + 1) / nv - (gap / 2 if i < nv - 1 else 0)
            q = [(R * math.sin(a0), zc + R * math.cos(a0)), (R * math.sin(a1), zc + R * math.cos(a1)),
                 (Ro * math.sin(a1), zc + Ro * math.cos(a1)), (Ro * math.sin(a0), zc + Ro * math.cos(a0))]
            B.new_shell()
            # UV qui suivent le rouleau : u le long de l'arc, v en travers (assises rayonnantes)
            ptw, pth = C.TILES["pierre"][1:]
            rm = (R + Ro) / 2
            quv = [(a0 * rm / ptw, 0.0), (a1 * rm / ptw, 0.0), (a1 * rm / ptw, P["rouleau"] / pth), (a0 * rm / ptw, P["rouleau"] / pth)]
            B.face([(x, y0, z) for x, z in q], "pierre", (0, side, 0), quv)
            for k in range(4):
                (ax, az), (bx, bz) = q[k], q[(k + 1) % 4]
                mx, mz = (ax + bx) / 2, (az + bz) / 2
                cxm, czm = sum(p[0] for p in q) / 4, sum(p[1] for p in q) / 4
                B.face([(ax, y0, az), (bx, y0, bz), (bx, y1, bz), (ax, y1, az)], "pierre", (mx - cxm, 0, mz - czm))

    # ---- parapets + bandeau (un bloc par côté, compris dans la largeur)
    ph, pe = P["parapet"]
    for side in (-1, 1):
        ya, yb = sorted((side * W / 2, side * (W / 2 - pe)))
        B.box((-L, ya, -P["corniche"]), (L, yb, ph), "pierre", skip=("-z",), bevel=0.03)

    ob = B.finish(P["nom"], mats)
    print("%s : R=%.3f naissance z=%.3f, sommet intrados z=%.3f" % (P["nom"], R, z_s, zk))
    return [ob]


if __name__ == "__main__":
    for nom, larg in VARIANTES:
        P = dict(PARAMS, largeur=larg, nom=nom)
        C.run_piece(nom, lambda p=P: build(p), P, nom)
