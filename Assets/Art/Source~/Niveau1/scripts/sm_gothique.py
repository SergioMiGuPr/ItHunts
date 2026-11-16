# -*- coding: utf-8 -*-
"""Kit gothique du couloir de 14 m (d'après reference_couloir_gothique, plan 04 de la cinématique ; retours du
2026-11-16) : voûte en ogive en tronçons, arcs-doubleaux, colonnes engagées, murs de travée à arcade ou à niche.

Couloir : 14 m entre les murs, travées de 8 m. Les murs montent à la naissance de la voûte (8 m) ; la voûte en arc brisé
(centres sur la ligne de naissance) a son intrados à 15.6 m à la clé ; les arcs-doubleaux (0.6 m sous la voûte) laissent
**15.0 m libres** au centre (0.48 m d'arc + 0.12 m de boudin) (le traqueur passe). Toutes les pièces : pivot au sol ; UV en coordonnées monde (matériaux
*_Monde du shader toon : M_Pierre_Monde, M_Voute_Briques_Monde) ; UV de repli fournies (projection monde du Builder).

Repère Blender : x en travers du couloir (pièces de voûte) ou le long du mur (murs, colonne), y le long du couloir
(voûte) ou vers le mur (murs, colonne : face côté couloir en y = 0, vers -y), z en haut.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Niveau1/scripts/sm_gothique.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("it_common", None)
import it_common as C
from mathutils import Vector

C.RENDER_FMT = "JPEG"

PARAMS = dict(
    demi_portee=7.0,        # m, du milieu du couloir à la face du mur
    naissance=8.0,          # m, hauteur des murs (naissance de la voûte, dessus des chapiteaux)
    cle=15.6,               # m, intrados de la voûte à la clé
    voute_ep=0.6,           # m, épaisseur de la voûte
    doubleau=(0.8, 0.48),   # m, largeur (le long du couloir) et hauteur sous la voûte de l'arc-doubleau
    doubleau_filet=(0.4, 0.12),   # m, boudin sous l'arc : largeur, saillie
    pas_arc=18,             # segments par demi-arc
    travee=8.0,             # m, longueur d'une travée (murs, voûte)
    mur_ep=1.2,             # m, épaisseur des murs de travée
    arcade=(2.2, 4.0, 7.0), # demi-largeur, naissance, clé du passage latéral
    niche=(1.2, 0.9, 3.6, 5.6, 0.6),   # demi-largeur, appui, naissance, clé, profondeur de la niche
    archivolte=(0.45, 0.15),  # largeur, saillie du cordon autour des arcs des murs
    colonne=dict(r=0.38, pilastre=(0.55, 0.12), socle=(0.6, 0.6, 0.35), base=(0.47, 0.15),
                 chapiteau=(0.55, 0.45), tailloir=(0.72, 0.78, 0.35)),
)


def arch(half, z0, apex, n, off=0.0):
    """Points (x, z) d'un arc brisé, de la naissance droite à la naissance gauche en passant par la clé ; centres sur la
    ligne de naissance ; off : décalage du rayon (> 0 : vers l'extérieur, extrados)."""
    a, R = C.pointed_arch_centers(half, apex - z0)
    cx = -a
    Rr = R + off
    tk = math.acos(min(1.0, a / Rr))                 # angle de la clé (x = 0)
    right = [(cx + Rr * math.cos(tk * i / n), z0 + Rr * math.sin(tk * i / n)) for i in range(n + 1)]
    left = [(-x, z) for x, z in reversed(right[:-1])]
    return right + left, (cx, z0, R)


def crescent(B, inner, outer, y0, y1, m_in, m_out, m_cap, ctr):
    """Bande d'arc extrudée le long de y : intrados (inner) et extrados (outer), mêmes nombres de points ; faces
    orientées par rapport au centre ctr (x, z) de l'arc."""
    B.new_shell()
    def V(p, y):
        return (p[0], y, p[1])
    n = len(inner)
    for i in range(n - 1):
        a, b = inner[i], inner[i + 1]
        mid = Vector(((a[0] + b[0]) / 2, 0, (a[1] + b[1]) / 2))
        rad = (mid - Vector((-ctr[0] if mid.x < 0 else ctr[0], 0, ctr[1]))).normalized()
        B.face([V(a, y0), V(b, y0), V(b, y1), V(a, y1)], m_in, -rad)
        a2, b2 = outer[i], outer[i + 1]
        B.face([V(a2, y0), V(b2, y0), V(b2, y1), V(a2, y1)], m_out, rad)
        for y, s in ((y0, -1), (y1, 1)):
            B.face([V(a, y), V(b, y), V(b2, y), V(a2, y)], m_cap, (0, s, 0))
    for k in (0, n - 1):                              # appuis (naissances)
        B.face([V(inner[k], y0), V(outer[k], y0), V(outer[k], y1), V(inner[k], y1)], m_cap, (0, 0, -1))


def vault(P, length):
    B = C.Builder()
    for k in ("pierre", "voute"):
        B.set_uv_params(k, *C.TILES[k][1:])
    n = P["pas_arc"]
    s, z0, apex, ep = P["demi_portee"], P["naissance"], P["cle"], P["voute_ep"]
    inner, g = arch(s, z0, apex, n)
    outer, _ = arch(s, z0, apex, n, off=ep)
    crescent(B, inner, outer, -length / 2, length / 2, "voute", "pierre", "pierre", (g[0], g[1]))
    return B


def rib(P):
    B = C.Builder()
    B.set_uv_params("pierre", *C.TILES["pierre"][1:])
    n = P["pas_arc"]
    s, z0, apex = P["demi_portee"], P["naissance"], P["cle"]
    w, h = P["doubleau"]
    fw, fh = P["doubleau_filet"]
    a, R = C.pointed_arch_centers(s, apex - z0)
    # l'arc suit l'intrados de la voûte (rayon R) ; corps de h, puis un boudin plus étroit de fh dessous
    body_out, g = arch(s, z0, apex, n)
    body_in, _ = arch(s, z0, apex, n, off=-h)
    fil_in, _ = arch(s, z0, apex, n, off=-h - fh)
    crescent(B, body_in, body_out, -w / 2, w / 2, "pierre", "pierre", "pierre", (g[0], g[1]))
    crescent(B, fil_in, body_in, -fw / 2, fw / 2, "pierre", "pierre", "pierre", (g[0], g[1]))
    return B


def half_cyl(B, r0, r1, z0, z1, m, n=10, y0=0.0):
    """Demi-cylindre (ou tronc de cône) contre le mur (y = y0), saillant vers -y."""
    B.new_shell()
    ring0 = [(r0 * math.cos(math.pi + math.pi * k / n), y0 + r0 * math.sin(math.pi + math.pi * k / n), z0) for k in range(n + 1)]
    ring1 = [(r1 * math.cos(math.pi + math.pi * k / n), y0 + r1 * math.sin(math.pi + math.pi * k / n), z1) for k in range(n + 1)]
    for k in range(n):
        q = [ring0[k], ring0[k + 1], ring1[k + 1], ring1[k]]
        am = math.pi + math.pi * (k + 0.5) / n
        B.face(q, m, (math.cos(am), math.sin(am), 0))
    B.face(ring0, m, (0, 0, -1))
    B.face(ring1, m, (0, 0, 1))


def column(P):
    c = P["colonne"]
    B = C.Builder()
    B.set_uv_params("pierre", *C.TILES["pierre"][1:])
    H = P["naissance"]
    sw, sd, sh = c["socle"]
    pw, pd = c["pilastre"]
    tw, td, th = c["tailloir"]
    cr, ch = c["chapiteau"]
    B.box((-sw, -sd, 0), (sw, 0, sh), "pierre")                               # socle
    half_cyl(B, c["base"][0], c["r"] * 1.06, sh, sh + c["base"][1], "pierre")  # base moulurée
    B.box((-pw, -pd, sh), (pw, 0, H - th), "pierre")                         # pilastre (dosseret)
    half_cyl(B, c["r"], c["r"], sh + c["base"][1], H - th - ch, "pierre", n=12)   # fût
    half_cyl(B, c["r"] * 1.05, cr, H - th - ch, H - th, "pierre", n=12)          # corbeille du chapiteau
    B.box((-tw, -td, H - th), (tw, 0, H), "pierre")                           # tailloir
    return B


def wall_with_arch(P, kind):
    """Mur de travée (longueur travee, hauteur naissance, épaisseur mur_ep) percé d'un passage (arcade) ou creusé d'une
    niche ; face côté couloir en y = 0 (vers -y), le mur s'étend vers +y."""
    B = C.Builder()
    B.set_uv_params("pierre", *C.TILES["pierre"][1:])
    L, H, T = P["travee"] / 2, P["naissance"], P["mur_ep"]
    n = P["pas_arc"]
    if kind == "arcade":
        w, zs, apex = P["arcade"]
        sill, depth = 0.0, T
    else:
        w, sill, zs, apex, depth = P["niche"]
    arc, g = arch(w, zs, apex, n)
    B.new_shell()
    for y, s in ((0.0, -1), (T, 1)):
        if s == 1 and kind != "arcade":                 # dos du mur (niche : plein)
            B.face([(-L, y, 0), (L, y, 0), (L, y, H), (-L, y, H)], "pierre", (0, 1, 0))
            continue
        B.face([(-L, y, 0), (-w, y, 0), (-w, y, H), (-L, y, H)], "pierre", (0, s, 0))     # trumeau gauche
        B.face([(w, y, 0), (L, y, 0), (L, y, H), (w, y, H)], "pierre", (0, s, 0))         # trumeau droit
        if sill > 0:
            B.face([(-w, y, 0), (w, y, 0), (w, y, sill), (-w, y, sill)], "pierre", (0, s, 0))   # allège
        for i in range(len(arc) - 1):                      # au-dessus de l'arc : bandes verticales
            (xa, za), (xb, zb) = arc[i], arc[i + 1]
            B.face([(xa, y, za), (xb, y, zb), (xb, y, H), (xa, y, H)], "pierre", (0, s, 0))
    # tableaux (côtés du passage ou de la niche) et intrados
    for (xa, za), (xb, zb) in [((w, sill), (w, zs)), ((-w, zs), (-w, sill))]:
        nx = -1 if xa > 0 else 1
        B.face([(xa, 0, za), (xb, 0, zb), (xb, depth, zb), (xa, depth, za)], "pierre", (nx, 0, 0))
    for i in range(len(arc) - 1):
        (xa, za), (xb, zb) = arc[i], arc[i + 1]
        mx, mz = (xa + xb) / 2, (za + zb) / 2
        cxs = g[0] if mx > 0 else -g[0]
        B.face([(xa, 0, za), (xb, 0, zb), (xb, depth, zb), (xa, depth, za)], "pierre", (cxs - mx, 0, g[1] - mz))
    if sill > 0:                                           # appui de la niche
        B.face([(-w, 0, sill), (w, 0, sill), (w, depth, sill), (-w, depth, sill)], "pierre", (0, 0, 1))
    if kind != "arcade":                                   # fond de la niche
        pts = [(w, depth, sill)] + [(x, depth, z) for x, z in arc] + [(-w, depth, sill)]
        B.face(pts, "pierre", (0, -1, 0))
    # dessus, bouts, dessous
    B.face([(-L, 0, H), (L, 0, H), (L, T, H), (-L, T, H)], "pierre", (0, 0, 1))
    for x, s in ((-L, -1), (L, 1)):
        B.face([(x, 0, 0), (x, T, 0), (x, T, H), (x, 0, H)], "pierre", (s, 0, 0))
    # cordon (archivolte) autour de l'arc, côté couloir, avec impostes
    bw, bp = P["archivolte"]
    outer, _ = arch(w, zs, apex, n, off=bw)
    inner_c = [(x, z) for x, z in arc]
    B.new_shell()
    for i in range(len(arc) - 1):
        a, b, a2, b2 = inner_c[i], inner_c[i + 1], outer[i], outer[i + 1]
        B.face([(a[0], -bp, a[1]), (b[0], -bp, b[1]), (b2[0], -bp, b2[1]), (a2[0], -bp, a2[1])], "pierre", (0, -1, 0))
        mx, mz = (a2[0] + b2[0]) / 2, (a2[1] + b2[1]) / 2
        cxs = g[0] if mx > 0 else -g[0]
        B.face([(a2[0], -bp, a2[1]), (b2[0], -bp, b2[1]), (b2[0], 0, b2[1]), (a2[0], 0, a2[1])], "pierre", (mx - cxs, 0, mz - g[1]))
        B.face([(a[0], -bp, a[1]), (b[0], -bp, b[1]), (b[0], 0, b[1]), (a[0], 0, a[1])], "pierre", (cxs - (a[0] + b[0]) / 2, 0, g[1] - (a[1] + b[1]) / 2))
    for k in (0, len(arc) - 1):
        a, a2 = inner_c[k], outer[k]
        B.face([(a[0], -bp, a[1]), (a2[0], -bp, a2[1]), (a2[0], 0, a2[1]), (a[0], 0, a[1])], "pierre", (0, 0, -1))
    for sx in (1, -1):                                     # impostes (blocs à la naissance)
        x0 = sx * w
        B.box((min(x0, x0 + sx * (bw + 0.15)), -bp - 0.05, zs - 0.3), (max(x0, x0 + sx * (bw + 0.15)), 0, zs), "pierre")
    return B


def finish(B, name):
    mats = {k: C.tile_material(k) for k in B.mats}
    return [B.finish(name, mats, sharp_angle=40.0)]


PIECES = [
    ("SM_Voute_Ogive_8m", lambda P: finish(vault(P, P["travee"]), "SM_Voute_Ogive_8m")),
    ("SM_Voute_Ogive_4m", lambda P: finish(vault(P, P["travee"] / 2), "SM_Voute_Ogive_4m")),
    ("SM_ArcDoubleau_14m", lambda P: finish(rib(P), "SM_ArcDoubleau_14m")),
    ("SM_Colonne_Engagee_8m", lambda P: finish(column(P), "SM_Colonne_Engagee_8m")),
    ("SM_Mur_Arcade_8m", lambda P: finish(wall_with_arch(P, "arcade"), "SM_Mur_Arcade_8m")),
    ("SM_Mur_Niche_8m", lambda P: finish(wall_with_arch(P, "niche"), "SM_Mur_Niche_8m")),
]


def clearance(P):
    """Largeur libre sous les arcs-doubleaux à quelques hauteurs (passage du traqueur)."""
    s, z0, apex = P["demi_portee"], P["naissance"], P["cle"]
    a, R = C.pointed_arch_centers(s, apex - z0)
    r = R - P["doubleau"][1] - P["doubleau_filet"][1]
    out = []
    for z in (8.0, 11.75, 13.0, 14.0, 14.5):
        dz = z - z0
        half = (-a + math.sqrt(max(0.0, r * r - dz * dz))) if dz >= 0 else s
        out.append("%.2f m : %.1f m" % (z, max(0.0, 2 * half)))
    top = z0 + math.sqrt(max(0.0, r * r - a * a))
    return "clé sous l'arc-doubleau %.2f m ; largeur libre à " % top + ", ".join(out)


if __name__ == "__main__":
    only = os.environ.get("PIECES")
    for name, fn in PIECES:
        if only and name not in only.split(","):
            continue
        C.run_piece(name, fn, PARAMS, name)
    print(clearance(PARAMS))
