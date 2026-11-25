# -*- coding: utf-8 -*-
"""Documentation des ensembles : section d'INTEGRATION-PROPS.md (réécrite entre ses bornes) et lignes du manifeste.
    python ens_doc.py <fiches.json (ensembles.py)> <construit.log (EnsembleBuilder.Construit)>"""
import json, os, re, sys
R = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), *[".."] * 5))
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
fiches = json.load(open(sys.argv[1], encoding="utf-8"))
pieces = json.load(open(os.path.join(R, "docs", "art", "ensembles", "pieces.json"), encoding="utf-8"))
mes = {}
for l in open(sys.argv[2], encoding="utf-8", errors="replace"):
    m = re.match(r"(\w+) ([\d,]+) x ([\d,]+) x ([\d,]+) min .* tri (\d+) lum (\d+) col (\d+) rend (\d+)", l.strip())
    if m:
        mes[m[1]] = m.groups()[1:]
# descriptions des pièces : docstring du script, sinon nom
doc = {}
for f in ("pieces_commun.py", "pieces_chateau.py", "pieces_jardin.py"):
    s = open(os.path.join(R, "Assets", "Art", "Source~", "Ensembles", "scripts", f), encoding="utf-8").read()
    for m in re.finditer(r'@piece\("(\w+)"[^\n]*\)\ndef _\(e[^\n]*\):\n(?:    """(.*?)""")?', s, re.S):
        doc[m[1]] = re.sub(r"\s+", " ", m[2] or m[1].replace("_", " "))
ACTES = [("Caves", "Acte I : caves"), ("Chateau", "Acte II : château"), ("Jardin", "Acte III : jardin")]
o = []
o.append("<!-- ENSEMBLES-DEBUT (généré par ens_doc.py) -->\n## Nuit 6, lot 1 : ensembles de décor (branche `art-ensembles`)\n")
o.append(f"""**{len(fiches)} prefabs prêts à poser** dans `Assets/Art/Levels/Ensembles/<Acte>/`, deux variantes par ensemble : meubles, objets, lumières,
collisions et repères. Ils remplissent les salles vides signalées dans `REVUE-CARTES.md` (points 1 et 11). Captures en jeu :
`docs/art/rendus/Nuit6/Lot1/` (salles générées graine 22 : salle des gardes, palier, verger ; éclairage du jeu, lanterne du joueur ; `*_loin`
et `*_pres`, plus `*_00_avant` : la salle vide au même endroit).

### Poser un ensemble (pour main)

- **Adossé** : le plan z = 0 est le mur, le pivot est au sol au milieu du mur, l'ensemble s'étend vers **+Z** (vers la salle). Poser le
  pivot à 2 cm du mur. **Îlot** : le pivot est au centre. Jamais d'échelle non uniforme ; rotation libre autour de Y.
- **Pas de mur ni de sol** dans les ensembles : ils se posent dans n'importe quelle salle de l'acte. Exception voulue : les murets de
  séparation des cellules.
- **Racine** : `StalkerPassesThrough` (le traqueur enjambe). Enfants : `Mobilier` (pièces avec collision), `Details` (petits objets sans
  collision, masqués de loin par un `LODGroup`), `Lumieres` (points sans ombre), `Collisions` (boîtes en plus), `Reperes`.
- **Repères** : `Accroche_*` = bord ou dessus où le joueur se hisse ou se tracte ; `Pose_Attrapable_*` = où poser un objet attrapable de main
  (les meubles des ensembles ne sont pas attrapables) ; `Place_Coffre` = place d'un coffre ou d'un marchand.
- **Emprise à réserver** : colonne « emprise » ; garder les allées de 10 m devant (`ActPlan.PlanInterior`). Retirer les props de main qui
  tombent dans l'emprise (torches, caisses, lustres).
- **Lumières** : 3 à 6 points par ensemble, portée 5 à 14 m, aucune ombre : une flamme motivée, une clé chaude devant, un appoint froid. Ne pas
  ajouter de torche à moins de 6 m.
- **Densité conseillée** : un ensemble adossé tous les 12 à 16 m de mur et un îlot par 250 m² de sol libre ; jamais deux fois la même variante
  dans une salle.
""")
o.append("### Inventaire : repris et nouveau\n")
repris = sorted({p for f in fiches for p in f["pieces"] if not p.startswith("Ens_")})
kits = [p for p in repris if not p.startswith("P_Prop") and not p.startswith("Decal")]
o.append(f"""**Inventaire fait avant l'assemblage** : les 11 salles d'exemple (`Assets/Art/Levels/Exemples/`), les 3 kits, les props 3D et les décalques.

- **Salles découpées** : chaque ensemble reprend la composition d'une salle d'exemple, sans ses murs (colonne « repris de » des fiches) :
  Cave_Cachots (cellules, chaînes), Cave_SalleDeGarde (poste, réserve), Cave_CavernePuits (puits, îlot, éboulis), Chateau_SalleBibliotheque,
  Chateau_GrandHall (tables, portraits), Chateau_Chapelle, Chateau_SalleDeBal (salon), Jardin_Labyrinthe (haies, kiosque),
  Jardin_AlleeDesStatues (fontaine, serre, cimetière, statues). Cave_Citerne et Arene_Boss n'ont pas de coin réutilisable (eau, arène).
- **Pièces de kit reprises telles quelles ({len(kits)})** : {", ".join("`" + k + "`" for k in kits)}.
- **Props repris** : {", ".join("`" + k + "`" for k in repris if k.startswith("P_Prop"))} (dont 2 nouveaux, plus bas).
- **Décalques repris** : {", ".join("`" + k + "`" for k in repris if k.startswith("Decal"))}.
- **Pièces de kit gardées et habillées** : la haie, le kiosque et la serre reçoivent un feuillage peint par-dessus (`Ens_Habillage_*`), au même
  pivot ; le puits reçoit un plan d'eau (`Ens_Eau_Disque`). Main peut poser ces habillages sur les pièces déjà en place.
- **Doublons supprimés de ce lot** (la pièce existante suffisait) : `Candelabre_Pied`, `Bougies_A`, `Seau`, `Caisse_A`, `Caisse_B`, `Tabouret`,
  `Grille_2m`, `Grille_Vantail`, `Mur_Cellule`, `Linteau_Cellule`, `Paillasse`, `Brasero`, `Torche`, `Puits`, `Rideau_Double`, `Applique_Bougies`,
  `Lambris_3m`.
- **Nouvelle version gardée parce que meilleure** (l'ancienne reste dans le kit, encore posée par les cartes ; main peut la remplacer) :
  `Ens_Chaines_Murales` (maillons réels) pour `Cave_Chaines_Mur` ; `Ens_Sacs_A/B` pour `Cave_Sacs` ; `Ens_Eboulis_A/B` pour
  `Cave_Eboulis_Grand` ; `Ens_Bougies_B` pour `Cave_Chandelles` (flamme trop grande) ; `Ens_Bibliotheque` (livres en relief) pour
  `Chateau_Bibliotheque` ; `Ens_Table_Longue` pour `Chateau_TableBanquet` ; `Ens_Lit_Baldaquin` pour `Chateau_LitBaldaquin` ;
  `Ens_Banc_Chapelle` pour `Chateau_Banc_Eglise` ; `Ens_Autel_Chapelle` pour `Chateau_Autel` ; `Ens_Pupitre` pour `Chateau_Lutrin` ;
  `Ens_Portrait_*` pour `Chateau_Tableau` (toile vide) ; `P_Prop_Fauteuil` pour `Chateau_Fauteuil` ; `P_Prop_AngePleureur` pour
  `Jardin_Statue_Pleureuse`.
""")
for cle, titre in ACTES:
    o.append(f"### {titre} : ensembles\n")
    for f in [f for f in fiches if f["acte"] == cle]:
        m = mes.get(f["nom"])
        n = f["nom"]
        o.append(f"#### `{n}` ({'adossé' if f['adosse'] else 'îlot'})\n")
        o.append(f"![{n}](rendus/Nuit6/Lot1/{n}_loin.jpg)\n")
        o.append(f"- **Emprise** : {f['emprise']} ; **hauteur** : {f['haut']}. Mesuré (feuillage et débords compris) : {m[0]} × {m[1]} × {m[2]} m.")
        o.append(f"- **Fonction** : {f['fonction']}. **Récit** : {f['recit']}")
        o.append(f"- **Accroches** : {f['accroches'] or 'aucune'}")
        o.append(f"- **Passage** : {f['passage']}")
        o.append(f"- **Repris de** : {f['reprend']}")
        o.append(f"- **Coût** : {int(m[3]):,} triangles, {m[4]} lumières, {m[5]} collisions, {m[6]} rendus.\n".replace(",", " ", 1))
o.append("### Nouvelles pièces (`Assets/Art/Models/Ensembles/SM_Ens_*.fbx`, prefabs `Assets/Art/Prefabs/Ensembles/Ens_*.prefab`)\n")
o.append("Modélisées par script dans Blender (`Assets/Art/Source~/Ensembles/scripts/`), pivot au sol au centre (pièces murales : au mur). Cotes Unity x × z × h.\n")
o.append("| Pièce | x × z × h (m) | Triangles | Collisions | Description |\n|---|---|---|---|---|")
for n in sorted(pieces):
    p = pieces[n]
    d = doc.get(n) or (f"Portrait peint {n[-1]} dans son cadre" if n.startswith("Portrait_") else n)
    o.append(f"| `Ens_{n}` | {p['x']} × {p['z']} × {p['h']} | {p['tris']} | {p['col']} | {d} |")
o.append("""
**Props Tripo ajoutés** : `P_Prop_AngePleureur` (1.42 × 1.30 × 2.40 m, 6 467 / 3 233 triangles, texture 2048, une boîte) et `P_Prop_Fauteuil`
(0.98 × 0.91 × 1.75 m, 4 637 / 2 317 triangles, texture 1024, une boîte), même chaîne que la nuit 4 (`nettoyer_prop.py`, `EnsembleBuilder.Props`).

**Outils** (`Assets/Art/Source~/Ensembles/scripts/`) : `ens_lib.py`, `pieces_*.py`, `construit.py` (Blender) ; `ens_textures.py` ; `ensembles.py` (plans) ;
`EnsembleBuilder.cs` (matériaux, import, prefabs, banc) ; `AuditEnsembles.cs` ; `CapturesEnsembles.cs` ; `ens_doc.py`.
<!-- ENSEMBLES-FIN -->""")
p = os.path.join(R, "docs", "art", "INTEGRATION-PROPS.md")
s = open(p, encoding="utf-8").read()
s = re.sub(r"\n*<!-- ENSEMBLES-DEBUT.*?ENSEMBLES-FIN -->\n*", "\n", s, flags=re.S).rstrip() + "\n\n" + "\n".join(o) + "\n"
open(p, "w", encoding="utf-8", newline="\n").write(s)

# manifeste
p = os.path.join(R, "docs", "assets", "MANIFEST.md")
s = open(p, encoding="utf-8").read()
s = "\n".join(l for l in s.splitlines() if "nuit 6, lot 1" not in l).rstrip() + "\n"
L = []
DATE = "2026-11-25"
AC = "À confirmer par l'utilisateur (forfait Higgsfield plus ; conditions Higgsfield et Tripo)"
for n in sorted(pieces):
    q = pieces[n]
    ia = "oui (texture peinte générée, voir sa ligne)" if any(k in "".join(q["mats"]) for k in ("Feuillage", "Portraits", "Vitrail", "Tapisserie")) or q["feuillage"] else "non"
    L.append(f"| `SM_Ens_{n}` (nuit 6, lot 1) | `Assets/Art/Models/Ensembles/SM_Ens_{n}.fbx` (prefab `Assets/Art/Prefabs/Ensembles/Ens_{n}.prefab`) | Modélisé par script dans Blender 5.0 | {ia} | Projet | Sans objet | Oui ({DATE}) : {q['x']} × {q['z']} × {q['h']} m | sol, centre (pièces murales : au mur) | {q['tris']} | À valider DA |")
for n, d, t in (("AngePleureur", "1.42 × 1.30 × 2.40 m", "6467 / 3233 (LOD1)"), ("Fauteuil", "0.98 × 0.91 × 1.75 m", "4637 / 2317 (LOD1)")):
    job = {"AngePleureur": "image 6928c0c0-631b-4c4a-8a69-bbb4fd1a215e, modèle 9c6bcfd7-0456-4c6c-8233-f24b63630b68", "Fauteuil": "image 307928ce-ac29-41ba-84ef-0d71e99f3fe7, modèle 5050bf3f-fbec-4d12-9ee5-341f9c972856"}[n]
    L.append(f"| `P_Prop_{n}` : `SM_Prop_{n}`, `M_Prop_{n}`, `T_Prop_{n}_BaseColor` (nuit 6, lot 1) | `Assets/Art/Models/Props/SM_Prop_{n}.fbx` | Higgsfield : Nano Banana (2 crédits) → Tripo H3.1 image vers 3D (9 crédits) → Blender (`nettoyer_prop.py`) ; jobs {job} | oui | Contenu généré, conditions Higgsfield / Tripo | {AC} | Oui ({DATE}) : {d} | sol, centre | {t} | À valider DA |")
for n, job, c, d in (("Feuillage", "ea26693b-160c-4a2a-8390-0cae2774accf", 3, "atlas 3 × 3 de feuillages peints, clé magenta retirée"), ("Portraits", "ea5a745a-1d08-44a5-a10f-3bb2dedbac66", 3, "8 portraits de personnages imaginaires, atlas 4 × 2"),
                     ("Vitrail", "efde9624-225e-4707-a5f3-64630f8d7d2f", 2, "vitrail de la sainte à la lanterne"), ("Tapisserie", "251e802a-dea5-4ddd-abc1-573009f6c1ba", 2, "tapisserie de la chasse")):
    L.append(f"| `T_Ens_{n}_BaseColor` ({d} ; nuit 6, lot 1) | `Assets/Art/Textures/Ensembles/T_Ens_{n}_BaseColor.png` (source `docs/art/ensembles/sources/gen_{n.lower()}.png`) | Higgsfield, Nano Banana, {c} crédits, job {job} ; découpé par `ens_textures.py` | oui | Contenu généré, conditions Higgsfield | {AC} | Sans objet | — | — | À valider DA |")
L.append("| `T_Ens_Bois`, `_Tissu`, `_Pierre`, `_Metal`, `_Papier`, `_Livres` `_BaseColor` et les matériaux `M_Ens_*` (nuit 6, lot 1) | `Assets/Art/Textures/Ensembles/`, `Assets/Art/Materials/Ensembles/` | Textures calculées par script (`ens_textures.py`), matériaux créés par `EnsembleBuilder.cs` | non | Projet | Sans objet | — | — | — | À valider DA |")
for f in fiches:
    m = mes[f["nom"]]
    L.append(f"| `{f['nom']}` (ensemble de décor ; nuit 6, lot 1) | `Assets/Art/Levels/Ensembles/{f['acte']}/{f['nom']}.prefab` | Assemblé par `EnsembleBuilder.cs` d'après `ensembles.py` | voir les pièces | Projet | Sans objet | Oui ({DATE}) : {m[0]} × {m[1]} × {m[2]} m | {'sol, milieu du mur' if f['adosse'] else 'sol, centre'} | {m[3]} | À valider DA |")
open(p, "w", encoding="utf-8", newline="\n").write(s + "\n".join(L) + "\n")
print(len(fiches), "fiches,", len(pieces), "pièces,", len(L), "lignes de manifeste")
