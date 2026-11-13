# Run et niveaux

**Status:** in progress · **Updated:** 2026-11-13 · [Implementation](implementation.md) · [Testing](testing.md) · [GDD](../../GDD.md) · D-046, D-048 à D-053

## Résumé

Une run va du menu au niveau 1, puis à son combat de boss, puis au niveau suivant ou à la fin. Seule la difficulté Facile
est jouable : elle s'arrête après le niveau 1. Dans chaque niveau, le timer démarre quand un joueur quitte la salle de départ.
Le traqueur est lâché à 3 min et la porte de sortie se déverrouille à 10 min ; on la paie en âmes. L'Enragé commence à
15 min. En haut des escaliers de sortie, le combat de boss se gagne avec trois pièges. Le niveau 1 est généré en greybox
(800 × 800 m environ, 49 salles), avec une minimap et un brouillard de guerre.

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
