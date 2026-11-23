# -*- coding: utf-8 -*-
"""Contrôles des pièces des kits (nuit 4, lot 3, corrections) sur les FBX exportés :

1. **maillages** : faces superposées par objet et sur la pièce entière (tous ses objets réunis), faces dégénérées,
   sommets isolés, arêtes nulles, pente maximale des rampes `Col_Rampe` ;
2. **parties mobiles** : à 0, 50 et 100 % de leur course, intersection (arbres BVH) entre chaque partie mobile et la
   maçonnerie de la pièce (objet principal et enfants fixes, sans les `Col_*` ni les `Deco_*`).

Mouvements (axes et angles Unity, docs/art/INTEGRATION-PROPS.md « Nuit 4, lot 3 ») convertis dans le repère de Blender :
x Unity = -x Blender, y Unity = z Blender, z Unity = -y Blender ; R_blender = M⁻¹ · R_unity · M.

    blender -b --factory-startup -P controle_lot3.py -- <rapport_maillages.json> <rapport_mobiles.json> <fbx> [<fbx> …]
"""
import bpy, bmesh, sys, os, json, math
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import faces_superposees as FS

M = Matrix(((-1, 0, 0), (0, 0, 1), (0, -1, 0)))          # Blender -> Unity
Mi = M.inverted()


def rot_unity(axe, deg):
    """Rotation Unity (Quaternion.Euler sur un axe) exprimée dans le repère de Blender (matrice 4×4)."""
    Ru = Matrix.Rotation(math.radians(deg), 3, axe)
    return (Mi @ Ru @ M).to_4x4()


def trans_unity(v):
    return Matrix.Translation(Mi @ Vector(v))


def mouvements(nom_piece, n):
    """Liste de phases [(objets, fonction(f) -> matrice monde relative au pivot ou translation)] pour une partie n."""
    if n.startswith("Battant_"):
        g = n.endswith("_G")
        if "Barricade" in nom_piece or "Voliere" in nom_piece:
            return [("rotation Y", "Y", 100 if g else -100)]
        return [("rotation Y", "Y", -90 if g else 90)]
    if n.startswith("Trappe_"):
        return [("rotation Z", "Z", -110 if n.endswith("_G") else 110)]
    if n == "Herse":
        return [("translation Y", None, 9.2)]
    return []


def bvh_de(objs, mats=None):
    bm = bmesh.new()
    for o in objs:
        t = bmesh.new()
        t.from_mesh(o.data)
        mw = (mats or {}).get(o.name, o.matrix_world)
        t.transform(mw)
        me = bpy.data.meshes.new("_tmp")
        t.to_mesh(me)
        t.free()
        bm.from_mesh(me)
        bpy.data.meshes.remove(me)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    tree = BVHTree.FromBMesh(bm)
    nb = len(bm.faces)
    bm.free()
    return tree, nb


def maillage(o):
    me = o.data
    used = set(v for p in me.polygons for v in p.vertices)
    isoles = len(me.vertices) - len(used)
    nulles = sum(1 for e in me.edges if (me.vertices[e.vertices[0]].co - me.vertices[e.vertices[1]].co).length < 1e-5)
    return isoles, nulles


def pente_col(o):
    mw = o.matrix_world
    pm = None
    for p in o.data.polygons:
        n = (mw.to_3x3() @ p.normal).normalized()
        if abs(n.z) > 0.5:   # surfaces marchables (rampes, paliers) ; les parois verticales des Col_* ne comptent pas
            a = math.degrees(math.acos(min(1.0, abs(n.z))))
            pm = a if pm is None else max(pm, a)
    return round(pm, 1) if pm is not None else None


def controle(fbx):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=fbx)
    nom = os.path.splitext(os.path.basename(fbx))[0]
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    rm = {"objets": sorted(o.name.split(".")[0] for o in bpy.context.scene.objects), "isoles": 0, "aretes_nulles": 0,
          "faces_superposees": 0, "faces_partielles": 0, "faces_degenerees": 0, "pente_max_col": None, "triangles": 0}
    for o in meshes:
        i, a = maillage(o)
        rm["isoles"] += i
        rm["aretes_nulles"] += a
        r = FS.analyse(o, False)
        rm["faces_superposees"] += r["superposees"]
        rm["faces_partielles"] += r["partielles"]
        rm["faces_degenerees"] += r["degenerees"]
        if o.name.startswith("Col_Rampe"):
            p = pente_col(o)
            if p is not None:
                rm["pente_max_col"] = p if rm["pente_max_col"] is None else max(rm["pente_max_col"], p)
        if not o.name.startswith("Col_"):
            rm["triangles"] += sum(len(p.vertices) - 2 for p in o.data.polygons)
    # pièce entière : recouvrements entre objets d'une même pièce (sans les parties mobiles, qui bougent)
    mob = ("Battant", "Vantail", "Serrure", "Herse", "Trappe")
    fixes = [o for o in meshes if not o.name.startswith(mob)]
    if len(fixes) > 1:
        bm = bmesh.new()
        for o in fixes:
            t = bmesh.new(); t.from_mesh(o.data); t.transform(o.matrix_world)
            me = bpy.data.meshes.new("_t"); t.to_mesh(me); t.free(); bm.from_mesh(me); bpy.data.meshes.remove(me)
        me = bpy.data.meshes.new("_piece"); bm.to_mesh(me); bm.free()
        ob = bpy.data.objects.new("_piece", me); bpy.context.scene.collection.objects.link(ob)
        r = FS.analyse(ob, False)
        rm["piece_entiere_superposees"] = r["superposees"] + r["partielles"]
        bpy.data.objects.remove(ob)
    # parties mobiles
    statiques = [o for o in meshes if not o.name.startswith(mob) and not o.name.startswith("Col_") and not o.name.startswith("Deco_")]
    mobiles = [o for o in meshes if o.name.startswith(mob)]
    rmob = {}
    if mobiles:
        tree_s, _ = bvh_de(statiques)
        vantail = next((o for o in mobiles if o.name.startswith("Vantail")), None)
        serrure = next((o for o in mobiles if o.name.startswith("Serrure")), None)
        for o in mobiles:
            n = o.name.split(".")[0]
            piv = o.matrix_world.translation.copy()
            res = []
            if n == "Serrure":
                # 1. quart de tour autour de Z (Unity), dans les deux sens ; 2. monte avec le vantail (+3.1 m)
                for sens in (90, -90):
                    for f in (0.0, 0.5, 1.0):
                        mw = Matrix.Translation(piv) @ rot_unity("Z", sens * f) @ Matrix.Translation(-piv) @ o.matrix_world
                        t, _ = bvh_de([o], {o.name: mw})
                        res.append(dict(phase=f"rotation Z {sens:+d}°", course=f, intersections=len(tree_s.overlap(t))))
                for f in (0.0, 0.5, 1.0):
                    mw = trans_unity((0, 3.1 * f, 0)) @ Matrix.Translation(piv) @ rot_unity("Z", 90) @ Matrix.Translation(-piv) @ o.matrix_world
                    t, _ = bvh_de([o], {o.name: mw})
                    res.append(dict(phase="translation Y +3.1 m (avec le vantail)", course=f, intersections=len(tree_s.overlap(t))))
            elif n == "Vantail":
                for f in (0.0, 0.5, 1.0):
                    mw = trans_unity((0, 3.1 * f, 0)) @ o.matrix_world
                    t, _ = bvh_de([o], {o.name: mw})
                    res.append(dict(phase="translation Y +3.1 m", course=f, intersections=len(tree_s.overlap(t))))
            else:
                for (phase, axe, val) in mouvements(nom, n):
                    for f in (0.0, 0.5, 1.0):
                        if axe:
                            mw = Matrix.Translation(piv) @ rot_unity(axe, val * f) @ Matrix.Translation(-piv) @ o.matrix_world
                        else:
                            mw = trans_unity((0, val * f, 0)) @ o.matrix_world
                        t, _ = bvh_de([o], {o.name: mw})
                        res.append(dict(phase=f"{phase} {val:+g}", course=f, intersections=len(tree_s.overlap(t))))
            # sens d'ouverture (contrôle) : déplacement du centre de la partie à 100 %, en axes Unity
            rmob[n] = dict(pivot_unity=[round(x, 3) for x in (M @ piv)], phases=res,
                           ok=all(r_["intersections"] == 0 for r_ in res))
    return nom, rm, rmob


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:]
    rap_m, rap_mob = argv[0], argv[1]
    R1, R2 = {}, {}
    for f in argv[2:]:
        nom, rm, rmob = controle(f)
        R1[nom] = rm
        if rmob:
            R2[nom] = rmob
        print("controle", nom, rm, {k: v["ok"] for k, v in rmob.items()}, flush=True)
    if rap_m != "-":
        json.dump(R1, open(rap_m, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    if rap_mob != "-":
        json.dump(R2, open(rap_mob, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
