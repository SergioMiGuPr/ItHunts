# -*- coding: utf-8 -*-
"""Écran titre : la maquette ui_menu_principal_v01 telle quelle (logo et 4 personnages compris), zone du menu remplie par
ui_menu_sans_texte_v01 recalée, curseur flamme et soulignement découpés par différence, masques des effets tirés des
pixels de la maquette. Sorties : Assets/Art/UI/Menus/Titre/ et Titre_Layout.json (rects en pixels de la maquette).

Usage : python titre.py [dossier_controle]
"""
import json, os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
UI = os.path.join(ROOT, "docs/art/ui")
OUT = os.path.join(ROOT, "Assets/Art/UI/Menus/Titre")
CTRL = sys.argv[1] if len(sys.argv) > 1 else None

# ---- réglages (pixels de la maquette, 1672 x 941)
ZONE_MENU = (16, 398, 414, 742)      # zone remplie par ui_menu_sans_texte (bord fondu)
FONDU_MENU = 22
CURSEUR = (62, 420, 136, 518)        # flamme violette
SOULIGNE = (96, 508, 392, 542)       # trait et losange
PERSOS = (696, 548, 1350, 792)       # rect de déformation des 4 personnages
PIEDS_Y = 776
# centre x, haut de la capuche (y), demi-largeur, phase de respiration (0..1)
CORPS = [(812, 560, 92, 0.0), (961, 576, 78, 0.27), (1102, 586, 78, 0.55), (1238, 572, 84, 0.8)]
LANTERNES = [(886, 700), (1024, 706), (1151, 706), (1305, 706)]
CHATEAU = (690, 40, 1300, 460)
SOL = (700, 760, 1420, 941)
LUNE = (1079.0, 119.0, 52.0)  # centre et rayon du disque, mesurés


def main():
    os.makedirs(OUT, exist_ok=True)
    T = reg.load(os.path.join(UI, "maquettes/ui_menu_principal_v01.png"))
    h, w = T.shape[:2]
    lay = {"ecran": "Titre", "taille": [w, h], "pieces": {}, "textes": {}}

    # ---------------- 1. zone du menu : ui_menu_sans_texte recalée
    G = reg.load(os.path.join(UI, "generees/ui_menu_sans_texte_v01.png"))
    excl = np.ones((h, w), np.float32)
    excl[ZONE_MENU[1]:ZONE_MENU[3], ZONE_MENU[0]:ZONE_MENU[2]] = 0
    M0, _, _ = reg.init_affine(T, G)
    M = reg.refine_affine(T, G, M0, excl)
    Gw = reg.warp(G, M, h, w)
    zone = reg.rect_mask(h, w, *ZONE_MENU, feather=FONDU_MENU)
    Gc, ring = reg.match_ring(Gw, T, zone > 0.01, 24)
    # le générateur a laissé un reste de lueur violette là où était la flamme : on garde la luminance, on reprend la
    # teinte de la brume voisine (pas de nouveau dessin)
    ghost = reg.ellipse_mask(h, w, 66, 474, 34, 58, feather=0.6)
    # bosse de lumière retirée : on garde le grain fin, on remplace les fréquences moyennes par le fond très flou
    flat = reg.blur(Gc, 22.0) + (Gc - reg.blur(Gc, 2.5))
    Gc = Gc * (1 - ghost[..., None]) + flat * ghost[..., None]
    fond = T * (1 - zone[..., None]) + Gc * zone[..., None]
    seam = reg.report(T, Gc, ring)
    lay["recalage_menu"] = {"M": M.tolist(), "ecart_anneau_raccord": seam}
    reg.save(os.path.join(OUT, "Titre_Fond.png"), fond, "RGB")
    lay["pieces"]["Titre_Fond"] = [0, 0, w, h]

    # ---------------- 2. curseur et soulignement : maquette - fond (sans texte)
    # ce sont des lueurs : lumière ajoutée (linéaire) = maquette - fond, nulle là où le grain repeint est plus clair
    for name, box in (("Titre_Curseur", CURSEUR), ("Titre_Soulignement", SOULIGNE)):
        roi = reg.rect_mask(h, w, *box, feather=3)
        al = reg.to_lin(reg.additive(T, fond))
        # le grain repeint du fond ajoute un bruit de 1 à 3 niveaux : seuil doux sur la lumière lissée
        sig = reg.smoothstep(0.0025, 0.008, reg.blur(reg.gray(al), 2.5))
        add = reg.to_srgb(al * (sig * roi)[..., None])
        lay["pieces"][name] = reg.save_piece(os.path.join(OUT, name + ".png"), add, np.ones((h, w), np.float32), box)

    # ---------------- 3. personnages : une ellipse douce par personnage (déformation de 1 à 2 px seulement)
    X, Y = reg.grid(h, w)
    wb = np.zeros((h, w), np.float32)
    ph = np.zeros((h, w), np.float32)
    for (cx, top, rx, p) in CORPS:
        cy = (top + PIEDS_Y) / 2.0
        ry = (PIEDS_Y - top) / 2.0 + 10
        e = reg.ellipse_mask(h, w, cx, cy, rx, ry, feather=0.35)
        ph = np.where(e > wb, p, ph)
        wb = np.maximum(wb, e)
    # cape : des épaules (y 640) au bas des capes, poids croissant vers le bas
    wc = wb * reg.smoothstep(640, 760, Y) * (1 - reg.smoothstep(PIEDS_Y - 12, PIEDS_Y + 2, Y))
    x0, y0, x1, y1 = PERSOS
    rgba = np.stack([wb, wc, ph, np.ones_like(wb)], -1)[y0:y1, x0:x1]
    reg.save(os.path.join(OUT, "Titre_Personnages_Poids.png"), rgba, "RGBA")
    reg.save(os.path.join(OUT, "Titre_Personnages.png"), fond[y0:y1, x0:x1], "RGB")
    lay["pieces"]["Titre_Personnages"] = [x0, y0, x1 - x0, y1 - y0]
    lay["pieds_y"] = PIEDS_Y

    # ---------------- 4. masques des effets (R fenêtres, G flaques, B lanternes, A halo de lune)
    S, V = reg.sat(T)
    L = reg.gray(T)
    r, g, b = T[..., 0], T[..., 1], T[..., 2]
    cx0, cy0, cx1, cy1 = CHATEAU
    win = reg.smoothstep(0.10, 0.30, r - g) * reg.smoothstep(0.30, 0.55, r) * reg.rect_mask(h, w, *CHATEAU, feather=20)
    win = reg.blur(win, 1.2)
    hp = L - reg.blur(L, 6.0)
    sx0, sy0, sx1, sy1 = SOL
    sol = reg.rect_mask(h, w, *SOL, feather=30) * reg.smoothstep(740, 800, Y)
    flaq = (reg.smoothstep(0.02, 0.10, hp) * reg.smoothstep(0.15, 0.45, V) * (0.4 + 0.6 * reg.smoothstep(0.2, 0.6, S))) * sol
    flaq = reg.blur(flaq, 0.8)
    lant = np.zeros((h, w), np.float32)
    for lx, ly in LANTERNES:
        e = reg.ellipse_mask(h, w, lx, ly, 34, 40, feather=0.7)
        lant = np.maximum(lant, e * reg.smoothstep(0.25, 0.7, V) * reg.smoothstep(0.25, 0.6, S))
        lant = np.maximum(lant, reg.ellipse_mask(h, w, lx, ly + 4, 70, 60, feather=1.0) * 0.45 * reg.smoothstep(0.1, 0.5, S) * reg.smoothstep(0.12, 0.5, V))
    lant = reg.blur(lant, 1.5)
    # lune : disque le plus clair dans le ciel
    mcx, mcy, mr = LUNE
    d = np.hypot(X - mcx, Y - mcy) / mr
    halo = (reg.smoothstep(0.85, 1.05, d) * (1 - reg.smoothstep(1.05, 3.2, d))) * reg.smoothstep(0.10, 0.35, L)
    lay["lune"] = {"centre": [mcx, mcy], "rayon": mr}
    pack = np.stack([win, flaq, lant, halo], -1)
    reg.save(os.path.join(OUT, "Titre_Effets_Masques.png"), pack, "RGBA")
    lay["pieces"]["Titre_Effets_Masques"] = [0, 0, w, h]

    # ---------------- 5. brume : zones déjà brumeuses (bleutées, peu détaillées, valeur moyenne), bas de l'image
    loc = np.sqrt(reg.blur(hp * hp, 6.0))
    blue = reg.smoothstep(0.0, 0.08, b - r * 0.85)
    fog = reg.smoothstep(0.08, 0.22, L) * (1 - reg.smoothstep(0.02, 0.07, loc)) * blue
    fog = reg.blur(fog, 14.0) * reg.smoothstep(0.30 * h, 0.55 * h, Y)
    fog = fog / max(np.percentile(fog, 99.5), 1e-3)
    reg.save(os.path.join(OUT, "Titre_Brume_Masque.png"), np.clip(fog, 0, 1)[::2, ::2], "L")
    lay["pieces"]["Titre_Brume_Masque"] = [0, 0, w, h]
    m = fog > 0.3
    lay["couleur_brume"] = np.round(np.median(T[m], 0), 3).tolist()

    with open(os.path.join(OUT, "Titre_Layout.json"), "w", encoding="utf-8") as f:
        json.dump(lay, f, indent=1, ensure_ascii=False)
    print(json.dumps({k: v for k, v in lay.items() if k != "pieces"}, ensure_ascii=False)[:800])
    print(lay["pieces"])
    if CTRL:
        reg.save(os.path.join(CTRL, "titre_fond.png"), fond, "RGB")
        reg.save(os.path.join(CTRL, "titre_masques.png"), pack[..., :3] + pack[..., 3:] * 0.5, "RGB")
        reg.save(os.path.join(CTRL, "titre_corps.png"), np.stack([wb, wc, ph], -1), "RGB")
        reg.save(os.path.join(CTRL, "titre_brume.png"), np.repeat(np.clip(fog, 0, 1)[..., None], 3, -1), "RGB")


if __name__ == "__main__":
    main()
