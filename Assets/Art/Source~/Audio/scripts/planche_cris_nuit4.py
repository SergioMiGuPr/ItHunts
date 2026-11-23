# -*- coding: utf-8 -*-
"""Planche des cris du traqueur (nuit 4, lot 7) : forme d'onde, durée, crête, niveau au-dessus de 80 Hz, part des médiums.
    python -I planche_cris_nuit4.py   (depuis la racine du dépôt)
"""
import os, wave
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", ".."))
POL = os.path.join(ROOT, "Assets", "Art", "UI", "Polices", "EBGaramond", "EBGaramond-%s.ttf")
f = lambda t, s="Regular": ImageFont.truetype(POL % s, t)
NOMS = [("Sfx_Traqueur_Cri_Apparition_01", "Apparition (01) : montée puis rugissement, présence ajoutée"),
        ("Sfx_Traqueur_Cri_Apparition_02", "Apparition (02) : cri éraillé qui monte"),
        ("Sfx_Traqueur_Cri_Vu_01", "« Je t'ai vu » (01) : aspiration 0,13 s fondue puis hurlement"),
        ("Sfx_Traqueur_Cri_Vu_02", "« Je t'ai vu » (02) : variante courte"),
        ("Sfx_Traqueur_Cri_Enrage_01", "Référence : cri d'enragé (déjà au jeu)")]
W, H = 1800, 170
o = Image.new("RGB", (W, 90 + len(NOMS) * (H + 50)), (18, 17, 20)); d = ImageDraw.Draw(o)
d.text((16, 12), "Lot 7 : cris du traqueur (44,1 kHz mono, passe-haut 30 Hz, crête sous −1 dBFS, niveau au-dessus de 80 Hz calé sur le cri d'enragé)",
       font=f(26, "SemiBold"), fill=(226, 214, 190))
y = 70
for n, leg in NOMS:
    w = wave.open(os.path.join(ROOT, "Assets", "Art", "Audio", "Traqueur", n + ".wav"))
    a = np.frombuffer(w.readframes(w.getnframes()), "<i2") / 32768; sr = w.getframerate()
    F = np.fft.rfft(a); fr = np.fft.rfftfreq(len(a), 1 / sr); P = np.abs(F) ** 2
    h = F.copy(); h[fr < 80] = 0; r80 = 20 * np.log10(np.sqrt((np.fft.irfft(h, len(a)) ** 2).mean()))
    med = 100 * P[(fr >= 300) & (fr < 4000)].sum() / P.sum()
    dur = len(a) / sr; pk = 20 * np.log10(np.abs(a).max())
    d.text((16, y), "%s  —  %s  (%.2f s, crête %.1f dBFS, RMS > 80 Hz %.1f dBFS, 300 Hz – 4 kHz : %.0f %%)" % (n, leg, dur, pk, r80, med),
           font=f(20), fill=(226, 214, 190)); y += 30
    x0 = 16; sc = (W - 32) / 5.0
    for i, s in enumerate(np.array_split(a, max(1, int(dur * sc)))):
        d.line([(x0 + i, y + H / 2 - s.max() * H / 2), (x0 + i, y + H / 2 - s.min() * H / 2)], fill=(110, 100, 90) if "Enrage" in n else (150, 40, 36))
    for t in range(6):
        d.line([(x0 + t * sc, y + H - 4), (x0 + t * sc, y + H)], fill=(120, 112, 100)); d.text((x0 + t * sc + 3, y + H - 22), "%d s" % t, font=f(14), fill=(120, 112, 100))
    y += H + 20
o.save(os.path.join(ROOT, "docs", "art", "rendus", "Nuit4", "Lot7", "Planche_Cris_Traqueur.png")); print("ok")
