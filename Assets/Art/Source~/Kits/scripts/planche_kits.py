# -*- coding: utf-8 -*-
"""Planche des rendus de contrôle d'un lot de pièces (nuit 4, lot 3) : une vignette par pièce, groupées par acte, avec nom,
emprise et triangles lus dans les .json des kits.

    python planche_kits.py <dossier des rendus> <planche.jpg> "<titre>" <nom> [<nom> …]
"""
import sys, os, json
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
dos, sortie, titre = sys.argv[1:4]
noms = sys.argv[4:]
M = {}
for js in ("kit_caves.json", "kit_chateau.json", "kit_jardin.json"):
    M.update(json.load(open(os.path.join(HERE, js), encoding="utf-8")))


def police(t):
    for f in ("C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf"):
        if os.path.exists(f):
            return ImageFont.truetype(f, t)
    return ImageFont.load_default()


F1, F2, F3 = police(54), police(22), police(19)
W, H, COLS = 480, 360, 8
actes = [("Cave", "Acte I : caves"), ("Chateau", "Acte II : château"), ("Jardin", "Acte III : jardin")]
groupes = [(t, [n for n in noms if n.split("_")[1] == a]) for a, t in actes]
haut = 110 + sum(60 + ((len(g) + COLS - 1) // COLS) * (H + 52) for _, g in groupes if g)
P = Image.new("RGB", (W * COLS, haut), (16, 17, 21))
d = ImageDraw.Draw(P)
d.text((24, 24), titre, font=F1, fill=(236, 230, 214))
y = 110
for t, g in groupes:
    if not g:
        continue
    d.text((24, y + 10), f"{t} ({len(g)} pièces)", font=F2, fill=(230, 190, 120))
    y += 60
    for i, n in enumerate(g):
        x0, y0 = (i % COLS) * W, y + (i // COLS) * (H + 52)
        im = Image.open(os.path.join(dos, n + ".jpg")).convert("RGB").resize((W, H), Image.LANCZOS)
        P.paste(im, (x0, y0))
        m = M.get(n, {})
        ta = m.get("taille", ["?"] * 3)
        d.text((x0 + 8, y0 + H + 4), n.replace("SM_", ""), font=F2, fill=(240, 240, 240))
        d.text((x0 + 8, y0 + H + 28), f"{ta[0]} × {ta[1]} × {ta[2]} m · {m.get('triangles', '?')} tri.", font=F3, fill=(170, 170, 175))
    y += ((len(g) + COLS - 1) // COLS) * (H + 52)
P.save(sortie, quality=86)
print("ok", sortie, P.size)
