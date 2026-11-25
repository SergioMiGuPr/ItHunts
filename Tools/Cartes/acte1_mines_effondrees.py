"""Acte I, carte 2 : « Les mines effondrées ».

Intention : une mine abandonnée creusée dans une falaise. Le départ est à l'ouest (Salle du puits), la sortie à l'est (Grande porte,
sur le plateau à +8), le traqueur arrive par le nord. Trois longues galeries de mine (16 m de large, 160 à 200 m de long) traversent
la carte d'ouest en est à trois hauteurs (+4, 0, -4) : ce sont les lignes droites du bunny hop, coupées tous les 40 m par une salle
où l'on change d'étage. Au centre, La Fosse (vide, 144 × 112 m) : galerie sur trois côtés à 8 m au-dessus du fond, reliée aux
galeries de mine par des portes hautes ; on la traverse par ses ponts de 14 m ou on fait le tour. Au sud, des salles plus petites
(cachots, caves) en dédale à boucles courtes pour semer le traqueur.
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from carte import Salle, ecrire

S = [
    # Nord : arrivée du traqueur, réserves, galerie de mine haute (+4).
    Salle("A", "Porte d'arrivée", [(27, 0, 7, 7)], sol=4, role="arrivee"),
    Salle("a", "Réserve", [(0, 0, 11, 9)], sol=4),
    Salle("b", "Salle des gardes", [(11, 0, 16, 7)], sol=4),
    Salle("c", "Cave à vin", [(34, 0, 13, 7)], sol=4),
    Salle("d", "Ossuaire", [(47, 0, 13, 11)], sol=8, trait="vide"),
    Salle("e", "Galerie de mine", [(11, 7, 36, 3)], sol=4),             # galerie haute, 288 × 24 m
    Salle("f", "Crypte", [(0, 9, 9, 10)], sol=0),
    # Milieu nord : La Fosse au centre, galerie de mine du milieu (0).
    Salle("g", "Cachot", [(9, 10, 10, 9)], sol=0),
    Salle("F", "La Fosse", [(19, 10, 18, 14)], sol=-8, trait="vide", galerie="n,o,e 8", plafond=28, fonction="Gouffre"),
    Salle("h", "Salle de torture", [(37, 10, 10, 8)], sol=0),
    Salle("i", "Chapelle souterraine", [(47, 11, 13, 11)], sol=8, galerie="o 4"),
    Salle("D", "Salle du puits", [(0, 19, 12, 12)], sol=0, role="depart", plafond=16),
    Salle("j", "Galerie de mine", [(12, 19, 7, 5), (12, 24, 30, 3)], sol=0),   # galerie du milieu, coude
    Salle("k", "Réserve", [(37, 18, 10, 6)], sol=0),
    Salle("S", "Grande porte", [(47, 22, 11, 10)], sol=8, role="sortie", plafond=20, fonction="Salle des gardes"),
    Salle("H", "Hall de sortie", [(58, 24, 2, 6)], sol=8, role="hall"),
    # Sud : -4 et 0, dédale de petites salles.
    Salle("l", "Cave à vin", [(0, 31, 10, 8)], sol=0),
    Salle("m", "Citerne", [(12, 27, 14, 10)], sol=-4, trait="eau", galerie="n 4"),
    Salle("n", "Gouffre", [(26, 27, 16, 13)], sol=0, trait="vide", galerie="s 6 sans-escalier"),
    Salle("o", "Galerie de mine", [(42, 24, 5, 16), (47, 32, 13, 4)], sol=4),   # galerie de l'est, en L
    Salle("p", "Grotte inondée", [(47, 36, 13, 11)], sol=0, trait="eau"),
    Salle("q", "Cachot", [(10, 31, 2, 8), (0, 39, 12, 6)], sol=0),
    Salle("r", "Salle des gardes", [(12, 37, 14, 6)], sol=-4),
    Salle("s", "Galerie de mine", [(12, 43, 30, 3)], sol=-4),             # galerie basse, 240 × 24 m
    Salle("t", "Crypte", [(26, 40, 16, 3)], sol=-4),
    Salle("u", "Ossuaire", [(42, 40, 5, 10)], sol=-4),
    Salle("v", "Réserve", [(0, 45, 10, 9)], sol=-4),
    Salle("w", "Cave à vin", [(10, 46, 12, 8)], sol=-8),
    Salle("x", "Salle de torture", [(22, 46, 12, 8)], sol=-4, galerie="s 6"),
    Salle("y", "Chapelle souterraine", [(34, 46, 8, 8)], sol=-4),
    Salle("z", "Cachot", [(42, 50, 9, 6), (47, 47, 4, 3)], sol=0),
    Salle("1", "Citerne", [(51, 47, 9, 9)], sol=0, trait="eau"),
    Salle("2", "Galerie de mine", [(0, 54, 22, 3)], sol=-4),
    Salle("3", "Crypte", [(22, 54, 20, 6)], sol=-8, trait="vide"),
    Salle("4", "Réserve", [(42, 56, 18, 4)], sol=0),
    Salle("L", "Salle verrouillée", [(0, 57, 8, 3)], sol=-4, role="verrouillee"),
    Salle("M", "Salle verrouillée", [(8, 57, 6, 3)], sol=-4, role="verrouillee"),
]

P = [
    "S-H sortie", "e-A arrivee",
    "a-b", "b-e rampe", "a-f", "c-e", "c-d", "d-i", "e-h rampe", "e-g haute", "g-F haute",
    "f-g", "f-D", "g-D", "g-j", "F-j", "F-h haute", "h-k", "h-i haute", "i-S", "k-S", "j-k",
    "D-l", "D-q", "j-m haute", "j-n", "m-n", "n-o rampe", "o-S", "o-p", "o-u", "n-t",
    "l-q", "q-r", "m-r", "r-s", "r-t", "s-t", "t-u", "u-p", "u-z", "q-v", "s-w haute", "s-x", "s-y", "v-w", "w-x",
    "x-y", "y-u", "z-1", "1-p", "z-4", "v-2", "w-2", "2-3", "x-3", "y-3", "3-4", "1-4",
    "2-L verrou", "2-M verrou",
]

ecrire("Acte1_MinesEffondrees.txt", "Les mines effondrées", 1, 60, 60, S, P,
       ancres=["coffre a d g i k m r u y z 3", "marchand l t h"],
       trous=[(0, 31, 0, 0)], organique=12)
