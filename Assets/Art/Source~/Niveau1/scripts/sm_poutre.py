# -*- coding: utf-8 -*-
"""SM_Poutre (+ SM_Poutre_Crochet) : poutre de bois équarri cerclée de fer, d'après fiche3d_poutre_v01.

La poutre s'étend sur X (6 m), sa face avant (la vue longue de la fiche) regarde vers -Y.
Pivot : au centre du dessous du bois. Le crochet est un objet séparé, pivot en haut (point
d'accroche), placé sous la première ferrure ; à dupliquer sous les autres en jeu (positions X dans
FERRURES_X).

Texture du bois : la vue longue de la fiche est projetée sur les quatre grandes faces, la vue de
bout sur les extrémités (atlas T_Poutre_Bois_BaseColor.png, généré par ce script).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_poutre.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, importlib
sys.dont_write_bytecode = True          # pas de __pycache__ dans Assets/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("it_common", None)   # recharge toujours la version du dossier courant
import it_common as C
import numpy as np
from mathutils import Vector

PARAMS = dict(
    longueur=6.0, section=0.5,
    ferrure_largeur=0.15, ferrure_ep=0.02,
    # centres des ferrures, mesurés sur la fiche (poutre de 909 px de long)
    ferrures_x=[-2.548, -0.927, 0.683, 2.558],
    rivets_z=[0.1, 0.25, 0.4], rivet=(0.05, 0.03, 0.02),       # base, sommet, hauteur
    crochet=dict(section=0.03, anneau_r=0.045, tige_bas=-0.33, crochet_r=0.06, segments=7),
    # recadrages dans la fiche (pixels, origine en haut à gauche)
    fiche="fiche3d_poutre_v01.png",
    crop_long=(66, 299, 969, 407),
    crop_bout=(1070, 292, 1175, 408),
    texture="T_Poutre_Bois_BaseColor.png",
)

# régions de l'atlas (u0, v0, u1, v1), avec une petite marge
REG_LONG = (0.0, 0.5, 1.0, 1.0)
REG_BOUT = (0.0, 0.0, 0.25, 0.5)


def make_atlas(P):
    px = C.load_pixels(os.path.join(C.FICHES_DIR, P["fiche"]))
    W, H = 1024, 512
    atlas = np.zeros((H, W, 4), dtype=np.float32)
    x0, y0, x1, y1 = P["crop_long"]
    atlas[0:256, :] = C.crop_resize(px, x0, y0, x1, y1, 1024, 256)            # moitié haute : vue longue
    x0, y0, x1, y1 = P["crop_bout"]
    atlas[256:512, 0:256] = C.crop_resize(px, x0, y0, x1, y1, 256, 256)       # bas gauche : vue de bout
    x0, y0, x1, y1 = P["crop_long"]
    atlas[256:512, 256:1024] = C.crop_resize(px, x0, y0, x1, y1, 768, 256)    # remplissage (évite les fuites)
    path = os.path.join(C.TEX_DIR, P["texture"])
    C.save_pixels(atlas, path)
    return path


def reg_uv(reg, s, t, pad=0.004):
    u0, v0, u1, v1 = reg
    return (u0 + pad + (u1 - u0 - 2 * pad) * s, v0 + pad + (v1 - v0 - 2 * pad) * t)


def build(P):
    tex = make_atlas(P)
    mats = {"bois": C.make_material("M_Poutre_Bois", tex), "fer": C.tile_material("fer")}
    B = C.Builder()
    B.set_uv_params("fer", *C.TILES["fer"][1:])
    L, S = P["longueur"] / 2, P["section"]
    h = S / 2

    # ---- bois : 6 faces, UV par projection de la fiche
    B.new_shell()
    def long_face(pts, n, along_idx):
        uvs = []
        for p in pts:
            s = (p[0] + L) / (2 * L)
            t = (p[along_idx] - (-h if along_idx == 1 else 0)) / S
            uvs.append(reg_uv(REG_LONG, s, t))
        B.face(pts, "bois", n, uvs)
    long_face([(-L, -h, 0), (L, -h, 0), (L, -h, S), (-L, -h, S)], (0, -1, 0), 2)        # avant
    # arrière : vue longue retournée pour que le bois se raccorde aux arêtes
    B.face([(-L, h, 0), (L, h, 0), (L, h, S), (-L, h, S)], "bois", (0, 1, 0),
           [reg_uv(REG_LONG, (p[0] + L) / (2 * L), p[2] / S) for p in [(-L, h, 0), (L, h, 0), (L, h, S), (-L, h, S)]])
    long_face([(-L, -h, S), (L, -h, S), (L, h, S), (-L, h, S)], (0, 0, 1), 1)          # dessus
    long_face([(-L, -h, 0), (L, -h, 0), (L, h, 0), (-L, h, 0)], (0, 0, -1), 1)         # dessous
    for sx in (-1, 1):
        pts = [(sx * L, -h, 0), (sx * L, h, 0), (sx * L, h, S), (sx * L, -h, S)]
        B.face(pts, "bois", (sx, 0, 0), [reg_uv(REG_BOUT, (p[1] + h) / S if sx < 0 else (h - p[1]) / S, p[2] / S) for p in pts])

    # ---- ferrures (anneaux sans faces intérieures) + rivets
    fw, fe = P["ferrure_largeur"], P["ferrure_ep"]
    for xc in P["ferrures_x"]:
        B.new_shell()
        xa, xb = xc - fw / 2, xc + fw / 2
        o = [(-h - fe, -fe), (h + fe, -fe), (h + fe, S + fe), (-h - fe, S + fe)]     # (y, z) extérieur
        i = [(-h, 0), (h, 0), (h, S), (-h, S)]                                       # (y, z) contre le bois
        for k in range(4):
            a, b = o[k], o[(k + 1) % 4]
            mid = ((a[0] + b[0]) / 2, (a[1] + b[1]) / 2 - h)
            B.face([(xa, a[0], a[1]), (xb, a[0], a[1]), (xb, b[0], b[1]), (xa, b[0], b[1])], "fer", (0, mid[0], mid[1]))
            for x, nx in ((xa, -1), (xb, 1)):
                ia, ib = i[k], i[(k + 1) % 4]
                B.face([(x, a[0], a[1]), (x, b[0], b[1]), (x, ib[0], ib[1]), (x, ia[0], ia[1])], "fer", (nx, 0, 0))
        rb, rt, rh = P["rivet"]
        for side in (-1, 1):
            y0 = side * (h + fe)
            for z in P["rivets_z"]:
                B.new_shell()
                base = [(xc - rb / 2, z - rb / 2), (xc + rb / 2, z - rb / 2), (xc + rb / 2, z + rb / 2), (xc - rb / 2, z + rb / 2)]
                top = [(xc - rt / 2, z - rt / 2), (xc + rt / 2, z - rt / 2), (xc + rt / 2, z + rt / 2), (xc - rt / 2, z + rt / 2)]
                y1 = y0 + side * rh
                B.face([(x, y1, zz) for x, zz in top], "fer", (0, side, 0))
                for k in range(4):
                    (ax, az), (bx, bz) = base[k], base[(k + 1) % 4]
                    (cx_, cz), (dx, dz) = top[(k + 1) % 4], top[k]
                    n = Vector(((ax + bx) / 2 - xc, 0, (az + bz) / 2 - z)).normalized() + Vector((0, side * 0.5, 0))
                    B.face([(ax, y0, az), (bx, y0, bz), (cx_, y1, cz), (dx, y1, dz)], "fer", n)
    poutre = B.finish("SM_Poutre", mats)

    # ---- crochet (objet séparé, pivot en haut)
    K = P["crochet"]
    B2 = C.Builder()
    B2.set_uv_params("fer", *C.TILES["fer"][1:])
    w = K["section"]
    ar = K["anneau_r"]
    zc = -(ar + w / 2)
    # anneau d'accroche, dans le plan YZ
    ring = [Vector((0, ar * math.cos(2 * math.pi * k / 8), zc + ar * math.sin(2 * math.pi * k / 8))) for k in range(8)]
    tube_closed(B2, ring, w, Vector((1, 0, 0)))
    # tige puis crochet en J, dans le plan XZ (ouvert vers +X comme sur la fiche)
    path = [Vector((0, 0, zc - ar + w * 0.5)), Vector((0, 0, K["tige_bas"]))]
    r = K["crochet_r"]
    cc = Vector((r, 0, K["tige_bas"]))
    n = K["segments"]
    a0, a1 = math.pi, 2 * math.pi + math.radians(40)
    for j in range(1, n + 1):
        a = a0 + (a1 - a0) * j / n
        path.append(cc + Vector((r * math.cos(a), 0, r * math.sin(a))))
    tube_open(B2, path, w, Vector((0, 1, 0)))
    crochet = B2.finish("SM_Poutre_Crochet", {"fer": mats["fer"]})
    crochet.location = (P["ferrures_x"][0], 0, -P["ferrure_ep"])
    print("bois + fer :", C.tri_count(poutre), "tris ; crochet :", C.tri_count(crochet), "tris")
    return [poutre, crochet]


def _section(p, t, b, w):
    n = t.cross(b).normalized()
    b = b.normalized()
    return [p + (b * sb + n * sn) * (w / 2) for sb, sn in ((-1, -1), (1, -1), (1, 1), (-1, 1))]


def tube_open(B, path, w, binormal):
    B.new_shell()
    rings = []
    for i, p in enumerate(path):
        t = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
        rings.append(_section(p, t, binormal, w))
    for i in range(len(rings) - 1):
        c = (path[i] + path[i + 1]) / 2
        for k in range(4):
            q = [rings[i][k], rings[i][(k + 1) % 4], rings[i + 1][(k + 1) % 4], rings[i + 1][k]]
            B.face(q, "fer", sum(q, Vector()) / 4 - c)
    B.face(rings[0], "fer", path[0] - path[1])
    B.face(rings[-1], "fer", path[-1] - path[-2])


def tube_closed(B, loop, w, binormal):
    B.new_shell()
    m = len(loop)
    rings = []
    for i in range(m):
        t = (loop[(i + 1) % m] - loop[i - 1]).normalized()
        rings.append(_section(loop[i], t, binormal, w))
    for i in range(m):
        j = (i + 1) % m
        c = (loop[i] + loop[j]) / 2
        for k in range(4):
            q = [rings[i][k], rings[i][(k + 1) % 4], rings[j][(k + 1) % 4], rings[j][k]]
            B.face(q, "fer", sum(q, Vector()) / 4 - c)


if __name__ == "__main__":
    C.run_piece("SM_Poutre", build, PARAMS, "SM_Poutre")
    C.render_detail(["SM_Poutre_Crochet"], "SM_Poutre_Crochet")
