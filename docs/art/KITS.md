# Kits de décor des actes I à III (nuit du 2026-11-22)

Modélisés par script dans Blender (aucune génération IA, aucun crédit). Architecture en **UV monde** : les matériaux
de la bibliothèque (`M_Bib_*`, docs/art/MATERIAUX.md) s'appliquent sans dépliage ; **peinture de sommets** sur chaque pièce
(R saleté en bas, G mousse ou salpêtre en plaques, B teinte de la pièce) lue par les matériaux `*_Peint`. Conventions : Unity
+Y en haut, face visible de la pièce vers **+Z** (dos des murs et des objets muraux vers −Z, contre le mur), pivot au sol au centre de
l'emprise sauf mention, 1 unité = 1 m, échelle du jeu (joueur 1.8 m, traqueur 14 m, portes des modules 8 × 10 m).
Matériaux en double face (surfaces ouvertes). Repères vides exportés : `Feu*` (flammes), `Accroche_*` (points de gameplay).
Lumières : torches à l'ambiance intermédiaire du jeu (1.35 / 15.4 m, orangé (1, 0.6, 0.32)), bougies 0.6 / 5 m ; pour que
`main` les règle avec l'ambiance de la scène, ajouter `AmbianceLight` (Torche ou Lampe) sur les objets « Lumiere ».

## Acte I : caves (KitCaves, PR art-kit-caves)

43 pièces pour des salles organiques, pas carrées : parois de roche courbes, droites et en saillie (6, 10 et 18 m de haut), pilier de roche (point d'accroche de traction), murs maçonnés brisés, éboulis (le grand fait rampe vers un rebord), arches basse (joueur seulement, 3 × 3.5 m), haute (8 × 11 m, le traqueur passe) et en ruine, pont de pierre (16 m) et passerelle de bois pourri (12 m), canal et bassin avec eau, bord de gouffre (25 m), rebord de wall jump, plateforme de roche (dessus à 4 m), corniche de bois ; cellules (grille avec ouverture, porte de barreaux, lit, paille, seau, chaînes murales à poser **contre le mur de la cellule**), salle de garde (table avec chope et dés, banc, tabouret, brasero), réserve (caisses, sacs, étagère à jarres, tonneaux), puits (repère de l'acte), **porte de sortie d'acte** classique (12 × 13 m, deux battants de bois clouté qui pivotent à leurs gonds, sans lumière colorée), torche murale, applique à trois bougies, chandelles, candélabre.

Modèles `Assets/Art/Models/KitCaves/`, prefabs `Assets/Art/Prefabs/KitCaves/` (collisions : maillage pour l'architecture, boîte pour les petits objets ; sources de lumière : feu `VFX_Torche_Feu` et lumière ponctuelle sur les repères `Feu*`), scène `Assets/Art/Scenes/Vitrine_KitCaves.unity`, planche `docs/art/rendus/KitCaves/Planche_Kit_Caves.jpg`, une capture par pièce dans `docs/art/rendus/KitCaves/captures/`. Source : `Assets/Art/Source~/Kits/scripts/` (`kitlib.py` et le script du kit, Blender en ligne de commande).

| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |
|---|---|---|---|---|---|
| `SM_Cave_Paroi_Droite_6m` |  | 12.48 × 4.25 × 6.84 | 0.0 | 268 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Paroi_Courbe_6m` |  | 12.12 × 12.07 × 7.02 | 0.0 | 340 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Paroi_Droite_10m` |  | 12.46 × 4.66 × 10.92 | 0.0 | 388 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Paroi_Courbe_10m` |  | 12.05 × 12.1 × 10.99 | 0.0 | 492 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Paroi_Droite_18m` |  | 12.57 × 4.37 × 18.79 | 0.0 | 688 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Paroi_Courbe_18m` |  | 12.12 × 12.09 × 18.87 | 0.0 | 872 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Paroi_Saillie_10m` | Avancée rocheuse convexe (casse un mur droit, cache une jointure), 8 m de large, avance de 3 m. | 11.25 × 5.31 × 10.99 | 0.0 | 336 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_PilierRoche_18m` | Pilier de roche naturel (Ø ≈ 5 m) du sol au plafond : point d'accroche de traction, contournement. | 6.72 × 6.83 × 18.98 | 0.0 | 874 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_MurBrise_4m` |  | 8.01 × 3.08 × 4.14 | 0.0 | 480 | `M_Bib_Moellons_Peint` |
| `SM_Cave_MurBrise_8m` |  | 8.01 × 3.04 × 8.18 | 0.0 | 480 | `M_Bib_Moellons_Peint` |
| `SM_Cave_Eboulis_Petit` |  | 4.02 × 3.56 × 0.75 | 0.0 | 432 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Eboulis_Grand` | Cône d'éboulis de 10 m (monte à 4.5 m) : rampe naturelle vers un rebord, gros blocs. | 10.42 × 8.64 × 5.71 | -1.2 | 840 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_ArcheBasse` | Passage bas de joueur : ouverture 3 × 3.5 m dans un massif maçonné de 8 × 6 m (le traqueur ne passe pas). | 8.0 × 2.54 × 6.0 | 0.0 | 376 | `M_Bib_Moellons_Peint` |
| `SM_Cave_ArcheHaute` | Grande arche brisée : ouverture 8 × 11 m (porte des modules, le traqueur passe), massif de 14 × 15 m. | 14.0 × 3.14 × 15.0 | 0.0 | 504 | `M_Bib_PierreTaillee_Peint` |
| `SM_Cave_ArcheRuine` | Arche brisée en ruine (moitié droite effondrée), ouverture 6 m : cadre de vue, repère. | 11.28 × 3.37 × 9.0 | 0.0 | 562 | `M_Bib_Moellons_Peint` |
| `SM_Cave_PontPierre_16m` | Pont de pierre en arc au-dessus de l'eau ou du vide : 16 m, tablier de 4 m de large (dessus à 0), arche de 12 m, | 16.0 × 4.4 × 11.8 | -11.0 | 296 | `M_Bib_Dalles_Sales`, `M_Bib_PierreTaillee_Peint` |
| `SM_Cave_PontBois_12m` | Passerelle de bois pourri : 12 m × 2.6 m, planches irrégulières (deux manquent), poteaux, mains courantes de corde. | 12.22 × 2.92 × 4.3 | -3.0 | 1056 | `M_Bib_BoisPourri`, `M_Corde` |
| `SM_Cave_Canal_8m` | Canal maçonné : 8 m de long, 4 m d'eau entre deux margelles de 1 m, fond à -2.5 m, eau à -0.8 m. | 8.0 × 6.25 × 3.16 | -3.0 | 58 | `M_Bib_Moellons_Peint`, `M_Bib_Dalles_Sales`, `M_Eau_Toon` |
| `SM_Cave_Bassin` | Bassin rond de citerne (Ø 9 m intérieur), margelle de 0.9 m, eau à -0.6 m, fond à -3 m. | 10.8 × 10.8 × 3.9 | -3.0 | 276 | `M_Bib_PierreTaillee_Peint`, `M_Eau_Toon`, `M_Bib_Moellons_Peint` |
| `SM_Cave_Gouffre_Bord_12m` | Lèvre de gouffre : sol de 12 × 3 m qui s'arrête sur un bord déchiqueté, paroi qui plonge à -25 m (fond noir). | 12.76 × 7.0 × 25.0 | -25.0 | 726 | `M_Bib_Terre`, `M_Bib_RocheCave_Peint`, `M_Fond_Noir` |
| `SM_Cave_Rebord_4m` | Rebord de roche accroché à une paroi : 4 m × 1.4 m, dessus plat (on s'y hisse), dessous en encorbellement. | 4.17 × 1.58 × 1.66 | -1.66 | 156 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Plateforme_3m` | Petite plateforme de roche : dessus 3 × 3 m à 4 m (atteint au double saut + hissage), pied en colonne. | 3.72 × 4.0 × 4.0 | 0.0 | 204 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Corniche_Bois_6m` | Corniche de planches sur consoles de bois, fixée à un mur : 6 × 1.6 m (route de wall jump), dessus à 0. | 5.97 × 1.63 × 1.68 | -1.68 | 216 | `M_Bib_BoisPourri` |
| `SM_Cave_Cellule_Grille` | Façade de cellule : grille de fer de 6 × 5 m (barreaux Ø 6 cm tous les 15 cm, traverses), ouverture de porte de | 6.6 × 0.8 × 5.6 | 0.0 | 480 | `M_Bib_FerRouille`, `M_Bib_PierreTaillee_Peint` |
| `SM_Cave_Cellule_Porte` | Porte de cellule en barreaux, 1.4 × 2.4 m, gonds à gauche (pivot au gond), serrure. | 1.4 × 0.14 × 2.36 | 0.02 | 156 | `M_Bib_FerRouille` |
| `SM_Cave_Lit` | Lit de cellule : bat-flanc de planches sur deux tréteaux, 2 × 0.9 m, dessus à 0.5 m, paillasse et couverture. | 2.0 × 0.9 × 0.63 | 0.0 | 176 | `M_Bib_BoisPourri`, `M_Bib_HerbeSeche`, `M_Kit_Toile` |
| `SM_Cave_Paille` | Tas de paille au sol (1.6 m), à mettre dans un coin de cellule. | 1.69 × 1.22 × 0.36 | -0.05 | 120 | `M_Bib_HerbeSeche` |
| `SM_Cave_Seau` | Seau de bois cerclé de fer (Ø 0.32 × 0.34 m). | 0.32 × 0.31 × 0.34 | 0.0 | 192 | `M_Bib_BoisPourri`, `M_Bib_FerRouille` |
| `SM_Cave_Chaines_Mur` | Chaînes murales de prisonnier : plaque d'ancrage à 1.9 m, deux chaînes qui pendent en U vers des fers ouverts. | 1.08 × 0.2 × 1.36 | 0.69 | 324 | `M_Bib_FerRouille` |
| `SM_Cave_Banc` |  | 2.0 × 0.4 × 0.47 | 0.0 | 36 | `M_Bib_BoisNeuf` |
| `SM_Cave_Tabouret` |  | 0.44 × 0.43 × 0.48 | -0.01 | 72 | `M_Bib_BoisNeuf` |
| `SM_Cave_TableGarde` | Table de garde (2.2 × 1 m, dessus à 0.8 m) avec chope, écuelle, dés : vie récente, partie interrompue. | 2.2 × 1.0 × 0.95 | 0.0 | 216 | `M_Bib_BoisNeuf`, `M_Bib_FerRouille`, `M_Os` |
| `SM_Cave_Brasero` | Brasero sur trépied (Ø 0.8 m, coupe à 1 m) : source de lumière de la salle de garde (le prefab ajoute le feu). | 0.9 × 0.86 × 1.06 | -0.01 | 196 | `M_Bib_FerRouille`, `M_Fond_Noir` |
| `SM_Cave_Caisses_Pile` | Pile de caisses de réserve (décor, non attrapables) : 3 m de large, 2.4 m de haut. | 3.11 × 1.37 × 2.6 | 0.0 | 216 | `M_Bib_BoisPourri`, `M_Bib_BoisNeuf` |
| `SM_Cave_Sacs` | Sacs de grain affaissés (tas de 1.8 m). | 1.39 × 1.26 × 0.7 | 0.0 | 400 | `M_Kit_Toile` |
| `SM_Cave_Etagere` | Étagère de réserve 2.4 × 0.6 × 2.6 m, pots et jarres. | 2.4 × 0.6 × 2.6 | 0.0 | 1136 | `M_Bib_BoisPourri`, `M_Bib_Briques_Peint` |
| `SM_Cave_Tonneaux_Pile` | Trois tonneaux couchés sur un chantier de bois + un debout (réserve, cave à vin). | 3.1 × 1.2 × 0.95 | 0.0 | 400 | `M_Bib_BoisNeuf`, `M_Bib_FerRouille`, `M_Bib_BoisPourri` |
| `SM_Cave_Puits` | Puits (repère de l'acte I, refait au lot 3) : margelle lisse Ø 3.2 m sans marche ni arête vive, eau à -1.5 m, fond à -2.4 m. | 4.58 × 3.2 × 6.45 | -2.41 | 1036 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Terre`, `M_Bib_BoisNeuf`, `M_Bib_FerRouille` |
| `SM_Cave_PorteSortie` | Porte de sortie de l'acte I : encadrement de pierre de taille 12 × 13 m, ouverture 8 × 10 m en plein cintre, deux battants de bois épais cloutés de fer (pivots aux gonds : objets enfants Battant_G et Battant_D), sceau de lune de sortie en fer sur les battants (lot 3). | 12.41 × 3.2 × 13.0 | 0.0 | 1432 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Cave_Torche_Murale` | Torche sur bras de fer scellé au mur (dos à y = 0) : flamme à 0.75 m du mur, à la hauteur du pivot + 0.55 m. | 0.18 × 0.76 × 0.75 | -0.25 | 92 | `M_Bib_FerRouille`, `M_Bib_BoisNeuf`, `M_Chiffon_Brule` |
| `SM_Cave_Applique` | Applique de fer à trois bougies (dos à y = 0), 0.6 m de large : flammes à « Feu_1 » … « Feu_3 ». | 0.62 × 0.36 × 0.54 | -0.25 | 240 | `M_Bib_FerRouille`, `M_Kit_Cire` |
| `SM_Cave_Chandelles` | Groupe de chandelles fondues au sol ou sur une table (0.4 m), coulures de cire : flammes « Feu_1 » … « Feu_5 ». | 0.42 × 0.34 × 0.29 | -0.03 | 220 | `M_Kit_Cire` |
| `SM_Cave_Candelabre` | Candélabre de fer sur pied (1.6 m), cinq bras : flammes « Feu_1 » … « Feu_5 ». | 0.66 × 0.56 × 1.71 | -0.02 | 404 | `M_Bib_FerRouille`, `M_Kit_Cire` |

## Acte II : étages hauts du château (KitChateau, PR art-kit-chateau)

31 pièces, avec des plafonds plus hauts et de grands volumes :
- **murs** : 8 × 12 m et 8 × 20 m, en pierre de taille ou en plâtre sur lambris ;
- **haute fenêtre** en lancette (baie de 3.2 × 11 m) ;
- **pilier** fasciculé de 20 m et **colonne** de 12 m ;
- **travée de voûte d'ogives** de 8 × 8 m et **arcade** de passage ;
- **galerie haute** sur consoles et **balcon** ;
- **grand escalier** : 8 m de large, monte de 8 m ;
- **grande porte d'entrée du château**, qui mène à l'acte III : façade de 20 × 22 m, portail à voussures de 9 × 13 m, deux battants qui pivotent, perron ;
- **mobilier et décor** : chandelier suspendu à 12 bougies, tenture déchirée, bibliothèque murale, tableau sombre, meubles renversés (chaise, table, armoire), table de banquet, fauteuil, lit à baldaquin, coffre, miroir brisé ;
- **chapelle** : bancs, autel avec cierges, lutrin, rosace ;
- **salle de bal** : estrade des musiciens.

**Rayon de lune** (prefab `Chateau_RayonLune`) :
- un volume de lumière en tronc de cône elliptique (shader additif `ItHunts/Lumiere`) qui s'efface vers le sol et sur les bords, avec des bandes de poussière qui descendent ;
- un projecteur de lune (0.62, 0.7, 1) de 4 / 27 m, avec ombres ;
- de la **poussière blanc-gris** en suspension : 260 particules, matériau `M_Kit_Poussiere`.

À poser à la fenêtre, origine au milieu de la baie : le rayon descend vers +Z.

L'acte II est un peu plus éclairé que les caves : ambiance (0.07, 0.075, 0.1), lune à 0.35, brouillard de 14 à 110 m. Le contrôle s'est fait dans un grand hall assemblé : `docs/art/rendus/KitChateau/Planche_GrandHall_RayonsDeLune.jpg`.

Modèles `Assets/Art/Models/KitChateau/`, prefabs `Assets/Art/Prefabs/KitChateau/` (collisions : maillage pour l'architecture, boîte pour les petits objets ; sources de lumière : feu `VFX_Torche_Feu` et lumière ponctuelle sur les repères `Feu*`), scène `Assets/Art/Scenes/Vitrine_KitChateau.unity`, planche `docs/art/rendus/KitChateau/Planche_Kit_Chateau.jpg`, une capture par pièce dans `docs/art/rendus/KitChateau/captures/`. Source : `Assets/Art/Source~/Kits/scripts/` (`kitlib.py` et le script du kit, Blender en ligne de commande).

| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |
|---|---|---|---|---|---|
| `SM_Chateau_Mur_8x12` | Mur de salle 8 × 12 m (1 m d'épaisseur, face vers +Z) : pierre de taille, plinthe, corniche à 11 m. | 8.0 × 1.7 × 12.0 | 0.0 | 60 | `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Mur_8x20` | Mur de grand hall 8 × 20 m : pierre de taille, plinthe, corniche à 19 m. | 8.0 × 1.7 × 20.0 | 0.0 | 60 | `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Mur_Platre_8x12` | Mur d'appartement 8 × 12 m : plâtre fissuré au-dessus d'un lambris de pierre de 3.2 m (chambres, bibliothèque). | 8.0 × 1.7 × 12.0 | 0.0 | 72 | `M_Bib_PlatreFissure_Sale`, `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Fenetre_Haute` | Haute fenêtre en lancette dans un mur de 8 × 20 m : baie de 3.2 m de large, de 4 m à 15 m, remplage et vitrage | 8.0 × 1.7 × 20.0 | 0.0 | 228 | `M_Bib_PierreTaillee_Peint`, `M_Kit_Vitrail` |
| `SM_Chateau_Pilier_20m` | Pilier gothique fasciculé de 20 m (Ø 2.2 m) : base moulurée, quatre colonnettes, chapiteau à feuillages simplifié, | 3.2 × 3.2 × 19.2 | 0.0 | 428 | `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Colonne_12m` | Colonne fine de 12 m (Ø 0.9 m) : galeries, arcades, chapelle. | 1.6 × 1.6 × 12.0 | 0.0 | 200 | `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Voute_8x8` | Travée de voûte d'ogives 8 × 8 m (posée sur les tailloirs à 19.4 m : pivot au niveau des tailloirs, au centre) : | 8.69 × 8.69 × 5.5 | -0.5 | 990 | `M_Bib_PlatreFissure_Sale`, `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Arcade_8x12` | Arcade de passage entre deux halls : baie en arc brisé de 6 × 10 m dans un mur de 8 × 14 m. | 8.0 × 1.94 × 14.0 | 0.0 | 440 | `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Galerie_8m` | Galerie haute de 8 m sur consoles (dessus à 0, profondeur 4 m, dos contre le mur en y = 0) : dallage, balustrade | 8.0 × 4.05 × 4.65 | -3.55 | 1470 | `M_Bib_Dalles_Sales`, `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_Balcon` | Balcon de 4 × 2.4 m en encorbellement (dessus à 0, dos au mur), balustrade sur trois côtés : point de vue, cible | 4.2 × 3.4 × 3.5 | -2.4 | 1196 | `M_Bib_Dalles_Sales`, `M_Bib_PierreTaillee_Peint` |
| `SM_Chateau_GrandEscalier` | Grand escalier droit de 8 m de large qui monte de 8 m (32 marches de 0.25 × 0.5 m), tapis rouge central, rampes | 9.3 × 20.45 × 9.39 | 0.0 | 6116 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Tapis`, `M_Bib_Dalles_Sales` |
| `SM_Chateau_GrandeEntree` | Grande porte d'entrée du château (mène à l'acte III) : façade de 20 × 22 m, portail en arc brisé de 9 × 13 m à voussures, deux battants de chêne ferré (Battant_G, Battant_D, pivots aux gonds) portant le sceau de lune de sortie (lot 3), perron de 5 marches, deux contreforts. | 20.2 × 6.2 × 22.01 | -0.01 | 1656 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Chateau_Chandelier` | Chandelier suspendu : couronne de fer Ø 3 m à 12 bougies, pendu par une chaîne de 6 m (pivot au point | 3.14 × 3.14 × 6.9 | -6.9 | 1106 | `M_Bib_FerRouille`, `M_Kit_Cire` |
| `SM_Chateau_Tenture` | Tenture lourde de 3 × 9 m sur tringle (pivot à la tringle, contre un mur), plis profonds, bas effrangé et déchiré. | 3.6 × 0.43 × 9.16 | -9.0 | 892 | `M_Bib_Tenture`, `M_Bib_FerRouille` |
| `SM_Chateau_Bibliotheque` | Bibliothèque murale de 4 × 0.7 × 5 m (dos au mur, face vers +Z) : 7 rayons de livres serrés, quelques vides et | 4.2 × 1.0 × 5.65 | 0.0 | 3780 | `M_Bib_BoisNeuf`, `M_Bib_Tenture`, `M_Bib_BoisPourri`, `M_Kit_Toile_Peinte`, `M_Kit_Toile`, `M_Bib_Tapis` |
| `SM_Chateau_Tableau` | Grand tableau sombre de 2 × 2.8 m dans un cadre doré éteint (accroché au mur, pivot au clou, face vers +Z). | 2.16 × 0.14 × 2.9 | -2.85 | 60 | `M_Kit_Toile_Peinte`, `M_Bib_Trim` |
| `SM_Chateau_Chaise_Renversee` | Chaise à haut dossier couchée au sol (meuble renversé). | 0.92 × 1.6 × 0.5 | 0.02 | 72 | `M_Bib_BoisNeuf` |
| `SM_Chateau_Chaise` |  | 0.5 × 0.5 × 1.52 | 0.0 | 72 | `M_Bib_BoisNeuf` |
| `SM_Chateau_Table_Renversee` | Longue table de banquet (4 × 1.2 m) renversée sur le flanc : abri bas, couvert de fortune. | 4.0 × 0.82 × 1.2 | 0.0 | 72 | `M_Bib_BoisNeuf` |
| `SM_Chateau_Armoire_Tombee` | Armoire de 2 × 0.7 × 2.6 m tombée en avant, une porte arrachée à côté (gros couvert, saut). | 3.83 × 3.99 × 0.7 | 0.0 | 72 | `M_Bib_BoisNeuf`, `M_Kit_Toile` |
| `SM_Chateau_TableBanquet` | Table de banquet de 6 × 1.4 m (dessus à 0.85 m), nappe déchirée, chandeliers éteints, assiettes. | 6.0 × 1.5 × 1.34 | 0.0 | 784 | `M_Bib_BoisNeuf`, `M_Kit_Toile`, `M_Bib_FerRouille`, `M_Kit_Cire`, `M_Kit_Os` |
| `SM_Chateau_Fauteuil` | Fauteuil de velours à oreilles (salon, chambre). | 1.0 × 0.88 × 1.65 | 0.0 | 72 | `M_Bib_Tenture` |
| `SM_Chateau_LitBaldaquin` | Lit à baldaquin (2.4 × 2.2 m, ciel à 3.2 m), rideaux déchirés tombant d'un côté (chambres). | 2.5 × 2.37 × 3.31 | 0.0 | 320 | `M_Bib_BoisNeuf`, `M_Kit_Toile`, `M_Bib_Tenture` |
| `SM_Chateau_Coffre` | Coffre de chambre en bois ferré (1.2 × 0.6 × 0.7 m), couvercle entrouvert (décor, pas le coffre du jeu). | 1.22 × 0.82 × 0.82 | -0.01 | 48 | `M_Bib_BoisNeuf`, `M_Bib_FerRouille` |
| `SM_Chateau_MiroirBrise` | Miroir sur pied (0.9 × 2 m), verre brisé (fragments sombres), cadre de bois sculpté. | 1.0 × 1.17 × 2.15 | 0.0 | 156 | `M_Kit_Vitrail`, `M_Bib_BoisNeuf` |
| `SM_Chateau_Banc_Eglise` | Banc d'église de 4 m (dossier, prie-Dieu) : rangées de la chapelle, couvert bas. | 4.02 × 0.83 × 1.25 | 0.0 | 60 | `M_Bib_BoisNeuf` |
| `SM_Chateau_Autel` | Autel de pierre (3 × 1.4 × 1.1 m) sur deux marches, nappe, chandeliers ; Feu_1 et Feu_2 sur les cierges. | 5.0 × 3.0 × 2.55 | 0.0 | 224 | `M_Bib_Marbre`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Toile`, `M_Bib_FerRouille`, `M_Kit_Cire` |
| `SM_Chateau_Lutrin` | Lutrin de fer (1.4 m) avec un livre ouvert. | 0.6 × 0.48 × 1.39 | 0.0 | 88 | `M_Bib_FerRouille`, `M_Kit_Toile` |
| `SM_Chateau_Rosace` | Rosace de chapelle Ø 7 m (au mur, face vers +Z, pivot au centre) : remplage rayonnant, vitrail sombre. | 7.0 × 0.75 × 7.0 | -3.5 | 696 | `M_Bib_PierreTaillee_Peint`, `M_Kit_Vitrail` |
| `SM_Chateau_Estrade_Musiciens` | Estrade des musiciens de la salle de bal : 8 × 4 m, 1.2 m de haut, trois marches, pupitres renversés. | 8.0 × 5.27 × 2.47 | 0.0 | 1700 | `M_Bib_BoisNeuf`, `M_Bib_FerRouille` |

## Acte III : jardin et arène du boss (KitJardin, PR art-kit-jardin)

25 pièces, plus le ciel et la brume. Le jardin, à l'abandon devant le château, est plus ouvert et un peu plus lumineux, sous la lune.

**Arbres et végétation :**
- **gros arbres morts** : grand (18 m, tronc de Ø 2.6 m), penché (rampe et mur de wall jump) et tordu. Racines, branches noueuses, `Accroche_Traction` à mi-tronc ;
- haies de 6 m et massifs ;
- **lisière de forêt dense** de 20 m : limite naturelle.

**Clôtures et murs** :
- palissade de jardin, grille d'enceinte en fer forgé et portail, à poser **le long des allées et des parcelles** ;
- muret ;
- **mur d'enceinte** de 9 m et **falaise** de 24 m : limites évidentes, jamais de mur invisible ;
- allée de gravier bordée de pierre.

**Statues, tombes, fabriques :**
- statue voilée de 5 m et statue géante de 11 m (points de traction) ;
- fontaine à vasques avec eau croupie ;
- tombes (stèle penchée, croix) et caveau ;
- serre en ruine (ossature de fer, carreaux brisés) ;
- kiosque octogonal (toit atteignable par traction).

**Arène du boss en contrebas :** escalier de descente de 12 m de large (8 m de dénivelé), piliers brisés (`Accroche_Traction`), pans de mur pour se couvrir, buttes de relief.

**Ciel nocturne** (prefab `Jardin_Ciel`, shader `ItHunts/Ciel`) :
- dégradé en aplats, étoiles qui scintillent ;
- **lune** avec ses mers et un halo en paliers, et une lumière de lune qui vient de sa direction ;
- nuages peints qui dérivent et passent devant elle.

**Brume au sol** (prefab `Jardin_Brume`) : nappes basses de 6 à 12 m qui dérivent, sur une zone de 40 × 40 m.

Éclairage de l'acte III : ambiance (0.08, 0.09, 0.12), lune à 0.6, brouillard de 18 à 140 m.

Contrôle : deux salles assemblées, l'allée des statues et l'arène du boss (`docs/art/rendus/KitJardin/Planche_Jardin_Arene_Ciel.jpg`).

Modèles `Assets/Art/Models/KitJardin/`, prefabs `Assets/Art/Prefabs/KitJardin/` (collisions : maillage pour l'architecture, boîte pour les petits objets ; sources de lumière : feu `VFX_Torche_Feu` et lumière ponctuelle sur les repères `Feu*`), scène `Assets/Art/Scenes/Vitrine_KitJardin.unity`, planche `docs/art/rendus/KitJardin/Planche_Kit_Jardin.jpg`, une capture par pièce dans `docs/art/rendus/KitJardin/captures/`. Source : `Assets/Art/Source~/Kits/scripts/` (`kitlib.py` et le script du kit, Blender en ligne de commande).

| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |
|---|---|---|---|---|---|
| `SM_Jardin_ArbreMort_Grand` | Gros arbre mort de 18 m, tronc Ø 2.6 m (wall jump d'un tronc à l'autre), racines, branches noueuses. | 8.61 × 12.48 × 26.72 | -0.62 | 2029 | `M_Bib_Ecorce` |
| `SM_Jardin_ArbreMort_Penche` | Arbre mort penché de 14 m (Ø 2.2 m) : le tronc incliné sert de rampe de course et de mur de wall jump. | 12.91 × 12.75 × 20.44 | -0.6 | 1729 | `M_Bib_Ecorce` |
| `SM_Jardin_ArbreMort_Tordu` | Arbre mort tordu de 11 m (Ø 2 m), couronne de branches basses (silhouette sur la lune). | 9.68 × 10.45 × 15.51 | -0.59 | 2153 | `M_Bib_Ecorce` |
| `SM_Jardin_Haie_6m` | Haie taillée devenue sauvage : 6 × 1.4 m, 2.6 m de haut, faces bosselées, dessus déchiqueté (à poser le long d'une allée). | 5.94 × 1.46 × 2.49 | 0.0 | 360 | `M_Kit_Feuillage` |
| `SM_Jardin_Massif` | Massif d'arbustes morts (Ø 3 m) au coin d'une parcelle : masse basse de feuillage sombre bosselée et tiges sèches. | 2.78 × 2.54 × 2.76 | 0.0 | 264 | `M_Kit_Feuillage`, `M_Bib_Ecorce` |
| `SM_Jardin_Foret_Lisiere_20m` | Lisière de forêt dense (20 m de front, 10 m de profondeur) : troncs serrés devant deux rideaux de feuillage sombre (5 et 9 m, crêtes déchiquetées) et un roncier continu. | 20.58 × 10.5 × 22.42 | -0.53 | 1378 | `M_Bib_Ecorce`, `M_Kit_Feuillage` |
| `SM_Jardin_Palissade_4m` | Palissade de jardin en planches pointues, 4 m × 1.6 m, deux planches manquantes, une penchée (le long d'une allée | 4.04 × 0.21 × 1.91 | -0.1 | 230 | `M_Bib_BoisPourri` |
| `SM_Jardin_Grille_4m` | Grille d'enceinte en fer forgé, 4 m × 3.5 m, barreaux à pointes de lance, volutes, entre deux piliers de pierre. | 5.24 × 0.84 × 4.3 | 0.0 | 559 | `M_Bib_PierreTaillee_Peint`, `M_Bib_FerRouille` |
| `SM_Jardin_Portail` | Portail de fer à deux vantaux (4 m) entre deux grands piliers à vases, un vantail entrouvert : entrée d'allée. | 6.1 × 1.66 × 5.4 | 0.0 | 432 | `M_Bib_PierreTaillee_Peint`, `M_Bib_FerRouille` |
| `SM_Jardin_Muret_4m` | Muret de pierre de 4 m × 0.6 m, 1 m de haut, chaperon arrondi, pierres manquantes (couvert bas, rebord de parcelle). | 4.0 × 0.72 × 1.04 | 0.0 | 84 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint` |
| `SM_Jardin_MurEnceinte_8m` | Mur d'enceinte du domaine : 8 m de long, 9 m de haut, contreforts, chaperon : limite évidente (on ne grimpe pas : | 8.2 × 2.4 × 9.3 | 0.0 | 48 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint` |
| `SM_Jardin_Falaise_16m` | Falaise de 16 m de front, 24 m de haut, herbe sèche au sommet : limite naturelle (arène en contrebas, bord du jardin). | 16.67 × 6.38 × 24.8 | 0.0 | 616 | `M_Bib_RocheCave_Peint`, `M_Bib_HerbeSeche` |
| `SM_Jardin_Chemin_8m` | Allée de gravier de 8 × 3 m, bordures de pierre, herbe qui gagne les bords (pivot au centre, dessus à 0.05). | 8.0 × 3.3 × 0.22 | -0.05 | 204 | `M_Bib_Gravier_Herbe`, `M_Bib_PierreTaillee_Peint` |
| `SM_Jardin_Statue_Pleureuse` | Statue de 5.4 m (figure voilée qui se cache le visage) sur piédestal : repère du jardin, point de traction. | 1.6 × 1.6 × 4.96 | 0.0 | 404 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Mortier` |
| `SM_Jardin_Statue_Geante` | Statue géante de 12 m dans l'arène : couvert, point de traction haut (`Accroche_Traction`). | 3.26 × 3.17 × 10.79 | 0.0 | 404 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Mortier` |
| `SM_Jardin_Fontaine` | Fontaine (Ø 8 m) : bassin octogonal avec eau croupie, vasque à 2.5 m sur colonne, vasque haute, statuette. | 8.0 × 8.0 × 5.77 | -0.4 | 414 | `M_Bib_PierreTaillee_Peint`, `M_Eau_Toon`, `M_Bib_Mortier` |
| `SM_Jardin_Tombe_Stele` | Stèle penchée (0.9 × 1.6 m) sur un tertre, fleurs mortes : à aligner en rangées tordues dans le cimetière. | 1.14 × 2.24 × 2.15 | -0.2 | 122 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Terre_Racines` |
| `SM_Jardin_Tombe_Croix` | Croix de pierre (2.2 m) ébréchée sur socle, tertre. | 1.0 × 2.22 × 2.4 | -0.2 | 116 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Terre_Racines` |
| `SM_Jardin_Caveau` | Caveau familial (5 × 6 × 5 m) : fronton, porte de fer entrouverte sur le noir, colonnes ; couvert, toit escaladable. | 5.6 × 6.67 × 5.6 | 0.0 | 158 | `M_Bib_PierreTaillee_Peint`, `M_Fond_Noir`, `M_Bib_FerRouille` |
| `SM_Jardin_Serre_Ruine` | Serre en ruine (10 × 6 m, 5 m) : soubassement de briques, ossature de fer, vitres brisées (quelques carreaux | 10.4 × 6.4 × 5.26 | 0.0 | 508 | `M_Bib_Briques_Peint`, `M_Bib_FerRouille`, `M_Kit_Vitrail` |
| `SM_Jardin_Kiosque` | Kiosque octogonal (Ø 8 m) : plancher surélevé de 1 m, 8 colonnes de fer, toit en pavillon percé, rambarde | 9.2 × 9.9 × 9.2 | 0.0 | 350 | `M_Bib_PierreTaillee_Peint`, `M_Bib_FerRouille` |
| `SM_Arene_EscalierDescente` | Grand escalier de descente vers l'arène : 12 m de large, descend de 8 m (32 marches), murs d'échiffre en ruine. | 13.6 × 16.0 × 10.0 | -8.5 | 576 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Moellons_Peint` |
| `SM_Arene_PilierBrise` | Pilier brisé de 10 m (Ø 2 m), sommet cassé en biais, tambours tombés : point d'accroche de traction de l'arène. | 5.2 × 5.4 × 9.9 | 0.0 | 146 | `M_Bib_PierreTaillee_Peint` |
| `SM_Arene_Couvert` | Pan de mur effondré (6 × 1.2 m, 3.5 m) : couvert contre les attaques du boss (pas contre le traqueur). | 6.53 × 3.39 × 3.44 | 0.0 | 312 | `M_Bib_Moellons_Peint` |
| `SM_Arene_Butte` | Butte de terre et de roche (14 × 10 m, 3 m de haut) : relief de l'arène, pentes de glissade, vue dégagée. | 13.96 × 10.08 × 6.26 | -2.2 | 464 | `M_Bib_Terre_Racines`, `M_Bib_RocheCave_Peint` |

## Nuit 4, lot 3 : pièces pour un level design en hauteur (branche art-kits-3)

68 nouvelles pièces pour jouer sur la verticalité dans les trois actes, et 3 pièces existantes modifiées. Chaque type a sa version dans les matériaux de son acte :
- **caves** : moellons, pierre de taille, roche, bois pourri, cordes ;
- **château** : pierre claire, parquet, dalles claires, marbre, tapis, laiton éteint ;
- **jardin** : moellons, pierre de taille, bois, fer forgé, lierre.

**Pièces communes aux trois actes** :
- escaliers droit, à quart tournant et en colimaçon (Ø 4 m, 3.6 m aux caves) ;
- mezzanine (dessus à 5 m), coursive, passerelle entre étages ;
- porte de joueur en hauteur (seuil à 5 m), porte étroite que le traqueur franchit en se faufilant ;
- échelle, mur à rebords pour wall jumps simples, mur escaladable ;
- porte verrouillée de salle payante.

**Pièces propres à un acte** :
- **caves** : trémie, cheminée de roche, balcon de bois, trois cellules d'apparition du traqueur (herse, barricade, fosse), puits refait ;
- **château** : trémie balustrée, porte de sortie, cellule du traqueur, trois hautes fenêtres en lancettes avec repère `RayonLune`, chandeliers petit, moyen et grand, lustre à pampilles, deux appliques, candélabre sur pied ;
- **jardin** : puits, porte de sortie, volière du traqueur, falaise à rebords, passage et fente dans la roche, deux terrasses, ponts de pierre et de bois, tour, brèche et chemin de ronde du mur d'enceinte.

**Mesures de jeu respectées** (ART-HANDOFF § 3 et § 5 ter) :
- marches de 18.75 à 20 cm sur des girons de 30 cm (38 cm au jardin), avec une rampe de collision lisse `Col_Rampe` sous les nez de marche : 33.7° (27.8° au jardin), 34.4° au plus dans les colimaçons (mesurée) ;
- rebords plats d'au moins 0.6 m de profondeur, espacés de 2.2 à 3.5 m (saut et hissage) ;
- portes étroites de 4.4 m qui contiennent le gabarit de 4 × 5.25 m du traqueur baissé ;
- plateformes de moins de 10 m de profondeur : le traqueur ne s'y pose pas.

**Enfants exportés avec les pièces** :
- `Col_*` : collision seule, rendu retiré par KitBuilder ;
- `Garde_Corps` : garde-corps, à mettre sur le calque « garde » ;
- `Deco_SansCollision` : décor sans collision ;
- `Col_Escalier` (relecture du lot 3) : volume de collision des 9 escaliers (joues, paliers, dos, noyau et sous-face des colimaçons, échiffres et socles du jardin, éboulis des caves), à côté de `Col_Rampe` ;
- `Deco_Eau` (relecture du lot 3) : eau des puits, sans collider ; percer le sol de la salle sur Ø 3.2 m sous le puits ;
- parties mobiles : `Battant_G` et `Battant_D`, `Vantail`, `Serrure`, `Herse`, `Trappe_G` et `Trappe_D`, avec leur pivot à l'axe.

**Repères exportés** :
- `Accroche_*` : points de gameplay ;
- `Feu_*` : flammes (`Feu_Torche` pour les deux torches du château ajoutées à la relecture) ;
- `RayonLune` : à la fenêtre ;
- `Apparition_Traqueur`, `Porte_Traqueur`, `Passage_*`, `Appui_*`.

**Relecture du lot 3** (branche `art-kits-3-b`) : touffes de lierre retirées partout (cartes de lierre plates à la place), haie, massif et lisière du jardin sans boules, torches du château (`SM_Chateau_Torche_Murale`, `SM_Chateau_Torchere`), murs d'escalade à prises intégrées à l'appareil, porte verrouillée à chambranle de porte et flamme à trois langues, lustre en `M_Kit_Cristal`, vitraux au plomb en losanges et médaillons, parties mobiles qui ne traversent plus la maçonnerie. Planche : `docs/art/rendus/Nuit4/Lot3/Planche_Corrections.jpg` ; contrôles : `controle_corrections_maillages.json` et `controle_parties_mobiles.json` dans le même dossier.

**Où trouver le reste** :
- dimensions, pivots, axes et angles d'ouverture : docs/art/INTEGRATION-PROPS.md, section « Nuit 4, lot 3 » ;
- planche : `docs/art/rendus/Nuit4/Lot3/Planche_Kits_Lot3.jpg`, une image par pièce dans le même dossier ;
- sources : `kit_hauteur.py`, module commun importé par les trois scripts de kit ; rendus : `rendu_kits.py` et `planche_kits.py` ; catalogue : `kit_doc_lot3.py`.

### Acte I : caves (18 pièces, `KitCaves`)

| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |
|---|---|---|---|---|---|
| `SM_Cave_Escalier_Droit_4m` | Escalier droit plein : monte de 4 m (marches de 20 cm ou moins), 3 m de large, palier de 1.5 m ; rampe de collision sous les nez. | 4.24 × 7.7 × 4.01 | -0.01 | 946 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales`, `M_Bib_RocheCave_Peint` |
| `SM_Cave_Escalier_Tournant_4m` | Escalier à quart tournant : deux volées de 2 m, palier carré, palier en haut ; rampes de collision. | 7.5 × 6.0 × 4.0 | 0.0 | 2410 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Cave_Escalier_Colimacon` | Escalier en colimaçon (Ø 4 m, 3.6 m aux caves) : monte de 6.4 m en deux tours, 16 marches de 20 cm par tour, palier de 100° ; rampe hélicoïdale. | 3.6 × 3.6 × 7.7 | 0.0 | 1323 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Moellons_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Cave_Mezzanine_8x6` | Mezzanine de 8 × 6 m, dessus à 5 m (double saut et hissage), contre un mur ; garde-corps devant et à gauche, droite ouverte. | 8.08 × 6.0 × 5.0 | 0.0 | 592 | `M_Bib_BoisPourri`, `M_Bib_PierreTaillee_Peint` |
| `SM_Cave_Coursive_8m` | Coursive le long d'un mur : 8 × 2.5 m, dessus à 0 (pivot au ras du mur), garde-corps côté vide. | 7.96 × 2.61 × 2.15 | -2.15 | 1080 | `M_Bib_BoisPourri`, `M_Bib_FerRouille` |
| `SM_Cave_Passerelle_10m` | Passerelle entre deux étages : 10 × 2.4 m, dessus à 0, appuis aux bouts, garde-corps des deux côtés. | 10.6 × 2.6 × 0.72 | -0.73 | 2004 | `M_Bib_BoisPourri` |
| `SM_Cave_PorteHaute` | Porte de joueur en hauteur : ouverture de 2.4 × 3.6 m, seuil à 5 m dans un mur de 8 × 10 m, rebord de 3 × 1.2 m devant. | 8.0 × 2.2 × 10.0 | 0.0 | 404 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_BoisPourri` |
| `SM_Cave_PorteEtroite` | Porte étroite au sol : ouverture de 4.4 m qui contient le gabarit de 4 × 5.25 m du traqueur baissé (il se faufile). | 12.0 × 3.32 × 10.01 | -0.01 | 490 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint` |
| `SM_Cave_Echelle_6m` | Échelle de 6 m contre un mur, barreaux tous les 30 cm, montants qui dépassent d'1 m en haut. | 0.75 × 0.28 × 7.0 | 0.0 | 488 | `M_Bib_BoisPourri`, `M_Corde`, `M_Bib_FerRouille` |
| `SM_Cave_MurRebords_10m` | Mur de 6 × 10 m à quatre rebords plats en quinconce (tous les 2.2 m) : montée de rebord en rebord, wall jumps simples. | 6.11 × 2.74 × 10.34 | 0.0 | 462 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_MurEscalade_12m` | Mur escaladable de 8 × 12 m : prises plates en quinconce tous les 2 m (on s'y hisse) ; mur grimpable du traqueur et des goules. | 8.2 × 2.18 × 12.3 | 0.0 | 1038 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_BoisPourri` |
| `SM_Cave_PorteVerrouillee` | Porte de salle payante : ouverture carrée de 3 × 3 m, vantail de fer en guillotine (Vantail), sceau d'âme qui tourne (Serrure). | 6.0 × 1.43 × 6.65 | -0.05 | 1120 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_FerRouille`, `M_Kit_Sceau_Ame` |
| `SM_Cave_Tremie_8m` | Trémie : plancher de 8 × 8 m (dessus à 0) percé d'un trou de 4 × 4 m aux bords plats et nets. | 8.0 × 8.0 × 1.0 | -1.0 | 88 | `M_Bib_BoisPourri`, `M_Bib_Moellons_Peint` |
| `SM_Cave_Cheminee_16m` | Cheminée de roche de 16 m : parois parallèles à 3.5 m pour monter en wall jumps. | 13.48 × 7.18 × 16.63 | -0.03 | 1342 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_Balcon_Bois` | Balcon de planches de 3.2 × 1.8 m en encorbellement, garde-corps sur trois côtés. | 3.16 × 1.85 × 1.85 | -1.85 | 300 | `M_Bib_BoisPourri`, `M_Bib_FerRouille` |
| `SM_Cave_CelluleTraqueur_Herse` | Cellule d'apparition du traqueur : façade de 18 × 21 m, arc de 10 × 11 m, herse qui monte de 9.2 m. | 18.0 × 15.44 × 21.05 | -0.05 | 1351 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales`, `M_Bib_FerRouille` |
| `SM_Cave_CelluleTraqueur_Barricade` | Cellule d'apparition du traqueur dans la roche : caverne, barricade de rondins à deux battants. | 24.51 × 18.15 × 21.07 | -0.04 | 3774 | `M_Bib_RocheCave_Peint` |
| `SM_Cave_CelluleTraqueur_Fosse` | Fosse d'apparition du traqueur : 12 × 12 × 15 m, mur à prises, deux trappes de grilles. | 15.0 × 15.0 × 16.82 | -15.02 | 1532 | `M_Bib_Moellons_Peint`, `M_Bib_Dalles_Sales`, `M_Bib_PierreTaillee_Peint`, `M_Bib_BoisNeuf`, `M_Bib_FerRouille` |

### Acte II : château (25 pièces, `KitChateau`)

| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |
|---|---|---|---|---|---|
| `SM_Chateau_Escalier_Droit_4m` | Escalier droit plein : monte de 4 m (marches de 20 cm ou moins), 4 m de large, palier de 1.5 m ; rampe de collision sous les nez. | 4.0 × 7.5 × 4.0 | 0.0 | 3014 | `M_Chateau_PierreClaire`, `M_Bib_Tapis`, `M_Chateau_Dalles` |
| `SM_Chateau_Escalier_Tournant_4m` | Escalier à quart tournant : deux volées de 2 m, palier carré, palier en haut ; rampes de collision. | 8.5 × 7.0 × 4.0 | 0.0 | 4114 | `M_Chateau_PierreClaire`, `M_Bib_Tapis`, `M_Chateau_Dalles` |
| `SM_Chateau_Escalier_Colimacon` | Escalier en colimaçon (Ø 4 m, 3.6 m aux caves) : monte de 6.4 m en deux tours, 16 marches de 20 cm par tour, palier de 100° ; rampe hélicoïdale. | 4.0 × 4.0 × 7.75 | 0.0 | 3583 | `M_Chateau_PierreClaire`, `M_Chateau_Dalles` |
| `SM_Chateau_Mezzanine_8x6` | Mezzanine de 8 × 6 m, dessus à 5 m (double saut et hissage), contre un mur ; garde-corps devant et à gauche, droite ouverte. | 8.2 × 6.23 × 5.0 | 0.0 | 3060 | `M_Chateau_PierreClaire`, `M_Chateau_Parquet` |
| `SM_Chateau_Coursive_8m` | Coursive le long d'un mur : 8 × 2.5 m, dessus à 0 (pivot au ras du mur), garde-corps côté vide. | 8.01 × 2.68 × 2.2 | -2.2 | 1576 | `M_Chateau_PierreClaire`, `M_Chateau_Parquet` |
| `SM_Chateau_Passerelle_10m` | Passerelle entre deux étages : 10 × 2.4 m, dessus à 0, appuis aux bouts, garde-corps des deux côtés. | 10.0 × 2.69 × 2.21 | -2.2 | 3706 | `M_Chateau_PierreClaire`, `M_Chateau_Dalles` |
| `SM_Chateau_PorteHaute` | Porte de joueur en hauteur : ouverture de 2.4 × 3.6 m, seuil à 5 m dans un mur de 8 × 10 m, rebord de 3 × 1.2 m devant. | 8.0 × 2.2 × 10.0 | 0.0 | 392 | `M_Chateau_PierreClaire` |
| `SM_Chateau_PorteEtroite` | Porte étroite au sol : ouverture de 4.4 m qui contient le gabarit de 4 × 5.25 m du traqueur baissé (il se faufile). | 12.01 × 2.08 × 10.0 | 0.0 | 322 | `M_Chateau_PierreClaire` |
| `SM_Chateau_Echelle_6m` | Échelle de 6 m contre un mur, barreaux tous les 30 cm, montants qui dépassent d'1 m en haut. | 0.6 × 0.28 × 6.85 | 0.27 | 864 | `M_Bib_FerRouille` |
| `SM_Chateau_MurRebords_10m` | Mur de 6 × 10 m à quatre rebords plats en quinconce (tous les 2.2 m) : montée de rebord en rebord, wall jumps simples. | 6.02 × 2.08 × 10.02 | -0.01 | 453 | `M_Chateau_PierreClaire` |
| `SM_Chateau_MurEscalade_12m` | Mur escaladable de 8 × 12 m : prises plates en quinconce tous les 2 m (on s'y hisse) ; mur grimpable du traqueur et des goules. | 8.01 × 1.95 × 12.0 | 0.0 | 1388 | `M_Chateau_PierreClaire` |
| `SM_Chateau_PorteVerrouillee` | Porte de salle payante : ouverture carrée de 3 × 3 m, vantail de fer en guillotine (Vantail), sceau d'âme qui tourne (Serrure). | 6.0 × 1.43 × 6.65 | -0.05 | 1120 | `M_Chateau_PierreClaire`, `M_Bib_Trim`, `M_Kit_Sceau_Ame`, `M_Bib_FerRouille` |
| `SM_Chateau_Tremie_8m` | Trémie : dalle de 8 × 8 m percée d'un trou de 4 × 4 m, bordure de marbre, balustrade sur trois côtés. | 8.0 × 8.0 × 0.6 | -0.6 | 2080 | `M_Bib_Marbre`, `M_Chateau_Dalles`, `M_Chateau_PierreClaire` |
| `SM_Chateau_PorteSortie` | Porte de sortie de l'acte II : arc brisé de 8 × 10 m à voussures, deux battants de boiserie, sceau de lune doré. | 12.41 × 2.9 × 14.01 | -0.01 | 2012 | `M_Chateau_PierreClaire`, `M_Bib_Marbre` |
| `SM_Chateau_CelluleTraqueur` | Cellule d'apparition du traqueur : arc brisé de 10 × 12 m, herse de fer forgé en losanges qui monte de 9.2 m. | 18.01 × 15.58 × 22.55 | -0.05 | 1977 | `M_Chateau_PierreClaire`, `M_Chateau_Dalles`, `M_Bib_FerRouille` |
| `SM_Chateau_Fenetre_Lancette_Simple` | Fenêtre en lancette simple : baie de 1.6 × 6.6 m (appui à 3 m) dans un panneau de 4 × 12 m, vitraux sombres, repère RayonLune. | 4.02 × 1.68 × 12.0 | 0.0 | 864 | `M_Chateau_PierreClaire`, `M_Bib_FerRouille`, `M_Kit_Vitrail`, `M_Kit_Vitrail_Ambre`, `M_Kit_Vitrail_Rouge` |
| `SM_Chateau_Fenetre_Lancette_Double` | Fenêtre à deux lancettes et oculus : baie de 4 × 7.3 m (appui à 2.5 m) dans un panneau de 8 × 12 m, vitraux sombres, repère RayonLune. | 8.01 × 1.68 × 12.0 | 0.0 | 1794 | `M_Chateau_PierreClaire`, `M_Bib_FerRouille`, `M_Kit_Vitrail`, `M_Kit_Vitrail_Ambre`, `M_Kit_Vitrail_Rouge` |
| `SM_Chateau_Fenetre_Lancette_Triple` | Haute fenêtre à trois lancettes et rose : baie de 5.6 × 14.3 m (appui à 3.5 m) dans un panneau de 8 × 20 m, vitraux sombres, repère RayonLune. | 8.01 × 1.68 × 20.0 | 0.0 | 2560 | `M_Chateau_PierreClaire`, `M_Bib_FerRouille`, `M_Kit_Vitrail`, `M_Kit_Vitrail_Ambre`, `M_Kit_Vitrail_Rouge` |
| `SM_Chateau_Chandelier_Petit` | Petit chandelier suspendu : Ø 2 m (pièce de 12 × 12 m), 8 bougies, bas à 2.4 m sous le plafond (plafond de 5.9 m au moins). | 2.15 × 2.15 × 2.4 | -2.4 | 1344 | `M_Bib_FerRouille`, `M_Bib_Trim`, `M_Kit_Cire` |
| `SM_Chateau_Chandelier_Moyen` | Chandelier moyen : Ø 3.3 m (salle de 24 × 16 m), 18 bougies sur deux couronnes, bas à 5 m sous le plafond (12 m). | 3.45 × 3.45 × 5.0 | -5.0 | 2796 | `M_Bib_FerRouille`, `M_Bib_Trim`, `M_Kit_Cire` |
| `SM_Chateau_Chandelier_Grand` | Grand chandelier de hall : Ø 4.6 m (hall de 32 × 24 m), 32 bougies sur trois couronnes, bas à 8 m sous le plafond (20 m). | 4.75 × 4.75 × 8.0 | -8.0 | 4636 | `M_Bib_FerRouille`, `M_Bib_Trim`, `M_Kit_Cire` |
| `SM_Chateau_Lustre_Cristal` | Lustre à pampilles de Ø 2.4 m : trois étages de bras dorés, 24 bougies, gouttes de cristal sombre. | 2.55 × 2.55 × 3.0 | -3.0 | 3904 | `M_Bib_Trim`, `M_Kit_Cire`, `M_Kit_Cristal` |
| `SM_Chateau_Applique_2Bougies` | Applique murale dorée à deux bougies (platine en écusson, bras en S). | 0.75 × 0.44 × 0.66 | -0.3 | 220 | `M_Bib_Trim`, `M_Kit_Cire` |
| `SM_Chateau_Applique_Miroir` | Applique à miroir ovale sombre et trois bougies. | 0.79 × 0.5 × 1.26 | -0.34 | 660 | `M_Kit_Vitrail`, `M_Bib_Trim`, `M_Kit_Cire` |
| `SM_Chateau_Candelabre_Pied` | Candélabre sur pied de 2.1 m, pied de marbre, sept bougies. | 0.84 × 0.84 × 2.27 | 0.0 | 744 | `M_Chateau_PierreClaire`, `M_Bib_Trim`, `M_Kit_Cire` |
| `SM_Chateau_Torchere` | Torchère sur pied de 1.85 m : trépied à volutes, fût à nœuds dorés, cage de fer et fagot de tissu goudronné ; repère Feu_Torche. | 0.67 × 0.59 × 1.88 | 0.0 | 832 | `M_Bib_FerRouille`, `M_Bib_Trim`, `M_Chiffon_Brule` |
| `SM_Chateau_Torche_Murale` | Torche murale : platine et console de fer forgé, manche de bois dans deux bagues, tête de tissu goudronné ; repère Feu_Torche. | 0.16 × 0.41 × 0.76 | -0.25 | 393 | `M_Bib_FerRouille`, `M_Bib_Trim`, `M_Bib_BoisNeuf`, `M_Chiffon_Brule` |

### Acte III : jardin (25 pièces, `KitJardin`)

| Pièce | Rôle | Emprise x × y × z (m) | Bas (m) | Triangles | Matériaux |
|---|---|---|---|---|---|
| `SM_Jardin_Escalier_Droit_3m` | Escalier droit plein : monte de 3 m (marches de 20 cm ou moins), 3.2 m de large, palier de 1.5 m ; rampe de collision sous les nez. | 4.45 × 8.31 × 4.03 | 0.0 | 442 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Jardin_Escalier_Tournant_4m` | Escalier à quart tournant : deux volées de 2 m, palier carré, palier en haut ; rampes de collision. | 8.5 × 7.0 × 4.0 | 0.0 | 2794 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Jardin_Escalier_Colimacon` | Escalier en colimaçon (Ø 4 m, 3.6 m aux caves) : monte de 6.4 m en deux tours, 16 marches de 20 cm par tour, palier de 100° ; rampe hélicoïdale. | 4.0 × 4.0 × 7.7 | 0.0 | 2007 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Moellons_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Jardin_Mezzanine_8x6` | Mezzanine de 8 × 6 m, dessus à 5 m (double saut et hissage), contre un mur ; garde-corps devant et à gauche, droite ouverte. | 8.05 × 6.05 × 5.0 | 0.0 | 1884 | `M_Bib_BoisPourri`, `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Feuillage` |
| `SM_Jardin_Coursive_8m` | Coursive le long d'un mur : 8 × 2.5 m, dessus à 0 (pivot au ras du mur), garde-corps côté vide. | 8.0 × 2.5 × 1.65 | -1.65 | 1260 | `M_Bib_BoisPourri`, `M_Bib_FerRouille` |
| `SM_Jardin_Passerelle_10m` | Passerelle entre deux étages : 10 × 2.4 m, dessus à 0, appuis aux bouts, garde-corps des deux côtés. | 10.02 × 2.4 × 0.59 | -0.59 | 2748 | `M_Bib_BoisPourri`, `M_Bib_FerRouille` |
| `SM_Jardin_PorteHaute` | Porte de joueur en hauteur : ouverture de 2.4 × 3.6 m, seuil à 5 m dans un mur de 8 × 10 m, rebord de 3 × 1.2 m devant. | 8.0 × 2.2 × 10.0 | 0.0 | 440 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Feuillage` |
| `SM_Jardin_PorteEtroite` | Porte étroite au sol : ouverture de 4.4 m qui contient le gabarit de 4 × 5.25 m du traqueur baissé (il se faufile). | 8.2 × 1.8 × 9.3 | 0.0 | 343 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Feuillage` |
| `SM_Jardin_Echelle_6m` | Échelle de 6 m contre un mur, barreaux tous les 30 cm, montants qui dépassent d'1 m en haut. | 0.87 × 0.26 × 7.0 | -0.0 | 448 | `M_Bib_BoisPourri`, `M_Bib_FerRouille` |
| `SM_Jardin_MurRebords_10m` | Mur de 6 × 10 m à quatre rebords plats en quinconce (tous les 2.2 m) : montée de rebord en rebord, wall jumps simples. | 6.1 × 2.17 × 10.24 | 0.0 | 213 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_FerRouille`, `M_Kit_Feuillage` |
| `SM_Jardin_MurEscalade_12m` | Mur escaladable de 8 × 12 m : prises plates en quinconce tous les 2 m (on s'y hisse) ; mur grimpable du traqueur et des goules. | 8.14 × 3.14 × 12.27 | -0.02 | 690 | `M_Bib_RocheCave_Peint`, `M_Bib_HerbeSeche`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Ecorce` |
| `SM_Jardin_PorteVerrouillee` | Porte de salle payante : ouverture carrée de 3 × 3 m, vantail de fer en guillotine (Vantail), sceau d'âme qui tourne (Serrure). | 6.0 × 1.43 × 6.65 | -0.05 | 1147 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_FerRouille`, `M_Kit_Sceau_Ame`, `M_Kit_Feuillage` |
| `SM_Jardin_Puits` | Puits de jardin : margelle lisse Ø 3.2 m, arceau de fer forgé et poulie, eau à -1.5 m, fond à -2.4 m. | 4.4 × 3.2 × 6.56 | -2.42 | 1296 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Terre_Racines`, `M_Bib_FerRouille` |
| `SM_Jardin_PorteSortie` | Porte de sortie de l'acte III (vers l'arène) : arche de 8 × 10 m entre deux contreforts, deux battants, sceau de lune. | 15.4 × 3.2 × 14.31 | -0.01 | 1781 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales`, `M_Bib_Moellons_Peint`, `M_Kit_Feuillage` |
| `SM_Jardin_CelluleTraqueur_Voliere` | Cellule d'apparition du traqueur : grande volière de fer de Ø 16 m (20 m), portail à deux vantaux. | 17.0 × 16.56 × 21.73 | -0.03 | 5056 | `M_Bib_PierreTaillee_Peint`, `M_Bib_FerRouille` |
| `SM_Jardin_Falaise_Rebords_16m` | Falaise de 16 × 16 m à quatre rebords plats tous les 3.5 m (saut et hissage). | 16.36 × 5.65 × 16.3 | 0.0 | 720 | `M_Bib_RocheCave_Peint`, `M_Bib_HerbeSeche` |
| `SM_Jardin_Falaise_Passage` | Passage dans la roche : gorge de 10 × 16 m sous une arche naturelle, le traqueur passe. | 30.43 × 18.98 × 18.66 | -0.03 | 3606 | `M_Bib_RocheCave_Peint`, `M_Bib_HerbeSeche` |
| `SM_Jardin_Falaise_Fente` | Fente dans la roche de 2.6 m de large et 12 m de long : joueurs seulement. | 18.5 × 14.99 × 14.8 | -0.04 | 2280 | `M_Bib_RocheCave_Peint`, `M_Bib_HerbeSeche` |
| `SM_Jardin_Terrasse_8m` | Terrasse : mur de soutènement de 8 × 3 m à contreforts, dessus dallé de 8 × 6 m, balustrade. | 8.0 × 6.9 × 3.0 | 0.0 | 1487 | `M_Bib_Moellons_Peint`, `M_Bib_Dalles_Sales`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Feuillage` |
| `SM_Jardin_Terrasse_Acces_8m` | Terrasse avec accès : ouverture de 3.6 m entre deux piliers à vases (arrivée d'escalier). | 8.0 × 6.9 × 5.0 | 0.0 | 687 | `M_Bib_Moellons_Peint`, `M_Bib_Dalles_Sales`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Feuillage` |
| `SM_Jardin_Pont_Pierre_12m` | Pont de pierre en dos d'âne de 12 m (tablier de 3.6 m, +1.2 m au milieu), arche de 8 m, parapets bas. | 12.0 × 3.78 × 3.9 | -2.5 | 644 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
| `SM_Jardin_Pont_Bois_10m` | Pont de rondins et de planches de 10 × 2.4 m, main courante de branches. | 11.0 × 3.2 × 1.44 | -1.43 | 860 | `M_Bib_Ecorce`, `M_Bib_BoisPourri`, `M_Bib_PierreTaillee_Peint` |
| `SM_Jardin_MurEnceinte_Tour` | Tour ronde d'angle du mur d'enceinte : Ø 6 m, 14 m, crénelée, meurtrières. | 7.2 × 7.2 × 14.4 | 0.0 | 615 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales`, `M_Fond_Noir`, `M_Kit_Feuillage` |
| `SM_Jardin_MurEnceinte_Breche_8m` | Mur d'enceinte de 8 m effondré au milieu : brèche à 1.4 m, éboulis en rampe. | 8.0 × 5.72 × 9.35 | -0.04 | 1269 | `M_Bib_Moellons_Peint`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Feuillage` |
| `SM_Jardin_MurEnceinte_CheminRonde_8m` | Mur d'enceinte de 8 × 9 m à chemin de ronde dallé et parapet crénelé. | 8.0 × 3.45 × 10.9 | 0.0 | 151 | `M_Bib_Moellons_Peint`, `M_Bib_Dalles_Sales`, `M_Bib_PierreTaillee_Peint`, `M_Kit_Feuillage` |

Pièces existantes modifiées par ce lot : `SM_Cave_Puits` (refait : margelle lisse, eau, fond), `SM_Cave_PorteSortie` et `SM_Chateau_GrandeEntree` (sceau de lune de sortie sur les battants) ; leurs lignes sont à jour dans les sections des actes.

## Nuit 5, lot 3 : pièces pour de vraies cartes (branche art-kits-5)

34 nouvelles pièces (verticalité, fenêtres qui montrent l'extérieur, lisières, sous-bois, rochers, grand arbre, sols percés, support de râtelier), porte verrouillée finie, sol mélangé du jardin (`M_Jardin_SolMelange`), lueur des coffres. Dimensions, pivots, usage et consignes de pose : docs/art/INTEGRATION-PROPS.md, section « Nuit 5, lot 3 » ; source `kit_nuit5.py` ; catalogue `kit_doc_nuit5.py` ; planches `docs/art/rendus/Nuit5/Lot3/`.
