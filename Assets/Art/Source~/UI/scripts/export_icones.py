import glob, os, numpy as np
from PIL import Image
from uilib import *
for f in glob.glob("out/*_src.png"):
    n = os.path.basename(f)[:-8]
    a = to_float(Image.open(f))
    if n == "Porte_Verrouillee":                      # bois trop sombre à 24 px : éclairci, trait noir conservé
        v = a[..., :3].max(-1); k = np.clip((v - 22) / 30, 0, 1)[..., None]
        a[..., :3] = a[..., :3] * (1 - k) + np.clip(a[..., :3] * 1.55 + 10, 0, 255) * k
    S = 48 if n.startswith("Joueur") else 96          # 2x la taille max à l'écran (24 / 48 px)
    m = 2; r = resize_rgba(a, size=(S - 2 * m, S - 2 * m))
    out = np.zeros((S, S, 4), np.float32); out[m:S - m, m:S - m] = r
    save_f(out, "out/MiniCarte/MiniCarte_%s.png" % n)
print(sorted(os.listdir("out/MiniCarte")))
