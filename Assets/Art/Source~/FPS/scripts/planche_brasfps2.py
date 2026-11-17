# Planche du bras FPS v2 d'un personnage : référence, ancienne version, rendus Blender de près, rendus en jeu (Unity, Test_Rendu).
# python planche_brasfps2.py <dossier des rendus Blender b_*.jpg> <dossier des rendus Unity jeu_*.png / gros_*.png> [Perso]
import sys, os
from PIL import Image, ImageDraw, ImageFont
B_DIR, U_DIR = sys.argv[1], sys.argv[2]
PERSO = sys.argv[3] if len(sys.argv) > 3 else "Renard"
NOMS = {"Renard": "renard", "Koala": "koala", "Cameleon": "caméléon", "Hibou": "hibou"}
DESC = {"Renard": "gant sans doigts, griffes", "Koala": "gant complet, griffes qui percent le gant, fourrure",
        "Cameleon": "mitaine, doigts et avant-bras écailleux, sans griffes", "Hibou": "sans gant, main écailleuse en serres, plumes"}
TRIS = {"Renard": "10 018", "Koala": "8 230", "Cameleon": "9 524", "Hibou": "8 005"}
LUM = {"Renard": "lanterne rouge du renard", "Koala": "lanterne verte du koala", "Cameleon": "lanterne orange du caméléon",
       "Hibou": "lanterne bleue du hibou"}
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".."))
OUT = os.path.join(ROOT, "docs", "art", "rendus", "FPS", "BrasFPS_v2_%s.jpg" % PERSO)
F = lambda s: ImageFont.truetype("C:/Windows/Fonts/georgia.ttf", s)
FB = lambda s: ImageFont.truetype("C:/Windows/Fonts/georgiab.ttf", s)
P = [("Pose_Repos", "Repos (flottement)"), ("Pose_Poing", "Poing"), ("Pose_MainOuverte", "Main ouverte"),
     ("Pose_Griffe", "Griffe"), ("Pose_TenirObjet", "Prise (tenir un objet)")]
W = 2500
img = Image.new("RGB", (W, 4200), (20, 19, 23)); d = ImageDraw.Draw(img)
d.text((40, 30), "Bras FPS v2 — %s : main anatomique (base CC0), %s, 20 os" % (NOMS[PERSO], DESC[PERSO]), font=FB(40), fill=(236, 228, 220))
d.text((40, 85), TRIS[PERSO] + " triangles par bras, 4 os max par sommet, texture peinte 2048 ; poses repos / poing / main ouverte / griffe / prise", font=F(26), fill=(170, 165, 175))

def fit(im, w, h):
    s = min(w / im.width, h / im.height); return im.resize((int(im.width * s), int(im.height * s)), Image.LANCZOS)

y = 140
ref = fit(Image.open(os.path.join(ROOT, "docs", "design", "references-visuelles", "reference_bras_fps.png")).convert("RGB"), 1220, 690)
old = fit(Image.open(os.path.join(ROOT, "docs", "art", "rendus", "FPS", "BrasFPS_Poses_%s.jpg" % PERSO)).convert("RGB"), 1200, 690)
d.text((40, y), "Référence", font=FB(28), fill=(200, 190, 180)); d.text((1290, y), "Avant (v1 : doigts en tubes)", font=FB(28), fill=(200, 120, 110))
y += 45; img.paste(ref, (40, y)); img.paste(old, (1290, y)); y += 720
d.text((40, y), "Après — Blender, de près (deux angles par pose)", font=FB(30), fill=(120, 200, 140)); y += 50
cw = (W - 80) // 5
for i, (p, lab) in enumerate(P):
    x = 40 + i * cw
    d.text((x, y), lab, font=F(24), fill=(220, 214, 206))
    for r, v in enumerate("ab"):
        im = fit(Image.open(os.path.join(B_DIR, "b_%s_%s.jpg" % (p, v))).convert("RGB"), cw - 10, cw - 10)
        img.paste(im, (x, y + 36 + r * (cw)))
y += 36 + 2 * cw + 20
d.text((40, y), "Après — en jeu (Unity, shader toon, lumière de Test_Rendu : " + LUM[PERSO] + ", torche du couloir ; cadrage de FpsView)", font=FB(30), fill=(120, 200, 140)); y += 50
uw = (W - 80 - 20) // 2; uh = int(uw * 9 / 16)
for i, (p, lab) in enumerate(P[:4]):
    x = 40 + (i % 2) * (uw + 20); yy = y + (i // 2) * (uh + 50)
    d.text((x, yy), lab, font=F(24), fill=(220, 214, 206))
    img.paste(fit(Image.open(os.path.join(U_DIR, "jeu_%s.png" % p)).convert("RGB"), uw, uh), (x, yy + 34))
y += 2 * (uh + 50)
gw = (W - 80 - 40) // 3
d.text((40, y), "En jeu, gros plan de la main droite", font=F(24), fill=(220, 214, 206)); y += 34
for i, p in enumerate(("Pose_TenirObjet", "Pose_Poing", "Pose_Griffe")):
    img.paste(fit(Image.open(os.path.join(U_DIR, "gros_%s.png" % p)).convert("RGB"), gw, gw), (40 + i * (gw + 20), y))
y += gw + 30
img = img.crop((0, 0, W, y)); img.save(OUT, quality=86); print(OUT, img.size)
