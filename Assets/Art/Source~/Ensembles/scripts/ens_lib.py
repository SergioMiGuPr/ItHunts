# -*- coding: utf-8 -*-
"""Bibliothèque des pièces d'ensemble (nuit 6, lot 1) : petits objets et mobilier modélisés par script dans Blender, sans IA.

Différences avec kitlib : primitives chanfreinées, UV propres en mètres (fil du bois le long de la pièce, décalage au hasard
par planche), peinture de sommets par primitive (R saleté, G mousse, B teinte : deux planches voisines diffèrent).
Conventions de kitlib : Blender Z en haut, avant vers -Y, pivot au sol au centre ; export FBX Unity (+Y haut, +Z avant).
"""
import math, os, random, sys
import bpy, bmesh
from mathutils import Vector, Matrix, noise, Euler
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "..", "Kits", "scripts"))
import kitlib  # noqa: E402

ROOT = kitlib.ROOT
OUT = os.path.join(ROOT, "Assets", "Art", "Models", "Ensembles")
BOIS, SOMBRE, GRIS = "M_Ens_Bois", "M_Ens_BoisSombre", "M_Ens_BoisGris"
FER, LAITON, ETAIN = "M_Ens_Fer", "M_Ens_Laiton", "M_Ens_Etain"
PIERRE, CLAIRE, MOUSSUE = "M_Ens_Pierre", "M_Ens_PierreClaire", "M_Ens_PierreMoussue"
LIN, DRAP, ROUGE, BLEU, CUIR, JUTE = "M_Ens_Lin", "M_Ens_Drap", "M_Ens_VeloursRouge", "M_Ens_VeloursBleu", "M_Ens_Cuir", "M_Ens_Jute"
CIRE, FLAMME, OS, PAPIER, LIVRES, CUITE = "M_Kit_Cire", "M_Ens_Flamme", "M_Kit_Os", "M_Ens_Papier", "M_Ens_Livres", "M_Ens_TerreCuite"
FEUILLE, ECORCE, NOIR, EAU, BRAISE, VERRE = "M_Ens_Feuillage", "M_Ens_Ecorce", "M_Ens_Noir", "M_Ens_Eau", "M_Ens_Braise", "M_Ens_Verre"
TAPIS, PORTRAITS, VITRAIL, TAPISSERIE, OR, TERRE = "M_Ens_Tapis", "M_Ens_Portraits", "M_Ens_Vitrail", "M_Ens_Tapisserie", "M_Ens_Dorure", "M_Ens_Terreau"
GRIS2 = "M_Ens_Ardoise"


def rotm(rx=0, ry=0, rz=0):
    return Euler((math.radians(rx), math.radians(ry), math.radians(rz)), "XYZ").to_matrix()


class E:
    def __init__(self, nom, graine=1):
        self.nom, self.rnd = nom, random.Random(graine * 7919 + len(nom))
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.col = self.bm.verts.layers.float_color.new("Col")
        self.bf = bmesh.new()      # feuillage (cartes) : maillage à part
        self.uf = self.bf.loops.layers.uv.new("UVMap")
        self.cf = self.bf.verts.layers.float_color.new("Col")
        self.mats, self.empties, self.cols = [], [], []
        self.sale, self.mousse = 0.9, 0.0
        self.smooth = set()
        self.nuque = {}          # normales forcées (feuillage) : index de sommet -> direction

    def mi(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    # -------------------------------------------------------------- accumulation
    def _ajoute(self, src, M, mat, lisse=False, teinte=None, uvs=True):
        """Copie un bmesh temporaire (repère local, UV posées) dans la pièce."""
        b = self.rnd.random() if teinte is None else teinte
        idx = self.mi(mat)
        luv = src.loops.layers.uv.active
        vm = {}
        flip = M.determinant() < 0
        for v in src.verts:
            nv = self.bm.verts.new(M @ v.co)
            nv[self.col] = (0, 0, b, 1)
            vm[v] = nv
        for f in src.faces:
            vs = [vm[l.vert] for l in f.loops]
            us = [l[luv].uv.copy() for l in f.loops] if luv else None
            if flip:
                vs.reverse()
                us and us.reverse()
            if len(set(vs)) < 3:
                continue
            try:
                nf = self.bm.faces.new(vs)
            except ValueError:
                continue
            nf.material_index = idx
            nf.smooth = lisse
            if us:
                for l, u in zip(nf.loops, us):
                    l[self.uv].uv = u
        src.free()

    def _uvboite(self, bm, axe, ech=1.0):
        """UV par projection boîte en mètres ; `axe` (0, 1, 2) = direction du fil (le long de V)."""
        uv = bm.loops.layers.uv.verify()
        du, dv = self.rnd.random(), self.rnd.random()
        for f in bm.faces:
            n = f.normal
            a = [abs(n.x), abs(n.y), abs(n.z)]
            k = a.index(max(a))
            rest = [i for i in (0, 1, 2) if i != k]
            if axe in rest:
                v, u = axe, [i for i in rest if i != axe][0]
            else:
                u, v = rest
            for l in f.loops:
                c = l.vert.co
                l[uv].uv = (c[u] * ech + du + k * 0.37, c[v] * ech + dv)

    # -------------------------------------------------------------- primitives
    def boite(self, c, s, mat, rot=(0, 0, 0), bis=0.012, fil=None, taper=(1, 1), jit=0.0, base=False, teinte=None, seg=1):
        """Boîte chanfreinée centrée en c (base=True : c est le milieu du dessous), tailles s, rotation en degrés."""
        bm = bmesh.new()
        hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
        z0 = hz if base else 0
        V = {}
        for a in (-1, 1):
            for b in (-1, 1):
                for d in (-1, 1):
                    kx, ky = (taper if d > 0 else (1, 1))
                    j = lambda: self.rnd.uniform(-jit, jit)
                    V[a, b, d] = bm.verts.new((a * hx * kx + j(), b * hy * ky + j(), d * hz + z0 + j()))
        for q in (((-1, -1, -1), (-1, 1, -1), (1, 1, -1), (1, -1, -1)), ((-1, -1, 1), (1, -1, 1), (1, 1, 1), (-1, 1, 1)),
                  ((-1, -1, -1), (1, -1, -1), (1, -1, 1), (-1, -1, 1)), ((1, 1, -1), (-1, 1, -1), (-1, 1, 1), (1, 1, 1)),
                  ((-1, 1, -1), (-1, -1, -1), (-1, -1, 1), (-1, 1, 1)), ((1, -1, -1), (1, 1, -1), (1, 1, 1), (1, -1, 1))):
            bm.faces.new([V[k] for k in q])
        bis = min(bis, min(s) * 0.3)
        if bis > 0.001:
            bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bis, segments=seg, affect="EDGES", profile=0.5)
        bm.normal_update()
        self._uvboite(bm, (list(s).index(max(s)) if fil is None else fil))
        M = Matrix.Translation(Vector(c)) @ rotm(*rot).to_4x4()
        self._ajoute(bm, M, mat, teinte=teinte)

    def barre(self, a, b, l, h, mat, roulis=0, bis=0.01, jit=0.0, taper=1.0):
        """Pièce de bois ou de fer de section l × h entre a et b (fil dans la longueur)."""
        a, b = Vector(a), Vector(b)
        d = b - a
        L = d.length
        z = d.normalized()
        up = Vector((0, 0, 1)) if abs(z.z) < 0.95 else Vector((0, 1, 0))
        x = up.cross(z).normalized()
        y = z.cross(x).normalized()
        M = Matrix((x, y, z)).transposed()
        M = M @ Matrix.Rotation(math.radians(roulis), 3, "Z")
        bm = bmesh.new()
        V = []
        for k, dz in enumerate((0, L)):
            t = taper if k else 1
            V.append([bm.verts.new((sx * l / 2 * t + self.rnd.uniform(-jit, jit), sy * h / 2 * t + self.rnd.uniform(-jit, jit), dz))
                      for sx, sy in ((-1, -1), (1, -1), (1, 1), (-1, 1))])
        bm.faces.new(V[0][::-1])
        bm.faces.new(V[1])
        for i in range(4):
            j = (i + 1) % 4
            bm.faces.new([V[0][i], V[0][j], V[1][j], V[1][i]])
        bis = min(bis, min(l, h) * 0.3)
        if bis > 0.001:
            bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bis, segments=1, affect="EDGES", profile=0.5)
        bm.normal_update()
        self._uvboite(bm, 2)
        self._ajoute(bm, Matrix.Translation(a) @ M.to_4x4(), mat)

    def tour(self, c, prof, mat, n=14, rot=(0, 0, 0), ech=(1, 1, 1), lisse=True, ferme=True, bruit=0.0, teinte=None, arc=None):
        """Révolution d'un profil [(r, z), …] autour de Z, fermée aux bouts si le rayon n'est pas nul."""
        bm = bmesh.new()
        uv = bm.loops.layers.uv.verify()
        s = self.rnd.uniform(0, 50)
        long = [0]
        for k in range(1, len(prof)):
            long.append(long[-1] + math.hypot(prof[k][0] - prof[k - 1][0], prof[k][1] - prof[k - 1][1]))
        rings = []
        for r, z in prof:
            if r < 1e-4:
                rings.append([bm.verts.new((0, 0, z))] * n)
            else:
                ring = []
                for i in range(n):
                    a = 2 * math.pi * i / n
                    k = 1 + (bruit * noise.noise(Vector((math.cos(a) * 1.5 + s, math.sin(a) * 1.5, z * 2.2))) if bruit else 0)
                    ring.append(bm.verts.new((math.cos(a) * r * k, math.sin(a) * r * k, z)))
                rings.append(ring)
        du = self.rnd.random()
        rm = max(r for r, _ in prof)
        for k in range(len(prof) - 1):
            for i in range(n):
                j = (i + 1) % n
                q = [rings[k][i], rings[k][j], rings[k + 1][j], rings[k + 1][i]]
                uq = [(i, k), (i + 1, k), (i + 1, k + 1), (i, k + 1)]
                vs, us = [], []
                for v, u in zip(q, uq):
                    if v not in vs:
                        vs.append(v)
                        us.append(u)
                if len(vs) < 3:
                    continue
                f = bm.faces.new(vs)
                for l, (a, kk) in zip(f.loops, us):
                    l[uv].uv = (a / n * 2 * math.pi * rm + du, long[kk])
        if ferme:
            for k, rev in ((0, True), (-1, False)):
                if prof[k][0] > 1e-4:
                    f = bm.faces.new(rings[k][::-1] if rev else rings[k])
                    for l in f.loops:
                        l[uv].uv = (l.vert.co.x + du, l.vert.co.y)
        M = Matrix.Translation(Vector(c)) @ rotm(*rot).to_4x4() @ Matrix.Diagonal((*ech, 1))
        bm.normal_update()
        self._ajoute(bm, M, mat, lisse, teinte)

    def cyl(self, a, b, r, mat, r2=None, n=10, lisse=True, teinte=None):
        a, b = Vector(a), Vector(b)
        d = b - a
        q = d.to_track_quat("Z", "Y")
        bm = bmesh.new()
        uv = bm.loops.layers.uv.verify()
        r2 = r if r2 is None else r2
        L = d.length
        R0 = [bm.verts.new((math.cos(2 * math.pi * i / n) * r, math.sin(2 * math.pi * i / n) * r, 0)) for i in range(n)]
        R1 = [bm.verts.new((math.cos(2 * math.pi * i / n) * r2, math.sin(2 * math.pi * i / n) * r2, L)) for i in range(n)]
        du, dv = self.rnd.random(), self.rnd.random()
        for i in range(n):
            j = (i + 1) % n
            f = bm.faces.new([R0[i], R0[j], R1[j], R1[i]])
            for l, u in zip(f.loops, ((i, 0), (i + 1, 0), (i + 1, L), (i, L))):
                l[uv].uv = (u[0] / n * 2 * math.pi * max(r, r2) + du, u[1] + dv)
        for ring, rev in ((R0, True), (R1, False)):
            f = bm.faces.new(ring[::-1] if rev else ring)
            f.smooth = False
            for l in f.loops:
                l[uv].uv = (l.vert.co.x + du, l.vert.co.y + dv)
        self._ajoute(bm, Matrix.Translation(a) @ q.to_matrix().to_4x4(), mat, lisse, teinte)

    def tube(self, pts, rayons, mat, n=8, rugo=0.0, lisse=True, bout=True, teinte=None):
        pts = [Vector(p) for p in pts]
        if not hasattr(rayons, "__len__"):
            rayons = [rayons] * len(pts)
        bm = bmesh.new()
        uv = bm.loops.layers.uv.verify()
        s = Vector((self.rnd.uniform(0, 60), self.rnd.uniform(0, 60), 0))
        R, L = [], 0
        x0 = None
        Ls = []
        for i, p in enumerate(pts):
            d = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
            if x0 is None:
                a = Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((1, 0, 0))
                x = d.cross(a).normalized()
            else:
                x = (x0 - d * x0.dot(d)).normalized()
            x0 = x
            y = d.cross(x)
            if i:
                L += (p - pts[i - 1]).length
            Ls.append(L)
            ring = []
            for j in range(n):
                t = 2 * math.pi * j / n
                o = x * math.cos(t) + y * math.sin(t)
                r = rayons[i]
                if rugo:
                    r *= 1 + rugo * noise.noise((p + o * r) * 2.1 + s)
                ring.append(bm.verts.new(p + o * r))
            R.append(ring)
        rm = max(rayons)
        tours = max(1, round(2 * math.pi * rm)) if rm > 0.1 else 2 * math.pi * rm
        for i in range(len(pts) - 1):
            for j in range(n):
                k = (j + 1) % n
                f = bm.faces.new([R[i][j], R[i][k], R[i + 1][k], R[i + 1][j]])
                for l, u in zip(f.loops, ((j, i), (j + 1, i), (j + 1, i + 1), (j, i + 1))):
                    l[uv].uv = (u[0] / n * tours, Ls[u[1]])
        if bout:
            for ring, rev in ((R[0], True), (R[-1], False)):
                f = bm.faces.new(ring[::-1] if rev else ring)
                f.smooth = False
                for l in f.loops:
                    l[uv].uv = (l.vert.co.x, l.vert.co.y)
        self._ajoute(bm, Matrix.Identity(4), mat, lisse, teinte)

    def nappe(self, P, nu, nv, mat, ech=1.0, lisse=True, uvf=None, ferme_u=False, epais=0.0):
        """Surface libre P(u, v) -> Vector, u et v dans 0..1 ; UV en mètres (longueurs approchées)."""
        bm = bmesh.new()
        uv = bm.loops.layers.uv.verify()
        G = [[Vector(P(i / nu, j / nv)) for j in range(nv + 1)] for i in range(nu + 1)]
        V = [[bm.verts.new(p) for p in col] for col in G]
        Lu = sum((G[i + 1][nv // 2] - G[i][nv // 2]).length for i in range(nu))
        Lv = sum((G[nu // 2][j + 1] - G[nu // 2][j]).length for j in range(nv))
        for i in range(nu):
            for j in range(nv):
                q = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
                f = bm.faces.new([V[a][b] for a, b in q])
                for l, (a, b) in zip(f.loops, q):
                    l[uv].uv = uvf(a / nu, b / nv) if uvf else (a / nu * Lu * ech, b / nv * Lv * ech)
        if ferme_u:
            bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=1e-4)
        if epais:
            bm.normal_update()
            bmesh.ops.solidify(bm, geom=bm.faces[:], thickness=epais)
        bm.normal_update()
        self._ajoute(bm, Matrix.Identity(4), mat, lisse)

    def plan(self, c, w, h, mat, rot=(0, 0, 0), uv4=((0, 0), (1, 0), (1, 1), (0, 1))):
        """Plan vertical w × h centré en c, face vers -Y, UV données (bas gauche, bas droite, haut droite, haut gauche)."""
        bm = bmesh.new()
        uv = bm.loops.layers.uv.verify()
        vs = [bm.verts.new(p) for p in ((-w / 2, 0, -h / 2), (w / 2, 0, -h / 2), (w / 2, 0, h / 2), (-w / 2, 0, h / 2))]
        f = bm.faces.new(vs)
        for l, u in zip(f.loops, uv4):
            l[uv].uv = u
        self._ajoute(bm, Matrix.Translation(Vector(c)) @ rotm(*rot).to_4x4(), mat)

    def roche(self, c, s, mat=PIERRE, n=14, bis=0.03, rot=None, enfonce=0.15, teinte=None):
        """Bloc anguleux : enveloppe convexe de points au hasard, arêtes chanfreinées, posé au sol."""
        bm = bmesh.new()
        for _ in range(n):
            d = Vector((self.rnd.gauss(0, 1), self.rnd.gauss(0, 1), self.rnd.gauss(0, 1))).normalized()
            d *= self.rnd.uniform(0.85, 1.05)
            bm.verts.new((d.x * s[0] / 2, d.y * s[1] / 2, d.z * s[2] / 2))
        r = bmesh.ops.convex_hull(bm, input=bm.verts[:])
        bmesh.ops.delete(bm, geom=list({g for g in r["geom_interior"] + r["geom_unused"] if isinstance(g, bmesh.types.BMVert)}), context="VERTS")
        bmesh.ops.dissolve_limit(bm, angle_limit=0.22, verts=bm.verts[:], edges=bm.edges[:])
        bmesh.ops.bevel(bm, geom=bm.edges[:], offset=bis, segments=1, affect="EDGES", profile=0.5)
        zm = min(v.co.z for v in bm.verts)
        bm.normal_update()
        self._uvboite(bm, 2)
        rot = rot or (0, 0, self.rnd.uniform(0, 360))
        M = Matrix.Translation(Vector(c) + Vector((0, 0, -zm - s[2] * enfonce))) @ rotm(*rot).to_4x4()
        self._ajoute(bm, M, mat, teinte=teinte)

    def maillage(self, bm, mat, M=None, lisse=False, axe=2, ech=1.0):
        bm.normal_update()
        self._uvboite(bm, axe, ech)
        self._ajoute(bm, M or Matrix.Identity(4), mat, lisse)

    # -------------------------------------------------------------- feuillage en cartes
    CELL = {"buis": (0, 2), "lierre": (1, 2), "fougere": (2, 2), "herbe": (0, 1), "rose": (1, 1), "chene": (2, 1),
            "sapin": (0, 0), "ronce": (1, 0), "champi": (2, 0)}

    def carte(self, c, w, h, cell, dir=(0, -1, 0), roulis=0.0, pied=False, normale=None, plis=0.0, haut=None):
        """Carte de feuillage w × h face à `dir` ; pied=True : c est le milieu du bas, carte verticale."""
        i, j = self.CELL[cell]
        m = 0.012
        u0, u1, v0, v1 = i / 3 + m, (i + 1) / 3 - m, j / 3 + m, (j + 1) / 3 - m
        d = Vector(dir).normalized()
        up = Vector(haut) if haut is not None else (Vector((0, 0, 1)) if abs(d.z) < 0.9 else Vector((0, 1, 0)))
        x = up.cross(d).normalized()
        y = d.cross(x).normalized()
        if roulis:
            R = Matrix.Rotation(math.radians(roulis), 3, d)
            x, y = R @ x, R @ y
        c = Vector(c)
        o = c if pied else c - y * h / 2
        b = self.rnd.random()
        seg = 2 if plis else 1
        rows = []
        for k in range(seg + 1):
            t = k / seg
            off = d * (plis * (t ** 2)) if plis else Vector()
            rows.append([self.bf.verts.new(o + x * (sx * w / 2) + y * (h * t) + off) for sx in (-1, 1)])
        nn = Vector(normale).normalized() if normale is not None else d
        for r in rows:
            for v in r:
                v[self.cf] = (0, 0, b, 1)
                v.co.z = max(v.co.z, -.06)      # rien ne passe sous le sol
        for k in range(seg):
            f = self.bf.faces.new([rows[k][0], rows[k][1], rows[k + 1][1], rows[k + 1][0]])
            f.smooth = True
            va, vb = v0 + (v1 - v0) * k / seg, v0 + (v1 - v0) * (k + 1) / seg
            for l, u in zip(f.loops, ((u0, va), (u1, va), (u1, vb), (u0, vb))):
                l[self.uf].uv = u
            self.nuque[f] = nn

    def buisson(self, c, r, cell="buis", n=26, taille=0.9, aplati=1.0, bas=True):
        """Boule de feuillage : cartes sur une sphère, normales vers l'extérieur."""
        c = Vector(c)
        r = Vector(r if hasattr(r, "__len__") else (r, r, r))
        k = 0
        for i in range(n):
            t = (i + 0.5) / n
            z = 1 - 2 * t
            if not bas and z < -0.35:
                continue
            a = i * 2.39996 + self.rnd.uniform(-0.3, 0.3)
            s = math.sqrt(max(0, 1 - z * z))
            d = Vector((math.cos(a) * s, math.sin(a) * s, z))
            p = c + Vector((d.x * r.x, d.y * r.y, d.z * r.z)) * self.rnd.uniform(0.78, 0.98)
            dd = (d + Vector((self.rnd.uniform(-.35, .35), self.rnd.uniform(-.35, .35), self.rnd.uniform(-.2, .35)))).normalized()
            w = taille * self.rnd.uniform(0.8, 1.2)
            self.carte(p, w, w, cell, dd, self.rnd.uniform(-35, 35), normale=d + Vector((0, 0, 0.4)))

    def touffe(self, c, h, cell="herbe", n=3, larg=None, etale=0.0):
        a0 = self.rnd.uniform(0, 180)
        for i in range(n):
            a = math.radians(a0 + i * 180 / n)
            o = Vector(c) + Vector((self.rnd.uniform(-etale, etale), self.rnd.uniform(-etale, etale), -0.03))
            k = self.rnd.uniform(0.8, 1.15)
            self.carte(o, (larg or h) * k, h * k, cell, (math.cos(a), math.sin(a), 0), pied=True, normale=(0, 0, 1))

    # -------------------------------------------------------------- repères, collisions, sortie
    def vide(self, nom, p):
        self.empties.append((nom, Vector(p)))

    def col_boite(self, c, s, rot=(0, 0, 0), nom=None):
        """Boîte de collision seule (objet Col_*, centre c)."""
        self.cols.append((nom or f"Col_{len(self.cols)}", Vector(c), Vector(s), rot))

    def fin(self, sol=None):
        bm = self.bm
        # peinture : saleté (bas) et mousse (dessus), bord bruité
        bm.normal_update()
        if bm.verts:
            zm = min(v.co.z for v in bm.verts) if sol is None else sol
            o = Vector((self.rnd.uniform(0, 90), self.rnd.uniform(0, 90), 0))
            for v in bm.verts:
                z = v.co.z - zm
                w = noise.noise(v.co * 1.3 + o) * 0.35
                r = max(0, min(1, 1 - (z + w * self.sale) / max(self.sale, 0.01))) if self.sale else 0
                mo = noise.noise(v.co * 0.8 + o) * 0.5 + 0.5
                g = self.mousse * max(0, min(1, (mo - 0.35) * 2.2)) * (0.45 + 0.55 * max(0, v.normal.z))
                v[self.col] = (r * 0.85, g, v[self.col][2], 1)
        def objet(b, nom, mats, norm):
            me = bpy.data.meshes.new(nom)
            li, N = 0, {}
            for f in b.faces:
                for l in f.loops:
                    N[li] = norm.get(f)
                    li += 1
            b.to_mesh(me)
            me.color_attributes.active_color_index = 0
            if norm:
                me.normals_split_custom_set([tuple(N[i]) for i in range(len(N))])
            b.free()
            o = bpy.data.objects.new(nom, me)
            bpy.context.scene.collection.objects.link(o)
            for m in mats:
                me.materials.append(kitlib.mat(m))
            return o
        if len(bm.faces) == 0:
            bm.free()
            ob = objet(self.bf, self.nom, [FEUILLE], self.nuque)
        else:
            ob = objet(bm, self.nom, self.mats, {})
            if len(self.bf.faces):
                of = objet(self.bf, "Feuillage", [FEUILLE], self.nuque)
                of.parent = ob
        for nom, p in self.empties:
            e = bpy.data.objects.new(nom, None)
            e.location = p
            e.parent = ob
            bpy.context.scene.collection.objects.link(e)
        for nom, c, s, rot in self.cols:
            b2 = bmesh.new()
            bmesh.ops.create_cube(b2, size=1)
            b2.transform(Matrix.Translation(c) @ rotm(*rot).to_4x4() @ Matrix.Diagonal((*s, 1)))
            m2 = bpy.data.meshes.new(nom)
            b2.to_mesh(m2)
            b2.free()
            o2 = bpy.data.objects.new(nom, m2)
            m2.materials.append(kitlib.mat("M_Collision_Invisible"))
            o2.parent = ob
            bpy.context.scene.collection.objects.link(o2)
        return ob


PIECES = {}


def piece(nom, graine=1, **kw):
    def deco(f):
        def construit():
            e = E("SM_Ens_" + nom, graine)
            for k, v in kw.items():
                setattr(e, k, v)
            f(e)
            return e.fin()
        construit.module = f.__module__
        PIECES[nom] = construit
        return f
    return deco


def exporte(noms=None):
    import faces_superposees as FS
    import json
    rap = {}
    chemin = os.path.join(ROOT, "docs", "art", "ensembles", "pieces.json")
    if os.path.exists(chemin):
        rap = json.load(open(chemin, encoding="utf-8"))
    for nom, f in PIECES.items():
        if noms and nom not in noms:
            continue
        kitlib.reset()
        ob = f()
        # les cartes de feuillage se croisent par nature : le nettoyage des faces ne s'applique pas à elles
        feu = any(m and m.name == FEUILLE for o in [ob] + list(ob.children) if o.type == "MESH" for m in o.data.materials)
        sauve = FS.nettoie
        FS.nettoie = lambda o: None if any(m and m.name == FEUILLE for m in o.data.materials) else sauve(o)
        try:
            kitlib.export([ob], os.path.join(OUT, ob.name + ".fbx"))
        finally:
            FS.nettoie = sauve
        vis = [o for o in [ob] + list(ob.children) if o.type == "MESH" and not o.name.startswith("Col_")]
        bb = [kitlib.bounds(o) for o in vis]
        (x0, x1), (y0, y1), (z0, z1) = [(min(b[k][0] for b in bb), max(b[k][1] for b in bb)) for k in range(3)]
        rap[nom] = dict(x=round(x1 - x0, 2), z=round(y1 - y0, 2), h=round(z1, 2), bas=round(z0, 2), tris=sum(kitlib.tris(o) for o in vis), feuillage=feu,
                        mats=[m.name for m in ob.data.materials], col=len([c for c in ob.children if c.name.startswith("Col_")]))
        print("PIECE", nom, rap[nom]["x"], rap[nom]["z"], rap[nom]["h"], rap[nom]["tris"], flush=True)
    os.makedirs(os.path.dirname(chemin), exist_ok=True)
    json.dump(rap, open(chemin, "w", encoding="utf-8"), indent=1, ensure_ascii=False, sort_keys=True)
