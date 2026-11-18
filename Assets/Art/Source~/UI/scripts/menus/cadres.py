# -*- coding: utf-8 -*-
"""Cadre des menus pause et paramètres : recomposé en 9 tranches à partir du cadre de la maquette ui_choix_perso_v01
(les 4 coins tels quels, des tronçons droits des rails, un morceau du panneau sombre pour le centre). Détourage : le
métal est clair et net sur un fond flou sombre (luminance), ses traits d'encre sont repris par une dilatation ; le
dedans du rail intérieur est opaque (panneau). Aucun nouveau dessin.
Sortie : Assets/Art/UI/Menus/Commun/Menus_Cadre.png (+ bordures dans Commun_Layout.json).

Usage : python cadres.py [dossier_controle]
"""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
UI = os.path.join(ROOT, "docs/art/ui")
OUT = os.path.join(ROOT, "Assets/Art/UI/Menus/Commun")
CTRL = sys.argv[1] if len(sys.argv) > 1 else None

C = 100          # taille d'un coin (px)
RAIL = 8         # tranches droites (raccordables), prises juste à côté de chaque coin
# coins (coin haut gauche du carré de C px) dans la maquette
COINS = {"hg": (392, 22), "hd": (1290 - C, 22), "bg": (392, 905 - C), "bd": (1290 - C, 905 - C)}
# tronçons droits (début) : haut, bas (x, y du coin haut gauche, longueur RAIL en x) ; gauche, droite (longueur en y)
# tranches contiguës aux coins dans la maquette : alignées par construction
RAILS = {"haut": (392 + C, 22), "bas": (392 + C, 905 - C), "gauche": (392, 22 + C), "droite": (1290 - C, 22 + C)}
CENTRE = (560, 720)   # morceau du panneau (RAIL x RAIL)
# limite du dedans (panneau opaque) dans un coin, mesurée depuis le bord extérieur du coin : rail intérieur
DEDANS = 40


def metal_alpha(img):
    L = reg.gray(img)
    hp = L - reg.blur(L, 3.0)
    a = reg.smoothstep(0.14, 0.26, L)
    a = np.maximum(a, reg.smoothstep(0.05, 0.12, hp) * reg.smoothstep(0.08, 0.16, L))
    ink = reg.dilate(a, 1)
    return np.clip(np.maximum(a, ink * 0.9), 0, 1)


def main():
    os.makedirs(OUT, exist_ok=True)
    # source : l'écran de choix composé (cadre de la maquette, intérieur vide)
    T = reg.load(os.path.join(ROOT, "Assets/Art/UI/Menus/Choix/Choix_Fond.png"))
    h, w = T.shape[:2]
    A = metal_alpha(T)
    # dedans opaque : tout ce qui est à l'intérieur du rail intérieur du panneau
    inside = reg.rect_mask(h, w, 392 + DEDANS, 22 + DEDANS, 1290 - DEDANS, 905 - DEDANS)
    A = np.maximum(A, inside)
    S = C * 2 + RAIL
    out = np.zeros((S, S, 4), np.float32)
    def put(dst_xy, src_xy, ww, hh, flip=False):
        x, y = src_xy; dx, dy = dst_xy
        c = T[y:y + hh, x:x + ww]; a = A[y:y + hh, x:x + ww]
        if flip:
            c = c[::-1]; a = a[::-1]
        out[dy:dy + hh, dx:dx + ww, :3] = c
        out[dy:dy + hh, dx:dx + ww, 3] = a
    put((0, 0), COINS["hg"], C, C)
    put((C + RAIL, 0), COINS["hd"], C, C)
    put((0, C + RAIL), COINS["bg"], C, C)
    put((C + RAIL, C + RAIL), COINS["bd"], C, C)
    put((C, 0), RAILS["haut"], RAIL, C)
    put((C, C + RAIL), RAILS["bas"], RAIL, C)
    put((0, C), RAILS["gauche"], C, RAIL)
    put((C + RAIL, C), RAILS["droite"], C, RAIL)
    put((C, C), CENTRE, RAIL, RAIL)
    # dans les tranches, le dedans (panneau) est pris au morceau du centre : pas de lueur de vignette étirée
    cx, cy = CENTRE
    patch = T[cy:cy + RAIL, cx:cx + RAIL]
    tile = np.tile(patch, (S // RAIL + 1, S // RAIL + 1, 1))[:S, :S]
    ins = np.zeros((S, S), np.float32)
    ins[DEDANS:S - DEDANS, DEDANS:S - DEDANS] = 1
    ins = reg.blur(ins, 1.0)
    strips = np.zeros((S, S), np.float32)
    strips[:, C:C + RAIL] = 1; strips[C:C + RAIL, :] = 1
    k = (ins * strips)[..., None]
    out[..., :3] = out[..., :3] * (1 - k) + tile * k
    out[C:C + RAIL, C:C + RAIL, 3] = 1
    reg.save(os.path.join(OUT, "Menus_Cadre.png"), out, "RGBA")
    lay = {"Menus_Cadre": {"taille": [S, S], "bordures_glbh": [C, C, C, C], "source": "ui_choix_perso_v01",
                            "coins": COINS, "rails": RAILS, "centre": CENTRE}}
    # cartouche vidé en 4 morceaux (boutons longs) : bout gauche, tranche droite raccordable, fleuron central, bout droit
    cart = reg.load_rgba(os.path.join(ROOT, "Assets/Art/UI/Menus/Campement/Campement_Cartouche_Nom.png"))
    ch, cw = cart.shape[:2]
    cx = cw // 2
    G_, Fl = 34, 18          # largeur des bouts, demi-largeur du fleuron
    parts = {"G": cart[:, :G_], "D": cart[:, cw - G_:], "C": cart[:, cx - Fl:cx + Fl], "M": cart[:, G_ + 8:G_ + 16]}
    for k, v in parts.items():
        reg.save(os.path.join(OUT, "Menus_Cartouche_%s.png" % k), v, "RGBA")
    lay["Menus_Cartouche"] = {"hauteur": ch, "bout": G_, "fleuron": 2 * Fl, "tranche": 8, "source": "Campement_Cartouche_Nom"}
    # tache de lumière radiale (technique, pour les lueurs additives teintées)
    n = 128
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    d = np.hypot(xx - (n - 1) / 2, yy - (n - 1) / 2) / ((n - 1) / 2)
    g = np.clip(np.exp(-d * d * 3.2) - np.exp(-3.2), 0, 1) / (1 - np.exp(-3.2))
    reg.save(os.path.join(OUT, "Menus_Lueur.png"), np.stack([np.ones_like(g)] * 3 + [g], -1), "RGBA")
    with open(os.path.join(OUT, "Commun_Layout.json"), "w", encoding="utf-8") as f:
        json.dump(lay, f, indent=1)
    if CTRL:
        chk = np.zeros((S, S, 3), np.float32) + 0.6
        reg.save(os.path.join(CTRL, "cadre.png"), out[..., :3] * out[..., 3:] + chk * (1 - out[..., 3:]), "RGB")
    print(lay)


if __name__ == "__main__":
    main()
