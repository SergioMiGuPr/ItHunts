# ItHunts — Game Design Document

**Version:** 0.16 · **Updated:** 2026-11-12 · [Features](#feature-index) · [Decisions](#decisions) · [Change log](#change-log)

## Résumé

Le joueur traverse quatre zones sombres (cave du château, château, forêt, zone finale). Dans
chacune des trois premières, il a environ 20 minutes pour farmer des âmes dans des arènes
reliées, en tuant goules et volants avec le décor qu'il attrape et lance par télékinésie. Il
fuit aussi en permanence un monstre géant, intuable, qui le tue en un coup. Seul un mouvement
maîtrisé lui échappe : glissade, wall jump, bunny hop, impulsions magnétiques. Le plaisir vient
de l'élan et de la tension du « encore une salle avant de sortir ». La run se gagne en tuant
enfin le monstre dans la zone finale ; elle se perd à la première mort (solo).

| | |
|---|---|
| Genre et caméra | Roguelite d'action à la première personne, axé sur le mouvement ; mains et orbe visibles |
| Public | Joueurs PC « core », amateurs de movement shooters et de roguelites |
| Plateformes et entrées | PC ; clavier-souris et manette dès le départ |
| Session | Une run ≈ 3 × 20 min + le combat final, soit environ 65-70 min [ASSUMPTION] ; durée de zone réglable (4 min en test) |
| Moteur | Unity 6.3 LTS (6000.3.28f1), URP, Input System, Cinemachine 3 |
| Scope | Projet indé : solo puis écran partagé à 2, coop en ligne 1-4 plus tard |
| Références | Deadlock (mouvement, arènes) ; Titanfall 2 (élan) ; Ultrakill (vitesse, style) ; Dishonored (mobilité, environnement) ; Hades (structure, améliorations) ; Lethal Company (traque, sortir à temps) ; Borderlands et Arcane (style visuel) — détail dans le [BRIEF](BRIEF.md#références) |

## Piliers

1. **Le mouvement est la survie.** Le boss est plus rapide que la course : chaque capacité de
   mouvement doit garder ou créer de l'élan. Aucune fonctionnalité ne doit arrêter le joueur net.
2. **L'arène est l'arme.** De base, les orbes ne font pas de dégâts directs : on tue avec
   l'environnement (objets lancés, lustres, piliers, pièges, points faibles). Les améliorations
   peuvent ajouter des dégâts aux orbes, en plus de l'environnement. Chaque arène fournit assez
   d'objets lumineux à lancer.
3. **Risque contre butin.** Rester farmer rapporte des âmes, mais le monstre rôde et le timer
   tourne. Sortir tôt est toujours possible ; rester après le timer multiplie les âmes, face à
   un boss enragé.
4. **On lit tout dans le noir.** Ce qui est interactif brille ; le boss s'entend avant de se voir.
5. **Prêt pour la coop.** Tout l'état d'un joueur vit sur son prefab, entrées séparées de la
   simulation (voir CLAUDE.md).

## Boucle de jeu

```
bouger → trouver objets / ennemis → lancer, tuer → âmes → coffres, marchands, améliorations
   → plus fort, mais le timer avance et le boss approche → sortir (ou rester) → zone suivante
```
- **Secondes :** [mouvement](features/mouvement/design.md), [orbes](features/orbes/design.md),
  fuite devant le [boss traqueur](features/boss-traqueur/design.md).
- **Minutes :** vagues dans les arènes, coffres et marchands, choix d'amélioration, décision de
  sortir.
- **Run et entre les runs :** 4 zones ; à la mort, une part des âmes devient monnaie de
  déblocage (méta-progression sans bonus chiffrés).

## Feature index

| Feature | Status | Scope | Summary | Docs |
|---|---|---|---|---|
| Mouvement | implemented | MVP | Course, saut tolérant, glissade (accélère en pente), wall slide et wall jump, bunny hop façon Source, dash ; charges de mouvement partagées (saut en l'air, dash, magnétisme) | [design](features/mouvement/design.md) |
| Orbes | in progress | MVP | **Une seule orbe** (D-024) : traction (3 charges d'attraction, 1 toutes les 4 s), répulsion (charge unique), attraper / relâcher (aide à la visée en cône) et lancer (tir tendu), toutes disponibles en même temps sur des touches séparées ; sans endurance ; couleur de l'orbe selon l'action ; dégâts d'impact = masse × vitesse au-dessus d'un seuil, zone et durabilité ; répulsion en onde qui repousse et étourdit | [design](features/orbes/design.md) |
| Artefacts | designed | MVP | 1 artefact tiré au hasard par joueur et par run (pas de doublon en coop), en main gauche ; premier : le barillet (6 munitions, projectiles) | [design](features/artefacts/design.md) |
| Boss traqueur | designed | MVP | Monstre géant intuable qui rôde, tue en un coup, plus rapide que la course ; enragé après le timer | [design](features/boss-traqueur/design.md) |
| Zones et timer | idea | MVP | Arènes faites main tirées au hasard et reliées ; timer de 20 min ; sortie anticipée | — |
| Ennemis zone 1 | idea | MVP | Goule (rampe, bondit, grimpe aux murs ; attrapable étourdie) et petit volant attrapable | — |
| Âmes et économie | idea | MVP | Collecte autour des orbes, coffres, marchands, salles verrouillées | — |
| Améliorations | idea | MVP | Modifs des orbes (dont des dégâts directs), stats, modifs du mouvement, reliques passives | — |
| Vie et mort | idea | MVP | PV fixes, soins rares, bilan de fin de run et relance en un bouton | — |
| HUD et debug | idea | MVP | Charges, recharges, âmes, timer ; affichage de debug activable | — |
| Vue FPS | idea | MVP | Mains et orbe, couleurs par mode | — |
| Méta-progression | idea | later | Déblocages achetés avec une part des âmes | — |
| Combat final | idea | later | Zone finale où le boss devient tuable par l'environnement (lustres, piliers, pièges, points faibles au magnétisme) | — |
| Coop locale | idea | later | Écran partagé à 2 | — |
| Coop en ligne | idea | later | 1 à 4 joueurs ; âmes et améliorations individuelles, réanimation | — |

## Jalons

Suivent l'ordre de travail de CLAUDE.md ; chaque jalon se termine par un playtest.
1. **M1, mouvement en greybox — terminé (2026-11-12) :** mouvement + HUD de debug dans `Test_Movement` · fini quand :
   l'utilisateur enchaîne glissade → wall jump → bunny hop sans perdre de vitesse, et valide
   les réglages.
2. **M2, orbe :** magnétisme puis télékinésie, réunis en une seule orbe (D-024) · fini quand : on atteint une
   plateforme hors de portée de saut avec l'attraction, et on tue un ennemi factice avec une
   caisse.
3. **M2 bis, artefacts :** système générique (tirage au hasard, pas de doublon en coop, emplacement en
   main gauche), puis le barillet · fini quand : on tue un ennemi factice au barillet quand il ne reste
   rien à lancer.
4. **M3, premier prototype de zone :** une seule zone, **une goule et le traqueur**, timer de
   test de 4 min, sortie · fini quand : on tue la goule avec l'environnement et on atteint la
   sortie en échappant au traqueur.
5. **M3 bis, boucle d'une zone :** vagues, âmes, coffre, marchand, améliorations · fini quand :
   une zone de 3 arènes se joue du début à la sortie.
6. **M4, run complète,** puis écran partagé, direction artistique, coop en ligne, build.

## Direction

- **Art :** cel-shading à contours épais et textures peintes, lumière dramatique et colorée ;
  très sombre, l'interactif brille (détails au guide de style, étape 10).
- **Son :** le boss s'annonce par le son (pas lourds, souffle, raclements) bien avant d'être
  visible ; c'est une information de jeu, pas seulement de l'ambiance.
- **Ressenti :** la vitesse se sent (FOV qui s'élargit, vent) ; une mort par le boss est brutale
  et immédiate, jamais injuste (il a été entendu avant).

## Decisions

| ID | Date | Decision | Alternatives considered | Why | Affects |
|---|---|---|---|---|---|
| D-001 | 2026-11-11 | Moteur Unity 6000.3.28f1 LTS, URP | 6000.3.1f1 | correctif de sécurité Brotli (CVE-2026-111497) | tout |
| D-002 | 2026-11-11 | Le boss est un traqueur unique, intuable avant la zone finale, qui tue en un coup | boss par zone ; boss qu'on étourdit | choix de l'utilisateur : la fuite par le mouvement est le cœur du jeu | [boss](features/boss-traqueur/design.md), [mouvement](features/mouvement/design.md) |
| D-003 | 2026-11-11 | Zones de farm avec timer de 20 min et sortie anticipée, au lieu d'une suite de salles fixe | salles enchaînées ; grande zone ouverte | risque contre butin, et place pour que le boss rôde | zones et timer |
| D-004 | 2026-11-11 | Les orbes ne font pas de dégâts directs → superseded by D-008 | petit tir de base ; dégâts au contact | « l'arène est l'arme » | [orbes](features/orbes/design.md) |
| D-005 | 2026-11-11 | Attraction = impulsion unique, sur surfaces seulement → superseded by D-021 | grappin maintenu ; cible ennemi | se combine avec le bunny hop sans l'éclipser ; simple à prédire en réseau | [orbes](features/orbes/design.md) |
| D-006 | 2026-11-11 | Méta-progression par déblocages, sans bonus chiffrés | petits bonus permanents ; rien | équité en coop entre joueurs de niveaux différents | méta-progression |
| D-007 | 2026-11-11 | Coop : âmes et améliorations individuelles ; non réanimé = retour à la zone suivante | âmes partagées ; mort jusqu'à la zone suivante sans réanimation | choix de l'utilisateur | coop en ligne |
| D-008 | 2026-11-11 | Pas de dégâts directs de base ; des améliorations peuvent en ajouter aux orbes et aux objets lancés, en plus de l'environnement. Boss final tué par l'environnement | aucun dégât direct jamais (D-004) | validation du GDD : plus de variété de builds | [orbes](features/orbes/design.md), améliorations, combat final |
| D-009 | 2026-11-11 | Phase enragée : multiplicateur d'âmes ; coop : le boss chasse le dernier joueur vu ou entendu | pas de bonus ; joueur le plus proche | validation du GDD : récompenser la prise de risque | [boss](features/boss-traqueur/design.md) |
| D-010 | 2026-11-11 | La glissade ne coûte pas d'endurance ; durée de zone réglable (4 min en test) | glissade payante ; 20 min fixes | validation du GDD : l'endurance reste aux orbes ; tests rapides | [mouvement](features/mouvement/design.md), [boss](features/boss-traqueur/design.md) |
| D-011 | 2026-11-11 | Premier prototype de zone : une zone, une goule, le traqueur | zone complète d'emblée | valider la tension de la traque avant le reste | jalon M3 |
| D-012 | 2026-11-11 | Mouvement façon Deadlock : contrôle aérien (W seul), auto-bunny hop, 2 sauts (réglable), pas de plafond bas de vitesse (sécurité à 50 m/s) ; la limite vient du nombre de sauts | Source pur ; plafond doux à 25 m/s | playtest de l'étape 1 : W en l'air partait de côté au lieu de suivre la caméra | [mouvement](features/mouvement/design.md) |
| D-013 | 2026-11-11 | Glissade façon Deadlock / Apex : élan conservé à 100 % à l'entrée, décélération constante (3 m/s²), vitesse minimale de 5 m/s sous un obstacle | friction proportionnelle à la vitesse (0.5 /s) ; fin de glissade en accroupi sous un plafond | playtest : le bunny hop perdait son élan en glissade, le passage bas finissait accroupi | [mouvement](features/mouvement/design.md) |
| D-014 | 2026-11-12 | Le contrôle aérien ne s'applique pas au-delà de 100° entre la caméra et la trajectoire | contrôle aérien plus long coupé après un wall jump ; ne rien changer | Play Mode : tenir W face au mur ramenait sur le mur après un wall jump | [mouvement](features/mouvement/design.md) |
| D-015 | 2026-11-12 | Charges de mouvement (3 par défaut) partagées par le saut en l'air et le dash ; la télékinésie garde sa propre endurance. *Mis à jour par D-021 : le magnétisme n'utilise plus les charges ; par D-024 : plus d'endurance pour la télékinésie* | deux barres séparées ; une seule barre pour tout | un seul budget pour toute la mobilité : lisible, et limite l'écart de vitesse avec le traqueur ; le combat (télékinésie) ne prive jamais de mobilité | [mouvement](features/mouvement/design.md), [orbes](features/orbes/design.md) |
| D-016 | 2026-11-12 | Murs façon Deadlock : wall jump à 1.8 m, impulsion ajoutée à la vitesse, direction mi-normale mi-avant, jamais moins vite qu'avant (+1 m/s) ; wall slide sans perte, actif en longeant le mur, effet wall run ; le mur recharge les sauts, pas les charges | wall jump perpendiculaire au mur (1.05 m) ; wall slide seulement en poussant | playtest : wall jump trop bas et de côté, perte de vitesse le long du mur (mesurée : 15 → 7.5 m/s sans détection, −8 %/s caméra vers le mur) | [mouvement](features/mouvement/design.md) |
| D-017 | 2026-11-12 | Dash avec durée (0.18 s, 6.5 m, ni friction ni gravité), sortie à max(vitesse d'avant, 12 m/s), appui mémorisé 0.3 s ; gain du wall jump plafonné à 20 m/s ; HUD des charges sous le viseur | dash instantané à 14 m/s | playtest et mesures : retombée à 7.5 m/s en 0.2 s, 4.2 m seulement, aucun effet à 20 m/s en l'air, 1 dash sur 3 avec des appuis à 0.05 s ; montée trop rapide dans la cheminée | [mouvement](features/mouvement/design.md) |
| D-018 | 2026-11-12 | Dash progressif sur 0.3 s (pic 30 m/s à l'arrêt), +1 m/s au sol, aucun gain en l'air (vitesse redirigée), dash-jump au sol +3 m/s (fenêtre 0.15 s), plafond commun des gains à 20 m/s (remplace celui du wall jump) | plateau constant plus bas et plus long (21.7 m/s) | playtest : pic à 35 m/s puis marche à 12 m/s jugée bizarre ; besoin d'une technique de timing au sol | [mouvement](features/mouvement/design.md) |
| D-019 | 2026-11-12 | Le bonus de glissade (+2 m/s) entre dans le plafond commun des gains (20 m/s) | bonus toujours ajouté | cohérence : un seul plafond pour tous les gains de mouvement | [mouvement](features/mouvement/design.md) |
| D-020 | 2026-11-12 | → superseded by D-021. Magnétisme : l'impulsion annule d'abord la part de vitesse qui va dans le sens contraire, puis s'ajoute ; gain horizontal dans le plafond commun des gains (20 m/s, au-delà vitesse conservée et redirigée) ; attraction et répulsion seulement sur les surfaces magnétiques ; clic gauche / RT attire, clic droit / LT repousse | addition pure ; traction continue façon grappin ; pas de plafond ; répulsion sur toute surface ; un bouton + changement de mode | l'attraction marche même en pleine chute ; un seul plafond pour tous les gains ; le level design garde la main ; réagir vite à pleine vitesse | [orbes](features/orbes/design.md) |
| D-021 | 2026-11-12 | Magnétisme, après playtest : **attraction = traction linéaire continue** (bouton maintenu, ligne droite vers le point fixé au clic, 80 m/s² jusqu'à 30 m/s, portée 60 m, 2.5 s max ; arrêt au relâchement, à l'arrivée, ligne de vue coupée, saut, dash, répulsion ou obstacle), sans gravité ; elle ne réduit jamais la vitesse (plus vite : vitesse gardée et redirigée) et **n'est pas soumise au plafond commun des gains** (exception, seul le plafond de sécurité de 50 m/s reste) ; **répulsion** instantanée (14 m/s, 25 m) dont la vitesse horizontale ne baisse jamais et dont le gain reste dans le plafond commun ; **toute surface** répond ; **aucune charge de mouvement** : **mis à jour après le playtest v2 :** 3 charges d'attraction séparées (1 traction = 1 charge, 1 charge toutes les 3 s, une par une) et une répulsion à charge unique (portée 40 m, recharge 5 s après chaque usage, marquage du point visé et de l'impact), au lieu des recharges de 0.25 s et 0.35 s jugées abusées ; pendant un dash, la vitesse gardée est celle de sortie du dash ; un saut arrête la traction | impulsion unique (D-020) ; charges partagées (D-015) ; surfaces magnétiques seulement ; répulsion sans plafond ; pic du dash gardé | playtest : l'impulsion ne portait pas assez haut ; la traction est l'outil de grande verticalité ; rien ne doit casser l'élan | [orbes](features/orbes/design.md), [mouvement](features/mouvement/design.md) |
| D-022 | 2026-11-12 | **Artefacts** : au début de chaque run, chaque joueur reçoit 1 artefact tiré au hasard (pas de doublon entre joueurs en coop), porté en main gauche ; il ajoute une mécanique de combat en plus des objets du terrain. Premier artefact : le **barillet** (la main gauche devient un barillet de 6 munitions qui tire des projectiles). Cohérent avec D-008 : de base, les orbes ne font pas de dégâts directs ; artefacts et améliorations en ajoutent | combat seulement par les objets lancés ; arme de base pour tous | une source de dégâts quand il ne reste rien à lancer ; variété d'une run à l'autre ; rôles différents en coop | [artefacts](features/artefacts/design.md) |
| D-023 | 2026-11-12 | *Commandes, échange et endurance remplacés par D-024 ; le reste tient.* Télékinésie et échange : un clic gauche / RT attrape (l'objet reste tenu sans maintenir), un nouveau clic gauche le pose, le clic droit / LT le lance à 30 m/s **plus la vitesse du joueur** ; échange par la molette (deux sens) ou Y, touches 1 et 2 pour choisir ; couleurs violet (attraper, tenir) et vert (lancer) ; dégâts des objets = masse × vitesse d'impact × facteur au-dessus d'une vitesse minimale (partie 2) | maintenir pour tenir ; lancer à vitesse fixe ; molette seule ; dégâts reportés à l'étape 6 | les mains restent libres pour bouger ; lancer en pleine course frappe plus fort (le mouvement est le jeu) ; condition de fin du jalon M2 | [orbes](features/orbes/design.md) |
| D-024 | 2026-11-12 | Après le playtest de la télékinésie, **une seule orbe** : plus d'orbe active ni d'échange (molette, Y, touches 1 et 2 supprimées) ; les quatre actions sont disponibles en même temps, chacune sur sa touche (on peut se tracter en tenant un objet et le lancer en arrivant). Commandes : clavier AZERTY **A maintenu = traction**, **clic droit = répulsion**, **F = attraper l'objet visé, ou le relâcher (posé doucement)**, **clic gauche = lancer l'objet tenu** (sans objet : réservé à l'artefact, ne fait rien pour l'instant) ; manette **RB maintenu = traction**, **LT = répulsion**, **X = attraper / relâcher**, **RT = lancer / artefact** (saut A, glissade B, dash LB inchangés ; Y libre). Couleur de l'orbe selon l'action : bleu traction, orange répulsion, violet objet tenu, vert lancer ; quand elles se superposent, **un éclat bref (lancer, répulsion, 0.15 s) l'emporte sur un état qui dure, et la traction l'emporte sur l'objet tenu** (l'objet et son trait restent violets). **Plus d'endurance** pour la télékinésie (jauge et barre retirées) ; seul reste le délai de 0.3 s après un lancer. Attraction : **1 charge toutes les 4 s** (au lieu de 3 s) | garder deux orbes et l'échange ; mélange des couleurs (bleu-violet) ; orbe coupée en deux couleurs ; garder l'endurance | combiner traction et objets en plein mouvement (« le mouvement est le jeu ») ; l'échange coupait l'élan des combos ; un mélange de couleurs se lit mal à pleine vitesse ; la recharge plus longue de l'attraction compense le fait qu'elle n'est plus jamais rangée | [orbes](features/orbes/design.md) |
| D-025 | 2026-11-12 | *Lancer et gravité précisés par D-027.* Télékinésie, partie 2 : **dégâts d'impact = masse (kg) × (vitesse d'impact − 8 m/s) × 0.1** (caisse de 25 kg lancée à l'arrêt : 55 ; en course : ≈ 80) ; **tout impact** d'un objet attrapable sur un ennemi au-dessus du seuil compte (lancé, rebond, chute, objet tenu), un même objet ne blesse la même cible qu'une fois toutes les 0.3 s ; **chute** (impact surtout vertical, objet au-dessus) : seuil de **3 m/s** ; **tir tendu** : pas de gravité pendant 0.5 s après un lancer ou jusqu'au premier choc, puis gravité normale ; les objets ne blessent **jamais les joueurs** (tir ami tranché à l'étape 11) ; ennemis factices de test : 3 fixes et 1 mobile (6 m/s), 100 PV, réapparition en 3 s, chiffres de dégâts et barre de PV ; le magnétisme ne s'accroche jamais à un ennemi | √masse ; énergie ½mv² ; seulement les objets lancés ; trajectoire compensée ou physique pure ; bonus par objet pour les chutes | la masse et la vitesse comptent toutes les deux (lancer en course frappe plus fort) ; un système qui marche aussi pour le lustre et les pièges ; mesuré : sans tir tendu, un objet lancé tombait de 2.2 m sur 20 m et ratait sa cible ; une caisse tombée de 4 m ne faisait que 1.3 dégât | [orbes](features/orbes/design.md) |
| D-026 | 2026-11-12 | **Aide à la visée de la prise (F)** : un objet attrapable vu dans un **cône** autour du viseur est sélectionné (demi-angle **8° jusqu'à 5 m, puis linéaire jusqu'à 3° à 60 m**) ; parmi plusieurs, le plus proche **du viseur** ; jamais à travers un mur, ni derrière ou loin sur le côté ; les objets trop lourds seulement au rayon exact ; l'objet sélectionné brille vivement et pulse, anneau violet au viseur ; réglable dans `Telekinesis_Default` | rayon exact ; cône fixe ; le plus proche du joueur | playtest : trop dur d'attraper à distance en mouvement ; mesuré : écart toléré ×4 à 10 m, ×8 à 30 et 60 m | [orbes](features/orbes/design.md) |
| D-027 | 2026-11-12 | Après le playtest de la partie 2 : **lancer toujours vers le point sous le viseur** au moment du clic (rayon depuis les yeux au début du tick, direction depuis la position réelle de l'objet) ; la vitesse du joueur **ne dévie plus** l'objet, seule sa part dans la direction du lancer s'ajoute (si elle est positive) ; **gravité des objets à 20 m/s²**, comme le joueur ; **dégâts de zone** au premier impact d'un objet lancé (rayon 1 m + 0.06 m par kg, 60 % des dégâts d'impact au centre, 25 % de ça au bord, jamais à travers un mur) ; **durabilité** selon la masse (≤ 5 kg : 1 lancer, ≤ 15 kg : 2, ≤ 25 kg : 3, plus lourd : 5), réglable par objet, puis l'objet se casse (éclat, débris, son) ; **lustre** : se casse au premier impact, lancé ou tombé, zone de 4 m ; **répulsion = onde au point visé** (4 m) qui repousse objets et ennemis (16 m/s pour une masse de référence de 15 kg ou 80 kg, ×0.35 à ×1.3 selon la masse, moitié au bord), en plus de la propulsion ; un objet accroché est décroché et tombe droit ; un ennemi repoussé qui percute un obstacle solide à **6 m/s** ou plus est **étourdi 1.5 s** (étoiles, lueur jaune, son ; il ne bouge plus ; état prêt pour la prise d'ennemis) ; la traction ne s'accroche toujours pas aux ennemis, la répulsion oui | cible directe (l'ennemi visé est repoussé sans propulsion) ; hybride ; vitesse du joueur ajoutée en entier ; gravité 9.81 | playtest : le lancer en mouvement partait de travers (mesuré : 7 m d'écart à 30 m en course de côté, 10 m en traction) ; l'onde garde la propulsion, au cœur du mouvement | [orbes](features/orbes/design.md) |

## Open questions

- **Hypothèses à confirmer :** durée totale d'une run ; tous les chiffres marqués [ASSUMPTION]
  dans les notes de fonctionnalités.
- **À trancher plus tard :** déroulé exact du combat final (phases, points faibles) ; valeur du
  multiplicateur d'âmes en phase enragée.
- **Risques :**
  - *Ressenti* : le boss plus rapide que la course peut rendre la fuite frustrante pour un
    débutant → le régler en playtest dès M3, avec une vitesse modifiable dans un ScriptableObject.
  - *Technique* : une goule qui grimpe aux murs sort du NavMesh classique (navigation sur mesure
    ou NavMeshLinks) → prototype isolé en M3.
  - *Réseau* : une mort en un coup est très sensible à la latence ; le bunny hop et les objets
    lancés aussi → serveur qui fait autorité sur les coups du boss, à prévoir à l'étape 11.

## Change log

**0.1 (2026-11-11):** premier jet, à partir des réponses au [brief](BRIEF.md).
**0.2 (2026-11-11):** GDD validé ; D-008 à D-011 (dégâts via améliorations, bonus en phase
enragée, glissade sans endurance, zone de test de 4 min, premier prototype de zone) ; références.
**0.3 (2026-11-11):** D-012, mouvement façon Deadlock après le premier playtest ([mouvement](features/mouvement/design.md)).
**0.4 (2026-11-11):** D-013, glissade qui conserve l'élan ([mouvement](features/mouvement/design.md)).
**0.5 (2026-11-12):** D-014 (seuil d'angle du contrôle aérien) et D-015 (charges de mouvement partagées, dash) ([mouvement](features/mouvement/design.md), [orbes](features/orbes/design.md)).
**0.6 (2026-11-12):** D-016, murs façon Deadlock ([mouvement](features/mouvement/design.md)).
**0.7 (2026-11-12):** D-017, dash v2, plafond du wall jump, HUD des charges ([mouvement](features/mouvement/design.md)).
**0.8 (2026-11-12):** D-018, dash progressif, gains plafonnés, dash-jump ([mouvement](features/mouvement/design.md)).
**0.9 (2026-11-12):** D-019 ; mouvement de base validé et implémenté ([mouvement](features/mouvement/design.md)).
**0.10 (2026-11-12):** D-020, orbe de magnétisme (partie 1 : attraction, répulsion, charges) ([orbes](features/orbes/design.md)) ; décisions et journal remis dans l'ordre.
**0.11 (2026-11-12):** D-021 : traction continue, toute surface, sans charges ; D-015 mise à jour, D-005 et D-020 remplacées ([orbes](features/orbes/design.md)).
**0.12 (2026-11-12):** D-021 mise à jour (charges d'attraction, répulsion unique) ; D-022, artefacts et barillet ([artefacts](features/artefacts/design.md)) ; jalon M2 bis.
**0.13 (2026-11-12):** D-023, télékinésie et échange des orbes ([orbes](features/orbes/design.md)).
**0.14 (2026-11-12):** D-024, une seule orbe : actions simultanées sur des touches séparées, couleur selon l'action, plus d'endurance, attraction toutes les 4 s ; D-023 et D-015 mises à jour ([orbes](features/orbes/design.md)).
**0.15 (2026-11-12):** D-025, dégâts d'impact, tir tendu, chutes, ennemis factices ; D-026, aide à la visée de la prise ; piste direction artistique en parallèle (branche `art`, [ART-HANDOFF](../art/ART-HANDOFF.md)).
**0.16 (2026-11-12):** D-027, lancer toujours vers le viseur, gravité des objets à 20 m/s², dégâts de zone, durabilité, onde de répulsion, étourdissement ; D-025 mise à jour.
