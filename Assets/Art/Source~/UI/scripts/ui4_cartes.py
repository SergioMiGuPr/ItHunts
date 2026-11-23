# -*- coding: utf-8 -*-
"""Lot 6 de la nuit 4 : un seul design pour la mini-carte (bulle) et la carte ouverte (Tab), et deux icônes nouvelles.

- Carte_Fond : même fond que la bulle (bleu nuit presque noir, marbrure peinte très douce) au lieu du parchemin brun.
- Carte_Cadre : mêmes filets de fer lavande, les rosaces des coins remplacées par l'ornement épineux de la bulle
  (MiniCarte_Cadre, pointe cardinale), tourné vers l'extérieur ; découpe 9 tranches inchangée (bord de 260 px).
- MiniCarte_Goule : petit point rouge sang cerné d'encre (64 px), lisible sur le fond sombre, sans halo.
- MiniCarte_Traqueur : symbole du traqueur : œil fendu rouge braise dans une couronne de six épines d'encre, cerné de noir,
  halo sombre (128 px) : inquiétant et lisible à 24 px.
Les GUID des deux textures de carte sont gardés (contenu remplacé) : la carte de main suit sans câblage.
    python -I ui4_cartes.py
"""
import os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
MC = os.path.join(ROOT, "Assets", "Art", "UI", "MiniCarte")
CA = os.path.join(ROOT, "Assets", "Art", "UI", "Carte")


def fond(n=1024, seed=7):
    """Fond de carte : moyenne et teinte du fond de la bulle, marbrure douce périodique."""
    ref = np.asarray(Image.open(os.path.join(MC, "MiniCarte_Fond.png")).convert("RGBA")).astype(np.float32)
    m = ref[..., 3] > 200
    base = ref[..., :3][m].mean(0)
    rng = np.random.default_rng(seed)
    y, x = np.mgrid[0:n, 0:n] / n
    v = np.zeros((n, n), np.float32)
    for f in (1, 2, 3, 5, 8):
        for _ in range(3):
            fx, fy = rng.integers(-f, f + 1, 2)
            if fx == 0 and fy == 0: continue
            v += np.cos(2 * np.pi * (fx * x + fy * y) + rng.uniform(0, 6.28)) / f
    v = (v - v.min()) / (v.max() - v.min())
    v = np.round(v * 3) / 3                       # aplats peints (3 tons)
    v = np.asarray(Image.fromarray((v * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(6))).astype(np.float32) / 255
    rgb = base[None, None, :] * (0.85 + 0.3 * v[..., None])
    return Image.fromarray(np.clip(rgb, 0, 255).astype(np.uint8), "RGB").convert("RGBA")


def pointe_bulle():
    """Ornement épineux du haut de la bulle (MiniCarte_Cadre), isolé."""
    im = Image.open(os.path.join(MC, "MiniCarte_Cadre.png")).convert("RGBA")
    W, H = im.size
    return im.crop((W // 2 - 70, 0, W // 2 + 70, 130))


def cadre():
    src = Image.open(os.path.join(CA, "Carte_Cadre.png")).convert("RGBA")
    a = np.asarray(src).astype(np.float32)
    N = a.shape[0]
    p = pointe_bulle()
    out = src.copy()
    d = ImageDraw.Draw(out)
    # efface les rosaces (zone carrée autour du croisement des filets), en gardant les filets
    for (cx, cy, ang) in ((128, 128, 45), (N - 128, 128, -45), (128, N - 128, 135), (N - 128, N - 128, -135)):
        r = 126
        # filets horizontaux (pris au milieu du bord haut ou bas) et verticaux (milieu du bord gauche ou droit), sans rosace
        h = a[cy - r:cy + r, N // 2 - r:N // 2 + r]
        v = a[N // 2 - r:N // 2 + r, cx - r:cx + r]
        comp = np.maximum(h, v)
        out.paste(Image.fromarray(comp.astype(np.uint8), "RGBA"), (cx - r, cy - r))
        orn = p.rotate(ang, resample=Image.BICUBIC, expand=True)
        s = 1.35
        orn = orn.resize((int(orn.width * s), int(orn.height * s)), Image.LANCZOS)
        out.alpha_composite(orn, (cx - orn.width // 2, cy - orn.height // 2))
    return out


def goule(n=64):
    k = 4; N = n * k
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0)); d = ImageDraw.Draw(im)
    r = N * 0.26
    d.ellipse((N / 2 - r - 7 * k / 2, N / 2 - r - 7 * k / 2, N / 2 + r + 7 * k / 2, N / 2 + r + 7 * k / 2), fill=(12, 8, 10, 255))
    d.ellipse((N / 2 - r, N / 2 - r, N / 2 + r, N / 2 + r), fill=(150, 26, 24, 255))
    d.ellipse((N / 2 - r * 0.55, N / 2 - r * 0.75, N / 2 + r * 0.1, N / 2 - r * 0.1), fill=(196, 58, 44, 255))   # reflet peint
    return im.resize((n, n), Image.LANCZOS)


def traqueur(n=128):
    k = 4; N = n * k; c = N / 2
    im = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    halo = Image.new("L", (N, N), 0); dh = ImageDraw.Draw(halo)
    dh.ellipse((c - N * 0.47, c - N * 0.47, c + N * 0.47, c + N * 0.47), fill=150)
    halo = halo.filter(ImageFilter.GaussianBlur(N * 0.06))
    im.paste(Image.new("RGBA", (N, N), (6, 2, 4, 255)), (0, 0), halo)
    d = ImageDraw.Draw(im)
    # couronne de six épines (encre), légèrement irrégulières
    for i in range(6):
        a = math.radians(-90 + i * 60 + (8 if i % 2 else -6))
        l = N * (0.46 if i % 2 == 0 else 0.40)
        w = N * 0.075
        tip = (c + l * math.cos(a), c + l * math.sin(a))
        b1 = (c + N * 0.2 * math.cos(a + 0.35), c + N * 0.2 * math.sin(a + 0.35))
        b2 = (c + N * 0.2 * math.cos(a - 0.35), c + N * 0.2 * math.sin(a - 0.35))
        d.polygon([tip, b1, (c, c), b2], fill=(8, 4, 6, 255))
        # épine pleine, encre noire, flanc éclairé rouge sombre
        t2 = (c + (l - 6 * k) * math.cos(a), c + (l - 6 * k) * math.sin(a))
        m1 = (c + N * 0.17 * math.cos(a + 0.3), c + N * 0.17 * math.sin(a + 0.3))
        m2 = (c + N * 0.17 * math.cos(a - 0.3), c + N * 0.17 * math.sin(a - 0.3))
        d.polygon([t2, m1, (c, c), m2], fill=(66, 22, 26, 255))
        d.polygon([t2, m1, (c, c)], fill=(104, 34, 34, 255))
    # œil en amande
    ew, eh = N * 0.36, N * 0.17
    pts = [(c + ew * math.cos(t), c + eh * math.sin(t) * (1 - 0.25 * abs(math.cos(t)))) for t in np.linspace(0, 2 * math.pi, 64)]
    d.polygon(pts, fill=(10, 4, 6, 255))
    pts2 = [(c + (ew - 4 * k) * math.cos(t), c + (eh - 4 * k) * math.sin(t) * (1 - 0.25 * abs(math.cos(t)))) for t in np.linspace(0, 2 * math.pi, 64)]
    d.polygon(pts2, fill=(132, 26, 18, 255))
    # iris plus chaud au centre, pupille fendue verticale
    d.ellipse((c - eh * 0.75, c - eh * 0.75, c + eh * 0.75, c + eh * 0.75), fill=(196, 64, 26, 255))
    d.polygon([(c, c - eh * 0.95), (c + N * 0.025, c), (c, c + eh * 0.95), (c - N * 0.025, c)], fill=(8, 2, 4, 255))
    d.ellipse((c - eh * 0.55, c - eh * 0.6, c - eh * 0.3, c - eh * 0.35), fill=(240, 160, 110, 255))   # éclat
    return im.resize((n, n), Image.LANCZOS)


if __name__ == "__main__":
    fond().save(os.path.join(CA, "Carte_Fond.png"))
    cadre().save(os.path.join(CA, "Carte_Cadre.png"))
    goule().save(os.path.join(MC, "MiniCarte_Goule.png"))
    traqueur().save(os.path.join(MC, "MiniCarte_Traqueur.png"))
    print("ok")
