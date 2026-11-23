# -*- coding: utf-8 -*-
"""
Génère les 70 assets d'amélioration (docs/design/AMELIORATIONS.md, nuit 4 point 2, D-100) en YAML Unity :
Assets/_Project/Settings/Upgrades/Upgrade_<Code>.asset (+ .meta), les pools (UpgradePool_Default : 30 génériques ;
UpgradePool_Renard / Cameleon / Koala / Hibou : 10 chacun) et branche les pools sur les personnages et les artefacts.

Usage : python docs/design/outils/gen_upgrades.py   (depuis la racine du projet ; relancer après un changement de la table)

Les assets qui existaient avant les rangs gardent leur GUID (LEGACY) ; les autres reçoivent un GUID stable dérivé du code.
Les anciens assets qui ne sont plus dans la liste sont supprimés avec leur .meta.
"""
import hashlib
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
UPGRADES_DIR = os.path.join(ROOT, "Assets", "_Project", "Settings", "Upgrades")
SETTINGS_DIR = os.path.join(ROOT, "Assets", "_Project", "Settings")
SCRIPT_GUID = "dfd449386b98ff941b363613b07b1571"       # UpgradeDefinition.cs
POOL_SCRIPT_GUID = "3acb984585714184b9f2883f6df4009f"  # UpgradePool.cs

# Anciens assets → GUID gardé (même amélioration, devenue à rangs).
LEGACY = {
    "G23": "8fe6842dd42756047a45974e63ef80ba",   # Upgrade_Charge
    "G10": "b2c471e5e50d26a48ba15f523da4ce39",   # Upgrade_Traction
    "G5": "546f4b2e014b60240a66e93345001817",    # Upgrade_Collecte
    "G18": "ed168600d4239364e90c834ae610377f",   # Upgrade_Projectiles
    "G14": "3e84a9176114a3d4a97b75bd8b31cbd8",   # Upgrade_Repulsion
    "G19": "2065ed9c70964fa44a1bb47d2fe7385c",   # Upgrade_PV
    "G6": "fdd414373f62cfa428a93f182ff813b9",    # Upgrade_Aspiration
    "R1": "26befee8eb793974ca541bb97013450d",    # Upgrade_Lanterne_Portee
    "R2": "becd6a958a4543e4b8ce71014fa32230",    # Upgrade_Lanterne_Cone
    "R3": "f1f9bebbe5f3b7a46a3b67663c8c1224",    # Upgrade_Lanterne_Recharge
    "R4": "8972aed93f1f47449b6c1d219ad6a9f6",    # Upgrade_Lanterne_Brulure
    "C1": "a21a70bdccb7db14e9d52872f8865edb",    # Upgrade_Cameleon_Paralysie
    "C2": "6c200e3314ab7e94483813523f754d33",    # Upgrade_Cameleon_Explosion
    "C3": "a9857c2357e572b45aed675a29ee28d7",    # Upgrade_Cameleon_Perforation
    "C4": "5946383c7257f8941989b406e8c371db",    # Upgrade_Cameleon_Ricochet
    "C5": "67a376081ba74204ba9d62c5147fa989",    # Upgrade_Cameleon_RechargeKill
    "C-P2": "88979d5585fbe744fb6999d4bd1ad7c6",  # Upgrade_Cameleon_Oeil
}

POOL_GUIDS = {
    "Default": "3b4226d748f2ec64a9e2ed472e75197a",
    "Renard": "e7242932d72fd4a4b84f89f607f1a2c6",    # ancien UpgradePool_Lanterne
    "Cameleon": "fd7eb9e02f368b644a015ec3ea4167dd",  # ancien UpgradePool_Barillet
    "Koala": None,
    "Hibou": None,
}
OLD_POOLS = ["UpgradePool_Lanterne", "UpgradePool_Barillet"]

CHAR = {"": 0, "Renard": 1, "Cameleon": 2, "Koala": 3, "Hibou": 4}

# Couleurs (HDR) par famille.
C_PARADE = (3.0, 2.4, 0.9)
C_AMES = (0.4, 2.6, 3.2)
C_TRACTION = (0.3, 0.9, 3.2)
C_REPULSION = (3.2, 1.3, 0.3)
C_COMBAT = (3.2, 0.5, 0.5)
C_MOUVEMENT = (0.4, 2.4, 3.0)
C_UTIL = (1.4, 2.8, 1.2)
C_RENARD = (3.4, 1.2, 0.3)
C_CAMELEON = (1.2, 3.0, 1.0)
C_KOALA = (2.6, 2.4, 1.8)
C_HIBOU = (2.0, 1.0, 3.2)

# code, kind, nom, description, symbole, couleur, personnage, passif, v1, v2, v3, textes des 5 rangs, synergies
U = []


def add(code, kind, name, desc, sym, color, char, passive, v1, v2, v3, texts, syn):
    assert len(v1) == 5 and len(texts) == 5, code
    assert v2 is None or len(v2) == 5, code
    assert v3 is None or len(v3) == 5, code
    U.append(dict(code=code, kind=kind, name=name, desc=desc, sym=sym, color=color, char=char, passive=passive,
                  v1=v1, v2=v2, v3=v3, texts=texts, syn=syn))


# ---------- Génériques (30) ----------
add("G1", 0, "Riposte", "Une parade réussie renvoie une onde qui blesse les ennemis autour.", "↯", C_PARADE, "", False,
    [15, 22, 30, 40, 55], [3, 3.5, 4, 4.5, 5], [0, 0, 0, 0, 1.5],
    ["15 PV, 3 m", "22 PV, 3.5 m", "30 PV, 4 m, repousse", "40 PV, 4.5 m", "55 PV, 5 m, l'onde étourdit 1.5 s"],
    "Détonateur (G20), Briseur d'os (G21)")
add("G2", 1, "Garde large", "Fenêtre de parade plus longue.", "⟨⟩", C_PARADE, "", False,
    [0.04, 0.06, 0.08, 0.10, 0.12], None, None,
    ["+40 ms", "+60 ms", "+80 ms, la tolérance tardive double", "+100 ms", "+120 ms, pare aussi les débris et les projectiles (boss compris)"],
    "Rythme de fer (G4)")
add("G3", 2, "Second souffle", "Une parade réussie rend des PV.", "♥", C_PARADE, "", False,
    [6, 9, 12, 16, 22], None, None,
    ["6 PV", "9 PV", "12 PV + une charge de mouvement", "16 PV", "22 PV, + une charge de traction"],
    "Charge en plus (G23), Traction rapide (G10)")
add("G4", 3, "Rythme de fer", "Chaque parade réussie d'affilée (6 s) augmente vos dégâts.", "♩", C_PARADE, "", False,
    [0.10, 0.15, 0.20, 0.25, 0.30], [3, 3, 4, 4, 5], None,
    ["+10 % par parade, max 3", "+15 %, max 3", "+20 %, max 4", "+25 %, max 4", "+30 %, max 5, à 3 parades le temps ralentit 1 s autour de vous"],
    "Prédateur (G22)")
add("G5", 4, "Grande orbe", "Rayon de collecte des âmes.", "R+", C_AMES, "", False,
    [0.25, 0.40, 0.60, 0.85, 1.20], None, None,
    ["+25 % (10 m)", "+40 %", "+60 %", "+85 %", "+120 % (17.6 m), collecte à travers les murs"],
    "Aspiration (G6), Flair (R-P2)")
add("G6", 5, "Aspiration", "Les âmes viennent plus vite, les coffres offrent leur choix plus longtemps.", ">>", C_AMES, "", False,
    [0.5, 0.8, 1.1, 1.5, 2.0], [0, 0, 45, 45, 45], None,
    ["+50 %", "+80 %", "+110 %, les âmes au sol durent 45 s", "+150 %", "+200 %, chaque âme ramassée soigne 1 PV"],
    "Grande orbe (G5), Âmes gourmandes (G7)")
add("G7", 6, "Âmes gourmandes", "Plus d'âmes par ennemi tué.", "✦", C_AMES, "", False,
    [0.10, 0.18, 0.25, 0.35, 0.50], None, None,
    ["+10 %", "+18 %", "+25 %, +1 âme par kill à plus de 14 m/s", "+35 %", "+50 %, kill à plus de 14 m/s : âmes doublées"],
    "Vif-argent (G26), Semelles de vent (G24)")
add("G8", 7, "Marchandage", "Prix du marchand réduits.", "%", C_AMES, "", False,
    [0.08, 0.14, 0.20, 0.26, 0.33], None, None,
    ["−8 %", "−14 %", "−20 %, la hausse par achat est divisée par 2", "−26 %", "−33 %, la première montée de rang de chaque acte est gratuite"],
    "Œil du collectionneur (G30)")
add("G9", 8, "Orbe vorace", "Chaque âme ramassée réduit vos recharges (traction, artefact, dash).", "◌", C_AMES, "", False,
    [0.05, 0.08, 0.11, 0.15, 0.20], None, None,
    ["0.05 s par âme", "0.08 s", "0.11 s, aussi la recharge de la répulsion", "0.15 s", "0.2 s, un kill rend une charge de traction"],
    "Traction rapide (G10), Main leste (C5)")
add("G10", 9, "Traction rapide", "Une charge de traction revient plus vite.", "-1s", C_TRACTION, "", False,
    [1, 1.6, 2.2, 2.8, 3.4], None, None,
    ["−1 s (5 s)", "−1.6 s", "−2.2 s", "−2.8 s", "−3.4 s, l'arrivée d'une traction rend la charge si vous atterrissez à plus de 12 m/s"],
    "Second souffle (G3), Orbe vorace (G9)")
add("G11", 10, "Harpon", "La traction vise aussi les ennemis : vous êtes tiré vers eux.", "⚓", C_TRACTION, "", False,
    [0, 0, 20, 35, 50], [0, 0, 0, 1, 1], [0, 0, 0, 0, 3],
    ["goules", "goules et volants", "l'arrivée frappe 20 PV", "35 PV et étourdit 1 s", "50 PV, l'arrivée explose (3 m)"],
    "Briseur d'os (G21), Détonateur (G20), Fente (K3)")
add("G12", 11, "Élan de traction", "La traction accélère plus vite et garde plus d'élan à l'arrivée (plafond 18 inchangé).", "→→", C_TRACTION, "", False,
    [0.25, 0.5, 0.75, 1.0, 1.25], None, None,
    ["accélération +25 %", "+50 %", "+75 %, l'élan de sortie est gardé 1 s de plus", "+100 %", "+125 %, 4 s de tractions sans charge après une parade réussie"],
    "Garde large (G2), Vif-argent (G26)")
add("G13", 12, "Sillage", "Pendant une traction, vous laissez une traînée qui blesse et ralentit les ennemis traversés.", "≋", C_TRACTION, "", False,
    [8, 12, 16, 22, 30], [0.20, 0.20, 0.35, 0.35, 0.35], [2, 2, 2, 2, 2],
    ["8 PV, −20 % 2 s", "12 PV", "16 PV, −35 %", "22 PV", "30 PV, la traînée enflamme"],
    "Allumette (G28), Fourrure de braise (R-P1)")
add("G14", 13, "Onde blessante", "L'onde de répulsion blesse les ennemis touchés.", "10", C_REPULSION, "", False,
    [10, 15, 20, 28, 38], [0, 0, 1.5, 1.5, 1.5], None,
    ["10 PV", "15 PV", "20 PV, ralentit 1.5 s", "28 PV", "38 PV, double onde (une seconde 0.3 s après)"],
    "Rebond (G16), Onde de choc (G15)")
add("G15", 14, "Onde de choc", "Répulsion plus large et plus longue.", "◠", C_REPULSION, "", False,
    [10, 15, 20, 25, 30], [2, 3, 4, 5, 6], None,
    ["+10°, +2 m", "+15°, +3 m", "+20°, +4 m, les objets repoussés vont 20 % plus vite", "+25°, +5 m", "+30°, +6 m, renvoie les débris et projectiles du boss"],
    "Projectiles lourds (G18)")
add("G16", 15, "Rebond", "Un ennemi repoussé contre un mur ou un autre ennemi prend des dégâts d'impact.", "⤾", C_REPULSION, "", False,
    [1.5, 1.8, 2.1, 2.5, 3.0], None, None,
    ["×1.5", "×1.8", "×2.1, étourdit 1 s", "×2.5", "×3, l'ennemi projeté qui meurt explose (3.5 m)"],
    "Détonateur (G20), Briseur d'os (G21)")
add("G17", 16, "Charge de répulsion", "La répulsion a une charge en plus ; sa recharge est plus courte.", "+1", C_REPULSION, "", False,
    [1, 1, 1, 2, 2], [0, 1, 1.5, 1.5, 1.5], None,
    ["+1 charge", "recharge −1 s", "−1.5 s", "+2 charges", "la répulsion sur le sol vous propulse en hauteur (saut de 4 m) sans dépenser de charge de mouvement"],
    "Pas de chat (G25), Chute amortie (G27)")
add("G18", 17, "Projectiles lourds", "Objets lancés ou repoussés : plus de dégâts (zone comprise).", "x1.25", C_COMBAT, "", False,
    [0.25, 0.4, 0.6, 0.85, 1.2], None, None,
    ["+25 %", "+40 %", "+60 %, les objets ne se brisent plus au premier impact", "+85 %", "+120 %, un objet lancé revient vers l'orbe après l'impact"],
    "Onde de choc (G15), Langue de caméléon (C8)")
add("G19", 18, "Cœur solide", "PV max.", "PV+", C_COMBAT, "", False,
    [25, 40, 55, 75, 100], None, None,
    ["+25 (et autant tout de suite)", "+40", "+55, régénération 1 PV / 4 s hors combat", "+75", "+100, une fois par run vous revenez à 50 % de PV au lieu de mourir (pas face à la prise du traqueur)"],
    "Second souffle (G3), Peau épaisse (K-P1)")
add("G20", 19, "Détonateur", "Un ennemi étourdi ou paralysé qui meurt explose.", "✸", C_COMBAT, "", False,
    [20, 28, 36, 46, 60], [3, 3.5, 4, 4.5, 5], [0, 0, 0.8, 0.8, 0.8],
    ["20 PV, 3 m", "28 PV, 3.5 m", "36 PV, 4 m, l'explosion étourdit 0.8 s", "46 PV, 4.5 m", "60 PV, 5 m, chaîne : les morts par explosion explosent (3 maillons)"],
    "Riposte (G1), Rebond (G16), Balles paralysantes (C1)")
add("G21", 20, "Briseur d'os", "Dégâts contre les ennemis étourdis ou paralysés.", "✕", C_COMBAT, "", False,
    [1.25, 1.45, 1.65, 1.9, 2.2], None, None,
    ["×1.25", "×1.45", "×1.65, l'étourdissement dure 0.5 s de plus", "×1.9", "×2.2, un étourdi sous 30 % de PV meurt du coup suivant"],
    "Détonateur (G20), Harpon (G11), Brise-garde (K7)")
add("G22", 21, "Prédateur", "Chaque kill : +dégâts pendant 5 s (piles).", "▲", C_COMBAT, "", False,
    [0.06, 0.08, 0.10, 0.12, 0.15], [5, 5, 6, 6, 8], None,
    ["+6 %, max 5", "+8 %, max 5", "+10 %, max 6, + vitesse de course +0.5 m/s", "+12 %, max 6", "+15 %, max 8, à 5 piles vos attaques étourdissent"],
    "Rythme de fer (G4), Élan du bûcheron (K8)")
add("G23", 22, "Charge en plus", "Charges de mouvement (dash, saut en l'air).", "+1", C_MOUVEMENT, "", False,
    [1, 1, 2, 2, 3], [0, 0.5, 0.5, 0.5, 0.5], [0, 0, 0, 1, 1],
    ["+1", "recharge au sol −0.5 s", "+2", "recharge en l'air −1 s", "+3, 2 s de dashs gratuits après un kill"],
    "Second souffle (G3), Élan du bûcheron (K8)")
add("G24", 23, "Semelles de vent", "Course plus rapide, élan au sol gardé plus longtemps.", "≫", C_MOUVEMENT, "", False,
    [0.5, 0.8, 1.1, 1.4, 1.7], None, None,
    ["+0.5 m/s", "+0.8 m/s", "+1.1 m/s, perte d'élan 30 % plus lente", "+1.4 m/s", "+1.7 m/s, plafond de l'élan au sol +2 m/s"],
    "Âmes gourmandes (G7), Vif-argent (G26)")
add("G25", 24, "Pas de chat", "Glissade plus longue, wall slide plus lent, wall jump plus fort.", "ᓚ", C_MOUVEMENT, "", False,
    [0.2, 0.2, 0.2, 0.2, 0.2], [0, 1, 1, 2, 2], [1, 1, 0.5, 0.5, 0.5],
    ["glissade −20 % de freinage", "wall jump +1 m/s", "wall slide 2× plus lent, hissage +0.5 m", "wall jump +2 m/s", "wall jump sur le même mur autorisé, les murs rendent aussi une charge"],
    "Griffes grimpantes (K-P2), Plumes (H-P2)")
add("G26", 25, "Vif-argent", "Bunny hop : gain par saut et contrôle en l'air.", "⇗", C_MOUVEMENT, "", False,
    [0.15, 0.25, 0.35, 0.5, 0.6], [0, 0, 0.2, 0.2, 0.2], None,
    ["+15 %", "+25 %", "+35 %, contrôle en l'air +20 %", "+50 %", "+60 %, au-delà de 16 m/s vous traversez les goules en les étourdissant"],
    "Âmes gourmandes (G7), Briseur d'os (G21)")
add("G27", 26, "Chute amortie", "Chute dans le vide moins punitive, atterrissage fort qui frappe autour.", "⤓", C_MOUVEMENT, "", False,
    [12, 8, 4, 0, 0], [0, 0, 15, 15, 30], [0, 2, 3, 3, 4],
    ["−20 → −12 PV", "−8 PV, l'atterrissage fort étourdit à 2 m", "−4 PV, 15 PV de dégâts à 3 m", "0 PV", "l'atterrissage fort explose (30 PV, 4 m) et vous relance d'un petit saut"],
    "Charge de répulsion (G17), Détonateur (G20)")
add("G28", 27, "Allumette", "Un ennemi en feu qui meurt enflamme ses voisins.", "♨", C_UTIL, "", False,
    [2, 2.5, 3, 3.5, 4], [3, 4, 5, 6, 7], [0, 0, 0.2, 0.2, 0.2],
    ["2 m, 3 s", "2.5 m, 4 s", "3 m, 5 s, le feu ralentit 20 %", "3.5 m, 6 s", "4 m, 7 s, le feu étourdit 0.5 s à la première brûlure"],
    "Sillage (G13), lanterne (R1 à R8), Feu grégeois (R5)")
add("G29", 28, "Souffle court", "Le traqueur vous détecte de moins loin et perd votre trace plus vite (sa vitesse ne change pas).", "◍", C_UTIL, "", False,
    [0.10, 0.16, 0.22, 0.28, 0.33], None, None,
    ["−10 %", "−16 %", "−22 %, les goules alertent le traqueur 1 s plus tard", "−28 %", "−33 %, 2 s d'invisibilité pour le traqueur après une parade réussie"],
    "Peau changeante (C-P1), Vision nocturne (H-P1)")
add("G30", 29, "Œil du collectionneur", "Les coffres offrent un choix en plus ; les salles verrouillées sont moins chères.", "◉", C_UTIL, "", False,
    [1, 1, 2, 2, 3], [0, 0.25, 0.25, 0.5, 0.5], None,
    ["+1 choix", "porte −25 %", "+1 choix, les coffres proposent une montée de rang dès l'acte I", "porte −50 %", "+2 choix, un coffre peut offrir une amélioration directement en Argent"],
    "Marchandage (G8)")

# ---------- Renard (10) ----------
add("R-P1", 30, "Fourrure de braise", "Insensible au feu ; un ennemi qui vous frappe s'enflamme.", "◆", C_RENARD, "Renard", True,
    [2, 3, 4, 5, 6], [0, 0, 0.1, 0.1, 0.1], None,
    ["brûlure 2 s", "3 s", "4 s, le coup reçu est réduit de 10 %", "5 s", "6 s, l'ennemi est repoussé de 4 m et étourdi 0.8 s"],
    "Sillage (G13), Flamme large (R2)")
add("R-P2", 31, "Flair", "Coffres, marchand et âmes brillent à travers les murs ; rayon de collecte.", "◆", C_RENARD, "Renard", True,
    [20, 30, 40, 50, 999], [0.10, 0.15, 0.20, 0.25, 0.30], None,
    ["20 m, +10 %", "30 m, +15 %", "40 m, +20 %, la sortie d'acte est marquée dès l'entrée", "50 m, +25 %", "partout, +30 %, une goule qui porte des âmes à moins de 20 m en lâche une quand elle vous voit"],
    "Grande orbe (G5)")
add("R1", 32, "Long souffle", "Portée du jet.", "+1,5", C_RENARD, "Renard", False,
    [1.5, 2.5, 3.5, 4.5, 6], None, None,
    ["+1.5 m", "+2.5 m", "+3.5 m, le jet traverse un objet", "+4.5 m", "+6 m, le jet rebondit sur le décor (une fois)"],
    "Flamme large (R2)")
add("R2", 33, "Flamme large", "Cône du jet.", "<>", C_RENARD, "Renard", False,
    [15, 25, 35, 45, 60], None, None,
    ["+15°", "+25°", "+35°, le jet pousse légèrement (1 m)", "+45°", "+60°, cercle complet autour du renard"],
    "Long souffle (R1), Fourrure de braise (R-P1)")
add("R3", 34, "Mèche vive", "Recharge du jet.", "-0,5", C_RENARD, "Renard", False,
    [0.5, 0.8, 1.1, 1.4, 1.7], [0, 0, 0.5, 0.5, 99], None,
    ["−0.5 s", "−0.8 s", "−1.1 s, un kill en feu rend 0.5 s", "−1.4 s", "−1.7 s, un kill en feu finit la recharge"],
    "Souffle double (R7), Orbe vorace (G9)")
add("R4", 35, "Braises", "Durée de la brûlure ; les brûlures se cumulent (plafond).", "+2 s", C_RENARD, "Renard", False,
    [2, 3, 4, 5, 6], [1, 1, 2, 2, 3], None,
    ["+2 s", "+3 s", "+4 s, 2 brûlures se cumulent", "+5 s", "+6 s, 3 brûlures se cumulent, à 3 l'ennemi est paralysé 1 s"],
    "Allumette (G28), Détonateur (G20)")
add("R5", 36, "Feu grégeois", "Le jet laisse des flaques de feu au sol.", "≈", C_RENARD, "Renard", False,
    [3, 4, 5, 6, 8], [1.5, 2, 2.5, 3, 3.5], [0, 0, 0.3, 0.3, 0.3],
    ["3 s, 1.5 m", "4 s, 2 m", "5 s, 2.5 m, les flaques ralentissent 30 %", "6 s, 3 m", "8 s, 3.5 m, la première entrée dans une flaque étourdit 0.6 s"],
    "Allumette (G28), Briseur d'os (G21)")
add("R6", 37, "Combustion", "Un ennemi qui meurt en feu explose en flammes.", "✹", C_RENARD, "Renard", False,
    [15, 22, 30, 40, 55], [2.5, 3, 3.5, 4, 4.5], None,
    ["15 PV, 2.5 m", "22 PV, 3 m", "30 PV, 3.5 m, les voisins brûlent", "40 PV, 4 m", "55 PV, 4.5 m, chaîne (3 maillons)"],
    "Allumette (G28), Détonateur (G20)")
add("R7", 38, "Souffle double", "Charges de jet.", "⫶", C_RENARD, "Renard", False,
    [1, 1, 1, 2, 2], [0.5, 1, 1, 1, 1], [0, 0, 0.25, 0.25, 0.25],
    ["2 charges (la 2e revient 2× plus lentement)", "la 2e revient à vitesse normale", "2 charges, le 2e jet consécutif +25 % de brûlure", "3 charges", "3 charges, un jet tenu enchaîne les charges sans pause (jet long)"],
    "Mèche vive (R3)")
add("R8", 39, "Foulée ardente", "Au-dessus de 14 m/s, vos pas laissent une traînée de feu.", "〰", C_RENARD, "Renard", False,
    [1.5, 2, 2.5, 3, 4], [6, 6, 9, 9, 12], None,
    ["1.5 s, 6 PV/s", "2 s", "2.5 s, 9 PV/s, aussi pendant une glissade", "3 s", "4 s, 12 PV/s, le dash lâche une boule de feu devant (20 PV, 2.5 m)"],
    "Vif-argent (G26), Sillage (G13), Semelles de vent (G24)")

# ---------- Caméléon (10) ----------
add("C-P1", 40, "Peau changeante", "Immobile et sans tirer, vous vous fondez : les goules vous perdent, le traqueur vous détecte de moins loin.", "◆", C_CAMELEON, "Cameleon", True,
    [1.5, 1.2, 1.0, 0.8, 0.6], [0.3, 0.4, 0.5, 0.6, 0.7], None,
    ["après 1.5 s, −30 % de détection", "1.2 s, −40 %", "1 s, −50 %, les goules proches s'arrêtent de chercher", "0.8 s, −60 %", "0.6 s, −70 %, le premier tir depuis le camouflage est un critique ×3 garanti"],
    "Souffle court (G29)")
add("C-P2", 41, "Œil fendu", "Zone de tête des ennemis plus grande ; les critiques s'enchaînent (2.5 s) : chacun augmente le suivant.", "◆", C_CAMELEON, "Cameleon", True,
    [0.2, 0.3, 0.4, 0.5, 0.6], [0.25, 0.35, 0.45, 0.55, 0.7], [3, 3, 4, 4, 5],
    ["zone +20 %, +0.25 par chaîne (max 3)", "+30 %, +0.35", "+40 %, +0.45 (max 4), 8 % de chance de critique sur tout le corps", "+50 %, +0.55", "+60 %, +0.7 (max 5), 15 % de critique sur le corps, headshot sur un paralysé = kill (goule et volant)"],
    "Ricochet (C4), Balles paralysantes (C1)")
add("C1", 42, "Balles paralysantes", "Chaque touche fige l'ennemi.", "ϟ", C_CAMELEON, "Cameleon", False,
    [0.35, 0.5, 0.65, 0.8, 1.0], None, None,
    ["0.35 s", "0.5 s", "0.65 s, un paralysé prend +15 %", "0.8 s", "1 s, un paralysé touché 3 fois d'affilée est projeté (rebond, G16)"],
    "Détonateur (G20), Briseur d'os (G21), Œil fendu (C-P2)")
add("C2", 43, "Cadavre explosif", "La balle qui tue fait exploser l'ennemi.", "✸", C_CAMELEON, "Cameleon", False,
    [16, 24, 32, 40, 50], [3.5, 4, 4.5, 5, 5.5], [1, 1, 2, 2, 3],
    ["16 PV, 3.5 m", "24 PV, 4 m", "32 PV, 4.5 m, chaîne 2", "40 PV, 5 m", "50 PV, 5.5 m, chaîne 3 et l'explosion paralyse 0.5 s"],
    "Main leste (C5), Détonateur (G20)")
add("C3", 44, "Balle perforante", "La balle traverse des ennemis.", "»", C_CAMELEON, "Cameleon", False,
    [1, 2, 2, 3, 4], [0.75, 0.75, 0.9, 0.9, 1.0], None,
    ["+1 (75 % gardés)", "+2", "+2 (90 % gardés), traverse les objets", "+3", "+4, 100 % gardés, le dernier traversé est paralysé"],
    "Ricochet (C4), Éventail (C7)")
add("C4", 45, "Ricochet", "La balle rebondit vers l'ennemi le plus proche.", "↯", C_CAMELEON, "Cameleon", False,
    [1, 1, 2, 2, 3], [0.6, 0.75, 0.75, 0.9, 1.0], [14, 14, 18, 18, 18],
    ["1 rebond, 60 %", "1, 75 %", "2 rebonds, 75 %, portée 18 m", "2, 90 %", "3 rebonds, 100 %, un ricochet sur un headshot est un headshot"],
    "Perforante (C3), Œil fendu (C-P2)")
add("C5", 46, "Main leste", "Un kill remet des balles ; rechargement plus court.", "↺", C_CAMELEON, "Cameleon", False,
    [2, 2, 3, 4, 4], [0, 0.2, 0.35, 0.35, 0.35], [0, 0, 0, 0, 2],
    ["+2 balles par kill", "rechargement −20 %", "+3 balles, −35 %", "+4 balles", "barillet de 8, un headshot qui tue recharge tout"],
    "Cadavre explosif (C2), Sixième chambre (C6)")
add("C6", 47, "Sixième chambre", "La dernière balle du barillet est spéciale.", "⑥", C_CAMELEON, "Cameleon", False,
    [2, 2.5, 3, 3.5, 4], None, None,
    ["×2 dégâts", "×2.5, étourdit 1 s", "×3, perce tout", "×3.5, explose (4 m)", "×4, si elle tue : rechargement instantané et la suivante est aussi une « sixième » (une fois)"],
    "Main leste (C5), Briseur d'os (G21)")
add("C7", 48, "Éventail", "Maintenir le clic vide le barillet en rafale (cadence ×3, dispersion).", "⋮⋮", C_CAMELEON, "Cameleon", False,
    [6, 5, 4, 3, 2], [0.8, 0.9, 1.0, 1.1, 1.2], None,
    ["dispersion 6°, dégâts ×0.8", "5°, ×0.9", "4°, ×1, la rafale pousse les goules", "3°, ×1.1", "2°, ×1.2, la rafale paralyse et rend 2 balles si elle tue"],
    "Balles paralysantes (C1), Perforante (C3)")
add("C8", 49, "Langue de caméléon", "La balle est un harpon : tirer un objet le ramène à l'orbe (prise à distance).", "⟲", C_CAMELEON, "Cameleon", False,
    [15, 25, 35, 45, 60], None, None,
    ["objets à 15 m", "25 m, l'objet arrive déjà « tenu »", "35 m, aussi les volants (tirés vers vous, étourdis)", "45 m, une goule touchée est tirée de 3 m", "60 m, une goule touchée est tirée jusqu'à vous, étourdie 1 s"],
    "Projectiles lourds (G18), Harpon (G11), Briseur d'os (G21)")

# ---------- Koala (10) ----------
add("K-P1", 50, "Peau épaisse", "Dégâts reçus réduits ; PV max.", "◆", C_KOALA, "Koala", True,
    [0.08, 0.12, 0.16, 0.20, 0.25], [10, 15, 20, 25, 30], None,
    ["−8 %, +10 PV", "−12 %, +15", "−16 %, +20, pas de recul quand vous êtes touché", "−20 %, +25", "−25 %, +30, sous 25 % de PV : coups reçus divisés par 2 et dégâts ×1.3"],
    "Cœur solide (G19)")
add("K-P2", 51, "Griffes grimpantes", "Wall slide plus lent, wall jump plus haut, hissage plus haut.", "◆", C_KOALA, "Koala", True,
    [0.3, 0.3, 0.5, 0.5, 0.5], [0.4, 0.4, 0.8, 0.8, 0.8], [0, 0.5, 0.5, 1, 1],
    ["slide −30 %, hissage +0.4 m", "wall jump +0.5 m", "slide −50 %, hissage +0.8 m, les murs rendent une charge", "wall jump +1 m", "s'accrocher au mur (1 s immobile) puis repartir d'un wall jump à pleine puissance"],
    "Pas de chat (G25)")
add("K1", 52, "Lame longue", "Portée et arc des coups.", "⟋", C_KOALA, "Koala", False,
    [0.4, 0.7, 1.0, 1.3, 1.6], [5, 10, 15, 20, 20], None,
    ["+0.4 m, +5°", "+0.7 m, +10°", "+1 m, +15°, l'aide à la visée porte à 8 m", "+1.3 m, +20°", "+1.6 m, le 3e coup frappe en cercle complet"],
    "Enchaînement (K2)")
add("K2", 53, "Enchaînement", "Combo plus rapide, fenêtre plus longue.", "⋯", C_KOALA, "Koala", False,
    [0.10, 0.18, 0.25, 0.32, 0.40], [0.2, 0.2, 0.4, 0.4, 0.4], None,
    ["−10 %, +0.2 s", "−18 %", "−25 %, +0.4 s, le 3e coup ne casse plus le combo", "−32 %", "−40 %, 4e coup : tourbillon (16 PV, 360°, repousse)"],
    "Lame longue (K1), Élan du bûcheron (K8)")
add("K3", 54, "Fente", "La fente (dash, glissade) va plus loin et frappe plus fort.", "⟶", C_KOALA, "Koala", False,
    [1, 2, 3, 4, 5], [1.3, 1.4, 1.5, 1.6, 1.8], None,
    ["+1 m, ×1.3", "+2 m, ×1.4", "+3 m, ×1.5, traverse un ennemi", "+4 m, ×1.6", "+5 m, ×1.8, la fente traverse tous les ennemis et les étourdit"],
    "Harpon (G11), Briseur d'os (G21)")
add("K4", 55, "Onde de taille", "Un coup porté à plus de 12 m/s envoie une onde tranchante devant.", "⌒", C_KOALA, "Koala", False,
    [6, 8, 10, 12, 14], [10, 14, 18, 24, 30], None,
    ["6 m, 10 PV", "8 m, 14 PV", "10 m, 18 PV, l'onde traverse", "12 m, 24 PV", "14 m, 30 PV, l'onde étourdit 1 s"],
    "Vif-argent (G26), Semelles de vent (G24)")
add("K5", 56, "Os brisé", "Les coups font saigner (dégâts sur la durée).", "♦", C_KOALA, "Koala", False,
    [3, 4, 5, 6, 8], [3, 4, 5, 6, 7], [1, 1, 2, 2, 2],
    ["3 PV/s, 3 s", "4 PV/s, 4 s", "5 PV/s, 5 s, 2 saignements se cumulent", "6 PV/s, 6 s", "8 PV/s, 7 s, un saignant qui meurt vous soigne 8 PV"],
    "Brise-garde (K7) ; saigner et brûler se cumulent (Allumette G28)")
add("K6", 57, "Garde d'os", "La parade avec l'épée : fenêtre plus longue, prochain coup critique après une parade.", "⛨", C_KOALA, "Koala", False,
    [0.03, 0.05, 0.07, 0.09, 0.11], [1.5, 1.75, 2, 2.25, 2.5], None,
    ["+30 ms, ×1.5", "+50 ms, ×1.75", "+70 ms, ×2, la parade frappe (10 PV)", "+90 ms, ×2.25", "+110 ms, ×2.5, riposte automatique : la parade porte le 3e coup"],
    "Riposte (G1), Rythme de fer (G4)")
add("K7", 58, "Brise-garde", "Coups sur un ennemi étourdi, paralysé ou saignant.", "⚔", C_KOALA, "Koala", False,
    [1.3, 1.5, 1.7, 1.9, 2.2], None, None,
    ["×1.3", "×1.5", "×1.7, le coup prolonge l'étourdissement 0.5 s", "×1.9", "×2.2, exécution : un étourdi sous 40 % de PV meurt"],
    "Briseur d'os (G21), Détonateur (G20)")
add("K8", 59, "Élan du bûcheron", "Un kill à l'épée rend une charge de mouvement et +vitesse.", "⇈", C_KOALA, "Koala", False,
    [1, 1.5, 2, 2.5, 3], [2, 2, 3, 3, 4], None,
    ["+1 m/s (2 s)", "+1.5 m/s", "+2 m/s (3 s), aussi un dash gratuit", "+2.5 m/s", "+3 m/s (4 s), un kill rend aussi une charge de traction"],
    "Charge en plus (G23), Prédateur (G22)")

# ---------- Hibou (10) ----------
add("H-P1", 60, "Vision nocturne", "Les ennemis ont un contour visible à travers l'obscurité ; le hibou lit l'état d'un ennemi (PV).", "◆", C_HIBOU, "Hibou", True,
    [20, 30, 40, 50, 999], None, None,
    ["20 m", "30 m", "40 m, et à travers les murs à moins de 15 m", "50 m", "partout, le traqueur est visible à travers les murs à moins de 30 m"],
    "Souffle court (G29), Longue portée (H5)")
add("H-P2", 61, "Plumes", "Chute ralentie en tenant saut (plané), pas de dégâts de chute.", "◆", C_HIBOU, "Hibou", True,
    [0.7, 0.55, 0.4, 0.3, 0.3], None, None,
    ["plané ×0.7 de gravité", "×0.55", "×0.4, le plané garde la vitesse horizontale", "×0.3", "+1 saut en l'air et le plané dirige (contrôle complet)"],
    "Pas de chat (G25)")
add("H1", 62, "Lien multiple", "Liens simultanés sur plusieurs ennemis.", "⫘", C_HIBOU, "Hibou", False,
    [2, 2, 3, 3, 4], [0.6, 1, 1, 1, 1], [0, 0, 0, 0.1, 0.1],
    ["2", "2, chacun à 100 %", "3", "3, +10 % de drain par lien", "4, tous les liés partagent les dégâts reçus (dégâts sur l'un = 30 % sur les autres)"],
    "Chaînage (H2), Lien vorace (H3)")
add("H2", 63, "Chaînage", "Le drain saute à l'ennemi le plus proche de la cible.", "⛓", C_HIBOU, "Hibou", False,
    [1, 1, 2, 2, 3], [0.5, 0.65, 0.75, 0.9, 1.0], [6, 8, 10, 10, 12],
    ["1 saut, 6 m, 50 %", "1 saut, 8 m, 65 %", "2 sauts, 10 m, 75 %", "2 sauts, 90 %", "3 sauts, 12 m, 100 %, la cible morte passe le lien au suivant sans recharge"],
    "Lien multiple (H1), Pages brûlantes (H7)")
add("H3", 64, "Lien vorace", "Drain par seconde.", "⇣", C_HIBOU, "Hibou", False,
    [4, 7, 10, 13, 16], [0, 0, 0.25, 0.25, 0.5], None,
    ["+4 PV/s", "+7", "+10, la cible ralentit 25 %", "+13", "+16, la cible ralentit 50 % et ne peut plus bondir"],
    "Sangsue (H4), Lien d'ancrage (H6)")
add("H4", 65, "Sangsue", "Part du drain rendue au hibou.", "♥", C_HIBOU, "Hibou", False,
    [0.3, 0.4, 0.5, 0.6, 0.75], [0, 0, 20, 20, 40], None,
    ["30 %", "40 %", "50 %, le surplus au-dessus du max devient un bouclier (max +20)", "60 %", "75 %, bouclier max +40, il absorbe un coup du traqueur dans l'arène du boss"],
    "Lien vorace (H3), Cœur solide (G19)")
add("H5", 66, "Longue portée", "Portée de lancement et de maintien.", "⟷", C_HIBOU, "Hibou", False,
    [5, 8, 12, 15, 20], [0.5, 0.5, 1.5, 1.5, 1.5], None,
    ["+5 m / +5 m", "+8 / +8", "+12 / +12, la vue peut se perdre 1.5 s", "+15 / +15", "+20 / +20, le lien traverse les murs 1 s"],
    "Vision nocturne (H-P1)")
add("H6", 67, "Lien d'ancrage", "La cible liée sert d'ancre de traction : vous pouvez vous tracter vers elle.", "⚓", C_HIBOU, "Hibou", False,
    [0, 0.3, 0.3, 0.3, 0.3], [0, 0, 20, 20, 20], [0, 0, 0, 2, 2],
    ["traction vers la cible", "la cible est freinée de 30 %", "la traction arrive avec un coup (20 PV)", "la cible est tirée de 2 m vers vous à l'accroche", "la cible est clouée au sol 2 s à l'accroche"],
    "Harpon (G11), Élan de traction (G12)")
add("H7", 68, "Pages brûlantes", "Un ennemi qui meurt sous le lien lâche des âmes en plus et rend la recharge.", "✦", C_HIBOU, "Hibou", False,
    [1, 1, 2, 2, 3], [0.5, 0.75, 1, 1, 1], None,
    ["+1 âme, recharge −50 %", "+1, −75 %", "+2, −100 %, le lien se relance sur le voisin (6 m)", "+2", "+3, la mort libère une onde qui étourdit (4 m)"],
    "Chaînage (H2), Âmes gourmandes (G7)")
add("H8", 69, "Soin lié", "Le soin d'allié est plus fort ; pendant un drain, vous régénérez aussi.", "✚", C_HIBOU, "Hibou", False,
    [10, 12, 14, 16, 20], [1, 2, 3, 4, 6], None,
    ["soin 10 PV/s, +1 PV/s pour vous", "12 PV/s, +2", "14 PV/s, +3, le soin a 2 charges", "16 PV/s, +4", "20 PV/s, +6, le soin ressuscite un allié tombé (coop) une fois par acte"],
    "Sangsue (H4), Cœur solide (G19)")

assert len(U) == 70, len(U)
assert len({u["kind"] for u in U}) == 70
assert len({u["name"] for u in U}) == 70


def guid_for(code):
    if code in LEGACY:
        return LEGACY[code]
    return hashlib.md5(("ItHunts.Upgrade." + code).encode("utf-8")).hexdigest()


def ystr(s):
    """Chaîne YAML entre guillemets, échappée comme Unity l'écrit (\\xHH, \\uHHHH)."""
    out = []
    for ch in s:
        o = ord(ch)
        if ch == "\\":
            out.append("\\\\")
        elif ch == '"':
            out.append('\\"')
        elif 32 <= o < 127:
            out.append(ch)
        elif o < 256:
            out.append("\\x%02X" % o)
        elif o < 0x10000:
            out.append("\\u%04X" % o)
        else:
            out.append("\\U%08X" % o)
    return '"' + "".join(out) + '"'


def ynum(v):
    if isinstance(v, bool):
        return "1" if v else "0"
    if isinstance(v, int):
        return str(v)
    r = repr(float(v))
    return r[:-2] if r.endswith(".0") else r


def ylist(vals):
    if not vals:
        return " []\n"
    return "\n" + "".join("  - %s\n" % ynum(v) for v in vals)


def ystrlist(vals):
    if not vals:
        return " []\n"
    return "\n" + "".join("  - %s\n" % ystr(v) for v in vals)


HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n--- !u!114 &11400000\nMonoBehaviour:\n  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n  m_GameObject: {fileID: 0}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n"


def asset_yaml(u, file_name):
    c = u["color"]
    y = HEADER
    y += "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n" % SCRIPT_GUID
    y += "  m_Name: %s\n" % file_name
    y += "  m_EditorClassIdentifier: ItHunts.Runtime::ItHunts.Settings.UpgradeDefinition\n"
    y += "  code: %s\n" % ystr(u["code"])
    y += "  displayName: %s\n" % ystr(u["name"])
    y += "  description: %s\n" % ystr(u["desc"])
    y += "  icon: {fileID: 0}\n"
    y += "  iconSymbol: %s\n" % ystr(u["sym"])
    y += "  color: {r: %s, g: %s, b: %s, a: 1}\n" % (ynum(c[0]), ynum(c[1]), ynum(c[2]))
    y += "  kind: %d\n" % u["kind"]
    y += "  character: %d\n" % CHAR[u["char"]]
    y += "  isPassive: %d\n" % (1 if u["passive"] else 0)
    y += "  rankValues:" + ylist(u["v1"])
    y += "  rankValues2:" + ylist(u["v2"] or [])
    y += "  rankValues3:" + ylist(u["v3"] or [])
    y += "  rankTexts:" + ystrlist(u["texts"])
    y += "  synergies: %s\n" % ystr(u["syn"])
    y += "  amount: %s\n" % ynum(u["v1"][0])
    y += "  forArtifact: {fileID: 0}\n"
    y += "  rarity: 0\n"
    y += "  merchantPrice: 60\n"
    return y


def meta_yaml(guid):
    return ("fileFormatVersion: 2\nguid: %s\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 11400000\n"
            "  userData: \n  assetBundleName: \n  assetBundleVariant: \n" % guid)


def pool_yaml(name, guids):
    y = HEADER
    y += "  m_Script: {fileID: 11500000, guid: %s, type: 3}\n" % POOL_SCRIPT_GUID
    y += "  m_Name: %s\n" % name
    y += "  m_EditorClassIdentifier: ItHunts.Runtime::ItHunts.Settings.UpgradePool\n"
    y += "  upgrades:\n"
    for g in guids:
        y += "  - {fileID: 11400000, guid: %s, type: 2}\n" % g
    return y


def write(path, text):
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def set_field(path, field, value_line):
    """Remplace (ou ajoute après m_EditorClassIdentifier) une ligne « field: … » d'un asset YAML."""
    with open(path, "r", encoding="utf-8") as f:
        lines = f.read().split("\n")
    done = False
    for i, line in enumerate(lines):
        if line.startswith("  %s:" % field):
            lines[i] = "  %s: %s" % (field, value_line)
            done = True
            break
    if not done:
        for i, line in enumerate(lines):
            if line.startswith("  m_EditorClassIdentifier:"):
                lines.insert(i + 1, "  %s: %s" % (field, value_line))
                break
    write(path, "\n".join(lines))


def main():
    os.makedirs(UPGRADES_DIR, exist_ok=True)
    keep = set()
    by_char = {k: [] for k in CHAR}
    for u in U:
        file_name = "Upgrade_" + u["code"]
        guid = guid_for(u["code"])
        write(os.path.join(UPGRADES_DIR, file_name + ".asset"), asset_yaml(u, file_name))
        write(os.path.join(UPGRADES_DIR, file_name + ".asset.meta"), meta_yaml(guid))
        keep.add(file_name + ".asset")
        keep.add(file_name + ".asset.meta")
        by_char[u["char"]].append(guid)
    removed = 0
    for f in os.listdir(UPGRADES_DIR):
        if f.startswith("Upgrade_") and f not in keep:
            os.remove(os.path.join(UPGRADES_DIR, f))
            removed += 1

    # Pools : génériques + un par personnage.
    for key, g in POOL_GUIDS.items():
        if g is None:
            POOL_GUIDS[key] = hashlib.md5(("ItHunts.UpgradePool." + key).encode("utf-8")).hexdigest()
    pools = {"Default": by_char[""], "Renard": by_char["Renard"], "Cameleon": by_char["Cameleon"], "Koala": by_char["Koala"], "Hibou": by_char["Hibou"]}
    for key, guids in pools.items():
        name = "UpgradePool_" + key
        write(os.path.join(SETTINGS_DIR, name + ".asset"), pool_yaml(name, guids))
        write(os.path.join(SETTINGS_DIR, name + ".asset.meta"), meta_yaml(POOL_GUIDS[key]))
    for old in OLD_POOLS:
        for ext in (".asset", ".asset.meta"):
            p = os.path.join(SETTINGS_DIR, old + ext)
            if os.path.exists(p):
                os.remove(p)

    # Branchements : personnages (CharacterDefinition.upgrades) et artefacts (ArtifactDefinition.upgrades, marchand de l'artefact).
    ref = lambda key: "{fileID: 11400000, guid: %s, type: 2}" % POOL_GUIDS[key]
    for char, key in (("renard", "Renard"), ("cameleon", "Cameleon"), ("koala", "Koala"), ("hibou", "Hibou")):
        set_field(os.path.join(SETTINGS_DIR, "Personnages", "Character_%s.asset" % char), "upgrades", ref(key))
    for art, key in (("Barrel_Default", "Cameleon"), ("FireLantern_Default", "Renard"), ("BoneSword_Default", "Koala"), ("SpiritBook_Default", "Hibou")):
        set_field(os.path.join(SETTINGS_DIR, art + ".asset"), "upgrades", ref(key))
    print("70 assets écrits, %d anciens supprimés ; pools : %s" % (removed, ", ".join("%s %d" % (k, len(v)) for k, v in pools.items())))


if __name__ == "__main__":
    main()
