"""Acte III, carte 2 : « La forêt des statues ».

Intention : au-delà du parc, la forêt reprend tout. On part de la terrasse du château (nord-ouest, +8) et l'on doit traverser la
forêt jusqu'à la grande grille (sud-est). Au centre, le Rocher (plateau de 20 × 14 caractères à +8, un cimetière et une allée de
statues géantes) : on y monte par ses corniches (portes hautes) et l'on en redescend vite. Deux ravins (fosses à -8) coupent la
forêt en diagonale ; on les franchit par des ponts de 14 m, ou on les contourne. Les clairières (vergers, potagers abandonnés,
kiosques) alternent avec des bosquets denses où les arbres sont espacés d'au moins 8 m pour que le traqueur passe. Tout le pourtour
est une falaise avec une corniche haute (route du toit du monde). Le traqueur arrive par l'est.
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from carte import Salle, ecrire

S = [
    Salle("D", "Terrasse du château", [(0, 0, 18, 12)], sol=8, ciel=True, role="depart"),
    Salle("a", "Roseraie", [(18, 0, 18, 12)], sol=4, ciel=True, galerie="n 6 sans-escalier"),
    Salle("b", "Bosquet", [(36, 0, 22, 12)], sol=4, ciel=True, galerie="n 6 sans-escalier"),
    Salle("c", "Verger", [(58, 0, 26, 14)], sol=0, ciel=True, galerie="n,e 6 sans-escalier"),
    Salle("d", "Potager", [(0, 12, 14, 18)], sol=4, ciel=True, galerie="o 6 sans-escalier"),
    Salle("e", "Bosquet", [(14, 12, 18, 12)], sol=0, ciel=True),
    Salle("R", "Ravin", [(32, 12, 10, 8), (26, 20, 10, 8), (20, 28, 10, 6)], sol=-8, ciel=True, trait="vide", galerie="e 8"),
    Salle("f", "Serre en ruine", [(42, 12, 16, 12)], sol=0, ciel=True),
    Salle("g", "Cimetière", [(58, 14, 26, 12)], sol=0, ciel=True, galerie="e 6 sans-escalier"),
    Salle("h", "Verger", [(14, 24, 12, 10)], sol=0, ciel=True),
    Salle("P", "Allée des statues", [(36, 24, 22, 16)], sol=8, ciel=True, fonction="Allée des statues"),
    Salle("i", "Bosquet", [(58, 26, 14, 14)], sol=0, ciel=True),
    Salle("A", "Porte d'arrivée", [(80, 26, 4, 10)], sol=0, role="arrivee"),
    Salle("j", "Kiosque", [(72, 26, 8, 14)], sol=0, ciel=True, trait="eau", fonction="Kiosque"),
    Salle("k", "Labyrinthe de haies", [(0, 30, 20, 16)], sol=0, ciel=True, galerie="o 6 sans-escalier"),
    Salle("l", "Bosquet", [(20, 34, 16, 12)], sol=0, ciel=True),
    Salle("m", "Grand bassin", [(36, 40, 22, 14)], sol=0, ciel=True, trait="eau"),
    Salle("T", "Ravin", [(58, 40, 10, 8), (64, 48, 10, 8), (70, 56, 10, 6)], sol=-8, ciel=True, trait="vide", galerie="o 8"),
    Salle("n", "Verger", [(68, 40, 16, 8)], sol=0, ciel=True, galerie="e 6 sans-escalier"),
    Salle("o", "Potager", [(0, 46, 16, 14)], sol=-4, ciel=True, galerie="o 6 sans-escalier"),
    Salle("p", "Bosquet", [(16, 46, 20, 12)], sol=0, ciel=True),
    Salle("q", "Cimetière", [(36, 54, 18, 10)], sol=-4, ciel=True),
    Salle("r", "Serre en ruine", [(54, 48, 10, 14)], sol=0, ciel=True),
    Salle("s", "Bosquet", [(74, 48, 10, 8)], sol=0, ciel=True, galerie="e 6 sans-escalier"),
    Salle("t", "Fontaine", [(0, 60, 18, 12)], sol=-4, ciel=True, trait="eau", galerie="o,s 6 sans-escalier"),
    Salle("u", "Verger", [(18, 58, 18, 14)], sol=-4, ciel=True, galerie="s 6 sans-escalier"),
    Salle("v", "Labyrinthe de haies", [(36, 64, 18, 10)], sol=-4, ciel=True, galerie="s 6 sans-escalier"),
    Salle("w", "Roseraie", [(54, 62, 16, 12)], sol=-4, ciel=True),
    Salle("S", "Grande grille", [(70, 62, 14, 12)], sol=-4, ciel=True, role="sortie", fonction="Allée des statues"),
    Salle("H", "Hall de sortie", [(74, 74, 8, 4)], sol=-4, role="hall"),
    Salle("x", "Bosquet", [(0, 72, 36, 8)], sol=-4, ciel=True, galerie="s 6 sans-escalier"),
    Salle("y", "Potager", [(36, 74, 34, 6)], sol=-4, ciel=True, galerie="s 6 sans-escalier"),
    Salle("L", "Salle verrouillée", [(80, 56, 4, 6)], sol=0, role="verrouillee"),
    Salle("M", "Salle verrouillée", [(70, 74, 4, 4)], sol=-4, role="verrouillee"),
]

P = [
    "S-H sortie", "j-A arrivee",
    "D-a", "a-b", "b-c rampe", "D-d", "a-e rampe", "e-R", "b-R", "b-f rampe", "R-f haute", "c-f", "c-g", "f-g",
    "d-e rampe", "d-k rampe", "e-h", "R-h", "f-P haute", "g-i", "P-i haute", "i-j", "g-j", "P-m haute",
    "h-k", "h-l", "R-l", "k-l", "l-P haute", "l-m", "i-T", "m-T", "j-n", "T-n", "k-o rampe", "l-p", "o-p rampe",
    "p-m", "m-q rampe", "T-r", "q-r rampe", "T-s", "n-s", "o-t", "p-u rampe", "q-u", "q-v", "r-w rampe", "w-S",
    "T-S", "t-u", "u-v", "v-w", "t-x", "u-x", "v-y", "w-y", "x-y",
    "s-L verrou", "S-M verrou",
]

ecrire("Acte3_ForetStatues.txt", "La forêt des statues", 3, 84, 80, S, P,
       ancres=["coffre a c d g h i n o q r t v x", "marchand k p w"],
       organique=12)
