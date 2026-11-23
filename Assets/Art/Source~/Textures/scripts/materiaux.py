# -*- coding: utf-8 -*-
"""Bibliothèque de matériaux toon peints (lot « matériaux », branche art-materiaux, 2026-11-22).

Procédural, sans photo ni IA : `peint.py` (bruits périodiques, cellules, traits). Chaque matériau sort une tuile qui se
raccorde (`T_Mat_<Nom>_BaseColor.png`, RVB = couleur peinte, **alpha = hauteur** pour le mélange en hauteur du shader
`ItHunts/Toon` en mode décor). Échelle : la tuile couvre `TAILLE[nom]` mètres (UV monde, comme le kit gothique).

Lancer (Blender ou Python) :
    blender -b -P materiaux.py -- [noms…]        ou        python materiaux.py [noms…]
Sorties : Assets/Art/Textures/Materiaux/, planche docs/art/rendus/Materiaux/Planche_Tuiles.jpg.
"""
import os, sys, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np
import peint as P
from peint import col, smooth, poster, fbm, lerp

ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
OUT = os.path.join(ROOT, "Assets", "Art", "Textures", "Materiaux")
DOC = os.path.join(ROOT, "docs", "art", "rendus", "Materiaux")
N = 1024
# Nuit 4 : densité de référence 512 px/m (ART-HANDOFF). Les tuiles de 4 m sont peintes en 2048 px : K = N / 1024 met à
# l'échelle les longueurs écrites en pixels (joints, biseaux, traits d'encre, fissures) pour garder leur taille réelle.
K = 1.0
RES = {}   # nom -> résolution (px) de la tuile, 1024 par défaut


def resolution(n):
    """Change la résolution de travail (N) et le facteur K des longueurs en pixels."""
    global N, K
    N, K = n, n / 1024.0

# taille de la tuile en mètres (x, y) : UV monde du shader (_TileSize)
TAILLE = {}


def tuile(nom, tx, ty):
    def deco(f):
        TAILLE[nom] = (tx, ty)
        f.nom = nom
        return f
    return deco


# ------------------------------------------------------------------ peinture d'une maçonnerie (commun)
def maconnerie(r, ID, D, pal, gap=5.0, bev=26.0, ink_w=2.2, mortier=col("#2a2630"), fiss=10, mottle=3,
               creux=0.35, lum=1.0, sat_var=0.012, val_var=0.14, ecl=0.3, coulures=0.0, M=None, ink_part=1.0,
               marbrure=0.2):
    """Peint des pierres d'après un id par pierre et la distance au joint (pixels)."""
    M = M or N
    gap, bev, ink_w = gap * K, bev * K, ink_w * K
    # bord irrégulier : la distance au joint est bruitée (pierres ébréchées)
    nb = fbm(N, r, 1.6, 6, 120, M=M)
    nb2 = fbm(N, r, 2.2, 2, 30, M=M)
    Dn = D + ((nb - 0.5) * 7.0 + (nb2 - 0.5) * 5.0) * K
    stone = Dn > gap
    # hauteur : dôme au bord, bosses internes
    bump = fbm(N, r, 2.0, 3, 60, M=M)
    h = smooth(gap, gap + bev, Dn) * (0.72 + 0.28 * bump)
    h = np.where(stone, h, 0.08 * bump)
    light = P.bevel_light(P.blur(h, 1.2), strength=9.0, levels=2)
    # couleur par pierre
    k = P.per_id(ID, r)
    v = P.per_id(ID, r, lo=-val_var, hi=val_var)
    base = np.zeros((M, N, 3), np.float32)
    n = len(pal)
    idx = np.minimum((k * n).astype(int), n - 1)
    for i, c in enumerate(pal):
        base[idx == i] = c
    # petite dérive de teinte par pierre
    dh = np.stack([P.per_id(ID, r, lo=-sat_var, hi=sat_var) for _ in range(3)], -1)
    base = np.clip(base * (1 + v[..., None]) + dh * 0.5, 0, 1)
    # marbrure posterisée (2-3 tons) et coups de pinceau
    mo = poster(fbm(N, r, 2.0, 3, 34, M=M), mottle, 0.15)
    st = fbm(N, r, 1.2, 3, 160, aniso=(1.0, 5.0), M=M)
    shade = 1.06 - marbrure + marbrure * mo + (st - 0.5) * 0.07
    c = base * shade[..., None]
    # lumière peinte : arête haute claire, bas sombre
    c = c * (1 + ecl * light[..., None] * np.where(light[..., None] > 0, 1.0, 1.6))
    # creux de la pierre près du joint (ombre portée sous l'arête)
    c = c * (1 - creux * (1 - smooth(gap, gap + 7 * K, Dn))[..., None] * stone[..., None])
    # mortier
    mt = mortier * (0.8 + 0.4 * poster(fbm(N, r, 1.5, 8, 200, M=M), 3))[..., None]
    c = np.where(stone[..., None], c, mt)
    # encre du joint (trait plus épais en bas des pierres : lumière d'en haut)
    gy = np.roll(D, -2, 0) - np.roll(D, 2, 0)
    w = ink_w * (1 + 0.6 * (gy < 0)) * (0.7 + 0.6 * nb)
    ink = (Dn > gap) & (Dn < gap + w)
    if ink_part < 1.0:
        ink = ink & (fbm(N, r, 2.0, 3, 40, M=M) < ink_part)
    # fissures dans les pierres
    I = P.Ink(N, M)
    P.cracks(I, r, fiss, N, length=(30 * K, 140 * K), width=(1.0 * K, 2.2 * K), M=M, inside=stone)
    cr = I.get(0.4)
    ink_m = np.maximum(ink.astype(np.float32), cr * stone)
    # lèvre claire sous la fissure (aplat éclairé)
    lip = np.roll(cr, max(1, int(round(2 * K))), 0) * (1 - cr) * stone
    c = c * (1 + 0.18 * lip[..., None])
    c = lerp(c, P.INK, np.clip(ink_m, 0, 1))
    h = np.where(cr > 0.3, h * 0.4, h)
    # coulures sombres (humidité) qui partent des joints horizontaux
    if coulures > 0:
        dr = fbm(N, r, 1.0, 2, 200, aniso=(0.35, 9.0), M=M)
        mask = smooth(0.62, 0.75, dr) * coulures
        c = c * (1 - 0.35 * mask[..., None])
    return np.clip(c * lum, 0, 1), np.clip(h, 0, 1)


# ------------------------------------------------------------------ pierres
@tuile("Moellons", 4, 4)
def moellons(r):
    """Moellons : pierres irrégulières grossièrement équarries, mortier épais (caves, murs de soutènement)."""
    wx = (fbm(N, r, 2.2, 2, 20) - 0.5) * 60
    wy = (fbm(N, r, 2.2, 2, 20) - 0.5) * 60
    F1, F2, ID, _, _ = P.cells(N, 9, 12, r, jitter=0.85, wx=wx, wy=wy)
    D = P.edge_dist(F1, F2) * N / 10.5
    pal = [col("#5a5765"), col("#53525f"), col("#605a62"), col("#57565c"), col("#4d4a57")]
    return maconnerie(r, ID, D, pal, gap=6.0, bev=30, fiss=14, mortier=col("#27232a"), coulures=0.5)


@tuile("PierreTaillee", 4, 3)
def pierre_taillee(r):
    """Pierre de taille : assises régulières, joints fins (château)."""
    M = N * 3 // 4
    ID, D, _, _ = P.courses(N, 6, r, 0.16, 0.34, M=M, row_jit=0.08)
    pal = [col("#77706d"), col("#6f6a6a"), col("#7c756e"), col("#726c70")]
    return maconnerie(r, ID, D, pal, gap=3.0, bev=18, ink_w=2.0, fiss=8, mortier=col("#3a3436"), val_var=0.1,
                      creux=0.25, M=M)


@tuile("Dalles", 4, 4)
def dalles(r):
    """Dalles de sol irrégulières (rectangles de tailles variées, usés au centre)."""
    ID, D, LU, LV = P.courses(N, 7, r, 0.10, 0.24, row_jit=0.25)
    pal = [col("#5e5a62"), col("#635e5e"), col("#5a5860"), col("#666062")]
    c, h = maconnerie(r, ID, D, pal, gap=4.0, bev=14, fiss=12, mortier=col("#201d22"), val_var=0.12, ecl=0.14)
    # usure : centre des dalles plus clair et plus lisse (passage)
    use = smooth(0.55, 0.95, P.fbm(N, r, 2.4, 1, 10)) * 0.12
    c = c * (1 + use[..., None])
    return c, h


@tuile("RocheCave", 4, 4)
def roche_cave(r):
    """Roche de cave humide : strates inclinées, gros pans en aplats, fissures, suintements brillants."""
    big = fbm(N, r, 2.6, 1, 12)
    wx = (fbm(N, r, 2.4, 1, 12) - 0.5) * 120 * K
    wy = (fbm(N, r, 2.4, 1, 12) - 0.5) * 120 * K
    F1, F2, ID, _, _ = P.cells(N, 4, 5, r, jitter=1.0, wx=wx * 1.5, wy=wy * 0.7)
    D = P.edge_dist(F1, F2) * N / 4.5
    yy = np.mgrid[0:N, 0:N][0].astype(np.float32)
    # strates : bandes légèrement inclinées, déformées
    strate = np.sin((yy / N * 9 + (big - 0.5) * 2.2) * 2 * math.pi)
    strate = poster((strate + 1) / 2, 3, 0.2)
    pal = [col("#41433f"), col("#444340"), col("#3e3f43"), col("#474643")]
    c, h = maconnerie(r, ID, D, pal, gap=0.5, bev=90, ink_w=2.4, fiss=30, mortier=col("#1c1c1e"), creux=0.6,
                      val_var=0.1, ecl=0.42, ink_part=0.55)
    c = c * (0.86 + 0.18 * strate[..., None])
    # suintement : coulures verticales brillantes (humide) et voile verdâtre
    dr = fbm(N, r, 1.0, 2, 220, aniso=(0.3, 10.0))
    wet = smooth(0.6, 0.7, dr) * smooth(0.3, 0.7, big)
    c = lerp(c, c * 0.7 + col("#4f6a66") * 0.35, wet * 0.8)
    algue = smooth(0.62, 0.7, fbm(N, r, 1.8, 3, 40)) * 0.35
    c = lerp(c, col("#34412f"), algue)
    # lignes de strates à l'encre, interrompues
    I = P.Ink(N)
    for k in range(7):
        y0 = (k + r.uniform(0.2, 0.8)) * N / 7
        x = r.uniform(0, N)
        pts = []
        for s_ in range(40):
            pts.append((x + s_ * N / 38, y0 + (big[int(y0) % N, int(x + s_ * N / 38) % N] - 0.5) * 80 * K + r.normal(0, 2 * K)))
        seg = 0
        while seg < len(pts) - 2:
            L = r.integers(3, 9)
            for i in range(seg, min(seg + L, len(pts) - 1)):
                I.line([pts[i], pts[i + 1]], r.uniform(1.4, 3.0) * K)
            seg += L + r.integers(2, 6)
    sl = I.get(0.4)
    c = lerp(c, P.INK, sl * 0.85)
    c = c * (1 + 0.12 * np.roll(sl, -int(round(3 * K)), 0)[..., None] * (1 - sl[..., None]))
    h = np.clip(h * 0.7 + big * 0.3 - sl * 0.2, 0, 1)
    return c, h


@tuile("Briques", 2, 2)
def briques(r):
    """Briques de voûte (assises fines, sombres et rouges éteintes)."""
    ID, D, _, _ = P.courses(N, 24, r, 0.11, 0.14, row_jit=0.05)
    pal = [col("#5c3d36"), col("#6a463b"), col("#4f3532"), col("#5a4a44")]
    return maconnerie(r, ID, D, pal, gap=2.6, bev=6, ink_w=1.4, fiss=6, mortier=col("#2b2626"), val_var=0.14, ecl=0.18)


# ------------------------------------------------------------------ couches (mortier, mousse, salpêtre, humidité)
@tuile("Mortier", 2, 2)
def mortier(r):
    """Mortier / enduit grossier : grain, petits cailloux, fissures fines."""
    g = fbm(N, r, 1.3, 20, 400)
    m = poster(fbm(N, r, 2.0, 3, 40), 3, 0.2)
    c = col("#47424a") * (0.72 + 0.38 * m + 0.2 * (g - 0.5))[..., None]
    I = P.Ink(N)
    for _ in range(220):
        x, y, rad = r.uniform(0, N), r.uniform(0, N), r.uniform(1.5, 5)
        I.dot(x, y, rad)
    pebble = I.get(0.3)
    c = lerp(c, col("#6a6266"), pebble * 0.7)
    I2 = P.Ink(N)
    P.cracks(I2, r, 10, N, length=(40, 200), width=(0.8, 1.6))
    c = lerp(c, P.INK, I2.get(0.3))
    return c, np.clip(0.35 + pebble * 0.4 + (g - 0.5) * 0.2, 0, 1)


def nappe(r, base, sombre, clair, seuil=0.5, bords=True, scale=(3, 60), tons=3):
    """Nappe organique en aplats (mousse, lichen) : masque = bruit seuillé, 3 tons, encre au bord."""
    f = fbm(N, r, 1.9, scale[0], scale[1])
    m = smooth(seuil - 0.02, seuil + 0.02, f)
    t = poster(fbm(N, r, 1.6, 6, 160), tons, 0.2)
    c = lerp(lerp(sombre, base, np.clip(t * 2, 0, 1)), clair, np.clip(t * 2 - 1, 0, 1))
    if bords:
        e = smooth(seuil - 0.03, seuil, f) * (1 - smooth(seuil + 0.0, seuil + 0.015, f))
        c = lerp(c, P.INK * 1.5, e * 0.6)
    return c, m, f


@tuile("Mousse", 2, 2)
def mousse(r):
    """Mousse : touffes en aplats vert sombre, points clairs ; alpha = hauteur (la mousse dépasse)."""
    f = fbm(N, r, 2.3, 2, 40)
    m = smooth(0.40, 0.43, f)
    dome = P.blur(m, 10)
    light = P.bevel_light(dome, 30.0, 2)
    t = poster(fbm(N, r, 1.8, 6, 60), 3, 0.2)
    c = lerp(fill(col("#1c2419")), fill(col("#2d3f24")), t)
    c = c * (1 + 0.35 * light[..., None])
    I = P.Ink(N)
    for _ in range(500):
        I.dot(r.uniform(0, N), r.uniform(0, N), r.uniform(1.2, 2.8))
    c = lerp(c, col("#5f7a3a"), I.get(0.2) * 0.7 * (light > 0))
    e = smooth(0.38, 0.40, f) * (1 - m)
    c = lerp(c, fill(col("#1d1a1c")), 1 - m)
    c = lerp(c, P.INK, e)
    h = np.clip(0.3 + dome * 0.7, 0, 1)
    return c, h


@tuile("Salpetre", 2, 2)
def salpetre(r):
    """Salpêtre : efflorescences blanc cassé en croûtes, bords dentelés, coulures blanchâtres."""
    base = col("#4b484c")
    f = fbm(N, r, 2.2, 2, 60)
    m = smooth(0.55, 0.57, f)
    t = poster(fbm(N, r, 1.8, 6, 120), 3, 0.1)
    crust = lerp(col("#9a978d"), col("#cfcbbd"), t)
    c = lerp(fill(base), crust, m)
    dr = fbm(N, r, 1.0, 2, 260, aniso=(0.3, 10.0))
    c = lerp(c, col("#b8b4a8"), smooth(0.66, 0.72, dr) * 0.5)
    e = smooth(0.53, 0.55, f) * (1 - m)
    c = lerp(c, col("#8d897f"), e * 0.6)
    c = lerp(c, P.INK * 2, smooth(0.55, 0.57, f) * (1 - smooth(0.57, 0.6, f)) * 0.6)
    return c, np.clip(0.3 + m * 0.5, 0, 1)


def fill(c):
    return P.fill(N, c)


@tuile("Humidite", 2, 4)
def humidite(r):
    """Traces d'humidité : coulures verticales sombres et brillantes, auréoles, sur fond neutre (couche de mélange)."""
    base = col("#3a3a3e")
    dr = fbm(N, r, 1.0, 1, 220, aniso=(0.25, 12.0))
    big = fbm(N, r, 2.4, 1, 8)
    m = smooth(0.55, 0.62, dr * 0.6 + big * 0.5)
    c = lerp(fill(base), col("#26292c"), m)
    # liseré clair au bord des auréoles (sel)
    e = smooth(0.53, 0.55, dr * 0.6 + big * 0.5) * (1 - m)
    c = lerp(c, col("#6c6a64"), e * 0.8)
    gl = smooth(0.8, 0.86, dr) * m
    c = lerp(c, col("#55625f"), gl * 0.7)
    return c, np.clip(0.2 + m * 0.3, 0, 1)


# ------------------------------------------------------------------ bois
def planches(r, pal, n=5, grain=(1.0, 40.0), noeuds=6, pourri=0.0, mouille=0.0, joint=col("#140f0c"), sat=1.0):
    """Planches verticales (fil vertical), largeurs irrégulières, nœuds, fil d'encre."""
    xx = np.mgrid[0:N, 0:N][1].astype(np.float32)
    ws = r.uniform(0.7, 1.3, n)
    ws = ws / ws.sum() * N
    xs = np.concatenate([[0], np.cumsum(ws)])
    ID = np.zeros((N, N), np.int64)
    D = np.zeros((N, N), np.float32)
    for i in range(n):
        inb = (xx >= xs[i]) & (xx < xs[i + 1])
        ID[inb] = i
        D = np.where(inb, np.minimum(xx - xs[i], xs[i + 1] - xx), D)
    # bouts de planches (joints horizontaux décalés)
    yy = np.mgrid[0:N, 0:N][0].astype(np.float32)
    cut = np.zeros((N, N), bool)
    for i in range(n):
        y0 = r.uniform(0, N)
        inb = ID == i
        dy = np.abs(((yy - y0 + N / 2) % N) - N / 2)
        D = np.where(inb, np.minimum(D, dy * 3.0 + 0.0), D)
    k = P.per_id(ID, r)
    base = np.zeros((N, N, 3), np.float32)
    idx = np.minimum((k * len(pal)).astype(int), len(pal) - 1)
    for i, c in enumerate(pal):
        base[idx == i] = c
    # fil : bruit très étiré verticalement + déformation par planche
    g = fbm(N, r, 1.3, 2, 300, aniso=(1.0, 14.0))
    g2 = fbm(N, r, 1.0, 4, 300, aniso=(1.0, 30.0))
    tone = poster(g, 4, 0.1)
    c = base * (0.78 + 0.32 * tone)[..., None]
    lines = smooth(0.62, 0.66, g2) * (1 - smooth(0.66, 0.7, g2))
    c = lerp(c, joint * 1.6, lines * 0.55)
    # nœuds (ellipses concentriques) et fil dessiné qui les contourne
    I = P.Ink(N)
    knots = [(r.uniform(0, N), r.uniform(0, N), r.uniform(7, 14)) for _ in range(noeuds)]
    for x, y, a in knots:
        for s_ in range(3):
            aa, bb = a * (1 + s_ * 0.9), a * (2.2 + s_ * 1.6)
            pts = [(x + aa * math.cos(t), y + bb * math.sin(t)) for t in np.linspace(0, 2 * math.pi, 28)]
            I.line(pts, 1.6 if s_ else 2.6)
        I.dot(x, y, a * 0.6)
    for i in range(n):
        for k in range(int(ws[i] / 16)):
            x0 = xs[i] + (k + r.uniform(0.2, 0.8)) * 16
            pts = []
            seg_on = r.random() < 0.7
            for yv in np.linspace(0, N, 48):
                dx = 0.0
                for x, y, a in knots:
                    dy = (yv - y + N / 2) % N - N / 2
                    if abs(x0 - x) < a * 6:
                        dx += math.copysign(1, x0 - x) * a * 2.2 * math.exp(-(dy / (a * 4)) ** 2)
                pts.append((x0 + dx + math.sin(yv * 0.01 + k) * 3, yv))
            j = 0
            while j < len(pts) - 1:
                L = r.integers(4, 14)
                for q in range(j, min(j + L, len(pts) - 1)):
                    I.line([pts[q], pts[q + 1]], 1.1)
                j += L + r.integers(1, 6)
    c = lerp(c, joint, I.get(0.3) * 0.65)
    # joints entre planches + lumière d'arête
    h = smooth(0, 10, D) * (0.7 + 0.3 * g)
    light = P.bevel_light(P.blur(h, 1.0), strength=8.0, levels=2)
    c = c * (1 + 0.2 * light[..., None])
    c = np.where((D < 2.2)[..., None], joint, c)
    if pourri > 0:
        rot = fbm(N, r, 2.2, 2, 30)
        m = smooth(0.62, 0.65, rot)
        c = lerp(c, col("#231a14"), m * 0.9)
        c = lerp(c, col("#3d4430"), smooth(0.7, 0.74, rot) * 0.6)
        I2 = P.Ink(N)
        P.cracks(I2, r, int(30 * pourri), N, length=(20, 120), width=(1.0, 2.4))
        c = lerp(c, joint, I2.get(0.2))
        h = h * (1 - 0.6 * m)
    if mouille > 0:
        yy2 = np.mgrid[0:N, 0:N][0].astype(np.float32) / N
        wet = np.clip(0.55 + 0.45 * np.sin(yy2 * 2 * math.pi) * 0 + (fbm(N, r, 2.0, 1, 6) - 0.5) * 0.5, 0, 1) * mouille
        c = c * (1 - 0.3 * wet[..., None])
        sh = smooth(0.78, 0.82, g2) * wet
        c = lerp(c, col("#8a8a80"), sh * 0.35)
    gray = c.mean(-1, keepdims=True)
    c = gray + (c - gray) * sat
    return np.clip(c, 0, 1), np.clip(h, 0, 1)


@tuile("BoisNeuf", 1, 1)
def bois_neuf(r):
    return planches(r, [col("#7a5434"), col("#865e3a"), col("#6e4a2f"), col("#8d6640")], n=4, noeuds=5)


@tuile("BoisPourri", 1, 1)
def bois_pourri(r):
    return planches(r, [col("#4b3b2e"), col("#55443a"), col("#433630")], n=5, noeuds=7, pourri=1.0, sat=0.7)


@tuile("BoisMouille", 1, 1)
def bois_mouille(r):
    return planches(r, [col("#4a3424"), col("#523a28"), col("#3f2c20")], n=4, noeuds=5, mouille=1.0, sat=0.85)


# ------------------------------------------------------------------ métal, plâtre, marbre, tissus
@tuile("FerRouille", 1, 1)
def fer_rouille(r):
    """Fer forgé noirci et rouille en plaques (aplats orangés à bord encré, piqûres)."""
    base = fill(col("#2f2c35"))
    mo = poster(fbm(N, r, 1.8, 3, 100), 3, 0.2)
    c = base * (0.85 + 0.3 * mo)[..., None]
    st = fbm(N, r, 1.1, 3, 300, aniso=(6.0, 1.0))
    c = c * (0.92 + 0.16 * st)[..., None]
    f = fbm(N, r, 2.0, 3, 120)
    m1 = smooth(0.58, 0.6, f)
    m2 = smooth(0.66, 0.68, f)
    c = lerp(c, col("#6b3a1e"), m1)
    c = lerp(c, col("#a35a26"), m2)
    e = smooth(0.56, 0.58, f) * (1 - m1)
    c = lerp(c, P.INK, e * 0.8)
    I = P.Ink(N)
    for _ in range(160):
        I.dot(r.uniform(0, N), r.uniform(0, N), r.uniform(2, 6))
    c = lerp(c, P.INK, I.get(0.3) * 0.85)
    dr = fbm(N, r, 1.0, 2, 200, aniso=(0.3, 10.0))
    c = lerp(c, col("#7a4321"), smooth(0.7, 0.74, dr) * 0.5)
    return c, np.clip(0.5 + m1 * 0.2 - I.get(0.3) * 0.4, 0, 1)


@tuile("PlatreFissure", 2, 2)
def platre_fissure(r):
    """Plâtre fissuré : enduit pâle sali, écaillé par plaques (la pierre apparaît), fissures en réseau."""
    base = col("#8f877b")
    mo = poster(fbm(N, r, 2.0, 2, 50), 3, 0.2)
    c = fill(base) * (0.82 + 0.25 * mo)[..., None]
    st = fbm(N, r, 1.2, 2, 200, aniso=(1.0, 6.0))
    c = c * (0.92 + 0.14 * st)[..., None]
    f = fbm(N, r, 2.6, 1, 20)
    hole = smooth(0.68, 0.7, f)
    sous = col("#4e4a50") * (0.85 + 0.3 * poster(fbm(N, r, 1.8, 6, 100), 3))[..., None]
    c = lerp(c, sous, hole)
    rim = smooth(0.672, 0.68, f) * (1 - hole)
    c = lerp(c, P.INK, rim)
    I = P.Ink(N)
    P.cracks(I, r, 16, N, length=(90, 320), width=(2.0, 3.6), branch=0.7)
    c = lerp(c, P.INK, I.get(0.3))
    dr = fbm(N, r, 1.0, 2, 220, aniso=(0.3, 10.0))
    c = c * (1 - 0.25 * smooth(0.64, 0.72, dr))[..., None]
    return c, np.clip(0.6 - hole * 0.5 - I.get(0.3) * 0.3, 0, 1)


@tuile("Marbre", 2, 2)
def marbre(r):
    """Marbre sombre veiné (sol de chapelle, salle de bal) : dalles en damier noir et blanc cassé, veines peintes."""
    ID, D, LU, LV = P.courses(N, 4, r, 0.25, 0.25, stagger=False)
    yy, xx = np.mgrid[0:N, 0:N]
    chk = ((xx // (N // 4) + yy // (N // 4)) % 2).astype(np.float32)
    a = col("#b9b1a4")
    b = col("#25222a")
    base = lerp(fill(a), fill(b), chk)
    # veines : déformation forte d'un sinus
    I = P.Ink(N)
    J = P.Ink(N)
    for _ in range(7):
        pts = P.crack_path(r, r.uniform(0, N), r.uniform(0, N), r.uniform(-0.6, 0.6) + math.pi * 0.25, r.uniform(700, 1300), step=10, wander=0.13)
        w0 = r.uniform(1.5, 4.0)
        for i in range(len(pts) - 1):
            I.line([pts[i], pts[i + 1]], w0 * (0.6 + 0.6 * math.sin(i * 0.3) ** 2))
            J.line([pts[i], pts[i + 1]], w0 * 5)
            if r.random() < 0.04:
                bb = P.crack_path(r, pts[i][0], pts[i][1], r.uniform(0, 6.28), r.uniform(60, 200), step=8, wander=0.4)
                for k in range(len(bb) - 1):
                    I.line([bb[k], bb[k + 1]], 1.0)
    vein = I.get(0.5)
    vein2 = J.get(6.0) * 0.5
    vc = np.where(chk[..., None] > 0.5, col("#6b6470"), col("#6a6160"))
    base = lerp(base, vc, np.maximum(vein, vein2 * 0.7))
    mo = poster(fbm(N, r, 2.0, 2, 40), 3, 0.2)
    base = base * (0.9 + 0.12 * mo)[..., None]
    gap = D < 2.5
    base = np.where(gap[..., None], P.INK, base)
    h = smooth(0, 6, D) * 0.6 + 0.3
    light = P.bevel_light(h, 6.0, 2)
    base = base * (1 + 0.1 * light[..., None])
    return base, h


@tuile("Tapis", 2, 2)
def tapis(r):
    """Tapis ancien : bordure et médaillons rouge sang éteint, motif géométrique, usure et trous effilochés."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    u, v = xx / N, yy / N
    fond = col("#4a1a1c")
    c = fill(fond)
    # losanges répétés (4 par tuile)
    du = np.abs(((u * 4) % 1) - 0.5)
    dv = np.abs(((v * 4) % 1) - 0.5)
    dia = du + dv
    c = lerp(c, col("#2a1e2c"), smooth(0.40, 0.41, dia) * (1 - smooth(0.47, 0.48, dia)))
    c = lerp(c, col("#9b7a3c"), smooth(0.2, 0.21, 0.5 - dia + 0.0) * (1 - smooth(0.27, 0.28, 0.5 - dia)))
    c = lerp(c, col("#6b2a25"), smooth(0.36, 0.37, 0.5 - dia))
    # trame
    tr = (np.sin(xx * 1.4) * np.sin(yy * 1.4) + 1) * 0.5
    c = c * (0.9 + 0.1 * tr)[..., None]
    usure = smooth(0.55, 0.7, fbm(N, r, 2.0, 1, 20))
    c = lerp(c, col("#3c302b"), usure * 0.6)
    mo = poster(fbm(N, r, 2.0, 3, 80), 3, 0.2)
    c = c * (0.85 + 0.2 * mo)[..., None]
    return c, np.clip(0.5 - usure * 0.2, 0, 1)


@tuile("Tenture", 2, 4)
def tenture(r):
    """Tenture lourde (velours) : plis verticaux en aplats, motif de fleurs de lys stylisées, bord brodé."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    pl = np.sin(xx / N * 2 * math.pi * 6 + (fbm(N, r, 2.0, 1, 6) - 0.5) * 3)
    plis = poster((pl + 1) / 2, 4, 0.15)
    c = fill(col("#3a1424")) * (0.6 + 0.6 * plis)[..., None]
    # motif : losanges pointés tous les 1/4
    u = (xx / N * 4) % 1 - 0.5
    v = (yy / N * 8) % 1 - 0.5
    mot = smooth(0.13, 0.12, np.abs(u) * 1.2 + np.abs(v) * 0.8)
    c = lerp(c, col("#7a5a2a") * (0.6 + 0.5 * plis)[..., None], mot * 0.8)
    st = fbm(N, r, 1.0, 3, 300, aniso=(1.0, 10.0))
    c = c * (0.92 + 0.14 * st)[..., None]
    pous = smooth(0.6, 0.75, fbm(N, r, 2.0, 1, 10))
    c = lerp(c, col("#4a4044"), pous * 0.4)
    return c, np.clip(0.3 + plis * 0.5, 0, 1)


# ------------------------------------------------------------------ sols naturels
@tuile("Terre", 2, 2)
def terre(r):
    """Terre battue : mottes en aplats, cailloux, traces, flaques sombres."""
    f = fbm(N, r, 2.0, 2, 120)
    t = poster(f, 4, 0.2)
    c = lerp(fill(col("#2e2520")), fill(col("#5a4836")), t)
    F1, F2, ID, _, _ = P.cells(N, 14, 14, r, 0.9)
    mot = P.edge_dist(F1, F2)
    c = lerp(c, col("#221a16"), smooth(0.04, 0.0, mot) * 0.6)
    I = P.Ink(N)
    for _ in range(400):
        x, y, rad = r.uniform(0, N), r.uniform(0, N), r.uniform(1.5, 6)
        I.dot(x, y, rad)
    peb = I.get(0.3)
    c = lerp(c, col("#6e6258"), peb * 0.75)
    c = lerp(c, P.INK, (P.blur(peb, 1.5) - peb).clip(0, 1) * 1.5)
    flq = smooth(0.8, 0.82, fbm(N, r, 2.4, 1, 12))
    c = lerp(c, col("#262019"), flq * 0.6)
    return c, np.clip(f * 0.6 + peb * 0.3 - flq * 0.3, 0, 1)


@tuile("HerbeSeche", 3, 3)
def herbe_seche(r):
    """Herbe sèche : touffes de brins jaunes-gris dessinés à l'encre sur terre sombre."""
    c, h = terre(r)
    c = c * 0.85
    I = P.Ink(N)
    J = P.Ink(N)
    for _ in range(260):
        x, y = r.uniform(0, N), r.uniform(0, N)
        for _ in range(r.integers(5, 12)):
            a = -math.pi / 2 + r.normal(0, 0.45)
            L = r.uniform(18, 46)
            pts = [(x + r.normal(0, 3), y)]
            bx, by = pts[0]
            for s in range(4):
                a += r.normal(0, 0.12)
                bx += math.cos(a) * L / 4
                by += math.sin(a) * L / 4
                pts.append((bx, by))
            J.line(pts, 3.4)
            I.line(pts, 1.6)
    out = J.get(0.3)
    inn = I.get(0.3)
    tone = poster(fbm(N, r, 2.0, 2, 40), 3)
    gc = lerp(fill(col("#6b6238")), fill(col("#9c8a4e")), tone)
    c = lerp(c, P.INK, out * 0.9)
    c = lerp(c, gc, inn)
    return c, np.clip(h * 0.5 + inn * 0.5, 0, 1)


@tuile("Ecorce", 1, 2)
def ecorce(r):
    """Écorce d'arbre mort : sillons verticaux profonds à l'encre, plaques grises, lichen pâle."""
    wx = (fbm(N, r, 2.0, 2, 30) - 0.5) * 30
    F1, F2, ID, _, _ = P.cells(N, 10, 4, r, 0.9, wx=wx, wy=np.zeros((N, N), np.float32))
    D = P.edge_dist(F1, F2) * N / 10
    pal = [col("#3b3433"), col("#443c38"), col("#352f31")]
    c, h = maconnerie(r, ID, D, pal, gap=3.5, bev=22, ink_w=2.6, fiss=6, mortier=col("#0e0c0d"), val_var=0.12,
                      ecl=0.3, creux=0.5)
    st = fbm(N, r, 1.0, 2, 300, aniso=(1.0, 14.0))
    c = c * (0.85 + 0.3 * poster(st, 3))[..., None]
    lich = smooth(0.66, 0.68, fbm(N, r, 1.8, 4, 80))
    c = lerp(c, col("#7d8270"), lich * 0.7)
    return c, h


@tuile("Racines", 2, 2)
def racines(r):
    """Racines qui courent sur de la terre : tubes encrés qui serpentent et se divisent."""
    c, h = terre(r)
    c = c * 0.8
    O = P.Ink(N)
    I = P.Ink(N)
    L = P.Ink(N)
    for _ in range(6):
        x, y = r.uniform(0, N), r.uniform(0, N)
        a = r.uniform(0, 2 * math.pi)
        w = r.uniform(26, 52)
        pts = P.crack_path(r, x, y, a, r.uniform(600, 1000), step=14, wander=0.16)
        for i in range(len(pts) - 1):
            t = i / len(pts)
            ww = w * (1 - 0.7 * t)
            O.line([pts[i], pts[i + 1]], ww + 4)
            I.line([pts[i], pts[i + 1]], ww)
            L.line([(pts[i][0] - ww * 0.2, pts[i][1] - ww * 0.25), (pts[i + 1][0] - ww * 0.2, pts[i + 1][1] - ww * 0.25)], ww * 0.3)
            if r.random() < 0.05:
                b = P.crack_path(r, pts[i][0], pts[i][1], a + r.normal(0, 1.2), r.uniform(80, 250), step=8, wander=0.3)
                for j in range(len(b) - 1):
                    bw = max(3.0, ww * 0.4 * (1 - j / len(b)))
                    O.line([b[j], b[j + 1]], bw + 3)
                    I.line([b[j], b[j + 1]], bw)
    o, i_, l_ = O.get(0.4), I.get(0.4), L.get(0.6)
    rc = lerp(fill(col("#4a3a2e")), fill(col("#6b5440")), poster(fbm(N, r, 1.2, 4, 200, aniso=(4, 4)), 3))
    c = lerp(c, P.INK, o)
    c = lerp(c, rc, i_)
    c = lerp(c, col("#8a7258"), l_ * i_ * 0.7)
    return c, np.clip(h * 0.4 + i_ * 0.6, 0, 1)


@tuile("Gravier", 2, 2)
def gravier(r):
    """Gravier d'allée : petits cailloux gris-beige en aplats encrés, serrés."""
    F1, F2, ID, _, _ = P.cells(N, 30, 30, r, 0.95)
    D = P.edge_dist(F1, F2) * N / 30
    pal = [col("#7d786f"), col("#8a8377"), col("#6c6a6a"), col("#948b7c"), col("#726a63")]
    c, h = maconnerie(r, ID, D, pal, gap=2.2, bev=6, ink_w=1.4, fiss=0, mortier=col("#1d1a1a"), val_var=0.18,
                      creux=0.4, ecl=0.3)
    return c, h


# ------------------------------------------------------------------ château (acte II, nuit 4) : plus aucune texture de cave
@tuile("PierreClaire", 4, 3)
def pierre_claire(r):
    """Pierre de taille claire (calcaire) du château : assises de 43 cm, blocs de 60 cm à 1.2 m, joints fins et clairs,
    arêtes nettes, peu de fissures ; une pierre plus chaude ou plus grise de temps en temps (appareil soigné)."""
    M = N * 3 // 4
    ID, D, _, _ = P.courses(N, 7, r, 0.15, 0.30, M=M, row_jit=0.04)
    pal = [col("#9a9182"), col("#a39a89"), col("#938a7c"), col("#a89d8a"), col("#8f877b")]
    return maconnerie(r, ID, D, pal, gap=2.2, bev=14, ink_w=1.6, fiss=3, mortier=col("#6d665c"), val_var=0.07,
                      creux=0.18, ecl=0.32, M=M, mottle=3, marbrure=0.06)


@tuile("Parquet", 2, 2)
def parquet(r):
    """Parquet de château : lames de 15 cm de large et 0.6 à 1.2 m de long, joints décalés, fil du bois, cire usée."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    n = 13                                      # 13 lames sur 2 m (15.4 cm)
    w = N / n
    row = np.floor(xx / w).astype(int)
    off = r.uniform(0, N, n)
    lens = r.uniform(0.3, 0.6, n) * N           # 0.6 à 1.2 m
    v = (yy + off[row]) % N
    seg = np.floor(v / lens[row]).astype(int)
    ID = row * 64 + seg
    dx = np.minimum(xx - row * w, (row + 1) * w - xx)
    dv = np.minimum(v % lens[row], lens[row] - v % lens[row])
    D = np.minimum(dx, dv)
    pal = [col("#5a3b23"), col("#64432a"), col("#4f341f"), col("#6b4a2e"), col("#573a24")]
    k = P.per_id(ID, r)
    base = np.zeros((N, N, 3), np.float32)
    for i, c0 in enumerate(pal):
        base[(k * len(pal)).astype(int).clip(0, len(pal) - 1) == i] = c0
    g = fbm(N, r, 1.0, 4, 300, aniso=(30.0, 1.0))            # fil du bois le long des lames (vertical)
    tone = poster(g, 4, 0.1)
    c = base * (0.82 + 0.28 * tone)[..., None]
    wax = smooth(0.45, 0.8, fbm(N, r, 2.2, 1, 8))            # cire usée : zones plus mates
    c = c * (1 + 0.08 * wax[..., None])
    h = smooth(0, 6 * K, D)
    light = P.bevel_light(P.blur(h, 1.0), strength=6.0, levels=2)
    c = c * (1 + 0.15 * light[..., None])
    c = np.where((D < 1.6 * K)[..., None], col("#1a110b"), c)
    return np.clip(c, 0, 1), np.clip(h * (0.7 + 0.3 * g), 0, 1)


@tuile("Boiserie", 2, 2)
def boiserie(r):
    """Boiseries (lambris) : panneaux de 0.8 x 1 m à cadre mouluré (montants et traverses de 10 cm), panneau en creux à
    biseau ; chêne sombre ciré."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    pw, ph = N / 2.5, N / 2.0                       # 0.8 x 1 m
    fx, fy = xx % pw, yy % ph
    cad = 0.1 * N / 2                               # cadre de 10 cm
    d_bord = np.minimum(np.minimum(fx, pw - fx), np.minimum(fy, ph - fy))
    cadre = d_bord < cad
    biseau = (d_bord >= cad) & (d_bord < cad + 0.035 * N / 2)
    base = fill(col("#4a3020"))
    g = fbm(N, r, 1.0, 4, 260, aniso=(1.0, 26.0))
    c = base * (0.84 + 0.26 * poster(g, 4, 0.1))[..., None]
    c = np.where(cadre[..., None], c * 1.12, c)
    # biseau : côtés haut et gauche éclairés, bas et droit à l'ombre (lumière d'en haut à gauche) ; le côté est celui
    # dont le bord du panneau est le plus proche
    dists = np.stack([fx, pw - fx, fy, ph - fy])              # gauche, droite, haut, bas
    haut = np.isin(dists.argmin(0), (0, 2))
    c = np.where((biseau & haut)[..., None], c * 1.25, c)
    c = np.where((biseau & ~haut)[..., None], c * 0.62, c)
    ink = (np.abs(d_bord - cad) < 1.2 * K) | (np.abs(d_bord - cad - 0.035 * N / 2) < 1.0 * K) | (d_bord < 1.4 * K)
    c = lerp(c, P.INK, ink.astype(np.float32)[..., None] * 0.85)
    h = np.where(cadre, 0.85, np.where(biseau, 0.55, 0.45)) * (0.9 + 0.1 * g)
    return np.clip(c, 0, 1), np.clip(h, 0, 1)


@tuile("PlatrePeint", 2, 2)
def platre_peint(r):
    """Plâtre peint : enduit lisse teinté vert-de-gris sombre, motif au pochoir effacé (fleurons en losange de 50 cm),
    usure et quelques fissures fines."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    base = fill(col("#3e4a44"))
    mo = poster(fbm(N, r, 2.2, 2, 40), 3, 0.2)
    c = base * (0.86 + 0.22 * mo)[..., None]
    # pochoir : losanges et fleurons (période 50 cm), estompés
    per = N / 4
    u, v = (xx % per) / per - 0.5, (yy % per) / per - 0.5
    los = np.abs(u) + np.abs(v)
    motif = (np.abs(los - 0.32) < 0.025) | ((u * u + v * v) < 0.012) | ((np.abs(u) < 0.012) & (np.abs(v) < 0.2)) | ((np.abs(v) < 0.012) & (np.abs(u) < 0.2))
    efface = smooth(0.35, 0.65, fbm(N, r, 2.0, 2, 30))
    c = lerp(c, col("#6f7a63"), (motif * efface * 0.55)[..., None])
    I = P.Ink(N)
    P.cracks(I, r, 6, N, length=(60, 220), width=(1.0, 1.8), branch=0.5)
    c = lerp(c, P.INK, I.get(0.3) * 0.8)
    dr = fbm(N, r, 1.0, 2, 220, aniso=(0.3, 10.0))
    c = c * (1 - 0.2 * smooth(0.66, 0.74, dr))[..., None]
    return np.clip(c, 0, 1), np.clip(0.55 + 0.1 * mo - I.get(0.3) * 0.3, 0, 1)


@tuile("Reliures", 0.5, 0.5)
def reliures(r):
    """Dos de livres (rayons de la bibliothèque) : reliures de 3 à 7 cm, cuir sombre rouge, vert, brun, bleu nuit, nerfs et
    filets dorés ternis, titres en traits. Tuile de 50 cm (les livres ne portent plus le motif du tapis)."""
    yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
    ws = []
    while sum(ws) < N:
        ws.append(r.uniform(0.06, 0.14) * N)
    ws = np.array(ws) * N / sum(ws)
    xs = np.concatenate([[0], np.cumsum(ws)])
    pal = [col("#4a1c1a"), col("#22331f"), col("#3b2716"), col("#1d2436"), col("#5a3a1c"), col("#2b1a22")]
    c = np.zeros((N, N, 3), np.float32)
    h = np.zeros((N, N), np.float32)
    for i in range(len(ws)):
        inb = (xx >= xs[i]) & (xx < xs[i + 1])
        c[inb] = pal[r.integers(0, len(pal))] * r.uniform(0.85, 1.15)
        u = (xx - xs[i]) / max(ws[i], 1)
        rond = 0.75 + 0.35 * np.sin(np.clip(u, 0, 1) * math.pi)       # dos arrondi
        c[inb] *= rond[inb][..., None]
        h[inb] = rond[inb] * 0.8
        for y0 in r.uniform(0.1, 0.9, r.integers(2, 4)) * N:          # nerfs et filets dorés
            band = inb & (np.abs(yy - y0) < 0.008 * N)
            c[band] = col("#8a7240")
    g = fbm(N, r, 1.2, 3, 200, aniso=(1.0, 4.0))
    c = c * (0.9 + 0.2 * g)[..., None]
    edge = np.zeros((N, N), bool)
    for x0 in xs:
        edge |= np.abs(xx - x0) < 1.6 * K
    c = np.where(edge[..., None], P.INK, c)
    return np.clip(c, 0, 1), np.clip(h, 0, 1)


RES.update({"Moellons": 2048, "PierreTaillee": 2048, "Dalles": 2048, "RocheCave": 2048, "PierreClaire": 2048})


TOUS = [moellons, pierre_taillee, dalles, roche_cave, briques, mortier, mousse, salpetre, humidite,
        bois_neuf, bois_pourri, bois_mouille, fer_rouille, platre_fissure, marbre, tapis, tenture,
        terre, herbe_seche, ecorce, racines, gravier, pierre_claire, parquet, boiserie, platre_peint, reliures]


def main(noms=None):
    os.makedirs(OUT, exist_ok=True)
    os.makedirs(DOC, exist_ok=True)
    faits = []
    for i, f in enumerate(TOUS):
        if noms and f.nom not in noms:
            continue
        r = P.rng(1000 + i)
        resolution(RES.get(f.nom, 1024))
        c, h = f(r)
        P.save(os.path.join(OUT, f"T_Mat_{f.nom}_BaseColor.png"), c, height=h)
        faits.append(f.nom)
        print("ok", f.nom, TAILLE[f.nom], flush=True)
    return faits


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    main(argv or None)
