# Vue FPS et HUD

**Status:** implemented · **Updated:** 2026-11-12 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md)

On ne voit que ses **mains** et l'**orbe**, dans la paume droite (BRIEF). Le HUD se lit sans quitter le viseur des
yeux, même à pleine vitesse, et il reste dans la zone d'écran de chaque joueur (écran partagé, étape 9). Greybox
jusqu'à l'étape direction artistique : formes simples, mesures fixes pour que la DA puisse remplacer les mains sans
retouches ([ART-HANDOFF](../../../art/ART-HANDOFF.md)). Décision D-028.

## Règles

1. **Rendu des mains (option 2, D-028) :** pas de caméra en plus ; dans la caméra du joueur, une passe « Render
   Objects » du renderer URP dessine les mains à **champ de vision fixe de 60°**, après une passe qui **efface la
   profondeur** : elles ne traversent jamais les murs et ne s'étirent pas avec le FOV de vitesse (70 → 82°). Une couche
   par joueur (`FirstPersonP1` à `P4`) : la caméra d'un joueur ne voit que la sienne (écran partagé). Le HUD est
   dessiné juste après par une autre passe (file transparente). Choisie après mesure dans un build : la caméra
   superposée (option 1) coûtait trop.
2. **Mains :** deux mains greybox ; l'**orbe dans la paume droite**, avec sa couleur selon l'action (D-024) ; la
   **main gauche** qui tient l'artefact (le barillet, D-032 : recul à chaque tir, abaissée quand il est inactif).
   Les mains ne reçoivent pas l'occlusion ambiante (SSAO) du décor situé derrière elles.
3. **Mouvement des mains** (réglé par l'**intensité du mouvement des mains**, réglage de confort du joueur de 0 à
   100 %, 100 % par défaut, enregistré par joueur) : **balancement** quand la vue tourne, **balancier de course**
   (proportionnel à la vitesse au sol, rien en l'air), **plongée à l'atterrissage** (selon la vitesse de chute).
4. **Poses d'état et gestes** (sans l'intensité) : mains plus basses et inclinées en glissade, recul avec la
   vitesse ; main droite : **bras tendu en traction**, **main relevée qui tient un objet**, **poussée** à la
   répulsion, **lancer** (recul puis poussée), **main qui se tend** pour attraper. Un geste démarre à l'image où le
   tick qui le produit est affiché. Ils n'influencent jamais la simulation.
5. **HUD (uGUI)**, un canvas sur la caméra de chaque joueur :
   - **autour du viseur** (comme avant) : point, anneaux (bleu traction, orange répulsion, puis violet / vert /
     rouge pour la télékinésie), charges de mouvement, charges d'attraction, répulsion avec le temps restant, refus
     en rouge ;
   - **dans le monde** : chiffres de dégâts, barres de PV, carrés de durabilité de l'objet tenu ;
   - **coins réservés, sans contenu** : âmes (haut gauche), timer (haut droite), traqueur enragé (sous le timer) ;
   - **mini-carte ronde** (haut droite, habillage de la DA) et **carte ouverte** (Tab maintenu) : même texture peinte depuis
     le plan de l'acte, ennemis marqués seulement dans le champ de vision du joueur (D-101) ; règles détaillées dans
     [run et niveaux](../run-et-niveaux/design.md), règle 11.
6. **HUD discret** (jauges seulement) : une jauge **pleine et inutilisée** s'estompe à 40 % après 1.5 s ; toute
   charge utilisée, tout refus et toute recharge en cours la ramènent **tout de suite à 100 %** ; une charge qui
   revient **clignote une fois**.
7. **Debug F3 / Select** en trois pages (mouvement, orbe, combat et vue), puis caché.

## Valeurs

`FpsView_Default` et `Hud_Default` ; l'intensité du mouvement des mains est sur le joueur (`FirstPersonView`).

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Champ de vision de la caméra des mains | 60 | ° | taille des mains, lisibilité |
| Main droite au repos (repère caméra) | (0.21, −0.22, 0.47), rotation (−15, −10, 0) | m, ° | |
| Main gauche au repos | (−0.23, −0.25, 0.45), rotation (−10, 12, 0) | m, ° | |
| Orbe : diamètre / centre dans la main droite | 0.08 / (0, 0.06, 0.045) | m | |
| Balancement : par degré, max, retour | 0.002 m/°, 0.04 m et 4°, 10/s | | mal au cœur ? |
| Balancier de course : amplitude, fréquence | 0.015 m, 1.8 pas/s à 7.5 m/s (borné à ×1.5) | | |
| Plongée à l'atterrissage | 0.04 m pour 10 m/s de chute (max 0.08), retour 8/s | | |
| Glissade | 0.06 m plus bas, 6° | | |
| Recul avec la vitesse | 0.03 m à 20 m/s | | |
| Gestes : répulsion / lancer / prise | 0.15 / 0.20 / 0.15 | s | |
| Intensité du mouvement des mains | 100 | % | confort |
| HUD : référence, échelle, marge des coins | 1920 × 1080, 1, 5 % | | |
| HUD discret : opacité, délai, vitesse | 40 %, 1.5 s, 1.5 /s | | trop discret ? |
| Clignotement d'une charge revenue | 0.2 s, luminosité ×1.8 | | |
| Plafond du bonus de vitesse au lancer | 20 | m/s | à rééquilibrer après des runs complètes |
| Immunité après un étourdissement | 2 | s | |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Orbes](../orbes/design.md) | couleur de l'orbe, anneaux du viseur, gestes, durabilité de l'objet tenu |
| [Artefacts](../artefacts/design.md) | la main gauche accueillera l'artefact (étape 5 bis) |
| [Boss traqueur](../boss-traqueur/design.md) | coin réservé à son état enragé |

## Cas particuliers

- Collé à un mur : les mains restent entières (caméra des mains).
- FOV de vitesse : les mains gardent la même taille (mesuré : 4.8 % de l'écran à 70°, 4.7 % à 81°).
- Écran partagé : chaque HUD reste dans sa moitié ; chaque caméra des mains ne dessine que les siennes.
- Les traits de traction et de télékinésie partent de l'orbe telle qu'elle est vue (les deux caméras n'ont pas le
  même champ de vision : le point de départ est recalculé).

## Questions ouvertes

- **Contour cel-shading des mains (étape DA) :** avec la passe de rendu, les mains sont dessinées après le monde, sur
  une profondeur effacée ; la texture de profondeur de la caméra (copiée avant) ne contient pas les mains. Un contour
  en post-process qui lit cette profondeur ne les entourera donc pas : il faudra un contour par le matériau (coque
  inversée, « inverted hull ») ou une passe de contour dédiée à la couche des mains. Ce n'était pas plus simple avec
  la caméra superposée (profondeur séparée elle aussi).
- Légères taches sombres près des mains (l'occlusion ambiante, SSAO, calculée sans les mains) : à revoir à l'étape DA.
- Menu d'options : l'intensité du mouvement des mains n'a pas encore d'écran ; réglable dans l'inspecteur du joueur.

## History

- 2026-11-12 : in progress : caméra des mains, mains greybox, gestes, HUD uGUI, HUD discret, debug en pages (D-028).
- 2026-11-12 : barres de PV, chiffres et durabilité collés à leur cible en mouvement (position calculée sans rayon vers le canvas).
- 2026-11-12 : option 2 après mesure dans un build (passe de rendu « Render Objects », sans caméra en plus) ; implemented.
- 2026-11-23 : D-101, mini-carte et carte ouverte : même texture à l'encre déduite du plan, ennemis à vue (renvoi à la règle 11 de run et niveaux).
