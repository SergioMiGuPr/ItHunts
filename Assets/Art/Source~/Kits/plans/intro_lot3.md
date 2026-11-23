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
- parties mobiles : `Battant_G` et `Battant_D`, `Vantail`, `Serrure`, `Herse`, `Trappe_G` et `Trappe_D`, avec leur pivot à l'axe.

**Repères exportés** :
- `Accroche_*` : points de gameplay ;
- `Feu_*` : flammes ;
- `RayonLune` : à la fenêtre ;
- `Apparition_Traqueur`, `Porte_Traqueur`, `Passage_*`, `Appui_*`.

**Où trouver le reste** :
- dimensions, pivots, axes et angles d'ouverture : docs/art/INTEGRATION-PROPS.md, section « Nuit 4, lot 3 » ;
- planche : `docs/art/rendus/Nuit4/Lot3/Planche_Kits_Lot3.jpg`, une image par pièce dans le même dossier ;
- sources : `kit_hauteur.py`, module commun importé par les trois scripts de kit ; rendus : `rendu_kits.py` et `planche_kits.py` ; catalogue : `kit_doc_lot3.py`.
