# -*- coding: utf-8 -*-
"""Éclaboussure peinte (lot F, 2026-11-21) : planche 4 x 4 d'une couronne d'eau qui jaillit puis retombe en gouttes,
aplats (écume claire, eau verte, eau sombre) et trait d'encre ; et un anneau de vague (onde qui s'élargit). Couleurs dans la
texture (particules URP Unlit). Sorties : Assets/Art/Eau/Textures/T_Eau_Eclaboussure_4x4.png, T_Eau_Onde.png.

Usage : python eau_textures.py
"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../.."))
OUT = os.path.join(ROOT, "Assets/Art/Eau/Textures")
os.makedirs(OUT, exist_ok=True)
ENCRE = np.array([0.02, 0.04, 0.04]); TONS = [np.array([0.1, 0.2, 0.15]), np.array([0.3, 0.45, 0.32]), np.array([0.72, 0.85, 0.7])]


def blur(a, r):
    k = 2 * r + 1
    p = np.pad(a, r, mode="edge")
    c = np.pad(np.cumsum(np.cumsum(p, 0), 1), ((1, 0), (1, 0)))
    return (c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]) / (k * k)


def peint(champ, seuils=(0.05, 0.35, 0.65), encre=2):
    inside = champ > seuils[0]
    m = inside.astype(float)
    ring = inside & (blur(m, encre) < 0.98)
    idx = np.where(champ > seuils[2], 2, np.where(champ > seuils[1], 1, 0))
    rgb = np.stack(TONS)[idx]
    rgb[ring] = ENCRE
    a = np.maximum(m, np.clip(blur(m, 1) * 1.5 - 0.3, 0, 1) * (blur(m, 1) > 0.05))
    return np.concatenate([rgb, a[..., None]], -1)


def couronne(S=128, N=16, seed=4):
    r = np.random.default_rng(seed)
    gouttes = [(r.uniform(-0.9, 0.9), r.uniform(1.2, 2.2), r.uniform(0.05, 0.1)) for _ in range(14)]
    y, x = np.mgrid[0:S, 0:S].astype(float)
    x = (x + 0.5) / S * 2 - 1; y = 1 - (y + 0.5) / S * 2
    frames = []
    for i in range(N):
        t = i / (N - 1)
        champ = np.zeros((S, S))
        # colonne / couronne qui monte puis retombe (parabole), s'ouvre
        h = 1.4 * t * (1.2 - t) * 2.2
        ouv = 0.25 + 0.6 * t
        base = -0.85
        for k in range(9):
            xk = (k - 4) / 4 * ouv
            hk = h * (1 - 0.5 * abs(k - 4) / 4)
            lw = 0.11 * (1 - 0.5 * t)
            d = np.where((y > base) & (y < base + hk), np.abs(x - xk * (1 + (y - base) * 0.4)) / lw, 9)
            champ = np.maximum(champ, np.clip(1 - d, 0, 1) * (1 - np.clip((y - base) / max(hk, 1e-3), 0, 1) * 0.5))
        # gouttes en vol
        for gx, gv, gr in gouttes:
            px = gx * t * 1.1
            py = base + gv * t - 2.6 * t * t
            d = np.hypot(x - px, (y - py) * 0.8) / (gr * (1 - 0.4 * t))
            champ = np.maximum(champ, np.clip(1 - d, 0, 1) * 1.3)
        # flaque d'écume à la base
        d = np.hypot(x / (0.5 + 0.4 * t), (y - base) / 0.08)
        champ = np.maximum(champ, np.clip(1 - d, 0, 1) * (1 - t))
        frames.append(peint(np.clip(champ, 0, 1)))
    out = np.zeros((S * 4, S * 4, 4))
    for i, f in enumerate(frames):
        rr, cc = divmod(i, 4)
        out[rr * S:(rr + 1) * S, cc * S:(cc + 1) * S] = f
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8), "RGBA").save(os.path.join(OUT, "T_Eau_Eclaboussure_4x4.png"))


def onde(S=256):
    y, x = np.mgrid[0:S, 0:S].astype(float)
    x = (x + 0.5) / S * 2 - 1; y = (y + 0.5) / S * 2 - 1
    d = np.hypot(x, y)
    champ = np.clip(1 - np.abs(d - 0.8) / 0.09, 0, 1) * 1.2
    img = peint(champ, (0.05, 0.4, 0.75), encre=3)
    Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8), "RGBA").save(os.path.join(OUT, "T_Eau_Onde.png"))


if __name__ == "__main__":
    couronne(); onde(); print("ok")
