# -*- coding: utf-8 -*-
"""Planches de contrôle du fondu des personnages assis (campement, 2026-11-22).

1. Campement_Fondu_Places.jpg : chaque place (configurations A et B : chaque personnage passe par 2 places) à côté de la
   même zone de la maquette d'origine, avant (PR #19) et après, avec les mesures sous chaque vignette.
2. Campement_Fondu_Bords.jpg : zoom x3 (pixels agrandis sans lissage) sur un bord de chaque personnage (queue, cape) et
   sur sa lanterne, avant et après.
3. Campement_Fondu_Mesures.md : tableau des mesures avant / après pour les 16 couples personnage x place.

Usage : python planche_fondu.py <dossier des captures après (cfg_A..D.png)> <capture avant A> <capture avant B>
        <mesures avant A.json> <mesures avant B.json> <dossier des sprites avant> <construction.txt avant>
"""
import json, os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg
import fondu

ROOT = fondu.ROOT
OUT = os.path.join(ROOT, "docs/art/rendus/UI/Menus")
MAQ = os.path.join(ROOT, "docs/art/ui/maquettes/ui_campement_v01.png")
POL = "C:/Windows/Fonts/segoeui.ttf"
ZONES = {1: (360, 280, 720, 700), 2: (620, 280, 980, 700), 3: (930, 250, 1310, 700), 4: (1230, 250, 1640, 700)}
NOMS = {"Renard": "renard", "Koala": "koala", "Cameleon": "caméléon", "Hibou": "hibou"}


def police(t):
    return ImageFont.truetype(POL, t)


def txt_mes(v):
    p, d = v["personnage"], v["decor"]
    return ["perso  lum %.0f  sat %.2f  net %.2f" % (p["lum"], p["sat"], p["nettete"]),
            "décor  lum %.0f  sat %.2f  net %.2f" % (d["lum"], d["sat"], d["nettete"])]


def planche_places(apres, avant, mes_av, mes_ap):
    M = Image.open(MAQ).convert("RGB")
    sc = 0.78
    pw, ph = int(380 * sc), int(450 * sc)
    lab = 62
    W = 2 * (3 * pw + 2 * 8) + 40 + 30
    H = 70 + 4 * (ph + lab + 34) + 10
    o = Image.new("RGB", (W, H), (18, 17, 22))
    d = ImageDraw.Draw(o)
    d.text((14, 10), "Campement : personnages assis fondus dans le décor peint (maquette | avant, PR #19 | après)", font=police(24), fill=(255, 232, 200))
    d.text((14, 40), "Configurations A (renard, hibou, koala, caméléon) et B (hibou, renard, caméléon, koala) : chaque personnage à 2 places. Captures Unity gelées.",
           font=police(15), fill=(190, 185, 200))
    for ci, cfg in enumerate("AB"):
        occ = fondu.CONFIGS[cfg]
        X0 = 14 + ci * (3 * pw + 2 * 8 + 40)
        for place in range(1, 5):
            perso = fondu.PERSOS[occ[place - 1]]
            y = 70 + (place - 1) * (ph + lab + 34)
            d.text((X0, y), "Place %d : %s (config. %s)" % (place, NOMS[perso], cfg), font=police(17), fill=(240, 220, 190))
            z = ZONES[place]
            for k, (nom, im) in enumerate((("maquette", M), ("avant", avant[cfg]), ("après", apres[cfg]))):
                c = im.crop(z)
                c = c.resize((pw, int(round((z[3] - z[1]) * pw / (z[2] - z[0])))), Image.LANCZOS)
                x = X0 + k * (pw + 8)
                o.paste(c, (x, y + 26))
                d.text((x + 6, y + 30), nom, font=police(15), fill=(255, 255, 255))
                mes = (mes_av if nom == "avant" else mes_ap if nom == "après" else None)
                if mes is not None:
                    v = mes[cfg]["place_%d" % place]
                    for li, t in enumerate(txt_mes(v)):
                        d.text((x + 2, y + 26 + c.size[1] + 4 + li * 18), t, font=police(13), fill=(200, 200, 210))
    o.save(os.path.join(OUT, "Campement_Fondu_Places.jpg"), quality=90)


def point_bord(perso, rect, H, W):
    """Point du contour à zoomer : le plus à gauche dans la moitié basse de la silhouette (queue, bas de cape)."""
    m = fondu.masque(perso, rect, H, W) > 0.5
    x, y, w, h = rect
    m[:int(y + h * 0.45)] = False
    ys, xs = np.nonzero(m)
    i = int(np.argmin(xs))
    return int(xs[i]) + 18, int(ys[i])


def planche_bords(apres, avant, constr_av):
    P_ap = fondu.places()
    old = fondu.CONSTR
    fondu.CONSTR = constr_av
    P_av = fondu.places()
    fondu.CONSTR = old
    occ = fondu.CONFIGS["A"]
    z, k = 64, 3
    t = z * k
    W = 14 + 4 * (t + 10) + 10
    H = 80 + 4 * (t + 40)
    o = Image.new("RGB", (W, H), (18, 17, 22))
    d = ImageDraw.Draw(o)
    d.text((14, 10), "Bords et lanternes, zoom x3 (pixels agrandis sans lissage), configuration A", font=police(22), fill=(255, 232, 200))
    d.text((14, 40), "Colonnes : bord avant | bord après | lanterne avant | lanterne après", font=police(15), fill=(190, 185, 200))
    for i, place in enumerate(range(1, 5)):
        perso = fondu.PERSOS[occ[place - 1]]
        y = 80 + i * (t + 40)
        d.text((14, y - 2), "Place %d : %s" % (place, NOMS[perso]), font=police(16), fill=(240, 220, 190))
        Wc, Hc = apres["A"].size
        bx, by = point_bord(perso, P_ap[(perso, place)][0:4], Hc, Wc)
        tuiles = []
        for im, P in ((avant["A"], P_av), (apres["A"], P_ap)):
            tuiles.append(im.crop((bx - z // 2, by - z // 2, bx + z // 2, by + z // 2)))
        for im, P in ((avant["A"], P_av), (apres["A"], P_ap)):
            lx, ly, lw, lh = P[(perso, place)][4:8]
            cx, cy = int(lx + lw / 2), int(ly + lh * 0.62)
            tuiles.append(im.crop((cx - z // 2, cy - z // 2, cx + z // 2, cy + z // 2)))
        for j, tu in enumerate(tuiles):
            o.paste(tu.resize((t, t), Image.NEAREST), (14 + j * (t + 10), y + 22))
    o.save(os.path.join(OUT, "Campement_Fondu_Bords.jpg"), quality=92)


def tableau(dossier_ap, mes_av_par_cfg):
    av = {}
    for cfg, r in mes_av_par_cfg.items():
        for k, v in r.items():
            av["%s_%s" % (v["perso"], k.split("_")[1])] = v
    ap = json.load(open(os.path.join(dossier_ap, "mesures.json"), encoding="utf-8"))
    L = ["# Campement : mesures du fondu des personnages assis (2026-11-22)", "",
         "Mesures sur les captures Unity gelées (1670 x 941), par `fondu.py` :", "",
         "- **personnage** : pixels du personnage à plus de 3 px de son contour ;",
         "- **décor** : anneau de 6 à 40 px autour de la silhouette, pris dans le décor peint seul (`Campement_Fond.png`), hors feu, cartouches et autres personnages ;",
         "- **lum** : luminance moyenne (0 à 255) ; **sat** : saturation moyenne ((max − min) / max) ;",
         "- **net** (netteté, acuité) : détails à 1 px divisés par détails à 3 px ; elle mesure la finesse, pas la quantité de traits ;",
         "- **détail** : contraste moyen des détails à 1 px (0 à 255), donné pour information ;",
         "- **lanterne** : pixels de la lanterne hors verre allumé ; son décor est l'anneau de 5 à 22 px autour d'elle.", "",
         "« Avant » : PR #19 (anciens sprites et placements). « Après » : cette PR.", "",
         "**Lecture.** Luminosité et saturation : égales à celles du décor à moins de 1 % près (16 couples). Netteté : les sprites "
         "réduits sont déjà un peu plus doux que le décor (acuité 0,52 à 0,61 contre 0,61 à 0,68) ; l'égaler demanderait de les "
         "renforcer, à l'inverse de la douceur demandée : douceur fixe de 0,5 px (contour adouci sur 1 px, grain de la peinture). "
         "Le contraste des traits (détail) reste celui du dessin, 2 à 3 fois celui du décor autour, **comme sur la maquette** "
         "(renard et hibou de la maquette : détail 11 et 14, acuité 0,68 et 0,64, pour un décor à 2,5 à 3 et 0,62 à 0,68) : "
         "l'atténuer pour l'égaler rendait les personnages ternes et boueux (essai rejeté, voir la PR).", "",
         "Places 3 et 4 de la configuration « avant » : seules les configurations A et B ont été capturées avant ; « — » = pas de mesure avant.", "",
         "| Personnage, place | lum avant | lum après | lum décor | sat avant | sat après | sat décor | net avant | net après | net décor | détail après / décor |",
         "|---|---|---|---|---|---|---|---|---|---|---|"]
    for k in sorted(ap):
        a, b = av.get(k), ap[k]
        p, d = b["personnage"], b["decor"]
        if a is None:
            L.append("| %s | — | %.1f | %.1f | — | %.3f | %.3f | — | %.3f | %.3f | %.1f / %.1f |" % (k.replace("_", ", place "), p["lum"], d["lum"], p["sat"], d["sat"], p["nettete"], d["nettete"], p["detail"], d["detail"]))
        else:
            pa = a["personnage"]
            L.append("| %s | %.1f | %.1f | %.1f | %.3f | %.3f | %.3f | %.3f | %.3f | %.3f | %.1f / %.1f |" % (k.replace("_", ", place "), pa["lum"], p["lum"], d["lum"], pa["sat"], p["sat"], d["sat"], pa["nettete"], p["nettete"], d["nettete"], p["detail"], d["detail"]))
    L += ["", "| Lanterne, place | lum avant | lum après | lum décor | sat avant | sat après | sat décor |", "|---|---|---|---|---|---|---|"]
    for k in sorted(ap):
        a, b = av.get(k), ap[k]
        p, d = b["lanterne"], b["decor_lanterne"]
        if a is None:
            L.append("| %s | — | %.1f | %.1f | — | %.3f | %.3f |" % (k.replace("_", ", place "), p["lum"], d["lum"], p["sat"], d["sat"]))
        else:
            pa = a["lanterne"]
            L.append("| %s | %.1f | %.1f | %.1f | %.3f | %.3f | %.3f |" % (k.replace("_", ", place "), pa["lum"], p["lum"], d["lum"], pa["sat"], p["sat"], d["sat"]))
    open(os.path.join(OUT, "Campement_Fondu_Mesures.md"), "w", encoding="utf-8").write("\n".join(L) + "\n")


if __name__ == "__main__":
    d_ap, av_a, av_b, m_a, m_b, spr_av, constr_av = sys.argv[1:8]
    apres = {c: Image.open(os.path.join(d_ap, "cfg_%s.png" % c)).convert("RGB") for c in "AB"}
    avant = {"A": Image.open(av_a).convert("RGB"), "B": Image.open(av_b).convert("RGB")}
    mes_av = {"A": json.load(open(m_a, encoding="utf-8")), "B": json.load(open(m_b, encoding="utf-8"))}
    mes_ap = {c: fondu.mesure(os.path.join(d_ap, "cfg_%s.png" % c), fondu.CONFIGS[c]) for c in "AB"}
    planche_places(apres, avant, mes_av, mes_ap)
    planche_bords(apres, avant, constr_av)
    tableau(d_ap, mes_av)
    print("planches écrites dans", OUT)
