"""Planche de contrôle du lot B : chaque découpe sur damier et sur fond sombre (docs/art/rendus/UI/LotB_decoupes.jpg)."""
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".."))
M = os.path.join(ROOT, "Assets", "Art", "UI", "Menus")
OUTP = os.path.join(ROOT, "docs", "art", "rendus", "UI", "LotB_decoupes.jpg")
F = lambda s: ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", s)
FB = lambda s: ImageFont.truetype("C:/Windows/Fonts/segoeuib.ttf", s)

# (fichier, mode) : alpha = mélange normal ; add = additif sur fond sombre ; masque = niveaux de gris
ITEMS = [("Fond/Fond_Ciel.png", "opaque"), ("Fond/Fond_Lune.png", "alpha"), ("Fond/Fond_Lune_Halo.png", "add"),
         ("Fond/Fond_Nuages_1.png", "alpha"), ("Fond/Fond_Nuages_3.png", "alpha"), ("Fond/Fond_Nuages_5.png", "alpha"),
         ("Fond/Fond_Brume_1.png", "add"), ("Fond/Fond_Brume_2.png", "add"),
         ("Fond/Fond_Chateau.png", "alpha"), ("Fond/Fond_Chateau_Fenetres.png", "masque"),
         ("Campement/Campement_Feu.png", "alpha"), ("Campement/Campement_Feu_Additif.png", "add"),
         ("Campement/Campement_Siege_Tronc.png", "alpha"), ("Campement/Campement_Siege_Pierre.png", "alpha"),
         ("Campement/Campement_Siege_Banc.png", "alpha"), ("Campement/Campement_Siege_Rocher.png", "alpha"),
         ("Campement/Campement_Fond.png", "opaque")]
CELL = (440, 250)
DARK = (22, 24, 40)


def checker(w, h, s=16):
    y, x = np.mgrid[0:h, 0:w]
    c = ((x // s + y // s) % 2) * 50 + 150
    return Image.fromarray(np.stack([c, c, c], -1).astype(np.uint8))


def fit(im):
    k = min(CELL[0] / im.width, CELL[1] / im.height)
    return im.resize((max(1, int(im.width * k)), max(1, int(im.height * k))), Image.LANCZOS)


def tile(im, mode, bg):
    base = checker(*CELL) if bg == "damier" else Image.new("RGB", CELL, DARK)
    if mode == "add":
        base = Image.new("RGB", CELL, DARK if bg != "damier" else (60, 60, 60))
        s = fit(im.convert("RGB"))
        ox, oy = (CELL[0] - s.width) // 2, (CELL[1] - s.height) // 2
        a = np.asarray(base).astype(int)
        a[oy:oy + s.height, ox:ox + s.width] += np.asarray(s).astype(int)
        return Image.fromarray(np.clip(a, 0, 255).astype(np.uint8))
    s = fit(im.convert("RGBA") if mode != "masque" else im.convert("RGB").convert("RGBA"))
    ox, oy = (CELL[0] - s.width) // 2, (CELL[1] - s.height) // 2
    b = base.convert("RGBA")
    b.alpha_composite(s, (ox, oy))
    return b.convert("RGB")


def run():
    cols = 2
    rowh = CELL[1] + 40
    W = 30 + 2 * cols * (CELL[0] + 10)
    n = (len(ITEMS) + cols - 1) // cols
    c = Image.new("RGB", (W, 100 + n * rowh), (14, 12, 18))
    d = ImageDraw.Draw(c)
    d.text((15, 12), "Lot B : découpes (sur damier | sur fond sombre)", font=FB(30), fill=(240, 170, 90))
    d.text((15, 54), "alpha = mélange normal ; additif = noir pur, Blend One One (montré ajouté au fond) ; masque = lueur des fenêtres",
           font=F(18), fill=(200, 200, 210))
    for i, (f, mode) in enumerate(ITEMS):
        im = Image.open(os.path.join(M, f))
        x = 15 + (i % cols) * 2 * (CELL[0] + 10)
        y = 100 + (i // cols) * rowh
        d.text((x, y), "%s  (%s, %d × %d)" % (os.path.basename(f), mode, im.width, im.height), font=F(17), fill=(230, 230, 235))
        c.paste(tile(im, mode, "damier"), (x, y + 26))
        c.paste(tile(im, mode, "sombre"), (x + CELL[0] + 10, y + 26))
    os.makedirs(os.path.dirname(OUTP), exist_ok=True)
    c.save(OUTP, quality=86)
    print(OUTP, c.size)


if __name__ == "__main__":
    run()
