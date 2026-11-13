# Volant : testing

**Updated:** 2026-11-13 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

- [x] Valeurs : 25 PV, 8 kg, 6 / 14 m/s, piqué 8 PV annoncé 0.4 s, recharge 3 s, étourdi 1.5 s, ronde 8 × 8 m, éclatement 3 m / 30 / ×0.25, 8 en chasse, 12 au total, aggro 20 / 8 m, rendormi 50 m / 5 s. *(`Assets/_Project/Tests/EditMode/EnemyTests.cs` › Volant_ValeursEtEclatement)*
- [x] Éclatement : 30 au centre, 18.75 à mi-rayon, 7.5 au bord, rien au-delà. *(EnemyTests › Volant_ValeursEtEclatement)*
- [x] Attrapable à tout moment (sans étourdissement), libéré au bout de 2 s ; un ennemi ordinaire seulement étourdi. *(`CombatTests.cs` › Volant_AttrapableATouteHeure_LibereAuBoutDe2s)*
- [x] Scène : 20 volants, 4 par grande salle, aucun dans les couloirs. *(EnemyTests › Scene_TestArena_Labyrinthe)*
- [x] Meurt au premier impact de projectile (objet lancé ou repoussé, ennemi lancé, mur), pas par une zone ; réglage coupé : dégâts ordinaires. *(CombatTests › Volant_MeurtAuPremierImpactDeProjectile_PasParUneZone)*
- [x] Une balle de 12 le tue (25 PV), aile comprise ; un déclencheur ordinaire est traversé. *(CombatTests › Volant_UneBalleLeTue_AilesComprises)*
- [x] Virage du piqué limité (angle, vitesse gardée), 80°/s, sans avance. *(EnemyTests › Volant_ValeursEtEclatement)*
- [x] En Play Mode (salle de mesure de 140 m), **zone de touche** : 200/200 points du modèle touchés (corps, bord avant, ailes au milieu, au bout, au bord arrière ; tirs de face, de côté, d'en dessous, ailes qui battent), 0/120 points à côté. **Barillet, visée sur sa position** : à l'arrêt à 10, 30 et 60 m, chaque balle partie l'a tué (44/44 ; les autres essais tombaient pendant un rechargement du script) ; en bunny hop à 18 m/s 20/20 à 10, 30 et 60 m. Caisse lancée : tué 6/6. Onde de répulsion seule : tombé étourdi 5/5, vivant 5/5. Répulsion contre un mur à 3 m : tué 3/5. *(Play Mode, 2026-11-13)*
- [x] En Play Mode (salle de mesure), **répulsion puis choc** : contre un mur à 3 m, tué **20/20** (repoussé 20/20) ; vers le sol (volant à 2 m, poussé à l'horizontale), tué **20/20**. Piqué sur un joueur **en traction** : touché 50/50 sans réaction, **0/50 avec un dash tard** (volant à moins de 3.5 m). *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **piqué à visée corrigée** (80°/s) : sans esquive, touche **32/100** joueurs qui courent (90°/s : 38/100 ; avance 0.3 : 68 %), 0/50 en bunny hop, 50/50 en traction ; joueur immobile : touché 20/20 sans réaction, 20/20 avec un dash pendant l'annonce, 20/20 au début du piqué, **0/20 avec un dash tard** (volant à moins de 3.5 m). *(Play Mode, 2026-11-13)*
- [x] *(Avant la visée corrigée)* En Play Mode, **piqué** en ligne droite : esquivé 20/20 avec un dash pendant l'annonce, 20/20 pendant le piqué, 0/5 sans réaction. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **prise en vol** (F, volant en vol à 6 m de haut, écart de visée tiré jusqu'à 1.5°) : à l'arrêt 20/20 à 10, 20 et 30 m ; en bunny hop à 18 m/s 20/20 à 10, 20 et 30 m. *(Play Mode, 2026-11-13)*
- [x] *(Avant la mort au premier impact)* barillet à 15 m : mort 10/10 en 1.13 s. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **lancé sur un groupe de 3 goules** (60 PV chacune, à 14 m) : éclaté 10/10, **66 dégâts au total en moyenne, dont 41 par l'éclatement** (essai 1 : 48 / 26 / 25), 0.79 s de la prise à l'éclatement ; il meurt. **Combo répulsion → prise → lancer** : éclaté 10/10, **72 dégâts en moyenne (47 par l'éclatement), 1.72 s de la répulsion à l'éclatement**. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **traversée du labyrinthe** en courant (102 s, 76 goules et 20 volants) : 8 à 12 volants en chasse (11 en moyenne), 116 piqués dont 3 touchés, **aucun volant bloqué** plus de 3 s loin du joueur, **aucun dans le décor** ; coût : volants **0.18 ms par pas fixe**, goules 0.28 ms (éditeur). *(Play Mode, 2026-11-13)*
- [ ] Ressenti : ronde, lisibilité et danger du piqué, prise en vol, éclatement, combo avec la répulsion. *(playtest)*

## Playtest — volant (Test_Arena)

4 volants par grande salle (pas dans les couloirs). Page 3 du debug : volants en vie, en chasse, en piqué, le plus proche.

1. **Ronde :** ils tournent au-dessus de toi (8 m) ; ils te suivent d'une salle à l'autre.
2. **Piqué :** cri aigu et lueur rouge (0.4 s), puis il fonce en te suivant : un dash au dernier moment l'esquive ; touché : 8 PV et une petite poussée.
3. **Prise en vol :** vise-le et F, à toute distance ; il se débat et se libère au bout de 2 s, puis remonte avant de repiquer.
4. **Éclatement :** attrape-le et lance-le sur un groupe de goules : il éclate (cercle au sol de 3 m), les goules autour
   prennent des dégâts, lui meurt.
5. **Combo :** repousse-le (clic droit) : il tombe au sol étourdi ; attrape-le (F) et lance-le sur des goules.
6. **Nombre :** 8 en chasse au plus ; passe près d'un volant en attente : il se réveille (12 au plus).

## Checklist F12 — corrections du 2026-11-13 (prochain playtest)

Test_Arena, page 3 du debug (F3) pour le volant le plus proche et la main plantée.

1. **Volant tué d'un coup :** une balle du barillet, une caisse lancée ou repoussée, une goule lancée : il meurt au premier
   impact. Vise aussi le bout d'une aile : la balle le touche. L'onde de répulsion seule le fait tomber étourdi sans le tuer ;
   repoussé contre un mur ou vers le sol, il meurt au choc (1.5 s après la répulsion, quelle que soit la vitesse). Un volant
   lancé éclate toujours sur les goules autour.
2. **Dash :** une charge revient en **2 s au sol, 4 s en l'air** (avant 1 et 3 s) ; le saut en l'air utilise les mêmes charges :
   enchaîne dash, saut en l'air, dash. Trop lent ?
3. **Main plantée, salle de 40 m :** provoque une prise, esquive, puis passe à côté : il avance vers toi et, à moins de 7 m,
   **revers** (zone rouge 0.2 s, puis coup). Fermé en 40 m ? Toujours faisable en 80 m (bunny hop, traction) ? Lisible ?
4. **Piqué :** cri et lueur 0.4 s, puis il fonce en te suivant (80°/s). Dash pendant l'annonce ou tout au début : il te
   rattrape ; dash au dernier moment : il te rate, même en traction. En courant sans esquiver, il touche environ une fois sur
   trois. Juste ?

## History

- 2026-11-13 : construit et mesuré (bloc 6, D-040).
- 2026-11-13 : mort au premier impact, zone de touche, piqué à visée corrigée ; checklist F12 ajoutée.
