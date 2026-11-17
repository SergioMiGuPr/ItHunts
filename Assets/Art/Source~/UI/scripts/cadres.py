# Découpe des cadres du HUD : barre de vie et cartouche des âmes en 3 parties (+ version entière à bordures),
# fond et remplissage, cadre de la mini-carte (+ masque et fond), flamme des âmes. Écrit aussi mesures.json.
import json, numpy as np
from PIL import Image, ImageDraw
from uilib import *
M = "C:/Users/PC/ItHunts/ItHunts-art/docs/art/ui/maquettes/"
O = "out/"
BORD = {}
FOND_RGB = np.array([14, 11, 18], np.float32)    # #0E0B12


def painted_fill(mask, top, mid, bot, seed, hi=None):
    """Remplissage peint : dégradé vertical sur la hauteur du masque, grain et traînées horizontales."""
    H, W = mask.shape
    ys = np.nonzero(mask.any(1))[0]
    y0, y1 = ys.min(), ys.max()
    t = np.clip((np.arange(H) - y0) / max(1, y1 - y0), 0, 1)[:, None, None]
    c = np.where(t < 0.45, top + (mid - top) * (t / 0.45), mid + (bot - mid) * ((t - 0.45) / 0.55))
    c = np.broadcast_to(c, (H, W, 3)).astype(np.float32).copy()
    n = noise(H, W, seed, (3, 9))
    streak = noise(H, max(2, W // 40), seed + 1, (2,))
    streak = np.array(Image.fromarray(streak, "F").resize((W, H), Image.BICUBIC))
    c *= (0.86 + 0.22 * n[..., None]) * (0.92 + 0.14 * streak[..., None])
    if hi is not None:  # liseré de lumière en haut du remplissage
        band = np.exp(-((np.arange(H) - (y0 + 0.16 * (y1 - y0))) / max(1.0, 0.06 * (y1 - y0))) ** 2)[:, None, None]
        c = c + (hi - c) * band * 0.55
    return np.clip(c, 0, 255)


def layers(src, inner_pts):
    """Silhouette pleine, intérieur (trous fermés par le cadre) et composantes de l'intérieur aux points (y, x)."""
    A = src[..., 3] > 0.5
    sil = fill_holes(A)
    interior = sil & ~A
    comps = []
    for (y, x) in inner_pts:
        im = Image.fromarray(interior.astype(np.uint8) * 255, "L").copy()
        ImageDraw.floodfill(im, (int(x), int(y)), 77)
        comps.append(np.array(im) == 77)
    return sil, interior, comps


def fond_layer(sil, interior, alpha=0.86, seed=3):
    m = dilate(interior, 4) & sil  # passe sous le bord intérieur du cadre
    H, W = m.shape
    rgb = np.broadcast_to(FOND_RGB, (H, W, 3)).astype(np.float32) * (0.85 + 0.3 * noise(H, W, seed, (4, 16))[..., None])
    a = soft(m, 1.0) * m * alpha
    return np.dstack([rgb, a])


def cut3(arr, c1, c2, tile_x, tile_w, k, name, scale):
    """Embleme [0,c1) / Milieu (segment raccordable, Tiled) / Bout [c2,W), plus la version entière à bordures."""
    parts = {"Embleme": arr[:, :c1], "Milieu": tileable(arr, tile_x, tile_w, k), "Bout": arr[:, c2:]}
    sizes = {}
    for p, a in parts.items():
        r = resize_rgba(a, scale)
        save_f(r, O + "HUD/%s_%s.png" % (name, p))
        sizes[p] = [r.shape[1], r.shape[0]]
    full = resize_rgba(arr, scale)
    save_f(full, O + "HUD/%s.png" % name)
    L = sizes["Embleme"][0]
    R = sizes["Bout"][0]
    BORD["HUD/%s.png" % name] = [L, 0, R, 0]  # Unity spriteBorder : x=gauche, y=bas, z=droite, w=haut
    sizes["entier"] = [full.shape[1], full.shape[0]]
    return sizes


TAILLES = {}

# ---------- Barre de vie ----------
S_VIE = 0.40
src = to_float(Image.open(M + "ui_barre_vie_v01.png"))
x0, y0, x1, y1 = bbox(src[..., 3] > 0.02, 2)
src = src[y0:y1, x0:x1]
sil, interior, (canal,) = layers(src, [(360 - y0, 1200 - x0)])
c1, c2 = 620 - x0, 1900 - x0
TAILLES["BarreVie_Cadre"] = cut3(src, c1, c2, 700 - x0, 320, 48, "BarreVie_Cadre", S_VIE)
fond = fond_layer(sil, interior)
TAILLES["BarreVie_Fond"] = cut3(fond, c1, c2, 700 - x0, 320, 48, "BarreVie_Fond", S_VIE)
cm = dilate(canal, 4) & sil
cx0, cy0, cx1, cy1 = bbox(cm, 0)
cm_c = cm[cy0:cy1, cx0:cx1]
for nom, top, mid, bot, hi in [("BarreVie_Remplissage", (196, 30, 38), (138, 10, 20), (62, 4, 10), (240, 120, 110)),
                               ("BarreVie_Remplissage_Retard", (236, 214, 206), (200, 176, 170), (150, 126, 124), (255, 245, 240))]:
    rgb = painted_fill(cm_c, np.array(top, np.float32), np.array(mid, np.float32), np.array(bot, np.float32), 11, np.array(hi, np.float32))
    arr = np.dstack([rgb, soft(cm_c, 0.8) * cm_c])
    r = resize_rgba(arr, S_VIE)
    save_f(r, O + "HUD/%s.png" % nom)
    cols = cm_c.sum(0)
    full_h = cols.max()
    l = int(np.argmax(cols >= full_h * 0.98))
    rr = int(np.argmax(cols[::-1] >= full_h * 0.98))
    BORD["HUD/%s.png" % nom] = [round(l * S_VIE) + 1, 0, round(rr * S_VIE) + 1, 0]
    TAILLES[nom] = [r.shape[1], r.shape[0]]
CANAL_VIE = {"x": round(cx0 * S_VIE), "y_haut": round(cy0 * S_VIE), "l": round((cx1 - cx0) * S_VIE), "h": round((cy1 - cy0) * S_VIE)}

# ---------- Cartouche des âmes ----------
S_AME = 0.30
src = to_float(Image.open(M + "ui_cadre_ames_v01.png"))
x0, y0, x1, y1 = bbox(src[..., 3] > 0.02, 2)
src = src[y0:y1, x0:x1]
# quatre-feuilles blancs opaques (défaut de génération) -> repeints en creux sombre, comme sur la mini-carte
val = src[..., :3].max(-1)
sat = (val - src[..., :3].min(-1)) / np.maximum(val, 1)
blanc = np.zeros(val.shape, bool)
for c in components((val > 230) & (sat < 0.08) & (src[..., 3] > 0.5), 400):
    yy, xx = np.nonzero(c); w, h = np.ptp(xx) + 1, np.ptp(yy) + 1
    if c.sum() < 1500 and 20 <= w <= 55 and 20 <= h <= 55: blanc |= fill_holes(c)
blanc = dilate(blanc, 2) & (src[..., 3] > 0.5)
src[blanc, :3] = np.array([24, 18, 28], np.float32)
sil, interior, (rond, rect) = layers(src, [(360 - y0, 425 - x0), (360 - y0, 1200 - x0)])
c1, c2 = 700 - x0, 1780 - x0
TAILLES["Ames_Cadre"] = cut3(src, c1, c2, 900 - x0, 320, 48, "Ames_Cadre", S_AME)
fond = fond_layer(sil, interior)
TAILLES["Ames_Fond"] = cut3(fond, c1, c2, 900 - x0, 320, 48, "Ames_Fond", S_AME)
ry, rx = np.nonzero(rond)
ty, tx = np.nonzero(rect)
ZONES_AME = {"rond_centre": [round(rx.mean() * S_AME), round(ry.mean() * S_AME)], "rond_diam": round((rx.max() - rx.min()) * S_AME),
             "texte_x0": round(tx.min() * S_AME), "texte_y0": round(ty.min() * S_AME), "texte_h": round((ty.max() - ty.min()) * S_AME),
             "quatrefeuilles_px": int(blanc.sum())}

# ---------- Mini-carte ----------
S_MC = 0.50
src = to_float(Image.open(M + "ui_cadre_minicarte_v01.png"))
x0, y0, x1, y1 = bbox(src[..., 3] > 0.02, 2)
src = src[y0:y1, x0:x1]
A = src[..., 3] > 0.5
H, W = A.shape
sil, interior, (trou,) = layers(src, [(H // 2, W // 2)])
ty, tx = np.nonzero(trou)
cy, cx = ty.mean(), tx.mean()
fy, fx = np.nonzero(A)
r_in = np.sqrt((fx - cx) ** 2 + (fy - cy) ** 2).min()
# bord intérieur de l'anneau (hors ornements) : médiane, sur 360 directions, de la distance au premier pixel du cadre
ray = []
for ang in np.linspace(0, 2 * np.pi, 360, endpoint=False):
    for rr in np.arange(r_in, r_in * 1.6, 0.5):
        px, py = int(cx + rr * np.cos(ang)), int(cy + rr * np.sin(ang))
        if A[py, px]: ray.append(rr); break
r_ring = float(np.median(ray))
save_f(resize_rgba(src, S_MC), O + "MiniCarte/MiniCarte_Cadre.png")
yy, xx = np.mgrid[0:H, 0:W]
d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
r_mask = r_ring + 8  # 8 px source sous le bord intérieur de l'anneau
disc = np.clip(r_mask - d + 0.5, 0, 1).astype(np.float32)
save_f(resize_rgba(np.dstack([np.full((H, W, 3), 255, np.float32), disc]), S_MC), O + "MiniCarte/MiniCarte_Masque.png")
rgb = np.broadcast_to(FOND_RGB, (H, W, 3)).astype(np.float32) * (0.85 + 0.3 * noise(H, W, 5, (4, 16))[..., None])
vign = np.clip(1 - (d / r_mask) ** 3 * 0.5, 0, 1)
save_f(resize_rgba(np.dstack([rgb * vign[..., None], disc * 0.82]), S_MC), O + "MiniCarte/MiniCarte_Fond.png")
MC = {"cadre": [round(W * S_MC), round(H * S_MC)], "centre_trou": [round(cx * S_MC, 1), round(cy * S_MC, 1)],
      "rayon_interieur_ornements": round(r_in * S_MC, 1), "rayon_interieur_anneau": round(r_ring * S_MC, 1), "rayon_masque": round(r_mask * S_MC, 1)}

# ---------- Flamme des âmes ----------
src = to_float(Image.open(M + "ui_flamme_ames_v01.png"))
x0, y0, x1, y1 = bbox(src[..., 3] > 0.02, 4)
src = src[y0:y1, x0:x1]
fl = resize_rgba(src, 256 / src.shape[0])
save_f(fl, O + "HUD/Ames_Flamme.png")
TAILLES["Ames_Flamme"] = [fl.shape[1], fl.shape[0]]
R = {"bordures": BORD, "tailles": TAILLES, "canal_vie": CANAL_VIE, "zones_ames": ZONES_AME, "minicarte": MC}
json.dump(R, open(O + "mesures.json", "w"), indent=1)
print(json.dumps(R, indent=1))
