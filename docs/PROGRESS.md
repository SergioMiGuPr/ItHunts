# Avancement

> Mémoire du projet entre les sessions. Claude la met à jour à la fin de chaque étape.

## Étape en cours

2 — Mouvement de base. Sous-étape 1 (course, saut, contrôle aérien, auto-bunny hop, 2 sauts)
**validée en playtest et commitée**. Glissade et accroupi : **validés en playtest (v2) et commités**.
Ensuite, dans l'ordre : (1) agrandir Test_Movement (zone ouverte ≥ 200 × 200 m, ligne droite de
bunny hop, longues pentes, grands murs, écarts larges, verticalité) ; (2) wall slide et wall jump
avec recharge des sauts au mur ; (3) dash et endurance de mouvement à charges (proposer d'abord
2-3 options sur le lien avec l'endurance des orbes). Chaque point : tests, checklist, playtest.

## Fait

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

- 2026-11-11 — Mouvement, sous-étape 1 (course et saut) : simulation pure à 60 Hz
  (`MovementMotor`), CharacterController, `PlayerInput` (schémas clavier-souris et manette),
  caméra Cinemachine 3 avec FOV selon la vitesse, `MovementSettings` et `CameraSettings`
  (sensibilités souris et manette), debug F3 / Select, scène `Test_Movement` générée par le menu
  *ItHunts > Reconstruire…*. Compilation sans erreur ni avertissement ; 14 tests EditMode verts
  (et vérifiés capables d'échouer) ; Play Mode vérifié par la CLI.
- 2026-11-11 — Corrections après playtest : contrôle aérien façon CPMA (`airControl` 0.15, W seul),
  auto-bunny hop (activé), sauts multiples (`maxJumps` 2), plafond de sécurité 50 m/s. 28 tests
  EditMode verts. Recharge des sauts au mur : à brancher avec la détection des murs.
- 2026-11-11 — Glissade et accroupi : touche Slide maintenue, bonus +2 m/s (recharge 0.8 s),
  accélération en pente, capsule 0.9 m avec relevé seulement s'il y a la place, saut prioritaire
  à l'atterrissage. Scène : couloir avec passage bas de 1.1 m et longue rampe de 20°. 39 tests verts.
- 2026-11-11 — Correction glissade après playtest (D-013) : élan conservé, décélération constante
  3 m/s², vitesse minimale 5 m/s sous un obstacle. 45 tests verts.

## Décisions

Le détail est dans `docs/design/GDD.md` §Decisions (D-001 à D-013).

- Moteur : Unity 6.3 LTS (6000.3.28f1), URP.
- Vue première personne, deux orbes (magnétisme, télékinésie), roguelite.
- 4 zones (cave, château, forêt, finale) ; 20 min de farm par zone avec un timer, sortie
  anticipée possible.
- Boss traqueur unique : rôde, tue en un coup, intuable avant la zone finale, plus rapide que la
  course.
- Architecture prête pour l'écran partagé et la coop en ligne (voir CLAUDE.md).

## Valeurs de réglage validées en playtest

- 2026-11-11 : course au sol validée (7.5 m/s, accélération 10, friction 6, arrêt 2.5 m/s).
  Saut accepté pour l'instant (1.3 m, gravité 20, chute ×1.5), jugé un peu flottant.
- 2026-11-11 : contrôle aérien 0.15, auto-bunny hop activé, 2 sauts, plafond 50 m/s validés.
  Pic de vitesse mesuré en bunny hop : 26 m/s.

## Problèmes connus / à faire

- Serveur MCP Blender : échec de connexion. Pas bloquant avant l'étape DA (10).
- Editor.log : deux « Pipe is broken » du runner ILPP à la réouverture, sans effet
  (bruit connu lors des recompilations).
- Le vérificateur `check_unity_project.py` signale 4 GUID introuvables dans
  `Assets/Settings/DefaultVolumeProfile.asset` (modèle URP) : faux positif, Unity charge les
  19 composants sans manque.
- En Play Mode dans l'éditeur, le clavier n'est lu que si la fenêtre Game a le focus (réglage
  Input System par défaut) : le clavier simulé par la CLI ne passe pas.
- Pas fixe passé à 60 Hz (`ProjectSettings/TimeManager.asset`).
- Unity est réglé sur « recompiler et continuer à jouer » : une recompilation pendant le Play
  Mode provoque des NullReferenceException (état du joueur perdu). Proposé : « recompiler à la
  fin du Play Mode » (Preferences > General > Script Changes While Playing).
