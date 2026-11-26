using System.IO;
using UnityEngine;

namespace ItHunts.Level
{
    /// <summary>
    /// Plantage à la fermeture du build (D3D12, D-121) : en détruisant le périphérique, Unity enregistre son cache de pipelines
    /// (dx12_pso_cache_lib.bin, dans le dossier temporaire du jeu) ; quand ce cache a été écrit par un build précédent, la sérialisation
    /// lit une mémoire libérée (D3D12Core, pile D3D12PipelineCacheLibrary::SaveCache) et le jeu plante en quittant. Vérifié le
    /// 2026-11-26 : avec le cache du build du 24/11, plantage ; cache effacé, 3 fermetures propres sur 3. Au lancement d'un build
    /// nouveau (identifiant de build différent de celui noté), le cache est effacé : il se reconstruit pendant la partie.
    /// </summary>
    public static class PsoCacheGuard
    {
        private const string CacheFile = "dx12_pso_cache_lib.bin", Marker = "ithunts_pso_build.txt";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Check()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                string dir = Path.Combine(Path.GetTempPath(), Application.companyName, Application.productName);
                string marker = Path.Combine(dir, Marker), cache = Path.Combine(dir, CacheFile);
                string build = Application.buildGUID;
                if (string.IsNullOrEmpty(build) || (File.Exists(marker) && File.ReadAllText(marker).Trim() == build))
                    return;
                Directory.CreateDirectory(dir);
                if (File.Exists(cache))
                    File.Delete(cache);
                File.WriteAllText(marker, build);
                Debug.Log($"Cache des pipelines D3D12 effacé (build {build}) : il sera reconstruit");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Cache des pipelines D3D12 : {e.Message}");
            }
#endif
        }
    }
}
