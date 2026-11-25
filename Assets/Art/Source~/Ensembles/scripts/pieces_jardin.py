# -*- coding: utf-8 -*-
"""Pièces de l'acte III (jardin) : uniquement ce qui manque aux kits. Les « Habillage_* » sont des feuillages peints à poser
sur les pièces de kit existantes (haie, kiosque, serre, fontaine), au même pivot."""
from ens_lib import *
from pieces_commun import anneau, chaine, clou, crane


# ------------------------------------------------------------------ habillages de kit
@piece("Habillage_Haie_6m", 130)
def _(e):
    """Feuillage de buis sur `Jardin_Haie_6m` (6.0 × 1.4 × 2.6 m environ) : flancs, dessus et bouts."""
    for x in [i * .42 - 2.9 for i in range(15)]:
        for z in (.45, 1.15, 1.8):
            for s in (-1, 1):
                k = e.rnd.uniform(.9, 1.2)
                e.carte((x + e.rnd.uniform(-.15, .15), s * (.9 + e.rnd.uniform(-.05, .1)), z + e.rnd.uniform(-.2, .2)), k * 1.15, k * 1.15, "chene" if e.rnd.random() < .22 else "buis",
                        (e.rnd.uniform(-.45, .45), s, e.rnd.uniform(-.2, .5)), e.rnd.uniform(-180, 180), normale=(e.rnd.uniform(-.3, .3), s, .5))
        for y in (-.45, .05, .5):
            k = e.rnd.uniform(.85, 1.2)
            e.carte((x + e.rnd.uniform(-.15, .15), y, 2.25 + e.rnd.uniform(-.12, .15)), k, k, "buis",
                    (e.rnd.uniform(-.4, .4), y + e.rnd.uniform(-.3, .3), 1), e.rnd.uniform(-180, 180), normale=(0, y * .6, 1))
    for sx in (-1, 1):
        for z in (.5, 1.2, 1.85):
            for y in (-.4, .4):
                e.carte((sx * 3.05, y, z), 1.0, 1.0, "buis", (sx, y * .5, .2), e.rnd.uniform(-40, 40), normale=(sx, 0, .4))
    for x in [i * .75 - 2.7 for i in range(8)]:
        for s in (-1, 1):
            e.touffe((x + e.rnd.uniform(-.2, .2), s * .95, 0), e.rnd.uniform(.35, .55), n=2)


@piece("Habillage_Lierre_4m", 131)
def _(e):
    """Rideau de lierre de 4 × 3.5 m à poser contre un mur, un treillage ou une colonnade (dos +Y)."""
    for i in range(34):
        x = e.rnd.uniform(-1.8, 1.8)
        z = 3.3 * (1 - abs(e.rnd.gauss(0, .38)))
        z = max(.5, min(3.3, z))
        k = e.rnd.uniform(.85, 1.35)
        e.carte((x, -.06 - .012 * (i % 9) , z), k, k, "lierre", (e.rnd.uniform(-.25, .25), -1, .15), e.rnd.uniform(-16, 16), normale=(0, -1, .5), plis=.08)
    for k in range(4):
        x = -1.4 + k * .95
        e.tube([(x, -.04, 0), (x + .1, -.05, .8), (x - .12, -.04, 1.7), (x + .08, -.05, 2.6), (x, -.04, 3.2)], [.045, .04, .032, .025, .014], ECORCE, n=6, rugo=.3)


@piece("Habillage_Kiosque", 132)
def _(e):
    """Rosiers grimpants et lierre pour `Jardin_Kiosque` (octogone d'environ 2.7 m de rayon au poteau, plancher à 0.9 m)."""
    for k in range(8):
        a = k * .7854 + .3927
        if k in (2, 6):
            continue
        x, y = math.cos(a) * 2.75, math.sin(a) * 2.75
        e.tube([(x * 1.05, y * 1.05, 0), (x * 1.03, y, 1.1), (x, y * 1.03, 2.2), (x * 1.02, y, 3.4)], [.05, .04, .03, .015], ECORCE, n=6, rugo=.3)
        for j in range(7):
            z = 1.0 + j * .42
            d = (math.cos(a) + e.rnd.uniform(-.5, .5), math.sin(a) + e.rnd.uniform(-.5, .5), .2)
            s = e.rnd.uniform(.7, 1.1)
            e.carte((x * 1.06, y * 1.06, z), s, s, "lierre" if j % 3 else "rose", d, e.rnd.uniform(-30, 30), normale=(d[0], d[1], .5))
        b = a + .7854
        x2, y2 = math.cos(b) * 2.75, math.sin(b) * 2.75
        for j in range(5):
            t = (j + .5) / 5
            e.carte((x * (1 - t) + x2 * t, y * (1 - t) + y2 * t, 3.75 - .25 * math.sin(t * 3.14)), 1.1, 1.1, "lierre", (math.cos(a + .39), math.sin(a + .39), 0), e.rnd.uniform(-12, 12), normale=(math.cos(a), math.sin(a), .6))
    for k in range(26):
        a = e.rnd.uniform(0, 6.28)
        if abs(math.sin(a)) > .94:
            continue
        e.touffe((math.cos(a) * 3.3, math.sin(a) * 3.3, 0), e.rnd.uniform(.4, .8), e.rnd.choice(("herbe", "herbe", "fougere")), n=2)


@piece("Lierre_Coulee", 133)
def _(e):
    """Petite coulée de lierre à poser sur une tombe, un socle ou un muret (1.2 m)."""
    for i in range(9):
        a = e.rnd.uniform(0, 6.28)
        e.carte((math.cos(a) * .25, math.sin(a) * .25, .12 + .05 * (i % 4)), .75, .75, "lierre", (math.cos(a), math.sin(a), .9), e.rnd.uniform(-60, 60), normale=(0, 0, 1))


# ------------------------------------------------------------------ plantes
@piece("Herbes_A", 134)
def _(e):
    for k in range(9):
        a, d = e.rnd.uniform(0, 6.28), math.sqrt(e.rnd.random()) * 1.3
        e.touffe((math.cos(a) * d, math.sin(a) * d, 0), e.rnd.uniform(.45, .9), n=3)


@piece("Fougeres", 135)
def _(e):
    for k in range(6):
        a, d = k * 1.1, .75 * (k > 0)
        for j in range(5):
            b = j * 1.2566 + k
            s = e.rnd.uniform(.75, 1.1)
            e.carte((math.cos(a) * d + math.cos(b) * .12, math.sin(a) * d + math.sin(b) * .12, 0), s, s, "fougere", (math.cos(b), math.sin(b), .55), pied=True, normale=(math.cos(b) * .4, math.sin(b) * .4, 1), plis=.12)


@piece("Ronces", 136)
def _(e):
    for k in range(10):
        a, d = e.rnd.uniform(0, 6.28), math.sqrt(e.rnd.random()) * .9
        s = e.rnd.uniform(.9, 1.5)
        b = e.rnd.uniform(0, 6.28)
        e.carte((math.cos(a) * d, math.sin(a) * d, -.05), s, s * .8, "ronce", (math.cos(b), math.sin(b), .3), pied=True, normale=(0, 0, 1))
    for k in range(4):
        e.touffe((e.rnd.uniform(-.7, .7), e.rnd.uniform(-.7, .7), 0), .45, n=2)


@piece("Rosiers", 137)
def _(e):
    for k in range(6):
        a = k * 1.05
        e.touffe((math.cos(a) * .55, math.sin(a) * .4, 0), e.rnd.uniform(.9, 1.25), "rose", n=2)
    for k in range(8):
        e.touffe((e.rnd.uniform(-.8, .8), e.rnd.uniform(-.6, .6), 0), .38, n=2)


@piece("Champignons_Cercle", 138)
def _(e):
    """Cercle de champignons pâles (rayon 1.6 m), faiblement lumineux par leur teinte."""
    for k in range(22):
        a = k * .2856 + e.rnd.uniform(-.1, .1)
        r = 1.6 + e.rnd.uniform(-.12, .12)
        for j in range(e.rnd.choice((1, 2, 3))):
            x, y = math.cos(a) * r + e.rnd.uniform(-.15, .15), math.sin(a) * r + e.rnd.uniform(-.15, .15)
            h = e.rnd.uniform(.14, .42)
            e.tube([(x, y, -.02), (x + .01, y, h * .6), (x + .02, y + .01, h)], [h * .16, h * .12, h * .1], OS, n=6)
            e.tour((x + .02, y + .01, h * .86), [(0, h * .05), (h * .42, 0), (h * .5, h * .06), (h * .36, h * .28), (0, h * .4)], "M_Ens_Champignon", n=10, rot=(e.rnd.uniform(-12, 12), e.rnd.uniform(-12, 12), 0))


@piece("Buis_Boule", 139)
def _(e):
    e.tour((0, 0, 0), [(0, .02), (.5, .05), (.68, .5), (.6, 1.0), (0, 1.22)], "M_Ens_Feuillage_Coeur", n=12, bruit=.2)
    e.buisson((0, 0, .62), (.72, .72, .66), "buis", n=46, taille=.72)
    e.col_boite((0, 0, .6), (1.15, 1.15, 1.2))


@piece("Topiaire", 140)
def _(e):
    """Topiaire en spirale de boules sur tige, dans un bac de pierre."""
    e.boite((0, 0, .0), (.85, .85, .7), CLAIRE, base=True, bis=.04, taper=(1.12, 1.12))
    e.boite((0, 0, .74), (1.0, 1.0, .1), CLAIRE, bis=.03)
    e.tour((0, 0, .78), [(.44, 0), (.2, .03), (0, .04)], TERRE, n=12, bruit=.2)
    e.tube([(0, 0, .75), (.02, 0, 1.6), (-.02, .01, 2.6), (0, 0, 3.3)], [.06, .055, .045, .03], ECORCE, n=7, rugo=.3)
    for z, r in ((1.35, .5), (2.25, .42), (3.0, .32)):
        e.tour((0, 0, z - r * .8), [(0, 0), (r * .7, r * .25), (r * .85, r * .8), (r * .6, r * 1.4), (0, r * 1.65)], "M_Ens_Feuillage_Coeur", n=10, bruit=.2)
        e.buisson((0, 0, z), r, "buis", n=24, taille=r * 1.25)
    e.col_boite((0, 0, .4), (1.0, 1.0, .8))
    e.col_boite((0, 0, 2.0), (.8, .8, 2.6))


@piece("Urne_Pierre", 141, sale=.5, mousse=.6)
def _(e):
    e.boite((0, 0, 0), (.8, .8, .16), CLAIRE, base=True, bis=.03)
    e.boite((0, 0, .16), (.6, .6, .62), CLAIRE, base=True, bis=.03)
    e.boite((0, 0, .78), (.74, .74, .1), CLAIRE, base=True, bis=.03)
    e.tour((0, 0, .88), [(.18, 0), (.2, .04), (.1, .1), (.1, .16), (.32, .3), (.4, .5), (.38, .56), (.48, .62), (.47, .67), (.36, .64), (0, .6)], CLAIRE, n=16)
    for k in range(5):
        a = k * 1.2566
        e.touffe((math.cos(a) * .18, math.sin(a) * .18, 1.48), e.rnd.uniform(.5, .75), "rose" if k % 2 else "fougere", n=2)
    e.col_boite((0, 0, .75), (.8, .8, 1.5))


def tronc(e, pts, r, n=12):
    e.tube(pts, r, ECORCE, n=n, rugo=.22)


@piece("Arbre_Chene_A", 142, sale=1.6)
def _(e):
    """Chêne dense de 11 m (traversable au-dessus de 4 m) : tronc noueux, racines, cinq branches, couronne de cartes."""
    tronc(e, [(0, 0, -.3), (.05, .02, .8), (.12, -.05, 2.2), (-.05, .05, 3.9), (.1, .1, 5.4), (0, 0, 7.2)], [.75, .52, .44, .4, .32, .16])
    for k in range(6):
        a = k * 1.05 + .2
        e.tube([(math.cos(a) * .25, math.sin(a) * .25, .75), (math.cos(a) * .6, math.sin(a) * .6, .25), (math.cos(a) * 1.15, math.sin(a) * 1.15, -.02), (math.cos(a) * 1.6, math.sin(a) * 1.6, -.15)], [.24, .2, .12, .05], ECORCE, n=7, rugo=.3)
    bouts = []
    for k in range(6):
        a = k * 1.05 + .5
        z = 3.6 + .55 * k
        L = 2.6 - .2 * k
        p = [Vector((0, 0, z)), Vector((math.cos(a) * L * .4, math.sin(a) * L * .4, z + .6)), Vector((math.cos(a + .2) * L, math.sin(a + .2) * L, z + 1.1 + .1 * k))]
        e.tube(p, [.2, .13, .05], ECORCE, n=7, rugo=.25)
        bouts.append(p[-1])
        q = p[1].lerp(p[2], .4)
        q2 = q + Vector((math.cos(a + 1) * .9, math.sin(a + 1) * .9, .7))
        e.tube([q, q2], [.08, .03], ECORCE, n=5)
        bouts.append(q2)
    bouts += [Vector((0, 0, 7.6)), Vector((.6, .4, 6.8)), Vector((-.7, -.3, 6.9))]
    for b in bouts:
        e.buisson(b + Vector((0, 0, .3)), (1.35, 1.35, 1.0), "chene", n=15, taille=1.75)
    e.col_boite((0, 0, 3), (.8, .8, 6))


@piece("Arbre_Chene_B", 143, sale=1.6)
def _(e):
    """Chêne penché et creux de 9 m."""
    tronc(e, [(0, 0, -.3), (.15, 0, .7), (.5, .05, 2.0), (.95, -.05, 3.4), (1.2, .1, 4.8), (1.1, .2, 6.2)], [.85, .62, .5, .42, .32, .15])
    for k in range(6):
        a = k * 1.05
        e.tube([(math.cos(a) * .3, math.sin(a) * .3, .7), (math.cos(a) * .8, math.sin(a) * .8, .2), (math.cos(a) * 1.4, math.sin(a) * 1.4, -.1)], [.26, .2, .07], ECORCE, n=7, rugo=.3)
    e.tour((.16, -.56, 1.15), [(0, 0), (.16, .03), (.21, .06), (0, .02)], NOIR, n=10, rot=(90, 0, 0), ech=(1, 1.7, 1))
    bouts = []
    for k in range(5):
        a = k * 1.2566 + .4
        z = 3.0 + .6 * k
        o = Vector((.6 + .12 * k, 0, z))
        p = [o, o + Vector((math.cos(a) * 1.1, math.sin(a) * 1.1, .6)), o + Vector((math.cos(a + .3) * 2.3, math.sin(a + .3) * 2.3, 1.0))]
        e.tube(p, [.19, .12, .04], ECORCE, n=7, rugo=.25)
        bouts += [p[-1], p[1] + Vector((0, 0, .8))]
    bouts.append(Vector((1.1, .2, 6.5)))
    for b in bouts:
        e.buisson(b, (1.3, 1.3, .95), "chene", n=15, taille=1.7)
    e.col_boite((.45, 0, 2), (.95, .9, 4.2), rot=(0, 14, 0))


@piece("Sapin", 144, sale=1.6)
def _(e):
    """Sapin sombre de 12 m, branches basses à 2 m."""
    tronc(e, [(0, 0, -.3), (.02, .01, 1.2), (.03, .02, 2.4)], [.42, .34, .3], n=9)
    e.tube([(.03, .02, 2.3), (-.02, 0, 6), (0, 0, 11.6)], [.28, .18, .03], "M_Ens_Feuillage_Coeur", n=8)
    z = 2.0
    k = 0
    while z < 11.4:
        R = 2.6 * (1 - (z - 2) / 10.2) + .25
        m = max(5, int(9 * R / 2.6) + 2)
        for i in range(m):
            a = 2 * math.pi * (i + .5 * (k % 2)) / m + e.rnd.uniform(-.2, .2)
            d = Vector((math.cos(a), math.sin(a), 0))
            t = Vector((-math.sin(a), math.cos(a), 0))
            c = d * R * .47 + Vector((0, 0, z - .14 * R))
            s = R * 1.15
            nn = (d * .45 + Vector((0, 0, 1))).normalized()
            e.carte(c, s * .95, s * 1.15, "sapin", (d * .35 + Vector((0, 0, 1))).normalized(), e.rnd.uniform(-10, 10), normale=nn, haut=d - Vector((0, 0, .35)))
        z += .5 + .12 * R
        k += 1
    e.col_boite((0, 0, 3), (.6, .6, 6))


@piece("Souche", 145, sale=.5, mousse=.8)
def _(e):
    e.tour((0, 0, -.1), [(.95, 0), (.72, .25), (.62, .6), (.6, .78)], ECORCE, n=16, bruit=.2, ferme=False)
    e.tour((0, 0, .6), [(.6, .08), (.45, .075), (0, .075)], BOIS, n=16, ferme=False)
    for k in range(5):
        a = k * 1.2566
        e.tube([(math.cos(a) * .5, math.sin(a) * .5, .35), (math.cos(a) * .95, math.sin(a) * .95, .08), (math.cos(a) * 1.4, math.sin(a) * 1.4, -.1)], [.2, .14, .04], ECORCE, n=7, rugo=.3)
    for k in range(5):
        a = k * .5
        e.boite((math.cos(a) * .52, math.sin(a) * .52, .82), (.14, .1, .34 - .05 * k), GRIS, rot=(e.rnd.uniform(-10, 10), e.rnd.uniform(-10, 10), e.rnd.uniform(0, 90)), taper=(.3, .5), bis=.01)
    for k in range(3):
        e.tour((.62 + .03 * k, -.25 + .16 * k, .28 + .12 * k), [(0, 0), (.14, .01), (.15, .04), (0, .06)], "M_Ens_Champignon", n=9)
    e.col_boite((0, 0, .4), (1.2, 1.2, .8))


@piece("Tronc_Couche", 146, sale=.4, mousse=.9)
def _(e):
    """Tronc abattu de 7 m, 0.9 m de diamètre, praticable (on court dessus), bout creux."""
    e.tube([(-3.5, 0, .5), (-2, .08, .47), (0, -.04, .46), (2, .1, .44), (3.4, .05, .42)], [.56, .5, .46, .42, .34], ECORCE, n=14, rugo=.16, bout=False)
    for x, r, s in ((-3.52, .56, -1), (3.42, .34, 1)):
        e.tour((x, 0 if s < 0 else .05, .5 if s < 0 else .42), [(r * .99, -.02), (r * .8, .0), (r * .55, -.25), (0, -.3)], SOMBRE, n=14, rot=(0, s * 90, 0), ferme=False)
    for x, a, L in ((-1.2, 60, 1.0), (.8, -50, .8), (2.2, 80, .7)):
        d = Vector((.3, math.cos(math.radians(a)) * L, math.sin(math.radians(abs(a))) * L))
        e.tube([(x, .0, .5), Vector((x, 0, .5)) + d * .6, Vector((x, 0, .5)) + d], [.14, .1, .05], ECORCE, n=7, rugo=.3)
    for k in range(9):
        e.touffe((-3 + k * .75, e.rnd.choice((-.55, .55)), 0), e.rnd.uniform(.35, .7), e.rnd.choice(("herbe", "fougere")), n=2)
    for k in range(5):
        e.tour((-2.4 + k * 1.0, -.42, .6), [(0, 0), (.1, .01), (.11, .03), (0, .05)], "M_Ens_Champignon", n=8)
    e.col_boite((0, 0, .46), (7, .8, .9))


# ------------------------------------------------------------------ mobilier de jardin
@piece("Banc_Pierre", 147, sale=.4, mousse=.6)
def _(e):
    e.boite((0, 0, .48), (2.2, .6, .14), CLAIRE, bis=.035, jit=.008, fil=0)
    for sx in (-1, 1):
        e.boite((sx * .8, 0, 0), (.3, .5, .42), CLAIRE, base=True, bis=.04, taper=(.8, .85))
        e.tour((sx * .8, -.26, .24), [(.1, 0), (.07, .03), (0, .04)], CLAIRE, n=10, rot=(90, 0, 0))
    for sx in (-1, 0, 1):
        e.boite((sx * .72, .26, .55), (.2, .12, .5), CLAIRE, base=True, bis=.03)
    e.boite((0, .27, 1.1), (2.3, .15, .16), CLAIRE, bis=.035, fil=0)
    e.col_boite((0, 0, .28), (2.2, .6, .56))
    e.col_boite((0, .27, .85), (2.3, .16, .7))


@piece("Banc_Pierre_Brise", 148, sale=.4, mousse=.6)
def _(e):
    e.boite((-.6, 0, .48), (1.1, .6, .14), CLAIRE, bis=.035, jit=.02, fil=0)
    e.boite((.62, .02, .28), (1.05, .6, .14), CLAIRE, rot=(4, 24, 6), bis=.035, jit=.02, fil=0)
    e.boite((-.85, 0, 0), (.3, .5, .42), CLAIRE, base=True, bis=.04, taper=(.8, .85))
    e.boite((.15, 0, 0), (.3, .5, .42), CLAIRE, base=True, bis=.04, taper=(.8, .85))
    e.boite((.95, -.35, .14), (.3, .5, .28), CLAIRE, rot=(0, 0, 35), bis=.04)
    for k in range(7):
        e.roche((e.rnd.uniform(0, 1.3), e.rnd.uniform(-.6, .5), 0), (.2, .16, .14), CLAIRE, n=9, bis=.015)
    e.touffe((.5, .1, 0), .6, n=3)
    e.col_boite((0, 0, .28), (2.2, .6, .56))


@piece("Socle_Statue", 149, sale=.6, mousse=.5)
def _(e):
    """Socle à gradins de 2.6 m, dessus à 1.3 m (1.5 × 1.5 m)."""
    e.boite((0, 0, 0), (2.6, 2.6, .22), PIERRE, base=True, bis=.04, jit=.01)
    e.boite((0, 0, .22), (2.1, 2.1, .2), CLAIRE, base=True, bis=.04, jit=.01)
    e.boite((0, 0, .42), (1.5, 1.5, .72), CLAIRE, base=True, bis=.03)
    for k in range(4):
        a = k * math.pi / 2
        e.boite((math.sin(a) * .76, -math.cos(a) * .76, .78), (1.0, .05, .44), PIERRE, rot=(0, 0, math.degrees(a)), bis=.02)
        e.boite((math.sin(a) * .785, -math.cos(a) * .785, .78), (.5, .04, .2), CLAIRE, rot=(0, 0, math.degrees(a)), bis=.02)
    e.boite((0, 0, 1.14), (1.75, 1.75, .16), CLAIRE, base=True, bis=.04)
    e.col_boite((0, 0, .11), (2.6, 2.6, .22))
    e.col_boite((0, 0, .32), (2.1, 2.1, .2))
    e.col_boite((0, 0, .85), (1.7, 1.7, .9))


@piece("Lanterne_Pierre", 150, sale=.4, mousse=.5)
def _(e):
    """Borne-lanterne de jardin, 1.7 m, repère Feu (lumière chaude)."""
    e.boite((0, 0, 0), (.6, .6, .18), CLAIRE, base=True, bis=.03)
    e.boite((0, 0, .18), (.34, .34, .85), CLAIRE, base=True, bis=.03, taper=(.85, .85))
    e.boite((0, 0, 1.03), (.56, .56, .1), CLAIRE, base=True, bis=.03)
    for sx in (-1, 1):
        for sy in (-1, 1):
            e.boite((sx * .2, sy * .2, 1.13), (.07, .07, .36), FER, base=True, bis=.008)
    for k in range(4):
        a = k * math.pi / 2
        e.boite((math.sin(a) * .2, -math.cos(a) * .2, 1.31), (.33, .012, .34), "M_Ens_Verre_Lampe", rot=(0, 0, math.degrees(a)), bis=0)
        for sx in (-.06, .06):
            e.boite((math.sin(a) * .208 + math.cos(a) * sx, -math.cos(a) * .208 + math.sin(a) * sx, 1.31), (.018, .016, .34), FER, rot=(0, 0, math.degrees(a)), bis=0)
    e.boite((0, 0, 1.49), (.62, .62, .26), FER, base=True, bis=.02, taper=(.15, .15))
    e.tour((0, 0, 1.74), [(.04, 0), (.06, .04), (0, .12)], FER, n=8)
    e.vide("Feu", (0, 0, 1.3))
    e.col_boite((0, 0, .85), (.56, .56, 1.7))


# ------------------------------------------------------------------ serre et jardinier
def pot(e, x, y, r=.18, h=.3, z=0, rot=(0, 0, 0), plante=None):
    e.tour((x, y, z), [(r * .66, 0), (r * .9, h * .78), (r * 1.08, h * .78), (r * 1.1, h), (r * .98, h), (r * .88, h * .86), (0, h * .84)], CUITE, n=12, rot=rot)
    if plante:
        e.touffe((x, y, z + h * .84), r * 4.2, plante, n=3)


@piece("Table_Rempotage", 151, sale=.4)
def _(e):
    """Table de rempotage 2.2 × 0.8 m : pots, terre répandue, arrosoir, outils."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            e.barre((sx * 1.0, sy * .3, 0), (sx * 1.0, sy * .3, .86), .09, .09, GRIS, jit=.006)
        e.barre((sx * 1.0, -.3, .2), (sx * 1.0, .3, .2), .07, .07, GRIS)
    for k in range(4):
        e.boite((0, -.3 + k * .2, .88), (2.25, .19, .05), GRIS, fil=0, jit=.008, rot=(0, e.rnd.uniform(-.5, .5), 0))
    for k in range(3):
        e.boite((0, -.2 + k * .2, .25), (2.0, .18, .03), GRIS, fil=0, jit=.008)
    e.boite((0, .38, 1.1), (2.2, .04, .4), GRIS, fil=0, jit=.008)
    e.tour((.2, -.05, .9), [(.36, 0), (.2, .06), (.06, .11), (0, .12)], TERRE, n=16, bruit=.35)
    for x, y, r, p in ((-.85, .1, .15, "fougere"), (-.5, -.15, .13, None), (-.45, .18, .16, "rose"), (.75, .15, .14, None), (.9, -.15, .12, "herbe")):
        pot(e, x, y, r, r * 1.6, .905, plante=p)
    pot(e, .5, -.22, .12, .2, 1.0, rot=(85, 0, 30))
    for x in (-.7, -.2, .5):
        for k in range(3):
            pot(e, x, .05, .15, .24, .27 + .07 * k)
    e.tour((-.1, .2, .905), [(.12, 0), (.13, .2), (.1, .24), (.09, .24), (.11, .05), (0, .04)], FER, n=12)
    e.tube([(.0, .2, 1.0), (.2, .2, 1.1), (.3, .2, 1.24)], [.03, .02, .016], FER, n=6)
    e.tube([(-.2, .2, 1.1), (-.28, .2, 1.2), (-.2, .2, 1.28), (-.08, .2, 1.16)], .012, FER, n=5)
    e.boite((.05, -.3, .915), (.06, .2, .012), FER, rot=(0, 0, 35), bis=.003)
    e.cyl((.13, -.21, .92), (.2, -.11, .925), .018, BOIS, n=6)
    e.col_boite((0, 0, .46), (2.25, .82, .92))


@piece("Pots_Tas", 152, sale=.3)
def _(e):
    for x, y, r, h in ((0, 0, .28, .44), (.5, .15, .2, .32), (-.45, .2, .22, .36), (.2, .5, .18, .3)):
        pot(e, x, y, r, h, plante=e.rnd.choice((None, "fougere", "rose")))
    pot(e, .55, -.4, .24, .4, .2, rot=(84, 0, 40))
    for k in range(9):
        a = e.rnd.uniform(0, 6.28)
        e.boite((-.4 + e.rnd.uniform(-.3, .3), -.4 + e.rnd.uniform(-.2, .2), .02), (.13, .1, .02), CUITE, rot=(e.rnd.uniform(-20, 20), e.rnd.uniform(-20, 20), e.rnd.uniform(0, 180)), bis=.006, jit=.02)
    e.tour((-.4, -.35, 0), [(.25, 0), (.12, .06), (0, .08)], TERRE, n=12, bruit=.35)
    e.col_boite((0, .15, .22), (1.2, .8, .44))


@piece("Serre_Vitres", 153)
def _(e):
    """Éclats de verre au sol et plantes envahissantes pour `Jardin_Serre_Ruine` (emprise 6 × 4 m)."""
    for _ in range(40):
        x, y = e.rnd.uniform(-2.9, 2.9), e.rnd.uniform(-1.9, 1.9)
        bm = bmesh.new()
        s = e.rnd.uniform(.06, .22)
        vs = [bm.verts.new((math.cos(t + e.rnd.uniform(0, .6)) * s * e.rnd.uniform(.4, 1), math.sin(t) * s * e.rnd.uniform(.4, 1), 0)) for t in (0, 2.1, 4.2)]
        bm.faces.new(vs)
        bm.normal_update()
        e.maillage(bm, "M_Ens_Miroir", Matrix.Translation((x, y, .015 + e.rnd.uniform(0, .01))) @ Matrix.Rotation(e.rnd.uniform(0, 6), 4, "Z"))
    for k in range(8):
        x, y = e.rnd.choice((-2.6, 2.6, e.rnd.uniform(-2.6, 2.6))), e.rnd.uniform(-1.7, 1.7)
        e.touffe((x, y, 0), e.rnd.uniform(.6, 1.2), e.rnd.choice(("fougere", "herbe", "ronce", "rose")), n=3)


@piece("Plante_Geante", 154)
def _(e):
    """Plante de serre devenue monstrueuse : tiges grosses comme le bras et feuilles de 1.6 m, 3.6 m de haut."""
    for k in range(5):
        a = k * 1.2566 + .3
        c, s = math.cos(a), math.sin(a)
        p = [Vector((c * .15, s * .15, 0)), Vector((c * .4, s * .4, 1.1)), Vector((c * .9, s * .9, 2.2 + .2 * k)), Vector((c * 1.6, s * 1.6, 2.5 + .2 * k))]
        e.tube(p, [.12, .1, .07, .03], "M_Ens_Tige", n=8, rugo=.2)
        for q, t in ((p[-1], 1.7), (p[2], 1.4)):
            e.carte(q, t, t, "fougere", (c, s, .8), e.rnd.uniform(-20, 20), normale=(c * .4, s * .4, 1))
            e.carte(q + Vector((-s, c, .2)) * .4, t * .8, t * .8, "chene", (c * .5 - s, s * .5 + c, .7), e.rnd.uniform(-40, 40), normale=(0, 0, 1))
    e.buisson((0, 0, .5), (.7, .7, .5), "fougere", n=14, taille=1.1, bas=False)
    e.col_boite((0, 0, 1), (.7, .7, 2))


def tas_bois(e, ecroule=False):
    """Tas de bûches rangées entre deux piquets : 3 m × 1.2 m × 1.5 m."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            e.barre((sx * 1.55, sy * .4, 0), (sx * (1.6 if not (ecroule and sx > 0) else 2.1), sy * .42, 1.75 if not (ecroule and sx > 0) else 1.1), .11, .11, GRIS, jit=.01)
    z, r = .0, .155
    k = 0
    while z < 1.45:
        n = 10 if k % 2 == 0 else 9
        for i in range(n):
            rr = r * e.rnd.uniform(.82, 1.02)
            x = -1.395 + (i + (0 if n == 10 else .5)) * .31
            if z > 1.1 and e.rnd.random() < .3:
                continue
            if ecroule and z > 1.5 - max(0, x + 1.3) * .62:
                continue
            L = e.rnd.uniform(1.0, 1.15)
            o = e.rnd.uniform(-.06, .06)
            e.cyl((x, o - L / 2, z + rr), (x, o + L / 2, z + rr), rr, ECORCE, n=9)
            for s in (-1, 1):
                e.tour((x, o + s * (L / 2 + .002), z + rr), [(rr * .93, 0), (rr * .9, .006), (0, .007)], BOIS, n=9, rot=(s * -90, 0, 0))
        z += r * 1.74
        k += 1
    if not ecroule:
        e.col_boite((0, 0, .75), (3.2, 1.15, 1.5))
        return
    e.col_boite((-.9, 0, .7), (1.4, 1.15, 1.4))
    e.col_boite((.7, 0, .3), (1.8, 1.15, .6))
    for k in range(13):      # bûches roulées devant et sur le côté
        rr, a = e.rnd.uniform(.11, .16), e.rnd.uniform(0, 3.14)
        c = Vector((e.rnd.uniform(.1, 2.5), e.rnd.uniform(-1.7, -.7), rr))
        if k > 9:
            c = Vector((1.9 + .25 * (k - 10), e.rnd.uniform(-.3, .3), rr))
        d = Vector((math.cos(a), math.sin(a), 0)) * .52
        e.cyl(c - d, c + d, rr, ECORCE, n=9)
        for s in (-1, 1):
            e.cyl(c + d * s, c + d * s * 1.008, rr * .92, BOIS, n=9, lisse=False)


@piece("Tas_Bois", 155, sale=.3)
def _(e):
    """Tas de bûches rangées entre deux piquets : 3 m × 1.2 m × 1.5 m."""
    tas_bois(e)


@piece("Tas_Bois_Ecroule", 155, sale=.3)
def _(e):
    """Tas de bûches écroulé d'un côté, bûches roulées devant."""
    tas_bois(e, True)


@piece("Billot_Hache", 156, sale=.3)
def _(e):
    e.tour((0, 0, 0), [(.46, 0), (.4, .15), (.38, .55), (.38, .6)], ECORCE, n=14, bruit=.08, ferme=False)
    e.tour((0, 0, .6), [(.38, 0), (0, .006)], BOIS, n=14, ferme=False)
    e.boite((.05, 0, .68), (.05, .16, .24), FER, rot=(0, -18, 0), taper=(1.2, .6), bis=.01)
    e.cyl((.08, 0, .74), (.75, .0, 1.12), .025, BOIS, n=7)
    for k in range(12):
        a, d = e.rnd.uniform(0, 6.28), e.rnd.uniform(.5, 1.2)
        e.boite((math.cos(a) * d, math.sin(a) * d, .06), (.12, .42, .11), BOIS, rot=(e.rnd.uniform(-15, 15), e.rnd.uniform(-20, 20), e.rnd.uniform(0, 180)), fil=1, jit=.015)
    for k in range(18):
        a, d = e.rnd.uniform(0, 6.28), e.rnd.uniform(.4, 1.1)
        e.boite((math.cos(a) * d, math.sin(a) * d, .012), (.05, .1, .014), BOIS, rot=(0, e.rnd.uniform(-20, 20), e.rnd.uniform(0, 180)), bis=0)
    e.col_boite((0, 0, .3), (.8, .8, .6))


@piece("Brouette", 157, sale=.35)
def _(e):
    for sy in (-1, 1):
        e.barre((-1.1, sy * .28, .62), (.62, sy * .2, .34), .07, .07, GRIS)
        e.barre((-.35, sy * .28, .0), (-.25, sy * .27, .5), .07, .07, GRIS)
    e.boite((0.02, 0, .46), (1.0, .62, .04), GRIS, rot=(0, 9, 0), fil=0)
    for sy in (-1, 1):
        e.boite((0, sy * .36, .64), (1.05, .04, .34), BOIS, rot=(sy * -14, 9, 0), fil=0)
    e.boite((.52, 0, .6), (.04, .66, .4), BOIS, rot=(0, 22, 0), fil=1)
    e.boite((-.5, 0, .68), (.04, .66, .24), BOIS, rot=(0, -5, 0), fil=1)
    anneau(e, (.82, 0, .28), .26, .035, GRIS, rot=(90, 0, 0), n=16)
    for k in range(4):
        a = k * .7854
        e.barre((.82 - math.cos(a) * .25, 0, .28 - math.sin(a) * .25), (.82 + math.cos(a) * .25, 0, .28 + math.sin(a) * .25), .04, .04, GRIS)
    e.cyl((.82, -.25, .28), (.82, .25, .28), .03, FER, n=8)
    e.tour((0, 0, .5), [(.46, 0), (.36, .14), (.14, .27), (0, .3)], TERRE, n=14, bruit=.25, ech=(1, .7, 1))
    for k in range(4):
        e.touffe((-.2 + .13 * k, .05 * (k % 2), .68), .4, e.rnd.choice(("herbe", "ronce")), n=2)
    e.col_boite((0, 0, .4), (1.5, .75, .8))


def outils(e, faux=True):
    """Faux, râteau, fourche et bêche appuyés à un mur (dos +Y)."""
    L = ((-.75, 1.75, "faux"), (-.25, 1.7, "rateau"), (.2, 1.6, "fourche"), (.65, 1.35, "beche"))
    for k, (x, h, t) in enumerate(L if faux else L[1:]):
        a, b = Vector((x - .08, -.45, 0 if t != "beche" else .3)), Vector((x + .06, -.05, h))
        if t in ("rateau", "fourche"):
            b, a = Vector((x + .06, -.06, h)), Vector((x - .06, -.45, 0))
        e.cyl(a, b, .019, BOIS, n=7)
        if t == "rateau":
            e.boite(b, (.46, .04, .04), BOIS, bis=.008)
            for j in range(8):
                e.cyl(b + Vector((-.2 + j * .057, 0, 0)), b + Vector((-.2 + j * .057, -.1, .02)), .008, FER, n=5)
        if t == "fourche":
            e.boite(b, (.24, .03, .03), FER, bis=.006)
            for j in range(4):
                e.tube([b + Vector((-.11 + j * .073, 0, 0)), b + Vector((-.11 + j * .073, -.03, .16)), b + Vector((-.11 + j * .073, 0, .32))], [.01, .009, .003], FER, n=5)
        if t == "beche":
            e.boite(a - Vector((.02, .03, .12)), (.22, .025, .32), FER, rot=(-18, 0, 0), bis=.008, taper=(1.1, 1))
            e.barre(b + Vector((-.09, 0, 0)), b + Vector((.09, 0, 0)), .035, .035, BOIS)
        if t == "faux":
            e.tube([b, b + Vector((-.2, -.06, .1)), b + Vector((-.5, -.1, .08)), b + Vector((-.8, -.12, -.08))], [.03, .035, .028, .004], FER, n=5)
            for q in (.45, .7):
                p = a.lerp(b, q)
                e.cyl(p, p + Vector((0, -.16, .04)), .015, BOIS, n=6)


@piece("Outils_Jardin", 158, sale=0)
def _(e):
    """Faux, râteau, fourche et bêche appuyés à un mur."""
    outils(e)


@piece("Outils_Sans_Faux", 158, sale=0)
def _(e):
    """Râteau, fourche et bêche appuyés à un mur : il manque la faux."""
    outils(e, False)


@piece("Appentis", 159, sale=.6, mousse=.4)
def _(e):
    """Appentis de jardinier de 3.6 × 2 m, 2.8 m : quatre poteaux, toit de planches moussues troué, fond ajouré."""
    for sx in (-1, 1):
        e.barre((sx * 1.7, -1.9, 0), (sx * 1.7, -1.9, 2.25), .15, .15, GRIS, bis=.02, jit=.012)
        e.barre((sx * 1.7, -.1, 0), (sx * 1.7, -.1, 2.8), .15, .15, GRIS, bis=.02, jit=.012)
        e.barre((sx * 1.7, -2.05, 2.25), (sx * 1.7, 0, 2.87), .12, .14, GRIS)
        e.barre((sx * 1.7, -1.9, 1.7), (sx * 1.2, -1.9, 2.2), .09, .09, GRIS)
    e.barre((-1.95, -1.9, 2.25), (1.95, -1.9, 2.25), .13, .13, GRIS)
    e.barre((-1.95, -.1, 2.8), (1.95, -.1, 2.8), .13, .13, GRIS)
    for k in range(13):
        if k in (4, 9):
            continue
        x = -1.92 + k * .32
        c = .0 if k != 8 else .7
        e.barre((x, -2.3 + c, 2.28 + c * .3), (x + e.rnd.uniform(-.02, .02), .06, 3.02), .3, .035, GRIS, jit=.012)
    for k in range(10):
        if k in (3, 6):
            continue
        x = -1.5 + k * .33
        e.boite((x, -.06, 1.35), (.27, .035, e.rnd.uniform(2.2, 2.7)), GRIS, rot=(0, e.rnd.uniform(-1.5, 1.5), 0), fil=2, jit=.01)
    for k in range(7):
        e.touffe((-1.6 + k * .55, e.rnd.uniform(-.1, .1), 0), .55, n=2)
    e.col_boite((0, -1.05, 2.65), (3.9, 2.4, .12), rot=(16.5, 0, 0))
    for sx in (-1, 1):
        for y in (-1.9, -.1):
            e.col_boite((sx * 1.7, y, 1.2), (.18, .18, 2.4))
    e.col_boite((0, -.06, 1.35), (3.4, .1, 2.6))


# ------------------------------------------------------------------ cimetière
@piece("Tombe_Penchee", 160, sale=.5, mousse=.8)
def _(e):
    """Stèle arrondie penchée, gravure, terre retournée, fleurs."""
    M = rotm(7, -6, 5)
    n = 10
    prof = [(-.45, 0), (-.45, .9)] + [(-math.cos(math.pi * k / n) * .45, .9 + math.sin(math.pi * k / n) * .4) for k in range(1, n)] + [(.45, .9), (.45, 0)]
    bm = bmesh.new()
    A = [bm.verts.new((x, -.11, z - .15)) for x, z in prof]
    B = [bm.verts.new((x, .11, z - .15)) for x, z in prof]
    bm.faces.new(A)
    bm.faces.new(B[::-1])
    for i in range(len(A)):
        j = (i + 1) % len(A)
        bm.faces.new([A[j], A[i], B[i], B[j]])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bmesh.ops.bevel(bm, geom=[x for x in bm.edges if x.calc_face_angle(0) > .6], offset=.03, segments=1, affect="EDGES", profile=.5)
    e.maillage(bm, CLAIRE, M.to_4x4())
    for k in range(4):
        e.boite(M @ Vector((0, -.112, .72 - k * .12)), (.5 - .06 * (k % 2), .01, .025), NOIR, rot=(7, -6, 5), bis=0)
    for a, b in (((0, 1.04), (0, .8)), ((-.09, .95), (.09, .95))):
        e.barre(M @ Vector((a[0], -.112, a[1])), M @ Vector((b[0], -.112, b[1])), .04, .015, NOIR, bis=0)
    e.tour((0, -.95, 0), [(.75, -.02), (.6, .1), (.3, .2), (0, .22)], TERRE, n=16, bruit=.22, ech=(.75, 1.25, 1))
    for k in range(5):
        e.roche((e.rnd.uniform(-.6, .6), e.rnd.uniform(-1.8, .3), 0), (.22, .18, .14), PIERRE, n=9, bis=.015)
    e.touffe((-.3, -.3, 0), .5, n=2)
    e.touffe((.35, -.25, 0), .6, "rose", n=2)
    e.col_boite((0, 0, .55), (.9, .25, 1.2), rot=(7, -6, 5))


@piece("Tombe_Ouverte", 161, sale=.4, mousse=.4)
def _(e):
    """Tombe vidée : dalle basculée, fosse noire peinte au ras du sol (aucun trou dans le sol), tas de terre, planches."""
    e.boite((0, -1.0, .06), (1.5, 2.4, .12), PIERRE, bis=.03, jit=.01)
    e.boite((0, -1.0, .13), (1.1, 2.0, .04), NOIR, bis=0)
    for sx in (-1, 1):
        e.boite((sx * .65, -1.0, .2), (.2, 2.4, .18), CLAIRE, bis=.03, jit=.01, fil=1)
    for y in (-2.1, .1):
        e.boite((0, y, .2), (1.1, .2, .18), CLAIRE, bis=.03, jit=.01, fil=0)
    e.boite((.95, -1.1, .42), (1.15, 2.1, .12), CLAIRE, rot=(0, 42, 8), bis=.03, jit=.015, fil=1)
    e.tour((-1.35, -1.0, 0), [(.8, -.02), (.6, .25), (.3, .48), (0, .54)], TERRE, n=16, bruit=.3, ech=(.85, 1.4, 1))
    for k in range(4):
        e.boite((-.2 + .15 * k, -1.2 + .3 * k, .2), (.2, .9, .035), GRIS, rot=(e.rnd.uniform(-12, 12), e.rnd.uniform(-10, 10), e.rnd.uniform(-40, 40)), fil=1, jit=.012)
    e.tube([(-.1, -.4, .12), (-.12, -.45, .28), (-.02, -.5, .4)], [.02, .016, .014], OS, n=6)
    for j in range(4):
        e.tube([(-.02, -.5, .4), (.0 + .025 * j, -.56, .46)], .007, OS, n=4)
    e.boite((0, .35, 0), (.95, .22, 1.3), CLAIRE, base=True, bis=.05, jit=.015, taper=(.75, 1))
    for a, b in (((0, 1.0), (0, .55)), ((-.16, .85), (.16, .85))):
        e.barre((a[0], .235, a[1]), (b[0], .235, b[1]), .06, .03, NOIR, bis=0)
    e.col_boite((0, -1, .15), (1.5, 2.4, .28))
    e.col_boite((0, .35, .65), (.95, .22, 1.3))
    e.col_boite((.95, -1.1, .42), (1.1, 2.0, .14), rot=(0, 42, 8))


@piece("Croix_Bois", 162, sale=.3, mousse=.3)
def _(e):
    e.barre((0, 0, -.1), (.08, .03, 1.35), .13, .08, GRIS, jit=.012, bis=.015)
    e.barre((-.34, .0, .92), (.46, .04, .98), .12, .07, GRIS, jit=.012, bis=.015)
    for z in (.91, .97):
        anneau(e, (.056, .02, z), .085, .014, JUTE, rot=(0, 10, 0), n=10, m=5)
    e.tour((0, -.6, 0), [(.5, -.02), (.4, .08), (0, .15)], TERRE, n=14, bruit=.25, ech=(.8, 1.3, 1))
    e.touffe((.2, -.1, 0), .5, n=2)
    e.col_boite((.04, 0, .65), (.2, .15, 1.3))


@piece("Grille_Cimetiere", 163, sale=.5, mousse=.2)
def _(e):
    """Clôture basse de fer forgé, 3 m × 1.3 m, sur muret de 0.35 m ; lances et volutes."""
    for k in range(3):
        e.boite((-1 + k, 0, .175), (.99, .32, .35), PIERRE, bis=.035, jit=.012, fil=0)
    for sx in (-1, 1):
        e.boite((sx * 1.4, 0, 0), (.34, .42, 1.35), CLAIRE, base=True, bis=.035, taper=(.92, .92))
        e.boite((sx * 1.4, 0, 1.35), (.44, .5, .12), CLAIRE, base=True, bis=.03)
    for z in (.5, 1.1):
        e.boite((0, 0, z), (2.5, .03, .045), FER, bis=.006)
    for k in range(11):
        x = -1.1 + k * .22
        e.cyl((x, 0, .34), (x, 0, 1.26), .013, FER, n=6)
        e.tour((x, 0, 1.24), [(.012, 0), (.035, .05), (0, .17)], FER, n=5, lisse=False, ech=(1, .5, 1))
    for k in range(10):
        x = -.99 + k * .22
        anneau(e, (x, 0, .8), .07, .009, FER, rot=(90, 0, 0), n=10, m=5)
    for k in range(6):
        e.touffe((-1.2 + k * .48, e.rnd.choice((-.24, .24)), 0), .5, n=2)
    e.col_boite((0, 0, .65), (3.0, .3, 1.3))


@piece("Corbeau", 164, sale=0)
def _(e):
    """Corbeau posé (0.35 m), à placer sur une tombe ou une branche ; pivot aux pattes."""
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=.1)
    for v in bm.verts:
        v.co.y *= 1.6
        v.co.z *= .95
        if v.co.y > 0:
            v.co.x *= 1 - v.co.y * 2.2
            v.co.z *= 1 - v.co.y * 2.0
    e.maillage(bm, NOIR, Matrix.Translation((0, .02, .17)) @ rotm(-28, 0, 0).to_4x4(), lisse=True)
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=.058)
    e.maillage(bm, NOIR, Matrix.Translation((.02, -.1, .29)), lisse=True)
    e.tube([(.02, -.14, .285), (.025, -.2, .275), (.028, -.23, .255)], [.024, .015, .002], "M_Ens_Ardoise", n=6)
    e.barre((0, .12, .12), (0, .3, .05), .09, .02, NOIR, bis=.005)
    for s in (-1, 1):
        e.cyl((s * .035, 0, 0), (s * .035, .01, .1), .007, "M_Ens_Ardoise", n=5)
        e.nappe(lambda u, v, s=s: (s * (.095 - .03 * v), -.06 + .3 * u, .25 - .14 * u - .13 * v * (1 - u * .4)), 4, 2, NOIR)


# ------------------------------------------------------------------ kiosque : mobilier
@piece("Table_Ronde_Fer", 165, sale=.3)
def _(e):
    """Petite table de jardin en fer et deux chaises, service à thé abandonné."""
    e.tour((0, 0, .72), [(0, 0), (.44, 0), (.46, .012), (.45, .025), (0, .025)], FER, n=20, lisse=False)
    for k in range(3):
        a = k * 2.0944
        e.tube([(0, 0, .72), (math.cos(a) * .1, math.sin(a) * .1, .45), (math.cos(a) * .2, math.sin(a) * .2, .25), (math.cos(a) * .36, math.sin(a) * .36, 0)], .018, FER, n=6)
    for cx, cy, r, tombe in ((.95, .1, 90, 0), (-.8, -.45, -60, 1)):
        M = Matrix.Translation((cx, cy, .23 if tombe else 0)) @ rotm(0, 0, r).to_4x4() @ (rotm(82, 0, 0).to_4x4() if tombe else Matrix.Identity(4))
        P = lambda *q: M @ Vector(q)
        for sx in (-1, 1):
            for sy in (-1, 1):
                e.cyl(P(sx * .18, sy * .18, 0), P(sx * .16, sy * .16, .45), .014, FER, n=6)
            e.tube([P(sx * .16, .17, .45), P(sx * .17, .2, .75), P(sx * .1, .22, .98), P(0, .22, 1.03)], .014, FER, n=6)
        anneau(e, P(0, 0, .45), .2, .016, FER, rot=(82, 0, r) if tombe else (0, 0, 0), n=16)
        e.tour(P(0, 0, .45), [(0, 0), (.19, 0), (.19, .01)], FER, n=14, rot=(82, 0, r) if tombe else (0, 0, 0))
        for k in range(3):
            e.tube([P(-.07 + .07 * k, .18, .5), P(-.07 + .07 * k, .205, .8), P(0, .22, .95)], .009, FER, n=5)
    e.tour((.05, .05, .745), [(.05, 0), (.09, .04), (.1, .1), (.06, .15), (.03, .16), (.02, .19), (0, .2)], "M_Ens_Faience", n=12)
    e.tube([(.14, .05, .8), (.2, .05, .84), (.22, .05, .9)], [.016, .012, .01], "M_Ens_Faience", n=6)
    for x, y, t in ((-.2, .1, 0), (.12, -.24, 80)):
        e.tour((x, y, .745 if not t else .78), [(.02, 0), (.04, .02), (.045, .05), (.038, .05), (0, .02)], "M_Ens_Faience", n=10, rot=(t, 0, 20))
    e.col_boite((0, 0, .37), (.8, .8, .74))


@piece("Pupitres_Musique", 166, sale=.2)
def _(e):
    """Trois pupitres et un violon abandonnés (orchestre disparu)."""
    for x, y, r, t in ((-.7, 0, 20, 0), (.2, .3, -10, 0), (.9, -.3, 60, 75)):
        M = Matrix.Translation((x, y, .02 if t else 0)) @ rotm(0, 0, r).to_4x4() @ rotm(t, 0, 0).to_4x4()
        P = lambda *q: M @ Vector(q)
        for k in range(3):
            a = k * 2.0944
            e.cyl(P(0, 0, .28), P(math.cos(a) * .26, math.sin(a) * .26, .0), .012, FER, n=6)
        e.cyl(P(0, 0, .25), P(0, 0, 1.1), .014, FER, n=6)
        for k in range(3):
            e.cyl(P(-.2 + .2 * k, -.06, .98), P(-.14 + .14 * k, .04, 1.3), .008, FER, n=5)
        e.cyl(P(-.22, -.07, .97), P(.22, -.07, .97), .012, FER, n=5)
        e.cyl(P(-.15, .035, 1.28), P(.15, .035, 1.28), .008, FER, n=5)
        if not t:
            e.nappe(lambda u, v: P((u - .5) * .42, -.075 + .1 * v + .02 * math.sin(u * 6.3), .985 + .3 * v), 8, 1, PAPIER)
    bm = bmesh.new()
    n = 20
    vs = [bm.verts.new(((.1 + .025 * math.cos(2 * (2 * math.pi * k / n))) * math.cos(2 * math.pi * k / n), (.19) * math.sin(2 * math.pi * k / n), 0)) for k in range(n)]
    f = bm.faces.new(vs)
    r = bmesh.ops.extrude_face_region(bm, geom=[f])
    for v in [g for g in r["geom"] if isinstance(g, bmesh.types.BMVert)]:
        v.co.z += .05
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    M = Matrix.Translation((-.1, -.6, .01)) @ rotm(0, 0, 35).to_4x4()
    e.maillage(bm, BOIS, M, axe=1)
    e.barre(M @ Vector((0, .15, .05)), M @ Vector((0, .44, .06)), .025, .025, SOMBRE, bis=.005)
    e.barre(M @ Vector((0, -.1, .07)), M @ Vector((0, .42, .075)), .02, .003, NOIR, bis=0)
    e.cyl((.25, -.7, .015), (.75, -.45, .015), .006, BOIS, n=5)


# ------------------------------------------------------------------ eau
@piece("Eau_Disque", 167, sale=0)
def _(e):
    """Disque d'eau noire de 1 m de rayon (puits, fontaine) : échelle libre, sans collision."""
    e.tour((0, 0, 0), [(1, 0), (0, .001)], EAU, n=28, ferme=False)


@piece("Nenuphars", 168, sale=0)
def _(e):
    for k in range(11):
        a, d = e.rnd.uniform(0, 6.28), math.sqrt(e.rnd.random()) * 1.4
        r = e.rnd.uniform(.12, .24)
        n = 12
        bm = bmesh.new()
        c = bm.verts.new((0, 0, 0))
        vs = [bm.verts.new((math.cos(.3 + 5.7 * i / n) * r, math.sin(.3 + 5.7 * i / n) * r, .008)) for i in range(n + 1)]
        for i in range(n):
            bm.faces.new([c, vs[i], vs[i + 1]])
        bm.normal_update()
        e.maillage(bm, "M_Ens_Tige", Matrix.Translation((math.cos(a) * d, math.sin(a) * d, .003 * k)) @ Matrix.Rotation(e.rnd.uniform(0, 6), 4, "Z"))
