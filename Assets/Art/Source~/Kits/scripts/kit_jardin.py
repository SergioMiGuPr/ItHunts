# -*- coding: utf-8 -*-
"""Kit de l'acte III : jardin à l'abandon devant le château, sous la lune, et arène du boss en contrebas (lot 4, nuit du
2026-11-22, branche art-kit-jardin).

Gros arbres morts (troncs de 2 à 2.6 m de diamètre : wall jumps), haies et massifs, palissades et clôtures (à poser le
long des allées et des parcelles), murets, statues, fontaine, serre en ruine, kiosque, tombes, grilles, chemins de gravier ;
limites naturelles (forêt dense, mur d'enceinte, falaise : jamais de mur invisible) ; arène du boss (escalier de descente,
piliers brisés et statues comme points d'accroche, couverts, buttes). Le ciel, la lune et la brume au sol sont des prefabs
faits dans Unity (KitBuilder).

Lancer :  blender -b -P kit_jardin.py [-- SM_Jardin_… …]      Sorties : Assets/Art/Models/KitJardin/
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

OUT = os.path.join(L.ROOT, "Assets", "Art", "Models", "KitJardin")

ECORCE = "M_Bib_Ecorce"
PIERRE = "M_Bib_PierreTaillee_Peint"
MOEL = "M_Bib_Moellons_Peint"
ROCHE = "M_Bib_RocheCave_Peint"
GRAV = "M_Bib_Gravier_Herbe"
TERRE = "M_Bib_Terre_Racines"
HERBE = "M_Bib_HerbeSeche"
BOIS = "M_Bib_BoisPourri"
FER = "M_Bib_FerRouille"
MARBRE = "M_Bib_Mortier"   # pierre fine des statues (le marbre en damier est un sol)
FEUIL = "M_Kit_Feuillage"
VITRAIL = "M_Kit_Vitrail"
EAU = "M_Eau_Toon"
NOIR = "M_Fond_Noir"

PIECES = {}


def piece(f):
    PIECES[f.__name__] = f
    return f


def K(n):
    return L.K(n)


# ------------------------------------------------------------------ arbres morts
def branche(k, rnd, base, d, r0, longueur, niveau, seed):
    """Branche tordue récursive (diminue de rayon, se divise)."""
    pts, radii = [Vector(base)], [r0]
    p = Vector(base)
    dd = Vector(d).normalized()
    nseg = max(3, int(longueur / 1.2))
    for i in range(nseg):
        dd = (dd + Vector((rnd.uniform(-0.35, 0.35), rnd.uniform(-0.35, 0.35), rnd.uniform(-0.15, 0.3)))).normalized()
        p = p + dd * (longueur / nseg)
        pts.append(p.copy())
        radii.append(r0 * (1 - 0.85 * (i + 1) / nseg) + 0.02)
    L.tube(k, pts, radii, ECORCE, sides=max(5, 9 - niveau * 2), rough=0.18, seed=seed)
    if niveau < 3:
        for j in range(rnd.randint(1, 3)):
            i = rnd.randint(1, len(pts) - 2)
            nd = (dd + Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), rnd.uniform(0.0, 0.8)))).normalized()
            branche(k, rnd, pts[i], nd, radii[i] * 0.7, longueur * rnd.uniform(0.45, 0.7), niveau + 1, seed + j * 7 + niveau)


def arbre(nom, seed, h, r, n_branches=5, penche=0.0):
    rnd = random.Random(seed)
    k = K(nom)
    # tronc noueux, évasé au pied (racines), légèrement penché
    pts, radii = [], []
    for i in range(9):
        t = i / 8
        z = h * t
        x = penche * z * z / h + math.sin(t * 4 + seed) * 0.4
        pts.append((x, math.cos(t * 3 + seed) * 0.3, z))
        radii.append(r * (1.0 - 0.45 * t) * (1 + 0.6 * max(0, 0.12 - t) / 0.12))
    L.tube(k, pts, radii, ECORCE, sides=14, rough=0.22, seed=seed)
    # racines qui plongent dans la terre
    for j in range(6):
        a = j * 2 * math.pi / 6 + rnd.uniform(-0.3, 0.3)
        p0 = Vector((math.cos(a) * r * 0.6, math.sin(a) * r * 0.6, 0.9))
        p1 = Vector((math.cos(a) * r * 1.6, math.sin(a) * r * 1.6, 0.2))
        p2 = Vector((math.cos(a) * r * 2.6, math.sin(a) * r * 2.6, -0.5))
        L.tube(k, [p0, p1, p2], [r * 0.45, r * 0.3, r * 0.1], ECORCE, sides=7, rough=0.2, seed=seed + j)
    # grosses branches depuis le tiers supérieur
    for j in range(n_branches):
        i = rnd.randint(4, 7)
        base = Vector(pts[i])
        a = j * 2 * math.pi / n_branches + rnd.uniform(-0.4, 0.4)
        d = Vector((math.cos(a), math.sin(a), rnd.uniform(0.2, 0.9)))
        branche(k, rnd, base, d, radii[i] * 0.55, h * rnd.uniform(0.35, 0.55), 1, seed + j * 31)
    k.empty("Accroche_Traction", (pts[6][0], pts[6][1] - r, pts[6][2]))
    return k.finish(seed, dict(sale_h=1.2, mousse=0.5))


@piece
def SM_Jardin_ArbreMort_Grand():
    """Gros arbre mort de 18 m, tronc Ø 2.6 m (wall jump d'un tronc à l'autre), racines, branches noueuses."""
    return arbre("SM_Jardin_ArbreMort_Grand", 401, 18, 1.3, 6)


@piece
def SM_Jardin_ArbreMort_Penche():
    """Arbre mort penché de 14 m (Ø 2.2 m) : le tronc incliné sert de rampe de course et de mur de wall jump."""
    return arbre("SM_Jardin_ArbreMort_Penche", 403, 14, 1.1, 4, penche=0.45)


@piece
def SM_Jardin_ArbreMort_Tordu():
    """Arbre mort tordu de 11 m (Ø 2 m), couronne de branches basses (silhouette sur la lune)."""
    return arbre("SM_Jardin_ArbreMort_Tordu", 405, 11, 1.0, 7)


# ------------------------------------------------------------------ haies, massifs, forêt
def bloc_feuillage(k, c, s, seed, cuts=4, amp=0.12, amp_haut=0.3):
    """Masse de feuillage taillée puis laissée à l'abandon (relecture du lot 3 : plus de boules) : bloc à faces planes
    subdivisé, faces bosselées par un bruit (amp), dessus déchiqueté (amp_haut), base plate posée au sol (c = centre de
    la base). Lecture de haie ou de massif, pas de volume en boule."""
    import bmesh
    from mathutils import Vector
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=bm.edges[:], cuts=cuts, use_grid_fill=True)
    off = Vector((seed * 3.7, seed * 1.3, seed * 5.9))
    sx, sy, sz = s
    cx, cy, cz = c
    for v in bm.verts:
        u = v.co.copy()
        p = Vector((u.x * sx, u.y * sy, (u.z + 0.5) * sz))
        if u.z > -0.49:
            n = Vector((u.x * 2 if abs(u.x) > 0.49 else 0, u.y * 2 if abs(u.y) > 0.49 else 0, 1.0 if u.z > 0.49 else 0)).normalized()
            d = L.fbm3((p + off) * 0.9, 3)
            a_ = amp_haut if u.z > 0.49 else amp * (0.3 + 0.7 * (u.z + 0.5))
            p += n * a_ * d
            if u.z > 0.49:
                p.z += amp_haut * 0.6 * L.fbm3((p + off) * 2.1, 2)
        v.co = p + Vector((cx, cy, cz))
    for f in bm.faces:
        if all(v.co.z <= cz + 1e-6 for v in f.verts):
            continue          # base posée au sol : face cachée
        k.face([tuple(v.co) for v in f.verts], FEUIL)
    bm.free()


@piece
def SM_Jardin_Haie_6m():
    """Haie taillée devenue sauvage : 6 × 1.4 m, 2.6 m de haut, faces bosselées, dessus déchiqueté (à poser le long d'une
    allée). Relecture du lot 3 : un bloc de feuillage bosselé au lieu des boules."""
    k = K("SM_Jardin_Haie_6m")
    bloc_feuillage(k, (0, 0, 0), (5.8, 1.3, 2.3), 411, cuts=5, amp=0.12, amp_haut=0.35)
    return k.finish(411, dict(sale_h=0.8, mousse=0.0))


@piece
def SM_Jardin_Massif():
    """Massif d'arbustes morts (Ø 3 m) au coin d'une parcelle : masse basse de feuillage sombre bosselée et tiges sèches.
    Relecture du lot 3 : une masse bosselée au lieu des boules."""
    rnd = random.Random(413)
    k = K("SM_Jardin_Massif")
    bloc_feuillage(k, (0, 0, 0), (2.6, 2.3, 1.2), 413, cuts=3, amp=0.18, amp_haut=0.3)
    for i in range(8):
        a = rnd.uniform(0, 6.28)
        L.tube(k, [(0, 0, 0.5), (math.cos(a) * 1.2, math.sin(a) * 1.2, 2.0 + rnd.uniform(0, 0.8))], [0.05, 0.015], ECORCE, sides=5)
    return k.finish(413, dict(sale_h=0.5, mousse=0.0))


@piece
def SM_Jardin_Foret_Lisiere_20m():
    """Lisière de forêt dense (20 m de front, 10 m de profondeur) : troncs serrés devant deux rideaux de feuillage sombre
    (5 et 9 m, crêtes déchiquetées) et un roncier continu. Limite naturelle du jardin (infranchissable, lisible), à poser
    en rangée. Relecture du lot 3 : rideaux de feuillage au lieu des boules."""
    rnd = random.Random(415)
    k = K("SM_Jardin_Foret_Lisiere_20m")
    for i in range(16):
        x = -10 + rnd.uniform(0, 20)
        y = rnd.uniform(0, 10)
        h = rnd.uniform(14, 22)
        r = rnd.uniform(0.3, 0.8)
        L.tube(k, [(x, y, -0.5), (x + rnd.uniform(-0.6, 0.6), y, h * 0.5), (x + rnd.uniform(-1, 1), y + rnd.uniform(-0.5, 0.5), h)], [r, r * 0.7, 0.1], ECORCE, sides=6, rough=0.15, seed=i)
    L.rock_wall(k, [(-10, 2.4), (-3, 1.9), (4, 2.6), (10, 2.2)], 5.0, FEUIL, thick=1.2, seg=1.2, amp=0.45, seed=430, inward=-1,
                base_flare=0.3, top_jag=2.2)
    L.rock_wall(k, [(-10, 6.5), (0, 6.0), (10, 6.8)], 9.0, FEUIL, thick=1.5, seg=1.4, amp=0.6, seed=431, inward=-1,
                base_flare=0.0, top_jag=3.0)
    # ronces au pied (barrière basse continue)
    bloc_feuillage(k, (0, 1.0, 0), (20, 1.6, 1.8), 415, cuts=6, amp=0.1, amp_haut=0.3)
    return k.finish(415, dict(sale_h=1.0, mousse=0.0))


# ------------------------------------------------------------------ clôtures, murets, grilles
@piece
def SM_Jardin_Palissade_4m():
    """Palissade de jardin en planches pointues, 4 m × 1.6 m, deux planches manquantes, une penchée (le long d'une allée
    ou d'une parcelle, jamais seule au milieu de rien)."""
    rnd = random.Random(421)
    k = K("SM_Jardin_Palissade_4m")
    for z in (0.35, 1.15):
        k.box((0, 0.06, z), (4, 0.08, 0.12), BOIS)
    for x in (-1.95, 1.95):
        k.box((x, 0.12, 0.7), (0.14, 0.14, 1.6), BOIS)
    for i in range(16):
        if i in (5, 11):
            continue
        x = -1.875 + i * 0.25
        h = 1.6 + rnd.uniform(-0.15, 0.1)
        rot = 0.25 if i == 8 else 0
        k.box((x, 0, h / 2 - 0.05), (0.2, 0.04, h - 0.1), BOIS, rot=0)
        k.face([(x - 0.1, -0.02, h - 0.1), (x + 0.1, -0.02, h - 0.1), (x, -0.02, h + 0.12)], BOIS)
    return k.finish(421, dict(sale_h=0.6, mousse=0.2))


@piece
def SM_Jardin_Grille_4m():
    """Grille d'enceinte en fer forgé, 4 m × 3.5 m, barreaux à pointes de lance, volutes, entre deux piliers de pierre."""
    k = K("SM_Jardin_Grille_4m")
    for sx in (-1, 1):
        k.box((sx * 2.2, 0, 1.9), (0.6, 0.6, 3.8), PIERRE)
        k.lathe((sx * 2.2, 0, 3.8), [(0.4, 0), (0.42, 0.15), (0.0, 0.5)], PIERRE, n=8)
    for z in (0.3, 2.9):
        k.box((0, 0, z), (3.8, 0.06, 0.08), FER)
    for i in range(19):
        x = -1.8 + i * 0.2
        k.box((x, 0, 1.7), (0.035, 0.035, 3.4), FER)
        k.face([(x - 0.06, 0, 3.4), (x + 0.06, 0, 3.4), (x, 0, 3.62)], FER)
    for i in range(9):
        x = -1.6 + i * 0.4
        L.tube(k, [(x - 0.2, 0, 2.95), (x - 0.1, 0, 3.15), (x + 0.1, 0, 3.15), (x + 0.2, 0, 2.95)], [0.02] * 4, FER, sides=4, cap=False)
    return k.finish(423, dict(sale_h=0.8, mousse=0.2))


@piece
def SM_Jardin_Portail():
    """Portail de fer à deux vantaux (4 m) entre deux grands piliers à vases, un vantail entrouvert : entrée d'allée."""
    k = K("SM_Jardin_Portail")
    for sx in (-1, 1):
        k.box((sx * 2.6, 0, 2.3), (0.9, 0.9, 4.6), PIERRE)
        k.lathe((sx * 2.6, 0, 4.6), [(0.3, 0), (0.45, 0.3), (0.3, 0.6), (0.35, 0.8)], PIERRE, n=10)
    for side, sx, ang in (("G", -1, 0.0), ("D", 1, 0.6)):
        for i in range(10):
            u = (i + 0.5) / 10 * 2.1
            x = sx * 2.15 - sx * u * math.cos(ang)
            y = -u * math.sin(ang) * sx * 0 - u * math.sin(ang)
            hh = 3.2 + 0.4 * math.sin(math.pi * u / 2.1)
            k.box((x, y, hh / 2), (0.035, 0.035, hh), FER)
        for z in (0.3, 3.0):
            a = Vector((sx * 2.15, 0, z))
            b = Vector((sx * 2.15 - sx * 2.1 * math.cos(ang), -2.1 * math.sin(ang), z))
            k.obox(a, b, 0.07, 0.07, FER)
    return k.finish(425, dict(sale_h=0.8, mousse=0.3))


@piece
def SM_Jardin_Muret_4m():
    """Muret de pierre de 4 m × 0.6 m, 1 m de haut, chaperon arrondi, pierres manquantes (couvert bas, rebord de parcelle)."""
    rnd = random.Random(427)
    k = K("SM_Jardin_Muret_4m")
    k.box((0, 0, 0.45), (4, 0.6, 0.9), MOEL)
    for i in range(8):
        if rnd.random() < 0.2:
            continue
        k.box((-1.75 + i * 0.5, 0, 0.97), (0.48, 0.7, 0.14), PIERRE, rot=rnd.uniform(-0.04, 0.04))
    return k.finish(427, dict(sale_h=0.6, mousse=0.6))


@piece
def SM_Jardin_MurEnceinte_8m():
    """Mur d'enceinte du domaine : 8 m de long, 9 m de haut, contreforts, chaperon : limite évidente (on ne grimpe pas :
    pas de prise, crête en surplomb)."""
    k = K("SM_Jardin_MurEnceinte_8m")
    k.box((0, 0.5, 4.5), (8, 1.2, 9), MOEL)
    k.box((0, 0.2, 9.15), (8.2, 1.8, 0.3), PIERRE)
    for x in (-3.5, 3.5):
        k.box((x, -0.6, 3.5), (1.2, 1.4, 7), MOEL, taper=0.3)
    return k.finish(429, dict(sale_h=2.5, mousse=0.7))


@piece
def SM_Jardin_Falaise_16m():
    """Falaise de 16 m de front, 24 m de haut, herbe sèche au sommet : limite naturelle (arène en contrebas, bord du jardin)."""
    k = K("SM_Jardin_Falaise_16m")
    L.rock_wall(k, [(-8, 0), (-3, -1.2), (2, 0.6), (8, 0)], 24, ROCHE, thick=3, seg=1.2, amp=1.3, seed=431, inward=-1)
    k.face([(-8, 3.0, 24.3), (8, 3.0, 24.3), (8, 0.5, 24.3), (-8, 0.5, 24.3)], HERBE)
    return k.finish(431, dict(sale_h=4, mousse=0.6))


@piece
def SM_Jardin_Chemin_8m():
    """Allée de gravier de 8 × 3 m, bordures de pierre, herbe qui gagne les bords (pivot au centre, dessus à 0.05)."""
    k = K("SM_Jardin_Chemin_8m")
    k.box((0, 0, 0.0), (8, 3, 0.1), GRAV)
    for sy in (-1, 1):
        for i in range(8):
            k.box((-3.5 + i, sy * 1.55, 0.06), (0.95, 0.18, 0.22), PIERRE, rot=0.02 * (i % 3 - 1))
    return k.finish(433, dict(sale_h=0.2, mousse=0.6))


# ------------------------------------------------------------------ statues, fontaine, tombes
def statue(nom, seed, h, bras=True):
    """Statue de figure drapée (sans visage), sur piédestal : repère, point de traction."""
    rnd = random.Random(seed)
    k = K(nom)
    k.box((0, 0, 0.6), (1.6, 1.6, 1.2), PIERRE)
    k.box((0, 0, 1.3), (1.3, 1.3, 0.2), PIERRE)
    s = h / 4.0
    k.lathe((0, 0, 1.4), [(0.62 * s, 0), (0.58 * s, 0.8 * s), (0.42 * s, 1.9 * s), (0.36 * s, 2.5 * s), (0.42 * s, 2.75 * s), (0.2 * s, 2.95 * s), (0.0, 3.0 * s)], MARBRE, n=14)
    L.rock_blob(k, (0, 0.05 * s, 1.4 + 3.0 * s), (0.42 * s, 0.46 * s, 0.6 * s), MARBRE, seed=seed, res=10, amp=0.12, flat_base=False)
    L.rock_blob(k, (0, 0.1 * s, 1.4 + 3.25 * s), (0.6 * s, 0.6 * s, 0.4 * s), MARBRE, seed=seed + 1, res=10, amp=0.25, flat_base=False)
    if bras:
        for sx in (-1, 1):
            L.tube(k, [(sx * 0.35 * s, 0, 1.4 + 2.6 * s), (sx * 0.5 * s, -0.25 * s, 1.4 + 2.2 * s), (sx * 0.2 * s, -0.4 * s, 1.4 + 2.75 * s)],
                   [0.13 * s, 0.11 * s, 0.09 * s], MARBRE, sides=7)
    k.empty("Accroche_Traction", (0, -0.6 * s, 1.4 + 2.5 * s))
    return k.finish(seed, dict(sale_h=1.6, mousse=0.8))


@piece
def SM_Jardin_Statue_Pleureuse():
    """Statue de 5.4 m (figure voilée qui se cache le visage) sur piédestal : repère du jardin, point de traction."""
    return statue("SM_Jardin_Statue_Pleureuse", 441, 4.0)


@piece
def SM_Jardin_Statue_Geante():
    """Statue géante de 12 m dans l'arène : couvert, point de traction haut (`Accroche_Traction`)."""
    return statue("SM_Jardin_Statue_Geante", 443, 10.5)


@piece
def SM_Jardin_Fontaine():
    """Fontaine (Ø 8 m) : bassin octogonal avec eau croupie, vasque à 2.5 m sur colonne, vasque haute, statuette."""
    k = K("SM_Jardin_Fontaine")
    k.lathe((0, 0, 0), [(4.0, 0.0), (4.0, 0.7), (3.7, 0.8), (3.5, 0.8), (3.5, -0.4)], PIERRE, n=8)
    n = 8
    k.face([(3.5 * math.cos(2 * math.pi * i / n), 3.5 * math.sin(2 * math.pi * i / n), 0.45) for i in range(n)], EAU)
    k.lathe((0, 0, 0.45), [(0.6, 0), (0.4, 0.5), (0.35, 1.8), (0.5, 2.0)], PIERRE, n=12)
    k.lathe((0, 0, 2.45), [(0.4, 0), (1.6, 0.35), (1.7, 0.55), (1.5, 0.5), (0.4, 0.15)], PIERRE, n=16)
    k.lathe((0, 0, 3.0), [(0.25, 0), (0.2, 1.0), (0.8, 1.3), (0.85, 1.45), (0.2, 1.3)], PIERRE, n=12)
    L.rock_blob(k, (0, 0, 4.4), (0.5, 0.5, 1.2), MARBRE, seed=445, res=8, amp=0.2)
    return k.finish(445, dict(sale_h=1.2, mousse=0.9))


@piece
def SM_Jardin_Tombe_Stele():
    """Stèle penchée (0.9 × 1.6 m) sur un tertre, fleurs mortes : à aligner en rangées tordues dans le cimetière."""
    k = K("SM_Jardin_Tombe_Stele")
    k.box((0, 0.1, 0.8), (0.9, 0.2, 1.6), PIERRE, rot=0.0)
    k.lathe((0, 0.1, 1.6), [(0.45, 0), (0.35, 0.25), (0.0, 0.35)], PIERRE, n=10)
    L.rock_blob(k, (0, -1.0, -0.2), (1.1, 2.0, 0.5), TERRE, seed=447, res=10, amp=0.15)
    ob = k.finish(447, dict(sale_h=0.8, mousse=0.7))
    for v in ob.data.vertices:
        if v.co.z > 0.1:
            v.co.y -= v.co.z * 0.12
            v.co.x += v.co.z * 0.05
    return ob


@piece
def SM_Jardin_Tombe_Croix():
    """Croix de pierre (2.2 m) ébréchée sur socle, tertre."""
    k = K("SM_Jardin_Tombe_Croix")
    k.box((0, 0, 0.25), (0.9, 0.6, 0.5), PIERRE)
    k.box((0, 0, 1.35), (0.25, 0.25, 1.7), PIERRE)
    k.box((0, 0, 1.75), (1.0, 0.25, 0.25), PIERRE)
    L.rock_blob(k, (0, -1.0, -0.2), (1.0, 1.9, 0.45), TERRE, seed=449, res=10, amp=0.15)
    return k.finish(449, dict(sale_h=0.8, mousse=0.7))


@piece
def SM_Jardin_Caveau():
    """Caveau familial (5 × 6 × 5 m) : fronton, porte de fer entrouverte sur le noir, colonnes ; couvert, toit escaladable."""
    k = K("SM_Jardin_Caveau")
    k.box((0, 0.5, 2.0), (5, 5, 4), PIERRE)
    for sx in (-1, 1):
        k.cyl((sx * 1.9, -2.4, 0), 0.25, 4.0, PIERRE, n=10)
    k.box((0, -1.0, 4.2), (5.6, 3.2, 0.4), PIERRE)
    k.face([(-2.8, -2.6, 4.4), (2.8, -2.6, 4.4), (0, -2.6, 5.6)], PIERRE)
    k.face([(-2.8, 3.0, 4.4), (0, 3.0, 5.6), (2.8, 3.0, 4.4)], PIERRE)
    k.face([(-2.8, -2.6, 4.4), (0, -2.6, 5.6), (0, 3.0, 5.6), (-2.8, 3.0, 4.4)], PIERRE)
    k.face([(2.8, -2.6, 4.4), (2.8, 3.0, 4.4), (0, 3.0, 5.6), (0, -2.6, 5.6)], PIERRE)
    k.box((0, -2.01, 1.3), (1.6, 0.05, 2.6), NOIR)
    k.box((-0.4, -2.3, 1.3), (0.8, 0.05, 2.6), FER, rot=0.6)
    for i in range(3):
        k.box((0, -2.8 - i * 0.35, 0.1 - i * 0.0), (2.4, 0.35, 0.2), PIERRE)
    return k.finish(451, dict(sale_h=1.5, mousse=0.8))


# ------------------------------------------------------------------ fabriques : serre, kiosque
@piece
def SM_Jardin_Serre_Ruine():
    """Serre en ruine (10 × 6 m, 5 m) : soubassement de briques, ossature de fer, vitres brisées (quelques carreaux
    restent), toit à deux pans effondré d'un côté. On la traverse ; ses montants sont des prises de traction."""
    rnd = random.Random(461)
    k = K("SM_Jardin_Serre_Ruine")
    for sy in (-3, 3):
        k.box((0, sy, 0.4), (10, 0.4, 0.8), "M_Bib_Briques_Peint")
    for sx in (-5, 5):
        k.box((sx, 0, 0.4), (0.4, 6, 0.8), "M_Bib_Briques_Peint")
    for i in range(6):
        x = -5 + i * 2
        for sy in (-1, 1):
            k.box((x, sy * 3, 2.3), (0.1, 0.1, 3.0), FER)
            if not (sy == 1 and i in (3, 4)):
                k.obox((x, sy * 3, 3.8), (x, 0, 5.2), 0.1, 0.1, FER)
    for sy in (-1, 1):
        k.obox((-5, sy * 3, 3.8), (5, sy * 3, 3.8), 0.1, 0.1, FER)
    k.obox((-5, 0, 5.2), (1, 0, 5.2), 0.12, 0.12, FER)
    k.obox((1, 0, 5.2), (4.5, 1.5, 0.9), 0.12, 0.12, FER)
    for i in range(14):
        x = -4.5 + rnd.uniform(0, 9)
        sy = rnd.choice([-1, 1])
        z = rnd.uniform(1.0, 3.5)
        w, h = rnd.uniform(0.5, 1.4), rnd.uniform(0.4, 1.0)
        k.face([(x, sy * 3.02, z), (x + w, sy * 3.02, z), (x + w * 0.7, sy * 3.02, z + h), (x, sy * 3.02, z + h * 0.6)], VITRAIL)
    for i in range(10):
        k.box((rnd.uniform(-4.5, 4.5), rnd.uniform(-2.5, 2.5), 0.03), (rnd.uniform(0.2, 0.6), rnd.uniform(0.2, 0.5), 0.02), VITRAIL, rot=rnd.uniform(0, 3))
    return k.finish(461, dict(sale_h=0.8, mousse=0.6))


@piece
def SM_Jardin_Kiosque():
    """Kiosque octogonal (Ø 8 m) : plancher surélevé de 1 m, 8 colonnes de fer, toit en pavillon percé, rambarde
    (couvert circulaire, toit atteignable par traction)."""
    k = K("SM_Jardin_Kiosque")
    n = 8
    k.lathe((0, 0, 0), [(4.2, 0), (4.2, 1.0), (0.0, 1.0)], PIERRE, n=n)
    for i in range(3):
        k.box((0, -4.3 - i * 0.4, 0.17 * (3 - i)), (2.4, 0.4, 0.34 * (3 - i)), PIERRE)
    for i in range(n):
        a = 2 * math.pi * (i + 0.5) / n
        x, y = math.cos(a) * 3.8, math.sin(a) * 3.8
        k.cyl((x, y, 1.0), 0.12, 4.2, FER, n=6)
        a2 = 2 * math.pi * (i + 1.5) / n
        if i != 5:
            k.obox((x, y, 1.9), (math.cos(a2) * 3.8, math.sin(a2) * 3.8, 1.9), 0.06, 0.06, FER)
    prof = [(4.6, 5.2), (3.0, 6.4), (1.0, 7.5), (0.0, 8.0)]
    k.lathe((0, 0, 0), prof, FER, n=n)
    k.lathe((0, 0, 8.0), [(0.12, 0), (0.0, 1.2)], FER, n=6)
    k.empty("Accroche_Traction", (0, -4.6, 5.2))
    return k.finish(463, dict(sale_h=0.8, mousse=0.5))


# ------------------------------------------------------------------ arène du boss
@piece
def SM_Arene_EscalierDescente():
    """Grand escalier de descente vers l'arène : 12 m de large, descend de 8 m (32 marches), murs d'échiffre en ruine.
    Pivot en haut, au centre ; descend vers +Z."""
    k = K("SM_Arene_EscalierDescente")
    for i in range(32):
        z = -(i + 1) * 0.25
        y = -i * 0.5
        k.box((0, y - 0.25, (z - 8.5) / 2), (12, 0.5, z + 8.5), PIERRE)   # marche pleine du dessous (-8.5 m) jusqu'à sa hauteur
    for sx in (-1, 1):
        for j in range(8):
            hh = 2.0 + (1.5 if j % 3 else 0)
            k.box((sx * 6.4, -j * 2 - 1, -j - 0.0 + hh / 2 - 1), (0.8, 2.0, hh), MOEL)
    return k.finish(471, dict(sale_h=1.5, mousse=0.6))


@piece
def SM_Arene_PilierBrise():
    """Pilier brisé de 10 m (Ø 2 m), sommet cassé en biais, tambours tombés : point d'accroche de traction de l'arène."""
    k = K("SM_Arene_PilierBrise")
    k.box((0, 0, 0.5), (2.6, 2.6, 1.0), PIERRE)
    k.cyl((0, 0, 1.0), 1.0, 8.0, PIERRE, n=12)
    k.face([(-1.0, -1.0, 9.6), (1.0, -1.0, 8.6), (1.0, 1.0, 8.9), (-1.0, 1.0, 9.9)], PIERRE)
    for i, (x, y) in enumerate(((2.2, 1.0), (-1.8, -2.4))):
        k.cyl((x, y, 1.0), 1.0, 1.2, PIERRE, n=12, axis="x")
    k.empty("Accroche_Traction", (0, -1.1, 8.0))
    return k.finish(473, dict(sale_h=1.5, mousse=0.7))


@piece
def SM_Arene_Couvert():
    """Pan de mur effondré (6 × 1.2 m, 3.5 m) : couvert contre les attaques du boss (pas contre le traqueur)."""
    k = K("SM_Arene_Couvert")
    rnd = random.Random(475)
    for i in range(6):
        h = 3.5 * (0.5 + 0.5 * abs(math.sin(i * 1.3)))
        k.box((-2.5 + i, 0, h / 2), (1.0, 1.2, h), MOEL)
    for j in range(5):
        L.rock_blob(k, (rnd.uniform(-3, 3), rnd.uniform(-2.5, -1), 0), (1.0, 0.8, 0.6), MOEL, seed=476 + j, res=8)
    return k.finish(475, dict(sale_h=1.2, mousse=0.6))


@piece
def SM_Arene_Butte():
    """Butte de terre et de roche (14 × 10 m, 3 m de haut) : relief de l'arène, pentes de glissade, vue dégagée."""
    k = K("SM_Arene_Butte")
    L.rock_blob(k, (0, 0, -2.2), (14, 10, 7.5), TERRE, seed=477, res=16, amp=0.2)
    for j in range(5):
        L.rock_blob(k, (random.Random(j).uniform(-4, 4), random.Random(j + 9).uniform(-3, 3), 1.8), (1.8, 1.4, 1.2), ROCHE, seed=478 + j, res=8)
    return k.finish(477, dict(sale_h=1.0, mousse=0.6))


# nuit 4, lot 3 : hauteur, falaises, terrasses, ponts, enceinte (kit_hauteur.py)
H.enregistre(PIECES, H.JARDIN, H.pieces_jardin(H.JARDIN))


def premiere_phrase(doc):
    """Première phrase entière de la docstring (relecture du lot 3 : la première ligne seule tronquait les descriptions)."""
    d = " ".join(l.strip() for l in (doc or "").strip().split("\n"))
    i = d.find(". ")
    return d if i < 0 else d[:i + 1]


def main(noms=None):
    mesures = {}
    path = os.path.join(HERE, "kit_jardin.json")
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
