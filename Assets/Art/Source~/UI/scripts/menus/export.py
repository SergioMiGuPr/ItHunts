# -*- coding: utf-8 -*-
"""Rassemble les rects des découpes (Layout.json de chaque écran), les placements des personnages assis et les textes
mesurés en un fichier simple lu par le constructeur Unity (MenusBuilder.cs) : construction.txt, une ligne par donnée :
  piece <Ecran> <Nom> x y w h            (pixels de la maquette, coin haut gauche)
  place <perso> <place> cx cy cw ch lx ly lw lh   (corps et lanterne d'un personnage assis)
  texte <Ecran> <Nom> x0 x1 haut base r g b
  valeur <clé> <valeurs...>
"""
import json, os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
MEN = os.path.join(ROOT, "Assets/Art/UI/Menus")
HERE = os.path.dirname(os.path.abspath(__file__))

# cibles des places du campement : haut de capuche, centre des épaules, pied de la lanterne, facteur d'échelle
# (places 1 et 2 mesurées sur la maquette ; 3 et 4 extrapolées : plus loin, assise plus haute)
CIBLES = {1: {"capuche": 352, "cx": 569, "lanterne": (510, 676), "f": 1.0},
          2: {"capuche": 346, "cx": 807, "lanterne": (918, 596), "f": 1.0},
          3: {"capuche": 315, "cx": 1150, "lanterne": (1267, 607), "f": 0.85},
          4: {"capuche": 310, "cx": 1462, "lanterne": (1540, 635), "f": 0.85}}
# cartouche de nom par place (rect de la pièce Campement_Cartouche_Nom, étirée en 9 tranches pour la place 1)
CARTOUCHES = {1: (438.6, 666, 239, 71), 2: (718, 663, 212, 72), 3: (1079, 663, 212, 72), 4: (1361, 663, 212, 72)}


def main():
    L = []
    for ecran in ("Titre", "Campement", "Choix"):
        lay = json.load(open(os.path.join(MEN, ecran, ecran + "_Layout.json"), encoding="utf-8"))
        for k, r in lay["pieces"].items():
            L.append("piece %s %s %s" % (ecran, k, " ".join("%.2f" % v for v in r)))
        L.append("valeur %s_taille %d %d" % (ecran, lay["taille"][0], lay["taille"][1]))
        if ecran == "Titre":
            L.append("valeur Titre_pieds_y %d" % lay["pieds_y"])
            L.append("valeur Titre_lune %.1f %.1f %.1f" % (lay["lune"]["centre"][0], lay["lune"]["centre"][1], lay["lune"]["rayon"]))
            L.append("valeur Titre_couleur_brume %.3f %.3f %.3f" % tuple(lay["couleur_brume"]))
        if ecran == "Campement":
            L.append("valeur Campement_foyer %d %d" % (lay["foyer"]["centre_x"], lay["foyer"]["base_y"]))
            for name, a in lay["assis"].items():
                L.append("valeur Campement_couleur_%s %.3f %.3f %.3f" % (name, *a["couleur"]))
                pl1 = a["places"]["1"]
                for place, c in CIBLES.items():
                    s = pl1["echelle"] * c["f"]
                    rx, ry, rw, rh = a["corps"]["rect_image"]
                    x = c["cx"] - (pl1["centre_epaules_x"] - rx) * s
                    y = c["capuche"] - (a["capuche_image"] - ry) * s
                    lx, ly, lw, lh = a["lanterne"]["rect_image"]
                    L.append("place %s %d %.1f %.1f %.1f %.1f %.1f %.1f %.1f %.1f" % (
                        name, place, x, y, rw * s, rh * s, c["lanterne"][0] - lw * s / 2, c["lanterne"][1] - lh * s, lw * s, lh * s))
            for place, r in CARTOUCHES.items():
                L.append("valeur Campement_cartouche_%d %.1f %.1f %.1f %.1f" % (place, *r))
        if ecran == "Choix":
            for n, a in lay["artefacts"].items():
                L.append("valeur Choix_centre_%s %.4f %.4f" % (n, *a["centre_uv"]))
            for n, p in lay["personnages"].items():
                L.append("valeur Choix_pieds_%s %d" % (n, p["pieds_y"]))
    cad = json.load(open(os.path.join(MEN, "Commun", "Commun_Layout.json"), encoding="utf-8"))
    L.append("valeur Cadre_bordure %d" % cad["Menus_Cadre"]["bordures_glbh"][0])
    tx = json.load(open(os.path.join(HERE, "textes.json"), encoding="utf-8"))
    for ecr, d in tx.items():
        e = {"ui_menu_principal_v01": "Titre", "ui_campement_v01": "Campement", "ui_choix_perso_v01": "Choix"}[ecr]
        for k, m in d.items():
            L.append("texte %s %s %d %d %d %d %.3f %.3f %.3f" % (e, k, m["x0"], m["x1"], m["haut"], m["base"], *m["couleur"]))
    out = os.path.join(HERE, "construction.txt")
    open(out, "w", encoding="utf-8").write("\n".join(L) + "\n")
    print(len(L), "lignes ->", out)


if __name__ == "__main__":
    main()
