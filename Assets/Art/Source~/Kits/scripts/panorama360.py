# -*- coding: utf-8 -*-
"""Bande de 360° sans couture pour les fenêtres de l'acte II (nuit 5, lot 3, relecture) : le panorama peint (150°) garde la
seule lune ; il est suivi de deux copies en miroir sans lune (raccords continus par construction), et la fin rejoint le
début par un fondu de 600 px. Sortie : Assets/Art/Textures/Ciel/T_Vue_Nuit_360.png (8192 × 1664).
    python -I panorama360.py <panorama recadré de 150°> (source : docs/art/textures/sources/vue_nuit_panorama_v01.png, recadrée de x 420 à 5925 et ramenée à 4096 de large)"""
import sys, os
import numpy as np
from PIL import Image
ICI = os.path.dirname(os.path.abspath(__file__))
R = os.path.abspath(os.path.join(ICI, "..", "..", "..", "..", ".."))
src = Image.open(sys.argv[1]).convert("RGB")
W, H = 8192, 1664
w150 = int(round(W * 150 / 360))
P = np.asarray(src.resize((w150, H), Image.LANCZOS)).astype(np.float32)
# lune : disque le plus clair du tiers haut ; remplacée par du ciel pris à côté, masque doux
lum = P[: H // 2].mean(-1)
# centre : maximum de la luminance floutée (boîte de 61 px, deux passes) ; rayon du disque et de son halo mesurés : 320 px
k = 61
for _ in range(2):
    c = np.cumsum(np.pad(lum, ((0, 0), (k // 2 + 1, k // 2))), axis=1); lum = (c[:, k:] - c[:, :-k]) / k
    c = np.cumsum(np.pad(lum, ((k // 2 + 1, k // 2), (0, 0))), axis=0); lum = (c[k:] - c[:-k]) / k
cy, cx = np.unravel_index(np.argmax(lum), lum.shape)
rayon = 320
# copies sans lune : disque et halo recouverts par du ciel étoilé pris 700 px à droite (masque doux, au-dessus des collines)
Y, X = np.mgrid[0:H, 0:w150]
d = np.sqrt((Y - cy) ** 2 + (X - cx) ** 2)
m = np.clip((560 - d) / 200.0, 0, 1) * np.clip((0.4 * H - Y) / (0.06 * H), 0, 1)
src = np.roll(P, -700, axis=1)
Q = P * (1 - m[..., None]) + src * m[..., None]
pieces = [P, Q[:, ::-1], Q]
bande = np.concatenate(pieces, axis=1)[:, : W + 600]
# fondu de la fin vers le début (600 px)
f = np.linspace(0, 1, 600)[None, :, None]

out = bande[:, :W].copy()
queue = bande[:, W - 600:W]
tete = bande[:, :600]
out[:, W - 600:W] = queue * (1 - f) + tete * f
Image.fromarray(np.clip(out, 0, 255).astype(np.uint8)).save(os.path.join(R, "Assets", "Art", "Textures", "Ciel", "T_Vue_Nuit_360.png"))
print("lune", cx, cy, rayon, "bande", out.shape)
