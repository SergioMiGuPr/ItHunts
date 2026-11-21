# -*- coding: utf-8 -*-
"""Lignes du manifeste pour les menus (art-menus) : retire les sprites du lot B supprimés, remplace les lignes des menus
et ajoute une ligne par pièce, par image source et par son. Relançable (les lignes des menus sont régénérées)."""
import glob, os, re
from PIL import Image

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../../../../.."))
MAN = os.path.join(ROOT, "docs/assets/MANIFEST.md")
MEN = "Assets/Art/UI/Menus"
SCR = "Assets/Art/Source~/UI/scripts/menus/"
H = "Contenu généré, conditions Higgsfield"
DROITS = "À confirmer par l'utilisateur. **Outils : Higgsfield** (maquettes et images retouchées par l'utilisateur) ; découpe par script (Python/numpy, sans IA). À vérifier : liste **H** sous le tableau"
MARQUE = "<!-- menus art-menus -->"

DESC = {
    "Titre_Fond": ("fond de l'écran titre : maquette telle quelle, zone du menu remplie par l'image sans texte", "titre.py", "`ui_menu_principal_v01` (maquette) et `ui_menu_sans_texte_v01`"),
    "Titre_Personnages": ("recadrage des 4 personnages du titre (déformation sur place)", "titre.py", "`ui_menu_principal_v01`"),
    "Titre_Personnages_Poids": ("poids de déformation des personnages du titre (données)", "titre.py", "`ui_menu_principal_v01`"),
    "Titre_Effets_Masques": ("masques des lumières du titre : fenêtres, flaques, lanternes, halo de lune (données)", "titre.py", "`ui_menu_principal_v01`"),
    "Titre_Brume_Masque": ("masque de la brume du titre (données)", "titre.py", "`ui_menu_principal_v01`"),
    "Titre_Curseur": ("curseur flamme violette (lumière ajoutée)", "titre.py", "`ui_menu_principal_v01` − `ui_menu_sans_texte_v01`"),
    "Titre_Soulignement": ("soulignement de l'élément sélectionné (lumière ajoutée)", "titre.py", "`ui_menu_principal_v01` − `ui_menu_sans_texte_v01`"),
    "Campement_Fond": ("fond du campement : maquette, zones qui changent remplies par le campement vide", "campement.py", "`ui_campement_v01` (maquette), `ui_campement_vide_sans_lanternes_v01`, `ui_campement_braises_v01`"),
    "Campement_Flammes": ("flammes du feu de camp (lumière ajoutée)", "campement.py", "`ui_campement_passe1_v01` − `ui_campement_braises_v01`"),
    "Campement_Effets_Masques": ("masques des lumières du campement : fenêtres, lanterne de la tente, lueur du feu (données)", "campement.py", "`ui_campement_v01`"),
    "Campement_Lanterne_Banc": ("lanterne éteinte des places 2 et 3", "campement.py", "`ui_campement_braises_v01` − `ui_campement_vide_sans_lanternes_v01`"),
    "Campement_Lanterne_Rocher": ("lanterne éteinte de la place 4", "campement.py", "`ui_campement_braises_v01` − `ui_campement_vide_sans_lanternes_v01`"),
    "Campement_Cartouche_Nom": ("cartouche de nom vidé de son texte peint", "campement.py", "`ui_campement_v01` (cartouche du hibou)"),
    "Campement_Coche": ("coche « prêt »", "campement.py", "`ui_campement_v01`"),
    "Campement_Modifier": ("bouton Modifier", "campement.py", "`ui_campement_v01` − `ui_campement_passe1_v01`"),
    "Campement_Plus_3": ("bouton + de la place 3", "campement.py", "`ui_campement_v01` − `ui_campement_passe1_v01`"),
    "Campement_Plus_4": ("bouton + de la place 4", "campement.py", "`ui_campement_v01` − `ui_campement_passe1_v01`"),
    "Campement_Pret": ("bouton Prêt", "campement.py", "`ui_campement_v01` − `ui_campement_passe1_v01`"),
    "Campement_Retour": ("flèche retour", "campement.py", "`ui_campement_v01` − `ui_campement_passe1_v01`"),
    "Campement_Parametres": ("icône paramètres", "campement.py", "`ui_campement_v01` − `ui_campement_passe1_v01`"),
    "Campement_Amis": ("icône amis", "campement.py", "`ui_campement_v01` − `ui_campement_passe1_v01`"),
    "Choix_Fond": ("fond de l'écran de choix : maquette, zones qui changent remplies par le fond", "choix.py", "`ui_choix_perso_v01` (maquette) et `ui_choix_fond_v01`"),
    "Choix_Valider": ("bouton Valider", "choix.py", "`ui_choix_perso_v01`"),
    "Choix_Artefact_Lanterne": ("artefact du renard : lanterne", "choix.py", "`ui_choix_passe3_v01` − `ui_choix_fond_v01`"),
    "Choix_Artefact_Lanterne_Trainees": ("traînées de lumière de la lanterne", "choix.py", "`ui_choix_passe3_v01` − `ui_choix_fond_v01`"),
    "Choix_Artefact_Barillet": ("artefact du caméléon : barillet spectral", "choix.py", "`ui_choix_artefact_barillet_v01` − `ui_choix_cameleon_v01`"),
    "Choix_Artefact_Barillet_Trainees": ("traînées de lumière du barillet", "choix.py", "`ui_choix_artefact_barillet_v01` − `ui_choix_cameleon_v01`"),
    "Menus_Cadre": ("cadre des menus pause et paramètres (9 tranches)", "cadres.py", "`ui_choix_perso_v01` (cadre)"),
    "Menus_Cartouche_G": ("cartouche long : bout gauche", "cadres.py", "`Campement_Cartouche_Nom`"),
    "Menus_Cartouche_M": ("cartouche long : tranche raccordable", "cadres.py", "`Campement_Cartouche_Nom`"),
    "Menus_Cartouche_C": ("cartouche long : fleuron central", "cadres.py", "`Campement_Cartouche_Nom`"),
    "Menus_Cartouche_D": ("cartouche long : bout droit", "cadres.py", "`Campement_Cartouche_Nom`"),
    "Menus_Lueur": ("tache de lumière radiale (technique)", "cadres.py", "calculée (aucune image)"),
}
PERSOS = {"Renard": "renard", "Koala": "koala", "Cameleon": "cameleon", "Hibou": "hibou"}
for P, p in PERSOS.items():
    DESC["Campement_Assis_" + P] = ("%s assis (campement), caisse retirée, lumière de la scène" % P.replace("Cameleon", "Caméléon"), "assis.py", "`ui_assis_buche_%s_v01`" % p)
    DESC["Campement_Assis_%s_Lanterne" % P] = ("lanterne allumée du %s" % P.replace("Cameleon", "Caméléon").lower(), "assis.py", "`ui_assis_buche_%s_v01`" % p)
    DESC["Choix_" + P] = ("grand personnage debout : %s" % P.replace("Cameleon", "caméléon").lower(), "choix.py", "`ui_choix_%s_v01` − `ui_choix_fond_v01`" % p)
    DESC["Choix_%s_Lueur" % P] = ("masque de la lanterne et du reflet du %s (données)" % P.replace("Cameleon", "caméléon").lower(), "choix.py", "`ui_choix_%s_v01`" % p)
    src = "`ui_choix_cameleon_v01`" if P == "Renard" else "`ui_choix_passe3_v01`"
    DESC["Choix_Vignette_%s_Discrete" % P] = ("vignette discrète : %s" % P.replace("Cameleon", "caméléon").lower(), "choix.py", src)
    DESC["Choix_Vignette_%s_Illuminee" % P] = ("vignette illuminée : %s" % P.replace("Cameleon", "caméléon").lower(), "choix.py",
                                               "`ui_choix_passe3_v01`" + ("" if P == "Renard" else " (lueur du cadre du renard recolorée)"))


def rows():
    out = []
    for f in sorted(glob.glob(os.path.join(ROOT, MEN, "*", "*.png"))):
        n = os.path.splitext(os.path.basename(f))[0]
        d, scr, src = DESC.get(n, (n, "?", "?"))
        w, h = Image.open(f).size
        rel = os.path.relpath(f, ROOT).replace("\\", "/")
        ia = "non (calculée)" if n == "Menus_Lueur" else "oui (images d'origine)"
        out.append("| `%s` (%s) | `%s` | Découpé par script (`%s%s`) dans %s (images de l'utilisateur, IA, Higgsfield) | %s | %s | %s | Oui (2026-11-18) : %d × %d px, PPU 100, posé en pixels de la maquette | Haut gauche (rect de la maquette) | — | À valider DA |"
                   % (n, d, rel, SCR, scr, src, ia, H if n != "Menus_Lueur" else "Projet", DROITS if n != "Menus_Lueur" else "sans objet", w, h))
    for f in sorted(glob.glob(os.path.join(ROOT, "docs/art/ui/generees", "*.png"))):
        n = os.path.splitext(os.path.basename(f))[0]
        w, h = Image.open(f).size
        out.append("| `%s` (image retouchée par l'utilisateur, source à découper, non intégrée) | `docs/art/ui/generees/%s.png` (hors `Assets/`) | Généré par l'utilisateur sur Higgsfield (modèle à préciser) à partir des maquettes | oui | %s | %s | sans objet (%d × %d px) | sans objet | sans objet | Source |" % (n, n, H, DROITS, w, h))
    for src, dst, outil in (("musique_menu_v01.mp3", "Musique_Menu_Boucle.wav", "ElevenLabs Music v2.5"), ("sfx_feu_boucle_v01.wav", "Sfx_Feu_Boucle.wav", "Seed Audio 1.0")):
        out.append("| `%s` (boucle du menu : silences retirés, fondu enchaîné fin → début) | `Assets/Art/Audio/Menu/%s` | Généré par l'utilisateur sur Higgsfield (%s), source `docs/art/audio/menu/%s` ; boucle par script (`%sboucles_audio.py`, ffmpeg + numpy, sans IA) | oui | Contenu généré, conditions Higgsfield et du modèle audio | À confirmer par l'utilisateur. **Outils : Higgsfield, %s**. À vérifier : liste **A** sous le tableau | sans objet (son) | sans objet | sans objet | À valider DA |"
                   % (os.path.splitext(dst)[0], dst, outil, src, SCR, outil))
    return out


def main():
    s = open(MAN, encoding="utf-8").read()
    lines = s.split("\n")
    keep = []
    for l in lines:
        if "Assets/Art/UI/Menus/" in l or "docs/art/ui/generees/" in l or "Assets/Art/Audio/Menu/" in l:
            continue
        keep.append(l)
    # insère après la dernière ligne de tableau
    last = max(i for i, l in enumerate(keep) if l.startswith("| ") and not l.startswith("|---"))
    keep = keep[:last + 1] + rows() + keep[last + 1:]
    s = "\n".join(keep)
    if "**A. Higgsfield, audio**" not in s:
        s = s.replace("**B. Blender 5.0**", "**A. Higgsfield, audio** (musique du menu : ElevenLabs Music v2.5 ; feu : Seed Audio 1.0) : mêmes questions que la liste\n"
                      "**H** (usage commercial au forfait de la date de génération, propriété des sorties, droit de les couper et boucler,\n"
                      "conditions du modèle audio répercutées par Higgsfield, mention obligatoire éventuelle).\n\n**B. Blender 5.0**")
    open(MAN, "w", encoding="utf-8").write(s)
    print(len(rows()), "lignes menus")


if __name__ == "__main__":
    main()
