# -*- coding: utf-8 -*-
"""Retouches des tuiles du niveau 1, sur image seulement (Python, Pillow et numpy ; hors de Blender et d'Unity).

1. Pierre (texture_mur_pierre_v01_tuile.png) : efface la grande coulure blanche qui revient sur la même pierre à
   chaque tuile (assise du bas, à cheval sur le bord droit et le bord gauche). Remplissage par diffusion depuis le bord
   du masque (périodique : la tuile reste sans couture), plus le grain d'une pierre propre de la même assise ; les traits
   sombres (joints, contours) sont gardés.
2. Eau (texture_eau_egout_v01_tuile.png) : la tuile se répétait elle-même tous les 512 px en hauteur (damier des
   taches d'écume). La moitié basse est remplacée par la moitié haute décalée de 352 px en largeur ; les deux raccords
   sont recousus par une coupe d'erreur minimale (quilting) : traits nets, pas de fondu.
3. Fer (texture_fer_rouille_v01_tuile.png) : 1024 px pour une tuile de 1 m (1024 px/m) ramenés à 256 px (256 px/m,
   la densité de la pierre : 1024 px pour 4 m). Les UV des modèles ne changent pas (tuile de 1 m).

Les fichiers sont réécrits à leur place (mêmes noms, mêmes GUID dans Unity : les matériaux suivent sans réglage).
Avant / après : docs/art/rendus/Textures/Retouche_*.jpg (2 × 2 tuiles).
    python textures_retouches.py
"""
import os, shutil
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
TEX = os.path.join(ROOT, "Assets", "Art", "Textures", "Niveau1")
OUT = os.path.join(ROOT, "docs", "art", "rendus", "Textures")


def load(name):
    return np.asarray(Image.open(os.path.join(TEX, name)).convert("RGB")).astype(np.float32) / 255.0


def save(a, name):
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8)).save(os.path.join(TEX, name), optimize=True)


def box_blur(a, r):
    """Flou boîte périodique (rayon r), axe par axe."""
    out = a.copy()
    for ax in (0, 1):
        acc = np.zeros_like(out)
        for d in range(-r, r + 1):
            acc += np.roll(out, d, axis=ax)
        out = acc / (2 * r + 1)
    return out


def dilate(m, r):
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            if dx * dx + dy * dy <= r * r:
                out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def tile2(a, size=512):
    im = Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8))
    h = int(size * im.height / im.width)
    im = im.resize((size, h), Image.LANCZOS)
    t = Image.new("RGB", (size * 2, h * 2))
    for x in (0, size):
        for y in (0, h):
            t.paste(im, (x, y))
    return t


def before_after(before, after, name, note):
    from PIL import ImageDraw, ImageFont
    a, b = tile2(before), tile2(after)
    band = 46
    out = Image.new("RGB", (a.width + b.width + 16, max(a.height, b.height) + band), (18, 17, 21))
    out.paste(a, (0, band)); out.paste(b, (a.width + 16, band))
    d = ImageDraw.Draw(out)
    try:
        f = ImageFont.truetype("C:/Windows/Fonts/arialbd.ttf", 22)
    except OSError:
        f = ImageFont.load_default()
    d.text((10, 10), "Avant (2 × 2 tuiles)", fill=(230, 220, 210), font=f)
    d.text((a.width + 26, 10), "Après : " + note, fill=(255, 150, 70), font=f)
    os.makedirs(OUT, exist_ok=True)
    out.save(os.path.join(OUT, name), quality=90)


# ----------------------------------------------------------------------------- 1. pierre
def pierre():
    name = "texture_mur_pierre_v01_tuile.png"
    a = load(name)
    mx, mn = a.max(2), a.min(2)
    lum = a.mean(2)
    white = (mx > 0.55) & ((mx - mn) < 0.16)
    H, W = white.shape
    # seulement la pierre de la coulure (assise du bas, bord droit et bord gauche) ; les reflets clairs des autres
    # pierres (arêtes) restent
    zone = np.zeros_like(white)
    zone[H * 3 // 4 - 20:, :] = True
    zone[:, 160:800] = False
    mask = dilate(white & zone, 4) & (lum > 0.16)          # garde les traits sombres (joints, contours)
    # remplissage par diffusion (périodique)
    fill = a.copy()
    m3 = mask[..., None]
    known = ~mask
    base = box_blur(np.where(m3, 0, a), 12) / np.maximum(box_blur(known[..., None].astype(np.float32), 12), 1e-3)
    fill = np.where(m3, base, a)
    for _ in range(300):
        avg = (np.roll(fill, 1, 0) + np.roll(fill, -1, 0) + np.roll(fill, 1, 1) + np.roll(fill, -1, 1)) / 4
        fill = np.where(m3, avg, a)
    # grain d'une pierre propre de la même assise (décalée de 448 px), fondu au bord du masque
    donor = np.roll(a, 448, axis=1)
    grain = donor - box_blur(donor, 4)
    soft = box_blur(mask.astype(np.float32)[..., None], 2)
    out = a * (1 - soft) + (fill + grain * 0.9) * soft
    save(out, name)
    before_after(a, out, "Retouche_Pierre.jpg", "coulure blanche effacée (%d px)" % int(mask.sum()))
    return int(mask.sum())


# ----------------------------------------------------------------------------- 2. eau
def min_cut(e, starts=12):
    """Chemin horizontal de coût minimal dans e (h × w), pas de ±1 ligne par colonne, **périodique** : il finit sur la
    ligne où il commence (la tuile se répète en largeur). On essaie les meilleures lignes de départ."""
    h, w = e.shape
    best = None
    for r in np.argsort(e[:, 0])[:starts]:
        cost = np.full(h, np.inf); cost[r] = e[r, 0]
        back = np.zeros((h, w), dtype=np.int32)
        for x in range(1, w):
            cand = np.stack([np.roll(cost, 1), cost, np.roll(cost, -1)])
            cand[0, 0] = cand[2, -1] = np.inf
            k = np.argmin(cand, axis=0)
            back[:, x] = np.arange(h) + (k - 1)        # k = 0 : venu de la ligne y - 1
            cost = cand[k, np.arange(h)] + e[:, x]
        # fin : à une ligne au plus de r (raccord avec la colonne 0 de la tuile suivante)
        ends = [y for y in (r - 1, r, r + 1) if 0 <= y < h]
        y_end = min(ends, key=lambda y: cost[y])
        if best is None or cost[y_end] < best[0]:
            path = np.zeros(w, dtype=np.int32); path[-1] = y_end
            for x in range(w - 1, 0, -1):
                path[x - 1] = back[path[x], x]
            best = (cost[y_end], path)
    return best[1]


def eau(shift=352, band=48):
    name = "texture_eau_egout_v01_tuile.png"
    a = load(name)
    H, W, _ = a.shape
    P = H // 2
    A = a[:P]                                   # une période (la moitié haute)
    B = np.roll(A, shift, axis=1)               # la même eau, décalée en largeur
    out = np.concatenate([A, B], axis=0)

    def seam(y0, upper, lower):
        """Raccord autour de la ligne y0 (période P) : au-dessus, « upper » continue ; en dessous, « lower »."""
        rows = np.arange(y0 - band, y0 + band)
        up = upper[rows % P]
        lo = lower[rows % P]
        e = ((up - lo) ** 2).sum(2)
        path = min_cut(e)
        for x in range(W):
            c = path[x]
            for i, y in enumerate(rows):
                t = np.clip((i - c) / 2.0 + 0.5, 0, 1)          # fondu de 2 px sur la coupe seulement
                out[y % H, x] = up[i, x] * (1 - t) + lo[i, x] * t
    seam(P, A, B)            # A au-dessus, B en dessous
    seam(H, B, A)            # B au-dessus, A en dessous (raccord du bord de la tuile)
    save(out, name)
    # contrôle : plus de répétition interne
    g = out.mean(2)
    rep = float(np.abs(g - np.roll(g, -P, 0)).mean() * 255)
    before_after(a, out, "Retouche_Eau.jpg", "moitié basse décalée de %d px, coutures nettes" % shift)
    return rep


# ----------------------------------------------------------------------------- 3. fer
def fer():
    name = "texture_fer_rouille_v01_tuile.png"
    a = load(name)
    im = Image.fromarray((a * 255).astype(np.uint8))
    small = im.resize((256, 256), Image.LANCZOS)
    small.save(os.path.join(TEX, name), optimize=True)
    b = np.asarray(small.resize((1024, 1024), Image.NEAREST)).astype(np.float32) / 255
    before_after(a, b, "Retouche_Fer.jpg", "256 px pour 1 m (256 px/m, comme la pierre)")
    return small.size


if __name__ == "__main__":
    print("pierre : %d px retouchés" % pierre())
    print("eau : écart moyen avec la moitié décalée de 512 px : %.1f (avant : 4.8, une image sans répétition : ≈ 50)" % eau())
    print("fer :", fer())
