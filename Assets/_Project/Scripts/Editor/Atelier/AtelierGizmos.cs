using ItHunts.Enemies;
using ItHunts.Level;
using UnityEditor;
using UnityEngine;

namespace ItHunts.EditorTools
{
    /// <summary>Étiquettes lisibles des éléments de gameplay dans la vue Scene (atelier) : entrées, coffres, apparitions, passages…</summary>
    public static class AtelierGizmos
    {
        private static GUIStyle _style;

        private static void Label(Vector3 p, string text, Color c)
        {
            if (_style == null)
                _style = new GUIStyle(EditorStyles.whiteBoldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
            _style.normal.textColor = c;
            Handles.Label(p, text, _style);
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Door(ModuleDoor d, GizmoType t) =>
            Label(d.transform.position + Vector3.up * (d.height + 1f), $"[ENTRÉE {d.side} {d.cell}] {d.width:0}×{d.height:0} m{(d.required ? " obligatoire" : "")}", new Color(0.3f, 1f, 0.5f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Chest(ChestSpot c, GizmoType t) =>
            Label(c.transform.position + Vector3.up * 1.6f, "[COFFRE] " + new[] { "facile", "moyen", "difficile" }[Mathf.Clamp(c.difficulty, 0, 2)], new Color(1f, 0.85f, 0.2f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Spawn(SpawnPoint s, GizmoType t) =>
            Label(s.transform.position + Vector3.up * 1.5f, s.Accepts(SpawnKinds.Volant) ? "[APPARITION] volants" : "[APPARITION] goules", new Color(1f, 0.4f, 0.4f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Resident(ResidentSpot r, GizmoType t) =>
            Label(r.transform.position + Vector3.up * 1.6f, r.flyer ? "[HABITANT] volant" : "[HABITANT] goule", new Color(0.5f, 1f, 0.5f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Passage(StalkerPassage p, GizmoType t)
        {
            Label(p.transform.position + Vector3.up * 2f, "[TRAQUEUR] " + p.kind, new Color(1f, 0.3f, 0.3f));
            if (p.end != null)
            {
                Handles.color = new Color(1f, 0.3f, 0.3f);
                Handles.DrawDottedLine(p.transform.position, p.end.position, 4f);
            }
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Merchant(MerchantSpot m, GizmoType t) => Label(m.transform.position + Vector3.up * 2.4f, "[MARCHAND]", new Color(1f, 0.75f, 0.2f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Locked(LockedRoomSpot l, GizmoType t) => Label(l.transform.position + Vector3.up * 6.5f, "[SALLE VERROUILLÉE]", new Color(0.9f, 0.4f, 1f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Arrival(StalkerArrivalSpot a, GizmoType t) => Label(a.transform.position + Vector3.up * 15f, "[ARRIVÉE DU TRAQUEUR]", new Color(1f, 0.2f, 0.2f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Plate(MagneticPlate p, GizmoType t) => Label(p.transform.position + Vector3.up * 2f, "[MAGNÉTIQUE]", new Color(0.4f, 0.7f, 1f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Zone(ModuleBounds b, GizmoType t) =>
            Label(b.transform.TransformPoint(b.center + Vector3.up * b.size.y * 0.5f), $"[ZONE DU MODULE] {b.size.x:0} × {b.size.z:0} × {b.size.y:0} m", new Color(1f, 0.85f, 0.2f));

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        private static void Grab(ItHunts.Orbs.Grabbable g, GizmoType t)
        {
            if (g.gameObject.scene.IsValid() && Object.FindFirstObjectByType<AtelierStage>() != null)
                Label(g.transform.position + Vector3.up * 1.4f, "[ATTRAPABLE]", new Color(0.75f, 0.5f, 1f));
        }
    }
}
