# -*- coding: utf-8 -*-
"""Légendes et planche des poses de l'orbe (Python avec Pillow, hors de Blender), après sm_orbe_poses.py.

Lit docs/art/rendus/FPS/_poses_tmp/ (PNG + legendes.json), écrit Orbe_Poses_<état>.jpg et Orbe_Poses_Planche.jpg
(qualité 90) dans docs/art/rendus/FPS/, puis supprime le dossier temporaire.
    python orbe_poses_legendes.py
"""
import os, json, shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", ".."))
OUT = os.path.join(ROOT, "docs", "art", "rendus", "FPS")
TMP = os.path.join(OUT, "_poses_tmp")
JAUGE = "Jauge : traction 02-03 · répulsion 04 · mouvement 01-05-06 · allumée = charge prête"


def font(bold, size):
    for p in (("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf"),
              "/usr/share/fonts/truetype/dejavu/DejaVuSans%s.ttf" % ("-Bold" if bold else "")):
        try:
            return ImageFont.truetype(p, size)
        except OSError:
            pass
    return ImageFont.load_default()


def main():
    leg = json.load(open(os.path.join(TMP, "legendes.json"), encoding="utf-8"))
    f1, f2 = font(True, 34), font(False, 20)
    outs = []
    for e in leg:
        im = Image.open(os.path.join(TMP, "Orbe_Poses_%s.png" % e["nom"])).convert("RGB")
        band = 150
        out = Image.new("RGB", (im.width, im.height + band), (18, 17, 21))
        out.paste(im, (0, 0))
        d = ImageDraw.Draw(out)
        d.text((24, im.height + 12), e["titre"], fill=(255, 150, 70), font=f1)
        line, y = "", im.height + 58
        for wd in e["texte"].split():
            if d.textlength((line + " " + wd).strip(), font=f2) > im.width - 48:
                d.text((24, y), line, fill=(220, 214, 206), font=f2); y += 26; line = wd
            else:
                line = (line + " " + wd).strip()
        d.text((24, y), line, fill=(220, 214, 206), font=f2)
        d.text((24, im.height + band - 30), JAUGE, fill=(150, 140, 130), font=f2)
        out.save(os.path.join(OUT, "Orbe_Poses_%s.jpg" % e["nom"]), quality=90)
        outs.append(out)
    w, h = outs[0].width // 2, outs[0].height // 2
    board = Image.new("RGB", (4 * w, 2 * h), (18, 17, 21))
    for i, o in enumerate(outs):
        board.paste(o.resize((w, h), Image.LANCZOS), ((i % 4) * w, (i // 4) * h))
    board.save(os.path.join(OUT, "Orbe_Poses_Planche.jpg"), quality=90)
    shutil.rmtree(TMP)
    print("écrit :", len(outs), "poses + planche")


if __name__ == "__main__":
    main()
