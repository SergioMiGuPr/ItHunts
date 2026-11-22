using ItHunts.Art.UIJeu;
using ItHunts.Combat;
using ItHunts.Economy;
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
            DamageEvents.Hit += OnHit;
            BuildMarker();
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

        private void OnDestroy()
        {
            ParryEvents.Parried -= OnParried;
            DamageEvents.Hit -= OnHit;
        }

        // ---------- Marqueur de touche (nuit 3, point 6) ----------

        private Image _marker;
        private float _markerAt = float.NegativeInfinity;
        private bool _markerCrit, _markerKill;
        /// <summary>Dernier marqueur montré (tests, captures) : critique ou non.</summary>
        public bool LastMarkerCritical => _markerCrit;
        public float LastMarkerTime => _markerAt;

        /// <summary>
        /// Marqueur de touche : l'étoile encrée du réticule de la DA, tournée de 45° (un X), blanche pour une touche, dorée et plus grande
        /// pour un critique (headshot), rouge sang au kill. Il éclate puis s'efface en 0.3 s.
        /// </summary>
        private MarqueurCritique _crit;
        private PanneauPorte _door;

        private void BuildMarker()
        {
            HudSkinSettings skin = _hud != null ? _hud.HudSkin : null;
            if (skin != null && skin.critPrefab != null)
                _crit = Spawn<MarqueurCritique>(skin.critPrefab, "Critique (DA)");
            if (_crit != null)
                _crit.Pose(1f);                                          // masqué jusqu'au premier critique
            if (skin != null && skin.doorPanelPrefab != null)
            {
                _door = Spawn<PanneauPorte>(skin.doorPanelPrefab, "Porte (DA)");
                if (_door != null)
                    _door.gameObject.SetActive(false);
            }

            if (_reticle == null || _reticle.etoile == null)
                return;
            var go = new GameObject("Marqueur de touche", typeof(RectTransform));
            go.layer = _canvas.gameObject.layer;
            go.transform.SetParent(_canvas.transform, false);
            _marker = go.AddComponent<Image>();
            _marker.sprite = _reticle.etoile.sprite;
            _marker.preserveAspect = true;
            _marker.raycastTarget = false;
            var r = (RectTransform)go.transform;
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.sizeDelta = ((RectTransform)_reticle.etoile.transform).rect.size * 1.6f;
            r.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _marker.enabled = false;
        }

        private void OnHit(DamageEvent e)
        {
            var hp = GetComponent<PlayerHealth>();
            if (_marker == null || hp == null || e.AttackerId != hp.PlayerId || (!e.Bullet && !e.Melee))
                return;
            _markerAt = Time.time;
            _markerCrit = e.Critical;
            _markerKill = e.Killed;
            if (e.Critical && _crit != null)
                _crit.Affiche();                                         // marqueur de critique de la DA
        }

        /// <summary>Menu d'interaction des portes payantes (Panneau_Porte) : nom, prix en âmes, payable ou non.</summary>
        private void UpdateDoorPanel()
        {
            if (_door == null)
                return;
            var interactor = GetComponent<PlayerInteractor>();
            var hp = GetComponent<PlayerHealth>();
            IInteractable f = interactor != null ? interactor.Focus : null;
            var run = ItHunts.Artifacts.RunState.Current;
            int souls = run != null && hp != null ? run.Souls(hp.PlayerId) : 0;
            if (f is ItHunts.Level.ExitDoor exit && !exit.Open)
            {
                _door.Montre(exit.Unlocked ? "Porte de sortie" : "Sortie verrouillée", exit.Unlocked ? exit.Price : 0, souls);
                PlayerHud.DoorPanelShown = true;
            }
            else if (f is LockedDoor locked && !locked.Open)
            {
                _door.Montre("Salle verrouillée", locked.PriceFor(hp), souls);
                PlayerHud.DoorPanelShown = true;
            }
            else
            {
                if (_door.gameObject.activeSelf)
                    _door.Cache();
                PlayerHud.DoorPanelShown = false;
            }
        }

        private void UpdateMarker()
        {
            if (_marker == null)
                return;
            float u = (Time.time - _markerAt) / 0.3f;
            bool on = u >= 0f && u < 1f;
            _marker.enabled = on;
            if (!on)
                return;
            float pop = 1f + (_markerCrit ? 0.6f : 0.25f) * Mathf.Sin(Mathf.Min(1f, u * 3f) * Mathf.PI);
            float scale = (_markerCrit ? 1.5f : 1f) * pop;
            _marker.rectTransform.localScale = Vector3.one * scale;
            Color c = _markerKill ? new Color(0.95f, 0.22f, 0.15f) : _markerCrit ? new Color(1f, 0.82f, 0.25f) : new Color(1f, 0.97f, 0.92f);
            c.a = 1f - u * u;
            _marker.color = c;
        }

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
            UpdateMarker();
            UpdateDoorPanel();

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
