# -*- coding: utf-8 -*-
"""Lot 5 de la nuit 4 : découpe des grilles 2 x 2 générées (docs/art/ameliorations/sources/grille_NN.png, 4096 px) en 70
illustrations d'améliorations, une par fichier, à trois tailles.

Découpe : quatre quarts égaux rognés de 2,5 % (gouttières et bord), recadrés au carré. Sorties :
  Assets/Art/UI/Ameliorations/Icones/ICO_<id>.png      512 px (marchand, écran de fin)
  docs/art/ameliorations/planche_64.png                planche de lisibilité à 64 px (HUD)
Ordre des cases : haut gauche, haut droite, bas gauche, bas droite ; grille N -> améliorations 4N à 4N + 3 de icones.ICONES.
La grille 17 contient H7, H8, puis la version corrigée de C8 (langue de caméléon) et une variante de G7 (non retenue,
laissée dans les sources). Les grilles 20 et 21 refont G13, G15, K4, G23, K-P1, H1, G17 et R3 (doublons ou sujets
faux relevés en relecture) ; leurs anciennes versions sont remplacées.

    python -I decoupe_icones.py
"""
import os, sys, json
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", "..", ".."))
SRC = os.path.join(ROOT, "docs", "art", "ameliorations", "sources")
OUT = os.path.join(ROOT, "Assets", "Art", "UI", "Ameliorations", "Icones")
sys.path.insert(0, HERE)
from liste_icones import ICONES


def cases(img):
    """Quatre quarts égaux ; chaque quart est rogné de 4 % de chaque côté (gouttières noires ou blanches d'environ 1 % et
    bord de l'image), puis recadré au carré sur son centre."""
    W, H = img.size
    out = []
    for (l, t) in ((0, 0), (W // 2, 0), (0, H // 2), (W // 2, H // 2)):
        w, h = W // 2, H // 2
        m = int(min(w, h) * 0.04)      # 4 % : plus aucun reste de gouttière (relecture)
        s = min(w, h) - 2 * m
        cx, cy = l + w // 2, t + h // 2
        c = img.crop((cx - s // 2, cy - s // 2, cx + s // 2, cy + s // 2))
        # gouttière claire résiduelle (certaines grilles ont des gouttières blanches) : on rogne tant qu'un bord est clair
        a = np.asarray(c.convert("L")).astype(np.float32)
        k = 0
        while k < s // 10 and max(a[k].mean(), a[-1 - k].mean(), a[:, k].mean(), a[:, -1 - k].mean()) > 120:
            k += 1
        if k:
            k += 4
            c = c.crop((k, k, s - k, s - k))
        out.append(c)
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    noms = [ic[0] for ic in ICONES]
    affect = {}
    REMPL = {20: ["G13", "G15", "K4", "G23"], 21: ["K-P1", "H1", "G17", "R3"]}   # refaites après la relecture
    for g in list(range(18)) + sorted(REMPL):
        f = os.path.join(SRC, "grille_%02d.png" % g)
        if not os.path.exists(f):
            print("manque", f); continue
        cs = cases(Image.open(f).convert("RGB"))
        if g < 17:
            ids = noms[4 * g:4 * g + 4]
        elif g == 17:
            ids = ["H7", "H8", "C8", "G7_variante"]
        else:
            ids = REMPL[g]
        for k, (i, c) in enumerate(zip(ids, cs)):
            affect[i] = "grille_%02d case %d" % (g, k)
            dest = OUT if i != "G7_variante" else SRC          # la variante non retenue reste dans les sources
            c.resize((512, 512), Image.LANCZOS).save(os.path.join(dest, "ICO_%s.png" % i.replace("-", "_")))
    json.dump(affect, open(os.path.join(SRC, "..", "affectation.json"), "w", encoding="utf-8"), indent=1)
    # planche de lisibilité à 64 px (taille du HUD)
    ids = [ic[0] for ic in ICONES]
    o = Image.new("RGB", (10 * 72 + 8, 7 * 72 + 8), (18, 17, 20))
    for k, i in enumerate(ids):
        p = os.path.join(OUT, "ICO_%s.png" % i.replace("-", "_"))
        if os.path.exists(p):
            o.paste(Image.open(p).resize((64, 64), Image.LANCZOS), (8 + (k % 10) * 72, 8 + (k // 10) * 72))
    o.save(os.path.join(SRC, "..", "planche_64.png"))
    print(len(affect), "icônes")


if __name__ == "__main__":
    main()
