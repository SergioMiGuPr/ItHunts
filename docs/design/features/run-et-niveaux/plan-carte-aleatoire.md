# Carte aléatoire assemblée à partir de modules : plan (rien de codé)

**Updated:** 2026-11-15 · [Design](design.md) · statut : **plan, en attente de la réponse de l'utilisateur**

But : créer les salles et les couloirs séparément (modules), puis les assembler au hasard avec la seed de la run, pour une
carte différente à chaque partie, en respectant les règles du jeu.

## 1. Ce qu'un module déclare

Un module est un prefab dans `Assets/_Project/Prefabs/Modules/`, avec le composant `RoomModule` (étendu) et des marqueurs :

- **Type :** salle, carrefour, couloir (droit, en pente, galerie de parcours, avec passage en hauteur), parcours, départ,
  sortie (avec escalier et arène du boss), pièce annexe.
- **Empreinte :** en cases de la grille (aujourd'hui une salle par case de 120 m ; on pourrait permettre 1 × 2 ou 2 × 2) et
  **niveau** (sol ou +16 m).
- **Entrées :** des marqueurs `ModuleDoor` posés sur les bords (côté, position, largeur 14 m, niveau). Le générateur ne relie que
  des entrées compatibles ; une entrée inutilisée est fermée par un mur fourni avec le module.
- **Contraintes :**
  - nombre minimal et maximal par carte ;
  - poids de tirage ;
  - distance au départ (en salles), minimale et maximale ;
  - voisins obligatoires ou interdits, par étiquette (« une salle de parcours relie deux salles *X* », « un pont traverse un
    couloir ») ;
  - niveau permis.
- **Contenu de jeu déjà porté par le module :** volumes de salle, points d'apparition, places des habitants et des coffres, murs
  grimpables, surfaces non magnétiques.

## 2. Comment l'assemblage garantit les règles

1. **Graphe d'abord, géométrie ensuite.** Avec la seed (`System.Random`, ordre de tirage fixe), on tire un graphe sur la grille :
   - chaque couloir fait partie d'une **boucle**, pour que le traqueur puisse toujours tourner (graphe sans pont, comme le
     labyrinthe actuel) ;
   - **départ et sortie** sont pris à au moins *N* salles l'un de l'autre (par exemple les deux bouts du plus long chemin) ;
   - l'arène est accrochée derrière la sortie.
2. **Placement des modules sous contraintes :**
   - d'abord ceux qui en ont (parcours entre deux salles précises, pont au-dessus d'un couloir, sortie), puis les autres au
     poids ;
   - retour en arrière limité ;
   - en cas d'échec, nouvel essai avec une sous-seed (au plus 20 essais, puis une carte de secours connue).
3. **Garanties par construction plutôt que par vérification :** chaque module est **validé une fois dans l'éditeur** :
   - aucune cachette (tout point à portée de la main du traqueur) ;
   - contournement du traqueur seulement dans les zones de parcours ;
   - voies de 11 m dégagées ;
   - NavMesh qui sort par chaque entrée ;
   - volumes de salle présents.

   Les couloirs sont des modèles éprouvés de longueur connue. À l'exécution, on ne vérifie que les **raccords** (entrées alignées,
   NavMesh continu entre deux modules) et la distance départ–sortie.
4. **Ce qui est déduit de la carte, comme aujourd'hui :**
   - `MazeGraph`, d'après le graphe tiré ;
   - minimap ;
   - tronçons de couloir de 40 m ;
   - surface de la carte, qui règle les vagues ;
   - NavMesh construit au lancement.

## 3. Créer un nouveau module sans code

1. Dupliquer `Module_Modele` (une salle vide avec ses 4 entrées).
2. Déplacer ou supprimer des entrées ; elles s'aimantent à la grille.
3. Meubler avec les props du catalogue.
4. Poser les marqueurs : apparitions, habitants, coffres.
5. Régler les contraintes dans l'inspecteur.
6. Lancer *ItHunts > Valider le module* : rapport des cachettes, des voies, des entrées, des volumes et des triangles. Le module
   n'entre dans le tirage que s'il est valide (une case à cocher).

Un catalogue (`ModuleCatalog`, un ScriptableObject) liste les modules et leurs poids par zone (cave, château…).

## 4. Ce que deviennent Level_01 et les modules existants

- Les **15 modules** actuels (13 salles, 2 carrefours) deviennent les premières entrées du catalogue, avec des marqueurs d'entrée
  aux ouvertures actuelles.
- Les couloirs du constructeur deviennent des modules de couloir :
  - droit ;
  - en pente (+16 m) ;
  - galerie de parcours ;
  - avec passage en hauteur.
- **Level_01** (copie faite à la main) reste une carte **fixe**, utile au level design et aux tests.
- Le constructeur de `Level_01_Base` devient le générateur. Avec une seed fixe, il redonne une carte 7 × 7 du même genre.

## 5. Risques

- **Temps de chargement :**
  - instancier environ 50 modules et 4 000 props, puis les regrouper (batching statique à l'exécution) : estimé 1 à 3 s ;
  - plus le NavMesh : 1.1 s mesurées aujourd'hui pour les goules et le traqueur, murs grimpables compris.

  Il faudra un écran de chargement. La construction du NavMesh peut passer en asynchrone.
- **Performance :**
  - pas d'éclairage ni d'occlusion précalculés pour une carte aléatoire, donc tout en lumières temps réel (c'est déjà le cas) ;
  - le culling des lumières reste le poste lourd en écran partagé (voir la liste d'optimisations) ;
  - une occlusion par salles voisines (portails) remplacerait le précalcul.
- **Validation trop lente à l'exécution :** le contrôle des cachettes (environ 48 000 points) prend trop de temps pendant la
  partie, d'où la validation des modules dans l'éditeur.
- **Réseau (étape 11) :** chaque machine génère la même carte à partir de la seed (rien à transmettre), à condition que le tirage
  soit entièrement déterministe (pas de `UnityEngine.Random`, ordre fixe).
- **Raccords :** deux modules au niveau différent demandent un couloir en pente. Les modules doivent donc déclarer leur
  niveau à chaque entrée.

## 6. Questions de design (à trancher)

1. **Grille :**
   - A : garder la grille de 120 m (une salle par case, 7 × 7) ;
   - B : permettre des modules de 1 × 2 ou 2 × 2 cases ;
   - C : grille plus fine (60 m) et salles de tailles variées.
2. **Taille d'un niveau :**
   - A : toujours 49 cases ;
   - B : 30 à 49 cases selon le niveau et la difficulté ;
   - C : nombre de salles tiré dans une fourchette.
3. **Level_01 :**
   - A : il reste le niveau 1 fixe de chaque run, les niveaux suivants sont générés ;
   - B : tous les niveaux sont générés, Level_01 sert de test ;
   - C : un tirage entre cartes faites main et cartes générées.
4. **Deux niveaux de hauteur (sol et +16 m) :**
   - A : les garder partout ;
   - B : seulement dans certains modules (tours, galeries) ;
   - C : un seul niveau pour commencer.
5. **Distance départ–sortie :**
   - A : au moins 6 salles par le chemin le plus court ;
   - B : les deux bouts du plus long chemin ;
   - C : réglable par niveau.
