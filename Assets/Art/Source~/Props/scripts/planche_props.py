# -*- coding: utf-8 -*-
"""Lot 4 b : planche de contrôle des props (rendus de rendu_props.py) avec légendes en EB Garamond.
    python -I planche_props.py <dossier_rendus> <sortie.jpg>
Chaque case : rendu du LOD0 (FBX réimporté, silhouette de 1.8 m, coffres ouverts à 70°), vignette du LOD1 en bas à
gauche, légende : nom, cotes l × p × h mesurées sur le FBX, triangles LOD0 / LOD1, passe. Les PNG du dossier sont
convertis en JPG (qualité 90) pour alléger le dépôt.
"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
POL = os.path.join(HERE, "..", "..", "..", "UI", "Polices", "EBGaramond")
DOS, SORTIE = sys.argv[1], sys.argv[2]
NOUVEAUX = ["Lanterne", "Etagere", "CoffreDeco", "Table", "Banc", "Paille", "Armoire", "Lit", "Chaudron", "Poutre"]
ANCIENS = ["Tonneau", "Chaise", "Sarcophage", "Ratelier", "Coffre", "CoffreVerrou", "Statue"]
NOMS_FR = dict(Lanterne="Lanterne", Etagere="Étagère", CoffreDeco="Coffre décoratif", Table="Table", Banc="Banc",
               Paille="Paille (litière)", Armoire="Armoire", Lit="Lit de cachot", Chaudron="Chaudron", Poutre="Poutre (refaite)",
               Tonneau="Tonneau", Chaise="Chaise", Sarcophage="Sarcophage", Ratelier="Râtelier", Coffre="Coffre de récompense",
               CoffreVerrou="Coffre verrouillé", Statue="Statue")
mes = json.load(open(os.path.join(DOS, "mesures_fbx.json"), encoding="utf-8"))
for f in os.listdir(DOS):
    if f.endswith(".png"):
        im = Image.open(os.path.join(DOS, f)).convert("RGB")
        im.save(os.path.join(DOS, f[:-4] + ".jpg"), quality=90)
        os.remove(os.path.join(DOS, f))

W, H, L, COLS = 600, 400, 112, 4
ordre = NOUVEAUX + ANCIENS
rows = (len(ordre) + COLS - 1) // COLS
TITRE = 96
out = Image.new("RGB", (W * COLS, TITRE + rows * (H + L)), (14, 11, 10))
d = ImageDraw.Draw(out)
f_t = ImageFont.truetype(os.path.join(POL, "EBGaramond-SemiBold.ttf"), 44)
f_n = ImageFont.truetype(os.path.join(POL, "EBGaramond-SemiBold.ttf"), 27)
f_c = ImageFont.truetype(os.path.join(POL, "EBGaramond-Regular.ttf"), 21)
f_i = ImageFont.truetype(os.path.join(POL, "EBGaramond-Italic.ttf"), 18)
d.text((24, 14), "Props du lot 4 : deuxième passe (nouveaux modèles, LOD1, coffres creusés)", font=f_t, fill=(232, 214, 186))
d.text((26, 64), "FBX réimportés dans Blender · silhouette de 1,80 m · éclairage sombre et chaud · vignette : LOD1 · coffres ouverts à 70°",
       font=f_i, fill=(170, 150, 128))
for i, n in enumerate(ordre):
    x, y = (i % COLS) * W, TITRE + (i // COLS) * (H + L)
    out.paste(Image.open(os.path.join(DOS, "Prop_%s.jpg" % n)).resize((W, H)), (x, y))
    p1 = os.path.join(DOS, "Prop_%s_LOD1.jpg" % n)
    if os.path.exists(p1):
        v = Image.open(p1).resize((150, 100))       # vignette du LOD1 dans la bande de légende, à droite
        out.paste(v, (x + W - 158, y + H + 6))
        d.rectangle((x + W - 158, y + H + 6, x + W - 8, y + H + 106), outline=(120, 100, 80))
        d.text((x + W - 152, y + H + 7), "LOD1", font=f_i, fill=(220, 200, 170))
    m = mes[n]
    dx, dy, dz = m["dimensions"]
    d.text((x + 14, y + H + 6), NOMS_FR[n], font=f_n, fill=(236, 220, 192))
    passe = "nouveau" if n in NOUVEAUX and n != "Poutre" else ("refait" if n == "Poutre" else "1re passe + LOD1")
    tri = "LOD0 %d tri" % m["triangles_lod0"] + (" · LOD1 %d" % m["triangles_lod1"] if m["triangles_lod1"] else " · sans LOD1")
    d.text((x + 14, y + H + 40), "%.2f × %.2f × %.2f m (l × p × h)" % (dx, dy, dz), font=f_c, fill=(196, 178, 152))
    d.text((x + 14, y + H + 66), "%s · %s" % (tri, passe), font=f_c, fill=(196, 178, 152))
out.save(SORTIE, quality=88)
print("planche", out.size, SORTIE)
