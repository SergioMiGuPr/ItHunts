# Rapport de la nuit 4 (2026-11-23)

Brief : « ambition maximale », vérification en jeu obligatoire. Recherche préalable : [RECHERCHE-NUIT-4.md](../RECHERCHE-NUIT-4.md)
(34 règles, une cinquantaine de sources). Décisions D-100 à D-109 dans le [GDD](../GDD.md). Rapports du bot : [bot/](bot/) ;
preuves de la parade : [bot/parade/](bot/parade/) ; preuves des portes : [bot/porte/](bot/porte/) ; mesures du build : [perf/](perf/) ;
planches des captures à points fixes : [captures/planches-da/](captures/planches-da/) (après les PR de la DA) et
[captures/planches/](captures/planches/) (avant).

## En bref

- **La nuit a débordé sur la journée** : arrêt vers 1 h (limite d'usage atteinte par un sous-agent), reprise à 12 h 18, fin vers 19 h.
- **Toutes les PR de la DA sont fusionnées et branchées** (#45 à #53), dont les props en 3D sur les greybox et les portes animées.
- **Le build tient les 60 images par seconde avec de la marge** : 92 à 105 en médiane à 1920 × 1080 (l'acte II était à 36 avant
  une correction de cette journée : 3 400 systèmes de particules de flammes simulés en permanence).
- **Trois bugs du playtest sont reproduits, expliqués et corrigés avec preuve en jeu** : porte de sortie (9 cartes sur 9), parade
  (la vraie cause est le bond), goules figées.
- **Le bot finit 6 cartes sur 9** (dont 2 par un bug de la fin d'acte, corrigé depuis) et lance le combat de boss sur les 3 graines, avec **0 erreur du journal partout**. Il ne finit pas
  3 cartes ni le boss : ce sont les limites de sa navigation (paliers, escalades) et de son combat, détaillées plus bas.
- **Pas tout vérifié manette en main** : la fuite, le boss, le screamer et le ressenti des nouvelles salles ne sont vérifiés que
  par le code, les tests et les mesures. La checklist en fin de rapport liste ce qu'il faut sentir.

## PR d'art

| PR | Branche | Contenu | État | Branchement côté jeu |
|---|---|---|---|---|
| #45 | art-textures-1 | 5 textures peintes sans couture | fusionnée (5dddcfc) | rien à câbler (GUID inchangés) |
| #46 | art-menus-4 | campement : personnages assis peints | fusionnée (464ef52) | prefab du campement à jour ; test vert |
| #47 | art-materiaux-2 | 512 px/m, matériaux du château, 0 face superposée | fusionnée (3571a07) | acte II câblé par fonction, bug des sols corrigé |
| #48 | art-ameliorations | 70 illustrations, 10 cadres de rang | fusionnée (424419f) | 70 icônes, cadres et losange du passif dans le HUD |
| #49 | art-ui-4 | écran de fin en grille, marchand à rangs, porte scellée, carte unifiée | fusionnée (d87515d) | écran de fin, panneau de la porte scellée |
| #50 | art-vfx-sons-4 | marquages du boss, contour des volants, apparitions, screamer, cris | fusionnée (86e2608) | `MarquageBoss`, `ContourDiscret`, `ScreamerMort`, cris d'apparition et « je t'ai vu » |
| #51 | art-props-3d | 17 props 3D, 3 variantes, coffres lumineux, LOD | fusionnée (5769fec) | `PropArt` : 219 / 152 / 45 props remplacés par acte, tous les coffres (couvercle), attrapables (lueur) |
| #52 | art-kits-3 | kits verticaux, portes animées, torches, ciel | fusionnée (7fffdbb) | portes de sortie animées par `OuverturePorte`, kits réappliqués |
| #53 | art-fin-4 | NUIT-FIN-4.md | fusionnée (951b1ac) | — |

Pour chaque PR : périmètre vérifié (seulement `Assets/Art/`, `docs/art/` et le manifeste), compilation sans erreur, tests EditMode
verts (557 réussis, 1 non concluant connu), fusion normale (conflits du manifeste résolus par union, aucun doublon nouveau), push de
`main`, fusion dans `animations`. Crédits de la DA : 265,5 sur 360 (détail dans `docs/art/NUIT-FIN-4.md`).

## Contrôle point par point

| # | Demande | État | Preuve |
|---|---|---|---|
| 0 | AMELIORATIONS.md : 70 améliorations, rangs, synergies, poussé sur main | ✅ | commit 1363294 (main, en début de nuit) |
| R | Recherche en ligne, résumée et appliquée | ✅ | `RECHERCHE-NUIT-4.md` ; zéro impasse, boucles, paliers, quota + repos (L4D), scaling plus lent que le joueur, marquages figés 300 ms, screamer 3 s avec option réduite |
| 1 | Porte qui n'envoie pas à l'acte suivant | ✅ | cause : aucun `OnTriggerEnter` quand le joueur traverse la boîte de fin d'acte ; test de boîte à chaque pas fixe ; `ExitHallTests` **9/9** (le joueur marche par l'entrée « avancer »), refait après le branchement de la porte animée ; captures `bot/porte/` |
| 1 | Parade : 0 réussie sur une run | ✅ | `ParryInGameTests` 3/3 (vraie touche) ; **cause** : le bond était toujours « trop tôt » ; appui pendant le vol accepté (D-108) ; `bot/parade/PARADE.md` ; bot : 195 à 863 parades réussies par carte |
| 1 | Goules figées après une répulsion | ✅ | recul relâché après 4 s (`EnemyBody.KnockTimeout`) ; commit 267dc13 |
| 1 | Améliorations visibles dans le monde | ✅ | `OfferOrb.WorldVisible` faux ; test `Coffre_SansTexteFlottant_InviteParJoueur` |
| 1 | Écran de fin : débordement ; Recommencer et Campement | ✅ | grille de la DA (PR #49) ; test du campement (personnages différents pour J2) ; **non joué par le bot** (il ne finit pas le boss) |
| 1 | Puits vide, sauts sur la margelle | ✅ code | eau à −1.2 m, collision en cylindre lisse ; à sentir manette en main |
| 1 | Faces superposées | ✅ | côté jeu : murs prolongés seulement aux coins, linteau et seuil exacts ; côté art : 0 face superposée (PR #47) |
| 1 | Acte II : défauts d'affichage | ✅ | tuiles à 512 px/m (PR #47), matériaux du château par fonction ; planches `captures/planches-da/Acte_2_*` |
| 1 | Boss : apparition du joueur et du boss | ✅ | boss perché, entrée par un bond (D-105) ; bot : combat lancé à 0:05 sur les 3 graines |
| 1 | Sphère rouge, ouvertures noires, formes noires (acte III) | ✅ | sphère rouge = annonce greybox du volant (retirée) ; cimes en sphères retirées, arbres morts du kit ; lueur chaude à chaque porte et hall éclairé ; planches `Acte_3_*` |
| 2 | 70 améliorations en ScriptableObjects, rangs, cadres | ✅ | 70 assets, 70 icônes et 10 cadres de la DA ; tests du marchand et des prix par rang |
| 2 | Limite 10, un passif | ✅ | `UpgradeSlots` ; tests |
| 2 | Marchand nouvelle + montée, coût croissant | ✅ | prix 60 / 90 / 140 / 220 / 350 ; test du marchand |
| 2 | Économie réglée sur les âmes gagnées | ⚠️ | bot à 10:00 : 53 à 334 âmes ; la sortie (150 / 200 / 250) n'était payable que sur 2 cartes sur 7 mesurées, l'admin a complété les autres ; **à trancher** (voir « Ce qui reste ») |
| 2 | HUD : améliorations au-dessus de la barre de vie | ✅ code | rangée de 10 cases, cadres de la DA, passif en losange ; à regarder en jeu |
| 3 | Vagues cachées, pression constante | ✅ | plus de « VAGUE n » ; quota par acte, repos après blessure (D-103) |
| 3 | Scaling sur la puissance du joueur, plus lent | ✅ | `EnemyScaling` (exposant 0.9), par acte |
| 3 | Aucune salle vide ; performances | ✅ | 81 à 112 ennemis actifs par carte ; **build : 9,5 / 10,9 / 9,6 ms** en médiane (voir « Mesures ») |
| 4 | Cartes plus grandes, boucles, plusieurs routes | ✅ | 24 à 26 salles (16 avant), 5 à 13 boucles, 0 ou 1 impasse sur 15 cartes (test `ZeroImpasse…`) |
| 4 | Verticalité : étages, portes en hauteur | ✅ | sols de −8 à +15 m, 3 à 11 paliers par carte |
| 4 | Plus de cellule d'apparition du traqueur | ✅ | porte pleine qu'il enfonce, vestibule sans lumière |
| 4 | Acte II : château | ✅ | fenêtres de lune, lustres, matériaux et props 3D du château ; planches |
| 4 | Acte III : falaises, terrasses | ⚠️ | terrasses et paliers, lisière de falaise du kit ; pas de passages creusés dans la roche |
| 4 | Durées réelles des actes | ⚠️ mesure | bot : 8:22 à 19:56 de jeu sur les 6 cartes finies, mais avec âmes complétées et téléportations ; un joueur humain sera plus rapide ou plus lent |
| 5 | Portes verrouillée et de sortie différentes, animées | ✅ | grille de fer qui se lève ; battants de la DA (`OuverturePorte`) ; porte enfoncée par le traqueur |
| 5 | Sortie avant la fin du temps : temps restant et explication | ✅ code | panneau de la DA : « Sortie scellée · m:ss » |
| 6 | Minimap | ✅ | `ActMapPainter` (9 tests), `MinimapVision` |
| 7 | Cri du traqueur à l'apparition et au repérage | ✅ | cris de la DA branchés (PR #50) |
| 7 | Mort par prise : caméra, gros plan, screamer, option réduite | ✅ code | `StalkerDeathCam` (3 s) + `ScreamerMort` ; à voir manette en main |
| 7 | Vitesse de la traction (mesurer, ne rien changer) | ✅ mesure | voir « Mesures » ; rien changé |
| 8 | Boss : patterns, marquages sobres, tracé exact, arène | ✅ | `MarquageBoss` de la DA ; bot : boss amené en phase 3 (365 et 643 PV sur 2400), 441 à 491 attaques, 0 erreur |
| 9 | Nettoyage DA | ✅ | voir « Inventaire » |
| V | Bot : chaque acte, 3 graines, puis boss et écran de fin | ⚠️ | 6 cartes finies sur 9, boss lancé sur 3 graines mais pas vaincu en 9 min, écran de fin non atteint par le bot |
| V | Captures à points fixes avant/après | ✅ | 9 cartes × 50 vues : `captures/planches/` (avant les PR #51-52) et `captures/planches-da/` (après) |
| V | Journaux à 0 erreur (éditeur et build) | ✅ | bot : 0 erreur sur 12 passages ; build : 0 exception sur les 3 actes |
| V | FPS du build (60) | ✅ | 92 à 105 en médiane ; 95e centile 11 à 15 ms |
| V | Relecture indépendante | ✅ | sous-agent qui n'avait rien vu ; 4 bugs corrigés après coup (voir « Relecture ») |

## Résultats du bot (3 graines par acte, temps réel, vitesse ×1)

Le bot joue avec les vraies entrées (`PlayerInputReader.Override`), invulnérable mais en comptant les coups reçus. Un « blocage » est
noté quand il ne progresse plus vers son but pendant 12 s : il est alors téléporté au point suivant. Ce sont surtout des paliers et
des passages d'escalade que sa navigation ne sait pas prendre (elle suit le NavMesh au sol), pas des murs invisibles constatés.

| Carte | Fin de l'acte | Âmes à 10:00 (prix de sortie) | Parades réussies | Blocages | Vitesse max | Erreurs |
|---|---|---|---|---|---|---|
| Acte I, graine 11 | non (bloqué vers la sortie) | 334 (150) | 773 | 2 | 36,4 m/s | 0 |
| Acte I, graine 22 | non (bloqué vers la sortie) | 53 (150), complété | 299 | 2 | 24,2 m/s | 0 |
| Acte I, graine 33 | 8:22, après une téléportation dans le hall (porte fermée : bug corrigé depuis) | fini avant 10:00 | 195 | 4 | 22,5 m/s | 0 |
| Acte II, graine 11 | **12:44** | 100 (200), complété | 438 | 11 | 22,0 m/s | 0 |
| Acte II, graine 22 | **8:34** | fini avant 10:00, porte payée | 706 | 3 | 22,0 m/s | 0 |
| Acte II, graine 33 | 11:21, après une téléportation dans le hall (même bug) | 162 (200), complété | 281 | 12 | 22,5 m/s | 0 |
| Acte III, graine 11 | **12:37** | 238 (250), complété | 604 | 8 | 22,0 m/s | 0 |
| Acte III, graine 22 | non (bloqué vers la sortie) | 144 (250), complété | 863 | 5 | 22,0 m/s | 0 |
| Acte III, graine 33 | **19:56** | 158 (250), complété | 751 | 5 | 23,1 m/s | 0 |
| Boss, graine 11 | combat lancé à 0:05, non fini en 9 min | — | 834 | 0 | 40,1 m/s | 0 |
| Boss, graine 22 | 365 / 2400 PV, phase 3 | — | 1127 | 0 | 35,4 m/s | 0 |
| Boss, graine 33 | 643 / 2400 PV, phase 3 | — | 794 | 0 | 36,3 m/s | 0 |

Correction faite pendant cette passe : le bot ne faisait rien dans l'arène du boss (il attendait un niveau d'acte) ; il descend
maintenant au centre, ce qui lance le combat.

## Mesures

- **Images par seconde du build** (build de développement, 1920 × 1080, joueur au centre de la carte, 20 s de mesure, graine 11,
  [perf/](perf/)) :

  | Acte | Avant (5 min de jeu) | Après correction, build final (5 min) | Pire cas (timer avancé à 24 min), après |
  |---|---|---|---|
  | I (caves) | 12,3 ms (81 i/s) | **9,5 ms (105 i/s)**, 95 % : 11,1 ms | 10,5 ms |
  | II (château) | 28,2 ms (35 i/s) | **10,9 ms (92 i/s)**, 95 % : 13,1 ms | 12,9 ms, 95 % : 23,0 ms |
  | III (jardin) | 19,6 ms (51 i/s) | **9,6 ms (104 i/s)**, 95 % : 15,1 ms | 12,6 ms |

  Cause trouvée : les flammes de la DA (appliques à 3 flammes, lustres à 12 flammes) faisaient 1 700 effets, soit 3 400 systèmes
  de particules simulés en permanence, même hors de l'écran. Ils sont maintenant coupés au-delà de 30 m du joueur (la lumière reste ;
  réglable dans `HorrorLook_*` : `vfxCulling`, `vfxCullDistance`). Vérifié en jeu : 3 flammes sur 3 allumées à moins de 22 m,
  0 sur 1 771 au-delà de 40 m. GPU : 4,6 à 7,1 ms ; c'est le processeur qui limite.
- **Vitesse** (bot, vitesse horizontale maximale du joueur, toutes sources : traction, bunny hop, chute) : 22 à 36 m/s dans les
  actes, 40 m/s dans l'arène du boss. Ce n'est pas la vitesse de la traction seule, que le bot ne mesure pas à part. Traqueur : chasse 9,5 m/s,
  pointe 16,5 (3 s toutes les 6 s), enragé 11,5, pointe enragée 18,5. Le joueur qui enchaîne traction et bunny hop distance donc le
  traqueur. **Proposition, non appliquée** : rien changer avant votre playtest ; si la fuite est trop facile, plafonner la traction
  à 18 m/s en sortie (D-036), pas le bunny hop.
- **Durée des actes** (bot) : 8:22 à 19:56 de jeu sur les 6 cartes finies (dont 2 par le bug de la boîte du hall, corrigé). Le bot
  est un joueur médiocre (invulnérable, il ramasse mal les âmes) : ces durées sont un ordre de grandeur, pas un réglage.
- **Fermeture du build** : le build plante parfois en se fermant (D3D12, code 139), **après** tout le nettoyage du jeu (le journal
  est complet) : 7 fermetures sur 9 dans les actes I et II, aucune dans l'acte III. Ni l'extinction des flammes, ni le stencil des
  mains, ni l'ambiance (`HorrorLook`) n'en sont la cause (essais avec chacun coupé). Cause non trouvée cette nuit.

## Réglages choisis

| Réglage | Valeur | Où |
|---|---|---|
| Prix de la porte de sortie | 150 / 200 / 250 âmes | `Zone_Acte1..3` |
| Prix des améliorations | 60, 90, 140, 220, 350 ; hausse +10 % linéaire, plafond ×2 | `Economy_Default` |
| Salles par acte | 22 / 20 / 22 demandées (24 à 26 obtenues avec les annexes) | `Act_1..3` |
| Boucles, impasses | `loopChance` 0.8, `minDoorsPerRoom` 2, `landingChance` 0.5 | `Act_1..3` |
| Pression | quota 6 / 8 / 10 goules par joueur, 2.5 s, plafond 16, repos 12 s | `Wave_*` |
| Scaling | PV +12 %, dégâts +6 % par point de puissance (exposant 0.9) ; ×1.25 / ×1.15 par acte | `EnemyScaling_Default` |
| Flammes du décor | coupées au-delà de 30 m (hystérésis 4 m) | `HorrorLook_*` |
| Props de la DA | une entrée par greybox, échelle bornée (coffres ×1.5, attrapables ×1), écart de forme ≤ 0.9 | `PropArt_Default` |

## Inventaire DA (retiré ou remplacé)

- Orbes d'offre et textes flottants des coffres et du marchand : retirés (menus de la DA).
- Boule rouge d'annonce du volant : retirée (contour discret de la DA).
- Violet d'un ennemi attrapé : retiré (clignotement blanc avant qu'il se libère).
- Lune du menu dans le ciel de l'acte III : désactivée.
- Cimes de forêt en sphères (acte III) : retirées.
- Herse à barreaux et cellule visible du traqueur : remplacées par une porte pleine.
- « VAGUE n » à l'écran et klaxon des vagues : retirés.
- Props greybox : remplacés par les modèles de la DA quand la forme concorde (416 props sur les 3 cartes de la graine 11) ; restent en
  greybox les caisses (pas de modèle), 38 props de l'acte II de forme trop différente (armoires couchées, petits râteliers).

## Relecture indépendante

Un sous-agent qui n'avait rien vu du travail a relu le brief, le code récent, les rapports, les mesures et les planches.

**Corrigé après la relecture :**
- **Fin d'acte sans payer** : la boîte du hall faisait gagner l'acte même porte fermée (atteinte par téléportation, ou par traction
  à travers les battants). Elle ne compte plus que porte ouverte ; `ExitHallTests` vérifie maintenant aussi le cas porte fermée (9/9).
- **Entrées coupées après la caméra de mort** : un joueur relevé en coop après une prise du traqueur ne répondait plus. Les entrées
  sont rendues à la fin de la mise en scène.
- **Âmes invisibles un instant** : l'extinction des effets lointains touchait aussi les flammes des âmes. Elle ne coupe plus que les
  effets seuls sur leur objet, hors âmes.
- **Rapports du bot** : l'en-tête disait « acte 1 » partout et le temps réel s'additionnait d'un passage à l'autre ; la note « porte
  payée : refusée » était lue une image trop tôt. Corrigé (les rapports déjà écrits gardent ces erreurs : seul leur temps de jeu fait foi).

**Contesté, avec la réponse :**
- « Les FPS ont été mesurés avant les props » : refait sur le build final avec les props et les correctifs (9,5 / 10,9 / 9,6 ms, `perf/final_*`).
- « 60 i/s non tenus au pire cas de l'acte II » : vrai pour le 95e centile (23 ms à 24 min de timer, 119 ennemis) ; la médiane reste
  à 12,9 ms. Noté dans « Ce qui reste ».
- « Ouvertures de porte noires » : ce sont les couloirs entre deux salles, sans lumière par choix (lueur chaude au seuil seulement).
  À juger en jeu : si c'est encore gênant, une lumière par couloir coûte en performance (lumières limitées par Forward+).
- « Coffres à l'échelle fixe 1,5 » : c'est la règle de la DA pour les coffres (`Props4.Regle`) ; le collider de la greybox reste.

- **Bot qui se croyait en marche** (trouvé en relançant le contrôle) : chaque recalcul de chemin comptait comme un progrès, donc un
  blocage contre un mur n'était jamais détecté. Corrigé ; passage de contrôle, acte II graine 22 : fini à 5:54, porte payée puis
  ouverte, 855 parades réussies, 5 blocages notés, 0 erreur.

**Gardé tel quel (mineur)** : l'indicateur `StalkerDeathCam.Active` est commun aux deux joueurs en écran partagé ; la caméra de mort
vise le premier traqueur trouvé (un seul par niveau aujourd'hui).

## Ce qui reste (honnêtement)

1. **Économie : à trancher par vous.** Le bot gagne 53 à 334 âmes en 10 minutes ; la sortie coûte 150 / 200 / 250. Le bot ramasse
   mal (il ne fait pas de détour pour les âmes), donc un joueur humain gagnera plus, mais combien ? Trois options :
   a) garder les prix et juger en playtest ; b) baisser à 100 / 150 / 200 ; c) prix de sortie proportionnel aux âmes gagnées dans
   l'acte (par exemple 60 %).
2. **Le bot ne termine pas 3 cartes ni le boss.** Les blocages sont sur les paliers et les escalades (sa navigation suit le NavMesh au
   sol). Je n'ai pas prouvé qu'un humain passe partout : la checklist le demande.
3. **Plantage du build à la fermeture** (D3D12, après le nettoyage du jeu) : 7 fermetures sur 9 dans les actes I et II. Pas de perte
   de données constatée (le journal est complet), mais Windows peut afficher la fenêtre de plantage. Cause non trouvée.
4. **Pire cas de l'acte II** : 95e centile à 23 ms quand le timer est très avancé (119 ennemis). Piste : les 665 lumières du château
   (lustres, appliques) et le coût du rendu sur le processeur.
5. **Props sans modèle** : caisses (aucun modèle de la DA), 38 props de l'acte II de forme trop différente (armoires couchées, petits
   râteliers) ; les props créés après le chargement (réapparitions) restent en greybox.
6. **Coffres moins lumineux** que le bloc jaune de la greybox : la lueur dorée de la DA est réelle mais douce. Pilier 4 (« tout ce qui
   est interactif brille ») : à regarder en jeu.
7. **Pas vu en jeu par le bot** : l'écran de fin (grille), les boutons Recommencer et Campement, le HUD des améliorations, la minicarte.
   Couverts par des tests EditMode, pas par une capture en jeu.
8. **Acte III** : pas de passages creusés dans la falaise ; terrasses et paliers seulement.
9. **Porte verrouillée** : c'est toujours la grille de fer du jeu (la porte guillotine du kit de la DA n'est pas branchée).

## Checklist de test (dans l'ordre)

Build : `Builds/Nuit_2026-11-23/ItHunts.exe` (développement). Debug : F3 ; Page bas pour l'admin.

1. **Menu, campement** : lancer, ouvrir le campement, revenir. Rien ne déborde.
2. **Acte I, 2 minutes** : course, glissade, bunny hop, traction. Les props sont en 3D (tonneaux, bancs, étagères) ; les coffres se
   lisent-ils dans le noir ?
3. **Parade** : attendre un bond de goule et parer pendant le vol. Ça passe ?
4. **Traqueur** : se faire repérer (cri), fuir en traction et bunny hop. Trop facile ? (voir « Mesures »).
5. **Marchand** : une nouvelle amélioration et une montée de rang ; les cadres de rang et le losange du passif au-dessus de la vie.
6. **Âmes à 10:00** : noter combien vous en avez, et le prix de la sortie (point 1 de « Ce qui reste »).
7. **Porte de sortie** : avant la fin du temps, le panneau « Sortie scellée » ; après paiement, les battants s'ouvrent ; le hall mène
   à l'acte II.
8. **Acte II** : couloirs entre les salles (assez éclairés ?), paliers et escaliers (on passe partout ?), images par seconde (F3).
9. **Acte III** : arbres morts, ciel, plus de sphères noires ; terrasses.
10. **Mort par le traqueur** : le screamer, puis la même chose avec « Réduire les animations » coché.
11. **Boss** : entrée du boss, marquages au sol, esquives ; puis l'écran de fin en grille, Recommencer et Campement.
12. **Fermeture** : quitter par le menu ; une fenêtre de plantage apparaît-elle ?
