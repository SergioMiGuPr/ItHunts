# Lot 1 de la nuit 4 : campement, personnages assis

## Ce qui était demandé

Les personnages assis étaient toujours mal incrustés, et ils étaient devenus ternes et boueux. Sur le caméléon : un fond gris
clair dans la spirale de la queue, un bord de queue découpé et une lanterne qui flotte. Le critère est **visuel** : on ne doit
plus voir qu'un personnage a été ajouté ; il doit avoir l'air peint dans la maquette, avec la même vivacité que ses
personnages.

## Causes trouvées

1. **Teint terne et boueux.** Le fondu de la nuit 3 (`fondu.py`) réglait la luminosité et la saturation de chaque place pour
   **égaler le décor** qui l'entoure. Or, mesurés de la même façon, les personnages de la maquette sont **3 à 4 fois plus
   lumineux** que leur décor (renard : médiane ×2,8 et 90ᵉ centile ×3,6 ; hibou : ×4,0 et ×3,5). Les sprites étaient donc
   assombris jusqu'à 0,56 et désaturés jusqu'à 0,34.
2. **Fond gris dans la spirale.** Le détourage bouchait tous les trous fermés de la silhouette.
3. **Bord de queue découpé.** Des faces de la caisse du dessin d'origine (mauve à l'ombre, orangée à la lumière de la lanterne)
   restaient collées à la queue et sous la cape.

## Ce qui a été fait

- **Sprites** (`assis.py`) :
  - un trou fermé redevient transparent quand il a la couleur du fond **et** un intérieur parfaitement lisse : la peinture a
    toujours des touches ou de l'encre. Les seuils viennent de la mesure de tous les trous des 4 personnages. Résultat : la
    spirale, l'espace entre les jambes, sous l'aile et entre les serres sont vides, tandis que la fourrure grise, les visages et
    les oreilles restent ;
  - les restes de caisse collés au personnage sont grignotés depuis le bord, puis les îlots détachés sont retirés
    (`retire_restes_caisse`).
- **Lumière** : un premier essai visait les chiffres de la maquette (gain et contraste réglés sur la médiane et le 90ᵉ
  centile). Il a donné des personnages néon aux places 3 et 4 : **essai rejeté sur captures**.

  Retenu : les sprites sont déjà étalonnés sur les personnages de la maquette à leur construction (`grade`), donc le shader les
  laisse **intacts** (luminosité, saturation et contraste à 1) et n'ajoute que la lumière de la scène :
  - côté feu chaud, côté lune froid ;
  - lueur de la lanterne sur les jambes ;
  - ombres de contact et ombres portées.

  Un contraste par place reste disponible (`Placement.contraste`, `fondu_reglages.json`).
- **Lanternes** : lueur au sol resserrée au pied de la lanterne, en ellipse, et couleur adoucie, au lieu des grandes taches
  colorées (`eclairageLanterne` passe de 1,6 / 0,55 / 0,85 à 1,15 / 0,42 / 0,55, force 0,6). Chaque lanterne reste posée à
  l'emplacement de celle de la maquette : au sol devant la bûche, sur la pierre, sur le bloc au bout du banc, sur le bloc du
  rocher.
- **Ordre de dessin** : les places se dessinent de droite à gauche (4, 3, 2, 1). La queue d'un caméléon passe derrière la
  lanterne du banc et derrière la botte de son voisin, au lieu de passer devant.

## Contrôle

- `Campement_Places.jpg` : chaque place en grand, avec la maquette à gauche puis les 4 personnages.
- `Campement_AvantApres.jpg` : chaque personnage à chaque place, avant (nuit 3) et après.
- `Campement_Zoom3.jpg` : zoom ×3 sur la queue du caméléon aux 4 places et sur les bords de chaque personnage (tête, pieds,
  contact avec le siège).
- Captures Unity gelées (1670 × 941) par `MenusBuilder.Capture`, configurations A à D : chaque personnage passe par chaque
  place.
