# Fin de la nuit du 2026-11-22 (piste art)

Ce fichier signale la fin de la nuit à `main`. Il contient :
- la liste des PR et leur état ;
- le contrôle du brief, point par point, avec une preuve pour chaque point ;
- la comparaison avec les meilleurs jeux du genre ;
- les crédits dépensés et les textures à fournir.

## PR de la nuit

Les branches sont empilées : chaque lot part du précédent, parce qu'il utilise ses matériaux ou ses kits. Elles se fusionnent donc dans l'ordre.

| PR | Branche | Contenu | État à l'écriture |
|---|---|---|---|
| [#36](https://github.com/SergioMiGuPr/ItHunts/pull/36) | `art-materiaux` | Lot 1 : 22 tuiles peintes, trim sheet, 16 décalcomanies, mode décor du shader toon, recherche | fusionnée |
| [#37](https://github.com/SergioMiGuPr/ItHunts/pull/37) | `art-kit-caves` | Lot 2 : kit de l'acte I (43 pièces) | fusionnée |
| [#38](https://github.com/SergioMiGuPr/ItHunts/pull/38) | `art-kit-chateau` | Lot 3 : kit de l'acte II (31 pièces, rayons de lune) | fusionnée |
| [#39](https://github.com/SergioMiGuPr/ItHunts/pull/39) | `art-kit-jardin` | Lot 4 : kit de l'acte III (25 pièces, ciel, brume, arène) | fusionnée |
| [#40](https://github.com/SergioMiGuPr/ItHunts/pull/40) | `art-salles-exemple` | Lot 5 : 11 salles d'exemple et leurs fiches | ouverte |
| [#41](https://github.com/SergioMiGuPr/ItHunts/pull/41) | `art-sons-2` | Lot 6 : 51 sons, musiques et ambiances | ouverte |
| [#42](https://github.com/SergioMiGuPr/ItHunts/pull/42) | `art-ui-3` | Lot 7 : 6 éléments d'interface | ouverte |
| [#43](https://github.com/SergioMiGuPr/ItHunts/pull/43) | `art-vfx-3` | Lot 8 : 12 effets | ouverte |
| cette PR | `art-fin-3` | ce fichier | ouverte |

## Crédits

| Poste | Plafond | Dépensé | Preuve |
|---|---|---|---|
| Sons et musiques | 60 | **57.59** | solde Higgsfield de 1017.31 à 959.72 ; devis détaillé dans la PR #41 |
| Images et textures | 40 | **0** | toutes les textures sont procédurales |
| **Total** | 100 | **57.59** | aucun achat de plugin |

## Textures à fournir

Voir `docs/art/TEXTURES-A-FOURNIR.md`. Chacune a déjà sa meilleure version procédurale en place.
- écorce (priorité haute : arbres de wall jump vus de près) ;
- herbe sèche et gravier ;
- tapis et tentures ;
- marbre.

## Contrôle du brief, point par point

✅ : fait et vérifié (la preuve est citée). ⚠️ : fait, avec une réserve écrite.

### Avant de commencer

| Point | | Preuve |
|---|---|---|
| Recherche en ligne : level design pro, kits modulaires, éclairage d'horreur, décors lisibles | ✅ | `docs/art/RECHERCHE-DECORS.md` : 7 thèmes, sources, tableau « appliqué dans » (PR #36) |
| Appliquer cette recherche | ✅ | mélange en hauteur et peinture de sommets (lot 1), trim sheet, kits en coque organique, repère par acte (puits, verrière et rayons de lune, lune au-dessus de l'arène), « que s'est-il passé ici ? » dans les fiches (lot 5) |

### Lot 1 : matériaux et textures (PR #36)

| Point | | Preuve |
|---|---|---|
| Pierres : moellons, pierre taillée, roche de cave humide, dalles | ✅ | `T_Mat_Moellons`, `_PierreTaillee`, `_RocheCave`, `_Dalles` ; `docs/art/rendus/Materiaux/Planche_Materiaux_Jeu.jpg` |
| Mortier, mousse, salpêtre, traces d'humidité | ✅ | `T_Mat_Mortier`, `_Mousse`, `_Salpetre`, `_Humidite` |
| Bois neuf, pourri, mouillé ; fer rouillé ; plâtre fissuré ; marbre ; tapis et tentures | ✅ | `T_Mat_BoisNeuf`, `_BoisPourri`, `_BoisMouille`, `_FerRouille`, `_PlatreFissure`, `_Marbre`, `_Tapis`, `_Tenture` |
| Terre, herbe sèche, écorce, racines, gravier | ✅ | `T_Mat_Terre`, `_HerbeSeche`, `_Ecorce`, `_Racines`, `_Gravier` |
| « Faits dans Blender, procédural puis cuit en texture » | ⚠️ | **Réserve :** procédural, puis cuit en PNG par des scripts Python (`peint.py`, `materiaux.py`) qui tournent dans le Python de Blender comme dans un Python seul. Ils ont été lancés en Python seul (plus rapide) ; ce ne sont pas des nœuds Blender. Le résultat est le même : tuiles qui se raccordent, sans IA. |
| Trim sheets | ✅ | `T_Mat_Trim_BaseColor`, 9 bandes ; `Planche_Voisins_Decals_Trim.jpg` |
| Décalcomanies : fissures, taches, mousse, coulures, éclats | ✅ | `T_Mat_Decals`, 16 prefabs `Decal_*` |
| Peinture de sommets et mélange en hauteur ; bas des murs plus sale et plus humide | ✅ | mode `_DECOR` d'`ItHunts/Toon` ; `Planche_Variantes_Hauteur.jpg` |
| Aucun mur ne ressemble à son voisin | ✅ | `captures/_Voisins.jpg` : 5 murs du même matériau, tous différents |
| UV monde comme le kit gothique | ✅ | `_WORLD_UV`, taille de tuile par matériau (`MATERIAUX.md`) |

### Lot 2 : acte I, caves (PR #37)

| Point | | Preuve |
|---|---|---|
| Salles organiques, pas carrées : murs de roche irréguliers et courbes | ✅ | `Cave_Paroi_Droite`, `_Courbe`, `_Saillie`, `PilierRoche` ; salles `Cave_Citerne` et `Cave_CavernePuits` en ellipse (`docs/art/rendus/Exemples/Caves/`) |
| Murs maçonnés brisés, plusieurs hauteurs de murs | ✅ | `Cave_MurBrise_4m`, `_8m` ; parois de 6, 10 et 18 m |
| Éboulis, arches basses et hautes | ✅ | `Cave_Eboulis_Petit`, `_Grand`, `ArcheBasse` (3 × 3.5 m), `ArcheHaute` (8 × 11 m), `ArcheRuine` |
| Ponts de pierre et de bois au-dessus de l'eau ou du vide ; canaux, bassins, gouffres | ✅ | `Cave_PontPierre_16m`, `PontBois_12m`, `Canal_8m`, `Bassin`, `Gouffre_Bord_12m` ; salle `Cave_Citerne` |
| Rebords et petites plateformes pour des wall jumps simples | ✅ | `Cave_Rebord_4m`, `Plateforme_3m`, `Corniche_Bois_6m` ; routes dans `INTEGRATION-PROPS.md` |
| Cellules avec barreaux, portes, lits, paille, seaux, chaînes **au bon endroit** | ✅ | `Cave_Cachots` : lit contre le mur du fond, paille au coin, seau près de la grille, chaînes au mur du fond, dans la cellule ; vue `Cave_Cachots_Cellule.jpg` |
| Salle de garde, réserve, puits | ✅ | salle `Cave_SalleDeGarde` (garde et réserve), `Cave_Puits` dans `Cave_CavernePuits` |
| Porte de sortie d'acte classique, sans lumière colorée | ✅ | `Cave_PorteSortie` : bois et fer, éclairée seulement par les candélabres du décor |
| Plus de torches et d'appliques, chandelles, sans éclaircir l'ambiance | ✅ | `Cave_Torche_Murale`, `Applique`, `Chandelles`, `Candelabre`, `Brasero` ; lumières ponctuelles de l'ambiance intermédiaire (1.35 / 15.4 m), entourées de noir |

### Lot 3 : acte II, château (PR #38)

| Point | | Preuve |
|---|---|---|
| Plafonds plus hauts, grands halls, grandes entrées, voûtes | ✅ | murs de 20 m, `Pilier_20m`, `Voute_8x8`, `Arcade_8x12` ; salle `Chateau_GrandHall` |
| Galeries, balcons, grands escaliers | ✅ | `Galerie_8m`, `Balcon`, `GrandEscalier` |
| Hautes fenêtres, rayons de lune, poussière blanc-gris (pas bleue) | ✅ | `Fenetre_Haute`, `Chateau_RayonLune` (poussière `#D9D9D1`) ; `Planche_GrandHall_RayonsDeLune.jpg` |
| Chandeliers, tentures, bibliothèques, tableaux sombres, meubles renversés | ✅ | `Chandelier`, `Tenture`, `Bibliotheque`, `Tableau`, `Chaise_Renversee`, `Table_Renversee`, `Armoire_Tombee` |
| Salle de bal, chapelle, chambres | ✅ | salles `Chateau_SalleDeBal` et `Chateau_Chapelle` ; pièces de chambre (`LitBaldaquin`, `Coffre`, `MiroirBrise`, `Fauteuil`) |
| Un peu plus éclairé que les caves, toujours inquiétant | ✅ | ambiance (0.07, 0.075, 0.1), lune à 0.35, contre (0.055, 0.058, 0.08) et pas de lune dans les caves |
| Grande porte d'entrée du château, vers l'acte III | ✅ | `Chateau_GrandeEntree` ; posée au sud du jardin (`Jardin_AlleeDesStatues_Porte.jpg`) |

### Lot 4 : acte III, jardin et arène (PR #39)

| Point | | Preuve |
|---|---|---|
| Jardin à l'abandon sous la lune, plus ouvert et un peu plus lumineux, ciel avec la lune | ✅ | `Jardin_Ciel` (shader `ItHunts/Ciel`), lune à 0.6 ; `Planche_Jardin_Arene_Ciel.jpg` |
| Gros arbres morts, troncs assez larges pour les wall jumps | ✅ | `ArbreMort_Grand` (Ø 2.6 m), `_Penche`, `_Tordu` |
| Haies, massifs ; palissades et clôtures le long des allées et des parcelles | ✅ | `Haie_6m`, `Massif`, `Palissade_4m`, `Grille_4m`, `Portail`, posés le long des allées dans `Jardin_AlleeDesStatues` |
| Murets, statues, fontaines, serre en ruine, kiosque, tombes, grilles, chemins de gravier, brume au sol | ✅ | `Muret_4m`, `Statue_Pleureuse`, `Fontaine`, `Serre_Ruine`, `Kiosque`, `Tombe_*`, `Caveau`, `Chemin_8m`, `Jardin_Brume` |
| Limites naturelles et évidentes, jamais de murs invisibles | ✅ | `Foret_Lisiere_20m`, `MurEnceinte_8m`, `Falaise_16m` |
| Arène du boss en contrebas : terrain dégagé, accroches de traction, couverts, relief | ✅ | salle `Arene_Boss` ; `PilierBrise` et `Statue_Geante` avec leurs `Accroche_Traction`, `Couvert`, `Butte`, `EscalierDescente` |

### Lot 5 : salles d'exemple (PR #40)

| Point | | Preuve |
|---|---|---|
| 3 à 4 salles par acte, habillées et éclairées, formes non géométriques | ✅ | 4 caves, 4 château, 3 jardin ; `docs/art/rendus/Exemples/Planche_Exemples_*.jpg` |
| Prefabs dans `Assets/Art/Levels/Exemples/<Acte>/` | ✅ | 11 prefabs |
| Props placés avec logique, aucun pilier inutile | ✅ | cachots (vue de cellule), réserve derrière le mur effondré ; chaque pilier est une accroche ou porte une voûte |
| Entrées, sorties et routes de parcours dans `INTEGRATION-PROPS.md` | ✅ | une fiche par salle : identité, fonction, entrées et sorties, routes, prises, tension |

### Lot 6 : sons et musiques (PR #41)

| Point | | Preuve |
|---|---|---|
| Remplacer **tous** les sons provisoires de `ProceduralSfx` | ✅ | colonne « Remplace » de la table d'ART-HANDOFF : les 24 fonctions sont couvertes avec le lot B |
| Générer par Higgsfield, devis avant, dans le plafond | ✅ | devis dans la PR #41 ; 57.59 crédits sur 60 |
| Traiter : silences coupés, normalisation, boucles | ✅ | `sons_nuit3.py`, `sons_nuit3.json` (durées, crêtes, raccords) |
| Pas de « piou piou » à la mort des goules | ✅ | `Sfx_Goule_Mort_Organique` (0.8 s, organique) remplace l'ancien son |
| Liste minimale complète | ✅ | portes, menu, achat, âme, télékinésie, impacts, mouvements, parade, étourdissement, critique, paralysie, explosion, chute, plongeon, cœur, arrivée du traqueur, boss, interface |
| Musiques en boucle (château, jardin, boss) et ambiances en boucle (cave, château, jardin) | ✅ | `Musique/*_Boucle`, `Ambiance/Amb_*_Boucle` |
| Liste « nom du son → événement » dans ART-HANDOFF | ✅ | section « Sons de la nuit du 2026-11-22 » |
| Qualité à l'écoute | ⚠️ | **Réserve :** je ne peux pas écouter. Le contrôle s'est fait sur la mesure (forme d'onde, spectrogramme, niveaux, raccords) : un son vide a été refait, un double battement corrigé. À valider à l'oreille. |

### Lot 7 : interface (PR #42)

| Point | | Preuve |
|---|---|---|
| Écran de fin de run (statistiques, mort ou victoire) | ✅ | `Panneau_FinDeRun` ; `FinDeRun_Mort.jpg`, `FinDeRun_Victoire.jpg` |
| Menu d'interaction de porte (prix en âmes) | ✅ | `Panneau_Porte` ; `Porte_Payable.jpg`, `Porte_Trop.jpg` |
| Menu de choix de l'acte avec flèches (playtest) | ✅ | `Panneau_ChoixActe` ; `ChoixActe_I.jpg`, `_II.jpg` |
| Panneau admin de playtest | ✅ | `Panneau_Admin` ; `Admin.jpg` |
| Barre de vie du boss | ✅ | `HUD_BarreVieBoss` ; `BarreVieBoss.jpg` |
| Marqueur de critique (headshot) | ✅ | `HUD_Critique` ; `Critique.jpg` |
| Dans la famille des cadres des maquettes ; UI-HANDOFF à jour | ✅ | seulement des pièces existantes ; `UI-HANDOFF.md` § 8 |

### Lot 8 : effets (PR #43)

| Point | | Preuve |
|---|---|---|
| Headshot critique ; balle paralysante (cible figée, éclairs) ; explosion de zone sur kill | ✅ | `VFX_Critique`, `VFX_Balle_Paralysante`, `VFX_Paralysie`, `VFX_Explosion_Kill` ; `Planche_VFX_Lot3.jpg` |
| Éclair de parade plus lisible | ✅ | `VFX_Parade_Eclair_Lisible` |
| Poussière blanc-gris dans les rayons de lune ; brume au sol du jardin | ✅ | `VFX_Poussiere_Lune`, `VFX_Brume_Sol` ; vus en situation dans `Planche_GrandHall_RayonsDeLune.jpg` et `Planche_Jardin_Arene_Ciel.jpg` |
| Attaques et blessures du boss ; point faible qui brille | ✅ | `VFX_Boss_Griffe`, `_Morsure`, `_Impact_Sol`, `_Blessure`, `_PointFaible` |

### Règles et livraison

| Point | | Preuve |
|---|---|---|
| Un lot par branche, commits avec les `.meta`, push, PR vers `main` ; jamais de fusion ni de push forcé | ✅ | PR #36 à #43 ; aucune fusion faite par la piste art |
| Une ligne au manifeste par asset (source, droits, IA oui ou non) | ✅ | `docs/assets/MANIFEST.md` : textures, matériaux, pièces des trois kits, salles, sons (IA : oui), interface, effets |
| Mains FPS non modifiées ; modèle du traqueur verrouillé | ✅ | aucun fichier de `FPS/`, `BrasFPS` ni `Traqueur_v4` touché |
| Aucun nom d'œuvre dans les prompts et les guides de style | ✅ | prompts audio descriptifs (PR #41) ; les noms de jeux n'apparaissent que comme références de recherche |
| Gratuit d'abord ; aucun achat | ✅ | tout le visuel est procédural (0 crédit d'image) |
| Unity et Blender lancés par moi, puis fermés | ✅ | Blender en ligne de commande ; Unity fermé à la fin (voir le résumé) |

## Comparaison avec les meilleurs jeux du genre

Ces comparaisons s'appuient sur la recherche (`RECHERCHE-DECORS.md`). La troisième colonne est mon jugement honnête de ce que l'art livré cette nuit apporte, et de ce qu'il reste à faire en jeu pour être au niveau.

| Pilier | Référence | Ce qu'elle fait de mieux | Où en est ItHunts après cette nuit |
|---|---|---|---|
| Décors sans répétition | Kits modulaires et trims d'Insomniac et de Naughty Dog, grottes de C. Crumpler | Trims partagées, peinture de sommets, mélange en hauteur | **Au niveau sur le principe** : mélange en hauteur à bords francs, peinture de sommets, variation par pièce, trims, décalcomanies. Reste : peindre les sommets à la main pièce par pièce lors de l'habillage des vraies salles. |
| Lumière d'horreur lisible | Alien: Isolation, Hollow Knight, Left 4 Dead | Noir réglé finement, couche jouable plus contrastée, lumières motivées | **Proche** : sources motivées (torches, bougies, lune), contre-jour froid, ombres bleu nuit, rayons de lune comme chemin critique. Reste : régler l'exposition en jeu, et donner un liseré plus fort aux prises jouables. |
| Identité de chaque salle | Dead Cells (identité dans la forme), Bloodborne (raccourcis) | Forme jouable propre au biome, boucles | **Bon départ** : 11 salles aux formes et fonctions différentes, routes écrites. Reste : les jouer et les itérer en playtest avec le mouvement réel (élan de 18 à 26 m/s). |
| Sensations | Doom Eternal, Ultrakill | Hit stop, son et caméra synchronisés, flash de parade | **Matière prête** : son, effet et marqueur d'interface pour le critique, la parade, l'explosion et la paralysie. Reste à `main` : hit stop, secousse de caméra et synchronisation. |
| Peur du poursuivant | RE2 (Mr X), Dead by Daylight, Alien: Isolation | Pas lourds, rayon de terreur à paliers, son ambigu | **Matière prête** : battement de cœur en boucle (paliers d'intensité proposés), arrivée par la porte, salles qui cachent (labyrinthe, cellules ouvertes). Reste à `main` : directeur de menace et mixage 3D. |
| Envie de relancer | Hades, Dead Cells, Returnal | Récompense visible, mort qui fait avancer, relance immédiate | **Matière prête** : écran de fin avec statistiques, choix de l'acte, porte qui affiche son prix. La boucle elle-même relève du game design. |

**Limites assumées :**
- Le visuel des kits est procédural et sans IA, solide en silhouette et en ambiance. Pour des plans de page de magasin « exceptionnels », les pièces héros (porte du château, statues, arbres) gagneraient à une passe de sculpture à la main.
- Les textures listées dans `TEXTURES-A-FOURNIR.md` gagneraient à être peintes à la main.
- Les sons n'ont pas été écoutés.
