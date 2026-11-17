"""Comble les texels noirs (sans surface d'origine en face : calotte des cheveux retirés) à l'intérieur des îlots d'UV
du corps, par diffusion depuis la peau voisine, puis légère texture de bruit pour ne pas laisser une tache lisse."""
import numpy as np
from PIL import Image, ImageDraw
D = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad\traq2"
im = np.asarray(Image.open(D + r"\T_Traqueur_Corps_BaseColor.png").convert("RGB"), np.float32) / 255
H, W_ = im.shape[:2]
T = np.load(D + r"\uv_corps.npy")
mask = Image.new("L", (W_, H), 0); dr = ImageDraw.Draw(mask)
for t in T:
    dr.polygon([(u * W_, (1 - v) * H) for u, v in t], fill=255)
cov = np.asarray(mask) > 0
black = cov & (im.max(2) < 0.002)
print("texels couverts %d, noirs %d (%.1f %%)" % (cov.sum(), black.sum(), 100 * black.sum() / cov.sum()))
known = cov & (im.max(2) > 0.25)   # sources : la peau seule (pas le pagne sombre)
out = im.copy(); out[~known] = 0
w = known.astype(np.float32)
# diffusion : moyennes de voisinage répétées, à résolution réduite puis pleine
def box(a, r):
    c = np.cumsum(np.cumsum(np.pad(a, ((r + 1, r), (r + 1, r)) + ((0, 0),) * (a.ndim - 2)), 0), 1)
    return c[2 * r + 1:, 2 * r + 1:] - c[:-2 * r - 1, 2 * r + 1:] - c[2 * r + 1:, :-2 * r - 1] + c[:-2 * r - 1, :-2 * r - 1]
fill = out.copy(); fw = w.copy()
for r in (2, 4, 8, 16, 32, 64):
    s = box(fill * fw[..., None], r); n = box(fw, r)
    upd = (fw < 0.5) & (n > 1e-3)
    fill[upd] = (s[upd] / n[upd][:, None]); fw = np.where(upd, 1.0, fw)
res = np.where(black[..., None], fill, im)
# grain de peinture sur la zone comblée (bruit doux à la valeur)
rng = np.random.default_rng(5)
g = rng.normal(0, 1, (H // 8, W_ // 8)).astype(np.float32)
g = np.asarray(Image.fromarray(((g * 0.5 + 0.5).clip(0, 1) * 255).astype(np.uint8)).resize((W_, H), Image.BICUBIC), np.float32) / 255 - 0.5
res = np.where(black[..., None], np.clip(res * (1 + 0.18 * g[..., None]), 0, 1), res)
Image.fromarray((res * 255).round().astype(np.uint8)).save(D + r"\T_Traqueur_Corps_BaseColor_comble.png")
print("ok")
