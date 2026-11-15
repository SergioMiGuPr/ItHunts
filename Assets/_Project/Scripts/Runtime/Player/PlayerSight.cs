using ItHunts.Combat;
using ItHunts.Economy;
using UnityEngine;

namespace ItHunts.Player
{
    /// <summary>
    /// Règle unique « un joueur voit-il ce point ? » (corrections des étapes 8 et 9) : dans le champ de sa caméra, et aucun décor
    /// entre ses yeux et le point. Le décor, c'est tout collider solide sans Rigidbody qui n'est ni un joueur ni un ennemi
    /// (Damageable) : caisses et objets physiques ne cachent rien. Servi aux apparitions des vagues, au déblocage des goules et
    /// aux textes en monde (ligne de vue en plus du test de profondeur).
    /// </summary>
    public static class PlayerSight
    {
        private static readonly RaycastHit[] Hits = new RaycastHit[32];

        /// <summary>Yeux du joueur : la position de sa caméra si elle existe (écran partagé compris), sinon 1.6 m au-dessus des pieds.</summary>
        public static Vector3 Eye(PlayerHealth p, out Vector3 forward, out float verticalFov, out float aspect)
        {
            Camera cam = Interactables.CameraOf(p);
            if (cam != null && cam.isActiveAndEnabled)
            {
                forward = cam.transform.forward;
                verticalFov = cam.fieldOfView;
                aspect = cam.aspect;
                return cam.transform.position;
            }

            var look = p.GetComponent<PlayerLook>();
            forward = look != null ? Quaternion.Euler(look.Pitch, look.Yaw, 0f) * Vector3.forward : p.transform.forward;
            verticalFov = look != null ? look.CurrentFov : 70f;
            aspect = 16f / 9f;
            return p.transform.position + Vector3.up * 1.6f;
        }

        /// <summary>Le joueur voit-il ce point (champ de vision élargi de marginDeg, sans décor entre ses yeux et lui) ?</summary>
        public static bool VisibleTo(PlayerHealth p, Vector3 point, float marginDeg = 10f, Transform ignoreRoot = null)
        {
            if (p == null)
                return false;
            Vector3 eye = Eye(p, out Vector3 forward, out float fov, out float aspect);
            if (OutsideView(eye, forward, point, fov, aspect, marginDeg))
                return false;
            return !DecorBetween(eye, point, ignoreRoot);
        }

        /// <summary>Un joueur vivant (pas à terre ni mort) voit-il l'un de ces points ?</summary>
        public static bool SeenByAnyPlayer(Vector3 a, Vector3 b, float marginDeg = 10f, Transform ignoreRoot = null)
        {
            foreach (PlayerHealth p in PlayerHealth.All)
            {
                if (p == null || p.Dead)
                    continue;
                if (VisibleTo(p, a, marginDeg, ignoreRoot) || VisibleTo(p, b, marginDeg, ignoreRoot))
                    return true;
            }

            return false;
        }

        /// <summary>Hors du champ (demi-angle horizontal, le plus large, + marge) ?</summary>
        public static bool OutsideView(Vector3 eye, Vector3 forward, Vector3 point, float verticalFov, float aspect, float marginDeg)
        {
            float half = Mathf.Atan(Mathf.Tan(verticalFov * 0.5f * Mathf.Deg2Rad) * Mathf.Max(1f, aspect)) * Mathf.Rad2Deg;
            return Vector3.Angle(forward, point - eye) > half + marginDeg;
        }

        /// <summary>Un décor coupe-t-il le segment ? (colliders sous ignoreRoot ignorés : l'objet qui porte le texte, la goule elle-même.)</summary>
        public static bool DecorBetween(Vector3 from, Vector3 to, Transform ignoreRoot = null)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.01f)
                return false;
            int n = Physics.RaycastNonAlloc(from, d / len, Hits, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (IsDecor(Hits[i].collider, ignoreRoot))
                    return true;
            return false;
        }

        /// <summary>Ce collider est-il du décor (cache la vue) ?</summary>
        public static bool IsDecor(Collider c, Transform ignoreRoot = null)
        {
            if (c == null || c.isTrigger || !c.enabled)
                return false;
            if (ignoreRoot != null && c.transform.IsChildOf(ignoreRoot))
                return false;
            return c.attachedRigidbody == null && c.GetComponentInParent<PlayerHealth>() == null && c.GetComponentInParent<Damageable>() == null;
        }
    }
}
