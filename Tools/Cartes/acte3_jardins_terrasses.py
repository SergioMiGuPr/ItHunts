"""Acte III, carte 1 : « Les jardins en terrasses ».

Intention : le parc du château, à ciel ouvert, qui descend en quatre terrasses du nord au sud : la terrasse du château (+8, départ),
les jardins d'apparat (+4 : roseraie, kiosque, serres), le grand parterre (0 : grand bassin au centre, labyrinthe de haies à
l'ouest, cimetière à l'est, allée des statues sur l'axe) et le fond du parc (-4 : vergers, bosquets, fontaine), coupé d'est en
ouest par un ravin (fosse, -8). La sortie (grande grille) est au sud. Tout le pourtour est fait de lisières de forêt et de
falaises : les salles du bord ont une corniche en hauteur côté falaise (galerie sans escalier, on y monte par traction, wall jump
ou hissage) qui fait une route haute presque continue autour du parc. Le traqueur arrive par la lisière ouest.
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from carte import Salle, ecrire

S = [
    # Terrasse du château (+8) et jardins d'apparat (+4), au nord.
    Salle("a", "Potager", [(0, 0, 16, 12)], sol=4, ciel=True, galerie="n,o 6 sans-escalier"),
    Salle("b", "Serre en ruine", [(16, 0, 14, 10)], sol=4, ciel=True, galerie="n 6 sans-escalier"),
    Salle("D", "Terrasse du château", [(30, 0, 20, 12)], sol=8, ciel=True, role="depart"),
    Salle("c", "Kiosque", [(50, 0, 14, 10)], sol=4, ciel=True, trait="eau", galerie="n 6 sans-escalier"),
    Salle("d", "Roseraie", [(64, 0, 16, 14)], sol=4, ciel=True, galerie="n 6 sans-escalier"),
    Salle("e", "Allée des statues", [(16, 10, 14, 6), (30, 12, 20, 4), (50, 10, 14, 6)], sol=4, ciel=True),
    # Grand parterre (0).
    Salle("A", "Porte d'arrivée", [(0, 24, 4, 10)], sol=0, role="arrivee"),
    Salle("f", "Labyrinthe de haies", [(0, 12, 16, 12), (4, 24, 14, 14)], sol=0, ciel=True, galerie="o 6 sans-escalier"),
    Salle("g", "Bosquet", [(16, 16, 12, 22)], sol=0, ciel=True),
    Salle("B", "Grand bassin", [(28, 16, 24, 24)], sol=0, ciel=True, trait="eau", galerie="n 4"),
    Salle("h", "Verger", [(52, 16, 12, 22)], sol=0, ciel=True),
    Salle("i", "Cimetière", [(64, 14, 16, 24)], sol=0, ciel=True),
    Salle("j", "Allée des statues", [(0, 38, 28, 6)], sol=0, ciel=True),
    Salle("k", "Allée des statues", [(52, 38, 28, 6)], sol=0, ciel=True),
    Salle("l", "Fontaine", [(28, 40, 24, 8)], sol=-4, ciel=True, trait="eau"),
    # Fond du parc (-4), ravin (-8), sortie.
    Salle("m", "Verger", [(0, 44, 14, 12)], sol=-4, ciel=True, galerie="o 6 sans-escalier"),
    Salle("n", "Potager", [(14, 44, 14, 10)], sol=-4, ciel=True),
    Salle("o", "Bosquet", [(52, 44, 14, 10)], sol=-4, ciel=True),
    Salle("p", "Roseraie", [(66, 44, 14, 12)], sol=-4, ciel=True),
    Salle("R", "Ravin", [(14, 54, 52, 8), (28, 48, 24, 6)], sol=-8, ciel=True, trait="vide", galerie="n 8"),
    Salle("q", "Cimetière", [(0, 56, 14, 14)], sol=-4, ciel=True, galerie="o,s 6 sans-escalier"),
    Salle("r", "Bosquet", [(14, 62, 18, 12)], sol=-4, ciel=True, galerie="s 6 sans-escalier"),
    Salle("S", "Grande grille", [(32, 62, 16, 10)], sol=-4, ciel=True, role="sortie", fonction="Allée des statues"),
    Salle("H", "Hall de sortie", [(36, 72, 8, 4)], sol=-4, role="hall"),
    Salle("s", "Verger", [(48, 62, 18, 12)], sol=-4, ciel=True, galerie="s 6 sans-escalier"),
    Salle("t", "Labyrinthe de haies", [(66, 56, 14, 14)], sol=-4, ciel=True, galerie="s 6 sans-escalier"),
    Salle("u", "Serre en ruine", [(0, 70, 14, 6)], sol=-4, ciel=True),
    Salle("v", "Kiosque", [(66, 70, 14, 6)], sol=-4, ciel=True, trait="eau"),
    # Bande est : lisière de forêt (ajoutée pour la surface de l'acte III, ×6).
    Salle("w", "Bosquet", [(80, 0, 10, 26)], sol=4, ciel=True, galerie="n,e 6 sans-escalier"),
    Salle("x", "Verger", [(80, 26, 10, 24)], sol=0, ciel=True, galerie="e 6 sans-escalier"),
    Salle("y", "Cimetière", [(80, 50, 10, 26)], sol=-4, ciel=True, galerie="e,s 6 sans-escalier"),
    Salle("L", "Salle verrouillée", [(14, 74, 6, 2)], sol=-4, role="verrouillee"),
    Salle("M", "Salle verrouillée", [(60, 74, 6, 2)], sol=-4, role="verrouillee"),
]

P = [
    "S-H sortie", "f-A arrivee",
    "a-b", "b-D", "D-c", "c-d", "b-e", "D-e", "c-e", "a-f", "d-i", "e-g", "e-B", "e-h",
    "f-g", "g-B", "B-h", "h-i", "f-j", "g-j", "i-k", "h-k", "B-l haute", "j-l rampe", "k-l rampe", "j-m rampe", "j-n rampe",
    "k-o rampe", "k-p rampe", "l-R rampe", "n-R", "o-R", "m-n", "o-p", "m-q", "p-t",
    "R-r", "R-S", "R-s", "q-r", "r-S", "S-s", "s-t", "q-u", "r-u", "t-v", "s-v",
    "d-w", "i-w rampe", "i-x", "k-x", "p-x rampe", "w-x rampe", "x-y rampe", "p-y", "t-y", "v-y",
    "r-L verrou", "s-M verrou",
]

ecrire("Acte3_JardinsTerrasses.txt", "Les jardins en terrasses", 3, 90, 76, S, P,
       ancres=["coffre a c d g h i m p q t u v", "marchand j k n"],
       organique=12)
