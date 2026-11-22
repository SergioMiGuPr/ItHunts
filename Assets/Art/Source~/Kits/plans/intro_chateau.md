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
