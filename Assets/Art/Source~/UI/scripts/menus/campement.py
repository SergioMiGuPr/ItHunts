# -*- coding: utf-8 -*-
"""Campement : la maquette ui_campement_v01 comme base. Dans les zones qui changent (personnages et leurs lanternes,
interface, lanternes des places 3 et 4, feu), le fond vient de ui_campement_vide_sans_lanternes_v01 recalé et raccordé
en couleur. Flammes = passe1 - braises ; lanternes éteintes = braises - vide ; interface = maquette - passe1 ;
personnages assis détourés du fond gris, mis à l'échelle du renard et du hibou de la maquette, relookés à la lumière de
la scène. Sorties : Assets/Art/UI/Menus/Campement/ + Campement_Layout.json (pixels de la maquette, 1670 x 941).

Usage : python campement.py [cache] [dossier_controle]
"""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
UI = os.path.join(ROOT, "docs/art/ui")
OUT = os.path.join(ROOT, "Assets/Art/UI/Menus/Campement")
CACHE = sys.argv[1] if len(sys.argv) > 1 else None
CTRL = sys.argv[2] if len(sys.argv) > 2 else None

# ---- zones qui changent (x0, y0, x1, y1), pixels de la maquette
ZONES = {
    "renard": (420, 300, 720, 690),
    "lanterne_renard": (466, 548, 556, 694),
    "hibou": (672, 316, 900, 640),
    "lanterne_hibou": (884, 474, 958, 612),
    "cartouche_renard": (436, 668, 676, 782),
    "cartouche_hibou": (712, 668, 934, 732),
    "lanterne_banc": (1234, 478, 1316, 610),
    "lanterne_rocher": (1498, 488, 1582, 634),
    "plus_3": (1138, 642, 1232, 738),
    "plus_4": (1418, 642, 1512, 738),
    "pret": (1272, 772, 1666, 941),
    "retour": (4, 10, 72, 74),
    "icones": (1538, 10, 1668, 74),
    "feu": (840, 540, 1130, 836),
}
# halos colorés des lanternes de la maquette (centre x, y, demi-axes)
HALOS = [(510, 640, 105, 80), (918, 560, 80, 75)]
SEUIL_ZONE = (0.07, 0.16)
FEU = (846, 470, 1126, 836)       # rect des flammes (étincelles comprises)
FOYER_X, FOYER_BASE_Y = 990, 772  # pied des flammes
CHATEAU = (1180, 0, 1670, 300)
LANTERNE_TENTE = (448, 318, 26, 32)
# cartouches de nom : rect de la pièce, zone du texte peint (à vider)
CARTOUCHES = {
    # hexagone (sens horaire depuis la pointe gauche), fleurons haut et bas (triangles), plaque intérieure à vider (None :
    # texte gardé). Le cartouche du renard porte la lueur rouge de sa lanterne : celui du hibou sert pour les 4 places.
    "Campement_Cartouche_Nom": ([(721, 696), (738, 674), (909, 674), (927, 696), (910, 718), (738, 718)],
                                [[(800, 676), (823, 664), (846, 676)], [(806, 716), (823, 729), (840, 716)]],
                                (740, 680, 908, 714)),
    "Campement_Modifier": ([(477, 747), (487, 729), (626, 729), (636, 747), (625, 767), (488, 767)],
                           [[(548, 731), (557, 723), (566, 731)], [(549, 765), (558, 772), (567, 765)]],
                           None),
}
# rect du cartouche de nom à chaque place (pixels de la maquette ; places 1 et 2 mesurées, 3 et 4 sous leur siège)
PLACES_CARTOUCHE = {1: (442, 664, 232, 68), 2: (721, 664, 206, 65), 3: (1082, 664, 206, 65), 4: (1358, 664, 206, 65)}
COCHE = (866, 680, 908, 722)
# pièces d'interface : rect, seuils de différence, bouchage des trous
UI_PIECES = {
    "Campement_Plus_3": ((1138, 642, 1232, 738), 0.05, 0.16, True),
    "Campement_Plus_4": ((1418, 642, 1512, 738), 0.05, 0.16, True),
    "Campement_Pret": ((1272, 772, 1666, 941), 0.07, 0.2, True),
    "Campement_Retour": ((4, 10, 72, 74), 0.05, 0.16, False),
    "Campement_Parametres": ((1538, 10, 1604, 74), 0.05, 0.16, False),
    "Campement_Amis": ((1604, 10, 1668, 74), 0.05, 0.16, False),
}   # écart (0..1) maquette / vide au-delà duquel le pixel est remplacé
MARGE_ZONE = 7              # dilatation du masque (px)


def load_reg(name, T, cache):
    """Image générée recalée sur la maquette (cache des matrices pour gagner du temps)."""
    G = reg.load(os.path.join(UI, "generees", name + ".png"))
    h, w = T.shape[:2]
    mp = os.path.join(cache, name + "_M.npy") if cache else None
    if mp and os.path.exists(mp):
        M = np.load(mp)
    else:
        M0, _, _ = reg.init_affine(T, G)
        M = reg.refine_affine(T, G, M0)
        if mp:
            np.save(mp, M)
    return reg.warp(G, M, h, w), M, G


def main():
    os.makedirs(OUT, exist_ok=True)
    T = reg.load(os.path.join(UI, "maquettes/ui_campement_v01.png"))
    h, w = T.shape[:2]
    lay = {"ecran": "Campement", "taille": [w, h], "pieces": {}, "recalage": {}}
    V, Mv, _ = load_reg("ui_campement_vide_sans_lanternes_v01", T, CACHE)
    P1, Mp, _ = load_reg("ui_campement_passe1_v01", T, CACHE)
    BR, Mb, _ = load_reg("ui_campement_braises_v01", T, CACHE)
    for k, M in (("vide", Mv), ("passe1", Mp), ("braises", Mb)):
        lay["recalage"][k] = M.tolist()

    # ---------------- 1. fond : maquette, sauf dans les zones qui changent
    Vb = reg.blur(V, 1.2); Tb = reg.blur(T, 1.2)
    d = reg.err_map(Tb, Vb)
    zone_all = np.zeros((h, w), np.float32)
    for k, (x0, y0, x1, y1) in ZONES.items():
        roi = reg.rect_mask(h, w, x0, y0, x1, y1)
        m = (reg.smoothstep(*SEUIL_ZONE, d) * roi) > 0.5
        m = reg.erode(reg.dilate(m.astype(np.float32), 4), 4) > 0.5      # fermeture
        sub = m[y0:y1, x0:x1]
        m[y0:y1, x0:x1] = reg.fill_holes(sub) if sub.any() else sub
        zone_all = np.maximum(zone_all, reg.blur(reg.dilate(m.astype(np.float32), MARGE_ZONE), 3.0))
    # halos des lanternes de la maquette (rouge, bleu) : remplacés largement, la lueur sera un effet
    X, Y = reg.grid(h, w)
    for (cx, cy, rx, ry) in HALOS:
        zone_all = np.maximum(zone_all, reg.ellipse_mask(h, w, cx, cy, rx, ry, feather=0.55))
    # raccord : champ de gain lisse estimé hors des zones (et hors des halos)
    est = reg.dilate((zone_all > 0.02).astype(np.float32), 18) < 0.5
    gain = reg.gain_field(V, T, est, r=40.0)
    Vc = np.clip(V * gain, 0, 1)
    fond = T * (1 - zone_all[..., None]) + Vc * zone_all[..., None]
    # foyer : les braises (référence des flammes) à la place du vide
    gainb = reg.gain_field(BR, T, est, r=40.0)
    BRc = np.clip(BR * gainb, 0, 1)
    fx0, fy0, fx1, fy1 = ZONES["feu"]
    zf = reg.rect_mask(h, w, fx0 + 10, fy0 + 10, fx1 - 10, fy1 - 10, feather=24) * zone_all
    fond = fond * (1 - zf[..., None]) + BRc * zf[..., None]
    ring = (reg.dilate((zone_all > 0.02).astype(np.float32), 10) > 0.5) & (zone_all <= 0.02)
    lay["raccord"] = reg.report(T, Vc, ring)
    reg.save(os.path.join(OUT, "Campement_Fond.png"), fond, "RGB")
    lay["pieces"]["Campement_Fond"] = [0, 0, w, h]
    np.save(os.path.join(CACHE, "camp_fond.npy"), fond) if CACHE else None

    L = reg.gray(T)
    X, Y = reg.grid(h, w)

    # ---------------- 2. flammes = passe1 - braises (additif), masquées autour du foyer
    fx0, fy0, fx1, fy1 = FEU
    dl = reg.gray(P1) - reg.gray(BR)
    fl = reg.smoothstep(0.05, 0.20, reg.blur(dl, 1.5)) * reg.rect_mask(h, w, *FEU, feather=12)
    fl = reg.blur(reg.dilate(fl, 2), 2.0)
    add = reg.to_srgb(reg.to_lin(reg.additive(P1, BR)) * fl[..., None])
    lay["pieces"]["Campement_Flammes"] = reg.save_piece(os.path.join(OUT, "Campement_Flammes.png"), add, np.ones((h, w), np.float32), FEU)
    lay["foyer"] = {"base_y": FOYER_BASE_Y, "centre_x": FOYER_X}

    # ---------------- 3. lanternes éteintes = braises - vide (places 3 et 4)
    for k, box in (("Campement_Lanterne_Banc", ZONES["lanterne_banc"]), ("Campement_Lanterne_Rocher", ZONES["lanterne_rocher"])):
        F, a_ = reg.cut_piece(BR, V, box, 0.035, 0.10, r=1.0, min_frac=0.03, grow=2)
        lay["pieces"][k] = reg.save_piece(os.path.join(OUT, k + ".png"), F, a_, reg.bbox(a_, pad=3))

    # ---------------- 4. interface = maquette - passe1
    for k, (box, t0, t1, fill) in UI_PIECES.items():
        F, a_ = reg.cut_piece(T, P1, box, t0, t1, r=1.0, min_frac=0.03, grow=2, fill=fill)
        lay["pieces"][k] = reg.save_piece(os.path.join(OUT, k + ".png"), F, a_, reg.bbox(a_, pad=3))

    # ---------------- 4 bis. cartouches de nom : contour mesuré sur la maquette (hexagone et fleurons), bord adouci par la
    # différence ; puis texte peint retiré
    for k, (poly, fleurons, tbox) in CARTOUCHES.items():
        shape = reg.poly_mask(h, w, np.array(poly, np.float64), aa=1.2)
        for fl_ in fleurons:
            shape = np.maximum(shape, reg.poly_mask(h, w, np.array(fl_, np.float64), aa=1.2))
        allowed = reg.dilate(shape, 3)
        da = reg.diff_alpha(T, P1, 0.07, 0.2, r=0.8)
        a_ = allowed * np.maximum(da, reg.erode(shape, 1))
        box = reg.bbox(a_, pad=2)
        piece = {"plein": reg.save_piece(os.path.join(OUT, k + "_Plein.png"), T, a_, box)}
        if tbox is None:
            lay["pieces"][k] = piece.pop("plein")
            os.replace(os.path.join(OUT, k + "_Plein.png"), os.path.join(OUT, k + ".png"))
            continue
        blue = (T[..., 2] - T[..., 0]) > 0.06
        E, txt = reg.empty_plate2(T, tbox, extra=blue)
        os.remove(os.path.join(OUT, k + "_Plein.png"))
        tx0, ty0, tx1, ty1 = tbox
        piece["vide"] = reg.save_piece(os.path.join(OUT, k + ".png"), E, a_, box)
        piece["zone_texte"] = [tx0, ty0, tx1 - tx0, ty1 - ty0]
        piece.pop("plein", None)
        lay["places_cartouche"] = PLACES_CARTOUCHE
        lay["pieces"][k] = piece["vide"]
        lay.setdefault("cartouches", {})[k] = piece
        if k == "Campement_Cartouche_Nom":
            # coche (prêt) : maquette - cartouche vidé
            cb = COCHE
            ac = reg.diff_alpha(T, E, 0.03, 0.12, r=0.6, roi=reg.rect_mask(h, w, *cb, feather=2))
            lay["pieces"]["Campement_Coche"] = reg.save_piece(os.path.join(OUT, "Campement_Coche.png"), reg.unmix(T, E, ac), ac, cb)

    # ---------------- 5. masques d'effets (R fenêtres du château, G lanterne de la tente, B lueur du feu au sol)
    r, g, b = T[..., 0], T[..., 1], T[..., 2]
    S_, V_ = reg.sat(T)
    win = reg.smoothstep(0.10, 0.30, r - g) * reg.smoothstep(0.30, 0.55, r) * reg.rect_mask(h, w, *CHATEAU, feather=20)
    tente = reg.ellipse_mask(h, w, *LANTERNE_TENTE, feather=0.8) * reg.smoothstep(0.2, 0.6, V_)
    dist = np.hypot((X - FOYER_X) / 1.0, (Y - FOYER_BASE_Y) / 0.62)
    warm = reg.smoothstep(0.02, 0.18, r - b)
    lueur = (1 - reg.smoothstep(60, 520, dist)) * warm * (1 - fl)
    pack = np.stack([reg.blur(win, 1.2), reg.blur(tente, 1.5), reg.blur(lueur, 3.0), np.ones((h, w), np.float32)], -1)
    reg.save(os.path.join(OUT, "Campement_Effets_Masques.png"), pack, "RGBA")
    lay["pieces"]["Campement_Effets_Masques"] = [0, 0, w, h]

    print(json.dumps(lay["raccord"], ensure_ascii=False))
    if CTRL:
        reg.save(os.path.join(CTRL, "camp_fond.png"), fond, "RGB")
        reg.save(os.path.join(CTRL, "camp_zones.png"), T * 0.5 + np.stack([zone_all, zone_all * 0, zone_all * 0], -1) * 0.5, "RGB")
    with open(os.path.join(OUT, "Campement_Layout.json"), "w", encoding="utf-8") as f:
        json.dump(lay, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
