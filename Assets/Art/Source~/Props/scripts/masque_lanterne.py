# -*- coding: utf-8 -*-
"""Lot 4 b : masque d'émission de la lanterne (T_Prop_Lanterne_Emission.png, 1024, niveaux de gris). Les vitres ambrées
et la bougie (teinte 15-55°, saturation > 0.18, valeur > 0.38 sur la texture de base éclaircie) valent 1, le fer 0 ;
bord adouci d'un pixel. Dans Unity : émission chaude du matériau multipliée par ce masque, en plus de la lumière posée
sur le vide « Feu » (les vitres sont opaques : sans ce masque, la lanterne paraît éteinte de près).
    python -I masque_lanterne.py
"""
import os
import numpy as np
from PIL import Image, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
TEX = os.path.join(HERE, "..", "..", "..", "..", "..", "Assets", "Art", "Textures", "Props")
im = Image.open(os.path.join(TEX, "T_Prop_Lanterne_BaseColor.png")).convert("RGB")
hsv = np.asarray(im.convert("HSV")).astype(np.float32) / 255
h, s, v = hsv[..., 0] * 360, hsv[..., 1], hsv[..., 2]
m = ((h > 15) & (h < 55) & (s > 0.18) & (v > 0.38)).astype(np.uint8) * 255
out = Image.fromarray(m, "L").filter(ImageFilter.GaussianBlur(1))
out.save(os.path.join(TEX, "T_Prop_Lanterne_Emission.png"))
print("masque : %.1f %% de la texture" % (100 * m.mean() / 255))
