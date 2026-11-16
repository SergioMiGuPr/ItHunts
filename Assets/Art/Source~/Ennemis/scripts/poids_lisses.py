# -*- coding: utf-8 -*-
"""Poids de déformation lissés pour les ennemis (retours de playtest du 2026-11-16 : la goule et le traqueur se
déformaient beaucoup).

Les maillages Tripo ne sont pas étanches (trous, îlots, faces retournées) : la chaleur de Blender (poids automatiques)
y échoue par endroits, ce qui donnait des sommets rattachés à un seul os et des pincements. Ici :
1. une copie étanche du maillage (remaillage en voxels) reçoit les poids par la chaleur, qui y sont propres et diffus ;
2. chaque sommet du vrai maillage prend les poids du point le plus proche de la copie (interpolés sur le triangle) ;
3. masque facultatif des os permis par sommet (pour qu'un bras ne tire jamais le torse, ni la mâchoire le crâne) ;
4. lissage des poids le long des arêtes du maillage (rampes plus larges : volumes mieux gardés en flexion) ;
5. îlots détachés (pagne, pointes, mèches) : poids du point le plus proche du corps ;
6. 4 os au plus par sommet (Unity), somme 1.
"""
import bpy, bmesh
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from mathutils import kdtree


class _only_deform:
    """Pendant la chaleur, seuls les os de names déforment (les os d'aide sont répartis ensuite)."""
    def __init__(self, arm_ob, names):
        self.arm, self.names, self.saved = arm_ob, set(names), {}
    def __enter__(self):
        for b in self.arm.data.bones:
            self.saved[b.name] = b.use_deform
            b.use_deform = b.name in self.names
    def __exit__(self, *a):
        for b in self.arm.data.bones:
            b.use_deform = self.saved[b.name]


def _sel(objs, active):
    vl = bpy.context.view_layer
    for o in vl.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    vl.objects.active = active


def proxy_weights(ob, arm_ob, voxel, names):
    """Copie étanche (voxels) de ob, poids par la chaleur ; renvoie (coords, triangles, W[nv, nb])."""
    tmp = ob.copy()
    tmp.data = ob.data.copy()
    tmp.modifiers.clear()
    tmp.vertex_groups.clear()
    tmp.parent = None
    tmp.matrix_world = ob.matrix_world.copy()
    bpy.context.scene.collection.objects.link(tmp)
    m = tmp.modifiers.new("Voxels", "REMESH")
    m.mode = "VOXEL"; m.voxel_size = voxel; m.adaptivity = 0.0; m.use_smooth_shade = True
    dg = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(tmp.evaluated_get(dg))
    tmp.modifiers.clear()
    old = tmp.data
    tmp.data = me
    bpy.data.meshes.remove(old)
    # le squelette au repos, aucun os posé
    _sel([tmp, arm_ob], arm_ob)
    with _only_deform(arm_ob, names), bpy.context.temp_override(selected_objects=[tmp, arm_ob],
                                                                 selected_editable_objects=[tmp, arm_ob],
                                                                 active_object=arm_ob, object=arm_ob):
        bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    nv = len(me.vertices)
    W = np.zeros((nv, len(names)), np.float32)
    gi = {g.index: g.name for g in tmp.vertex_groups}
    idx = {n: i for i, n in enumerate(names)}
    for v in me.vertices:
        for g in v.groups:
            n = gi[g.group]
            if n in idx:
                W[v.index, idx[n]] = g.weight
    co = np.array([v.co[:] for v in me.vertices], np.float64)
    me.calc_loop_triangles()
    tris = np.array([t.vertices[:] for t in me.loop_triangles], np.int64)
    # sommets oubliés par la chaleur : os le plus proche (segment)
    miss = W.sum(1) < 1e-4
    bpy.data.objects.remove(tmp, do_unlink=True)
    bpy.data.meshes.remove(me)
    return co, tris, W, miss


def distance_weights(co, arm_ob, names, allowed=None, k=3, soft=0.02):
    """Poids doux par distance aux os (1/(d+e)^4 sur les k os permis les plus proches ; e = soft × taille du modèle)."""
    bones = [(np.array(arm_ob.data.bones[n].head_local[:]), np.array(arm_ob.data.bones[n].tail_local[:])) for n in names]
    D = np.stack([seg_dist(co, a, b) for a, b in bones], 1)
    e = soft * float(np.ptp(co, 0).max())
    Wd = 1.0 / (D + e) ** 4
    if allowed is not None:
        Wd = np.where(allowed, Wd, 0.0)
    order = np.argsort(-Wd, 1)
    keep = np.zeros_like(Wd, dtype=bool)
    np.put_along_axis(keep, order[:, :k], True, 1)
    Wd = np.where(keep, Wd, 0.0)
    return (Wd / np.maximum(Wd.sum(1, keepdims=True), 1e-30)).astype(np.float32)


def seg_dist(P, a, b):
    ab = b - a
    t = np.clip(((P - a) @ ab) / max(float(ab @ ab), 1e-12), 0, 1)
    return np.linalg.norm(P - (a + t[:, None] * ab), axis=1)


def transfer(co_src, tris, W_src, co_dst):
    """Poids du point le plus proche de la surface source (barycentriques)."""
    bvh = BVHTree.FromPolygons([Vector(c) for c in co_src], tris.tolist())
    out = np.zeros((len(co_dst), W_src.shape[1]), np.float32)
    for i, p in enumerate(co_dst):
        loc, nrm, fi, d = bvh.find_nearest(Vector(p))
        a, b, c = (co_src[k] for k in tris[fi])
        v0, v1, v2 = b - a, c - a, np.array(loc[:]) - a
        d00, d01, d11 = v0 @ v0, v0 @ v1, v1 @ v1
        d20, d21 = v2 @ v0, v2 @ v1
        den = d00 * d11 - d01 * d01
        if abs(den) < 1e-18:
            l = np.array((1.0, 0.0, 0.0))
        else:
            vv = (d11 * d20 - d01 * d21) / den
            ww = (d00 * d21 - d01 * d20) / den
            l = np.clip(np.array((1 - vv - ww, vv, ww)), 0, 1)
            l /= max(l.sum(), 1e-9)
        out[i] = l @ W_src[tris[fi]]
    return out


def components(nv, edges):
    parent = np.arange(nv)
    def find(x):
        while parent[x] != x:
            parent[x] = parent[parent[x]]
            x = parent[x]
        return x
    for a, b in edges:
        ra, rb = find(a), find(b)
        if ra != rb:
            parent[ra] = rb
    return np.array([find(i) for i in range(nv)])


def smooth(W, edges, it, lam=0.5, allowed=None, fixed=None):
    i, j = edges[:, 0], edges[:, 1]
    deg = np.bincount(np.concatenate([i, j]), minlength=len(W)).astype(np.float32)
    for _ in range(it):
        acc = np.zeros_like(W)
        np.add.at(acc, i, W[j]); np.add.at(acc, j, W[i])
        has = deg > 0
        new = W.copy()
        new[has] = (1 - lam) * W[has] + lam * acc[has] / deg[has, None]
        if allowed is not None:
            new = np.where(allowed, new, 0.0)
        if fixed is not None:
            new[fixed] = W[fixed]
        new /= np.maximum(new.sum(1, keepdims=True), 1e-9)
        W = new
    return W


def prune(W, k=4, eps=0.01):
    order = np.argsort(-W, 1)
    keep = np.zeros_like(W, dtype=bool)
    np.put_along_axis(keep, order[:, :k], True, 1)
    W = np.where(keep & (W > eps), W, 0.0)
    return W / np.maximum(W.sum(1, keepdims=True), 1e-9)


def write_groups(ob, arm_ob, names, W):
    ob.vertex_groups.clear()
    groups = {n: ob.vertex_groups.new(name=n) for n in names}
    for j, n in enumerate(names):
        for i in np.nonzero(W[:, j] > 0)[0]:
            groups[n].add([int(i)], float(W[i, j]), "REPLACE")
    ob.parent = arm_ob
    ob.matrix_parent_inverse.identity()
    if not any(m.type == "ARMATURE" for m in ob.modifiers):
        m = ob.modifiers.new("Armature", "ARMATURE"); m.object = arm_ob


def heat_original(ob, arm_ob, names):
    """Chaleur directement sur le maillage d'origine (copie) : utile là où la copie en voxels ferme une ouverture
    (bouche) ; renvoie W[nv, nb] (lignes vides là où la chaleur a échoué)."""
    tmp = ob.copy(); tmp.data = ob.data.copy(); tmp.modifiers.clear(); tmp.vertex_groups.clear(); tmp.parent = None
    tmp.matrix_world = ob.matrix_world.copy()
    bpy.context.scene.collection.objects.link(tmp)
    _sel([tmp, arm_ob], arm_ob)
    with _only_deform(arm_ob, names), bpy.context.temp_override(selected_objects=[tmp, arm_ob],
                                                                 selected_editable_objects=[tmp, arm_ob],
                                                                 active_object=arm_ob, object=arm_ob):
        bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    W = np.zeros((len(tmp.data.vertices), len(names)), np.float32)
    gi = {g.index: g.name for g in tmp.vertex_groups}
    idx = {n: i for i, n in enumerate(names)}
    for v in tmp.data.vertices:
        for g in v.groups:
            if gi[g.group] in idx:
                W[v.index, idx[gi[g.group]]] = g.weight
    me = tmp.data
    bpy.data.objects.remove(tmp, do_unlink=True); bpy.data.meshes.remove(me)
    s = W.sum(1, keepdims=True)
    return np.where(s > 1e-6, W / np.maximum(s, 1e-9), 0.0)


def skin_lisse(ob, arm_ob, voxel, names=None, allowed=None, smooth_it=8, lam=0.5, exclude=("Racine",), helpers=(), mix=None):
    """Poids lissés sur ob (maillage au repos du squelette). allowed : tableau booléen [nv, nb] facultatif (os permis
    par sommet, même ordre que names). Renvoie (names, W, rapport)."""
    if names is None:
        names = [b.name for b in arm_ob.data.bones if b.use_deform and b.name not in exclude and "_Aide" not in b.name]
    me = ob.data
    co = np.array([v.co[:] for v in me.vertices], np.float64)
    pco, ptris, PW, pmiss = proxy_weights(ob, arm_ob, voxel, names)
    W = transfer(pco, ptris, np.concatenate([PW, pmiss[:, None].astype(np.float32)], 1), co)
    fb = W[:, -1] > 0.01                     # sommets où la chaleur a échoué (copie) : poids doux par distance
    W = W[:, :-1]
    if fb.any():
        Wd = distance_weights(co[fb], arm_ob, names, None if allowed is None else allowed[fb])
        W[fb] = Wd
    miss = int(fb.sum())
    if mix is not None:                      # correction locale (ex. tête : chaleur sur le maillage d'origine)
        W = mix(co, W, names)
    if allowed is not None:
        Wm = np.where(allowed, W, 0.0)
        # sommets dont tous les os chauffés sont interdits : os permis le plus proche
        bad = Wm.sum(1) < 1e-4
        if bad.any():
            bones = [(np.array(arm_ob.data.bones[n].head_local[:]), np.array(arm_ob.data.bones[n].tail_local[:])) for n in names]
            D = np.stack([seg_dist(co[bad], a, b) for a, b in bones], 1)
            D = np.where(allowed[bad], D, 1e9)
            Wm[np.nonzero(bad)[0], np.argmin(D, 1)] = 1.0
        W = Wm / np.maximum(Wm.sum(1, keepdims=True), 1e-9)
    edges = np.array([e.vertices[:] for e in me.edges], np.int64)
    comp = components(len(co), edges)
    ids, counts = np.unique(comp, return_counts=True)
    main = ids[np.argmax(counts)]
    W = smooth(W, edges, smooth_it, lam, allowed)
    # îlots : poids du point du corps principal le plus proche
    isl = comp != main
    if isl.any():
        mi = np.nonzero(~isl)[0]
        kd = kdtree.KDTree(len(mi))
        for k, i in enumerate(mi):
            kd.insert(co[i], k)
        kd.balance()
        for i in np.nonzero(isl)[0]:
            _, k, _ = kd.find(co[i])
            W[i] = W[mi[k]]
    if helpers:
        W, names = helper_weights(W, names, arm_ob, helpers)
    W = prune(W)
    write_groups(ob, arm_ob, names, W)
    rep = "poids lissés : copie en voxels de %.3f m, chaleur en échec sur %d sommets (poids par distance), %d îlots (%d sommets)" % (
        voxel, miss, len(ids) - 1, int(isl.sum()))
    return names, W, rep


def copy_to(dst_objs, src_co, src_tris, W, names, arm_ob):
    """Recopie des poids (LOD, mèches) depuis une surface source déjà pondérée."""
    for o in dst_objs:
        co = np.array([v.co[:] for v in o.data.vertices], np.float64)
        Wd = prune(transfer(src_co, src_tris, W, co))
        write_groups(o, arm_ob, names, Wd)


def surface(ob):
    me = ob.data
    me.calc_loop_triangles()
    return (np.array([v.co[:] for v in me.vertices], np.float64),
            np.array([t.vertices[:] for t in me.loop_triangles], np.int64))


# ----------------------------------------------------------------------------- os d'aide (demi-rotation)
# Le skinning linéaire d'Unity écrase une articulation pliée (à 90°, l'anneau du milieu perd 30 % de son rayon). Un os
# d'aide par grande articulation (épaule, coude, poignet, hanche, genou, cheville, cou) : même position et même axe que
# l'os enfant, enfant du même parent, il copie la moitié de la rotation de l'os enfant (contrainte Copy Rotation, espace
# local, influence 0.5). Les sommets du pli passent sur lui : chaque côté du pli ne tourne que de la moitié de l'angle.
# La contrainte suit toute animation faite dans Blender (vidéos de référence) ; l'export FBX cuit le résultat dans les
# clips : Unity n'a rien à piloter.
def helper_name(n):
    if "." in n:
        a, b = n.rsplit(".", 1)
        return a + "_Aide." + b
    return n + "_Aide"


def add_helpers(arm_ob, joints, influence=0.5):
    vl = bpy.context.view_layer
    _sel([arm_ob], arm_ob)
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm_ob.data.edit_bones
    made = []
    for n in joints:
        c = eb.get(n)
        if c is None or c.parent is None:
            continue
        h = eb.new(helper_name(n))
        h.head = c.head.copy()
        h.tail = c.head + (c.tail - c.head) * 0.5
        h.roll = c.roll
        h.parent = c.parent
        h.use_connect = False
        h.use_deform = True
        made.append((h.name, n))
    bpy.ops.object.mode_set(mode="OBJECT")
    for hn, n in made:
        pb = arm_ob.pose.bones[hn]
        pb.rotation_mode = "QUATERNION"
        k = pb.constraints.new("COPY_ROTATION")
        k.target = arm_ob; k.subtarget = n
        k.target_space = "LOCAL"; k.owner_space = "LOCAL"
        k.mix_mode = "REPLACE"
        k.influence = influence
    return made


def helper_weights(W, names, arm_ob, made):
    """Redistribue les poids du pli de chaque articulation sur son os d'aide (fonction triangle : 1 au milieu du pli)."""
    names = list(names)
    for hn, n in made:
        names.append(hn)
        W = np.concatenate([W, np.zeros((len(W), 1), W.dtype)], 1)
        b = arm_ob.data.bones[n]
        child = [n] + [c.name for c in b.children_recursive]
        cm = np.array([nm in child for nm in names])
        cm[-1] = False
        wc = W[:, cm].sum(1)
        wr = W[:, ~cm].sum(1) - W[:, -1]
        s = wc + wr
        t = np.where(s > 1e-9, wc / np.maximum(s, 1e-9), 0.0)
        m = (wc > 1e-4) & (wr > 1e-4)
        hw = np.where(m, s * (1 - np.abs(2 * t - 1)), 0.0)
        fr = np.where(m & (t < 1), np.maximum(0, 1 - 2 * t) / np.maximum(1 - t, 1e-9), 1.0)
        fc = np.where(m & (t > 0), np.maximum(0, 2 * t - 1) / np.maximum(t, 1e-9), 1.0)
        rest = ~cm; rest[-1] = False
        W[:, rest] *= fr[:, None]
        W[:, cm] *= fc[:, None]
        W[:, -1] = hw
    return W, names
