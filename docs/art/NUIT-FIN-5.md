# Fin de la nuit 5 (2026-11-24 au 25) : bilan de la DA

Bilan pour main et l'utilisateur.

Exigence de la nuit : niveau « exceptionnel », toute capture utilisable en boutique, rien hors DA. Chaque lot a sa branche,
sa PR vers `main`, ses captures en jeu, et une relecture par un sous-agent qui n'avait pas vu le travail. Les corrections
sont faites avant la PR.

## PR

| Lot | Branche | PR | État |
|---|---|---|---|
| 1 : campement (silhouettes noires derrière les « + », halos autour des personnages) | `art-menus-5` | #54 | fusionnée par main |
| 2 : UI (cadres par personnage, rangs en étoiles, colonne du HUD, coffre et marchand, infobulle, codex) | `art-ui-5` | #55 | fusionnée par main |
| 3 : pièces pour cartes en 3D (verticalité, fenêtres avec vue, forêt, sol mélangé, porte verrouillée, puits et fosses, coffres) | `art-kits-5` | #56 | fusionnée par main |
| 4 : effets (plus de rouge, objet tenu, lien du livre, apparitions, sons de l'épée) | `art-vfx-5` | #57 | **ouverte** |
| 5 : revue des cartes faites à la main | `art-fin-5` | cette PR | `docs/art/REVUE-CARTES.md` |

## Tableau de contrôle

| Demande | Preuve | État |
|---|---|---|
| Lot 0 : skills Blender au niveau utilisateur, relus avant installation | `~/.claude/skills/` (97 dossiers, dont `blender-tooling`) ; rien dans le dépôt | fait |
| Lot 0 : supprimer les dossiers vides `C:\docs\…` | refusé par la sécurité de la session (suppression hors projet) ; il reste `C:\docs\art\…` (10 jpg, copies de captures déjà commitées) | **à faire à la main** |
| Lot 1 : plus d'ombres noires derrière les places vides ni autour des personnages, 4 personnages par place, coop 2 à 4 | `docs/art/rendus/Nuit5/Lot1/` (places vides et occupées à côté de la maquette, joueur 1, coop), `LOT1-CAMPEMENT.md` ; 2 relectures | fait (#54) |
| Lot 2 : cadres par personnage, rangs, formes, colonne du HUD, cartes du coffre et du marchand, infobulle, codex, UI-HANDOFF | `docs/art/rendus/Nuit5/Lot2/` (planches HUD, coffre et marchand, codex), `UI-HANDOFF.md` § 10 | fait (#55) |
| Lot 3 : pièces de verticalité, fenêtres de l'acte II qui montrent l'extérieur, forêt, sol mélangé, porte animée, puits et fosses, coffres lumineux, formes de l'acte II, `INTEGRATION-PROPS.md` | `docs/art/rendus/Nuit5/Lot3/` (planches par acte, `Jeu/`), `controle_faces.json` (37 FBX : 0 face superposée ou dégénérée) | fait (#56) |
| Lot 4 : plus de marquage rouge (traqueur, goules, volants, boss), indice sombre | `docs/art/rendus/Nuit5/Lot4/Planche_Marquages_Boss.jpg`, `Planche_Marquages_Ennemis.jpg` (avant / après, en jeu) | fait (#57) ; branchement côté main (voir ART-HANDOFF) |
| Lot 4 : objet tenu, ni violet plein ni trait épais | `Planche_Prise_Apparition.jpg` | fait (#57) ; branchement côté main |
| Lot 4 : lien du livre à chaque coup, accroche visible (drain rouge sombre, soin bleu-vert) | `Planche_Livre.jpg`, `test_lien.txt` (7 cas testés en jeu) | fait (#57) ; branchement côté main |
| Lot 4 : épée du koala, vide et impact (2 à 3 variantes) | `Assets/Art/Audio/Armes/Sfx_Epee_*` (3 + 3), manifeste | fait (#57) ; droits à confirmer |
| Lot 4 : apparition des goules (trou, grille, ombre) | `Planche_Apparitions.jpg` | fait (#57) |
| Lot 5 : lancer chaque carte de main et noter ce qui n'est pas au niveau | `docs/art/REVUE-CARTES.md`, `docs/art/rendus/Nuit5/Lot5/` (6 cartes : vues, audits, gros plans) | fait ; corrections côté main |
| Journal Unity : 0 erreur sur nos assets | relevé à chaque lot (seuls des avertissements d'atlas d'ombres de la scène) | fait |
| Mains FPS non modifiées, modèle du traqueur verrouillé | aucun fichier des bras FPS ni du traqueur dans les PR #54 à #57 | respecté |
| Pas d'ennemi ni de marchand en 3D, aucun achat | aucune génération 3D | respecté |
| Ni fusion ni push forcé | les PR sont fusionnées par main | respecté |

## Crédits

**8,03 crédits** sur 150 (solde Higgsfield : 684,22 avant, 676,19 après), chacun avec un devis :
- icône de soin, Nano Banana 2.1 : 2 ;
- panorama de nuit des fenêtres, 4K 21:9 : 3 ;
- 9 sons Mirelo pour l'épée : environ 3 (6 retenus).

## Ce qui reste ouvert

- **Droits d'usage commercial** : sons de l'épée (Higgsfield, Mirelo), panorama et icône de soin (Higgsfield, Nano Banana).
  Ils sont à confirmer par l'utilisateur ; le manifeste les marque « à confirmer ». Ils sont exigés avant la fusion de #57.
- **Branchements côté main** (`ART-HANDOFF.md`, section « Nuit 5, lot 4 ») : matériaux d'indice sur les annonces des goules et
  du traqueur, contour discret du volant et de l'objet tenu, lien du livre à chaque appui, sons de l'épée, effets
  d'apparition. Deux réglages de main sont aussi signalés : la lanterne du hibou (bleu saturé) et le halo de la lanterne des
  joueurs (une boule orange vue de l'extérieur).
- **Cartes faites à la main** (`REVUE-CARTES.md`) : salles vides et démesurées, jardin pas à ciel ouvert, damier sur les murs,
  lustres empilés, torches sans mur, décor qui se chevauche ou qu'on traverse, greybox restante. La DA peut refaire la revue
  après les corrections.
- **Pièces du lot 3 à poser** : fenêtres avec vue, lisières, sol mélangé, dalles percées.
- `C:\docs\art\…` : à supprimer à la main.

## Fin de session

Unity (projet de la DA) est fermé. Blender n'était pas ouvert. Les éditeurs Unity d'autres projets n'ont pas été touchés.
