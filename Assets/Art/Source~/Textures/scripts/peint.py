# -*- coding: utf-8 -*-
"""Boîte à outils de peinture procédurale (lot « matériaux », 2026-11-22).

Tout est **périodique** (bruits spectraux par FFT, cellules de Voronoï sur grille avec repli, traits dessinés aux 9 copies) :
chaque tuile se raccorde sans couture. Numpy et PIL seulement : tourne dans le Python de Blender (`blender -b -P`) comme
dans un Python ordinaire. Le style suit le guide de la DA : aplats posterisés (2 à 4 tons par pierre), encre sombre sur les
joints et les fissures, arête haute éclairée et arête basse dans l'ombre (lumière peinte venant d'en haut à gauche),
coups de pinceau directionnels, aucune photo.
"""
import math
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

INK = np.array([0.055, 0.045, 0.07], np.float32)


# ------------------------------------------------------------------ bases
def rng(seed):
    return np.random.default_rng(seed)


def norm(a):
    a = a - a.min()
    m = a.max()
    return (a / m if m > 0 else a).astype(np.float32)


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0 + 1e-9), 0, 1)
    return t * t * (3 - 2 * t)


def poster(x, levels, soft=0.0):
    """Posterise 0..1 en `levels` aplats ; soft > 0 adoucit un peu le bord des aplats (en fraction de palier)."""
    y = x * levels
    f = np.floor(y)
    if soft > 0:
        fr = y - f
        f = f + smooth(1 - soft, 1.0, fr)
    return np.clip(f / max(levels - 1, 1), 0, 1).astype(np.float32)


def lerp(a, b, t):
    if np.ndim(t) == 2 and (np.ndim(a) in (1, 3) or np.ndim(b) in (1, 3)):
        t = t[..., None]
    return a + (b - a) * t


def col(hexa):
    h = hexa.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], np.float32)


def fill(N, c, M=None):
    M = M or N
    return np.broadcast_to(np.asarray(c, np.float32), (M, N, 3)).copy()


# ------------------------------------------------------------------ bruits périodiques
def fbm(N, r, beta=2.0, lo=1, hi=None, aniso=(1.0, 1.0), M=None):
    """Bruit spectral périodique : spectre en 1/f^beta entre les fréquences lo et hi (en cycles par tuile).
    aniso = (ax, ay) : > 1 étire la fréquence dans cet axe (ax grand : traits verticaux fins…)."""
    M = M or N
    hi = hi or N // 2
    w = r.standard_normal((M, N)).astype(np.float32)
    F = np.fft.fft2(w)
    ky = np.fft.fftfreq(M) * M * aniso[1]
    kx = np.fft.fftfreq(N) * N * aniso[0]
    k = np.sqrt(kx[None, :] ** 2 + ky[:, None] ** 2)
    k[0, 0] = 1
    filt = np.where((k >= lo) & (k <= hi), k ** (-beta / 2.0), 0.0)
    filt[0, 0] = 0
    return norm(np.real(np.fft.ifft2(F * filt)))


def sample(img, x, y):
    """Lecture bilinéaire avec repli (x, y en pixels, img 2D ou 3D)."""
    H, W = img.shape[:2]
    x0 = np.floor(x).astype(np.int64)
    y0 = np.floor(y).astype(np.int64)
    fx = (x - x0).astype(np.float32)
    fy = (y - y0).astype(np.float32)
    x0 %= W
    y0 %= H
    x1 = (x0 + 1) % W
    y1 = (y0 + 1) % H
    if img.ndim == 3:
        fx = fx[..., None]
        fy = fy[..., None]
    a = img[y0, x0] * (1 - fx) + img[y0, x1] * fx
    b = img[y1, x0] * (1 - fx) + img[y1, x1] * fx
    return a * (1 - fy) + b * fy


def warp(img, dx, dy):
    H, W = img.shape[:2]
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
    return sample(img, xx + dx, yy + dy)


def grad(h):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5
    return gx, gy


def blur(a, rad):
    """Flou gaussien périodique (FFT)."""
    if rad <= 0:
        return a
    H, W = a.shape[:2]
    ky = np.fft.fftfreq(H)[:, None]
    kx = np.fft.fftfreq(W)[None, :]
    g = np.exp(-2 * (math.pi ** 2) * (rad ** 2) * (kx ** 2 + ky ** 2))
    if a.ndim == 3:
        return np.stack([np.real(np.fft.ifft2(np.fft.fft2(a[..., c]) * g)) for c in range(a.shape[2])], -1).astype(np.float32)
    return np.real(np.fft.ifft2(np.fft.fft2(a) * g)).astype(np.float32)


# ------------------------------------------------------------------ cellules
def cells(N, gx, gy, r, jitter=0.9, wx=None, wy=None, M=None):
    """Voronoï périodique sur une grille gx × gy (un germe par case). wx, wy : déformation (pixels) des coordonnées.
    Renvoie F1, F2 (en unités de case), id de cellule (entier) et position du germe le plus proche (pixels)."""
    M = M or N
    yy, xx = np.mgrid[0:M, 0:N].astype(np.float32)
    if wx is not None:
        xx = xx + wx
        yy = yy + wy
    u = xx / N * gx
    v = yy / M * gy
    ci = np.floor(u).astype(np.int64)
    cj = np.floor(v).astype(np.int64)
    pts = r.random((gy, gx, 2)).astype(np.float32) * jitter + (1 - jitter) / 2
    F1 = np.full((M, N), 9.0, np.float32)
    F2 = np.full((M, N), 9.0, np.float32)
    ID = np.zeros((M, N), np.int64)
    PX = np.zeros((M, N), np.float32)
    PY = np.zeros((M, N), np.float32)
    for dj in (-1, 0, 1):
        for di in (-1, 0, 1):
            ni = ci + di
            nj = cj + dj
            wi = ni % gx
            wj = nj % gy
            p = pts[wj, wi]
            px = ni + p[..., 0]
            py = nj + p[..., 1]
            d = np.sqrt((px - u) ** 2 + (py - v) ** 2)
            closer = d < F1
            F2 = np.where(closer, F1, np.minimum(F2, d))
            F1 = np.where(closer, d, F1)
            ID = np.where(closer, wj * gx + wi, ID)
            PX = np.where(closer, px / gx * N, PX)
            PY = np.where(closer, py / gy * M, PY)
    return F1, F2, ID, PX, PY


def edge_dist(F1, F2):
    """Distance approchée au bord de la cellule (unités de case) : (F2 - F1) / 2."""
    return (F2 - F1) * 0.5


def courses(N, rows, r, wmin, wmax, M=None, stagger=True, row_jit=0.0):
    """Appareil en assises : `rows` rangées, blocs de largeur tirée entre wmin et wmax (fraction de tuile), raccord
    périodique en x. Renvoie id de bloc, distance au bord (pixels), coordonnées locales (0..1)."""
    M = M or N
    yy, xx = np.mgrid[0:M, 0:N].astype(np.float32)
    # hauteurs de rangées légèrement irrégulières
    hts = np.full(rows, 1.0) + r.uniform(-row_jit, row_jit, rows)
    hts = hts / hts.sum() * M
    edges = np.concatenate([[0], np.cumsum(hts)])
    ID = np.zeros((M, N), np.int64)
    D = np.zeros((M, N), np.float32)
    LU = np.zeros((M, N), np.float32)
    LV = np.zeros((M, N), np.float32)
    nid = 0
    for k in range(rows):
        y0, y1 = edges[k], edges[k + 1]
        ws = []
        tot = 0.0
        while tot < 1.0:
            w = r.uniform(wmin, wmax)
            ws.append(w)
            tot += w
        ws = np.array(ws) / tot * N
        xs = np.concatenate([[0], np.cumsum(ws)]) + (r.uniform(0, N) if stagger else 0)
        band = (yy >= y0) & (yy < y1)
        for b in range(len(ws)):
            a0, a1 = xs[b], xs[b + 1]
            lx = (xx - a0) % N
            inb = band & (lx < (a1 - a0))
            dx = np.minimum(lx, (a1 - a0) - lx)
            dy = np.minimum(yy - y0, y1 - yy)
            ID = np.where(inb, nid, ID)
            D = np.where(inb, np.minimum(dx, dy), D)
            LU = np.where(inb, lx / (a1 - a0), LU)
            LV = np.where(inb, (yy - y0) / (y1 - y0), LV)
            nid += 1
    return ID, D, LU, LV


# ------------------------------------------------------------------ ombrage peint
def bevel_light(height, strength=6.0, levels=3, light=(-0.55, -0.62, 0.56)):
    """Lumière peinte venant d'en haut à gauche sur un champ de hauteur (0..1) : renvoie -1..1 posterisé."""
    gx, gy = grad(height)
    nx, ny, nz = -gx * strength, -gy * strength, np.ones_like(height)
    l = np.array(light, np.float32)
    l = l / np.linalg.norm(l)
    n = np.sqrt(nx * nx + ny * ny + nz * nz)
    d = (nx * l[0] + ny * l[1] + nz * l[2]) / n
    flat = l[2]
    s = np.clip((d - flat) * 2.2, -1, 1)
    if levels:
        s = np.sign(s) * poster(np.abs(s), levels)
    return s.astype(np.float32)


def per_id(ID, r, n=None, lo=0.0, hi=1.0):
    n = n or int(ID.max()) + 1
    v = r.uniform(lo, hi, n).astype(np.float32)
    return v[ID]


def hue_jitter(base, amt, r):
    return np.clip(base + r.uniform(-amt, amt, 3).astype(np.float32), 0, 1)


def strokes(N, r, angle=0.0, length=10.0, M=None, scale=(2, 90)):
    """Coups de pinceau : bruit étiré dans une direction (radians, 0 = horizontal)."""
    M = M or N
    a = fbm(N, r, beta=1.4, lo=scale[0], hi=scale[1], aniso=(1.0, length) if abs(math.cos(angle)) > 0.7 else (length, 1.0), M=M)
    return a


# ------------------------------------------------------------------ traits (fissures, veines, fil du bois)
class Ink:
    """Calque de traits dessinés avec PIL, en 9 copies : se raccorde. Valeur 0..1 (1 = encre)."""

    def __init__(self, N, M=None, ss=2):
        self.N, self.M, self.ss = N, (M or N), ss
        self.im = Image.new("L", (self.N * ss, self.M * ss), 0)
        self.d = ImageDraw.Draw(self.im)

    def line(self, pts, w, v=255):
        s = self.ss
        for ox in (-self.N, 0, self.N):
            for oy in (-self.M, 0, self.M):
                q = [((x + ox) * s, (y + oy) * s) for x, y in pts]
                self.d.line(q, fill=int(v), width=max(1, int(round(w * s))), joint="curve")

    def poly(self, pts, v=255):
        s = self.ss
        for ox in (-self.N, 0, self.N):
            for oy in (-self.M, 0, self.M):
                self.d.polygon([((x + ox) * s, (y + oy) * s) for x, y in pts], fill=int(v))

    def dot(self, x, y, rad, v=255):
        s = self.ss
        for ox in (-self.N, 0, self.N):
            for oy in (-self.M, 0, self.M):
                cx, cy = (x + ox) * s, (y + oy) * s
                self.d.ellipse([cx - rad * s, cy - rad * s, cx + rad * s, cy + rad * s], fill=int(v))

    def get(self, blur_px=0.0):
        im = self.im.resize((self.N, self.M), Image.LANCZOS)
        if blur_px > 0:
            im = im.filter(ImageFilter.GaussianBlur(blur_px))
        return np.asarray(im, np.float32) / 255.0


def crack_path(r, x, y, ang, length, step=6.0, wander=0.5):
    pts = [(x, y)]
    n = max(2, int(length / step))
    for _ in range(n):
        ang += r.normal(0, wander)
        x += math.cos(ang) * step
        y += math.sin(ang) * step
        pts.append((x, y))
    return pts


def cracks(ink, r, count, N, length=(40, 160), width=(1.2, 2.6), branch=0.35, M=None, inside=None):
    M = M or N
    for _ in range(count):
        for _try in range(20):
            x, y = r.uniform(0, N), r.uniform(0, M)
            if inside is None or inside[int(y) % M, int(x) % N]:
                break
        ang = r.uniform(0, 2 * math.pi)
        L = r.uniform(*length)
        pts = crack_path(r, x, y, ang, L)
        w = r.uniform(*width)
        # trait effilé : segments de plus en plus fins
        for i in range(len(pts) - 1):
            t = i / max(len(pts) - 1, 1)
            ink.line([pts[i], pts[i + 1]], max(0.6, w * (1 - t * 0.8)))
        if r.random() < branch and len(pts) > 4:
            k = r.integers(1, len(pts) - 2)
            bx, by = pts[k]
            b = crack_path(r, bx, by, ang + r.choice([-1, 1]) * r.uniform(0.5, 1.2), L * 0.45)
            for i in range(len(b) - 1):
                t = i / max(len(b) - 1, 1)
                ink.line([b[i], b[i + 1]], max(0.5, w * 0.7 * (1 - t * 0.8)))


# ------------------------------------------------------------------ sortie
def save(path, rgb, height=None, alpha=None):
    rgb = np.clip(rgb, 0, 1)
    a = alpha if alpha is not None else (height if height is not None else None)
    if a is not None:
        arr = np.concatenate([rgb, np.clip(a, 0, 1)[..., None]], -1)
        Image.fromarray((arr * 255 + 0.5).astype(np.uint8), "RGBA").save(path, optimize=True)
    else:
        Image.fromarray((rgb * 255 + 0.5).astype(np.uint8), "RGB").save(path, optimize=True)


def tiled_preview(rgb, k=2):
    return np.tile(rgb, (k, k, 1))
