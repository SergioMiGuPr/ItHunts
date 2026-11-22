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
| `SM_Cave_Puits` | Puits : margelle ronde Ø 3 m (hauteur 0.9 m), conduit noir, deux montants de bois, traverse, treuil, corde et seau. | 3.1 × 3.1 × 9.88 | -6.0 | 404 | `M_Bib_Moellons_Peint`, `M_Fond_Noir`, `M_Bib_BoisNeuf`, `M_Corde`, `M_Bib_BoisPourri`, `M_Bib_FerRouille` |
| `SM_Cave_PorteSortie` | Porte de sortie de l'acte I : encadrement de pierre de taille 12 × 13 m, ouverture 8 × 10 m en plein cintre, | 12.4 × 3.2 × 13.0 | 0.0 | 1372 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
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
| `SM_Chateau_GrandeEntree` | Grande porte d'entrée du château (mène à l'acte III) : façade de 20 × 22 m, portail en arc brisé de 9 × 13 m à | 20.2 × 6.47 × 22.0 | 0.0 | 1448 | `M_Bib_PierreTaillee_Peint`, `M_Bib_Dalles_Sales` |
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