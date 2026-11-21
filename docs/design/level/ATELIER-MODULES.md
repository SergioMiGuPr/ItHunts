# Atelier des modules

L'atelier sert à construire soi-même les salles (modules) du niveau aléatoire dans Unity. On les sauvegarde, on les
vérifie, on les teste, et on les retrouve ensuite dans la génération.

![Scène de l'atelier](captures/atelier_scene.png)

## 1. Ouvrir l'atelier

- Ouvrez **ItHunts > Atelier : ouvrir la scène Atelier_Modules** et **ItHunts > Palette des modules**.
  Ancrez la palette à côté de la vue Scene.
- Le sol a une grille. Choisissez son pas en haut de la palette : **1 m, 2 m ou 4 m**.
- **Éclairage du jeu** passe dans le noir, avec seulement les lumières du module. Recliquez pour revenir à l'éclairage neutre.
- À l'ouest, des repères d'échelle :
  - le joueur (1,8 m) ;
  - le traqueur v4, debout (14 m) et baissé (8,5 m) ;
  - la plus petite porte qu'il franchit (4 × 5,2 m) ;
  - la limite du hissage (5,5 m).

## 2. Créer ou ouvrir un module

Onglet **Module** de la palette :

- **Nouveau module** : donnez un nom, le type de salle (salle ou carrefour), la forme (qui fixe la hauteur du plafond), le
  nombre de cases (1 ou 2) et le côté (multiple de 4 m).
  - L'atelier pose la **zone limite** (boîte jaune), deux **entrées** (nord et sud) et une **coque d'aperçu**.
  - La coque représente le sol et des murs bas : ce que le générateur ajoutera autour. Elle n'est jamais sauvegardée.
- **Charger** : choisissez un module dans la liste, qui contient aussi les modules du jeu (prison à étages, caserne,
  citerne, puits…).
- **Dupliquer** : copie le module sous un autre nom. La copie est mise hors génération jusqu'à sa vérification.
- **Supprimer** : supprime le module et le retire de la génération, après confirmation.

## 3. Poser et tourner des pièces

![Palette et pose](captures/atelier_palette.png)

| Action | Commande |
|---|---|
| Choisir une pièce | Onglet **Pièces**, choisir une catégorie, puis cliquer sa vignette |
| Poser la pièce | Clic dans la vue Scene (on peut poser plusieurs fois de suite) |
| Tourner de 90° | **R** (sens inverse : **Maj+R**) |
| Tourner de 15° | **Ctrl+R** |
| Monter ou descendre | **Alt + molette** |
| Arrêter de poser | **Échap** |
| Dupliquer, annuler | **Ctrl+D**, **Ctrl+Z** |
| Déplacer | Outils normaux d'Unity |

- Une pièce s'aimante à la grille. Elle s'aimante aussi aux coins des pièces voisines (case à cocher en haut de la palette).
- Les pièces sont des versions « jeu » : elles ont déjà leurs collisions et des réglages pour le NavMesh. Celles marquées
  « collisions approchées » ont une boîte autour, ou des jambages et un linteau pour les portes.
- Les objets attrapables, les plaques magnétiques et les coffres gardent leur lueur.

## 4. Ajouter le gameplay

La catégorie **Gameplay** de la palette contient les éléments suivants. Chacun a une étiquette dans la vue Scene.

- **Connecteur** : une entrée vers un couloir.
  - Il se place tout seul au bon endroit quand on change son **côté** ou sa **case** dans l'inspecteur.
  - Réglez aussi la **taille de la porte** et cochez **obligatoire** si besoin.
  - Le bouton **+ Entrée** de l'onglet Module en ajoute un.
- **Zone limite** : rien de praticable ne doit en sortir.
- **Coffres** (facile, moyen, difficile).
- **Apparitions** de goules ou de volants, et **habitants**.
- **Passages du traqueur** : bond, escalade de rebord, mur grimpable, chaîne à grimper, saut de chaîne, porte étroite.
  - Placez le départ, puis l'enfant **Arrivée**.
  - Le jeu en tire tout seul ses liens de navigation.
- **Objets attrapables**, **plaque magnétique**, **eau**, **lumières**.

Le **marchand**, la **salle verrouillée** et l'**arrivée du traqueur** se posent et se sauvegardent. Le générateur ne les
utilise pas encore : il pose le marchand dans la salle de départ, les salles verrouillées le long des couloirs, et fait
arriver le traqueur par son repaire.

## 5. Sauvegarder et mettre dans la génération

- **Sauvegarder** écrit deux fichiers :
  - le prefab, dans `Assets/_Project/Level/Modules/Atelier/` ;
  - la fiche (nom, taille, connecteurs, difficulté, poids, zone, coûts), dans `Assets/_Project/Level/Modules/Atelier/Fiches/`.
- **Actif dans la génération** met le module dans la bibliothèque du générateur.
- Un module n'est tiré au sort qu'**une fois validé** (voir la section suivante).
- **Poids d'apparition** : réglez-le plus haut pour voir le module plus souvent.

## 6. Vérifier

![Vérification et repères](captures/atelier_verification.png)

**Vérifier le module** sauvegarde le module, puis contrôle les points suivants :

- **Entrées** : à leur place sur la grille des couloirs, et assez grandes pour le traqueur (4 × 5,2 m).
- **Voies du traqueur** : rien de solide sous 10 m entre les entrées.
- **NavMesh** : les goules et le traqueur passent d'une entrée à l'autre.
- **Cachettes** : aucun point où l'on tient debout n'est hors de portée de sa main.
- **Trous et sorties** : aucun sol praticable collé au plafond (sortie par hissage).
- **Passages du traqueur** : distance et hauteur permises, chaque bout sur son NavMesh.
- **Coffres** : atteignables par le joueur, à pied, en sautant ou en se hissant, ou en traction.
- **Portes** : le rapport dit lesquelles il franchit, et pour lesquelles seule sa main passe.
- **Coût** : nombre de pièces, de triangles et de lumières.

Le rapport s'affiche dans la palette. Des **repères** apparaissent dans la vue Scene : rouge pour une erreur (cachette,
entrée mal placée, passage invalide, coffre hors d'atteinte), jaune pour un avertissement.

## 7. Tester

- **Jouer ce module** lance le niveau aléatoire avec ce module, même s'il n'est pas encore validé. Le joueur part dans sa
  salle, avec le traqueur et les goules. L'atelier se rouvre à la sortie du Play Mode.
- Dans le build, ouvrez le menu **Playtest (F3)**, entrée **Modules de l'atelier**. ◄ ► choisit le module.

## Exemple

Ouvrez **Exemple de l'atelier** (bouton Charger) : une salle sur deux niveaux, construite avec l'outil, qui passe la
vérification. On peut s'en servir comme point de départ, avec **Dupliquer**.
