# ItHunts — Game Design Document

**Version:** 0.4 · **Updated:** 2026-11-11 · [Features](#feature-index) · [Decisions](#decisions) · [Change log](#change-log)

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
| Mouvement | in progress | MVP | Course, saut tolérant, glissade (accélère en pente), wall slide et wall jump, bunny hop façon Source | [design](features/mouvement/design.md) |
| Orbes | designed | MVP | Magnétisme (attirer / repousser) et télékinésie (attraper / lancer), échange instantané, endurance | [design](features/orbes/design.md) |
| Boss traqueur | designed | MVP | Monstre géant intuable qui rôde, tue en un coup, plus rapide que la course ; enragé après le timer | [design](features/boss-traqueur/design.md) |
| Zones et timer | idea | MVP | Arènes faites main tirées au hasard et reliées ; timer de 20 min ; sortie anticipée | — |
| Ennemis zone 1 | idea | MVP | Goule (rampe, bondit, grimpe aux murs ; attrapable étourdie) et petit volant attrapable | — |
| Âmes et économie | idea | MVP | Collecte autour des orbes, coffres, marchands, salles verrouillées | — |
| Améliorations | idea | MVP | Modifs des orbes (dont des dégâts directs), stats, modifs du mouvement, reliques passives | — |
| Vie et mort | idea | MVP | PV fixes, soins rares, bilan de fin de run et relance en un bouton | — |
| HUD et debug | idea | MVP | Endurance, recharges, âmes, timer ; affichage de debug activable | — |
| Vue FPS | idea | MVP | Mains et orbe, couleurs par mode | — |
| Méta-progression | idea | later | Déblocages achetés avec une part des âmes | — |
| Combat final | idea | later | Zone finale où le boss devient tuable par l'environnement (lustres, piliers, pièges, points faibles au magnétisme) | — |
| Coop locale | idea | later | Écran partagé à 2 | — |
| Coop en ligne | idea | later | 1 à 4 joueurs ; âmes et améliorations individuelles, réanimation | — |

## Jalons

Suivent l'ordre de travail de CLAUDE.md ; chaque jalon se termine par un playtest.
1. **M1, mouvement en greybox :** mouvement + HUD de debug dans `Test_Movement` · fini quand :
   l'utilisateur enchaîne glissade → wall jump → bunny hop sans perdre de vitesse, et valide
   les réglages.
2. **M2, orbes :** magnétisme puis télékinésie, endurance, échange · fini quand : on atteint une
   plateforme hors de portée de saut avec l'attraction, et on tue un ennemi factice avec une
   caisse.
3. **M3, premier prototype de zone :** une seule zone, **une goule et le traqueur**, timer de
   test de 4 min, sortie · fini quand : on tue la goule avec l'environnement et on atteint la
   sortie en échappant au traqueur.
4. **M3 bis, boucle d'une zone :** vagues, âmes, coffre, marchand, améliorations · fini quand :
   une zone de 3 arènes se joue du début à la sortie.
5. **M4, run complète,** puis écran partagé, direction artistique, coop en ligne, build.

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
| D-005 | 2026-11-11 | Attraction = impulsion unique, sur surfaces seulement | grappin maintenu ; cible ennemi | se combine avec le bunny hop sans l'éclipser ; simple à prédire en réseau | [orbes](features/orbes/design.md) |
| D-006 | 2026-11-11 | Méta-progression par déblocages, sans bonus chiffrés | petits bonus permanents ; rien | équité en coop entre joueurs de niveaux différents | méta-progression |
| D-007 | 2026-11-11 | Coop : âmes et améliorations individuelles ; non réanimé = retour à la zone suivante | âmes partagées ; mort jusqu'à la zone suivante sans réanimation | choix de l'utilisateur | coop en ligne |
| D-008 | 2026-11-11 | Pas de dégâts directs de base ; des améliorations peuvent en ajouter aux orbes et aux objets lancés, en plus de l'environnement. Boss final tué par l'environnement | aucun dégât direct jamais (D-004) | validation du GDD : plus de variété de builds | [orbes](features/orbes/design.md), améliorations, combat final |
| D-009 | 2026-11-11 | Phase enragée : multiplicateur d'âmes ; coop : le boss chasse le dernier joueur vu ou entendu | pas de bonus ; joueur le plus proche | validation du GDD : récompenser la prise de risque | [boss](features/boss-traqueur/design.md) |
| D-010 | 2026-11-11 | La glissade ne coûte pas d'endurance ; durée de zone réglable (4 min en test) | glissade payante ; 20 min fixes | validation du GDD : l'endurance reste aux orbes ; tests rapides | [mouvement](features/mouvement/design.md), [boss](features/boss-traqueur/design.md) |
| D-011 | 2026-11-11 | Premier prototype de zone : une zone, une goule, le traqueur | zone complète d'emblée | valider la tension de la traque avant le reste | jalon M3 |
| D-012 | 2026-11-11 | Mouvement façon Deadlock : contrôle aérien (W seul), auto-bunny hop, 2 sauts (réglable), pas de plafond bas de vitesse (sécurité à 50 m/s) ; la limite vient du nombre de sauts | Source pur ; plafond doux à 25 m/s | playtest de l'étape 1 : W en l'air partait de côté au lieu de suivre la caméra | [mouvement](features/mouvement/design.md) |
| D-013 | 2026-11-11 | Glissade façon Deadlock / Apex : élan conservé à 100 % à l'entrée, décélération constante (3 m/s²), vitesse minimale de 5 m/s sous un obstacle | friction proportionnelle à la vitesse (0.5 /s) ; fin de glissade en accroupi sous un plafond | playtest : le bunny hop perdait son élan en glissade, le passage bas finissait accroupi | [mouvement](features/mouvement/design.md) |

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
