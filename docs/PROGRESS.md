# Avancement

> Mémoire du projet entre les sessions. Claude la met à jour à la fin de chaque étape.

## Étape en cours

**4 — Télékinésie, puis une seule orbe** (D-023, puis D-024), en deux temps.

1. **Cœur** — fait.
   - Partie 1 (D-023) : attraper, tenir, poser, lancer (30 m/s + vitesse du joueur), objets qui brillent,
     zone Télékinésie (F12) : caisses de 5 à 60 kg, tonneaux, pile, lustre accroché. Playtest du
     2026-11-12 : retour de design ci-dessous. Dégâts selon la vitesse pas encore testés (pas d'ennemis).
   - **Une seule orbe (D-024), fait le 2026-11-12, playtest en attente :** plus d'échange ni d'orbe
     active ; les quatre actions en même temps, chacune sur sa touche : **A** (AZERTY) maintenu =
     traction, **clic droit** = répulsion, **F** = attraper / relâcher, **clic gauche** = lancer (sans
     objet : réservé à l'artefact) ; manette **RB**, **LT**, **X**, **RT**. Couleur de l'orbe selon
     l'action (lancer > répulsion > traction > objet tenu). **Plus d'endurance** (seul le délai de 0.3 s
     après un lancer). Attraction : **1 charge toutes les 4 s**. La visée du magnétisme ignore l'objet
     tenu. **Défaut corrigé :** l'objet tenu prenait un tick d'avance à grande vitesse (0.47 m à
     40 m/s) ; mesuré en Play Mode avec le vrai joueur : 0.000 m en traction à 30 m/s, 0.05 m en bunny
     hop à 20 m/s. 181 tests. Checklist : `docs/design/features/orbes/testing.md` (« une seule orbe »).
2. **Dégâts et cibles** — **prochaine étape après ce playtest** : dégâts = masse × vitesse d'impact ×
   facteur au-dessus d'un seuil, ennemis factices avec PV et chiffres de dégâts, éclats et sons
   provisoires.

**Note pour plus tard (lustre) :** le lustre doit pouvoir tomber **à distance** — traction sèche, objet
lancé dessus, ou chaîne visée — pour s'en servir contre les ennemis et contre le boss final (combat
final par l'environnement, D-008). Aujourd'hui, il ne se décroche qu'en l'attrapant.

L'étape 3 (orbe de magnétisme) est **terminée et validée en playtest** (2026-11-12).

## Feuille de route

Ordre de CLAUDE.md, une étape validée en playtest avant la suivante :
3. Magnétisme (fait) → **4. Télékinésie, une seule orbe** → 5. Vue FPS et HUD →
**5 bis. Artefacts** (système générique : tirage au hasard, pas de doublon en coop, main gauche ; puis
le barillet, D-022 ; avant ou avec les ennemis) → 6. Ennemis et vagues → 7. Âmes et économie →
8. Run complète → 9. Écran partagé → 10. Direction artistique (dont les **vrais sons**, voir
ci-dessous) → 11. Coop en ligne → 12. Build.

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

## Problèmes connus / à faire

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
