# Vagues

**Status:** implemented · **Updated:** 2026-11-15 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md) · D-041, D-060, D-061

## Résumé

Le nombre d'ennemis présents dans une zone suit le temps passé dans la zone : peu au début, de plus en plus jusqu'au timer,
plus encore en Enragé. Par-dessus cette tendance, des **pics de vague** annoncés alternent avec des **accalmies**. Les
ennemis apparaissent hors de la vue des joueurs, sur des points d'apparition posés partout dans la zone.

## Revu après le playtest des étapes 8 et 9 (2026-11-15) : D-060 et D-061

Ces règles **remplacent** les règles 2 (accalmie : plafond de chasse et recul), 3 (rythme), 5 (population de départ), 6
(reconstitution) et 7 (plafonds de chasse) ci-dessous. Le reste (points d'apparition, distances, salle et tronçon du joueur, coop)
ne change pas.

1. **Apparitions seulement au début d'une vague (D-060).** Au début de chaque pic, une **fournée** apparaît : `ghoulWaveSpawns`
   (3, 4, 6, 8, 10 aux moments de la tendance, 14 en Enragé) et `flyerWaveSpawns` (1, 1, 2, 2, 3 ; 4 en Enragé), × joueurs ×
   surface de la carte, sans dépasser le **plafond de population** (l'ancienne tendance : 10 → 60 goules, × joueurs × surface).
   Les apparitions s'étalent sur au plus **8 s** (une toutes les 0.2 s, chacune annoncée 0.6 s) ; ce qui n'a pas trouvé de point
   est abandonné. **Plus aucune apparition entre deux vagues**, ni continue, ni pour remplacer les tués. Les accalmies ne règlent
   plus que le rythme des fournées.
2. **Lieux interdits en plus (D-060)** : jamais dans une **salle ou un tronçon où un joueur est passé depuis 60 s**
   (`recentVisitSeconds`) ; jamais dans la **salle de départ**, pendant toute la zone (un ennemi qui poursuit peut y entrer).
3. **Aggro par proximité, sans limite de nombre (D-061)** : un ennemi attend tant qu'aucun joueur debout ne passe dans sa zone
   d'aggro (**20 m en vue, 8 m sans**) ; pris en aggro, il poursuit jusqu'à être **semé** : plus aucun joueur à moins de
   **50 m pendant 5 s** (réglables, réglages de l'ennemi). Plus de plafond de chasse (25 goules, 8 volants).
4. **Chasseurs (D-061)** : en plus, chaque joueur debout a un petit groupe d'ennemis qui **savent toujours où il est** et le
   poursuivent. Nombre par joueur, par paliers en part du timer : **1** dès le début, **2** à 15 %, **4** à 35 %, **6** à 55 %,
   **8** à 75 %, **10** en Enragé (`chaserTimes`, `chaserCounts`, `chaserEnraged`). Revu toutes les 0.5 s : quand un chasseur
   meurt, **l'ennemi en attente le plus proche du joueur par le chemin** prend le relais (à défaut, un ennemi déjà en aggro). Un
   chasseur dont le joueur tombe redevient un ennemi en aggro ordinaire. Aucun chasseur pendant le combat de boss.
5. **Habitants des salles** : chaque grande salle garde **1 à 3 goules** selon sa taille (46 → 66 m) et au plus **1 volant**
   (`residentGhoulsMin/Max`, `residentFlyersMax`) ; les autres posées par le constructeur sont retirées au début de la zone.

## Règles (D-041)

1. **Tendance :** nombre voulu de goules et de volants à des moments donnés **en part du timer** (marche avec 4 min en test
   et 20 min en vrai), interpolé entre ces moments ; en **Enragé** (après le timer), un niveau plus haut.
2. **Pics et accalmies :** la zone commence par une **accalmie de 15 s**, puis un **pic de 20 s**, et ainsi de suite (réglables).
   Pendant un pic, le nombre voulu est la tendance ; pendant une accalmie, **60 %** de la tendance (réglable). Chaque pic est
   annoncé par **un son (cor grave)** et une indication brève dans le HUD (« VAGUE n », 1.5 s, en haut au centre). Pendant une
   accalmie, aucun ennemi n'est retiré : il n'en arrive plus tant que le nombre présent dépasse le voulu, et **le plafond de
   chasse (sans l'aggro) descend à 60 %** (réglable : `lullHuntingShare`) : les ennemis en trop (les plus loin d'un joueur)
   **repassent en attente**, **reculent d'environ 10 m** (`lullRetreatDistance`, réglage de l'ennemi) et ne poursuivent plus,
   sauf si le joueur **repasse dans leur zone d'aggro** (il doit en être sorti d'abord) ; au pic suivant, ils reprennent la
   chasse. Personne ne disparaît.
3. **Rythme des apparitions :** une apparition de chaque type toutes les **3 s** au début, **1 s** à la fin du timer
   (interpolé), **0.5 s** en Enragé, tant que le nombre présent est sous le voulu.
4. **Où :** sur des **points d'apparition** (`SpawnPoint`) posés par le constructeur de scène dans les salles, les couloirs et
   en hauteur pour les volants ; au level design, on en pose à la main (trous, grilles, nids) **sans code** : un objet vide avec
   le composant, type goule ou volant. À chaque apparition, un point du bon type est choisi **hors de la vue de tous les
   joueurs** (hors du champ de vision, ou caché par le décor), **à 25 m au moins de chaque joueur et à 80 m au plus du plus
   proche**, et pas utilisé dans les 2 dernières secondes. **Jamais dans une salle où se trouve un joueur (D-045)** : chaque
   salle a un **volume de salle** (`RoomVolume`, posé par le constructeur, et à la main au level design) ; vérifié au choix du
   point et à la fin de l'annonce (un joueur entré pendant l'annonce l'annule). **Couloirs (D-047)** : découpés en tronçons de
   40 m au plus, chacun avec son volume ; jamais dans le tronçon d'un joueur **ni dans les tronçons voisins**.
5. **Annonce :** une lueur orange au sol au point choisi pendant **0.6 s**, puis l'ennemi apparaît (en attente ou en chasse
   selon les plafonds). Au début de la zone, la population de départ est posée d'un coup, hors de vue, sans annonce.
6. **Ennemis tués :** ils ne reviennent pas par les apparitions normales ; le nombre voulu **se reconstitue lentement**, au
   plus **un ennemi toutes les 4 s** (réglable) en plus des apparitions normales, jamais d'un coup. Un ennemi mort rentre dans
   la réserve après 2 s.
7. **Plafonds de chasse** (goules : 25 et 40 avec l'aggro ; volants : 8 et 12) : ils suivent la tendance (réglage × tendance
   du moment / tendance à la fin du timer) ; en Enragé, au-dessus (goules 33 / 53, volants 10 / 15).
8. **Coop :** nombre voulu et plafonds de chasse **× (1 + 0.5 par joueur vivant en plus)** (×1.5 à 2, ×2 à 3, ×2.5 à 4 ;
   réglable : `extraPlayerShare`) ; **chaque joueur a sa part** du nombre voulu et les apparitions se font **à tour de rôle
   autour de chaque joueur** (à qui il en manque) : un point à 80 m au plus de ce joueur, **à 25 m au moins de chacun** et caché
   de tous. Un joueur mort ne compte plus. Choix de cible des goules et des volants : voir leurs fiches. La réserve (80 / 20)
   limite le nombre en coop à 4 (à agrandir à l'étape 9 ou 11).
9. **Âmes (étape 7) :** D-009 s'applique, multiplicateur ×2 en Enragé ; rien à coder avant l'étape 7.
10. **Réserve :** les ennemis sont créés une fois par le constructeur (80 goules, 20 volants, inactifs) et réutilisés ; plus
   aucun ennemi posé à la main dans `Test_Arena`. Le point de départ plus sûr de la zone viendra à l'étape 8.

## Valeurs

`Waves_Default` (`WaveSettings`), validées le 2026-11-13.

| Nom | Valeur | Unité | À observer en playtest |
|---|---|---|---|
| Moments de la tendance | 0, 25, 50, 75, 100 | % du timer | |
| Goules voulues (pic) | 10, 20, 35, 50, 60 ; Enragé 80 | | pression qui monte |
| Volants voulus (pic) | 2, 4, 8, 12, 16 ; Enragé 20 | | |
| Pic, accalmie, part en accalmie | 20, 15, 60 % | s, s | respiration lisible |
| Intervalle entre deux apparitions | 3 → 1 ; Enragé 0.5 | s | |
| Distance au joueur | 25 à 80 | m | jamais « dans le dos » |
| Annonce d'une apparition | 0.6 | s | |
| Reconstitution des tués | 1 toutes les 4 s | | tuer achète du répit |
| Accalmie : plafond de chasse, recul | 60 %, 10 m | | on respire |
| Coop : multiplicateur par joueur en plus | +0.5 | | |
| Retour dans la réserve après la mort | 2 | s | |
| Indication du pic dans le HUD | 1.5 | s | |
| Réserve | 80 goules, 20 volants | | |

## Interactions

| Fonctionnalité | Lien |
|---|---|
| [Goule](../goule/design.md) | apparaît sur les points au sol ; plafonds de chasse selon la tendance |
| [Volant](../volant/design.md) | apparaît sur les points en hauteur ; plafonds de chasse selon la tendance |
| [Boss traqueur](../boss-traqueur/design.md) | indépendant ; l'Enragé monte le niveau des vagues |
| Âmes (étape 7) | ×2 en Enragé (D-009) |

## Questions ouvertes

- Points d'apparition à la main au level design (trous, grilles, nids) : un retour visuel propre (grille qui s'ouvre) viendra
  avec la DA.
- Point de départ plus sûr de la zone : étape 8.
- Population de départ : au début de la zone, peu de points sont cachés et à 25–80 m du départ (0 à 2 ennemis posés d'un coup
  dans les mesures, 6 voulus) ; la zone se remplit ensuite au rythme des apparitions. À revoir avec le level design.
- Pendant une accalmie, rien n'est retiré : si le joueur ne tue rien, les présents restent au-dessus du voulu (mais 40 % de moins
  en chasse).
- Coop à 4 : le nombre voulu (×2.5) dépasse vite la réserve de 80 goules et 20 volants.

## History

- 2026-11-13 : designed et construit (bloc 7, D-041).
- 2026-11-13 : playtest validé ; accalmie (plafond de chasse à 60 %, recul), coop (multiplicateur, parts, tour de rôle), choix de
  cible.
- 2026-11-13 : jamais d'apparition dans la salle d'un joueur, volumes de salle (D-045).
- 2026-11-13 : tronçons de couloir de 40 m, ni le tronçon du joueur ni les voisins (D-047).
