using System.IO;
using UnityEngine;

namespace ItHunts.Debugging
{
    /// <summary>
    /// Outil de contrôle (éditeur, captures) : enregistre une caméra image par image en PNG à cadence fixe (Time.captureFramerate),
    /// pour des vidéos de contrôle sans package (assemblées ensuite avec ffmpeg). Suivi optionnel d'une cible (caméra de profil).
    /// </summary>
    public sealed class FrameDump : MonoBehaviour
    {
        public Camera cam;
        public string folder;
        public int frames = 150;
        public int fps = 30;
        public int width = 960, height = 540;
        [Tooltip("Cible suivie (null : caméra fixe), décalage de la caméra dans le repère de la cible, point visé au-dessus d'elle.")]
        public Transform follow;
        public Vector3 followOffset = new Vector3(24f, 8f, 4f);
        public float lookHeight = 6f;

        public int Written { get; private set; }
        public bool Done => Written >= frames;

        private RenderTexture _rt;
        private Texture2D _tex;

        private void Start()
        {
            Directory.CreateDirectory(folder);
            Time.captureFramerate = fps;
            _rt = new RenderTexture(width, height, 24);
            _tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        }

        private void LateUpdate()
        {
            if (Done || cam == null)
                return;
            if (follow != null)
            {
                cam.transform.position = follow.position + follow.rotation * followOffset;
                cam.transform.LookAt(follow.position + Vector3.up * lookHeight);
            }

            cam.targetTexture = _rt;
            cam.Render();
            RenderTexture.active = _rt;
            _tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            _tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(folder, $"f{Written:0000}.png"), _tex.EncodeToPNG());
            Written++;
            if (Done)
                Time.captureFramerate = 0;
        }

        private void OnDestroy()
        {
            Time.captureFramerate = 0;
            if (_rt != null)
                Destroy(_rt);
            if (_tex != null)
                Destroy(_tex);
        }
    }
}
