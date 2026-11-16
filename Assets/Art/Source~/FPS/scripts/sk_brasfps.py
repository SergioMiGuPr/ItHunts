# -*- coding: utf-8 -*-
"""SK_BrasFPS_<Perso>_D / _G : bras de la vue FPS des 4 personnages (renard, koala, caméléon, hibou), du coude au bout
des doigts, d'après planche_bras_fps_v01 (rangée du haut : les 4 avant-bras ; rangée du bas : les poses, sur le renard).

Mesures : les mains greybox d'ART-HANDOFF § 5 bis (repère de la main : origine au poignet, doigts vers +z, dos de la
main vers +y ; avant-bras de z −0.27 à −0.01, Ø 0.06 ; paume 0.085 × 0.03 × 0.09 ; articulation des doigts à z 0.09,
doigts de 0.07 ; pouce côté intérieur, à z 0.05, tourné de 30°). La taille des mains suit la greybox.

Squelette commun aux 4 personnages et aux deux côtés (mêmes noms d'os) : Avant_Bras, Poignet, Pouce_1 à _3,
Index_1 à _3, Majeur_1 à _3, Annulaire_1 à _3, Auriculaire_1 à _3 (21 os). Les serres du hibou suivent les os des
doigts. Le bras gauche est le miroir exact du droit (maillage et os, roll symétrique) ; ses poses sont le miroir des
poses du droit, calculé par le script : les poses sont définies une fois.

Objets : armature « BrasFPS » (racine du FBX ; pivot au bout coupé de la manche, au coude), maillage
SK_BrasFPS_<Perso>_<D|G> ; repères vides enfants de l'armature (fixes, comme les repères de la greybox) :
Socket_Poignet (au poignet, axes de l'ancrage d'ART-HANDOFF : +Z = doigts, +Y = dos de la main), Socket_Orbe (main
droite, centre de l'orbe de FpsView), Socket_Artefact (main gauche, repère du barillet d'ART-HANDOFF) et
Socket_Artefact_Anneau (main gauche, centre de l'anneau de la lanterne tenu dans Pose_PriseAnneau).

Poses (actions, mêmes noms pour les 4 personnages et les deux côtés) : Pose_Repos, Pose_OrbePaume, Pose_Pistolet,
Pose_PriseAnneau, Pose_Traction, Pose_Repulsion, Pose_Attraper (3 clés : ouverte puis refermée), Pose_TenirObjet,
Pose_Lancer, Pose_Poing, et 4 boucles Boucle_Repos, Boucle_OrbePaume, Boucle_Pistolet, Boucle_PriseAnneau (respiration
de 3.5 s, doigts qui ondulent ; poses de flottement : paume vers +Y, rien n'est tenu). Elles portent la forme de la main (poignet, doigts, rotation de l'avant-bras pour la paume
vers le haut) ; les déplacements de la main entière (bras tendu, recul, poussée) restent dans le code (FirstPersonView).

Un FBX par bras (Assets/Art/Models/FPS/SK_BrasFPS_<Perso>_<D|G>.fbx) : deux squelettes aux mêmes noms dans un même FBX
recevraient chacun les actions de l'autre.

Lancer dans Blender :
    p = r"<dépôt>/Assets/Art/Source~/FPS/scripts/sk_brasfps.py"
    exec(compile(open(p, encoding="utf-8").read(), p, "exec"), {"__file__": p, "__name__": "__main__"})
"""
import sys, os, math
sys.dont_write_bytecode = True
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
sys.modules.pop("fps_common", None)
import fps_common as F
C = F.C
sys.modules.pop("peinture_bras", None)
import peinture_bras as PB
import numpy as np
TEX = 2048
TEX_DIR = os.path.join(C.ROOT, "Assets", "Art", "Textures", "FPS")
# valeur minimale par matière (lisibilité dans l'ambiance sombre, sans émission)
LIFT = {"gant": 0.38, "cuir": 0.34, "sangle": 0.22, "manche": 0.30, "lisere": 0.34, "ourlet": 0.4, "peau": 0.5, "griffe": 0.12,
        "fond": 0.0, "bandage": 0.6, "rayure1": 0.4, "rayure2": 0.3}
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion

ELBOW = -0.27          # coude (bout coupé de la manche), repère de la main
# L'armature, le maillage et les repères sont stockés dans les axes du FBX (Y en haut, Z vers l'avant) ; l'objet armature
# est tourné de +90° autour de X dans Blender, ce que la conversion d'axes de l'export annule : racine à rotation nulle.
RX = Matrix.Rotation(math.radians(-90), 4, "X")         # vue Blender (UH) -> axes du fichier
PERSOS = ("Renard", "Koala", "Cameleon", "Hibou")

# ----------------------------------------------------------------------------- styles (couleurs relevées sur la planche)
STYLE = {
    "Renard": dict(manche="#6A2226", lisere="#3A1C1E",      # manche rouge déchirée (reference_bras_fps)
                   bandage="#C9BCA6", cuir="#3A2A24", sangle="#2A1E1A", boucle="#B08A3A",
                   gant="#4A3329", peau="#C9502E", griffe="#171313", sans_doigts=True, griffes=0.012,
                   couture="#6A5A54", poil="#B8492A", extras=("bandage", "lisere", "poil")),
    "Koala": dict(manche="#4A4B38", lisere="#6E8A3E", fourrure="#C8C4BE", cuir="#5A4134", sangle="#3E2C24", boucle="#B08A3A",
                  gant="#3D433A", peau="#3D433A", griffe="#1B1B19", sans_doigts=False, griffes=0.010, couture="#6B7060",
                  extras=("fourrure", "lisere")),
    "Cameleon": dict(manche="#5A2424", ourlet="#D2581E", rayure1="#7E9A56", rayure2="#3C5A2C", cuir="#5A3E32", sangle="#3E2A22",
                     boucle="#B08A3A", gant="#45414A", peau="#8DAA5A", griffe="#4E6B2E", sans_doigts=True, griffes=0.0,
                     couture="#77717C", ecaille="#6F8E45",
                     extras=("ourlet", "rayures"), doigts_fins=0.85),
    "Hibou": dict(plume1="#B19B85", plume2="#615048", duvet="#D6CBB6", cuir="#4A362C", sangle="#33251F", boucle="#B08A3A",
                  peau="#CFC4AE", griffe="#141111", sans_doigts=False, griffes=0.022, serres=True, ecaille="#B3A68E",
                  extras=("plumes", "duvet"), doigts_fins=0.85),
}

# ----------------------------------------------------------------------------- squelette (main droite, repère de la main)
KNUCKLE_Z = 0.088
FINGERS = [  # nom, x (main droite : pouce côté x négatif), longueur, y de l'articulation, écart (signe)
    ("Index", -0.0285, 0.066, 0.002, 1.0),
    ("Majeur", -0.0095, 0.072, 0.003, 0.3),
    ("Annulaire", 0.0095, 0.067, 0.002, -0.5),
    ("Auriculaire", 0.0285, 0.055, 0.0, -1.0),
]
PHAL = (0.45, 0.31, 0.24)
# Repos : doigts fléchis de REST_CURL degrés à chaque articulation (jointure, 2e, 3e phalange). Un pli de 90° ne demande
# alors que 70° environ de rotation, ce qui limite l'écrasement du skinning linéaire d'Unity. Les poses sont données en
# angles absolus (comme avant) : fingers_pose retire le pli de repos.
REST_CURL = (16.0, 20.0, 12.0)
FAN = {"Index": -6.0, "Majeur": -2.0, "Annulaire": 2.5, "Auriculaire": 7.0}      # degrés, vers le pouce < 0
THUMB = [(-0.033, -0.004, 0.018), (-0.051, -0.006, 0.043), (-0.061, -0.007, 0.064), (-0.066, -0.008, 0.082)]   # sur le côté de la paume


def UH(x, y, z):
    """Repère de la main (Unity) -> Blender, modèle : origine au coude (bout coupé de la manche)."""
    return Vector((-x, -(z - ELBOW), y))


def bones_right():
    b = {"Avant_Bras": (UH(0, 0, ELBOW), UH(0, 0, 0), None), "Poignet": (UH(0, 0, 0), UH(0, 0, KNUCKLE_Z), "Avant_Bras")}
    for name, x, L, y, _ in FINGERS:
        fa = math.radians(FAN[name])                 # doigts en léger éventail au repos : séparés dès la 1re phalange
        px, py, pz = x, y, KNUCKLE_Z
        prev = "Poignet"
        phi = 0.0
        for i, f in enumerate(PHAL):
            phi += math.radians(REST_CURL[i])         # et légèrement fléchis (moins de pincement en flexion)
            l = L * f
            nx, ny, nz = px + math.sin(fa) * l, py - 0.0015 - math.cos(fa) * l * math.sin(phi), pz + math.cos(fa) * l * math.cos(phi)
            b["%s_%d" % (name, i + 1)] = (UH(px, py, pz), UH(nx, ny, nz), prev)
            prev = "%s_%d" % (name, i + 1)
            px, py, pz = nx, ny, nz
    prev = "Poignet"
    for i in range(3):
        b["Pouce_%d" % (i + 1)] = (UH(*THUMB[i]), UH(*THUMB[i + 1]), prev)
        prev = "Pouce_%d" % (i + 1)
    return b


# ----------------------------------------------------------------------------- géométrie
def ring(cz, rx, ry, n, phase=0.0, yoff=0.0, jag=None, droop=0.0):
    """Anneau autour de l'axe de l'avant-bras (repère de la main) ; jag(k) décale z (bord déchiré), droop allonge le
    dessous (côté paume, y < 0)."""
    pts = []
    for k in range(n):
        a = phase + 2 * math.pi * k / n
        x, y = rx * math.cos(a), ry * math.sin(a) + yoff
        z = cz - (jag(k) if jag else 0.0) - droop * max(0.0, -math.sin(a))
        pts.append(UH(x, y, z))
    return pts


# ----------------------------------------------------------------------------- géométrie (repère de la main)
# Le bras est un maillage d'un seul tenant (retours du 2026-11-16) : manche, brassard, gant, paume, doigts et pouce sont
# une seule surface fermée, soudée, sans coupure aux articulations. Anneaux de NB sommets du coude aux jointures ; à
# l'extrémité de la paume, chaque doigt part d'un « port » de NF sommets (4 sur le dos, 4 sous la paume, 2 sur les côtés,
# partagés avec le doigt voisin : la palmure) ; le pouce part d'un trou de NF sommets percé sur le côté de la paume.
# Seuls les décors restent des coques à part, collées au bras : sangles, boucles, poils, plumes, liserés, griffes. Ils
# reçoivent les poids du point du bras le plus proche (ou de la dernière phalange pour les griffes).
NB = 34
NF = 10
JAG18 = (0.0, 0.012, 0.004, 0.019, 0.002, 0.011, 0.006, 0.016, 0.0, 0.009, 0.014, 0.003, 0.018, 0.007, 0.001, 0.013, 0.005, 0.017)
REG_ARM, REG_PALM, REG_THUMB = 1, 2, 20          # régions du corps (doigts : 10 + indice du doigt)


def ang(k, n):
    """Angle du sommet k d'un anneau de n : 0 = côté pouce (x < 0), puis le dos de la main (y > 0), puis la paume."""
    return math.pi - 2 * math.pi * k / n


def hv(p):
    return Vector(p)


def circ_ring(z, rx, ry, jag=False, droop=0.0, fold=None):
    pts = []
    for k in range(NB):
        a = ang(k, NB)
        c, s = math.cos(a), math.sin(a)
        f = 1.0
        if fold is not None:              # plis en biais de la manche, décalés d'un anneau à l'autre
            f = 1 + 0.055 * math.sin(3 * a + 1.3 * fold) + 0.03 * math.sin(7 * a - 2.1 * fold) + 0.015 * math.sin(11 * a + fold)
        zz = z - (JAG18[(k * 18) // NB] if jag else 0.0) - droop * max(0.0, -s)
        pts.append(Vector((rx * f * c, ry * f * 0.95 * s if fold is not None else ry * s, zz)))
    return pts


def palm_sec(z, w, tb, tp):
    pts = []
    for k in range(NB):
        a = ang(k, NB)
        c, s = math.cos(a), math.sin(a)
        x = w / 2 * math.copysign(abs(c) ** 0.78, c)
        y = (tb if s > 0 else tp) * math.copysign(abs(s) ** 0.8, s)
        pts.append(Vector((x, y, z)))
    return pts


def frame(d, up=Vector((0, 1, 0))):
    d = d.normalized()
    side = up.cross(d)
    if side.length < 1e-6:
        side = Vector((1, 0, 0))
    side.normalize()
    return d, side, d.cross(side).normalized()


def tube_ring(c, d, r, n=NF, up_f=0.88, down_f=0.9):
    d, side, u = frame(d)
    pts = []
    for j in range(n):
        a = ang(j, n)
        s = math.sin(a)
        pts.append(c + side * r * math.cos(a) + u * r * (up_f if s > 0 else down_f) * s)
    return pts


def chain_hand(bones, chain):
    """Points (repère de la main) de la chaîne : tête du 1er os puis la queue de chaque os."""
    pts = [hs(bones[chain[0]][0])] + [hs(bones[b][1]) for b in chain]
    return pts


def hs(vb):
    """Blender (modèle) -> repère de la main (inverse de UH)."""
    return Vector((-vb[0], vb[2], ELBOW - vb[1]))


def finger_profile(pts, rj, start, glove_to, bour):
    """Échantillons (point, rayon, seg, direction) le long d'une chaîne de 3 phalanges : plis aux articulations (anneau
    sur l'articulation, renflements de part et d'autre), phalange plus fine au milieu, bout rond. seg = i + t."""
    S = []
    for i in range(3):
        if i == 0:
            ts = ([(start, 1.08), (start + (1 - start) * 0.4, 0.96), (start + (1 - start) * 0.72, 0.97)] if start < 0.3
                  else [(start, 1.08), (start + (1 - start) * 0.45, 0.98)])
        else:
            ts = [(-0.08, 1.1), (0.0, 1.02), (0.08, 1.1), (0.4, 0.92), (0.72, 0.96)]
        for t, f in ts:
            S.append((i + t, f))
    if bour is not None:                                  # bourrelet du gant sans doigts (bord roulé)
        S = [s for s in S if not (glove_to - 0.05 < s[0] < glove_to + 0.14)]
        S += [(glove_to - 0.03, 1.02), (glove_to, 1.17), (glove_to + 0.08, 1.12), (glove_to + 0.13, 0.98)]
        S.sort()
    out = []
    for seg, f in S:
        i = min(int(math.floor(seg + 1e-9)), 2) if seg >= 0 else 0
        t = seg - i
        a, b = pts[i], pts[i + 1]
        p = a.lerp(b, t)
        r = (rj[i] + (rj[i + 1] - rj[i]) * max(0.0, t)) * f
        out.append((p, r, seg, (b - a).normalized()))
    d = (pts[3] - pts[2]).normalized()
    for t, f in ((0.0, 1.04), (0.35, 0.97), (0.65, 0.8), (0.88, 0.5)):      # bout rond (pulpe)
        out.append((pts[3] + d * rj[3] * t, rj[3] * f, 3.0, d))
    # directions lissées (tangente du chemin)
    P = [o[0] for o in out]
    res = []
    for k, (p, r, seg, d0) in enumerate(out):
        t = (P[min(k + 1, len(P) - 1)] - P[max(k - 1, 0)])
        res.append((p, r, seg, t.normalized() if t.length > 1e-9 else d0))
    tip = pts[3] + d * rj[3] * 1.05
    return res, tip


class Arm:
    def __init__(self, perso):
        self.p = perso
        self.st = STYLE[perso]
        self.B = C.Builder()
        self.reg = self.B.bm.verts.layers.int.new("region")
        self.parts = []            # (début, fin, os candidats | "BODY" | "COPY")
        self.mats = {}
        self.body_faces = []
        self.hole_center = None

    def mat(self, key, hexa):
        name = "M_BrasFPS_%s_%s" % (self.p, key.capitalize())
        if key not in self.mats:
            self.mats[key] = F.flat_material(name, hexa)
            self.B.set_uv_params(key, 0.05, 0.05)
        return key

    def part(self, bones, fn):
        self.B.bm.verts.ensure_lookup_table()
        a = len(self.B.bm.verts)
        fn()
        self.B.bm.verts.ensure_lookup_table()
        self.parts.append((a, len(self.B.bm.verts), bones))

    def sweep(self, rings, key, caps=(False, False)):
        self.B.sweep(rings, key, caps=caps)

    # ---- corps d'un seul tenant
    def bf(self, pts, key, nrm, tag):
        """Face du corps : points dans le repère de la main."""
        f = self.B.face([UH(*p) for p in pts], key, UH(*nrm) - UH(0, 0, 0))
        if f is not None:
            self.body_faces.append(f)
            for v in f.verts:
                if v[self.reg] == 0:
                    v[self.reg] = tag
        return f

    def band(self, ra, rb, key, tag, keys=None):
        n = len(ra)
        ca, cb = sum(ra, Vector()) / n, sum(rb, Vector()) / n
        for k in range(n):
            k2 = (k + 1) % n
            q = [ra[k], ra[k2], rb[k2], rb[k]]
            self.bf(q, keys[k] if keys else key, sum(q, Vector()) / 4 - (ca + cb) / 2, tag)

    def bridge(self, loop, ring, key, tag):
        """Relie une boucle de bord (trou) à un anneau de même taille, avec le décalage qui tord le moins."""
        n = len(loop)
        best = None
        for rr in (ring, ring[::-1]):
            for o in range(n):
                s = sum((loop[j] - rr[(j + o) % n]).length for j in range(n))
                if best is None or s < best[0]:
                    best = (s, [rr[(j + o) % n] for j in range(n)])
        rb = best[1]
        c = (sum(loop, Vector()) + sum(rb, Vector())) / (2 * n)
        for j in range(n):
            j2 = (j + 1) % n
            q = [loop[j], loop[j2], rb[j2], rb[j]]
            self.bf(q, key, sum(q, Vector()) / 4 - c, tag)

    def body(self, bones):
        st = self.st
        gk = self.mat("gant", st["gant"]) if "gant" in st else self.mat("peau", st["peau"])
        skin = self.mat("peau", st["peau"])
        ck = self.mat("cuir", st["cuir"])
        scale = self.mat("ecaille", st["ecaille"]) if "ecaille" in st else None
        fond = self.mat("fond", "#120E0E")
        fin = st.get("doigts_fins", 1.0)
        self.B.new_shell()
        # -- avant-bras : manche (ou corps sous les plumes), ourlet replié vers l'intérieur, brassard, manchette du gant
        rings, keys = [], []
        if "plumes" in st.get("extras", ()):
            k2 = self.mat("plume2", st["plume2"])
            for z, r in ((ELBOW, 0.036), (-0.23, 0.036), (-0.2, 0.0355), (-0.175, 0.0345)):
                rings.append(circ_ring(z, r, r * 0.94)); keys.append(k2)
        else:
            mk = self.mat("manche", st["manche"])
            lip = self.mat("ourlet", st["ourlet"]) if "ourlet" in st else mk
            zs = [ELBOW, -0.257, -0.243, -0.229, -0.215, -0.2, -0.185, -0.172]
            rs = [0.043, 0.0428, 0.0425, 0.042, 0.0408, 0.0398, 0.0388, 0.0383]
            for i, (z, r) in enumerate(zip(zs, rs)):
                rings.append(circ_ring(z, r, r, fold=i)); keys.append(mk)
            rings.append(circ_ring(-0.16, 0.038, 0.036, jag=True, droop=0.02)); keys.append(lip)      # ourlet déchiré
            rings.append(circ_ring(-0.166, 0.0348, 0.0335, jag=True, droop=0.02)); keys.append(ck)    # doublure (repli)
        for z, r in ((-0.158, 0.0336), (-0.12, 0.0325), (-0.085, 0.0318), (-0.06, 0.0311), (-0.047, 0.0308)):
            rings.append(circ_ring(z, r, r * 0.92)); keys.append(ck)
        keys[-1] = gk
        for z, r in ((-0.044, 0.0322), (-0.037, 0.0329), (-0.027, 0.0318), (-0.013, 0.0306)):
            rings.append(circ_ring(z, r, r * 0.9)); keys.append(gk)
        # -- paume : coupes arrondies ; la dernière se rapproche des ports des doigts
        secs = [palm_sec(0.002, 0.058, 0.014, 0.013), palm_sec(0.015, 0.068, 0.0155, 0.0145), palm_sec(0.03, 0.076, 0.0165, 0.0158),
                palm_sec(0.048, 0.082, 0.0168, 0.0155), palm_sec(0.065, 0.086, 0.0165, 0.0145), palm_sec(0.078, 0.086, 0.016, 0.013)]
        zk = KNUCKLE_Z
        ports = []
        for i, (name, x, L, y, s) in enumerate(FINGERS):
            pr = []
            for j in range(NF):
                a = ang(j, NF)
                c, sn = math.cos(a), math.sin(a)
                p = Vector((x + 0.0095 * c, (0.0128 if sn > 0 else 0.0118) * sn, zk))
                if (j == 0 and i > 0) or (j == 5 and i < 3):       # palmure entre deux doigts : un peu plus en avant
                    p = Vector((p.x, -0.002, zk + 0.007))
                pr.append(p)
            ports.append(pr)
        knu = [None] * NB
        knu[0] = ports[0][0]
        knu[17] = ports[3][5]
        for i in range(4):
            for q in range(4):
                knu[1 + 4 * i + q] = ports[i][1 + q]             # dos de la main
                knu[33 - (4 * i + q)] = ports[i][9 - q]          # paume
        secs[-1] = [p.lerp(Vector((kp.x, kp.y * 1.15, p.z)), 0.45) for p, kp in zip(secs[-1], knu)]
        # tube du coude à la paume
        allr = rings + secs
        for i in range(len(allr) - 1):
            tag = REG_ARM if i < len(rings) - 1 else REG_PALM
            key = keys[i] if i < len(rings) - 1 else gk
            sa, sb = allr[i], allr[i + 1]
            # trou du pouce : faces des colonnes 32, 33, 0 entre les coupes 1-2 et 2-3 de la paume
            hole_rows = (len(rings) + 1, len(rings) + 2)
            n = NB
            ca, cb = sum(sa, Vector()) / n, sum(sb, Vector()) / n
            for k in range(n):
                if i in hole_rows and k in (32, 33, 0):
                    continue
                k2 = (k + 1) % n
                qd = [sa[k], sa[k2], sb[k2], sb[k]]
                self.bf(qd, key, sum(qd, Vector()) / 4 - (ca + cb) / 2, tag)
        self.bf(rings[0], fond if "plumes" not in st.get("extras", ()) else keys[0], Vector((0, 0, -1)), REG_ARM)   # fond (coude)
        # jointures : bande paume -> ports, triangles de palmure
        self.band(secs[-1], knu, gk, REG_PALM)
        for i in range(3):
            w = ports[i][5]
            self.bf([knu[4 + 4 * i], knu[5 + 4 * i], w], gk, Vector((0, 1, 0.3)), REG_PALM)
            self.bf([knu[29 - 4 * i], knu[30 - 4 * i], w], gk, Vector((0, -1, 0.3)), REG_PALM)
        # -- doigts
        glove_to = None
        for i, (name, x, L, y, s) in enumerate(FINGERS):
            chain = ["%s_%d" % (name, k) for k in (1, 2, 3)]
            pts = chain_hand(bones, chain)
            r = 0.0098 * fin
            rj = [r * 1.04, r * 0.94, r * 0.86, r * 0.78]
            gt = (0.6 if st["sans_doigts"] else (9.0 if "gant" in st else -1.0))
            prof, tip = finger_profile(pts, rj, 0.12, gt, gt if st["sans_doigts"] else None)
            self.tube(ports[i], prof, tip, 10 + i, gk, skin, scale, gt)
        # -- pouce : trou sur le côté de la paume
        A, Bs, Cs = secs[1], secs[2], secs[3]
        loop = [A[32], A[33], A[0], A[1], Bs[1], Cs[1], Cs[0], Cs[33], Cs[32], Bs[32]]
        self.hole_center = sum(loop, Vector()) / len(loop)
        chain = ["Pouce_1", "Pouce_2", "Pouce_3"]
        pts = chain_hand(bones, chain)
        r = 0.0106 * fin
        rj = [r * 1.25, r * 1.0, r * 0.92, r * 0.8]
        gt = 0.66 if st["sans_doigts"] else (9.0 if "gant" in st else -1.0)
        prof, tip = finger_profile(pts, rj, 0.55, gt, gt if st["sans_doigts"] else None)
        self.tube(loop, prof, tip, REG_THUMB, gk, skin, scale, gt, bridge=True)
        bmesh.ops.recalc_face_normals(self.B.bm, faces=[f for f in self.body_faces if f.is_valid])

    def tube(self, port, prof, tip, tag, gk, skin, scale, glove_to, bridge=False):
        rings = [tube_ring(p, d, r) for p, r, seg, d in prof]
        segs = [seg for p, r, seg, d in prof]

        def key(k):
            seg = segs[k]
            if seg < glove_to + (0.1 if glove_to < 3 else 0):
                return gk
            if scale and abs((seg % 1.0) - 0.4) < 0.17 and seg < 3:      # écailles : une bande au milieu de chaque phalange
                return scale
            return skin
        if bridge:
            self.bridge(port, rings[0], gk, tag)
        else:
            self.band(port, rings[0], gk, tag)
        for k in range(len(rings) - 1):
            self.band(rings[k], rings[k + 1], key(k), tag)
        last = rings[-1]
        c = sum(last, Vector()) / len(last)
        for k in range(len(last)):
            tri = [last[k], last[(k + 1) % len(last)], tip]
            self.bf(tri, key(len(rings) - 1), sum(tri, Vector()) / 3 - c + (tip - c) * 0.5, tag)

    # ---- décors (coques collées au corps)
    def feathers(self):
        """Hibou : manche en rangs de plumes qui se recouvrent (pointes vers le poignet), deux tons."""
        st = self.st
        k1, k2 = self.mat("plume1", st["plume1"]), self.mat("plume2", st["plume2"])
        n = 12
        for row, (z0, r0) in enumerate(((ELBOW, 0.043), (-0.245, 0.042), (-0.22, 0.040), (-0.195, 0.038))):
            for k in range(n):
                a0 = 2 * math.pi * (k + 0.5 * (row % 2)) / n
                a1 = a0 + 2 * math.pi / n * 1.15
                am = (a0 + a1) / 2
                L = 0.04
                def P(a, r, z):
                    return UH(r * math.cos(a), r * math.sin(a), z)
                base0, base1 = P(a0, r0, z0), P(a1, r0, z0)
                tip = P(am, r0 * 0.93, z0 + L)
                mid0, mid1 = P(a0 + 0.12, r0 * 0.97, z0 + L * 0.6), P(a1 - 0.12, r0 * 0.97, z0 + L * 0.6)
                key = k1 if (k + row) % 2 == 0 else k2
                def mk(base0=base0, base1=base1, mid0=mid0, mid1=mid1, tip=tip, key=key, am=am):
                    self.B.new_shell()
                    out = UH(math.cos(am), math.sin(am), 0) - UH(0, 0, 0)
                    self.B.face([base0, base1, mid1, tip, mid0], key, out)
                    self.B.face([base0, mid0, tip, mid1, base1], key, -out)       # revers (feuille double face)
                self.part(["COPY"], mk)

    def decor(self):
        st = self.st
        n = 16
        ex = st.get("extras", ())
        if "plumes" in ex:
            self.feathers()
        if "lisere" in ex:
            lk = self.mat("lisere", st["lisere"])
            lr = [ring(-0.205, 0.0405, 0.0385, 24), ring(-0.195, 0.0402, 0.0382, 24)]
            self.part(["COPY"], lambda: self.sweep(lr, lk))
        if "ourlet" in ex:
            ok = self.mat("ourlet", st["ourlet"])
            J = lambda k: JAG18[k * 18 // 24]
            orr = [ring(-0.158, 0.0388, 0.0368, 24, jag=J, droop=0.02), ring(-0.153, 0.0386, 0.0366, 24, jag=J, droop=0.02)]
            self.part(["COPY"], lambda: self.sweep(orr, ok))
        sk, bk = self.mat("sangle", st["sangle"]), self.mat("boucle", st["boucle"])
        straps = (-0.14, -0.105, -0.07) if self.p == "Renard" else (-0.13, -0.075)
        for i, z in enumerate(straps):
            r = 0.0334 - 0.0012 * i
            sr = [ring(z - 0.006, r, r * 0.93, n), ring(z + 0.006, r, r * 0.93, n)]
            self.part(["COPY"], lambda sr=sr: self.sweep(sr, sk))
            big = self.p == "Koala" and i == 0
            w, h = (0.016, 0.012) if big else (0.011, 0.009)
            c = UH(0.008 if i % 2 else -0.006, r * 0.93 + 0.002, z)
            self.part(["COPY"], lambda c=c, w=w, h=h: self.B.box(c - Vector((w / 2, h / 2, 0.0)), c + Vector((w / 2, h / 2, 0.005)), bk))
        if "bandage" in ex:
            bd = self.mat("bandage", st["bandage"])
            br = [ring(-0.205, 0.0352, 0.0335, n, phase=0.2), ring(-0.188, 0.0356, 0.0337, n, phase=0.2)]
            self.part(["COPY"], lambda: self.sweep(br, bd))
        if "fourrure" in ex:
            self.fur(-0.19, 0.036, 0.052, "fourrure", st["fourrure"], 28)
            self.fur(-0.183, 0.035, 0.047, "fourrure", st["fourrure"], 22, phase=0.5)
        if "poil" in ex:                       # renard : fourrure rousse au bord de la manche, et aux poignets (référence)
            self.fur(-0.168, 0.036, 0.046, "poil", st["poil"], 40)
            self.fur(-0.163, 0.0355, 0.043, "poil", st["poil"], 34, phase=0.5)
            self.fur(-0.05, 0.0318, 0.041, "poil", st["poil"], 40)
            self.fur(-0.045, 0.0318, 0.039, "poil", st["poil"], 34, phase=0.5)
        if "rayures" in ex:
            k1, k2 = self.mat("rayure1", st["rayure1"]), self.mat("rayure2", st["rayure2"])
            zs = [-0.205 + 0.006 * i for i in range(6)]
            for i in range(len(zs) - 1):
                rr = [ring(zs[i], 0.0355, 0.034, n), ring(zs[i + 1], 0.0352, 0.0337, n)]
                self.part(["COPY"], lambda rr=rr, i=i: self.sweep(rr, k1 if i % 2 == 0 else k2))
        if "duvet" in ex:
            self.fur(-0.012, 0.0308, 0.04, "duvet", st["duvet"], 16)

    def fur(self, z, r0, r1, key, hexa, n, phase=0.0):
        """Collerette de poils en pointes (deux faces), autour de l'avant-bras."""
        k = self.mat(key, hexa)
        def mk():
            self.B.new_shell()
            for i in range(n):
                a0 = 2 * math.pi * (i + phase) / n
                a1 = 2 * math.pi * (i + 1 + phase) / n
                am = (a0 + a1) / 2
                b0 = UH(r0 * math.cos(a0), r0 * math.sin(a0), z - 0.008)
                b1 = UH(r0 * math.cos(a1), r0 * math.sin(a1), z - 0.008)
                rt = r1 * (0.86 + 0.14 * ((i * 7) % 5) / 4)
                tip = UH(rt * math.cos(am), rt * math.sin(am), z + 0.009 + 0.004 * ((i * 3) % 4) / 3)
                out = UH(math.cos(am), math.sin(am), 0.4) - UH(0, 0, 0)
                self.B.face([b0, b1, tip], k, out)
                self.B.face([b0, tip, b1], k, -out)
        self.part(["COPY"], mk)

    def claws(self, bones):
        st = self.st
        L = st["griffes"]
        if L <= 0:
            return
        claw = self.mat("griffe", st["griffe"])
        fin = st.get("doigts_fins", 1.0)
        for name, *_ in FINGERS + [("Pouce",)]:
            chain = ["%s_%d" % (name, i) for i in (1, 2, 3)]
            pts = [bones[chain[0]][0]] + [bones[b][1] for b in chain]       # Blender
            r = (0.0106 if name == "Pouce" else 0.0098) * fin * 0.78
            d = (pts[-1] - pts[-2]).normalized()
            up = Vector((0, 0, 1)); down = -up
            base_c = pts[-1] + d * r * 0.55 + up * r * 0.22
            side = d.cross(up).normalized()
            w = r * 0.55
            if st.get("serres"):
                path = [base_c, base_c + d * L * 0.55 + down * L * 0.15, base_c + d * L * 0.9 + down * L * 0.6]
            else:
                path = [base_c, base_c + d * L * 0.6 + down * L * 0.1, base_c + d * L + down * L * 0.45]
            def mkc(path=path, w=w, side=side):
                self.B.new_shell()
                r0 = [path[0] + side * w, path[0] + up * w * 0.8, path[0] - side * w, path[0] + down * w * 0.6]
                r1 = [path[1] + side * w * 0.55, path[1] + up * w * 0.45, path[1] - side * w * 0.55, path[1] + down * w * 0.3]
                apex = path[2]
                cen = (path[0] + path[1]) / 2
                for k in range(4):
                    q = [r0[k], r0[(k + 1) % 4], r1[(k + 1) % 4], r1[k]]
                    self.B.face(q, claw, sum(q, Vector()) / 4 - cen)
                    tri = [r1[k], r1[(k + 1) % 4], apex]
                    self.B.face(tri, claw, sum(tri, Vector()) / 3 - path[1])
            self.part([chain[-1]], mkc)

    # ---- construction
    def build(self, paint=True):
        bones = bones_right()
        self.part(["BODY"], lambda: self.body(bones))
        self.decor()
        self.claws(bones)
        name = "SK_BrasFPS_%s_D" % self.p
        ob = self.B.finish(name, self.mats, sharp_angle=75.0)
        self.painted(ob, bones, paint)
        return ob, bones

    def painted(self, ob, bones, paint):
        """Dépliage « Peinture » (projection intelligente, avant le miroir : mêmes UV à gauche), texture peinte
        T_BrasFPS_<Perso>_BaseColor (TEX px), matériau unique M_BrasFPS_<Perso>."""
        me = ob.data
        if bpy.context.window is not None and ob.users_scene:
            bpy.context.window.scene = ob.users_scene[0]
        vl = bpy.context.view_layer
        for o in vl.objects:
            o.select_set(o == ob)
        vl.objects.active = ob
        uvp = me.uv_layers.new(name="Peinture")
        me.uv_layers.active = uvp
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.003)
        bpy.ops.object.mode_set(mode="OBJECT")
        for l in [l for l in me.uv_layers if l.name != "Peinture"]:
            me.uv_layers.remove(l)
        path = os.path.join(TEX_DIR, "T_BrasFPS_%s_BaseColor.png" % self.p)
        if paint:
            keys = [m.name.replace("M_BrasFPS_%s_" % self.p, "").lower() for m in me.materials]
            pos, mid = PB.raster(me, "Peinture", TEX)
            ok = mid >= 0
            H = PB.hand_space(pos[ok], ELBOW)
            sel = mid[ok]
            out = np.zeros((TEX, TEX, 3), np.float32)
            vals = np.zeros((len(H), 3), np.float32)
            st = self.st
            joints = []
            for nm, *_ in FINGERS + [("Pouce",)]:
                for k in (1, 2, 3):
                    b = "%s_%d" % (nm, k)
                    h, t, _p = bones[b]
                    hh, tt = PB.hand_space(np.array([h[:], t[:]], np.float32), ELBOW)
                    D = (tt - hh) / max(np.linalg.norm(tt - hh), 1e-6)
                    joints.append((hh, D, 0.0095))
            seams = None
            if "couture" in st:
                seams = dict(couleur=PB.W.hexrgb(st["couture"]), segments=[
                    (np.array((x, 0.017, 0.004)), np.array((x, 0.0185, 0.078))) for x in (-0.012, 0.014)])
            for i, key in enumerate(keys):
                m = sel == i
                if not m.any():
                    continue
                hexa = st.get(key) or st.get(key.capitalize()) or "#808080"
                if key == "fond":
                    hexa = "#120E0E"
                base = PB.lift(PB.W.hexrgb(hexa)[None], LIFT.get(key, 0.26))[0]
                vals[m] = PB.paint(key, H[m], base, joints, seams)
            out[ok] = vals
            out = PB.W.dilate(out, ok, 12)
            px = np.ones((TEX, TEX, 4), np.float32); px[..., :3] = out
            im = bpy.data.images.new("T_BrasFPS_%s_BaseColor" % self.p, TEX, TEX, alpha=False)
            im.pixels.foreach_set(px.ravel())
            os.makedirs(os.path.dirname(path), exist_ok=True)
            im.filepath_raw = path; im.file_format = "PNG"; im.save()
            bpy.data.images.remove(im)
        mat = C.make_material("M_BrasFPS_%s" % self.p, path)
        for p_ in me.polygons:
            p_.material_index = 0
        me.materials.clear(); me.materials.append(mat)



# ----------------------------------------------------------------------------- poids
# Poids calculés le long des chaînes d'os puis lissés (retours du 2026-11-16 : les doigts plient comme une vraie main).
# À chaque articulation, le passage d'un os à l'autre est une rampe douce (smoothstep) centrée sur l'articulation,
# plus large côté dos (jointure arrondie) que côté paume (pli net, sans écrasement) : demi-largeurs en mètres.
H_MCP = (0.011, 0.005)       # jointures (Poignet -> phalange 1) : dos, paume
H_PIP = (0.0075, 0.0032)
H_DIP = (0.006, 0.0028)
H_WRIST = 0.017              # poignet (Avant_Bras -> Poignet)
H_CMC = 0.02                 # base du pouce (Poignet -> Pouce_1), décalée de CMC_OFF vers le pouce
CMC_OFF = 0.006
MAX_INF = 4                  # influences par sommet (Unity : 4 os)
SMOOTH_IT = 6


def sstep(e0, e1, x):
    t = min(1.0, max(0.0, (x - e0) / (e1 - e0)))
    return t * t * (3 - 2 * t)


def joint_s(p, J, D, h, off=0.0):
    """Part de l'os enfant à une articulation J (direction D de l'os enfant), rampe asymétrique dos / paume."""
    v = p - J
    a = v.dot(D) - off
    if isinstance(h, tuple):
        u = Vector((0, 1, 0)) - D * D.y
        u = u.normalized() if u.length > 1e-6 else Vector((0, 1, 0))
        dor = sstep(-0.005, 0.005, v.dot(u))
        h = h[1] + (h[0] - h[1]) * dor
    return sstep(-h, h, a)


def chain_weights(p, names, J, D, H, off0=0.0):
    s = [joint_s(p, J[k], D[k], H[k], off0 if k == 0 else 0.0) for k in range(3)]
    return {names[0]: 1 - s[0], names[1]: s[0] * (1 - s[1]), names[2]: s[0] * s[1] * (1 - s[2]), names[3]: s[0] * s[1] * s[2]}


def skin(ob, parts, bones, hole_center):
    from mathutils import kdtree
    for name in bones:
        if name not in ob.vertex_groups:
            ob.vertex_groups.new(name=name)
    me = ob.data
    reg = [d.value for d in me.attributes["region"].data]
    me.attributes.remove(me.attributes["region"])
    H = [hs(v.co) for v in me.vertices]
    chains = {}
    for name, *_ in FINGERS:
        c = ["%s_%d" % (name, k) for k in (1, 2, 3)]
        P = chain_hand(bones, c)
        chains[name] = (["Poignet"] + c, P, [(P[k + 1] - P[k]).normalized() for k in range(3)])
    c = ["Pouce_1", "Pouce_2", "Pouce_3"]
    P = chain_hand(bones, c)
    thumb = (["Poignet"] + c, P, [(P[k + 1] - P[k]).normalized() for k in range(3)])
    body = set()
    for a, b, cands in parts:
        if cands == ["BODY"]:
            body.update(range(a, b))
    W = {}
    for i in body:
        p = H[i]
        r = reg[i]
        sw = sstep(-H_WRIST, H_WRIST, p.z)
        w = {"Avant_Bras": 1 - sw}
        if r >= 10 and r < 20:
            name = FINGERS[r - 10][0]
            names, P, D = chains[name]
            for k, v in chain_weights(p, names, P[:3], D, (H_MCP, H_PIP, H_DIP)).items():
                w[k] = w.get(k, 0) + sw * v
        elif r == REG_THUMB:
            names, P, D = thumb
            for k, v in chain_weights(p, names, P[:3], D, (H_CMC, 0.0065, 0.0055), off0=CMC_OFF).items():
                w[k] = w.get(k, 0) + sw * v
        else:
            rest = 1.0
            if r == REG_PALM:
                g = [math.exp(-((p.x - chains[n][1][0].x) / 0.0085) ** 2) for n, *_ in FINGERS]
                gs = sum(g) or 1.0
                for (n, *_), gi in zip(FINGERS, g):
                    names, P, D = chains[n]
                    s0 = joint_s(p, P[0], D[0], H_MCP)
                    v = s0 * gi / gs
                    if v > 1e-4:
                        w[names[1]] = w.get(names[1], 0) + sw * v
                        rest -= v
                tw = 0.5 * math.exp(-((p - hole_center).length / 0.016) ** 2)
                if tw > 1e-4:
                    w["Pouce_1"] = w.get("Pouce_1", 0) + sw * tw
                    rest -= tw
            w["Poignet"] = w.get("Poignet", 0) + sw * max(0.0, rest)
        W[i] = w
    # lissage (paume, avant-bras, base du pouce ; les doigts gardent leurs rampes)
    adj = {i: [] for i in body}
    for e in me.edges:
        a, b = e.vertices
        if a in body and b in body:
            adj[a].append(b); adj[b].append(a)
    mask = [i for i in body if reg[i] in (REG_ARM, REG_PALM) or (reg[i] == REG_THUMB and (H[i] - hole_center).length < 0.026)]
    for it in range(SMOOTH_IT):
        new = {}
        for i in mask:
            nb = adj[i]
            if not nb:
                continue
            acc = {}
            for j in nb:
                for k, v in W[j].items():
                    acc[k] = acc.get(k, 0) + v / len(nb)
            m = {}
            for k in set(acc) | set(W[i]):
                m[k] = 0.5 * W[i].get(k, 0) + 0.5 * acc.get(k, 0)
            new[i] = m
        W.update(new)
    # décors : poids du point du corps le plus proche ; griffes : dernière phalange
    kd = kdtree.KDTree(len(body))
    for i in body:
        kd.insert(me.vertices[i].co, i)
    kd.balance()
    for a, b, cands in parts:
        if cands == ["BODY"]:
            continue
        for i in range(a, b):
            if cands == ["COPY"]:
                _, j, _ = kd.find(me.vertices[i].co)
                W[i] = dict(W[j])
            else:
                W[i] = {cands[0]: 1.0}
    vg = ob.vertex_groups
    for i, w in W.items():
        items = sorted(((k, v) for k, v in w.items() if v > 0.01), key=lambda kv: -kv[1])[:MAX_INF]
        s = sum(v for k, v in items) or 1.0
        for k, v in items:
            vg[k].add([i], v / s, "REPLACE")


# ----------------------------------------------------------------------------- armature
def make_armature(bones, mirror=False):
    """bones : têtes et queues dans les axes du fichier (après RX)."""
    other = bpy.data.objects.get("BrasFPS")
    if other is not None:                    # nom exact dans le FBX (clips « BrasFPS|Pose_* » identiques partout)
        other.name = "BrasFPS_autre"
    arm = bpy.data.armatures.new("BrasFPS")
    ob = bpy.data.objects.new("BrasFPS", arm)
    ob.name = "BrasFPS"
    bpy.context.scene.collection.objects.link(ob)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for name, (h, t, parent) in bones.items():
        b = arm.edit_bones.new(name)
        hh, tt = Vector(h), Vector(t)
        if mirror:
            hh.x, tt.x = -hh.x, -tt.x
        b.head, b.tail = hh, tt
        b.roll = 0.0
        eb[name] = b
    for name, (h, t, parent) in bones.items():
        if parent:
            eb[name].parent = eb[parent]
            eb[name].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.display_type = "STICK"
    ob.rotation_euler = (math.radians(90), 0.0, 0.0)
    return ob


# ----------------------------------------------------------------------------- poses (main droite ; la gauche en miroir)
AX = {"curl": Vector((1, 0, 0)), "spread": Vector((0, 0, 1)), "twist": Vector((0, -1, 0))}
TH_DIR = (UH(*THUMB[3]) - UH(*THUMB[1])).normalized()
AX["tcurl"] = TH_DIR.cross(Vector((0, 0, -1))).normalized()       # le pouce se replie vers la paume
AX["tlift"] = TH_DIR.cross(Vector((0, 0, 1))).normalized()        # le pouce se lève (vers le dos de la main)
AX["tabd"] = Vector((0, 0, 1))                                     # le pouce s'écarte de l'index


def fingers_pose(c1, c2, c3, spread=0.0, only=None):
    d = {}
    for name, x, L, y, s in FINGERS:
        if only and name not in only:
            continue
        d["%s_1" % name] = [("curl", c1 - REST_CURL[0]), ("spread", spread * s)]
        d["%s_2" % name] = [("curl", c2 - REST_CURL[1])]
        d["%s_3" % name] = [("curl", c3 - REST_CURL[2])]
    return d


def merge(*ds):
    out = {}
    for d in ds:
        for k, v in d.items():
            out.setdefault(k, []).extend(v)
    return out


def thumb(curl=0.0, lift=0.0, abd=0.0, c1=None):
    return {"Pouce_1": [("tabd", abd), ("tlift", lift), ("tcurl", c1 if c1 is not None else curl * 0.4)],
            "Pouce_2": [("tcurl", curl)], "Pouce_3": [("tcurl", curl * 0.8)]}


# Poses de flottement (retours de playtest du 2026-11-16) : rien n'est tenu, l'orbe et l'artefact flottent au-dessus de la
# paume. L'avant-bras est tourné de 180° : la paume regarde +Y du repère de la main (Socket_Poignet), là où le code pose
# l'orbe (orbInHand), le barillet (barrelInHand) et la lanterne (lanternInHand). Doigts recourbés « comme pour porter
# quelque chose sans le toucher ». Le placement de la main dans la vue (mains levées, paumes l'une vers l'autre) reste
# dans FpsView (rightHandRest, leftHandRest) : valeurs conseillées dans INTEGRATION-PROPS.md.
PALM_UP = {"Avant_Bras": [("twist", 180)]}
FLOAT = {
    # d'après reference_bras_fps.png (2026-11-16) : paume ouverte, doigts écartés et détendus, presque tendus
    "Pose_Repos": merge(PALM_UP, fingers_pose(8, 14, 10, 15), thumb(curl=4, abd=40, c1=4), {"Poignet": [("curl", -14)]}),
    "Pose_OrbePaume": merge(PALM_UP, fingers_pose(4, 10, 8, 19), thumb(curl=2, abd=44, c1=2), {"Poignet": [("curl", -18)]}),
    "Pose_Pistolet": merge(PALM_UP, fingers_pose(6, 8, 4, 2, only=("Index", "Majeur")),
                           fingers_pose(26, 36, 24, 4, only=("Annulaire", "Auriculaire")),
                           thumb(curl=4, lift=20, abd=26), {"Poignet": [("curl", -12)]}),
    "Pose_PriseAnneau": merge(PALM_UP, fingers_pose(12, 20, 14, 13), thumb(curl=6, abd=36, c1=4), {"Poignet": [("curl", -14)]}),
}
POSES = {
    "Pose_Repos": [FLOAT["Pose_Repos"]],
    "Pose_OrbePaume": [FLOAT["Pose_OrbePaume"]],
    "Pose_Pistolet": [FLOAT["Pose_Pistolet"]],
    "Pose_PriseAnneau": [FLOAT["Pose_PriseAnneau"]],
    # poses d'action (2026-11-16) : même convention que le flottement (paume vers +Y : vers le haut ou le centre), pour
    # qu'aucun fondu ne fasse tourner l'avant-bras d'un demi-tour ; la forme de la main est gardée
    "Pose_Traction": [merge(PALM_UP, fingers_pose(-4, 4, 2, 12), thumb(curl=0, abd=24), {"Poignet": [("curl", -14)]})],
    "Pose_Repulsion": [merge(PALM_UP, fingers_pose(-14, -6, 0, 18), thumb(curl=-4, abd=30), {"Poignet": [("curl", -30)]})],
    "Pose_Attraper": [FLOAT["Pose_Repos"],
                      merge(PALM_UP, fingers_pose(-6, 0, 0, 14), thumb(curl=0, abd=26), {"Poignet": [("curl", -14)]}),
                      merge(PALM_UP, fingers_pose(42, 62, 42, -2), thumb(curl=32, abd=-8, c1=14), {"Poignet": [("curl", -8)]})],
    "Pose_TenirObjet": [merge(PALM_UP, fingers_pose(20, 36, 26, 10), thumb(curl=16, abd=20, c1=10), {"Poignet": [("curl", -14)]})],
    "Pose_Lancer": [merge(PALM_UP, fingers_pose(0, 4, 0, 10), thumb(curl=4, abd=18), {"Poignet": [("curl", 14)]})],
    "Pose_Poing": [merge(fingers_pose(88, 100, 75, -1), thumb(curl=35, abd=-80, c1=-20))],      # pouce replié sur les doigts
}
FRAMES = {"Pose_Attraper": (1, 4, 8)}

# Boucles (même nom que la pose, préfixe Boucle_) : respiration de l'avant-bras (bascule de BREATH_DEG autour du coude et
# torsion légère, période LOOP_S) et doigts qui ondulent pendant le flottement (deux cycles par boucle, phase décalée par
# doigt). Première et dernière clés identiques : boucle propre.
FPS_ANIM = 24
LOOP_S = 3.5
BREATH_DEG = 1.4
FINGER_DEG = 5.0
LOOP_STEP = 3                       # une clé toutes les 3 images (0.125 s)
FINGER_PHASE = {"Index": 0.0, "Majeur": 0.6, "Annulaire": 1.2, "Auriculaire": 1.8, "Pouce": 2.6}


def loop_spec(base, t):
    """Pose de flottement à l'instant t (s) : base + respiration + doigts."""
    w = 2 * math.pi * t / LOOP_S
    extra = {"Avant_Bras": [("curl", -BREATH_DEG * math.sin(w)), ("twist", 0.8 * math.sin(w + 0.7))],
             "Poignet": [("curl", 1.2 * math.sin(w + 1.4))]}
    for name, *_ in FINGERS:
        a = FINGER_DEG * math.sin(2 * w + FINGER_PHASE[name])
        extra["%s_1" % name] = [("curl", a)]
        extra["%s_2" % name] = [("curl", a * 0.8)]
        extra["%s_3" % name] = [("curl", a * 0.5)]
    extra["Pouce_2"] = [("tcurl", 0.6 * FINGER_DEG * math.sin(2 * w + FINGER_PHASE["Pouce"]))]
    return merge(base, extra)


def mirror_axis(v):
    return Vector((v.x, -v.y, -v.z))


def apply_pose(arm_ob, spec, mirror):
    """spec : os -> [(axe, angle°)] dans le repère de repos de l'armature ; converti dans le repère local de chaque os."""
    for pb in arm_ob.pose.bones:
        pb.rotation_mode = "QUATERNION"
        pb.rotation_quaternion = Quaternion()
    for name, rots in spec.items():
        pb = arm_ob.pose.bones[name]
        R = pb.bone.matrix_local.to_quaternion()
        q = Quaternion()
        for axn, ang in rots:
            ax = (RX.to_3x3() @ AX[axn]).normalized()      # axe de la vue -> axes de l'armature
            if mirror:
                ax = mirror_axis(ax)
            q = Quaternion(ax, math.radians(ang)) @ q
        pb.rotation_quaternion = R.inverted() @ q @ R


def make_actions(arm_ob, mirror):
    acts = []
    arm_ob.animation_data_create()
    for name, keys in POSES.items():
        act = bpy.data.actions.new(name)
        act.use_fake_user = False
        arm_ob.animation_data.action = act
        frames = FRAMES.get(name, (1, 2))
        if len(keys) == 1:                   # pose fixe : deux clés identiques (une seule image donne un clip vide dans Unity)
            keys = keys * len(frames)
        for f, spec in zip(frames, keys):
            apply_pose(arm_ob, spec, mirror)
            for pb in arm_ob.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f)
                pb.keyframe_insert("location", frame=f)
        acts.append(act)
    n = int(round(LOOP_S * FPS_ANIM))
    for name, base in FLOAT.items():
        act = bpy.data.actions.new(name.replace("Pose_", "Boucle_"))
        act.use_fake_user = False
        arm_ob.animation_data.action = act
        for f in range(0, n + 1, LOOP_STEP):
            apply_pose(arm_ob, loop_spec(base, f / FPS_ANIM), mirror)
            for pb in arm_ob.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=1 + f)
                pb.keyframe_insert("location", frame=1 + f)
        acts.append(act)
    arm_ob.animation_data.action = acts[0]
    return acts


# ----------------------------------------------------------------------------- repères
SOCKETS = {  # repère de la main (Unity) ; côtés
    "Socket_Poignet": ((0.0, 0.0, 0.0), ("D", "G")),
    "Socket_Orbe": ((0.0, 0.06, 0.045), ("D",)),
    "Socket_Artefact": ((0.0, 0.055, 0.05), ("G",)),
    "Socket_Artefact_Anneau": ((0.0, 0.075, 0.05), ("G",)),     # anneau de la lanterne : flotte au-dessus de la paume
}


def sockets(arm_ob, side):
    out = []
    for name, (p, sides) in SOCKETS.items():
        if side not in sides:
            continue
        other = bpy.data.objects.get(name)
        if other is not None:
            other.name = name + "_autre"
        e = bpy.data.objects.new(name, None)
        e.name = name
        e.empty_display_type = "ARROWS"; e.empty_display_size = 0.02
        bpy.context.scene.collection.objects.link(e)
        e.parent = arm_ob
        e.matrix_parent_inverse.identity()
        e.location = RX @ UH(*p)          # axes du fichier ; rotation nulle (+Z = doigts, +Y = dos de la main) ; x = 0
        out.append(e)
    return out


# ----------------------------------------------------------------------------- export, vérification
MODELS_DIR = F.MODELS_DIR
SOURCE_DIR = F.SOURCE_DIR


def export_skinned(objs, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_mesh_modifiers=False, mesh_smooth_type="FACE", use_triangles=True, use_tspace=False,
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X", use_armature_deform_only=True,
        armature_nodetype="NULL",
        bake_anim=True, bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        path_mode="RELATIVE", embed_textures=False, use_custom_props=False, use_metadata=True)


def verify(path):
    prev = bpy.context.window.scene
    sc = bpy.data.scenes.new("_VerifBras")
    bpy.context.window.scene = sc
    acts_before = set(bpy.data.actions.keys())
    bpy.ops.import_scene.fbx(filepath=path, use_manual_orientation=True, axis_forward="Y", axis_up="Z",
                             use_image_search=False, automatic_bone_orientation=False)
    lines = []
    for o in sc.objects:
        if o.type == "ARMATURE":
            lines.append("  armature %s : %d os, rot %s, échelle %s" % (o.name, len(o.data.bones),
                         tuple(round(math.degrees(a), 1) for a in o.matrix_world.to_euler()), tuple(round(s, 3) for s in o.matrix_world.to_scale())))
            names = sorted(b.name for b in o.data.bones)
            lines.append("  os : " + ", ".join(names))
            for bn in ("Avant_Bras", "Poignet", "Index_3"):
                b = o.data.bones.get(bn)
                if b:
                    hw = o.matrix_world @ b.head_local
                    lines.append("    %s tête (fichier/Unity, X inversé) %s" % (bn, tuple(round(v, 4) for v in (-hw.x, hw.y, hw.z))))
        elif o.type == "MESH":
            tris = sum(len(p.vertices) - 2 for p in o.data.polygons)
            mn = Vector((min(v.co.x for v in o.data.vertices), min(v.co.y for v in o.data.vertices), min(v.co.z for v in o.data.vertices)))
            mx = Vector((max(v.co.x for v in o.data.vertices), max(v.co.y for v in o.data.vertices), max(v.co.z for v in o.data.vertices)))
            mw = o.matrix_world
            a, b = mw @ mn, mw @ mx
            lines.append("  maillage %s : %d triangles, %d groupes, taille %s" % (o.name, tris, len(o.vertex_groups),
                         tuple(round(abs(b[i] - a[i]), 3) for i in range(3))))
        elif o.type == "EMPTY":
            m = o.matrix_world
            p = m.translation
            z = m.col[2].xyz.normalized(); y = m.col[1].xyz.normalized()
            lines.append("  repère %s : position Unity (%.4f, %.4f, %.4f), +Z -> (%.2f, %.2f, %.2f), +Y -> (%.2f, %.2f, %.2f)"
                         % (o.name, -p.x, p.y, p.z, -z.x, z.y, z.z, -y.x, y.y, y.z))
    new_acts = [a for a in bpy.data.actions.keys() if a not in acts_before]
    lines.append("  actions importées : %d (%s)" % (len(new_acts), ", ".join(sorted(new_acts))))
    for o in list(sc.objects):
        d = o.data
        bpy.data.objects.remove(o, do_unlink=True)
        if d is not None and getattr(d, "users", 1) == 0:
            if isinstance(d, bpy.types.Mesh):
                bpy.data.meshes.remove(d)
            elif isinstance(d, bpy.types.Armature):
                bpy.data.armatures.remove(d)
    for a in list(bpy.data.actions):
        if a.name in new_acts:
            bpy.data.actions.remove(a)
    for m in list(bpy.data.materials):
        if m.users == 0 and "." in m.name:
            bpy.data.materials.remove(m)
    bpy.data.scenes.remove(sc)
    bpy.context.window.scene = prev
    return "\n".join(lines)


def save_blend(scene, acts, path):
    """.blend de la scène du bras, avec ses actions (poses)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    blocks = {scene} | set(scene.objects) | {o.data for o in scene.objects if o.data is not None} | set(acts)
    scene.use_fake_user = True
    bpy.data.libraries.write(path, blocks, path_remap="RELATIVE_ALL", fake_user=True, compress=True)


def clear_actions():
    for a in list(bpy.data.actions):
        if a.name.startswith(("Pose_", "Boucle_")):
            bpy.data.actions.remove(a)


# ----------------------------------------------------------------------------- un bras complet
def build_side(perso, side):
    sc = C.new_scene("SK_BrasFPS_%s_%s" % (perso, side))
    sc.render.fps = FPS_ANIM
    clear_actions()
    global LAST_ARM
    A = LAST_ARM = Arm(perso)
    mesh_ob, bones = A.build(paint=(side == "D"))
    skin(mesh_ob, A.parts, bones, A.hole_center)
    mirror = side == "G"
    mesh_ob.data.transform(RX)
    bones = {n: (RX @ h, RX @ t, par) for n, (h, t, par) in bones.items()}
    if mirror:
        mesh_ob.data.transform(Matrix.Scale(-1, 4, (1, 0, 0)))
        mesh_ob.data.flip_normals()
        mesh_ob.data.update()
    mesh_ob.name = "SK_BrasFPS_%s_%s" % (perso, side)
    mesh_ob.data.name = mesh_ob.name
    arm_ob = make_armature(bones, mirror)
    mesh_ob.location = (0, 0, 0)
    mesh_ob.parent = arm_ob
    mesh_ob.matrix_parent_inverse.identity()
    mod = mesh_ob.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_ob
    socks = sockets(arm_ob, side)
    acts = make_actions(arm_ob, mirror)
    tris = C.tri_count(mesh_ob)
    return sc, arm_ob, mesh_ob, socks, acts, tris


def run(persos=PERSOS):
    report = []
    for perso in persos:
        for side in ("D", "G"):
            sc, arm_ob, mesh_ob, socks, acts, tris = build_side(perso, side)
            name = "SK_BrasFPS_%s_%s" % (perso, side)
            fbx = os.path.join(MODELS_DIR, name + ".fbx")
            export_skinned([arm_ob, mesh_ob] + socks, fbx)
            save_blend(sc, acts, os.path.join(SOURCE_DIR, name + ".blend"))
            report.append("%s : %d triangles, %d actions\n%s" % (name, tris, len(acts), verify(fbx)))
            bpy.context.window.scene = sc
    clear_actions()
    print("\n".join(report))


if __name__ == "__main__":
    run()
