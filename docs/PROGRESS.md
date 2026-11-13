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

**6 — Ennemis, vagues et traqueur** (D-033), ordre de l'utilisateur (D-011 d'abord) :
1. **Base commune et coups des ennemis** — fait (2026-11-12) : `EnemySettings` (base des factices, de la goule et du
   traqueur), `EnemyLocomotion` (NavMesh, pas fixe, sans NavMeshAgent), `EnemySenses`, `NoiseEvents`,
   `PlayerHealth.TakeEnemyDamage` / `Kill`, poussée sans ralentissement.
2. **Goule au sol** — fait : 10 m/s, bond annoncé 0.3 s, contact 15 PV/s ; tuée avec l'environnement en Play Mode.
3. **Traqueur complet** — fait : états, vue et ouïe, prédiction 0.8 s, pointe 16 / 18 m/s, suivi 85 % plafonné, NavMesh
   large construit au lancement (`ZoneNavMesh`), raccourcis réservés (`StalkerShortcut`), coup mortel, intuable, sons
   provisoires, timer et coin « enragé ». Mesuré : course rattrapée en 3.1 s ; bunny hop 18 / 26, traction, wall jump
   + glissade : jamais ; glissade + saut qui ralentit : 12 s.
4. **Prototype M3 dans Test_Arena** — fait. **Playtest du 2026-11-12 : arène beaucoup trop petite, pas assez
   d'ennemis, traqueur pas assez géant** → D-034 :
   - **Traqueur géant** (14 × 7 m) qui **attrape à la main** (15 m depuis l'épaule, annonce 0.5 s, recharge 2.5 s, mort
     quels que soient les PV), contact mortel, abris = passages étroits et bas seulement (4.5 m de dégagement), il
     contourne pour couper la sortie, perception de proximité, vue et sons à 60 m, tremblement de caméra à moins de
     25 m (confort), suivi de vitesse à 0.
   - **Nouvelle Test_Arena** : arènes de 100 / 90 / 90 m (plafonds 35 / 40 / 32 m), couloirs de 12 × 18 m (pente, sinueux,
     raccourcis à grilles) et tunnels de 4 × 4 m à petites portes, 3 repaires, 10 goules et 21 objets par arène.
     Dimensions réglables : `Assets/_Project/Settings/Editor/ArenaLayout_Test.asset`.
   - Mesuré : portée de la main sur 6 547 points, aucun hors de portée (14.8 m au plus), aucun abri dans les arènes ;
     dash qui esquive 20/20 à l'arrêt, en course, en bunny hop, sur la plateforme de 25 m (sans réaction : mort 5/5) ;
     lignes droites de 104 à 127 m (6.8 s au-dessus de 18 m/s dans A) ; 30 goules en chasse : 0.22 ms par pas fixe.
   - **Playtest en attente** (rien de commité). Checklist : `docs/design/features/boss-traqueur/testing.md`. 271 tests
     (1 non concluant à dessein).
   - **Playtest D-034 → D-035 et D-036** (« tu n'es en sécurité nulle part ») : fait le 2026-11-12, **playtest en
     attente, rien de commité** :
     - **Goules** : savent toujours où sont les joueurs (le plus proche par le chemin), encerclement (6 m), plus de poste ;
       **murs** (bloc 5 avancé) : liens `ClimbLink`, mode Mur, bond depuis un mur, une corniche ou un pont.
     - **Traqueur façon Pac-Man** : poursuite continue après la grâce, jamais de demi-tour (sauf cul-de-sac, ou Enragé),
       droit dessus quand le joueur est proche et devant, se baisse jusqu'à 9 m (×0.6), passe le bras par les ouvertures
       des pièces (15 m), portée depuis là où il tient (penché au bord), visée corrigée 0.15 s, cible la plus proche par
       le chemin toutes les 5 s au plus. `MazeGraph`, `ReachRoom`, `NotWalkable`, `ClimbLink`.
     - **Test_Arena en labyrinthe** (≈ 260 × 260 m, 9 intersections + repaire, 12 couloirs de 14 × 19 m en boucles, niveau
       haut à +16 m, pentes de 14 à 16°, ponts et poutres à 10 m, 6 pièces, 76 goules, 86 objets) ; tunnels et grilles
       supprimés. Captures : vue de dessus, traqueur baissé sous un pont, bras par une porte.
     - **Traction plafonnée à 18 m/s** sans jamais freiner (D-036).
     - Mesuré : 7 893/7 893 points à portée ; 9 passages bas franchis (0.5 à 0.7 s perdues) ; course rattrapée en 6.9 s
       (ligne) et 13.5 s (boucle), bunny hop et traction pas rattrapés en ligne droite, bunny hop rattrapé en 35 s dans
       la boucle ; Enragé : traction 8 à 12.5 s, traction + bunny hop 26 s dans la boucle (demi-tour) ; 0 demi-tour
       interdit ; goules : pont 6–8 s, salle haute 9.9 s, bond d'en haut esquivé 20/20 par un dash pendant le vol (0/20
       pendant l'annonce) ; 76 goules : 0.55 ms par pas fixe. 277 tests (1 non concluant à dessein).
     - Checklist : `docs/design/features/boss-traqueur/testing.md` (et goule, orbes).
   - **Playtest D-035 → D-037 et D-036 mise à jour** (2026-11-12), **playtest en attente, rien de commité** :
     - **Traqueur, mains levées** au-dessus de la tête (cylindre 10 m, jusqu'à 12 m au-dessus de la tête, trajectoire
       prédite 0.6 s ; annonce 0.2 s, zone mortelle 1 s, recharge 1.5 s, séparée de la prise) ; **corps, bras et mains
       mortels au contact** (modèle couvert à 100 %). Mesuré : **0 passage sur 200 essais**.
     - **Traction** : recharge d'une charge à 6 s (durée 2.5 s et plafond 18 m/s gardés).
     - **Goules** : murs grimpables partout (corniches tous les 5 m, ponts) et **murs de 16 m** au bout des voies au sol des
       couloirs en pente (16 m : pente 8 m + voie 8 m) ; salle haute en 6.8 s par le mur contre 9.7 s par la pente ;
       **25 en chasse au plus**, relais par la plus proche par le chemin (0.31 s) ; coût 0.34 ms par pas fixe.
     - 278 tests (1 non concluant à dessein). Checklist : `docs/design/features/boss-traqueur/testing.md`.
     - **Playtest du 2026-11-12 : traqueur beaucoup mieux, le reste bon** → D-038 : Enragé qui accélère sur la même cible
       (11 → 18 m/s en 30 s, paliers annoncés), aggro des goules (20 m en vue, 8 m, total 40, rendormie à 50 m / 5 s), murs
       grimpables automatiques (191 liens, 73 ms) ; passage à côté du traqueur mesuré quasi impossible (0/60 en 40 m, 4/60 en
       80 m) → trois pistes proposées (question ouverte dans boss-traqueur/design.md). 279 tests (1 non concluant à dessein).
     - **Playtest du 2026-11-12 : D-038 validé** (Enragé qui accélère, aggro, escalade automatique, mains levées, Pac-Man,
       pièces, traction à 6 s). Passage à côté : option (c) → **main plantée** (D-039) : 1.5 s après une prise ratée, sans
       nouvelle prise, rotation ×0.4, sans pointe. Mesuré : salle de 80 m 20/20 en bunny hop et en traction, salle de 40 m 3/20
       en bunny hop mais 20/20 en traction (plus facile que voulu, pistes dans boss-traqueur/design.md), couloir 0/60. Mur de
       16 m : 6.2 s contre 9.8 s par la pente. 280 tests (1 non concluant à dessein). **Main plantée : playtest en attente.**
     - Commit et push sur `main` : étape 6, blocs 1 à 5 (D-033 à D-039).
5. Goule sur les murs (fait et validé, D-035, D-037, D-038).
6. **Volant** (D-040) — fait le 2026-11-13, **commité sur `main` sans playtest complet (accord de l'utilisateur)** : poursuite (8 en chasse,
   aggro jusqu'à 12), ronde 8 m, piqué annoncé 0.4 s à 14 m/s (8 PV), attrapable à tout moment et libéré au bout de 2 s,
   éclatement de 3 m (30 → 7.5) quand il est lancé, chute étourdie après une répulsion, 4 par grande salle. Mesuré : prise
   en vol 120/120, 3 goules : 66 dégâts (combo répulsion → prise → lancer : 72 en 1.72 s), piqué esquivé 20/20, coût 0.18 ms.
   Main plantée passée à ×0.6 : la salle de 40 m reste à 20/20 en traction (×0.8 et ×1 aussi) → pistes proposées.
   282 tests (1 non concluant à dessein). Checklist : `docs/design/features/volant/testing.md`.
   - **Corrections du 2026-11-13, sans playtest** (vérifiées au prochain : checklist F12 de `volant/testing.md`) :
     - **Volant tué au premier impact de projectile** (`dieOnProjectileHit`) : balle, objet lancé ou repoussé, ennemi lancé, mur ;
       pas par une zone ni par l'onde seule. **Zone de touche** : ailes en déclencheurs `Hitbox` (les balles touchent désormais les
       déclencheurs qui ont un `Hitbox`, et eux seuls). Mesuré : 200/200 points du modèle, 0/120 à côté ; une balle le tue à 10, 30
       et 60 m (bunny hop 60/60).
     - **Piqué à visée corrigée** : 80°/s (90 demandé : 38 % en course), sans avance (`diveLead` 0 ; 0.3 : 68 %) ; course 32/100,
       bunny hop 0/50, traction 50/50 ; dash tard 0/20 touché, dash pendant l'annonce ou tôt 20/20 touché.
     - **Dash** : recharge d'une charge +1 s (sol 1 → 2 s, air 3 → 4 s, D-015 mise à jour), saut en l'air ralenti aussi.
     - **Main plantée** (D-039) : piste (b) (corps à vitesse normale, rotation ×0.4) insuffisante (40 m traction 20/20), puis piste
       (a) : **revers de la main gauche** (7 m du bord du corps, délai 0.5 s, annonce 0.2 s, coup 0.25 s). Mesuré : 80 m 20/20 en
       bunny hop et en traction, 40 m 0/20, couloir 0/60.
     - 286 tests (1 non concluant à dessein).
7. Vagues — à faire (plan court envoyé, en attente de réponse).

**Notes pour plus tard (ne pas coder maintenant) :**
- **Bloc 7, vagues :** le nombre de goules augmente avec le temps passé dans la zone : peu au début, de plus en plus
  jusqu'au timer, plus encore en Enragé (le plafond de goules en chasse, D-037, pourra suivre la même courbe).
- **Étape 8 et level design :** point de départ de la zone plus sûr (moins d'ennemis à proximité, loin du repaire du
  traqueur). Test_Arena reste un banc d'essai ; le vrai level design viendra plus tard.

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
- **Charges et dash :** 3 charges, recharge 1 s au sol et 3 s en l'air (**2 s et 4 s depuis le 2026-11-13**, D-015 mise à jour,
  à revoir au playtest) ; dash 6.5 m en 0.3 s
  (profil progressif), sortie ≥ 12 m/s, +1 m/s au sol, aucun gain en l'air, dash-jump +3 m/s
  (fenêtre 0.15 s), délai 0.3 s, mémorisation 0.3 s, 0.2 s sans friction après.
- **Plafonds :** gains 20 m/s ; sécurité 50 m/s. Pic mesuré en bunny hop : 26 m/s.
- **Caméra :** FOV 70° (+12° de 8 à 20 m/s), souris 0.1°/point, manette 270 / 180 °/s (courbe 2).

Magnétisme validé le 2026-11-12 (v3) avec `Assets/_Project/Settings/Magnetism_Default.asset` :

- **Traction (attraction) :** portée 60 m, 80 m/s² jusqu'à 30 m/s (**18 m/s depuis D-036**), 2.5 s max, arrivée à 1 m ;
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
  - barillet (12 dégâts, 0.25 s, 1.2 s) face aux goules et aux volants.
- **Validés au playtest du 2026-11-12 :** dégâts contre un mur, objets repoussés, rendu des mains, occlusion ambiante,
  barillet (recharge, prise pendant une recharge, précision).
- **Tir ami :** réglage `Coop_Default.friendlyFire` (désactivé) en place (D-032) ; les PV du joueur sont provisoires
  (réapparition au départ, la vraie mort vient à l'étape 8). À tester pour de vrai à l'étape 9 (écran partagé).
- **Coop, traqueur :** il vise le joueur le plus proche par le chemin, au plus un changement toutes les 5 s (D-035 ; prêt,
  testé en solo seulement). Mort : la zone
  se remet à zéro quand tous les joueurs sont morts (réanimation : étape 9 / 11).
- **Traqueur, prise à la main :** visée corrigée 0.15 s avant la descente (D-035) ; encore trop facile ? À juger au playtest.
- **Traction (D-036) :** à 18 m/s, la durée max de 2.5 s limite une traction à ≈ 43 m (55 m avant) ; durée gardée par
  l'utilisateur (2026-11-12), recharge d'une charge passée à 6 s.
- **Goules (D-035) :** bond d'en haut esquivable seulement pendant le vol ; salles hautes atteintes par les pentes (pas
  de mur direct du sol vers +16 m : ajoutés par D-037) ; 76 goules, 25 en chasse au plus (D-037).
- **Mesures du volant :** dans une salle de mesure temporaire (140 × 140 m, loin du labyrinthe) ; les coins de la salle SE supposés
  au départ étaient hors du sol (le joueur tombait). Un script qui désactive des objets (traqueur, volants) les rend introuvables pour
  le script suivant : chercher avec `FindObjectsInactive.Include` ou relancer le Play Mode.
- **Mesures dans le labyrinthe :** un traqueur qui démarre au milieu d'une intersection prend son dos comme couloir
  d'arrivée (corrigé) ; la traction a une portée de 60 m (viser plus près dans les scripts).
- **`ProjectSettings/NavMeshAreas.asset` :** chaque Play de Test_Arena avance `m_LastAgentTypeID` (types d'agent créés
  à l'exécution par `ZoneNavMesh`) ; sans effet : ne pas commiter ce seul changement (le rétablir avec `git checkout`).
- **Unity (étape 6) :** les types d'agent de NavMesh créés à l'exécution ont des identifiants **négatifs** (ne pas
  tester `< 0`) ; un `NavMeshPath` ne se crée pas dans un initialiseur de champ d'un MonoBehaviour ; dans un
  constructeur de scène, `EditorSceneManager.NewScene` décharge les assets chargés avant (les recharger après) ; dans
  les scripts de mesure, viser après une téléportation seulement une image plus tard (la caméra suit en LateUpdate).
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
