"""Acte II, carte 1 : « Le château des cendres ».

Intention : un château en trois niveaux. Au centre, la Grande salle (192 × 128 m, plafond à 30 m) : galerie à 8 m sur ses quatre
côtés, qui la relie par des portes hautes aux ailes de l'étage ; c'est le hub, traversé par les grandes boucles. Au rez-de-chaussée
(0), un anneau de salles d'apparat autour d'elle (salle de bal, salle à manger, salon de musique, chapelle) ; à l'étage (+8), les
appartements à l'ouest et la bibliothèque au nord-est ; au sous-sol (-6), les bains et l'armurerie, sous l'aile sud. Départ au sud
(Palier), sortie au nord (Grande entrée vers le jardin), le traqueur arrive par l'ouest. Les galeries des portraits sont de longues
lignes droites de 16 m de large pour la vitesse, coupées par des salles où l'on change d'étage.
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from carte import Salle, ecrire

S = [
    # Aile nord : sortie, chapelle, salle du trône, bibliothèque (étage).
    Salle("H", "Hall de sortie", [(30, 0, 6, 2)], sol=8, role="hall"),
    Salle("S", "Grande entrée", [(26, 2, 14, 9)], sol=8, role="sortie", plafond=24, fonction="Salle du trône"),
    Salle("a", "Chapelle", [(4, 0, 14, 11)], sol=0, plafond=26),
    Salle("b", "Salon de musique", [(18, 0, 8, 11)], sol=4),
    Salle("c", "Salle du trône", [(40, 0, 14, 11)], sol=4, plafond=22, galerie="n 6 sans-escalier"),
    Salle("d", "Bibliothèque", [(54, 0, 10, 14)], sol=8, galerie="e 6", plafond=20),
    Salle("e", "Galerie des portraits", [(0, 11, 26, 2)], sol=0),
    Salle("f", "Galerie des portraits", [(26, 11, 28, 2)], sol=4),
    # Grande salle au centre, ailes est et ouest.
    Salle("A", "Porte d'arrivée", [(0, 22, 4, 8)], sol=0, role="arrivee"),
    Salle("g", "Armurerie", [(0, 13, 10, 9)], sol=0),
    Salle("h", "Chambre", [(10, 13, 8, 9)], sol=8),
    Salle("G", "Grande salle", [(18, 13, 24, 16)], sol=0, plafond=30, galerie="n,s,e,o 8", fonction="Salle de bal"),
    Salle("i", "Salle à manger", [(42, 13, 12, 9)], sol=0),
    Salle("j", "Grand escalier", [(54, 14, 10, 10)], sol=4),
    Salle("k", "Chambre", [(4, 22, 14, 7)], sol=8),
    Salle("l", "Salle de bal", [(42, 22, 12, 12)], sol=0, plafond=24, galerie="e 6"),
    Salle("m", "Salon de musique", [(54, 24, 10, 10)], sol=0),
    Salle("n", "Galerie des portraits", [(0, 30, 18, 3), (4, 29, 14, 1)], sol=0),
    Salle("o", "Chapelle", [(18, 29, 12, 9)], sol=0),
    Salle("p", "Salle du trône", [(30, 29, 12, 9)], sol=-6, trait="eau", fonction="Bains"),
    # Aile sud : départ, bains (sous-sol), armurerie.
    Salle("q", "Bibliothèque", [(0, 33, 12, 11)], sol=0, galerie="o 6 sans-escalier"),
    Salle("r", "Chambre", [(12, 33, 6, 11)], sol=4),
    Salle("s", "Tour effondrée", [(18, 38, 12, 10)], sol=-6, trait="vide"),
    Salle("t", "Bains", [(30, 38, 14, 10)], sol=-6, trait="eau"),
    Salle("u", "Armurerie", [(44, 34, 10, 8)], sol=0),
    Salle("v", "Salle à manger", [(54, 34, 10, 12)], sol=4),
    Salle("w", "Galerie des portraits", [(0, 44, 18, 3)], sol=0),
    Salle("x", "Salle des gardes", [(44, 42, 10, 6)], sol=-6, fonction="Armurerie"),
    Salle("D", "Palier", [(18, 48, 16, 10)], sol=0, role="depart", plafond=18),
    Salle("y", "Salon de musique", [(0, 47, 10, 11)], sol=0),
    Salle("z", "Chambre", [(10, 47, 8, 11)], sol=4),
    Salle("1", "Grand escalier", [(34, 48, 12, 10)], sol=0, galerie="n 6"),
    Salle("2", "Salle à manger", [(46, 48, 18, 10)], sol=4),
    Salle("3", "Chapelle", [(54, 46, 10, 2)], sol=4, fonction="Galerie des portraits"),
    Salle("L", "Salle verrouillée", [(0, 58, 8, 2)], sol=0, role="verrouillee"),
    Salle("M", "Salle verrouillée", [(56, 58, 8, 2)], sol=4, role="verrouillee"),
]

P = [
    "S-H sortie", "A-g arrivee",
    # Nord
    "a-b", "b-S", "S-c", "c-d", "a-e", "b-e rampe", "S-f", "c-f", "d-f haute", "d-j",
    # Grande salle et ses ailes
    "e-g", "e-h haute", "g-h haute", "h-G haute", "f-G", "e-G", "f-i", "i-j", "G-i", "G-l", "i-l", "j-m", "l-m",
    "g-k haute", "k-G haute", "k-n haute", "n-o", "G-o", "G-p haute", "o-p", "p-l haute", "l-u", "m-v", "u-v",
    # Sud
    "n-q", "q-r", "r-n rampe", "q-w", "o-s", "p-t", "s-t", "t-u", "u-x rampe", "t-x", "v-3", "x-3 haute", "3-2",
    "w-y", "w-z", "y-z", "s-D", "z-D", "D-1", "t-1", "1-2", "x-1",
    "y-L verrou", "2-M verrou",
]

ecrire("Acte2_ChateauCendres.txt", "Le château des cendres", 2, 64, 60, S, P,
       ancres=["coffre a c d h k p q s u x z 2", "marchand o i y"],
       trous=[(26, 0, 4, 2), (36, 0, 4, 2), (0, 0, 4, 11), (54, 14, 0, 0)])
