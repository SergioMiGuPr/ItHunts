# -*- coding: utf-8 -*-
"""Kit de l'acte I : caves (lot 2, nuit du 2026-11-22, branche art-kit-caves).

Salles organiques, pas carrées : parois de roche irrégulières et courbes, murs maçonnés brisés, plusieurs hauteurs,
éboulis, arches basses et hautes, ponts de pierre et de bois, canaux et bassins, gouffres, rebords et petites plateformes
pour les wall jumps, cellules (grille, porte, lit, paille, seau, chaînes murales), salle de garde, réserve, puits, porte
de sortie d'acte (classique, sans lumière colorée), torches, appliques, chandelles.

Lancer :  blender -b -P kit_caves.py           (exporte tout)   |   blender -b -P kit_caves.py -- SM_Cave_Puits …
Sorties : Assets/Art/Models/KitCaves/SM_Cave_*.fbx ; mesures dans kit_caves.json (lu par la doc).
"""
import os, sys, math, json, random
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import importlib
import kitlib as L
importlib.reload(L)
import kit_hauteur as H
importlib.reload(H)
from mathutils import Vector

OUT = os.path.join(L.ROOT, "Assets", "Art", "Models", "KitCaves")

ROCHE = "M_Bib_RocheCave_Peint"
MOEL = "M_Bib_Moellons_Peint"
PIERRE = "M_Bib_PierreTaillee_Peint"
BRIQ = "M_Bib_Briques_Peint"
DAL = "M_Bib_Dalles_Sales"
BOISP = "M_Bib_BoisPourri"
BOISN = "M_Bib_BoisNeuf"
BOISM = "M_Bib_BoisMouille"
FER = "M_Bib_FerRouille"
PAILLE = "M_Bib_HerbeSeche"
TERRE = "M_Bib_Terre"
CORDE = "M_Corde"
CIRE = "M_Kit_Cire"
FLAMME = "M_Kit_Flamme"
TOILE = "M_Kit_Toile"
EAU = "M_Eau_Toon"
NOIR = "M_Fond_Noir"

PIECES = {}


def piece(f):
    PIECES[f.__name__] = f
    return f


def K(n):
    return L.K(n)


# ------------------------------------------------------------------ parois de roche
def _paroi(nom, pts, h, seed, inward=-1, amp=0.75):
    k = K(nom)
    L.rock_wall(k, pts, h, ROCHE, thick=2.0, seg=0.85, amp=amp, seed=seed, inward=inward)
    return k.finish(seed, dict(sale_h=2.6, mousse=0.7))


for _h in (6, 10, 18):
    def _f(h=_h):
        return _paroi(f"SM_Cave_Paroi_Droite_{h}m", [(-6, 0), (-2, 0.7), (2, -0.5), (6, 0)], h, 10 + h)
    _f.__name__ = f"SM_Cave_Paroi_Droite_{_h}m"
    piece(_f)

    def _g(h=_h):
        # quart de cercle concave de rayon 10 m (coin de salle arrondi), face vers l'intérieur du cercle
        pts = [(10 * math.cos(a) - 10, 10 * math.sin(a) - 0) for a in [math.radians(t) for t in range(0, 91, 10)]]
        pts = [(x + 10, y) for x, y in pts]
        pts = [(x - 5, y - 5) for x, y in pts]
        return _paroi(f"SM_Cave_Paroi_Courbe_{h}m", pts, h, 30 + h, inward=1)
    _g.__name__ = f"SM_Cave_Paroi_Courbe_{_h}m"
    piece(_g)


@piece
def SM_Cave_Paroi_Saillie_10m():
    """Avancée rocheuse convexe (casse un mur droit, cache une jointure), 8 m de large, avance de 3 m."""
    pts = [(-4, 0), (-2.5, -2.2), (0, -3.0), (2.5, -2.0), (4, 0)]
    return _paroi("SM_Cave_Paroi_Saillie_10m", pts, 10, 51, inward=1)


@piece
def SM_Cave_PilierRoche_18m():
    """Pilier de roche naturel (Ø ≈ 5 m) du sol au plafond : point d'accroche de traction, contournement."""
    pts = [(2.6 * math.cos(a), 2.6 * math.sin(a)) for a in [2 * math.pi * i / 14 for i in range(14)]]
    k = K("SM_Cave_PilierRoche_18m")
    L.rock_wall(k, pts, 18, ROCHE, thick=1.2, seg=0.85, amp=0.6, seed=61, inward=-1, closed=True)
    k.empty("Accroche_Traction", (0, -3.2, 9))
    return k.finish(61, dict(mousse=0.6))


# ------------------------------------------------------------------ maçonnerie brisée
def _mur_brise(nom, h, seed):
    """Mur de moellons de 8 m, 1.2 m d'épaisseur, crête en gradins cassés, blocs tombés au pied."""
    rnd = random.Random(seed)
    k = K(nom)
    w, d = 8.0, 1.2
    n = 8
    for i in range(n):
        x0 = -w / 2 + i * w / n
        hh = h * (0.55 + 0.45 * abs(math.sin(i * 1.7 + seed))) if i not in (0, n - 1) else h
        hh = max(1.5, hh - rnd.uniform(0, 0.8))
        k.box((x0 + w / n / 2, 0, hh / 2), (w / n + 0.01, d, hh), MOEL)
        # blocs saillants sur la crête
        k.box((x0 + w / n / 2 + rnd.uniform(-0.2, 0.2), rnd.uniform(-0.1, 0.1), hh + 0.2), (rnd.uniform(0.5, 0.9), d * 0.9, 0.4), MOEL, rot=rnd.uniform(-0.1, 0.1))
    for j in range(6):
        L.rock_blob(k, (rnd.uniform(-3.5, 3.5), rnd.uniform(-2.2, -0.9), 0), (rnd.uniform(0.5, 1.1), rnd.uniform(0.4, 0.9), rnd.uniform(0.35, 0.7)), MOEL, seed=seed + j, res=8)
    return k.finish(seed, dict(sale_h=2.0, mousse=0.6))


@piece
def SM_Cave_MurBrise_4m():
    return _mur_brise("SM_Cave_MurBrise_4m", 4, 71)


@piece
def SM_Cave_MurBrise_8m():
    return _mur_brise("SM_Cave_MurBrise_8m", 8, 73)


# ------------------------------------------------------------------ éboulis
@piece
def SM_Cave_Eboulis_Petit():
    k = K("SM_Cave_Eboulis_Petit")
    rnd = random.Random(81)
    for j in range(9):
        r = rnd.uniform(0, 2.2)
        a = rnd.uniform(0, 6.28)
        s = rnd.uniform(0.4, 1.3) * (1.2 - r / 3)
        L.rock_blob(k, (math.cos(a) * r, math.sin(a) * r, 0), (s * 1.3, s, s * 0.8), ROCHE, seed=81 + j, res=8)
    return k.finish(81, dict(sale_h=1.0, mousse=0.4))


@piece
def SM_Cave_Eboulis_Grand():
    """Cône d'éboulis de 10 m (monte à 4.5 m) : rampe naturelle vers un rebord, gros blocs."""
    k = K("SM_Cave_Eboulis_Grand")
    rnd = random.Random(83)
    L.rock_blob(k, (0, 0, -1.2), (10, 8, 7), ROCHE, seed=83, res=14, amp=0.25)
    for j in range(14):
        r = rnd.uniform(1, 5)
        a = rnd.uniform(0, 6.28)
        z = max(0, 3.6 - r * 0.8)
        s = rnd.uniform(0.6, 1.8)
        L.rock_blob(k, (math.cos(a) * r, math.sin(a) * r * 0.8, z), (s * 1.3, s, s * 0.8), ROCHE, seed=90 + j, res=8)
    return k.finish(83, dict(sale_h=1.5, mousse=0.5))


# ------------------------------------------------------------------ arches
@piece
def SM_Cave_ArcheBasse():
    """Passage bas de joueur : ouverture 3 × 3.5 m dans un massif maçonné de 8 × 6 m (le traqueur ne passe pas)."""
    k = K("SM_Cave_ArcheBasse")
    L.masonry_arch(k, 3.0, 2.2, 1.3, 2.0, 2.5, MOEL, n=10, top=6.0, seed=91)
    return k.finish(91, dict(sale_h=2.0, mousse=0.5))


@piece
def SM_Cave_ArcheHaute():
    """Grande arche brisée : ouverture 8 × 11 m (porte des modules, le traqueur passe), massif de 14 × 15 m."""
    k = K("SM_Cave_ArcheHaute")
    L.masonry_arch(k, 8.0, 6.5, 4.5, 2.6, 3.0, PIERRE, n=14, pointed=True, top=15.0, seed=93)
    return k.finish(93, dict(sale_h=3.0, mousse=0.5))


@piece
def SM_Cave_ArcheRuine():
    """Arche brisée en ruine (moitié droite effondrée), ouverture 6 m : cadre de vue, repère."""
    k = K("SM_Cave_ArcheRuine")
    L.masonry_arch(k, 6.0, 4.5, 3.0, 1.8, 1.8, MOEL, n=12, top=9.0, broken=0.45, seed=95)
    rnd = random.Random(95)
    for j in range(7):
        L.rock_blob(k, (rnd.uniform(2, 6), rnd.uniform(-2, 2), 0), (1.1, 0.9, 0.7), MOEL, seed=96 + j, res=8)
    return k.finish(95, dict(sale_h=2.0, mousse=0.7))


# ------------------------------------------------------------------ ponts
@piece
def SM_Cave_PontPierre_16m():
    """Pont de pierre en arc au-dessus de l'eau ou du vide : 16 m, tablier de 4 m de large (dessus à 0), arche de 12 m,
    parapets bas de 0.8 m (on saute par-dessus), piles jusqu'à -10 m."""
    k = K("SM_Cave_PontPierre_16m")
    span, w = 16.0, 4.0
    # tablier
    k.box((0, 0, -0.5), (span, w, 1.0), DAL)
    # voûte sous le tablier (arc d'intrados 12 m, montée 5 m), parois latérales
    prof = L.arch_profile(12, 5, 12)
    for i in range(len(prof) - 1):
        (xa, za), (xb, zb) = prof[i], prof[i + 1]
        za -= 6.0
        zb -= 6.0
        for y, rev in ((-w / 2, False), (w / 2, True)):
            q = [(xa, y, za), (xb, y, zb), (xb, y, -1.0), (xa, y, -1.0)]
            k.face(list(reversed(q)) if rev else q, PIERRE)
        k.face([(xa, -w / 2, za), (xa, w / 2, za), (xb, w / 2, zb), (xb, -w / 2, zb)], PIERRE)
    # piles
    for sx in (-1, 1):
        k.box((sx * 7.0, 0, -6.0), (2.0, w + 0.4, 10.0), PIERRE)
    # parapets
    for sy in (-1, 1):
        for i in range(8):
            x = -span / 2 + 1 + i * 2
            k.box((x, sy * (w / 2 - 0.25), 0.4), (1.7, 0.5, 0.8), PIERRE)
    return k.finish(101, dict(sale_h=1.0, mousse=0.6))


@piece
def SM_Cave_PontBois_12m():
    """Passerelle de bois pourri : 12 m × 2.6 m, planches irrégulières (deux manquent), poteaux, mains courantes de corde."""
    rnd = random.Random(103)
    k = K("SM_Cave_PontBois_12m")
    n = 24
    for i in range(n):
        if i in (9, 17):
            continue
        x = -6 + (i + 0.5) * 12 / n
        k.box((x, rnd.uniform(-0.05, 0.05), -0.06 + rnd.uniform(-0.03, 0.02)), (12 / n - 0.05, 2.6 + rnd.uniform(-0.2, 0.15), 0.12), BOISP, rot=rnd.uniform(-0.03, 0.03))
    for sy in (-1, 1):
        k.box((0, sy * 1.0, -0.22), (12, 0.25, 0.2), BOISP)
        for x in (-6, -2, 2, 6):
            k.box((x, sy * 1.35, 0.3), (0.2, 0.2, 1.4 + 0.6), BOISP)
            k.box((x, sy * 1.35, -1.5), (0.22, 0.22, 3.0), BOISP)
        # corde en chaînette entre les poteaux
        for x0 in (-6, -2, 2):
            pts = []
            for t in range(9):
                u = t / 8
                pts.append(Vector((x0 + 4 * u, sy * 1.35, 1.1 - 0.35 * math.sin(math.pi * u))))
            for t in range(8):
                k.obox(pts[t], pts[t + 1], 0.07, 0.07, CORDE)
    return k.finish(103, dict(sale_h=0.6, mousse=0.3))


# ------------------------------------------------------------------ eau : canal, bassin
@piece
def SM_Cave_Canal_8m():
    """Canal maçonné : 8 m de long, 4 m d'eau entre deux margelles de 1 m, fond à -2.5 m, eau à -0.8 m."""
    k = K("SM_Cave_Canal_8m")
    for sy in (-1, 1):
        k.box((0, sy * 2.5, -1.25), (8, 1.0, 2.5), MOEL, skip=("-z",))
        k.box((0, sy * 2.55, 0.08), (8, 1.15, 0.16), DAL)
    k.box((0, 0, -2.75), (8, 4.0, 0.5), MOEL)
    k.face([(-4, -2, -0.8), (4, -2, -0.8), (4, 2, -0.8), (-4, 2, -0.8)], EAU)
    return k.finish(111, dict(sale_h=2.8, mousse=0.8, sol=-2.5))


@piece
def SM_Cave_Bassin():
    """Bassin rond de citerne (Ø 9 m intérieur), margelle de 0.9 m, eau à -0.6 m, fond à -3 m."""
    k = K("SM_Cave_Bassin")
    k.lathe((0, 0, 0), [(5.4, 0.0), (5.4, 0.7), (5.1, 0.9), (4.5, 0.9), (4.5, -3.0)], PIERRE, n=28)
    n = 28
    pts = [(4.5 * math.cos(2 * math.pi * i / n), 4.5 * math.sin(2 * math.pi * i / n), -0.6) for i in range(n)]
    k.face(pts, EAU)
    k.face([(4.5 * math.cos(2 * math.pi * i / n), 4.5 * math.sin(2 * math.pi * i / n), -3.0) for i in reversed(range(n))], MOEL)
    return k.finish(113, dict(sale_h=1.5, mousse=0.8))


# ------------------------------------------------------------------ gouffre, rebords, plateformes
@piece
def SM_Cave_Gouffre_Bord_12m():
    """Lèvre de gouffre : sol de 12 × 3 m qui s'arrête sur un bord déchiqueté, paroi qui plonge à -25 m (fond noir)."""
    k = K("SM_Cave_Gouffre_Bord_12m")
    rnd = random.Random(121)
    xs = [-6 + i * 1.0 for i in range(13)]
    edge = [(x, -rnd.uniform(0.0, 0.9)) for x in xs]
    # dessus
    for i in range(12):
        (xa, ya), (xb, yb) = edge[i], edge[i + 1]
        k.face([(xa, ya, 0), (xb, yb, 0), (xb, 3, 0), (xa, 3, 0)], TERRE)
    L.rock_wall(k, [(x, y) for x, y in edge], 25, ROCHE, thick=2.0, seg=1.0, amp=0.9, seed=121, inward=-1, dz=-25.0, top_jag=0.0, base_flare=0.0)
    k.face([(-6, -4, -25), (6, -4, -25), (6, 3, -25), (-6, 3, -25)], NOIR)
    return k.finish(121, dict(sale_h=1.0, mousse=0.4, sol=-25))


@piece
def SM_Cave_Rebord_4m():
    """Rebord de roche accroché à une paroi : 4 m × 1.4 m, dessus plat (on s'y hisse), dessous en encorbellement."""
    k = K("SM_Cave_Rebord_4m")
    k.box((0, -0.7, -0.3), (4, 1.4, 0.6), ROCHE)
    for j, x in enumerate((-1.3, 0.0, 1.3)):
        L.rock_blob(k, (x, -0.5, -1.4), (1.6, 1.2, 1.6), ROCHE, seed=131 + j, res=8, flat_base=False)
    k.empty("Accroche_Rebord", (0, -1.4, 0))
    return k.finish(131, dict(sale_h=0.5, mousse=0.6, sol=-2))


@piece
def SM_Cave_Plateforme_3m():
    """Petite plateforme de roche : dessus 3 × 3 m à 4 m (atteint au double saut + hissage), pied en colonne."""
    k = K("SM_Cave_Plateforme_3m")
    k.box((0, 0, 3.75), (3, 3, 0.5), ROCHE)
    pts = [(1.2 * math.cos(a), 1.2 * math.sin(a)) for a in [2 * math.pi * i / 10 for i in range(10)]]
    L.rock_wall(k, pts, 3.6, ROCHE, thick=0.6, seg=0.6, amp=0.35, seed=141, inward=-1, closed=True, top_jag=0.0)
    return k.finish(141, dict(sale_h=1.5, mousse=0.5))


@piece
def SM_Cave_Corniche_Bois_6m():
    """Corniche de planches sur consoles de bois, fixée à un mur : 6 × 1.6 m (route de wall jump), dessus à 0."""
    k = K("SM_Cave_Corniche_Bois_6m")
    for i in range(12):
        x = -3 + (i + 0.5) * 0.5
        k.box((x, -0.8, -0.06), (0.47, 1.6, 0.12), BOISP)
    for x in (-2.5, 0, 2.5):
        k.box((x, -0.7, -0.35), (0.25, 1.4, 0.3), BOISP)
        k.obox((x, -0.05, -1.6), (x, -1.3, -0.4), 0.22, 0.22, BOISP)
    return k.finish(151, dict(sale_h=0.3, mousse=0.3))


# ------------------------------------------------------------------ cellules
@piece
def SM_Cave_Cellule_Grille():
    """Façade de cellule : grille de fer de 6 × 5 m (barreaux Ø 6 cm tous les 15 cm, traverses), ouverture de porte de
    1.4 × 2.4 m au centre (la porte est SM_Cave_Cellule_Porte), linteau de pierre."""
    k = K("SM_Cave_Cellule_Grille")
    w, h = 6.0, 5.0
    x = -w / 2 + 0.1
    while x < w / 2:
        if not (-0.75 < x < 0.75):
            k.box((x, 0, h / 2), (0.06, 0.06, h), FER)
        x += 0.15
    for z in (0.15, 2.5, 4.85):
        k.box((-w / 4 - 0.4, 0, z), (w / 2 - 0.7, 0.1, 0.1), FER)
        k.box((w / 4 + 0.4, 0, z), (w / 2 - 0.7, 0.1, 0.1), FER)
    k.box((0, 0, 2.5), (1.6, 0.12, 0.12), FER)
    for sx in (-1, 1):
        k.box((sx * 0.75, 0, h / 2), (0.14, 0.14, h), FER)
    k.box((0, 0, h + 0.3), (w + 0.6, 0.8, 0.6), PIERRE)
    return k.finish(161, dict(sale_h=1.2, mousse=0.2))


@piece
def SM_Cave_Cellule_Porte():
    """Porte de cellule en barreaux, 1.4 × 2.4 m, gonds à gauche (pivot au gond), serrure."""
    k = K("SM_Cave_Cellule_Porte")
    for i in range(9):
        k.box((0.1 + i * 0.15, 0, 1.2), (0.05, 0.05, 2.36), FER)
    for z in (0.1, 1.2, 2.3):
        k.box((0.7, 0, z), (1.4, 0.08, 0.08), FER)
    k.box((1.25, -0.06, 1.1), (0.18, 0.08, 0.26), FER)
    return k.finish(163, dict(sale_h=0.6, mousse=0.1))


@piece
def SM_Cave_Lit():
    """Lit de cellule : bat-flanc de planches sur deux tréteaux, 2 × 0.9 m, dessus à 0.5 m, paillasse et couverture."""
    k = K("SM_Cave_Lit")
    for i in range(5):
        k.box((0, -0.36 + i * 0.18, 0.47), (2.0, 0.16, 0.06), BOISP)
    for x in (-0.85, 0.85):
        k.box((x, 0, 0.22), (0.1, 0.8, 0.44), BOISP)
    L.rock_blob(k, (0.05, 0, 0.42), (1.8, 0.78, 0.22), PAILLE, seed=165, res=10, amp=0.12)
    k.box((0.45, 0.02, 0.6), (0.9, 0.84, 0.06), TOILE, rot=0.05)
    return k.finish(165, dict(sale_h=0.4, mousse=0.0))


@piece
def SM_Cave_Paille():
    """Tas de paille au sol (1.6 m), à mettre dans un coin de cellule."""
    k = K("SM_Cave_Paille")
    L.rock_blob(k, (0, 0, -0.05), (1.6, 1.2, 0.45), PAILLE, seed=167, res=12, amp=0.3)
    return k.finish(167, dict(sale_h=0.2, mousse=0.0))


@piece
def SM_Cave_Seau():
    """Seau de bois cerclé de fer (Ø 0.32 × 0.34 m)."""
    k = K("SM_Cave_Seau")
    k.lathe((0, 0, 0), [(0.13, 0.0), (0.16, 0.34), (0.145, 0.34), (0.12, 0.03)], BOISP, n=14)
    k.cyl((0, 0, 0), 0.13, 0.02, BOISP, n=14)
    for z in (0.06, 0.27):
        k.cyl((0, 0, z), 0.142 + z * 0.06, 0.03, FER, n=14, caps=(False, False))
    return k.finish(169, dict(sale_h=0.2, mousse=0.0))


def _maillons(k, a, b, n, s=0.06):
    a, b = Vector(a), Vector(b)
    for i in range(n):
        p = a.lerp(b, (i + 0.5) / n)
        d = (b - a).normalized()
        L_ = (b - a).length / n
        if i % 2 == 0:
            k.obox(p - d * L_ * 0.55, p + d * L_ * 0.55, s, s * 0.35, FER)
        else:
            k.obox(p - d * L_ * 0.55, p + d * L_ * 0.55, s * 0.35, s, FER)


@piece
def SM_Cave_Chaines_Mur():
    """Chaînes murales de prisonnier : plaque d'ancrage à 1.9 m, deux chaînes qui pendent en U vers des fers ouverts.
    À poser **contre un mur de cellule** (dos de la pièce contre le mur, face vers +Z dans Unity)."""
    k = K("SM_Cave_Chaines_Mur")
    k.box((0, -0.04, 1.9), (0.5, 0.08, 0.3), FER)
    for sx in (-1, 1):
        a = (sx * 0.18, -0.1, 1.85)
        mid = (sx * 0.45, -0.18, 0.9)
        _maillons(k, a, mid, 10)
        k.cyl((sx * 0.45, -0.18, 0.78), 0.09, 0.07, FER, n=10, axis="y")
    return k.finish(171, dict(sale_h=0.5, mousse=0.0))


# ------------------------------------------------------------------ salle de garde, réserve
@piece
def SM_Cave_Banc():
    k = K("SM_Cave_Banc")
    k.box((0, 0, 0.44), (2.0, 0.4, 0.07), BOISN)
    for x in (-0.8, 0.8):
        k.box((x, 0, 0.21), (0.08, 0.34, 0.42), BOISN)
    return k.finish(181, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Cave_Tabouret():
    k = K("SM_Cave_Tabouret")
    k.cyl((0, 0, 0.42), 0.2, 0.05, BOISN, n=10)
    for a in range(3):
        t = a * 2 * math.pi / 3
        k.obox((math.cos(t) * 0.22, math.sin(t) * 0.22, 0), (math.cos(t) * 0.12, math.sin(t) * 0.12, 0.43), 0.05, 0.05, BOISN)
    return k.finish(183, dict(sale_h=0.2, mousse=0.0))


@piece
def SM_Cave_TableGarde():
    """Table de garde (2.2 × 1 m, dessus à 0.8 m) avec chope, écuelle, dés : vie récente, partie interrompue."""
    k = K("SM_Cave_TableGarde")
    k.box((0, 0, 0.78), (2.2, 1.0, 0.08), BOISN)
    for x in (-0.95, 0.95):
        for y in (-0.38, 0.38):
            k.box((x, y, 0.37), (0.1, 0.1, 0.74), BOISN)
    k.box((0, 0, 0.2), (1.9, 0.08, 0.08), BOISN)
    k.cyl((0.5, 0.2, 0.82), 0.05, 0.13, BOISN, n=10)
    k.lathe((-0.4, -0.15, 0.82), [(0.06, 0.0), (0.12, 0.05), (0.11, 0.06), (0.05, 0.01)], FER, n=12)
    for i in range(3):
        k.box((0.1 + i * 0.05, -0.3, 0.845), (0.03, 0.03, 0.03), "M_Os", rot=i)
    return k.finish(185, dict(sale_h=0.4, mousse=0.0))


@piece
def SM_Cave_Brasero():
    """Brasero sur trépied (Ø 0.8 m, coupe à 1 m) : source de lumière de la salle de garde (le prefab ajoute le feu)."""
    k = K("SM_Cave_Brasero")
    k.lathe((0, 0, 0.75), [(0.12, 0.0), (0.4, 0.22), (0.42, 0.3), (0.36, 0.3), (0.1, 0.06)], FER, n=14)
    for a in range(3):
        t = a * 2 * math.pi / 3
        k.obox((math.cos(t) * 0.45, math.sin(t) * 0.45, 0), (math.cos(t) * 0.15, math.sin(t) * 0.15, 0.8), 0.06, 0.06, FER)
    L.rock_blob(k, (0, 0, 0.9), (0.6, 0.6, 0.18), NOIR, seed=187, res=8)
    k.empty("Feu", (0, 0, 1.05))
    return k.finish(187, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Cave_Caisses_Pile():
    """Pile de caisses de réserve (décor, non attrapables) : 3 m de large, 2.4 m de haut."""
    rnd = random.Random(191)
    k = K("SM_Cave_Caisses_Pile")
    for (x, y, z, s) in [(-0.8, 0, 0, 1.0), (0.4, 0.1, 0, 1.1), (1.3, -0.2, 0, 0.8), (-0.4, 0.05, 1.05, 0.95), (0.6, 0, 1.1, 0.8), (-0.1, 0.1, 2.0, 0.6)]:
        r = rnd.uniform(-0.15, 0.15)
        k.box((x, y, z + s / 2), (s, s, s), BOISP, rot=r)
        for zz in (0.08, s - 0.08):
            k.box((x, y, z + zz), (s + 0.03, s + 0.03, 0.08), BOISN, rot=r)
    return k.finish(191, dict(sale_h=0.6, mousse=0.1))


@piece
def SM_Cave_Sacs():
    """Sacs de grain affaissés (tas de 1.8 m)."""
    rnd = random.Random(193)
    k = K("SM_Cave_Sacs")
    for j in range(5):
        L.rock_blob(k, (rnd.uniform(-0.6, 0.6), rnd.uniform(-0.4, 0.4), rnd.choice([0, 0, 0.35])), (0.9, 0.6, 0.45), TOILE, seed=193 + j, res=10, amp=0.15)
    return k.finish(193, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Cave_Etagere():
    """Étagère de réserve 2.4 × 0.6 × 2.6 m, pots et jarres."""
    k = K("SM_Cave_Etagere")
    for x in (-1.15, 1.15):
        for y in (-0.25, 0.25):
            k.box((x, y, 1.3), (0.08, 0.08, 2.6), BOISP)
    for z in (0.3, 1.1, 1.9, 2.55):
        k.box((0, 0, z), (2.4, 0.6, 0.06), BOISP)
    rnd = random.Random(195)
    for z in (0.33, 1.13, 1.93):
        x = -1.0
        while x < 1.0:
            r = rnd.uniform(0.1, 0.18)
            if rnd.random() < 0.75:
                k.lathe((x + r, rnd.uniform(-0.1, 0.1), z), [(r * 0.6, 0), (r, r * 0.8), (r * 0.9, r * 1.8), (r * 0.5, r * 2.2), (r * 0.55, r * 2.4)], BRIQ, n=10)
            x += 2 * r + 0.08
    return k.finish(195, dict(sale_h=0.4, mousse=0.0))


@piece
def SM_Cave_Tonneaux_Pile():
    """Trois tonneaux couchés sur un chantier de bois + un debout (réserve, cave à vin)."""
    k = K("SM_Cave_Tonneaux_Pile")
    for i, x in enumerate((-0.75, 0.0, 0.75)):
        k.cyl((x, -0.55, 0.45), 0.36, 1.1, BOISN, n=14, axis="y", rough=0.0)
        for yy in (-0.45, 0.45):
            k.cyl((x, yy - 0.03, 0.45), 0.375, 0.06, FER, n=14, axis="y", caps=(False, False))
    for x in (-1.1, 1.1):
        k.box((x, 0, 0.08), (0.12, 1.2, 0.16), BOISP)
    k.cyl((1.6, 0.2, 0), 0.34, 0.95, BOISN, n=14)
    return k.finish(197, dict(sale_h=0.3, mousse=0.0))


# ------------------------------------------------------------------ puits
@piece
def SM_Cave_Puits():
    """Puits (repère de l'acte I, refait au lot 3) : margelle lisse Ø 3.2 m sans marche ni arête vive, eau à -1.5 m, fond à -2.4 m.
    Margelle de 0.9 m à dessus arrondi, montants de bois posés au sol hors de la margelle, treuil ; on ressort du fond d'un
    saut et d'un hissage (3.3 m) ; corde et seau sans collision (enfant Deco_SansCollision)."""
    return H.puits(H.CAVE, "SM_Cave_Puits")


# ------------------------------------------------------------------ porte de sortie d'acte
@piece
def SM_Cave_PorteSortie():
    """Porte de sortie de l'acte I : encadrement de pierre de taille 12 × 13 m, ouverture 8 × 10 m en plein cintre,
    deux battants de bois épais cloutés de fer (pivots aux gonds : objets enfants Battant_G et Battant_D), sceau de lune de
    sortie en fer sur les battants (lot 3). Classique, sans lumière colorée : ce sont les torches du décor qui l'éclairent."""
    k = K("SM_Cave_PorteSortie")
    # relecture du lot 3 : archivolte sur la face avant seulement, battants contre la face arrière (y = 1.1), gonds 1 cm
    # derrière : ils s'ouvrent vers l'arrière sans traverser les piédroits
    L.masonry_arch(k, 8.0, 7.0, 3.6, 2.2, 2.2, PIERRE, n=14, top=13.0, seed=211, arriere=False)
    yg = 1.11
    yc = yg + 0.175
    k.box((0, 0, 0.1), (12.4, 3.2, 0.2), DAL)
    ob = k.finish(211, dict(sale_h=3.0, mousse=0.4))
    for side, sx in (("G", -1), ("D", 1)):
        kb = K(f"Battant_{side}")
        w = 4.0
        # planches verticales
        for i in range(8):
            x = sx * (i + 0.5) * w / 8
            hh = 7.0 + 3.6 * math.sqrt(max(0.0, 1 - ((abs(x)) / 4.0) ** 2)) - 0.05 - 0.21
            # nuit 4 : le battant commence au-dessus de la dalle de seuil (0.2 m) : plus de faces confondues avec elle
            kb.box((x, yc, 0.21 + hh / 2), (w / 8 - 0.02, 0.35, hh), BOISN)
        for z in (1.2, 3.0):
            kb.box((sx * w / 2, yc - 0.2, z), (w - 0.2, 0.06, 0.28), FER)
            for i in range(7):
                kb.box((sx * (0.3 + i * 0.55), yc - 0.24, z), (0.1, 0.05, 0.1), FER)
        kb.cyl((sx * 3.2, yc - 0.2, 3.6), 0.28, 0.08, FER, n=12, axis="y", caps=(True, True))
        # nuit 4, lot 3 : sceau de sortie (anneau partagé, croissant de lune à gauche, étoile à droite, rayons)
        H.sceau_lune(kb, sx, 0.0, 5.4, 1.6, yc - 0.215, FER, FER)
        b = kb.finish(212 + sx, dict(sale_h=1.5, mousse=0.1))
        # pivot au gond (x = ±4 m, y = 1.11) : la géométrie est décalée, l'objet est posé au gond
        for v in b.data.vertices:
            v.co.x -= sx * 4.0
            v.co.y -= yg
        b.parent = ob
        b.location = (sx * 4.0, yg, 0)
    return ob


# ------------------------------------------------------------------ lumières
@piece
def SM_Cave_Torche_Murale():
    """Torche sur bras de fer scellé au mur (dos à y = 0) : flamme à 0.75 m du mur, à la hauteur du pivot + 0.55 m.
    Le prefab ajoute la lumière (1.8 / 11 m) et le feu VFX_Torche_Feu à l'objet vide « Feu »."""
    k = K("SM_Cave_Torche_Murale")
    k.box((0, -0.03, 0), (0.18, 0.06, 0.4), FER)
    k.obox((0, -0.05, -0.05), (0, -0.55, 0.15), 0.05, 0.05, FER)
    k.cyl((0, -0.6, 0.12), 0.07, 0.05, FER, n=10, caps=(False, False))
    k.obox((0, -0.62, -0.25), (0, -0.7, 0.45), 0.06, 0.06, BOISN)
    k.cyl((0, -0.7, 0.36), 0.065, 0.14, "M_Chiffon_Brule", n=10)
    k.empty("Feu", (0, -0.7, 0.55))
    return k.finish(221, dict(sale_h=0.0, mousse=0.0))


@piece
def SM_Cave_Applique():
    """Applique de fer à trois bougies (dos à y = 0), 0.6 m de large : flammes à « Feu_1 » … « Feu_3 »."""
    k = K("SM_Cave_Applique")
    k.box((0, -0.03, 0), (0.12, 0.06, 0.5), FER)
    for i, x in enumerate((-0.25, 0.0, 0.25)):
        k.obox((0, -0.05, -0.1), (x, -0.3, 0.0 + (0.08 if x == 0 else 0)), 0.04, 0.04, FER)
        z = 0.0 + (0.08 if x == 0 else 0)
        k.cyl((x, -0.3, z), 0.06, 0.03, FER, n=10)
        k.cyl((x, -0.3, z + 0.03), 0.028, 0.18 - abs(x) * 0.2, CIRE, n=8)
        k.empty(f"Feu_{i + 1}", (x, -0.3, z + 0.25 - abs(x) * 0.2))
    return k.finish(223, dict(sale_h=0.0, mousse=0.0))


@piece
def SM_Cave_Chandelles():
    """Groupe de chandelles fondues au sol ou sur une table (0.4 m), coulures de cire : flammes « Feu_1 » … « Feu_5 »."""
    rnd = random.Random(225)
    k = K("SM_Cave_Chandelles")
    L.rock_blob(k, (0, 0, -0.03), (0.42, 0.36, 0.08), CIRE, seed=225, res=10, amp=0.25)
    for i in range(5):
        a = rnd.uniform(0, 6.28)
        r = rnd.uniform(0, 0.13)
        h = rnd.uniform(0.07, 0.26)
        x, y = math.cos(a) * r, math.sin(a) * r
        k.cyl((x, y, 0), 0.025 + rnd.uniform(0, 0.01), h, CIRE, n=8, rough=0.15, seed=i)
        k.empty(f"Feu_{i + 1}", (x, y, h + 0.035))
    return k.finish(225, dict(sale_h=0.0, mousse=0.0))


@piece
def SM_Cave_Candelabre():
    """Candélabre de fer sur pied (1.6 m), cinq bras : flammes « Feu_1 » … « Feu_5 »."""
    k = K("SM_Cave_Candelabre")
    for a in range(3):
        t = a * 2 * math.pi / 3
        k.obox((math.cos(t) * 0.3, math.sin(t) * 0.3, 0), (0, 0, 0.25), 0.04, 0.04, FER)
    k.cyl((0, 0, 0.2), 0.03, 1.2, FER, n=8)
    for i, ang in enumerate((-2, -1, 0, 1, 2)):
        x = ang * 0.14
        z = 1.4 + (0.12 if ang == 0 else 0)
        k.obox((0, 0, 1.3), (x, 0, z), 0.03, 0.03, FER)
        k.cyl((x, 0, z), 0.05, 0.03, FER, n=8)
        k.cyl((x, 0, z + 0.03), 0.022, 0.14, CIRE, n=8)
        k.empty(f"Feu_{i + 1}", (x, 0, z + 0.2))
    return k.finish(227, dict(sale_h=0.0, mousse=0.0))


# ------------------------------------------------------------------ nuit 4, lot 3 : hauteur (kit_hauteur.py)
H.enregistre(PIECES, H.CAVE, H.pieces_caves(H.CAVE))


# ------------------------------------------------------------------ principal
def premiere_phrase(doc):
    """Première phrase entière de la docstring (relecture du lot 3 : la première ligne seule tronquait les descriptions)."""
    d = " ".join(l.strip() for l in (doc or "").strip().split("\n"))
    i = d.find(". ")
    return d if i < 0 else d[:i + 1]


def main(noms=None):
    mesures = {}
    path = os.path.join(HERE, "kit_caves.json")
    if os.path.exists(path):
        mesures = json.load(open(path, encoding="utf-8"))
    for nom, f in PIECES.items():
        if noms and nom not in noms:
            continue
        L.reset()
        ob = f()
        L.export([ob], os.path.join(OUT, nom + ".fbx"))
        (x0, x1), (y0, y1), (z0, z1) = L.bounds(ob)
        t = L.tris(ob) + sum(L.tris(c) for c in ob.children_recursive if c.type == "MESH" and not c.name.startswith("Col_"))
        mesures[nom] = dict(doc=premiere_phrase(f.__doc__), taille=[round(x1 - x0, 2), round(y1 - y0, 2), round(z1 - z0, 2)],
                            bas=round(z0, 2), triangles=t, materiaux=[m.name for m in ob.data.materials])
        print("ok", nom, mesures[nom]["taille"], t, flush=True)
    json.dump(mesures, open(path, "w", encoding="utf-8"), ensure_ascii=False, indent=1)


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    main(argv or None)
