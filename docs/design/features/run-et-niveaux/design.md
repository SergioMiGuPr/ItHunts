# Run et niveaux

**Status:** in progress · **Updated:** 2026-11-13 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md) · D-046, D-048 à D-053

## Résumé

Une run va du menu au niveau 1, puis à son combat de boss, puis au niveau suivant ou à la fin. Seule la difficulté Facile
est jouable : elle s'arrête après le niveau 1. Dans chaque niveau, le timer démarre quand un joueur quitte la salle de départ.
Le traqueur est lâché à 3 min et la porte de sortie se déverrouille à 10 min ; on la paie en âmes. L'Enragé commence à
15 min. En haut des escaliers de sortie, le combat de boss se gagne avec trois pièges. Le niveau 1 est généré en greybox
(800 × 800 m environ, 49 salles), avec une minimap et un brouillard de guerre.

## Après le playtest des étapes 8 et 9 (2026-11-15)

- **Playtest du combat de boss :** entrée « Playtest : combat de boss » au menu principal (niveau 1 en Facile, directement dans
  l'arène, traqueur, pièges et leviers) ; en jeu, **Page bas** (éditeur et builds de développement) y emmène directement (le timer
  avance jusqu'à l'ouverture de la sortie, comme en vraie partie).
- **Minimap :** la sortie est affichée **dès le début de la zone**, sans la découvrir.
- **Textes en monde** (marchand, sortie, coffres, orbes de choix) : **cachés par le décor** (shader `ItHunts/WorldText` avec test de
  profondeur), toujours lisibles à 10 m quand rien ne les cache.
- **Menu pause** par joueur (Échap / Start), le jeu continue ; « Quitter l'écran partagé » pour le joueur 2.
- **Affichage de debug (F3)** éteint au lancement.

## Règles

1. **Run (D-048).** Un asset (`Run_Default`) liste les niveaux : le niveau 1 est rempli ; les niveaux 2 et 3 et la zone
   finale sont encore vides. Il liste aussi les difficultés :

   | Difficulté | Jouable | Niveaux joués | PV des ennemis | Dégâts | Vitesse | Nombre |
   |---|---|---|---|---|---|---|
   | Facile | oui | 1 | ×1 | ×1 | ×1 | ×1 |
   | Moyenne | non (grisée) | 2 | ×1.4 | ×1.25 | ×1.1 | ×1.25 |
   | Difficile | non (grisée) | 4 | ×1.8 | ×1.5 | ×1.2 | ×1.5 |

   Le traqueur garde toujours ses PV et sa vitesse (D-036).
2. **Déroulé.** Menu, puis niveau, puis combat de boss, puis niveau suivant ou écran de victoire (Facile : victoire après le
   niveau 1). Le menu enchaîne sans pause.
3. **Salle de départ (D-048).** Elle porte un volume de salle de type « Départ ». Tant qu'aucun joueur n'en est sorti, le
   timer attend : pas de vague, aucun ennemi n'existe, et le traqueur reste dans son repaire. À la première sortie, le timer
   démarre et la salle redevient normale.
4. **Timers** (réglables dans `Zone_*`) :
   - **180 s :** le traqueur est lâché ;
   - **600 s :** la porte de sortie se déverrouille (rouge avant, orange après). Un joueur la paie **200 âmes** et elle
     s'ouvre pour tous ; s'il manque des âmes, refus en rouge ;
   - **900 s :** Enragé (×2 sur les âmes).

   Le HUD affiche le prochain événement : « sors de la salle de départ », « traqueur dans », « sortie dans », « sortie ouverte ».
5. **Mort (D-048).** Quand le dernier joueur vivant meurt, l'écran de fin s'affiche (Menu principal ou Rejouer). Il ne met
   pas le jeu en pause. En solo, une mort termine la run. La réanimation et le spectateur sont prévus comme événements d'état
   (`CoopLife`) ; ils seront branchés à l'étape 9.
6. **PV du joueur : 150.**
7. **Marchand de l'artefact (D-050).** Il se trouve dans chaque salle de départ :
   - chaque joueur voit ses propres offres : 3 améliorations du barillet tirées au hasard ;
   - après un achat, ses 3 offres sont tirées de nouveau ;
   - le prix monte de **25 %** par achat, par joueur ;
   - **4e offre fixe : le soin** (+40 PV pour 35 âmes, réglable dans `Economy_Default`), seul moyen de se soigner pendant
     une zone (après le playtest de l'étape 8).

   Les 5 améliorations du barillet sont provisoires : balles +2, dégâts +30 %, cadence +25 %, rechargement −35 %,
   portée +25 m. Les coffres gardent les 7 améliorations de stats. Le marchand placé au hasard est retiré.
8. **Surface non magnétique (D-051).** C'est un composant qu'on pose sur une surface. La traction ne s'y accroche pas :
   refus en rouge, et la charge reste. La répulsion marche. En greybox, la surface est gris pâle à rayures, sans lueur.
9. **Niveau 1 (D-052).** Il est généré par *ItHunts > Générer le niveau 1*, dans `Level_01_Base`. `Level_01` est la copie que
   l'utilisateur retravaille : le constructeur la crée une fois et ne l'écrase jamais. Son plan :
   - **Grille :** 7 × 7 salles à 120 m d'écart, soit 800 × 785 m. 41 salles (46 à 66 m de côté) et 8 carrefours ; environ
     30 % des salles sont à +16 m, avec des pentes entre les deux niveaux.
   - **Couloirs :** 20 % des couloirs de la grille sont retirés, mais chaque salle en garde au moins 2 (boucles, règle de
     Pac-Man).
   - **Points fixes :** repaire du traqueur en cul-de-sac à l'est ; départ au sud-ouest, avec le marchand ; sortie au
     nord-est, avec la grande porte et les escaliers.
   - **Canaux d'égout :** 7, larges de 6 m, avec un grand pont dans l'axe du couloir et un petit pont. L'eau profonde est un
     vide : le joueur remonte sur la rive de son côté et perd **20 PV**. Un ennemi qui tombe dedans se noie. Ennemis et
     traqueur ne traversent que par les ponts.
   - **Coffres :** 14, dans un coin, derrière un muret de 3 m.
   - **Zones de parcours non magnétiques :** 6, avec 4 plateformes de 2.5 à 6.5 m et des panneaux de course.
   - **Pièces fermées :** 6 sur le pourtour, dont 2 salles verrouillées.
   - **Dans les salles :** corniches pleines de 11 m reliées par un pont (dessous à 10 m), murs de course, cibles de traction,
     objets et lustres. Une partie des couloirs a des poutres et des ponts.
   - **Volumes et apparitions :** un volume par salle et par tronçon de couloir ; 760 points d'apparition.

   Tous ces chiffres se règlent dans `LevelLayout_Level01`.
10. **Combat de boss (D-049).** Il commence quand un joueur atteint le palier en haut des escaliers. Les joueurs entrent dans
    l'arène (80 × 80 m, 30 m de haut) et le traqueur arrive par le fond. Il garde toutes ses attaques : prise annoncée, mains
    levées, contact mortel. Les vagues s'arrêtent.
    - **Les pièges.** Chacun a une cible qui brille (un objet lancé, une balle ou une onde blessante la déclenche) et une zone
      orange au sol où il faut attirer le traqueur :
      - **herse :** seul passage vers la poche ouest, fermée par des murs jusqu'au plafond ;
      - **pont qui s'effondre :** seul passage vers la poche est, au-dessus d'une fosse de 8 m qui traverse l'arène ;
      - **lustre géant :** au centre, avec un treuil sur le mur nord.
    - **Piège réussi** (traqueur dans la zone au moment du déclenchement) : il est sonné 4 s puis revient par le fond. Le
      passage se rouvre et la cible reste éteinte.
    - **Piège raté :** le piège se réarme en 4 s.
    - **Au troisième piège réussi**, il tombe : le niveau est gagné.
11. **Minimap (D-053).** Elle est dans la vue de chaque joueur, nord en haut, sur 320 m autour de lui ; Tab maintenu affiche
    toute la carte.
    - Une salle ou un tronçon se découvre quand un joueur y entre, et les découvertes sont communes en coop.
    - Une fois découverts, les points clés s'affichent : coffres (gris une fois ouverts par ce joueur), marchand, salle
      verrouillée, sortie (rouge, puis verte), départ.
    - Les alliés apparaissent en bleu.
13. **Population (après le playtest de l'étape 8).**
    - **Habitants :** en plus des vagues, chaque grande salle (pas la salle de départ) a dès le départ 2 à 5 goules selon sa
      taille, plus 1 à 2 volants si elle est au niveau haut. Ils attendent, avec l'aggro habituelle. Tués, ils ne reviennent
      pas avant la remise à zéro.
    - **Avant la première sortie,** rien ne bouge sur la carte (`ZoneState.Asleep`).
    - **Vagues :** le nombre voulu suit la surface de la carte : × surface / 36 300 m² (Test_Arena), borné entre 0.5 et 2.5.
      Le niveau 1 (214 816 m²) est à ×2.5. Les plafonds de chasse ne changent pas, et la réserve des vagues suit la même
      échelle.
    - **Mesure** (traversée de 15 min, temps ×5, allers-retours départ ↔ sortie à 7.5 m/s) :
      - 164 habitants ;
      - ennemis vivants : 191 à 1 min, 251 à 6 min, 344 à 14 min ;
      - 7 à 38 goules en chasse ;
      - **0 grande salle sur 40 sans ennemi** ;
      - **0 salle vide à l'entrée** sur 46 traversées.
    - Réglages : `LevelLayout_Level01` (habitants), `Waves_Default` (surface).
14. **Modules de salle et décor (D-055).** Chaque grande salle du niveau 1 est un module fait à la main (prefab), choisi
    par le générateur avec la graine. La taille de la salle est celle du module (48, 56 ou 64 m), et le module est tourné d'un
    quart de tour au hasard.
    - **Bibliothèque :** 13 modules de salle et 2 de carrefour :
      - étages en anneau, galeries hautes, égouts, salle encombrée ;
      - salle haute remplie (niveau haut), parcours (fosse) ;
      - crypte, chapelle, armurerie, grand hall à colonnes, cuisine et réserve, salle de garde, prison ;
      - carrefour à niches, carrefour encombré.
    - **Contenu d'un module :** 44 à 96 props, des points d'apparition, des emplacements d'habitants et de coffres (derrière un
      obstacle), des objets à lancer et des torches.
    - **Pièces annexes :** une vingtaine, meublées, le long des couloirs (chambre, cellule, réserve, cuisine, armurerie,
      chapelle, crypte), dont les salles verrouillées.
    - **Décor des couloirs :** dans une bande de 3 m le long des murs (voie de 8 m au milieu), torches tous les 12 m.
    - **Catalogue des props :** dans ART-HANDOFF.md.
    - **Créer son propre module, sans code :**
      1. Dans `Assets/_Project/Prefabs/Modules`, dupliquer un module (Ctrl+D) et lui donner un nom qui ne commence pas par
         `Module_` (le constructeur ne recrée que ceux-là).
      2. Sur la racine, régler le composant `RoomModule` : nom, salle ou carrefour, côté (48, 56, 64 ; 20 pour un carrefour),
         niveau (sol, haut ou les deux), poids du tirage, sol percé (canal ou fosse).
      3. Modifier l'intérieur avec les prefabs `Prop_*`, des blocs ou des escaliers. Règles :
         - centre du module = centre de la salle, sol à y = 0, murs à ±côté/2 (faits par le générateur) ;
         - ouvertures des couloirs au milieu de chaque mur ;
         - **garder libre une croix de 11 m de large** entre les ouvertures, sous 10 m de haut ;
         - étages pleins jusqu'au sol ; ponts dont le dessous est à 10 m ;
         - groupes de décor séparés d'au moins 8 m ;
         - rien qui ferme un angle.
      4. Ajouter des objets vides avec `SpawnPoint` (goule ou volant), `ResidentSpot` (habitant) et `ChestSpot` (coffre
         possible, derrière un obstacle).
      5. Relancer *ItHunts > Générer le niveau 1* : le module est tiré comme les autres.
      6. Vérifier la portée de la main du traqueur avec la mesure habituelle (aucun recoin hors de portée).
15. **Passages en hauteur (D-056).** Il y en a 10, à côté de l'entrée d'un couloir dans une salle :
    - une ouverture de 3 × 3 m à 4.5 m du sol, dans le mur de la salle, mène par un conduit de 8 m à une ouverture à la même
      hauteur dans le mur du couloir ;
    - on y monte en saut mural, et les murs autour sont non magnétiques ;
    - c'est trop petit pour le traqueur, qui fait le tour, mais sa main l'atteint.

    C'est une sortie de secours quand il bloque l'entrée.
16. **Contourner le traqueur seulement dans les zones de parcours (D-057).**
    - **Pentes :** elles prennent toute la largeur du couloir (plus de voie au sol avec du vide à côté).
    - **Galeries de parcours :** il y en a 6, le long des couloirs plats de 55 m et plus, derrière le mur du couloir (20 m de
      large). Portes de 3 m aux deux bouts, puis des paliers et des poutres à sa portée, et des plateformes qui tombent
      0.6 s après qu'on s'y pose et reviennent 4 s plus tard. Les plateformes sont à 21 à 25 m de l'axe, hors de sa portée.
      En dessous, un vide de 10 m : on revient au palier et on perd 20 PV. Tout est non magnétique.
    - **Modules de parcours :** 3, avec une fosse centrale et les mêmes plateformes.
    - **Mesures :**
      - **dépasser hors parcours :** 0 sur 240 essais (8 pentes et 4 couloirs, 10 en course et 10 en bunny hop chacun, dash à
        chaque prise annoncée) ; le joueur est tué par ses bras en passant à 5 m ;
      - **traverser une galerie avec le traqueur devant :** 89 sur 120 ressortent derrière lui (de 12 à 20 sur 20 selon la
        galerie ; déplacement imposé à 13.5 m/s, sans esquive : les échecs sont des prises sur les paliers et les poutres, ou
        un traqueur qui suit en parallèle) ;
      - **portée de la main (grille de 2 m, 48 323 points) :** 0 hors de portée hors des zones de parcours ; 83 hors de portée,
        tous sur des plateformes qui tombent (on n'y reste pas plus de 0.6 s) ; 32 abrités de ses mains levées (la prise les
        atteint).
17. **Combat de boss, après le playtest (D-058).**
    - **Leviers :** chaque piège s'arme d'abord. Il faut frapper ses **2 leviers lumineux** (orange, puis verts) avec un
      objet lancé ou l'onde de répulsion. Les leviers sont placés à l'écart du piège et à des hauteurs différentes ; ceux d'un
      piège armé restent allumés, et la cible n'apparaît qu'une fois le piège armé. Emplacements :
      - herse : galeries nord et sud à 6 m ;
      - pont : sol et galerie à 6 m de la poche est ;
      - lustre : sommets des tours de 11 m.
    - **Pression :** à chaque piège gagné, les recharges de ses attaques (prise, mains levées, pointe) × 0.7 (réglable,
      `pressurePerTrap`).
    - **Arène sur plusieurs hauteurs :**
      - galeries pleines à 6 m le long des murs nord et sud (hors des entrées), avec escaliers ;
      - deux tours de 11 m ;
      - galerie à 6 m et escalier dans la poche est ;
      - poche ouest de la herse à 22 m (sa main ne passait pas l'arche, mais elle touchait le fond de l'ancienne poche
        de 15 m) ;
      - décor le long des murs (torches, tonneaux, statues, échafaudages, sarcophages, grilles, tapisseries, chaînes).
    - **Contournement :** seulement par une traversée de la fosse en plateformes qui tombent (zone de parcours).
    - **Mesure** (joueur simulé : trajet imposé, sans esquive ni vrais sauts, invulnérable pour la mesure ; trajets vers les
      leviers en ligne droite à la vitesse donnée) :
      - **à 7.5 m/s :** gagné en 64.6 s. Lustre 21.8 s (armé en 17.3 s), herse 22.5 s (armée en 16.9 s), pont 11.8 s.
      - **à 13.5 et 18 m/s :** lustre 9.7 et 7.2 s, pont 6.9 et 5.0 s ; **la herse échoue en simulation**. Le joueur simulé
        file au fond de la poche, et le traqueur essaie ses prises depuis l'arche sans y entrer. Un vrai joueur doit
        l'appâter près de l'arche.
      - **Estimation** pour un joueur rapide : 35 à 45 s, sous la cible de 60 à 90 s (à valider).
12. **Tronçons de couloir (D-047).** Ils font 40 m au plus. Aucun ennemi n'apparaît dans le tronçon d'un joueur ni dans les
    tronçons voisins.

## Valeurs

| Réglage | Valeur | Où |
|---|---|---|
| Traqueur lâché / porte / Enragé | 180 / 600 / 900 s | `Zone_Test`, `Zone_Level01` |
| Prix de la porte de sortie | 200 âmes | idem |
| PV du joueur | 150 | `PlayerHealth_Default` |
| Hausse du marchand | +25 % par achat | `Economy_Default` |
| Eau profonde | 20 PV | `LevelLayout_Level01` |
| Combat : sonné, réarmement | 4 s, 4 s | `BossFight`, `BossTrap` (scène) |
| Tronçon de couloir | 40 m au plus | `ArenaLayout_*` |

## À valider (choix pris seul)

- **Après le playtest de l'étape 8 :**
  - **Pièces fermées :** elles sont sur les couloirs (annexes) et non plus sur les murs des salles, que les étages des
    modules bouchaient.
  - **Lancement d'un module :** la salle de départ n'en a pas (marchand) ; voie centrale des couloirs à 8 m (réglable) pour
    laisser passer le traqueur (7 m de large).
  - **Passages en hauteur :** à 4.5 m (saut, double saut et saut mural) ; ils servent de sortie de secours d'une salle
    vers son couloir.
  - **Galeries :** les paliers et poutres sont à portée de sa main ; à cause d'eux, environ 1 traversée sur 4 échoue si le
    joueur n'esquive pas.
  - **Test_Arena :** garde sa voie au sol à côté des pentes (scène de test).
  - **Combat de boss :** probablement trop court pour un joueur rapide (35 à 45 s estimées). Pistes : 3 leviers par piège,
    leviers plus loin, piège qui se réarme plus lentement, traqueur sonné moins longtemps.
  - **Level_01 :** recopiée une dernière fois depuis la base le 2026-11-14 (sans retouche de l'utilisateur) ; plus jamais
    ensuite.

- **Avant la sortie de la salle de départ, aucune vague ne tourne**, nulle part sur la carte, et pas seulement dans la salle.
- **La porte de sortie se paie une fois pour tous**, au prix fixe de 200 âmes, sans hausse.
- **Pendant le combat de boss, les vagues s'arrêtent.** Les pièges ne blessent pas les joueurs.
- Le marchand tire de nouveau ses 3 offres après chaque achat ; le soin compte comme un achat au marchand (hausse de 25 %).
- **Multiplicateurs de Moyenne et de Difficile** (×1.4, ×1.25…) : provisoires.
- **Le niveau 1 est une grille régulière** : retirer des couloirs et varier les salles, les hauteurs et les canaux ne
  suffit pas à casser cette régularité ; c'est à reprendre au level design.
- **Les zones de parcours** ne garantissent pas encore qu'on contourne le traqueur ; elles ne l'empêchent pas non plus
  ailleurs. C'est à régler en playtest.

## Historique

- 2026-11-13 : création (étape 8, blocs 1 à 6).
