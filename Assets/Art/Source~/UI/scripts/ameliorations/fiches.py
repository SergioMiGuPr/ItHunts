# -*- coding: utf-8 -*-
"""Fiches des 70 améliorations tirées de docs/design/AMELIORATIONS.md (nuit 5, lot 2) pour les aperçus de la DA (infobulle,
codex, cartes) : id, nom, effet, 5 rangs, synergies. Le gras (**…**) devient <b>…</b> (texte riche Unity).
Texte vu par le joueur (relecture du lot 2) : virgule décimale, pas d'anglicisme, pas de note de développement.
Sortie : fiches.tsv (une ligne par amélioration, champs séparés par des tabulations), lue par UIJeu5Builder."""
import os, re
ICI = os.path.dirname(os.path.abspath(__file__))
RACINE = os.path.abspath(os.path.join(ICI, "..", "..", "..", "..", "..", ".."))
SRC = os.path.join(RACINE, "docs", "design", "AMELIORATIONS.md")
ID = re.compile(r"^(G\d+|[RCKH]\d+|[RCKH]-P\d)$")

REMPLACE = [
    (r"\s*\(pas en coop en ligne : effet local\)", ""),
    (r"plafond 18 inchangé", "plafond de 18 m/s inchangé"),
    (r"\bun kill\b", "un ennemi tué"), (r"\bkills\b", "ennemis tués"), (r"\bkill\b", "ennemi tué"),
    (r"\bheadshots?\b", "tir à la tête"), (r"\bwall slide\b", "glissade murale"), (r"\bwall jumps?\b", "saut mural"),
    (r"\bslide\b", "glissade"), (r"\bdash\b", "ruée"), (r"\bbunny hop\b", "saut enchaîné"),
]


def propre(t):
    t = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", t.strip())
    t = re.sub(r"(\d)\.(\d)", r"\1,\2", t)
    for a, b in REMPLACE:
        t = re.sub(a, b, t)
    return t.replace("\t", " ")


lignes = []
for l in open(SRC, encoding="utf-8"):
    if not l.startswith("|"):
        continue
    c = [x.strip() for x in l.strip().strip("|").split("|")]
    if not c or not ID.match(c[0]):
        continue
    nom = propre(c[1].replace("**", ""))
    rangs = c[3:8]
    syn = c[8] if len(c) > 8 else ""
    eff = propre(c[2])
    eff = eff[0].upper() + eff[1:] if eff else eff
    lignes.append("\t".join([c[0], nom, eff] + [propre(r) for r in rangs] + [propre(syn)]))
open(os.path.join(ICI, "fiches.tsv"), "w", encoding="utf-8").write("\n".join(lignes) + "\n")
print(len(lignes), "fiches")
