# -*- coding: utf-8 -*-
"""Textures neutres et raccordables des pièces d'ensemble (nuit 6, lot 1), peintes par calcul (bruit périodique par FFT,
aplats, traits d'encre), plus les découpes des images générées (feuillage, portraits, vitrail, tapisserie).
    python ens_textures.py <dossier des images générées>"""
import sys, os
import numpy as np
from PIL import Image, ImageFilter
R = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), *[".."] * 5))
OUT = os.path.join(R, "Assets", "Art", "Textures", "Ensembles")
os.makedirs(OUT, exist_ok=True)
rng = np.random.default_rng(6)
N = 1024


def bruit(fx, fy):
    """Bruit périodique N x N ; fx, fy : fréquences dominantes (cycles par tuile)."""
    F = np.fft.fft2(rng.standard_normal((N, N)))
    kx = np.fft.fftfreq(N, 1 / N)[None, :]
    ky = np.fft.fftfreq(N, 1 / N)[:, None]
    F *= np.exp(-((kx / fx) ** 2 + (ky / fy) ** 2) * 0.5)
    x = np.real(np.fft.ifft2(F))
    x -= x.mean()
    return x / (x.std() + 1e-9)


def paliers(x, seuils):
    return sum((x > s).astype(float) for s in seuils) / len(seuils)


def sauve(nom, rgb, h=None):
    a = np.clip(rgb, 0, 1)
    h = np.ones(a.shape[:2]) if h is None else h
    Image.fromarray((np.dstack([a, np.clip(h, 0, 1)]) * 255 + 0.5).astype(np.uint8)).save(os.path.join(OUT, f"T_Ens_{nom}_BaseColor.png"))


def mix(a, b, t):
    return np.array(a) * (1 - t[..., None]) + np.array(b) * t[..., None]


def ou(m, a, b):
    return np.where(m[..., None], a, b)


# --- bois d'un seul tenant : fil vertical
w, w2, w3 = bruit(10, 1.2), bruit(40, 2.0), bruit(120, 3.0)
t = paliers(w * 0.8 + w2 * 0.6, (-0.9, -0.2, 0.5, 1.1))
c = mix((0.40, 0.285, 0.185), (0.60, 0.45, 0.30), t)
fil = (np.abs(w * 1.4 + w2 * 0.9) % 0.9 < 0.085) & (w3 > -0.9)
c = ou(fil, c * 0.52, c)
c = ou(w3 > 1.75, c * 0.72, c)
sauve("Bois", c, 0.55 + 0.35 * t - 0.5 * fil)
# --- tissu : trame fine et usure
y, x = np.mgrid[0:N, 0:N]
tr = (((x // 4) + (y // 4)) % 2).astype(float)
us = paliers(bruit(5, 5) + 0.35 * bruit(30, 30), (-0.8, 0.1, 0.9))
pl = paliers(bruit(3, 1), (-0.4, 0.6))
c = mix((0.66, 0.64, 0.60), (0.86, 0.84, 0.80), us * 0.6 + pl * 0.4) * (0.94 + 0.06 * tr)[..., None]
c = ou(bruit(60, 60) > 2.35, c * 0.6, c)
sauve("Tissu", c, 0.5 + 0.4 * us)
# --- pierre d'un seul tenant : éclats de taille
t = paliers(bruit(6, 6) + 0.5 * bruit(22, 22), (-1.0, -0.3, 0.4, 1.1))
c = mix((0.485, 0.48, 0.49), (0.585, 0.58, 0.57), t)
tr = (np.abs(bruit(26, 9)) < 0.035) & (bruit(5, 5) > 0.5)
tr |= (np.abs(bruit(9, 26)) < 0.03) & (bruit(5, 5) > 0.9)
pts = bruit(90, 90) > 2.6
c = ou(tr, c * 0.5, ou(pts, c * 0.68, c))
sauve("Pierre", c, 0.5 + 0.35 * t - 0.4 * tr)
# --- métal martelé
t = paliers(bruit(14, 14) + 0.4 * bruit(50, 50), (-0.9, 0.0, 0.9))
c = mix((0.60, 0.60, 0.62), (0.73, 0.73, 0.73), t)
ray = (np.abs(bruit(70, 3)) < 0.02) & (bruit(6, 6) > 0.6)
tac = (bruit(8, 8) + 0.6 * bruit(45, 45)) > 1.6
c = ou(ray, c * 1.25, c)
c = ou(tac, c * 0.65, c)
sauve("Metal", c, 0.5 + 0.4 * t)
# --- papier
b = paliers(bruit(4, 4) + 0.5 * bruit(24, 24), (-0.7, 0.4, 1.3))
sauve("Papier", mix((0.88, 0.82, 0.68), (0.78, 0.69, 0.53), b), 1 - 0.4 * b)
# --- livres : 16 dos (colonnes de 64 px, nerfs et filets dorés), haut de la tuile = haut du livre
im = np.zeros((N, N, 3))
cols = [(0.36, 0.09, 0.09), (0.12, 0.17, 0.27), (0.30, 0.19, 0.10), (0.13, 0.22, 0.15), (0.26, 0.12, 0.20),
        (0.42, 0.28, 0.13), (0.20, 0.19, 0.20), (0.40, 0.35, 0.26)] * 2
g, g2 = bruit(24, 8), bruit(80, 40)
for k in range(16):
    x0, x1 = k * 64, (k + 1) * 64
    u = (np.arange(64) + 0.5) / 64
    omb = np.where(u < 0.1, 0.45, np.where(u > 0.9, 0.5, np.where(u < 0.28, 0.82, np.where(u > 0.62, 0.78, 1.0))))
    base = np.array(cols[k]) * (1.25 if k >= 8 else 1.0)
    cell = base[None, None, :] * omb[None, :, None] * (0.88 + 0.14 * paliers(g[:, x0:x1], (0.2,)) + 0.1 * (g2[:, x0:x1] > 1.3))[..., None]
    for yy in ([80, 230, 420, 620, 850] if k % 3 else [110, 160, 780, 900]):
        cell[yy:yy + 26] = cell[yy:yy + 26] * 1.25
        cell[yy + 26:yy + 34] *= 0.45
        if k % 2 == 0 or k % 5 == 0:
            cell[yy - 12:yy - 4] = np.array((0.68, 0.50, 0.22))[None, None, :] * omb[None, :, None]
    if k % 4 in (1, 2):
        cell[270:360, 12:52] = np.array((0.62, 0.45, 0.2)) * 0.9
        cell[285:345, 18:46] = base * 0.6
    im[:, x0:x1] = cell
sauve("Livres", im)

# ---------------------------------------------------------------- images générées
if len(sys.argv) > 1:
    S = sys.argv[1]
    # feuillage : clé magenta, bords décontaminés, atlas 2048
    a = np.asarray(Image.open(os.path.join(S, "feuillage.png")).convert("RGB")).astype(float) / 255
    mag = np.minimum(a[..., 0], a[..., 2]) - a[..., 1]
    al = np.clip(1 - (mag - 0.12) / 0.35, 0, 1)
    ex = np.clip(mag, 0, 1) * (mag > 0.04) * 0.9
    # les roses (rouge, bleu faible) ne sont pas touchées : min(r, b) - v y reste petit
    rgb = a.copy()
    rgb[..., 0] -= ex
    rgb[..., 2] -= ex
    rgb = np.clip(rgb, 0, 1)
    c = 4096 // 3
    for p in (c, 2 * c, c + 1, 2 * c + 1):      # traits de grille effacés
        al[p - 14:p + 14, :] = 0
        al[:, p - 14:p + 14] = 0
    for s in (slice(0, 12), slice(-12, None)):
        al[s, :] = 0
        al[:, s] = 0
    arr = np.asarray(Image.fromarray((np.dstack([rgb, al]) * 255).astype(np.uint8)).resize((2048, 2048), Image.LANCZOS)).astype(float) / 255
    # couleur des feuilles étendue sous l'alpha (pas de liseré au filtrage)
    pw = (arr[..., 3] > 0.6).astype(float)
    acc = arr[..., :3] * pw[..., None]

    def flou(m, rad):
        return np.asarray(Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(rad))).astype(float) / 255
    for rad in (4, 12, 40):
        bl = np.dstack([flou(acc[..., i], rad) for i in range(3)])
        bw = flou(pw, rad)
        m = (pw < 0.5) & (bw > 0.01)
        acc[m] = bl[m] / bw[m][..., None]
        pw[m] = 1
    acc[pw < 0.5] = (0.1, 0.13, 0.07)
    acc = acc * np.array((0.82, 0.86, 0.86))
    Image.fromarray((np.clip(np.dstack([acc, arr[..., 3]]), 0, 1) * 255).astype(np.uint8)).save(os.path.join(OUT, "T_Ens_Feuillage_BaseColor.png"))
    # portraits : atlas 4 x 2 (cellules de 512 x 640)
    p = Image.open(os.path.join(S, "portraits.png")).convert("RGB")
    W, H = p.size
    at = Image.new("RGB", (2048, 1280))
    for k in range(8):
        i, j = k % 4, k // 4
        cw, ch = W / 4, H / 2
        cel = p.crop((int(i * cw + 24), int(j * ch + 24), int((i + 1) * cw - 24), int((j + 1) * ch - 24)))
        w0, h0 = cel.size
        ww = h0 * 512 / 640
        at.paste(cel.crop((int((w0 - ww) / 2), 0, int((w0 + ww) / 2), h0)).resize((512, 640), Image.LANCZOS), (i * 512, j * 640))
    at.save(os.path.join(OUT, "T_Ens_Portraits_BaseColor.png"))
    Image.open(os.path.join(S, "tapisserie.png")).convert("RGB").resize((1536, 2048), Image.LANCZOS).save(os.path.join(OUT, "T_Ens_Tapisserie_BaseColor.png"))
    Image.open(os.path.join(S, "vitrail.png")).convert("RGB").resize((1024, 2048), Image.LANCZOS).save(os.path.join(OUT, "T_Ens_Vitrail_BaseColor.png"))
