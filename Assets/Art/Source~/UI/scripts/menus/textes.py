# -*- coding: utf-8 -*-
"""Mesure des textes peints des maquettes (bord gauche, droit, haut des capitales, ligne de base, couleur) : les textes
Unity sont posés à ces emplacements et dans ce style. Sortie : Assets/Art/Source~/UI/scripts/menus/textes.json."""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
M = os.path.join(ROOT, "docs/art/ui/maquettes")
BOITES = {
    "ui_menu_principal_v01": {"Jouer": (137, 440, 290, 506), "Settings": (120, 560, 330, 620), "Quitter": (120, 650, 330, 715)},
    "ui_campement_v01": {"RenardRouge": (464, 684, 652, 720), "HibouLune": (740, 682, 866, 716), "Modifier": (540, 734, 630, 762)},
    "ui_choix_perso_v01": {"Titre1": (950, 460, 1210, 488), "Desc1a": (950, 489, 1216, 511), "Desc1b": (950, 511, 1216, 533),
                           "Titre2": (950, 553, 1216, 581), "Desc2a": (950, 583, 1216, 605), "Desc2b": (950, 605, 1216, 628),
                           "Valider": (760, 820, 920, 862)},
}


def mesure(img, box, seuil=0.16):
    x0, y0, x1, y1 = box
    sub = img[y0:y1, x0:x1]
    L = reg.gray(sub)
    bg = np.median(L)
    m = L > bg + seuil
    ys, xs = np.nonzero(m)
    rows = m.sum(1).astype(float)
    mx = rows.max()
    core = np.nonzero(rows > 0.33 * mx)[0]
    top = int(np.nonzero(rows > 0.08 * mx)[0][0])
    col = sub[m]
    return {"x0": int(x0 + xs.min()), "x1": int(x0 + xs.max() + 1), "haut": int(y0 + top), "base": int(y0 + core.max() + 1),
            "bas": int(y0 + ys.max() + 1), "xhaut": int(y0 + core.min()),
            "couleur": [round(float(v), 3) for v in np.percentile(col, 85, axis=0)], "fond": round(float(bg), 3)}


def main():
    out = {}
    for n, bx in BOITES.items():
        img = reg.load(os.path.join(M, n + ".png"))
        out[n] = {k: mesure(img, b) for k, b in bx.items()}
    p = os.path.join(os.path.dirname(os.path.abspath(__file__)), "textes.json")
    json.dump(out, open(p, "w", encoding="utf-8"), indent=1)
    for n, v in out.items():
        for k, m in v.items():
            print(n[:12], k, m)


if __name__ == "__main__":
    main()
