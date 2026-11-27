# -*- coding: utf-8 -*-
"""Écran de choix du personnage : la maquette ui_choix_perso_v01 comme base (cadre, fond flouté du campement, bouton
Valider) ; l'intérieur du panneau vient de ui_choix_fond_v01 recalé (sans grand personnage, artefact, textes, skins).
Grands personnages = ui_choix_<perso> - fond (zone de gauche) ; artefacts : artefacts_choix.py (nuit 7) ; vignettes découpées de passe3 (renard illuminé, autres discrets ; renard discret pris
dans ui_choix_cameleon), version illuminée = vignette discrète + lueur du cadre du renard recolorée. Sorties :
Assets/Art/UI/Menus/Choix/ + Choix_Layout.json (pixels de la maquette, 1671 x 941).

Usage : python choix.py [cache] [dossier_controle]
"""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
UI = os.path.join(ROOT, "docs/art/ui")
OUT = os.path.join(ROOT, "Assets/Art/UI/Menus/Choix")
CACHE = sys.argv[1] if len(sys.argv) > 1 else None
CTRL = sys.argv[2] if len(sys.argv) > 2 else None

INTERIEUR = (424, 50, 1258, 872)        # intérieur du panneau (bord fondu sur le filet sombre du cadre)
FONDU_INT = 6
VALIDER_EXCL = (650, 786, 1022, 900)    # le bouton Valider reste celui de la maquette
GAUCHE = (436, 236, 838, 680)           # grand personnage et son reflet au sol
DROITE = (880, 226, 1250, 452)          # artefact et ses traînées
# vignettes : rect commun aux deux versions (lueur comprise), cadre intérieur (portrait) pour isoler la lueur du cadre
VIGNETTES = {"Renard": (500, 68, 680, 246), "Koala": (680, 68, 852, 246), "Cameleon": (852, 68, 1024, 246), "Hibou": (1024, 68, 1196, 246)}
PORTRAIT_INSET = 16
COULEURS = {"Renard": (0.87, 0.22, 0.25), "Koala": (0.33, 0.78, 0.32), "Cameleon": (0.90, 0.62, 0.20), "Hibou": (0.55, 0.82, 1.0)}
# zone de texte (entre les deux filets ornementaux) et styles mesurés
TEXTE = (900, 458, 1206, 636)
# zones qui changent : (rect, rempli entièrement) ; sinon masque par différence dans le rect
CONTENUS = [((436, 236, 840, 684), False),       # grand personnage et reflet
            ((876, 218, 1252, 456), False),      # artefact et traînées
            ((878, 455, 1252, 637), True),       # textes et pastilles (le filet du milieu de la maquette disparaît)
            ((462, 660, 1240, 802), True),       # rangée des skins et flèches
            ((498, 64, 1200, 250), True)]        # vignettes (recouvertes par les sprites)
# zones vides du panneau dans les deux images (apprentissage de la courbe de tons)
PANNEAU_VIDE = [(430, 250, 480, 600), (1205, 250, 1250, 440), (860, 460, 895, 640), (1210, 470, 1250, 640),
                (440, 700, 520, 760), (1140, 700, 1240, 780), (430, 800, 640, 860)]
# bouton Valider : contour mesuré (hexagone allongé et fleurons)
VALIDER = ([(657, 839), (694, 804), (984, 804), (1019, 839), (984, 873), (694, 873)],
           [[(812, 805), (838, 790), (864, 805)], [(814, 872), (838, 892), (862, 872)]])


def load_reg(name, T, cache):
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
    return reg.warp(G, M, h, w), M


def main():
    os.makedirs(OUT, exist_ok=True)
    T = reg.load(os.path.join(UI, "maquettes/ui_choix_perso_v01.png"))
    h, w = T.shape[:2]
    lay = {"ecran": "Choix", "taille": [w, h], "pieces": {}, "recalage": {}}
    imgs = {}
    for n in ("fond", "passe3", "cameleon", "renard", "koala", "hibou", "artefact_barillet"):
        imgs[n], M = load_reg("ui_choix_%s_v01" % n, T, CACHE)
        lay["recalage"][n] = M.tolist()
    FD = imgs["fond"]
    X, Y = reg.grid(h, w)

    # ---------------- 1. fond : maquette hors du panneau, intérieur du panneau repris de ui_choix_fond
    zone = reg.rect_mask(h, w, *INTERIEUR, feather=FONDU_INT)
    poly, fleurons = VALIDER
    vshape = reg.poly_mask(h, w, np.array(poly, np.float64), aa=1.2)
    for fl_ in fleurons:
        vshape = np.maximum(vshape, reg.poly_mask(h, w, np.array(fl_, np.float64), aa=1.2))
    zone *= 1 - reg.blur(reg.dilate(vshape, 5), 2.5)
    # couleur : le générateur a relevé les ombres (panneau 31/22/35 au lieu de 12/9/11). Courbe de tons apprise sur le
    # cadre (présent et aligné dans les deux images) et les zones vides du panneau, appliquée à toutes les images
    # générées de cet écran
    outer = reg.rect_mask(h, w, 398, 26, 1284, 898); inner = reg.rect_mask(h, w, 432, 60, 1250, 862)
    learn = (outer > 0.5) & (inner < 0.5) & (vshape < 0.01)
    for bx in PANNEAU_VIDE:
        learn |= reg.rect_mask(h, w, *bx) > 0.5
    vide = np.zeros((h, w), bool)
    for bx in PANNEAU_VIDE:
        vide |= reg.rect_mask(h, w, *bx) > 0.5
    curve, info_c = reg.tone_curve_lum(FD, T, learn, vide)
    lay["courbe_tons"] = {"x": np.round(info_c[0], 4).tolist(), "y": np.round(info_c[1], 4).tolist(), "teinte": info_c[2]}
    for k in list(imgs):
        imgs[k] = np.clip(curve(imgs[k]), 0, 1)
    FD = imgs["fond"]
    FDc = FD
    # base = la maquette ; le fond retouché ne remplit que les zones qui changent (grand personnage, artefact, textes,
    # rangée des skins, vignettes), par masque tiré de la différence, puis raccord par champ de gain lisse
    d = reg.err_map(reg.blur(T, 1.2), reg.blur(FD, 1.2))
    zone = np.zeros((h, w), np.float32)
    for bx, plein in CONTENUS:
        if plein:
            zone = np.maximum(zone, reg.rect_mask(h, w, *bx, feather=6))
            continue
        x0, y0, x1, y1 = bx
        m = (reg.smoothstep(0.06, 0.14, d) * reg.rect_mask(h, w, *bx)) > 0.5
        m = reg.erode(reg.dilate(m.astype(np.float32), 4), 4) > 0.5
        m[y0:y1, x0:x1] = reg.fill_holes(m[y0:y1, x0:x1]) if m[y0:y1, x0:x1].any() else m[y0:y1, x0:x1]
        zone = np.maximum(zone, reg.blur(reg.dilate(m.astype(np.float32), 8), 3.5) * reg.rect_mask(h, w, *bx, feather=4))
    est = (reg.dilate((zone > 0.02).astype(np.float32), 14) < 0.5) & (inner > 0.5)
    gain = reg.gain_field(FD, T, est, r=30.0)
    FDc = np.clip(FD * gain, 0, 1)
    # le panneau généré est plus marbré que celui de la maquette : grain fin ramené à l'écart type de la maquette
    lowF = reg.blur(FDc, 5.0); lowT = reg.blur(T, 5.0)
    ref = est & (reg.rect_mask(h, w, 430, 240, 1250, 870) > 0.5)
    sf = float(np.std(reg.gray(FDc - lowF)[ref])); stt = float(np.std(reg.gray(T - lowT)[ref]))
    FDc = np.clip(lowF + (FDc - lowF) * min(1.0, stt / max(sf, 1e-4)), 0, 1)
    lay["grain_panneau"] = {"ecart_type_genere": sf, "ecart_type_maquette": stt}
    fond = T * (1 - zone[..., None]) + FDc * zone[..., None]
    ring = (reg.dilate((zone > 0.02).astype(np.float32), 8) > 0.5) & (zone <= 0.02)
    lay["raccord"] = reg.report(T, FDc, ring)
    reg.save(os.path.join(OUT, "Choix_Fond.png"), fond, "RGB")
    lay["pieces"]["Choix_Fond"] = [0, 0, w, h]

    # ---------------- 2. grands personnages = choix_<perso> - fond, zone de gauche
    lay["personnages"] = {}
    for n, key in (("Renard", "renard"), ("Koala", "koala"), ("Cameleon", "cameleon"), ("Hibou", "hibou")):
        C = imgs[key]
        F, a = reg.cut_piece(C, FD, GAUCHE, 0.05, 0.14, r=1.2, min_frac=0.02, grow=3, fill=True, close=6)
        # la lueur au sol déborde du rect : fondu sur 30 px près des bords de la zone (pas de coupure nette)
        a = a * reg.rect_mask(h, w, *GAUCHE, feather=30)
        # lueur au sol et reflet : partie douce gardée (alpha < 1), couleur démêlée sur le fond
        box = reg.bbox(a, pad=4)
        r_ = reg.save_piece(os.path.join(OUT, "Choix_%s.png" % n), F, a, box)
        lay["pieces"]["Choix_" + n] = r_
        # masque de la lanterne (pixels lumineux et saturés) pour le vacillement, dans le rect du sprite
        S_, V_ = reg.sat(F)
        lm = reg.blur((reg.smoothstep(0.45, 0.8, V_) * reg.smoothstep(0.35, 0.7, S_) * a), 1.5)
        lm = np.maximum(lm, reg.blur(lm, 10.0) * 2.5)
        x0, y0, x1, y1 = box
        reg.save(os.path.join(OUT, "Choix_%s_Lueur.png" % n), np.clip(np.repeat(lm[y0:y1, x0:x1, None], 3, -1), 0, 1), "RGB")
        ys = np.nonzero((a[:, x0:x1] > 0.6).any(1))[0]
        lay["personnages"][n] = {"rect": r_, "pieds_y": int(ys.max()) if len(ys) else y1}

    # ---------------- 3. artefacts : refaits la nuit 7 par artefacts_choix.py (Choix_Art_*, cadre commun 850,192,400,300) ;
    # les anciennes découpes Choix_Artefact_Lanterne / _Barillet et leurs traînées ne sont plus produites.
    lay["artefacts"] = {}

    # ---------------- 4. vignettes
    P3 = imgs["passe3"]; CA = imgs["cameleon"]
    lay["vignettes"] = {}
    def soft_rect(box, f=6):
        return reg.rect_mask(h, w, *box, feather=f)
    for n, box in VIGNETTES.items():
        x0, y0, x1, y1 = box
        a = soft_rect(box)
        disc_src = CA if n == "Renard" else P3
        lay["pieces"]["Choix_Vignette_%s_Discrete" % n] = reg.save_piece(os.path.join(OUT, "Choix_Vignette_%s_Discrete.png" % n), disc_src, a, box)
    # lueur du cadre illuminé du renard : passe3 (illuminé) - caméléon (discret), hors du portrait
    rb = VIGNETTES["Renard"]
    ring = soft_rect(rb, 2) * (1 - reg.rect_mask(h, w, rb[0] + PORTRAIT_INSET, rb[1] + PORTRAIT_INSET, rb[2] - PORTRAIT_INSET, rb[3] - PORTRAIT_INSET, feather=4))
    glow = np.clip(P3 - CA, 0, 1) * ring[..., None]
    gl = reg.gray(glow) * 1.0 + glow.max(-1) * 0.0
    gl = np.maximum(reg.gray(glow), glow[..., 0] * 0.6)
    for n, box in VIGNETTES.items():
        x0, y0, x1, y1 = box
        disc = reg.load_rgba(os.path.join(OUT, "Choix_Vignette_%s_Discrete.png" % n))
        if n == "Renard":
            ill = np.concatenate([P3[y0:y1, x0:x1], disc[..., 3:]], -1)
        else:
            dx = x0 - rb[0]
            g = np.zeros((h, w), np.float32)
            g[y0:y1, x0:x1] = gl[rb[1]:rb[3], rb[0]:rb[2]][: y1 - y0, : x1 - x0]
            col = np.array(COULEURS[n], np.float32)
            add = g[..., None] * col[None, None, :] / max(float(col.max()), 1e-3) * 1.15
            # le cadre métallique discret, gris, passe à la couleur sous la lueur
            base = disc[..., :3]
            ill_rgb = np.clip(base + add[y0:y1, x0:x1], 0, 1)
            ill = np.concatenate([ill_rgb, disc[..., 3:]], -1)
        reg.save(os.path.join(OUT, "Choix_Vignette_%s_Illuminee.png" % n), ill, "RGBA")
        lay["pieces"]["Choix_Vignette_%s_Illuminee" % n] = [x0, y0, x1 - x0, y1 - y0]
        lay["vignettes"][n] = [x0, y0, x1 - x0, y1 - y0]

    # ---------------- 5. bouton Valider (contour mesuré, pixels de la maquette)
    poly, fleurons = VALIDER
    shape = reg.poly_mask(h, w, np.array(poly, np.float64), aa=1.2)
    for fl_ in fleurons:
        shape = np.maximum(shape, reg.poly_mask(h, w, np.array(fl_, np.float64), aa=1.2))
    lay["pieces"]["Choix_Valider"] = reg.save_piece(os.path.join(OUT, "Choix_Valider.png"), T, shape, reg.bbox(shape, pad=2))

    lay["texte"] = list(TEXTE)
    with open(os.path.join(OUT, "Choix_Layout.json"), "w", encoding="utf-8") as f:
        json.dump(lay, f, indent=1, ensure_ascii=False)
    print(json.dumps(lay["raccord"]), json.dumps(lay["personnages"]), json.dumps(lay["artefacts"]))
    if CTRL:
        reg.save(os.path.join(CTRL, "choix_fond.png"), fond, "RGB")


if __name__ == "__main__":
    main()
