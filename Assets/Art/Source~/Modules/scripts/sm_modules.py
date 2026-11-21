# -*- coding: utf-8 -*-
"""Kit des nouveaux modules (lot H, 2026-11-21) d'après les 4 références de l'utilisateur (docs/art/references-modules :
prison à étages, caserne, citerne inondée, puits circulaire). Échelle du jeu : traqueur de 14 m, couloirs gothiques de 14 m,
joueur de 1.8 m. Modélisé par script dans Blender (aucune génération), UV en échelle monde (même projection et mêmes tuiles
que le kit gothique : pierre 4 × 3 m, dalles 4 × 4 m, fer 1 m, bois 1 m), matériaux du niveau 1 (pierre et dalles en
version *_Monde dans Unity).

Conventions : Blender Z en haut, avant vers -Y ; dans Unity +Y en haut, +Z vers l'avant. Pivot au sol, au centre de
l'emprise, sauf mention. Éléments de passage : repères vides `Accroche_*` (points d'accroche, pour le jeu) exportés dans le
FBX. Sorties : Assets/Art/Models/Modules/SM_Mod_*.fbx, .blend dans Assets/Art/Source~/Modules/, rendus dans
docs/art/rendus/Modules/, mesures dans modules_mesures.json (lu par INTEGRATION-PROPS).

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/Modules/scripts/sm_modules.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math, json
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Niveau1", "scripts"))
sys.modules.pop("n1_props", None)
import n1_props as N
C, F = N.C, N.F
import bpy
from mathutils import Vector

ROOT = C.ROOT
C.MODELS_DIR = os.path.join(ROOT, "Assets", "Art", "Models", "Modules")
C.SOURCE_DIR = os.path.join(ROOT, "Assets", "Art", "Source~", "Modules")
C.RENDER_DIR = os.path.join(ROOT, "docs", "art", "rendus", "Modules")
C.RENDER_DIST_ADD = 1.0
MESURES = {}


def B_():
    return N.builder()


def V(x, y, z):
    return Vector((x, y, z))


def accroche(name, loc, parent):
    return F.empty(name, loc, parent, size=0.3)


# ------------------------------------------------------------------ escaliers, échelles, passerelles
def escalier_pierre(P):
    """Escalier droit en pierre, 3 m de large, monte de 4 m sur 6 m (16 marches de 0.25 × 0.375), plein dessous."""
    m = N.materials(); B = B_()
    w, h, n = 3.0, 4.0, 16
    run = 6.0
    for i in range(n):
        y0 = -run / 2 + i * run / n
        B.box((-w / 2, y0, 0), (w / 2, y0 + run / n, (i + 1) * h / n), "pierre", skip=("-z",))
    ob = B.finish("SM_Mod_EscalierPierre_4m", m)
    return [ob]


def escalier_bois(P):
    """Escalier de bois ouvert : 2 m de large, 4 m de haut sur 5 m, 16 marches, limons et rampe d'un côté."""
    m = N.materials(); B = B_()
    w, h, run, n = 2.0, 4.0, 5.0, 16
    ang = math.atan2(h, run)
    L = math.hypot(h, run)
    c = V(0, 0, h / 2)
    ex, ey, ez = V(1, 0, 0), V(0, math.cos(ang), math.sin(ang)), V(0, -math.sin(ang), math.cos(ang))
    for sx in (-1, 1):
        N.obox(B, c + V(sx * (w / 2 - 0.06), 0, -0.05), ex, ey, ez, 0.06, L / 2, 0.18, "bois")
    for i in range(n):
        y = -run / 2 + (i + 0.5) * run / n
        z = (i + 1) * h / n
        B.box((-w / 2 + 0.12, y - 0.16, z - 0.05), (w / 2 - 0.12, y + 0.16, z), "bois")
    # rampe (côté +x) : poteaux tous les 4 marches + main courante
    for i in range(0, n + 1, 4):
        y = -run / 2 + i * run / n; z = i * h / n
        B.box((w / 2 - 0.1, y - 0.05, z), (w / 2, y + 0.05, z + 1.0), "bois")
    N.obox(B, c + V(w / 2 - 0.05, 0, 1.0), ex, ey, ez, 0.05, L / 2, 0.05, "bois")
    return [B.finish("SM_Mod_EscalierBois_4m", m)]


def echelle(P):
    """Échelle de bois : 0.6 m de large, 6 m, barreaux tous les 0.3 m. Pivot au pied, contre le mur (+Y)."""
    m = N.materials(); B = B_()
    H = 6.0
    for sx in (-0.3, 0.3):
        B.box((sx - 0.035, -0.03, 0), (sx + 0.035, 0.03, H), "bois")
    for k in range(1, int(H / 0.3)):
        C.tube(B, [V(-0.3, 0, k * 0.3), V(0.3, 0, k * 0.3)], 0.018, 5, "bois")
    ob = B.finish("SM_Mod_Echelle_6m", m)
    return [ob, accroche("Accroche_Bas", (0, -0.4, 0), ob), accroche("Accroche_Haut", (0, -0.4, H), ob)]


def planches(B, x0, x1, y0, y1, z, ep=0.06, larg=0.25, jeu=0.02, sens="x"):
    """Plancher de planches posées en travers (le long de x) entre y0 et y1."""
    y = y0
    while y < y1 - 0.01:
        y2 = min(y + larg, y1)
        B.box((x0, y, z - ep), (x1, y2 - jeu, z), "bois")
        y = y2


def garde_corps(B, x, y0, y1, z, h=1.1, pas=2.0):
    n = max(1, int(round((y1 - y0) / pas)))
    for i in range(n + 1):
        y = y0 + (y1 - y0) * i / n
        B.box((x - 0.06, y - 0.06, z), (x + 0.06, y + 0.06, z + h), "bois")
    B.box((x - 0.05, y0, z + h - 0.1), (x + 0.05, y1, z + h), "bois")
    B.box((x - 0.04, y0, z + h * 0.5 - 0.04), (x + 0.04, y1, z + h * 0.5 + 0.04), "bois")


def passerelle(P):
    """Passerelle de bois de 8 m × 2.5 m (dessus à 0), deux longerons, garde-corps de 1.1 m des deux côtés."""
    m = N.materials(); B = B_()
    L, W = 8.0, 2.5
    planches(B, -W / 2, W / 2, -L / 2, L / 2, 0.0)
    for sx in (-1, 1):
        B.box((sx * W / 2 - 0.15 * (sx > 0), -L / 2, -0.36), (sx * W / 2 + 0.15 * (sx < 0), L / 2, -0.06), "bois")
        garde_corps(B, sx * (W / 2 - 0.08), -L / 2, L / 2, 0.0)
    return [B.finish("SM_Mod_Passerelle_8m", m)]


def pilotis(P):
    """Passerelle sur pilotis : 8 m × 2 m, dessus à 0, 6 pieux qui descendent à −3 m (fond de la citerne), sans garde-corps."""
    m = N.materials(); B = B_()
    L, W = 8.0, 2.0
    planches(B, -W / 2, W / 2, -L / 2, L / 2, 0.0)
    for y in (-L / 2 + 0.2, 0.0, L / 2 - 0.2):
        for sx in (-1, 1):
            B.box((sx * (W / 2 - 0.1) - 0.1, y - 0.1, -3.0), (sx * (W / 2 - 0.1) + 0.1, y + 0.1, -0.06), "bois", skip=("-z",))
        B.box((-W / 2, y - 0.1, -0.3), (W / 2, y + 0.1, -0.06), "bois")
    return [B.finish("SM_Mod_PasserellePilotis_8m", m)]


def pont_cordes(P):
    """Pont de cordes et de planches : 12 m entre les poteaux, 1.6 m de large, flèche de 0.8 m ; poteaux de 1.4 m aux deux bouts.
    Pivot au milieu, au niveau des appuis (dessus des planches aux bouts)."""
    m = N.materials(); B = B_()
    L, W, sag = 12.0, 1.6, 0.8
    def z(y):
        return -sag * (1 - (2 * y / L) ** 2)
    k = 40
    for i in range(k):
        y = -L / 2 + (i + 0.5) * L / k
        if i % 1 == 0:
            B.box((-W / 2, y - 0.11, z(y) - 0.05), (W / 2, y + 0.11, z(y)), "bois")
    for sx in (-1, 1):
        pts = [V(sx * W / 2, -L / 2 + L * i / 24, z(-L / 2 + L * i / 24) - 0.03) for i in range(25)]
        C.tube(B, pts, 0.03, 5, "corde")
        top = [V(sx * W / 2, -L / 2 + L * i / 24, z(-L / 2 + L * i / 24) * 0.6 + 1.0) for i in range(25)]
        C.tube(B, top, 0.03, 5, "corde")
        for i in range(0, 25, 3):
            C.tube(B, [pts[i], top[i]], 0.012, 4, "corde")
        for sy in (-1, 1):
            B.box((sx * W / 2 - 0.1, sy * L / 2 - 0.1, -1.0), (sx * W / 2 + 0.1, sy * L / 2 + 0.1, 1.4), "bois")
    ob = B.finish("SM_Mod_PontCordes_12m", m)
    return [ob, accroche("Accroche_Bout_A", (0, -L / 2, 0), ob), accroche("Accroche_Bout_B", (0, L / 2, 0), ob)]


def balcon(P):
    """Balcon de pierre : dalle de 6 × 2.5 m (dessus à 0) portée par 3 consoles, garde-corps de bois. Pivot au ras du mur
    (le mur est en +Y, le balcon avance vers -Y)."""
    m = N.materials(); B = B_()
    Wd, D = 6.0, 2.5
    B.box((-Wd / 2, -D, -0.4), (Wd / 2, 0, 0), "dalles")
    for x in (-Wd / 2 + 0.5, 0, Wd / 2 - 0.5):
        B.prism_y([(x - 0.3, -0.4), (x + 0.3, -0.4), (x + 0.3, -0.6), (x, -1.6), (x - 0.3, -0.6)][::1], -1.2, 0.0, "pierre")
    B.box((-Wd / 2, -D, 0), (-Wd / 2 + 0.12, 0, 1.1), "bois")
    B.box((Wd / 2 - 0.12, -D, 0), (Wd / 2, 0, 1.1), "bois")
    B.box((-Wd / 2, -D, 1.0), (Wd / 2, -D + 0.1, 1.1), "bois")
    for i in range(7):
        x = -Wd / 2 + 0.06 + i * (Wd - 0.12) / 6
        B.box((x - 0.06, -D, 0), (x + 0.06, -D + 0.12, 1.0), "bois")
    return [B.finish("SM_Mod_Balcon_6m", m)]


# ------------------------------------------------------------------ cellules et portes
def barreaux(B, x0, x1, z0, z1, y, pas=0.16, r=0.022):
    n = int((x1 - x0) / pas)
    for i in range(n + 1):
        x = x0 + (x1 - x0) * i / n
        C.tube(B, [V(x, y, z0), V(x, y, z1)], r, 5, "fer")
    for z in (z0 + 0.12, (z0 + z1) / 2, z1 - 0.12):
        B.box((x0 - 0.03, y - 0.03, z - 0.03), (x1 + 0.03, y + 0.03, z + 0.03), "fer")


def porte_cellule(P):
    """Porte de cellule à barreaux : 1.2 × 2.4 m, cadre de fer, pivot au bas de la charnière (côté -X)."""
    m = N.materials(); B = B_()
    barreaux(B, 0.06, 1.14, 0.05, 2.35, 0.0)
    B.box((0, -0.04, 0), (0.06, 0.04, 2.4), "fer"); B.box((1.14, -0.04, 0), (1.2, 0.04, 2.4), "fer")
    B.box((0, -0.04, 2.34), (1.2, 0.04, 2.4), "fer"); B.box((0, -0.04, 0), (1.2, 0.04, 0.06), "fer")
    B.box((1.0, -0.09, 1.0), (1.14, 0.09, 1.2), "fer")
    return [B.finish("SM_Mod_PorteCellule", m)]


def cellule(P):
    """Cellule de 4 × 4 m, 3.5 m de haut : trois murs de pierre (0.5 m), plafond, façade à barreaux avec une ouverture de
    1.2 m pour SM_Mod_PorteCellule (charnière en x = −0.6). Pivot au sol, au milieu de la façade (façade vers -Y)."""
    m = N.materials(); B = B_()
    W, D, H, e = 4.0, 4.0, 3.5, 0.5
    B.box((-W / 2 - e, 0, 0), (-W / 2, D + e, H), "pierre")
    B.box((W / 2, 0, 0), (W / 2 + e, D + e, H), "pierre")
    B.box((-W / 2, D, 0), (W / 2, D + e, H), "pierre")
    B.box((-W / 2 - e, -0.2, H), (W / 2 + e, D + e, H + e), "pierre")
    B.box((-W / 2, 0, -0.1), (W / 2, D, 0), "dalles", skip=("-z",))
    barreaux(B, -W / 2 + 0.05, -0.65, 0.05, H - 0.05, 0.0)
    barreaux(B, 0.65, W / 2 - 0.05, 0.05, H - 0.05, 0.0)
    B.box((-0.65, -0.05, 2.4), (0.65, 0.05, H), "fer")
    ob = B.finish("SM_Mod_Cellule_4m", m)
    return [ob, F.empty("Porte_Charniere", (-0.6, 0, 0), ob, size=0.2)]


def grande_porte(W, H, nom):
    def build(P):
        """Encadrement de grande porte en pierre : ouverture W × H (m), arc brisé en haut, piédroits de 1.2 m, mur de 1.5 m
        d'épaisseur autour (2 m de chaque côté et au-dessus). Pivot au sol, au milieu de l'ouverture."""
        m = N.materials(); B = B_()
        j, e, marge = 1.2, 1.5, 2.0
        hw = W / 2
        z0 = H - hw * 0.9          # naissance de l'arc
        tot_w = hw + j + marge
        top = H + marge
        # profil de l'ouverture (arc brisé approché)
        arc = []
        for i in range(13):
            t = i / 12
            a = math.pi * t
            arc.append((hw * math.cos(a), z0 + (H - z0) * math.sin(a) ** 0.85))
        # mur : polygone extérieur moins l'ouverture -> faces avant/arrière découpées en bandes verticales
        xs = [p[0] for p in arc]
        for i in range(12):
            (xa, za), (xb, zb) = arc[i], arc[i + 1]
            B.prism_y([(xb, zb), (xa, za), (xa, top), (xb, top)], -e / 2, e / 2, "pierre")
        B.box((hw, -e / 2, 0), (tot_w, e / 2, top), "pierre")
        B.box((-tot_w, -e / 2, 0), (-hw, e / 2, top), "pierre")
        # piédroits saillants et archivolte
        for sx in (-1, 1):
            xa, xb = (hw, hw + 0.8) if sx > 0 else (-hw - 0.8, -hw)
            B.box((xa, -e / 2 - 0.25, 0), (xb, -e / 2, z0), "pierre")
        pts = [V(x * 1.08, -e / 2 - 0.12, z + 0.25) for x, z in arc]
        C.tube(B, pts, 0.3, 6, "pierre")
        ob = B.finish(nom, m)
        MESURES[nom] = {"ouverture": [W, H], "mur": [round(2 * tot_w, 2), round(top, 2), e]}
        return [ob]
    return build


# ------------------------------------------------------------------ piliers, arches, tuyaux, roue, sols
def arche_brisee(P):
    """Arche brisée de citerne : deux piliers de 1.2 m de section, 9 m de haut, 8 m d'axe en axe, arc en plein cintre dont
    il ne reste que les deux départs (le milieu s'est effondré). Pivot au sol, au milieu."""
    m = N.materials(); B = B_()
    s, Hh, span = 1.2, 9.0, 8.0
    for sx in (-1, 1):
        B.box((sx * span / 2 - s / 2, -s / 2, 0), (sx * span / 2 + s / 2, s / 2, Hh), "pierre")
        B.box((sx * span / 2 - s / 2 - 0.15, -s / 2 - 0.15, Hh), (sx * span / 2 + s / 2 + 0.15, s / 2 + 0.15, Hh + 0.3), "pierre")
    R = span / 2
    for sx in (-1, 1):
        n = 7
        for i in range(n):
            a0, a1 = math.pi * i / 18, math.pi * (i + 1) / 18
            x0, x1 = sx * R * math.cos(a0), sx * R * math.cos(a1)
            z0, z1 = Hh + 0.3 + R * math.sin(a0), Hh + 0.3 + R * math.sin(a1)
            poly = [(x0, z0), (x1, z1), (x1 * 1.0, z1 + 0.9), (x0, z0 + 0.9)] if sx > 0 else [(x1, z1), (x0, z0), (x0, z0 + 0.9), (x1, z1 + 0.9)]
            B.prism_y(poly, -s / 2, s / 2, "pierre")
    # blocs tombés au pied
    for (x, y, r) in ((0.6, 1.2, 0.5), (-0.4, -1.4, 0.4), (1.5, -0.6, 0.35)):
        B.box((x - r, y - r, 0), (x + r, y + r, r * 1.2), "pierre")
    return [B.finish("SM_Mod_ArcheBrisee_8m", m)]


def tuyau_gros(P):
    """Gros tuyau de fer (Ø 1.2 m) qui sort du mur (+Y) sur 2 m, coude vers le bas, bouche à 1 m sous l'axe, collerettes ;
    l'eau qui en tombe est un effet du jeu. Pivot au ras du mur, sur l'axe."""
    m = N.materials(); B = B_()
    r = 0.6
    pts = [V(0, 0.3, 0), V(0, -1.2, 0)] + [V(0, -1.2 - 0.8 * math.sin(a), -0.8 + 0.8 * math.cos(a)) for a in [math.pi / 2 * k / 6 for k in range(1, 7)]] + [V(0, -2.0, -1.4)]
    C.tube(B, pts, r, 12, "fer")
    C.tube(B, [V(0, -0.2, 0), V(0, -0.4, 0)], r + 0.1, 12, "fer")
    C.tube(B, [V(0, -2.0, -1.2), V(0, -2.0, -1.4)], r + 0.1, 12, "fer")
    ob = B.finish("SM_Mod_TuyauGros", m)
    return [ob, F.empty("FX_Bouche", (0, -2.0, -1.45), ob, size=0.3)]


def roue_eau(P):
    """Grande roue à eau de bois : Ø 8 m, 1.6 m de large, 16 aubes, axe de fer de 0.4 m sur deux paliers de pierre.
    Pivot au centre de l'axe (pour la faire tourner autour de X)."""
    m = N.materials(); B = B_()
    R, w, n = 4.0, 1.6, 16
    for sx in (-w / 2, w / 2):
        ring = [V(sx, R * math.cos(2 * math.pi * k / 32), R * math.sin(2 * math.pi * k / 32)) for k in range(32)]
        C.tube(B, ring, 0.12, 6, "bois", closed=True, up=V(1, 0, 0))
        for k in range(8):
            a = 2 * math.pi * k / 8
            C.tube(B, [V(sx, 0.3 * math.cos(a), 0.3 * math.sin(a)), V(sx, R * math.cos(a), R * math.sin(a))], 0.09, 5, "bois")
    for k in range(n):
        a = 2 * math.pi * k / n
        c = V(0, (R - 0.35) * math.cos(a), (R - 0.35) * math.sin(a))
        N.obox(B, c, V(1, 0, 0), V(0, math.cos(a), math.sin(a)), V(0, -math.sin(a), math.cos(a)), w / 2 + 0.05, 0.45, 0.04, "bois")
    C.tube(B, [V(-w / 2 - 1.0, 0, 0), V(w / 2 + 1.0, 0, 0)], 0.2, 10, "fer")
    ob = B.finish("SM_Mod_RoueEau_8m", m)
    return [ob]


def sol_inonde(P):
    """Sol inondé : dalle de 8 × 8 m (dessus à 0) affaissée en son milieu (−0.5 m), dalles soulevées et pierres éparses ; on
    pose l'eau (M_Eau_Toon) à −0.2 m. Pivot au sol, au centre."""
    m = N.materials(); B = B_()
    S = 8.0
    for i in range(4):
        for j in range(4):
            x0, y0 = -S / 2 + i * 2, -S / 2 + j * 2
            c = ((x0 + 1) ** 2 + (y0 + 1) ** 2) ** 0.5
            dz = -0.5 * max(0, 1 - c / 4.5)
            B.box((x0 + 0.02, y0 + 0.02, -0.3 + dz), (x0 + 1.98, y0 + 1.98, dz), "dalles", skip=("-z",))
    for (x, y, r) in ((1.2, -0.5, 0.35), (-1.8, 1.4, 0.45), (2.6, 2.2, 0.3), (-0.4, -2.6, 0.5)):
        B.box((x - r, y - r, -0.5), (x + r, y + r, -0.5 + r * 1.3), "pierre")
    return [B.finish("SM_Mod_SolInonde_8m", m)]


# ------------------------------------------------------------------ puits : spirale et coursives
def secteur(B, r0, r1, a0, a1, z0, z1, mat, n=6):
    """Secteur d'anneau plein (dessus à z1)."""
    for k in range(n):
        t0, t1 = a0 + (a1 - a0) * k / n, a0 + (a1 - a0) * (k + 1) / n
        poly = [(r0 * math.cos(t0), r0 * math.sin(t0)), (r1 * math.cos(t0), r1 * math.sin(t0)),
                (r1 * math.cos(t1), r1 * math.sin(t1)), (r0 * math.cos(t1), r0 * math.sin(t1))]
        B.prism_z(poly, z0, z1, mat, skip_bottom=False)


def spirale(P):
    """Segment d'escalier en spirale du puits : 45° de tour, rayon 9.5 à 12 m (contre la paroi du puits de 12 m), monte de
    2.5 m en 10 marches. Huit segments font un tour de 20 m de haut. Pivot au centre du puits, au pied du segment."""
    m = N.materials(); B = B_()
    r0, r1, n, H = 9.5, 12.0, 10, 2.5
    for k in range(n):
        a0, a1 = math.radians(45 * k / n), math.radians(45 * (k + 1) / n)
        secteur(B, r0, r1, a0, a1, (k + 1) * H / n - 0.6, (k + 1) * H / n, "pierre", n=1)
    # garde-corps de bois côté vide
    for k in range(0, n + 1, 2):
        a = math.radians(45 * k / n)
        z = k * H / n
        B.box((r0 * math.cos(a) - 0.06, r0 * math.sin(a) - 0.06, z), (r0 * math.cos(a) + 0.06, r0 * math.sin(a) + 0.06, z + 1.1), "bois")
    rail = [V((r0 + 0.0) * math.cos(math.radians(45 * k / 12)), (r0 + 0.0) * math.sin(math.radians(45 * k / 12)), 1.05 + H * k / 12) for k in range(13)]
    C.tube(B, rail, 0.05, 5, "bois")
    return [B.finish("SM_Mod_EscalierSpirale_45", m)]


def coursive(P):
    """Anneau de coursive du puits : segment de 45°, 2.5 m de large (rayon 9.5 à 12 m), dalle de 0.5 m (dessus à 0),
    consoles sous la dalle, garde-corps de bois côté vide. Pivot au centre du puits."""
    m = N.materials(); B = B_()
    r0, r1 = 9.5, 12.0
    secteur(B, r0, r1, 0, math.radians(45), -0.5, 0, "dalles")
    for k in range(4):
        a = math.radians(45 * (k + 0.5) / 4)
        c = V(((r0 + r1) / 2) * math.cos(a), ((r0 + r1) / 2) * math.sin(a), -0.9)
        N.obox(B, c, V(math.cos(a), math.sin(a), 0), V(-math.sin(a), math.cos(a), 0), V(0, 0, 1), 1.1, 0.25, 0.4, "pierre")
    for k in range(5):
        a = math.radians(45 * k / 4)
        B.box((r0 * math.cos(a) - 0.06, r0 * math.sin(a) - 0.06, 0), (r0 * math.cos(a) + 0.06, r0 * math.sin(a) + 0.06, 1.1), "bois")
    rail = [V(r0 * math.cos(math.radians(45 * k / 12)), r0 * math.sin(math.radians(45 * k / 12)), 1.05) for k in range(13)]
    C.tube(B, rail, 0.05, 5, "bois")
    return [B.finish("SM_Mod_Coursive_45", m)]


# ------------------------------------------------------------------ éléments de passage
def maillons(B, top, n, pas, fil, larg, axe=V(0, 0, -1), sag=None):
    """Chaîne de n maillons ovales alternés à 90°, depuis top dans la direction axe (ou le long d'une courbe sag(t))."""
    for i in range(n):
        if sag is None:
            c = top + axe * (pas * (i + 0.5))
            d = axe
        else:
            t0, t1 = i / n, (i + 1) / n
            p0, p1 = sag(t0), sag(t1)
            c = (p0 + p1) / 2
            d = (p1 - p0).normalized()
        side = V(1, 0, 0) if abs(d.x) < 0.9 else V(0, 0, 1)
        u = d.cross(side).normalized()
        if i % 2:
            u = d.cross(u).normalized()
        hl, hw = pas / 2 + fil, larg / 2 - fil
        ring = []
        for k in range(10):
            a = 2 * math.pi * k / 10
            ring.append(c + d * (hl * math.cos(a)) + u * (hw * math.sin(a)))
        C.tube(B, ring, fil, 4, "fer", closed=True, up=d.cross(u))


def chaine_grimpe(P):
    """Grosse chaîne suspendue à grimper (traqueur et joueur) : 12 m, maillons de 0.6 × 0.36 m, fil de Ø 0.12 m, anneau
    d'ancrage en haut. Pivot en haut (point d'ancrage)."""
    m = N.materials(); B = B_()
    L, pas = 12.0, 0.5
    n = int(L / pas)
    maillons(B, V(0, 0, -0.3), n, pas, 0.06, 0.36)
    B.box((-0.4, -0.4, -0.15), (0.4, 0.4, 0.15), "fer")
    ob = B.finish("SM_Mod_ChaineGrimpe_12m", m)
    acc = [ob]
    for k in range(0, 13, 3):
        acc.append(accroche("Accroche_%02dm" % k, (0, 0, -k), ob))
    MESURES["SM_Mod_ChaineGrimpe_12m"] = {"type": "chaine_grimpe", "longueur": L}
    return acc


def chaine_tendue(P):
    """Chaîne tendue entre deux points : 16 m d'ancrage à ancrage, flèche de 1.2 m, maillons de 0.5 m (fil Ø 0.1 m) ;
    platines d'ancrage aux bouts. Pivot au milieu de la ligne des ancrages."""
    m = N.materials(); B = B_()
    L, sag = 16.0, 1.2
    def courbe(t):
        y = -L / 2 + L * t
        return V(0, y, -sag * (1 - (2 * y / L) ** 2))
    n = int(L / 0.45)
    maillons(B, None, n, 0.45, 0.05, 0.3, sag=courbe)
    for sy in (-1, 1):
        B.box((-0.4, sy * L / 2 - 0.1 * (sy > 0) - 0.0, -0.4), (0.4, sy * L / 2 + 0.1 * (sy < 0) + 0.0, 0.4), "fer")
    ob = B.finish("SM_Mod_ChaineTendue_16m", m)
    return [ob, accroche("Accroche_A", (0, -L / 2, 0), ob), accroche("Accroche_Milieu", (0, 0, -sag), ob), accroche("Accroche_B", (0, L / 2, 0), ob)]


def plateforme_bond(P):
    """Plateforme de pierre pour les bonds : dessus de 3 × 3 m sur un pilier de 6 m (fût de 2 m), chapiteau. Pivot au sol,
    sous le centre ; dessus à 6 m."""
    m = N.materials(); B = B_()
    H = 6.0
    B.box((-1.0, -1.0, 0), (1.0, 1.0, H - 0.6), "pierre", skip=("-z",))
    B.box((-1.3, -1.3, H - 0.6), (1.3, 1.3, H - 0.3), "pierre")
    B.box((-1.5, -1.5, H - 0.3), (1.5, 1.5, H), "dalles")
    ob = B.finish("SM_Mod_PlateformeBond_6m", m)
    return [ob, accroche("Accroche_Dessus", (0, 0, H), ob)]


def rebord(P):
    """Rebord de pierre le long d'un mur : 6 m de long, 1.2 m de profondeur, 0.4 m d'épaisseur sur 3 consoles. Pivot au
    ras du mur (le mur est en +Y), au dessus du rebord."""
    m = N.materials(); B = B_()
    Lr, D = 6.0, 1.2
    B.box((-Lr / 2, -D, -0.4), (Lr / 2, 0, 0), "pierre")
    for x in (-Lr / 2 + 0.6, 0, Lr / 2 - 0.6):
        B.prism_y([(x - 0.25, -0.4), (x + 0.25, -0.4), (x + 0.25, -0.6), (x, -1.4), (x - 0.25, -0.6)], -D + 0.3, 0, "pierre")
    ob = B.finish("SM_Mod_Rebord_6m", m)
    return [ob, accroche("Accroche_Bord", (0, -D, 0), ob)]


def mur_grimpe(P):
    """Mur grimpable marqué : pan de 8 × 10 m (0.6 m d'épaisseur) en pierre brute, prises saillantes en quinconce (blocs de
    0.4 à 0.7 m qui dépassent de 0.25 à 0.45 m) et racines de bois qui descendent. Face grimpable vers -Y, pivot au sol au
    milieu de la face."""
    m = N.materials(); B = B_()
    W, H = 8.0, 10.0
    B.box((-W / 2, 0, 0), (W / 2, 0.6, H), "pierre")
    import random
    rnd = random.Random(7)
    rows = int(H / 0.9)
    for r in range(rows):
        for c in range(6):
            x = -W / 2 + 0.7 + c * (W - 1.4) / 5 + (0.6 if r % 2 else 0) + rnd.uniform(-0.2, 0.2)
            if abs(x) > W / 2 - 0.4:
                continue
            z = 0.6 + r * 0.9 + rnd.uniform(-0.15, 0.15)
            s = rnd.uniform(0.2, 0.35); d = rnd.uniform(0.25, 0.45)
            B.box((x - s, -d, z - s * 0.7), (x + s, 0.02, z + s * 0.5), "pierre")
    for k in range(4):
        x0 = -W / 2 + 1.2 + k * 1.9
        pts = [V(x0 + 0.3 * math.sin(z * 0.9 + k), -0.12 - 0.05 * math.sin(z * 2.3), H - z) for z in [i * 0.5 for i in range(17)]]
        C.tube(B, pts, 0.07, 5, "bois")
    ob = B.finish("SM_Mod_MurGrimpe_8x10", m)
    return [ob, accroche("Accroche_Pied", (0, -0.5, 0), ob), accroche("Accroche_Sommet", (0, -0.5, H), ob)]


def galerie(P):
    """Corniche et galerie haute : dalle de 8 × 3 m (dessus à 0) sur 4 consoles, parapet de pierre de 1 m avec créneaux
    côté vide (-Y), mur en +Y. Pivot au ras du mur, au dessus de la dalle."""
    m = N.materials(); B = B_()
    L, D = 8.0, 3.0
    B.box((-L / 2, -D, -0.5), (L / 2, 0, 0), "dalles")
    for x in (-L / 2 + 0.8, -1.0, 1.0, L / 2 - 0.8):
        B.prism_y([(x - 0.3, -0.5), (x + 0.3, -0.5), (x + 0.3, -0.8), (x, -2.2), (x - 0.3, -0.8)], -D + 0.5, 0, "pierre")
    B.box((-L / 2, -D, 0), (L / 2, -D + 0.4, 0.7), "pierre")
    for k in range(8):
        x = -L / 2 + 0.5 + k * (L - 1) / 7
        B.box((x - 0.3, -D, 0.7), (x + 0.3, -D + 0.4, 1.1), "pierre")
    ob = B.finish("SM_Mod_Galerie_8m", m)
    return [ob, accroche("Accroche_Bord", (0, -D + 0.4, 0), ob)]


# ------------------------------------------------------------------ props simples (absents du catalogue)
def lits(P):
    """Lits superposés de caserne : 2 × 0.9 m, 1.9 m de haut, deux couchages (0.4 et 1.4 m), échelle sur le côté."""
    m = N.materials(); B = B_()
    for x in (-0.95, 0.95):
        for y in (-0.4, 0.4):
            B.box((x - 0.05, y - 0.05, 0), (x + 0.05, y + 0.05, 1.9), "bois")
    for z in (0.4, 1.4):
        B.box((-1.0, -0.45, z - 0.08), (1.0, 0.45, z), "bois")
        B.box((-0.95, -0.42, z), (0.95, 0.42, z + 0.12), "chiffon")
    for k in range(4):
        C.tube(B, [V(1.0, -0.15, 0.35 + k * 0.35), V(1.0, 0.15, 0.35 + k * 0.35)], 0.02, 5, "bois")
    return [B.finish("SM_Mod_LitsSuperposes", m)]


def ratelier(P):
    """Râtelier d'armes : 2 × 0.4 m, 1.8 m de haut, 6 lances (hampe de bois, fer)."""
    m = N.materials(); B = B_()
    for x in (-1.0, 1.0):
        B.box((x - 0.06, -0.2, 0), (x + 0.06, 0.2, 1.8), "bois")
    for z in (0.15, 1.2):
        B.box((-1.0, -0.15, z), (1.0, 0.15, z + 0.08), "bois")
    for k in range(6):
        x = -0.8 + k * 0.32
        C.tube(B, [V(x, 0, 0.15), V(x, 0.05, 2.1)], 0.025, 5, "bois")
        B.prism_y([(x - 0.05, 2.1), (x + 0.05, 2.1), (x, 2.4)], -0.01, 0.06, "fer")
    return [B.finish("SM_Mod_Ratelier", m)]


def table(P):
    """Table de bois : 2.4 × 1 m, plateau à 0.8 m, deux bancs."""
    m = N.materials(); B = B_()
    B.box((-1.2, -0.5, 0.72), (1.2, 0.5, 0.8), "bois")
    for x in (-1.0, 1.0):
        for y in (-0.35, 0.35):
            B.box((x - 0.06, y - 0.06, 0), (x + 0.06, y + 0.06, 0.72), "bois")
    for y in (-0.85, 0.85):
        B.box((-1.1, y - 0.15, 0.4), (1.1, y + 0.15, 0.46), "bois")
        for x in (-0.95, 0.95):
            B.box((x - 0.05, y - 0.1, 0), (x + 0.05, y + 0.1, 0.4), "bois")
    return [B.finish("SM_Mod_Table", m)]


def tonneau(P):
    """Tonneau : Ø 0.7 m (0.6 aux bouts), 1 m de haut, cercles de fer."""
    m = N.materials(); B = B_()
    prof = [(0.0, 0.0), (0.3, 0.0), (0.35, 0.25), (0.37, 0.5), (0.35, 0.75), (0.3, 1.0), (0.0, 1.0)]
    C.revolve(B, prof, 14, "bois", cap_top=True, cap_bottom=True)
    for z in (0.12, 0.88, 0.35, 0.65):
        r = 0.35 if z in (0.35, 0.65) else 0.31
        C.revolve(B, [(r + 0.012, z - 0.03), (r + 0.012, z + 0.03)], 14, "fer")
    return [B.finish("SM_Mod_Tonneau", m)]


PIECES = [
    ("SM_Mod_EscalierPierre_4m", escalier_pierre), ("SM_Mod_EscalierBois_4m", escalier_bois), ("SM_Mod_Echelle_6m", echelle),
    ("SM_Mod_Passerelle_8m", passerelle), ("SM_Mod_PasserellePilotis_8m", pilotis), ("SM_Mod_PontCordes_12m", pont_cordes),
    ("SM_Mod_Balcon_6m", balcon), ("SM_Mod_PorteCellule", porte_cellule), ("SM_Mod_Cellule_4m", cellule),
    ("SM_Mod_GrandePorte_4x10", grande_porte(4, 10, "SM_Mod_GrandePorte_4x10")),
    ("SM_Mod_GrandePorte_6x10", grande_porte(6, 10, "SM_Mod_GrandePorte_6x10")),
    ("SM_Mod_GrandePorte_6x12", grande_porte(6, 12, "SM_Mod_GrandePorte_6x12")),
    ("SM_Mod_GrandePorte_8x14", grande_porte(8, 14, "SM_Mod_GrandePorte_8x14")),
    ("SM_Mod_GrandePorte_8x16", grande_porte(8, 16, "SM_Mod_GrandePorte_8x16")),
    ("SM_Mod_ArcheBrisee_8m", arche_brisee), ("SM_Mod_TuyauGros", tuyau_gros), ("SM_Mod_RoueEau_8m", roue_eau),
    ("SM_Mod_SolInonde_8m", sol_inonde), ("SM_Mod_EscalierSpirale_45", spirale), ("SM_Mod_Coursive_45", coursive),
    ("SM_Mod_ChaineGrimpe_12m", chaine_grimpe), ("SM_Mod_ChaineTendue_16m", chaine_tendue),
    ("SM_Mod_PlateformeBond_6m", plateforme_bond), ("SM_Mod_Rebord_6m", rebord), ("SM_Mod_MurGrimpe_8x10", mur_grimpe),
    ("SM_Mod_Galerie_8m", galerie), ("SM_Mod_LitsSuperposes", lits), ("SM_Mod_Ratelier", ratelier), ("SM_Mod_Table", table),
    ("SM_Mod_Tonneau", tonneau),
]


def run(names=None):
    out = {}
    for name, build in PIECES:
        if names and name not in names:
            continue
        res = C.run_piece(name, build, {}, name, views=("34",))
        out[name] = res
    # mesures (taille hors tout, pivot, triangles) depuis le réimport
    mes = {}
    for name, res in out.items():
        mes[name] = {"res": str(res)[:400]}
    path = os.path.join(HERE, "modules_mesures.json")
    old = json.load(open(path, encoding="utf-8")) if os.path.exists(path) else {}
    old.update({k: v for k, v in MESURES.items()})
    json.dump(old, open(path, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    return out


if __name__ == "__main__":
    run()
