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
La même marche, en miroir : c'est la main gauche qui ouvre le cycle, comme dans la seconde moitié de la vidéo. Le balancement du buste et de la tête est inversé, la tête un peu plus haute. Mêmes mesures que Marche_A.

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
