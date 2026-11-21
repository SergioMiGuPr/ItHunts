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
