# Planche de la carte ouverte : montage 1920 x 1080 (fond raccordé, brume sur la zone non découverte, vignette, cadre et
# légende en 9 tranches à l'échelle des sprites PPU 200, rose, icônes de la mini-carte dans la légende).
import os, json
import numpy as np
from PIL import Image, ImageDraw, ImageFont
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
D = os.path.join(ROOT, "Assets", "Art", "UI", "Carte"); MC = os.path.join(ROOT, "Assets", "Art", "UI", "MiniCarte")
OUT = os.path.join(ROOT, "docs", "art", "rendus", "UI", "Carte_montage.jpg")
M = json.load(open(os.path.join(HERE, "carte_mesures.json"), encoding="utf-8"))
W, H = 1920, 1080
def img(n, d=D): return Image.open(os.path.join(d, n + ".png")).convert("RGBA")
def slice9(src, B, w, h, k=0.5):
    a = np.asarray(src); S = a.shape[0]; b = int(B * k); out = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    xs = [(0, B, 0, b), (B, S - B, b, w - 2 * b), (S - B, S, w - b, b)]; ys = [(0, B, 0, b), (B, S - B, b, h - 2 * b), (S - B, S, h - b, b)]
    for (y0, y1, dy, hh) in ys:
        for (x0, x1, dx, ww) in xs:
            out.alpha_composite(Image.fromarray(a[y0:y1, x0:x1]).resize((max(1, ww), max(1, hh)), Image.LANCZOS), (dx, dy))
    return out
can = Image.new("RGBA", (W, H), (0, 0, 0, 255))
fond = img("Carte_Fond").resize((512, 512))
for x in range(0, W, 512):
    for y in range(0, H, 512): can.alpha_composite(fond, (x, y))
# couloirs et salles « découverts », tracés à l'encre claire (exemple de contenu dessiné par le jeu)
dr = ImageDraw.Draw(can)
for r in ((300, 300, 520, 470), (700, 380, 980, 600), (420, 650, 640, 840)):
    dr.rectangle(r, outline=(205, 190, 165, 255), width=4)
for l in (((520, 390), (700, 450)), ((540, 470), (540, 650)), ((640, 740), (840, 600))):
    dr.line(l, fill=(205, 190, 165, 255), width=10)
br = img("Carte_Brume").resize((512, 512)); fog = Image.new("RGBA", (W, H))
for x in range(0, W, 512):
    for y in range(0, H, 512): fog.alpha_composite(br, (x, y))
yy, xx = np.mgrid[0:H, 0:W]
m = np.clip((xx - 1050) / 220.0, 0, 1) * 1.0
fa = np.asarray(fog, np.float32); fa[..., 3] *= m; can.alpha_composite(Image.fromarray(fa.astype(np.uint8)))
can.alpha_composite(img("Carte_Vignette").resize((W, H)))
can.alpha_composite(slice9(img("Carte_Cadre"), M["Carte_Cadre"]["bordures"][0], W, H))
lw, lh = 380, 330; lx, ly = W - 130 - 40 - lw, H - 130 - 40 - lh
can.alpha_composite(slice9(img("Carte_Legende_Cadre"), M["Carte_Legende_Cadre"]["bordures"][0], lw, lh), (lx, ly))
try:
    fnt = ImageFont.truetype(os.path.join(ROOT, "Assets", "Art", "UI", "Polices", "EBGaramond", "EBGaramond-SemiBold.ttf"), 26)
    ttl = ImageFont.truetype(os.path.join(ROOT, "Assets", "Art", "UI", "Polices", "GrenzeGotisch", "GrenzeGotisch-SemiBold.ttf"), 44)
except Exception:
    fnt = ttl = ImageFont.load_default()
d = ImageDraw.Draw(can)
for i, (ic, lab) in enumerate((("MiniCarte_Fleche", "Vous"), ("MiniCarte_Coffre_Normal", "Coffre"), ("MiniCarte_Bougie", "Marchand"), ("MiniCarte_Flamme", "Âmes"), ("MiniCarte_Porte_Verrouillee", "Sortie"))):
    ico = img(ic, MC).resize((40, 40)); y = ly + 50 + i * 52
    can.alpha_composite(ico, (lx + 45, y)); d.text((lx + 100, y + 4), lab, font=fnt, fill=(214, 224, 235))
d.text((W // 2 - 70, 40), "Carte", font=ttl, fill=(216, 198, 205))
ro = img("Carte_Rose"); ro = ro.resize((ro.width // 2, ro.height // 2)); can.alpha_composite(ro, (170, 170))
os.makedirs(os.path.dirname(OUT), exist_ok=True); can.convert("RGB").save(OUT, quality=86); print(OUT)
