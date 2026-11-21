# Traqueur : passage à un avatar Humanoid (préparation, 2026-11-21)

L'utilisateur refait le gréement du traqueur avec un outil standard, et refera les clips par capture vidéo. Cette note liste
ce qu'il faudra changer côté jeu quand le nouveau modèle arrivera. Le modèle ne change jamais : aucune échelle d'os,
aucune déformation (D-080).

## Déjà prêt

- **`StalkerRig`** (`Assets/_Project/Scripts/Runtime/Enemies/StalkerRig.cs`) donne un os par rôle : tête, cou, mâchoire,
  cuisses, jambes, pieds, bras, avant-bras, mains, points de contact.
  - Avec un avatar Humanoid, il passe par `Animator.GetBoneTransform`.
  - Sans avatar, il cherche l'os par son nom v4.
  - Le visage et l'IK des membres passent déjà par lui.
  - Il faudra seulement lui passer l'`Animator` (paramètre `animator`).
- L'**IK des membres** (`StalkerLimbIK`) et celle de la **prise** (`Stalker.SolveArm`) sont analytiques à deux os : elles ne
  dépendent que des trois os de chaque membre et de son point de contact.
- La **mâchoire** tourne autour de l'axe latéral du traqueur. Le sens est trouvé au démarrage (celui qui fait descendre le
  menton), quelle que soit l'orientation de l'os.

## À changer

| Sujet | Aujourd'hui (v4, Generic) | Avec l'avatar Humanoid |
|---|---|---|
| Os par nom | `Stalker.cs` cite encore 11 noms d'os (bras pour l'IK de la prise, `Racine`, `Bassin`, colonne) ; `StalkerProceduralPose` en cite 3 | Passer par `StalkerRig.Find(bones, rôle, animator)`, en ajoutant les rôles manquants (colonne, bassin, racine) |
| Points de contact | Os ajoutés `Contact_Main_*` et `Contact_Pied_*` (sous les griffes) | Pas d'os Humanoid pour eux : les garder comme os supplémentaires dans le gréement, avec les **mêmes noms** ; sinon, un décalage fixe depuis la main ou le pied |
| Mâchoire | Os `Machoire` et forme `MachoirePendante` | `HumanBodyBones.Jaw`, optionnel dans l'avatar : le **déclarer** dans l'avatar. Garder les **noms des 6 formes** (`Cri`, `MachoirePendante`, `LevresRetroussees`, `YeuxPlisses`, `YeuxEcarquilles`, `Clignement`) |
| Regard | Rotation du cou (40 %) et de la tête (60 %) après l'Animator | Possible avec `Animator.SetLookAtPosition` (OnAnimatorIK, calque « IK Pass »), ou rester sur la rotation après l'Animator (plus simple, déjà réglée) |
| IK des membres | Deux os, après l'Animator, rotations seules | Possible avec l'IK Humanoid (`SetIKPosition` mains et pieds) ou Animation Rigging. Garder la règle « seulement à portée réelle » (`limbIkReachMargin`) |
| Cheveux | 7 chaînes `Cheveux_N_1..4` simulées par `BoneSpringChain` | Hors de l'avatar : ce sont des os supplémentaires sous la tête. **Mêmes noms** et même hiérarchie, sinon adapter `Stalker.SetupHair` |
| Root motion | `PinRoot` ramène `Racine` à sa place (le code déplace le traqueur) | Avec Humanoid, le déplacement passe par le corps de l'avatar : couper « Apply Root Motion » et garder `Bake Into Pose` sur les clips |
| Clips et liens | 16 états nommés `Traqueur_*` ; instants d'impact réglés dans `StalkerSettings` (prise 2.37 s, prise de rebord 3.25 s, chaîne 3.75 s, bonds…) ; durées de clip utilisées par le coût des liens NavMesh (`StalkerPassageMath.ClipTime`) | Garder les **mêmes noms d'états** et remesurer chaque instant d'impact et chaque durée (`clip*` dans `Stalker_Default`). `EnemyAnimationSetup` reconstruit l'Animator à partir des clips trouvés |
| Mouvement provisoire | `StalkerProceduralPose` anime les os par leur nom quand un clip manque | Le passer par `StalkerRig`, ou le retirer une fois que tous les clips existent |
| Mesures | Épaule (10.5 m), bras réel (8.3 m), portée de prise (14.8 m), porte étroite (4 × 5.2 m), taille de l'agent NavMesh | À **remesurer** sur le nouveau modèle (`ReachCheck.Configure`, validateur des modules) |

## Ordre conseillé

1. Importer le modèle en Humanoid. Déclarer la mâchoire dans l'avatar. Garder les os supplémentaires : contacts et cheveux.
2. Passer `animator` à `StalkerRig` et migrer les noms restants de `Stalker.cs` et `StalkerProceduralPose`.
3. Lancer `EnemyAnimationSetup`, puis remesurer les instants d'impact et les durées des clips.
4. Relancer les mesures de portée et le validateur des modules. Mettre à jour `docs/art/ART-HANDOFF.md`.
