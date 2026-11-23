# -*- coding: utf-8 -*-
"""Planche avant / après de la relecture du lot 3 (nuit 4) : une paire d'images par pièce corrigée, légendes en EB Garamond
(nom, triangles avant → après, correction).

    python planche_corrections.py <dossier des rendus> <json des mesures avant (dossier)> <planche.jpg>
"""
import sys, os, json
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", "..", ".."))
FONTS = os.path.join(ROOT, "Assets", "Art", "UI", "Polices", "EBGaramond")
dos, avant_dir, sortie = sys.argv[1:4]


def mesures(d):
    M = {}
    for js in ("kit_caves.json", "kit_chateau.json", "kit_jardin.json"):
        p = os.path.join(d, js)
        if os.path.exists(p):
            M.update(json.load(open(p, encoding="utf-8")))
    return M


MA, MB = mesures(avant_dir), mesures(HERE)
NOTES = [
    ("SM_Cave_PorteVerrouillee", "Chambranle d'un seul tenant, flamme à trois langues, rainure du vantail"),
    ("SM_Chateau_PorteVerrouillee", "Idem ; plus de corniche de manteau"),
    ("SM_Jardin_PorteVerrouillee", "Idem ; touffes remplacées par deux cartes de lierre"),
    ("SM_Cave_MurEscalade_12m", "Boutisses et abouts de poutre dans l'appareil, joints, arêtes usées"),
    ("SM_Chateau_MurEscalade_12m", "Pierres d'attente chanfreinées dessous, assise en saillie"),
    ("SM_Chateau_Fenetre_Lancette_Simple", "Losanges au plomb, médaillon, bordure rouge et ambre"),
    ("SM_Chateau_Fenetre_Lancette_Double", "Losanges, deux médaillons, rose en secteurs"),
    ("SM_Chateau_Fenetre_Lancette_Triple", "Losanges, trois médaillons, rose en secteurs"),
    ("SM_Chateau_Lustre_Cristal", "Gouttes en M_Kit_Cristal (au lieu du verre de vitrail noir)"),
    ("SM_Chateau_Torche_Murale", "Nouvelle : fer forgé, tissu goudronné, Feu_Torche"),
    ("SM_Chateau_Torchere", "Nouvelle : torchère de 1.85 m, Feu_Torche"),
    ("SM_Cave_Puits", "Eau dans l'enfant Deco_Eau (sans collider)"),
    ("SM_Jardin_Puits", "Deco_Eau ; touffe retirée"),
    ("SM_Cave_PorteSortie", "Battants contre la face arrière : course libre"),
    ("SM_Chateau_PorteSortie", "Battants contre la face arrière : course libre"),
    ("SM_Jardin_PorteSortie", "Course libre ; lierre en cartes plates"),
    ("SM_Chateau_GrandeEntree", "Battants contre la face arrière : course libre"),
    ("SM_Cave_CelluleTraqueur_Herse", "Rainure de la herse dans la façade"),
    ("SM_Chateau_CelluleTraqueur", "Rainure de la herse, façade portée à 22.5 m"),
    ("SM_Cave_CelluleTraqueur_Barricade", "Roche dégagée de la course des battants"),
    ("SM_Jardin_CelluleTraqueur_Voliere", "Socles des poteaux, jeu au gond, touffes retirées"),
    ("SM_Cave_Escalier_Droit_4m", "Col_Escalier (magenta) : joues, palier, éboulis"),
    ("SM_Cave_Escalier_Tournant_4m", "Col_Escalier : volées, paliers, dos"),
    ("SM_Cave_Escalier_Colimacon", "Col_Escalier : noyau, sous-face, palier"),
    ("SM_Chateau_Escalier_Droit_4m", "Col_Escalier : joues, palier, dos"),
    ("SM_Chateau_Escalier_Tournant_4m", "Col_Escalier : volées, paliers, dos"),
    ("SM_Chateau_Escalier_Colimacon", "Col_Escalier : noyau et socle, sous-face, palier"),
    ("SM_Jardin_Escalier_Droit_3m", "Col_Escalier : échiffres, socles ; touffes des vases retirées"),
    ("SM_Jardin_Escalier_Tournant_4m", "Col_Escalier ; touffe retirée"),
    ("SM_Jardin_Escalier_Colimacon", "Col_Escalier : noyau, sous-face, palier"),
    ("SM_Jardin_Mezzanine_8x6", "Touffes des piles remplacées par du lierre plat"),
    ("SM_Jardin_Coursive_8m", "Touffe retirée"),
    ("SM_Jardin_PorteHaute", "Touffes remplacées par deux cartes de lierre"),
    ("SM_Jardin_PorteEtroite", "Touffes remplacées par deux cartes de lierre"),
    ("SM_Jardin_MurRebords_10m", "Touffes remplacées par deux cartes de lierre"),
    ("SM_Jardin_MurEscalade_12m", "Touffes retirées (roche, racines)"),
    ("SM_Jardin_Falaise_Rebords_16m", "Touffes d'herbe en boule retirées"),
    ("SM_Jardin_Terrasse_8m", "Touffes retirées, lierre plat"),
    ("SM_Jardin_Terrasse_Acces_8m", "Touffes des vases et du pied retirées, lierre plat"),
    ("SM_Jardin_MurEnceinte_Tour", "Touffes retirées, lierre qui suit le fût"),
    ("SM_Jardin_MurEnceinte_Breche_8m", "Touffe retirée, lierre plat"),
    ("SM_Jardin_MurEnceinte_CheminRonde_8m", "Touffes retirées, lierre plat"),
    ("SM_Jardin_Haie_6m", "Bloc de feuillage bosselé au lieu des boules"),
    ("SM_Jardin_Massif", "Masse bosselée au lieu des boules"),
    ("SM_Jardin_Foret_Lisiere_20m", "Rideaux de feuillage au lieu des boules"),
]


def police(nom, t):
    return ImageFont.truetype(os.path.join(FONTS, nom), t)


F1, F2, F3, F4 = police("EBGaramond-SemiBold.ttf", 64), police("EBGaramond-SemiBold.ttf", 27), police("EBGaramond-Regular.ttf", 23), \
    police("EBGaramond-Italic.ttf", 22)
W, H, COLS, PAD, LEG = 400, 300, 3, 24, 92
cw = 2 * W + 8
rows = (len(NOTES) + COLS - 1) // COLS
P = Image.new("RGB", (COLS * cw + (COLS + 1) * PAD, 150 + rows * (H + LEG + PAD)), (16, 17, 21))
d = ImageDraw.Draw(P)
d.text((PAD, 30), "Nuit 4, lot 3 : relecture et corrections (avant / après)", font=F1, fill=(236, 230, 214))
for i, (n, note) in enumerate(NOTES):
    x0 = PAD + (i % COLS) * (cw + PAD)
    y0 = 150 + (i // COLS) * (H + LEG + PAD)
    for j, suf in enumerate(("_avant", "_apres")):
        p = os.path.join(dos, n + suf + ".jpg")
        if os.path.exists(p):
            P.paste(Image.open(p).convert("RGB").resize((W, H), Image.LANCZOS), (x0 + j * (W + 8), y0))
        else:
            d.rectangle((x0 + j * (W + 8), y0, x0 + j * (W + 8) + W, y0 + H), fill=(28, 29, 34))
            d.text((x0 + j * (W + 8) + 150, y0 + 135), "nouvelle pièce", font=F4, fill=(150, 150, 150))
        d.text((x0 + j * (W + 8) + 10, y0 + 6), "avant" if j == 0 else "après", font=F4, fill=(230, 190, 120))
    ta = MA.get(n, {}).get("triangles", "—")
    tb = MB.get(n, {}).get("triangles", "?")
    d.text((x0, y0 + H + 6), n.replace("SM_", ""), font=F2, fill=(240, 240, 240))
    d.text((x0 + cw - 230, y0 + H + 10), f"{ta} → {tb} triangles", font=F3, fill=(170, 170, 175))
    d.text((x0, y0 + H + 44), note, font=F3, fill=(200, 196, 186))
P.save(sortie, quality=85)
print("ok", sortie, P.size)
