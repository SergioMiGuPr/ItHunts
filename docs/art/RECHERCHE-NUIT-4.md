# Recherche courte de la nuit 4 (2026-11-23)

Onze recherches web, faites avant les lots. Les sources sont surtout des forums et des articles de praticiens, car les
studios publient peu de chiffres. **[usage]** signale une pratique courante pour laquelle je n'ai trouvé aucune source.

## 1. Densité de texels et échelle des textures

- Conversion : 1024 px/m = 10,24 px/cm, et 512 px/m = 5,12 px/cm.
- CRYENGINE vise environ 512 texels/m pour la plupart des assets. Les guides FPS citent 1024 à 2048 px/m, parce que la
  caméra colle aux murs. Le consensus : on choisit une densité et on la tient partout. Un écart de densité entre deux pièces
  voisines se remarque plus qu'une densité un peu basse.
- **Choix pour ItHunts** (noté dans ART-HANDOFF § Densité de texels) :
  - **512 px/m de référence** pour les murs, les sols et les props, soit une tuile de 1024 px pour 2 m ;
  - **1024 px/m** pour les objets tenus ou vus de très près ;
  - **256 px/m minimum** pour les fonds hors de portée (falaises lointaines, plafonds de plus de 8 m).
  - Le cel-shading supporte cette densité si les traits peints font au moins 4 px.
- **Tailles réelles à respecter dans toutes les tuiles** [usage] :

  | Élément | Dimensions |
  |---|---|
  | Brique | 22 × 6,5 cm (joint de 1 cm) |
  | Pierre de taille | assises de 30 à 50 cm, blocs de 50 à 100 cm |
  | Moellons | 20 à 40 cm |
  | Pavés | 10 à 15 cm |
  | Dalles de château | 40 à 60 cm |
  | Lames de parquet | 12 à 20 cm de large |
  | Marche | 15 à 20 cm |

- **Casser la répétition** :
  - macro-variation en coordonnées monde (taches de 8 à 20 m, 5 à 15 % de teinte et de valeur) ;
  - mélange de couches par vertex color ou par hauteur (mousse, salpêtre, saleté) ;
  - décalcomanies d'usure sur les angles ;
  - props d'habillage posés sur les jointures des modules.

## 2. Intérieurs de châteaux gothiques

- **Grandes salles** : 30 × 12 à 14 m (Chepstow, Durham), 10 à 15 m sous la charpente. On y trouve une galerie ou une
  mezzanine à 4 ou 5 m de haut, large de 2,5 à 3 m.
- **Lancettes** : 6 à 10 fois plus hautes que larges, arc brisé, souvent groupées par 2 ou 3 (par exemple 0,6 à 0,9 m × 4 à
  7 m). Les rosaces vont dans les pignons.
- **Matériaux** :
  - calcaire clair en pierre de taille ;
  - lambris jusqu'à 2 ou 3 m ;
  - parquet ou dalles ;
  - tapisseries de 3 à 6 m de haut ;
  - plâtre peint ;
  - marbre dans les salles d'apparat.
- **Escalier en colimaçon** :
  - château : 1,5 à 2,5 m de diamètre, marches de 18 à 25 cm [usage] ;
  - **pour notre jeu de mouvement** : 3 à 4 m de diamètre, avec une rampe de collision sous les marches.
- **Lustres** :
  - diamètre (cm) ≈ 8,3 × (longueur + largeur de la salle en m) ;
  - on en met plusieurs en enfilade dans une grande salle, de 2 à 4 m de diamètre ;
  - le bas du lustre est à 3,5 m du sol au minimum, pour laisser passer les sauts.

## 3. Lumière des fenêtres et noir lisible

- **Méthode de Dishonored** : régler les valeurs non éclairées d'abord, puis la lumière. Le post-process ne doit pas
  rattraper un problème de valeurs.
- **Froid et chaud** :
  - la lune est bleutée (6500 à 9000 K), les bougies et torches chaudes (1800 à 2500 K) ;
  - le bleu « fait nuit » sans assombrir l'image.
- **Rayons de lune** : sans volumétrique sous URP, on utilise des cartes de rayons additives qui s'effacent selon l'angle
  de vue, plus la brume de hauteur.
- **Lisibilité** :
  - au moins 70 % de l'image dans les valeurs sombres ;
  - le noir pur est réservé aux silhouettes ;
  - le sol du chemin ne descend jamais sous 8 à 10 % de luminance ;
  - les interactifs ressortent par la teinte et l'émission, et le décor reste désaturé.

## 4. Marquages au sol des attaques de boss

- **Durées des annonces** :
  - l'alerte esquivable la plus courte dure environ 20 images (333 ms) ;
  - attaques normales : 0,8 à 1,2 s ;
  - grosses zones : 1,5 à 2,5 s.
- **Lisibilité** :
  - le marquage épouse le sol et ne se cache jamais dans le relief ;
  - on distingue les attaques par la **forme** (cercle, cône, ligne, anneau) plutôt que par la couleur ;
  - les dégâts tombent quand le marquage se termine (FFXIV).
- **Pour ItHunts** : le bleu, l'orange, le violet et le vert sont déjà pris par l'orbe. La recette est :
  - un **bord net** (4 à 8 cm, opaque) ;
  - un **remplissage sombre et translucide** (20 à 35 %, plus sombre que le sol, qui laisse voir le décor) ;
  - un **front qui se remplit** pendant l'annonce ;
  - une **montée d'intensité** sur les derniers 40 % ;
  - un éclair bref au moment de l'impact.

  Une seule teinte sert à toutes les attaques du boss (rouge sang éteint), et la forme dit le type d'attaque.

## Sources

- Densité de texels :
  - [forum Unreal](https://forums.unrealengine.com/t/texel-density-standards-for-aaa-first-person-shooter-games/1646118) ;
  - [CRYENGINE](https://cryengine.com/docs/static/engines/cryengine-5/categories/23756816/pages/52199429) ;
  - [Polycount](https://polycount.com/discussion/136614/variety-in-modular-textures) ;
  - [80.lv, Concrete Evergreen](https://80.lv/articles/concrete-evergreen-making-a-modular-scene-less-repetitive).
- Châteaux et escaliers :
  - [la grande salle](https://www.worldhistory.org/Great_Hall/) ;
  - [architecture gothique](https://www.designingbuildings.co.uk/wiki/Gothic_architecture) ;
  - [escaliers en colimaçon](https://www.salterspiralstair.com/resources/sizing-dimensions/).
- Lustres : [taille d'un lustre](https://lightingnewyork.com/guides/guide-to-proper-chandelier-sizing-pro.html).
- Lumière :
  - [Dishonored, 80.lv](https://80.lv/articles/dishonored-interiors-lighting-props) ;
  - [Glasshouse, Unity](https://unity.com/blog/glasshouse-flat28-building-atmosphere-hdrp).
- Marquages d'attaque :
  - [FFXIV, zones d'effet](https://ffxiv.consolegameswiki.com/wiki/AoE) ;
  - [annonces d'attaque](https://bugnet.io/blog/how-to-design-enemy-attack-telegraphs) ;
  - [20 images d'alerte](https://itch.io/post/8070975).
