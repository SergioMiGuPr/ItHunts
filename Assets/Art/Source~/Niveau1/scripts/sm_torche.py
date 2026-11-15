# -*- coding: utf-8 -*-
"""SM_Torche : torche murale (Prop_Torche et Prop_TorcheEteinte, 1 231 exemplaires : très légère), d'après
concept_torche_v01 et fiche3d_torche_v01 (le décor, le mur et la flamme des images sont ignorés).

Plaque murale carrée en fer rouillé à 4 rivets, dos parfaitement plat (contre le mur) ; bras horizontal et
jambe de force sous le bras ; anneau (collier) ouvert dessus et dessous qui tient le manche ; manche en bois
brut de 0.6 m incliné de 20° vers l'avant (le haut s'écarte du mur) ; tête en chiffons noircis ficelés
(2 tours de corde), sans flamme. Repère FX_Flamme au sommet de la tête (+Z local = hors du mur).

Pivot : au centre du dos de la plaque (sur le mur). +Z (Unity) hors du mur : en Blender, le mur est le plan
y = 0 et la torche avance vers -Y.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_torche.py"
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
    plaque=(0.18, 0.012, 0.22),          # largeur, épaisseur, hauteur
    rivets=dict(dx=0.062, dz=0.08, s=0.011, h=0.008),
    bras=dict(section=0.024, z=0.02, y_anneau=-0.20),
    jambe=dict(section=0.018, z_bas=-0.10, y_haut=-0.13),
    anneau=dict(rin=0.027, rout=0.036, h=0.04, cotes=8),
    manche=dict(r=0.024, cotes=6, bas=0.24, haut=0.36, inclinaison=20.0),   # longueurs sous et au-dessus de l'anneau (0.6 m)
    tete=dict(r=0.043, debut=0.22, fin=0.40, cotes=7, bosses=0.006),        # le long de l'axe depuis l'anneau
    cordes=dict(positions=(0.25, 0.30), r=0.046, fil=0.005),
    fx_dz=0.03,
)


def build(P):
    mats = N.materials()
    B = N.builder()
    w, e, h = P["plaque"]
    # plaque : dos à y = 0 (contre le mur), face avant à y = -e
    B.box((-w / 2, -e, -h / 2), (w / 2, 0.0, h / 2), "fer", bevel=0.003)
    R = P["rivets"]
    for sx in (-1, 1):
        for sz in (-1, 1):
            N.rivet(B, (sx * R["dx"], -e, sz * R["dz"]), (0, -1, 0), R["s"], R["h"], "fer")
    # bras et anneau
    br = P["bras"]
    ya = br["y_anneau"]
    A = P["anneau"]
    M = P["manche"]
    t = math.radians(M["inclinaison"])
    axis = Vector((0, -math.sin(t), math.cos(t)))          # le haut du manche s'écarte du mur
    pa = Vector((0, ya, br["z"]))
    N.beam(B, (0, -e, br["z"]), (0, ya + A["rout"] * 0.9, br["z"]), br["section"], br["section"], "fer")
    N.collar(B, pa, axis, A["rin"], A["rout"], A["h"], A["cotes"], "fer", phase=math.pi / A["cotes"])
    # jambe de force : du bas de la plaque au bras
    jb = P["jambe"]
    N.beam(B, (0, -e, jb["z_bas"]), (0, jb["y_haut"], br["z"] - br["section"] / 2), jb["section"], jb["section"], "fer",
           up=(0, -1, 0))
    # manche (bois brut)
    p0, p1 = pa - axis * M["bas"], pa + axis * M["haut"]
    C.tube(B, [p0, p1], M["r"], M["cotes"], "bois", up=Vector((1, 0, 0)))
    # tête en chiffons : tronc irrégulier (rayons variables), dessus bombé et noirci
    T = P["tete"]
    rings = []
    ref_u = Vector((1, 0, 0))
    ref_v = axis.cross(ref_u).normalized()
    bumps = [1.0, 0.86, 1.05, 0.92, 1.0, 0.88, 1.04]
    for j, s in enumerate((T["debut"], (T["debut"] + T["fin"]) / 2, T["fin"])):
        c = pa + axis * s
        rr = T["r"] * (0.9 if j == 0 else 1.0)
        ring = []
        for k in range(T["cotes"]):
            a = 2 * math.pi * k / T["cotes"] + j * 0.3
            f = bumps[(k + j) % len(bumps)]
            ring.append(c + (ref_u * math.cos(a) + ref_v * math.sin(a)) * rr * f)
        rings.append(ring)
    B.sweep(rings, "chiffon", caps=(True, False))
    top = pa + axis * (T["fin"] + 0.02)
    B.new_shell()
    for k in range(T["cotes"]):
        k2 = (k + 1) % T["cotes"]
        tri = [rings[-1][k], rings[-1][k2], top]
        B.face(tri, "chiffon", sum(tri, Vector()) / 3 - (pa + axis * T["fin"]))
    # cordes : 2 tours ficelés
    Cd = P["cordes"]
    for s in Cd["positions"]:
        c = pa + axis * s
        ring = [c + (ref_u * math.cos(2 * math.pi * k / 8) + ref_v * math.sin(2 * math.pi * k / 8)) * Cd["r"] for k in range(8)]
        C.tube(B, ring, Cd["fil"], 3, "corde", closed=True, up=axis)
    ob = B.finish("SM_Torche", mats)
    fx = F.empty("FX_Flamme", tuple(top + axis * P["fx_dz"]), ob)
    print("torche : %d triangles ; FX_Flamme à %s (Blender)" % (C.tri_count(ob), tuple(round(x, 3) for x in fx.matrix_world.translation)))
    return [ob, fx]


if __name__ == "__main__":
    N.run("SM_Torche", build, PARAMS, views=("face", "cote", "34"))
