# -*- coding: utf-8 -*-
"""Lot 0 de la nuit 4 (2026-11-22) : intègre les 5 textures peintes fournies (Higgsfield, Nano Banana) dans les tuiles du jeu.

Entrées : docs/art/textures/sources/tex_*_v01.png (2048 px, fournies par l'utilisateur).
Étapes : raccord sans couture (raccord.py) -> docs/art/textures/raccord/ ; puis tuiles du jeu (1024 px, RVB = couleur, alpha =
hauteur pour le mélange en hauteur du shader ItHunts/Toon en mode décor) qui remplacent le contenu des T_Mat_* procéduraux (mêmes
GUID : tous les matériaux qui les utilisent suivent).
  - Ecorce, Gravier, HerbeSeche, Tapis : la texture raccordée, réduite.
  - Tenture : le motif de la tapisserie, assombri, avec des plis verticaux peints en aplats (4 plis par tuile).
  - Marbre : dalles de 50 cm en damier (marbre clair / marbre sombre), joints encrés : prises à des endroits différents de la
    texture, tournées : plus de grandes veines diagonales qui se répètent.
Lancer : python -I nuit4_textures.py
"""
import os, sys
sys.dont_write_bytecode = True
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np
from PIL import Image, ImageFilter
import raccord as R

ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
SRC = os.path.join(ROOT, "docs", "art", "textures", "sources")
RAC = os.path.join(ROOT, "docs", "art", "textures", "raccord")
OUT = os.path.join(ROOT, "Assets", "Art", "Textures", "Materiaux")
N = 1024


def lum(a):
    return a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114


def hauteur(a, flou=1.5, gamma=1.0):
    h = lum(a)
    h = np.asarray(Image.fromarray(np.clip(h, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(flou))).astype(np.float32)
    lo, hi = np.percentile(h, 2), np.percentile(h, 98)
    h = np.clip((h - lo) / max(1e-3, hi - lo), 0, 1) ** gamma
    return h


def sortie(nom, rgb, h):
    rgba = np.dstack([np.clip(rgb, 0, 255), np.clip(h * 255, 0, 255)]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(os.path.join(OUT, "T_Mat_%s_BaseColor.png" % nom))
    print("T_Mat_%s_BaseColor.png" % nom, rgb.shape, "moyenne %.0f" % rgb.mean())


def reduit(a, n=N):
    return np.asarray(Image.fromarray(np.clip(a, 0, 255).astype(np.uint8)).resize((n, n), Image.LANCZOS)).astype(np.float32)


def charge_raccord(nom):
    os.makedirs(RAC, exist_ok=True)
    dst = os.path.join(RAC, nom + "_raccord.png")
    if not os.path.exists(dst):
        a = np.asarray(Image.open(os.path.join(SRC, nom + ".png")).convert("RGB")).astype(np.float32)
        o, _ = R.raccord(a)
        Image.fromarray(np.clip(o + 0.5, 0, 255).astype(np.uint8)).save(dst)
    return np.asarray(Image.open(dst).convert("RGB")).astype(np.float32)


def compresse_hautes(a, seuil=0.5, force=0.55):
    """Compresse les valeurs claires (lichen blanc de l'écorce : il scintillait comme de la neige à distance)."""
    l = lum(a)[..., None] / 255.0
    k = np.where(l > seuil, (seuil + (l - seuil) * force) / np.maximum(l, 1e-4), 1.0)
    return a * k


def simple(nom_src, nom, flou=1.5, gamma=1.0, gain=1.0, compresse=False):
    a = reduit(charge_raccord(nom_src)) * gain
    if compresse:
        a = compresse_hautes(a)
    sortie(nom, a, hauteur(a, flou, gamma))


def tenture_plis(tap):
    a = reduit(tap)
    # velours plus sombre et plus profond que le tapis (mur, pas sol)
    a = a * np.array([1.0, 0.86, 0.92])
    x = np.arange(N) / N
    # 4 plis : profil périodique peint en 3 aplats adoucis (creux, flanc, crête)
    p = 0.5 + 0.5 * np.cos(2 * np.pi * 4 * x + 0.6 * np.sin(2 * np.pi * x))
    step = np.where(p < 0.28, 0.58, np.where(p < 0.72, 0.84, 1.0))
    step = np.asarray(Image.fromarray((step[None, :].repeat(8, 0) * 255).astype(np.uint8)).resize((N, 8), Image.BILINEAR)
                      .filter(ImageFilter.GaussianBlur(2))).astype(np.float32)[0] / 255
    a = a * step[None, :, None]
    # trait d'encre au fond des plis
    ink = np.clip(1 - np.abs(p - 0.0) / 0.05, 0, 1)[None, :, None]
    a = a * (1 - 0.45 * ink)
    h = 0.35 * hauteur(a) + 0.65 * np.repeat(step[None, :], N, 0)
    sortie("Tenture", a, h)


def tenture(tap):
    """Tenture (velours mural) : même motif que le tapis à la période (2 × 2 rosaces, tuile de 2 m : rosace de 1 m, lisible à
    5 m), plus sombre et plus froid ; pas de plis peints (la géométrie a les siens)."""
    crop = tap[0:1024, 0:1024]
    crop, _ = R.raccord(crop, 128, 32)
    a = crop * np.array([0.78, 0.66, 0.74])
    sortie("Tenture", a, hauteur(a, 1.0, 1.0))


def bruit_periodique(n, graine, freqs=(1, 2, 3, 5), flou=0):
    """Bruit lisse et périodique (somme de sinusoïdes à fréquences entières) : la tuile se raccorde."""
    rng = np.random.default_rng(graine)
    y, x = np.mgrid[0:n, 0:n] / n
    v = np.zeros((n, n), np.float32)
    for f in freqs:
        for _ in range(3):
            fx, fy = rng.integers(-f, f + 1, 2)
            if fx == 0 and fy == 0:
                continue
            v += np.cos(2 * np.pi * (fx * x + fy * y) + rng.uniform(0, 6.28)) / f
    return (v - v.min()) / (v.max() - v.min())


def herbe(src):
    """Herbe sèche : la source pousse en rangées (bandes visibles en 3 × 3). Recomposée par « texture bombing » : trois copies
    tournées d'un quart de tour et décalées, choisies par mélange en hauteur (la touffe la plus haute gagne) sous un bruit
    périodique : plus de bandes, la tuile se raccorde toujours."""
    a = reduit(src)
    h = hauteur(a, 1.0)
    couches = [(a, h)]
    for k, (dx, dy) in zip((1, 2, 3), ((317, 611), (733, 251), (149, 877))):
        ak = np.roll(np.roll(np.rot90(a, k), dy, 0), dx, 1)
        hk = np.roll(np.roll(np.rot90(h, k), dy, 0), dx, 1)
        couches.append((ak, hk))
    poids = [bruit_periodique(N, 10 + i) for i in range(len(couches))]
    score = np.stack([0.55 * hk + 0.9 * w for (ak, hk), w in zip(couches, poids)])
    # transition douce (3 px) entre couches : softmax à température basse
    e = np.exp((score - score.max(0)) / 0.04)
    wts = e / e.sum(0)
    rgb = sum(wts[i][..., None] * couches[i][0] for i in range(len(couches)))
    hh = sum(wts[i] * couches[i][1] for i in range(len(couches)))
    sortie("HerbeSeche", rgb * 0.7, hh)   # touffes plus nombreuses que dans la source : valeur ramenée à celle de la source


def marbre(src):
    rng = np.random.default_rng(4242)
    S = 4
    cell = N // S
    H = src.shape[0]
    out = np.zeros((N, N, 3), np.float32)
    hh = np.zeros((N, N), np.float32)
    clair_fond = np.array([0.66, 0.65, 0.64])                       # gris pierre clair, pas de blanc (château très sombre)
    for j in range(S):
        for i in range(S):
            x0, y0 = rng.integers(0, H - 512, 2)
            crop = src[y0:y0 + 512, x0:x0 + 512]
            crop = np.asarray(Image.fromarray(crop.astype(np.uint8)).resize((cell, cell), Image.LANCZOS)).astype(np.float32)
            crop = np.rot90(crop, rng.integers(0, 4))
            if rng.random() < 0.5:
                crop = crop[:, ::-1]
            if (i + j) % 2 == 0:
                c = crop * clair_fond                                     # marbre clair, cassé (pas de blanc éclatant)
            else:
                l = lum(crop)[..., None] / 255.0
                v = 1.0 - l                                               # veines claires sur fond sombre
                c = 255 * (0.13 + 0.42 * v ** 1.5) * np.array([0.80, 0.83, 0.90])   # veines lisibles dans la pénombre
                c = c * (0.85 + 0.15 * crop / 255.0)
            out[j * cell:(j + 1) * cell, i * cell:(i + 1) * cell] = c
            hh[j * cell:(j + 1) * cell, i * cell:(i + 1) * cell] = 0.8
    # joints : 3 px d'encre, léger biseau clair en haut et à gauche de chaque dalle
    ink = np.array([28, 24, 30], np.float32)
    for k in range(S):
        e = k * cell
        for d in (-1, 0, 1):
            out[(e + d) % N, :] = ink
            out[:, (e + d) % N] = ink
            hh[(e + d) % N, :] = 0.05
            hh[:, (e + d) % N] = 0.05
        out[(e + 2) % N, :] *= 1.18
        out[:, (e + 2) % N] *= 1.18
    sortie("Marbre", out, hh)


if __name__ == "__main__":
    simple("tex_ecorce_v01", "Ecorce", 1.5, 0.9, compresse=True)
    simple("tex_gravier_v01", "Gravier", 1.2, 1.0)
    herbe(charge_raccord("tex_herbe_seche_v01"))
    tap = charge_raccord("tex_tapis_tenture_v01")
    # tapis : la période du motif est de 1024 px (mesurée par autocorrélation) ; on garde 2 × 2 rosaces à pleine résolution
    # (tuile de 2 m, 512 px/m : rosace de 1 m, lisible à 10 m) au lieu de 4 × 4 rosaces réduites (bruit à distance).
    crop = tap[512:1536, 512:1536]
    crop, _ = R.raccord(crop, 128, 32)
    sortie("Tapis", crop, hauteur(crop, 1.0, 1.0))
    tenture(tap)
    marbre(charge_raccord("tex_marbre_v01"))
