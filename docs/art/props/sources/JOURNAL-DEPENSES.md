# Lot 4 (nuit 4) : journal des dépenses Higgsfield

Plafond du lot : 120 crédits (reprises comprises). Devis : 8 × (2 image 2K + 9 Tripo H3.1) = 88 crédits.

| Étape | Détail | Crédits | Solde |
|---|---|---|---|
| Solde de départ | 2026-11-23, forfait Plus | — | 898.72 |
| 8 images concept | Nano Banana 2.1, 2K, 1:1 (tonneau, chaise, poutre, sarcophage, râtelier, coffre, coffre verrouillé, statue), toutes gardées au premier essai | 16 | — |
| 8 modèles 3D | Tripo H3.1 image vers 3D (texture, sans PBR, face_limit 3000 à 7000, auto_size) | 72 | — |
| Solde après génération | relevé par l'outil balance | **88** pour ce lot | 801.72 |

Note : le solde a baissé de 97 crédits entre les deux relevés ; l'historique des transactions montre 3 images à 3 crédits
(11:57:23 UTC) lancées en même temps par une autre session, qui ne font pas partie de ce lot. Dépense du lot 4 : 16 + 72 =
**88 crédits** (plafond 120), aucune reprise.

Relevé final (après nettoyage, rendus et documentation, sans nouvelle génération) : **795.72**. Les 6 crédits de
différence avec 801.72 viennent d'autres sessions ; ce lot n'a rien lancé après les 16 générations ci-dessus.

Jobs Higgsfield (pour retrouver les sources) :

| Prop | Image (Nano Banana 2.1) | Modèle (Tripo H3.1, face_limit) |
|---|---|---|
| Tonneau | 82604087-ce11-4075-91c5-8b32910453ae | 04c4083f-2c62-466b-9ad9-71127285a69b (4000) |
| Chaise | f8e1721a-e5ae-4946-9f65-d7820c8f5396 | 850b7e1a-97ce-4a15-83e8-30c18dde82c4 (4000) |
| Poutre | 0aaa43a9-54f6-41e8-95c2-75b784f43811 | 341a35c8-3a0d-4391-a5a0-37b4223b4f63 (3000) |
| Sarcophage | 0c322ff8-616c-41ff-841a-a35caea385ce | cf159b3a-b964-456d-be1c-922f4a48ce89 (7000) |
| Râtelier | 40f446e3-62ac-4459-b689-6ad695ccf429 | 96569d3a-07f3-4fb7-8b96-0bdfa3f8586f (6000) |
| Coffre | 7a4f6381-38f3-4de7-b29b-20163d91d025 | 76edb32c-0ee6-49c4-8147-eba84f069a1f (5000) |
| Coffre verrouillé | 89197179-dad3-4a28-b3c6-6b396004c680 | 60c404e1-5875-4553-be99-31aba654b15e (7000) |
| Statue | 292935dc-3270-4d6e-a8a4-81b27fad7de1 | 6bb0f604-6e97-452b-a322-ac6bf7a09381 (7000) |

## Lot 4, deuxième passe (2026-11-23, branche `art-props-3d-b`)

Plafond : **112 crédits**. Devis : 9 × (2 image 2K + 9 Tripo H3.1) + 2 (texture de la paille, modélisée par script) = 101.

| Étape | Détail | Crédits | Solde |
|---|---|---|---|
| Solde de départ | relevé par l'outil balance, avant tout lancement | — | 787.22 |
| 10 images (14:15 UTC) | Nano Banana 2.1, 2K (1:1, poutre en 16:9), deux concepts du lot 4 (tonneau, chaise) en référence de style : lanterne, coffre décoratif, table, armoire, lit, chaudron, poutre v02, texture de paille ; étagère et banc refusés par la limite de 8 travaux simultanés (rien de lancé) | 16 | — |
| Coffre décoratif, 1er essai | refusé par le filtre de sécurité (« trunk »), **remboursé** | 2 − 2 | — |
| 3 images (14:16 UTC) | étagère, banc, coffre décoratif reformulé (« storage chest ») | 6 | — |
| Banc, reprise (14:22 UTC) | le premier banc montrait une chaise fantôme venue de l'image de référence (échec net) : refait avec la seule référence du tonneau | 2 | — |
| 9 modèles 3D | Tripo H3.1 image vers 3D (texture, sans PBR, auto_size, face_limit 2500 à 6000) : table, armoire, chaudron, lit, lanterne, étagère, coffre décoratif, poutre v02, banc. Quatre appels lancés ensemble ont expiré côté outil : seuls table, armoire et chaudron étaient partis (historique vérifié) ; le lit a été relancé une fois, sans doublon | 81 | — |
| Solde final | relevé par balance ; l'historique (`transactions`) ne montre aucune autre dépense entre les deux relevés | **103** pour cette passe | 684.22 |

Reste du plafond : 9 crédits, non dépensés. La paille n'a pas de modèle Tripo (maillage par `paille.py`).

Jobs Higgsfield (deuxième passe) :

| Prop | Image (Nano Banana 2.1) | Modèle (Tripo H3.1, face_limit) |
|---|---|---|
| Lanterne | a90680f2-3e58-4d51-9434-b81ace22dd00 | 5f600a2a-9e48-46de-82ac-1ae6f9f2f8fc (2500) |
| Étagère | 1e621628-57ab-4b2f-88ef-4bd64851e090 | 9e961355-ec78-45fd-b524-cedef6f10af2 (6000) |
| Coffre décoratif | 7ba2d6a1-0450-4c34-8149-26ddfd59e1e6 (1er essai refusé : 72082c26-651f-497a-949e-ab0f27174e88) | 948d2846-bbfd-4423-baa7-fb719bfe1fc5 (3500) |
| Table | bf924e36-9a8f-4573-952b-45a9aeeeef6a | b1b98e89-b903-4761-8505-f8a7859a6642 (3500) |
| Banc | 46e8ff1c-b17c-4de7-ab25-a2b0af2149a7 (rejeté : 229c9963-a5a5-4324-89d7-d5d069d58e3a) | 4354b2c3-3b54-45de-bc45-26d4b4110c1b (2500) |
| Paille (texture) | e0841a02-faf9-4643-85cf-06400ce2649a | — (maillage par script) |
| Armoire | 88886009-b992-492c-a855-f21fbad47cb3 | 95d7b9d1-7adf-4818-962b-6aa0401a7e25 (5000) |
| Lit | 6fcabdee-0c52-4f57-b131-1b049e44c47d | 9f786a2c-5700-4ba7-ae81-3b4577dd56e1 (4500) |
| Chaudron | 8bc375a1-645e-4e53-a046-ad8cb60f43d5 | 7fddc259-b17a-4efe-bd8d-1ff000bbef06 (4000) |
| Poutre v02 | 814a5d07-c33a-4b02-b77d-c2cff076076e | d615e5b4-75c6-41fe-a2f6-b5f09f02527f (3000) |

Références de style téléversées (non facturées) : concept_tonneau_v01 (acc58627-d82f-43af-82d2-783a78094e06),
concept_chaise_v01 (3b9ba558-89b6-4381-ad7e-8ac37ede9aa5).
