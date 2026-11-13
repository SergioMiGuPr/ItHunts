# Âmes et économie : testing

**Updated:** 2026-11-13 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

- [x] Valeurs validées (`Economy_Default`), âmes des ennemis (goule 3, volant 2, traqueur 0), 8 améliorations décrites. *(`Assets/_Project/Tests/EditMode/EconomyTests.cs` › Assets_ValeursValidees_HuitAmeliorationsDecrites)*
- [x] Tirage avec la seed : même seed, même tirage ; trois différentes ; même emplacement du marchand. *(EconomyTests › Tirage_MemeSeedMemeChoix_TroisDifferents)*
- [x] Prix et refus : rien n'est retiré, refus signalé ; réserves individuelles ; coffre gratuit. *(EconomyTests › Prix_EtRefus_RienNEstRetire)*
- [x] Hausse du prix à chaque achat : 0 % par défaut ; composée ; par joueur et par catégorie ; un refus ou un coffre gratuit ne compte pas. *(EconomyTests › HausseDuPrix_ParAchatParJoueurEtParCategorie)*
- [x] ×2 en Enragé au moment de la mort. *(EconomyTests › Enrage_x2_AuMomentDeLaMort)*
- [x] Morts sans joueur (5 s). *(EconomyTests › MortsSansJoueur_DelaiDe5s)*
- [x] Coop (D-007 révisée) : gagnées en commun (celui qui ramasse et l'allié à 200 m reçoivent autant), dépensées individuellement, joueur mort exclu ; n'importe quelle orbe ramasse ; perdu au bout de 30 s. *(EconomyTests › Coop_GagneesEnCommun_DepenseesIndividuellement_MortExclu)*
- [x] Coffre par joueur : une fois chacun, son propre choix parmi 3 (réservé, vu par lui seul), refus sans rien retirer, remise à zéro ; texte « ouvert » pour toi ou ton prix. *(EconomyTests › Coffre_ParJoueur_UneFoisChacun_SonPropreChoix, Coffre_TexteParJoueur_OuvertPourToiOuSonPrix)*
- [x] Éclats communs : pas d'âme sans joueur impliqué ; 3 éclats une seule fois (plus d'éclats par joueur). *(EconomyTests › EclatsCommuns_SeulementSiUnJoueurYEstPourQuelqueChose)*
- [x] Interaction E / Y (Y partagé avec la recharge selon le contexte). *(`TelekinesisTests.cs` › Commandes_OrbeUnique_ClavierAzertyEtManette)*
- [x] En Play Mode, **coffre** : visé, aide « E : ouvrir le coffre (30 âmes) » ; 0 âme + E : refus ; 40 âmes + E : ouvert (reste 10), 3 orbes (Grande orbe, Barillet garni, Onde blessante) ; F sur une orbe : amélioration prise, les deux autres disparaissent. **Marchand** : 4 offres ; sans âme, refus ; 70 âmes : acheté, offre retirée. **Porte** (50 âmes) : ouverte. *(Play Mode, 2026-11-13 ; l'ouverture par une caisse lancée ou une répulsion, mesurée ce jour-là, a été retirée après le playtest)*
- [x] En Play Mode, **seed** : seed 123 deux fois : même coffre (Charge en plus, Grande orbe, Projectiles lourds) et même emplacement du marchand ; seed 999 : autre tirage. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **collecte à pleine vitesse** (20 éclats à 4–6 m de côté de la trajectoire) : course 17 à 20/20, bunny hop 20/20, traction 12 à 14/19. Coût : **0.06 ms** par pas fixe avec 40 éclats. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **zone de 4 min** (vagues, traqueur coupé, joueur immortel qui suit la boucle ; modèle de combat : il tue l'ennemi le plus proche à moins de 20 m chaque seconde) : **course** : 175 tués, 489 âmes tombées, **280 ramassées, 209 perdues (43 %)** ; par minute 26 / 53 / 56 / 142 ; premier coffre payable à 65 s ; 9 coffres payables. **Bunny hop** : 129 tués, 364 tombées, **314 ramassées, 50 perdues (14 %)** ; par minute 35 / 46 / 68 / 165 ; premier coffre à 59 s ; 10 coffres payables. *(Play Mode, 2026-11-13)*
- [x] Ressenti : collecte, refus, marchand, coffres et choix des améliorations. *(playtest, 2026-11-13 ; salle verrouillée non testée)*

- [x] En Play Mode, **coop** (2e joueur simulé dans la salle SE, à 140 m) : 6 éclats près de lui, ramassés par son orbe : **0 → 6 âmes pour chacun** ; 4 près du joueur 1 : 10 chacun ; le joueur 1 dépense 5 : 5 pour lui, **10 inchangé pour l'autre** ; joueur 2 mort, 3 éclats ramassés : 8 pour le 1, **rien pour le mort**. *(Play Mode, 2026-11-13)*
- [x] En Play Mode, **coffre par joueur** (salle C) : ouvert par le joueur 2 : vu par la caméra du joueur 1, **couvercle fermé, « Coffre / 30 âmes », orbes du 2 non dessinées** ; ouvrable par le 1, plus par le 2 ; ouvert par le 1 : **couvercle ouvert, « Coffre / ouvert », ses 3 orbes dessinées, 0 des 3 du 2** ; deux choix différents tirés avec la seed. *(Play Mode, 2026-11-13)*

## Checklist F12 — âmes et économie (Test_Arena)

1. **Éclats :** tue une goule (barillet, objet lancé, répulsion contre un mur) : 3 éclats cyan tombent ; approche-toi (8 m) :
   ils filent vers l'orbe, compteur « Âmes » en haut à gauche. En Enragé : le double.
2. **Mort sans toi :** une goule qui meurt sans que tu y sois pour quelque chose depuis 5 s ne lâche rien.
3. **Perdus :** laisse des éclats au sol : ils clignotent les 5 dernières secondes, puis disparaissent au bout de 30 s.
4. **Coffre (salles C et SE, 30 âmes) :** E devant lui (aide sous le viseur) ; sans assez d'âmes : refus en rouge et son ;
   ouvert : 3 orbes au-dessus, nom et icône lisibles à 10 m ; prends-en une avec F ou en la regardant avec E ; les deux autres
   disparaissent. Une caisse lancée ou une répulsion ne l'ouvre pas.
5. **Marchand :** il change de place avec la seed ; 3 améliorations et un soin, avec leur prix ; achat avec E (ou F).
6. **Salle verrouillée (pièce NE, 50 âmes) :** E sur la porte : elle s'ouvre ; un coffre gratuit dedans.
7. **Améliorations :** vérifie l'effet de celle que tu prends (charges en plus dans le HUD, PV, balles, rayon de collecte…).
8. **Manette :** Y devant un coffre l'ouvre ; ailleurs, Y recharge le barillet.

## Checklist F12 — après le playtest (partie 2)

1. **Traction (2 charges) :** deux tractions de suite, la troisième est refusée ; une charge revient en 6 s. Est-ce que tu
   t'appuies moins sur elle ?
2. **Murs :** enchaîne wall run et wall jumps (couloir, cheminée de Test_Movement) : la vitesse monte moins vite et plafonne à
   18 m/s (F3) ; lancé plus vite (bunny hop, dash), les murs ne te freinent pas.
3. **Âmes en coop (écran partagé plus tard ; en solo, rien ne change) :** les éclats sont visibles par tous, n'importe quelle orbe
   les ramasse, chaque joueur vivant reçoit les mêmes âmes ; chacun dépense les siennes.
4. **Coffres par joueur :** un coffre ouvert affiche « Coffre / ouvert » pour toi ; en coop, il reste fermé et payable pour
   l'autre, avec ses propres orbes.
5. **Apparitions :** reste dans une grande salle pendant un pic : aucun ennemi n'apparaît dans ta salle ; ils arrivent par les
   couloirs et les salles voisines.

## History

- 2026-11-13 : construit et mesuré (étape 7, D-043) ; playtest validé ; coffre seulement avec E / Y, hausse du prix.
- 2026-11-13 : âmes gagnées en commun, coffres par joueur ; mesurés.
