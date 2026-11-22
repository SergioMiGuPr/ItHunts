# -*- coding: utf-8 -*-
"""Textures du lot 8 (nuit du 2026-11-22), même codage que vfx_textures.py (R ton : 0 encre, 1/3 sombre, 2/3 moyen,
1 cœur ; G dissolution ; A couverture) :
  T_VFX_Eclair_2x2 : quatre éclairs ramifiés dessinés (trait d'encre, gaine moyenne, cœur clair), pour la balle
  paralysante et l'éclair de parade ;
  T_VFX_Fissure : anneau de fissures au sol (impact du boss, explosion de zone).
Sans IA. Usage : python vfx3_textures.py
"""
import os, math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../.."))
OUT = os.path.join(ROOT, "Assets/Art/VFX/Textures")
R = np.random.default_rng(31)


def bruit(n, k=8):
    g = R.random((k + 1, k + 1))
    y, x = np.mgrid[0:n, 0:n] / n * k
    x0, y0 = x.astype(int), y.astype(int)
    fx, fy = x - x0, y - y0
    fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
    a = g[y0, x0] * (1 - fx) + g[y0, x0 + 1] * fx
    b = g[y0 + 1, x0] * (1 - fx) + g[y0 + 1, x0 + 1] * fx
    return a * (1 - fy) + b * fy


def eclair(n, seed):
    rnd = np.random.default_rng(seed)
    ss = 3
    enc = Image.new("L", (n * ss, n * ss), 0)
    moy = Image.new("L", (n * ss, n * ss), 0)
    coeur = Image.new("L", (n * ss, n * ss), 0)
    de, dm, dc = ImageDraw.Draw(enc), ImageDraw.Draw(moy), ImageDraw.Draw(coeur)

    def branche(x, y, ang, L, w, prof):
        pts = [(x, y)]
        seg = max(4, int(L / (n * 0.06)))
        for i in range(seg):
            ang += rnd.normal(0, 0.55)
            x += math.cos(ang) * L / seg
            y += math.sin(ang) * L / seg
            pts.append((x, y))
        S = [(px * ss, py * ss) for px, py in pts]
        de.line(S, fill=255, width=int((w + 5) * ss), joint="curve")
        dm.line(S, fill=255, width=int((w + 1.5) * ss), joint="curve")
        dc.line(S, fill=255, width=max(1, int(w * 0.45 * ss)), joint="curve")
        if prof < 2:
            for _ in range(rnd.integers(1, 3)):
                k = rnd.integers(1, len(pts) - 1)
                branche(pts[k][0], pts[k][1], ang + rnd.choice([-1, 1]) * rnd.uniform(0.5, 1.1), L * 0.45, w * 0.55, prof + 1)

    branche(n * 0.5, n * 0.06, math.pi / 2 + rnd.normal(0, 0.15), n * 0.88, n * 0.035, 0)
    E = np.asarray(enc.resize((n, n), Image.LANCZOS), np.float32) / 255
    M = np.asarray(moy.resize((n, n), Image.LANCZOS), np.float32) / 255
    C = np.asarray(coeur.resize((n, n), Image.LANCZOS), np.float32) / 255
    ton = np.where(C > 0.5, 1.0, np.where(M > 0.5, 0.66, 0.0))
    return ton, E


def planche(fn, n=256, k=2, seeds=(1, 2, 3, 4)):
    big = np.zeros((n * k, n * k, 4), np.float32)
    for i, s in enumerate(seeds):
        ton, a = fn(n, s)
        g = bruit(n, 6)
        y, x = (i // k) * n, (i % k) * n
        big[y:y + n, x:x + n, 0] = ton
        big[y:y + n, x:x + n, 1] = g
        big[y:y + n, x:x + n, 3] = a
    return big


def fissure(n=512):
    ss = 2
    enc = Image.new("L", (n * ss, n * ss), 0)
    d = ImageDraw.Draw(enc)
    rnd = np.random.default_rng(9)
    c = n * ss / 2
    for k in range(14):
        ang = k * 2 * math.pi / 14 + rnd.normal(0, 0.12)
        r0 = n * ss * 0.08
        x, y = c + math.cos(ang) * r0, c + math.sin(ang) * r0
        pts = [(x, y)]
        for i in range(14):
            ang += rnd.normal(0, 0.25)
            x += math.cos(ang) * n * ss * 0.028
            y += math.sin(ang) * n * ss * 0.028
            pts.append((x, y))
        for i in range(len(pts) - 1):
            w = int((1 - i / len(pts)) * 9 * ss) + 1
            d.line([pts[i], pts[i + 1]], fill=255, width=w)
    d.ellipse([c - n * ss * 0.09, c - n * ss * 0.09, c + n * ss * 0.09, c + n * ss * 0.09], fill=255)
    A = np.asarray(enc.resize((n, n), Image.LANCZOS), np.float32) / 255
    out = np.zeros((n, n, 4), np.float32)
    out[..., 0] = 0.0
    out[..., 1] = bruit(n, 8)
    out[..., 3] = A
    return out


def sauve(a, nom):
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(os.path.join(OUT, nom + ".png"))


if __name__ == "__main__":
    sauve(planche(eclair), "T_VFX_Eclair_2x2")
    sauve(fissure(), "T_VFX_Fissure")
    print("ok")
