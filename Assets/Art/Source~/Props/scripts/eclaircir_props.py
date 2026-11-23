# -*- coding: utf-8 -*-
"""Lot 4 (nuit 4) : textures des props éclaircies pour la lecture en jeu (scènes très sombres, shader toon) ;
(la lueur dorée des coffres passe par le rim du shader toon, réglé dans le matériau : l'atlas de Tripo ne sépare pas
proprement le laiton du bois clair, un masque d'émission ferait briller les planches).
- couleur : courbe gamma 0.8 puis gain 1.08 (les ombres peintes par Tripo s'ouvrent, les teintes restent) ;
Les textures d'origine restent dans l'historique git (commit 28faa57).
    python -I eclaircir_props.py
"""
import os
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
TEX = os.path.join(ROOT, "Assets", "Art", "Textures", "Props")
NOMS = ["Tonneau", "Chaise", "Poutre", "Sarcophage", "Ratelier", "Coffre", "CoffreVerrou", "Statue"]

for n in NOMS:
    p = os.path.join(TEX, "T_Prop_%s_BaseColor.png" % n)
    im = Image.open(p)
    a = np.asarray(im.convert("RGBA")).astype(np.float32) / 255
    rgb = np.clip(a[..., :3] ** 0.8 * 1.08, 0, 1)
    out = np.concatenate([rgb, a[..., 3:]], -1)
    Image.fromarray((out * 255 + 0.5).astype(np.uint8), "RGBA").convert(im.mode).save(p)
    L = (0.2126 * rgb[..., 0] + 0.7152 * rgb[..., 1] + 0.0722 * rgb[..., 2])
    print(n, "luminance moyenne %.3f" % L.mean())
