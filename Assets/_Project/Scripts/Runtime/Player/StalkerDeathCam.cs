using ItHunts.Enemies;
using ItHunts.Hud;
using ItHunts.Input;
using ItHunts.Level;
using UnityEngine;
using UnityEngine.UI;

namespace ItHunts.Player
{
    /// <summary>
    /// Mort par le traqueur (nuit 4, point 7, recherche § 6 : Alien Isolation, Dead by Daylight) : le joueur perd le contrôle, la vue est
    /// tournée de force vers la tête du traqueur et s'y approche (gros plan), il hurle gueule ouverte (StalkerFace), flash et coupure, secousse,
    /// puis fondu au noir et l'écran de fin. Durée 3 s (passable après 1 s avec Entrée / A). Option « Réduire les animations » : pas de
    /// visage plein écran (la vue reste à distance), flash remplacé par un fondu, cri à −12 dB, secousse × 0.3 ; même durée.
    /// Réseau : mise en scène locale au joueur tué, rien à synchroniser.
    /// </summary>
    public sealed class StalkerDeathCam : MonoBehaviour
    {
        public const float FullDuration = 3f;
        public static bool Active { get; private set; }
        public static float Duration => FullDuration;
        /// <summary>Mises en scène jouées (mesures).</summary>
        public static int Count { get; private set; }

        private PlayerHealth _victim;
        private PlayerLook _look;
        private PlayerInputReader _reader;
        private CameraShake _shake;
        private Stalker _stalker;
        private float _t0;
        private bool _reduced;
        private Image _flash, _fade;
        private Canvas _canvas;
        private bool _cried, _bit;
        private Vector3 _startOffset;

        public static void Begin(PlayerHealth victim)
        {
            if (victim == null || victim.GetComponent<StalkerDeathCam>() != null)
                return;
            Stalker stalker = FindFirstObjectByType<Stalker>();
            if (stalker == null)
                return;
            var cam = victim.gameObject.AddComponent<StalkerDeathCam>();
            cam._stalker = stalker;
        }

        private void Awake()
        {
            _victim = GetComponent<PlayerHealth>();
            _look = GetComponent<PlayerLook>();
            _reader = GetComponent<PlayerInputReader>();
            _shake = GetComponentInChildren<CameraShake>(true);
            if (_shake == null && _look != null && _look.ViewRig != null)
                _shake = _look.ViewRig.GetComponentInChildren<CameraShake>(true);
        }

        private void OnEnable()
        {
            _t0 = Time.unscaledTime;
            _reduced = GameSettings.ReduceMotion;
            Active = true;
            Count++;
            if (_reader != null)
                _reader.Override = _ => default;                        // plus aucune entrée : le joueur est pris
            BuildOverlay();
            // Post-traitement de la DA (PR #50, ScreamerMort) : volume global à priorité 200, désaturé puis rouge sang, sursaut, distorsion.
            var go = new GameObject("Screamer (DA)");
            go.transform.SetParent(transform, false);
            go.AddComponent<UnityEngine.Rendering.Volume>();
            _screamer = go.AddComponent<ItHunts.Art.VFX.ScreamerMort>();
        }

        private ItHunts.Art.VFX.ScreamerMort _screamer;

        private void OnDisable()
        {
            Active = false;
            if (_reader != null)
                _reader.Override = null;                                // relecture : sinon un joueur relevé en coop ne répond plus
            if (_screamer != null)
            {
                _screamer.Arrete();
                Destroy(_screamer.gameObject);
            }
            if (_look != null)
            {
                _look.ViewOffset = Vector3.zero;
                _look.FovOverride = 0f;
            }

            if (_canvas != null)
                Destroy(_canvas.gameObject);
        }

        private void BuildOverlay()
        {
            _canvas = UiKit.NewCanvas("Mort par le traqueur", 94, transform);
            _flash = Layer("Éclair", new Color(0.9f, 0.08f, 0.06f, 0f));
            _fade = Layer("Fondu", new Color(0f, 0f, 0f, 0f));
        }

        private Image Layer(string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private void LateUpdate()
        {
            if (_look == null || _stalker == null)
                return;
            float t = Time.unscaledTime - _t0;
            Transform head = _stalker.Face != null && _stalker.Face.Head != null ? _stalker.Face.Head : _stalker.transform;
            Vector3 headPos = head.position;
            if (head == _stalker.transform)
                headPos += Vector3.up * 11f;
            // 1. Vue tournée de force vers la tête (vite : 0.25 s), puis tenue.
            Vector3 eye = _look.ViewRig != null ? _look.ViewRig.position - _look.ViewOffset : transform.position + Vector3.up * 1.6f;
            Vector3 d = headPos - eye;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            float k = Mathf.Clamp01(t / 0.25f);
            _look.SetView(Mathf.LerpAngle(_look.Yaw, yaw, k >= 1f ? 1f : k * 0.6f), Mathf.Lerp(_look.Pitch, Mathf.Clamp(pitch, -80f, 80f), k >= 1f ? 1f : k * 0.6f));
            // 2. Gros plan : la caméra avance vers le visage (à 1.2 m), FOV resserré ; en mode réduit elle reste sur place, FOV normal.
            float approach = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - 0.2f) / 0.9f));
            if (!_reduced)
            {
                Vector3 target = headPos - d.normalized * 1.2f;
                _look.ViewOffset = Vector3.Lerp(Vector3.zero, target - eye, approach);
                _look.FovOverride = Mathf.Lerp(0f, 46f, approach) > 1f ? Mathf.Lerp(90f, 46f, approach) : 0f;
            }

            // 3. Cri gueule ouverte, flash et secousse au moment où la tête arrive (0.35 s), morsure à 1.3 s.
            if (!_cried && t >= 0.3f)
            {
                _cried = true;
                _stalker.ForceCry(2f);
                if (_screamer != null)
                    _screamer.Joue(_reduced);
                var art = ItHunts.Audio.GameAudio.Settings;
                float vol = _reduced ? 0.25f : 1f;
                ItHunts.Audio.GameAudio.PlayAt(art != null ? art.stalkerGrabCry : null, headPos, vol);
                ItHunts.Audio.GameAudio.PlayAt(ItHunts.Audio.GameAudio.Pick(art != null ? art.stalkerEnragedCries : null), headPos, vol * 0.8f);
                Shake(1f);
            }

            if (!_bit && t >= 1.3f)
            {
                _bit = true;
                var art = ItHunts.Audio.GameAudio.Settings;
                ItHunts.Audio.GameAudio.PlayAt(art != null ? art.bossBite : null, headPos, _reduced ? 0.3f : 1f);
                Shake(0.8f);
            }

            // 4. Éclair rouge bref (0.3 s) puis fondu au noir sur les 0.6 dernières secondes ; mode réduit : fondu doux dès 0.3 s.
            float flash = _reduced ? 0f : Mathf.Clamp01(1f - Mathf.Abs(t - 0.45f) / 0.3f) * 0.85f;
            _flash.color = new Color(_flash.color.r, _flash.color.g, _flash.color.b, flash);
            float fade = _reduced ? Mathf.Clamp01((t - 0.3f) / (FullDuration - 0.5f)) : Mathf.Clamp01((t - (FullDuration - 0.6f)) / 0.6f);
            _fade.color = new Color(0f, 0f, 0f, fade);
            if (!_reduced && t > 0.5f)
                _look.ViewOffset += new Vector3(Mathf.Sin(t * 37f), Mathf.Cos(t * 29f), 0f) * 0.02f * GameFeel.Intensity;   // le traqueur le secoue
            if (t >= FullDuration + 0.5f)
                Destroy(this);
        }

        private void Shake(float amount)
        {
            if (_shake == null)
                return;
            float scale = (GameSettings.ReduceShake || _reduced ? 0.3f : 1f) * GameFeel.Intensity;
            _shake.AddTrauma(amount * scale);
        }
    }
}
