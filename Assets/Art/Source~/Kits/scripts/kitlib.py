# -*- coding: utf-8 -*-
"""Bibliothèque commune des kits de décor (actes I à III, nuit du 2026-11-22).

Modélisation par script dans Blender (bmesh), sans génération IA. Les matériaux d'architecture sont ceux de la
bibliothèque (`M_Bib_*`, shader `ItHunts/Toon` en UV monde et mode décor) : aucune UV à déplier, la texture suit le monde.
Chaque pièce reçoit une **peinture de sommets** (attribut de couleur « Col », flottant, exporté en linéaire) :
R = saleté (bas de la pièce, creux), G = mousse ou salpêtre (plaques), B = teinte de la pièce (tirée au hasard) ; c'est ce
que lisent les matériaux `*_Peint` (`_VertexPaint`).

Conventions : Blender Z en haut, l'avant de la pièce vers -Y ; export FBX : Unity +Y en haut, +Z vers l'avant, échelle 1.
Pivot au sol, au centre de l'emprise, sauf mention. 1 unité = 1 m. Échelle du jeu : joueur 1.8 m, traqueur 14 m, salles de
46 à 66 m, portes des modules 8 × 10 m (ART-HANDOFF.md § 0).
"""
import math, os, random
import bpy, bmesh
from mathutils import Vector, Matrix, noise

ROOT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "..", "..", ".."))


# ------------------------------------------------------------------ scène
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for c in list(bpy.data.collections):
        bpy.data.collections.remove(c)


def mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
    return m


# ------------------------------------------------------------------ constructeur de maillage
class K:
    """Accumule de la géométrie (bmesh) avec un matériau par face."""

    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.mats = []
        self.empties = []

    def mi(self, m):
        if m not in self.mats:
            self.mats.append(m)
        return self.mats.index(m)

    def face(self, pts, m):
        vs = [self.bm.verts.new(Vector(p)) for p in pts]
        f = self.bm.faces.new(vs)
        f.material_index = self.mi(m)
        return f

    def grid(self, P, nu, nv, m, closed_u=False, flip=False):
        """Surface en grille : P(i, j) -> Vector, i in 0..nu, j in 0..nv."""
        cols = nu if closed_u else nu + 1
        V = [[self.bm.verts.new(P(i, j)) for j in range(nv + 1)] for i in range(cols)]
        idx = self.mi(m)
        for i in range(nu):
            i2 = (i + 1) % cols
            for j in range(nv):
                q = [V[i][j], V[i2][j], V[i2][j + 1], V[i][j + 1]]
                if flip:
                    q.reverse()
                f = self.bm.faces.new(q)
                f.material_index = idx
        return V

    def box(self, c, s, m, rot=0.0, skip=(), taper=0.0):
        """Boîte centrée en c (x, y, z du centre), tailles s ; rot autour de Z (radians) ; taper : rétrécit le dessus."""
        cx, cy, cz = c
        hx, hy, hz = s[0] / 2, s[1] / 2, s[2] / 2
        R = Matrix.Rotation(rot, 3, "Z")
        def P(a, b, d):
            k = 1 - taper if d > 0 else 1
            return Vector((cx, cy, cz)) + R @ Vector((a * hx * k, b * hy * k, d * hz))
        F = {
            "-z": [P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), P(1, -1, -1)],
            "+z": [P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1)],
            "-y": [P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), P(-1, -1, 1)],
            "+y": [P(1, 1, -1), P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1)],
            "-x": [P(-1, 1, -1), P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1)],
            "+x": [P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), P(1, -1, 1)],
        }
        for k, pts in F.items():
            if k not in skip:
                self.face(pts, m)

    def obox(self, a, b, w, h, m, up=(0, 0, 1)):
        """Poutre de section w × h entre a et b."""
        a, b = Vector(a), Vector(b)
        d = (b - a)
        L = d.length
        d.normalize()
        u = Vector(up)
        if abs(d.dot(u)) > 0.95:
            u = Vector((1, 0, 0))
        x = d.cross(u).normalized()
        z = x.cross(d).normalized()
        def P(s, i, j):
            return a + d * s * L + x * i * w / 2 + z * j * h / 2
        q = [(-1, -1), (1, -1), (1, 1), (-1, 1)]
        for k in range(4):
            i0, j0 = q[k]
            i1, j1 = q[(k + 1) % 4]
            self.face([P(0, i0, j0), P(0, i1, j1), P(1, i1, j1), P(1, i0, j0)], m)
        self.face([P(0, *q[3]), P(0, *q[2]), P(0, *q[1]), P(0, *q[0])], m)
        self.face([P(1, *q[0]), P(1, *q[1]), P(1, *q[2]), P(1, *q[3])], m)

    def cyl(self, c, r, h, m, n=12, r2=None, caps=(True, True), axis="z", rough=0.0, seed=0):
        """Cylindre (ou cône si r2) le long d'un axe, base en c."""
        r2 = r if r2 is None else r2
        c = Vector(c)
        def P(i, j):
            a = 2 * math.pi * i / n
            rr = (r if j == 0 else r2)
            if rough:
                rr *= 1 + rough * (noise.noise(Vector((math.cos(a) * 2 + seed, math.sin(a) * 2, j * 1.3))) )
            x, y = math.cos(a) * rr, math.sin(a) * rr
            if axis == "z":
                return c + Vector((x, y, j * h))
            if axis == "x":
                return c + Vector((j * h, x, y))
            return c + Vector((x, j * h, y))
        V = self.grid(P, n, 1, m, closed_u=True)
        idx = self.mi(m)
        if caps[0]:
            f = self.bm.faces.new([V[i][0] for i in reversed(range(n))]); f.material_index = idx
        if caps[1]:
            f = self.bm.faces.new([V[i][1] for i in range(n)]); f.material_index = idx

    def lathe(self, c, prof, m, n=16):
        """Révolution d'un profil [(r, z), …] autour de Z."""
        c = Vector(c)
        def P(i, j):
            a = 2 * math.pi * i / n
            r, z = prof[j]
            return c + Vector((math.cos(a) * r, math.sin(a) * r, z))
        self.grid(P, n, len(prof) - 1, m, closed_u=True)

    def empty(self, name, loc, size=0.3):
        self.empties.append((name, Vector(loc), size))

    def finish(self, seed=0, peinture=None, collection=None, smooth=False):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=0.0005)
        bmesh.ops.recalc_face_normals(self.bm, faces=self.bm.faces)
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me)
        self.bm.free()
        ob = bpy.data.objects.new(self.name, me)
        (collection or bpy.context.scene.collection).objects.link(ob)
        for m in self.mats:
            me.materials.append(mat(m))
        for p in me.polygons:
            p.use_smooth = smooth
        peindre(ob, seed, **(peinture or {}))
        for name, loc, size in self.empties:
            e = bpy.data.objects.new(name, None)
            e.empty_display_size = size
            e.location = loc
            e.parent = ob
            (collection or bpy.context.scene.collection).objects.link(e)
        return ob


# ------------------------------------------------------------------ roche organique
def fbm3(p, oct=4, lac=2.0, gain=0.5):
    s, a, f = 0.0, 1.0, 1.0
    for _ in range(oct):
        s += a * noise.noise(p * f)
        a *= gain
        f *= lac
    return s


def rock_wall(k, pts, height, m, thick=1.5, seg=0.9, amp=0.7, seed=0, base_flare=0.8, top_jag=1.2, closed=False,
              inward=1.0, dz=0.0):
    """Paroi rocheuse le long d'une polyligne `pts` [(x, y), …] (vue de dessus) : la face rocheuse regarde du côté
    gauche du parcours (sens de la polyligne) multiplié par `inward`. Surface bruitée (bosses de 1 à 3 m, strates),
    pied évasé, crête dentelée ; dos plat (caché) à `thick` derrière."""
    # rééchantillonnage régulier du parcours
    P = [Vector((x, y, 0)) for x, y in pts]
    if closed:
        P.append(P[0])
    L = [0.0]
    for i in range(1, len(P)):
        L.append(L[-1] + (P[i] - P[i - 1]).length)
    nu = max(2, int(L[-1] / seg))
    def at(s):
        s = min(max(s, 0), L[-1])
        for i in range(1, len(P)):
            if s <= L[i] or i == len(P) - 1:
                t = (s - L[i - 1]) / max(L[i] - L[i - 1], 1e-6)
                p = P[i - 1].lerp(P[i], t)
                d = (P[i] - P[i - 1]).normalized()
                return p, d
    nv = max(2, int(height / seg))
    off = Vector((seed * 17.3, seed * 7.1, seed * 3.7))
    front = []
    for i in range(nu + 1):
        p, d = at(L[-1] * i / nu)
        n = Vector((-d.y, d.x, 0)) * inward
        col = []
        for j in range(nv + 1):
            z = height * j / nv
            q = p + Vector((0, 0, z))
            w = fbm3((q + off) * 0.28, 4) * amp * 2.0 + fbm3((q + off) * 0.9, 2) * amp * 0.5
            strata = math.sin(z * 1.7 + fbm3((q + off) * 0.15) * 3) * amp * 0.25
            flare = base_flare * max(0.0, 1 - z / 2.5) ** 2
            top = 0.0
            if j == nv:
                top = (fbm3((q + off) * 0.6, 2) * 0.5 + 0.5) * top_jag
            col.append(q + n * (w + strata + flare) + Vector((0, 0, top + dz)))
        front.append(col)
    # face rocheuse
    def F(i, j):
        return front[i][j]
    k.grid(F, nu, nv, m, flip=(inward < 0))
    # dos plat et dessus
    back = []
    for i in range(nu + 1):
        p, d = at(L[-1] * i / nu)
        n = Vector((-d.y, d.x, 0)) * inward
        back.append([p - n * thick + Vector((0, 0, dz)), p - n * thick + Vector((0, 0, height + dz))])
    for i in range(nu):
        a, b = (i, i + 1)
        q = [back[a][0], back[b][0], back[b][1], back[a][1]]
        if inward > 0:
            q.reverse()
        k.face(q, m)
        t = [front[a][nv], front[b][nv], back[b][1], back[a][1]]
        if inward < 0:
            t.reverse()
        k.face(t, m)
    if not closed:
        for i, sgn in ((0, 1), (nu, -1)):
            ring = [front[i][j] for j in range(nv + 1)] + [back[i][1], back[i][0]]
            if (sgn * inward) < 0:
                ring.reverse()
            k.face(ring, m)


def rock_blob(k, c, size, m, seed=0, res=10, amp=0.35, flat_base=True):
    """Bloc de roche (éboulis, rocher) : sphère déformée, base aplatie."""
    c = Vector(c)
    sx, sy, sz = size
    off = Vector((seed * 5.1, seed * 2.3, seed * 9.7))
    def P(i, j):
        a = 2 * math.pi * i / res
        b = math.pi * j / (res // 2)
        d = Vector((math.sin(b) * math.cos(a), math.sin(b) * math.sin(a), -math.cos(b)))
        r = 1 + amp * fbm3(d * 1.6 + off, 3)
        p = Vector((d.x * sx / 2 * r, d.y * sy / 2 * r, d.z * sz / 2 * r))
        if flat_base and p.z < -sz * 0.3:
            p.z = -sz * 0.3
        return c + p + Vector((0, 0, sz * 0.3))
    V = k.grid(P, res, res // 2, m, closed_u=True)


def arch_profile(span, rise, n=12, pointed=False):
    """Intrados d'un arc de -span/2 à +span/2 : plein cintre (demi-ellipse de hauteur `rise`) ou arc brisé (deux arcs
    de cercle de rayon = span, ramenés à la hauteur `rise`)."""
    pts = []
    for i in range(n + 1):
        t = i / n
        if pointed:
            x = -span / 2 + span * t
            r = span
            cx = span / 2 if x <= 0 else -span / 2
            z = math.sqrt(max(r * r - (x - cx) ** 2, 0.0))
            z *= rise / (span * 0.8660254)
        else:
            a = math.pi * t
            x = -math.cos(a) * span / 2
            z = math.sin(a) * rise
        pts.append((x, z))
    return pts


def masonry_arch(k, span, spring, rise, depth, pier_w, m, n=12, pointed=False, top=None, broken=0.0, seed=0, voussoir=True):
    """Arche maçonnée : deux piédroits (pier_w) et un arc d'intrados `span` × `rise` posé à `spring` ; mur plein
    jusqu'à `top` (sinon arc seul avec claveaux). `broken` > 0 : la partie droite de l'arc manque (ruine)."""
    rnd = random.Random(seed)
    top = top or (spring + rise + 1.0)
    hw = span / 2 + pier_w
    prof = arch_profile(span, rise, n, pointed)
    y0, y1 = -depth / 2, depth / 2
    # piédroits
    k.box((-span / 2 - pier_w / 2, 0, spring / 2), (pier_w, depth, spring), m)
    if broken < 0.99:
        k.box((span / 2 + pier_w / 2, 0, spring / 2), (pier_w, depth, spring * (1 - broken * 0.5)), m)
    # tympan : on remplit entre l'intrados et le dessus, colonne par colonne
    nmax = n if broken <= 0 else int(n * (1 - broken))
    for i in range(nmax):
        (xa, za), (xb, zb) = prof[i], prof[i + 1]
        za += spring
        zb += spring
        tt = top if not broken else top - rnd.uniform(0, 2.5) * (i / n)
        # face avant et arrière
        for y, rev in ((y0, False), (y1, True)):
            q = [(xa, y, za), (xb, y, zb), (xb, y, tt), (xa, y, tt)]
            k.face(list(reversed(q)) if rev else q, m)
        # intrados
        q = [(xa, y0, za), (xa, y1, za), (xb, y1, zb), (xb, y0, zb)]
        k.face(q, m)
        k.face([(xa, y0, tt), (xb, y0, tt), (xb, y1, tt), (xa, y1, tt)], m)
    # parties pleines au-dessus des piédroits (jusqu'au dessus)
    k.box((-span / 2 - pier_w / 2, 0, (spring + top) / 2), (pier_w, depth, top - spring), m, skip=("-z",))
    if not broken:
        k.box((span / 2 + pier_w / 2, 0, (spring + top) / 2), (pier_w, depth, top - spring), m, skip=("-z",))
    # archivolte : bandeau mouluré qui suit l'arc (saillie de 0.15 m, 0.8 m de large), lecture nette à l'encre
    if voussoir:
        ring = []
        for (x, z) in prof[: nmax + 1]:
            # normale extérieure approchée : depuis le centre de l'arc
            cx, cz = 0.0, 0.0
            d = Vector((x - cx, 0, z - cz))
            d = d.normalized() if d.length > 1e-4 else Vector((0, 0, 1))
            ring.append((Vector((x, 0, z + spring)), d))
        for y in (y0 - 0.12, y1 + 0.12):
            for i in range(len(ring) - 1):
                (pa, da), (pb, db) = ring[i], ring[i + 1]
                ca, cb = pa + da * 0.42, pb + db * 0.42
                k.obox((ca.x, y, ca.z), (cb.x, y, cb.z), 0.84, 0.3, m, up=(0, 1, 0))
        # clé d'arc en saillie
        if not broken:
            xk, zk = prof[len(prof) // 2]
            k.box((xk, 0, zk + spring + 0.45), (0.9, depth + 0.5, 1.1), m, taper=0.15)


# ------------------------------------------------------------------ peinture de sommets
def peindre(ob, seed=0, sale_h=2.2, mousse=0.5, mousse_ech=0.25, teinte=None, sol=None):
    """R saleté (depuis le bas de la pièce ou `sol`), G mousse en plaques (plus en bas et sur les dessus),
    B teinte de la pièce (constante, tirée au hasard)."""
    me = ob.data
    if not len(me.vertices):
        return
    rnd = random.Random(seed)
    b = rnd.random() if teinte is None else teinte
    zmin = min(v.co.z for v in me.vertices) if sol is None else sol
    attr = me.color_attributes.new(name="Col", type="FLOAT_COLOR", domain="POINT")
    off = Vector((rnd.uniform(0, 100), rnd.uniform(0, 100), rnd.uniform(0, 100)))
    vn = [v.normal for v in me.vertices]
    for i, v in enumerate(me.vertices):
        z = v.co.z - zmin
        w = fbm3((v.co + off) * 0.5, 2) * 0.35
        r = max(0.0, min(1.0, 1 - (z + w * sale_h) / max(sale_h, 0.01)))
        mo = fbm3((v.co + off) * mousse_ech, 3) * 0.5 + 0.5
        bas = max(0.0, 1 - z / 6.0)
        haut = max(0.0, vn[i].z)
        g = max(0.0, min(1.0, (mo - 0.55) * 3.0)) * mousse * (0.4 + 0.6 * max(bas, haut))
        attr.data[i].color = (r, g, b, 1.0)
    me.color_attributes.active_color = attr


# ------------------------------------------------------------------ export
def export(objs, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    sel = []
    for ob in objs:
        sel.append(ob)
        sel.extend(ob.children)
    for ob in sel:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH", "EMPTY"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="FACE", use_triangles=True, use_tspace=False,
        colors_type="LINEAR", prioritize_active_color=True,
        add_leaf_bones=False, bake_anim=False, path_mode="RELATIVE", embed_textures=False,
        use_custom_props=False, use_metadata=True)


def tris(ob):
    return sum(len(p.vertices) - 2 for p in ob.data.polygons)


def bounds(ob):
    xs = [v.co.x for v in ob.data.vertices]
    ys = [v.co.y for v in ob.data.vertices]
    zs = [v.co.z for v in ob.data.vertices]
    return (min(xs), max(xs)), (min(ys), max(ys)), (min(zs), max(zs))


# ------------------------------------------------------------------ tubes (troncs, branches, racines)
def tube(k, pts, radii, m, sides=10, rough=0.0, seed=0, cap=True):
    """Tube le long d'une polyligne 3D `pts` avec un rayon par point ; `rough` : bosses (écorce noueuse)."""
    pts = [Vector(p) for p in pts]
    n = len(pts)
    frames = []
    up = Vector((0, 0, 1))
    for i in range(n):
        d = (pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized()
        a = up if abs(d.dot(up)) < 0.9 else Vector((1, 0, 0))
        x = d.cross(a).normalized()
        y = d.cross(x).normalized()
        frames.append((x, y))
    off = Vector((seed * 3.1, seed * 1.7, seed * 2.3))
    def P(j, i):
        x, y = frames[i]
        t = 2 * math.pi * j / sides
        r = radii[i]
        if rough:
            q = pts[i] + (x * math.cos(t) + y * math.sin(t)) * r
            r *= 1 + rough * noise.noise((q + off) * 1.3)
        return pts[i] + (x * math.cos(t) + y * math.sin(t)) * r
    V = k.grid(P, sides, n - 1, m, closed_u=True)
    if cap:
        f = k.bm.faces.new([V[j][n - 1] for j in range(sides)])
        f.material_index = k.mi(m)
