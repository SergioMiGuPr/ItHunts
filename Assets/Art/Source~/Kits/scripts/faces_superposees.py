# -*- coding: utf-8 -*-
"""Contrôle des pièces des kits (nuit 4, lot 2) : faces superposées (coplanaires qui se recouvrent, cause des lignes et des
clignotements en z-fighting sur les rebords) et maillages invalides (faces dégénérées, normales nulles).

    blender -b --factory-startup --python faces_superposees.py -- <dossier FBX> <rapport.json> [--corrige <dossier sortie>]

Détection : faces regroupées par plan (normale arrondie à 1e-3, distance à 1 mm près, dans les deux sens) ; deux faces d'un
même plan se recouvrent si leurs polygones projetés se coupent sur plus de 1 cm². La correction se fait à la source :
`kitlib.export` appelle `nettoie` sur chaque objet avant l'export (le réexport d'un FBX importé changeait l'orientation
de certaines pièces dans Unity : à ne pas utiliser). Face entièrement recouverte dans le même sens : supprimée ; faces
internes dos à dos : supprimées ; recouvrement partiel : la plus petite face avance de 2 à 8 mm le long de sa normale.
"""
import bpy, bmesh, sys, os, json, math
from mathutils import Vector




def aire_inter(pa, pb):
    """Aire d'intersection de deux polygones convexes 2D (Sutherland-Hodgman)."""
    def clip(sub, a, b):
        out = []
        for i in range(len(sub)):
            p, q = sub[i], sub[(i + 1) % len(sub)]
            ip = (b[0] - a[0]) * (p[1] - a[1]) - (b[1] - a[1]) * (p[0] - a[0])
            iq = (b[0] - a[0]) * (q[1] - a[1]) - (b[1] - a[1]) * (q[0] - a[0])
            if ip >= 0:
                out.append(p)
            if (ip >= 0) != (iq >= 0):
                t = ip / (ip - iq)
                out.append((p[0] + t * (q[0] - p[0]), p[1] + t * (q[1] - p[1])))
        return out
    def aire(p):
        return 0.5 * sum(p[i][0] * p[(i + 1) % len(p)][1] - p[(i + 1) % len(p)][0] * p[i][1] for i in range(len(p)))
    if aire(pa) < 0: pa = pa[::-1]
    if aire(pb) < 0: pb = pb[::-1]
    r = pa
    for i in range(len(pb)):
        if not r: return 0.0
        r = clip(r, pb[i], pb[(i + 1) % len(pb)])
    return abs(aire(r)) if len(r) >= 3 else 0.0


def analyse(obj, corrige):
    me = obj.data
    bm = bmesh.new(); bm.from_mesh(me)
    bm.transform(obj.matrix_world)
    bmesh.ops.triangulate(bm, faces=bm.faces[:])
    bm.faces.ensure_lookup_table()
    degen = [f for f in bm.faces if f.calc_area() < 1e-8]
    plans = {}
    for f in bm.faces:
        if f.calc_area() < 1e-8: continue
        n = f.normal.normalized()
        if n.length < 0.5: continue
        # sens indifférent : on range n et -n ensemble
        if (n.x, n.y, n.z) < (0, 0, 0): n = -n
        d = n.dot(f.verts[0].co)
        cle = (round(n.x, 3), round(n.y, 3), round(n.z, 3), round(d, 3))
        plans.setdefault(cle, []).append(f)
    paires, ote, partiels, pousse = [], set(), [], set()
    for cle, fs in plans.items():
        if len(fs) < 2: continue
        n = Vector(cle[:3]).normalized()
        u = n.orthogonal().normalized(); v = n.cross(u)
        P = [[(vv.co.dot(u), vv.co.dot(v)) for vv in f.verts] for f in fs]
        for i in range(len(fs)):
            for j in range(i + 1, len(fs)):
                if set(fs[i].verts) == set(fs[j].verts) or len(set(fs[i].verts) & set(fs[j].verts)) == 3:
                    a = fs[i].calc_area()
                else:
                    a = aire_inter(P[i], P[j])
                if a > 1e-4:
                    petite = fs[i] if fs[i].calc_area() <= fs[j].calc_area() else fs[j]
                    meme_sens = fs[i].normal.dot(fs[j].normal) > 0
                    grande = fs[j] if petite is fs[i] else fs[i]
                    if meme_sens and a >= 0.95 * petite.calc_area():
                        paires.append((fs[i].index, fs[j].index, round(a, 4)))
                        ote.add(petite)               # entièrement recouverte, même sens : retirée, sans trou
                    elif not meme_sens and a >= 0.95 * petite.calc_area() and a >= 0.95 * grande.calc_area():
                        paires.append((fs[i].index, fs[j].index, round(a, 4)))
                        ote.add(petite); ote.add(grande)   # faces internes dos à dos (deux blocs accolés) : invisibles
                    else:
                        partiels.append(round(a, 4))
                        # recouvrement partiel, ou faces dos à dos : la plus petite (les deux si dos à dos) avance de
                        # 2 mm le long de sa normale ; plus de plan commun, plus de clignotement
                        pousse.add(petite)
                        if not meme_sens:
                            pousse.add(fs[j] if petite is fs[i] else fs[i])
    res = {"faces": len(bm.faces), "degenerees": len(degen), "superposees": len(paires),
           "aire_m2": round(sum(p[2] for p in paires), 3), "partielles": len(partiels), "aire_partielle_m2": round(sum(partiels), 3)}
    if corrige and (ote or degen or pousse):
        pousse -= ote
        if pousse:
            fl = [f for f in pousse if f.is_valid]
            normales = [f.normal.copy() for f in fl]
            dup = bmesh.ops.duplicate(bm, geom=fl)
            fmap = dup["face_map"]
            nouvelles = [(fmap[f], n) for f, n in zip(fl, normales) if f in fmap]
            bmesh.ops.delete(bm, geom=fl, context="FACES")
            bouge = {}
            for f, n in nouvelles:
                for v in f.verts:
                    bouge.setdefault(v, n)
            for v, n in bouge.items():
                v.co += n * 0.002 * (1 + (v.index % 4))   # décalage varié : des faces poussées ensemble ne restent pas coplanaires
        bmesh.ops.delete(bm, geom=[f for f in (ote | set(degen)) if f.is_valid], context="FACES")
        bm.transform(obj.matrix_world.inverted())
        bm.to_mesh(me); me.update()
    bm.free()
    return res




def nettoie(obj, passes=8):
    """Appelé par kitlib.export avant l'export : corrige l'objet jusqu'à ce qu'il ne reste aucune face superposée."""
    tot = None
    for _ in range(passes):
        r = analyse(obj, True)
        tot = tot or r
        if not (r["superposees"] or r["partielles"] or r["degenerees"]):
            break
    return tot


if __name__ == "__main__":
    argv = sys.argv[sys.argv.index("--") + 1:]
    DOS, RAP = argv[0], argv[1]
    CORR = argv[argv.index("--corrige") + 1] if "--corrige" in argv else None
    rapport = {}
    for fn in sorted(os.listdir(DOS)):
        if not fn.lower().endswith(".fbx"): continue
        bpy.ops.wm.read_factory_settings(use_empty=True)
        bpy.ops.import_scene.fbx(filepath=os.path.join(DOS, fn))
        tot = {"faces": 0, "degenerees": 0, "superposees": 0, "aire_m2": 0.0, "partielles": 0, "aire_partielle_m2": 0.0}
        for o in [o for o in bpy.context.scene.objects if o.type == "MESH"]:
            r = analyse(o, CORR is not None)
            for k in tot: tot[k] += r[k]
        rapport[fn] = tot
        print(fn, tot, flush=True)
        if CORR and (tot["superposees"] or tot["degenerees"] or tot["partielles"]):
            os.makedirs(CORR, exist_ok=True)
            # mêmes réglages que kitlib.py (l'import a ramené la pièce dans le repère de Blender)
            bpy.ops.export_scene.fbx(filepath=os.path.join(CORR, fn), use_selection=False, object_types={"MESH", "EMPTY"},
                                     apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", global_scale=1.0,
                                     axis_forward="-Z", axis_up="Y", bake_space_transform=True,
                                     use_mesh_modifiers=True, mesh_smooth_type="FACE", use_triangles=True, use_tspace=False,
                                     add_leaf_bones=False)
    json.dump(rapport, open(RAP, "w", encoding="utf-8"), indent=1)
