# -*- coding: utf-8 -*-
"""SM_Grille : grille de cachot (Prop_Grille, 3 × 3 × 0.2 m), d'après concept_grille_cachot_v01 (le mur, le
personnage et le décor de l'image sont ignorés).

Cadre plat (montants et rails haut et bas) ; barreaux carrés de 0.06 m tous les 0.15 m ; 3 traverses plates
rivetées sur la face avant : basse (0.55 m), haute (2.45 m) et rail du haut (le concept montre deux traverses et
le cadre) ; deux barreaux tordus dans le vantail gauche, au même endroit que le concept (entre 0.6 et 1 m du bord
gauche), bombés vers la gauche et un peu vers l'avant entre les deux traverses ; section de porte au milieu :
montant central double, plaque de serrure et cadenas à 1.25 m, charnières sur les montants extérieurs.

Pivot : au sol, au centre. Face avant vers -Y (Blender) = +Z (Unity).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_grille.py"
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
    largeur=3.0, hauteur=3.0,
    cadre=dict(l=0.08, p=0.12),                  # largeur vue de face, profondeur
    barreau=0.06, pas=0.15,
    traverses=dict(z=(0.55, 2.45, 2.92), h=0.1, e=0.02, rivets_pas=0.3),
    centre=dict(l=0.07),                         # chaque montant du milieu (porte à deux vantaux)
    tordus=dict(x=(-0.89, -0.74), fleche=(0.13, 0.07), avant=(0.05, 0.02), z=(0.62, 2.38), segs=6),
    serrure=dict(z=1.25, plaque=(0.26, 0.03, 0.26), cadenas=(0.08, 0.03, 0.1)),
    charnieres=dict(z=(0.45, 2.55), taille=(0.06, 0.05, 0.12)),
)


def build(P):
    mats = N.materials()
    B = N.builder()
    W, H = P["largeur"], P["hauteur"]
    cl, cp = P["cadre"]["l"], P["cadre"]["p"]
    hw = W / 2
    # cadre : montants extérieurs, rails haut et bas
    for sx in (-1, 1):
        x0 = sx * hw
        B.box((min(x0, x0 - sx * cl), -cp / 2, 0), (max(x0, x0 - sx * cl), cp / 2, H), "fer", bevel=0.005)
    B.box((-hw + cl, -cp / 2, 0), (hw - cl, cp / 2, cl), "fer", skip=("-z",))
    B.box((-hw + cl, -cp / 2, H - cl), (hw - cl, cp / 2, H), "fer")
    # montant central double (porte)
    ce = P["centre"]["l"]
    for sx in (-1, 1):
        B.box((min(0, sx * ce) + 0.004 * sx, -cp / 2, cl), (max(0, sx * ce) + 0.004 * sx, cp / 2, H - cl), "fer")
    # barreaux droits (sauf les tordus)
    b, pas = P["barreau"], P["pas"]
    T = P["tordus"]
    xs = []
    x = -hw + cl + pas / 2 + 0.005
    while x < hw - cl - b / 2:
        if abs(x) > ce + b / 2 + 0.01:
            xs.append(round(x, 4))
        x += pas
    bent = []
    for x in xs:
        if any(abs(x - xt) < 0.03 for xt in T["x"]):
            bent.append(x)
            continue
        B.box((x - b / 2, -b / 2, cl), (x + b / 2, b / 2, H - cl), "fer", skip=("-z", "+z"))
    # barreaux tordus : tronçons droits en bas et en haut, flèche vers la gauche et un peu vers l'avant au milieu
    z0, z1 = T["z"]
    for i, x in enumerate(sorted(bent)):
        fl, av = T["fleche"][i % 2], T["avant"][i % 2]
        pts = [Vector((x, 0, cl))]
        for s in range(T["segs"] + 1):
            u = s / T["segs"]
            z = z0 + (z1 - z0) * u
            k = math.sin(math.pi * u) ** 1.5
            pts.append(Vector((x - fl * k, -av * k, z)))
        pts.append(Vector((x, 0, H - cl)))
        C.tube(B, pts, b / 2 * 1.41, 4, "fer", caps=False, phase=math.pi / 4, up=Vector((0, -1, 0)))
    # traverses plates rivetées (face avant)
    Tr = P["traverses"]
    for z in Tr["z"]:
        y0 = -max(b, cp) / 2
        if z > H - 0.2:     # rail du haut : plat riveté sur le cadre
            y0 = -cp / 2
        B.box((-hw + cl * 0.5, y0 - Tr["e"], z - Tr["h"] / 2), (hw - cl * 0.5, y0, z + Tr["h"] / 2), "fer", skip=("+y",))
        nr = int((W - 0.2) / Tr["rivets_pas"]) + 1
        for k in range(nr):
            xr = -hw + 0.1 + k * (W - 0.2) / (nr - 1)
            N.rivet(B, (xr, y0 - Tr["e"], z), (0, -1, 0), 0.018, 0.012, "fer")
    # charnières sur les montants extérieurs
    Ch = P["charnieres"]
    sx_, sy_, sz_ = Ch["taille"]
    for sx in (-1, 1):
        for z in Ch["z"]:
            xc = sx * (hw - cl - sx_ / 2 + 0.01)
            B.box((xc - sx_ / 2, -cp / 2 - sy_, z - sz_ / 2), (xc + sx_ / 2, -cp / 2, z + sz_ / 2), "fer", skip=("+y",))
    # serrure : plaque à cheval sur le montant central, cadenas qui pend devant
    S = P["serrure"]
    px, py, pz = S["plaque"]
    yf = -cp / 2
    B.box((-px / 2, yf - py, S["z"] - pz / 2), (px / 2, yf, S["z"] + pz / 2), "fer", skip=("+y",), bevel=0.006)
    N.rivet(B, (-px / 2 + 0.03, yf - py, S["z"] + pz / 2 - 0.03), (0, -1, 0), 0.012, 0.008, "fer")
    N.rivet(B, (px / 2 - 0.03, yf - py, S["z"] - pz / 2 + 0.03), (0, -1, 0), 0.012, 0.008, "fer")
    cx_, cy_, cz_ = S["cadenas"]
    yc = yf - py - 0.02
    B.box((-cx_ / 2, yc - cy_, S["z"] - 0.06 - cz_), (cx_ / 2, yc, S["z"] - 0.06), "fer", bevel=0.005)
    sh = [Vector((0.028 * math.cos(math.pi * i / 4), yc - cy_ / 2, S["z"] - 0.06 + 0.04 * math.sin(math.pi * i / 4))) for i in range(5)]
    C.tube(B, sh, 0.009, 4, "fer", caps=False, up=Vector((0, -1, 0)))
    ob = B.finish("SM_Grille", mats, origin=(0.0, -0.04, 0.0))   # centre de l'emprise (le cadenas dépasse à l'avant)
    ob.location = (0.0, 0.0, 0.0)
    print("grille : %d triangles ; %d barreaux droits, %d tordus" % (C.tri_count(ob), len(xs) - len(bent), len(bent)))
    return [ob]


if __name__ == "__main__":
    N.run("SM_Grille", build, PARAMS, views=("face", "cote", "34"))
