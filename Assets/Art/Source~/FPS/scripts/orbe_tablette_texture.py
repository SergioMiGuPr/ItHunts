"""Atlas des tablettes de l'orbe : 6 glyphes (3 colonnes x 2 lignes), une case par glyphe = une face de tablette.

- T_Orbe_Tablette_BaseColor.png : pierre de l'orbe (T_Orbe_Pierre_BaseColor, même densité), glyphe GRAVÉ (sillon sombre,
  bord ébréché clair en haut à gauche, ombre en bas à droite) ;
- T_Orbe_Tablette_Emission.png : masque d'émission (blanc dans le sillon, halo doux autour) : le jeu écrit la couleur du
  personnage dans _EmissionColor, le masque la limite à la rune et à sa lueur.

Case k (0 à 5) : colonne k % 3, ligne k // 3 (ligne 0 en haut de l'image). UV de la case : u de (k % 3) / 3 à (k % 3 + 1) / 3,
v de 1 - (k // 3 + 1) / 2 à 1 - (k // 3) / 2. Face de 0.035 m (u) x 0.05 m (v).
Usage : python orbe_tablette_texture.py
"""
import os
import sys

import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from orbe_glyphes import GLYPHES  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
TEX = os.path.join(ROOT, "Assets", "Art", "Textures", "FPS")
CW, CH = 256, 384                     # case : 0.035 x 0.05 m
FACE_W, FACE_H = 0.035, 0.05
GX, GY = 0.70, 0.78                   # emprise du glyphe sur la face (comme sm_orbe.py)
TRAIT = 0.0022                        # largeur du trait (m)


def seg_dist(px, py, a, b):
    ax, ay = a; bx, by = b
    dx, dy = bx - ax, by - ay
    L2 = max(dx * dx + dy * dy, 1e-9)
    t = np.clip(((px - ax) * dx + (py - ay) * dy) / L2, 0, 1)
    return np.hypot(px - (ax + t * dx), py - (ay + t * dy))


def blur(a, s):
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]; fx = np.fft.fftfreq(w)[None, :]
    return np.real(np.fft.ifft2(np.fft.fft2(a) * np.exp(-2 * (np.pi * s) ** 2 * (fx ** 2 + fy ** 2))))


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def cell(k, stone):
    ys, xs = np.mgrid[0:CH, 0:CW].astype(np.float64)
    # coordonnées de la face en mètres, origine au centre, y vers le haut
    fx = (xs + 0.5) / CW * FACE_W - FACE_W / 2
    fy = FACE_H / 2 - (ys + 0.5) / CH * FACE_H
    sx, sy = FACE_W / 2 * GX, FACE_H / 2 * GY
    d = np.full(fx.shape, 1e9)
    for poly in GLYPHES[k]:
        for a, b in zip(poly[:-1], poly[1:]):
            d = np.minimum(d, seg_dist(fx, fy, (a[0] * sx, a[1] * sy), (b[0] * sx, b[1] * sy)))
    px_m = FACE_W / CW
    half = TRAIT / 2
    groove = 1 - smooth(half - 1.2 * px_m, half + 0.4 * px_m, d)           # 1 dans le sillon
    # bord ébréché : bruit sur la largeur
    rng = np.random.default_rng(100 + k)
    jitter = blur(rng.standard_normal((CH, CW)), 2.0)
    jitter = jitter / max(1e-6, np.abs(jitter).max())
    groove = 1 - smooth(half * (0.85 + 0.25 * jitter) - 1.2 * px_m, half * (0.85 + 0.25 * jitter) + 0.4 * px_m, d)
    # relief : lumière en haut à gauche -> bord clair en haut à gauche du sillon, ombre portée en bas à droite
    sh = np.roll(np.roll(groove, -3, 0), -3, 1)       # sillon décalé vers le haut-gauche
    lip_light = np.clip(sh - groove, 0, 1)            # bord bas-droite du sillon vu décalé : arête éclairée en face
    sh2 = np.roll(np.roll(groove, 3, 0), 3, 1)
    lip_dark = np.clip(sh2 - groove, 0, 1)
    col = stone.copy()
    deep = np.array([0.085, 0.07, 0.07])
    inner = smooth(0.2, 1.0, groove)
    col = col * (1 - inner[..., None]) + deep * inner[..., None]
    col = col * (1 - 0.35 * lip_dark[..., None]) + np.array([0.80, 0.76, 0.70]) * 0.35 * lip_light[..., None] + col * 0.0
    # usure : petites taches claires dans le sillon (poussière de pierre)
    dust = smooth(0.6, 0.9, blur(rng.random((CH, CW)), 1.5) * 2) * groove * 0.25
    col = np.clip(col + dust[..., None] * 0.3, 0, 1)
    # émission : sillon plein, halo gaussien doux autour
    halo = blur(groove, 5.0)
    halo = halo / max(1e-6, halo.max())
    em = np.clip(groove * 1.0 + halo * 0.45, 0, 1)
    return col, em


def main():
    tile = np.asarray(Image.open(os.path.join(TEX, "T_Orbe_Pierre_BaseColor.png")).convert("RGB"), dtype=np.float64) / 255
    # pierre à la même densité que la tuile (256 px pour 0.05 m) : la case (0.035 x 0.05 m) en lit 179 x 256 px, agrandis
    th, tw = tile.shape[:2]
    big = np.tile(tile, (3, 3, 1))
    base = np.zeros((CH * 2, CW * 3, 3)); emi = np.zeros((CH * 2, CW * 3))
    for k in range(6):
        c, r = k % 3, k // 3
        oy, ox = (k * 37) % th, (k * 61) % tw                   # un morceau de pierre différent par case
        need_h, need_w = int(round(FACE_H / 0.05 * th)), int(round(FACE_W / 0.05 * tw))
        crop = big[oy:oy + need_h, ox:ox + need_w]
        stone = np.asarray(Image.fromarray((crop * 255).astype(np.uint8)).resize((CW, CH), Image.LANCZOS), dtype=np.float64) / 255
        col, em = cell(k, stone)
        base[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = col
        emi[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = em
    # 768 x 768 (puissance de deux pour la compression) : cases de 256 x 384
    Image.fromarray((np.clip(base, 0, 1) * 255).astype(np.uint8)).save(os.path.join(TEX, "T_Orbe_Tablette_BaseColor.png"))
    Image.fromarray((np.clip(emi, 0, 1) * 255).astype(np.uint8)).convert("RGB").save(os.path.join(TEX, "T_Orbe_Tablette_Emission.png"))
    print("ok", base.shape)


if __name__ == "__main__":
    main()
