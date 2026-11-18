# -*- coding: utf-8 -*-
"""Planche des états des menus (captures Unity) et des 4 états des boutons (formule du shader ItHunts/UI/Bouton :
pixel x gris x 2, en gamma)."""
import os, sys
import numpy as np
from PIL import Image, ImageDraw, ImageFont
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import reg

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
CAP = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Temp/menus")
OUT = os.path.join(ROOT, "docs/art/rendus/UI/Menus")
F = ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 18)


def planche(items, cols, tw, nom, titre):
    th = int(tw * 941 / 1672)
    rows = (len(items) + cols - 1) // cols
    W = cols * (tw + 10) + 10; H = rows * (th + 34) + 50
    b = Image.new("RGB", (W, H), (18, 16, 20)); d = ImageDraw.Draw(b)
    d.text((10, 10), titre, font=ImageFont.truetype("C:/Windows/Fonts/segoeui.ttf", 22), fill=(255, 235, 200))
    for i, (f, lab) in enumerate(items):
        im = Image.open(os.path.join(CAP, f)).convert("RGB").resize((tw, th), Image.LANCZOS)
        x = 10 + (i % cols) * (tw + 10); y = 50 + (i // cols) * (th + 34)
        d.text((x, y), lab, font=F, fill=(230, 220, 210)); b.paste(im, (x, y + 24))
    b.save(os.path.join(OUT, nom), quality=90)


def etats_boutons():
    men = os.path.join(ROOT, "Assets/Art/UI/Menus")
    sprites = [("Campement/Campement_Pret.png", "Prêt"), ("Choix/Choix_Valider.png", "Valider"),
               ("Campement/Campement_Modifier.png", "Modifier"), ("Campement/Campement_Plus_3.png", "+"),
               ("Campement/Campement_Cartouche_Nom.png", "cartouche (bouton long)")]
    etats = [("normal", (0.5, 0.5, 0.5, 1)), ("survol / sélection", (0.60, 0.575, 0.55, 1)), ("pressé", (0.40, 0.38, 0.36, 1)),
             ("désactivé", (0.30, 0.29, 0.31, 0.85))]
    cw, rh = 300, 150
    b = Image.new("RGB", (cw * 4 + 180, rh * len(sprites) + 70), (14, 11, 18)); d = ImageDraw.Draw(b)
    d.text((10, 10), "Boutons : 4 états par teinte et luminosité (ItHunts/UI/Bouton, ColorBlock du Button)", font=F, fill=(255, 235, 200))
    for j, (n, _) in enumerate(etats):
        d.text((180 + j * cw, 40), n, font=F, fill=(230, 220, 210))
    bg = np.array([14, 11, 18]) / 255.0
    for i, (p, lab) in enumerate(sprites):
        a = reg.load_rgba(os.path.join(men, p))
        s = min((cw - 20) / a.shape[1], (rh - 20) / a.shape[0], 1.0)
        d.text((10, 70 + i * rh + rh // 2 - 10), lab, font=F, fill=(230, 220, 210))
        for j, (_, c) in enumerate(etats):
            rgb = np.clip(a[..., :3] * np.array(c[:3]) * 2.0, 0, 1)
            al = a[..., 3:] * c[3]
            out = rgb * al + bg * (1 - al)
            im = Image.fromarray((out * 255).astype(np.uint8)).resize((int(a.shape[1] * s), int(a.shape[0] * s)), Image.LANCZOS)
            b.paste(im, (180 + j * cw, 70 + i * rh))
    b.save(os.path.join(OUT, "Menus_Boutons_Etats.jpg"), quality=92)


if __name__ == "__main__":
    planche([("Titre_fige.png", "Titre : Jouer sélectionné (gelé)"), ("Titre_Parametres_fige.png", "Titre : Paramètres sélectionné"),
             ("Titre_anim.png", "Titre : effets en marche (t = 3,7 s)"), ("Campement_fige.png", "Campement : état de la maquette"),
             ("Campement_Cameleon_Koala.png", "Campement : caméléon place 1, koala place 2"), ("Campement_4joueurs.png", "Campement : 4 joueurs (places 3 et 4 extrapolées)"),
             ("Campement_anim.png", "Campement : effets en marche"), ("Choix_Renard.png", "Choix : renard (lanterne de feu)"),
             ("Choix_Koala.png", "Choix : koala (à venir)"), ("Choix_Cameleon.png", "Choix : caméléon (barillet)"),
             ("Choix_Hibou.png", "Choix : hibou (à venir)"), ("Choix_anim.png", "Choix : effets en marche"),
             ("Pause.png", "Pause (Échap)"), ("Parametres.png", "Paramètres")], 3, 620, "Menus_Etats.jpg", "Menus : états (captures Unity)")
    etats_boutons()
