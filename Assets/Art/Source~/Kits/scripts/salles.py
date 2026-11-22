# -*- coding: utf-8 -*-
"""Plans des salles d'exemple (lot 5, nuit du 2026-11-22) : écrit un fichier de plans par acte lu par SalleBuilder.cs.

Coordonnées Unity (m) : +Y en haut ; les pièces des kits regardent vers +Z (rotation 0). Un mur posé à la limite d'une
salle se tourne vers l'intérieur : côté sud (z min) rot 0, côté nord (z max) rot 180, côté ouest (x min) rot 90,
côté est (x max) rot -90.

    python salles.py           -> Assets/Art/Source~/Kits/plans/<acte>.txt
Chaque salle a aussi sa fiche (entrées, sorties, routes de parcours, prises) dans FICHES, reprise dans
docs/art/INTEGRATION-PROPS.md par salles_doc.py.
"""
import os, math, random
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "plans")
FICHES = {}


class Plan:
    def __init__(self, nom, acte):
        self.nom, self.acte, self.l = nom, acte, [f"N|{nom}|{acte}"]
        self.r = random.Random(hash(nom) & 0xffff)

    def p(self, pf, x, y, z, rot=0, s=None):
        self.l.append(f"P|{pf}|{x:.3f}|{y:.3f}|{z:.3f}|{rot:.1f}" + (f"|{s}" if s is not None else ""))

    def bloc(self, mat, x, y, z, sx, sy, sz, rot=0):
        self.l.append(f"S|{mat}|{x:.3f}|{y:.3f}|{z:.3f}|{sx:.3f}|{sy:.3f}|{sz:.3f}|{rot:.1f}")

    def lum(self, x, y, z, c=(1, 0.6, 0.32), i=1.35, r=15.4, spot=None):
        if spot:
            rx, ry, ang = spot
            self.l.append(f"L|Spot|{x}|{y}|{z}|{c[0]}|{c[1]}|{c[2]}|{i}|{r}|{rx}|{ry}|{ang}")
        else:
            self.l.append(f"L|Point|{x}|{y}|{z}|{c[0]}|{c[1]}|{c[2]}|{i}|{r}")

    def vue(self, nom, pos, cible):
        self.l.append(f"V|{nom}|{pos[0]}|{pos[1]}|{pos[2]}|{cible[0]}|{cible[1]}|{cible[2]}")

    def murs(self, pf, x0, x1, z, rot, larg=8.0, sauf=()):
        """Rangée de murs de largeur `larg` le long de x (rot 0 ou 180) ; `sauf` : indices remplacés par autre chose."""
        n = int(round((x1 - x0) / larg))
        for i in range(n):
            if i in sauf:
                continue
            self.p(pf, x0 + (i + 0.5) * larg, 0, z, rot)

    def murs_z(self, pf, z0, z1, x, rot, larg=8.0, sauf=()):
        n = int(round((z1 - z0) / larg))
        for i in range(n):
            if i in sauf:
                continue
            self.p(pf, x, 0, z0 + (i + 0.5) * larg, rot)

    def face_centre(self, pf, x, z, cx, cz, y=0, s=None, decal=0.0):
        """Pose une pièce tournée vers (cx, cz) (sa face +Z regarde le centre)."""
        rot = math.degrees(math.atan2(cx - x, cz - z)) + decal
        self.p(pf, x, y, z, rot, s)

    def anneau(self, pfs, cx, cz, rx, rz, n, y=0, a0=0.0, sauf=()):
        """Paroi organique : n pièces sur une ellipse, tournées vers l'intérieur ; `pfs` est parcouru en boucle."""
        for i in range(n):
            if i in sauf:
                continue
            a = a0 + 2 * math.pi * i / n
            x, z = cx + math.cos(a) * rx, cz + math.sin(a) * rz
            self.face_centre(pfs[i % len(pfs)], x, z, cx, cz, y, decal=self.r.uniform(-8, 8))

    def texte(self):
        return "\n".join(self.l)


# =================================================================== acte II : château
def grand_hall():
    """Grand hall : nef de 32 × 24 m sous voûtes d'ogives à 19 m, hautes fenêtres au nord d'où tombent des rayons de lune,
    galerie à 8 m à l'est atteinte par le grand escalier, chandeliers."""
    P = Plan("Chateau_GrandHall", "Chateau")
    W, D = 16.0, 12.0
    P.bloc("M_Bib_Marbre", 0, -0.5, 0, 2 * W, 1, 2 * D)
    P.bloc("M_Bib_Tapis", 0, 0.02, 0, 4, 0.04, 2 * D - 4)
    # murs : sud plein (porte d'entrée au centre), nord percé de 3 hautes fenêtres, ouest plein, est (galerie)
    P.murs("Chateau_Mur_8x20", -W, W, -D, 0, sauf=(0,))
    P.p("Chateau_Arcade_8x12", -12, 0, -D, 0)
    for i in range(4):
        x = -W + 4 + i * 8
        P.p("Chateau_Fenetre_Haute" if i in (0, 1, 3) else "Chateau_Mur_8x20", x, 0, D, 180)
    P.murs_z("Chateau_Mur_8x20", -D, D, -W, 90)
    P.murs_z("Chateau_Mur_8x20", -D, D, W, -90)
    # piliers de la nef (z = ±5) et voûtes (tailloirs à 19.2 m)
    for x in (-12, -4, 4, 12):
        for z in (-5, 5):
            P.p("Chateau_Pilier_20m", x, 0, z)
    for x in (-8, 0, 8):
        P.p("Chateau_Voute_8x8", x, 19.2, 0, 0, "1,1,1.25")
    # rayons de lune sous les fenêtres nord (fenêtre de 4 à 15 m : origine au milieu, rayon vers le sud)
    for x in (-12, -4, 12):
        P.p("Chateau_RayonLune", x, 12.5, D - 0.8, 180)
    # galerie haute à l'est (dessus à 8 m), grand escalier contre le mur ouest du fond qui y mène par le nord
    for z in (-8, 0, 8):
        P.p("Chateau_Galerie_8m", W, 8, z, -90)
    P.p("Chateau_GrandEscalier", -8, 0, -8.8, -90, "0.8,1,1")   # bas-côté sud, monte vers l'est jusqu'à la galerie
    # chandeliers suspendus (pivot au plafond de la voûte, couronne à 12 m)
    for x in (-8, 8):
        P.p("Chateau_Chandelier", x, 19.0, 0)
    # tentures entre les fenêtres, tableaux sombres côté ouest, meubles renversés (couverts)
    for z in (-8, 0, 8):
        P.p("Chateau_Tenture", -W + 0.3, 13, z, 90)
        P.p("Chateau_Tableau", -W + 0.2, 6, z + 3.5, 90)
    P.p("Chateau_Table_Renversee", -6, 0, -2, 25)
    P.p("Chateau_Chaise_Renversee", -4.5, 0, 1, 70)
    P.p("Chateau_Chaise_Renversee", 3, 0, -3.5, 200)
    P.p("Chateau_TableBanquet", 0, 0, 7.5, 0)
    P.p("Chateau_Armoire_Tombee", 9, 0, 6, 160)
    for x in (-3, 3):
        P.p("Cave_Candelabre", x, 0, -9)
    P.vue("Entree", (-12, 1.7, -10.5), (2, 7, 8))
    P.vue("Galerie", (15, 9.7, 9), (-8, 3, -4))
    P.vue("Rayons", (-2, 1.7, -2), (-10, 9, 10))
    FICHES[P.nom] = dict(acte="Château", taille="32 × 24 m, voûtes à 19–24 m",
                         identite="Grand hall de réception abandonné : nef à piliers, trois rayons de lune qui tombent des hautes fenêtres nord sur le marbre, tables renversées d'un banquet interrompu.",
                         fonction="Salle d'arène de l'acte II (vagues) et hub vers les galeries.",
                         entrees="Arcade sud-ouest (6 × 10 m, x = -12) ; galerie est à 8 m (sortie haute vers les étages).",
                         route="Course sur le tapis central → saut sur la table renversée → traction sur un pilier (accroches à 12 m) → galerie est à 8 m ; ou grand escalier du bas-côté sud (monte vers l'est jusqu'à la galerie). Bunny hop dans la nef de 32 m.",
                         prises="Piliers (`Accroche_Traction` à 12 m), balustrade de la galerie, chandeliers (lustres que le jeu peut faire tomber).",
                         tension="Les rayons de lune sont les seuls endroits éclairés : on y voit le traqueur entrer par l'arcade avant qu'il ne replonge dans le noir des bas-côtés.")
    return P


def chateau_bibliotheque():
    """Bibliothèque à deux niveaux : salle de 24 × 16 m, 12 m de haut, rayonnages muraux continus, galerie de lecture
    à 6 m sur trois côtés, échelles de fait (rayonnages = murs de wall jump), lustre."""
    P = Plan("Chateau_SalleBibliotheque", "Chateau")
    W, D = 12.0, 8.0
    P.bloc("M_Bib_BoisNeuf", 0, -0.5, 0, 2 * W, 1, 2 * D)
    P.bloc("M_Bib_Tapis", 0, 0.02, 0, 10, 0.04, 8)
    P.bloc("M_Bib_PlatreFissure_Sale", 0, 12.5, 0, 2 * W, 1, 2 * D)
    P.murs("Chateau_Mur_Platre_8x12", -W, W, -D, 0, sauf=(1,))
    P.p("Chateau_Arcade_8x12", 0, 0, -D, 0, "0.7,0.85,1")
    P.murs("Chateau_Mur_Platre_8x12", -W, W, D, 180, sauf=(1,))
    P.p("Chateau_Fenetre_Haute", 0, 0, D, 180, "1,0.6,1")
    P.murs_z("Chateau_Mur_Platre_8x12", -D, D, -W, 90)
    P.murs_z("Chateau_Mur_Platre_8x12", -D, D, W, -90)
    # rayonnages au rez-de-chaussée (dos au mur) et au niveau de la galerie
    for x in (-10, -6, 6, 10):
        P.p("Chateau_Bibliotheque", x, 0, D - 0.1, 180)
        P.p("Chateau_Bibliotheque", x, 0, -D + 0.1, 0)
        P.p("Chateau_Bibliotheque", x, 6.0, D - 0.1, 180)
    for z in (-4, 0, 4):
        P.p("Chateau_Bibliotheque", -W + 0.1, 0, z, 90)
        P.p("Chateau_Bibliotheque", W - 0.1, 0, z, -90)
        P.p("Chateau_Bibliotheque", -W + 0.1, 6.0, z, 90)
        P.p("Chateau_Bibliotheque", W - 0.1, 6.0, z, -90)
    # galerie de lecture à 6 m (ouest, est, nord) : planchers sur consoles
    for z in (-4, 4):
        P.p("Chateau_Galerie_8m", -W + 0.6, 6, z, 90)
        P.p("Chateau_Galerie_8m", W - 0.6, 6, z, -90)
    P.p("Chateau_Galerie_8m", -4, 6, D - 0.6, 180)
    P.p("Chateau_Galerie_8m", 4, 6, D - 0.6, 180)
    # mobilier de lecture renversé, lutrin, fauteuils
    P.p("Chateau_TableBanquet", 0, 0, -1, 90, "0.6,1,1")
    P.p("Chateau_Chaise_Renversee", -2, 0, 1.5, 40)
    P.p("Chateau_Fauteuil", 4, 0, 3, -140)
    P.p("Chateau_Fauteuil", -5, 0, -4, 30)
    P.p("Chateau_Lutrin", 2.5, 0, -4, 10)
    P.p("Chateau_Armoire_Tombee", 6, 0, -3, 75)
    P.p("Chateau_Chandelier", 0, 12, 0, 0, "0.7")
    P.p("Chateau_RayonLune", 0, 6.5, D - 0.8, 180, "0.6")
    for x in (-8, 8):
        P.p("Cave_Candelabre", x, 0, 0)
    P.vue("Entree", (0, 1.7, -6), (0, 5, 8))
    P.vue("Galerie", (-10.5, 7.7, 5), (8, 3, -6))
    FICHES[P.nom] = dict(acte="Château", taille="24 × 16 m, 12 m de haut, galerie à 6 m",
                         identite="Bibliothèque silencieuse : rayonnages du sol au plafond, galerie de lecture, livres jetés au sol, un lutrin encore ouvert.",
                         fonction="Salle de transition verticale : on y monte vers les étages ; petite vague dans les allées.",
                         entrees="Porte basse sud (arcade réduite, 4 × 8.5 m) ; sortie haute par la galerie nord sous la fenêtre (6 m).",
                         route="Wall jump entre deux rayonnages qui se font face (coins nord-est et nord-ouest, 5.5 m d'écart) jusqu'à la galerie ; course sur la galerie en U (48 m) ; descente en glissade sur la table.",
                         prises="Lustre (centre, 12 m), balustrades de la galerie.",
                         tension="Allées étroites entre les rayonnages : on n'y voit pas venir ce qui tourne le coin ; le lustre grince au-dessus de la seule zone éclairée.")
    return P


def chateau_chapelle():
    """Chapelle : nef de 14 × 30 m, rosace au fond, bancs en rangées (couverts bas), autel sur estrade, colonnes,
    rayons de lune par les fenêtres hautes ; la sortie est derrière l'autel."""
    P = Plan("Chateau_Chapelle", "Chateau")
    W, D = 7.0, 15.0
    P.bloc("M_Bib_Marbre", 0, -0.5, 0, 2 * W + 8, 1, 2 * D)
    P.p("Chateau_Arcade_8x12", 0, 0, -D, 0)
    for x in (-8, 8):
        P.p("Chateau_Mur_8x20", x, 0, -D, 0)
    P.p("Chateau_Mur_8x20", -4, 0, D, 180)
    P.p("Chateau_Mur_8x20", 4, 0, D, 180)
    P.p("Chateau_Rosace", 0, 14, D - 1.2, 180)
    for i in range(4):
        z = -D + 4 + i * 8
        P.p("Chateau_Fenetre_Haute" if i % 2 else "Chateau_Mur_8x20", -W - 4, 0, z, 90)
        P.p("Chateau_Mur_8x20" if i % 2 else "Chateau_Fenetre_Haute", W + 4, 0, z, -90)
    for i in range(3):
        z = -D + 8 + i * 8
        for sx in (-1, 1):
            P.p("Chateau_Colonne_12m", sx * W, 0, z)
    for (sx, z) in ((-1, -7), (1, 1)):
        P.p("Chateau_RayonLune", sx * (W + 3.4), 12.5, z, 90 if sx < 0 else -90)
    for i in range(6):
        z = -10 + i * 2.6
        for sx in (-1, 1):
            P.p("Chateau_Banc_Eglise", sx * 3.2, 0, z, 180 if i != 3 else 165)
    P.p("Chateau_Autel", 0, 0, D - 5.5, 180)
    P.p("Chateau_Lutrin", -3, 0, D - 8, 160)
    for x in (-4.5, 4.5):
        P.p("Cave_Candelabre", x, 0, D - 6)
    P.p("Chateau_Tenture", -2.5, 11, D - 0.4, 180)
    P.p("Chateau_Tenture", 2.5, 11, D - 0.4, 180)
    P.vue("Nef", (0, 1.7, -12), (0, 7, 14))
    P.vue("Autel", (4, 2.5, 6), (-3, 5, -10))
    FICHES[P.nom] = dict(acte="Château", taille="22 × 30 m, 20 m sous les fenêtres",
                         identite="Chapelle profanée : rosace au fond, deux rayons de lune croisés sur les bancs, cierges encore allumés sur l'autel.",
                         fonction="Salle de tension avant le boss de l'acte II, ou salle à coffre (sur l'autel).",
                         entrees="Arcade ouest-sud (porte de 6 × 10 m) ; sortie derrière l'autel (à percer dans le mur nord par le jeu).",
                         route="Bunny hop dans l'allée centrale (24 m) ; bancs = couverts bas et marches de saut ; colonnes de 12 m en quinconce pour la traction ; rosace = repère visible depuis l'entrée.",
                         prises="Colonnes (sommet à 12 m), rebord de l'autel, tentures (décor).",
                         tension="Bancs alignés comme des rangées de silhouettes ; seul l'autel est éclairé, on le voit de loin et on sait qu'il faut y aller.")
    return P


def chateau_salle_de_bal():
    """Salle de bal : 36 × 24 m en damier de marbre, 20 m sous voûtes, estrade des musiciens, galerie haute, lustres,
    tables renversées autour d'une piste vide."""
    P = Plan("Chateau_SalleDeBal", "Chateau")
    W, D = 18.0, 12.0
    P.bloc("M_Bib_Marbre", 0, -0.5, 0, 2 * W, 1, 2 * D)
    P.murs("Chateau_Mur_8x20", -16, 16, -D, 0, sauf=(1,))
    P.p("Chateau_Arcade_8x12", -4, 0, -D, 0)
    for i in range(4):
        P.p("Chateau_Fenetre_Haute", -12 + i * 8, 0, D, 180)
    P.murs_z("Chateau_Mur_8x20", -D, D, -W, 90)
    P.murs_z("Chateau_Mur_8x20", -D, D, W, -90)
    for x in (-12, -4, 4, 12):
        P.p("Chateau_RayonLune", x, 12.5, D - 0.8, 180)
    for x in (-12, 0, 12):
        P.p("Chateau_Chandelier", x, 19, 0)
    P.p("Chateau_Estrade_Musiciens", 0, 0, -D + 3, 0)
    for z in (-6, 6):
        P.p("Chateau_Galerie_8m", -W + 0.6, 8, z, 90)
        P.p("Chateau_Galerie_8m", W - 0.6, 8, z, -90)
    for (x, z, r) in ((-12, -6, 10), (-13, 4, 80), (12, -5, 170), (13, 5, 260), (-6, 9, 5), (7, 9, 190)):
        P.p("Chateau_Table_Renversee", x, 0, z, r)
    for i in range(10):
        P.p("Chateau_Chaise_Renversee" if i % 3 else "Chateau_Chaise", P.r.uniform(-15, 15), 0, P.r.uniform(-9, 9), P.r.uniform(0, 360))
    for z in (-6, 0, 6):
        P.p("Chateau_Tenture", -W + 0.3, 14, z, 90)
        P.p("Chateau_Tenture", W - 0.3, 14, z, -90)
    P.p("Chateau_MiroirBrise", -16.5, 0, -10, 45)
    P.vue("Piste", (-4, 1.7, -9), (6, 6, 10))
    P.vue("Galerie", (17, 9.7, -8), (-6, 2, 6))
    FICHES[P.nom] = dict(acte="Château", taille="36 × 24 m, 20 m sous plafond, galeries à 8 m",
                         identite="Salle de bal figée au milieu de la fête : piste vide en damier sous quatre rayons de lune, tables et chaises renversées tout autour, estrade des musiciens abandonnée.",
                         fonction="Arène principale de l'acte II (vagues) : piste centrale dégagée, bords encombrés.",
                         entrees="Arcade sud (x = -4) ; galeries est et ouest à 8 m (sorties hautes).",
                         route="Bunny hop en rond sur la piste (cercle de 20 m) ; traction vers les lustres (pivot à 19 m, couronne à 12 m) puis lâcher vers une galerie ; estrade = tremplin.",
                         prises="Lustres (3), balustrades des galeries, estrade.",
                         tension="Les rayons de lune découpent la piste en bandes claires : ce qui la traverse est visible une seconde, puis disparaît.")
    return P


ACTES = {"Chateau": [grand_hall, chateau_bibliotheque, chateau_chapelle, chateau_salle_de_bal]}


# =================================================================== acte I : caves
def cave_cachots():
    """Cachots : galerie maçonnée de 40 m bordée de six cellules (grille, porte, lit, paille, seau, chaînes au mur du
    fond), voûte basse de briques, salle des gardes à un bout."""
    P = Plan("Cave_Cachots", "Caves")
    L_, Wc = 40.0, 6.0
    P.bloc("M_Bib_Dalles_Sales", 0, -0.5, 0, L_, 1, 26)
    P.bloc("M_Bib_Briques_Salpetre", 0, 9.5, 0, L_, 1, 26)
    for side in (-1, 1):
        zf = side * 4.0          # façade des cellules
        zb = side * 10.0         # mur du fond des cellules
        P.bloc("M_Bib_Moellons_Moussus", 0, 4.5, zb + side * 0.5, L_, 9, 1)
        for i in range(6):
            x = -15 + i * 6
            P.p("Cave_Cellule_Grille", x, 0, zf, 180 if side > 0 else 0)
            # murs de séparation entre cellules
            P.bloc("M_Bib_Moellons_Moussus", x - 3.1, 2.8, (zf + zb) / 2, 0.6, 5.6, 6)
            if i not in (1, 4):   # deux cellules forcées : porte arrachée
                P.p("Cave_Cellule_Porte", x - 0.7, 0, zf, 180 if side > 0 else 0)
            if (i + (side > 0)) % 2 == 0:
                P.p("Cave_Chandelles", x + 2.2, 0, zb - side * 0.5)
            # mobilier DANS la cellule : lit contre le mur du fond, paille au coin, seau, chaînes au mur du fond
            P.p("Cave_Lit", x + 1.2, 0, zb - side * 0.6, 0 if side > 0 else 180)
            P.p("Cave_Paille", x - 1.6, 0, zb - side * 1.2, P.r.uniform(0, 360))
            P.p("Cave_Seau", x - 1.7, 0, zf + side * 1.0)
            P.p("Cave_Chaines_Mur", x - 1.0, 0, zb - side * 0.05, 180 if side > 0 else 0)
        P.bloc("M_Bib_Moellons_Moussus", 18.1, 2.8, (zf + zb) / 2, 0.6, 5.6, 6)
        P.bloc("M_Bib_Moellons_Moussus", 0, 7.3, zf, L_, 3.8, 1.2)
        # torches sur les trumeaux, une sur deux, en quinconce
        for i in range(0, 6, 2):
            x = -15 + i * 6 + (3 if side > 0 else 0) - 3.1
            P.p("Cave_Torche_Murale", x, 3.2, zf - side * 0.6, 180 if side > 0 else 0)
    # salle des gardes à l'est : table, banc, brasero
    P.p("Cave_TableGarde", 15, 0, 0, 90)
    P.p("Cave_Banc", 13.6, 0, 0, 90)
    P.p("Cave_Tabouret", 16.4, 0, 0.8)
    P.p("Cave_Brasero", 18, 0, -2.5)
    P.p("Cave_Applique", -19.8, 4, 0, 90)
    P.vue("Galerie", (-18, 1.7, 0), (10, 2, 0))
    P.vue("Cellule", (-9.4, 1.7, 3.2), (-8.0, 0.6, 9.4))
    FICHES[P.nom] = dict(acte="Caves", taille="40 × 20 m, voûte à 9 m",
                         identite="Cachots du château : douze cellules alignées, deux forcées, une partie de dés interrompue à la table des gardes.",
                         fonction="Couloir de tension : goules dans les cellules ouvertes, embuscades latérales.",
                         entrees="Bout ouest de la galerie (6 m de large) ; bout est par la salle des gardes.",
                         route="Course droite de 40 m (bunny hop) ; saut sur la table des gardes ; les grilles servent de murs de wall jump (5 m) pour monter sur le linteau des cellules et courir dessus.",
                         prises="Linteaux de pierre des cellules (5.6 m), brasero (repère lumineux).",
                         tension="Les deux cellules ouvertes : on passe devant le noir de chacune sans savoir si elle est vide.")
    return P


def cave_citerne():
    """Citerne : grande caverne organique (ellipse de 50 × 40 m) traversée par un canal ; bassin, pont de pierre en arc,
    passerelle de bois au-dessus d'un gouffre, rebords et plateformes pour les wall jumps."""
    P = Plan("Cave_Citerne", "Caves")
    P.bloc("M_Bib_Terre", 0, -0.5, 0, 60, 1, 52)
    P.bloc("M_Bib_RocheCave_Humide", 0, 19, 0, 60, 1, 52)
    P.anneau(["Cave_Paroi_Droite_18m", "Cave_Paroi_Saillie_10m", "Cave_Paroi_Droite_10m", "Cave_Paroi_Droite_18m", "Cave_Paroi_Courbe_18m"],
             0, 0, 27, 22, 14, sauf=(0,))
    # canal nord-sud au centre, pont de pierre par-dessus, bassin au nord
    for z in (-16, -8, 0, 8):
        P.p("Cave_Canal_8m", 0, 0, z, 90)
    P.p("Cave_PontPierre_16m", 0, 0, -4, 0)
    P.p("Cave_Bassin", 0, 0, 15)
    # gouffre à l'est franchi par la passerelle de bois
    P.p("Cave_Gouffre_Bord_12m", 12, 0, -6, 90)
    P.p("Cave_Gouffre_Bord_12m", 22, 0, -6, -90)
    P.bloc("M_Fond_Noir", 17, -20, -6, 8, 1, 14)
    P.p("Cave_PontBois_12m", 17, 0, -6, 0)
    # rebords et plateformes (route de wall jump vers la sortie haute ouest)
    P.p("Cave_Rebord_4m", -24, 4.5, -6, 90)
    P.p("Cave_Rebord_4m", -24, 8.5, 2, 90)
    P.p("Cave_Plateforme_3m", -16, 0, -10)
    P.p("Cave_Plateforme_3m", -12, 2, 8)
    P.p("Cave_PilierRoche_18m", -8, 0, -12)
    P.p("Cave_Eboulis_Grand", -18, 0, 14, 30)
    P.p("Cave_Eboulis_Petit", 8, 0, 12)
    for a in (0.5, 1.4, 2.4, 3.3, 4.3, 5.3):
        P.face_centre("Cave_Torche_Murale", math.cos(a) * 24.5, math.sin(a) * 19.5, 0, 0, y=4)
    P.p("Cave_Brasero", -4, 0, -10)
    P.p("Cave_Brasero", 6, 0, 8)
    P.p("Cave_Chandelles", 3, 0.9, 14)
    P.vue("Entree", (-14, 1.7, -14), (8, 3, 8))
    P.vue("Pont", (0, 3, -14), (18, 2, -4))
    FICHES[P.nom] = dict(acte="Caves", taille="54 × 44 m, voûte à 19 m",
                         identite="Ancienne citerne devenue caverne : un canal noir la coupe en deux, un pont de pierre l'enjambe, une passerelle pourrie franchit un gouffre.",
                         fonction="Salle de parcours de l'acte I : le sol est coupé par l'eau et le vide, il faut choisir sa route.",
                         entrees="Brèche sud-est (paroi manquante, 12 m) ; sortie haute à l'ouest par les rebords (8.5 m).",
                         route="Pont de pierre (bunny hop par-dessus le canal) ; passerelle de bois (deux planches manquent : saut) ; plateformes de roche (dessus à 4 m et 6 m) puis rebords à 4.5 et 8.5 m contre la paroi ouest (wall jump) ; pilier de roche = traction pour couper le canal.",
                         prises="Pilier de roche (Accroche_Traction à 9 m), rebords (Accroche_Rebord), parapets du pont.",
                         tension="Le gouffre sous la passerelle : on entend l'eau tomber, la planche manquante se voit au dernier moment.")
    return P


def cave_puits():
    """Caverne du puits : salle ronde organique (Ø 40 m), le puits au centre (repère), arches basse et haute vers les
    galeries, réserve dans une alcôve, porte de sortie de l'acte au nord."""
    P = Plan("Cave_CavernePuits", "Caves")
    P.bloc("M_Bib_Dalles_Sales", 0, -0.5, 0, 52, 1, 52)
    P.bloc("M_Bib_RocheCave_Humide", 0, 18.5, 0, 52, 1, 52)
    P.anneau(["Cave_Paroi_Courbe_18m", "Cave_Paroi_Droite_18m", "Cave_Paroi_Saillie_10m", "Cave_Paroi_Droite_10m"], 0, 0, 22, 22, 12, sauf=(3, 9))
    P.p("Cave_PorteSortie", 0, 0, 22, 180)
    P.p("Cave_ArcheHaute", 0, 0, -22, 0)
    P.p("Cave_ArcheBasse", -22, 0, 0, 90)
    P.p("Cave_Puits", 0, 0, 0, 20)
    P.p("Cave_Chandelles", 1.2, 0.95, -0.6)
    P.p("Cave_Brasero", -6, 0, 6)
    # réserve dans l'alcôve est
    P.p("Cave_Caisses_Pile", 17, 0, -7, -70)
    P.p("Cave_Tonneaux_Pile", 17, 0, 6, -110)
    P.p("Cave_Etagere", 19, 0, -1, -90)
    P.p("Cave_Sacs", 15, 0, 2)
    P.p("Cave_MurBrise_4m", 12, 0, -12, -40)
    P.p("Cave_ArcheRuine", -12, 0, 12, 140)
    P.p("Cave_Corniche_Bois_6m", -18, 6, -8, 60)
    P.p("Cave_Plateforme_3m", -10, 0, -10)
    for a in range(6):
        ang = a * math.pi / 3 + 0.3
        P.face_centre("Cave_Torche_Murale", math.cos(ang) * 20.5, math.sin(ang) * 20.5, 0, 0, y=4)
    for x in (-5, 5):
        P.p("Cave_Candelabre", x, 0, 18)
    P.vue("Puits", (-12, 1.7, -14), (2, 3, 6))
    P.vue("Sortie", (6, 2.2, -4), (0, 6, 22))
    FICHES[P.nom] = dict(acte="Caves", taille="Ø 44 m, voûte à 18 m",
                         identite="Caverne ronde autour d'un vieux puits, réserve pillée dans une alcôve, et au nord la grande porte de bois qui ferme l'acte.",
                         fonction="Salle de fin de l'acte I (sortie) et hub des galeries.",
                         entrees="Arche haute sud (8 × 11 m, le traqueur passe) ; arche basse ouest (joueur seul) ; porte de sortie nord (12 × 13 m).",
                         route="Tour complet de la caverne en course (140 m) ; plateforme de roche → corniche de bois (6 m) → saut vers l'arche en ruine ; le puits est un obstacle central (on le contourne en glissade).",
                         prises="Traverse du puits (3.8 m), arche en ruine, corniche de bois.",
                         tension="La porte de sortie est en vue dès l'entrée, éclairée par deux candélabres : il faut traverser toute la caverne pour l'atteindre.")
    return P


def cave_garde_reserve():
    """Salle de garde et réserve : salle maçonnée de 24 × 18 m (murs brisés, parois de roche qui percent), râtelier,
    tables, brasero ; réserve derrière un mur à moitié effondré."""
    P = Plan("Cave_SalleDeGarde", "Caves")
    P.bloc("M_Bib_Dalles_Sales", 0, -0.5, 0, 30, 1, 24)
    P.bloc("M_Bib_Briques_Salpetre", 0, 10.5, 0, 30, 1, 24)
    for x in (-8, 0, 8):
        P.p("Cave_MurBrise_8m", x, 0, -9, 0)
    P.p("Cave_Paroi_Droite_10m", -6, 0, 10, 180)
    P.p("Cave_Paroi_Droite_10m", 6, 0, 10, 180)
    P.p("Cave_Paroi_Courbe_10m", -14, 0, 4, 135)
    P.p("Cave_Paroi_Courbe_10m", 14, 0, 4, -135)
    P.p("Cave_ArcheBasse", -12, 0, -6, 60)
    P.p("Cave_MurBrise_4m", 4, 0, 2, 0)
    P.p("Cave_TableGarde", -4, 0, -2, 10)
    P.p("Cave_Banc", -4, 0, -3.2, 10)
    P.p("Cave_Banc", -4, 0, -0.8, 190)
    P.p("Cave_Tabouret", -1.8, 0, -1.5)
    P.p("Cave_Brasero", -8, 0, 2)
    P.p("Cave_Tonneaux_Pile", 8, 0, 6, 180)
    P.p("Cave_Caisses_Pile", 11, 0, 3, -120)
    P.p("Cave_Sacs", 6, 0, 4.5)
    P.p("Cave_Etagere", 9, 0, -6, 0)
    P.p("Cave_Chandelles", 8, 0.95, 6)
    P.p("Cave_Candelabre", 11, 0, -2)
    for x in (-6, 6):
        P.p("Cave_Torche_Murale", x, 4, -8.4, 0)
    P.vue("Garde", (8, 1.7, -6), (-6, 1.5, 2))
    P.vue("Reserve", (1, 2.2, 0), (10, 1.2, 3))
    FICHES[P.nom] = dict(acte="Caves", taille="28 × 20 m, voûte à 10 m",
                         identite="Poste de garde à moitié effondré : la roche a percé la maçonnerie, le repas des gardes est resté sur la table, la réserve est derrière le mur écroulé.",
                         fonction="Salle de combat courte (goules) et salle à coffre (dans la réserve).",
                         entrees="Arche basse sud-ouest ; brèches des murs brisés au sud (2 à 4 m de haut, on saute par-dessus).",
                         route="Saut par-dessus les murs brisés (crêtes de 4 à 8 m : wall jump sur les tronçons hauts) ; table et bancs comme marches ; contournement de la réserve par le mur à moitié écroulé.",
                         prises="Crêtes des murs brisés, brasero (repère).",
                         tension="La réserve n'est visible qu'en passant le mur écroulé : coffre ou goule, on ne le sait qu'au dernier moment.")
    return P


ACTES["Caves"] = [cave_cachots, cave_garde_reserve, cave_citerne, cave_puits]


# =================================================================== acte III : jardin et arène
def jardin_allee():
    """Allée des statues : 80 × 64 m devant la grande porte du château, allée de gravier vers la fontaine, parcelles
    bordées de haies et de palissades, gros arbres morts, kiosque, serre en ruine, coin du cimetière ; limites : forêt
    à l'ouest, mur d'enceinte à l'est, falaise au nord (l'arène est en contrebas)."""
    P = Plan("Jardin_AlleeDesStatues", "Jardin")
    P.p("Jardin_Ciel", 0, 0, 0)
    P.bloc("M_Bib_Terre_Racines", 0, -0.5, 0, 90, 1, 72)
    P.bloc("M_Bib_HerbeSeche", -22, 0.01, 6, 26, 0.02, 40)
    P.bloc("M_Bib_HerbeSeche", 22, 0.01, 6, 26, 0.02, 40)
    # château au sud : la grande porte d'où l'on sort, façade de part et d'autre
    P.p("Chateau_GrandeEntree", 0, 0, -34, 180)
    for x in (-14, -22, -30, 14, 22, 30):
        P.p("Chateau_Mur_8x20", x, 0, -34, 180)
    # allée principale (sud → nord) et allée transversale à la fontaine
    for z in range(-26, 30, 8):
        P.p("Jardin_Chemin_8m", 0, 0, z, 90)
    for x in (-12, -4, 4, 12):
        P.p("Jardin_Chemin_8m", x, 0.01, 2, 0)
    P.p("Jardin_Fontaine", 0, 0, 2)
    # statues le long de l'allée (repères, points de traction)
    for z in (-20, -10, 14, 24):
        for sx in (-1, 1):
            P.p("Jardin_Statue_Pleureuse", sx * 3.2, 0, z, -90 * sx)
    # parcelles : haies le long des allées, palissades le long des bords de parcelle
    for z in (-22, -14, 10, 18):
        P.p("Jardin_Haie_6m", -8, 0, z, 90)
        P.p("Jardin_Haie_6m", 8, 0, z, 90)
    for x in (-32, -28, -24, -20, -16):
        P.p("Jardin_Palissade_4m", x, 0, -4, 0)
        P.p("Jardin_Palissade_4m", -x, 0, 8, 180)
    P.p("Jardin_Massif", -12, 0, -6)
    P.p("Jardin_Massif", 12, 0, 10)
    # gros arbres morts (wall jump d'un tronc à l'autre : 5 à 6 m d'écart)
    for (x, z, pf, r) in ((-20, -18, "Jardin_ArbreMort_Grand", 0), (-14, -22, "Jardin_ArbreMort_Tordu", 140),
                          (20, 20, "Jardin_ArbreMort_Grand", 60), (25, 15, "Jardin_ArbreMort_Penche", 200), (-24, 22, "Jardin_ArbreMort_Tordu", 30)):
        P.p(pf, x, 0, z, r)
    P.p("Jardin_Kiosque", -22, 0, 12, 30)
    P.p("Jardin_Serre_Ruine", 22, 0, -14, 90)
    # cimetière (coin nord-est) : rangées tordues de stèles et de croix, caveau, grille et portail
    rr = P.r
    for i in range(4):
        for j in range(5):
            pf = "Jardin_Tombe_Stele" if (i + j) % 3 else "Jardin_Tombe_Croix"
            P.p(pf, 18 + j * 3.2 + rr.uniform(-0.4, 0.4), 0, 24 + i * 3.5 + rr.uniform(-0.4, 0.4), 180 + rr.uniform(-12, 12))
    P.p("Jardin_Caveau", 34, 0, 30, -90)
    P.p("Jardin_Grille_4m", 15, 0, 22, 90)
    P.p("Jardin_Portail", 15, 0, 28, 90)
    # limites naturelles : forêt à l'ouest, mur d'enceinte à l'est, falaise au nord (vers l'arène en contrebas)
    for z in (-24, -4, 16):
        P.p("Jardin_Foret_Lisiere_20m", -42, 0, z, 90)
    for z in (-28, -20, -12, -4, 4, 12, 20, 28):
        P.p("Jardin_MurEnceinte_8m", 44, 0, z, -90)
    for x in (-32, -16, 16, 32):
        P.p("Jardin_Falaise_16m", x, -24, 38, 180)
    for (x, z) in ((-20, -10), (20, -10), (-20, 25), (20, 25), (0, 5)):
        P.p("Jardin_Brume", x, 0, z)
    P.p("Cave_Brasero", -6, 0, -28)
    P.p("Cave_Brasero", 6, 0, -28)
    P.vue("Porte", (0, 1.7, -27), (0, 4, 10))
    P.vue("Fontaine", (6, 2.2, -4), (-16, 6, 18))
    P.vue("Cimetiere", (10, 3, 14), (28, 2, 30))
    FICHES[P.nom] = dict(acte="Jardin", taille="80 × 64 m, ciel ouvert",
                         identite="Jardin à la française retourné à la sauvagerie : allée de statues voilées sous la lune, fontaine croupie, cimetière du domaine.",
                         fonction="Première salle de l'acte III : traversée ouverte, vagues d'ennemis dans les parcelles.",
                         entrees="Grande porte du château (sud, 9 × 13 m) ; sortie vers l'arène par la brèche de la falaise au nord (x = 0), où se raccorde l'escalier de descente de l'arène.",
                         route="Course sur l'allée de gravier (60 m, bunny hop) ; wall jump entre les deux gros arbres du sud-ouest (5 à 6 m d'écart) puis traction sur une statue ; toit du kiosque par traction ; serre traversée en glissade.",
                         prises="Statues (Accroche_Traction), arbres (Accroche_Traction à mi-tronc), toit du kiosque (Accroche_Traction).",
                         tension="La brume cache le sol et les goules qui rampent ; les statues voilées ressemblent à des silhouettes qui attendent : on ne sait pas laquelle va bouger.")
    return P


def arene_boss():
    """Arène du boss en contrebas : cuvette de 64 × 64 m, 8 m sous le jardin, fermée par des falaises ; escalier de
    descente au sud ; piliers brisés et statues géantes (accroches), couverts, buttes ; ciel et lune au-dessus."""
    P = Plan("Arene_Boss", "Jardin")
    P.p("Jardin_Ciel", 0, 0, 0)
    P.bloc("M_Bib_Terre_Racines", 0, -0.5, 0, 72, 1, 72)
    for k in range(4):
        for i in range(4):
            o = -24 + i * 16
            if k == 0 and i in (1, 2):
                continue
            x, z = {0: (o, -34), 1: (-34, o), 2: (o, 34), 3: (34, o)}[k]
            P.p("Jardin_Falaise_16m", x, 0, z, {0: 0, 1: 90, 2: 180, 3: -90}[k])
    P.p("Arene_EscalierDescente", 0, 8, -42, 0)
    for (x, z) in ((-16, -12), (16, -12), (-20, 10), (20, 10), (-6, 20), (6, 20)):
        P.p("Arene_PilierBrise", x, 0, z, P.r.uniform(0, 360))
    P.p("Jardin_Statue_Geante", -24, 0, -24, 45)
    P.p("Jardin_Statue_Geante", 24, 0, 24, -135)
    for (x, z, r) in ((-8, -4, 20), (9, 2, -30), (0, 12, 90), (-14, 18, 10), (16, -20, 60)):
        P.p("Arene_Couvert", x, 0, z, r)
    P.p("Arene_Butte", -18, 0, 4, 30)
    P.p("Arene_Butte", 18, 0, -2, 200)
    P.p("Jardin_ArbreMort_Tordu", 26, 0, -26, 0)
    for (x, z) in ((-14, -14), (14, 14), (0, 0)):
        P.p("Jardin_Brume", x, 0, z)
    for (x, z) in ((-8, -24), (8, -24)):
        P.p("Cave_Brasero", x, 0, z)
    P.vue("Descente", (0, 9.7, -40), (0, 0, 6))
    P.vue("Centre", (-4, 1.7, -14), (10, 6, 24))
    FICHES[P.nom] = dict(acte="Jardin", taille="64 × 64 m, 8 m sous le jardin",
                         identite="Cuvette de falaises au pied du domaine, ancien théâtre de verdure : piliers brisés, deux statues géantes, la lune juste au-dessus.",
                         fonction="Arène du boss : terrain dégagé avec relief, accroches de traction, couverts.",
                         entrees="Escalier de descente au sud (12 m de large, depuis le jardin) ; aucune autre sortie avant la mort du boss.",
                         route="Boucle complète autour des buttes (bunny hop sur 60 m) ; traction de pilier en pilier (16 à 20 m d'écart) ; statues géantes (accroche à 9 m) ; glissade sur les pentes des buttes.",
                         prises="6 piliers brisés (Accroche_Traction à 8 m), 2 statues géantes (Accroche_Traction à 9 m), l'arbre mort du coin sud-est.",
                         tension="On descend l'escalier vers une arène vide sous la lune ; le boss entre par le haut des falaises.")
    return P


def jardin_labyrinthe():
    """Labyrinthe de haies : carré de 48 m, haies de 2.6 m en couloirs de 4 m, kiosque au centre (repère et point haut),
    arbres morts aux angles, brume épaisse ; on s'y perd, on le survole par le kiosque et les arbres."""
    P = Plan("Jardin_Labyrinthe", "Jardin")
    P.p("Jardin_Ciel", 0, 0, 0)
    P.bloc("M_Bib_Gravier_Herbe", 0, -0.5, 0, 60, 1, 60)
    rr = P.r
    # grille de 8 × 8 cases de 6 m ; murs de haies sur les bords des cases selon un labyrinthe parfait (parcours en profondeur)
    N, C = 8, 6.0
    vis = [[False] * N for _ in range(N)]
    murs_h = {(i, j) for i in range(N) for j in range(N + 1)}
    murs_v = {(i, j) for i in range(N + 1) for j in range(N)}
    pile = [(0, 0)]
    vis[0][0] = True
    while pile:
        i, j = pile[-1]
        voisins = [(a, b) for a, b in ((i + 1, j), (i - 1, j), (i, j + 1), (i, j - 1)) if 0 <= a < N and 0 <= b < N and not vis[a][b]]
        if not voisins:
            pile.pop()
            continue
        a, b = voisins[rr.randrange(len(voisins))]
        if a != i:
            murs_v.discard((max(a, i), j))
        else:
            murs_h.discard((i, max(b, j)))
        vis[a][b] = True
        pile.append((a, b))
    # centre ouvert (kiosque) et entrées sud et nord
    for (i, j) in ((3, 4), (4, 4)):
        murs_h.discard((i, j))
    for (i, j) in ((4, 3), (4, 4)):
        murs_v.discard((i, j))
    murs_h.discard((3, 0))
    murs_h.discard((4, N))
    o = -N * C / 2
    for (i, j) in murs_h:
        P.p("Jardin_Haie_6m", o + (i + 0.5) * C, 0, o + j * C, 0)
    for (i, j) in murs_v:
        P.p("Jardin_Haie_6m", o + i * C, 0, o + (j + 0.5) * C, 90)
    P.p("Jardin_Kiosque", 0, 0, 0, 22.5)
    for (x, z, pf) in ((-30, -30, "Jardin_ArbreMort_Grand"), (30, 30, "Jardin_ArbreMort_Tordu"), (30, -30, "Jardin_ArbreMort_Penche"), (-30, 30, "Jardin_ArbreMort_Tordu")):
        P.p(pf, x, 0, z, rr.uniform(0, 360))
    for (x, z) in ((-12, -12), (12, -12), (-12, 12), (12, 12), (0, 0)):
        P.p("Jardin_Brume", x, 0, z)
    for (x, z) in ((-9, -21), (9, 15)):
        P.p("Jardin_Statue_Pleureuse", x, 0, z, rr.uniform(0, 360))
    P.p("Jardin_Portail", -3, 0, -24.5, 0)
    P.vue("Entree", (-3, 1.7, -30), (-3, 2, 0))
    P.vue("Couloir", (-9, 1.7, -15), (6, 2, -9))
    P.vue("Kiosque", (2, 7.5, -3), (-20, 1, -20))
    FICHES[P.nom] = dict(acte="Jardin", taille="48 × 48 m, haies de 2.6 m",
                         identite="Labyrinthe de haies devenu sauvage, kiosque au centre, statues voilées aux détours, brume jusqu'aux genoux.",
                         fonction="Salle de traque : le traqueur y entre aussi (les haies ne l'arrêtent pas visuellement mais le cachent) ; objectif au centre.",
                         entrees="Portail sud (x = -3) ; sortie nord (x = 3).",
                         route="Couloirs de 6 m (le chemin le plus court fait environ 70 m) ; double saut sur une haie (2.6 m < 2.9 m) pour couper ; toit du kiosque par traction : vue sur tout le labyrinthe ; arbres morts aux angles pour remonter.",
                         prises="Kiosque (Accroche_Traction à 5.2 m), arbres des angles, statues.",
                         tension="On n'entend que la brume et ses propres pas ; au détour d'une haie, une statue voilée, ou autre chose.")
    return P


ACTES["Jardin"] = [jardin_allee, jardin_labyrinthe, arene_boss]


def main():
    os.makedirs(OUT, exist_ok=True)
    for acte, fs in ACTES.items():
        txt = "\n---\n".join(f().texte() for f in fs)
        open(os.path.join(OUT, acte + ".txt"), "w", encoding="utf-8").write(txt + "\n")
        print("ok", acte, len(fs))


if __name__ == "__main__":
    main()
