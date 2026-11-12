# Avancement

> Mémoire du projet entre les sessions. Claude la met à jour à la fin de chaque étape.

## Étape en cours

**3 — Orbe de magnétisme** (prochaine étape, pas encore commencée) : attirer et repousser,
consommation des **charges de mouvement** (1 par usage, D-015), recharge, retour visuel et sonore.
Partir de `docs/design/features/orbes/design.md` ; proposer d'abord un plan (architecture de
l'orbe, états Prête → EnUsage → Recharge, surfaces magnétiques qui brillent, ajouts à
`Test_Movement`) avant de coder.

L'étape 2 (mouvement de base) est **terminée et validée en playtest** (2026-11-12).

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

Le détail est dans `docs/design/GDD.md` §Decisions (D-001 à D-019).

- Moteur : Unity 6.3 LTS (6000.3.28f1), URP.
- Vue première personne, deux orbes (magnétisme, télékinésie), roguelite.
- 4 zones (cave, château, forêt, finale) ; 20 min de farm par zone avec un timer, sortie
  anticipée possible.
- Boss traqueur unique : rôde, tue en un coup, intuable avant la zone finale, plus rapide que la
  course.
- Mouvement façon Deadlock : la vitesse se gagne et se garde ; la limite vient des sauts et des
  charges ; un seul plafond pour tous les gains (20 m/s).
- Magnétisme, dash et saut en l'air partagent les charges de mouvement ; la télékinésie garde sa
  propre endurance.
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

## Problèmes connus / à faire

- **À traiter avec le traqueur :** un bon joueur va presque 3 × plus vite que lui (26 m/s contre
  9-11 m/s) ; pistes notées dans `docs/design/features/boss-traqueur/design.md`.
- Le HUD des charges (IMGUI, provisoire) n'a pas été vérifié à l'écran par Claude (pas de capture par
  la CLI) ; le vrai HUD viendra à l'étape 5.
- Serveur MCP Blender : échec de connexion. Pas bloquant avant l'étape DA (10).
- Editor.log : deux « Pipe is broken » du runner ILPP à la réouverture, sans effet
  (bruit connu lors des recompilations).
- Le vérificateur `check_unity_project.py` signale 4 GUID introuvables dans
  `Assets/Settings/DefaultVolumeProfile.asset` (modèle URP) : faux positif, Unity charge les
  19 composants sans manque.
- En Play Mode dans l'éditeur, le clavier n'est lu que si la fenêtre Game a le focus (réglage
  Input System par défaut) : le clavier simulé par la CLI ne passe pas. Les vérifications en Play
  Mode passent donc par la simulation avancée tick par tick avec des entrées injectées.
- Pas fixe passé à 60 Hz (`ProjectSettings/TimeManager.asset`).
- Unity est réglé sur « recompiler et continuer à jouer » : une recompilation pendant le Play
  Mode provoque des NullReferenceException (état du joueur perdu). Proposé : « recompiler à la
  fin du Play Mode » (Preferences > General > Script Changes While Playing). Claude attend la fin
  du Play Mode avant de compiler.
