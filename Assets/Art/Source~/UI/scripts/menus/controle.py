# -*- coding: utf-8 -*-
"""Contrôle avant PR : pour chaque écran, capture Unity à l'arrêt (effets gelés) à côté de la maquette au même cadrage,
superposition à 50 %, carte de l'écart, et écart mesuré hors texte variable et hors personnages redessinés (zones
exclues hachurées). Planches et mesures : docs/art/rendus/UI/Menus/.

Usage : python controle.py <dossier des captures>
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
OUT = os.path.join(ROOT, "docs/art/rendus/UI/Menus")
CAP = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Temp/menus")

# zones exclues de la mesure : (x0, y0, x1, y1, raison)
ECRANS = {
    "Titre": ("ui_menu_principal_v01", "Titre_fige.png", [
        (128, 446, 300, 512, "texte"), (118, 560, 382, 622, "texte"), (118, 650, 302, 714, "texte")]),
    "Campement": ("ui_campement_v01", "Campement_fige.png", [
        (420, 296, 724, 694, "personnage"), (668, 312, 962, 642, "personnage"),
        (446, 684, 672, 724, "texte"), (726, 682, 924, 722, "texte"),
        (70, 10, 300, 82, "ajout"), (24, 852, 444, 916, "ajout")]),
    "Choix": ("ui_choix_perso_v01", "Choix_Renard.png", [
        (436, 236, 838, 680, "personnage"), (900, 452, 1250, 640, "texte"), (470, 660, 1230, 800, "retiré")]),
}


def font(sz):
    for f in ("C:/Windows/Fonts/segoeui.ttf", "C:/Windows/Fonts/arial.ttf"):
        if os.path.exists(f):
            return ImageFont.truetype(f, sz)
    return ImageFont.load_default()


def hatch(img, box, col=(255, 60, 200)):
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = box
    d.rectangle(box, outline=col, width=2)
    for k in range(x0 - (y1 - y0), x1, 14):
        d.line([(max(k, x0), y0 + max(0, x0 - k)), (min(k + (y1 - y0), x1), y0 + min(y1 - y0, x1 - k))], fill=col, width=1)


def main():
    os.makedirs(OUT, exist_ok=True)
    res = {}
    for ecran, (maq, cap, excl) in ECRANS.items():
        T = reg.load(os.path.join(ROOT, "docs/art/ui/maquettes", maq + ".png"))
        C = reg.load(os.path.join(CAP, cap))
        h, w = T.shape[:2]
        e = reg.err_map(T, C) * 255
        keep = np.ones((h, w), bool)
        for x0, y0, x1, y1, _ in excl:
            keep[y0:y1, x0:x1] = False
        def st(m):
            v = e[m]
            return {"moy": round(float(v.mean()), 2), "med": round(float(np.median(v)), 2), "p95": round(float(np.percentile(v, 95)), 2),
                    "pixels_egaux_pct": round(float((v < 1.0).mean() * 100), 1), "pixels_ecart_sup_16_pct": round(float((v > 16).mean() * 100), 2)}
        res[ecran] = {"maquette": maq, "capture": cap, "global": st(np.ones((h, w), bool)), "hors_texte_et_personnages": st(keep),
                      "zones_exclues": [{"rect": z[:4], "raison": z[4]} for z in excl]}
        # planche : maquette | capture / superposition 50 % | écart (x4)
        tw, th = w // 2, h // 2
        def tile(a):
            return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8)).resize((tw, th), Image.LANCZOS)
        heat = np.clip(e / 64.0, 0, 1)
        hm = np.stack([heat, heat * 0.6, heat * 0.2], -1) * 1.0 + reg.gray(T)[..., None] * 0.15
        tiles = [tile(T), tile(C), tile(T * 0.5 + C * 0.5), tile(hm)]
        board = Image.new("RGB", (tw * 2 + 30, th * 2 + 182), (18, 16, 20))
        dr = ImageDraw.Draw(board)
        f1, f2 = font(22), font(17)
        labels = ["Maquette (%s)" % maq, "Capture Unity à l'arrêt (effets gelés)", "Superposition 50 %", "Écart |maquette - capture| (x4), zones exclues hachurées"]
        for i, (t, lab) in enumerate(zip(tiles, labels)):
            x = 10 + (i % 2) * (tw + 10); y = 72 + (i // 2) * (th + 36)
            if i == 3:
                for z in excl:
                    hatch(t, tuple(v // 2 for v in z[:4]))
            board.paste(t, (x, y))
            dr.text((x, y - 26), lab, font=f2, fill=(230, 220, 210))
        g, k = res[ecran]["global"], res[ecran]["hors_texte_et_personnages"]
        dr.text((10, 6), "%s : contrôle avant PR" % ecran, font=f1, fill=(255, 235, 200))
        dr.text((10, th * 2 + 122), "Écart (0-255) global : moyen %.2f, médian %.2f, 95e centile %.2f   |   hors texte variable et personnages redessinés : "
                "moyen %.2f, médian %.2f, 95e centile %.2f, pixels identiques (<1) %.1f %%, écart > 16 : %.2f %%" % (
                    g["moy"], g["med"], g["p95"], k["moy"], k["med"], k["p95"], k["pixels_egaux_pct"], k["pixels_ecart_sup_16_pct"]),
                font=f2, fill=(230, 220, 210))
        board.save(os.path.join(OUT, "Controle_%s.jpg" % ecran), quality=90)
        print(ecran, json.dumps(k))
    with open(os.path.join(OUT, "Controle_mesures.json"), "w", encoding="utf-8") as f:
        json.dump(res, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
