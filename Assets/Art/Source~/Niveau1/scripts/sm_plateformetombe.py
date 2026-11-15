# -*- coding: utf-8 -*-
"""SM_PlateformeTombe : plateforme qui s'effondre (arène du boss : 5 plateformes de 2.4 m au-dessus de la fosse).

Dalle de pierre de 2.4 × 0.4 × 2.4 m, dessus plat : 4 morceaux séparés par des fissures marquées (1.5 cm,
sur 0.28 m de profondeur) posés sur une sous-couche continue (rien ne se voit au travers), bords ébréchés
(coins et éclats en creux sur le pourtour). Elle s'effondre en jeu : les fissures annoncent la cassure.

Pivot : au centre du dessous de la dalle.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_plateformetombe.py"
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
    cote=2.4, epaisseur=0.4, sous_couche=0.12, jeu=0.015,
    # point central de la cassure et points des fissures sur les bords (fraction du demi-côté)
    centre=(0.12, -0.08),
    fissures=[(1.0, 0.18), (-0.25, 1.0), (-1.0, -0.5), (0.3, -1.0)],
    # éclats : (côté 0 = -y, 1 = +x, 2 = +y, 3 = -x ; position 0..1 le long du côté, largeur, profondeur)
    eclats=[(0, 0.3, 0.22, 0.07), (1, 0.75, 0.3, 0.09), (2, 0.2, 0.18, 0.06), (3, 0.6, 0.25, 0.08), (0, 0.85, 0.15, 0.05)],
    coins=0.12,
)


def outline(P):
    """Pourtour ébréché (sens trigonométrique), points sur le carré avec coins coupés et éclats en creux."""
    h = P["cote"] / 2
    cn = P["coins"]
    corners = [Vector((h, -h)), Vector((h, h)), Vector((-h, h)), Vector((-h, -h))]   # côté s : du coin s-1 au coin s
    pts = []
    for s in range(4):
        a, b = corners[(s + 3) % 4], corners[s]
        d = (b - a).normalized()
        L = (b - a).length
        nin = Vector((-d.y, d.x))                         # vers l'intérieur (sens trigonométrique)
        seq = [a + d * cn]
        for side, t, w, dep in sorted([e for e in P["eclats"] if e[0] == s], key=lambda e: e[1]):
            p0 = a + d * (L * t - w / 2)
            seq += [p0, a + d * (L * t) + nin * dep, p0 + d * w]
        seq.append(b - d * cn)
        pts += seq
    return pts


def build(P):
    mats = N.materials()
    B = N.builder()
    h, e, sc, jeu = P["cote"] / 2, P["epaisseur"], P["sous_couche"], P["jeu"]
    poly = outline(P)
    # sous-couche continue
    B.prism_z([(p.x, p.y) for p in poly], 0.0, sc, "pierre", skip_bottom=False, skip_top=True)
    # 4 morceaux : le pourtour est découpé par les fissures (du centre vers les points des bords)
    c = Vector(P["centre"]) * h
    ends = [Vector(f) * h for f in P["fissures"]]
    angs = [math.atan2(v.y - c.y, v.x - c.x) for v in ends]
    order = sorted(range(4), key=lambda i: angs[i])
    def ang(p):
        return math.atan2(p.y - c.y, p.x - c.x)
    for k in range(4):
        i0, i1 = order[k], order[(k + 1) % 4]
        a0, a1 = angs[i0], angs[i1]
        if a1 <= a0:
            a1 += 2 * math.pi
        inside = []
        for p in poly:
            a = ang(p)
            while a < a0:
                a += 2 * math.pi
            if a0 < a < a1:
                inside.append((a, p))
        inside.sort(key=lambda t: t[0])
        piece = [ends[i0]] + [p for _, p in inside] + [ends[i1], c]
        # retrait de la moitié du jeu vers l'intérieur du morceau (fissure ouverte)
        cen = sum(piece, Vector((0, 0))) / len(piece)
        piece = [p + (cen - p).normalized() * jeu / 2 for p in piece]
        C.fit_stones(B, B.prism_z([(p.x, p.y) for p in piece], sc - 0.01, e, "pierre", skip_bottom=True))
    ob = B.finish("SM_PlateformeTombe", mats, sharp_angle=30.0)
    print("plateforme : %d triangles" % C.tri_count(ob))
    return [ob]


if __name__ == "__main__":
    N.run("SM_PlateformeTombe", build, PARAMS, views=("face", "cote", "34"))
