# -*- coding: utf-8 -*-
"""SM_GrandePorte : grande porte de sortie (passage 10 × 10 m), d'après fiche3d_grande_porte_v01
(vue de face et vues de côté seulement).

Objets :
- SM_GrandePorte_Cadre : cadre en pierre 13 × 14 × 2 m, arc brisé surbaissé (à quatre centres,
  tangent aux piédroits), claveaux marqués, bandeau intérieur en retrait, écoinçons pleins (bords
  extérieurs droits), base plate. Pivot au sol, au centre.
- SM_GrandePorte_BattantG / _BattantD : 5 m × 0.4 m, rectangulaires sur 10 m puis arrondis selon
  l'intrados. Planches verticales, 3 pentures côté face, traverses et charnières côté dos. Ils sont
  posés contre la face arrière du cadre (y = +1) : ouverts vers l'intérieur (+Y), ils laissent les
  10 m libres (prévoir 0.61 m de dégagement derrière le mur). Pivot sur l'axe des gonds : arête avant extérieure, au sol (x = ∓5, y = +1).
- SM_GrandePorte_Serrure_G / _D : plaque ornée au centre, coupée en deux moitiés (une par battant,
  enfant de son battant, pivot sur la ligne de jonction), matériau à part M_GrandePorte_Serrure
  (pour la lueur rouge ou orange en jeu).
- SM_GrandePorte_Anneau_G / _D : anneaux de tirage, enfants de leur battant.

L'avant (vue de face de la fiche) regarde vers -Y.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_grandeporte.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, importlib
sys.dont_write_bytecode = True          # pas de __pycache__ dans Assets/
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("it_common", None)   # recharge toujours la version du dossier courant
import it_common as C
import numpy as np
from mathutils import Vector, Matrix

PARAMS = dict(
    largeur=13.0, hauteur=14.0, profondeur=2.0,
    passage=(10.0, 10.0),
    arc_r1=0.8, arc_phi=60.0,        # petit arc des naissances (rayon, angle en degrés)
    bandeau=0.3, bandeau_retrait=0.25,   # bandeau intérieur (en retrait sur la face avant)
    claveaux_ep=0.9, claveaux=7,     # par côté
    assises=13,                      # blocs par piédroit
    ecoincon_retrait=0.1,
    chanfrein=0.03,
    battant_ep=0.4, planches=6, planche_retrait=0.02,
    pentures_z=(2.51, 6.56, 9.08),   # mesurées sur la fiche
    penture=dict(d0=0.15, d1=3.55, d2=3.8, d3=4.25, h=0.5, h_croix=1.0, ep=0.05),
    traverse=dict(h=0.6, ep=0.12),
    # noeud_retrait : le noeud est reculé de l'arête des gonds vers l'intérieur du battant ; ouvert à 90°,
    # un point à la distance d de l'arête se retrouve à y = 1 + d : il faut d > noeud_r pour ne pas
    # entrer dans le piédroit
    charniere=dict(l=2.4, h=0.35, ep=0.05, noeud_r=0.11, noeud_h=0.6, noeud_retrait=0.14, depart=0.02),
    rivet=(0.08, 0.05, 0.04),
    serrure=dict(z=4.65, ep=0.06, bossage=0.04),
    anneau=dict(x=2.36, z=5.34, r=0.45, fil=0.06, rosace=0.2),
    # fiche (pixels) : ouverture et repères de la vue de face
    fiche="fiche3d_grande_porte_v01.png",
    fx=(112, 297, 482), fy_sol=592, fy_naissance=265, fy_sommet=117,
    fy_bande_propre=(318, 358),      # bande de planches sans fer (pour le dos)
    texture="T_GrandePorte_Bois_BaseColor.png",
    v_decalage=0.0104,
)


# --------------------------------------------------------------------------- arc à quatre centres
def solve_arch(P):
    s = P["passage"][0] / 2
    T = P["bandeau"] + P["claveaux_ep"]
    rise_max = P["hauteur"] - P["passage"][1]
    phi = math.radians(P["arc_phi"])
    lo, hi = 0.5, rise_max
    for _ in range(80):
        m = (lo + hi) / 2
        try:
            a = C.tudor_arch(s, P["arc_r1"], phi, m)
        except ZeroDivisionError:
            lo = m; continue
        if a[4] <= 0:
            lo = m; continue
        if apex_z(a, T) > rise_max:
            hi = m
        else:
            lo = m
    return C.tudor_arch(s, P["arc_r1"], phi, (lo + hi) / 2)


def apex_z(a, off):
    C1, r1, phi, C2, r2 = a
    R = r2 + off
    return C2.y + math.sqrt(R * R - C2.x * C2.x)


def theta_end(a, off):
    C1, r1, phi, C2, r2 = a
    return math.acos(-C2.x / (r2 + off))


def arch_pt(a, th, off, z0):
    """Point (x, z) de l'arc droit à l'angle th, décalé de off vers l'extérieur ; naissance en z0."""
    C1, r1, phi, C2, r2 = a
    c, R = (C1, r1 + off) if th <= phi else (C2, r2 + off)
    return (c.x + R * math.cos(th), z0 + c.y + R * math.sin(th))


def arch_curve(a, off, z0, t0=0.0, t1=None, n=12):
    t1 = theta_end(a, off) if t1 is None else t1
    # échantillonne en gardant le point de tangence phi
    phi = a[2]
    ts = [t0 + (t1 - t0) * k / n for k in range(n + 1)]
    if t0 < phi < t1:
        ts = sorted(set([round(t, 9) for t in ts] + [phi]))
    return [arch_pt(a, t, off, z0) for t in ts]


# --------------------------------------------------------------------------- texture du bois
def make_atlas(P):
    px = C.load_pixels(os.path.join(C.FICHES_DIR, P["fiche"])).copy()
    # efface la serrure peinte (à droite du milieu sur la fiche) : mêmes planches, prises plus bas
    # (entre la penture basse et le bas de la porte), étirées en hauteur dans le sens des planches
    patch = C.crop_resize(px, 300, 530, 366, 568, 66, 92)
    px[390:482, 300:366] = patch
    xg, xm, xd = P["fx"]
    y0, y1 = P["fy_sommet"], P["fy_sol"]
    atlas = np.zeros((1024, 1024, 4), dtype=np.float32)
    atlas[:, 0:512] = C.crop_resize(px, xg, y0, xm, y1, 512, 1024)
    atlas[:, 512:1024] = C.crop_resize(px, xm, y0, xd, y1, 512, 1024)
    path = os.path.join(C.TEX_DIR, P["texture"])
    C.save_pixels(atlas, path)
    return path


def build(P):
    tex = make_atlas(P)
    mats = {"pierre": C.tile_material("pierre"), "fer": C.tile_material("fer"),
            "bois": C.make_material("M_GrandePorte_Bois", tex),
            "serrure": C.make_material("M_GrandePorte_Serrure", os.path.join(C.TEX_DIR, C.TILES["fer"][0]))}
    W2, H, D2 = P["largeur"] / 2, P["hauteur"], P["profondeur"] / 2
    s, zb = P["passage"][0] / 2, P["passage"][1]
    a = solve_arch(P)
    band, cl = P["bandeau"], P["claveaux_ep"]
    T = band + cl
    ch = P["chanfrein"]
    n_as = P["assises"]
    th_course = zb / n_as * 4                     # hauteur de tuile qui cale une assise peinte par bloc

    # =========================================================== cadre
    B = C.Builder()
    B.set_uv_params("pierre", 4.0, th_course, P["v_decalage"])
    yb_front = -D2 + P["bandeau_retrait"]
    # piédroits : bandeau intérieur + blocs
    for sx in (-1, 1):
        xa, xb = sorted((sx * s, sx * (s + band)))
        B.box((xa, yb_front, 0), (xb, D2, zb), "pierre", skip=("-z",), bevel=ch)
        for i in range(n_as):
            z0, z1 = zb * i / n_as, zb * (i + 1) / n_as
            xa, xb = sorted((sx * (s + band), sx * W2))
            C.fit_stones(B, B.box((xa, -D2, z0), (xb, D2, z1), "pierre", bevel=ch))
    # bandeau de l'arc (en retrait), d'un seul tenant par côté
    for sx in (-1, 1):
        inner = arch_curve(a, 0.0, zb, n=16)
        outer = arch_curve(a, band, zb, n=16)
        poly = [(sx * x, z) for x, z in inner] + [(sx * x, z) for x, z in outer[::-1]]
        B.prism_y(poly, yb_front, D2, "pierre", bevel=ch)
    # claveaux : joints suivant les normales des arcs, le dernier coupé à x = 0
    nc = P["claveaux"]
    om = band + cl / 2
    # répartition à longueur d'arc égale (au milieu du rouleau)
    fine = [theta_end(a, om) * k / 400 for k in range(401)]
    lens = [0.0]
    for t0, t1 in zip(fine[:-1], fine[1:]):
        p0, p1 = arch_pt(a, t0, om, 0), arch_pt(a, t1, om, 0)
        lens.append(lens[-1] + math.dist(p0, p1))
    joints = [0.0]
    for j in range(1, nc):
        target = lens[-1] * j / nc
        k = next(i for i, L in enumerate(lens) if L >= target)
        joints.append(fine[k])
    tw, thh = 4.0, th_course
    vb = P["v_decalage"] + 0.25 * 0.1

    def arc_uv(sx):
        def f(pts):
            out = []
            for x_, y_, z_ in pts:
                # u ~ longueur le long de l'arc (angle × rayon moyen), v ~ distance à l'intrados
                X, Z = sx * x_, z_ - zb
                best = None
                for off in (band, band + cl):
                    pass
                C1, r1, phi, C2, r2 = a
                c = C1 if math.atan2(Z - C1.y, X - C1.x) <= phi else C2
                th = math.atan2(Z - c.y, X - c.x)
                rho = math.hypot(X - c.x, Z - c.y)
                base_r = r1 if c is C1 else r2
                L = th * (base_r + om) if c is C1 else (phi * (r1 + om) + (th - phi) * (r2 + om))
                out.append((sx * L / tw, vb + (rho - base_r - band) / thh * 0.9))
            return out
        return f

    for sx in (-1, 1):
        for j in range(nc):
            t0 = joints[j]
            last = j == nc - 1
            ti1 = theta_end(a, band) if last else joints[j + 1]
            to1 = theta_end(a, T) if last else joints[j + 1]
            inner = arch_curve(a, band, zb, t0, ti1, n=3)
            outer = arch_curve(a, T, zb, t0, to1, n=3)
            poly = [(sx * x, z) for x, z in inner] + [(sx * x, z) for x, z in outer[::-1]]
            B.prism_y(poly, -D2, D2, "pierre", bevel=ch, uv_front=arc_uv(sx), uv_back=arc_uv(sx))
    # écoinçons (remplissent le rectangle jusqu'à 14 m), en léger retrait
    ye = -D2 + P["ecoincon_retrait"]
    for sx in (-1, 1):
        ext = arch_curve(a, T, zb, n=16)
        poly = [(sx * (s + T), zb), (sx * W2, zb), (sx * W2, H), (0.0, H)] + [(sx * x, z) for x, z in ext[::-1][1:-1]]
        B.prism_y(poly, ye, D2, "pierre")
    cadre = B.finish("SM_GrandePorte_Cadre", mats)

    # =========================================================== battants
    xg, xm, xd = P["fx"]
    fy0, fyn, fys = P["fy_sol"], P["fy_naissance"], P["fy_sommet"]
    z_ap = zb + apex_z(a, 0.0)
    yf, ep = D2, P["battant_ep"]

    def fiche_y(z):
        if z <= zb:
            return fy0 - (z / zb) * (fy0 - fyn)
        return fyn - (z - zb) / (z_ap - zb) * (fyn - fys)

    def uv_face(side):
        """Projection de la vue de face de la fiche (battant G : moitié gauche de l'atlas)."""
        def f(pts):
            out = []
            for x_, y_, z_ in pts:
                if side < 0:
                    fx = xg + (x_ + s) / s * (xm - xg); u = (fx - xg) / (xm - xg) * 0.5
                else:
                    fx = xm + x_ / s * (xd - xm); u = 0.5 + (fx - xm) / (xd - xm) * 0.5
                v = 1 - (fiche_y(z_) - fys) / (fy0 - fys)
                out.append((u, v))
            return out
        return f

    def uv_dos(side, dz=0.0):
        """Dos : mêmes colonnes, mais une bande de planches sans fer étirée sur la hauteur."""
        b0, b1 = P["fy_bande_propre"]
        def f(pts):
            out = []
            for (u, _), (x_, y_, z_) in zip(uv_face(side)(pts), pts):
                fy = b1 - ((z_ + dz) % z_ap) / z_ap * (b1 - b0)
                out.append((u, 1 - (fy - fys) / (fy0 - fys)))
            return out
        return f

    def top(x):
        return zb + C.tudor_top(a, x, 0.0)

    battants, anneaux = [], []
    for side, nom in ((-1, "SM_GrandePorte_BattantG"), (1, "SM_GrandePorte_BattantD")):
        Bb = C.Builder()
        Bb.set_uv_params("fer", *C.TILES["fer"][1:])
        npl = P["planches"]
        for j in range(npl):
            xa = side * s * (1 - j / npl) if side < 0 else s * j / npl
            xb_ = side * s * (1 - (j + 1) / npl) if side < 0 else s * (j + 1) / npl
            xa, xb_ = sorted((xa, xb_))
            tops = [(xb_ + (xa - xb_) * k / 3) for k in range(4)]      # de xb vers xa
            poly = [(xa, 0.0), (xb_, 0.0), (xb_, zb)] + [(x_, top(x_)) for x_ in tops] + [(xa, zb)]
            r = P["planche_retrait"] if j % 2 else 0.0
            Bb.prism_y(poly, yf + r, yf + ep - r, "bois", skip_front=False,
                       uv_front=uv_face(side), uv_back=uv_dos(side), uv_side=uv_face(side))
        # pentures (face avant)
        Pp = P["penture"]
        for zp in P["pentures_z"]:
            d0, d1, d2, d3, hh, hc = Pp["d0"], Pp["d1"], Pp["d2"], Pp["d3"], Pp["h"] / 2, Pp["h_croix"] / 2
            loc = [(d0, -hh), (d1, -hh), (d1, -hc), (d2, -hc), (d2, -hh * 0.8), (d3, 0), (d2, hh * 0.8),
                   (d2, hc), (d1, hc), (d1, hh), (d0, hh)]
            poly = [(side * (s - d), zp + hv) for d, hv in loc]
            Bb.prism_y(poly, yf - Pp["ep"], yf, "fer", skip_back=True)
            for k in range(5):
                d = d0 + 0.35 + (d1 - d0 - 0.5) * k / 4
                rivet(Bb, side * (s - d), zp, yf - Pp["ep"], -1, P["rivet"])
        # dos : traverses + charnières + noeuds de gonds
        Tt, Cc = P["traverse"], P["charniere"]
        yd = yf + ep
        for zp in P["pentures_z"]:
            xa, xb_ = sorted((side * (s - 0.05), side * 0.1))
            Bb.prism_y([(xa, zp - Tt["h"] / 2), (xb_, zp - Tt["h"] / 2), (xb_, zp + Tt["h"] / 2), (xa, zp + Tt["h"] / 2)],
                       yd, yd + Tt["ep"], "bois", skip_front=True,
                       uv_back=uv_dos(side, 1.3), uv_side=uv_dos(side, 1.3))
            y2 = yd + Tt["ep"]
            xa, xb_ = sorted((side * s, side * (s - Cc["l"])))
            tip = side * (s - Cc["l"] - 0.3)
            x0c = side * (s - Cc["depart"])
            poly = [(x0c, zp - Cc["h"] / 2), (side * (s - Cc["l"]), zp - Cc["h"] / 2), (tip, zp),
                    (side * (s - Cc["l"]), zp + Cc["h"] / 2), (x0c, zp + Cc["h"] / 2)]
            Bb.prism_y(poly, y2, y2 + Cc["ep"], "fer", skip_front=True)
            for k in range(3):
                rivet(Bb, side * (s - 0.5 - 0.7 * k), zp, y2 + Cc["ep"], 1, P["rivet"])
            # noeud de gond (cylindre vertical près de l'arête extérieure, côté dos)
            r = Cc["noeud_r"]
            hexa = [(side * (s - Cc["noeud_retrait"]) + r * math.cos(math.pi / 3 * k), yd + 0.06 + r * math.sin(math.pi / 3 * k)) for k in range(6)]
            Bb.prism_z(hexa, zp - Cc["noeud_h"] / 2, zp + Cc["noeud_h"] / 2, "fer", skip_bottom=False)
        pivot = (side * s, yf, 0.0)
        ob = Bb.finish(nom, {"bois": mats["bois"], "fer": mats["fer"]}, origin=pivot)
        battants.append(ob)

        # anneau de tirage (enfant du battant)
        A = P["anneau"]
        Ba = C.Builder()
        Ba.set_uv_params("fer", *C.TILES["fer"][1:])
        ax, az = side * A["x"], A["z"]
        ro = A["rosace"]
        octo = [(ax + ro * math.cos(math.pi / 4 * k + math.pi / 8), az + ro * math.sin(math.pi / 4 * k + math.pi / 8)) for k in range(8)]
        Ba.prism_y(octo, yf - 0.05, yf, "fer", skip_back=True)
        # oeil
        eye = [Vector((ax, yf - 0.05 - 0.06 + 0.06 * math.cos(2 * math.pi * k / 8), az - 0.1 + 0.06 * math.sin(2 * math.pi * k / 8))) for k in range(8)]
        C.tube(Ba, eye, 0.025, 4, "fer", closed=True)
        rr, fil = A["r"], A["fil"]
        yc = yf - 0.05 - 0.12
        zc = az - 0.1 - 0.06 - rr + fil
        ring = [Vector((ax + rr * math.cos(2 * math.pi * k / 16), yc, zc + rr * math.sin(2 * math.pi * k / 16))) for k in range(16)]
        C.tube(Ba, ring, fil, 6, "fer", closed=True, up=Vector((0, 1, 0)))
        an = Ba.finish("SM_GrandePorte_Anneau_" + ("G" if side < 0 else "D"), {"fer": mats["fer"]}, origin=(ax, yf, az))
        anneaux.append((an, ob))

    # =========================================================== serrure (deux moitiés, une par battant)
    Sx = P["serrure"]
    zs = Sx["z"]
    # moitié gauche de la plaque ornée (x <= 0) ; la droite est son symétrique
    plate_l = [(0.0, -0.85), (0.0, 0.85), (-0.45, 0.85), (-0.45, 0.65), (-0.65, 0.55), (-0.65, 0.35), (-0.45, 0.3),
               (-0.45, -0.3), (-0.65, -0.35), (-0.65, -0.55), (-0.45, -0.65), (-0.45, -0.85)]
    # moitié gauche du bossage : contour extérieur + bord gauche du trou (fente puis cercle)
    rc, zc_h = 0.07, 0.12
    circ = [(rc * math.cos(math.radians(d)), zc_h + rc * math.sin(math.radians(d))) for d in (240, 210, 180, 150, 120, 90)]
    boss_l = [(0.0, -0.45), (0.0, -0.2), (-0.06, -0.2)] + circ + [(0.0, 0.45), (-0.3, 0.45), (-0.3, -0.45)]
    y_pl = yf - Sx["ep"]
    yb2 = y_pl - Sx["bossage"]
    serrures = []
    for sxx, suf, bat in ((1, "G", battants[0]), (-1, "D", battants[1])):
        Bs = C.Builder()
        Bs.set_uv_params("serrure", *C.TILES["fer"][1:])
        Bs.prism_y([(sxx * x, zs + z) for x, z in plate_l], y_pl, yf, "serrure", skip_back=True)
        Bs.prism_y([(sxx * x, zs + z) for x, z in boss_l], yb2, y_pl, "serrure", skip_back=True)
        half = Bs.finish("SM_GrandePorte_Serrure_" + suf, {"serrure": mats["serrure"]}, origin=(0.0, yf, zs))
        serrures.append((half, bat))

    # parenté des anneaux et des moitiés de serrure (position monde conservée)
    # (parent sans matrice inverse : position locale = position monde - pivot du battant, ce que
    # l'export FBX transmet tel quel)
    for an, bat in anneaux + serrures:
        loc = an.location - bat.location
        an.parent = bat
        an.matrix_parent_inverse = Matrix.Identity(4)
        an.location = loc
    bpy_update()
    objs = [cadre] + battants + [an for an, _ in anneaux] + [h for h, _ in serrures]
    tot = sum(C.tri_count(o) for o in objs)
    print("arc : intrados sommet z=%.3f, extrados sommet z=%.3f ; triangles : %s ; total %d" % (
        z_ap, zb + apex_z(a, T), {o.name: C.tri_count(o) for o in objs}, tot))
    return objs


def rivet(B, x, z, y0, side, dims):
    """Petit rivet tronconique sur une face de normale (0, side, 0) située en y0."""
    rb, rt, rh = dims
    B.new_shell()
    base = [(x - rb / 2, z - rb / 2), (x + rb / 2, z - rb / 2), (x + rb / 2, z + rb / 2), (x - rb / 2, z + rb / 2)]
    topq = [(x - rt / 2, z - rt / 2), (x + rt / 2, z - rt / 2), (x + rt / 2, z + rt / 2), (x - rt / 2, z + rt / 2)]
    y1 = y0 + side * rh
    B.face([(px, y1, pz) for px, pz in topq], "fer", (0, side, 0))
    for k in range(4):
        (ax, az), (bx, bz) = base[k], base[(k + 1) % 4]
        (cx_, cz), (dx, dz) = topq[(k + 1) % 4], topq[k]
        n = Vector(((ax + bx) / 2 - x, 0, (az + bz) / 2 - z)).normalized() + Vector((0, side * 0.5, 0))
        B.face([(ax, y0, az), (bx, y0, bz), (cx_, y1, cz), (dx, y1, dz)], "fer", n)


def verifier_ouverture(angles=(0, 90)):
    """Contrôle d'interpénétration avec le cadre, porte fermée puis ouverte (vers l'arrière, +Y) :
    compte les sommets des battants (et de leurs enfants) à l'intérieur des piédroits et de l'arc.
    Fait aussi un rendu de dos porte ouverte (SM_GrandePorte_ouverte_dos.png)."""
    import bpy
    from mathutils.bvhtree import BVHTree
    sc = bpy.context.window.scene
    cadre = sc.objects["SM_GrandePorte_Cadre"]
    dg = bpy.context.evaluated_depsgraph_get()
    tree = BVHTree.FromObject(cadre, dg)
    G, D = sc.objects["SM_GrandePorte_BattantG"], sc.objects["SM_GrandePorte_BattantD"]
    for ang in angles:
        G.rotation_euler.z, D.rotation_euler.z = math.radians(ang), math.radians(-ang)
        bpy.context.view_layer.update()
        dg = bpy.context.evaluated_depsgraph_get()
        inside = 0
        for bat in (G, D):
            for ob in [bat] + list(bat.children):
                mw = ob.matrix_world
                for v in ob.data.vertices:
                    p = mw @ v.co
                    loc, nrm, idx, dist = tree.find_nearest(p)
                    # sommet dans la pierre : du côté intérieur de la face la plus proche, à plus de 1 mm
                    if loc is not None and (p - loc).dot(nrm) < -0.001:
                        inside += 1
        print("battants à %d° : %d sommet(s) dans le cadre" % (ang, inside))
        if ang:
            objs = [o for o in sc.objects if o.type == "MESH"]
            C.render_views(sc, objs, "SM_GrandePorte_ouverte", ("dos", "34dos"))
    G.rotation_euler.z = D.rotation_euler.z = 0.0
    bpy.context.view_layer.update()


def bpy_update():
    import bpy
    bpy.context.view_layer.update()


if __name__ == "__main__":
    C.run_piece("SM_GrandePorte", build, PARAMS, "SM_GrandePorte", views=("face", "cote", "34", "dos"))
    verifier_ouverture()
    C.render_detail(["SM_GrandePorte_Serrure_G", "SM_GrandePorte_Serrure_D", "SM_GrandePorte_Anneau_G",
                     "SM_GrandePorte_Anneau_D"], "SM_GrandePorte_Ferrures")
