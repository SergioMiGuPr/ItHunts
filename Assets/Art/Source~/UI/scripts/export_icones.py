import glob, os, numpy as np
from PIL import Image, ImageFilter
from uilib import *
for f in glob.glob("out/*_src.png"):
    n = os.path.basename(f)[:-8]
    a = to_float(Image.open(f))
    if n == "Porte_Verrouillee":                      # bois trop sombre à 24 px : éclairci, trait noir conservé
        v = a[..., :3].max(-1); k = np.clip((v - 22) / 30, 0, 1)[..., None]
        a[..., :3] = a[..., :3] * (1 - k) + np.clip(a[..., :3] * 1.55 + 10, 0, 255) * k
    if n == "Flamme":                                 # violet -> gris-bleu des âmes (#647A8D, #7EC2DB, #E0FAFD) selon la luminosité
        L = a[..., :3].max(-1) / 255.0
        lo, mi, hi = np.array([96, 116, 134.]), np.array([128, 172, 196.]), np.array([224, 250, 253.])
        body = (a[..., 3] > 0.5) & (L > 0.2)
        Ls = np.array(Image.fromarray((L * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(2.5))) / 255.0
        p5, p95 = np.percentile(Ls[body], [5, 95])
        t = (np.clip((Ls - p5) / (p95 - p5), 0, 1) ** 2.0)[..., None]
        g = np.where(t < 0.5, lo + (mi - lo) * (t / 0.5), mi + (hi - mi) * ((t - 0.5) / 0.5))
        dark = np.clip((L - 0.08) / 0.22, 0, 1)[..., None]          # le trait noir reste noir
        a[..., :3] = a[..., :3] * (1 - dark) + g * dark
    S = 48 if n.startswith("Joueur") else 96          # 2x la taille max à l'écran (24 / 48 px)
    m = 2; r = resize_rgba(a, size=(S - 2 * m, S - 2 * m))
    out = np.zeros((S, S, 4), np.float32); out[m:S - m, m:S - m] = r
    save_f(out, "out/MiniCarte/MiniCarte_%s.png" % n)
print(sorted(os.listdir("out/MiniCarte")))
