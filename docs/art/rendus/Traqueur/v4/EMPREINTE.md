# Traqueur v4 : empreinte du modèle (VERROUILLÉ depuis le 2026-11-21)

Le maillage, les poids et les formes (blend shapes) du traqueur sont **verrouillés**. Les animations ne touchent qu'à la
**rotation des os**, plus la **position de `Racine` et de `Bassin`**. Jamais d'échelle d'os différente de 1, jamais de déplacement
d'un autre os (aucun membre étiré), jamais de modification du maillage ou des poids pour « arranger » une animation.

Contrôle après chaque animation (Blender, `Assets/Art/Source~/Ennemis/scripts/anim_traqueur.py`) :
- `empreinte()` doit redonner exactement les valeurs ci-dessous (et `empreinte.json`, la même chose en données) ;
- `verifier_action(action)` doit renvoyer une liste vide (aucune clé d'échelle, aucune clé de position hors `Racine` et `Bassin`).
Si une valeur change : annuler le changement et le signaler. Si une animation ne peut pas être fidèle sans toucher au modèle : la
sauter et expliquer pourquoi, avec une capture.

Source : `Assets/Art/Source~/Ennemis/Traqueur_v4/SK_Traqueur_v4.blend` (scène `Traqueur_v4`, armature `Traqueur`) ; export
`Assets/Art/Models/Ennemis/SK_Traqueur_v4.fbx`. Unités : mètres, repère Blender (Z en haut, face vers -Y), pose de repos.

## Maillages

| Objet | Sommets | Triangles | Boîte min (x, y, z) | Boîte max (x, y, z) | Somme des poids | Formes |
|---|---|---|---|---|---|---|
| `SK_Traqueur_v4_Corps` | 23500 | 37999 | -5.7046, -1.343, 0.0 | 5.7077, 1.347, 11.915 | 23500.0 | — |
| `SK_Traqueur_v4_Tete` | 10049 | 15827 | -0.5516, -0.6991, 11.5472 | 0.8148, 0.8671, 14.0 | 10049.0 | Base, Cri, MachoirePendante, LevresRetroussees, YeuxPlisses, YeuxEcarquilles, Clignement |
| `SK_Traqueur_v4_Cheveux` | 3240 | 4152 | -1.2785, -0.8539, 8.1964 | 1.5241, 1.5209, 14.1946 | 3416.107 | — |

Total : **57978 triangles**, 68 os.

## Longueur des os (pose de repos, m)

| Os | Longueur | Os | Longueur | Os | Longueur |
|---|---|---|---|---|---|
| `AvantBras_D` | 2.7285 | `AvantBras_G` | 2.7285 | `Bassin` | 0.8459 |
| `Bras_D` | 3.3486 | `Bras_G` | 3.3486 | `Cheveux_1_1` | 0.9277 |
| `Cheveux_1_2` | 0.9326 | `Cheveux_1_3` | 0.9321 | `Cheveux_1_4` | 0.9255 |
| `Cheveux_2_1` | 1.0306 | `Cheveux_2_2` | 1.0342 | `Cheveux_2_3` | 1.0064 |
| `Cheveux_2_4` | 1.0289 | `Cheveux_3_1` | 0.9249 | `Cheveux_3_2` | 0.9315 |
| `Cheveux_3_3` | 0.933 | `Cheveux_3_4` | 0.9266 | `Cheveux_4_1` | 1.025 |
| `Cheveux_4_2` | 1.0266 | `Cheveux_4_3` | 1.0027 | `Cheveux_4_4` | 1.0267 |
| `Cheveux_5_1` | 1.0516 | `Cheveux_5_2` | 1.0559 | `Cheveux_5_3` | 1.0544 |
| `Cheveux_5_4` | 1.0528 | `Cheveux_6_1` | 1.0567 | `Cheveux_6_2` | 1.0611 |
| `Cheveux_6_3` | 1.06 | `Cheveux_6_4` | 1.0593 | `Cheveux_7_1` | 1.0962 |
| `Cheveux_7_2` | 1.1074 | `Cheveux_7_3` | 1.1038 | `Cheveux_7_4` | 1.1047 |
| `Clavicule_D` | 1.1224 | `Clavicule_G` | 1.1224 | `Colonne_1` | 0.8499 |
| `Colonne_2` | 0.7403 | `Colonne_3` | 0.7412 | `Colonne_4` | 0.7427 |
| `Contact_Main_D` | 0.3171 | `Contact_Main_G` | 0.3171 | `Contact_Pied_D` | 0.3 |
| `Contact_Pied_G` | 0.3 | `Cou_1` | 0.6878 | `Cou_2` | 0.5777 |
| `Cuisse_D` | 3.6038 | `Cuisse_G` | 3.6038 | `Doigts_D` | 0.9499 |
| `Doigts_G` | 0.9499 | `IK_Main_D` | 0.74 | `IK_Main_G` | 0.74 |
| `IK_Pied_D` | 0.74 | `IK_Pied_G` | 0.74 | `Jambe_D` | 3.4264 |
| `Jambe_G` | 3.4264 | `Machoire` | 1.1559 | `Main_D` | 1.2519 |
| `Main_G` | 1.2519 | `Orteils_D` | 0.5302 | `Orteils_G` | 0.5302 |
| `Pied_D` | 0.9415 | `Pied_G` | 0.9415 | `Pole_Coude_D` | 0.5285 |
| `Pole_Coude_G` | 0.5285 | `Pole_Genou_D` | 0.5285 | `Pole_Genou_G` | 0.5285 |
| `Racine` | 0.8 | `Tete` | 1.4222 |  |  |
