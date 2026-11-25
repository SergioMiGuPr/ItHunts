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
