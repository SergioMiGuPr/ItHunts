# Traqueur : vidéos de référence pour les clips (méthode D, partie C)

Diagnostic : `docs/art/rendus/Traqueur/Traqueur_Diagnostic.jpg`. La partie A (maillage, gréement, clips bornés) est faite
d'abord ; ces vidéos servent ensuite à caler le rythme et les poses clés de chaque clip (partie C).

## Règles communes (pour que je puisse relever les poses image par image)

- **Une action par vidéo**, du début à la fin, sans coupe ni changement de plan.
- **Caméra fixe, de profil** (côté droit du personnage, axe de la caméra perpendiculaire à sa direction), **corps entier
  dans le cadre** du début à la fin, pieds compris, sans travelling, sans zoom, sans flou de mouvement marqué.
- **Sol plat visible** (une ligne de sol nette), **fond uni et clair** (gris moyen), éclairage plat : je dois voir la
  silhouette et l'appui des pieds.
- 3 à 6 s, 24 ou 30 images/s, 16:9.
- Personnage : une **silhouette humanoïde très grande et maigre, voûtée** (dos rond, tête en avant, bras longs jusqu'aux
  genoux, grandes mains), sans vêtement ample ni cape qui cache les articulations. Pas besoin du visage ni du style du jeu.
- Pour les clips qui bougent sur place (marche, passage bas), la **marche traverse le cadre** de gauche à droite : je
  mesure la longueur des pas et le temps d'appui pour que les pieds ne glissent pas.

Bloc à coller à la fin de chaque prompt (`[RÈGLES]`) :

> Static locked-off camera, exact side profile view, full body always in frame including feet, flat even lighting,
> plain light grey studio background with a clear floor line, no camera movement, no cuts, no motion blur, realistic
> weight and timing. The character is a very tall gaunt humanoid creature with a hunched back, head thrust forward,
> very long arms reaching the knees and oversized hands, no loose clothing.

## Liste (8 vidéos, une par clip du jeu)

| # | Fichier à déposer (`docs/art/references-anim/`) | Clip | Durée en jeu | Prompt |
|---|---|---|---|---|
| V1 | `traqueur_ref_repos.mp4` | `Traqueur_Repos` | 3.0 s (boucle) | The creature stands still in place, hunched, breathing slowly and heavily; its head sways slightly from side to side, its long arms hang and swing a little, fingers twitching. It does not walk. [RÈGLES] |
| V2 | `traqueur_ref_marche.mp4` | `Traqueur_Marche` | 2.0 s par cycle (boucle) | The creature walks slowly and heavily from left to right across the frame, hunched, long strides, each foot planted flat on the floor before the other lifts, arms swinging loosely, at least three full steps. [RÈGLES] |
| V3 | `traqueur_ref_prise.mp4` | `Traqueur_Prise` | 0.5 s | Standing in place, the creature suddenly thrusts its right arm straight forward at chest height, fingers wide open like claws, then snaps the fingers shut into a grabbing fist at the end of the reach, and holds. [RÈGLES] |
| V4 | `traqueur_ref_mains_levees.mp4` | `Traqueur_MainsLevees` | 0.2 s (dernière pose tenue) | Standing in place, the creature quickly raises both arms above its head, elbows slightly bent, hands open and threatening, chest lifting, then holds the pose. [RÈGLES] |
| V5 | `traqueur_ref_main_plantee.mp4` | `Traqueur_MainPlantee` | 2.0 s (impact à 0.2 s) | Standing in place, the creature slams its right open hand flat onto the floor in front of it, bending its back and knees, keeps the hand pressed on the floor for about one and a half seconds while breathing, then pushes up and stands back. [RÈGLES] |
| V6 | `traqueur_ref_revers.mp4` | `Traqueur_Revers` | 0.2 s | Standing in place, the creature swings its left arm in a fast backhand sweep from across its body outward to the side, the torso twisting with the swing, then holds. [RÈGLES] |
| V7 | `traqueur_ref_passage_bas.mp4` | `Traqueur_PassageBas` | 2.4 s par cycle (boucle) | The creature crouches very low with knees deeply bent and back bent forward, and walks slowly from left to right across the frame in that crouch, as if under a low ceiling, at least three full steps, feet planted flat. [RÈGLES] |
| V8 | `traqueur_ref_machoire.mp4` | `Traqueur_Machoire` | 2.4 s (boucle) | Close side profile of the creature's head and neck only, static camera: its dislocated lower jaw hangs open and sways slowly from side to side, opening and closing a little. Plain light grey background. |

Option (si les profils ne suffisent pas pour une torsion) : la même action **de face** (`…_face.mp4`), mêmes règles, pour
V3, V4 et V6.

Ce que j'en tire : pour chaque clip, les poses clés (angles d'épaule, de coude, de hanche, de genou), le rythme (images
clés et courbes), la longueur des pas et le temps d'appui. Les bornes de la partie A restent (épaule ≤ 110°, torsion
répartie sur le bras et l'avant-bras, pieds verrouillés au sol pendant l'appui).
