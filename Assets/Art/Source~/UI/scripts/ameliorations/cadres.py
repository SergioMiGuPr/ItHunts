# -*- coding: utf-8 -*-
"""Lot 5 de la nuit 4 : cadres de rang des améliorations (Bronze, Argent, Or, Diamant, Mythique), carrés (améliorations) et
losanges (passifs), extraits des planches générées sur fond blanc (docs/art/ameliorations/sources/cadres_*.png).

Alpha : opaque hors du blanc presque pur, transition douce entre 0.80 et 0.96 de min(R, G, B), couleur démêlée du blanc au bord ; l'intérieur blanc devient
transparent, les halos (mythique) gardent leur teinte en semi-transparence. Chaque cadre est isolé (composantes connexes),
recadré au carré et réduit à 512 px. Sorties : Assets/Art/UI/Ameliorations/Cadres/Cadre_<Rang>.png et
Cadre_Passif_<Rang>.png, puis une planche de contrôle (icônes dans leurs cadres à 64 et 128 px sur le fond du HUD).
    python -I cadres.py
"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", "..", ".."))
SRC = os.path.join(ROOT, "docs", "art", "ameliorations", "sources")
OUT = os.path.join(ROOT, "Assets", "Art", "UI", "Ameliorations", "Cadres")
ICO = os.path.join(ROOT, "Assets", "Art", "UI", "Ameliorations", "Icones")
RANGS = ["Bronze", "Argent", "Or", "Diamant", "Mythique"]


def extrait(path, rangee_haute=True):
    a = np.asarray(Image.open(path).convert("RGB")).astype(np.float32) / 255.0
    H, W, _ = a.shape
    if rangee_haute:
        # planche des carrés : deux rangées ; on garde la première (même dessin, la seconde est une variante)
        bas = np.where((1 - a.min(2)) > 0.12)[0]
        a = a[: H // 2] if bas.max() > H * 0.55 else a
    H = a.shape[0]
    # alpha : opaque sur tout ce qui n'est pas du blanc presque pur (le métal gris reste opaque) ; transition douce entre
    # 0.80 et 0.96 de la plus faible composante (bords anticrénelés, halo du mythique)
    mn = a.min(2)
    t = np.clip((mn - 0.80) / 0.16, 0, 1)
    al = 1 - t * t * (3 - 2 * t)
    # alpha resserré : plus de liseré clair ni de fumée blanche (relecture) ; bord de 1 à 2 px
    al = np.clip((al - 0.55) / 0.4, 0, 1)
    # couleur du bord : celle du métal opaque voisin (décontamination du blanc), par diffusion depuis l'intérieur
    plein = al > 0.97
    col = a.copy()
    acc = a * plein[..., None]; kn = plein.astype(np.float32)
    for _ in range(8):
        sa = np.asarray(Image.fromarray((np.clip(acc, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(2))).astype(np.float32) / 255
        sk = np.asarray(Image.fromarray((kn * 255).astype(np.uint8)).filter(ImageFilter.BoxBlur(2))).astype(np.float32) / 255
        nouv = (kn < 0.5) & (sk > 0.02)
        acc[nouv] = sa[nouv] / sk[nouv][:, None]
        kn = np.where(nouv, 1.0, kn)
    col = np.where(plein[..., None], a, acc)
    # cadres : colonnes occupées, séparées par des colonnes vides
    occ = (al > 0.3).sum(0) > 8          # seuil strict : les pointes de deux losanges voisins se frôlent
    segs, x = [], 0
    while x < W:
        if occ[x]:
            y = x
            while y < W and occ[y]:
                y += 1
            if y - x > W * 0.04:
                segs.append((x, y))
            x = y
        x += 1
    out = []
    for (x0, x1) in segs[:5]:
        rows = np.where((al[:, x0:x1] > 0.08).sum(1) > 3)[0]
        y0, y1 = rows.min(), rows.max() + 1
        cx, cy, s = (x0 + x1) // 2, (y0 + y1) // 2, int(max(x1 - x0, y1 - y0) * 1.04)
        al2 = al.copy(); al2[:, :max(0, x0 - 2)] = 0; al2[:, x1 + 2:] = 0     # pas de pointe du cadre voisin
        rgba = np.dstack([col, al2])
        pad = np.zeros((H + 2 * s, W + 2 * s, 4), np.float32)
        pad[s:s + H, s:s + W] = rgba
        c = pad[cy + s - s // 2: cy + s + s // 2, cx + s - s // 2: cx + s + s // 2]
        im = Image.fromarray((c * 255 + 0.5).astype(np.uint8), "RGBA")
        bb = im.getchannel("A").point(lambda v: 255 if v > 40 else 0).getbbox()
        im = im.crop(bb)
        side = max(im.size)
        sq = Image.new("RGBA", (side, side), (0, 0, 0, 0)); sq.paste(im, ((side - im.width) // 2, (side - im.height) // 2))
        im = Image.new("RGBA", (512, 512), (0, 0, 0, 0))
        im.alpha_composite(sq.resize((496, 496), Image.LANCZOS), (8, 8))   # même emprise pour les 5 rangs (HUD sans saut)
        out.append(im)
    return out


def ouverture(cadre):
    """Rayon intérieur utile (fraction) : zone transparente au centre du cadre."""
    a = np.asarray(cadre)[..., 3]
    n = a.shape[0]; c = n // 2
    r = 0
    while r < c and a[c, c + r] < 40 and a[c + r, c] < 40:
        r += 1
    return r / n


def gemmes(cadre, n, couleur, passif):
    """Repère de forme du rang (lisible sans la couleur) : n petites gemmes serties sous le cadre (1 bronze … 5 mythique)."""
    from PIL import ImageDraw
    im = cadre.copy(); d = ImageDraw.Draw(im)
    W = im.width; r = 24; pas = 50
    y = W - 30 if not passif else W - 92
    x0 = W / 2 - (n - 1) * pas / 2
    for i in range(n):
        x = x0 + i * pas
        pts = [(x, y - r), (x + r * 0.8, y), (x, y + r), (x - r * 0.8, y)]
        d.polygon([(px + (2 if j % 2 else 0), py) for j, (px, py) in enumerate(pts)], fill=(10, 8, 10, 255))
        d.polygon([(x, y - r + 4), (x + r * 0.8 - 4, y), (x, y + r - 4), (x - r * 0.8 + 4, y)], fill=couleur)
        d.polygon([(x, y - r + 4), (x - r * 0.8 + 4, y), (x, y)], fill=tuple(min(255, int(v * 1.35)) for v in couleur[:3]) + (255,))
    return im


GEMMES = {"Bronze": (150, 92, 52, 255), "Argent": (176, 176, 184, 255), "Or": (214, 170, 72, 255),
          "Diamant": (150, 205, 235, 255), "Mythique": (186, 82, 214, 255)}


def masque_losange(n=512, f=0.62):
    from PIL import ImageDraw
    m = Image.new("RGBA", (n, n), (0, 0, 0, 0)); d = ImageDraw.Draw(m)
    s = n * f / 2; c = n / 2
    d.polygon([(c, c - s), (c + s, c), (c, c + s), (c - s, c)], fill=(255, 255, 255, 255))
    return m


def main():
    os.makedirs(OUT, exist_ok=True)
    carres = extrait(os.path.join(SRC, "cadres_carres.png"), True)
    losanges = extrait(os.path.join(SRC, "cadres_losanges.png"), False)
    assert len(carres) == 5 and len(losanges) == 5, (len(carres), len(losanges))
    carres = [gemmes(c, i + 1, GEMMES[r], False) for i, (r, c) in enumerate(zip(RANGS, carres))]
    losanges = [gemmes(l, i + 1, GEMMES[r], True) for i, (r, l) in enumerate(zip(RANGS, losanges))]
    for r, c, l in zip(RANGS, carres, losanges):
        c.save(os.path.join(OUT, "Cadre_%s.png" % r))
        l.save(os.path.join(OUT, "Cadre_Passif_%s.png" % r))
    masque_losange().save(os.path.join(OUT, "Masque_Passif_Losange.png"))
    print("ouverture carré %.3f, losange %.3f" % (ouverture(carres[0]), ouverture(losanges[0])))
    # planche de contrôle : 5 rangs x (amélioration, passif) à 128 et à 64 px, sur le fond sombre du HUD
    exemples = ["G1", "R1", "C6", "K4", "H6"]
    passifs = ["R_P1", "C_P2", "K_P1", "H_P1", "R_P2"]
    o = Image.new("RGB", (5 * 300 + 20, 330), (14, 13, 16))
    for k, r in enumerate(RANGS):
        for j, (ic, cad, f) in enumerate(((exemples[k], carres[k], 0.80), (passifs[k], losanges[k], 0.62))):
            for t, (taille, y) in enumerate(((128, 20), (64, 190))):
                base = Image.new("RGBA", (taille, taille), (0, 0, 0, 0))
                ico = Image.open(os.path.join(ICO, "ICO_%s.png" % ic)).convert("RGBA")
                si = int(taille * f)
                ico = ico.resize((si, si), Image.LANCZOS)
                if j == 1:   # passif : l'illustration est découpée en losange
                    m = Image.new("L", (si, si), 0)
                    from PIL import ImageDraw
                    ImageDraw.Draw(m).polygon([(si / 2, 0), (si, si / 2), (si / 2, si), (0, si / 2)], fill=255)
                    ico.putalpha(m)
                base.alpha_composite(ico, ((taille - si) // 2, (taille - si) // 2))
                base.alpha_composite(cad.resize((taille, taille), Image.LANCZOS))
                o.paste(base, (20 + k * 300 + j * 140, y), base)
    o.save(os.path.join(ROOT, "docs", "art", "ameliorations", "planche_cadres.png"))


if __name__ == "__main__":
    main()
