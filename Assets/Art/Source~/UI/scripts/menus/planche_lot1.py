# -*- coding: utf-8 -*-
"""Planches de contrôle du campement (nuit 4, lot 1) à partir des captures Unity gelées des configurations A à D.

    python -I planche_lot1.py <dossier captures après> <dossier captures avant> <dossier de sortie>
- Campement_Places.jpg : chaque place en grand, la maquette à gauche puis les 4 personnages à cette place ;
- Campement_AvantApres.jpg : chaque personnage à chaque place, avant (nuit 3) et après ;
- Campement_Zoom3.jpg : zoom x3 sur la queue du caméléon (4 places) et sur les bords de chaque personnage (tête, mains, pieds).
Légendes en police du jeu (EB Garamond).
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", "..", ".."))
POL = os.path.join(ROOT, "Assets", "Art", "UI", "Polices", "EBGaramond")
MAQ = os.path.join(ROOT, "docs", "art", "ui", "maquettes", "ui_campement_v01.png")
FOND, ENCRE, GRIS = (18, 17, 20), (226, 214, 190), (150, 142, 128)
PERSOS = ["Renard", "Koala", "Cameleon", "Hibou"]
CONFIGS = {"A": [0, 3, 1, 2], "B": [3, 0, 2, 1], "C": [1, 2, 3, 0], "D": [2, 1, 0, 3]}
# rect de chaque place dans l'écran 1670 x 941 (corps, lanterne et siège)
PLACES = {1: (330, 290, 720, 700), 2: (600, 290, 990, 700), 3: (940, 270, 1330, 680), 4: (1240, 270, 1660, 680)}


def pol(t, s="Regular"):
    return ImageFont.truetype(os.path.join(POL, "EBGaramond-%s.ttf" % s), t)


def config_de(perso, place):
    i = PERSOS.index(perso)
    for c, occ in CONFIGS.items():
        if occ[place - 1] == i:
            return c


def places(apres, sortie, larg=300):
    rects = PLACES
    h = int(larg * 410 / 390)
    W, H = 16 + 5 * (larg + 6), 60 + 4 * (h + 34)
    o = Image.new("RGB", (W, H), FOND); d = ImageDraw.Draw(o)
    d.text((16, 10), "Campement : chaque place en grand (maquette à gauche, puis les 4 personnages)", font=pol(28, "SemiBold"), fill=ENCRE)
    maq = Image.open(MAQ).convert("RGB")
    y = 60
    for pl in range(1, 5):
        r = rects[pl]
        d.text((16, y), "Place %d" % pl, font=pol(20), fill=ENCRE)
        x = 16
        for k, src in enumerate(["maquette"] + PERSOS):
            im = maq if src == "maquette" else Image.open(os.path.join(apres, "cfg_%s.png" % config_de(src, pl))).convert("RGB")
            c = im.crop(r).resize((larg, h), Image.LANCZOS)
            o.paste(c, (x, y + 26))
            d.text((x + 4, y + 26 + h - 24), "Maquette" if src == "maquette" else src.replace("Cameleon", "Caméléon"), font=pol(18, "Italic"), fill=ENCRE)
            x += larg + 6
        y += h + 34
    o.save(sortie, quality=90)


def avant_apres(apres, avant, sortie, larg=230):
    h = int(larg * 410 / 390)
    W, H = 16 + 8 * (larg + 4) + 3 * 10, 90 + 4 * (h + 30)
    o = Image.new("RGB", (W, H), FOND); d = ImageDraw.Draw(o)
    d.text((16, 10), "Campement : avant (nuit 3, fondu terne) et après (nuit 4), chaque personnage à chaque place", font=pol(26, "SemiBold"), fill=ENCRE)
    for pl in range(1, 5):
        d.text((16 + (pl - 1) * (2 * (larg + 4) + 10), 52), "Place %d : avant | après" % pl, font=pol(19, "Italic"), fill=GRIS)
    y = 84
    for p in PERSOS:
        d.text((16, y), p.replace("Cameleon", "Caméléon"), font=pol(19), fill=ENCRE)
        x = 16
        for pl in range(1, 5):
            c = config_de(p, pl)
            for dos in (avant, apres):
                im = Image.open(os.path.join(dos, "cfg_%s.png" % c)).convert("RGB").crop(PLACES[pl]).resize((larg, h), Image.LANCZOS)
                o.paste(im, (x, y + 24)); x += larg + 4
            x += 10
        y += h + 30
    o.save(sortie, quality=90)


def zoom3(apres, sortie):
    """Zoom x3 (pixels de l'écran 1670 x 941) : queue du caméléon aux 4 places, puis tête, mains et pieds de chaque personnage."""
    import json
    cons = {}
    for ln in open(os.path.join(HERE, "construction.txt"), encoding="utf-8"):
        t = ln.split()
        if t and t[0] == "place":
            cons[(t[1], int(t[2]))] = [float(v) for v in t[3:]]
    tuiles = []
    for pl in range(1, 5):
        x, y, w, h = cons[("Cameleon", pl)][:4]
        tuiles.append(("Caméléon place %d : queue" % pl, config_de("Cameleon", pl), (int(x), int(y + h * 0.55), int(x + w * 0.45), int(y + h))))
    for p in PERSOS:
        x, y, w, h = cons[(p, 1)][:4]
        c = config_de(p, 1)
        n = p.replace("Cameleon", "Caméléon")
        tuiles.append(("%s : tête (bords)" % n, c, (int(x + w * 0.35), int(y), int(x + w * 0.85), int(y + h * 0.33))))
        tuiles.append(("%s : pieds et siège" % n, c, (int(x + w * 0.35), int(y + h * 0.62), int(x + w), int(y + h + 8))))
    cw = 420
    rows = (len(tuiles) + 2) // 3
    o = Image.new("RGB", (16 + 3 * (cw + 8), 60 + rows * (cw + 34)), FOND); d = ImageDraw.Draw(o)
    d.text((16, 10), "Campement : zoom x3 (queue du caméléon, bords de chaque personnage, contact avec le siège)", font=pol(26, "SemiBold"), fill=ENCRE)
    for k, (leg, c, r) in enumerate(tuiles):
        im = Image.open(os.path.join(apres, "cfg_%s.png" % c)).convert("RGB").crop(r)
        im = im.resize((im.width * 3, im.height * 3), Image.NEAREST)
        im.thumbnail((cw, cw))
        x, y = 16 + (k % 3) * (cw + 8), 60 + (k // 3) * (cw + 34)
        d.text((x, y), leg, font=pol(18), fill=ENCRE)
        o.paste(im, (x, y + 26))
    o.save(sortie, quality=92)


if __name__ == "__main__":
    apres, avant, out = sys.argv[1], sys.argv[2], sys.argv[3]
    os.makedirs(out, exist_ok=True)
    places(apres, os.path.join(out, "Campement_Places.jpg"))
    avant_apres(apres, avant, os.path.join(out, "Campement_AvantApres.jpg"))
    zoom3(apres, os.path.join(out, "Campement_Zoom3.jpg"))
    print("ok")
