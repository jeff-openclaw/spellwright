using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Spellwright.LLM
{
    /// <summary>
    /// Pre-loads LLamaSharp's native libraries before any LLamaSharp type is used.
    ///
    /// <para>
    /// LLamaSharp's NativeLibraryConfig.WithSearchDirectory is not available in
    /// .NET Standard 2.0 (Unity). This class uses dlopen to manually load the
    /// native libraries from Assets/Plugins/macOS/ so that LLamaSharp's P/Invoke
    /// calls can resolve them.
    /// </para>
    /// </summary>
    public static class NativeLibraryLoader
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        private const int RTLD_NOW = 2;
        private const int RTLD_GLOBAL = 8;

        [DllImport("libdl.dylib")]
        private static extern IntPtr dlopen(string path, int flags);

        [DllImport("libdl.dylib")]
        private static extern IntPtr dlerror();
#endif

        // Libraries in dependency order (leaf deps first)
        private static readonly string[] LibraryNames = new[]
        {
            "libggml-base",
            "libggml-cpu",
            "libggml-blas",
            "libggml-metal",
            "libggml",
            "libllama",
            "libmtmd"
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadNativeLibraries()
        {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            var pluginDir = Path.Combine(Application.dataPath, "Plugins", "macOS");

            if (!Directory.Exists(pluginDir))
            {
                Debug.LogWarning($"[NativeLibraryLoader] Plugin directory not found: {pluginDir}");
                return;
            }

            foreach (var libName in LibraryNames)
            {
                var path = Path.Combine(pluginDir, libName + ".dylib");
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[NativeLibraryLoader] Missing: {libName}.dylib");
                    continue;
                }

                var handle = dlopen(path, RTLD_NOW | RTLD_GLOBAL);
                if (handle == IntPtr.Zero)
                {
                    var error = Marshal.PtrToStringAnsi(dlerror());
                    Debug.LogError($"[NativeLibraryLoader] Failed to load {libName}: {error}");
                }
                else
                {
                    Debug.Log($"[NativeLibraryLoader] Loaded {libName}");
                }
            }
#endif
        }
    }
}
