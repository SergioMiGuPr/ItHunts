# -*- coding: utf-8 -*-
"""Lune du ciel des actes (nuit 4, lot 3) : la lune peinte du menu principal (Titre_Fond.png), découpée en disque.
Les flèches du château qui la masquent sont remplacées par le disque tourné (90°, 270°, 180°) là où il est propre ;
bord adouci, fond transparent. Sortie : Assets/Art/Textures/Ciel/T_Ciel_Lune.png (256 px, RGBA).
    python -I lune_menu.py
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", ".."))
im = np.asarray(Image.open(os.path.join(ROOT, "Assets", "Art", "UI", "Menus", "Titre", "Titre_Fond.png")).convert("RGB")).astype(np.float32)
# centre et rayon : disque clair autour du point le plus lumineux du haut de l'image
L = im.mean(-1)
zone = L[:300, 900:1300]
cy, cx = np.unravel_index(np.argmax(np.where(zone > 200, 1, 0) * 0 + zone), zone.shape)
ys, xs = np.nonzero(zone > 150)
cx, cy = xs.mean() + 900, ys.mean()
R = np.sqrt((zone > 150).sum() / np.pi)
R = max(R, 40) * 1.04
print("centre %.0f %.0f rayon %.1f" % (cx, cy, R))
n = int(R * 2 + 8)
x0, y0 = int(cx - n / 2), int(cy - n / 2)
crop = im[y0:y0 + n, x0:x0 + n].copy()
yy, xx = np.mgrid[0:n, 0:n] - (n - 1) / 2
r = np.sqrt(xx ** 2 + yy ** 2) / R
lum = crop.mean(-1)
med = np.median(lum[r < 0.9])
# flèches et tour du château (sombres, ou rouges), un peu élargies : remplies par le disque tourné de 90°, 270° ou 180° autour
# du centre là où la rotation tombe sur du ciel lunaire propre (la lune du menu n'a pas de face cachée à respecter)
sat = crop.max(-1) - crop.min(-1)
masque = (r < 1.0) & ((lum < med * 0.72) | ((crop[..., 0] > crop[..., 2]) & (sat > 25)))
from PIL import ImageFilter
masque = np.asarray(Image.fromarray((masque * 255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))) > 0
for k in (1, 3, 2):
    rot = np.rot90(crop, k); mrot = np.rot90(masque, k)
    pris = masque & ~mrot
    crop[pris] = rot[pris]; masque = masque & ~pris
crop[masque] = np.median(crop[(r < 0.9)], axis=0)
crop = np.asarray(Image.fromarray(crop.astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.6))).astype(np.float32)
alpha = np.clip((1.0 - r) / 0.04, 0, 1)
out = np.dstack([crop, alpha * 255]).astype(np.uint8)
Image.fromarray(out, "RGBA").resize((256, 256), Image.LANCZOS).save(os.path.join(ROOT, "Assets", "Art", "Textures", "Ciel", "T_Ciel_Lune.png"))
print("pixels restés sans source (couleur médiane) : %d" % masque.sum())
