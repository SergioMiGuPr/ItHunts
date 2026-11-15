# -*- coding: utf-8 -*-
"""SM_Tapisserie (+ SM_Tapisserie_Tringle) : tapisserie murale (Prop_Tapisserie, 2.6 × 4 × 0.08 m).

Un plan de 2.6 × 4 m qui porte le tissu de texture_tapisserie_v02_alpha (UV recadrées sur le tissu, sans la
tringle peinte de l'image ; la transparence découpe les trous et le bord déchiré), avec une ondulation verticale douce (3.5 plis, plus marqués vers le
bas) ; matériau M_Tapisserie (découpe alpha, double face, réglés dans le .mat d'Unity). Tringle en bois
SM_Tapisserie_Tringle (Ø 0.05 × 2.9 m) avec deux embouts, objet séparé (même pivot).

Pivot : en haut, au centre de la tringle (accrochée au mur). Le mur est le plan y = 0 (Blender) ; la
tapisserie pend devant, vers -Y (+Z Unity).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_tapisserie.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("n1_props", None)
import n1_props as N
C, F = N.C, N.F
import bpy
from mathutils import Vector

PARAMS = dict(
    largeur=2.6, hauteur=4.0, plis=3.5, amplitude=(0.012, 0.022), nx=28, nz=4,
    tringle=dict(r=0.025, longueur=2.9, cotes=8, y=-0.035, embout_r=0.04),
    texture="texture_tapisserie_v02_alpha.png",
    # recadrage UV sur le tissu : l'image (428 × 678 px) porte aussi une tringle peinte (lignes 0 à 28) et des
    # marges transparentes ; le tissu va de x = 36 à 390 px, à partir de la ligne 30
    uv_cadre=(36 / 428, 0.0, 390 / 428, 1 - 30 / 678),
)


def tapestry_material(path):
    """M_Tapisserie : couleur et alpha de l'image (découpe), double face."""
    mat = C.make_material("M_Tapisserie", path)
    nt = mat.node_tree
    tex = next(n for n in nt.nodes if n.type == "TEX_IMAGE")
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    mat.use_backface_culling = False
    for attr, val in (("surface_render_method", "DITHERED"), ("blend_method", "CLIP")):
        try:
            setattr(mat, attr, val)
        except (AttributeError, TypeError):
            pass
    return mat


def build(P):
    mats = N.materials()
    mats["tapisserie"] = tapestry_material(os.path.join(C.TEX_DIR, P["texture"]))
    T = P["tringle"]
    yc = T["y"]
    W, H = P["largeur"], P["hauteur"]
    nx, nz = P["nx"], P["nz"]
    a0, a1 = P["amplitude"]
    lam = W / P["plis"]

    def pos(i, j):
        x = -W / 2 + W * i / nx
        z = -H * j / nz                       # j = 0 en haut (z = 0, axe de la tringle)
        amp = a0 + (a1 - a0) * (j / nz)
        return Vector((x, yc + amp * math.sin(2 * math.pi * (x + W / 2) / lam), z))

    B = N.builder()
    B.new_shell()
    for i in range(nx):
        for j in range(nz):
            q = [pos(i, j + 1), pos(i + 1, j + 1), pos(i + 1, j), pos(i, j)]
            u0, v0, u1, v1 = P["uv_cadre"]
            U_ = lambda a: u0 + (u1 - u0) * a
            V_ = lambda b: v0 + (v1 - v0) * b
            uv = [(U_(i / nx), V_(1 - (j + 1) / nz)), (U_((i + 1) / nx), V_(1 - (j + 1) / nz)),
                  (U_((i + 1) / nx), V_(1 - j / nz)), (U_(i / nx), V_(1 - j / nz))]
            B.face(q, "tapisserie", (0, -1, 0), uv)
    cloth = B.finish("SM_Tapisserie", mats, sharp_angle=80.0)

    B2 = N.builder()
    L = T["longueur"]
    C.tube(B2, [Vector((-L / 2, yc, 0)), Vector((L / 2, yc, 0))], T["r"], T["cotes"], "bois", up=Vector((0, 0, 1)))
    for sx in (-1, 1):   # embouts en bulbe
        x0 = sx * L / 2
        prof = [(T["r"] * 0.9, 0.0), (T["embout_r"], 0.03), (T["embout_r"] * 0.8, 0.06), (0.012, 0.075)]
        rings = []
        for r, d in prof:
            rings.append([Vector((x0 + sx * d, yc + r * math.cos(2 * math.pi * k / 8), r * math.sin(2 * math.pi * k / 8))) for k in range(8)])
        B2.sweep(rings, "bois", caps=(False, True))
    rod = B2.finish("SM_Tapisserie_Tringle", mats)
    # pivot au centre de la tringle : les deux objets ont leur origine en (0, yc, 0)
    for ob in (cloth, rod):
        ob.data.transform(__import__("mathutils").Matrix.Translation((0, -yc, 0)))
    F.attach(rod, cloth)
    print("tapisserie %d, tringle %d triangles" % (C.tri_count(cloth), C.tri_count(rod)))
    return [cloth, rod]


if __name__ == "__main__":
    N.run("SM_Tapisserie", build, PARAMS, views=("face", "cote", "34"))
