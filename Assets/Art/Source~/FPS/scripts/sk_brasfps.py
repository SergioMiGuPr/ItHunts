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
Pose_Lancer, Pose_Poing. Elles portent la forme de la main (poignet, doigts, rotation de l'avant-bras pour la paume
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
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion

ELBOW = -0.27          # coude (bout coupé de la manche), repère de la main
# L'armature, le maillage et les repères sont stockés dans les axes du FBX (Y en haut, Z vers l'avant) ; l'objet armature
# est tourné de +90° autour de X dans Blender, ce que la conversion d'axes de l'export annule : racine à rotation nulle.
RX = Matrix.Rotation(math.radians(-90), 4, "X")         # vue Blender (UH) -> axes du fichier
PERSOS = ("Renard", "Koala", "Cameleon", "Hibou")

# ----------------------------------------------------------------------------- styles (couleurs relevées sur la planche)
STYLE = {
    "Renard": dict(manche="#3A3033", lisere="#7A2226", bandage="#C9BCA6", cuir="#3A2A24", sangle="#2A1E1A", boucle="#B08A3A",
                   gant="#3B3034", peau="#C9502E", griffe="#171313", sans_doigts=True, griffes=0.012,
                   extras=("bandage", "lisere")),
    "Koala": dict(manche="#4A4B38", lisere="#6E8A3E", fourrure="#C8C4BE", cuir="#5A4134", sangle="#3E2C24", boucle="#B08A3A",
                  gant="#3D433A", peau="#3D433A", griffe="#1B1B19", sans_doigts=False, griffes=0.010,
                  extras=("fourrure", "lisere")),
    "Cameleon": dict(manche="#5A2424", ourlet="#D2581E", rayure1="#7E9A56", rayure2="#3C5A2C", cuir="#5A3E32", sangle="#3E2A22",
                     boucle="#B08A3A", gant="#45414A", peau="#8DAA5A", griffe="#4E6B2E", sans_doigts=True, griffes=0.0,
                     extras=("ourlet", "rayures"), doigts_fins=0.85),
    "Hibou": dict(plume1="#B19B85", plume2="#615048", duvet="#D6CBB6", cuir="#4A362C", sangle="#33251F", boucle="#B08A3A",
                  peau="#CFC4AE", griffe="#141111", sans_doigts=False, griffes=0.022, serres=True,
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
THUMB = [(-0.033, -0.004, 0.018), (-0.051, -0.006, 0.043), (-0.061, -0.007, 0.064), (-0.066, -0.008, 0.082)]   # sur le côté de la paume


def UH(x, y, z):
    """Repère de la main (Unity) -> Blender, modèle : origine au coude (bout coupé de la manche)."""
    return Vector((-x, -(z - ELBOW), y))


def bones_right():
    b = {"Avant_Bras": (UH(0, 0, ELBOW), UH(0, 0, 0), None), "Poignet": (UH(0, 0, 0), UH(0, 0, KNUCKLE_Z), "Avant_Bras")}
    for name, x, L, y, _ in FINGERS:
        z = KNUCKLE_Z
        prev = "Poignet"
        for i, f in enumerate(PHAL):
            z1 = z + L * f
            b["%s_%d" % (name, i + 1)] = (UH(x, y - 0.0015 * i, z), UH(x, y - 0.0015 * (i + 1), z1), prev)
            prev = "%s_%d" % (name, i + 1)
            z = z1
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


def path_rings(path, radii, n, up=Vector((0, 0, 1)), flat=1.0):
    """Anneaux perpendiculaires à un chemin (Blender) ; flat < 1 aplatit le profil vers le dos de la main."""
    rings = []
    for i, p in enumerate(path):
        t = (path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)]).normalized()
        side = t.cross(up)
        if side.length < 1e-6:
            side = Vector((1, 0, 0))
        side.normalize()
        nrm = side.cross(t).normalized()
        rings.append([p + side * radii[i] * math.cos(2 * math.pi * k / n) + nrm * radii[i] * flat * math.sin(2 * math.pi * k / n)
                      for k in range(n)])
    return rings


class Arm:
    def __init__(self, perso):
        self.p = perso
        self.st = STYLE[perso]
        self.B = C.Builder()
        self.parts = []            # (début, fin, os candidats)
        self.mats = {}

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

    # ---- pièces
    def sleeve(self):
        st = self.st
        jag = lambda k: (0.0, 0.012, 0.004, 0.019, 0.002, 0.011, 0.006, 0.016, 0.0, 0.009, 0.014, 0.003, 0.018, 0.007, 0.001, 0.013, 0.005, 0.017)[k % 18]
        n = 18
        if st.get("extras") and "plumes" in st["extras"]:
            return self.feathers()
        key = self.mat("manche", st["manche"])
        zs = [ELBOW, -0.24, -0.21, -0.185]
        rs = [0.043, 0.042, 0.040, 0.0385]
        rings = [ring(z, r, r * 0.95, n) for z, r in zip(zs, rs)]
        hem = ring(-0.16, 0.038, 0.036, n, jag=jag, droop=0.02)
        rings.append(hem)
        self.part(["Avant_Bras"], lambda: self.sweep(rings, key, caps=(True, False)))
        # doublure (intérieur de l'ourlet déchiré, visible de près)
        rin = [ring(-0.185, 0.0345, 0.033, n), ring(-0.16, 0.0345, 0.033, n, jag=jag, droop=0.02)]
        key2 = self.mat("ourlet", st["ourlet"]) if "ourlet" in st else key
        self.part(["Avant_Bras"], lambda: self.sweep(rin, key2))
        if "lisere" in st.get("extras", ()):
            lk = self.mat("lisere", st["lisere"])
            lr = [ring(-0.205, 0.0405, 0.0385, n), ring(-0.195, 0.0402, 0.0382, n)]
            self.part(["Avant_Bras"], lambda: self.sweep(lr, lk))
        if "ourlet" in st.get("extras", ()):
            ok = self.mat("ourlet", st["ourlet"])
            orr = [ring(-0.158, 0.0388, 0.0368, n, jag=jag, droop=0.02), ring(-0.153, 0.0386, 0.0366, n, jag=jag, droop=0.02)]
            self.part(["Avant_Bras"], lambda: self.sweep(orr, ok))
        # fond de la manche, au coude (le bout coupé)
        cap = self.mat("fond", "#120E0E")
        self.part(["Avant_Bras"], lambda: self.B.face(ring(ELBOW + 0.0005, 0.04, 0.038, n), cap, UH(0, 0, -1) - UH(0, 0, 0)))

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
                self.part(["Avant_Bras"], mk)
        # corps sous les plumes et fond
        core = [ring(z, 0.036, 0.034, n) for z in (ELBOW, -0.2, -0.17)]
        self.part(["Avant_Bras"], lambda: self.sweep(core, k2, caps=(True, False)))

    def bracer(self):
        st = self.st
        n = 16
        ck, sk, bk = self.mat("cuir", st["cuir"]), self.mat("sangle", st["sangle"]), self.mat("boucle", st["boucle"])
        rings = [ring(z, r, r * 0.92, n) for z, r in ((-0.19, 0.0335), (-0.12, 0.0325), (-0.06, 0.031), (-0.02, 0.0305))]
        self.part(["Avant_Bras"], lambda: self.sweep(rings, ck))
        straps = (-0.165, -0.11, -0.055) if self.p == "Renard" else (-0.15, -0.075)
        for i, z in enumerate(straps):
            r = 0.0345 - 0.0012 * i
            sr = [ring(z - 0.006, r, r * 0.93, n), ring(z + 0.006, r, r * 0.93, n)]
            self.part(["Avant_Bras"], lambda sr=sr: self.sweep(sr, sk))
            big = self.p == "Koala" and i == 0
            w, h = (0.016, 0.012) if big else (0.011, 0.009)
            c = UH(0.008 if i % 2 else -0.006, r * 0.93 + 0.002, z)
            self.part(["Avant_Bras"], lambda c=c, w=w, h=h: self.B.box(c - Vector((w / 2, h / 2, 0.0)), c + Vector((w / 2, h / 2, 0.005)), bk))
        if "bandage" in st.get("extras", ()):
            bd = self.mat("bandage", st["bandage"])
            br = [ring(-0.205, 0.0345, 0.0325, n, phase=0.2), ring(-0.188, 0.0352, 0.033, n, phase=0.2)]
            self.part(["Avant_Bras"], lambda: self.sweep(br, bd))
        if "fourrure" in st.get("extras", ()):
            self.fur(-0.19, 0.036, 0.052, "fourrure", st["fourrure"], 18)
        if "rayures" in st.get("extras", ()):
            # manche intérieure rayée qui dépasse entre l'ourlet et le brassard (anneaux alternés)
            k1, k2 = self.mat("rayure1", st["rayure1"]), self.mat("rayure2", st["rayure2"])
            zs = [-0.205 + 0.006 * i for i in range(6)]
            for i in range(len(zs) - 1):
                rr = [ring(zs[i], 0.0355, 0.034, n), ring(zs[i + 1], 0.0352, 0.0337, n)]
                self.part(["Avant_Bras"], lambda rr=rr, i=i: self.sweep(rr, k1 if i % 2 == 0 else k2))
        if "duvet" in st.get("extras", ()):
            self.fur(-0.012, 0.031, 0.04, "duvet", st["duvet"], 16, bones=["Avant_Bras", "Poignet"])

    def fur(self, z, r0, r1, key, hexa, n, bones=("Avant_Bras",)):
        """Collerette de poils en pointes (deux faces), autour de l'avant-bras."""
        k = self.mat(key, hexa)
        def mk():
            self.B.new_shell()
            for i in range(n):
                a0 = 2 * math.pi * i / n
                a1 = 2 * math.pi * (i + 1) / n
                am = (a0 + a1) / 2
                b0 = UH(r0 * math.cos(a0), r0 * math.sin(a0), z - 0.008)
                b1 = UH(r0 * math.cos(a1), r0 * math.sin(a1), z - 0.008)
                rt = r1 * (0.85 if i % 2 else 1.0)
                tip = UH(rt * math.cos(am), rt * math.sin(am), z + 0.016)
                out = UH(math.cos(am), math.sin(am), 0.4) - UH(0, 0, 0)
                self.B.face([b0, b1, tip], k, out)
                self.B.face([b0, tip, b1], k, -out)
        self.part(list(bones), mk)

    def hand(self):
        st = self.st
        n = 16
        gk = self.mat("gant", st["gant"]) if "gant" in st else self.mat("peau", st["peau"])
        # poignet du gant (manchette) puis paume en coupes arrondies
        cuff = [ring(z, r, r * 0.9, n) for z, r in ((-0.035, 0.0315), (-0.005, 0.0305), (0.012, 0.03))]
        self.part(["Avant_Bras", "Poignet"], lambda: self.sweep(cuff, gk))

        def section(z, w, tb, tp):
            pts = []
            for k in range(n):
                a = 2 * math.pi * k / n
                c, s = math.cos(a), math.sin(a)
                x = w / 2 * math.copysign(abs(c) ** 0.55, c)
                y = (tb if s > 0 else tp) * math.copysign(abs(s) ** 0.65, s)
                pts.append(UH(x, y, z))
            return pts
        secs = [section(0.0, 0.058, 0.014, 0.013), section(0.03, 0.076, 0.016, 0.015), section(0.065, 0.086, 0.016, 0.014),
                section(0.092, 0.082, 0.013, 0.012)]
        self.part(["Poignet"], lambda: self.sweep(secs, gk, caps=(False, True)))

    def fingers(self, bones):
        st = self.st
        n = 8
        fin = st.get("doigts_fins", 1.0)
        skin = self.mat("peau", st["peau"])
        glove = self.mat("gant", st["gant"]) if "gant" in st else skin
        claw = self.mat("griffe", st["griffe"])
        chains = [(name, ["%s_%d" % (name, i) for i in (1, 2, 3)], 0.0088) for name, *_ in FINGERS]
        chains.append(("Pouce", ["Pouce_1", "Pouce_2", "Pouce_3"], 0.0095))
        for name, chain, r in chains:
            pts = [bones[chain[0]][0]] + [bones[b][1] for b in chain]
            if name == "Pouce":
                radii = [r * 1.25 * fin, r * fin, r * 0.92 * fin, r * 0.8 * fin]
            else:
                radii = [r * fin, r * 0.94 * fin, r * 0.86 * fin, r * 0.78 * fin]
            d = (pts[-1] - pts[-2]).normalized()
            tip = pts[-1] + d * radii[-1] * 0.9
            rings = path_rings(pts, radii, n, flat=0.85)
            seg_mats = [glove if (not st["sans_doigts"]) or (i == 0) else skin for i in range(3)]
            if name == "Pouce" and st["sans_doigts"]:
                seg_mats = [glove, skin, skin]
            def mk(rings=rings, tip=tip, seg_mats=seg_mats, pts=pts):
                self.B.new_shell()
                for i in range(len(rings) - 1):
                    c = (sum(rings[i], Vector()) + sum(rings[i + 1], Vector())) / (2 * n)
                    for k in range(n):
                        q = [rings[i][k], rings[i][(k + 1) % n], rings[i + 1][(k + 1) % n], rings[i + 1][k]]
                        self.B.face(q, seg_mats[i], sum(q, Vector()) / 4 - c)
                for k in range(n):
                    tri = [rings[-1][k], rings[-1][(k + 1) % n], tip]
                    self.B.face(tri, seg_mats[-1], sum(tri, Vector()) / 3 - pts[-1] + (tip - pts[-1]) * 0.5)
                # base du doigt : bouchon (caché dans la paume)
                self.B.face(rings[0], seg_mats[0], pts[0] - pts[1])
            self.part(chain + ["Poignet"], mk)
            L = st["griffes"]
            if L > 0:
                up = Vector((0, 0, 1))
                down = -up
                base_c = pts[-1] + d * radii[-1] * 0.2 + up * radii[-1] * 0.45
                side = d.cross(up).normalized()
                w = radii[-1] * 0.55
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
            elif "griffe" in st and name != "Pouce":
                pass

    # ---- construction
    def build(self):
        bones = bones_right()
        self.sleeve()
        self.bracer()
        self.hand()
        self.fingers(bones)
        name = "SK_BrasFPS_%s_D" % self.p
        ob = self.B.finish(name, self.mats, sharp_angle=60.0)
        return ob, bones


# ----------------------------------------------------------------------------- poids
def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (p - (a + ab * t)).length, (p - a).dot(ab) / ab.length_squared


def skin(ob, parts, bones):
    for name in bones:
        if name not in ob.vertex_groups:
            ob.vertex_groups.new(name=name)
    vg = ob.vertex_groups
    verts = ob.data.vertices
    for a, b, cands in parts:
        for i in range(a, b):
            p = verts[i].co
            best, bt = None, 0.0
            bd = 1e9
            for n in cands:
                h, t = bones[n][0], bones[n][1]
                d, tt = seg_dist(p, h, t)
                if d < bd - 1e-9:
                    bd, best, bt = d, n, tt
            parent = bones[best][2]
            if parent in cands and bt < 0.3:
                w = 0.5 + max(0.0, bt) / 0.6
                vg[best].add([i], w, "REPLACE")
                vg[parent].add([i], 1.0 - w, "REPLACE")
            else:
                vg[best].add([i], 1.0, "REPLACE")


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
        d["%s_1" % name] = [("curl", c1), ("spread", spread * s)]
        d["%s_2" % name] = [("curl", c2)]
        d["%s_3" % name] = [("curl", c3)]
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


POSES = {
    "Pose_Repos": [merge(fingers_pose(12, 14, 8, 2), thumb(curl=10, abd=4))],
    "Pose_OrbePaume": [merge({"Avant_Bras": [("twist", 180)]}, fingers_pose(10, 16, 10, 6), thumb(curl=6, abd=14),
                             {"Poignet": [("curl", -8)]})],
    "Pose_Pistolet": [merge(fingers_pose(0, 0, 0, 1.5, only=("Index", "Majeur")),
                            fingers_pose(85, 95, 60, only=("Annulaire", "Auriculaire")),
                            thumb(curl=0, lift=35, abd=8))],
    "Pose_PriseAnneau": [merge(fingers_pose(68, 88, 52, -1), thumb(curl=38, abd=-14, c1=18))],
    "Pose_Traction": [merge(fingers_pose(-4, 0, 0, 10), thumb(curl=0, abd=16), {"Poignet": [("curl", -12)]})],
    "Pose_Repulsion": [merge(fingers_pose(-14, -6, 0, 18), thumb(curl=-4, abd=26), {"Poignet": [("curl", -28)]})],
    "Pose_Attraper": [merge(fingers_pose(12, 14, 8, 2), thumb(curl=10, abd=4)),
                      merge(fingers_pose(-6, 0, 0, 14), thumb(curl=0, abd=22)),
                      merge(fingers_pose(42, 62, 42, -2), thumb(curl=32, abd=-8, c1=14))],
    "Pose_TenirObjet": [merge(fingers_pose(22, 55, 45, 12), thumb(curl=30, abd=12), {"Poignet": [("curl", -10)]})],
    "Pose_Lancer": [merge(fingers_pose(0, 4, 0, 8), thumb(curl=4, abd=10), {"Poignet": [("curl", 18)]})],
    "Pose_Poing": [merge(fingers_pose(88, 100, 75, -1), thumb(curl=45, abd=-18, c1=24))],
}
FRAMES = {"Pose_Attraper": (1, 4, 8)}


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
        frames = FRAMES.get(name, (1,))
        for f, spec in zip(frames, keys):
            apply_pose(arm_ob, spec, mirror)
            for pb in arm_ob.pose.bones:
                pb.keyframe_insert("rotation_quaternion", frame=f)
                pb.keyframe_insert("location", frame=f)
        acts.append(act)
    arm_ob.animation_data.action = acts[0]
    return acts


# ----------------------------------------------------------------------------- repères
SOCKETS = {  # repère de la main (Unity) ; côtés
    "Socket_Poignet": ((0.0, 0.0, 0.0), ("D", "G")),
    "Socket_Orbe": ((0.0, 0.06, 0.045), ("D",)),
    "Socket_Artefact": ((0.0, 0.055, 0.05), ("G",)),
    "Socket_Artefact_Anneau": ((0.0, -0.05, 0.105), ("G",)),
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
        if a.name.startswith("Pose_"):
            bpy.data.actions.remove(a)


# ----------------------------------------------------------------------------- un bras complet
def build_side(perso, side):
    sc = C.new_scene("SK_BrasFPS_%s_%s" % (perso, side))
    clear_actions()
    A = Arm(perso)
    mesh_ob, bones = A.build()
    skin(mesh_ob, A.parts, bones)
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
