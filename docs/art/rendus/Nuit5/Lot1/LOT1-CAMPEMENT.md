# Lot 1 de la nuit 5 : campement, places vides et ombres

## Demande

- Places vides : des ombres noires en forme de silhouette derrière les « + » ; pareil autour de certains personnages (koala, J1).
- Vérifier les 4 personnages à chaque place, et en coop (2 à 4 joueurs).
- Contrôle : captures de chaque place, vide et occupée, à côté de la maquette.

## Causes

1. **Calques orphelins.** `MenusBuilder.CompleteCampement` déplaçait les ombres et la lumière de lanterne de chaque place dans le
   calque commun `Sol_Lumieres_Ombres`. À la reconstruction suivante, il ne les retrouvait plus sous la place et en recréait :
   les anciens restaient dans le calque commun, reliés à aucune place, donc jamais cachés. Le prefab en contenait 36 au lieu
   de 12 : l'ombre du hibou de l'état de départ à la place 2 (silhouette derrière le « + »), et trois ombres du renard
   empilées à la place 1 (halo sombre autour de J1, et autour du koala assis à la place 2).
2. **Calque au-dessus de la place 4** (relecture). L'index du calque était lu avant de le retirer : décalage d'un cran, il se
   dessinait sur le personnage de la place 4 (jambes assombries).
3. **Arrivée d'un joueur** (relecture). Le fondu passe par le `CanvasGroup` de la place, mais les ombres vivent dans le calque
   commun : pendant 0,6 s, ombre pleine sous un corps transparent. Et le fondu (ici et dans `MainMenuArt.Appear` de main)
   remettait `localScale` à 1, ce qui annulait le retournement des personnages aux places 3 et 4.
4. **Lueur verte du koala** en aplat sur la bûche et le rocher (relecture).

## Corrections

- Constructeur : calques retrouvés aussi dans le calque commun (`Eclairage_N`, `Ombre_Portee_N`, `Ombre_Lanterne_N`), rangés
  après la boucle, seuls les calques de place inconnus détruits ; calque placé juste sous la place 4 ; chaque lanterne éteinte
  juste sous sa place ; nombre de places lu sur le composant. Retouche idempotente : 12 calques après deux passes.
- `SuitGroupe` (nouveau) sur chaque calque d'ombre ou de lumière : il recopie l'opacité du groupe de sa place. L'ombre apparaît
  avec le personnage, **sans rien changer au code de main**.
- Retournement par une rotation de 180° au lieu d'une échelle −1 : les fondus peuvent régler l'échelle sans le défaire.
- Lueurs de lanterne ramenées à une même luminance perçue (`luminanceLueur` 0,4) et moitié moins de couleur du personnage
  au sol (`eclairageLanterne.z` 0,2 → 0,1).
- Robustesse : un personnage sans placement laisse la place vide ; un sprite manquant cache son ombre.

## Contrôle (vrai menu, `MainMenu.unity` en Play Mode, 1920 × 1080)

- `Campement_Places_Vides.jpg` : J1 seul, coop à 2, coop à 3, place 2 vide avec 3 et 4 occupées ; maquette, avant (prefab de
  main), après.
- `Campement_Place1.jpg` à `Campement_Place4.jpg` : chaque place à l'échelle 1:1 avec la maquette au même cadrage, vide puis
  chacun des 4 personnages, avant et après.
- `Campement_Joueur1.jpg` : chaque personnage seul, avec la maquette.
- `Campement_Coop.jpg` : coches « Prêt » (coop 2 et 4), arrivée d'un joueur figée à 40 % du fondu aux places 2, 3 et 4.
- Planches : `Assets/Art/Source~/UI/scripts/menus/planche_lot1_5.py`.

## Reste connu

- La place 1 n'est jamais vide en jeu (joueur 1) : pas de capture « vide » pour elle.
- Le caméléon de la place 4 a la queue qui s'étend vers le bord droit de l'écran (sprite retourné), sans être coupée en 16:9.
