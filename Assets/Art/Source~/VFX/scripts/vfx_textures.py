# -*- coding: utf-8 -*-
"""Textures des effets peints (lot D, 2026-11-21) : formes dessinées en aplats, trait d'encre, pas de particules réalistes.
Chaque texture code la forme, pas la couleur (la couleur vient d'une rampe de 3 tons + encre dans le shader
ItHunts/VFX/Peint, réglable par effet) :
  R = ton (0 = encre, ~0,33 = ton sombre, ~0,66 = ton moyen, 1 = cœur clair) ; G = bruit de dissolution (la particule
  s'efface par morceaux, comme un trait qui se défait, au lieu d'un fondu) ; B = 0 ; A = couverture (bord net, 1 px adouci).
Planches : flamme 4 x 4 en boucle, fumée 4 x 4 (bouffée qui gonfle et se défait), éclat 2 x 2 (étoile qui se referme),
étincelle, étoile à 5 branches, anneau, trace (bande effilée), lien (bande ondulée raccordable en x), poussière 4 x 4,
arc de lame 2 x 2 (2026-11-22 : croissant balayé qui se déploie puis se défait).
Sorties : Assets/Art/VFX/Textures/T_VFX_*.png ; planche de contrôle docs/art/rendus/VFX/Textures_VFX.png.

Usage : python vfx_textures.py
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../.."))
OUT = os.path.join(ROOT, "Assets/Art/VFX/Textures")
CTRL = os.path.join(ROOT, "docs/art/rendus/VFX")
os.makedirs(OUT, exist_ok=True)
os.makedirs(CTRL, exist_ok=True)
RNG = np.random.default_rng(7)


# ------------------------------------------------------------------ bruit
def _lattice(n, seed):
    r = np.random.default_rng(seed)
    return r.random((n + 1, n + 1))


def value_noise(h, w, cells, seed, ox=0.0, oy=0.0, tile_x=False):
    g = _lattice(cells + 2, seed)
    if tile_x:
        g[:, cells] = g[:, 0]
    y = (np.arange(h)[:, None] / h * cells + oy) % cells
    x = (np.arange(w)[None, :] / w * cells + ox) % cells
    yi, xi = np.floor(y).astype(int), np.floor(x).astype(int)
    fy, fx = y - yi, x - xi
    fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)
    xi1 = (xi + 1) % cells if tile_x else xi + 1
    a, b = g[yi, xi], g[yi, xi1]
    c, d = g[yi + 1, xi], g[yi + 1, xi1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def fbm(h, w, cells, seed, oct=4, ox=0.0, oy=0.0, tile_x=False):
    s, a, t = 0.0, 0.5, 0.0
    for k in range(oct):
        s = s + a * value_noise(h, w, cells * 2 ** k, seed + k * 13, ox * 2 ** k, oy * 2 ** k, tile_x)
        t += a; a *= 0.5
    return s / t


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def box_blur(a, r):
    if r < 1:
        return a
    k = 2 * r + 1
    p = np.pad(a, r, mode="edge")
    c = np.cumsum(np.cumsum(p, 0), 1)
    c = np.pad(c, ((1, 0), (1, 0)))
    return (c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]) / (k * k)


# ------------------------------------------------------------------ dessin d'une forme : tons + encre
def peint(champ, seuils=(0.0, 0.35, 0.62), encre=2.0, grain=0.03, seed=0):
    """champ : intensité 0..1 (0 = dehors). Renvoie R (ton), A (couverture) : silhouette au seuil seuils[0], tons
    posés en aplats (0,33 / 0,66 / 1) aux seuils suivants, trait d'encre d'`encre` px sur le bord extérieur et léger
    trait entre les tons."""
    h, w = champ.shape
    n = (value_noise(h, w, max(4, w // 10), seed) - 0.5) * grain
    f = champ + n
    inside = f > max(seuils[0], 0.02)
    ton = np.where(f > seuils[2], 1.0, np.where(f > seuils[1], 0.66, 0.33))
    # encre : couronne de la silhouette (distance au bord approchée par un flou du masque)
    m = inside.astype(np.float64)
    bl = box_blur(m, max(1, int(round(encre))))
    ring = inside & (bl < 0.98)
    R = np.where(ring, 0.0, ton)
    # trait fin entre les tons (moitié de l'encre), seulement au bord des aplats clairs
    for s in seuils[1:]:
        e = np.abs(f - s) < 0.012
        R = np.where(e & inside & ~ring, np.minimum(R, 0.18), R)
    # couverture adoucie sur 1 px
    A = np.clip(box_blur(m, 1) * 1.6 - 0.3, 0, 1) * (box_blur(m, 1) > 0.05)
    A = np.maximum(A, m)
    return R, A


def save(name, R, G, A):
    img = np.stack([R, G, np.zeros_like(R), A], -1)
    Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(os.path.join(OUT, name + ".png"))
    return img


def planche(frames, n):
    s = frames[0].shape[0]
    out = np.zeros((s * n, s * n, frames[0].shape[2]))
    for i, f in enumerate(frames):
        r, c = divmod(i, n)
        out[r * s:(r + 1) * s, c * s:(c + 1) * s] = f
    return out


def grille(h, w):
    y, x = np.mgrid[0:h, 0:w].astype(np.float64)
    return (x + 0.5) / w * 2 - 1, 1 - (y + 0.5) / h * 2      # x, y dans [-1, 1], y vers le haut


# ------------------------------------------------------------------ flamme en boucle (torches, lanterne, âmes)
def flamme(S=128, N=16):
    frames = []
    for i in range(N):
        t = i / N
        x, y = grille(S, S)
        # langue de flamme : goutte qui s'effile vers le haut, ondule ; bruit qui monte (boucle : décalage d'une période)
        nz = fbm(S, S, 3, 11, oct=3, ox=0.0, oy=-t * 3.0)
        nz2 = fbm(S, S, 5, 29, oct=2, ox=t * 1.0, oy=-t * 5.0)
        yy = (y + 0.9) / 1.85                      # 0 en bas de la flamme, 1 en haut
        larg = 0.85 * np.clip(1 - yy, 0, 1) ** 0.8 * (0.6 + 0.4 * np.sin(np.pi * np.clip(yy * 1.4, 0, 1)))
        xo = x + 0.42 * (nz - 0.5) * yy * 2 + 0.08 * np.sin(2 * np.pi * (t * 2 + yy * 1.3))
        champ = np.clip(1 - np.abs(xo) / np.maximum(larg, 1e-3), 0, 1) * smooth(-0.02, 0.12, yy) * (yy < 1.05)
        champ = champ * (0.7 + 0.6 * nz2) - 0.25 * smooth(0.55, 1.0, yy) * (1 - nz2)
        champ = np.clip(champ, 0, 1)
        R, A = peint(champ, (0.06, 0.3, 0.6), encre=2.2, seed=i)
        G = fbm(S, S, 6, 101 + i, oct=3)
        frames.append(np.stack([R, G, np.zeros_like(R), A], -1))
    p = planche(frames, 4)
    return save("T_VFX_Flamme_4x4", p[..., 0], p[..., 1], p[..., 3])


# ------------------------------------------------------------------ fumée / bouffée (morts, impacts)
def bouffee(nom, S=128, N=16, lobes=6, seed=3, monte=0.25):
    frames = []
    r = np.random.default_rng(seed)
    cx, cy = r.uniform(-0.35, 0.35, lobes), r.uniform(-0.25, 0.3, lobes)
    rr = r.uniform(0.36, 0.55, lobes)
    for i in range(N):
        t = i / (N - 1)
        x, y = grille(S, S)
        g = 0.45 + 0.55 * (1 - (1 - t) ** 2)       # gonfle vite puis ralentit
        champ = np.zeros((S, S))
        for k in range(lobes):
            d = np.hypot(x - cx[k] * g * 1.5, y - (cy[k] * g * 1.2 + monte * t)) / (rr[k] * g)
            champ = np.maximum(champ, np.clip(1 - d, 0, 1))
        nz = fbm(S, S, 4, seed * 7 + 1, oct=3, oy=-t)
        champ = champ * (0.75 + 0.5 * nz)
        # se défait : le cœur clair disparaît d'abord, puis la silhouette se troue
        champ = champ - smooth(0.45, 1.0, t) * (0.55 * (1 - nz) + 0.15)
        champ = np.clip(champ, 0, 1) * (1.0 - 0.35 * t)
        R, A = peint(champ, (0.05, 0.28, 0.55 + 0.2 * t), encre=2.0, seed=i + seed)
        G = fbm(S, S, 6, 300 + i + seed, oct=3)
        frames.append(np.stack([R, G, np.zeros_like(R), A], -1))
    p = planche(frames, 4)
    return save(nom, p[..., 0], p[..., 1], p[..., 3])


# ------------------------------------------------------------------ éclat (tir, parade) : étoile qui se referme
def eclat(S=128, N=4, branches=8):
    frames = []
    for i in range(N):
        t = i / N
        x, y = grille(S, S)
        a = np.arctan2(y, x)
        d = np.hypot(x, y)
        rayon = (0.95 - 0.35 * t) * (0.45 + 0.55 * np.abs(np.cos(a * branches / 2)) ** (6 - 3 * t))
        rayon = rayon * (0.9 + 0.15 * np.cos(a * 3 + i))
        champ = np.clip(1 - d / np.maximum(rayon, 1e-3), 0, 1) ** (0.7 + 0.6 * t)
        R, A = peint(champ, (0.03, 0.25, 0.55), encre=2.0, seed=i)
        G = fbm(S, S, 6, 500 + i, oct=3)
        frames.append(np.stack([R, G, np.zeros_like(R), A], -1))
    p = planche(frames, 2)
    return save("T_VFX_Eclat_2x2", p[..., 0], p[..., 1], p[..., 3])


# ------------------------------------------------------------------ arc de lame (épée du koala, 2026-11-22)
def arc(S=256, N=4):
    """Croissant balayé de gauche à droite : la tête (à droite) est épaisse et claire sur son bord extérieur (le fil de la
    lame), la queue s'effile. Images : balayage à 45 %, 80 %, 100 %, puis amincie et trouée (elle se défait)."""
    frames = []
    for i in range(N):
        x, y = grille(S, S)
        cx, cy, R = 0.0, -0.75, 1.25
        d = np.hypot(x - cx, y - cy)
        a = np.arctan2(y - cy, x - cx)                     # angle : pi à gauche, 0 à droite
        a0, a1 = np.radians(158), np.radians(22)            # début (queue, gauche) et fin (tête, droite)
        balaye = [0.45, 0.8, 1.0, 1.0][i]
        tete = a0 + (a1 - a0) * balaye
        s_ = np.clip((a0 - a) / (a0 - a1), 0, 1) / max(balaye, 1e-3)    # 0 queue -> 1 tête
        dans = (a <= a0) & (a >= tete)
        ep = (0.05 + 0.32 * s_ ** 1.2) * (1.0 - 0.45 * (i == 3))
        # le fil (bord extérieur) est le plus clair : champ qui monte vers l'extérieur
        rr = (d - (R - ep)) / np.maximum(ep, 1e-3)            # 0 bord intérieur -> 1 bord extérieur
        champ = np.clip(1 - np.abs(rr - 0.62) / 0.62, 0, 1) * dans
        champ = champ * (0.55 + 0.6 * rr.clip(0, 1)) * smooth(0.0, 0.12, s_)
        # tête arrondie
        champ *= smooth(0.0, 0.03, (tete - a) * -1 + 0.03) if False else 1.0
        nz = fbm(S, S, 5, 700 + i, oct=3)
        if i == 3:
            champ = champ * (0.6 + 0.6 * nz) - 0.25 * (1 - nz)
        champ = np.clip(champ * 1.5, 0, 1)
        R_, A = peint(champ, (0.05, 0.32, 0.66), encre=2.5, seed=40 + i)
        G = fbm(S, S, 6, 720 + i, oct=3)
        frames.append(np.stack([R_, G, np.zeros_like(R_), A], -1))
    p = planche(frames, 2)
    return save("T_VFX_Arc_2x2", p[..., 0], p[..., 1], p[..., 3])


def simple(nom, champ, seuils=(0.04, 0.3, 0.6), encre=2.0, seed=0):
    R, A = peint(champ, seuils, encre=encre, seed=seed)
    G = fbm(champ.shape[0], champ.shape[1], 6, 900 + seed, oct=3)
    return save(nom, R, G, A)


def main():
    rep = {}
    rep["flamme"] = flamme()
    rep["fumee"] = bouffee("T_VFX_Fumee_4x4", seed=3)
    rep["poussiere"] = bouffee("T_VFX_Poussiere_4x4", lobes=9, seed=8, monte=0.05)
    rep["eclat"] = eclat()
    rep["arc"] = arc()
    # étincelle : losange étiré (la particule est étirée par la vitesse)
    x, y = grille(64, 64)
    rep["etincelle"] = simple("T_VFX_Etincelle", np.clip(1 - (np.abs(x) / 0.35 + np.abs(y) / 0.95), 0, 1) * 1.6, (0.04, 0.3, 0.55), encre=1.5, seed=1)
    # étoile à 5 branches (étourdissement)
    x, y = grille(128, 128)
    a = np.arctan2(x, y); d = np.hypot(x, y)
    r5 = 0.45 + 0.45 * np.abs(np.cos(a * 5 / 2)) ** 3
    rep["etoile"] = simple("T_VFX_Etoile", np.clip(1 - d / r5, 0, 1) * 1.5, (0.04, 0.3, 0.62), encre=3.0, seed=2)
    # anneau (onde de la parade)
    d = np.hypot(x, y)
    rep["anneau"] = simple("T_VFX_Anneau", np.clip(1 - np.abs(d - 0.78) / 0.14, 0, 1) * (0.85 + 0.3 * fbm(128, 128, 8, 77, 2)), (0.05, 0.35, 0.7), encre=2.0, seed=3)
    # trace de balle : bande effilée vers l'arrière (u = 0 à l'arrière, 1 à la balle)
    h, w = 32, 256
    yy = (np.arange(h)[:, None] + 0.5) / h * 2 - 1
    xx = (np.arange(w)[None, :] + 0.5) / w
    larg = 0.15 + 0.85 * xx ** 0.6
    rep["trace"] = simple("T_VFX_Trace", np.clip(1 - np.abs(yy) / larg, 0, 1) * smooth(0.0, 0.25, xx) * 1.4, (0.05, 0.35, 0.68), encre=1.5, seed=4)
    # lien (livre du hibou) : bande ondulée raccordable en x, deux brins qui s'enroulent
    h, w = 64, 256
    yy = (np.arange(h)[:, None] + 0.5) / h * 2 - 1
    u = (np.arange(w)[None, :] + 0.5) / w
    nz = fbm(h, w, 4, 55, oct=3, tile_x=True)
    b1 = np.clip(1 - np.abs(yy - 0.38 * np.sin(2 * np.pi * u * 2)) / 0.24, 0, 1)
    b2 = np.clip(1 - np.abs(yy + 0.38 * np.sin(2 * np.pi * (u * 2 + 0.25))) / 0.18, 0, 1)
    rep["lien"] = simple("T_VFX_Lien", np.maximum(b1, b2 * 0.85) * (0.8 + 0.4 * nz) * 1.3, (0.05, 0.32, 0.62), encre=2.0, seed=5)
    # planche de contrôle : tons sur une rampe de feu, encre noire, fond gris foncé
    ramp = np.array([[0.02, 0.01, 0.02], [0.55, 0.12, 0.05], [1.0, 0.45, 0.1], [1.0, 0.9, 0.55]])
    tiles = []
    for k, im in rep.items():
        R, A = im[..., 0], im[..., 3]
        idx = np.clip(np.round(R * 3), 0, 3).astype(int)
        col = ramp[idx]
        bg = np.array([0.16, 0.16, 0.19])
        c = col * A[..., None] + bg * (1 - A[..., None])
        hh, ww = c.shape[:2]
        sc = 256 / max(hh, ww)
        t = Image.fromarray((c * 255).astype(np.uint8)).resize((max(1, int(ww * sc)), max(1, int(hh * sc))), Image.NEAREST)
        canvas = Image.new("RGB", (264, 264), (40, 40, 46)); canvas.paste(t, (4, 4))
        tiles.append(canvas)
    W = Image.new("RGB", (264 * 6, 264 * 2), (20, 20, 24))
    for i, t in enumerate(tiles):
        W.paste(t, ((i % 6) * 264, (i // 6) * 264))
    W.save(os.path.join(CTRL, "Textures_VFX.png"))
    print({k: v.shape for k, v in rep.items()})


if __name__ == "__main__":
    main()
