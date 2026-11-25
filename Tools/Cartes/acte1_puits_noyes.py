"""Acte I, carte 1 : « Les puits noyés ».

Intention : des caves en trois étages (plateau nord à +8, galeries du milieu à 0, fonds à -4 et -8). Au centre, le Grand puits
(128 × 128 m, fosse, sol à -8) : on y entre par une galerie qui en fait le tour à 0 m (portes hautes nord, est et ouest), on
descend au fond par son escalier ou on saute ; c'est le hub que traversent les boucles. Départ au sud (Salle du puits), sortie au
nord (Grande porte, +8), le traqueur entre par l'est (Porte d'arrivée). Chaque salle a au moins deux sorties, les grandes en ont
trois ou plus ; des portes hautes sans escalier (traction, wall jump) donnent des raccourcis que le traqueur prend en escaladant.
"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
from carte import Salle, ecrire

S = [
    # Bande nord : plateau à +8 (sortie), puis +4.
    Salle("a", "Ossuaire", [(0, 0, 16, 8)], sol=8, trait="vide"),
    Salle("b", "Crypte", [(16, 0, 8, 8)], sol=8),
    Salle("H", "Hall de sortie", [(28, 0, 8, 2)], sol=8, role="hall"),
    Salle("S", "Grande porte", [(24, 2, 16, 8)], sol=8, role="sortie", plafond=20, fonction="Salle des gardes"),
    Salle("c", "Cachot", [(40, 0, 8, 8)], sol=8),
    Salle("d", "Galerie de mine", [(48, 0, 8, 6), (56, 3, 8, 3)], sol=8),
    Salle("L", "Salle verrouillée", [(56, 0, 8, 3)], sol=8, role="verrouillee"),
    Salle("e", "Cave à vin", [(0, 8, 8, 8)], sol=4),
    Salle("f", "Gouffre", [(8, 8, 16, 8)], sol=4, trait="vide", galerie="s 4"),
    Salle("g", "Salle de torture", [(24, 10, 16, 6)], sol=0),
    Salle("h", "Citerne", [(40, 8, 16, 8), (48, 6, 8, 2)], sol=4, trait="eau", galerie="n 4"),
    Salle("i", "Réserve", [(56, 6, 8, 10)], sol=4),
    # Milieu : hub du Grand puits (fond à -8, galerie à 0).
    Salle("j", "Cachot", [(0, 16, 8, 8)], sol=0),
    Salle("k", "Chapelle souterraine", [(0, 24, 8, 8)], sol=0),
    Salle("l", "Grotte inondée", [(8, 16, 16, 12)], sol=-8, trait="eau"),
    Salle("n2", "Salle des gardes", [(8, 28, 16, 4)], sol=0),
    Salle("P", "Grand puits", [(24, 16, 16, 16)], sol=-8, trait="vide", galerie="n,s,e,o 8", plafond=26,
          fonction="Gouffre"),
    Salle("m", "Salle des gardes", [(40, 16, 8, 8)], sol=0),
    Salle("n", "Réserve", [(40, 24, 8, 8)], sol=-4),
    Salle("o", "Galerie de mine", [(48, 16, 16, 8)], sol=0),
    Salle("p", "Ossuaire", [(48, 24, 16, 8)], sol=-4, trait="vide"),
    # Sud : -4 et 0, le départ.
    Salle("q", "Crypte", [(5, 32, 11, 8), (0, 36, 5, 4)], sol=0),
    Salle("M", "Salle verrouillée", [(0, 32, 5, 4)], sol=0, role="verrouillee"),
    Salle("r", "Cave à vin", [(16, 32, 8, 8)], sol=-4),
    Salle("s", "Citerne", [(24, 32, 16, 8)], sol=-4, trait="eau"),
    Salle("t", "Salle de torture", [(40, 32, 16, 8)], sol=0, galerie="e 5 sans-escalier"),
    Salle("A", "Porte d'arrivée", [(56, 32, 8, 8)], sol=0, role="arrivee"),
    Salle("u", "Réserve", [(0, 40, 8, 8)], sol=0),
    Salle("v", "Chapelle souterraine", [(8, 40, 16, 8)], sol=4),
    Salle("D", "Salle du puits", [(24, 40, 16, 12)], sol=0, role="depart", plafond=16),
    Salle("w", "Cachot", [(40, 40, 8, 8)], sol=0),
    Salle("x", "Grotte inondée", [(48, 40, 16, 8)], sol=4, trait="eau", galerie="n 4"),
    Salle("y", "Galerie de mine", [(0, 48, 16, 8)], sol=0),
    Salle("z", "Ossuaire", [(16, 48, 8, 8)], sol=-4),
    Salle("1", "Salle des gardes", [(24, 52, 16, 4)], sol=0),
    Salle("2", "Gouffre", [(40, 48, 16, 8)], sol=0, trait="vide", galerie="s 6"),
    Salle("3", "Crypte", [(56, 48, 8, 8)], sol=4),
]

# Identifiants d'un seul caractère pour le jeu.
for s in S:
    if s.id == "n2":
        s.id = "N"

P = [
    "H-S sortie",
    "a-b", "b-S", "S-c", "c-d", "d-L verrou", "a-e", "a-f", "b-f", "S-g rampe", "c-h", "d-h", "d-i",
    "e-f", "f-g", "g-h", "h-i", "e-j", "g-P haute", "h-m", "h-o", "i-o",
    "j-k", "j-l haute", "k-l haute", "k-N", "l-N rampe", "l-P", "N-P haute", "P-m haute", "P-n", "P-s",
    "m-n", "m-o", "n-p", "o-p",
    "k-q", "N-q", "N-r rampe", "q-M verrou", "q-r rampe", "r-s", "s-t", "n-t", "p-t", "t-A arrivee",
    "q-u", "q-v", "r-v haute", "s-D", "t-w", "t-x", "u-v", "v-D", "D-w", "w-x",
    "u-y", "v-y", "v-z haute", "y-z", "z-1", "z-D", "D-1", "1-2", "D-2", "w-2", "x-2 rampe", "x-3", "2-3 rampe",
]

ecrire("Acte1_PuitsNoyes.txt", "Les puits noyés", 1, 64, 56, S, P,
       ancres=["coffre b i j n r w z 3 P t", "marchand k m v"],
       trous=[(24, 0, 4, 2), (36, 0, 4, 2)], organique=12)
