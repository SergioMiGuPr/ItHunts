# -*- coding: utf-8 -*-
"""Raccord sans couture d'une texture carrée (lot 0 de la nuit 4, 2026-11-22).

Méthode : découpe de coût minimal (image quilting). Pour chaque axe, l'image est décalée d'une demi-taille (la couture
du bord passe au centre), puis une bande prise ailleurs dans l'image (continue, elle) est collée sur la couture. Ses deux
bords suivent le chemin où la bande et l'image se ressemblent le plus (programmation dynamique), avec un fondu de 3 px.
La position de la bande est cherchée pour minimiser l'écart : sur un motif régulier (tapisserie), elle tombe sur la
période du motif et le raccord est exact.

    python -I raccord.py entree.png sortie.png [bande_px=256] [recouvrement_px=64]
Mesure imprimée : écart moyen à la couture rapporté à l'écart moyen entre pixels voisins (1.0 = invisible).
"""
import sys
import numpy as np
from PIL import Image, ImageFilter


def mesure(a):
    a = a.astype(np.float32)
    inner = (np.abs(np.diff(a, axis=1)).mean() + np.abs(np.diff(a, axis=0)).mean()) / 2
    sx = np.abs(a[:, 0] - a[:, -1]).mean()
    sy = np.abs(a[0] - a[-1]).mean()
    return sx / inner, sy / inner


def coupe_min(E):
    """E (h, w) : chemin de coût minimal de gauche à droite, une ligne par colonne, pas de ±1. Rend y(x)."""
    h, w = E.shape
    C = E.copy()
    B = np.zeros((h, w), np.int8)
    for x in range(1, w):
        prev = C[:, x - 1]
        up = np.r_[np.inf, prev[:-1]]
        dn = np.r_[prev[1:], np.inf]
        st = np.stack([up, prev, dn])
        k = st.argmin(0)
        C[:, x] += st[k, np.arange(h)]
        B[:, x] = k - 1
    y = np.zeros(w, int)
    y[-1] = int(C[:, -1].argmin())
    for x in range(w - 1, 0, -1):
        y[x - 1] = y[x] + B[y[x], x]
    return y


def raccord_vertical(a, bande, ov):
    """Supprime la couture haut/bas. a : (H, W, C) float."""
    H, W, _ = a.shape
    J = np.roll(a, H // 2, axis=0)  # couture au milieu (ligne H//2)
    c = H // 2
    y0, y1 = c - bande // 2, c + bande // 2
    top = J[y0:y0 + ov]
    bot = J[y1 - ov:y1]
    # recherche de la bande donneuse dans l'image d'origine (continue), loin de la couture
    best, bs = None, 1e18
    for s in range(bande, H - 2 * bande, 4):
        D = a[s:s + bande]
        e = ((D[:ov] - top) ** 2).mean() + ((D[-ov:] - bot) ** 2).mean()
        if e < bs:
            bs, best = e, s
    D = a[best:best + bande].copy()
    mask = np.zeros((bande, W), np.float32)  # 1 = donneuse
    yt = coupe_min(((D[:ov] - top) ** 2).sum(2))
    yb = coupe_min(((D[-ov:] - bot) ** 2).sum(2))
    rows = np.arange(bande)[:, None]
    mask[(rows >= yt[None, :]) & (rows <= (bande - ov + yb)[None, :])] = 1.0
    m = Image.fromarray((mask * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3))
    m = np.asarray(m).astype(np.float32)[..., None] / 255.0
    out = J.copy()
    out[y0:y1] = J[y0:y1] * (1 - m) + D * m
    return out, best


def raccord(a, bande=256, ov=64):
    a, sy = raccord_vertical(a, bande, ov)
    b = np.transpose(a, (1, 0, 2))
    b, sx = raccord_vertical(b, bande, ov)
    return np.transpose(b, (1, 0, 2)), (sx, sy)


if __name__ == "__main__":
    src, dst = sys.argv[1], sys.argv[2]
    bande = int(sys.argv[3]) if len(sys.argv) > 3 else 256
    ov = int(sys.argv[4]) if len(sys.argv) > 4 else 64
    im = Image.open(src).convert("RGB")
    a = np.asarray(im).astype(np.float32)
    print("avant  couture x %.2f y %.2f" % mesure(a))
    o, pos = raccord(a, bande, ov)
    o = np.clip(o + 0.5, 0, 255).astype(np.uint8)
    print("apres  couture x %.2f y %.2f  (bandes donneuses %s)" % (mesure(o) + (pos,)))
    Image.fromarray(o).save(dst)
