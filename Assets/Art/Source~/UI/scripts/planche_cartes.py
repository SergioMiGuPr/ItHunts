# -*- coding: utf-8 -*-
"""Planche de contrôle du lot 6 (nuit 4) : bulle de la mini-carte et carte ouverte (16:9) avec les sprites réels (fond,
cadre, rose, cadre de légende, icônes de goule et de traqueur). Les salles sont un tracé d'exemple.
    python -I planche_cartes.py
"""
import os, sys
from PIL import Image, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
from PIL import ImageFont


class PL:   # police du jeu et couleurs des planches
    FOND, ENCRE = (18, 17, 20), (226, 214, 190)

    @staticmethod
    def police(t, s="Regular"):
        return ImageFont.truetype(os.path.join(ROOT, "Assets", "Art", "UI", "Polices", "EBGaramond", "EBGaramond-%s.ttf" % s), t)
MC = os.path.join(ROOT, "Assets", "Art", "UI", "MiniCarte") + os.sep
CA = os.path.join(ROOT, "Assets", "Art", "UI", "Carte") + os.sep


def salles(dr, ox, oy, s, col):
    for (x, y, w, h) in [(0, 0, 3, 2), (4, 0.5, 3, 2.5), (1, 3, 3, 2), (5.5, 3.5, 2, 2)]:
        dr.rectangle((ox + x * s, oy + y * s, ox + (x + w) * s, oy + (y + h) * s), outline=col, width=3)
    for a in [((3, 1), (4, 1.3)), ((2, 2), (2, 3)), ((6, 3), (6.4, 3.5))]:
        dr.line((ox + a[0][0] * s, oy + a[0][1] * s, ox + a[1][0] * s, oy + a[1][1] * s), fill=col, width=3)


def main():
    G = Image.open(MC + "MiniCarte_Goule.png"); T = Image.open(MC + "MiniCarte_Traqueur.png")
    o = Image.new("RGB", (1920, 1300), PL.FOND); d = ImageDraw.Draw(o)
    d.text((24, 14), "Lot 6 : mini-carte (bulle) et carte ouverte, un seul design ; goule (point rouge), traqueur (œil jauni fendu)", font=PL.police(28, "SemiBold"), fill=PL.ENCRE)
    # bulle : fond rond masqué, salles, icônes, cadre
    b = Image.open(MC + "MiniCarte_Fond.png").convert("RGBA").resize((420, 441))
    calque = Image.new("RGBA", b.size, (0, 0, 0, 0)); dr = ImageDraw.Draw(calque); salles(dr, 80, 120, 34, (206, 196, 176, 255))
    for (x, y) in [(150, 170), (250, 160), (130, 250), (290, 300)]: calque.alpha_composite(G.resize((18, 18)), (x, y))
    calque.alpha_composite(T.resize((40, 40)), (230, 240))
    calque.putalpha(Image.fromarray(__import__("numpy").minimum(__import__("numpy").asarray(calque.getchannel("A")), __import__("numpy").asarray(b.getchannel("A")))))
    b.alpha_composite(calque); b.alpha_composite(Image.open(MC + "MiniCarte_Cadre.png").convert("RGBA").resize((420, 441)))
    o.paste(b, (40, 110), b)
    d.text((40, 570), "Bulle du coin", font=PL.police(22), fill=PL.ENCRE)
    # carte ouverte 16:9 : fond, cadre 9 tranches simplifié (coins + filets), rose, légende
    W, H = 1360, 765
    c = Image.open(CA + "Carte_Fond.png").convert("RGBA").resize((W, H))
    cad = Image.open(CA + "Carte_Cadre.png").convert("RGBA")
    n = cad.width; k = 260; s = 0.55
    def piece(box, size): return cad.crop(box).resize(size, Image.LANCZOS)
    kk = int(k * s)
    c.alpha_composite(piece((0, 0, k, k), (kk, kk)), (0, 0)); c.alpha_composite(piece((n - k, 0, n, k), (kk, kk)), (W - kk, 0))
    c.alpha_composite(piece((0, n - k, k, n), (kk, kk)), (0, H - kk)); c.alpha_composite(piece((n - k, n - k, n, n), (kk, kk)), (W - kk, H - kk))
    c.alpha_composite(piece((k, 0, n - k, k), (W - 2 * kk, kk)), (kk, 0)); c.alpha_composite(piece((k, n - k, n - k, n), (W - 2 * kk, kk)), (kk, H - kk))
    c.alpha_composite(piece((0, k, k, n - k), (kk, H - 2 * kk)), (0, kk)); c.alpha_composite(piece((n - k, k, n, n - k), (kk, H - 2 * kk)), (W - kk, kk))
    dr = ImageDraw.Draw(c); salles(dr, 220, 180, 64, (206, 196, 176, 255))
    for (x, y) in [(300, 230), (520, 250), (290, 420), (640, 470), (440, 400)]: c.alpha_composite(G.resize((22, 22)), (x, y))
    c.alpha_composite(T.resize((48, 48)), (560, 330))
    c.alpha_composite(Image.open(CA + "Carte_Rose.png").convert("RGBA").resize((120, 120)), (110, 100))
    lg = Image.open(CA + "Carte_Legende_Cadre.png").convert("RGBA")
    lg.thumbnail((300, 300)); c.alpha_composite(lg, (W - 360, H - 330))
    dd = ImageDraw.Draw(c)
    for i, (im, lab) in enumerate([(T.resize((30, 30)), "Traqueur"), (G.resize((18, 18)), "Goule")]):
        c.alpha_composite(im, (W - 330 + (30 - im.width) // 2, H - 300 + i * 44 + (30 - im.height) // 2))
        dd.text((W - 285, H - 300 + i * 44), lab, font=PL.police(22), fill=PL.ENCRE)
    c = c.resize((int(W * 0.95), int(H * 0.95)), Image.LANCZOS)
    o.paste(c, (520, 110), c)
    d.text((520, 850), "Carte ouverte (Tab), 16:9 : même fond, même fer épineux (coins et rose), mêmes icônes", font=PL.police(22), fill=PL.ENCRE)
    for i, (im, lab) in enumerate([(T, "Traqueur, 128 px"), (T.resize((40, 40)), "40 px"), (T.resize((28, 28)), "28 px"), (G, "Goule, 64 px"), (G.resize((18, 18)), "18 px")]):
        x = 40 + i * 300; y = 960
        bg = Image.new("RGBA", (140, 140), (19, 19, 25, 255)); bg.alpha_composite(im, ((140 - im.width) // 2, (140 - im.height) // 2))
        o.paste(bg, (x, y)); d.text((x, y + 150), lab, font=PL.police(22), fill=PL.ENCRE)
    o.save(os.path.join(ROOT, "docs", "art", "rendus", "Nuit4", "Lot6", "Cartes_Unifiees.jpg"), quality=90)


if __name__ == "__main__":
    main()
