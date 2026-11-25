# -*- coding: utf-8 -*-
"""Plans des ensembles de décor (nuit 6, lot 1), lus par EnsembleBuilder.Construit.

    python ensembles.py <plan.txt>
Repère Unity : +Y haut ; le mur (réel ou imaginaire) est le plan z = 0, l'ensemble s'étend vers +Z (vers le joueur), pivot au sol,
au milieu du mur. Ensembles « îlot » (tables, statues, kiosque…) : pivot au centre. Noms : « e:X » = pièce Ens_X, sinon prefab de
kit ou de prop existant. Options : D petit détail (masqué de loin), X sans collision, S sans lumière."""
import math, sys, random, json

TORCHE, BOUGIE, LUNE, BRAISE = "1,0.6,0.32", "1,0.62,0.34", "0.55,0.66,1", "1,0.45,0.2"
FROID = "0.42,0.52,0.85"
TOUS = []


class Ens:
    def __init__(self, nom, acte, emprise, haut, fonction, recit, adosse=True, accroches="", passage="", reprend=""):
        self.nom, self.acte = nom, acte
        self.l = [f"N|{nom}|{acte}"]
        self.f = dict(nom=nom, acte=acte, emprise=emprise, haut=haut, fonction=fonction, recit=recit, adosse=adosse, accroches=accroches, passage=passage, reprend=reprend)
        self.r = random.Random(nom)
        self.pieces = []
        TOUS.append(self)

    def p(self, n, x, z, ry=0, y=0, s="", op="", rx=0, rz=0):
        n = "Ens_" + n[2:] if n.startswith("e:") else n
        self.pieces.append(n)
        self.l.append(f"P|{n}|{x:.3f}|{y:.3f}|{z:.3f}|{rx:.1f},{ry:.1f},{rz:.1f}|{s}|{op}")
        return self

    def L(self, x, y, z, c=BOUGIE, i=0.9, r=6):
        self.l.append(f"L|{x:.2f}|{y:.2f}|{z:.2f}|{c}|{i}|{r}")
        return self

    def F(self, x, y, z, s):
        self.l.append(f"F|{x:.2f}|{y:.2f}|{z:.2f}|{s}")
        return self

    def C(self, x, y, z, sx, sy, sz, ry=0):
        self.l.append(f"C|{x:.2f}|{y:.2f}|{z:.2f}|{sx:.2f}|{sy:.2f}|{sz:.2f}|{ry}")
        return self

    def A(self, nom, x, y, z):
        self.l.append(f"A|{nom}|{x:.2f}|{y:.2f}|{z:.2f}")
        return self

    # raccourcis
    def torche(self, x, y=2.6, z=0.0, ry=0):
        return self.p("Cave_Torche_Murale", x, z, ry, y)

    def brasero(self, x, z):
        self.p("Cave_Brasero", x, z)
        return self.C(x, .53, z, .86, 1.06, .86)

    def bougies(self, x, z, y=0, l=True, ry=0):
        self.p("e:Bougies_B", x, z, ry or self.r.uniform(0, 360), y, op="D")
        return self.L(x, y + .6, z, BOUGIE, .8, 5) if l else self

    def chaises(self, *lst, pf="P_Prop_Chaise", s=1.12):
        for x, z, ry in lst:
            self.p(pf, x, z, ry, s=s)
        return self


# ==================================================================================== ACTE I
def caves():
    # ---------------------------------------------------------------- cellule de cachot
    e = Ens("Cave_Cellule_A", "Caves", "7.2 × 6.4 m", "5.6 m", "cachot occupé, porte close : on voit l'intérieur",
            "Le prisonnier n'est jamais sorti : squelette assis sous ses chaînes, compte des jours gravé, bougie à peine éteinte.",
            passage="Porte close et imposte barrée ; ouverte par le haut (murs de 4 m). Aucun refuge : tout l'ensemble laisse passer le bras et le regard du traqueur (`StalkerPassesThrough`).",
            accroches="Linteau à 5.6 m (`Accroche_Linteau`) ; grille = mur de saut mural.",
            reprend="Cave_Cachots : grille, porte, lit, paille, seau, murs de séparation ; traqueur : aucune cachette.")
    e.p("e:Gravats_Epars_B", .2, 3.4, 20, op="D").p("e:Gravats_Epars_C", .5, 7.2, 100, op="D").p("e:Etagere_Murale", 1.9, .04, 0, 1.7, s=.8)
    e.p("Cave_Cellule_Grille", 0, 6.0).p("Cave_Cellule_Porte", .7, 6.0).p("e:Grille_Imposte", 0, 6.0, 0, 2.55).p("e:Marques_Jours", -.2, .02, 0, 1.75)
    for sx in (-1, 1):
        e.p("Cave_MurBrise_4m", sx * 3.35, 2.0, 90 * sx, s="0.5,1,0.6")
        e.p("Cave_MurBrise_4m", sx * 3.35, 4.2, 90 * sx, s="0.45,1.12,0.6")
    e.C(-3.35, 2.0, 3.0, .7, 4.0, 6).C(3.35, 2.0, 3.0, .7, 4.0, 6)
    e.p("Cave_Lit", 1.9, .7, 0).p("P_Prop_Paille", -1.6, 2.4, 25).p("P_Prop_Paille", 1.5, 2.2, 110, y=.012, s=.8)
    e.p("e:Chaines_Murales", -1.3, .05).p("e:Squelette_Assis", -1.35, .35).p("Cave_Seau", 2.5, 4.6).p("e:Os_Tas", .9, 4.0, 60, op="D")
    e.p("e:Chope_Cruche", .35, 1.0, 20, op="D").bougies(.55, .55, 0)
    e.p("Decal_CoulureLongue", .8, .06, 0, 2.6).p("Decal_ToileAraignee", 2.6, .06, 0, 3.4)
    e.L(0, 2.2, 2.2, BOUGIE, .9, 7)
    e.p("Cave_Torche_Murale", -3.35, 6.12, 0, 2.7).p("Cave_Torche_Murale", 3.35, 6.12, 0, 2.7, op="S")
    e.A("Accroche_Linteau", 0, 5.6, 6.0)

    e = Ens("Cave_Cellule_B", "Caves", "7.2 × 6.6 m", "4.2 m", "cachot forcé, ouvert : cul-de-sac visible en entier",
            "Évasion : barreaux écartés, porte arrachée au sol, trou creusé dans le mur du fond, chaînes vides.",
            passage="Ouverture de 3.2 m entre les deux panneaux de grille, sans plafond : le traqueur y passe le bras (règle des 3 m) et enjambe les murs de 4 m.",
            accroches="Tête des murets à 4 m (`Accroche_Muret`).", reprend="Cave_Cachots : porte, paille, seau, murs ; cellule forcée n° 2.")
    for sx in (-1, 1):
        e.p("Cave_MurBrise_4m", sx * 3.35, 2.0, 90 * sx, s="0.5,0.95,0.6").p("Cave_MurBrise_4m", sx * 3.35, 4.3, 90 * sx, s="0.45,0.8,0.6")
    e.p("e:Gravats_Epars", 1.4, 2.2, 20, op="D").p("e:Gravats_Epars_B", -.3, 6.6, 100, op="D")
    e.C(-3.35, 1.9, 3.0, .7, 3.8, 6).C(3.35, 1.9, 3.0, .7, 3.8, 6)
    e.p("e:Grille_2m_Pliee", -2.2, 5.9, 6).p("e:Grille_2m_Pliee", 2.35, 5.7, -14)
    e.p("Cave_Cellule_Porte", .2, 7.2, 24, y=.16, rx=-86)
    e.p("Cave_Lit", -1.7, 1.6, 64).p("P_Prop_Paille", .9, 2.0, 10).p("P_Prop_Paille", -.8, 3.4, 80, s=.75, y=.012)
    e.p("e:Eboulis_B", 2.0, .9, 30, s=.62).p("Decal_EclatAngle", 1.9, .06, 0, .9, s=2)
    e.p("e:Chaines_Murales", -.6, .05).p("Cave_Seau", 2.4, 4.2, rz=-82, y=.17).p("e:Pioches", 2.75, 2.9, -90)
    e.p("e:Os_Tas", -2.3, 4.4, 200, op="D").p("e:Corde", .4, 4.2, 40, op="D").p("Decal_TacheSang", .6, 5.2, 0, .02, rx=90)
    e.p("P_Prop_Lanterne", 1.2, 3.1, 30)
    e.L(0, 2.6, 2.4, BOUGIE, .8, 8)
    e.p("Cave_Torche_Murale", -3.2, 6.08, 0, 2.6)
    e.A("Accroche_Muret", -3.35, 3.9, 3).A("Pose_Attrapable", .4, .4, 4.2)

    # ---------------------------------------------------------------- poste de garde
    e = Ens("Cave_PosteGarde_A", "Caves", "7.5 × 4.5 m", "3.2 m", "poste de garde contre un mur : lumière et objets à lancer",
            "Partie de cartes interrompue : pièces sur la table, un tabouret renversé, chopes pleines.",
            accroches="Table (0.95 m) = tremplin.", passage="Voie libre de 10 m devant ; rien au-dessus de 2 m sauf le trophée mural.",
            reprend="Cave_SalleDeGarde (table, tabourets, brasero, torches) ; P_Prop_Ratelier, P_Prop_Table, tonneau.")
    e.p("e:Banniere", -2.9, .02, 0, 4.6).p("e:Banniere", 3.0, .02, 0, 4.6).p("e:Etagere_Murale", 3.0, .02, 0, 1.45).p("e:Gravats_Epars_C", .4, 3.6, 10, op="D").p("e:Gravats_Epars", -1.6, 1.6, 120, op="D").p("P_Prop_CoffreDeco", -1.5, .55, 4).p("e:Caisse_Ouverte", 3.6, 3.0, 70, s=.8)
    e.p("P_Prop_Table", 0, 2.1, 8).p("e:Des_Cartes", .2, 2.1, 190, y=.955, op="D").p("e:Chope_Cruche", -.7, 2.2, 30, y=.955, op="D").p("e:Vaisselle_B", .75, 2.05, 160, y=.955, op="D")
    e.bougies(-.15, 2.4, .955)
    e.p("P_Prop_Banc", .05, 1.15, 6).p("Cave_Tabouret", -1.8, 2.8, 20, s=1.1).p("Cave_Tabouret", .4, 3.5, 40, y=.2, rz=88, s=1.1)
    e.p("P_Prop_Ratelier", -2.9, .35).p("e:Bouclier_Epees", 0, .02, 0, 2.5).p("P_Prop_Tonneau", 2.5, .55, 30).p("P_Prop_Tonneau", 3.15, .75, 100)
    e.p("e:Jarres", 3.1, 1.9, 200, op="D", s=.8).p("e:Corde", -2.0, 1.0, 30, op="D")
    e.brasero(-3.0, 3.1).torche(-1.5).torche(1.7)
    e.p("Decal_TacheSuie", 0, .05, 0, 3.6).p("e:Sacs_A", 2.2, 3.6, 150, s=.85)
    e.A("Pose_Attrapable_1", 2.5, .5, 1.2).A("Pose_Attrapable_2", -1.9, .5, 3.6)

    e = Ens("Cave_PosteGarde_B", "Caves", "7.5 × 4.8 m", "3.2 m", "poste de garde mis à sac : couverts bas",
            "Garde surprise : table basculée en barricade, coffre forcé, un corps contre le mur.",
            accroches="Table basculée (1.2 m) = couvert et appui.", passage="Voie libre de 10 m devant.", reprend="Cave_SalleDeGarde ; P_Prop_Table, P_Prop_Ratelier, P_Prop_Chaise.")
    e.p("e:Banniere", -2.9, .02, 0, 4.5, rz=8).p("e:Etagere_Murale_Vide", .3, .02, 0, 2.0).p("e:Gravats_Epars_B", -.6, 3.8, 10, op="D").p("e:Gravats_Epars_C", 2.0, 2.4, 80, op="D").p("P_Prop_Paille", -2.0, 2.4, 0, s=.8)
    e.p("P_Prop_Table", .2, 2.7, 40, y=.62, rx=78).p("P_Prop_Banc", -1.9, 3.4, 64, y=.3, rx=88).p("Cave_Tabouret", 2.1, 3.7, 10, y=.2, rz=-86, s=1.1)
    e.p("P_Prop_Ratelier", -2.7, .5, 4).p("e:Squelette_Assis", 2.2, .4, 0).p("e:Bouclier_Epees", 3.6, .35, 10, y=.62, rx=-14)
    e.p("e:Caisse_Ouverte", 3.0, 1.3, 70).p("P_Prop_Tonneau", -1.6, 1.3, 20, y=.3, rz=90).p("P_Prop_Tonneau", 3.3, 2.7, 0)
    e.p("e:Vaisselle_B", .6, 4.1, 100, op="D").p("e:Des_Cartes", -.5, 4.0, 60, op="D")
    e.p("Decal_TacheSang", .6, 1.2, 0, .02, rx=90, s=1.4).p("e:Os_Tas", -3.1, 2.2, 0, op="D")
    e.brasero(-3.2, 3.3).torche(1.6).p("Decal_CoulureRouille", -2.7, .05, 0, 2.2)
    e.A("Pose_Attrapable_1", 1.6, .5, 1.6)

    # ---------------------------------------------------------------- réserve
    e = Ens("Cave_Reserve_A", "Caves", "8 × 3.6 m", "2.8 m", "réserve rangée contre un mur : gisement d'objets attrapables",
            "Provisions pour un siège : tonneaux en rang, caisses empilées, sacs de grain, étagère de jarres.",
            accroches="Pile de caisses (1.3 et 2.6 m) = marches vers une corniche.", passage="Voie libre devant.",
            reprend="Cave_SalleDeGarde (réserve) et Cave_CavernePuits : tonneaux, caisses, étagère.")
    e.p("e:Rail_Suspendu", .3, .02, 0, 2.35).p("e:Etagere_Murale", -2.4, .02, 0, 3.0, s=1.1).p("e:Gravats_Epars", .0, 2.4, 10, op="D").p("e:Gravats_Epars_B", 2.4, 3.2, 80, op="D").p("P_Prop_Paille", -.4, 1.9, 40, s=.9)
    e.p("Cave_Caisses_Pile", -2.6, .95, 0).p("P_Prop_Tonneaux_Couches", .9, 1.1, 0).p("P_Prop_Tonneaux_Couches", .9, 1.1, 0, y=.6, s=.9)
    e.p("Cave_Etagere", 3.0, .36, 0, s=1.1).p("e:Sacs_B", -.75, 2.6, 40).p("e:Sacs_A", 2.9, 1.7, 170).p("e:Jarres", -3.4, 2.5, 30)
    e.p("P_Prop_Tonneau", -.8, .9, 50).p("e:Caisse_Ouverte", 1.6, 2.8, 12).p("e:Corde", .6, 3.2, 80, op="D").p("Cave_Seau", 2.4, 3.1)
    e.p("P_Prop_Lanterne", -.8, .9, 0, .9, s=.58).torche(-1.0, 3.2).torche(1.62, 3.2)
    e.p("Decal_MoussePlaque", -2.0, .05, 0, 2.0)
    e.A("Pose_Attrapable_1", -1.9, .45, 2.6).A("Pose_Attrapable_2", .4, .5, 2.4).A("Pose_Attrapable_3", 3.5, .5, 2.9)

    e = Ens("Cave_Reserve_B", "Caves", "8 × 4 m", "2.7 m", "réserve pillée en coin : couverts et marches",
            "Pillée par ce qui rôde : sacs éventrés, tonneau brisé, traînée de grain vers l'ombre.",
            accroches="Pile de caisses (1.3 et 2.6 m).", passage="Voie libre devant ; aucun objet au-dessus de 2.7 m.", reprend="Cave_SalleDeGarde, Cave_CavernePuits ; P_Prop_Etagere, P_Prop_Tonneau.")
    e.p("e:Rail_Suspendu", -1.8, .02, 0, 2.6, rz=6).p("e:Etagere_Murale_B", 1.0, .02, 0, 1.9).p("e:Gravats_Epars_C", -.4, 2.6, 10, op="D").p("e:Gravats_Epars", 1.8, 3.4, 80, op="D")
    e.p("P_Prop_Etagere", -2.4, 1.6, 8, y=.42, rx=-86).p("Cave_Caisses_Pile", 2.95, 1.0, 180).p("e:Caisse_Ouverte", 1.3, 3.2, -30).p("e:Sacs_Eventres", -.6, 1.4, 20)
    e.p("e:Sacs_B", .75, 1.6, 190).p("e:Tonneau_Brise", -3.2, 3.3, 40).p("P_Prop_Tonneau", 3.4, 2.6, 0).p("P_Prop_Tonneau", 2.5, 3.3, 70, y=.3, rx=90)
    e.p("e:Jarres", -1.6, 3.4, 110).p("P_Prop_Paille", 1.5, 3.1, 60, s=.7).p("e:Os_Tas", -.1, 3.9, 20, op="D").p("P_Prop_Poutre", -.3, .4, 2, y=.0)
    e.torche(-.6, 2.8).p("P_Prop_Lanterne", 2.4, 4.0, 0)
    e.L(2.6, 2.2, 2.4, BOUGIE, .7, 6)
    e.A("Pose_Attrapable_1", -.2, .5, 3.7).A("Pose_Attrapable_2", 3.1, .5, 1.9)

    # ---------------------------------------------------------------- éboulis
    e = Ens("Cave_Eboulis_A", "Caves", "7 × 4.5 m", "3.7 m", "coin d'éboulis étayé : fin de galerie, appui de saut",
            "Galerie de mine effondrée derrière son étai ; les outils sont restés.",
            accroches="Blocs à 0.8, 1.6 et 2.2 m ; chapeau de l'étai à 3.4 m (`Accroche_Etai`).", passage="Voie libre devant ; le fond est plein (ni cachette ni trou).",
            reprend="Cave_CavernePuits (éboulis, décalques) ; Cave_Eboulis_Petit.")
    e.p("e:Gravats_Epars_B", .4, 3.4, 10, op="D", s=1.2).p("e:Gravats_Epars_C", -1.6, 3.4, 80, op="D").p("Cave_Caisses_Pile", 3.4, 1.0, 190, s=.7)
    e.p("e:Eboulis_A", 0, 1.4, 180 * 0).p("e:Etai", 0, 2.6).p("e:Eboulis_B", -2.7, 2.2, 40, s=.8).p("Cave_Eboulis_Petit", 2.9, 2.2, 160, s=.8)
    e.p("P_Prop_Poutre", 2.7, 3.4, 60, y=0).p("e:Bouche_Mine", 0, .0).p("e:Pioches", -3.7, .02)
    e.p("P_Prop_Lanterne", -1.1, 3.4, 20).p("e:Corde", -2.1, 3.7, 20, op="D").p("Cave_Seau", -.6, 4.2)
    e.L(0, 1.9, 3.4, BOUGIE, 1.0, 8).torche(3.3, 2.7)
    e.A("Accroche_Etai", 0, 3.45, 2.6)

    e = Ens("Cave_Eboulis_B", "Caves", "7 × 7 m", "3.7 m", "voie de mine et wagonnet : glissade et saut",
            "Le wagonnet a déraillé quand la voûte a cédé ; un mineur est resté dessous.",
            accroches="Wagonnet (1.1 m) et étai (3.4 m).", passage="La voie ferrée est plate (0.2 m) : on glisse dessus.", reprend="Cave_Eboulis_Petit, P_Prop_Poutre, P_Prop_Lanterne.")
    e.p("e:Gravats_Epars", .4, 3.6, 10, op="D", s=1.2).p("e:Gravats_Epars_B", 1.8, 5.6, 80, op="D")
    e.p("e:Eboulis_A", -.6, 1.4, 0).p("e:Eboulis_B", 2.4, 1.6, 70, s=.9).p("e:Etai", .3, 2.9, 8)
    e.p("e:Rails_4m", .4, 4.6, 6).p("e:Wagonnet", 1.2, 4.6, 12, y=.66, rx=88).p("e:Eboulis_B", .9, 5.7, 60, s=.5).p("e:Bouche_Mine", 0, 0).p("e:Os_Tas", 2.3, 5.3, 60, op="D")
    e.p("P_Prop_Poutre", -2.3, 3.2, 70, y=.1).p("e:Pioches", 3.3, .02).p("P_Prop_Lanterne", -1.2, 5.2, 20)
    e.p("Cave_Torche_Murale", 2.05, 3.05, 8, 2.4)
    e.A("Accroche_Etai", .3, 3.45, 2.9).A("Pose_Attrapable", -1.8, .5, 5.6)

    # ---------------------------------------------------------------- autel, puits
    e = Ens("Cave_Autel_A", "Caves", "Ø 8 m", "2.9 m", "autel ancien, îlot : repère de salle, place d'un coffre ou d'un marchand",
            "Plus vieux que le château : dalle fendue, coupe, crânes, pierres levées qui portent le même signe.", adosse=False,
            accroches="Pierres levées (2.8 m) : appuis et murs de saut mural.", passage="On tourne autour : 1.6 m libres entre les pierres.", reprend="Cave_CavernePuits (îlot central, chandelles, décalques).")
    e.p("e:Gravats_Epars_C", 1.6, 2.0, 10, op="D").p("e:Gravats_Epars", -1.9, -1.2, 80, op="D").p("e:Gravats_Epars_B", -1.0, 2.4, 200, op="D")
    e.p("e:Autel_Ancien", 0, 0)
    for k, (a, pf, s) in enumerate(((14, "A", 1), (158, "A", .92), (205, "B", 1.1), (270, "A", 1.15), (330, "B", 1))):
        x, z = math.cos(math.radians(a)) * 3.5, math.sin(math.radians(a)) * 3.2
        e.p("e:Pierre_Levee_" + pf, x, z, math.degrees(math.atan2(-x, -z)), s=s)
    e.bougies(-1.15, .75, .22).bougies(1.2, .7, .22).bougies(-1.2, -.7, .22, False).bougies(1.25, -.75, .22, False)
    e.bougies(-.55, .35, 1.19, False).p("e:Os_Tas", 2.2, 1.9, 100, op="D").p("e:Os_Tas", -2.3, 1.6, 0, op="D")
    e.p("Decal_TacheSang", 0, 1.6, 0, .02, rx=90, s=1.5).L(0, 2.4, .4, "0.72,0.86,0.7", .9, 7)
    e.A("Place_Coffre", 0, 0, 2.2)

    e = Ens("Cave_Puits_A", "Caves", "Ø 7 m", "4.0 m", "puits ancien, îlot : repère, point de rassemblement",
            "La margelle est usée par les cordes ; on y a vidé bien autre chose que de l'eau.", adosse=False,
            accroches="Portique du puits à 4 m (`Accroche_Portique`).", passage="On tourne autour ; eau à 0.45 m sous la margelle (rien sous le sol).", reprend="Cave_CavernePuits : puits du kit, chandelles, caisses.")
    e.p("e:Gravats_Epars_C", 1.6, 2.0, 10, op="D").p("e:Gravats_Epars", -1.9, -1.2, 80, op="D").p("e:Gravats_Epars_B", -1.0, 2.4, 200, op="D")
    e.p("Cave_Puits", 0, 0, 15).p("e:Eau_Disque", 0, 0, y=.42, s="1.2,1,1.2", op="").p("e:Corde", 1.7, 1.5, 60, op="D").p("Cave_Seau", -1.6, 1.7).p("Cave_Seau", -2.0, 1.4, rz=84, y=.17)
    e.p("e:Baquet", 2.7, -1.7).p("e:Jarres", -2.9, -1.7, 60).p("P_Prop_Banc", .4, 2.8, 8).p("e:Eboulis_B", 2.4, -2.0, 30, s=.5)
    e.bougies(1.1, .9, .9, True).bougies(-1.2, .7, .9, False).p("P_Prop_Lanterne", 0, 2.75, 0, .56, s=.58).p("Decal_MoussePlaque", 0, 2.0, 0, .02, rx=90, s=2)
    e.C(0, .4, 0, 2.4, .8, 2.4, 45)
    e.L(0, 2.2, 0, "0.5,0.8,0.75", .9, 6).A("Accroche_Portique", 0, 3.9, 0)

    e = Ens("Cave_Puits_B", "Caves", "Ø 7 m", "4.0 m", "puits condamné, îlot : couverts bas et repère", "On a cloué des planches sur le puits et posé des pierres dessus ; quelque chose a poussé d'en dessous.", adosse=False,
            accroches="Portique à 4 m (`Accroche_Portique`).", passage="On tourne autour ; le puits est bouché (planches et pierres).", reprend="Cave_CavernePuits : puits du kit, éboulis, brasero.")
    e.p("Cave_Puits", 0, 0, 200).p("e:Eau_Disque", 0, 0, y=.42, s="1.2,1,1.2")
    e.p("e:Planches_Puits", 0, 0, 12).p("e:Pierre_Levee_B", -2.7, -1.0, 70, s=.9).p("e:Pierre_Levee_B", 2.7, -1.2, -60, s=.85)
    e.p("e:Eboulis_B", -1.7, 1.9, 20, s=.6).p("e:Eboulis_B", 2.2, 1.9, 120, s=.5).p("e:Os_Tas", .3, 2.1, 0, op="D").p("e:Corde", -2.4, .6, 10, op="D").p("e:Gravats_Epars_C", .4, 2.4, 0, op="D").p("e:Gravats_Epars", -1.0, -1.8, 90, op="D").p("Decal_TacheSang", .6, 1.9, 0, .02, rx=90)
    e.bougies(1.0, 2.5).bougies(-.7, 2.7, 0, False).C(0, .4, 0, 2.4, .8, 2.4, 45).L(0, 2.0, .6, "0.5,0.8,0.75", 1.1, 7).A("Accroche_Portique", 0, 3.9, 0)

    e = Ens("Cave_Autel_B", "Caves", "7 × 4.2 m", "3.2 m", "autel mural profané : fond de salle, niche à objet",
            "L'autel sert encore : cire fraîche, offrandes, chaînes qui pendent.", accroches="Dessus d'autel à 1.2 m.",
            passage="Voie libre devant.", reprend="Cave_CavernePuits ; P_Prop_Sarcophage.")
    e.p("e:Gravats_Epars_B", 1.6, 2.8, 10, op="D").p("e:Banniere", -1.4, .02, 0, 4.0).p("e:Banniere", 1.4, .02, 0, 4.0)
    e.p("e:Autel_Ancien", 0, 1.5).p("e:Pierre_Levee_A", -2.7, .9, 20).p("e:Pierre_Levee_A", 2.8, 1.0, -24, s=.95).p("P_Prop_Sarcophage", 0, 3.4, 4).p("e:Pierre_Levee_B", -2.4, 2.9, 60, s=.8)
    e.bougies(-1.4, 2.3, .22).bougies(1.4, 2.2, .22).bougies(-.7, 1.2, 1.19, False).bougies(.9, 3.4, .78, False)
    e.p("e:Os_Tas", 2.1, 2.9, 200, op="D").p("Decal_TacheSang", 0, 2.6, 0, .235, rx=90).p("Decal_CoulureLongue", -1.2, .05, 0, 2.6).p("Decal_FissureEtoile", 1.1, .05, 0, 2.2)
    e.L(0, 2.6, 1.0, "0.72,0.86,0.7", .9, 7)

    # ---------------------------------------------------------------- torture (discrète)
    e = Ens("Cave_Torture_A", "Caves", "8 × 4.8 m", "4.3 m", "salle de question, discrète : suggérée, jamais montrée",
            "Tout est rangé, propre et prêt ; seule la cage pend encore.", accroches="Bras de potence à 4 m (`Accroche_Potence`).",
            passage="Cage à 0.5 m du sol, contre le mur ; voie libre devant.", reprend="Cave_Cachots (chaînes) ; P_Prop_Table, P_Prop_Chaudron, brasero du kit.")
    e.p("e:Etagere_Murale_B", -3.0, .02, 0, 1.9).p("e:Gravats_Epars_C", 1.0, 2.8, 10, op="D").p("e:Gravats_Epars", -2.2, 2.6, 80, op="D").p("e:Banniere_Bleue", -.3, .02, 0, 4.6).p("P_Prop_Paille", -.6, 2.4, 20).p("P_Prop_Tonneau", -4.0, 2.0, 0)
    e.p("e:Chevalet", -.4, 2.2, 78).p("e:Potence", 3.0, .02).p("e:Cage_Suspendue", 3.0, 1.35, 20, 3.88).p("P_Prop_Table", -2.9, .85, 0).p("e:Outils_Table", -3.1, .9, 180, y=.955, op="D")
    e.bougies(-2.2, .75, .955).p("e:Chaines_Murales", 1.35, .04).p("P_Prop_Chaudron", 1.9, 3.4, 20, s=.8).p("Cave_Seau", -1.4, 3.6).p("e:Baquet", -3.6, 3.0)
    e.brasero(1.8, 1.2).p("e:Fers_Brasero", 1.8, 1.2, 40, y=.02, s=.9).torche(-1.7, 2.7)
    e.p("Decal_TacheSang", -.5, 3.6, 0, .02, rx=90, s=.9).p("Decal_CoulureRouille", 3.0, .05, 0, 2.0)
    e.A("Accroche_Potence", 3.0, 4.1, 1.35)

    e = Ens("Cave_Torture_B", "Caves", "8 × 4.6 m", "4.3 m", "salle de question abandonnée : couverts bas",
            "Abandonnée en hâte : pilori ouvert, chaînes au bout de la potence, un registre encore ouvert.", accroches="Bras de potence à 4 m (`Accroche_Potence`).",
            passage="Chaînes à 1 m du sol : on passe dessous ; voie libre devant.", reprend="Cave_Cachots ; P_Prop_Table, P_Prop_Banc, P_Prop_CoffreDeco.")
    e.p("e:Etagere_Murale_B", 1.3, .02, 0, 1.9).p("e:Gravats_Epars_B", .2, 2.6, 10, op="D").p("e:Gravats_Epars_C", -2.6, 3.8, 80, op="D").p("P_Prop_Tonneau", -3.7, 2.2, 0).p("e:Jarres", -3.4, 4.0, 40, s=.8)
    e.p("e:Pilori", 1.9, 2.4, 14).p("e:Potence", -2.6, .02).p("e:Chaines_Plafond", -2.6, 1.0, 90, 3.95).p("P_Prop_Table", .2, .9, 4).p("e:Livres_Table", .1, .9, 180, y=.955, op="D")
    e.bougies(.95, .8, .955).p("P_Prop_Banc", -1.3, 2.6, 12).p("P_Prop_Chaudron", -3.4, 1.4, 0, s=.75).p("P_Prop_CoffreDeco", 3.2, .6, -6).p("e:Chaines_Murales", -1.0, .04).p("e:Outils_Table", 2.9, 3.6, 40, op="D")
    e.p("P_Prop_Paille", -2.4, 1.6, 40, s=.8).p("e:Os_Tas", -3.4, 1.0, 0, op="D").p("e:Corde", .9, 3.6, 10, op="D").p("Cave_Candelabre", 3.5, 2.0)
    e.C(3.4, .85, 2.0, .5, 1.7, .5).torche(-.2, 2.8).p("Decal_TacheHumidite", -2.0, .05, 0, 2.6).p("Decal_TacheSang", 2.0, 3.4, 0, .02, rx=90, s=.8)
    e.A("Accroche_Potence", -2.6, 4.1, 1.35)


# ==================================================================================== ACTE II
def chateau():
    e = Ens("Chateau_Bibliotheque_A", "Chateau", "12.4 × 5.5 m (9.5 m sans les tentures)", "4.4 m", "coin de bibliothèque : fond de salle, mur de saut",
            "Quelqu'un lisait encore tout à l'heure : fauteuil tourné, livre ouvert, bougies hautes.",
            accroches="Dessus des bibliothèques à 4 m (`Accroche_Corniche`) ; table à 0.95 m.", passage="Voie libre devant ; 3 m entre la table et les rayons.",
            reprend="Chateau_SalleBibliotheque (rayons, table, fauteuils, lutrin, tapis) : composition reprise, livres en relief à la place des rayons peints.")
    for x in (-3.75, -1.25, 1.25, 3.75):
        e.p("e:Bibliotheque", x, .02)
    e.p("Chateau_Tenture", -5.65, .0, 0, 4.6, s="0.3,0.5,0.8").p("Chateau_Tenture", 5.65, .0, 0, 4.6, s="0.3,0.5,0.8").p("e:Papiers_Epars", 1.0, 2.4, 10, op="D").p("e:Papiers_Epars", -2.2, 4.8, 80, op="D", s=.7)
    e.p("e:Echelle_Biblio", 1.5, .5).p("e:Tapis_4x3", 0, 3.4, 4).p("P_Prop_Table", -.6, 3.4, 6).p("e:Livres_Table", -.6, 3.4, 190, y=.955, op="D").p("e:Candelabre_Table", .35, 3.35, 70, y=.955)
    e.p("P_Prop_Fauteuil", -2.6, 3.6, 110).p("P_Prop_Chaise", .2, 4.4, 190, s=1.15).p("e:Globe", 2.9, 2.2).p("e:Pupitre", 3.4, 4.0, -30).p("e:Livres_Sol", -3.6, 1.9, 30, op="D").p("e:Livres_Sol", 2.0, 1.7, 160, op="D", s=.85)
    e.p("Chateau_Candelabre_Pied", -3.9, 4.7)
    e.L(.2, 2.0, 3.3, BOUGIE, 1.1, 8).L(3.2, 2.2, 3.4, BOUGIE, .7, 5)
    e.A("Accroche_Corniche", 0, 4.0, .4)

    e = Ens("Chateau_Bibliotheque_B", "Chateau", "10.5 × 6 m", "4.4 m", "coin de bibliothèque ravagé : couverts bas et pente",
            "Un rayon a été renversé sur le flanc, vidé ; les pages ont volé jusqu'au milieu de la salle.", accroches="Rayon couché (dessus à 2.5 m) = appui ; corniche à 4 m.",
            passage="Voie libre devant.", reprend="Chateau_SalleBibliotheque ; P_Prop_Etagere, Chateau_Table_Renversee.")
    e.p("e:Papiers_Epars", -1.0, 3.2, 10, op="D", s=1.2).p("e:Papiers_Epars", 1.6, 5.4, 80, op="D").p("Chateau_Tenture", -5.2, .0, 0, 4.6, s="0.3,0.5,0.8").p("e:Gravats_Epars", 1.4, 2.0, 0, op="D").p("e:Livres_Sol", .2, 5.3, 300, op="D").p("e:Echelle_Biblio", -2.6, 3.0, 60, rx=0, y=.1, rz=0, op="X") if False else e
    e.p("e:Bibliotheque", -3.4, .02).p("e:Bibliotheque", 4.0, .02).p("e:Bibliotheque_Pillee", 2.05, 1.0, -4, y=1.25, rz=90).p("e:Livres_Sol", -.9, 2.0, 30, op="D").p("e:Livres_Sol", .3, 2.3, 140, op="D").p("e:Livres_Sol", 1.2, 1.9, 250, op="D").p("e:Livres_Sol", -.2, 3.2, 80, op="D")
    e.p("e:Livres_Sol", -1.6, 4.9, 0, op="D").p("e:Livres_Sol", 1.3, 5.4, 120, op="D").p("e:Livres_Sol", 2.1, 2.1, 220, op="D")
    e.p("P_Prop_Chaise", -2.0, 4.0, 130, y=.24, rx=88, s=1.15).p("P_Prop_Fauteuil", 3.2, 4.4, -140, y=.48, rz=86).p("e:Globe", -2.5, 2.2, 60, y=.35, rz=70).p("e:Pupitre", 3.4, 2.0, -20)
    e.p("e:Candelabre_Table", 2.4, 4.7, 20, y=.1, rx=84, op="D").p("P_Prop_Table", -3.7, 4.6, 80).p("e:Livres_Table", -3.7, 4.6, 260, y=.955, op="D").bougies(-3.75, 5.5, .955)
    e.p("Decal_ToileAraignee", -2.1, .6, 0, 3.4).L(3.0, 2.4, 2.2, BOUGIE, .9, 7).L(0, 3.0, 3.4, LUNE, .8, 8)
    e.A("Accroche_Corniche", 3.5, 4.0, .4)

    # ---------------------------------------------------------------- salle à manger
    e = Ens("Chateau_SalleManger_A", "Chateau", "8.5 × 5 m", "1.8 m (cheminée 4.7 m en fond possible)", "salle à manger dressée, îlot : longue table à courir",
            "Le banquet attend toujours ses convives : couverts mis, bougies allumées, viande rassise.", adosse=False,
            accroches="Table de 6 m à 0.93 m : piste de course et de glissade ; un lustre de main peut pendre au-dessus.", passage="On tourne autour (2 m libres).",
            reprend="Chateau_GrandHall (table de banquet et chaises autour du tapis) ; P_Prop_Chaise.")
    e.p("e:Tapis_4x3", -2.25, 0, 0, s=1.08).p("e:Tapis_4x3", 2.25, 0, 180, s=1.08).p("e:Table_Longue", 0, 0)
    for i, x in enumerate((-2.3, -1.15, 0, 1.15, 2.3)):
        e.p("P_Prop_Chaise", x, 1.2, 180 + (i * 37 % 13 - 6), s=1.15).p("P_Prop_Chaise", x, -1.2, (i * 53 % 15 - 7), s=1.15)
        e.p("e:Vaisselle_A" if i % 2 == 0 else "e:Vaisselle_B", x, .42, 180, y=.935, op="D", s=.85)
        if i % 2 == 0:
            e.p("e:Vaisselle_A", x, -.42, 0, y=.935, op="D", s=.85)
    e.p("P_Prop_Fauteuil", 3.7, 0, -90).p("P_Prop_Fauteuil", -3.75, .1, 84)
    e.p("e:Banquet", 0, 0, 0, y=.935, op="D").p("e:Candelabre_Table", -1.7, 0, 0, y=.935).p("e:Candelabre_Table", 1.7, 0, 0, y=.935)
    e.L(-1.7, 2.0, 0, BOUGIE, 1.1, 7).L(1.7, 2.0, 0, BOUGIE, 1.1, 7)

    e = Ens("Chateau_SalleManger_B", "Chateau", "9 × 5.5 m", "1.8 m", "salle à manger après l'attaque, îlot : couverts bas",
            "Ils ont fui au milieu du plat : chaises renversées, vaisselle balayée, lustre tombé sur la table.", adosse=False,
            accroches="Table à 0.93 m ; lustre tombé (1 m).", passage="On tourne autour.", reprend="Chateau_GrandHall, Chateau_SalleDeBal (tables et chaises renversées).")
    e.p("e:Verre_Brise", .8, 1.6, 0, op="D").p("e:Vaisselle_A", 1.2, 3.0, 40, op="D").p("e:Papiers_Epars", -1.6, 2.6, 0, op="D", s=.6)
    e.p("e:Table_Longue", 0, 0, 5).p("e:Lustre_Tombe", .7, .1, 30, y=.9, s=.85)
    e.chaises((-2.4, 1.5, 200), (2.6, -1.3, 8), (-.8, -1.25, -4))
    for x, z, ry, rx, rz in ((-1.2, 1.8, 30, 88, 0), (1.9, 1.7, 120, 88, 0), (.4, 2.3, 250, 0, 88), (-2.6, -1.9, 60, 88, 0), (1.0, -2.1, 10, 0, -88)):
        e.p("P_Prop_Chaise", x, z, ry, y=.24, s=1.15, rx=rx, rz=rz)
    e.p("P_Prop_Fauteuil", 3.9, .8, -60).p("P_Prop_Fauteuil", -3.8, -.4, 50, y=.37, rx=-84)
    e.p("e:Vaisselle_B", -1.9, .3, 20, y=.935, op="D").p("e:Vaisselle_A", -2.4, -.3, 170, y=.935, op="D").p("e:Vaisselle_B", 3.2, 2.9, 60, op="D").p("e:Vaisselle_B", -.2, 3.0, 100, op="D").p("e:Banquet", -1.0, -2.4, 40, op="D")
    e.p("e:Candelabre_Table", -1.5, -.15, 10, y=.935).p("Decal_TacheSang", -.4, 2.6, 0, .02, rx=90)
    e.L(-1.2, 2.0, 0, BOUGIE, 1.0, 8).L(2.2, 2.2, 0, LUNE, .8, 7)

    e = Ens("Chateau_Cheminee_A", "Chateau", "6.5 × 4.5 m", "8.3 m (conduit)", "cheminée et fauteuils : foyer de lumière d'une grande salle",
            "Le feu brûle sans personne pour le nourrir.", accroches="Manteau à 2.9 m (`Accroche_Manteau`).", passage="Voie libre devant.", reprend="P_Prop_CoffreDeco, P_Prop_Fauteuil ; tapis du château.")
    e.p("e:Banniere", -3.0, .02, 0, 4.6).p("e:Banniere", 3.0, .02, 0, 4.9, s=.85).p("e:Portrait_7", 0, .55, 0, 3.75, s=.8, rx=-12) if False else e.p("e:Banniere", -3.0, .02, 0, 4.6).p("e:Pupitre", 2.9, 2.4, -50).p("e:Papiers_Epars", -1.4, 2.0, 0, op="D", s=.6)
    e.p("e:Cheminee", 0, .02).p("e:Tapis_4x3", 0, 3.2, 0, s=.9).p("P_Prop_Fauteuil", -1.7, 3.2, 140).p("P_Prop_Fauteuil", 1.8, 3.1, -135).p("e:Gueridon", -.1, 3.5, 0, s=.8).p("e:Livres_Table", -.05, 3.5, 30, y=.62, op="D", s=.55)
    e.p("P_Prop_CoffreDeco", -2.8, .7, 8).p("e:Horloge", 2.8, .02).p("e:Candelabre_Table", -1.5, .75, 0, y=2.9, s=.9).p("e:Candelabre_Table", 1.5, .75, 0, y=2.9, s=.9)
    e.F(0, .62, .66, 2.6).F(-.45, .6, .66, 2.0).F(.48, .6, .66, 2.0).L(0, 1.0, .85, TORCHE, 1.8, 9).L(0, 1.2, 1.9, TORCHE, 1.0, 10).L(0, 3.4, 1.2, BOUGIE, .8, 5)
    e.A("Accroche_Manteau", 0, 2.9, .9)

    e = Ens("Chateau_Cheminee_B", "Chateau", "8 × 4.8 m", "8.3 m (conduit)", "cheminée éteinte : fond de salle froid, couverts", "Le foyer est mort ; on a brûlé les livres, puis les cadres, puis les chaises.", accroches="Manteau à 2.9 m (`Accroche_Manteau`).", passage="Voie libre devant.", reprend="Chateau_Chaise_Renversee, P_Prop_Fauteuil, P_Prop_Tonneau.")
    e.p("e:Cheminee_Froide", 0, .02).p("e:Livres_Sol", -.2, 1.9, 0, op="D").p("e:Livres_Sol", 1.3, 2.4, 100, op="D").p("e:Papiers_Epars", .2, 2.6, 0, op="D").p("e:Portrait_Lacere_2", -3.6, .02, 0, 3.0, rz=6).p("Chateau_Tenture", 4.7, .0, 0, 4.8, s="0.4,0.5,0.8")
    e.p("P_Prop_Fauteuil", -2.0, 3.0, 130).p("P_Prop_Fauteuil", 2.2, 3.8, -100, y=.45, rx=86).p("P_Prop_Chaise", -.2, 3.8, 40, y=.24, rx=88, s=1.15).p("e:Drap_Fauteuil", -3.0, 1.6, 30, s=.95).p("e:Horloge", 3.5, .1, 0).p("e:Verre_Brise", 2.5, 1.9, 0, op="D").p("e:Gravats_Epars_B", .0, 1.6, 0, op="D").p("Decal_ToileAraignee", -2.0, .1, 0, 4.2)
    e.bougies(-1.2, .8, 2.9, True).bougies(1.0, .8, 2.9, False).L(0, 2.6, 2.4, LUNE, 1.4, 9)
    e.A("Accroche_Manteau", 0, 2.9, .9)

    # ---------------------------------------------------------------- chambre
    e = Ens("Chateau_Chambre_A", "Chateau", "11 × 4.8 m", "4.2 m", "chambre seigneuriale contre un mur", "Lit refait pour personne, la tapisserie montre la bête qui chasse les chasseurs.",
            accroches="Dais à 3.1 m (`Accroche_Dais`).", passage="Voie libre devant.", reprend="P_Prop_Armoire, P_Prop_CoffreDeco, Chateau_Applique_Miroir ; meubles d'Act II de main (chambre).")
    e.p("Chateau_Tenture", .5, .0, 0, 4.8, s="0.35,0.52,0.8").p("e:Portrait_2", -3.6, .02, 0, 3.6, s=.75).p("e:Livres_Sol", 1.2, 2.2, 0, op="D", s=.7)
    e.p("e:Lit_Baldaquin", -1.2, .02).p("e:Chevet", .45, .4).bougies(.45, .4, .75).p("e:Tenture", 2.6, .02).p("P_Prop_Armoire", -3.95, .5).p("e:Coiffeuse", 5.2, .05).p("P_Prop_Chaise", 5.2, 1.5, 170, s=1.1)
    e.p("P_Prop_CoffreDeco", -1.2, 3.2).p("e:Tapis_4x3", .6, 3.3, 8).p("Chateau_Candelabre_Pied", 4.2, 1.6).p("P_Prop_Fauteuil", 3.7, 3.9, -140).p("Chateau_Applique_Miroir", -2.82, .0, 0, 2.2)
    e.L(0, 2.3, 2.6, BOUGIE, .8, 7).A("Accroche_Dais", -1.2, 3.15, 1.3)

    e = Ens("Chateau_Chambre_B", "Chateau", "9.5 × 5.2 m", "3.4 m", "chambre abandonnée : couverts, silhouette brisée", "Le dais s'est effondré sur le lit ; l'armoire est restée ouverte, le miroir terni.",
            accroches="Dais effondré (pente de 1 à 3 m).", passage="Voie libre devant.", reprend="Chateau_MiroirBrise, Chateau_Armoire_Tombee (famille), Chateau_Chaise_Renversee.")
    e.p("e:Portrait_Lacere_3", 4.4, .02, 0, 2.6, rz=7).p("e:Papiers_Epars", .6, 2.8, 0, op="D").p("e:Gravats_Epars_C", -1.6, 3.4, 0, op="D").p("Chateau_Tenture", .95, .0, 0, 4.4, s="0.35,0.46,0.8").p("e:Drap_Fauteuil", 3.9, 4.6, -120)
    e.p("e:Lit_Effondre", -.8, .02).p("e:Armoire_Ouverte", -3.5, .05).p("Chateau_MiroirBrise", 2.9, 1.0, -20).p("e:Verre_Brise", 2.7, 1.9, 0, op="D").p("e:Chevet", 1.4, 1.2, 30, y=.28, rz=86)
    e.p("P_Prop_Chaise", 2.4, 3.2, 200, y=.24, rx=88, s=1.15).p("e:Livres_Sol", -2.9, 2.9, 40, op="D", s=.8).p("P_Prop_CoffreDeco", -1.0, 4.0, 14).p("e:Candelabre_Table", .3, 4.5, 10, y=.08, rx=86, op="D")
    e.p("Decal_ToileAraignee", 1.9, .05, 0, 3.2).p("Decal_TacheHumidite", -1.0, .05, 0, 3.6).bougies(3.7, 2.4).L(-.6, 2.6, 3.4, LUNE, 1.0, 8).L(-3, 1.8, 2, BOUGIE, .6, 5)

    # ---------------------------------------------------------------- chapelle
    e = Ens("Chateau_Chapelle_A", "Chateau", "11 × 10 m", "7.2 m", "chapelle : grand repère lumineux du fond de salle",
            "La sainte à la lanterne veille encore ; les cierges brûlent, les bancs sont vides.", accroches="Bancs (0.5 et 1.1 m), autel (1.3 m), appui de la verrière à 1.5 m.",
            passage="Allée centrale de 1.6 m et côtés libres ; le traqueur enjambe les bancs.", reprend="Chateau_Chapelle (bancs, autel, lutrin, candélabres, rayon de lune) : composition reprise en réduction.")
    e.p("e:Banniere_Bleue", -2.9, .02, 0, 5.6, s=1.3).p("e:Banniere_Bleue", 2.9, .02, 0, 5.6, s=1.3).p("P_Prop_Statue", -3.6, .7, 20, s=1.1).p("P_Prop_Statue", 3.6, .7, -20, s=1.1).p("e:Papiers_Epars", -1.0, 5.0, 0, op="D", s=.6)
    e.p("e:Verriere", 0, .02).p("e:Autel_Chapelle", 0, 1.5, 0, s=.92).p("e:Chemin_Tapis_8m", 0, 6.6, 90, s=".62,1,1")
    e.p("e:Cierges", -4.7, 1.9, 35).p("e:Cierges", 4.7, 1.9, -35).p("e:Pupitre", 2.5, 3.9, -24).p("e:Prie_Dieu", -1.2, 4.1, 180)
    for z in (5.4, 7.0, 8.6):
        for sx in (-1, 1):
            e.p("e:Banc_Chapelle", sx * 2.55, z, 180, s=".98,1,1")
    e.p("Chateau_Candelabre_Pied", -1.9, 2.9, 0, .165).p("Chateau_Candelabre_Pied", 1.9, 2.9, 0, .165)
    e.L(0, 4.2, 1.6, "0.72,0.74,1", 1.7, 11).L(-3.2, 1.9, 2.2, BOUGIE, .9, 6).L(3.2, 1.9, 2.2, BOUGIE, .9, 6).L(0, 2.0, 3.2, BOUGIE, .8, 6)
    e.A("Place_Coffre", 0, 0, 4.4)

    e = Ens("Chateau_Chapelle_B", "Chateau", "8.6 × 9 m", "7.2 m", "chapelle funèbre profanée : catafalque au centre", "Une veillée interrompue : le cercueil est cloué, mais un banc a été renversé en travers de l'allée.",
            accroches="Catafalque à 1 m ; bancs.", passage="3 m libres autour du catafalque.", reprend="Chateau_Chapelle ; P_Prop_Statue, Chateau_Banc_Eglise.")
    e.p("e:Banniere_Bleue", -2.9, .02, 0, 5.6, s=1.3, rz=5).p("e:Banniere_Bleue", 2.9, .02, 0, 5.6, s=1.3).p("e:Gravats_Epars", 1.0, 6.0, 0, op="D").p("e:Papiers_Epars", -1.4, 6.0, 0, op="D")
    e.p("e:Verriere", 0, .02).p("e:Catafalque", 0, 4.0, 90).p("P_Prop_Statue", -3.0, .9, 20, s=1.1).p("P_Prop_Statue", 3.0, .9, -20, s=1.1).p("e:Cierges", 0, 1.3, 0)
    for x, z in ((-1.7, 3.0), (1.7, 3.0), (-1.7, 5.2), (1.7, 5.2)):
        e.p("Chateau_Candelabre_Pied", x, z, 0, op="S")
    e.p("e:Banc_Chapelle", -2.7, 6.9, 172).p("e:Banc_Chapelle", 2.6, 7.2, 200).p("e:Banc_Chapelle", 1.2, 9.5, 24, y=.42, rx=-78).p("e:Prie_Dieu", 2.6, 4.4, -80).p("e:Livres_Sol", -3.0, 5.0, 10, op="D", s=.7)
    e.p("e:Verre_Brise", 1.1, 1.3, 0, op="D").p("Decal_ToileAraignee", 2.4, .05, 0, 3.6)
    e.L(0, 4.2, 1.6, "0.72,0.74,1", 1.7, 11).L(0, 2.2, 4.2, BOUGIE, 1.1, 8).L(0, 1.8, 1.9, BOUGIE, .7, 5)

    # ---------------------------------------------------------------- galerie de portraits
    e = Ens("Chateau_Galerie_A", "Chateau", "13.5 × 2.6 m", "4.6 m", "galerie de portraits : habille un long mur", "Sept ancêtres entourent celui qui n'a pas de visage ; un cadre vide attend déjà le suivant.",
            accroches="Aucune (décor mural) ; banquettes 0.55 m.", passage="Profondeur 1.3 m : couloir libre devant.", reprend="Chateau_GrandHall (tableaux, tentures), Chateau_Applique_Miroir, Chateau_Torchere.")
    for x, n, y in ((-5.0, "1", 2.55), (-3.35, "2", 2.5), (3.35, "4", 2.5), (5.0, "7", 2.55), (-3.4, "6", 4.4), (3.4, "3", 4.5)):
        e.p("e:Portrait_" + n, x, .02, 0, y, s=.95)
    e.p("e:Portrait_Grand", 0, .02, 0, 3.0).p("e:Portrait_5", -1.95, .02, 0, 2.6, s=.72).p("e:Cadre_Vide", 1.95, .02, 0, 2.6, s=.72)
    for x in (-6.35, -1.95, 1.95, 6.35):
        e.p("Chateau_Applique_Miroir", x, 0, 0, 2.4 if abs(x) > 4 else 4.1)
    e.p("e:Banquette", -3.4, .62).p("e:Banquette", 3.4, .62).p("e:Gueridon_Urne", -5.6, .55).p("e:Gueridon_Urne", 5.6, .55).p("e:Gueridon_Urne", 0, .55, 0, s=.9).p("e:Chemin_Tapis_8m", 0, 1.9, 0, s="1.45,1,0.9")
    e.L(0, 3.0, 2.0, BOUGIE, 1.1, 8).L(-4.2, 2.6, 1.9, BOUGIE, .8, 6).L(4.2, 2.6, 1.9, BOUGIE, .8, 6)

    e = Ens("Chateau_Galerie_B", "Chateau", "12 × 2.8 m", "4.4 m", "galerie de portraits lacérée : habille un long mur", "Quelqu'un a lacéré les visages, un par un ; le portrait sans visage, lui, est intact.",
            accroches="Aucune ; banquette.", passage="Profondeur 1.5 m.", reprend="Chateau_GrandHall, Chateau_Torchere, Chateau_Armoire_Tombee (famille).")
    e.p("e:Papiers_Epars", -.6, 1.9, 0, op="D")
    e.p("e:Portrait_Lacere", -4.9, .02, 0, 2.6, rz=-5).p("e:Portrait_Lacere_2", -2.9, .02, 0, 2.5, rz=4).p("e:Portrait_Grand", 0, .02, 0, 2.95, rz=-3).p("e:Cadre_Vide", 2.5, .02, 0, 2.6).p("e:Portrait_Lacere_4", 4.7, .02, 0, 2.7)
    e.p("e:Portrait_Lacere_3", 2.6, .55, 10, .95, rx=-16).p("e:Portrait_Lacere_4", -1.6, .95, 20, .1, rx=-82).p("e:Portrait_Lacere_5", -3.9, .02, 0, 4.45, s=.8, rz=2)
    e.p("e:Banquette", -3.6, .9, 8).p("e:Gueridon_Urne", 5.6, .55).p("e:Gueridon", 1.3, 1.3, 40, y=.4, rz=84).p("e:Verre_Brise", 1.6, 1.8, 0, op="D").p("e:Drap_Statue", -5.6, .95, 20)
    e.p("Chateau_Torchere", 4.05, .6).p("Chateau_Applique_Miroir", -1.6, 0, 0, 3.9).p("Decal_FissureLongue", 1.4, .05, 0, 3.6).p("Decal_CoulureLongue", 3.6, .05, 0, 3.4)
    e.L(-3.6, 2.6, 2.0, LUNE, .9, 7).L(0, 3.0, 2.0, BOUGIE, .8, 7).L(3.6, 2.4, 1.8, BOUGIE, .8, 6)

    # ---------------------------------------------------------------- salon renversé
    e = Ens("Chateau_Salon_A", "Chateau", "8.5 × 6 m", "2.2 m", "salon sous housses, îlot : couverts et silhouettes",
            "Maison fermée : les meubles dorment sous les draps, et l'un des draps ressemble à quelqu'un.", adosse=False, accroches="Clavecin (1.1 m), table (0.9 m).",
            passage="Meubles espacés de 1.5 à 2 m : le joueur zigzague ; traqueur : tout est enjambé.", reprend="Chateau_SalleDeBal (meubles autour de la piste vide), Chateau_Candelabre_Pied.")
    e.p("e:Papiers_Epars", -1.6, 1.4, 0, op="D")
    e.p("e:Clavecin", -2.0, -.6, 30).p("e:Drap_Fauteuil", 1.4, -1.6, -30).p("e:Drap_Fauteuil", 3.2, .2, -100).p("e:Drap_Table", .9, 1.3, 15).p("e:Drap_Statue", -3.5, 1.8, 40).p("e:Gueridon", -.75, 2.8).bougies(-.75, 2.8, .77)
    e.p("Cave_Tabouret", -1.15, 1.0, 10, s=1.05).p("e:Tapis_4x3", .7, 0, 12, y=.0).p("Chateau_Candelabre_Pied", -3.6, -2.0).p("e:Horloge", 3.4, -2.2, -30).p("e:Livres_Sol", 3.2, 2.0, 70, op="D", s=.7)
    e.L(-.6, 1.7, 2.0, BOUGIE, 1.0, 7).L(1.8, 3.0, -.6, LUNE, .9, 8)

    e = Ens("Chateau_Salon_B", "Chateau", "8.5 × 6 m", "2 m", "salon renversé, îlot : couverts bas et barricade",
            "On s'est barricadé ici, derrière les meubles ; la barricade a cédé.", adosse=False, accroches="Meubles couchés (0.7 à 1.2 m) : marches.",
            passage="Brèche de 2 m au milieu de la barricade.", reprend="Chateau_SalleDeBal : Chateau_Table_Renversee, Chaise_Renversee, Armoire_Tombee.")
    for x, z, ry in ((-2.7, -1.5, 8), (2.6, -1.4, -10)):
        e.p("Chateau_Table_Renversee", x, z, ry, s=.9)
    e.p("Chateau_Armoire_Tombee", 3.2, -3.7, 95, s=.8).p("P_Prop_Etagere", -3.6, -2.7, 14, y=.42, rx=-86, s=.9).p("P_Prop_Fauteuil", -1.9, -2.6, 160, y=.45, rx=84).p("P_Prop_Fauteuil", -3.4, 1.4, 60)
    e.p("e:Horloge", 2.9, .7, 100, y=.26, rx=-84).p("e:Lustre_Tombe", .2, 2.0, 30, s=.9).p("P_Prop_Chaise", -2.7, .5, 30, y=.24, rx=88, s=1.15).p("P_Prop_Chaise", 2.7, 3.0, 260, y=.24, rx=88, s=1.15).p("P_Prop_Chaise", -1.7, 4.0, 100, y=.24, rx=88, s=1.15)
    e.p("e:Banquette", -3.0, 3.0, 24).p("e:Gueridon", 3.6, 2.0, 20, y=.4, rz=84).p("e:Livres_Sol", -2.2, -.3, 40, op="D").p("e:Portrait_Lacere_3", -2.6, -1.15, 12, .95, rx=-14).p("e:Verre_Brise", .4, .4, 0, op="D").p("e:Papiers_Epars", -.4, 1.0, 0, op="D").p("e:Gravats_Epars_B", 1.2, .2, 0, op="D").p("e:Vaisselle_B", 1.7, 4.0, 0, op="D")
    e.bougies(-3.0, 2.4).p("Decal_TacheSang", .0, .2, 0, .02, rx=90, s=1.4).L(0, 2.6, -2.8, LUNE, 1.4, 8).L(0, 2.4, .6, BOUGIE, 1.3, 8).L(-2.6, 1.5, 1.6, BOUGIE, .9, 6)



# ==================================================================================== ACTE III
def jardin():
    def touffes(e, n, rx, rz, r0=0.0, sortes=("Herbes_A", "Herbes_A", "Fougeres", "Ronces")):
        for k in range(n):
            for _ in range(20):
                x, z = e.r.uniform(-rx, rx), e.r.uniform(-rz, rz)
                if math.hypot(x / rx, z / rz) > r0:
                    break
            e.p("e:" + e.r.choice(sortes), x, z, e.r.uniform(0, 360), s=round(e.r.uniform(.7, 1.1), 2), op="D")

    def haie(e, x, z, ry=0):
        e.p("Jardin_Haie_6m", x, z, ry).p("e:Habillage_Haie_6m", x, z, ry)

    e = Ens("Jardin_Massif_A", "Jardin", "15 × 10 m", "4.6 m", "massif de haies autour d'une statue, îlot en U : niche et repère",
            "L'ange pleure au fond de sa niche de buis ; on vient encore lui porter des lanternes.", adosse=False, accroches="Socle à 1.3 m ; haies de 2.5 m (dessus praticable).",
            passage="U ouvert de 7 m : jamais de cul-de-sac fermé ; le traqueur enjambe les haies.", reprend="Jardin_Labyrinthe et Jardin_AlleeDesStatues : haies du kit (habillées de buis peint), chemin.")
    haie(e, -3.4, -3.6, 0)
    haie(e, 3.4, -3.6, 0)
    haie(e, -6.2, -.4, 90)
    haie(e, 6.2, -.4, 90)
    e.p("e:Socle_Statue", 0, -1.2).p("P_Prop_AngePleureur", 0, -1.2, 0, 1.3, s=1.25).p("e:Topiaire", -3.7, 2.9).p("e:Topiaire", 3.7, 2.9).p("e:Buis_Boule", -3.6, -1.6).p("e:Buis_Boule", 3.6, -1.7, 90)
    e.p("e:Rosiers", -2.0, -.6, 20).p("e:Rosiers", 2.1, -.7, 160).p("e:Lanterne_Pierre", -2.1, 1.8).p("e:Lanterne_Pierre", 2.1, 1.8).p("e:Banc_Pierre", -4.4, .9, 90).p("Jardin_Chemin_8m", 0, 4.0, 90, s=".7,1,0.8")
    touffes(e, 7, 6, 4, .5)
    e.L(-2.1, 1.5, 2.1, TORCHE, 1.0, 7).L(2.1, 1.5, 2.1, TORCHE, 1.0, 7).L(0, 4.6, 1.6, LUNE, 1.1, 8)
    e.A("Accroche_Socle", 0, 1.3, -1.2)

    e = Ens("Jardin_Massif_B", "Jardin", "13 × 8 m", "5 m", "massif retourné à l'état sauvage, îlot : couverts", "Le buis a tout mangé ; le moine de pierre prie toujours, et quelqu'un lui laisse une lanterne.",
            adosse=False, accroches="Haies de 2.5 m.", passage="Deux passages de 4 m.", reprend="Jardin_Labyrinthe, Jardin_AlleeDesStatues : haies du kit, P_Prop_Statue.")
    haie(e, -3.6, -2.6, 14)
    haie(e, 4.2, -1.6, -32)
    e.p("e:Buis_Boule", -4.8, 1.9, 0, s=1.2).p("e:Buis_Boule", -5.9, .4, 70, s=1.15).p("e:Buis_Boule", -3.7, 1.0, 140, s=1.05).p("e:Socle_Statue", 0, -.4, 10).p("P_Prop_Statue", 0, -.4, 10, 1.3, s=1.25).p("e:Lierre_Coulee", .9, 1.0, 0, 0, op="D", s=1.2)
    e.p("e:Ronces", 1.9, .4, 0, s=1.3).p("e:Ronces", -1.7, .9, 80, s=1.2).p("e:Rosiers", .4, 1.6, 0).p("e:Banc_Pierre_Brise", 2.6, 2.6, 200).p("e:Urne_Pierre", -2.6, 2.9, 0).p("e:Topiaire", 5.0, 2.2, 0).p("e:Corbeau", -2.6, 2.9, 150, 1.5)
    touffes(e, 9, 5.5, 3.6, .3)
    e.L(0, 4.0, 2.6, LUNE, 1.2, 9).p("P_Prop_Lanterne", 2.0, 2.4, 0, .02, s=.58)

    # ---------------------------------------------------------------- fontaine
    e = Ens("Jardin_FontaineBancs_A", "Jardin", "Ø 13 m", "5.4 m", "place de fontaine, îlot : carrefour et repère", "L'eau noire ne coule plus ; les bancs l'entourent comme pour un spectacle.",
            adosse=False, accroches="Vasque haute à 3.5 m, margelle 0.6 m (`Accroche_Vasque`).", passage="Anneau libre de 3 m autour du bassin.", reprend="Jardin_AlleeDesStatues (fontaine, allée, haies).")
    e.p("Jardin_Fontaine", 0, 0).p("e:Eau_Disque", 0, 0, y=.36, s="3.35,1,3.35").p("e:Nenuphars", 1.6, 1.6, 0, .375, op="D").p("e:Nenuphars", -1.9, -.8, 100, .375, op="D")
    for k, a in enumerate((40, 140, 220, 320)):
        x, z = math.cos(math.radians(a)) * 6.0, math.sin(math.radians(a)) * 6.0
        e.p("e:Banc_Pierre", x, z, math.degrees(math.atan2(-x, -z)))
    for a in (0, 90, 180, 270):
        x, z = math.cos(math.radians(a)) * 5.9, math.sin(math.radians(a)) * 5.9
        e.p("e:Lanterne_Pierre" if a % 180 else "e:Urne_Pierre", x, z)
    touffes(e, 8, 6.3, 6.3, .85)
    e.L(0, 1.5, 5.4, TORCHE, 1.1, 8).L(0, 1.5, -5.4, TORCHE, 1.1, 8).L(0, 4.8, 0, LUNE, 1.1, 8)
    e.A("Accroche_Vasque", 0, 3.5, 0)

    e = Ens("Jardin_FontaineBancs_B", "Jardin", "Ø 12 m", "5.4 m", "fontaine envahie, îlot : carrefour", "Le lierre a pris la fontaine ; un corbeau garde le dernier banc entier.", adosse=False,
            accroches="Vasque haute à 3.5 m.", passage="Anneau libre de 2.5 m.", reprend="Jardin_AlleeDesStatues (fontaine, statues).")
    e.p("Jardin_Fontaine", 0, 0, 22).p("e:Eau_Disque", 0, 0, y=.28, s="3.35,1,3.35").p("e:Nenuphars", -1.4, 1.8, 0, .3, op="D").p("e:Lierre_Coulee", 2.6, 2.2, 40, .6, s=1.6, op="D").p("e:Lierre_Coulee", -2.7, 2.0, 40, .6, s=1.6, op="D")
    e.p("e:Banc_Pierre_Brise", 4.4, 3.9, 225).p("e:Banc_Pierre", -4.6, 3.8, 135).p("e:Corbeau", -4.4, 3.9, 170, 1.18).p("e:Urne_Pierre", 5.4, -1.2, 0).p("e:Urne_Pierre", -5.2, -2.0, 0, y=.4, rz=82).p("e:Ronces", 3.6, -3.8, 0, s=1.4).p("e:Ronces", -3.4, -4.0, 0, s=1.5)
    e.p("Jardin_SousBois_A", -5.0, .8, 60, op="D").p("e:Buis_Boule", 4.8, 1.6, 0, s=1.1)
    touffes(e, 10, 5.8, 5.8, .8)
    e.L(0, 4.6, 2.0, LUNE, 1.3, 10).p("P_Prop_Lanterne", 4.0, 3.6, 0, .5, s=.58)

    # ---------------------------------------------------------------- serre
    e = Ens("Jardin_Serre_A", "Jardin", "11 × 7 m", "5.3 m", "serre en ruine, îlot à enjamber : couverts et muret", "Table de rempotage encore servie ; les plantes, elles, ont continué.",
            adosse=False, accroches="Faîtage à 5 m (`Accroche_Faitage`).", passage="Muret de 0.8 m sur quatre côtés : on entre par un saut ; le traqueur l'enjambe (toit ouvert).", reprend="Jardin_AlleeDesStatues (serre du kit).")
    e.p("Jardin_Serre_Ruine", 0, 0, op="V").p("e:Serre_Vitres", 0, 0, s="1.6,1,1.4", op="D").p("e:Table_Rempotage", -2.6, -1.9, 0).p("e:Pots_Tas", .9, -1.9, 20).p("e:Pots_Tas", 3.6, 1.8, 200).p("e:Plante_Geante", 3.4, -1.6, 0, s=.8).p("e:Brouette", -3.2, 1.6, 20)
    e.p("e:Urne_Pierre", -6.0, 4.0).p("e:Urne_Pierre", 6.0, 4.0).p("e:Habillage_Lierre_4m", 3.2, 3.05, 0, s=.9).p("P_Prop_Lanterne", -2.0, -1.8, 0, .92, s=.58).p("e:Fougeres", 1.0, 1.9, 0, op="D").p("e:Ronces", -4.6, -2.3, 0)
    touffes(e, 6, 5.4, 3.6, .85)
    e.L(-2.2, 2.0, -1.0, TORCHE, 1.0, 7).L(2.4, 3.4, .5, LUNE, 1.2, 9).A("Accroche_Faitage", 0, 5.0, 0)

    e = Ens("Jardin_Serre_B", "Jardin", "11.5 × 7.5 m", "5.3 m", "serre dévorée, îlot à enjamber : jungle dense, couverts", "Ce qui poussait ici a mangé le reste, vitres comprises.", adosse=False, accroches="Faîtage à 5 m.", passage="Muret de 0.8 m sur quatre côtés : on entre par un saut ; le traqueur l'enjambe.", reprend="Jardin_AlleeDesStatues (serre du kit), Jardin_SousBois_A et B.")
    e.p("Jardin_Serre_Ruine", 0, 0, op="V").p("e:Serre_Vitres", 0, 0, 180, s="1.6,1,1.4", op="D").p("e:Plante_Geante", 0, -1.4, 40, s=1.1).p("e:Plante_Geante", -3.4, 1.2, 100, s=.9).p("e:Plante_Geante", 3.6, .9, 200, s=.85)
    e.p("e:Habillage_Lierre_4m", -3.2, 3.05, 0, s=.95).p("e:Habillage_Lierre_4m", 1.6, -3.05, 180, s=.95).p("e:Pots_Tas", 2.4, 2.3, 100).p("Jardin_SousBois_A", -4.4, -1.8, 0, op="D").p("Jardin_SousBois_B", 1.6, 1.8, 0, op="D").p("e:Fougeres", -1.6, 1.9, 0, op="D").p("e:Ronces", 4.4, 2.6, 0, s=1.2).p("e:Squelette_Assis", -1.7, -2.5, 0)
    touffes(e, 8, 5.4, 3.6, .7)
    e.L(0, 2.2, .4, "0.6,0.95,0.6", .9, 7).L(0, 4.4, 2.2, LUNE, 1.2, 9)

    # ---------------------------------------------------------------- cimetière
    e = Ens("Jardin_Cimetiere_A", "Jardin", "12 × 9 m", "4.5 m", "petit cimetière clos bas, îlot : couverts réguliers", "Le carré de la famille ; l'ange est plus récent que les tombes.", adosse=False,
            accroches="Tombes (1 à 2 m) : marches ; clôture basse 1.3 m.", passage="Entrée de 4 m ouverte devant ; clôture sautée sans élan.", reprend="Jardin_AlleeDesStatues (cimetière : tombes, portail).")
    for x in (-4.5, -1.5, 1.5, 4.5):
        e.p("e:Grille_Cimetiere", x, -4.2)
    for z in (-2.7, .3):
        e.p("e:Grille_Cimetiere", -6.0, z, 90).p("e:Grille_Cimetiere", 6.0, z, 90)
    e.p("e:Grille_Cimetiere", -4.0, 3.6, 4).p("e:Grille_Cimetiere", 4.1, 3.6, -3)
    e.p("e:Socle_Statue", 0, -2.4, 0, s=.75).p("P_Prop_AngePleureur", 0, -2.4, 0, .97, s=1.0)
    for i, (x, z, pf, r) in enumerate(((-4.3, -2.6, "Jardin_Tombe_Croix", 4), (-2.5, -2.4, "e:Tombe_Penchee", -6), (2.5, -2.6, "Jardin_Tombe_Stele", 5), (4.4, -2.4, "e:Tombe_Penchee", 10), (-4.4, 1.0, "Jardin_Tombe_Stele", -5), (-2.2, .9, "e:Croix_Bois", 8), (2.4, .8, "Jardin_Tombe_Croix", -8), (4.5, 1.2, "e:Tombe_Penchee", -12))):
        e.p(pf, x, z, r)
    e.p("e:Lanterne_Pierre", -2.0, 3.6).p("e:Lanterne_Pierre", 2.1, 3.6).p("e:Corbeau", 2.4, .8, 200, 2.2).p("e:Rosiers", 1.2, -1.2, 0, op="D", s=.8).bougies(0, -1.0, .0, False)
    touffes(e, 8, 5.4, 3.6, .2)
    e.L(-2, 1.4, 3.3, TORCHE, 1.0, 7).L(2.1, 1.4, 3.3, TORCHE, 1.0, 7).L(0, 4.2, -.6, LUNE, 1.1, 8)

    e = Ens("Jardin_Cimetiere_B", "Jardin", "11 × 8 m", "5.6 m", "cimetière ouvert par en dessous, îlot", "Une tombe s'est ouverte de l'intérieur ; les corbeaux attendent.", adosse=False, accroches="Caveau (dessus à 5 m), dalles.",
            passage="Pas de trou dans le sol (fosse peinte) ; passages de 2 m.", reprend="Jardin_AlleeDesStatues (caveau, tombes, arbre mort).")
    e.p("Jardin_Caveau", -3.2, -3.0, 10).p("e:Tombe_Ouverte", 1.2, .2, -10).p("Jardin_Tombe_Croix", 4.4, -1.6, -20).p("e:Tombe_Penchee", 3.0, -3.0, 6).p("e:Croix_Bois", -1.2, 2.0, 14).p("e:Croix_Bois", -2.2, 3.3, -20).p("Jardin_Tombe_Stele", 4.6, 1.9, -8).p("P_Prop_Sarcophage", -4.0, 2.2, 60)
    e.p("e:Grille_Cimetiere", 4.9, -3.9, -14).p("e:Corbeau", 1.2, -.2, 160, 1.3).p("e:Corbeau", 4.45, -1.6, 120, 2.2).p("e:Os_Tas", 3.3, 2.7, 0, op="D").p("P_Prop_Lanterne", -.3, 3.1, 0)
    e.p("e:Ronces", 3.2, 3.4, 0)
    touffes(e, 9, 5.2, 4, .3)
    e.L(0, 3.8, 2.2, LUNE, 1.2, 9).L(-2.4, 1.4, 1.6, TORCHE, .7, 5)

    # ---------------------------------------------------------------- kiosque
    e = Ens("Jardin_KiosqueEns_A", "Jardin", "Ø 10 m", "9.2 m", "kiosque de jardin, îlot : estrade haute et repère", "Le thé est servi depuis un siècle.", adosse=False, accroches="Plancher à 1 m, toit (`Accroche_Toit` 5 m).", passage="Ouvert sur tous les côtés ; marches devant.",
            reprend="Jardin_Labyrinthe, Jardin_AlleeDesStatues (kiosque du kit).")
    e.p("Jardin_Kiosque", 0, 0).p("e:Habillage_Kiosque", 0, 0, s=1.38).p("e:Table_Ronde_Fer", .2, -.2, 30, 1.0).p("P_Prop_Lanterne", .25, -.1, 0, 1.75, s=.58).p("e:Buis_Boule", -4.6, 3.4).p("e:Buis_Boule", 4.6, 3.4).p("e:Lanterne_Pierre", -2.3, 5.4).p("e:Lanterne_Pierre", 2.3, 5.4).p("e:Rosiers", 4.6, -1.0, 0).p("e:Rosiers", -4.7, -.6, 0)
    touffes(e, 8, 5.2, 5.2, .9)
    e.L(0, 3.0, 0, TORCHE, 1.3, 8).L(-2.3, 1.4, 5.2, TORCHE, .8, 5).L(2.3, 1.4, 5.2, TORCHE, .8, 5).A("Accroche_Toit", 0, 5.0, 3.4)

    e = Ens("Jardin_KiosqueEns_B", "Jardin", "Ø 10 m", "9.2 m", "kiosque à musique abandonné, îlot", "Les pupitres sont encore tournés vers un chef qui ne reviendra pas.", adosse=False, accroches="Plancher à 1 m, toit.", passage="Ouvert sur tous les côtés.", reprend="Jardin_Labyrinthe (kiosque du kit), Jardin_SousBois_A.")
    e.p("Jardin_Kiosque", 0, 0, 12).p("e:Habillage_Kiosque", 0, 0, 147, s=1.38).p("e:Pupitres_Musique", 0, 0, 20, 1.0).p("P_Prop_Chaise", -2.3, -1.2, 40, 1.0, s=1.1).p("P_Prop_Chaise", 2.2, -1.7, -60, 1.24, s=1.1, rx=88).p("e:Corbeau", 1.5, 2.0, 30, 1.0).bougies(-2.0, 1.4, 1.0)
    e.p("Jardin_SousBois_A", -4.9, 2.4, 0, op="D").p("e:Ronces", 4.6, 2.6, 0, s=1.3).p("e:Banc_Pierre_Brise", 4.2, 4.2, 210).p("e:Urne_Pierre", -3.6, 4.6, 0)
    touffes(e, 10, 5.2, 5.2, .85)
    e.L(0, 3.0, 0, LUNE, 1.2, 8).L(0, 4.2, 4.6, LUNE, .8, 7)

    # ---------------------------------------------------------------- tas de bois
    e = Ens("Jardin_TasBois_A", "Jardin", "7.5 × 4.6 m", "3.1 m", "coin du jardinier contre un mur ou une haie", "La hache est encore plantée ; le bois n'a jamais été rentré.", accroches="Toit de l'appentis (2.4 à 3 m) ; tas de bois 1.5 m.", passage="Voie libre devant.", reprend="Jardin_Palissade_4m, P_Prop_Lanterne, P_Prop_Tonneau.")
    e.p("e:Appentis", -1.4, .1).p("e:Tas_Bois", 2.3, .95, 0).p("e:Billot_Hache", 1.6, 3.3, 20).p("e:Brouette", -3.2, 3.3, 130).p("e:Outils_Jardin", -1.2, .28, 0).p("e:Pots_Tas", -2.4, 1.2, 0).p("P_Prop_Tonneau", -.1, 1.35, 30).p("P_Prop_Lanterne", -.1, 1.35, 0, .9, s=.58).p("e:Sacs_A", -1.2, 1.9, 160, s=.8)
    for x, z in ((3.6, 2.2), (-3.7, 1.0), (.4, 4.2), (3.2, 4.0)):
        e.p("e:Herbes_A", x, z, e.r.uniform(0, 360), s=.8, op="D")
    e.L(-.1, 1.7, 1.8, TORCHE, 1.2, 8).L(2.4, 3.0, 3.0, LUNE, .9, 8)
    e.A("Pose_Attrapable", .9, .4, 2.4)

    e = Ens("Jardin_TasBois_B", "Jardin", "8 × 4.8 m", "2.2 m", "tas de bois écroulé et brouette, contre une palissade", "Le jardinier est parti sans ses outils ; sa faux, elle, manque.", accroches="Tas de bois 1.5 m, souche 0.8 m.", passage="Voie libre devant.", reprend="Jardin_Palissade_4m, P_Prop_Poutre, P_Prop_Tonneau.")
    e.p("Jardin_Palissade_4m", -2.1, .15).p("Jardin_Palissade_4m", 2.1, .15, 0).p("e:Tas_Bois_Ecroule", -2.4, 1.0, 4).p("e:Billot_Hache", .7, 2.6, 80).p("e:Souche", 3.0, 2.4, 0).p("e:Brouette", -.9, 4.3, 200).p("P_Prop_Tonneau", .7, .8, 0).p("P_Prop_Tonneau", 1.5, .75, 80, y=.3, rx=90).p("e:Outils_Sans_Faux", 2.9, .4, 0, s=.95)
    e.p("e:Ronces", 3.6, .9, 0).p("e:Herbes_A", -3.6, 2.0, 0, op="D").p("e:Fougeres", .2, 1.0, 0, op="D", s=.8).p("e:Herbes_A", 2.4, 4.2, 30, op="D").p("P_Prop_Lanterne", .7, .8, 0, .9, s=.58).p("e:Corbeau", 3.0, 2.4, 200, .67)
    e.L(1.4, 1.7, 1.4, TORCHE, 1.0, 7).L(-2, 3.0, 3.2, LUNE, 1.0, 8)

    # ---------------------------------------------------------------- clairière
    e = Ens("Jardin_Clairiere_A", "Jardin", "Ø 30 m (clairière libre Ø 10 m)", "12 m", "clairière de forêt dense, îlot : arène naturelle", "Un cercle de champignons pâles, des pierres levées : on a dansé ici, et pas seul.", adosse=False,
            accroches="Tronc couché (0.9 m, 7 m de long) ; pierres levées.", passage="Couronne d'arbres espacés de 3.5 à 5 m, ouverte de 8 m devant et de 5 m derrière : le traqueur passe entre les troncs.", reprend="Jardin_SousBois_A/B, Jardin_ArbreMort_Tordu ; lisière du kit à mettre derrière.")
    for k, (a, r, pf, s) in enumerate(((20, 9.5, "Arbre_Chene_A", 1.0), (52, 10, "Sapin", .95), (118, 10.2, "Sapin", 1.05), (150, 9.2, "Arbre_Chene_B", 1.0), (178, 10, "Sapin", .9), (208, 9.4, "Arbre_Chene_A", .95), (236, 10.2, "Sapin", 1.1), (304, 10, "Arbre_Chene_B", .95), (330, 9.4, "Sapin", 1.0), (352, 10.4, "Arbre_Chene_A", .9))):
        e.p("e:" + pf, math.cos(math.radians(a)) * r, -math.sin(math.radians(a)) * r, a * 3, s=s)
    e.p("e:Champignons_Cercle", 0, 0).p("e:Pierre_Levee_A", -3.6, -3.4, 46).p("e:Pierre_Levee_B", 3.9, -3.2, -50).p("e:Pierre_Levee_A", 5.0, 1.0, -100, s=.8).p("e:Tronc_Couche", -5.4, 2.6, 60).p("e:Souche", 4.4, 4.4, 0)
    for k in range(12):
        a = k * 30 + 10
        if 60 < a < 110 or 250 < a < 290:
            continue
        e.p("e:" + ("Fougeres", "Herbes_A", "Ronces")[k % 3], math.cos(math.radians(a)) * 7.3, -math.sin(math.radians(a)) * 7.3, k * 70, s=1.1, op="D")
    e.p("Jardin_SousBois_A", -7.6, -2.4, 0, op="D").p("Jardin_SousBois_B", 7.4, -1.4, 80, op="D")
    for k in range(13):
        a = 8 + k * 27.7 + e.r.uniform(-5, 5)
        if 80 < a < 100 or 262 < a < 290:
            continue
        rr = e.r.uniform(12.4, 14)
        e.p("e:" + ("Sapin", "Arbre_Chene_A", "Sapin", "Arbre_Chene_B")[k % 4], math.cos(math.radians(a)) * rr, -math.sin(math.radians(a)) * rr, k * 77, s=round(e.r.uniform(.9, 1.2), 2))
    for k in range(16):
        a = 12 + k * 22.5
        if 62 < a < 115 or 258 < a < 292:
            continue
        e.p("e:" + ("Fougeres", "Ronces", "Buis_Boule", "Herbes_A")[k % 4], math.cos(math.radians(a)) * 10.6, -math.sin(math.radians(a)) * 10.6, k * 50, s=round(e.r.uniform(1.1, 1.6), 2), op="D")
    e.L(0, 1.2, 0, "0.75,0.9,0.75", 1.8, 8).L(0, 6, 2, LUNE, 2.6, 22).L(-6, 5, 7, LUNE, 1.4, 13).L(6, 5, -6, FROID, 1.4, 13)

    e = Ens("Jardin_Clairiere_B", "Jardin", "Ø 30 m (libre Ø 8 m)", "12 m", "clairière à la souche, îlot : couverts naturels", "Un arbre énorme est tombé ; sa souche sert d'autel.", adosse=False, accroches="Troncs couchés (0.9 m), souche.", passage="Couronne ouverte de 8 m devant, 6 m derrière.", reprend="Jardin_SousBois_A/B, Jardin_ArbreMort_Penche.")
    for k, (a, r, pf, s) in enumerate(((10, 9.0, "Sapin", 1.0), (42, 9.4, "Arbre_Chene_B", 1.0), (64, 9.8, "Sapin", .85), (124, 9.6, "Arbre_Chene_A", .95), (156, 9.0, "Sapin", 1.1), (186, 9.6, "Sapin", .95), (214, 9.0, "Arbre_Chene_B", 1.0), (318, 9.6, "Arbre_Chene_A", 1.0), (344, 9.2, "Sapin", .9))):
        e.p("e:" + pf, math.cos(math.radians(a)) * r, -math.sin(math.radians(a)) * r, a * 5, s=s)
    e.p("e:Souche", 0, -.4, 0, s=1.5).bougies(.1, -.3, 1.01).bougies(-.5, -.5, 1.01, False).p("e:Crane", .4, -.2, 20, 1.01, op="D").p("e:Tronc_Couche", 4.2, 1.2, 110).p("e:Tronc_Couche", -4.2, -2.4, 30, s=.85).p("e:Pierre_Levee_B", -2.6, -4.4, 40, s=.9).p("e:Ronces", 2.0, -3.4, 0, s=1.2)
    for k in range(11):
        a = k * 33
        if 70 < a < 115 or 250 < a < 300:
            continue
        e.p("e:" + ("Herbes_A", "Fougeres", "Ronces")[k % 3], math.cos(math.radians(a)) * 6.6, -math.sin(math.radians(a)) * 6.6, k * 50, s=1.1, op="D")
    e.p("Jardin_SousBois_B", -6.6, -1.6, 0, op="D").p("Jardin_SousBois_A", 6.4, -2.4, 0, op="D").p("e:Corbeau", -.2, -.75, 190, 1.01)
    for k in range(13):
        a = 8 + k * 27.7 + e.r.uniform(-5, 5)
        if 80 < a < 100 or 262 < a < 290:
            continue
        rr = e.r.uniform(12.4, 14)
        e.p("e:" + ("Sapin", "Arbre_Chene_A", "Sapin", "Arbre_Chene_B")[k % 4], math.cos(math.radians(a)) * rr, -math.sin(math.radians(a)) * rr, k * 77, s=round(e.r.uniform(.9, 1.2), 2))
    for k in range(16):
        a = 12 + k * 22.5
        if 62 < a < 115 or 258 < a < 292:
            continue
        e.p("e:" + ("Fougeres", "Ronces", "Buis_Boule", "Herbes_A")[k % 4], math.cos(math.radians(a)) * 10.6, -math.sin(math.radians(a)) * 10.6, k * 50, s=round(e.r.uniform(1.1, 1.6), 2), op="D")
    e.L(0, 2.2, .2, BOUGIE, 1.0, 7).L(0, 6, 2, LUNE, 2.4, 21).L(-6, 5, 7, LUNE, 1.4, 13).L(6, 5, -6, FROID, 1.4, 13)


caves()
chateau()
jardin()


# Lumière motivée (flammes) derrière ou au milieu ; une clé chaude devant, en hauteur, et un appoint froid de côté détachent les
# meubles du mur (sinon leur face avant reste noire). Aucune ombre, 3 à 6 lumières par ensemble.
CLES = {"Caves": (TORCHE, 1.5, FROID, .6), "Chateau": (BOUGIE, 1.1, LUNE, .32), "Jardin": (LUNE, 3.4, FROID, 1.8)}
for e in TOUS:
    if "Clairiere" in e.nom:
        continue
    c1, i1, c2, i2 = CLES[e.acte]
    nums = [tuple(float(v) for v in l.split("|")[2:5:2]) for l in e.l if l.startswith("P|")]
    zm = max(z for _, z in nums)
    xm = max(abs(x) for x, _ in nums)
    e.L(-.25 * xm, 2.9, zm * .75 + .6, c1, i1, 10)
    e.L(xm * .75, 3.2, zm * .6 + .4, c2, i2, 9)
    if e.acte == "Jardin":      # liseré de lune derrière : détache les silhouettes du fond noir
        e.L(.3 * xm, 4.2, min(z for _, z in nums) - 1.5, LUNE, 2.4, 11)
if __name__ == "__main__":
    open(sys.argv[1], "w", encoding="utf-8", newline="\n").write("\n---\n".join("\n".join(e.l) for e in TOUS))
    if len(sys.argv) > 2:
        json.dump([dict(e.f, pieces=sorted(set(e.pieces)), lumieres=sum(l.startswith("L|") for l in e.l)) for e in TOUS], open(sys.argv[2], "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print(len(TOUS), "ensembles")
