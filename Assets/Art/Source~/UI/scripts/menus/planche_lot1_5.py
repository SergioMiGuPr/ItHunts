# -*- coding: utf-8 -*-
"""Planches de contrôle du lot 1 de la nuit 5 (campement : places vides, 4 personnages à chaque place, coop).

    python -I planche_lot1_5.py <dossier des captures> <sortie>

Le dossier contient avant/ (prefab de main avant le lot) et v2/ (après), captures 1920 × 1080 du vrai menu en Play Mode
(MainMenu.unity), nommées j1_<perso>, rot_A..D, coop2, coop3, coop_p2vide, coop2_prets, coop4_prets, app_p*_40.
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.abspath(os.path.join(ICI, "..", "..", "..", "..", "..", ".."))
POL = os.path.join(RACINE, "Assets", "Art", "UI", "Polices", "EBGaramond")
MAQ = os.path.join(RACINE, "docs", "art", "ui", "maquettes", "ui_campement_v01.png")
FOND, ENCRE, GRIS = (18, 17, 20), (226, 214, 190), (150, 142, 128)
PERSOS = ["renard", "koala", "caméléon", "hibou"]
# rotations : personnage (index) à chaque place
ROT = {"rot_A": [0, 1, 2, 3], "rot_B": [1, 2, 3, 0], "rot_C": [2, 3, 0, 1], "rot_D": [3, 0, 1, 2]}
# cadrage de chaque place dans la capture 1920 × 1080 (x0, y0, x1, y1)
PLACE = [(400, 300, 880, 840), (705, 300, 1185, 840), (1120, 300, 1600, 840), (1440, 300, 1920, 840)]
VIDE = {1: "coop_p2vide", 2: "coop2", 3: "coop3"}
K = 1670 / 1920.0


def pol(t, s="Regular"):
    return ImageFont.truetype(os.path.join(POL, "EBGaramond-%s.ttf" % s), t)


def maquette(box=None, taille=(1920, 1080)):
    m = Image.open(MAQ).convert("RGB")
    if box is None:
        return m.resize(taille, Image.LANCZOS)
    b = tuple(int(round(v * K)) for v in box)
    return m.crop(b).resize((box[2] - box[0], box[3] - box[1]), Image.LANCZOS)


def grille(sortie, titre, entetes, lignes, w, h):
    """lignes : [(légende, [images PIL ou None])]."""
    mg, ht, hc, hl = 20, 64, 40, 36
    nc = len(entetes)
    W = mg * 2 + nc * w + (nc - 1) * 10
    H = ht + hc + len(lignes) * (h + hl + 12) + mg
    o = Image.new("RGB", (W, H), FOND)
    d = ImageDraw.Draw(o)
    d.text((mg, 14), titre, font=pol(38, "SemiBold"), fill=ENCRE)
    for i, e in enumerate(entetes):
        d.text((mg + i * (w + 10), ht + 4), e, font=pol(28, "Italic"), fill=GRIS)
    y = ht + hc
    for leg, ims in lignes:
        d.text((mg, y), leg, font=pol(27), fill=ENCRE)
        y += hl
        for i, im in enumerate(ims):
            x = mg + i * (w + 10)
            if im is None:
                d.rectangle([x, y, x + w - 1, y + h - 1], outline=GRIS)
                d.text((x + w // 2 - 40, y + h // 2 - 16), "sans objet", font=pol(24, "Italic"), fill=GRIS)
            else:
                o.paste(im.convert("RGB").resize((w, h), Image.LANCZOS), (x, y))
        y += h + 12
    o.save(sortie, quality=90)
    print(sortie, o.size)


def main(dos, sortie):
    os.makedirs(sortie, exist_ok=True)
    av = lambda n: Image.open(os.path.join(dos, "avant", n + ".png"))
    ap = lambda n: Image.open(os.path.join(dos, "v2", n + ".png"))
    M = maquette()
    # 1. places vides, plein écran
    grille(os.path.join(sortie, "Campement_Places_Vides.jpg"), "Lot 1 — places vides : maquette, avant (main), après",
           ["Maquette", "Avant (prefab de main)", "Après"],
           [("Joueur 1 seul : places 2 à 4 vides", [M, av("j1_renard"), ap("j1_renard")]),
            ("Coop à 2 : places 3 et 4 vides", [M, av("coop2"), ap("coop2")]),
            ("Coop à 3 : place 4 vide", [M, av("coop3"), ap("coop3")]),
            ("Un joueur part : place 2 vide, places 3 et 4 occupées", [M, av("coop_p2vide"), ap("coop_p2vide")])], 960, 540)
    # 2. chaque place en gros plan : maquette au même cadrage, place vide, les 4 personnages (avant / après)
    for p in range(4):
        box = PLACE[p]
        w, h = box[2] - box[0], box[3] - box[1]
        lignes = []
        for etat, f in (("Avant", av), ("Après", ap)):
            ims = [maquette(box), f(VIDE[p]).crop(box) if p in VIDE else None]
            for c in range(4):
                rot = next(r for r, l in ROT.items() if l[p] == c)
                ims.append(f(rot).crop(box))
            lignes.append((etat, ims))
        grille(os.path.join(sortie, "Campement_Place%d.jpg" % (p + 1)), "Lot 1 — place %d en gros plan (pixels du jeu, 1:1)" % (p + 1),
               ["Maquette", "Vide"] + [c.capitalize() for c in PERSOS], lignes, w, h)
    # 3. joueur 1 seul, chaque personnage, à côté de la maquette
    box = (300, 250, 1300, 900)
    grille(os.path.join(sortie, "Campement_Joueur1.jpg"), "Lot 1 — joueur 1 seul (places 2 à 4 vides), chaque personnage",
           ["Maquette"] + [c.capitalize() for c in PERSOS],
           [("Avant", [maquette(box)] + [av("j1_" + n).crop(box) for n in ("renard", "koala", "cameleon", "hibou")]),
            ("Après", [maquette(box)] + [ap("j1_" + n).crop(box) for n in ("renard", "koala", "cameleon", "hibou")])], 700, 455)
    # 4. coop : prêts, apparition d'un joueur (fondu à 40 %)
    grille(os.path.join(sortie, "Campement_Coop.jpg"), "Lot 1 — coop : prêts, arrivée d'un joueur (image figée à 40 % du fondu)",
           ["Maquette", "Après", "Après"],
           [("Coop à 2, deux prêts · coop à 4, trois prêts", [M, ap("coop2_prets"), ap("coop4_prets")]),
            ("Arrivée à la place 3 · arrivée à la place 4 (ombre et lumière suivent le fondu, personnage retourné)", [M, ap("app_p3_40"), ap("app_p4_40")]),
            ("Arrivée à la place 2", [M, ap("app_p2_40"), None])], 960, 540)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
