# -*- coding: utf-8 -*-
"""SM_LanterneFeu : lanterne de feu (artefact du renard), vue FPS, d'après fiche3d_lanterne_feu_v01.

Hauteur 0.30 m anneau compris, corps (verre et montants) de Ø 0.14 m. Base ronde à deux gradins,
4 montants carrés (à 45°, l'avant reste dégagé pour le jet), verre cylindrique, bandeaux cuivrés
(haut de la base, haut du corps, bord du chapeau, tête du fleuron), chapeau en dôme, fleuron,
anneau de portage dans le plan de face (on le voit en entier de face, comme sur la fiche).

Objets : SM_LanterneFeu (corps, racine, pivot au centre de l'anneau), enfants SM_LanterneFeu_Verre
(M_LanterneFeu_Verre, rouge sang, translucide en jeu ; même pivot) et SM_LanterneFeu_Meche (mèche,
pivot à son pied) ; repères FX_Flamme (au-dessus de la mèche) et FX_Jet (face avant du verre, +Z
vers l'avant). Depuis le 2026-11-16 : SM_LanterneFeu_Flamme (flamme en goutte au-dessus de la mèche, visible à
travers le verre ; M_LanterneFeu_Flamme, émission pilotée par le jeu à la couleur du personnage ; pivot au bout de
la mèche).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sm_lanternefeu.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("fps_common", None)
import fps_common as F
C = F.C
import bpy
from mathutils import Vector

PARAMS = dict(
    segs=16,
    # (r, z) de bas en haut, en m ; z = 0 sous la base
    base=[(0.075, 0.0), (0.075, 0.014), (0.069, 0.014), (0.069, 0.028)],
    cuivre_base=(0.058, 0.071, 0.028, 0.036),      # bandeau : r int, r ext, z0, z1
    cuivre_bas=(0.0745, 0.0765, 0.002, 0.008),     # liseré du gradin du bas
    verre=(0.060, 0.036, 0.182),                    # r, z0, z1
    montant=dict(r=0.0645, cote=0.011, angles=(45, 135, 225, 315)),
    cuivre_haut=(0.054, 0.072, 0.182, 0.192),
    chapeau=[(0.070, 0.192), (0.069, 0.200), (0.064, 0.210), (0.053, 0.219), (0.037, 0.226), (0.018, 0.229)],
    fleuron=dict(r=0.011, z0=0.226, z1=0.244, tete=(0.013, 0.244, 0.249)),
    anneau=dict(rayon=0.022, fil=0.0035, haut=0.300, cotes=8, segs=16),
    support=[(0.024, 0.036), (0.024, 0.046), (0.017, 0.046), (0.017, 0.060)],   # coupelle de la mèche (fer)
    meche=dict(r=0.0018, pts=[(0.0, 0.060), (0.002, 0.068), (0.005, 0.075), (0.003, 0.081)]),
    fx_flamme_dz=0.012,                              # au-dessus du bout de la mèche
    # flamme : profil (r, dz) depuis le bout de la mèche, révolution autour de l'axe de la lanterne
    flamme=[(0.0, -0.004), (0.008, 0.004), (0.013, 0.014), (0.0125, 0.024), (0.009, 0.034), (0.0045, 0.043), (0.0, 0.052)],
    fx_jet_z=0.105,                                  # hauteur du jet sur la face avant du verre
    couleurs=dict(verre="#7A0A12", meche="#17110D", flamme="#FFC27A"),   # fer et cuivre : tuiles peintes (fps_common.PAINT)
    alpha_verre=0.55,
)


def band(B, rin, rout, z0, z1, segs, mat):
    C.revolve(B, [(rin, z0), (rout, z0), (rout, z1), (rin, z1), (rin, z0)], segs, mat)


def build(P):
    col = P["couleurs"]
    mats = {"fer": F.tex_material("M_LanterneFeu_Fer", "T_LanterneFeu_Fer_BaseColor"),
            "cuivre": F.tex_material("M_LanterneFeu_Cuivre", "T_LanterneFeu_Cuivre_BaseColor"),
            "verre": F.flat_material("M_LanterneFeu_Verre", col["verre"], P["alpha_verre"]),
            "meche": F.flat_material("M_LanterneFeu_Meche", col["meche"]),
            "flamme": F.flat_material("M_LanterneFeu_Flamme", col["flamme"])}
    seg = P["segs"]
    A = P["anneau"]
    zc = A["haut"] - A["rayon"] - A["fil"]          # centre de l'anneau = pivot
    pivot = (0.0, 0.0, zc)

    # ---- corps
    B = C.Builder()
    for k in ("fer", "cuivre"):
        B.set_uv_params(k, F.TILE_M, F.TILE_M)      # 5 120 px/m, comme le barillet
    C.revolve(B, P["base"], seg, "fer", cap_bottom=True, cap_top=True)
    band(B, *P["cuivre_base"], seg, "cuivre")
    band(B, *P["cuivre_bas"], seg, "cuivre")
    C.revolve(B, P["support"], 12, "fer", cap_top=True)
    m = P["montant"]
    zv0, zv1 = P["verre"][1], P["verre"][2]
    for a in m["angles"]:
        t = math.radians(a)
        cx, cy = m["r"] * math.cos(t), m["r"] * math.sin(t)
        h = m["cote"] / 2
        ux, uy = math.cos(t), math.sin(t)              # carré tourné : un côté face au centre
        vx, vy = -uy, ux
        sq = [(cx + (sx * ux + sy * vx) * h, cy + (sx * uy + sy * vy) * h) for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
        B.prism_z(sq, zv0, zv1, "fer", skip_bottom=True, skip_top=True)
    band(B, *P["cuivre_haut"], seg, "cuivre")
    ch = P["chapeau"]
    C.revolve(B, ch, seg, "fer", cap_top=True)
    # liseré cuivré au bord du chapeau
    band(B, ch[0][0] - 0.004, ch[0][0] + 0.0015, ch[0][1], ch[0][1] + 0.004, seg, "cuivre")
    fl = P["fleuron"]
    C.revolve(B, [(fl["r"], fl["z0"]), (fl["r"], fl["z1"])], 10, "fer")
    rt, z0, z1 = fl["tete"]
    C.revolve(B, [(rt, z0), (rt, z1)], 10, "cuivre", cap_top=True, cap_bottom=True)
    # anneau de portage, dans le plan XZ (face à l'avant -Y)
    ring = [Vector((A["rayon"] * math.cos(2 * math.pi * k / A["segs"]), 0.0,
                    zc + A["rayon"] * math.sin(2 * math.pi * k / A["segs"]))) for k in range(A["segs"])]
    C.tube(B, ring, A["fil"], A["cotes"], "fer", closed=True, up=Vector((0, 1, 0)))
    body = B.finish("SM_LanterneFeu", mats, origin=pivot)

    # ---- verre (cylindre ouvert, double face inutile : on le voit de l'extérieur)
    Bv = C.Builder()
    Bv.set_uv_params("verre", 0.1, 0.1)
    rv = P["verre"][0]
    C.revolve(Bv, [(rv, zv0), (rv, zv1)], seg, "verre", phase=math.pi / seg)
    verre = Bv.finish("SM_LanterneFeu_Verre", mats, origin=pivot)

    # ---- mèche
    Bm = C.Builder()
    mp = [Vector((x, 0.0, z)) for x, z in P["meche"]["pts"]]
    C.tube(Bm, mp, P["meche"]["r"], 4, "meche", up=Vector((0, 1, 0)))
    meche = Bm.finish("SM_LanterneFeu_Meche", mats, origin=tuple(mp[0]))

    # ---- flamme (cœur lumineux vu à travers le verre)
    tip = mp[-1]
    Bf = C.Builder()
    Bf.set_uv_params("flamme", 0.1, 0.1)
    C.revolve(Bf, [(r, tip.z + dz) for r, dz in P["flamme"]], 12, "flamme")
    flamme = Bf.finish("SM_LanterneFeu_Flamme", mats, origin=(0.0, 0.0, tip.z))

    for ob in (verre, meche, flamme):
        F.attach(ob, body)
    fx_f = F.empty("FX_Flamme", (tip.x, 0.0, tip.z + P["fx_flamme_dz"]), body)
    fx_j = F.empty("FX_Jet", (0.0, -rv, P["fx_jet_z"]), body)
    bpy.context.view_layer.update()
    body.location = (0.0, 0.0, 0.0)          # le pivot (centre de l'anneau) à l'origine du fichier
    print("corps %d, verre %d, mèche %d, flamme %d triangles" % (C.tri_count(body), C.tri_count(verre), C.tri_count(meche),
                                                                  C.tri_count(flamme)))
    return [body, verre, meche, flamme, fx_f, fx_j]


if __name__ == "__main__":
    F.run("SM_LanterneFeu", build, PARAMS)
