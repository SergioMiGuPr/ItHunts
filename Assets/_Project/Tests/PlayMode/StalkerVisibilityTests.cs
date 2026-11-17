using System.Collections;
using System.Linq;
using ItHunts.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ItHunts.Tests
{
    /// <summary>
    /// Playtest du 2026-11-17 : on entendait le traqueur sans le voir (corps gréé noir dans l'obscurité). Devant la caméra à 10, 30
    /// et 60 m, à découvert : ses rendus sont actifs, dans le champ, éclairés par sa lumière d'appoint (calque « Traqueur »), et
    /// son corps n'est pas noir à l'image ; les anciennes pièces des bras ne sont pas dessinées en double.
    /// </summary>
    public class StalkerVisibilityTests
    {
        private const string ScenePath = "Assets/Scenes/Test_Vitrine.unity";

        [UnityTest]
        public IEnumerator Traqueur_VisibleDevantLaCamera_A10_30_60m()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Test de l'éditeur (scène chargée par son chemin).");
#endif
            for (int i = 0; i < 10; i++)
                yield return null;
            var stalker = Object.FindFirstObjectByType<Stalker>();
            Assert.IsNotNull(stalker, "pas de traqueur dans " + ScenePath);
            stalker.enabled = false;                                     // immobile
            // À découvert, à hauteur du sol mais loin du décor de la vitrine : seul ce qui l'éclaire lui-même compte.
            stalker.transform.SetPositionAndRotation(new Vector3(stalker.transform.position.x + 500f, stalker.transform.position.y, 0f),
                Quaternion.identity);
            Physics.SyncTransforms();
            yield return null;

            var body = stalker.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r => r.name == "SK_Traqueur");
            Assert.IsNotNull(body, "corps gréé SK_Traqueur");
            Assert.IsNotNull(stalker.SelfLight, "lumière d'appoint du traqueur");
            Assert.AreNotEqual(0u, body.renderingLayerMask & (uint)stalker.SelfLight.renderingLayerMask, "corps sur le calque de sa lumière");
            foreach (SkinnedMeshRenderer smr in stalker.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Assert.AreEqual(smr.sharedMesh.bindposes.Length, smr.bones.Length, $"{smr.name} : os du rendu et du maillage");
            foreach (MeshRenderer piece in stalker.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.name.StartsWith("Bras") || r.name.StartsWith("Main")))
                Assert.IsTrue(piece.forceRenderingOff || !piece.enabled, $"ancienne pièce « {piece.name} » dessinée en double");

            var go = new GameObject("Caméra du test");
            var cam = go.AddComponent<Camera>();
            cam.fieldOfView = 70f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            var rt = new RenderTexture(320, 180, 24);
            var tex = new Texture2D(320, 180, TextureFormat.RGB24, false);
            cam.targetTexture = rt;
            try
            {
                foreach (float d in new[] { 10f, 30f, 60f })
                {
                    Vector3 center = body.bounds.center;
                    Vector3 eye = stalker.transform.position + stalker.transform.forward * d + Vector3.up * 1.7f;
                    go.transform.SetPositionAndRotation(eye, Quaternion.LookRotation(center - eye));
                    yield return null;
                    cam.Render();
                    Assert.IsTrue(body.enabled && body.gameObject.activeInHierarchy && !body.forceRenderingOff, $"{d} m : rendu actif");
                    Assert.IsTrue(GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(cam), body.bounds), $"{d} m : dans le champ");
                    Assert.IsTrue(body.isVisible, $"{d} m : rendu dessiné (isVisible)");

                    RenderTexture.active = rt;
                    tex.ReadPixels(new Rect(0, 0, 320, 180), 0, 0);
                    tex.Apply();
                    RenderTexture.active = null;
                    Vector3 a = cam.WorldToScreenPoint(body.bounds.min), b = cam.WorldToScreenPoint(body.bounds.max);
                    int x0 = Mathf.Clamp((int)Mathf.Min(a.x, b.x), 0, 319), x1 = Mathf.Clamp((int)Mathf.Max(a.x, b.x), 1, 320);
                    int y0 = Mathf.Clamp((int)Mathf.Min(a.y, b.y), 0, 179), y1 = Mathf.Clamp((int)Mathf.Max(a.y, b.y), 1, 180);
                    float lit = 0f, brightest = 0f;
                    int n = 0;
                    for (int y = y0; y < y1; y++)
                        for (int x = x0; x < x1; x++)
                        {
                            Color c = tex.GetPixel(x, y);
                            float l = 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
                            if (l > 0.06f)
                                lit++;
                            brightest = Mathf.Max(brightest, l);
                            n++;
                        }

                    // Le brouillard des ambiances sombres est total vers 35-38 m : de près le corps est éclairé (lumière d'appoint),
                    // de loin ce sont les yeux (émission qui traverse le brouillard) qui le trahissent.
                    if (d <= 10f)
                        Assert.Greater(lit / Mathf.Max(1, n), 0.1f, $"{d} m : corps presque noir à l'image ({lit}/{n} pixels éclairés)");
                    else if (d <= 30f)
                        Assert.Greater(lit / Mathf.Max(1, n), 0.02f, $"{d} m : corps presque noir à l'image ({lit}/{n} pixels éclairés)");
                    else
                        Assert.Greater(brightest, 0.06f, $"{d} m : rien ne le trahit dans le brouillard (yeux éteints)");
                    Debug.Log($"Traqueur à {d} m : {lit}/{n} pixels éclairés, luminance max {brightest:0.000}");
                }
            }
            finally
            {
                cam.targetTexture = null;
                Object.Destroy(rt);
                Object.Destroy(go);
            }
        }
    }
}
