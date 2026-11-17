# Montage d'essai du HUD à partir des sprites découpés (taille native 1080p = pixels sprite / 2).
import json, sys, numpy as np
from PIL import Image, ImageDraw, ImageFont, ImageFilter
O = "out/"
K = float(sys.argv[1]) if len(sys.argv) > 1 else 0.5   # facteur sprite -> écran
W, H = int(sys.argv[2]) if len(sys.argv) > 2 else 1920, 1080
mes = json.load(open(O + "mesures.json"))
bg = Image.open("C:/Users/PC/ItHunts/ItHunts-art/docs/art/ui/maquettes/ui_menu_principal_fond_v01.png").convert("RGB")
bg = bg.resize((int(bg.width * H / bg.height), H)).crop((0, 0, W, H)).filter(ImageFilter.GaussianBlur(3))
can = bg.convert("RGBA")

def L(p, k=K):
    im = Image.open(O + p).convert("RGBA")
    return im.resize((max(1, round(im.width * k)), max(1, round(im.height * k))), Image.LANCZOS)

def bar3(prefix, n_mid, x, y):
    e, m, b = L(prefix + "_Embleme.png"), L(prefix + "_Milieu.png"), L(prefix + "_Bout.png")
    can.alpha_composite(e, (x, y)); cx = x + e.width
    for i in range(n_mid): can.alpha_composite(m, (cx, y)); cx += m.width
    can.alpha_composite(b, (cx, y))
    return cx + b.width - x

def font(sz, bold=False):
    for f in ["georgiab.ttf" if bold else "georgia.ttf", "times.ttf"]:
        try: return ImageFont.truetype("C:/Windows/Fonts/" + f, sz)
        except OSError: pass
    return ImageFont.load_default()

# --- barre de vie (bas gauche) : fond, remplissage retard 60 %, remplissage 45 %, cadre
m = 40; yv = H - m - round(mes["tailles"]["BarreVie_Cadre"]["Embleme"][1] * K)
n_mid = 4
wtot = bar3("HUD/BarreVie_Fond", n_mid, m, yv)
cv = mes["canal_vie"]; extra = n_mid * mes["tailles"]["BarreVie_Cadre"]["Milieu"][0] - (mes["tailles"]["BarreVie_Cadre"]["entier"][0] - mes["tailles"]["BarreVie_Cadre"]["Embleme"][0] - mes["tailles"]["BarreVie_Cadre"]["Bout"][0])
for nom, f in [("BarreVie_Remplissage_Retard", 0.60), ("BarreVie_Remplissage", 0.45)]:
    im = Image.open(O + "HUD/%s.png" % nom).convert("RGBA")
    im = im.resize((round((im.width + extra) * K), round(im.height * K)), Image.LANCZOS)
    im = im.crop((0, 0, round(im.width * f), im.height))
    can.alpha_composite(im, (m + round(cv["x"] * K), yv + round(cv["y_haut"] * K)))
bar3("HUD/BarreVie_Cadre", n_mid, m, yv)
d = ImageDraw.Draw(can)
fs = max(10, round(cv["h"] * K * 0.62))
d.text((m + round((cv["x"] + 24) * K), yv + round((cv["y_haut"] + cv["h"] / 2) * K)), "45/100", font=font(fs, True), fill=(240, 228, 220), anchor="lm", stroke_width=2, stroke_fill=(10, 6, 10))

# --- âmes (haut gauche) : fond, cadre, flamme dans le rond, nombre
ya = m; xa = m
bar3("HUD/Ames_Fond", 2, xa, ya); bar3("HUD/Ames_Cadre", 2, xa, ya)
za = mes["zones_ames"]; fl = Image.open(O + "HUD/Ames_Flamme.png").convert("RGBA")
fh = round(za["rond_diam"] * K * 0.80); fl = fl.resize((round(fl.width * fh / fl.height), fh), Image.LANCZOS)
can.alpha_composite(fl, (xa + round(za["rond_centre"][0] * K) - fl.width // 2, ya + round(za["rond_centre"][1] * K) - fl.height // 2))
d.text((xa + round((za["texte_x0"] + 22) * K), ya + round((za["texte_y0"] + za["texte_h"] / 2) * K)), "1 284", font=font(round(za["texte_h"] * K * 0.62), True),
       fill=(214, 226, 236), anchor="lm", stroke_width=2, stroke_fill=(10, 6, 10))

# --- mini-carte (haut droite) : fond, fausse carte masquée, icônes, cadre
mc = mes["minicarte"]; cadre = L("MiniCarte/MiniCarte_Cadre.png"); xm, ym = W - m - cadre.width, m
fondm = L("MiniCarte/MiniCarte_Fond.png"); masque = L("MiniCarte/MiniCarte_Masque.png")
can.alpha_composite(fondm, (xm, ym))
carte = Image.new("RGBA", cadre.size, (0, 0, 0, 0)); dc = ImageDraw.Draw(carte)
rng = np.random.default_rng(2)
for i in range(14):   # couloirs factices
    x0, y0 = rng.integers(0, cadre.width, 2); dx = rng.integers(40, 160)
    if i % 2: dc.rectangle((x0, y0, x0 + dx, y0 + 10), fill=(70, 66, 84, 255))
    else: dc.rectangle((x0, y0, x0 + 10, y0 + dx), fill=(70, 66, 84, 255))
carte.putalpha(Image.fromarray((np.array(carte)[..., 3].astype(float) * np.array(masque)[..., 3] / 255).astype(np.uint8)))
can.alpha_composite(carte, (xm, ym))
cxm, cym = xm + round(mc["centre_trou"][0] * K), ym + round(mc["centre_trou"][1] * K)
for nom, dx, dy, sz in [("Fleche", 0, 0, 24), ("Joueur_Vert", -50, 30, 16), ("Joueur_Orange", 40, -45, 16), ("Joueur_Bleu", 60, 40, 16),
                        ("Coffre_Normal", -70, -40, 24), ("Coffre_Rare", 20, 70, 24), ("Coffre_Ouvert", -20, -80, 24),
                        ("Porte_Verrouillee", 80, -5, 24), ("Allie_Mort", -85, 15, 24), ("Bougie", -40, 75, 24), ("Flamme", 45, 20, 24)]:
    ic = Image.open(O + "MiniCarte/MiniCarte_%s.png" % nom).convert("RGBA").resize((sz, sz), Image.LANCZOS)
    can.alpha_composite(ic, (cxm + round(dx * K * 2) - sz // 2, cym + round(dy * K * 2) - sz // 2))
can.alpha_composite(cadre, (xm, ym))
can.convert("RGB").save(sys.argv[3] if len(sys.argv) > 3 else "verif_hud.jpg", quality=90)
print("ok", W, H)
