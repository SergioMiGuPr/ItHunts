using System.Collections;
using System.IO;
using UnityEngine;

namespace ItHunts.Art.Eau
{
    /// <summary>
    /// Contrôle que l'eau bouge vraiment en jeu et dans un build (2026-11-22) : deux captures d'écran à `intervalle` s d'écart,
    /// écart moyen entre elles dans une zone d'eau et dans une zone de décor fixe (référence), écrits dans `controle_eau.txt`
    /// avec les deux images. Dans un build : lancé avec `-eauControle <dossier>`, le jeu quitte une fois la mesure faite.
    /// À poser sur un objet de la scène d'essai (Test_Eau) ; sans l'argument, la mesure se fait aussi en Play Mode et le
    /// résultat part dans la console et dans Application.persistentDataPath.
    /// </summary>
    public sealed class EauControleMouvement : MonoBehaviour
    {
        [Tooltip("Attente avant la première capture (s).")]
        public float attente = 1.5f;
        [Tooltip("Écart entre les deux captures (s).")]
        public float intervalle = 0.5f;
        [Tooltip("Zone d'eau à l'écran (x, y, largeur, hauteur en fraction de l'écran, origine en bas à gauche).")]
        public Rect zoneEau = new Rect(0.3f, 0.05f, 0.4f, 0.3f);
        [Tooltip("Zone de décor fixe (référence : bruit de la mesure, flammes des torches exclues).")]
        public Rect zoneFixe = new Rect(0.02f, 0.55f, 0.15f, 0.2f);

        private IEnumerator Start()
        {
            string dossier = Application.persistentDataPath;
            bool quitter = false;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-eauControle") { dossier = args[i + 1]; quitter = true; }
            Directory.CreateDirectory(dossier);
            yield return new WaitForSeconds(attente);
            yield return new WaitForEndOfFrame();
            var a = ScreenCapture.CaptureScreenshotAsTexture();
            float t0 = Time.time;
            yield return new WaitForSeconds(intervalle);
            yield return new WaitForEndOfFrame();
            var b = ScreenCapture.CaptureScreenshotAsTexture();
            float dt = Time.time - t0;
            float eau = Ecart(a, b, zoneEau), fixe = Ecart(a, b, zoneFixe);
            string txt = $"build={!Application.isEditor} ecart_s={dt:0.000} eau={eau:0.00} fixe={fixe:0.00} (écart moyen /255, zone d'eau et zone fixe) " +
                         $"ecran={Screen.width}x{Screen.height} {SystemInfo.graphicsDeviceType}";
            File.WriteAllText(Path.Combine(dossier, "controle_eau.txt"), txt);
            File.WriteAllBytes(Path.Combine(dossier, "controle_eau_a.png"), a.EncodeToPNG());
            File.WriteAllBytes(Path.Combine(dossier, "controle_eau_b.png"), b.EncodeToPNG());
            Debug.Log("[EauControleMouvement] " + txt);
            Destroy(a); Destroy(b);
            if (quitter) Application.Quit();
        }

        private static float Ecart(Texture2D a, Texture2D b, Rect z)
        {
            int x0 = Mathf.RoundToInt(z.x * a.width), y0 = Mathf.RoundToInt(z.y * a.height);
            int w = Mathf.RoundToInt(z.width * a.width), h = Mathf.RoundToInt(z.height * a.height);
            var pa = a.GetPixels(x0, y0, w, h); var pb = b.GetPixels(x0, y0, w, h);
            double s = 0;
            for (int i = 0; i < pa.Length; i++)
                s += Mathf.Abs(pa[i].r - pb[i].r) + Mathf.Abs(pa[i].g - pb[i].g) + Mathf.Abs(pa[i].b - pb[i].b);
            return (float)(s / (pa.Length * 3) * 255.0);
        }
    }
}
