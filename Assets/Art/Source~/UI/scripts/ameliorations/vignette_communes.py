# -*- coding: utf-8 -*-
"""Vignettes de l'onglet « Communes » du codex (nuit 5, lot 2) : même facture que les vignettes du choix du personnage
(cadre lumineux, portrait au centre). Cadre repris de Choix_Vignette_Hibou_<Discrete|Illuminee>, repassé en fer neutre
(luminance gardée, couleur retirée) ; au centre, l'illustration de la Grande orbe (G5), assombrie sur ses bords.
Sorties : Assets/Art/UI/Ameliorations/Codex_Vignette_Communes_<Discrete|Illuminee>.png"""
import os
import numpy as np
from PIL import Image, ImageFilter
ICI = os.path.dirname(os.path.abspath(__file__))
R = os.path.abspath(os.path.join(ICI, "..", "..", "..", "..", "..", ".."))
CH = os.path.join(R, "Assets", "Art", "UI", "Menus", "Choix")
AM = os.path.join(R, "Assets", "Art", "UI", "Ameliorations")
orbe = Image.open(os.path.join(AM, "Icones", "ICO_G5.png")).convert("RGB")
for etat, gain in (("Discrete", 0.42), ("Illuminee", 1.0)):
    v = np.asarray(Image.open(os.path.join(CH, f"Choix_Vignette_Hibou_{etat}.png")).convert("RGBA")).astype(np.float32) / 255
    h, w = v.shape[:2]
    lum = (v[..., :3] * [0.2126, 0.7152, 0.0722]).sum(-1, keepdims=True)
    fer = np.clip(lum * np.array([1.02, 1.0, 1.04]) * (0.95 if etat == "Illuminee" else 0.8), 0, 1)
    out = v.copy()
    out[..., :3] = fer
    # intérieur : orbe, fondu vers le noir sur les bords (comme les portraits)
    m = 20
    iw, ih = w - 2 * m, h - 2 * m
    o = np.asarray(orbe.resize((iw, ih), Image.LANCZOS)).astype(np.float32) / 255 * gain
    yy, xx = np.mgrid[0:ih, 0:iw]
    d = np.minimum.reduce([xx, yy, iw - 1 - xx, ih - 1 - yy]).astype(np.float32)
    k = np.clip(d / 14.0, 0, 1)[..., None]
    out[m:m + ih, m:m + iw, :3] = o * k + out[m:m + ih, m:m + iw, :3] * (1 - k) * 0.3
    out[m:m + ih, m:m + iw, 3] = 1.0
    Image.fromarray((np.clip(out, 0, 1) * 255).astype(np.uint8)).save(os.path.join(AM, f"Codex_Vignette_Communes_{etat}.png"))
    print(etat, w, h)
