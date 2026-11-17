import sys, json, numpy as np
from PIL import Image
from uilib import *
SRC = sys.argv[1]; OUT = sys.argv[2]; TC = json.loads(sys.argv[3]) if len(sys.argv) > 3 else {}
im = np.array(Image.open(SRC).convert("RGB")).astype(float)
xs = [285, 652, 1020, 1387]; ys = [130, 345, 550, 775]
ICONS = {"Fleche": (0, 0), "Joueur_Rouge": (0, 1), "Joueur_Vert": (0, 2), "Joueur_Orange": (0, 3), "Joueur_Bleu": (1, 0),
         "Flamme": (2, 0), "Coffre_Normal": (2, 1), "Coffre_Rare": (2, 2), "Coffre_Ouvert": (2, 3),
         "Porte_Ouverte": (3, 0), "Porte_Verrouillee": (3, 1), "Bougie": (3, 2), "Allie_Mort": (3, 3)}
OUTLINE = 4          # épaisseur du trait noir ajouté autour du cœur (px source)
res = {}
for name, (r, c) in ICONS.items():
    x, y = xs[c], ys[r]; H = 105
    cell = im[y-H:y+H, x-H:x+H]
    val = cell.max(-1)
    t = TC.get(name, 60)
    sat = (val - cell.min(-1)) / np.maximum(val, 1)
    if name == "Allie_Mort": raw = (val > 55) & (sat < 0.6)
    elif name == "Bougie": raw = (val > 215) | ((val > 30) & (sat < 0.74))
    else: raw = val > t
    core = keep_big(raw, 30)
    core = fill_holes(erode(dilate(core, 4), 4) | core)
    sil = dilate(core, 3 if name.startswith('Joueur') else OUTLINE)
    a = soft(sil, 0.8) * sil + soft(sil, 0.8) * (~sil)   # bord adouci (1 px)
    rgb = cell.copy()
    ring = sil & ~core
    rgb[ring] = np.minimum(rgb[ring], 18)                  # trait d'encre net, sans lueur colorée
    x0, y0, x1, y1 = bbox(a > 0.02, 2)
    rgb, a = rgb[y0:y1, x0:x1], a[y0:y1, x0:x1]
    # carré centré
    h, w = a.shape; S = max(h, w)
    R = np.zeros((S, S, 3)); A = np.zeros((S, S))
    oy, ox = (S - h) // 2, (S - w) // 2
    R[oy:oy+h, ox:ox+w] = rgb; A[oy:oy+h, ox:ox+w] = a
    save_rgba(R, A, "%s/%s_src.png" % (OUT, name))
    res[name] = S
print(res)
