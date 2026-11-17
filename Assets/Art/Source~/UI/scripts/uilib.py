# Outils de détourage sans scipy : dilatation, érosion, bouchage des trous, flou.
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

def disk(r):
    y, x = np.mgrid[-r:r+1, -r:r+1]
    return [(dy, dx) for dy, dx in zip(y.ravel(), x.ravel()) if dx*dx + dy*dy <= r*r + r*0.5]

def dilate(m, r):
    if r <= 0: return m.copy()
    H, W = m.shape; p = np.pad(m, r); out = np.zeros_like(m)
    for dy, dx in disk(r):
        out |= p[r+dy:r+dy+H, r+dx:r+dx+W]
    return out

def erode(m, r):
    return ~dilate(~m, r)

def fill_holes(m):
    """Remplit les zones fausses entourées par m (flood fill du fond depuis les bords)."""
    H, W = m.shape
    im = Image.fromarray(np.pad(~m, 1, constant_values=True).astype(np.uint8) * 255, "L").copy()
    ImageDraw.floodfill(im, (0, 0), 128)
    reach = np.array(im)[1:-1, 1:-1] == 128
    return ~reach

def keep_big(m, min_px):
    """Retire les composantes connexes plus petites que min_px (étiquetage par flood fill)."""
    H, W = m.shape; lab = np.zeros((H, W), np.int32); out = np.zeros_like(m); n = 0
    ys, xs = np.nonzero(m)
    im = Image.fromarray(m.astype(np.uint8) * 255, "L").copy()
    seen = np.zeros_like(m)
    for y, x in zip(ys, xs):
        if seen[y, x]: continue
        n += 1; v = 1 + (n % 250)
        tmp = im.copy(); ImageDraw.floodfill(tmp, (int(x), int(y)), 77)
        comp = np.array(tmp) == 77
        seen |= comp
        if comp.sum() >= min_px: out |= comp
    return out

def soft(m, rad=1.0):
    return np.array(Image.fromarray(m.astype(np.uint8) * 255, "L").filter(ImageFilter.GaussianBlur(rad))) / 255.0

def bbox(a, pad=0):
    ys, xs = np.nonzero(a)
    return max(0, xs.min()-pad), max(0, ys.min()-pad), xs.max()+1+pad, ys.max()+1+pad

def save_rgba(rgb, alpha, path):
    a = np.clip(alpha * 255, 0, 255).astype(np.uint8)
    rgb = rgb.astype(np.uint8).copy(); rgb[a == 0] = 0
    Image.fromarray(np.dstack([rgb, a]), "RGBA").save(path, optimize=True)

def checker(w, h, s=16, c1=(70, 70, 76), c2=(110, 110, 118)):
    y, x = np.mgrid[0:h, 0:w]; m = ((x // s + y // s) % 2) == 0
    a = np.where(m[..., None], c1, c2).astype(np.uint8); return Image.fromarray(a, "RGB")

def resize_rgba(arr, scale=None, size=None):
    """Redimensionne en alpha prémultiplié (pas de frange sombre). arr : float HxWx4, alpha 0..1, rgb 0..255."""
    H, W = arr.shape[:2]
    if size is None: size = (max(1, round(W * scale)), max(1, round(H * scale)))
    pm = arr.copy(); pm[..., :3] *= pm[..., 3:4]
    chans = [np.array(Image.fromarray(pm[..., i].astype(np.float32), "F").resize(size, Image.LANCZOS)) for i in range(4)]
    out = np.dstack(chans); a = np.clip(out[..., 3], 0, 1)
    rgb = np.where(a[..., None] > 1e-4, out[..., :3] / np.maximum(a[..., None], 1e-4), 0)
    return np.dstack([np.clip(rgb, 0, 255), a])

def to_float(img):
    a = np.array(img.convert("RGBA")).astype(np.float32); a[..., 3] /= 255.0; return a

def save_f(arr, path):
    save_rgba(arr[..., :3], arr[..., 3], path)

def noise(h, w, seed, scales=(2, 6, 18)):
    rng = np.random.default_rng(seed); out = np.zeros((h, w), np.float32)
    for s in scales:
        n = rng.random((max(2, h // s + 2), max(2, w // s + 2))).astype(np.float32)
        out += np.array(Image.fromarray(n, "F").resize((w, h), Image.BICUBIC)) / len(scales)
    return out

def tileable(arr, x0, w, k):
    """Segment [x0, x0+w) rendu raccordable : les k premières colonnes fondues vers [x0+w, x0+w+k)."""
    seg = arr[:, x0:x0 + w].copy(); nxt = arr[:, x0 + w:x0 + w + k]
    t = np.linspace(0, 1, k, dtype=np.float32)[None, :, None]
    seg[:, :k] = nxt * (1 - t) + seg[:, :k] * t
    return seg

def components(m, min_px=1):
    from PIL import ImageDraw
    im = Image.fromarray(m.astype(np.uint8) * 255, "L").copy(); seen = np.zeros_like(m); out = []
    for y, x in zip(*np.nonzero(m)):
        if seen[y, x]: continue
        t = im.copy(); ImageDraw.floodfill(t, (int(x), int(y)), 77); c = np.array(t) == 77; seen |= c
        if c.sum() >= min_px: out.append(c)
    return out
