# -*- coding: utf-8 -*-
"""Recalage d'images (numpy + Pillow seuls) pour les menus : aligne une image générée (2752 x 1536, recadrée par le
générateur) sur la maquette (1672 x 941) au pixel près.

Modèle : affine global (6 paramètres, Gauss-Newton robuste, du grossier au fin) puis, si demandé, un champ de déplacement
local lissé (blocs) pour rattraper les petites dérives du générateur. Les zones qui changent (texte, personnages) sont
exclues par un masque et par la pondération robuste.

Convention : M (2 x 3) envoie un pixel (x, y) de la CIBLE vers les coordonnées du pixel de la SOURCE.
"""
import numpy as np
from PIL import Image, ImageFilter

# ---------------------------------------------------------------- E/S

def load(path):
    im = Image.open(path).convert("RGB")
    return np.asarray(im, dtype=np.float32) / 255.0


def load_rgba(path):
    im = Image.open(path).convert("RGBA")
    return np.asarray(im, dtype=np.float32) / 255.0


def save(path, a, mode=None):
    a = np.clip(a, 0, 1)
    u8 = (a * 255.0 + 0.5).astype(np.uint8)
    if mode is None:
        mode = {2: "L", 3: "RGB", 4: "RGBA"}[u8.shape[2] if u8.ndim == 3 else 2]
        if u8.ndim == 2:
            mode = "L"
    Image.fromarray(u8, mode).save(path)


def gray(a):
    return a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114


def _box1(a, k, axis):
    """Moyenne glissante de largeur 2k+1 le long d'un axe, bords recopiés."""
    if k <= 0:
        return a
    pad = [(0, 0)] * a.ndim
    pad[axis] = (k + 1, k)
    p = np.pad(a, pad, mode="edge")
    c = np.cumsum(p, axis=axis, dtype=np.float64)
    n = a.shape[axis]
    hi = np.take(c, np.arange(2 * k + 1, 2 * k + 1 + n), axis=axis)
    lo = np.take(c, np.arange(0, n), axis=axis)
    return ((hi - lo) / (2 * k + 1)).astype(np.float32)


def blur(a, r):
    """Flou quasi gaussien d'écart type r (trois passes de boîte), numpy seul."""
    if r <= 0.3:
        return a.astype(np.float32)
    # largeur de boîte pour 3 passes : sigma^2 = 3 * ((2k+1)^2 - 1) / 12
    k = max(int(round((np.sqrt(4.0 * r * r + 1.0) - 1.0) / 2.0)), 1)
    out = a.astype(np.float32)
    for _ in range(3):
        out = _box1(out, k, 0)
        out = _box1(out, k, 1)
    return out


def resize(a, w, h):
    if a.ndim == 3:
        return np.stack([resize(a[..., c], w, h) for c in range(a.shape[2])], -1)
    im = Image.fromarray(a.astype(np.float32), "F")
    return np.asarray(im.resize((w, h), Image.BILINEAR if w > a.shape[1] else Image.BOX), dtype=np.float32)

# ---------------------------------------------------------------- échantillonnage

def sample(img, X, Y):
    """Bilinéaire, bord recopié. img (H, W[, C]), X, Y (h, w) en pixels source."""
    H, W = img.shape[:2]
    X = np.clip(X, 0, W - 1.001)
    Y = np.clip(Y, 0, H - 1.001)
    x0 = np.floor(X).astype(np.int32)
    y0 = np.floor(Y).astype(np.int32)
    fx = (X - x0)
    fy = (Y - y0)
    if img.ndim == 3:
        fx = fx[..., None]
        fy = fy[..., None]
    a = img[y0, x0]
    b = img[y0, x0 + 1]
    c = img[y0 + 1, x0]
    d = img[y0 + 1, x0 + 1]
    return (a * (1 - fx) + b * fx) * (1 - fy) + (c * (1 - fx) + d * fx) * fy


def grid(h, w):
    Y, X = np.mgrid[0:h, 0:w].astype(np.float32)
    return X, Y


def warp(src, M, h, w, flow=None):
    """Image source vue dans le repère de la cible (h, w). flow : (dx, dy) ajoutés en coordonnées source."""
    X, Y = grid(h, w)
    Xs = M[0, 0] * X + M[0, 1] * Y + M[0, 2]
    Ys = M[1, 0] * X + M[1, 1] * Y + M[1, 2]
    if flow is not None:
        Xs = Xs + flow[0]
        Ys = Ys + flow[1]
    return sample(src, Xs, Ys)


def inside(M, h, w, sh, sw, flow=None, margin=1.0):
    """Masque des pixels cibles dont l'antécédent tombe dans la source."""
    X, Y = grid(h, w)
    Xs = M[0, 0] * X + M[0, 1] * Y + M[0, 2]
    Ys = M[1, 0] * X + M[1, 1] * Y + M[1, 2]
    if flow is not None:
        Xs = Xs + flow[0]
        Ys = Ys + flow[1]
    return (Xs >= margin) & (Ys >= margin) & (Xs <= sw - 1 - margin) & (Ys <= sh - 1 - margin)

# ---------------------------------------------------------------- image de travail

def feature(a, r=None):
    """Gris passe-haut normalisé localement : insensible aux petits écarts de couleur et d'exposition."""
    g = gray(a) if a.ndim == 3 else a
    s = max(g.shape) / 1672.0
    r = r or 6.0 * s
    hp = g - blur(g, r)
    en = np.sqrt(blur(hp * hp, r * 2.0) + 1e-4)
    return hp / en


def grads(a):
    gx = np.zeros_like(a)
    gy = np.zeros_like(a)
    gx[:, 1:-1] = (a[:, 2:] - a[:, :-2]) * 0.5
    gy[1:-1] = (a[2:] - a[:-2]) * 0.5
    return gx, gy

# ---------------------------------------------------------------- affine robuste

def _scale_M(M, ft, fs):
    """M au niveau plein -> M en coordonnées réduites (cible / ft, source / fs)."""
    S_t = np.array([[ft, 0, 0], [0, ft, 0], [0, 0, 1]], np.float64)
    S_s_inv = np.array([[1 / fs, 0, 0], [0, 1 / fs, 0], [0, 0, 1]], np.float64)
    M3 = np.vstack([M, [0, 0, 1]])
    return (S_s_inv @ M3 @ S_t)[:2]


def _unscale_M(Ml, ft, fs):
    S_t_inv = np.array([[1 / ft, 0, 0], [0, 1 / ft, 0], [0, 0, 1]], np.float64)
    S_s = np.array([[fs, 0, 0], [0, fs, 0], [0, 0, 1]], np.float64)
    M3 = np.vstack([Ml, [0, 0, 1]])
    return (S_s @ M3 @ S_t_inv)[:2]


def phase_corr(a, b):
    """Translation (dx, dy) telle que b(x) ~ a(x - d)."""
    A = np.fft.rfft2(a)
    B = np.fft.rfft2(b)
    R = B * np.conj(A)
    R /= np.abs(R) + 1e-9
    r = np.fft.irfft2(R, s=a.shape)
    i = np.unravel_index(np.argmax(r), r.shape)
    dy, dx = i
    if dy > a.shape[0] // 2:
        dy -= a.shape[0]
    if dx > a.shape[1] // 2:
        dx -= a.shape[1]
    return float(dx), float(dy), float(r.max())


def init_affine(T, G, scales=None, level_w=420):
    """Échelle uniforme + translation : recherche par corrélation de phase à basse résolution."""
    h, w = T.shape[:2]
    sh, sw = G.shape[:2]
    ft = w / level_w
    th, tw = int(round(h / ft)), int(round(w / ft))
    Tf = feature(resize(gray(T), tw, th), 3.0)
    if scales is None:
        s0 = sw / w
        scales = np.linspace(s0 * 0.9, s0 * 1.06, 33)
    Gs = blur(gray(G), max(sw / tw / 2.0, 0.5))
    best = None
    for s in scales:
        M = np.array([[s, 0, 0], [0, s, 0]], np.float64)
        # centre sur centre
        M[0, 2] = (sw - s * w) / 2
        M[1, 2] = (sh - s * h) / 2
        Wl = warp(Gs, _scale_M(M, ft, 1.0), th, tw)
        Wf = feature(Wl, 3.0)
        dx, dy, pk = phase_corr(Tf, Wf)
        # b(x) ~ a(x - d)  : Wf décalé de d par rapport à Tf -> le pixel cible x correspond au pixel x + d de Wf
        if best is None or pk > best[0]:
            Mt = M.copy()
            Mt[0, 2] += s * dx * ft
            Mt[1, 2] += s * dy * ft
            best = (pk, s, Mt)
    return best[2], best[1], best[0]


def refine_affine(T, G, M, mask=None, levels=(8, 4, 2, 1), iters=30, model="affine", verbose=False):
    """Gauss-Newton direct (additif), poids de Tukey, du grossier au fin. mask : poids (h, w) dans la cible."""
    h, w = T.shape[:2]
    sh, sw = G.shape[:2]
    Tg = gray(T)
    Gg = gray(G)
    M = np.array(M, np.float64)
    for f in levels:
        th, tw = max(int(round(h / f)), 8), max(int(round(w / f)), 8)
        ft = w / tw
        fs = ft * (M[0, 0] if f > 1 else 1.0)
        fs = max(fs, 1.0)
        Tl = feature(resize(Tg, tw, th), 2.0 + 1.0 * (f == 1))
        Gl = resize(blur(Gg, max(fs / 2.0 - 0.5, 0)), int(round(sw / fs)), int(round(sh / fs)))
        Gf = feature(Gl, 2.0 + 1.0 * (f == 1))
        gx_s, gy_s = grads(Gf)
        ml = resize(mask.astype(np.float32), tw, th) if mask is not None else np.ones((th, tw), np.float32)
        Ml = _scale_M(M, ft, fs)
        X, Y = grid(th, tw)
        for it in range(iters):
            Xs = Ml[0, 0] * X + Ml[0, 1] * Y + Ml[0, 2]
            Ys = Ml[1, 0] * X + Ml[1, 1] * Y + Ml[1, 2]
            ok = (Xs > 1) & (Ys > 1) & (Xs < Gf.shape[1] - 2) & (Ys < Gf.shape[0] - 2)
            I = sample(Gf, Xs, Ys)
            gx = sample(gx_s, Xs, Ys)
            gy = sample(gy_s, Xs, Ys)
            e = (I - Tl)
            c = 4.685 * 1.4826 * (np.median(np.abs(e[ok])) + 1e-6)
            wgt = np.where(np.abs(e) < c, (1 - (e / c) ** 2) ** 2, 0.0) * ok * ml
            if model == "affine":
                J = np.stack([gx * X, gx * Y, gx, gy * X, gy * Y, gy], -1)
            else:  # échelle x, échelle y, translation
                J = np.stack([gx * X, gx, gy * Y, gy], -1)
            Jw = J * wgt[..., None]
            H = np.einsum("hwi,hwj->ij", Jw, J)
            b = -np.einsum("hwi,hw->i", Jw, e)
            dp = np.linalg.solve(H + np.eye(len(b)) * 1e-6 * np.trace(H), b)
            if model == "affine":
                Ml = Ml + dp.reshape(2, 3)
            else:
                Ml[0, 0] += dp[0]; Ml[0, 2] += dp[1]; Ml[1, 1] += dp[2]; Ml[1, 2] += dp[3]
            if np.abs(dp[[2, 5] if model == "affine" else [1, 3]]).max() < 1e-3:
                break
        M = _unscale_M(Ml, ft, fs)
        if verbose:
            print("  niveau", f, "iters", it + 1, "M", np.round(M, 4).tolist())
    return M


def local_flow(T, Gw, mask=None, block=48, step=24, radius=6, smooth=2.0):
    """Champ de déplacement résiduel (cible -> image déjà recalée Gw) par blocs, corrélation de phase + affinage
    subpixel, lissé et interpolé. Renvoie (dx, dy) en pixels de la cible, et la carte de confiance."""
    h, w = T.shape[:2]
    Tf = feature(T)
    Gf = feature(Gw)
    ys = list(range(0, h - block + 1, step))
    xs = list(range(0, w - block + 1, step))
    DX = np.zeros((len(ys), len(xs)), np.float32)
    DY = np.zeros_like(DX)
    C = np.zeros_like(DX)
    win = np.outer(np.hanning(block), np.hanning(block)).astype(np.float32)
    for i, y in enumerate(ys):
        for j, x in enumerate(xs):
            a = Tf[y:y + block, x:x + block] * win
            b = Gf[y:y + block, x:x + block] * win
            mk = 1.0 if mask is None else float(mask[y:y + block, x:x + block].mean())
            A = np.fft.fft2(a); B = np.fft.fft2(b)
            R = B * np.conj(A); R /= np.abs(R) + 1e-9
            r = np.real(np.fft.ifft2(R))
            k = np.unravel_index(np.argmax(r), r.shape)
            pk = r[k]
            dy, dx = k
            # sous-pixel parabolique
            def para(m, c0, p):
                d = m - 2 * c0 + p
                return 0.0 if abs(d) < 1e-9 else 0.5 * (m - p) / d
            ox = para(r[dy, (dx - 1) % block], r[dy, dx], r[dy, (dx + 1) % block])
            oy = para(r[(dy - 1) % block, dx], r[dy, dx], r[(dy + 1) % block, dx])
            if dy > block // 2: dy -= block
            if dx > block // 2: dx -= block
            dx += ox; dy += oy
            tex = float(np.std(a))
            if abs(dx) > radius or abs(dy) > radius or pk < 0.15 or tex < 0.05:
                pk = 0.0
            DX[i, j] = dx; DY[i, j] = dy; C[i, j] = pk * mk
    # lissage pondéré (remplit les blocs sans confiance)
    def wsmooth(D):
        num = blur(D * C, smooth)
        den = blur(C, smooth) + 1e-6
        return num / den
    DXs, DYs = wsmooth(DX), wsmooth(DY)
    Cs = blur(C, smooth)
    # interpolation au pixel (centres de blocs)
    cy = np.array(ys, np.float32) + block / 2
    cx = np.array(xs, np.float32) + block / 2
    Xg, Yg = grid(h, w)
    gi = np.interp(Yg[:, 0], cy, np.arange(len(ys)))
    gj = np.interp(Xg[0], cx, np.arange(len(xs)))
    GI, GJ = np.meshgrid(gi, gj, indexing="ij")
    fdx = sample(DXs, GJ, GI)
    fdy = sample(DYs, GJ, GI)
    return fdx.astype(np.float32), fdy.astype(np.float32), sample(Cs, GJ, GI)

# ---------------------------------------------------------------- couleur

def color_match(src, ref, mask, deg=1):
    """Ajuste src sur ref par canal (gain, décalage) dans mask : corrige les petits écarts d'exposition du générateur."""
    out = np.empty_like(src)
    m = mask.astype(bool)
    for c in range(3):
        x = src[..., c][m].ravel(); y = ref[..., c][m].ravel()
        if x.size > 200000:
            idx = np.random.default_rng(0).choice(x.size, 200000, replace=False); x = x[idx]; y = y[idx]
        A = np.stack([x, np.ones_like(x)], 1)
        p, *_ = np.linalg.lstsq(A, y, rcond=None)
        out[..., c] = src[..., c] * p[0] + p[1]
    return out


def err_map(a, b):
    return np.sqrt(((a - b) ** 2).sum(-1) / 3.0)


def report(T, W, mask):
    e = err_map(T, W)[mask.astype(bool)] * 255.0
    return {"moy": float(e.mean()), "med": float(np.median(e)), "p95": float(np.percentile(e, 95))}

# ---------------------------------------------------------------- masques et détourage

def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / max(e1 - e0, 1e-9), 0, 1)
    return t * t * (3 - 2 * t)


def _shift_reduce(a, r, fn):
    out = a.copy()
    for axis in (0, 1):
        acc = out.copy()
        for d in range(1, r + 1):
            for s in (d, -d):
                sh = np.roll(out, s, axis=axis)
                if axis == 0:
                    if s > 0: sh[:s] = out[:s]
                    else: sh[s:] = out[s:]
                else:
                    if s > 0: sh[:, :s] = out[:, :s]
                    else: sh[:, s:] = out[:, s:]
                acc = fn(acc, sh)
        out = acc
    return out


def dilate(a, r):
    return _shift_reduce(a, int(r), np.maximum) if r >= 1 else a


def erode(a, r):
    return _shift_reduce(a, int(r), np.minimum) if r >= 1 else a


def fill_holes(m, roi_border=True):
    """Bouche les trous d'un masque binaire (reconstruction depuis le bord)."""
    inv = ~m
    seed = np.zeros_like(m)
    seed[0, :] = inv[0, :]; seed[-1, :] = inv[-1, :]; seed[:, 0] = inv[:, 0]; seed[:, -1] = inv[:, -1]
    while True:
        nxt = dilate(seed.astype(np.uint8), 4).astype(bool) & inv
        if (nxt == seed).all():
            break
        seed = nxt
    return ~seed


def keep_large(m, min_px):
    """Retire les petites taches (étiquetage par propagation, sur une image réduite si besoin)."""
    lab = np.zeros(m.shape, np.int32)
    H, W = m.shape
    cur = 0
    sizes = {}
    ys, xs = np.nonzero(m)
    for y0, x0 in zip(ys, xs):
        if lab[y0, x0]:
            continue
        cur += 1
        stack = [(y0, x0)]; lab[y0, x0] = cur; n = 0
        while stack:
            y, x = stack.pop(); n += 1
            for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= yy < H and 0 <= xx < W and m[yy, xx] and not lab[yy, xx]:
                    lab[yy, xx] = cur; stack.append((yy, xx))
        sizes[cur] = n
    keep = np.zeros(cur + 1, bool)
    for k, n in sizes.items():
        keep[k] = n >= min_px
    return keep[lab] & m


def rect_mask(h, w, x0, y0, x1, y1, feather=0.0):
    X, Y = grid(h, w)
    if feather <= 0:
        return ((X >= x0) & (X < x1) & (Y >= y0) & (Y < y1)).astype(np.float32)
    dx = np.minimum(X - x0, x1 - 1 - X)
    dy = np.minimum(Y - y0, y1 - 1 - Y)
    d = np.minimum(dx, dy)
    return smoothstep(0, feather, d).astype(np.float32)


def ellipse_mask(h, w, cx, cy, rx, ry, feather=0.15):
    X, Y = grid(h, w)
    r = np.sqrt(((X - cx) / rx) ** 2 + ((Y - cy) / ry) ** 2)
    return (1 - smoothstep(1 - feather, 1, r)).astype(np.float32)


def to_lin(x):
    x = np.clip(x, 0, 1)
    return np.where(x <= 0.04045, x / 12.92, ((x + 0.055) / 1.055) ** 2.4)


def to_srgb(x):
    x = np.clip(x, 0, 1)
    return np.where(x <= 0.0031308, x * 12.92, 1.055 * np.power(x, 1 / 2.4) - 0.055)


def unmix(C, B, alpha):
    """Couleur de l'élément telle que alpha * F + (1 - alpha) * B = C exactement sur le même fond. Le projet Unity est
    en espace linéaire (le mélange se fait sur des valeurs linéaires) : le calcul se fait donc en linéaire."""
    a = np.clip(alpha, 0, 1)[..., None]
    Cl, Bl = to_lin(C), to_lin(B)
    F = Bl + (Cl - Bl) / np.maximum(a, 1e-3)
    F = np.where(a > 1e-3, F, Cl)
    return to_srgb(np.clip(F, 0, 1))


def additive(C, B):
    """Lumière ajoutée telle que B + F = C en linéaire (mélange One One dans un projet linéaire), encodée en sRGB."""
    return to_srgb(np.clip(to_lin(C) - to_lin(B), 0, 1))


def diff_alpha(C, B, t0, t1, r=1.0, roi=None):
    d = err_map(C, B)
    if r > 0:
        d = blur(d, r)
    a = smoothstep(t0, t1, d)
    if roi is not None:
        a = a * roi
    return a.astype(np.float32)


def bbox(a, thr=1.0 / 255, pad=2):
    ys, xs = np.nonzero(a > thr)
    if len(xs) == 0:
        return None
    H, W = a.shape[:2]
    return (max(int(xs.min()) - pad, 0), max(int(ys.min()) - pad, 0), min(int(xs.max()) + 1 + pad, W), min(int(ys.max()) + 1 + pad, H))


def save_piece(path, rgb, alpha, box=None):
    """Enregistre rgb + alpha recadrés sur box (ou sur l'alpha). Renvoie le rect (x, y, w, h) dans le repère de l'écran."""
    if box is None:
        box = bbox(alpha)
    x0, y0, x1, y1 = box
    rgba = np.concatenate([rgb[y0:y1, x0:x1], alpha[y0:y1, x0:x1, None]], -1)
    # couleur des pixels transparents : celle du voisinage (évite les franges au filtrage)
    save(path, rgba, "RGBA")
    return [x0, y0, x1 - x0, y1 - y0]


def sat(a):
    mx = a.max(-1); mn = a.min(-1)
    return (mx - mn) / np.maximum(mx, 1e-4), mx


def match_ring(src, ref, inner, ring_r=20):
    """Ajuste src sur ref dans un anneau autour du masque inner (raccord invisible)."""
    ring = (dilate(inner.astype(np.float32), ring_r) > 0.5) & ~(inner > 0.5)
    return color_match(src, ref, ring), ring


def gain_field(src, ref, mask, r=45.0):
    """Champ de gain lisse (par canal) qui amène src sur ref, estimé seulement dans mask et prolongé dans les trous
    (convolution normalisée à plusieurs échelles)."""
    m = mask.astype(np.float32)
    out = np.ones_like(src)
    for c in range(3):
        acc = None
        for rr in (r, r * 2.5, r * 6):
            num = reg_blur(ref[..., c] * m, rr); den_s = reg_blur(src[..., c] * m, rr); wm = reg_blur(m, rr)
            g = (num + 1e-4) / (den_s + 1e-4)
            conf = np.clip(wm * 4, 0, 1)
            acc = g if acc is None else acc * conf_prev + g * (1 - conf_prev)
            conf_prev = conf
        out[..., c] = np.clip(acc, 0.4, 2.5)
    return out


def reg_blur(a, r):
    return blur(a, r)


def cut_piece(C, B, box, t0, t1, r=1.0, min_frac=0.04, grow=2, fill=True, core_t=0.5, close=2):
    """Détourage par différence : alpha doux de |C - B|, restreint aux grandes composantes (le bruit du grain repeint
    est écarté), trous bouchés. Renvoie (couleur démêlée, alpha) en plein écran."""
    h, w = C.shape[:2]
    x0, y0, x1, y1 = box
    roi = rect_mask(h, w, x0, y0, x1, y1)
    a = diff_alpha(C, B, t0, t1, r=r, roi=roi)
    sub = a[y0:y1, x0:x1] > core_t
    sub = erode(dilate(sub.astype(np.float32), close), close) > 0.5
    sub = keep_large(sub, int(min_frac * sub.size))
    if fill and sub.any():
        sub = fill_holes(sub)
    core = np.zeros((h, w), bool); core[y0:y1, x0:x1] = sub
    support = dilate(core.astype(np.float32), grow)
    a = np.maximum(a * support, erode(core.astype(np.float32), 1))
    return unmix(C, B, a), a.astype(np.float32)


def convex_hull(pts):
    """Enveloppe convexe (chaîne monotone). pts : (n, 2) x, y."""
    pts = sorted(set(map(tuple, pts.tolist())))
    if len(pts) < 3:
        return np.array(pts, np.float64)
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lower, upper = [], []
    for p in pts:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(pts):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return np.array(lower[:-1] + upper[:-1], np.float64)


def poly_mask(h, w, poly, aa=1.0):
    """Masque d'un polygone convexe, bord adouci sur aa px (distance signée aux côtés)."""
    X, Y = grid(h, w)
    d = np.full((h, w), np.inf, np.float32)
    n = len(poly)
    # orientation
    area = 0.5 * sum(poly[i][0] * poly[(i + 1) % n][1] - poly[(i + 1) % n][0] * poly[i][1] for i in range(n))
    sgn = 1.0 if area > 0 else -1.0
    for i in range(n):
        x0, y0 = poly[i]; x1, y1 = poly[(i + 1) % n]
        ex, ey = x1 - x0, y1 - y0
        L = np.hypot(ex, ey) + 1e-9
        # distance positive à l'intérieur
        di = sgn * ((X - x0) * ey - (Y - y0) * ex) / L * -1.0
        d = np.minimum(d, di)
    return smoothstep(-aa * 0.5, aa * 0.5, d).astype(np.float32)


def harmonic_fill(img, hole, iters=400):
    """Bouche les pixels hole par diffusion depuis le bord (aucun dessin : seuls les pixels voisins servent)."""
    out = img.copy()
    m = hole.astype(bool)
    # initialisation : convolution normalisée
    k = (~m).astype(np.float32)
    for r in (2, 4, 8, 16):
        num = blur(out * k[..., None], r); den = blur(k, r)[..., None]
        est = num / np.maximum(den, 1e-4)
        out = np.where(m[..., None] & (den > 0.05), est, out)
    for _ in range(iters):
        nb = (np.roll(out, 1, 0) + np.roll(out, -1, 0) + np.roll(out, 1, 1) + np.roll(out, -1, 1)) * 0.25
        out = np.where(m[..., None], nb, out)
    return out


def regrain(filled, src, hole, ref_mask, r=3.0, seed=0):
    """Rend au remplissage le grain de la plaque : les hautes fréquences de pixels de la plaque elle-même (ref_mask),
    recopiées par blocs décalés (aucune texture inventée)."""
    hp = src - blur(src, r)
    ys, xs = np.nonzero(ref_mask)
    rng = np.random.default_rng(seed)
    out = filled.copy()
    H, W = hole.shape
    hy, hx = np.nonzero(hole)
    if len(hy) == 0 or len(ys) == 0:
        return out
    B = 6
    for y in range(hy.min(), hy.max() + 1, B):
        for x in range(hx.min(), hx.max() + 1, B):
            blk = hole[y:y + B, x:x + B]
            if not blk.any():
                continue
            for _ in range(30):
                i = rng.integers(len(ys)); sy, sx = ys[i], xs[i]
                if sy + B <= H and sx + B <= W and ref_mask[sy:sy + B, sx:sx + B].all():
                    break
            else:
                continue
            bh, bw = blk.shape
            out[y:y + bh, x:x + bw] += hp[sy:sy + bh, sx:sx + bw] * blk[..., None]
    return np.clip(out, 0, 1)


def empty_plate(img, box, deg=2, thr=0.022, grow=1, extra=None):
    """Vide une plaque de cartouche de son texte peint : par ligne, ajustement robuste (Tukey) d'un polynôme en x sur
    les pixels de la plaque, puis remplacement des pixels qui s'en écartent (lettres, contour sombre, lueur).
    Renvoie (image, masque du texte)."""
    x0, y0, x1, y1 = box
    out = img.copy()
    sub = img[y0:y1, x0:x1]
    hh, ww = sub.shape[:2]
    xs = np.linspace(-1, 1, ww)
    A = np.stack([xs ** k for k in range(deg + 1)], 1)
    fit = np.zeros_like(sub)
    txt = np.zeros((hh, ww), bool)
    L = gray(sub)
    for y in range(hh):
        wgt = np.ones(ww)
        for _ in range(8):
            Aw = A * wgt[:, None]
            p, *_ = np.linalg.lstsq(Aw, L[y] * wgt, rcond=None)
            res = L[y] - A @ p
            c = 4.685 * 1.4826 * (np.median(np.abs(res[wgt > 0])) + 2e-3)
            wgt = np.where(np.abs(res) < c, (1 - (res / c) ** 2) ** 2, 0.0)
        txt[y] = np.abs(res) > thr
        for ch in range(3):
            pc, *_ = np.linalg.lstsq(A * wgt[:, None], sub[y, :, ch] * wgt, rcond=None)
            fit[y, :, ch] = A @ pc
    if extra is not None:
        txt |= extra[y0:y1, x0:x1]
    txt = dilate(txt.astype(np.float32), grow) > 0.5
    # lissage vertical léger du modèle (les lignes voisines se ressemblent)
    fit = blur(fit, 1.0)
    soft = blur(txt.astype(np.float32), 0.8)
    soft = np.maximum(soft, txt)
    out[y0:y1, x0:x1] = sub * (1 - soft[..., None]) + fit * soft[..., None]
    full = np.zeros(img.shape[:2], bool); full[y0:y1, x0:x1] = txt
    return out, full


def empty_plate2(img, box, thr=0.03, iters=5, extra=None, seed=1):
    """Vide une plaque de cartouche : modèle séparable robuste couleur(x, y) = ligne(y) + colonne(x) (médianes sur les
    pixels propres, colonne lissée), le texte (lettres, contour sombre, lueur) est ce qui s'en écarte. Les pixels du
    texte reçoivent le modèle plus le grain de la plaque pris sur la même ligne, ailleurs (aucune texture inventée)."""
    x0, y0, x1, y1 = box
    sub = img[y0:y1, x0:x1].astype(np.float32)
    hh, ww = sub.shape[:2]
    L = gray(sub)
    med = np.median(L)
    txt = (np.abs(L - med) > 0.05)
    if extra is not None:
        txt |= extra[y0:y1, x0:x1]
    txt = dilate(txt.astype(np.float32), 1) > 0.5
    model = np.zeros_like(sub)
    for it in range(iters):
        for c in range(3):
            ch = sub[..., c]
            col = np.zeros(ww, np.float32)
            for _ in range(3):
                row = np.array([np.median((ch[y] - col)[~txt[y]]) if (~txt[y]).sum() > 5 else np.nan for y in range(hh)], np.float32)
                ok = ~np.isnan(row)
                row = np.interp(np.arange(hh), np.nonzero(ok)[0], row[ok])
                col = np.array([np.median((ch[:, x] - row)[~txt[:, x]]) if (~txt[:, x]).sum() > 3 else np.nan for x in range(ww)], np.float32)
                ok = ~np.isnan(col)
                col = np.interp(np.arange(ww), np.nonzero(ok)[0], col[ok])
                col = blur(col[None, :], 5.0)[0]
            model[..., c] = row[:, None] + col[None, :]
        res = gray(sub) - gray(model)
        new = np.abs(res) > thr
        if extra is not None:
            new |= extra[y0:y1, x0:x1]
        new = dilate(new.astype(np.float32), 1) > 0.5
        if (new == txt).all():
            break
        txt = new
    # grain : résidu de la plaque, pris sur la même ligne à un décalage propre
    resid = sub - model
    rng = np.random.default_rng(seed)
    grain = np.zeros_like(sub)
    for y in range(hh):
        clean = np.nonzero(~txt[y])[0]
        if len(clean) < 4:
            continue
        for x in np.nonzero(txt[y])[0]:
            grain[y, x] = resid[y, clean[rng.integers(len(clean))]]
    grain = grain * 0.7
    soft = np.maximum(blur(txt.astype(np.float32), 0.7), txt)
    fill = np.clip(model + grain, 0, 1)
    out = img.copy()
    out[y0:y1, x0:x1] = sub * (1 - soft[..., None]) + fill * soft[..., None]
    full = np.zeros(img.shape[:2], bool); full[y0:y1, x0:x1] = txt
    return out, full


def robust_color_fit(src, ref, mask, iters=6):
    """Couleur : gain et décalage par canal, ajustés avec des poids de Tukey (les zones dont le contenu diffère sont des
    valeurs aberrantes et ne comptent pas)."""
    m = mask.astype(bool)
    out = np.empty_like(src)
    for c in range(3):
        x = src[..., c][m].astype(np.float64); y = ref[..., c][m].astype(np.float64)
        if x.size > 300000:
            idx = np.random.default_rng(0).choice(x.size, 300000, replace=False); x = x[idx]; y = y[idx]
        wgt = np.ones_like(x)
        p = np.array([1.0, 0.0])
        for _ in range(iters):
            A = np.stack([x, np.ones_like(x)], 1) * wgt[:, None]
            p, *_ = np.linalg.lstsq(A, y * wgt, rcond=None)
            r = y - (x * p[0] + p[1])
            cst = 4.685 * 1.4826 * (np.median(np.abs(r)) + 1e-4)
            wgt = np.where(np.abs(r) < cst, (1 - (r / cst) ** 2) ** 2, 0.0)
        out[..., c] = src[..., c] * p[0] + p[1]
    return out


def tone_curve(src, ref, mask, n=101):
    """Courbe de tons par canal (correspondance des quantiles de src sur ref dans mask), monotone. Renvoie une fonction
    qui l'applique à une image."""
    m = mask.astype(bool)
    qs = np.linspace(0, 1, n)
    curves = []
    for c in range(3):
        x = np.quantile(src[..., c][m], qs); y = np.quantile(ref[..., c][m], qs)
        x = np.maximum.accumulate(x + np.arange(n) * 1e-6)
        y = np.maximum.accumulate(y)
        # prolongement : au-delà du dernier quantile, pente 1 jusqu'au blanc
        x = np.concatenate([[0.0], x, [1.0]]); y = np.concatenate([[0.0], y, [min(1.0, y[-1] + (1.0 - x[-2]))]])
        curves.append((x, y))
    def apply(img):
        out = np.empty_like(img)
        for c in range(3):
            out[..., c] = np.interp(img[..., c], *curves[c])
        return out
    return apply, curves


def tone_curve_lum(src, ref, mask, tint_mask=None, n=101):
    """Courbe de tons sur la luminance seule (quantiles de src -> ref dans mask), appliquée comme un gain commun aux
    trois canaux (le grain ne change pas de teinte), puis gain de teinte global calé sur tint_mask."""
    m = mask.astype(bool)
    qs = np.linspace(0, 1, n)
    Ls, Lr = gray(src)[m], gray(ref)[m]
    x = np.quantile(Ls, qs); y = np.quantile(Lr, qs)
    x = np.maximum.accumulate(x + np.arange(n) * 1e-6); y = np.maximum.accumulate(y)
    x = np.concatenate([[0.0], x, [1.0]]); y = np.concatenate([[0.0], y, [min(1.0, y[-1] + (1.0 - x[-2]))]])
    tm = (tint_mask if tint_mask is not None else mask).astype(bool)
    def base(img):
        L = gray(img)
        g = np.interp(L, x, y) / np.maximum(L, 1e-4)
        return img * g[..., None]
    t0 = base(src)
    tint = np.median(ref[tm], 0) / np.maximum(np.median(t0[tm], 0), 1e-4)
    tint = tint / max(float(gray(tint[None, None])[0, 0]), 1e-4)
    def apply(img):
        return base(img) * tint[None, None, :]
    return apply, (x.tolist(), y.tolist(), tint.tolist())
