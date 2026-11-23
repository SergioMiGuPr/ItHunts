# -*- coding: utf-8 -*-
"""Nuit 4, lot 3 : pièces pour un level design en hauteur, communes aux trois actes (importé par kit_caves.py,
kit_chateau.py et kit_jardin.py, qui enregistrent leurs versions avec `enregistre`).

Chaque type de pièce est un constructeur paramétré par un **style d'acte** (`CAVE`, `CHATEAU`, `JARDIN` : matériaux,
garde-corps, décor). Mesures tirées du contrôleur (ART-HANDOFF § 3) : marches de 18 à 20 cm, saut 1.3 m, hissage jusqu'à
3.7 m (un saut) ou 5.3 m (double saut), dessus d'au moins 0.6 m de profondeur, wall jump de 1.8 m ; traqueur (§ 5 ter,
D-078) : porte étroite d'au moins 4 × 5.25 m (il se faufile), plateformes de moins de 10 m de profondeur où il ne se pose pas,
mur grimpable de 16 m au plus.

Conventions (comme kitlib) : Blender Z en haut, avant de la pièce vers -Y (Unity +Z), pivot au sol sauf mention.
Enfants nommés exportés avec la pièce :
- `Col_*` : surfaces de **collision seule** (matériau `M_Collision_Invisible`, rendu retiré par KitBuilder) : rampes lisses
  sous les marches des escaliers ;
- `Garde_Corps` : garde-corps, cordes et rampes (collision des joueurs, traversée par la main du traqueur, D-078) ;
- parties mobiles (`Battant_G`, `Battant_D`, `Vantail`, `Serrure`, `Herse`, `Trappe_G`, `Trappe_D`) : pivot à l'axe de
  rotation ou au bas du coulissement, axes et angles dans docs/art/INTEGRATION-PROPS.md (Nuit 4, lot 3).
"""
import math, random
from mathutils import Vector
import kitlib as L

COL = "M_Collision_Invisible"
SCEAU_AME = "M_Kit_Sceau_Ame"          # cyan des âmes (réservé au jeu) : sceau de la porte verrouillée
VITRAUX = ("M_Kit_Vitrail", "M_Kit_Vitrail_Rouge", "M_Kit_Vitrail_Ambre")
CRISTAL = "M_Kit_Cristal"               # gouttes du lustre à pampilles (relecture du lot 3 ; créé par KitBuilder)
CHIFFON = "M_Chiffon_Brule"             # tête des torches : tissu goudronné

CAVE = dict(act="Cave", mur="M_Bib_Moellons_Peint", pierre="M_Bib_PierreTaillee_Peint", marche="M_Bib_PierreTaillee_Peint",
            sol="M_Bib_Dalles_Sales", bois="M_Bib_BoisPourri", planche="M_Bib_BoisPourri", porte="M_Bib_BoisNeuf",
            fer="M_Bib_FerRouille", roche="M_Bib_RocheCave_Peint", corde="M_Corde", terre="M_Bib_Terre", garde="corde", seed=600)
CHATEAU = dict(act="Chateau", mur="M_Chateau_PierreClaire", pierre="M_Chateau_PierreClaire", marche="M_Chateau_PierreClaire", marbre="M_Bib_Marbre",
               sol="M_Chateau_Dalles", parquet="M_Chateau_Parquet", boiserie="M_Chateau_Boiserie", bois="M_Bib_BoisNeuf",
               planche="M_Chateau_Parquet", porte="M_Chateau_Boiserie", fer="M_Bib_FerRouille", tapis="M_Bib_Tapis",
               dore="M_Bib_Trim", corde="M_Corde", garde="balustre", seed=700)
JARDIN = dict(act="Jardin", mur="M_Bib_Moellons_Peint", pierre="M_Bib_PierreTaillee_Peint", marche="M_Bib_PierreTaillee_Peint",
              sol="M_Bib_Dalles_Sales", bois="M_Bib_BoisPourri", planche="M_Bib_BoisPourri", porte="M_Bib_BoisNeuf",
              fer="M_Bib_FerRouille", roche="M_Bib_RocheCave_Peint", feuil="M_Kit_Feuillage", herbe="M_Bib_HerbeSeche",
              terre="M_Bib_Terre_Racines", ecorce="M_Bib_Ecorce", corde="M_Corde", garde="fer", seed=800)

PEINT_ARCHI = dict(sale_h=2.0, mousse=0.45)
PEINT_OBJET = dict(sale_h=0.5, mousse=0.1)


# =================================================================== aides géométriques
def prism(k, poly, a0, a1, m, axis="x", m_cap=None, caps=(True, True)):
    """Polygone 2D extrudé : axis x -> poly en (y, z) ; axis y -> poly en (x, z) ; axis z -> poly en (x, y)."""
    def P(p, a):
        u, v = p
        return (a, u, v) if axis == "x" else (u, a, v) if axis == "y" else (u, v, a)
    mc = m_cap or m
    if caps[0]:
        k.face([P(p, a0) for p in poly], mc)
    if caps[1]:
        k.face([P(p, a1) for p in reversed(poly)], mc)
    for i in range(len(poly)):
        p, q = poly[i], poly[(i + 1) % len(poly)]
        k.face([P(p, a0), P(q, a0), P(q, a1), P(p, a1)], m)


def enfant(parent, kc, seed=0, pivot=None, peinture=None):
    """Termine un K enfant, le rattache au parent ; pivot (coordonnées de la pièce) : la géométrie est recentrée sur ce
    point (axe de rotation ou bas d'un coulissement). Le parent peut lui-même être un enfant (pièces mobiles imbriquées)."""
    if not len(kc.bm.faces) and not kc.empties:
        kc.bm.free()
        return None
    c = kc.finish(seed, peinture or PEINT_OBJET)
    origine = Vector((0, 0, 0))
    p_ = parent
    while p_ is not None:
        origine += p_.location
        p_ = p_.parent
    if pivot is not None:
        p = Vector(pivot)
        for v in c.data.vertices:
            v.co -= p
        for e in [o for o in c.children]:
            e.location -= p
        c.location = p - origine
    else:
        for v in c.data.vertices:
            v.co -= origine
        c.location = Vector((0, 0, 0))
    c.parent = parent
    return c


_NB = [0]


def dz():
    """Petit enfoncement (2 à 40 mm, 20 valeurs qui tournent) des blocs posés à plat : leurs bases plates ne sont jamais
    coplanaires (le nettoyage de l'export ne distingue que 4 décalages)."""
    _NB[0] = _NB[0] % 20 + 1
    return -0.002 * _NB[0]


def rampe_col(kc, pts):
    """Surface de collision (quad ou polygone plan)."""
    kc.face([tuple(p) for p in pts], COL)


# ------------------------------------------------------------------ corrections du lot 3 (relecture)
def col_boite(kc, k, n0, marge=0.01, zmin=None):
    """Boîte de collision (Col_*) qui englobe les sommets ajoutés à `k` depuis l'indice `n0` (blocs de décor posés le long
    d'un escalier : éboulis, socles, vases), agrandie de `marge` (aucune face confondue avec le décor)."""
    k.bm.verts.ensure_lookup_table()
    vs = [k.bm.verts[i].co for i in range(n0, len(k.bm.verts))]
    if not vs:
        return
    lo = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))) - Vector((marge, marge, marge))
    hi = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs))) + Vector((marge, marge, marge))
    if zmin is not None:
        lo.z = zmin
    kc.box(tuple((lo + hi) / 2), tuple(hi - lo), COL)


def col_prisme(kc, prof, x0, x1):
    """Volume de collision : profil (y, z) extrudé de x0 à x1."""
    prism(kc, prof, x0, x1, COL, axis="x")


def lierre(k, S, chemin, normale, l=0.32, seed=0, ecart=0.03):
    """Carte de lierre plate plaquée contre une paroi : bande fine qui suit `chemin` (points 3D, du haut vers le bas), à
    `ecart` de la paroi (le long de `normale(p)`, vecteur sortant de la paroi), bords dentelés de feuilles (triangles
    alternés), pointe effilée en bas. Matériau de feuillage de l'acte, double face. Aucun volume."""
    rnd = random.Random(seed)
    P = [Vector(p) for p in chemin]
    n = len(P)
    G, D = [], []
    for i, p in enumerate(P):
        t = (P[min(i + 1, n - 1)] - P[max(i - 1, 0)]).normalized()
        nn = Vector(normale(p)).normalized()
        s = t.cross(nn).normalized()
        u = i / (n - 1)
        w = l * (0.55 + 0.45 * math.sin(math.pi * min(1.0, u * 1.4 + 0.1))) * rnd.uniform(0.8, 1.1)
        if i == n - 1:
            w = 0.02
        c = p + nn * ecart
        G.append(c - s * w / 2)
        D.append(c + s * w / 2)
    m = S["feuil"]
    for i in range(n - 1):
        k.face([G[i], D[i], D[i + 1], G[i + 1]], m)
        # feuilles : une pointe par segment, d'un côté puis de l'autre
        a, b = (G[i], G[i + 1]) if i % 2 == 0 else (D[i + 1], D[i])
        mil = (a + b) / 2
        dehors = (mil - (G[i] + D[i] + G[i + 1] + D[i + 1]) / 4)
        if dehors.length > 1e-6:
            pointe = mil + dehors.normalized() * l * rnd.uniform(0.2, 0.3) + (b - a) * 0.25
            k.face([a, pointe, b] if i % 2 == 0 else [a, pointe, b], m)


def lierre_mur(k, S, x, z0, z1, y=0.0, l=0.32, seed=0, ecart=0.03):
    """Carte de lierre sur une face de mur plane tournée vers -Y (face en y), du haut z0 au bas z1, autour de x."""
    rnd = random.Random(seed)
    nseg = max(3, int((z0 - z1) / 0.45))
    pts = []
    for i in range(nseg + 1):
        z = z0 - (z0 - z1) * i / nseg
        pts.append((x + 0.08 * math.sin(i * 1.3 + seed) + rnd.uniform(-0.04, 0.04), y, z))
    lierre(k, S, pts, lambda p: (0, -1, 0), l=l, seed=seed, ecart=ecart)


def pierre(k, c, s, m, ch=0.035, bas=None, seed=0, usure=0.012, dos=False):
    """Pierre de taille ou about de poutre engagé dans un mur dont la face est vers -Y : boîte centrée en c, tailles s,
    **arêtes avant chanfreinées** (`ch`), chanfrein du dessous avant plus fort si `bas` = (profondeur, hauteur) (pierre
    d'attente, console), coins légèrement irréguliers (arêtes usées). Enveloppe convexe : volume fermé, dessus plat.
    La face arrière (dans le mur) est omise sauf si `dos`."""
    import bmesh
    rnd = random.Random(seed)
    cx, cy, cz = c
    hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
    bm = bmesh.new()
    pts = []
    for sx in (-1, 1):
        for sz in (-1, 1):
            pts.append((cx + sx * hx, cy + hy, cz + sz * hz))
            cy_, cz_ = (ch, ch) if (sz > 0 or bas is None) else bas
            j = lambda: rnd.uniform(-usure, usure)
            pts.append((cx + sx * (hx - ch) + j(), cy - hy, cz + sz * (hz - cz_) + j()))
            pts.append((cx + sx * hx, cy - hy + cy_ + j(), cz + sz * (hz - cz_) + j()))
            pts.append((cx + sx * (hx - ch) + j(), cy - hy + cy_ + j(), cz + sz * hz))
    vs = [bm.verts.new(p) for p in pts]
    bmesh.ops.convex_hull(bm, input=vs)
    for f in bm.faces:
        if not dos and all(abs(v.co.y - (cy + hy)) < 1e-6 for v in f.verts):
            continue
        k.face([tuple(v.co) for v in f.verts], m)
    bm.free()


def flamme3(cx, cz, h):
    """Flamme d'âme lisible à trois langues (sceau de la porte verrouillée) : langue centrale haute, deux langues
    latérales plus basses qui s'écartent, base ronde ; hauteur h, base en (cx, cz)."""
    P = [(0.00, 0.00), (0.19, 0.03), (0.32, 0.12), (0.39, 0.26), (0.39, 0.40), (0.47, 0.64), (0.30, 0.49), (0.21, 0.52),
         (0.17, 0.70), (0.07, 0.86), (0.02, 1.00), (-0.07, 0.85), (-0.15, 0.70), (-0.19, 0.53), (-0.29, 0.50),
         (-0.47, 0.66), (-0.40, 0.42), (-0.39, 0.26), (-0.32, 0.12), (-0.19, 0.03)]
    return [(cx + x * h, cz + z * h) for x, z in P]


def mur_rainure(k, x0, x1, h, t, o, m, m_reveal, ya, yb, xs, zs, zb=-0.05):
    """Mur de x0 à x1 (face avant en y = 0, épaisseur t, hauteur h) percé d'une ouverture `o` (dict de `_contour`) et
    creusé d'une **rainure** cachée pour une partie qui coulisse vers le haut (herse, vantail) : vide de ya à yb (y),
    de -xs à xs (x) et de zb à zs (z). Le tableau de l'ouverture est interrompu dans la rainure ; les parois de la
    rainure sont des faces réelles, la partie mobile y monte sans traverser la maçonnerie."""
    c = _contour(o)
    _face_percee(k, x0, x1, h, 0.0, [c], m, +1)
    _face_percee(k, x0, x1, h, t, [c], m, -1)
    n = len(c)
    for i in range(n - 1):
        (xa_, za_), (xb_, zb_) = c[i], c[i + 1]
        for (y_a, y_b) in ((0.0, ya), (yb, t)):
            k.face([(xa_, y_a, za_), (xb_, y_a, zb_), (xb_, y_b, zb_), (xa_, y_b, za_)], m_reveal)
    k.face([(x0, 0, h), (x1, 0, h), (x1, t, h), (x0, t, h)], m)
    for x in (x0, x1):
        k.face([(x, 0, 0), (x, t, 0), (x, t, h), (x, 0, h)], m)
    # parois de la rainure : faces percées de l'ouverture (avant et arrière), joues, fond haut, fonds bas des joues
    for y, sens in ((ya, -1), (yb, +1)):
        _face_percee_z(k, -xs, xs, zb, zs, y, c, m_reveal, sens)
    for sx in (-1, 1):
        k.face([(sx * xs, ya, zb), (sx * xs, yb, zb), (sx * xs, yb, zs), (sx * xs, ya, zs)], m_reveal)
    k.face([(-xs, ya, zs), (xs, ya, zs), (xs, yb, zs), (-xs, yb, zs)], m_reveal)
    xo = o["ow"] / 2
    for sx in (-1, 1):
        a, b = sorted((sx * xo, sx * xs))
        k.face([(a, ya, zb), (b, ya, zb), (b, yb, zb), (a, yb, zb)], m_reveal)


def _face_percee_z(k, x0, x1, z0, z1, y, contour, m, sens):
    """Face plane en y de x0 à x1 et de z0 à z1, privée de l'ouverture `contour` (posée au sol, z de 0 au haut de l'arc) :
    bandes verticales découpées aux abscisses du contour."""
    xs_ = sorted(set([x0, x1] + [p[0] for p in contour if x0 < p[0] < x1]))
    l, r = contour[0][0], contour[-1][0]
    for i in range(len(xs_) - 1):
        xa, xb = xs_[i], xs_[i + 1]
        xm = (xa + xb) / 2
        if l - 1e-6 <= xm <= r + 1e-6:
            za, zb_ = _ztop(contour, xa), _ztop(contour, xb)
        else:
            za = zb_ = z0
        if z1 - max(za, zb_) < 1e-6:
            continue
        q = [(xa, y, za), (xb, y, zb_), (xb, y, z1), (xa, y, z1)]
        k.face(q if sens > 0 else list(reversed(q)), m)


def anneau_obox(k, c, r, w, d, m, n=16, a0=math.pi / 2, plan="xz", y=0.0):
    """Anneau de poutres (cercle de rayon r) dans le plan XZ (face -Y), centre c (x, z)."""
    pts = []
    for i in range(n + 1):
        a = a0 + 2 * math.pi * i / n
        pts.append(Vector((c[0] + math.cos(a) * r, y, c[1] + math.sin(a) * r)))
    for i in range(n):
        k.obox(pts[i], pts[i + 1], w, d, m, up=(0, 1, 0))
    return pts


def flamme(cx, cz, h):
    """Contour d'une flamme d'âme (pointe légèrement recourbée), hauteur h, base en (cx, cz)."""
    P = [(0.08, 1.0), (-0.10, 0.80), (-0.26, 0.58), (-0.33, 0.38), (-0.31, 0.20), (-0.20, 0.06), (0.0, 0.0),
         (0.20, 0.06), (0.31, 0.20), (0.32, 0.38), (0.22, 0.58), (0.10, 0.74), (0.13, 0.87)]
    return [(cx + x * h, cz + z * h) for x, z in P]


def croissant(cx, cz, r1, d=0.42, r2=0.82, rot=0.6, n=14):
    """Croissant de lune : cercle (0, r1) privé du cercle (d·r1, r2·r1), tourné de `rot`."""
    d, r2 = d * r1, r2 * r1
    xi = (d * d + r1 * r1 - r2 * r2) / (2 * d)
    yi = math.sqrt(max(r1 * r1 - xi * xi, 0))
    a = math.atan2(yi, xi)
    pts = []
    for i in range(n + 1):
        t = a + (2 * math.pi - 2 * a) * i / n
        pts.append((math.cos(t) * r1, math.sin(t) * r1))
    b0 = math.atan2(-yi, xi - d) % (2 * math.pi)
    b1 = math.atan2(yi, xi - d)
    for i in range(1, n):
        t = b0 + (b1 - b0) * i / n
        pts.append((d + math.cos(t) * r2, math.sin(t) * r2))
    cr, sr = math.cos(rot), math.sin(rot)
    return [(cx + x * cr - z * sr, cz + x * sr + z * cr) for x, z in pts]


def etoile(cx, cz, r, n=4, creux=0.35, rot=0.0):
    pts = []
    for i in range(2 * n):
        a = rot + math.pi * i / n + math.pi / 2
        rr = r if i % 2 == 0 else r * creux
        pts.append((cx + math.cos(a) * rr, cz + math.sin(a) * rr))
    return pts


# ------------------------------------------------------------------ garde-corps
def garde(k, S, a, b, h=1.1, kind=None, bout=True):
    """Garde-corps de a à b (points au niveau du sol de marche) ; `kind` : balustre, corde, bois, fer, parapet."""
    kind = kind or S["garde"]
    a, b = Vector(a), Vector(b)
    d = b - a
    Lh = Vector((d.x, d.y, 0)).length
    up = Vector((0, 0, 1))
    def poteaux(pas):
        n = max(1, int(math.ceil(Lh / pas)))
        return [a.lerp(b, i / n) for i in range(0 if bout else 1, n + 1 if bout else n)]
    if kind == "balustre":
        m = S["pierre"]
        k.obox(a + up * 0.08, b + up * 0.08, 0.34, 0.2, m)
        k.obox(a + up * (h - 0.08), b + up * (h - 0.08), 0.4, 0.16, m)
        n = max(2, int(round(Lh / 0.42)))
        hb = h - 0.34
        for i in range(1, n):
            p = a.lerp(b, i / n)
            k.lathe((p.x, p.y, p.z + 0.17), [(0.065, 0), (0.1, hb * 0.18), (0.055, hb * 0.45), (0.11, hb * 0.7), (0.055, hb * 0.92),
                                             (0.075, hb + 0.02)], m, n=8)
        if bout:
            for p in (a, b):
                k.box((p.x, p.y, p.z + h / 2 + 0.05), (0.42, 0.42, h + 0.14), m)
    elif kind == "corde":
        pts = poteaux(2.2)
        for p in pts:
            k.box((p.x, p.y, p.z + (h + 0.15) / 2 - 0.05), (0.16, 0.16, h + 0.15), S["bois"])
        for i in range(len(pts) - 1):
            for zz, sag in ((h - 0.05, 0.14), (h * 0.55, 0.1)):
                seg = []
                for t in range(7):
                    u = t / 6
                    p = pts[i].lerp(pts[i + 1], u)
                    seg.append(p + up * (zz - sag * math.sin(math.pi * u)))
                for t in range(6):
                    k.obox(seg[t], seg[t + 1], 0.05, 0.05, S["corde"])
    elif kind == "bois":
        for p in poteaux(1.8):
            k.box((p.x, p.y, p.z + (h + 0.1) / 2 - 0.05), (0.14, 0.14, h + 0.1), S["bois"])
        k.obox(a + up * h, b + up * h, 0.16, 0.1, S["bois"])
        k.obox(a + up * h * 0.5, b + up * h * 0.5, 0.1, 0.08, S["bois"])
    elif kind == "fer":
        for p in poteaux(2.0):
            k.box((p.x, p.y, p.z + (h + 0.05) / 2 - 0.03), (0.07, 0.07, h + 0.05), S["fer"])
            k.lathe((p.x, p.y, p.z + h), [(0.0, 0), (0.06, 0.04), (0.05, 0.1), (0.0, 0.16)], S["fer"], n=6)
        k.obox(a + up * (h - 0.03), b + up * (h - 0.03), 0.07, 0.05, S["fer"])
        k.obox(a + up * 0.12, b + up * 0.12, 0.05, 0.04, S["fer"])
        n = max(2, int(round(Lh / 0.15)))
        for i in range(1, n):
            p = a.lerp(b, i / n)
            k.box((p.x, p.y, p.z + (h + 0.12) / 2 - 0.0), (0.026, 0.026, h - 0.16), S["fer"])
    elif kind == "parapet":
        # bandeau continu à faces verticales (les tronçons successifs se soudent, même en pente)
        n_ = Vector((-d.y, d.x, 0)).normalized()
        def bloc(w, z0, z1, m):
            pa = [a + n_ * (w / 2) + up * z0, a - n_ * (w / 2) + up * z0, a - n_ * (w / 2) + up * z1, a + n_ * (w / 2) + up * z1]
            pb = [b + n_ * (w / 2) + up * z0, b - n_ * (w / 2) + up * z0, b - n_ * (w / 2) + up * z1, b + n_ * (w / 2) + up * z1]
            for i in range(4):
                j = (i + 1) % 4
                k.face([pa[i], pa[j], pb[j], pb[i]], m)
            if bout:
                k.face(list(reversed(pa)), m)
                k.face(pb, m)
        bloc(0.42, -0.03, h, S["mur"])
        bloc(0.54, h - 0.03, h + 0.11, S["pierre"])


# ------------------------------------------------------------------ murs percés
def _contour(o, e=0.0):
    """Contour (x, z) d'une ouverture, de bas-gauche à bas-droite par le haut ; `e` élargit (ébrasement)."""
    ow = o["ow"] + 2 * e
    rise = o.get("rise", 0.0) * (ow / o["ow"]) if o.get("rise") else 0.0
    z0 = o["z0"] - (e * 0.5 if o["z0"] > 0 else 0.0)
    spring = o["spring"] + (e * 0.2 if rise else e)
    cx = o["cx"]
    if rise:
        prof = L.arch_profile(ow, rise, o.get("n", 12), o.get("pointed", False))
        haut = [(cx + x, spring + z) for x, z in prof]
    else:
        haut = [(cx - ow / 2, spring), (cx + ow / 2, spring)]
    return [(cx - ow / 2, z0)] + haut + [(cx + ow / 2, z0)]


def _ztop(c, x):
    haut = c[1:-1]
    for i in range(len(haut) - 1):
        (xa, za), (xb, zb) = haut[i], haut[i + 1]
        if xa - 1e-6 <= x <= xb + 1e-6:
            t = 0 if abs(xb - xa) < 1e-9 else (x - xa) / (xb - xa)
            return za + (zb - za) * t
    return None


def _face_percee(k, x0, x1, h, y, contours, m, sens):
    xs = sorted(set([round(x0, 5), round(x1, 5)] + [round(p[0], 5) for c in contours for p in c]))
    xs = [x for x in xs if x0 - 1e-6 <= x <= x1 + 1e-6]
    def trou(x, xa, xb):
        for c in contours:
            l, r = c[0][0], c[-1][0]
            if l - 1e-6 <= xa and xb <= r + 1e-6:
                return c
        return None
    def coupes(x):
        zs = {0.0, h}
        for c in contours:
            l, r = c[0][0], c[-1][0]
            if l - 1e-6 <= x <= r + 1e-6:
                zs.add(round(c[0][1], 5))
                zt = _ztop(c, x)
                if zt is not None:
                    zs.add(round(zt, 5))
        return sorted(zs)
    for i in range(len(xs) - 1):
        xa, xb = xs[i], xs[i + 1]
        c = trou((xa + xb) / 2, xa, xb)
        if c is None:
            pleins = [((0.0, 0.0), (h, h))]
        else:
            z0 = c[0][1]
            pleins = []
            if z0 > 1e-6:
                pleins.append(((0.0, 0.0), (z0, z0)))
            pleins.append(((_ztop(c, xa), _ztop(c, xb)), (h, h)))
        ca, cb = coupes(xa), coupes(xb)
        for (la, lb), (ha, hb) in pleins:
            if ha - la < 1e-5 and hb - lb < 1e-5:
                continue
            pts = [(xa, la), (xb, lb)]
            pts += [(xb, z) for z in cb if lb + 1e-5 < z < hb - 1e-5]
            pts += [(xb, hb), (xa, ha)]
            pts += [(xa, z) for z in reversed(ca) if la + 1e-5 < z < ha - 1e-5]
            q = [(x, y, z) for x, z in pts]
            if hb - lb < 1e-5:
                q = [p for j, p in enumerate(q) if j != 2]
            k.face(q if sens > 0 else list(reversed(q)), m)


def mur_perce(k, x0, x1, h, y0, t, ouvertures, m, m_reveal=None, ebr=0.0, dessus=True, cotes=True):
    """Mur de x0 à x1, hauteur h, face avant en y0 (vers -Y), épaisseur t, percé d'ouvertures
    (dict cx, ow, z0, spring, rise, pointed, n). `ebr` : ébrasement (ouverture élargie côté face avant).
    Retourne les contours avant et arrière."""
    mr = m_reveal or m
    cf = [_contour(o, ebr) for o in ouvertures]
    cb = [_contour(o) for o in ouvertures]
    _face_percee(k, x0, x1, h, y0, cf, m, +1)
    _face_percee(k, x0, x1, h, y0 + t, cb, m, -1)
    for o, f, b in zip(ouvertures, cf, cb):
        n = len(f)
        for i in range(n - 1 if o["z0"] <= 1e-6 else n):
            j = (i + 1) % n
            if o["z0"] <= 1e-6 and i == n - 1:
                continue
            k.face([(f[i][0], y0, f[i][1]), (f[j][0], y0, f[j][1]), (b[j][0], y0 + t, b[j][1]), (b[i][0], y0 + t, b[i][1])], mr)
    if dessus:
        k.face([(x0, y0, h), (x1, y0, h), (x1, y0 + t, h), (x0, y0 + t, h)], m)
    if cotes:
        for x in (x0, x1):
            k.face([(x, y0, 0), (x, y0 + t, 0), (x, y0 + t, h), (x, y0, h)], m)
    return cf, cb


def encadrement(k, c, y, w, d, m, sill=False):
    """Chambranle (bandeau saillant) le long du contour d'une ouverture, devant la face en y (saillie d)."""
    pts = c if sill else c
    cx = (c[0][0] + c[-1][0]) / 2
    zc = (c[0][1] + max(p[1] for p in c)) / 2
    for i in range(len(pts) - 1):
        (xa, za), (xb, zb) = pts[i], pts[i + 1]
        if abs(za - zb) < 1e-6 and abs(za - c[0][1]) < 1e-6:
            continue   # pas de bandeau au seuil
        da = Vector((xb - xa, 0, zb - za))
        if da.length < 1e-6:
            continue
        nrm = Vector((da.z, 0, -da.x)).normalized()
        if nrm.dot(Vector(((xa + xb) / 2 - cx, 0, (za + zb) / 2 - zc))) < 0:
            nrm = -nrm
        o = nrm * (w / 2)
        ext = da.normalized() * (w / 2 if abs(da.z) > abs(da.x) * 2 else 0.04)
        # pas de prolongement sous le seuil (le bandeau s'arrête au sol ou à l'appui)
        a = Vector((xa, y - d / 2 + 0.02, za)) + o - (ext if i > 0 else ext * 0)
        b = Vector((xb, y - d / 2 + 0.02, zb)) + o + (ext if i < len(pts) - 2 else ext * 0)
        k.obox(a, b, w, d, m, up=(0, 1, 0))


# ------------------------------------------------------------------ escaliers
def volee(k, T, w, n, r, g, z0, m_side, m_step, base=0.0, palier=0.0, m_pal=None, tapis=0.0, m_tapis=None):
    """Volée pleine : n marches (hauteur r, giron g) qui montent vers +v depuis le niveau z0, largeur u ∈ [-w/2, w/2],
    pleine jusqu'à `base` ; palier en haut. T(u, v, z) -> (x, y, z). Retourne la ligne de nez (rampe) en (v, z)."""
    prof = [(0.0, base)]
    if z0 > base + 1e-6:
        prof.append((0.0, z0))
    for i in range(n):
        prof.append((i * g, z0 + (i + 1) * r))
        prof.append(((i + 1) * g, z0 + (i + 1) * r))
    vend = n * g + palier
    if palier:
        prof.append((vend, z0 + n * r))
    prof.append((vend, base))
    for u in (-w / 2, w / 2):
        k.face([T(u, v, z) for v, z in prof], m_side)
    bandes = [(-w / 2, w / 2, m_step)] if not tapis else [(-w / 2, -tapis / 2, m_step), (-tapis / 2, tapis / 2, m_tapis), (tapis / 2, w / 2, m_step)]
    def quad(v1, z1, v2, z2, ua, ub, m):
        k.face([T(ua, v1, z1), T(ub, v1, z1), T(ub, v2, z2), T(ua, v2, z2)], m)
    if z0 > base + 1e-6:
        quad(0, base, 0, z0, -w / 2, w / 2, m_side)
    for i in range(n):
        for ua, ub, m in bandes:
            quad(i * g, z0 + i * r, i * g, z0 + (i + 1) * r, ua, ub, m)
            quad(i * g, z0 + (i + 1) * r, (i + 1) * g, z0 + (i + 1) * r, ua, ub, m)
    if palier:
        quad(n * g, z0 + n * r, vend, z0 + n * r, -w / 2, w / 2, m_pal or m_step)
    quad(vend, z0 + n * r, vend, base, -w / 2, w / 2, m_side)
    if base > 1e-6 or base < -1e-6:
        quad(vend, base, 0, base, -w / 2, w / 2, m_side)
    return (-g, z0), ((n - 1) * g, z0 + n * r)


def marches(H):
    n = int(math.ceil(H / 0.2 - 1e-6))
    return n, H / n


# =================================================================== 1. escalier droit
def escalier_droit(S, nom, H=4.0, w=3.0, g=0.3, palier=1.5):
    """Escalier droit plein : monte de H (marches de 18 à 20 cm), pied à y = 0, monte vers +Y (Unity -Z), palier en haut."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    kc = L.K("Col_Rampe")
    n, r = marches(H)
    T = lambda u, v, z: (u, v, z)
    act = S["act"]
    tapis = 1.6 if act == "Chateau" else 0.0
    (v0, za), (v1, zb) = volee(k, T, w, n, r, g, 0.0, S["mur"] if act != "Chateau" else S["pierre"], S["marche"],
                               palier=palier, m_pal=S["sol"], tapis=tapis, m_tapis=S.get("tapis"))
    rampe_col(kc, [(-w / 2, v0, za), (w / 2, v0, za), (w / 2, v1, zb), (-w / 2, v1, zb)])
    yt = n * g + palier
    # volume de collision du bloc (KitBuilder ne met plus de collider au rendu des pièces à Col_*) : joues, palier, dos ;
    # 1 cm hors des faces visibles (aucune face confondue)
    kv = L.K("Col_Escalier")
    e_ = 0.01
    # pente 2.5 cm sous la rampe (aucune face confondue avec Col_Rampe), dessus du palier 6 mm au-dessus du palier visible
    col_prisme(kv, [(v0 + 0.04, -0.02), (v1, H - 0.025), (yt + e_, H - 0.025), (yt + e_, -0.02)], -w / 2 - e_, w / 2 + e_)
    kv.face([(-w / 2 - e_, v1, H + 0.006), (w / 2 + e_, v1, H + 0.006), (w / 2 + e_, yt + e_, H + 0.006), (-w / 2 - e_, yt + e_, H + 0.006)], COL)
    if act == "Cave":
        garde(kg, S, (w / 2 - 0.15, 0.15, r), (w / 2 - 0.15, (n - 1) * g + 0.1, H), kind="corde")
        garde(kg, S, (w / 2 - 0.15, (n - 1) * g + 0.1, H), (w / 2 - 0.15, yt - 0.15, H), kind="corde")
        rnd = random.Random(S["seed"] + 1)
        for j in range(3):
            n0 = len(k.bm.verts)
            L.rock_blob(k, (-w / 2 - 0.6 + rnd.uniform(-0.2, 0.2), 0.4 + j * 1.6, 0 + dz()), (1.0, 1.1, 0.7), S["roche"], seed=S["seed"] + j, res=8)
            col_boite(kv, k, n0, zmin=-0.05)
    elif act == "Chateau":
        for sx in (-1, 1):
            x = sx * (w / 2 - 0.22)
            garde(kg, S, (x, 0.1, r), (x, (n - 1) * g + 0.1, H), kind="balustre")
            garde(kg, S, (x, (n - 1) * g + 0.1, H), (x, yt - 0.23, H), kind="balustre")
            kg.lathe((x, 0.1, r + 1.1 + 0.12), [(0.0, 0), (0.18, 0.04), (0.22, 0.2), (0.16, 0.36), (0.0, 0.42)], S["pierre"], n=10)
    else:
        # murs d'échiffre pleins de chaque côté (0.45 m), chaperon qui suit la pente, socles à vase au pied
        prof = [(-0.4, 0.0), (-0.4, 1.05), (0.0, r + 0.9), ((n - 1) * g, H + 0.9), (yt, H + 0.9), (yt, 0.0)]
        for sx in (-1, 1):
            x0, x1 = sorted((sx * w / 2, sx * (w / 2 + 0.45)))
            prism(k, prof, x0, x1, S["mur"], axis="x")
            xc = sx * (w / 2 + 0.225)
            for (pa, pb) in zip(prof[1:4], prof[2:5]):
                k.obox((xc, pa[0], pa[1] + 0.06), (xc, pb[0], pb[1] + 0.06), 0.6, 0.14, S["pierre"])
            n0 = len(k.bm.verts)
            k.box((xc, -0.75, 0.6), (0.8, 0.7, 1.2), S["pierre"])
            k.lathe((xc, -0.75, 1.2), [(0.18, 0), (0.32, 0.25), (0.28, 0.55), (0.12, 0.6), (0.2, 0.75), (0.0, 0.75)], S["pierre"], n=10)
            col_boite(kv, k, n0, zmin=-0.05)
            # mur d'échiffre et son chaperon (0.6 m de large, 0.13 m au-dessus du mur)
            a_, b_ = sorted((sx * (w / 2 - 0.08), sx * (w / 2 + 0.53)))
            col_prisme(kv, [(y_, z_ + (0.135 if z_ > 0 else -0.02)) for y_, z_ in [(-0.41, 0.0), (-0.41, 1.05), (0.0, r + 0.9),
                                                                                  ((n - 1) * g, H + 0.9), (yt + 0.01, H + 0.9), (yt + 0.01, 0.0)]], a_, b_)
    ob = k.finish(S["seed"] + 11, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 12, peinture=PEINT_ARCHI)
    enfant(ob, kc, 0)
    enfant(ob, kv, 0)
    return ob


# =================================================================== 2. escalier tournant (quart tournant, palier)
def escalier_tournant(S, nom, H=4.0, w=3.0, g=0.3):
    """Escalier à quart tournant : volée 1 vers +Y, palier carré à mi-hauteur, volée 2 vers +X (à droite), palier en haut."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    kc = L.K("Col_Rampe")
    n, r = marches(H)
    n1 = n // 2
    n2 = n - n1
    h1 = n1 * r
    act = S["act"]
    ms = S["mur"] if act != "Chateau" else S["pierre"]
    tapis = 1.6 if act == "Chateau" else 0.0
    T1 = lambda u, v, z: (u, v, z)
    (a0, za0), (a1, za1) = volee(k, T1, w, n1, r, g, 0.0, ms, S["marche"], tapis=tapis, m_tapis=S.get("tapis"))
    y1 = n1 * g
    # palier : bloc plein
    k.box((0, y1 + w / 2, h1 / 2), (w, w, h1), ms, skip=("-z",))
    # tapis du palier (bande découpée dans le dessus : remplacée par trois bandes)
    yc = y1 + w / 2
    T2 = lambda u, v, z: (w / 2 + v, yc - u, z)
    (b0, zb0), (b1, zb1) = volee(k, T2, w, n2, r, g, h1, ms, S["marche"], palier=1.5, m_pal=S["sol"], tapis=tapis, m_tapis=S.get("tapis"))
    rampe_col(kc, [(-w / 2, a0, za0), (w / 2, a0, za0), (w / 2, a1, za1), (-w / 2, a1, za1)])
    rampe_col(kc, [T2(-w / 2, b0, zb0), T2(w / 2, b0, zb0), T2(w / 2, b1, zb1), T2(-w / 2, b1, zb1)])
    kind = {"Cave": "corde", "Chateau": "balustre", "Jardin": "fer"}[act]
    e = 0.22 if kind == "balustre" else 0.12
    xe = w / 2 + n2 * g + 1.5
    # volée 1 : deux côtés ; palier : côtés extérieurs ; volée 2 : deux côtés
    j1 = (n1 - 1) * g + 0.1
    j2 = w / 2 + (n2 - 1) * g + 0.1
    garde(kg, S, (-w / 2 + e, 0.1, r), (-w / 2 + e, j1, h1), kind=kind)
    garde(kg, S, (w / 2 - e, 0.1, r), (w / 2 - e, j1, h1), kind=kind)
    garde(kg, S, (-w / 2 + e, j1, h1), (-w / 2 + e, y1 + w - e, h1), kind=kind)
    garde(kg, S, (-w / 2 + e, y1 + w - e, h1), (w / 2 - 0.03, y1 + w - e, h1), kind=kind)
    garde(kg, S, (w / 2 + 0.03, y1 + w - e, h1 + r), (j2, y1 + w - e, H), kind=kind)
    garde(kg, S, (j2, y1 + w - e, H), (xe - e, y1 + w - e, H), kind=kind)
    garde(kg, S, (w / 2 + 0.1, y1 + e, h1 + r), (j2, y1 + e, H), kind=kind)
    garde(kg, S, (j2, y1 + e, H), (xe - e, y1 + e, H), kind=kind)
    # volume de collision : A = volée 1 et palier carré (pente 2.5 cm sous la rampe), B = volée 2 et palier haut, séparés
    # d'1 cm ; dessus des paliers 6 mm au-dessus des paliers visibles ; 1 cm hors des faces visibles
    kv = L.K("Col_Escalier")
    e_ = 0.01
    yb_ = y1 + w + e_
    col_prisme(kv, [(a0 + 0.04, -0.02), (a1, h1 - 0.025), (yb_, h1 - 0.025), (yb_, -0.02)], -w / 2 - e_, w / 2 + e_)
    xb0 = w / 2 + b0
    xb1 = w / 2 + b1
    xl = w / 2 + 0.017
    pente = (H - h1) / (xb1 - xb0 - 0.04)
    zl = h1 - 0.025 + (xl - xb0 - 0.04) * pente
    prism(kv, [(xl, -0.02), (xl, zl), (xb1, H - 0.025), (xe + e_, H - 0.025), (xe + e_, -0.02)], y1 - e_, yb_, COL, axis="y")
    kv.face([(-w / 2 - e_, a1, h1 + 0.006), (xb0, a1, h1 + 0.006), (xb0, yb_, h1 + 0.006), (-w / 2 - e_, yb_, h1 + 0.006)], COL)
    kv.face([(xb1, y1 - e_, H + 0.006), (xe + e_, y1 - e_, H + 0.006), (xe + e_, yb_, H + 0.006), (xb1, yb_, H + 0.006)], COL)
    ob = k.finish(S["seed"] + 21, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 22, peinture=PEINT_ARCHI)
    enfant(ob, kc, 0)
    enfant(ob, kv, 0)
    return ob


# =================================================================== 3. escalier en colimaçon
def colimacon(S, nom, H=6.4, R=2.0, noyau=0.45, par_tour=16):
    """Escalier en colimaçon autour d'un noyau, Ø 2R, marches de 20 cm (16 par tour : 3.2 m de hauteur libre par tour),
    sous-face hélicoïdale, palier de 100° en haut ; rampe de collision hélicoïdale sous les nez de marche."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    kc = L.K("Col_Rampe")
    n, r = marches(H)
    da = 2 * math.pi / par_tour
    ri = noyau - 0.03
    act = S["act"]
    ep = 0.28
    def P(rr, a, z):
        return (math.cos(a) * rr, math.sin(a) * rr, z)
    def zsous(a):
        return max(0.0, a / da * r - ep + r)
    for i in range(n):
        a0, a1 = i * da, (i + 1) * da
        zt = (i + 1) * r
        zm = (a0 + a1) / 2
        # dessus (2 secteurs) et contremarche
        k.face([P(ri, a0, zt), P(R, a0, zt), P(R, zm, zt), P(ri, zm, zt)], S["marche"])
        k.face([P(ri, zm, zt), P(R, zm, zt), P(R, a1, zt), P(ri, a1, zt)], S["marche"])
        k.face([P(ri, a0, i * r), P(R, a0, i * r), P(R, a0, zt), P(ri, a0, zt)], S["marche"])
        # bande extérieure : du dessous hélicoïdal au dessus de marche (+ sommet de la marche suivante côté a1)
        k.face([P(R, a0, zsous(a0)), P(R, zm, zsous(zm)), P(R, a1, zsous(a1)), P(R, a1, zt), P(R, zm, zt), P(R, a0, zt)]
               + ([P(R, a0, i * r)] if zsous(a0) < i * r - 1e-6 else []), S["marche"])
        # sous-face
        for (b0, b1) in ((a0, zm), (zm, a1)):
            if zsous(b0) > 1e-6 or zsous(b1) > 1e-6:
                k.face([P(ri, b0, zsous(b0)), P(ri, b1, zsous(b1)), P(R, b1, zsous(b1)), P(R, b0, zsous(b0))], S["mur"] if act != "Chateau" else S["pierre"])
    # palier en haut : secteur de 100° (dalle de 0.3 m)
    a0 = n * da
    na = 6
    secteur = [a0 + math.radians(100) * j / na for j in range(na + 1)]
    for j in range(na):
        b0, b1 = secteur[j], secteur[j + 1]
        k.face([P(ri, b0, H), P(R, b0, H), P(R, b1, H), P(ri, b1, H)], S["sol"])
        k.face([P(ri, b1, H - 0.3), P(R, b1, H - 0.3), P(R, b0, H - 0.3), P(ri, b0, H - 0.3)], S["mur"] if act != "Chateau" else S["pierre"])
        k.face([P(R, b0, H - 0.3), P(R, b1, H - 0.3), P(R, b1, H), P(R, b0, H)], S["marche"])
    b1 = secteur[-1]
    k.face([P(ri, b1, H - 0.3), P(R, b1, H - 0.3), P(R, b1, H), P(ri, b1, H)], S["marche"])
    k.face([P(ri, a0, H - 0.3), P(R, a0, H - 0.3), P(R, a0, zsous(a0)), P(ri, a0, zsous(a0))], S["marche"])
    # noyau
    hn = H + 1.1
    if act == "Chateau":
        k.lathe((0, 0, 0), [(noyau + 0.25, 0), (noyau + 0.25, 0.3), (noyau + 0.08, 0.45), (noyau, 0.55)], S["pierre"], n=par_tour)
        k.cyl((0, 0, 0.55), noyau, hn - 0.55, S["pierre"], n=par_tour, caps=(False, False))
        k.lathe((0, 0, hn), [(noyau, 0), (noyau + 0.12, 0.12), (noyau + 0.12, 0.25), (0.0, 0.25)], S["pierre"], n=par_tour)
        kg.lathe((0, 0, hn + 0.25), [(0.0, 0), (0.22, 0.05), (0.27, 0.25), (0.2, 0.45), (0.0, 0.5)], S["dore"], n=10)
    else:
        k.cyl((0, 0, 0), noyau, hn, S["mur"], n=par_tour, rough=0.04 if act == "Cave" else 0.0, seed=S["seed"])
        k.lathe((0, 0, hn), [(noyau + 0.06, 0), (noyau + 0.06, 0.15), (0.0, 0.2)], S["pierre"], n=par_tour)
    # rampe de collision hélicoïdale sous les nez de marche
    m_ = 2 * n
    for j in range(m_):
        t0 = -da + (n * da) * j / m_
        t1 = -da + (n * da) * (j + 1) / m_
        z0 = (t0 / da + 1) * r
        z1 = (t1 / da + 1) * r
        # bord intérieur à 0.3 m du noyau : le centre du joueur (rayon 0.4) n'en approche pas, pente de 34.4° au plus (mesurée)
        rampe_col(kc, [P(noyau + 0.3, t0, z0), P(R, t0, z0), P(R, t1, z1), P(noyau + 0.3, t1, z1)])
    # volume de collision : noyau, sous-face hélicoïdale et bandeau extérieur des marches, palier (dessus, dessous, bord),
    # 1 cm hors des faces visibles
    kv = L.K("Col_Escalier")
    rn = noyau * (1.06 if act == "Cave" else 1.0) + 0.012
    kv.cyl((0, 0, -0.02), rn, hn + 0.3, COL, n=par_tour)
    if act == "Chateau":
        kv.cyl((0, 0, -0.02), noyau + 0.262, 0.47, COL, n=par_tour)
    Re = R + 0.01
    def zr(t):
        return (t / da + 1) * r
    def zc(t):
        return max(-0.02, zr(t) - ep - 0.01)
    def zt(t):
        return min(zr(t), H + 0.006)
    nseg = 2 * n
    for j in range(nseg):
        t0 = n * da * j / nseg
        t1 = n * da * (j + 1) / nseg
        if zc(t0) > -0.019 or zc(t1) > -0.019:
            kv.face([P(rn, t1, zc(t1)), P(Re, t1, zc(t1)), P(Re, t0, zc(t0)), P(rn, t0, zc(t0))], COL)
        kv.face([P(Re, t0, zc(t0)), P(Re, t1, zc(t1)), P(Re, t1, zt(t1)), P(Re, t0, zt(t0))], COL)
    # dessus du dernier secteur (marche du haut, à H) et du palier : 6 mm au-dessus
    sect_c = secteur[:-1] + [secteur[-1] + 0.006]       # bord du palier de collision 6 mrad au-delà du bord visible
    haut = [(n - 1) * da + da * j / 2 for j in range(3)] + sect_c[1:]
    for j in range(len(haut) - 1):
        b0, b1 = haut[j], haut[j + 1]
        kv.face([P(rn, b0, H + 0.006), P(Re, b0, H + 0.006), P(Re, b1, H + 0.006), P(rn, b1, H + 0.006)], COL)
    for j in range(na):
        b0, b1 = sect_c[j], sect_c[j + 1]
        kv.face([P(rn, b1, H - 0.31), P(Re, b1, H - 0.31), P(Re, b0, H - 0.31), P(rn, b0, H - 0.31)], COL)
        kv.face([P(Re, b0, H - 0.31), P(Re, b1, H - 0.31), P(Re, b1, H + 0.006), P(Re, b0, H + 0.006)], COL)
    b1 = sect_c[-1]
    kv.face([P(rn, b1, H - 0.31), P(Re, b1, H - 0.31), P(Re, b1, H + 0.006), P(rn, b1, H + 0.006)], COL)
    kv.face([P(rn, a0 + 0.006, zc(a0)), P(Re, a0 + 0.006, zc(a0)), P(Re, a0 + 0.006, H - 0.31), P(rn, a0 + 0.006, H - 0.31)], COL)
    # garde-corps extérieur : main courante hélicoïdale + barreaux
    rr = R - 0.07
    hm = 1.0
    pts = []
    for i in range(n + 1):
        a = i * da
        pts.append(Vector(P(rr, a, (i + 1) * r + hm if i < n else H + hm)))
    pal = [Vector(P(rr, a, H + hm)) for a in secteur]
    rail = pts + pal[1:]
    mrail = {"Cave": S["fer"], "Chateau": S["pierre"], "Jardin": S["fer"]}[act]
    for i in range(len(rail) - 1):
        kg.obox(rail[i], rail[i + 1], 0.09 if act != "Chateau" else 0.16, 0.07 if act != "Chateau" else 0.12, mrail)
    for i, p in enumerate(rail):
        zb = p.z - hm
        if act == "Chateau":
            kg.lathe((p.x, p.y, zb - 0.02), [(0.06, 0), (0.09, 0.15), (0.05, 0.4), (0.09, 0.7), (0.05, 0.9), (0.06, hm - 0.05)], S["pierre"], n=6)
        elif act == "Jardin" or i % 2 == 0:
            kg.box((p.x, p.y, zb + hm / 2 - 0.02), (0.05, 0.05, hm), S["fer"])
            if act == "Jardin" and i < len(rail) - 1:
                q = rail[i + 1]
                mid = (p + q) / 2
                kg.box((mid.x, mid.y, (zb + q.z - hm) / 2 + hm / 2 - 0.02), (0.03, 0.03, hm - 0.1), S["fer"])
    ob = k.finish(S["seed"] + 31, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 32, peinture=PEINT_OBJET)
    enfant(ob, kc, 0)
    enfant(ob, kv, 0)
    ob["sortie"] = "palier"
    return ob


# =================================================================== 4. mezzanine
def mezzanine(S, nom, Lx=8.0, D=6.0, Hd=5.0):
    """Mezzanine contre un mur (dos en y = 0), plancher de Lx × D m, dessus à Hd, sur poteaux ou colonnes côté vide ;
    garde-corps devant et à gauche, côté droit (+X Blender, -X Unity) ouvert pour l'arrivée d'un escalier ou d'une échelle."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    act = S["act"]
    rnd = random.Random(S["seed"] + 41)
    yf = -D
    if act == "Cave":
        # planches posées en travers, solives, poutre maîtresse sur poteaux, contrefiches
        npl = int(D / 0.3)
        for i in range(npl):
            y = -(i + 0.5) * D / npl
            k.box((rnd.uniform(-0.04, 0.04), y, Hd - 0.05), (Lx + rnd.uniform(-0.1, 0.05), D / npl - 0.03, 0.1), S["planche"], rot=rnd.uniform(-0.004, 0.004))
        for x in (-Lx / 2 + 0.2, -Lx / 6, Lx / 6, Lx / 2 - 0.2):
            k.box((x, -D / 2, Hd - 0.28), (0.22, D, 0.36), S["bois"])
        k.box((0, yf + 0.3, Hd - 0.65), (Lx, 0.35, 0.45), S["bois"])
        for x in (-Lx / 2 + 0.4, 0.0, Lx / 2 - 0.4):
            k.box((x, yf + 0.3, (Hd - 0.85) / 2), (0.34, 0.34, Hd - 0.85), S["bois"])
            k.box((x, yf + 0.3, 0.12), (0.6, 0.6, 0.24), S["pierre"])
            for sx in (-1, 1):
                if abs(x + sx * 1.4) < Lx / 2:
                    k.obox((x + sx * 0.12, yf + 0.3, Hd - 2.3), (x + sx * 1.4, yf + 0.3, Hd - 0.85), 0.18, 0.18, S["bois"])
        k.box((0, -0.15, Hd - 0.65), (Lx, 0.3, 0.45), S["bois"])
        kind = "bois"
    elif act == "Chateau":
        # dalle de pierre moulurée sur colonnes, dessus de parquet
        k.box((0, -D / 2, Hd - 0.35), (Lx, D, 0.7), S["pierre"], skip=("+z",))
        k.face([(-Lx / 2, -D, Hd), (Lx / 2, -D, Hd), (Lx / 2, 0, Hd), (-Lx / 2, 0, Hd)], S["parquet"])
        for z, dd, hh in ((Hd - 0.85, 0.25, 0.3), (Hd - 0.6, 0.12, 0.2)):
            k.box((0, yf - dd / 2 + 0.02, z), (Lx + 0.2, dd, hh), S["pierre"])
        for x in (-Lx / 2 + 0.5, 0.0, Lx / 2 - 0.5):
            k.lathe((x, yf + 0.5, 0), [(0.5, 0), (0.5, 0.3), (0.36, 0.45), (0.3, 0.6)], S["pierre"], n=12)
            k.cyl((x, yf + 0.5, 0.6), 0.28, Hd - 1.8, S["pierre"], n=12, caps=(False, False))
            k.lathe((x, yf + 0.5, Hd - 1.2), [(0.3, 0), (0.42, 0.2), (0.5, 0.3), (0.5, 0.5), (0.0, 0.5)], S["pierre"], n=12)
        kind = "balustre"
    else:
        # plancher de bois sur piles de moellons (estrade haute de jardin), lierre
        npl = int(Lx / 0.3)
        for i in range(npl):
            x = -Lx / 2 + (i + 0.5) * Lx / npl
            k.box((x, -D / 2 + rnd.uniform(-0.03, 0.03), Hd - 0.05), (Lx / npl - 0.03, D, 0.1), S["planche"])
        for y in (-0.3, -D / 2, yf + 0.3):
            k.box((0, y, Hd - 0.3), (Lx, 0.25, 0.4), S["bois"])
        for x in (-Lx / 2 + 0.45, Lx / 2 - 0.45):
            for y in (-0.45, yf + 0.45):
                k.box((x, y, (Hd - 0.5) / 2), (0.8, 0.8, Hd - 0.5), S["mur"])
                k.box((x, y, Hd - 0.45), (0.95, 0.95, 0.1), S["pierre"])
                if y < -1.0:
                    lierre_mur(k, S, x + 0.05, Hd - 0.55, 1.6, y=y - 0.4, l=0.3, seed=S["seed"] + int(x * 10))
        kind = "fer"
    e = 0.22 if kind == "balustre" else 0.115
    garde(kg, S, (-Lx / 2 + e, yf + e, Hd), (Lx / 2 - e, yf + e, Hd), kind=kind)
    garde(kg, S, (-Lx / 2 + e, -0.05, Hd), (-Lx / 2 + e, yf + e, Hd), kind=kind)
    k.empty("Accroche_Bord", (0, yf, Hd))
    ob = k.finish(S["seed"] + 41, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 42, peinture=PEINT_OBJET)
    return ob


# =================================================================== 5. coursive
def coursive(S, nom, Lx=8.0, D=2.5):
    """Coursive le long d'un mur (dos en y = 0), dessus à 0, profondeur D (joueurs seulement : le traqueur ne s'y pose pas),
    consoles ou contrefiches, garde-corps côté vide."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    act = S["act"]
    rnd = random.Random(S["seed"] + 51)
    if act == "Cave":
        npl = int(Lx / 0.32)
        for i in range(npl):
            x = -Lx / 2 + (i + 0.5) * Lx / npl
            k.box((x, -D / 2 + rnd.uniform(-0.04, 0.04), -0.06), (Lx / npl - 0.04, D + rnd.uniform(-0.12, 0.05), 0.12), S["planche"])
        for x in (-Lx / 2 + 0.4, -Lx / 6, Lx / 6, Lx / 2 - 0.4):
            k.box((x, -D / 2, -0.3), (0.22, D, 0.34), S["bois"])
            k.obox((x, -0.1, -2.0), (x, -D + 0.3, -0.45), 0.2, 0.2, S["bois"])
            k.box((x, -0.06, -1.95), (0.36, 0.12, 0.4), S["fer"])
        kind = "corde"
    elif act == "Chateau":
        k.box((0, -D / 2, -0.25), (Lx, D, 0.5), S["pierre"], skip=("+z",))
        k.face([(-Lx / 2, -D, 0), (Lx / 2, -D, 0), (Lx / 2, 0, 0), (-Lx / 2, 0, 0)], S["parquet"])
        k.box((0, -D - 0.08, -0.4), (Lx, 0.2, 0.25), S["pierre"])
        for x in (-Lx / 2 + 1.0, -Lx / 6, Lx / 6, Lx / 2 - 1.0):
            for j in range(3):
                k.box((x, -0.35 - j * 0.6, -0.75 - (2 - j) * 0.3), (0.55, 0.7, 0.5 + (2 - j) * 0.6), S["pierre"])
        kind = "balustre"
    else:
        npl = int(Lx / 0.3)
        for i in range(npl):
            x = -Lx / 2 + (i + 0.5) * Lx / npl
            k.box((x, -D / 2, -0.05), (Lx / npl - 0.03, D, 0.1), S["planche"])
        k.box((0, -0.2, -0.25), (Lx, 0.2, 0.3), S["bois"])
        k.box((0, -D + 0.2, -0.25), (Lx, 0.2, 0.3), S["bois"])
        for x in (-Lx / 2 + 0.5, 0.0, Lx / 2 - 0.5):
            k.obox((x, -0.04, -1.6), (x, -D + 0.25, -0.42), 0.08, 0.06, S["fer"], up=(1, 0, 0))
            k.box((x, -D / 2, -0.43), (0.08, D - 0.1, 0.06), S["fer"])
            k.box((x, -0.04, -1.0), (0.24, 0.08, 1.3), S["fer"])
        kind = "fer"
    e = 0.22 if kind == "balustre" else 0.115
    garde(kg, S, (-Lx / 2 + e, -D + e, 0), (Lx / 2 - e, -D + e, 0), kind=kind)
    k.empty("Accroche_Bord", (0, -D, 0))
    ob = k.finish(S["seed"] + 51, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 52, peinture=PEINT_OBJET)
    return ob


# =================================================================== 6. passerelle entre étages
def passerelle(S, nom, Lx=10.0, W=2.4):
    """Passerelle entre deux étages : tablier de Lx × W m, dessus à 0 (pivot au centre du dessus), appuis aux deux bouts
    (à poser sur deux planchers), garde-corps des deux côtés. 2.4 m de large : le traqueur n'y monte pas."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    act = S["act"]
    rnd = random.Random(S["seed"] + 61)
    if act == "Cave":
        n = int(Lx / 0.32)
        for i in range(n):
            x = -Lx / 2 + (i + 0.5) * Lx / n
            k.box((x, rnd.uniform(-0.05, 0.05), -0.06 + rnd.uniform(-0.02, 0.0)), (Lx / n - 0.04, W + rnd.uniform(-0.15, 0.1), 0.12),
                  S["planche"], rot=rnd.uniform(-0.03, 0.03))
        for sy in (-1, 1):
            k.box((0, sy * (W / 2 - 0.35), -0.32), (Lx + 0.6, 0.28, 0.4), S["bois"])
        for sx in (-1, 1):
            k.box((sx * (Lx / 2 - 0.1), 0, -0.6), (0.3, W + 0.2, 0.25), S["bois"])
        kind = "corde"
    elif act == "Chateau":
        # pont de pierre volant : arc surbaissé sous un tablier dallé
        nx = 12
        under = [(-Lx / 2 + Lx * i / nx, -2.2 + 1.5 * math.sin(math.pi * i / nx)) for i in range(nx + 1)]
        poly = [(x, z) for x, z in under] + [(Lx / 2, 0.0), (-Lx / 2, 0.0)]
        prism(k, list(reversed(poly)), -W / 2, W / 2, S["pierre"], axis="y")
        k.box((0, 0, 0.005), (Lx - 0.02, W - 0.05, 0.01), S["sol"], skip=("-z",))
        for sy in (-1, 1):
            k.box((0, sy * (W / 2 + 0.075), -0.25), (Lx, 0.14, 0.3), S["pierre"])
        kind = "balustre"
    else:
        n = int(Lx / 0.3)
        for i in range(n):
            x = -Lx / 2 + (i + 0.5) * Lx / n
            k.box((x, 0, -0.05), (Lx / n - 0.03, W, 0.1), S["planche"])
        for sy in (-1, 1):
            k.box((0, sy * (W / 2 - 0.1), -0.32), (Lx, 0.12, 0.44), S["fer"])
            for i in range(10):
                x0 = -Lx / 2 + i * Lx / 10
                k.obox((x0, sy * (W / 2 - 0.1), -0.52), (x0 + Lx / 10, sy * (W / 2 - 0.1), -0.12), 0.05, 0.04, S["fer"], up=(0, 1, 0))
        for i in range(6):
            x = -Lx / 2 + 0.6 + i * (Lx - 1.2) / 5
            k.box((x, 0, -0.56), (0.08, W, 0.06), S["fer"])
        kind = "fer"
    e = 0.22 if kind == "balustre" else 0.115
    for sy in (-1, 1):
        garde(kg, S, (-Lx / 2 + 0.1, sy * (W / 2 - e), 0), (Lx / 2 - 0.1, sy * (W / 2 - e), 0), kind=kind)
    k.empty("Appui_A", (-Lx / 2, 0, 0))
    k.empty("Appui_B", (Lx / 2, 0, 0))
    ob = k.finish(S["seed"] + 61, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 62, peinture=PEINT_OBJET)
    return ob


# =================================================================== 7. porte en hauteur (taille joueur)
def porte_haute(S, nom, w=8.0, h=10.0, t=1.0, z0=5.0):
    """Mur de w × h m percé d'une porte de joueur (2.4 × 3.6 m) dont le seuil est à z0 ; rebord de 3 × 1.2 m devant le
    seuil (dessus plat, on s'y hisse ou on s'y tracte) ; l'autre côté donne sur un étage haut."""
    k = L.K(nom)
    act = S["act"]
    ow = 2.4
    pointed = act == "Chateau"
    rise = 1.5 if pointed else ow / 2
    o = dict(cx=0.0, ow=ow, z0=z0, spring=z0 + 3.6 - rise, rise=rise, pointed=pointed, n=10)
    cf, cb = mur_perce(k, -w / 2, w / 2, h, 0.0, t, [o], S["mur"], S["pierre"])
    encadrement(k, cf[0], 0.0, 0.42, 0.22, S["pierre"])
    # rebord : dalle sur trois consoles
    k.box((0, -0.6 + 0.01, z0 - 0.15), (3.0, 1.22, 0.3), S["pierre"])
    for x in (-1.1, 0.0, 1.1):
        for j in range(3):
            k.box((x, -0.25 - j * 0.3, z0 - 0.45 - (2 - j) * 0.22), (0.4, 0.4, 0.3 + (2 - j) * 0.44), S["pierre"])
    if act == "Cave":
        k.box((0, -0.18, z0 + 3.75), (3.6, 0.4, 0.45), S["bois"])
    if act == "Jardin":
        # lierre : deux cartes plates qui pendent du haut du mur, loin de l'encadrement et du rebord
        lierre_mur(k, S, -3.1, h - 0.02, 5.4, l=0.34, seed=S["seed"] + 72)
        lierre_mur(k, S, 2.9, h - 0.02, 7.2, l=0.3, seed=S["seed"] + 73)
    k.empty("Accroche_Bord", (0, -1.2, z0))
    return k.finish(S["seed"] + 71, PEINT_ARCHI)


# =================================================================== 8. porte étroite (le traqueur se faufile)
def porte_etroite(S, nom, w=12.0, h=10.0, t=1.5):
    """Porte étroite au sol : ouverture de 4.4 m de large qui contient un rectangle de 4 × 5.25 m (gabarit du traqueur
    baissé, `narrowDoorWidth` 4, `narrowDoorHeight` 5.2 plus 5 cm de marge) : il s'y faufile (vitesse × 0.45) ; le joueur passe à l'aise."""
    k = L.K(nom)
    act = S["act"]
    ow = 4.4
    if act == "Chateau":
        rise, pointed = 2.6, True
    else:
        rise, pointed = 2.2, False
    # naissance calculée pour que le rectangle de 4 × 5.25 m tienne sous l'arc
    prof = L.arch_profile(ow, rise, 14, pointed)
    def haut_a(xq):
        for i in range(len(prof) - 1):
            (xa, za), (xb, zb) = prof[i], prof[i + 1]
            if xa <= xq <= xb:
                return za + (zb - za) * (xq - xa) / (xb - xa)
        return 0.0
    spring = 5.25 - haut_a(2.0)
    o = dict(cx=0.0, ow=ow, z0=0.0, spring=spring, rise=rise, pointed=pointed, n=14)
    if act == "Jardin":
        w, h, t = 8.0, 9.0, 1.2
    cf, cb = mur_perce(k, -w / 2, w / 2, h, 0.0, t, [o], S["mur"], S["pierre"])
    encadrement(k, cf[0], 0.0, 0.7, 0.3, S["pierre"])
    if act == "Cave":
        rnd = random.Random(S["seed"] + 81)
        for j in range(4):
            L.rock_blob(k, (rnd.choice([-1, 1]) * rnd.uniform(3.2, 5.5), rnd.uniform(-1.4, -0.6), 0 + dz()), (1.0, 0.9, 0.7), S["mur"], seed=S["seed"] + 82 + j, res=8)
    elif act == "Jardin":
        k.box((0, 0.5 * t - 0.0, h + 0.15), (w + 0.2, t + 0.6, 0.3), S["pierre"])
        lierre_mur(k, S, -3.4, h - 0.02, 5.6, l=0.32, seed=S["seed"] + 82)
        lierre_mur(k, S, 3.45, h - 0.02, 6.9, l=0.28, seed=S["seed"] + 83)
    else:
        corniche_simple(k, -w / 2, w / 2, h - 0.9, S["pierre"])
    k.empty("Accroche_PorteEtroite", (0, t / 2, 0))
    ob = k.finish(S["seed"] + 81, PEINT_ARCHI)
    ob["gabarit"] = round(spring + haut_a(2.0), 2)
    return ob


def corniche_simple(k, x0, x1, z, m, y=0.0, d=0.6):
    k.box(((x0 + x1) / 2, y - d * 0.25 + 0.02, z), (x1 - x0, d * 0.5, 0.3), m)
    k.box(((x0 + x1) / 2, y - d * 0.5 + 0.02, z + 0.3), (x1 - x0, d, 0.25), m)


# =================================================================== 9. trémie (trou dans un plancher)
def tremie(S, nom, A=8.0, B=4.0, ep=0.6):
    """Dalle de plancher de A × A m (dessus à 0, pivot au centre du dessus) percée d'une trémie de B × B m au centre :
    bords plats et nets (aucun rebord où s'accrocher) ; on y tombe ou on y remonte (traction, échelle)."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    act = S["act"]
    a, b = A / 2, B / 2
    bord = b + (0.45 if act == "Chateau" else 0.3)
    def anneau(r0, r1, z, m):
        c0 = [(-r0, -r0), (r0, -r0), (r0, r0), (-r0, r0)]
        c1 = [(-r1, -r1), (r1, -r1), (r1, r1), (-r1, r1)]
        for i in range(4):
            j = (i + 1) % 4
            k.face([(c0[i][0], c0[i][1], z), (c0[j][0], c0[j][1], z), (c1[j][0], c1[j][1], z), (c1[i][0], c1[i][1], z)], m)
    top_m = {"Cave": S["planche"], "Chateau": S["sol"], "Jardin": S["sol"]}[act]
    bord_m = {"Cave": S["bois"], "Chateau": S.get("marbre"), "Jardin": S["pierre"]}[act]
    anneau(b, bord, 0.0, bord_m)
    anneau(bord, a, 0.0, top_m)
    anneau(b, a, -ep, S["mur"] if act != "Chateau" else S["pierre"])
    for (p, q) in (((-a, -a), (a, -a)), ((a, -a), (a, a)), ((a, a), (-a, a)), ((-a, a), (-a, -a))):
        k.face([(p[0], p[1], -ep), (q[0], q[1], -ep), (q[0], q[1], 0), (p[0], p[1], 0)], S["mur"] if act != "Chateau" else S["pierre"])
    for (p, q) in (((-b, -b), (b, -b)), ((b, -b), (b, b)), ((b, b), (-b, b)), ((-b, b), (-b, -b))):
        k.face([(p[0], p[1], 0), (q[0], q[1], 0), (q[0], q[1], -ep), (p[0], p[1], -ep)], bord_m)
    if act == "Cave":
        for x in (-a + 0.3, -b - 0.2, b + 0.2, a - 0.3):
            k.box((x, 0, -ep - 0.2), (0.3, A, 0.4), S["bois"])
    elif act == "Chateau":
        # balustrade sur trois côtés, le quatrième (avant, -Y) ouvert pour descendre ou remonter
        bb = b + 0.25
        garde(kg, S, (-bb, -bb + 1.2, 0), (-bb, bb, 0), kind="balustre")
        garde(kg, S, (-bb, bb, 0), (bb, bb, 0), kind="balustre")
        garde(kg, S, (bb, bb, 0), (bb, -bb + 1.2, 0), kind="balustre")
    k.empty("Accroche_Bord", (0, -b, 0))
    ob = k.finish(S["seed"] + 91, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 92, peinture=PEINT_OBJET)
    return ob


# =================================================================== 10. échelle
def echelle(S, nom, H=6.0):
    """Échelle verticale de H m contre un mur (dos du mur en y = 0), barreaux tous les 30 cm, montants qui dépassent de
    1 m en haut (prise pour se hisser) ; repères Accroche_Bas et Accroche_Haut."""
    k = L.K(nom)
    act = S["act"]
    y = -0.22
    if act == "Chateau":
        # échelons de fer scellés en U (échelle d'entretien des combles)
        z = 0.3
        while z <= H + 0.01:
            k.box((0, y, z), (0.5, 0.04, 0.04), S["fer"])
            for sx in (-1, 1):
                k.box((sx * 0.25, y / 2, z), (0.04, abs(y) + 0.05, 0.04), S["fer"])
            z += 0.3
        for sx in (-1, 1):
            k.obox((sx * 0.25, 0.02, H), (sx * 0.25, y, H + 0.25), 0.045, 0.045, S["fer"])
            k.obox((sx * 0.25, y, H + 0.25), (sx * 0.25, y, H + 1.0), 0.045, 0.045, S["fer"])
            k.obox((sx * 0.25, y, H + 1.0), (sx * 0.25, 0.02, H + 1.1), 0.045, 0.045, S["fer"])
            for zz in (0.6, H / 2, H - 0.3):
                k.box((sx * 0.25, -0.01, zz), (0.1, 0.04, 0.16), S["fer"])
    else:
        w0, w1 = (0.62, 0.62) if act == "Cave" else (0.78, 0.5)
        ht = H + 1.0
        for sx in (-1, 1):
            k.obox((sx * w0 / 2, y, 0), (sx * w1 / 2, y, ht), 0.09, 0.07, S["bois"], up=(0, 1, 0))
        z = 0.3
        rnd = random.Random(S["seed"] + 101)
        while z <= H + 0.01:
            u = z / ht
            ww = w0 + (w1 - w0) * u
            k.cyl((-ww / 2 + 0.03, y, z), 0.025, ww - 0.06, S["bois"], n=6, axis="x")
            z += 0.3
        if act == "Cave":
            for z in (1.2, 3.3):
                k.cyl((-w0 / 2 - 0.02, y, z - 0.08), 0.06, 0.1, S["corde"], n=6, axis="z")
        for sx in (-1, 1):
            k.box((sx * w1 / 2, -0.05, H + 0.2), (0.06, 0.12, 0.06), S["fer"])
    k.empty("Accroche_Bas", (0, y, 0.3))
    k.empty("Accroche_Haut", (0, y, H))
    return k.finish(S["seed"] + 101, PEINT_OBJET)


# =================================================================== 11. mur à rebords (wall jumps simples)
def mur_rebords(S, nom, w=6.0, h=10.0):
    """Mur de w × h m avec quatre rebords plats en quinconce (2.2 m d'écart vertical, 2.2 × 1 m, dessus à 2.2, 4.4, 6.6 et
    8.8 m) : on monte de rebord en rebord en sautant (hissage) ou en wall jump depuis un mur d'en face (à 3–6 m)."""
    k = L.K(nom)
    act = S["act"]
    rnd = random.Random(S["seed"] + 111)
    t = 1.0
    if act == "Cave":
        L.rock_wall(k, [(-w / 2, 0.15), (-w / 6, -0.05), (w / 6, 0.1), (w / 2, 0.0)], h, S["roche"], thick=1.6, seg=0.8, amp=0.22,
                    seed=S["seed"] + 112, inward=-1, base_flare=0.4, top_jag=0.5)
    else:
        k.box((0, t / 2, h / 2), (w, t, h), S["mur"])
        if act == "Chateau":
            for sx in (-1, 1):
                k.box((sx * (w / 2 - 0.3), -0.1, h / 2), (0.6, 0.24, h), S["pierre"])
            corniche_simple(k, -w / 2, w / 2, h - 0.6, S["pierre"])
            k.box((0, -0.06, 0.25), (w, 0.16, 0.5), S["pierre"])
        else:
            k.box((0, t / 2, h + 0.12), (w + 0.1, t + 0.3, 0.24), S["pierre"])
    for i, zt in enumerate((2.2, 4.4, 6.6, 8.8)):
        x = -1.4 if i % 2 == 0 else 1.4
        dep = 1.0
        if act == "Cave":
            k.box((x, -dep / 2 + 0.25, zt - 0.2), (2.2, dep + 0.5, 0.4), S["roche"], taper=0.0)
            L.rock_blob(k, (x, -0.2, zt - 1.3), (2.0, 1.2, 1.3), S["roche"], seed=S["seed"] + 113 + i, res=8, flat_base=False, amp=0.25)
        elif act == "Chateau":
            k.box((x, -dep / 2 + 0.01, zt - 0.14), (2.2, dep + 0.02, 0.28), S["pierre"])
            k.box((x, -dep - 0.03, zt - 0.36), (2.2, 0.1, 0.16), S["pierre"])
            for sx in (-0.7, 0.7):
                for j in range(3):
                    k.box((x + sx, -0.18 - j * 0.24, zt - 0.5 - (2 - j) * 0.2), (0.3, 0.3, 0.26 + (2 - j) * 0.4), S["pierre"])
        else:
            k.box((x, -dep / 2 + 0.01, zt - 0.15), (2.2 + rnd.uniform(-0.1, 0.1), dep + 0.02, 0.3), S["pierre"], rot=rnd.uniform(-0.02, 0.02))
            for sx in (-0.75, 0.75):
                k.obox((x + sx, 0.0, zt - 1.0), (x + sx, -dep + 0.2, zt - 0.32), 0.1, 0.08, S["fer"], up=(1, 0, 0))
        k.empty(f"Accroche_Rebord_{i + 1}", (x, -dep, zt))
    if act == "Jardin":
        # deux cartes de lierre hors des rebords et de leurs consoles
        lierre_mur(k, S, -2.62, h - 0.02, 6.75, l=0.26, seed=S["seed"] + 120)
        lierre_mur(k, S, 2.62, 8.45, 4.7, l=0.26, seed=S["seed"] + 121)
    return k.finish(S["seed"] + 111, PEINT_ARCHI)


# =================================================================== 12. mur escaladable
def mur_escalade(S, nom, w=8.0, h=12.0):
    """Mur escaladable de w × h m : prises plates en quinconce tous les 2 m de haut (0.7 m de profondeur : on s'y hisse
    d'une prise à l'autre), arête du haut nette où se hisser ; type `mur_grimpe` pour le traqueur (16 m au plus) et les
    goules (Accroche_Pied, Accroche_Sommet). Caves et château (relecture du lot 3) : les prises font partie de l'appareil
    (boutisses et abouts de poutre aux caves, pierres d'attente chanfreinées dessous au château), arêtes usées, pierres
    voisines de la même assise légèrement en saillie, joints creux entre elles."""
    k = L.K(nom)
    act = S["act"]
    rnd = random.Random(S["seed"] + 131)
    rp = random.Random(S["seed"] + 137)          # forme des pierres (n'altère pas la grille des prises)
    t = 1.0
    if act == "Cave":
        k.box((0, t / 2, h / 2), (w, t, h), S["mur"])
        k.box((0, t / 2, h + 0.15), (w + 0.2, t + 0.4, 0.3), S["pierre"])
    elif act == "Chateau":
        k.box((0, t / 2, h / 2), (w, t, h), S["mur"])
        corniche_simple(k, -w / 2, w / 2, h - 0.55, S["pierre"])
    else:
        L.rock_wall(k, [(-w / 2, 0.1), (-w / 4, -0.1), (w / 4, 0.12), (w / 2, 0.0)], h, S["roche"], thick=1.8, seg=0.8, amp=0.25,
                    seed=S["seed"] + 132, inward=-1, base_flare=0.5, top_jag=0.3)
        k.face([(-w / 2, 0.6, h + 0.25), (w / 2, 0.6, h + 0.25), (w / 2, 2.0, h + 0.25), (-w / 2, 2.0, h + 0.25)], S["herbe"])

    def voisines(x, z, lg, nb):
        """Pierres de la même assise que la prise (à gauche, à droite, au-dessus pour le château) : 3 cm de saillie,
        arêtes chanfreinées, joints de 3 cm."""
        g = 0.035
        hl = 0.44
        xa = x - lg / 2 - g
        wl = min(rp.uniform(0.55, 0.85), xa + w / 2 - 0.06)
        pierre(k, (xa - wl / 2, 0.015, z - hl / 2 - rp.uniform(0.0, 0.03)), (wl, 0.1, hl - rp.uniform(0.0, 0.05)), S["pierre"],
               ch=0.025, seed=rp.randint(0, 9999))
        xb = x + lg / 2 + g
        wr = min(rp.uniform(0.55, 0.85), w / 2 - 0.06 - xb)
        pierre(k, (xb + wr / 2, 0.015, z - hl / 2 - rp.uniform(0.0, 0.03)), (wr, 0.1, hl - rp.uniform(0.0, 0.05)), S["pierre"],
               ch=0.025, seed=rp.randint(0, 9999))
        if nb > 2:
            wh = rp.uniform(0.7, 1.0)
            pierre(k, (x + rp.uniform(-0.15, 0.15), 0.015, z + g + 0.19), (wh, 0.1, 0.38), S["pierre"], ch=0.025, seed=rp.randint(0, 9999))

    rang = 0
    z = 1.8
    while z < h - 0.5:
        xs = [-2.7, 0.0, 2.7] if rang % 2 == 0 else [-1.35, 1.35]
        for x in xs:
            x += rnd.uniform(-0.25, 0.25)
            if act == "Cave":
                if rnd.random() < 0.5:
                    # about de poutre engagé dans le mur : 0.6 m de large, 0.75 m de saillie, arêtes chanfreinées
                    rnd.uniform(-0.05, 0.05)
                    pierre(k, (x, -0.3, z - 0.19), (0.6, 1.35, 0.38), S["bois"], ch=0.04, seed=rp.randint(0, 9999), usure=0.015)
                    voisines(x, z, 0.6, 2)
                else:
                    # boutisse de moellon : 1 m de large, 0.75 m de saillie, dessous chanfreiné, arêtes usées
                    pierre(k, (x, -0.25, z - 0.21), (1.0, 1.25, 0.42), S["pierre"], ch=0.035, bas=(0.3, 0.16),
                           seed=rp.randint(0, 9999), usure=0.02)
                    voisines(x, z, 1.0, 2)
            elif act == "Chateau":
                # pierre d'attente (harpe) : boutisse de 1.1 m, 0.7 m de saillie, fort chanfrein dessous (console), arête
                # du dessus à peine adoucie
                pierre(k, (x, -0.3, z - 0.22), (1.1, 1.3, 0.44), S["pierre"], ch=0.025, bas=(0.4, 0.22), seed=rp.randint(0, 9999),
                       usure=0.008)
                voisines(x, z, 1.1, 3)
            else:
                k.box((x, -0.35, z - 0.2), (1.0, 1.5, 0.4), S["pierre"], rot=rnd.uniform(-0.08, 0.08))
        rang += 1
        z += 2.0
    if act == "Jardin":
        for j in range(4):
            x0 = rnd.uniform(-3, 3)
            L.tube(k, [(x0, 0.0, h + 0.2), (x0 + rnd.uniform(-0.6, 0.6), -0.5, h * 0.6), (x0 + rnd.uniform(-1, 1), -0.3, h * 0.25), (x0, -0.6, 0.0)],
                   [0.16, 0.12, 0.09, 0.05], S["ecorce"], sides=6, rough=0.2, seed=j)
    k.empty("Accroche_Pied", (0, -0.6, 0))
    k.empty("Accroche_Sommet", (0, -0.6, h))
    return k.finish(S["seed"] + 131, PEINT_ARCHI)


# =================================================================== portes à parties mobiles
def porte_verrouillee(S, nom):
    """Porte de la salle payante (verrouillée) : ouverture **carrée** de 3 × 3 m dans un mur de 6 × 6.6 × 1 m, **chambranle
    de porte** mouluré d'un seul tenant (deux piédroits et le linteau, à deux ressauts, sur des socles), clef portant le
    sceau d'âme ; **vantail de fer riveté** qui coulisse vers le haut dans une rainure du mur (guillotine, `Vantail`),
    coulisses de fer dans le tableau, **sceau d'âme** rond (flamme à trois langues, `Serrure`) qui tourne d'un quart de
    tour avant l'ouverture. Rien de commun avec la porte de sortie (arc, deux battants de bois, lune).
    Relecture du lot 3 : plus de linteau à corniche (elle se lisait comme une cheminée) ; la rainure (y de 0.37 à 0.63,
    x ±1.68, jusqu'à 6.45 m) contient le vantail et la serrure sur toute leur course."""
    k = L.K(nom)
    act = S["act"]
    w, h, t = 6.0, 6.6, 1.0
    o = dict(cx=0.0, ow=3.0, z0=0.0, spring=3.0, rise=0.0)
    ya, yb, xs, zs = 0.37, 0.63, 1.68, 6.45
    mur_rainure(k, -w / 2, w / 2, h, t, o, S["mur"], S["pierre"], ya, yb, xs, zs)
    mp = S["pierre"]
    m_or = S["fer"] if act != "Chateau" else S["dore"]
    # chambranle d'un seul tenant : profil en U (piédroits et linteau), deux ressauts ; il commence dans les socles
    def U(xi, zi, lg, z0, y0, y1):
        poly = [(-xi - lg, z0), (-xi, z0), (-xi, zi), (xi, zi), (xi, z0), (xi + lg, z0), (xi + lg, zi + lg), (-xi - lg, zi + lg)]
        prism(k, poly, y0, y1, mp, axis="y")
    U(1.56, 3.06, 0.52, 0.3, -0.17, 0.02)      # bandeau large, 17 cm de saillie
    U(1.53, 3.03, 0.2, 0.32, -0.27, -0.1)       # baguette intérieure, 27 cm de saillie
    for sx in (-1, 1):
        k.box((sx * 1.84, -0.15, 0.225 + dz()), (0.64, 0.38, 0.45), mp)       # socles des piédroits
    # clef : trapèze au milieu du linteau, porte le sceau d'âme
    clef = [(-0.36, 3.04), (0.36, 3.04), (0.46, 4.08), (-0.46, 4.08)]
    prism(k, clef, -0.34, 0.01, mp, axis="y")
    disque = [(math.cos(2 * math.pi * i / 16) * 0.31, 3.6 + math.sin(2 * math.pi * i / 16) * 0.31) for i in range(16)]
    prism(k, disque, -0.4, -0.33, m_or, axis="y")
    prism(k, flamme3(0.0, 3.37, 0.46), -0.43, -0.39, SCEAU_AME, axis="y")
    # coulisses de fer dans le tableau, devant la rainure (le vantail coulisse derrière)
    for sx in (-1, 1):
        k.box((sx * 1.48, 0.33, 1.49 + dz()), (0.05, 0.06, 2.98), S["fer"])
        k.box((sx * 1.48, 0.67, 1.49 + dz()), (0.05, 0.06, 2.98), S["fer"])
    if act == "Jardin":
        lierre_mur(k, S, -2.55, h - 0.02, 3.9, l=0.28, seed=S["seed"] + 152)
        lierre_mur(k, S, 2.6, h - 0.02, 4.6, l=0.26, seed=S["seed"] + 153)
    ob = k.finish(S["seed"] + 151, PEINT_ARCHI)
    # vantail : plaque de fer de 3.3 × 3.2 m dans la rainure (plaque de 0.45 à 0.55), bandes, montants et rivets devant
    kv = L.K("Vantail")
    yv = t / 2
    kv.box((0, yv, 1.62), (3.3, 0.1, 3.2), S["fer"])
    for z in (0.35, 0.85, 2.15, 2.82):
        kv.box((0, 0.435, z), (2.96, 0.04, 0.2), m_or)
        for i in range(9):
            if abs(-1.35 + i * 0.3375) > 0.6 or abs(z - 1.5) > 0.6:
                kv.box((-1.35 + i * 0.3375, 0.409, z), (0.07, 0.022, 0.07), S["fer"])
    for sx in (-1, 1):
        kv.box((sx * 1.2, 0.437, 1.62), (0.16, 0.034, 2.9), S["fer"])
    enfant(ob, kv, S["seed"] + 153, pivot=(0, yv, 0.02))
    # serrure : disque du sceau d'âme au milieu du vantail (tourne autour de l'axe de la porte), plaqué contre lui
    ks = L.K("Serrure")
    zs_ = 1.5
    disque = [(math.cos(2 * math.pi * i / 20) * 0.5, zs_ + math.sin(2 * math.pi * i / 20) * 0.5) for i in range(20)]
    prism(ks, disque, 0.405, 0.447, m_or, axis="y")
    prism(ks, flamme3(0.0, zs_ - 0.3, 0.62), 0.392, 0.408, SCEAU_AME, axis="y")
    for i in range(6):
        a = math.pi / 2 + i * math.pi / 3
        ks.cyl((math.cos(a) * 0.43, 0.392, zs_ + math.sin(a) * 0.43), 0.035, 0.016, S["fer"], n=6, axis="y")
    # enfant direct de la pièce (un enfant de second niveau sort mal orienté du FBX) : KitBuilder la range sous Vantail
    enfant(ob, ks, S["seed"] + 154, pivot=(0, yv - 0.15, zs_))
    return ob


def sceau_lune(kb, sx, cx, cz, R, y, m, m_ext):
    """Moitié (sx = -1 gauche, +1 droite) du sceau de sortie : anneau partagé par les deux battants, croissant à gauche,
    étoile à droite, huit rayons."""
    n = 16
    for i in range(n):
        a0 = math.pi / 2 + 2 * math.pi * i / n
        a1 = math.pi / 2 + 2 * math.pi * (i + 1) / n
        xm = math.cos((a0 + a1) / 2)
        if (xm < 0) != (sx < 0):
            continue
        pa = Vector((cx + math.cos(a0) * R, y, cz + math.sin(a0) * R))
        pb = Vector((cx + math.cos(a1) * R, y, cz + math.sin(a1) * R))
        for p_ in (pa, pb):
            if abs(p_.x - cx) < 1e-4:
                p_.x = cx + sx * 0.012
        kb.obox(pa, pb, 0.22, 0.1, m, up=(0, 1, 0))
    for i in range(8):
        a = math.pi / 8 + i * math.pi / 4
        if (math.cos(a) < 0) != (sx < 0):
            continue
        pa = Vector((cx + math.cos(a) * (R + 0.2), y, cz + math.sin(a) * (R + 0.2)))
        pb = Vector((cx + math.cos(a) * (R + 0.65), y, cz + math.sin(a) * (R + 0.65)))
        kb.obox(pa, pb, 0.16, 0.08, m, up=(0, 1, 0))
    if sx < 0:
        prism(kb, croissant(cx - R * 0.42, cz, R * 0.42, rot=0.5), y - 0.06, y + 0.05, m_ext, axis="y")
    else:
        prism(kb, etoile(cx + R * 0.45, cz + R * 0.05, R * 0.33), y - 0.06, y + 0.05, m_ext, axis="y")


def battants(ob, S, ow, spring, rise, pointed, seed, ep=0.35, y=0.0, z0=0.21, sceau_z=None, m_planche=None, panneaux=False, n=14):
    """Deux battants (Battant_G à -X Blender = gauche vu de face, Battant_D à +X) qui remplissent une ouverture en arc ;
    pivots aux gonds (x = ±ow/2, y), géométrie au-dessus du seuil z0 ; sceau de lune de sortie en fer sur les deux battants.
    Relecture du lot 3 : `y` est le plan des gonds, posé 1 cm derrière la face arrière de la maçonnerie ; le battant
    occupe y … y + ep (vers l'arrière) et s'ouvre vers l'arrière : rien ne traverse les piédroits pendant la course. Le
    haut des planches suit l'intrados à `n` segments (le même que l'arche), 5 cm dessous."""
    prof = L.arch_profile(ow, rise, n, pointed)
    y = y + ep / 2           # plan médian du battant
    def haut(x):
        for i in range(len(prof) - 1):
            (xa, za), (xb, zb) = prof[i], prof[i + 1]
            if xa - 1e-6 <= x <= xb + 1e-6:
                return spring + za + (zb - za) * (x - xa) / max(xb - xa, 1e-9)
        return spring
    m_pl = m_planche or S["porte"]
    res = []
    hw = ow / 2
    for side, sx in (("G", -1), ("D", 1)):
        kb = L.K(f"Battant_{side}")
        npl = 8
        for i in range(npl):
            x0 = sx * (i * hw / npl + 0.01)
            x1 = sx * ((i + 1) * hw / npl - 0.01)
            # planche à dessus taillé en suivant l'arc (corde), 5 cm sous l'intrados
            xs = [x0 + (x1 - x0) * j / 3 for j in range(4)]
            poly = [(x0, z0), (x1, z0)] + [(x, haut(x) - 0.05) for x in reversed(xs)]
            if sx < 0:
                poly = list(reversed(poly))
            prism(kb, poly, y - ep / 2, y + ep / 2, m_pl, axis="y")
        if panneaux:
            kb.box((sx * hw / 2, y - ep / 2 - 0.03, 1.6), (hw - 0.8, 0.08, 2.2), m_pl)
            kb.box((sx * hw / 2, y - ep / 2 - 0.09, 1.6), (hw - 1.3, 0.06, 1.7), m_pl)
        for z in (1.15, 3.0):
            kb.box((sx * hw / 2, y - ep / 2 - 0.02, z), (hw - 0.2, 0.06, 0.28), S["fer"])
            for i in range(7):
                kb.box((sx * (0.3 + i * (hw - 0.6) / 6), y - ep / 2 - 0.06, z), (0.1, 0.05, 0.1), S["fer"])
        kb.cyl((sx * (hw * 0.18 + 0.3), y - ep / 2 - 0.06, 2.2), 0.28, 0.08, S["fer"], n=12, axis="y")
        sz = sceau_z if sceau_z is not None else spring * 0.6
        m_s = S["fer"] if S["act"] != "Chateau" else S["dore"]
        sceau_lune(kb, sx, 0.0, sz, min(1.8, hw * 0.45), y - ep / 2 - 0.04, m_s, m_s)
        b = enfant(ob, kb, seed + (1 if sx > 0 else 2), pivot=(sx * hw, y - ep / 2, 0), peinture=dict(sale_h=1.5, mousse=0.1))
        res.append(b)
    return res


def porte_sortie(S, nom):
    """Porte de sortie d'acte : **grande arche** (ouverture 8 × 10 m) dans une façade de 12 × 14 m, **deux battants de bois**
    (`Battant_G`, `Battant_D`) qui pivotent à leurs gonds, **sceau de lune** (anneau, croissant, étoile) en fer sur les
    battants. Rien de commun avec la porte verrouillée (carrée, fer, sceau d'âme)."""
    k = L.K(nom)
    act = S["act"]
    if act == "Chateau":
        L.masonry_arch(k, 8.0, 6.0, 4.0, 2.2, 2.2, S["pierre"], n=14, pointed=True, top=14.0, seed=S["seed"] + 161, arriere=False)
        for j in range(2):
            c = _contour(dict(cx=0.0, ow=8.0 + 1.0 * (j + 1), z0=0.0, spring=6.0, rise=4.0 + 0.5 * (j + 1), pointed=True, n=14))
            encadrement(k, c, -1.1, 0.5, 0.5 - 0.18 * j, S["pierre"])
        corniche_simple(k, -6.2, 6.2, 13.1, S["pierre"], y=-1.1)
        k.box((0, -1.4, 0.1), (11.0, 0.8, 0.2), S["marbre"])
        spring, rise, pointed = 6.0, 4.0, True
    else:
        L.masonry_arch(k, 8.0, 6.0, 4.0, 2.2, 2.2, S["pierre"], n=14, top=14.0, seed=S["seed"] + 161, arriere=False)
        k.box((0, 0, 0.1), (12.4, 3.2, 0.2), S["sol"])
        spring, rise, pointed = 6.0, 4.0, False
        if act == "Jardin":
            rnd = random.Random(S["seed"] + 163)
            for sx in (-1, 1):
                k.box((sx * 6.9, -0.2, 6.5), (1.6, 2.6, 13.0), S["mur"], taper=0.12)
                k.lathe((sx * 6.9, -0.2, 13.0), [(0.6, 0), (0.75, 0.3), (0.5, 0.8), (0.65, 1.0), (0.0, 1.3)], S["pierre"], n=10)
            # lierre : deux cartes plates sur la façade, entre l'arche et les contreforts
            lierre_mur(k, S, -5.45, 13.95, 8.6, y=-1.1, l=0.34, seed=S["seed"] + 164)
            lierre_mur(k, S, 5.5, 13.95, 10.2, y=-1.1, l=0.3, seed=S["seed"] + 165)
    ob = k.finish(S["seed"] + 161, dict(sale_h=3.0, mousse=0.4))
    z0 = 0.21 if act != "Chateau" else 0.02
    # battants contre la face arrière de l'arche (y = 1.1), gonds 1 cm derrière
    battants(ob, S, 8.0, spring, rise, pointed, S["seed"] + 165, y=1.11, z0=z0, sceau_z=5.4, panneaux=(act == "Chateau"))
    return ob


# =================================================================== puits (margelle lisse, eau, fond)
def puits(S, nom, R=1.6, Ri=1.1, hm=0.9, eau=-1.5, fond=-2.4):
    """Puits à margelle **lisse** (paroi droite, dessus arrondi en deux quarts de rond de 12 cm, aucune marche ni arête
    vive), conduit maçonné, **eau** à -1.5 m (M_Eau_Toon) et **fond** à -2.4 m (on en ressort d'un saut et d'un hissage :
    3.3 m) ; montants posés **au sol hors de la margelle** ; corde et seau dans l'enfant `Deco_SansCollision`.
    Relecture du lot 3 : l'eau est un enfant séparé `Deco_Eau` (KitBuilder ne lui met pas de collider) : on traverse sa
    surface et on descend jusqu'au fond, qui reste solide. Le sol de la salle est à percer sur Ø 3.2 m sous le puits."""
    k = L.K(nom)
    kd = L.K("Deco_SansCollision")
    ke = L.K("Deco_Eau")
    act = S["act"]
    n = 24
    q = 0.12
    k.lathe((0, 0, 0), [(R, 0.0), (R, hm - q)], S["mur"], n=n)
    prof = []
    for j in range(5):
        a = math.pi / 2 * j / 4
        prof.append((R - q + q * math.cos(a), hm - q + q * math.sin(a)))
    for j in range(5):
        a = math.pi / 2 + math.pi / 2 * j / 4
        prof.append((Ri + q + q * math.cos(a), hm - q + q * math.sin(a)))
    k.lathe((0, 0, 0), prof, S["pierre"], n=n)
    k.lathe((0, 0, 0), [(Ri, hm - q), (Ri, fond)], S["mur"], n=n)
    ke.face([(math.cos(2 * math.pi * i / n) * (Ri - 0.005), math.sin(2 * math.pi * i / n) * (Ri - 0.005), eau) for i in range(n)], "M_Eau_Toon")
    k.face([(math.cos(2 * math.pi * i / n) * Ri, math.sin(2 * math.pi * i / n) * Ri, fond) for i in reversed(range(n))], S.get("terre", "M_Bib_Terre"))
    rnd = random.Random(S["seed"] + 171)
    for j in range(5):
        a = rnd.uniform(0, 6.28)
        L.rock_blob(k, (math.cos(a) * 0.6, math.sin(a) * 0.6, fond + dz()), (0.5, 0.4, 0.3), S["mur"], seed=S["seed"] + 172 + j, res=6)
    xp = R + 0.35
    if act == "Cave":
        for sx in (-1, 1):
            k.box((sx * xp, 0, 0.2), (0.6, 0.6, 0.4), S["pierre"])
            k.box((sx * xp, 0, 0.4 + 1.7), (0.24, 0.24, 3.4), S["porte"])
        k.box((0, 0, 3.9), (2 * xp + 0.5, 0.28, 0.28), S["porte"])
        k.cyl((-0.7, 0, 3.35), 0.17, 1.4, S["porte"], n=10, axis="x")
        for sx in (-1, 1):
            k.obox((sx * xp, 0, 3.35), (sx * 0.7, 0, 3.35), 0.06, 0.06, S["fer"])
        k.obox((xp, 0, 3.35), (xp + 0.35, -0.1, 3.35), 0.05, 0.05, S["fer"])
        k.obox((xp + 0.35, -0.1, 3.35), (xp + 0.35, -0.1, 3.0), 0.05, 0.05, S["fer"])
        h_corde = 3.2
    else:
        # arceau de fer forgé : deux montants et un demi-cercle à volutes, poulie au sommet
        for sx in (-1, 1):
            k.box((sx * xp, 0, 0.2), (0.5, 0.5, 0.4), S["pierre"])
            k.box((sx * xp, 0, 0.4 + 1.3), (0.08, 0.08, 2.6), S["fer"])
        arc = [Vector((math.cos(math.pi * i / 12) * xp, 0, 3.0 + math.sin(math.pi * i / 12) * 1.1)) for i in range(13)]
        for i in range(12):
            k.obox(arc[i], arc[i + 1], 0.08, 0.08, S["fer"], up=(0, 1, 0))
        for sx in (-1, 1):
            vol = [Vector((sx * (xp - 0.05 - 0.35 * (1 - math.cos(t * 0.5))), 0, 2.6 + 0.45 * math.sin(t * 0.5))) for t in range(0, 8)]
            for i in range(len(vol) - 1):
                k.obox(vol[i], vol[i + 1], 0.04, 0.04, S["fer"], up=(0, 1, 0))
        k.cyl((0, -0.05, 3.85), 0.22, 0.1, S["fer"], n=12, axis="y")
        h_corde = 3.7
    kd.obox((0, 0, h_corde), (0, 0, 1.75), 0.035, 0.035, S["corde"])
    kd.lathe((0, 0, 1.35), [(0.13, 0.0), (0.16, 0.34), (0.145, 0.34), (0.12, 0.03)], S["bois"], n=12)
    kd.cyl((0, 0, 1.35), 0.13, 0.02, S["bois"], n=12)
    for z in (1.41, 1.62):
        kd.cyl((0, 0, z), 0.142 + (z - 1.35) * 0.06, 0.03, S["fer"], n=12, caps=(False, False))
    kd.obox((-0.15, 0, 1.69), (0, 0, 1.78), 0.02, 0.02, S["fer"])
    kd.obox((0.15, 0, 1.69), (0, 0, 1.78), 0.02, 0.02, S["fer"])
    ob = k.finish(S["seed"] + 171, dict(sale_h=1.0, mousse=0.6, sol=fond))
    enfant(ob, kd, S["seed"] + 178)
    enfant(ob, ke, 0, peinture=dict(sale_h=0.0, mousse=0.0))
    return ob


# =================================================================== cheminée de roche (acte I)
def cheminee(S, nom, lg=3.5, prof=4.0, h=16.0):
    """Cheminée de roche ouverte devant : deux parois parallèles à 3.5 m (wall jumps en zigzag) et un fond, 16 m de haut,
    taillée dans une paroi de 13 m ; parois peu bossues (wall slide), lèvre du haut nette."""
    k = L.K(nom)
    a = dict(thick=2.0, seg=0.85, amp=0.2, base_flare=0.3, top_jag=0.4)
    sd = S["seed"] + 181
    L.rock_wall(k, [(-lg / 2, -0.4), (-lg / 2 - 0.1, prof / 2), (-lg / 2, prof + 0.3)], h, S["roche"], seed=sd, inward=-1, **a)
    L.rock_wall(k, [(lg / 2, prof + 0.3), (lg / 2 + 0.1, prof / 2), (lg / 2, -0.4)], h, S["roche"], seed=sd + 1, inward=-1, **a)
    L.rock_wall(k, [(-lg / 2 - 0.3, prof), (0, prof + 0.15), (lg / 2 + 0.3, prof)], h, S["roche"], seed=sd + 2, inward=-1, **a)
    b = dict(thick=2.5, seg=0.85, amp=0.55, base_flare=0.6, top_jag=0.8)
    L.rock_wall(k, [(-6.5, 0.4), (-4.0, -0.3), (-lg / 2 - 0.2, 0.0)], h, S["roche"], seed=sd + 3, inward=-1, **b)
    L.rock_wall(k, [(lg / 2 + 0.2, 0.0), (4.0, -0.3), (6.5, 0.4)], h, S["roche"], seed=sd + 4, inward=-1, **b)
    rnd = random.Random(sd)
    for j in range(4):
        L.rock_blob(k, (rnd.uniform(-1.2, 1.2), rnd.uniform(1.0, prof - 0.5), 0 + dz()), (1.1, 0.9, 0.6), S["roche"], seed=sd + 10 + j, res=8)
    k.empty("Accroche_Pied", (0, prof / 2, 0))
    k.empty("Accroche_Sommet", (0, prof / 2, h))
    return k.finish(sd, dict(sale_h=2.6, mousse=0.7))


# =================================================================== balcon de bois (acte I)
def balcon_bois(S, nom, Lx=3.2, D=1.8):
    """Balcon de planches en encorbellement (dessus à 0, dos au mur) sur deux contrefiches, garde-corps sur trois côtés :
    point de vue, cible de traction (Accroche_Traction)."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    rnd = random.Random(S["seed"] + 191)
    npl = int(Lx / 0.32)
    for i in range(npl):
        x = -Lx / 2 + (i + 0.5) * Lx / npl
        k.box((x, -D / 2 + rnd.uniform(-0.03, 0.03), -0.06), (Lx / npl - 0.04, D, 0.12), S["planche"])
    for x in (-Lx / 2 + 0.3, Lx / 2 - 0.3):
        k.box((x, -D / 2, -0.28), (0.22, D, 0.3), S["bois"])
        k.obox((x, -0.08, -1.7), (x, -D + 0.2, -0.43), 0.2, 0.2, S["bois"])
        k.box((x, -0.05, -1.65), (0.34, 0.1, 0.4), S["fer"])
    garde(kg, S, (-Lx / 2 + 0.1, -D + 0.1, 0), (Lx / 2 - 0.1, -D + 0.1, 0), kind="bois")
    garde(kg, S, (-Lx / 2 + 0.1, -0.1, 0), (-Lx / 2 + 0.1, -D + 0.1, 0), kind="bois", bout=False)
    garde(kg, S, (Lx / 2 - 0.1, -0.1, 0), (Lx / 2 - 0.1, -D + 0.1, 0), kind="bois", bout=False)
    k.empty("Accroche_Traction", (0, -D, 1.1))
    ob = k.finish(S["seed"] + 191, PEINT_ARCHI)
    enfant(ob, kg, S["seed"] + 192)
    return ob


# =================================================================== cellules d'apparition du traqueur
def herse(S, kh, lg, ht, y, style):
    """Herse de lg × ht m dans le plan y (barreaux carrés à pointe, traverses), base à z = 0."""
    n = int(round(lg / 0.88))
    xs = [-lg / 2 + 0.15 + i * (lg - 0.3) / n for i in range(n + 1)]
    for x in xs:
        kh.box((x, y, ht / 2 + 0.125), (0.24, 0.24, ht - 0.25), S["fer"])
        b = 0.25
        p = [(x - 0.12, y - 0.12, b), (x + 0.12, y - 0.12, b), (x + 0.12, y + 0.12, b), (x - 0.12, y + 0.12, b)]
        tip = (x, y, 0.0)
        for i in range(4):
            kh.face([p[i], p[(i + 1) % 4], tip], S["fer"])
    z = 1.0
    while z < ht - 0.2:
        kh.box((0, y - 0.2, z), (lg, 0.16, 0.3), S["fer"])
        z += 1.4
    kh.box((0, y, ht - 0.1), (lg, 0.3, 0.2), S["fer"])
    if style == "Chateau":
        # treillis en losanges (fer forgé) devant les barreaux, fleurons dorés en haut
        for i in range(-7, 8):
            for sgn in (-1, 1):
                a = Vector((i * 1.6, y + 0.22, 1.0))
                d = Vector((sgn * (ht - 2.0) * 0.55, 0, ht - 2.0))
                t0, t1 = 0.0, 1.0
                lim = lg / 2 - 0.15
                if abs(d.x) > 1e-6:
                    for L_ in (-lim, lim):
                        tt = (L_ - a.x) / d.x
                        if (d.x > 0) == (L_ < 0):
                            t0 = max(t0, tt)
                        else:
                            t1 = min(t1, tt)
                if t1 - t0 > 0.05:
                    kh.obox(a + d * t0, a + d * t1, 0.08, 0.06, S["fer"], up=(0, 1, 0))
        for x in xs[1:-1:2]:
            kh.lathe((x, y - 0.3, ht - 0.2), [(0.0, 0), (0.16, 0.05), (0.08, 0.3), (0.0, 0.55)], S["dore"], n=6)


def cellule_herse(S, nom):
    """Cellule d'apparition du traqueur à herse : façade de 18 × 21 m (2 m d'épaisseur), ouverture en arc de 10 × 11 m
    (10 × 12 m en arc brisé au château), cellule voûtée de 12 × 12 m derrière ; `Herse` de 10.6 m qui monte de 9.2 m dans le
    mur (le traqueur sort baissé). Chaînes et anneaux au fond."""
    k = L.K(nom)
    act = S["act"]
    W, Hf, t = 18.0, 21.0, 2.0
    ow = 10.0
    if act == "Chateau":
        o = dict(cx=0.0, ow=ow, z0=0.0, spring=7.0, rise=5.0, pointed=True, n=16)
        Hf = 22.5          # relecture du lot 3 : la herse levée (fleurons à 12.75 + 9.2 m) reste cachée dans la façade
    else:
        o = dict(cx=0.0, ow=ow, z0=0.0, spring=6.0, rise=5.0, pointed=False, n=16)
    crown = o["spring"] + o["rise"]
    # façade creusée d'une rainure (y de 0.48 à 1.32, x ±5.36) où la herse monte sans traverser la maçonnerie
    mur_rainure(k, -W / 2, W / 2, Hf, t, o, S["mur"], S["pierre"], 0.48, 1.32, 5.36,
                crown + 0.4 + (0.35 if act == "Chateau" else 0.0) + 9.2 + 0.12)
    encadrement(k, _contour(o), 0.0, 1.1, 0.45, S["pierre"])
    if act == "Chateau":
        c2 = _contour(dict(cx=0.0, ow=ow + 2.4, z0=0.0, spring=7.0, rise=5.6, pointed=True, n=16))
        encadrement(k, c2, 0.0, 0.6, 0.3, S["pierre"])
        corniche_simple(k, -W / 2, W / 2, Hf - 1.2, S["pierre"])
    # cellule : bloc maçonné fermé (murs latéraux et du fond jusqu'au haut de la façade, toit plat), berceau intérieur
    D, xi, hv = 12.0, 6.0, 12.0
    y0, y1 = t, t + D
    xe = xi + 1.5
    for sx in (-1, 1):
        k.face([(sx * xi, y0, 0), (sx * xi, y1, 0), (sx * xi, y1, hv), (sx * xi, y0, hv)], S["mur"])
        k.face([(sx * xe, y0, 0), (sx * xe, y1 + 1.0, 0), (sx * xe, y1 + 1.0, Hf), (sx * xe, y0, Hf)], S["mur"])
    k.face([(-xi, y1, 0), (xi, y1, 0), (xi, y1, Hf - 1.0), (-xi, y1, Hf - 1.0)], S["mur"])
    k.face([(-xe, y1 + 1.0, 0), (xe, y1 + 1.0, 0), (xe, y1 + 1.0, Hf), (-xe, y1 + 1.0, Hf)], S["mur"])
    k.face([(-xe, y0, Hf), (xe, y0, Hf), (xe, y1 + 1.0, Hf), (-xe, y1 + 1.0, Hf)], S["mur"])
    nv = 12
    pointe = act == "Chateau"
    def V(i, rr):
        a = math.pi * i / nv
        return -math.cos(a) * rr, hv + math.sin(a) * rr * (1.25 if pointe else 1.0)
    k.grid(lambda i, j: Vector((V(i, xi)[0], y0 + (y1 - y0) * j, V(i, xi)[1])), nv, 1, S["mur"])
    # arc-doubleau au fond de la voûte
    for i in range(nv):
        (xa, za), (xb, zb) = V(i, xi - 0.02), V(i + 1, xi - 0.02)
        k.obox((xa, y1 - 0.4, za), (xb, y1 - 0.4, zb), 0.7, 0.8, S["pierre"], up=(0, 1, 0))
    k.face([(-xi, y0, 0), (xi, y0, 0), (xi, y1, 0), (-xi, y1, 0)], S["sol"])
    for sx in (-1, 1):
        a = Vector((sx * 3.5, y1 - 0.15, 9.5))
        b = Vector((sx * 4.2, y1 - 1.6, 0.6))
        nl = 14
        d = (b - a).normalized()
        Ll = (b - a).length / nl
        for i in range(nl):
            p = a.lerp(b, (i + 0.5) / nl) + Vector((0, -0.6 * math.sin(math.pi * (i + 0.5) / nl), 0))
            if i % 2 == 0:
                k.obox(p - d * Ll * 0.6, p + d * Ll * 0.6, 0.32, 0.1, S["fer"])
            else:
                k.obox(p - d * Ll * 0.6, p + d * Ll * 0.6, 0.1, 0.32, S["fer"])
        k.box((sx * 3.5, y1 - 0.05, 9.7), (0.8, 0.12, 0.8), S["fer"])
        k.cyl((sx * 4.2, y1 - 1.6, 0.0), 0.75, 0.25, S["fer"], n=12)
    k.empty("Apparition_Traqueur", (0, y0 + D / 2, 0))
    k.empty("Porte_Traqueur", (0, t / 2, 0))
    ob = k.finish(S["seed"] + 201, dict(sale_h=3.0, mousse=0.4))
    kh = L.K("Herse")
    herse(S, kh, ow + 0.6, crown + 0.4, t / 2, act)
    enfant(ob, kh, S["seed"] + 202, pivot=(0, t / 2, 0))
    return ob


def cellule_barricade(S, nom):
    """Cellule d'apparition du traqueur dans la roche : bouche de caverne de 10 × 11 m, caverne de 13 × 13 m (14 m sous voûte),
    barricade de rondins à deux battants (`Battant_G`, `Battant_D`, 5 × 12 m chacun) qui s'ouvrent vers la salle."""
    k = L.K(nom)
    sd = S["seed"] + 211
    a = dict(thick=2.5, seg=0.95, amp=0.6, base_flare=0.6, top_jag=0.9)
    L.rock_wall(k, [(-12.0, 0.6), (-8.5, -0.4), (-5.0, 0.0)], 20, S["roche"], seed=sd, inward=-1, **a)
    L.rock_wall(k, [(5.0, 0.0), (8.5, -0.4), (12.0, 0.6)], 20, S["roche"], seed=sd + 1, inward=-1, **a)
    L.rock_wall(k, [(-5.6, 0.0), (0.0, -0.4), (5.6, 0.0)], 8, S["roche"], seed=sd + 2, inward=-1, dz=12.0,
                thick=3.0, seg=0.95, amp=0.45, base_flare=0.0, top_jag=0.9)
    L.rock_blob(k, (0, 1.3, 11.6), (11.5, 3.2, 2.0), S["roche"], seed=sd + 3, res=10, amp=0.2, flat_base=False)
    L.rock_wall(k, [(-5.0, 0.4), (-6.8, 5.0), (-5.8, 11.0), (0.0, 13.6), (5.8, 11.0), (6.8, 5.0), (5.0, 0.4)], 18, S["roche"],
                seed=sd + 4, inward=-1, thick=2.0, seg=1.0, amp=0.5, base_flare=0.6, top_jag=0.0)
    L.rock_blob(k, (0, 7.0, 16.2), (15.5, 16.0, 6.0), S["roche"], seed=sd + 5, res=12, amp=0.18, flat_base=False)
    rnd = random.Random(sd)
    # relecture du lot 3 : la roche de la bouche est repoussée derrière le plan y = -0.62 (|x| < 6.1 m, jusqu'à 12.8 m) :
    # les battants (y de -1.43 à -0.68) pivotent sans la traverser ; éboulis écartés de la course des battants
    for v in k.bm.verts:
        if abs(v.co.x) < 6.1 and v.co.z < 12.8 and v.co.y < -0.62:
            v.co.y = -0.62
    for j in range(6):
        L.rock_blob(k, (rnd.choice([-1, 1]) * rnd.uniform(7.3, 10), rnd.uniform(-2.2, -1.2), 0 + dz()), (rnd.uniform(1.0, 2.0), 1.2, 0.9),
                    S["roche"], seed=sd + 20 + j, res=8)
    k.empty("Apparition_Traqueur", (0, 7.0, 0))
    k.empty("Porte_Traqueur", (0, 0.0, 0))
    ob = k.finish(sd, dict(sale_h=3.0, mousse=0.6))
    for side, sx in (("G", -1), ("D", 1)):
        kb = L.K(f"Battant_{side}")
        yb = -0.9
        nl = 11
        for i in range(nl):
            x = sx * (0.25 + i * 4.5 / (nl - 1))
            hgt = 11.4 + rnd.uniform(-0.5, 0.3)
            kb.cyl((x, yb, 0.05), 0.22, hgt, S["bois"], n=7, rough=0.05, seed=i, caps=(True, False))
            kb.cyl((x, yb, 0.05 + hgt), 0.22, 0.45, S["bois"], r2=0.03, n=7, caps=(False, False))
        for z in (1.6, 5.8, 10.0):
            kb.box((sx * 2.52, yb - 0.32, z), (4.96, 0.3, 0.45), S["bois"])
        kb.obox((sx * 4.6, yb - 0.36, 1.6), (sx * 0.4, yb - 0.36, 5.8), 0.35, 0.22, S["bois"], up=(0, 1, 0))
        kb.obox((sx * 4.6, yb - 0.36, 5.8), (sx * 0.4, yb - 0.36, 10.0), 0.35, 0.22, S["bois"], up=(0, 1, 0))
        for z in (1.6, 5.8, 10.0):
            kb.box((sx * 4.6, yb - 0.5, z), (0.9, 0.06, 0.6), S["fer"])
        enfant(ob, kb, sd + 30 + sx, pivot=(sx * 5.0, yb, 0), peinture=dict(sale_h=1.5, mousse=0.2))
    return ob


def cellule_fosse(S, nom, c=12.0, prof=15.0):
    """Fosse d'apparition du traqueur : puits carré de 12 × 12 m, 15 m de profondeur (il y tient debout, caché), mur du fond
    à prises (`mur_grimpe` : il en sort en grimpant), margelle plate au ras du sol, deux trappes de grilles (`Trappe_G`,
    `Trappe_D`) qui se relèvent ; treuils aux angles. À poser dans un trou de 15 × 15 m du sol de la salle."""
    k = L.K(nom)
    sd = S["seed"] + 221
    a = c / 2
    b = a + 1.5
    for (p, q) in (((-a, -a), (a, -a)), ((a, -a), (a, a)), ((a, a), (-a, a)), ((-a, a), (-a, -a))):
        k.face([(p[0], p[1], -prof), (q[0], q[1], -prof), (q[0], q[1], 0), (p[0], p[1], 0)], S["mur"])
    k.face([(-a, -a, -prof), (-a, a, -prof), (a, a, -prof), (a, -a, -prof)], S["sol"])
    c0 = [(-a, -a), (a, -a), (a, a), (-a, a)]
    c1 = [(-b, -b), (b, -b), (b, b), (-b, b)]
    for i in range(4):
        j = (i + 1) % 4
        k.face([(c0[i][0], c0[i][1], 0), (c0[j][0], c0[j][1], 0), (c1[j][0], c1[j][1], 0), (c1[i][0], c1[i][1], 0)], S["pierre"])
    for sx in (-1, 1):
        k.box((sx * (a - 0.2), 0, -0.47), (0.4, c, 0.4), S["pierre"])
    rnd = random.Random(sd)
    z = -prof + 2.0
    rang = 0
    while z < -1.0:
        for x in ([-3.5, 0.0, 3.5] if rang % 2 == 0 else [-1.8, 1.8]):
            k.box((x + rnd.uniform(-0.3, 0.3), a - 0.5, z), (1.4, 1.2, 0.5), S["pierre"])
        z += 2.2
        rang += 1
    for j in range(8):
        L.rock_blob(k, (rnd.uniform(-5, 5), rnd.uniform(-5, 4), -prof + dz()), (1.2, 1.0, 0.7), S["mur"], seed=sd + j, res=8)
    for (x, y) in ((-b + 0.6, -b + 0.6), (b - 0.6, -b + 0.6)):
        k.box((x, y, 0.5), (0.9, 0.9, 1.0), S["pierre"])
        k.cyl((x - 0.45, y, 1.4), 0.3, 0.9, S["porte"], n=10, axis="x")
        for sx in (-1, 1):
            k.box((x + sx * 0.5, y, 1.25), (0.12, 0.5, 1.1), S["fer"])
    k.empty("Apparition_Traqueur", (0, 0, -prof))
    k.empty("Accroche_Pied", (0, a - 1.2, -prof))
    k.empty("Accroche_Sommet", (0, a - 1.2, 0))
    ob = k.finish(sd, dict(sale_h=4.0, mousse=0.5, sol=-prof))
    for side, sx in (("G", -1), ("D", 1)):
        kt = L.K(f"Trappe_{side}")
        x0, x1 = sorted((sx * 0.02, sx * (a - 0.02)))
        ep = 0.25
        for xx in (x0 + 0.1, x1 - 0.1):
            kt.box((xx, 0, -ep / 2), (0.2, c - 0.04, ep), S["fer"])
        for yy in (-a + 0.12, a - 0.12):
            kt.box(((x0 + x1) / 2, yy, -ep / 2), (x1 - x0 - 0.4, 0.2, ep), S["fer"])
        y = -a + 0.6
        while y < a - 0.5:
            kt.box(((x0 + x1) / 2, y, -ep / 2 + 0.03), (x1 - x0 - 0.4, 0.12, ep - 0.06), S["fer"])
            y += 0.6
        x = x0 + 0.7
        while x < x1 - 0.5:
            kt.box((x, 0, -ep / 2 - 0.02), (0.08, c - 0.44, ep - 0.1), S["fer"])
            x += 0.6
        for yy in (-3.0, 3.0):
            kt.box((sx * (a - 0.3), yy, 0.03), (0.5, 0.5, 0.06), S["fer"])
        enfant(ob, kt, sd + 40 + sx, pivot=(sx * (a - 0.02), 0, 0))
    return ob


def voliere(S, nom, R=8.0, hb=12.0, hd=20.0):
    """Cellule d'apparition du traqueur au jardin : grande volière de fer (Ø 16 m, 20 m sous le dôme) sur un socle de
    pierre, barreaux serrés ; portail de 8.5 × 11 m à deux vantaux (`Battant_G`, `Battant_D`) qui s'ouvrent vers le
    jardin."""
    k = L.K(nom)
    sd = S["seed"] + 231
    rnd = random.Random(sd)
    nseg = 32
    gate = math.radians(32)
    fa = -math.pi / 2
    def dans_porte(a, marge=0.0):
        d = (a - fa + math.pi) % (2 * math.pi) - math.pi
        return abs(d) < gate + marge
    for i in range(nseg):
        a0 = 2 * math.pi * i / nseg
        a1 = 2 * math.pi * (i + 1) / nseg
        if dans_porte((a0 + a1) / 2):
            continue
        p = [(math.cos(a) * rr, math.sin(a) * rr) for a, rr in ((a0, R - 0.5), (a1, R - 0.5), (a1, R + 0.5), (a0, R + 0.5))]
        prism(k, p, 0.0, 1.0, S["pierre"], axis="z")
    nr = 16
    def prof_r(z):
        if z <= hb:
            return R
        t = (z - hb) / (hd - hb)
        return R * math.sqrt(max(0.0, 1 - t * t))
    for i in range(nr):
        a = 2 * math.pi * i / nr + math.pi / nr
        zs = [hb, hb + 3, hb + 5.5, hb + 7, hd - 0.2]
        if not dans_porte(a):
            zs = [1.0, 4.0, 8.0] + zs
        pts = [Vector((math.cos(a) * prof_r(z), math.sin(a) * prof_r(z), z)) for z in zs]
        L.tube(k, pts, [0.16] * len(pts), S["fer"], sides=5, cap=False)
    for z in (1.15, 4.0, 8.0, hb, hb + 4.5):
        rr = prof_r(z)
        for i in range(nseg):
            a0 = 2 * math.pi * i / nseg
            a1 = 2 * math.pi * (i + 1) / nseg
            if z < hb - 0.1 and dans_porte((a0 + a1) / 2):
                continue
            k.obox((math.cos(a0) * rr, math.sin(a0) * rr, z), (math.cos(a1) * rr, math.sin(a1) * rr, z), 0.12, 0.16, S["fer"])
    nb = 96
    for i in range(nb):
        a = 2 * math.pi * (i + 0.5) / nb
        if dans_porte(a, 0.04) or rnd.random() < 0.04:
            continue
        x, y = math.cos(a) * R, math.sin(a) * R
        k.box((x, y, (1.0 + hb) / 2), (0.05, 0.05, hb - 1.0), S["fer"], rot=a)
    k.lathe((0, 0, hd - 0.3), [(0.5, 0), (0.6, 0.3), (0.2, 0.8), (0.35, 1.2), (0.0, 2.0)], S["fer"], n=8)
    xg = R * math.sin(gate)
    yg = -R * math.cos(gate)
    for sx in (-1, 1):
        k.box((sx * (xg + 0.25), yg, 6.0), (0.5, 0.5, 12.0), S["fer"])
        # socle du poteau hors de la course du vantail (relecture du lot 3)
        k.box((sx * (xg + 0.32), yg + 0.02, 0.5 + dz()), (0.62, 0.62, 1.0), S["pierre"])
    k.box((0, yg, 11.9), (2 * xg + 1.0, 0.45, 0.45), S["fer"])
    arc = [Vector((-xg + 2 * xg * i / 12, yg, 12.1 + 1.4 * math.sin(math.pi * i / 12))) for i in range(13)]
    for i in range(12):
        k.obox(arc[i], arc[i + 1], 0.14, 0.14, S["fer"], up=(0, 1, 0))
    k.empty("Apparition_Traqueur", (0, 0, 0))
    k.empty("Porte_Traqueur", (0, yg, 0))
    ob = k.finish(sd, dict(sale_h=1.5, mousse=0.6))
    for side, sx in (("G", -1), ("D", 1)):
        kb = L.K(f"Battant_{side}")
        x0 = sx * 0.03
        x1 = sx * (xg - 0.04)      # 4 cm de jeu au gond : le vantail ne frotte ni le poteau ni son socle
        yb = yg - 0.35
        for xx in (x0, x1):
            kb.box((xx - sx * 0.08 * (1 if xx == x1 else -1), yb, 5.6), (0.16, 0.16, 11.0), S["fer"])
        for z in (0.35, 4.0, 8.0, 11.0):
            kb.box(((x0 + x1) / 2, yb, z), (abs(x1 - x0), 0.14, 0.16), S["fer"])
        nbx = int(abs(x1 - x0) / 0.22)
        for i in range(1, nbx):
            xx = x0 + (x1 - x0) * i / nbx
            top = 11.0 + 0.6 * math.sin(math.pi * i / nbx)
            kb.box((xx, yb, (0.35 + top) / 2), (0.045, 0.045, top - 0.35), S["fer"])
            kb.face([(xx - 0.07, yb, top), (xx + 0.07, yb, top), (xx, yb, top + 0.25)], S["fer"])
        cx = (x0 + x1) / 2
        vol = [Vector((cx + 0.9 * math.cos(t / 6 * 2 * math.pi) * (1 - t / 14), yb - 0.1, 6.0 + 0.9 * math.sin(t / 6 * 2 * math.pi) * (1 - t / 14))) for t in range(13)]
        for i in range(12):
            kb.obox(vol[i], vol[i + 1], 0.06, 0.05, S["fer"], up=(0, 1, 0))
        enfant(ob, kb, sd + 50 + sx, pivot=(sx * xg, yb, 0))
    return ob


# =================================================================== fenêtres gothiques (acte II)
def _clip_z(poly, za, zb):
    def clip(pts, keep, z):
        out = []
        for i in range(len(pts)):
            p, q = pts[i], pts[(i + 1) % len(pts)]
            ip, iq = keep(p[1] - z), keep(q[1] - z)
            if ip:
                out.append(p)
            if ip != iq:
                t = (z - p[1]) / (q[1] - p[1])
                out.append((p[0] + t * (q[0] - p[0]), z))
        return out
    r = clip(poly, lambda d: d >= -1e-9, za)
    if len(r) < 3:
        return []
    r = clip(r, lambda d: d <= 1e-9, zb)
    return r if len(r) >= 3 else []


def _demi_largeur(prof, zr):
    """Demi-largeur d'un arc (profil arch_profile) à la hauteur zr au-dessus de sa naissance."""
    for q in range(len(prof) // 2 + 1):
        (xa, za), (xb, zb) = prof[q], prof[q + 1]
        if za - 1e-9 <= zr <= zb + 1e-9:
            return -(xa + (xb - xa) * (zr - za) / max(zb - za, 1e-9))
    return 0.0


def _clip_convexe(sub, clip):
    """Polygone `sub` découpé par le polygone convexe `clip` (Sutherland-Hodgman, sens quelconque)."""
    def aire(p):
        return 0.5 * sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p)))
    if aire(clip) < 0:
        clip = clip[::-1]
    r = list(sub)
    for i in range(len(clip)):
        a, b = clip[i], clip[(i + 1) % len(clip)]
        if not r:
            return []
        out = []
        for q in range(len(r)):
            p_, n_ = r[q], r[(q + 1) % len(r)]
            ip = (b[0] - a[0]) * (p_[1] - a[1]) - (b[1] - a[1]) * (p_[0] - a[0])
            iq = (b[0] - a[0]) * (n_[1] - a[1]) - (b[1] - a[1]) * (n_[0] - a[0])
            if ip >= 0:
                out.append(p_)
            if (ip >= 0) != (iq >= 0):
                t = ip / (ip - iq)
                out.append((p_[0] + t * (n_[0] - p_[0]), p_[1] + t * (n_[1] - p_[1])))
        r = out
    # sommets confondus retirés
    res = []
    for p_ in r:
        if not res or (abs(p_[0] - res[-1][0]) > 1e-4 or abs(p_[1] - res[-1][1]) > 1e-4):
            res.append(p_)
    if len(res) > 1 and abs(res[0][0] - res[-1][0]) < 1e-4 and abs(res[0][1] - res[-1][1]) < 1e-4:
        res.pop()
    return res if len(res) >= 3 and abs(aire(res)) > 1e-4 else []


def medaillon_z(sill, spring_l):
    return sill + (spring_l - sill) * 0.56


def vitrail(k, S, cg, yg, bw, nl, wl, mw, sill, spring_l, oculus):
    """Vitrail en panneaux au plomb (relecture du lot 3) : fond de plomb (fer) juste derrière le verre ; losanges sertis
    (jeu de 2.5 cm = filets de plomb) découpés dans la baie, en majorité bleu-gris sombre (`M_Kit_Vitrail`) ; bordure
    ponctuée de rouge et d'ambre ; un médaillon rond par lancette (couronne ambre, quartiers rouges, cœur ambre), posé
    1.2 cm devant les losanges ; rose : secteurs alternés rouge et ambre. Mêmes trois matériaux qu'avant."""
    bleu, rouge, ambre = VITRAUX
    k.face([(x, yg + 0.012, z) for x, z in cg], S["fer"])
    a = 0.2 if bw < 5.0 else 0.28
    b = a * 1.4
    g = 0.025
    zmin = min(p[1] for p in cg)
    zmax = max(p[1] for p in cg)
    rmed = min(0.42 * wl, 1.0)
    zm = medaillon_z(sill, spring_l)
    meds = [(-bw / 2 + wl / 2 + j * (wl + mw), zm) for j in range(nl)]
    def dist_bord(x, z):
        d = 1e9
        for q in range(len(cg)):
            (x1, z1), (x2, z2) = cg[q], cg[(q + 1) % len(cg)]
            vx, vz = x2 - x1, z2 - z1
            L2 = vx * vx + vz * vz
            t = 0 if L2 < 1e-12 else max(0.0, min(1.0, ((x - x1) * vx + (z - z1) * vz) / L2))
            d = min(d, math.hypot(x - x1 - t * vx, z - z1 - t * vz))
        return d
    jr = 0
    z = zmin
    while z < zmax + b:
        off = a if jr % 2 else 0.0
        ir = 0
        x = -bw / 2 - 2 * a + off
        while x < bw / 2 + 2 * a:
            if any(math.hypot(x - mx, z - mz) + max(a, b) < rmed - 0.02 for mx, mz in meds):
                x += 2 * a; ir += 1
                continue
            sa, sb = a - g, b - g * b / a
            poly = _clip_convexe([(x, z - sb), (x + sa, z), (x, z + sb), (x - sa, z)], cg)
            if poly:
                m = bleu
                if oculus and math.hypot(x, z - oculus[0]) < oculus[1]:
                    ang = math.atan2(z - oculus[0], x) - math.pi / 2
                    sect = int(math.floor((ang % (2 * math.pi)) / (2 * math.pi / oculus[2])))
                    m = ambre if math.hypot(x, z - oculus[0]) < 0.3 else (rouge if sect % 2 == 0 else ambre)
                elif dist_bord(x, z) < 0.3:
                    m = rouge if (ir + jr) % 4 == 0 else ambre if (ir + jr) % 4 == 2 else bleu
                elif (ir * 7 + jr * 3) % 13 == 0:
                    m = ambre
                k.face([(px, yg, pz) for px, pz in poly], m)
            x += 2 * a
            ir += 1
        z += b
        jr += 1
    # médaillons : couronne ambre (16 claveaux), quartiers rouges et bleus, cœur ambre ; plomb derrière
    yf = yg - 0.012
    for mx, mz in meds:
        n = 16
        k.face([(mx + math.cos(2 * math.pi * q / 24) * (rmed + 0.02), yf + 0.006, mz + math.sin(2 * math.pi * q / 24) * (rmed + 0.02))
                for q in range(24)], S["fer"])
        for q in range(n):
            t0 = 2 * math.pi * q / n + 0.012
            t1 = 2 * math.pi * (q + 1) / n - 0.012
            r0, r1 = rmed * 0.8, rmed - 0.012
            k.face([(mx + math.cos(t0) * r0, yf, mz + math.sin(t0) * r0), (mx + math.cos(t1) * r0, yf, mz + math.sin(t1) * r0),
                    (mx + math.cos(t1) * r1, yf, mz + math.sin(t1) * r1), (mx + math.cos(t0) * r1, yf, mz + math.sin(t0) * r1)],
                   ambre if q % 4 else rouge)
        nq = 8
        for q in range(nq):
            t0 = 2 * math.pi * q / nq + 0.03
            t1 = 2 * math.pi * (q + 1) / nq - 0.03
            r0, r1 = rmed * 0.26, rmed * 0.8 - 0.024
            arc = [(mx + math.cos(t0 + (t1 - t0) * u / 3) * r1, mz + math.sin(t0 + (t1 - t0) * u / 3) * r1) for u in range(4)]
            pts = [(mx + math.cos(t0) * r0, mz + math.sin(t0) * r0)] + arc + [(mx + math.cos(t1) * r0, mz + math.sin(t1) * r0)]
            k.face([(px, yf, pz) for px, pz in pts], rouge if q % 2 == 0 else bleu)
        k.face([(mx + math.cos(2 * math.pi * q / 10) * (rmed * 0.26 - 0.024), yf, mz + math.sin(2 * math.pi * q / 10) * (rmed * 0.26 - 0.024))
                for q in range(10)], ambre)


def fenetre(S, nom, w, h, bw, sill, top, nl, oculus=None):
    """Haute fenêtre gothique en lancettes dans un panneau de mur de w × h m (1 m d'épaisseur, comme les murs du kit) :
    baie en arc brisé, ébrasement intérieur de 0.45 m, remplage (meneaux, arcs des lancettes, oculus), vitrail au plomb
    (losanges bleu-gris sombre, bordure et médaillon rouge et ambre, rose en secteurs, voir `vitrail`) ; repère vide
    `RayonLune` au milieu du vitrage."""
    k = L.K(nom)
    t = 1.0
    rise = bw * 0.85
    o = dict(cx=0.0, ow=bw, z0=sill, spring=top - rise, rise=rise, pointed=True, n=16)
    cf, cb = mur_perce(k, -w / 2, w / 2, h, 0.0, t, [o], S["mur"], S["pierre"], ebr=0.45)
    encadrement(k, cf[0], 0.0, 0.3, 0.18, S["pierre"])
    k.box((0, -0.12 + 0.01, 0.25), (w, 0.35, 0.5), S["pierre"])
    corniche_simple(k, -w / 2, w / 2, h - 1.0, S["pierre"], d=0.7)
    yg = 0.72
    fr = yg / t
    cg = [(f[0] + (b[0] - f[0]) * fr, f[1] + (b[1] - f[1]) * fr) for f, b in zip(cf[0], cb[0])]
    mw = 0.22
    wl = (bw - (nl - 1) * mw) / nl
    if nl > 1:
        spring_l = o["spring"] + rise * 0.05
        rise_l = wl * 0.95
        prl = L.arch_profile(wl, rise_l, 10, True)
    else:
        spring_l, rise_l, prl = o["spring"], rise, L.arch_profile(bw, rise, 16, True)
    hreg = 0.9
    vitrail(k, S, cg, yg, bw, nl, wl, mw, sill, spring_l, oculus)
    zmed = [medaillon_z(sill, spring_l)]
    rmed = min(0.42 * wl, 1.0)
    for j in range(nl):
        cx = -bw / 2 + wl / 2 + j * (wl + mw)
        if nl > 1:
            for q in range(len(prl) - 1):
                (xa, za), (xb, zb) = prl[q], prl[q + 1]
                k.obox((cx + xa, yg, spring_l + za), (cx + xb, yg, spring_l + zb), 0.18, 0.32, S["pierre"], up=(0, 1, 0))
        # barlotières de fer (sauf à travers le médaillon)
        z = sill + hreg
        while z < spring_l + rise_l - 0.3:
            hwz = wl / 2 if z <= spring_l else _demi_largeur(prl, z - spring_l)
            if hwz > 0.15 and all(abs(z - zm) > rmed + 0.12 for zm in zmed):
                k.box((cx, yg, z), (2 * hwz, 0.1, 0.05), S["fer"])
            z += hreg
        if j < nl - 1:
            xm = cx + wl / 2 + mw / 2
            k.box((xm, yg, (sill + spring_l) / 2), (mw, 0.34, spring_l - sill), S["pierre"])
    if oculus:
        zc, rc, rayons = oculus
        anneau_obox(k, (0.0, zc), rc, 0.18, 0.34, S["pierre"], n=16, y=yg)
        for q in range(rayons):
            a = math.pi / 2 + 2 * math.pi * q / rayons
            k.obox((math.cos(a) * 0.15, yg, zc + math.sin(a) * 0.15), (math.cos(a) * (rc - 0.05), yg, zc + math.sin(a) * (rc - 0.05)),
                   0.1, 0.26, S["pierre"], up=(0, 1, 0))
        k.cyl((0, yg - 0.15, zc), 0.2, 0.3, S["pierre"], n=8, axis="y")
    k.empty("RayonLune", (0, yg, (sill + top) / 2))
    return k.finish(S["seed"] + 241 + nl, dict(sale_h=1.5, mousse=0.05))


# =================================================================== chandeliers, lustres, appliques (acte II)
def _maillons(k, a, b, n, s, m):
    a, b = Vector(a), Vector(b)
    d = (b - a).normalized()
    Ll = (b - a).length / n
    for i in range(n):
        p = a.lerp(b, (i + 0.5) / n)
        if i % 2 == 0:
            k.obox(p - d * Ll * 0.55, p + d * Ll * 0.55, s, s * 0.35, m)
        else:
            k.obox(p - d * Ll * 0.55, p + d * Ll * 0.55, s * 0.35, s, m)


def _bougie(k, S, p, i, h=0.22, r=0.035):
    x, y, z = p
    k.cyl((x, y, z), 0.075, 0.03, S["dore"], n=8)
    k.cyl((x, y, z + 0.03), r, h, "M_Kit_Cire", n=8)
    k.empty(f"Feu_{i}", (x, y, z + 0.03 + h + 0.04))


def chandelier(S, nom, D, chute, etages):
    """Chandelier suspendu de fer (couronnes, bras en S, bobèches dorées) de Ø D m, pivot au point d'accroche du plafond,
    bas à `chute` m sous le plafond ; bougies sur les couronnes (`Feu_1` …)."""
    k = L.K(nom)
    corps = 0.6 + 0.75 * (len(etages) - 1) + 0.3
    zb = -chute + 0.35          # la pointe basse du fût est à -chute
    zh = zb + corps
    k.lathe((0, 0, 0), [(0.0, 0.0), (0.25, -0.02), (0.22, -0.08), (0.0, -0.1)], S["fer"], n=10)
    _maillons(k, (0, 0, -0.1), (0, 0, zh), max(4, int(abs(zh + 0.1) / 0.16)), 0.07 if D < 3 else 0.1, S["fer"])
    k.lathe((0, 0, zb), [(0.0, -0.35), (0.09, -0.25), (0.06, -0.1), (0.12, 0.0), (0.08, 0.2), (0.1, corps * 0.5), (0.06, corps - 0.15),
                         (0.14, corps - 0.08), (0.0, corps)], S["fer"], n=10)
    nf = 1
    for j, (frac, nb) in enumerate(etages):
        rr = D / 2 * frac
        z = zb + 0.3 + 0.75 * j
        ring = [Vector((math.cos(2 * math.pi * q / 24) * rr, math.sin(2 * math.pi * q / 24) * rr, z)) for q in range(25)]
        for q in range(24):
            k.obox(ring[q], ring[q + 1], 0.07, 0.05 + 0.02 * (D > 3), S["fer"])
        for q in range(nb):
            a = 2 * math.pi * (q + 0.5 * (j % 2)) / nb
            c, s_ = math.cos(a), math.sin(a)
            pts = [Vector((c * rr * u, s_ * rr * u, z + 0.12 * math.sin(math.pi * u) - 0.04 * u)) for u in (0.05, 0.35, 0.7, 1.0)]
            L.tube(k, pts, [0.022] * 4, S["fer"], sides=6)
            _bougie(k, S, (c * rr, s_ * rr, z + 0.03), nf)
            nf += 1
        ns = 4 if D > 2.5 else 3
        for q in range(ns):
            a = 2 * math.pi * q / ns + 0.3
            k.obox((0, 0, zh - 0.05), (math.cos(a) * rr * 0.98, math.sin(a) * rr * 0.98, z + 0.03), 0.03, 0.03, S["fer"])
    return k.finish(S["seed"] + 251 + int(D * 10), dict(sale_h=0.0, mousse=0.0))


def lustre_cristal(S, nom, D=2.4, chute=3.0):
    """Lustre à pampilles (Ø 2.4 m) : trois étages de bras dorés (12, 8 et 4 bougies), gouttes de cristal (`M_Kit_Cristal`,
    relecture du lot 3 : elles étaient en verre de vitrail presque noir), pivot au point d'accroche du plafond, bas à 3 m
    sous le plafond."""
    k = L.K(nom)
    zb = -chute + 0.3           # la pointe basse est à -chute
    corps = 1.9
    zh = zb + corps
    k.lathe((0, 0, 0), [(0.0, 0.0), (0.3, -0.02), (0.26, -0.1), (0.0, -0.12)], S["dore"], n=12)
    _maillons(k, (0, 0, -0.12), (0, 0, zh), max(4, int(abs(zh + 0.12) / 0.14)), 0.07, S["dore"])
    k.lathe((0, 0, zb), [(0.0, -0.3), (0.12, -0.2), (0.22, 0.0), (0.12, 0.25), (0.08, 0.6), (0.2, 0.9), (0.08, 1.2), (0.16, 1.55),
                         (0.08, 1.75), (0.0, corps)], S["dore"], n=12)
    nf = 1
    gouttes = []
    for j, (rr, nb, z) in enumerate(((D / 2, 12, zb + 0.35), (D / 2 * 0.66, 8, zb + 0.95), (D / 2 * 0.36, 4, zb + 1.45))):
        ring = [Vector((math.cos(2 * math.pi * q / 24) * rr * 0.92, math.sin(2 * math.pi * q / 24) * rr * 0.92, z - 0.05)) for q in range(25)]
        for q in range(24):
            k.obox(ring[q], ring[q + 1], 0.045, 0.04, S["dore"])
        for q in range(nb):
            a = 2 * math.pi * (q + 0.5 * j) / nb
            c, s_ = math.cos(a), math.sin(a)
            pts = [Vector((c * rr * u, s_ * rr * u, z + 0.18 * math.sin(math.pi * u * 0.8) - 0.1 * u)) for u in (0.08, 0.4, 0.75, 1.0)]
            L.tube(k, pts, [0.024] * 4, S["dore"], sides=6)
            _bougie(k, S, (c * rr, s_ * rr, pts[-1].z), nf, h=0.18, r=0.03)
            nf += 1
        ng = 24 if j == 0 else 16
        for q in range(ng):
            a = 2 * math.pi * (q + 0.5) / ng
            gouttes.append((math.cos(a) * rr * 0.92, math.sin(a) * rr * 0.92, z - 0.12, 0.07 if q % 2 else 0.1))
    for x, y, z, s in gouttes:
        top, bot = (x, y, z + 0.06), (x, y, z - s * 2)
        mid = [(x + s * 0.5, y, z - s * 0.6), (x, y + s * 0.5, z - s * 0.6), (x - s * 0.5, y, z - s * 0.6), (x, y - s * 0.5, z - s * 0.6)]
        for q in range(4):
            k.face([top, mid[q], mid[(q + 1) % 4]], CRISTAL)
            k.face([bot, mid[(q + 1) % 4], mid[q]], CRISTAL)
    return k.finish(S["seed"] + 261, dict(sale_h=0.0, mousse=0.0))


def applique(S, nom, nb=2, miroir=False):
    """Applique murale à bougies (dos au mur en y = 0, pivot au point de scellement) : platine dorée, bras en S, bobèches,
    bougies (`Feu_1` …) ; version à miroir sombre ovale."""
    k = L.K(nom)
    if miroir:
        ov = [(math.cos(2 * math.pi * i / 20) * 0.32, 0.25 + math.sin(2 * math.pi * i / 20) * 0.45) for i in range(20)]
        prism(k, ov, -0.03, 0.0, VITRAUX[0], axis="y")
        pts = [Vector((math.cos(2 * math.pi * i / 20) * 0.36, -0.035, 0.25 + math.sin(2 * math.pi * i / 20) * 0.49)) for i in range(21)]
        for i in range(20):
            k.obox(pts[i], pts[i + 1], 0.07, 0.06, S["dore"], up=(0, 1, 0))
        k.lathe((0, -0.04, 0.72), [(0.0, 0), (0.08, 0.03), (0.05, 0.12), (0.0, 0.2)], S["dore"], n=6)
        base = (0, -0.06, -0.27)
        k.lathe((0, -0.04, -0.34), [(0.0, 0.0), (0.09, 0.05), (0.07, 0.12), (0.0, 0.14)], S["dore"], n=8)
    else:
        bou = [(0.0, 0.32), (0.12, 0.22), (0.11, 0.0), (0.05, -0.22), (0.0, -0.3), (-0.05, -0.22), (-0.11, 0.0), (-0.12, 0.22)]
        prism(k, bou, -0.04, 0.0, S["dore"], axis="y")
        base = (0, -0.04, -0.05)
    xs = [0.0] if nb == 1 else [(-0.3 + 0.6 * i / (nb - 1)) for i in range(nb)]
    for i, x in enumerate(xs):
        zc = base[2] + 0.18 + (0.08 if (nb == 3 and i == 1) else 0.0)
        pts = [Vector((x * u, base[1] - 0.32 * u, base[2] - 0.08 * math.sin(math.pi * u) + (zc - base[2]) * u * u)) for u in (0.0, 0.35, 0.7, 1.0)]
        L.tube(k, pts, [0.02] * 4, S["dore"], sides=6)
        _bougie(k, S, (pts[-1].x, pts[-1].y, pts[-1].z), i + 1, h=0.2, r=0.028)
    return k.finish(S["seed"] + 271 + nb, dict(sale_h=0.0, mousse=0.0))


def candelabre_pied(S, nom):
    """Candélabre sur pied de 2.1 m (pied de marbre, fût doré tourné), couronne de sept bougies (`Feu_1` … `Feu_7`)."""
    k = L.K(nom)
    k.lathe((0, 0, 0), [(0.42, 0.0), (0.42, 0.08), (0.3, 0.14), (0.24, 0.3), (0.16, 0.36)], S["pierre"], n=8)
    k.lathe((0, 0, 0.36), [(0.08, 0.0), (0.12, 0.1), (0.05, 0.3), (0.06, 0.8), (0.1, 0.9), (0.04, 1.2), (0.05, 1.45), (0.12, 1.52), (0.0, 1.56)],
            S["dore"], n=8)
    z = 1.92
    _bougie(k, S, (0, 0, z + 0.06), 1, h=0.26)
    for i in range(6):
        a = 2 * math.pi * i / 6
        p = [Vector((math.cos(a) * 0.32 * u, math.sin(a) * 0.32 * u, z - 0.1 * math.sin(math.pi * u) + 0.02 * u)) for u in (0.15, 0.55, 1.0)]
        L.tube(k, p, [0.02] * 3, S["dore"], sides=6)
        _bougie(k, S, (p[-1].x, p[-1].y, p[-1].z), i + 2, h=0.2)
    return k.finish(S["seed"] + 281, dict(sale_h=0.2, mousse=0.0))


# =================================================================== torches du château (relecture du lot 3)
def torche_murale(S, nom):
    """Torche murale du château : platine de fer forgé en losange étiré (dos contre le mur, plan y = 0, pivot au point de
    scellement), console en S et contrefiche, deux bagues (dorée en haut) qui tiennent un manche de bois, tête enveloppée
    de tissu goudronné en trois tours ; flamme au repère `Feu_Torche`, 0.34 m devant le mur et 0.56 m au-dessus du pivot."""
    k = L.K(nom)
    fer, dore = S["fer"], S["dore"]
    plat = [(0.0, 0.25), (0.05, 0.17), (0.08, 0.05), (0.08, -0.05), (0.05, -0.17), (0.0, -0.25), (-0.05, -0.17), (-0.08, -0.05),
            (-0.08, 0.05), (-0.05, 0.17)]
    prism(k, plat, -0.03, 0.0, fer, axis="y")
    for z in (0.15, -0.15):
        k.cyl((0, -0.042, z), 0.018, 0.014, dore, n=6, axis="y")      # rivets de scellement
    yt = -0.34
    L.tube(k, [(0, -0.02, 0.02), (0, -0.12, -0.02), (0, -0.24, 0.02), (0, yt, 0.07)], [0.017] * 4, fer, sides=6)
    L.tube(k, [(0, -0.02, -0.18), (0, -0.1, -0.16), (0, -0.2, -0.1), (0, yt + 0.02, -0.07)], [0.012] * 4, fer, sides=5)
    # volute sous la console
    vol = [(0, -0.16 - 0.05 * math.cos(t * 0.9), -0.05 - 0.05 * math.sin(t * 0.9)) for t in range(7)]
    L.tube(k, vol, [0.009] * len(vol), fer, sides=4, cap=False)
    k.cyl((0, yt, 0.07), 0.052, 0.04, dore, n=10, caps=(False, False))
    k.cyl((0, yt, -0.09), 0.048, 0.035, fer, n=10, caps=(False, False))
    k.cyl((0, yt, -0.24), 0.034, 0.56, S["bois"], n=8, r2=0.038)
    k.lathe((0, yt, 0.32), [(0.0, -0.005), (0.04, 0.0), (0.062, 0.025), (0.055, 0.055), (0.068, 0.075), (0.06, 0.105), (0.07, 0.125),
                            (0.058, 0.155), (0.046, 0.175), (0.0, 0.19)], CHIFFON, n=8)
    k.empty("Feu_Torche", (0, yt, 0.56))
    return k.finish(S["seed"] + 291, dict(sale_h=0.0, mousse=0.0))


def torchere(S, nom):
    """Torchère sur pied du château (1.85 m) : trépied de fer à pieds en volute, fût à nœuds dorés, coupe de fer et cage de
    quatre feuillards qui tient un fagot de tissu goudronné ; flamme au repère `Feu_Torche` (1.9 m). Pivot au sol, axe."""
    k = L.K(nom)
    fer, dore = S["fer"], S["dore"]
    for q in range(3):
        a = math.pi / 2 + 2 * math.pi * q / 3
        c, s_ = math.cos(a), math.sin(a)
        pts = [(c * 0.03, s_ * 0.03, 0.42), (c * 0.14, s_ * 0.14, 0.3), (c * 0.26, s_ * 0.26, 0.1), (c * 0.33, s_ * 0.33, 0.04)]
        L.tube(k, pts, [0.022, 0.02, 0.018, 0.016], fer, sides=6)
        vol = [(c * (0.33 + 0.045 * math.sin(t * 0.9)), s_ * (0.33 + 0.045 * math.sin(t * 0.9)), 0.045 - 0.04 * math.cos(t * 0.9) + 0.04)
               for t in range(6)]
        L.tube(k, vol, [0.012] * len(vol), fer, sides=4, cap=False)
        k.cyl((c * 0.33, s_ * 0.33, 0.0), 0.04, 0.025, fer, n=6)
    k.lathe((0, 0, 0.36), [(0.0, 0.0), (0.06, 0.02), (0.075, 0.07), (0.04, 0.12), (0.028, 0.15)], dore, n=8)
    k.cyl((0, 0, 0.5), 0.026, 1.05, fer, n=8, caps=(False, False))
    for z in (0.95, 1.5):
        k.lathe((0, 0, z - 0.06), [(0.026, 0.0), (0.05, 0.03), (0.058, 0.06), (0.05, 0.09), (0.026, 0.12)], dore, n=8)
    k.lathe((0, 0, 1.55), [(0.0, 0.0), (0.04, 0.0), (0.1, 0.04), (0.14, 0.11), (0.155, 0.16), (0.14, 0.17)], fer, n=12)
    k.lathe((0, 0, 1.6), [(0.0, 0.0), (0.11, 0.02), (0.125, 0.07), (0.112, 0.11), (0.124, 0.15), (0.1, 0.2), (0.07, 0.24), (0.0, 0.27)],
            CHIFFON, n=10)
    for q in range(4):
        a = math.pi / 4 + q * math.pi / 2
        c, s_ = math.cos(a), math.sin(a)
        L.tube(k, [(c * 0.15, s_ * 0.15, 1.7), (c * 0.15, s_ * 0.15, 1.78), (c * 0.12, s_ * 0.12, 1.84), (c * 0.15, s_ * 0.15, 1.87)],
               [0.012] * 4, fer, sides=4)
    k.empty("Feu_Torche", (0, 0, 1.92))
    return k.finish(S["seed"] + 295, dict(sale_h=0.3, mousse=0.0))


# =================================================================== jardin : falaises, passages, terrasses, ponts, enceinte
def falaise_rebords(S, nom, h=16.0):
    """Falaise de 16 m de front et de haut à quatre rebords plats (3.5 m d'écart : saut et hissage de l'un à l'autre),
    herbe au sommet : on passe d'un niveau de la forêt à l'autre en grimpant."""
    k = L.K(nom)
    sd = S["seed"] + 301
    L.rock_wall(k, [(-8, 0.5), (-3, -0.4), (2, 0.4), (8, 0.0)], h, S["roche"], thick=3.0, seg=1.1, amp=0.55, seed=sd, inward=-1,
                base_flare=0.8, top_jag=0.4)
    k.face([(-8, 3.2, h + 0.3), (8, 3.2, h + 0.3), (8, 0.8, h + 0.3), (-8, 0.8, h + 0.3)], S["herbe"])
    for i, (x, z) in enumerate(((-4.0, 3.5), (2.5, 7.0), (-2.5, 10.5), (3.5, 14.0))):
        k.box((x, -0.6, z - 0.3), (4.0, 3.2, 0.6), S["roche"])
        L.rock_blob(k, (x, -0.8, z - 2.0), (3.6, 2.4, 2.0), S["roche"], seed=sd + i, res=8, amp=0.25, flat_base=False)
        k.empty(f"Accroche_Rebord_{i + 1}", (x, -2.2, z))
    k.empty("Accroche_Sommet", (0, 0.8, h + 0.3))
    return k.finish(sd, dict(sale_h=3.0, mousse=0.7))


def _gorge(k, S, sd, lg, Lp, h, front_w):
    a = dict(thick=2.5, seg=1.0, amp=0.5, base_flare=0.7, top_jag=0.8)
    hw = lg / 2
    ai = dict(a, amp=0.3, base_flare=0.3)
    L.rock_wall(k, [(-hw, -0.3), (-hw - 0.5, Lp / 2), (-hw, Lp + 0.3)], h, S["roche"], seed=sd, inward=-1, **ai)
    L.rock_wall(k, [(hw, Lp + 0.3), (hw + 0.5, Lp / 2), (hw, -0.3)], h, S["roche"], seed=sd + 1, inward=-1, **ai)
    L.rock_wall(k, [(-front_w, 0.6), (-(front_w + hw) / 2, -0.5), (-hw - 0.2, 0.0)], h, S["roche"], seed=sd + 2, inward=-1, **a)
    L.rock_wall(k, [(hw + 0.2, 0.0), ((front_w + hw) / 2, -0.5), (front_w, 0.6)], h, S["roche"], seed=sd + 3, inward=-1, **a)
    L.rock_wall(k, [(-hw - 0.2, Lp), (-(front_w + hw) / 2, Lp + 0.5), (-front_w, Lp - 0.6)], h, S["roche"], seed=sd + 4, inward=-1, **a)
    L.rock_wall(k, [(front_w, Lp - 0.6), ((front_w + hw) / 2, Lp + 0.5), (hw + 0.2, Lp)], h, S["roche"], seed=sd + 5, inward=-1, **a)
    for sx in (-1, 1):
        x0, x1 = sorted((sx * (hw + 0.5), sx * front_w))
        k.face([(x0, 0.8, h + 0.3), (x1, 0.8, h + 0.3), (x1, Lp - 0.8, h + 0.3), (x0, Lp - 0.8, h + 0.3)], S["herbe"])
    rnd = random.Random(sd)
    for j in range(8):
        sx = rnd.choice([-1, 1])
        L.rock_blob(k, (sx * (hw - rnd.uniform(0.2, 0.5) * min(1.0, hw / 2)), rnd.uniform(1, Lp - 1), dz()),
                    (rnd.uniform(0.8, 1.4) * min(1.0, hw / 2), rnd.uniform(1.2, 2.2), 0.8), S["roche"], seed=sd + 20 + j, res=8)


def falaise_passage(S, nom):
    """Passage dans la roche entre deux zones de la forêt : gorge de 10 m de large et 16 m de long dans un massif de 30 m
    de front et 18 m de haut, arche naturelle à l'entrée (dessous vers 10.5 m) : le traqueur passe (baissé)."""
    k = L.K(nom)
    sd = S["seed"] + 311
    _gorge(k, S, sd, 10.0, 16.0, 18.0, 15.0)
    L.rock_wall(k, [(-5.8, 0.0), (0.0, -0.4), (5.8, 0.0)], 6.0, S["roche"], seed=sd + 7, inward=-1, dz=12.0, thick=4.0, seg=1.0,
                amp=0.45, base_flare=0.0, top_jag=0.6)
    L.rock_blob(k, (0, 1.8, 11.9), (11.5, 4.6, 2.5), S["roche"], seed=sd + 8, res=10, amp=0.2, flat_base=False)
    k.empty("Passage_Entree", (0, 0, 0))
    k.empty("Passage_Sortie", (0, 16.0, 0))
    return k.finish(sd, dict(sale_h=3.0, mousse=0.7))


def falaise_fente(S, nom):
    """Fente dans la roche pour les joueurs seulement : 2.6 m de large (le traqueur ne passe pas, il contourne), 12 m de
    long (tout point à moins de 15 m d'une ouverture : pas d'abri, D-035), 14 m de haut, bloc coincé vers 6 m."""
    k = L.K(nom)
    sd = S["seed"] + 321
    _gorge(k, S, sd, 2.6, 12.0, 14.0, 9.0)
    L.rock_blob(k, (0, 5.0, 6.6), (3.8, 2.6, 2.2), S["roche"], seed=sd + 9, res=8, amp=0.2, flat_base=False)
    k.empty("Passage_Entree", (0, 0, 0))
    k.empty("Passage_Sortie", (0, 12.0, 0))
    return k.finish(sd, dict(sale_h=3.0, mousse=0.7))


def terrasse(S, nom, acces=False, Lx=8.0, D=6.0, H=3.0):
    """Terrasse de jardin : mur de soutènement de 8 × 3 m à contreforts (face vers -Y), dessus dallé de 8 × 6 m à 3 m,
    balustrade de pierre au bord ; version `acces` : ouverture de 3.6 m entre deux piliers à vases, pour l'arrivée de
    SM_Jardin_Escalier_Droit_3m posé devant."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    sd = S["seed"] + 331 + int(acces)
    k.box((0, D / 2, H / 2), (Lx, D, H), S["mur"], skip=("+z", "-z"))
    k.face([(-Lx / 2, 0, H), (Lx / 2, 0, H), (Lx / 2, D, H), (-Lx / 2, D, H)], S["sol"])
    k.box((0, -0.12, H - 0.08), (Lx, 0.3, 0.16), S["pierre"])
    for x in ((-3.2, 3.2) if acces else (-3.2, 0.0, 3.2)):
        k.box((x, -0.45, (H - 0.3) / 2), (0.7, 0.9, H - 0.3), S["pierre"], taper=0.25)
    if acces:
        garde(kg, S, (-Lx / 2 + 0.22, 0.3, H), (-2.45, 0.3, H), kind="balustre")
        garde(kg, S, (2.45, 0.3, H), (Lx / 2 - 0.22, 0.3, H), kind="balustre")
        for sx in (-1, 1):
            k.box((sx * 2.1, 0.3, H + 0.65), (0.6, 0.6, 1.3), S["pierre"])
            k.lathe((sx * 2.1, 0.3, H + 1.3), [(0.16, 0), (0.3, 0.22), (0.26, 0.5), (0.1, 0.55), (0.18, 0.7), (0.0, 0.7)], S["pierre"], n=10)
    else:
        garde(kg, S, (-Lx / 2 + 0.22, 0.3, H), (Lx / 2 - 0.22, 0.3, H), kind="balustre")
    # lierre : cartes plates qui pendent du chaperon, entre les contreforts
    for j, x in enumerate((-2.45, 2.45) if acces else (-1.6, 1.7)):
        lierre_mur(k, S, x, H - 0.3, 0.9 + 0.4 * j, l=0.22 if acces else 0.28, seed=sd + 5 + j)
    k.empty("Accroche_Bord", (0, 0, H))
    ob = k.finish(sd, dict(sale_h=1.5, mousse=0.8))
    enfant(ob, kg, sd + 1, peinture=dict(sale_h=0.5, mousse=0.6))
    return ob


def pont_pierre(S, nom, Lx=12.0, W=3.6):
    """Pont de pierre en dos d'âne au-dessus d'un ruisseau ou d'un fossé (lit à -2.5 m) : 12 m, tablier de 3.6 m qui monte
    de 1.2 m au milieu (pente de 17° au plus, sans marche), arche de 8 m, parapets bas de 0.8 m."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    sd = S["seed"] + 341
    nx = 12
    top = [(-Lx / 2 + Lx * i / nx, 1.2 * math.cos(math.pi * (-Lx / 2 + Lx * i / nx) / Lx)) for i in range(nx + 1)]
    arc = [(x, -2.0 + z) for x, z in L.arch_profile(8.0, 2.6, 12, False)]
    poly = [(-Lx / 2, -2.5), (-4.0, -2.5)] + arc + [(4.0, -2.5), (Lx / 2, -2.5)] + list(reversed(top))
    for y, rev in ((-W / 2, False), (W / 2, True)):
        k.face([(x, y, z) for x, z in (reversed(poly) if rev else poly)], S["pierre"])
    n = len(poly)
    ntop = len(top)
    for i in range(n):
        (xa, za), (xb, zb) = poly[i], poly[(i + 1) % n]
        if i == n - 1:
            continue
        m = S["sol"] if i >= n - ntop else S["pierre"]
        k.face([(xa, -W / 2, za), (xb, -W / 2, zb), (xb, W / 2, zb), (xa, W / 2, za)], m)
    k.face([(-Lx / 2, -W / 2, top[0][1]), (-Lx / 2, W / 2, top[0][1]), (-Lx / 2, W / 2, -2.5), (-Lx / 2, -W / 2, -2.5)], S["pierre"])
    for sy in (-1, 1):
        for i in range(nx):
            (xa, za), (xb, zb) = top[i], top[i + 1]
            garde(kg, S, (xa, sy * (W / 2 - 0.23), za), (xb, sy * (W / 2 - 0.23), zb), h=0.8, kind="parapet", bout=False)
        for x in (-Lx / 2 + 0.3, Lx / 2 - 0.3):
            k.box((x, sy * (W / 2 - 0.21), 0.55), (0.6, 0.6, 1.1), S["pierre"])
            k.lathe((x, sy * (W / 2 - 0.21), 1.1), [(0.2, 0), (0.24, 0.12), (0.0, 0.3)], S["pierre"], n=8)
    ob = k.finish(sd, dict(sale_h=1.5, mousse=0.8, sol=-2.5))
    enfant(ob, kg, sd + 1, peinture=dict(sale_h=0.5, mousse=0.7))
    return ob


def pont_bois(S, nom, Lx=10.0, W=2.4):
    """Pont de rondins et de planches (10 × 2.4 m, dessus à 0) sur deux longerons, appuyé sur deux blocs de pierre aux
    bouts, main courante de branches : franchit un ruisseau ou une ravine."""
    k = L.K(nom)
    kg = L.K("Garde_Corps")
    sd = S["seed"] + 351
    rnd = random.Random(sd)
    for sy in (-1, 1):
        k.cyl((-Lx / 2 - 0.3, sy * 0.8, -0.42), 0.3, Lx + 0.6, S["ecorce"], n=8, axis="x", rough=0.06, seed=sy)
    n = int(Lx / 0.3)
    for i in range(n):
        x = -Lx / 2 + (i + 0.5) * Lx / n
        k.box((x, rnd.uniform(-0.06, 0.06), -0.06), (Lx / n - 0.04, W + rnd.uniform(-0.2, 0.1), 0.12), S["planche"], rot=rnd.uniform(-0.03, 0.03))
    for sx in (-1, 1):
        k.box((sx * (Lx / 2 - 0.2), 0, -1.08), (1.4, W + 0.8, 0.7), S["pierre"])
    for sy in (-1, 1):
        ps = [(-Lx / 2 + 0.3 + i * (Lx - 0.6) / 4, sy * (W / 2 + 0.05)) for i in range(5)]
        for x, y in ps:
            L.tube(kg, [(x, y, -0.5), (x + rnd.uniform(-0.05, 0.05), y, 0.6), (x, y, 1.15)], [0.09, 0.08, 0.07], S["ecorce"], sides=6, rough=0.1, seed=int(x * 10))
        rail = [(x, y, 1.05 + rnd.uniform(-0.04, 0.04)) for x, y in ps]
        L.tube(kg, rail, [0.07] * len(rail), S["ecorce"], sides=6, rough=0.1, seed=sy)
    ob = k.finish(sd, dict(sale_h=0.6, mousse=0.5))
    enfant(ob, kg, sd + 1)
    return ob


def tour_enceinte(S, nom, R=3.0, h=14.0):
    """Tour ronde d'angle du mur d'enceinte (Ø 6 m, 14 m) : fruit à la base, cordon, meurtrières, couronnement crénelé,
    plateforme au sommet (traction). Raccorde deux SM_Jardin_MurEnceinte_8m (9 m)."""
    k = L.K(nom)
    sd = S["seed"] + 361
    n = 20
    k.lathe((0, 0, 0), [(R + 0.6, 0.0), (R, 2.5)], S["mur"], n=n)
    k.cyl((0, 0, 2.5), R, h - 2.5 - 1.3, S["mur"], n=n, caps=(False, False))
    k.lathe((0, 0, 9.0), [(R, 0.0), (R + 0.2, 0.05), (R + 0.2, 0.3), (R, 0.35)], S["pierre"], n=n)
    k.lathe((0, 0, h - 1.3), [(R, 0.0), (R + 0.35, 0.3), (R + 0.35, 0.5), (R - 0.5, 0.5), (R - 0.5, 0.45)], S["pierre"], n=n)
    k.face([(math.cos(2 * math.pi * i / n) * (R - 0.5), math.sin(2 * math.pi * i / n) * (R - 0.5), h - 0.85) for i in range(n)], S["sol"])
    for i in range(10):
        a = 2 * math.pi * i / 10
        k.box((math.cos(a) * (R + 0.05), math.sin(a) * (R + 0.05), h - 0.8 + 0.6), (0.6, 1.1, 1.2), S["pierre"], rot=a)
    for j, (a, z) in enumerate(((-math.pi / 2, 4.0), (0.3, 6.0), (2.6, 6.0), (-math.pi / 2 + 0.5, 10.5), (1.8, 10.5))):
        k.box((math.cos(a) * (R + 0.02), math.sin(a) * (R + 0.02), z), (0.2, 0.2, 1.4), "M_Fond_Noir", rot=a)
    # lierre : deux cartes plates qui suivent le fût (au-dessus et au-dessous du cordon), loin des meurtrières
    for j, (a, z0, z1) in enumerate(((1.2, h - 1.42, 9.5), (4.0, 8.9, 3.0))):
        nseg = int((z0 - z1) / 0.45)
        pts = [(math.cos(a + 0.03 * math.sin(i * 1.3)) * R, math.sin(a + 0.03 * math.sin(i * 1.3)) * R, z0 - (z0 - z1) * i / nseg)
               for i in range(nseg + 1)]
        lierre(k, S, pts, lambda p: (p[0], p[1], 0.0), l=0.32, seed=sd + j, ecart=0.0)
    k.empty("Accroche_Traction", (0, -R - 0.4, h - 0.8))
    return k.finish(sd, dict(sale_h=2.5, mousse=0.8))


def breche_enceinte(S, nom, Lx=8.0, h=9.0, t=1.2):
    """Mur d'enceinte de 8 m effondré au milieu : brèche de 3 m dont le fond est à 1.4 m, éboulis en rampe des deux côtés
    (on passe d'un saut, le traqueur l'enjambe) ; bords dentelés, chaperon sur les parties restées debout."""
    k = L.K(nom)
    sd = S["seed"] + 371
    rnd = random.Random(sd)
    n = 16
    for i in range(n):
        x0 = -Lx / 2 + i * Lx / n
        xc = x0 + Lx / n / 2
        hh = 1.4 if abs(xc) < 1.5 else min(h, 1.4 + (abs(xc) - 1.5) * 4.2 + rnd.uniform(-0.6, 0.6))
        hh = h if abs(xc) > 3.4 else hh
        k.box((xc, t / 2, hh / 2), (Lx / n + 0.002, t, hh), S["mur"])
        if hh >= h - 0.01:
            k.box((xc, t / 2, h + 0.15), (Lx / n + 0.002, t + 0.3, 0.3), S["pierre"])
        else:
            k.box((xc + rnd.uniform(-0.1, 0.1), t / 2 + rnd.uniform(-0.1, 0.1), hh + 0.15), (rnd.uniform(0.35, 0.5), t * 0.8, 0.3), S["mur"], rot=rnd.uniform(-0.2, 0.2))
    for sy in (-1, 1):
        for j in range(9):
            r = rnd.uniform(0, 2.6)
            L.rock_blob(k, (rnd.uniform(-2.2, 2.2), t / 2 + sy * (t / 2 + 0.4 + r * 0.6), 0 + dz()), (rnd.uniform(0.7, 1.3), rnd.uniform(0.6, 1.0), 0.9 * (1.4 - r * 0.4)),
                        S["mur"], seed=sd + j + (10 if sy > 0 else 0), res=8)
    lierre_mur(k, S, -3.66, 7.9, 4.6, l=0.22, seed=sd + 30)
    return k.finish(sd, dict(sale_h=2.0, mousse=0.8))


def chemin_ronde(S, nom, Lx=8.0, h=9.0, t=2.4):
    """Mur d'enceinte de 8 m à chemin de ronde : 9 m de haut, 2.4 m d'épaisseur, allée dallée au sommet (1.9 m), parapet
    crénelé côté extérieur (+Y Blender), face du jardin vers -Y ; on y monte par un escalier ou une échelle du kit."""
    k = L.K(nom)
    sd = S["seed"] + 381
    k.box((0, t / 2, h / 2), (Lx, t, h), S["mur"], skip=("+z",))
    k.face([(-Lx / 2, 0, h), (Lx / 2, 0, h), (Lx / 2, t - 0.5, h), (-Lx / 2, t - 0.5, h)], S["sol"])
    k.box((0, t - 0.25, h + 0.5), (Lx, 0.5, 1.0), S["mur"], skip=("-z",))
    for i in range(4):
        x = -Lx / 2 + 1.0 + i * 2.0
        k.box((x, t - 0.25, h + 1.0 + 0.45), (1.0, 0.6, 0.9), S["pierre"])
    k.box((0, -0.15, h - 0.2), (Lx, 0.3, 0.25), S["pierre"])
    for x in (-3.0, 3.0):
        k.box((x, -0.5, 3.5), (1.0, 1.0, 7.0), S["mur"], taper=0.3)
    lierre_mur(k, S, -2.5, h - 0.35, 4.2, l=0.3, seed=sd + 1)
    lierre_mur(k, S, 2.9, h - 0.35, 5.6, l=0.28, seed=sd + 2)
    k.empty("Accroche_Bord", (0, 0, h))
    return k.finish(sd, dict(sale_h=2.5, mousse=0.8))


# =================================================================== listes par acte
def pieces_caves(S):
    a = S["act"]
    return communs(S) + [
        ("SM_Cave_Tremie_8m", tremie, dict(), "Trémie : plancher de 8 × 8 m (dessus à 0) percé d'un trou de 4 × 4 m aux bords plats et nets."),
        ("SM_Cave_Cheminee_16m", cheminee, dict(), "Cheminée de roche de 16 m : parois parallèles à 3.5 m pour monter en wall jumps."),
        ("SM_Cave_Balcon_Bois", balcon_bois, dict(), "Balcon de planches de 3.2 × 1.8 m en encorbellement, garde-corps sur trois côtés."),
        ("SM_Cave_CelluleTraqueur_Herse", cellule_herse, dict(), "Cellule d'apparition du traqueur : façade de 18 × 21 m, arc de 10 × 11 m, herse qui monte de 9.2 m."),
        ("SM_Cave_CelluleTraqueur_Barricade", cellule_barricade, dict(), "Cellule d'apparition du traqueur dans la roche : caverne, barricade de rondins à deux battants."),
        ("SM_Cave_CelluleTraqueur_Fosse", cellule_fosse, dict(), "Fosse d'apparition du traqueur : 12 × 12 × 15 m, mur à prises, deux trappes de grilles."),
    ]


def pieces_chateau(S):
    return communs(S, w=4.0) + [
        ("SM_Chateau_Tremie_8m", tremie, dict(), "Trémie : dalle de 8 × 8 m percée d'un trou de 4 × 4 m, bordure de marbre, balustrade sur trois côtés."),
        ("SM_Chateau_PorteSortie", porte_sortie, dict(), "Porte de sortie de l'acte II : arc brisé de 8 × 10 m à voussures, deux battants de boiserie, sceau de lune doré."),
        ("SM_Chateau_CelluleTraqueur", cellule_herse, dict(), "Cellule d'apparition du traqueur : arc brisé de 10 × 12 m, herse de fer forgé en losanges qui monte de 9.2 m."),
        ("SM_Chateau_Fenetre_Lancette_Simple", fenetre, dict(w=4.0, h=12.0, bw=1.6, sill=3.0, top=9.6, nl=1),
         "Fenêtre en lancette simple : baie de 1.6 × 6.6 m (appui à 3 m) dans un panneau de 4 × 12 m, vitraux sombres, repère RayonLune."),
        ("SM_Chateau_Fenetre_Lancette_Double", fenetre, dict(w=8.0, h=12.0, bw=4.0, sill=2.5, top=9.8, nl=2, oculus=(8.95, 0.5, 4)),
         "Fenêtre à deux lancettes et oculus : baie de 4 × 7.3 m (appui à 2.5 m) dans un panneau de 8 × 12 m, vitraux sombres, repère RayonLune."),
        ("SM_Chateau_Fenetre_Lancette_Triple", fenetre, dict(w=8.0, h=20.0, bw=5.6, sill=3.5, top=17.8, nl=3, oculus=(16.0, 0.85, 6)),
         "Haute fenêtre à trois lancettes et rose : baie de 5.6 × 14.3 m (appui à 3.5 m) dans un panneau de 8 × 20 m, vitraux sombres, repère RayonLune."),
        ("SM_Chateau_Chandelier_Petit", chandelier, dict(D=2.0, chute=2.4, etages=((1.0, 8),)),
         "Petit chandelier suspendu : Ø 2 m (pièce de 12 × 12 m), 8 bougies, bas à 2.4 m sous le plafond (plafond de 5.9 m au moins)."),
        ("SM_Chateau_Chandelier_Moyen", chandelier, dict(D=3.3, chute=5.0, etages=((1.0, 12), (0.55, 6))),
         "Chandelier moyen : Ø 3.3 m (salle de 24 × 16 m), 18 bougies sur deux couronnes, bas à 5 m sous le plafond (12 m)."),
        ("SM_Chateau_Chandelier_Grand", chandelier, dict(D=4.6, chute=8.0, etages=((1.0, 16), (0.66, 10), (0.36, 6))),
         "Grand chandelier de hall : Ø 4.6 m (hall de 32 × 24 m), 32 bougies sur trois couronnes, bas à 8 m sous le plafond (20 m)."),
        ("SM_Chateau_Lustre_Cristal", lustre_cristal, dict(), "Lustre à pampilles de Ø 2.4 m : trois étages de bras dorés, 24 bougies, gouttes de cristal sombre."),
        ("SM_Chateau_Applique_2Bougies", applique, dict(nb=2), "Applique murale dorée à deux bougies (platine en écusson, bras en S)."),
        ("SM_Chateau_Applique_Miroir", applique, dict(nb=3, miroir=True), "Applique à miroir ovale sombre et trois bougies."),
        ("SM_Chateau_Candelabre_Pied", candelabre_pied, dict(), "Candélabre sur pied de 2.1 m, pied de marbre, sept bougies."),
        ("SM_Chateau_Torche_Murale", torche_murale, dict(),
         "Torche murale : platine et console de fer forgé, manche de bois dans deux bagues, tête de tissu goudronné ; repère Feu_Torche."),
        ("SM_Chateau_Torchere", torchere, dict(),
         "Torchère sur pied de 1.85 m : trépied à volutes, fût à nœuds dorés, cage de fer et fagot de tissu goudronné ; repère Feu_Torche."),
    ]


def pieces_jardin(S):
    return communs(S, H_esc=3.0, g=0.38, w=3.2) + [
        ("SM_Jardin_Puits", puits, dict(), "Puits de jardin : margelle lisse Ø 3.2 m, arceau de fer forgé et poulie, eau à -1.5 m, fond à -2.4 m."),
        ("SM_Jardin_PorteSortie", porte_sortie, dict(), "Porte de sortie de l'acte III (vers l'arène) : arche de 8 × 10 m entre deux contreforts, deux battants, sceau de lune."),
        ("SM_Jardin_CelluleTraqueur_Voliere", voliere, dict(), "Cellule d'apparition du traqueur : grande volière de fer de Ø 16 m (20 m), portail à deux vantaux."),
        ("SM_Jardin_Falaise_Rebords_16m", falaise_rebords, dict(), "Falaise de 16 × 16 m à quatre rebords plats tous les 3.5 m (saut et hissage)."),
        ("SM_Jardin_Falaise_Passage", falaise_passage, dict(), "Passage dans la roche : gorge de 10 × 16 m sous une arche naturelle, le traqueur passe."),
        ("SM_Jardin_Falaise_Fente", falaise_fente, dict(), "Fente dans la roche de 2.6 m de large et 12 m de long : joueurs seulement."),
        ("SM_Jardin_Terrasse_8m", terrasse, dict(), "Terrasse : mur de soutènement de 8 × 3 m à contreforts, dessus dallé de 8 × 6 m, balustrade."),
        ("SM_Jardin_Terrasse_Acces_8m", terrasse, dict(acces=True), "Terrasse avec accès : ouverture de 3.6 m entre deux piliers à vases (arrivée d'escalier)."),
        ("SM_Jardin_Pont_Pierre_12m", pont_pierre, dict(), "Pont de pierre en dos d'âne de 12 m (tablier de 3.6 m, +1.2 m au milieu), arche de 8 m, parapets bas."),
        ("SM_Jardin_Pont_Bois_10m", pont_bois, dict(), "Pont de rondins et de planches de 10 × 2.4 m, main courante de branches."),
        ("SM_Jardin_MurEnceinte_Tour", tour_enceinte, dict(), "Tour ronde d'angle du mur d'enceinte : Ø 6 m, 14 m, crénelée, meurtrières."),
        ("SM_Jardin_MurEnceinte_Breche_8m", breche_enceinte, dict(), "Mur d'enceinte de 8 m effondré au milieu : brèche à 1.4 m, éboulis en rampe."),
        ("SM_Jardin_MurEnceinte_CheminRonde_8m", chemin_ronde, dict(), "Mur d'enceinte de 8 × 9 m à chemin de ronde dallé et parapet crénelé."),
    ]


# =================================================================== enregistrement
def enregistre(PIECES, S, fabriques):
    """fabriques : liste de (nom, fonction(S, nom, **kw), kw, doc)."""
    for nom, f, kw, doc in fabriques:
        def g(nom=nom, f=f, kw=kw):
            return f(S, nom, **kw)
        g.__name__ = nom
        g.__doc__ = doc
        PIECES[nom] = g


def communs(S, H_esc=4.0, g=0.3, w=3.0):
    a = S["act"]
    return [
        (f"SM_{a}_Escalier_Droit_{H_esc:g}m", escalier_droit, dict(H=H_esc, w=w, g=g),
         f"Escalier droit plein : monte de {H_esc:g} m (marches de 20 cm ou moins), {w:g} m de large, palier de 1.5 m ; rampe de collision sous les nez."),
        (f"SM_{a}_Escalier_Tournant_4m", escalier_tournant, dict(H=4.0, w=w, g=g),
         "Escalier à quart tournant : deux volées de 2 m, palier carré, palier en haut ; rampes de collision."),
        (f"SM_{a}_Escalier_Colimacon", colimacon, dict(H=6.4, R=2.0 if a != "Cave" else 1.8),
         "Escalier en colimaçon (Ø 4 m, 3.6 m aux caves) : monte de 6.4 m en deux tours, 16 marches de 20 cm par tour, palier de 100° ; rampe hélicoïdale."),
        (f"SM_{a}_Mezzanine_8x6", mezzanine, dict(),
         "Mezzanine de 8 × 6 m, dessus à 5 m (double saut et hissage), contre un mur ; garde-corps devant et à gauche, droite ouverte."),
        (f"SM_{a}_Coursive_8m", coursive, dict(),
         "Coursive le long d'un mur : 8 × 2.5 m, dessus à 0 (pivot au ras du mur), garde-corps côté vide."),
        (f"SM_{a}_Passerelle_10m", passerelle, dict(),
         "Passerelle entre deux étages : 10 × 2.4 m, dessus à 0, appuis aux bouts, garde-corps des deux côtés."),
        (f"SM_{a}_PorteHaute", porte_haute, dict(),
         "Porte de joueur en hauteur : ouverture de 2.4 × 3.6 m, seuil à 5 m dans un mur de 8 × 10 m, rebord de 3 × 1.2 m devant."),
        (f"SM_{a}_PorteEtroite", porte_etroite, dict(),
         "Porte étroite au sol : ouverture de 4.4 m qui contient le gabarit de 4 × 5.25 m du traqueur baissé (il se faufile)."),
        (f"SM_{a}_Echelle_6m", echelle, dict(),
         "Échelle de 6 m contre un mur, barreaux tous les 30 cm, montants qui dépassent d'1 m en haut."),
        (f"SM_{a}_MurRebords_10m", mur_rebords, dict(),
         "Mur de 6 × 10 m à quatre rebords plats en quinconce (tous les 2.2 m) : montée de rebord en rebord, wall jumps simples."),
        (f"SM_{a}_MurEscalade_12m", mur_escalade, dict(),
         "Mur escaladable de 8 × 12 m : prises plates en quinconce tous les 2 m (on s'y hisse) ; mur grimpable du traqueur et des goules."),
        (f"SM_{a}_PorteVerrouillee", porte_verrouillee, dict(),
         "Porte de salle payante : ouverture carrée de 3 × 3 m, vantail de fer en guillotine (Vantail), sceau d'âme qui tourne (Serrure)."),
    ]
