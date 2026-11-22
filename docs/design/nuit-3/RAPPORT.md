# Rapport de la nuit 3 (2026-11-22)

Brief : « ambition maximale », 9 points, PR d'art intégrées au fil de l'eau, run complète jouable dans `main`, build de
développement dans `Builds/Nuit_2026-11-22/`. Recherche préalable : [RECHERCHE-NUIT-3.md](../RECHERCHE-NUIT-3.md). Décisions :
D-092 à D-099 dans le [GDD](../GDD.md). Captures : [captures/](captures/).

## En bref

- **Fait** : les 9 points du brief sont dans le jeu. Les 9 PR d'art (#36 à #44) sont fusionnées dans `main` puis branchées.
  Une run se joue d'un bout à l'autre (acte I → II → III → boss → victoire), et le stuff passe d'un acte à l'autre (bug trouvé
  et corrigé ce matin, voir le point 3).
- **Pas au niveau « exceptionnel », je le dis franchement** : les actes sont jouables, avec les kits de la DA, des salles
  organiques et de la lumière, mais **pas encore au niveau d'une page Steam**. C'est encore sombre et répétitif par endroits,
  avec des aplats de matériaux sur les grands murs et peu de points de vue mis en scène (voir « Ce qui reste »). Le ressenti
  (parade, poursuite, boss) n'a été mesuré que par des sondes et des calculs : c'est à valider manette en main.
- **Tests** : 547 tests EditMode, 546 passent, 1 non concluant (déjà connu : la collision de caisse ne se vérifie qu'en Play Mode).

## PR d'art

| PR | Branche | Contenu | État | Branchement dans le jeu |
|---|---|---|---|---|
| #36 | art-materiaux | 22 tuiles peintes, trim sheet, décalcomanies | fusionnée | matériaux selon la fonction des salles (`ActKit.functionMaterials`) |
| #37 | art-kit-caves | kit de l'acte I (43 pièces) | fusionnée | parois de roche, cellules, garde, réserve, puits, porte de sortie |
| #38 | art-kit-chateau | kit de l'acte II (31 pièces, rayons de lune) | fusionnée | murs, hautes fenêtres, rayons de lune, lustres, grande porte |
| #39 | art-kit-jardin | kit de l'acte III (25 pièces, ciel, brume, arène) | fusionnée | lisières, haies, falaises, arène du boss, ciel, brume |
| #40 | art-salles-exemple | 11 salles d'exemple et leurs fiches | fusionnée | références pour le générateur (fiches de parcours) |
| #41 | art-sons-2 | 51 sons, musiques, ambiances | fusionnée | **les 51 branchés**, plus aucun emplacement vide dans `GameAudio_Default` |
| #42 | art-ui-3 | fin de run, menu de porte, choix de l'acte, admin, barre du boss, critique | fusionnée | tous branchés (`Night3ArtUi`, `PlayerHudArt`, `MainMenuArt`, `AdminPanelArt`) |
| #43 | art-vfx-3 | 12 effets | fusionnée | critique, paralysie, explosion, parade lisible, boss (griffe, morsure, impact, blessure, point faible) |
| #44 | art-fin-3 | NUIT-FIN-3.md | fusionnée | — |

Aucune PR bloquée. Pour chaque PR : périmètre vérifié (`Assets/Art/`, `docs/art/`, ligne du manifeste), compilation, tests EditMode
verts, fusion normale, push de `main`, fusion dans `animations`. Aucun achat, aucun crédit dépensé de mon côté.

## Réglages choisis

| Réglage | Valeur | Où |
|---|---|---|
| Prix de la porte de sortie | 200 âmes + 50 par acte | `Settings/Actes/Zone_Acte*.exitPrice` |
| Timer d'un acte / ouverture de la sortie | 12 min / après 240 s | `Zone_Acte*` |
| Arrivée du traqueur par la porte (actes II, III) | 60 s (période de grâce ; la herse se lève avec le son de la DA) | `Zone_Acte*.gracePeriod`, `Act_*.stalkerFromArrival` |
| Densité des goules | 0.22 / 0.24 / 0.20 par 100 m² | `Act_*.ghoulsPer100` |
| Portes entre salles | 12 m de large | `Act_*.doorWidth` |
| Chute dans le vide | −20 PV, retour au dernier sol sûr | `FallVoid` |
| Traqueur | +0.5 m/s dans tous les états | `Stalker_Default.speedBonus` |
| Headshot | ×2 | `BarrelMods_Default` |
| Parade | fenêtre 0.25 s + 0.08 s de tolérance tardive, 0.5 s d'invulnérabilité, recharge 0.7 s après un raté | `Parry_Default` |
| Boss final | 2 400 PV, phases à 66 % et 33 %, annonces de 0.8 à 1.2 s (×0.8 en phase 3, jamais sous 0.45 s), points faibles ×2.5 | `Settings/Actes/FinalBoss_Default` |

## Contrôle point par point

| # | Demande | Fait | Preuve |
|---|---|---|---|
| 1 | Portes classiques avec menu d'interaction de la DA et prix en âmes | ✅ | commit `5c2bf73`, `73b3bd1` ; capture `porte_menu_prix.jpg` |
| 1 | Plus de lumières bleues flottantes | ✅ | `5c2bf73` |
| 1 | Volants sans surbrillance violette | ✅ | `5c2bf73` |
| 1 | Améliorations visibles sur le joueur retirées | ✅ (rien de tel trouvé dans le code ni en jeu : aucune modification nécessaire) | recherche dans le code, captures |
| 1 | Sons `ProceduralSfx` retirés, remplacés par art-sons-2 | ✅ | `ProceduralSfx.Enabled = false` ; 51 sons branchés (`73b3bd1` + ce matin : impact lourd, morsure du boss) ; vérification : tout clip de `Assets/Art/Audio` est référencé, sauf l'ancien son de mort des goules (remplacé à la demande de la DA) |
| 1 | Inventaire de ce qui n'est pas dans la DA | ✅ | liste plus bas |
| 1 | Toute l'interface dans la DA (playtest compris) | ✅ | captures `menu_playtest.jpg`, `panneau_admin.jpg`, `fin_de_run_victoire.jpg` |
| 2 | Menu Playtest : Vitrine + Choisir l'acte (I, II, III, Boss final) | ✅ | `187d17e` ; `menu_playtest.jpg` |
| 2 | Panneau admin seulement depuis ce menu, toutes les commandes demandées | ✅ | `RunSession.AdminMode`, test `RunFlowTests` ; `panneau_admin.jpg` |
| 3 | 3 actes + boss, stuff gardé | ✅ | enchaînement mesuré ce matin : 300 âmes et 1 amélioration retrouvées dans les actes II, III et au boss (commit `db4c274`, test `Stuff_GardeDUnActeALAutre`) |
| 3 | Porte de sortie 200 âmes réglable | ✅ | `Zone_Acte*.exitPrice` |
| 3 | Acte II : le traqueur entre par la porte d'arrivée vers 1 min | ✅ | `ArrivalGate`, `stalkerFromArrival` |
| 3 | Sortie de l'acte II : grande porte du château ; acte III : jardin puis boss en contrebas | ✅ | `0beb0ef`, `89cff31` ; `acte3_tour.jpg` |
| 3 | Écran de fin : toutes les statistiques demandées, puis campement | ✅ | 13 lignes (temps, acte, tués par type, âmes gagnées / dépensées, améliorations, artefact, caisses, headshots, parades, distance, vitesse max, durée des actes, cause de la mort) ; `fin_de_run_victoire.jpg` |
| 4 | Salles organiques, hauteurs, ponts, chutes, eau sûre avec escaliers | ✅ | `187d17e`, `0beb0ef` ; `plan_acte.jpg`, `acte*_tour.jpg` |
| 4 | Parkour généreux, aucune zone sans traction | ✅ | surfaces des kits magnétiques ; non magnétiques seulement : le haut des falaises extérieures du jardin et de l'arène (au-dessus de la hauteur réglable `outerMagneticTop`), pour ne pas sortir de la carte |
| 4 | Accessoires selon la fonction, plus de torches ; acte II : hautes salles, lustres, rayons de lune ; acte III : jardin ouvert sans mur invisible | ✅ | `0beb0ef` ; `acte2_*.jpg`, `acte3_*.jpg` |
| 4 | 5 seeds par acte + captures | ✅ | `ActAudit` et test `ChaqueActe_5Seeds_ToutesLesSallesAtteignables_SortieEtTraqueur` (`592a3e2`) |
| 5 | Plus de goules (densité selon la taille de la salle) | ✅ | 45 à 49 goules actives par acte au chargement (mesuré ce matin) |
| 5 | Traqueur +0.5 m/s partout | ✅ | `fe36b51`, `EnemyMath.StalkerSpeed` |
| 5 | Aucun blocage sur 5 seeds par acte | ✅ | le traqueur atteint toutes les salles des 3 actes sur 5 seeds (`592a3e2`) ; chemin départ → sortie complet : 131 à 143 m (I), 254 m (II), 133 m (III) |
| 6 | Headshots ×2, marqueur et son | ✅ | `defc518` ; marqueur `HUD_Critique` de la DA, son `Sfx_Headshot_Critique` |
| 6 | Paralysie, explosion au kill + 4 à 6 autres, dans des SO, marchand et coffres | ✅ | 8 améliorations `Upgrade_Cameleon_*` (`CameleonTests`) |
| 7 | Parade réparée, geste des mains, 0.5 s d'invulnérabilité, étourdissement, arrêt sur image, éclair, son, debug en ms | ✅ | `ae7bc77` ; sonde `ParryProbe` : réussie de −250 à +80 ms autour de l'impact ; `ParryTests`, `ParryFeedbackTests` |
| 8 | Boss blessable, barre de vie de la DA, 2 à 3 phases, attaques annoncées, points faibles par traction, statistiques de victoire | ✅ | `89cff31`, `73b3bd1` ; `boss_phase1.jpg`, `boss_phase3.jpg` |
| 9 | Run complète jouable avec chaque personnage | ✅ (vérifiée par script, pas manette en main) | enchaînement I → II → III → boss → victoire ce matin ; chacun des 4 personnages lancé avec son artefact (lanterne, barillet, épée en os, livre à lien), aucune exception dans la console |
| 9 | Un bon mouvement échappe au traqueur | ✅ par le calcul, à confirmer en playtest | voir « Mesures » |
| 9 | Durée des actes mesurée | ⚠️ estimée | voir « Mesures » : je ne peux pas jouer une vraie run |
| Fin | `animations` fusionnée dans `main`, push, build de développement | ✅ | voir la fin de PROGRESS.md |

## Inventaire : ce qui n'était pas dans la DA (retiré ou remplacé)

- Sons synthétisés `ProceduralSfx` (tous) : remplacés par les 51 sons de la DA, sans silence restant.
- Textes 3D « Sortie / Salle verrouillée / prix » sur les portes et leurs lueurs rouge, jaune et orange : remplacés par le
  `Panneau_Porte` de la DA.
- Petite lumière bleue flottante (repère greybox) : retirée.
- Surbrillance violette des volants au survol : retirée.
- Néon violet du marchand : retiré (lumière tamisée).
- Barres de PV des ennemis toujours visibles : seulement 2.5 s après un coup, aux couleurs de la DA.
- Écran de chargement, écran de fin, panneau admin et menu Playtest provisoires : remplacés par ceux de la DA.
- Barre de vie provisoire du boss : remplacée par `HUD_BarreVieBoss` (la provisoire reste en secours si le prefab manque).
- Timer affiché dans l'arène du boss : masqué.
- Restent hors DA, faute d'asset : la capsule et les étiquettes du joueur 2 en coop locale, et les étiquettes flottantes des
  offres du marchand (texte du jeu, police de la DA).

## Mesures

**Poursuite.** Vitesses du traqueur (réglages + 0.5) : chasse 9.5 m/s, pointe 16.5 m/s pendant 3 s toutes les 6 s, soit
**11.8 m/s en moyenne** ; enragé 11.5 m/s, pointe 18.5 m/s, soit **13.8 m/s en moyenne**. Joueur : course 8.5 m/s, dash
16.3 → 18.3 m/s en trois dashs, bunny hop et traction jusqu'à 18 à 20 m/s.
- En ne faisant que courir, le joueur perd 3.3 m/s : rattrapé en une dizaine de secondes depuis 40 m.
- Avec un bon enchaînement (13 à 16 m/s en moyenne), il prend 1 à 4 m/s d'avance sur la chasse : **il s'échappe**.
- Contre l'enragé, seuls les meilleurs enchaînements (≥ 14 m/s en continu) tiennent : c'est voulu (fin du timer).

**Durée des actes** (estimation : je ne peux pas tenir la manette). Le chemin départ → sortie se parcourt en 16 à 30 s à la
course ; la sortie s'ouvre à 4 min et coûte 200 à 300 âmes. Un acte devrait donc durer **5 à 8 min** et la run **20 à 30 min**,
boss compris. L'écran de fin note maintenant la durée de chaque acte : la première vraie run donnera les chiffres exacts.

## Comparaison avec les meilleurs jeux (recherche détaillée dans RECHERCHE-NUIT-3.md)

| Élément | Référence | Où on en est |
|---|---|---|
| Level design organique, routes de mouvement | Titanfall 2, Doom Eternal (arènes à boucles et hauteurs) | boucles, rampes, terrasses, ponts et fosses générés, sans la mise en scène d'un niveau fait main : les points de vue et les « moments » restent à placer (salles d'exemple de la DA) |
| Peur et poursuite | Alien: Isolation, Dead by Daylight (rayon de terreur, cœur) | cœur à 3 paliers (60 / 40 / 20 m), musique par acte, arrivée par la porte annoncée par un son : proche de DbD, sans l'IA d'écoute d'Alien |
| Parade | Sekiro (~200 ms), Doom: The Dark Ages (~500 ms), Ultrakill (arrêt sur image) | 330 ms au total (0.25 + 0.08), arrêt sur image, éclair, étourdissement : entre Sekiro et Doom, comme le conseille la recherche |
| Améliorations | Risk of Rain 2 (proc, chaînes bornées), Hades | 8 améliorations propres au barillet avec chaînes bornées ; pas encore de synergies entre artefacts façon Hades |
| Écran de fin | Hades, Dead Cells (cause de la mort, relance rapide) | 13 statistiques, cause de la mort, relance avec R / Y ; manque la frise des salles |
| Boss en arène ouverte | Elden Ring, Hades (annonces lisibles, phases) | 3 phases, 8 attaques annoncées dans un panneau, points faibles en hauteur ; animations dédiées absentes (modèle et gréement du traqueur verrouillés) |

## Ce qui reste (honnêtement)

1. **Rendu « page Steam »** : pas atteint. Il faudrait des points de vue mis en scène par acte (salles d'exemple de la DA
   posées telles quelles dans la génération), plus de contraste et de lumières colorées, des matériaux qui cassent les grands
   aplats. C'est le plus gros chantier restant, à mener avec la DA.
2. **Ressenti** : parade, poursuite, boss et densité de goules sont réglés au calcul et par sonde ; seul un playtest dira si
   c'est juste.
3. **Animations du boss** : le traqueur réutilise ses clips (prise, bond, cri) ; son gréement est mis de côté comme demandé.
4. **Performance** : 45 à 49 goules actives par acte au chargement ; pas de mesure d'images par seconde dans le build cette
   nuit (à faire au premier test de l'exe).

## Checklist de test (dans l'ordre, environ 30 min)

1. Lancer `Builds/Nuit_2026-11-22/ItHunts.exe`, Jouer, faire une run normale : vérifier que F1 ne fait rien.
2. Acte I : sortir de la salle de départ (le timer démarre), tuer des goules, ramasser des âmes, acheter au marchand.
3. Viser une porte de sortie à moins de 9 m : le menu de la DA montre le prix ; l'ouvrir dès 240 s avec assez d'âmes.
4. Acte II : vos âmes et améliorations sont toujours là ; vers 1 min, la herse d'arrivée se lève et le traqueur entre.
5. Fuir le traqueur en enchaînant glissade, dash, bunny hop et traction : le cœur doit monter à 40 puis 20 m, et vous devez
   pouvoir le distancer.
6. Parer une goule (V ou Alt) juste avant l'impact : arrêt sur image, éclair, son, goule étourdie ; la ligne de debug
   donne l'écart en ms.
7. Acte III : traverser le jardin, descendre l'escalier vers l'arène du boss.
8. Boss : lire les annonces, monter sur un pilier par traction, viser un point faible lumineux, passer les 3 phases.
9. Victoire : relire les 13 statistiques, puis Entrée pour aller au campement.
10. Menu Playtest → Choisir l'acte → Boss final avec le caméléon : headshots (marqueur doré, son), F1 pour donner des
    améliorations du barillet (paralysie, cadavre explosif).
