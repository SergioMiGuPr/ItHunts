# Traqueur v4 : animations (clés à la main relevées sur les vidéos)

## Méthode

Tous les clips sont à **30 images/s**. Les scripts sont dans `Assets/Art/Source~/Ennemis/scripts/` :
- `anim_clips.py` : un clip par fonction ;
- `anim_traqueur.py` : outils de pose et contrôles.

Le fichier Blender est `Assets/Art/Source~/Ennemis/Traqueur_v4/SK_Traqueur_v4_Anim.blend`. Les FBX sont dans `Assets/Art/Animations/Traqueur_v4/` ; il y en a un par clip, avec l'armature seule.

Comment une pose est construite :
- **Relevé sur la vidéo** : images extraites à 24 i/s, planches toutes les 3 ou 4 images, puis mesure automatique de la silhouette image par image (centre, points au sol). Ces mesures donnent le rythme, les instants d'appui et la vitesse.
- **Poses** : chaque os est orienté par une direction relevée dans le plan de profil. Une clé est posée à chaque image (2 images pour le repos), sur des courbes calculées pour passer par les poses relevées aux mêmes instants que la vidéo.
- **Appuis** : un pied ou une main en appui reste sur un point fixe du sol, tenu par une IK analytique. La main et le pied gardent leur orientation complète, sans torsion parasite. Le talon se déroule autour du bout du pied avant de décoller.
- **Adaptation aux proportions** (bras et jambes du modèle plus courts que dans la vidéo, sans rien étirer) :
  - le dos et les épaules plient davantage ;
  - les mains sont posées à 96 % de l'allonge du bras ;
  - à quatre pattes, les mains touchent le sol.
- **Mouvements secondaires** : respiration, balancement du buste, tête, clavicules qui avancent avec le bras, doigts, mâchoire. Les cheveux pendent avec un léger retard.

Contrôles faits sur chaque clip :

| Contrôle | Méthode | Seuil attendu |
|---|---|---|
| Verrou du modèle | `verifier_action` | aucune clé d'échelle, aucune position hors `Racine` et `Bassin` |
| Empreinte | `empreinte()` comparée à `empreinte.json` | identique |
| Glissement | point de contact pendant l'appui | nul, hors déroulé du talon ou des griffes |
| Fluidité | vitesse et variation de vitesse angulaires de chaque os, raccord de boucle compris | pas de saut |
| Côte à côte | vidéo et animation à 30 i/s, même durée, même angle | — |
| Planche | 6 instants identiques | — |
| Superposition | vidéo et animation à 50 % | — |
| Capture Unity | le clip appliqué au modèle | — |

## Lot 1 (PR `art-traqueur-anim-1`)

### Traqueur_Repos
Vidéo de référence : `traqueur_ref_repos` (5 s, profil). Boucle de **3.2 s** (96 images), sans root motion.

Relevé :
- un cycle de respiration : le centre de la silhouette monte de 0 à 1.8 s, puis redescend jusqu'à 3.2 s ;
- buste très arrondi, cou à l'horizontale, tête pendante (le sommet du crâne en avant) ;
- bras ballants jusqu'à mi-tibia, genoux un peu fléchis.

Mouvements de l'animation :
- le buste se soulève à l'inspiration, les clavicules aussi ;
- les genoux fléchissent de 5 cm ;
- la tête se balance latéralement ;
- les bras se balancent avec un décalage entre les deux côtés ;
- les doigts frémissent, la mâchoire bouge.

Contrôles :
- glissement des pieds : **0.3 mm** ;
- verrou : OK ;
- vitesse angulaire maximale : 3.2°/image (doigts) ; à-coup maximal : 1.5°/image², au raccord de boucle compris, avec des courbes cycliques.

Fichiers : `Traqueur_Repos_cote_a_cote.mp4`, `_planche6.jpg`, `_superposition.jpg`.

### Traqueur_Marche_A
Vidéo de référence : `traqueur_ref_marche` (5 s, profil, caméra fixe). Boucle de **5.5 s** (165 images), **root motion** de 7.0 m par cycle, soit **1.27 m/s**.

Relevé :
- la marche est **lente et à quatre pattes** ;
- les appuis ont été relevés sur les points au sol de la silhouette, image par image ;
- moments de pose en fraction du cycle :
  - main droite : 0.10, soit **0.55 s** ;
  - jambe gauche : 0.44, soit **2.42 s** ;
  - main gauche : 0.61, soit **3.36 s** ;
  - jambe droite : 0.91, soit **5.0 s** ;
- c'est une séquence diagonale ;
- durée en l'air : **25 % du cycle** pour une main (1.4 s : le bras part loin devant), **15 %** pour un pied (0.8 s) ;
- posture : dos presque horizontal avec la bosse, épaules plus basses que les hanches, tête basse.

Contrôles :
- glissement : pieds 3 à 4 cm (seulement pendant le déroulé du talon), mains 4 cm (seulement pendant le déroulé des griffes) ; nul en appui à plat ;
- verrou : OK ;
- extension maximale : bras 96 %, jambes 85 %.

**Écarts connus.** Dans la vidéo, les bras sont 1.4 fois plus longs que les jambes. Sur le modèle, les bras paraissent donc plus pliés au milieu de l'appui, et la créature paraît plus trapue.

### Traqueur_Marche_B
La même marche, en miroir : c'est la main gauche qui ouvre le cycle.

**Correction (lot 3)** : de profil, avec la créature tournée vers la droite de l'image, on voit son côté **droit**. Dans la vidéo, la première main posée est donc la gauche (la main éloignée). **C'est `Traqueur_Marche_B` qui suit exactement les côtés de la vidéo** ; `_A` en est le miroir. Le balancement du buste et de la tête est inversé, la tête un peu plus haute. Mêmes mesures que Marche_A.

## Lot 2 (PR `art-traqueur-anim-2`) : courses enragées

Les trois vidéos sont filmées **de face** : la caméra recule avec la créature, dans un couloir. Le rythme a été relevé image par image sur la silhouette, après retrait du fond (médiane temporelle) : largeur (un bras écarté vers un mur), bas (une main qui se pose), centre (le corps qui va vers un mur).

Les murs sont placés à **7 m de l'axe** : c'est un couloir de 14 m, comme ceux du jeu. Les vitesses reprennent celles du jeu, car la caméra qui suit ne permet pas de mesurer la vitesse réelle : 11 m/s en enragé, 9 m/s en chasse. La cadence, elle, est celle de la vidéo.

Les rendus de contrôle sont faits de face, avec une caméra qui suit la créature.

### Traqueur_CourseEnragee_A
Vidéo de référence : `traqueur_ref_course_enragee`.

| Réglage | Valeur |
|---|---|
| Durée | boucle de 2 cycles, **2.6 s** |
| Root motion | **11 m/s**, foulée de 14.3 m |
| Allure | course à quatre pattes ; une main se pose toutes les **0.65 s** (cycle de 1.3 s, relevé sur le bas de la silhouette) |
| Moments de pose (s) | main droite 0, 1.3 ; pied gauche 0.2, 1.5 ; main gauche 0.65, 1.95 ; pied droit 0.85, 2.15 |
| Frappes au mur | main gauche à 0.25 s, main droite à 0.9 s, une pose sans frappe à 1.5 s, main droite à 2.2 s |

Pendant une frappe, le bras part frapper le mur au milieu de son vol, en passant par l'extérieur ; il touche le mur 0.1 s, puis revient se poser.

Posture : dos bas, tête relevée (le visage visible comme dans la vidéo), mâchoire ouverte.

Contrôles :
- glissement : pieds 1 à 3 cm, mains 0 à 4 cm (déroulé) ;
- verrou : OK ;
- vitesse angulaire maximale : 40°/image (doigts, pendant la frappe) ; le geste vers le mur dure environ 0.25 s, comme le flou de la vidéo.

### Traqueur_CourseEnragee_B
Vidéo de référence : `traqueur_ref_course_enragee_profil` (filmée de face elle aussi).

| Réglage | Valeur |
|---|---|
| Durée | boucle de 3 cycles, **3.9 s** |
| Root motion | **11 m/s** vers l'avant, plus un **décalage latéral** de 2.2 m vers le mur, qui revient au centre à chaque boucle |
| Déroulé | élan vers le mur droit (0.2 à 1.5 s : main droite plaquée au mur à 0.9 s, buste penché vers le mur), frappe droite à 2.2 s, élan vers le mur gauche (1.7 à 2.8 s, main gauche au mur à 2.9 s), puis bonds au centre |

Contrôles :
- glissement : pieds 1 à 3 cm, mains 1 cm ;
- verrou : OK ;
- vitesse angulaire maximale : 67°/image (avant-bras, pendant le balayage vers le mur : environ 10 m en 0.25 s). C'est le geste le plus brusque du lot, et il reste continu.

### Traqueur_CourseEnragee_C
Vidéo de référence : `traqueur_ref_course_subjective`. Malgré son nom, elle est filmée de face : la créature y court sur deux jambes.

| Réglage | Valeur |
|---|---|
| Durée | boucle de 2 cycles, **2 s** |
| Root motion | **9 m/s** |
| Allure | course voûtée sur les deux jambes ; pas toutes les 0.5 s (pied gauche à 0 et 1 s, pied droit à 0.5 et 1.5 s) |
| Bras | libres : ils pendent devant, se balancent à l'opposé des jambes, puis partent frapper les murs |
| Frappes au mur | main droite à **0.6 s**, main gauche à **1.6 s**, mêmes instants que la vidéo ; le buste roule vers le mur frappé |

Contrôles :
- glissement : 3 mm ;
- verrou : OK ;
- vitesse angulaire maximale : 37°/image (doigts).

## Lot 3 (PR `art-traqueur-anim-3`) : prise, bond, bond-prise, prise depuis un rebord

Ces clips d'action utilisent le **moteur pose à pose** :
- des poses clés aux instants relevés sur la vidéo ;
- des courbes monotones entre les clés, pour qu'il n'y ait ni dépassement, ni arrêt figé, ni pied qui dérive ;
- des appuis tenus par IK sur des points fixes.

Les rendus de contrôle sont faits de profil, à l'échelle de la vidéo. Le bloc de la vidéo est ajouté pour le contrôle, mais il n'est pas exporté.

**Côté vu dans les vidéos de profil** : la créature est tournée vers la droite, on voit donc son **côté droit**. Le bras qui saisit est le **bras droit**.

### Traqueur_Prise
Vidéo de référence : `traqueur_ref_prise`, de 0.8 à 4.3 s. Durée **3.5 s**, sans boucle.

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| départ | à genoux, tibias au sol, mains au sol, dos voûté, tête à hauteur d'épaule |
| 0.45 s | le bras droit part droit devant, à hauteur d'épaule, doigts ouverts en griffe |
| **0.70 s** | bras tendu ; l'épaule avance avec la clavicule |
| jusqu'à 2.37 s | bras tenu, tendu |
| **2.37 → 2.6 s** | les doigts se referment en **poing** : c'est la prise |
| jusqu'à 3.5 s | poing tenu |

Contrôles :
- genoux et main gauche fixes au sol (glissement de 1.6 cm, à la pose des genoux) ;
- verrou : OK ;
- superposition très proche de la vidéo.

**Limite** : le modèle n'a qu'un os pour tous les doigts (`Doigts`). Le poing se lit donc comme un crochet de griffes.

### Traqueur_Bond
Vidéo de référence : `traqueur_ref_bond`, de 0 à 6 s. Durée **6.0 s**, sans boucle.

Mesures sur la vidéo, à l'échelle du modèle :
- bond de **19 m** sur un **bloc de 5.4 m** ;
- les hanches montent jusqu'à environ 11.9 m.

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| 0 → 1.0 s | accroupi à quatre pattes |
| 1.0 → 1.75 s | il se ramasse |
| **2.05 s** | détente et décollage ; les pieds quittent le sol verticalement, sans glisser |
| 2.05 → 4.6 s | vol de 2.55 s : montée rapide, plané haut, corps tendu, bras devant griffes ouvertes, jambes traînées puis ramenées sous le corps |
| **4.6 s** | les mains se posent sur le bloc |
| **4.85 s** | les pieds se posent ; il reste accroupi sur le bloc |

Root motion : 19.4 m vers l'avant et 5.4 m vers le haut.

Contrôles : glissement nul, verrou OK.

### Traqueur_BondPrise
Vidéo de référence : `traqueur_ref_bond_prise`, de 0 à 5 s. Durée **5.0 s**, sans boucle.

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| 0 → 0.9 s | accroupi |
| **1.05 s** | détente : bond rasant de 8 m, bras droit tendu devant |
| **2.55 → 2.72 s** | **poing**, prise en l'air |
| **3.0 s** | réception des mains |
| **3.15 s** | réception des pieds ; le poing est ramené contre la poitrine et tenu |

Contrôles : glissement de 1 cm (pose des pieds), verrou OK.

### Traqueur_PriseRebord
Vidéo de référence : `traqueur_ref_prise_rebord`, de 0 à 6 s. Durée **6.0 s**, sans boucle.

Départ : accroupi au bord d'un **rebord de 6 m** (le clip est à jouer au sommet du rebord ; la racine reste à 6 m).

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| 1.25 s | le bras droit part vers l'avant et le bas |
| **1.75 → 3.1 s** | bras tendu, griffes ouvertes à 6.3 m devant l'épaule, à mi-hauteur du rebord |
| **3.25 s** | **poing** |
| 4.4 → 5.2 s | il remonte le poing contre la poitrine |

Contrôles : appuis fixes, verrou OK.

## Lot 4 (PR `art-traqueur-anim-4`) : réception, escalade, chaîne (grimper)

Le moteur pose à pose a été amélioré :
- l'orientation des mains reprend leur orientation de repos, simplement pivotée vers la direction voulue : elle n'est plus jamais indéterminée ;
- le coude et le genou restent du même côté d'une image à l'autre ;
- les mains ne traversent plus les épaules pendant les transitions.

Ces trois corrections ont supprimé les bascules du coude.

### Traqueur_Reception
Vidéo de référence : `traqueur_ref_reception`, de 0.2 à 5.2 s. Durée **5.0 s**, sans boucle.

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| départ | chute en diagonale, corps penché vers l'avant, bras tendus vers le sol |
| **0.35 s** | les mains se posent |
| **0.5 s** | les pieds se posent |
| **0.85 s** | écrasé au plus bas |
| **1.1 s** | stable : **sortie possible vers une course dès ce moment** |
| 1.1 → 5.0 s | tapi, il respire |

Root motion : la racine avance de 6 m et descend de 8.5 m pendant la chute.

Contrôles : glissement nul, verrou OK.

### Traqueur_Escalade
Vidéo de référence : `traqueur_ref_escalade`, de 2.5 à 7.0 s. Durée **4.5 s**, sans boucle.

Départ : il arrive à quatre pattes contre un **bloc de 7.5 m** (la hauteur est mesurée sur la vidéo).

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| **0.25 s** | la main droite agrippe le haut du bloc, en passant par le côté |
| **0.6 s** | la main gauche agrippe le haut |
| 1.0 → 2.2 s | debout contre le bloc |
| 2.35 s | il fléchit |
| **2.55 s** | il se hisse d'une détente ; les mains se replacent plus loin sur le bloc pendant la poussée, pour que le corps ne passe pas au-dessus d'elles |
| **3.25 s** | les pieds se posent sur le bloc |
| jusqu'à 4.5 s | accroupi en haut |

Root motion : il avance de 9.5 m et monte de 7.5 m.

Contrôles : glissement de 6 mm, verrou OK ; vitesse maximale de 53°/image, pendant la détente.

### Traqueur_ChaineGrimpe
Vidéo de référence : `traqueur_ref_chaine_grimpe`, de 2.0 à 7.0 s. Durée **5.0 s**, sans boucle.

Départ : accroupi devant une chaîne pendante.

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| **0.35 s** | il saute ; les bras passent tendus par l'avant |
| **0.55 s** | les mains attrapent la chaîne en haut |
| 0.9 s | le corps pend sous les mains, jambes repliées serrées contre la chaîne |
| 1.1 → 2.9 s | montée main sur main : **2 cycles de 0.9 s, +2.4 m par cycle** (main droite, pieds, main gauche). **Cette partie boucle** pour une chaîne plus longue (le module `ChaineGrimpe_12m` demande environ 3 cycles de plus) |
| jusqu'à 5.0 s | tenu replié |

Contrôles :
- mains et pieds fixes sur la chaîne pendant chaque prise ;
- verrou : OK ;
- le geste le plus rapide est le saut vers la chaîne (60°/image sur 0.2 s, comme dans la vidéo).

**Écart** : dans la vidéo, la chaîne se balance. La chaîne du jeu est un objet du décor ; le clip la suppose droite.

## Lot 5 (PR `art-traqueur-anim-5`) : chaîne (saut, prise), porte étroite

### Traqueur_ChaineSaut
Vidéo de référence : `traqueur_ref_chaine_saut`, de 0 à 6 s. Durée **6.0 s**, sans boucle.

Le balancement suit un pendule dont le point d'accroche est à 40 m (hors champ, comme dans la vidéo).

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| 0 → 2.0 s | pendu replié sur la chaîne, avec un léger balancement |
| 2.0 → 2.8 s | grand balancement vers l'arrière (−12°) |
| 2.8 → 3.4 s | balancement vers l'avant (+11°) |
| **3.4 s** | **il lâche la chaîne** |
| 3.4 → 4.2 s | vol corps tendu, bras devant ; les deux tiers du trajet sont faits à 3.6 s, comme dans la vidéo |
| **4.2 s** | les mains se posent sur un **bloc de 3.3 m**, à 15 m de la chaîne |
| **4.35 s** | les pieds se posent ; il reste accroupi |

Contrôles :
- verrou : OK ;
- pics lissés au lâcher, quand bras et jambes sont libres : 37°/image au maximum ;
- pieds et griffes jamais sous le sol.

### Traqueur_ChainePrise
Vidéo de référence : `traqueur_ref_chaine_prise`, de 0 à 6 s. Durée **6.0 s**, sans boucle.

Départ : pendu à la chaîne par les jambes et la main gauche.

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| départ | la main droite lâche la chaîne et passe par l'extérieur |
| **0.75 → 3.5 s** | le buste bascule ; la main droite est tendue vers l'avant et le bas, griffes ouvertes |
| **3.75 s** | **poing** (prise) |
| 4.0 → 5.0 s | il remonte ; la main droite reprend la chaîne |
| jusqu'à 6 s | tenu replié |

Contrôles :
- la main gauche et les pieds restent fixes sur la chaîne ;
- verrou : OK ;
- 39°/image au maximum.

### Traqueur_PorteEtroite
Vidéo de référence : `traqueur_ref_porte_etroite`, de 0 à 7 s. Durée **7.0 s**, sans boucle.

Allure : marche à quatre pattes à **1.0 m/s** (2 cycles de 3.5 s, même ordre d'appuis que la marche). Elle suit les côtés de la vidéo : la main gauche ouvre le cycle.

Déroulé :

| Temps | Ce qui se passe |
|---|---|
| **2.5 → 3.5 s** | le corps s'abaisse : la racine descend de 2.3 m, le dos s'aplatit, les coudes se serrent vers l'arrière et les genoux rentrent sous le corps |
| vers 3 s | tête et bras passent la porte |
| 3.5 → 5 s | le buste passe |
| jusqu'à 7 s | les jambes passent |

Mesures au droit de la porte, sur le modèle animé :
- hauteur maximale **5.2 m** ;
- le buste tient dans **4 m de large** ;
- mains, griffes et genoux s'écartent jusqu'à **3.6 m de l'axe** près du sol pendant les pas.

**La plus petite porte franchissable avec ce clip est donc d'environ 4 m de large et 5.5 m de haut pour le corps, avec un dégagement d'environ 7 m au ras du sol pour les membres.** L'estimation « 4 × 6 m » de la v4 est confirmée pour le corps. Les membres demandent une ouverture plus large en bas, ou une collision qui ignore les griffes.

Contrôles :
- glissement : 3 cm pendant le déroulé du talon, mains fixes ;
- verrou : OK.

**Défaut restant** : un à-coup des doigts gauches à **6.6 s** (77°/image pendant 2 images, au moment où la main quitte le sol). Les essais de lissage déplaçaient la main posée ou faisaient passer les griffes sous le sol, donc il n'est pas corrigé.

## Clips sans vidéo : non faits (signalé)

- **`Traqueur_MurGrimpe`** : il n'y a pas de vidéo « grimper un mur » dans `docs/art/references-anim/`. `traqueur_ref_escalade` montre un rebord de 7.5 m, et c'est `Traqueur_Escalade`.
- **`Traqueur_Visage_Cri`, `_Grognement`, `_Machoire`** : il n'y a pas de vidéo du visage. `traqueur_ref_machoire.mp4`, prévue dans la liste V8, n'a pas été livrée. Les 6 formes du visage (blend shapes) existent sur le modèle et peuvent être animées par le code en attendant.
