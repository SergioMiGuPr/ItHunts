# -*- coding: utf-8 -*-
"""Pièces de l'acte II (château)."""
from ens_lib import *
from pieces_commun import chandelle, chaine, anneau, livre, pile_livres, clou, crane, flamme_braise


def moulure(e, x0, x1, y, z, prof, mat):
    """Moulure droite le long de X : profil [(avancée vers -Y, hauteur)], bouts fermés."""
    def P(u, v):
        k = v * (len(prof) - 1)
        i = min(int(k), len(prof) - 2)
        t = k - i
        d = prof[i][0] * (1 - t) + prof[i + 1][0] * t
        h = prof[i][1] * (1 - t) + prof[i + 1][1] * t
        return (x0 + (x1 - x0) * u, y - d, z + h)
    e.nappe(P, 1, len(prof) - 1, mat, lisse=False)
    for x in (x0, x1):
        bm = bmesh.new()
        vs = [bm.verts.new((x, y - d, z + h)) for d, h in prof] + [bm.verts.new((x, y, z + prof[-1][1])), bm.verts.new((x, y, z + prof[0][1]))]
        try:
            bm.faces.new(vs)
        except ValueError:
            bm.free()
            continue
        bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-5)
        e.maillage(bm, mat)


def pied_tourne(e, x, y, h, r, mat):
    e.tour((x, y, 0), [(r * .7, 0), (r * .75, h * .03), (r * .5, h * .07), (r * .95, h * .16), (r, h * .24), (r * .55, h * .34), (r * .62, h * .62), (r * .85, h * .72), (r * .6, h * .78), (r, h * .82), (r, h)], mat, n=10)


def rangee(e, x0, x1, y, z, hmax, p=.24, trou=.12):
    """Rangée de livres sur une tablette, dos vers -Y."""
    x = x0
    while x < x1 - .05:
        if e.rnd.random() < trou:
            v = e.rnd.uniform(.05, .22)
            if x + v > x1 - .06:
                break
            if e.rnd.random() < .5 and v > .12:
                h = hmax * .75
                livre(e, (x + .09, y, z + .012), .05, p * .9, h, rot=(0, 22, 0))
                x += max(v, .24)
                continue
            x += v
        l = e.rnd.uniform(.035, .075)
        h = hmax * e.rnd.uniform(.66, .96)
        if x + l > x1:
            break
        livre(e, (x + l / 2, y + e.rnd.uniform(-.015, .02), z + .001), l - .003, p * e.rnd.uniform(.85, 1), h, rot=(0, e.rnd.uniform(-.6, .6), 0))
        x += l


def bibliotheque(e, pleine=True):
    """Bibliothèque de 2.4 × 4 m, 0.5 m de profondeur, dos au mur (+Y à 0) : socle, colonnettes, corniche, 6 rangs de livres."""
    for x in (-1.195, 0, 1.195):
        e.boite((x, -.24, 1.95), (.1, .44, 3.7), SOMBRE, bis=.012, fil=2)
        e.tour((x, -.47, .3), [(.05, 0), (.05, .08), (.034, .12), (.03, 1.6), (.04, 1.66), (.03, 1.72), (.028, 3.2), (.05, 3.3), (.05, 3.38)], BOIS, n=8)
    e.boite((0, -.02, 1.95), (2.44, .03, 3.8), SOMBRE, bis=0, fil=2)
    e.boite((0, -.25, .15), (2.494, .52, .3), SOMBRE, bis=.02, fil=0)
    moulure(e, -1.247, 1.247, -.5, .28, [(0, 0), (.04, .0), (.05, .03), (.02, .06), (0, .07)], BOIS)
    e.boite((0, -.27, 3.82), (2.494, .56, .14), SOMBRE, bis=.015, fil=0)
    moulure(e, -1.247, 1.247, -.54, 3.72, [(0, 0), (.02, .02), (.03, .1), (.1, .17), (.13, .18), (.13, .24), (0, .25)], BOIS)
    for sx in (-1, 1):
        e.boite((sx * .6, -.49, 3.6), (1.0, .03, .16), BOIS, bis=.01, fil=0)
    z = .33
    hs = (.48, .55, .5, .5, .52, .46)
    for k, h in enumerate(hs):
        for sx in (-1, 1):
            e.boite((sx * .57, -.25, z), (1.04, .42, .035), BOIS, bis=.006, fil=0)
            rangee(e, sx * .57 - .5, sx * .57 + .5, -.28, z + .02, h - .08, trou=(.1 if k < 4 else .2) if pleine else .62)
        z += h + .02
    for k in range(3 if pleine else 0):
        pile_livres(e, -.7 + k * .65, -.28, 3.9, e.rnd.randrange(1, 4))
    e.col_boite((0, -.27, 2), (2.5, .54, 4))


@piece("Bibliotheque", 60, sale=.5)
def _(e):
    """Bibliothèque de 2.5 × 4 m, 0.5 m de profondeur, dos au mur : socle, colonnettes, corniche, 6 rangs de livres ; se pose au pas de 2.5 m."""
    bibliotheque(e)


@piece("Bibliotheque_Pillee", 61, sale=.6)
def _(e):
    """La même, presque vidée (pour la poser couchée sur le flanc)."""
    bibliotheque(e, False)


@piece("Echelle_Biblio", 61, sale=.3)
def _(e):
    """Échelle de bibliothèque appuyée (dos +Y) : pied à 1 m du mur, haut à 3.7 m."""
    for sx in (-1, 1):
        e.barre((sx * .28, -1.05, 0), (sx * .24, -.1, 3.75), .06, .09, BOIS, bis=.01)
    for k in range(11):
        t = (k + .8) / 12
        e.cyl((-.28 + .04 * t, -1.05 + .95 * t, 3.75 * t), (.28 - .04 * t, -1.05 + .95 * t, 3.75 * t), .02, BOIS, n=7)
    e.col_boite((0, -.58, 1.88), (.55, .08, 3.85), rot=(-14.2, 0, 0))


@piece("Globe", 62, sale=.2)
def _(e):
    for k in range(3):
        a = k * 2.0944
        e.tube([(0, 0, .42), (math.cos(a) * .1, math.sin(a) * .1, .3), (math.cos(a) * .18, math.sin(a) * .18, .12), (math.cos(a) * .3, math.sin(a) * .3, .02)], [.03, .026, .024, .03], SOMBRE, n=7)
    e.tour((0, 0, .38), [(.03, 0), (.06, .05), (.035, .12), (.03, .2)], SOMBRE, n=10)
    anneau(e, (0, 0, .98), .42, .022, SOMBRE, n=24)
    anneau(e, (0, 0, .98), .39, .016, LAITON, rot=(90, 0, 20), n=24)
    e.tour((0, 0, .98), [(0, -.35), (.13, -.325), (.25, -.245), (.33, -.117), (.35, 0), (.33, .117), (.25, .245), (.13, .325), (0, .35)], PAPIER, n=18, rot=(0, 23, 0))
    for k, (lat, r) in enumerate(((0, .353), (.12, .333), (-.12, .333))):
        anneau(e, Vector((0, 0, .98)) + rotm(0, 23, 0) @ Vector((0, 0, lat)), r, .005, CUIR, rot=(0, 23, 0), n=22, m=4)
    for k in range(9):      # continents peints en cuir
        th, ph = e.rnd.uniform(0, 6.28), e.rnd.uniform(-.8, .9)
        c = Vector((math.cos(th) * math.cos(ph), math.sin(th) * math.cos(ph), math.sin(ph))) * .349
        e.tour(Vector((0, 0, .98)) + c, [(.1 + .05 * e.rnd.random(), -.012), (.06, .006), (0, .008)], CUIR, n=9, bruit=.5, rot=[math.degrees(v) for v in c.to_track_quat("Z", "Y").to_euler()])
    e.col_boite((0, 0, .65), (.6, .6, 1.3))


@piece("Pupitre", 63, sale=.3)
def _(e):
    """Lutrin gothique avec un grand livre ouvert."""
    e.boite((0, 0, .05), (.62, .5, .1), SOMBRE, bis=.02)
    e.tour((0, 0, .1), [(.16, 0), (.17, .04), (.08, .1), (.06, .5), (.09, .55), (.06, .6), (.06, .85), (.1, .95)], SOMBRE, n=8, lisse=False)
    e.boite((0, -.05, 1.15), (.72, .04, .56), BOIS, rot=(-62, 0, 0), bis=.01, fil=0)
    e.boite((0, -.29, 1.02), (.72, .07, .03), BOIS, rot=(-62, 0, 0), bis=.006, fil=0)
    e.boite((0, .02, 1.02), (.3, .06, .34), SOMBRE, bis=.02)
    for s in (-1, 1):
        def P(u, v, s=s):
            x = s * u * .3
            z = .035 * math.sin(min(1, u * 1.3) * math.pi) * (1 - u * .4) + .02
            y = (v - .5) * .42
            q = rotm(28, 0, 0) @ Vector((x, y, z))
            return q + Vector((0, -.07, 1.155))
        e.nappe(P, 6, 1, PAPIER, lisse=True)
        e.boite(Vector((s * .155, -.065, 1.145)), (.31, .44, .016), ROUGE, rot=(28, 0, 0), bis=.003)
        for k in range(7):
            p = rotm(28, 0, 0) @ Vector((s * .15, .15 - k * .045, .054)) + Vector((0, -.07, 1.155))
            e.boite(p, (.19 - .05 * (k == 6), .008, .002), CUIR, rot=(28, 0, 0), bis=0)
    e.col_boite((0, 0, .6), (.6, .5, 1.2))


@piece("Livres_Sol", 64, sale=0)
def _(e):
    pile_livres(e, 0, 0, 0, 7)
    pile_livres(e, .42, .12, 0, 4)
    pile_livres(e, -.3, .3, 0, 3)
    for k in range(3):
        x, y, a = ((.2, -.4, 30), (-.4, -.25, -50), (.7, -.2, 80))[k]
        # livre ouvert posé
        for s in (-1, 1):
            def P(u, v, s=s):
                q = rotm(0, 0, a) @ Vector((s * u * .2, (v - .5) * .28, .012 + .035 * math.sin(min(1, u * 1.3) * math.pi)))
                return q + Vector((x, y, 0))
            e.nappe(P, 5, 1, PAPIER)
            e.boite(Vector((x, y, .006)) + rotm(0, 0, a) @ Vector((s * .105, 0, 0)), (.21, .3, .012), [ROUGE, CUIR, BLEU][k], rot=(0, 0, a), bis=.002)


@piece("Livres_Table", 65, sale=0)
def _(e):
    pile_livres(e, 0, 0, 0, 4, 15)
    pile_livres(e, .5, .05, 0, 2, 20)
    e.tour((-.4, .12, .03), [(.028, -.2), (.03, -.2), (.03, .2), (.028, .2)], PAPIER, n=9, rot=(0, 90, 25))
    e.tour((-.36, .2, .03), [(.026, -.17), (.026, .17)], PAPIER, n=9, rot=(0, 90, 10))
    e.nappe(lambda u, v: ((u - .5) * .42 - .35, (v - .5) * .3 - .12, .004 + .05 * max(0, abs(u - .5) - .4) * 5), 10, 1, PAPIER)
    for k in range(6):
        e.boite((-.35, -.02 - k * .035, .007), (.26, .006, .001), CUIR, bis=0)
    e.tour((.2, -.2, 0), [(.04, 0), (.045, .02), (.035, .05), (.018, .06), (.022, .07), (0, .066)], NOIR, n=9)
    e.tube([(.2, -.2, .06), (.24, -.17, .14), (.31, -.13, .24)], [.003, .004, .002], OS, n=4)
    e.nappe(lambda u, v: (.22 + .1 * v + (u - .5) * .05 * math.sin(v * 3.1) + .0, -.19 + .06 * v, .1 + .16 * v), 2, 5, OS)


def rideau(e, x0, x1, z0, z1, y=0.0, plis=7, amp=.08, mat=ROUGE, retenu=0.0, nv=10, dech=0.0):
    """Rideau plissé de z1 (haut) à z0, `retenu` : resserré vers x0 (signe) à mi-hauteur."""
    def P(u, v):
        z = z1 + (z0 - z1) * v
        x = x0 + (x1 - x0) * u
        if retenu:
            k = math.exp(-((v - .62) / .28) ** 2)
            c = x0 if retenu > 0 else x1
            x = c + (x - c) * (1 - abs(retenu) * k)
        a = amp * (.45 + .55 * v)
        yy = y - a * math.sin(u * plis * 2 * math.pi + .8 * math.sin(v * 2.2)) - a * .35 * math.sin(u * plis * 4.4 + 1)
        if dech and v > .9:
            z += dech * (noise.noise(Vector((u * 14, 0, 3))) * .5 + .5) * (v - .9) * 10
        return (x, yy, z)
    e.nappe(P, plis * 6, nv, mat)


@piece("Tenture", 67, sale=0)
def _(e):
    """Tapisserie peinte de 3 × 4 m, pendue à une barre (pivot au mur, bas à 0)."""
    e.cyl((-1.7, -.07, 4.1), (1.7, -.07, 4.1), .035, SOMBRE, n=8)
    for sx in (-1, 1):
        e.tour((sx * 1.7, -.07, 4.1), [(0, -.08), (.06, 0), (0, .1)], LAITON, n=8, rot=(0, sx * 90, 0))
    for k in range(7):
        x = -1.35 + k * .45
        e.boite((x, -.07, 4.08), (.14, .09, .1), CUIR, bis=.01)
    e.nappe(lambda u, v: ((u - .5) * 3.0, -.06 - .035 * math.sin(u * 17 + v) * (.3 + v) - .01 * math.sin(v * 9 + u * 3), 4.02 - 4.0 * v), 30, 12, TAPISSERIE, uvf=lambda u, v: (u, 1 - v))
    for k in range(40):
        x = -1.48 + k * .076
        e.boite((x, -.07, -.03), (.012, .012, .12), OR, bis=0)


@piece("Tapis_4x3", 68, sale=0)
def _(e):
    """Tapis de 4 × 2.8 m, 2 cm, coin replié, franges. UV : motif entier centré."""
    def P(u, v):
        x, y = (u - .5) * 4.0, (v - .5) * 2.8
        z = .012 + .006 * noise.noise(Vector((x * 1.5, y * 1.5, 0)))
        d = (u - .8) + (1 - v) * 0 + (v * 0)
        k = (u + (1 - v)) - 1.72
        if k > 0:
            z += .1 * math.sin(min(1, k / .28) * math.pi) + k * .05
            x -= k * .7
            y += k * .4
        return (x, y, z)
    e.nappe(P, 24, 16, TAPIS, uvf=lambda u, v: (u * 2, v * 1.4))
    for s in (-1, 1):
        for k in range(45):
            y = -1.38 + k * .0627
            if s > 0 and y < -.6:
                continue
            e.boite((s * 2.06, y, .009), (.13, .012, .008), OR, bis=0, rot=(0, 0, e.rnd.uniform(-14, 14)))


@piece("Chemin_Tapis_8m", 69, sale=0)
def _(e):
    e.nappe(lambda u, v: ((u - .5) * 8, (v - .5) * 1.8, .012 + .005 * noise.noise(Vector((u * 9, v * 2, 0)))), 16, 4, TAPIS, uvf=lambda u, v: (u * 4, v * .9))
    for s in (-1, 1):
        e.boite((0, s * .87, .012), (8, .07, .014), OR, bis=0)


# ------------------------------------------------------------------ tables
@piece("Table_Longue", 70, sale=.35)
def _(e):
    """Table de banquet de 6 × 1.5 m, nappe à plis."""
    for x in (-2.5, 0, 2.5):
        for sy in (-1, 1):
            pied_tourne(e, x, sy * .5, .84, .09, SOMBRE)
        e.boite((x, 0, .14), (.12, 1.1, .09), SOMBRE, bis=.015)
    e.boite((0, 0, .14), (5.0, .1, .09), SOMBRE, bis=.015, fil=0)
    for k in range(5):
        e.boite((0, -.6 + k * .3, .88), (6.0, .29, .07), BOIS, bis=.01, fil=0)

    def nap(u, v):
        # v : contour (0..1) avec retombée ; u le long de la table
        x = (u - .5) * 6.06
        w = 2.2
        s = (v - .5) * w
        if abs(s) <= .76:
            return (x, s, .925 + .004 * math.sin(x * 4 + s * 3))
        d = abs(s) - .76
        pli = .05 * math.sin(x * 5.2 + (1 if s > 0 else 0)) * (d / .34) + .025 * math.sin(x * 11) * (d / .34)
        return (x, math.copysign(.77 + .03 * d + pli, s), .925 - d * (1 + .08 * math.sin(x * 2.1)))
    e.nappe(nap, 80, 14, DRAP)
    e.nappe(lambda u, v: ((u - .5) * 6.3, (v - .5) * .5, .932 - (max(0, abs(u - .5) - .478) * 12) * 1.0), 40, 2, ROUGE)
    e.col_boite((0, 0, .47), (6.0, 1.5, .94))


def cheminee(e, feu=True):
    """Cheminée monumentale de 4.2 × 4.6 m, 1.1 m de saillie, dos au mur : piédroits à colonnettes, linteau gravé, hotte,
    braises ; repère Feu."""
    e.boite((0, -.55, .08), (4.2, 1.5, .16), CLAIRE, bis=.03)
    for sx in (-1, 1):
        for k in range(5):
            e.boite((sx * 1.62, -.42, .16 + .2 + k * .4), (.72 + (.06 if k % 2 else 0), .84, .4 - .012), CLAIRE, bis=.025, jit=.006)
        e.tour((sx * 1.62, -.94, .16), [(.2, 0), (.2, .12), (.14, .2), (.12, .28), (.115, 1.5), (.14, 1.56), (.12, 1.62), (.22, 1.86), (.24, 2.0)], CLAIRE, n=12)
    for k, (x, w) in enumerate(((-1.35, 1.35), (0, 1.3), (1.35, 1.35))):
        e.boite((x, -.55, 2.42), (w - .012, 1.1, .56), CLAIRE, bis=.03, jit=.006, fil=0)
    moulure(e, -2.15, 2.15, -1.08, 2.68, [(0, 0), (.06, .02), (.08, .08), (.16, .12), (.2, .13), (.2, .2), (0, .22)], CLAIRE)
    e.boite((0, -.54, 2.79), (4.3, 1.08, .2), CLAIRE, bis=.02, fil=0)

    def hotte(u, v):
        w = 1.95 - .75 * v
        d = 1.0 - .55 * v
        pts = [(-w, 0), (-w, -d), (w, -d), (w, 0)]
        k = u * 3
        i = min(int(k), 2)
        t = k - i
        return (pts[i][0] * (1 - t) + pts[i + 1][0] * t, pts[i][1] * (1 - t) + pts[i + 1][1] * t, 2.88 + 1.75 * v)
    e.nappe(hotte, 3, 3, "M_Chateau_PierreClaire", lisse=False)
    for k in range(4):      # arêtiers de la hotte en pierre de taille
        for sx in (-1, 1):
            v = (k + .5) / 4
            e.boite((sx * (1.95 - .75 * v), -(1.0 - .55 * v), 2.88 + 1.75 * v), (.26 + .05 * (k % 2), .26, .42), CLAIRE, bis=.03, rot=(0, 0, 45 * sx), jit=.008)
    e.boite((0, -.25, 4.66), (2.5, .56, .12), CLAIRE, bis=.02)
    e.boite((0, -.2, 6.4), (2.1, .42, 3.4), "M_Chateau_PierreClaire", bis=0)
    for k in range(7):
        for sx in (-1, 1):
            e.boite((sx * 1.05, -.36, 4.95 + k * .48), (.26 + .06 * (k % 2), .24, .46), CLAIRE, bis=.03, jit=.008)
    e.boite((0, -.25, 8.15), (2.5, .56, .16), CLAIRE, bis=.02)
    # écu sculpté
    e.boite((0, -1.14, 2.42), (.5, .1, .46), PIERRE, bis=.05, taper=(1, 1))
    e.boite((0, -1.2, 2.42), (.1, .06, .34), CLAIRE, bis=.02)
    e.boite((0, -1.2, 2.46), (.32, .06, .09), CLAIRE, bis=.02)
    # foyer
    e.boite((0, -.04, 1.2), (2.6, .06, 2.1), "M_Bib_Briques", bis=0)
    for sx in (-1, 1):
        e.boite((sx * 1.27, -.4, 1.2), (.05, .7, 2.1), "M_Bib_Briques", bis=0)
    e.boite((0, -.4, .168), (2.5, .8, .02), NOIR, bis=0)
    for sx in (-1, 1):
        e.boite((sx * .75, -.6, .42), (.07, .8, .07), FER)
        e.tour((sx * .75, -1.0, .16), [(.07, 0), (.05, .06), (.03, .1), (.03, .5), (.06, .56), (.05, .64), (0, .68)], FER, n=8)
        e.boite((sx * .75, -.25, .28), (.07, .07, .26), FER)
    if not feu:
        for k in range(9):      # restes calcinés : dos de chaise, cadre, reliures
            e.boite((e.rnd.uniform(-.75, .75), -.6 + e.rnd.uniform(-.2, .2), .3 + .05 * k), (e.rnd.uniform(.25, .7), .06, .05), "M_Ens_Bois_Brule", rot=(e.rnd.uniform(-30, 30), e.rnd.uniform(-50, 50), e.rnd.uniform(0, 180)), jit=.01)
        for k in range(5):
            livre(e, (-.5 + k * .25, -.75, .19), .06, .2, .27, rot=(0, 80, e.rnd.uniform(0, 90)))
    for k, (y, z, x) in enumerate(((-.45, .54, 0), (-.72, .53, .05), (-.58, .7, -.06))):
        e.cyl((-.9 + x, y, z), (.9 + x, y + .05, z + .03), .1, "M_Ens_Bois_Brule", n=9)
    if feu:
        flamme_braise(e, (0, -.6, .2), .6, 28)
    else:
        e.tour((0, -.6, .17), [(.75, 0), (.5, .07), (.2, .12), (0, .13)], NOIR, n=16, bruit=.3)
    for k in range(11 if feu else 0):      # flammes en volume (émissives), hautes au centre
        x = -.72 + k * .144
        h = (.4 + .75 * math.cos(x * 1.7) ** 2) * e.rnd.uniform(.75, 1.15)
        r = e.rnd.uniform(.1, .16)
        e.tour((x, -.6 + e.rnd.uniform(-.14, .14), .6), [(0, 0), (r * .8, h * .1), (r, h * .26), (r * .72, h * .5), (r * .3, h * .78), (0, h)], FLAMME, n=9, rot=(e.rnd.uniform(-9, 9), e.rnd.uniform(-12, 12), 0), bruit=.35)
        e.tour((x, -.66, .6), [(0, 0), (r * .5, h * .06), (r * .55, h * .16), (r * .34, h * .34), (0, h * .58)], "M_Ens_Flamme_Coeur", n=8, rot=(0, e.rnd.uniform(-10, 10), 0), bruit=.3)
    if feu:
        e.vide("Feu", (0, -.6, .72))
    for sx in (-1, 1):
        e.col_boite((sx * 1.62, -.5, 1.1), (.8, 1.05, 2.2))
    e.col_boite((0, -.55, 2.6), (4.2, 1.1, .8))
    e.col_boite((0, -.4, 3.8), (3.2, .8, 1.7))
    e.col_boite((0, -.5, .45), (2.0, .8, .6))


@piece("Cheminee", 71, sale=.9)
def _(e):
    cheminee(e)


@piece("Cheminee_Froide", 71, sale=2.2)
def _(e):
    cheminee(e, False)


# ------------------------------------------------------------------ chambre
@piece("Lit_Baldaquin", 72, sale=.4)
def _(e):
    """Lit à baldaquin de 2.3 × 2.6 m, 3.2 m de haut, tête au mur (+Y) : colonnes tournées, dais à lambrequin, rideaux, édredon."""
    for sx in (-1, 1):
        for y in (-2.45, -.12):
            e.boite((sx * 1.05, y, .35), (.2, .2, .7), SOMBRE, bis=.02)
            e.tour((sx * 1.05, y, .7), [(.09, 0), (.1, .05), (.06, .12), (.1, .35), (.055, .6), (.05, 1.5), (.085, 1.65), (.05, 1.75), (.06, 2.2), (.1, 2.28)], SOMBRE, n=12)
            e.tour((sx * 1.05, y, 3.15), [(.1, 0), (.06, .05), (.09, .14), (0, .26)], OR, n=10)
        e.boite((sx * 1.05, -1.28, .45), (.1, 2.2, .3), SOMBRE, bis=.015)
    e.boite((0, -2.45, .5), (2.0, .1, .55), SOMBRE, bis=.02)
    for k in range(4):
        e.boite((-.72 + k * .48, -2.51, .5), (.36, .03, .34), BOIS, bis=.012)
    e.boite((0, -.1, 1.15), (2.0, .1, 1.5), SOMBRE, bis=.02, fil=2)
    for k in range(3):
        x = -.62 + k * .62
        e.boite((x, -.17, 1.2), (.46, .03, .9), BOIS, bis=.015, fil=2)
    e.boite((0, -1.28, .55), (2.0, 2.2, .24), LIN, bis=.06, seg=2)

    def edr(u, v):
        s = (u - .5) * 2.75
        y = -2.4 + 1.75 * v
        gon = .06 * math.sin(v * 9.4 + .4 * math.sin(u * 8)) * .6 + .05 * noise.noise(Vector((s * 2, y * 2, 2)))
        if abs(s) < 1.0:
            return (s, y, .76 + gon + .05 * math.cos(s * 1.5))
        d = abs(s) - 1.0
        return (math.copysign(1.08 + .03 * math.sin(y * 7), s), y + .02 * math.sin(d * 9), .76 + gon * .4 - d * 1.35)
    e.nappe(edr, 30, 22, ROUGE)
    e.nappe(lambda u, v: ((u - .5) * 2.1, -.95 + .38 * v + .03 * math.sin(u * 8), .84 + .04 * math.sin(u * 6 + v * 2) - .02 * v), 18, 5, DRAP)
    for sx in (-1, 1):
        def ore(u, v, sx=sx):
            a, b = u * 2 * math.pi, (v - .5) * math.pi
            k = lambda q: math.copysign(abs(math.cos(q)) ** .6, math.cos(q))
            k2 = lambda q: math.copysign(abs(math.sin(q)) ** .6, math.sin(q))
            return (sx * .5 + .42 * k(a) * k(b), -.42 + .25 * k2(a) * k(b), .86 + .13 * math.sin(b))
        e.nappe(ore, 18, 8, DRAP, ferme_u=True)
    # dais
    for sx in (-1, 1):
        e.boite((sx * 1.05, -1.28, 3.06), (.12, 2.45, .16), SOMBRE, bis=.015, fil=1)
    for y in (-2.45, -.12):
        e.boite((0, y, 3.06), (2.2, .12, .16), SOMBRE, bis=.015, fil=0)
    e.nappe(lambda u, v: ((u - .5) * 2.1, -.12 - 2.33 * v, 3.05 - .12 * math.sin(u * math.pi) * math.sin(v * math.pi)), 8, 8, ROUGE)

    def lamb(p0, p1, n):
        d = (Vector(p1) - Vector(p0))
        L = d.length
        x = d.normalized()
        o = Vector((x.y, -x.x, 0))
        e.nappe(lambda u, v: Vector(p0) + d * u + o * (.015 + .02 * math.sin(u * n * 6.28) * v) - Vector((0, 0, (.28 + .1 * abs(math.sin(u * n * math.pi))) * v)), n * 8, 3, ROUGE)
        e.tube([Vector(p0) + d * (k / (n * 8)) + o * .03 - Vector((0, 0, .28 + .1 * abs(math.sin(k / (n * 8) * n * math.pi)))) for k in range(n * 8 + 1)], .016, OR, n=5)
    lamb((-1.12, -2.52, 3.0), (1.12, -2.52, 3.0), 5)
    lamb((-1.12, -.1, 3.0), (-1.12, -2.52, 3.0), 5)
    lamb((1.12, -2.52, 3.0), (1.12, -.1, 3.0), 5)

    def rid(x, y0, y1, n):
        e.nappe(lambda u, v: (x + .05 * math.sin(u * n * 6.28 + v), y0 + (y1 - y0) * u * (1 - .45 * math.exp(-((v - .6) / .25) ** 2)), 2.98 - 2.9 * v), n * 6, 10, ROUGE)
    for sx in (-1, 1):
        rid(sx * 1.05, -2.36, -1.75, 4)
        rid(sx * 1.05, -.2, -.75, 4)
    rideau(e, -.98, .98, .9, 2.98, -.22, plis=8, amp=.05)
    e.col_boite((0, -1.28, .42), (2.3, 2.6, .84))
    e.col_boite((0, -.12, 1.3), (2.2, .2, 1.7))
    for sx in (-1, 1):
        for y in (-2.45, -.12):
            e.col_boite((sx * 1.05, y, 1.6), (.2, .2, 3.2))


@piece("Lit_Effondre", 73, sale=.5)
def _(e):
    """Lit à baldaquin ruiné : deux colonnes cassées, dais tombé en biais, drap déchiré jusqu'au sol."""
    for sx, y, h in ((-1, -2.45, 1.0), (1, -2.45, 2.3), (-1, -.12, 2.3), (1, -.12, 2.3)):
        e.boite((sx * 1.05, y, .35), (.2, .2, .7), SOMBRE, bis=.02)
        e.tour((sx * 1.05, y, .7), [(.09, 0), (.1, .05), (.06, .12), (.1, .35), (.055, .6), (.05, h - .02), (.02 if h < 2 else .09, h)], SOMBRE, n=12)
    for sx in (-1, 1):
        e.boite((sx * 1.05, -1.28, .45), (.1, 2.2, .3), SOMBRE, bis=.015)
    e.boite((0, -2.45, .5), (2.0, .1, .55), SOMBRE, bis=.02)
    e.boite((0, -.1, 1.15), (2.0, .1, 1.5), SOMBRE, bis=.02, fil=2)
    for k in range(3):
        e.boite((-.62 + k * .62, -.17, 1.2), (.46, .03, .9), BOIS, bis=.015, fil=2)
    e.boite((.05, -1.3, .5), (2.0, 2.2, .22), LIN, bis=.06, seg=2, rot=(2, -4, 1))
    # dais tombé : cadre incliné du coin -x avant
    A = [Vector((-1.12, -2.6, 1.3)), Vector((1.1, -2.5, 3.0)), Vector((1.1, -.1, 3.05)), Vector((-1.1, -.1, 3.0))]
    A = [Vector((-1.25, -2.75, .95)), Vector((1.1, -2.5, 3.0)), Vector((1.1, -.1, 3.05)), Vector((-1.1, -.15, 2.55))]
    for i in range(4):
        e.barre(A[i], A[(i + 1) % 4], .12, .16, SOMBRE, bis=.015)

    def toile(u, v):
        p = A[0].lerp(A[1], u).lerp(A[3].lerp(A[2], u), v)
        s = math.sin(u * math.pi) * math.sin(v * math.pi)
        return p - Vector((0, 0, .55 * s + .08 * noise.noise(Vector((u * 6, v * 6, 0)))))
    e.nappe(toile, 16, 16, ROUGE)

    def chute(u, v):
        p = A[0].lerp(A[1], u * .72)
        z = p.z * (1 - v) + .03 * v
        return (p.x - .12 * v + .1 * math.sin(u * 22) * (.3 + v), p.y - .12 - .34 * v ** 2 + .15 * (1 - abs(math.sin(u * 15 + 1 + v))) * (.35 + v) + .05 * math.sin(u * 37), max(.02, z + .25 * v * noise.noise(Vector((u * 9, 4, 0)))))
    e.nappe(chute, 64, 12, ROUGE)

    def drap(u, v):
        s = (u - .5) * 2.9
        y = -2.3 + 1.6 * v
        g = .07 * noise.noise(Vector((s * 2.5, y * 2.5, 5)))
        if s < 1.0:
            return (s, y + .1 * math.sin(s * 3), .68 + g)
        d = s - 1.0
        return (1.1 + .25 * d + .05 * math.sin(y * 9), y, max(.02, .66 + g - d * 1.6))
    e.nappe(drap, 26, 18, DRAP)
    e.col_boite((0, -1.28, .42), (2.3, 2.6, .84))
    e.col_boite((0, -.12, 1.3), (2.2, .2, 2.4))
    e.col_boite((.2, -1.35, 2.5), (2.2, 2.4, .3), rot=(0, -22, 0))


@piece("Chevet", 74, sale=.3)
def _(e):
    for sx in (-1, 1):
        for sy in (-1, 1):
            pied_tourne(e, sx * .2, sy * .17, .7, .035, SOMBRE)
    e.boite((0, 0, .6), (.5, .44, .2), SOMBRE, bis=.012)
    e.boite((0, -.225, .6), (.36, .02, .12), BOIS, bis=.008)
    e.tour((0, -.24, .6), [(.02, 0), (.02, .03), (0, .035)], LAITON, n=8, rot=(90, 0, 0))
    e.boite((0, 0, .725), (.58, .52, .05), BOIS, bis=.012)
    e.col_boite((0, 0, .38), (.56, .5, .76))


@piece("Coiffeuse", 75, sale=.3)
def _(e):
    """Coiffeuse à miroir ovale fendu."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            pied_tourne(e, sx * .55, -.28 + sy * .2, .74, .045, SOMBRE)
    e.boite((0, -.28, .74), (1.3, .56, .16), SOMBRE, bis=.015)
    for x in (-.4, 0, .4):
        e.boite((x, -.565, .74), (.34, .02, .1), BOIS, bis=.008)
        e.tour((x, -.58, .74), [(.016, 0), (0, .02)], LAITON, n=8, rot=(90, 0, 0))
    e.boite((0, -.28, .84), (1.4, .62, .05), BOIS, bis=.012)
    for sx in (-1, 1):
        e.barre((sx * .4, -.08, .86), (sx * .42, -.08, 1.5), .06, .05, SOMBRE)
    n = 24
    P = [Vector((math.cos(2 * math.pi * k / n) * .38, -.1, 1.45 + math.sin(2 * math.pi * k / n) * .52)) for k in range(n + 1)]
    e.tube(P, .045, OR, n=7, bout=False)
    e.tour((0, -.06, 1.45), [(0.38, 0), (.0, .002)], "M_Ens_Miroir", n=24, rot=(90, 0, 0), ech=(1, 1.37, 1), lisse=False)
    for a, b in (((-.1, .2), (.12, -.1)), ((.12, -.1), (.3, -.2)), ((.12, -.1), (.02, -.42)), ((-.1, .2), (-.3, .3)), ((-.1, .2), (.05, .45))):
        e.barre((a[0], -.066, 1.45 + a[1]), (b[0], -.066, 1.45 + b[1]), .006, .004, NOIR, bis=0)
    e.tour((0, -.1, 2.0), [(0, 0), (.1, .03), (.05, .1), (.07, .15), (0, .2)], OR, n=8, ech=(1, .4, 1))
    e.tour((-.45, -.35, .865), [(.05, 0), (.06, .05), (.03, .1), (.015, .12), (.02, .16), (0, .16)], VERRE, n=10)
    e.tour((.4, -.3, .865), [(.07, 0), (.075, .04), (0, .05)], LAITON, n=10)
    e.col_boite((0, -.28, .44), (1.4, .62, .88))
    e.col_boite((0, -.1, 1.45), (.9, .12, 1.1))


@piece("Armoire_Ouverte", 76, sale=.4)
def _(e):
    """Armoire de 1.6 × 2.6 m, un battant entrouvert, vêtements pendus ; dos +Y."""
    for sx in (-1, 1):
        e.boite((sx * .78, -.33, 1.35), (.08, .66, 2.5), SOMBRE, bis=.012, fil=2)
    e.boite((0, -.02, 1.35), (1.56, .04, 2.5), SOMBRE, bis=0, fil=2)
    e.boite((0, -.34, .1), (1.7, .7, .2), SOMBRE, bis=.02, fil=0)
    e.boite((0, -.34, 2.62), (1.76, .74, .14), SOMBRE, bis=.02, fil=0)
    moulure(e, -.9, .9, -.7, 2.6, [(0, 0), (.03, .02), (.1, .1), (.1, .16), (0, .18)], BOIS)
    e.cyl((-.74, -.34, 2.3), (.74, -.34, 2.3), .02, FER, n=7)
    for k, m in enumerate((ROUGE, LIN, BLEU, CUIR, LIN)):
        x = -.55 + k * .24
        rideau(e, x - .1, x + .1, .5 + .2 * (k % 3), 2.26, -.34, plis=2, amp=.12, mat=m, nv=6)
    e.boite((-.4, -.69, 1.4), (.74, .05, 2.28), BOIS, bis=.012, fil=2)
    for z, h in ((.85, .95), (1.95, .95)):
        e.boite((-.4, -.72, z), (.5, .03, h), SOMBRE, bis=.014, fil=2)
    M = Matrix.Translation((.78, -.69, 0)) @ Matrix.Rotation(math.radians(62), 4, "Z")
    e.boite(M @ Vector((-.37, 0, 1.4)), (.74, .05, 2.28), BOIS, rot=(0, 0, 62), bis=.012, fil=2)
    for z, h in ((.85, .95), (1.95, .95)):
        for s in (-.03, .03):
            e.boite(M @ Vector((-.37, s, z)), (.5, .03, h), SOMBRE, rot=(0, 0, 62), bis=.014, fil=2)
    e.boite((.1, -.45, .24), (.5, .34, .08), LIN, bis=.03, rot=(0, 0, 8))
    e.col_boite((0, -.34, 1.35), (1.7, .7, 2.7))


@piece("Horloge", 77, sale=.4)
def _(e):
    """Horloge de parquet, 2.6 m, dos +Y."""
    e.boite((0, -.22, .25), (.66, .42, .5), SOMBRE, bis=.02, fil=2)
    e.boite((0, -.2, 1.1), (.46, .34, 1.3), SOMBRE, bis=.015, fil=2)
    e.boite((0, -.375, 1.1), (.3, .02, 1.05), NOIR, bis=.0)
    for sx in (-1, 1):
        e.boite((sx * .17, -.385, 1.1), (.05, .03, 1.15), BOIS, bis=.008)
    for z in (.54, 1.66):
        e.boite((0, -.385, z), (.39, .03, .06), BOIS, bis=.008)
    e.cyl((0.03, -.39, 1.6), (.06, -.39, .85), .008, LAITON, n=6)
    e.tour((.06, -.395, .8), [(0, -.01), (.08, 0), (0, .012)], LAITON, n=14, rot=(90, 0, 0))
    e.boite((0, -.23, 2.02), (.64, .42, .6), SOMBRE, bis=.02)
    e.tour((0, -.445, 2.02), [(.24, 0), (.24, .015), (0, .016)], PAPIER, n=20, rot=(90, 0, 0))
    anneau(e, (0, -.46, 2.02), .24, .022, LAITON, rot=(90, 0, 0), n=20)
    for k in range(12):
        a = k * .5236
        e.boite((math.sin(a) * .19, -.464, 2.02 + math.cos(a) * .19), (.012, .006, .045), NOIR, rot=(0, math.degrees(a), 0), bis=0)
    e.barre((0, -.47, 2.02), (.06, -.47, 2.14), .016, .006, NOIR, bis=0)
    e.barre((0, -.472, 2.02), (-.02, -.472, 2.19), .011, .006, NOIR, bis=0)
    e.boite((0, -.23, 2.38), (.74, .48, .1), SOMBRE, bis=.02)
    e.boite((0, -.23, 2.5), (.5, .36, .16), SOMBRE, bis=.02, taper=(.3, .8))
    for sx in (-1, 1):
        e.tour((sx * .3, -.4, 2.43), [(.03, 0), (.045, .06), (.02, .12), (0, .18)], OR, n=8)
    e.col_boite((0, -.22, 1.25), (.66, .44, 2.5))


# ------------------------------------------------------------------ chapelle
@piece("Banc_Chapelle", 78, sale=.35)
def _(e):
    """Banc d'église de 3.4 m à joues sculptées, face -Y (l'assise regarde -Y)."""
    for sx in (-1, 1):
        e.boite((sx * 1.66, 0, .5), (.09, .62, 1.0), SOMBRE, bis=.02, fil=2)
        e.boite((sx * 1.66, 0, 1.08), (.11, .4, .22), SOMBRE, bis=.03, taper=(1, .3))
        e.tour((sx * 1.66, 0, 1.2), [(.05, 0), (.075, .05), (.03, .12), (0, .16)], SOMBRE, n=8)
        e.boite((sx * 1.72, 0, .62), (.03, .36, .5), BOIS, bis=.015)
        e.boite((sx * 1.72, 0, .62), (.045, .2, .2), BOIS, rot=(45, 0, 0), bis=.012)
    for k in range(2):
        e.boite((0, -.17 + k * .23, .46), (3.24, .22, .05), BOIS, bis=.01, fil=0)
    for k in range(3):
        e.boite((0, .2 + k * .03, .6 + k * .185), (3.24, .03, .17), BOIS, rot=(-8, 0, 0), bis=.008, fil=0)
    e.boite((0, .29, 1.0), (3.24, .09, .06), SOMBRE, bis=.012, fil=0)
    e.boite((0, .0, .3), (3.24, .04, .26), SOMBRE, bis=.008, fil=0)
    e.boite((0, .36, .84), (3.2, .13, .03), BOIS, rot=(14, 0, 0), bis=.006, fil=0)
    e.col_boite((0, 0, .25), (3.4, .6, .5))
    e.col_boite((0, .27, .7), (3.4, .16, .9))


@piece("Autel_Chapelle", 79, sale=.4)
def _(e):
    """Autel de 3 m sur deux marches, antependium brodé, nappe, retable bas doré."""
    e.boite((0, -.6, .09), (5.0, 2.8, .18), CLAIRE, bis=.03)
    e.boite((0, -.45, .26), (4.0, 2.0, .17), CLAIRE, bis=.03)
    e.boite((0, -.3, .78), (2.8, .85, .88), CLAIRE, bis=.03)
    for sx in (-1, 1):
        for k in range(1):
            e.tour((sx * 1.3, -.76, .35), [(.09, 0), (.09, .08), (.06, .12), (.06, .68), (.1, .78), (.1, .85)], CLAIRE, n=10)
    e.boite((0, -.32, 1.27), (3.1, 1.05, .12), CLAIRE, bis=.03, fil=0)

    def nap(u, v):
        s = u * 1.9
        x = (v - .5) * 1.7
        if s < .98:
            return (x, .16 - s, 1.336)
        d = s - .98
        return (x + .02 * math.sin(d * 8), -.825 - .02 * math.sin(x * 13) * d, 1.336 - d)
    e.nappe(nap, 22, 24, DRAP)
    e.nappe(lambda u, v: ((v - .5) * .75, -.87 - .01 * math.sin(u * 5), 1.2 - u * .72), 8, 4, ROUGE)
    e.boite((0, -.885, .5), (.74, .012, .05), OR, bis=0)
    for a, b in (((0, 1.1), (0, .62)), ((-.14, .96), (.14, .96))):
        e.barre((a[0], -.89, a[1]), (b[0], -.89, b[1]), .04, .008, OR, bis=0)
    # retable : cinq arcs dorés
    e.boite((0, .12, 1.45), (2.9, .24, .22), SOMBRE, bis=.02)
    for k in range(5):
        x = -1.1 + k * .55
        h = 1.15 if k == 2 else .8
        e.boite((x, .14, 1.56 + h / 2), (.5, .1, h), OR, bis=.015)
        kp = (1, 5, 4, 6, 3)[k]
        ii, jj = kp % 4, kp // 4
        mu = (.34 / (h * .62)) / .8
        cu, du = (ii + .5) / 4, .121 * min(1, mu)
        vv1, vv0 = 1 - jj / 2 - .01, 1 - jj / 2 - .49
        e.plan((x, .084, 1.56 + h * .45), .34, h * .62, PORTRAITS, uv4=((cu - du, vv0), (cu + du, vv0), (cu + du, vv1), (cu - du, vv1)))
        for sx2 in (-1, 1):
            e.boite((x + sx2 * .19, .08, 1.56 + h * .45), (.04, .03, h * .7), OR, bis=.008)
        e.boite((x, .08, 1.56 + h * .8), (.4, .03, .05), OR, bis=.008)
        e.boite((x, .14, 1.56 + h), (.35, .1, .35), OR, rot=(0, 45, 0), bis=.015)
        e.tour((x, .14, 1.56 + h + .26), [(.03, 0), (.05, .05), (0, .16)], OR, n=8)
        for sx in (-1, 1):
            e.tour((x + sx * .26, .08, 1.56), [(.03, 0), (.022, .05), (.02, h - .05), (.03, h)], OR, n=7)
    e.col_boite((0, -.6, .09), (5.0, 2.8, .18))
    e.col_boite((0, -.45, .26), (4.0, 2.0, .17))
    e.col_boite((0, -.2, 1.1), (3.1, 1.1, 1.5))


@piece("Verriere", 80, sale=0)
def _(e):
    """Verrière gothique : arc brisé de pierre (3.4 × 6.4 m, 0.45 m de saillie) et vitrail peint lumineux ; pivot au sol, dos au mur ;
    allège à 1.4 m."""
    w, z0, z1 = 1.25, 1.5, 4.2           # demi-largeur, bas, naissance de l'arc
    H = 6.1
    n = 14

    def arc(t, dw=0.0):
        # t de 0 (bas gauche naissance) à 1 (naissance droite) : arc brisé ; dw : élargissement
        ww = w + dw
        R = 2 * ww * 1.02
        if t <= .5:
            cx = -ww + R
            a0 = math.pi
            a1 = math.pi - math.acos((cx) / R)
            a = a0 + (a1 - a0) * (t * 2)
            return Vector((cx + R * math.cos(a), 0, z1 + R * math.sin(a)))
        p = arc(1 - t, dw)
        return Vector((-p.x, 0, p.z))
    top = arc(.5).z
    # vitrail : éventail depuis l'axe
    cont = [Vector((-w, 0, z0))] + [arc(k / n) for k in range(n + 1)] + [Vector((w, 0, z0))]
    bm = bmesh.new()
    uv = bm.loops.layers.uv.verify()
    vs = [bm.verts.new(p + Vector((0, -.1, 0))) for p in cont]
    c = bm.verts.new((0, -.1, (z0 + z1) / 2 + .3))
    for i in range(len(vs)):
        f = bm.faces.new([vs[i], vs[(i + 1) % len(vs)], c])
        for l in f.loops:
            l[uv].uv = ((l.vert.co.x + w) / (2 * w) * .8 + .1, (l.vert.co.z - z0) / (top - z0) * .94 + .03)
    bm.normal_update()
    for f in bm.faces:
        if f.normal.y > 0:
            f.normal_flip()
    e._ajoute(bm, Matrix.Identity(4), VITRAIL, teinte=.5)
    e.boite((0, -.05, (z0 + top) / 2), (2 * w, .04, .1), NOIR, bis=0) if False else None
    # claveaux
    for k in range(n):
        a, b = (arc(k / n, .2)), (arc((k + 1) / n, .2))
        m = (a + b) / 2
        an = math.degrees(math.atan2(b.z - a.z, b.x - a.x))
        e.boite((m.x, -.22, m.z), ((b - a).length + .05, .44, .42), CLAIRE, rot=(0, -an, 0), bis=.03, jit=.006, fil=0)
    k = 0
    z = 1.28
    while z < z1 - .05:
        h = min(.5, z1 - z)
        for sx in (-1, 1):
            e.boite((sx * (w + .22 + (.05 if k % 2 else 0)), -.22, z + h / 2), (.44 + (.1 if k % 2 else 0), .44, h - .014), CLAIRE, bis=.03, jit=.006, fil=2)
        z += h
        k += 1
    e.boite((0, -.28, 1.36), (3.5, .56, .26), CLAIRE, bis=.03, fil=0)
    moulure(e, -1.6, 1.6, -.02, 1.05, [(.0, 0), (.05, .02), (.2, .16), (.24, .17), (.24, .22), (0, .23)], CLAIRE)
    for k in range(3):
        e.boite((-1.1 + k * 1.1, -.08, .62), (1.0, .16, .86), CLAIRE, bis=.03)
        e.boite((-1.1 + k * 1.1, -.175, .62), (.6, .04, .5), PIERRE, bis=.02)
    e.boite((0, -.12, .09), (3.5, .24, .18), CLAIRE, bis=.02)
    # meneaux
    for z in (2.35, 3.3, 4.2):
        e.barre((-w, -.125, z), (w, -.125, z), .03, .03, FER, bis=.004)
    e.boite((0, -.22, top + .42), (.34, .5, .5), CLAIRE, bis=.04, taper=(.75, 1))
    e.vide("Vitrail", (0, -.3, 3.6))
    for sx in (-1, 1):
        e.col_boite((sx * 1.47, -.22, 2.75), (.5, .44, 5.5))
    e.col_boite((0, -.2, .75), (3.5, .45, 1.5))


@piece("Prie_Dieu", 81, sale=.3)
def _(e):
    e.boite((0, -.3, .12), (.62, .4, .1), SOMBRE, bis=.015)
    e.boite((0, -.32, .2), (.56, .34, .09), ROUGE, bis=.035, seg=2)
    for sx in (-1, 1):
        e.boite((sx * .28, .05, .5), (.05, .34, .92), SOMBRE, bis=.012, fil=2)
    e.boite((0, .08, .5), (.52, .03, .8), BOIS, bis=.008, fil=2)
    e.boite((0, -.02, .98), (.68, .42, .04), BOIS, rot=(20, 0, 0), bis=.01, fil=0)
    livre(e, (0.12, -.06, 1.0), .05, .2, .28, rot=(0, 90, 12)) if False else pile_livres(e, .05, -.02, .995, 1)
    e.col_boite((0, -.1, .5), (.65, .7, 1.0))


# ------------------------------------------------------------------ portraits
def cadre(e, w, h, k=None, mat=OR, larg=.16, lacere=False, vide=False):
    """Cadre mouluré (pivot au centre, dos au mur à y = 0) et toile : cellule k de l'atlas des portraits."""
    P = [(0, .05), (.03, .09), (.07, .1), (.09, .06), (.13, .05), (.15, .075), (larg, .06), (larg, .0)]
    L = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
    for i in range(4):
        (xa, za), (xb, zb) = L[i], L[(i + 1) % 4]

        def Q(u, v, xa=xa, za=za, xb=xb, zb=zb):
            kk = v * (len(P) - 1)
            j = min(int(kk), len(P) - 2)
            t = kk - j
            d = P[j][0] * (1 - t) + P[j + 1][0] * t
            y = P[j][1] * (1 - t) + P[j + 1][1] * t
            A = Vector((xa * (w / 2 + d), -y, za * (h / 2 + d)))
            B = Vector((xb * (w / 2 + d), -y, zb * (h / 2 + d)))
            return A.lerp(B, u)
        e.nappe(Q, 1, len(P) - 1, mat, lisse=False)
    for sx in (-1, 1):
        for sz in (-1, 1):
            e.tour((sx * (w / 2 + .08), -.07, sz * (h / 2 + .08)), [(.13, 0), (.12, .03), (.06, .05), (.05, .075), (0, .085)], mat, n=8, rot=(90, 0, 0))
    for z in (-1, 1):
        e.boite((0, -.085, z * (h / 2 + .09)), (.3, .05, .2), mat, bis=.03)
    for x in (-1, 1):
        e.boite((x * (w / 2 + .09), -.085, 0), (.2, .05, .28), mat, bis=.03)
    if vide:
        e.boite((0, -.02, 0), (w, .02, h), SOMBRE, bis=0, fil=2)
        return
    i, j = k % 4, k // 4
    m = .004
    u0, u1 = i / 4 + m, (i + 1) / 4 - m
    v1, v0 = 1 - j / 2 - m, 1 - (j + 1) / 2 + m
    # cellule 4:5 ; cadrage selon les proportions de la toile
    ar = (w / h) / .8
    if ar < 1:
        c, d = (u0 + u1) / 2, (u1 - u0) / 2 * ar
        u0, u1 = c - d, c + d
    else:
        d = (v1 - v0) / ar
        v0 = v1 - d
    if not lacere:
        e.plan((0, -.045, 0), w, h, PORTRAITS, uv4=((u0, v0), (u1, v0), (u1, v1), (u0, v1)))
        return
    e.boite((0, -.015, 0), (w, .02, h), NOIR, bis=0)
    # toile lacérée : bandes obliques qui pendent
    n = 7
    for s in range(n):
        a, b = s / n, (s + 1) / n - .022

        def T(u, v, a=a, b=b, s=s):
            x = a + (b - a) * u
            xx = (x - .5) * w
            z = (v - .5) * h
            y = -.045
            xx += (v - .5) * .12
            xx = max(-w / 2, min(w / 2, xx))
            if 0 < s < n - 1:
                f = max(0, (.82 if s in (2, 3, 4) else .55) - v)
                y -= (f * .6 + .04 * math.sin(v * 9 + s)) * (1 if s % 2 else .7) * (.35 + (u if s % 2 else 1 - u)) * min(1, f * 2.6)
                z -= f * f * .25
                xx += (.1 if s % 2 else -.08) * f
            return (xx, y, z)
        e.nappe(T, 2, 10, PORTRAITS, uvf=lambda u, v, a=a, b=b: (u0 + (u1 - u0) * max(0, min(1, a + (b - a) * u + (v - .5) * .12 / w)), v0 + (v1 - v0) * v))


for _k in range(8):
    @piece(f"Portrait_{_k + 1}", 90 + _k, sale=0)
    def _(e, k=_k):
        cadre(e, 1.15 if k % 2 else 1.35, 1.5 if k % 2 else 1.75, k, mat=OR if k % 3 else SOMBRE)


@piece("Portrait_Grand", 98, sale=0)
def _(e):
    cadre(e, 1.9, 2.6, 7, larg=.2)
    e.boite((0, -.1, -1.6), (.7, .04, .16), LAITON, bis=.01)


@piece("Portrait_Lacere", 99, sale=0)
def _(e):
    cadre(e, 1.35, 1.75, 6, lacere=True)


@piece("Portrait_Lacere_2", 100, sale=0)
def _(e):
    cadre(e, 1.15, 1.5, 0, lacere=True, mat=SOMBRE)


for _n, _k in ((3, 1), (4, 3), (5, 4)):
    @piece(f"Portrait_Lacere_{_n}", 120 + _n, sale=0)
    def _(e, k=_k):
        cadre(e, 1.15 if k != 4 else 1.35, 1.5 if k != 4 else 1.75, k, lacere=True, mat=OR if k != 3 else SOMBRE)


@piece("Cadre_Vide", 101, sale=0)
def _(e):
    cadre(e, 1.15, 1.5, vide=True)


@piece("Gueridon_Urne", 104, sale=.3)
def _(e):
    e.boite((0, 0, .06), (.62, .62, .12), CLAIRE, bis=.02)
    e.boite((0, 0, .58), (.46, .46, .92), CLAIRE, bis=.02, taper=(.9, .9))
    e.boite((0, 0, 1.08), (.6, .6, .1), CLAIRE, bis=.025)
    e.tour((0, 0, 1.13), [(.12, 0), (.13, .03), (.05, .08), (.05, .13), (.16, .25), (.2, .42), (.17, .5), (.09, .56), (.1, .6), (.15, .64), (.13, .65), (.07, .6), (0, .58)], "M_Ens_Faience", n=16)
    for s in (-1, 1):
        e.tube([(s * .18, 0, 1.6), (s * .28, 0, 1.64), (s * .27, 0, 1.5), (s * .2, 0, 1.42)], .016, LAITON, n=6)
    e.col_boite((0, 0, .9), (.6, .6, 1.8))


@piece("Banquette", 105, sale=.25)
def _(e):
    for sx in (-1, 1):
        for sy in (-1, 1):
            pied_tourne(e, sx * .9, sy * .22, .34, .06, SOMBRE)
        e.boite((sx * .98, 0, .62), (.1, .56, .14), SOMBRE, bis=.03)
        e.barre((sx * .96, 0, .36), (sx * .99, 0, .6), .07, .4, SOMBRE, bis=.02)
    e.boite((0, 0, .36), (2.0, .56, .1), SOMBRE, bis=.015, fil=0)
    e.boite((0, 0, .46), (1.84, .52, .14), ROUGE, bis=.05, seg=2)
    for k in range(5):
        for sy in (-1, 1):
            e.tour((-.72 + k * .36, sy * .12, .525), [(.02, 0), (.012, .008), (0, .01)], OR, n=7)
    e.col_boite((0, 0, .3), (2.1, .58, .6))


# ------------------------------------------------------------------ salon renversé
@piece("Clavecin", 106, sale=.35)
def _(e):
    """Clavecin au couvercle relevé, 1.1 × 2.4 m, clavier vers -Y ; cordes rompues."""
    C = [(-.55, -1.2), (.55, -1.2), (.55, -.7), (.5, -.3), (.2, .3), (-.1, .75), (-.35, 1.1), (-.55, 1.1)]
    for a, b, dz in ((.78, 1.06, 0),):
        for i in range(len(C)):
            (xa, ya), (xb, yb) = C[i], C[(i + 1) % len(C)]
            e.barre((xa, ya, .92), (xb, yb, .92), .04, .28, SOMBRE, bis=.008, roulis=0) if abs(xa - xb) + abs(ya - yb) > .02 else None
    bm = bmesh.new()
    bm.faces.new([bm.verts.new((x, y, .8)) for x, y in C])
    e.maillage(bm, SOMBRE)
    bm = bmesh.new()
    bm.faces.new([bm.verts.new((x * .97, y * .97, .95)) for x, y in C[2:]] + [bm.verts.new((-.53, -.7, .95))])
    e.maillage(bm, BOIS)
    for k in range(18):
        x = -.5 + k * .054
        y1 = 1.0 if x < -.35 else (1.0 - (x + .35) * 1.55)
        if k in (7, 8, 12):
            e.tube([(x, -.65, 1.0), (x + .02, -.5, 1.12), (x + .1, -.42, 1.2)], .003, LAITON, n=3)
            continue
        e.cyl((x, -.65, 1.0), (x, y1, 1.0), .0025, LAITON, n=3)
    e.boite((0, -.98, .86), (1.04, .36, .05), SOMBRE, bis=.006)
    for k in range(28):
        x = -.49 + k * .0363
        e.boite((x, -1.02, .91), (.033, .26, .03), OS if k not in (9, 10, 19) else NOIR, bis=.003, rot=(0 if k != 14 else -9, 0, 0))
        if k % 7 in (0, 1, 3, 4, 5):
            e.boite((x + .018, -.96, .93), (.018, .15, .03), NOIR, bis=.003)
    for x, y in ((-.48, -1.1), (.48, -1.1), (-.42, .85)):
        pied_tourne(e, x, y, .78, .07, SOMBRE)
    # couvercle relevé
    bm = bmesh.new()
    th = math.radians(52)

    def pt(x, y, o):
        d = x + .55
        return (-.55 + math.cos(th) * d - math.sin(th) * o, y, 1.08 + math.sin(th) * d + math.cos(th) * o)
    cc = C[2:] + [(-.55, -.7)]
    A = [bm.verts.new(pt(x, y, 0)) for x, y in cc]
    B = [bm.verts.new(pt(x, y, .03)) for x, y in cc]
    bm.faces.new(A)
    bm.faces.new(B[::-1])
    for i in range(len(cc)):
        j = (i + 1) % len(cc)
        bm.faces.new([A[i], A[j], B[j], B[i]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    e.maillage(bm, ROUGE)
    e.barre((.5, -.2, 1.05), (.12, -.2, 1.86), .025, .025, BOIS)
    e.col_boite((0, 0, .55), (1.15, 2.4, 1.1))


@piece("Gueridon", 107, sale=.25)
def _(e):
    e.tour((0, 0, .72), [(0, 0), (.4, 0), (.45, .02), (.45, .045), (0, .05)], BOIS, n=18, lisse=False)
    e.tour((0, 0, .1), [(.05, 0), (.08, .08), (.04, .16), (.06, .3), (.035, .45), (.08, .6), (.08, .63)], SOMBRE, n=10)
    for k in range(3):
        a = k * 2.0944
        e.tube([(0, 0, .18), (math.cos(a) * .15, math.sin(a) * .15, .16), (math.cos(a) * .3, math.sin(a) * .3, .03)], [.035, .03, .035], SOMBRE, n=7)
    e.col_boite((0, 0, .38), (.8, .8, .76))


@piece("Lustre_Tombe", 108, sale=.3)
def _(e):
    """Lustre de fer tombé : couronne cassée, bougies, chaîne au sol."""
    M = rotm(14, 8, 0)
    R = .85
    n = 22
    P = [M @ Vector((math.cos(2 * math.pi * i / n) * R, math.sin(2 * math.pi * i / n) * R, 0)) + Vector((0, 0, .3)) for i in range(n + 1)]
    e.tube(P, .035, FER, n=7, bout=False)
    for k in range(6):
        a = k * 1.0472
        e.barre(M @ Vector((math.cos(a) * R, math.sin(a) * R, 0)) + Vector((0, 0, .3)), M @ Vector((0, 0, .7)) + Vector((0, 0, .3)), .03, .03, FER)
    for k in range(9):
        a = k * .698
        c = M @ Vector((math.cos(a) * R, math.sin(a) * R, .02)) + Vector((0, 0, .3))
        e.tour(c, [(.02, 0), (.06, .02), (.062, .03), (.03, .032), (.03, .05)], FER, n=8, rot=(14, 8, 0))
        if k % 3:
            e.tour(c + Vector((0, 0, .05)), [(.026, 0), (.027, e.rnd.uniform(.05, .14)), (0, .15)], CIRE, n=7, rot=(14, 8, 0))
    p = M @ Vector((0, 0, .72)) + Vector((0, 0, .3))
    pts = [p, p + Vector((.3, -.3, -.3)), p + Vector((.6, -.8, -.85)), Vector((1.1, -1.0, .03)), Vector((1.5, -.7, .03)), Vector((1.3, -.2, .03))]
    for i in range(len(pts) - 1):
        chaine(e, pts[i], pts[i + 1], 0, pas=.11, r=.014, larg=.036)
    for _ in range(10):
        a, d = e.rnd.uniform(0, 6.28), e.rnd.uniform(.9, 1.5)
        e.cyl((math.cos(a) * d, math.sin(a) * d, .025), (math.cos(a) * d + .1, math.sin(a) * d + .05, .025), .024, CIRE, n=7)
    e.col_boite((0, 0, .35), (1.6, 1.6, .6))


# ---------------------------------- draps jetés (simulation de tissu dans Blender)
def drape(e, boites, cote, haut, mat=DRAP, n=46, images=110):
    """Simule un drap carré de `cote` (x, y) lâché de `haut` sur des boîtes (c, s) et un sol ; ajoute le résultat."""
    sc = bpy.context.scene
    objs = []
    for c, s in list(boites) + [((0, 0, -.05), (6, 6, .1))]:
        bpy.ops.mesh.primitive_cube_add(size=1, location=c)
        o = bpy.context.object
        o.scale = s
        bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
        b = o.modifiers.new("b", "BEVEL")
        b.width = min(.04, min(s) * .2)
        b.segments = 2
        m = o.modifiers.new("c", "COLLISION")
        o.collision.thickness_outer = .015
        o.collision.cloth_friction = 12
        objs.append(o)
    bpy.ops.mesh.primitive_grid_add(x_subdivisions=n, y_subdivisions=int(n * cote[1] / cote[0]), size=1, location=(0, 0, haut))
    g = bpy.context.object
    g.scale = (cote[0], cote[1], 1)
    g.rotation_euler.z = math.radians(e.rnd.uniform(-9, 9))
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    for v in g.data.vertices:
        v.co.z += .02 * noise.noise(v.co * 3)
    cl = g.modifiers.new("t", "CLOTH")
    cl.settings.quality = 8
    cl.settings.mass = .35
    cl.settings.bending_stiffness = .12
    cl.settings.air_damping = 2
    cl.collision_settings.use_self_collision = True
    cl.collision_settings.self_distance_min = .012
    cl.collision_settings.distance_min = .012
    cl.collision_settings.collision_quality = 4
    cl.point_cache.frame_start = 1
    cl.point_cache.frame_end = images
    for f in range(1, images + 1):
        sc.frame_set(f)
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(g.evaluated_get(dg))
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    for v in bm.verts:
        v.co += g.location
        v.co.z = max(v.co.z, .012 + .004 * noise.noise(v.co * 8))
    bm.normal_update()
    if sum(f.normal.z for f in bm.faces) < 0:
        bmesh.ops.reverse_faces(bm, faces=bm.faces[:])
    uv = bm.loops.layers.uv.verify()
    ny = int(n * cote[1] / cote[0])
    e.maillage(bm, mat, lisse=True, axe=1)
    for o in objs + [g]:
        bpy.data.objects.remove(o, do_unlink=True)


@piece("Drap_Fauteuil", 109, sale=.3)
def _(e):
    """Fauteuil sous un drap (volume seul)."""
    drape(e, [((0, 0, .3), (.82, .8, .6)), ((0, .34, .75), (.78, .18, 1.5)), ((-.4, 0, .42), (.16, .74, .84)), ((.4, 0, .42), (.16, .74, .84))], (2.5, 2.6), 1.75, n=40)
    e.col_boite((0, .05, .4), (.95, .9, .8))
    e.col_boite((0, .34, .8), (.85, .25, 1.5))


@piece("Drap_Table", 110, sale=.3)
def _(e):
    drape(e, [((0, 0, .45), (1.7, 1.0, .9)), ((.3, .1, 1.0), (.4, .4, .3))], (3.0, 2.3), 1.3, n=38)
    e.col_boite((0, 0, .46), (1.75, 1.05, .92))


@piece("Drap_Statue", 111, sale=.3)
def _(e):
    """Silhouette haute sous un drap (statue voilée)."""
    drape(e, [((0, 0, .3), (.7, .7, .6)), ((0, 0, 1.0), (.5, .4, 1.6)), ((0, 0, 1.6), (.72, .34, .3)), ((0, -.03, 1.95), (.26, .28, .36))], (3.6, 3.6), 2.4, n=42, images=110)
    e.col_boite((0, 0, 1.05), (.8, .7, 2.1))


@piece("Verre_Brise", 113, sale=0)
def _(e):
    for _ in range(22):
        a, d = e.rnd.uniform(0, 6.28), e.rnd.uniform(.02, .6)
        c = Vector((math.cos(a) * d, math.sin(a) * d, .004 + e.rnd.uniform(0, .004)))
        bm = bmesh.new()
        s = e.rnd.uniform(.03, .11)
        vs = [bm.verts.new((math.cos(t + e.rnd.uniform(0, .5)) * s * e.rnd.uniform(.4, 1), math.sin(t) * s * e.rnd.uniform(.4, 1), 0)) for t in (0, 2.1, 4.2)]
        bm.faces.new(vs)
        bm.normal_update()
        e.maillage(bm, "M_Ens_Miroir", Matrix.Translation(c) @ Matrix.Rotation(e.rnd.uniform(0, 6), 4, "Z"))


@piece("Catafalque", 114, sale=.3)
def _(e):
    """Catafalque : cercueil drapé sur estrade, pour la variante funèbre de la chapelle."""
    e.boite((0, 0, .12), (3.0, 1.8, .24), CLAIRE, bis=.03)
    e.boite((0, 0, .3), (2.6, 1.4, .14), CLAIRE, bis=.03)
    drape(e, [((0, 0, .7), (2.1, .78, .66)), ((-.1, 0, 1.06), (1.6, .5, .12))], (3.6, 2.5), 1.4, mat=ROUGE, n=50) if False else None
    # cercueil à six pans
    C = [(-1.1, -.28), (-.5, -.42), (1.05, -.26), (1.05, .26), (-.5, .42), (-1.1, .28)]
    for z0, z1, k, m in ((.37, .88, 1.0, SOMBRE), (.88, 1.0, .9, BOIS)):
        bm = bmesh.new()
        A = [bm.verts.new((x * (1 if k == 1 else 1.03), y * (1 if k == 1 else 1.06), z0)) for x, y in C]
        B = [bm.verts.new((x * k, y * k * (1 if k == 1 else .86), z1)) for x, y in C]
        bm.faces.new(A[::-1])
        bm.faces.new(B)
        for i in range(6):
            j = (i + 1) % 6
            bm.faces.new([A[i], A[j], B[j], B[i]])
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=.012, segments=1, affect="EDGES", profile=.5)
        e.maillage(bm, m, axe=0)
    for a, b in (((-.75, 0), (.55, 0)), ((-.45, -.2), (-.45, .2))):
        e.barre((a[0], a[1], 1.015), (b[0], b[1], 1.015), .09, .03, OR, bis=.008)
    for sy in (-1, 1):
        for x in (-.6, .1, .75):
            anneau(e, (x, sy * (.42 - abs(x + .3) * .1), .62), .07, .012, LAITON, rot=(90, 0, 0), n=12)

    def ech(u, v):
        s = (u - .5) * 2.2
        x = .05 + v * .6
        z = 1.03 if abs(s) < .4 else 1.03 - (abs(s) - .4)
        y = s if abs(s) < .4 else math.copysign(.42 + .03 * math.sin(x * 15), s)
        return (x, y, max(.38, z))
    e.nappe(ech, 24, 6, ROUGE)
    e.col_boite((0, 0, .18), (3.0, 1.8, .36))
    e.col_boite((0, 0, .7), (2.3, .9, .66))


@piece("Papiers_Epars", 115, sale=0)
def _(e):
    """Feuilles envolées (3 × 2 m), sans collision."""
    for k in range(24):
        a, d, r = e.rnd.uniform(0, 6.28), abs(e.rnd.gauss(0, .65)), e.rnd.uniform(0, 6.28)
        c = Vector((math.cos(a) * d * 1.5, math.sin(a) * d, .006 + .0012 * k))
        b = e.rnd.uniform(.0, .07)
        R = Matrix.Rotation(r, 3, "Z")
        e.nappe(lambda u, v: c + R @ Vector(((u - .5) * .2, (v - .5) * .28, b * (abs(u - .5) * 2) ** 2)), 4, 1, PAPIER)
        for j in range(4):
            e.boite(c + R @ Vector((0, .08 - j * .045, .004)), (.13 - .03 * (j == 3), .006, .001), CUIR, rot=(0, 0, math.degrees(r)), bis=0)
