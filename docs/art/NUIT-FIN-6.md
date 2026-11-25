# Fin de la nuit 6 (2026-11-25)

## Branches et PR

| Lot | Branche | État |
|---|---|---|
| 1. Ensembles de décor | `art-ensembles` (commit `d92c372`) | poussée ; **PR à ouvrir à la main** : https://github.com/SergioMiGuPr/ItHunts/pull/new/art-ensembles |
| 2. Seconde revue des cartes | — | **pas commencée** : condition non remplie (voir plus bas) |
| Fin | `art-fin-6` (ce fichier) | poussée ; **PR à ouvrir à la main** : https://github.com/SergioMiGuPr/ItHunts/pull/new/art-fin-6 |

Les PR n'ont pas été ouvertes : `gh` n'est pas installé et l'appel qui contenait l'ouverture de la PR a été refusé par les
permissions de la session. Il n'a pas été contourné.

## Lot 1 : tableau de contrôle

Preuves dans `docs/art/rendus/Nuit6/Lot1/` (branche `art-ensembles`).

| Demande | État | Preuve |
|---|---|---|
| Inventaire avant création ; repris et nouveau listés | fait ; 17 pièces en double supprimées | `INTEGRATION-PROPS.md`, « Nuit 6, lot 1 » |
| Un ensemble par demande, 2 variantes au minimum | 42 prefabs, 21 thèmes × 2 (6 thèmes demandés par acte I et II, 7 pour l'acte III ; en plus : puits séparé de l'autel, cheminée) | `Assets/Art/Levels/Ensembles/` |
| Fiche : emprise, hauteur, fonction, accroches, capture | fait, générée (`ens_doc.py`) | `INTEGRATION-PROPS.md` |
| Captures en jeu à points fixes | 84, salles générées graine 22, plus la salle vide ; positions notées | `*_loin.jpg`, `*_pres.jpg`, `*_00_avant.jpg`, `points_*.txt` |
| 0 erreur au journal | 0 erreur dans les trois actes. Un avertissement (inspecteur du shader) corrigé après la capture du journal | `journal_unity.txt` |
| Aucune face superposée | 0 sur 142 FBX (contrôle par pièce ; les cartes de feuillage se croisent par nature) | `controle_faces.json` |
| Collisions sur tout ce qui est solide | 3 alertes : marques gravées et bouche de mine (décor peint sur le mur, sans collision voulue) | `audit_ensembles.txt` |
| Objets posés | 3 alertes : tentures à 10 cm du sol, étagère à 7 cm, pas d'objet flottant à l'image | `audit_ensembles.txt` |
| Chevauchements | 81 boîtes englobantes qui se coupent, lues une à une : objets posés sur un meuble, ou amas voulus. L'audit compare des boîtes, pas les maillages | `audit_ensembles.txt` |
| Lumières | 6 au plus par ensemble, aucune ombre | `EnsembleBuilder.cs` |
| Une ligne au manifeste par asset | 191 lignes | `docs/assets/MANIFEST.md` |
| Relecture indépendante | 41 points ; 33 corrigés, 8 laissés ou corrigés en partie (liste suivante) | — |

**Reste de la relecture, non corrigé ou en partie :**
- triangles de `Ens_Bibliotheque` (6 254, sans LOD1) : quatre dans un même ensemble ;
- `Jardin_TasBois_A` a un fond ajouré : à ne poser que contre un mur ou une haie ;
- `Jardin_Massif_A` : le bout du chemin du kit est coupé net ;
- fond noir encore dominant autour des îlots du jardin (liseré de lune ajouté, à juger dans les cartes) ;
- miroir de `Chateau_Chambre_B` (pièce de kit) : surface unie ;
- couronnes des chênes encore en boule ;
- draps : `Drap_Fauteuil` mieux plié, mais encore simple ; les draps au sol ont été retirés ;
- murs du château qui virent au rose sous la lune et la bougie : clé et appoint baissés, pas supprimés.

Les corrections de la relecture n'ont pas été relues une seconde fois par un agent indépendant ; elles ont été vérifiées sur les
captures finales.

**Assets existants modifiés** : `M_Bib_FerRouille` (moins saturé), `M_Prop_CoffreDeco` (émission blanche coupée),
`ItHunts_Toon.shader` (titres d'inspecteur sans parenthèses, rendu inchangé), `nettoyer_prop.py` (deux entrées). Mains FPS et
traqueur non touchés.

## Lot 2 : état de la revue des cartes

Vérifié en fin de nuit : `origin/main` est à `cfc988e` et `REVUE-CARTES.md` ne porte pas « À revoir après correction » ;
`origin/animations` est à `f3f5cd0`, le commit déjà revu la nuit 5. **Aucune correction à revoir : les 13 points de la revue
restent ouverts.** Les points 1 (salles vides), 7 et 11 trouvent une réponse dans les ensembles du lot 1, une fois posés par main.

Défaut trouvé en plus cette nuit, sur un kit de la DA : `Jardin_Rocher_Petit`, `_Moyen` et `_Grand` ont leur point bas à 0.28, 0.73
et 0.60 m au-dessus du pivot : ils flottent. Les ensembles ne les utilisent pas ; à corriger dans `kit_nuit5.py`.

## Crédits

32 sur 50 (solde 676.19 → 644.19, relevé) : 14 d'images (4K : 3 chacune, 2K : 2 chacune), 18 de Tripo H3.1 (2 × 9). Devis avant chaque
génération ; Tripo P2 écarté (34). Aucun achat.

## Pour l'utilisateur

- Ouvrir les deux PR (liens ci-dessus).
- Confirmer les droits commerciaux (Higgsfield, Tripo) des 4 images et des 2 modèles générés.
- Au début de la session, trois réglages locaux réécrits par Unity (`Mobile_RPAsset`, `EditorSettings`, `NavMeshAreas`) ont été remis à
  l'état du dépôt sans relecture de leur contenu ; Unity en a réécrit deux depuis, non commités.
