> **Seconde revue faite (2026-11-26)** sur `main` au commit `52d1cea` : 7 points de la première revue corrigés, 1 presque, 4 en partie,
> 1 non vérifiable ; 8 défauts nouveaux ou restants ci-dessous, du plus grave au moins grave. **À corriger par main** (sauf les points
> marqués DA), puis remettre l'en-tête « À revoir après correction ».

# Seconde revue des cartes (2026-11-26)

Même méthode que la première revue (6 cartes, graines 22 et 11, Play Mode, éclairage du jeu), avec deux ajouts :
- une vue « haut » par salle (vue loin relevée de 40°) pour juger les plafonds ;
- une sonde : un rayon tiré à un point précis d'une vue donne l'objet, son chemin, son matériau et son collider.

Preuves dans `docs/art/rendus/Revue2/` :
- `Planche_Defauts.jpg` : les défauts ci-dessous, par rangée (numéros 1 à 6) ;
- `Salles_C<acte>_s<graine>.jpg` (28 vues) et `Plafonds_C<acte>_s<graine>.jpg` (14 vues vers le haut) ;
- `audit_*.txt` (audit automatique) et `sonde_*.txt` (identification des objets).

**Lecture de l'audit.** Mes compteurs restent élevés (acte I : 65 à 70 « flottants », acte II : 101 à 119 chevauchements), mais
presque tout est voulu : bougies sur une margelle, candélabres sur une table ou un cercueil, étagères murales sans collision,
lustres **tombés** au sol (`Ens_Lustre_Tombe`, pris pour des suspendus), nénuphars sur l'eau, bancs sous les tables, poutres
croisées des salles d'exemple. Les gros plans le confirment. Seuls les cas cités ci-dessous sont de vrais défauts. Le contrôle de
main (`DecorAudit`, 0 défaut) n'est pas en cause.

## Nouveaux défauts ou défauts restants (du plus grave au moins grave)

1. **Plafonds noirs, actes I et II** (signalé par main ; rangée 1). *Pas corrigé.*
   - Acte II : le plafond de la Grande salle est à **30 m**, en `M_Chateau_PlatrePeint` plat ; les autres salles sont dans le même
     cas. Acte I : la Salle du puits a un plafond à **16 m**, en `M_Bib_RocheCave`.
   - À cette hauteur, aucune lumière ne l'atteint. Le tiers haut de chaque vue « loin » est un aplat noir-olive taché par les
     halos des lustres. Les piliers de 20 m (`Chateau_Pilier_20m`) s'y perdent.
   - Options pour main (à choisir) :
     - **a.** baisser les plafonds des grandes salles à 10 ou 12 m (8 m pour les caves) ; c'est la solution la plus nette ;
     - **b.** garder la hauteur, mais ajouter une structure qui accroche la lumière : voûtes, arcs, poutres, avec quelques lumières
       tournées vers le haut (braseros, appliques hautes) ;
     - **c.** un dégradé de brouillard qui fond le plafond au lieu d'un aplat (cache sans corriger).
2. **« Poteaux noirs » = plateformes de traction mal suspendues** (signalé par main, à identifier ; rangée 2). *Nouveau.*
   - Ce sont `Chateau_PlateformeTraction` (armurerie de l'acte II, échelle 1,4) et `Cave_PlateformeTraction` (Grand puits de
     l'acte I). La sonde les trouve à 3,2 à 3,9 m du sol.
   - L'asset est fait pour pendre sous un plafond : 4 chaînes montent à +4 m jusqu'à `Accroche_Plafond`
     (`INTEGRATION-PROPS.md`). Avec un plafond à 16 ou 30 m, les chaînes s'arrêtent dans le vide et se lisent comme de grands
     poteaux noirs plantés au sol. La plaque en fer rouillé est elle aussi noire, alors qu'elle est interactive (pilier 4 : tout
     ce qui sert doit briller).
   - À faire (main) : accrocher chaque plateforme sous une poutre ou une voûte placée à +4 m au-dessus, ou bien poser la
     plateforme sur un pilier ou une console. Vérifier que les runes de `Plaque_Magnetique` sont allumées en jeu.
3. **Jardin : horizon fermé et feuillage en éclats noirs** (signalé par main ; rangée 3). *En partie corrigé.*
   - Corrigé : le ciel étoilé est ouvert partout, et le sol est en `M_Jardin_SolMelange`.
   - Pas corrigé : chaque salle reste fermée par un `Mur` d'environ 14 m surmonté d'une `Falaise` en `M_Bib_RocheCave` qui monte
     jusqu'à 35 m. Ces murs sont à 50 à 75 m du joueur.
   - Les éclats noirs ont une cause précise. Les `Jardin_Lisiere_12m` (24 dans le seul Potager) sont posées **devant** ces murs
     au lieu de les remplacer. Leurs cartes de feuillage sombre se découpent sur la roche éclairée, d'où des rangées d'éclats
     noirs et de longs traits verticaux (les troncs).
   - À faire (main) : retirer `Mur` et `Falaise` derrière les lisières. La lisière ferme seule : `Col_Lisiere` fait 22 m de haut
     et elle a son propre fond noir à 6,5 m. La forêt se lira alors sur le ciel. Là où il faut un relief, garder une falaise
     basse, sous 8 m, derrière la lisière.
   - Côté DA : rendre `M_Kit_Feuillage_Sombre` moins noir pour le cas d'un fond éclairé.
4. **DA — `Ens_Buis_Boule` : trous triangulaires noirs** (rangée 4, image a). *Nouveau ; à corriger par la DA (PR #59).*
   Des faces du buis rendent en noir (faces arrière ou normales inversées, comme les cartes de la nuit 6). Il est en premier plan
   dans le Potager. À reprendre dans `Assets/Art/Source~/Ensembles/`.
5. **Pans isolés au milieu des salles** (signalé par main : « pan de mur isolé dans la salle de bal » ; rangée 5). *En partie.*
   - Grande salle (acte II, graine 22) : une `Chateau_Table_Renversee` vue par-dessous se lit comme un pan de bois debout. La
     tourner pour qu'on voie ses pieds, ou la poser contre un mur.
   - Bibliothèques (deux graines) : les `Chateau_Bibliotheque` posées au milieu montrent leur dos nu, de grands panneaux de bois
     sans livres. Les poser dos à dos, par paires, ou contre un mur.
   - Je n'ai pas trouvé de pan de mur dans le Palier, sur aucune des deux graines. Le grand panneau rouge à gauche de la vue du
     palier est le mur de la salle à manger, vu par une large ouverture : ce n'est pas un défaut. Si main pense à un autre objet,
     donner sa position.
6. **Halls de sortie vides** (rangée 6, images a et b). *Reste du point 1.*
   - Actes I et II : le hall de sortie est une salle nue (dallage, murs, appliques), sans rien d'autre.
   - Les autres salles sont maintenant bien remplies : ensembles, colonnades, sources au sol.
   - La Galerie de mine de l'acte I garde de grands sols de terre presque vides.
7. **Greybox : silhouette du marchand** (rangée 6, image c, bord gauche). *Nouveau.*
   `Jeu du niveau/Marchand de l'artefact (salle de départ)/Marchand (silhouette)` : bloc bleu marine de 0,9 × 2,2 m, matériau
   `Economy_Merchant`. À remplacer par un vrai modèle ou à cacher derrière un étal (demande à la DA si besoin).
8. **Mineurs.**
   - `Prop_Chaudron` sans collision : 1 cas (acte I, graine 11) et 2 cas (graine 22). C'est un reste du point 7.
   - Acte III : la `Jardin_Haie_6m` de l'Allée des statues se lit comme un cube brun à l'ombre (rangée 4, b et c). À éclairer,
     ou demander à la DA une haie plus claire.
   - Débris noirs semés sur les sols de l'acte II (palier) : à distance, ils se lisent comme des trous.

## Points de la première revue

| # | Point | État |
|---|---|---|
| 1 | Salles vides et démesurées | **En partie corrigé** : salles remplies par les ensembles, colonnades et sources ; restent les halls de sortie et quelques grands sols (point 6 ci-dessus) |
| 2 | Jardin pas à ciel ouvert | **En partie corrigé** : ciel ouvert, sol mélangé, bosquets ; murs et falaises toujours là (point 3 ci-dessus) |
| 3 | Damier sur murs et contremarches, carreaux trop grands | **Corrigé** : damier sur les sols seulement ; carreaux d'environ 1 m à l'œil, acceptable |
| 4 | Lustres empilés | **Corrigé** : 0 paire à moins de 3 m sur les 6 cartes |
| 5 | Torches murales sans mur | **Corrigé** : plus aucune torche dans les flottants |
| 6 | Décor qui se chevauche | **Corrigé** : de 59 / 87 à 45 / 52 cas (acte I), de 143 / 220 à 119 / 101 (acte II), tous des groupes voulus (voir « Lecture de l'audit ») |
| 7 | Décor traversable | **Presque corrigé** : `Cave_Brasero` réglé ; restent 1 à 2 `Prop_Chaudron` |
| 8 | Greybox visible | **En partie corrigé** : gravats et monolithe non revus ; la silhouette du marchand reste (point 7 ci-dessus) |
| 9 | Annonces rouges | **Corrigé** : aucun disque rouge sur les 168 vues |
| 10 | Échelle des textures | **Corrigé** : briques à l'échelle sur les murs et les marches |
| 11 | Obscurité | **En partie corrigé** : l'acte II est bien éclairé au sol ; l'acte I reste sombre dans le Gouffre, l'Ossuaire et la Grotte inondée ; plafonds (point 1 ci-dessus) |
| 12 | Eau sans rive | **Corrigé** : margelles et bords de pierre (Grotte inondée, Bains) |
| 13 | Chaînes des cages sans plafond | **Non vérifié** sur les vues ; même défaut que les plateformes de traction (point 2 ci-dessus) |

## Outils de la seconde revue

Dans le dossier de travail de la session (non versionnés) :
- `carte.sh` (préparation, audit, vues, vues vers le haut), `sonde.sh` (identification d'objets par rayon) ;
- `CapturesJeu.cs` (avec `ShootH` et `Sonde`), `AuditCartes.cs`, `Haut.cs` (objets en hauteur d'une salle).

La graine est forcée par la préférence d'éditeur `ItHunts.Outils.SeedForcee` (celle des outils de main). Elle a été remise à 22,
sa valeur avant la revue.

---

# Revue des cartes faites à la main (nuit 5, lot 5)

Revue de la DA, pour main. Cartes de la branche `animations` au commit `f3f5cd0` (« Jour 5, point 1 », D-110), pas encore
dans `main`. Les 6 cartes sont lancées dans leur scène d'acte (graine 22 : carte 1 de l'acte, graine 11 : carte 2) :

| Acte | Graine 22 | Graine 11 |
|---|---|---|
| I (`Acte_1_Caves`) | Les mines effondrées | Les puits noyés |
| II (`Acte_2_Chateau`) | Le château des cendres | Le cloître et les tours |
| III (`Acte_3_Jardin`) | La forêt des statues | Les jardins en terrasses |

**Méthode.** Pour chaque carte, en Play Mode, avec l'éclairage du jeu :
- 28 vues (2 par salle, de près et de loin, pour 14 salles) ;
- un audit automatique du décor : objets flottants ou mal accrochés, au-dessus du vide, lustres collés, décor traversable,
  chevauchements ;
- des gros plans automatiques des cas signalés.

Preuves dans `docs/art/rendus/Nuit5/Lot5/` :
- `Salles_C<acte>_s<graine>.jpg` : 24 vues par carte ;
- `audit_*.txt` : listes complètes avec les positions ;
- `Planche_Defauts.jpg` : gros plans.

L'audit repère les objets par leur nom (`Prop_*`, `*Torche*`, `*Lustre*`, `*Chandelier*`…). Il ne voit donc pas le décor
fusionné de l'acte III (`Décor fusionné (…)`), que seules les vues couvrent.

## À corriger en priorité (bloquant pour des images de boutique)

1. **Salles vides et démesurées, dans les trois actes.**
   - De grands sols plats sans rien dessus : seulement deux ou trois props pour 30 à 60 m de côté, et des murs nus.
   - On le voit sur presque toutes les vues « loin » : Acte II, damier ; Acte I, dallage ; Acte III, gravier sur des
     dizaines de mètres.
   - La surface a été multipliée par 4 à 6 (règle de `CARTES.md`) sans que le décor suive. À pleine vitesse, le joueur ne lit
     plus aucun repère.
   - À faire, au choix :
     - densifier : colonnes, piliers, estrades, mobilier et ruines en groupes, toutes les 6 à 10 m ;
     - ou réduire les surfaces vides en découpant les grandes salles par des volumes : mezzanines, ponts, coursives, salles
       hautes, pièces du lot 3 de la nuit 5.
2. **Acte III : le jardin ne paraît pas à ciel ouvert** (`Salles_C3_*`).
   - Le ciel n'apparaît que par une fente en escalier dans un plafond ou des murs noirs, avec des marches de 8 m bien
     visibles (vue `00_Potager_loin`).
   - Le sol est un gravier uniforme partout.
   - À faire :
     - plafond ouvert sur toute la salle ;
     - limites en `Jardin_Lisiere_12m` et `Jardin_Lisiere_Coin` (PR #56) à la place des murs ;
     - sol en `M_Jardin_SolMelange` ;
     - arbres et sous-bois du lot 3 en bosquets.
3. **Acte II : le damier du sol est posé sur des murs et des contremarches** (`Salles_C2_s22`, rangées 1 et 2, dernière
   colonne).
   - Le matériau de sol est appliqué à des faces verticales.
   - Et ce damier est trop grand : des carreaux d'environ 2 m, contre 0,5 à 0,8 m à l'échelle humaine.
4. **Acte II : lustres empilés au même point.**
   - L'audit trouve 29 paires de chandeliers à moins de 3 m l'un de l'autre (carte graine 11) et 24 (graine 22), dont
     plusieurs à 0 m : doublons exacts.
   - Exemples en (−123, 23.6, −67) et (−51, 23.6, −247). Voir `Planche_Defauts.jpg`, rangée 3, image 1.
   - Un seul lustre par point d'accroche, au moins 6 m entre deux lustres.
5. **Acte I : torches murales sans mur.**
   - 48 cas dans la carte graine 11 et 35 dans la graine 22.
   - Les torches restent en l'air au bord d'une ouverture percée après coup (galerie, porte haute, encoche de 16 m), ou à
     côté d'un pilier (`Planche_Defauts.jpg`, rangée 1).
   - À faire : retirer ou déplacer une torche dès que son mur est percé ; l'audit donne la position de chacune.

## Important

6. **Décor qui se chevauche.**
   - Nombre de cas : 59 et 87 (acte I), 143 et 220 (acte II).
   - Cas typiques :
     - torche plantée dans un `Prop_MurEffondre` ou contre une `Prop_Statue` (rangée 2 de la planche) ;
     - `Prop_Etagere` posées l'une dans l'autre (acte II, rangée 3, image 2) ;
     - caisses dans des tables, tonneau dans une pile de caisses.
   - À faire : réserver l'emprise au sol de chaque prop au placement, et éloigner torches et appliques d'au moins 1 m d'un
     prop haut.
7. **Décor traversable** (acte I : 15 et 16 cas).
   - `Cave_Brasero` (0,9 × 1,1 m) et `Prop_Chaudron` (1,2 à 1,4 m) n'ont pas de collision : le joueur et les goules passent
     à travers.
   - Les tapisseries sont exclues de ce compte : elles sont murales, sans collision voulue.
8. **Greybox encore visible.**
   - Gravats en cubes bleu nuit (`Prop_Gravats`, acte II, « Tour effondrée ») et monolithe gris sans matériau (acte II,
     graine 22, rangée 4).
   - L'audit ne trouve aucun sol sous les gravats de la tour effondrée, en y = −5,5 : il faut vérifier que le fond de la tour
     a bien une collision.
9. **Annonces rouges encore présentes.**
   - Le disque orange-rouge du bond des goules se voit sur la carte graine 11 de l'acte I (rangées 2 et 4).
   - C'est le lot 4 de la DA (PR #57) : voir la section « Nuit 5, lot 4 » de `ART-HANDOFF.md`.
10. **Échelle des textures.**
    - Briques et dalles géantes sur certains murs et escaliers : à peu près ×2 à ×3 (acte I, rangée 4 ; acte II, escaliers).
    - Le pavage de l'escalier ne suit pas les marches.
    - Garder la densité de texels de la nuit 4 (`docs/art/ART-HANDOFF.md`, « Densité de texels ») : environ 1 brique de
      0,25 m de haut.
11. **Obscurité.**
    - Beaucoup de vues de loin sont presque noires : sur 20 à 40 m, la seule lumière est la lanterne.
    - Torches et braseros manquent loin des murs.
    - Proposition : un brasero ou un lustre par zone de 12 × 12 m, et des sources sur les îlots et les ponts. Le décor doit
      se lire à 20 m, et tout ce qui est interactif doit briller (pilier 4).

## Mineur

12. Eau de l'acte I (« Grotte inondée ») : grands plans d'eau plats, sans bord ni rive. Les poser contre des rochers ou des
    margelles.
13. Les cages et les chaînes des salles de torture ne sont pas raccordées à un plafond proche (l'audit ne trouve rien à
    0,6 m au-dessus). À vérifier en jeu : il faut que les chaînes montent jusqu'à une poutre visible.

## Ce qui est bien

- Les paliers et les escaliers donnent enfin de la verticalité.
- Les portes de salle sont lisibles, et les grandes portes de bois sont dans le style.
- Le château alterne bien les matériaux : tapis, parquet, papier peint.
- Les arbres de l'acte III ont la bonne échelle.

## Outils

Outils de cette revue (dans le dossier de travail de la session, non versionnés) : `AuditCartes.cs` (audit et gros plans),
`CapturesJeu.cs` (vues des salles), `carte.sh` (une carte : préparation, audit, vues). La DA peut refaire la revue quand main
aura corrigé les cartes.
