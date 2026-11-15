# -*- coding: utf-8 -*-
"""SM_Treuil (+ SM_Treuil_Manivelle) : treuil du lustre géant de l'arène du boss (fixé au mur nord).

Plaque murale arrière plate (fer, 2.5 × 2.5 m) ; deux montants en bois avec jambes de force vers la plaque ;
tambour en bois de Ø 1.2 × 2 m, joues et 3 frettes en fer ; axe en fer ; chaîne enroulée en hélice sur le tambour
et brin qui part vers le haut (vers le lustre). Manivelle séparée SM_Treuil_Manivelle (bras en fer, poignée en
bois), pivot sur l'axe du tambour : le jeu la fait tourner autour de X. Encombrement ≈ 2.5 × 2.5 × 1.5 m.

Pivot : au sol, au centre du dos de la plaque (contre le mur). Avant vers -Y (Blender) = +Z (Unity).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_treuil.py"
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
    plaque=(2.5, 2.5, 0.08),
    axe=dict(y=-0.82, z=1.3, r=0.06),
    tambour=dict(r=0.6, longueur=2.0, segs=20, joue_r=0.68, joue_e=0.08, frettes=(-0.62, 0.0, 0.62), frette=(0.62, 0.08)),
    montant=dict(x=1.14, l=0.2, p=0.3),
    chaine=dict(x0=-0.78, x1=0.5, tours=6, pts=12, fil=0.03, maillons=8, pas=0.13),
    manivelle=dict(x=1.36, bras=0.5, section=0.07, poignee=(0.045, 0.3)),
)


def build(P):
    mats = N.materials()
    B = N.builder()
    pw, ph, pe = P["plaque"]
    B.box((-pw / 2, -pe, 0.0), (pw / 2, 0.0, ph), "fer", bevel=0.02)
    for x in (-pw / 2 + 0.15, pw / 2 - 0.15):
        for z in (0.15, ph - 0.15):
            N.rivet(B, (x, -pe, z), (0, -1, 0), 0.04, 0.03, "fer")
    A, T, Mo = P["axe"], P["tambour"], P["montant"]
    ya, za = A["y"], A["z"]
    # montants et jambes de force
    for sx in (-1, 1):
        x = sx * Mo["x"]
        B.box((x - Mo["l"] / 2, ya - Mo["p"] / 2, 0), (x + Mo["l"] / 2, ya + Mo["p"] / 2, za + 0.25), "bois", skip=("-z",))
        N.beam(B, (x, ya + Mo["p"] / 2 - 0.05, za + 0.1), (x, -pe, za + 0.85), 0.14, 0.14, "bois", up=(1, 0, 0))
    # axe
    C.tube(B, [Vector((-Mo["x"] - 0.15, ya, za)), Vector((P["manivelle"]["x"] + 0.03, ya, za))], A["r"], 8, "fer",
           up=Vector((0, 0, 1)))
    # tambour : construit autour de Z local (dans le même constructeur), puis seuls ses sommets sont couchés selon X
    import bmesh
    Bt = B
    B.bm.verts.ensure_lookup_table()
    n0 = len(B.bm.verts)
    L, R, seg = T["longueur"], T["r"], T["segs"]
    C.revolve(Bt, [(R, -L / 2 + T["joue_e"]), (R, L / 2 - T["joue_e"])], seg, "bois")
    for zc in (-L / 2 + T["joue_e"] / 2, L / 2 - T["joue_e"] / 2):
        e = T["joue_e"] / 2
        C.revolve(Bt, [(A["r"] + 0.02, zc - e), (T["joue_r"], zc - e), (T["joue_r"], zc + e), (A["r"] + 0.02, zc + e), (A["r"] + 0.02, zc - e)],
                  seg, "fer")
    fr, fw = T["frette"]
    for zc in T["frettes"]:
        C.revolve(Bt, [(R - 0.005, zc - fw / 2), (fr, zc - fw / 2), (fr, zc + fw / 2), (R - 0.005, zc + fw / 2)], seg, "fer")
    # chaîne enroulée (hélice) : x local = axe du tambour
    Ch = P["chaine"]
    n = Ch["tours"] * Ch["pts"]
    rc = R + Ch["fil"]
    helix = [Vector((rc * math.cos(2 * math.pi * i / Ch["pts"]), rc * math.sin(2 * math.pi * i / Ch["pts"]),
                     Ch["x0"] + (Ch["x1"] - Ch["x0"]) * i / n)) for i in range(n + 1)]
    C.tube(Bt, helix, Ch["fil"], 4, "fer", up=Vector((0, 0, 1)))
    # tambour couché : Z local -> X ; centre sur l'axe (avant le calcul des UV)
    M = Matrix.Translation((0, ya, za)) @ Matrix.Rotation(math.radians(90), 4, "Y")
    B.bm.verts.ensure_lookup_table()
    bmesh.ops.transform(B.bm, matrix=M, verts=[B.bm.verts[i] for i in range(n0, len(B.bm.verts))])
    # brin qui monte vers le lustre : maillons alternés, depuis le haut du tambour, côté avant
    xb = M @ Vector((0, 0, Ch["x1"]))
    top = Vector((xb.x, ya, za + R + Ch["fil"]))
    d = 0.011
    hl, hw = (Ch["pas"] + 4 * d) / 2 - d, 0.045 - d
    for i in range(Ch["maillons"]):
        cz = top.z + 0.05 + i * Ch["pas"]
        pts = []
        for k in range(6):
            t = 2 * math.pi * k / 6 + math.pi / 2
            a, c = hw * math.cos(t), hl * math.sin(t)
            pts.append(Vector((top.x, ya + a, cz + c)) if i % 2 == 0 else Vector((top.x + a, ya, cz + c)))
        C.tube(B, pts, d * 1.6, 4, "fer", closed=True, up=Vector((1, 0, 0)) if i % 2 == 0 else Vector((0, 1, 0)), phase=math.pi / 4)
    treuil = B.finish("SM_Treuil", mats)

    # manivelle (objet séparé), pivot sur l'axe
    Mv = P["manivelle"]
    B2 = N.builder()
    s = Mv["section"]
    B2.box((-s / 2, -s / 2, -0.06), (s / 2, s / 2, Mv["bras"]), "fer")
    pr, pl = Mv["poignee"]
    C.tube(B2, [Vector((0, 0, Mv["bras"] - 0.03)), Vector((pl, 0, Mv["bras"] - 0.03))], pr, 8, "bois", up=Vector((0, 0, 1)))
    C.tube(B2, [Vector((-0.04, 0, 0)), Vector((0.06, 0, 0))], 0.09, 8, "fer", up=Vector((0, 0, 1)))   # moyeu
    manivelle = B2.finish("SM_Treuil_Manivelle", mats)
    manivelle.location = (Mv["x"], ya, za)
    F.attach(manivelle, treuil)
    print("treuil %d, manivelle %d triangles" % (C.tri_count(treuil), C.tri_count(manivelle)))
    return [treuil, manivelle]


if __name__ == "__main__":
    N.run("SM_Treuil", build, PARAMS, views=("face", "cote", "34"))
