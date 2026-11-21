# Traqueur v4 : notes (version b, 2026-11-21, branche `art-traqueur-v4`)

**Planche : `planche_v4b.png`.**
- Modèle de base Tripo et rendu Unity, sous les mêmes angles : face, profil, dos, trois quarts.
- Pose voûtée de profil, à côté d'une image de la vidéo `traqueur_ref_repos`.
- Visage : repos, puis les 6 formes.

L'ancienne planche `planche_v4.png` (version a) reste pour mémoire.

Fichiers :
- Scène Unity : `Assets/Art/Scenes/Vitrine_Traqueur_v4.unity`.
- Scripts : `sk_traqueur_v4.py` (assemblage, gréement, bouche, formes, cheveux, export) et `anim_traqueur.py` (outils de pose et contrôle du verrou) dans Blender, `TraqueurV4Builder.cs` dans Unity. Les trois sont dans `Assets/Art/Source~/Ennemis/scripts/`.
- Fichier Blender : `Assets/Art/Source~/Ennemis/Traqueur_v4/SK_Traqueur_v4.blend`.
- **Empreinte du modèle verrouillé** : `EMPREINTE.md` et `empreinte.json`.

## Référence d'apparence : le modèle Tripo de l'utilisateur

Les sources sont les deux GLB : `traqueur_v4_corps_source.glb` et `traqueur_v4_tete_source.glb`. Les vidéos servent de référence **pour les mouvements uniquement**.

1. **Aucune déformation.** Il n'y a ni changement de proportions ni étirement d'un segment : les membres sont ceux du modèle Tripo.
2. **Tête, une seule mise à l'échelle (uniforme, tête seule).** Elle sert à faire correspondre les cous, en mesurant la largeur minimale de chacun (repère source) :
   - cou du corps : **0.0955** (à z = 0.674) ;
   - cou de la tête : **0.0910** (à z = -0.066 dans son fichier) ;
   - échelle obtenue : **× 1.050**.

   **Comparaison avec l'ancienne version (× 1.25)** : l'ancienne tête était **19 % trop grosse** (1.25 / 1.05).

   La nouvelle tête n'est pas plus grosse que celle du modèle de base. Le corps Tripo porte son propre crâne : 0.20 de large et 0.23 du cou au sommet. La nouvelle tête mesure **0.18 de large et 0.25 du cou au sommet**, soit plus étroite et à peine plus haute. Avec l'ancienne échelle, elle mesurait 0.21 × 0.30.

   Placement :
   - les deux cous sont centrés l'un sur l'autre, à la même hauteur pour leur partie la plus étroite ;
   - le crâne du corps est retiré au-dessus du cou ;
   - le collet de la tête recouvre le moignon (emboîtement, pas de soudure : le maillage Tripo est fait de coques ouvertes).
3. **Une seule échelle uniforme pour 14 m** : **× 7.663**, sur le corps et la tête ensemble. Pieds à z = 0, sommet du crâne à 14.0 m ; les cheveux dépassent de 0.19 m.
4. **Pagne gardé.**
5. **Allègement** (sans changer la forme) :
   - corps : 38 000 triangles ;
   - tête : 15 827 triangles (bouche et dents comprises) ;
   - cheveux : 4 152 triangles ;
   - **total : 57 978**, dans le budget du boss (40 000 à 60 000).

   Textures 2048 : `T_Traqueur_v4_Corps_*` et `T_Traqueur_v4_Tete_*`. La peau de la tête a été harmonisée sur celle du corps. La normal map est celle de Tripo, elle n'a pas été recuite.

## Visage

**Orbites creusées**, avec un matériau noir mat (`M_Traqueur_v4_Orbite`).

**Bouche ouverte possible**, ajoutée pour que les formes soient lisibles :
- la peau est fendue le long de la ligne des lèvres (les lèvres se séparent sans étirer la peau) ;
- une cavité sombre est placée derrière les lèvres ;
- deux rangées de dents pointues sont ajoutées (9 en haut, 8 en bas, `M_Traqueur_v4_Dents`).

Au repos, la bouche reste fermée comme sur le modèle Tripo.

La **mâchoire** utilise l'os `Machoire`, qui pivote à la charnière sous les oreilles. Ses poids sont lissés vers les joues, l'oreille et le cou. Les formes de bouche utilisent les mêmes poids.

**6 formes**, réglées pour être nettement lisibles à la distance de jeu (voir la planche) :

| Forme | Effet |
|---|---|
| `Cri` | mâchoire ouverte de 44°, de travers (roulis de 15°, décalage latéral), lèvres retroussées, coins étirés : bouche très ouverte, dents du haut et du bas visibles |
| `MachoirePendante` | ouverte de 27°, nettement de travers (roulis de 13°) |
| `LevresRetroussees` | lèvre supérieure relevée, lèvre inférieure abaissée : les deux rangées de dents se voient, bouche fermée derrière |
| `YeuxPlisses` | l'orbite se réduit à une fente |
| `YeuxEcarquilles` | orbite agrandie d'environ 30 % |
| `Clignement` | paupières fermées, le trou noir disparaît |

## Cheveux (`SK_Traqueur_v4_Cheveux`)

Un rideau noir, épais et lisse, avec la raie au milieu, en **48 mèches**. Ce sont des rubans bombés, larges de 0.55 à 0.8 m, à 4 sommets en travers, ombrés lisses et visibles des deux côtés. Les pointes sont effilées (2 ou 3 dents) et toutes orientées vers le bas.

Les mèches sont réparties en trois groupes, sur deux couches décalées (0.06 m et 0.15 m au-dessus de la peau) :

| Groupe | Nombre | Trajet |
|---|---|---|
| Mèches avant | 6 par côté | de la tempe, devant l'épaule, jusqu'à la poitrine (9.7 à 10.4 m) |
| Mèches latérales | 10 par côté | par-dessus l'épaule, dans le dos (9.2 à 9.9 m) |
| Mèches du dos | 8 par côté | jusqu'au milieu du dos (8.7 à 9.3 m) |

Les longueurs sont étagées de l'avant vers l'arrière et les pointes sont écartées : de profil, le rideau suit la tête et les épaules et ne forme plus un bloc carré. Le visage reste dégagé de face. Les mèches s'appuient sur la peau de la tête, du cou, des épaules et du dos, jamais sur les bras.

**Chaînes d'os conservées** : 7 chaînes de 4 os, suivant les mêmes noms qu'avant :
- `Cheveux_1_*` : avant gauche ;
- `Cheveux_2_*` : côté gauche ;
- `Cheveux_3_*` : avant droit ;
- `Cheveux_4_*` : côté droit ;
- `Cheveux_5_*` : dos gauche ;
- `Cheveux_6_*` : dos droit ;
- `Cheveux_7_*` : dos milieu.

Sur le crâne, les mèches sont liées à `Tete`, puis passent aux 4 os de leur chaîne avec un fondu.

**Physique des cheveux** : `main` a choisi le ressort maison (`BoneSpringChain`, réglages dans `Stalker_Default`).
- Os à simuler : `Cheveux_c_2` à `_4`.
- Collisions conseillées (à 14 m) :
  - tête : sphère de r = 0.95 m sur `Tete` ;
  - cou : capsule de r = 0.4 m de `Cou_1` à `Cou_2` ;
  - épaules : capsules de r = 0.55 m sur `Clavicule_G` et `Clavicule_D` ;
  - haut du dos : capsule de r = 0.75 m de `Colonne_3` à `Colonne_4` ;
  - poitrine : sphère de r = 0.9 m sur `Colonne_4`.

## Gréement

L'armature `Traqueur` compte **68 os**, avec les mêmes noms que la version a. Les positions ont été recalées par le changement d'échelle.

Os du tronc et de la tête :
- `Racine` (au sol) ;
- `Bassin` ;
- `Colonne_1` à `_4` ;
- `Cou_1`, `Cou_2` ;
- `Tete`, `Machoire`.

Os de chaque côté (`_G` gauche, `_D` droite) :
- `Clavicule`, `Bras`, `AvantBras`, `Main`, `Doigts` ;
- `Cuisse`, `Jambe`, `Pied`, `Orteils`.

Points de contact nommés :
- `Contact_Main_G/D`, sous les griffes ;
- `Contact_Pied_G/D`, sous la plante, exactement à z = 0.

Os pour l'IK du jeu (non déformants) :
- cibles `IK_Main_G/D`, `IK_Pied_G/D` ;
- pôles `Pole_Coude_G/D`, `Pole_Genou_G/D`.

Les poids sont calculés par distance aux os. Une pose d'essai (dos courbé, bras et jambes pliés, mâchoire ouverte) ne déchire pas le maillage.

## Portée réelle (mesurée sur le modèle à 14 m)

| Posture | Mesures |
|---|---|
| Debout, bras tendu | Épaule à **11.1 m** du sol, à 1.37 m de l'axe. Bras de **8.3 m** de l'épaule au bout des griffes une fois tendu (7.8 m dans la pose de repos, coude un peu plié). Portée horizontale : **9.6 m** depuis l'axe du corps (environ 8.3 m devant la poitrine). Portée vers le haut : **19.4 m**. |
| Debout voûté (pose de la vidéo de repos, planche) | Hauteur **11.7 m**, cheveux compris. Épaules à 10.5 m, 0.9 m devant les pieds. Portée vers l'avant à hauteur d'épaule : **≈ 9.2 m** devant les pieds. Bras ballants, les griffes descendent à 2.3 m du sol (mi-tibia, comme dans la vidéo). Il doit plier le dos pour toucher le sol. |
| À quatre pattes | Mains au sol, bras presque verticaux : épaules vers **7.6 à 8 m**, haut du dos **≈ 8.3 m**, hanches de 5.5 à 7 m (jambes de 7.0 m, pliées). Portée vers l'avant sans baisser les épaules : ≈ 3.3 m devant l'épaule. En s'aplatissant (épaules à 4 m) : **≈ 7 m**. **Estimation**, à confirmer avec les animations. |
| **Plus petite porte franchissable en se faufilant** | **≈ 4 m de large × 6 m de haut** : torse de 2.8 m de large et 2.15 m d'épaisseur, épaules de 2.75 m plus les coudes repliés, rampé bas avec le dos vers 5.5 m. **Estimation, à confirmer avec l'animation `Traqueur_PorteEtroite`.** Dans le kit des modules : `GrandePorte_4x10` et `_6x10` l'obligent à se faufiler, `_6x12` à se baisser. |

## Écarts avec les vidéos (attendus : la silhouette peut différer)

- Dans les vidéos, les bras et les jambes sont plus longs et plus fins. Le modèle garde les proportions Tripo. Les animations compenseront par plus de flexion du dos, des épaules et des hanches (consigne de la partie 2).
- Les cheveux des vidéos sont plus fins et couvrent le visage quand la tête pend. Les mèches de la v4b retombent aussi devant le visage dans la pose voûtée (chaînes d'os vers le bas).
