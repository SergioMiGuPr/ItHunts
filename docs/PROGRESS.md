# Avancement

> Mémoire du projet entre les sessions. Claude la met à jour à la fin de chaque étape.

## Étape en cours

**4 — Télékinésie, une seule orbe, dégâts** (D-023 à D-027) — terminée.

1. **Cœur et orbe unique** — **fait et validé en playtest** (2026-11-12), commit `5a8d454`. Retour : la prise à
   distance en mouvement était trop dure → aide à la visée.
2. **Aide à la visée de la prise (D-026)** — fait le 2026-11-12, **playtest en attente** : cône de 8° jusqu'à 5 m,
   3° à 60 m, le plus proche du viseur, jamais à travers un mur ; l'objet sélectionné pulse. Mesuré : écart toléré
   1.8 m à 10 m, 3.5 m à 30 m, 3.6 m à 60 m (0.4 m sans aide) ; en bunny hop à 20 m/s, caisse sélectionnée 86 à
   100 % du temps (17 à 71 % sans).
3. **Partie 2, dégâts (D-025)** — fait le 2026-11-12, **playtest en attente** (avec l'aide à la visée) : dégâts =
   masse × (vitesse d'impact − 8 m/s) × 0.1 pour tout impact sur un ennemi, seuil de 3 m/s pour une chute, tir
   tendu (sans gravité 0.5 s), 3 factices fixes et 1 mobile (100 PV, réapparition 3 s), chiffres de dégâts, barre de
   PV, éclats, sons provisoires ; jamais de dégâts aux joueurs ; pas de magnétisme sur un ennemi. Mesuré en Play
   Mode : 55 dégâts (25 kg à 10 et 20 m), 79.6 en course, 13.8 pour une chute de 4 m. 203 tests (1 non concluant à
   dessein : collision vérifiée en Play Mode). Checklist unique : `docs/design/features/orbes/testing.md`.
   **Playtest du 2026-11-12 :** dégâts bons ; gravité des objets à 20 m/s² ; lancer de travers en mouvement → D-027.
4. **Après ce playtest (D-027)** — fait et **validé en playtest** le 2026-11-12 (visée du lancer en mouvement,
   répulsion et étourdissement contre un mur) : lancer toujours vers le
   viseur (cause : toute la vitesse du joueur s'ajoutait ; écart en traction 10.3 m → 0.15 m à 30 m), gravité des
   objets à 20 m/s², dégâts de zone, durabilité selon la masse (1 / 2 / 3 / 5 lancers), lustre fragile (zone 4 m),
   répulsion en onde (4 m) qui repousse objets et ennemis, étourdissement 1.5 s contre un mur à 6 m/s ou plus.
   215 tests (1 non concluant à dessein). Checklist : `docs/design/features/orbes/testing.md`.
5. **Étape 4 terminée** (commit `44c2a2e`, poussé sur `main`).

**5 — Vue FPS et HUD, répulsion façon aimant, prise des ennemis étourdis, dégâts contre un mur** (D-028 à D-031) —
**fait et validé en playtest** (2026-11-12) ; commit et push sur `main`.
- Mains greybox, orbe dans la paume droite, mouvement et gestes ; HUD uGUI par joueur (barres de PV corrigées en
  mouvement : 168 px d'écart en saut → 0) ; debug F3 en trois pages.
- **Rendu des mains : option 2** (D-028), décidée après un build de développement : passe « Render Objects » à 60°
  après effacement de la profondeur, sans caméra en plus. Surcoût mesuré : 1 joueur **+0.26 ms** (option 1 : +1.60),
  2 joueurs **+0.63 ms** (option 1 : +7.19).
- Répulsion façon aimant (D-029) : cône de 14 m, onde de choc à l'appui, poussée continue au maintien (0.8 s).
- Prise des ennemis étourdis (D-030), dégâts contre un mur d'un ennemi repoussé (5 PV/m/s au-delà de 6, 40 au plus)
  et objets repoussés qui blessent (×0.5, sans zone) (D-031).
- 243 tests (1 non concluant à dessein). Commits `ba228b1` et `ce89ec8`, poussés sur `main`.
- Après coup : `0b23a0d` (poussé) rétablit trois réglages URP modifiés par le build sans le vouloir
  (préfiltres de shaders de `PC_RPAsset`, liste de `UniversalRenderPipelineGlobalSettings`, deux références mortes de
  `DefaultVolumeProfile`) et prévient la branche `art` dans ART-HANDOFF ; occlusion ambiante coupée pour les mains
  (taches sombres 21.9 % → 0.1 % des pixels des mains), validée en playtest.

**5 bis — Artefacts** (D-032) — **fait et validé en playtest** (2026-11-12) ; commit et push sur `main`.
- Système générique : un asset par artefact, pool `ArtifactPool_Default`, `RunState` (seed de la run) dans la scène,
  tirage sans doublon tant que le pool suffit, puis nouveau tour (validé).
- Barillet (`Barrel_Default`) : clic gauche / RT sans objet, tir instantané depuis les yeux (60 m), 6 coups, 12 dégâts,
  0.25 s, rechargement 1.2 s automatique ou R / Y ; inactif et rechargement annulé quand l'orbe tient un objet ; tir et
  lancer jamais au même tick ; munitions sur les chambres du barillet et sous le viseur ; traqueur insensible.
- Tir ami (`Coop_Default`, désactivé) : balles, objets lancés et repoussés ; jamais soi-même ; PV du joueur
  provisoires (`PlayerHealth_Default`, 100, réapparition). Découvert : Unity n'envoie pas `OnCollisionEnter` à un
  objet qui percute un `CharacterController` → contact testé à la main (`ImpactDamager.FixedUpdate`).
- Mesuré en Play Mode : temps pour tuer 2.95 s (9 balles), cadence 0.250 s, 100 % de touches à 10 / 30 / 60 m à l'arrêt
  et en bunny hop à 18 m/s (écart vu / balle 10 cm en moyenne), coût 0.015 ms par image. Pas de tir au sud de la zone
  Télékinésie (cibles à 10, 30 et 60 m). 255 tests (1 non concluant à dessein). Checklist :
  `docs/design/features/artefacts/testing.md`.

**Prochaine étape : 6, ennemis et vagues, avec le traqueur** (voir CLAUDE.md : rôde, tue en un coup, enragé à la fin
du timer, intuable).

**Note pour plus tard (lustre) :** le lustre doit pouvoir tomber **à distance** — traction sèche, objet
lancé dessus, ou chaîne visée — pour s'en servir contre les ennemis et contre le boss final (combat
final par l'environnement, D-008). Aujourd'hui, il ne se décroche qu'en l'attrapant.

L'étape 3 (orbe de magnétisme) est **terminée et validée en playtest** (2026-11-12).

## Feuille de route

Ordre de CLAUDE.md, une étape validée en playtest avant la suivante :
3. Magnétisme (fait) → 4. Télékinésie, une seule orbe, dégâts (fait) → 5. Vue FPS et HUD (fait) →
**5 bis. Artefacts** (fait, playtest en attente, D-032) → **6. Ennemis et vagues, avec le traqueur** (rôde, tue en un
coup, enragé à la fin du timer, intuable : exclu de la répulsion, de l'étourdissement, des dégâts de zone, des balles
et de la prise) → 7. Âmes et économie →
8. Run complète → 9. Écran partagé → 10. Direction artistique (dont les **vrais sons**, voir
ci-dessous) → 11. Coop en ligne → 12. Build.

**Piste parallèle : direction artistique (étape 10)**, menée par une deuxième personne depuis le 2026-11-12 :
branche `art` du même dépôt, uniquement `Assets/Art/` et `docs/art/` (plus sa ligne du manifeste), ses propres
scènes, fusion par pull request (règles dans CLAUDE.md, « Piste DA en parallèle »). Pipeline : Higgsfield (image
concept → Tripo P2 ou Tripo H3.1 multivues → Meshy pour le squelette et l'animation) → Blender → Unity ; droits
commerciaux vérifiés et notés au manifeste. Mesures, budgets, style et livraison : `docs/art/ART-HANDOFF.md` (à
tenir à jour quand un réglage qui y figure change). Première livraison attendue : le guide de style.

**Sons provisoires :** les sons actuels sont synthétisés par le code (`ProceduralSfx`). Ils seront
remplacés par de vrais sons à l'étape 10 (skill `audio-design`), avec la même liste d'événements :
début de traction, bourdonnement de traction (hauteur selon la vitesse), fin de traction,
répulsion, refus, charge revenue / répulsion prête.

## Fait

- 2026-11-12 — **Orbe de magnétisme (étape 3), terminée et validée en playtest.**
  - Partie 1, cœur (v3) : traction linéaire continue (clic gauche / RT maintenu, 60 m, 80 m/s² jusqu'à
    30 m/s, 2.5 s max) sur 3 charges d'attraction (1 toutes les 3 s), répulsion instantanée (clic droit
    / LT) à charge unique (40 m, 5 s), toute surface, aucune charge de mouvement, aucune perte d'élan,
    traction hors plafond commun ; HUD des charges ; zone Magnétisme (F10) et grande salle de 45 m (F11).
  - Partie 2, retour visuel et sonore : bloom à seuil 1, lueur de l'orbe, du trait, de l'ancre et des
    éclats ; viseur à deux anneaux ; refus en rouge dans le HUD ; sons synthétisés provisoires ;
    réglages dans `MagnetismFeedback_Default`.
  - 155 tests ; mesures en Play Mode ; captures de la vue Game. Détail : `docs/design/features/orbes/`.
  - Trois itérations du cœur : impulsion sur surfaces magnétiques (D-020) → traction continue sur toute
    surface, sans charges de mouvement (D-021) → 3 charges d'attraction et répulsion unique (D-021 mise
    à jour). Artefacts documentés (D-022).

- 2026-11-11 — Vérification de l'environnement (étape 0) :
  - Structure OK : Assets/, Packages/, ProjectSettings/ ; kit à la racine (29 skills,
    settings.json, .mcp.json, docs/, LICENSES/) ; aucun .meta orphelin du kit.
  - Disque C: = SSD NVMe (WD Blue SN570).
  - Unity mis à jour de 6000.3.1f1 vers **6000.3.28f1** (correctif sécurité Brotli
    CVE-2026-111497, corrigé à partir de 6000.3.27f2). Ouverture sans erreur.
  - Packages : URP 17.3.0 (actif), Input System 1.20.1 (seul actif), **Cinemachine 3.1.7**
    (ajouté), Test Framework 1.6.0, Pipeline 1.0.0, AI Navigation 2.0.16. Compilation OK.
  - Visible Meta Files + Force Text. CLI Unity 1.0.0-beta.13 (beta.17 disponible).
  - Git 2.56 + LFS 3.8.1 ; dépôt sur `main`, LFS activé, .gitignore et .gitattributes ;
    remote `origin` = https://github.com/SergioMiGuPr/ItHunts.git ; premier commit
    « Projet Unity + kit Claude Code ».

- 2026-11-11 — Brief (étape 1) : 16 questions ouvertes répondues, `docs/design/BRIEF.md` mis à
  jour ; `docs/design/GDD.md` écrit, avec les notes de conception mouvement, orbes et boss
  traqueur dans `docs/design/features/` (vérificateur game-docs : 0 erreur). GDD 0.2 validé
  par l'utilisateur (D-008 à D-011).

- 2026-11-11 → 2026-11-12 — **Mouvement de base (étape 2), terminé.** Détail :
  `docs/design/features/mouvement/` (design, implementation, testing).
  - Simulation pure à 60 Hz (`MovementMotor`), CharacterController, `PlayerInput` (clavier-souris
    et manette), caméra Cinemachine 3 avec FOV selon la vitesse, réglages dans `MovementSettings`
    et `CameraSettings`, debug F3 / Select, HUD des charges sous le viseur, téléportation de debug
    F4 (départ) et F5-F9 (zones).
  - Course, saut (coyote, buffer, hauteur variable), contrôle aérien façon CPMA limité à 100°,
    auto-bunny hop, 2 sauts (D-012, D-014).
  - Glissade qui conserve l'élan, accroupi, passage bas (D-013).
  - Wall slide sans perte (détection latérale, effet wall run) et wall jump façon Deadlock (D-016).
  - Charges de mouvement partagées (saut en l'air, dash, puis magnétisme) et dash progressif avec
    dash-jump (D-015, D-017, D-018) ; plafond commun des gains à 20 m/s, bonus de glissade inclus
    (D-019).
  - Scène `Test_Movement` de 400 × 400 m (zone ouverte, ligne droite de 380 m, pentes, grands murs,
    cheminée, écarts, verticalité), générée par *ItHunts > Reconstruire…*.
  - 109 tests EditMode ; mesures en Play Mode avec le vrai CharacterController à chaque correction.

## Décisions

Le détail est dans `docs/design/GDD.md` §Decisions (D-001 à D-023).

- Moteur : Unity 6.3 LTS (6000.3.28f1), URP.
- Vue première personne, deux orbes (magnétisme, télékinésie), roguelite.
- 4 zones (cave, château, forêt, finale) ; 20 min de farm par zone avec un timer, sortie
  anticipée possible.
- Boss traqueur unique : rôde, tue en un coup, intuable avant la zone finale, plus rapide que la
  course.
- Mouvement façon Deadlock : la vitesse se gagne et se garde ; la limite vient des sauts et des
  charges ; un seul plafond pour tous les gains (20 m/s).
- Dash et saut en l'air partagent les charges de mouvement ; le magnétisme a ses propres charges
  (3 charges d'attraction, répulsion unique, D-021) ; la télékinésie n'a pas d'endurance (D-024).
- **Une seule orbe** (D-024) : traction, répulsion, attraper / relâcher et lancer en même temps, chacun sur
  sa touche ; couleur de l'orbe selon l'action.
- Artefacts : 1 par joueur et par run, tiré au hasard, sans doublon en coop, en main gauche ;
  premier : le barillet (D-022).
- Architecture prête pour l'écran partagé et la coop en ligne (voir CLAUDE.md).

## Valeurs de réglage validées en playtest

Mouvement de base validé le 2026-11-12 avec les valeurs de `Assets/_Project/Settings/Movement_Default.asset`
et `Camera_Default.asset` (la note de conception en fait foi) :

- **Sol :** course 7.5 m/s, accélération 10, friction 6, vitesse d'arrêt 2.5 m/s.
- **Saut :** 1.3 m, gravité 20 m/s², chute ×1.5, coupe 0.5, coyote 0.10 s, buffer 0.12 s (« un
  peu flottant », accepté). Saut en l'air 1.6 m ; 2 sauts ; auto-bunny hop activé.
- **Air :** accélération 10, plafond 1 m/s, contrôle aérien 0.15 (jusqu'à 100°).
- **Glissade :** dès 6 m/s, bonus +2 m/s (recharge 0.8 s), décélération 3 m/s², 5 m/s minimum sous
  un obstacle, sortie sous 4 m/s ; accroupi 3.5 m/s, capsule 0.9 m.
- **Murs :** wall slide (chute 3 m/s, 0.5 m/s en wall run de 8 à 15 m/s, tolérance 75°, dès 3 m/s
  le long du mur) ; wall jump 1.8 m, impulsion 7.5 m/s, direction 0.5, bonus +1 m/s, wall-coyote
  0.1 s, verrou 0.15 s ; le mur recharge les sauts, pas les charges.
- **Charges et dash :** 3 charges, recharge 1 s au sol et 3 s en l'air ; dash 6.5 m en 0.3 s
  (profil progressif), sortie ≥ 12 m/s, +1 m/s au sol, aucun gain en l'air, dash-jump +3 m/s
  (fenêtre 0.15 s), délai 0.3 s, mémorisation 0.3 s, 0.2 s sans friction après.
- **Plafonds :** gains 20 m/s ; sécurité 50 m/s. Pic mesuré en bunny hop : 26 m/s.
- **Caméra :** FOV 70° (+12° de 8 à 20 m/s), souris 0.1°/point, manette 270 / 180 °/s (courbe 2).

Magnétisme validé le 2026-11-12 (v3) avec `Assets/_Project/Settings/Magnetism_Default.asset` :

- **Traction (attraction) :** portée 60 m, 80 m/s² jusqu'à 30 m/s, 2.5 s max, arrivée à 1 m ;
  3 charges d'attraction, 1 toutes les 3 s (passé à **4 s** par D-024, à valider en playtest) ; jamais
  de perte, hors plafond commun.
- **Répulsion :** 14 m/s, portée 40 m, charge unique rechargée en 5 s ; gain dans le plafond commun.
- **Commun :** toute surface ; aucune charge de mouvement ; mémorisation d'un appui 0.1 s.

Télékinésie et orbe unique validées le 2026-11-12 (D-024 à D-027), `Telekinesis_Default.asset`,
`Magnetism_Default.asset`, `Damage_Default.asset`, `TrainingDummy_Default.asset` :

- **Visée du lancer :** toujours vers le point sous le viseur au clic (rayon depuis les yeux au début du tick) ;
  vitesse de lancer 30 m/s, plus la part de la vitesse du joueur dans la direction du lancer (jamais négative).
  Mesuré : écart latéral au plus 0.11 m à 10, 20 et 30 m, à l'arrêt, de côté, en bunny hop et en traction.
- **Tir tendu :** sans gravité pendant 0.5 s (≈ 15 m) ou jusqu'au premier choc ; gravité des objets 20 m/s².
- **Onde de répulsion :** rayon 4 m ; 16 m/s pour un objet de 15 kg ou un ennemi de 80 kg, facteur de masse de 0.35
  à 1.3, 50 % au bord, montée 0.25 ; un objet accroché est décroché et tombe droit.
- **Étourdissement contre un mur :** à partir de 6 m/s d'impact, 1.5 s ; mesuré : 10.3 et 12.3 m/s → étourdi.
- Aussi en place (non revus un à un en playtest) : aide à la visée de la prise (8° → 3° à 60 m), dégâts d'impact
  (masse × (vitesse − 8) × 0.1, seuil de chute 3 m/s), zone (1 m + 0.06 m/kg, 60 % / 25 %), durabilité
  (1 / 2 / 3 / 5 lancers), lustre fragile (zone 4 m).

## Problèmes connus / à faire

- **Équilibrage ouvert, à traiter quand les goules et les volants auront leurs PV (pas avant) :**
  - dégâts d'un ennemi lancé : 176 pour 80 kg à 30 m/s (un factice de 100 PV meurt, sa cible aussi) ;
  - plafond du bonus de vitesse au lancer (20 m/s) ;
  - force de la répulsion (22 m/s, 35 m/s², 14 m) ;
  - vitesse de la traction (30 m/s), peut-être à augmenter : **à régler à l'étape 6, en même temps que la vitesse du
    traqueur**, car la traction n'a pas de plafond (avec les autres points ci-dessus) ;
  - barillet (12 dégâts, 0.25 s, 1.2 s) face aux goules et aux volants.
- **Validés au playtest du 2026-11-12 :** dégâts contre un mur, objets repoussés, rendu des mains, occlusion ambiante,
  barillet (recharge, prise pendant une recharge, précision).
- **Tir ami :** réglage `Coop_Default.friendlyFire` (désactivé) en place (D-032) ; les PV du joueur sont provisoires
  (réapparition au départ, la vraie mort vient à l'étape 8). À tester pour de vrai à l'étape 9 (écran partagé).
- **Étape 8, point ouvert : PV du joueur, mort et réapparition.** Aujourd'hui : 100 PV provisoires (validés pour les
  tests), réapparition au point de départ après 1 s (`PlayerHealth_Default`). À décider à l'étape 8 : PV définitifs,
  soins rares, écran de bilan et relance (BRIEF), réanimation en coop. Le test du traqueur qui tue en un coup est prêt
  (`StalkerTests`, ignoré jusqu'à l'étape 6).
- **Coop en ligne (étape 11) :** la détection maison du contact objet-joueur (`ImpactDamager.FixedUpdate`) et le tir
  ami devront être calculés côté autorité réseau.
- **Constructeur de la scène :** il ne touche plus aux renderers des packages (il modifiait celui d'URP, en lecture
  seule) et réutilise les passes des mains existantes (sinon chaque reconstruction changeait les identifiants dans
  `PC_Renderer` et `Mobile_Renderer`, partagés avec la branche `art`).
- **Unity :** un `GlobalKeyword.Create` dans un initialiseur statique d'une passe de rendu fait planter l'éditeur au
  rechargement du code (« not allowed during serialization ») ; le créer dans `Create()`. Après ce plantage, URP
  restait cassé (« Blitter is already initialized ») jusqu'à la correction du code et un redémarrage.
- **Tests :** un `CharacterController` créé dans une scène de prévisualisation reste dans la physique de la scène
  principale (les rayons du test ne le touchent pas) ; ajouter une capsule ordinaire au joueur simulé.
- **Mesures à 2 joueurs :** le `PlayerInput` d'un 2e joueur instancié à la main lit la même souris : couper son
  `PlayerInputReader` pendant une mesure.

- **Menu d'options** : l'intensité du mouvement des mains n'a pas encore d'écran (inspecteur du joueur, PlayerPrefs).
- Mesures en Play Mode par la CLI : une boucle d'attente trop courte laisse un script tourner ; attendre le `#fin`
  du journal avant d'arrêter le Play Mode. Un 2e joueur temporaire : couper son `AudioListener` (sinon avertissement
  à chaque image) ; sa caméra virtuelle n'est pas trouvée par `GetComponentInChildren` (P2 sans suivi, assez pour
  mesurer).

- **Point ouvert, à rééquilibrer après des runs complètes (pas avant) :** plafond du bonus de vitesse au lancer
  (20 m/s par défaut, `TelekinesisOrbSettings.throwSpeedBonusCap`, ajouté à l'étape 5).
- **À revérifier au prochain playtest :**
  - gravité des objets à 20 m/s² (en place, D-027) ;
  - exclusion du traqueur de la répulsion, de l'étourdissement, des dégâts de zone, des balles et de la prise : **le
    traqueur n'existe pas encore** (étape 6) ; tous ces systèmes respectent déjà `Damageable.ImmuneToOrbs`
    (`RepulsionWave`, `EnemyBody`, `ImpactDamager.ApplyArea`, `BarrelShooter`, prise) : le traqueur devra l'avoir ;
  - immunité de 2 s après un étourdissement : faite à l'étape 5 (`TrainingDummySettings.stunImmunity`).

- **Gravité :** objets à 20 m/s² comme le joueur (D-027). Unity n'enregistre pas `Physics.gravity` changée par
  la CLI : modifier `ProjectSettings/DynamicsManager.asset` et la valeur en mémoire.
- **Proposé :** tir tendu de 1 s au lieu de 0.5 s (avec la gravité à 20, l'objet retombe de 1.8 m à 30 m).
- **Objet tenu qui percute un ennemi** : il blesse (« tout impact »), effet bélier en traction : à juger en playtest.
- **Mesures en Play Mode :** choisir les points de départ sur le sol de la scène (au sud de la zone Télékinésie,
  au-delà de z = −40, il n'y a plus de sol). Les messages de collision (`OnCollisionEnter`) ne partent pas en mode
  édition : les dégâts réels se vérifient en Play Mode.
- **Ne pas recompiler pendant le Play Mode** d'une mesure (la recompilation attend et la CLI expire) : arrêter le
  Play Mode d'abord (`unity command editor.playmode stop`).

- **Écran partagé (étape 9) :** la lueur des objets attrapables suit un seul joueur (celui dont la vue
  est mise à jour en dernier) ; à rendre propre à chaque caméra.
- **Réseau (étape 11) :** les objets lancés sont des corps dynamiques locaux ; le serveur devra faire
  autorité dessus (voir CLAUDE.md).

- **Sons provisoires** (magnétisme) : synthétisés par le code, à remplacer à l'étape 10 (skill
  `audio-design`) en gardant les mêmes événements (début de traction, bourdonnement de traction (hauteur selon la vitesse), fin de traction, répulsion, refus, charge revenue / répulsion prête).

- **À traiter avec le traqueur :** un bon joueur va presque 3 × plus vite que lui (26 m/s contre
  9-11 m/s) ; pistes notées dans `docs/design/features/boss-traqueur/design.md`.
- Le HUD (IMGUI, provisoire) est vérifiable par `unity command capture game_view` (capture PNG de la vue
  Game) ; le vrai HUD viendra à l'étape 5.
- Serveur MCP Blender : échec de connexion. Pas bloquant avant l'étape DA (10).
- Editor.log : deux « Pipe is broken » du runner ILPP à la réouverture, sans effet
  (bruit connu lors des recompilations).
- Le vérificateur `check_unity_project.py` signale 4 GUID introuvables dans
  `Assets/Settings/DefaultVolumeProfile.asset` (modèle URP) : faux positif, Unity charge les
  19 composants sans manque.
- Touches clavier de l'Input System = positions physiques QWERTY : `<Keyboard>/q` est la touche marquée A
  sur l'AZERTY de l'utilisateur, `<Keyboard>/w` son Z (déplacements ZQSD).
- Tests EditMode : pas de scène additive (« untitled scene unsaved ») ; utiliser une scène de prévisualisation
  (`EditorSceneManager.NewPreviewScene`) et `PhysicsAimProbe.Scene`. Commandes CLI : `unity command test run
  --mode EditMode`, `unity command editor.playmode start` / `stop`, `unity command script eval --file`.
- En Play Mode dans l'éditeur, le clavier n'est lu que si la fenêtre Game a le focus (réglage
  Input System par défaut) : le clavier simulé par la CLI ne passe pas. Les vérifications en Play
  Mode passent donc par la simulation avancée tick par tick avec des entrées injectées.
- Mesures en Play Mode par la CLI : tous les ticks s'enchaînent dans une seule image ; il faut un
  `Physics.Simulate` entre deux ticks (mode de simulation `Script` le temps de la mesure), sinon la
  capsule retombe sur une copie figée d'elle-même à son point de départ.
  **Toujours remettre `Physics.simulationMode = FixedUpdate` dans un `finally`** : un script de mesure
  qui a planté l'avait laissé en `Script`, et les objets de la télékinésie ne bougeaient plus pendant
  le playtest (2026-11-12). `PlayerMotor` avertit maintenant au lancement si ce n'est pas `FixedUpdate`.
- Vraies entrées en Play Mode par la CLI : `InputSystem.settings.backgroundBehavior = IgnoreFocus` et
  `editorInputBehaviorInPlayMode = AllDeviceInputAlwaysGoesToGameView`, puis `InputSystem.QueueStateEvent`
  (souris, clavier), appui et relâchement sur deux images ; journal par coroutine lu dans un second eval.
- Pas fixe passé à 60 Hz (`ProjectSettings/TimeManager.asset`).
- Unity est réglé sur « recompiler et continuer à jouer » : une recompilation pendant le Play
  Mode provoque des NullReferenceException (état du joueur perdu). Proposé : « recompiler à la
  fin du Play Mode » (Preferences > General > Script Changes While Playing). Claude attend la fin
  du Play Mode avant de compiler.
