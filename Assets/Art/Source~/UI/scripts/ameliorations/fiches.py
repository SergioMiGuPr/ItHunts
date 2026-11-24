# -*- coding: utf-8 -*-
"""Fiches des 70 améliorations tirées de docs/design/AMELIORATIONS.md (nuit 5, lot 2) pour les aperçus de la DA (infobulle,
codex, cartes) : id, nom, effet, 5 rangs, synergies. Le gras (**…**) devient <b>…</b> (texte riche Unity).
Sortie : fiches.tsv (une ligne par amélioration, champs séparés par des tabulations), lue par UIJeu5Builder."""
import os, re
ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.abspath(os.path.join(ICI, "..", "..", "..", "..", "..", ".."))
SRC = os.path.join(RACINE, "docs", "design", "AMELIORATIONS.md")
ID = re.compile(r"^(G\d+|[RCKH]\d+|[RCKH]-P\d)$")

def propre(t):
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t.strip())
    return t.replace("\t", " ")

lignes = []
for l in open(SRC, encoding="utf-8"):
    if not l.startswith("|"): continue
    c = [x.strip() for x in l.strip().strip("|").split("|")]
    if not c or not ID.match(c[0]): continue
    nom = c[1].replace("**", "")
    rangs = c[3:8]
    syn = c[8] if len(c) > 8 else ""
    eff = c[2]
    eff = eff[0].upper() + eff[1:] if eff else eff
    lignes.append("\t".join([c[0], nom, propre(eff)] + [propre(r) for r in rangs] + [propre(syn)]))
open(os.path.join(ICI, "fiches.tsv"), "w", encoding="utf-8").write("\n".join(lignes) + "\n")
print(len(lignes), "fiches")
