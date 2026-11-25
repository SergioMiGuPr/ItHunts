"""Outil de dessin des cartes faites à la main (D-110).

Une carte se décrit en Python (rectangles de chaque salle, en caractères du plan, x vers la droite, y vers le bas), puis cet outil
écrit le fichier texte lu par le jeu (Assets/_Project/Settings/Cartes/*.txt) et vérifie : surface, contact réel de chaque porte,
nombre de sorties par salle, accessibilité depuis le départ, différences de hauteur des portes.

Usage : python Tools/Cartes/<carte>.py  (chaque carte appelle ecrire()).
"""
import os
from collections import deque

RACINE = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))


class Salle:
    def __init__(self, id, nom, rects, **opts):
        self.id, self.nom, self.rects, self.opts = id, nom, rects, opts


def peindre(largeur, hauteur, salles):
    g = [["." for _ in range(largeur)] for _ in range(hauteur)]
    for s in salles:
        for (x, y, w, h) in s.rects:
            for yy in range(y, y + h):
                for xx in range(x, x + w):
                    if 0 <= xx < largeur and 0 <= yy < hauteur:
                        g[yy][xx] = s.id
    for (x, y, w, h) in getattr(peindre, "trous", []):
        for yy in range(y, y + h):
            for xx in range(x, x + w):
                g[yy][xx] = "."
    return g


def contact(g, a, b):
    """Longueur (en caractères) de la plus longue limite droite commune entre a et b."""
    best = 0
    H, W = len(g), len(g[0])
    for (dx, dy) in ((1, 0), (0, 1)):
        runs = {}
        for y in range(H):
            for x in range(W):
                x2, y2 = x + dx, y + dy
                if x2 >= W or y2 >= H:
                    continue
                p = {g[y][x], g[y2][x2]}
                if p == {a, b}:
                    key = (dx, x if dx else y)
                    runs.setdefault(key, []).append(y if dx else x)
        for pos in runs.values():
            pos.sort()
            n = 1
            for i in range(1, len(pos) + 1):
                if i == len(pos) or pos[i] != pos[i - 1] + 1:
                    best = max(best, n)
                    n = 1
                else:
                    n += 1
    return best


def ecrire(fichier, nom, acte, largeur, hauteur, salles, portes, structures=(), ancres=(), echelle=8, trous=(), organique=0):
    peindre.trous = list(trous)
    g = peindre(largeur, hauteur, salles)
    par_id = {s.id: s for s in salles}
    erreurs = []
    # Portes : contact, hauteurs.
    sorties = {s.id: 0 for s in salles}
    voisins = {s.id: set() for s in salles}
    for p in portes:
        ab, mots = p.split()[0], p.split()[1:]
        a, b = ab.split("-")
        n = contact(g, a, b)
        if n < 2:
            erreurs.append(f"porte {ab} : contact de {n} caractère(s)")
        fa, fb = par_id[a].opts.get("sol", 0), par_id[b].opts.get("sol", 0)
        dz = abs(fa - fb)
        if "haute" not in mots and dz > 0.01 and n * echelle < 16:
            erreurs.append(f"porte {ab} : dénivelé {dz} m sur un contact de {n * echelle} m")
        sorties[a] += 1
        sorties[b] += 1
        voisins[a].add(b)
        voisins[b].add(a)
    speciaux = {"hall", "arrivee", "verrouillee"}
    for s in salles:
        role = s.opts.get("role", "")
        if role not in speciaux and sorties[s.id] < 2:
            erreurs.append(f"salle {s.id} ({s.nom}) : {sorties[s.id]} sortie(s)")
    depart = next(s.id for s in salles if s.opts.get("role") == "depart")
    vu, q = {depart}, deque([depart])
    while q:
        c = q.popleft()
        for v in voisins[c]:
            if v not in vu:
                vu.add(v)
                q.append(v)
    for s in salles:
        if s.id not in vu:
            erreurs.append(f"salle {s.id} ({s.nom}) inaccessible")
    cases = sum(1 for row in g for c in row if c != ".")
    surface = cases * echelle * echelle

    lignes = [f"# Carte faite à la main (D-110), écrite par Tools/Cartes. Plan : 1 caractère = {echelle} m, nord en haut.",
              f"nom: {nom}", f"acte: {acte}", f"echelle: {echelle}", f"organique: {organique}", "", "[plan]"]
    lignes += ["|" + "".join(row) + "|" for row in g]
    lignes += ["", "[salles]"]
    for s in salles:
        o = s.opts
        parts = [f"{s.id} : {s.nom}", f"sol {o.get('sol', 0)}"]
        if "plafond" in o:
            parts.append(f"plafond {o['plafond']}")
        if o.get("ciel"):
            parts.append("ciel")
        if "role" in o:
            parts.append(o["role"])
        if "trait" in o:
            parts.append(o["trait"])
        if "galerie" in o:
            parts.append("galerie " + o["galerie"])
        if "fonction" in o:
            parts.append("fonction " + o["fonction"])
        lignes.append(" ; ".join(parts))
    lignes += ["", "[portes]"] + list(portes)
    if structures:
        lignes += ["", "[structures]"] + list(structures)
    lignes += ["", "[ancres]"] + list(ancres)
    chemin = os.path.join(RACINE, "Assets", "_Project", "Settings", "Cartes", fichier)
    os.makedirs(os.path.dirname(chemin), exist_ok=True)
    with open(chemin, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(lignes) + "\n")
    print(f"{fichier} : {len(salles)} salles, {len(portes)} portes, {surface:,} m² ({largeur * echelle} × {hauteur * echelle} m)".replace(",", " "))
    for e in erreurs:
        print("  ERREUR", e)
    return erreurs
