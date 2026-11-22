# Kits de décor des actes I à III (nuit du 2026-11-22)

Modélisés par script dans Blender (aucune génération IA, aucun crédit). Architecture en **UV monde** : les matériaux
de la bibliothèque (`M_Bib_*`, docs/art/MATERIAUX.md) s'appliquent sans dépliage ; **peinture de sommets** sur chaque pièce
(R saleté en bas, G mousse ou salpêtre en plaques, B teinte de la pièce) lue par les matériaux `*_Peint`. Conventions : Unity
+Y en haut, avant de la pièce vers −Z (face visible vers −Z pour les murs et les objets muraux), pivot au sol au centre de
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
