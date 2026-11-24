# -*- coding: utf-8 -*-
"""Catalogue des pièces du lot 3 de la nuit 5 (kit_nuit5.py et porte verrouillée refaite) : tableau par acte tiré des mesures
d'export (kit_<acte>.json : doc, taille, bas, triangles, matériaux), pour INTEGRATION-PROPS.md et KITS.md.

    python -I kit_doc_nuit5.py  -> imprime le markdown"""
import json, os
ICI = os.path.dirname(os.path.abspath(__file__))
PIVOT = {
    "PontSuspendu": "centre du tablier aux bouts (dessus à 0 aux appuis `Appui_A` / `Appui_B`, flèche de 0.8 m au milieu)",
    "Rebord": "ras du mur, milieu du bord arrière, dessus à 0 (saillie vers l'avant)",
    "PlateformeTraction": "centre du dessus ; accroches des chaînes à +4 m (`Accroche_Plafond`) ; `Accroche_Traction` sous la plaque",
    "SalleHaute": "sol, centre ; plancher à 6 m ; `Accroche_Echelle` devant, `Passage_Porte` à gauche",
    "Passerelle_Pente": "milieu du bord bas, dessus à 0 ; `Appui_Bas` (0, 0, 0), `Appui_Haut` (10, 0, 3)",
    "Balcon_Pierre": "ras du mur, dessus à 0",
    "Fenetre_Vue": "sol, milieu de la face ; **à poser contre un mur plein** : la vitre est 5 cm devant le mur, la baie dépasse de 0.77 m",
    "SupportRatelier": "sol, centre",
    "Lisiere_12m": "sol, milieu du bord avant ; la forêt s'étend sur 6.5 m derrière (vers −Z Unity), fond noir à 6.5 m",
    "Lisiere_Coin": "sol, coin intérieur de la clairière ; quart de cercle de rayon 8 m",
    "SousBois": "sol, centre",
    "Rocher": "sol, centre (base enfoncée de 10 cm)",
    "GrandArbre": "sol, centre du tronc ; `Appui_Branche_1..3` au bout des branches",
    "Sol_Puits": "centre, dessus de la dalle à −4 mm ; eau `Deco_Eau` à −1.5 m",
    "Sol_Fosse": "centre, dessus à 0 ; fond à −4 m ; `Accroche_Echelle` au pied de l'échelle",
    "PorteVerrouillee": "sol, milieu de la face ; pièce de mur de 1 m d'épaisseur",
}
USAGE = {
    "PontSuspendu": "relier deux plateformes de même hauteur ; collision `Col_Tablier` lisse, garde-corps `Garde_Corps`",
    "Rebord": "en quinconce tous les 2.2 à 3.5 m pour monter (saut, hissage, saut mural)",
    "PlateformeTraction": "suspendue sous un plafond ou une voûte : on s'y tracte (plaque `Plaque_Magnetique`, runes bleu pâle) puis on s'y pose",
    "SalleHaute": "pièce en hauteur reliée par échelle, pont ou coursive",
    "Passerelle_Pente": "relier deux étages de 3 m d'écart ; `Col_Rampe` lisse (16.7°)",
    "Balcon_Pierre": "balcon accessible par une porte haute",
    "Fenetre_Vue": "fenêtres de l'acte II : le paysage de nuit (ciel, lune, vallée) se voit à travers les vitres, jamais le papier peint",
    "SupportRatelier": "remplace la greybox `Prop_Ratelier` de 0.6 × 1.4 × 0.5 m",
    "Lisiere": "délimite les zones de l'acte III à la place des murs gris ; infranchissable (`Col_Lisiere`, 8 m)",
    "SousBois": "habillage au pied des arbres et des lisières ; traversable (`Deco_SansCollision`)",
    "Rocher": "obstacle et appui (petit : enjambé ; moyen : hissage ; grand : double saut)",
    "GrandArbre": "arbre de 20 m qu'on gravit par trois branches plates (3.4, 6.6, 9.8 m)",
    "Sol_Puits": "dalle de sol qui remplace une case du sol de la salle : le puits a son trou",
    "Sol_Fosse": "dalle de sol qui remplace une case du sol : fosse de 4 × 4 × 4 m dont on ressort par l'échelle",
    "PorteVerrouillee": "salle payante : `OuverturePorte.Ouvre()` (sceau et barre tournent, puis le vantail monte par à-coups, poussière)",
}


def cle(nom, table):
    for k in table:
        if k in nom:
            return table[k]
    return ""


def main():
    out = []
    for acte, f in (("Acte I : caves", "kit_caves.json"), ("Acte II : château", "kit_chateau.json"), ("Acte III : jardin", "kit_jardin.json")):
        d = json.load(open(os.path.join(ICI, f), encoding="utf-8"))
        lignes = [(n, v) for n, v in d.items() if any(k in n for k in list(PIVOT) + ["Lisiere", "SousBois", "Rocher"])]
        if not lignes:
            continue
        out.append(f"\n### {acte}\n")
        out.append("| Pièce | Emprise x × y × z (m) | Bas (m) | Triangles | Pivot | Usage |")
        out.append("|---|---|---|---|---|---|")
        for n, v in lignes:
            t = v["taille"]
            out.append(f"| `{n}` | {t[0]} × {t[1]} × {t[2]} | {v['bas']} | {v['triangles']} | {cle(n, PIVOT)} | {cle(n, USAGE)} |")
    import sys
    sys.stdout.buffer.write(("\n".join(out) + "\n").encode("utf-8"))


if __name__ == "__main__":
    main()
