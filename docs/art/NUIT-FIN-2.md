# Fin de la nuit 2 (2026-11-21) : traqueur v4b et ses animations

Ce fichier sert de **signal de fin pour `main`** : tout le travail de la nuit est livré.

## PR de la nuit

| PR | Branche | Contenu | État |
|---|---|---|---|
| [#27](https://github.com/SergioMiGuPr/ItHunts/pull/27) | `art-traqueur-v4` | traqueur v4b et planche `planche_v4b.png` | fusionnée |
| [#28](https://github.com/SergioMiGuPr/ItHunts/pull/28) | `art-traqueur-anim-1` | `Traqueur_Repos`, `Traqueur_Marche_A`, `Traqueur_Marche_B` | fusionnée |
| [#29](https://github.com/SergioMiGuPr/ItHunts/pull/29) | `art-traqueur-anim-2` | `Traqueur_CourseEnragee_A`, `_B`, `_C` | fusionnée |
| [#30](https://github.com/SergioMiGuPr/ItHunts/pull/30) | `art-traqueur-anim-3` | `Traqueur_Prise`, `Traqueur_Bond`, `Traqueur_BondPrise`, `Traqueur_PriseRebord` | fusionnée |
| [#31](https://github.com/SergioMiGuPr/ItHunts/pull/31) | `art-traqueur-anim-4` | `Traqueur_Reception`, `Traqueur_Escalade`, `Traqueur_ChaineGrimpe` | fusionnée |
| [#32](https://github.com/SergioMiGuPr/ItHunts/pull/32) | `art-traqueur-anim-5` | `Traqueur_ChaineSaut`, `Traqueur_ChainePrise`, `Traqueur_PorteEtroite` | fusionnée |
| cette PR | `art-fin-2` | ce fichier | ouverte |

Détail de la PR #27 (traqueur v4b) :
- modèle Tripo de l'utilisateur sans déformation, une seule échelle uniforme pour 14 m ;
- tête à × 1.05 : la largeur de son cou est égale à celle du cou du corps ;
- pagne gardé ;
- bouche avec dents et 6 formes lisibles ;
- 48 mèches ;
- **modèle verrouillé** (`EMPREINTE.md`).

**18 clips** livrés au total, dans `Assets/Art/Animations/Traqueur_v4/`.

Où trouver le reste :
- durées, boucles, root motion, instants clés et vitesses : `docs/art/ART-HANDOFF.md`, section « Clips du traqueur v4 » ;
- contrôles (vidéos côte à côte, planches de 6 instants, superpositions, captures Unity) : `docs/art/rendus/Traqueur/v4/anim/` (`ANIMATIONS.md`).

## Verrou du modèle

Après chacun des 5 lots, l'empreinte du modèle est **identique** à `docs/art/rendus/Traqueur/v4/EMPREINTE.md` (sommets, triangles, boîtes, poids, formes, longueurs des 68 os).

Aucune clé d'échelle n'a été posée, ni aucune clé de position en dehors de `Racine` et `Bassin`. L'export refuse un clip fautif.

## Ce qui n'est pas fait

| Point | Raison |
|---|---|
| `Traqueur_MurGrimpe` | il n'y a pas de vidéo « grimper un mur » |
| `Traqueur_Visage_Cri`, `_Grognement`, `_Machoire` | il n'y a pas de vidéo du visage ; les 6 formes sont sur le modèle |
| Capture vidéo vers 3D (DeepMotion, Rokoko Vision) | elle attend ta connexion ; les vidéos sont prêtes dans `docs/art/references-anim/capture/` (voir `LISEZMOI.md`) |

## Écarts et défauts connus

- **Silhouette** : dans les vidéos, les bras sont plus longs (1.4 fois les jambes). Le modèle n'est pas étiré : le dos, les épaules et les hanches plient davantage, et les bras restent plus pliés.
- **Marche** : `Traqueur_Marche_B` suit exactement les côtés de la vidéo ; `_A` en est le miroir.
- **Porte étroite** :
  - un à-coup des doigts gauches à 6.6 s (2 images) ;
  - le corps passe dans 4 × 5.2 m, mais les membres s'écartent jusqu'à 3.6 m de l'axe au ras du sol.
- **Poing** : le modèle n'a qu'un os pour tous les doigts, donc le poing se lit comme un crochet de griffes.
- **Chaîne** : la chaîne est supposée droite ; dans la vidéo, elle se balance.
