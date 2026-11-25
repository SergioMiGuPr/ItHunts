"""Acte II, carte 2 : « Le cloître et les tours ».

Intention : un château autour d'un cloître à ciel ouvert (au centre, 128 × 112 m, la lune au-dessus, une fontaine), bordé de
quatre galeries couvertes. Deux tours se font face : la tour ouest (armurerie, salle des gardes, chambres à +8 et +14) et la tour
est (bibliothèque à étages, salle du trône à +6). Au nord, la grande salle de bal à galerie (sortie derrière elle, à +6) ; au sud,
le palier du départ et les cuisines. Les galeries du cloître sont à deux niveaux : on court au sol, ou sur leurs toits à +6
(galeries hautes reliées aux tours par des portes hautes). Le traqueur arrive par l'est.
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from carte import Salle, ecrire

S = [
    # Nord : salle de bal, sortie.
    Salle("H", "Hall de sortie", [(28, 0, 6, 2)], sol=6, role="hall"),
    Salle("S", "Grande entrée", [(24, 2, 14, 8)], sol=6, role="sortie", plafond=22, fonction="Salle du trône"),
    Salle("B", "Salle de bal", [(14, 10, 34, 9)], sol=0, plafond=26, galerie="n 6", fonction="Salle de bal"),
    Salle("a", "Chapelle", [(4, 0, 20, 10)], sol=0, plafond=28),
    Salle("b", "Salon de musique", [(38, 0, 14, 10)], sol=6),
    Salle("c", "Chambre", [(52, 0, 10, 10)], sol=14),
    # Tour ouest.
    Salle("d", "Armurerie", [(0, 10, 14, 10)], sol=0),
    Salle("e", "Salle des gardes", [(0, 20, 8, 14)], sol=0, fonction="Armurerie"),
    Salle("f", "Chambre", [(8, 20, 8, 7)], sol=8),
    Salle("g", "Chambre", [(8, 27, 8, 7)], sol=14),
    Salle("h", "Tour effondrée", [(0, 34, 16, 12)], sol=-6, trait="vide", galerie="n 6"),
    # Cloître et ses galeries.
    Salle("n", "Galerie des portraits", [(16, 19, 30, 3)], sol=0),
    Salle("C", "Cloître", [(18, 22, 26, 16)], sol=0, ciel=True, trait="eau", fonction="Fontaine"),
    Salle("o", "Galerie des portraits", [(16, 22, 2, 16)], sol=6, fonction="Galerie des portraits"),
    Salle("p", "Galerie des portraits", [(44, 22, 2, 16)], sol=6, fonction="Galerie des portraits"),
    Salle("q", "Galerie des portraits", [(16, 38, 30, 3)], sol=0),
    # Tour est.
    Salle("i", "Bibliothèque", [(46, 10, 16, 12)], sol=6, galerie="e 8", plafond=24),
    Salle("A", "Porte d'arrivée", [(58, 22, 4, 8)], sol=0, role="arrivee"),
    Salle("j", "Grand escalier", [(46, 22, 12, 10)], sol=0, galerie="n 6"),
    Salle("k", "Salle du trône", [(46, 32, 16, 10)], sol=6, plafond=22),
    Salle("l", "Bains", [(46, 42, 16, 12)], sol=-6, trait="eau"),
    # Sud : départ, cuisines, bains.
    Salle("r", "Salle à manger", [(16, 41, 14, 9)], sol=0),
    Salle("s", "Salon de musique", [(30, 41, 16, 9)], sol=-6, fonction="Salle des gardes"),
    Salle("D", "Palier", [(18, 50, 18, 10)], sol=0, role="depart", plafond=18),
    Salle("t", "Chambre", [(0, 46, 18, 7)], sol=0),
    Salle("u", "Salle à manger", [(0, 53, 18, 9)], sol=4),
    Salle("v", "Armurerie", [(36, 54, 26, 8)], sol=0),
    Salle("w", "Chapelle", [(36, 50, 10, 4)], sol=0, fonction="Galerie des portraits"),
    Salle("L", "Salle verrouillée", [(52, 62, 10, 2)], sol=0, role="verrouillee"),
    Salle("M", "Salle verrouillée", [(0, 62, 8, 2)], sol=4, role="verrouillee"),
    Salle("x", "Salle à manger", [(18, 60, 18, 4)], sol=0, fonction="Galerie des portraits"),
]

P = [
    "S-H sortie", "j-A arrivee",
    "a-S rampe", "S-b", "b-c haute", "a-B", "S-B haute", "B-b rampe", "a-d", "B-d", "B-i rampe", "b-i", "c-i haute",
    "d-e", "d-f haute", "e-f haute", "f-g haute", "e-h", "B-n",
    "n-C", "n-o haute", "n-p haute", "o-C haute", "p-C haute", "C-q", "o-q haute", "p-q haute", "g-o haute",
    "i-j", "p-j haute", "j-k", "p-k", "k-l", "q-k haute",
    "h-q", "h-t", "q-r", "q-s", "r-s", "s-l", "s-w", "r-D", "w-D", "t-u", "t-r", "u-D haute", "D-v", "w-v", "v-l haute",
    "D-x", "u-x", "x-v",
    "v-L verrou", "u-M verrou",
]

ecrire("Acte2_CloitreTours.txt", "Le cloître et les tours", 2, 62, 64, S, P,
       ancres=["coffre a c g h i k l s t u v", "marchand r e j"],
       trous=[(24, 0, 4, 2), (34, 0, 4, 2), (0, 0, 4, 10)])
