# Goule : testing

**Updated:** 2026-11-12 · [Design](design.md) · [Implementation](implementation.md)

## Acceptance criteria

Après le playtest des étapes 8 et 9 (D-062) :

- [x] Trajet long par étapes : entrée de la salle suivante, jamais de retour au seuil. *(`GhoulPathTests.cs` › LongTrajet_EntreeDeLaSalleSuivante_JamaisDeRetourAuSeuil)*
- [x] Seuil de NavMesh sous les portes des pièces (pas sous les fenêtres). *(GhoulPathTests › SeuilDePorte_DalleAuSol_PasSousUneFenetre)*
- [x] Réglages : escalade 30 / 12 / 12 m, accrochée 3 s, long trajet 100 m, déblocage 2 s, annonce du bond 0.3 s. *(GhoulPathTests › Reglages_Escalade_Deblocage_LongTrajet)*
- [x] En Play Mode (Level_01_Base), **10 plateformes**, joueur dessus, une goule chasseuse à 16–19 m au sol :
  - salles (galerie de module 6 m, cages 3 m, pile de caisses 2.6 m, échafaudage 6 m) : **5 / 5 atteintes**, médiane **6.1 s**, max **11.6 s** (escalades et bonds depuis le mur) ;
  - couloirs (passages en hauteur à 4.5 m) : **5 / 5 atteintes**, médiane **3.0 s**, max **4.8 s**. *(Play Mode, 2026-11-15 ; captures `p7_plateforme_*.png`)*
- [x] En Play Mode, **zone complète** (timer 0 → 930 s, temps ×4, joueur invulnérable qui change de salle toutes les 20 s) : **0 goule immobile plus de 8 s** en chasse ; 15 déblocages, 4 replacements après 6 s hors du NavMesh. Causes trouvées et corrigées : trajets de 900 m au-delà de ce que calcule Unity (chemin partiel au même point), goules sorties du NavMesh au bord des fosses, seuil sans sol sous les portes (21 pièces inaccessibles avant, 2 salles verrouillées après). *(Play Mode, 2026-11-15)*
- [x] NavMesh des goules (niveau 1) : 270 étapes sur 271 reliées (la seule exception : derrière la porte de sortie fermée). *(Play Mode, 2026-11-15)*

Avant :

- [x] Valeurs de départ : 60 PV, 50 kg, 10 m/s, bond 14 m/s annoncé 0.3 s, recharge 2.5 s, contact 15 PV / 1 s, étourdie 1.5 s. *(`Assets/_Project/Tests/EditMode/EnemyTests.cs` › Reglages_ValeursDeDepart)*
- [x] Bond : arc de 1 m, retombe 1 m après le joueur, au plus 14 m/s ; contact mesuré entre les capsules. *(EnemyTests › Bond_ArcEtVitesseMax_Contact)*
- [x] La poussée ne ralentit jamais le joueur (500 cas, dont poussé de face). *(EnemyTests › Poussee_NeRalentitJamaisLeJoueur)*
- [x] Coups des ennemis : sans tir ami, mort annoncée une fois, PV rendus à la remise à zéro. *(EnemyTests › CoupsDesEnnemis_SansTirAmi_MortAnnoncee)*
- [x] En Play Mode (Test_Arena, joueur immobile dans B) : repérée à 12.5 m, chasse à 10 m/s, annonce à 6 m, bond de 6.8 m à 11 m/s, touché : 100 → 85 PV, puis contact 15 PV par seconde (25 PV après 6 s). *(Play Mode, 2026-11-12, capture de l'annonce)*
- [x] En Play Mode, **tuée avec l'environnement** : vraie répulsion contre le mur ouest de B → impact 25.7 m/s, étourdie, 40 PV (60 → 20) ; prise (F) pendant l'étourdissement ; lancée contre le mur d'en face → 0 PV, morte. Étourdie, elle s'arrête ; après, elle reprend la chasse (annonce du bond). *(Play Mode, 2026-11-12, capture étourdie)*
- [x] Nouvelle Test_Arena : 10 goules par arène (30 en tout), réparties pour garder la pression ; la goule passe les petites portes de 1.2 m (NavMesh de la goule à 0.1 m de voxel) mais pas les grilles du traqueur. *(EnemyTests › Scene_TestArena_PrototypeM3 ; Play Mode, 2026-11-12)*
- [x] Coût : 30 goules en chasse autour du joueur, 0.22 ms par pas fixe. *(Play Mode, 2026-11-12)*
- [x] Bond d'en haut : depuis une corniche de 11 m, l'arc retombe sur le joueur 5 m devant, au plus 14 m/s ; au même niveau, identique au bond au sol. *(EnemyTests › BondDeHaut_AtteintLePointPlusBas)*
- [x] En Play Mode, **banc d'escalade isolé** (corniche de 11 m, deux murs grimpables, loin du labyrinthe) : joueur sur la corniche, goule au sol à 25 m → elle grimpe et le touche en 4.1 s ; joueur au sol → elle redescend et le touche en 3.0 s ; bond depuis la corniche, joueur 7 m devant : 20/20 esquivés avec un dash pendant l'annonce, 0/5 sans réaction. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **labyrinthe** : joueur sur le pont de la salle SE (11 m) touché en 8.4 s (départ à 36 m, un mur grimpé), sur le pont du couloir S – C en 6.2 s (30 m), sur la corniche de la salle centrale en 5.3 s (39 m), dans la salle haute NO (+16 m) en 9.9 s depuis le carrefour O (96 m, par la pente), sur la corniche de NO (+27 m) en 5.7 s. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **esquive du bond d'en haut** (corniche de C, 11 m) : joueur à 10 m ou 6 m, dash de côté 0.15–0.35 s après le saut : **20/20** esquivés ; dash pendant l'annonce (joueur à 6 m) : **0/20** (elle vise après l'annonce). *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **coût** (éditeur) : 40 goules en chasse + traqueur : goules 0.28 ms par pas fixe, traqueur 0.07 ms ; les 76 goules : 0.55 ms et 0.08 ms ; image médiane 4.7–4.9 ms. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **mur de 16 m vers la salle haute NO** (joueur près du haut du mur) : goule partie du milieu de la voie au sol, **6.8 s par le mur contre 9.7 s par la pente** (murs retirés) ; partie du carrefour O, 8.5 s dans les deux cas (elle prend la pente, aussi courte). Bond depuis le haut du mur sur un joueur 10 m devant au pied : 20/20 esquivés avec un dash pendant le vol, 0/5 sans réaction. *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **plafond de 25 goules en chasse** : 25 en chasse sur 76 après 1.2 s ; une chasseuse tuée : relais en 0.31 s par la goule en attente la plus proche par le chemin (121 m) ; coût avec 25 en chasse et le traqueur : goules 0.34 ms par pas fixe, traqueur 0.09 ms, image médiane 5.4 ms (éditeur). *(Play Mode, 2026-11-12)*
- [x] Valeurs D-038 : aggro 20 m / 8 m, total 40, rendormie à 50 m pendant 5 s. *(EnemyTests › Reglages_ValeursDeDepart)* ; plus aucun mur grimpable posé à la main dans Test_Arena. *(EnemyTests › Scene_TestArena_Labyrinthe)*
- [x] En Play Mode, **murs grimpables automatiques** : 191 liens trouvés (19 de 2 à 5 m, 157 de 5 à 12 m, 15 de 12 à 20 m), **73 ms** de recherche (construction totale 147 ms, dont 73 ms de NavMesh) ; 5 liens sur la face d'une corniche de la salle centrale, 4 sous chaque pont de couloir, 2 sur le mur de 16 m de la voie au sol O – NO, 0 sur les murs de salle de 36 m ; **un mur de 12 m posé au hasard dans la salle SO : 4 liens trouvés sans rien placer** (reconstruction 130 ms). *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **traversée du labyrinthe en courant** (SO → S → SE → E → C → O → NO → N → NE, 100 s, joueur immortel) : **25 à 40 goules en chasse (34.6 en moyenne)**, dont 0 à 15 par l'aggro (9.6 en moyenne), 15 réveils ; **coût avec 40 en chasse : 0.44 ms par pas fixe** (max 1.15 ms par image), image médiane 5.1 ms (éditeur). *(Play Mode, 2026-11-12)*
- [x] En Play Mode, **mur de 16 m avec les liens automatiques** (salle haute NO, joueur près du haut du mur) : goule partie du milieu de la voie au sol, **6.2 s par le mur contre 9.8 s par la pente** (liens coupés) ; partie du carrefour O, 9.1 s par le mur contre 8.4 s par la pente (elle grimpe le mur alors que la pente était un peu plus courte). *(Play Mode, 2026-11-12)*
- [ ] Ressenti : poursuite permanente, encerclement, murs, bond d'en haut, nombre (76). *(playtest)*

## Playtest — goule dans le labyrinthe (D-035)

Test_Arena : 8 goules par salle, 3 par couloir (76). Checklist complète : [boss-traqueur/testing.md](../boss-traqueur/testing.md).

1. **Toujours là :** elles savent où tu es : même derrière un mur ou dans ton dos, elles viennent (par le chemin).
2. **Encerclement :** de loin, elles arrivent de plusieurs côtés, pas en file.
3. **Murs :** monte sur une corniche (11 m), un pont, ou dans une salle haute par le bout d'une voie au sol (mur de 16 m) :
   elles grimpent le mur (en biais) et te rejoignent ;
   redescends : elles sautent derrière toi.
4. **Bond d'en haut :** sous une goule perchée, elle s'accroupit (lueur rouge, sifflement) puis saute ; dash de côté
   **pendant son vol** pour l'esquiver.
5. **Plafond et aggro :** 25 en chasse au plus ; passe près d'une goule en attente (8 m, ou 20 m en vue) : elle se réveille
   (page 3 : « dont réveillées par l'aggro », 40 au plus) ; éloigne-toi à plus de 50 m pendant 5 s : elle se rendort.
   Pose un mur où tu veux (futur level design) : les goules le grimpent sans réglage.
6. **Bond au sol, contact, environnement :** comme avant (15 PV, poussée légère ; répulsion contre un mur, F, lancer).

## History

- 2026-11-12 : au sol (étape 6, bloc 2).
- 2026-11-12 : 10 par arène dans la nouvelle Test_Arena (D-034).
- 2026-11-12 : poursuite permanente, encerclement, murs et bond d'en haut, labyrinthe (D-035).
