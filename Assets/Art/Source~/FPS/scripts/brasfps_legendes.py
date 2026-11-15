# -*- coding: utf-8 -*-
"""Planches JPEG des bras FPS (Python avec Pillow, hors de Blender), après sk_brasfps_rendus.py.

Lit docs/art/rendus/FPS/_bras_tmp/ et écrit dans docs/art/rendus/FPS/ :
- BrasFPS_Planche.jpg : les 4 bras (main droite, repos, de profil côté pouce) ;
- BrasFPS_VueFPS_<Perso>.jpg : vue de la caméra des mains (60°), mains à leur pose de repos de FpsView, orbe greybox ;
- BrasFPS_Poses_<Perso>.jpg : les 10 poses de la main droite, plus les 3 clés de Pose_Attraper.
Puis supprime le dossier temporaire.
    python brasfps_legendes.py
"""
import os, json, shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", ".."))
OUT = os.path.join(ROOT, "docs", "art", "rendus", "FPS")
TMP = os.path.join(OUT, "_bras_tmp")
BG = (18, 17, 21)
NOMS = {"Renard": "Renard", "Koala": "Koala", "Cameleon": "Caméléon", "Hibou": "Hibou"}
POSE_TXT = {
    "Pose_Repos": "Repos", "Pose_OrbePaume": "Orbe dans la paume", "Pose_Pistolet": "Pistolet",
    "Pose_PriseAnneau": "Prise d'anneau (lanterne)", "Pose_Traction": "Traction", "Pose_Repulsion": "Répulsion",
    "Pose_Attraper": "Attraper (fin : refermée)", "Pose_TenirObjet": "Tenir un objet", "Pose_Lancer": "Lancer",
    "Pose_Poing": "Poing (bonus)",
}


def font(bold, size):
    for p in ("C:/Windows/Fonts/arialbd.ttf" if bold else "C:/Windows/Fonts/arial.ttf",
              "/usr/share/fonts/truetype/dejavu/DejaVuSans%s.ttf" % ("-Bold" if bold else "")):
        try:
            return ImageFont.truetype(p, size)
        except OSError:
            pass
    return ImageFont.load_default()


def label(im, title, sub=None, band=64):
    out = Image.new("RGB", (im.width, im.height + band), BG)
    out.paste(im, (0, 0))
    d = ImageDraw.Draw(out)
    d.text((14, im.height + 8), title, fill=(255, 150, 70), font=font(True, 24))
    if sub:
        d.text((14, im.height + 38), sub, fill=(200, 194, 186), font=font(False, 16))
    return out


def grid(ims, cols, path):
    w, h = ims[0].size
    rows = (len(ims) + cols - 1) // cols
    board = Image.new("RGB", (cols * w, rows * h), BG)
    for i, im in enumerate(ims):
        board.paste(im, ((i % cols) * w, (i // cols) * h))
    board.save(path, quality=90)


def main():
    leg = json.load(open(os.path.join(TMP, "legendes.json"), encoding="utf-8"))
    arms = []
    for e in leg["bras"]:
        im = Image.open(os.path.join(TMP, e["png"])).convert("RGB").resize((540, 540), Image.LANCZOS)
        arms.append(label(im, NOMS[e["perso"]], "%d triangles par bras" % e["tris"]))
    grid(arms, 4, os.path.join(OUT, "BrasFPS_Planche.jpg"))
    for e in leg["fps"]:
        im = Image.open(os.path.join(TMP, e["png"])).convert("RGB")
        label(im, "%s : vue de la caméra des mains (60°), pose de repos" % NOMS[e["perso"]],
              "mains à leur position de repos de FpsView ; sphère orange = orbe greybox (Ø 0.08 m) sur Socket_Orbe",
              band=70).save(os.path.join(OUT, "BrasFPS_VueFPS_%s.jpg" % e["perso"]), quality=90)
    for perso in NOMS:
        ims = []
        for e in [x for x in leg["poses"] if x["perso"] == perso]:
            im = Image.open(os.path.join(TMP, e["png"])).convert("RGB").resize((420, 420), Image.LANCZOS)
            ims.append(label(im, POSE_TXT[e["pose"]], e["pose"], band=58))
        for i in (1, 2, 3):
            fn = os.path.join(TMP, "Pose_%s_Pose_Attraper_cle%d.png" % (perso, i))
            if os.path.exists(fn):
                im = Image.open(fn).convert("RGB").resize((420, 420), Image.LANCZOS)
                ims.append(label(im, "Attraper, clé %d / 3" % i, ("repos", "ouverte", "refermée")[i - 1], band=58))
        grid(ims, 5, os.path.join(OUT, "BrasFPS_Poses_%s.jpg" % perso))
    shutil.rmtree(TMP)
    print("planches écrites")


if __name__ == "__main__":
    main()
