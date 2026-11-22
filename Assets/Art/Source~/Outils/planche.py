# -*- coding: utf-8 -*-
"""Planches de contrôle (nuit 4) : lignes d'images côte à côte, titre et légendes en police du jeu (EB Garamond).

    python -I planche.py sortie.jpg "Titre" "colonne1|colonne2" largeur "légende 1|img_a|img_b" "légende 2|img_a|img_b" ...
Une image absente est remplacée par un cadre vide marqué « — ».
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
POL = os.path.join(ROOT, "Assets", "Art", "UI", "Polices", "EBGaramond")
FOND = (18, 17, 20)
ENCRE = (226, 214, 190)
GRIS = (150, 142, 128)


def police(taille, style="Regular"):
    return ImageFont.truetype(os.path.join(POL, "EBGaramond-%s.ttf" % style), taille)


def planche(sortie, titre, colonnes, largeur, lignes):
    """lignes : liste de (légende, [chemins])."""
    h = int(largeur * 9 / 16)
    nc = len(colonnes)
    marge, haut_titre, haut_col, haut_leg = 16, 56, 34, 30
    H = haut_titre + haut_col + len(lignes) * (h + haut_leg + 10) + marge
    W = marge * 2 + nc * largeur + (nc - 1) * 8
    o = Image.new("RGB", (W, H), FOND)
    d = ImageDraw.Draw(o)
    d.text((marge, 10), titre, font=police(30, "SemiBold"), fill=ENCRE)
    for j, c in enumerate(colonnes):
        d.text((marge + j * (largeur + 8), haut_titre), c, font=police(22, "Italic"), fill=GRIS)
    y = haut_titre + haut_col
    for leg, chemins in lignes:
        d.text((marge, y), leg, font=police(20), fill=ENCRE)
        y += haut_leg
        for j, ch in enumerate(chemins):
            x = marge + j * (largeur + 8)
            if ch and os.path.exists(ch):
                im = Image.open(ch).convert("RGB")
                r = min(largeur / im.width, h / im.height)
                im = im.resize((int(im.width * r), int(im.height * r)), Image.LANCZOS)
                o.paste(im, (x + (largeur - im.width) // 2, y + (h - im.height) // 2))
            else:
                d.rectangle((x, y, x + largeur, y + h), outline=GRIS)
                d.text((x + largeur // 2 - 6, y + h // 2 - 12), "—", font=police(24), fill=GRIS)
        y += h + 10
    o.save(sortie, quality=88)
    return sortie


if __name__ == "__main__":
    out, titre, cols, larg = sys.argv[1], sys.argv[2], sys.argv[3].split("|"), int(sys.argv[4])
    lignes = []
    for a in sys.argv[5:]:
        t = a.split("|")
        lignes.append((t[0], t[1:]))
    print(planche(out, titre, cols, larg, lignes))
