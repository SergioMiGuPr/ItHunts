# -*- coding: utf-8 -*-
"""Campement : mesure du fondu des personnages assis dans le décor peint (lot du 2026-11-22).

Pour chaque place occupée d'une capture Unity (1670 x 941, gelée), compare le personnage au décor peint qui l'entoure :
- personnage : alpha du sprite posé dans son rect (construction.txt), rongé de 3 px (le bord n'est pas compté) ;
- décor : anneau de 6 à 30 px autour de la silhouette, pris dans Campement_Fond.png (le décor peint seul), hors feu,
  cartouches et autres personnages.
Mesures (valeurs sRGB 0..255) :
- luminosité : moyenne de la luminance (Rec. 709) ;
- saturation : moyenne de (max - min) / max ;
- netteté (acuité) : énergie des détails à 1 px divisée par celle à 3 px, |L - flou(L, 1)| / |L - flou(L, 3)| ; elle ne
  dépend pas de la quantité de traits (un personnage encré a bien plus de détails que le mur du fond) mais de leur
  finesse : plus elle est basse, plus l'image est douce ;
- détail : moyenne de |L - flou(L, 1 px)| (contraste des détails fins, donné pour information).

Usage : python fondu.py <capture.png> <occupants "0,3,1,2"> [sortie.json]
        python fondu.py --ajuste <dossier des captures cfg_A.png .. cfg_D.png>   (met à jour fondu_reglages.json)
        python fondu.py --mesure <dossier>   (mesures.json seul)
La lanterne est mesurée hors verre allumé (pixels du sprite de luminance linéaire < 0,25).
"""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
CAMP = os.environ.get("FONDU_CAMP", os.path.join(ROOT, "Assets/Art/UI/Menus/Campement"))   # sprites (état « avant » : autre dossier)
CONSTR = os.environ.get("FONDU_CONSTR", os.path.join(os.path.dirname(os.path.abspath(__file__)), "construction.txt"))
PERSOS = ["Renard", "Koala", "Cameleon", "Hibou"]


def places():
    out = {}
    for ln in open(CONSTR, encoding="utf-8"):
        t = ln.split()
        if t and t[0] == "place":
            out[(t[1], int(t[2]))] = [float(v) for v in t[3:]]
    return out


def masque(perso, rect, H, W, lanterne=False):
    """Alpha du sprite posé dans son rect (pixels de la maquette)."""
    spr = reg.load_rgba(os.path.join(CAMP, "Campement_Assis_%s%s.png" % (perso, "_Lanterne" if lanterne else "")))
    x, y, w, h = rect
    a = reg.resize(spr[..., 3:4], max(int(round(w)), 1), max(int(round(h)), 1))[..., 0]
    m = np.zeros((H, W), np.float32)
    x0, y0 = int(round(x)), int(round(y))
    hh, ww = a.shape
    ys, xs = slice(max(y0, 0), min(y0 + hh, H)), slice(max(x0, 0), min(x0 + ww, W))
    m[ys, xs] = a[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
    return m


def masque_verre(perso, rect, H, W):
    spr = reg.load_rgba(os.path.join(CAMP, "Campement_Assis_%s_Lanterne.png" % perso))
    lum = reg.gray(reg.to_lin(spr[..., :3])) * (spr[..., 3] > 0.5)
    x, y, w, h = rect
    a = reg.resize(lum[..., None], max(int(round(w)), 1), max(int(round(h)), 1))[..., 0]
    m = np.zeros((H, W), np.float32)
    x0, y0 = int(round(x)), int(round(y))
    hh, ww = a.shape
    ys, xs = slice(max(y0, 0), min(y0 + hh, H)), slice(max(x0, 0), min(x0 + ww, W))
    m[ys, xs] = a[ys.start - y0:ys.stop - y0, xs.start - x0:xs.stop - x0]
    return reg.dilate((m > 0.25).astype(np.float32), 2)


def stats(img, sel):
    L = reg.gray(img)
    S, V = reg.sat(img)
    hf = np.abs(L - reg.blur(L, 1.0))
    hf3 = np.abs(L - reg.blur(L, 3.0))
    return {"lum": float(L[sel].mean() * 255), "sat": float(S[sel].mean()), "nettete": float(hf[sel].mean() / max(hf3[sel].mean(), 1e-6)),
            "detail": float(hf[sel].mean() * 255), "px": int(sel.sum())}


def mesure(capture, occupants):
    C = reg.load(capture)[..., :3]
    H, W = C.shape[:2]
    F = reg.load(os.path.join(CAMP, "Campement_Fond.png"))[..., :3]
    P = places()
    exclu = np.zeros((H, W), bool)
    exclu[470:840, 846:1130] = True   # feu
    exclu[655:, :] = True             # cartouches, boutons
    ms = {}
    for i, o in enumerate(occupants):
        if o < 0:
            continue
        v = P[(PERSOS[o], i + 1)]
        ms[i] = (PERSOS[o], masque(PERSOS[o], v[0:4], H, W), masque(PERSOS[o], v[4:8], H, W, True))
    tous = np.zeros((H, W), bool)
    for _, m, ml in ms.values():
        tous |= (m > 0.05) | (ml > 0.05)
    res = {}
    for i, (nom, m, ml) in ms.items():
        coeur = reg.erode((m > 0.9).astype(np.float32), 3) > 0.5
        coeur &= ~exclu
        bin_ = (m > 0.5).astype(np.float32)
        anneau = (reg.dilate(bin_, 40) > 0.5) & ~(reg.dilate(bin_, 6) > 0.5) & ~exclu & ~tous
        lc = reg.erode((ml > 0.9).astype(np.float32), 2) > 0.5
        lc &= ~(masque_verre(nom, P[(nom, i + 1)][4:8], H, W) > 0.3)
        la = (reg.dilate((ml > 0.5).astype(np.float32), 22) > 0.5) & ~(reg.dilate((ml > 0.5).astype(np.float32), 5) > 0.5) & ~tous
        la[655:, :] = False
        res["place_%d" % (i + 1)] = {"perso": nom, "personnage": stats(C, coeur), "decor": stats(F, anneau),
                                    "lanterne": stats(C, lc), "decor_lanterne": stats(F, la)}
    return res


CONFIGS = {"A": [0, 3, 1, 2], "B": [3, 0, 2, 1], "C": [1, 2, 3, 0], "D": [2, 1, 0, 3]}
REGLAGES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "fondu_reglages.json")


def ajuste(dossier, ecrire=True):
    """Une itération : mesure les 4 captures, corrige luminosité (gain ~ rapport^2,2 en sRGB), saturation (rapport) et
    douceur (netteté), pour chaque personnage à chaque place. Écrit aussi le tableau des mesures."""
    regl = json.load(open(REGLAGES, encoding="utf-8"))
    idx = {(r["perso"], r["place"]): r for r in regl["reglages"]}
    tab = {}
    for cfg, occ in CONFIGS.items():
        res = mesure(os.path.join(dossier, "cfg_%s.png" % cfg), occ)
        for k, v in res.items():
            place = int(k.split("_")[1])
            r = idx[(v["perso"], place)]
            p, d, lp, ld = v["personnage"], v["decor"], v["lanterne"], v["decor_lanterne"]
            tab["%s_%d" % (v["perso"], place)] = v
            r["lum"] = round(float(np.clip(r["lum"] * (d["lum"] / p["lum"]) ** 2.0, 0.2, 4.0)), 4)
            r["sat"] = round(float(np.clip(r["sat"] * (d["sat"] / max(p["sat"], 1e-3)) ** 1.2, 0.1, 2.0)), 4)
            # acuité : baisse quand la douceur monte (pente mesurée ~0,13 par pixel de rayon) ; douceur minimale 0,5 px
            # netteté (acuité) : les sprites réduits sont déjà un peu plus doux que le décor (0,54 à 0,63 contre 0,61 à
            # 0,68) ; l'égaler demanderait de les renforcer. Douceur fixe de 0,5 px : contour adouci et grain de la
            # peinture, sans flou visible. Le contraste des traits (encre) reste celui du dessin, comme sur la maquette
            # (personnages de la maquette : détail 11 à 14 pour 2,5 à 3 dans le décor, à acuité égale).
            r["douceur"] = 0.5
            r["detail"] = 1.0
            r["lanterneLum"] = round(float(np.clip(r["lanterneLum"] * (ld["lum"] / max(lp["lum"], 1)) ** 2.0, 0.2, 4.0)), 4)
            r["lanterneSat"] = round(float(np.clip(r["lanterneSat"] * (ld["sat"] / max(lp["sat"], 1e-3)) ** 1.2, 0.1, 2.0)), 4)
    if ecrire:
        json.dump(regl, open(REGLAGES, "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    json.dump(tab, open(os.path.join(dossier, "mesures.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    ecarts = []
    for k, v in sorted(tab.items()):
        p, d = v["personnage"], v["decor"]
        ecarts.append((abs(p["lum"] / d["lum"] - 1), abs(p["sat"] / d["sat"] - 1), abs(p["nettete"] / d["nettete"] - 1), abs(p["detail"] / d["detail"] - 1)))
        print("%-12s lum %5.1f/%5.1f  sat %.3f/%.3f  net %5.3f/%5.3f | lanterne lum %5.1f/%5.1f sat %.3f/%.3f"
              % (k, p["lum"], d["lum"], p["sat"], d["sat"], p["nettete"], d["nettete"], v["lanterne"]["lum"],
                 v["decor_lanterne"]["lum"], v["lanterne"]["sat"], v["decor_lanterne"]["sat"]))
    e = np.array(ecarts)
    print("écart relatif max : lum %.1f %%, sat %.1f %%, netteté %.1f %%, détail %.1f %%" % tuple(e.max(0) * 100))


if __name__ == "__main__" and len(sys.argv) > 2 and sys.argv[1] in ("--ajuste", "--mesure"):
    ajuste(sys.argv[2], ecrire=sys.argv[1] == "--ajuste")   # --mesure : tableau seul, réglages inchangés
    sys.exit(0)

if __name__ == "__main__":
    occ = [int(v) for v in sys.argv[2].split(",")]
    r = mesure(sys.argv[1], occ)
    for k, v in r.items():
        p, d = v["personnage"], v["decor"]
        print("%s %-9s perso lum %5.1f sat %.3f net %5.2f | décor lum %5.1f sat %.3f net %5.2f | lanterne lum %5.1f sat %.3f net %5.2f | décor %5.1f %.3f %5.2f"
              % (k, v["perso"], p["lum"], p["sat"], p["nettete"], d["lum"], d["sat"], d["nettete"],
                 v["lanterne"]["lum"], v["lanterne"]["sat"], v["lanterne"]["nettete"],
                 v["decor_lanterne"]["lum"], v["decor_lanterne"]["sat"], v["decor_lanterne"]["nettete"]))
    if len(sys.argv) > 3:
        json.dump(r, open(sys.argv[3], "w", encoding="utf-8"), indent=1, ensure_ascii=False)
