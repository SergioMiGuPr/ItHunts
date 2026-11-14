# Coop locale : testing

**Updated:** 2026-11-14 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

- [x] Réglages : à terre 30 s, relevé par 3 s à 2 m, 50 PV ; pas de tir ami ; gauche / droite ; 2 joueurs. *(`Assets/_Project/Tests/EditMode/CoopTests.cs` › Reglages_AterreTrenteSecondes_RelevePar3sA2m_50PV)*
- [x] Seul, la mort tue ; en coop, à terre ; le dernier debout meurt. *(CoopTests › Seul_LaMortTue_EnCoop_ATerre)*
- [x] Réanimation : rien sans maintenir ; 1.5 s pas assez ; relâché, le compte repart de zéro ; 3 s, relevé à 50 PV ; à 4 m, trop loin. *(CoopTests › Reanimation_Maintien3s_A2m_Revient50PV)*
- [x] Spectateur à 30 s (pas à 29 s), plus de réanimation ; revenu à la zone suivante. *(CoopTests › Spectateur_Apres30s_RevientALaZoneSuivante)*
- [x] Fin de partie quand tous sont à terre ou morts. *(CoopTests › FinDePartie_QuandTousSontATerreOuMorts)*
- [x] Découpe gauche / droite et haut / bas. *(CoopTests › Ecran_GaucheDroite_OuHautBas)*
- [x] FOV : 1 seul, plus large en gauche / droite, plus serré en haut / bas. *(CoopTests › Fov_AdapteALaMoitieDEcran)*
- [x] Le niveau 1 accueille un 2e joueur. *(CoopTests › Scene_Niveau1_EcranPartage)*
- [x] En Play Mode (Level_01_Base), **2e joueur** (`Join`) :
  - écran découpé ;
  - joueur 1 verrouillé sur clavier-souris ;
  - chaque cerveau Cinemachine sur son canal, chaque caméra au bon joueur ;
  - chacun voit la capsule de l'autre, mais pas la sienne.

  *(Play Mode, 2026-11-14 ; captures `coop_ecran_partage.png`, `coop_a_terre.png`)*
- [x] En Play Mode, **à terre** :
  - le traqueur l'ignore ;
  - bandeau dans sa moitié ;
  - capsule couchée et étiquette pour l'autre ;
  - relevé en 3 s par le joueur 1 tenu à côté.

  *(Play Mode, 2026-11-14)*
- [x] En Play Mode, **spectateur** : la moitié du joueur à terre montre la vue de l'allié, avec le bandeau ; tous à terre →
  écran de fin. *(Play Mode, 2026-11-14 ; capture `coop_spectateur.png`)*
- [x] En Play Mode, lueurs des objets : toujours allumées en solo (200 sur 200). Elles sont maintenant calculées par caméra.
  *(Play Mode, 2026-11-14)*
- [ ] Ressenti à deux, avec une vraie manette (playtest de l'utilisateur).

## Performance (écran partagé)

Mesures au pic de la fin du niveau 1 : joueur au centre, environ 210 ennemis actifs, 1920 × 1080. La machine était chargée
pendant les mesures, il y a du bruit d'un essai à l'autre.

| Cas | Temps d'image médian |
|---|---|
| Build de développement, solo | 13.4 ms |
| Build, écran partagé, Level_01 | 18.3 ms |
| Build, écran partagé, distance de vue 250 m | 17.2 ms |
| Build, écran partagé, lumières ponctuelles coupées | 15.1 ms |
| Éditeur, solo | 30.1 ms |
| Éditeur, écran partagé | 39.4 ms |

**Objectif de 16.6 ms non atteint en écran partagé : 17 à 18 ms.** Les postes les plus lourds :
- les 1 600 scripts en FixedUpdate, environ 4 à 6 ms ;
- le rendu, doublé par la 2e caméra ;
- les lumières : environ 3 ms rien que pour le culling et l'éclairage par caméra.

Une torche sur deux n'éclaire plus dans les bases régénérées (1 921 → 1 297 lumières), mais **pas dans Level_01** (qui ne doit
plus être recopiée). La mesure faite après ce changement, sur une machine plus chargée, n'est pas comparable. La liste
d'optimisations est dans PROGRESS.md.

## Notes

- Les mesures et les captures utilisent `PlayerHealth.DebugInvulnerable`, une variable statique qui survit à la sortie du Play
  Mode : la remettre à false avant les tests EditMode, sinon 11 tests échouent.

## Playtest checklist (à deux)

1. **Rejoindre au menu :** au menu principal, appuie sur Start de la manette. Lance une run : deux moitiés gauche / droite,
   clavier-souris à gauche, manette à droite. Bouge la souris : seule la vue de gauche bouge.
2. **Rejoindre en jeu :** relance en solo, puis appuie sur Start de la manette en pleine partie. Le 2e joueur apparaît à côté
   du 1er.
3. **Deux manettes :** commence à la manette, puis fais rejoindre une 2e manette. Chaque manette ne pilote que sa moitié.
4. **Vues :** regardez-vous. Chacun voit l'autre (capsule bleue pour J1, orange pour J2), ses propres mains, son orbe et son
   HUD. Visez chacun une caisse différente : seule la vôtre pulse dans votre moitié.
5. **FOV :** le FOV de chaque moitié te paraît-il bon (×1.1) ? Teste aussi haut / bas (`Coop_Default.splitHorizontal`, ×0.75).
6. **Règles inchangées :** un objet lancé ou une balle ne blesse pas l'allié. Les âmes montent pour les deux. Les coffres
   s'ouvrent une fois par joueur. La porte de sortie est payée une fois. La minimap est commune.
7. **À terre :** laisse un joueur tomber à 0 PV. Il est couché et ne peut plus agir. Bandeau et compte à rebours de 30 s dans
   sa moitié ; étiquette « À terre, maintiens E / Y » vue par l'autre. Les goules et le traqueur le laissent.
8. **Relever :** maintiens E / Y à côté de lui. Relâche à mi-chemin : la barre repart de zéro. Puis 3 s : il revient à 50 PV.
9. **Spectateur :** ne le relève pas pendant 30 s. Sa moitié montre ta vue avec le bandeau « Spectateur ». Il revient au
   niveau suivant.
10. **Fin :** laisse tomber les deux joueurs. Écran de fin, Rejouer.
11. **Traqueur :** un coup du traqueur met à terre (pas de mort directe tant que l'autre est debout).
12. **Fluidité :** au pic de la fin du niveau, le jeu reste-t-il fluide à deux ? Note les endroits où ça saccade.
