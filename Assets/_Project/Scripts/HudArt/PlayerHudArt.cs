using ItHunts.Art.UIJeu;
using ItHunts.Combat;
using ItHunts.Hud;
using ItHunts.Menus;
using ItHunts.Orbs;
using ItHunts.Player;
using ItHunts.Settings;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ItHunts.HudArt
{
    /// <summary>
    /// Interface en jeu de la DA (PR #21, UI-HANDOFF § 7) sur le HUD d'un joueur : réticule à 3 états (neutre, attrapable,
    /// ennemi), 3 charges de mouvement en bas de l'écran, recharge de la parade à leur droite (éclat au contre réussi). Canevas
    /// propre en 1920 × 1080 (hauteur), sur la caméra du HUD du joueur (écran partagé compris). Le HUD masque alors ses formes
    /// provisoires (point, anneau de la télékinésie, segments des charges, icône de la parade).
    /// </summary>
    [DefaultExecutionOrder(210)]
    public sealed class PlayerHudArt : MonoBehaviour
    {
        private PlayerHud _hud;
        private PlayerMotor _motor;
        private PlayerParry _parry;
        private Camera _worldCamera;
        private Canvas _canvas;
        private ReticuleJeu _reticle;
        private ChargesJeu _charges;
        private ParadeJeu _parade;

        public ReticuleJeu Reticle => _reticle;
        public ChargesJeu Charges => _charges;
        public ParadeJeu Parade => _parade;
        public EtatReticule ReticleState { get; private set; }

        public bool Build(PlayerHud hud)
        {
            HudSkinSettings skin = hud != null ? hud.HudSkin : null;
            if (skin == null || (skin.reticlePrefab == null && skin.chargesPrefab == null && skin.parryPrefab == null) || hud.HudCanvas == null)
                return false;
            _hud = hud;
            _motor = GetComponent<PlayerMotor>();
            _parry = GetComponent<PlayerParry>();
            var pi = GetComponent<PlayerInput>();
            _worldCamera = pi != null ? pi.camera : null;

            Canvas source = hud.HudCanvas;
            var go = new GameObject("HUD de la DA (réticule, charges, parade)", typeof(RectTransform));
            go.transform.SetParent(source.transform.parent, false);
            go.layer = source.gameObject.layer;
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = source.renderMode;
            _canvas.worldCamera = source.worldCamera;
            _canvas.planeDistance = source.planeDistance * 0.98f;   // devant le HUD
            _canvas.sortingOrder = source.sortingOrder + 1;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            _reticle = Spawn<ReticuleJeu>(skin.reticlePrefab, "Réticule");
            _charges = Spawn<ChargesJeu>(skin.chargesPrefab, "Charges");
            _parade = Spawn<ParadeJeu>(skin.parryPrefab, "Parade");
            if (_parade != null && _parry == null)
                _parade.gameObject.SetActive(false);
            ParryEvents.Parried += OnParried;
            hud.UseArtGauges();
            return true;
        }

        private T Spawn<T>(GameObject prefab, string name) where T : Component
        {
            if (prefab == null)
                return null;
            GameObject go = Instantiate(prefab, _canvas.transform, false);
            go.name = name;
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = _canvas.gameObject.layer;
            foreach (Graphic g in go.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;
            return go.GetComponent<T>();
        }

        private void OnDestroy() => ParryEvents.Parried -= OnParried;

        private void OnParried(ParryEvent e)
        {
            if (_parade != null && e.Player == transform)
                _parade.Contre();
        }

        private void LateUpdate()
        {
            if (_hud == null || _motor == null)
                return;
            if (_charges != null)
            {
                float c = Mathf.Max(0f, _motor.State.Charges);
                int full = Mathf.FloorToInt(c + 1e-4f);
                _charges.RegleTout(full, c - full);
            }

            if (_parade != null && _parry != null)
                _parade.Regle(_parry.Fill);

            if (_reticle != null)
            {
                ReticleState = Aim();
                _reticle.SetEtat(ReticleState);
            }
        }

        /// <summary>Attrapable : la télékinésie vise un objet qu'elle peut prendre ; ennemi : un ennemi sous le viseur (60 m).</summary>
        private EtatReticule Aim()
        {
            TelekinesisState t = _motor.Telekinesis;
            if (_motor.TelekinesisSettings != null && t.HeldId == 0 && t.AimHit && !t.AimTooHeavy)
                return EtatReticule.Attrapable;
            Camera cam = _worldCamera;
            if (cam == null)
                return EtatReticule.Neutre;
            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Ignore))
            {
                Damageable d = hit.collider.GetComponentInParent<Damageable>();
                if (d != null && d.transform != transform && !d.Dead)
                    return EtatReticule.Ennemi;
            }

            return EtatReticule.Neutre;
        }
    }
}
