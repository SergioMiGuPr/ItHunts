# -*- coding: utf-8 -*-
"""Kit de l'acte II : étages hauts du château (lot 3, nuit du 2026-11-22, branche art-kit-chateau).

Plafonds plus hauts, grands halls, grandes entrées, voûtes d'ogives, galeries et balcons, grands escaliers, hautes
fenêtres (les rayons de lune sont des prefabs : `Chateau_RayonLune`), chandeliers suspendus, tentures, bibliothèques,
tableaux sombres, meubles renversés, salle de bal, chapelle, chambres ; grande porte d'entrée du château (mène à l'acte III).

Lancer :  blender -b -P kit_chateau.py [-- SM_Chateau_… …]      Sorties : Assets/Art/Models/KitChateau/
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

OUT = os.path.join(L.ROOT, "Assets", "Art", "Models", "KitChateau")

PIERRE = "M_Bib_PierreTaillee_Peint"
BRIQ = "M_Bib_Briques_Peint"
PLATRE = "M_Bib_PlatreFissure_Sale"
MARBRE = "M_Bib_Marbre"
DAL = "M_Bib_Dalles_Sales"
BOIS = "M_Bib_BoisNeuf"
BOISP = "M_Bib_BoisPourri"
FER = "M_Bib_FerRouille"
TAPIS = "M_Bib_Tapis"
TENT = "M_Bib_Tenture"
TRIM = "M_Bib_Trim"
CIRE = "M_Kit_Cire"
VITRAIL = "M_Kit_Vitrail"
TOILE = "M_Kit_Toile"
PEINT = "M_Kit_Toile_Peinte"
NOIR = "M_Fond_Noir"
CORDE = "M_Corde"

PIECES = {}


def piece(f):
    PIECES[f.__name__] = f
    return f


def K(n):
    return L.K(n)


def corniche(k, x0, x1, z, d=0.6, y=0.0, m=PIERRE):
    """Corniche moulurée (trois ressauts) le long de x, face vers -Y, posée au mur en y."""
    k.box(((x0 + x1) / 2, y - d * 0.25, z), (x1 - x0, d * 0.5, 0.3), m)
    k.box(((x0 + x1) / 2, y - d * 0.5, z + 0.3), (x1 - x0, d, 0.25), m)
    k.box(((x0 + x1) / 2, y - d * 0.35, z + 0.55), (x1 - x0, d * 0.7, 0.2), m)


def balustrade(k, a, b, h=1.1, m=PIERRE, pas=0.45):
    """Balustrade de pierre entre a et b (x, y au sol z) : main courante, balustres tournés, socle."""
    a, b = Vector(a), Vector(b)
    d = b - a
    n = max(2, int(d.length / pas))
    k.obox(a + Vector((0, 0, 0.1)), b + Vector((0, 0, 0.1)), 0.35, 0.2, m)
    k.obox(a + Vector((0, 0, h - 0.08)), b + Vector((0, 0, h - 0.08)), 0.4, 0.16, m)
    for i in range(1, n):
        p = a.lerp(b, i / n)
        k.lathe((p.x, p.y, p.z + 0.2), [(0.07, 0), (0.11, 0.15), (0.06, 0.35), (0.12, 0.55), (0.06, 0.75), (0.08, h - 0.36)], m, n=8)


# ------------------------------------------------------------------ murs et piliers
def _mur(nom, w, h, bas, haut, seed, fenetre=None):
    k = K(nom)
    t = 1.0
    if fenetre is None:
        k.box((0, 0.5, h / 2), (w, t, h), haut, skip=())
    else:
        fw, fz0, fz1 = fenetre
        # mur percé d'une baie en lancette : montants, allège, tympan plein au-dessus du cintre
        k.box((-(w / 2 + fw / 2) / 2 - 0.0, 0.5, h / 2), (w / 2 - fw / 2, t, h), haut)
        k.box(((w / 2 + fw / 2) / 2, 0.5, h / 2), (w / 2 - fw / 2, t, h), haut)
        k.box((0, 0.5, fz0 / 2), (fw, t, fz0), haut)
        prof = L.arch_profile(fw, fw * 0.9, 12, pointed=True)
        spring = fz1 - fw * 0.9
        for i in range(len(prof) - 1):
            (xa, za), (xb, zb) = prof[i], prof[i + 1]
            for y, rev in ((0.0, False), (1.0, True)):
                q = [(xa, y, za + spring), (xb, y, zb + spring), (xb, y, h), (xa, y, h)]
                k.face(list(reversed(q)) if rev else q, haut)
            k.face([(xa, 0, za + spring), (xa, 1, za + spring), (xb, 1, zb + spring), (xb, 0, zb + spring)], haut)
        # ébrasement : remplage (meneau central, deux lancettes, oculus) et vitrage sombre
        k.box((0, 0.5, (fz0 + spring) / 2 + 0.5), (0.22, 0.3, spring - fz0 + 1.0), PIERRE)
        k.cyl((0, 0.5, spring + fw * 0.5), fw * 0.18, 0.3, PIERRE, n=12, axis="y", caps=(False, False))
        k.face([(-fw / 2, 0.62, fz0), (fw / 2, 0.62, fz0), (fw / 2, 0.62, spring), (-fw / 2, 0.62, spring)], VITRAIL)
        for i in range(len(prof) - 1):
            (xa, za), (xb, zb) = prof[i], prof[i + 1]
            k.face([(xa, 0.62, spring), (xb, 0.62, spring), (xb, 0.62, zb + spring), (xa, 0.62, za + spring)], VITRAIL)
        # appui saillant
        k.box((0, -0.15, fz0 - 0.1), (fw + 0.8, 0.5, 0.25), PIERRE)
    # soubassement (lambris de pierre) et corniche
    if bas != haut:
        k.box((0, -0.05, 1.6), (w, 0.15, 3.2), bas)
    k.box((0, -0.12, 0.25), (w, 0.35, 0.5), PIERRE)
    corniche(k, -w / 2, w / 2, h - 1.0, 0.7)
    return k.finish(seed, dict(sale_h=2.0, mousse=0.15))


@piece
def SM_Chateau_Mur_8x12():
    """Mur de salle 8 × 12 m (1 m d'épaisseur, face vers +Z) : pierre de taille, plinthe, corniche à 11 m."""
    return _mur("SM_Chateau_Mur_8x12", 8, 12, PIERRE, PIERRE, 301)


@piece
def SM_Chateau_Mur_8x20():
    """Mur de grand hall 8 × 20 m : pierre de taille, plinthe, corniche à 19 m."""
    return _mur("SM_Chateau_Mur_8x20", 8, 20, PIERRE, PIERRE, 303)


@piece
def SM_Chateau_Mur_Platre_8x12():
    """Mur d'appartement 8 × 12 m : plâtre fissuré au-dessus d'un lambris de pierre de 3.2 m (chambres, bibliothèque)."""
    return _mur("SM_Chateau_Mur_Platre_8x12", 8, 12, PIERRE, PLATRE, 305)


@piece
def SM_Chateau_Fenetre_Haute():
    """Haute fenêtre en lancette dans un mur de 8 × 20 m : baie de 3.2 m de large, de 4 m à 15 m, remplage et vitrage
    sombre ; le rayon de lune est le prefab Chateau_RayonLune (à poser devant, orienté vers la salle)."""
    k_ = _mur("SM_Chateau_Fenetre_Haute", 8, 20, PIERRE, PIERRE, 307, fenetre=(3.2, 4.0, 15.0))
    return k_


@piece
def SM_Chateau_Pilier_20m():
    """Pilier gothique fasciculé de 20 m (Ø 2.2 m) : base moulurée, quatre colonnettes, chapiteau à feuillages simplifié,
    tailloir à 18 m qui reçoit les ogives de SM_Chateau_Voute_8x8."""
    k = K("SM_Chateau_Pilier_20m")
    k.lathe((0, 0, 0), [(1.5, 0), (1.5, 0.5), (1.3, 0.7), (1.25, 1.1), (1.1, 1.2)], PIERRE, n=16)
    k.cyl((0, 0, 1.2), 0.9, 16.6, PIERRE, n=12)
    for a in range(4):
        t = a * math.pi / 2 + math.pi / 4
        k.cyl((math.cos(t) * 0.85, math.sin(t) * 0.85, 1.2), 0.32, 16.6, PIERRE, n=8)
    k.lathe((0, 0, 17.8), [(1.0, 0), (1.25, 0.4), (1.45, 0.9), (1.6, 1.0), (1.6, 1.4), (0.0, 1.4)], PIERRE, n=16)
    k.empty("Accroche_Traction", (0, -1.4, 12))
    return k.finish(311, dict(sale_h=2.5, mousse=0.1))


@piece
def SM_Chateau_Colonne_12m():
    """Colonne fine de 12 m (Ø 0.9 m) : galeries, arcades, chapelle."""
    k = K("SM_Chateau_Colonne_12m")
    k.lathe((0, 0, 0), [(0.75, 0), (0.75, 0.4), (0.55, 0.6), (0.45, 0.75)], PIERRE, n=12)
    k.cyl((0, 0, 0.75), 0.42, 10.4, PIERRE, n=12)
    k.lathe((0, 0, 11.15), [(0.45, 0), (0.7, 0.45), (0.8, 0.55), (0.8, 0.85), (0, 0.85)], PIERRE, n=12)
    return k.finish(313, dict(sale_h=2.0, mousse=0.05))


@piece
def SM_Chateau_Voute_8x8():
    """Travée de voûte d'ogives 8 × 8 m (posée sur les tailloirs à 19.4 m : pivot au niveau des tailloirs, au centre) :
    deux ogives croisées, arcs formerets, quatre voûtains, clé pendante. Monte de 5 m."""
    k = K("SM_Chateau_Voute_8x8")
    rise = 5.0
    n = 10
    # voûtains : surface z = rise * (1 - max(|x|,|y|)/4)^0.6 approchée par une grille
    def Z(x, y):
        r = max(abs(x), abs(y)) / 4.0
        return rise * (1 - r ** 1.6) ** 0.6
    k.grid(lambda i, j: Vector((-4 + 8 * i / n, -4 + 8 * j / n, Z(-4 + 8 * i / n, -4 + 8 * j / n))), n, n, PLATRE, flip=True)
    # ogives (diagonales) et formerets (bords), en nervures saillantes
    for (x0, y0, x1, y1) in ((-4, -4, 4, 4), (-4, 4, 4, -4)):
        pts = []
        for i in range(n + 1):
            t = i / n
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            pts.append(Vector((x, y, rise * math.sin(math.pi * t) ** 0.8 - 0.35)))
        for i in range(n):
            k.obox(pts[i], pts[i + 1], 0.45, 0.6, PIERRE)
    for (x0, y0, x1, y1) in ((-4, -4, 4, -4), (-4, 4, 4, 4), (-4, -4, -4, 4), (4, -4, 4, 4)):
        pts = []
        for i in range(n + 1):
            t = i / n
            pts.append(Vector((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, (rise * 0.55) * math.sin(math.pi * t) ** 0.7 - 0.3)))
        for i in range(n):
            k.obox(pts[i], pts[i + 1], 0.4, 0.5, PIERRE)
    k.lathe((0, 0, rise - 1.2), [(0.0, 0), (0.35, 0.2), (0.6, 0.7), (0.5, 1.1), (0.3, 1.2)], PIERRE, n=10)
    return k.finish(315, dict(sale_h=0.0, mousse=0.0, teinte=0.5))


@piece
def SM_Chateau_Arcade_8x12():
    """Arcade de passage entre deux halls : baie en arc brisé de 6 × 10 m dans un mur de 8 × 14 m."""
    k = K("SM_Chateau_Arcade_8x12")
    L.masonry_arch(k, 6.0, 6.0, 4.0, 1.4, 1.0, PIERRE, n=12, pointed=True, top=14.0, seed=317)
    return k.finish(317, dict(sale_h=2.0, mousse=0.1))


# ------------------------------------------------------------------ galeries, balcons, escaliers
@piece
def SM_Chateau_Galerie_8m():
    """Galerie haute de 8 m sur consoles (dessus à 0, profondeur 4 m, dos contre le mur en y = 0) : dallage, balustrade
    de pierre côté vide, consoles en quart de rond (le traqueur ne s'y pose pas : 4 m < 7 m)."""
    k = K("SM_Chateau_Galerie_8m")
    k.box((0, -2.0, -0.4), (8, 4, 0.8), DAL)
    for x in (-3, 0, 3):
        for j in range(4):
            k.box((x, -0.4 - j * 0.85, -1.1 - (3 - j) * 0.35), (0.8, 0.9, 0.7 + (3 - j) * 0.7), PIERRE)
    balustrade(k, (-4, -3.8, 0), (4, -3.8, 0))
    k.box((0, -2.0, -0.85), (8, 4, 0.1), PIERRE)
    return k.finish(321, dict(sale_h=0.3, mousse=0.05))


@piece
def SM_Chateau_Balcon():
    """Balcon de 4 × 2.4 m en encorbellement (dessus à 0, dos au mur), balustrade sur trois côtés : point de vue, cible
    de traction."""
    k = K("SM_Chateau_Balcon")
    k.box((0, -1.2, -0.35), (4, 2.4, 0.7), DAL)
    k.lathe((0, -1.2, -2.4), [(0.2, 0), (0.9, 0.9), (1.6, 1.7), (1.7, 1.75)], PIERRE, n=12)
    balustrade(k, (-2, -2.3, 0), (2, -2.3, 0))
    balustrade(k, (-1.9, -0.1, 0), (-1.9, -2.3, 0))
    balustrade(k, (1.9, -0.1, 0), (1.9, -2.3, 0))
    k.empty("Accroche_Traction", (0, -2.4, 1.2))
    return k.finish(323, dict(sale_h=0.3, mousse=0.05))


@piece
def SM_Chateau_GrandEscalier():
    """Grand escalier droit de 8 m de large qui monte de 8 m (32 marches de 0.25 × 0.5 m), tapis rouge central, rampes
    de pierre à balustres, gros pilastres de départ avec boules. Pied à y = 0, monte vers +y (vers l'arrière)."""
    k = K("SM_Chateau_GrandEscalier")
    n, w = 32, 8.0
    for i in range(n):
        z = (i + 1) * 0.25
        y = i * 0.5
        k.box((0, y + 0.25, z / 2), (w, 0.5, z), PIERRE, skip=("-z",))
        k.box((0, y + 0.22, z + 0.01), (3.0, 0.48, 0.02), TAPIS)
        k.box((0, y + 0.02, z - 0.05), (w + 0.04, 0.08, 0.12), PIERRE)
    for sx in (-1, 1):
        x = sx * (w / 2 + 0.2)
        k.box((x, 8.0, 4.0), (0.4, 16, 0.4), PIERRE, skip=())
        balustrade(k, (x, 0.5, 0.4), (x, 16.0, 8.4), h=1.0, pas=0.55)
        k.box((x, 0.0, 0.8), (0.9, 0.9, 1.6), PIERRE)
        k.lathe((x, 0.0, 1.6), [(0.0, 0), (0.3, 0.05), (0.38, 0.35), (0.3, 0.65), (0.0, 0.7)], PIERRE, n=10)
    k.box((0, 18.0, 7.8), (w + 1.0, 4.0, 0.4), DAL)
    return k.finish(325, dict(sale_h=0.5, mousse=0.05))


@piece
def SM_Chateau_GrandeEntree():
    """Grande porte d'entrée du château (mène à l'acte III) : façade de 20 × 22 m, portail en arc brisé de 9 × 13 m à
    voussures, deux battants de chêne ferré (Battant_G, Battant_D, pivots aux gonds) portant le sceau de lune de sortie
    (lot 3), perron de 5 marches, deux contreforts. Face vers -Z (extérieur côté jardin à l'acte III)."""
    k = K("SM_Chateau_GrandeEntree")
    # relecture du lot 3 : archivolte sur la face avant seulement, battants contre la face arrière (y = 1.5), gonds 1 cm
    # derrière : ils s'ouvrent vers l'arrière sans traverser les piédroits
    L.masonry_arch(k, 9.0, 8.0, 5.0, 3.0, 5.5, PIERRE, n=14, pointed=True, top=22.0, seed=327, arriere=False)
    yg = 1.51
    yc = yg + 0.2
    for sx in (-1, 1):
        k.box((sx * 9.0, -1.8, 9.0), (2.2, 2.0, 18.0), PIERRE, taper=0.15)
        k.box((sx * 9.0, -1.8, 18.5), (1.6, 1.4, 1.2), PIERRE, taper=0.5)
    for i in range(5):
        k.box((0, -2.0 - i * 0.6, 0.18 * (5 - i) / 2 - 0.0), (13 + i * 1.2, 0.6, 0.18 * (5 - i)), DAL)
    for j in range(3):
        L.masonry_arch(k, 9.0 + 0.9 * (j + 1), 8.0, 5.0 + 0.5 * (j + 1), 0.4, 0.45, PIERRE, n=14, pointed=True, top=8.0 + 5.0 + 0.5 * (j + 1) + 0.6, voussoir=False, seed=328 + j)
    ob = k.finish(327, dict(sale_h=3.0, mousse=0.25))
    for side, sx in (("G", -1), ("D", 1)):
        kb = K(f"Battant_{side}")
        for i in range(9):
            x = sx * (i + 0.5) * 0.5
            prof_top = 8.0 + 5.0 * max(0.0, 1 - (abs(x) / 4.5) ** 1.7) ** 0.5
            kb.box((x, yc, prof_top / 2), (0.48, 0.4, prof_top), BOIS)
        for z in (1.5, 3.4):
            kb.box((sx * 2.25, yc - 0.24, z), (4.3, 0.08, 0.3), FER)
        kb.cyl((sx * 3.6, yc - 0.24, 4.0), 0.35, 0.08, FER, n=12, axis="y")
        # nuit 4, lot 3 : sceau de sortie d'acte (anneau partagé, croissant, étoile, rayons)
        H.sceau_lune(kb, sx, 0.0, 6.4, 2.0, yc - 0.24, FER, FER)
        b = kb.finish(329 + sx, dict(sale_h=1.5, mousse=0.1))
        for v in b.data.vertices:
            v.co.x -= sx * 4.5
            v.co.y -= yg
        b.parent = ob
        b.location = (sx * 4.5, yg, 0)
    return ob


# ------------------------------------------------------------------ lumière et tentures
@piece
def SM_Chateau_Chandelier():
    """Chandelier suspendu : couronne de fer Ø 3 m à 12 bougies, pendu par une chaîne de 6 m (pivot au point
    d'accroche du plafond). Flammes Feu_1 … Feu_12."""
    k = K("SM_Chateau_Chandelier")
    for i in range(12):
        z0 = -i * 0.5
        if i % 2 == 0:
            k.obox((0, 0, z0), (0, 0, z0 - 0.5), 0.07, 0.025, FER)
        else:
            k.obox((0, 0, z0), (0, 0, z0 - 0.5), 0.025, 0.07, FER)
    k.lathe((0, 0, -6.4), [(1.5, 0), (1.55, 0.05), (1.5, 0.15), (1.42, 0.12)], FER, n=24)
    k.lathe((0, 0, -6.9), [(0.0, 0), (0.25, 0.1), (0.15, 0.5), (0.05, 0.55)], FER, n=10)
    for i in range(4):
        a = i * math.pi / 2
        k.obox((0, 0, -6.4), (math.cos(a) * 1.45, math.sin(a) * 1.45, -6.35), 0.06, 0.06, FER)
        k.obox((0, 0, -5.9), (math.cos(a) * 1.45, math.sin(a) * 1.45, -6.35), 0.04, 0.04, FER)
    for i in range(12):
        a = i * 2 * math.pi / 12
        x, y = math.cos(a) * 1.5, math.sin(a) * 1.5
        k.cyl((x, y, -6.27), 0.07, 0.03, FER, n=8)
        k.cyl((x, y, -6.24), 0.035, 0.22, CIRE, n=8)
        k.empty(f"Feu_{i + 1}", (x, y, -5.97))
    return k.finish(331, dict(sale_h=0.0, mousse=0.0))


@piece
def SM_Chateau_Tenture():
    """Tenture lourde de 3 × 9 m sur tringle (pivot à la tringle, contre un mur), plis profonds, bas effrangé et déchiré."""
    k = K("SM_Chateau_Tenture")
    nu, nv = 24, 18
    def P(i, j):
        x = -1.5 + 3.0 * i / nu
        z = -9.0 * j / nv
        y = -0.25 - 0.22 * math.sin(i / nu * math.pi * 7) * (0.4 + 0.6 * j / nv)
        if j == nv:
            z += 0.6 * abs(math.sin(i * 1.9)) + (0.9 if 15 <= i <= 18 else 0)
        return Vector((x, y, z))
    k.grid(P, nu, nv, TENT)
    k.cyl((-1.8, -0.25, 0.1), 0.06, 3.6, FER, n=8, axis="x")
    return k.finish(333, dict(sale_h=0.0, mousse=0.0))


# ------------------------------------------------------------------ mobilier
def livres(k, x0, x1, y, z, prof, h, rnd):
    x = x0
    while x < x1 - 0.06:
        w = rnd.uniform(0.04, 0.1)
        hh = h * rnd.uniform(0.7, 0.98)
        tilt = rnd.uniform(-0.15, 0.15) if rnd.random() < 0.12 else 0.0
        m = rnd.choice([TENT, TOILE, BOISP, TAPIS, PEINT])
        k.box((x + w / 2, y, z + hh / 2), (w, prof * rnd.uniform(0.8, 1.0), hh), m, rot=tilt)
        x += w + (rnd.uniform(0.05, 0.4) if rnd.random() < 0.06 else 0.005)


@piece
def SM_Chateau_Bibliotheque():
    """Bibliothèque murale de 4 × 0.7 × 5 m (dos au mur, face vers +Z) : 7 rayons de livres serrés, quelques vides et
    livres penchés ; corniche. Se monte en rangée continue ; escalade possible en bout (wall jump)."""
    k = K("SM_Chateau_Bibliotheque")
    rnd = random.Random(341)
    k.box((0, -0.05, 2.5), (4.0, 0.1, 5.0), BOIS)
    for sx in (-1, 1):
        k.box((sx * 1.97, -0.35, 2.5), (0.08, 0.7, 5.0), BOIS)
    for j in range(8):
        z = 0.1 + j * 0.68
        k.box((0, -0.35, z), (3.9, 0.7, 0.06), BOIS)
        if j < 7:
            livres(k, -1.9, 1.9, -0.38, z + 0.03, 0.55, 0.55, rnd)
    corniche(k, -2.1, 2.1, 5.0, 0.4, y=-0.6, m=BOIS)
    return k.finish(341, dict(sale_h=0.4, mousse=0.0))


@piece
def SM_Chateau_Tableau():
    """Grand tableau sombre de 2 × 2.8 m dans un cadre doré éteint (accroché au mur, pivot au clou, face vers +Z)."""
    k = K("SM_Chateau_Tableau")
    k.box((0, -0.04, -1.4), (1.8, 0.04, 2.6), PEINT)
    for (cx, cz, sx, sz) in ((0, -0.05, 2.1, 0.2), (0, -2.75, 2.1, 0.2), (-0.98, -1.4, 0.2, 2.9), (0.98, -1.4, 0.2, 2.9)):
        k.box((cx, -0.08, cz), (sx, 0.14, sz), TRIM)
    return k.finish(343, dict(sale_h=0.0, mousse=0.0))


def chaise(k, c, rot=0.0, renverse=False, m=BOIS):
    """Chaise à haut dossier ; renversée : couchée sur le dos."""
    from mathutils import Matrix
    parts = [((0, 0, 0.47), (0.5, 0.5, 0.06)), ((0, 0.23, 1.0), (0.5, 0.05, 1.05))]
    for x in (-0.22, 0.22):
        for y in (-0.22, 0.22):
            parts.append(((x, y, 0.23), (0.05, 0.05, 0.46)))
    for (p, s) in parts:
        p = Vector(p)
        if renverse:
            p = Vector((p.x, p.z * -1 + 0.0, p.y + 0.27))
            s = (s[0], s[2], s[1])
        pr = Vector((p.x * math.cos(rot) - p.y * math.sin(rot), p.x * math.sin(rot) + p.y * math.cos(rot), p.z))
        k.box((c[0] + pr.x, c[1] + pr.y, c[2] + pr.z), s, m, rot=rot)


@piece
def SM_Chateau_Chaise_Renversee():
    """Chaise à haut dossier couchée au sol (meuble renversé)."""
    k = K("SM_Chateau_Chaise_Renversee")
    chaise(k, (0, 0, 0), 0.3, True)
    return k.finish(351, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Chateau_Chaise():
    k = K("SM_Chateau_Chaise")
    chaise(k, (0, 0, 0))
    return k.finish(353, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Chateau_Table_Renversee():
    """Longue table de banquet (4 × 1.2 m) renversée sur le flanc : abri bas, couvert de fortune."""
    k = K("SM_Chateau_Table_Renversee")
    k.box((0, 0, 0.6), (4.0, 0.1, 1.2), BOIS)
    for x in (-1.7, 1.7):
        for zz in (0.15, 1.05):
            k.box((x, -0.4, zz), (0.12, 0.75, 0.12), BOIS)
    k.box((0, -0.4, 0.6), (3.4, 0.1, 0.1), BOIS)
    return k.finish(355, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Chateau_Armoire_Tombee():
    """Armoire de 2 × 0.7 × 2.6 m tombée en avant, une porte arrachée à côté (gros couvert, saut)."""
    k = K("SM_Chateau_Armoire_Tombee")
    k.box((0, 0, 0.35), (2.0, 2.6, 0.7), BOIS, rot=0.12)
    k.box((1.6, 0.9, 0.04), (0.9, 2.3, 0.08), BOIS, rot=0.7)
    for i in range(4):
        k.box((-0.6 + i * 0.4, -1.5 - i * 0.1, 0.03), (0.3, 0.2, 0.05), TOILE, rot=i)
    return k.finish(357, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Chateau_TableBanquet():
    """Table de banquet de 6 × 1.4 m (dessus à 0.85 m), nappe déchirée, chandeliers éteints, assiettes."""
    k = K("SM_Chateau_TableBanquet")
    k.box((0, 0, 0.82), (6, 1.4, 0.08), BOIS)
    for x in (-2.6, 0, 2.6):
        k.lathe((x, 0, 0), [(0.35, 0), (0.15, 0.2), (0.12, 0.5), (0.2, 0.78)], BOIS, n=10)
    k.box((-0.5, 0, 0.865), (4.2, 1.5, 0.01), TOILE)
    for x in (-2.0, 0.5, 2.2):
        k.lathe((x, 0, 0.87), [(0.08, 0), (0.05, 0.05), (0.02, 0.3), (0.07, 0.35)], FER, n=8)
        k.cyl((x, 0, 1.22), 0.025, 0.12, CIRE, n=8)
    for x in (-2.5, -1.3, -0.1, 1.1):
        for y in (-0.45, 0.45):
            k.cyl((x, y, 0.87), 0.14, 0.02, "M_Kit_Os", n=12)
    return k.finish(359, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Chateau_Fauteuil():
    """Fauteuil de velours à oreilles (salon, chambre)."""
    k = K("SM_Chateau_Fauteuil")
    k.box((0, 0, 0.25), (0.9, 0.85, 0.5), TENT)
    k.box((0, 0.36, 0.9), (0.9, 0.18, 1.3), TENT)
    for sx in (-1, 1):
        k.box((sx * 0.4, 0.0, 0.62), (0.15, 0.85, 0.28), TENT)
        k.box((sx * 0.42, 0.28, 1.4), (0.16, 0.3, 0.5), TENT)
    return k.finish(361, dict(sale_h=0.2, mousse=0.0))


@piece
def SM_Chateau_LitBaldaquin():
    """Lit à baldaquin (2.4 × 2.2 m, ciel à 3.2 m), rideaux déchirés tombant d'un côté (chambres)."""
    k = K("SM_Chateau_LitBaldaquin")
    k.box((0, 0, 0.45), (2.4, 2.2, 0.5), BOIS)
    k.box((0, 0.1, 0.78), (2.2, 1.9, 0.18), TOILE)
    k.box((0, 1.05, 1.1), (2.4, 0.12, 1.4), BOIS)
    for x in (-1.15, 1.15):
        for y in (-1.05, 1.05):
            k.cyl((x, y, 0), 0.08, 3.2, BOIS, n=8)
    k.box((0, 0, 3.25), (2.5, 2.3, 0.12), TENT)
    nu, nv = 10, 8
    def P(i, j):
        return Vector((-1.2 + 0.8 * i / nu, -1.12 - 0.1 * math.sin(i * 1.7), 3.2 - 3.0 * j / nv + (0.4 * abs(math.sin(i)) if j == nv else 0)))
    k.grid(P, nu, nv, TENT)
    return k.finish(363, dict(sale_h=0.2, mousse=0.0))


@piece
def SM_Chateau_Coffre():
    """Coffre de chambre en bois ferré (1.2 × 0.6 × 0.7 m), couvercle entrouvert (décor, pas le coffre du jeu)."""
    k = K("SM_Chateau_Coffre")
    k.box((0, 0, 0.3), (1.2, 0.6, 0.6), BOIS)
    k.box((0, 0.2, 0.75), (1.22, 0.62, 0.12), BOIS, rot=0.0)
    for x in (-0.45, 0.45):
        k.box((x, 0, 0.3), (0.08, 0.62, 0.62), FER)
    return k.finish(365, dict(sale_h=0.2, mousse=0.0))


@piece
def SM_Chateau_MiroirBrise():
    """Miroir sur pied (0.9 × 2 m), verre brisé (fragments sombres), cadre de bois sculpté."""
    k = K("SM_Chateau_MiroirBrise")
    k.box((0, 0, 1.2), (0.8, 0.04, 1.7), VITRAIL)
    for (cx, cz, sx, sz) in ((0, 2.08, 0.95, 0.12), (0, 0.32, 0.95, 0.12), (-0.45, 1.2, 0.1, 1.9), (0.45, 1.2, 0.1, 1.9)):
        k.box((cx, -0.03, cz), (sx, 0.08, sz), BOIS)
    for x in (-0.4, 0.4):
        k.box((x, 0.2, 0.15), (0.08, 0.6, 0.3), BOIS)
    for i in range(6):
        k.box((-0.2 + i * 0.09, -0.4 - (i % 3) * 0.1, 0.01), (0.12, 0.08, 0.01), VITRAIL, rot=i * 0.7)
    return k.finish(367, dict(sale_h=0.1, mousse=0.0))


# ------------------------------------------------------------------ chapelle
@piece
def SM_Chateau_Banc_Eglise():
    """Banc d'église de 4 m (dossier, prie-Dieu) : rangées de la chapelle, couvert bas."""
    k = K("SM_Chateau_Banc_Eglise")
    k.box((0, 0, 0.45), (4, 0.45, 0.06), BOIS)
    k.box((0, 0.22, 0.85), (4, 0.06, 0.8), BOIS)
    for x in (-1.97, 1.97):
        k.box((x, 0.0, 0.55), (0.08, 0.55, 1.1), BOIS)
    k.box((0, -0.45, 0.2), (3.9, 0.2, 0.08), BOIS)
    return k.finish(371, dict(sale_h=0.3, mousse=0.0))


@piece
def SM_Chateau_Autel():
    """Autel de pierre (3 × 1.4 × 1.1 m) sur deux marches, nappe, chandeliers ; Feu_1 et Feu_2 sur les cierges."""
    k = K("SM_Chateau_Autel")
    k.box((0, 0, 0.15), (5.0, 3.0, 0.3), MARBRE)
    k.box((0, 0.2, 0.45), (4.0, 2.2, 0.3), MARBRE)
    k.box((0, 0.3, 1.05), (3.0, 1.4, 0.9), PIERRE)
    k.box((0, 0.3, 1.52), (3.2, 1.6, 0.06), TOILE)
    for i, x in enumerate((-1.2, 1.2)):
        k.lathe((x, 0.5, 1.55), [(0.15, 0), (0.06, 0.1), (0.04, 0.6), (0.1, 0.65)], FER, n=10)
        k.cyl((x, 0.5, 2.2), 0.04, 0.35, CIRE, n=8)
        k.empty(f"Feu_{i + 1}", (x, 0.5, 2.6))
    return k.finish(373, dict(sale_h=0.4, mousse=0.0))


@piece
def SM_Chateau_Lutrin():
    """Lutrin de fer (1.4 m) avec un livre ouvert."""
    k = K("SM_Chateau_Lutrin")
    k.cyl((0, 0, 0), 0.25, 0.06, FER, n=10)
    k.cyl((0, 0, 0.06), 0.04, 1.2, FER, n=8)
    k.box((0, 0, 1.32), (0.6, 0.45, 0.04), FER, rot=0)
    k.box((0, -0.02, 1.37), (0.55, 0.4, 0.05), TOILE)
    return k.finish(375, dict(sale_h=0.2, mousse=0.0))


@piece
def SM_Chateau_Rosace():
    """Rosace de chapelle Ø 7 m (au mur, face vers +Z, pivot au centre) : remplage rayonnant, vitrail sombre."""
    k = K("SM_Chateau_Rosace")
    n = 16
    # construite à plat (plan XY, épaisseur en Z), puis redressée dans le plan XZ
    k.lathe((0, 0, 0), [(3.5, 0), (3.5, 0.6), (3.1, 0.6), (3.1, 0.0)], PIERRE, n=32)
    k.cyl((0, 0, 0.3), 3.12, 0.04, VITRAIL, n=32)
    for i in range(n):
        a_ = i * 2 * math.pi / n
        k.obox((math.cos(a_) * 0.5, math.sin(a_) * 0.5, 0.45), (math.cos(a_) * 3.1, math.sin(a_) * 3.1, 0.45), 0.18, 0.35, PIERRE)
    k.lathe((0, 0, 0.0), [(1.6, 0.25), (1.6, 0.65), (1.4, 0.65), (1.4, 0.25)], PIERRE, n=24)
    k.cyl((0, 0, 0.0), 0.6, 0.75, PIERRE, n=12)
    ob = k.finish(377, dict(sale_h=0.0, mousse=0.0))
    for v in ob.data.vertices:
        x, y, z = v.co
        v.co = Vector((x, -z, y))
    ob.data.update()
    return ob


# ------------------------------------------------------------------ salle de bal
@piece
def SM_Chateau_Estrade_Musiciens():
    """Estrade des musiciens de la salle de bal : 8 × 4 m, 1.2 m de haut, trois marches, pupitres renversés."""
    k = K("SM_Chateau_Estrade_Musiciens")
    k.box((0, 0, 0.6), (8, 4, 1.2), BOIS)
    for i in range(3):
        k.box((0, -2.3 - i * 0.35, 0.2 * (3 - i)), (3, 0.35, 0.4 * (3 - i)), BOIS)
    balustrade(k, (-4, 1.9, 1.2), (4, 1.9, 1.2), h=0.9, m=BOIS, pas=0.4)
    for x in (-2.5, 0.5, 2.5):
        k.cyl((x, 0.5, 1.2), 0.03, 1.1, FER, n=6)
        k.box((x, 0.5, 2.3), (0.5, 0.05, 0.35), FER)
    k.box((1.5, -0.8, 1.24), (0.6, 0.4, 0.05), FER, rot=1.2)
    return k.finish(381, dict(sale_h=0.3, mousse=0.0))


# nuit 4, lot 3 : hauteur, fenêtres, chandeliers (kit_hauteur.py)
H.enregistre(PIECES, H.CHATEAU, H.pieces_chateau(H.CHATEAU))


def premiere_phrase(doc):
    """Première phrase entière de la docstring (relecture du lot 3 : la première ligne seule tronquait les descriptions)."""
    d = " ".join(l.strip() for l in (doc or "").strip().split("\n"))
    i = d.find(". ")
    return d if i < 0 else d[:i + 1]


def main(noms=None):
    mesures = {}
    path = os.path.join(HERE, "kit_chateau.json")
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
