# Planche avant/après du lot A (HUD) : maquettes à gauche, sprites découpés sur damier à droite, captures Unity en bas.
import os
from PIL import Image, ImageDraw, ImageFont
from uilib import checker
M = "C:/Users/PC/ItHunts/ItHunts-art/docs/art/ui/maquettes/"
A = "C:/Users/PC/ItHunts/ItHunts-art/Assets/Art/UI/"
OUT = "C:/Users/PC/ItHunts/ItHunts-art/docs/art/rendus/UI/LotA_HUD_avant_apres.jpg"
os.makedirs(os.path.dirname(OUT), exist_ok=True)
F = lambda s: ImageFont.truetype("C:/Windows/Fonts/georgia.ttf", s)
FB = lambda s: ImageFont.truetype("C:/Windows/Fonts/georgiab.ttf", s)
W = 2400
B = Image.new("RGB", (W, 3000), (22, 20, 26)); d = ImageDraw.Draw(B)
d.text((40, 30), "Lot A — pièces du HUD : avant (maquettes générées) / après (sprites découpés, damier = transparent)", font=FB(38), fill=(235, 228, 220))
d.text((40, 82), "Sprites en double définition (PPU 200) ; captures Unity : Test_UI_HUD, canvas 1920×1080 (hauteur), fond provisoire", font=F(26), fill=(170, 165, 175))

def fit(im, w, h):
    s = min(w / im.width, h / im.height); return im.resize((max(1, int(im.width * s)), max(1, int(im.height * s))), Image.LANCZOS)

def on_check(im, w, h, s=12):
    im = fit(im.convert("RGBA"), w, h); bg = checker(im.width, im.height, s).convert("RGBA"); bg.alpha_composite(im); return bg.convert("RGB")

def on_black(im, w, h):
    im = fit(im.convert("RGBA"), w, h); bg = Image.new("RGBA", im.size, (0, 0, 0, 255)); bg.alpha_composite(im); return bg.convert("RGB")

y = 140; col = W // 2
d.text((40, y), "AVANT", font=FB(30), fill=(200, 120, 110)); d.text((col + 20, y), "APRÈS", font=FB(30), fill=(120, 200, 140)); y += 50
rows = [
    ("Barre de vie : emblème | milieu raccordable | bout ; fond et remplissage séparés", "ui_barre_vie_v01.png",
     ["HUD/BarreVie_Cadre_Embleme.png", "HUD/BarreVie_Cadre_Milieu.png", "HUD/BarreVie_Cadre_Bout.png", "|", "HUD/BarreVie_Fond.png", "HUD/BarreVie_Remplissage.png", "HUD/BarreVie_Remplissage_Retard.png"]),
    ("Cartouche des âmes : 3 parties + fond ; quatre-feuilles blancs repeints en creux sombre", "ui_cadre_ames_v01.png",
     ["HUD/Ames_Cadre_Embleme.png", "HUD/Ames_Cadre_Milieu.png", "HUD/Ames_Cadre_Bout.png", "|", "HUD/Ames_Fond.png", "HUD/Ames_Flamme.png"]),
    ("Mini-carte : cadre, masque circulaire (sous l'anneau), fond", "ui_cadre_minicarte_v01.png",
     ["MiniCarte/MiniCarte_Cadre.png", "MiniCarte/MiniCarte_Masque.png", "MiniCarte/MiniCarte_Fond.png"]),
]
for title, src, parts in rows:
    d.text((40, y), title, font=F(26), fill=(220, 214, 206)); y += 40
    h = 300
    B.paste(on_black(Image.open(M + src), col - 80, h), (40, y))
    x = col + 20; stack_x = None; sy = 0
    for p in parts:
        if "Remplissage" in p:
            im = Image.open(A + p); t = on_check(im, 560, 60, 8); B.paste(t, (stack_x, y + sy)); sy += t.height + 10; continue
        if p == "|": x += 24; continue
        im = Image.open(A + p)
        hh = h if "Remplissage" not in p else 60
        ww = int(im.width * hh / im.height)
        if x + ww > W - 20: ww = W - 20 - x
        if "Fond" in p and "BarreVie" in p:
            t = on_check(im, 560, 150, 10); B.paste(t, (x, y)); stack_x = x; sy = t.height + 10; x += t.width + 8; continue
        t = on_check(im, ww, hh, 10); B.paste(t, (x, y)); x += t.width + 8
    y += h + 30
# icônes
d.text((40, y), "Icônes de mini-carte : lueur et fond noir retirés, trait d'encre net (cage, orbe fêlée, losange non découpés)", font=F(26), fill=(220, 214, 206)); y += 40
B.paste(on_black(Image.open(M + "ui_icones_minicarte_v01.png"), col - 80, 520), (40, y))
icons = ["Fleche", "Joueur_Rouge", "Joueur_Vert", "Joueur_Orange", "Joueur_Bleu", "Coffre_Normal", "Coffre_Rare", "Coffre_Ouvert",
         "Porte_Verrouillee", "Porte_Ouverte", "Allie_Mort", "Bougie", "Flamme"]
x0 = col + 20; xx, yy = x0, y
for i, n in enumerate(icons):
    t = on_check(Image.open(A + "MiniCarte/MiniCarte_%s.png" % n), 120, 120, 8); B.paste(t, (xx, yy))
    d.text((xx, yy + 124), n.replace("_", " "), font=F(17), fill=(190, 185, 195))
    xx += 165
    if (i + 1) % 7 == 0: xx = x0; yy += 170
y += 560
# captures Unity
d.text((40, y), "Unity : HUD habillé (Test_UI_HUD) — 1920×1080 et demi-écran 960×1080 ; bas droite : icônes à 48 et 24 px", font=F(26), fill=(220, 214, 206)); y += 40
a = fit(Image.open("unity_hud_1920x1080.png").convert("RGB"), 1480, 900); B.paste(a, (40, y))
b = fit(Image.open("unity_hud_960x1080.png").convert("RGB"), 820, a.height); B.paste(b, (40 + a.width + 20, y))
y += a.height + 30
B = B.crop((0, 0, W, y))
B.save(OUT, quality=88)
print(OUT, B.size)
