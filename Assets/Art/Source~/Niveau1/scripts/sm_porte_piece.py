# -*- coding: utf-8 -*-
"""SM_Porte_Piece : encadrement de porte de pièce (ouverture vide 3 × 3 m), d'après fiche3d_porte_piece_v01.

Cadre de 4.4 × 5 m × 1 m : piédroits en blocs, linteau en 3 blocs, arc de décharge brisé avec
tympan plein en retrait, 2 gonds en fer sur le piédroit gauche (vu de face). Bords extérieurs droits
(rectangle 4.4 × 5) pour s'insérer dans un mur : les écoinçons au-dessus du linteau sont remplis.
La face avant regarde vers -Y. Pivot : au sol, au centre de l'ouverture.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_porte_piece.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, importlib
sys.dont_write_bytecode = True          # pas de __pycache__ dans Assets/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("it_common", None)   # recharge toujours la version du dossier courant
import it_common as C
from mathutils import Vector

PARAMS = dict(
    largeur=4.4, hauteur=5.0, profondeur=1.0,
    ouverture=(3.0, 3.0),
    assises=4,                 # blocs par piédroit (0.75 m : calés sur les assises de la texture)
    linteau_h=0.7, linteau_blocs=(1.4, 1.6, 1.4),
    arc_demi_portee=2.0,       # extrados, à la naissance (sur le linteau)
    arc_ep=0.45,               # épaisseur des claveaux
    arc_centre_x=-0.5,         # centre de l'arc droit (négatif : arc brisé)
    claveaux=3,                # par côté
    retrait_tympan=0.1, retrait_ecoincon=0.05,
    chanfrein=0.03,
    gonds_z=(0.73, 2.42),      # mesurés sur la fiche
    gond=dict(plaque=(0.05, 0.14, 0.3), pivot_r=0.035, pivot_h=0.22, saillie=0.08, y=-0.3),
    segments=3,                # segments d'arc par claveau
    v_decalage=0.0104,        # cale les joints peints de la texture sur z = 0, 0.75, 1.5...
)


def build(P):
    mats = {"pierre": C.tile_material("pierre"), "fer": C.tile_material("fer")}
    B = C.Builder()
    B.set_uv_params("pierre", 4.0, 3.0, P["v_decalage"])
    B.set_uv_params("fer", *C.TILES["fer"][1:])
    W2, H, D2 = P["largeur"] / 2, P["hauteur"], P["profondeur"] / 2
    ow, oh = P["ouverture"]
    o2 = ow / 2
    ch = P["chanfrein"]

    # ---- piédroits
    n = P["assises"]
    for sx in (-1, 1):
        for i in range(n):
            z0, z1 = oh * i / n, oh * (i + 1) / n
            xa, xb = sorted((sx * o2, sx * W2))
            C.fit_stones(B, B.box((xa, -D2, z0), (xb, D2, z1), "pierre", bevel=ch))
    # ---- linteau (3 blocs)
    z0, z1 = oh, oh + P["linteau_h"]
    x = -W2
    for w in P["linteau_blocs"]:
        C.fit_stones(B, B.box((x, -D2, z0), (x + w, D2, z1), "pierre", bevel=ch))
        x += w

    # ---- arc de décharge brisé (centres sous la naissance)
    zb = z1                                   # naissance = dessus du linteau
    s, h = P["arc_demi_portee"], H - zb
    cx = P["arc_centre_x"]
    b = (s * s - h * h - 2 * s * cx) / (2 * h)
    cz = zb - b
    Re = math.hypot(s - cx, b)
    Ri = Re - P["arc_ep"]

    def ang_base(rho):                        # angle où le cercle de rayon rho coupe z = zb
        return math.asin(b / rho)

    def ang_apex(rho):                        # angle où il coupe x = 0
        return math.acos(-cx / rho)

    def pt(rho, a, sx=1):
        return (sx * (cx + rho * math.cos(a)), cz + rho * math.sin(a))

    tw, th = 4.0, 3.0
    vb = P["v_decalage"] + 0.25 * 0.15        # le rouleau tient dans une assise peinte

    def arc_uv(sx):
        """UV des faces avant/arrière des claveaux : u le long de l'arc, v en travers."""
        def f(pts):
            out = []
            for x_, y_, z_ in pts:
                dx, dz = sx * x_ - cx, z_ - cz
                rho, a = math.hypot(dx, dz), math.atan2(dz, dx)
                out.append((sx * a * (Re + Ri) / 2 / tw, vb + (rho - Ri) / th))
            return out
        return f

    nc, seg = P["claveaux"], P["segments"]
    ae0, aeA = ang_base(Re), ang_apex(Re)
    joints = [ae0 + (aeA - ae0) * j / nc for j in range(nc + 1)]
    for sx in (1, -1):
        for j in range(nc):
            a_i0 = ang_base(Ri) if j == 0 else joints[j]
            a_e0 = ae0 if j == 0 else joints[j]
            a_i1 = ang_apex(Ri) if j == nc - 1 else joints[j + 1]
            a_e1 = aeA if j == nc - 1 else joints[j + 1]
            intr = [pt(Ri, a_i0 + (a_i1 - a_i0) * k / seg, sx) for k in range(seg + 1)]
            extr = [pt(Re, a_e0 + (a_e1 - a_e0) * k / seg, sx) for k in range(seg + 1)]
            poly = intr + extr[::-1]
            B.prism_y(poly, -D2, D2, "pierre", bevel=ch, uv_front=arc_uv(sx), uv_back=arc_uv(sx))

    # ---- tympan plein, en retrait
    yt = D2 - P["retrait_tympan"]
    m = 8
    ai0, aiA = ang_base(Ri), ang_apex(Ri)
    right = [pt(Ri, ai0 + (aiA - ai0) * k / m) for k in range(m + 1)]
    tymp = right + [(-x_, z_) for x_, z_ in right[::-1][1:]]
    B.new_shell()
    B.face([(x_, -yt, z_) for x_, z_ in tymp], "pierre", (0, -1, 0))
    B.face([(x_, yt, z_) for x_, z_ in tymp], "pierre", (0, 1, 0))

    # ---- écoinçons (remplissent le rectangle au-dessus du linteau)
    ye = D2 - P["retrait_ecoincon"]
    extr = [pt(Re, ae0 + (aeA - ae0) * k / m) for k in range(m + 1)]   # de la naissance au sommet
    for sx in (1, -1):
        poly = [(sx * W2, zb), (sx * W2, H), (0.0, H)] + [(sx * x_, z_) for x_, z_ in extr[::-1][1:]]
        B.prism_y(poly, -ye, ye, "pierre")

    # ---- gonds (piédroit gauche vu de face, face intérieure x = -o2)
    G = P["gond"]
    pe, pl, ph = G["plaque"]
    for zg in P["gonds_z"]:
        B.box((-o2, G["y"] - pl / 2, zg - ph / 2), (-o2 + pe, G["y"] + pl / 2, zg + ph / 2), "fer", skip=("-x",))
        # pivot vertical (hexagonal) en saillie dans l'ouverture
        r, xc = G["pivot_r"], -o2 + G["saillie"]
        hexa = [(xc + r * math.cos(math.pi / 3 * k), G["y"] + r * math.sin(math.pi / 3 * k)) for k in range(6)]
        B.prism_z(hexa, zg - G["pivot_h"] / 2, zg + G["pivot_h"] / 2, "fer", skip_bottom=False)
        # patte qui relie la plaque au pivot
        B.box((-o2 + pe, G["y"] - 0.025, zg - 0.04), (xc, G["y"] + 0.025, zg + 0.04), "fer", skip=("-x", "+x"))

    ob = B.finish("SM_Porte_Piece", mats)
    print("arc de décharge : Re=%.3f Ri=%.3f centre (%.2f, %.3f) ; intrados sommet z=%.3f" % (
        Re, Ri, cx, cz, cz + math.sqrt(Ri * Ri - cx * cx)))
    return [ob]


if __name__ == "__main__":
    C.run_piece("SM_Porte_Piece", build, PARAMS, "SM_Porte_Piece")
