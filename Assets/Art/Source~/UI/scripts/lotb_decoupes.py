"""Lot B (fonds de menu) : découpes des images générées (Higgsfield, Nano Banana Pro) en calques Unity.

Entrées : docs/art/ui/generees/*.png, docs/art/ui/maquettes/ui_menu_principal_fond_v01.png
Sorties : Assets/Art/UI/Menus/<Groupe>/<Groupe_Element>.png, docs/art/ui/entrees/entree_raccord.png
Usage   : python lotb_decoupes.py [ciel lune nuages brume feu sieges campement chateau raccord]  (rien = tout)

Conventions (UI-HANDOFF § 2) :
- « alpha » : PNG RGBA, couleurs non prémultipliées, mélange normal ;
- « additif » : PNG RGB sur noir pur, à afficher avec un matériau additif (Blend One One) : le noir n'ajoute rien.
"""
import heapq
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", "..", "..", ".."))
GEN = os.path.join(ROOT, "docs", "art", "ui", "generees")
MAQ = os.path.join(ROOT, "docs", "art", "ui", "maquettes")
OUT = os.path.join(ROOT, "Assets", "Art", "UI", "Menus")
ENT = os.path.join(ROOT, "docs", "art", "ui", "entrees")
LUM = np.array([0.299, 0.587, 0.114])


# ----------------------------------------------------------------------------- outils
def load(path):
    return np.asarray(Image.open(path).convert("RGB"), dtype=np.float64) / 255.0


def save_rgb(a, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    Image.fromarray(np.round(np.clip(a, 0, 1) * 255).astype(np.uint8), "RGB").save(path)
    return path


def save_rgba(rgb, alpha, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    a = np.concatenate([np.clip(rgb, 0, 1), np.clip(alpha, 0, 1)[..., None]], -1)
    Image.fromarray(np.round(a * 255).astype(np.uint8), "RGBA").save(path)
    return path


def blur(a, s):
    """Flou gaussien séparable (bords répliqués)."""
    if s <= 0:
        return a
    r = int(3 * s) + 1
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / s) ** 2)
    k /= k.sum()
    out = a
    for ax in (0, 1):
        pad = [(0, 0)] * out.ndim
        pad[ax] = (r, r)
        p = np.pad(out, pad, mode="edge")
        acc = np.zeros_like(out)
        for i, w in enumerate(k):
            sl = [slice(None)] * out.ndim
            sl[ax] = slice(i, i + out.shape[ax])
            acc += w * p[tuple(sl)]
        out = acc
    return out


def maxf(m, r):
    """Dilatation binaire (carré de rayon r)."""
    out = m.copy()
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            out |= np.roll(np.roll(m, dy, 0), dx, 1)
    return out


def minf(m, r):
    return ~maxf(~m, r)


def fill_holes(m):
    """Bouche les trous (zones de fond non reliées au bord)."""
    h, w = m.shape
    im = Image.fromarray(np.pad(m, 1).astype(np.uint8) * 255).copy()
    ImageDraw.floodfill(im, (0, 0), 128)
    a = np.asarray(im)[1:-1, 1:-1]
    return a != 128


def components(m, min_px=200):
    """Composantes 4-connexes (PIL floodfill), triées par taille décroissante."""
    lab = np.zeros(m.shape, np.int32)
    im = Image.fromarray(m.astype(np.uint8) * 255).copy()
    comps = []
    ys, xs = np.nonzero(m)
    k = 0
    for y, x in zip(ys, xs):
        if lab[y, x] or im.getpixel((int(x), int(y))) != 255:
            continue
        k += 1
        ImageDraw.floodfill(im, (int(x), int(y)), 100)
        sel = np.asarray(im) == 100
        lab[sel] = k
        im.paste(50, mask=Image.fromarray(sel.astype(np.uint8) * 255))
        comps.append(sel)
    comps = [c for c in comps if c.sum() >= min_px]
    return sorted(comps, key=lambda c: -c.sum())


def bbox(m, pad=0):
    ys, xs = np.nonzero(m)
    return max(ys.min() - pad, 0), ys.max() + pad + 1, max(xs.min() - pad, 0), xs.max() + pad + 1


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def unblend(rgb, alpha, bg):
    """Retire le fond uni mélangé dans les pixels de bord (couleur non prémultipliée)."""
    a = np.maximum(alpha, 1e-3)[..., None]
    return np.clip((rgb - (1 - a) * np.asarray(bg)) / a, 0, 1)


# ----------------------------------------------------------------------------- pièces
def ciel():
    """B3 : assombri (courbe gamma + gain, bleu un peu gardé). Réglage dans CIEL_*."""
    a = load(os.path.join(GEN, "ui_ciel_v01.png"))
    out = np.power(a, CIEL_GAMMA) * CIEL_GAIN
    out[..., 2] = np.power(a[..., 2], CIEL_GAMMA * 0.92) * CIEL_GAIN
    return [save_rgb(out, os.path.join(OUT, "Fond", "Fond_Ciel.png"))]


CIEL_GAMMA, CIEL_GAIN = 1.25, 0.72


def kscale(a):
    """Facteur d'échelle des réglages en pixels : 1 pour les images de 1280 px de large (premier envoi), ~2.15 en 2K."""
    return a.shape[1] / 1280.0


def lune():
    """B4 : disque (alpha) + halo (additif, reconstruit symétrique à partir du profil au-dessus du sol)."""
    a = load(os.path.join(GEN, "ui_lune_v01.png"))
    h, w, _ = a.shape
    L = a @ LUM
    disc = fill_holes(minf(maxf(L > 0.55, 2), 2))
    disc = components(disc)[0]
    ys, xs = np.nonzero(disc)
    cy, cx = ys.mean(), xs.mean()
    r = np.sqrt(disc.sum() / np.pi)
    yy, xx = np.mgrid[0:h, 0:w]
    d = np.hypot(yy - cy, xx - cx)
    # sol : premières lignes (sous le centre) où le fond hors disque n'est plus noir
    side = (d > r * 1.9)
    rows = np.array([L[y][side[y]].mean() if side[y].any() else 0 for y in range(h)])
    horizon = int(next((y for y in range(int(cy), h) if rows[y] > 0.06), h))
    # profil radial du halo (au-dessus du sol, hors disque)
    ok = (yy < horizon - 4) & (d > r + 2)
    rb = np.arange(0, int(r * 3))
    prof = np.zeros((len(rb), 3))
    for i in rb:
        m = ok & (np.abs(d - i) < 0.75)
        if m.sum() > 5:
            prof[i] = np.median(a[m], axis=0)
    for i in range(len(rb)):
        if not prof[i].any():
            prof[i] = prof[i - 1] if i else 0
    prof = prof - prof[-12:].mean(0)                       # le fond noir lointain = 0
    prof = np.clip(prof, 0, 1)
    R = int(r * 2.9)
    box = (int(cy) - R, int(cy) + R, int(cx) - R, int(cx) + R)
    sub_d = d[max(box[0], 0):box[1], max(box[2], 0):box[3]]
    # halo synthétique sur une toile carrée centrée (la lune déborde peut-être de l'image en haut)
    size = 2 * R
    gy, gx = np.mgrid[0:size, 0:size]
    gd = np.hypot(gy - R + 0.5, gx - R + 0.5)
    idx = np.clip(gd.astype(int), 0, len(prof) - 1)
    halo = prof[idx] * smoothstep(r * 2.9, r * 2.2, gd)[..., None]
    halo[gd < r - 1] = prof[int(r) + 2]                     # sous le disque : continuité
    # disque : pixels d'origine, bord anticrénelé
    disc_rgb = np.zeros((size, size, 3))
    y0, x0 = int(cy) - R, int(cx) - R
    sy0, sx0 = max(y0, 0), max(x0, 0)
    sy1, sx1 = min(y0 + size, h), min(x0 + size, w)
    disc_rgb[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = a[sy0:sy1, sx0:sx1]
    alpha = smoothstep(r + 1.0, r - 1.0, gd)
    out = [save_rgba(disc_rgb, alpha, os.path.join(OUT, "Fond", "Fond_Lune.png")),
           save_rgb(halo, os.path.join(OUT, "Fond", "Fond_Lune_Halo.png"))]
    INFO["lune"] = "disque r = %.0f px (image %dx%d), centre (%.0f, %.0f), sol à y = %d ; sprites %d px" % (r, w, h, cx, cy, horizon, size)
    return out


def nuages():
    """B5 : 5 bandes de nuages peints opaques (contour encré) : alpha plein, mélange normal."""
    a = load(os.path.join(GEN, "ui_nuages_v01.png")); K = kscale(a)
    mx = a.max(-1)
    m = fill_holes(maxf(mx > 0.07, max(1, round(2 * K))))
    comps = components(m, int(2000 * K * K))
    # regroupe par bande horizontale (centre en y)
    comps.sort(key=lambda c: np.nonzero(c)[0].mean())
    bands = []
    for c in comps:
        cyc = np.nonzero(c)[0].mean()
        if bands and abs(cyc - bands[-1][0]) < 40 * K:
            bands[-1] = (bands[-1][0], bands[-1][1] | c)
        else:
            bands.append((cyc, c))
    out = []
    for i, (_, c) in enumerate(bands, 1):
        al = blur(c.astype(float), 0.8 * K)
        al = np.clip(al * 1.3 - 0.15, 0, 1)
        y0, y1, x0, x1 = bbox(c, int(4 * K))
        out.append(save_rgba(unblend(a, al, (0, 0, 0))[y0:y1, x0:x1], al[y0:y1, x0:x1],
                             os.path.join(OUT, "Fond", "Fond_Nuages_%d.png" % i)))
    INFO["nuages"] = "%d bandes" % len(bands)
    return out


def brume():
    """B6 : 2 bandes de brume, additif ; noir nettoyé, bas de chaque bande adouci, bords gauche/droit raccordés."""
    a = load(os.path.join(GEN, "ui_brume_v01.png")); K = kscale(a)
    a = np.clip((a - 0.025) / 0.975, 0, 1)
    h, w, _ = a.shape
    row = a.max(-1).mean(1)
    on = row > 0.05
    runs, y = [], 0
    while y < h:
        if on[y]:
            y1 = y
            while y1 < h and on[y1]:
                y1 += 1
            if y1 - y > 30 * K:
                runs.append((y, y1))
            y = y1
        else:
            y += 1
    out = []
    for i, (y0, y1) in enumerate(runs, 1):
        y0 = max(y0 - int(12 * K), 0); y1 = min(y1 + int(12 * K), h)
        b = a[y0:y1].copy()
        bh = y1 - y0
        t = np.arange(bh)[:, None]
        b *= smoothstep(bh, bh * 0.55, t)[..., None]     # fondu du bas sur 45 % de la hauteur
        b *= smoothstep(0, bh * 0.12, t)[..., None]
        # raccord horizontal : fondu enchaîné sur 12 % de la largeur avec la bande décalée d'une demi-largeur
        ov = int(w * 0.12)
        ramp = smoothstep(0, ov, np.arange(w))[None, :, None]
        wrapped = np.roll(b, w // 2, axis=1)
        mix = np.minimum(ramp, ramp[:, ::-1])
        b = b * mix + wrapped * (1 - mix)
        out.append(save_rgb(b, os.path.join(OUT, "Fond", "Fond_Brume_%d.png" % i)))
    INFO["brume"] = "%d bandes : %s" % (len(runs), runs)
    return out


def feu():
    """B15 : flammes. Deux versions : alpha (contour encré gardé, mélange normal) et additif (lueur comprise)."""
    a = load(os.path.join(GEN, "ui_feu_v01.png")); K = kscale(a)
    mx = a.max(-1)
    core = fill_holes(maxf(mx > 0.30, max(1, round(3 * K))))
    core = components(core, int(500 * K * K))
    body = np.zeros_like(mx, bool)
    for c in core:
        body |= c
    al = blur(body.astype(float), 1.0 * K)
    glow = np.clip((a - 0.02) / 0.98, 0, 1)
    y0, y1, x0, x1 = bbox(mx > 0.06, int(6 * K))
    return [save_rgba(unblend(a, al, (0, 0, 0))[y0:y1, x0:x1], al[y0:y1, x0:x1], os.path.join(OUT, "Campement", "Campement_Feu.png")),
            save_rgb(glow[y0:y1, x0:x1], os.path.join(OUT, "Campement", "Campement_Feu_Additif.png"))]


def sieges():
    """B16 : fond gris #C8C8C8 ; 4 sprites de même hauteur de toile (assises alignées), gris retiré des bords."""
    a = load(os.path.join(GEN, "ui_sieges_v01.png")); K = kscale(a)
    g = np.array([200, 200, 200]) / 255.0
    bg = a.reshape(-1, 3)[np.r_[0:200, -200:0]].mean(0)       # gris mesuré (coins)
    dist = np.linalg.norm(a - bg, axis=-1)
    m = fill_holes(minf(maxf(dist > 0.07, max(1, round(2 * K))), max(1, round(2 * K))))
    comps = components(m, int(3000 * K * K))[:4]
    comps.sort(key=lambda c: np.nonzero(c)[1].mean())
    ys = np.nonzero(np.any(np.stack(comps), 0))[0]
    gy0, gy1 = ys.min() - int(4 * K), ys.max() + int(5 * K)
    names = ["Tronc", "Pierre", "Banc", "Rocher"]
    out, info = [], []
    for n, c in zip(names, comps):
        # bord : on rentre d'un pixel (le liseré anticrénelé est mélangé au gris), puis bord doux d'un pixel
        core = minf(c, max(1, round(K)))
        al = np.clip(blur(core.astype(float), 0.8 * K) * 1.15, 0, 1) * maxf(core, max(1, round(K)))
        _, _, x0, x1 = bbox(c, int(4 * K))
        out.append(save_rgba(unblend(a, al, bg)[gy0:gy1, x0:x1], al[gy0:gy1, x0:x1], os.path.join(OUT, "Campement", "Campement_Siege_%s.png" % n)))
        cy = np.nonzero(c)[0]
        info.append("%s : x %d-%d, haut %d, bas %d" % (n, x0, x1, cy.min(), cy.max()))
    INFO["sieges"] = "toile y %d-%d (commune) ; gris mesuré %s ; " % (gy0, gy1, np.round(bg * 255).astype(int)) + " ; ".join(info)
    return out


def campement():
    """B1 : fond du campement (moitié droite du panorama) tel quel ; contrôle du calage avec la passe 1."""
    a = load(os.path.join(GEN, "ui_campement_vide_v01.png"))
    return [save_rgb(a, os.path.join(OUT, "Campement", "Campement_Fond.png"))]


# ----------------------------------------------------------------------------- château
# polygone englobant (coordonnées de ui_menu_principal_fond_v01, 2752 × 1536) : château, ponts, falaise, cascades
CH_POLY = [(1150, 610), (1180, 560), (1250, 538), (1370, 515), (1385, 470), (1412, 412), (1425, 392), (1440, 420),
           (1470, 465), (1530, 440), (1563, 330), (1578, 280), (1590, 226), (1602, 280), (1622, 325), (1650, 325),
           (1666, 300), (1685, 252), (1702, 300), (1720, 305), (1745, 248), (1765, 160), (1784, 200), (1800, 82),
           (1813, 150), (1830, 195), (1850, 185), (1870, 134), (1886, 200), (1902, 258), (1912, 280), (1935, 298),
           (1950, 256), (1966, 328), (1990, 345), (2012, 420), (2050, 438), (2140, 445), (2168, 520), (2160, 760),
           (2120, 1000), (1450, 1010), (1300, 900), (1150, 760)]
CH_CASTLE_SEEDS = [(1650, 1950, 390, 520), (1560, 1610, 310, 420), (1450, 2000, 650, 850), (1390, 1440, 470, 560)]  # x0,x1,y0,y1
# flèches et tours : (x, haut, bas), germes « château » le long de l'axe (relevés sur une vue quadrillée)
CH_SPIRES = [(1805, 92, 300), (1765, 168, 280), (1870, 148, 300), (1590, 238, 540), (1685, 262, 400), (1425, 405, 560),
             (1910, 288, 430), (1950, 268, 430), (1990, 355, 450), (1660, 325, 420), (1725, 318, 400), (1838, 200, 300)]
CH_MOON = (1782, 180, 92)                   # centre x, y, rayon : disque clair = ciel
CH_FADE = (790, 1000)                       # la falaise se fond dans la brume entre ces lignes


def geodesic(cost, seeds):
    """Distance géodésique (Dijkstra, 4 voisins) depuis un masque de départ."""
    h, w = cost.shape
    dist = np.full((h, w), np.inf)
    pq = []
    for y, x in zip(*np.nonzero(seeds)):
        dist[y, x] = 0.0
        pq.append((0.0, int(y), int(x)))
    heapq.heapify(pq)
    while pq:
        d, y, x = heapq.heappop(pq)
        if d > dist[y, x]:
            continue
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w:
                nd = d + cost[ny, nx]
                if nd < dist[ny, nx]:
                    dist[ny, nx] = nd
                    heapq.heappush(pq, (nd, ny, nx))
    return dist


def chateau():
    """Château + falaise découpés dans ui_menu_principal_fond_v01 (B7/B8 abandonnées) ; masque des fenêtres rouges."""
    im = Image.open(os.path.join(MAQ, "ui_menu_principal_fond_v01.png")).convert("RGB")
    a = np.asarray(im, dtype=np.float64) / 255.0
    H, W, _ = a.shape
    poly = Image.new("L", (W, H), 0)
    ImageDraw.Draw(poly).polygon(CH_POLY, fill=255)
    poly = np.asarray(poly) > 0
    y0, y1, x0, x1 = bbox(poly, 8)
    sub = a[y0:y1, x0:x1]
    # segmentation à mi-résolution : coût = contours (gradient de luminance + couleur)
    s = 2
    small = np.asarray(Image.fromarray((sub * 255).astype(np.uint8)).resize(((x1 - x0) // s, (y1 - y0) // s), Image.LANCZOS), dtype=np.float64) / 255
    L = small @ LUM
    gy, gx = np.gradient(blur(L, 0.7))
    cg = [np.gradient(blur(small[..., k], 0.7)) for k in range(3)]
    grad = np.hypot(gx, gy) + 0.5 * sum(np.hypot(*c) for c in cg)
    cost = 1.0 + 60.0 * grad / np.percentile(grad, 99)
    ph, pw = L.shape
    spoly = np.asarray(Image.fromarray(poly[y0:y1, x0:x1].astype(np.uint8) * 255).resize((pw, ph))) > 127
    zone = maxf(spoly, 6)                                         # bande d'affinage autour du polygone
    castle_seed = np.zeros((ph, pw), bool)
    for (cx0, cx1, cy0, cy1) in CH_CASTLE_SEEDS:
        castle_seed[(cy0 - y0) // s:(cy1 - y0) // s, (cx0 - x0) // s:(cx1 - x0) // s] = True
    for (sx, st, sb) in CH_SPIRES:
        castle_seed[(st - y0) // s:(sb - y0) // s, (sx - x0) // s - 1:(sx - x0) // s + 1] = True
    sky_seed = ~maxf(spoly, 6)                                    # hors du polygone (+12 px) = ciel / arbres
    gy_, gx_ = np.mgrid[0:ph, 0:pw]
    moon = np.hypot(gy_ * s + y0 - CH_MOON[1], gx_ * s + x0 - CH_MOON[0]) < CH_MOON[2]
    sky_seed |= moon & (L > 0.45) & ~castle_seed                   # disque de la lune (les flèches devant restent sombres)
    bluesky = (L > 0.30) & (small[..., 2] - small[..., 0] > 0.28) & (gy_ * s + y0 < 430)   # ciel clair au-dessus des toits
    sky_seed |= bluesky & ~maxf(castle_seed, 3)
    d_c = geodesic(cost, castle_seed)
    d_s = geodesic(cost, sky_seed)
    lab = (d_c < d_s) & zone
    lab = fill_holes(lab)
    lab = components(lab, 50)[0]
    # retour pleine résolution, bord doux
    m = np.asarray(Image.fromarray(lab.astype(np.uint8) * 255).resize((x1 - x0, y1 - y0), Image.BILINEAR), dtype=np.float64) / 255
    m = blur(m, 1.0)
    m = smoothstep(0.35, 0.65, m)
    yy = np.arange(y0, y1)[:, None]
    m *= smoothstep(CH_FADE[1], CH_FADE[0], yy)
    # fenêtres : rouge / magenta lumineux (r > b, r >> g)
    r, g, b = sub[..., 0], sub[..., 1], sub[..., 2]
    win = smoothstep(0.04, 0.14, r - g) * smoothstep(-0.08, 0.04, r - 0.6 * b) * smoothstep(0.14, 0.26, r)
    win = blur(win, 0.8) * (m > 0.2)
    out = [save_rgba(sub, m, os.path.join(OUT, "Fond", "Fond_Chateau.png")),
           save_rgb(np.repeat(np.clip(win, 0, 1)[..., None], 3, -1), os.path.join(OUT, "Fond", "Fond_Chateau_Fenetres.png"))]
    INFO["chateau"] = "découpe x %d-%d, y %d-%d dans le fond 2752×1536 (position du sprite dans le panorama : coin haut gauche (%d, %d))" % (x0, x1, y0, y1, x0, y0)
    return out


# ----------------------------------------------------------------------------- entrée de B2
def raccord():
    """entree_raccord.png (2752 × 1536) : moitié droite du fond du menu | bande grise | moitié gauche de B1 (agrandie)."""
    menu = Image.open(os.path.join(MAQ, "ui_menu_principal_fond_v01.png")).convert("RGB")
    W, H = menu.size
    camp = Image.open(os.path.join(GEN, "ui_campement_vide_v01.png")).convert("RGB").resize((W, H), Image.LANCZOS)
    half = W // 2
    out = Image.new("RGB", (W, H))
    out.paste(menu.crop((W - half, 0, W, H)), (0, 0))
    out.paste(camp.crop((0, 0, W - half, H)), (half, 0))
    band = RACCORD_BAND
    ImageDraw.Draw(out).rectangle((half - band // 2, 0, half + band // 2 - 1, H), fill=(128, 128, 128))
    os.makedirs(ENT, exist_ok=True)
    p = os.path.join(ENT, "entree_raccord.png")
    out.save(p)
    INFO["raccord"] = "bande grise x %d-%d (largeur %d) ; à gauche : x %d-%d du fond du menu ; à droite : x 0-%d de B1 agrandi à %dx%d" % (
        half - band // 2, half + band // 2, band, W - half, W, W - half, W, H)
    return [p]


RACCORD_BAND = 560
INFO = {}
STEPS = dict(ciel=ciel, lune=lune, nuages=nuages, brume=brume, feu=feu, sieges=sieges, campement=campement,
             chateau=chateau, raccord=raccord)

if __name__ == "__main__":
    for k in (sys.argv[1:] or list(STEPS)):
        for p in STEPS[k]():
            print(os.path.relpath(p, ROOT))
    for k, v in INFO.items():
        print("[%s] %s" % (k, v))
