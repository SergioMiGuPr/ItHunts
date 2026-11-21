# -*- coding: utf-8 -*-
"""Interface en jeu (lot C, 2026-11-21) : pièces découpées dans les maquettes de l'utilisateur, aucun nouveau dessin.
- Reticule_Etoile : l'étoile à 4 pointes du cadre de la mini-carte (pointe nord de MiniCarte_Cadre, déjà détourée avec son
  trait d'encre), isolée par un losange qui suit ses pointes (l'anneau est coupé au ras des pointes latérales).
- Charge_Pleine, Charge_Vide : l'orbe bleue et l'orbe fêlée grise de ui_icones_minicarte_v01 (même découpe que les icônes
  de la mini-carte : cœur au seuil, trait d'encre de 4 px), en carré ; la lueur est ajoutée par l'interface (Menus_Lueur).
- Parade_Icone : le losange cerclé de ui_icones_minicarte_v01.
Sorties : Assets/Art/UI/Jeu/*.png (+ ui_jeu.json : tailles et centres).

Usage : python ui_jeu.py
"""
import json, os, sys
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from uilib import *

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../.."))
OUT = os.path.join(ROOT, "Assets/Art/UI/Jeu")
os.makedirs(OUT, exist_ok=True)
rep = {}

# ---- réticule : étoile du cadre de la mini-carte
cad = np.asarray(Image.open(os.path.join(ROOT, "Assets/Art/UI/MiniCarte/MiniCarte_Cadre.png")).convert("RGBA")).astype(float)
cx, cy, hw, hh = 280.0, 67.5, 57.0, 66.0          # centre et demi-axes du losange (pointes de l'étoile)
yy, xx = np.mgrid[0:cad.shape[0], 0:cad.shape[1]].astype(float)
d = np.abs(xx - cx) / hw + np.abs(yy - cy) / hh
m = np.clip((1.0 - d) / 0.035, 0, 1)              # bord du losange adouci (~2 px)
A = cad[..., 3] / 255.0 * m
x0, y0, x1, y1 = bbox(A > 0.02, 2)
rgb, a = cad[y0:y1, x0:x1, :3], A[y0:y1, x0:x1]
h, w = a.shape; S = max(h, w)
R = np.zeros((S, S, 3)); AA = np.zeros((S, S))
oy, ox = (S - h) // 2, (S - w) // 2
R[oy:oy + h, ox:ox + w] = rgb; AA[oy:oy + h, ox:ox + w] = a
save_rgba(R, AA, os.path.join(OUT, "Reticule_Etoile.png"))
rep["Reticule_Etoile"] = {"taille": S, "source": "MiniCarte_Cadre (pointe nord)"}

# ---- orbes et losange : même découpe que icones.py
im = np.array(Image.open(os.path.join(ROOT, "docs/art/ui/maquettes/ui_icones_minicarte_v01.png")).convert("RGB")).astype(float)
xs = [285, 652, 1020, 1387]; ys = [130, 345, 550, 775]
PIECES = {"Charge_Pleine": (1, 0, 60), "Charge_Vide": (1, 1, 60), "Parade_Icone": (1, 3, 70)}
for name, (r, c, t) in PIECES.items():
    x, y = xs[c], ys[r]; H = 105
    cell = im[y - H:y + H, x - H:x + H]
    val = cell.max(-1)
    core = keep_big(val > t, 30)
    core = fill_holes(erode(dilate(core, 4), 4) | core)
    sil = dilate(core, 4)
    a = soft(sil, 0.8) * sil + soft(sil, 0.8) * (~sil)
    rgb = cell.copy()
    ring = sil & ~core
    rgb[ring] = np.minimum(rgb[ring], 18)
    x0, y0, x1, y1 = bbox(a > 0.02, 2)
    rgb, a = rgb[y0:y1, x0:x1], a[y0:y1, x0:x1]
    h, w = a.shape; S = max(h, w)
    R = np.zeros((S, S, 3)); AA = np.zeros((S, S))
    oy, ox = (S - h) // 2, (S - w) // 2
    R[oy:oy + h, ox:ox + w] = rgb; AA[oy:oy + h, ox:ox + w] = a
    save_rgba(R, AA, os.path.join(OUT, name + ".png"))
    rep[name] = {"taille": S, "source": "ui_icones_minicarte_v01 (ligne %d, colonne %d)" % (r + 1, c + 1)}
json.dump(rep, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "ui_jeu.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print(rep)
