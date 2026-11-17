# Carte ouverte (Tab) : sprites dans la famille du HUD, tirés des pièces existantes (rails de la barre de vie, étoile de
# son emblème) + fond de parchemin sombre, brume de la zone non découverte et pinceau de dévoilement, panneau de légende,
# rose des vents. Double définition (PPU 200), comme le lot A.
#   python carte.py        -> Assets/Art/UI/Carte/*.png, mesures dans carte_mesures.json
import os, json
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
HUD = os.path.join(ROOT, "Assets", "Art", "UI", "HUD")
OUT = os.path.join(ROOT, "Assets", "Art", "UI", "Carte")
FOND = np.array([14, 11, 18], np.float32)          # #0E0B12, fond des cartouches du HUD


def load(n):
    return np.asarray(Image.open(os.path.join(HUD, n + ".png")).convert("RGBA"), np.float32) / 255


def save(a, n):
    os.makedirs(OUT, exist_ok=True)
    Image.fromarray(np.clip(a * 255, 0, 255).round().astype(np.uint8), "RGBA").save(os.path.join(OUT, n + ".png"))


def resize(a, s):
    h, w = a.shape[:2]
    im = Image.fromarray(np.clip(a * 255, 0, 255).astype(np.uint8), "RGBA").resize((max(1, int(w * s)), max(1, int(h * s))), Image.LANCZOS)
    return np.asarray(im, np.float32) / 255


def over(dst, src, x, y):
    """Compose src (RGBA) sur dst à (x, y), alpha prémultiplié à la volée."""
    h, w = src.shape[:2]
    x0, y0 = max(0, x), max(0, y); x1, y1 = min(dst.shape[1], x + w), min(dst.shape[0], y + h)
    if x1 <= x0 or y1 <= y0: return
    s = src[y0 - y:y1 - y, x0 - x:x1 - x]; d = dst[y0:y1, x0:x1]
    a = s[..., 3:4]; out_a = a + d[..., 3:4] * (1 - a)
    rgb = (s[..., :3] * a + d[..., :3] * d[..., 3:4] * (1 - a)) / np.maximum(out_a, 1e-6)
    dst[y0:y1, x0:x1, :3] = rgb; dst[y0:y1, x0:x1, 3:4] = out_a


def blur(a, s):
    h, w = a.shape
    fy = np.fft.fftfreq(h)[:, None]; fx = np.fft.fftfreq(w)[None, :]
    return np.real(np.fft.ifft2(np.fft.fft2(a) * np.exp(-2 * (np.pi * s) ** 2 * (fx ** 2 + fy ** 2))))


def tnoise(n, seed, scales):
    """Bruit raccordable (périodique) de taille n, somme d'échelles (en px)."""
    rng = np.random.default_rng(seed); out = np.zeros((n, n))
    for i, s in enumerate(scales):
        g = blur(rng.standard_normal((n, n)), s); out += g / (np.abs(g).max() + 1e-9) * (0.6 ** i)
    out -= out.min(); return out / out.max()


M = {}
# ------------------------------------------------------------------ pièces du HUD
mil = load("BarreVie_Cadre_Milieu")           # 262 x 128, rails à y 93-109 et 152-167
rail2 = mil[86:175]                            # les deux rails et leur liseré
rail1 = mil[86:116]                            # un rail seul
emb = load("BarreVie_Cadre_Embleme")           # 262 x 245, étoile à gauche, rails vers la droite
a = emb[..., 3] > 0.15
cx = int(np.round(np.nonzero(a[3])[0].mean())); cy = emb.shape[0] // 2
yy, xx = np.mgrid[0:emb.shape[0], 0:emb.shape[1]]
R = 128
star = emb.copy(); star[..., 3] *= np.clip((R - np.hypot(xx - cx, yy - cy)) / 6.0, 0, 1)   # sans les amorces des rails
star = star[cy - R:cy + R, max(0, cx - R):cx + R]
M["étoile (centre dans l'emblème)"] = [cx, cy]

# ------------------------------------------------------------------ 1. cadre plein écran (9 tranches)
B = 260                                         # bordure (px du sprite) : 130 px en 1080p
S = 1024
sc_star = 1.0
st = resize(star, sc_star)
r2 = resize(rail2, 1.0); rh = r2.shape[0]
cad = np.zeros((S, S, 4), np.float32)
# bande sombre du bord de l'écran jusqu'au rail intérieur (86 %), dégradé vers l'intérieur
band = np.zeros((S, S), np.float32)
d_edge = np.minimum(np.minimum(np.arange(S)[:, None], np.arange(S)[None, :]), np.minimum(S - 1 - np.arange(S)[:, None], S - 1 - np.arange(S)[None, :]))
band = np.clip((B * 0.5 + rh * 0.5 + 6 - d_edge) / 10.0, 0, 1) * 0.86
cad[..., :3] = FOND / 255; cad[..., 3] = band
mid = B // 2
# rails : haut et bas (tuile répétée), gauche et droite (tuile tournée)
tile = r2; tw = tile.shape[1]
for x in range(0, S, tw):
    over(cad, tile, x, mid - rh // 2); over(cad, tile, x, S - mid - rh // 2 - 1)
tv = np.rot90(tile).copy()
for y in range(0, S, tv.shape[0]):
    over(cad, tv, mid - rh // 2, y); over(cad, tv, S - mid - rh // 2 - 1, y)
for (x, y) in ((mid, mid), (S - mid - 1, mid), (mid, S - mid - 1), (S - mid - 1, S - mid - 1)):
    over(cad, st, x - st.shape[1] // 2, y - st.shape[0] // 2)
save(cad, "Carte_Cadre")
M["Carte_Cadre"] = dict(taille=[S, S], bordures=[B, B, B, B], note="Image Sliced, plein écran ; centre transparent")

# ------------------------------------------------------------------ 2. fond : parchemin sombre raccordable (1024)
N = 1024
fib = tnoise(N, 11, (1.2,)); fib_l = np.asarray(Image.fromarray((fib * 255).astype(np.uint8)).resize((N, N)), np.float32) / 255
grain = tnoise(N, 12, (1.0, 3.0)); taches = tnoise(N, 13, (60, 25)); plis = tnoise(N, 14, (120,))
# fibres étirées : bruit flouté surtout en x
rng = np.random.default_rng(15); f = rng.standard_normal((N, N))
fy = np.fft.fftfreq(N)[:, None]; fx = np.fft.fftfreq(N)[None, :]
fibre = np.real(np.fft.ifft2(np.fft.fft2(f) * np.exp(-2 * np.pi ** 2 * ((fx * 9) ** 2 + (fy * 1.2) ** 2))))
fibre = (fibre - fibre.min()) / np.ptp(fibre)
c0 = np.array([0.105, 0.088, 0.078]); c1 = np.array([0.300, 0.255, 0.205])     # brun gris très sombre -> parchemin
t = 0.40 * taches + 0.25 * plis + 0.25 * fibre + 0.10 * grain
col = c0 + (c1 - c0) * np.clip(t, 0, 1)[..., None]
col *= (0.92 + 0.10 * grain[..., None])
tache = np.clip((taches - 0.72) / 0.2, 0, 1)[..., None]
col = col * (1 - 0.55 * tache) + np.array([0.08, 0.05, 0.04]) * 0.55 * tache
bord_t = np.clip((np.abs(taches - 0.72) < 0.012).astype(float), 0, 1)[..., None]
col = col * (1 - 0.25 * bord_t)                                                     # liseré plus sombre des taches        # taches d'humidité
fond = np.concatenate([col, np.ones((N, N, 1))], -1)
save(fond, "Carte_Fond")
M["Carte_Fond"] = dict(taille=[N, N], note="Image Tiled (raccordable), sous la carte dessinée ; ou fond d'une RawImage avec UV répétées")
# vignette (assombrit les bords de la zone de carte), étirée
V = 512; yv, xv = np.mgrid[0:V, 0:V] / (V - 1) * 2 - 1
vig = np.clip((np.maximum(np.abs(xv), np.abs(yv)) ** 2.2 + 0.25 * (xv ** 2 + yv ** 2) - 0.25) / 0.9, 0, 1) * 0.75
vg = np.zeros((V, V, 4), np.float32); vg[..., 3] = vig
save(vg, "Carte_Vignette")

# ------------------------------------------------------------------ 3. brume de la zone non découverte (raccordable) et pinceau
N2 = 512
nb = 0.55 * tnoise(N2, 21, (40,)) + 0.30 * tnoise(N2, 22, (14,)) + 0.15 * tnoise(N2, 23, (5,))
alpha = 0.82 + 0.18 * nb
c = np.array([0.030, 0.028, 0.045]) + np.array([0.11, 0.11, 0.15]) * (nb[..., None] ** 2.0)   # volutes gris-bleu
br = np.concatenate([c, alpha[..., None]], -1)
save(br, "Carte_Brume")
P = 256; yp, xp = np.mgrid[0:P, 0:P] / (P - 1) * 2 - 1; r = np.hypot(xp, yp)
edge = tnoise(P, 24, (6, 2)) * 0.25
pin = np.zeros((P, P, 4), np.float32); pin[..., :3] = 1.0
pin[..., 3] = np.clip((1.0 - r - edge * r) / 0.45, 0, 1) ** 1.5
save(pin, "Carte_Brume_Pinceau")
M["Carte_Brume"] = dict(taille=[N2, N2], note="raccordable ; masquée là où le joueur est passé (masque peint avec Carte_Brume_Pinceau dans une RenderTexture)")

# ------------------------------------------------------------------ 4. panneau de légende (9 tranches) et rose des vents
LS = 384; LB = 64
leg = np.zeros((LS, LS, 4), np.float32); leg[..., :3] = FOND / 255
d2 = np.minimum(np.minimum(np.arange(LS)[:, None], np.arange(LS)[None, :]), np.minimum(LS - 1 - np.arange(LS)[:, None], LS - 1 - np.arange(LS)[None, :]))
leg[..., 3] = np.clip((d2 - 14) / 4.0, 0, 1) * 0.86
r1 = resize(rail1, 0.6); rh1 = r1.shape[0]; m1 = 18
for x in range(0, LS, r1.shape[1]):
    over(leg, r1, x, m1 - rh1 // 2); over(leg, r1, x, LS - m1 - rh1 // 2 - 1)
r1v = np.rot90(r1).copy()
for y in range(0, LS, r1v.shape[0]):
    over(leg, r1v, m1 - rh1 // 2, y); over(leg, r1v, LS - m1 - rh1 // 2 - 1, y)
sm = resize(star, 0.30)
for (x, y) in ((m1, m1), (LS - m1 - 1, m1), (m1, LS - m1 - 1), (LS - m1 - 1, LS - m1 - 1)):
    over(leg, sm, x - sm.shape[1] // 2, y - sm.shape[0] // 2)
save(leg, "Carte_Legende_Cadre")
M["Carte_Legende_Cadre"] = dict(taille=[LS, LS], bordures=[LB, LB, LB, LB], note="Image Sliced ; icônes de la mini-carte + texte Unity dedans")
save(resize(star, 1.0), "Carte_Rose")
M["Carte_Rose"] = dict(taille=list(star.shape[:2]), note="rose des vents (étoile de l'emblème) ; tourner selon le nord de la carte")
json.dump(M, open(os.path.join(HERE, "carte_mesures.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print("ok", sorted(f for f in os.listdir(OUT) if f.endswith(".png")))
