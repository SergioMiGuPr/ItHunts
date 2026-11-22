# -*- coding: utf-8 -*-
"""Trim sheet et atlas de décalcomanies (lot « matériaux », 2026-11-22). Procédural, sans photo ni IA.

T_Mat_Trim_BaseColor.png (1024 × 1024, bandes horizontales qui se raccordent en x ; la tuile couvre 2 m de large) :
les bandes et leurs coordonnées v sont dans BANDES (reprises dans docs/art/MATERIAUX.md).
T_Mat_Decals.png (2048 × 2048, RVBA, 4 × 4 cases de 512 px) : CASES donne le nom de chaque case (ligne, colonne).
"""
import os, sys, math
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np
import peint as P
from peint import col, smooth, poster, fbm, lerp
import materiaux as MAT

OUT = MAT.OUT
N = 1024

# nom, v0, v1 en pixels depuis le haut de l'image (v Unity = 1 - y / 1024)
BANDES = [
    ("Corniche moulurée (pierre)", 0, 176),
    ("Bandeau de pierre taillée (une assise)", 176, 336),
    ("Plinthe sale et humide", 336, 464),
    ("Poutre de bois fendue", 464, 592),
    ("Bande de fer cloutée", 592, 688),
    ("Frise de briques (deux assises)", 688, 784),
    ("Nez de marche / bordure de dalle", 784, 896),
    ("Galon de tenture brodé", 896, 960),
    ("Corde", 960, 1024),
]


def strip_pierre(r, h, pal, profil=None, ink=P.INK):
    """Bande de pierre de hauteur h (px), largeur N, raccord en x."""
    yy, xx = np.mgrid[0:h, 0:N].astype(np.float32)
    v = yy / h
    if profil is None:
        prof = np.zeros_like(v)
    else:
        prof = profil(v)
    mo = poster(fbm(N, r, 2.0, 3, 40, M=h), 3, 0.15)
    c = np.broadcast_to(pal, (h, N, 3)) * (0.85 + 0.25 * mo)[..., None]
    return c, prof


def moulure(v):
    """Profil de corniche : tore, gorge, filets (valeur d'éclairage -1..1 par hauteur)."""
    s = np.zeros_like(v)
    s = np.where(v < 0.12, 0.5, s)                      # filet supérieur éclairé
    s = np.where((v >= 0.12) & (v < 0.16), -1.0, s)      # ombre sous le filet
    t = (v - 0.16) / 0.3
    s = np.where((v >= 0.16) & (v < 0.46), np.cos(t * math.pi) * 0.9, s)   # tore
    s = np.where((v >= 0.46) & (v < 0.52), -1.0, s)
    s = np.where((v >= 0.52) & (v < 0.72), -0.45, s)     # gorge
    s = np.where((v >= 0.72) & (v < 0.78), 0.6, s)       # listel
    s = np.where((v >= 0.78) & (v < 0.84), -0.9, s)
    t2 = (v - 0.84) / 0.16
    s = np.where(v >= 0.84, 0.3 - t2 * 0.9, s)           # talon
    return s


def trim(r):
    img = np.zeros((N, N, 3), np.float32)
    hgt = np.zeros((N, N), np.float32)
    # 1 corniche
    v0, v1 = BANDES[0][1:]
    h = v1 - v0
    c, prof = strip_pierre(r, h, col("#77706d"), moulure)
    pp = poster((prof + 1) / 2, 4) * 2 - 1
    c = c * (1 + 0.3 * pp[..., None])
    # joints verticaux tous les 50 cm, ébréchures
    xx = np.mgrid[0:h, 0:N][1]
    j = (xx % 256) < 3
    c = np.where(j[..., None], P.INK, c)
    edges = np.abs(np.diff(pp, axis=0, prepend=pp[:1])) > 0.4
    c = lerp(c, P.INK, edges.astype(np.float32) * 0.8)
    I = P.Ink(N, h)
    P.cracks(I, r, 6, N, length=(20, 70), width=(1.0, 2.0), M=h)
    c = lerp(c, P.INK, I.get(0.3))
    img[v0:v1] = c
    hgt[v0:v1] = (prof + 1) / 2
    # 2 bandeau : une assise de pierre taillée
    v0, v1 = BANDES[1][1:]
    h = v1 - v0
    ID, D, _, _ = P.courses(N, 1, r, 0.2, 0.32, M=h)
    pal = [col("#77706d"), col("#6f6a6a"), col("#7c756e"), col("#726c70")]
    c, hh = MAT.maconnerie(r, ID, D, pal, gap=3.0, bev=16, fiss=3, M=h, mortier=col("#3a3436"))
    img[v0:v1] = c
    hgt[v0:v1] = hh
    # 3 plinthe sale : pierre + saleté montante + humidité
    v0, v1 = BANDES[2][1:]
    h = v1 - v0
    ID, D, _, _ = P.courses(N, 1, r, 0.25, 0.4, M=h)
    c, hh = MAT.maconnerie(r, ID, D, pal, gap=3.0, bev=14, fiss=4, M=h, mortier=col("#2a2628"))
    yy = np.mgrid[0:h, 0:N][0].astype(np.float32) / h
    dirt = smooth(0.25, 0.9, yy + (fbm(N, r, 2.0, 2, 30, M=h) - 0.5) * 0.5)
    c = lerp(c, c * 0.45 + col("#1b1a17") * 0.3, poster(dirt, 3, 0.2) * 0.9)
    dr = fbm(N, r, 1.0, 2, 200, aniso=(0.3, 10.0), M=h)
    c = lerp(c, col("#2a3030"), smooth(0.62, 0.7, dr) * 0.6)
    img[v0:v1] = c
    hgt[v0:v1] = hh
    # 4 poutre de bois : planche unique horizontale (fil horizontal), fentes
    v0, v1 = BANDES[3][1:]
    h = v1 - v0
    g = fbm(N, r, 1.3, 2, 300, aniso=(14.0, 1.0), M=h)
    tone = poster(g, 4, 0.1)
    c = lerp(np.broadcast_to(col("#4a3424"), (h, N, 3)), np.broadcast_to(col("#7a5434"), (h, N, 3)), tone)
    yy = np.mgrid[0:h, 0:N][0].astype(np.float32) / h
    sh = np.where(yy < 0.12, 0.25, np.where(yy > 0.85, -0.4, 0.0))
    c = c * (1 + sh[..., None])
    I = P.Ink(N, h)
    for _ in range(9):
        y = r.uniform(h * 0.2, h * 0.8)
        x = r.uniform(0, N)
        pts = P.crack_path(r, x, y, r.choice([0, math.pi]) + r.normal(0, 0.05), r.uniform(80, 300), step=8, wander=0.06)
        for i in range(len(pts) - 1):
            I.line([pts[i], pts[i + 1]], 2.4 * (1 - i / len(pts)) + 0.6)
    c = lerp(c, P.INK, I.get(0.3))
    c[:3] = P.INK
    c[-3:] = P.INK
    img[v0:v1] = c
    hgt[v0:v1] = 0.6 + (g - 0.5) * 0.2
    # 5 bande de fer cloutée
    v0, v1 = BANDES[4][1:]
    h = v1 - v0
    fr, _ = MAT.fer_rouille(r)
    c = fr[:h].copy() * 0.9
    yy = np.mgrid[0:h, 0:N][0].astype(np.float32) / h
    c = c * (1 + np.where(yy < 0.15, 0.3, np.where(yy > 0.85, -0.45, 0.0))[..., None])
    I = P.Ink(N, h)
    L = P.Ink(N, h)
    for k in range(8):
        x = (k + 0.5) * N / 8
        I.dot(x, h / 2, 13)
        L.dot(x - 4, h / 2 - 4, 5)
    c = lerp(c, P.INK, I.get(0.4))
    c = lerp(c, col("#3d3942"), np.clip(I.get(0.4) * 1.5 - 0.6, 0, 1))
    c = lerp(c, col("#7a7480"), L.get(0.5) * 0.8)
    c[:2] = P.INK
    c[-2:] = P.INK
    img[v0:v1] = c
    hgt[v0:v1] = 0.7 + I.get(0.4) * 0.3
    # 6 frise de briques
    v0, v1 = BANDES[5][1:]
    h = v1 - v0
    ID, D, _, _ = P.courses(N, 2, r, 0.06, 0.08, M=h)
    c, hh = MAT.maconnerie(r, ID, D, [col("#5c3d36"), col("#6a463b"), col("#4f3532")], gap=3.0, bev=8, ink_w=1.6,
                           fiss=2, M=h, mortier=col("#2b2626"))
    img[v0:v1] = c
    hgt[v0:v1] = hh
    # 7 nez de marche : dessus clair usé, arrondi, puis contremarche sombre
    v0, v1 = BANDES[6][1:]
    h = v1 - v0
    ID, D, _, _ = P.courses(N, 1, r, 0.3, 0.5, M=h)
    c, hh = MAT.maconnerie(r, ID, D, [col("#6f6a6a"), col("#77706d")], gap=3.0, bev=10, fiss=3, M=h)
    yy = np.mgrid[0:h, 0:N][0].astype(np.float32) / h
    c = c * (1 + np.where(yy < 0.35, 0.22, np.where(yy < 0.45, 0.0, -0.35))[..., None])
    c = lerp(c, P.INK, ((yy > 0.44) & (yy < 0.47)).astype(np.float32))
    img[v0:v1] = c
    hgt[v0:v1] = hh
    # 8 galon brodé
    v0, v1 = BANDES[7][1:]
    h = v1 - v0
    xx, yy = np.mgrid[0:h, 0:N][1].astype(np.float32), np.mgrid[0:h, 0:N][0].astype(np.float32)
    c = np.broadcast_to(col("#3a1424"), (h, N, 3)).copy()
    u = (xx / N * 16) % 1 - 0.5
    v = yy / h - 0.5
    lo = np.abs(u) + np.abs(v) * 1.2 < 0.32
    c = np.where(lo[..., None], col("#8a6a30"), c)
    c = np.where(((np.abs(v) > 0.38) & (np.abs(v) < 0.45))[..., None], col("#7a5a2a"), c)
    img[v0:v1] = c
    hgt[v0:v1] = 0.5 + lo * 0.2
    # 9 corde : torsades obliques
    v0, v1 = BANDES[8][1:]
    h = v1 - v0
    xx, yy = np.mgrid[0:h, 0:N][1].astype(np.float32), np.mgrid[0:h, 0:N][0].astype(np.float32)
    t = ((xx + yy * 0.9) / 28) % 1
    c = lerp(np.broadcast_to(col("#4a3a26"), (h, N, 3)), np.broadcast_to(col("#8a7250"), (h, N, 3)), poster(t, 3))
    c = lerp(c, P.INK, (t > 0.9).astype(np.float32))
    edge = np.abs(yy / h - 0.5) * 2
    c = c * (1 - 0.6 * smooth(0.6, 1.0, edge))[..., None]
    img[v0:v1] = c
    hgt[v0:v1] = 1 - edge
    P.save(os.path.join(OUT, "T_Mat_Trim_BaseColor.png"), img, height=hgt)


# ------------------------------------------------------------------ décalcomanies
CASES = [
    ["Fissure longue", "Fissure en étoile", "Fissures fines (réseau)", "Éclat d'angle"],
    ["Tache d'humidité", "Tache sombre (suie)", "Tache de sang séché", "Auréole de salpêtre"],
    ["Mousse (plaque)", "Mousse (coulée de joint)", "Lichen pâle", "Éclats et gravats au sol"],
    ["Coulure longue", "Coulures multiples", "Coulure de rouille", "Toile d'araignée"],
]
S = 512


def blob(r, rad, rough=0.35, n=S):
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    d = np.sqrt((xx - n / 2) ** 2 + (yy - n / 2) ** 2) / (n / 2)
    no = fbm(n, r, 2.2, 2, 14) * 0.8 + fbm(n, r, 1.6, 10, 60) * 0.2
    return d + (no - 0.5) * rough * 2 - rad


def decal(r, kind):
    rgb = np.zeros((S, S, 3), np.float32)
    a = np.zeros((S, S), np.float32)
    yy, xx = np.mgrid[0:S, 0:S].astype(np.float32)
    fade = 1 - smooth(0.82, 0.98, np.maximum(np.abs(xx / S - 0.5), np.abs(yy / S - 0.5)) * 2)
    if kind.startswith("Fissure") or kind.startswith("Fissures"):
        I = P.Ink(S)
        L = P.Ink(S)
        if kind == "Fissure longue":
            pts = P.crack_path(r, 50, S / 2, r.normal(0, 0.1), S - 110, step=8, wander=0.05)
            for i in range(len(pts) - 1):
                w = 7 * math.sin(math.pi * i / len(pts)) + 1
                I.line([pts[i], pts[i + 1]], w)
                L.line([(pts[i][0], pts[i][1] + w * 0.8), (pts[i + 1][0], pts[i + 1][1] + w * 0.8)], w * 0.5)
                if r.random() < 0.15:
                    b = P.crack_path(r, *pts[i], r.uniform(0, 6.28), r.uniform(30, 90), step=6, wander=0.5)
                    for k in range(len(b) - 1):
                        I.line([b[k], b[k + 1]], max(1, w * 0.4 * (1 - k / len(b))))
        elif kind == "Fissure en étoile":
            for k in range(9):
                ang = k * 2 * math.pi / 9 + r.normal(0, 0.2)
                pts = P.crack_path(r, S / 2, S / 2, ang, r.uniform(120, 230), step=7, wander=0.3)
                for i in range(len(pts) - 1):
                    w = 6 * (1 - i / len(pts)) + 0.8
                    I.line([pts[i], pts[i + 1]], w)
            I.dot(S / 2, S / 2, 14)
        else:
            P.cracks(I, r, 26, S, length=(40, 160), width=(1.0, 2.4), branch=0.7)
        ink = I.get(0.4)
        lip = L.get(0.6) * (1 - ink)
        rgb[:] = P.INK
        rgb = lerp(rgb, col("#8a8590"), lip)
        a = np.maximum(ink, lip * 0.5)
        if kind == "Fissures fines (réseau)":
            a = a * (1 - smooth(0.6, 1.0, np.sqrt((xx - S / 2) ** 2 + (yy - S / 2) ** 2) / (S / 2)))
    elif kind == "Éclat d'angle":
        b = blob(r, 0.55, 0.25)
        m = smooth(0.02, -0.02, b)
        sh = P.bevel_light(P.blur(m, 6), 25, 2)
        rgb[:] = col("#4e4a50") * (1 + 0.3 * sh)[..., None]
        e = smooth(0.03, 0.0, np.abs(b)) * 1.0
        rgb = lerp(rgb, P.INK, e)
        a = np.maximum(m, e)
    elif kind in ("Tache d'humidité", "Tache sombre (suie)", "Tache de sang séché", "Auréole de salpêtre"):
        b = blob(r, 0.62, 0.45)
        m = poster(smooth(0.25, -0.35, b), 3, 0.2)
        c = {"Tache d'humidité": col("#1e2426"), "Tache sombre (suie)": col("#0c0b0c"),
             "Tache de sang séché": col("#3a0e0e"), "Auréole de salpêtre": col("#c8c3b6")}[kind]
        rgb[:] = c
        a = m * (0.75 if kind != "Auréole de salpêtre" else 0.0)
        if kind == "Auréole de salpêtre":
            ring = smooth(0.06, 0.0, np.abs(b + 0.04)) + smooth(0.04, 0.0, np.abs(b + 0.22)) * 0.6
            a = np.clip(ring, 0, 1) * 0.9
        if kind == "Tache de sang séché":
            dr = fbm(S, r, 1.0, 2, 200, aniso=(0.3, 10.0))
            drip = smooth(0.62, 0.7, dr) * smooth(0.1, 0.5, yy / S) * smooth(0.3, 0.0, np.abs(xx / S - 0.5))
            a = np.maximum(a, drip * 0.8)
            rgb = lerp(rgb, col("#250808"), poster(smooth(0.0, -0.4, b), 2))
    elif kind.startswith("Mousse") or kind == "Lichen pâle":
        if kind == "Mousse (coulée de joint)":
            b = np.abs(yy / S - 0.55 - (fbm(S, r, 2.0, 1, 8) - 0.5) * 0.2) * 6 + (fbm(S, r, 2.0, 2, 40) - 0.5) * 1.4 - 0.4
            b = np.maximum(b, np.abs(xx / S - 0.5) * 2.4 - 1.0)
        else:
            b = blob(r, 0.6, 0.55)
        m = smooth(0.03, -0.03, b)
        dome = P.blur(m, 8)
        sh = P.bevel_light(dome, 30, 2)
        t = poster(fbm(S, r, 1.8, 6, 60), 3, 0.2)
        if kind == "Lichen pâle":
            c = lerp(P.fill(S, col("#5e6658")), P.fill(S, col("#8a917c")), t)
        else:
            c = lerp(P.fill(S, col("#1c2419")), P.fill(S, col("#33482a")), t)
        rgb = c * (1 + 0.35 * sh)[..., None]
        e = smooth(0.06, 0.0, np.abs(b)) * (1 - m * 0.5)
        rgb = lerp(rgb, P.INK, e * 0.7)
        a = np.maximum(m, e * 0.8)
    elif kind == "Éclats et gravats au sol":
        I = P.Ink(S)
        O = P.Ink(S)
        for _ in range(60):
            rr = abs(r.normal(0, 0.22)) * S
            ang = r.uniform(0, 6.28)
            x, y = S / 2 + math.cos(ang) * rr, S / 2 + math.sin(ang) * rr * 0.8
            k = r.uniform(4, 22)
            pts = [(x + math.cos(t) * k * r.uniform(0.6, 1.2), y + math.sin(t) * k * r.uniform(0.6, 1.2)) for t in np.linspace(0, 6.28, 7)[:-1]]
            O.poly([(px + (px - x) * 0.25, py + (py - y) * 0.25) for px, py in pts])
            I.poly(pts)
        o, i_ = O.get(0.5), I.get(0.5)
        rgb = lerp(P.fill(S, P.INK), P.fill(S, col("#6e6a68")), i_)
        hl = np.roll(np.roll(i_, 3, 0), 3, 1)
        rgb = rgb * (1 + 0.25 * (i_ - hl).clip(0, 1))[..., None]
        a = np.maximum(o, i_)
    elif kind.startswith("Coulure"):
        k = {"Coulure longue": 1, "Coulures multiples": 5, "Coulure de rouille": 3}[kind]
        I = P.Ink(S)
        for j in range(k):
            x = S / 2 + (j - (k - 1) / 2) * 60 + r.normal(0, 10)
            w = r.uniform(30, 44) if k == 1 else r.uniform(16, 28)
            L = r.uniform(0.5, 0.95) * S
            pts = [(x + 4 * math.sin(y * 0.03 + j), y) for y in np.linspace(10, L, 30)]
            for i in range(len(pts) - 1):
                I.line([pts[i], pts[i + 1]], w * (1 - 0.5 * i / len(pts)))
            I.dot(pts[-1][0], pts[-1][1], w * 0.7)
        m = poster(I.get(3.0), 3, 0.3)
        c = col("#5a2a14") if "rouille" in kind else col("#161a1b")
        rgb[:] = c
        rgb = lerp(rgb, c * 1.6, poster(np.roll(m, -4, 1) * (1 - m), 2) * 0)
        a = m * 0.8 * smooth(0.0, 0.15, yy / S)
    elif kind == "Toile d'araignée":
        I = P.Ink(S)
        cx, cy = 20, 20
        rays = [r.uniform(0.0, math.pi / 2) for _ in range(9)]
        for ang in rays:
            I.line([(cx, cy), (cx + math.cos(ang) * 600, cy + math.sin(ang) * 600)], 1.2)
        rays.sort()
        for ring in range(1, 11):
            rad = ring * 42 * r.uniform(0.9, 1.1)
            pts = []
            for ang in rays:
                pts.append((cx + math.cos(ang) * rad, cy + math.sin(ang) * rad))
            for i in range(len(pts) - 1):
                mx, my = (pts[i][0] + pts[i + 1][0]) / 2, (pts[i][1] + pts[i + 1][1]) / 2
                sag = 0.92
                I.line([pts[i], (cx + (mx - cx) * sag, cy + (my - cy) * sag), pts[i + 1]], 1.1)
        rgb[:] = col("#c9c6cf")
        a = I.get(0.3) * 0.85
        fade = 1 - smooth(0.7, 1.0, np.sqrt(xx ** 2 + yy ** 2) / S)
    return rgb, np.clip(a * fade, 0, 1)


def decals(r):
    big = np.zeros((S * 4, S * 4, 3), np.float32)
    A = np.zeros((S * 4, S * 4), np.float32)
    for i, row in enumerate(CASES):
        for j, kind in enumerate(row):
            rgb, a = decal(r, kind)
            big[i * S:(i + 1) * S, j * S:(j + 1) * S] = rgb
            A[i * S:(i + 1) * S, j * S:(j + 1) * S] = a
            print("décal", kind, flush=True)
    P.save(os.path.join(OUT, "T_Mat_Decals.png"), big, alpha=A)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    trim(P.rng(77))
    decals(P.rng(78))
    print("ok")
