import os
from PIL import Image, ImageDraw, ImageFont
S = r"C:\Users\PC\AppData\Local\Temp\claude\fa13b7ae4f76688a\5c2bc682-360e-47d5-a52c-698998cd4727\scratchpad"
T = os.path.join(S, "traq2"); Dg = os.path.join(S, "traqueur_diag", "img")
OUT = r"C:\Users\PC\ItHunts\ItHunts-art\docs\art\rendus\Traqueur\Traqueur_v3_Retopo_Gréement.jpg"
F = lambda s: ImageFont.truetype("C:/Windows/Fonts/georgia.ttf", s); FB = lambda s: ImageFont.truetype("C:/Windows/Fonts/georgiab.ttf", s)
W = 2400; img = Image.new("RGB", (W, 6000), (18, 17, 21)); d = ImageDraw.Draw(img)
def fit(p, w, h):
    im = Image.open(p).convert("RGB"); s = min(w / im.width, h / im.height); return im.resize((int(im.width * s), int(im.height * s)), Image.LANCZOS)
y = 30
d.text((40, y), "Traqueur v3 (méthode D, partie A) : retopologie à boucles, os recalés, poids par régions, clips bornés, étirement par l'échelle", font=FB(36), fill=(236, 228, 220)); y += 56
for t in ["Maillage : corps en quads (QuadriFlow, 97 % de sommets de valence 4, boucles continues aux épaules, coudes, hanches, genoux) ; mains, tête et pieds d'origine (UV et textures d'origine) ; 38 364 + 1 344 triangles (budget 40–60 k)",
          "Liaison : debout, bras écartés (pose du modèle) ; la voûte est dans les clips. 32 os (plus d'os d'aide), recalés au centre du volume (coudes, poignets, tête, mâchoire, pied).",
          "Poids : os le plus proche puis diffusion le long des arêtes, masqués par région : 0 sommet tiré par un os à plus de 2.5 m (avant : 1 079) ; le bras n'est tiré que par la chaîne du bras.",
          "Clips : mains levées : épaule 104° (≤ 110°), torsion 0° ; revers : 70°, torsion 0° (avant : 88° et 61°) ; prise : doigts qui se ferment vers la paume ; marche et passage bas : pieds verrouillés",
          "par IK, au sol 61 % du temps, vitesse au sol constante 3.71 m/s (marche) et 2.29 m/s (passage bas) ; passage bas : sommet 8.5 m.",
          "Étirement ×3.8 par l'échelle de Bras (comme D-070, bras tendu) : main ×1.18 au plus, 8 triangles retournés ; seule la bande au-dessus de l'épaule s'étire fort (continue, sans fente).",
          "M_Traqueur_Mains (clair) arrêté au poignet ; avant le poignet : texture d'origine sans liseré (M_Traqueur_Tete)."]:
    d.text((40, y), t, font=F(22), fill=(175, 170, 180)); y += 32
y += 20
d.text((40, y), "Avant (diagnostic) : mains levées et marche", font=FB(28), fill=(200, 120, 110)); d.text((1240, y), "Topologie : avant (Tripo) et après (quads)", font=FB(28), fill=(120, 200, 140)); y += 44
img.paste(fit(os.path.join(Dg, "MainsLevees_f07_34.jpg"), 560, 560), (40, y)); img.paste(fit(os.path.join(Dg, "Marche_f46_34.jpg"), 560, 560), (620, y))
img.paste(fit(os.path.join(T, "w.jpg"), 1120, 560), (1240, y)); y += 600
d.text((40, y), "Après, en jeu (Unity, shader toon, matériaux réels) : repos, prise, mains levées, marche, revers, passage bas", font=FB(28), fill=(120, 200, 140)); y += 44
for i, k in enumerate(["Repos", "Prise", "MainsLevees", "Marche", "Revers", "PassageBas"]):
    img.paste(fit(os.path.join(T, "unity", "u_Traqueur_%s.jpg" % k), 760, 760), (40 + (i % 3) * 780, y + (i // 3) * 780))
y += 1580
d.text((40, y), "Étirement du bras droit, bras tendu : ×1 et ×3.8 (rouge : arêtes étirées au-delà de ×4.4)", font=FB(28), fill=(120, 200, 140)); d.text((1240, y), "Matériaux : corps / mains (clair, dès le poignet) / origine", font=FB(28), fill=(120, 200, 140)); y += 44
img.paste(fit(os.path.join(T, "k1.0_cote.jpg"), 1140, 330), (40, y)); img.paste(fit(os.path.join(T, "k3.8_cote.jpg"), 1140, 330), (40, y + 340))
img.paste(fit(os.path.join(T, "materiaux.jpg"), 1120, 660), (1240, y)); y += 700
img = img.crop((0, 0, W, y)); img.save(OUT, quality=86); print(OUT, img.size)
