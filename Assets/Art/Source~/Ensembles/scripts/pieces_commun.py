# -*- coding: utf-8 -*-
"""Petits objets communs et pièces de l'acte I (caves)."""
from ens_lib import *


# ------------------------------------------------------------------ outils
def chandelle(e, x, y, z, r, h, coule=True, feu=True, i=1):
    n = 9
    prof = [(r * 1.02, 0), (r * 0.98, h * 0.5), (r, h * 0.9), (r * 0.82, h), (r * 0.4, h - r * 0.18), (0, h - r * 0.2)]
    e.tour((x, y, z), prof, CIRE, n=n, bruit=0.12)
    if coule:
        for _ in range(3):
            a = e.rnd.uniform(0, 6.28)
            l = h * e.rnd.uniform(0.25, 0.6)
            p = Vector((x + math.cos(a) * r * 0.98, y + math.sin(a) * r * 0.98, z + h * 0.96))
            e.tube([p, p - Vector((0, 0, l * 0.6)), p - Vector((0, 0, l))], [r * 0.2, r * 0.17, r * 0.24], CIRE, n=5)
        e.tour((x, y, z - 0.002), [(r * 1.75, 0), (r * 1.6, 0.012), (r * 1.1, 0.02), (r * 0.9, 0.03)], CIRE, n=9, bruit=0.5, ferme=False)
    e.cyl((x, y, z + h - r * 0.25), (x + r * 0.05, y, z + h + 0.02), 0.005, NOIR, n=5)
    if feu:
        e.tour((x + r * .05, y, z + h + .012), [(0, 0), (.018, .016), (.023, .042), (.014, .085), (.006, .12), (0, .15)], FLAMME, n=7, teinte=.5)
        e.tour((x + r * .05, y, z + h + .014), [(0, 0), (.009, .012), (.011, .034), (.005, .07), (0, .095)], "M_Ens_Flamme_Coeur", n=6, teinte=.5)
        e.vide(f"Feu_{i}", (x, y, z + h + 0.045))


def chaine(e, a, b, fleche=0.0, pas=0.085, r=0.011, larg=0.032, mat=FER):
    """Chaîne de maillons entre a et b, avec une flèche."""
    a, b = Vector(a), Vector(b)
    L = (b - a).length + fleche * 1.2
    n = max(2, int(L / pas))
    P = [a.lerp(b, i / n) - Vector((0, 0, math.sin(math.pi * i / n) * fleche)) for i in range(n + 1)]
    for i in range(n):
        c, d = (P[i] + P[i + 1]) / 2, (P[i + 1] - P[i]).normalized()
        s = Vector((0, 0, 1)) if abs(d.z) < 0.8 else Vector((1, 0, 0))
        x = d.cross(s).normalized()
        if i % 2:
            x = d.cross(x).normalized()
        l = pas * 0.72
        pts = [c - d * l + x * 0, c - d * l * 0.5 + x * larg, c + d * l * 0.5 + x * larg, c + d * l, c + d * l * 0.5 - x * larg, c - d * l * 0.5 - x * larg, c - d * l]
        e.tube(pts, r, mat, n=5, bout=False, lisse=True)
    return P


def anneau(e, c, R, r, mat=FER, rot=(0, 0, 0), n=14, m=6, ouvert=0):
    M = rotm(*rot)
    k = n - ouvert
    pts = [Vector(c) + M @ Vector((math.cos(2 * math.pi * i / n) * R, math.sin(2 * math.pi * i / n) * R, 0)) for i in range(k + 1)]
    e.tube(pts, r, mat, n=m, bout=bool(ouvert))


def planches(e, x0, x1, y, z, prof, ep, n, mat, jeu=0.008, jit=0.006, trou=()):
    """Rangée de n planches posées à plat (fil selon Y)."""
    w = (x1 - x0) / n
    for i in range(n):
        if i in trou:
            continue
        L = prof + e.rnd.uniform(-0.02, 0.02)
        e.boite((x0 + w * (i + 0.5), y + e.rnd.uniform(-0.01, 0.01), z + e.rnd.uniform(-jit, jit)), (w - jeu, L, ep), mat, fil=1, jit=0.004,
                rot=(e.rnd.uniform(-0.4, 0.4), 0, e.rnd.uniform(-0.3, 0.3)))


def clou(e, c, axe="y", r=0.014, mat=FER):
    d = {"y": (0, -0.012, 0), "x": (0.012, 0, 0), "z": (0, 0, 0.012), "-x": (-0.012, 0, 0)}[axe]
    e.cyl(c, Vector(c) + Vector(d), r, mat, r2=r * 0.6, n=6, lisse=False)


def livre(e, c, l, p, h, rot=(0, 0, 0), k=None, ouvert=False):
    """Livre debout : l épaisseur (x), p profondeur (y), h hauteur (z), dos vers -Y, c = milieu du dessous."""
    k = e.rnd.randrange(16) if k is None else k
    M = Matrix.Translation(Vector(c)) @ rotm(*rot).to_4x4()
    uc = (k + 0.5) / 16
    ep = min(0.012, l * 0.2)
    bm = bmesh.new()
    uv = bm.loops.layers.uv.verify()

    def q(pts, us):
        f = bm.faces.new([bm.verts.new(p) for p in pts])
        for lo, u in zip(f.loops, us):
            lo[uv].uv = u
    x0, x1, y0, y1 = -l / 2, l / 2, -p / 2, p / 2
    u0, u1 = k / 16 + 0.002, (k + 1) / 16 - 0.002
    b = p * 0.05
    # dos bombé (3 pans), plats, chants de couverture
    prof = [(x0, y0 + b), (x0 + l * 0.25, y0), (x1 - l * 0.25, y0), (x1, y0 + b)]
    for i in range(3):
        (xa, ya), (xb, yb) = prof[i], prof[i + 1]
        ua, ub = u0 + (u1 - u0) * (i / 3), u0 + (u1 - u0) * ((i + 1) / 3)
        q([(xa, ya, 0), (xb, yb, 0), (xb, yb, h), (xa, ya, h)], [(ua, 0), (ub, 0), (ub, 1), (ua, 1)])
    um = uc - 0.012, uc + 0.012
    for x, s in ((x0, -1), (x1, 1)):
        pts = [(x, y1, 0), (x, y0 + b, 0), (x, y0 + b, h), (x, y1, h)]
        us = [(um[1], 0.02), (um[0], 0.02), (um[0], 0.06), (um[1], 0.06)]
        if s > 0:
            pts.reverse(); us.reverse()
        q(pts, us)
        # intérieur du plat et chant
        xi = x - s * ep
        pts = [(x, y1, 0), (x, y1, h), (xi, y1, h), (xi, y1, 0)]
        if s < 0:
            pts.reverse()
        q(pts, [(uc, 0.02)] * 4)
        pts = [(x, y0 + b, h), (xi, y0 + b, h), (xi, y1, h), (x, y1, h)]
        if s > 0:
            pts = pts
        else:
            pts.reverse()
        q(pts[::-1] if s > 0 else pts[::-1], [(uc, 0.02)] * 4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    e._ajoute(bm, M, LIVRES, teinte=0.5)
    # tranche de papier
    e.maillage(_bloc((x1 - x0) - 2 * ep, p - b - 0.015, h - 0.02), PAPIER, M @ Matrix.Translation((0, b / 2 - 0.005, h / 2)))


def _bloc(x, y, z):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1)
    bm.transform(Matrix.Diagonal((x, y, z, 1)))
    return bm


def pile_livres(e, x, y, z, n, rmax=25):
    for _ in range(n):
        h = e.rnd.uniform(0.04, 0.085)
        L, P = e.rnd.uniform(0.26, 0.36), e.rnd.uniform(0.19, 0.25)
        a = e.rnd.uniform(-rmax, rmax)
        # couché sur le côté : épaisseur selon Z
        M = rotm(0, -90, 0)
        # livre debout (l, p, h) tourné de -90 autour de Y : l -> z, h -> x
        R = rotm(0, 0, a) @ rotm(0, 90, 0)
        c = Vector((x, y, z)) + rotm(0, 0, a) @ Vector((-L / 2, 0, h / 2))
        eu = R.to_euler()
        livre(e, c, h, P, L, rot=[math.degrees(v) for v in eu])
        z += h + 0.001
    return z


def crane(e, c, s=1.0, rot=(0, 0, 0)):
    M = Matrix.Translation(Vector(c)) @ rotm(*rot).to_4x4() @ Matrix.Diagonal((s, s, s, 1))
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=0.1)
    for v in bm.verts:
        p = v.co
        p.y *= 1.22
        if p.y < -0.03 and p.z < 0.0:
            p.x *= 0.78
        if p.z < -0.045:
            p.z = -0.045 + (p.z + 0.045) * 0.3
        # orbites
        for sx in (-1, 1):
            d = (Vector((p.x, p.y, p.z)) - Vector((sx * 0.037, -0.105, 0.0))).length
            if d < 0.036:
                p.y += 0.045 * (1 - d / 0.036) + 0.012
    e.maillage(bm, OS, M @ Matrix.Translation((0, 0, 0.1)), lisse=True)
    for sx in (-1, 1):
        e.tour(M @ Vector((sx * 0.037 , -0.098, 0.1)), [(0.024 * s, 0), (0.02 * s, 0.008 * s), (0, 0.01 * s)], NOIR, n=8, rot=(90 + rot[0], rot[1], rot[2]))
    e.maillage(_bloc(0.07, 0.05, 0.05), OS, M @ Matrix.Translation((0, -0.085, 0.042)))
    for i in range(5):
        e.maillage(_bloc(0.002, 0.004, 0.028), NOIR, M @ Matrix.Translation((-0.026 + i * 0.013, -0.1105, 0.042)))
    e.maillage(_bloc(0.018, 0.02, 0.03), NOIR, M @ Matrix.Translation((0, -0.102, 0.078)))


def os_long(e, a, b, r=0.016):
    a, b = Vector(a), Vector(b)
    e.tube([a, a.lerp(b, 0.12), a.lerp(b, 0.5), a.lerp(b, 0.88), b], [r * 1.7, r, r * 0.85, r, r * 1.7], OS, n=7)


def flamme_braise(e, c, r, n=16):
    for _ in range(n):
        a, d = e.rnd.uniform(0, 6.28), r * math.sqrt(e.rnd.random())
        s = e.rnd.uniform(0.07, 0.13)
        e.roche((c[0] + math.cos(a) * d, c[1] + math.sin(a) * d, c[2]), (s, s, s * 0.8), BRAISE if e.rnd.random() < 0.6 else NOIR, n=8, bis=0.004, enfonce=0.2)


def sac(e, c, r=0.28, h=0.6, rot=(0, 0, 0), ouvert=False, mat=JUTE):
    """Sac de toile : fond large affaissé, plis rayonnants vers le col, col noué ou roulé."""
    M = Matrix.Translation(Vector(c)) @ rotm(*rot).to_4x4()
    s = e.rnd.uniform(0, 50)
    penche = Vector((e.rnd.uniform(-.05, .05), e.rnd.uniform(-.05, .05), 0))
    prof = [(.02, 0), (.72, 0), (1.02, .07), (1.12, .2), (1.1, .36), (.98, .54), (.8, .7), (.55, .82)]
    prof += [(.62, .9), (.74, .93), (.78, .88), (.7, .84), (.4, .8), (.02, .8)] if ouvert else [(.26, .9), (.17, .94), (.16, .96), (.3, 1.04), (.44, 1.12), (.36, 1.13), (.1, 1.0)]
    n = len(prof) - 1

    def P(u, v):
        k = v * n
        i = min(int(k), n - 1)
        t = k - i
        rr = (prof[i][0] * (1 - t) + prof[i + 1][0] * t) * r
        z = (prof[i][1] * (1 - t) + prof[i + 1][1] * t) * h
        a = u * 2 * math.pi
        f = min(1, max(0, (v - .3) / .35))
        rr *= 1 + f * .11 * math.sin(a * 8 + s + 1.5 * math.sin(v * 5)) + .13 * noise.noise(Vector((math.cos(a) * 1.4 + s, math.sin(a) * 1.4, v * 2.2)))
        return M @ (Vector((math.cos(a) * rr, math.sin(a) * rr * .88, max(0, z))) + penche * z * 2)
    e.nappe(P, 30, n, mat, ferme_u=True)
    if not ouvert:
        anneau(e, M @ (Vector((0, 0, h * .95)) + penche * h * 1.9), r * .2, .02, CUIR, rot=rot, n=10, m=5)


# ------------------------------------------------------------------ lumières et vaisselle
@piece("Bougies_B", 2, sale=0)
def _(e):
    for i, (x, y, r, h) in enumerate([(0, 0, .05, .34), (.12, .03, .04, .22), (-.11, .05, .045, .13), (.03, .13, .035, .28), (-.04, -.12, .04, .08), (.17, -.09, .03, .12)]):
        chandelle(e, x, y, 0, r, h, i=i + 1)


@piece("Candelabre_Table", 3, sale=0)
def _(e):
    e.tour((0, 0, 0), [(0.13, 0), (0.13, 0.015), (0.09, 0.03), (0.05, 0.05), (0.03, 0.09), (0.045, 0.12), (0.025, 0.16), (0.02, 0.3), (0.04, 0.33), (0.02, 0.36), (0.018, 0.42)], LAITON, n=12)
    for k, sx in enumerate((-1, 1)):
        e.tube([(0, 0, .3), (sx * .07, 0, .27), (sx * .15, 0, .29), (sx * .18, 0, .36)], [.014, .013, .012, .012], LAITON, n=6)
    for k, (x, z) in enumerate(((0, .42), (-.18, .36), (.18, .36))):
        e.tour((x, 0, z), [(0.012, 0), (0.05, 0.015), (0.052, 0.022), (0.024, 0.025), (0.026, 0.05)], LAITON, n=10)
        chandelle(e, x, 0, z + .05, .02, .15 - .02 * k, i=k + 1)


@piece("Cierges", 5, sale=0.6)
def _(e):
    """Porte-cierges d'église : herse de fer à trois gradins."""
    for sx in (-1, 1):
        for sy in (-1, 1):
            e.barre((sx * .55, sy * .22, 0), (sx * .5, sy * .18, .8), .03, .03, FER)
    for k, (y, z) in enumerate(((-.2, .8), (0, .95), (.2, 1.1))):
        e.boite((0, y, z), (1.2, .16, .025), FER)
        e.boite((0, y - .08, z + .03), (1.2, .014, .06), FER, bis=.004)
        for i in range(7):
            x = -.51 + i * .17 + e.rnd.uniform(-.01, .01)
            e.cyl((x, y, z + .01), (x, y, z + .04), .03, LAITON, n=8)
            if e.rnd.random() < .85:
                chandelle(e, x, y, z + .035, .019, e.rnd.uniform(.06, .28), coule=False, feu=e.rnd.random() < .75, i=k * 7 + i + 1)
    for sx in (-1, 1):
        e.barre((sx * .5, .2, .8), (sx * .5, .2, 1.1), .025, .025, FER)
        e.barre((sx * .5, 0, .8), (sx * .5, 0, .95), .025, .025, FER)
    e.col_boite((0, 0, .6), (1.2, .55, 1.2))


@piece("Vaisselle_A", 6, sale=0)
def _(e):
    for x, y in ((0, 0), (.34, .06), (-.36, .03)):
        e.tour((x, y, 0), [(.06, 0), (.07, .006), (.12, .016), (.125, .02), (.07, .012), (0, .01)], ETAIN, n=14)
    e.tour((.17, .2, 0), [(.045, 0), (.045, .008), (.012, .02), (.011, .1), (.04, .13), (.047, .19), (.042, .19), (.034, .14), (0, .13)], ETAIN, n=10)
    e.tour((-.2, .22, 0), [(.045, 0), (.045, .008), (.012, .02), (.011, .1), (.04, .13), (.047, .19), (.042, .19), (.034, .14), (0, .13)], ETAIN, n=10)
    e.boite((.16, -.02, .006), (.012, .2, .006), ETAIN, rot=(0, 0, 8), bis=.002)
    e.boite((-.18, -.02, .006), (.014, .18, .006), ETAIN, rot=(0, 0, -6), bis=.002)
    e.roche((0, 0, .012), (.12, .09, .07), CUIR, n=12, bis=.01)
    e.roche((.34, .05, .012), (.09, .07, .05), JUTE, n=10, bis=.008)


@piece("Vaisselle_B", 7, sale=0)
def _(e):
    e.tour((0, 0, 0), [(.06, 0), (.07, .006), (.12, .016), (.125, .02), (.07, .012), (0, .01)], ETAIN, n=14, rot=(0, 0, 0))
    e.tour((.36, -.1, .012), [(.06, 0), (.07, .006), (.12, .016), (.125, .02), (.07, .012), (0, .01)], ETAIN, n=14, rot=(7, 0, 30))
    e.tour((-.3, .05, .045), [(.045, 0), (.045, .008), (.012, .02), (.011, .1), (.04, .13), (.047, .19), (.042, .19), (.034, .14), (0, .13)], ETAIN, n=10, rot=(88, 0, 40))
    e.tour((.2, .18, .1), [(.07, 0), (.08, .02), (.1, .12), (.075, .2), (.05, .25), (.06, .28), (.052, .28), (.04, .24), (0, .2)], CUITE, n=12, rot=(86, 0, 60))
    e.roche((.02, 0, .012), (.15, .1, .06), OS, n=14, bis=.01)


@piece("Banquet", 8, sale=0)
def _(e):
    """Milieu de table : volaille rassise, coupe de fruits, soupière."""
    e.tour((0, 0, 0), [(.12, 0), (.14, .02), (.3, .035), (.32, .05), (.26, .045), (0, .03)], ETAIN, n=18, ech=(1.25, .85, 1))
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=2, radius=.17)
    for v in bm.verts:
        v.co.x *= 1.25
        v.co.z *= .8
        v.co *= 1 + .12 * noise.noise(v.co * 9)
    e.maillage(bm, CUIR, Matrix.Translation((0, 0, .15)), lisse=True)
    for s in (-1, 1):
        e.tube([(.1, s * .1, .16), (.2, s * .13, .22), (.29, s * .11, .25)], [.06, .04, .02], CUIR, n=8)
        os_long(e, (.28, s * .11, .25), (.36, s * .1, .28), .012)
    e.tour((-.62, .02, 0), [(.08, 0), (.08, .01), (.03, .03), (.03, .08), (.18, .13), (.2, .17), (.17, .15), (0, .12)], LAITON, n=14)
    for k in range(9):
        a = k * .7
        r = .1 if k < 7 else .03
        e.roche((-.62 + math.cos(a) * r, .02 + math.sin(a) * r, .14 + (.06 if k >= 7 else 0)), (.085, .085, .08), [ROUGE, CUITE, JUTE][k % 3], n=14, bis=.012, enfonce=.1)
    e.tour((.64, 0, 0), [(.09, 0), (.1, .02), (.16, .08), (.17, .15), (.14, .17), (.15, .18), (.1, .23), (.03, .25), (.035, .28), (0, .3)], ETAIN, n=14)


@piece("Chope_Cruche", 9, sale=0)
def _(e):
    for x, y, r in ((0, 0, 0), (.16, .1, 40)):
        e.tour((x, y, 0), [(.048, 0), (.05, .02), (.046, .12), (.04, .12), (.038, .05), (0, .05)], BOIS, n=10)
        for z in (.03, .09):
            e.tour((x, y, z), [(.051, 0), (.052, .012)], FER, n=10, ferme=False)
        a = math.radians(r)
        c = Vector((x + math.cos(a) * .05, y + math.sin(a) * .05, .065))
        d = Vector((math.cos(a), math.sin(a), 0))
        e.tube([c + Vector((0, 0, .035)), c + d * .035 + Vector((0, 0, .03)), c + d * .035 - Vector((0, 0, .03)), c - Vector((0, 0, .035))], .008, BOIS, n=5)
    e.tour((-.2, .05, 0), [(.06, 0), (.07, .02), (.095, .1), (.085, .18), (.045, .24), (.042, .29), (.055, .32), (.046, .32), (.034, .28), (0, .24)], CUITE, n=12, bruit=.04)
    e.tube([(-.2, .13, .12), (-.2, .18, .18), (-.2, .15, .26), (-.2, .095, .28)], .012, CUITE, n=6)


@piece("Des_Cartes", 10, sale=0)
def _(e):
    for k, (x, y, a) in enumerate(((0, 0, 10), (.02, .03, 35), (.05, .01, -20), (-.09, .05, 70), (.3, -.05, 12), (.31, -.02, -8))):
        e.boite((x, y, .004 + k * .002), (.07, .11, .0015), PAPIER, rot=(0, 0, a), bis=0)
    for x, y, a in ((.15, .1, 20), (.2, .14, 50)):
        e.boite((x, y, .013), (.026, .026, .026), OS, rot=(0, 0, a), bis=.004)
    for _ in range(14):
        e.cyl((.12 + e.rnd.gauss(0, .05), -.1 + e.rnd.gauss(0, .04), .002), (.12, -.1, .0035), .016, OR, n=8) if False else \
            e.tour((.12 + e.rnd.gauss(0, .05), -.1 + e.rnd.gauss(0, .04), e.rnd.choice((0, .004))), [(.016, 0), (.016, .004)], OR, n=8, lisse=False)


@piece("Crane", 11, sale=0)
def _(e):
    crane(e, (0, 0, 0))


@piece("Os_Tas", 12, sale=.2)
def _(e):
    crane(e, (0, -.1, 0), rot=(0, 8, 15))
    crane(e, (.28, .08, -.01), rot=(20, 70, 40))
    for k in range(9):
        a = e.rnd.uniform(0, 6.28)
        x, y = e.rnd.gauss(0, .2), e.rnd.gauss(.1, .15)
        L = e.rnd.uniform(.14, .22)
        z = .02 + .03 * (k % 3)
        os_long(e, (x - math.cos(a) * L, y - math.sin(a) * L, z), (x + math.cos(a) * L, y + math.sin(a) * L, z + .02))
    for k in range(5):
        a = -1.2 + k * .25
        e.tube([(-.3 + k * .05, .25, .02), (-.3 + k * .05, .18, .12), (-.22 + k * .05, .08, .15), (-.16 + k * .05, .02, .05)], .009, OS, n=5)


@piece("Squelette_Assis", 13, sale=.2)
def _(e):
    """Squelette assis contre un mur (dos au +Y), tête tombée sur l'épaule."""
    crane(e, (.06, -.08, .62), rot=(25, 22, 18))
    P = [(0, .02, .06), (0, .05, .2), (0, .06, .36), (.01, .03, .52), (.03, -.01, .63)]
    e.tube(P, [.04, .03, .03, .028, .025], OS, n=7, rugo=.4)
    for k in range(6):
        z = .25 + k * .05
        w = .15 - abs(k - 3) * .012
        for s in (-1, 1):
            e.tube([(0, .06, z + .02), (s * w * .8, .04, z + .02), (s * w, -.05, z - .01), (s * w * .55, -.12, z - .03)], .0095, OS, n=5)
    e.boite((0, .03, .07), (.26, .12, .1), OS, bis=.03, jit=.02)
    for s, pl in ((-1, 0), (1, 1)):
        g = Vector((s * .16, -.4, .3 if pl else .07))
        os_long(e, (s * .1, 0, .08), g, .022)
        os_long(e, g, (s * (.2 if pl else .3), -.62 if pl else -.8, .04), .018)
        e.boite((s * (.2 if pl else .31), (-.67 if pl else -.86), .03), (.07, .15, .04), OS, bis=.015)
        ep = Vector((s * .2, .02, .54))
        e.tube([(0, .03, .56), ep], .012, OS, n=5)
        co = Vector((s * .28, -.04, .28))
        os_long(e, ep, co, .015)
        ma = Vector((s * .2, -.25, .1)) if s < 0 else Vector((.36, -.16, .03))
        os_long(e, co, ma, .012)
        for j in range(4):
            e.tube([ma, ma + Vector((s * .012 * (j - 1.5), -.07, -.02))], .006, OS, n=4)
    anneau(e, (-.21, -.22, .13), .04, .012, FER, rot=(60, 0, 0), n=10)


# ------------------------------------------------------------------ bois, contenants
@piece("Baquet", 15, sale=.35)
def _(e):
    n = 18
    for i in range(n):
        a = 2 * math.pi * i / n
        e.boite((math.cos(a) * .42, math.sin(a) * .42, .22), (.03, .146, .44), GRIS, rot=(0, 5, math.degrees(a)), fil=2, bis=.006, jit=.004)
    e.tour((0, 0, .3), [(.405, 0), (.405, .004), (0, .005)], EAU, n=18)
    for z, r in ((.07, .43), (.33, .452)):
        e.tour((0, 0, z), [(r, 0), (r + .004, .045)], FER, n=20, ferme=False)
    e.col_boite((0, 0, .22), (.8, .8, .44))


def caisse(e, c, s, rz=0, ouverte=False, mat=BOIS):
    x, y, z = s
    M = Matrix.Translation(Vector(c)) @ rotm(0, 0, rz).to_4x4()

    def b(p, t, fil, m=mat, bis=.008):
        e.boite(M @ Vector(p), t, m, rot=(0, 0, rz), fil=fil, bis=bis, jit=.004)
    n = max(2, round(z / .16))
    for i in range(n):
        h = z / n
        for sy in (-1, 1):
            b((0, sy * (y / 2 - .015), h * (i + .5)), (x - .02, .026, h - .01), 0)
        for sx in (-1, 1):
            b((sx * (x / 2 - .015), 0, h * (i + .5)), (.026, y - .07, h - .01), 1)
    for sx in (-1, 1):
        for sy in (-1, 1):
            b((sx * (x / 2 - .05), sy * (y / 2 + .008), z / 2), (.09, .024, z), 2, SOMBRE)
            b((sx * (x / 2 + .008), sy * (y / 2 - .045), z / 2), (.024, .085, z - .006), 2, SOMBRE)
            for zz in (.07, z - .07):
                clou(e, M @ Vector((sx * (x / 2 - .05), sy * (y / 2 + .02), zz)) - Vector((0, 0, 0)), r=.013)
    b((0, 0, .02), (x - .06, y - .06, .03), 0)
    if not ouverte:
        m = max(2, round(y / .2))
        for i in range(m):
            b((0, -y / 2 + y / m * (i + .5), z + .012), (x + .03, y / m - .012, .026), 0)


@piece("Caisse_Ouverte", 18, sale=.35)
def _(e):
    caisse(e, (0, 0, 0), (1.0, .7, .55), ouverte=True)
    for k in range(26):
        a = e.rnd.uniform(0, 6.28)
        p = Vector((e.rnd.uniform(-.4, .4), e.rnd.uniform(-.25, .25), .42))
        e.tube([p, p + Vector((math.cos(a) * .08, math.sin(a) * .08, .09)), p + Vector((math.cos(a) * .2, math.sin(a) * .2, .07))], [.006, .006, .003], JUTE, n=4)
    for k in range(4):
        e.tour((-.3 + k * .2, e.rnd.uniform(-.1, .1), .3), [(.05, 0), (.055, .12), (.03, .18), (.018, .22), (.018, .3), (.024, .31), (0, .31)], VERRE, n=9, rot=(e.rnd.uniform(-15, 15), e.rnd.uniform(-20, 20), 0))
    e.boite((.2, -.46, .3), (1.02, .03, .55), BOIS, rot=(-24, 0, 4), fil=0)
    e.col_boite((0, 0, .29), (1.05, .75, .58))


@piece("Sacs_A", 19, sale=.4)
def _(e):
    sac(e, (0, 0, 0), .3, .62)
    sac(e, (.55, .1, 0), .28, .55, rot=(0, 10, 0))
    sac(e, (.25, .3, .22), .27, .5, rot=(-76, 0, 20))
    sac(e, (-.42, .22, 0), .3, .5, ouvert=True)
    e.tour((-.42, .22, .36), [(.2, 0), (.15, .06), (0, .09)], "M_Ens_Grain", n=14, bruit=.1)
    e.col_boite((.1, .12, .28), (1.4, .7, .56))


@piece("Sacs_B", 20, sale=.4)
def _(e):
    sac(e, (0, 0, .24), .25, .72, rot=(0, 88, 0))
    sac(e, (-.05, .5, .24), .25, .72, rot=(0, 92, 6))
    sac(e, (.02, .25, .62), .25, .7, rot=(0, 88, -5))
    sac(e, (.95, -.3, 0), .26, .52, rot=(0, -14, 0))
    e.tour((1.15, -.62, 0), [(.28, 0), (.16, .04), (.06, .08), (0, .09)], "M_Ens_Grain", n=16, bruit=.35)
    e.col_boite((.35, .25, .36), (.95, 1.0, .72))


@piece("Jarres", 21, sale=.35)
def _(e):
    for x, y, s, r in ((0, 0, 1, (0, 0, 0)), (.5, .1, .8, (0, 0, 0)), (.28, -.42, .75, (80, 0, 30)), (-.4, .15, .65, (0, 0, 0))):
        p = [(.12, 0), (.16, .03), (.26, .25), (.28, .42), (.2, .6), (.11, .68), (.1, .74), (.15, .79), (.14, .81), (.09, .76), (0, .7)]
        e.tour((x, y, .2 if r[0] else 0), [(a * s, b * s) for a, b in p], CUITE, n=16, rot=r, bruit=.03)
        if not r[0]:
            anneau(e, (x, y, .66 * s), .125 * s, .016, JUTE, n=12, m=5)
    e.col_boite((.05, .05, .4), (1.2, .7, .8))


@piece("Corde", 22, sale=.2)
def _(e):
    pts = []
    for k in range(60):
        a = k * .55
        r = .22 + .03 * math.sin(k * .31) - .0012 * k
        pts.append((math.cos(a) * r, math.sin(a) * r, .025 + .0035 * k))
    pts += [(.3, -.1, .03), (.45, -.2, .02), (.62, -.14, .02), (.75, -.26, .02)]
    e.tube(pts, .022, JUTE, n=6)


# ------------------------------------------------------------------ cachot
def barreaux(e, x0, x1, z0, z1, y=0.0, pas=.2, plie=()):
    n = round((x1 - x0) / pas)
    for i in range(n + 1):
        x = x0 + (x1 - x0) * i / n
        if i in plie:
            d = plie[i]
            e.tube([(x, y, z0), (x + d * .2, y - abs(d) * .2, z0 + (z1 - z0) * .3), (x + d, y - abs(d) * .5, z0 + (z1 - z0) * .45), (x + d * .2, y - abs(d) * .1, z0 + (z1 - z0) * .66), (x, y, z1)], .021, FER, n=7)
        else:
            e.cyl((x, y, z0), (x, y, z1), .021, FER, n=8)


@piece("Grille_2m_Pliee", 25, sale=.8)
def _(e):
    barreaux(e, -.9, .9, 0, 3.4, plie={4: -.17, 5: .17, 3: -.07, 6: .08})
    for z in (.18, 2.3, 3.25):
        e.boite((0, 0, z), (2.0, .03, .09), FER, bis=.006)
    for sx in (-1, 1):
        e.barre((sx * 1.0, 0, 1.2), (sx * .35, -.02, 1.18), .03, .09, FER)
        e.barre((sx * .35, -.02, 1.18), (sx * .22, -.16, 1.1 + sx * .06), .03, .085, FER)
    e.col_boite((0, 0, 1.7), (2, .12, 3.4))


@piece("Chaines_Murales", 29, sale=0)
def _(e):
    """Plaque, deux chaînes et fers ; pivot au mur (y = 0), plaque à 1.9 m."""
    for x in (-.45, .45):
        e.boite((x, -.02, 1.95), (.2, .04, .24), FER, bis=.012)
        for sx in (-1, 1):
            for sz in (-1, 1):
                clou(e, (x + sx * .07, -.04, 1.95 + sz * .08), r=.016)
        anneau(e, (x, -.09, 1.9), .055, .014, FER, rot=(90, 0, 90), n=12)
        bas = Vector((x * 1.15, -.1, .82))
        chaine(e, (x, -.09, 1.84), bas, 0)
        anneau(e, bas - Vector((0, 0, .09)), .07, .018, FER, rot=(80, 0, 20), n=14)
        e.boite(bas - Vector((.07, 0, .09)), (.03, .05, .06), FER, bis=.006)


@piece("Chaines_Plafond", 30, sale=0)
def _(e):
    """Trois chaînes et un crochet pendus d'une poutre ; pivot au point d'accroche (z = 0), pendent de 3 m."""
    for x, L, y in ((-.5, 2.3, 0), (.05, 3.0, .06), (.6, 1.7, -.05)):
        anneau(e, (x, y, -.05), .06, .014, FER, rot=(90, 0, 0), n=12)
        chaine(e, (x, y, -.1), (x, y, -L), 0, pas=.1, r=.013, larg=.036)
        if L > 2.5:
            e.tube([(x, y, -L), (x, y, -L - .12), (x + .02, y, -L - .2), (x + .1, y, -L - .25), (x + .16, y, -L - .18), (x + .15, y, -L - .1)], [.018, .018, .018, .018, .015, .006], FER, n=7)
        else:
            anneau(e, (x, y, -L - .08), .08, .018, FER, rot=(90, 0, 30), n=14, ouvert=2)


@piece("Bouclier_Epees", 32, sale=0)
def _(e):
    """Trophée mural : écu et deux épées croisées, pivot au mur, centre à 0."""
    prof = [(-.0, -.5), (.2, -.36), (.34, -.1), (.38, .2), (.36, .38), (0, .42)]
    bm = bmesh.new()
    pts = [(-x, z) for x, z in prof[:0:-1]] + prof
    av = [bm.verts.new((x, -.09 - .06 * (1 - abs(x) / .4), z)) for x, z in pts]
    ar = [bm.verts.new((x, -.06, z)) for x, z in pts]
    c = bm.verts.new((0, -.17, .02))
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new([av[i], av[j], c])
        bm.faces.new([av[j], av[i], ar[i], ar[j]])
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    e.maillage(bm, ROUGE)
    for i in range(n - 1):
        (xa, za), (xb, zb) = pts[i], pts[(i + 1)]
        e.barre((xa, -.1, za), (xb, -.1, zb), .05, .03, FER, bis=.006)
    e.barre((0, -.175, -.45), (0, -.175, .4), .05, .02, LAITON, bis=.005)
    e.barre((-.33, -.165, .1), (.33, -.165, .1), .05, .02, LAITON, bis=.005)
    for s in (-1, 1):
        a, b = Vector((s * -.6, -.04, -.75)), Vector((s * .62, -.04, .78))
        d = (b - a).normalized()
        e.barre(a, a.lerp(b, .8), .055, .014, ETAIN, bis=.005, roulis=90 if False else 0)
        g = a.lerp(b, .8)
        p = Vector((d.z, 0, -d.x))
        e.barre(g - p * .13, g + p * .13, .03, .03, FER)
        e.cyl(g, a.lerp(b, .96), .017, CUIR, n=7)
        e.tour(b - d * .04, [(0, -.03), (.03, 0), (0, .03)], FER, n=8)


# ------------------------------------------------------------------ autel, puits, pierres
def gravure(e, c, r, rot, mat=NOIR, ep=.03):
    """Signe gravé : cercle fendu et traits, barres sombres en saillie de 4 mm."""
    M = Matrix.Translation(Vector(c)) @ rotm(*rot).to_4x4()
    n = 14
    P = [M @ Vector((math.cos(2 * math.pi * i / n) * r, 0, math.sin(2 * math.pi * i / n) * r)) for i in range(n + 1)]
    for i in range(n - 2):
        e.barre(P[i], P[i + 1], r * .14, ep, mat, bis=0)
    for a, b in (((0, 1.3), (0, -1.5)), ((-.5, -.2), (.5, .35)), ((-.45, .5), (.4, -.5))):
        e.barre(M @ Vector((a[0] * r, 0, a[1] * r)), M @ Vector((b[0] * r, 0, b[1] * r)), r * .12, ep, mat, bis=0)


@piece("Autel_Ancien", 36, sale=.6, mousse=.6)
def _(e):
    """Table d'offrande : dalle fendue sur deux blocs, marche, coupe de sang séché, cire."""
    e.boite((0, 0, .11), (3.4, 2.2, .22), PIERRE, bis=.05, jit=.02)
    for sx in (-1, 1):
        e.boite((sx * .85, 0, .22), (.6, 1.0, .72), PIERRE, bis=.05, jit=.03, base=True, taper=(.88, .9))
    e.boite((-.48, 0, 1.06), (1.62, 1.25, .26), CLAIRE, bis=.045, jit=.02, rot=(0, 0.6, 0))
    e.boite((.86, .01, 1.05), (.98, 1.23, .25), CLAIRE, bis=.045, jit=.02, rot=(0, -1.4, 1.2))
    gravure(e, (0, -.507, .6), .2, (0, 0, 0)) if False else None
    for sx in (-1, 1):
        gravure(e, (sx * .85, -.495, .6), .16, (-5, 0, 0))
    e.tour((.05, -.1, 1.19), [(.1, 0), (.12, .02), (.05, .04), (.05, .07), (.24, .13), (.27, .18), (.24, .165), (0, .1)], LAITON, n=16)
    e.tour((.05, -.1, 1.31), [(.22, 0), (0, .004)], "M_Ens_Sang", n=16, ferme=False)
    crane(e, (-.75, .2, 1.18), rot=(0, 0, 25))
    crane(e, (.8, .25, 1.16), rot=(0, 0, -30))
    for k in range(5):
        a = k * 1.3
        os_long(e, (-.3 + .1 * k, .35, 1.21), (-.15 + .1 * k, .42, 1.22), .014)
    e.col_boite((0, 0, .11), (3.4, 2.2, .22))
    e.col_boite((0, 0, .7), (2.7, 1.25, 1.0))


@piece("Pierre_Levee_A", 37, sale=.9, mousse=.7)
def _(e):
    e.boite((0, 0, -.1), (1.0, .55, 2.9), PIERRE, base=True, bis=.09, jit=.09, taper=(.72, .7), rot=(0, 4, 8))
    gravure(e, (0.06, -.262, 1.5), .17, (-3, 0, 8))
    gravure(e, (0.06, .262, 1.5), .17, (3, 0, 8))
    e.roche((.6, -.3, 0), (.5, .4, .35))
    e.roche((-.55, .25, 0), (.6, .4, .3))
    e.col_boite((0.05, 0, 1.4), (.95, .55, 2.8), rot=(0, 4, 8))


@piece("Pierre_Levee_B", 38, sale=.9, mousse=.7)
def _(e):
    e.boite((0, 0, -.1), (.85, .6, 1.5), PIERRE, base=True, bis=.08, jit=.08, taper=(.9, .85), rot=(0, -5, -6))
    e.roche((-.75, -.35, 0), (1.0, .7, .55))
    gravure(e, (-.05, -.295, .9), .15, (-2, 0, -6))
    gravure(e, (-.05, .295, .9), .15, (2, 0, -6))
    e.col_boite((0, 0, .7), (.85, .6, 1.4))


# ------------------------------------------------------------------ éboulis, mine
@piece("Eboulis_A", 39, sale=.9, mousse=.5)
def _(e):
    """Coulée d'éboulis contre un mur (dos +Y) : 5 × 3 m, 2.4 m de haut."""
    for x, y, s, z in ((-1.3, .9, 1.7, 0), (.1, 1.0, 2.1, 0), (1.5, .95, 1.6, 0), (-.5, .9, 1.4, 1.05), (.75, 1.0, 1.2, 1.1), (-.6, -.1, 1.1, 0), (.9, -.05, 1.0, 0), (.1, -.35, .8, 0), (-1.9, .2, .9, 0), (2.1, .3, .8, 0)):
        e.roche((x, y, z), (s * e.rnd.uniform(.9, 1.15), s * e.rnd.uniform(.8, 1), s * .85), PIERRE if e.rnd.random() < .7 else MOUSSUE, n=16, bis=.05)
    for _ in range(26):
        x = e.rnd.gauss(0, 1.5)
        y = -.5 - abs(e.rnd.gauss(0, .45)) + abs(x) * .15
        s = e.rnd.uniform(.12, .42)
        e.roche((x, y, 0), (s, s * .85, s * .7), PIERRE, n=10, bis=.02)
    for x, y, z, rz, rx in ((-1.2, -.2, .75, 20, 8), (.5, -.5, .55, -35, -12)):
        e.boite((x, y, z), (.55, .4, .34), CLAIRE, rot=(rx, 14, rz), bis=.03, jit=.015)
    e.col_boite((0, .95, .8), (4.2, 1.6, 1.6))
    e.col_boite((.1, 1.1, 1.75), (2.2, 1.1, .9))
    e.col_boite((.1, -.1, .4), (2.6, 1.0, .8))


@piece("Eboulis_B", 40, sale=.8, mousse=.3)
def _(e):
    """Tas bas de moellons et de terre, 3 × 2 m, 0.9 m."""
    e.tour((0, 0, 0), [(1.5, -.02), (1.1, .2), (.6, .42), (.2, .5), (0, .52)], TERRE, n=22, bruit=.22, ech=(1, .68, 1))
    for _ in range(34):
        a, d = e.rnd.uniform(0, 6.28), math.sqrt(e.rnd.random()) * 1.5
        z = max(0, .42 * (1 - d / 1.2))
        s = e.rnd.uniform(.14, .5)
        e.roche((math.cos(a) * d, math.sin(a) * d * .68, z), (s, s * .85, s * .75), PIERRE, n=11, bis=.02)
    for k in range(5):
        a = k * 1.3
        e.boite((math.cos(a) * .8, math.sin(a) * .5, .3), (.5, .32, .28), CLAIRE, rot=(e.rnd.uniform(-25, 25), e.rnd.uniform(-25, 25), e.rnd.uniform(0, 90)), bis=.03, jit=.015)
    e.col_boite((0, 0, .3), (2.2, 1.4, .6))


@piece("Etai", 41, sale=.5)
def _(e):
    """Cadre d'étai de mine : deux poteaux, chapeau, jambes de force ; 4 × 3.6 m."""
    for sx in (-1, 1):
        e.barre((sx * 1.85, 0, 0), (sx * 1.72, 0, 3.3), .3, .3, GRIS, bis=.035, jit=.02)
        e.barre((sx * 1.74, 0, 2.3), (sx * 1.0, 0, 3.28), .18, .18, GRIS, bis=.02, jit=.01)
        for z in (.35, 2.95):
            e.boite((sx * (1.85 - z * .04), 0, z), (.34, .34, .07), FER, bis=.008)
        e.roche((sx * 2.1, -.2, 0), (.45, .4, .3))
    e.barre((-2.25, 0, 3.45), (2.3, 0, 3.42), .34, .32, GRIS, bis=.035, jit=.02)
    e.boite((0, -.2, 3.1), (.02, .02, .5), FER, bis=0)
    for sx in (-1, 1):
        e.col_boite((sx * 1.8, 0, 1.7), (.34, .34, 3.4))
    e.col_boite((0, 0, 3.44), (4.6, .34, .34))


@piece("Wagonnet", 42, sale=.5)
def _(e):
    """Wagonnet de mine renversé de côté : caisse de planches ferrée, quatre roues, minerai répandu. Le modèle est droit :
    la pose couchée se fait à la mise en place."""
    for sy in (-1, 1):
        for i in range(3):
            e.boite((0, sy * (.42 + .06 * i), .52 + i * .22), (1.5 + .1 * i, .05, .205), SOMBRE, rot=(sy * -16, 0, 0), fil=0, jit=.006)
    for sx in (-1, 1):
        for i in range(3):
            e.boite((sx * (.74 + .05 * i), 0, .52 + i * .22), (.05, .84 + .12 * i, .205), SOMBRE, rot=(0, sx * 14, 0), fil=1, jit=.006)
    for sx in (-1, 1):
        for sy in (-1, 1):
            e.barre((sx * .75, sy * .42, .4), (sx * .89, sy * .62, 1.1), .09, .09, FER, bis=.01)
        e.barre((sx * .55, -.6, .22), (sx * .55, .6, .22), .07, .07, FER)
        for sy in (-1, 1):
            e.cyl((sx * .5, sy * .5, .24), (sx * .5, sy * .58, .24), .2, FER, n=14)
            e.cyl((sx * .5, sy * .56, .24), (sx * .5, sy * .6, .24), .24, FER, n=14)
    e.boite((0, 0, .4), (1.45, .82, .07), SOMBRE, fil=0)
    for sy in (-1, 1):
        e.boite((0, sy * .64, 1.1), (1.85, .06, .09), FER, bis=.01)
    for sx in (-1, 1):
        e.boite((sx * .9, 0, 1.1), (.06, 1.24, .09), FER, bis=.01)
    e.col_boite((0, 0, .62), (1.8, 1.25, 1.1))


@piece("Rails_4m", 43, sale=.2)
def _(e):
    for i in range(6):
        e.boite((e.rnd.uniform(-.04, .04), -1.75 + i * .7, .05), (1.6, .22, .12), GRIS, rot=(0, e.rnd.uniform(-2, 2), e.rnd.uniform(-3, 3)), fil=0, jit=.012)
    for sx in (-1, 1):
        e.boite((sx * .5, 0, .15), (.06, 4.0, .09), FER, bis=.01, fil=1)
        e.boite((sx * .5, 0, .115), (.12, 4.0, .02), FER, bis=.004, fil=1)


@piece("Pioches", 44, sale=0)
def _(e):
    """Pioche et pelle appuyées au mur (dos +Y, inclinées), plus une lanterne éteinte au sol."""
    e.cyl((0, -.35, 0), (.05, -.04, 1.15), .02, BOIS, n=7)
    e.tube([(-.28, -.1, .98), (-.1, -.08, 1.1), (.05, -.04, 1.13), (.2, -.06, 1.1), (.36, -.1, .98)], [.008, .026, .034, .026, .008], FER, n=6)
    e.cyl((.4, -.4, .25), (.5, -.04, 1.35), .02, BOIS, n=7)
    e.nappe(lambda u, v: ((u - .5) * .26 + .39, -.42 - .02 * math.sin(u * math.pi) - (1 - v) * .03, .27 * v), 4, 3, FER, epais=.01)


# ------------------------------------------------------------------ salle de torture (discrète)
@piece("Chevalet", 45, sale=.4)
def _(e):
    """Banc d'étirement : cadre, planches, deux treuils à barres, cordes et fers."""
    for sx in (-1, 1):
        e.barre((sx * .5, -1.35, .75), (sx * .5, 1.35, .78), .14, .18, SOMBRE, bis=.02, jit=.008)
        for y in (-1.15, 1.15):
            e.barre((sx * .56, y, 0), (sx * .5, y, .75), .15, .15, SOMBRE, bis=.02)
        e.barre((sx * .5, -1.1, .3), (sx * .5, 1.1, .3), .1, .1, SOMBRE)
    for y in (-.85, -.45, -.05, .35, .75):
        e.boite((0, y, .82), (1.0, .24, .05), BOIS, fil=0, jit=.008, rot=(0, e.rnd.uniform(-1, 1), e.rnd.uniform(-2, 2)))
    for y in (-1.22, 1.22):
        e.cyl((-.72, y, .88), (.72, y, .88), .085, BOIS, n=10)
        for k in range(4):
            anneau(e, (-.12 + .06 * k, y, .88), .098, .02, JUTE, rot=(0, 90, 0), n=10, m=5)
        for sx in (-1, 1):
            e.barre((sx * .68, y, .62), (sx * .68, y, 1.16), .045, .045, BOIS)
            e.barre((sx * .68, y - .27, .88), (sx * .68, y + .27, .88), .045, .045, BOIS)
        s = 1 if y < 0 else -1
        for sx in (-1, 1):
            e.tube([(sx * .05, y, .98), (sx * .16, y + s * .25, .9), (sx * .22, y + s * .45, .87)], .014, JUTE, n=5)
            anneau(e, (sx * .24, y + s * .52, .87), .06, .016, CUIR, n=10)
    e.col_boite((0, 0, .5), (1.5, 2.8, 1.0))


@piece("Cage_Suspendue", 46, sale=0)
def _(e):
    """Cage de fer suspendue : pivot au crochet (z = 0), fond à -3.4 m ; un crâne au fond."""
    chaine(e, (0, 0, 0), (0, 0, -1.2), 0, pas=.11, r=.015, larg=.04)
    e.tour((0, 0, -1.35), [(.05, .14), (.06, .05), (.3, -.1), (.42, -.28)], FER, n=10, ferme=False, lisse=False) if False else None
    for k in range(10):
        a = k * .6283
        c, s = math.cos(a), math.sin(a)
        e.tube([(0, 0, -1.22), (c * .25, s * .25, -1.35), (c * .42, s * .42, -1.65), (c * .46, s * .46, -2.4), (c * .42, s * .42, -3.1), (c * .3, s * .3, -3.35)], .016, FER, n=6)
    for z, r in ((-1.65, .425), (-2.1, .455), (-2.7, .45), (-3.1, .425)):
        anneau(e, (0, 0, z), r, .02, FER, n=20)
    e.tour((0, 0, -3.4), [(0, 0), (.32, 0), (.33, .04), (0, .05)], FER, n=14)
    crane(e, (.08, -.1, -3.36), rot=(0, 20, 30))
    os_long(e, (-.2, .05, -3.33), (.1, .2, -3.33))
    os_long(e, (-.15, -.2, -3.33), (-.05, .2, -3.32))
    e.col_boite((0, 0, -2.5), (.9, .9, 1.9))


@piece("Pilori", 47, sale=.6)
def _(e):
    for sx in (-1, 1):
        e.barre((sx * .8, 0, 0), (sx * .8, 0, 1.75), .16, .16, SOMBRE, bis=.02, jit=.01)
    e.boite((0, 0, .06), (2.0, 1.0, .12), SOMBRE, bis=.02, fil=0)
    for z, t in ((1.22, 0), (1.5, 1)):
        # planche haute relevée de 35° autour de sa charnière (x = -0.72) : pilori ouvert
        a = math.radians(35) if t else 0
        def W(x, zz):
            dx, dz = x + .72, zz - 1.38
            return Vector((-.72 + math.cos(a) * dx - math.sin(a) * dz, 0, 1.38 + math.sin(a) * dx + math.cos(a) * dz)) if t else Vector((x, 0, zz))
        for x0, x1 in ((-.72, -.43), (-.27, -.13), (.13, .27), (.43, .72)):
            e.boite(W((x0 + x1) / 2, z), (x1 - x0, .08, .26), BOIS, fil=0, bis=.012, rot=(0, -math.degrees(a), 0))
        for x0, x1 in ((-.43, -.27), (-.13, .13), (.27, .43)):
            e.boite(W((x0 + x1) / 2, z + (.07 if t else -.07)), (x1 - x0 + .02, .078, .12), BOIS, fil=0, bis=.006, rot=(0, -math.degrees(a), 0))
    e.boite((.78, -.1, 1.36), (.12, .06, .18), FER, bis=.01)
    e.col_boite((0, 0, 1.36), (1.75, .16, .6))
    for sx in (-1, 1):
        e.col_boite((sx * .8, 0, .8), (.18, .18, 1.6))


@piece("Outils_Table", 48, sale=0)
def _(e):
    """Instruments posés sur une toile roulée (0.9 × 0.45 m)."""
    e.nappe(lambda u, v: ((u - .5) * .95, (v - .5) * .45, .006 + .004 * math.sin(u * 20) + (.05 * max(0, u - .92) / .08 if u > .92 else 0)), 22, 4, CUIR)
    for k in range(6):
        x = -.36 + k * .13
        L = e.rnd.uniform(.22, .34)
        e.cyl((x, -.12, .02), (x + e.rnd.uniform(-.02, .02), -.12 + L * .4, .02), .012, BOIS, n=6)
        e.cyl((x, -.12 + L * .4, .02), (x, -.12 + L, .02), .007, FER, r2=.004 if k % 2 else .012, n=6)
        if k % 3 == 0:
            e.boite((x, -.12 + L, .02), (.07, .03, .012), FER, bis=.004)
    anneau(e, (.55, .05, .012), .05, .01, FER, n=12)
    for k in range(4):
        a = k * .6
        e.barre((.55 + math.cos(a) * .05, .05 + math.sin(a) * .05, .012), (.55 + math.cos(a) * .14, .05 + math.sin(a) * .14, .008), .012, .008, FER, bis=.002)


@piece("Fers_Brasero", 49, sale=0)
def _(e):
    """Trois tisonniers à poser dans le brasero (pivot au centre du panier, z = 1 m)."""
    for k, a in enumerate((.4, 1.1, 2.4)):
        c, s = math.cos(a), math.sin(a)
        e.cyl((-c * .15, -s * .15, .86), (c * .75, s * .75, 1.32), .013, FER, n=6)
        e.cyl((c * .6, s * .6, 1.245), (c * .78, s * .78, 1.335), .022, BOIS, n=7)
        e.tour((-c * .15, -s * .15, .86), [(0, -.02), (.035, 0), (0, .03)], BRAISE, n=7)


@piece("Potence", 50, sale=.4)
def _(e):
    """Potence murale de bois (dos +Y) pour la cage ou les chaînes : bras de 1.4 m à 4 m de haut, repère Accroche."""
    e.barre((0, -.12, 0), (0, -.12, 4.3), .24, .24, SOMBRE, bis=.025, jit=.01)
    e.barre((0, .0, 4.1), (0, -1.5, 4.1), .2, .22, SOMBRE, bis=.02)
    e.barre((0, -.2, 3.1), (0, -1.1, 4.05), .14, .14, SOMBRE, bis=.015)
    e.boite((0, -.12, 4.1), (.28, .3, .1), FER, bis=.008)
    anneau(e, (0, -1.35, 3.93), .06, .016, FER, rot=(90, 0, 90), n=12)
    e.vide("Accroche", (0, -1.35, 3.88))
    e.col_boite((0, -.12, 2.15), (.26, .26, 4.3))
    e.col_boite((0, -.75, 4.1), (.22, 1.5, .24))


@piece("Tapis_Cendres", 51, sale=0)
def _(e):
    e.tour((0, 0, .002), [(.7, 0), (.4, .012), (.1, .025), (0, .026)], NOIR, n=20, bruit=.4)


@piece("Etagere_Murale", 52, sale=0)
def _(e):
    """Tablette murale de 1.8 m sur consoles (pivot au mur, dessus à 0) : pots, fioles, pain, bougie éteinte."""
    e.boite((0, -.19, -.03), (1.8, .36, .05), GRIS, fil=0, jit=.006)
    for sx in (-1, 1):
        e.barre((sx * .7, -.02, -.42), (sx * .7, -.34, -.06), .07, .07, GRIS)
        e.boite((sx * .7, -.03, -.24), (.09, .06, .42), GRIS)
    e.tour((-.68, -.2, 0), [(.07, 0), (.12, .08), (.1, .2), (.06, .24), (.07, .28), (0, .26)], CUITE, n=12)
    e.tour((-.4, -.16, 0), [(.06, 0), (.09, .06), (.08, .15), (.05, .18), (0, .18)], CUITE, n=12)
    for k in range(3):
        e.tour((-.12 + k * .13, -.17 - .04 * (k % 2), 0), [(.035, 0), (.04, .1), (.02, .14), (.013, .16), (.013, .22), (.018, .23), (0, .23)], VERRE, n=9)
    e.roche((.36, -.18, 0), (.26, .16, .13), CUIR, n=16, bis=.02, enfonce=.1)
    e.tour((.66, -.2, 0), [(.05, 0), (.052, .1), (.045, .12), (0, .11)], ETAIN, n=10)
    crane(e, (.82, -.16, 0), .8, rot=(0, 0, 20))


@piece("Rail_Suspendu", 53, sale=0)
def _(e):
    """Rail de fer mural de 2 m (pivot au mur, rail à 0) : jambons, tresses d'ail, herbes sèches, poêle."""
    for sx in (-1, 1):
        e.barre((sx * .9, 0, 0), (sx * .9, -.22, 0), .035, .035, FER)
    e.cyl((-1.05, -.22, 0), (1.05, -.22, 0), .018, FER, n=7)
    for x, m in ((-.75, "jambon"), (-.4, "ail"), (-.05, "herbe"), (.3, "jambon"), (.68, "poele")):
        e.tube([(x, -.22, .03), (x, -.25, .0), (x, -.22, -.06)], .008, FER, n=5)
        if m == "jambon":
            e.tube([(x, -.22, -.05), (x, -.22, -.2)], .022, OS, n=6)
            e.tour((x, -.22, -.62), [(0, 0), (.1, .04), (.14, .16), (.1, .3), (.045, .43), (0, .45)], CUIR, n=12, ech=(1, .7, 1), bruit=.1)
        elif m == "ail":
            e.cyl((x, -.22, -.05), (x, -.22, -.7), .012, JUTE, n=5)
            for k in range(9):
                a = k * 2.2
                e.tour((x + math.cos(a) * .05, -.22 + math.sin(a) * .05, -.68 + k * .065), [(0, 0), (.04, .02), (.035, .05), (0, .08)], PAPIER, n=7)
        elif m == "herbe":
            for k in range(14):
                a = k * .45
                e.tube([(x, -.22, -.08), (x + math.cos(a) * .05, -.22 + math.sin(a) * .04, -.3), (x + math.cos(a) * .14, -.22 + math.sin(a) * .1, -.58)], [.006, .012, .003], "M_Ens_Tige", n=4)
        else:
            e.boite((x, -.22, -.25), (.04, .02, .42), FER)
            e.tour((x, -.2, -.62), [(.17, 0), (.2, .05), (.19, .05), (.16, .012), (0, .012)], FER, n=14, rot=(90, 0, 0))


@piece("Gravats_Epars", 54, sale=.1)
def _(e):
    """Cailloux, éclats et brins de paille épars (3 × 2 m) : casse les sols nus, sans collision."""
    for _ in range(42):
        a, d = e.rnd.uniform(0, 6.28), abs(e.rnd.gauss(0, .55))
        s = e.rnd.uniform(.04, .16)
        e.roche((math.cos(a) * d * 1.6, math.sin(a) * d, 0), (s, s * .8, s * .6), PIERRE, n=8, bis=.006)
    for _ in range(50):
        a, d, b = e.rnd.uniform(0, 6.28), abs(e.rnd.gauss(0, .7)), e.rnd.uniform(0, 6.28)
        p = Vector((math.cos(a) * d * 1.6, math.sin(a) * d, .006))
        e.tube([p, p + Vector((math.cos(b), math.sin(b), .05)) * e.rnd.uniform(.08, .2)], [.006, .003], "M_Ens_Grain", n=3, bout=False)


@piece("Banniere", 55, sale=0)
def _(e):
    """Bannière pendue de 1.2 × 2.8 m, deux pointes effilochées, croissant cousu (pivot au mur, haut à 0)."""
    e.cyl((-.75, -.1, 0), (.75, -.1, 0), .03, SOMBRE, n=8)
    for sx in (-1, 1):
        e.tour((sx * .75, -.1, 0), [(0, -.06), (.05, 0), (0, .08)], LAITON, n=8, rot=(0, sx * 90, 0))
        e.barre((sx * .6, 0, .0), (sx * .6, -.1, 0), .03, .03, FER)

    def P(u, v):
        bas = 2.5 + .35 * abs(u - .5) * 2 + .06 * noise.noise(Vector((u * 12, 0, 1)))
        return ((u - .5) * 1.2, -.11 - .05 * math.sin(u * 13 + v * 2) * (.25 + v), -.02 - bas * v)
    e.nappe(P, 24, 10, ROUGE)
    gravure(e, (0, -.2, -1.15), .3, (0, 0, 0), OR, .02)


@piece("Grille_Imposte", 56, sale=0)
def _(e):
    """Imposte de barreaux de 1.5 × 2.7 m (au-dessus de la porte d'une cellule du kit) ; pivot au milieu du bas."""
    barreaux(e, -.7, .7, 0, 2.7)
    for z in (.05, 1.3, 2.6):
        e.boite((0, 0, z), (1.5, .03, .09), FER, bis=.006)
    e.col_boite((0, 0, 1.35), (1.5, .1, 2.7))


@piece("Marques_Jours", 57, sale=0)
def _(e):
    """Jours comptés, gravés au mur (pivot au mur, milieu) : 11 paquets de cinq traits."""
    for k in range(11):
        x, z = -.6 + (k % 4) * .4, .3 - (k // 4) * .3
        for j in range(4):
            e.boite((x + j * .055, -.004, z + e.rnd.uniform(-.015, .015)), (.016, .008, .2), NOIR, rot=(0, e.rnd.uniform(-6, 6), 0), bis=0)
        e.boite((x + .085, -.006, z), (.016, .008, .3), NOIR, rot=(0, 62, 0), bis=0)


@piece("Planches_Puits", 58, sale=0, mousse=.3)
def _(e):
    """Puits condamné : sept planches clouées en croix sur la margelle (dessous à 0.9 m) et pierres posées dessus ; deux planches
    soulevées d'en dessous."""
    for k in range(5):
        y = -1.05 + k * .52
        L = 2 * math.sqrt(max(.1, 1.55 ** 2 - y * y)) + .25
        s = k in (2, 3)
        e.boite((0.05 * k, y, .95 + (.16 if s else 0)), (L, .46, .055), GRIS, fil=0, jit=.014, rot=(e.rnd.uniform(-2, 2) + (9 if k == 2 else -8 if k == 3 else 0), 4 if s else 0, e.rnd.uniform(-4, 4)))
    for x, r in ((-.85, 84), (.95, 97)):
        e.boite((x, 0, 1.03), (2.5, .32, .06), GRIS, fil=0, jit=.014, rot=(0, 0, r))
        for k in range(4):
            e.cyl((x, -.95 + k * .6, 1.05), (x, -.95 + k * .6, 1.075), .022, FER, n=6)
    for x, y, s in ((-.85, -.6, .5), (-.75, .5, .42), (.95, .45, .55), (.85, -.55, .4), (.5, -.95, .34)):
        e.roche((x, y, 1.05), (s, s * .9, s * .75), PIERRE, n=14, bis=.03, enfonce=.08)
    e.col_boite((0, 0, 1.0), (2.6, 2.6, .25))


@piece("Bouche_Mine", 59, sale=0)
def _(e):
    """Galerie éventrée peinte sur le mur (pivot au mur) : fond noir irrégulier de 3.4 × 3.3 m bordé de pierres arrachées. Aucun trou
    réel : le mur de la salle reste entier."""
    n = 22
    pts = []
    for k in range(n + 1):
        a = math.pi * k / n
        r = 1 + .1 * noise.noise(Vector((k * .9, 3, 1)))
        pts.append((-math.cos(a) * 1.75 * r, math.sin(a) * 3.2 * r))
    bm = bmesh.new()
    c = bm.verts.new((0, -.02, .6))
    vs = [bm.verts.new((x, -.02, max(0, z))) for x, z in pts]
    for i in range(n):
        bm.faces.new([vs[i + 1], vs[i], c])
    bm.faces.new([vs[0], vs[-1], c])
    bm.normal_update()
    e.maillage(bm, NOIR)
    for k, (x, z) in enumerate(pts):
        s = e.rnd.uniform(.3, .6)
        e.boite((x, -.08, max(.12, z)), (s, .34, s * .7), PIERRE if k % 3 else CLAIRE, rot=(e.rnd.uniform(-14, 14), e.rnd.uniform(-40, 40), e.rnd.uniform(-10, 10)), bis=.035, jit=.03)


@piece("Sacs_Eventres", 60, sale=.4)
def _(e):
    """Sacs éventrés et grain répandu en traînée de 2.4 m."""
    sac(e, (0, 0, .2), .28, .6, rot=(82, 0, 15), ouvert=True)
    sac(e, (-.7, .3, 0), .27, .42, ouvert=True)
    e.tour((0, -.55, 0), [(.5, -.01), (.3, .07), (.1, .12), (0, .13)], "M_Ens_Grain", n=18, bruit=.35)
    for k in range(7):
        r = .32 - k * .03
        e.tour((.15 + .12 * math.sin(k * 1.1), -.9 - k * .24, 0), [(r, -.01), (r * .5, .035), (0, .05)], "M_Ens_Grain", n=12, bruit=.4)
    e.col_boite((-.35, .15, .2), (1.1, .6, .4))


@piece("Tonneau_Brise", 61, sale=.4)
def _(e):
    """Tonneau éclaté : fond, cercles et douves en éventail, flaque sombre."""
    e.tour((0, 0, .003), [(1.0, 0), (.8, .006), (0, .008)], "M_Ens_Eau", n=20, bruit=.35)
    e.tour((0, 0, 0), [(.3, 0), (.3, .05), (0, .05)], BOIS, n=14, lisse=False)
    for k in range(13):
        a = k * .483 + e.rnd.uniform(-.1, .1)
        t = e.rnd.uniform(38, 84)
        d = .3 + .42 * math.sin(math.radians(t))
        e.boite((math.cos(a) * d, math.sin(a) * d, max(.04, .45 * math.cos(math.radians(t)))), (.03, .13, .88), BOIS, rot=(0, t, math.degrees(a)), fil=2, jit=.006)
    anneau(e, (.5, -.5, .06), .33, .02, FER, rot=(10, 4, 0), n=18, ouvert=3)
    anneau(e, (-.1, .1, .12), .31, .02, FER, rot=(-16, 10, 0), n=18)
    e.col_boite((0, 0, .18), (1.0, 1.0, .36))


@piece("Etagere_Murale_B", 62, sale=0)
def _(e):
    """Deux tablettes murales décalées : cordes, marteaux, lampes, bûchettes (pivot au mur, dessus bas à 0)."""
    for x, z, L in ((-.3, 0, 1.3), (.35, .55, 1.1)):
        e.boite((x, -.17, z - .03), (L, .32, .05), GRIS, fil=0, jit=.006)
        for sx in (-1, 1):
            e.barre((x + sx * L * .38, -.02, z - .3), (x + sx * L * .38, -.28, z - .05), .06, .06, GRIS)
    for k in range(5):
        anneau(e, (-.65, -.17, .03 + k * .035), .13, .02, JUTE, n=12, m=5)
    e.cyl((-.35, -.22, .04), (-.05, -.12, .04), .016, BOIS, n=6)
    e.boite((-.02, -.11, .05), (.07, .13, .07), FER, bis=.01)
    e.tour((.12, -.17, 0), [(.05, 0), (.06, .02), (.03, .04), (.045, .07), (.05, .18), (.03, .21), (0, .21)], LAITON, n=10)
    for k in range(6):
        e.cyl((.0 + .06 * (k % 4), -.3, .59 + .055 * (k // 4)), (.0 + .06 * (k % 4), -.04, .59 + .055 * (k // 4)), .027, BOIS, n=7)
    e.tour((.5, -.17, .55), [(.08, 0), (.13, .1), (.09, .22), (.05, .26), (.06, .3), (0, .28)], CUITE, n=12)
    crane(e, (.75, -.17, .55), .9, rot=(0, 0, -20))


@piece("Etagere_Murale_Vide", 63, sale=0)
def _(e):
    """Tablette murale décrochée d'un côté, vidée."""
    e.boite((0, -.17, -.08), (1.8, .34, .05), GRIS, fil=0, jit=.008, rot=(0, 13, 0))
    e.barre((-.7, -.02, -.3), (-.7, -.3, .05), .07, .07, GRIS)
    e.boite((.72, -.03, -.5), (.09, .06, .42), GRIS, rot=(0, 24, 0))


for _g in ("B", "C"):
    @piece("Gravats_Epars_" + _g, 64 + ord(_g), sale=.1)
    def _(e, g=_g):
        """Éclats de pierre épars, version sans paille."""
        for _ in range(26 if g == "B" else 16):
            a, d = e.rnd.uniform(0, 6.28), abs(e.rnd.gauss(0, .6))
            s = e.rnd.uniform(.06, .3 if g == "C" else .17)
            e.roche((math.cos(a) * d * 1.5, math.sin(a) * d, 0), (s, s * .8, s * .5), CLAIRE if e.rnd.random() < .35 else PIERRE, n=9, bis=.008)


@piece("Banniere_Bleue", 70, sale=0)
def _(e):
    """Étendard étroit bleu de 0.8 × 3.2 m, franges dorées, déchiré (pivot au mur, haut à 0)."""
    e.cyl((-.52, -.1, 0), (.52, -.1, 0), .028, SOMBRE, n=8)
    for sx in (-1, 1):
        e.barre((sx * .4, 0, 0), (sx * .4, -.1, 0), .03, .03, FER)

    def P(u, v):
        bas = 3.0 + .25 * noise.noise(Vector((u * 9, 0, 4))) - .35 * max(0, u - .7) * 3
        return ((u - .5) * .8, -.11 - .04 * math.sin(u * 11 + v * 3) * (.25 + v), -.02 - bas * v)
    e.nappe(P, 20, 12, BLEU)
    for sx in (-1, 1):
        e.boite((sx * .36, -.115, -1.2), (.05, .012, 2.3), OR, bis=0)
    for z in (-.5, -.95, -1.4):
        e.boite((0, -.175, z), (.2, .012, .2), OR, rot=(0, 45, 0), bis=0)
