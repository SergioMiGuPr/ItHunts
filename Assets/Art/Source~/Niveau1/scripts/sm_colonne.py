# -*- coding: utf-8 -*-
"""SM_Colonne (+ SM_Colonne_Anneau) : colonne de 8 m, d'après fiche3d_colonne_v03 (remplace Prop_Colonne).

Base carrée à deux gradins, fût cylindrique en tambours de 0.6 m (joints marqués), anneau mouluré
en bas et en haut, chapiteau de 1 m (corbeille évasée, 8 feuilles en crochet, tailloir carré plat).
L'anneau de fer et sa chaîne sont un objet séparé, sur la face avant (-Y) à 3 m du sol, pivot au
point d'accroche contre le fût. Pivot de la colonne : au sol, au centre.

Texture du fût : une tuile fait le tour (pas de couture) et une assise peinte par tambour, pour que
les joints peints tombent sur les joints modélisés (densité un peu plus serrée que sur les murs).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_colonne.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, importlib
sys.dont_write_bytecode = True          # pas de __pycache__ dans Assets/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("it_common", None)   # recharge toujours la version du dossier courant
import it_common as C
from mathutils import Vector

PARAMS = dict(
    hauteur=8.0,
    gradins=[(1.4, 0.4), (1.2, 0.3)],        # (côté, hauteur) de bas en haut
    fut_r=0.4,
    anneau_h=0.15, anneau_r=0.47,            # anneaux moulurés bas et haut du fût
    tambour=0.6,                             # hauteur visée des tambours
    joint=0.02,
    segments=16,
    chapiteau_h=1.0, tailloir=(1.3, 0.2),
    corbeille=[(0.42, 0.0), (0.44, 0.3), (0.5, 0.58), (0.62, 0.8)],   # (r, z relatif au bas du chapiteau)
    feuilles=8, feuille_l=(0.16, 0.24), feuille_ep=0.05,
    anneau=dict(z=3.0, r=0.1, fil=0.018, oeil_r=0.035, maillons=6, maillon=(0.1, 0.05, 0.013), pas=0.075),
    v_decalage=0.0104,
)


def build(P):
    mats = {"pierre": C.tile_material("pierre"), "fer": C.tile_material("fer")}
    tw, th = 4.0, 3.0
    voff = P["v_decalage"]
    B = C.Builder()
    B.set_uv_params("pierre", tw, th, voff)
    B.set_uv_params("fer", *C.TILES["fer"][1:])
    seg = P["segments"]
    R = P["fut_r"]

    # ---- base à gradins
    z = 0.0
    for side, hh in P["gradins"]:
        s = side / 2
        C.fit_stones(B, B.box((-s, -s, z), (s, s, z + hh), "pierre", skip=("-z",), bevel=0.03))
        z += hh
    z_base = z

    # ---- anneau bas, fût en tambours, anneau haut
    Ra, ah = P["anneau_r"], P["anneau_h"]
    z_chap = P["hauteur"] - P["chapiteau_h"]
    z_f0, z_f1 = z_base + ah, z_chap - ah

    def uv_round(z0, course):
        """u : une tuile par tour ; v : l'assise peinte n° course, étirée sur la hauteur du tambour."""
        def f(k, i, r, zz, z0=z0, course=course, h=None):
            return (k / seg, voff + (course + 0.04 + 0.92 * (zz - z0) / f.h) / 4)
        return f

    f_bas = uv_round(z_base, 0); f_bas.h = ah
    C.revolve(B, [(Ra, z_base), (Ra, z_base + ah * 0.55), (R, z_f0)], seg, "pierre", uv=f_bas)
    n = max(1, round((z_f1 - z_f0) / P["tambour"]))
    hd = (z_f1 - z_f0) / n
    j = P["joint"]
    for i in range(n):
        za, zb = z_f0 + i * hd, z_f0 + (i + 1) * hd
        prof = [(R - j, za), (R, za + j), (R, zb - j), (R - j, zb)]
        f = uv_round(za, (i * 3 + 1) % 4); f.h = hd
        # chaque tambour tourne d'un angle différent : les pierres peintes ne s'alignent pas
        ph = (i * 0.37) % 1.0
        def fu(k, ii, r, zz, f=f, ph=ph):
            u, v = f(k, ii, r, zz)
            return (u + ph, v)
        C.revolve(B, prof, seg, "pierre", uv=fu)
    f_haut = uv_round(z_f1, 2); f_haut.h = ah
    C.revolve(B, [(R, z_f1), (Ra, z_f1 + ah * 0.45), (Ra, z_chap)], seg, "pierre", uv=f_haut)

    # ---- chapiteau : corbeille évasée
    cb = [(r, z_chap + dz) for r, dz in P["corbeille"]]
    z_tail = z_chap + P["corbeille"][-1][1]
    def uv_bell(k, i, r, zz):
        return (k / seg * 0.5, voff + (zz - z_chap) / th)
    C.revolve(B, cb, seg, "pierre", uv=uv_bell)
    # tailloir carré, dessus plat
    ts, tth = P["tailloir"]
    C.fit_stones(B, B.box((-ts / 2, -ts / 2, z_tail), (ts / 2, ts / 2, z_tail + tth), "pierre", bevel=0.02))

    # ---- 8 feuilles en crochet (bandes épaisses qui montent sur la corbeille puis s'enroulent)
    def bell_r(dz):
        pts = P["corbeille"]
        for (r0, z0), (r1, z1) in zip(pts[:-1], pts[1:]):
            if z0 <= dz <= z1:
                return r0 + (r1 - r0) * (dz - z0) / (z1 - z0)
        return pts[-1][0]
    nf = P["feuilles"]
    w0, w1 = P["feuille_l"]
    ep = P["feuille_ep"]
    # chemin (r, dz) le long de la corbeille puis volute sous le tailloir
    # volute : les feuilles des faces restent dans 1.4 m hors tout, celles des angles avancent plus
    def leaf_path(c):
        return [(bell_r(0.05) + 0.01, 0.05), (bell_r(0.3) + 0.01, 0.3), (bell_r(0.55) + 0.015, 0.55),
                (bell_r(0.7) + 0.03 * c, 0.71), (bell_r(0.7) + 0.08 * c, 0.75), (bell_r(0.7) + 0.12 * c, 0.69),
                (bell_r(0.7) + 0.10 * c, 0.62)]
    for k in range(nf):
        a = 2 * math.pi * k / nf
        path_rz = leaf_path(0.8 if k % 2 == 0 else 1.8)
        radial = Vector((math.cos(a), math.sin(a), 0))
        tang = Vector((-math.sin(a), math.cos(a), 0))
        rings, m = [], len(path_rz)
        for i, (r, dz) in enumerate(path_rz):
            c = radial * r + Vector((0, 0, z_chap + dz))
            # direction du chemin (dans le plan radial-vertical) et normale à la feuille
            r_a, z_a = path_rz[max(i - 1, 0)]
            r_b, z_b = path_rz[min(i + 1, m - 1)]
            t = (radial * (r_b - r_a) + Vector((0, 0, z_b - z_a))).normalized()
            nrm = tang.cross(t).normalized()          # vers l'extérieur de la feuille
            w = (w0 + (w1 - w0) * min(1.0, i / 3)) / 2 * (0.6 if i == m - 1 else 1.0)
            e = ep / 2
            rings.append([c - tang * w - nrm * e, c + tang * w - nrm * e, c + tang * w + nrm * e, c - tang * w + nrm * e])
        B.new_shell()
        for i in range(m - 1):
            cen = (sum(rings[i], Vector()) + sum(rings[i + 1], Vector())) / 8
            for q in range(4):
                quad = [rings[i][q], rings[i][(q + 1) % 4], rings[i + 1][(q + 1) % 4], rings[i + 1][q]]
                B.face(quad, "pierre", sum(quad, Vector()) / 4 - cen)
        B.face(rings[0], "pierre", rings[0][0] - rings[1][0])
        B.face(rings[-1], "pierre", rings[-1][0] - rings[-2][0])

    col = B.finish("SM_Colonne", mats)

    # ---- anneau de fer + chaîne (objet séparé), face avant (-Y)
    A = P["anneau"]
    B2 = C.Builder()
    B2.set_uv_params("fer", *C.TILES["fer"][1:])
    y0 = -R                                   # surface du fût
    za = A["z"]
    # plaque d'ancrage
    B2.box((-0.06, y0 - 0.025, za - 0.06), (0.06, y0 + 0.02, za + 0.06), "fer", skip=("+y",))
    # oeil (boucle perpendiculaire au mur)
    yo = y0 - 0.025 - A["oeil_r"]
    eye = [Vector((0, yo + A["oeil_r"] * math.cos(2 * math.pi * k / 8), za + A["oeil_r"] * math.sin(2 * math.pi * k / 8))) for k in range(8)]
    C.tube(B2, eye, 0.012, 4, "fer", closed=True)
    # anneau (parallèle au mur), suspendu à l'oeil
    zc = za - A["oeil_r"] - A["r"] + A["fil"]
    yr = yo - A["oeil_r"]
    ring = [Vector((A["r"] * math.cos(2 * math.pi * k / 12), yr, zc + A["r"] * math.sin(2 * math.pi * k / 12))) for k in range(12)]
    C.tube(B2, ring, A["fil"], 4, "fer", closed=True, up=Vector((0, 1, 0)))
    # chaîne : maillons en alternance (plan XZ / plan YZ)
    L, Wm, fil = A["maillon"]
    ztop = zc - A["r"] + fil
    for i in range(A["maillons"]):
        cz = ztop - L / 2 - i * A["pas"]
        pts = []
        for k in range(8):
            t = 2 * math.pi * k / 8
            dx, dz = (Wm / 2 - fil) * math.cos(t), (L / 2 - fil) * math.sin(t)
            pts.append(Vector((dx, yr, cz + dz)) if i % 2 == 0 else Vector((0, yr + dx, cz + dz)))
        C.tube(B2, pts, fil, 4, "fer", closed=True, up=Vector((0, 1, 0)) if i % 2 == 0 else Vector((1, 0, 0)))
    pivot = (0.0, y0, za)
    ann = B2.finish("SM_Colonne_Anneau", {"fer": mats["fer"]}, origin=pivot)
    print("colonne : %d tris, anneau : %d tris, %d tambours de %.3f m" % (C.tri_count(col), C.tri_count(ann), n, hd))
    return [col, ann]


if __name__ == "__main__":
    C.run_piece("SM_Colonne", build, PARAMS, "SM_Colonne")
    C.render_detail(["SM_Colonne_Anneau"], "SM_Colonne_Anneau")
